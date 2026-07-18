// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GIDebugVisualisation — GI 调试可视化
//
// 📌 作用：
//   提供全局光照系统的调试可视化工具，用于编辑器模式下
//   检查光照贴图、光照探测（Light Probe）等数据。
//
// 💡 核心类型：
//   - GITextureType：可显示的 GI 纹理类型
//     Charting(图集)/Albedo(反照率)/Emissive(自发光)/Irradiance(辐照度)
//     Directionality(方向性)/Baked(烘焙)/BakedDirectional(烘焙方向光)
//   - GIVisualizationType：可视化模式
//     Albedo(反照率)/Emissive(自发光)/Irradiance(辐照度)/Directionality(方向性)
//
// 📌 仅有编辑器可用，运行时不可用。
// ==============================================================

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngineInternal
{
    // Texture type that can be shown when GIDebugVisualisation is enabled.
    public enum GITextureType
    {
        Charting,
        Albedo,
        Emissive,
        Irradiance,
        Directionality,
        Baked,
        BakedDirectional,
        InputWorkspace,
        BakedShadowMask,
        BakedAlbedo,
        BakedEmissive,
        BakedCharting,
        BakedTexelValidity,
        BakedUVOverlap,
        BakedLightmapCulling
    }

    [NativeHeader("Runtime/Export/GI/GIDebugVisualisation.bindings.h")]
    public static partial class GIDebugVisualisation
    {
        [FreeFunction]
        public extern static void ResetRuntimeInputTextures();

        [FreeFunction]
        public extern static void PlayCycleMode();

        [FreeFunction]
        public extern static void PauseCycleMode();

        [FreeFunction]
        public extern static void StopCycleMode();

        // Skip forwards or backwards in the systems list.
        [FreeFunction]
        public extern static void CycleSkipSystems(int skip);

        // Skip forwards or backwards in the instance list.
        [FreeFunction]
        public extern static void CycleSkipInstances(int skip);

        public static extern bool cycleMode {[FreeFunction] get; }

        public static extern bool pauseCycleMode {[FreeFunction] get; }

        public static extern GITextureType texType {[FreeFunction] get; [FreeFunction] set; }
    }
}
