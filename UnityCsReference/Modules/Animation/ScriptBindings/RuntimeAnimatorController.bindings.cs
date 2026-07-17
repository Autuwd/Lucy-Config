// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// RuntimeAnimatorController —— 运行时动画控制器基类
//
// 【概述】
// RuntimeAnimatorController 是 AnimatorController 和
// AnimatorOverrideController 的运行时基类。它是一个资源（Object），
// 由构建后的 Animator Controller 资产实例化而来。
//
// 【派生类型】
// - AnimatorController（内部，由 .controller 资产创建）
// - AnimatorOverrideController（可替换动画剪辑的覆盖控制器）
//
// 【角色】
// Animator 组件通过引用 RuntimeAnimatorController 来驱动动画。
// 运行时可通过 Animator.runtimeAnimatorController 动态切换。
// ============================================================

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    /// RuntimeAnimatorController —— 运行时动画控制器的基类。
    /// Animator 组件引用此类来驱动状态机和动画播放。
    /// </summary>
    [NativeHeader("Modules/Animation/RuntimeAnimatorController.h")]
    [UsedByNativeCode]
    [ExcludeFromObjectFactory]
    public partial class RuntimeAnimatorController : Object
    {
        protected RuntimeAnimatorController() {}

        /// <summary>此控制器引用的所有 AnimationClip 数组。</summary>
        extern public AnimationClip[] animationClips { get; }
    }
}
