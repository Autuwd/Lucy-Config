// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 NavMesh — Unity 导航网格全局系统
//
// 📌 作用：
//   提供全局寻路查询 API（SamplePosition、Raycast、CalculatePath）、
//   NavMeshData 的运行时加载/卸载、Area Cost 管理、以及 Link 管理。
//   这是访问所有导航功能的入口点。
//
// 🔑 核心类型：
//   - NavMeshHit:          采样/射线检测结果（位置、法线、距离、面积掩码）
//   - NavMeshTriangulation: 网格三角化数据（顶点、索引、面积）
//   - NavMeshData:          运行时 NavMesh 数据容器（可动态加载/卸载）
//   - NavMeshLinkData:      网格外链接定义（起点、终点、双向、代价）
//   - NavMeshQueryFilter:   查询过滤器（areaMask、agentTypeID、areaCost）
//
// 💡 NavMesh Baking（烘焙）:
//   将场景中静态物体的 RenderMesh 或 Collider 转化为 NavMesh 数据，
//   经过体素化 → 区域生成 → 轮廓提取 → 多边形网格四个阶段，
//   最终生成可供 A* 寻路算法使用的多边形网格（Polygon Mesh）。
//   烘焙参数（agentRadius/Height/Slope/Climb）直接影响导航质量和性能。
//
// ⚡ Area Cost 系统：
//   每个区域类型（地面/水/楼梯等）可设置不同的通行代价，
//   寻路时自动选择总代价最低的路径，而非最短距离。
// ==============================================================

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // ==============================================================
    // 🎯 NavMeshHit — 导航查询结果
    //
    // 📌 包含 NavMesh 查询（SamplePosition/Raycast/FindClosestEdge）的结果：
    //   - position:  命中点世界坐标
    //   - normal:    命中点法线方向
    //   - distance:  到命中点的距离
    //   - mask:      命中点的 area mask
    //   - hit:       是否命中（bool 标志位）
    //
    // 💡 所有查询方法都通过 out NavMeshHit 返回结果，
    //    先检查 hit 字段再使用 position/normal 等数据。
    // ==============================================================
    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    [MovedFrom("UnityEngine")]
    public struct NavMeshHit
    {
        Vector3 m_Position;
        Vector3 m_Normal;
        float m_Distance;
        int m_Mask;
        int m_Hit;

        // Position of hit.
        public Vector3 position { get => m_Position; set => m_Position = value; }

        // Normal at the point of hit.
        public Vector3 normal { get => m_Normal; set => m_Normal = value; }

        // Distance to the point of hit.
        public float distance { get => m_Distance; set => m_Distance = value; }

        // Mask specifying NavMesh area index at point of hit.
        public int mask { get => m_Mask; set => m_Mask = value; }

        // Flag set when hit.
        public bool hit { get => m_Hit != 0; set => m_Hit = value ? 1 : 0; }
    }

    // ==============================================================
    // 🎯 NavMeshTriangulation — 导航网格三角化数据
    //
    // 📌 NavMesh.CalculateTriangulation() 的返回结果：
    //   - vertices: 多边形顶点数组
    //   - indices:  三角形索引数组（每 3 个构成一个三角形）
    //   - areas:    每个三角形的区域编号
    //
    // 💡 用于可视化调试或自定义 NavMesh 处理。
    //    layers 已废弃，改用 areas。
    // ==============================================================
    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    [UsedByNativeCode]
    [MovedFrom("UnityEngine")]
    public struct NavMeshTriangulation
    {
        public Vector3[] vertices;
        public int[] indices;
        public int[] areas;

        [Obsolete("Use areas instead.")]
        public int[] layers => areas;
    }

    // ==============================================================
    // 🎯 NavMeshData — 运行时 NavMesh 数据容器
    //
    // 📌 作为运行时动态 NavMesh 数据的载体：
    //   - 通过 NavMeshBuilder.BuildNavMeshData() 构建
    //   - 通过 NavMesh.AddNavMeshData() 动态加载到寻路系统
    //   - 支持位置（position）和旋转（rotation）的偏移设置
    //
    // 💡 使用场景：
    //   动态生成的 NavMesh（破坏的桥梁、打开的门）、
    //   分块加载预烘焙数据（大型开放世界）。
    // ==============================================================
    [NativeHeader("Modules/AI/NavMesh/NavMesh.bindings.h")]
    public sealed class NavMeshData : Object
    {
        public NavMeshData()
        {
            Internal_Create(this, 0);
        }

        public NavMeshData(int agentTypeID)
        {
            Internal_Create(this, agentTypeID);
        }

        [StaticAccessor("NavMeshDataBindings", StaticAccessorType.DoubleColon)]
        static extern void Internal_Create([Writable] NavMeshData mono, int agentTypeID);

        public extern Bounds sourceBounds { get; }
        public extern Vector3 position { get; set; }
        public extern Quaternion rotation { get; set; }
        internal extern bool hasHeightMeshData { [NativeMethod("HasHeightMeshData")] get; }

        internal extern NavMeshBuildSettings buildSettings { get; }
    }

    public struct NavMeshDataInstance
    {
        public bool valid => id != 0 && NavMesh.IsValidNavMeshDataHandle(id);
        internal int id { get; set; }

        public void Remove()
        {
            NavMesh.RemoveNavMeshDataInternal(id);
        }

        public Object owner
        {
            get => NavMesh.InternalGetOwner(id);
            set
            {
                var ownerID = value != null ? value.GetEntityId() : EntityId.None;
                if (!NavMesh.InternalSetOwner(id, ownerID))
                    Debug.LogError("Cannot set 'owner' on an invalid NavMeshDataInstance");
            }
        }

        internal void FlagAsInSelectionHierarchy()
        {
            if (valid)
                FlagSurfaceAsInSelectionHierarchy(id);
        }

        [StaticAccessor("GetNavMeshManager()", StaticAccessorType.Dot)]
        static extern void FlagSurfaceAsInSelectionHierarchy(int id);
    }

    // ==============================================================
    // 🎯 NavMeshLinkData — 网格外链接定义
    //
    // 📌 定义两个 NavMesh 之间的连接路径（跳跃、梯子、传送门）：
    //   - startPosition/endPosition: 链接两端世界坐标
    //   - bidirectional:             是否双向通行
    //   - costModifier:              额外代价修正（引导/惩罚路径选择）
    //   - width:                     链接宽度
    //   - area/agentTypeID:          区域和 Agent 类型
    //
    // 💡 双向 vs 单向：
    //   bidirectional=true 两端都可通行（适合平地跳跃）
    //   bidirectional=false 仅从 start 到 end（适合单向传送/掉落）
    // ==============================================================
    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    public struct NavMeshLinkData
    {
        Vector3 m_StartPosition;
        Vector3 m_EndPosition;
        float m_CostModifier;
        int m_Bidirectional;
        float m_Width;
        int m_Area;
        int m_AgentTypeID;

        public Vector3 startPosition { get => m_StartPosition; set => m_StartPosition = value; }
        public Vector3 endPosition { get => m_EndPosition; set => m_EndPosition = value; }
        public float costModifier { get => m_CostModifier; set => m_CostModifier = value; }
        public bool bidirectional { get => m_Bidirectional != 0; set => m_Bidirectional = value ? 1 : 0; }
        public float width { get => m_Width; set => m_Width = value; }
        public int area { get => m_Area; set => m_Area = value; }
        public int agentTypeID { get => m_AgentTypeID; set => m_AgentTypeID = value; }
    }

    public partial struct NavMeshLinkInstance
    {
        internal int id { get; set; }
    }

    // ==============================================================
    // 🎯 NavMeshQueryFilter — 查询过滤器
    //
    // 📌 精细控制 NavMesh 查询行为：
    //   - areaMask:     允许通行的区域位掩码（按位标记）
    //   - agentTypeID:  指定 Agent 类型
    //   - areaCost:     每个区域的额外代价（32 区域，默认 1.0）
    //
    // 💡 通过 SetAreaCost() 可引导寻路避开高代价区域，
    //   而不需要完全禁止通行（降低 areaMask 是一刀切）。
    //   costs 数组为 null 时所有区域代价为 1.0。
    // ==============================================================
    public struct NavMeshQueryFilter
    {
        const int k_AreaCostElementCount = 32;

        internal float[] costs { get; private set; }

        public int areaMask { get; set; }
        public int agentTypeID { get; set; }

        public float GetAreaCost(int areaIndex)
        {
            if (costs == null)
            {
                if (areaIndex < 0 || areaIndex >= k_AreaCostElementCount)
                {
                    var msg = string.Format("The valid range is [0:{0}]", k_AreaCostElementCount - 1);
                    throw new IndexOutOfRangeException(msg);
                }
                return 1.0f;
            }
            return costs[areaIndex];
        }

        public void SetAreaCost(int areaIndex, float cost)
        {
            if (costs == null)
            {
                costs = new float[k_AreaCostElementCount];
                for (int j = 0; j < k_AreaCostElementCount; ++j)
                    costs[j] = 1.0f;
            }
            costs[areaIndex] = cost;
        }
    }

    // ==============================================================
    // 🎯 NavMesh — 静态导航 API 入口
    //
    // 📌 主要功能分类：
    //
    // 🔍 查询类：
    //   - SamplePosition:  在世界中采样最近 NavMesh 位置
    //   - Raycast:         沿 NavMesh 表面射线检测
    //   - CalculatePath:   计算两点间路径（A* 寻路）
    //   - FindClosestEdge: 查找最近 NavMesh 边缘
    //
    // ⚙ 配置类：
    //   - SetAreaCost/GetAreaCost:  区域通行代价调整
    //   - avoidancePredictionTime:  避让预测时间（影响避让质量）
    //   - pathfindingIterationsPerFrame:  每帧寻路迭代数（性能控制）
    //
    // 🔗 Link 管理：
    //   - AddLink/RemoveLink:     动态增删 NavMeshLink
    //   - IsLinkActive/SetLinkActive: 控制链接激活状态
    //
    // 🗺 Data 管理：
    //   - AddNavMeshData/RemoveNavMeshData: 运行时加载/卸载数据
    //   - CreateSettings/RemoveSettings: Agent 类型设置管理
    //
    // ⚡ 与 NavMeshQueryFilter 配合：
    //   过滤器版本的查询（SamplePosition/Raycast/CalculatePath）
    //   支持按 agentTypeID + areaMask + areaCost 精细控制。
    // ==============================================================
    [NativeHeader("Modules/AI/NavMeshManager.h")]
    [NativeHeader("Modules/AI/NavMesh/NavMesh.bindings.h")]
    [StaticAccessor("NavMeshBindings", StaticAccessorType.DoubleColon)]
    [MovedFrom("UnityEngine")]
    public static partial class NavMesh
    {
        public const int AllAreas = ~0;

        public delegate void OnNavMeshPreUpdate();
        [AutoStaticsCleanupOnCodeReload] // holds user-registered pre-update callbacks
        public static OnNavMeshPreUpdate onPreUpdate;

        [RequiredByNativeCode]
        static void ClearPreUpdateListeners()
        {
            onPreUpdate = null;
        }

        [RequiredByNativeCode]
        static void Internal_CallPreUpdateListeners()
        {
            if (onPreUpdate != null)
                onPreUpdate();
        }

        // Trace a ray between two points on the NavMesh.
        public static extern bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int areaMask);

        // Calculate a path between two points and store the resulting path.
        public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathInternal(sourcePosition, targetPosition, areaMask, path);
        }

        static extern bool CalculatePathInternal(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path);

        // Locate the closest NavMesh edge from a point on the NavMesh.
        public static extern bool FindClosestEdge(Vector3 sourcePosition, out NavMeshHit hit, int areaMask);

        // Sample the NavMesh closest to the point specified.
        public static extern bool SamplePosition(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int areaMask);

        [Obsolete("Use SetAreaCost instead.")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("SetAreaCost")]
        public static extern void SetLayerCost(int layer, float cost);

        [Obsolete("Use GetAreaCost instead.")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaCost")]
        public static extern float GetLayerCost(int layer);

        [Obsolete("Use GetAreaFromName instead.")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaFromName")]
        public static extern int GetNavMeshLayerFromName(string layerName);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("SetAreaCost")]
        public static extern void SetAreaCost(int areaIndex, float cost);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaCost")]
        public static extern float GetAreaCost(int areaIndex);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaFromName")]
        public static extern int GetAreaFromName(string areaName);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaNames")]
        public static extern string[] GetAreaNames();

        public static extern NavMeshTriangulation CalculateTriangulation();

        //*undocumented* DEPRECATED
        [Obsolete("use NavMesh.CalculateTriangulation() instead.")]
        public static void Triangulate(out Vector3[] vertices, out int[] indices)
        {
            NavMeshTriangulation results = CalculateTriangulation();
            vertices = results.vertices;
            indices = results.indices;
        }

        [Obsolete("AddOffMeshLinks has no effect and is deprecated.")]
        public static void AddOffMeshLinks() {}

        [Obsolete("RestoreNavMesh has no effect and is deprecated.")]
        public static void RestoreNavMesh() {}

        [StaticAccessor("GetNavMeshManager()")]
        public static extern float avoidancePredictionTime { get; set; }

        [StaticAccessor("GetNavMeshManager()")]
        public static extern int pathfindingIterationsPerFrame { get; set; }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NavMeshDataInstance AddNavMeshData(NavMeshData navMeshData)
        {
            if (navMeshData == null) throw new ArgumentNullException(nameof(navMeshData));

            var handle = new NavMeshDataInstance();
            handle.id = AddNavMeshDataInternal(navMeshData);
            return handle;
        }

        public static NavMeshDataInstance AddNavMeshData(NavMeshData navMeshData, Vector3 position, Quaternion rotation)
        {
            if (navMeshData == null) throw new ArgumentNullException(nameof(navMeshData));

            var handle = new NavMeshDataInstance();
            handle.id = AddNavMeshDataTransformedInternal(navMeshData, position, rotation);
            return handle;
        }

        public static void RemoveNavMeshData(NavMeshDataInstance handle)
        {
            RemoveNavMeshDataInternal(handle.id);
        }

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("IsValidSurfaceID")]
        internal static extern bool IsValidNavMeshDataHandle(int handle);

        [StaticAccessor("GetNavMeshManager()")]
        internal static extern bool IsValidLinkHandle(int handle);

        internal static extern Object InternalGetOwner(int dataID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("SetSurfaceUserID")]
        internal static extern bool InternalSetOwner(int dataID, UnityEngine.EntityId ownerID);

        internal static extern Object InternalGetLinkOwner(int linkID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("SetLinkUserID")]
        internal static extern bool InternalSetLinkOwner(int linkID, UnityEngine.EntityId ownerID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("LoadData")]
        internal static extern int AddNavMeshDataInternal(NavMeshData navMeshData);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("LoadData")]
        internal static extern int AddNavMeshDataTransformedInternal(NavMeshData navMeshData, Vector3 position, Quaternion rotation);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("UnloadData")]
        internal static extern void RemoveNavMeshDataInternal(int handle);

        public static NavMeshLinkInstance AddLink(NavMeshLinkData link)
        {
            var handle = new NavMeshLinkInstance();
            handle.id = AddLinkInternal(link, Vector3.zero, Quaternion.identity);
            return handle;
        }

        public static NavMeshLinkInstance AddLink(NavMeshLinkData link, Vector3 position, Quaternion rotation)
        {
            var handle = new NavMeshLinkInstance();
            handle.id = AddLinkInternal(link, position, rotation);
            return handle;
        }

        public static void RemoveLink(NavMeshLinkInstance handle)
        {
            RemoveLinkInternal(handle.id);
        }

        public static bool IsLinkActive(NavMeshLinkInstance handle)
        {
            return IsOffMeshConnectionActive(handle.id);
        }

        public static void SetLinkActive(NavMeshLinkInstance handle, bool value)
        {
            SetOffMeshConnectionActive(handle.id, value);
        }

        public static bool IsLinkOccupied(NavMeshLinkInstance handle)
        {
            return IsOffMeshConnectionOccupied(handle.id);
        }

        public static bool IsLinkValid(NavMeshLinkInstance handle)
        {
            return IsValidLinkHandle(handle.id);
        }

        public static Object GetLinkOwner(NavMeshLinkInstance handle)
        {
            return InternalGetLinkOwner(handle.id);
        }

        public static void SetLinkOwner(NavMeshLinkInstance handle, Object owner)
        {
            var ownerID = owner != null ? owner.GetEntityId() : EntityId.None;
            if (!InternalSetLinkOwner(handle.id, ownerID))
                Debug.LogError("Cannot set 'owner' on an invalid NavMeshLinkInstance");
        }

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("AddLink")]
        internal static extern int AddLinkInternal(NavMeshLinkData link, Vector3 position, Quaternion rotation);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("RemoveLink")]
        internal static extern void RemoveLinkInternal(int handle);

        [StaticAccessor("GetNavMeshManager()")]
        internal static extern bool IsOffMeshConnectionOccupied(int handle);

        [StaticAccessor("GetNavMeshManager()")]
        internal static extern bool IsOffMeshConnectionActive(int linkHandle);

        [StaticAccessor("GetNavMeshManager()")]
        internal static extern void SetOffMeshConnectionActive(int linkHandle, bool activated);

        public static bool SamplePosition(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, NavMeshQueryFilter filter)
        {
            return SamplePositionFilter(sourcePosition, out hit, maxDistance, filter.agentTypeID, filter.areaMask);
        }

        // a CUSTOM "SamplePosition" exists elsewhere. We need to pick unique name here to compile generated code in batch-builds
        static extern bool SamplePositionFilter(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int type, int mask);

        public static bool FindClosestEdge(Vector3 sourcePosition, out NavMeshHit hit, NavMeshQueryFilter filter)
        {
            return FindClosestEdgeFilter(sourcePosition, out hit, filter.agentTypeID, filter.areaMask);
        }

        // a CUSTOM "FindClosestEdge" exists elsewhere. We need to pick unique name here to compile generated code in batch-builds
        static extern bool FindClosestEdgeFilter(Vector3 sourcePosition, out NavMeshHit hit, int type, int mask);

        public static bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, NavMeshQueryFilter filter)
        {
            return RaycastFilter(sourcePosition, targetPosition, out hit, filter.agentTypeID, filter.areaMask);
        }

        // a CUSTOM "Raycast" exists elsewhere. We need to pick unique name here to compile generated code in batch-builds
        static extern bool RaycastFilter(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int type, int mask);

        public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, NavMeshQueryFilter filter, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathFilterInternal(sourcePosition, targetPosition, path, filter.agentTypeID, filter.areaMask, filter.costs);
        }

        static extern bool CalculatePathFilterInternal(Vector3 sourcePosition, Vector3 targetPosition, NavMeshPath path, int type, int mask, float[] costs);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        public static extern NavMeshBuildSettings CreateSettings();

        //[StaticAccessor("GetNavMeshProjectSettings()")]
        //public static extern void UpdateSettings(NavMeshBuildSettings buildSettings);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        public static extern void RemoveSettings(int agentTypeID);

        public static extern NavMeshBuildSettings GetSettingsByID(int agentTypeID);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        public static extern int GetSettingsCount();

        public static extern NavMeshBuildSettings GetSettingsByIndex(int index);

        public static extern string GetSettingsNameFromID(int agentTypeID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("CleanupAfterCarving")]
        public static extern void RemoveAllNavMeshData();
    }
}
