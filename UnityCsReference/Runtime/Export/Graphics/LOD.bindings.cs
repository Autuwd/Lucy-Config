// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 LOD 系统 — Level of Detail 多层次细节管理
//
// 📌 作用：
//   LOD（Level of Detail）系统根据物体在屏幕上的占比自动切换
//   不同精度的模型，是游戏性能优化的核心技术之一。
//
// 🏗 核心概念：
//
//   【LOD 结构体】
//   - screenRelativeTransitionHeight：切换阈值（占屏幕高度的比例 [0-1]）
//     例如 0.25 表示物体占屏幕 25% 高度时切换到下一级 LOD
//   - fadeTransitionWidth：淡入淡出过渡宽度
//   - renderers：该 LOD 级别使用的渲染器数组
//
//   【LODGroup 组件】
//   - 挂载在 GameObject 上，管理多个 LOD 级别
//   - localReferencePoint：计算距离的参考点（默认为包围盒中心）
//   - size：LOD 对象的大小（用于距离计算）
//   - lodCount：LOD 级别数量
//   - fadeMode：过渡模式（None / CrossFade / SpeedTree）
//   - animateCrossFading：是否动画化交叉淡入淡出
//   - ForceLOD：强制使用指定 LOD 级别（调试用）
//
//   【LODFadeMode — 过渡模式】
//   - None：直接切换，无过渡
//   - CrossFade：使用 dithering（抖动）实现 LOD 间平滑过渡
//   - SpeedTree：SpeedTree 专用过渡模式
//
// 💡 理解关键：
//   - LOD 切换基于物体在屏幕上的相对大小，而非与摄像机的距离
//   - QualitySettings.lodBias 可全局调整 LOD 偏移（越大越倾向高精度）
//   - QualitySettings.maximumLODLevel 可全局限制最低 LOD 级别
//   - RecalculateBounds 在 LOD 配置变更后必须调用
//
// ⚡ 性能提示：
//   - 合理配置 LOD 可大幅减少渲染开销
//   - CrossFade 模式下 enableLODCrossFade 需要在 QualitySettings 中开启
//   - lastLODBillboard 将最后一级 LOD 显示为 Billboard（最低性能开销）
//
// 📍 对应 C++ 头文件：Runtime/Graphics/LOD/LODGroup.h
// ==============================================================

using System;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // ==============================================================
    // LODFadeMode — LOD 过渡模式枚举
    // 🎯 控制 LOD 级别之间如何切换
    // ==============================================================
    public enum LODFadeMode
    {
        None = 0,
        CrossFade = 1,
        SpeedTree = 2
    }

    // ==============================================================
    // LOD — LOD 级别数据结构体
    // 🎯 描述单个 LOD 级别的渲染器列表和切换阈值
    // ==============================================================
    [UsedByNativeCode]
    public struct LOD
    {
        // Construct a LOD
        public LOD(float screenRelativeTransitionHeight, Renderer[] renderers)
        {
            this.screenRelativeTransitionHeight = screenRelativeTransitionHeight;
            this.fadeTransitionWidth = 0;
            this.renderers = renderers;
        }

        // The screen relative height to use for the transition [0-1]
        public float screenRelativeTransitionHeight;
        // Width of the transition (proportion to the current LOD's whole length).
        public float fadeTransitionWidth;
        // List of renderers for this LOD level
        public Renderer[] renderers;
    }

    // LODGroup lets you group multiple Renderers into LOD levels.
    [NativeHeader("Runtime/Graphics/LOD/LODGroup.h")]
    [NativeHeader("Runtime/Graphics/LOD/LODGroupManager.h")]
    [NativeHeader("Runtime/Graphics/LOD/LODUtility.h")]
    [StaticAccessor("GetLODGroupManager()", StaticAccessorType.Dot)]
    public class LODGroup : Component
    {
        // The local reference point against which the LOD distance is calculated.
        extern public Vector3 localReferencePoint { get; set; }

        // The size of LOD object in local space
        extern public float size { get; set; }

        // The number of LOD levels
        extern public int lodCount  {[NativeMethod("GetLODCount")] get; }

        public extern bool lastLODBillboard
        {
            [NativeMethod("GetLastLODIsBillboard")] get;
            [NativeMethod("SetLastLODIsBillboard")] set;
        }

        private extern int _globalIlluminationLOD
        {
            [NativeMethod("GetGlobalIlluminationLOD")]
            get;
            [NativeMethod("SetGlobalIlluminationLOD")]
            set;
        }

        internal int globalIlluminationLOD
        {
            get
            {
                return _globalIlluminationLOD;
            }
            set
            {
                if (value < -1 || value >= lodCount)
                {
                    var validLodRange = lodCount <= 1 ? "" : $"(0-{lodCount - 1})";
                    throw new ArgumentOutOfRangeException(nameof(globalIlluminationLOD), value, $"{nameof(globalIlluminationLOD)} must be -1 or point to a valid LOD {validLodRange} in the list of {lodCount} lod(s).");
                }
                _globalIlluminationLOD = value;
            }
        }

        // The fade mode
        extern public LODFadeMode fadeMode  { get; set; }

        // Is cross-fading animated?
        extern public bool animateCrossFading  { get; set; }

        // Enable / Disable the LODGroup - Disabling will turn off all renderers.
        extern public bool enabled  { get; set; }

        // Recalculate the bounding region for the LODGroup (Relatively slow, do not call often)
        [FreeFunction("UpdateLODGroupBoundingBox", HasExplicitThis = true)]
        extern public void RecalculateBounds();

        [FreeFunction("GetLODs_Binding", HasExplicitThis = true)]
        extern public LOD[] GetLODs();

        [Obsolete("Use SetLODs instead.")]
        public void SetLODS(LOD[] lods) { SetLODs(lods); }

        // Set the LODs for the LOD group. This will remove any existing LODs configured on the LODGroup
        [FreeFunction("SetLODs_Binding", HasExplicitThis = true)]
        extern public void SetLODs(LOD[] lods);

        // Force a LOD level on this LOD group
        //
        // @param index The LOD level to use. Passing index < 0 will return to standard LOD processing
        [FreeFunction("ForceLODLevel", HasExplicitThis = true)]
        extern public void ForceLOD(int index);

        [StaticAccessor("GetLODGroupManager()")]
        extern public static float crossFadeAnimationDuration { get; set; }

        extern internal Vector3 worldReferencePoint { get; }
    }
}
