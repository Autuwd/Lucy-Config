// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // =====================================================
    // DrivenTransformProperties — 驱动变换属性枚举（位标记）
    // =====================================================
    // 此枚举定义了 RectTransform 的哪些属性被外部组件"驱动"（自动控制）。
    //
    // 驱动者（Driver）通常是布局组件（如 Layout Group、Content Size Fitter 等），
    // 它们会自动计算并设置 RectTransform 的某些属性。
    //
    // 当某个属性被驱动时：
    // 1. 用户在 Inspector 中无法手动修改此属性（灰色锁定状态）
    // 2. 属性值由驱动者组件自动控制
    // 3. DrivenRectTransformTracker 会追踪这些驱动关系
    //
    // 典型场景：
    // - HorizontalLayoutGroup 驱动子元素的 AnchoredPosition 和 SizeDelta
    // - ContentSizeFitter 驱动元素的 Width/Height
    // - AspectRatioFitter 驱动元素的 SizeDelta 或 Width/Height
    // =====================================================
    [Flags]
    public enum DrivenTransformProperties
    {
        /// <summary>无任何属性被驱动</summary>
        None = 0,
        /// <summary>所有属性都被驱动（全部位标记置 1）</summary>
        All = ~None,

        // ---- 独立位置属性 ----
        /// <summary>锚定位置 X 轴被驱动</summary>
        AnchoredPositionX = 1 << 1,
        /// <summary>锚定位置 Y 轴被驱动</summary>
        AnchoredPositionY = 1 << 2,
        /// <summary>锚定位置 Z 轴被驱动（仅 3D 模式）</summary>
        AnchoredPositionZ = 1 << 3,

        /// <summary>旋转被驱动</summary>
        Rotation = 1 << 4,

        // ---- 独立缩放属性 ----
        /// <summary>缩放 X 被驱动</summary>
        ScaleX = 1 << 5,
        /// <summary>缩放 Y 被驱动</summary>
        ScaleY = 1 << 6,
        /// <summary>缩放 Z 被驱动</summary>
        ScaleZ = 1 << 7,

        // ---- 独立锚点属性 ----
        /// <summary>最小锚点 X 被驱动</summary>
        AnchorMinX = 1 << 8,
        /// <summary>最小锚点 Y 被驱动</summary>
        AnchorMinY = 1 << 9,
        /// <summary>最大锚点 X 被驱动</summary>
        AnchorMaxX = 1 << 10,
        /// <summary>最大锚点 Y 被驱动</summary>
        AnchorMaxY = 1 << 11,

        // ---- 独立尺寸增量属性 ----
        /// <summary>尺寸增量 X 被驱动</summary>
        SizeDeltaX = 1 << 12,
        /// <summary>尺寸增量 Y 被驱动</summary>
        SizeDeltaY = 1 << 13,

        // ---- 独立枢轴属性 ----
        /// <summary>枢轴 X 被驱动</summary>
        PivotX = 1 << 14,
        /// <summary>枢轴 Y 被驱动</summary>
        PivotY = 1 << 15,

        // ---- 组合属性（便捷组合） ----
        /// <summary>锚定位置 2D（X+Y）被驱动</summary>
        AnchoredPosition = AnchoredPositionX | AnchoredPositionY,
        /// <summary>锚定位置 3D（X+Y+Z）被驱动</summary>
        AnchoredPosition3D = AnchoredPositionX | AnchoredPositionY | AnchoredPositionZ,
        /// <summary>整体缩放（X+Y+Z）被驱动</summary>
        Scale = ScaleX | ScaleY | ScaleZ,
        /// <summary>最小锚点（X+Y）被驱动</summary>
        AnchorMin = AnchorMinX | AnchorMinY,
        /// <summary>最大锚点（X+Y）被驱动</summary>
        AnchorMax = AnchorMaxX | AnchorMaxY,
        /// <summary>所有锚点（Min+Max）被驱动</summary>
        Anchors = AnchorMin | AnchorMax,
        /// <summary>尺寸增量（X+Y）被驱动</summary>
        SizeDelta = SizeDeltaX | SizeDeltaY,
        /// <summary>枢轴（X+Y）被驱动</summary>
        Pivot = PivotX | PivotY
    }

    // =====================================================
    // DrivenRectTransformTracker — 驱动 RectTransform 追踪器
    // =====================================================
    // 这是一个值类型结构体，用于追踪哪些 RectTransform 被布局组件"驱动"。
    //
    // 工作流程：
    // 1. 布局组件（如 LayoutGroup）在 OnEnable 中调用 Add() 注册被驱动的 RectTransform
    // 2. 当布局组件重新计算时，直接设置 RectTransform 的属性值
    // 3. 布局组件在 OnDisable 中调用 Clear() 释放所有驱动关系
    //
    // 驱动的效果：
    // - 被驱动的属性在 Inspector 中显示为灰色（锁定状态）
    // - 用户无法手动修改被驱动的属性
    // - 当驱动者被销毁或禁用时，属性恢复可编辑状态
    //
    // 编辑器模式下的 Undo 处理：
    // - 仅在已有 Undo 录制时才记录修改（避免自动布局导致场景无意义变脏）
    // - 修复了案例 1268783（合并预制体时 LayoutGroup 导致场景被标记为脏）
    // - s_BlockUndo 用于阻止不必要的 Undo 记录（例如动画模式中）
    // =====================================================
    [NativeHeader("Editor/Src/Animation/AnimationModeSnapshot.h")]
    [NativeHeader("Editor/Src/Undo/PropertyUndoManager.h")]
    public struct DrivenRectTransformTracker
    {
        /// <summary>被追踪的 RectTransform 列表</summary>
        private List<RectTransform> m_Tracked;

        /// <summary>
        /// 判断是否可以记录修改（用于 Undo 系统）。
        /// 条件：不在动画模式中，并且（正在撤销/重做 或 已有 Undo 录制）。
        /// 这避免了自动布局过程意外创建 Undo 记录。
        /// </summary>
        internal static bool CanRecordModifications()
        {
            return !IsInAnimationMode() && (IsUndoingOrRedoing() || HasUndoRecordObjects());
        }

        /// <summary>检查是否处于动画模式</summary>
        [FreeFunction("GetAnimationModeSnapshot().IsInAnimationMode")]
        static extern bool IsInAnimationMode();

        /// <summary>检查 PropertyUndoManager 是否有录制记录</summary>
        [FreeFunction("GetPropertyUndoManager().HasRecordings")]
        static extern bool HasUndoRecordObjects();

        /// <summary>检查当前是否正在执行撤销/重做操作</summary>
        [FreeFunction("GetPropertyUndoManager().IsUndoingOrRedoing")]
        static extern bool IsUndoingOrRedoing();

        /// <summary>阻止 Undo 录制的全局开关</summary>
        private static bool s_BlockUndo;

        /// <summary>全局停止录制 Undo</summary>
        public static void StopRecordingUndo() { s_BlockUndo = true; }

        /// <summary>全局恢复录制 Undo</summary>
        public static void StartRecordingUndo() { s_BlockUndo = false; }

        /// <summary>
        /// 注册一个被驱动的 RectTransform。
        /// </summary>
        /// <param name="driver">驱动者对象（通常是布局组件自身）</param>
        /// <param name="rectTransform">被驱动的 RectTransform</param>
        /// <param name="drivenProperties">哪些属性被驱动</param>
        public void Add(Object driver, RectTransform rectTransform, DrivenTransformProperties drivenProperties)
        {
            if (m_Tracked == null)
                m_Tracked = new List<RectTransform>();

            // 编辑器模式下，如果条件允许则记录 Undo
            if (!Application.isPlaying && CanRecordModifications() && !s_BlockUndo)
                RuntimeUndo.RecordObject(rectTransform, "Driving RectTransform");

            // 如果驱动者变了，先清除之前的驱动属性
            if (rectTransform.drivenByObject != driver)
                rectTransform.drivenProperties = DrivenTransformProperties.None;

            rectTransform.drivenByObject = driver;
            rectTransform.drivenProperties = rectTransform.drivenProperties | drivenProperties;

            m_Tracked.Add(rectTransform);
        }

        [Obsolete("revertValues parameter is ignored. Please use Clear() instead.")]
        public void Clear(bool revertValues)
        {
            Clear();
        }

        /// <summary>
        /// 清除所有追踪的驱动关系。
        /// 将所有被追踪的 RectTransform 的 drivenByObject 和 drivenProperties 重置。
        /// 布局组件应在 OnDisable 中调用此方法。
        /// </summary>
        public void Clear()
        {
            if (m_Tracked != null)
            {
                for (int i = 0; i < m_Tracked.Count; i++)
                {
                    if (m_Tracked[i] != null)
                    {
                        if (!Application.isPlaying && CanRecordModifications() && !s_BlockUndo)
                            RuntimeUndo.RecordObject(m_Tracked[i], "Driving RectTransform");

                        // 清除驱动关系，恢复属性的可编辑状态
                        m_Tracked[i].drivenByObject = null;
                        m_Tracked[i].drivenProperties = DrivenTransformProperties.None;
                    }
                }
                m_Tracked.Clear();
            }
        }
    }

    // =====================================================
    // RectTransform — 矩形变换组件
    // =====================================================
    // RectTransform 是 Transform 的专用于 UI 的子类。
    //
    // 与 Transform 的区别：
    // ——————————————————————————————————————————————————————
    //  Transform：使用 position/rotation/scale 定位，无大小概念
    //  RectTransform：使用 锚点(anchor) + 枢轴(pivot) + 相对位置
    //                 + 尺寸增量(sizeDelta) 来定位和设置大小
    //
    // 锚点系统（Anchor）—— Unity UI 最核心的概念：
    // ——————————————————————————————————————————————————————
    //  anchorMin/anchorMax 定义了矩形相对于父级 RectTransform 的"锚点矩形"。
    //  锚点以父级矩形的比例表示（0~1），(0,0) 为父级左下角，(1,1) 为右上角。
    //
    //  锚点的对齐方式决定了 UI 元素如何响应父级尺寸变化：
    //  ├─ 锚点聚在一起 → UI 元素保持相对于锚点的固定距离
    //  ├─ 锚点分散展开 → UI 元素随父级缩放而拉伸/压缩
    //  └─ 锚点居中     → UI 元素保持相对于父级边缘的比例
    //
    // 位置计算：
    // ——————————————————————————————————————————————————————
    //  要理解 RectTransform 的布局，需要掌握以下属性的关系：
    //
    //  anchoredPosition — UI 元素的"锚点位置"（相对于锚点的偏移）
    //  sizeDelta        — UI 元素相对于锚点矩形的大小差
    //  pivot            — 枢轴点（旋转/缩放的中心点，0~1 比例）
    //  rect             — 计算后的最终矩形（只读，由以上属性推导）
    //
    //  offsetMin 和 offsetMax 是便捷属性，分别对应矩形的左下和右上角：
    //  offsetMin = anchoredPosition - pivot * sizeDelta
    //  offsetMax = anchoredPosition + (1-pivot) * sizeDelta
    //
    // 核心公式：
    //  rect.size = 父级尺寸 × (anchorMax - anchorMin) + sizeDelta
    //  anchoredPosition = 偏移量 + pivot 对齐调整
    //
    // 注意：RectTransform 是 sealed 类，不能被继承。
    // =====================================================

    [NativeHeader("Runtime/Transform/RectTransform.h"),
     NativeClass("UI::RectTransform")]
    [UIModuleHelpURL("class-RectTransform")]
    public sealed class RectTransform : Transform
    {
        /// <summary>矩形四边枚举（用于 SetInsetAndSizeFromParentEdge）</summary>
        public enum Edge { Left = 0, Right = 1, Top = 2, Bottom = 3 }

        /// <summary>轴向枚举</summary>
        public enum Axis { Horizontal = 0, Vertical = 1 }

        /// <summary>尝试适配到目标矩形内部的结果</summary>
        public enum FitResult
        {
            /// <summary>适配成功</summary>
            Success = 0,
            /// <summary>已经在目标内部，无需调整</summary>
            AlreadyInside = 1,
            /// <summary>比目标矩形大，无法适配</summary>
            FailLargerThanTarget = 2,
            /// <summary>两个矩形不在同一平面上</summary>
            FailNotCoplanar = 3,
            /// <summary>Z 轴旋转不匹配</summary>
            FailZRotationMismatch = 4,
            /// <summary>目标尺寸无效</summary>
            FailInvalidSizeTarget = 5,
        }

        /// <summary>
        /// 重新应用驱动属性时的委托。
        /// 当布局组件需要重新应用被驱动的属性时触发。
        /// </summary>
        public delegate void ReapplyDrivenProperties(RectTransform driven);

        /// <summary>
        /// 重新应用驱动属性事件。
        /// 当一个 RectTransform 的驱动属性需要重新计算时触发。
        /// 布局组件（如 LayoutGroup）监听此事件来重新计算布局。
        /// </summary>
        public static event ReapplyDrivenProperties reapplyDrivenProperties;

        // ============================================================
        // RectTransform 核心属性
        // ============================================================

        /// <summary>
        /// RectTransform 的本地空间矩形（只读）。
        /// 返回计算后的最终矩形，包含位置和大小信息。
        /// 该值由 anchorMin/anchorMax/pivot/sizeDelta/anchorPosition 综合计算得出。
        /// x,y 是矩形左下角在本地坐标系中的位置（相对于枢轴），
        /// width/height 是矩形的实际像素大小。
        /// </summary>
        public extern Rect rect { get; }

        /// <summary>
        /// 最小锚点 — 锚点矩形的左下角（父级比例 0~1）。
        /// (0,0) = 父级左下角，(1,1) = 父级右上角。
        /// 例如：anchorMin=(0.25,0.25) 表示锚点矩形起始于父级 25% 位置。
        /// </summary>
        public extern Vector2 anchorMin { get; set; }

        /// <summary>
        /// 最大锚点 — 锚点矩形的右上角（父级比例 0~1）。
        /// anchorMax 的各个分量应 >= anchorMin。
        /// 当 anchorMin == anchorMax 时，锚点成为一个点（"锚点聚在一起"）。
        /// 当 anchorMin < anchorMax 时，锚点展开为一个矩形区域。
        /// </summary>
        public extern Vector2 anchorMax { get; set; }

        /// <summary>
        /// 锚定位置 —— UI 元素相对于锚点的偏移量。
        /// 当锚点聚在一起（anchorMin == anchorMax）时：
        ///   — 表示 UI 元素枢轴到锚点的距离（类似 position 但相对于锚点）
        /// 当锚点展开（anchorMin < anchorMax）时：
        ///   — 表示 UI 元素在锚点矩形内的偏移
        /// 值可以为正也可以为负。
        /// </summary>
        public extern Vector2 anchoredPosition { get; set; }

        /// <summary>
        /// 尺寸增量 —— UI 元素相对于锚点矩形的大小差。
        /// 关键公式：rect.size = 锚点矩形大小 + sizeDelta
        /// 当 anchorMin == anchorMax（锚点聚在一起）时：
        ///   — sizeDelta = UI 元素的大小（因为锚点矩形大小为 0）
        /// 当 anchorMin < anchorMax（锚点展开）时：
        ///   — sizeDelta 决定了 UI 元素比锚点矩形大（正）或小（负）多少
        /// 正值表示 UI 比锚点矩形大，负值表示比锚点矩形小。
        /// </summary>
        public extern Vector2 sizeDelta { get; set; }

        /// <summary>
        /// 枢轴 —— 旋转和缩放的中心点（相对比例 0~1）。
        /// (0,0) = 矩形左下角，(1,1) = 矩形右上角，(0.5,0.5) = 中心。
        /// 枢轴也影响 anchoredPosition 的计算方式。
        /// 例如：pivot=(0,0) 时 anchoredPosition 相对于左下角；
        ///       pivot=(0.5,0.5) 时 anchoredPosition 相对于中心。
        /// </summary>
        public extern Vector2 pivot { get; set; }

        /// <summary>
        /// 3D 锚定位置 —— 带 Z 轴的锚定位置。
        /// X/Y 分量为 anchoredPosition，Z 分量为 localPosition.z。
        /// 主要用于在 Z 轴上对 UI 元素进行排序或定位。
        /// </summary>
        public Vector3 anchoredPosition3D
        {
            get
            {
                Vector2 pos2 = anchoredPosition;
                return new Vector3(pos2.x, pos2.y, localPosition.z);
            }
            set
            {
                anchoredPosition = new Vector2(value.x, value.y);
                Vector3 pos3 = localPosition;
                pos3.z = value.z;
                localPosition = pos3;
            }
        }

        /// <summary>
        /// 偏移最小值 —— 矩形左下角相对于锚点左下角的偏移。
        /// 这是一个便捷属性，等价于矩形左边缘和下边缘的偏移。
        ///
        /// 计算公式：
        ///   offsetMin = anchoredPosition - Vector2.Scale(sizeDelta, pivot)
        ///
        /// 设置 offsetMin 会同时调整 sizeDelta 和 anchoredPosition，
        /// 保持 offsetMax（右上角）不变。常用于手动控制矩形大小。
        /// </summary>
        public Vector2 offsetMin
        {
            get
            {
                return anchoredPosition - Vector2.Scale(sizeDelta, pivot);
            }
            set
            {
                Vector2 offset = value - (anchoredPosition - Vector2.Scale(sizeDelta, pivot));
                sizeDelta -= offset;
                anchoredPosition += Vector2.Scale(offset, Vector2.one - pivot);
            }
        }

        /// <summary>
        /// 偏移最大值 —— 矩形右上角相对于锚点右上角的偏移。
        /// 这是一个便捷属性，等价于矩形右边缘和上边缘的偏移。
        ///
        /// 计算公式：
        ///   offsetMax = anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot)
        ///
        /// 设置 offsetMax 会同时调整 sizeDelta 和 anchoredPosition，
        /// 保持 offsetMin（左下角）不变。常用于手动控制矩形大小。
        /// </summary>
        public Vector2 offsetMax
        {
            get
            {
                return anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot);
            }
            set
            {
                Vector2 offset = value - (anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot));
                sizeDelta += offset;
                anchoredPosition += Vector2.Scale(offset, pivot);
            }
        }

        // ============================================================
        // 驱动属性系统
        // ============================================================

        /// <summary>
        /// 驱动此 RectTransform 的对象。
        /// 当被布局组件（如 LayoutGroup）驱动时，此属性指向该组件。
        /// 被驱动的属性在 Inspector 中为灰色锁定状态，不可手动编辑。
        /// </summary>
        extern public Object drivenByObject { get; internal set; }

        /// <summary>
        /// 当前被驱动的属性集合（位标记）。
        /// 例如：如果 AnchoredPositionX | SizeDeltaX 被设置，
        /// 意味着 X 轴位置和宽度被布局组件自动控制。
        /// </summary>
        extern internal DrivenTransformProperties drivenProperties { get; set; }

        /// <summary>
        /// 是否在子元素尺寸变化时发送通知（实验性功能）。
        /// 开启后，布局系统可以监听子元素尺寸变化来重新计算布局。
        /// </summary>
        extern public bool sendChildDimensionsChange { get; set; }

        /// <summary>
        /// 强制更新 RectTransform 的数据。
        /// 如果在变换脏标记（TransformDispatch Dirty）时调用此方法，
        /// 会立即重新计算 rect 和相关属性。
        /// 用于需要立即获取最新 rect 值的场景。
        /// </summary>
        [NativeMethod("UpdateIfTransformDispatchIsDirty")] public extern void ForceUpdateRectTransforms();

        /// <summary>
        /// 获取矩形在本地空间中的四个角点。
        /// 角点顺序：左下(0) → 左上(1) → 右上(2) → 右下(3)
        /// 所有坐标相对于 RectTransform 的本地坐标系。
        /// </summary>
        /// <param name="fourCornersArray">长度为 4 的数组，用于存储角点坐标</param>
        public void GetLocalCorners(Vector3[] fourCornersArray)
        {
            if (fourCornersArray == null || fourCornersArray.Length < 4)
            {
                Debug.LogError("Calling GetLocalCorners with an array that is null or has less than 4 elements.");
                return;
            }

            Rect tmpRect = rect;
            float x0 = tmpRect.x;
            float y0 = tmpRect.y;
            float x1 = tmpRect.xMax;
            float y1 = tmpRect.yMax;

            fourCornersArray[0] = new Vector3(x0, y0, 0f);
            fourCornersArray[1] = new Vector3(x0, y1, 0f);
            fourCornersArray[2] = new Vector3(x1, y1, 0f);
            fourCornersArray[3] = new Vector3(x1, y0, 0f);
        }

        /// <summary>
        /// 获取矩形在本地空间中的四个角点（Span 版本）。
        /// 角点顺序：左下(0) → 左上(1) → 右上(2) → 右下(3)
        /// 使用 stackalloc 可避免 GC 分配。
        /// </summary>
        /// <param name="fourCorners">长度至少为 4 的 Span</param>
        public void GetLocalCorners(Span<Vector3> fourCorners)
        {
            if (fourCorners.Length < 4)
            {
                Debug.LogError("Calling GetLocalCorners with a Span<Vector3> that has less than 4 elements.");
                return;
            }

            Rect tmpRect = rect;
            float x0 = tmpRect.x;
            float y0 = tmpRect.y;
            float x1 = tmpRect.xMax;
            float y1 = tmpRect.yMax;

            fourCorners[0] = new Vector3(x0, y0, 0f);
            fourCorners[1] = new Vector3(x0, y1, 0f);
            fourCorners[2] = new Vector3(x1, y1, 0f);
            fourCorners[3] = new Vector3(x1, y0, 0f);
        }

        /// <summary>
        /// 获取矩形在世界空间中的四个角点。
        /// 通过 localToWorldMatrix 将本地角点转换到世界空间。
        /// 常用于碰撞检测、屏幕空间计算等需要世界坐标的场景。
        ///
        /// 注意：对于 ScreenSpaceOverlay 模式的 Canvas，世界坐标 == 屏幕坐标。
        /// </summary>
        /// <param name="fourCornersArray">长度为 4 的数组，用于存储世界空间角点</param>
        public void GetWorldCorners(Vector3[] fourCornersArray)
        {
            if (fourCornersArray == null || fourCornersArray.Length < 4)
            {
                Debug.LogError("Calling GetWorldCorners with an array that is null or has less than 4 elements.");
                return;
            }

            GetLocalCorners(fourCornersArray);

            Matrix4x4 mat = localToWorldMatrix;
            for (int i = 0; i < 4; i++)
                fourCornersArray[i] = mat.MultiplyPoint(fourCornersArray[i]);
        }

        /// <summary>
        /// 获取矩形在世界空间中的四个角点（Span 版本）。
        /// 角点顺序：左下(0) → 左上(1) → 右上(2) → 右下(3)
        /// </summary>
        /// <param name="fourCorners">长度至少为 4 的 Span</param>
        public void GetWorldCorners(Span<Vector3> fourCorners)
        {
            if (fourCorners.Length < 4)
            {
                Debug.LogError("Calling GetWorldCorners with Span<Vector3> that has less than 4 elements.");
                return;
            }

            Rect r = rect;
            Matrix4x4 m = localToWorldMatrix;
            fourCorners[0] = m.MultiplyPoint(new Vector3(r.xMin, r.yMin));
            fourCorners[1] = m.MultiplyPoint(new Vector3(r.xMin, r.yMax));
            fourCorners[2] = m.MultiplyPoint(new Vector3(r.xMax, r.yMax));
            fourCorners[3] = m.MultiplyPoint(new Vector3(r.xMax, r.yMin));
        }

        /// <summary>
        /// 获取矩形在世界空间中的轴对齐包围矩形（AABB）。
        /// 通过 GetWorldCorners 计算四个角点，然后取最小/最大值构建 Rect。
        /// 注意：如果矩形有旋转，返回的 Rect 会比实际矩形大（包含旋转后的外包围盒）。
        /// </summary>
        public Rect GetWorldRect()
        {
            Span<Vector3> c = stackalloc Vector3[4];
            GetWorldCorners(c);

            Vector3 min = Vector3.Min(Vector3.Min(c[0], c[1]), Vector3.Min(c[2], c[3]));
            Vector3 max = Vector3.Max(Vector3.Max(c[0], c[1]), Vector3.Max(c[2], c[3]));

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        /// <summary>
        /// 判断此 RectTransform 是否完全包含另一个 RectTransform。
        /// 使用世界空间的轴对齐矩形进行比较。
        /// 注意：如果两者有旋转，比较的是外包围盒而非精确形状。
        /// </summary>
        /// <param name="other">要检查是否包含在内的另一个 RectTransform</param>
        public bool Contains(RectTransform other)
        {
            Rect worldRect = GetWorldRect();
            Rect otherRect = other.GetWorldRect();
            return worldRect.xMin <= otherRect.xMin
             && worldRect.xMax >= otherRect.xMax
             && worldRect.yMin <= otherRect.yMin
             && worldRect.yMax >= otherRect.yMax;
        }

        // ============================================================
        // 局部边缘位置便捷方法
        // ============================================================
        // 以下方法用于获取/设置矩形四边在父级空间中的位置。
        // preserveSize 参数决定调整时是否保持大小（仅移动，不改变尺寸）：
        //   true  = 只移动位置，不改变 sizeDelta
        //   false = 调整 sizeDelta 来改变边缘位置
        // ============================================================

        /// <summary>获取上边缘在父级空间中的 Y 坐标</summary>
        public float GetLocalTop() => GetRectInParentSpace().yMax;
        /// <summary>获取下边缘在父级空间中的 Y 坐标</summary>
        public float GetLocalBottom() => GetRectInParentSpace().y;
        /// <summary>获取左边缘在父级空间中的 X 坐标</summary>
        public float GetLocalLeft() => GetRectInParentSpace().x;
        /// <summary>获取右边缘在父级空间中的 X 坐标</summary>
        public float GetLocalRight() => GetRectInParentSpace().xMax;

        /// <summary>
        /// 设置上边缘在父级空间中的位置。
        /// </summary>
        /// <param name="value">新的 Y 坐标</param>
        /// <param name="preserveSize">是否保持当前尺寸（true=仅移动，false=调整高度）</param>
        public void SetLocalTop(float value, bool preserveSize = true)
        {
            if (preserveSize)
            {
                anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + (value - GetLocalTop()));
            }
            else
            {
                float delta = value - GetLocalTop();
                sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + delta);
                anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + pivot.y * delta);
            }
        }

        /// <summary>
        /// 设置下边缘在父级空间中的位置。
        /// </summary>
        /// <param name="value">新的 Y 坐标</param>
        /// <param name="preserveSize">是否保持当前尺寸（true=仅移动，false=调整高度）</param>
        public void SetLocalBottom(float value, bool preserveSize = true)
        {
            if (preserveSize)
            {
                anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + (value - GetLocalBottom()));
            }
            else
            {
                float delta = value - GetLocalBottom();
                sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y - delta);
                anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + (1f - pivot.y) * delta);
            }
        }

        /// <summary>
        /// 设置左边缘在父级空间中的位置。
        /// </summary>
        /// <param name="value">新的 X 坐标</param>
        /// <param name="preserveSize">是否保持当前尺寸（true=仅移动，false=调整宽度）</param>
        public void SetLocalLeft(float value, bool preserveSize = true)
        {
            if (preserveSize)
            {
                anchoredPosition = new Vector2(anchoredPosition.x + (value - GetLocalLeft()), anchoredPosition.y);
            }
            else
            {
                float delta = value - GetLocalLeft();
                sizeDelta = new Vector2(sizeDelta.x - delta, sizeDelta.y);
                anchoredPosition = new Vector2(anchoredPosition.x + (1f - pivot.x) * delta, anchoredPosition.y);
            }
        }

        /// <summary>
        /// 设置右边缘在父级空间中的位置。
        /// </summary>
        /// <param name="value">新的 X 坐标</param>
        /// <param name="preserveSize">是否保持当前尺寸（true=仅移动，false=调整宽度）</param>
        public void SetLocalRight(float value, bool preserveSize = true)
        {
            if (preserveSize)
            {
                anchoredPosition = new Vector2(anchoredPosition.x + (value - GetLocalRight()), anchoredPosition.y);
            }
            else
            {
                float delta = value - GetLocalRight();
                sizeDelta = new Vector2(sizeDelta.x + delta, sizeDelta.y);
                anchoredPosition = new Vector2(anchoredPosition.x + pivot.x * delta, anchoredPosition.y);
            }
        }

        // ============================================================
        // 便捷设置方法
        // ============================================================

        /// <summary>
        /// 同时将 anchorMin 和 anchorMax 设置为同一值（锚点聚在一起）。
        /// 用于快速将锚点定位到指定位置。
        /// </summary>
        /// <param name="position">锚点位置（父级比例 0~1），如 (0.5,0.5) 表示居中</param>
        public void SetAnchors(Vector2 position)
        {
            anchorMin = position;
            anchorMax = position;
        }

        /// <summary>
        /// 同时设置枢轴和锚点到同一位置。
        /// 常用于将 UI 元素的枢轴和锚点对齐到相同位置（如一键居中）。
        /// 示例：SetPivotAndAnchors(new Vector2(0.5f, 0.5f)) 将枢轴和锚点都设置到中心。
        /// </summary>
        /// <param name="position">枢轴和锚点的位置（比例 0~1）</param>
        public void SetPivotAndAnchors(Vector2 position)
        {
            pivot = position;
            anchorMin = position;
            anchorMax = position;
        }

        /// <summary>
        /// 从父级边缘设置内边距和大小。
        /// 这是将 UI 元素对齐到父级某条边的最便捷方法。
        /// 会自动将锚点对齐到指定边缘，并设置尺寸和内边距。
        ///
        /// 示例：SetInsetAndSizeFromParentEdge(Edge.Left, 10, 100)
        ///   → 左边缘内边距 10px，宽度 100px，锚点锁定在左侧
        /// </summary>
        /// <param name="edge">对齐的父级边缘（Left/Right/Top/Bottom）</param>
        /// <param name="inset">与父级边缘的内边距</param>
        /// <param name="size">UI 元素的尺寸（沿对齐方向的长度）</param>
        public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size)
        {
            int axis = (edge == Edge.Top || edge == Edge.Bottom) ? 1 : 0;
            bool end = (edge == Edge.Top || edge == Edge.Right);

            // 将锚点对齐到指定边缘（0=起始端，1=结束端）
            float anchorValue = end ? 1 : 0;
            Vector2 anchor = anchorMin;
            anchor[axis] = anchorValue;
            anchorMin = anchor;
            anchor = anchorMax;
            anchor[axis] = anchorValue;
            anchorMax = anchor;

            // 设置尺寸。锚点聚在一起时，sizeDelta 直接等于元素尺寸
            Vector2 sizeD = sizeDelta;
            sizeD[axis] = size;
            sizeDelta = sizeD;

            // 设置内边距。需要考虑枢轴位置来正确计算偏移
            Vector2 positionCopy = anchoredPosition;
            positionCopy[axis] = end ? -inset - size * (1 - pivot[axis]) : inset + size * pivot[axis];
            anchoredPosition = positionCopy;
        }

        /// <summary>
        /// 在保持当前锚点设置的情况下设置 UI 元素的大小。
        /// 当锚点展开（anchorMin != anchorMax）时特别有用：
        /// sizeDelta 需要减去锚点矩形中的对应尺寸才能得到期望的 UI 大小。
        ///
        /// 公式：sizeDelta = 期望尺寸 - 父级尺寸 × 锚点跨度
        /// </summary>
        /// <param name="axis">轴向（Horizontal=宽度，Vertical=高度）</param>
        /// <param name="size">期望的 UI 元素尺寸</param>
        public void SetSizeWithCurrentAnchors(Axis axis, float size)
        {
            int i = (int)axis;
            Vector2 sizeD = sizeDelta;
            sizeD[i] = size - GetParentSize()[i] * (anchorMax[i] - anchorMin[i]);
            sizeDelta = sizeD;
        }

        /// <summary>
        /// 设置枢轴并同时调整子元素位置（Native 方法）。
        /// 改变枢轴时，子元素的位置会自动调整以保持视觉不变。
        /// adjustChildren 参数控制是否同时调整子元素。
        /// </summary>
        [NativeMethod("SetPivotWithCounterAdjust")]
        private extern void Internal_SetPivotWithCounterAdjust(Vector2 newPivot, bool adjustChildren);

        /// <summary>
        /// 设置枢轴并同时调整子元素位置。
        /// 默认不调整子元素（adjustChildren=false），只改变当前元素的枢轴。
        /// 设置为 true 时，子元素位置会自动补偿枢轴变化。
        /// </summary>
        /// <param name="newPivot">新的枢轴值（0~1）</param>
        /// <param name="adjustChildren">是否调整子元素位置</param>
        public void SetPivotWithCounterAdjust(Vector2 newPivot, bool adjustChildren = false)
        {
            Internal_SetPivotWithCounterAdjust(newPivot, adjustChildren);
        }

        // ============================================================
        // 驱动属性事件通知
        // ============================================================
        // [RequiredByNativeCode] 表示此方法会被 C++ 端代码回调。
        // 当驱动者（布局组件）需要重新应用驱动属性时触发。
        // ============================================================

        /// <summary>
        /// [由 Native 代码调用] 发送重新应用驱动属性事件。
        /// 布局组件通过此事件告知 RectTransform 重新执行布局计算。
        /// </summary>
        [RequiredByNativeCode]
        internal static void SendReapplyDrivenProperties(RectTransform driven)
        {
            reapplyDrivenProperties?.Invoke(driven);
        }

        /// <summary>
        /// 获取相对于父级空间的矩形（以父级左下角为原点）。
        /// 此方法考虑了锚点偏移和枢轴的影响。
        /// 用于计算边缘位置（GetLocalTop/Bottom/Left/Right）的底层实现。
        ///
        /// 计算过程：
        /// 1. 从 rect 获取本地矩形的原始位置和大小
        /// 2. 加上 offsetMin（左下角偏移）
        /// 3. 加上枢轴的缩放偏移
        /// 4. 加上锚点相对于父级左下角的偏移
        /// </summary>
        internal Rect GetRectInParentSpace()
        {
            Rect rectResult = rect;
            Vector2 offset = offsetMin + Vector2.Scale(pivot, rectResult.size);
            RectTransform parentRectTransform = parent as RectTransform;
            if (parentRectTransform)
            {
                offset += Vector2.Scale(anchorMin, parentRectTransform.rect.size);
            }

            rectResult.x += offset.x;
            rectResult.y += offset.y;
            return rectResult;
        }

        /// <summary>
        /// 获取父级 RectTransform 的大小。
        /// 如果父级不是 RectTransform，返回 Vector2.zero。
        /// </summary>
        private Vector2 GetParentSize()
        {
            RectTransform parentRect = parent as RectTransform;
            if (!parentRect)
                return Vector2.zero;
            return parentRect.rect.size;
        }

        // ============================================================
        // 矩形适配方法（用于布局计算）
        // ============================================================

        /// <summary>
        /// 检查两个 RectTransform 是否共面（在同一平面上）。
        /// 当两个矩形位于同一平面且 Z 轴对齐时返回 true。
        /// 用于布局系统判断是否能进行平面内的适配操作。
        /// </summary>
        /// <param name="target">要检查的目标 RectTransform</param>
        /// <exception cref="ArgumentNullException">target 为 null 时抛出</exception>
        public bool IsCoplanarWith(RectTransform target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return Internal_IsCoplanarWith(target);
        }

        [NativeMethod("IsCoplanarWith")]
        private extern bool Internal_IsCoplanarWith(RectTransform target);

        /// <summary>
        /// 使此 RectTransform 适配到目标 RectTransform 内部（共面条件下）。
        /// 会实际修改此 RectTransform 的大小和位置来适配。
        /// </summary>
        /// <param name="target">目标矩形</param>
        /// <param name="allowShrink">是否允许缩小</param>
        /// <returns>适配结果</returns>
        public FitResult FitInsideCoplanarRectTransform(RectTransform target, bool allowShrink = false)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return Internal_FitInsideCoplanarRectTransform(target, allowShrink);
        }

        [NativeMethod("FitInsideCoplanarRectTransform")]
        private extern FitResult Internal_FitInsideCoplanarRectTransform(RectTransform target, bool allowShrink);

        /// <summary>
        /// 尝试适配到目标 RectTransform 内部（不实际修改，仅判断可行性）。
        /// 与 FitInsideCoplanarRectTransform 的区别在于不修改实际大小和位置。
        /// </summary>
        /// <param name="target">目标矩形</param>
        /// <param name="allowShrink">是否允许缩小</param>
        /// <returns>适配结果</returns>
        public FitResult TryFitInsideCoplanarRectTransform(RectTransform target, bool allowShrink = false)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return Internal_TryFitInsideCoplanarRectTransform(target, allowShrink);
        }

        [NativeMethod("TryFitInsideCoplanarRectTransform")]
        private extern FitResult Internal_TryFitInsideCoplanarRectTransform(RectTransform target, bool allowShrink);
    }
}
