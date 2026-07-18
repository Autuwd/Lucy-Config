// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// ============================================================
// 🎯 DSPSampleProvider —— DSP 采样供给器绑定
//     在 DSP 图内部读取 AudioSampleProvider 的采样数据
// 💡 支持多种格式：UInt8 / SInt16 / Float
// 💡 通过 providerId 或原生 provider 指针访问
// 💡 GetChannelCount / GetSampleRate 获取格式信息
// ⚡ 全部标记 IsThreadSafe，可在音频线程调用
// 📌 用于 DSPNode 内部实时读取音频数据
// ============================================================
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeHeader("Modules/DSPGraph/Public/DSPSampleProvider.bindings.h")]
    internal partial struct DSPSampleProviderInternal
    {
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadUInt8FromSampleProvider(void* provider, int format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadSInt16FromSampleProvider(void* provider, int format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadFloatFromSampleProvider(void* provider, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe ushort Internal_GetChannelCount(void* provider);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe uint Internal_GetSampleRate(void* provider);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadUInt8FromSampleProviderById(uint providerId, int format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadSInt16FromSampleProviderById(uint providerId, int format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadFloatFromSampleProviderById(uint providerId, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe ushort Internal_GetChannelCountById(uint providerId);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe uint Internal_GetSampleRateById(uint providerId);
    }
}

