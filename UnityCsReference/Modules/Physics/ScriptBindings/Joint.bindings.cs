// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Joint — 关节基类
//
// 📌 作用：
//   所有关节组件（FixedJoint/HingeJoint/SpringJoint/ConfigurableJoint/CharacterJoint）
//   的基类，定义通用的关节属性和行为。底层对应 PhysX 的 PxJoint。
//
// 💡 核心属性：
//   - connectedBody：连接的刚体（null=连接到世界固定点）
//   - anchor/connectedAnchor：关节锚点（局部坐标）
//   - axis：关节轴方向
//   - breakForce/breakTorque：断开力/扭矩阈值（Infinity=永不）
//   - enableCollision：连接物体间是否碰撞
//   - enablePreprocessing：关节预处理（提高稳定性）
//   - massScale/connectedMassScale：质量缩放（调整力的分配）
//   - currentForce/currentTorque：当前约束力（只读）
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    /// <summary>
    /// 关节基类组件，用于在两个刚体之间建立物理约束。
    /// 底层对应 PhysX 的 PxConstraint / PxJoint 系列。
    ///
    /// 派生类包括：
    /// - FixedJoint：固定两个物体间的相对位置和旋转
    /// - HingeJoint：模拟铰链（门轴）运动
    /// - SpringJoint：弹簧连接
    /// - ConfigurableJoint：完全可配置的关节（6 自由度）
    /// - CharacterJoint：角色关节（布娃娃系统用）
    ///
    /// 关节通过约束求解器在每个物理步中计算力和扭矩，以维持连接约束。
    /// 当施加的力超过 breakForce 时，关节会自动断开（break）。
    /// </summary>
    [NativeHeader("Modules/Physics/Joint.h")]
    [NativeClass("Unity::Joint")]
    public class Joint : Component
    {
        /// <summary>
        /// 关节连接的另一个刚体。
        /// 如果为 null，则关节将物体连接到世界空间中的固定点。
        /// 底层对应 PhysX 的 PxJoint::setActors。
        /// </summary>
        extern public Rigidbody connectedBody
        {
            [NativeName("GetConnectedRigidbody")]
            get;
            [NativeName("SetConnectedRigidbody")]
            set;
        }

        /// <summary>关节连接的 ArticulationBody。</summary>
        extern public ArticulationBody connectedArticulationBody { get; set; }

        /// <summary>关节轴方向（在物体的局部空间中定义）。</summary>
        extern public Vector3 axis { get; set; }

        /// <summary>关节锚点位置（在物体的局部空间中定义）。</summary>
        extern public Vector3 anchor { get; set; }

        /// <summary>连接物体上的关节锚点位置（在连接物体的局部空间中）。</summary>
        extern public Vector3 connectedAnchor { get; set; }

        /// <summary>
        /// 是否自动配置连接锚点。
        /// 启用时，connectedAnchor 自动计算为连接物体上与 anchor 对应的世界空间位置。
        /// </summary>
        extern public bool autoConfigureConnectedAnchor { get; set; }

        /// <summary>
        /// 关节断开所需的最小力（单位：牛顿）。
        /// 当关节承受的力超过此值时，关节会自动销毁。
        /// 设置为 Mathf.Infinity 则关节永不因受力而断开。
        /// </summary>
        extern public float breakForce { get; set; }

        /// <summary>
        /// 关节断开所需的最小扭矩（单位：N·m）。
        /// 当关节承受的扭矩超过此值时，关节会自动销毁。
        /// 设置为 Mathf.Infinity 则关节永不因扭矩而断开。
        /// </summary>
        extern public float breakTorque { get; set; }

        /// <summary>
        /// 是否启用连接物体之间的碰撞检测。
        /// 启用时，两个通过关节连接的物体可以相互碰撞（产生碰撞事件）。
        /// 禁用时（默认），关节连接的物体彼此穿透而不产生碰撞响应。
        /// </summary>
        extern public bool enableCollision { get; set; }

        /// <summary>
        /// 是否启用关节预处理（Joint Preprocessing）。
        /// 启用时，PhysX 会在求解前预处理关节约束以提高稳定性。
        /// 对于某些特殊场景（如高度连锁的关节链），禁用预处理可能有助于防止不稳定。
        /// </summary>
        extern public bool enablePreprocessing { get; set; }

        /// <summary>
        /// 此物体的质量缩放因子。
        /// 用于调整关节约束中此物体的有效质量，影响力的分配比例。
        /// 默认值 1.0。用于模拟不同重量比例的物体连接。
        /// 底层对应 PhysX 的 PxJoint::setInvMassScale。
        /// </summary>
        extern public float massScale { get; set; }

        /// <summary>
        /// 连接物体的质量缩放因子。
        /// 与 massScale 配合使用，调整关节约束中的质量分配。
        /// 默认值 1.0。
        /// 底层对应 PhysX 的 PxJoint::setInvMassScale（作用于连接物体）。
        /// </summary>
        extern public float connectedMassScale { get; set; }

        /// <summary>
        /// 内部方法：获取关节当前施加的力和扭矩。
        /// 通过引用参数返回线性力和角力，避免 struct 拷贝。
        /// 底层调用 PhysX 的 PxJoint::getConstraint 获取约束求解结果。
        /// </summary>
        extern private void GetCurrentForces(ref Vector3 linearForce, ref Vector3 angularForce);

        /// <summary>关节当前施加的线性力（单位：牛顿），由约束求解器计算。</summary>
        public Vector3 currentForce
        {
            get
            {
                Vector3 force = Vector3.zero;
                Vector3 torque = Vector3.zero;
                GetCurrentForces(ref force, ref torque);
                return force;
            }
        }

        /// <summary>关节当前施加的角力/扭矩（单位：N·m），由约束求解器计算。</summary>
        public Vector3 currentTorque
        {
            get
            {
                Vector3 force = Vector3.zero;
                Vector3 torque = Vector3.zero;
                GetCurrentForces(ref force, ref torque);
                return torque;
            }
        }

        /// <summary>内部方法：获取关节在指定身体索引处的本地姿态矩阵。</summary>
        extern internal Matrix4x4 GetLocalPoseMatrix(int bodyIndex);
    }
}
