// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// SpriteDataAccess — Sprite 底层数据访问接口
//
// 🎯 核心作用：
//   提供对 Sprite 内部网格数据的低级访问能力，包括顶点属性
//   （位置/法线/切线/颜色/UV）、骨骼数据、绑定姿态和索引。
//   这些 API 主要用于自定义 2D 动画系统和 GPU 蒙皮。
//
// 📌 两大扩展类：
//   1. SpriteDataAccessExtensions：
//      - 操作 Sprite 资源本身的顶点/骨骼/索引数据
//      - GetVertexAttribute<T>()：读取指定通道的顶点属性
//      - SetVertexAttribute<T>()：修改顶点属性（运行时变形）
//      - GetBones() / SetBones()：骨骼层级数据
//      - GetBindPoses() / SetBindPoses()：骨骼绑定姿态矩阵
//
//   2. SpriteRendererDataAccessExtensions：
//      - 操作 SpriteRenderer 的可变形缓冲区
//      - SetDeformableBuffer()：设置 CPU 端变形数据
//      - SetBatchDeformableBuffer...：批量设置变形数据（高性能）
//      - IsGPUSkinningEnabled()：检查 GPU 蒙皮是否启用
//      - SetLocalAABB()：更新本地包围盒
//
// 💡 使用场景：
//   - 2D 骨骼动画系统（自定义 Animation Job）
//   - 运行时网格变形（挤压、拉伸、波浪效果）
//   - GPU 蒙皮优化（将变形计算放到 GPU）
//   - 自定义渲染管线中的 Sprite 数据读取
//
// ⚠️ 注意事项：
//   - SpriteBone 结构体包含 position/rotation/length/parentId/color
//   - 修改顶点数据后需要刷新渲染（SetVertexCount 可改变顶点数）
//   - 带 BlendShape 的 Sprite 设置 Tangent 时需要有 Normal 数据
//   - NativeArray 返回值绑定到 Sprite 的生命周期，Sprite 销毁后不可访问
//
// 📍 对应 C++ 头文件：Runtime/2D/Common/SpriteDataAccess.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting.APIUpdating;

using Unity.Jobs;

namespace UnityEngine.U2D
{
    // ==============================================================
    // SpriteBone — Sprite 骨骼数据结构
    // ==============================================================
    // 🎯 描述 2D 骨骼动画中的单根骨骼
    //
    // 📌 字段说明：
    //   name：骨骼名称
    //   guid：骨骼全局唯一标识符（用于跨资源引用）
    //   position：骨骼在 Sprite 本地空间中的位置
    //   rotation：骨骼的初始旋转（四元数）
    //   length：骨骼长度
    //   parentId：父骨骼索引（-1 表示根骨骼）
    //   color：骨骼颜色（用于骨骼调试可视化）
    //
    // 💡 层级关系：
    //   通过 parentId 构成树形骨骼层级。
    //   根骨骼的 parentId = -1，子骨骼指向父骨骼的数组索引。
    //   这种扁平数组+parentId 的设计便于 Job System 并行处理。
    // ==============================================================
    [NativeHeader("Runtime/2D/Common/SpriteDataAccess.h")]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/2D/Common/SpriteTypes.h")]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct SpriteBone
    {
        [SerializeField]
        [NativeNameAttribute("name")]
        string m_Name;
        [SerializeField]
        [NativeNameAttribute("guid")]
        string m_Guid;
        [SerializeField]
        [NativeNameAttribute("position")]
        Vector3 m_Position;
        [SerializeField]
        [NativeNameAttribute("rotation")]
        Quaternion m_Rotation;
        [SerializeField]
        [NativeNameAttribute("length")]
        float m_Length;
        [SerializeField]
        [NativeNameAttribute("parentId")]
        int m_ParentId;
        [SerializeField]
        [NativeNameAttribute("color")]
        Color32 m_Color;

        public string name { get { return m_Name; } set { m_Name = value; } }
        public string guid { get { return m_Guid; } set { m_Guid = value; } }
        public Vector3 position { get { return m_Position; } set { m_Position = value; } }
        public Quaternion rotation { get { return m_Rotation; } set { m_Rotation = value; } }
        public float length { get { return m_Length; } set { m_Length = value; } }
        public int parentId { get { return m_ParentId; } set { m_ParentId = value; } }
        public Color32 color { get { return m_Color; } set { m_Color = value; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules]
    internal struct SpriteChannelInfo
    {
        [NativeNameAttribute("buffer")]
        IntPtr m_Buffer;
        [NativeNameAttribute("count")]
        int m_Count;
        [NativeNameAttribute("offset")]
        int m_Offset;
        [NativeNameAttribute("stride")]
        int m_Stride;

        unsafe public void* buffer { get { return (void*)m_Buffer; } set { m_Buffer = (IntPtr)value; } }
        public int count { get { return m_Count; } set { m_Count = value; } }
        public int offset { get { return m_Offset; } set { m_Offset = value; } }
        public int stride { get { return m_Stride; } set { m_Stride = value; } }
    }

    // ==============================================================
    // SpriteDataAccessExtensions — Sprite 网格数据访问扩展方法
    // ==============================================================
    // 🎯 以扩展方法形式提供 Sprite 内部数据的读写接口
    //
    // 📌 顶点属性访问：
    //   GetVertexAttribute<T>(channel)：读取指定通道的 NativeSlice
    //     · Position (Vector3)：顶点位置
    //     · Normal (Vector3)：法线
    //     · Tangent (Vector4)：切线
    //     · Color (Color32)：顶点颜色
    //     · TexCoord0~7 (Vector2)：纹理坐标（最多 8 通道）
    //     · BlendWeight (BoneWeight)：骨骼权重
    //
    //   SetVertexAttribute<T>(channel, src)：修改顶点属性
    //     ⚠️ 设置 Tangent 时如果有 BlendShape 且无 Normal 会跳过并警告
    //
    // 📌 骨骼数据：
    //   GetBones()/SetBones()：骨骼层级数组
    //   GetBindPoses()/SetBindPoses()：骨骼绑定姿态矩阵数组
    //
    // 📌 索引数据：
    //   GetIndices()/SetIndices()：三角面索引（ushort 类型）
    //   GetVertexCount()/SetVertexCount()：顶点数量
    //
    // ⚡ 性能提示：
    //   返回的 NativeSlice/NativeArray 直接引用 Sprite 内部缓冲区，
    //   零拷贝访问。但 Sprite 销毁后这些引用将失效！
    // ==============================================================
    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeHeader("Runtime/2D/Common/SpriteDataAccess.h")]
    public static class SpriteDataAccessExtensions
    {
        private static void CheckAttributeTypeMatchesAndThrow<T>(VertexAttribute channel)
        {
            var channelTypeMatches = false;
            switch (channel)
            {
                case VertexAttribute.Position:
                case VertexAttribute.Normal:
                    channelTypeMatches = typeof(T) == typeof(Vector3); break;
                case VertexAttribute.Tangent:
                    channelTypeMatches = typeof(T) == typeof(Vector4); break;
                case VertexAttribute.Color:
                    channelTypeMatches = typeof(T) == typeof(Color32); break;
                case VertexAttribute.TexCoord0:
                case VertexAttribute.TexCoord1:
                case VertexAttribute.TexCoord2:
                case VertexAttribute.TexCoord3:
                case VertexAttribute.TexCoord4:
                case VertexAttribute.TexCoord5:
                case VertexAttribute.TexCoord6:
                case VertexAttribute.TexCoord7:
                    channelTypeMatches = typeof(T) == typeof(Vector2); break;
                case VertexAttribute.BlendWeight:
                    channelTypeMatches = typeof(T) == typeof(BoneWeight); break;
                default:
                    throw new InvalidOperationException(String.Format("The requested channel '{0}' is unknown.", channel));
            }

            if (!channelTypeMatches)
                throw new InvalidOperationException(String.Format("The requested channel '{0}' does not match the return type {1}.", channel, typeof(T).Name));
        }

        public unsafe static NativeSlice<T> GetVertexAttribute<T>(this Sprite sprite, VertexAttribute channel) where T : struct
        {
            CheckAttributeTypeMatchesAndThrow<T>(channel);
            var info = GetChannelInfo(sprite, channel);
            var buffer = (byte*)(info.buffer) + info.offset;
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(buffer, info.stride, info.count);
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, sprite.GetSafetyHandle());
            return slice;
        }

        unsafe public static void SetVertexAttribute<T>(this Sprite sprite, VertexAttribute channel, NativeArray<T> src) where T : struct
        {
            CheckAttributeTypeMatchesAndThrow<T>(channel);

            // Warn if setting tangents when blend shapes exist and sprite don't have normals
            if (channel == VertexAttribute.Tangent)
            {
                if (sprite.blendShapeCount > 0 && !sprite.HasVertexAttribute(VertexAttribute.Normal))
                {
                    Debug.LogWarning($"Blend shapes are not supported on sprites with position-and-tangent-only layout on sprite '{sprite.name}'.");
                    return;
                }
            }

            SetChannelData(sprite, channel, src.GetUnsafeReadOnlyPtr());
        }

        unsafe public static NativeArray<Matrix4x4> GetBindPoses(this Sprite sprite)
        {
            var info = GetBindPoseInfo(sprite);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>(info.buffer, info.count, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, sprite.GetSafetyHandle());
            return arr;
        }

        unsafe public static void SetBindPoses(this Sprite sprite, NativeArray<Matrix4x4> src)
        {
            SetBindPoseData(sprite, src.GetUnsafeReadOnlyPtr(), src.Length);
        }

        unsafe public static NativeArray<ushort> GetIndices(this Sprite sprite)
        {
            var info = GetIndicesInfo(sprite);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ushort>(info.buffer, info.count, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, sprite.GetSafetyHandle());
            return arr;
        }

        unsafe public static void SetIndices(this Sprite sprite, NativeArray<ushort> src)
        {
            SetIndicesData(sprite, src.GetUnsafeReadOnlyPtr(), src.Length);
        }

        public static SpriteBone[] GetBones(this Sprite sprite)
        {
            return GetBoneInfo(sprite);
        }

        public static void SetBones(this Sprite sprite, SpriteBone[] src)
        {
            SetBoneData(sprite, src);
        }

        [NativeName("HasChannel")]
        extern public static bool HasVertexAttribute([NotNull] this Sprite sprite, VertexAttribute channel);

        // The only way to change the vertex count
        extern public static void SetVertexCount([NotNull] this Sprite sprite, int count);
        extern public static int GetVertexCount([NotNull] this Sprite sprite);

        // This lenght is not tied to vertexCount
        extern private static SpriteChannelInfo GetBindPoseInfo([NotNull] Sprite sprite);
        unsafe extern private static void SetBindPoseData([NotNull] Sprite sprite, void* src, int count);

        extern private static SpriteChannelInfo GetIndicesInfo([NotNull] Sprite sprite);
        unsafe extern private static void SetIndicesData([NotNull] Sprite sprite, void* src, int count);

        extern private static SpriteChannelInfo GetChannelInfo([NotNull] Sprite sprite, VertexAttribute channel);
        unsafe extern private static void SetChannelData([NotNull] Sprite sprite, VertexAttribute channel, void* src);

        extern private static SpriteBone[] GetBoneInfo([NotNull] Sprite sprite);
        extern private static void SetBoneData([NotNull] Sprite sprite, SpriteBone[] src);

        extern internal static int GetPrimaryVertexStreamSize(Sprite sprite);

        extern internal static AtomicSafetyHandle GetSafetyHandle([NotNull] this Sprite sprite);
    }

    // ==============================================================
    // SpriteRendererDataAccessExtensions — SpriteRenderer 变形缓冲区扩展
    // ==============================================================
    // 🎯 提供 SpriteRenderer 级别的 GPU 蒙皮和变形缓冲区管理
    //
    // 📌 核心功能：
    //   SetDeformableBuffer()：设置 CPU 端变形顶点数据
    //     · 数据大小必须与 Sprite 的顶点流大小一致
    //     · 用于运行时骨骼动画变形、物理变形等
    //
    //   SetBatchDeformableBufferAndLocalAABBArray()：批量设置
    //     · 一次性更新多个 SpriteRenderer 的变形缓冲区和 AABB
    //     · 大幅减少 CPU→GPU 传输次数
    //
    //   SetBatchBoneTransformIndexAndLocalAABBArray()：
    //     批量更新骨骼变换索引和包围盒
    //
    // 📌 GPU 蒙皮支持：
    //   IsGPUSkinningEnabled()：检查当前平台是否支持 GPU 蒙皮
    //   GPU 蒙皮将骨骼矩阵上传到 GPU，由顶点着色器执行变形
    //   比 CPU 蒙皮性能更好，但需要硬件支持
    //
    // ⚡ SRP Batching：
    //   IsSRPBatchingEnabled()：检查 SRP 批处理是否启用
    //   SetShaderUserValue()/GetShaderUserValue()：
    //     设置自定义 Shader 值，用于 SRP Batching 的 per-object 数据
    // ==============================================================
    [NativeHeader("Runtime/2D/Common/SpriteDataAccess.h")]
    [NativeHeader("Runtime/Graphics/Mesh/SpriteRenderer.h")]
    public static class SpriteRendererDataAccessExtensions
    {
        internal unsafe static void SetDeformableBuffer(this SpriteRenderer spriteRenderer, NativeArray<byte> src)
        {
            if (spriteRenderer.sprite == null)
                throw new ArgumentException(String.Format("spriteRenderer does not have a valid sprite set."));

            if (src.Length != SpriteDataAccessExtensions.GetPrimaryVertexStreamSize(spriteRenderer.sprite))
                throw new InvalidOperationException(String.Format("custom sprite vertex data size must match sprite asset's vertex data size {0} {1}", src.Length, SpriteDataAccessExtensions.GetPrimaryVertexStreamSize(spriteRenderer.sprite)));

            SetDeformableBuffer(spriteRenderer, src.GetUnsafeReadOnlyPtr(), src.Length);
        }

        internal unsafe static void SetDeformableBuffer(this SpriteRenderer spriteRenderer, NativeArray<Vector3> src)
        {
            if (spriteRenderer.sprite == null)
                throw new InvalidOperationException("spriteRenderer does not have a valid sprite set.");

            if (src.Length != spriteRenderer.sprite.GetVertexCount())
                throw new InvalidOperationException(String.Format("The src length {0} must match the vertex count of source Sprite {1}.", src.Length, spriteRenderer.sprite.GetVertexCount()));

            SetDeformableBuffer(spriteRenderer, src.GetUnsafeReadOnlyPtr(), src.Length);
        }

        internal unsafe static void SetBatchDeformableBufferAndLocalAABBArray(SpriteRenderer[] spriteRenderers, NativeArray<IntPtr> buffers, NativeArray<int> bufferSizes, NativeArray<Bounds> bounds)
        {
            int count = spriteRenderers.Length;
            if (count != buffers.Length
                || count != bufferSizes.Length
                || count != bounds.Length)
            {
                throw new ArgumentException("Input array sizes are not the same.");
            }

            SetBatchDeformableBufferAndLocalAABBArray(spriteRenderers, buffers.GetUnsafeReadOnlyPtr(), bufferSizes.GetUnsafeReadOnlyPtr(), bounds.GetUnsafeReadOnlyPtr(), count);
        }

        /// <summary>
        /// Performs a batch update of boneTransformIndex and AABB for the specified SpriteRenderers.
        /// </summary>
        internal static unsafe void SetBatchBoneTransformIndexAndLocalAABBArray(SpriteRenderer[] spriteRenderers, NativeArray<int> boneTransformIndices, NativeArray<Bounds> bounds)
        {
            int count = spriteRenderers.Length;
            if (count != boneTransformIndices.Length
                || count != bounds.Length)
            {
                throw new ArgumentException("Input array sizes are not the same.");
            }

            SetBatchBoneTransformIndexAndLocalAABBArray(spriteRenderers, boneTransformIndices.GetUnsafeReadOnlyPtr(), bounds.GetUnsafeReadOnlyPtr(), count);
        }

        internal unsafe static bool IsUsingDeformableBuffer(this SpriteRenderer spriteRenderer, IntPtr buffer)
        {
            return IsUsingDeformableBuffer(spriteRenderer, (void*)buffer);
        }

        internal static bool IsGPUSkinningEnabled()
        {
            return IsGPUSkinningEnabled(null);
        }

        extern public static void DeactivateDeformableBuffer([NotNull] this SpriteRenderer renderer);

        extern internal static void SetLocalAABB([NotNull] this SpriteRenderer renderer, Bounds aabb);

        extern private unsafe static void SetDeformableBuffer([NotNull] SpriteRenderer spriteRenderer, void* src, int count);

        extern private unsafe static void SetBatchDeformableBufferAndLocalAABBArray(SpriteRenderer[] spriteRenderers, void* buffers, void* bufferSizes, void* bounds, int count);

        extern private unsafe static bool IsUsingDeformableBuffer([NotNull] SpriteRenderer spriteRenderer, void* buffer);

        extern private unsafe static void SetBatchBoneTransformIndexAndLocalAABBArray(SpriteRenderer[] spriteRenderers, void* boneTransformIndices, void* bounds, int count);

        extern internal unsafe static void SetupMaterialProperties([NotNull] SpriteRenderer spriteRenderer);

        extern internal static bool IsGPUSkinningEnabled(SpriteRenderer spriteRenderer);

        extern internal static bool IsSRPBatchingEnabled([NotNull] this SpriteRenderer spriteRenderer);

        extern public static void SetShaderUserValue([NotNull] this SpriteRenderer spriteRenderer, UInt32 v);

        extern public static UInt32 GetShaderUserValue([NotNull] this SpriteRenderer spriteRenderer);
    }
}
