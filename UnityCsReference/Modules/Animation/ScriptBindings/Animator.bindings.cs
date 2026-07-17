// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// Animator 模块 - Mecanim 动画系统
//
// 【架构概述】
// Mecanim 是 Unity 的第三代动画系统，核心组件是 Animator。
// 它使用"状态机(StateMachine)"思想来管理动画流程，包含以下核心概念：
//
// 1. Animator Controller（动画控制器）
//    - 资源文件(.controller)，由 AnimatorController 类管理
//    - 包含状态机、状态、过渡(Transition)、 Blend Tree 等
//    - 运行时表现为 RuntimeAnimatorController
//
// 2. 状态（State）
//    - 代表一个动画状态，通常关联一个 AnimationClip 或 Blend Tree
//    - 每个状态有速度(Speed)、镜像(Mirror)、标签(Tag)等属性
//    - 通过 AnimatorStateInfo 暴露运行时信息
//
// 3. 过渡（Transition）
//    - 定义状态之间的切换条件
//    - 支持持续时间(Duration)、偏移量(Offset)、中断源等
//    - 通过 AnimatorTransitionInfo 暴露运行时信息
//
// 4. 参数（Parameter）
//    - 状态机的输入条件：Float / Int / Bool / Trigger
//    - Trigger 类似 Bool 但可被自动重置（适合一次性事件）
//    - 参数可通过脚本 (SetFloat/SetTrigger等) 或动画曲线驱动
//
// 5. 层级（Layer）
//    - 可以叠加多个动画层（如上半身射击 + 下半身行走）
//    - 每层有权重(Weight)、遮罩(AvatarMask)和混合模式
//    - 支持 Additive 和 Override 两种混合模式
//
// 6. IK 系统（Inverse Kinematics）
//    - 通过 OnAnimatorIK 回调实现反向动力学
//    - 支持手、脚、头部的 IK 目标
//    - 支持 LookAt（看向目标点）
//
// 7. 根运动（Root Motion）
//    - 角色位置/旋转由动画驱动而非物理引擎
//    - 通过 ApplyRootMotion 开关控制
//    - 通过 deltaPosition/deltaRotation 获取每一帧的位移
//
// 8. Playable API 集成
//    - Mecanim 建立于 Playable 框架之上
//    - AnimatorControllerPlayable 允许在 PlayableGraph 中使用
//    - 支持自定义 Playable 与 Animator 混合
//
// 【数据流】
// 输入参数 → 状态机评估 → 当前/下一状态 → 混合 → 姿势(Pose) → 骨骼(Transform)
//
// 【性能层级】
// 根据 CullingMode 不同，Animator 的更新分为：
// 1. AlwaysAnimate - 永远全量更新
// 2. CullUpdateTransforms - 不可见时禁用骨骼写入，但状态机继续运行
// 3. CullCompletely - 不可见时完全禁用所有动画更新
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Playables;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    /// <summary>
    /// Avatar 目标部位枚举。
    /// 用于 MatchTarget 功能，指定要将角色的哪个部位匹配到目标位置/旋转。
    /// </summary>
    public enum AvatarTarget
    {
        /// <summary>根节点，即游戏对象的位置。</summary>
        Root = 0,
        /// <summary>身体质心(COM, Center of Mass)。</summary>
        Body = 1,
        /// <summary>左脚。</summary>
        LeftFoot = 2,
        /// <summary>右脚。</summary>
        RightFoot = 3,
        /// <summary>左手。</summary>
        LeftHand = 4,
        /// <summary>右手。</summary>
        RightHand = 5,
    }

    /// <summary>
    /// IK（反向动力学）目标部位枚举。
    /// 用于 SetIKPosition / SetIKRotation 等 IK 相关方法，指定要控制哪个肢体末端。
    /// IK 是 Mecanim 的高级功能，在 OnAnimatorIK 回调中使用。
    /// </summary>
    public enum AvatarIKGoal
    {
        /// <summary>左脚 IK 目标。</summary>
        LeftFoot = 0,
        /// <summary>右脚 IK 目标。</summary>
        RightFoot = 1,
        /// <summary>左手 IK 目标。</summary>
        LeftHand = 2,
        /// <summary>右手 IK 目标。</summary>
        RightHand = 3
    }

    /// <summary>
    /// IK 提示（Hint）部位枚举。
    /// 用于设置 IK 的"提示位置"，帮助 IK 算法确定中间关节（如膝盖、手肘）的朝向。
    /// 例如设置膝盖的 Hint 位置可以控制抬腿时膝盖是朝外还是朝前。
    /// </summary>
    public enum AvatarIKHint
    {
        /// <summary>左膝盖 IK 提示。</summary>
        LeftKnee = 0,
        /// <summary>右膝盖 IK 提示。</summary>
        RightKnee = 1,
        /// <summary>左手肘 IK 提示。</summary>
        LeftElbow = 2,
        /// <summary>右手肘 IK 提示。</summary>
        RightElbow = 3
    }

    /// <summary>
    /// Animator Controller 参数类型枚举。
    /// 定义状态机中可以使用的参数类型。
    ///
    /// 对比说明：
    /// - Float/Int：数值型参数，适合连续值（速度、血量）或离散值（ID、计数）
    /// - Bool：布尔型参数，适合状态开关（是否在跑、是否死亡）
    /// - Trigger：触发器参数，触发后自动重置（适合一次性的攻击、跳跃事件）
    ///
    /// Trigger 和 Bool 的关键区别：
    /// Trigger 在被状态机消费后会自动重置回 false，无需手动 ResetTrigger。
    /// Bool 则保持其值不变，直到被脚本主动修改。
    /// </summary>
    public enum AnimatorControllerParameterType
    {
        Float = 1,
        Int = 3,
        Bool = 4,
        Trigger = 9,
    }

    /// <summary>
    /// 控制器参数类型常量。
    /// 类型 0 表示"无效类型"，用于参数不存在时的返回判断。
    /// 用户不需要直接接触此类型。
    /// </summary>
    internal static class AnimatorControllerParameterTypeConstants
    {
        public const int InvalidType = 0;
    }

    /// <summary>
    /// 过渡类型枚举（内部使用）。
    /// 区分一个 Transition 是普通的 Normal，还是从 Entry 节点或到 Exit 节点。
    /// 位标志设计，可组合使用。
    /// </summary>
    internal enum TransitionType
    {
        Normal = 1 << 0,
        Entry  = 1 << 1,
        Exit   = 1 << 2
    }

    /// <summary>
    /// 动画状态信息索引（内部使用）。
    /// 用于 GetAnimatorStateInfo 时指定要获取哪一帧的状态信息。
    /// </summary>
    internal enum StateInfoIndex
    {
        CurrentState,      // 当前状态
        NextState,         // 下一状态（过渡目标）
        ExitState,         // 退出状态
        InterruptedState   // 被中断的状态
    }

    /// <summary>
    /// Animator 录制模式枚举。
    /// </summary>
    public enum AnimatorRecorderMode
    {
        Offline,   // 离线模式（正常模式）
        Playback,  // 回放模式（播放录制的动画数据）
        Record     // 录制模式（录制动画数据）
    }

    /// <summary>
    /// 持续时间单位枚举。
    /// 用于 AnimatorTransitionInfo 中的过渡持续时间表示方式。
    /// Fixed = 以秒为单位的绝对时间，Normalized = 以状态时长为基准的归一化值。
    /// </summary>
    public enum DurationUnit
    {
        Fixed,
        Normalized
    }

    /// <summary>
    /// Animator 裁剪模式枚举。
    /// 控制当角色在摄像机视野外时，Animator 的更新行为。
    /// 这是一个性能优化层级：
    /// AlwaysAnimate &gt; CullUpdateTransforms &gt; CullCompletely
    /// </summary>
    public enum AnimatorCullingMode
    {
        /// <summary>始终完全动画化角色。即使在屏幕外也会持续更新所有动画计算。</summary>
        AlwaysAnimate = 0,

        /// <summary>
        /// 当渲染器不可见时，禁用重定向目标、IK 和 Transform 写入。
        /// 状态机仍然运行，但不更新骨骼位置，适合角色远离相机但仍然需要触发动画事件的场景。
        /// </summary>
        CullUpdateTransforms = 1,

        /// <summary>
        /// 当渲染器不可见时，完全禁用动画更新。
        /// 状态机停止，骨骼不做任何更新。性能最优但可能导致角色从屏幕外进入时位置跳跃。
        /// </summary>
        CullCompletely = 2,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Enum member AnimatorCullingMode.BasedOnRenderers has been deprecated. Use AnimatorCullingMode.CullUpdateTransforms instead. (UnityUpgradable) -> CullUpdateTransforms", true)]
        BasedOnRenderers = 1,
    }

    /// <summary>
    /// Animator 更新模式枚举。
    /// 控制 Animator 的评估时机，影响动画更新与游戏循环的同步方式。
    /// </summary>
    public enum AnimatorUpdateMode
    {
        /// <summary>正常模式：Animator 在 Update 中更新，受 Time.timeScale 影响。</summary>
        Normal = 0,
        /// <summary>固定更新模式：Animator 在 FixedUpdate 中更新，适合与物理系统交互时使用。</summary>
        Fixed = 1,
        /// <summary>不受时间缩放影响：Animator 使用 Time.unscaledDeltaTime，适合 UI、暂停菜单等不应被 timeScale 影响的动画。</summary>
        UnscaledTime = 2,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Enum member AnimatorUpdateMode.AnimatePhysics has been deprecated. Use AnimatorUpdateMode.Fixed to evaluate in FixedUpdate time and Animator.animatePhysics to sync transforms for physics. (UnityUpgradable) -> Fixed", true)]
        AnimatePhysics = 1
    }

    #pragma warning disable 649 // 字段从未被赋值，将保持默认值。这些字段由 C++ 端填充。

    /// <summary>
    /// 当前正在播放的动画剪辑及其混合权重的信息结构体。
    /// 由 C++ 端填充数据，C# 端只读。
    /// 用于 GetCurrentAnimatorClipInfo / GetNextAnimatorClipInfo 的返回值。
    ///
    /// 一个状态可能包含多个 AnimationClip（如 Blend Tree），
    /// 每个 clip 对应一个 AnimatorClipInfo，有各自的混合权重。
    /// </summary>
    [NativeHeader("Modules/Animation/AnimatorInfo.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animation.bindings.h")]
    [UsedByNativeCode]
    public struct AnimatorClipInfo
    {
        /// <summary>当前播放的 AnimationClip 引用（可为 null，当只有一个剪辑或剪辑被覆盖时）。</summary>
        public AnimationClip clip
        {
            get {return m_ClipEntityId != EntityId.None ? InstanceIDToAnimationClipPPtr(m_ClipEntityId) : null; }
        }

        /// <summary>该动画剪辑在当前混合中的权重。范围为 [0, 1]。</summary>
        public float weight
        {
            get { return m_Weight; }
        }

        [FreeFunction("AnimationBindings::InstanceIDToAnimationClipPPtr")]
        extern private static AnimationClip InstanceIDToAnimationClipPPtr(EntityId entityId);

        private EntityId m_ClipEntityId;   // 动画剪辑的实体 ID（内部标识符）
        private float m_Weight;            // 该剪辑的混合权重
    }

    /// <summary>
    /// 当前或下一状态的运行时信息结构体。
    /// 通过 GetCurrentAnimatorStateInfo / GetNextAnimatorStateInfo 获取。
    /// 包含归一化时间、时长、速度、标签、是否循环等关键状态数据。
    ///
    /// 重要概念：
    /// - normalizedTime：归一化时间，0~1表示播放进度。超过1表示正在循环或在过渡中。
    ///   loop为true时，normalizedTime = 1.5 表示已播放到第二遍的50%。
    /// - fullPathHash / shortNameHash：状态名称的哈希值，用于比较身份。
    /// - tagHash：状态的标签（Tag）哈希值，用于分组判断。
    /// - speed：状态机的速度乘数，受 Animator.speed 影响。
    /// </summary>
    [NativeHeader("Modules/Animation/AnimatorInfo.h")]
    [RequiredByNativeCode]
    public struct AnimatorStateInfo
    {
        /// <summary>
        /// 检查活动状态的名称是否与给定名称匹配。
        /// 内部同时匹配 fullPathHash、shortNameHash 和 pathHash，兼容不同层级的名称引用。
        /// </summary>
        public bool IsName(string name)    { int hash = Animator.StringToHash(name); return hash == m_FullPath || hash == m_Name || hash == m_Path; }

        /// <summary>状态的完整路径哈希值（包含层级和状态机结构）。推荐使用此字段进行状态比较。</summary>
        public int fullPathHash             { get { return m_FullPath; } }

        [Obsolete("AnimatorStateInfo.nameHash has been deprecated. Use AnimatorStateInfo.fullPathHash instead.")]
        public int nameHash                 { get { return m_Path; } }

        /// <summary>状态的短名称哈希值（仅状态名本身，不包含路径）。</summary>
        public int shortNameHash            { get { return m_Name; } }

        /// <summary>
        /// 状态的归一化时间。
        /// 范围 [0, 1) 表示未循环单次播放，[1, 2) 表示第一次循环，以此类推。
        /// 如果状态正在过渡到另一个状态，normalizedTime 会停在过渡源状态的归一化时间上。
        /// </summary>
        public float normalizedTime         { get { return m_NormalizedTime; } }

        /// <summary>状态的当前时长（秒）。等于关联的 AnimationClip 的长度或 Blend Tree 的平均时长。</summary>
        public float length                 { get { return m_Length; } }

        /// <summary>状态的播放速度（受 Animator.speed 和状态机的 Speed 参数影响）。</summary>
        public float speed                  { get { return m_Speed; } }

        /// <summary>状态的速度乘数（可在状态机中单独设置）。</summary>
        public float speedMultiplier        { get { return m_SpeedMultiplier; } }

        /// <summary>状态的标签哈希值。标签可以用于对状态进行分组。</summary>
        public int tagHash                  { get { return m_Tag; } }

        /// <summary>
        /// 检查活动状态的标签是否与给定标签匹配。
        /// 标签可用于将多个状态分组并在代码中统一判断（如"受击"分组）。
        /// </summary>
        public bool IsTag(string tag)      { return Animator.StringToHash(tag) == m_Tag; }

        /// <summary>该状态是否设置为循环播放。</summary>
        public bool loop                    { get { return m_Loop != 0; } }

        private int    m_Name;             // 短名称哈希
        private int    m_Path;             // 路径哈希（向后兼容）
        private int    m_FullPath;         // 完整路径哈希
        private float  m_NormalizedTime;   // 归一化时间
        private float  m_Length;           // 状态时长（秒）
        private float  m_Speed;            // 播放速度
        private float  m_SpeedMultiplier;  // 速度乘数
        private int    m_Tag;              // 标签哈希
        private int    m_Loop;             // 是否循环标志
    }

    /// <summary>
    /// 当前过渡（Transition）的运行时信息结构体。
    /// 通过 GetAnimatorTransitionInfo 获取。
    /// 包含过渡的持续时间、归一化进度、过渡类型等信息。
    ///
    /// 过渡是状态机中状态切换的过程，duration 和 normalizedTime 描述了过渡的进度。
    /// duration 的单位由 hasFixedDuration 决定（秒或归一化百分比）。
    /// </summary>
    [NativeHeader("Modules/Animation/AnimatorInfo.h")]
    [RequiredByNativeCode]
    public struct AnimatorTransitionInfo
    {
        /// <summary>检查活动过渡的名称是否与给定名称匹配。</summary>
        public bool IsName(string name) { return Animator.StringToHash(name) == m_Name  || Animator.StringToHash(name) == m_FullPath; }

        /// <summary>检查活动过渡的用户指定名称是否与给定名称匹配。</summary>
        public bool IsUserName(string name) { return Animator.StringToHash(name) == m_UserName; }

        /// <summary>过渡的完整路径哈希。</summary>
        public int fullPathHash               { get { return m_FullPath; } }

        /// <summary>过渡的唯一名称哈希。</summary>
        public int nameHash                   { get { return m_Name; } }

        /// <summary>过渡的用户指定名称哈希。</summary>
        public int userNameHash               { get { return m_UserName; } }

        /// <summary>过渡持续时间的单位：Fixed（秒）或 Normalized（状态时长的百分比）。</summary>
        public DurationUnit durationUnit      { get { return m_HasFixedDuration ? DurationUnit.Fixed : DurationUnit.Normalized; } }

        /// <summary>过渡的持续时间（秒或归一化值，取决于 durationUnit）。</summary>
        public float duration                 { get { return m_Duration; } }

        /// <summary>过渡的归一化时间进度。[0, 1] 范围，0=过渡开始，1=过渡结束。</summary>
        public float normalizedTime           { get { return m_NormalizedTime; } }

        /// <summary>该过渡是否来自 Any State（任意状态）节点。</summary>
        public bool anyState                  { get { return m_AnyState; } }

        /// <summary>（内部）是否为从 Entry 节点的过渡。</summary>
        internal bool entry                   { get { return (m_TransitionType & (int)TransitionType.Entry) != 0; }}

        /// <summary>（内部）是否为到 Exit 节点的过渡。</summary>
        internal bool exit                    { get { return (m_TransitionType & (int)TransitionType.Exit) != 0; }}

        // 以下字段由 C++ 端通过 [NativeName] 属性自动映射填充
        [NativeName("fullPathHash")]
        private int   m_FullPath;            // 完整路径哈希
        [NativeName("userNameHash")]
        private int   m_UserName;            // 用户指定名称哈希
        [NativeName("nameHash")]
        private int   m_Name;                // 过渡名称哈希
        [NativeName("hasFixedDuration")]
        private bool  m_HasFixedDuration;    // 是否使用固定时长（否则使用归一化时长）
        [NativeName("duration")]
        private float m_Duration;            // 过渡持续时长
        [NativeName("normalizedTime")]
        private float m_NormalizedTime;      // 过渡归一化进度
        [NativeName("anyState")]
        private bool  m_AnyState;            // 是否来自 Any State
        [NativeName("transitionType")]
        private int   m_TransitionType;      // 过渡类型（位标志组合）
    }
    #pragma warning restore 649

    /// <summary>
    /// 位置和旋转权重遮罩，用于 Animator.MatchTarget 方法。
    /// 指定在匹配目标时，位置和旋转各占多少权重。
    /// 例如 positionXYZWeight = (1,0,0) 表示仅匹配 X 轴位置，忽略 Y 和 Z。
    /// </summary>
    [NativeHeader("Modules/Animation/Animator.h")]
    public struct MatchTargetWeightMask
    {
        /// <summary>使用指定的位置和旋转权重创建 MatchTargetWeightMask。</summary>
        /// <param name="positionXYZWeight">位置权重向量（每个轴独立权重）。</param>
        /// <param name="rotationWeight">旋转权重（标量，0~1）。</param>
        public MatchTargetWeightMask(Vector3 positionXYZWeight, float rotationWeight)
        {
            m_PositionXYZWeight = positionXYZWeight;
            m_RotationWeight = rotationWeight;
        }

        /// <summary>位置权重（XYZ 每个轴独立设置，值为0~1）。例如 (0,1,0) 表示只匹配 Y 轴位置。</summary>
        public Vector3 positionXYZWeight
        {
            get { return m_PositionXYZWeight; }
            set { m_PositionXYZWeight = value; }
        }

        /// <summary>旋转权重（0~1）。0表示不匹配旋转，1表示完全匹配目标旋转。</summary>
        public float rotationWeight
        {
            get { return m_RotationWeight; }
            set { m_RotationWeight = value; }
        }

        private Vector3 m_PositionXYZWeight;  // 各轴位置权重
        private float m_RotationWeight;        // 旋转权重
    }

    /// <summary>
    /// Animator - Mecanim 动画系统的核心组件。
    ///
    /// Animator 是 Unity 中最主要的动画控制组件，通过状态机驱动角色动画。
    /// 它管理 Avatar、Animation Controller、状态切换、参数传递、IK 和 Root Motion 等功能。
    ///
    /// 【使用方式】
    /// 1. 将 Animator Controller 资源赋予 runtimeAnimatorController
    /// 2. 设置 Avatar（人形角色必须）
    /// 3. 通过脚本设置参数（SetFloat / SetBool / SetTrigger）控制状态切换
    /// 4. 通过 Play / CrossFade 方法直接播放特定状态
    /// 5. 通过 GetCurrentAnimatorStateInfo 获取当前播放状态的信息
    ///
    /// 【生命周期】
    /// OnEnable → OnAnimatorMove（可选）→ Update → OnAnimatorIK（可选）
    /// OnStateEnter / OnStateUpdate / OnStateExit（StateMachineBehaviour 回调）
    ///
    /// 【性能优化】
    /// - 合理使用 cullingMode 避免屏幕外角色消耗性能
    /// - 使用 layer 分离不同身体部位的动画
    /// - 对于简单动画，考虑使用旧版 Animation 组件或直接操作 Transform
    /// </summary>
    [NativeHeader("Modules/Animation/Animator.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animator.bindings.h")]
    [UsedByNativeCode]
    public partial class Animator : Behaviour
    {
        /// <summary>当前角色骨架是否可优化（Optimize Game Object 启用时返回 true）。</summary>
        extern public bool isOptimizable
        {
            [NativeMethod("IsOptimizable")]
            get;
        }

        /// <summary>
        /// 当前角色是否为 Humanoid（人形）类型。
        /// 返回 true 表示使用 Humanoid Avatar（支持 IK、重定向等高级功能），
        /// 返回 false 表示 Generic（通用）类型（功能受限）。
        /// </summary>
        extern public bool isHuman
        {
            [NativeMethod("IsHuman")]
            get;
        }

        /// <summary>
        /// 当前 Generic 角色是否有根运动（Root Motion）。
        /// 对于 Humanoid 角色，此属性始终返回 true；
        /// 对于 Generic 角色，取决于导入设置中是否勾选 Root Motion。
        /// </summary>
        extern public bool hasRootMotion
        {
            [NativeMethod("HasRootMotion")]
            get;
        }

        /// <summary>（内部）根位置/旋转是否由动画曲线驱动。</summary>
        extern internal bool isRootPositionOrRotationControlledByCurves
        {
            [NativeMethod("IsRootTranslationOrRotationControllerByCurves")]
            get;
        }

        /// <summary>
        /// 当前 Humanoid Avata 的缩放比例。
        /// Generic 角色默认为 1.0。
        /// 该值取决于 Avatar 定义中的人体骨骼描述和角色实际身高。
        /// </summary>
        extern public float humanScale
        {
            get;
        }

        /// <summary>
        /// Animator 是否已完成初始化并可用于动画更新。
        /// 通常在 Awake 之后变为 true，Disable 时为 false。
        /// </summary>
        extern public bool isInitialized
        {
            [NativeMethod("IsInitialized")]
            get;
        }

        // ============================================================
        // 参数访问方法
        //
        // Animator 支持四种参数类型：Float、Int、Bool、Trigger
        // 每个参数可通过字符串名称(name)或整数哈希(id)访问。
        //
        // 使用整数哈希(id)访问更快（避免字符串哈希计算），推荐在性能敏感场景使用。
        // 哈希值可通过 Animator.StringToHash 提前计算。
        //
        // SetFloat 的 dampTime 参数可实现值的"缓动"效果：
        // 当 dampTime > 0 时，值会从当前值平滑过渡到目标值，而非立即跳变。
        // ============================================================

        /// <summary>获取 Float 类型参数的值（通过名称）。</summary>
        public float GetFloat(string name)             { return GetFloatString(name); }
        /// <summary>获取 Float 类型参数的值（通过哈希 ID）。</summary>
        public float GetFloat(int id)                  { return GetFloatID(id); }
        /// <summary>设置 Float 类型参数的值（通过名称）。</summary>
        public void SetFloat(string name, float value) { SetFloatString(name, value); }
        /// <summary>
        /// 设置 Float 类型参数的值，带缓动效果（通过名称）。
        /// dampTime 控制平滑过渡时间（秒），值为 0 时立即跳变。
        /// deltaTime 通常传入 Time.deltaTime。
        /// </summary>
        public void SetFloat(string name, float value, float dampTime, float deltaTime) { SetFloatStringDamp(name, value, dampTime, deltaTime); }

        /// <summary>设置 Float 类型参数的值（通过哈希 ID）。</summary>
        public void SetFloat(int id, float value)       { SetFloatID(id, value); }
        /// <summary>设置 Float 类型参数的值，带缓动效果（通过哈希 ID）。</summary>
        public void SetFloat(int id, float value, float dampTime, float deltaTime) { SetFloatIDDamp(id, value, dampTime, deltaTime); }

        /// <summary>获取 Bool 类型参数的值（通过名称）。</summary>
        public bool GetBool(string name)                { return GetBoolString(name); }
        /// <summary>获取 Bool 类型参数的值（通过哈希 ID）。</summary>
        public bool GetBool(int id)                     { return GetBoolID(id); }
        /// <summary>设置 Bool 类型参数的值（通过名称）。</summary>
        public void SetBool(string name, bool value)    { SetBoolString(name, value); }
        /// <summary>设置 Bool 类型参数的值（通过哈希 ID）。</summary>
        public void SetBool(int id, bool value)         { SetBoolID(id, value); }

        /// <summary>获取 Int 类型参数的值（通过名称）。</summary>
        public int GetInteger(string name)              { return GetIntegerString(name); }
        /// <summary>获取 Int 类型参数的值（通过哈希 ID）。</summary>
        public int GetInteger(int id)                   { return GetIntegerID(id); }
        /// <summary>设置 Int 类型参数的值（通过名称）。</summary>
        public void SetInteger(string name, int value)  { SetIntegerString(name, value); }

        /// <summary>设置 Int 类型参数的值（通过哈希 ID）。</summary>
        public void SetInteger(int id, int value)       { SetIntegerID(id, value); }

        /// <summary>
        /// 触发 Trigger 类型参数（通过名称）。
        /// Trigger 在被状态机"消费"后会自动重置，不同于需要手动设置的 Bool。
        /// 适合表示"一次性的"事件，如攻击、跳跃、受击。
        /// </summary>
        public void SetTrigger(string name)       { SetTriggerString(name); }

        /// <summary>触发 Trigger 类型参数（通过哈希 ID）。</summary>
        public void SetTrigger(int id)       { SetTriggerID(id); }

        /// <summary>
        /// 手动重置 Trigger 参数（通过名称）。
        /// 通常情况下不需要调用此方法，因为状态机消费 Trigger 后会自动重置。
        /// 只有在需要提前取消触发时使用。
        /// </summary>
        public void ResetTrigger(string name)       { ResetTriggerString(name); }

        /// <summary>手动重置 Trigger 参数（通过哈希 ID）。</summary>
        public void ResetTrigger(int id)       { ResetTriggerID(id); }

        /// <summary>
        /// 检查参数是否由动画曲线控制（通过名称）。
        /// 如果返回 true，则脚本设置的参数值会被动画曲线覆盖。
        /// </summary>
        public bool IsParameterControlledByCurve(string name)     { return IsParameterControlledByCurveString(name); }
        /// <summary>检查参数是否由动画曲线控制（通过哈希 ID）。</summary>
        public bool IsParameterControlledByCurve(int id)          { return IsParameterControlledByCurveID(id); }

        // ============================================================
        // Root Motion（根运动）
        //
        // 根运动是指角色的根节点（Root）的位置/旋转由动画驱动而非手动控制。
        // 用于实现"脚步自然贴合地面"的效果，避免滑步。
        //
        // deltaPosition / deltaRotation：每帧的增量值，可在 OnAnimatorMove 中读取。
        // velocity / angularVelocity：基于增量的速度值。
        // rootPosition / rootRotation：读写根节点的位置/旋转。
        // applyRootMotion：是否启用根运动。
        // ============================================================

        /// <summary>上一帧评估的角色根位置增量（位移）。与 velocity 相关但不同：delta 是帧间差值。</summary>
        extern public Vector3 deltaPosition { get; }
        /// <summary>上一帧评估的角色根旋转增量。</summary>
        extern public Quaternion  deltaRotation { get; }

        /// <summary>当前根位移速度（基于 deltaPosition 计算）。</summary>
        extern public Vector3 velocity { get; }
        /// <summary>当前根角速度（基于 deltaRotation 计算）。</summary>
        extern public Vector3 angularVelocity { get; }

        /// <summary>根节点的世界空间位置。设置此值会覆盖动画驱动的根位置。</summary>
        extern public Vector3 rootPosition
        {
            [NativeMethod("GetAvatarPosition")]
            get;
            [NativeMethod("SetAvatarPosition")]
            set;
        }
        /// <summary>根节点的世界空间旋转。设置此值会覆盖动画驱动的根旋转。</summary>
        extern public Quaternion rootRotation
        {
            [NativeMethod("GetAvatarRotation")]
            get;
            [NativeMethod("SetAvatarRotation")]
            set;
        }
        /// <summary>
        /// 是否启用根运动（Root Motion）。
        /// 启用时，角色的位置/旋转由动画驱动，禁用时需手动控制 Transform。
        /// Humanoid 角色默认使用根运动，Generic 角色按需设置。
        /// </summary>
        extern public bool applyRootMotion
        {
            get;
            set;
        }

        [Obsolete("Animator.linearVelocityBlending is no longer used and has been deprecated.")]
        extern public bool linearVelocityBlending
        {
            get;
            set;
        }

        /// <summary>
        /// 动画是否与物理系统同步 Transform。
        /// 启用后动画更新发生在 FixedUpdate，配合运动学刚体使用。
        /// 适用于物理交互的角色（如物理布娃娃与动画混合）。
        /// </summary>
        extern public bool animatePhysics
        {
            get;
            set;
        }

        /// <summary>Animator 更新模式：Normal（Update）、Fixed（FixedUpdate）、UnscaledTime（不受 timeScale 影响）。</summary>
        extern public AnimatorUpdateMode updateMode
        {
            get;
            set;
        }

        /// <summary>当前角色是否拥有 Transform 层级结构。Optimize Game Object 时可能为 false。</summary>
        extern public bool hasTransformHierarchy
        {
            get;
        }

        extern internal bool allowConstantClipSamplingOptimization
        {
            get;
            set;
        }

        /// <summary>
        /// 由当前播放动画计算出的重力权重。
        /// 值范围 [0, 1]，0=无重力，1=完全受重力影响。
        /// 用于调整角色脚步贴合地面的程度。
        /// </summary>
        extern public float gravityWeight
        {
            get;
        }


        // ============================================================
        // Body Position / Rotation（身体质心位置/旋转）
        //
        // bodyPosition / bodyRotation 表示角色身体的质心（Center of Mass）。
        // 这些值只能在 OnAnimatorIK 回调中修改，否则会打印警告。
        // 修改质心位置可以调整角色的平衡状态。
        // ============================================================

        /// <summary>
        /// 身体质心（COM）在世界空间中的位置。
        /// 只能在 OnAnimatorIK 回调中设置/获取。修改此值可影响角色平衡。
        /// </summary>
        public Vector3 bodyPosition
        {
            get { CheckIfInIKPass(); return bodyPositionInternal; }
            set { CheckIfInIKPass(); bodyPositionInternal = value; }
        }

        extern internal Vector3 bodyPositionInternal
        {
            [NativeMethod("GetBodyPosition")]
            get;
            [NativeMethod("SetBodyPosition")]
            set;
        }

        /// <summary>
        /// 身体质心（COM）在世界空间中的旋转。
        /// 只能在 OnAnimatorIK 回调中设置/获取。
        /// </summary>
        public Quaternion bodyRotation
        {
            get { CheckIfInIKPass(); return bodyRotationInternal; }
            set { CheckIfInIKPass(); bodyRotationInternal = value; }
        }

        extern internal Quaternion bodyRotationInternal
        {
            [NativeMethod("GetBodyRotation")]
            get;
            [NativeMethod("SetBodyRotation")]
            set;
        }

        // ============================================================
        // IK Goals（IK 目标）
        //
        // IK（Inverse Kinematics，反向动力学）允许手动控制肢体末端的位置和旋转。
        // 这些方法必须在 OnAnimatorIK 回调中调用。
        //
        // IK Goal 是最终目标位置（手/脚要达到的点）。
        // IK Hint 是中间关节的提示方向（膝盖/手肘的朝向），帮助 IK 算法确定弯曲方向。
        //
        // 权重范围 [0, 1]：0 = 完全保留原动画姿势，1 = 完全使用 IK 目标值。
        // 对于位置和旋转可以分别控制权重。
        // ============================================================

        /// <summary>获取 IK 目标的世界空间位置。只能在 OnAnimatorIK 中调用。</summary>
        public Vector3 GetIKPosition(AvatarIKGoal goal) {  CheckIfInIKPass(); return GetGoalPosition(goal); }
        extern private Vector3 GetGoalPosition(AvatarIKGoal goal);

        /// <summary>设置 IK 目标的世界空间位置。只能在 OnAnimatorIK 中调用。</summary>
        public void SetIKPosition(AvatarIKGoal goal, Vector3 goalPosition) { CheckIfInIKPass(); SetGoalPosition(goal, goalPosition); }
        extern private void SetGoalPosition(AvatarIKGoal goal, Vector3 goalPosition);

        /// <summary>获取 IK 目标的世界空间旋转。只能在 OnAnimatorIK 中调用。</summary>
        public Quaternion GetIKRotation(AvatarIKGoal goal) { CheckIfInIKPass(); return GetGoalRotation(goal); }
        extern private Quaternion GetGoalRotation(AvatarIKGoal goal);

        /// <summary>设置 IK 目标的世界空间旋转。只能在 OnAnimatorIK 中调用。</summary>
        public void SetIKRotation(AvatarIKGoal goal, Quaternion goalRotation) { CheckIfInIKPass();  SetGoalRotation(goal, goalRotation); }
        extern private void SetGoalRotation(AvatarIKGoal goal, Quaternion goalRotation);

        /// <summary>获取 IK 目标位置的混合权重（0=原动画位置，1=完全 IK 位置）。只能在 OnAnimatorIK 中调用。</summary>
        public float GetIKPositionWeight(AvatarIKGoal goal) { CheckIfInIKPass(); return GetGoalWeightPosition(goal); }
        extern private float GetGoalWeightPosition(AvatarIKGoal goal);

        /// <summary>设置 IK 目标位置的混合权重（0=原动画位置，1=完全 IK 位置）。只能在 OnAnimatorIK 中调用。</summary>
        public void SetIKPositionWeight(AvatarIKGoal goal, float value) { CheckIfInIKPass(); SetGoalWeightPosition(goal, value); }
        extern private void SetGoalWeightPosition(AvatarIKGoal goal, float value);

        /// <summary>获取 IK 目标旋转的混合权重。只能在 OnAnimatorIK 中调用。</summary>
        public float GetIKRotationWeight(AvatarIKGoal goal) { CheckIfInIKPass(); return GetGoalWeightRotation(goal); }
        extern private float GetGoalWeightRotation(AvatarIKGoal goal);

        /// <summary>设置 IK 目标旋转的混合权重。只能在 OnAnimatorIK 中调用。</summary>
        public void SetIKRotationWeight(AvatarIKGoal goal, float value) { CheckIfInIKPass(); SetGoalWeightRotation(goal, value); }
        extern private void SetGoalWeightRotation(AvatarIKGoal goal, float value);

        /// <summary>获取 IK 提示（Hint）位置。用于控制中间关节（膝盖/手肘）的朝向。只能在 OnAnimatorIK 中调用。</summary>
        public Vector3 GetIKHintPosition(AvatarIKHint hint) {  CheckIfInIKPass(); return GetHintPosition(hint); }
        extern private Vector3 GetHintPosition(AvatarIKHint hint);

        /// <summary>设置 IK 提示（Hint）位置。用于控制中间关节的弯曲方向。只能在 OnAnimatorIK 中调用。</summary>
        public void SetIKHintPosition(AvatarIKHint hint, Vector3 hintPosition) { CheckIfInIKPass(); SetHintPosition(hint, hintPosition); }
        extern private void SetHintPosition(AvatarIKHint hint, Vector3 hintPosition);

        /// <summary>获取 IK 提示位置的权重。只能在 OnAnimatorIK 中调用。</summary>
        public float GetIKHintPositionWeight(AvatarIKHint hint) { CheckIfInIKPass(); return GetHintWeightPosition(hint); }
        extern private float GetHintWeightPosition(AvatarIKHint hint);

        /// <summary>设置 IK 提示位置的权重。只能在 OnAnimatorIK 中调用。</summary>
        public void SetIKHintPositionWeight(AvatarIKHint hint, float value) { CheckIfInIKPass(); SetHintWeightPosition(hint, value); }
        extern private void SetHintWeightPosition(AvatarIKHint hint, float value);

        // ============================================================
        // LookAt IK（看向目标 IK）
        //
        // SetLookAtPosition 设置角色要"看"的目标世界坐标。
        // SetLookAtWeight 控制身体各部位参与"看"这个动作的权重。
        //
        // weight 参数说明：
        // - weight:    整体 LookAt 权重（0~1）
        // - bodyWeight: 身体旋转参与度（0=不转体，1=完全转体）
        // - headWeight: 头部旋转参与度
        // - eyesWeight: 眼睛旋转参与度
        // - clampWeight: 角度钳制（0=不钳制，0.5=中间钳制，1=完全钳制）
        //   钳制可以防止头部/眼睛过度扭曲到不自然的程度。
        // ============================================================

        /// <summary>设置 LookAt 目标的世界空间位置。只能在 OnAnimatorIK 中调用。</summary>
        public void SetLookAtPosition(Vector3 lookAtPosition) { CheckIfInIKPass(); SetLookAtPositionInternal(lookAtPosition); }

        [NativeMethod("SetLookAtPosition")]
        extern private void SetLookAtPositionInternal(Vector3 lookAtPosition);

        /// <summary>设置整体 LookAt 权重（其他参数使用默认值）。只能在 OnAnimatorIK 中调用。</summary>
        public void SetLookAtWeight(float weight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, 0.00f, 1.00f, 0.00f, 0.50f);
        }

        /// <summary>设置 LookAt 权重，可指定身体权重。只能在 OnAnimatorIK 中调用。</summary>
        public void SetLookAtWeight(float weight, float bodyWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, 1.00f, 0.00f, 0.50f);
        }

        /// <summary>设置 LookAt 权重，可指定身体和头部权重。只能在 OnAnimatorIK 中调用。</summary>
        public void SetLookAtWeight(float weight, float bodyWeight, float headWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, 0.00f, 0.50f);
        }

        /// <summary>设置 LookAt 权重，可指定身体、头部和眼睛权重。只能在 OnAnimatorIK 中调用。</summary>
        public void SetLookAtWeight(float weight, float bodyWeight, float headWeight, float eyesWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, eyesWeight, 0.50f);
        }

        /// <summary>
        /// 完整参数的 SetLookAtWeight。只能在 OnAnimatorIK 中调用。
        /// </summary>
        /// <param name="weight">LookAt 整体权重（0~1）。</param>
        /// <param name="bodyWeight">身体旋转参与度（0~1）。默认 0.0。</param>
        /// <param name="headWeight">头部旋转参与度（0~1）。默认 1.0。</param>
        /// <param name="eyesWeight">眼睛旋转参与度（0~1）。默认 0.0。</param>
        /// <param name="clampWeight">角度钳制（0~1）。默认 0.5，防止过度扭曲。</param>
        public void SetLookAtWeight(float weight, [DefaultValue("0.0f")] float bodyWeight, [DefaultValue("1.0f")] float headWeight, [DefaultValue("0.0f")] float eyesWeight, [DefaultValue("0.5f")] float clampWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, eyesWeight, clampWeight);
        }

        [NativeMethod("SetLookAtWeight")]
        extern private void SetLookAtWeightInternal(float weight, float bodyWeight, float headWeight, float eyesWeight, float clampWeight);

        /// <summary>
        /// 在 IK Pass 期间设置人形骨骼的局部旋转。
        /// 只能在 OnAnimatorIK 回调中调用。
        /// 用于手动调整骨骼姿态，覆盖动画中的骨骼旋转。
        /// </summary>
        public void SetBoneLocalRotation(HumanBodyBones humanBoneId, Quaternion rotation) { CheckIfInIKPass(); SetBoneLocalRotationInternal(HumanTrait.GetBoneIndexFromMono((int)humanBoneId), rotation); }

        [NativeMethod("SetBoneLocalRotation")]
        extern private void SetBoneLocalRotationInternal(int humanBoneId, Quaternion rotation);

        // ============================================================
        // StateMachineBehaviour（状态机行为）
        //
        // StateMachineBehaviour 是挂载在 Animator Controller 状态上的脚本组件。
        // 它提供状态生命周期回调：OnStateEnter / OnStateUpdate / OnStateExit / OnStateMove / OnStateIK
        // 以及状态机级别回调：OnStateMachineEnter / OnStateMachineExit
        //
        // GetBehaviour / GetBehaviours 用于获取指定类型的 StateMachineBehaviour 实例。
        // ============================================================

        extern private ScriptableObject GetBehaviour([NotNull] Type type);

        /// <summary>获取指定类型的第一个 StateMachineBehaviour 实例。</summary>
        public T GetBehaviour<T>() where T : StateMachineBehaviour { return GetBehaviour(typeof(T)) as T; }

        private static T[] ConvertStateMachineBehaviour<T>(ScriptableObject[] rawObjects) where T : StateMachineBehaviour
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        /// <summary>获取指定类型的所有 StateMachineBehaviour 实例。</summary>
        public T[] GetBehaviours<T>() where T : StateMachineBehaviour
        {
            return ConvertStateMachineBehaviour<T>(InternalGetBehaviours(typeof(T)));
        }

        [FreeFunction(Name = "AnimatorBindings::InternalGetBehaviours", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal ScriptableObject[] InternalGetBehaviours([NotNull] Type type);

        /// <summary>根据状态完整路径哈希和层级，获取该状态上的所有 StateMachineBehaviour。</summary>
        public StateMachineBehaviour[] GetBehaviours(int fullPathHash, int layerIndex)
        {
            return InternalGetBehavioursByKey(fullPathHash, layerIndex, typeof(StateMachineBehaviour)) as StateMachineBehaviour[];
        }

        [FreeFunction(Name = "AnimatorBindings::InternalGetBehavioursByKey", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal ScriptableObject[] InternalGetBehavioursByKey(int fullPathHash, int layerIndex, [NotNull] Type type);

        /// <summary>
        /// 是否启用脚步稳定功能。
        /// 启用后，过渡和混合期间角色的脚部会自动稳定，减少"飘移"感。
        /// 对 Humanoid 角色尤其有用。
        /// </summary>
        extern public bool stabilizeFeet
        {
            get;
            set;
        }

        // ============================================================
        // 层级（Layer）管理
        //
        // Animator 支持多个动画层，每个层可以管理不同身体部位的动画。
        // 典型用法：Layer 0 = 全身基本动画，Layer 1 = 上半身射击动画。
        // 每个层有不同的权重、遮罩(AvatarMask)和混合模式。
        // ============================================================

        /// <summary>Animator Controller 的层级数量。</summary>
        extern public int layerCount
        {
            get;
        }

        /// <summary>获取指定层级的名称。</summary>
        extern public string GetLayerName(int layerIndex);
        /// <summary>根据层级名称获取层级索引。</summary>
        extern public int GetLayerIndex(string layerName);
        /// <summary>获取指定层级的当前权重（0~1）。</summary>
        extern public float GetLayerWeight(int layerIndex);
        /// <summary>设置指定层级的混合权重（0~1）。0=完全不可见，1=完全覆盖。</summary>
        extern public void SetLayerWeight(int layerIndex, float weight);

        // ============================================================
        // 状态信息查询
        //
        // GetCurrentAnimatorStateInfo: 获取当前状态的运行时信息（归一化时间、时长等）
        // GetNextAnimatorStateInfo: 获取过渡目标状态的信息
        // GetAnimatorTransitionInfo: 获取当前过渡的信息
        // IsInTransition: 判断指定层是否正在进行过渡
        //
        // GetCurrentAnimatorClipInfo: 获取当前状态下所有正在播放的 AnimationClip 及其权重
        // ============================================================

        extern private void GetAnimatorStateInfo(int layerIndex, StateInfoIndex stateInfoIndex, out AnimatorStateInfo info);

        /// <summary>获取指定层级的当前状态信息。</summary>
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex)
        {
            AnimatorStateInfo info;
            GetAnimatorStateInfo(layerIndex, StateInfoIndex.CurrentState, out info);
            return info;
        }

        /// <summary>获取指定层级的下一状态（过渡目标）信息。如果没有过渡，返回空信息。</summary>
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex)
        {
            AnimatorStateInfo info;
            GetAnimatorStateInfo(layerIndex, StateInfoIndex.NextState, out info);
            return info;
        }

        extern private void GetAnimatorTransitionInfo(int layerIndex, out AnimatorTransitionInfo info);

        /// <summary>获取指定层级当前过渡的信息。</summary>
        public AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex)
        {
            AnimatorTransitionInfo  info;
            GetAnimatorTransitionInfo(layerIndex, out info);
            return info;
        }

        extern internal int GetAnimatorClipInfoCount(int layerIndex, bool current);

        /// <summary>获取当前状态正在播放的动画剪辑数量。</summary>
        public int GetCurrentAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCount(layerIndex, true);
        }

        /// <summary>获取下一状态正在播放的动画剪辑数量。</summary>
        public int GetNextAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCount(layerIndex, false);
        }

        [FreeFunction(Name = "AnimatorBindings::GetCurrentAnimatorClipInfo", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex);

        [FreeFunction(Name = "AnimatorBindings::GetNextAnimatorClipInfo", HasExplicitThis = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public AnimatorClipInfo[] GetNextAnimatorClipInfo(int layerIndex);

        /// <summary>获取当前状态的动画剪辑信息列表（分配到已有的 List 中以减少 GC 分配）。</summary>
        public void GetCurrentAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(layerIndex, true, clips);
        }

        [FreeFunction(Name = "AnimatorBindings::GetAnimatorClipInfoInternal", HasExplicitThis = true)]
        extern private void GetAnimatorClipInfoInternal(int layerIndex, bool isCurrent, [Out,NotNull] List<AnimatorClipInfo> clips);

        /// <summary>获取下一状态的动画剪辑信息列表（分配到已有的 List 中以减少 GC 分配）。</summary>
        public void GetNextAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(layerIndex, false, clips);
        }

        /// <summary>指定层级是否正在执行状态过渡。</summary>
        extern public bool IsInTransition(int layerIndex);

        // ============================================================
        // 参数系统
        //
        // parameters: 获取控制器的所有参数数组
        // parameterCount: 参数数量
        // GetParameter(index): 获取指定索引的参数信息
        // ============================================================

        /// <summary>获取 Animator Controller 的所有参数数组。</summary>
        public extern AnimatorControllerParameter[] parameters
        {
            [FreeFunction(Name = "AnimatorBindings::GetParameters", HasExplicitThis = true)]
            get;
        }

        /// <summary>Animator Controller 的参数数量。</summary>
        public extern int parameterCount
        {
            get;
        }

        [FreeFunction(Name = "AnimatorBindings::GetParameterInternal", HasExplicitThis = true)]
        private extern AnimatorControllerParameter GetParameterInternal(int index);

        /// <summary>获取指定索引的 AnimatorControllerParameter。超出范围会抛出 IndexOutOfRangeException。</summary>
        public AnimatorControllerParameter GetParameter(int index)
        {
            var parameter = GetParameterInternal(index);
            if ((int)parameter.m_Type == AnimatorControllerParameterTypeConstants.InvalidType)
                throw new IndexOutOfRangeException("Index must be between 0 and " + parameterCount);
            return parameter;
        }

        /// <summary>
        /// 枢轴(Pivot)混合值。
        /// 0% = 混合中心在身体质心(COM)，100% = 混合中心在两脚之间。
        /// 影响角色位移的计算方式，用于脚步贴合地面。
        /// </summary>
        extern public float feetPivotActive
        {
            get;
            set;
        }

        /// <summary>
        /// 枢轴权重。0=质心，1=脚底。
        /// 在脚步贴合地面过程中，重心从质心向脚底转移的比例。
        /// </summary>
        extern public float pivotWeight
        {
            get;
        }

        /// <summary>当前枢轴位置的世界坐标。</summary>
        extern public Vector3 pivotPosition
        {
            get;
        }

        // ============================================================
        // MatchTarget（目标匹配）
        //
        // MatchTarget 用于自动调整角色的位置和旋转，使指定的身体部位
        // (AvatarTarget) 在动画播放到特定进度时到达目标位置。
        //
        // 典型用途：角色跳过一个间隙，手抓住栏杆的精确位置。
        //
        // startNormalizedTime: 开始匹配的归一化时间
        // targetNormalizedTime: 完成匹配的归一化时间
        // completeMatch: 完成后是否维持匹配位置
        // ============================================================

        extern private void MatchTarget(Vector3 matchPosition, Quaternion matchRotation, int targetBodyPart, MatchTargetWeightMask weightMask, float startNormalizedTime, float targetNormalizedTime, bool completeMatch);

        /// <summary>
        /// 启动目标匹配（默认 targetNormalizedTime=1, completeMatch=true）。
        /// 当状态播放到 startNormalizedTime 时开始将 targetBodyPart 移动到 matchPosition/rotation。
        /// </summary>
        public void MatchTarget(Vector3 matchPosition,  Quaternion matchRotation, AvatarTarget targetBodyPart,  MatchTargetWeightMask weightMask, float startNormalizedTime)
        {
            MatchTarget(matchPosition, matchRotation, (int)targetBodyPart, weightMask, startNormalizedTime, 1, true);
        }

        /// <summary>启动目标匹配（默认 completeMatch=true）。</summary>
        public void MatchTarget(Vector3 matchPosition,  Quaternion matchRotation, AvatarTarget targetBodyPart,  MatchTargetWeightMask weightMask, float startNormalizedTime, [DefaultValue("1")] float targetNormalizedTime)
        {
            MatchTarget(matchPosition, matchRotation, (int)targetBodyPart, weightMask, startNormalizedTime, targetNormalizedTime, true);
        }

        /// <summary>启动目标匹配，完整参数版本。</summary>
        public void MatchTarget(Vector3 matchPosition,  Quaternion matchRotation, AvatarTarget targetBodyPart,  MatchTargetWeightMask weightMask, float startNormalizedTime, [DefaultValue("1")] float targetNormalizedTime, [DefaultValue("true")] bool completeMatch)
        {
            MatchTarget(matchPosition, matchRotation, (int)targetBodyPart, weightMask, startNormalizedTime, targetNormalizedTime, completeMatch);
        }

        /// <summary>中断当前的目标匹配。</summary>
        public void InterruptMatchTarget()
        {
            InterruptMatchTarget(true);
        }

        /// <summary>中断当前的目标匹配。completeMatch=true 时，角色保持在当前匹配位置。</summary>
        extern public void InterruptMatchTarget([DefaultValue("true")] bool completeMatch);

        /// <summary>目标匹配是否正在进行中。</summary>
        extern public bool isMatchingTarget
        {
            [NativeMethod("IsMatchingTarget")]
            get;
        }

        // ============================================================
        // 播放控制
        //
        // speed: Animator 的整体播放速度乘数。1=正常，2=两倍速，0=暂停。
        // Play: 立即切换到指定状态（可指定归一化时间偏移）。
        // CrossFade: 平滑过渡到指定状态（使用归一化过渡时间）。
        // CrossFadeInFixedTime: 平滑过渡到指定状态（使用秒为单位的时间）。
        //
        // Play vs CrossFade:
        // - Play 立即切换（适合不需要过渡的场景，如死亡）
        // - CrossFade 做平滑混合（适合大多数状态切换）
        // ============================================================

        /// <summary>Animator 的播放速度乘数。1=正常速度，2=两倍速，0=暂停，负数=倒放。</summary>
        extern public float speed
        {
            get;
            set;
        }

        [Obsolete("ForceStateNormalizedTime is deprecated. Please use Play or CrossFade instead.")]
        public void ForceStateNormalizedTime(float normalizedTime) { Play(0, 0, normalizedTime); }

        /// <summary>
        /// 以固定过渡时间（秒）平滑过渡到指定状态（状态名字符串版本）。
        /// </summary>
        /// <param name="stateName">目标状态名称。</param>
        /// <param name="fixedTransitionDuration">过渡持续时间（秒）。</param>
        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration)
        {
            float normalizedTransitionTime = 0.0f;
            float fixedTimeOffset = 0.0f;
            int layer = -1;
            CrossFadeInFixedTime(StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以固定过渡时间（秒）平滑过渡到指定状态（指定层级）。</summary>
        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer)
        {
            float normalizedTransitionTime = 0.0f;
            float fixedTimeOffset = 0.0f;
            CrossFadeInFixedTime(StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以固定过渡时间（秒）平滑过渡到指定状态（指定层级和时间偏移）。</summary>
        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer, float fixedTimeOffset)
        {
            float normalizedTransitionTime = 0.0f;
            CrossFadeInFixedTime(StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>
        /// 以固定过渡时间（秒）平滑过渡到指定状态（完整参数版本）。
        /// </summary>
        /// <param name="fixedTimeOffset">目标状态的时间偏移（秒）。float.NegativeInfinity 表示使用默认值。</param>
        /// <param name="normalizedTransitionTime">过渡自身的归一化开始时间（通常为0）。</param>
        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, [DefaultValue("-1")] int layer, [DefaultValue("0.0f")] float fixedTimeOffset, [DefaultValue("0.0f")] float normalizedTransitionTime)
        {
            CrossFadeInFixedTime(StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以固定过渡时间（秒）平滑过渡到指定状态（哈希 ID 版本）。</summary>
        public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration, int layer , float fixedTimeOffset)
        {
            float normalizedTransitionTime = 0.0f;
            CrossFadeInFixedTime(stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以固定过渡时间（秒）平滑过渡到指定状态（哈希 ID，指定层级）。</summary>
        public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration, int layer)
        {
            float normalizedTransitionTime = 0.0f;
            float fixedTimeOffset = 0.0f;
            CrossFadeInFixedTime(stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以固定过渡时间（秒）平滑过渡到指定状态（哈希 ID，默认层）。</summary>
        public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration)
        {
            float normalizedTransitionTime = 0.0f;
            float fixedTimeOffset = 0.0f;
            int layer = -1;
            CrossFadeInFixedTime(stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        [FreeFunction(Name = "AnimatorBindings::CrossFadeInFixedTime", HasExplicitThis = true)]
        extern public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration, [DefaultValue("-1")]  int layer , [DefaultValue("0.0f")]  float fixedTimeOffset , [DefaultValue("0.0f")]  float normalizedTransitionTime);

        [FreeFunction(Name = "AnimatorBindings::WriteDefaultValues", HasExplicitThis = true)]
        extern public void WriteDefaultValues();

        /// <summary>
        /// 以归一化过渡时间平滑过渡到指定状态（使用状态名，指定层和时间偏移）。
        /// </summary>
        public void CrossFade(string stateName, float normalizedTransitionDuration, int layer , float normalizedTimeOffset)
        {
            float normalizedTransitionTime = 0.0f;
            CrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以归一化过渡时间平滑过渡到指定状态（指定层）。</summary>
        public void CrossFade(string stateName, float normalizedTransitionDuration, int layer)
        {
            float normalizedTransitionTime = 0.0f;
            float normalizedTimeOffset = float.NegativeInfinity;
            CrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以归一化过渡时间平滑过渡到指定状态（默认层-1）。</summary>
        public void CrossFade(string stateName, float normalizedTransitionDuration)
        {
            float normalizedTransitionTime = 0.0f;
            float normalizedTimeOffset = float.NegativeInfinity;
            int layer = -1;
            CrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以归一化过渡时间平滑过渡到指定状态（完整参数版本）。</summary>
        public void CrossFade(string stateName, float normalizedTransitionDuration, [DefaultValue("-1")]  int layer , [DefaultValue("float.NegativeInfinity")]  float normalizedTimeOffset , [DefaultValue("0.0f")]  float normalizedTransitionTime)
        {
            CrossFade(StringToHash(stateName), normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        [FreeFunction(Name = "AnimatorBindings::CrossFade", HasExplicitThis = true)]
        extern public void CrossFade(int stateHashName, float normalizedTransitionDuration, [DefaultValue("-1")]  int layer , [DefaultValue("0.0f")]  float normalizedTimeOffset , [DefaultValue("0.0f")]  float normalizedTransitionTime);

        /// <summary>以归一化过渡时间平滑过渡到指定状态（哈希 ID 版本）。</summary>
        public void CrossFade(int stateHashName, float normalizedTransitionDuration, int layer , float normalizedTimeOffset)
        {
            float normalizedTransitionTime = 0.0f;
            CrossFade(stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以归一化过渡时间平滑过渡到指定状态（哈希 ID，指定层）。</summary>
        public void CrossFade(int stateHashName, float normalizedTransitionDuration, int layer)
        {
            float normalizedTransitionTime = 0.0f;
            float normalizedTimeOffset = float.NegativeInfinity;
            CrossFade(stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>以归一化过渡时间平滑过渡到指定状态（哈希 ID，默认层）。</summary>
        public void CrossFade(int stateHashName, float normalizedTransitionDuration)
        {
            float normalizedTransitionTime = 0.0f;
            float normalizedTimeOffset = float.NegativeInfinity;
            int layer = -1;
            CrossFade(stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        /// <summary>在固定时间位置播放指定状态（指定层）。</summary>
        public void PlayInFixedTime(string stateName, int layer)
        {
            float fixedTime = float.NegativeInfinity;
            PlayInFixedTime(stateName, layer, fixedTime);
        }

        /// <summary>在固定时间位置播放指定状态（默认层-1）。</summary>
        public void PlayInFixedTime(string stateName)
        {
            float fixedTime = float.NegativeInfinity;
            int layer = -1;
            PlayInFixedTime(stateName, layer, fixedTime);
        }

        /// <summary>在固定时间位置播放指定状态（完整参数版本）。</summary>
        public void PlayInFixedTime(string stateName, [DefaultValue("-1")]  int layer, [DefaultValue("float.NegativeInfinity")] float fixedTime)
        {
            PlayInFixedTime(StringToHash(stateName), layer, fixedTime);
        }

        [FreeFunction(Name = "AnimatorBindings::PlayInFixedTime", HasExplicitThis = true)]
        extern public void PlayInFixedTime(int stateNameHash, [DefaultValue("-1")]  int layer, [DefaultValue("float.NegativeInfinity")] float fixedTime);

        /// <summary>在固定时间位置播放指定状态（哈希 ID，指定层）。</summary>
        public void PlayInFixedTime(int stateNameHash, int layer)
        {
            float fixedTime = float.NegativeInfinity;
            PlayInFixedTime(stateNameHash, layer, fixedTime);
        }

        /// <summary>在固定时间位置播放指定状态（哈希 ID，默认层）。</summary>
        public void PlayInFixedTime(int stateNameHash)
        {
            float fixedTime = float.NegativeInfinity;
            int layer = -1;
            PlayInFixedTime(stateNameHash, layer, fixedTime);
        }

        /// <summary>立即播放指定状态（指定层，归一化时间从指定偏移开始）。</summary>
        public void Play(string stateName, int layer)
        {
            float normalizedTime = float.NegativeInfinity;
            Play(stateName, layer, normalizedTime);
        }

        /// <summary>立即播放指定状态（默认层-1，从头开始）。</summary>
        public void Play(string stateName)
        {
            float normalizedTime = float.NegativeInfinity;
            int layer = -1;
            Play(stateName, layer, normalizedTime);
        }

        /// <summary>
        /// 立即播放指定状态。
        /// </summary>
        /// <param name="normalizedTime">从状态的归一化时间位置开始播放。float.NegativeInfinity 表示从头。</param>
        public void Play(string stateName, [DefaultValue("-1")]  int layer, [DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            Play(StringToHash(stateName), layer, normalizedTime);
        }

        [FreeFunction(Name = "AnimatorBindings::Play", HasExplicitThis = true)]
        extern public void Play(int stateNameHash, [DefaultValue("-1")] int layer, [DefaultValue("float.NegativeInfinity")] float normalizedTime);

        /// <summary>立即播放指定状态（哈希 ID，指定层）。</summary>
        public void Play(int stateNameHash, int layer)
        {
            float normalizedTime = float.NegativeInfinity;
            Play(stateNameHash, layer, normalizedTime);
        }

        /// <summary>立即播放指定状态（哈希 ID，默认层-1）。</summary>
        public void Play(int stateNameHash)
        {
            float normalizedTime = float.NegativeInfinity;
            int layer = -1;
            Play(stateNameHash, layer, normalizedTime);
        }

        /// <summary>重置控制器状态。resetParameters=true 同时重置所有参数到默认值。</summary>
        extern public void ResetControllerState([DefaultValue("true")] bool resetParameters = true);

        /// <summary>为当前状态设置 AvatarTarget 和目标归一化时间。</summary>
        extern public void SetTarget(AvatarTarget targetIndex, float targetNormalizedTime);

        /// <summary>SetTarget 指定的目标位置。</summary>
        extern public Vector3 targetPosition
        {
            get;
        }
        /// <summary>SetTarget 指定的目标旋转。</summary>
        extern public Quaternion targetRotation
        {
            get;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use mask and layers to control subset of transfroms in a skeleton.", true)]
        public bool IsControlled(Transform transform) {return false; }

        /// <summary>（内部）指定 Transform 是否为人形骨骼的一部分。</summary>
        extern internal bool IsBoneTransform(Transform transform);

        /// <summary>Avatar 的根 Transform。对于 Humanoid 是 hips 的父级。</summary>
        extern public Transform avatarRoot
        {
            get;
        }

        /// <summary>
        /// 获取指定人形骨骼对应的 Transform。
        /// 会进行 Avatar 有效性、Humanoid 类型、骨骼索引范围检查。
        /// </summary>
        public Transform GetBoneTransform(HumanBodyBones humanBoneId)
        {
            if (avatar == null)
                throw new InvalidOperationException("Avatar is null.");

            if (!avatar.isValid)
                throw new InvalidOperationException("Avatar is not valid.");

            if (!avatar.isHuman)
                throw new InvalidOperationException("Avatar is not of type humanoid.");

            if (humanBoneId < 0 || humanBoneId >= HumanBodyBones.LastBone)
                throw new IndexOutOfRangeException("humanBoneId must be between 0 and " + HumanBodyBones.LastBone);

            return GetBoneTransformInternal(HumanTrait.GetBoneIndexFromMono((int)humanBoneId));
        }

        [NativeMethod("GetBoneTransform")]
        extern internal Transform GetBoneTransformInternal(int humanBoneId);

        /// <summary>控制此 Animator 组件的裁剪模式（屏幕外行为）。</summary>
        extern public AnimatorCullingMode cullingMode
        {
            get;
            set;
        }

        // ============================================================
        // 录制和回放
        //
        // Animator 支持录制和回放动画数据。
        // StartRecording(frameCount) 开始录制，最多记录 frameCount 帧。
        // StartPlayback() / StopPlayback() 控制回放。
        // playbackTime 控制回放的进度位置。
        // recorderStartTime / recorderStopTime 记录录制的起止时间。
        // ============================================================

        /// <summary>进入回放模式。回放模式下，Animator 播放之前录制的数据而非实时动画。</summary>
        extern public void StartPlayback();
        /// <summary>退出回放模式。</summary>
        extern public void StopPlayback();
        /// <summary>回放模式下的播放进度时间。</summary>
        extern public float playbackTime
        {
            get;
            set;
        }

        /// <summary>进入录制模式，开始录制指定的帧数。</summary>
        extern public void StartRecording(int frameCount);
        /// <summary>退出录制模式。</summary>
        extern public void StopRecording();

        /// <summary>录制数据的开始时间（只读）。set 方法保留为空以避免 API 破坏。</summary>
        public float recorderStartTime
        {
            get { return GetRecorderStartTime(); }
            set {}
        }

        extern private float GetRecorderStartTime();

        /// <summary>录制数据的结束时间（只读）。set 方法保留为空以避免 API 破坏。</summary>
        public float recorderStopTime
        {
            get { return GetRecorderStopTime(); }
            set {}
        }

        extern private float GetRecorderStopTime();

        /// <summary>当前录制模式状态。</summary>
        extern public AnimatorRecorderMode recorderMode
        {
            get;
        }

        /// <summary>控制此 Animator 的 RuntimeAnimatorController（运行时动画控制器）。</summary>
        extern public RuntimeAnimatorController runtimeAnimatorController
        {
            get;
            set;
        }

        /// <summary>Animator 是否绑定了 Playable（有活动的 PlayableGraph）。</summary>
        extern public bool hasBoundPlayables
        {
            [NativeMethod("HasBoundPlayables")]
            get;
        }

        extern internal void ClearInternalControllerPlayable();

        /// <summary>检查指定层级是否有指定哈希的状态。</summary>
        extern public bool HasState(int layerIndex, int stateID);

        /// <summary>
        /// 将字符串转换为整数哈希（CRC32）。
        /// 所有 Animator 参数名/状态名都使用此方法生成的哈希进行匹配。
        /// 性能提示：在 Update 中反复调用 StringToHash 会产生 GC 分配，
        /// 建议在 Start/Awake 中预先计算好哈希并存入变量。
        /// </summary>
        [NativeMethod(Name = "ScriptingStringToCRC32", IsThreadSafe = true)]
        extern public static int StringToHash(string name);

        /// <summary>当前 Animator 使用的 Avatar（人形/通用角色定义）。</summary>
        extern public Avatar avatar
        {
            get;
            set;
        }

        extern internal string GetStats();

        /// <summary>获取此 Animator 关联的 PlayableGraph。</summary>
        public PlayableGraph playableGraph
        {
            get
            {
                PlayableGraph graph = new PlayableGraph();
                GetCurrentGraph(ref graph);
                return graph;
            }
        }

        [FreeFunction(Name = "AnimatorBindings::GetCurrentGraph", HasExplicitThis = true)]
        extern private void GetCurrentGraph(ref PlayableGraph graph);

        /// <summary>检查是否在 IK Pass 中，如果不是且 logWarnings 为 true，则打印警告。</summary>
        private void CheckIfInIKPass()
        {
            if (logWarnings && !IsInIKPass())
                Debug.LogWarning("Setting and getting Body Position/Rotation, IK Goals, Lookat and BoneLocalRotation should only be done in OnAnimatorIK or OnStateIK");
        }

        extern private bool IsInIKPass();

        [FreeFunction(Name = "AnimatorBindings::SetFloatString", HasExplicitThis = true)]
        extern private void SetFloatString(string name, float value);

        [FreeFunction(Name = "AnimatorBindings::SetFloatID", HasExplicitThis = true)]
        extern private void SetFloatID(int id, float value);

        [FreeFunction(Name = "AnimatorBindings::GetFloatString", HasExplicitThis = true)]
        extern private float GetFloatString(string name);
        [FreeFunction(Name = "AnimatorBindings::GetFloatID", HasExplicitThis = true)]
        extern private float GetFloatID(int id);

        [FreeFunction(Name = "AnimatorBindings::SetBoolString", HasExplicitThis = true)]
        extern private void SetBoolString(string name, bool value);
        [FreeFunction(Name = "AnimatorBindings::SetBoolID", HasExplicitThis = true)]
        extern private void SetBoolID(int id, bool value);

        [FreeFunction(Name = "AnimatorBindings::GetBoolString", HasExplicitThis = true)]
        extern private bool GetBoolString(string name);
        [FreeFunction(Name = "AnimatorBindings::GetBoolID", HasExplicitThis = true)]
        extern private bool GetBoolID(int id);

        [FreeFunction(Name = "AnimatorBindings::SetIntegerString", HasExplicitThis = true)]
        extern private void SetIntegerString(string name, int value);
        [FreeFunction(Name = "AnimatorBindings::SetIntegerID", HasExplicitThis = true)]
        extern private void SetIntegerID(int id, int value);

        [FreeFunction(Name = "AnimatorBindings::GetIntegerString", HasExplicitThis = true)]
        extern private int GetIntegerString(string name);
        [FreeFunction(Name = "AnimatorBindings::GetIntegerID", HasExplicitThis = true)]
        extern private int GetIntegerID(int id);

        [FreeFunction(Name = "AnimatorBindings::SetTriggerString", HasExplicitThis = true)]
        extern private void SetTriggerString(string name);
        [FreeFunction(Name = "AnimatorBindings::SetTriggerID", HasExplicitThis = true)]
        extern private void SetTriggerID(int id);

        [FreeFunction(Name = "AnimatorBindings::ResetTriggerString", HasExplicitThis = true)]
        extern private void ResetTriggerString(string name);
        [FreeFunction(Name = "AnimatorBindings::ResetTriggerID", HasExplicitThis = true)]
        extern private void ResetTriggerID(int id);

        [FreeFunction(Name = "AnimatorBindings::IsParameterControlledByCurveString", HasExplicitThis = true)]
        extern private bool IsParameterControlledByCurveString(string name);
        [FreeFunction(Name = "AnimatorBindings::IsParameterControlledByCurveID", HasExplicitThis = true)]
        extern private bool IsParameterControlledByCurveID(int id);

        [FreeFunction(Name = "AnimatorBindings::SetFloatStringDamp", HasExplicitThis = true)]
        extern private void SetFloatStringDamp(string name, float value, float dampTime, float deltaTime);
        [FreeFunction(Name = "AnimatorBindings::SetFloatIDDamp", HasExplicitThis = true)]
        extern private void SetFloatIDDamp(int id, float value, float dampTime, float deltaTime);

        /// <summary>附加动画层是否影响质心(COM)的计算。</summary>
        extern public bool layersAffectMassCenter
        {
            get;
            set;
        }

        /// <summary>左脚底部高度（用于 IK 脚部贴合地面）。</summary>
        extern public float leftFeetBottomHeight
        {
            get;
        }

        /// <summary>右脚底部高度（用于 IK 脚部贴合地面）。</summary>
        extern public float rightFeetBottomHeight
        {
            get;
        }

        [NativeConditional("UNITY_EDITOR")]
        extern internal bool supportsOnAnimatorMove
        {
            [NativeMethod("SupportsOnAnimatorMove")]
            get;
        }

        [NativeConditional("UNITY_EDITOR")]
        extern internal void OnUpdateModeChanged();

        [NativeConditional("UNITY_EDITOR")]
        extern internal void OnCullingModeChanged();

        [NativeConditional("UNITY_EDITOR")]
        extern internal void WriteDefaultPose();

        /// <summary>使用指定 deltaTime 手动更新 Animator。</summary>
        [NativeMethod("UpdateWithDelta")]
        extern public void Update(float deltaTime);

        /// <summary>重新绑定 Animator（重置状态机到初始状态）。</summary>
        public void Rebind() { Rebind(true); }
        extern private void Rebind(bool writeDefaultValues);

        /// <summary>应用默认根运动。在不想覆盖默认根运动逻辑时使用。</summary>
        extern public void ApplyBuiltinRootMotion();

        /// <summary>（编辑器）仅评估状态机，不写入 Transform。</summary>
        [NativeConditional("UNITY_EDITOR")]
        internal void EvaluateController() { EvaluateController(0); }
        extern private void EvaluateController(float deltaTime);

        [NativeConditional("UNITY_EDITOR")]
        internal string GetCurrentStateName(int layerIndex) { return GetAnimatorStateName(layerIndex, true); }

        [NativeConditional("UNITY_EDITOR")]
        internal string GetNextStateName(int layerIndex) { return GetAnimatorStateName(layerIndex, false); }

        [NativeConditional("UNITY_EDITOR")]
        extern private string GetAnimatorStateName(int layerIndex, bool current);

        extern internal string ResolveHash(int hash);

        /// <summary>是否启用日志警告。</summary>
        extern public bool logWarnings
        {
            get;
            set;
        }
        /// <summary>是否触发动画事件（AnimationEvent）。</summary>
        extern public bool fireEvents
        {
            get;
            set;
        }

        [Obsolete("keepAnimatorControllerStateOnDisable is deprecated, use keepAnimatorStateOnDisable instead. (UnityUpgradable) -> keepAnimatorStateOnDisable", false)]
        public bool keepAnimatorControllerStateOnDisable
        {
            get { return keepAnimatorStateOnDisable; }
            set { keepAnimatorStateOnDisable = value;}
        }

        /// <summary>禁用时是否保持 Animator 状态。启用后重新激活时动画从中断处继续。</summary>
        extern public bool keepAnimatorStateOnDisable
        {
            get;
            set;
        }

        /// <summary>禁用时是否将属性重置为默认值。</summary>
        extern public bool writeDefaultValuesOnDisable
        {
            get;
            set;
        }

        [Obsolete("GetVector is deprecated.")]
        public Vector3 GetVector(string name)                     { return Vector3.zero; }
        [Obsolete("GetVector is deprecated.")]
        public Vector3 GetVector(int id)                          { return Vector3.zero; }
        [Obsolete("SetVector is deprecated.")]
        public void SetVector(string name, Vector3 value)         {}
        [Obsolete("SetVector is deprecated.")]
        public void SetVector(int id, Vector3 value)              {}

        [Obsolete("GetQuaternion is deprecated.")]
        public Quaternion GetQuaternion(string name)              { return Quaternion.identity; }
        [Obsolete("GetQuaternion is deprecated.")]
        public Quaternion GetQuaternion(int id)                   { return Quaternion.identity; }
        [Obsolete("SetQuaternion is deprecated.")]
        public void SetQuaternion(string name, Quaternion value)  {}
        [Obsolete("SetQuaternion is deprecated.")]
        public void SetQuaternion(int id, Quaternion value)       {}
    }
}
