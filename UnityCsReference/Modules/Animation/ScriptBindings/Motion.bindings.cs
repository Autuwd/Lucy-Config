// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// Motion —— 动画运动数据基类
//
// 【概述】
// Motion 是 AnimationClip 和 BlendTree 的抽象基类。
// 它代表了"可播放的动画运动数据"，是 Mecanim 系统的核心抽象之一。
//
// 【派生类型】
// - AnimationClip：基于关键帧的动画剪辑
// - BlendTree：混合树（内部实现，不对外公开）
//
// 【作用】
// 提供运动数据的通用属性接口：
// - 平均速度/角速度（用于 Root Motion 计算）
// - 是否循环
// - 是否为旧版/人形动画
// ============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    /// Motion —— 运动数据的抽象基类。
    /// AnimationClip 和 BlendTree 的共同基类，提供运动特征信息。
    /// </summary>
    [NativeHeader("Modules/Animation/Motion.h")]
    public partial class Motion : Object
    {
        protected Motion() {}

        /// <summary>动画的平均持续时间（针对 BlendTree 等混合动画）。</summary>
        extern public float averageDuration { get; }
        /// <summary>平均角速度（用于 Root Motion 旋转计算）。</summary>
        extern public float averageAngularSpeed { get; }
        /// <summary>平均速度向量（用于 Root Motion 位移计算）。</summary>
        extern public Vector3 averageSpeed { get; }
        /// <summary>表观速度（考虑到缩放/混合后的速度值）。</summary>
        extern public float apparentSpeed { get; }

        /// <summary>是否为循环动画。</summary>
        extern public bool isLooping
        {
            [NativeMethod("IsLooping")]
            get;
        }

        /// <summary>是否为旧版（Legacy）动画。</summary>
        extern public bool legacy
        {
            [NativeMethod("IsLegacy")]
            get;
        }

        /// <summary>是否为人形动画。</summary>
        extern public bool isHumanMotion
        {
            [NativeMethod("IsHumanMotion")]
            get;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("ValidateIfRetargetable is not supported anymore, please use isHumanMotion instead.", true)]
        public bool ValidateIfRetargetable(bool val) { return false; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("isAnimatorMotion is not supported anymore, please use !legacy instead.", true)]
        public bool isAnimatorMotion { get; }
    }
}
