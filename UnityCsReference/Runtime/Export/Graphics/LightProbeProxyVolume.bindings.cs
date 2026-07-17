// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 LightProbeProxyVolume — 光照探针代理体积（LPPV）
//
// 📌 作用：
//   LPPV 将离散的光照探针数据扩展为连续的 3D 纹理体积，
//   使得大型动态物体（如角色、载具）可以平滑地接收间接光照，
//   避免穿过光照探针边界时的突变跳变。
//
// 💡 LightProbe vs LightProbeProxyVolume：
//   - LightProbe：离散点采样，物体选择最近 4 个探针插值
//   - LPPV：连续体积采样，物体在 3D 纹理中三线性插值
//   - LPPV 解决了大物体跨越多个探针时的光照不连续问题
//
// 📌 工作原理：
//   1. 在 AABB 包围盒内生成规则网格的采样点
//   2. 每个采样点读取最近的 LightProbe 数据
//   3. 将数据存储为 3D 纹理（或 2D 纹理数组）
//   4. 渲染时，物体根据自身包围盒采样 3D 纹理
//
// 📌 包围盒模式（BoundingBoxMode）：
//   - AutomaticLocal：自动根据场景探针计算
//   - AutomaticWorld：世界空间自动包围盒
//   - Custom：手动设置 size 和 origin
//
// 📌 分辨率模式（ResolutionMode）：
//   - Automatic：根据包围盒大小自动计算
//   - Custom：手动设置 gridResolutionX/Y/Z
//
// ⚠️ 已废弃（Deprecated）：
//   随着 Built-In Render Pipeline 废弃，LPPV 被标记为过时。
//   SRP 中的替代方案是 Adaptive Probe Volume（APV）。
//
// 📍 对应 C++ 头文件：Runtime/Camera/LightProbeProxyVolume.h
// ==============================================================

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // LightProbeProxyVolume — 光照探针代理体积组件
    //
    // 🎯 继承链：LightProbeProxyVolume → Behaviour → Component → Object
    //
    // 🔑 关键属性：
    //   - boundsGlobal：全局包围盒（只读，由系统计算）
    //   - sizeCustom / originCustom：自定义包围盒的尺寸和原点
    //   - probeDensity：探针密度（每单位体积的探针数）
    //   - gridResolutionX/Y/Z：网格分辨率
    //   - boundingBoxMode：包围盒模式（自动/自定义）
    //   - resolutionMode：分辨率模式（自动/自定义）
    //   - refreshMode：刷新模式（On Load/Every Frame/Via Scripting）
    //   - qualityMode：质量模式（Low/Normal/High）
    //   - dataFormat：数据格式（Float/Half）
    //
    // 🔑 关键方法：
    //   - Update()：手动触发更新（refreshMode=ViaScripting 时使用）
    //   - isFeatureSupported：检查当前平台是否支持 LPPV
    //
    // 💡 使用流程：
    //   1. 确保场景中有 LightProbeGroup 并已烘焙
    //   2. 创建空 GameObject → 添加 LPPV 组件
    //   3. 调整包围盒覆盖需要连续光照的区域
    //   4. 大型动态物体的 Renderer 上启用 "Use Proxy Volume"
    // ==============================================================

    [NativeHeader("Runtime/Camera/LightProbeProxyVolume.h")]
    [Obsolete("The Light Probe Proxy Volume component is deprecated now that the Built-In Render Pipeline is deprecated. To use an alternative, refer to the documentation in the component help icon. #from(6000.5)", false)]
    [SRPReplacementComponentAttribute("UnityEngine.Rendering.ProbeVolume", "Adaptive Probe Volume")]
    public sealed partial class LightProbeProxyVolume : Behaviour
    {
        public static extern bool isFeatureSupported {[NativeName("IsFeatureSupported")] get; }

        [NativeName("GlobalAABB")]
        public extern Bounds boundsGlobal { get; }

        [NativeName("BoundingBoxSizeCustom")]
        public extern Vector3 sizeCustom { get; set; }

        [NativeName("BoundingBoxOriginCustom")]
        public extern Vector3 originCustom { get; set; }

        public extern float probeDensity { get; set; }

        public extern int gridResolutionX { get; set; }

        public extern int gridResolutionY { get; set; }

        public extern int gridResolutionZ { get; set; }

        public extern LightProbeProxyVolume.BoundingBoxMode boundingBoxMode { get; set; }

        public extern LightProbeProxyVolume.ResolutionMode resolutionMode { get; set; }

        public extern LightProbeProxyVolume.ProbePositionMode probePositionMode { get; set; }

        public extern LightProbeProxyVolume.RefreshMode refreshMode { get; set; }

        public extern LightProbeProxyVolume.QualityMode qualityMode { get; set; }

        public extern LightProbeProxyVolume.DataFormat dataFormat { get; set; }

        public void Update()
        {
            SetDirtyFlag(true);
        }

        private extern void SetDirtyFlag(bool flag);
    }
}
