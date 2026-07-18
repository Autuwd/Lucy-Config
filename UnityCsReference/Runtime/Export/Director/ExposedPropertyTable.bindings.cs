// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ExposedPropertyResolver — Timeline 暴露属性解析器
//
// 📌 作用：
//   ExposedPropertyResolver 是 Timeline 中"暴露属性"
//   （Exposed Property）的核心解析机制。当用户在 Timeline
//   中将一个引用拖拽到绑定字段时，这个引用信息以 PropertyName
//   的形式存储，运行时通过 Resolver 解析为实际的 Object 引用。
//
// 💡 理解关键（Timeline 绑定链路）：
//   PlayableDirector（监视 TimelineAsset）
//     ↓ GetResolver()
//   IExposedPropertyTable（用户在 Inspector 中拖拽的绑定）
//     ↓ ResolveReferenceInternal
//   ExposedPropertyResolver（根据 PropertyName 查找 Object）
//
// 🎯 这个机制实现了 Timeline 的资源引用解耦：
//   - TimelineAsset 不直接持有场景对象的引用
//   - 而是通过 PropertyName 作为"钥匙"
//   - PlayableDirector 在运行时提供 IExposedPropertyTable
//   - Resolver 根据钥匙找到实际场景中的对象
//
// 📍 对应 C++ 头文件：Runtime/Director/Core/ExposedPropertyTable.bindings.h
// ==============================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // ==============================================================
    // ExposedPropertyResolver — Timeline 暴露属性解析器
    //
    // 🔑 关键字段：
    //   table (IntPtr) — 指向 C++ 端 ExposedPropertyTable 的指针
    //
    // 🔑 核心操作：
    //   ResolveReferenceInternal(ptr, name, isValid) — 解析属性引用
    //
    // 💡 工作机制：
    //   TimelineAsset 中保存的绑定引用不直接存储 Object 引用，
    //  而是存储 PropertyName。运行时 PlayableDirector 提供
    //   IExposedPropertyTable，ExposedPropertyResolver 在表中
    //   根据 PropertyName 查找并返回实际的 Object 引用。
    //
    // 🎯 这种"名称 → 引用"的延迟解析模式实现了：
    //   - TimelineAsset 的资源引用于场景解耦
    //   - 同一个 TimelineAsset 可以在不同场景中使用
    //   - Prefab 中的 Timeline 也可以正确绑定场景对象
    // ==============================================================
    [NativeHeader("Runtime/Director/Core/ExposedPropertyTable.bindings.h")]
    [NativeHeader("Runtime/Utilities/PropertyName.h")]
    public struct ExposedPropertyResolver
    {
        internal IntPtr table;

        // ==============================================================
        // ResolveReferenceInternal — 解析 PropertyName 为 Object 引用
        //
        // 🎯 Timeline 运行时绑定的核心方法
        // ⚡ ptr == IntPtr.Zero 时抛出 ArgumentNullException
        // 📌 out isValid 返回解析是否成功
        // ==============================================================
        internal static Object ResolveReferenceInternal(IntPtr ptr, PropertyName name, out bool isValid)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException("Argument \"ptr\" can't be null.");

            return ResolveReferenceBindingsInternal(ptr, name, out isValid);
        }

        [FreeFunction("ExposedPropertyTableBindings::ResolveReferenceInternal")]
        extern private static Object ResolveReferenceBindingsInternal(IntPtr ptr, PropertyName name, out bool isValid);
    }
}
