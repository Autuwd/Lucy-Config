// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// ============================================================
// 🎯 DSPNodeUpdateRequest —— DSP 节点更新请求
//     在 DSP 图处理中异步获取节点更新结果
// 💡 Internal_GetUpdateJobData 获取更新 Job 数据指针
// 💡 Internal_HasError 检查是否发生错误
// 💡 Internal_GetDSPNode 获取更新的节点句柄
// 💡 Internal_GetFence 获取完成信号 JobHandle
// 💡 Internal_Dispose 释放更新请求
// ⚡ 用于 DSPNode.Update 操作的异步结果追踪
// ============================================================
using System;
using Unity.Jobs;
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeHeader("Modules/DSPGraph/Public/DSPNodeUpdateRequest.bindings.h")]
    internal struct DSPNodeUpdateRequestHandleInternal
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void* Internal_GetUpdateJobData(ref Handle graph, ref Handle requestHandle);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern bool Internal_HasError(ref Handle graph, ref Handle requestHandle);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_GetDSPNode(ref Handle graph, ref Handle requestHandle, ref Handle node);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_GetFence(ref Handle graph, ref Handle requestHandle, ref JobHandle fence);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_Dispose(ref Handle graph, ref Handle requestHandle);
    }
}

