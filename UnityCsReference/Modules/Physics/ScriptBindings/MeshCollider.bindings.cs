// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    /// <summary>
    ///   MeshCollider 的网格烹饪选项（位标志）。
    ///   控制如何预处理网格数据以用于物理碰撞检测。
    /// </summary>
    [Flags]
    public enum MeshColliderCookingOptions
    {
        None,                                         ///< 无特殊选项
        [Obsolete("自 Unity 2018.3 起不再使用", true)]
        InflateConvexMesh = 1 << 0,                    ///< 已废弃，凸包膨胀不再需要
        CookForFasterSimulation = 1 << 1,              ///< 优化网格以加快仿真速度（牺牲一些内存）
        EnableMeshCleaning = 1 << 2,                    ///< 启用网格清理（移除退化的三角形）
        WeldColocatedVertices = 1 << 3,                 ///< 焊接共位顶点（合并相同位置的顶点）
        UseFastMidphase = 1 << 4                        ///< 使用快速中间阶段算法
    }

    //=============================================================================
    // MeshCollider - 网格碰撞器
    //=============================================================================
    // 使用任意网格（Mesh）作为碰撞体积，可以创建任意形状的碰撞器。
    //
    // 两种模式：
    //   1. 凸包（Convex = true）：
    //      - 碰撞器是网格的凸包近似
    //      - 可以与其它碰撞器发生碰撞
    //      - 性能较好
    //      - 顶点数有限制（不超过 255 个顶点）
    //   2. 非凸包（Convex = false）：
    //      - 使用网格原始形状
    //      - 仅能与凸包碰撞器碰撞
    //      - 性能较差，但形状精确
    //      - 适用于静态环境（地形、建筑）
    //
    // 适用于复杂形状的物体，如地形、建筑、道具等。
    // 注意：网格在被用作碰撞器前需要"烹饪"（预计算加速结构）。
    //=============================================================================
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/MeshCollider.h")]
    [NativeHeader("Runtime/Graphics/Mesh/Mesh.h")]
    public partial class MeshCollider : Collider
    {
        /// <summary>用于碰撞检测的网格资源。</summary>
        extern public Mesh sharedMesh { get; set; }

        /// <summary>
        ///   是否为凸包。
        ///   true：使用网格的凸包近似（可以与其他碰撞器碰撞）。
        ///   false：使用网格原始形状（仅做静态碰撞，性能较差）。
        /// </summary>
        extern public bool convex { get; set; }

        /// <summary>网格烹饪选项，控制碰撞数据的预处理方式。</summary>
        extern public MeshColliderCookingOptions cookingOptions { get; set; }

        [NativeMethod("IsScaleBakingRequired")]
        [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
        extern internal bool IsScaleBakingRequired();
    }
}
