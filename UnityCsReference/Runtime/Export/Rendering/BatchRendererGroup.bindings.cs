// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 BatchRendererGroup — GPU 驱动渲染核心 API
//
// 📌 作用：
//   BatchRendererGroup（BRG）是 Unity 2021+ 提供的 GPU 驱动渲染接口，
//   允许开发者完全控制渲染数据的组织和提交，绕过传统的 RenderPipeline 逻辑。
//   这是实现 DOTS 渲染器（Entities Graphics）的底层 API。
//
// 🏗 核心概念：
//
//   【BatchID / BatchMaterialID / BatchMeshID】
//   - GPU 端资源的轻量级句柄，用于在渲染命令中引用批次、材质、网格
//   - 通过 RegisterMaterial / RegisterMesh 注册资源后获取
//
//   【BatchDrawCommand】
//   - 单个渲染命令：指定 batchID、materialID、meshID、实例范围
//   - 支持 Direct / Indirect / Procedural / ProceduralIndirect 四种绘制类型
//
//   【BatchCullingContext】
//   - 裁剪上下文：包含裁剪面、LOD 参数、视图矩阵等
//   - OnPerformCulling 回调在 Job System 中执行自定义裁剪
//
//   【BatchFilterSettings】
//   - 每个 BatchDrawRange 的过滤参数：
//     renderingLayerMask、layer、shadowCastingMode、motionMode 等
//
//   【ThreadedBatchContext】
//   - 线程安全的批次管理，可在 Job 中添加/移除批次
//
// 💡 理解关键：
//   - BRG 将传统的 "每帧提交 DrawCall" 模式转变为 "GPU 自主裁剪和绘制"
//   - 开发者需要实现 OnPerformCulling 回调来提供裁剪结果
//   - BatchRendererGroup 支持四种视图类型：Camera、Light、Picking、SelectionOutline
//
// ⚡ 性能优势：
//   - 支持大规模实例渲染（百万级物体）
//   - GPU 端裁剪减少 CPU 开销
//   - 与 DOTS/Jobs 完美配合，充分利用多核
//
// 📍 对应 C++ 头文件：Runtime/Camera/BatchRendererGroup.h
// ==============================================================


using System;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine.Scripting;
using UnityEngine.Bindings;

using Unity.Jobs;

namespace UnityEngine.Rendering
{
    // ==============================================================
    // BatchID — 批次标识符
    // 🎯 GPU 端批次的轻量级句柄（uint 值），用于 DrawCommand 引用
    // ==============================================================
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [NativeClass("BatchID")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchID : IEquatable<BatchID>
    {
        public readonly static BatchID Null = new BatchID { value = 0 };

        public uint value;

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BatchID)
            {
                return Equals((BatchID)obj);
            }

            return false;
        }

        public bool Equals(BatchID other)
        {
            return value == other.value;
        }

        public int CompareTo(BatchID other)
        {
            return value.CompareTo(other.value);
        }

        public static bool operator ==(BatchID a, BatchID b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(BatchID a, BatchID b)
        {
            return !a.Equals(b);
        }
    }

    // ==============================================================
    // BatchMaterialID — 材质标识符
    // 🎯 通过 RegisterMaterial 注册后获得的 GPU 端材质句柄
    // ==============================================================
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [NativeClass("BatchMaterialID")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchMaterialID : IEquatable<BatchMaterialID>
    {
        public readonly static BatchMaterialID Null = new BatchMaterialID { value = 0 };

        public uint value;

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BatchMaterialID)
            {
                return Equals((BatchMaterialID)obj);
            }

            return false;
        }

        public bool Equals(BatchMaterialID other)
        {
            return value == other.value;
        }

        public int CompareTo(BatchMaterialID other)
        {
            return value.CompareTo(other.value);
        }

        public static bool operator ==(BatchMaterialID a, BatchMaterialID b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(BatchMaterialID a, BatchMaterialID b)
        {
            return !a.Equals(b);
        }
    }

    // ==============================================================
    // BatchMeshID — 网格标识符
    // 🎯 通过 RegisterMesh 注册后获得的 GPU 端网格句柄
    // ==============================================================
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [NativeClass("BatchMeshID")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchMeshID : IEquatable<BatchMeshID>
    {
        public readonly static BatchMeshID Null = new BatchMeshID { value = 0 };

        public uint value;

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BatchMeshID)
            {
                return Equals((BatchMeshID)obj);
            }

            return false;
        }

        public bool Equals(BatchMeshID other)
        {
            return value == other.value;
        }

        public int CompareTo(BatchMeshID other)
        {
            return value.CompareTo(other.value);
        }

        public static bool operator ==(BatchMeshID a, BatchMeshID b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(BatchMeshID a, BatchMeshID b)
        {
            return !a.Equals(b);
        }
    }

    // ==============================================================
    // BatchDrawCommandType — 绘制命令类型枚举
    // 🎯 Direct=直接绘制 / Indirect=间接绘制 / Procedural=过程化绘制
    // ==============================================================
    // Match with BatchDrawCommandType in C++ side
    public enum BatchDrawCommandType : int
    {
        Direct = 0,
        Indirect = 1,
        Procedural = 2,
        ProceduralIndirect = 3,
    }

    // ==============================================================
    // BatchDrawCommandFlags — 绘制命令标志位枚举
    // 🎯 描述单个 DrawCommand 的附加属性（运动向量、光贴图、LOD 淡出等）
    // ==============================================================
    // Match with BatchDrawCommandFlags in C++ side
    [Flags]
    public enum BatchDrawCommandFlags : int
    {
        None = 0,
        FlipWinding = 1 << 0, // Flip triangle winding when rendering, e.g. when the scale is negative
        HasMotion = 1 << 1, // Draw command contains at least one instance that requires per-object motion vectors
        IsLightMapped = 1 << 2, // Draw command contains lightmapped objects, which has implications for setting some lighting constants
        HasSortingPosition = 1 << 3, // Draw command instances have explicit world space float3 sorting positions to be used for depth sorting
        LODCrossFadeKeyword = 1 << 4, // Draw command instances have LOD_FADE_CROSSFADE keyword enabled
        LODCrossFadeValuePacked = 1 << 5, // Draw command instances have a 8-bit SNORM crossfade dither factor in the highest bits of their visible instance index
        LODCrossFade = LODCrossFadeKeyword | LODCrossFadeValuePacked,
        UseLegacyLightmapsKeyword = 1 << 6, // Draw command instances have USE_LEGACY_LIGHTMAPS keyword enabled
    }

    // ==============================================================
    // BatchCullingFlags — 裁剪标志位枚举
    // 🎯 控制裁剪行为（如是否裁剪光照贴图阴影投射器）
    // ==============================================================
    // Match with CullLightmappedShadowCasters in C++ side
    [Flags]
    public enum BatchCullingFlags : int
    {
        None = 0,
        CullLightmappedShadowCasters = 1 << 0,
    }

    // ==============================================================
    // BatchCullingViewType — 裁剪视图类型枚举
    // 🎯 区分不同渲染用途：Camera / Light / Picking / SelectionOutline / Filtering
    // ==============================================================
    // Match with BatchCullingViewType in C++ side
    public enum BatchCullingViewType : int
    {
        Unknown = 0,
        Camera = 1,
        Light = 2,
        Picking = 3,
        SelectionOutline = 4,
        Filtering = 5
    }

    // ==============================================================
    // BatchCullingProjectionType — 裁剪投影类型
    // 🎯 区分透视投影和正交投影，用于裁剪算法选择
    // ==============================================================
    // Match with BatchCullingProjectionType in C++ side
    public enum BatchCullingProjectionType : int
    {
        Unknown = 0,
        Perspective = 1,
        Orthographic = 2,
    }

    // ==============================================================
    // BatchBufferTarget — BRG 缓冲区目标类型
    // 🎯 指示实例数据使用 SSBO（RawBuffer）还是 UBO（ConstantBuffer）
    // ==============================================================
    // Match with BatchBufferTarget in C++ side
    public enum BatchBufferTarget : int
    {
        Unknown = 0,
        UnsupportedByUnderlyingGraphicsApi = -1, // BRG not supported on this platform or graphics API
        RawBuffer = 1, // BRG supported using raw buffer instance data (SSBO)
        ConstantBuffer = 2, // BRG supported using constant buffer instance data (UBO)
    };

    // ==============================================================
    // BatchPackedCullingViewID — 打包后的裁剪视图标识符
    // 🎯 内部使用的视图 ID 句柄，包含 EntityId 信息
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchPackedCullingViewID : IEquatable<BatchPackedCullingViewID>
    {
        internal readonly ulong handle;

        public override int GetHashCode()
        {
            return handle.GetHashCode();
        }

        public bool Equals(BatchPackedCullingViewID other)
        {
            return this.handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BatchPackedCullingViewID))
            {
                return false;
            }
            return this.Equals((BatchPackedCullingViewID)obj);
        }

        public static bool operator ==(BatchPackedCullingViewID lhs, BatchPackedCullingViewID rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(BatchPackedCullingViewID lhs, BatchPackedCullingViewID rhs)
        {
            return !lhs.Equals(rhs);
        }

        [Obsolete("BatchPackedCullingViewID(int instanceID, int sliceIndex) is obsolete use BatchPackedCullingViewID(EntityId entityId).", true)]
        public BatchPackedCullingViewID(int instanceID, int sliceIndex)
        {
        }

        internal BatchPackedCullingViewID(ulong viewID)
        {
            this.handle = viewID;
        }

        [Obsolete("GetInstanceID() is obsolete, use GetEntityId() instead.", true)]
        public int GetInstanceID()
        {
            return 0;
        }

        public readonly EntityId GetEntityId()
        {
            int val = (int)(handle & 0xffffffff);
            return EntityId.FromULong((ulong)val);
        }

        [Obsolete("GetSliceIndex() is obsolete.", true)]
        public int GetSliceIndex()
        {
            return 0;
        }
    }

    // ==============================================================
    // BatchDrawCommand — 直接绘制命令结构体
    // 🎯 描述一次直接实例化绘制调用（batchID + materialID + meshID + 实例范围）
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchDrawCommand
    {
        public BatchDrawCommandFlags flags; // includes flipWinding and other dynamic flags
        public BatchID batchID;
        public BatchMaterialID materialID;
        public ushort splitVisibilityMask;
        public ushort lightmapIndex;
        public int sortingPosition; // If HasSortingPosition is set, this points to a float3 in instanceSortingPositions. If not, it will be directly casted into float and used as the distance.
        public uint visibleOffset;

        public uint visibleCount;
        public BatchMeshID meshID;
        public ushort submeshIndex;
        public ushort activeMeshLod;
    }

    // ==============================================================
    // BatchDrawCommandIndirect — 间接绘制命令结构体
    // 🎯 使用 GPU Buffer 中的实例数据进行间接绘制
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchDrawCommandIndirect
    {
        public BatchDrawCommandFlags flags; // includes flipWinding and other dynamic flags
        public BatchID batchID;
        public BatchMaterialID materialID;
        public ushort splitVisibilityMask;
        public ushort lightmapIndex;
        public int sortingPosition; // If HasSortingPosition is set, this points to a float3 in instanceSortingPositions. If not, it will be directly casted into float and used as the distance.
        public uint visibleOffset;

        public BatchMeshID meshID;
        public MeshTopology topology;
        public GraphicsBufferHandle visibleInstancesBufferHandle;
        public uint visibleInstancesBufferWindowOffset;
        public uint visibleInstancesBufferWindowSizeBytes;
        public GraphicsBufferHandle indirectArgsBufferHandle;
        public uint indirectArgsBufferOffset;
    }

    // ==============================================================
    // BatchDrawCommandProcedural — 过程化绘制命令结构体
    // 🎯 过程化渲染（无网格顶点，由 Shader 生成几何）
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchDrawCommandProcedural
    {
        public BatchDrawCommandFlags flags; // includes flipWinding and other dynamic flags
        public BatchID batchID;
        public BatchMaterialID materialID;
        public ushort splitVisibilityMask;
        public ushort lightmapIndex;
        public int sortingPosition; // If HasSortingPosition is set, this points to a float3 in instanceSortingPositions. If not, it will be directly casted into float and used as the distance.
        public uint visibleOffset;

        public uint visibleCount;
        public MeshTopology topology;
        public GraphicsBufferHandle indexBufferHandle;
        public uint baseVertex;
        public uint indexOffsetBytes;
        public uint elementCount;
    }

    // ==============================================================
    // BatchDrawCommandProceduralIndirect — 过程化间接绘制命令
    // 🎯 结合过程化渲染 + 间接绘制的组合命令
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchDrawCommandProceduralIndirect
    {
        public BatchDrawCommandFlags flags; // includes flipWinding and other dynamic flags
        public BatchID batchID;
        public BatchMaterialID materialID;
        public ushort splitVisibilityMask;
        public ushort lightmapIndex;
        public int sortingPosition; // If HasSortingPosition is set, this points to a float3 in instanceSortingPositions. If not, it will be directly casted into float and used as the distance.
        public uint visibleOffset;

        public MeshTopology topology;
        public GraphicsBufferHandle indexBufferHandle;
        public GraphicsBufferHandle visibleInstancesBufferHandle;
        public uint visibleInstancesBufferWindowOffset;
        public uint visibleInstancesBufferWindowSizeBytes;
        public GraphicsBufferHandle indirectArgsBufferHandle;
        public uint indirectArgsBufferOffset;
    }

    // ==============================================================
    // BatchFilterSettings — 批次过滤设置
    // 🎯 控制每个 BatchDrawRange 的渲染层、阴影模式、运动模式等过滤条件
    // ==============================================================
    // Match with BatchFilterSettings in C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchFilterSettings
    {
        public uint renderingLayerMask;
        public int rendererPriority;
        private ulong m_sceneCullingMask;
        public byte layer;
        private byte m_batchLayer;
        private byte m_motionMode;
        private byte m_shadowMode;
        private byte m_receiveShadows;
        private byte m_staticShadowCaster;
        private byte m_allDepthSorted;
        private byte m_isSceneCullingMaskSet;

        public byte batchLayer
        {
            get => m_batchLayer;
            set => m_batchLayer = value;
        }

        public MotionVectorGenerationMode motionMode
        {
            get => (MotionVectorGenerationMode)m_motionMode;
            set => m_motionMode = (byte)value;
        }

        public ShadowCastingMode shadowCastingMode
        {
            get => (ShadowCastingMode)m_shadowMode;
            set => m_shadowMode = (byte)value;
        }

        public bool receiveShadows
        {
            get => m_receiveShadows != 0;
            set => m_receiveShadows = (byte)(value ? 1 : 0);
        }

        public bool staticShadowCaster
        {
            get => m_staticShadowCaster != 0;
            set => m_staticShadowCaster = (byte)(value ? 1 : 0);
        }

        public bool allDepthSorted
        {
            get => m_allDepthSorted != 0;
            set => m_allDepthSorted = (byte)(value ? 1 : 0);
        }

        [FreeFunction("BatchFilterSettings::DefaultCullingMask", IsThreadSafe = true)]
        private extern static ulong DefaultCullingMask();

        public ulong sceneCullingMask
        {
            get => (m_isSceneCullingMaskSet != 0) ? m_sceneCullingMask : DefaultCullingMask();
            set
            {
                m_isSceneCullingMaskSet = 1;
                m_sceneCullingMask = value;
            }
        }
    }

    // ==============================================================
    // BatchDrawRange — 绘制范围结构体
    // 🎯 定义一组 DrawCommand 的范围和共享过滤设置
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchDrawRange
    {
        // Specifies which array of commands this range indexes into.
        public BatchDrawCommandType drawCommandsType;
        // The first BatchDrawCommand of this range is at this index in BatchCullingOutputDrawCommands.drawCommands
        public uint drawCommandsBegin;
        // How many BatchDrawCommand structs this range has. Can be 0 if there are no draws.
        public uint drawCommandsCount;
        // Filter settings for every draw in the range. If the filter settings don't match, the entire range can be skipped.
        public BatchFilterSettings filterSettings;
    }

    // ==============================================================
    // BatchCullingOutputDrawCommands — 裁剪输出绘制命令集合
    // 🎯 OnPerformCulling 回调中填充的输出结构体，包含所有绘制命令和实例数据
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BatchCullingOutputDrawCommands
    {
        // TempJob allocated by C#, released by C++
        public BatchDrawCommand* drawCommands;
        // TempJob allocated by C#, released by C++
        public BatchDrawCommandIndirect* indirectDrawCommands;
        // TempJob allocated by C#, released by C++
        public BatchDrawCommandProcedural* proceduralDrawCommands;
        // TempJob allocated by C#, released by C++
        public BatchDrawCommandProceduralIndirect* proceduralIndirectDrawCommands;
        // TempJob allocated by C#, released by C++
        public int* visibleInstances;
        // TempJob allocated by C#, released by C++
        public BatchDrawRange* drawRanges;
        // TempJob allocated by C#, released by C++
        public float* instanceSortingPositions;
        // TempJob allocated by C#, released by C++
        public EntityId* drawCommandPickingEntityIds;

        [Obsolete("drawCommandPickingInstanceIDs is deprecated. Use drawCommandPickingEntityIds instead.", true)]
        public int* drawCommandPickingInstanceIDs
        {
            get => (int*)drawCommandPickingEntityIds;
            set => drawCommandPickingEntityIds = (EntityId*)value;
        }
        public int drawCommandCount;
        public int indirectDrawCommandCount;
        public int proceduralDrawCommandCount;
        public int proceduralIndirectDrawCommandCount;
        public int visibleInstanceCount;
        public int drawRangeCount;
        public int instanceSortingPositionFloatCount;
    }

    // ==============================================================
    // MetadataValue — 元数据键值对结构体
    // 🎯 用于设置 Shader Property 的元数据值
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    public struct MetadataValue
    {
        public int NameID;
        public uint Value;
    }

    // ==============================================================
    // CullingSplit — 阴影级联裁剪分割数据
    // 🎯 描述阴影级联的包围球、裁剪面等参数
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [UsedByNativeCode]
    unsafe public struct CullingSplit
    {
        public Vector3 sphereCenter;
        public float sphereRadius;
        public int cullingPlaneOffset;
        public int cullingPlaneCount;
        public float cascadeBlendCullingFactor;
        public float nearPlane;
        public Matrix4x4 cullingMatrix;
    }

    // ==============================================================
    // BatchCullingContext — 裁剪上下文结构体
    // 🎯 OnPerformCulling 回调的输入参数，包含裁剪面、LOD参数、视图矩阵等
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct BatchCullingContext
    {
        internal BatchCullingContext(
            NativeArray<Plane> inCullingPlanes,
            NativeArray<CullingSplit> inCullingSplits,
            LODParameters inLodParameters,
            Matrix4x4 inLocalToWorldMatrix,
            BatchCullingViewType inViewType,
            BatchCullingProjectionType inProjectionType,
            BatchCullingFlags inBatchCullingFlags,
            ulong inViewID,
            uint inCullingLayerMask,
            ulong inSceneCullingMask,
            byte inExclusionSplitMask,
            int inReceiverPlaneOffset,
            int inReceiverPlaneCount,
            IntPtr inOcclusionBuffer)
        {
            cullingPlanes = inCullingPlanes;
            cullingSplits = inCullingSplits;
            lodParameters = inLodParameters;
            localToWorldMatrix = inLocalToWorldMatrix;
            viewType = inViewType;
            projectionType = inProjectionType;
            cullingFlags = inBatchCullingFlags;
            viewID = new BatchPackedCullingViewID(inViewID);
            cullingLayerMask = inCullingLayerMask;
            sceneCullingMask = inSceneCullingMask;
            splitExclusionMask = inExclusionSplitMask;
            receiverPlaneOffset = inReceiverPlaneOffset;
            receiverPlaneCount = inReceiverPlaneCount;
#pragma warning disable CS0618 // Type or member is obsolete
            isOrthographic = 0;
#pragma warning restore CS0618 // Type or member is obsolete
            occlusionBuffer = inOcclusionBuffer;
        }

        readonly public NativeArray<Plane> cullingPlanes;
        readonly public NativeArray<CullingSplit> cullingSplits;
        readonly public LODParameters lodParameters;
        readonly public Matrix4x4 localToWorldMatrix;
        readonly public BatchCullingViewType viewType;
        readonly public BatchCullingProjectionType projectionType;
        readonly public BatchCullingFlags cullingFlags;
        readonly public BatchPackedCullingViewID viewID;
        readonly public uint cullingLayerMask;
        readonly public ulong sceneCullingMask;
        readonly public ushort splitExclusionMask;
        [System.Obsolete("BatchCullingContext.isOrthographic is deprecated. Use BatchCullingContext.projectionType instead.")]
        readonly public byte isOrthographic;
        readonly public int receiverPlaneOffset;
        readonly public int receiverPlaneCount;
        readonly internal IntPtr occlusionBuffer;
    }

    // ==============================================================
    // BatchCullingOutput — 裁剪输出结构体
    // 🎯 封装裁剪后的绘制命令和自定义裁剪结果
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchCullingOutput
    {
        // One-element NativeArray to make it writable from C#
        public NativeArray<BatchCullingOutputDrawCommands> drawCommands;
        public NativeArray<IntPtr> customCullingResult;
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [UsedByNativeCode]
    unsafe struct BatchRendererCullingOutput
    {
        public JobHandle cullingJobsFence;
        public Matrix4x4 localToWorldMatrix;
        public Plane* cullingPlanes;
        public int cullingPlaneCount;
        public int receiverPlaneOffset;
        public int receiverPlaneCount;
        public CullingSplit* cullingSplits;
        public int cullingSplitCount;
        public BatchCullingViewType viewType;
        public BatchCullingProjectionType projectionType;
        public BatchCullingFlags cullingFlags;
        public ulong viewID;
        public uint  cullingLayerMask;
        public byte  splitExclusionMask;
        public ulong sceneCullingMask;
        public BatchCullingOutputDrawCommands* drawCommands;
        public uint brgId;
        public IntPtr occlusionBuffer;
        public IntPtr customCullingResult;
    }

    // ==============================================================
    // ThreadedBatchContext — 线程安全的批次管理上下文
    // 🎯 允许在 Job System 中添加/移除批次，无需主线程同步
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    public struct ThreadedBatchContext
    {
        public IntPtr batchRendererGroup;

        [FreeFunction("BatchRendererGroup::AddDrawCommandBatch_Threaded", IsThreadSafe = true)]
        private extern static BatchID AddDrawCommandBatch(IntPtr brg, IntPtr values, int count, GraphicsBufferHandle buffer, uint bufferOffset, uint windowSize);

        [FreeFunction("BatchRendererGroup::SetDrawCommandBatchBuffer_Threaded", IsThreadSafe = true)]
        private extern static void SetDrawCommandBatchBuffer(IntPtr brg, BatchID batchID, GraphicsBufferHandle buffer);

        [FreeFunction("BatchRendererGroup::RemoveDrawCommandBatch_Threaded", IsThreadSafe = true)]
        private extern static void RemoveDrawCommandBatch(IntPtr brg, BatchID batchID);


        unsafe public BatchID AddBatch(NativeArray<MetadataValue> batchMetadata, GraphicsBufferHandle buffer)
        {
            return AddDrawCommandBatch(batchRendererGroup, (IntPtr)batchMetadata.GetUnsafeReadOnlyPtr(), batchMetadata.Length, buffer, 0, 0);
        }

        unsafe public BatchID AddBatch(NativeArray<MetadataValue> batchMetadata, GraphicsBufferHandle buffer, uint bufferOffset, uint windowSize)
        {
            return AddDrawCommandBatch(batchRendererGroup, (IntPtr)batchMetadata.GetUnsafeReadOnlyPtr(), batchMetadata.Length, buffer, bufferOffset, windowSize);
        }

        public void SetBatchBuffer(BatchID batchID, GraphicsBufferHandle buffer)
        {
            SetDrawCommandBatchBuffer(batchRendererGroup, batchID, buffer);
        }

        public void RemoveBatch(BatchID batchID)
        {
            RemoveDrawCommandBatch(batchRendererGroup, batchID);
        }
    }

    // ==============================================================
    // BatchRendererGroupCreateInfo — BRG 创建参数结构体
    // 🎯 用于构造函数：指定裁剪回调、完成回调和用户上下文
    // ==============================================================
    public struct BatchRendererGroupCreateInfo
    {
        public BatchRendererGroup.OnPerformCulling cullingCallback;
        public BatchRendererGroup.OnFinishedCulling finishedCullingCallback;
        public IntPtr userContext;
    };

    // ==============================================================
    // BatchRendererGroup — GPU 驱动渲染核心管理类
    //
    // 🔑 核心委托：
    //   - OnPerformCulling：裁剪回调（在 Job 中执行，填充 BatchCullingOutput）
    //   - OnFinishedCulling：裁剪完成回调（可选）
    //
    // 🔑 核心方法：
    //   - AddBatch()：注册批次（材质 + 网格数据）→ 返回 BatchID
    //   - RemoveBatch()：移除批次
    //   - SetBatchMetadata()：更新批次元数据
    //   - RegisterMaterial() / RegisterMesh()：注册资源 → 返回 BatchMaterialID / BatchMeshID
    //   - UnregisterMaterial() / UnregisterMesh()：注销资源
    //
    // 💡 使用流程：
    //   1. 创建 BatchRendererGroup（传入 OnPerformCulling 回调）
    //   2. RegisterMaterial / RegisterMesh 获取 GPU 句柄
    //   3. AddBatch 注册批次数据
    //   4. 在 OnPerformCulling 中填充 BatchCullingOutputDrawCommands
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Math/Matrix4x4.h")]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [RequiredByNativeCode]
    public class BatchRendererGroup : IDisposable
    {
        IntPtr m_GroupHandle = IntPtr.Zero;
        OnPerformCulling m_PerformCulling;
        OnFinishedCulling m_FinishedCulling;

        internal IntPtr Handle => m_GroupHandle;

        unsafe public delegate JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext);
        unsafe public delegate void OnFinishedCulling(IntPtr customCullingResult);

        public unsafe BatchRendererGroup(OnPerformCulling cullingCallback, IntPtr userContext)
        {
            m_PerformCulling = cullingCallback;
            m_GroupHandle = Create(this, (void*)userContext);
        }

        public unsafe BatchRendererGroup(BatchRendererGroupCreateInfo info)
        {
            m_PerformCulling = info.cullingCallback;
            m_GroupHandle = Create(this, (void*)info.userContext);
            m_FinishedCulling = info.finishedCullingCallback;
        }

        public void Dispose()
        {
            Destroy(m_GroupHandle);
            m_GroupHandle = IntPtr.Zero;
        }

        public ThreadedBatchContext GetThreadedBatchContext()
        {
            return new ThreadedBatchContext { batchRendererGroup = m_GroupHandle };
        }

        private extern BatchID AddDrawCommandBatch(IntPtr values, int count, GraphicsBufferHandle buffer, uint bufferOffset, uint windowSize);
        unsafe public BatchID AddBatch(NativeArray<MetadataValue> batchMetadata, GraphicsBufferHandle buffer)
        {
            return AddDrawCommandBatch((IntPtr)batchMetadata.GetUnsafeReadOnlyPtr(), batchMetadata.Length, buffer, 0, 0);
        }
        unsafe public BatchID AddBatch(NativeArray<MetadataValue> batchMetadata, GraphicsBufferHandle buffer, uint bufferOffset, uint windowSize)
        {
            return AddDrawCommandBatch((IntPtr)batchMetadata.GetUnsafeReadOnlyPtr(), batchMetadata.Length, buffer, bufferOffset, windowSize);
        }

        private extern void RemoveDrawCommandBatch(BatchID batchID);
        public void RemoveBatch(BatchID batchID) { RemoveDrawCommandBatch(batchID); }

        private extern void SetDrawCommandBatchBuffer(BatchID batchID, GraphicsBufferHandle buffer);
        public void SetBatchBuffer(BatchID batchID, GraphicsBufferHandle buffer) { SetDrawCommandBatchBuffer(batchID, buffer); }

        public extern BatchMaterialID RegisterMaterial(Material material);
        public extern void UnregisterMaterial(BatchMaterialID material);
        public extern Material GetRegisteredMaterial(BatchMaterialID material);

        public extern BatchMeshID RegisterMesh(Mesh mesh);
        public extern void UnregisterMesh(BatchMeshID mesh);
        public extern Mesh GetRegisteredMesh(BatchMeshID mesh);

        public extern void SetGlobalBounds(Bounds bounds);

        public extern void SetPickingMaterial(Material material);
        public extern void SetErrorMaterial(Material material);
        public extern void SetLoadingMaterial(Material material);

        public extern void SetEnabledViewTypes(BatchCullingViewType[] viewTypes);

        private extern static BatchBufferTarget GetBufferTarget();
        public static BatchBufferTarget BufferTarget => GetBufferTarget();

        public extern static int GetConstantBufferMaxWindowSize();
        public extern static int GetConstantBufferOffsetAlignment();

        static extern unsafe IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] BatchRendererGroup group, void* userContext);

        static extern void Destroy(IntPtr groupHandle);

        [RequiredByNativeCode]
        unsafe static void InvokeOnPerformCulling(BatchRendererGroup group, ref BatchRendererCullingOutput context, ref LODParameters lodParameters, IntPtr userContext)
        {
            NativeArray<Plane> cullingPlanes = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Plane>(context.cullingPlanes, context.cullingPlaneCount, Allocator.Invalid);
            NativeArray<CullingSplit> cullingSplits = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<CullingSplit>(context.cullingSplits, context.cullingSplitCount, Allocator.Invalid);
            NativeArray<BatchCullingOutputDrawCommands> drawCommands = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<BatchCullingOutputDrawCommands>(
                context.drawCommands, 1, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref cullingPlanes, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref cullingSplits, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref drawCommands, AtomicSafetyHandle.Create());

            try
            {
                BatchCullingOutput cullingOutput = new BatchCullingOutput
                {
                    drawCommands = drawCommands,
                    customCullingResult = new NativeArray<IntPtr>(1, Allocator.Temp)
                };
                context.cullingJobsFence = group.m_PerformCulling(
                    group, new BatchCullingContext(
                        cullingPlanes,
                        cullingSplits,
                        lodParameters,
                        context.localToWorldMatrix,
                        context.viewType,
                        context.projectionType,
                        context.cullingFlags,
                        context.viewID,
                        context.cullingLayerMask,
                        context.sceneCullingMask,
                        context.splitExclusionMask,
                        context.receiverPlaneOffset,
                        context.receiverPlaneCount,
                        context.occlusionBuffer
                    ),
                    cullingOutput,
                    userContext
                );
                context.customCullingResult = cullingOutput.customCullingResult[0];
            }
            finally
            {
                JobHandle.ScheduleBatchedJobs();

                var valid = AtomicSafetyHandle.CheckAllBufferJobsAreDependencyOrHaveCompleted(cullingPlanes.m_Safety, context.cullingJobsFence);
                if (!valid)
                {
                    Debug.LogError("Error: The JobHandle returned from OnPerformCulling does not depend on all outstanding jobs scheduled against the " +
                        "provided NativeArray<Plane> of culling planes. This is not safe and may result in crashes or undefined behavior.");
                }

                valid = AtomicSafetyHandle.CheckAllBufferJobsAreDependencyOrHaveCompleted(cullingSplits.m_Safety, context.cullingJobsFence);
                if (!valid)
                {
                    Debug.LogError("Error: The JobHandle returned from OnPerformCulling does not depend on all outstanding jobs scheduled against the " +
                        "provided NativeArray<CullingSplit> of culling splits. This is not safe and may result in crashes or undefined behavior.");
                }

                valid = AtomicSafetyHandle.CheckAllBufferJobsAreDependencyOrHaveCompleted(drawCommands.m_Safety, context.cullingJobsFence);
                if (!valid)
                {
                    Debug.LogError("Error: The JobHandle returned from OnPerformCulling does not depend on all outstanding jobs scheduled against the " +
                        "output NativeArray<BatchCullingOutputDrawCommands> of draw commands. This is not safe and may result in crashes or undefined behavior.");
                }

                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(cullingPlanes));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(cullingSplits));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(drawCommands));
            }
        }

        [RequiredByNativeCode]
        static void InvokeOnFinishedCulling(BatchRendererGroup group, IntPtr customCullingResult)
        {
            try
            {
                if(group.m_FinishedCulling != null) group.m_FinishedCulling(customCullingResult);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(BatchRendererGroup batchRendererGroup) => batchRendererGroup.m_GroupHandle;
        }

        [FreeFunction("BatchRendererGroup::OcclusionTestAABB", IsThreadSafe = true)]
        internal extern static bool OcclusionTestAABB(IntPtr occlusionBuffer, Bounds aabb);
    }
}
