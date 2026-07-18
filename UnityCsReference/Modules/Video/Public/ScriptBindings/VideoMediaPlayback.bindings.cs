// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 VideoPlaybackMgr — 视频播放管理器（内部）
//
// 📌 作用：
//   VideoPlaybackMgr 是 Unity 内部的视频播放管理器。
//   生命周期管理与 VideoPlayback 实例的创建和释放。
//   类似于一个"视频播放器池"，管理多个同时进行的播放。
//
// 🏗 核心概念：
//   VideoPlaybackMgr → 管理 VideoPlayback 实例
//   VideoPlayback    → 单个视频播放实例（在 MediaComponent 中定义）
//   通过 IntPtr m_Ptr 持有 C++ 端的原生对象指针。
//
// 💡 理解关键：
//   这是 internal 类，不对用户暴露。
//   CreateVideoPlayback() 接受文件名和三个回调：
//   - errorCallback：出错时通知
//   - readyCallback：准备好时通知
//   - reachedEndCallback：播放结束时通知
//   splitAlpha：是否分离 Alpha 通道（用于抗锯齿）
//
// 📍 对应 C++ 头文件：Modules/Video/Public/Base/VideoMediaPlayback.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("VideoTesting")]
[assembly: InternalsVisibleTo("Unity.Audio.DSPGraph.Tests")]

namespace UnityEngineInternal.Video
{
    [UsedByNativeCode]
    [NativeHeader("Modules/Video/Public/Base/VideoMediaPlayback.h")]
    internal class VideoPlaybackMgr : IDisposable
    {
        internal IntPtr m_Ptr;

        // ==============================================================
        // 🏗 构造/析构 — 创建/销毁 C++ 端管理器
        //
        // 📌 Internal_Create() 在 C++ 端实例化 VideoMediaPlayback 管理器。
        //    Dispose() 释放原生对象并调用 GC.SuppressFinalize 优化 GC。
        // ==============================================================
        public VideoPlaybackMgr()
        {
            m_Ptr = Internal_Create();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        private static extern IntPtr Internal_Create();
        private static extern void Internal_Destroy(IntPtr ptr);

        // ==============================================================
        // 📌 播放实例管理
        //
        // 🎯 CreateVideoPlayback  — 根据文件名创建新的播放实例
        // 🎯 ReleaseVideoPlayback — 释放指定播放实例
        // 🎯 videoPlaybackCount   — 当前活跃的播放实例数
        // 🎯 Update()             — 每帧更新所有播放实例
        //
        // 📌 回调委托：
        //   Callback        — 无参数回调（ready, reachedEnd）
        //   MessageCallback — 带错误信息的回调
        // ==============================================================
        public delegate void Callback();
        public delegate void MessageCallback(string message);
        extern public VideoPlayback CreateVideoPlayback(string fileName, MessageCallback errorCallback, Callback readyCallback, Callback reachedEndCallback, bool splitAlpha = false);
        extern public void ReleaseVideoPlayback(VideoPlayback playback);
        extern public ulong videoPlaybackCount { get; }
        extern public void Update();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(VideoPlaybackMgr videoPlaybackMgr) => videoPlaybackMgr.m_Ptr;
        }
    }
}

