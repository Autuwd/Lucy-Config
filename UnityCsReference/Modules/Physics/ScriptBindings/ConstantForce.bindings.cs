// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    /// <summary>
    /// 恒力组件，对 Rigidbody 持续施加力和扭矩。
    /// 适用于模拟风力、恒定推力、重力修改等场景。
    /// 底层在 PhysX 的每个模拟步中调用 PxRigidBody::addForce，使用 ForceMode.Force 模式。
    /// 需要 Rigidbody 组件才能工作（由 RequireComponent 自动添加）。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/ConstantForce.h")]
    public class ConstantForce : Behaviour
    {
        /// <summary>
        /// 在世界坐标系中施加的持续力向量（单位：牛顿）。
        /// 每个物理步自动应用，力方向固定在世界空间中。
        /// </summary>
        extern public Vector3 force { get; set; }

        /// <summary>
        /// 在世界坐标系中施加的持续扭矩向量（单位：N·m）。
        /// 每个物理步自动应用，扭矩轴固定在世界空间中。
        /// </summary>
        extern public Vector3 torque { get; set; }

        /// <summary>
        /// 在局部坐标系中施加的持续力向量（单位：牛顿）。
        /// 力方向相对于刚体的本地旋转，刚体旋转时力的方向也随之变化。
        /// 适合模拟火箭引擎推力（沿物体局部朝向）。
        /// </summary>
        extern public Vector3 relativeForce { get; set; }

        /// <summary>
        /// 在局部坐标系中施加的持续扭矩向量（单位：N·m）。
        /// 扭矩轴相对于刚体的本地旋转。
        /// </summary>
        extern public Vector3 relativeTorque { get; set; }
    }
}
