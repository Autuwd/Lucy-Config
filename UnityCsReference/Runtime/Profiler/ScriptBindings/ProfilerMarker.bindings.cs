// ====================================================================================================
// 🎯 ProfilerMarker —— 零分配、Burst 兼容的现代性能标记（struct）
//
// 【与 Profiler.BeginSample 的核心区别】
//   💡 ProfilerMarker 是值类型 struct，而非字符串查找——构造函数中提前创建 native marker，
//      Begin/End 仅通过 IntPtr m_Ptr 直接调用 native 层，避免每帧字符串哈希。
//   🎯 Profiler.BeginSample(string) 每次调用需字符串查找 + 哈希，GC 分配；ProfilerMarker 零 GC。
//   🎯 ProfilerMarker 支持 AutoScope（using 模式）自动配对 Begin/End，防止遗漏。
//   💡 完整支持 metadata 传递：Auto(string metadata) / BeginWithObject，适合传上下文对象。
//   ⚠️ 所有 profiling 方法标记 [Conditional("ENABLE_PROFILER")]，Release 编译时被剥离。
//
// 【构造方式】
//   6 组构造函数重载覆盖：
//     - (string name)                                    —— 默认 CategoryScripts
//     - (char* name, int nameLen)                       —— unsafe 版本，避免托管字符串分配
//     - (ProfilerCategory category, string name)         —— 指定类别
//     - (ProfilerCategory category, char* name, int nameLen)
//     - (string name, MarkerFlags flags)                 —— 自定义标志
//     - (ProfilerCategory category, string name, MarkerFlags flags)
//   📌 所有构造函数 [MethodImpl(256)] AggressiveInlining，控制内联到调用点。
//
// 【AutoScope 嵌套结构】
//   AutoScope 是 ProfilerMarker 的内部 struct，实现 IDisposable：
//     using (marker.Auto()) { ... }  等价于 marker.Begin(); ... marker.End();
//   ⚠️ AutoScope 同样通过 m_Ptr 非空判断进行防御性保护。
//
// 【关联类型】
//   ProfilerFlowEventType (byte)   —— 流事件类型 Begin/ParallelNext/End/Next
//   ProfilerMarkerDataUnit (byte)  —— 数据单元（TimeNanoseconds/Bytes/Count/Percent/FrequencyHz）
//   ProfilerCounterOptions (ushort) —— 计数器选项（FlushOnEndOfFrame/ResetToZeroOnFlush）
// ====================================================================================================
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace Unity.Profiling
{
    [UsedByNativeCode]
    [IgnoredByDeepProfiler]
    [StructLayout(LayoutKind.Sequential)]
    public struct ProfilerMarker
    {
        [NativeDisableUnsafePtrRestriction]
        [NonSerialized]
        internal readonly IntPtr m_Ptr;

        public IntPtr Handle => m_Ptr;

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public ProfilerMarker(string name)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, ProfilerUnsafeUtility.CategoryScripts, MarkerFlags.Default, 0);
        }

        [MethodImpl(256)]
        public unsafe ProfilerMarker(char* name, int nameLen)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, nameLen, ProfilerUnsafeUtility.CategoryScripts, MarkerFlags.Default, 0);
        }

        [MethodImpl(256)]
        public ProfilerMarker(ProfilerCategory category, string name)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, category, MarkerFlags.Default, 0);
        }

        [MethodImpl(256)]
        public unsafe ProfilerMarker(ProfilerCategory category, char* name, int nameLen)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, nameLen, category, MarkerFlags.Default, 0);
        }

        [MethodImpl(256)]
        public ProfilerMarker(string name, MarkerFlags flags)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, ProfilerUnsafeUtility.CategoryScripts, flags, 0);
        }

        [MethodImpl(256)]
        public unsafe ProfilerMarker(char* name, int nameLen, MarkerFlags flags)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, nameLen, ProfilerUnsafeUtility.CategoryScripts, flags, 0);
        }

        [MethodImpl(256)]
        public ProfilerMarker(ProfilerCategory category, string name, MarkerFlags flags)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, category, flags, 0);
        }

        [MethodImpl(256)]
        public unsafe ProfilerMarker(ProfilerCategory category, char* name, int nameLen, MarkerFlags flags)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, nameLen, category, flags, 0);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        [Pure]
        public void Begin()
        {
            ProfilerUnsafeUtility.BeginSample(m_Ptr);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public void Begin(Object contextUnityObject)
        {
            ProfilerUnsafeUtility.Internal_BeginWithObject(m_Ptr, contextUnityObject);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        [Pure]
        public void End()
        {
            ProfilerUnsafeUtility.EndSample(m_Ptr);
        }

        [Conditional("ENABLE_PROFILER")]
        internal void GetName(ref string name)
        {
            name = ProfilerUnsafeUtility.Internal_GetName(m_Ptr);
        }

        [UsedByNativeCode]
        [IgnoredByDeepProfiler]
        public struct AutoScope : IDisposable
        {
            [NativeDisableUnsafePtrRestriction]
            internal readonly IntPtr m_Ptr;

            [MethodImpl(256)]
            internal AutoScope(IntPtr markerPtr)
            {
                m_Ptr = markerPtr;
                if (markerPtr != IntPtr.Zero)
                    ProfilerUnsafeUtility.BeginSample(markerPtr);
            }

            [MethodImpl(256)]
            internal AutoScope(IntPtr markerPtr, Object contextUnityObject)
            {
                m_Ptr = markerPtr;
                if (markerPtr != IntPtr.Zero)
                    ProfilerUnsafeUtility.Internal_BeginWithObject(markerPtr, contextUnityObject);
            }

            [MethodImpl(256)]
            internal unsafe AutoScope(IntPtr markerPtr, string metadata)
            {
                m_Ptr = markerPtr;
                if (markerPtr != IntPtr.Zero)
                {
                    if (String.IsNullOrEmpty(metadata))
                    {
                        ProfilerUnsafeUtility.BeginSample(markerPtr);
                    }
                    else
                    {
                        ProfilerMarkerData data = new ProfilerMarkerData { Type = (byte)ProfilerMarkerDataType.String16 };
                        fixed (char* metadataChars = metadata)
                        {
                            data.Size = ((uint)metadata.Length + 1) * 2;
                            data.Ptr = metadataChars;
                            ProfilerUnsafeUtility.BeginSampleWithMetadata(markerPtr, 1, &data);
                        }
                    }
                }
            }

            [MethodImpl(256)]
            public void Dispose()
            {
                if (m_Ptr != IntPtr.Zero)
                    ProfilerUnsafeUtility.EndSample(m_Ptr);
            }
        }

        [MethodImpl(256)]
        [Pure]
        public AutoScope Auto()
        {
            return new AutoScope(m_Ptr);
        }

        [MethodImpl(256)]
        [Pure]
        public AutoScope Auto(Object contextUnityObject)
        {
            return new AutoScope(m_Ptr, contextUnityObject);
        }

        [MethodImpl(256)]
        [Pure]
        public AutoScope Auto(string metadata)
        {
            return new AutoScope(m_Ptr, metadata);
        }
    }

    // Supported profiler flow types.
    // Must be in sync with UnityProfilerFlowEventType!
    public enum ProfilerFlowEventType : byte
    {
        Begin = 0,
        ParallelNext = 1,
        End = 2,
        Next = 3,
    }

    // Supported profiler metadata units.
    // Must be in sync with UnityProfilerMarkerDataUnit!
    public enum ProfilerMarkerDataUnit : byte
    {
        Undefined = 0,
        TimeNanoseconds = 1,
        Bytes = 2,
        Count = 3,
        Percent = 4,
        FrequencyHz = 5,
    }

    // Supported profiler metadata types.
    // Must be in sync with profiling::CounterBase::Flags!
    [Flags]
    public enum ProfilerCounterOptions : ushort
    {
        None = 0,
        FlushOnEndOfFrame = 1 << 1,
        ResetToZeroOnFlush = 1 << 2,
    }
}
