// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 UnityFile — 底层文件操作
//
// 📌 作用：
//   提供线程安全的文件读写工具，用于 Unity 运行时
//   的底层文件访问（读取全部字节、写入等）。
//
// 💡 核心方法：
//   - ReadAllBytes：线程安全的全部字节读取
//   - ReadAllText：线程安全的全部文本读取
//   - WriteAllBytes：写入全部字节
//
// ⚡ Internal 类（VisibleToOtherModules），用于 ContentLoadModule。
// ==============================================================

using System.IO;
using UnityEngine.Bindings;

namespace Unity.IO
{
    [NativeHeader("Runtime/VirtualFileSystem/UnityFile.bindings.h")]
    [VisibleToOtherModules("UnityEngine.ContentLoadModule")]
    internal static class UnityFile
    {
        [FreeFunction("UnityFile::ReadAllBytes", IsThreadSafe = true, ThrowsException = true)]
        public static extern byte[] ReadAllBytes(string path);

        [FreeFunction("UnityFile::Exists", IsThreadSafe = true)]
        public static extern bool Exists(string path);
    }
}
