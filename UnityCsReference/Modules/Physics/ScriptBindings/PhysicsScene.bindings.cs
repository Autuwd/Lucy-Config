// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using UnityEngine.Internal;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    //=============================================================================
    // PhysicsScene 结构体
    //=============================================================================
    // PhysicsScene 代表一个独立的物理场景，包含其自己的物理世界。
    // Unity 支持多物理场景，每个 Unity Scene 都可以关联一个 PhysicsScene。
    //
    // 默认情况下，所有 Physics.Raycast 等静态方法使用的都是默认物理场景
    // （Physics.defaultPhysicsScene）。
    //
    // 使用 PhysicsSceneExtensions.GetPhysicsScene() 可从 Unity Scene 获取对应的
    // PhysicsScene，实现多场景物理。
    //=============================================================================
    [NativeHeader("Modules/Physics/PhysicsQuery.h")]
    [NativeHeader("Modules/Physics/Public/PhysicsSceneHandle.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct PhysicsScene : IEquatable<PhysicsScene>
    {
        private int m_index;    ///< 物理场景在引擎内部的索引
        private int m_version;  ///< 版本号，用于校验场景句柄的有效性

        public override string ToString() { return string.Format("PhysicsScene(Index: {0}, Version: {1})", m_index, m_version); }
        public static bool operator ==(PhysicsScene lhs, PhysicsScene rhs) { return lhs.m_index == rhs.m_index && lhs.m_version == rhs.m_version; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(PhysicsScene lhs, PhysicsScene rhs) { return !(lhs == rhs); }
        public override int GetHashCode() { return HashCode.Combine(m_index, m_version); }
        public override bool Equals(object other)
        {
            if (!(other is PhysicsScene))
                return false;

            PhysicsScene rhs = (PhysicsScene)other;
            return this == rhs;
        }

        public bool Equals(PhysicsScene other)
        {
            return this == other;
        }

        /// <summary>检查此 PhysicsScene 是否有效（句柄是否指向有效的物理世界）。</summary>
        public bool IsValid() { return IsValid_Internal(this); }
        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("IsPhysicsSceneValid")]
        extern private static bool IsValid_Internal(PhysicsScene physicsScene);

        /// <summary>
        ///   获取默认物理场景（内部方法）。
        ///   必须与 PhysicsSceneHandle.h 中的 kDefaultPhysicsSceneHandle 保持同步。
        ///   默认场景的索引和版本都为 0。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal PhysicsScene GetDefaultScene()
        {
            var scene = new PhysicsScene();
            scene.m_index = 0;
            scene.m_version = 0;

            return scene;
        }

        /// <summary>检查此物理场景是否为空（没有任何物理对象）。</summary>
        public bool IsEmpty()
        {
            if (IsValid())
                return IsEmpty_Internal(this);

            throw new InvalidOperationException("无法检查物理场景是否为空，因为场景句柄无效。");
        }

        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("IsPhysicsWorldEmpty")]
        extern private static bool IsEmpty_Internal(PhysicsScene physicsScene);

        //----- 手动物理仿真 ---------------------------------------------------

        /// <summary>
        ///   在此物理场景上执行一步手动物理仿真。
        ///   仅当 simulationMode 为 Script 时有效。
        /// </summary>
        /// <param name="step">仿真步长（秒）</param>
        public void Simulate(float step)
        {
            if (IsValid())
            {
                // 仅在模拟默认物理场景时检查自动仿真模式
                if (this == GetDefaultScene() && Physics.simulationMode != SimulationMode.Script)
                {
                    Debug.LogWarning("PhysicsScene.Simulate(...) 被调用但仿真模式未设置为 Script。请先将 simulationMode 设置为 Script。");
                    return;
                }

                Physics.Simulate_Internal(this, step, SimulationStage.All, SimulationOption.All);
                return;
            }

            throw new InvalidOperationException("无法模拟物理场景，因为场景句柄无效。");
        }

        /// <summary>
        ///   运行指定的物理仿真阶段。
        ///   允许对仿真进行更细粒度的控制，例如只运行准备阶段而不运行求解阶段。
        /// </summary>
        /// <param name="step">仿真步长（秒）</param>
        /// <param name="stages">要运行的仿真阶段（位掩码）</param>
        /// <param name="options">仿真选项</param>
        public void RunSimulationStages(float step, SimulationStage stages, [DefaultValue("SimulationOption.All")] SimulationOption options = SimulationOption.All)
        {
            if (!IsValid())
                throw new InvalidOperationException("无法模拟物理场景，因为场景句柄无效。");

            if (this == GetDefaultScene() && Physics.simulationMode != SimulationMode.Script)
            {
                Debug.LogWarning("PhysicsScene.Simulate(...) 被调用但仿真模式未设置为 Script。");
                return;
            }

            Physics.Simulate_Internal(this, step, stages, options);
        }

        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("ReleasePhysicsSceneSimulationBuffers")]
        private extern static void ReleasePhysicsSceneSimulationBuffers_Internal(PhysicsScene handle);

        /// <summary>释放上一次仿真步的缓冲区内存。通常在自定义仿真循环后调用。</summary>
        public void ReleaseLastSimulationStepBuffers()
        {
            ReleasePhysicsSceneSimulationBuffers_Internal(this);
        }

        /// <summary>
        ///   对物理场景中的物体进行插值。
        ///   用于支持非默认物理场景的渲染插值。
        ///   默认物理场景的插值是自动完成的。
        /// </summary>
        public void InterpolateBodies()
        {
            if (!IsValid())
                throw new InvalidOperationException("无法对物理场景进行插值，因为场景句柄无效。");

            if (this == Physics.defaultPhysicsScene)
            {
                Debug.LogWarning("PhysicsScene.InterpolateBodies() 在默认物理场景上调用，此操作会自动完成，调用将被忽略。");
                return;
            }

            Physics.InterpolateBodies_Internal(this);
        }

        /// <summary>重置插值姿势。用于同步物理和渲染位置。</summary>
        public void ResetInterpolationPoses()
        {
            if (!IsValid())
                throw new InvalidOperationException("无法重置物理场景的插值姿势，因为场景句柄无效。");

            if (this == Physics.defaultPhysicsScene)
            {
                Debug.LogWarning("PhysicsScene.ResetInterpolationPoses() 在默认物理场景上调用，此操作会自动完成，调用将被忽略。");
                return;
            }

            Physics.ResetInterpolationPoses_Internal(this);
        }

        //=============================================================================
        // PhysicsScene 的物理查询方法
        //
        // 这些方法与 Physics 静态类中的同名方法功能相同，
        // 但作用域限定在当前 PhysicsScene 中。
        //=============================================================================

        //----- 射线检测（是/否）--------------------------------------------------

        /// <summary>检测射线是否与本物理场景中的任何碰撞器相交。</summary>
        public bool Raycast(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("Physics.DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);
                return Internal_RaycastTest(this, ray, maxDistance, layerMask, queryTriggerInteraction);
            }

            return false;
        }

        [FreeFunction("Physics::RaycastTest")]
        extern private static bool Internal_RaycastTest(PhysicsScene physicsScene, Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        //----- 射线检测（单物体）--------------------------------------------------

        /// <summary>检测射线并获取第一个命中的碰撞器详细信息。</summary>
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("Physics.DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            hitInfo = new RaycastHit();

            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);

                return Internal_Raycast(this, ray, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        [FreeFunction("Physics::Raycast")]
        extern private static bool Internal_Raycast(PhysicsScene physicsScene, Ray ray, float maxDistance, ref RaycastHit hit, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        //----- 射线检测（多物体，无分配）------------------------------------------

        /// <summary>无分配射线检测，将结果填充到提供的数组中。</summary>
        public int Raycast(Vector3 origin, Vector3 direction, RaycastHit[] raycastHits, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("Physics.DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                Ray ray = new Ray(origin, direction.normalized);
                return Internal_RaycastNonAlloc(this, ray, raycastHits, maxDistance, layerMask, queryTriggerInteraction);
            }

            return 0;
        }

        [FreeFunction("Physics::RaycastNonAlloc")]
        extern private static int Internal_RaycastNonAlloc(PhysicsScene physicsScene, Ray ray, RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        //----- 胶囊体投射 ---------------------------------------------------

        [FreeFunction("Physics::CapsuleCast")]
        extern private static bool Query_CapsuleCast(PhysicsScene physicsScene, Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, ref RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        private static bool Internal_CapsuleCast(PhysicsScene physicsScene, Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            hitInfo = new RaycastHit();
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_CapsuleCast(physicsScene, point1, point2, radius, normalizedDirection, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        /// <summary>胶囊体投射——获取单个碰撞信息。</summary>
        public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Internal_CapsuleCast(this, point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::CapsuleCastNonAlloc")]
        extern private static int Internal_CapsuleCastNonAlloc(PhysicsScene physicsScene, Vector3 p0, Vector3 p1, float radius, Vector3 direction, RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>胶囊体投射——无分配模式，填充到提供的数组中。</summary>
        public int CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_CapsuleCastNonAlloc(this, point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        //----- 胶囊体重叠检测（无分配）------------------------------------------

        [FreeFunction("Physics::OverlapCapsuleNonAlloc")]
        extern private static int OverlapCapsuleNonAlloc_Internal(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>无分配胶囊体重叠检测——将结果填充到提供的数组中。</summary>
        public int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask = Physics.AllLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapCapsuleNonAlloc_Internal(this, point0, point1, radius, results, layerMask, queryTriggerInteraction);
        }

        //----- 球体投射 ---------------------------------------------------

        [FreeFunction("Physics::SphereCast")]
        extern private static bool Query_SphereCast(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, float maxDistance, ref RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        private static bool Internal_SphereCast(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            hitInfo = new RaycastHit();
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_SphereCast(physicsScene, origin, radius, normalizedDirection, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        /// <summary>球体投射——获取单个碰撞信息。</summary>
        public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Internal_SphereCast(this, origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::SphereCastNonAlloc")]
        extern private static int Internal_SphereCastNonAlloc(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>球体投射——无分配模式。</summary>
        public int SphereCast(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_SphereCastNonAlloc(this, origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        //----- 球体重叠检测（无分配）------------------------------------------

        [FreeFunction("Physics::OverlapSphereNonAlloc")]
        extern private static int OverlapSphereNonAlloc_Internal(PhysicsScene physicsScene, Vector3 position, float radius, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>无分配球体重叠检测——将结果填充到提供的数组中。</summary>
        public int OverlapSphere(Vector3 position, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapSphereNonAlloc_Internal(this, position, radius, results, layerMask, queryTriggerInteraction);
        }

        //----- 盒子投射 ---------------------------------------------------

        [FreeFunction("Physics::BoxCast")]
        extern static private bool Query_BoxCast(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, ref RaycastHit outHit, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        private static bool Internal_BoxCast(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            hitInfo = new RaycastHit();
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_BoxCast(physicsScene, center, halfExtents, normalizedDirection, orientation, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        /// <summary>盒子投射——获取单个碰撞信息。</summary>
        public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Internal_BoxCast(this, center, halfExtents, orientation, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo)
        {
            return Internal_BoxCast(this, center, halfExtents, Quaternion.identity, direction, out hitInfo, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- 盒子重叠检测（无分配）------------------------------------------

        [FreeFunction("Physics::OverlapBoxNonAlloc")]
        extern private static int OverlapBoxNonAlloc_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Collider[] results, Quaternion orientation, int mask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>无分配盒子重叠检测——将结果填充到提供的数组中。</summary>
        public int OverlapBox(Vector3 center, Vector3 halfExtents, Collider[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapBoxNonAlloc_Internal(this, center, halfExtents, results, orientation, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public int OverlapBox(Vector3 center, Vector3 halfExtents, Collider[] results)
        {
            return OverlapBoxNonAlloc_Internal(this, center, halfExtents, results, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- 盒子投射（无分配）----------------------------------------------

        [FreeFunction("Physics::BoxCastNonAlloc")]
        private static extern int Internal_BoxCastNonAlloc(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] raycastHits, Quaternion orientation, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>盒子投射——无分配模式。</summary>
        public int BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_BoxCastNonAlloc(this, center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [ExcludeFromDocs]
        public int BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results)
        {
            return BoxCast(center, halfExtents, direction, results, Quaternion.identity, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }
    }

    /// <summary>
    ///   PhysicsScene 扩展方法。
    ///   提供从 Unity Scene 获取对应 PhysicsScene 的方法。
    /// </summary>
    public static class PhysicsSceneExtensions
    {
        /// <summary>
        ///   获取与指定 Unity Scene 关联的 PhysicsScene。
        ///   如果场景无效或关联的物理场景无效，会抛出异常。
        /// </summary>
        /// <param name="scene">Unity 场景</param>
        /// <returns>对应的 PhysicsScene 结构体</returns>
        public static PhysicsScene GetPhysicsScene(this Scene scene)
        {
            if (!scene.IsValid())
                throw new ArgumentException("无法获取物理场景，Unity 场景无效。", "scene");

            PhysicsScene physicsScene = GetPhysicsScene_Internal(scene);
            if (physicsScene.IsValid())
                return physicsScene;

            throw new Exception("与 Unity 场景关联的物理场景无效。");
        }

        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("GetPhysicsSceneFromUnityScene")]
        extern private static PhysicsScene GetPhysicsScene_Internal(Scene scene);
    }
}
