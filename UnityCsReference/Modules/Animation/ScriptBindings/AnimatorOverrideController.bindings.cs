// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AnimatorOverrideController —— 动画覆盖控制器
//
// 【概述】
// AnimatorOverrideController 允许在运行时替换 Animator Controller
// 中的动画剪辑而不修改原始控制器。常用于：
// - 换装系统（不同装备替换不同动画）
// - 角色变体（不同种族/体型使用不同动画）
// - 武器切换（持剑/持弓等不同动作集）
//
// 【工作原理】
// 1. 创建时可传入 RuntimeAnimatorController 作为基础
// 2. 通过 this[originalClip] = overrideClip 替换映射
// 3. 替换后 Animator 使用替换后的剪辑播放
//
// 【使用方式（推荐）】
// AnimatorOverrideController overrideCtrl = new AnimatorOverrideController(baseCtrl);
// var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
// overrideCtrl.GetOverrides(overrides);
// overrides[0] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[0].Key, newClip);
// overrideCtrl.ApplyOverrides(overrides);
// ============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    /// <summary>
    /// AnimationClipPair —— 已弃用的原始/覆盖剪辑配对类。
    /// 使用 AnimatorOverrideController.GetOverrides / ApplyOverrides 替代。
    /// </summary>
    [Obsolete("This class is not used anymore. See AnimatorOverrideController.GetOverrides() and AnimatorOverrideController.ApplyOverrides()")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public class AnimationClipPair
    {
        /// <summary>原始动画剪辑。</summary>
        public AnimationClip originalClip;
        /// <summary>替换后的动画剪辑。</summary>
        public AnimationClip overrideClip;
    }

    /// <summary>
    /// AnimatorOverrideController —— 运行时动画剪辑覆盖控制器。
    /// 可在不修改原始控制器的情况下替换动画剪辑。
    /// </summary>
    [NativeHeader("Modules/Animation/AnimatorOverrideController.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animation.bindings.h")]
    [UsedByNativeCode]
    [HelpURL("AnimatorOverrideController")]
    public class AnimatorOverrideController : RuntimeAnimatorController
    {
        /// <summary>创建空的覆盖控制器（不基于任何控制器）。</summary>
        public AnimatorOverrideController()
        {
            Internal_Create(this, null);
            OnOverrideControllerDirty = null;
        }

        /// <summary>创建基于指定控制器的覆盖控制器。</summary>
        /// <param name="controller">要覆盖的基础控制器</param>
        public AnimatorOverrideController(RuntimeAnimatorController controller)
        {
            Internal_Create(this, controller);
            OnOverrideControllerDirty = null;
        }

        [FreeFunction("AnimationBindings::CreateAnimatorOverrideController")]
        extern private static void Internal_Create([Writable] AnimatorOverrideController self, RuntimeAnimatorController controller);

        /// <summary>获取或设置被覆盖的运行时控制器。</summary>
        extern public RuntimeAnimatorController runtimeAnimatorController
        {
            [NativeMethod("GetAnimatorController")]
            get;
            [NativeMethod("SetAnimatorController")]
            set;
        }

        /// <summary>通过剪辑名称获取/设置覆盖剪辑。</summary>
        /// <param name="name">原始剪辑的名称</param>
        public AnimationClip this[string name]
        {
            get { return Internal_GetClipByName(name, true); }
            set { Internal_SetClipByName(name, value); }
        }

        [NativeMethod("GetClip")]
        extern private AnimationClip Internal_GetClipByName(string name, bool returnEffectiveClip);

        [NativeMethod("SetClip")]
        extern private void Internal_SetClipByName(string name, AnimationClip clip);

        /// <summary>通过原始剪辑获取/设置其覆盖剪辑。</summary>
        /// <param name="clip">原始动画剪辑</param>
        public AnimationClip this[AnimationClip clip]
        {
            get { return GetClip(clip, true); }
            set { SetClip(clip, value, true); }
        }

        extern private AnimationClip GetClip(AnimationClip originalClip, bool returnEffectiveClip);

        extern private void SetClip(AnimationClip originalClip, AnimationClip overrideClip, bool notify);

        /// <summary>发送覆盖更改通知到 C++ 侧。</summary>
        extern private void SendNotification();

        extern private AnimationClip GetOriginalClip(int index);
        extern private AnimationClip GetOverrideClip(AnimationClip originalClip);

        /// <summary>覆盖剪辑的总数。</summary>
        extern public int overridesCount
        {
            [NativeMethod("GetOriginalClipsCount")]
            get;
        }

        /// <summary>获取所有覆盖映射（推荐方式）。</summary>
        /// <param name="overrides">填充覆盖对的列表</param>
        public void GetOverrides(List<KeyValuePair<AnimationClip, AnimationClip>> overrides)
        {
            if (overrides == null)
                throw new System.ArgumentNullException("overrides");

            int count = overridesCount;
            if (overrides.Capacity < count)
                overrides.Capacity = count;

            overrides.Clear();
            for (int i = 0; i < count; ++i)
            {
                AnimationClip originalClip = GetOriginalClip(i);
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, GetOverrideClip(originalClip)));
            }
        }

        /// <summary>应用覆盖映射（推荐方式）。</summary>
        /// <param name="overrides">覆盖对的列表</param>
        public void ApplyOverrides(IList<KeyValuePair<AnimationClip, AnimationClip>> overrides)
        {
            if (overrides == null)
                throw new System.ArgumentNullException("overrides");

            for (int i = 0; i < overrides.Count; i++)
                SetClip(overrides[i].Key, overrides[i].Value, false);

            SendNotification();
        }

        /// <summary>[已弃用] 使用 GetOverrides / ApplyOverrides 替代。</summary>
        [Obsolete("AnimatorOverrideController.clips property is deprecated. Use AnimatorOverrideController.GetOverrides and AnimatorOverrideController.ApplyOverrides instead.")]
        public AnimationClipPair[] clips
        {
            get
            {
                int count = overridesCount;

                AnimationClipPair[] clipPair = new AnimationClipPair[count];
                for (int i = 0; i < count; i++)
                {
                    clipPair[i] = new AnimationClipPair();
                    clipPair[i].originalClip = GetOriginalClip(i);
                    clipPair[i].overrideClip = GetOverrideClip(clipPair[i].originalClip);
                }

                return clipPair;
            }
            set
            {
                for (int i = 0; i < value.Length; i++)
                    SetClip(value[i].originalClip, value[i].overrideClip, false);

                SendNotification();
            }
        }

        [NativeConditional("UNITY_EDITOR")]
        extern internal void PerformOverrideClipListCleanup();

        internal delegate void OnOverrideControllerDirtyCallback();

        internal OnOverrideControllerDirtyCallback OnOverrideControllerDirty;

        [NativeConditional("UNITY_EDITOR")]
        [RequiredByNativeCode]
        internal static void OnInvalidateOverrideController(AnimatorOverrideController controller)
        {
            if (controller.OnOverrideControllerDirty != null)
                controller.OnOverrideControllerDirty();
        }
    }
}
