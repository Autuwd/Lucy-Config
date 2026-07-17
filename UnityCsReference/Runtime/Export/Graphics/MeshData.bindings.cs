// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 MeshData — Mesh 的只读视图（线程安全）
//
// 📌 作用：
//   MeshData 是 Mesh 几何数据的只读快照，设计用于 Job System 中
//   并行读取网格数据，而不会产生竞态条件。
//   与 Mesh 不同，MeshData 不持有 C++ 对象的所有权。
//
// 🏗 核心概念：
//   - MeshData：单个 Mesh 的只读数据视图
//   - MeshDataArray：多个 MeshData 的批量获取句柄
//
// 💡 与 Mesh 的对比：
//   ┌─────────────┬──────────────────┬──────────────────┐
//   │   特性       │     Mesh         │    MeshData      │
//   ├─────────────┼──────────────────┼──────────────────┤
//   │ 读取数据     │ ✅（需 isReadable）│ ✅（始终可读）    │
//   │ 写入数据     │ ✅               │ ❌（只读）        │
//   │ 线程安全     │ ❌（单线程）      │ ✅（多线程安全）   │
//   │ 持有所有权   │ ✅               │ ❌（临时视图）     │
//   │ GPU 数据访问 │ ✅               │ ❌               │
//   └─────────────┴──────────────────┴──────────────────┘
//
// 📌 典型使用场景：
//   1. 在 Job System 中并行读取多个 Mesh 的顶点/索引数据
//   2. 在后台线程计算 Mesh 的包围盒、LOD 信息
//   3. 高性能的 Mesh 数据分析和处理
//
// ⚡ 性能提示：
//   - AcquireReadOnlyMeshData：获取只读视图（零拷贝）
//   - AcquireMeshDataCopy：获取数据副本（较慢但可修改）
//   - 使用完后必须调用 Release（由 Dispose 自动处理）
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    public sealed partial class Mesh
    {
        // ==============================================================
        // MeshData — 单个网格的只读数据视图
        //
        // 🎯 所有方法均为线程安全（IsThreadSafe = true），
        //    可在 Job System 的 IJob 中安全调用。
        //
        // 🔑 关键方法：
        //   - HasVertexAttribute / GetVertexAttributeDimension：查询顶点属性
        //   - GetVertexCount / GetVertexBufferCount：获取顶点数量和流数量
        //   - GetVertexDataPtr / GetVertexDataSize：访问原始顶点数据
        //   - GetIndexFormat / GetIndexCount / GetIndexDataPtr：索引数据
        //   - GetSubMeshCount / GetSubMesh：子网格信息
        //   - CopyAttributeIntoPtr / CopyIndicesIntoPtr：批量拷贝数据
        //
        // 📌 LOD 支持：
        //   - GetLodCount / SetLodCount：LOD 级别数量
        //   - GetLod / SetLod：获取/设置 LOD 范围
        //   - GetLodSelectionCurve / SetLodSelectionCurve：LOD 选择曲线
        //
        // 💡 注意：MeshData 是值类型（struct），通过 IntPtr 传递给 C++。
        //    不要缓存过期的 MeshData 引用。
        // ==============================================================
        [StaticAccessor("MeshDataBindings", StaticAccessorType.DoubleColon)]
        [NativeHeader("Runtime/Graphics/Mesh/MeshScriptBindings.h")]
        public partial struct MeshData
        {
            [NativeMethod(IsThreadSafe = true)] static extern bool HasVertexAttribute(IntPtr self, VertexAttribute attr);
            [NativeMethod(IsThreadSafe = true)] static extern int GetVertexAttributeDimension(IntPtr self, VertexAttribute attr);
            [NativeMethod(IsThreadSafe = true)] static extern VertexAttributeFormat GetVertexAttributeFormat(IntPtr self, VertexAttribute attr);
            [NativeMethod(IsThreadSafe = true)] static extern int GetVertexAttributeStream(IntPtr self, VertexAttribute attr);
            [NativeMethod(IsThreadSafe = true)] static extern int GetVertexAttributeOffset(IntPtr self, VertexAttribute attr);

            [NativeMethod(IsThreadSafe = true)] static extern int GetVertexCount(IntPtr self);
            [NativeMethod(IsThreadSafe = true)] static extern int GetVertexBufferCount(IntPtr self);

            [NativeMethod(IsThreadSafe = true)] static extern IntPtr GetVertexDataPtr(IntPtr self, int stream);
            [NativeMethod(IsThreadSafe = true)] static extern ulong GetVertexDataSize(IntPtr self, int stream);
            [NativeMethod(IsThreadSafe = true)] static extern int GetVertexBufferStride(IntPtr self, int stream);

            [NativeMethod(IsThreadSafe = true)] static extern void CopyAttributeIntoPtr(IntPtr self, VertexAttribute attr, VertexAttributeFormat format, int dim, IntPtr dst);
            [NativeMethod(IsThreadSafe = true)] static extern void CopyIndicesIntoPtr(IntPtr self, int submesh, int meshLod, bool applyBaseVertex, int dstStride, IntPtr dst);

            [NativeMethod(IsThreadSafe = true)] static extern IndexFormat GetIndexFormat(IntPtr self);
            [NativeMethod(IsThreadSafe = true)] static extern int GetIndexCount(IntPtr self, int submesh, int meshlod);

            [NativeMethod(IsThreadSafe = true)] static extern IntPtr GetIndexDataPtr(IntPtr self);
            [NativeMethod(IsThreadSafe = true)] static extern ulong GetIndexDataSize(IntPtr self);

            [NativeMethod(IsThreadSafe = true)] static extern int GetSubMeshCount(IntPtr self);

            [NativeMethod(IsThreadSafe = true)] static extern int GetLodCount(IntPtr self);
            [NativeMethod(IsThreadSafe = true)] static extern void SetLodCount(IntPtr self, int count);
            [NativeMethod(IsThreadSafe = true)] static extern LodSelectionCurve GetLodSelectionCurve(IntPtr self);
            [NativeMethod(IsThreadSafe = true, ThrowsException = true)] static extern void SetLodSelectionCurve(IntPtr self, LodSelectionCurve lodSelectionCurve);
            [NativeMethod(IsThreadSafe = true)] static extern MeshLodRange GetLod(IntPtr self, int submesh, int level);
            [NativeMethod(IsThreadSafe = true, ThrowsException = true)] static extern void SetLod(IntPtr self, int submesh, int level, MeshLodRange levelRange, MeshUpdateFlags flags);
            [NativeMethod(IsThreadSafe = true, ThrowsException = true)] static extern SubMeshDescriptor GetSubMesh(IntPtr self, int index);
            [NativeMethod(IsThreadSafe = true, ThrowsException = true)] static extern void SetVertexBufferParamsFromPtr(IntPtr self, int vertexCount, IntPtr attributesPtr, int attributesCount);
            [NativeMethod(IsThreadSafe = true, ThrowsException = true)] static extern void SetVertexBufferParamsFromArray(IntPtr self, int vertexCount, params VertexAttributeDescriptor[] attributes);
            [NativeMethod(IsThreadSafe = true, ThrowsException = true)] static extern void SetIndexBufferParamsImpl(IntPtr self, int indexCount, IndexFormat indexFormat);
            [NativeMethod(IsThreadSafe = true)] static extern void SetSubMeshCount(IntPtr self, int count);
            [NativeMethod(IsThreadSafe = true, ThrowsException = true)] static extern void SetSubMeshImpl(IntPtr self, int index, SubMeshDescriptor desc, MeshUpdateFlags flags);
        }

        // ==============================================================
        // MeshDataArray — 批量获取 MeshData 的句柄
        //
        // 🎯 一次性获取多个 Mesh 的只读数据视图，适合批量处理：
        //
        // 📌 获取方式：
        //   - AcquireReadOnlyMeshData(mesh)：获取单个 Mesh 的只读视图
        //   - AcquireReadOnlyMeshDatas(meshes[])：批量获取多个 Mesh
        //   - AcquireMeshDataCopy(mesh)：获取可修改的副本
        //   - AcquireMeshDatasCopy(meshes[])：批量获取可修改的副本
        //
        // 📌 修改和应用（仅限 Copy 版本）：
        //   - CreateNewMeshDatas(count)：创建新的空 MeshData
        //   - ApplyToMesh(mesh)：将修改后的数据应用回 Mesh
        //   - ApplyToMeshes(meshes[])：批量应用
        //
        // 💡 使用模式：
        //   using (var dataArray = Mesh.AcquireReadOnlyMeshData(meshes))
        //   {
        //       // 在 Job System 中并行处理
        //       // dataArray[i] 获取第 i 个 Mesh 的 MeshData
        //   }  // 自动 Release
        //
        // ⚠️ 必须在 using 块或显式 Dispose 中释放，否则会内存泄漏。
        // ==============================================================
        [StaticAccessor("MeshDataArrayBindings", StaticAccessorType.DoubleColon)]
        public partial struct MeshDataArray
        {
            static extern unsafe void AcquireReadOnlyMeshData([NotNull] Mesh mesh, IntPtr* datas);
            static extern unsafe void AcquireReadOnlyMeshDatas([NotNull] Mesh[] meshes, IntPtr* datas, int count);

            static extern unsafe void AcquireMeshDataCopy([NotNull] Mesh mesh, IntPtr* datas);
            static extern unsafe void AcquireMeshDatasCopy([NotNull] Mesh[] meshes, IntPtr* datas, int count);

            static extern unsafe void ReleaseMeshDatas(IntPtr* datas, int count);

            static extern unsafe void CreateNewMeshDatas(IntPtr* datas, int count);
            [NativeMethod(ThrowsException = true)] static extern unsafe void ApplyToMeshesImpl([NotNull] Mesh[] meshes, IntPtr* datas, int count, MeshUpdateFlags flags);
            [NativeMethod(ThrowsException = true)] static extern void ApplyToMeshImpl([NotNull] Mesh mesh, IntPtr data, MeshUpdateFlags flags);
        }
    }
}
