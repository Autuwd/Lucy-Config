// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    /// <summary>
    /// 角色控制器移动碰撞标志位掩码，由 CharacterController.Move 返回。
    /// 指示角色移动过程中与环境的碰撞情况：是否碰到侧面、上方或下方。
    /// 使用位域（Flags）设计，可组合多个碰撞方向。
    /// </summary>
    public enum CollisionFlags
    {
        /// <summary>未发生碰撞。</summary>
        None = 0,

        /// <summary>角色侧面发生了碰撞（左右方向）。</summary>
        Sides = 1,

        /// <summary>角色上方发生了碰撞（头顶方向）。</summary>
        Above = 2,

        /// <summary>角色下方发生了碰撞（脚底方向，即接触地面）。</summary>
        Below = 4,

        /// <summary>已废弃，功能同 Sides，保留兼容。</summary>
        CollidedSides = 1,

        /// <summary>已废弃，功能同 Above，保留兼容。</summary>
        CollidedAbove = 2,

        /// <summary>已废弃，功能同 Below，保留兼容。</summary>
        CollidedBelow = 4,
    }

    /// <summary>
    /// CharacterController 碰撞事件数据类。
    /// 当角色控制器碰撞到其他物体时，OnControllerColliderHit 回调接收此对象。
    /// 提供碰撞点、法线、移动方向、移动距离等详细信息。
    /// 底层由 C++ 侧填充数据，通过 RequiredByNativeCode 标记的静态方法创建/更新。
    ///
    /// 注意：这是一个 class（引用类型），在碰撞回调中可安全缓存引用。
    /// 使用 s_ReusableCollision 单例模式复用对象以减少 GC 分配。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public partial class ControllerColliderHit
    {
        // 可复用的单例碰撞对象，用于减少 GC 分配（由 C++ 侧触发 Update 复用）
        private static readonly ControllerColliderHit s_ReusableCollision = new ControllerColliderHit();

        //==================== 内部字段 ====================//
        internal CharacterController m_Controller;   // 触发碰撞的角色控制器
        internal Collider m_Collider;                // 被碰撞的碰撞器
        internal Vector3 m_Point;                    // 碰撞点（世界空间）
        internal Vector3 m_Normal;                   // 碰撞点表面法线（世界空间）
        internal Vector3 m_MoveDirection;            // 角色移动方向
        internal float m_MoveLength;                 // 穿透深度（移动距离）
        internal int m_Push;                         // 是否推挤（已废弃，保留二进制兼容）

        //==================== 公开属性 ====================//
        public CharacterController controller { get { return m_Controller; } }
        public Collider collider { get { return m_Collider; } }
        public Rigidbody rigidbody { get { return m_Collider.attachedRigidbody; } }
        public GameObject gameObject { get { return m_Collider.gameObject; } }
        public Transform transform { get { return m_Collider.transform; } }
        public Vector3 point { get { return m_Point; } }
        public Vector3 normal { get { return m_Normal; } }
        public Vector3 moveDirection { get { return m_MoveDirection; } }
        public float moveLength { get { return m_MoveLength; } }
        private bool push { get { return m_Push != 0; } set { m_Push = value ? 1 : 0; } }

        /// <summary>内部方法：一次性设置所有字段值。</summary>
        private void SetAllFields(CharacterController controller, Collider collider, Vector3 point, Vector3 normal, Vector3 moveDirection, float moveLength)
        {
            m_Controller = controller;
            m_Collider = collider;
            m_Point = point;
            m_Normal = normal;
            m_MoveDirection = moveDirection;
            m_MoveLength = moveLength;
            m_Push = 0;
        }

        /// <summary>内部方法：清空所有字段，准备复用。</summary>
        internal void Clear()
        {
            m_Controller = null;
            m_Collider = null;
            m_Point = Vector3.zero;
            m_Normal = Vector3.zero;
            m_MoveDirection = moveDirection;
            m_MoveLength = 0.0f;
            m_Push = 0;
        }

        /// <summary>由 C++ 侧调用的工厂方法：创建新的 ControllerColliderHit 实例。</summary>
        [RequiredByNativeCode]
        static ControllerColliderHit Create(CharacterController controller, Collider collider, Vector3 point, Vector3 normal, Vector3 moveDirection, float moveLength)
        {
            var hit = new ControllerColliderHit();
            hit.SetAllFields(controller, collider, point, normal, moveDirection, moveLength);
            return hit;
        }

        /// <summary>由 C++ 侧调用的更新方法：复用已有实例填充新数据以降低 GC 压力。</summary>
        [RequiredByNativeCode]
        static void Update(ControllerColliderHit hit, CharacterController controller, Collider collider, Vector3 point, Vector3 normal, Vector3 moveDirection, float moveLength)
        {
            hit.SetAllFields(controller, collider, point, normal, moveDirection, moveLength);
        }
    }

    /// <summary>
    /// 角色控制器组件，用于实现不依赖刚体的角色移动。
    /// 底层封装 PhysX 的 PxCapsuleController（基于胶囊体的角色控制器）。
    ///
    /// 与 Rigidbody 碰撞器的区别：
    /// - 不响应物理力（不受重力、冲击力影响）
    /// - 通过 SimpleMove / Move 方法手动控制移动
    /// - 自动处理爬坡、上下台阶、碰撞响应等角色移动逻辑
    /// - 使用特殊的 CCD 算法防止角色穿透环境
    /// </summary>
    [NativeHeader("Modules/Physics/CharacterController.h")]
    public class CharacterController : Collider
    {
        /// <summary>
        /// 简单移动方法（自动应用重力）。
        /// 以给定速度在世界空间中水平移动角色，自动处理重力下落和地面检测。
        /// 返回值表示角色是否位于地面上。
        /// 底层调用 PhysX 的 PxController::move。
        /// </summary>
        /// <param name="speed">水平方向的速度向量（m/s），Y 分量被忽略。</param>
        /// <returns>如果角色接触地面返回 true。</returns>
        extern public bool SimpleMove(Vector3 speed);

        /// <summary>
        /// 手动移动方法（不自动应用重力）。
        /// 按给定的位移向量移动角色，需要手动处理重力和跳跃逻辑。
        /// 返回 CollisionFlags 指示碰撞方向。
        /// 底层调用 PhysX 的 PxController::move。
        /// </summary>
        /// <param name="motion">位移向量（世界空间，单位：米）。</param>
        /// <returns>碰撞标志位掩码，指示碰撞方向。</returns>
        extern public CollisionFlags Move(Vector3 motion);

        /// <summary>角色当前的移动速度（世界空间，单位：m/s）。</summary>
        extern public Vector3 velocity { get; }

        /// <summary>角色是否接触地面（位于可以站立移动的表面上）。</summary>
        extern public bool isGrounded { [NativeName("IsGrounded")] get; }

        /// <summary>最近一次 Move 调用的碰撞标志位。</summary>
        extern public CollisionFlags collisionFlags { get; }

        /// <summary>胶囊体的半径（单位：米）。</summary>
        extern public float radius { get; set; }

        /// <summary>胶囊体的高度（单位：米）。</summary>
        extern public float height { get; set; }

        /// <summary>胶囊体中心相对于变换位置的偏移。</summary>
        extern public Vector3 center { get; set; }

        /// <summary>最大可爬坡角度（度数，范围 [0, 90]），默认 45°。</summary>
        extern public float slopeLimit { get; set; }

        /// <summary>最大可跨过的台阶高度（单位：米）。</summary>
        extern public float stepOffset { get; set; }

        /// <summary>
        /// 皮肤宽度（单位：米），角色与其他物体之间的最小间距。
        /// 用于防止角色卡在环境中，值越大角色越不容易被卡住，但穿透感越明显。
        /// </summary>
        extern public float skinWidth { get; set; }

        /// <summary>
        /// 最小移动距离（单位：米）。
        /// 如果移动量小于此值，控制器不会移动（可以避免微小抖动）。
        /// </summary>
        extern public float minMoveDistance { get; set; }

        /// <summary>是否启用碰撞检测。禁用时角色可穿过其他物体。</summary>
        extern public bool detectCollisions { get; set; }

        /// <summary>
        /// 是否启用重叠恢复（Overlap Recovery）。
        /// 启用后，角色从嵌入状态自动推出。推荐开启以防止角色被挤入环境。
        /// </summary>
        extern public bool enableOverlapRecovery { get; set; }

        /// <summary>内部属性：角色当前是否被支撑（有可站立的表面）。</summary>
        extern internal bool isSupported { get; }
    }
}
