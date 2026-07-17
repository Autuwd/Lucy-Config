// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ObjectGUIState — IMGUI 状态对象（C#/C++ 桥接容器）
//
// 📌 作用：
//   ObjectGUIState 是一个轻量级的原生对象包装器，
//   用于将 IMGUI 的内部 GUI 状态数据暴露给 C# 层。
//   它实现了 IDisposable 模式来管理非托管内存。
//
// 🔄 在 IMGUI 架构中的位置：
//   ObjectGUIState 是 GUIUtility.BeginContainer/EndContainer 的
//   参数类型，用于在 C# 端持有 C++ GUIState 对象的引用。
//
//   C# 端                    C++ 端
//   ┌──────────────┐         ┌──────────────┐
//   │ObjectGUIState│ ──m_Ptr→│  GUIState    │
//   │  m_Ptr: IntPtr│         │  (完整GUI状态)│
//   └──────────────┘         └──────────────┘
//
// 💡 使用场景：
//   UI Toolkit 与 IMGUI 混合使用时，
//   UIElementsModule 需要一个 C# 对象来持有 GUI 状态。
//   ObjectGUIState 就是这个"桥梁"对象。
//
// ⚠️ 这是一个 internal 类，不暴露给用户代码。
//    仅在引擎内部（UIElementsModule 等）使用。
//
// 📍 对应 C++ 头文件：Modules/IMGUI/GUIState.h
// ==============================================================

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // ObjectGUIState — IMGUI 状态的 C#/C++ 桥接容器
    //
    // 🔑 关键成员：
    //   - m_Ptr: 指向 C++ GUIState 对象的非托管指针
    //   - Dispose(): 显式释放非托管资源
    //   - ~ObjectGUIState(): 析构函数作为安全网
    //
    // 📌 IDisposable 模式：
    //   实现标准的 Dispose 模式：
    //   1. Dispose() — 显式释放 + 抑制终结器（推荐路径）
    //   2. ~ObjectGUIState() — 终结器作为安全网（GC 路径）
    //   3. Destroy() — 实际的释放逻辑（检查 null 防止双重释放）
    //
    // 💡 为什么需要 IDisposable：
    //   ObjectGUIState 持有非托管内存（C++ 对象），
    //   C# 垃圾回收器不知道如何释放它。
    //   必须通过 Dispose 或终结器手动释放。
    //
    // ⚡ VisibleToOtherModules("UnityEngine.UIElementsModule")：
    //   UIElementsModule 是主要的使用者。
    //   它通过 ObjectGUIState 在 IMGUI 和 UI Toolkit 之间
    //   共享 GUI 状态数据。
    // ==============================================================
    [NativeHeader("Modules/IMGUI/GUIState.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal class ObjectGUIState : IDisposable
    {
        // ==============================================================
        // 原生对象指针
        //
        // 🎯 m_Ptr — 指向 C++ 端 GUIState 对象的指针。
        //    所有 C++ 调用都通过此指针访问原生数据。
        //    IntPtr.Zero 表示对象已被释放。
        // ==============================================================
        internal IntPtr m_Ptr;

        // ==============================================================
        // 构造函数 — 创建新的原生 GUIState 对象
        //
        // 📌 调用 C++ 端的 Internal_Create() 分配新的 GUIState。
        //    新创建的对象包含默认的 GUI 状态数据。
        // ==============================================================
        public ObjectGUIState()
        {
            m_Ptr = Internal_Create();
        }

        // ==============================================================
        // Dispose — 显式释放非托管资源（推荐路径）
        //
        // 🎯 两步操作：
        //   1. Destroy() — 释放 C++ 对象内存
        //   2. GC.SuppressFinalize(this) — 告诉 GC 不需要调用终结器
        //
        // 💡 使用 using 语句时会自动调用 Dispose：
        //   using (var state = new ObjectGUIState()) {
        //       GUIUtility.BeginContainer(state);
        //       // ... 使用 GUI 状态 ...
        //       GUIUtility.Internal_EndContainer();
        //   } // 自动 Dispose
        // ==============================================================
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        // ==============================================================
        // 终止器（析构函数）— GC 安全网
        //
        // 📌 当开发者忘记调用 Dispose() 时，
        //    GC 回收对象前会调用此终结器作为最后保障。
        //    ⚠️ 终结器在 GC 线程执行，性能较差，
        //    应尽量通过 Dispose() 显式释放。
        // ==============================================================
        ~ObjectGUIState()
        {
            Destroy();
        }

        // ==============================================================
        // Destroy — 实际的资源释放逻辑
        //
        // 🎯 安全的释放检查：
        //   1. 检查 m_Ptr != IntPtr.Zero（防止双重释放）
        //   2. 调用 Internal_Destroy(m_Ptr) 释放 C++ 内存
        //   3. 将 m_Ptr 设为 IntPtr.Zero（标记已释放）
        //
        // 💡 双重释放防护：
        //   如果 Dispose() 和终结器都被调用，
        //   第二次调用 Destroy() 时 m_Ptr 已经是 Zero，
        //   不会重复释放内存。
        // ==============================================================
        void Destroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        // ==============================================================
        // 原生对象创建与销毁
        //
        // 📌 Internal_Create — 在 C++ 端分配新的 GUIState 对象。
        //    返回新对象的指针。
        //
        // 📌 Internal_Destroy — 在 C++ 端释放 GUIState 对象。
        //    IsThreadSafe = true 允许在非主线程调用，
        //    但实际使用中通常在主线程。
        // ==============================================================
        private static extern IntPtr Internal_Create();

        [NativeMethod(IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        // ==============================================================
        // C#/C++ 对象编组（Marshalling）
        //
        // 📌 BindingsMarshaller — 将 C# ObjectGUIState 转换为 C++ 指针。
        //    当 ObjectGUIState 作为参数传递给 C++ 方法时，
        //    编组器自动提取 m_Ptr 进行跨语言传递。
        // ==============================================================
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(ObjectGUIState objectGUIState) => objectGUIState.m_Ptr;
        }
    }
}
