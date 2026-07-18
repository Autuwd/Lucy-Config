// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// ============================================================
// 🎯 AudioMemoryManager —— DSP 音频内存管理器
//     分配/释放音频管线专用的原生内存
// 💡 Internal_AllocateAudioMemory：按对齐分配
// 💡 Internal_FreeAudioMemory：释放内存
// ⚡ 用于 DSPNode 内部工作缓冲区的分配
// ============================================================
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeHeader("Modules/DSPGraph/Public/AudioMemoryManager.bindings.h")]
    internal struct AudioMemoryManager
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = false)]
        public static extern unsafe void* Internal_AllocateAudioMemory(int size, int alignment);

        [NativeMethod(IsFreeFunction = true, ThrowsException = false)]
        public static extern unsafe void Internal_FreeAudioMemory(void* memory);
    }
}

