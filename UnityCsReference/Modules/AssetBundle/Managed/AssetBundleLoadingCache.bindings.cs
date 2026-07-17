// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AssetBundleLoadingCache — AssetBundle 加载缓存管理器
//
// 管理 AssetBundle 异步加载过程中的内存缓冲区。
// 当并行加载多个 AssetBundle 时，Unity 使用此缓存来控制
// 最大内存占用。
//
// 通过 AssetBundle.memoryBudgetKB 暴露给用户调节。
// ============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    /// AssetBundle 加载缓存（静态类）。
    /// 使用分块（Block）机制管理 IO 缓冲区：
    ///   - memoryBudgetKB = blockCount × blockSize
    ///   - 当设置 memoryBudgetKB 时，自动调整 blockCount 和 maxBlocksPerFile
    ///   - maxBlocksPerFile 限制单个文件最多同时占用的块数
    ///
    /// 适用场景：在有大量 AssetBundle 并行加载时，通过调节此缓存
    /// 来控制 IO 吞吐量和内存占用的平衡。
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadingCache.h")]
    static class AssetBundleLoadingCache
    {
        /// <summary>最少允许的块数量（最少 2 块）</summary>
        internal const int kMinAllowedBlockCount = 2;

        /// <summary>每个文件最少允许的最大块数</summary>
        internal const int kMinAllowedMaxBlocksPerFile = 2;

        /// <summary>每个文件最多同时占用的缓存块数</summary>
        internal static extern uint maxBlocksPerFile { get; set; }

        /// <summary>缓存块总数</summary>
        internal static extern uint blockCount { get; set; }

        /// <summary>每个缓存块的大小（字节），由 C++ 端计算</summary>
        internal static extern uint blockSize { get; }

        /// <summary>
        /// 缓存内存总预算（KB）。
        /// 设置时会自动计算合适的 blockCount 和 maxBlocksPerFile。
        /// 如果预算太小（< kMinAllowedBlockCount * blockSize），会被提升到最小值。
        /// </summary>
        internal static uint memoryBudgetKB
        {
            get
            {
                return blockCount * blockSize;
            }
            set
            {
                uint newBlockCount = Math.Max(value / blockSize, kMinAllowedBlockCount);
                uint newMaxBlocksPerFile = Math.Max(blockCount / 4, kMinAllowedMaxBlocksPerFile);
                if (newBlockCount != blockCount || newMaxBlocksPerFile != maxBlocksPerFile)
                {
                    blockCount = newBlockCount;
                    maxBlocksPerFile = newMaxBlocksPerFile;
                }
            }
        }
    }
}
