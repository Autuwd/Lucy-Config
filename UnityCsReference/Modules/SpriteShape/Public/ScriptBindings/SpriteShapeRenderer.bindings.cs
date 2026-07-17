// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// SpriteShapeRenderer — 2D 地形/形状渲染器
//
// 🎯 核心作用：
//   SpriteShapeRenderer 用于渲染基于样条曲线（Spline）定义的
//   2D 形状，常用于游戏中的地形、道路、围栏等可弯曲的 2D 元素。
//   它根据 Control Points（控制点）和 Sprite 沿曲线生成网格。
//
// 📌 工作流程：
//   1. 定义 ShapeControlPoint（控制点位置 + 切线）
//   2. 通过 SpriteShapeParameters 配置：
//      - angleThreshold：角度阈值，控制何时切换 Sprite
//      - fillTexture/fillScale：填充纹理和缩放
//      - splineDetail：样条插值精度
//      - bevelCutoff/bevelSize：斜面效果参数
//      - carpet：是否为"地毯"模式（有填充）
//      - spriteBorders：是否启用 9 切片角点
//   3. 调用 Prepare() 提交 Job 数据生成网格
//
// 💡 数据通道（Channels）：
//   GetChannels() 系列方法返回 NativeArray/NativeSlice：
//   - Position (Vector3)：顶点位置
//   - TexCoord0 (Vector2)：纹理坐标
//   - Color (Color32)：顶点颜色
//   - Tangent (Vector4)：切线
//   - Normal (Vector3)：法线
//   通过 bit mask 选择需要的通道组合，减少不必要的数据传输。
//
// ⚡ 性能提示：
//   - 使用 Jobs 系统异步生成网格（Prepare 方法接受 JobHandle）
//   - SpriteShapeSegment 描述每个网格段的几何信息
//   - SetLocalAABB 可手动更新包围盒以优化裁剪
//
// 🔗 继承链：
//   SpriteShapeRenderer -> Renderer -> Component -> Object
//
// 📍 对应 C++ 头文件：Modules/SpriteShape/Public/SpriteShapeRenderer.h
// ==============================================================

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting.APIUpdating;
using System;


using Unity.Jobs;

namespace UnityEngine.U2D
{
    /// <summary>
    /// SpriteShapeParameters contains SpriteShape properties that are used for generating it.
    /// </summary>
    // ==============================================================
    // SpriteShapeParameters — 形状生成参数
    // ==============================================================
    // 🎯 配置 SpriteShape 网格生成的所有参数
    //
    // 📌 字段说明：
    //   transform：变换矩阵（应用到生成的网格）
    //   fillTexture：填充区域使用的纹理
    //   fillScale：填充纹理缩放（0 = 无填充）
    //   splineDetail：样条插值精度（越大越平滑但顶点越多）
    //   angleThreshold：角度阈值（度），超过此角度时切换到 Corner Sprite
    //   borderPivot：边界轴心点
    //   bevelCutoff：斜面截断角度
    //   bevelSize：斜面大小
    //
    // 💡 布尔标志：
    //   carpet：是否为"地毯"模式（有填充区域）
    //   smartSprite：智能 Sprite 模式（整个形状使用同一纹理）
    //   adaptiveUV：自适应 UV（根据形状自动调整 UV 布局）
    //   spriteBorders：启用 9 切片角点
    //   stretchUV：填充 UV 拉伸模式
    // ==============================================================
    [MovedFrom("UnityEngine.Experimental.U2D")]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct SpriteShapeParameters
    {
        public Matrix4x4 transform;
        public Texture2D fillTexture;
        public uint fillScale;                          // A Fill Scale of 0 means NO fill.
        public uint splineDetail;
        public float angleThreshold;
        public float borderPivot;
        public float bevelCutoff;
        public float bevelSize;

        public bool carpet;                             // Carpets have Fills.
        public bool smartSprite;                        // Enabling this would mean a specialized Shape using only one Texture for all Sprites. If enabled must define CarpetInfo.
        public bool adaptiveUV;                         // Adaptive UV.
        public bool spriteBorders;                      // Allow 9 - Splice Corners to be used.
        public bool stretchUV;                          // Fill UVs are stretched.
    }

    /// <summary>
    /// SpriteShapeSegment contains data for each segment of mesh generated for SpriteShape.
    /// </summary>
    // ==============================================================
    // SpriteShapeSegment — 网格段描述结构
    // ==============================================================
    // 🎯 描述 SpriteShape 生成网格中每一段的几何信息
    //
    // 📌 字段说明：
    //   geomIndex：几何数据起始索引
    //   indexCount：该段的索引数量
    //   vertexCount：该段的顶点数量
    //   spriteIndex：该段使用的 Sprite 索引
    //
    // 💡 与 Sprite 的关系：
    //   SpriteShape 会根据样条曲线的角度自动选择合适的 Sprite，
    //   spriteIndex 指向 SpriteShapeComponent.Sprites 数组中的元素。
    //   角度变化超过 angleThreshold 时，spriteIndex 会切换到角点 Sprite。
    // ==============================================================
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public struct SpriteShapeSegment
    {
        private int m_GeomIndex;
        private int m_IndexCount;
        private int m_VertexCount;
        private int m_SpriteIndex;

        public int geomIndex
        {
            get { return m_GeomIndex; }
            set { m_GeomIndex = value; }
        }
        public int indexCount
        {
            get { return m_IndexCount; }
            set { m_IndexCount = value; }
        }
        public int vertexCount
        {
            get { return m_VertexCount; }
            set { m_VertexCount = value; }
        }
        public int spriteIndex
        {
            get { return m_SpriteIndex; }
            set { m_SpriteIndex = value; }
        }
    }

    internal enum SpriteShapeDataType
    {
        Index,
        Segment,
        BoundingBox,
        ChannelVertex,
        ChannelTexCoord0,
        ChannelNormal,
        ChannelTangent,
        ChannelColor,
        DataCount
    }

    // ==============================================================
    // SpriteShapeRenderer — 2D 形状/地形渲染器
    // ==============================================================
    // 🎯 根据样条控制点和 Sprite 生成并渲染 2D 曲面形状
    //
    // 📌 核心方法：
    //   Prepare(handle, shapeParams, sprites)：
    //     提交 Job 数据生成网格。使用 Job System 异步处理。
    //   GetSegments(dataSize)：获取网格段数组
    //   GetChannels(...)：获取顶点/UV/颜色/法线/切线通道数据
    //   SetLocalAABB()：设置本地包围盒
    //   GetSplineMeshCount()：获取子网格数量
    //
    // 📌 颜色和遮罩：
    //   color：颜色叠加
    //   maskInteraction：SpriteMask 交互模式
    //
    // ⚡ 数据访问优化：
    //   通过 GetChannelDataArray<T>() 提供零拷贝的 NativeSlice 访问。
    //   可选择需要的通道组合，避免不必要的数据传输。
    //   hotChannelMask 位掩码控制哪些通道实际分配 GPU 内存。
    // ==============================================================
    [NativeHeader("Modules/SpriteShape/Public/SpriteShapeRenderer.h")]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public class SpriteShapeRenderer : Renderer
    {
        public extern Color color
        {
            get;
            set;
        }

        public extern SpriteMaskInteraction maskInteraction
        {
            get;
            set;
        }


        extern internal int GetVertexCount();
        extern internal int GetIndexCount();
        extern internal Bounds GetLocalAABB();

        extern public void Prepare(JobHandle handle, SpriteShapeParameters shapeParams, Sprite[] sprites);

        extern private void RefreshSafetyHandle(SpriteShapeDataType arrayType);
        extern private AtomicSafetyHandle GetSafetyHandle(SpriteShapeDataType arrayType);
        unsafe private NativeArray<T> GetNativeDataArray<T>(SpriteShapeDataType dataType) where T : struct
        {
            RefreshSafetyHandle(dataType);

            var info = GetDataInfo(dataType);
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(info.buffer, info.count, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetSafetyHandle(dataType));
            return array;
        }

        unsafe private NativeSlice<T> GetChannelDataArray<T>(SpriteShapeDataType dataType, VertexAttribute channel) where T : struct
        {
            RefreshSafetyHandle(dataType);

            var info = GetChannelInfo(channel);
            var buffer = (byte*)(info.buffer) + info.offset;
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(buffer, info.stride, info.count);

            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, GetSafetyHandle(dataType));
            return slice;
        }

        extern private void SetSegmentCount(int geomCount);
        extern private void SetMeshDataCount(int vertexCount, int indexCount);
        extern private void SetMeshChannelInfo(int vertexCount, int indexCount, int hotChannelMask);
        extern private SpriteChannelInfo GetDataInfo(SpriteShapeDataType arrayType);
        extern private SpriteChannelInfo GetChannelInfo(VertexAttribute channel);

        /// <summary>
        /// Sets the local axis aligned bounding box.
        /// </summary>
        /// <param name="bounds"> The bounding box to set. </param>
        extern public void SetLocalAABB(Bounds bounds);


        /// <summary>
        /// Returns the <B>SpriteShapeRender's</B> number of submeshes.
        /// </summary>
        /// <returns>Returns the number of submeshes.</returns>
        extern public int GetSplineMeshCount();


        /// <summary>
        /// Returns Bounds of SpriteShapeRenderer in a NativeArray so C# Job can access it.
        /// </summary>
        /// <returns>Returns a NativeArray of Bounds with size always 1.</returns>
        unsafe public NativeArray<Bounds> GetBounds()
        {
            return GetNativeDataArray<Bounds>(SpriteShapeDataType.BoundingBox);
        }

        /// <summary>
        /// Returns a NativeArray of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the Array requested.</param>
        /// <returns>Returns a NativeArray of SpriteShapeSegments with requested Array size.</returns>
        unsafe public NativeArray<SpriteShapeSegment> GetSegments(int dataSize)
        {
            SetSegmentCount(dataSize);
            return GetNativeDataArray<SpriteShapeSegment>(SpriteShapeDataType.Segment);
        }

        /// <summary>
        /// Gets NativeArrays of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the NativeArray requested.</param>
        /// <param name="indices">NativeArray of indices.</param>
        /// <param name="vertices">NativeSlice of vertices.</param>
        /// <param name="texcoords">NativeSlice of texture coordinate for channel 0.</param>
        unsafe public void GetChannels(int dataSize, out NativeArray<ushort> indices, out NativeSlice<Vector3> vertices, out NativeSlice<Vector2> texcoords)
        {
            SetMeshDataCount(dataSize, dataSize);
            indices = GetNativeDataArray<ushort>(SpriteShapeDataType.Index);
            vertices = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelVertex, VertexAttribute.Position);
            texcoords = GetChannelDataArray<Vector2>(SpriteShapeDataType.ChannelTexCoord0, VertexAttribute.TexCoord0);
        }

        /// <summary>
        /// Gets NativeArrays of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the NativeArray requested.</param>
        /// <param name="indices">NativeArray of indices.</param>
        /// <param name="vertices">NativeSlice of vertices.</param>
        /// <param name="texcoords">NativeSlice of texture coordinate for channel 0.</param>
        /// <param name="tangents">NativeSlice of colors.</param>
        unsafe public void GetChannels(int dataSize, out NativeArray<ushort> indices, out NativeSlice<Vector3> vertices, out NativeSlice<Vector2> texcoords, out NativeSlice<Color32> colors)
        {
            SetMeshChannelInfo(dataSize, dataSize, (int)(1 << (int)VertexAttribute.Color));
            indices = GetNativeDataArray<ushort>(SpriteShapeDataType.Index);
            vertices = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelVertex, VertexAttribute.Position);
            texcoords = GetChannelDataArray<Vector2>(SpriteShapeDataType.ChannelTexCoord0, VertexAttribute.TexCoord0);
            colors = GetChannelDataArray<Color32>(SpriteShapeDataType.ChannelColor, VertexAttribute.Color);
        }

        /// <summary>
        /// Gets NativeArrays of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the NativeArray requested.</param>
        /// <param name="indices">NativeArray of indices.</param>
        /// <param name="vertices">NativeSlice of vertices.</param>
        /// <param name="texcoords">NativeSlice of texture coordinate for channel 0.</param>
        /// <param name="tangents">NativeSlice of tangents.</param>
        unsafe public void GetChannels(int dataSize, out NativeArray<ushort> indices, out NativeSlice<Vector3> vertices, out NativeSlice<Vector2> texcoords, out NativeSlice<Vector4> tangents)
        {
            SetMeshChannelInfo(dataSize, dataSize, (int)(1 << (int)VertexAttribute.Tangent));
            indices = GetNativeDataArray<ushort>(SpriteShapeDataType.Index);
            vertices = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelVertex, VertexAttribute.Position);
            texcoords = GetChannelDataArray<Vector2>(SpriteShapeDataType.ChannelTexCoord0, VertexAttribute.TexCoord0);
            tangents = GetChannelDataArray<Vector4>(SpriteShapeDataType.ChannelTangent, VertexAttribute.Tangent);
        }

        /// <summary>
        /// Gets NativeArrays of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the NativeArray requested.</param>
        /// <param name="indices">NativeArray of indices.</param>
        /// <param name="vertices">NativeSlice of vertices.</param>
        /// <param name="texcoords">NativeSlice of texture coordinate for channel 0.</param>
        /// <param name="tangents">NativeSlice of colors.</param>
        unsafe public void GetChannels(int dataSize, out NativeArray<ushort> indices, out NativeSlice<Vector3> vertices, out NativeSlice<Vector2> texcoords, out NativeSlice<Color32> colors, out NativeSlice<Vector4> tangents)
        {
            SetMeshChannelInfo(dataSize, dataSize, (int)((1 << (int)VertexAttribute.Color) | (1 << (int)VertexAttribute.Tangent)));
            indices = GetNativeDataArray<ushort>(SpriteShapeDataType.Index);
            vertices = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelVertex, VertexAttribute.Position);
            texcoords = GetChannelDataArray<Vector2>(SpriteShapeDataType.ChannelTexCoord0, VertexAttribute.TexCoord0);
            colors = GetChannelDataArray<Color32>(SpriteShapeDataType.ChannelColor, VertexAttribute.Color);
            tangents = GetChannelDataArray<Vector4>(SpriteShapeDataType.ChannelTangent, VertexAttribute.Tangent);
        }

        /// <summary>
        /// Gets NativeArrays of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the NativeArray requested.</param>
        /// <param name="indices">NativeArray of indices.</param>
        /// <param name="vertices">NativeSlice of vertices.</param>
        /// <param name="texcoords">NativeSlice of texture coordinate for channel 0.</param>
        /// <param name="tangents">NativeSlice of tangents.</param>///
        /// <param name="normals">NativeSlice of normals.</param>
        unsafe public void GetChannels(int dataSize, out NativeArray<ushort> indices, out NativeSlice<Vector3> vertices, out NativeSlice<Vector2> texcoords, out NativeSlice<Vector4> tangents, out NativeSlice<Vector3> normals)
        {
            SetMeshChannelInfo(dataSize, dataSize, (int)((1 << (int)VertexAttribute.Normal) | (1 << (int)VertexAttribute.Tangent)));
            indices = GetNativeDataArray<ushort>(SpriteShapeDataType.Index);
            vertices = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelVertex, VertexAttribute.Position);
            texcoords = GetChannelDataArray<Vector2>(SpriteShapeDataType.ChannelTexCoord0, VertexAttribute.TexCoord0);
            tangents = GetChannelDataArray<Vector4>(SpriteShapeDataType.ChannelTangent, VertexAttribute.Tangent);
            normals = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelNormal, VertexAttribute.Normal);
        }

        /// <summary>
        /// Gets NativeArrays of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the NativeArray requested.</param>
        /// <param name="indices">NativeArray of indices.</param>
        /// <param name="vertices">NativeSlice of vertices.</param>
        /// <param name="texcoords">NativeSlice of texture coordinate for channel 0.</param>
        /// <param name="tangents">NativeSlice of colors.</param>
        unsafe public void GetChannels(int dataSize, out NativeArray<ushort> indices, out NativeSlice<Vector3> vertices, out NativeSlice<Vector2> texcoords, out NativeSlice<Color32> colors, out NativeSlice<Vector4> tangents, out NativeSlice<Vector3> normals)
        {
            SetMeshChannelInfo(dataSize, dataSize, (int)((1 << (int)VertexAttribute.Color) | (1 << (int)VertexAttribute.Normal) | (1 << (int)VertexAttribute.Tangent)));
            indices = GetNativeDataArray<ushort>(SpriteShapeDataType.Index);
            vertices = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelVertex, VertexAttribute.Position);
            texcoords = GetChannelDataArray<Vector2>(SpriteShapeDataType.ChannelTexCoord0, VertexAttribute.TexCoord0);
            colors = GetChannelDataArray<Color32>(SpriteShapeDataType.ChannelColor, VertexAttribute.Color);
            tangents = GetChannelDataArray<Vector4>(SpriteShapeDataType.ChannelTangent, VertexAttribute.Tangent);
            normals = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelNormal, VertexAttribute.Normal);
        }

        [FreeFunction(Name = "SpriteShapeUtility::SetShaderUserValue", HasExplicitThis = true)] extern internal void Internal_SetShaderUserValueUInt(UInt32 v);
        public void SetShaderUserValue(UInt32 v) => Internal_SetShaderUserValueUInt(v);
        [FreeFunction(Name = "SpriteShapeUtility::GetShaderUserValue", HasExplicitThis = true)] extern internal UInt32 Internal_GetShaderUserValueUInt();
        public UInt32 GetShaderUserValue() { return Internal_GetShaderUserValueUInt(); }
    }
}
