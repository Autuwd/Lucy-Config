// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AvatarMask —— 人形骨骼遮罩
//
// 【概述】
// AvatarMask 用于控制哪些骨骼/身体部位参与动画播放。
// 常用于：
// - 动画层遮罩（上身射击 + 下身走路）
// - 动画混合时排除某些部位
// - 换装系统中不同部位的动画控制
//
// 【工作原理】
// 两种遮罩模式：
// 1. 身体部位模式（Humanoid Body Parts）：
//    通过 GetHumanoidBodyPartActive/SetHumanoidBodyPartActive 控制
//    13 个人形身体部位（根/身体/头/四肢/手指/IK等）
// 2. 变换路径模式（Transform Paths）：
//    通过具体的骨骼路径精确控制每个 Transform 的权重
//
// 【使用示例】
// Animator 的层可以设置 AvatarMask：
// animator.SetLayerWeight(1, 1.0f);
// animator.SetLayerAffectsMask(1, upperBodyMask);
//
// 这样层 1 的动画只影响上半身，下半身保留层 0 的走路动画。
// ============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Internal;

namespace UnityEngine
{
    /// <summary>
    /// 人形身体部位枚举 —— 用于 AvatarMask 控制哪些部位受动画影响。
    /// 从 UnityEditor.Animations 命名空间移入。
    /// </summary>
    [MovedFrom(true, "UnityEditor.Animations", "UnityEditor")]
    public enum AvatarMaskBodyPart
    {
        /// <summary>根骨骼（臀部）。</summary>
        Root = 0,
        /// <summary>躯干（脊椎）。</summary>
        Body = 1,
        /// <summary>头部。</summary>
        Head = 2,
        /// <summary>左腿。</summary>
        LeftLeg = 3,
        /// <summary>右腿。</summary>
        RightLeg = 4,
        /// <summary>左臂。</summary>
        LeftArm = 5,
        /// <summary>右臂。</summary>
        RightArm = 6,
        /// <summary>左手手指。</summary>
        LeftFingers = 7,
        /// <summary>右手手指。</summary>
        RightFingers = 8,
        /// <summary>左脚 IK（反向动力学控制）。</summary>
        LeftFootIK = 9,
        /// <summary>右脚 IK。</summary>
        RightFootIK = 10,
        /// <summary>左手 IK。</summary>
        LeftHandIK = 11,
        /// <summary>右手 IK。</summary>
        RightHandIK = 12,
        /// <summary>结束标记（用于枚举遍历）。</summary>
        LastBodyPart = 13
    }

    /// <summary>
    /// AvatarMask —— 人形骨骼遮罩资源。
    /// 控制动画层影响哪些身体部位或具体骨骼路径。
    /// </summary>
    [MovedFrom(true, "UnityEditor.Animations", "UnityEditor")]
    [NativeHeader("Modules/Animation/AvatarMask.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animation.bindings.h")]
    [UsedByNativeCode]
    public sealed partial class AvatarMask : Object
    {
        /// <summary>创建空的 AvatarMask。</summary>
        public AvatarMask()
        {
            Internal_Create(this);
        }

        [FreeFunction("AnimationBindings::CreateAvatarMask")]
        extern private static void Internal_Create([Writable] AvatarMask self);

        /// <summary>[已弃用] 使用 AvatarMaskBodyPart.LastBodyPart 替代。</summary>
        [Obsolete("AvatarMask.humanoidBodyPartCount is deprecated, use AvatarMaskBodyPart.LastBodyPart instead.")]
        public int humanoidBodyPartCount
        {
            get { return (int)AvatarMaskBodyPart.LastBodyPart; }
        }

        /// <summary>获取指定身体部位是否激活（受动画影响）。</summary>
        [NativeMethod("GetBodyPart")]
        extern public bool GetHumanoidBodyPartActive(AvatarMaskBodyPart index);

        /// <summary>设置指定身体部位是否激活。</summary>
        [NativeMethod("SetBodyPart")]
        extern public void SetHumanoidBodyPartActive(AvatarMaskBodyPart index, bool value);

        /// <summary>变换路径条数。通过路径精确控制单个 Transform 的遮罩。</summary>
        extern public int transformCount { get; set; }

        /// <summary>添加变换路径到遮罩（可选递归添加到子节点）。</summary>
        public void AddTransformPath(Transform transform) { AddTransformPath(transform, true);  }
        extern public void AddTransformPath([NotNull] Transform transform, [DefaultValue("true")] bool recursive);

        /// <summary>从遮罩移除变换路径。</summary>
        public void RemoveTransformPath(Transform transform) { RemoveTransformPath(transform, true); }
        extern public void RemoveTransformPath([NotNull] Transform transform, [DefaultValue("true")] bool recursive);

        /// <summary>获取指定索引的变换路径。</summary>
        extern public string GetTransformPath(int index);
        /// <summary>设置指定索引的变换路径。</summary>
        extern public void SetTransformPath(int index, string path);

        extern private float GetTransformWeight(int index);
        extern private void SetTransformWeight(int index, float weight);

        /// <summary>获取指定索引的变换是否激活（包装了 weight > 0.5）。</summary>
        public bool GetTransformActive(int index) { return GetTransformWeight(index) > 0.5F; }
        /// <summary>设置指定索引的变换是否激活（转换为 1.0 / 0.0 权重）。</summary>
        public void SetTransformActive(int index, bool value) { SetTransformWeight(index, value ? 1.0F : 0.0F); }

        /// <summary>内部 — 是否包含脚部 IK。</summary>
        extern internal bool hasFeetIK { get; }

        /// <summary>复制另一个 AvatarMask 的全部数据到当前遮罩。</summary>
        internal void Copy(AvatarMask other)
        {
            for (AvatarMaskBodyPart i = 0; i < AvatarMaskBodyPart.LastBodyPart; i++)
                SetHumanoidBodyPartActive(i, other.GetHumanoidBodyPartActive(i));

            transformCount = other.transformCount;

            for (int i = 0; i < other.transformCount; i++)
            {
                SetTransformPath(i, other.GetTransformPath(i));
                SetTransformActive(i, other.GetTransformActive(i));
            }
        }
    }
}
