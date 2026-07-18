// ====================================================================================================
// 🎯 MemoryProfiling —— 内存快照系统
//
// 【功能定位】
//   提供运行时 / Editor 下抓取完整内存快照的能力，生成 .snap 文件供 Memory Profiler 窗口分析。
//   支持托管对象、原生对象、原生分配、堆栈跟踪等粒度的捕获。
//
// 【核心流程】
//   TakeSnapshot(path, finishCallback, screenshotCallback?, captureFlags)
//     → 异步操作，结果通过回调返回 (string path, bool result)
//   TakeTempSnapshot(finishCallback, captureFlags)
//     → 自动生成临时路径，适合快速排查
//
// 【CaptureFlags 控制粒度】
//   📌 ManagedObjects     —— 托管堆对象（Mono/IL2CPP 托管对象）
//   📌 NativeObjects      —— C++ 原生对象（GameObject/Mesh/Texture 等）
//   📌 NativeAllocations  —— 原生内存分配块
//   📌 NativeAllocationSites —— 分配调用栈（可用于定位内存泄漏源头）
//   📌 NativeStackTraces  —— 完整原生堆栈
//   ⚠️ 捕获粒度越细，.snap 文件越大，生成耗时越长。
//
// 【设计要点】
//   💡 静态 partial 类，事件驱动：m_SnapshotFinished / m_SaveScreenshotToDisk
//   💡 [AutoStaticsCleanupOnCodeReload] 确保重载时清理事件，避免悬挂引用
//   💡 编译中自动阻止快照（isCompiling 检查），防止不一致状态被捕获
//   💡 CreateMetaData → PrepareMetadata() 回调链允许注入自定义元数据
//   💡 SaveScreenshotToDisk 回调通过 NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray
//      零拷贝包装像素数据，避免大块内存复制
//   ⚠️ 一次只允许一个快照在进行中，嵌套快照会直接回调失败
// ====================================================================================================
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Profiling.Memory
{
    [Flags]
    public enum CaptureFlags : uint
    {
        ManagedObjects        = 1 << 0,
        NativeObjects         = 1 << 1,
        NativeAllocations     = 1 << 2,
        NativeAllocationSites = 1 << 3,
        NativeStackTraces     = 1 << 4,
    }

    public class MemorySnapshotMetadata
    {
        public string    Description { get; set; }
        // Not part of the public API for now, but Memory Profiler Package may choose to use and expose this
        // via unsafe code, the MetaDataInjector and extension methods.
        internal byte[]  Data { get; set; }
    }

    [NativeHeader("Runtime/Profiler/Runtime/MemorySnapshotManager.h")]
    public static partial class MemoryProfiler
    {
        [AutoStaticsCleanupOnCodeReload]
        private static event Action<string, bool> m_SnapshotFinished;
        [AutoStaticsCleanupOnCodeReload]
        private static event Action<string, bool, DebugScreenCapture> m_SaveScreenshotToDisk;

        [AutoStaticsCleanupOnCodeReload]
        public static event Action<MemorySnapshotMetadata>     CreatingMetadata;

        [AutoStaticsCleanupOnCodeReload]
        static bool isCompiling = false;
        internal static void StartedCompilationCallback(object msg)
        {
            isCompiling = true;
        }

        internal static void FinishedCompilationCallback(object msg)
        {
            isCompiling = false;
        }


        [StaticAccessor("profiling::memory::GetMemorySnapshotManager()", StaticAccessorType.Dot)]
        [NativeMethod("StartOperation")]
        [NativeConditional("ENABLE_PROFILER")]
        private static extern void StartOperation(uint captureFlags, bool requestScreenshot, string path, bool isRemote);

        [StaticAccessor("profiling::memory::GetMemorySnapshotManager()", StaticAccessorType.Dot)]
        [NativeMethod("RequestEditorTakeSnapshotOfPlayer")]
        [NativeConditional("ENABLE_PROFILER")]
        private static extern void RequestEditorTakeSnapshotOfPlayer(uint captureFlags, bool requestScreenshot);

        public static void TakeSnapshot(string path, Action<string, bool> finishCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects)
        {
            TakeSnapshot(path, finishCallback, null, captureFlags);
        }

        public static void TakeSnapshot(string path, Action<string, bool> finishCallback, Action<string, bool, DebugScreenCapture> screenshotCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects)
        {
            if (isCompiling)
            {
                Debug.LogError("Canceling snapshot, there is a compilation in progress.");
                return;
            }

            if (m_SnapshotFinished != null)
            {
                Debug.LogWarning("Canceling snapshot, there is another snapshot in progress.");
                finishCallback(path, false);
            }
            else
            {
                m_SnapshotFinished += finishCallback;
                m_SaveScreenshotToDisk += screenshotCallback;
                StartOperation((uint)captureFlags, m_SaveScreenshotToDisk != null, path, false);
            }
        }

        public static void TakeTempSnapshot(Action<string, bool>  finishCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects)
        {
            string[] s = Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];
            string path = Application.temporaryCachePath + "/" + projectName + ".snap";
            TakeSnapshot(path, finishCallback, captureFlags);
        }

        [RequiredByNativeCode]
        static byte[] PrepareMetadata()
        {
            if (CreatingMetadata == null)
            {
                return Array.Empty<byte>();
            }

            MemorySnapshotMetadata data = new MemorySnapshotMetadata();
            data.Description = string.Empty;
            CreatingMetadata(data);

            if (data.Description == null) data.Description = "";

            int contentLength = sizeof(char) * data.Description.Length;
            int dataLength = (data.Data == null ? 0 : data.Data.Length);

            int metaDataSize = contentLength + dataLength + sizeof(int) * 3 /*data.Description.Length + data.Data.Length*/;

            byte[] metaDataBytes = new byte[metaDataSize];
            // encoded as
            //   description_data_length
            //   description_data
            //   bytearraydata_data_length
            //   bytearraydata_data

            int offset = 0;
            offset = WriteIntToByteArray(metaDataBytes, offset, data.Description.Length);
            offset = WriteStringToByteArray(metaDataBytes, offset, data.Description);

            offset = WriteIntToByteArray(metaDataBytes, offset, dataLength);
            unsafe
            {
                fixed(byte* src = data.Data, dst = metaDataBytes)
                {
                    var start = dst + offset;
                    UnsafeUtility.MemCpy(start, src, dataLength);
                }
            }

            return metaDataBytes;
        }

        internal static int WriteIntToByteArray(byte[] array, int offset, int value)
        {
            unsafe
            {
                byte* pi = (byte*)&value;
                array[offset++] = pi[0];
                array[offset++] = pi[1];
                array[offset++] = pi[2];
                array[offset++] = pi[3];
            }

            return offset;
        }

        internal static int WriteStringToByteArray(byte[] array, int offset, string value)
        {
            if (value.Length != 0)
            {
                unsafe
                {
                    fixed(char* p = value)
                    {
                        char* begin = p;
                        char* end = p + value.Length;

                        while (begin != end)
                        {
                            for (int i = 0; i < sizeof(char); ++i)
                            {
                                array[offset++] = ((byte*)begin)[i];
                            }

                            begin++;
                        }
                    }
                }
            }

            return offset;
        }

        [RequiredByNativeCode]
        static void FinalizeSnapshot(string path, bool result)
        {
            if (m_SnapshotFinished != null)
            {
                var onSnapshotFinished = m_SnapshotFinished;

                m_SnapshotFinished = null;

                onSnapshotFinished(path, result);
            }
        }

        [RequiredByNativeCode]
        static void SaveScreenshotToDisk(string path, bool result, IntPtr pixelsPtr, int pixelsCount, TextureFormat format, int width, int height)
        {
            if (m_SaveScreenshotToDisk != null)
            {
                var saveScreenshotToDisk = m_SaveScreenshotToDisk;
                m_SaveScreenshotToDisk = null;
                DebugScreenCapture debugScreenCapture = default(DebugScreenCapture);

                if (result)
                {
                    unsafe
                    {
                        var nonOwningNativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(pixelsPtr.ToPointer(), pixelsCount, Allocator.Persistent);
                        debugScreenCapture.RawImageDataReference = nonOwningNativeArray;
                    }

                    debugScreenCapture.Height = height;
                    debugScreenCapture.Width = width;
                    debugScreenCapture.ImageFormat = format;
                }

                saveScreenshotToDisk(path, result, debugScreenCapture);
            }
        }
    }
}
