// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine.LowLevelPhysics
{
    /// <summary>
    /// 几何形状接口（低层级物理 API 用）。
    /// 所有具体几何类型（Box/Sphere/Capsule/ConvexMesh/TriangleMesh/Terrain）都实现此接口，
    /// 用于泛型方法 As&lt;T&gt; / Create&lt;T&gt; 的类型约束。
    /// </summary>
    public interface IGeometry
    {
        GeometryType GeometryType { get; }
    }

    //////////////////////////////////////////////////////////////////////////
    //  以下几何结构体封装 PhysX 的几何类型，内存布局必须与 C++ 侧严格一致  //
    //  用于 ImmediatePhysics 和底层碰撞检测 API                              //
    //////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 盒体几何，对应 PhysX 的 PxBoxGeometry。
    /// 内存对齐 4 字节（Pack = 4），布局按顺序：保留字段 + HalfExtents。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct BoxGeometry : IGeometry
    {
        private int m_UnusedReserved;   // 保留字段（用于在联合体中统一布局）
        private Vector3 m_HalfExtents;  // 半长宽高向量（half-extents）

        public Vector3 HalfExtents { get { return m_HalfExtents; } set { m_HalfExtents = value; } }

        public BoxGeometry(Vector3 halfExtents)
        {
            m_UnusedReserved = -1;         // -1 标识此字段无效（几何类型由 GeometryType 决定）
            m_HalfExtents = halfExtents;
        }

        public GeometryType GeometryType => GeometryType.Box;
    }

    /// <summary>
    /// 球体几何，对应 PhysX 的 PxSphereGeometry。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SphereGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private float m_Radius;        // 球体半径

        public float Radius { get { return m_Radius; } set { m_Radius = value; } }

        public SphereGeometry(float radius)
        {
            m_UnusedReserved = -1;
            m_Radius = radius;
        }

        public GeometryType GeometryType => GeometryType.Sphere;
    }

    /// <summary>
    /// 胶囊体几何，对应 PhysX 的 PxCapsuleGeometry。
    /// 胶囊体由半径和半长度定义（半长度是圆柱段高度的一半）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CapsuleGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private float m_Radius;        // 胶囊体半径
        private float m_HalfLength;    // 胶囊体圆柱段半长度（总高度 = 2 * m_HalfLength + 2 * m_Radius）

        public float Radius { get { return m_Radius; } set { m_Radius = value; } }
        public float HalfLength { get { return m_HalfLength; } set { m_HalfLength = value; } }

        public CapsuleGeometry(float radius, float halfLength)
        {
            m_UnusedReserved = -1;
            m_Radius = radius;
            m_HalfLength = halfLength;
        }

        public GeometryType GeometryType => GeometryType.Capsule;
    }

    /// <summary>
    /// 凸网格几何，对应 PhysX 的 PxConvexMeshGeometry。
    /// 包含网格缩放、轴旋转和指向 PxConvexMesh 的指针。
    /// 内存布局参考 PhysX 源码的 PxConvexMeshGeometry.h。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ConvexMeshGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private Vector3 m_Scale;         // 网格缩放
        private Quaternion m_Rotation;   // 缩放轴旋转（PxMeshScale）
        private IntPtr m_ConvexMesh;     // 指向原生 PxConvexMesh 的指针
        private byte m_MeshFlags;        // 网格标志位（如 PxConvexMeshGeometryFlag）
        private fixed byte m_MeshFlagsPadding[3];  // 补齐到 4 字节

        public Vector3 Scale { get { return m_Scale; } set { m_Scale = value; } }
        public Quaternion ScaleAxisRotation { get { return m_Rotation; } set { m_Rotation = value; } }

        public GeometryType GeometryType => GeometryType.ConvexMesh;
    }

    /// <summary>
    /// 三角形网格几何，对应 PhysX 的 PxTriangleMeshGeometry。
    /// 包含网格缩放、轴旋转和指向 PxTriangleMesh 的指针。
    /// 内存布局参考 PhysX 源码的 PxTriangleMeshGeometry.h。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TriangleMeshGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private Vector3 m_Scale;             // 网格缩放
        private Quaternion m_Rotation;       // 缩放轴旋转（PxMeshScale）
        private byte m_MeshFlags;            // 网格标志位（如 PxMeshGeometryFlag）
        private fixed byte m_MeshFlagsPadding[3];  // 补齐到 4 字节
        private IntPtr m_TriangleMesh;       // 指向原生 PxTriangleMesh 的指针

        public Vector3 Scale { get { return m_Scale; } set { m_Scale = value; } }
        public Quaternion ScaleAxisRotation { get { return m_Rotation; } set { m_Rotation = value; } }

        public GeometryType GeometryType => GeometryType.TriangleMesh;
    }

    /// <summary>
    /// 地形几何，封装 PhysX 地形碰撞数据。
    /// 包含指向地形数据的指针和行列/高度缩放因子。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TerrainGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private IntPtr m_TerrainData;       // 指向原生地形数据的指针
        private float m_HeightScale;        // 高度缩放
        private float m_RowScale;           // 行方向缩放
        private float m_ColumnScale;        // 列方向缩放
        private byte m_TerrainFlags;        // 地形标志位
        private fixed byte m_TerrainFlagsPadding[3];  // 补齐到 4 字节

        public GeometryType GeometryType => GeometryType.Terrain;
    }

    /// <summary>
    /// 几何类型枚举，与 PhysX 的 PxGeometryType::Enum 对应。
    /// 用于区分 GeometryHolder 中存储的具体几何类型。
    /// 注意：值 1 未使用（PhysX 中 eSPHERE=0, ePLANE=1 但 Plane 此处不需要）。
    /// </summary>
    public enum GeometryType : int
    {
        Sphere = 0,
        Capsule = 2,
        Box = 3,
        ConvexMesh = 4,
        TriangleMesh = 5,
        Terrain = 6,
        Invalid = -1      // 非法/未初始化
    }

    /// <summary>
    /// 几何数据容器（联合体风格），可容纳所有类型的 PhysX 几何数据。
    ///
    /// 内存对齐为 PxConvexMeshGeometry 的大小，确保能容纳所有更小的几何类型。
    /// 布局规则（对应 PhysX 的 PxGeometryHolder）：
    /// [00...03] -- PxGeometryType（几何类型）
    /// [04...31] -- PxMeshScale（缩放+旋转）
    /// [32...39] -- PxConvexMesh 指针（64位）
    /// [40...43] -- PxConvexMeshGeometryFlag + 3 字节补齐
    /// [44...47] -- 4 字节补齐
    /// 总大小 = 12 × 4 = 48 字节（12 个 int）
    ///
    /// 保留字段 m_UnusedReserved 在子类型中为 -1 以区分于 GeometryType。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GeometryHolder
    {
        // 与 C++ PhysicsCollisionGeometry.h 保持同步！
        internal fixed int m_Data[12];  // 48 字节的二进制块，强转为具体几何类型

        /// <summary>
        /// 将 GeometryHolder 转换为指定的几何类型 T。
        /// 会检查类型匹配，若 GeometryHolder 中存储的类型不是 T 则抛出异常。
        /// </summary>
        public T As<T>() where T : struct, IGeometry
        {
            T geometry = default;

            if (geometry.GeometryType != Type)
                throw new InvalidOperationException($"无法从存储 {Type} 的 GeometryHolder 中获取 {geometry.GeometryType} 类型的几何数据。");

            UnsafeUtility.CopyPtrToStructure(UnsafeUtility.AddressOf(ref this), out geometry);
            return geometry;
        }

        /// <summary>
        /// 从指定几何类型创建 GeometryHolder。
        /// 通过底层内存拷贝将具体几何结构体填入容器，并确保 GeometryType 字段正确。
        /// </summary>
        public static GeometryHolder Create<T>(T geometry) where T : struct, IGeometry
        {
            GeometryHolder holder = default;
            UnsafeUtility.CopyStructureToPtr(ref geometry, UnsafeUtility.AddressOf(ref holder));
            // 确保 GeometryType 被正确设置（因为结构体中的保留字段可能是 -1 而非 0）
            holder.m_Data[0] = (int)geometry.GeometryType;
            return holder;
        }

        /// <summary>获取 GeometryHolder 中存储的几何类型。</summary>
        public GeometryType Type => (GeometryType)m_Data[0];
    }

    /// <summary>
    /// 扩展方法：从 Collider 获取其底层 PhysX 几何数据。
    /// 用于低层级物理 API（如 ImmediatePhysics）读取碰撞器的形状数据。
    /// </summary>
    [NativeHeader("Modules/Physics/PhysicsCollisionGeometry.h")]
    internal static class PhysXGeometryHolderExtension
    {
        [FreeFunction("Physics::PhysXGeometryExtension::GetGeometryHolderFromCollider")]
        public static extern GeometryHolder GetGeometryHolder(this Collider col);
    }
}
