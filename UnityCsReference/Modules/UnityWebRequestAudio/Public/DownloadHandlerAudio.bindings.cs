// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 DownloadHandlerAudioClip — 音频下载处理器
// ================================================================
//
// 📌 职责
//   接收 HTTP 响应的音频数据并直接解码为 AudioClip 对象。
//   支持多种音频格式（由 AudioType 参数指定）。
//
// 💡 构造方式
//   支持 string url 和 Uri 两种 URI 输入，内部统一转为 AbsoluteUri：
//   - DownloadHandlerAudioClip(string url, AudioType audioType)
//   - DownloadHandlerAudioClip(Uri uri, AudioType audioType)
//   - DownloadHandlerAudioClip(string url, AudioType audioType, bool ambisonic)
//
// 📌 关键属性
//   - audioClip       : 解码后的 AudioClip 对象（[NativeMethod(ThrowsException=true)]）
//   - streamAudio     : 是否边下载边播放（true=流式，false=完整下载后再播放）
//   - compressed      : 音频在内存中是否保持压缩格式（节省内存）
//   - ambisonic       : 是否是 Ambisonic 音频（360°/空间音频场景）
//
// ⚡ 数据流
//   音频数据通过 m_NativeData (NativeArray<byte>) 零拷贝共享
//   → InternalGetNativeArray(this, ref m_NativeData)
//   → C++ 原生端直接解码为 AudioClip
//   注意：GetText() 抛出 NotSupportedException，音频数据不可转字符串
//
// 🎯 使用示例
//   using (var uwr = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
//   {
//       yield return uwr.SendWebRequest();
//       AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
//   }
//
// ================================================================
// ⚠️ DownloadHandlerMovieTexture — 已废弃
// ================================================================
// 📌 Unity 5.x 时期用于下载 MovieTexture
//   现在所有成员都抛出 "FeatureRemoved()" 异常
//   替代方案：VideoPlayer 组件
// ================================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngineInternal;
using Unity.Collections;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequestAudio/Public/DownloadHandlerAudioClip.h")]
    public sealed class DownloadHandlerAudioClip : DownloadHandler
    {
        private NativeArray<byte> m_NativeData;

        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerAudioClip obj, string url, AudioType audioType);

        private void InternalCreateAudioClip(string url, AudioType audioType)
        {
            m_Ptr = Create(this, url, audioType);
        }

        public DownloadHandlerAudioClip(string url, AudioType audioType)
        {
            InternalCreateAudioClip(url, audioType);
        }

        public DownloadHandlerAudioClip(Uri uri, AudioType audioType)
        {
            InternalCreateAudioClip(uri.AbsoluteUri, audioType);
        }

        public DownloadHandlerAudioClip(string url, AudioType audioType, bool ambisonic)
        {
            InternalCreateAudioClip(url, audioType);
            this.ambisonic = ambisonic;
        }

        public DownloadHandlerAudioClip(Uri uri, AudioType audioType, bool ambisonic)
        {
            InternalCreateAudioClip(uri.AbsoluteUri, audioType);
            this.ambisonic = ambisonic;
        }

        protected override NativeArray<byte> GetNativeData()
        {
            return InternalGetNativeArray(this, ref m_NativeData);
        }

        public override void Dispose()
        {
            DisposeNativeArray(ref m_NativeData);
            base.Dispose();
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for audio clips");
        }

        [NativeMethod(ThrowsException = true)]
        public extern AudioClip audioClip { get; }

        public extern bool streamAudio { get; set; }

        public extern bool compressed { get; set; }

        public extern bool ambisonic { get; set; }

        public static AudioClip GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerAudioClip>(www).audioClip;
        }
        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerAudioClip handler) => handler.m_Ptr;
        }

    }

    [System.Obsolete("MovieTexture is deprecated. Use VideoPlayer instead.", true)]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class DownloadHandlerMovieTexture : DownloadHandler
    {
        public DownloadHandlerMovieTexture()
        {
            FeatureRemoved();
        }

        protected override byte[] GetData()
        {
            FeatureRemoved();
            return null;
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for movies");
        }

        public MovieTexture movieTexture { get { FeatureRemoved(); return null; } }

        public static MovieTexture GetContent(UnityWebRequest uwr)
        {
            FeatureRemoved();
            return null;
        }

        static void FeatureRemoved()
        {
            throw new Exception("Movie texture has been removed, use VideoPlayer instead");
        }

    }
}
