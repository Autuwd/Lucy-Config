// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    // =====================================================
    // CanvasRenderer — 画布渲染器组件
    // =====================================================
    // CanvasRenderer 是每个 UI 元素（Graphic）都会自动获取的组件，
    // 负责将 UI 元素的网格数据提交给 Canvas 进行合批渲染。
    //
    // 核心职责：
    // 1. 管理 UI 元素的 Mesh（网格）—— SetMesh/GetMesh
    // 2. 管理材质和纹理—— SetMaterial/SetTexture/SetAlphaTexture
    // 3. 控制渲染颜色—— SetColor/GetColor/SetAlpha
    // 4. 管理矩形裁剪—— EnableRectClipping/DisableRectClipping
    // 5. 提供渲染排序信息—— absoluteDepth/relativeDepth
    //
    // 与 Canvas 的关系：
    // - CanvasRenderer 直接和 C++ 层的 Canvas 交互
    // - Canvas 负责收集所有子 CanvasRenderer 的网格进行合批
    // - 同一 Canvas 下的所有 CanvasRenderer 共享渲染批次
    //
    // 与 Graphic 的关系：
    // - Graphic 是 UI 逻辑层（颜色、射线检测等）
    // - CanvasRenderer 是渲染层（网格、材质、纹理）
    // - Graphic 通过 canvasRenderer 属性访问对应的 CanvasRenderer
    // - 当 Graphic 标记为脏时，会触发 CanvasRenderer 的网格更新
    //
    // 裁剪系统：
    // - EnableRectClipping 启用矩形裁剪，常用于 Mask 组件
    // - 裁剪软度（clippingSoftness）控制裁剪边缘的柔化程度
    // - hasRectClipping 指示当前是否启用裁剪
    //
    // 材质栈（Material Stack）：
    // - 支持多个材质的堆叠渲染（如遮罩效果）
    // - materialCount / popMaterialCount 管理材质栈的大小
    // - SetPopMaterial 设置弹出材质（用于遮罩反转）
    // =====================================================

    [NativeClass("UI::CanvasRenderer"),
     NativeHeader("Modules/UI/CanvasRenderer.h")]
    [UIModuleHelpURL("class-CanvasRenderer")]
    public sealed partial class CanvasRenderer : Component
    {
        // ============================================================
        // 属性
        // ============================================================

        /// <summary>
        /// 是否有弹出指令（Pop Instruction）。
        /// 在材质栈中使用，标记此渲染器需要执行弹出操作。
        /// 常用于 UI 遮罩系统。
        /// </summary>
        public extern bool hasPopInstruction { get; set; }

        /// <summary>
        /// 材质栈中的材质数量。
        /// 默认值为 1。当使用多材质渲染（如 Stencil 遮罩）时增加。
        /// </summary>
        public extern int materialCount { get; set; }

        /// <summary>
        /// 弹出材质的数量。
        /// 在材质栈中，弹出材质用于还原之前的渲染状态（如恢复模板缓冲）。
        /// </summary>
        public extern int popMaterialCount { get; set; }

        /// <summary>
        /// 绝对深度值（只读）。
        /// 表示此渲染器在 Canvas 层级的全局深度，值越大渲染越靠前。
        /// 由 Canvas 系统自动计算，考虑所有嵌套关系。
        /// </summary>
        public extern int absoluteDepth { get; }

        /// <summary>
        /// 渲染器是否已移动（自上次渲染后）。
        /// Canvas 系统使用此标记判断是否需要重新合批此渲染器。
        /// 为 true 时表示需要重新计算提交数据。
        /// </summary>
        public extern bool hasMoved { get; }

        /// <summary>
        /// 是否剔除透明网格（Cull Transparent Mesh）。
        /// 开启后，完全透明的网格不会提交渲染，可优化性能。
        /// 适用于有透明度变化的 UI 元素（如淡入淡出动画）。
        /// </summary>
        public extern bool cullTransparentMesh { get; set; }

        /// <summary>
        /// 是否有矩形裁剪（只读）。
        /// 当通过 EnableRectClipping 启用裁剪后，此属性为 true。
        /// 用于判断当前渲染器是否被裁剪区域限制。
        /// </summary>
        [NativeProperty("RectClipping", false, TargetType.Function)] public extern bool hasRectClipping { get; }

        /// <summary>
        /// 相对深度值（只读）。
        /// 表示此渲染器在兄弟 CanvasRenderer 中的相对渲染顺序。
        /// 与 absoluteDepth 不同，此值仅在同一 Canvas 内有效。
        /// 值越大渲染越靠前。
        /// </summary>
        [NativeProperty("Depth", false, TargetType.Function)] public extern int relativeDepth { get; }

        /// <summary>
        /// 是否应被剔除（不渲染）。
        /// 当设置为 true 时，Canvas 会跳过此渲染器的渲染。
        /// 用于实现 UI 元素的显示/隐藏而不销毁 GameObject。
        /// 注意：与 Graphic.enabled 不同，此属性仅控制渲染，不影响布局。
        /// </summary>
        [NativeProperty("ShouldCull", false, TargetType.Function)] public extern bool cull { get; set; }

        [Obsolete("isMask is no longer supported.See EnableClipping for vertex clipping configuration", false)]
        public bool isMask { get; set; }

        // ============================================================
        // 颜色控制
        // ============================================================

        /// <summary>设置渲染颜色（与 Graphic.color 同步）</summary>
        public extern void SetColor(Color color);
        /// <summary>获取当前渲染颜色</summary>
        public extern Color GetColor();

        // ============================================================
        // 矩形裁剪系统
        // ============================================================

        /// <summary>
        /// 启用矩形裁剪。
        /// 只渲染指定矩形区域内的部分，矩形外的部分被裁剪掉。
        /// 常用于 Mask（遮罩）组件的实现。
        /// </summary>
        /// <param name="rect">裁剪矩形（Canvas 空间中的矩形）</param>
        public extern void EnableRectClipping(Rect rect);

        /// <summary>
        /// 裁剪边缘的柔化度。
        /// 控制裁剪边界的羽化/柔化程度。值越大边缘过渡越柔和。
        /// X = 水平柔化度，Y = 垂直柔化度（单位：像素）。
        /// </summary>
        public extern Vector2 clippingSoftness { get; set; }

        /// <summary>禁用矩形裁剪，恢复完整渲染区域</summary>
        public extern void DisableRectClipping();

        // ============================================================
        // 材质管理
        // ============================================================

        /// <summary>设置指定索引位置的材质（材质栈）</summary>
        public extern void SetMaterial(Material material, int index);
        /// <summary>获取指定索引位置的材质</summary>
        public extern Material GetMaterial(int index);
        /// <summary>设置指定索引位置的弹出材质（用于遮罩还原）</summary>
        public extern void SetPopMaterial(Material material, int index);
        /// <summary>获取指定索引位置的弹出材质</summary>
        public extern Material GetPopMaterial(int index);

        // ============================================================
        // 纹理管理
        // ============================================================

        /// <summary>设置主纹理</summary>
        public extern void SetTexture(Texture texture);
        /// <summary>获取次要纹理的数量</summary>
        public extern int GetSecondaryTextureCount();
        /// <summary>设置次要纹理的数量</summary>
        public extern void SetSecondaryTextureCount(int size);
        /// <summary>获取指定索引的次要纹理名称</summary>
        public extern string GetSecondaryTextureName(int index);
        /// <summary>获取指定索引的次要纹理</summary>
        public extern Texture2D GetSecondaryTexture(int index);
        /// <summary>设置指定索引的次要纹理（名称 + 纹理）</summary>
        public extern void SetSecondaryTexture(int index, string name, Texture2D texture);
        /// <summary>设置 Alpha 纹理（灰度纹理控制透明度通道）</summary>
        public extern void SetAlphaTexture(Texture texture);

        // ============================================================
        // 网格管理
        // ============================================================

        /// <summary>
        /// 设置渲染网格。
        /// UI 元素通过此方法将生成的顶点数据提交给渲染系统。
        /// Graphic 的 OnPopulateMesh 方法生成网格后，通过此方法提交。
        /// </summary>
        public extern void SetMesh(Mesh mesh);
        /// <summary>获取当前渲染网格</summary>
        public extern Mesh GetMesh();

        /// <summary>
        /// 清除渲染数据。
        /// 重置 CanvasRenderer 的状态，清除颜色、材质、纹理和网格。
        /// 在 UI 元素被销毁时自动调用。
        /// </summary>
        public extern void Clear();

        // ============================================================
        // Alpha 通道便捷方法
        // ============================================================

        /// <summary>
        /// 获取当前 Alpha 值（从颜色中提取）。
        /// 注意：此为实例方法而非属性，通过 GetColor().a 实现。
        /// </summary>
        public float GetAlpha()
        {
            return GetColor().a;
        }

        /// <summary>
        /// 设置 Alpha 值。
        /// 保持颜色 RGB 不变，只修改 A 通道。
        /// 常用于透明度动画（如渐入渐出效果）。
        /// </summary>
        public void SetAlpha(float alpha)
        {
            var color = GetColor();
            color.a = alpha;
            SetColor(color);
        }

        /// <summary>
        /// 获取继承的 Alpha 值。
        /// 考虑了父级 CanvasGroup 的叠加透明度。
        /// 用于计算 UI 元素最终显示的实际透明度。
        /// </summary>
        public extern float GetInheritedAlpha();

        /// <summary>
        /// 便捷方法：同时设置材质和纹理。
        /// 内部先确保 materialCount 至少为 1，然后设置索引 0 的材质和纹理。
        /// 等价于 SetMaterial(material, 0) + SetTexture(texture)。
        /// </summary>
        public void SetMaterial(Material material, Texture texture)
        {
            materialCount = Math.Max(1, materialCount);
            SetMaterial(material, 0);
            SetTexture(texture);
        }

        /// <summary>
        /// 便捷方法：获取索引 0 的材质。
        /// 等价于 GetMaterial(0)。
        /// </summary>
        public Material GetMaterial()
        {
            return GetMaterial(0);
        }

        // ============================================================
        // 顶点流拆分/合并静态方法
        // ============================================================
        // 以下方法用于在 UIVertex（大结构体）和分离的数组流之间转换。
        //
        // UIVertex 包含了 position/color/uv/normal/tangent 等所有顶点数据，
        // 而 Mesh 类内部将这些数据存储为分离的数组。
        //
        // SplitUIVertexStreams — 将 UIVertex 列表拆分为分离的流（用于设置 Mesh）
        // CreateUIVertexStream — 从分离的流创建 UIVertex 列表（反向操作）
        // AddUIVertexStream    — 将分离的流数据追加到 UIVertex 列表
        //
        // 这些方法在 Graphic 的网格重建过程中被调用，
        // 用于在 managed 端的 UIVertex 和 native 端的 Mesh 数据格式之间转换。
        // 使用 NoAllocHelpers 优化以减少 GC 分配。
        //
        // 多个重载版本提供不同的兼容性级别：
        // - 基础版本：只有 UV0、UV1
        // - 扩展版本：支持 UV2、UV3
        // - 完整版本：支持 prevPositions（用于运动向量/顶点动画）
        // ============================================================

        /// <summary>拆分 UIVertex 流为基础版本（支持 UV0/UV1）。</summary>
        public static void SplitUIVertexStreams(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S,
            List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            SplitUIVertexStreams(verts, positions, colors, uv0S, uv1S, new List<Vector4>(), new List<Vector4>(), normals, tangents, new List<Vector4>(), indices);
        }

        /// <summary>拆分 UIVertex 流为扩展版本（支持 UV0/UV1/UV2/UV3）。</summary>
        public static void SplitUIVertexStreams(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S,
            List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            SplitUIVertexStreams(verts, positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, new List<Vector4>(), indices);
        }

        /// <summary>
        /// 拆分 UIVertex 流为完整版本（支持 UV0-UV3 + prevPositions）。
        /// 先调用 Native 方法拆分顶点属性，再通过 SplitIndicesStreamsInternal 生成索引。
        /// </summary>
        public static void SplitUIVertexStreams(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S,
            List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<Vector4> prevPositions, List<int> indices)
        {
            SplitUIVertexStreamsInternal(NoAllocHelpers.CreateReadOnlySpan(verts), positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, prevPositions);
            SplitIndicesStreamsInternal(verts, indices);
        }

        /// <summary>
        /// 从分离的流创建 UIVertex 列表（基础版本，仅 UV0/UV1）。
        /// 缺省的 UV2/UV3/prevPositions 用零值填充。
        /// </summary>
        public static void CreateUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            var defaultValues = new List<Vector4>();
            NoAllocHelpers.EnsureListElemCount(defaultValues, positions.Count);
            CreateUIVertexStream(verts, positions, colors, uv0S, uv1S, defaultValues, defaultValues, normals, tangents, defaultValues, indices);
        }

        /// <summary>
        /// 从分离的流创建 UIVertex 列表（扩展版本，支持 UV0-UV3）。
        /// 缺省的 prevPositions 用零值填充。
        /// </summary>
        public static void CreateUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            var defaultValues = new List<Vector4>();
            NoAllocHelpers.EnsureListElemCount(defaultValues, positions.Count);
            CreateUIVertexStream(verts, positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, defaultValues, indices);
        }

        /// <summary>
        /// 从分离的流创建 UIVertex 列表（完整版本）。
        /// 这是最终的真实实现，所有其他重载最终都会调用此版本。
        /// 使用 NoAllocHelpers.CreateReadOnlySpan 来避免内存分配。
        /// </summary>
        public static void CreateUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<Vector4> prevPositions, List<int> indices)
        {
            CreateUIVertexStreamInternal(verts, NoAllocHelpers.CreateReadOnlySpan(positions), NoAllocHelpers.CreateReadOnlySpan(colors),
                NoAllocHelpers.CreateReadOnlySpan(uv0S), NoAllocHelpers.CreateReadOnlySpan(uv1S), NoAllocHelpers.CreateReadOnlySpan(uv2S),
                NoAllocHelpers.CreateReadOnlySpan(uv3S), NoAllocHelpers.CreateReadOnlySpan(normals), NoAllocHelpers.CreateReadOnlySpan(tangents),
                NoAllocHelpers.CreateReadOnlySpan(prevPositions), NoAllocHelpers.CreateReadOnlySpan(indices));
        }

        /// <summary>
        /// 将 UIVertex 数据拆分到分离流中（基础版本）。
        /// 注意：此方法虽然名为 Add，但实际上是在进行拆分操作。
        /// </summary>
        public static void AddUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector3> normals, List<Vector4> tangents)
        {
            AddUIVertexStream(verts, positions, colors, uv0S, uv1S, new List<Vector4>(), new List<Vector4>(), normals, tangents, new List<Vector4>());
        }

        /// <summary>
        /// 将 UIVertex 数据拆分到分离流中（扩展版本，支持 UV0-UV3）。
        /// </summary>
        public static void AddUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents)
        {
            AddUIVertexStream(verts, positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, new List<Vector4>());
        }

        /// <summary>
        /// 将 UIVertex 数据拆分到分离流中（完整版本）。
        /// 实际调用 SplitUIVertexStreamsInternal Native 方法执行拆分。
        /// </summary>
        public static void AddUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<Vector4> prevPositions)
        {
            SplitUIVertexStreamsInternal(NoAllocHelpers.CreateReadOnlySpan(verts), positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, prevPositions);
        }

        // ============================================================
        // 已过时的 SetVertices 方法（兼容旧代码）
        // ============================================================
        // 在旧版 UI 系统中，SetVertices 直接接收 UIVertex 数组。
        // 新版系统使用 SetMesh(Mesh) 代替，因为 Mesh 的顶点流格式
        // 与 UIVertex 的大结构体格式不同。
        //
        // 向后兼容实现：将 UIVertex 数组转换为 Mesh，再调用 SetMesh。
        // 每 4 个顶点构成一个四边形（2 个三角形）。
        // 注意每次调用都会分配新的 Mesh 并立即销毁，性能较差，
        // 仅用于兼容旧代码，新代码应使用 SetMesh。
        // ============================================================

        [Obsolete("UI System now uses meshes.Generate a mesh and use 'SetMesh' instead", false)]
        public void SetVertices(List<UIVertex> vertices)
        {
            SetVertices(vertices.ToArray(), vertices.Count);
        }

        [Obsolete("UI System now uses meshes.Generate a mesh and use 'SetMesh' instead", false)]
        public void SetVertices(UIVertex[] vertices, int size)
        {
            // 创建一个临时 Mesh，将 UIVertex 数据填充进去
            var mesh = new Mesh();

            var positions = new List<Vector3>();
            var colors = new List<Color32>();
            var uv0S = new List<Vector4>();
            var uv1S = new List<Vector4>();
            var uv2S = new List<Vector4>();
            var uv3S = new List<Vector4>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var prevPositions = new List<Vector4>();
            var indices = new List<int>();

            // 每 4 个顶点（一个四边形）生成 2 个三角形
            // 三角形 1: i, i+1, i+2
            // 三角形 2: i+2, i+3, i
            for (var i = 0; i < size; i += 4)
            {
                for (var k = 0; k < 4; k++)
                {
                    positions.Add(vertices[i + k].position);
                    colors.Add(vertices[i + k].color);
                    uv0S.Add(vertices[i + k].uv0);
                    uv1S.Add(vertices[i + k].uv1);
                    uv2S.Add(vertices[i + k].uv2);
                    uv3S.Add(vertices[i + k].uv3);
                    normals.Add(vertices[i + k].normal);
                    tangents.Add(vertices[i + k].tangent);
                    prevPositions.Add(vertices[i + k].prevPosition);
                }
                indices.Add(i);
                indices.Add(i + 1);
                indices.Add(i + 2);

                indices.Add(i + 2);
                indices.Add(i + 3);
                indices.Add(i);
            }

            mesh.SetVertices(positions);
            mesh.SetColors(colors);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uv0S);
            mesh.SetUVs(1, uv1S);
            mesh.SetUVs(2, uv2S);
            mesh.SetUVs(3, uv3S);
            mesh.SetUVs(4, prevPositions);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            SetMesh(mesh);
            // 提交到 CanvasRenderer 后立即销毁临时 Mesh
            // Mesh 数据已被复制到 CanvasRenderer 内部，原 Mesh 不再需要
            DestroyImmediate(mesh);
        }

        /// <summary>
        /// 内部方法：生成索引流。
        /// 为 UIVertex 列表中的每个顶点生成线性索引（0, 1, 2, 3, ...）。
        /// 因为 UI 网格顶点和索引是一一对应的（没有索引复用）。
        /// </summary>
        private static void SplitIndicesStreamsInternal(List<UIVertex> verts, List<int> indices)
        {
            indices.Clear();
            for (var i = 0; i < verts.Count; ++i)
                indices.Add(i);
        }

        /// <summary>
        /// [Native] 将 UIVertex Span 拆分为分离的顶点数据流。
        /// 位置 → positions 列表，颜色 → colors 列表，UV → uv0S 列表等。
        /// 这是实际执行拆分操作的 C++ 端方法。
        /// </summary>
        [StaticAccessor("UI", StaticAccessorType.DoubleColon)]
        private static extern void SplitUIVertexStreamsInternal(ReadOnlySpan<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S,
            List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<Vector4> prevPositions);

        /// <summary>
        /// [Native] 从分离的顶点数据流创建 UIVertex 列表。
        /// 是 SplitUIVertexStreamsInternal 的逆操作。
        /// 使用 ReadOnlySpan 作为输入以避免内存分配。
        /// </summary>
        [StaticAccessor("UI", StaticAccessorType.DoubleColon)]
        private static extern void CreateUIVertexStreamInternal(List<UIVertex> verts, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Color32> colors, ReadOnlySpan<Vector4> uv0S, ReadOnlySpan<Vector4> uv1S, ReadOnlySpan<Vector4> uv2S,
            ReadOnlySpan<Vector4> uv3S, ReadOnlySpan<Vector3> normals, ReadOnlySpan<Vector4> tangents, ReadOnlySpan<Vector4> prevPositions, ReadOnlySpan<int> indices);

        // ============================================================
        // 重建请求回调
        // ============================================================

        /// <summary>重建请求委托</summary>
        public delegate void OnRequestRebuild();

        /// <summary>
        /// 重建请求事件。
        /// 当 Canvas 系统需要重新构建渲染数据时触发。
        /// CanvasManager 会监听此事件来触发网格重建流程。
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event OnRequestRebuild onRequestRebuild;

        /// <summary>
        /// [由 Native 代码调用] 请求刷新。
        /// 通知 Canvas 系统需要重新构建渲染数据。
        /// 在 UI 布局或视觉属性发生变化时由 C++ 端调用。
        /// </summary>
        [RequiredByNativeCode]
        internal static void RequestRefresh()
        {
            onRequestRebuild?.Invoke();
        }

    }
}
