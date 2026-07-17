// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UnityEngine
{
    //////////////////////////////////////////////////////////////////////////
    //  批量查询命令结构体，用于 Unity Job System 中的并行物理查询        //
    //  每个 Command 结构体对应一种形状的物理查询，通过 ScheduleBatch       //
    //  方法调度到原生 PhysX 批量查询管线。                                //
    //////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 物理查询参数结构体，控制物理查询的行为（层遮罩、触发器处理、背面检测等）。
    /// 用于 RaycastCommand、SpherecastCommand 等批量查询命令的 queryParameters 字段。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct QueryParameters
    {
        /// <summary>要检测的层级遮罩（LayerMask），决定查询哪些层的碰撞器。</summary>
        public int layerMask;

        /// <summary>
        /// 是否命中同一碰撞器的多个三角面。
        /// 启用时，当射线穿过 MeshCollider 的多个面时返回多个命中结果。
        /// 默认为 false（每个碰撞器只返回最近的命中点）。
        /// </summary>
        public bool hitMultipleFaces;

        /// <summary>对触发器（Trigger）碰撞器的处理方式。</summary>
        public QueryTriggerInteraction hitTriggers;

        /// <summary>是否检测三角面的背面。启用时可检测从后方进入的面。</summary>
        public bool hitBackfaces;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="layerMask">层级遮罩，默认使用 Physics.DefaultRaycastLayers（除 IgnoreRaycast 层外的所有层）。</param>
        /// <param name="hitMultipleFaces">是否命中多个三角面。</param>
        /// <param name="hitTriggers">触发器处理方式。</param>
        /// <param name="hitBackfaces">是否检测背面。</param>
        public QueryParameters(int layerMask = Physics.DefaultRaycastLayers, bool hitMultipleFaces = false, QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal, bool hitBackfaces = false)
        {
            this.layerMask = layerMask;
            this.hitMultipleFaces = hitMultipleFaces;
            this.hitTriggers = hitTriggers;
            this.hitBackfaces = hitBackfaces;
        }

        /// <summary>默认查询参数（所有层、不命中多面、使用全局触发器设置、不检测背面）。</summary>
        public static QueryParameters Default => new QueryParameters(Physics.DefaultRaycastLayers, false, QueryTriggerInteraction.UseGlobal, false);
    }

    /// <summary>
    /// 重叠查询（Overlap）的命中结果结构体。
    /// 与 RaycastHit 不同，ColliderHit 只包含被命中的碰撞器 ID，不包含命中点、法线等详细信息。
    /// 用于 OverlapSphere / OverlapBox / OverlapCapsule 的批量查询结果。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ColliderHit
    {
        private EntityId m_ColliderEntityId;  // 被命中碰撞器的实体 ID

        /// <summary>被命中碰撞器的实体 ID。</summary>
        public EntityId entityId => m_ColliderEntityId;

        /// <summary>已废弃，使用 entityId 代替。</summary>
        [System.Obsolete("instanceID is deprecated, use entityId instead.", true)]
        public int instanceID => m_ColliderEntityId;

        /// <summary>被命中的 Collider 引用（注意：这是仅主线程可用的 API）。</summary>
        public Collider collider => Object.FindObjectFromInstanceID(entityId) as Collider;
    }

    //======================================================================
    //  RaycastCommand — 批量射线检测命令
    //======================================================================
    /// <summary>
    /// 批量射线检测命令，用于在 Job System 中并行执行多条射线检测。
    /// 通过 RaycastCommand.ScheduleBatch 调度到原生 PhysX 批量查询管线，
    /// 可大幅减少多射线检测的 CPU 开销。
    ///
    /// 底层调用 PhysX 的 PxBatchQuery::raycast。
    /// 线程安全，可在 IJobParallelFor 中构造。
    /// </summary>
    [NativeHeader("Modules/Physics/BatchCommands/RaycastCommand.h")]
    [NativeHeader("ManagedKernel/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public partial struct RaycastCommand
    {
        /// <summary>
        /// 构造函数：使用默认 PhysicsScene 创建射线检测命令。
        /// </summary>
        /// <param name="from">射线起点（世界空间）。</param>
        /// <param name="direction">射线方向向量（世界空间，无需归一化）。</param>
        /// <param name="queryParameters">查询参数（层遮罩、触发器处理等）。</param>
        /// <param name="distance">最大检测距离（默认无限远）。</param>
        public RaycastCommand(Vector3 from, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.from = from;
            this.direction = direction;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.distance = distance;
            this.queryParameters = queryParameters;
        }

        /// <summary>
        /// 构造函数：指定 PhysicsScene 创建射线检测命令。
        /// </summary>
        /// <param name="physicsScene">要查询的物理场景。</param>
        /// <param name="from">射线起点（世界空间）。</param>
        /// <param name="direction">射线方向向量。</param>
        /// <param name="queryParameters">查询参数。</param>
        /// <param name="distance">最大检测距离（默认无限远）。</param>
        public RaycastCommand(PhysicsScene physicsScene, Vector3 from, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.from = from;
            this.direction = direction;
            this.physicsScene = physicsScene;
            this.distance = distance;
            this.queryParameters = queryParameters;
        }

        public Vector3 from { get; set; }                     // 射线起点
        public Vector3 direction { get; set; }                // 射线方向
        public PhysicsScene physicsScene { get; set; }        // 目标物理场景
        public float distance { get; set; }                   // 最大检测距离
        public QueryParameters queryParameters;               // 查询参数（字段，非属性）

        /// <summary>
        /// 调度批量射线检测 Job。
        /// 将多条射线命令打包成并行 Job，在 Worker Thread 上同时执行。
        /// </summary>
        /// <param name="commands">射线命令数组（NativeArray）。</param>
        /// <param name="results">结果数组，大小至少为 maxHits × commands.Length。</param>
        /// <param name="minCommandsPerJob">每个 Job 的最小命令数，用于负载均衡。</param>
        /// <param name="maxHits">每条命令的最大命中数。</param>
        /// <param name="dependsOn">依赖的 JobHandle。</param>
        /// <returns>调度后的 JobHandle。</returns>
        public unsafe static JobHandle ScheduleBatch(NativeArray<RaycastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits 应大于 0。");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("提供的结果缓冲区太小，每个命令至少需要 maxHits 个结果位置。");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<RaycastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<RaycastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleRaycastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        /// <summary>
        /// 调度批量射线检测 Job（每个命令只取最近一个命中结果的重载版本）。
        /// </summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<RaycastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            return ScheduleBatch(commands, results, minCommandsPerJob, 1, dependsOn);
        }

        [FreeFunction("ScheduleRaycastCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleRaycastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    //======================================================================
    //  SpherecastCommand — 批量球体投射命令
    //======================================================================
    /// <summary>
    /// 批量球体投射命令。沿给定方向推动球体，检测所有碰撞。
    /// 底层调用 PhysX 的 PxBatchQuery::spherecast。
    /// </summary>
    [NativeHeader("Modules/Physics/BatchCommands/SpherecastCommand.h")]
    [NativeHeader("ManagedKernel/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public partial struct SpherecastCommand
    {
        /// <summary>构造函数：使用默认 PhysicsScene。</summary>
        public SpherecastCommand(Vector3 origin, float radius, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.origin = origin;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = queryParameters;
        }

        /// <summary>构造函数：指定 PhysicsScene。</summary>
        public SpherecastCommand(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.origin = origin;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = physicsScene;
            this.queryParameters = queryParameters;
        }

        public Vector3 origin { get; set; }                   // 球体中心起点
        public float radius { get; set; }                     // 球体半径
        public Vector3 direction { get; set; }                // 投射方向
        public float distance { get; set; }                   // 最大投射距离
        public PhysicsScene physicsScene { get; set; }        // 目标物理场景
        public QueryParameters queryParameters;               // 查询参数

        /// <summary>调度批量球体投射 Job。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<SpherecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits 应大于 0。");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("提供的结果缓冲区太小，每个命令至少需要 maxHits 个结果位置。");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<SpherecastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<SpherecastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleSpherecastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        /// <summary>调度批量球体投射 Job（每个命令只取最近一个命中的重载）。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<SpherecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            return ScheduleBatch(commands, results, minCommandsPerJob, 1, dependsOn);
        }

        [FreeFunction("ScheduleSpherecastCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleSpherecastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    //======================================================================
    //  CapsulecastCommand — 批量胶囊体投射命令
    //======================================================================
    /// <summary>
    /// 批量胶囊体投射命令。沿给定方向推动胶囊体，检测所有碰撞。
    /// 胶囊体由 point1（底部）和 point2（顶部）两个端点及半径定义。
    /// 底层调用 PhysX 的 PxBatchQuery::sweep（使用胶囊体形状）。
    /// </summary>
    [NativeHeader("Modules/Physics/BatchCommands/CapsulecastCommand.h")]
    [NativeHeader("ManagedKernel/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public partial struct CapsulecastCommand
    {
        /// <summary>构造函数：使用默认 PhysicsScene。</summary>
        public CapsulecastCommand(Vector3 p1, Vector3 p2, float radius, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.point1 = p1;
            this.point2 = p2;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = queryParameters;
        }

        /// <summary>构造函数：指定 PhysicsScene。</summary>
        public CapsulecastCommand(PhysicsScene physicsScene, Vector3 p1, Vector3 p2, float radius, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.point1 = p1;
            this.point2 = p2;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = physicsScene;
            this.queryParameters = queryParameters;
        }

        public Vector3 point1 { get; set; }                   // 胶囊体底部端点（世界空间）
        public Vector3 point2 { get; set; }                   // 胶囊体顶部端点（世界空间）
        public float radius { get; set; }                     // 胶囊体半径
        public Vector3 direction { get; set; }                // 投射方向
        public float distance { get; set; }                   // 最大投射距离
        public PhysicsScene physicsScene { get; set; }        // 目标物理场景
        public QueryParameters queryParameters;               // 查询参数

        /// <summary>调度批量胶囊体投射 Job。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<CapsulecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits 应大于 0。");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("提供的结果缓冲区太小，每个命令至少需要 maxHits 个结果位置。");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<CapsulecastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<CapsulecastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleCapsulecastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        /// <summary>调度批量胶囊体投射 Job（每个命令只取最近一个命中的重载）。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<CapsulecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            return ScheduleBatch(commands, results, minCommandsPerJob, 1, dependsOn);
        }

        [FreeFunction("ScheduleCapsulecastCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleCapsulecastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    //======================================================================
    //  BoxcastCommand — 批量盒体投射命令
    //======================================================================
    /// <summary>
    /// 批量盒体投射命令。沿给定方向推动盒体，检测所有碰撞。
    /// 盒体由中心、半长宽高和朝向定义。
    /// 底层调用 PhysX 的 PxBatchQuery::sweep（使用盒体形状）。
    /// </summary>
    [NativeHeader("Modules/Physics/BatchCommands/BoxcastCommand.h")]
    [NativeHeader("ManagedKernel/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public partial struct BoxcastCommand
    {
        /// <summary>构造函数：使用默认 PhysicsScene。</summary>
        public BoxcastCommand(Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.direction = direction;
            this.distance = distance;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = queryParameters;
        }

        /// <summary>构造函数：指定 PhysicsScene。</summary>
        public BoxcastCommand(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.direction = direction;
            this.distance = distance;
            this.physicsScene = physicsScene;
            this.queryParameters = queryParameters;
        }

        public Vector3 center { get; set; }                   // 盒体中心（世界空间）
        public Vector3 halfExtents { get; set; }              // 盒体半长宽高（half-extents）
        public Quaternion orientation { get; set; }           // 盒体朝向
        public Vector3 direction { get; set; }                // 投射方向
        public float distance { get; set; }                   // 最大投射距离
        public PhysicsScene physicsScene { get; set; }        // 目标物理场景
        public QueryParameters queryParameters;               // 查询参数

        /// <summary>调度批量盒体投射 Job。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<BoxcastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits 应大于 0。");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("提供的结果缓冲区太小，每个命令至少需要 maxHits 个结果位置。");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<BoxcastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<BoxcastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleBoxcastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        /// <summary>调度批量盒体投射 Job（每个命令只取最近一个命中的重载）。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<BoxcastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            return ScheduleBatch(commands, results, minCommandsPerJob, 1, dependsOn);
        }

        [FreeFunction("ScheduleBoxcastCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleBoxcastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    //======================================================================
    //  ClosestPointCommand — 批量最近点查询命令
    //======================================================================
    /// <summary>
    /// 批量最近点查询命令。给定一个空间点和碰撞器，计算碰撞器上距离该点最近的点。
    /// 底层调用 PhysX 的 PxGeometryQuery::computePointDistance。
    /// </summary>
    [NativeHeader("Modules/Physics/BatchCommands/ClosestPointCommand.h")]
    [NativeHeader("ManagedKernel/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public struct ClosestPointCommand
    {
        /// <summary>已废弃的构造函数（使用 instanceID）。</summary>
        [System.Obsolete("ClosestPointCommand(Vector3, int, Vector3, Quaternion, Vector3) is obsolete. Use ClosestPointCommand(Vector3, EntityId, Vector3, Quaternion, Vector3) instead.", true)]
        public ClosestPointCommand(Vector3 point, int colliderInstanceID, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.point = point;
            this.colliderEntityId = colliderInstanceID;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        /// <summary>构造函数：通过 Collider 引用创建。</summary>
        public ClosestPointCommand(Vector3 point, Collider collider, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.point = point;
            this.colliderEntityId = collider.GetEntityId();
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        /// <summary>构造函数：通过 EntityId 创建。</summary>
        public ClosestPointCommand(Vector3 point, EntityId colliderEntityId, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.point = point;
            this.colliderEntityId = colliderEntityId;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public Vector3 point { get; set; }                        // 空间查询点
        public EntityId colliderEntityId { get; set; }            // 目标碰撞器实体 ID
        [System.Obsolete("colliderInstanceID is deprecated, use colliderEntityId instead.", true)]
        public int colliderInstanceID
        {
            get { return colliderEntityId; }
            set { colliderEntityId = value; }
        }
        public Vector3 position { get; set; }                     // 碰撞器的世界位置
        public Quaternion rotation { get; set; }                  // 碰撞器的世界旋转
        public Vector3 scale { get; set; }                        // 碰撞器的世界缩放

        /// <summary>调度批量最近点查询 Job。结果数组为 Vector3（最近点坐标）。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<ClosestPointCommand> commands, NativeArray<Vector3> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            var jobData = new BatchQueryJob<ClosestPointCommand, Vector3>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<ClosestPointCommand, Vector3>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleClosestPointCommandBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob);
        }

        [FreeFunction("ScheduleClosestPointCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleClosestPointCommandBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob);
    }

    //======================================================================
    //  OverlapSphereCommand — 批量球体重叠检测命令
    //======================================================================
    /// <summary>
    /// 批量球体重叠检测命令。检测与指定球体相交的所有碰撞器。
    /// 结果为 ColliderHit 结构体（只包含碰撞器 ID）。
    /// 底层调用 PhysX 的 PxBatchQuery::overlap（使用球体形状）。
    /// </summary>
    [NativeHeader("Modules/Physics/BatchCommands/OverlapSphereCommand.h")]
    public struct OverlapSphereCommand
    {
        /// <summary>构造函数：使用默认 PhysicsScene。</summary>
        public OverlapSphereCommand(Vector3 point, float radius, QueryParameters queryParameters)
        {
            this.point = point;
            this.radius = radius;
            this.queryParameters = queryParameters;
            this.physicsScene = Physics.defaultPhysicsScene;
        }

        /// <summary>构造函数：指定 PhysicsScene。</summary>
        public OverlapSphereCommand(PhysicsScene physicsScene, Vector3 point, float radius, QueryParameters queryParameters)
        {
            this.physicsScene = physicsScene;
            this.point = point;
            this.radius = radius;
            this.queryParameters = queryParameters;
        }

        public Vector3 point { get; set; }                   // 球心位置
        public float radius { get; set; }                    // 球体半径
        public PhysicsScene physicsScene { get; set; }       // 目标物理场景
        public QueryParameters queryParameters;              // 查询参数

        /// <summary>调度批量球体重叠检测 Job。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<OverlapSphereCommand> commands, NativeArray<ColliderHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits 应大于 0。");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("提供的结果缓冲区太小，每个命令至少需要 maxHits 个结果位置。");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<OverlapSphereCommand, ColliderHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<OverlapSphereCommand, ColliderHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleOverlapSphereBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        [FreeFunction("ScheduleOverlapSphereCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleOverlapSphereBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    //======================================================================
    //  OverlapBoxCommand — 批量盒体重叠检测命令
    //======================================================================
    /// <summary>
    /// 批量盒体重叠检测命令。检测与指定盒体相交的所有碰撞器。
    /// 底层调用 PhysX 的 PxBatchQuery::overlap（使用盒体形状）。
    /// </summary>
    [NativeHeader("Modules/Physics/BatchCommands/OverlapBoxCommand.h")]
    public struct OverlapBoxCommand
    {
        /// <summary>构造函数：使用默认 PhysicsScene。</summary>
        public OverlapBoxCommand(Vector3 center, Vector3 halfExtents, Quaternion orientation, QueryParameters queryParameters)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.queryParameters = queryParameters;
            this.physicsScene = Physics.defaultPhysicsScene;
        }

        /// <summary>构造函数：指定 PhysicsScene。</summary>
        public OverlapBoxCommand(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, QueryParameters queryParameters)
        {
            this.physicsScene = physicsScene;
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.queryParameters = queryParameters;
        }

        public Vector3 center { get; set; }                   // 盒体中心
        public Vector3 halfExtents { get; set; }              // 半长宽高
        public Quaternion orientation { get; set; }           // 盒体朝向
        public PhysicsScene physicsScene { get; set; }        // 目标物理场景
        public QueryParameters queryParameters;               // 查询参数

        /// <summary>调度批量盒体重叠检测 Job。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<OverlapBoxCommand> commands, NativeArray<ColliderHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits 应大于 0。");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("提供的结果缓冲区太小，每个命令至少需要 maxHits 个结果位置。");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<OverlapBoxCommand, ColliderHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<OverlapBoxCommand, ColliderHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleOverlapBoxBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        [FreeFunction("ScheduleOverlapBoxCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleOverlapBoxBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    //======================================================================
    //  OverlapCapsuleCommand — 批量胶囊体重叠检测命令
    //======================================================================
    /// <summary>
    /// 批量胶囊体重叠检测命令。检测与指定胶囊体相交的所有碰撞器。
    /// 胶囊体由两个端点（point0, point1）和半径定义。
    /// 底层调用 PhysX 的 PxBatchQuery::overlap（使用胶囊体形状）。
    /// </summary>
    [NativeHeader("Modules/Physics/BatchCommands/OverlapCapsuleCommand.h")]
    public struct OverlapCapsuleCommand
    {
        /// <summary>构造函数：使用默认 PhysicsScene。</summary>
        public OverlapCapsuleCommand(Vector3 point0, Vector3 point1, float radius, QueryParameters queryParameters)
        {
            this.point0 = point0;
            this.point1 = point1;
            this.radius = radius;
            this.queryParameters = queryParameters;
            this.physicsScene = Physics.defaultPhysicsScene;
        }

        /// <summary>构造函数：指定 PhysicsScene。</summary>
        public OverlapCapsuleCommand(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, QueryParameters queryParameters)
        {
            this.physicsScene = physicsScene;
            this.point0 = point0;
            this.point1 = point1;
            this.radius = radius;
            this.queryParameters = queryParameters;
        }

        public Vector3 point0 { get; set; }                   // 胶囊体底部端点
        public Vector3 point1 { get; set; }                   // 胶囊体顶部端点
        public float radius { get; set; }                     // 胶囊体半径
        public PhysicsScene physicsScene { get; set; }        // 目标物理场景
        public QueryParameters queryParameters;               // 查询参数

        /// <summary>调度批量胶囊体重叠检测 Job。</summary>
        public unsafe static JobHandle ScheduleBatch(NativeArray<OverlapCapsuleCommand> commands, NativeArray<ColliderHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits 应大于 0。");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("提供的结果缓冲区太小，每个命令至少需要 maxHits 个结果位置。");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<OverlapCapsuleCommand, ColliderHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<OverlapCapsuleCommand, ColliderHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleOverlapCapsuleBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        [FreeFunction("ScheduleOverlapCapsuleCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleOverlapCapsuleBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }
}
