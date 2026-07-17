// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // ============================================================
    // ScriptableObject — 数据容器基类
    //
    // ScriptableObject 是 Unity 中专门用于"数据存储"的基类，它与
    // MonoBehaviour 最大的不同是：不需要挂载到 GameObject 上。
    //
    // 设计定位：
    //   ScriptableObject 本质上是 Unity 引擎级别的"纯数据对象"容器，
    //   它继承了 UnityEngine.Object，因此享有 Unity 对象的所有特性：
    //   - 生命周期由引擎管理（引用计数 + 自动垃圾回收）
    //   - 支持序列化/反序列化（Inspector 编辑、Asset 保存）
    //   - 可作为资源（.asset 文件）存储在项目中
    //   - 支持撤销/重做（Undo/Redo）操作
    //
    // 与 MonoBehaviour 的核心区别：
    //   MonoBehaviour                       ScriptableObject
    //   ──────────────────────────────      ──────────────────────────────
    //   必须挂载在 GameObject 上            不与 GameObject 绑定
    //   有 Awake/Start/Update 等生命周期    没有游戏循环消息
    //   每帧参与更新，有性能开销             仅在需要时被使用
    //   创建方式：AddComponent<T>()          创建方式：CreateInstance<T>()
    //   跨场景不持久化                       可以保存为资源文件（.asset）
    //
    // 最佳实践（数据与逻辑分离）：
    //   1. 用 ScriptableObject 存储配置数据（武器属性、NPC 数据等）
    //   2. 用 MonoBehaviour 控制游戏行为（读取 ScriptableObject 的数据执行逻辑）
    //   3. 通过 Asset 引用，多个场景/对象可以共享同一个数据实例
    //
    // 典型用途：
    //   - 游戏配置表（武器属性、关卡参数、角色数值）
    //   - 事件系统参数（ScriptableObject 作为事件通道）
    //   - 状态机配置（AnimationState、AI 行为树参数）
    //   - Editor 工具数据（自定义窗口设置、关卡编辑数据）
    //
    // Unity 源码位置：
    //   此文件是 C# 层绑定，C++ 端实现位于 Runtime/Mono/MonoBehaviour.h
    //   关键区别：与 MonoBehaviour 共享同一个 native 类（都是 MonoBehavour），
    //   但通过不同标记区分：ScriptableObject 在 C++ 端的 m_IsScriptableObject = true。
    //
    // [StructLayout(LayoutKind.Sequential)] 说明：
    //   强制内存布局顺序与声明一致，这是与 C++ 端进行原生交互的必要条件。
    //   确保 C# 对象的字段排列与 C++ 端的 ScriptableObject 结构体完全对齐，
    //   从而允许直接内存访问（绕过编组开销）。
    // ============================================================

    /// <summary>
    /// ScriptableObject 是不需要挂载到 GameObject 上的数据容器基类。
    /// 继承此类可以创建纯数据对象，用于存储和共享游戏数据。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]  // 确保内存布局与 C++ 端一致
    [RequiredByNativeCode]                 // 防止 IL2CPP/AOT 裁剪掉此类
    [ExtensionOfNativeClass]               // 标记为 C++ 原生类的 C# 扩展
    [NativeClass(null)]                    // 没有对应的 C++ 类名（由 MonoBehaviour 处理）
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]  // C++ 端头文件位置
    public class ScriptableObject : Object
    {
        // ============================================================
        // 构造函数
        //
        // ScriptableObject 的构造函数不像普通 C# 类那样直接分配内存。
        // 它通过调用 CreateScriptableObject(this) 将构造工作委托给
        // C++ 端。这保证了：
        //   1. 对象的内存在 C++ 堆上分配（而非 C# 托管堆）
        //   2. 引擎可以跟踪这个对象的生命周期
        //   3. 对象可以被序列化/反序列化
        //
        // 注意：用户不应该直接 new ScriptableObject()，而应始终使用
        // CreateInstance<T>() 工厂方法，因为后者有完整的初始化流程。
        // ============================================================

        /// <summary>
        /// 创建 ScriptableObject 实例。内部调用 C++ 端完成实际内存分配。
        /// </summary>
        public ScriptableObject()
        {
            CreateScriptableObject(this);
        }

        // ============================================================
        // SetDirty — 标记对象为"已修改"（已废弃）
        //
        // [Obsolete] 原因：
        //   此方法已被 EditorUtility.SetDirty() 替代。直接在 ScriptableObject
        //   上标记脏的语义不够精确，EditorUtility.SetDirty 更明确地表示
        //   "编辑器中的某个对象被修改了"。
        //
        // [NativeConditional("ENABLE_MONO")]：
        //   此方法仅在 Mono 脚本后端下编译，IL2CPP 构建中排除。
        //   这是因为 IL2CPP 中标记脏的逻辑由 Editor 端的 CPP 代码处理。
        // ============================================================

        [NativeConditional("ENABLE_MONO")]
        [Obsolete("Use EditorUtility.SetDirty instead")]  // Unity 5.0 起废弃
        public extern void SetDirty();

        // ============================================================
        // CreateInstance — 创建 ScriptableObject 实例的工厂方法
        //
        // 三个重载覆盖了不同的使用场景：
        //
        // 1. CreateInstance(string className)
        //    通过类名字符串创建。用于运行时动态创建（如从配置文件读取类名）。
        //    内部调用 Scripting::CreateScriptableObject（C++ 端的名称查找）。
        //
        // 2. CreateInstance(Type type)
        //    通过 System.Type 创建。最常用的形式，类型安全比字符串版更好。
        //    内部调用 Scripting::CreateScriptableObjectWithType。
        //    applyDefaultsAndReset = true 表示会应用默认值并重置。
        //
        // 3. CreateInstance<T>()
        //    泛型版本，编译时类型安全，不需要类型转换。语法最简洁：
        //    var data = ScriptableObject.CreateInstance<MyData>();
        //
        // 为什么要用工厂方法而不是 new？
        //   - new ScriptableObject() 调用构造函数，但不会应用默认值
        //   - CreateInstance 在 C++ 端完成完整的初始化流程：
        //     分配原生内存 → 设置类型信息 → 应用默认值 → 构造 C# 对象
        //   - 编辑器模式下还会触发 Undo/Redo 记录
        // ============================================================

        /// <summary>
        /// 通过类名创建 ScriptableObject 实例。类名必须是完整或简化的类型名称。
        /// </summary>
        /// <param name="className">要创建的 ScriptableObject 类型名称</param>
        /// <returns>新创建的 ScriptableObject 实例</returns>
        public static ScriptableObject CreateInstance(string className)
        {
            return CreateScriptableObjectInstanceFromName(className);
        }

        /// <summary>
        /// 通过 System.Type 创建 ScriptableObject 实例。
        /// </summary>
        /// <param name="type">要创建的 ScriptableObject 的具体类型</param>
        /// <returns>新创建的 ScriptableObject 实例</returns>
        public static ScriptableObject CreateInstance(Type type)
        {
            return CreateScriptableObjectInstanceFromType(type, true);
        }

        /// <summary>
        /// 泛型版本，编译时类型安全的创建方式。推荐使用。
        /// </summary>
        /// <typeparam name="T">要创建的 ScriptableObject 子类类型</typeparam>
        /// <returns>新创建的 T 类型实例</returns>
        public static T CreateInstance<T>() where T : ScriptableObject
        {
            return (T)CreateInstance(typeof(T));
        }

        // ============================================================
        // CreateInstance(Type, Action<ScriptableObject>) — 内部重载
        //
        // 这个 internal 重载比公开版本多了一个 initialize 回调参数，
        // 允许在创建实例后立即执行自定义初始化，然后再调用默认值重置。
        //
        // 流程：
        //   1. 类型检查：确保 type 继承自 ScriptableObject
        //   2. 创建原生实例（applyDefaultsAndReset = false，暂不应用默认值）
        //   3. 执行自定义初始化（initialize 委托）
        //   4. finally 块确保无论如何都会调用 ResetAndApplyDefaultInstances
        //
        // 这种 try-finally 模式保证：即使初始化抛出异常，对象也能
        // 恢复到有效的默认状态，防止泄露未初始化的对象。
        //
        // [EditorBrowsable(Never)] 隐藏此重载，因为它是内部工具使用的 API。
        // ============================================================

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static ScriptableObject CreateInstance(Type type, Action<ScriptableObject> initialize)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(type))
                throw new ArgumentException("Type must inherit ScriptableObject.", "type");

            // 先创建实例（不应用默认值），让自定义初始化有机会修改
            var res = CreateScriptableObjectInstanceFromType(type, false);

            try
            {
                initialize(res);  // 执行自定义初始化逻辑
            }
            finally
            {
                // 无论如何都要应用默认值并重置，确保对象状态一致
                ResetAndApplyDefaultInstances(res);
            }

            return res;
        }

        // ============================================================
        // 以下为 C++ 原生方法的外部声明（P/Invoke 绑定）
        //
        // 这些方法由 Unity 的 C++ 运行时实现，通过自动生成的绑定代码
        // 桥接 C# 和 C++ 之间的调用。它们的共同特点是：
        //   - 使用 extern 关键字（没有 C# 实现体）
        //   - 通过 [NativeMethod] / [FreeFunction] 属性声明 C++ 端函数名
        //   - 执行真正的内存分配和类型注册工作
        // ============================================================

        /// <summary>
        /// 在 C++ 端创建 ScriptableObject 的原生对象。
        /// 此方法线程安全，且会在失败时抛出异常。
        /// </summary>
        /// <param name="self">当前 C# 对象引用（会被写入 C++ 端分配的原生对象指针）</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        extern static void CreateScriptableObject([Writable] ScriptableObject self);

        /// <summary>
        /// 通过类名字符串在 C++ 端查找类型并创建 ScriptableObject 实例。
        /// 对应 C++ 函数：Scripting::CreateScriptableObject
        /// </summary>
        /// <param name="className">类名称（支持完整命名空间路径或仅类名）</param>
        /// <returns>新创建的原生 ScriptableObject 实例的 C# 包装</returns>
        [FreeFunction("Scripting::CreateScriptableObject")]
        extern static ScriptableObject CreateScriptableObjectInstanceFromName(string className);

        /// <summary>
        /// 通过 System.Type 在 C++ 端创建 ScriptableObject 实例。
        /// 对应 C++ 函数：Scripting::CreateScriptableObjectWithType
        /// applyDefaultsAndReset 参数控制是否在创建后应用序列化默认值。
        /// </summary>
        /// <param name="type">要创建的 ScriptableObject 类型</param>
        /// <param name="applyDefaultsAndReset">是否应用默认值并重置对象状态</param>
        /// <returns>新创建的原生 ScriptableObject 实例的 C# 包装</returns>
        [NativeMethod(Name = "Scripting::CreateScriptableObjectWithType", IsFreeFunction = true, ThrowsException = true)]
        extern internal static ScriptableObject CreateScriptableObjectInstanceFromType(Type type, bool applyDefaultsAndReset);

        /// <summary>
        /// 重置对象到其默认序列化状态。
        /// 对应 C++ 函数：Scripting::ResetAndApplyDefaultInstances
        /// 用于在编辑器模式下重新应用预制体/资源的默认属性值。
        /// </summary>
        /// <param name="obj">要重置的 Unity 对象</param>
        [FreeFunction("Scripting::ResetAndApplyDefaultInstances")]
        extern internal static void ResetAndApplyDefaultInstances([NotNull] Object obj);
    }
}
