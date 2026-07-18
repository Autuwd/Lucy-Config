// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 DataPlayableOutput — 通用数据流输出
//
// 📌 作用：
//   DataPlayableOutput 将 Playables 输出端扩展到自定义数据流。
//   它允许将任意 TDataStream 类型的数据从 PlayableGraph
//   输出到实现了 IDataPlayer 接口的消费者。
//
// 🏗 核心链路：
//   PlayableGraph ──→ DataPlayable（数据处理节点）
//     → DataPlayableOutput（数据输出端）
//       → IDataPlayer（数据消费者，如 MonoBehaviour）
//
// 🔑 核心操作：
//   Create<TDataStream>(graph, name) — 创建泛型数据输出
//   GetDataStream<T>() / SetDataStream<T>() — 数据流读写
//   GetPlayer() / SetPlayer<T>() — 绑定数据消费者
//   GetConnectionChanged() — 检测连接状态变化
//
// 🎯 生命周期管理（重要的 Native → C# 回调）：
//   Internal_CallOnPlayerChanged 是 [RequiredByNativeCode] 方法，
//   在 Player 变更时由 C++ 端自动回调：
//   - 旧 Player.Release(output) — 释放资源
//   - 新 Player.Bind(output) — 建立连接
//   这种模式实现了自动的资源生命周期管理。
//
// 📍 对应 C++ 头文件：Modules/Director/DataPlayableOutput.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    // ==============================================================
    // DataPlayableOutput — 通用数据流输出
    //
    // 🔑 关键字段：
    //   m_Handle (PlayableOutputHandle) — 包装的底层 Output 句柄
    //
    // 🔑 核心操作：
    //   Create<TDataStream>(graph, name) — 创建泛型数据输出
    //   GetDataStream<T>() / SetDataStream<T>() — 数据流读写
    //   GetPlayer() / SetPlayer<T>() — 绑定数据消费者
    //   GetStreamType() — 获取数据流类型
    //   GetConnectionChanged() — 检测连接状态变化
    //
    // 🏗 完整数据流 Playables 管线：
    //   DataPlayable（数据处理节点，用户自定义逻辑）
    //     → DataPlayableOutput（数据输出端）
    //       → IDataPlayer（数据消费者，如 MonoBehaviour）
    //
    // 💡 与 IDataPlayer 的生命周期管理：
    //   Internal_CallOnPlayerChanged 是 [RequiredByNativeCode] 回调，
    //   在 Player 变更时由 C++ 端自动调用：
    //   1. 旧 IDataPlayer.Release(output) — 释放旧资源
    //   2. 新 IDataPlayer.Bind(output) — 建立新连接
    //   这种模式实现了"连接即绑定，断开即释放"的自动生命周期。
    // ==============================================================
    [NativeHeader("Modules/Director/ScriptBindings/DataPlayableOutput.bindings.h")]
    [NativeHeader("Modules/Director/ScriptBindings/DataPlayableOutputExtensions.bindings.h")]
    [NativeHeader("Modules/Director/DataPlayableOutput.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("DataPlayableOutputBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    internal struct DataPlayableOutput : IPlayableOutput
    {
        private PlayableOutputHandle m_Handle;

        // 🎯 获取数据流的运行时类型
        public System.Type GetStreamType() { return InternalGetType(ref m_Handle); }

        // 🎯 检测连接是否发生变化
        public bool GetConnectionChanged() { return InternalGetConnectionChanged(ref m_Handle); }

        // 🎯 清除连接变化标记
        public void ClearConnectionChanged() { InternalClearConnectionChanged(ref m_Handle); }

        // ==============================================================
        // GetDataStream — 获取数据流对象
        //
        // 🎯 从输出端读取自定义数据流
        // 💡 类型不匹配时返回 default(TDataStream)
        // ==============================================================
        public TDataStream GetDataStream<TDataStream>()
            where TDataStream: new()
        {
            object stream = InternalGetStream(ref m_Handle);
            if (stream is TDataStream)
            {
                return (TDataStream)stream;
            }
            return default;
        }

        // ==============================================================
        // SetDataStream — 设置数据流对象
        //
        // 🎯 向输出端写入自定义数据流
        // ⚡ 类型安全检查：抛出 ArgumentException 如果类型不匹配
        // ==============================================================
        public void SetDataStream<TDataStream>(TDataStream stream)
            where TDataStream : new()
        {
            Type streamType = GetStreamType();
            if ( !streamType.IsAssignableFrom(typeof(TDataStream)) )
                throw new ArgumentException($"{nameof(stream)} is of the wrong type. This output only accepts streams with type {streamType} or inheriting from type {streamType}", nameof(stream));

            InternalSetStream(ref m_Handle, stream);
        }

        // ==============================================================
        // Create — 创建数据流输出
        //
        // 🎯 在 PlayableGraph 中创建泛型数据输出节点
        // 📌 TDataStream 类型决定了输出端处理的数据类型
        // ==============================================================
        public static DataPlayableOutput Create<TDataStream>(PlayableGraph graph, string name)
            where TDataStream : new()
        {
            PlayableOutputHandle handle;
            if (!DataPlayableOutputExtensions.InternalCreateDataOutput(ref graph, name, typeof(TDataStream), out handle))
                return Null;

            DataPlayableOutput output = new DataPlayableOutput(handle);

            return output;
        }

        // ==============================================================
        // 构造函数 — 带类型安全检查的 Handle 包装
        //
        // ⚡ 确保传入的 Handle 是 DataPlayableOutput 类型
        // ==============================================================
        internal DataPlayableOutput(PlayableOutputHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOutputOfType<DataPlayableOutput>())
                    throw new InvalidCastException("Can't set handle: the playable is not a DataPlayableOutput.");
            }

            m_Handle = handle;
        }

        // 🎯 Null 实例
        public static DataPlayableOutput Null
        {
            get { return new DataPlayableOutput(PlayableOutputHandle.Null); } 
        }

        public PlayableOutputHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator PlayableOutput(DataPlayableOutput output)
        {
            return new PlayableOutput(output.GetHandle());
        }

        public static explicit operator DataPlayableOutput(PlayableOutput output)
        {
            return new DataPlayableOutput(output.GetHandle());
        }

        // ==============================================================
        // GetPlayer / SetPlayer — 绑定数据消费者
        //
        // 🎯 Player 是实现 IDataPlayer 的数据消费者
        // 📌 SetPlayer 约束 TPlayer : Object, IDataPlayer
        // ==============================================================
        public IDataPlayer GetPlayer()
        {
            return InternalGetPlayer(ref m_Handle) as IDataPlayer;
        }

        public void SetPlayer<TPlayer>(TPlayer player) where TPlayer: Object, IDataPlayer
        {
            InternalSetPlayer(ref m_Handle, player);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static Object InternalGetPlayer(ref PlayableOutputHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void InternalSetPlayer(ref PlayableOutputHandle handle, Object player);

        [NativeMethod(ThrowsException = true)]
        private extern static Type InternalGetType(ref PlayableOutputHandle handle);

        [NativeMethod(ThrowsException = true)]
        private extern static void InternalSetStream(ref PlayableOutputHandle handle, object stream);

        [NativeMethod(ThrowsException = true)]
        private extern static object InternalGetStream(ref PlayableOutputHandle handle);

        [NativeMethod(ThrowsException = true)]
        private extern static bool InternalGetConnectionChanged(ref PlayableOutputHandle handle);

        [NativeMethod(ThrowsException = true)]
        private extern static void InternalClearConnectionChanged(ref PlayableOutputHandle handle);

        // ==============================================================
        // Internal_CallOnPlayerChanged — Native 端 Player 变更回调
        //
        // 🎯 [RequiredByNativeCode]：由 C++ 端自动调用
        // ⚡ 旧 Player.Release(output) → 新 Player.Bind(output)
        // 💡 这是自动资源生命周期管理的核心
        // ==============================================================
        [RequiredByNativeCode]
        private static void Internal_CallOnPlayerChanged(PlayableOutputHandle handle, object previousPlayer, object currentPlayer)
        {
            var output = new DataPlayableOutput(handle);
            if (previousPlayer is IDataPlayer previousDataPlayer)
            {
                previousDataPlayer.Release(output);
            }

            if (currentPlayer is IDataPlayer currentDataPlayer)
            {
                currentDataPlayer.Bind(output);
            }
        }
    }
}
