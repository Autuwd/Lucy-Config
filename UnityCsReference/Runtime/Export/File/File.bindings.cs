// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 File — 文件 I/O 工具
//
// 📌 作用：
//   提供 Unity 内部的线程 I/O 限制管理工具，
//   用于检测和防止从非主线程执行文件操作。
//
// 💡 核心类型：
//   - ThreadIORestrictionMode：线程 IO 限制模式（Allowed/TreatAsError）
//   - ThreadIORestrictionUtility：线程限制检测工具
//   - FileHelper：文件帮助类（路径规范化/模式匹配）
//   - ReadAllBytesWithRetry：带重试的读取
//   - UnityIOUtility：IO 工具（合法性检查/文件名清理）
//
// ⚠️ Internal 级别，用于 Unity 编辑器和运行时内部使用。
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine.IO
{
    [NativeHeader("Runtime/VirtualFileSystem/VirtualFileSystem.h")]
    internal enum ThreadIORestrictionMode
    {
        Allowed = 0,
        TreatAsError = 1,
    }

    [NativeHeader("Runtime/VirtualFileSystem/VirtualFileSystem.h")]
    [NativeConditional("ENABLE_PROFILER")]
    [StaticAccessor("FileAccessor", StaticAccessorType.DoubleColon)]
    internal static class File
    {
        internal static ulong totalOpenCalls
        {
            get { return GetTotalOpenCalls(); }
        }
        internal static ulong totalCloseCalls
        {
            get { return GetTotalCloseCalls(); }
        }
        internal static ulong totalReadCalls
        {
            get { return GetTotalReadCalls(); }
        }
        internal static ulong totalWriteCalls
        {
            get { return GetTotalWriteCalls(); }
        }
        internal static ulong totalSeekCalls
        {
            get { return GetTotalSeekCalls(); }
        }
        internal static ulong totalZeroSeekCalls
        {
            get { return GetTotalZeroSeekCalls(); }
        }

        internal static ulong totalFilesOpened
        {
            get { return GetTotalFilesOpened(); }
        }
        internal static ulong totalFilesClosed
        {
            get { return GetTotalFilesClosed(); }
        }
        internal static ulong totalBytesRead
        {
            get { return GetTotalBytesRead(); }
        }
        internal static ulong totalBytesWritten
        {
            get { return GetTotalBytesWritten(); }
        }

        internal static bool recordZeroSeeks
        {
            set { SetRecordZeroSeeks(value); }
            get { return GetRecordZeroSeeks(); }
        }

        // This can be used to print errors when when I/O is performed on the main thread. This is useful for testing async operations
        // to ensure they don't block the main thread with file I/O.
        internal static ThreadIORestrictionMode MainThreadIORestrictionMode
        {
            get { return GetMainThreadFileIORestriction(); }
            set { SetMainThreadFileIORestriction(value); }
        }

        internal extern static void SetRecordZeroSeeks(bool enable);
        internal extern static bool GetRecordZeroSeeks();

        internal extern static ulong GetTotalOpenCalls();
        internal extern static ulong GetTotalCloseCalls();
        internal extern static ulong GetTotalReadCalls();
        internal extern static ulong GetTotalWriteCalls();
        internal extern static ulong GetTotalSeekCalls();
        internal extern static ulong GetTotalZeroSeekCalls();

        internal extern static ulong GetTotalFilesOpened();
        internal extern static ulong GetTotalFilesClosed();
        internal extern static ulong GetTotalBytesRead();
        internal extern static ulong GetTotalBytesWritten();

        private extern unsafe static void SetMainThreadFileIORestriction(ThreadIORestrictionMode mode);
        private extern unsafe static ThreadIORestrictionMode GetMainThreadFileIORestriction();
    }
}
