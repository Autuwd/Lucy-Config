// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Mesh — Unity 网格数据容器
//
// 📌 作用：
//   Mesh 存储 3D 模型的几何数据：顶点、索引、法线、UV、骨骼权重等。
//   它是 GPU 渲染 3D 物体的基础数据结构。
//   MeshRenderer 或 SkinnedMeshRenderer 通过 Mesh 来渲染物体。
//
// 🏗 核心数据结构：
//   - 顶点属性（Vertex Attributes）：位置 / 法线 / UV / 切线 / 颜色等
//   - 索引缓冲（Index Buffer）：定义三角形面片的顶点连接顺序
//   - 子网格（SubMesh）：同一 Mesh 中可包含多个子网格，使用不同材质
//   - 混合形状（Blend Shape）：面部表情等变形动画数据
//   - 骨骼蒙皮（Skinning）：骨骼权重和绑定姿势矩阵
//
// 💡 关键概念：
//   - VertexAttribute：描述顶点数据的语义（位置/法线/UV 等）
//   - IndexFormat：索引格式（16-bit UInt16 / 32-bit UInt32）
//   - MeshTopology：图元拓扑（三角形/线段/点/扇形等）
//   - SubMeshDescriptor：子网格描述（索引起始、计数、拓扑等）
//
// ⚡ 性能提示：
//   - MarkDynamic()：标记为动态 Mesh，GPU 端使用频繁更新的缓冲
//   - isReadable：控制 CPU 是否可读取网格数据
//   - UploadMeshData(true)：上传后标记为不可再读，释放 CPU 内存
//   - CombineMeshes()：将多个 Mesh 合并，减少 Draw Call
//   - LOD 系统：SetLod/SetLodCount 在 Mesh 内嵌多级细节
//
// 📍 对应 C++ 头文件：Runtime/Graphics/Mesh/MeshScriptBindings.h
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace UnityEngine
{
    // ==============================================================
    // Mesh — 网格几何数据容器
    //
    // 🎯 继承链：Mesh → Object（直接继承自 UnityEngine.Object）
    //
    // 🔑 关键属性：
    //   - vertexCount：顶点总数
    //   - subMeshCount：子网格数量
    //   - indexFormat：索引格式（16/32 位）
    //   - bounds：网格包围盒（用于视锥体剔除）
    //   - isReadable：是否可从 CPU 读取数据
    //
    // 🔑 核心方法：
    //   - SetVertexBufferParams / SetIndexBufferParams：定义缓冲布局
    //   - GetVertexAttribute / HasVertexAttribute：查询顶点属性
    //   - SetSubMesh / GetSubMesh：管理子网格
    //   - RecalculateNormals / RecalculateBounds：重新计算法线和包围盒
    //   - CombineMeshes：合并多个网格
    //
    // 💡 Mesh vs MeshData：
    //   - Mesh：可读写的完整网格对象（含 C++ 原生数据）
    //   - MeshData / MeshDataArray：只读视图，线程安全，适合 Job System
    // ==============================================================
    [UnityEngine.ExcludeFromPreset]
    [NativeHeader("Runtime/Graphics/Mesh/MeshScriptBindings.h")]
    public sealed partial class Mesh
    {
        [FreeFunction("MeshScripting::CreateMesh")] extern private static void Internal_Create([Writable] Mesh mono);

        [RequiredByNativeCode] // Used by IMGUI (even on empty projects, it draws development console & watermarks)
        public Mesh()
        {
            Internal_Create(this);
        }

        [FreeFunction("MeshScripting::MeshFromInstanceId")] extern internal static Mesh FromInstanceID(EntityId id);


        // ==============================================================
        // 索引缓冲（Index Buffer）— 三角形面片连接关系
        //
        // 📌 索引定义了哪些顶点构成一个三角形。
        //   例如索引 [0,1,2] 表示用第 0、1、2 个顶点构成一个三角形。
        //   多个三角形可共享顶点，节省内存。
        //
        // 📌 indexFormat：索引数据格式
        //   - UInt16（16 位）：最大 65535 个顶点（小型模型推荐）
        //   - UInt32（32 位）：支持任意数量顶点
        //   - 默认为 UInt16 以节省内存
        //
        // 📌 MeshTopology（图元拓扑）：
        //   - Triangles：三角形（最常用）
        //   - Lines：线段
        //   - Points：点
        //   - TriangleStrip / TriangleFan：三角形带/扇
        //
        // 💡 SetIndexBufferParams(count, format) 分配索引缓冲大小和格式。
        //    之后通过 SetIndices / SetIndexBufferData 填充数据。
        // ==============================================================

        // triangles/indices

        extern public UnityEngine.Rendering.IndexFormat indexFormat { get; set; }

        extern internal UInt32 GetTotalIndexCount();

        [FreeFunction(Name = "MeshScripting::SetIndexBufferParams", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetIndexBufferParams(int indexCount, UnityEngine.Rendering.IndexFormat format);

        [FreeFunction(Name = "MeshScripting::InternalSetIndexBufferData", HasExplicitThis = true, ThrowsException = true)]
        extern private void InternalSetIndexBufferData(IntPtr data, int dataStart, int meshBufferStart, int count, int elemSize, UnityEngine.Rendering.MeshUpdateFlags flags);
        [FreeFunction(Name = "MeshScripting::InternalSetIndexBufferDataFromArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void InternalSetIndexBufferDataFromArray(Span<byte> data, int dataStart, int meshBufferStart, int count, int elemSize, UnityEngine.Rendering.MeshUpdateFlags flags);

        [FreeFunction(Name = "MeshScripting::SetVertexBufferParamsFromPtr", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetVertexBufferParamsFromPtr(int vertexCount, IntPtr attributesPtr, int attributesCount);
        [FreeFunction(Name = "MeshScripting::SetVertexBufferParamsFromArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetVertexBufferParamsFromArray(int vertexCount, params UnityEngine.Rendering.VertexAttributeDescriptor[] attributes);

        [FreeFunction(Name = "MeshScripting::InternalSetVertexBufferData", HasExplicitThis = true)]
        extern private void InternalSetVertexBufferData(int stream, IntPtr data, int dataStart, int meshBufferStart, int count, int elemSize, UnityEngine.Rendering.MeshUpdateFlags flags);
        [FreeFunction(Name = "MeshScripting::InternalSetVertexBufferDataFromArray", HasExplicitThis = true)]
        extern private void InternalSetVertexBufferDataFromArray(int stream, Span<byte> data, int dataStart, int meshBufferStart, int count, int elemSize, UnityEngine.Rendering.MeshUpdateFlags flags);

        [FreeFunction(Name = "MeshScripting::GetVertexAttributesAlloc", HasExplicitThis = true)]
        extern private System.Array GetVertexAttributesAlloc();
        [FreeFunction(Name = "MeshScripting::GetVertexAttributesArray", HasExplicitThis = true)]
        extern private int GetVertexAttributesArray([NotNull] UnityEngine.Rendering.VertexAttributeDescriptor[] attributes);
        [FreeFunction(Name = "MeshScripting::GetVertexAttributesList", HasExplicitThis = true)]
        extern private int GetVertexAttributesList([NotNull] System.Collections.Generic.List<UnityEngine.Rendering.VertexAttributeDescriptor> attributes);
        [FreeFunction(Name = "MeshScripting::GetVertexAttributesCount", HasExplicitThis = true)]
        extern private int GetVertexAttributeCountImpl();
        [FreeFunction(Name = "MeshScripting::GetVertexAttributeByIndex", HasExplicitThis = true, ThrowsException = true)]
        extern public UnityEngine.Rendering.VertexAttributeDescriptor GetVertexAttribute(int index);

        [FreeFunction(Name = "MeshScripting::GetIndexStart", HasExplicitThis = true)]
        extern private UInt32 GetIndexStartImpl(int submesh, int meshlod);

        [FreeFunction(Name = "MeshScripting::GetIndexCount", HasExplicitThis = true)]
        extern private UInt32 GetIndexCountImpl(int submesh, int meshlod);

        [FreeFunction(Name = "MeshScripting::GetTrianglesCount", HasExplicitThis = true)]
        extern private UInt32 GetTrianglesCountImpl(int submesh, int meshlod);

        [FreeFunction(Name = "MeshScripting::GetBaseVertex", HasExplicitThis = true)]
        extern private UInt32 GetBaseVertexImpl(int submesh);

        [FreeFunction(Name = "MeshScripting::GetTriangles", HasExplicitThis = true)]
        extern private int[] GetTrianglesImpl(int submesh, bool applyBaseVertex, int meshlod);

        [FreeFunction(Name = "MeshScripting::GetIndices", HasExplicitThis = true)]
        extern private int[] GetIndicesImpl(int submesh, bool applyBaseVertex, int meshlod);

        [FreeFunction(Name = "SetMeshIndicesFromScript", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetIndicesImpl(int submesh, MeshTopology topology, UnityEngine.Rendering.IndexFormat indicesFormat, Span<byte> indices, int arrayStart, int arraySize, bool calculateBounds, int baseVertex, int meshlod);

        [FreeFunction(Name = "SetMeshIndicesFromNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetIndicesNativeArrayImpl(int submesh, MeshTopology topology, UnityEngine.Rendering.IndexFormat indicesFormat, IntPtr indices, int arrayStart, int arraySize, bool calculateBounds, int baseVertex, int meshlod);

        [FreeFunction(Name = "MeshScripting::ExtractTrianglesToArray", HasExplicitThis = true)]
        extern private void GetTrianglesNonAllocImpl([Out] int[] values, int submesh, bool applyBaseVertex, int meshlod);

        [FreeFunction(Name = "MeshScripting::ExtractTrianglesToArray16", HasExplicitThis = true)]
        extern private void GetTrianglesNonAllocImpl16([Out] ushort[] values, int submesh, bool applyBaseVertex, int meshlod);

        [FreeFunction(Name = "MeshScripting::ExtractIndicesToArray", HasExplicitThis = true)]
        extern private void GetIndicesNonAllocImpl([Out] int[] values, int submesh, bool applyBaseVertex, int meshlod);

        [FreeFunction(Name = "MeshScripting::ExtractIndicesToArray16", HasExplicitThis = true)]
        extern private void GetIndicesNonAllocImpl16([Out] ushort[] values, int submesh, bool applyBaseVertex, int meshlod);

        // ==============================================================
        // 顶点属性（Vertex Attributes）— 顶点数据布局
        //
        // 🎯 每个顶点可以携带多种属性数据：
        //   - Position（位置）：Vector3，必需
        //   - Normal（法线）：Vector3，用于光照计算
        //   - Tangent（切线）：Vector4，用于法线贴图
        //   - Color（颜色）：Color32，顶点颜色
        //   - TexCoord（UV 坐标）：Vector2/3/4，纹理映射坐标
        //   - BlendWeight（骨骼权重）：用于蒙皮动画
        //
        // 📌 顶点属性存储在"流"（Stream）中：
        //   - 同一 Stream 中的属性交错存储（Interleaved）
        //   - 不同 Stream 可独立更新（如只更新位置不动画 UV）
        //   - VertexAttributeDescriptor 描述每个属性的维度、格式、流、偏移
        //
        // 💡 HasVertexAttribute(attr) 检查是否存在某属性
        //    GetVertexAttributeDimension(attr) 获取属性维度（1/2/3/4）
        //    GetVertexAttributeFormat(attr) 获取属性数据格式（Float/UNorm 等）
        // ==============================================================

        // component (channels) setters/getters helpers

        [FreeFunction(Name = "MeshScripting::PrintErrorCantAccessChannel", HasExplicitThis = true)]
        extern private void PrintErrorCantAccessChannel(VertexAttribute ch);

        [FreeFunction(Name = "MeshScripting::HasChannel", HasExplicitThis = true)]
        extern public bool HasVertexAttribute(VertexAttribute attr);
        [FreeFunction(Name = "MeshScripting::GetChannelDimension", HasExplicitThis = true)]
        extern public int GetVertexAttributeDimension(VertexAttribute attr);
        [FreeFunction(Name = "MeshScripting::GetChannelFormat", HasExplicitThis = true)]
        extern public VertexAttributeFormat GetVertexAttributeFormat(VertexAttribute attr);
        [FreeFunction(Name = "MeshScripting::GetChannelStream", HasExplicitThis = true)]
        public extern int GetVertexAttributeStream(VertexAttribute attr);
        [FreeFunction(Name = "MeshScripting::GetChannelOffset", HasExplicitThis = true)]
        public extern int GetVertexAttributeOffset(VertexAttribute attr);

        [FreeFunction(Name = "SetMeshComponentFromSpanFromScript", HasExplicitThis = true)]
        extern private void SetArrayForChannelImpl(VertexAttribute channel, VertexAttributeFormat format, int dim, Span<byte> values, int arraySize, int valuesStart, int valuesCount, UnityEngine.Rendering.MeshUpdateFlags flags);

        [FreeFunction(Name = "SetMeshComponentFromNativeArrayFromScript", HasExplicitThis = true)]
        extern private void SetNativeArrayForChannelImpl(VertexAttribute channel, VertexAttributeFormat format, int dim, IntPtr values, int arraySize, int valuesStart, int valuesCount, UnityEngine.Rendering.MeshUpdateFlags flags);

        [FreeFunction(Name = "AllocExtractMeshComponentFromScript", HasExplicitThis = true)]
        extern private System.Array GetAllocArrayFromChannelImpl(VertexAttribute channel, VertexAttributeFormat format, int dim);

        [FreeFunction(Name = "ExtractMeshComponentFromScript", HasExplicitThis = true)]
        extern private void GetArrayFromChannelImpl(VertexAttribute channel, VertexAttributeFormat format, int dim, Span<byte> values);

        // ==============================================================
        // GPU 缓冲区访问（Native GPU Buffer Access）
        //
        // ⚡ 底层图形 API 资源访问（主要用于原生代码插件）：
        //   - vertexBufferCount：顶点流数量
        //   - GetVertexBufferStride(stream)：单个流的步幅（字节）
        //   - GetNativeVertexBufferPtr / GetNativeIndexBufferPtr：
        //     获取原生缓冲区指针（仅限 C++ 插件使用）
        //   - GetVertexBuffer / GetIndexBuffer：获取 GraphicsBuffer 对象
        //     可用于 Compute Shader 读写顶点数据
        //   - GetBoneWeightBuffer / GetBlendShapeBuffer：
        //     获取骨骼权重/混合形状的 GPU 缓冲
        //
        // 📌 vertexBufferTarget / indexBufferTarget：
        //   控制缓冲区可绑定到哪些 GPU 阶段（顶点/计算/结构化缓冲等）
        // ==============================================================

        // access to native underlying graphics API resources (mostly for native code plugins)

        extern public int vertexBufferCount
        {
            [FreeFunction(Name = "MeshScripting::GetVertexBufferCount", HasExplicitThis = true)] get;
        }
        [FreeFunction(Name = "MeshScripting::GetVertexBufferStride", HasExplicitThis = true)]
        public extern int GetVertexBufferStride(int stream);

        [FreeFunction(Name = "MeshScripting::GetNativeVertexBufferPtr", HasExplicitThis = true, ThrowsException = true)]
        extern public IntPtr GetNativeVertexBufferPtr(int index);

        [FreeFunction(Name = "MeshScripting::GetNativeIndexBufferPtr", HasExplicitThis = true)]
        extern public IntPtr GetNativeIndexBufferPtr();

        [FreeFunction(Name = "MeshScripting::GetVertexBufferPtr", HasExplicitThis = true, ThrowsException = true)]
        extern GraphicsBuffer GetVertexBufferImpl(int index);
        [FreeFunction(Name = "MeshScripting::GetIndexBufferPtr", HasExplicitThis = true, ThrowsException = true)]
        extern GraphicsBuffer GetIndexBufferImpl();
        [FreeFunction(Name = "MeshScripting::GetBoneWeightBufferPtr", HasExplicitThis = true, ThrowsException = true)]
        extern GraphicsBuffer GetBoneWeightBufferImpl(int bonesPerVertex);

        [FreeFunction(Name = "MeshScripting::GetBlendShapeBufferPtr", HasExplicitThis = true, ThrowsException = true)]
        extern GraphicsBuffer GetBlendShapeBufferImpl(int layout);

        extern public GraphicsBuffer.Target vertexBufferTarget
        {
            get;
            [FreeFunction(Name = "MeshScripting::SetVertexBufferTarget", HasExplicitThis = true, ThrowsException = true)] set;
        }

        extern public GraphicsBuffer.Target indexBufferTarget
        {
            get;
            [FreeFunction(Name = "MeshScripting::SetIndexBufferTarget", HasExplicitThis = true, ThrowsException = true)] set;
        }

        // ==============================================================
        // 混合形状（Blend Shapes）— 形变动画系统
        //
        // 💡 Blend Shape 存储顶点的偏移量，用于实现面部表情、
        //   肌肉变形等形变动画。每个 Blend Shape 有权重（0~100），
        //   GPU 根据权重在基础形状和目标形状之间插值。
        //
        // 📌 数据结构：
        //   - shapeIndex → shapeName（名称标识，如 "smile"、"blink_L"）
        //   - 每个 Shape 有多个 Frame（关键帧），每个帧有权重
        //   - 每帧存储 deltaVertices / deltaNormals / deltaTangents
        //     （相对于基础网格的顶点偏移、法线偏移、切线偏移）
        //
        // 📌 工作流程：
        //   1. AddBlendShapeFrame() 添加关键帧
        //   2. 运行时通过 SkinnedMeshRenderer.SetBlendShapeWeight() 控制权重
        //   3. GPU 在着色器中根据权重自动插值
        //
        // 📌 典型来源：Maya / Blender 导出的面部动画数据
        // ==============================================================

        // blend shapes

        extern public int blendShapeCount {[NativeMethod(Name = "GetBlendShapeChannelCount")] get; }

        [FreeFunction(Name = "MeshScripting::ClearBlendShapes", HasExplicitThis = true)]
        extern public void ClearBlendShapes();

        [FreeFunction(Name = "MeshScripting::GetBlendShapeName", HasExplicitThis = true, ThrowsException = true)]
        extern public string GetBlendShapeName(int shapeIndex);

        [FreeFunction(Name = "MeshScripting::GetBlendShapeIndex", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetBlendShapeIndex(string blendShapeName);

        [FreeFunction(Name = "MeshScripting::GetBlendShapeFrameCount", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetBlendShapeFrameCount(int shapeIndex);

        [FreeFunction(Name = "MeshScripting::GetBlendShapeFrameWeight", HasExplicitThis = true, ThrowsException = true)]
        extern public float GetBlendShapeFrameWeight(int shapeIndex, int frameIndex);

        [FreeFunction(Name = "GetBlendShapeFrameVerticesFromScript", HasExplicitThis = true, ThrowsException = true)]
        extern public void GetBlendShapeFrameVertices(int shapeIndex, int frameIndex, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents);

        [FreeFunction(Name = "AddBlendShapeFrameFromScript", HasExplicitThis = true, ThrowsException = true)]
        extern public void AddBlendShapeFrame(string shapeName, float frameWeight, ReadOnlySpan<Vector3> deltaVertices, ReadOnlySpan<Vector3> deltaNormals, ReadOnlySpan<Vector3> deltaTangents);
        public void AddBlendShapeFrame(string shapeName, float frameWeight, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents)
            => AddBlendShapeFrame(shapeName, frameWeight, new ReadOnlySpan<Vector3>(deltaVertices), new ReadOnlySpan<Vector3>(deltaNormals), new ReadOnlySpan<Vector3>(deltaTangents));

        [FreeFunction(Name = "MeshScripting::GetBlendShapeOffset", HasExplicitThis = true)]
        extern private BlendShape GetBlendShapeOffsetInternal(int index);

        // ==============================================================
        // 骨骼蒙皮（Skinning）— 骨骼动画绑定数据
        //
        // 💡 蒙皮数据让网格可以跟随骨骼运动，实现角色动画。
        //   每个顶点绑定到若干骨骼，由骨骼变换矩阵驱动顶点位置。
        //
        // 📌 关键数据：
        //   - BoneWeight / BoneWeight1：每个顶点的骨骼权重
        //     （哪些骨骼影响此顶点，影响权重百分比）
        //   - bonesPerVertex：每个顶点关联的骨骼数量
        //   - bindposes：绑定姿势矩阵数组（Mesh 创建时的初始姿势）
        //     每个骨骼一个矩阵，描述从 Mesh 空间到骨骼空间的变换
        //
        // 📌 高性能版本（NativeArray）：
        //   - SetBoneWeights(NativeArray<byte>, NativeArray<BoneWeight1>)
        //   - GetAllBoneWeights() / GetBonesPerVertex()
        //   适用于 Job System 并行处理大量蒙皮数据
        //
        // 📌 GPU 蒙皮缓冲：
        //   GetBoneWeightBuffer(bonesPerVertex) 获取 GPU 端骨骼权重缓冲，
        //   用于 GPU Skinning（在顶点着色器中执行骨骼变换）。
        // ==============================================================

        // skinning

        [NativeMethod("HasBoneWeights")]
        extern private bool HasBoneWeights();
        [FreeFunction(Name = "MeshScripting::GetBoneWeights", HasExplicitThis = true)]
        extern private BoneWeight[] GetBoneWeightsImpl();
        [FreeFunction(Name = "MeshScripting::SetBoneWeights", HasExplicitThis = true)]
        extern private void SetBoneWeightsImpl(BoneWeight[] weights);

        public unsafe void SetBoneWeights(NativeArray<byte> bonesPerVertex, NativeArray<BoneWeight1> weights)
        {
            InternalSetBoneWeights((IntPtr)bonesPerVertex.GetUnsafeReadOnlyPtr(), bonesPerVertex.Length, (IntPtr)weights.GetUnsafeReadOnlyPtr(), weights.Length);
        }

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "MeshScripting::SetBoneWeights", HasExplicitThis = true)]
        extern private void InternalSetBoneWeights(IntPtr bonesPerVertex, int bonesPerVertexSize, IntPtr weights, int weightsSize);

        public unsafe NativeArray<BoneWeight1> GetAllBoneWeights()
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<BoneWeight1>((void*)GetAllBoneWeightsArray(), GetAllBoneWeightsArraySize(), Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetReadOnlySafetyHandle(SafetyHandleIndex.BonesWeightsArray));
            return array;
        }

        public unsafe NativeArray<byte> GetBonesPerVertex()
        {
            int size = HasBoneWeights() ? vertexCount : 0;
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>((void*)GetBonesPerVertexArray(), size, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetReadOnlySafetyHandle(SafetyHandleIndex.BonesPerVertexArray));
            return array;
        }

        [FreeFunction(Name = "MeshScripting::GetAllBoneWeightsArraySize", HasExplicitThis = true)]
        extern private int GetAllBoneWeightsArraySize();

        [NativeMethod("GetBoneWeightBufferDimension")]
        extern int GetBoneWeightBufferLayoutInternal();

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "MeshScripting::GetAllBoneWeightsArray", HasExplicitThis = true)]
        extern private IntPtr GetAllBoneWeightsArray();

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "MeshScripting::GetBonesPerVertexArray", HasExplicitThis = true)]
        extern private IntPtr GetBonesPerVertexArray();

        extern public int bindposeCount { get; }
        [NativeName("BindPosesFromScript")] extern public Matrix4x4[] bindposes { get; set; }

        public unsafe NativeArray<Matrix4x4> GetBindposes()
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>((void*)GetBindposesArray(), bindposeCount, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetReadOnlySafetyHandle(SafetyHandleIndex.BindposeArray));
            return array;
        }

        public unsafe void SetBindposes(NativeArray<Matrix4x4> poses)
        {
            if (!poses.IsCreated || poses.Length == 0)
                throw new ArgumentException("Cannot set bindposes as the native poses array is empty.", "poses");

            SetBindposesFromScript_NativeArray((IntPtr)poses.GetUnsafeReadOnlyPtr(), poses.Length);
        }

        [NativeMethod("SetBindposes")]
        extern private void SetBindposesFromScript_NativeArray(IntPtr posesPtr, int posesCount);

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "MeshScripting::GetBindposesArray", HasExplicitThis = true)]
        extern private IntPtr GetBindposesArray();

        [FreeFunction(Name = "MeshScripting::ExtractBoneWeightsIntoArray", HasExplicitThis = true)]
        extern private void GetBoneWeightsNonAllocImpl([Out] BoneWeight[] values);

        [FreeFunction(Name = "MeshScripting::ExtractBindPosesIntoArray", HasExplicitThis = true)]
        extern private void GetBindposesNonAllocImpl([Out] Matrix4x4[] values);

        private enum SafetyHandleIndex
        {
            // Keep in sync with C++ Mesh::SafetyHandleIndex class
            BonesPerVertexArray,
            BonesWeightsArray,
            BindposeArray,
        }

        [FreeFunction(Name = "MeshScripting::GetReadOnlySafetyHandle", HasExplicitThis = true)]
        extern private AtomicSafetyHandle GetReadOnlySafetyHandle(SafetyHandleIndex index);

        // ==============================================================
        // 网格元信息与 LOD（Mesh Metadata & LOD System）
        //
        // 📌 基本属性：
        //   - isReadable：网格数据是否可从 CPU 读取
        //   - vertexCount：顶点总数
        //   - subMeshCount：子网格数量
        //   - bounds：网格空间包围盒
        //
        // 📌 子网格（SubMesh）：
        //   一个 Mesh 可包含多个子网格，每个子网格使用不同材质渲染。
        //   SubMeshDescriptor 描述每个子网格的拓扑、索引起始和计数。
        //
        // 📌 LOD 系统（Mesh 内嵌 LOD）：
        //   - SetLodCount(n)：设置 LOD 级别数量
        //   - SetLod(subMesh, level, range)：设置某子网格某级别的 LOD 范围
        //   - GetLodSelectionCurve()：获取 LOD 选择曲线
        //   与 LODGroup 组件不同，这是 Mesh 级别的内嵌 LOD。
        //
        // 📌 优化方法：
        //   - Optimize()：自动优化顶点和索引缓冲布局
        //   - OptimizeIndexBuffers()：仅优化索引缓冲
        //   - OptimizeReorderVertexBuffer()：仅优化顶点缓冲
        //   - CombineMeshes()：合并多个 Mesh 减少 Draw Call
        // ==============================================================

        // random things

        extern public bool isReadable
        {
            [NativeMethod("GetIsReadable")] get;
            // Should not be called during playmode
            [NativeMethod("SetIsReadable")] internal set;
        }
        extern internal bool canAccess  {[NativeMethod("CanAccessFromScript")] get; }

        extern public int vertexCount   {[NativeMethod("GetVertexCount")] get; }
        extern public int subMeshCount
        {
            [NativeMethod(Name = "GetSubMeshCount")] get;
            [FreeFunction(Name = "MeshScripting::SetSubMeshCount", HasExplicitThis = true)] set;
        }

        [VisibleToOtherModules("UnityEditor.PhysicsEditorModule")]
        extern internal bool HasPreBakeCollisionMeshInternal(bool isConvex);

        [VisibleToOtherModules("UnityEditor.PhysicsEditorModule")]
        extern internal void SetPreBakeCollisionMeshInternal(bool isConvex, bool preBake);

        [FreeFunction("MeshScripting::SetSubMesh", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetSubMesh(int index, SubMeshDescriptor desc, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default);
        [FreeFunction("MeshScripting::GetSubMesh", HasExplicitThis = true, ThrowsException = true)]
        extern public SubMeshDescriptor GetSubMesh(int index);

        [FreeFunction("MeshScripting::SetAllSubMeshesAtOnceFromArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetAllSubMeshesAtOnceFromArray(SubMeshDescriptor[] desc, int start, int count, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default);
        [FreeFunction("MeshScripting::SetAllSubMeshesAtOnceFromNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetAllSubMeshesAtOnceFromNativeArray(IntPtr desc, int start, int count, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default);

        [FreeFunction("MeshScripting::SetLodCount", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetLodCount(int numLevels);

        [FreeFunction("MeshScripting::SetLodSelectionCurve",  HasExplicitThis = true, ThrowsException = true)]
        extern private void SetLodSelectionCurve(LodSelectionCurve lodSelectionCurve);

        [FreeFunction("MeshScripting::SetLods", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetLodsFromArray(MeshLodRange[] levelRanges, int start, int count, int submesh, MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default);

        [FreeFunction("MeshScripting::SetLodsFromNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetLodsFromNativeArray(IntPtr lodLevels, int count, int submesh, MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default);

        [FreeFunction("MeshScripting::SetLod", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetLodImpl(int subMeshIndex, int level, MeshLodRange levelRange, MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default);

        [FreeFunction("MeshScripting::GetLods", HasExplicitThis = true, ThrowsException = true)]
        extern private MeshLodRange[] GetLodsAlloc(int subMeshIndex);

        [FreeFunction(Name = "MeshScripting::GetLodsNonAlloc", HasExplicitThis = true, ThrowsException = true)]
        extern private void GetLodsNonAlloc([Out] MeshLodRange[] levels, int subMeshIndex);

        [FreeFunction("MeshScripting::GetLodCount", HasExplicitThis = true)]
        extern private int GetLodCount();

        [FreeFunction("MeshScripting::GetLodSelectionCurve", HasExplicitThis = true)]
        extern private LodSelectionCurve GetLodSelectionCurve();

        [FreeFunction("MeshScripting::GetLod", HasExplicitThis = true, ThrowsException = true)]
        extern public MeshLodRange GetLod(int subMeshIndex, int levelIndex);

        extern public Bounds bounds { get; set; }

        [NativeMethod("Clear")]                 extern private void ClearImpl(bool keepVertexLayout);
        [NativeMethod("RecalculateBounds")]     extern private void RecalculateBoundsImpl(UnityEngine.Rendering.MeshUpdateFlags flags);
        [NativeMethod("RecalculateNormals")]    extern private void RecalculateNormalsImpl(UnityEngine.Rendering.MeshUpdateFlags flags);
        [NativeMethod("RecalculateTangents")]   extern private void RecalculateTangentsImpl(UnityEngine.Rendering.MeshUpdateFlags flags);
        [NativeMethod("MarkDynamic")]           extern private void MarkDynamicImpl();
        [NativeMethod("MarkModified")]          extern public  void MarkModified();
        [NativeMethod("UploadMeshData")]        extern private void UploadMeshDataImpl(bool markNoLongerReadable);

        [FreeFunction(Name = "MeshScripting::GetPrimitiveType", HasExplicitThis = true)]
        extern private MeshTopology GetTopologyImpl(int submesh);

        [NativeMethod("RecalculateMeshMetric")] extern private void RecalculateUVDistributionMetricImpl(int uvSetIndex, float uvAreaThreshold);
        [NativeMethod("RecalculateMeshMetrics")] extern private void RecalculateUVDistributionMetricsImpl(float uvAreaThreshold);
        [NativeMethod("GetMeshMetric")] extern public float GetUVDistributionMetric(int uvSetIndex);

        [NativeMethod(Name = "MeshScripting::CombineMeshes", IsFreeFunction = true, ThrowsException = true, HasExplicitThis = true)]
        extern private void CombineMeshesImpl(CombineInstance[] combine, bool mergeSubMeshes, bool useMatrices, bool hasLightmapData);

        [NativeMethod("Optimize")]                    extern private void OptimizeImpl();
        [NativeMethod("OptimizeIndexBuffers")]        extern private void OptimizeIndexBuffersImpl();
        [NativeMethod("OptimizeReorderVertexBuffer")] extern private void OptimizeReorderVertexBufferImpl();
    }

    // ==============================================================
    // StaticBatchingHelper — 静态批处理辅助结构
    //
    // 💡 静态批处理（Static Batching）是 Unity 的渲染优化技术：
    //   将多个标记为 Static 的 GameObject 的 Mesh 合并成一个大 Mesh，
    //   减少 Draw Call 数量，提升渲染性能。
    //
    // 📌 工作流程：
    //   1. 在 Inspector 中勾选 Static 标记
    //   2. 构建时自动调用 CombineMeshesForStaticBatching
    //   3. 合并后的 Mesh 在运行时直接渲染
    //
    // ⚠️ 静态批处理的 Mesh 会占用更多内存（存储合并后的副本），
    //   且合并后的对象无法再移动（否则会穿帮）。
    // ==============================================================
    [NativeHeader("Runtime/Graphics/Mesh/StaticBatching.h")]
    internal struct StaticBatchingHelper
    {
        [FreeFunction("StaticBatching::CombineMeshesForStaticBatching")]
        extern internal static void CombineMeshes(GameObject[] gos, GameObject staticBatchRoot);
    }
}
