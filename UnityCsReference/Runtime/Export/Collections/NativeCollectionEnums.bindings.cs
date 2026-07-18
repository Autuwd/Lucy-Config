// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Collections
{
    //=============================================================================
    // 🎯 NativeCollectionEnums —— 原生集合核心枚举
    //
    // 设计说明:
    //   定义了 Unity 原生集合（NativeContainer）系统的三大核心枚举:
    //
    //   📌 Allocator（分配器类型）:
    //     Invalid = 0     — 默认值，确保 new NativeArray\<T\>() 产生无效分配
    //     None            — 无分配
    //     Temp            — 临时分配（单帧内使用，自动回收）
    //     TempJob         — 作业级分配（4 帧内自动回收）
    //     Persistent      — 持久分配（手动 Dispose）
    //     AudioKernel     — 音频内核专用
    //     Domain          — Domain 级别
    //     FirstUserIndex  — 用户自定义分配器起始索引
    //
    //   📌 NativeLeakDetectionMode（泄漏检测模式）:
    //     控制对未 Dispose 的 NativeContainer 的检测行为
    //
    //   📌 LeakCategory（泄漏分类）:
    //     用于内存泄漏追踪，按子系统区分（内部使用）
    //
    // 💡 枚举值必须与 C++ 侧的对应头文件严格同步:
    //     Runtime/Export/Collections/NativeCollectionAllocator.h
    //     Runtime/Export/Collections/NativeCollectionLeakDetectionMode.h
    //     Runtime/Export/Collections/NativeCollectionLeakCategory.h
    //=============================================================================

    [UsedByNativeCode]
    public enum Allocator
    {
        // NOTE: The items must be kept in sync with Runtime/Export/Collections/NativeCollectionAllocator.h

        Invalid = 0,
        // NOTE: this is important to let Invalid = 0 so that new NativeArray<xxx>() will lead to an invalid allocation by default.

        None = 1,
        Temp = 2,
        TempJob = 3,
        Persistent = 4,
        AudioKernel = 5,
        Domain = 6,
        FirstUserIndex = 64,
    }

    [UsedByNativeCode]
    public enum NativeLeakDetectionMode
    {
        // NOTE: Any changes to this enum must be kept in sync with Runtime\Export\Collections\NativeCollectionLeakDetectionMode.h
        Disabled = 1,
        Enabled = 2,
        EnabledWithStackTrace = 3
    }

    [UsedByNativeCode]
    [VisibleToOtherModules("UnityEngine.AIModule")]
    internal enum LeakCategory
    {
        // NOTE: Any changes to this enum must be kept in sync with Runtime\Export\Collections\NativeCollectionLeakCategory.h
        // and the strings in Runtime\Allocator\LeakDetection.cpp
        Invalid = 0,
        Malloc = 1,
        TempJob = 2,
        Persistent = 3,
        LightProbesQuery = 4,
        NativeTest = 5,
        MeshDataArray = 6,
        TransformAccessArray = 7,
        NavMeshQuery = 8,
        NavQueryBuffer = 9,
        UIElementsStyleData = 10
    }
}
