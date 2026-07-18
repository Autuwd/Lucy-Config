// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ArchiveFile — 归档文件（AssetBundle/Unity 归档）
//
// 📌 作用：
//   管理和操作 Unity 的归档文件系统（Archive），封装底层
//   的压缩/解压、文件索引和读取操作。
//
// 💡 核心类型：
//   - ArchiveStatus：归档状态枚举（Pending/InProgress/Success/Error/Cancelled）
//   - ArchiveInfo：归档文件信息结构
//   - ArchiveFileInterface：底层文件接口
//   - CompressedBlockSize：压缩块大小结构
//   - AsyncReadIntoArchiveJob：异步读取归档作业
//   - ReadCommand/ReadHandle：异步读取命令和处理
//
// ⚡ 用于 Addressables、AssetBundle 等资源管理系统。
// ==============================================================

using System;
using Unity.Content;
using Unity.Jobs;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.IO.Archive
{
    [RequiredByNativeCode]
    public enum ArchiveStatus
    {
        InProgress,
        Complete,
        Failed
    }

    [Flags]
    [RequiredByNativeCode]
    internal enum ManagedArchiveOptions
    {
        None = 0,
        MountCAH = 1 << 0,
    };

    [NativeHeader("Runtime/VirtualFileSystem/ArchiveFileSystem/ArchiveFileHandle.h")]
    [RequiredByNativeCode]
    public struct ArchiveFileInfo
    {
        public string Filename;
        public ulong FileSize;
    }

    [NativeHeader("Runtime/VirtualFileSystem/ArchiveFileSystem/ArchiveFileHandle.h")]
    [RequiredByNativeCode]
    public struct ArchiveHandle
    {
        internal UInt64 Handle;

        public ArchiveStatus Status
        {
            get
            {
                ThrowIfInvalid();
                return ArchiveFileInterface.Archive_GetStatus(this);
            }
        }

        public JobHandle JobHandle
        {
            get
            {
                ThrowIfInvalid();
                return ArchiveFileInterface.Archive_GetJobHandle(this);
            }
        }

        public JobHandle Unmount()
        {
            ThrowIfInvalid();
            return ArchiveFileInterface.Archive_UnmountAsync(this);
        }

        void ThrowIfInvalid()
        {
            if (!ArchiveFileInterface.Archive_IsValid(this))
                throw new InvalidOperationException("The archive has already been unmounted.");
        }

        public string GetMountPath()
        {
            ThrowIfInvalid();
            return ArchiveFileInterface.Archive_GetMountPath(this);
        }

        public UnityEngine.CompressionType Compression
        {
            get
            {
                ThrowIfInvalid();
                return ArchiveFileInterface.Archive_GetCompression(this);
            }
        }

        public bool IsStreamed
        {
            get
            {
                ThrowIfInvalid();
                return ArchiveFileInterface.Archive_IsStreamed(this);
            }
        }

        public ArchiveFileInfo[] GetFileInfo()
        {
            ThrowIfInvalid();
            return ArchiveFileInterface.Archive_GetFileInfo(this);
        }
    }

    [RequiredByNativeCode]
    [NativeHeader("Runtime/VirtualFileSystem/ArchiveFileSystem/ArchiveFileHandle.h")]
    [StaticAccessor("GetManagedArchiveSystem()", StaticAccessorType.Dot)]
    public static class ArchiveFileInterface
    {
        internal static extern ArchiveHandle MountAsync(ContentNamespace namespaceId, string filePath, string prefix, ManagedArchiveOptions options);
        public static ArchiveHandle MountAsync(ContentNamespace namespaceId, string filePath, string prefix)
        {
            return MountAsync(namespaceId, filePath, prefix, ManagedArchiveOptions.None);
        }
        public static extern ArchiveHandle[] GetMountedArchives(ContentNamespace namespaceId);

        internal static extern ArchiveStatus Archive_GetStatus(ArchiveHandle handle);
        internal static extern JobHandle Archive_GetJobHandle(ArchiveHandle handle);
        internal static extern bool Archive_IsValid(ArchiveHandle handle);
        internal static extern JobHandle Archive_UnmountAsync(ArchiveHandle handle);
        internal static extern string Archive_GetMountPath(ArchiveHandle handle);
        internal static extern UnityEngine.CompressionType Archive_GetCompression(ArchiveHandle handle);
        internal static extern bool Archive_IsStreamed(ArchiveHandle handle);
        internal static extern ArchiveFileInfo[] Archive_GetFileInfo(ArchiveHandle handle);
    }
}
