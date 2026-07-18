// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PlayableHandle — 所有 Playable 的底层句柄
//
// 📌 作用：
//   PlayableHandle 是 Playables 系统中所有可播放节点的
//   通用句柄。每个具体的 Playable 类型（AnimationClipPlayable、
//   CameraPlayable 等）内部都持有 PlayableHandle 来包装
//   C++ 端的实际 Playable 对象。
//
// 🏗 核心架构（句柄 + 版本号）：
//   m_Handle  (IntPtr)  — 指向 C++ 端 Playable 对象的指针
//   m_Version (UInt32)  — 版本号，确保操作的是正确的 Playable 实例
//
// 💡 理解关键：
//   这是"句柄模式"的经典实现：C# 端不直接持有 C++ 对象，
//   而是通过 IntPtr 间接引用，配合版本号做安全校验。
//   当 C++ 端的对象被销毁重建时，版本号会变化，
//   旧的 PlayableHandle 会通过 IsValid() 检测到失效。
//
// 🔑 PlayableHandle 提供的能力：
//   - 状态控制：Play/Pause/GetTime/SetTime/GetSpeed/SetSpeed
//   - 拓扑访问：GetInput/GetOutput/GetInputCount/GetInputWeight
//   - 图连接：通过所属的 PlayableGraph 连接其他 Playable
//   - 生命周期：IsValid/IsDone/Destroy
//   - 脚本实例：GetScriptInstance/SetScriptInstance
//
// 📍 对应 C++ 头文件：Runtime/Director/Core/HPlayable.h
// ==============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

using Object = UnityEngine.Object;


namespace UnityEngine.Playables
{
    // ==============================================================
    // PlayState — Playable 的播放状态枚举
    //
    // 🎯 Paused = 0：暂停状态
    // 🎯 Playing = 1：播放中
    // ⚠️ Delayed = 2：已废弃，用 ScriptPlayable 替代
    // ==============================================================
    public enum PlayState
    {
        Paused = 0,
        Playing = 1,
        [Obsolete("Delayed is obsolete; use a custom ScriptPlayable to implement this feature", false)]
        Delayed = 2
    }

    // ==============================================================
    // PlayableHandle — 所有 Playable 的通用底层句柄
    //
    // 🔑 关键字段：
    //   m_Handle  (IntPtr)  — 指向 C++ 端 Playable 对象的指针
    //   m_Version (UInt32)  — 版本号安全校验
    //
    // 🔑 核心方法分类：
    //   状态控制：Play/Pause/GetTime/SetTime/GetSpeed/SetSpeed
    //   拓扑访问：GetInput/GetOutput/GetInputCount/GetInputWeight
    //   生命周期：IsValid/IsDone/Destroy
    //   脚本实例：GetScriptInstance/SetScriptInstance/GetObject<T>
    //   泛型负载：GetPayload<T>/SetPayload<T>（值类型的脚本实例）
    //
    // 💡 版本号安全机制：
    //   CompareVersion 比较两个 Handle 的 m_Handle + m_Version 是否都相等。
    //   当 C++ 端对象销毁时，m_Version 变化，旧 Handle 的 IsValid() 返回 false。
    //   这防止了"悬垂指针"问题——C++ 对象销毁后 C# 端仍持有指向它的 Handle。
    //
    // 🎯 PlayableHandle 是值类型（struct）：
    //   Null = default(PlayableHandle) = { m_Handle = 0, m_Version = 0 }
    //   默认的 Null 是安全的，因为任何操作前都会通过 IsValid() 检查。
    // ==============================================================
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Export/Director/PlayableHandle.bindings.h")]
    [UsedByNativeCode]
    public struct PlayableHandle : IEquatable<PlayableHandle>
    {
        internal IntPtr m_Handle;
        internal UInt32 m_Version;

        // ==============================================================
        // GetObject<T> — 获取关联的 PlayableBehaviour 脚本实例
        //
        // 🎯 获取与 PlayableHandle 关联的类类型脚本实例
        // 📌 T : class, IPlayableBehaviour
        // 💡 如果 Handle 无效或脚本为 null，返回 null
        // ==============================================================
        internal T GetObject<T>()
            where T : class, IPlayableBehaviour
        {
            if (!IsValid())
                return null;

            var playable = GetScriptInstance();
            if (playable == null)
                return null;

            return (T)playable;
        }

        // ==============================================================
        // GetPayload<T> / SetPayload<T> — 泛型值类型脚本负载
        //
        // 🎯 与 GetObject<T> 类似，但针对 struct 值类型
        // 💡 使用场景：ScriptPlayable<T> 中 T 为 struct 时的数据访问
        // ⚡ 无效 Handle 返回 default(T)
        // ==============================================================
        [VisibleToOtherModules("UnityEngine.DirectorModule")]
        internal T GetPayload<T>()
           where T : struct
        {
            if (!IsValid())
                return default;

            var payload = GetScriptInstance();
            if (payload == null)
                return default;

            return (T)payload;
        }

        [VisibleToOtherModules("UnityEngine.DirectorModule")]
        internal void SetPayload<T>(T payload)
          where T : struct
        {
            if (!IsValid())
                return;
            SetScriptInstance(payload);
        }
        
        // 🎯 类型检查：判断 Handle 是否是泛型 T 类型
        [VisibleToOtherModules]
        internal bool IsPlayableOfType<T>()
        {
            return GetPlayableType() == typeof(T);
        }

        // 🎯 Null 实例：default(PlayableHandle)，安全值
        static readonly PlayableHandle m_Null = new PlayableHandle();
        public static PlayableHandle Null
        {
            get { return m_Null; }
        }

        // ==============================================================
        // 拓扑访问方法
        //
        // 🎯 GetInput — 获取指定输入端口连接的 Playable
        // 🎯 GetOutput — 获取指定输出端口连接的 Playable
        // 🎯 GetOutputPortFromInputConnection — 输入→输出端口映射
        // 🎯 GetInputPortFromOutputConnection — 输出→输入端口映射
        // ==============================================================
        internal Playable GetInput(int inputPort)
        {
            return new Playable(GetInputHandle(inputPort));
        }

        internal Playable GetOutput(int outputPort)
        {
            return new Playable(GetOutputHandle(outputPort));
        }

        internal int GetOutputPortFromInputConnection(int inputPort)
        {
            return GetOutputPortFromInputIndex(inputPort);
        }

        internal int GetInputPortFromOutputConnection(int inputPort)
        {
            return GetInputPortFromOutputIndex(inputPort);
        }

        // ==============================================================
        // SetInputWeight / GetInputWeight — 输入端口权重控制
        //
        // 🎯 控制混合器中各输入的混合权重
        // 💡 含边界检查：超出范围返回 false/0.0f
        // ==============================================================
        internal bool SetInputWeight(int inputIndex, float weight)
        {
            if (CheckInputBounds(inputIndex))
            {
                SetInputWeightFromIndex(inputIndex, weight);
                return true;
            }
            return false;
        }

        internal float GetInputWeight(int inputIndex)
        {
            if (CheckInputBounds(inputIndex))
            {
                return GetInputWeightFromIndex(inputIndex);
            }
            return 0.0f;
        }

        // 🎯 销毁此 Playable 节点
        internal void Destroy()
        {
            GetGraph().DestroyPlayable(new Playable(this));
        }

        // 💡 版本号比较运算
        public static bool operator==(PlayableHandle x, PlayableHandle y) { return CompareVersion(x, y); }
        public static bool operator!=(PlayableHandle x, PlayableHandle y) { return !CompareVersion(x, y); }

        public override bool Equals(object p)
        {
            return p is PlayableHandle && Equals((PlayableHandle)p);
        }

        public bool Equals(PlayableHandle other)
        {
            return CompareVersion(this, other);
        }

        // 🎯 HashCode = Handle 的 Hash XOR Version 的 Hash
        public override int GetHashCode() { return m_Handle.GetHashCode() ^ m_Version.GetHashCode(); }

        // 💡 版本号比较核心逻辑
        static internal bool CompareVersion(PlayableHandle lhs, PlayableHandle rhs)
        {
            return (lhs.m_Handle == rhs.m_Handle) && (lhs.m_Version == rhs.m_Version);
        }

        // ==============================================================
        // CheckInputBounds — 输入端口边界检查
        //
        // 🎯 确保 inputIndex 在当前输入范围内
        // 📌 acceptAny = true 时，inputIndex = -1 表示接受任意端口
        // ⚡ 越界时抛出 IndexOutOfRangeException
        // ==============================================================
        internal bool CheckInputBounds(int inputIndex)
        {
            return CheckInputBounds(inputIndex, false);
        }

        internal bool CheckInputBounds(int inputIndex, bool acceptAny)
        {
            if (inputIndex == -1 && acceptAny)
                return true;

            if (inputIndex < 0)
            {
                throw new IndexOutOfRangeException("Index must be greater than 0");
            }

            if (GetInputCount() <= inputIndex)
            {
                throw new IndexOutOfRangeException("inputIndex " + inputIndex +  " is greater than the number of available inputs (" + GetInputCount() + ").");
            }

            return true;
        }

        [VisibleToOtherModules]
        extern internal bool IsNull();

        [VisibleToOtherModules]
        extern internal bool IsValid();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetPlayableType", HasExplicitThis = true, ThrowsException = true)]
        extern internal Type GetPlayableType();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetJobType", HasExplicitThis = true, ThrowsException = true)]
        extern internal Type GetJobType();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetScriptInstance", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetScriptInstance(object scriptInstance);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::CanChangeInputs", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool CanChangeInputs();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::CanSetWeights", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool CanSetWeights();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::CanDestroy", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool CanDestroy();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetPlayState", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayState GetPlayState();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::Play", HasExplicitThis = true, ThrowsException = true)]
        extern internal void Play();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::Pause", HasExplicitThis = true, ThrowsException = true)]
        extern internal void Pause();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetSpeed", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetSpeed();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetSpeed", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetSpeed(double value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetTime();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetTime(double value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::IsDone", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool IsDone();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetDone", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetDone(bool value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetDuration", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetDuration();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetDuration", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetDuration(double value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetPropagateSetTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool GetPropagateSetTime();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetPropagateSetTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetPropagateSetTime(bool value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetGraph", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableGraph GetGraph();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetInputCount", HasExplicitThis = true, ThrowsException = true)]
        extern internal int GetInputCount();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetOutputPortFromInputIndex", HasExplicitThis = true, ThrowsException = true)]
        extern internal int GetOutputPortFromInputIndex(int index);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetInputPortFromOutputIndex", HasExplicitThis = true, ThrowsException = true)]
        extern internal int GetInputPortFromOutputIndex(int index);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetInputCount", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetInputCount(int value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetOutputCount", HasExplicitThis = true, ThrowsException = true)]
        extern internal int GetOutputCount();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetOutputCount", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetOutputCount(int value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetInputWeight", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetInputWeight(PlayableHandle input, float weight);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetDelay", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetDelay(double delay);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetDelay", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetDelay();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::IsDelayed", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool IsDelayed();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetPreviousTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetPreviousTime();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetLeadTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetLeadTime(float value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetLeadTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal float GetLeadTime();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetTraversalMode", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableTraversalMode GetTraversalMode();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetTraversalMode", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetTraversalMode(PlayableTraversalMode mode);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetJobData", HasExplicitThis = true, ThrowsException = true)]
        extern internal IntPtr GetJobData();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetTimeWrapMode", HasExplicitThis = true, ThrowsException = true)]
        extern internal DirectorWrapMode GetTimeWrapMode();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetTimeWrapMode", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetTimeWrapMode(DirectorWrapMode mode);

        [FreeFunction("PlayableHandleBindings::GetScriptInstance", HasExplicitThis = true, ThrowsException = true)]
        extern private object GetScriptInstance();

        [FreeFunction("PlayableHandleBindings::GetInputHandle", HasExplicitThis = true, ThrowsException = true)]
        extern private PlayableHandle GetInputHandle(int index);

        [FreeFunction("PlayableHandleBindings::GetOutputHandle", HasExplicitThis = true, ThrowsException = true)]
        extern private PlayableHandle GetOutputHandle(int index);

        [FreeFunction("PlayableHandleBindings::SetInputWeightFromIndex", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetInputWeightFromIndex(int index, float weight);

        [FreeFunction("PlayableHandleBindings::GetInputWeightFromIndex", HasExplicitThis = true, ThrowsException = true)]
        extern private float GetInputWeightFromIndex(int index);
    }
}
