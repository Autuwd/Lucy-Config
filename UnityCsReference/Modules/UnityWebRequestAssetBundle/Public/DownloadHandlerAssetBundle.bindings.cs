// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 DownloadHandlerAssetBundle — AssetBundle 下载处理器
// ================================================================
//
// 📌 职责
//   接收 HTTP 响应的 AssetBundle 数据并直接解析为 AssetBundle 对象。
//   支持 CRC 校验和缓存机制（CachedAssetBundle / Hash128）。
//
// 🏗 构造体系
//   DownloadHandlerAssetBundle(string url, uint crc)
//     → 无缓存下载，仅 CRC 校验
//
//   DownloadHandlerAssetBundle(string url, Hash128 hash, uint crc)
//     → 基于 Hash128 的缓存下载
//     → 若本地缓存匹配则跳过下载
//
//   DownloadHandlerAssetBundle(string url, string name, Hash128 hash, uint crc)
//     → 命名缓存 + hash，适合同一 URL 返回不同版本
//
//   DownloadHandlerAssetBundle(string url, CachedAssetBundle cachedBundle, uint crc)
//     → 推荐的现代 API，统一管理 name + hash
//
// 💡 缓存机制
//   - 内部调用 CreateCached() 传入 hash
//   - C++ 端根据 hash 查找本地缓存文件
//   - 匹配则直接加载本地文件，不发起网络请求
//   - 不匹配则下载并自动更新缓存
//
// 📌 CRC 验证
//   所有重载都包含 crc 参数（默认 0 表示跳过校验）
//   用于检测下载的 AssetBundle 是否损坏
//
// ⚡ 关键属性
//   - assetBundle         : 获取解析后的 AssetBundle 对象
//   - autoLoadAssetBundle : 是否下载完成后立即自动加载（可关闭延迟加载）
//   - isDownloadComplete  : 下载是否已完成（用于判断 assetBundle 就绪）
//
// ⚠️ 注意事项
//   - 不继承 GetNativeData()，GetData()/GetText() 抛出 NotSupportedException
//   - 必须通过 assetBundle 属性访问数据
//   - 用完后记得 assetBundle.Unload() 释放资源
//   - GetContent(www) 静态方法做了空/null 安全检查
// ================================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequestAssetBundle/Public/DownloadHandlerAssetBundle.h")]
    public sealed class DownloadHandlerAssetBundle : DownloadHandler
    {
        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerAssetBundle obj, string url, uint crc);
        private extern static IntPtr CreateCached([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerAssetBundle obj, string url, string name, Hash128 hash, uint crc);

        private void InternalCreateAssetBundle(string url, uint crc)
        {
            m_Ptr = Create(this, url, crc);
        }

        private void InternalCreateAssetBundleCached(string url, string name, Hash128 hash, uint crc)
        {
            m_Ptr = CreateCached(this, url, name, hash, crc);
        }

        public DownloadHandlerAssetBundle(string url, uint crc)
        {
            InternalCreateAssetBundle(url, crc);
        }

        public DownloadHandlerAssetBundle(string url, uint version, uint crc)
        {
            InternalCreateAssetBundleCached(url, "", new Hash128(0, 0, 0, version), crc);
        }

        public DownloadHandlerAssetBundle(string url, Hash128 hash, uint crc)
        {
            InternalCreateAssetBundleCached(url, "", hash, crc);
        }

        public DownloadHandlerAssetBundle(string url, string name, Hash128 hash, uint crc)
        {
            InternalCreateAssetBundleCached(url, name, hash, crc);
        }

        public DownloadHandlerAssetBundle(string url, CachedAssetBundle cachedBundle, uint crc)
        {
            InternalCreateAssetBundleCached(url, cachedBundle.name, cachedBundle.hash, crc);
        }

        protected override byte[] GetData()
        {
            throw new System.NotSupportedException("Raw data access is not supported for asset bundles");
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for asset bundles");
        }

        public extern AssetBundle assetBundle { get; }

        public extern bool autoLoadAssetBundle { get; [NativeMethod(ThrowsException = true)] set; }

        public extern bool isDownloadComplete { get; }

        public static AssetBundle GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerAssetBundle>(www).assetBundle;
        }
        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerAssetBundle handler) => handler.m_Ptr;
        }
    }
}
