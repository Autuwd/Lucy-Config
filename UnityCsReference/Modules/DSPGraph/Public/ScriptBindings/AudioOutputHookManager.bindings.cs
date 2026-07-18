// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// ============================================================
// 🎯 AudioOutputHookManager —— 音频输出钩子管理器
//     创建/销毁音频输出的拦截钩子
// 💡 Internal_CreateAudioOutputHook：创建输出钩子
//     jobReflectionData + jobData 绑定 Job 处理
// 💡 Internal_DisposeAudioOutputHook：释放钩子
// 📌 用于低层音频输出监听和中间件集成
// ============================================================
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeHeader("Modules/DSPGraph/Public/AudioOutputHookManager.bindings.h")]
    internal struct AudioOutputHookManager
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_CreateAudioOutputHook(out Handle outputHook, void* jobReflectionData, void* jobData);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_DisposeAudioOutputHook(ref Handle outputHook);
    }
}

