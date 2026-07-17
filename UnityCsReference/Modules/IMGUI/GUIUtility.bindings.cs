// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GUIUtility — IMGUI 工具类：控件 ID、坐标转换、输入管理
//
// 📌 作用：
//   GUIUtility 提供 IMGUI 系统的底层工具方法。
//   它不直接绘制 UI，而是管理：
//   - 控件 ID（ControlID）：每个 IMGUI 控件的唯一标识
//   - 坐标转换：Screen ↔ GUI ↔ Window 坐标系
//   - 输入状态：键盘焦点、鼠标控制、IME 输入
//   - 全局状态：模态窗口检测、剪贴板、changed 标志
//
// 🔄 坐标系概念：
//   Screen Point — 屏幕像素坐标（左下角原点）
//   GUI Point   — GUI 坐标（可能有缩放，如 Retina 屏幕）
//   Window Point — 窗口内的局部坐标
//
// 💡 ControlID 是 IMGUI 的核心概念：
//   每个控件（Button, TextField 等）都有唯一的 int ID。
//   引擎通过 ID 追踪焦点、热控件（鼠标按下的控件）等。
//   ID 通过 hash(hint + focusType + rect) 计算。
//
// ⚡ GUIUtility 控件 ID 机制：
//   GetControlID(hint, focusType, rect) — 获取/创建控件 ID
//   hotControl — 当前被鼠标按下的控件 ID
//   keyboardControl — 当前键盘聚焦的控件 ID
//
// 📍 对应 C++ 头文件：Modules/IMGUI/GUIUtility.h
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // GUIUtility — IMGUI 工具类（C++ 绑定部分）
    //
    // 🔑 关键公共方法：
    //   - GetControlID：获取控件唯一 ID
    //   - AlignRectToDevice：将矩形对齐到像素网格
    //   - systemCopyBuffer：系统剪贴板读写
    //   - hasModalWindow：是否有模态窗口打开
    //
    // 💡 GUIUtility 的方法大多在 GUIUtility.cs 中有更高层的
    //    C# 包装，这里只有 C++ native 绑定。
    // ==============================================================
    [NativeHeader("Modules/IMGUI/GUIUtility.h"),
     NativeHeader("Modules/IMGUI/GUIManager.h"),
     NativeHeader("Runtime/Input/InputBindings.h"),
     NativeHeader("Runtime/Input/InputManager.h"),
     NativeHeader("Runtime/Camera/RenderLayers/GUITexture.h"),
     NativeHeader("Runtime/Utilities/CopyPaste.h")]
    public partial class GUIUtility
    {
        // ==============================================================
        // 模态窗口检测
        //
        // 🎯 hasModalWindow — 检查当前是否有 IMGUI 模态窗口打开。
        //    模态窗口会阻断其他 GUI 控件的交互，
        //    类似于对话框遮罩效果。
        // ==============================================================
        public static extern bool hasModalWindow { get; }

        // ==============================================================
        // 像素密度与 DPI 适配
        //
        // 🎯 pixelsPerPoint — 每逻辑点对应的像素数。
        //    在 Retina/HiDPI 屏幕上，这个值 > 1。
        //    用于在高 DPI 屏幕上正确计算 IMGUI 控件大小。
        //
        // 💡 仅在 Editor 和 Canvas GUI 模式下有效。
        //    UIElementsModule 可见——UI Toolkit 也使用此值。
        // ==============================================================
        [NativeProperty("GetGUIState().m_PixelsPerPoint", true, TargetType.Field)]
        internal static extern float pixelsPerPoint
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.UIToolkitAuthoringModule")]
            get;

            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            set;
        }

        // ==============================================================
        // GUI 递归深度
        //
        // 🎯 guiDepth — 当前 OnGUI 调用的递归深度。
        //    每次 BeginGUI/EndGUI 嵌套会增加/减少。
        //    用于检测 GUI 嵌套错误。
        // ==============================================================
        [NativeProperty("GetGUIState().m_OnGUIDepth", true, TargetType.Field)]
        internal static extern int guiDepth
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            get;
        }

        // ==============================================================
        // 屏幕/GUI 坐标偏移
        //
        // 🎯 s_EditorScreenPointOffset — 屏幕坐标到 GUI 坐标的偏移量。
        //    在编辑器中，GUI 坐标系的原点可能不在屏幕左下角。
        //    例如 Scene View 的 GUI 原点是该视口的左下角。
        //
        // 💡 这是 GUIUtility.GUIToScreenPoint / ScreenToGUIPoint
        //    内部实现的一部分。
        // ==============================================================
        internal static extern Vector2 s_EditorScreenPointOffset
        {
            [NativeMethod("GetGUIState().GetGUIPixelOffset", true)]
            get;
            [NativeMethod("GetGUIState().SetGUIPixelOffset", true)]
            set;
        }

        // ==============================================================
        // 鼠标/输入占用标记
        //
        // 🎯 mouseUsed — 是否有控件正在使用鼠标。
        //    用于避免多个 GUI 系统（如 IMGUI + UI Toolkit）
        //    同时响应鼠标事件。
        //
        // 🎯 textFieldInput — 是否有文本输入框获得焦点。
        //    影响输入法（IME）的激活状态。
        // ==============================================================
        [NativeProperty("GetGUIState().m_CanvasGUIState.m_IsMouseUsed", true, TargetType.Field)]
        internal static extern bool mouseUsed { get; set; }

        [StaticAccessor("GetInputManager()", StaticAccessorType.Dot)]
        internal static extern bool textFieldInput { get; set; }

        // ==============================================================
        // GUITexture SRGB 控制（内部）
        //
        // 📌 用于控制 GUITexture 组件的 sRGB 颜色空间转换。
        //    已废弃的 GUITexture 组件的兼容性保留。
        // ==============================================================
        internal static extern bool manualTex2SRGBEnabled
        {
            [FreeFunction("GUITexture::IsManualTex2SRGBEnabled")] get;
            [FreeFunction("GUITexture::SetManualTex2SRGBEnabled")] set;
        }

        // ==============================================================
        // 系统剪贴板
        //
        // 🎯 systemCopyBuffer — 读写系统剪贴板。
        //
        // 💡 典型用法：
        //   string copied = GUIUtility.systemCopyBuffer; // 读取
        //   GUIUtility.systemCopyBuffer = "Hello";        // 写入
        //
        // ⚠️ 这个属性跨平台，但行为可能因 OS 而异。
        //    macOS 上最大 16KB，Windows/Linux 无严格限制。
        // ==============================================================
        public static extern string systemCopyBuffer
        {
            [FreeFunction("GetCopyBuffer")] get;
            [FreeFunction("SetCopyBuffer")] set;
        }

        // ==============================================================
        // ControlID 管理 — IMGUI 控件的唯一标识系统
        //
        // 🎯 每个 IMGUI 控件需要一个唯一的 ControlID：
        //   GetControlID(hint, focusType, rect) — 获取控件 ID
        //
        // 🔧 ID 生成机制：
        //   - hint：整数提示值（通常用控件类型 hash）
        //   - focusType：None/Forward/Keyboard（焦点类型）
        //   - rect：控件位置（用于区分同一 hint 的不同实例）
        //   引擎内部对 (hint, focusType, rect) 进行 hash 得到 ID
        //
        // 💡 ReorderableList 依赖此计数器追踪控件数量变化。
        //    每帧控件数量变化时需要重新缓存元素。
        // ==============================================================
        [FreeFunction("GetGUIState().GetControlID")]
        static extern int Internal_GetControlID(int hint, FocusType focusType, Rect rect);

        // Control counting is required by ReorderableList. Element rendering callbacks can change and use
        // different number of controls to represent an element each frame. We need a way to be able to track
        // if the control count changed from the last frame so we can recache those elements.
        internal static int s_ControlCount = 0;
        public static int GetControlID(int hint, FocusType focusType, Rect rect)
        {
            s_ControlCount++;
            return Internal_GetControlID(hint, focusType, rect);
        }

        // ==============================================================
        // ObjectGUIState 容器管理
        //
        // 🎯 用于 UIElements (UI Toolkit) 与 IMGUI 的桥接。
        //   BeginContainerFromOwner(ScriptableObject) — 从所有者开启容器
        //   BeginContainer(ObjectGUIState) — 从 GUI 状态开启容器
        //   Internal_EndContainer() — 结束容器
        //
        // 💡 UI Toolkit 使用 ObjectGUIState 包装 IMGUI 状态，
        //    使得两种 UI 系统可以共存。
        // ==============================================================
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void BeginContainerFromOwner(ScriptableObject owner);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void BeginContainer(ObjectGUIState objectGUIState);

        [NativeMethod("EndContainer")]
        internal static extern void Internal_EndContainer();

        // ==============================================================
        // 永久控件 ID
        //
        // 📌 获取一个不会与其他控件冲突的永久唯一 ID。
        //    用于内部需要持久 ID 的场景。
        // ==============================================================
        [FreeFunction("GetSpecificGUIState(0).m_EternalGUIState->GetNextUniqueID")]
        internal static extern int GetPermanentControlID();

        // ==============================================================
        // Undo 系统集成
        //
        // 📌 通知 Undo 系统更新当前操作名称。
        //    IMGUI 编辑器控件在修改值时会调用此方法。
        // ==============================================================
        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern void UpdateUndoName();

        // ==============================================================
        // Tab 键事件与键盘焦点管理
        //
        // 🎯 Tab 键导航支持（为 UIElements 提供）：
        //   CheckForTabEvent(evt) — 检测并处理 Tab 事件
        //   SetKeyboardControlToFirstControlId() — 焦点到第一个控件
        //   SetKeyboardControlToLastControlId() — 焦点到最后一个控件
        //   HasFocusableControls() — 是否有可聚焦控件
        //   OwnsId(id) — 检查指定 ID 是否属于当前容器
        //
        // 💡 这些方法让 UIElements 能利用 IMGUI 的焦点管理机制。
        // ==============================================================
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern int CheckForTabEvent(Event evt);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void SetKeyboardControlToFirstControlId();

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void SetKeyboardControlToLastControlId();

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern bool HasFocusableControls();

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern bool OwnsId(int id);

        // ==============================================================
        // 控件矩形对齐到设备像素
        //
        // 🎯 AlignRectToDevice — 将矩形对齐到最近的像素边界。
        //    返回对齐后的宽高（像素值），避免模糊渲染。
        //
        // 💡 在 HiDPI 屏幕上，非整数像素坐标会导致文字模糊。
        //    此方法确保控件边缘对齐到物理像素。
        // ==============================================================
        public static extern Rect AlignRectToDevice(Rect rect, out int widthInPixels, out int heightInPixels);

        // ==============================================================
        // IME（输入法）支持
        //
        // 🎯 输入法编辑器状态管理：
        //   compositionString — 当前正在输入的组合字符串
        //   imeCompositionMode — IME 模式（Auto/On/Off/OSNative）
        //   compositionCursorPos — IME 选字框的显示位置
        //
        // 💡 中文/日文/韩文等语言输入需要 IME 支持。
        //    当用户在 TextField 中输入时，需要正确定位选字框。
        //
        // ⚠️ 这些属性通过 InputBindings 静态访问器实现，
        //    因为 Input 模块依赖关系需要反转。
        // ==============================================================
        [StaticAccessor("InputBindings", StaticAccessorType.DoubleColon)]
        internal extern static string compositionString
        {
            get;
        }

        [StaticAccessor("InputBindings", StaticAccessorType.DoubleColon)]
        internal extern static IMECompositionMode imeCompositionMode
        {
            get;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            set;
        }

        [StaticAccessor("InputBindings", StaticAccessorType.DoubleColon)]
        internal extern static Vector2 compositionCursorPos
        {
            get;
            set;
        }

        // ==============================================================
        // 矩阵乘法辅助（坐标变换）
        //
        // 📌 内部辅助方法：用变换矩阵乘以点坐标。
        //    用于 GUIClip 等坐标转换场景。
        //    ⚠️ 在敏感的对齐操作中使用，尽量避免直接调用。
        // ==============================================================
        internal static extern Vector3 Internal_MultiplyPoint(Vector3 point, Matrix4x4 transform);

        // ==============================================================
        // GUI.changed 标志的底层访问
        //
        // 🎯 GetChanged / SetChanged — GUI.changed 的 C++ 实现。
        //    还有 SetDidGUIWindowsEatLastEvent 用于追踪
        //    窗口是否消费了最近的事件。
        // ==============================================================
        internal static extern bool GetChanged();
        internal static extern void SetChanged(bool changed);
        internal static extern void SetDidGUIWindowsEatLastEvent(bool value);

        // ==============================================================
        // HotControl / KeyboardControl — 控件焦点底层 API
        //
        // 🎯 IMGUI 焦点系统的底层实现：
        //   hotControl     — 鼠标当前按住的控件 ID（拖拽时锁定）
        //   keyboardControl — 键盘聚焦的控件 ID（接受键盘输入）
        //
        // 💡 这是 GUI.GrabMouseControl / FocusControl 的底层实现。
        //    一般开发者不需要直接调用这些方法。
        //
        // 🔄 焦点转移流程：
        //   1. 鼠标点击 → 设置 hotControl = 点击的控件 ID
        //   2. 鼠标释放 → hotControl = 0
        //   3. Tab 键 → keyboardControl 转移到下一个控件
        //   4. 鼠标点击 → keyboardControl = 点击的控件 ID
        // ==============================================================
        private static extern int Internal_GetHotControl();
        private static extern int Internal_GetKeyboardControl();
        private static extern void Internal_SetHotControl(int value);
        private static extern void Internal_SetKeyboardControl(int value);

        // ==============================================================
        // 默认 GUISkin 获取
        //
        // 🎯 IMGUI 的皮肤（Skin）系统：
        //   Internal_GetDefaultSkin(skinMode) — 获取默认 GUI 皮肤
        //   Internal_GetBuiltinSkin(skin) — 获取内置皮肤
        //     skin = 0 → "dark" 暗色主题（编辑器默认）
        //     skin = 1 → "light" 亮色主题
        //
        // 💡 GUISkin 包含所有 GUIStyle 的预设，
        //    如 button、label、textField 等控件的样式。
        //    这是 IMGUI 外观定制的核心。
        // ==============================================================
        private static extern System.Object Internal_GetDefaultSkin(int skinMode);
        private static extern Object Internal_GetBuiltinSkin(int skin);

        // ==============================================================
        // 退出 GUI 系统
        //
        // 📌 Internal_ExitGUI — 立即退出当前 OnGUI 调用。
        //    通常在检测到致命错误或需要中止 GUI 渲染时调用。
        //    ⚠️ 不要常规使用，这会中断 GUI 渲染流程。
        // ==============================================================
        private static extern void Internal_ExitGUI();

        // ==============================================================
        // 坐标系转换（Window ↔ Screen）
        //
        // 🎯 Window 坐标 ↔ Screen 坐标的转换：
        //   InternalWindowToScreenPoint(windowPoint) — 窗口坐标 → 屏幕坐标
        //   InternalScreenToWindowPoint(screenPoint) — 屏幕坐标 → 窗口坐标
        //
        // 💡 三个坐标系的关系：
        //   Screen Point: 整个屏幕的像素坐标（左下角原点）
        //   GUI Point:    GUI 坐标系（可能有缩放）
        //   Window Point: 相对于窗口内容区域的坐标
        //
        // ⚠️ 这些是内部方法，公开 API 是
        //    GUIUtility.ScreenToGUIPoint / GUIToScreenPoint。
        // ==============================================================
        private static extern Vector2 InternalWindowToScreenPoint(Vector2 windowPoint);
        private static extern Vector2 InternalScreenToWindowPoint(Vector2 screenPoint);
    }
}
