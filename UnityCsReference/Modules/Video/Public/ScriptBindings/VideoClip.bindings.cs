// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 VideoClip — 导入的视频资源数据对象
//
// 📌 作用：
//   VideoClip 是 Unity 中导入视频文件后的资源对象。
//   类似于 Texture2D 是图片资源，VideoClip 是视频资源。
//   它存储视频的元数据：分辨率、帧率、时长、音轨信息。
//
// 🏗 核心概念：
//   VideoClip = 视频资源的"元数据容器"，不包含像素数据。
//   实际解码和帧数据由 VideoPlayer 在运行时管理。
//   originalPath 显示导入前的原始文件路径。
//
// 💡 理解关键：
//   VideoClip 继承自 Object，具有 Unity 对象的生命周期。
//   它是不可变的——构造方法私有，只能通过导入创建。
//   音轨信息用于配置 VideoPlayer 的音频输出。
//
// 📍 对应 C++ 头文件：Modules/Video/Public/VideoClip.h
// ==============================================================

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Video
{
    [RequiredByNativeCode]
    [NativeHeader("Modules/Video/Public/VideoClip.h")]
    public sealed class VideoClip : Object
    {
        private VideoClip() {}

        // ==============================================================
        // 📌 视频元数据 — 导入时确定的只读属性
        //
        // 🎯 originalPath — 原始文件路径（导入前的路径）
        // 🎯 frameCount   — 视频总帧数
        // 🎯 frameRate    — 帧率（fps）
        // 🎯 length       — 视频总时长（秒）
        // 🎯 width/height — 视频分辨率（像素）
        // 🎯 pixelAspectRatio — 像素宽高比（非方形像素用）
        // 🎯 sRGB         — 是否已标记为 sRGB 色彩空间
        // ==============================================================
        public extern string originalPath { get; }

        public extern ulong frameCount { get; }

        public extern double frameRate { get; }

        [NativeName("Duration")]
        public extern double length { get; }

        public extern uint width { get; }

        public extern uint height { get; }

        public extern uint pixelAspectRatioNumerator { get; }

        public extern uint pixelAspectRatioDenominator { get; }

        public extern bool sRGB { [NativeName("IssRGB")] get; }

        // ==============================================================
        // 🎵 音轨信息 — 视频中包含的音频轨道
        //
        // 🎯 audioTrackCount     — 音轨总数
        // 🎯 GetAudioChannelCount — 指定音轨的声道数（1=单声道, 2=立体声）
        // 🎯 GetAudioSampleRate   — 指定音轨的采样率（Hz）
        // 🎯 GetAudioLanguage     — 指定音轨的语言代码（如 "eng", "chi"）
        //
        // 💡 多音轨视频常见于 Blu-ray 或高清下载片源。
        // ==============================================================
        public extern ushort audioTrackCount { get; }

        public extern ushort GetAudioChannelCount(ushort audioTrackIdx);

        public extern uint GetAudioSampleRate(ushort audioTrackIdx);

        public extern string GetAudioLanguage(ushort audioTrackIdx);
    }
}
