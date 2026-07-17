// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GUI — Unity 即时模式 GUI (IMGUI) 的核心入口类
//
// 📌 作用：
//   GUI 是 Immediate Mode GUI（即时模式图形用户界面）的主控制类。
//   所有 GUI 控件（Label, Button, TextField 等）都是 GUI 的静态方法。
//   与 Unity 现有的 UGUI/UI Toolkit 不同，IMGUI 每帧重建 UI。
//
// 🔄 即时模式 vs 保留模式：
//   即时模式 (IMGUI)：每帧重新调用绘制代码，没有持久的 UI 对象树。
//     - 优点：代码简洁、适合编辑器工具
//     - 缺点：每帧都重建，性能不如保留模式
//   保留模式 (UGUI/UI Toolkit)：创建 UI 对象后保留在场景中。
//     - 优点：性能好、适合游戏运行时 UI
//     - 缺点：需要额外的组件/层级管理
//
// ⚡ GUI 事件循环：
//   每帧 Unity 会触发 EventType 事件：
//     Layout → Repaint → 鼠标/键盘事件 → ...
//   你写的 GUI 代码在每个事件类型下都执行一遍，
//   Layout 阶段计算位置，Repaint 阶段实际绘制。
//
// 💡 GUI.changed 模式：
//   GUI.changed = true 表示用户与某个控件交互了。
//   每次调用控件前应重置 GUI.changed = false，
//   调用后检查 GUI.changed 是否为 true 来响应用户操作。
//
// 📍 对应 C++ 头文件：Modules/IMGUI/GUI.bindings.h
// ==============================================================

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // GUI — 即时模式 GUI 核心类（C++ 绑定部分）
    //
    // 🔑 关键全局状态属性：
    //   - color / backgroundColor / contentColor：绘图颜色
    //   - changed：用户是否交互了
    //   - enabled：是否启用控件交互
    //   - depth：绘制深度排序
    //
    // 🔑 关键方法：
    //   - DragWindow：拖拽窗口
    //   - FocusWindow / UnfocusWindow：窗口焦点管理
    //   - ModalWindow / DoWindow：模态/非模态窗口
    //
    // ⚠️ 注意：
    //   GUI 类的大部分方法（如 Label, Button, TextField 等）
    //   在 GUI.cs 中实现（非 bindings），这里只有 C++ 绑定部分。
    // ==============================================================
    [NativeHeader("Modules/IMGUI/GUI.bindings.h"),
     NativeHeader("Modules/IMGUI/GUISkin.bindings.h")]
    partial class GUI
    {
        // ==============================================================
        // 全局绘图颜色状态
        //
        // 🎯 这些颜色属性影响后续所有 GUI 绘制调用。
        //
        //   color — 控件整体色调乘法颜色（影响背景+文字）
        //   backgroundColor — 控件背景颜色
        //   contentColor — 控件文字/内容颜色
        //
        // 💡 典型用法：
        //   GUI.color = Color.red;  // 接下来所有控件变红
        //   GUI.Button(...);
        //   GUI.color = Color.white; // 恢复默认
        //
        // ⚠️ 这些是全局状态，会污染后续所有绘制！
        //    使用 GUI.backgroundColor = Color.yellow { ... } 模式
        //    临时修改后要恢复。
        // ==============================================================
        public static extern Color color { get; set; }
        public static extern Color backgroundColor { get; set; }
        public static extern Color contentColor { get; set; }

        // ==============================================================
        // GUI.changed — 用户交互检测标志
        //
        // 🎯 核心交互检测模式：
        //   GUI.changed = false;           // 1. 重置
        //   name = GUI.TextField(name);    // 2. 调用控件
        //   if (GUI.changed)               // 3. 检查变化
        //       Debug.Log("用户修改了输入");
        //
        // 💡 每个 GUI 控件在用户操作时会设置 changed = true。
        //    这样你不需要单独追踪每个控件的状态。
        //
        // ⚠️ changed 是全局的，任何控件的变化都会触发！
        // ==============================================================
        public static extern bool changed { get; set; }

        // ==============================================================
        // GUI.enabled — 控件交互开关
        //
        // 🎯 设为 false 后，所有控件变为灰色且不可交互。
        //    常用于条件禁用某些控件。
        //
        // 💡 典型用法：
        //   GUI.enabled = canInteract;
        //   GUI.Button(...); // 只有 canInteract=true 时可点击
        //   GUI.enabled = true; // 恢复
        // ==============================================================
        public static extern bool enabled { get; set; }

        // ==============================================================
        // GUI.depth — 绘制深度/排序
        //
        // 🎯 控制 GUI 控件的渲染顺序（z-order）。
        //    值越大越在前面（最后绘制的在最上面）。
        //    用于确保不同面板的 GUI 正确层叠。
        // ==============================================================
        public static extern int depth { get; set; }

        // ==============================================================
        // 内部渲染材质
        //
        // ⚡ 这些材质用于 IMGUI 的底层渲染：
        //   - blendMaterial：标准 GUI 混合渲染
        //   - blitMaterial：纹理拷贝（GUI.DrawTexture 使用）
        //   - roundedRectMaterial：圆角矩形绘制
        //   - roundedRectWithColorPerBorderMaterial：每边不同颜色的圆角矩形
        //
        // 💡 这些是 C++ 引擎内部使用的材质，开发者通常不需要关心。
        // ==============================================================
        internal static extern bool usePageScrollbars { get; }
        internal static extern bool isInsideList { get; set; }
        internal static extern Material blendMaterial {[FreeFunction("GetGUIBlendMaterial")] get; }
        internal static extern Material blitMaterial {[FreeFunction("GetGUIBlitMaterial")] get; }
        internal static extern Material roundedRectMaterial {[FreeFunction("GetGUIRoundedRectMaterial")] get; }
        internal static extern Material roundedRectWithColorPerBorderMaterial { [FreeFunction("GetGUIRoundedRectWithColorPerBorderMaterial")] get; }

        // ==============================================================
        // 鼠标控件抓取机制
        //
        // 🎯 用于实现拖拽等交互——"抓住"鼠标直到释放。
        //   GrabMouseControl(id)  — 抓取鼠标到指定控件
        //   HasMouseControl(id)   — 检查鼠标是否被指定控件控制
        //   ReleaseMouseControl() — 释放鼠标控制
        //
        // 💡 典型场景：窗口拖拽、滑块拖动时，
        //    需要"锁住"鼠标不被其他控件抢走。
        // ==============================================================
        internal static extern void GrabMouseControl(int id);
        internal static extern bool HasMouseControl(int id);
        internal static extern void ReleaseMouseControl();

        // ==============================================================
        // 焦点/键盘控制管理
        //
        // 🎯 IMGUI 使用名称来追踪焦点控件：
        //   SetNextControlName(name)     — 给下一个创建的控件命名
        //   GetNameOfFocusedControl()    — 获取当前焦点控件名
        //   FocusControl(name)           — 将焦点设到指定名称的控件
        //
        // 💡 控件名是 IMGUI 键盘导航（Tab 切换焦点）的基础。
        //    类似于 HTML 中 <input name="..."> 的概念。
        // ==============================================================
        [FreeFunction("GetGUIState().SetNameOfNextControl")]
        public static extern void SetNextControlName(string name);

        [FreeFunction("GetGUIState().GetNameOfFocusedControl")]
        public static extern string GetNameOfFocusedControl();

        [FreeFunction("GetGUIState().FocusKeyboardControl")]
        public static extern void FocusControl(string name);

        // ==============================================================
        // 编辑器窗口重绘
        //
        // 📌 仅在编辑器中使用，强制重绘编辑器窗口。
        //    运行时不需要此方法。
        // ==============================================================
        internal static extern void InternalRepaintEditorWindow();

        // ==============================================================
        // Tooltip（工具提示）系统
        //
        // 🎯 IMGUI 内置的 Tooltip 机制：
        //   Internal_GetTooltip()     — 获取当前 Tooltip 文本
        //   Internal_SetTooltip()     — 设置全局 Tooltip
        //   Internal_GetMouseTooltip() — 获取鼠标悬停处的 Tooltip
        //
        // 💡 在 GUIContent 中设置 tooltip 字段，
        //    当鼠标悬停时引擎自动显示。
        // ==============================================================
        private static extern string Internal_GetTooltip();
        private static extern void Internal_SetTooltip(string value);
        private static extern string Internal_GetMouseTooltip();

        // ==============================================================
        // 窗口系统（Modal/Regular Window）
        //
        // 🎯 IMGUI 的窗口是浮动面板，可以拖拽、调整大小。
        //
        //   ModalWindow  — 模态窗口（阻断其他 GUI 交互）
        //   Internal_DoWindow — 普通窗口
        //
        // ⚡ 窗口回调 (WindowFunction) 模式：
        //   GUI.Window(id, rect, DrawWindow, "标题");
        //   void DrawWindow(int id) { 在此绘制窗口内容 }
        //
        // 💡 每个窗口有唯一 ID（int），通过 id 参数传递。
        //    引擎需要 EntityId 来追踪窗口所属的实体对象。
        // ==============================================================
        private static extern Rect Internal_DoModalWindow(int id, EntityId entityId, Rect clientRect, WindowFunction func, GUIContent content, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] GUIStyle style, System.Object skin);
        private static extern Rect Internal_DoWindow(int id, EntityId entityId, Rect clientRect, WindowFunction func, GUIContent title, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] GUIStyle style, System.Object skin, bool forceRectOnLayout);

        // ==============================================================
        // 窗口管理方法
        //
        // 🎯 窗口操作：
        //   DragWindow()           — 让窗口可被拖拽
        //   BringWindowToFront()   — 将窗口移到最前
        //   BringWindowToBack()    — 将窗口移到最后
        //   FocusWindow()          — 聚焦到指定窗口
        //   UnfocusWindow()        — 取消当前聚焦的窗口
        //
        // ⚠️ windowID 是 int 类型，与 Window(id,...) 中的 id 对应。
        //    BeginWindows/EndWindows 用于批量管理所有窗口。
        // ==============================================================
        public static extern void DragWindow(Rect position);
        public static extern void BringWindowToFront(int windowID);
        public static extern void BringWindowToBack(int windowID);
        public static extern void FocusWindow(int windowID);
        public static extern void UnfocusWindow();
        private static extern void Internal_BeginWindows();
        private static extern void Internal_EndWindows();

        // ==============================================================
        // GUIContent 拼接（内部辅助）
        //
        // 📌 用于将两个 GUIContent 合并，
        //    主要在 GUI 内部处理多行文本时使用。
        // ==============================================================
        internal static extern string Internal_Concatenate(GUIContent first, GUIContent second);
    }
}
