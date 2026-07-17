// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// Audio 模块 — Unity 音频引擎的 C# 绑定层
// ================================================================
//
// 【概述】
// 本文件是 Unity 音频系统的核心绑定文件，包含音频播放、监听、
// 混音、空间化等全部关键类的 P/Invoke 声明。
//
// 【音频管线架构】
//   AudioClip（音频数据）
//       ↓
//   AudioSource（播放器/发射源）  ——  挂载在 GameObject 上
//       ↓  空间化处理（3D 衰减、多普勒、混响）
//   AudioListener（监听器/耳朵）  ——  挂载在主摄像机上
//       ↓
//   AudioOutput（硬件输出到扬声器/耳机）
//
// 【核心类关系】
//   AudioResource（抽象基类）
//       ├── AudioClip        — 音频剪辑数据容器（PCM/压缩/流式）
//       └── AudioMixerGroup  — 混音器分组（本文件未定义）
//
//   AudioBehaviour（Behaviour 派生，可挂载到 GameObject）
//       ├── AudioListener   — 3D 空间中的监听器（耳朵位置）
//       └── AudioSource     — 3D 空间中的声音发射源
//
// 【空间音频核心概念】
//   🎯 spatialBlend（空间混合）：
//     0.0 = 纯 2D（不受距离/方向影响，始终等音量）
//     1.0 = 纯 3D（受距离衰减、方向、多普勒效应影响）
//
//   🎯 rolloffMode（衰减模式）：
//     Logarithmic — 真实物理衰减（距离翻倍，音量降 6dB）
//     Linear      — 线性衰减（游戏常用，更易控制）
//     Custom      — 自定义衰减曲线
//
//   🎯 minDistance / maxDistance：
//     minDistance 内：音量不增长（保持最大）
//     minDistance ~ maxDistance：音量衰减区间
//     maxDistance 外：静音（Logarithmic 模式下）
//
// 【内部绑定机制】
//   - [StaticAccessor] 将 C# 静态调用路由到 C++ 单例管理器
//   - [FreeFunction] 是无 this 指针的全局函数调用
//   - [NativeProperty] 映射到 C++ 成员属性
//   - extern 方法在运行时由 IL2CPP/Mono 桥接到原生代码
//
// 📍 对应 C++ 头文件：Modules/Audio/Public/ScriptBindings/Audio.bindings.h
// ================================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Audio;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Playables;
using Unity.IntegerTime;

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

[assembly: InternalsVisibleTo("Unity.AudioMixer.Tests")]

namespace UnityEngine.Audio
{
    [NativeHeader("Modules/Audio/Public/AudioResource.h")]
    public abstract class AudioResource : Object
    {
        protected internal AudioResource() {}
    }

    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    sealed class AudioManagerTestProxy
    {
        [NativeMethod(Name = "AudioManagerTestProxy::ComputeAudibilityConsistency", IsFreeFunction = true)]
        internal static extern bool ComputeAudibilityConsistency();
    }

    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    sealed class PlatformAudioTestProxy
    {
        [NativeMethod(Name = "PlatformAudioTestProxy::HasDefaultPlaybackDevice", IsFreeFunction = true)]
        internal static extern bool HasDefaultPlaybackDevice();
    }
}

namespace UnityEngine
{
    // ================================================================
    // 音频枚举与配置类型
    // ================================================================
    // 本节定义了音频系统的全部枚举类型和配置结构体。
    // 它们控制音频硬件配置、压缩格式、空间化行为等底层参数。
    // ================================================================

    // ================================================================
    // AudioSpeakerMode - 扬声器输出模式
    // ================================================================
    // 定义音频系统的输出声道配置，对应硬件扬声器布局。
    //
    // 🎯 关键概念：
    //   此枚举决定了 AudioListener 混音后的输出声道数。
    //   例如 Mono = 1 声道，Stereo = 2 声道，Mode5point1 = 6 声道。
    //
    // 💡 与 AudioConfiguration 的关系：
    //   AudioConfiguration.speakerMode 使用此枚举设定全局输出模式。
    //   更改 speakerMode 会影响所有 AudioSource 的混音输出。
    //
    // ⚠️ 注意：
    //   speakerMode 的设置已被标记为过时，推荐使用
    //   AudioSettings.GetConfiguration/Reset API 进行配置。
    // ================================================================
    // These are speaker types defined for use with [[AudioSettings.speakerMode]].
    // Must be kept in sync with its C++ counterpart `AudioSpeakerMode`.
    public enum AudioSpeakerMode
    {
        // Channel count is unaffected.
        [Obsolete("Raw speaker mode is not supported. Do not use.", true)] Raw = 0,
        // Channel count is set to 1. The speakers are monaural.
        Mono = 1,
        // Channel count is set to 2. The speakers are stereo. This is the editor default.
        Stereo = 2,
        // Channel count is set to 4. 4 speaker setup. This includes front left, front right, rear left, rear right.
        Quad = 3,
        // Channel count is set to 5. 5 speaker setup. This includes front left, front right, center, rear left, rear right.
        Surround = 4,
        // Channel count is set to 6. 5.1 speaker setup. This includes front left, front right, center, rear left, rear right and a subwoofer.
        Mode5point1 = 5,
        // Channel count is set to 8. 7.1 speaker setup. This includes front left, front right, center, rear left, rear right, side left, side right and a subwoofer.
        Mode7point1 = 6,
        // Channel count is set to 2. Stereo output, but data is encoded in a way that is picked up by a Prologic/Prologic2 decoder and split into a 5.1 speaker setup.
        Prologic = 7
    }

    // ================================================================
    // AudioFoundation - 音频基础架构模式（内部）
    // ================================================================
    // Classic  = 传统音频管线（旧版兼容）
    // Enhanced = 增强音频管线（支持更多声道布局、灵活采样率）
    // ================================================================
    internal enum AudioFoundation
    {
        Classic = 0,
        Enhanced = 1,
    }

    // ================================================================
    // ChannelLayoutBehavior / SamplingRateBehavior - 增强音频配置参数
    // ================================================================
    // 🎯 ChannelLayoutBehavior：
    //   增强模式下的声道布局选择。
    //   DeviceNative 表示使用系统默认声道数，
    //   其他值强制指定输出声道数（如 Surround_7_1_4 = 12 声道）。
    //
    // 🎯 SamplingRateBehavior：
    //   增强模式下的采样率选择。
    //   DeviceNative 表示使用硬件默认采样率，
    //   其他值强制指定（如 44100Hz = CD 品质，48000Hz = 视频标准）。
    //
    // 💡 这些枚举仅在 Enhanced 音频基础架构下有效。
    // ================================================================
	internal enum ChannelLayoutBehavior
    {
        DeviceNative = 0,
        Mono = 1,
        Stereo = 2,
        Quadraphonic = 4,
        Surround_5_0 = 5,
        Surround_5_1 = 6,
        Surround_7_1 = 8,
        Surround_7_1_4 = 12
    };

    internal enum SamplingRateBehavior
    {
        DeviceNative = 0,
        Hz8000 = 8000,
        Hz16000 = 16000,
        Hz22050 = 22050,
        Hz24000 = 24000,
        Hz32000 = 32000,
        Hz44100 = 44100,
        Hz48000 = 48000
    };

    // ================================================================
    // AudioExtensions - 音频扬声器模式扩展方法
    // ================================================================
    // 为 AudioSpeakerMode 枚举提供便捷的扩展方法。
    //
    // 🎯 ChannelCount()：
    //   返回给定扬声器模式对应的声道数量。
    //   例如 Mode5point1 → 6 声道，Mode7point1 → 8 声道。
    //
    // ⚡ 内部调用 C++ 端的 AudioSpeakerModeBindings 获取准确声道数，
    //    C# 端的 switch 分支提供冗余备份。
    // ================================================================
    public static class AudioExtensions
    {
        [NativeMethod(Name = "AudioSpeakerModeBindings::InternalIAudioSpeakerModeChannelCount", IsFreeFunction = true)]
        internal static extern int InternalIAudioSpeakerModeChannelCount(AudioSpeakerMode speakerMode);

        public static int ChannelCount(this AudioSpeakerMode speakerMode)
        {
            switch (speakerMode)
            {
                case AudioSpeakerMode.Mono: return 1;
                case AudioSpeakerMode.Stereo: return 2;
                case AudioSpeakerMode.Quad: return 4;
                case AudioSpeakerMode.Surround: return 5;
                case AudioSpeakerMode.Mode5point1: return 6;
                case AudioSpeakerMode.Mode7point1: return 8;
                case AudioSpeakerMode.Prologic: return 2;
                throw new ArgumentException($"{nameof(speakerMode)}");
            }

            throw new ArgumentException($"{nameof(speakerMode)}");
        }

        [NativeMethod(Name = "AudioSpeakerModeBindings::InternaIAudioSpeakerModeIsCapped", IsFreeFunction = true)]
        internal static extern bool InternalAudioSpeakerModeIsCapped(AudioSpeakerMode speakerMode);
    }

    // ================================================================
    // AudioDataLoadState - AudioClip 加载状态
    // ================================================================
    // 🎯 表示 AudioClip 数据在内存中的加载进度。
    //   Unloaded → Loading → Loaded（成功）或 Failed（失败）
    //
    // 💡 使用场景：
    //   通过 AudioClip.loadState 可以监控异步加载进度，
    //   特别是在流式加载（Streaming）或后台加载（loadInBackground）时。
    //
    // ⚠️ 常见陷阱：
    //   在 Loaded 之前使用 AudioClip 可能导致无声或异常。
    //   检查 loadState 是编写健壮音频代码的关键。
    // ================================================================
    public enum AudioDataLoadState
    {
        Unloaded = 0,
        Loading = 1,
        Loaded = 2,
        Failed = 3
    }

    // ================================================================
    // AudioConfiguration - 音频系统全局配置结构体
    // ================================================================
    // 🎯 描述音频硬件的全局输出配置：
    //   speakerMode  — 扬声器输出模式（Mono/Stereo/5.1/7.1 等）
    //   dspBufferSize — DSP 缓冲区大小（样本数），影响延迟和性能
    //   sampleRate    — 输出采样率（如 44100Hz、48000Hz）
    //   numRealVoices — 真实语音数（实际发声的最大 AudioSource 数量）
    //   numVirtualVoices — 虚拟语音数（暂停/距离外的语音占位数）
    //
    // 💡 dspBufferSize 与延迟的关系：
    //   缓冲区越小 → 延迟越低，但 CPU 开销越高
    //   缓冲区越大 → 延迟越高，但 CPU 越平滑
    //   典型值：512 样本 ≈ 11.6ms 延迟（44100Hz 下）
    //
    // ⚠️ numRealVoices 限制了同时播放的音频源数量。
    //   超出限制时，低优先级（priority 值大）的 AudioSource 会被静音。
    // ================================================================
    public struct AudioConfiguration
    {
        public AudioSpeakerMode speakerMode;
        public int dspBufferSize;
        public int sampleRate;
        public int numRealVoices;
        public int numVirtualVoices;
    }

    internal struct EnhancedAudioConfiguration
    {
        public AudioFoundation audioFoundation;
        public ChannelLayoutBehavior outputChannelLayout;
        public SamplingRateBehavior outputSampleRate;
    }

    // ================================================================
    // AudioCompressionFormat - 音频压缩格式
    // ================================================================
    // 🎯 定义 AudioClip 在磁盘和内存中的压缩编码方式。
    //
    // 💡 各格式特点：
    //   PCM    — 无压缩，品质最高，内存占用最大
    //   Vorbis — 有损压缩，平衡品质和大小（Unity 默认导入格式）
    //   ADPCM  — 适合短促音效（爆炸、脚步声），解压快但品质略低
    //   MP3    — 通用有损压缩，适合背景音乐
    //   AAC    — Apple 平台首选格式
    //   HEVAG/XMA/GCADPCM/ATRAC9 — 主机平台专用格式
    //
    // ⚡ 性能提示：
    //   DecompressOnLoad 加载时解压为 PCM（适合短音效）
    //   CompressedInMemory 保持压缩态，播放时实时解压（节省内存）
    //   Streaming 从磁盘流式读取（适合长音乐，最低内存占用）
    // ================================================================
    // Imported audio format for [[AudioImporter]].
    public enum AudioCompressionFormat
    {
        PCM = 0,
        Vorbis = 1,
        ADPCM = 2,
        MP3 = 3,
        VAG = 4,
        HEVAG = 5,
        XMA = 6,
        AAC = 7,
        GCADPCM = 8,
        ATRAC9 = 9
    }

    // ================================================================
    // AudioClipLoadType - AudioClip 加载类型
    // ================================================================
    // 🎯 决定 AudioClip 数据如何从磁盘加载到内存。
    //
    //   DecompressOnLoad  — 加载时完全解压为 PCM 到内存
    //                       ✅ 播放零开销   ❌ 内存占用大
    //                       适合：短音效（< 1秒）
    //
    //   CompressedInMemory — 保持压缩态存于内存，播放时实时解压
    //                       ✅ 内存占用小   ❌ 播放时有解压 CPU 开销
    //                       适合：中等长度音效（1-10秒）
    //
    //   Streaming          — 从磁盘流式读取，不占用预加载内存
    //                       ✅ 内存占用极小  ❌ 有磁盘 I/O 开销
    //                       适合：背景音乐、长语音（> 10秒）
    //
    // ⚠️ 常见陷阱：
    //   Streaming 类型的 AudioClip 在加载完成前不会播放。
    //   preloadAudioData 设为 false 时需要手动调用 LoadAudioData()。
    // ================================================================
    // The way we load audio assets [[AudioImporter]].
    public enum AudioClipLoadType
    {
        DecompressOnLoad = 0,
        CompressedInMemory = 1,
        Streaming = 2
    }

    // ================================================================
    // AudioVelocityUpdateMode - 音频速度更新模式
    // ================================================================
    // 🎯 控制 AudioSource/AudioListener 在哪个更新循环中计算多普勒效应。
    //
    //   Auto   — 自动选择：有 Rigidbody 用 FixedUpdate，否则用 Update
    //   Fixed  — 始终在 FixedUpdate 中更新（适合物理驱动的音频源）
    //   Dynamic — 始终在 Update 中更新（默认，适合大多数情况）
    //
    // 💡 为什么需要这个设置？
    //   多普勒效应依赖速度计算，而速度在不同更新循环中可能不同。
    //   FixedUpdate 与物理引擎同步，Update 与渲染帧同步。
    //   如果 AudioSource 挂载了 Rigidbody，应设为 Fixed 以保证一致性。
    // ================================================================
    // Describes when an [[AudioSource]] or [[AudioListener]] is updated.
    public enum AudioVelocityUpdateMode
    {
        // Updates the source or listener in the fixed update loop if it is attached to a [[Rigidbody]], dynamic otherwise.
        Auto = 0,
        // Updates the source or listener in the fixed update loop.
        Fixed = 1,
        // Updates the source or listener in the dynamic update loop.
        Dynamic = 2
    }

    // ================================================================
    // FFTWindow - 快速傅里叶变换窗函数类型
    // ================================================================
    // 🎯 用于 GetSpectrumData() 的频谱分析窗函数。
    //
    // 💡 什么是窗函数？
    //   FFT 将时域信号转换为频域信号时，截断信号会产生频谱泄漏。
    //   窗函数通过对截断边缘进行平滑处理来减少泄漏。
    //
    // 🎯 各窗函数特点：
    //   Rectangular    — 不做处理，泄漏最严重，但频率分辨率最高
    //   Hamming        — 最常用，平衡泄漏和分辨率
    //   Hanning        — 类似 Hamming，边缘更平滑
    //   Blackman       — 泄漏最小，但频率分辨率略低
    //   BlackmanHarris — 泄漏最小，适合精确频率分析
    //
    // ⚡ 实际建议：
    //   做可视化效果（音频条形图）用 Hamming 或 Hanning
    //   做精确频率检测用 BlackmanHarris
    // ================================================================
    // Spectrum analysis windowing types
    public enum FFTWindow
    {
        // w[n] = 1.0
        Rectangular = 0,
        // w[n] = TRI(2n/N)
        Triangle = 1,
        // w[n] = 0.54 - (0.46 * COS(n/N) )
        Hamming = 2,
        // w[n] = 0.5 * (1.0 - COS(n/N) )
        Hanning = 3,
        // w[n] = 0.42 - (0.5 * COS(n/N) ) + (0.08 * COS(2.0 * n/N) )
        Blackman = 4,
        // w[n] = 0.35875 - (0.48829 * COS(1.0 * n/N)) + (0.14128 * COS(2.0 * n/N)) - (0.01168 * COS(3.0 * n/N))
        BlackmanHarris = 5
    }

    // ================================================================
    // AudioRolloffMode - 3D 音频距离衰减模式
    // ================================================================
    // 🎯 控制 AudioSource 在 3D 空间中随距离的音量衰减方式。
    //
    //   Logarithmic — 对数衰减，模拟真实物理声学
    //                 距离翻倍 → 音量降低约 6dB
    //                 适合：真实感场景（模拟器、VR）
    //
    //   Linear — 线性衰减，从 minDistance 到 maxDistance 匀速降为 0
    //            适合：大多数游戏（直观可控）
    //
    //   Custom — 使用 AnimationCurve 自定义衰减曲线
    //            适合：需要特殊音效（如近距离爆发衰减）
    //
    // 💡 配合 minDistance / maxDistance 使用：
    //   minDistance 内：音量 = 1.0（满音量）
    //   minDistance → maxDistance：按曲线衰减
    //   maxDistance 外：音量 = 0.0（静音）
    //
    // ⚠️ Logarithmic 模式下 maxDistance 外完全静音，
    //    但 Linear 模式下 maxDistance 处刚好降为 0。
    // ================================================================
    // Rolloff modes that a 3D sound can have in an audio source.
    public enum AudioRolloffMode
    {
        // Use this mode when you want a real-world rolloff.
        Logarithmic = 0,
        // Use this mode when you want to lower the volume of your sound over the distance
        Linear = 1,
        // Use this when you want to use a custom rolloff.
        Custom = 2
    }

    // ================================================================
    // AudioSourceCurveType - AudioSource 自定义曲线类型
    // ================================================================
    // 🎯 定义 AudioSource 可以使用 AnimationCurve 自定义的行为曲线：
    //
    //   CustomRolloff  — 自定义距离衰减曲线（rolloffMode = Custom 时生效）
    //   SpatialBlend   — 空间混合曲线（2D ↔ 3D 的距离过渡）
    //   ReverbZoneMix  — 混响区混合曲线
    //   Spread         — 声音扩散角度曲线（控制立体声宽度随距离的变化）
    //
    // 💡 这些曲线通过 SetCustomCurve() / GetCustomCurve() 读写。
    //    曲线的 X 轴是归一化距离（0 = minDistance, 1 = maxDistance）。
    //    Y 轴是对应行为的值（0.0 ~ 1.0）。
    // ================================================================
    public enum AudioSourceCurveType
    {
        CustomRolloff = 0,
        SpatialBlend  = 1,
        ReverbZoneMix = 2,
        Spread        = 3
    }

    public enum GamepadSpeakerOutputType
    {
        Speaker = 0,
        Vibration = 1,
        SecondaryVibration = 2
    }


    // ================================================================
    // AudioReverbPreset - 混响预设
    // ================================================================
    // 🎯 用于 AudioReverbZone 和 AudioReverbFilter 的环境混响预设。
    //
    // 💡 混响模拟声音在不同空间中的反射：
    //   Off          — 无混响
    //   Room/Bathroom/Livingroom — 小空间环境
    //   Auditorium/Concerthall   — 大空间环境
    //   Cave/Forest/City         — 户外特殊环境
    //   Underwater/Dizzy/Drugged — 特殊效果环境
    //
    // 🎯 预设 vs 手动参数：
    //   选择预设会自动设置 decayTime、reflections、reverb 等参数。
    //   如需精细控制，可以先选预设再手动微调各参数值。
    // ================================================================
    // Reverb presets used by the Reverb Zone class and the audio reverb filter
    public enum AudioReverbPreset
    {
        // No reverb preset selected
        Off = 0,

        // Generic preset.
        Generic = 1,

        // Padded cell preset.
        PaddedCell = 2,

        // Room preset.
        Room = 3,

        // Bathroom preset.
        Bathroom = 4,

        // Livingroom preset
        Livingroom = 5,

        // Stoneroom preset
        Stoneroom = 6,

        // Auditorium preset.
        Auditorium = 7,

        // Concert hall preset.
        Concerthall = 8,

        // Cave preset.
        Cave = 9,

        // Arena preset.
        Arena = 10,

        // Hangar preset.
        Hangar = 11,

        // Carpeted hallway preset.
        CarpetedHallway = 12,

        // Hallway preset.
        Hallway = 13,

        // Stone corridor preset.
        StoneCorridor = 14,

        // Alley preset.
        Alley = 15,

        // Forest preset.
        Forest = 16,

        // City preset.
        City = 17,

        // Mountains preset.
        Mountains = 18,

        // Quarry preset.
        Quarry = 19,

        // Plain preset.
        Plain = 20,

        // Parking Lot preset
        ParkingLot = 21,

        // Sewer pipe preset.
        SewerPipe = 22,

        // Underwater presset
        Underwater = 23,

        // Drugged preset
        Drugged = 24,

        // Dizzy preset.
        Dizzy = 25,

        // Psychotic preset.
        Psychotic = 26,

        // User defined preset.
        User = 27
    }

    internal struct PlayableSettings
    {
        public AudioContainerElement element { get; }
        public double scheduledTime { get; }
        public float pitchOffset { get; }
        public float volumeOffset { get; }
        public double triggerTimeOffset { get; }
    }

    internal struct ActivePlayable
    {
        public PlayableSettings settings { get; }
        public PlayableHandle clipPlayableHandle { get; }
    }

    // ================================================================
    // AudioSpatialExperience - 音频空间体验模式
    // ================================================================
    // 🎯 控制音频系统空间化处理的全局行为：
    //
    //   Bypassed    — 跳过空间化处理（默认值，所有平台都支持）
    //   HeadTracked — 启用头部追踪空间化（需要 XR 头显硬件支持）
    //   Fixed       — 固定位置空间化（无头部追踪，但保留 3D 音效）
    //
    // ⚠️ 当前此属性在大多数平台上仅返回 Bypassed，
    //    实际空间化通过 Spatializer 插件实现。
    // ================================================================
    public enum AudioSpatialExperience
    {
        Bypassed = 0,
        HeadTracked = 1,
        Fixed = 2
    }

    // ================================================================
    // AudioSettings - 全局音频系统配置（静态类）
    // ================================================================
    //
    // 【概述】
    // AudioSettings 是一个 sealed 静态类，提供对 Unity 音频系统全局配置的
    // 访问和修改能力。它不挂载到 GameObject，而是直接控制底层音频引擎。
    //
    // 【绑定机制】
    //   [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
    //   → 所有 C# 静态方法调用被路由到 C++ 端的 AudioManager 单例对象。
    //   这意味着 AudioSettings 实际上是 C++ AudioManager 的 C# 代理。
    //
    // 【核心功能】
    //   🎯 speakerMode      — 获取/设置输出声道模式（已过时，推荐 Reset API）
    //   🎯 dspTime          — 获取音频系统的高精度 DSP 时间
    //   🎯 outputSampleRate — 获取/设置输出采样率
    //   🎯 GetDSPBufferSize — 获取 DSP 缓冲区大小（影响延迟）
    //   🎯 GetConfiguration — 获取完整音频配置结构体
    //   🎯 Reset            — 应用新的音频配置
    //
    // 【事件系统】
    //   OnAudioConfigurationChanged — 当音频设备变更时触发（如插拔耳机）
    //   OnAudioSystemShuttingDown   — 音频系统关闭时触发（内部）
    //   OnAudioSystemStartedUp      — 音频系统启动时触发（内部）
    //
    // 【Spatializer 插件】
    //   可通过 GetSpatializerPluginName() / SetSpatializerPluginName()
    //   切换空间化处理插件（如 Oculus Spatializer、Resonance Audio）。
    //
    // 【Mobile 子类】
    //   AudioSettings.Mobile 提供移动平台特有的音频控制：
    //   - muteState：静音状态
    //   - StartAudioOutput/StopAudioOutput：控制音频输出（省电）
    //   - OnMuteStateChanged：静音状态变更事件
    //
    // ⚡ 性能提示：
    //   dspTime 基于音频系统处理的样本数计算，比 Time.time 更精确，
    //   适合用于精确的音频调度（如节拍同步）。
    //
    // 📍 C++ 端：Modules/Audio/Public/AudioManager.h
    // ================================================================
    // Controls the global audio settings from script.
    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
    public sealed partial class AudioSettings
    {
        extern static private AudioSpeakerMode GetSpeakerMode();
        [NativeMethod(Name = "AudioSettings::SetConfiguration", IsFreeFunction = true, ThrowsException = true)]
        extern static private bool SetConfiguration(AudioConfiguration config);

        [NativeMethod(Name = "AudioSettings::SetEnhancedConfiguration", IsFreeFunction = true)]
        extern static internal bool SetEnhancedConfiguration(EnhancedAudioConfiguration config);

        [NativeMethod(Name = "AudioSettings::GetSampleRate", IsFreeFunction = true)]
        extern static private int GetSampleRate();

        extern static private bool SetSpatializerName(string pluginName);

        // Returns the speaker mode capability of the current audio driver. Read only.
        extern static public AudioSpeakerMode driverCapabilities
        {
            [NativeName("GetSpeakerModeCaps")]
            get;
        }

        // Gets the current speaker mode. Default is 2 channel stereo.
        static public AudioSpeakerMode speakerMode
        {
            get
            {
                return GetSpeakerMode();
            }
            set
            {
                Debug.LogWarning("Setting AudioSettings.speakerMode is deprecated and has been replaced by audio project settings and the AudioSettings.GetConfiguration/AudioSettings.Reset API.");
                AudioConfiguration config = GetConfiguration();
                config.speakerMode = value;
                if (!SetConfiguration(config))
                    Debug.LogWarning("Setting AudioSettings.speakerMode failed");
            }
        }

        extern static internal int profilerCaptureFlags { get; }

        // ================================================================
        // dspTime - 数字信号处理时间（高精度）
        // ================================================================
        // 🎯 返回音频系统当前的 DSP 时间（秒），基于已处理的音频样本数。
        //
        // 💡 为什么比 Time.time 更精确？
        //   Time.time 基于渲染帧率，帧率波动时不够精确。
        //   dspTime 基于音频样本计数，精度可达微秒级（44100Hz 下 ≈ 22.7μs）。
        //
        // 🎯 典型用途：
        //   - 节拍同步：根据 dspTime 计算精确的节拍点
        //   - PlayScheduled：在精确时间点触发音频播放
        //   - 交叉淡入淡出：精确控制两个 AudioClip 的过渡时机
        //
        // ⚠️ Unity 暂停时 dspTime 保持不变（不随游戏时间流逝）。
        // ================================================================
        // Returns the current time of the audio system. This is based on the number of samples the audio system processes and is therefore more exact than the time obtained via the Time.time property.
        // It is constant while Unity is paused.
        extern static public double dspTime
        {
            [NativeMethod(Name = "GetDSPTime", IsThreadSafe = true)]
            get;
        }

        // Get and set the mixer's current output rate.
        static public int outputSampleRate
        {
            get
            {
                return GetSampleRate();
            }

            set
            {
                Debug.LogWarning("Setting AudioSettings.outputSampleRate is deprecated and has been replaced by audio project settings and the AudioSettings.GetConfiguration/AudioSettings.Reset API.");
                AudioConfiguration config = GetConfiguration();
                config.sampleRate = value;
                if (!SetConfiguration(config))
                    Debug.LogWarning("Setting AudioSettings.outputSampleRate failed");
            }
        }

        [NativeMethod(Name = "AudioSettings::GetDSPBufferSize", IsFreeFunction = true)]
        extern static public void GetDSPBufferSize(out int bufferLength, out int numBuffers);

        // Set the mixer's buffer size in samples.
        [Obsolete("AudioSettings.SetDSPBufferSize is deprecated and has been replaced by audio project settings and the AudioSettings.GetConfiguration/AudioSettings.Reset API.")]
        static public void SetDSPBufferSize(int bufferLength, int numBuffers)
        {
            Debug.LogWarning("AudioSettings.SetDSPBufferSize is deprecated and has been replaced by audio project settings and the AudioSettings.GetConfiguration/AudioSettings.Reset API.");
            AudioConfiguration config = GetConfiguration();
            config.dspBufferSize = bufferLength;
            if (!SetConfiguration(config))
                Debug.LogWarning("SetDSPBufferSize failed");
        }

        extern static internal bool editingInPlaymode
        {
            [NativeName("IsEditingInPlaymode")]
            get;

            [NativeName("SetEditingInPlaymode")]
            set;
        }

        [NativeMethod(Name = "AudioSettings::GetSpatializerNames", IsFreeFunction = true)]
        extern static public string[] GetSpatializerPluginNames();

        [NativeName("GetCurrentSpatializerDefinitionName")]
        extern static public string GetSpatializerPluginName();

        static public void SetSpatializerPluginName(string pluginName)
        {
            if (!SetSpatializerName(pluginName))
                throw new ArgumentException("Invalid spatializer plugin name");
        }


        // ================================================================
        // 音频配置获取与重置
        // ================================================================
        // 🎯 GetConfiguration()：
        //   返回当前完整的 AudioConfiguration 结构体。
        //   可以读取当前的 speakerMode、dspBufferSize、sampleRate 等。
        //
        // 🎯 Reset(config)：
        //   应用新的 AudioConfiguration 并重新初始化音频引擎。
        //   ⚠️ 更改 speakerMode 可能需要重启音频引擎，导致短暂中断。
        //
        // 💡 推荐用法：
        //   先 GetConfiguration 获取当前配置 → 修改需要的字段 → Reset 应用。
        //   而不是直接设置 speakerMode 等已过时的属性。
        // ================================================================
        extern static public AudioConfiguration GetConfiguration();
        extern static internal EnhancedAudioConfiguration GetEnhancedConfiguration();

        static public bool Reset(AudioConfiguration config)
        {
            return SetConfiguration(config);
        }

        static internal bool Reset(EnhancedAudioConfiguration config)
        {
            return SetEnhancedConfiguration(config);
        }

        // ================================================================
        // 音频配置变更事件
        // ================================================================
        // 🎯 OnAudioConfigurationChanged：
        //   当音频设备配置发生变更时触发。
        //   参数 deviceWasChanged = true 表示切换了物理设备（如插拔耳机）。
        //   参数 deviceWasChanged = false 表示内部配置变更（如更改采样率）。
        //
        // 💡 使用场景：
        //   - 检测耳机插拔并更新 UI 提示
        //   - 在设备变更后重新初始化音频效果
        //   - 记录音频设备变更日志
        //
        // ⚠️ 此事件从原生线程触发，如果需要操作 Unity 对象，
        //    请使用 MonoBehaviour 的回调中转。
        // ================================================================
        public delegate void AudioConfigurationChangeHandler(bool deviceWasChanged);

        static public event AudioConfigurationChangeHandler OnAudioConfigurationChanged;
        internal static event Action OnAudioSystemShuttingDown;
        internal static event Action OnAudioSystemStartedUp;

        [RequiredByNativeCode]
        static internal void InvokeOnAudioConfigurationChanged(bool deviceWasChanged)
        {
            if (OnAudioConfigurationChanged != null)
                OnAudioConfigurationChanged(deviceWasChanged);
        }

        [RequiredByNativeCode]
        internal static void InvokeOnAudioSystemShuttingDown()
            => OnAudioSystemShuttingDown?.Invoke();

        [RequiredByNativeCode]
        internal static void InvokeOnAudioSystemStartedUp()
            => OnAudioSystemStartedUp?.Invoke();

        extern static internal bool unityAudioDisabled
        {
            [NativeName("IsAudioDisabled")]
            get;
            [NativeName("DisableAudio")]
            set;
        }

        [NativeMethod(Name = "AudioSettings::GetCurrentAmbisonicDefinitionName", IsFreeFunction = true)]
        extern static internal string GetAmbisonicDecoderPluginName();

        [NativeMethod(Name = "AudioSettings::SetAmbisonicName", IsFreeFunction = true)]
        extern static internal void SetAmbisonicDecoderPluginName(string name);

        static public AudioSpatialExperience audioSpatialExperience
        {
            get { return AudioSpatialExperience.Bypassed; }
            set { Debug.LogWarning("AudioSettings.audioSpatialExperience is not implemented on this platform."); }
        }

        public static class Mobile
        {
            static public bool muteState
            {
                get { return false; }
            }

            static public bool stopAudioOutputOnMute
            {
                get { return false; }
                set
                {
                    Debug.LogWarning("Setting AudioSettings.Mobile.stopAudioOutputOnMute is possible on iOS and Android only");
                }
            }

            static public bool audioOutputStarted
            {
                get { return true; }
            }

#pragma warning disable 0067
            static public event Action<bool> OnMuteStateChanged;
#pragma warning restore 0067

            static public void StartAudioOutput()
            {
                Debug.LogWarning("AudioSettings.Mobile.StartAudioOutput is implemented for iOS and Android only");
            }

            static public void StopAudioOutput()
            {
                Debug.LogWarning("AudioSettings.Mobile.StopAudioOutput is implemented for iOS and Android only");
            }
        }
    }

    // ================================================================
    // AudioClip - 音频剪辑数据容器
    // ================================================================
    //
    // 【概述】
    // AudioClip 是 Unity 音频数据的核心容器，存储 PCM 音频采样数据。
    // 它本身不能播放声音，必须通过 AudioSource 组件来播放。
    //
    // 【继承链】
    // AudioClip -> AudioResource -> Object
    // 实现了 IAudioGenerator 接口，支持新的音频管线（Playable API）。
    //
    // 【内部绑定机制】
    //   [StaticAccessor("AudioClipBindings", StaticAccessorType.DoubleColon)]
    //   → 静态方法路由到 C++ 的 AudioClipBindings 全局命名空间
    //   extern 方法（如 GetData/SetData）通过 P/Invoke 直接访问原生内存
    //
    // 【音频数据存储流程】
    //   1. 磁盘文件（.wav/.mp3/.ogg）→ AudioImporter 解码/压缩
    //   2. 导入后数据存为 AudioClip 资源（.asset）
    //   3. 运行时 AudioClip.loadType 决定数据如何加载到内存：
    //      - DecompressOnLoad → 完全解压到原生内存
    //      - CompressedInMemory → 压缩态保存，播放时实时解压
    //      - Streaming → 从磁盘流式读取
    //   4. AudioSource 从 AudioClip 读取采样数据，经处理后输出
    //
    // 【核心属性】
    //   🎯 length     — 音频时长（秒），只读
    //   🎯 samples    — 总采样点数，只读
    //   🎯 channels   — 声道数（1=单声道, 2=立体声），只读
    //   🎯 frequency  — 采样频率（Hz），只读
    //   🎯 loadType   — 加载类型，只读
    //   🎯 loadState  — 当前加载状态（Unloaded/Loading/Loaded/Failed）
    //
    // 【程序化创建】
    //   AudioClip.Create() 可以在运行时创建 AudioClip：
    //   - 提供 PCMReaderCallback 委托来填充采样数据
    //   - 提供 PCMSetPositionCallback 来响应播放位置变化
    //   - 支持流式（stream=true）和非流式两种模式
    //
    // 【IAudioGenerator 接口】
    //   AudioClip 实现了 IAudioGenerator，使其可以作为新的
    //   音频管线（Playable API）中的音频源节点。
    //   CheckIsNotPersistent() 确保只使用持久化 AudioClip，
    //   运行时动态创建的 AudioClip 不支持此接口。
    //
    // ⚠️ 常见陷阱：
    //   - AudioClip 不能被 GC 回收，必须显式调用 UnloadAudioData()
    //   - 运行时 Create 的 AudioClip 在场景卸载后不会自动销毁
    //   - GetData/SetData 的采样值范围是 [-1.0, 1.0]，超出会产生杂音
    //
    // 📍 C++ 端：Modules/Audio/Public/AudioClip.h
    // ================================================================
    // A container for audio data.
    [NativeHeader("Modules/Audio/Public/ScriptBindings/Audio.bindings.h")]
    [StaticAccessor("AudioClipBindings", StaticAccessorType.DoubleColon)]
    public sealed class AudioClip : AudioResource, IAudioGenerator
    {
        private AudioClip() {}

        extern static private bool GetData([NotNull] AudioClip clip, Span<float> data, int samplesOffset);
        extern static private bool SetData([NotNull] AudioClip clip, ReadOnlySpan<float> data, int samplesOffset);
        extern static private AudioClip Construct_Internal();

        extern private string GetName();
        extern private void CreateUserSound(string name, int lengthSamples, int channels, int frequency, bool stream);
        extern private bool IsLegacyFormat();

        // ================================================================
        // AudioClip 属性 — 音频元数据
        // ================================================================
        // 🎯 这些属性全部映射到 C++ 原生属性（[NativeProperty]），
        //    返回 AudioClip 创建时确定的不可变元数据。
        //
        // 💡 channels × frequency = 每秒采样点数（bytes/sec 的基础）
        //    samples / frequency = length（秒）
        //    总内存 = samples × channels × sizeof(float)（PCM 格式下）
        // ================================================================

        // The length of the audio clip in seconds (read-only)
        [NativeProperty("LengthSec")]
        extern public float length { get; }

        // The length of the audio clip in samples (read-only)
        // Prints how many samples the attached audio source has
        [NativeProperty("SampleCount")]
        extern public int samples { get; }

        // Channels in audio clip (read-only)
        [NativeProperty("ChannelCount")]
        extern public int channels { get; }

        // Sample frequency (read-only)
        extern public int frequency { get; }

        // Is a streamed audio clip ready to play? (read-only)
        [Obsolete("Use AudioClip.loadState instead to get more detailed information about the loading process.")]
        extern public bool isReadyToPlay
        {
            [NativeName("ReadyToPlay")]
            get;
        }

        // AudioClip load type (read-only)
        extern public AudioClipLoadType loadType { get; }

        extern public bool LoadAudioData();
        extern public bool UnloadAudioData();

        extern public bool preloadAudioData { get; }

        extern public bool ambisonic { get; }

        [NativeMethod(Name = "AudioClipBindings::IsValidAmbisonicChannelCount", IsFreeFunction = true)]
        internal static extern bool IsValidAmbisonicChannelCount(int channels);

        extern public bool loadInBackground { get; }

        extern public AudioDataLoadState loadState
        {
            [NativeMethod(Name = "AudioClipBindings::GetLoadState", HasExplicitThis = true)]
            get;
        }

        // ================================================================
        // 采样数据读写 — GetData / SetData
        // ================================================================
        // 🎯 GetData() — 从 AudioClip 读取原始 PCM 采样数据。
        //   data: float 数组，接收 [-1.0, 1.0] 范围的采样值
        //   offsetSamples: 从第几个采样点开始读取
        //
        // 🎯 SetData() — 向 AudioClip 写入 PCM 采样数据。
        //   仅对通过 Create() 创建的 AudioClip 有效。
        //   ⚠️ 值超出 [-1.0, 1.0] 范围会导致音频杂音和未定义行为！
        //
        // 💡 Span<T> 重载 vs float[] 重载：
        //   Span 版本使用 stackalloc 或 MemoryPool 避免堆分配。
        //   float[] 版本内部调用 AsSpan() 转换后委托给 Span 版本。
        //   推荐在高频调用场景使用 Span 版本以减少 GC 压力。
        //
        // ⚡ 性能提示：
        //   GetData/SetData 通过 P/Invoke 桥接到原生内存操作。
        //   频繁调用会有显著开销，建议在音频回调中批量处理。
        // ================================================================

        // Fills a Span with sample data from the clip. The samples are floats ranging from -1.0f to 1.0f. The sample count is determined by the length of the Span.
        public unsafe bool GetData(Span<float> data, int offsetSamples)
        {
            if (channels <= 0)
            {
                Debug.Log("AudioClip.GetData failed; AudioClip " + GetName() + " contains no data");
                return false;
            }

            return GetData(this, data, offsetSamples);
        }

        // Fills an array with sample data from the clip. The samples are floats ranging from -1.0f to 1.0f. The sample count is determined by the length of the float array.
        public bool GetData(float[] data, int offsetSamples)
        {
            if (channels <= 0)
            {
                Debug.Log("AudioClip.GetData failed; AudioClip " + GetName() + " contains no data");
                return false;
            }

            return GetData(this, data.AsSpan(), offsetSamples);
        }

        // Set sample data in a clip. The samples should be float values ranging from -1.0f to 1.0f. Exceeding these limits causes artifacts and undefined behaviour.
        public bool SetData(float[] data, int offsetSamples)
        {
            if (channels <= 0)
            {
                Debug.Log("AudioClip.SetData failed; AudioClip " + GetName() + " contains no data");
                return false;
            }

            if ((offsetSamples < 0) || (offsetSamples >= samples))
                throw new ArgumentException("AudioClip.SetData failed; invalid offsetSamples");

            if ((data == null) || (data.Length == 0))
                throw new ArgumentException("AudioClip.SetData failed; invalid data");

            return SetData(this, data.AsSpan(), offsetSamples);
        }

        // Set sample data in a clip. The samples should be float values ranging from -1.0f to 1.0f. Exceeding these limits causes artifacts and undefined behaviour.
        public unsafe bool SetData(ReadOnlySpan<float> data, int offsetSamples)
        {
            if (channels <= 0)
            {
                Debug.Log("AudioClip.SetData failed; AudioClip " + GetName() + " contains no data");
                return false;
            }

            if ((offsetSamples < 0) || (offsetSamples >= samples))
                throw new ArgumentException("AudioClip.SetData failed; invalid offsetSamples");

            if (data.Length == 0)
                throw new ArgumentException("AudioClip.SetData failed; invalid data");

            return SetData(this, data, offsetSamples);
        }

        /// *listonly*
        [Obsolete("The _3D argument of AudioClip is deprecated. Use the spatialBlend property of AudioSource instead to morph between 2D and 3D playback.")]
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool _3D, bool stream)
        {
            return Create(name, lengthSamples, channels, frequency, stream);
        }

        [Obsolete("The _3D argument of AudioClip is deprecated. Use the spatialBlend property of AudioSource instead to morph between 2D and 3D playback.")]
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool _3D, bool stream, PCMReaderCallback pcmreadercallback)
        {
            return Create(name, lengthSamples, channels, frequency, stream, pcmreadercallback, null);
        }

        [Obsolete("The _3D argument of AudioClip is deprecated. Use the spatialBlend property of AudioSource instead to morph between 2D and 3D playback.")]
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool _3D, bool stream, PCMReaderCallback pcmreadercallback, PCMSetPositionCallback pcmsetpositioncallback)
        {
            return Create(name, lengthSamples, channels, frequency, stream, pcmreadercallback, pcmsetpositioncallback);
        }

        // ================================================================
        // AudioClip.Create() — 运行时程序化创建音频剪辑
        // ================================================================
        // 🎯 在运行时动态创建一个 AudioClip，无需预置资源文件。
        //
        // 💡 参数说明：
        //   name           — AudioClip 名称（用于调试和日志）
        //   lengthSamples  — 总采样点数（不是秒数！）
        //   channels       — 声道数（1=单声道，2=立体声）
        //   frequency      — 采样频率（Hz，如 44100）
        //   stream         — 是否流式播放（需要持续提供数据）
        //
        // 🎯 PCMReaderCallback：
        //   音频系统需要数据时调用此委托。
        //   你可以在此生成正弦波、噪声、程序化音效等。
        //   数组长度 = 期望的采样点数 × 声道数。
        //
        // 🎯 PCMSetPositionCallback：
        //   播放位置变化时调用（用于循环播放逻辑）。
        //
        // ⚠️ 注意：
        //   - lengthSamples 必须 > 0，channels/frequency 必须 > 0
        //   - 运行时创建的 AudioClip 不实现 IAudioGenerator 接口
        //   - 运行时创建的 AudioClip 在场景卸载后需要手动管理生命周期
        // ================================================================
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream)
        {
            AudioClip clip = Create(name, lengthSamples, channels, frequency, stream, null, null);
            return clip;
        }

        /// *listonly*
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream, PCMReaderCallback pcmreadercallback)
        {
            AudioClip clip = Create(name, lengthSamples, channels, frequency, stream, pcmreadercallback, null);
            return clip;
        }

        // Creates a user AudioClip with a name and with the given length in samples, channels and frequency.
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream, PCMReaderCallback pcmreadercallback, PCMSetPositionCallback pcmsetpositioncallback)
        {
            if (name == null) throw new NullReferenceException();
            if (lengthSamples <= 0) throw new ArgumentException("Length of created clip must be larger than 0");
            if (channels <= 0) throw new ArgumentException("Number of channels in created clip must be greater than 0");
            if (frequency <= 0) throw new ArgumentException("Frequency in created clip must be greater than 0");

            AudioClip clip = Construct_Internal();
            if (pcmreadercallback != null)
                clip.m_PCMReaderCallback += pcmreadercallback;
            if (pcmsetpositioncallback != null)
                clip.m_PCMSetPositionCallback += pcmsetpositioncallback;

            clip.CreateUserSound(name, lengthSamples, channels, frequency, stream);

            return clip;
        }

        // ================================================================
        // PCM 回调委托 — 程序化音频生成接口
        // ================================================================
        // 🎯 PCMReaderCallback(float[] data)：
        //   音频系统请求采样数据时由原生代码回调。
        //   你需要用音频数据填充 data 数组（值域 [-1.0, 1.0]）。
        //   对于立体声，左右声道交替排列：[L0, R0, L1, R1, ...]
        //
        // 🎯 PCMSetPositionCallback(int position)：
        //   播放位置回到起点时触发（用于循环逻辑重置状态）。
        //
        // ⚡ 这些回调在音频线程上调用，不要在此执行：
        //   - Unity API 调用（如 Debug.Log、Instantiate）
        //   - 阻塞操作（IO、锁、Sleep）
        //   - 复杂计算（会导致音频卡顿）
        // ================================================================
        /// *listonly*
        public delegate void PCMReaderCallback(float[] data);
        private event PCMReaderCallback m_PCMReaderCallback = null;

        /// *listonly*
        public delegate void PCMSetPositionCallback(int position);
        private event PCMSetPositionCallback m_PCMSetPositionCallback = null;

        [RequiredByNativeCode]
        private void InvokePCMReaderCallback_Internal(float[] data)
        {
            if (m_PCMReaderCallback != null)
                m_PCMReaderCallback(data);
        }

        [RequiredByNativeCode]
        private void InvokePCMSetPositionCallback_Internal(int position)
        {
            if (m_PCMSetPositionCallback != null)
                m_PCMSetPositionCallback(position);
        }

        #region Generator.IAudioGenerator

        void CheckIsNotPersistent()
        {
            if (IsLegacyFormat())
                throw new NotSupportedException($"AudioClip {name} is not a valid {nameof(IAudioGenerator)}. Only persistent {nameof(AudioClip)} can be used, not runtime created ones.");
        }

        bool GeneratorInstance.ICapabilities.isRealtime
        {
            get
            {
                CheckIsNotPersistent();
                return false;
            }
        }

        bool GeneratorInstance.ICapabilities.isFinite
        {
            get
            {
                CheckIsNotPersistent();
                return true;
            }
        }

        DiscreteTime? GeneratorInstance.ICapabilities.length
        {
            get
            {
                CheckIsNotPersistent();
                return DiscreteTime.FromTicks(GeneratorInstance.Configuration.FramesAndSampleRateToDiscreteTimeTicks(samples, (uint)frequency));
            }
        }
        
        public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat, ProcessorInstance.CreationParameters creationParameters)
        {
            CheckIsNotPersistent();

            unsafe
            {
                AudioConfiguration* configPtr = null;

                if (nestedFormat.HasValue)
                {
                    var config = nestedFormat.Value.audioConfiguration;
                    configPtr = &config;
                }

                var header = (GeneratorInstance.GeneratorHeader*)
                    SampleProviderBindings.CreateGeneratorHeader(this, context.Header, configPtr);

                return new GeneratorInstance(header);
            }
        }

        #endregion
    }


    public class AudioBehaviour : Behaviour
    {
    }

    // ================================================================
    // AudioListener - 3D 空间中的音频监听器（"耳朵"）
    // ================================================================
    //
    // 【概述】
    // AudioListener 代表 3D 空间中的听者位置，是音频管线的终点。
    // 所有 AudioSource 的声音最终都要经过 AudioListener 混音后输出。
    //
    // 【继承链】
    // AudioListener -> AudioBehaviour -> Behaviour -> Component -> Object
    //
    // 【在音频管线中的位置】
    //   AudioSource → 空间化处理 → AudioListener → 硬件输出
    //   AudioListener 决定了"听者"在 3D 空间中的位置和朝向。
    //   3D AudioSource 的衰减、方向效果都以 AudioListener 为参考。
    //
    // 【绑定机制】
    //   [StaticAccessor("AudioListenerBindings", StaticAccessorType.DoubleColon)]
    //   → 静态属性/方法路由到 C++ 的 AudioListenerBindings
    //   [RequireComponent(typeof(Transform))] — 必须挂载在有 Transform 的 GameObject 上
    //
    // 【核心属性】
    //   🎯 volume — 全局音量控制（0.0 ~ 1.0），影响所有 AudioSource
    //   🎯 pause  — 暂停整个音频系统（所有声音停止播放）
    //   🎯 velocityUpdateMode — 多普勒效应的更新时机
    //
    // 【音频分析方法】
    //   🎯 GetOutputData()  — 获取最终混音输出的时域波形数据
    //   🎯 GetSpectrumData() — 获取最终混音输出的频域频谱数据
    //   这些方法是实现音频可视化（如均衡器、波形显示）的基础。
    //
    // 【重要限制】
    //   ⚠️ 场景中同一时间只能有一个激活的 AudioListener！
    //   ⚠️ 默认挂载在 Main Camera 上。如果有多相机，
    //      需要禁用非主相机上的 AudioListener。
    //
    // 💡 AudioListener 是 static 类（volume/pause 是静态属性），
    //    这意味着它的行为是全局的，与具体挂载的 GameObject 无关。
    //    position 由 Transform 决定（跟随 GameObject 移动）。
    //
    // 📍 C++ 端：Modules/Audio/Public/AudioListener.h
    // ================================================================
    // Representation of a listener in 3D space.
    [RequireComponent(typeof(Transform))]
    [StaticAccessor("AudioListenerBindings", StaticAccessorType.DoubleColon)]
    public sealed class AudioListener : AudioBehaviour
    {
        [NativeMethod(ThrowsException = true)]
        extern static private void GetOutputDataHelper([Out] float[] samples, int channel);

        [NativeMethod(ThrowsException = true)]
        extern static private void GetSpectrumDataHelper([Out] float[] samples, int channel, FFTWindow window);

        // Controls the game sound volume (0.0 to 1.0)
        extern static public float volume { get; set; }

        // The paused state of the audio. If set to True, the listener will not generate sound.
        [NativeProperty("ListenerPause")]
        extern static public bool pause { get; set; }

        // This lets you set whether the Audio Listener should be updated in the fixed or dynamic update.
        extern public AudioVelocityUpdateMode velocityUpdateMode { get; set; }

        // ================================================================
        // 音频分析方法 — OutputData / SpectrumData
        // ================================================================
        // 🎯 GetOutputData(samples, channel)：
        //   获取 AudioListener 最终混音输出的时域波形数据。
        //   samples 数组被填充为 [-1.0, 1.0] 范围的振幅值。
        //   channel 参数指定要分析的声道（0=混合，1=左，2=右）。
        //
        // 🎯 GetSpectrumData(samples, channel, window)：
        //   对混音输出执行 FFT，获取频域频谱数据。
        //   samples 数组被填充为各频率段的能量值。
        //   window 参数指定 FFT 窗函数类型（影响分析精度）。
        //
        // 💡 应用场景：
        //   - 音频可视化：用 SpectrumData 驱动 UI 频谱条
        //   - 节拍检测：分析低频段能量变化检测鼓点
        //   - 音频响应：根据 OutputData 控制灯光/特效
        //
        // ⚡ 性能提示：
        //   建议每帧仅分析一个声道，使用 256 或 512 个采样点。
        //   samples.Length 必须是 2 的幂（如 256, 512, 1024）。
        //   避免在 Update 中同时获取 OutputData 和 SpectrumData。
        //
        // ⚠️ 已过时的 float[] 返回版本：
        //   GetOutputData(int, int) 和 GetSpectrumData(int, int, FFTWindow)
        //   每次调用都会分配新数组，产生 GC 压力。
        //   请使用传入预分配数组的重载版本。
        // ================================================================

        // Returns a block of the listener (master)'s output data
        [Obsolete("GetOutputData returning a float[] is deprecated, use GetOutputData and pass a pre allocated array instead.")]
        static public float[] GetOutputData(int numSamples, int channel)
        {
            float[] samples = new float[numSamples];
            GetOutputDataHelper(samples, channel);
            return samples;
        }

        // Returns a block of the listener (master)'s output data
        static public void GetOutputData(float[] samples, int channel)
        {
            GetOutputDataHelper(samples, channel);
        }

        // Returns a block of the listener (master)'s spectrum data
        [Obsolete("GetSpectrumData returning a float[] is deprecated, use GetSpectrumData and pass a pre allocated array instead.")]
        static public float[] GetSpectrumData(int numSamples, int channel, FFTWindow window)
        {
            float[] samples = new float[numSamples];
            GetSpectrumDataHelper(samples, channel, window);
            return samples;
        }

        // Returns a block of the listener (master)'s spectrum data
        static public void GetSpectrumData(float[] samples, int channel, FFTWindow window)
        {
            GetSpectrumDataHelper(samples, channel, window);
        }
    }

    // ================================================================
    // AudioSource - 3D 空间音频发射源（音频播放器）
    // ================================================================
    //
    // 【概述】
    // AudioSource 是 Unity 音频播放的核心组件，挂载到 GameObject 上后，
    // 可以在 3D 空间中播放 AudioClip，并根据距离和方向自动处理音量衰减。
    // 它是连接 AudioClip（数据）和 AudioListener（听者）之间的桥梁。
    //
    // 【继承链】
    // AudioSource -> AudioBehaviour -> Behaviour -> Component -> Object
    //   sealed class，不可继承。
    //
    // 【在音频管线中的位置】
    //   AudioClip（音频数据）
    //       ↓ clip 属性引用
    //   AudioSource（播放控制 + 空间化处理）
    //       ↓ Transform.position 作为声源位置
    //   3D 空间计算（距离衰减、方向、多普勒）
    //       ↓
    //   AudioListener（听者位置）→ AudioOutput（硬件）
    //
    // 【绑定机制】
    //   [StaticAccessor("AudioSourceBindings", StaticAccessorType.DoubleColon)]
    //   → 属性/方法通过 C++ AudioSourceBindings 路由
    //   [RequireComponent(typeof(Transform))] — 必须有 Transform（位置信息来源）
    //
    // 【核心功能分组】
    //   1. 播放控制：Play/Stop/Pause/PlayOneShot/PlayScheduled
    //   2. 音频属性：volume/pitch/loop/mute/priority
    //   3. 3D 空间化：spatialBlend/minDistance/maxDistance/rolloffMode
    //   4. 高级效果：dopplerLevel/spread/reverbZoneMix/spatialize
    //   5. 音频分析：GetOutputData/GetSpectrumData
    //   6. 手柄输出：PlayOnGamepad/SetGamepadSpeakerMixLevel（主机平台）
    //
    // 【2D vs 3D 播放】
    //   🎯 spatialBlend = 0.0 → 纯 2D 播放（BGM、UI 音效）
    //   🎯 spatialBlend = 1.0 → 纯 3D 播放（3D 环境音效）
    //   🎯 spatialBlend = 0.5 → 混合模式（半空间化效果）
    //
    // 📍 C++ 端：Modules/Audio/Public/AudioSource.h
    // ================================================================
    // A representation of audio sources in 3D.
    [RequireComponent(typeof(Transform))]
    [StaticAccessor("AudioSourceBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class AudioSource : AudioBehaviour
    {
        extern static private float GetPitch([NotNull] AudioSource source);
        extern static private void SetPitch([NotNull] AudioSource source, float pitch);

        extern static private void PlayHelper([NotNull] AudioSource source, UInt64 delay);
        extern private void Play(double delay);

        extern static private void PlayOneShotHelper([NotNull] AudioSource source, [NotNull] AudioClip clip, float volumeScale);

        extern private void Stop(bool stopOneShots);

        [NativeMethod(ThrowsException = true)]
        extern static private void SetCustomCurveHelper([NotNull] AudioSource source, AudioSourceCurveType type, AnimationCurve curve);
        extern static private AnimationCurve GetCustomCurveHelper([NotNull] AudioSource source, AudioSourceCurveType type);

        extern static private void GetOutputDataHelper([NotNull] AudioSource source, [Out] float[] samples, int channel);
        [NativeMethod(ThrowsException = true)]
        extern static private void GetSpectrumDataHelper([NotNull] AudioSource source, [Out] float[] samples, int channel, FFTWindow window);

        // ================================================================
        // AudioSource 基础属性 — 音量与音高
        // ================================================================
        // 🎯 volume (0.0 ~ 1.0)：
        //   控制 AudioSource 的输出音量。
        //   最终音量 = AudioSource.volume × AudioListener.volume × 距离衰减。
        //
        // 🎯 pitch (默认 1.0)：
        //   控制播放速度和音高。pitch = 2.0 表示 2 倍速播放（音高翻倍）。
        //   pitch = 0.5 表示半速播放（音高低一个八度）。
        //
        // 💡 pitch 的 C# 封装：
        //   C# 端不直接 extern 属性，而是通过 GetPitch/SetPitch 静态方法。
        //   这是因为 pitch 设置需要同步到原生音频引擎的播放速率参数。
        //
        // ⚡ 性能提示：
        //   pitch 变化会触发原生端的重采样计算，频繁变更有开销。
        //   如需频繁变速，考虑使用 AudioSource.pitch 缓动而非逐帧设置。
        // ================================================================

        // The volume of the audio source (0.0 to 1.0)
        extern public float volume { get; set; }

        // The pitch of the audio source.
        public float pitch
        {
            get { return GetPitch(this); }
            set { SetPitch(this, value); }
        }

        // ================================================================
        // 播放位置 — time / timeSamples
        // ================================================================
        // 🎯 time（秒）：
        //   获取/设置当前播放位置（秒）。
        //   可用于跳转到指定时间点（如 seek 到歌曲副歌部分）。
        //
        // 🎯 timeSamples（采样点）：
        //   获取/设置当前播放位置（采样点数）。
        //   精度更高（= time × frequency），适合精确音频操作。
        //
        // 💡 两者关系：
        //   timeSamples = time × frequency
        //   time = timeSamples / frequency
        //
        // ⚡ timeSamples 标记了 [IsThreadSafe = true]，
        //    可以安全地从非主线程读写（用于音频线程回调）。
        // ================================================================

        // Playback position in seconds.
        [NativeProperty("SecPosition")]
        extern public float time { get; set; }

        // Playback position in PCM samples.
        [NativeProperty("SamplePosition")]
        extern public int timeSamples
        {
            [NativeMethod(IsThreadSafe = true)]
            get;

            [NativeMethod(IsThreadSafe = true)]
            set;
        }

        // ================================================================
        // AudioSource 数据源 — clip / resource / generator
        // ================================================================
        // 🎯 clip（AudioClip）：
        //   最常用的音频数据源引用。
        //   设置 clip = xxx 会自动调用 generatorObject setter。
        //
        // 🎯 resource（AudioResource）：
        //   更通用的数据源引用，支持 AudioResource 及其子类。
        //   未来将支持 AudioContainer 等新音频资源类型。
        //
        // 🎯 generator（IAudioGenerator）：
        //   接口级别的数据源引用，支持所有实现了 IAudioGenerator 的对象。
        //
        // 💡 三者的关系：
        //   clip ⊂ resource ⊂ generator
        //   clip 是最具体的（只能是 AudioClip）。
        //   resource 更通用（可以是 AudioResource 任何子类）。
        //   generator 是最抽象的（任何 IAudioGenerator 实现）。
        //
        // ⚠️ 设置 clip 为 null 会停止播放。
        //    source.clip = null ≠ source.Stop()（后者会触发停止回调）。
        // ================================================================

        // The default [[AudioClip]] to play
        public AudioClip clip
        {
            get => generatorObject as AudioClip;
            set => generatorObject = value;
        }

        public AudioResource resource
        {
            get => generatorObject as AudioResource;
            set => generatorObject = value;
        }

        public IAudioGenerator generator
        {
            // These shall always succeed
            get => (IAudioGenerator)generatorObject;
            set => generatorObject = (Object)value;
        }

        public unsafe ProcessorInstance generatorInstance
        {
            get
            {
                var header = (GeneratorInstance.GeneratorHeader*)generatorHeader;

                if (header != null)
                    return new GeneratorInstance(header);

                return default;
            }
        }

        extern internal unsafe void* generatorHeader { get; }

        extern internal Object generatorObject { get; set; }

        // ================================================================
        // 混音器路由与效果旁路 — outputAudioMixerGroup / bypass*
        // ================================================================
        // 🎯 outputAudioMixerGroup：
        //   将 AudioSource 的输出路由到 AudioMixer 的指定分组。
        //   通过 AudioMixer 可以对不同分组应用不同的效果和音量控制。
        //   例如：将所有 BGM 路由到 Music 分组，音效路由到 SFX 分组。
        //
        // 🎯 bypassEffects — 旁路 AudioSource 上的音效滤波器
        //   设为 true 时跳过所有挂载在 AudioSource 上的滤波器组件。
        //
        // 🎯 bypassListenerEffects — 旁路 AudioListener 上的音效滤波器
        //   设为 true 时跳过所有挂载在 AudioListener 上的滤波器。
        //
        // 🎯 bypassReverbZones — 旁路混响区域效果
        //   设为 true 时不受 AudioReverbZone 的混响影响。
        //
        // 💡 音频处理链路（从上到下）：
        //   AudioSource.clip → AudioSource 滤波器 → ReverbZone 混响
        //   → AudioListener 滤波器 → AudioListener 输出 → 硬件
        //   bypass* 属性可以跳过链路中的特定环节。
        // ================================================================

        extern public AudioMixerGroup outputAudioMixerGroup { get; set; }

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::PlayOnDualShock4", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use PlayOnGamepad instead")]
        extern public bool PlayOnDualShock4(Int32 userId);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetDualShock4SpeakerMixLevel", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerMixLevel instead")]
        extern public bool SetDualShock4PadSpeakerMixLevel(Int32 userId, Int32 mixLevel);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetDualShock4SpeakerMixLevelDefault", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerMixLevelDefault instead")]
        extern public bool SetDualShock4PadSpeakerMixLevelDefault(Int32 userId);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetDualShock4SpeakerRestrictedAudio", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetgamepadSpeakerRestrictedAudio instead")]
        extern public bool SetDualShock4PadSpeakerRestrictedAudio(Int32 userId, bool restricted);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::PlayOnGamepad", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use PlayOnGamepad instead")]
        extern public bool PlayOnDualShock4PadIndex(Int32 slot);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::DisableGamepadOutput", HasExplicitThis = true)]
        [Obsolete("Use DisableGamepadOutput instead")]
        extern public bool DisableDualShock4Output();

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevel", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerMixLevel instead")]
        extern public bool SetDualShock4PadSpeakerMixLevelPadIndex(Int32 slot, Int32 mixLevel);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevelDefault", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerMixLevelDefault instead")]
        extern public bool SetDualShock4PadSpeakerMixLevelDefaultPadIndex(Int32 slot);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerRestrictedAudio", HasExplicitThis = true, ThrowsException = true)]
        [Obsolete("Use SetGamepadSpeakerRestrictedAudio instead")]
        extern public bool SetDualShock4PadSpeakerRestrictedAudioPadIndex(Int32 slot, bool restricted);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::PlayOnGamepad", HasExplicitThis = true, ThrowsException = true)]
        extern public bool PlayOnGamepad(Int32 slot);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::DisableGamepadOutput", HasExplicitThis = true)]
        extern public bool DisableGamepadOutput();

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevel", HasExplicitThis = true, ThrowsException = true)]
        extern public bool SetGamepadSpeakerMixLevel(Int32 slot, Int32 mixLevel);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerMixLevelDefault", HasExplicitThis = true, ThrowsException = true)]
        extern public bool SetGamepadSpeakerMixLevelDefault(Int32 slot);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "AudioSourceBindings::SetGamepadSpeakerRestrictedAudio", HasExplicitThis = true, ThrowsException = true)]
        extern public bool SetGamepadSpeakerRestrictedAudio(Int32 slot, bool restricted);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        [NativeMethod(Name = "GamepadSpeakerSupportsOutputType", HasExplicitThis = false)]
        extern static public bool GamepadSpeakerSupportsOutputType(GamepadSpeakerOutputType outputType);

        [NativeConditional("PLATFORM_SUPPORTS_GAMEPAD_AUDIO")]
        extern public GamepadSpeakerOutputType gamepadSpeakerOutputType { get; set; }

        // ================================================================
        // 播放控制方法 — Play / PlayDelayed / PlayScheduled / PlayOneShot
        // ================================================================
        // 🎯 Play() — 立即开始播放当前 clip。
        //   delay 参数（已过时）指定采样点级别的延迟。
        //
        // 🎯 PlayDelayed(delay) — 延迟指定秒数后播放。
        //   内部将秒数转换为负值传递给 Play(delay)，
        //   负值表示"延迟 N 秒后开始"。
        //
        // 🎯 PlayScheduled(time) — 在绝对 DSP 时间点播放。
        //   🎯 这是精确音频调度的推荐方式！
        //   基于 AudioSettings.dspTime，不受帧率影响。
        //   适合音乐节拍同步、交叉淡入淡出等精确场景。
        //
        // 🎯 PlayOneShot(clip, volumeScale) — 一次性播放剪辑。
        //   不影响 AudioSource 的 clip 属性，可以快速连续播放。
        //   volumeScale 缩放当前音量（默认 1.0）。
        //   适合：枪声、脚步声、碰撞音效等短促音效。
        //
        // 💡 Play() vs PlayOneShot()：
        //   Play() 会替换当前 clip 并开始播放。
        //   PlayOneShot() 叠加播放，不影响当前播放状态。
        //   同一个 AudioSource 只能同时播放一个 clip，
        //   但可以叠加多个 OneShot（通过创建临时 AudioSource）。
        //
        // ⚠️ Play() 在 clip 为 null 时不会报错但也不会播放。
        //    PlayOneShot() 在 clip 为 null 时会打印警告。
        // ================================================================

        // Plays the ::ref::clip with a certain delay (the optional delay argument is deprecated since 4.1a3) and the functionality has been replaced by PlayDelayed.
        [ExcludeFromDocs]
        public void Play()
        {
            PlayHelper(this, 0);
        }

        public void Play([UnityEngine.Internal.DefaultValue("0")] UInt64 delay)
        {
            PlayHelper(this, delay);
        }

        // Plays the ::ref::clip with a delay specified in seconds. Users are advised to use this function instead of the old Play(delay) function that took a delay specified in samples relative to a reference rate of 44.1 kHz as an argument.
        public void PlayDelayed(float delay)
        {
            Play((delay < 0.0f) ? 0.0 : -(double)delay);
        }

        // Schedules the ::ref::clip to play at the specified absolute time. This is the preferred way to stitch AudioClips in music players because it is independent of the frame rate and gives the audio system enough time to prepare the playback of the sound to fetch it from media where the opening and buffering takes a lot of time (streams) without causing sudden performance peaks.
        public void PlayScheduled(double time)
        {
            Play((time < 0.0) ? 0.0 : time);
        }

        // Plays an [[AudioClip]], and scales the [[AudioSource]] volume by volumeScale.
        [ExcludeFromDocs]
        public void PlayOneShot(AudioClip clip)
        {
            PlayOneShot(clip, 1.0f);
        }

        public void PlayOneShot(AudioClip clip, [UnityEngine.Internal.DefaultValue("1.0F")] float volumeScale)
        {
            if (clip == null)
            {
                Debug.LogWarning("PlayOneShot was called with a null AudioClip.");
                return;
            }

            PlayOneShotHelper(this, clip, volumeScale);
        }

        // ================================================================
        // 定时调度与播放控制 — SetScheduledStartTime/EndTime, Stop/Pause/UnPause
        // ================================================================
        // 🎯 SetScheduledStartTime(time)：
        //   设置已计划播放的开始时间（DSP 绝对时间）。
        //   配合 PlayScheduled() 使用，可以在播放前调整起始点。
        //
        // 🎯 SetScheduledEndTime(time)：
        //   设置已计划播放的结束时间。
        //   用于精确控制播放时长（如淡出效果）。
        //
        // 🎯 Stop() — 停止播放并将播放位置重置到开头。
        //   内部调用 Stop(stopOneShots: true)，同时停止所有 OneShot。
        //
        // 🎯 Pause() — 暂停播放（保留播放位置）。
        //   调用 UnPause() 或 Play() 可以从暂停位置继续。
        //
        // 🎯 UnPause() — 从暂停位置继续播放。
        //   与 Play() 的区别：Play() 会重新开始，UnPause() 接续之前的位置。
        //
        // 💡 典型用法模式：
        //   source.PlayScheduled(AudioSettings.dspTime + 1.0);  // 1秒后播放
        //   source.SetScheduledEndTime(AudioSettings.dspTime + 3.0);  // 3秒后停止
        // ================================================================

        extern public void SetScheduledStartTime(double time);
        extern public void SetScheduledEndTime(double time);

        // Stops playing the ::ref::clip.
        public void Stop()
        {
            Stop(true);
        }

        // Pauses playing the ::ref::clip.
        extern public void Pause();

        // Unpauses the paused source, different from play in that it does not start any new playback.
        extern public void UnPause();

        // Calls skip on any AudioContainers on the source
        internal extern void SkipToNextElementIfHasContainer();

        // Is the ::ref::clip playing right now (RO)?
        extern public bool isPlaying
        {
            [NativeName("IsPlayingScripting")]
            get;
        }

        internal extern bool isContainerPlaying
        {
            [NativeName("IsContainerPlaying")]
            get;
        }

        internal extern ActivePlayable[] containerActivePlayables { get; }

        extern public bool isVirtual
        {
            [NativeName("GetLastVirtualState")]
            get;
        }

        // ================================================================
        // PlayClipAtPoint — 一次性位置播放（静态方法）
        // ================================================================
        // 🎯 在指定世界坐标位置播放一次 AudioClip，播放完毕自动销毁。
        //
        // 💡 内部实现：
        //   1. 创建临时 GameObject（"One shot audio"）
        //   2. 设置位置为 position
        //   3. 添加 AudioSource 组件
        //   4. 设置 clip、spatialBlend=1.0（纯 3D）、volume
        //   5. 调用 Play() 开始播放
        //   6. 延迟 Destroy(go, length * timeScale) 销毁对象
        //
        // ⚠️ timeScale 处理的注意事项：
        //   - timeScale > 1（加速）：声音仍以正常速度播放，
        //     所以需要延长 Destroy 延迟以覆盖实际播放时间
        //   - timeScale ≈ 0：浮点精度问题可能导致过早销毁
        //   - timeScale = 0：对象会被立即销毁（无法播放）
        //   - timeScale < 0.01 时使用 0.01 作为最小值避免精度问题
        //
        // 💡 使用场景：
        //   适合偶尔播放的 3D 音效（如爆炸、撞击）。
        //   不适合频繁播放（每次都会创建/销毁 GameObject，GC 压力大）。
        //   频繁播放应使用对象池 + AudioSource 组件。
        // ================================================================

        // Plays the clip at position. Automatically cleans up the audio source after it has finished playing.
        [ExcludeFromDocs]
        static public void PlayClipAtPoint(AudioClip clip, Vector3 position)
        {
            PlayClipAtPoint(clip, position, 1.0f);
        }

        static public void PlayClipAtPoint(AudioClip clip, Vector3 position, [UnityEngine.Internal.DefaultValue("1.0F")] float volume)
        {
            GameObject go = new GameObject("One shot audio");
            go.transform.position = position;
            AudioSource source = (AudioSource)go.AddComponent(typeof(AudioSource));
            source.clip = clip;
            source.spatialBlend = 1.0f;
            source.volume = volume;
            source.Play();

            // Note: timeScale > 1 means that game time is accelerated. However, the sounds play at their normal speed,
            // so we need to postpone the point in time, when the sound is stopped.
            // Conversly, when timescale approaches 0, the inaccuracies of float precision mean that it kills the sound early
            // Also when timescale is 0, the object is destroyed immediately.
            // Note: The behaviour here means that when the timescale is 0, GameObjects will pile up until the timescale
            // is taken above 0 again.
            Destroy(go, clip.length * (Time.timeScale < 0.01f ? 0.01f : Time.timeScale));
        }

        // Is the audio clip looping?
        extern public bool loop { get; set; }

        // This makes the audio source not take into account the volume of the audio listener.
        extern public bool ignoreListenerVolume { get; set; }

        // If set to true, the audio source will automatically start playing on awake
        extern public bool playOnAwake { get; set; }

        // If set to true, the audio source will be playable while the AudioListener is paused
        extern public bool ignoreListenerPause { get; set; }

        // Whether the Audio Source should be updated in the fixed or dynamic update.
        extern public AudioVelocityUpdateMode velocityUpdateMode { get; set; }

        // Sets how a Mono or 2D sound is panned linearly to the left or right.
        [NativeProperty("StereoPan")]
        extern public float panStereo { get; set; }

        // ================================================================
        // 3D 空间化属性 — 空间混合、衰减、多普勒
        // ================================================================
        // 🎯 spatialBlend（空间混合）— 最重要的 3D 属性！
        //   0.0 = 纯 2D：不受距离/方向影响，始终等音量（适合 BGM）
        //   1.0 = 纯 3D：受距离衰减、方向、多普勒效应完全影响
        //   中间值 = 混合：部分空间化效果
        //
        // 🎯 minDistance — 最近距离阈值
        //   在 minDistance 范围内，音量保持最大不衰减。
        //   默认值 = 1.0 单位。
        //
        // 🎯 maxDistance — 最远距离阈值
        //   Logarithmic 模式下，超出 maxDistance 完全静音。
        //   Linear 模式下，maxDistance 处音量恰好降为 0。
        //   默认值 = 500.0 单位。
        //
        // 🎯 rolloffMode — 衰减曲线类型
        //   Logarithmic — 真实物理衰减（距离翻倍，降 6dB）
        //   Linear — 匀速线性衰减（游戏常用）
        //   Custom — 自定义 AnimationCurve 衰减
        //
        // 🎯 dopplerLevel — 多普勒效应强度
        //   0.0 = 无多普勒效应
        //   1.0 = 真实物理多普勒
        //   > 1.0 = 夸张效果（适合科幻游戏）
        //   🎯 多普勒效应：物体靠近时音高升高，远离时音高降低
        //
        // 🎯 spread — 声音扩散角度（度）
        //   0° = 声音从单一点发出（点声源）
        //   360° = 声音从所有方向均匀发出（环境声）
        //   影响立体声宽度随距离的变化。
        //
        // 💡 SetCustomCurve() 可以为上述属性设置自定义曲线：
        //   CustomRolloff — 自定义衰减曲线
        //   SpatialBlend — 自定义空间混合曲线
        //   ReverbZoneMix — 自定义混响混合曲线
        //   Spread — 自定义扩散角度曲线
        // ================================================================

        // Sets how much a playing sound is treated as a 3D source
        [NativeProperty("SpatialBlendMix")]
        extern public float spatialBlend { get; set; }

        // Enables/disables custom spatialization
        extern public bool spatialize { get; set; }

        // Determines if the spatializer effect is inserted before or after the effect filters.
        extern public bool spatializePostEffects { get; set; }

        public void SetCustomCurve(AudioSourceCurveType type, AnimationCurve curve)
        {
            SetCustomCurveHelper(this, type, curve);
        }

        public AnimationCurve GetCustomCurve(AudioSourceCurveType type)
        {
            return GetCustomCurveHelper(this, type);
        }

        // Sets how much a playing sound is mixed into the reverb zones
        extern public float reverbZoneMix { get; set; }

        // Bypass effects
        extern public bool bypassEffects { get; set; }

        // Bypass listener effects
        extern public bool bypassListenerEffects { get; set; }

        // Bypass reverb zones
        extern public bool bypassReverbZones { get; set; }

        // Sets the Doppler scale for this AudioSource
        extern public float dopplerLevel { get; set; }

        // Sets the spread angle a 3d stereo or multichannel sound in speaker space.
        extern public float spread { get; set; }

        // Sets the priority of the [[AudioSource]]
        extern public int priority { get; set; }

        // Un- / Mutes the AudioSource. Mute sets the volume=0, Un-Mute restore the original volume.
        extern public bool mute { get; set; }

        // Within the Min distance the AudioSource will cease to grow louder in volume.
        extern public float minDistance { get; set; }

        // (Logarithmic rolloff) MaxDistance is the distance a sound stops attenuating at.
        extern public float maxDistance { get; set; }

        // Sets/Gets how the AudioSource attenuates over distance
        extern public AudioRolloffMode rolloffMode { get; set; }

        // ================================================================
        // AudioSource 音频分析 — OutputData / SpectrumData
        // ================================================================
        // 🎯 与 AudioListener 的同名方法类似，但分析的是单个 AudioSource
        //    的输出（而非最终混音输出）。
        //
        // 💡 GetOutputData(samples, channel)：
        //   获取此 AudioSource 当前输出的时域波形数据。
        //   可用于实现单个音源的波形可视化。
        //
        // 💡 GetSpectrumData(samples, channel, window)：
        //   对此 AudioSource 的输出执行 FFT 分析。
        //   可用于实现单个音源的频谱分析。
        //
        // ⚠️ 已过时的 float[] 返回版本会产生 GC 分配，
        //    请使用传入预分配数组的重载版本。
        // ================================================================

        // Returns a block of the currently playing source's output data
        [Obsolete("GetOutputData returning a float[] is deprecated, use GetOutputData and pass a pre allocated array instead.")]
        public float[] GetOutputData(int numSamples, int channel)
        {
            float[] samples = new float[numSamples];
            GetOutputDataHelper(this, samples, channel);
            return samples;
        }

        // Returns a block of the currently playing source's output data
        public void GetOutputData(float[] samples, int channel)
        {
            GetOutputDataHelper(this, samples, channel);
        }

        // Returns a block of the currently playing source's spectrum data
        [Obsolete("GetSpectrumData returning a float[] is deprecated, use GetSpectrumData and pass a pre allocated array instead.")]
        public float[] GetSpectrumData(int numSamples, int channel, FFTWindow window)
        {
            float[] samples = new float[numSamples];
            GetSpectrumDataHelper(this, samples, channel, window);
            return samples;
        }

        // Returns a block of the currently playing source's spectrum data
        public void GetSpectrumData(float[] samples, int channel, FFTWindow window)
        {
            GetSpectrumDataHelper(this, samples, channel, window);
        }

        [Obsolete("minVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead.", true)]
        public float minVolume
        {
            get { Debug.LogError("minVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); return 0.0f; }
            set { Debug.LogError("minVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); }
        }

        [Obsolete("maxVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead.", true)]
        public float maxVolume
        {
            get { Debug.LogError("maxVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); return 0.0f; }
            set { Debug.LogError("maxVolume is not supported anymore. Use min-, maxDistance and rolloffMode instead."); }
        }

        [Obsolete("rolloffFactor is not supported anymore. Use min-, maxDistance and rolloffMode instead.", true)]
        public float rolloffFactor
        {
            get { Debug.LogError("rolloffFactor is not supported anymore. Use min-, maxDistance and rolloffMode instead."); return 0.0f; }
            set { Debug.LogError("rolloffFactor is not supported anymore. Use min-, maxDistance and rolloffMode instead."); }
        }

        // ================================================================
        // 自定义空间化器参数 — SetSpatializerFloat / GetSpatializerFloat
        // ================================================================
        // 🎯 与第三方空间化插件（如 Oculus Spatializer、Resonance Audio）
        //    进行参数交互的接口。
        //
        // 💡 SetSpatializerFloat(index, value)：
        //   向空间化器插件写入自定义参数。
        //   index 和 value 的含义由具体插件定义。
        //
        // 💡 GetSpatializerFloat(index, out value)：
        //   从空间化器插件读取自定义参数。
        //   返回 true 表示读取成功。
        //
        // 🎯 Ambisonic 解码器也有类似的 Float 接口：
        //   GetAmbisonicDecoderFloat / SetAmbisonicDecoderFloat
        //   用于 Ambisonics 全景声格式的参数控制。
        //
        // ⚠️ 这些方法仅在使用对应插件时有意义。
        //    未启用空间化插件时调用会返回 false。
        // ================================================================

        extern public bool SetSpatializerFloat(int index, float value);
        extern public bool GetSpatializerFloat(int index, out float value);

        extern public bool GetAmbisonicDecoderFloat(int index, out float value);
        extern public bool SetAmbisonicDecoderFloat(int index, float value);

        extern internal float GetAudioRandomContainerRuntimeMeterValue();
    }

    // ================================================================
    // AudioReverbZone - 混响区域（环境音效模拟）
    // ================================================================
    //
    // 【概述】
    // AudioReverbZone 在 3D 空间中定义一个混响效果区域。
    // 当 AudioListener 进入此区域时，AudioSource 会自动叠加对应的混响效果。
    // 用于模拟不同环境的声学特性（如大厅回声、浴室反射）。
    //
    // 【工作原理】
    //   1. 在场景中放置 AudioReverbZone 并设置中心位置
    //   2. 定义 minDistance（完全效果范围）和 maxDistance（影响范围边界）
    //   3. AudioListener 在 maxDistance 外：无混响效果
    //   4. AudioListener 在 minDistance ~ maxDistance 之间：混响逐渐增强
    //   5. AudioListener 在 minDistance 内：混响达到最大强度
    //
    // 【混响参数】
    //   reverbPreset — 快速选择预设（Room/Bathroom/Auditorium 等）
    //   room/roomHF/roomLF — 房间反射级别
    //   decayTime/decayHFRatio — 混响衰减时间
    //   reflections/reflectionsDelay — 早期反射
    //   reverb/reverbDelay — 后期混响
    //   diffusion/density — 扩散和密度
    //
    // 💡 场景设计建议：
    //   每个房间/区域放置一个 AudioReverbZone，重叠区域自动混合。
    //   注意：场景中可以有多个 ReverbZone，效果会叠加。
    // ================================================================
    // Reverb Zones are used when you want to gradually change from a point
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Audio/Public/AudioReverbZone.h")]
    public sealed class AudioReverbZone : Behaviour
    {
        //  The distance from the centerpoint that the reverb will have full effect at. Default = 10.0.
        extern public float minDistance { get; set; }

        //  The distance from the centerpoint that the reverb will not have any effect. Default = 15.0.
        extern public float maxDistance { get; set; }

        // Set/Get reverb preset properties
        extern public AudioReverbPreset reverbPreset { get; set; }

        // room effect level (at mid frequencies)
        extern public int room { get; set; }

        // relative room effect level at high frequencies
        extern public int roomHF { get; set; }

        // relative room effect level at low frequencies
        extern public int roomLF { get; set; }

        // reverberation decay time at mid frequencies
        extern public float decayTime { get; set; }

        //  high-frequency to mid-frequency decay time ratio
        extern public float decayHFRatio { get; set; }

        // early reflections level relative to room effect
        extern public int reflections { get; set; }

        //  initial reflection delay time
        extern public float reflectionsDelay { get; set; }

        // late reverberation level relative to room effect
        extern public int reverb { get; set; }

        //  late reverberation delay time relative to initial reflection
        extern public float reverbDelay { get; set; }

        //  reference high frequency (hz)
        extern public float HFReference { get; set; }

        // reference low frequency (hz)
        extern public float LFReference { get; set; }

        // like rolloffscale in global settings, but for reverb room size effect
        [Obsolete("Warning! roomRolloffFactor is no longer supported.")]
        public float roomRolloffFactor
        {
            get { Debug.LogWarning("Warning! roomRolloffFactor is no longer supported."); return 10.0f; }
            set { Debug.LogWarning("Warning! roomRolloffFactor is no longer supported."); }
        }

        // Value that controls the echo density in the late reverberation decay
        extern public float diffusion { get; set; }

        // Value that controls the modal density in the late reverberation decay
        extern public float density { get; set; }

        extern internal bool active { get; set; }
    }

    // ================================================================
    // 音频滤波器组件 — AudioLowPassFilter / HighPass / Distortion / Echo / Chorus / Reverb
    // ================================================================
    //
    // 【概述】
    // 这些组件通过挂载到 AudioSource 或 AudioListener 上来修改音频信号。
    // 每个滤波器都修改音频管线中的特定频段或添加效果。
    //
    // 【音频处理链路】
    //   AudioSource.clip → AudioLowPassFilter → AudioHighPassFilter
    //   → AudioDistortionFilter → AudioEchoFilter → AudioChorusFilter
    //   → AudioReverbFilter → AudioListener → 输出
    //   滤波器按挂载顺序依次处理（同一 GameObject 上按组件顺序）。
    //
    // 【各滤波器功能】
    //   🎯 AudioLowPassFilter  — 低通滤波：允许低频通过，衰减高频
    //                           用途：水下效果、墙壁遮挡、远处模糊声
    //   🎯 AudioHighPassFilter — 高通滤波：允许高频通过，衰减低频
    //                           用途：电话音效、收音机效果、去除低频隆隆声
    //   🎯 AudioDistortionFilter — 失真效果：对信号施加非线性失真
    //                              用途：机器人声、爆炸过载、老式喇叭
    //   🎯 AudioEchoFilter — 回声效果：重复播放延迟衰减的声音
    //                       用途：山谷回声、空旷大厅
    //   🎯 AudioChorusFilter — 合唱效果：叠加延迟和音高变化的多声道
    //                         用途：合唱团、梦幻效果、加厚人声
    //   🎯 AudioReverbFilter — 混响效果：模拟空间反射
    //                         用途：室内环境音效
    //
    // ⚠️ 注意：
    //   所有滤波器都要求 [RequireComponent(typeof(AudioBehaviour))]，
    //   即必须和 AudioSource 或 AudioListener 挂载在同一 GameObject 上。
    // ================================================================

    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioLowPassFilter : Behaviour
    {
        extern private AnimationCurve GetCustomLowpassLevelCurveCopy();

        [NativeMethod(Name = "AudioLowPassFilterBindings::SetCustomLowpassLevelCurveHelper", IsFreeFunction = true, ThrowsException = true)]
        extern static private void SetCustomLowpassLevelCurveHelper([NotNull] AudioLowPassFilter source, AnimationCurve curve);

        public AnimationCurve customCutoffCurve
        {
            get { return GetCustomLowpassLevelCurveCopy(); }
            set { SetCustomLowpassLevelCurveHelper(this, value); }
        }

        // Lowpass cutoff frequency in hz. 10.0 to 22000.0. Default = 5000.0.
        extern public float cutoffFrequency { get; set; }

        // Determines how much the filter's self-resonance is dampened.
        extern public float lowpassResonanceQ { get; set; }
    }

    // ================================================================
    // AudioLowPassFilter - 低通滤波器
    // ================================================================
    // 🎯 允许低于 cutoffFrequency 的频率通过，衰减高于 cutoffFrequency 的频率。
    //
    // 💡 cutoffFrequency（10.0 ~ 22000.0 Hz，默认 5000.0）：
    //   频率低于此值的信号基本不受影响。
    //   频率高于此值的信号被逐渐衰减。
    //
    // 💡 lowpassResonanceQ — 共振品质因数：
    //   Q 值越高，截止频率处的共振峰越尖锐（增强截止频率附近的声音）。
    //   Q = 0 时无共振，信号平滑衰减。
    //
    // 💡 customCutoffCurve — 自定义截止频率曲线：
    //   用 AnimationCurve 控制 cutoffFrequency 随时间的变化。
    //   曲线 X 轴为归一化时间 [0,1]，Y 轴为频率值。
    //
    // 🎯 典型用途：
    //   - 水下效果：将 cutoff 降至 500Hz 模拟水的滤波
    //   - 墙壁遮挡：根据遮挡程度动态调整 cutoff
    //   - 距离衰减：远处声音高频损失更严重
    // ================================================================

    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioHighPassFilter : Behaviour
    {
        // Highpass cutoff frequency in hz. 10.0 to 22000.0. Default = 5000.0.
        extern public float cutoffFrequency { get; set; }

        // Determines how much the filter's self-resonance isdampened.
        extern public float highpassResonanceQ { get; set; }
    }

    // The Audio Distortion Filter distorts the sound from an AudioSource or
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed class AudioDistortionFilter : Behaviour
    {
        // Distortion value. 0.0 to 1.0. Default = 0.5.
        extern public float distortionLevel { get; set; }
    }

    // The Audio Echo Filter repeats a sound after a given Delay, attenuating
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed class AudioEchoFilter : Behaviour
    {
        // Echo delay in ms. 10 to 5000. Default = 500.
        extern public float delay { get; set; }

        // Echo decay per delay. 0 to 1. 1.0 = No decay, 0.0 = total decay (i.e. simple 1 line delay). Default = 0.5.
        extern public float decayRatio { get; set; }

        // Volume of original signal to pass to output. 0.0 to 1.0. Default = 1.0.
        extern public float dryMix { get; set; }

        // Volume of echo signal to pass to output. 0.0 to 1.0. Default = 1.0.
        extern public float wetMix { get; set; }
    }

    // The Audio Chorus Filter takes an Audio Clip and processes it creating a chorus effect.
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed class AudioChorusFilter : Behaviour
    {
        // Volume of original signal to pass to output. 0.0 to 1.0. Default = 0.5.
        extern public float dryMix { get; set; }

        // Volume of 1st chorus tap. 0.0 to 1.0. Default = 0.5.
        extern public float wetMix1 { get; set; }

        // Volume of 2nd chorus tap. This tap is 90 degrees out of phase of the first tap. 0.0 to 1.0. Default = 0.5.
        extern public float wetMix2 { get; set; }

        // Volume of 3rd chorus tap. This tap is 90 degrees out of phase of the second tap. 0.0 to 1.0. Default = 0.5.
        extern public float wetMix3 { get; set; }

        // Chorus delay in ms. 0.1 to 100.0. Default = 40.0 ms.
        extern public float delay { get; set; }

        // Chorus modulation rate in hz. 0.0 to 20.0. Default = 0.8 hz.
        extern public float rate { get; set; }

        //  Chorus modulation depth. 0.0 to 1.0. Default = 0.03.
        extern public float depth { get; set; }

        // Chorus feedback. Controls how much of the wet signal gets fed back into the chorus buffer. 0.0 to 1.0. Default = 0.0.
        [Obsolete("Warning! Feedback is deprecated. This property does nothing.")]
        public float feedback
        {
            get { Debug.LogWarning("Warning! Feedback is deprecated. This property does nothing."); return 0.0f; }
            set { Debug.LogWarning("Warning! Feedback is deprecated. This property does nothing."); }
        }
    }

    // The Audio Reverb Filter takes an Audio Clip and distortionates it in a
    [RequireComponent(typeof(AudioBehaviour))]
    public sealed partial class AudioReverbFilter : Behaviour
    {
        // Set/Get reverb preset properties
        extern public AudioReverbPreset reverbPreset { get; set; }

        // Mix level of dry signal in output in mB. Ranges from -10000.0 to 0.0. Default is 0.
        extern public float dryLevel { get; set; }

        // Room effect level at low frequencies in mB. Ranges from -10000.0 to 0.0. Default is 0.0.
        extern public float room { get; set; }

        // Room effect high-frequency level re. low frequency level in mB. Ranges from -10000.0 to 0.0. Default is 0.0.
        extern public float roomHF { get; set; }

        // Rolloff factor for room effect. Ranges from 0.0 to 10.0. Default is 10.0
        [Obsolete("Warning! roomRolloffFactor is no longer supported.")]
        public float roomRolloffFactor
        {
            get { Debug.LogWarning("Warning! roomRolloffFactor is no longer supported."); return 10.0f; }
            set { Debug.LogWarning("Warning! roomRolloffFactor is no longer supported."); }
        }

        // Reverberation decay time at low-frequencies in seconds. Ranges from 0.1 to 20.0. Default is 1.0.
        extern public float decayTime { get; set; }

        // Decay HF Ratio : High-frequency to low-frequency decay time ratio. Ranges from 0.1 to 2.0. Default is 0.5.
        extern public float decayHFRatio { get; set; }

        //  Early reflections level relative to room effect in mB. Ranges from -10000.0 to 1000.0. Default is -10000.0.
        extern public float reflectionsLevel { get; set; }

        // Late reverberation level relative to room effect in mB. Ranges from -10000.0 to 2000.0. Default is 0.0.
        extern public float reflectionsDelay { get; set; }

        //  Late reverberation level relative to room effect in mB. Ranges from -10000.0 to 2000.0. Default is 0.0.
        extern public float reverbLevel { get; set; }

        // Late reverberation delay time relative to first reflection in seconds. Ranges from 0.0 to 0.1. Default is 0.04.
        extern public float reverbDelay { get; set; }

        // Reverberation diffusion (echo density) in percent. Ranges from 0.0 to 100.0. Default is 100.0.
        extern public float diffusion { get; set; }

        // Reverberation density (modal density) in percent. Ranges from 0.0 to 100.0. Default is 100.0.
        extern public float density { get; set; }

        // Reference high frequency in Hz. Ranges from 20.0 to 20000.0. Default is 5000.0.
        extern public float hfReference { get; set; }

        // Room effect low-frequency level in mB. Ranges from -10000.0 to 0.0. Default is 0.0.
        extern public float roomLF { get; set; }

        // Reference low-frequency in Hz. Ranges from 20.0 to 1000.0. Default is 250.0.
        extern public float lfReference { get; set; }
    }

    // ================================================================
    // Microphone - 麦克风录音接口（静态类）
    // ================================================================
    //
    // 【概述】
    // Microphone 提供通过连接的麦克风设备录制音频到 AudioClip 的能力。
    // 这是一个纯静态类，不需要挂载到 GameObject。
    //
    // 【录音流程】
    //   1. Microphone.devices → 获取可用设备列表
    //   2. Microphone.Start(deviceName, loop, lengthSec, frequency) → 开始录音
    //      返回一个 AudioClip 作为录音缓冲区（需要指定大小和采样率）
    //   3. 持续录音中...（可通过 GetPosition 监控进度）
    //   4. Microphone.End(deviceName) → 停止录音
    //   5. 将录制的 AudioClip 赋给 AudioSource.clip 播放
    //
    // 【参数约束】
    //   lengthSec — 录音时长必须 > 0 且 < 3600 秒（1小时）
    //   frequency — 采样率必须 > 0（建议使用设备支持的频率）
    //   loop      — 是否循环录制（环形缓冲区覆盖旧数据）
    //
    // 💡 loop=true 的环形缓冲区：
    //   录音数据在固定大小的缓冲区中循环写入。
    //   新数据覆盖最旧的数据，始终保留最近 lengthSec 秒的录音。
    //   适合无限录音场景（如语音聊天），用 End() 停止时保留全部数据。
    //
    // 💡 GetPosition() 的返回值：
    //   返回当前录音写入位置（采样点数）。
    //   可用于判断录音是否在进行、计算已录制时长。
    //
    // 🎯 GetDeviceCaps() — 查询设备频率能力：
    //   返回设备支持的最低和最高采样率。
    //   录音频率应在此范围内才能获得最佳品质。
    //
    // ⚠️ 平台限制：
    //   - 需要麦克风权限（Android/iOS 需运行时申请）
    //   - 部分平台不支持多个设备同时录音
    //   - WebGL 平台支持有限
    //
    // 📍 C++ 端：Modules/Audio/Public/Microphone.h
    // ================================================================
    // Use this class to record to an [[AudioClip|audio clip]] using a connected microphone.
    [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
    public sealed class Microphone
    {
        [NativeMethod(IsThreadSafe = true)]
        extern static private int GetMicrophoneDeviceIDFromName(string name);

        extern static private AudioClip StartRecord(int deviceID, bool loop, float lengthSec, int frequency);

        extern static private void EndRecord(int deviceID);

        extern static private bool IsRecording(int deviceID);

        [NativeMethod(IsThreadSafe = true)]
        extern static private int GetRecordPosition(int deviceID);

        extern static private void GetDeviceCaps(int deviceID, out int minFreq, out int maxFreq);

        // ================================================================
        // Microphone.Start() — 开始录音
        // ================================================================
        // 🎯 启动指定麦克风设备的录音，返回一个 AudioClip 作为录音缓冲区。
        //
        // 💡 参数说明：
        //   deviceName — 麦克风设备名称（从 Microphone.devices 获取）
        //   loop       — 是否循环录制（环形缓冲区模式）
        //   lengthSec  — 录音缓冲区大小（秒），必须 > 0 且 < 3600
        //   frequency  — 采样率（Hz），应使用设备支持的频率
        //
        // 💡 返回的 AudioClip：
        //   - 这是一个预先分配的缓冲区，随录音推进被填充
        //   - clip.length == lengthSec（无论 loop 为何值）
        //   - 可以直接赋给 AudioSource.clip 进行实时播放（边录边播）
        //
        // ⚠️ 验证逻辑：
        //   - deviceName 无效 → 抛出 ArgumentException
        //   - lengthSec <= 0 → 抛出 ArgumentException
        //   - lengthSec > 3600 → 抛出 ArgumentException（最多录 1 小时）
        //   - frequency <= 0 → 抛出 ArgumentException
        // ================================================================

        // Start Recording with device
        static public AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);

            if (deviceID == -1)
                throw new ArgumentException("Couldn't acquire device ID for device name " + deviceName);

            if (lengthSec <= 0)
                throw new ArgumentException("Length of recording must be greater than zero seconds (was: " + lengthSec + " seconds)");

            if (lengthSec > 60 * 60)
                throw new ArgumentException("Length of recording must be less than one hour (was: " + lengthSec + " seconds)");

            if (frequency <= 0)
                throw new ArgumentException("Frequency of recording must be greater than zero (was: " + frequency + " Hz)");

            return StartRecord(deviceID, loop, lengthSec, frequency);
        }

        // Stops recording
        static public void End(string deviceName)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return;

            EndRecord(deviceID);
        }

        // Gives you a list microphone devices, identified by name.
        extern static public string[] devices
        {
            [NativeName("GetRecordDevices")]
            get;
        }

        internal static extern bool isAnyDeviceRecording
        {
            [NativeName("IsAnyRecordDeviceActive")]
            get;
        }

        // Query if a device is currently recording.
        static public bool IsRecording(string deviceName)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return false;

            return IsRecording(deviceID);
        }

        // Get the position in samples of the recording.
        static public int GetPosition(string deviceName)
        {
            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return 0;

            return GetRecordPosition(deviceID);
        }

        // Get the frequency capabilities of a device.
        static public void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
            minFreq = 0;
            maxFreq = 0;

            int deviceID = GetMicrophoneDeviceIDFromName(deviceName);
            if (deviceID == -1)
                return;

            GetDeviceCaps(deviceID, out minFreq, out maxFreq);
        }
    }

}
