// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AssetBundleCreateRequest — AssetBundle 异步创建请求
//
// 当调用 AssetBundle.LoadFromFileAsync / LoadFromMemoryAsync /
// LoadFromStreamAsync 时，立即返回此对象。
//
// 继承自 AsyncOperation，支持：
//   - isDone 属性检查是否完成
//   - completed 回调监听完成事件
//   - 协程 yield return 等待
//   - assetBundle 属性获取最终加载的 AssetBundle
//
// 注意：访问 assetBundle 属性会阻塞当前线程，直到加载完成。
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
    /// <summary>
    /// AssetBundle 的异步创建请求。
    /// 通过静态方法 AssetBundle.LoadFromFileAsync 等创建。
    ///
    /// 使用示例：
    ///   AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(path);
    ///   yield return req;
    ///   AssetBundle bundle = req.assetBundle;
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromAsyncOperation.h")]
    public class AssetBundleCreateRequest : AsyncOperation
    {
        /// <summary>
        /// 获取创建的 AssetBundle 对象。
        /// 如果操作尚未完成，此属性会阻塞等待。
        /// 加载失败时返回 null。
        /// </summary>
        public extern UnityEngine.AssetBundle assetBundle
        {
            [NativeMethod("GetAssetBundleBlocking")]
            get;
        }

        /// <summary>设置是否启用兼容性检查（内部使用）。</summary>
        [NativeMethod("SetEnableCompatibilityChecks")]
        private extern void SetEnableCompatibilityChecks(bool set);

        /// <summary>禁用兼容性检查（用于 Editor 中的 AssetBundle 预览等场景）。</summary>
        internal void DisableCompatibilityChecks()
        {
            SetEnableCompatibilityChecks(false);
        }

        public AssetBundleCreateRequest() { }

        private AssetBundleCreateRequest(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static AssetBundleCreateRequest ConvertToManaged(IntPtr ptr) => new AssetBundleCreateRequest(ptr);
            public static IntPtr ConvertToNative(AssetBundleCreateRequest assetBundleCreateRequest) => assetBundleCreateRequest.m_Ptr;
        }
    }
}
