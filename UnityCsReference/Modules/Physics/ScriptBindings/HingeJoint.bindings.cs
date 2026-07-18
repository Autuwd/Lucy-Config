// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 HingeJoint — 铰链关节（门轴/旋转关节）
//
// 📌 作用：
//   限制两个刚体绕一个轴旋转，模拟门、摆锤、车轮等旋转运动。
//   可配置马达（motor）、弹簧（spring）和角度限制（limits）。
//
// 💡 核心参数：
//   - motor：关节马达（目标速度 + 最大力）
//   - limits：角度极限（min/max）
//   - spring：弹簧回正（弹性 + 阻尼）
//   - useMotor/useLimits/useSpring：开关控制
//   - velocity：当前角速度（只读）
//   - angle：当前角度（只读）
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/HingeJoint.h")]
    [NativeClass("Unity::HingeJoint")]
    public class HingeJoint : Joint
    {
        extern public JointMotor motor { get; set; }
        extern public JointLimits limits { get; set; }
        extern public JointSpring spring { get; set; }
        extern public bool useMotor { get; set; }
        extern public bool useLimits { get; set; }
        extern public bool extendedLimits { get; set; }
        extern public bool useSpring { get; set; }
        extern public float velocity { get; }
        extern public float angle { get; }
        extern public bool useAcceleration { get; set; }
    }
}
