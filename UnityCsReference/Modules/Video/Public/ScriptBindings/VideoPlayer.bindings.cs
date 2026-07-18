// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 VideoPlayer — Unity 视频播放核心组件
//
// 📌 作用：
//   VideoPlayer 是 Unity 中播放视频的主要组件。
//   支持视频来源：VideoClip 资源 / URL 远程视频。
//   支持渲染模式：Camera 远近平面、RenderTexture、材质覆盖、API-only。
//   支持多种时间同步模式：DSPTime、GameTime、UnscaledGameTime。
//
// 🏗 核心概念：
//   VideoSource.VideoClip — 导入的视频资源（支持多种格式）
//   VideoSource.Url      — 远程/本地 URL 视频流
//   VideoRenderMode      — 视频渲染到目标的方式
//   VideoAudioOutputMode — 音频输出方式（AudioSource/Direct/APIOnly）
//
// 💡 理解关键：
//   VideoPlayer 继承自 Behaviour，需要挂载在 GameObject 上。
//   播放 URL 视频时，Unity 使用平台原生解码器。
//   Audio Output Mode 决定音频如何路由到音频系统。
//
// ⚡ 常用事件流：
//   Prepare() → prepareCompleted → Play() → started
//   → loopPointReached（循环点）/ frameDropped（丢帧）
//   → errorReceived（出错）/ seekCompleted（跳转完成）
//
// 📍 对应 C++ 头文件：Modules/Video/Public/VideoPlayer.h
// ==============================================================

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Video
{
    // ==============================================================
    // 🎯 VideoRenderMode — 视频渲染目标模式
    //
    // 📌 各模式说明：
    //   CameraFarPlane/NearPlane — 在 Camera 的远/近裁剪面渲染
    //   RenderTexture            — 渲染到 RenderTexture，供 Shader 使用
    //   MaterialOverride         — 覆盖材质的主纹理
    //   APIOnly                  — 不自动渲染，通过 texture 属性手动控制
    //
    // 💡 最常用的是 RenderTexture 和 MaterialOverride。
    //    Camera 模式适合 UI 背景/过场动画。
    // ==============================================================
    [RequiredByNativeCode]
    public enum VideoRenderMode
    {
        CameraFarPlane   = 0,
        CameraNearPlane  = 1,
        RenderTexture    = 2,
        MaterialOverride = 3,
        APIOnly          = 4
    }

    // ==============================================================
    // 🎯 Video3DLayout — 3D 视频布局格式
    //
    // 📌 用于立体 3D 视频的左右眼排列方式：
    //   No3D          — 普通 2D 视频
    //   SideBySide3D  — 左右排列（左右眼各一半宽度）
    //   OverUnder3D   — 上下排列（左右眼各一半高度）
    //
    // 💡 需要对应的 3D 显示器或 VR 设备才能正常观看。
    // ==============================================================
    [RequiredByNativeCode]
    public enum Video3DLayout
    {
        No3D         = 0,
        SideBySide3D = 1,
        OverUnder3D  = 2
    }

    // ==============================================================
    // 🎯 VideoAspectRatio — 视频画面缩放/裁剪模式
    //
    // 📌 控制视频如何适应显示区域：
    //   NoScaling       — 原始大小，不缩放
    //   FitVertically   — 适配高度（宽度可能被裁剪）
    //   FitHorizontally — 适配宽度（高度可能被裁剪）
    //   FitInside       — 完整显示（可能有黑边，类似 Letterbox）
    //   FitOutside      — 填满区域（内容可能被裁剪，类似 Fill）
    //   Stretch         — 拉伸填满（不保持宽高比）
    // ==============================================================
    [RequiredByNativeCode]
    public enum VideoAspectRatio
    {
        NoScaling       = 0,
        FitVertically   = 1,
        FitHorizontally = 2,
        FitInside       = 3,
        FitOutside      = 4,
        Stretch         = 5
    }

    [RequiredByNativeCode]
    [System.Obsolete("VideoTimeSource is deprecated. Use TimeUpdateMode instead. (UnityUpgradable) -> VideoTimeUpdateMode")]
    public enum VideoTimeSource
    {
        [System.Obsolete("AudioDSPTimeSource is deprecated. Use DSPTime instead. (UnityUpgradable) -> DSPTime")]
        AudioDSPTimeSource = 0,
        [System.Obsolete("GameTimeSource is deprecated. Use GameTime instead. (UnityUpgradable) -> GameTime")]
        GameTimeSource     = 1
    }

    [RequiredByNativeCode]
    public enum VideoTimeReference
    {
        Freerun         = 0,
        InternalTime    = 1,
        ExternalTime    = 2
    }

    // ==============================================================
    // 🎯 VideoSource — 视频来源类型
    //
    // 📌 两种视频来源：
    //   VideoClip — 导入到项目的视频资源（Assets 中的视频文件）
    //               ✅ 支持格式：.mp4、.mov、.webm（平台差异）
    //               ✅ 支持导入设置（压缩、换帧等）
    //
    //   Url       — 远程或本地文件 URL
    //               ✅ 支持 http://、https://、file:// 协议
    //               ⚠️ 平台解码器限制决定支持格式
    //               ⚠️ 需要处理网络缓冲和流式加载
    //
    // 💡 VideoClip 适合小尺寸、频繁使用的视频；
    //    Url 适合大视频、流媒体、动态加载场景。
    // ==============================================================
    [RequiredByNativeCode]
    public enum VideoSource
    {
        VideoClip = 0,
        Url       = 1
    }

    // ==============================================================
    // 🎯 VideoTimeUpdateMode — 视频时间同步模式
    //
    // 📌 控制视频帧的推进时钟源：
    //   DSPTime          — 使用音频 DSP 时钟（音视频同步最佳）
    //   GameTime         — 使用 Time.time（受 Time.timeScale 影响）
    //   UnscaledGameTime — 使用 Time.unscaledTime（不受暂停影响）
    //
    // 💡 音频视频同步推荐 DSPTime。
    //     UI/菜单视频推荐 UnscaledGameTime（暂停时继续播放）。
    // ==============================================================
    [RequiredByNativeCode]
    public enum VideoTimeUpdateMode
    {
        DSPTime          = 0,
        GameTime         = 1,
        UnscaledGameTime = 2
    }

    // ==============================================================
    // 🎯 VideoAudioOutputMode — 视频音频输出模式
    //
    // 📌 音频路由方式：
    //   None        — 不输出音频
    //   AudioSource — 通过 AudioSource 组件输出（支持 3D 音效）
    //   Direct      — 直接输出到音频设备（低延迟）
    //   APIOnly     — 通过 AudioSampleProvider API 手动获取音频数据
    //
    // 💡 AudioSource 模式最常用，支持空间音频。
    //     APIOnly 模式用于自定义音频处理（DSP、音频分析等）。
    // ==============================================================
    [RequiredByNativeCode]
    public enum VideoAudioOutputMode
    {
        None        = 0,
        AudioSource = 1,
        Direct      = 2,
        APIOnly     = 3
    }

    [RequiredByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Video/Public/VideoPlayer.h")]
    public sealed partial class VideoPlayer : Behaviour
    {
        // ==============================================================
        // 📌 视频来源配置 — 选择播放 VideoClip 还是 URL
        //
        // 🎯 source — VideoClip 或 Url
        // 🎯 clip   — 导入的视频资源（source=VideoClip 时使用）
        // 🎯 url    — 远程/本地 URL（source=Url 时使用）
        //
        // ⚡ 切换 source 后需要重新调用 Prepare()。
        // 💡 url 支持 http:// https:// file:// 协议。
        // ==============================================================
        public extern VideoSource source { get; set; }
        public extern VideoTimeUpdateMode timeUpdateMode { get; set; }

        [NativeName("VideoUrl")]
        public extern string url { get; set; }

        [NativeName("VideoClip")]
        public extern VideoClip clip { get; set; }

        // ==============================================================
        // 📌 渲染目标配置 — 视频画面渲染到哪
        //
        // 🎯 renderMode — 渲染模式（Camera/RenderTexture/材质/API）
        // 🎯 targetCamera/RenderTexture/MaterialRenderer — 具体目标
        // 🎯 aspectRatio — 画面缩放模式
        // 🎯 targetCameraAlpha — Camera 模式下的透明度
        // 🎯 texture    — 视频的纹理数据（只读）
        //
        // 💡 renderMode 决定哪个 target 属性有效。
        // ==============================================================
        public extern VideoRenderMode renderMode { get; set; }

        public extern bool canSetTimeUpdateMode
        {
            [NativeName("CanSetTimeUpdateMode")]
            get;
        }

        [NativeHeader("Runtime/Camera/Camera.h")]
        public extern Camera targetCamera { get; set; }

        [NativeHeader("Runtime/Graphics/RenderTexture.h")]
        public extern RenderTexture targetTexture { get; set; }

        [NativeHeader("Runtime/Graphics/Renderer.h")]
        public extern Renderer targetMaterialRenderer { get; set; }

        public extern string targetMaterialProperty { get; set; }

        [VisibleToOtherModules("UnityEditor.VideoModule")]
        internal extern string effectiveTargetMaterialProperty { get; }

        public extern VideoAspectRatio aspectRatio { get; set; }

        public extern float targetCameraAlpha { get; set; }

        public extern Video3DLayout targetCamera3DLayout { get; set; }

        [NativeHeader("Runtime/Graphics/Texture.h")]
        public extern Texture texture { get; }

        // ==============================================================
        // 📌 播放控制 — 生命周期管理
        //
        // 🎯 Prepare()   — 预加载视频（解码准备）
        // 🎯 Play()      — 开始/恢复播放
        // 🎯 Pause()     — 暂停播放
        // 🎯 Stop()      — 停止播放并释放资源
        // 🎯 isPrepared  — 视频是否已准备好播放
        // 🎯 isPlaying   — 是否正在播放
        // 🎯 isPaused    — 是否已暂停
        // 🎯 playOnAwake — 启动时自动播放
        // 🎯 waitForFirstFrame — 等待第一帧准备好再开始
        //
        // ⚡ 完整流程：Prepare → isPrepared=true → Play → started
        // ==============================================================
        public extern void Prepare();

        public extern bool isPrepared
        {
            [NativeName("IsPrepared")]
            get;
        }


        public extern bool waitForFirstFrame { get; set; }

        public extern bool playOnAwake { get; set; }

        public extern void Play();

        public extern void Pause();

        public extern void Stop();

        public extern bool isPlaying
        {
            [NativeName("IsPlaying")]
            get;
        }
        public extern bool isPaused
        {
            [NativeName("IsPaused")]
            get;
        }

        // ==============================================================
        // 📌 时间与定位 — 视频时间轴操作
        //
        // 🎯 time/time/frame — 当前时间/帧位置（可读写）
        // 🎯 canSetTime      — 是否支持设置时间（跳转）
        // 🎯 clockTime       — 底层时钟时间（与音频时钟同步）
        // 🎯 canStep         — 是否支持逐帧前进
        // 🎯 StepForward()   — 前进一帧
        // 🎯 canSetPlaybackSpeed — 是否支持变速
        // 🎯 playbackSpeed   — 播放速度倍率（1.0=正常）
        //
        // ⚡ 并非所有视频格式/平台支持跳转和变速。
        // 💡 frame 是整型帧索引，time 是浮点秒数。
        // ==============================================================
        public extern bool canSetTime
        {
            [NativeName("CanSetTime")]
            get;
        }

        [NativeName("SecPosition")]
        public extern double time { get; set; }

        [NativeName("FramePosition")]
        public extern long frame { get; set; }

        public extern double clockTime { get; }

        public extern bool canStep
        {
            [NativeName("CanStep")]
            get;
        }

        public extern void StepForward();

        public extern bool canSetPlaybackSpeed
        {
            [NativeName("CanSetPlaybackSpeed")]
            get;
        }

        public extern float playbackSpeed { get; set; }

        [NativeName("Loop")]
        public extern bool isLooping { get; set; }

        [System.Obsolete("VideoPlayer.canSetTimeSource is deprecated. Use canSetTimeUpdateMode instead. (UnityUpgradable) -> canSetTimeUpdateMode")]
        public extern bool canSetTimeSource
        {
            [NativeName("CanSetTimeSource")]
            get;
        }

        [System.Obsolete("VideoPlayer.timeSource is deprecated. Use timeUpdateMode instead. (UnityUpgradable) -> timeUpdateMode")]
        public extern VideoTimeSource timeSource { get; set; }

        public extern VideoTimeReference timeReference { get; set; }

        public extern double externalReferenceTime { get; set; }

        // ==============================================================
        // 📌 视频元数据 — 解析后的视频信息
        //
        // 🎯 frameCount    — 总帧数
        // 🎯 frameRate     — 帧率（fps）
        // 🎯 length        — 视频总时长（秒）
        // 🎯 width/height  — 视频原始分辨率
        // 🎯 pixelAspectRatio — 像素宽高比（非方形像素用）
        //
        // 🎯 canSetSkipOnDrop — 是否支持跳帧
        // 🎯 skipOnDrop       — 解码跟不上时是否丢帧
        //
        // 💡 这些属性在 Prepare() 完成后才可用。
        // ==============================================================
        public extern bool canSetSkipOnDrop
        {
            [NativeName("CanSetSkipOnDrop")]
            get;
        }

        public extern bool skipOnDrop { get; set; }

        public extern ulong frameCount { get; }

        public extern float frameRate { get; }

        [NativeName("Duration")]
        public extern double length
        {
            get;
        }

        public extern uint width { get; }

        public extern uint height { get; }

        public extern uint pixelAspectRatioNumerator { get; }

        public extern uint pixelAspectRatioDenominator { get; }

        // ==============================================================
        // 🎵 音频轨道管理 — 多音轨选择和控制
        //
        // 📌 部分视频包含多个语言/类型的音轨。
        //    audioTrackCount — 视频中的音轨数量
        //    controlledAudioTrackCount — 当前控制激活的音轨数
        //
        // 💡 controlledAudioTrackCount ≤ controlledAudioTrackMaxCount
        //    可以通过 EnableAudioTrack() 切换各音轨的启用状态。
        // ==============================================================
        public extern ushort audioTrackCount { get; }

        public extern string GetAudioLanguageCode(ushort trackIndex);

        public extern ushort GetAudioChannelCount(ushort trackIndex);

        public extern uint GetAudioSampleRate(ushort trackIndex);

        public static extern ushort controlledAudioTrackMaxCount { get; }

        public ushort controlledAudioTrackCount
        {
            get
            {
                return GetControlledAudioTrackCount();
            }

            set
            {
                int maxNumTracks = controlledAudioTrackMaxCount;
                if (value > maxNumTracks)
                    throw new ArgumentException(string.Format("Cannot control more than {0} tracks.", maxNumTracks), "value");

                SetControlledAudioTrackCount(value);
            }
        }

        private extern ushort GetControlledAudioTrackCount();

        private extern void SetControlledAudioTrackCount(ushort value);

        public extern void EnableAudioTrack(ushort trackIndex, bool enabled);

        public extern bool IsAudioTrackEnabled(ushort trackIndex);

        // ==============================================================
        // 📌 音频输出配置 — 每个音轨的输出路由
        //
        // 📌 audioOutputMode 决定音频如何输出：
        //   None        — 不输出
        //   AudioSource — 通过 AudioSource 组件（3D 空间音频）
        //   Direct      — 直接输出到音频设备
        //   APIOnly     — 通过 AudioSampleProvider API
        //
        // 🎯 Direct 模式下可用 Get/SetDirectAudioVolume/Mute
        // 🎯 AudioSource 模式下可用 Get/SetTargetAudioSource
        // ==============================================================
        public extern VideoAudioOutputMode audioOutputMode { get; set; }

        public extern bool canSetDirectAudioVolume
        {
            [NativeName("CanSetDirectAudioVolume")]
            get;
        }

        public extern float GetDirectAudioVolume(ushort trackIndex);

        public extern void SetDirectAudioVolume(ushort trackIndex, float volume);

        public extern bool GetDirectAudioMute(ushort trackIndex);

        public extern void SetDirectAudioMute(ushort trackIndex, bool mute);

        [NativeHeader("Modules/Audio/Public/AudioSource.h")]
        public extern AudioSource GetTargetAudioSource(ushort trackIndex);

        public extern void SetTargetAudioSource(ushort trackIndex, AudioSource source);

        // ==============================================================
        // 📌 事件系统 — VideoPlayer 生命周期回调
        //
        // 📌 事件触发顺序与说明：
        //   prepareCompleted — Prepare() 完成后触发
        //   started          — Play() 后实际开始播放时触发
        //   loopPointReached — 循环点到达时触发
        //   frameDropped     — 解码丢弃帧时触发
        //   errorReceived    — 播放出错时触发（含错误信息）
        //   seekCompleted    — 跳转完成后触发
        //   clockResyncOccurred — 音视频时钟重新同步时触发
        //
        // 📌 frameReady 事件（需要启用 sendFrameReadyEvents）：
        //   — 每帧解码完成后触发，用于自定义帧处理
        //
        // 💡 这些事件由 C++ Native 端通过 Invoke*Callback_Internal 触发。
        //   事件参数均为 VideoPlayer 本身，便于一对多监听。
        // ==============================================================
        public delegate void EventHandler(VideoPlayer source);
        public delegate void ErrorEventHandler(VideoPlayer source, string message);
        public delegate void FrameReadyEventHandler(VideoPlayer source, long frameIdx);
        public delegate void TimeEventHandler(VideoPlayer source, double seconds);

        public event EventHandler prepareCompleted;
        public event EventHandler loopPointReached;
        public event EventHandler started;
        public event EventHandler frameDropped;
        public event ErrorEventHandler errorReceived;
        public event EventHandler seekCompleted;
        public event TimeEventHandler clockResyncOccurred;

        public extern bool sendFrameReadyEvents
        {
            [NativeName("AreFrameReadyEventsEnabled")]
            get;
            [NativeName("EnableFrameReadyEvents")]
            set;
        }

        public event FrameReadyEventHandler frameReady;

        // ==============================================================
        // ⚡ Native 回调桥接 — C++ 端通过 [RequiredByNativeCode] 调用
        //
        // 📌 这些 Internal 方法由 C++ VideoPlayer 在特定事件发生时调用。
        //    每个方法检查 C# 侧的 event 是否为空，不为空则触发。
        //    [RequiredByNativeCode] 确保 IL2CPP 不会裁剪这些方法。
        // ==============================================================
        [RequiredByNativeCode]
        private static void InvokePrepareCompletedCallback_Internal(VideoPlayer source)
        {
            if (source.prepareCompleted != null)
                source.prepareCompleted(source);
        }

        [RequiredByNativeCode]
        private static void InvokeFrameReadyCallback_Internal(VideoPlayer source, long frameIdx)
        {
            if (source.frameReady != null)
                source.frameReady(source, frameIdx);
        }

        [RequiredByNativeCode]
        private static void InvokeLoopPointReachedCallback_Internal(VideoPlayer source)
        {
            if (source.loopPointReached != null)
                source.loopPointReached(source);
        }

        [RequiredByNativeCode]
        private static void InvokeStartedCallback_Internal(VideoPlayer source)
        {
            if (source.started != null)
                source.started(source);
        }

        [RequiredByNativeCode]
        private static void InvokeFrameDroppedCallback_Internal(VideoPlayer source)
        {
            if (source.frameDropped != null)
                source.frameDropped(source);
        }

        [RequiredByNativeCode]
        private static void InvokeErrorReceivedCallback_Internal(VideoPlayer source, string errorStr)
        {
            if (source.errorReceived != null)
                source.errorReceived(source, errorStr);
        }

        [RequiredByNativeCode]
        private static void InvokeSeekCompletedCallback_Internal(VideoPlayer source)
        {
            if (source.seekCompleted != null)
                source.seekCompleted(source);
        }

        [RequiredByNativeCode]
        private static void InvokeClockResyncOccurredCallback_Internal(VideoPlayer source, double seconds)
        {
            if (source.clockResyncOccurred != null)
                source.clockResyncOccurred(source, seconds);
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<string> analyticsSent;

        [RequiredByNativeCode]
        private static void InvokeAnalyticsSentCallback_Internal(string analytics)
        {
            if (analyticsSent != null)
                analyticsSent(analytics);
        }

        [RequiredByNativeCode]
        private static bool AnalyticsEventHandlerAttached_Internal()
        {
            return analyticsSent != null;
        }
    }
}
