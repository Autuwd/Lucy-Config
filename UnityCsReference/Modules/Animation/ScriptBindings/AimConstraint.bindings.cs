// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 AimConstraint — 瞄准约束（旋转指向目标）
//
// 📌 作用：
//   让当前对象的旋转（Rotation）指向一个或多个目标。
//   类似 Transform.LookAt()，但支持多源权重混合和世界轴控制。
//
// 🔑 Constraint 系统核心概念：
//   - Sources（源）：一个或多个 Transform，各自有权重
//   - Weight（权重）：约束的总影响力（0=无效果，1=完全约束）
//   - Offset（偏移）：在约束结果上额外叠加的旋转偏移
//   - constraintActive：开关，控制约束是否生效
//   - locked：锁定，编辑器中防止误修改
//
// 💡 AimConstraint vs Transform.LookAt()：
//   - AimConstraint 支持 Blending（多源混合）
//   - AimConstraint 有 WorldUp 类型控制
//   - AimConstraint 是 Component，可在编辑器中使用
//
// 📍 对应 C++ 头文件：Modules/Animation/Constraints/AimConstraint.h
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Animations
{
    // ==============================================================
    // AimConstraint — 瞄准约束组件
    //
    // 🎯 功能：
    //   使对象的指定轴向（aimVector）指向目标位置。
    //   可配置世界"上方向"（upVector）和旋转轴限制。
    //
    // 🔑 关键属性：
    //   - aimVector:       本地的"瞄准方向"（默认 Z 轴正方向）
    //   - upVector:        本地的"上方向"（默认 Y 轴正方向）
    //   - worldUpType:     世界"上方向"的类型
    //   - worldUpVector:   自定义的世界"上方向"向量
    //   - worldUpObject:   作为世界"上方向"参考的 Transform
    //   - rotationAxis:    允许旋转的轴（可组合 Axis.X | Axis.Y | Axis.Z）
    //
    // 💡 WorldUpType 说明：
    //   - SceneUp:        使用场景的 Y 轴（默认）
    //   - ObjectUp:       使用 worldUpObject 的位置
    //   - ObjectRotationUp: 使用 worldUpObject 的旋转
    //   - Vector:         使用自定义 worldUpVector
    //   - None:           不使用上方向（可能导致翻滚）
    //
    // 📍 典型用法：敌人炮塔指向玩家、摄像机跟随目标
    // ==============================================================
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/AimConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class AimConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        // ==============================================================
        // WorldUpType — 世界"上方向"类型枚举
        //
        // 📌 控制 AimConstraint 如何确定"上"的方向。
        //    正确的上方向可以防止瞄准时发生翻滚。
        // ==============================================================
        public enum WorldUpType
        {
            SceneUp,
            ObjectUp,
            ObjectRotationUp,
            Vector,
            None
        }

        AimConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] AimConstraint self);

        // ==============================================================
        // 📌 约束基本属性（继承自 IConstraint）
        //   weight:           约束总权重 [0, 1]（所有 Source 共享）
        //   constraintActive: 约束开关
        //   locked:           锁定（编辑器保护）
        // ==============================================================
        public extern float weight { get; set; }
        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        // ==============================================================
        // 📌 AimConstraint 专有属性
        //   rotationAtRest:   无目标时的默认旋转
        //   rotationOffset:   叠加的旋转偏移
        //   rotationAxis:     允许旋转的轴
        //   aimVector:        本地瞄准方向（对象的前方）
        //   upVector:         本地向上方向
        //   worldUpVector:    世界向上方向向量
        //   worldUpObject:    世界向上参考对象
        //   worldUpType:      世界向上类型
        // ==============================================================
        public extern Vector3 rotationAtRest { get; set; }
        public extern Vector3 rotationOffset { get; set; }
        public extern Axis rotationAxis { get; set; }
        public extern Vector3 aimVector { get; set; }
        public extern Vector3 upVector { get; set; }
        public extern Vector3 worldUpVector { get; set; }
        public extern Transform worldUpObject { get; set; }
        public extern WorldUpType worldUpType { get; set; }

        // ==============================================================
        // 📌 Source 管理（IConstraint 接口实现）
        //
        //   ConstraintSource 包含：
        //     - sourceTransform: 源 Transform
        //     - weight:          该源的权重
        //
        //   AddSource / RemoveSource / GetSource / SetSource：
        //     单个 Source 的增删改查
        //   GetSources / SetSources：
        //     批量读写所有 Source（减少跨 Native 调用）
        // ==============================================================
        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] AimConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull][Out] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources", ThrowsException = true)]
        private static extern void SetSourcesInternal([NotNull] AimConstraint self, [In] List<ConstraintSource> sources);

        public extern int AddSource(ConstraintSource source);

        public void RemoveSource(int index)
        {
            ValidateSourceIndex(index);
            RemoveSourceInternal(index);
        }

        [NativeName("RemoveSource")]
        private extern void RemoveSourceInternal(int index);

        public ConstraintSource GetSource(int index)
        {
            ValidateSourceIndex(index);
            return GetSourceInternal(index);
        }

        [NativeName("GetSource")]
        private extern ConstraintSource GetSourceInternal(int index);

        public void SetSource(int index, ConstraintSource source)
        {
            ValidateSourceIndex(index);
            SetSourceInternal(index, source);
        }

        [NativeName("SetSource")]
        private extern void SetSourceInternal(int index, ConstraintSource source);

        private void ValidateSourceIndex(int index)
        {
            if (sourceCount == 0)
            {
                throw new InvalidOperationException("The AimConstraint component has no sources.");
            }

            if (index < 0 || index >= sourceCount)
            {
                throw new ArgumentOutOfRangeException("index", string.Format("Constraint source index {0} is out of bounds (0-{1}).", index, sourceCount));
            }
        }

        extern void ActivateAndPreserveOffset();
        extern void ActivateWithZeroOffset();
        extern void UserUpdateOffset();

        void IConstraintInternal.ActivateAndPreserveOffset()
        {
            this.ActivateAndPreserveOffset();
        }

        void IConstraintInternal.ActivateWithZeroOffset()
        {
            this.ActivateWithZeroOffset();
        }

        void IConstraintInternal.UserUpdateOffset()
        {
            this.UserUpdateOffset();
        }

        Transform IConstraintInternal.transform
        {
            get { return this.transform; }
        }
    }
}
