// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 AnimationPlayableOutput — PlayableGraph 的输出节点
//
// 📌 作用：
//   将 PlayableGraph 中经过混合/处理的最终 Pose 输出到
//   Animator 组件，驱动角色播放动画。这是 Graph 的"终点"。
//
// 🔑 PlayableGraph 完整数据流：
//
//   输入数据（AnimationClip / 自定义 Job）
//          ↓
//   Playable 节点（ClipPlayable / MixerPlayable / ScriptPlayable）
//          ↓  ← Playable 之间通过 SetInput() 连接
//   PlayableOutput 节点（AnimationPlayableOutput）
//          ↓
//   Animator 组件（最终消费 Pose，驱动 SkinnedMeshRenderer）
//
// 💡 关键理解：
//   - AnimationPlayableOutput 必须绑定到一个 Animator
//   - 每个 PlayableGraph 可以有多个 Output（不同 Animator）
//   - Output 是 Graph 中唯一与场景物体直接交互的部分
//
// 📍 对应 C++ 头文件：Modules/Animation/Director/AnimationPlayableOutput.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
    // ==============================================================
    // AnimationPlayableOutput — 动画 Playable 输出节点 struct
    //
    // 🎯 功能：
    //   作为 PlayableGraph 的"出口"，将处理后的 Pose 数据
    //   发送到指定的 Animator 组件。
    //
    // 🔑 关键 API：
    //   - Create():     创建并绑定到目标 Animator
    //   - SetTarget():  切换目标 Animator
    //   - GetTarget():  获取当前绑定的 Animator
    //
    // 💡 使用方式：
    //   var output = AnimationPlayableOutput.Create(graph, "MyOutput", animator);
    //   output.SetSourcePlayable(mixer);  // 将混合器输出连接到 Output
    //
    // 📍 对应 C++ 头文件：Modules/Animation/Director/AnimationPlayableOutput.h
    // ==============================================================
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationPlayableOutput.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimationPlayableOutput.h")]
    [NativeHeader("Modules/Animation/Animator.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("AnimationPlayableOutputBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationPlayableOutput : IPlayableOutput
    {
        private PlayableOutputHandle m_Handle;

        // ==============================================================
        // Create — 创建动画输出并绑定到 Animator
        //
        // 🎯 步骤：
        //   1. 调用 InternalCreateAnimationOutput 在 C++ 端创建 Output
        //   2. 将返回的 PlayableOutputHandle 包装为 C# struct
        //   3. 立即调用 SetTarget() 绑定到指定 Animator
        //
        // ⚡ 如果创建失败，返回 Null（IsValid() == false）
        // ==============================================================
        public static AnimationPlayableOutput Create(PlayableGraph graph, string name, Animator target)
        {
            PlayableOutputHandle handle;
            if (!AnimationPlayableGraphExtensions.InternalCreateAnimationOutput(ref graph, name, out handle))
                return AnimationPlayableOutput.Null;

            AnimationPlayableOutput output = new AnimationPlayableOutput(handle);
            output.SetTarget(target);

            return output;
        }

        internal AnimationPlayableOutput(PlayableOutputHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOutputOfType<AnimationPlayableOutput>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationPlayableOutput.");
            }

            m_Handle = handle;
        }

        public static AnimationPlayableOutput Null
        {
            get { return new AnimationPlayableOutput(PlayableOutputHandle.Null); }
        }

        public PlayableOutputHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator PlayableOutput(AnimationPlayableOutput output)
        {
            return new PlayableOutput(output.GetHandle());
        }

        public static explicit operator AnimationPlayableOutput(PlayableOutput output)
        {
            return new AnimationPlayableOutput(output.GetHandle());
        }

        public Animator GetTarget()
        {
            return InternalGetTarget(ref m_Handle);
        }

        public void SetTarget(Animator value)
        {
            InternalSetTarget(ref m_Handle, value);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static Animator InternalGetTarget(ref PlayableOutputHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void InternalSetTarget(ref PlayableOutputHandle handle, Animator target);
    }
}
