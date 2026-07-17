// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AssetBundleRecompressOperation — AssetBundle 异步重新压缩操作
//
// 由 AssetBundle.RecompressAssetBundleAsync() 创建。
// 用于将一个 AssetBundle 从原始压缩格式转换为另一种运行时支持的格式。
//
// 典型用例：
//   下载时使用 LZMA（压缩率最高，包体最小），
//   下载完成后重新压缩为 LZ4（支持随机读取，加载更快）。
// ============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    /// AssetBundle 的异步重新压缩操作。
    ///
    /// 将已下载的 AssetBundle 从原始压缩方式转换为运行时支持的格式。
    /// 通常用于下载完成后，将 LZMA 压缩的包重新压缩为 LZ4。
    ///
    /// 使用方式：
    ///   AssetBundleRecompressOperation op =
    ///       AssetBundle.RecompressAssetBundleAsync(inputPath, outputPath, BuildCompression.LZ4);
    ///   yield return op;
    ///   if (op.success) { /* 重新压缩完成 */ }
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleRecompressOperation.h")]
    public class AssetBundleRecompressOperation : AsyncOperation
    {
        /// <summary>获取人类可读的操作结果描述文本（如 "OK"、"CRC failed" 等）。</summary>
        public extern string humanReadableResult
        {
            [NativeMethod("GetResultStr")]
            get;
        }

        /// <summary>获取输入 AssetBundle 的文件路径</summary>
        public extern string inputPath
        {
            [NativeMethod("GetInputPath")]
            get;
        }

        /// <summary>获取输出 AssetBundle 的文件路径</summary>
        public extern string outputPath
        {
            [NativeMethod("GetOutputPath")]
            get;
        }

        /// <summary>获取操作结果（AssetBundleLoadResult 枚举）</summary>
        public extern AssetBundleLoadResult result
        {
            [NativeMethod("GetResult")]
            get;
        }

        /// <summary>获取操作是否成功</summary>
        public extern bool success
        {
            [NativeMethod("GetSuccess")]
            get;
        }

        public AssetBundleRecompressOperation() { }

        private AssetBundleRecompressOperation(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static AssetBundleRecompressOperation ConvertToManaged(IntPtr ptr) => new AssetBundleRecompressOperation(ptr);
            public static IntPtr ConvertToNative(AssetBundleRecompressOperation op) => op.m_Ptr;
        }
    }
}
