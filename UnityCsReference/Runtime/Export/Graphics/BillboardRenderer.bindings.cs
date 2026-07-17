// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 BillboardRenderer — 公告板渲染系统
//
// 📌 作用：
//   公告板（Billboard）是一种始终面向摄像机的 2D 精灵渲染技术，
//   常用于树木、草地、粒子、远景装饰等，避免使用高面数 3D 模型。
//
// 📌 本文件包含两个类：
//   - BillboardAsset：公告板资源（定义顶点、UV、索引等几何数据）
//   - BillboardRenderer：公告板渲染器（组件，挂载到 GameObject 上渲染）
//
// 💡 BillboardAsset 数据结构：
//   - 图像纹理坐标（ImageTexCoords）：UV 矩形，指定每帧使用的纹理区域
//   - 顶点（Vertices）：公告板的 2D 顶点坐标
//   - 索引（Indices）：三角面索引（UInt16）
//   - 宽度/高度/底部偏移：公告板的尺寸参数
//   - 材质（Material）：渲染用材质
//
// 📌 使用方式：
//   1. 在 Project 窗口右键 → Create → Billboard Asset
//   2. 设置宽度、高度、底部偏移
//   3. 分配材质和纹理
//   4. 在场景中创建空 GameObject → 添加 BillboardRenderer 组件
//   5. 将 BillboardAsset 拖到 billboard 属性
//
// ⚡ 性能提示：
//   - 公告板比低模 3D 树木节省 10-100 倍多边形
//   - 适合远景 LOD 和大面积植被
//   - Unity 2D Sprite 路径也可实现类似效果
//
// 📍 对应 C++ 头文件：
//   - Runtime/Graphics/Billboard/BillboardAsset.h
//   - Runtime/Graphics/Billboard/BillboardRenderer.h
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // BillboardAsset — 公告板资源
    //
    // 🎯 继承链：BillboardAsset → Object（UnityEngine）
    //
    // 🔑 关键属性：
    //   - width / height / bottom：尺寸参数
    //   - imageCount / vertexCount / indexCount：只读，几何数据计数
    //   - material：渲染材质
    //
    // 🔑 关键方法：
    //   - GetImageTexCoords()：获取图像纹理坐标
    //   - SetImageTexCoords()：设置图像纹理坐标
    //   - GetVertices() / SetVertices()：获取/设置顶点
    //   - GetIndices() / SetIndices()：获取/设置索引
    //
    // 📌 API 设计模式：
    //   每种数据都提供 List<T> 版本和 T[]/Span 版本，
    //   List<T> 版本用于编辑器便利，Span 版本用于高性能。
    //   内部通过 NoAllocHelpers.CreateReadOnlySpan 桥接。
    // ==============================================================

    /// Represents a billboard
    [NativeHeader("Runtime/Graphics/Billboard/BillboardAsset.h")]
    [NativeHeader("Runtime/Export/Graphics/BillboardRenderer.bindings.h")]
    public sealed class BillboardAsset : Object
    {
        public BillboardAsset()
        {
            Internal_Create(this);
        }

        [FreeFunction(Name = "BillboardRenderer_Bindings::Internal_Create")]
        extern private static void Internal_Create([Writable] BillboardAsset obj);

        extern public float width { get; set; }
        extern public float height { get; set; }
        extern public float bottom { get; set; }

        extern public int imageCount
        {
            [NativeMethod("GetNumImages")]
            get;
        }

        extern public int vertexCount
        {
            [NativeMethod("GetNumVertices")]
            get;
        }

        extern public int indexCount
        {
            [NativeMethod("GetNumIndices")]
            get;
        }

        extern public Material material { get; set; }

        // List<T> version
        public void GetImageTexCoords(List<Vector4> imageTexCoords)
        {
            if (imageTexCoords == null)
                throw new ArgumentNullException("imageTexCoords");

            GetImageTexCoordsInternal(imageTexCoords);
        }

        // T[] version
        [NativeMethod("GetBillboardDataReadonly().GetImageTexCoords")]
        extern public Vector4[] GetImageTexCoords();

        [FreeFunction(Name = "BillboardRenderer_Bindings::GetImageTexCoordsInternal", HasExplicitThis = true)]
        extern internal void GetImageTexCoordsInternal(List<Vector4> list);

        // List<T> version
        public void SetImageTexCoords(List<Vector4> imageTexCoords)
        {
            if (imageTexCoords == null)
                throw new ArgumentNullException("imageTexCoords");

            SetImageTexCoords(NoAllocHelpers.CreateReadOnlySpan(imageTexCoords));
        }

        // T[] version
        public void SetImageTexCoords(Vector4[] imageTexCoords)
        {
            if (imageTexCoords == null)
                throw new ArgumentNullException("imageTexCoords");

            SetImageTexCoords(imageTexCoords.AsSpan());
        }

        [FreeFunction(Name = "BillboardRenderer_Bindings::SetImageTexCoords", HasExplicitThis = true)]
        extern void SetImageTexCoords(ReadOnlySpan<Vector4> imageTexCoords);

        // List<T> version
        public void GetVertices(List<Vector2> vertices)
        {
            if (vertices == null)
                throw new ArgumentNullException("vertices");

            GetVerticesInternal(vertices);
        }

        // T[] version
        [NativeMethod("GetBillboardDataReadonly().GetVertices")]
        extern public Vector2[] GetVertices();

        [FreeFunction(Name = "BillboardRenderer_Bindings::GetVerticesInternal", HasExplicitThis = true)]
        extern internal void GetVerticesInternal(List<Vector2> list);

        // List<T> version
        public void SetVertices(List<Vector2> vertices)
        {
            if (vertices == null)
                throw new ArgumentNullException("vertices");

            SetVertices(NoAllocHelpers.CreateReadOnlySpan(vertices));
        }

        // T[] version
        public void SetVertices(Vector2[] vertices)
        {
            if (vertices == null)
                throw new ArgumentNullException("vertices");

            SetVertices(vertices.AsSpan());
        }

        [FreeFunction(Name = "BillboardRenderer_Bindings::SetVertices", HasExplicitThis = true)]
        extern void SetVertices(ReadOnlySpan<Vector2> vertices);

        // List<T> version
        public void GetIndices(List<UInt16> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            GetIndicesInternal(indices);
        }

        // T[] version
        [NativeMethod("GetBillboardDataReadonly().GetIndices")]
        extern public UInt16[] GetIndices();

        [FreeFunction(Name = "BillboardRenderer_Bindings::GetIndicesInternal", HasExplicitThis = true)]
        extern internal void GetIndicesInternal(List<UInt16> list);

        // List<T> version
        public void SetIndices(List<UInt16> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            SetIndices(NoAllocHelpers.CreateReadOnlySpan(indices));
        }

        // T[] version
        public void SetIndices(UInt16[] indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            SetIndices(indices.AsSpan());
        }

        [FreeFunction(Name = "BillboardRenderer_Bindings::SetIndices", HasExplicitThis = true)]
        extern void SetIndices(ReadOnlySpan<UInt16> indices);

        [FreeFunction(Name = "BillboardRenderer_Bindings::MakeMaterialProperties", HasExplicitThis = true)]
        extern internal void MakeMaterialProperties(MaterialPropertyBlock properties, Camera camera);
    }

    // ==============================================================
    // BillboardRenderer — 公告板渲染器组件
    //
    // 🎯 继承链：BillboardRenderer → Renderer → Component → Object
    //
    // 🔑 关键属性：
    //   - billboard：关联的 BillboardAsset 资源
    //
    // 📌 运行时行为：
    //   每帧根据摄像机朝向自动旋转 Billboard 面向摄像机，
    //   使用 BillboardAsset 中的顶点和 UV 渲染 2D 精灵。
    //   可实现树木/草地的风摆动画（通过切换纹理帧）。
    // ==============================================================

    /// Renders a billboard.
    [NativeHeader("Runtime/Graphics/Billboard/BillboardRenderer.h")]
    public sealed class BillboardRenderer : Renderer
    {
        extern public BillboardAsset billboard { get; set; }
    }
}
