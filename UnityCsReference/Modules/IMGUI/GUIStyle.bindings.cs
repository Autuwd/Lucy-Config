// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GUIStyle / GUIStyleState — IMGUI 控件的外观样式系统
//
// 📌 作用：
//   GUIStyle 定义了 IMGUI 控件的视觉外观：
//   - 字体、字号、字体样式（粗体/斜体）
//   - 文本对齐、换行、裁剪
//   - 背景图片（9-slice 切片渲染）
//   - 正常/悬停/按下/禁用 四种状态的样式
//   - 内边距、外边距、内容偏移
//
// 🔄 9-Slice 渲染原理：
//   GUIStyle 的背景图片使用 9-slice（九宫格）技术：
//   ┌───┬───┬───┐
//   │ 角 │ 边 │ 角 │  四角不缩放
//   ├───┼───┼───┤
//   │ 边 │ 中 │ 边 │  边缘单方向拉伸
//   ├───┼───┼───┤
//   │ 角 │ 边 │ 角 │  中心双向拉伸
//   └───┴───┴───┘
//   通过 4 个 RectOffset（border）定义切片位置。
//   这样可以用小图片拉伸出任意大小的背景。
//
// 💡 GUIStyle vs GUISkin：
//   GUIStyle = 单个控件的样式（如"按钮样式"）
//   GUISkin   = 一组 GUIStyle 的集合（如"默认皮肤"）
//   GUISkin 包含 button、label、toggle 等所有控件的 GUIStyle。
//
// ⚡ 代理模式（代理样式）：
//   GUIStyle 可以有"normal"、"hover"、"onNormal"等状态。
//   引擎根据控件状态自动选择对应的 GUIStyleState 进行渲染。
//
// 📍 对应 C++ 头文件：Modules/IMGUI/GUIStyle.bindings.h
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    // ==============================================================
    // GUIStyleState — 单个状态下的样式定义
    //
    // 🎯 每个 GUIStyle 有多个状态，每个状态是一个 GUIStyleState：
    //   - normal:    正常状态
    //   - hover:     鼠标悬停
    //   - active:    鼠标按下
    //   - focused:   键盘聚焦
    //   - onNormal:  选中状态（如 Toggle 开启时的正常状态）
    //   - onHover:   选中 + 悬停
    //   - onActive:  选中 + 按下
    //   - onFocused: 选中 + 聚焦
    //
    // 🔑 每个状态包含：
    //   - background: 背景纹理（Texture2D）
    //   - textColor: 文字颜色
    //   - scaledBackgrounds: 高 DPI 下的多分辨率背景
    //     （如 @1x, @2x, @3x 对应不同设备像素密度）
    //
    // 💡 scaledBackgrounds 是 9-slice 渲染的关键：
    //   引擎根据 pixelsPerPoint 选择合适分辨率的背景纹理。
    // ==============================================================
    [NativeHeader("Modules/IMGUI/GUIStyle.bindings.h")]
    partial class GUIStyleState
    {
        // ==============================================================
        // 背景纹理
        //
        // 🎯 background — 此状态下的背景图片。
        //    使用 9-slice 拉伸渲染到控件背景区域。
        //    如果为空，只绘制纯色。
        // ==============================================================
        [NativeProperty("Background", false, TargetType.Function)] public extern Texture2D background { get; set; }

        // ==============================================================
        // 文字颜色
        //
        // 🎯 textColor — 此状态下文字的颜色。
        //    正常情况下是白色（由 GUI.contentColor 全局乘法调整）。
        // ==============================================================
        [NativeProperty("textColor", false, TargetType.Field)] public extern Color textColor { get; set; }

        // ==============================================================
        // 多分辨率背景（HiDPI 适配）
        //
        // 🎯 scaledBackgrounds — 按 DPI 缩放选择的背景纹理数组。
        //
        // 💡 例如 scaledBackgrounds = [1x纹理, 2x纹理, 3x纹理]
        //   pixelsPerPoint=1 时用 1x
        //   pixelsPerPoint=2 时用 2x
        //   这确保在 Retina 屏幕上背景图片清晰。
        // ==============================================================
        [NativeProperty("scaledBackgrounds", false, TargetType.Function)]
        public extern Texture2D[] scaledBackgrounds { get; set; }

        // ==============================================================
        // 原生对象生命周期管理
        //
        // 📌 Init() — 在 C++ 端创建 GUIStyleState 对象
        //    Cleanup() — 释放 C++ 端的内存
        //    BindingsMarshaller — C#/C++ 对象指针转换辅助
        // ==============================================================
        [FreeFunction(Name = "GUIStyleState_Bindings::Init", IsThreadSafe = true)] private static extern IntPtr Init();
        [FreeFunction(Name = "GUIStyleState_Bindings::Cleanup", IsThreadSafe = true, HasExplicitThis = true)] private extern void Cleanup();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(GUIStyleState guiStyleState) => guiStyleState.m_Ptr;
        }
    }

    // ==============================================================
    // GUIStyle — IMGUI 控件的完整样式定义
    //
    // 🎯 核心职责：
    //   1. 定义控件的文字属性（字体、大小、对齐、换行）
    //   2. 定义控件的背景渲染（9-slice 纹理）
    //   3. 定义控件的尺寸属性（固定/拉伸宽高）
    //   4. 提供 Draw 方法绘制控件到指定位置
    //   5. 提供 CalcSize/CalcHeight 计算控件所需大小
    //
    // 📌 关键属性分类：
    //   文字属性：font, fontSize, fontStyle, alignment, richText
    //   布局属性：fixedWidth/Height, stretchWidth/Height
    //   渲染属性：normal/hover/active/focused 状态样式
    //   间距属性：margin, padding, overflow (RectOffset)
    //
    // ⚠️ GUIStyle 是引用类型（有 m_Ptr 指向 C++ 对象），
    //    不要频繁创建，尽量复用或从 GUISkin 获取。
    // ==============================================================
    [RequiredByNativeCode]
    [NativeHeader("Modules/IMGUI/GUIStyle.bindings.h")]
    [NativeHeader("IMGUIScriptingClasses.h")]
    partial class GUIStyle
    {
        // ==============================================================
        // 样式名称与关联字体
        //
        // 🎯 rawName — 样式的内部名称（如 "button", "label"）。
        //    GUISkin 通过名称查找对应的 GUIStyle。
        //
        // 🎯 font — 此样式使用的字体。
        //    设为 null 则使用 GUISkin 的默认字体。
        // ==============================================================
        [NativeProperty("Name", false, TargetType.Function)] internal extern string rawName { get; set; }
        [NativeProperty("Font", false, TargetType.Function)] public extern Font font { get; set; }

        // ==============================================================
        // 文字布局属性
        //
        // 🎯 控制文字在控件内的排列方式：
        //   imagePosition — 图片与文字的位置关系（Left/Right/Top/Only）
        //   alignment     — 文字对齐（TopLeft/TopCenter/.../BottomRight 共9种）
        //   wordWrap      — 是否自动换行
        //   clipping      — 文字超出时的处理（Clip/Overflow）
        //   richText      — 是否支持富文本标签（<b>, <i>, <color> 等）
        //
        // 💡 alignment 使用 TextAnchor 枚举：
        //   TopLeft, TopCenter, TopCenter,
        //   MiddleLeft, MiddleCenter, MiddleRight,
        //   BottomLeft, BottomCenter, BottomRight
        // ==============================================================
        [NativeProperty("m_ImagePosition", false, TargetType.Field)] public extern ImagePosition imagePosition { get; set; }
        [NativeProperty("m_Alignment", false, TargetType.Field)] public extern TextAnchor alignment { get; set; }
        [NativeProperty("m_WordWrap", false, TargetType.Field)] public extern bool wordWrap { get; set; }
        [NativeProperty("m_Clipping", false, TargetType.Field)] public extern TextClipping clipping { get; set; }

        // ==============================================================
        // 内容偏移与间距
        //
        // 🎯 contentOffset — 内容区域相对于控件矩形的偏移。
        //    可以让文字"浮出"控件边界。
        //   contentSpacing — 内容元素之间的间距。
        // ==============================================================
        [NativeProperty("m_ContentOffset", false, TargetType.Field)] public extern Vector2 contentOffset { get; set; }
        [NativeProperty("m_ContentSpacing", false, TargetType.Field)] internal extern float contentSpacing { get; set; }

        // ==============================================================
        // 尺寸控制
        //
        // 🎯 固定尺寸 vs 拉伸尺寸：
        //   fixedWidth/Height  — 固定宽高（0 表示不固定）
        //   stretchWidth/Height — 是否允许自动拉伸
        //
        // 💡 组合逻辑：
        //   fixedWidth=100, stretchWidth=false → 始终 100 像素宽
        //   fixedWidth=0,   stretchWidth=true  → 自适应内容宽度
        //   fixedWidth=100, stretchWidth=true  → 最小 100，可拉伸
        //
        // ⚠️ 固定尺寸优先于拉伸尺寸。
        // ==============================================================
        [NativeProperty("m_FixedWidth", false, TargetType.Field)] public extern float fixedWidth { get; set; }
        [NativeProperty("m_FixedHeight", false, TargetType.Field)] public extern float fixedHeight { get; set; }
        [NativeProperty("m_StretchWidth", false, TargetType.Field)] public extern bool stretchWidth { get; set; }
        [NativeProperty("m_StretchHeight", false, TargetType.Field)] public extern bool stretchHeight { get; set; }

        // ==============================================================
        // 字体设置
        //
        // 🎯 fontSize — 字号（像素）。
        //    fontStyle — 字体样式（Normal/Bold/Italic/BoldAndItalic）
        // ==============================================================
        [NativeProperty("m_FontSize", false, TargetType.Field)] public extern int fontSize { get; set; }
        [NativeProperty("m_FontStyle", false, TargetType.Field)] public extern FontStyle fontStyle { get; set; }

        // ==============================================================
        // 富文本与文本渲染
        //
        // 🎯 richText — 是否解析 HTML 风格的富文本标签。
        //   imageIsTopAligned — 图片是否顶部对齐（内部属性）
        //   isSDF — 是否使用 SDF（Signed Distance Field）字体渲染。
        //     SDF 渲染支持无损缩放，常用于 UI Toolkit。
        // ==============================================================
        [NativeProperty("m_RichText", false, TargetType.Field)] public extern bool richText { get; set; }
        [NativeProperty("m_ImageIsTopAligned", false, TargetType.Field)] internal extern bool imageIsTopAligned { get; set; }
        [NativeProperty("m_IsSDF", false, TargetType.Field)] internal extern bool isSDF { get; set; }

        // ==============================================================
        // clipOffset（已废弃）
        //
        // ⚠️ 已废弃！请使用 BeginGroup 代替。
        //    原来用于控制内容裁剪偏移，现在由 GUIClip 系统管理。
        // ==============================================================
        [Obsolete("Don't use clipOffset - put things inside BeginGroup instead. This functionality will be removed in a later version.", false)]
        [NativeProperty("m_ClipOffset", false, TargetType.Field)] public extern Vector2 clipOffset { get; set; }
        [NativeProperty("m_ClipOffset", false, TargetType.Field)] internal extern Vector2 Internal_clipOffset { get; set; }

        // ==============================================================
        // 原生对象生命周期
        //
        // 📌 GUIStyle 在 C++ 端维护完整的样式数据。
        //   Internal_Create — 分配原生对象
        //   Internal_Copy   — 拷贝另一个 GUIStyle
        //   Internal_Destroy — 释放原生对象
        //
        // ⚠️ IsThreadSafe = true 表示这些方法可以在非主线程调用，
        //    但实际的 Draw 操作必须在主线程。
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Create", IsThreadSafe = true)] private static extern IntPtr Internal_Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] GUIStyle self);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Copy", IsThreadSafe = true)] private static extern IntPtr Internal_Copy([UnityMarshalAs(NativeType.ScriptingObjectPtr)] GUIStyle self, GUIStyle other);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Destroy", IsThreadSafe = true)] private static extern void Internal_Destroy(IntPtr self);

        // ==============================================================
        // GUIStyleState / RectOffset 访问
        //
        // 🎯 通过索引访问不同状态的样式和间距：
        //   GetStyleStatePtr(idx) — 获取指定状态的 GUIStyleState 指针
        //     idx 0=normal, 1=hover, 2=active, 3=focused
        //     4=onNormal, 5=onHover, 6=onActive, 7=onFocused
        //   AssignStyleState(idx, src) — 设置指定状态的样式
        //   GetRectOffsetPtr(idx) — 获取边距偏移指针
        //     idx 0=margin, 1=padding, 2=overflow
        //   AssignRectOffset(idx, src) — 设置边距偏移
        //
        // 💡 RectOffset 包含 left/right/top/bottom 四个值。
        //    margin: 控件外部间距（与其他控件之间）
        //    padding: 控件内部间距（文字与边框之间）
        //    overflow: 允许内容超出控件的范围
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::GetStyleStatePtr", IsThreadSafe = true, HasExplicitThis = true)]
        private extern IntPtr GetStyleStatePtr(int idx);

        [FreeFunction(Name = "GUIStyle_Bindings::AssignStyleState", HasExplicitThis = true)]
        private extern void AssignStyleState(int idx, IntPtr srcStyleState);

        [FreeFunction(Name = "GUIStyle_Bindings::GetRectOffsetPtr", HasExplicitThis = true)]
        private extern IntPtr GetRectOffsetPtr(int idx);

        [FreeFunction(Name = "GUIStyle_Bindings::AssignRectOffset", HasExplicitThis = true)]
        private extern void AssignRectOffset(int idx, IntPtr srcRectOffset);

        // ==============================================================
        // 🎯 控件绘制方法（核心渲染 API）
        //
        // Internal_Draw — 标准绘制（最常用）
        //   screenRect:  绘制位置和大小
        //   content:     要显示的内容（文字+图标+tooltip）
        //   isHover:     鼠标是否悬停
        //   isActive:    是否按下
        //   on:          是否选中状态（Toggle/RepeatButton）
        //   hasKeyboardFocus: 是否有键盘焦点
        //
        // Internal_Draw2 — 简化版绘制（指定 controlID 和 on 状态）
        //   用于不需要手动管理交互状态的场景。
        //
        // Internal_DrawCursor — 带光标闪烁的绘制
        //   用于 TextField/TextArea 中的文字光标。
        //
        // Internal_DrawWithTextSelection — 带文字选择高亮的绘制
        //   用于可选中文本的渲染（选中区域高亮+光标）。
        //
        // ⚡ 这些方法在 C++ 端完成实际的顶点生成和渲染。
        //    C# 端只负责传递参数。
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Draw", HasExplicitThis = true)]
        private extern void Internal_Draw(Rect screenRect, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Draw2", HasExplicitThis = true)]
        private extern void Internal_Draw2(Rect position, GUIContent content, int controlID, bool on);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawCursor", HasExplicitThis = true)]
        private extern void Internal_DrawCursor(Rect position, GUIContent content, Vector2 pos, Color cursorColor);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawWithTextSelection", HasExplicitThis = true)]
        private extern void Internal_DrawWithTextSelection(Rect screenRect, GUIContent content, bool isHover, bool isActive,
            bool on, bool hasKeyboardFocus, bool drawSelectionAsComposition, Vector2 cursorFirstPosition, Vector2 cursorLastPosition, Color cursorColor,
            Color selectionColor);

        // ==============================================================
        // 🎯 内容尺寸计算方法
        //
        // Internal_CalcSize — 计算内容所需的最小尺寸
        //   返回包含文字+图标所需的 Vector2 大小。
        //
        // Internal_CalcSizeWithConstraints — 带约束的尺寸计算
        //   maxSize 限制最大宽度/高度（用于自动换行场景）。
        //
        // Internal_CalcHeight — 给定宽度计算所需高度
        //   用于多行文本：给定宽度，计算需要多少行。
        //
        // Internal_CalcMinMaxWidth — 计算最小和最大宽度
        //   返回 x=最小宽度, y=最大宽度。
        //   布局系统用此来确定控件的宽度范围。
        //
        // 💡 这些方法是 GUILayout 自动布局的基础：
        //   1. 先 CalcSize 获取控件所需空间
        //   2. 再根据 LayoutGroup 规则分配位置
        //   3. 最后 Internal_Draw 绘制
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CalcSize", HasExplicitThis = true)]
        internal extern Vector2 Internal_CalcSize(GUIContent content);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CalcSizeWithConstraints", HasExplicitThis = true)]
        internal extern Vector2 Internal_CalcSizeWithConstraints(GUIContent content, Vector2 maxSize);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CalcHeight", HasExplicitThis = true)]
        private extern float Internal_CalcHeight(GUIContent content, float width);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CalcMinMaxWidth", HasExplicitThis = true)]
        private extern Vector2 Internal_CalcMinMaxWidth(GUIContent content);

        // ==============================================================
        // 序列化支持（跨域传输）
        //
        // 📌 Internal_EnsureCachedScriptingObject — 反序列化后
        //    重新关联 C# 对象和 C++ 原生对象的反向引用。
        //    GUIDebugger 依赖此机制来正确引用 GUIStyle。
        //
        // ManagedSerializationPostDispatchHook — 序列化后的回调，
        //    确保反序列化的 GUIStyle 正确链接到 C++ 端。
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_EnsureCachedScriptingObject", IsThreadSafe = true)]
        private static extern void Internal_EnsureCachedScriptingObject([UnityMarshalAs(NativeType.ScriptingObjectPtr)] GUIStyle self);

        private static void ManagedSerializationPostDispatchHook(object wrapper, IntPtr nativePtr)
        {
            var style = (GUIStyle)wrapper;
            Internal_EnsureCachedScriptingObject(style);
            style.InternalOnAfterDeserialize();
        }

        [RequiredByNativeCode]
        internal static unsafe IntPtr GetGUIStylePostDispatchHookFunctionPointer()
            => (IntPtr)(delegate*<object, IntPtr, void>)&ManagedSerializationPostDispatchHook);

        // ==============================================================
        // 前缀标签与完整内容绘制
        //
        // 🎯 Internal_DrawPrefixLabel — 绘制标签前缀
        //   用于 EditorGUILayout.PropertyField 中的属性标签。
        //   标签绘制在控件左侧。
        //
        // 🎯 Internal_DrawContent — 完整内容绘制（参数最丰富）
        //   支持文字偏移、图片偏移、溢出裁剪等高级功能。
        //   这是 GUI.DrawContent 的底层实现。
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawPrefixLabel", HasExplicitThis = true)]
        private extern void Internal_DrawPrefixLabel(Rect position, GUIContent content, int controlID, bool on);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawContent", HasExplicitThis = true)]
        internal extern void Internal_DrawContent(Rect screenRect, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus,
            bool hasTextInput, bool drawSelectionAsComposition, Vector2 cursorFirst, Vector2 cursorLast, Color cursorColor, Color selectionColor,
            Color imageColor, float textOffsetX, float textOffsetY, float imageTopOffset, float imageLeftOffset, bool overflowX, bool overflowY);

        // ==============================================================
        // 文本矩形偏移计算
        //
        // 📌 Internal_GetTextRectOffset — 计算文字区域相对于控件矩形的偏移。
        //    用于精确定位文字在背景中的位置。
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetTextRectOffset", HasExplicitThis = true)]
        internal extern Vector2 Internal_GetTextRectOffset(Rect screenRect, GUIContent content, Vector2 textSize);

        // ==============================================================
        // Tooltip 管理
        //
        // 🎯 SetMouseTooltip — 设置鼠标悬停时显示的提示文本。
        //   IsTooltipActive — 检查指定 Tooltip 是否正在显示。
        //
        // 💡 与 GUIContent.tooltip 配合使用：
        //   当鼠标悬停在控件上超过一定时间，
        //   系统自动调用 SetMouseTooltip 显示提示。
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::SetMouseTooltip")] internal static extern void SetMouseTooltip(string tooltip, Rect screenRect);
        [FreeFunction(Name = "GUIStyle_Bindings::IsTooltipActive")] internal static extern bool IsTooltipActive(string tooltip);

        // ==============================================================
        // 光标闪烁与默认字体
        //
        // 📌 Internal_GetCursorFlashOffset — 获取光标闪烁偏移量。
        //    用于 TextField 中的文本光标闪烁动画。
        //
        // 📌 SetDefaultFont / GetDefaultFont — 全局默认字体管理。
        //    当 GUIStyle.font 为 null 时使用此默认字体。
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetCursorFlashOffset")] private static extern float Internal_GetCursorFlashOffset();
        [FreeFunction(Name = "GUIStyle::SetDefaultFont")] internal static extern void SetDefaultFont(Font font);
        [FreeFunction(Name = "GUIStyle::GetDefaultFont")] internal static extern Font GetDefaultFont();

        // ==============================================================
        // 文本生成器生命周期管理
        //
        // 📌 文本渲染需要生成网格数据，这些网格数据缓存在 C++ 端。
        //   Internal_DestroyTextGenerator — 销毁指定 mesh 信息
        //   Internal_CleanupAllTextGenerator — 清理所有文本生成器
        //    在样式销毁或场景切换时调用以释放内存。
        // ==============================================================
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DestroyTextGenerator")]
        internal static extern void Internal_DestroyTextGenerator(int meshInfoId);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CleanupAllTextGenerator")]
        internal static extern void Internal_CleanupAllTextGenerator();

        // ==============================================================
        // C#/C++ 对象编组（Marshalling）
        //
        // 📌 BindingsMarshaller — 将 C# GUIStyle 对象转换为
        //    C++ 端的原生指针（m_Ptr），用于跨语言调用。
        // ==============================================================
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(GUIStyle guiStyle) => guiStyle.m_Ptr;
        }
    }
}
