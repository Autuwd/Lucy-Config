// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PlayableSystems — Playable 系统更新调度器
//
// 📌 作用：
//   PlayableSystems 管理自定义数据流 Playable 的更新生命周期。
//   它允许在 Unity 的各个更新阶段（FixedUpdate/Update/LateUpdate/
//   Render 等）注册自定义的数据处理委托。
//
// 🏗 核心架构：
//   PlayableSystems（调度器）
//     ├── 注册阶段枚举（PlayableSystemStage）
//     ├── 委托分发（PlayableSystemDelegate）
//     ├── 线程安全（ReaderWriterLockSlim）
//     └── Native 回调入口（Internal_CallSystemDelegate）
//
// 💡 理解关键（更新链路）：
//   1. 用户注册系统阶段委托 RegisterSystemPhaseDelegate
//   2. Native 端在对应的更新阶段调用 Internal_CallSystemDelegate
//   3. 查找注册的委托和对应的系统类型
//   4. 将 PlayableOutputHandle* 转换为 C# 的 DataPlayableOutputList
//   5. 调用用户的自定义处理委托
//
// 🔑 PlayableSystemStage 阶段说明：
//   FixedUpdate — 物理更新前
//   FixedUpdatePostPhysics — 物理更新后
//   Update — 主循环更新
//   AnimationBegin — 动画开始
//   AnimationEnd — 动画结束后
//   LateUpdate — 相机更新前
//   Render — 渲染前
//
// 📍 对应 C++ 头文件：Modules/Director/ScriptBindings/PlayableSystems.bindings.h
// ==============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    // ==============================================================
    // PlayableSystems — Playable 系统更新调度器
    //
    // 🔑 核心成员：
    //   PlayableSystemDelegate   — 用户自定义的处理委托
    //   PlayableSystemStage      — 更新阶段枚举
    //   s_Delegates/s_SystemTypes — 注册表
    //   s_RWLock                 — 读写锁（线程安全）
    //
    // 🏗 调度架构：
    //   注册阶段：RegisterSystemPhaseDelegate<T>(stage, delegate)
    //     → RegisterStreamStage (C++ Native 注册类型+阶段)
    //     → 存入 s_SystemTypes 和 s_Delegates
    //
    //   回调阶段：Internal_CallSystemDelegate（[RequiredByNativeCode]）
    //     ← C++ 端在对应更新阶段自动调用
    //     → 查表找到对应的委托
    //     → 构造 DataPlayableOutputList（从指针数组转换）
    //     → 调用用户委托
    //
    // 💡 线程安全：
    //   使用 ReaderWriterLockSlim 保护注册表，支持并发读写。
    //   Native 端 IsThreadSafe = true 的 RegisterStreamStage
    //   可以从任意线程调用。
    // ==============================================================
    [NativeHeader("Modules/Director/ScriptBindings/PlayableSystems.bindings.h")]
    [StaticAccessor("PlayableSystemsBindings", StaticAccessorType.DoubleColon)]
    internal static class PlayableSystems
    {
        // 🎯 用户自定义的数据处理委托签名
        public delegate void PlayableSystemDelegate(IReadOnlyList<DataPlayableOutput> outputs);

        // ==============================================================
        // PlayableSystemStage — 更新阶段枚举
        //
        // 🎯 定义了 Playable 系统在 Unity 生命周期中的更新时机
        // 📌 按执行顺序排列：
        //   FixedUpdate → FixedUpdatePostPhysics
        //   → Update → AnimationBegin → AnimationEnd
        //   → LateUpdate → Render
        // ==============================================================
        public enum PlayableSystemStage : ushort
        {
            FixedUpdate,
            FixedUpdatePostPhysics,
            Update,
            AnimationBegin,
            AnimationEnd,
            LateUpdate,
            Render
        }

        // ==============================================================
        // RegisterSystemPhaseDelegate — 注册系统阶段委托
        //
        // 🎯 在指定更新阶段注册自定义数据处理逻辑
        // 📌 TDataStream 指定数据类型
        // 📌 stage 指定在哪个更新阶段执行
        // 📌 systemDelegate 指定处理逻辑
        // ⚡ 重复注册会覆盖已有委托
        // ==============================================================
        public static void RegisterSystemPhaseDelegate<TDataStream>(PlayableSystemStage stage, PlayableSystemDelegate systemDelegate)
            where TDataStream : new()
        {
            RegisterSystemPhaseDelegate(typeof(TDataStream), stage, systemDelegate);
        }

        // 💡 内部实现：类型擦除后用 int typeIndex 标识
        static void RegisterSystemPhaseDelegate(System.Type streamType, PlayableSystemStage stage, PlayableSystemDelegate systemDelegate)
        {
            int typeIndex = RegisterStreamStage(streamType, (int)stage);
            try
            {
                s_RWLock.EnterWriteLock();
                s_SystemTypes.TryAdd(typeIndex, streamType);
                int combinedId = CombineTypeAndIndex(typeIndex, stage);
                if (!s_Delegates.TryAdd(combinedId, systemDelegate))
                {
                    s_Delegates[combinedId] = systemDelegate;
                }
            }
            finally
            {
                s_RWLock.ExitWriteLock();
            }
        }

        // 💡 将 typeIndex 和 stage 编码为唯一的 int key
        static int CombineTypeAndIndex(int typeIndex, PlayableSystemStage stage)
        {
            return typeIndex << 16 | (int)stage;
        }

        // ==============================================================
        // DataPlayableOutputList — unsafe 数据输出列表包装
        //
        // 🎯 将 C++ 端的 PlayableOutputHandle* 数组包装为 C# 的
        //    IReadOnlyList<DataPlayableOutput>，供用户委托调用。
        // 💡 使用 unsafe 代码直接从指针读取，避免数据复制。
        // ==============================================================
        private unsafe class DataPlayableOutputList : IReadOnlyList<DataPlayableOutput>
        {
            public DataPlayableOutputList(PlayableOutputHandle* outputs, int count)
            {
                m_Outputs = outputs;
                m_Count = count;
            }

            // 💡 索引器：从指针数组中读取 PlayableOutputHandle 并包装
            public DataPlayableOutput this[int index]
            {
                get
                {
                    if (index >= m_Count)
                        throw new IndexOutOfRangeException($"index {index} is greater than the number of items: {m_Count}");
                    if (index < 0)
                        throw new IndexOutOfRangeException($"index cannot be negative");

                    return new DataPlayableOutput(m_Outputs[index]);
                }
            }

            public int Count => m_Count;

            public IEnumerator<DataPlayableOutput> GetEnumerator()
            {
                return new DataPlayableOutputEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class DataPlayableOutputEnumerator : IEnumerator<DataPlayableOutput>
            {
                public DataPlayableOutputEnumerator(DataPlayableOutputList list)
                {
                    m_List = list;
                    m_Index = -1;
                }
                public DataPlayableOutput Current
                {
                    get
                    {
                        try
                        {
                            return m_List[m_Index];
                        }
                        catch (IndexOutOfRangeException)
                        {
                            throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                        }
                    }
                }

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    m_List = null;
                }

                public bool MoveNext()
                {
                    m_Index++;
                    return m_Index < m_List.Count;
                }

                public void Reset()
                {
                    m_Index = -1;
                }

                DataPlayableOutputList m_List;
                int m_Index;
            }

            PlayableOutputHandle* m_Outputs;
            int m_Count;
        }

        // ==============================================================
        // Internal_CallSystemDelegate — Native 端系统委托回调
        //
        // 🎯 [RequiredByNativeCode]：由 C++ 端在对应更新阶段自动调用
        // 📌 systemIndex — 注册时获得的系统类型索引
        // 📌 stage — 当前更新阶段
        // 📌 outputsPtr — PlayableOutputHandle* 数组指针
        // 📌 numOutputs — 数组长度
        // 💡 返回 false 表示没有找到对应的注册委托
        // ==============================================================
        [RequiredByNativeCode]
        private unsafe static bool Internal_CallSystemDelegate(int systemIndex, PlayableSystemStage stage, IntPtr outputsPtr, int numOutputs)
        {
            PlayableOutputHandle* outputs = (PlayableOutputHandle*)outputsPtr;

            int combinedId = CombineTypeAndIndex(systemIndex, stage);

            bool typeFound = false;
            bool systemFound = false;
            PlayableSystemDelegate systemDelegate = null;
            s_RWLock.EnterReadLock();
            typeFound = s_SystemTypes.TryGetValue(systemIndex, out Type systemType);
            if (typeFound)
            {
                systemFound = s_Delegates.TryGetValue(combinedId, out systemDelegate) && systemDelegate != null;
            }
            s_RWLock.ExitReadLock();

            if (!typeFound || !systemFound)
                return false;

            var outputsArgument = new DataPlayableOutputList(outputs, numOutputs);
            systemDelegate(outputsArgument);

            return true;
        }

        // 🎯 在 C++ 端注册流类型和阶段，返回类型索引
        [NativeMethod(IsThreadSafe = true)]
        private extern static int RegisterStreamStage(System.Type streamType, int stage);

        // 💡 静态构造函数：初始化注册表
        static PlayableSystems()
        {
            s_Delegates = new Dictionary<int, PlayableSystemDelegate>();
            s_SystemTypes = new Dictionary<int, Type>();
            s_RWLock = new ReaderWriterLockSlim();
        }

        // 📌 注册表：类型索引 → 系统类型
        static Dictionary<int, Type> s_SystemTypes;
        // 📌 注册表：组合 ID → 系统委托
        static Dictionary<int, PlayableSystemDelegate> s_Delegates;
        // 📌 读写锁：保护注册表线程安全
        static ReaderWriterLockSlim s_RWLock;
    }
}
