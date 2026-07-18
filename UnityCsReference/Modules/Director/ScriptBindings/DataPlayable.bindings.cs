// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 DataPlayableBindings — 自定义数据流 Playable 绑定
//
// 📌 作用：
//   DataPlayableBindings 提供了在 PlayableGraph 中创建
//   自定义数据流 Playable 的底层绑定。它允许用户通过
//   泛型类型 TDataStream 定义自定义数据在 Playables 系统中
//   的流动和处理方式。
//
// 💡 理解关键（数据流 Playables 架构）：
//   传统 Playables 处理的是动画、音频、纹理等引擎内建类型。
//   DataPlayable + DataPlayableOutput 将 Playables 架构
//   泛化到任意自定义数据类型，实现数据驱动的游戏逻辑管道。
//
// 🔑 核心方法：
//   CreateHandleInternal — 在指定图中创建一个数据 Playable 句柄
//
// 📍 对应 C++ 头文件：Modules/Director/ScriptBindings/DataPlayable.bindings.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    // ==============================================================
    // DataPlayableBindings — 自定义数据流 Playable 的底层绑定
    //
    // 🔑 核心操作：
    //   CreateHandleInternal — 在 PlayableGraph 中创建数据 Playable 句柄
    //
    // 💡 这是 DataPlayable 的"桥梁"类：
    //   DataPlayable 是 Users 可以编写自定义数据的 Playable 类型，
    //   通过 TDataStream 泛型参数定义数据类型。
    //   DataPlayableBindings 封装了 C++ 端的创建调用。
    //
    // 📍 对应 C++ 头文件：Modules/Director/ScriptBindings/DataPlayable.bindings.h
    // ==============================================================
    [NativeHeader("Modules/Director/ScriptBindings/DataPlayable.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("DataPlayableBindings", StaticAccessorType.DoubleColon)]
    static class DataPlayableBindings
    {
        [NativeMethod(ThrowsException = true)]
        extern public static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);
    }
}
