// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 AnimationMixerPlayable — 动画混合器（基础版）
//
// 📌 作用：
//   将多个动画输入按照权重混合为一个 Pose 输出。
//   这是最基础的混合器，不区分层、不做遮罩。
//
// 🔑 与 AnimationLayerMixerPlayable 的区别：
//   AnimationMixerPlayable：        简单权重混合，没有层的概念
//   AnimationLayerMixerPlayable：   分层混合，支持 Additive/Override/AvatarMask
//
// 💡 PlayableGraph 数据流示例：
//   [奔跑] ──┐
//   [走路] ──┤
//   [待机] ──┼── AnimationMixerPlayable ──→ AnimationPlayableOutput
//   [受伤] ──┘        ↑ 权重控制
//                 Blend Tree 根据参数调整各输入权重
//
// 📍 对应 C++ 头文件：Modules/Animation/Director/AnimationMixerPlayable.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    // ==============================================================
    // AnimationMixerPlayable — 基础动画混合器 struct
    //
    // 🎯 功能：
    //   接收多个动画 Pose 输入，加权混合输出单一 Pose。
    //   常用于 Animator Controller 中的 Blend Tree（混合树）。
    //
    // 🔑 关键特性：
    //   - 所有输入无差别混合（不分区层）
    //   - 权重由外部（Blend Tree 或自定义逻辑）控制
    //   - normalizeWeights 参数已废弃——引擎内部总是自动归一化
    //
    // 💡 使用场景：
    //   角色根据移动速度在"待机→走路→奔跑"之间平滑过渡。
    //   每个输入是一个 AnimationClipPlayable，它们的权重由速度参数决定。
    //
    // 📍 对应 C++ 头文件：Modules/Animation/Director/AnimationMixerPlayable.h
    // ==============================================================
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationMixerPlayable.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimationMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationMixerPlayable : IPlayable, IEquatable<AnimationMixerPlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimationMixerPlayable m_NullPlayable = new AnimationMixerPlayable(PlayableHandle.Null);
        public static AnimationMixerPlayable Null { get { return m_NullPlayable; } }

        // ==============================================================
        // Create — 创建混合器
        //
        // ⚡ normalizeWeights 已废弃（v1 遗留），现在引擎总是自动归一化权重。
        //    在旧版本中需要手动设置，现已无效。使用无此参数的重载即可。
        // ==============================================================
        [Obsolete("normalizeWeights is obsolete. It has no effect and will be removed.")]
        public static AnimationMixerPlayable Create(PlayableGraph graph, int inputCount, bool normalizeWeights)
        {
            return Create(graph, inputCount);
        }

        public static AnimationMixerPlayable Create(PlayableGraph graph, int inputCount = 0)
        {
            var handle = CreateHandle(graph, inputCount);
            return new AnimationMixerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, int inputCount = 0)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle))
                return PlayableHandle.Null;
            handle.SetInputCount(inputCount);
            return handle;
        }

        internal AnimationMixerPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationMixerPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationMixerPlayable(Playable playable)
        {
            return new AnimationMixerPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);
    }
}
