// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 LightProbeGroup — 光照探针组组件
//
// 📌 作用：
//   在场景中放置一组"光照探针"（Light Probe），每个探针在烘焙时
//   采样该位置的间接光照数据（存储为球谐函数 SH 系数）。
//   烘焙后，这些探针为动态物体（角色/NPC）提供间接光照信息。
//
// 💡 光照探针 vs 光照贴图（Lightmap）：
//   - 光照贴图：烘焙到静态物体表面（仅静态物体可用）
//   - 光照探针：烘焙到空间中的点（动态物体通过探针获取间接光）
//   - 动态物体没有光照贴图，必须依赖光照探针
//
// 📌 工作流程：
//   1. 创建空 GameObject → 添加 LightProbeGroup 组件
//   2. 调整探针位置（黄色小球），覆盖需要间接光照的区域
//   3. 烘焙（Lighting → Generate Lighting）
//   4. 动态物体的 Renderer 勾选 "Use Light Probes"
//   5. 运行时自动选择最近 4 个探针进行四面体插值
//
// 📌 布局原则：
//   - 地面和天花板各放一排探针
//   - 光照边界处（窗户/门洞）密集放置
//   - 开阔区域稀疏放置
//   - 探针数量：小场景 50-200，大场景 500-2000
//
// ⚠️ 避免将探针放在封闭空间内部（如箱子内），
//    否则该区域间接光照会不准确。
// ==============================================================

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // LightProbeGroup — 光照探针组组件
    //
    // 🎯 继承链：LightProbeGroup → Behaviour → Component → Object
    //
    // 🔑 关键属性：
    //   - probePositions：探针世界坐标数组
    //   - dering：是否启用 Deringing（减少光照探针闪烁伪影）
    //
    // 💡 烘焙系统使用 Delaunay 四面体剖分连接探针，
    //    运行时根据物体所在四面体对 4 个顶点探针插值。
    // ==============================================================
    [NativeHeader("Runtime/Graphics/LightProbeGroup.h")]
    [UnityEngine.Scripting.RequiresEngineModule("Tetgen")]
    public sealed partial class LightProbeGroup : Behaviour
    {
        [NativeName("Positions")]
        public extern Vector3[] probePositions { get; set; }
        [NativeName("Dering")]
        public extern bool dering { get; set; }
    }
}
