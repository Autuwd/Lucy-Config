// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine
{
    //=============================================================================
    // RigidbodyConstraints 枚举
    //=============================================================================
    // 控制 Rigidbody 在哪些轴向上被冻结位置/旋转。
    // 位标志组合，例如：FreezePositionX | FreezeRotationY
    //=============================================================================
    [Flags]
    public enum RigidbodyConstraints
    {
        None = 0,                               ///< 无约束，所有自由度全部开放
        FreezePositionX = 1 << 1,               ///< 冻结 X 轴位置
        FreezePositionY = 1 << 2,               ///< 冻结 Y 轴位置
        FreezePositionZ = 1 << 3,               ///< 冻结 Z 轴位置
        FreezeRotationX = 1 << 4,               ///< 冻结 X 轴旋转
        FreezeRotationY = 1 << 5,               ///< 冻结 Y 轴旋转
        FreezeRotationZ = 1 << 6,               ///< 冻结 Z 轴旋转
        FreezePosition = FreezePositionX | FreezePositionY | FreezePositionZ,  ///< 冻结所有位置
        FreezeRotation = FreezeRotationX | FreezeRotationY | FreezeRotationZ,  ///< 冻结所有旋转
        FreezeAll = FreezePosition | FreezeRotation  ///< 冻结所有自由度和旋转
    }

    /// <summary>
    ///   Rigidbody 渲染插值模式。
    ///   解决物理步长（FixedUpdate）与渲染帧率不一致导致的抖动问题。
    /// </summary>
    public enum RigidbodyInterpolation
    {
        None = 0,          ///< 无插值，直接使用最新位置
        Interpolate = 1,   ///< 插值：基于上一帧和当前帧位置平滑过渡
        Extrapolate = 2    ///< 外推：基于运动趋势预测位置
    }

    //=============================================================================
    // Rigidbody 组件
    //=============================================================================
    // Rigidbody 是 Unity 物理系统中控制物体运动的核心组件。
    // 为 GameObject 添加 Rigidbody 后，它将受物理引擎控制：
    //   - 受重力影响（useGravity）
    //   - 与其他碰撞器产生碰撞响应
    //   - 可以通过 AddForce/AddTorque 施加力/力矩
    //
    // 两种主要模式：
    //   1. 动力学模式（isKinematic = false）：
    //      - 通过力/力矩/碰撞来驱动运动
    //      - 受重力影响
    //   2. 运动学模式（isKinematic = true）：
    //      - 不受重力/力/碰撞影响
    //      - 通过 MovePosition/MoveRotation 控制位置
    //      - 可以推动其他动力学物体
    //
    // 注意：Rigidbody 需要 Transform 组件（[RequireComponent(typeof(Transform))]）。
    //=============================================================================
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/Rigidbody.h")]
    public partial class Rigidbody : Component
    {
        //----- 基本运动属性 ---------------------------------------------------

        /// <summary>
        ///   线速度向量（米/秒）。
        ///   表示 Rigidbody 当前移动的速度大小和方向。
        ///   （旧版 API Rigidbody.velocity 的替代）
        /// </summary>
        extern public Vector3 linearVelocity { get; set; }

        /// <summary>
        ///   角速度向量（弧度/秒）。
        ///   表示 Rigidbody 当前旋转的速度。向量方向为旋转轴，大小为旋转速度。
        ///   （旧版 API Rigidbody.angularVelocity 的替代）
        /// </summary>
        extern public Vector3 angularVelocity { get; set; }

        /// <summary>
        ///   线性阻尼。
        ///   模拟空气阻力等线性速度衰减。值越大，物体减速越快。
        ///   范围：0 到 Infinity。默认值为 0。
        ///   （旧版 API Rigidbody.drag 的替代）
        /// </summary>
        extern public float linearDamping { get; set; }

        /// <summary>
        ///   角阻尼。
        ///   模拟旋转摩擦。值越大，旋转减速越快。
        ///   范围：0 到 Infinity。默认值为 0.05。
        ///   （旧版 API Rigidbody.angularDrag 的替代）
        /// </summary>
        extern public float angularDamping { get; set; }

        /// <summary>
        ///   质量（千克）。
        ///   影响重力效果、碰撞响应和力的加速度（F = ma）。
        ///   质量越大，同等力下加速度越小。
        ///   推荐范围：0.1 到 1000。
        /// </summary>
        extern public float mass { get; set; }

        /// <summary>此 Rigidbody 是否受重力影响。默认值为 true。</summary>
        extern public bool useGravity { get; set; }

        /// <summary>
        ///   最大分离速度。
        ///   当 Rigidbody 与其他碰撞器穿透时，物理引擎将其推开的最大速度。
        ///   限制此值可以防止"爆炸式分离"现象。
        /// </summary>
        extern public float maxDepenetrationVelocity { get; set; }

        /// <summary>
        ///   是否为运动学（Kinematic）模式。
        ///   true：不受力/重力/碰撞的物理影响，通过 MovePosition/Rotation 控制。
        ///   false（默认）：受物理引擎控制，通过力/碰撞来驱动。
        /// </summary>
        extern public bool isKinematic { get; set; }

        /// <summary>
        ///   冻结旋转的便捷属性。
        ///   设置后等效于在 constraints 中添加或移除 FreezeRotation 标志。
        ///   注意 setter 中有一个已知 bug：
        ///   当设为 false 时使用 constraints &= ~FreezeRotation 更准确，
        ///   而不是当前的 constraints &= FreezePosition。
        /// </summary>
        public bool freezeRotation
        {
            get => constraints.HasFlag(RigidbodyConstraints.FreezeRotation);
            set
            {
                if (value)
                    constraints |= RigidbodyConstraints.FreezeRotation;
                else
                    constraints &= RigidbodyConstraints.FreezePosition;
            }
        }

        /// <summary>
        ///   刚体的约束标志。
        ///   控制哪些位置和旋转轴被冻结（不参与物理运动）。
        ///   例如：constraints = RigidbodyConstraints.FreezePositionZ 将冻结 Z 轴移动。
        /// </summary>
        extern public RigidbodyConstraints constraints { get; set; }

        /// <summary>
        ///   碰撞检测模式。
        ///   Discrete（离散）：默认模式，性能最高，但快速移动时可能穿透。
        ///   Continuous（连续）：对与其他物体的碰撞使用连续碰撞检测。
        ///   ContinuousDynamic（连续动态）：用于高速物体。
        ///   ContinuousSpeculative（推测连续）：基于推测的 CCD 模式。
        /// </summary>
        extern public CollisionDetectionMode collisionDetectionMode { get; set; }

        //----- 质心与惯性张量 ------------------------------------------------

        /// <summary>是否自动计算质心。默认值为 true。</summary>
        extern public bool automaticCenterOfMass { get; set; }

        /// <summary>
        ///   质心位置（局部坐标）。
        ///   如果 automaticCenterOfMass 为 true，此值由物理引擎根据碰撞器自动计算。
        ///   手动设置后可改变物体的旋转行为。
        /// </summary>
        extern public Vector3 centerOfMass { get; set; }

        /// <summary>质心的世界空间位置（只读）。</summary>
        extern public Vector3 worldCenterOfMass { get; }

        /// <summary>是否自动计算惯性张量。默认值为 true。</summary>
        extern public bool automaticInertiaTensor { get; set; }

        /// <summary>惯性张量的旋转（局部坐标）。</summary>
        extern public Quaternion inertiaTensorRotation { get; set; }

        /// <summary>
        ///   惯性张量（局部坐标）。
        ///   控制物体绕各轴旋转的惯性。值越大，旋转越难以改变。
        ///   如果 automaticInertiaTensor 为 true，此值由引擎自动计算。
        /// </summary>
        extern public Vector3 inertiaTensor { get; set; }

        extern internal Matrix4x4 worldInertiaTensorMatrix { get; }

        /// <summary>是否启用碰撞检测。禁用后该 Rigidbody 不与其他物体碰撞。</summary>
        extern public bool detectCollisions { get; set; }

        /// <summary>
        ///   Rigidbody 的位置（世界坐标）。
        ///   通过此属性设置位置会直接移动 Rigidbody，不影响速度。
        ///   推荐使用 MovePosition 来实现平滑运动。
        /// </summary>
        extern public Vector3 position { get; set; }

        /// <summary>
        ///   Rigidbody 的旋转（世界坐标）。
        ///   推荐使用 MoveRotation 来实现平滑旋转。
        /// </summary>
        extern public Quaternion rotation { get; set; }

        /// <summary>
        ///   渲染插值模式。
        ///   None：不插值。
        ///   Interpolate：基于前两帧位置插值（推荐）。
        ///   Extrapolate：基于运动趋势外推。
        /// </summary>
        extern public RigidbodyInterpolation interpolation { get; set; }

        /// <summary>此 Rigidbody 的求解器迭代次数。覆盖 Physics.defaultSolverIterations。</summary>
        extern public int solverIterations { get; set; }

        /// <summary>睡眠阈值。低于此值时 Rigidbody 进入睡眠以节省性能。</summary>
        extern public float sleepThreshold { get; set; }

        /// <summary>最大角速度（弧度/秒）。防止高速旋转导致不稳定。</summary>
        extern public float maxAngularVelocity { get; set; }

        /// <summary>最大线速度（米/秒）。限制物体最大移动速度。</summary>
        extern public float maxLinearVelocity { get; set; }

        //----- 运动控制方法 ---------------------------------------------------

        /// <summary>
        ///   使用 MovePosition 将 Rigidbody 移动到指定位置。
        ///   此方法会驱动物理引擎在下一个物理步长中完成移动，
        ///   不会产生瞬移，适合实现平滑运动。
        ///   通常在 FixedUpdate 中调用。
        /// </summary>
        extern public void MovePosition(Vector3 position);

        /// <summary>
        ///   使用 MoveRotation 将 Rigidbody 旋转到指定角度。
        ///   与 MovePosition 配合使用，实现平滑的物理运动。
        /// </summary>
        extern public void MoveRotation(Quaternion rotation);

        /// <summary>
        ///   同时移动位置和旋转。Move 等效于顺序调用 MovePosition 和 MoveRotation。
        /// </summary>
        extern public void Move(Vector3 position, Quaternion rotation);

        /// <summary>强制 Rigidbody 进入睡眠状态，停止物理模拟以节省性能。</summary>
        extern public void Sleep();

        /// <summary>查询 Rigidbody 是否处于睡眠状态。</summary>
        extern public bool IsSleeping();

        /// <summary>唤醒 Rigidbody，重新参与物理模拟。</summary>
        extern public void WakeUp();

        /// <summary>重置质心为引擎自动计算的位置。</summary>
        extern public void ResetCenterOfMass();

        /// <summary>重置惯性张量为引擎自动计算的值。</summary>
        extern public void ResetInertiaTensor();

        /// <summary>
        ///   获取 Rigidbody 上指定局部点的速度。
        ///   可用于计算角色脚部等局部位置的速度。
        /// </summary>
        /// <param name="relativePoint">相对 Rigidbody 局部坐标的点</param>
        extern public Vector3 GetRelativePointVelocity(Vector3 relativePoint);

        /// <summary>
        ///   获取 Rigidbody 上指定世界点的速度。
        ///   结合线速度和角速度计算该点的合速度。
        /// </summary>
        /// <param name="worldPoint">世界空间中的点</param>
        extern public Vector3 GetPointVelocity(Vector3 worldPoint);

        /// <summary>覆盖 Physics.defaultSolverVelocityIterations 的迭代次数。</summary>
        extern public int solverVelocityIterations { get; set; }

        /// <summary>将 Transform 位置同步到物理引擎。</summary>
        extern public void PublishTransform();

        //----- 碰撞层过滤 -----------------------------------------------------

        /// <summary>
        ///   排除的碰撞层。
        ///   此 Rigidbody 上的碰撞器不会与这些层上的碰撞器发生碰撞。
        /// </summary>
        extern public LayerMask excludeLayers { get; set; }

        /// <summary>
        ///   包含的碰撞层。
        ///   此 Rigidbody 上的碰撞器仅与这些层上的碰撞器发生碰撞。
        /// </summary>
        extern public LayerMask includeLayers { get; set; }

        //----- 累积力/力矩信息 -----------------------------------------------

        /// <summary>
        ///   获取指定时间步长内施加到此 Rigidbody 上的累积力。
        ///   用于调试和自定义力反馈。
        /// </summary>
        /// <param name="step">时间步长（默认 Time.fixedDeltaTime）</param>
        extern public Vector3 GetAccumulatedForce([DefaultValue("Time.fixedDeltaTime")] float step);

        [ExcludeFromDocs]
        public Vector3 GetAccumulatedForce()
        {
            return GetAccumulatedForce(Time.fixedDeltaTime);
        }

        /// <summary>获取指定时间步长内施加到此 Rigidbody 上的累积力矩。</summary>
        extern public Vector3 GetAccumulatedTorque([DefaultValue("Time.fixedDeltaTime")] float step);

        [ExcludeFromDocs]
        public Vector3 GetAccumulatedTorque()
        {
            return GetAccumulatedTorque(Time.fixedDeltaTime);
        }

        //----- 施加力的方法 ---------------------------------------------------
        //
        // ForceMode 说明：
        //   Force：连续力（牛顿），质量相关，F = ma，持续施加
        //   Acceleration：连续加速度（m/s²），忽略质量
        //   Impulse：瞬时冲量（牛顿·秒），质量相关，用于瞬间力（如子弹击中）
        //   VelocityChange：瞬时速度变化（m/s），忽略质量
        //

        /// <summary>
        ///   在世界空间中对 Rigidbody 施加一个力。
        ///   力是持续作用的，需要在 FixedUpdate 中每帧施加。
        /// </summary>
        /// <param name="force">力的向量（牛顿）</param>
        /// <param name="mode">力的模式（默认 ForceMode.Force）</param>
        extern public void AddForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForce(Vector3 force)
        {
            AddForce(force, ForceMode.Force);
        }

        /// <summary>用 x/y/z 分量施加力。</summary>
        public void AddForce(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddForce(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddForce(float x, float y, float z)
        {
            AddForce(new Vector3(x, y, z), ForceMode.Force);
        }

        /// <summary>
        ///   在局部空间中对 Rigidbody 施加一个力。
        ///   力的方向相对于 Rigidbody 自身的旋转。
        /// </summary>
        extern public void AddRelativeForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeForce(Vector3 force)
        {
            AddRelativeForce(force, ForceMode.Force);
        }

        public void AddRelativeForce(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddRelativeForce(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddRelativeForce(float x, float y, float z)
        {
            AddRelativeForce(new Vector3(x, y, z), ForceMode.Force);
        }

        /// <summary>
        ///   在世界空间中对 Rigidbody 施加力矩（扭矩），使其旋转。
        ///   力矩向量方向为旋转轴，大小为旋转力的大小。
        /// </summary>
        extern public void AddTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddTorque(Vector3 torque)
        {
            AddTorque(torque, ForceMode.Force);
        }

        public void AddTorque(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddTorque(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddTorque(float x, float y, float z)
        {
            AddTorque(new Vector3(x, y, z), ForceMode.Force);
        }

        /// <summary>在局部空间中施加力矩。</summary>
        extern public void AddRelativeTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeTorque(Vector3 torque)
        {
            AddRelativeTorque(torque, ForceMode.Force);
        }

        public void AddRelativeTorque(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddRelativeTorque(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddRelativeTorque(float x, float y, float z)
        {
            AddRelativeTorque(x, y, z, ForceMode.Force);
        }

        /// <summary>
        ///   在世界空间中的指定位置施加力。
        ///   这会在 Rigidbody 上同时产生线性和旋转效果。
        ///   例如在物体的边缘施加力会使物体旋转。
        /// </summary>
        /// <param name="force">力的向量</param>
        /// <param name="position">施加力的世界空间位置</param>
        /// <param name="mode">力的模式</param>
        extern public void AddForceAtPosition(Vector3 force, Vector3 position, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForceAtPosition(Vector3 force, Vector3 position)
        {
            AddForceAtPosition(force, position, ForceMode.Force);
        }

        /// <summary>
        ///   施加爆炸力。
        ///   模拟爆炸效果，根据物体到爆炸中心的距离计算力的大小。
        ///   explosionForce：爆炸力大小
        ///   explosionPosition：爆炸中心点
        ///   explosionRadius：爆炸影响半径
        ///   upwardsModifier：向上修正因子（让爆炸效果更立体）
        /// </summary>
        extern public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, [DefaultValue("0.0f")] float upwardsModifier, [DefaultValue("ForceMode.Force)")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier)
        {
            AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, ForceMode.Force);
        }

        [ExcludeFromDocs]
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius)
        {
            AddExplosionForce(explosionForce, explosionPosition, explosionRadius, 0.0f, ForceMode.Force);
        }

        //----- 边界上的最近点 -------------------------------------------------

        [NativeName("ClosestPointOnBounds")]
        extern private void Internal_ClosestPointOnBounds(Vector3 point, ref Vector3 outPos, ref float distance);

        /// <summary>
        ///   获取 Rigidbody 的 AABB 边界上离指定点最近的点。
        ///   相当于 Rigidbody 的 Collider.ClosestPointOnBounds 的快捷方式。
        /// </summary>
        public Vector3 ClosestPointOnBounds(Vector3 position)
        {
            float dist = 0f;
            Vector3 outpos = Vector3.zero;
            Internal_ClosestPointOnBounds(position, ref outpos, ref dist);
            return outpos;
        }

        //----- 扫描检测（SweepTest）--------------------------------------------
        // SweepTest 将 Rigidbody 的碰撞器沿方向扫描，检测路径上的碰撞器。
        // 类似于 Physics.SphereCast/BoxCast，但使用 Rigidbody 自身的碰撞器形状。
        //=============================================================================

        extern private RaycastHit SweepTest(Vector3 direction, float maxDistance, QueryTriggerInteraction queryTriggerInteraction, ref bool hasHit);

        /// <summary>
        ///   将 Rigidbody 的碰撞器沿方向扫描，检测是否碰到其他碰撞器。
        ///   方向向量会被自动归一化，零向量返回 false。
        /// </summary>
        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                bool hasHit = false;
                hitInfo = SweepTest(normalizedDirection, maxDistance, queryTriggerInteraction, ref hasHit);
                return hasHit;
            }
            else
            {
                hitInfo = new RaycastHit();
                return false;
            }
        }

        [ExcludeFromDocs]
        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return SweepTest(direction, out hitInfo, maxDistance, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo)
        {
            return SweepTest(direction, out hitInfo, Mathf.Infinity, QueryTriggerInteraction.UseGlobal);
        }

        /// <summary>将 Rigidbody 沿方向扫描，返回路径上所有碰撞器（产生 GC 分配）。</summary>
        [NativeName("SweepTestAll")]
        extern private RaycastHit[] Internal_SweepTestAll(Vector3 direction, float maxDistance, QueryTriggerInteraction queryTriggerInteraction);

        public RaycastHit[] SweepTestAll(Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                return Internal_SweepTestAll(normalizedDirection, maxDistance, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        public RaycastHit[] SweepTestAll(Vector3 direction, float maxDistance)
        {
            return SweepTestAll(direction, maxDistance, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public RaycastHit[] SweepTestAll(Vector3 direction)
        {
            return SweepTestAll(direction, Mathf.Infinity, QueryTriggerInteraction.UseGlobal);
        }
    }
}
