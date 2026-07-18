// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Constraint System — Unity 约束系统的核心定义
//
// 📌 本文件定义了约束系统的三大核心：
//   1. Axis 枚举：控制约束生效的轴向（X/Y/Z 的组合）
//   2. ConstraintSource struct：约束源（Transform + 权重）
//   3. IConstraint 接口：所有约束的公共 API
//   4. PositionConstraint / RotationConstraint / ScaleConstraint
//
// 🔑 约束系统架构：
//   IConstraint（接口） ← 实现
//   ├── PositionConstraint  — 位置约束
//   ├── RotationConstraint  — 旋转约束
//   ├── ScaleConstraint     — 缩放约束
//   ├── AimConstraint       — 瞄准约束（本目录单独文件）
//   ├── LookAtConstraint    — 注视约束（本目录单独文件）
//   └── ParentConstraint    — 父级约束（本目录单独文件）
//
// 💡 所有约束共享相同的 Source 管理模式：
//   每个约束有 1 到多个 ConstraintSource，每个 Source 有
//   一个 Transform 和一个 float 权重。引擎根据权重混合所有
//   Source 的变换数据，得到最终结果。
//
// 📍 对应 C++ 头文件：
//   Modules/Animation/Constraints/ConstraintEnums.h
//   Modules/Animation/Constraints/ConstraintSource.h
//   Modules/Animation/Constraints/Constraint.bindings.h
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Animations
{
    // ==============================================================
    // Axis — 轴位标志枚举
    //
    // 📌 用于指定约束在哪些轴向上生效。
    //    使用 [Flags] 标记，可以组合：
    //    Axis.X | Axis.Y — 只在 X 和 Y 轴上生效
    //    Axis.None — 所有轴都不生效（相当于禁用）
    //
    // 💡 例如 PositionConstraint.translationAxis = Axis.X | Axis.Z
    //    表示对象只能在 XZ 平面上移动，Y 轴高度固定。
    // ==============================================================
    [NativeHeader("Modules/Animation/Constraints/ConstraintEnums.h")]
    [Flags]
    public enum Axis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4
    }

    // ==============================================================
    // ConstraintSource — 约束源数据 struct
    //
    // 🎯 每个约束操作的对象，包含：
    //   - sourceTransform: 源 Transform（位置/旋转/缩放的参考）
    //   - weight:          该源的权重 [0, 1]
    //
    // 💡 如果有多于一个 Source，引擎计算加权平均值。
    //    例如：两个 Source 各 0.5 权重，结果是它们的中间位置。
    //
    // 📌 对应 C++ 结构体：ConstraintSource（Native 端）
    // ==============================================================
    [System.Serializable]
    [NativeHeader("Modules/Animation/Constraints/ConstraintSource.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    [UsedByNativeCode]
    public struct ConstraintSource
    {
        [NativeName("sourceTransform")]
        private Transform m_SourceTransform;
        [NativeName("weight")]
        private float m_Weight;

        public Transform sourceTransform { get { return m_SourceTransform; } set { m_SourceTransform = value; } }
        public float weight { get { return m_Weight; } set { m_Weight = value; } }
    }

    // ==============================================================
    // IConstraint — 约束公共接口
    //
    // 🎯 所有约束类型（Aim/Parent/LookAt/Position/Rotation/Scale）
    //    都实现此接口，保证统一的 API 访问方式。
    //
    // 📌 接口成员说明：
    //   - weight:           约束总影响力 [0, 1]
    //   - constraintActive: 开关
    //   - locked:           锁定状态（编辑器用）
    //   - sourceCount:      当前 Source 数量
    //   - Add/Remove/Get/SetSource: 单个 Source 操作
    //   - Get/SetSources:   批量 Source 操作
    // ==============================================================
    public interface IConstraint
    {
        float weight { get; set; }

        bool constraintActive { get; set; }
        bool locked { get; set; }

        int sourceCount { get; }

        int AddSource(ConstraintSource source);
        void RemoveSource(int index);
        ConstraintSource GetSource(int index);
        void SetSource(int index, ConstraintSource source);

        void GetSources(List<ConstraintSource> sources);
        void SetSources(List<ConstraintSource> sources);
    }

    // ==============================================================
    // IConstraintInternal — 约束内部接口
    //
    // ⚡ 编辑器使用的内部 API：
    //   - ActivateAndPreserveOffset：激活约束并保持当前偏移
    //   - ActivateWithZeroOffset：激活约束并将偏移清零
    //   - UserUpdateOffset：用户手动更新偏移
    //
    // 📌 这些方法在编辑器中通过 Inspector 操作约束时调用。
    // ==============================================================
    internal interface IConstraintInternal
    {
        void ActivateAndPreserveOffset();
        void ActivateWithZeroOffset();
        void UserUpdateOffset();
        Transform transform { get; }
    }

    // ==============================================================
    // PositionConstraint — 位置约束
    //
    // 🎯 功能：
    //   使对象的位置跟随一个或多个源 Transform 的加权平均位置。
    //   可指定受影响的轴和偏移量。
    //
    // 🔑 关键属性：
    //   - translationAtRest: 无源时的默认位置
    //   - translationOffset:  位置偏移量（在约束结果上叠加）
    //   - translationAxis:    受约束的位置轴（X/Y/Z 可组合）
    //
    // 💡 典型场景：
    //   - 角色站在移动平台上，位置跟随平台
    //   - 多个物体的加权平均位置
    // ==============================================================
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/PositionConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class PositionConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        PositionConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] PositionConstraint self);

        public extern float weight { get; set; }
        public extern Vector3 translationAtRest { get; set; }
        public extern Vector3 translationOffset { get; set; }
        public extern Axis translationAxis { get; set; }
        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] PositionConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull][Out] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources", ThrowsException = true)]
        private static extern void SetSourcesInternal([NotNull] PositionConstraint self, [In] List<ConstraintSource> sources);

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
                throw new InvalidOperationException("The PositionConstraint component has no sources.");
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

    // ==============================================================
    // RotationConstraint — 旋转约束
    //
    // 🎯 功能：
    //   使对象的旋转跟随一个或多个源 Transform 的加权平均旋转。
    //   可指定受影响的旋转轴和旋转偏移。
    //
    // 🔑 关键属性：
    //   - rotationAtRest: 无源时的默认旋转
    //   - rotationOffset:  旋转偏移量
    //   - rotationAxis:    受约束的旋转轴（X/Y/Z 可组合）
    //
    // 💡 与 AimConstraint 的区别：
    //   AimConstraint 根据"指向目标"计算旋转角度，
    //   RotationConstraint 直接复制目标的旋转值。
    // ==============================================================
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/RotationConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class RotationConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        RotationConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] RotationConstraint self);

        public extern float weight { get; set; }
        public extern Vector3 rotationAtRest { get; set; }
        public extern Vector3 rotationOffset { get; set; }
        public extern Axis rotationAxis { get; set; }
        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] RotationConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull][Out] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources", ThrowsException = true)]
        private static extern void SetSourcesInternal([NotNull] RotationConstraint self, [In] List<ConstraintSource> sources);

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
                throw new InvalidOperationException("The RotationConstraint component has no sources.");
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

    // ==============================================================
    // ScaleConstraint — 缩放约束
    //
    // 🎯 功能：
    //   使对象的缩放跟随一个或多个源 Transform 的加权平均缩放。
    //   可指定受影响的缩放轴和偏移量。
    //
    // 🔑 关键属性：
    //   - scaleAtRest:  无源时的默认缩放
    //   - scaleOffset:  缩放偏移量
    //   - scalingAxis:  受约束的缩放轴（X/Y/Z 可组合）
    //
    // 💡 典型场景：
    //   - 角色手持武器的缩放跟随角色缩放
    //   - UI 元素的缩放根据参考对象调整
    // ==============================================================
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/ScaleConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class ScaleConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        ScaleConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] ScaleConstraint self);

        public extern float weight { get; set; }
        public extern Vector3 scaleAtRest { get; set; }
        public extern Vector3 scaleOffset { get; set; }
        public extern Axis scalingAxis { get; set; }
        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] ScaleConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull][Out] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources", ThrowsException = true)]
        private static extern void SetSourcesInternal([NotNull] ScaleConstraint self, [In] List<ConstraintSource> sources);

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
                throw new InvalidOperationException("The ScaleConstraint component has no sources.");
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
