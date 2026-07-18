// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 AnimationClipPlayable — 将 AnimationClip 封装为 Playable 节点
//
// 📌 作用：
//   将 AnimationClip（动画剪辑）封装为 PlayableGraph 中的一个 Playable 节点，
//   使得动画剪辑可以被 Playable 系统驱动、混合和控制。
//
// 🏗 核心概念：
//   AnimationClipPlayable = AnimationClip 的 Playable 封装
//   PlayableHandle       = 指向 C++ 端 Playable 的轻量级句柄
//   PlayableGraph        = 动画节点的有向无环图（DAG）
//
// 💡 理解关键：
//   这是一个 struct（值类型），内部持有 PlayableHandle。
//   通过 static Create(PlayableGraph, AnimationClip) 工厂方法创建。
//   支持 Foot IK 和 Playable IK 控制，以及循环/采样率设置。
//
// 📍 对应 C++ 头文件：Modules/Animation/Director/AnimationClipPlayable.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
    // ==============================================================
    // AnimationClipPlayable — 动画剪辑的 Playable 封装
    //
    // 🎯 功能：
    //   将 AnimationClip 包装为 Playable 节点，接入 PlayableGraph。
    //   这是 Playable 系统中"叶子节点"的典型代表——它不混合其他输入，
    //   而是直接消费一个 AnimationClip 并输出 Pose。
    //
    // 🔑 关键属性：
    //   - GetAnimationClip():    获取关联的 AnimationClip
    //   - ApplyFootIK:           是否应用脚部 IK（反向动力学）
    //   - ApplyPlayableIK:       是否应用 Playable IK（手动 IK）
    //   - LoopTime:              是否循环播放
    //   - SampleRate:            采样率
    //
    // 💡 PlayableGraph 中的数据流：
    //   AnimationClipPlayable（叶子节点，产出 Pose）
    //   → AnimationMixerPlayable（中间节点，混合多个 Pose）
    //   → AnimationPlayableOutput（输出节点，驱动 Animator）
    //
    // 📍 对应 C++ 头文件：Modules/Animation/Director/AnimationClipPlayable.h
    // ==============================================================
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationClipPlayable.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimationClipPlayable.h")]
    [StaticAccessor("AnimationClipPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationClipPlayable : IPlayable, IEquatable<AnimationClipPlayable>
    {
        PlayableHandle m_Handle;

        // ==============================================================
        // Create — 创建 AnimationClipPlayable 实例
        //
        // 🎯 调用 C++ 端 CreateHandleInternal 创建原生 Playable 对象，
        //    返回的 PlayableHandle 包装为 C# 的 struct。
        // ⚡ 如果创建失败，返回的 Playable 是无效的（IsValid() == false）。
        // ==============================================================
        public static AnimationClipPlayable Create(PlayableGraph graph, AnimationClip clip)
        {
            var handle = CreateHandle(graph, clip);
            return new AnimationClipPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, AnimationClip clip)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, clip, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        internal AnimationClipPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationClipPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationClipPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationClipPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationClipPlayable(Playable playable)
        {
            return new AnimationClipPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationClipPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // ==============================================================
        // GetAnimationClip — 获取关联的 AnimationClip
        //
        // 🎯 返回此 Playable 正在播放的动画剪辑资源。
        // ==============================================================
        public AnimationClip GetAnimationClip()
        {
            return GetAnimationClipInternal(ref m_Handle);
        }

        // ==============================================================
        // 💡 ApplyFootIK — 脚部反向动力学控制
        //
        // 📌 Foot IK：让角色脚部根据地面自动调整位置，防止脚穿地。
        //    Humanoid 动画专用，Generic 动画无效。
        //
        // 📌 Playable IK：在 Playable 层手动控制的 IK，需要额外实现。
        // ==============================================================
        public bool GetApplyFootIK()
        {
            return GetApplyFootIKInternal(ref m_Handle);
        }

        public void SetApplyFootIK(bool value)
        {
            SetApplyFootIKInternal(ref m_Handle, value);
        }

        public bool GetApplyPlayableIK()
        {
            return GetApplyPlayableIKInternal(ref m_Handle);
        }

        public void SetApplyPlayableIK(bool value)
        {
            SetApplyPlayableIKInternal(ref m_Handle, value);
        }

        // ==============================================================
        // ⚡ RemoveStartOffset — 移除动画起始偏移
        //
        // 📌 默认情况下，动画从第一帧开始播放。
        //    开启此选项后，动画会从当前时间偏移处开始，
        //    用于混合时保持各层动画的同步。
        // ==============================================================
        internal bool GetRemoveStartOffset()
        {
            return GetRemoveStartOffsetInternal(ref m_Handle);
        }

        internal void SetRemoveStartOffset(bool value)
        {
            SetRemoveStartOffsetInternal(ref m_Handle, value);
        }

        // ==============================================================
        // 💡 LoopTime — 循环控制
        //
        // 📌 OverrideLoopTime：是否覆盖 AnimationClip 自身的循环设置
        // 📌 LoopTime：循环开关（仅在 OverrideLoopTime=true 时生效）
        //
        // 🎯 典型用法：在混合树中，某些动画需要单次播放（如受击），
        //    某些需要循环播放（如待机），通过此设置控制。
        // ==============================================================
        internal bool GetOverrideLoopTime()
        {
            return GetOverrideLoopTimeInternal(ref m_Handle);
        }

        internal void SetOverrideLoopTime(bool value)
        {
            SetOverrideLoopTimeInternal(ref m_Handle, value);
        }

        internal bool GetLoopTime()
        {
            return GetLoopTimeInternal(ref m_Handle);
        }

        internal void SetLoopTime(bool value)
        {
            SetLoopTimeInternal(ref m_Handle, value);
        }

        // ==============================================================
        // SampleRate — 采样率控制
        //
        // 📌 控制动画的播放采样率（帧/秒）。
        //    降低采样率可以节省性能，但会降低动画平滑度。
        // ==============================================================
        internal float GetSampleRate()
        {
            return GetSampleRateInternal(ref m_Handle);
        }

        internal void SetSampleRate(float value)
        {
            SetSampleRateInternal(ref m_Handle, value);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateHandleInternal(PlayableGraph graph, AnimationClip clip, ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimationClip GetAnimationClipInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetApplyFootIKInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetApplyFootIKInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetApplyPlayableIKInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetApplyPlayableIKInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetRemoveStartOffsetInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetRemoveStartOffsetInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetOverrideLoopTimeInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetOverrideLoopTimeInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetLoopTimeInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetLoopTimeInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static float GetSampleRateInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetSampleRateInternal(ref PlayableHandle handle, float value);
    }
}
