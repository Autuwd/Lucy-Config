// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ====================================================================
// 🎯 图形作业过滤模式枚举 — 控制Graphics Jobs的多线程执行策略
// 💡 Off: 禁用图形作业  Native: 原生实现  Legacy: 旧版实现  Split: 拆分模式
// 💡 对应C++端GraphicsJobsFilterModeValues枚举，需保持同步
// ⚡ 用于平台设备过滤列表(D3D12/Vulkan/WebGPU)中指定首选模式
// ====================================================================

namespace UnityEngine
{
    // Must match the enumeration GraphicsJobsFilterModeValues in GfxDeviceTypes.h
    public enum GraphicsJobsFilterMode
    {
        Off = 0,
        Native = 1,
        Legacy = 2,
        Split = 3,
    }
}
