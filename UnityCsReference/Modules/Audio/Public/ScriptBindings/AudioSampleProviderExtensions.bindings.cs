// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// 🎯 AudioSampleProviderExtensions —— 采样供给器扩展
//     提供 GetSpeed() 获取 AudioSampleProvider 播放速度
// 💡 线程安全，通过 InternalGetAudioSampleProviderSpeed 调用原生
// 📌 internal 扩展方法类
// ============================================================
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

[assembly: InternalsVisibleTo("Unity.Audio.Tests")]

namespace UnityEngine.Experimental.Audio
{
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioSampleProviderExtensions.bindings.h")]
    [StaticAccessor("AudioSampleProviderExtensionsBindings", StaticAccessorType.DoubleColon)]
    internal static class AudioSampleProviderExtensionsInternal
    {
        public static float GetSpeed(this AudioSampleProvider provider)
        {
            return InternalGetAudioSampleProviderSpeed(provider.id);
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        private extern static float InternalGetAudioSampleProviderSpeed(uint providerId);
    }
}
