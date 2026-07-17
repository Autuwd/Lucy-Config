// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine
{
    /// <summary>
    /// Physics 类的部分定义，包含接触修改（Contact Modification）事件。
    ///
    /// 接触修改允许用户在 PhysX 约束求解器处理接触之前，动态修改接触点的属性。
    /// 这是实现自定义物理材质行为、摩擦力修改、弹力调整、接触忽略等功能的基础。
    ///
    /// 流程：
    /// 1. C++ 侧在求解器前调用 OnSceneContactModify
    /// 2. 将原生缓冲区转换为托管 NativeArray&lt;ModifiableContactPair&gt;
    /// 3. 分发 ContactModifyEvent（常规）或 ContactModifyEventCCD（CCD）
    /// 4. 用户在回调中通过 ModifiableContactPair 的 Set/Get 方法修改接触属性
    /// 5. 修改后的数据被 PhysX 求解器使用
    /// </summary>
    public partial class Physics
    {
        /// <summary>常规接触修改事件，在每次物理模拟的接触求解前触发。</summary>
        public static event Action<PhysicsScene, NativeArray<ModifiableContactPair>> ContactModifyEvent;

        /// <summary>CCD 接触修改事件，在 CCD 接触求解前触发。</summary>
        public static event Action<PhysicsScene, NativeArray<ModifiableContactPair>> ContactModifyEventCCD;

        /// <summary>
        /// 内部通用接触修改事件，默认指向 PhysX 实现。
        /// 允许通过替换此委托来注入自定义接触修改逻辑（用于测试或自定义物理引擎）。
        /// </summary>
        internal static event Action<PhysicsScene, IntPtr, int, bool> GenericContactModifyEvent = PhysXOnSceneContactModify;

        /// <summary>
        /// [由 C++ 侧调用] 物理场景接触修改入口。
        /// 将原生缓冲区转换为托管数组后分发到 GenericContactModifyEvent。
        /// </summary>
        [RequiredByNativeCode]
        private static unsafe void OnSceneContactModify(PhysicsScene scene, IntPtr buffer, int count, bool isCCD)
        {
            GenericContactModifyEvent?.Invoke(scene, buffer, count, isCCD);
        }

        /// <summary>
        /// PhysX 接触修改实现。
        /// 将原生 ModifiableContactPair 缓冲区包装为托管 NativeArray，
        /// 然后触发 ContactModifyEvent（常规）或 ContactModifyEventCCD（CCD）。
        /// </summary>
        private static unsafe void PhysXOnSceneContactModify(PhysicsScene scene, IntPtr buffer, int count, bool isCCD)
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ModifiableContactPair>(buffer.ToPointer(), count, Allocator.None);

            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);

            if (!isCCD)
                ContactModifyEvent?.Invoke(scene, array);
            else
                ContactModifyEventCCD?.Invoke(scene, array);

            AtomicSafetyHandle.Release(safety);
        }
    }

    //======================================================================
    //  ModifiableContactPair — 可修改的接触对
    //  提供读取/修改接触属性的 API，在 ContactModifyEvent 中提供给用户
    //======================================================================
    /// <summary>
    /// 可修改的接触对结构体，在 ContactModifyEvent 回调中提供给开发者。
    /// 允许在 PhysX 约束求解器处理之前，动态修改接触点的各种物理属性，
    /// 包括位置、法线、摩擦力、弹性、目标速度、冲量限制等。
    ///
    /// 数据布局：
    /// - 两个 PhysX 对象指针（actor, shape）
    /// - 两个物体的变换（position, rotation）
    /// - 接触点数据指针（contacts + numContacts）
    /// - 接触块（ContactPatch）元数据：位于 contacts 指针之前
    ///
    /// 内存布局为 [ModifiableContactPatch × numContacts] [ModifiableContact × numContacts]
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Physics/PhysicsCollisionGeometry.h")]
    [NativeHeader("Modules/Physics/PhysXContactModification.h")]
    public struct ModifiableContactPair
    {
        //==================== 底层指针 ====================//
        private IntPtr actor;               // "本物体"的 PhysX PxActor 指针
        private IntPtr otherActor;          // "对方物体"的 PhysX PxActor 指针
        private IntPtr shape;               // "本碰撞器"的 PhysX PxShape 指针
        private IntPtr otherShape;          // "对方碰撞器"的 PhysX PxShape 指针

        //==================== 变换信息 ====================//
        public Quaternion rotation;         // "本物体"的旋转
        public Vector3 position;            // "本物体"的位置
        public Quaternion otherRotation;    // "对方物体"的旋转
        public Vector3 otherPosition;       // "对方物体"的位置

        //==================== 接触点数据 ====================//
        private int numContacts;            // 接触点数量
        private IntPtr contacts;            // 指向接触点数据的指针（布局：Patch + Contact 数据块）

        //==================== 内部原生方法 ====================//
        [FreeFunction("Physics::PhysXGeometryExtension::TranslateTriangleIndex", true)]
        extern internal static uint TranslateTriangleIndex(IntPtr shapePtr, uint rawIndex);

        [FreeFunction("Physics::PhysXContactModificationExtension::ResolveShapeToEntityId", true)]
        extern internal static EntityId ResolveShapeToEntityId(IntPtr shapePtr);

        [FreeFunction("Physics::PhysXContactModificationExtension::ResolveActorToEntityId", true)]
        extern internal static EntityId ResolveActorToEntityId(IntPtr actorPtr);

        [FreeFunction("Physics::PhysXContactModificationExtension::GetActorLinearVelocity", true)]
        extern internal static Vector3 GetActorLinearVelocity(IntPtr actorPtr);

        [FreeFunction("Physics::PhysXContactModificationExtension::GetActorAngularVelocity", true)]
        extern internal static Vector3 GetActorAngularVelocity(IntPtr actorPtr);

        //==================== 公开属性 ====================//
        public EntityId colliderEntityId => ResolveShapeToEntityId(shape);
        [System.Obsolete("colliderInstanceID is deprecated, use colliderEntityId instead.", true)]
        public int colliderInstanceID => ResolveShapeToEntityId(shape);

        public EntityId otherColliderEntityId => ResolveShapeToEntityId(otherShape);
        [System.Obsolete("otherColliderInstanceID is deprecated, use otherColliderEntityId instead.", true)]
        public int otherColliderInstanceID => ResolveShapeToEntityId(otherShape);

        public EntityId bodyEntityId => ResolveActorToEntityId(actor);
        [System.Obsolete("bodyInstanceID is deprecated, use bodyEntityId instead.", true)]
        public int bodyInstanceID => ResolveActorToEntityId(actor);

        public EntityId otherBodyEntityId => ResolveActorToEntityId(otherActor);
        [System.Obsolete("otherBodyInstanceID is deprecated, use otherBodyEntityId instead.", true)]
        public int otherBodyInstanceID => ResolveActorToEntityId(otherActor);

        public Vector3 bodyVelocity => GetActorLinearVelocity(actor);
        public Vector3 bodyAngularVelocity => GetActorAngularVelocity(actor);
        public Vector3 otherBodyVelocity => GetActorLinearVelocity(otherActor);
        public Vector3 otherBodyAngularVelocity => GetActorAngularVelocity(otherActor);

        /// <summary>此接触对中包含的可修改接触点数量。</summary>
        public int contactCount => numContacts;

        //==================== 质量属性 ====================//
        /// <summary>
        /// 获取或设置接触块的质量属性（质量缩放、惯性缩放）。
        /// 设置时会自动标记 HasModifiedMassRatios 标志。
        /// </summary>
        public unsafe ModifiableMassProperties massProperties
        {
            get { return GetContactPatch()->massProperties; }
            set
            {
                var contactPatch = GetContactPatch();
                contactPatch->massProperties = value;
                contactPatch->internalFlags |= (byte)ModifiableContactPatch.Flags.HasModifiedMassRatios;
            }
        }

        //==================== 接触点读写方法 ====================//

        /// <summary>获取指定接触点的位置。</summary>
        public unsafe Vector3 GetPoint(int i) { return GetContact(i)->contact; }

        /// <summary>设置指定接触点的位置。</summary>
        public unsafe void SetPoint(int i, Vector3 v) { GetContact(i)->contact = v; }

        /// <summary>获取指定接触点的法线。</summary>
        public unsafe Vector3 GetNormal(int i) { return GetContact(i)->normal; }

        /// <summary>
        /// 设置指定接触点的法线。
        /// 法线修改会自动标记 RegeneratePatches 标志，触发 PhysX 重新生成接触块。
        /// </summary>
        public unsafe void SetNormal(int i, Vector3 normal)
        {
            GetContact(i)->normal = normal;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.RegeneratePatches;
        }

        /// <summary>获取指定接触点的分离距离。</summary>
        public unsafe float GetSeparation(int i) { return GetContact(i)->separation; }

        /// <summary>设置指定接触点的分离距离。</summary>
        public unsafe void SetSeparation(int i, float separation) { GetContact(i)->separation = separation; }

        /// <summary>获取指定接触点的目标速度（用于马达效果）。</summary>
        public unsafe Vector3 GetTargetVelocity(int i) { return GetContact(i)->targetVelocity; }

        /// <summary>
        /// 设置指定接触点的目标速度。
        /// 自动标记 HasTargetVelocity 标志。
        /// </summary>
        public unsafe void SetTargetVelocity(int i, Vector3 velocity)
        {
            GetContact(i)->targetVelocity = velocity;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.HasTargetVelocity;
        }

        /// <summary>获取指定接触点的弹性系数。</summary>
        public unsafe float GetBounciness(int i) { return GetContact(i)->restitution; }

        /// <summary>
        /// 设置指定接触点的弹性系数。
        /// 自动标记 RegeneratePatches 标志，触发 PhysX 重新生成接触块。
        /// </summary>
        public unsafe void SetBounciness(int i, float bounciness)
        {
            GetContact(i)->restitution = bounciness;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.RegeneratePatches;
        }

        /// <summary>获取指定接触点的静摩擦系数。</summary>
        public unsafe float GetStaticFriction(int i) { return GetContact(i)->staticFriction; }

        /// <summary>
        /// 设置指定接触点的静摩擦系数。
        /// 自动标记 RegeneratePatches 标志。
        /// </summary>
        public unsafe void SetStaticFriction(int i, float staticFriction)
        {
            GetContact(i)->staticFriction = staticFriction;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.RegeneratePatches;
        }

        /// <summary>获取指定接触点的动摩擦系数。</summary>
        public unsafe float GetDynamicFriction(int i) { return GetContact(i)->dynamicFriction; }

        /// <summary>
        /// 设置指定接触点的动摩擦系数。
        /// 自动标记 RegeneratePatches 标志。
        /// </summary>
        public unsafe void SetDynamicFriction(int i, float dynamicFriction)
        {
            GetContact(i)->dynamicFriction = dynamicFriction;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.RegeneratePatches;
        }

        /// <summary>获取指定接触点的最大冲量限制。</summary>
        public unsafe float GetMaxImpulse(int i) { return GetContact(i)->maxImpulse; }

        /// <summary>
        /// 设置指定接触点的最大冲量限制。
        /// 自动标记 HasMaxImpulse 标志。
        /// </summary>
        public unsafe void SetMaxImpulse(int i, float value)
        {
            GetContact(i)->maxImpulse = value;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.HasMaxImpulse;
        }

        /// <summary>
        /// 忽略指定接触点（将冲量限制设为 0，使该接触点不产生碰撞响应）。
        /// </summary>
        public void IgnoreContact(int i) { SetMaxImpulse(i, 0); }

        /// <summary>
        /// 获取指定接触点的三角面索引。
        /// 需要接触块包含 HasFaceIndices 标志位信息。
        /// 面索引数据存储在接触块数据的末尾。
        /// </summary>
        public unsafe uint GetFaceIndex(int i)
        {
            if ((GetContactPatch()->internalFlags & (byte)ModifiableContactPatch.Flags.HasFaceIndices) != 0)
            {
                // 面索引存储在接触块数据的末尾（具体布局参见 PhysX PxContactModifyCallback.h:150）
                var item = new IntPtr(contacts.ToInt64() + numContacts * sizeof(ModifiableContact) + (numContacts + i) * sizeof(int));
                uint rawIndex = *(uint*)item;

                return TranslateTriangleIndex(otherShape, rawIndex);
            }

            return 0xffffFFFF;  // 无效面索引
        }

        /// <summary>内部方法：获取指定索引的 ModifiableContact 指针。</summary>
        private unsafe ModifiableContact* GetContact(int index)
        {
            var item = new IntPtr(contacts.ToInt64() + index * sizeof(ModifiableContact));
            return (ModifiableContact*)item;
        }

        /// <summary>
        /// 内部方法：获取 ModifiableContactPatch 指针。
        /// ContactPatch 存储在 contacts 指针之前（负偏移）。
        /// 布局：[-Patch数据-] --- [0...numContacts 个 Contact 数据]
        /// </summary>
        private unsafe ModifiableContactPatch* GetContactPatch()
        {
            var item = new IntPtr(contacts.ToInt64() - numContacts * sizeof(ModifiableContactPatch));
            return (ModifiableContactPatch*)item;
        }
    }

    //======================================================================
    //  ModifiableMassProperties — 可修改的质量属性
    //======================================================================
    /// <summary>
    /// 可修改的质量属性结构体，用于接触修改中调整两个物体的有效质量。
    /// 通过缩放逆质量和逆惯性来改变力的分配比例。
    /// 当两个物体的质量差异很大时，可以通过调整此值获得更真实的物理效果。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ModifiableMassProperties
    {
        public float inverseMassScale;          // "本物体"的逆质量缩放
        public float inverseInertiaScale;       // "本物体"的逆惯性缩放
        public float otherInverseMassScale;     // "对方物体"的逆质量缩放
        public float otherInverseInertiaScale;  // "对方物体"的逆惯性缩放
    }

    //======================================================================
    //  内部数据结构（与 PhysX 库的 PxContactPoint / PxContactPatch 对应）
    //======================================================================

    /// <summary>
    /// 可修改的单个接触点数据（内部结构体）。
    /// 包含位置、分离距离、目标速度、最大冲量、法线、摩擦力、弹性等完整接触属性。
    /// 对应 PhysX 的 PxContactPoint 扩展版本。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ModifiableContact
    {
        public Vector3 contact;               // 接触点位置
        public float separation;              // 分离距离
        public Vector3 targetVelocity;        // 目标速度（接触马达用）
        public float maxImpulse;              // 最大冲量限制
        public Vector3 normal;                // 接触法线
        public float restitution;             // 弹性系数
        public uint materialFlags;            // 材质标志位
        public ushort materialIndex;          // 材质索引
        public ushort otherMaterialIndex;     // 对方材质索引
        public float staticFriction;          // 静摩擦系数
        public float dynamicFriction;         // 动摩擦系数
    }

    /// <summary>
    /// 可修改的接触块（Contact Patch）元数据（内部结构体）。
    ///
    /// 一个接触块包含一组接触点（同一法线方向上的多个接触点），
    /// 以及这些接触点共享的物理属性（法线、摩擦力、弹性等）。
    ///
    /// Flags 枚举值需与 PhysX 源码中的 PxContactPatchFlags 保持一致：
    /// External/PhysX/builds/Include/PxContact.h
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ModifiableContactPatch
    {
        /// <summary>
        /// 接触块标志位，与 PhysX 的 PxContactPatchFlag 对应。
        /// </summary>
        public enum Flags
        {
            HasFaceIndices = 1,          // 接触块包含三角面索引信息
            HasModifiedMassRatios = 8,   // 已修改质量比例
            HasTargetVelocity = 16,      // 已设置目标速度
            HasMaxImpulse = 32,          // 已设置最大冲量
            RegeneratePatches = 64,      // 需要重新生成接触块（标记修改了法线/摩擦/弹性）
        };

        public ModifiableMassProperties massProperties;  // 本接触块的质量属性

        public Vector3 normal;          // 接触块法线方向
        public float restitution;       // 接触块弹性系数

        public float dynamicFriction;   // 动摩擦系数
        public float staticFriction;    // 静摩擦系数
        public byte startContactIndex;  // 本块在接触数组中的起始索引
        public byte contactCount;       // 本块包含的接触点数量

        public byte materialFlags;      // 材质标志位
        public byte internalFlags;      // 内部标志位
        public ushort materialIndex;    // 材质索引
        public ushort otherMaterialIndex;  // 对方材质索引
    }
}
