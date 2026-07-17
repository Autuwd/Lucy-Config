// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GUILayoutUtility — 自动布局系统的底层工具
//
// 📌 作用：
//   GUILayoutUtility 是 IMGUI 自动布局（GUILayout）的底层支撑。
//   它负责获取窗口矩形和移动窗口位置等底层操作。
//
// 🔄 GUILayout vs GUI 的区别：
//   GUI — 绝对定位：
//     GUI.Button(new Rect(10, 10, 100, 30), "Click"); // 手动指定位置
//     优点：完全控制位置
//     缺点：需要手动管理布局
//
//   GUILayout — 自动布局：
//     GUILayout.Button("Click"); // 引擎自动计算位置
//     优点：自动排列、自适应内容
//     缺点：灵活性较低
//
// 💡 自动布局工作原理：
//   1. Layout 阶段： GUILayout 系列方法被调用两次
//      - 第一次：CalcSize() 计算每个控件需要的空间
//      - 第二次：Draw() 在分配的位置绘制控件
//   2. Repaint 阶段：只调用 Draw() 一次
//
//   布局容器：
//     GUILayout.BeginHorizontal() — 水平排列
//     GUILayout.BeginVertical()   — 垂直排列
//     GUILayout.BeginArea(rect)   — 固定区域排列
//     GUILayout.BeginScrollView() — 滚动区域
//
// ⚡ 布局计算（在 GUILayoutUtility.cs 和 LayoutGroup.cs 中）：
//   - LayoutGroup 管理容器内的控件排列
//   - LayoutEntry 存储每个控件的尺寸信息
//   - GUILayoutOption 提供额外的布局约束（minWidth/maxHeight 等）
//
// 📍 对应 C++ 头文件：Modules/IMGUI/GUILayoutUtility.bindings.h
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // GUILayoutUtility — 自动布局底层工具（C++ 绑定部分）
    //
    // 🔑 关键方法：
    //   - Internal_GetWindowRect: 获取窗口的矩形区域
    //   - Internal_MoveWindow: 移动窗口位置
    //   - GetWindowsBounds: 获取所有窗口的边界
    //
    // 💡 大部分布局计算逻辑在 GUILayoutUtility.cs 中用 C# 实现，
    //    这里只有需要直接访问 C++ 引擎状态的方法。
    //    C++ 端主要负责窗口（Window 控件）的位置追踪。
    // ==============================================================
    [NativeHeader("Modules/IMGUI/GUILayoutUtility.bindings.h")]
    partial class GUILayoutUtility
    {
        // ==============================================================
        // 窗口矩形获取
        //
        // 🎯 Internal_GetWindowRect — 获取指定窗口 ID 的当前矩形。
        //    窗口在用户拖拽/调整大小后位置会改变，
        //    此方法用于查询最新位置。
        //
        // 💡 窗口 ID 与 GUI.Window(id, ...) 中的 id 对应。
        //    返回的 Rect 包含窗口的屏幕坐标和尺寸。
        // ==============================================================
        private static extern Rect Internal_GetWindowRect(int windowID);

        // ==============================================================
        // 窗口位置移动
        //
        // 🎯 Internal_MoveWindow — 将窗口移动到指定矩形位置。
        //    用于实现窗口拖拽功能。
        //    传入新的 Rect 来设置窗口位置和大小。
        //
        // 💡 GUI.DragWindow() 内部最终调用此方法。
        //    一般不需要直接调用，通过 DragWindow 即可。
        // ==============================================================
        private static extern void Internal_MoveWindow(int windowID, Rect r);

        // ==============================================================
        // 所有窗口的边界
        //
        // 📌 GetWindowsBounds — 获取所有 IMGUI 窗口的联合边界矩形。
        //    用于编辑器中管理多个浮动窗口的布局。
        //    返回包含所有窗口的最小矩形。
        // ==============================================================
        internal static extern Rect GetWindowsBounds();
    }
}
