// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AssetBundleRequest — AssetBundle 异步资源加载请求
//
// 由 AssetBundle.LoadAssetAsync / LoadAllAssetsAsync /
// LoadAssetWithSubAssetsAsync 创建。
//
// 继承自 ResourceRequest（后者继承自 AsyncOperation）。
// 相比 ResourceRequest，多了 allAssets 属性，用于
// LoadAllAssetsAsync 和 LoadAssetWithSubAssetsAsync 返回多个资源。
//
// 注意：
//   - asset 属性返回第一个（或唯一的）已加载资源
//   - allAssets 属性返回加载的所有资源
// ============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    /// AssetBundle 的异步资源加载请求。
    /// 当调用 AssetBundle.LoadAssetAsync 等方法时返回。
    ///
    /// 使用示例：
    ///   AssetBundleRequest req = bundle.LoadAssetAsync<GameObject>("myPrefab");
    ///   yield return req;
    ///   GameObject prefab = req.asset as GameObject;
    ///
    /// 批量加载时使用 allAssets：
    ///   AssetBundleRequest req = bundle.LoadAllAssetsAsync<GameObject>();
    ///   yield return req;
    ///   GameObject[] allPrefabs = req.allAssets;  // 自动转为 GameObject[]
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadAssetOperation.h")]
    public class AssetBundleRequest : ResourceRequest
    {
        /// <summary>重写基类 GetResult，从原生层获取已加载的资源对象。</summary>
        [NativeMethod("GetLoadedAsset")]
        protected override extern Object GetResult();

        /// <summary>获取异步加载的第一个（或唯一的）资源。</summary>
        public new Object asset { get { return GetResult(); } }

        /// <summary>
        /// 获取异步加载的所有资源。
        /// 当使用 LoadAllAssetsAsync 或 LoadAssetWithSubAssetsAsync 时，
        /// 此属性包含所有已加载的资源。
        /// </summary>
        public extern Object[] allAssets
        {
            [NativeMethod("GetAllLoadedAssets")]
            get;
        }

        public AssetBundleRequest() { }

        private AssetBundleRequest(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static AssetBundleRequest ConvertToManaged(IntPtr ptr) => new AssetBundleRequest(ptr);
            public static IntPtr ConvertToNative(AssetBundleRequest request) => request.m_Ptr;
        }
    }
}
