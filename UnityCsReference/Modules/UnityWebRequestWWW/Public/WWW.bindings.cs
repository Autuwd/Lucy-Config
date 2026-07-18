// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 WebRequestWWW — 内部音频剪辑创建桥接（internal）
// ================================================================
//
// 📌 职责
//   为旧版 WWW.GetAudioClip() 系列方法提供原生层面的
//   AudioClip 创建能力。是 WWW 兼容层的 C++ 桥接。
//
// 💡 实现
//   InternalCreateAudioClipUsingDH 标记为 [FreeFunction]
//   直接调用 C++ 端 UnityWebRequestCreateAudioClip() 函数，
//   通过已有的 DownloadHandler 创建 AudioClip。
//
// 📌 调用链路
//   WWW.GetAudioClip()
//     → WWW.GetAudioClipInternal()
//       → WebRequestWWW.InternalCreateAudioClipUsingDH()
//         → [C++] UnityWebRequestCreateAudioClip()
//
// ================================================================
// ⚡ 遗留对比：WWW vs UnityWebRequest
// ================================================================
// 📌 WWW 是 Unity 5.4 前的旧版网络 API，已被 UnityWebRequest 取代
//
// ⚠️ WWW 的已知限制：
//   1. 仅支持 GET/POST，不支持 PUT/DELETE/HEAD/自定义
//   2. 不能自定义请求头（仅支持有限的标准头）
//   3. 不支持超时控制（timeout 属性）
//   4. 不支持上传进度追踪
//   5. 无 CertificateHandler，不能做证书验证
//   6. 无重定向控制
//   7. 内存效率较低（WWW 内部实际使用 UnityWebRequest 实现）
//
// 💡 WWW 在 Unity 5.4+ 内部实现已完全基于 UnityWebRequest：
//   - WWW(string url)            → UnityWebRequest.Get(url)
//   - WWW(string url, WWWForm)   → UnityWebRequest.Post(url, form)
//   - WWW(string url, byte[])    → new UnityWebRequest + UploadHandlerRaw
//   - LoadFromCacheOrDownload()  → UnityWebRequestAssetBundle.GetAssetBundle()
//
// 📌 迁移建议
//   ❌ WWW.AssetBundle         → DownloadHandlerAssetBundle.GetContent()
//   ❌ WWW.bytes / .text      → DownloadHandlerBuffer.data / .text
//   ❌ WWW.LoadImageIntoTexture→ DownloadHandlerTexture.GetContent()
//   ✅ WWW.EscapeURL           → UnityWebRequest.EscapeURL()（未废弃）
// ================================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Networking
{
    [NativeHeader("Modules/UnityWebRequestAudio/Public/DownloadHandlerAudioClip.h")]
    internal static class WebRequestWWW
    {
        [FreeFunction("UnityWebRequestCreateAudioClip")]
        internal extern static AudioClip InternalCreateAudioClipUsingDH(DownloadHandler dh, string url, bool stream, bool compressed, AudioType audioType);
    }
}
