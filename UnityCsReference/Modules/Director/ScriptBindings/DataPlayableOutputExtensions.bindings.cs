// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 DataPlayableOutputExtensions — 数据输出创建扩展
//
// 📌 作用：
//   提供在 PlayableGraph 中创建 DataPlayableOutput 的扩展方法。
//   这是 DataPlayableOutput.Create() 的底层 Native 绑定。
//
// 💡 理解关键：
//   InternalCreateDataOutput 通过类型参数 streamType，在 C++ 端
//   创建对应类型的 DataPlayableOutput，并返回 PlayableOutputHandle。
//   这种设计使得 DataPlayableOutput 支持泛型数据流：
//   不同类型的数据流在 Native 端对应不同的内部处理逻辑。
//
// 📍 对应 C++ 头文件：Modules/Director/ScriptBindings/DataPlayableOutputExtensions.bindings.h
// ==============================================================

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Playables
{
    // ==============================================================
    // DataPlayableOutputExtensions — 数据输出创建扩展
    //
    // 🔑 核心操作：
    //   InternalCreateDataOutput — 在图中创建数据输出句柄
    //
    // 💡 这是 DataPlayableOutput.Create() 的底层桥梁：
    //   Create<TDataStream>(graph, name)
    //     → InternalCreateDataOutput(graph, name, typeof(TDataStream), out handle)
    //     → C++ 端创建对应类型的数据输出对象
    //
    // 📍 对应 C++ 头文件：Modules/Director/ScriptBindings/DataPlayableOutputExtensions.bindings.h
    // ==============================================================
    [NativeHeader("Modules/Director/ScriptBindings/DataPlayableOutputExtensions.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("DataPlayableOutputExtensionsBindings", StaticAccessorType.DoubleColon)]
    internal static class DataPlayableOutputExtensions
    {
        [NativeMethod(ThrowsException = true)]
        extern internal static bool InternalCreateDataOutput(ref PlayableGraph graph, string name, Type type, out PlayableOutputHandle handle);
    }
}
