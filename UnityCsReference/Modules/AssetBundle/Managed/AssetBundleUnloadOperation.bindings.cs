// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AssetBundleUnloadOperation — AssetBundle 异步卸载操作
//
// 由 AssetBundle.UnloadAsync() 创建。
// 继承自 AsyncOperation，可通过协程等待卸载完成。
//
// 提供了 WaitForCompletion() 方法以阻塞当前线程直到卸载完成。
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
    /// <summary>
    /// AssetBundle 的异步卸载操作。
    ///
    /// 使用方式：
    ///   AssetBundleUnloadOperation op = bundle.UnloadAsync(true);
    ///   yield return op;  // 方式 1：协程等待
    ///   // 或
    ///   op.WaitForCompletion();  // 方式 2：阻塞等待（注意会卡主线程）
    ///
    /// 注意：UnloadAsync 只是异步触发卸载流程，实际对象销毁
    /// 可能仍在后台进行。WaitForCompletion 可确保完全完成。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleUnloadOperation.h")]
    public class AssetBundleUnloadOperation : AsyncOperation
    {
        /// <summary>
        /// 阻塞当前线程，直到 AssetBundle 卸载操作完全完成。
        /// 调用此方法后，所有从此包加载的 Object（根据 unloadAllLoadedObjects 参数）
        /// 会被销毁。
        ///
        /// 注意：在主线程调用此方法可能导致短暂的帧率下降。
        /// </summary>
        [NativeMethod("WaitForCompletion")]
        public extern void WaitForCompletion();

        public AssetBundleUnloadOperation() { }

        private AssetBundleUnloadOperation(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static AssetBundleUnloadOperation ConvertToManaged(IntPtr ptr) => new AssetBundleUnloadOperation(ptr);
            public static IntPtr ConvertToNative(AssetBundleUnloadOperation assetBundleUnloadOperation) => assetBundleUnloadOperation.m_Ptr;
        }
    }
}
