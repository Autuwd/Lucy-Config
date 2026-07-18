// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// =================================================================================================
// 🎯 WheelCollider — 车辆悬挂与轮胎物理系统
// =================================================================================================
// WheelCollider 是 Unity 车辆系统的核心，使用基于弹簧的悬挂模型 + 摩擦圆（Friction Circle）
// 轮胎模型模拟真实车辆动力学。
//
// 📌 悬挂系统（Suspension）
//   - suspensionDistance: 悬挂最大行程（m），决定车轮上下活动范围
//   - suspensionSpring: JointSpring { spring, damper, targetPosition } 弹簧/阻尼参数
//   - suspensionExpansionLimited: 是否限制悬挂扩展行程
//   - forceAppPointDistance: 力的作用点偏移，影响车辆侧倾
//   - sprungMass: 悬挂簧下质量（由系统自动计算或手动覆盖）
//   - ConfigureVehicleSubsteps: 配置亚步进参数（speedThreshold 以上/以下分别设定）
//
// 💡 轮胎摩擦模型（Friction Circle）
//   - forwardFriction: 纵向摩擦力（驱动/制动），控制加速和刹车
//   - sidewaysFriction: 侧向摩擦力，控制转向和侧滑
//   - WheelFrictionCurve: asymptote（渐近值）/ extremum（极值）/ stiffness（刚度）
//   - forwardSlip / sidewaysSlip: 当前滑移率（WheelHit 中报告）
//
// ⚡ 驱动与转向
//   - motorTorque: 电机扭矩（N·m），正=前进，负=倒车
//   - brakeTorque: 制动扭矩，停车/减速用
//   - steerAngle: 转向角度（度），前轮通常 30°-45°
//   - rpm: 当前转速，用于视觉同步（轮子旋转动画）
//
// 📌 WheelHit 碰撞信息
//   - GetGroundHit(out WheelHit): 获取轮胎与地面的接触信息
//   - GetWorldPose(out pos, out quat): 获取车轮的渲染位置/旋转（视觉跟随）
//   - isGrounded: 轮胎是否触地
//   - WheelHit: point/normal/forwardDir/sidewaysDir/force/slip
//
// ⚡ 性能注意
//   - WheelCollider 使用射线投射检测地面（比物理碰撞体更高效）
//   - ResetSprungMasses 在车辆配置改变后调用重新计算质量分布
//   - VehicleSubsteps 在高/低速分别设置不同迭代精度
// =================================================================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/Vehicles/WheelCollider.h")]
    public struct WheelHit
    {
        [NativeName("point")] private Vector3 m_Point;
        [NativeName("normal")] private Vector3 m_Normal;
        [NativeName("forwardDir")] private Vector3 m_ForwardDir;
        [NativeName("sidewaysDir")] private Vector3 m_SidewaysDir;
        [NativeName("force")] private float m_Force;
        [NativeName("forwardSlip")] private float m_ForwardSlip;
        [NativeName("sidewaysSlip")] private float m_SidewaysSlip;
        [NativeName("collider")] private Collider m_Collider;

        public Collider collider { get { return m_Collider; } set { m_Collider = value; }}
        public Vector3    point { get { return m_Point; } set { m_Point = value; } }
        public Vector3    normal { get { return m_Normal; } set { m_Normal = value; } }
        public Vector3    forwardDir { get { return m_ForwardDir; } set { m_ForwardDir = value; } }
        public Vector3    sidewaysDir { get { return m_SidewaysDir; } set { m_SidewaysDir = value; } }
        public float      force { get { return m_Force; } set { m_Force = value; } }
        public float      forwardSlip { get { return m_ForwardSlip; } set { m_ForwardSlip = value; } }
        public float      sidewaysSlip { get { return m_SidewaysSlip; } set { m_SidewaysSlip = value; } }
    }

    [NativeHeader("Modules/Vehicles/WheelCollider.h")]
    [NativeHeader("PhysicsScriptingClasses.h")]
    public class WheelCollider : Collider
    {
        public extern Vector3 center {get; set; }
        public extern float radius {get; set; }
        public extern float suspensionDistance {get; set; }
        public extern JointSpring suspensionSpring {get; set; }
        public extern bool suspensionExpansionLimited {get; set; }
        public extern float forceAppPointDistance {get; set; }
        public extern float mass {get; set; }
        public extern float wheelDampingRate {get; set; }
        public extern WheelFrictionCurve forwardFriction {get; set; }
        public extern WheelFrictionCurve sidewaysFriction {get; set; }
        public extern float motorTorque {get; set; }
        public extern float brakeTorque {get; set; }
        public extern float steerAngle {get; set; }
        public extern bool isGrounded {[NativeName("IsGrounded")] get; }
        public extern float rpm { get; }
        public extern float sprungMass { get; set; }
        public extern float rotationSpeed { get; set; }
        public extern void ResetSprungMasses();
        public extern void ConfigureVehicleSubsteps(float speedThreshold, int stepsBelowThreshold, int stepsAboveThreshold);
        public extern void GetWorldPose(out Vector3 pos, out Quaternion quat);
        public extern bool GetGroundHit(out WheelHit hit);
        extern internal bool isSupported { get; }
    }
}
