// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 DynamicGI — 动态全局光照
//
// 📌 作用：
//   控制运行时全局光照（GI）的行为，管理间接光照的更新。
//   用于调整预计算光照（Baked GI）在运行时的效果。
//
// 💡 核心 API：
//   - indirectScale：间接光强度缩放
//   - updateThreshold：更新阈值（变化小于此值不更新）
//   - materialUpdateTimeSlice：每帧材质更新时间分配
//   - UpdateEnvironment：强制更新环境光照
//   - SetEmissive：设置渲染器的自发光对 GI 的影响
//
// ⚡ 影响的是预烘焙 GI 在运行时的表现，不是实时 GI 计算。
// ==============================================================

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/GI/DynamicGI.h")]
    public sealed partial class DynamicGI
    {
        public static extern float indirectScale { get; set; }
        public static extern float updateThreshold { get; set; }
        public static extern int   materialUpdateTimeSlice { get; set; }
        public static extern void  SetEmissive(Renderer renderer, Color color);
        [NativeMethod(ThrowsException = true)] public static extern void  SetEnvironmentData([NotNull] float[] input);
        public static extern bool  synchronousMode { get; set; }
        public static extern bool  isConverged { get; }

        internal static extern int scheduledMaterialUpdatesCount { get; }
        internal static extern bool asyncMaterialUpdates { get; set; }
        public static extern void UpdateEnvironment();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("DynamicGI.UpdateMaterials(Renderer) is deprecated; instead, use extension method from RendererExtensions: 'renderer.UpdateGIMaterials()' (UnityUpgradable).", true)]
        public static void UpdateMaterials(Renderer renderer) {}
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("DynamicGI.UpdateMaterials(Terrain) is deprecated; instead, use extension method from TerrainExtensions: 'terrain.UpdateGIMaterials()' (UnityUpgradable).", true)]
        public static void UpdateMaterials(Object renderer) {}
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("DynamicGI.UpdateMaterials(Terrain, int, int, int, int) is deprecated; instead, use extension method from TerrainExtensions: 'terrain.UpdateGIMaterials(x, y, width, height)' (UnityUpgradable).", true)]
        public static void UpdateMaterials(Object renderer, int x, int y, int width, int height) {}
    }
}
