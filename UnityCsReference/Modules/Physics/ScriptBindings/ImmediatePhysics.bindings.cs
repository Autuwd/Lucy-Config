// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.LowLevelPhysics
{
    /// <summary>
    /// 即时物理使用的变换数据结构，包含位置和旋转。
    /// 用于在 ImmediatePhysics 中直接传递物体的变换信息给 PhysX。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ImmediateTransform
    {
        private Quaternion m_Rotation;  // 旋转（四元数）
        private Vector3 m_Position;     // 位置（世界空间）

        public Quaternion Rotation { get { return m_Rotation; } set { m_Rotation = value; } }
        public Vector3 Position { get { return m_Position; } set { m_Position = value; } }
    }

    /// <summary>
    /// 即时物理生成的接触点数据。
    /// 包含法线、分离距离、接触点、摩擦/弹性参数等完整接触信息。
    /// 底层对应 PhysX 接触点格式，用于 ImmediatePhysics 的低层级碰撞检测。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ImmediateContact
    {
        //==================== 字段布局与 PhysX 接触点数据对齐 ====================//
        private Vector3 m_Normal;          // 接触法线
        private float m_Separation;        // 分离距离（负数=穿透深度）
        private Vector3 m_Point;           // 接触点位置

        private float m_MaxImpulse;        // 最大冲量限制
        private Vector3 m_TargetVel;       // 目标速度（用于马达效果）
        private float m_StaticFriction;    // 静摩擦系数
        private byte m_MaterialFlags;      // 材质标志位
        private byte m_Pad;                // 对齐补齐
        private ushort m_InternalUse;      // 内部使用
        private uint m_InternalFaceIndex1; // 内部三角面索引
        private float m_DynamicFriction;   // 动摩擦系数
        private float m_Restitution;       // 弹性系数

        //==================== 公开属性 ====================//
        public Vector3 Normal { get { return m_Normal; } set { m_Normal = value; } }
        public float Separation { get { return m_Separation; } set { m_Separation = value; } }
        public Vector3 Point { get { return m_Point; } set { m_Point = value; } }
    }

    /// <summary>
    /// 即时物理（Immediate Physics）静态 API。
    ///
    /// 提供低层级的、"即时的"碰撞检测功能，不依赖于完整的 PhysX 场景模拟流程。
    /// 在内部直接调用 PhysX 的 PxGeometryQuery::generateContacts，不需要创建场景、不需要模拟步。
    ///
    /// 适用于：
    /// - 自定义物理查询
    /// - 编辑器工具中的碰撞预览
    /// - 不需要物理模拟的纯碰撞检测场景
    /// - Job System 中的并行碰撞检测
    ///
    /// 线程安全：GenerateContacts 方法标记了 isThreadSafe: true，可在 Job 中调用。
    /// </summary>
    [NativeHeader("Modules/Physics/ImmediatePhysics.h")]
    public static class ImmediatePhysics
    {
        /// <summary>
        /// 原生方法：调用 PhysX 的 PxGeometryQuery::generateContacts 生成碰撞接触点。
        /// 线程安全，可在多线程 Job 中调用。
        /// </summary>
        [FreeFunction("Physics::Immediate::GenerateContacts", isThreadSafe: true)]
        private static unsafe extern int GenerateContacts_Native(void* geom1, void* geom2, void* xform1, void* xform2,
            int numPairs, void* contacts, int contactArrayLength, void* sizes, int sizesArrayLength, float contactDistance);

        /// <summary>
        /// 在多对几何体之间生成碰撞接触点。
        /// 输入两组成对几何体和变换矩阵，输出接触点数据和每对的接触点数量。
        ///
        /// 使用 NativeArray 接口，可在 Unity Job System 中安全调用。
        /// 所有参与计算的几何体通过 GeometryHolder 传入（支持 Box/Sphere/Capsule/ConvexMesh/TriangleMesh/Terrain）。
        /// </summary>
        /// <param name="geom1">第一组几何体集合（只读）。</param>
        /// <param name="geom2">第二组几何体集合（只读）。</param>
        /// <param name="xform1">第一组几何体的变换集合（只读）。</param>
        /// <param name="xform2">第二组几何体的变换集合（只读）。</param>
        /// <param name="pairCount">要检测的几何体对数。</param>
        /// <param name="outContacts">输出的接触点数组。</param>
        /// <param name="outContactCounts">每对几何体生成的接触点数量（输出数组）。</param>
        /// <param name="contactDistance">接触检测距离阈值（默认 0.01）。必须为正数。</param>
        /// <returns>生成的接触点总数。</returns>
        public unsafe static int GenerateContacts(NativeArray<GeometryHolder>.ReadOnly geom1, NativeArray<GeometryHolder>.ReadOnly geom2,
            NativeArray<ImmediateTransform>.ReadOnly xform1, NativeArray<ImmediateTransform>.ReadOnly xform2, int pairCount,
            NativeArray<ImmediateContact> outContacts, NativeArray<int> outContactCounts, float contactDistance = 0.01f)
        {
            if (geom1.Length < pairCount ||
                geom2.Length < pairCount ||
                xform1.Length < pairCount ||
                xform2.Length < pairCount)
                throw new ArgumentException("提供的几何体或变换数组不足以容纳指定数量的配对。");

            if (pairCount > outContactCounts.Length)
                throw new ArgumentException("输出接触点计数数组不够大，其大小需要等于或超过配对数量。");

            if (contactDistance <= 0)
                throw new ArgumentException("接触距离必须为正数且不能为零。");

            return GenerateContacts_Native(
                geom1.GetUnsafeReadOnlyPtr(),
                geom2.GetUnsafeReadOnlyPtr(),
                xform1.GetUnsafeReadOnlyPtr(),
                xform2.GetUnsafeReadOnlyPtr(),
                pairCount,
                outContacts.GetUnsafePtr(),
                outContacts.Length,
                outContactCounts.GetUnsafePtr(),
                outContactCounts.Length,
                contactDistance);
        }
    }
}
