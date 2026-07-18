// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ParentConstraint — 父级约束（位置 + 旋转跟随）
//
// 📌 作用：
//   让当前对象的 Position 和 Rotation 跟随一个或多个目标。
//   这是"多父级"系统——一个对象可以同时受多个父对象影响。
//
// 🔑 ParentConstraint vs 普通父子关系：
//   - 普通父子（Transform.SetParent）：一个对象只能有一个父级
//   - ParentConstraint：多个源有权重混合，支持独立的偏移
//
// 💡 典型场景：
//   - 角色手持武器：武器位置跟随手的骨骼，但可以有偏移
//   - 多平台运动：角色在移动平台上，位置跟随平台混合
//   - 载具座位：角色坐在车上，位置跟随车辆移动
//
// 📍 对应 C++ 头文件：Modules/Animation/Constraints/ParentConstraint.h
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Animations
{
    // ==============================================================
    // ParentConstraint — 多源父级约束组件
    //
    // 🎯 功能：
    //   使对象的位置和旋转跟随多个源 Transform，并按权重混合。
    //   每个源可以有独立的 Translation 和 Rotation 偏移。
    //
    // 🔑 与普通父子关系的区别：
    //   ParentConstraint 的"父级"是逻辑上的，不会改变 Transform
    //   层级结构。对象仍然在场景根节点或原父级下，只是位置/旋转
    //   被约束到目标。
    //
    // 🔑 关键属性：
    //   - translationAtRest:  无源时的默认位置
    //   - rotationAtRest:     无源时的默认旋转
    //   - translationOffsets: 每个源的位置偏移数组
    //   - rotationOffsets:    每个源的旋转偏移数组
    //   - translationAxis:    允许跟随的移动轴
    //   - rotationAxis:       允许跟随的旋转轴
    //
    // 💡 多源混合示例：
    //   角色位于两个平台之间：
    //   Source A（weight=0.7）：左侧平台
    //   Source B（weight=0.3）：右侧平台
    //   角色位置会根据权重在两者之间插值
    //
    // 📍 对应 C++ 头文件：Modules/Animation/Constraints/ParentConstraint.h
    // ==============================================================
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/ParentConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class ParentConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        ParentConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] ParentConstraint self);

        // ==============================================================
        // 📌 基本约束属性
        // ==============================================================
        public extern float weight { get; set; }
        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] ParentConstraint self);

        // ==============================================================
        // 📌 ParentConstraint 专有属性
        //   translationAtRest: 无源时的默认位置（"休息位置"）
        //   rotationAtRest:    无源时的默认旋转
        //   translationOffsets: 每个源的独立位置偏移
        //   rotationOffsets:    每个源的独立旋转偏移
        //   translationAxis:    受约束的移动轴（可组合）
        //   rotationAxis:       受约束的旋转轴（可组合）
        // ==============================================================
        public extern Vector3 translationAtRest { get; set; }
        public extern Vector3 rotationAtRest { get; set; }
        public extern Vector3[] translationOffsets { get; set; }
        public extern Vector3[] rotationOffsets { get; set; }
        public extern Axis translationAxis { get; set; }
        public extern Axis rotationAxis { get; set; }

        // ==============================================================
        // GetTranslationOffset / SetTranslationOffset — 每个源的位置偏移
        //
        // 📌 每个源可以有独立的偏移量，使得约束结果不是简单的
        //    位置跟随，而是"跟随 + 固定偏移"。
        // ==============================================================
        public Vector3 GetTranslationOffset(int index)
        {
            ValidateSourceIndex(index);
            return GetTranslationOffsetInternal(index);
        }

        public void SetTranslationOffset(int index, Vector3 value)
        {
            ValidateSourceIndex(index);
            SetTranslationOffsetInternal(index, value);
        }

        [NativeName("GetTranslationOffset")]
        private extern Vector3 GetTranslationOffsetInternal(int index);
        [NativeName("SetTranslationOffset")]
        private extern void SetTranslationOffsetInternal(int index, Vector3 value);

        // ==============================================================
        // GetRotationOffset / SetRotationOffset — 每个源的旋转偏移
        // ==============================================================
        public Vector3 GetRotationOffset(int index)
        {
            ValidateSourceIndex(index);
            return GetRotationOffsetInternal(index);
        }

        public void SetRotationOffset(int index, Vector3 value)
        {
            ValidateSourceIndex(index);
            SetRotationOffsetInternal(index, value);
        }

        [NativeName("GetRotationOffset")]
        private extern Vector3 GetRotationOffsetInternal(int index);
        [NativeName("SetRotationOffset")]
        private extern void SetRotationOffsetInternal(int index, Vector3 value);

        private void ValidateSourceIndex(int index)
        {
            if (sourceCount == 0)
            {
                throw new InvalidOperationException("The ParentConstraint component has no sources.");
            }

            if (index < 0 || index >= sourceCount)
            {
                throw new ArgumentOutOfRangeException("index", string.Format("Constraint source index {0} is out of bounds (0-{1}).", index, sourceCount));
            }
        }

        // ==============================================================
        // 📌 统一的 Source 管理 API（与 IConstraint 接口一致）
        // ==============================================================
        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull][Out] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources", ThrowsException = true)]
        private static extern void SetSourcesInternal([NotNull] ParentConstraint self, [In] List<ConstraintSource> sources);

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

        extern void ActivateAndPreserveOffset();
        extern void ActivateWithZeroOffset();
        extern void UserUpdateOffset();

        void IConstraintInternal.ActivateAndPreserveOffset()
        {
            ActivateAndPreserveOffset();
        }

        void IConstraintInternal.ActivateWithZeroOffset()
        {
            ActivateWithZeroOffset();
        }

        void IConstraintInternal.UserUpdateOffset()
        {
            UserUpdateOffset();
        }

        Transform IConstraintInternal.transform
        {
            get { return this.transform; }
        }
    }
}
