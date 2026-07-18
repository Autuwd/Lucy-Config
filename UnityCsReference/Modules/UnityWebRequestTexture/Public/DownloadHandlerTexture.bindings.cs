// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 DownloadHandlerTexture — 纹理下载处理器
// ================================================================
//
// 📌 职责
//   接收 HTTP 响应的图像数据并直接解码为 Texture2D 对象。
//   支持的格式：JPG、PNG、GIF、BMP、TGA 等 Unity 图像加载器支持的格式。
//
// 🏗 构造体系
//   DownloadHandlerTexture()                    → 可读 + 生成 Mipmap（默认）
//   DownloadHandlerTexture(bool readable)       → 控制是否可读
//   DownloadHandlerTexture(DownloadedTextureParams) → 完整参数控制
//
// 💡 DownloadedTextureParams 参数详解
//   struct DownloadedTextureParams {
//       flags: DownloadedTextureFlags  // 位标记组合
//       mipmapCount: int               // mip 级别数（-1=完整链）
//   }
//
// 📌 DownloadedTextureFlags 枚举
//   ⚡ None             = 0     → 无特殊处理
//   ⚡ Readable         = 1<<0  → CPU 可访问像素数据（用于 GetPixels 等）
//   ⚡ MipmapChain      = 1<<1  → 生成完整 Mipmap 链
//   ⚡ LinearColorSpace = 1<<2  → 线性颜色空间（默认 sRGB）
//
// 💡 最佳实践
//   - 只显示不编辑 → readable=false（节省内存）
//   - 需要缩放/修改 → readable=true
//   - UI 图片 → 默认 MipmapChain=false
//   - 3D 纹理 → MipmapChain=true
//   - HDR/法线贴图 → LinearColorSpace=true
//
// ⚡ 数据流
//   纹理数据通过 m_NativeData (NativeArray<byte>) 零拷贝共享
//   → InternalGetTextureNative() 在 C++ 端直接解码为 Texture2D
//   → texture 属性对外暴露
//
// 🎯 使用示例
//   using (var uwr = UnityWebRequestTexture.GetTexture(url))
//   {
//       yield return uwr.SendWebRequest();
//       Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
//   }
// ================================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngineInternal;
using Unity.Collections;

namespace UnityEngine.Networking
{
    [Flags]
    public enum DownloadedTextureFlags : uint
    {
        None = 0,
        Readable = 1 << 0,
        MipmapChain = 1 << 1,
        LinearColorSpace = 1 << 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DownloadedTextureParams
    {
        public DownloadedTextureFlags flags;
        public int mipmapCount;

        public static DownloadedTextureParams Default => new DownloadedTextureParams()
        {
            flags = DownloadedTextureFlags.Readable | DownloadedTextureFlags.MipmapChain,
            mipmapCount = -1,
        };

        public bool readable
        {
            get => flags.HasFlag(DownloadedTextureFlags.Readable);
            set => SetFlags(DownloadedTextureFlags.Readable, value);
        }

        public bool mipmapChain
        {
            get => flags.HasFlag(DownloadedTextureFlags.MipmapChain);
            set => SetFlags(DownloadedTextureFlags.MipmapChain, value);
        }

        public bool linearColorSpace
        {
            get => flags.HasFlag(DownloadedTextureFlags.LinearColorSpace);
            set => SetFlags(DownloadedTextureFlags.LinearColorSpace, value);
        }

        void SetFlags(DownloadedTextureFlags flgs, bool add)
        {
            if (add)
                flags |= flgs;
            else
                flags &= ~flgs;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequestTexture/Public/DownloadHandlerTexture.h")]
    public sealed class DownloadHandlerTexture : DownloadHandler
    {
        private NativeArray<byte> m_NativeData;

        private static extern IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerTexture obj, DownloadedTextureParams parameters);

        private void InternalCreateTexture(DownloadedTextureParams parameters)
        {
            m_Ptr = Create(this, parameters);
        }

        public DownloadHandlerTexture()
            : this(true)
        {
        }

        public DownloadHandlerTexture(bool readable)
        {
            var parameters = DownloadedTextureParams.Default;
            parameters.readable = readable;
            InternalCreateTexture(parameters);
        }

        public DownloadHandlerTexture(DownloadedTextureParams parameters)
        {
            InternalCreateTexture(parameters);
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

        public Texture2D texture
        {
            get { return InternalGetTextureNative(); }
        }

        [NativeMethod(ThrowsException = true)]
        private extern Texture2D InternalGetTextureNative();

        public static Texture2D GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerTexture>(www).texture;
        }
        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerTexture handler) => handler.m_Ptr;
        }

    }
}
