// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// ============================================================
// 🎯 AudioSourceExtensions —— AudioSource 扩展方法
//     注册/注销 AudioSampleProvider 到 AudioSource
// 💡 RegisterSampleProvider：将自定义采样供给器绑定到 AudioSource
// 💡 UnregisterSampleProvider：解除绑定
// 📌 用于程序化音频替代传统 AudioClip
// ============================================================
using System;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.Audio.Tests")]

namespace UnityEngine.Experimental.Audio
{
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioSourceExtensions.bindings.h")]
    [NativeHeader("Modules/Audio/Public/AudioSource.h")]
    [NativeHeader("AudioScriptingClasses.h")]
    internal static class AudioSourceExtensionsInternal
    {
        public static void RegisterSampleProvider(this AudioSource source, AudioSampleProvider provider)
        {
            Internal_RegisterSampleProviderWithAudioSource(source, provider.id);
        }

        public static void UnregisterSampleProvider(this AudioSource source, AudioSampleProvider provider)
        {
            Internal_UnregisterSampleProviderFromAudioSource(source, provider.id);
        }

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        private extern static void Internal_RegisterSampleProviderWithAudioSource([NotNull] AudioSource source, uint providerId);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        private extern static void Internal_UnregisterSampleProviderFromAudioSource([NotNull] AudioSource source, uint providerId);
    }
}

