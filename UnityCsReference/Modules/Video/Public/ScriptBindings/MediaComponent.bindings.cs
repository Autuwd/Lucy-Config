// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 VideoPlayback — 视频播放底层实例（内部）
//
// 📌 作用：
//   VideoPlayback 是 Unity 内部视频播放的核心类。
//   它代表一个正在播放或准备播放的视频实例。
//   由 VideoPlaybackMgr 创建和管理生命周期。
//
// 🏗 核心概念：
//   VideoPlayback 封装了平台原生视频解码器的句柄。
//   所有方法直接映射到 C++ MediaComponent 的操作。
//   这是 VideoPlayer 组件底层的"真正干活"的对象。
//
// 💡 理解关键：
//   这是 internal 类，对用户不可见。
//   VideoPlayer 内部持有 VideoPlayback 引用。
//   平台编解码支持通过 PlatformSupportsH264/H265 查询。
//
// ⚠️ 平台限制：
//   H.264 — 几乎所有平台支持
//   H.265 — Windows/macOS/iOS 支持，部分 Android 不支持
//   具体支持格式取决于平台原生解码器
//
// 📍 对应 C++ 头文件：Modules/Video/Public/Base/MediaComponent.h
// ==============================================================

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Experimental.Audio;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("VideoTesting")]
[assembly: InternalsVisibleTo("Unity.Audio.DSPGraph")]

namespace UnityEngineInternal.Video
{
    // ==============================================================
    // 🎯 VideoError — 视频播放错误码
    //
    // 📌 由 C++ 端返回，指示播放失败的原因：
    //   NoErr、OutOfMemoryErr、CantReadFile、CantWriteFile
    //   BadParams、NoData、BadPermissions
    //   DeviceNotAvailable、ResourceNotAvailable、NetworkErr
    // ==============================================================
    [UsedByNativeCode]
    internal enum VideoError
    {
        NoErr                = 0,
        OutOfMemoryErr       = 1,
        CantReadFile         = 2,
        CantWriteFile        = 3,
        BadParams            = 4,
        NoData               = 5,
        BadPermissions       = 6,
        DeviceNotAvailable   = 7,
        ResourceNotAvailable = 8,
        NetworkErr           = 9
    }

    // ==============================================================
    // 🎯 VideoPixelFormat — 视频像素格式
    //
    // 💡 RGB/RGBA — 直接存储，GPU 友好
    //     YUV/YUVA — 压缩存储，需要 Shader 转换
    //     大多数视频解码输出 YUV，Unity 自动转换到 RGB
    // ==============================================================
    [UsedByNativeCode]
    internal enum VideoPixelFormat
    {
        RGB  = 0,
        RGBA = 1,
        YUV  = 2,
        YUVA = 3
    }

    // ==============================================================
    // 🎯 VideoAlphaLayout — 视频 Alpha 通道布局
    //
    // 💡 Native — Alpha 合并在像素中
    //     Split  — Alpha 单独通道（用于高质量透明视频）
    // ==============================================================
    [UsedByNativeCode]
    [VisibleToOtherModules("UnityEditor.MediaModule")]
    internal enum VideoAlphaLayout
    {
        Native,
        Split
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/Video/Public/Base/MediaComponent.h")]
    internal class VideoPlayback
    {
        internal IntPtr m_Ptr;

        private VideoPlayback(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        // ==============================================================
        // 📌 播放控制
        //
        // 🎯 StartPlayback/PausePlayback/StopPlayback — 播放/暂停/停止
        // 🎯 GetStatus                   — 获取当前状态（VideoError）
        // 🎯 IsReady                     — 是否已准备好解码
        // 🎯 IsPlaying                   — 是否正在播放
        // ==============================================================
        extern public void StartPlayback();
        extern public void PausePlayback();
        extern public void StopPlayback();

        extern public VideoError GetStatus();
        extern public bool IsReady();
        extern public bool IsPlaying();

        // ==============================================================
        // 📌 逐帧控制
        //
        // 🎯 Step()    — 前进一帧
        // 🎯 CanStep() — 是否支持逐帧前进
        // ==============================================================
        extern public void Step();
        extern public bool CanStep();

        // ==============================================================
        // 📌 视频元数据查询
        //
        // 🎯 GetWidth/Height/FrameRate/Duration/FrameCount
        // 🎯 GetPixelAspectRatioNumerator/Denominator
        // 🎯 GetPixelFormat — 像素格式（RGB/YUV 等）
        // ==============================================================
        extern public uint GetWidth();
        extern public uint GetHeight();
        extern public float GetFrameRate();
        extern public float GetDuration();
        extern public ulong GetFrameCount();
        extern public uint GetPixelAspectRatioNumerator();
        extern public uint GetPixelAspectRatioDenominator();
        extern public VideoPixelFormat GetPixelFormat();

        // ==============================================================
        // 📌 帧控制与纹理获取
        //
        // 🎯 CanNotSkipOnDrop/SetSkipOnDrop/GetSkipOnDrop — 跳帧策略
        // 🎯 GetTexture  — 将当前帧写入 Texture 对象
        //                  outputFrameNum 返回当前帧索引
        // ==============================================================
        extern public bool CanNotSkipOnDrop();
        extern public void SetSkipOnDrop(bool skipOnDrop);
        extern public bool GetSkipOnDrop();
        extern public bool GetTexture(Texture texture, out long outputFrameNum);

        // ==============================================================
        // 📌 跳转
        //
        // 🎯 SeekToFrame — 跳转到指定帧索引
        // 🎯 SeekToTime  — 跳转到指定时间（秒）
        // 两者都需要 seekCompletedCallback 回调通知完成
        // ==============================================================
        public delegate void Callback();
        extern public void SeekToFrame(long frameIndex, Callback seekCompletedCallback);
        extern public void SeekToTime(double secs, Callback seekCompletedCallback);

        // ==============================================================
        // 📌 播放属性
        //
        // 🎯 GetPlaybackSpeed/SetPlaybackSpeed — 播放速度
        // 🎯 GetLoop/SetLoop                   — 循环播放
        // 🎯 SetAdjustToLinearSpace            — 线性空间色彩校正
        // ==============================================================
        extern public float GetPlaybackSpeed();
        extern public void SetPlaybackSpeed(float value);
        extern public bool GetLoop();
        extern public void SetLoop(bool value);

        extern public void SetAdjustToLinearSpace(bool enable);

        // ==============================================================
        // 🎵 音频轨道
        //
        // 🎯 GetAudioTrackCount/ChannelCount/SampleRate/LanguageCode
        // 🎯 SetAudioTarget — 配置音轨输出目标
        //                     enabled: 是否启用
        //                     softwareOutput: 软件输出（vs 硬件解码直出）
        //                     audioSource: 目标 AudioSource
        // 🎯 GetAudioSampleProvider — 获取 AudioSampleProvider
        //                            用于 APIOnly 模式手动获取音频数据
        // ==============================================================
        [NativeHeader("Modules/Audio/Public/AudioSource.h")]
        extern public UInt16 GetAudioTrackCount();
        extern public UInt16 GetAudioChannelCount(UInt16 trackIdx);
        extern public UInt32 GetAudioSampleRate(UInt16 trackIdx);
        extern public string GetAudioLanguageCode(UInt16 trackIdx);
        extern public void SetAudioTarget(UInt16 trackIdx, bool enabled, bool softwareOutput, AudioSource audioSource);
        extern private UInt32 GetAudioSampleProviderId(UInt16 trackIndex);
        public AudioSampleProvider GetAudioSampleProvider(ushort trackIndex)
        {
            if (trackIndex >= GetAudioTrackCount())
                throw new ArgumentOutOfRangeException(
                    "trackIndex", trackIndex,
                    "VideoPlayback has " + GetAudioTrackCount() + " tracks.");

            var provider = AudioSampleProvider.Lookup(GetAudioSampleProviderId(trackIndex), null, trackIndex);

            if (provider == null)
                throw new InvalidOperationException(
                    "VideoPlayback.GetAudioSampleProvider got null provider.");

            if (provider.owner != null)
                throw new InvalidOperationException(
                    "Internal error: VideoPlayback.GetAudioSampleProvider got unexpected non-null provider owner.");

            if (provider.trackIndex != trackIndex)
                throw new InvalidOperationException(
                    "Internal error: VideoPlayback.GetAudioSampleProvider got provider for track " +
                    provider.trackIndex + " instead of " + trackIndex);

            return provider;
        }

        // ==============================================================
        // ⚡ 平台编解码支持查询
        //
        // 📌 PlatformSupportsH264() — 当前平台是否支持 H.264 解码
        //    PlatformSupportsH265() — 当前平台是否支持 H.265/HEVC 解码
        //
        // 💡 H.264 几乎所有平台支持。
        //    H.265 在 Windows 10+、macOS、iOS 上支持，
        //    但在 Android 上因授权问题支持不完整。
        // ==============================================================
        extern static internal bool PlatformSupportsH264();
        extern static internal bool PlatformSupportsH265();

        internal static class BindingsMarshaller
        {
            public static VideoPlayback ConvertToManaged(IntPtr ptr) => new VideoPlayback(ptr);
            public static IntPtr ConvertToNative(VideoPlayback videoPlayback) => videoPlayback.m_Ptr;
        }
    }
}

