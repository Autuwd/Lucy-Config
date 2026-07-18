// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 SpeedTreeWind — SpeedTree 风系统绑定
//
// 📌 作用：
//   管理 SpeedTree 树木的风响应计算和 GPU 参数写入。
//   SpeedTree 是 Unity 的树木解决方案，支持程序化风动画。
//
// 🏗 风系统架构：
//
//   SpeedTreeWindManager（静态管理器）
//        ↓
//   UpdateWindAndWriteBufferWindParams()
//        ↓
//   读取风配置 → 计算每个树木的风响应
//        ↓
//   写入 GPU Buffer（SpeedTreeWindParamsBufferIterator）
//        ↓
//   GPU 上的树木着色器读取风参数 → 顶点动画
//
// 💡 两个版本兼容（ST8 / ST9）：
//   SpeedTreeWindParamIndex 枚举同时包含
//   SpeedTree 8（15个参数）和 SpeedTree 9（11个参数）的索引，
//   通过 WindParamsCount_v8 / WindParamsCount_v9 区分版本。
//
// 🏗 风参数层级（ST9）：
//   1. Shared（树干基础晃动）
//   2. Branch1（一级树枝）
//   3. Branch2（二级树枝）
//   4. Ripple（叶片 + 闪烁）
//
// 📦 数据流：
//   SpeedTreeWindAsset (ScriptableObject)
//   → SpeedTreeWindConfig9 (结构体)
//   → byte[] (二进制序列化)
//   → C++ 端反序列化
//   → GPU 实时风计算
// ==============================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.GPUDriven.Runtime")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor.Tests")]

namespace UnityEngine.Rendering
{
    // ==============================================================
    // 💨 SpeedTreeWindParamIndex — 风参数 GPU Buffer 索引
    //
    // 📌 作用：
    //   定义风速参数在 GPU 常量缓冲区中的布局索引。
    //   每个索引对应一个 vec4 参数。
    //
    // 🏗 版本兼容设计：
    //   同一枚举同时包含 ST8 和 ST9 的索引定义。
    //   - WindParamsCount_v8 = 15（ST8 用 15 个 vec4）
    //   - WindParamsCount_v9 = 11（ST9 用 11 个 vec4）
    //   - MaxWindParamsCount = 16（缓冲区预留空间）
    //
    // 💡 为什么需要两套索引？
    //   ST8 和 ST9 的风计算模型不同，参数组织方式也不同。
    //   老项目使用 ST8 格式，新项目推荐使用 ST9。
    // ==============================================================
    [NativeHeader("Modules/Terrain/Public/SpeedTreeWindManager.h")]
    internal enum SpeedTreeWindParamIndex
    {
        // ST8                     // ST9
        WindVector = 0,
        WindGlobal = 1,            TreeExtents_SharedHeightStart = 1,
        WindBranch = 2,            BranchStretchLimits = 2,
        WindBranchTwitch = 3,      Shared_NoisePosTurbulence_Independence = 3,
        WindBranchWhip = 4,        Shared_Bend_Oscillation_Turbulence_Flexibility = 4,
        WindBranchAnchor = 5,      Branch1_NoisePosTurbulence_Independence = 5,
        WindBranchAdherences = 6,  Branch1_Bend_Oscillation_Turbulence_Flexibility = 6,
        WindTurbulences = 7,       Branch2_NoisePosTurbulence_Independence = 7,
        WindLeaf1Ripple = 8,       Branch2_Bend_Oscillation_Turbulence_Flexibility = 8,
        WindLeaf1Tumble = 9,       Ripple_NoisePosTurbulence_Independence = 9,
        WindLeaf1Twitch = 10,      Ripple_Planar_Directional_Flexibility_Shimmer = 10,
        WindLeaf2Ripple = 11,
        WindLeaf2Tumble = 12,
        WindLeaf2Twitch = 13,
        WindFrondRipple = 14,

        WindParamsCount_v8 = 15,   WindParamsCount_v9 = 11,
        MaxWindParamsCount = 16
    }

    // ==============================================================
    // 💨 SpeedTreeWindManager — 风管理器（静态类）
    //
    // 📌 作用：
    //   引擎内部的静态类，负责驱动所有 SpeedTree 树木的风计算。
    //
    // 🔄 UpdateWindAndWriteBufferWindParams()：
    //   1. 传入需要更新的树木列表（renderersID）
    //   2. 计算每棵树在当前风场下的风响应
    //   3. 将结果写入 GPU Buffer（windParams）
    //   4. history 参数控制是否计算历史帧数据（用于 TAA 等）
    //
    // 💡 调用时机：
    //   每帧由引擎自动调用，开发者不需要手动管理。
    // ==============================================================
    [NativeHeader("Modules/Terrain/Public/SpeedTreeWindManager.h")]
    [StaticAccessor("GetSpeedTreeWindManager()", StaticAccessorType.Dot)]
    internal static class SpeedTreeWindManager
    {
        public static extern void UpdateWindAndWriteBufferWindParams(ReadOnlySpan<EntityId> renderersID, SpeedTreeWindParamsBufferIterator windParams, bool history);
    }

    // ==============================================================
    // 📦 SpeedTreeWindParamsBufferIterator — GPU Buffer 写入迭代器
    //
    // 📌 作用：
    //   用于在 C# 端遍历和写入 GPU 常量缓冲区中的风速参数。
    //   包含指向缓冲区的指针、每个 uint 参数的偏移数组、步长等。
    //
    // 💡 这是一个 unsafe 结构体，用于高性能的批量 GPU 数据写入。
    // ==============================================================
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Terrain/Public/SpeedTreeWind.h")]
    internal unsafe struct SpeedTreeWindParamsBufferIterator
    {
        public IntPtr bufferPtr;
        public fixed int uintParamOffsets[(int)SpeedTreeWindParamIndex.MaxWindParamsCount];
        public int uintStride;
        public int elementOffset;
        public int elementsCount;
    };
}
