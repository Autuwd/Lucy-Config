// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 Physics — Unity 3D 物理系统核心 API
// ================================================================
//
// 📌 概述：
//   Physics 是 Unity 物理系统的核心入口类，所有物理查询、碰撞检测、
//   仿真控制的静态方法都定义在此。
//
// 🏗 核心功能：
//   - 射线/形状投射检测（Raycast, SphereCast, BoxCast, CapsuleCast）
//   - 重叠检测（OverlapSphere, OverlapBox, OverlapCapsule）
//   - 碰撞忽略与层管理（IgnoreCollision, IgnoreLayerCollision）
//   - 穿透计算（ComputePenetration, ClosestPoint）
//   - 全局物理参数（重力、迭代次数、接触偏移等）
//   - 手动物理仿真（Simulate）
//
// 💡 底层原理：
//   基于 NVIDIA PhysX 物理引擎，所有公开方法委托给 PhysicsScene 结构体，
//   支持多物理场景（Multi-Scene Physics）。
//
// 📍 对应 C++ 头文件：
//   Modules/Physics/PhysicsQuery.h, Modules/Physics/PhysicsManager.h
// ================================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Internal;

namespace UnityEngine
{
    // ================================================================
    // 🎯 QueryTriggerInteraction — 射线触发交互模式枚举
    // ================================================================
    // 控制 Physics 查询（如 Raycast、SphereCast 等）如何处理带有 Trigger 碰撞器的对象。
    // ================================================================
    public enum QueryTriggerInteraction
    {
        UseGlobal = 0,  // 使用 Physics.queriesHitTriggers 全局设置
        Ignore = 1,     // 忽略 Trigger 碰撞器，仅检测普通碰撞器
        Collide = 2     // 与 Trigger 碰撞器发生交互（穿透检测）
    }

    // ================================================================
    // 🎯 SimulationMode — 物理仿真模式枚举
    // ================================================================
    // 控制何时运行 PhysX 物理引擎的仿真步进。
    // ================================================================
    public enum SimulationMode
    {
        FixedUpdate = 0,  // 在 FixedUpdate 中自动运行物理仿真（默认模式）
        Update = 1,       // 在 Update 中自动运行物理仿真（不推荐，物理步长不再固定）
        Script = 2        // 手动调用 Physics.Simulate() 控制仿真时机
    }

    // ================================================================
    // 🎯 SimulationStage — 物理仿真阶段枚举（位掩码）
    // ================================================================
    // 允许对物理仿真进行细分阶段控制，用于 PhysicsScene.RunSimulationStages()。
    // ================================================================
    public enum SimulationStage : ushort
    {
        None = 0,                                    // 无仿真阶段
        PrepareSimulation = 1 << 0,                  // 准备阶段：同步 Transform、更新宽相位碰撞检测
        RunSimulation = 1 << 1,                      // 运行阶段：执行 PhysX 求解器（碰撞求解、约束求解）
        PublishSimulationResults = 1 << 2,            // 发布阶段：触发碰撞回调、更新触发器状态
        All = PrepareSimulation | RunSimulation | PublishSimulationResults  // 所有阶段
    }

    // ================================================================
    // 🎯 SimulationOption — 物理仿真选项枚举（位掩码）
    // ================================================================
    // 控制 PhysicsScene.RunSimulationStages() 的行为选项。
    // ================================================================
    public enum SimulationOption : ushort
    {
        None = 0,                   // 无额外选项
        SyncTransforms = 1 << 0,    // 仿真前自动同步 Transform（将 Unity Transform 写回 PhysX）
        IgnoreEmptyScenes = 1 << 1,  // 跳过空场景的仿真
        All = SyncTransforms | IgnoreEmptyScenes  // 所有选项
    }

    // ================================================================
    // 🎯 JointLimitRange — 关节限制范围结构体（内部使用）
    // ================================================================
    // 定义单个关节的最小/最大角度或距离限制。
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    internal struct JointLimitRange
    {
        public float min;   // 限制最小值
        public float max;   // 限制最大值
    }

    // ================================================================
    // 🎯 IntegrationLimits — 物理引擎集成限制结构体（内部使用）
    // ================================================================
    // 存储每种关节类型的限制范围，用于 PhysX 集成层校验。
    // JointLimitRange 每个占 2 个 float，固定数组有 JointTypeCount * 2 个元素。
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IntegrationLimits
    {
        public const int JointTypeCount = 7;  // 对应 C++ 端 JointType::Count

        // 每个 JointLimitRange 由 2 个 float 组成，因 C# 固定大小数组不支持结构体数组
        fixed float m_Joints[JointTypeCount * 2];

        // 获取指定关节类型的限制范围
        public JointLimitRange GetJointLimit(ArticulationJointType jointType)
        {
            int index = (int)jointType * 2;
            if (index < 0 || index >= JointTypeCount * 2 - 1)
                return default;

            return new JointLimitRange { min = m_Joints[index], max = m_Joints[index + 1] };
        }
    }

    // ================================================================
    // 🎯 IntegrationInfo — 物理引擎集成信息结构体
    // ================================================================
    // 提供当前使用的物理引擎（PhysX）的版本、功能和限制信息。
    // 可通过 Physics.GetCurrentIntegrationInfo() 获取。
    // ================================================================
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct IntegrationInfo
    {
        // Unity 支持的物理引擎特性标记（内部使用）
        [Flags]
        internal enum SupportedUnityFeatures
        {
            None = 0,
            DynamicsSupport = 1 << 1,              // 刚体动力学支持
            SDKVisualDebuggerSupport = 1 << 2,     // PhysX 可视化调试器
            ArticulationSupport = 1 << 3,          // 关节体（ArticulationBody）支持
            ImmediateModeSupport = 1 << 4,         // 即时模式支持
            VehicleSupport = 1 << 5,               // 车辆（WheelCollider）支持
            CharacterControllerSupport = 1 << 6    // 角色控制器支持
        };

        internal const uint k_InvalidID = 0;
        internal const uint k_FallbackIntegrationId = 0xDECAFBAD;

        [FieldOffset(0)]
        readonly uint m_Id;                         // 集成引擎的唯一标识 ID
        [FieldOffset(4)]
        fixed ushort m_IntegrationVersion[3];       // 集成版本号（major.minor.patch）
        [FieldOffset(10)]
        fixed ushort m_SdkVersion[3];               // 底层 SDK（PhysX）版本号
        [FieldOffset(16)]
        readonly SupportedUnityFeatures m_Features; // 支持的 Unity 特性标记
        [FieldOffset(20)]
        fixed byte m_Name[16];                      // 引擎名称（如 "PhysX"）
        [FieldOffset(36)]
        fixed byte m_Desc[220];                     // 引擎描述文本
        [FieldOffset(256)]
        IntegrationLimits m_Limit;                  // 关节限制等集成层参数

        public readonly uint id => m_Id;

        // 获取物理引擎的名称
        public unsafe string name {
            get
            {
                fixed(byte* ptr = m_Name)
                    return Marshal.PtrToStringAnsi(new IntPtr(ptr));
            }
        }

        // 获取物理引擎的描述文本
        public unsafe string description
        {
            get
            {
                fixed (byte* ptr = m_Desc)
                    return Marshal.PtrToStringAnsi(new IntPtr(ptr));
            }
        }

        internal ushort sDKMajorVersion => m_SdkVersion[0];
        internal ushort sDKMinorVersion => m_SdkVersion[1];
        internal ushort sDKPatchVersion => m_SdkVersion[2];

        internal ushort majorVersion => m_IntegrationVersion[0];
        internal ushort minorVersion => m_IntegrationVersion[1];
        internal ushort patchVersion => m_IntegrationVersion[2];

        // 是否为回退引擎（当默认引擎不可用时）
        public bool isFallback => id == k_FallbackIntegrationId;

        internal bool isExperimental => m_IntegrationVersion[0] < 1;

        internal IntegrationLimits limit => m_Limit;
    }

    // ================================================================
    // 🎯 Physics — Unity 3D 物理系统核心静态类
    // ================================================================
    //
    // 📌 核心概念：
    //   1. 射线投射（Raycast）
    //      从一点沿方向发射射线，检测碰撞器
    //      返回 bool（简单检测）或 RaycastHit（详细信息）
    //
    //   2. 形状投射（Shape Cast）
    //      用球体/盒子/胶囊体沿方向扫描，检测路径上的碰撞器
    //      比射线更粗，适合角色移动检测
    //
    //   3. 重叠检测（Overlap）
    //      在固定位置检测形状是否与其他碰撞器重叠
    //      不沿方向扫描，只检测静态位置
    //
    //   4. 碰撞管理
    //      IgnoreCollision / IgnoreLayerCollision 控制碰撞矩阵
    //
    //   5. 物理仿真控制
    //      Simulate() 手动步进、SyncTransforms() 同步变换
    //
    // ⚡ 性能提示：
    //   - All/NonAlloc 方法中，NonAlloc 不产生 GC 分配，推荐频繁调用时使用
    //   - 碰撞器位置变化后需调用 SyncTransforms() 确保物理引擎同步
    //   - sleepThreshold 控制休眠阈值，可减少不必要的计算
    //
    // 🔄 默认场景模式：
    //   所有 Physics.XXX 静态方法都委托给 defaultPhysicsScene，
    //   多场景可通过 PhysicsScene 结构体独立操作。
    // ================================================================
    [NativeHeader("Modules/Physics/PhysicsQuery.h")]
    [NativeHeader("Modules/Physics/PhysicsManager.h")]
    [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
    public partial class Physics
    {
        // 对应 PhysicsConstants.h 中的 kFloatMaxMinusEpsilon，
        // 用于关节限制一致性检查等边界校验
        internal const float k_MaxFloatMinusEpsilon = 340282326356119260000000000000000000000f;

        // 🎯 忽略射线层（第 2 层），用于忽略某些对象的射线检测
        public const int IgnoreRaycastLayer = 1 << 2;

        // 🎯 默认射线层掩码，排除 IgnoreRaycastLayer 的所有层
        public const int DefaultRaycastLayers = ~IgnoreRaycastLayer;

        // 🎯 所有层的掩码（-1，即所有位为 1）
        public const int AllLayers = ~0;

        // ================================================================
        // 🔧 物理引擎集成信息
        // ================================================================

        extern private unsafe static void GetIntegrationInfos(out IntPtr integrations, out ulong integrationCount);

        [NativeMethod(IsThreadSafe = true)]
        extern private unsafe static void GetCurrentIntegrationInfo(out IntPtr integration);

        // 获取所有已注册物理引擎集成的信息列表（内部使用）
        internal static ReadOnlySpan<IntegrationInfo> GetIntegrationInfos()
        {
            unsafe
            {
                IntPtr integrations;
                ulong count;
                GetIntegrationInfos(out integrations, out count);

                return new ReadOnlySpan<IntegrationInfo>(integrations.ToPointer(), (int)count);
            }
        }

        // ================================================================
        // 🎯 GetCurrentIntegrationInfo — 获取当前物理引擎信息
        // ================================================================
        // 获取当前正在使用的物理引擎集成信息。
        // 可用于查看底层 PhysX 版本和功能支持情况。
        // ================================================================
        public unsafe static IntegrationInfo GetCurrentIntegrationInfo()
        {
            IntPtr infoPtr;
            GetCurrentIntegrationInfo(out infoPtr);

            return *(IntegrationInfo*)infoPtr.ToPointer();
        }

        // ================================================================
        // 🔧 全局物理参数
        // ================================================================

        // ================================================================
        // 🎯 gravity — 全局重力加速度
        // ================================================================
        // 全局三维重力加速度向量。
        // 默认值为 (0, -9.81, 0)，单位：米/秒²。
        // 所有 Rigidbody 的重力效果受此值影响（除非设置了 useGravity = false）。
        // ================================================================
        extern public static Vector3 gravity { [NativeMethod(IsThreadSafe = true)] get; set; }

        // ================================================================
        // 🎯 defaultContactOffset — 默认接触偏移量
        // ================================================================
        // 新创建的碰撞器的默认接触偏移量（Contact Offset）。
        // 接触偏移量是碰撞器周围的一个额外区域，用于在碰撞发生前产生接触事件，
        // 有助于提高碰撞检测的稳定性。默认值通常为 0.01。
        // ================================================================
        extern public static float defaultContactOffset { get; set; }

        // ================================================================
        // 🎯 sleepThreshold — 全局睡眠阈值
        // ================================================================
        // 当物体的动能低于此值时，物理引擎将其标记为"睡眠"状态，
        // 停止对其进行动力学求解以节省性能。
        // 默认值取决于物理引擎配置。
        // ================================================================
        extern public static float sleepThreshold { get; set; }

        // ================================================================
        // 🎯 queriesHitTriggers — 是否默认检测 Trigger
        // ================================================================
        // 控制 Physics 查询（Raycast、SphereCast 等）是否默认与 Trigger 碰撞器交互。
        // 如果为 true，查询默认会检测 Trigger；如果为 false，默认跳过 Trigger。
        // 可以在单个查询中通过 QueryTriggerInteraction 参数覆盖此设置。
        // ================================================================
        extern public static bool queriesHitTriggers { get; set; }

        // ================================================================
        // 🎯 queriesHitBackfaces — 是否检测网格背面
        // ================================================================
        // 控制 Physics 查询（Raycast、SphereCast 等）是否检测网格碰撞器的背面。
        // 如果为 true，射线从背面击中网格时也会产生命中；
        // 如果为 false（默认），仅检测正面。
        // ================================================================
        extern public static bool queriesHitBackfaces { get; set; }

        // ================================================================
        // 🎯 bounceThreshold — 弹跳阈值
        // ================================================================
        // 当两个物体的相对速度低于此值时，物理引擎不会产生弹跳效果。
        // 用于避免微小弹跳导致的性能问题。
        // ================================================================
        extern public static float bounceThreshold { get; set; }

        // ================================================================
        // 🎯 defaultMaxDepenetrationVelocity — 默认最大分离速度
        // ================================================================
        // 全局默认最大分离速度。
        // 限制物理引擎将重叠物体推开的最大速度。
        // 默认值为 Infinity，设置一个有限的数值可以防止物体被突然弹飞。
        // ================================================================
        extern public static float defaultMaxDepenetrationVelocity { get; set; }

        // ================================================================
        // 🎯 defaultSolverIterations — 默认求解器迭代次数
        // ================================================================
        // 全局默认求解器迭代次数。
        // 控制物理引擎每帧求解约束（关节、接触）的迭代次数。
        // ⚡ 增加此值可以提高物理稳定性，但会增加 CPU 开销。
        // 默认值为 6。
        // ================================================================
        extern public static int defaultSolverIterations { get; set; }

        // ================================================================
        // 🎯 defaultSolverVelocityIterations — 默认求解器速度迭代次数
        // ================================================================
        // 全局默认求解器速度迭代次数。
        // 专门用于速度层面的约束求解迭代，影响摩擦和弹跳的准确性。
        // 默认值为 1。
        // ================================================================
        extern public static int defaultSolverVelocityIterations { get; set; }

        // ================================================================
        // 🎯 simulationMode — 物理仿真模式
        // ================================================================
        // 当前物理仿真模式。
        //   - FixedUpdate：在 FixedUpdate 中自动运行（默认，推荐）
        //   - Update：在 Update 中自动运行
        //   - Script：手动调用 Physics.Simulate()
        // ================================================================
        extern public static SimulationMode simulationMode { get; set; }

        // ================================================================
        // 🎯 defaultMaxAngularSpeed — 默认最大角速度
        // ================================================================
        // 默认最大角速度（弧度/秒）。
        // 限制 Rigidbody 的最大旋转速度，防止高速旋转导致的不稳定。
        // ================================================================
        extern static public float defaultMaxAngularSpeed { get; set; }

        // ================================================================
        // 🎯 improvedPatchFriction — 改进的块状摩擦模型
        // ================================================================
        // 是否启用改进的块状摩擦（Patch Friction）模型。
        // ⚠️ 启用后使用更精确的摩擦模型，但会增加计算开销。
        // ================================================================
        extern static public bool improvedPatchFriction { get; set; }

        // ================================================================
        // 🎯 invokeCollisionCallbacks — 是否触发碰撞回调
        // ================================================================
        // 是否触发碰撞回调事件（OnCollisionEnter/Stay/Exit）。
        // 如果设为 false，所有碰撞回调都不会被调用，可节省性能。
        // ================================================================
        extern static public bool invokeCollisionCallbacks { get; set; }

        // ================================================================
        // 🎯 generateOnTriggerStayEvents — 是否生成 OnTriggerStay 事件
        // ================================================================
        // 是否生成 OnTriggerStay 事件（只读）。
        // 💡 此功能默认启用，由引擎内部管理。
        // ================================================================
        extern static public bool generateOnTriggerStayEvents { get; }

        // ================================================================
        // 🎯 defaultPhysicsScene — 默认物理场景
        // ================================================================
        // 获取默认的物理场景。
        // 所有 Physics.Raycast、Physics.SphereCast 等静态方法都使用此默认场景。
        // 💡 多场景物理可以通过 PhysicsScene 结构体进行操作。
        // ================================================================
        public static PhysicsScene defaultPhysicsScene => PhysicsScene.GetDefaultScene();

        // ================================================================
        // 🔧 碰撞忽略
        // ================================================================

        // ================================================================
        // 🎯 IgnoreCollision — 设置两个碰撞器之间的碰撞忽略
        // ================================================================
        // 设置两个碰撞器之间的碰撞忽略状态。
        // 当 ignore = true 时，两个碰撞器不会发生碰撞。
        // 📌 常用于游戏中需要穿透的效果，如玩家穿过友军。
        //
        // 参数：collider1 - 第一个碰撞器
        //       collider2 - 第二个碰撞器
        //       ignore    - 是否忽略碰撞（默认 true）
        // ================================================================
        extern public static void IgnoreCollision([NotNull] Collider collider1, [NotNull] Collider collider2, [DefaultValue("true")] bool ignore);

        [ExcludeFromDocs]
        public static void IgnoreCollision(Collider collider1, Collider collider2)
        {
            IgnoreCollision(collider1, collider2, true);
        }

        // ================================================================
        // 🎯 IgnoreLayerCollision — 设置两个层级之间的碰撞忽略
        // ================================================================
        // 设置两个层级之间的碰撞忽略状态。
        // 如果 ignore = true，则所有在 layer1 和 layer2 上的对象都不会相互碰撞。
        // 📌 这是通过 Unity 的层碰撞矩阵（Layer Collision Matrix）实现的，
        //   在 Edit → Project Settings → Physics 中可以可视化配置。
        //
        // 参数：layer1 - 第一个层的索引（0-31）
        //       layer2 - 第二个层的索引（0-31）
        //       ignore - 是否忽略碰撞（默认 true）
        // ================================================================
        [NativeName("IgnoreCollision")]
        extern public static void IgnoreLayerCollision(int layer1, int layer2, [DefaultValue("true")] bool ignore);

        [ExcludeFromDocs]
        public static void IgnoreLayerCollision(int layer1, int layer2)
        {
            IgnoreLayerCollision(layer1, layer2, true);
        }

        // 查询两个层之间是否已经设置了碰撞忽略
        extern public static bool GetIgnoreLayerCollision(int layer1, int layer2);

        // 查询两个碰撞器之间是否已经设置了碰撞忽略
        extern public static bool GetIgnoreCollision([NotNull] Collider collider1, [NotNull] Collider collider2);

        // ================================================================
        // 🔧 射线检测 (Raycast)
        // ================================================================
        // 射线检测是 3D 中最常用的碰撞查询方式。
        // 从 origin 点沿 direction 方向发射一条无限细的射线，检测是否能碰到碰撞器。
        //
        // 🎯 四种变体：
        //   Raycast()          — 简单的是/否检测，返回 bool
        //   Raycast(out hit)   — 获取第一个碰到的碰撞器详细信息
        //   RaycastAll()       — 获取路径上所有碰撞器（返回数组）
        //   RaycastNonAlloc()  — 获取所有碰撞器（复用数组，无 GC 分配）
        //
        // 📌 参数说明：
        //   origin  - 射线起点（世界空间）
        //   direction - 射线方向向量（方法内部会自动归一化）
        //   maxDistance - 最大检测距离（默认 Mathf.Infinity 无限远）
        //   layerMask - 层掩码，用于过滤检测（默认 DefaultRaycastLayers）
        //   queryTriggerInteraction - 触发器交互策略
        // ================================================================

        // ================================================================
        // 🎯 Raycast — 简单射线检测（返回 bool）
        // ================================================================
        // 发射一条简单射线，检测是否碰到任何碰撞器。
        // 返回 true 表示碰到物体，false 表示没有。不获取具体碰撞信息。
        // ================================================================
        static public bool Raycast(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🎯 Raycast — 射线检测并获取碰撞信息（out RaycastHit）
        // ================================================================
        // 发射一条射线并获取第一个碰到的碰撞器详细信息。
        // 通过 out RaycastHit 参数返回碰撞点、法线、距离等数据。
        // ================================================================
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        // 此方法实际上不由原生代码直接调用，
        // 但需要 [RequiredByNativeCode] 属性，因为 GraphicsRaycaster.cs
        // 通过反射调用此方法，以避免对 Physics 模块的硬依赖。
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 使用 Ray 结构体发射射线
        static public bool Raycast(Ray ray, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 使用 Ray 结构体发射射线并获取碰撞信息
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 线段检测 (Linecast)
        // ================================================================
        // Linecast 本质上是 Raycast 的便捷封装：
        // 从 start 到 end 发射射线，方向为 end - start，距离为两点之间的距离。
        // ================================================================

        // ================================================================
        // 🎯 Linecast — 两点之间的线段检测
        // ================================================================
        // 在两个点之间发射一条线段，检测是否碰到任何碰撞器。
        // 等同于从 start 向 (end - start) 方向发射，距离为两点距离的 Raycast。
        // ================================================================
        static public bool Linecast(Vector3 start, Vector3 end, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dir = end - start;
            return defaultPhysicsScene.Raycast(start, dir, dir.magnitude, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, int layerMask)
        {
            return Linecast(start, end, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end)
        {
            return Linecast(start, end, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 线段检测并获取碰撞信息
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dir = end - start;
            return defaultPhysicsScene.Raycast(start, dir, out hitInfo, dir.magnitude, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask)
        {
            return Linecast(start, end, out hitInfo, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo)
        {
            return Linecast(start, end, out hitInfo, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 胶囊体投射 (CapsuleCast)
        // ================================================================
        // CapsuleCast 用胶囊体沿方向扫描，检测路径上的碰撞器。
        // 胶囊体由两个端点（point1, point2）定义球体中心，中间是圆柱体连接。
        // 🎯 常用于角色移动检测（角色碰撞器通常是胶囊体）。
        //
        // 📌 参数说明：
        //   point1, point2 - 胶囊体两端的球心位置（世界坐标）
        //   radius         - 胶囊体半径
        // ================================================================

        // ================================================================
        // 🎯 CapsuleCast — 胶囊体投射（简单检测）
        // ================================================================
        // 用胶囊体沿指定方向扫描，检测是否碰到任何碰撞器。
        // 返回 bool 表示是否命中，不获取详细碰撞信息。
        // ================================================================
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hit;
            return defaultPhysicsScene.CapsuleCast(point1, point2, radius, direction, out hit, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return CapsuleCast(point1, point2, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance)
        {
            return CapsuleCast(point1, point2, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction)
        {
            return CapsuleCast(point1, point2, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 胶囊体投射并获取详细碰撞信息
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 球体投射 (SphereCast)
        // ================================================================
        // SphereCast 用球体沿方向扫描，检测路径上的碰撞器。
        // 球体由 origin 和 radius 定义。
        // 🎯 常用于子弹检测、角色碰撞检测等。
        //
        // 💡 SphereCast 与 OverlapSphere 的区别：
        //   - SphereCast：球体沿方向运动，检测路径上的物体
        //   - OverlapSphere：球体在固定位置，检测重叠的物体
        // ================================================================

        // ================================================================
        // 🎯 SphereCast — 球体投射（Vector3 参数）
        // ================================================================
        // 用球体沿指定方向扫描，检测并获取碰撞信息。
        // ================================================================
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo)
        {
            return SphereCast(origin, radius, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 使用 Ray 和半径进行球体投射（简单检测）
        static public bool SphereCast(Ray ray, float radius, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hitInfo;
            return SphereCast(ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, float maxDistance, int layerMask)
        {
            return SphereCast(ray, radius, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, float maxDistance)
        {
            return SphereCast(ray, radius, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius)
        {
            return SphereCast(ray, radius, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 使用 Ray 和半径进行球体投射（获取碰撞信息）
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCast(ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo)
        {
            return SphereCast(ray, radius, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 盒子投射 (BoxCast)
        // ================================================================
        // BoxCast 用轴对齐或旋转的盒子沿方向扫描。
        // 📌 参数说明：
        //   center     - 盒子的中心位置（世界坐标）
        //   halfExtents - 盒子的半尺寸（即 size/2）
        //   orientation - 盒子的旋转角度（默认 Quaternion.identity）
        // 💡 常用于大体积物体的移动碰撞检测。
        // ================================================================

        // ================================================================
        // 🎯 BoxCast — 盒子投射（简单检测）
        // ================================================================
        // 用盒子沿指定方向扫描（简单检测）。
        // ================================================================
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hitInfo;
            return defaultPhysicsScene.BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCast(center, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance)
        {
            return BoxCast(center, halfExtents, direction, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation)
        {
            return BoxCast(center, halfExtents, direction, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction)
        {
            return BoxCast(center, halfExtents, direction, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 用盒子沿指定方向扫描并获取碰撞信息
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 多物体射线检测 (RaycastAll)
        // ================================================================
        // 三种模式：
        //   RaycastAll()       — 返回路径上所有碰撞器（产生 GC 分配）
        //   RaycastNonAlloc()  — 复用数组，无 GC 分配（推荐用于频繁调用的场景）
        //
        // 💡 RaycastAll 在内部调用 Internal_RaycastAll（C++ FreeFunction），
        //   从 PhysX 获取所有命中结果后以托管数组形式返回。
        // ================================================================

        [FreeFunction("Physics::RaycastAll")]
        extern static RaycastHit[] Internal_RaycastAll(PhysicsScene physicsScene, Ray ray, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        // ================================================================
        // 🎯 RaycastAll — 多物体射线检测
        // ================================================================
        // 发射射线并返回路径上所有碰撞器的命中信息数组。
        // ⚠️ 注意：此方法会产生 GC 分配（分配 RaycastHit[] 数组），
        //   如果需要在每帧或 Update 中频繁调用，推荐使用 RaycastNonAlloc。
        // ================================================================
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);
                return Internal_RaycastAll(defaultPhysicsScene, ray, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            return RaycastAll(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return RaycastAll(origin, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction)
        {
            return RaycastAll(origin, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 使用 Ray 结构体的多物体射线检测
        static public RaycastHit[] RaycastAll(Ray ray, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        // 通过反射从 GraphicsRaycaster.cs 调用，避免硬依赖
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray, float maxDistance)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray)
        {
            return RaycastAll(ray.origin, ray.direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 无分配射线检测 (RaycastNonAlloc)
        // ================================================================
        // 复用传入的 RaycastHit[] 数组，不产生 GC 分配。
        // 返回值为实际命中的物体数量。
        // ⚡ 适用于需要高性能的场合（如每帧检测）。
        // ================================================================

        // ================================================================
        // 🎯 RaycastNonAlloc — 无分配射线检测（Ray 参数）
        // ================================================================
        // 无分配射线检测——将结果填充到提供的数组中。
        // 返回实际命中的物体数量（不超过数组长度）。
        // ⚡ 不产生 GC 分配，推荐在性能敏感代码中使用。
        // ================================================================
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        // 通过反射从 GraphicsRaycaster.cs 调用，避免硬依赖
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 使用 Vector3 参数的 NonAlloc 射线检测
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 胶囊体投射 - 多物体版本 (CapsuleCastAll / CapsuleCastNonAlloc)
        // ================================================================

        [FreeFunction("Physics::CapsuleCastAll")]
        extern private static RaycastHit[] Query_CapsuleCastAll(PhysicsScene physicsScene, Vector3 p0, Vector3 p1, float radius, Vector3 direction, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        // 胶囊体投射，返回路径上所有碰撞器（产生 GC 分配）
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_CapsuleCastAll(defaultPhysicsScene, point1, point2, radius, normalizedDirection, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction)
        {
            return CapsuleCastAll(point1, point2, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 球体投射 - 多物体版本 (SphereCastAll / SphereCastNonAlloc)
        // ================================================================

        [FreeFunction("Physics::SphereCastAll")]
        extern private static RaycastHit[] Query_SphereCastAll(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        // 球体投射，返回路径上所有碰撞器（产生 GC 分配）
        public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_SphereCastAll(defaultPhysicsScene, origin, radius, normalizedDirection, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction)
        {
            return SphereCastAll(origin, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 使用 Ray 参数的球体多物体检测
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCastAll(ray.origin, radius, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, int layerMask)
        {
            return SphereCastAll(ray, radius, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance)
        {
            return SphereCastAll(ray, radius, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius)
        {
            return SphereCastAll(ray, radius, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 重叠检测 (Overlap)
        // ================================================================
        // 重叠检测用于判断指定形状（球体/盒子/胶囊体）是否与任何碰撞器重叠。
        // 与 Cast 不同，Overlap 不沿方向扫描，只检测静态位置。
        //
        // 🎯 三种变体：
        //   OverlapSphere/Capsule/Box — 返回重叠碰撞器数组（GC 分配）
        //   OverlapSphere/Capsule/Box NonAlloc — 复用数组，无 GC 分配
        //   CheckSphere/Capsule/Box — 只返回是否重叠，不获取碰撞器
        // ================================================================

        //----- OverlapCapsule ---------------------------------------------------

        [FreeFunction("Physics::OverlapCapsule")]
        extern private static Collider[] OverlapCapsule_Internal(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        // ================================================================
        // 🎯 OverlapCapsule — 胶囊体重叠检测
        // ================================================================
        // 检测指定位置的胶囊体与哪些碰撞器重叠。
        // ⚠️ 返回重叠的碰撞器数组（产生 GC 分配）。
        // ================================================================
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapCapsule_Internal(defaultPhysicsScene, point0, point1, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask)
        {
            return OverlapCapsule(point0, point1, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius)
        {
            return OverlapCapsule(point0, point1, radius, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- OverlapSphere ---------------------------------------------------

        [FreeFunction("Physics::OverlapSphere")]
        extern private static Collider[] OverlapSphere_Internal(PhysicsScene physicsScene, Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        // ================================================================
        // 🎯 OverlapSphere — 球体重叠检测
        // ================================================================
        // 检测指定位置的球体与哪些碰撞器重叠。
        // ⚠️ 返回重叠的碰撞器数组（产生 GC 分配）。
        // ================================================================
        public static Collider[] OverlapSphere(Vector3 position, float radius, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapSphere_Internal(defaultPhysicsScene, position, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask)
        {
            return OverlapSphere(position, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapSphere(Vector3 position, float radius)
        {
            return OverlapSphere(position, radius, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 物理仿真控制
        // ================================================================

        // 🎯 Simulate_Internal — 执行一步物理仿真（内部方法）
        // step：仿真步长时间（秒），通常使用 Time.fixedDeltaTime
        // stages：要执行的仿真阶段
        // options：仿真选项
        [NativeName("Simulate")]
        extern internal static void Simulate_Internal(PhysicsScene physicsScene, float step, SimulationStage stages, SimulationOption options);

        // ================================================================
        // 🎯 Simulate — 手动执行一步物理仿真
        // ================================================================
        // 手动执行一步物理仿真。
        // 📌 仅在 Physics.simulationMode = Script 时有效。
        //   step：仿真步长（秒）。
        //   调用此方法会自动运行所有仿真阶段（准备→模拟→发布）。
        // ================================================================
        public static void Simulate(float step)
        {
            if (simulationMode != SimulationMode.Script)
            {
                Debug.LogWarning("Physics.Simulate(...) was called but simulation mode is not set to Script. You should set simulation mode to Script first before calling this function therefore the simulation was not run.");
                return;
            }

            Simulate_Internal(defaultPhysicsScene, step, SimulationStage.All, SimulationOption.All);
        }

        [NativeName("InterpolateBodies")]
        extern internal static void InterpolateBodies_Internal(PhysicsScene physicsScene);

        [NativeName("ResetInterpolatedTransformPosition")]
        extern internal static void ResetInterpolationPoses_Internal(PhysicsScene physicsScene);

        // ================================================================
        // 🎯 SyncTransforms — 手动同步 Transform 到物理引擎
        // ================================================================
        // 手动同步 Transform 到物理引擎。
        // 📌 当通过 Transform.position/rotation 直接移动物体时，
        //   物理引擎中的碰撞器位置不会自动更新。
        //   调用此方法可以强制同步，确保后续的 Raycast/Overlap 使用最新位置。
        // ================================================================
        extern public static void SyncTransforms();

        // ================================================================
        // 🎯 reuseCollisionCallbacks — 复用碰撞回调中的 Collision 对象
        // ================================================================
        // 是否复用碰撞回调中的 Collision 对象。
        // 💡 启用后减少 GC 分配，但需要注意在回调中及时处理数据，
        //   因为同一个 Collision 对象可能在下一个回调中被重用。
        // ================================================================
        extern public static bool reuseCollisionCallbacks { get; set; }

        // ================================================================
        // 🔧 穿透计算
        // ================================================================

        // ================================================================
        // 🎯 ComputePenetration — 计算碰撞器穿透信息
        // ================================================================
        // 计算两个碰撞器之间的穿透（Penetration）信息。
        // 返回是否有穿透，以及最小分离方向和距离。
        // 📌 用于手动处理碰撞恢复，或在碰撞发生前检测即将发生的穿透。
        // ================================================================
        [FreeFunction("Physics::ComputePenetration")]
        extern private static bool Query_ComputePenetration([NotNull] Collider colliderA, Vector3 positionA, Quaternion rotationA, [NotNull] Collider colliderB, Vector3 positionB, Quaternion rotationB, ref Vector3 direction, ref float distance);

        public static bool ComputePenetration(Collider colliderA, Vector3 positionA, Quaternion rotationA, Collider colliderB, Vector3 positionB, Quaternion rotationB, out Vector3 direction, out float distance)
        {
            direction = Vector3.zero;
            distance = 0f;
            return Query_ComputePenetration(colliderA, positionA, rotationA, colliderB, positionB, rotationB, ref direction, ref distance);
        }

        // ================================================================
        // 🎯 ClosestPoint — 计算碰撞器表面最近点
        // ================================================================
        // 计算碰撞器表面上离指定点最近的点。
        // 💡 可用于获取精确的"碰撞表面"位置。
        // ================================================================
        [FreeFunction("Physics::ClosestPoint")]
        extern private static Vector3 Query_ClosestPoint([NotNull] Collider collider, Vector3 position, Quaternion rotation, Vector3 point);

        public static Vector3 ClosestPoint(Vector3 point, Collider collider, Vector3 position, Quaternion rotation)
        {
            return Query_ClosestPoint(collider, position, rotation, point);
        }

        // ================================================================
        // 🔧 布料（Cloth）碰撞参数
        // ================================================================

        [StaticAccessor("GetPhysicsManager()")]
        public extern static float interCollisionDistance {[NativeName("GetClothInterCollisionDistance")] get; [NativeName("SetClothInterCollisionDistance")] set; }

        [StaticAccessor("GetPhysicsManager()")]
        public extern static float interCollisionStiffness {[NativeName("GetClothInterCollisionStiffness")] get; [NativeName("SetClothInterCollisionStiffness")] set; }

        [StaticAccessor("GetPhysicsManager()")]
        public extern static bool interCollisionSettingsToggle {[NativeName("GetClothInterCollisionSettingsToggle")] get; [NativeName("SetClothInterCollisionSettingsToggle")] set; }

        // 🎯 布料（Cloth）使用的独立重力向量
        extern public static Vector3 clothGravity { [NativeMethod(IsThreadSafe = true)] get; set; }

        //----- OverlapSphere NonAlloc -------------------------------------------

        // 🎯 无分配球体重叠检测——将结果填充到提供的数组中
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.OverlapSphere(position, radius, results, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask)
        {
            return OverlapSphereNonAlloc(position, radius, results, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results)
        {
            return OverlapSphereNonAlloc(position, radius, results, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- CheckSphere ------------------------------------------------------

        // 🎯 检测指定位置的球体是否与任何碰撞器重叠（仅返回 bool，不获取碰撞器）
        [FreeFunction("Physics::SphereTest")]
        extern private static bool CheckSphere_Internal(PhysicsScene physicsScene, Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        public static bool CheckSphere(Vector3 position, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckSphere_Internal(defaultPhysicsScene, position, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckSphere(Vector3 position, float radius, int layerMask)
        {
            return CheckSphere(position, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckSphere(Vector3 position, float radius)
        {
            return CheckSphere(position, radius, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- CapsuleCast NonAlloc ---------------------------------------------

        // 🎯 无分配胶囊体投射——将结果填充到提供的数组中
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.CapsuleCast(point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- SphereCast NonAlloc ---------------------------------------------

        // 🎯 无分配球体投射——将结果填充到提供的数组中
        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.SphereCast(origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        // 使用 Ray 参数的无分配球体投射
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCastNonAlloc(ray.origin, radius, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results)
        {
            return SphereCastNonAlloc(ray, radius, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- CheckCapsule -----------------------------------------------------

        // 🎯 检测指定位置的胶囊体是否与任何碰撞器重叠
        [FreeFunction("Physics::CapsuleTest")]
        extern private static bool CheckCapsule_Internal(PhysicsScene physicsScene, Vector3 start, Vector3 end, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckCapsule_Internal(defaultPhysicsScene, start, end, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask)
        {
            return CheckCapsule(start, end, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius)
        {
            return CheckCapsule(start, end, radius, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- CheckBox ---------------------------------------------------------

        // ================================================================
        // 🎯 CheckBox — 检测盒子是否与碰撞器重叠
        // ================================================================
        // 检测指定位置和旋转的盒子是否与任何碰撞器重叠。
        // 📌 参数说明：
        //   center      - 盒子的世界坐标中心点
        //   halfExtents - 盒子的半尺寸（即 size/2）
        //   orientation - 盒子的旋转（默认 Quaternion.identity）
        // ================================================================
        [FreeFunction("Physics::BoxTest")]
        extern private static bool CheckBox_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, int layermask, QueryTriggerInteraction queryTriggerInteraction);

        public static bool CheckBox(Vector3 center, Vector3 halfExtents, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("DefaultRaycastLayers")] int layermask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckBox_Internal(defaultPhysicsScene, center, halfExtents, orientation, layermask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask)
        {
            return CheckBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            return CheckBox(center, halfExtents, orientation, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents)
        {
            return CheckBox(center, halfExtents, Quaternion.identity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- OverlapBox -------------------------------------------------------

        // 🎯 检测指定位置的盒子与哪些碰撞器重叠（产生 GC 分配）
        [FreeFunction("Physics::OverlapBox")]
        extern private static Collider[] OverlapBox_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapBox_Internal(defaultPhysicsScene, center, halfExtents, orientation, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask)
        {
            return OverlapBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            return OverlapBox(center, halfExtents, orientation, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        // 🎯 无分配盒子重叠检测——将结果填充到提供的数组中
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("AllLayers")] int mask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.OverlapBox(center, halfExtents, results, orientation, mask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, int mask)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, mask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, Quaternion.identity, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- BoxCast NonAlloc --------------------------------------------------

        // 🎯 无分配盒子投射——将结果填充到提供的数组中
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.BoxCast(center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- BoxCastAll --------------------------------------------------------

        [FreeFunction("Physics::BoxCastAll")]
        private static extern RaycastHit[] Internal_BoxCastAll(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        // 🎯 盒子投射，返回路径上所有碰撞器（产生 GC 分配）
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Internal_BoxCastAll(defaultPhysicsScene, center, halfExtents, normalizedDirection, orientation, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction)
        {
            return BoxCastAll(center, halfExtents, direction, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        //----- OverlapCapsule NonAlloc -------------------------------------------

        // 🎯 无分配胶囊体重叠检测——将结果填充到提供的数组中
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.OverlapCapsule(point0, point1, radius, results, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, int layerMask)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        // ================================================================
        // 🔧 宽阶段（Broadphase）优化
        // ================================================================

        // ================================================================
        // 🎯 RebuildBroadphaseRegions — 重建宽阶段碰撞检测区域
        // ================================================================
        // 重建物理引擎的宽阶段碰撞检测区域（Broadphase Regions）。
        // 💡 用于优化大型开放世界的物理检测性能。
        // 📌 参数说明：
        //   worldBounds    - 世界边界范围
        //   subdivisions   - 空间划分数量（建议值为 2-8）
        // ================================================================
        [StaticAccessor("GetPhysicsManager()")]
        public static extern void RebuildBroadphaseRegions(Bounds worldBounds, int subdivisions);

        // ================================================================
        // 🔧 Mesh 烘焙
        // ================================================================

        // ================================================================
        // 🎯 BakeMesh — 烘焙网格数据用于 MeshCollider
        // ================================================================
        // 烘焙（预处理）网格数据以用于 MeshCollider。
        // 📌 烘焙过程会生成 PhysX 内部使用的加速结构（BVH 等）。
        //   此操作可在线程安全环境中调用。
        // ================================================================
        [StaticAccessor("GetPhysicsManager()")]
        [NativeMethod(IsThreadSafe = true)]
        public static extern void BakeMesh(EntityId meshEntityId, bool convex, MeshColliderCookingOptions cookingOptions);

        [Obsolete("BakeMesh(int, bool, MeshColliderCookingOptions) is obsolete. Use BakeMesh(EntityId, bool, MeshColliderCookingOptions) instead.", true)]
        public static void BakeMesh(int meshID, bool convex, MeshColliderCookingOptions cookingOptions) => BakeMesh((EntityId)meshID, convex, cookingOptions);

        [Obsolete("BakeMesh(int, bool) is obsolete. Use BakeMesh(EntityId, bool) instead.", true)]
        public static void BakeMesh(int meshID, bool convex)
        {
            BakeMesh((EntityId)meshID, convex, MeshColliderCookingOptions.CookForFasterSimulation |
                                     MeshColliderCookingOptions.EnableMeshCleaning |
                                     MeshColliderCookingOptions.WeldColocatedVertices |
                                     MeshColliderCookingOptions.UseFastMidphase);
        }

        // 烘焙网格数据用于 MeshCollider（使用默认烹饪选项）
        public static void BakeMesh(EntityId meshEntityId, bool convex)
        {
            BakeMesh(meshEntityId, convex, MeshColliderCookingOptions.CookForFasterSimulation |
                                     MeshColliderCookingOptions.EnableMeshCleaning |
                                     MeshColliderCookingOptions.WeldColocatedVertices |
                                     MeshColliderCookingOptions.UseFastMidphase);
        }

        // ================================================================
        // 🔧 内部方法：调试与事件分发
        // ================================================================

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern bool ConnectPhysicsSDKVisualDebugger();

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern void DisconnectPhysicsSDKVisualDebugger();

        // 🎯 通过 EntityId 获取碰撞器实例（内部使用）
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        extern internal static Collider GetColliderByInstanceID(EntityId entityId);

        // 🎯 通过 EntityId 获取物理体（Rigidbody/ArticulationBody）组件（内部使用）
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern Component GetBodyByInstanceID(EntityId entityId);

        // 🎯 将 PhysX 内部的三角形索引转换为 Unity 的三角形索引（线程安全）
        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern uint TranslateTriangleIndexFromID(EntityId instanceID, uint faceIndex);

        // ================================================================
        // 🔧 碰撞事件分发
        // ================================================================
        // 以下方法由物理引擎在碰撞发生时通过原生代码调用，
        // 最终触发 MonoBehaviour 的 OnCollisionEnter/Stay/Exit 回调。

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        private static extern void SendOnCollisionEnter(Component component, Collision collision);
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        private static extern void SendOnCollisionStay(Component component,  Collision collision);
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        private static extern void SendOnCollisionExit(Component component,  Collision collision);
    }
}
