// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// ============================================================
// 🎯 DSPCommandBlock —— DSP 命令缓冲区
//     批量提交对 DSP 图的修改操作（原子化、零 GC）
// 💡 支持的操作：
//     - CreateDSPNode / ReleaseDSPNode 节点生命周期
//     - SetFloat / AddFloatKey / SustainFloat 参数自动化
//     - Connect / Disconnect / DisconnectByHandle 连接管理
//     - SetAttenuation / AddAttenuationKey 衰减自动化
//     - AddInletPort / AddOutletPort 端口管理
//     - SetSampleProvider / InsertSampleProvider 采样供给器管理
//     - UpdateAudioJob / CreateUpdateRequest 更新请求
//     - Complete / Cancel 提交或取消命令块
// ⚡ 零 GC 命令缓冲区设计：所有操作暂存后一次提交
// 📌 通过 DSPGraph.CreateCommandBlock() 创建
// ============================================================
using System;
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeHeader("Modules/DSPGraph/Public/DSPSampleProvider.bindings.h")]
    [NativeHeader("Modules/DSPGraph/Public/DSPCommandBlock.bindings.h")]
    internal struct DSPCommandBlockInternal
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_CreateDSPNode(ref Handle graph, ref Handle block, ref Handle node, void* jobReflectionData, void* jobMemory, void* parameterDescriptionArray, int parameterCount, void* sampleProviderDescriptionArray, int sampleProviderCount);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_SetFloat(ref Handle graph, ref Handle block, ref Handle node, void* jobReflectionData, uint pIndex, float value, uint interpolationLength);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_AddFloatKey(ref Handle graph, ref Handle block, ref Handle node, void* jobReflectionData, uint pIndex, ulong dspClock, float value);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_SustainFloat(ref Handle graph, ref Handle block, ref Handle node, void* jobReflectionData, uint pIndex, ulong dspClock);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_UpdateAudioJob(ref Handle graph, ref Handle block, ref Handle node, void* updateJobMem, void* updateJobReflectionData, void* nodeReflectionData);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_CreateUpdateRequest(
            ref Handle graph, ref Handle block, ref Handle node, ref Handle request,
            object callback, void* updateJobMem, void* updateJobReflectionData, void* nodeReflectionData);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_ReleaseDSPNode(ref Handle graph, ref Handle block, ref Handle node);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_Connect(ref Handle graph, ref Handle block,
            ref Handle output, int outputPort, ref Handle input, int inputPort, ref Handle connection);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_Disconnect(ref Handle graph, ref Handle block,
            ref Handle output, int outputPort, ref Handle input, int inputPort);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_DisconnectByHandle(ref Handle graph, ref Handle block, ref Handle connection);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_SetAttenuation(ref Handle graph, ref Handle block, ref Handle connection, void* value, byte dimension, uint interpolationLength);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_AddAttenuationKey(ref Handle graph, ref Handle block, ref Handle connection, ulong dspClock, void* value, byte dimension);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_SustainAttenuation(ref Handle graph, ref Handle block, ref Handle connection, ulong dspClock);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_AddInletPort(ref Handle graph, ref Handle block, ref Handle node, int channelCount, int format);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_AddOutletPort(ref Handle graph, ref Handle block, ref Handle node, int channelCount, int format);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_SetSampleProvider(ref Handle graph, ref Handle block, ref Handle node, int item, int index, uint audioSampleProviderId, bool destroyOnRemove);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_InsertSampleProvider(ref Handle graph, ref Handle block, ref Handle node, int item, int index, uint audioSampleProviderId, bool destroyOnRemove);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_RemoveSampleProvider(ref Handle graph, ref Handle block, ref Handle node, int item, int index);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_Complete(ref Handle graph, ref Handle block);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_Cancel(ref Handle graph, ref Handle block);
    }
}

