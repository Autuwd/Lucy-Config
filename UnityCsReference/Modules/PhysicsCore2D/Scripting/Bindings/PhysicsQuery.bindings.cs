// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PhysicsQuery — 碰撞检测与查询系统核心
//
// 📌 作用：
//   PhysicsQuery 提供所有碰撞检测的基础算法，
//   包括形状间碰撞检测、射线检测、形状投射、距离计算和 TOI。
//   被 PhysicsWorld 和 PhysicsSpace 的查询 API 调用。
//
// 🏗 架构位置：底层碰撞检测引擎
//   PhysicsWorld.Overlap/Cast → PhysicsQuery.{ShapeAndShape, CastShapes, ...}
//
// 💡 查询类型：
//   - ShapeAndShape：两个形状之间的接触检测（生成 ContactManifold）
//   - CastShapes：形状投射（检测运动路径上的碰撞）
//   - ShapeDistance：形状间最短距离
//   - ShapeTimeOfImpact：形状连续碰撞时间（TOI）
//   - SegmentDistance：线段间最短距离
//
// 📐 碰撞对（ShapeAndShape 变体）：
//   - 每个几何体类型组合有独立的窄相位函数：
//     Circle-Circle, Capsule-Circle, Polygon-Circle,
//     Capsule-Capsule, Polygon-Capsule, Polygon-Polygon,
//     Segment-*, ChainSegment-*
// ==============================================================

using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsQuery::ShapeAndShape", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_ShapeAndShape(PhysicsShape shapeA, PhysicsTransform transformA, PhysicsShape shapeB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::CircleAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_CircleAndCircle(CircleGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::CapsuleAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_CapsuleAndCircle(CapsuleGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::SegmentAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_SegmentAndCircle(SegmentGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::PolygonAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_PolygonAndCircle(PolygonGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::CapsuleAndCapsule", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_CapsuleAndCapsule(CapsuleGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::SegmentAndCapsule", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_SegmentAndCapsule(SegmentGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::PolygonAndCapsule", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_PolygonAndCapsule(PolygonGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::PolygonAndPolygon", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_PolygonAndPolygon(PolygonGeometry geometryA, PhysicsTransform transformA, PolygonGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::SegmentAndPolygon", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_SegmentAndPolygon(SegmentGeometry geometryA, PhysicsTransform transformA, PolygonGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::ChainSegmentAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_ChainSegmentAndCircle(ChainSegmentGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::ChainSegmentAndCapsule", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_ChainSegmentAndCapsule(ChainSegmentGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::ChainSegmentAndPolygon", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_ChainSegmentAndPolygon(ChainSegmentGeometry geometryA, PhysicsTransform transformA, PolygonGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::CastShapes", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PhysicsQuery_CastShapes(PhysicsQuery.CastShapePairInput input);
        [NativeMethod(Name = "PhysicsQuery::SegmentDistance", IsThreadSafe = true)] extern internal static PhysicsQuery.SegmentDistanceResult PhysicsQuery_SegmentDistance(SegmentGeometry geometryA, PhysicsTransform transformA, SegmentGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::ShapeDistance", IsThreadSafe = true)] extern internal static PhysicsQuery.DistanceResult PhysicsQuery_ShapeDistance(PhysicsQuery.DistanceInput distanceInput);
        [NativeMethod(Name = "PhysicsQuery::ShapeTimeOfImpact", IsThreadSafe = true)] extern internal static PhysicsQuery.TimeOfImpactResult PhysicsQuery_ShapeTimeOfImpact(PhysicsQuery.TimeOfImpactInput toiInput);
    }
}
