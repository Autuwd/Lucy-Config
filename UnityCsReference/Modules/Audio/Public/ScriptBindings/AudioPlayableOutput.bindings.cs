// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// 🎯 AudioPlayableOutput —— PlayableGraph 的音频输出节点
//     将 PlayableGraph 的音频数据路由到 AudioSource
// 💡 Create(graph, name, target) 指定目标 AudioSource
// 💡 EvaluateOnSeek 控制 Seek 时是否重新评估 DSP
// ⚡ 每次 Evaluate() 触发 DSP 管线处理
// 📌 IPlayableOutput 结构体，支持隐式转换为 PlayableOutput
// ============================================================
using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Audio
{
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioPlayableOutput.bindings.h")]
    [NativeHeader("Modules/Audio/Public/Director/AudioPlayableOutput.h")]
    [NativeHeader("Modules/Audio/Public/AudioSource.h")]
    [StaticAccessor("AudioPlayableOutputBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AudioPlayableOutput : IPlayableOutput
    {
        private PlayableOutputHandle m_Handle;

        public static AudioPlayableOutput Create(PlayableGraph graph, string name, AudioSource target)
        {
            PlayableOutputHandle handle;
            if (!AudioPlayableGraphExtensions.InternalCreateAudioOutput(ref graph, name, out handle))
                return AudioPlayableOutput.Null;

            AudioPlayableOutput output = new AudioPlayableOutput(handle);
            output.SetTarget(target);

            return output;
        }

        internal AudioPlayableOutput(PlayableOutputHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOutputOfType<AudioPlayableOutput>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AudioPlayableOutput.");
            }

            m_Handle = handle;
        }

        public static AudioPlayableOutput Null
        {
            get { return new AudioPlayableOutput(PlayableOutputHandle.Null); }
        }

        public PlayableOutputHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator PlayableOutput(AudioPlayableOutput output)
        {
            return new PlayableOutput(output.GetHandle());
        }

        public static explicit operator AudioPlayableOutput(PlayableOutput output)
        {
            return new AudioPlayableOutput(output.GetHandle());
        }


        public AudioSource GetTarget()
        {
            return InternalGetTarget(ref m_Handle);
        }

        public void SetTarget(AudioSource value)
        {
            InternalSetTarget(ref m_Handle, value);
        }

        public bool GetEvaluateOnSeek()
        {
            return InternalGetEvaluateOnSeek(ref m_Handle);
        }

        public void SetEvaluateOnSeek(bool value)
        {
            InternalSetEvaluateOnSeek(ref m_Handle, value);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static AudioSource InternalGetTarget(ref PlayableOutputHandle output);

        [NativeMethod(ThrowsException = true)]
        extern private static void InternalSetTarget(ref PlayableOutputHandle output, AudioSource target);

        [NativeMethod(ThrowsException = true)]
        extern private static bool InternalGetEvaluateOnSeek(ref PlayableOutputHandle output);

        [NativeMethod(ThrowsException = true)]
        extern private static void InternalSetEvaluateOnSeek(ref PlayableOutputHandle output, bool value);

    }
}
