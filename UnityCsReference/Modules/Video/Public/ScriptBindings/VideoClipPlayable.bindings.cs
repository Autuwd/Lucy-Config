// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 VideoClipPlayable — Playables 系统中的视频播放节点
//
// 📌 作用：
//   VideoClipPlayable 是一个 Playable，允许将视频剪辑
//   集成到 Unity 的 Playables 系统中（Timeline、自定义图）。
//   它是 struct 类型，包装了一个 PlayableHandle。
//
// 🏗 核心概念：
//   Playable 是 Unity 高级时间轴系统的核心抽象。
//   VideoClipPlayable = 播放视频的 Playable 节点。
//   通过 PlayableGraph 连接视频输出到渲染/音频系统。
//
// 💡 理解关键：
//   这是一个 struct（值类型），但 m_Handle 指向 Native 端。
//   支持：播放/暂停/跳转/循环/延迟播放/变速。
//   Seek() 方法封装了 StartDelay + Duration + PauseDelay 逻辑。
//
// 📍 对应 C++ 头文件：
//   Modules/Video/Public/ScriptBindings/VideoClipPlayable.bindings.h
//   Modules/Video/Public/Director/VideoClipPlayable.h
// ==============================================================

using System;
using System.ComponentModel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;
using UnityEngine.Video;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Experimental.Video
{
    [NativeHeader("Modules/Video/Public/ScriptBindings/VideoClipPlayable.bindings.h")]
    [NativeHeader("Modules/Video/Public/Director/VideoClipPlayable.h")]
    [NativeHeader("Modules/Video/Public/VideoClip.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("VideoClipPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct VideoClipPlayable : IPlayable, IEquatable<VideoClipPlayable>
    {
        PlayableHandle m_Handle;

        // ==============================================================
        // 🎯 Create — 在 PlayableGraph 中创建视频播放节点
        //
        // 📌 参数：
        //   graph   — 所属的 PlayableGraph
        //   clip    — 要播放的视频资源（null 则播放空节点）
        //   looping — 是否循环播放
        //
        // 💡 创建后自动设置 Duration 为 clip.length。
        // ==============================================================
        public static VideoClipPlayable Create(PlayableGraph graph, VideoClip clip, bool looping)
        {
            var handle = CreateHandle(graph, clip, looping);
            var playable = new VideoClipPlayable(handle);
            if (clip != null)
                playable.SetDuration(clip.length);
            return playable;
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, VideoClip clip, bool looping)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateVideoClipPlayable(ref graph, clip, looping, ref handle))
                return PlayableHandle.Null;
            return handle;
        }

        // ==============================================================
        // 📌 Handle 管理 — Playable 生命周期
        //
        // 🎯 GetHandle() — 获取底层 PlayableHandle
        // 🎯 隐式/显式转换 — VideoClipPlayable ↔ Playable
        // 🎯 Equals() — 比较两个 PlayableHandle 是否相同
        //
        // 💡 构造函数执行类型检查：
        //    handle.IsPlayableOfType<VideoClipPlayable>() 确保类型安全。
        // ==============================================================
        internal VideoClipPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<VideoClipPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an VideoClipPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(VideoClipPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator VideoClipPlayable(Playable playable)
        {
            return new VideoClipPlayable(playable.GetHandle());
        }

        public bool Equals(VideoClipPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // ==============================================================
        // 📌 播放属性控制
        //
        // 🎯 GetClip/SetClip   — 获取/设置播放的 VideoClip
        // 🎯 GetLooped/SetLooped — 获取/设置循环模式
        // 🎯 IsPlaying           — 是否正在播放
        // ==============================================================
        public VideoClip GetClip()
        {
            return GetClipInternal(ref m_Handle);
        }

        public void SetClip(VideoClip value)
        {
            SetClipInternal(ref m_Handle, value);
        }

        public bool GetLooped()
        {
            return GetLoopedInternal(ref m_Handle);
        }

        public void SetLooped(bool value)
        {
            SetLoopedInternal(ref m_Handle, value);
        }

        public bool IsPlaying()
        {
            return GetIsPlayingInternal(ref m_Handle);
        }

        // ==============================================================
        // 📌 延迟播放控制
        //
        // 🎯 GetStartDelay/SetStartDelay — 开始延迟（等待多久开始）
        // 🎯 GetPauseDelay/SetPauseDelay — 暂停延迟（播多久后暂停）
        //
        // ⚠️ 设置延迟时有校验逻辑：
        //   如果当前延迟太小（< 0.05s），系统来不及反应。
        //   FIXME 注释中提到 0.5 是随意值，应基于采样率和 DSP buffer。
        // ==============================================================
        public double GetStartDelay()
        {
            return GetStartDelayInternal(ref m_Handle);
        }

        internal void SetStartDelay(double value)
        {
            ValidateStartDelayInternal(value);
            SetStartDelayInternal(ref m_Handle, value);
        }

        public double GetPauseDelay()
        {
            return GetPauseDelayInternal(ref m_Handle);
        }

        internal void GetPauseDelay(double value)
        {
            double currentDelay = GetPauseDelayInternal(ref m_Handle);
            if (m_Handle.GetPlayState() == PlayState.Playing &&
                (value < 0.05 || (currentDelay != 0.0 && currentDelay < 0.05)))
                throw new ArgumentException("VideoClipPlayable.pauseDelay: Setting new delay when existing delay is too small or 0.0 ("
                    + currentDelay + "), Video system will not be able to change in time");

            SetPauseDelayInternal(ref m_Handle, value);
        }

        // ==============================================================
        // 🎯 Seek — 跳转到指定时间播放
        //
        // 📌 参数：
        //   startTime  — 跳转到的目标时间（秒）
        //   startDelay — 跳转后的播放延迟
        //   duration   — 播放持续时间（0=播放到结束）
        //
        // 💡 实现逻辑：
        //   1. 设置 StartDelay
        //   2. 如果 duration > 0，计算结束时间并设 PauseDelay
        //   3. 否则设置无限时长
        //   4. SetTime + Play
        // ==============================================================
        public void Seek(double startTime, double startDelay)
        {
            Seek(startTime, startDelay, 0);
        }

        public void Seek(double startTime, double startDelay, [DefaultValue("0")] double duration)
        {
            ValidateStartDelayInternal(startDelay);
            SetStartDelayInternal(ref m_Handle, startDelay);
            if (duration > 0)
            {
                m_Handle.SetDuration(duration + startTime);
                SetPauseDelayInternal(ref m_Handle, startDelay + duration);
            }
            else
            {
                m_Handle.SetDuration(double.MaxValue);
                SetPauseDelayInternal(ref m_Handle, 0);
            }

            m_Handle.SetTime(startTime);
            m_Handle.Play();
        }

        // ==============================================================
        // ⚠️ ValidateStartDelay — 延迟校验
        //
        // 📌 如果正在播放且设置的延迟太小，发出警告。
        //    因为视频系统需要最小延迟时间来做好解码准备。
        // ==============================================================
        private void ValidateStartDelayInternal(double startDelay)
        {
            double currentDelay = GetStartDelayInternal(ref m_Handle);

            const double validEndDelay = 0.05;
            const double validStartDelay = 0.00001;

            if (IsPlaying() &&
                (startDelay < validEndDelay || (currentDelay >= validStartDelay && currentDelay < validEndDelay)))
            {
                Debug.LogWarning("VideoClipPlayable.StartDelay: Setting new delay when existing delay is too small or 0.0 ("
                    + currentDelay + "), Video system will not be able to change in time");
            }
        }

        // ==============================================================
        // ⚡ Native 方法声明 — 对应 C++ VideoClipPlayableBindings
        //
        // 📌 所有方法标记 [NativeMethod(ThrowsException = true)]
        //   表示 C++ 端可能抛异常。
        //
        // 📌 InternalCreateVideoClipPlayable — 在 C++ 端创建 Playable
        // 📌 ValidateType — 验证 PlayableHandle 是否为 VideoClipPlayable
        // ==============================================================
        [NativeMethod(ThrowsException = true)]
        extern private static VideoClip GetClipInternal(ref PlayableHandle hdl);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetClipInternal(ref PlayableHandle hdl, VideoClip clip);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetLoopedInternal(ref PlayableHandle hdl);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetLoopedInternal(ref PlayableHandle hdl, bool looped);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetIsPlayingInternal(ref PlayableHandle hdl);

        [NativeMethod(ThrowsException = true)]
        extern private static double GetStartDelayInternal(ref PlayableHandle hdl);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetStartDelayInternal(ref PlayableHandle hdl, double delay);

        [NativeMethod(ThrowsException = true)]
        extern private static double GetPauseDelayInternal(ref PlayableHandle hdl);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetPauseDelayInternal(ref PlayableHandle hdl, double delay);

        [NativeMethod(ThrowsException = true)]
        extern private static bool InternalCreateVideoClipPlayable(ref PlayableGraph graph, VideoClip clip, bool looping, ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static bool ValidateType(ref PlayableHandle hdl);

    }
}
