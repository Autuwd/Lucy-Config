// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 CharacterJoint — 布娃娃链（角色关节）
//
// 📌 作用：
//   专为布娃娃物理系统设计的关节，限制两刚体间的旋转范围。
//   基于 ConfigurableJoint 简化，提供 twist（扭转）和 swing（摆动）限制。
//
// 💡 核心参数：
//   - swingAxis：摆动轴方向
//   - twistLimitSpring/swingLimitSpring：弹性回正弹簧
//   - lowTwistLimit/highTwistLimit：扭转角度范围
//   - swing1Limit/swing2Limit：两个摆动方向的限制
//   - enableProjection：启用投影修正（防止关节过度拉伸）
//
// ⚡ 适用场景：布娃娃角色、断肢效果、物理动画混合。
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/CharacterJoint.h")]
    [NativeClass("Unity::CharacterJoint")]
    public partial class CharacterJoint : Joint
    {
        extern public Vector3 swingAxis { get; set; }
        extern public SoftJointLimitSpring twistLimitSpring { get; set; }
        extern public SoftJointLimitSpring swingLimitSpring { get; set; }
        extern public SoftJointLimit lowTwistLimit { get; set; }
        extern public SoftJointLimit highTwistLimit { get; set; }
        extern public SoftJointLimit swing1Limit { get; set; }
        extern public SoftJointLimit swing2Limit { get; set; }
        extern public bool enableProjection { get; set; }
        extern public float projectionDistance { get; set; }
        extern public float projectionAngle { get; set; }
    }
}
