// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 NavMeshBindingTypes — 构建/查询相关数据类型
//
// 📌 包含 NavMesh 构建管道和查询中使用的数据类型：
//
// 🔑 枚举：
//   - NavMeshBuildDebugFlags:    调试绘制标志（体素/区域/轮廓/多边形）
//   - NavMeshBuildSourceShape:   构建源形状（Mesh/Terrain/Box/Sphere/Capsule）
//   - NavMeshCollectGeometry:    几何来源（RenderMeshes/PhysicsColliders）
//
// 🔑 结构体：
//   - NavMeshBuildSource:  构建源数据（transform + shape + area + sourceObject）
//   - NavMeshBuildMarkup:   构建标记（覆盖特定物体的 area/ignore/generateLinks）
//
// 💡 NavMeshBuildSource.sourceObject：
//   引用场景中的 Mesh/Terrain/Collider 等，
//   构建时从这些物体提取几何数据。
//   component 字段引用具体的 Component 实例。
//
// ⚡ Markup 系统：
//   允许在不修改原始物体的前提下，
//   精细控制每个物体在 NavMesh 构建中的表现。
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.AI
{
    // ==============================================================
    // 🎯 NavMeshBuildDebugFlags — 构建过程调试可视化标志
    //
    // 📌 控制 NavMesh 构建调试的绘制内容：
    //   InputGeometry:       原始输入几何
    //   Voxels:              体素化结果
    //   Regions:             区域合并结果
    //   RawContours:         原始轮廓
    //   SimplifiedContours:  简化后轮廓
    //   PolygonMeshes:       多边形网格
    //   PolygonMeshesDetail: 多边形细节
    //
    // 💡 用于排查 NavMesh 构建质量问题，
    //   如缺口、重叠、不可达区域等。
    // ==============================================================
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    [Flags]
    public enum NavMeshBuildDebugFlags
    {
        None = 0,
        InputGeometry = 1 << 0,
        Voxels = 1 << 1,
        Regions = 1 << 2,
        RawContours = 1 << 3,
        SimplifiedContours = 1 << 4,
        PolygonMeshes = 1 << 5,
        PolygonMeshesDetail = 1 << 6,
        All = unchecked((int)(~(~0U << 7)))
    }

    // ==============================================================
    // 🎯 NavMeshBuildSourceShape — 构建源形状枚举
    //
    // 📌 NavMeshBuildSource 中的形状类型：
    //   Mesh:         自定义网格（MeshFilter）
    //   Terrain:      地形（Terrain Collider）
    //   Box:          盒体碰撞器
    //   Sphere:       球体碰撞器
    //   Capsule:      胶囊体碰撞器
    //   ModifierBox:  修改器盒子（仅影响区域标记，不贡献几何）
    //
    // 💡 ModifierBox 不参与几何构建，
    //   只用来覆盖其范围内物体的 area 标记。
    // ==============================================================
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    public enum NavMeshBuildSourceShape
    {
        Mesh = 0,
        Terrain = 1,
        Box = 2,
        Sphere = 3,
        Capsule = 4,
        ModifierBox = 5
    }

    // ==============================================================
    // 🎯 NavMeshCollectGeometry — 几何来源枚举
    //
    // 📌 控制 NavMeshBuilder.CollectSources 从何处收集几何：
    //   RenderMeshes:     从 MeshFilter 的共享网格收集 ✅ 更准确
    //   PhysicsColliders: 从 Collider 收集 ✅ 与物理碰撞一致
    //
    // 💡 RenderMeshes 通常更精确（使用实际渲染网格），
    //   PhysicsColliders 保证寻路与物理碰撞一致（推荐）。
    // ==============================================================
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    public enum NavMeshCollectGeometry
    {
        RenderMeshes = 0,
        PhysicsColliders = 1
    }

    // ==============================================================
    // 🎯 NavMeshBuildSource — 构建源数据单元
    //
    // 📌 描述 NavMesh 构建中的一个几何输入：
    //   - transform:  物体变换矩阵（位置/旋转/缩放）
    //   - size:       形状尺寸
    //   - shape:      形状类型（Mesh/Terrain/Box...）
    //   - area:       所属区域编号
    //   - sourceObject: 引用的场景物体
    //   - component:    引用的 Component（Collider/Renderer 等）
    //   - generateLinks: 是否从此源生成自动链接
    //
    // 💡 NavMeshBuilder.CollectSources() 的输出就是
    //   List<NavMeshBuildSource>，传递给 BuildNavMeshData()。
    // ==============================================================
    // Struct containing source geometry data and annotation for runtime navmesh building
    [UsedByNativeCode]
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    public struct NavMeshBuildSource
    {
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
        public Vector3 size { get { return m_Size; } set { m_Size = value; } }
        public NavMeshBuildSourceShape shape { get { return m_Shape; } set { m_Shape = value; } }
        public int area { get { return m_Area; } set { m_Area = value; } }
        public bool generateLinks { get { return m_GenerateLinks != 0; } set { m_GenerateLinks = value ? 1 : 0; } }
        public Object sourceObject { get { return InternalGetObject(m_EntityId); } set { m_EntityId = value != null ? value.GetEntityId() : EntityId.None; } }
        public Component component { get { return InternalGetComponent(m_ComponentID); } set { m_ComponentID = value != null ? value.GetEntityId() : EntityId.None; } }

        Matrix4x4 m_Transform;
        Vector3 m_Size;
        NavMeshBuildSourceShape m_Shape;
        int m_Area;
        EntityId m_EntityId;
        EntityId m_ComponentID;
        int m_GenerateLinks;

        [StaticAccessor("NavMeshBuildSource", StaticAccessorType.DoubleColon)]
        static extern Component InternalGetComponent(EntityId instanceID);

        [StaticAccessor("NavMeshBuildSource", StaticAccessorType.DoubleColon)]
        static extern Object InternalGetObject(EntityId instanceID);
    }

    // ==============================================================
    // 🎯 NavMeshBuildMarkup — 构建覆盖标记
    //
    // 📌 在不修改原始物体的前提下，覆盖构建参数：
    //   - overrideArea:   是否覆盖区域标记
    //   - area:           覆盖后的区域编号
    //   - ignoreFromBuild: 是否排除此物体
    //   - overrideGenerateLinks: 是否覆盖链接生成
    //   - generateLinks:  是否为此物体生成链接
    //   - applyToChildren: 是否应用到子物体
    //   - root:           标记作用的根 Transform
    //
    // 💡 通过 markups 数组传递给 CollectSources()，
    //   可以精细控制每个物体的 NavMesh 构建行为。
    // ==============================================================
    // Struct containing source geometry data and annotation for runtime navmesh building
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    public struct NavMeshBuildMarkup
    {
        public bool overrideArea { get { return m_OverrideArea != 0; } set { m_OverrideArea = value ? 1 : 0; } }
        public int area { get { return m_Area; } set { m_Area = value; } }
        public bool overrideIgnore { get { return m_InheritIgnoreFromBuild == 0; } set { m_InheritIgnoreFromBuild = value ? 0: 1; } }
        public bool ignoreFromBuild { get { return m_IgnoreFromBuild != 0; } set { m_IgnoreFromBuild = value ? 1 : 0; } }
        public bool overrideGenerateLinks { get { return m_OverrideGenerateLinks != 0; } set { m_OverrideGenerateLinks = value ? 1 : 0; } }
        public bool generateLinks { get { return m_GenerateLinks != 0; } set { m_GenerateLinks = value ? 1 : 0; } }
        public bool applyToChildren { get { return m_IgnoreChildren == 0; } set { m_IgnoreChildren = value ? 0 : 1; } }
        public Transform root { get { return InternalGetRootGO(m_EntityId); } set { m_EntityId = value != null ? value.GetEntityId() : EntityId.None; } }

        int m_OverrideArea;
        int m_Area;
        int m_InheritIgnoreFromBuild; // backing field is reversed for the default value to align with the legacy default behaviour
        int m_IgnoreFromBuild;
        int m_OverrideGenerateLinks;
        int m_GenerateLinks;
        EntityId m_EntityId;
        int m_IgnoreChildren; // backing field is reversed for the default value to align with the legacy default behaviour

        [StaticAccessor("NavMeshBuildMarkup", StaticAccessorType.DoubleColon)]
        static extern Transform InternalGetRootGO(EntityId instanceID);
    }
}
