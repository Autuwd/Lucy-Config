// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GUIClip — IMGUI 裁剪与坐标变换系统
//
// 📌 作用：
//   GUIClip 管理 IMGUI 的裁剪矩形栈（Clipping Stack）。
//   当 GUI 控件超出可见区域时，裁剪系统会隐藏超出部分。
//   这是 ScrollView（滚动视图）的核心实现基础。
//
// 🔄 裁剪栈机制：
//   GUIClip 使用栈（Stack）管理裁剪区域：
//
//   Push(rect) → 压入新裁剪区域（如 ScrollView 的可视区域）
//     ┌──────────────────┐ ← 新裁剪区域
//     │   ┌──────────┐   │
//     │   │ 子控件   │   │ ← 只有这个区域内的内容可见
//     │   └──────────┘   │
//     └──────────────────┘
//   Pop() → 弹出裁剪区域（ScrollView 结束时）
//
//   裁剪区域可以嵌套！
//   嵌套 ScrollView 会叠加裁剪区域。
//
// 🔄 坐标转换系统：
//   Clip — 将绝对坐标转换为裁剪后的坐标
//   Unclip — 将裁剪坐标转换回绝对坐标
//   ClipToWindow — 相对于窗口的裁剪转换
//   UnclipToWindow — 相对于窗口的取消裁剪转换
//
// 💡 关键概念：
//   visibleRect — 当前实际可见的矩形区域
//   topmostRect — 栈顶的物理裁剪矩形（未裁剪坐标系）
//   scrollOffset — 滚动偏移量（ScrollView 的滚动位置）
//   renderOffset — 渲染偏移量（用于动画/过渡效果）
//
// ⚡ 矩阵变换：
//   GUIClip 维护变换矩阵（GetMatrix/SetMatrix），
//   支持对 GUI 坐标系进行旋转、缩放等变换。
//   这也用于 UI Toolkit 与 IMGUI 的坐标桥接。
//
// 📍 对应 C++ 头文件：Modules/IMGUI/GUIClip.h
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // GUIClip — IMGUI 裁剪与坐标变换（C++ 绑定部分）
    //
    // 🔑 关键属性：
    //   - enabled: 裁剪是否启用
    //   - visibleRect: 当前可见矩形
    //   - topmostRect: 栈顶裁剪矩形
    //
    // 🔑 关键方法：
    //   - Push/Pop: 压入/弹出裁剪区域
    //   - Clip/Unclip: 坐标转换
    //   - Reapply: 重新应用裁剪状态
    //
    // 💡 所有方法都通过 GetGUIState() 访问 GUI 全局状态，
    //    GUIClip 的数据存储在 CanvasGUIState 的 GUIClipState 中。
    // ==============================================================
    [NativeHeader("Modules/IMGUI/GUIClip.h"),
     NativeHeader("Modules/IMGUI/GUIState.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.UIBuilderModule")]
    internal partial class GUIClip
    {
        // ==============================================================
        // 裁剪启用状态
        //
        // 🎯 enabled — 当前是否有激活的裁剪区域。
        //    如果没有 Push 过裁剪区域，enabled = false。
        //    ScrollView 启用时 enabled = true。
        // ==============================================================
        internal static extern bool enabled {[FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetEnabled")] get; }

        // ==============================================================
        // 可见矩形与顶层矩形
        //
        // 🎯 visibleRect — 当前所有裁剪区域交集后的实际可见矩形。
        //   坐标已经考虑了滚动偏移和裁剪。
        //   控件只有在这个矩形内才会被渲染。
        //
        // 🎯 topmostRect — 栈顶的物理裁剪矩形（未应用偏移）。
        //   Editor 中用于在 ScrollView 内裁剪光标矩形。
        //   坐标是绝对坐标，未经过 scroll/render offset 变换。
        // ==============================================================
        // The visible rectangle.
        internal static extern Rect visibleRect {[FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetVisibleRect")] get; }

        // The topmost physical rect in unclipped coordinates
        // Used in editor to clip cursor rects inside scroll views
        internal static extern Rect topmostRect
        {
            [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetTopMostPhysicalRect")]
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get;
        }

        // ==============================================================
        // 🎯 裁剪区域 Push/Pop — 裁剪栈核心操作
        //
        // Internal_Push — 压入新的裁剪区域
        //   screenRect:    裁剪矩形（屏幕坐标）
        //   scrollOffset:  滚动偏移（ScrollView 的滚动量）
        //   renderOffset:  渲染偏移（UI 动画偏移）
        //   resetOffset:   是否重置偏移（而非累加）
        //
        // Internal_Pop — 弹出最顶层的裁剪区域
        //   撤销最近一次 Push 的效果。
        //
        // 💡 典型使用流程（ScrollView 内部实现）：
        //   GUIClip.Push(clipRect, scrollOffset, Vector2.zero, false);
        //   // ... 绘制 ScrollView 内容 ...
        //   GUIClip.Pop();
        //
        // 🔄 裁剪栈嵌套：
        //   BeginScrollView → Push(rect)
        //     BeginScrollView → Push(rect)  ← 嵌套！
        //     EndScrollView → Pop()
        //   EndScrollView → Pop()
        // ==============================================================
        // Push a clip rect to the stack with pixel offsets.
        internal static extern void Internal_Push(Rect screenRect, Vector2 scrollOffset, Vector2 renderOffset, bool resetOffset);

        // Removes the topmost clipping rectangle, undoing the effect of the latest GUIClip.Push
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void Internal_Pop();

        // ==============================================================
        // 裁剪栈深度与栈顶矩形
        //
        // 📌 Internal_GetCount — 当前裁剪栈的深度。
        //    0 表示没有裁剪，1 表示有一层裁剪，以此类推。
        //
        // 📌 GetTopRect — 获取栈顶裁剪矩形（已经应用偏移）。
        //    与 topmostRect 不同，这个是裁剪后的坐标。
        // ==============================================================
        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetCount")]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern int Internal_GetCount();

        // Get the topmost rectangle
        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetTopRect")]
        internal static extern Rect GetTopRect();

        // ==============================================================
        // 🎯 坐标转换方法（Clip / Unclip）
        //
        // Unclip — 将裁剪坐标系中的点/矩形转换为绝对坐标
        //   Unclip_Vector2(pos) — 点的转换
        //   Unclip_Rect(rect)   — 矩形的转换
        //
        // Clip — 将绝对坐标转换为裁剪坐标系
        //   Clip_Vector2(absolutePos) — 点的转换
        //   Internal_Clip_Rect(rect)   — 矩形的转换
        //
        // 💡 什么时候用：
        //   当你从 ScrollView 外部获得一个坐标，
        //   需要转换到 ScrollView 内部坐标系时使用 Clip。
        //   反向操作使用 Unclip。
        //
        // 🔄 坐标转换方向：
        //   绝对坐标 ──Clip──→ 裁剪坐标
        //   裁剪坐标 ──Unclip──→ 绝对坐标
        // ==============================================================
        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.Unclip")]
        private static extern Vector2 Unclip_Vector2(Vector2 pos);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.Unclip")]
        private static extern Rect Unclip_Rect(Rect rect);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.Clip")]
        private static extern Vector2 Clip_Vector2(Vector2 absolutePos);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.Clip")]
        private static extern Rect Internal_Clip_Rect(Rect absoluteRect);

        // ==============================================================
        // 窗口级坐标转换（ClipToWindow / UnclipToWindow）
        //
        // 🎯 与 Clip/Unclip 类似，但基于窗口坐标系。
        //   UnclipToWindow — 裁剪坐标 → 窗口坐标
        //   ClipToWindow   — 窗口坐标 → 裁剪坐标
        //
        // 💡 窗口坐标 = 相对于当前 Window 控件的坐标。
        //    用于 Window 内部的 ScrollView 场景。
        // ==============================================================
        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.UnclipToWindow")]
        private static extern Vector2 UnclipToWindow_Vector2(Vector2 pos);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.UnclipToWindow")]
        private static extern Rect UnclipToWindow_Rect(Rect rect);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.ClipToWindow")]
        private static extern Vector2 ClipToWindow_Vector2(Vector2 absolutePos);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.ClipToWindow")]
        private static extern Rect ClipToWindow_Rect(Rect absoluteRect);

        // ==============================================================
        // 绝对鼠标位置
        //
        // 📌 Internal_GetAbsoluteMousePosition — 获取不受裁剪影响的
        //    鼠标绝对位置。用于在裁剪区域外检测鼠标。
        // ==============================================================
        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetAbsoluteMousePosition")]
        private static extern Vector2 Internal_GetAbsoluteMousePosition();

        // ==============================================================
        // 裁剪状态重新应用
        //
        // 📌 Reapply — 重新应用当前裁剪信息。
        //    在某些情况下（如窗口大小变化）需要重新应用裁剪。
        // ==============================================================
        // Reapply the clipping info.
        internal static extern void Reapply();

        // ==============================================================
        // 🎯 GUI 变换矩阵（Matrix4x4）
        //
        // GetMatrix — 获取当前用户定义的 GUI 变换矩阵。
        // SetMatrix — 设置 GUI 变换矩阵。
        //
        // 💡 矩阵变换可以对整个 GUI 坐标系进行旋转、缩放、平移。
        //    这也用于 Canvas GUI 模式下的坐标变换。
        //
        // 📌 GetParentMatrix — 获取父级变换矩阵。
        //    用于 UI Toolkit 与 IMGUI 的坐标桥接。
        // ==============================================================
        // Set the GUIMatrix. This is here as this class handles all coordinate transforms anyways.
        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetUserMatrix")]
        internal static extern Matrix4x4 GetMatrix();

        internal static extern void SetMatrix(Matrix4x4 m);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetParentTransform")]
        internal static extern Matrix4x4 GetParentMatrix();

        // ==============================================================
        // 父级裁剪 Push/Pop（带变换矩阵）
        //
        // 🎯 内部方法：带变换矩阵的裁剪压入。
        //   renderTransform  — 渲染变换（影响视觉位置）
        //   inputTransform   — 输入变换（影响鼠标点击检测）
        //   clipRect         — 裁剪区域
        //
        // 💡 区分 render 和 input 变换是因为：
        //   - 视觉上可能需要偏移动画（renderTransform）
        //   - 但鼠标点击位置不应该被动画偏移影响（inputTransform）
        //   这是 UI Toolkit 使用的高级特性。
        // ==============================================================
        internal static extern void Internal_PushParentClip(Matrix4x4 renderTransform, Matrix4x4 inputTransform, Rect clipRect);

        internal static extern void Internal_PopParentClip();
    }
}
