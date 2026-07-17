// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // ============================================================
    // Behaviour — 组件行为基类
    //
    // Behaviour 是 Unity 组件体系中的关键中间层，继承链为：
    //   UnityEngine.Object
    //     → Component           （挂载在 GameObject 上的组件基类）
    //       → Behaviour          （引入了"启用/禁用"概念的组件）
    //         → MonoBehaviour    （用户脚本的基类）
    //         → OtherBehaviour   （如 AudioSource、Renderer 等内置组件）
    //
    // Behaviour 与普通 Component 的核心区别在于：Behaviour 拥有 enabled 开关，
    // 可以控制自身的激活状态。当一个 Behaviour 被禁用时，它的 Update/FixedUpdate
    // 等生命周期方法不会被 Unity 引擎调用，但组件本身仍然存在于 GameObject 上。
    //
    // 从架构角度看，Behaviour 是 Unity "组件驱动"设计哲学的关键枢纽：
    //   - Component 提供了"挂载"能力（依附于 GameObject）
    //   - Behaviour 在此基础上提供了"开关"能力（启用/禁用生命周期）
    //   - MonoBehaviour 进一步提供了"脚本能力"（Awake/Start/Update 等消息）
    // ============================================================

    /// <summary>
    /// Behaviour 是可启用或禁用的组件基类。
    /// 所有需要 enabled 开关的组件（包括用户编写的 MonoBehaviour）都继承自此类。
    /// </summary>
    [UsedByNativeCode]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
    public class Behaviour : Component
    {
        // ============================================================
        // enabled — 组件启用/禁用开关
        //
        // enabled 属性控制当前 Behaviour 是否处于激活状态：
        //   - true  （默认）：组件正常参与游戏循环，Update 等消息会被派发
        //   - false         ：组件被禁用，不再接收 Update/FixedUpdate/OnCollision 等消息
        //
        // 关键理解：
        //   1. enabled 只影响本组件，不影响挂载在同一 GameObject 上的其他组件
        //   2. 即使 enabled = true，如果所在 GameObject 非激活（SetActive(false)），
        //      组件仍然不会被更新。GameObject 的激活状态是"总开关"
        //   3. enabled 在 Inspector 中对应组件左上角的勾选框
        //
        // [RequiredByNativeCode] 的原因：
        //   这个属性被标记为 [RequiredByNativeCode] 是因为 C++ 端的
        //   GetFixedBehaviourManager 在 FixedUpdate 循环中直接读取该属性
        //   来判断哪些 Behaviour 需要执行固定更新。如果这个绑定被裁剪掉，
        //   引擎将无法确定哪些组件需要参与 FixedUpdate 阶段。
        //
        // [NativeProperty] 标记说明：
        //   该属性的 get/set 由 C++ 端实现（通过 extern 声明），
        //   实际存储在 C++ 的 Behaviour 对象中，C# 只是通过绑定访问。
        // ============================================================

        /// <summary>
        /// 获取或设置组件的启用状态。
        /// true 表示组件已启用（可被更新），false 表示组件已禁用（不参与更新）。
        /// </summary>
        [RequiredByNativeCode]  // GetFixedBehaviourManager 在 FixedUpdate 循环中直接读取此属性
        [NativeProperty]
        extern public bool enabled { get; set; }

        // ============================================================
        // isActiveAndEnabled — 综合激活状态查询（只读）
        //
        // 这个只读属性综合检查两个条件：
        //   1. 当前 Behaviour 的 enabled == true
        //   2. 当前 Behaviour 所在 GameObject 的 activeInHierarchy == true
        //      （即 GameObject 本身及其所有父级都处于激活状态）
        //
        // 只有当两个条件同时满足时，isActiveAndEnabled 才返回 true。
        // 它在运行时特别重要，用于判断一个组件是否真正"活着"——即是否
        // 在游戏循环中被更新。
        //
        // 与 enabled 的区别：
        //   - enabled 只检查组件自身的开关（不关心 GameObject 是否激活）
        //   - isActiveAndEnabled 检查的是最终的"运行时激活状态"
        //
        // C++ 端实现细节：
        //   [NativeMethod("IsAddedToManager")] 说明此 getter 调用的是
        //   C++ Behaviour 的 IsAddedToManager() 方法，该方法检查
        //   Behaviour 是否已被添加到引擎的管理器列表中。只有同时满足
        //   awake 完成 + enabled + GameObject 激活 三个条件时，
        //   组件才会被添加到管理器，isActiveAndEnabled 才返回 true。
        // ============================================================

        /// <summary>
        /// 获取组件是否在运行时处于真正的激活状态（只读）。
        /// 当 enabled == true 且 gameObject.activeInHierarchy == true 时返回 true。
        /// </summary>
        [NativeProperty]
        extern public bool isActiveAndEnabled
        {
            [NativeMethod("IsAddedToManager")]  // 对应 C++ Behaviour::IsAddedToManager()
            get;
        }
    }
}
