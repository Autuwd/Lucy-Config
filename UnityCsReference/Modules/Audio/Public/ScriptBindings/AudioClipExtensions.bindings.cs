// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// ============================================================
// 🎯 AudioClipExtensions —— AudioClip 扩展方法
//     Internal_CreateAudioClipSampleProvider 创建采样供给器
// 💡 用于从 AudioClip 生成实时采样数据流
// 📌 internal 类，仅引擎内部调用
// ============================================================
using UnityEngine.Bindings;

namespace UnityEngine.Experimental.Audio
{
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioClipExtensions.bindings.h")]
    [NativeHeader("Modules/Audio/Public/AudioClip.h")]
    [NativeHeader("AudioScriptingClasses.h")]
    internal static class AudioClipExtensionsInternal
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern uint Internal_CreateAudioClipSampleProvider([NotNull] this AudioClip audioClip, ulong start, long end, bool loop, bool allowDrop, bool loopPointIsStart = false);
    }
}

