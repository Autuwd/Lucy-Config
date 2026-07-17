// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GUIDebugger — IMGUI 调试器（编辑器专用）
//
// 📌 作用：
//   GUIDebugger 是 Unity 编辑器的 IMGUI 调试工具。
//   它在编辑器中实时显示 IMGUI 控件的布局信息，
//   帮助开发者可视化调试 GUI 布局问题。
//
// 🔄 工作原理：
//   当 Unity 编辑器开启 IMGUI Debugger 窗口时：
//   1. 每次 OnGUI 执行时，引擎会调用这些日志方法
//   2. 记录每个控件的矩形区域、边距、样式等信息
//   3. 在 Debugger 窗口中以可视化方式展示
//
// 💡 使用场景：
//   - 调试 GUILayout 布局（为什么某个控件位置不对？）
//   - 查看控件的 margin/padding 实际值
//   - 分析嵌套布局组的层级结构
//   - 检查 SerializedProperty 在 GUI 中的显示区域
//
// ⚠️ 所有方法标记 [NativeConditional("UNITY_EDITOR")]，
//    表示只在编辑器中有效，构建时完全移除。
//    运行时 Build 不会包含这些调用。
//
// 📍 对应 C++ 头文件：Modules/IMGUI/GUIDebugger.bindings.h
// ==============================================================

using UnityEngine;
using UnityEngine.Bindings;
using System;

namespace UnityEngine
{
    // ==============================================================
    // GUIDebugger — IMGUI 布局调试器（C++ 绑定部分）
    //
    // 🔑 关键方法：
    //   - LogLayoutEntry: 记录单个控件的布局信息
    //   - LogLayoutGroupEntry: 记录布局组的信息
    //   - LogLayoutEndGroup: 记录布局组结束
    //   - LogBeginProperty/LogEndProperty: 记录属性区域
    //   - active: 调试器是否激活
    //
    // 💡 这些方法由 IMGUI 引擎在 Layout/Repaint 阶段
    //    自动调用，开发者通常不需要手动调用。
    // ==============================================================
    [NativeHeader("Modules/IMGUI/GUIDebugger.bindings.h")]
    internal partial class GUIDebugger
    {
        // ==============================================================
        // 布局条目日志记录
        //
        // 🎯 LogLayoutEntry — 记录单个 IMGUI 控件的布局信息。
        //   rect:    控件在屏幕上的矩形位置
        //   left/right/top/bottom: 四个方向的 margin 值
        //   style:   控件使用的 GUIStyle（用于查看样式信息）
        //
        // 💡 当你在 Inspector 中看到一个控件的蓝色虚线框时，
        //    就是 Debug 调试器显示的 LogLayoutEntry 信息。
        //
        // ⚠️ TODO 注释：可以跳过到 Native 的 trip，
        //    如果当前 GUI View 正在被调试的话。
        // ==============================================================
        //TODO: We could skip the trip to native if we check here if the current GUIVIew is being debugged.
        [NativeConditional("UNITY_EDITOR")]
        public static extern void LogLayoutEntry(Rect rect, int left, int right, int top, int bottom, GUIStyle style);

        // ==============================================================
        // 布局组条目日志记录
        //
        // 🎯 LogLayoutGroupEntry — 记录布局组（Horizontal/Vertical）的信息。
        //   参数与 LogLayoutEntry 类似，额外有：
        //   isVertical — 是否为垂直布局组（true=Vertical, false=Horizontal）
        //
        // 💡 布局组是 GUILayout 的核心容器：
        //   GUILayout.BeginHorizontal() → LogLayoutGroupEntry(isVertical=false)
        //   GUILayout.BeginVertical()   → LogLayoutGroupEntry(isVertical=true)
        //   GUILayout.EndHorizontal/Vertical() → LogLayoutEndGroup()
        // ==============================================================
        [NativeConditional("UNITY_EDITOR")]
        public static extern void LogLayoutGroupEntry(Rect rect, int left, int right, int top, int bottom, GUIStyle style, bool isVertical);

        // ==============================================================
        // 布局组结束标记
        //
        // 📌 LogLayoutEndGroup — 记录布局组结束。
        //    对应 GUILayout.EndHorizontal() / EndVertical() 的调用。
        //    调试器用此来正确处理嵌套布局组的层级。
        // ==============================================================
        [NativeConditional("UNITY_EDITOR")]
        [StaticAccessor("GetGUIDebuggerManager()", StaticAccessorType.Dot)]
        [NativeMethod("LogEndGroup")]
        public static extern void LogLayoutEndGroup();

        // ==============================================================
        // 属性区域日志（SerializedProperty 可视化）
        //
        // 🎯 LogBeginProperty — 记录一个属性的 GUI 区域开始。
        //   targetTypeAssemblyQualifiedName — 属性所属类型的完整程序集限定名
        //   path — 属性路径（如 "m_Width" 或 "m_Color.r"）
        //   position — 属性在 GUI 中的位置矩形
        //
        // 🎯 LogEndProperty — 记录属性区域结束。
        //
        // 💡 这对方法让调试器能够将 IMGUI 区域与
        //    SerializedProperty 关联起来。
        //    在 Debugger 中点击一个区域可以直接定位到对应的属性。
        // ==============================================================
        [NativeConditional("UNITY_EDITOR")]
        [StaticAccessor("GetGUIDebuggerManager()", StaticAccessorType.Dot)]
        public static extern void LogBeginProperty(string targetTypeAssemblyQualifiedName, string path, Rect position);

        [NativeConditional("UNITY_EDITOR")]
        [StaticAccessor("GetGUIDebuggerManager()", StaticAccessorType.Dot)]
        public static extern void LogEndProperty();

        // ==============================================================
        // 调试器激活状态
        //
        // 🎯 active — IMGUI Debugger 是否正在监听/记录。
        //    当 Debugger 窗口打开时为 true。
        //    可用于优化：如果 Debugger 未激活，
        //    可以跳过一些调试信息的收集。
        // ==============================================================
        [NativeConditional("UNITY_EDITOR")]
        public static extern bool active {get; }
    }
}
