// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// Avatar —— 人形动画 Avatar 和骨骼映射定义
//
// 【概述】
// 本文件定义了 Mecanim 人形动画系统的基础数据结构和 Avatar 类。
// 包含从 Unity 标准骨骼映射到具体骨骼的枚举（HumanBodyBones），
// 以及各部位的自由度（DOF）枚举。
//
// 【核心概念】
// - HumanBodyBones：Unity 定义的 55 个人形标准骨骼
// - BodyDof/HeadDof/LegDof/ArmDof/FingerDof：各部位的自由度枚举
// - Dof：内部使用的全局 DOF 序列化索引（所有部位串联）
// - HumanParameter：人体参数（扭曲/拉伸/足间距等）
// - Avatar：Avatar 资源类，存储骨骼映射关系
//
// 【HumanBodyBones 说明】
// Unity 的人形动画使用统一的人类骨骼映射（类似 FBX 的 HumanIK）。
// 任何符合人形结构的模型通过 Avatar 映射后，都能共用同一套动画。
// 映射包含 55 个标准骨骼 + 3 段脊椎（Spine → Chest → UpperChest）。
// ============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEngine
{
    /// <summary>
    /// 躯干自由度（DOF）枚举。
    /// 控制 Spine/Chest/UpperChest 三个脊椎节段的旋转自由度。
    /// </summary>
    public enum BodyDof
    {
        SpineFrontBack = 0,      // 脊柱前后弯曲
        SpineLeftRight,          // 脊柱左右侧弯
        SpineRollLeftRight,      // 脊柱左右旋转
        ChestFrontBack,          // 胸部前后弯曲
        ChestLeftRight,          // 胸部左右侧弯
        ChestRollLeftRight,      // 胸部左右旋转
        UpperChestFrontBack,     // 上胸部前后弯曲
        UpperChestLeftRight,     // 上胸部左右侧弯
        UpperChestRollLeftRight, // 上胸部左右旋转
        LastBodyDof              // 结束标记
    }

    /// <summary>
    /// 头部自由度（DOF）枚举。
    /// 控制颈部/头部/眼睛/下巴的旋转自由度。
    /// </summary>
    public enum HeadDof
    {
        NeckFrontBack = 0,    // 颈部前后弯曲
        NeckLeftRight,        // 颈部左右侧弯
        NeckRollLeftRight,    // 颈部左右旋转
        HeadFrontBack,        // 头部前后弯曲
        HeadLeftRight,        // 头部左右侧弯
        HeadRollLeftRight,    // 头部左右旋转
        LeftEyeDownUp,        // 左眼上下转动
        LeftEyeInOut,         // 左眼左右转动
        RightEyeDownUp,       // 右眼上下转动
        RightEyeInOut,        // 右眼左右转动
        JawDownUp,            // 下巴上下
        JawLeftRight,         // 下巴左右
        LastHeadDof           // 结束标记
    }

    /// <summary>
    /// 腿部自由度（DOF）枚举。
    /// 控制大腿/小腿/脚/脚趾的旋转自由度。
    /// </summary>
    public enum LegDof
    {
        UpperLegFrontBack = 0, // 大腿前后摆动
        UpperLegInOut,         // 大腿内外收展
        UpperLegRollInOut,     // 大腿内外旋转
        LegCloseOpen,          // 膝盖弯曲伸展
        LegRollInOut,          // 小腿旋转
        FootCloseOpen,         // 脚踝弯曲伸展
        FootInOut,             // 脚踝内外翻
        ToesUpDown,            // 脚趾上下
        LastLegDof             // 结束标记
    }

    /// <summary>
    /// 手臂自由度（DOF）枚举。
    /// 控制肩/上臂/前臂/手的旋转自由度。
    /// </summary>
    public enum ArmDof
    {
        ShoulderDownUp = 0,    // 肩部上下
        ShoulderFrontBack,     // 肩部前后
        ArmDownUp,             // 上臂上下摆动
        ArmFrontBack,          // 上臂前后摆动
        ArmRollInOut,          // 上臂旋转
        ForeArmCloseOpen,      // 前臂弯曲伸展
        ForeArmRollInOut,      // 前臂旋转
        HandDownUp,            // 手腕上下
        HandInOut,             // 手腕左右
        LastArmDof             // 结束标记
    }

    /// <summary>
    /// 手指自由度（DOF）枚举。
    /// 控制每个手指关节的弯曲旋转（共 4 DOF）。
    /// </summary>
    public enum FingerDof
    {
        ProximalDownUp = 0,     // 近端指节弯曲
        ProximalInOut,          // 近端指节展开
        IntermediateCloseOpen,  // 中端指节弯曲
        DistalCloseOpen,        // 远端指节弯曲
        LastFingerDof           // 结束标记
    }

    /// <summary>
    /// 人体部位分类枚举。
    /// 用于将 DOF 分组到大的身体区域。
    /// </summary>
    public enum HumanPartDof
    {
        Body = 0,       // 躯干
        Head,           // 头部
        LeftLeg,        // 左腿
        RightLeg,       // 右腿
        LeftArm,        // 左臂
        RightArm,       // 右臂
        LeftThumb,      // 左手拇指
        LeftIndex,      // 左手食指
        LeftMiddle,     // 左手中指
        LeftRing,       // 左手无名指
        LeftLittle,     // 左手小指
        RightThumb,     // 右手拇指
        RightIndex,     // 右手食指
        RightMiddle,    // 右手中指
        RightRing,      // 右手无名指
        RightLittle,    // 右手小指
        LastHumanPartDof
    }

    /// <summary>
    /// 内部 — 全局 DOF 序列化索引枚举。
    /// 将各部位的 DOF 排列成连续的线性索引数组，方便 C++ 侧快速访问。
    /// </summary>
    internal enum Dof
    {
        BodyDofStart = 0,
        HeadDofStart = (int)BodyDofStart + (int)BodyDof.LastBodyDof,
        LeftLegDofStart = (int)HeadDofStart + (int)HeadDof.LastHeadDof,
        RightLegDofStart = (int)LeftLegDofStart + (int)LegDof.LastLegDof,
        LeftArmDofStart = (int)RightLegDofStart + (int)LegDof.LastLegDof,
        RightArmDofStart = (int)LeftArmDofStart + (int)ArmDof.LastArmDof,

        LeftThumbDofStart = (int)RightArmDofStart + (int)ArmDof.LastArmDof,
        LeftIndexDofStart = (int)LeftThumbDofStart + (int)FingerDof.LastFingerDof,
        LeftMiddleDofStart = (int)LeftIndexDofStart + (int)FingerDof.LastFingerDof,

        LeftRingDofStart = (int)LeftMiddleDofStart + (int)FingerDof.LastFingerDof,
        LeftLittleDofStart = (int)LeftRingDofStart + (int)FingerDof.LastFingerDof,

        RightThumbDofStart = (int)LeftLittleDofStart + (int)FingerDof.LastFingerDof,
        RightIndexDofStart = (int)RightThumbDofStart + (int)FingerDof.LastFingerDof,
        RightMiddleDofStart = (int)RightIndexDofStart + (int)FingerDof.LastFingerDof,
        RightRingDofStart = (int)RightMiddleDofStart + (int)FingerDof.LastFingerDof,
        RightLittleDofStart = (int)RightRingDofStart + (int)FingerDof.LastFingerDof,

        LastDof = (int)RightLittleDofStart + (int)FingerDof.LastFingerDof
    }

    /// <summary>
    /// HumanBodyBones —— 人形骨骼标准映射枚举。
    ///
    /// 定义了 Unity 人形动画系统的 55 块标准骨骼索引。
    /// 这是 FBX 的 HumanIK 映射在 Unity 中的具体实现。
    ///
    /// 【骨骼层级】
    /// Hips → Spine → Chest → UpperChest → Neck → Head
    ///                                   → Left/Right Shoulder → ...
    ///         → Left/Right UpperLeg → LowerLeg → Foot → Toes
    ///
    /// 【手指骨骼】（每只手 5 根 × 3 节 = 15）
    /// Thumb（拇指）: Proximal → Intermediate → Distal
    /// Index（食指）/ Middle（中指）/ Ring（无名指）/ Little（小指）: 同上
    ///
    /// 【注意】
    /// UpperChest 的索引值为 54（在手指骨骼之后），
    /// 这是因为历史原因，UpperChest 是在后来才加入的。
    /// </summary>
    public enum HumanBodyBones
    {
        /// <summary>臀部骨骼（根骨骼）。</summary>
        Hips = 0,
        /// <summary>左大腿骨。</summary>
        LeftUpperLeg = 1,
        /// <summary>右大腿骨。</summary>
        RightUpperLeg = 2,
        /// <summary>左膝盖/小腿骨。</summary>
        LeftLowerLeg = 3,
        /// <summary>右膝盖/小腿骨。</summary>
        RightLowerLeg = 4,
        /// <summary>左脚踝/脚掌骨。</summary>
        LeftFoot = 5,
        /// <summary>右脚踝/脚掌骨。</summary>
        RightFoot = 6,
        /// <summary>第一段脊椎骨。</summary>
        Spine = 7,
        /// <summary>胸部/第二段脊椎骨。</summary>
        Chest = 8,
        /// <summary>上胸部/第三段脊椎骨（54，在手指骨骼之后）。</summary>
        UpperChest = 54,
        /// <summary>颈部骨骼。</summary>
        Neck = 9,
        /// <summary>头部骨骼。</summary>
        Head = 10,
        /// <summary>左肩骨骼。</summary>
        LeftShoulder = 11,
        /// <summary>右肩骨骼。</summary>
        RightShoulder = 12,
        /// <summary>左上臂骨。</summary>
        LeftUpperArm = 13,
        /// <summary>右上臂骨。</summary>
        RightUpperArm = 14,
        /// <summary>左前臂骨（肘关节）。</summary>
        LeftLowerArm = 15,
        /// <summary>右前臂骨（肘关节）。</summary>
        RightLowerArm = 16,
        /// <summary>左手腕/手掌骨。</summary>
        LeftHand = 17,
        /// <summary>右手腕/手掌骨。</summary>
        RightHand = 18,
        /// <summary>左脚趾骨。</summary>
        LeftToes = 19,
        /// <summary>右脚趾骨。</summary>
        RightToes = 20,
        /// <summary>左眼骨骼。</summary>
        LeftEye = 21,
        /// <summary>右眼骨骼。</summary>
        RightEye = 22,
        /// <summary>下巴骨骼。</summary>
        Jaw = 23,

        /// <summary>左手拇指近端指节。</summary>
        LeftThumbProximal = 24,
        /// <summary>左手拇指中端指节。</summary>
        LeftThumbIntermediate = 25,
        /// <summary>左手拇指远端指节。</summary>
        LeftThumbDistal = 26,

        /// <summary>左手食指近端指节。</summary>
        LeftIndexProximal = 27,
        /// <summary>左手食指中端指节。</summary>
        LeftIndexIntermediate = 28,
        /// <summary>左手食指远端指节。</summary>
        LeftIndexDistal = 29,

        /// <summary>左手中指近端指节。</summary>
        LeftMiddleProximal = 30,
        /// <summary>左手中指中端指节。</summary>
        LeftMiddleIntermediate = 31,
        /// <summary>左手中指远端指节。</summary>
        LeftMiddleDistal = 32,

        /// <summary>左手无名指近端指节。</summary>
        LeftRingProximal = 33,
        /// <summary>左手无名指中端指节。</summary>
        LeftRingIntermediate = 34,
        /// <summary>左手无名指远端指节。</summary>
        LeftRingDistal = 35,

        /// <summary>左手小指近端指节。</summary>
        LeftLittleProximal = 36,
        /// <summary>左手小指中端指节。</summary>
        LeftLittleIntermediate = 37,
        /// <summary>左手小指远端指节。</summary>
        LeftLittleDistal = 38,

        /// <summary>右手拇指近端指节。</summary>
        RightThumbProximal = 39,
        /// <summary>右手拇指中端指节。</summary>
        RightThumbIntermediate = 40,
        /// <summary>右手拇指远端指节。</summary>
        RightThumbDistal = 41,

        /// <summary>右手食指近端指节。</summary>
        RightIndexProximal = 42,
        /// <summary>右手食指中端指节。</summary>
        RightIndexIntermediate = 43,
        /// <summary>右手食指远端指节。</summary>
        RightIndexDistal = 44,

        /// <summary>右手中指近端指节。</summary>
        RightMiddleProximal = 45,
        /// <summary>右手中指中端指节。</summary>
        RightMiddleIntermediate = 46,
        /// <summary>右手中指远端指节。</summary>
        RightMiddleDistal = 47,

        /// <summary>右手无名指近端指节。</summary>
        RightRingProximal = 48,
        /// <summary>右手无名指中端指节。</summary>
        RightRingIntermediate = 49,
        /// <summary>右手无名指远端指节。</summary>
        RightRingDistal = 50,

        /// <summary>右手小指近端指节。</summary>
        RightLittleProximal = 51,
        /// <summary>右手小指中端指节。</summary>
        RightLittleIntermediate = 52,
        /// <summary>右手小指远端指节。</summary>
        RightLittleDistal = 53,

        // UpperChest = 54 (定义在上面)

        /// <summary>最后一块骨骼的索引分隔符。</summary>
        LastBone = 55
    }

    /// <summary>
    /// 内部 — 人体参数枚举。
    /// 控制骨骼扭曲补偿、拉伸和足间距等参数。
    /// </summary>
    internal enum  HumanParameter
    {
        /// <summary>上臂扭曲补偿。</summary>
        UpperArmTwist = 0,
        /// <summary>前臂扭曲补偿。</summary>
        LowerArmTwist,
        /// <summary>大腿扭曲补偿。</summary>
        UpperLegTwist,
        /// <summary>小腿扭曲补偿。</summary>
        LowerLegTwist,
        /// <summary>手臂拉伸允许量。</summary>
        ArmStretch,
        /// <summary>腿部拉伸允许量。</summary>
        LegStretch,
        /// <summary>脚间距调整量。</summary>
        FeetSpacing
    }

    /// <summary>
    /// Avatar —— 人形动画的骨骼映射资源。
    ///
    /// Avatar 定义了模型骨骼与 Unity 标准人形骨骼（HumanBodyBones）的映射关系。
    /// 每个使用人形动画的模型都必须有一个有效的 Avatar 才能进行动画重定向。
    ///
    /// 【创建方式】
    /// - 导入 FBX/模型时在 Rig 页签选择 "Humanoid" 自动生成
    /// - 在 Configure Avatar 界面手动配置骨骼映射
    /// - 运行时不可创建（构造函数为 private）
    ///
    /// 【关键属性】
    /// - isValid：是否为有效的 Avatar（通用或人形均可）
    /// - isHuman：是否为有效的人形 Avatar（支持动画重定向）
    ///
    /// 【内部方法】
    /// 提供骨骼旋转/轴长/限幅等低级查询，用于 HumanTrait 工具类。
    /// </summary>
    [NativeHeader("Modules/Animation/Avatar.h")]
    [UsedByNativeCode]
    public class Avatar : Object
    {
        /// <summary>私有构造 — Avatar 只能由 Unity 内部创建（导入时生成）。</summary>
        private Avatar()
        {
        }

        /// <summary>是否有效（通用 Avatar 或人形 Avatar 均返回 true）。</summary>
        extern public bool isValid
        {
            [NativeMethod("IsValid")]
            get;
        }

        /// <summary>是否为有效的人形 Avatar（支持动画重定向/Retargeting）。</summary>
        extern public bool isHuman
        {
            [NativeMethod("IsHuman")]
            get;
        }

        /// <summary>获取 HumanDescription 数据（包含骨骼映射、Skeleton 定义等）。</summary>
        extern public HumanDescription humanDescription
        {
            get;
        }

        /// <summary>内部 — 设置肌肉的最小/最大范围。</summary>
        extern internal void SetMuscleMinMax(int muscleId, float min, float max);

        /// <summary>内部 — 设置人体参数值。</summary>
        extern internal void SetParameter(int parameterId, float value);

        /// <summary>获取骨骼轴长度（用于肌肉空间计算）。</summary>
        internal float GetAxisLength(int humanId)
        {
            return Internal_GetAxisLength(HumanTrait.GetBoneIndexFromMono(humanId));
        }

        /// <summary>获取骨骼在父空间中的预旋转（骨骼创建时的初始旋转偏移）。</summary>
        internal Quaternion GetPreRotation(int humanId)
        {
            return Internal_GetPreRotation(HumanTrait.GetBoneIndexFromMono(humanId));
        }

        /// <summary>获取骨骼的后旋转（骨骼创建后的旋转偏移修正）。</summary>
        internal Quaternion GetPostRotation(int humanId)
        {
            return Internal_GetPostRotation(HumanTrait.GetBoneIndexFromMono(humanId));
        }

        /// <summary>获取 ZY 顺序后四元数（用于肌肉空间解算的中间步骤）。</summary>
        internal Quaternion GetZYPostQ(int humanId, Quaternion parentQ, Quaternion q)
        {
            return Internal_GetZYPostQ(HumanTrait.GetBoneIndexFromMono(humanId), parentQ, q);
        }

        /// <summary>获取 ZY 顺序的旋转量（用于肌肉空间解算）。</summary>
        internal Quaternion GetZYRoll(int humanId, Vector3 uvw)
        {
            return Internal_GetZYRoll(HumanTrait.GetBoneIndexFromMono(humanId), uvw);
        }

        /// <summary>获取限幅符号向量（用于 DOF 限幅计算）。</summary>
        internal Vector3 GetLimitSign(int humanId)
        {
            return Internal_GetLimitSign(HumanTrait.GetBoneIndexFromMono(humanId));
        }

        [NativeMethod("GetAxisLength")]
        extern internal float Internal_GetAxisLength(int humanId);

        [NativeMethod("GetPreRotation")]
        extern internal Quaternion Internal_GetPreRotation(int humanId);

        [NativeMethod("GetPostRotation")]
        extern internal Quaternion Internal_GetPostRotation(int humanId);

        [NativeMethod("GetZYPostQ")]
        extern internal Quaternion Internal_GetZYPostQ(int humanId, Quaternion parentQ, Quaternion q);

        [NativeMethod("GetZYRoll")]
        extern internal Quaternion Internal_GetZYRoll(int humanId, Vector3 uvw);

        [NativeMethod("GetLimitSign")]
        extern internal Vector3 Internal_GetLimitSign(int humanId);
    }
}
