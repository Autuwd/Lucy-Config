// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// 🎯 AudioPlayableGraphExtensions —— 内部 PlayableGraph 音频扩展
//     在 Graph 中创建音频输出（AudioPlayableOutput 的底层）
// 💡 InternalCreateAudioOutput 被 AudioPlayableOutput.Create 调用
// 📌 internal 类，仅引擎内部使用
// ============================================================
using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Audio
{

    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioPlayableGraphExtensions.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("AudioPlayableGraphExtensionsBindings", StaticAccessorType.DoubleColon)]
    internal static class AudioPlayableGraphExtensions
    {
        [NativeMethod(ThrowsException = true)]
        extern internal static bool InternalCreateAudioOutput(ref PlayableGraph graph, string name, out PlayableOutputHandle handle);
    }

}
