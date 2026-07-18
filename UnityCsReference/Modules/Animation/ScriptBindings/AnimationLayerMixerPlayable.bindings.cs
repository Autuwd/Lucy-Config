// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 AnimationLayerMixerPlayable — 动画层混合器
//
// 📌 作用：
//   将多个动画层（Layer）混合在一起，支持 Additive（叠加）和
//   Override（覆盖）两种混合模式。
//
// 🔑 关键概念：
//   - 每个输入（Input）对应一个动画层
//   - 层可以设为 Additive（叠加模式）或 Override（覆盖模式）
//   - 每层可以绑定 AvatarMask（骨骼遮罩），实现部分身体动画
//   - SingleLayerOptimization：当只有一层时跳过混合计算
//
// 💡 PlayableGraph 数据流：
//   [Idle 动画]──→[Layer 0: Override, FullBody]──┐
//   [Aim 动画]───→[Layer 1: Additive, UpperBody] ─┤
//   [Look 动画]──→[Layer 2: Additive, Head] ─────┤
//                                                  ↓
//                              AnimationLayerMixerPlayable
//                                                  ↓
//                                  AnimationPlayableOutput
//
// 📍 对应 C++ 头文件：Modules/Animation/Director/AnimationLayerMixerPlayable.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    // ==============================================================
    // AnimationLayerMixerPlayable — 动画层混合器 struct
    //
    // 🎯 功能：
    //   接收多个动画 Pose 输入，按层混合输出最终 Pose。
    //   每层可以独立控制混合模式（Additive/Override）和骨骼遮罩。
    //
    // 🔑 关键 API：
    //   - SetLayerAdditive():    将某层设为 Additive/Override 模式
    //   - SetLayerMaskFromAvatarMask(): 为某层设置骨骼遮罩
    //   - IsLayerAdditive():     查询某层的混合模式
    //
    // 💡 单层优化（SingleLayerOptimization）：
    //   当 Graph 中只有一个活动层时，跳过层混合计算直接输出，
    //   是重要的性能优化手段。
    //
    // 📍 对应 C++ 头文件：Modules/Animation/Director/AnimationLayerMixerPlayable.h
    // ==============================================================
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationLayerMixerPlayable.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimationLayerMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationLayerMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationLayerMixerPlayable : IPlayable, IEquatable<AnimationLayerMixerPlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimationLayerMixerPlayable m_NullPlayable = new AnimationLayerMixerPlayable(PlayableHandle.Null);
        public static AnimationLayerMixerPlayable Null { get { return m_NullPlayable; } }

        // ==============================================================
        // Create — 创建层混合器
        //
        // 🎯 两个重载版本：
        //   1. Create(graph, inputCount)：默认启用单层优化
        //   2. Create(graph, inputCount, singleLayerOptimization)：手动控制
        //
        // ⚡ singleLayerOptimization=true 时，如果只有一层活动，
        //   引擎会跳过混合直接输出，提升性能。
        // ==============================================================
        public static AnimationLayerMixerPlayable Create(PlayableGraph graph, int inputCount = 0)
        {
            return Create(graph, inputCount,true);
        }

        public static AnimationLayerMixerPlayable Create(PlayableGraph graph, int inputCount ,bool singleLayerOptimization)
        {
            var handle = CreateHandle(graph, inputCount);
            var mixer = new AnimationLayerMixerPlayable(handle, singleLayerOptimization);
            return mixer;
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, int inputCount = 0)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle))
                return PlayableHandle.Null;
            handle.SetInputCount(inputCount);
            return handle;
        }

        internal AnimationLayerMixerPlayable(PlayableHandle handle, bool singleLayerOptimization = true)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationLayerMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationLayerMixerPlayable.");

                SetSingleLayerOptimizationInternal(ref handle, singleLayerOptimization);
            }
            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationLayerMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationLayerMixerPlayable(Playable playable)
        {
            return new AnimationLayerMixerPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationLayerMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // ==============================================================
        // IsLayerAdditive / SetLayerAdditive — 层的叠加/覆盖模式控制
        //
        // 🎯 Additive 模式：动画叠加在底层之上（如"挥手"叠加在"行走"上）
        //    适合：上半身瞄准、头部追踪等部分身体动画
        // 📌 Override 模式：动画完全覆盖底层（如"受伤"替换"待机"）
        //    适合：全状态切换、动作替换
        //
        // ⚡ 参数验证：layerIndex 必须在 [0, inputCount) 范围内
        // ==============================================================
        public bool IsLayerAdditive(uint layerIndex)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            return IsLayerAdditiveInternal(ref m_Handle, layerIndex);
        }

        public void SetLayerAdditive(uint layerIndex, bool value)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            SetLayerAdditiveInternal(ref m_Handle, layerIndex, value);
        }

        // ==============================================================
        // SetLayerMaskFromAvatarMask — 为层设置骨骼遮罩
        //
        // 📌 AvatarMask 指定该层影响哪些骨骼：
        //    例如：上半身层只影响 Spine→Chest→Head→Arms
        //          下半身层只影响 Hips→Legs→Feet
        //
        // 💡 典型用法：角色射击时——
        //   Layer 0：全身行走动画（Override）
        //   Layer 1：上半身瞄准射击（Additive + AvatarMask(UpperBody)）
        // ==============================================================
        public void SetLayerMaskFromAvatarMask(uint layerIndex, AvatarMask mask)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            if (mask == null)
                throw new System.ArgumentNullException("mask");

            SetLayerMaskFromAvatarMaskInternal(ref m_Handle, layerIndex, mask);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static bool IsLayerAdditiveInternal(ref PlayableHandle handle, uint layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetLayerAdditiveInternal(ref PlayableHandle handle, uint layerIndex, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetSingleLayerOptimizationInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetLayerMaskFromAvatarMaskInternal(ref PlayableHandle handle, uint layerIndex, AvatarMask mask);
    }
}
