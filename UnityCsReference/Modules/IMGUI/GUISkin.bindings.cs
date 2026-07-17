// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GUISkin / GUISettings — IMGUI 皮肤系统与全局设置
//
// 📌 作用：
//   GUISkin 是 IMGUI 的"皮肤"概念——一组预定义的 GUIStyle 集合。
//   它定义了所有 IMGUI 控件的默认外观。
//
// 🔄 皮肤层级结构：
//   GUI.skin → GUISkin (ScriptableObject)
//     ├── button    → GUIStyle（按钮样式）
//     ├── label     → GUIStyle（标签样式）
//     ├── textField → GUIStyle（文本输入框样式）
//     ├── textArea  → GUIStyle（多行文本样式）
//     ├── toggle    → GUIStyle（开关样式）
//     ├── window    → GUIStyle（窗口样式）
//     ├── box       → GUIStyle（盒子样式）
//     ├── horizontalScrollbar → GUIStyle
//     ├── verticalScrollbar   → GUIStyle
//     ├── horizontalSlider    → GUIStyle
//     ├── verticalSlider      → GUIStyle
//     ├── scrollView          → GUIStyle
//     ├── horizontalThumb     → GUIStyle
//     └── verticalThumb       → GUIStyle
//
// 💡 皮肤切换：
//   GUI.skin = myCustomSkin;  // 切换到自定义皮肤
//   // ... 绘制控件（使用新皮肤的样式）
//   GUI.skin = null;          // 恢复默认皮肤
//
// ⚡ 内置皮肤：
//   - "Dark" 皮肤（编辑器默认暗色主题）
//   - "Light" 皮肤（编辑器亮色主题）
//   运行时也有默认皮肤，可通过 GUISkin 构造函数创建。
//
// 📍 对应 C++ 头文件：Modules/IMGUI/GUISkin.bindings.h
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // GUISettings — IMGUI 全局设置（光标闪烁速度等）
    //
    // 🎯 控制 IMGUI 系统的全局行为参数。
    //   主要暴露给 Editor 内部使用。
    //
    // 💡 GUISettings 是 GUISkin 的配置来源：
    //   GUISkin 引用 GUISettings 来获取全局设置。
    //   运行时使用默认 GUISettings。
    // ==============================================================
    [NativeHeader("Modules/IMGUI/GUISkin.bindings.h")]
    partial class GUISettings
    {
        // ==============================================================
        // 光标闪烁速度
        //
        // 📌 Internal_GetCursorFlashSpeed — 获取文本光标闪烁频率。
        //    控制 TextField 中 | 光标的闪烁速度（秒/次）。
        //    默认约 0.5 秒闪烁一次。
        // ==============================================================
        private static extern float Internal_GetCursorFlashSpeed();
    }
}
