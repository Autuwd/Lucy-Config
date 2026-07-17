// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    /// <summary>
    /// Physics 类的部分定义，包含接触事件（Contact Event）和碰撞回调的分发逻辑。
    ///
    /// 本文件负责处理 PhysX 原生层传递上来的接触数据（ContactPairHeader），
    /// 将其转换为 C# 层的 Collision 对象，并分发到 MonoBehaviour 的
    /// OnCollisionEnter/Stay/Exit 回调。
    ///
    /// 核心流程：
    /// 1. C++ 侧调用 OnSceneContact（通过 RequiredByNativeCode）
    /// 2. 将原生缓冲区转换为托管 NativeArray&lt;ContactPairHeader&gt;
    /// 3. 触发 ContactEvent 委托（高层 API）
    /// 4. ReportContacts 遍历所有接触对，分发到各个 MonoBehaviour
    /// </summary>
    public partial class Physics
    {
        // 使用 delegate 而非 Action<T> 可以在用户项目中获得更好的代码补全（尤其是参数名称提示）
        /// <summary>
        /// 接触事件委托类型，接收物理场景和只读的 ContactPairHeader 数组。
        /// </summary>
        /// <param name="scene">发生接触的物理场景。</param>
        /// <param name="headerArray">接触对头部信息数组（只读）。</param>
        public delegate void ContactEventDelegate(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly headerArray);

        /// <summary>
        /// 接触事件（Contact Event），在每次物理模拟中发生接触时触发。
        /// 提供低层级接触数据（ContactPairHeader），适合需要直接处理 PhysX 接触对的高级物理系统。
        /// 比 OnCollisionX 回调更底层，数据更丰富。
        /// </summary>
        public static event ContactEventDelegate ContactEvent;

        // 可复用的 Collision 对象（当 reuseCollisionCallbacks 启用时）
        private static readonly Collision s_ReusableCollision = new Collision();

        // Profiler 标记，用于性能分析
        static readonly ProfilerMarker s_ContactEventMarker = new ProfilerMarker("Physics.ContactEvent");
        static readonly ProfilerMarker s_InvokeOnCollisionEventsMarker = new ProfilerMarker("Physics.InvokeOnCollisionEvents");

        /// <summary>
        /// [由 C++ 侧调用] 当物理场景中的接触被检测到时触发。
        /// 将原生缓冲区转换为托管数组，先触发 ContactEvent 委托（高层 API），
        /// 然后调用 ReportContacts 分发到各个 MonoBehaviour 的 OnCollisionX 回调。
        /// </summary>
        /// <param name="scene">发生接触的物理场景。</param>
        /// <param name="buffer">指向 ContactPairHeader 数组的原生指针。</param>
        /// <param name="count">ContactPairHeader 的数量。</param>
        [RequiredByNativeCode]
        private static unsafe void OnSceneContact(PhysicsScene scene, IntPtr buffer, int count)
        {
            if (count == 0)
                return;

            // 将原生缓冲区包装为托管 NativeArray（不复制数据）
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ContactPairHeader>(buffer.ToPointer(), count, Allocator.None);

            // 创建线程安全句柄（防止在 Job 中误用）
            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);

            try
            {
                using (s_ContactEventMarker.Auto())
                {
                    ContactEvent?.Invoke(scene, array.AsReadOnly());
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                ReportContacts(array.AsReadOnly());
            }

            AtomicSafetyHandle.Release(safety);
        }

        /// <summary>
        /// 遍历所有 ContactPairHeader，提取每个接触对中的碰撞状态
        /// （Enter/Stay/Exit），并分发到对应的 MonoBehaviour。
        ///
        /// 处理逻辑：
        /// - 忽略已移除的物体/碰撞器
        /// - 为碰撞双方都生成 Collision 对象（通过 flipped 参数反转视角）
        /// - 根据 CollisionPairEventFlags 判断是 Enter/Stay/Exit
        /// </summary>
        private static void ReportContacts(NativeArray<ContactPairHeader>.ReadOnly array)
        {
            if (!Physics.invokeCollisionCallbacks)
                return;

            using (s_InvokeOnCollisionEventsMarker.Auto())
            {
                for (int i = 0; i < array.Length; i++)
                {
                    ContactPairHeader header = array[i];

                    if (header.hasRemovedBody)
                        continue;

                    for (int j = 0; j < header.m_NbPairs; j++)
                    {
                        ref readonly ContactPair pair = ref header.GetContactPair(j);

                        if (pair.hasRemovedCollider)
                            continue;

                        var actor = header.body;
                        var otherActor = header.otherBody;
                        var component = actor != null ? actor : pair.collider;
                        var otherComponent = otherActor != null ? otherActor : pair.otherCollider;

                        if (!component || !otherComponent)
                            continue;

                        if (pair.isCollisionEnter)
                        {
                            Physics.SendOnCollisionEnter(component, GetCollisionToReport(in header, in pair, false));
                            Physics.SendOnCollisionEnter(otherComponent, GetCollisionToReport(in header, in pair, true));
                        }
                        if (pair.isCollisionStay)
                        {
                            Physics.SendOnCollisionStay(component, GetCollisionToReport(in header, in pair, false));
                            Physics.SendOnCollisionStay(otherComponent, GetCollisionToReport(in header, in pair, true));
                        }
                        if (pair.isCollisionExit)
                        {
                            Physics.SendOnCollisionExit(component, GetCollisionToReport(in header, in pair, false));
                            Physics.SendOnCollisionExit(otherComponent, GetCollisionToReport(in header, in pair, true));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取要报告给回调的 Collision 对象。
        /// 当 reuseCollisionCallbacks 启用时，复用全局 s_ReusableCollision 以减少 GC 分配；
        /// 否则创建新的 Collision 实例。
        /// </summary>
        private static Collision GetCollisionToReport(in ContactPairHeader header, in ContactPair pair, bool flipped)
        {
            if (reuseCollisionCallbacks)
            {
                s_ReusableCollision.Reuse(in header, in pair);
                s_ReusableCollision.Flipped = flipped;
                return s_ReusableCollision;
            }
            else
            {
                return new Collision(in header, in pair, flipped);
            }
        }
    }

    //======================================================================
    //  ContactPairHeader — 接触对头部（一次物理接触的高级描述）
    //  对应 C++ MessageParameters.h 中的数据结构
    //======================================================================
    /// <summary>
    /// 接触对头部信息结构体，描述一对发生接触的物理物体的概要信息。
    /// 包含两个物体的实体 ID、速度、以及指向 ContactPair 数组的指针。
    ///
    /// 每个 ContactPairHeader 可以包含多个 ContactPair（一对物体可能有多个接触点）。
    /// 底层对应 PhysX 的 PxContactPairHeader，用于 PxSimulationEventCallback。
    /// </summary>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct ContactPairHeader
    {
        //==================== 内部只读字段 ====================//
        internal readonly EntityId m_BodyID;              // "本物体" Rigidbody/ArticulationBody 的实体 ID
        internal readonly EntityId m_OtherBodyID;         // "对方物体" Rigidbody/ArticulationBody 的实体 ID
        internal readonly IntPtr m_StartPtr;              // 指向 ContactPair 数组的起始指针
        internal readonly uint m_NbPairs;                 // 此头部下包含的 ContactPair 数量
        internal readonly CollisionPairHeaderFlags m_Flags;  // 头部标志位
        internal readonly Vector3 m_ThisBodyLinearVelocity;  // "本物体"的线速度
        internal readonly Vector3 m_ThisBodyAngularVelocity; // "本物体"的角速度
        internal readonly Vector3 m_OtherBodyLinearVelocity;  // "对方物体"的线速度
        internal readonly Vector3 m_OtherBodyAngularVelocity; // "对方物体"的角速度

        //==================== 已废弃属性 ====================//
        [Obsolete("bodyInstanceID is deprecated, use bodyEntityId instead.", true)]
        public int bodyInstanceID => m_BodyID;
        [Obsolete("otherBodyInstanceID is deprecated, use otherBodyEntityId instead.", true)]
        public int otherBodyInstanceID => m_OtherBodyID;

        //==================== 公开属性 ====================//
        public EntityId bodyEntityId => m_BodyID;
        public EntityId otherBodyEntityId => m_OtherBodyID;

        public Component body => Physics.GetBodyByInstanceID(m_BodyID) as Component;
        public Component otherBody => Physics.GetBodyByInstanceID(m_OtherBodyID) as Component;

        public Vector3 bodyLinearVelocity => m_ThisBodyLinearVelocity;
        public Vector3 bodyAngularVelocity => m_ThisBodyAngularVelocity;

        public Vector3 otherBodyLinearVelocity => m_OtherBodyLinearVelocity;
        public Vector3 otherBodyAngularVelocity => m_OtherBodyAngularVelocity;

        /// <summary>此物理接触中包含的 ContactPair 数量（对应碰撞器的不同部分）。</summary>
        public int pairCount => (int)m_NbPairs;

        /// <summary>头部是否标记了"物体已被移除"。如果任一物体被销毁，不再分发碰撞回调。</summary>
        internal bool hasRemovedBody => (m_Flags & CollisionPairHeaderFlags.RemovedActor) != 0
                                     || (m_Flags & CollisionPairHeaderFlags.RemovedOtherActor) != 0;

        /// <summary>获取指定索引的 ContactPair 引用（只读）。</summary>
        public unsafe ref readonly ContactPair GetContactPair(int index)
        {
            return ref *GetContactPair_Internal(index);
        }

        /// <summary>内部方法：获取指定索引的 ContactPair 指针。</summary>
        internal unsafe ContactPair* GetContactPair_Internal(int index)
        {
            if (index >= m_NbPairs)
                throw new IndexOutOfRangeException("无效的 ContactPair 索引。索引应在 0 和 ContactPairHeader.PairCount 之间。");

            return (ContactPair*)(m_StartPtr.ToInt64() + index * sizeof(ContactPair));
        }
    }

    //======================================================================
    //  ContactPair — 单个接触对（具体的物体对间接触信息）
    //======================================================================
    /// <summary>
    /// 接触对结构体，描述两个碰撞器之间的接触详情。
    /// 包含两个碰撞器的实体 ID、接触点数量、标志位、总冲量等。
    /// 通过 m_StartPtr 和 m_NbPoints 引用实际的 ContactPairPoint 数组。
    ///
    /// 底层对应 PhysX 的 PxContactPair。
    /// </summary>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe readonly partial struct ContactPair
    {
        private const uint c_InvalidFaceIndex = 0xffffFFFF;  // 非法三角面索引标记

        internal readonly EntityId m_ColliderID;          // "本碰撞器" 实体 ID
        internal readonly EntityId m_OtherColliderID;     // "对方碰撞器" 实体 ID
        internal readonly IntPtr m_StartPtr;              // 指向 ContactPairPoint 数组的起始指针
        internal readonly uint m_NbPoints;                // 接触点数量
        internal readonly CollisionPairFlags m_Flags;     // 碰撞对标志位
        internal readonly CollisionPairEventFlags m_Events;  // 碰撞事件标志位
        internal readonly Vector3 m_ImpulseSum;           // 总冲量（所有接触点冲量和）

        //==================== 已废弃属性 ====================//
        [Obsolete("colliderInstanceID is deprecated, use colliderEntityId instead.", true)]
        public int colliderInstanceID => m_ColliderID;
        [Obsolete("otherColliderInstanceID is deprecated, use otherColliderEntityId instead.", true)]
        public int otherColliderInstanceID => m_OtherColliderID;

        //==================== 公开属性 ====================//
        public EntityId colliderEntityId => m_ColliderID;
        public EntityId otherColliderEntityId => m_OtherColliderID;

        public Collider collider => m_ColliderID == EntityId.None ? null : Physics.GetColliderByInstanceID(m_ColliderID) as Collider;
        public Collider otherCollider => m_OtherColliderID == EntityId.None ? null : Physics.GetColliderByInstanceID(m_OtherColliderID) as Collider;

        /// <summary>接触点数量（此接触对中具体的碰撞接触点数）。</summary>
        public int contactCount => (int)m_NbPoints;

        /// <summary>所有接触点的冲量和（总碰撞冲量）。</summary>
        public Vector3 impulseSum => m_ImpulseSum;

        /// <summary>此接触对是否发生了"新碰撞进入"事件。</summary>
        public bool isCollisionEnter => (m_Events & CollisionPairEventFlags.NotifyTouchFound) != 0;

        /// <summary>此接触对是否发生了"碰撞分离"事件。</summary>
        public bool isCollisionExit => (m_Events & CollisionPairEventFlags.NotifyTouchLost) != 0;

        /// <summary>此接触对是否持续保持接触（碰撞持续中）。</summary>
        public bool isCollisionStay => (m_Events & CollisionPairEventFlags.NotifyTouchPersists) != 0;

        /// <summary>碰撞器中是否有已被移除的。</summary>
        internal bool hasRemovedCollider => (m_Flags & CollisionPairFlags.RemovedShape) != 0
                                         || (m_Flags & CollisionPairFlags.RemovedOtherShape) != 0;

        //==================== 提取接触点方法 ====================//

        /// <summary>
        /// 将原生接触点数据提取到托管 List&lt;ContactPoint&gt; 中。
        /// 注意：传入的 List 必须已预先设置足够的 Capacity。
        /// </summary>
        internal int ExtractContacts(List<ContactPoint> managedContainer, bool flipped)
        {
            int size = (int)Math.Min(managedContainer.Capacity, m_NbPoints);
            managedContainer.Clear();

            for (int i = 0; i < size; ++i)
            {
                ref readonly ContactPairPoint nativePoint = ref GetContactPoint(i);
                var contactPoint = new ContactPoint()
                {
                    m_Point = nativePoint.position,
                    m_Impulse = nativePoint.impulse,
                    m_Separation = nativePoint.separation,
                };

                if (flipped)
                {
                    contactPoint.m_Normal = -nativePoint.normal;
                    contactPoint.m_ThisColliderEntityId = m_OtherColliderID;
                    contactPoint.m_OtherColliderEntityId = m_ColliderID;
                }
                else
                {
                    contactPoint.m_Normal = nativePoint.normal;
                    contactPoint.m_ThisColliderEntityId = m_ColliderID;
                    contactPoint.m_OtherColliderEntityId = m_OtherColliderID;
                }

                managedContainer.Add(contactPoint);
            }

            return size;
        }

        /// <summary>将原生接触点数据提取到托管 ContactPoint[] 数组中。</summary>
        internal int ExtractContactsArray(ContactPoint[] managedContainer, bool flipped)
        {
            int size = (int)Math.Min(managedContainer.Length, m_NbPoints);

            for (int i = 0; i < size; ++i)
            {
                ref readonly ContactPairPoint nativePoint = ref GetContactPoint(i);
                var contactPoint = new ContactPoint()
                {
                    m_Point = nativePoint.position,
                    m_Impulse = nativePoint.impulse,
                    m_Separation = nativePoint.separation,
                };

                if (flipped)
                {
                    contactPoint.m_Normal = -nativePoint.normal;
                    contactPoint.m_ThisColliderEntityId = m_OtherColliderID;
                    contactPoint.m_OtherColliderEntityId = m_ColliderID;
                }
                else
                {
                    contactPoint.m_Normal = nativePoint.normal;
                    contactPoint.m_ThisColliderEntityId = m_ColliderID;
                    contactPoint.m_OtherColliderEntityId = m_OtherColliderID;
                }

                managedContainer[i] = contactPoint;
            }
            return size;
        }

        /// <summary>将接触点数据复制到 NativeArray 中（用于 Job System）。</summary>
        public void CopyToNativeArray(NativeArray<ContactPairPoint> buffer)
        {
            int n = Mathf.Min(buffer.Length, contactCount);

            for (int i = 0; i < n; i++)
                buffer[i] = GetContactPoint(i);
        }

        /// <summary>获取指定索引的 ContactPairPoint 引用（只读）。</summary>
        public unsafe ref readonly ContactPairPoint GetContactPoint(int index)
        {
            return ref *GetContactPoint_Internal(index);
        }

        /// <summary>
        /// 获取指定接触点的三角面索引。
        /// 返回碰撞体中命中的三角面索引。两个碰撞器中只有一个的面索引是有效的。
        /// 底层调用 PhysX 的面索引转换函数。
        /// </summary>
        public unsafe uint GetContactPointFaceIndex(int contactIndex)
        {
            var index0 = GetContactPoint_Internal(contactIndex)->m_InternalFaceIndex0;
            var index1 = GetContactPoint_Internal(contactIndex)->m_InternalFaceIndex1;

            // 只有一个索引是有效的
            if (index0 != c_InvalidFaceIndex)
                return Physics.TranslateTriangleIndexFromID(m_ColliderID, index0);

            if (index1 != c_InvalidFaceIndex)
                return Physics.TranslateTriangleIndexFromID(m_OtherColliderID, index1);

            return c_InvalidFaceIndex;
        }

        /// <summary>内部方法：获取指定索引的 ContactPairPoint 指针。</summary>
        internal unsafe ContactPairPoint* GetContactPoint_Internal(int index)
        {
            if (index >= m_NbPoints)
                throw new IndexOutOfRangeException("无效的 ContactPairPoint 索引。索引应在 0 和 ContactPair.ContactCount 之间。");

            return (ContactPairPoint*)(m_StartPtr.ToInt64() + index * sizeof(ContactPairPoint));
        }
    }

    //======================================================================
    //  ContactPairPoint — 单个接触点（基础数据单位）
    //  对应 PhysX 的 PxContactPairPoint
    //  参考：https://github.com/NVIDIAGameWorks/PhysX/blob/4.1/physx/include/PxSimulationEventCallback.h#L463
    //======================================================================
    /// <summary>
    /// 单个接触点数据，包含位置、法线、分离距离、冲量以及内部三角面索引。
    /// 是 PhysX 接触数据的最小单位，多个 ContactPairPoint 组成一个 ContactPair。
    /// 底层对应 PhysX 的 PxContactPairPoint。
    /// </summary>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct ContactPairPoint
    {
        internal readonly Vector3 m_Position;           // 接触点位置（世界空间）
        internal readonly float m_Separation;           // 分离距离（正=间隙，负=穿透深度）
        internal readonly Vector3 m_Normal;             // 接触法线（指向本物体）
        internal readonly uint m_InternalFaceIndex0;    // 碰撞器 0 的内部三角面索引
        internal readonly Vector3 m_Impulse;            // 该接触点的碰撞冲量
        internal readonly uint m_InternalFaceIndex1;    // 碰撞器 1 的内部三角面索引

        public Vector3 position => m_Position;
        public float separation => m_Separation;
        public Vector3 normal => m_Normal;
        public Vector3 impulse => m_Impulse;
    };

    //======================================================================
    //  内部标志位枚举
    //======================================================================

    /// <summary>
    /// 碰撞对头部标志位（ushort）。
    /// 标记物体是否在碰撞后被移除（销毁）。
    /// </summary>
    internal enum CollisionPairHeaderFlags : ushort
    {
        RemovedActor = (1 << 0),           // "本物体"已被移除
        RemovedOtherActor = (1 << 1),      // "对方物体"已被移除
    };

    /// <summary>
    /// 碰撞对标志位（ushort）。
    /// 标记碰撞器的存在状态和内部冲量计算状态。
    /// </summary>
    internal enum CollisionPairFlags : ushort
    {
        RemovedShape = (1 << 0),                 // "本碰撞器"已被移除
        RemovedOtherShape = (1 << 1),            // "对方碰撞器"已被移除
        ActorPairHasFirstTouch = (1 << 2),       // 物体对首次接触
        ActorPairLostTouch = (1 << 3),           // 物体对失去接触
        InternalHasImpulses = (1 << 4),          // 包含冲量数据
        InternalContactsAreFlipped = (1 << 5),   // 接触点法线已翻转
    };

    /// <summary>
    /// 碰撞事件标志位（ushort）。
    /// 由 PhysX 的 PxPairFlag 映射而来，用于控制碰撞回调的行为和事件产生条件。
    ///
    /// 关键标志说明：
    /// - NotifyTouchFound / Persists / Lost：控制 Enter / Stay / Exit 事件
    /// - SolveContacts：让 PhysX 求解此接触对（产生碰撞响应）
    /// - DetectDiscreteContact / DetectCCDContact：检测模式
    /// - ContactDefault / TriggerDefault：预设组合值
    /// </summary>
    internal enum CollisionPairEventFlags : ushort
    {
        SolveContacts = (1 << 0),                     // 求解接触约束（产生碰撞响应）
        ModifyContacts = (1 << 1),                    // 允许修改接触点
        NotifyTouchFound = (1 << 2),                  // 产生 OnCollisionEnter
        NotifyTouchPersists = (1 << 3),               // 产生 OnCollisionStay
        NotifyTouchLost = (1 << 4),                   // 产生 OnCollisionExit
        NotifyTouchCCD = (1 << 5),                    // CCD 接触通知
        NotifyThresholdForceFound = (1 << 6),         // 超过力阈值的接触开始
        NotifyThresholdForcePersists = (1 << 7),      // 超过力阈值的接触持续
        NotifyThresholdForceLost = (1 << 8),          // 超过力阈值的接触结束
        NotifyContactPoint = (1 << 9),                // 接触点通知
        DetectDiscreteContact = (1 << 10),            // 离散接触检测
        DetectCCDContact = (1 << 11),                 // CCD 接触检测
        PreSolverVelocity = (1 << 12),                // 记录求解前速度
        PostSolverVelocity = (1 << 13),               // 记录求解后速度
        ContactEventPose = (1 << 14),                 // 接触事件姿态
        NextFree = (1 << 15),                         // 下一个可用标志位

        /// <summary>默认碰撞接触标志组合：求解约束 + 离散检测。</summary>
        ContactDefault = SolveContacts | DetectDiscreteContact,

        /// <summary>默认触发器标志组合：触发 Enter/Exit + 离散检测。</summary>
        TriggerDefault = NotifyTouchFound | NotifyTouchLost | DetectDiscreteContact,
    };
}
