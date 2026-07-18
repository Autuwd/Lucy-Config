// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PhysicsComposer — 几何组合器，将散点组合为凸多边形
//
// 📌 作用：
//   PhysicsComposer 接收一组无序顶点，通过 Delaunay 三角剖分
//   计算并组合出最优的凸多边形集合，用于 PhysicsShape 的碰撞体。
//   是自动生成碰撞几何的核心工具。
//
// 🏗 架构位置：Composer → PolygonGeometry/ChainGeometry
//   组合器输出可直接用于创建 PhysicsShape 的几何体
//
// 💡 关键能力：
//   - CreatePolygonGeometry()：从散点生成凸多边形列表
//   - CreateConvexHulls()：从散点生成凸包列表
//   - CreateChainGeometry()：从散点生成链形几何
//   - GetGeometryIslands()：获取几何"孤岛"分组
//   - 支持多层（Layer）重叠组合
//   - 可设置最大多边形顶点数
//
// ⚡ 使用 Delaunay 三角剖分算法（可开关）
//   关闭时使用更简单的组合策略
// ==============================================================

using Unity.Collections;
using UnityEngine;
using UnityEngine.Bindings;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    [NativeHeader("Modules/PhysicsCore2D/Core/PhysicsComposer2D.h")]
    [StaticAccessor("PhysicsComposer2D", StaticAccessorType.DoubleColon)]
    static class PhysicsComposerScripting2D
    {
        [NativeMethod(Name = "Create", IsThreadSafe = true)] extern internal static PhysicsComposer PhysicsComposer_Create(Allocator allocator);
        [NativeMethod(Name = "Destroy", IsThreadSafe = true)] extern internal static bool PhysicsComposer_Destroy(PhysicsComposer composer);
        [NativeMethod(Name = "DestroyAll", IsThreadSafe = true)] extern internal static void PhysicsComposer_DestroyAll();
        [NativeMethod(Name = "IsValid", IsThreadSafe = true)] extern internal static bool Composer_IsValid(PhysicsComposer composer);
        [NativeMethod(Name = "GetComposers", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_GetComposers(Allocator allocator);
        [NativeMethod(Name = "AddLayer", IsThreadSafe = true)] extern internal static PhysicsComposer.LayerHandle PhysicsComposer_AddLayer(PhysicsComposer composer, PhysicsComposer.Layer layer);
        [NativeMethod(Name = "RemoveLayer", IsThreadSafe = true)] extern internal static void PhysicsComposer_RemoveLayer(PhysicsComposer composer, PhysicsComposer.LayerHandle layerHandle);
        [NativeMethod(Name = "ClearLayers", IsThreadSafe = true)] extern internal static void PhysicsComposer_ClearLayers(PhysicsComposer composer);
        [NativeMethod(Name = "GetLayerCount", IsThreadSafe = true)] extern internal static int PhysicsComposer_GetLayerCount(PhysicsComposer composer);
        [NativeMethod(Name = "GetRejectedGeometryCount", IsThreadSafe = true)] extern internal static int PhysicsComposer_GetRejectedGeometryCount(PhysicsComposer composer);
        [NativeMethod(Name = "GetLayerHandles", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_GetLayerHandles(PhysicsComposer composer);
        [NativeMethod(Name = "SetUseDelaunay", IsThreadSafe = true)] extern internal static void PhysicsComposer_SetUseDelaunay(PhysicsComposer composer, bool flag);
        [NativeMethod(Name = "GetUseDelaunay", IsThreadSafe = true)] extern internal static bool PhysicsComposer_GetUseDelaunay(PhysicsComposer composer);
        [NativeMethod(Name = "SetMaxPolygonVertices", IsThreadSafe = true)] extern internal static void PhysicsComposer_SetMaxPolygonVertices(PhysicsComposer composer, int maxPolygonVertices);
        [NativeMethod(Name = "GetMaxPolygonVertices", IsThreadSafe = true)] extern internal static int PhysicsComposer_GetMaxPolygonVertices(PhysicsComposer composer);
        [NativeMethod(Name = "CreatePolygonGeometry", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_CreatePolygonGeometry(PhysicsComposer composer, Vector2 vertexScale, float radius, Allocator allocator);
        [NativeMethod(Name = "CreateConvexHulls", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_CreateConvexHulls(PhysicsComposer composer, Vector2 vertexScale, Allocator allocator);
        [NativeMethod(Name = "CreateChainGeometry", IsThreadSafe = true)] extern internal static PhysicsBufferPair PhysicsComposer_CreateChainGeometry(PhysicsComposer composer, Vector2 vertexScale, Allocator allocator);
        [NativeMethod(Name = "GetGeometryIslands", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_GetGeometryIslands(PhysicsComposer composer, Allocator allocator);
    }
}
