// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 LookAtConstraint — 注视约束（面向目标）
//
// 📌 作用：
//   使当前对象的 Z 轴正方向（前方向）指向目标位置。
//   类似 Transform.LookAt() 的 Component 版本，支持多源混合。
//
// 🔑 AimConstraint vs LookAtConstraint：
//   AimConstraint：可自定义 aimVector（本地前方向），更灵活
//   LookAtConstraint：固定使用 Z 轴，但有 roll（翻滚）控制
//
// 💡 典型场景：
//   - 角色的眼睛看向某对象
//   - 摄像机注视目标
//   - 灯光的聚光灯方向
//
// 📍 对应 C++ 头文件：Modules/Animation/Constraints/LookAtConstraint.h
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Animations
{
    // ==============================================================
    // LookAtConstraint — 注视约束组件
    //
    // 🎯 功能：
    //   使对象的 Z 轴指向目标，类似 Transform.LookAt() 的组件化版本。
    //   支持多源加权混合和翻滚控制。
    //
    // 🔑 关键属性：
    //   - roll:          Z 轴翻滚角度（围绕 Z 轴旋转）
    //   - rotationAtRest: 无目标时的默认旋转
    //   - rotationOffset: 叠加的旋转偏移
    //   - worldUpObject:  上方向参考对象
    //   - useUpObject:    是否使用上方向参考对象
    //
    // 💡 roll 的作用：
    //   当注视目标时，对象 Z 轴指向目标，但 Y 轴方向不确定。
    //   roll 控制围绕 Z 轴的旋转，固定"上方向"的朝向。
    //
    // 📍 对应 C++ 头文件：Modules/Animation/Constraints/LookAtConstraint.h
    // ==============================================================
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/LookAtConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class LookAtConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        LookAtConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] LookAtConstraint self);

        // ==============================================================
        // 📌 约束基本属性
        // ==============================================================
        public extern float weight { get; set; }
        public extern float roll { get; set; }
        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        // ==============================================================
        // 📌 LookAtConstraint 专有属性
        //   rotationAtRest:  无目标时的默认旋转
        //   rotationOffset:  叠加的旋转偏移
        //   worldUpObject:   上方向参考 Transform
        //   useUpObject:     是否启用上方向参考
        // ==============================================================
        public extern Vector3 rotationAtRest { get; set; }
        public extern Vector3 rotationOffset { get; set; }
        public extern Transform worldUpObject { get; set; }
        public extern bool useUpObject { get; set; }

        // ==============================================================
        // 📌 Source 管理（与 IConstraint 接口一致）
        // ==============================================================
        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] LookAtConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull][Out] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources", ThrowsException = true)]
        private static extern void SetSourcesInternal([NotNull] LookAtConstraint self, [In] List<ConstraintSource> sources);

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
                throw new InvalidOperationException("The LookAtConstraint component has no sources.");
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
