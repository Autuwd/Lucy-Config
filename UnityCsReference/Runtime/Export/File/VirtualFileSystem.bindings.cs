// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 VirtualFileSystem — 虚拟文件系统
//
// 📌 作用：
//   管理 Unity 的虚拟文件系统（VFS），提供逻辑路径到
//   物理文件的映射查询功能。
//
// 💡 核心 API：
//   - GetLocalFileSystemName：将 VFS 文件名解析为本地
//     文件系统中的实际路径、偏移量和大小
//   - IsVFS：判断路径是否为 VFS 路径
//
// ⚡ 用于 Unity 的流式加载和资源管理系统。
// ==============================================================

using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace Unity.IO.LowLevel.Unsafe
{
    [NativeHeader("Runtime/VirtualFileSystem/VirtualFileSystem.h")]
    [StaticAccessor("GetFileSystem()", StaticAccessorType.Dot)]
    public static class VirtualFileSystem
    {
        [FreeFunction(IsThreadSafe = true)]
        public extern static bool GetLocalFileSystemName(string vfsFileName, out string localFileName, out ulong localFileOffset, out ulong localFileSize);

        internal extern static string ToLogicalPath(string physicalPath);
    }
}
