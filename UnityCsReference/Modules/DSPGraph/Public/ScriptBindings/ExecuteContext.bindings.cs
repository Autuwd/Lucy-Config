// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// ============================================================
// 🎯 ExecuteContext —— DSP 执行上下文
//     在 DSP 节点处理过程中发布事件
// 💡 Internal_PostEvent：向指定 DSPNode 投递事件
//     事件通过 typeHashCode 标识类型
// ⚡ 标记 IsThreadSafe，可在音频线程调用
// 📌 用于 DSPNode 间的信号/事件通信
// ============================================================
using System;
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeHeader("Modules/DSPGraph/Public/ExecuteContext.bindings.h")]
    internal unsafe struct ExecuteContextInternal
    {
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        public static extern unsafe void Internal_PostEvent(void* dspNodePtr, long eventTypeHashCode, void* eventPtr, int eventSize);
    }
}

