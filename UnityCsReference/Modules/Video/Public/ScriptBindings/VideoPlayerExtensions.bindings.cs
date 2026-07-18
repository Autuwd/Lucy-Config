// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 VideoPlayerExtensions — VideoPlayer 扩展方法
//
// 📌 作用：
//   VideoPlayerExtensions 为 VideoPlayer 提供了 GetAudioSampleProvider
//   扩展方法，用于在 APIOnly 模式下获取音频样本提供者。
//
// 🏗 核心概念：
//   AudioSampleProvider 是 Unity 音频系统的底层 API，
//   允许开发者直接访问音频样本数据进行自定义处理。
//   需要 VideoPlayer.audioOutputMode = APIOnly 才能使用。
//
// 💡 理解关键：
//   APIOnly 模式下的音频不会自动路由到扬声器。
//   开发者必须通过 AudioSampleProvider 手动获取并处理音频数据。
//   适用于：音频可视化、自定义 DSP 效果、录音等场景。
//
// 📍 对应 C++ 头文件：
//   Modules/Video/Public/ScriptBindings/VideoPlayerExtensions.bindings.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Video;
using UnityEngine.Experimental.Audio;

namespace UnityEngine.Experimental.Video
{
    [NativeHeader("Modules/Video/Public/ScriptBindings/VideoPlayerExtensions.bindings.h")]
    [NativeHeader("Modules/Video/Public/VideoPlayer.h")]
    [NativeHeader("VideoScriptingClasses.h")]
    [StaticAccessor("VideoPlayerExtensionsBindings", StaticAccessorType.DoubleColon)]
    public static class VideoPlayerExtensions
    {
        // ==============================================================
        // 🎯 GetAudioSampleProvider — 获取音频样本提供者
        //
        // 📌 前置条件：
        //   1. VideoPlayer.audioOutputMode == APIOnly
        //   2. trackIndex < VideoPlayer.controlledAudioTrackCount
        //
        // 💡 AudioSampleProvider 提供：
        //   - sampleFramesAvailable 事件（新样本就绪时触发）
        //   - sampleFramesOverflow 事件（缓冲区溢出时触发）
        //   - ConsumeSampleFrames() 方法消耗样本
        //
        // ⚠️ 这是实验性 API（Experimental 命名空间），
        //    未来版本可能变更或移除。
        // ==============================================================
        public static AudioSampleProvider GetAudioSampleProvider(this VideoPlayer vp, ushort trackIndex)
        {
            var count = vp.controlledAudioTrackCount;
            if (trackIndex >= count)
                throw new ArgumentOutOfRangeException(
                    "trackIndex", trackIndex,
                    "VideoPlayer is currently configured with " + count + " tracks.");

            var mode = vp.audioOutputMode;
            if (mode != VideoAudioOutputMode.APIOnly)
                throw new InvalidOperationException(
                    "VideoPlayer.GetAudioSampleProvider requires audioOutputMode to be APIOnly. " +
                    "Current: " + mode);

            var provider = AudioSampleProvider.Lookup(
                vp.InternalGetAudioSampleProviderId(trackIndex), vp, trackIndex);

            if (provider == null)
                throw new InvalidOperationException(
                    "VideoPlayer.GetAudioSampleProvider got null provider.");

            if (provider.owner != vp)
                throw new InvalidOperationException(
                    "Internal error: VideoPlayer.GetAudioSampleProvider got provider used by another object.");

            if (provider.trackIndex != trackIndex)
                throw new InvalidOperationException(
                    "Internal error: VideoPlayer.GetAudioSampleProvider got provider for track " +
                    provider.trackIndex + " instead of " + trackIndex);

            return provider;
        }

        // ==============================================================
        // ⚡ Native 方法 — 获取 AudioSampleProvider 的内部 ID
        //    由 C++ 端的 VideoPlayerExtensionsBindings 实现
        // ==============================================================
        extern internal static uint InternalGetAudioSampleProviderId([NotNull] this VideoPlayer vp, ushort trackIndex);
    }
}

