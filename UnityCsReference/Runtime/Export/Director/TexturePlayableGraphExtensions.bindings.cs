// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 TexturePlayableGraphExtensions — 纹理输出创建扩展
//
// 📌 作用：
//   提供在 PlayableGraph 中创建纹理输出（TexturePlayableOutput）
//   的扩展方法。这是 TexturePlayableOutput.Create() 的底层实现。
//
// 💡 理解关键：
//   InternalCreateTextureOutput 在 C++ 端的 PlayableGraph 中
//   注册一个纹理类型的 Output，返回对应的 PlayableOutputHandle。
//   TexturePlayableOutput 再基于这个 Handle 进行封装。
//
// 📍 对应 C++ 头文件：Runtime/Export/Director/TexturePlayableGraphExtensions.bindings.h
// ==============================================================

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Experimental.Playables
{

    // ==============================================================
    // TexturePlayableGraphExtensions — 纹理输出创建扩展
    //
    // 🔑 核心操作：
    //   InternalCreateTextureOutput — 在图中创建纹理类型 Output
    //
    // 💡 这是 TexturePlayableOutput.Create() 的底层桥梁：
    //   C#: TexturePlayableOutput.Create(graph, name, target)
    //     ↓ 调用
    //   C#: TexturePlayableGraphExtensions.InternalCreateTextureOutput()
    //     ↓ Native 绑定
    //   C++: TexturePlayableGraphExtensionsBindings::InternalCreateTextureOutput
    //     → 在 C++ 端创建对应的 TexturePlayableOutput 对象
    // ==============================================================
    [NativeHeader("Runtime/Export/Director/TexturePlayableGraphExtensions.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("TexturePlayableGraphExtensionsBindings", StaticAccessorType.DoubleColon)]
    internal static class TexturePlayableGraphExtensions
    {
        [NativeMethod(ThrowsException = true)]
        extern internal static bool InternalCreateTextureOutput(ref PlayableGraph graph, string name, out PlayableOutputHandle handle);
    }

}
