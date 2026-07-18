// ====================================================================================================
// 🎯 ProfilerRecorder —— 帧数据采集器（值类型 struct，支持 Burst）
//
// 【功能定位】
//   以零分配方式按帧收集 profiler 统计数据（CPU/GPU 耗时、计数、内存等），
//   替代旧的 Recorder 类（class，有 GC 压力），是 Unity 现代 Profiler 采集层的核心值类型。
//
// 【架构分层】
//   📌 ProfilerRecorderDescription (16 字节显式布局) —— 描述符：类别、标志、数据类型、名称
//   📌 ProfilerRecorderHandle (8 字节显式布局)        —— 句柄：定位特定统计量，内部 ulong
//   📌 ProfilerRecorder (顺序布局)                    —— 采集器主体：创建 → 控制 → 读取 → 释放
//
// 【核心能力】
//   💡 采集方式：Start/Stop/Reset 控制生命周期；StartImmediately 选项可在构造时自动启动。
//   💡 读取方式：CurrentValue / LastValue（立即值），GetSample(index) / CopyTo（历史样本）。
//   💡 容量控制：capacity 参数控制环形缓冲区大小；WrapAroundWhenCapacityReached 控制溢出行为。
//   💡 线程过滤：FilterToCurrentThread / CollectFromAllThreads 控制采样范围。
//   ⚠️ 所有 extern 方法标记 [NativeMethod(IsThreadSafe = true)]，支持多线程安全调用。
//   ⚠️ CheckInitializedAndThrow / CheckInitializedWithParamsAndThrow 用 [BurstDiscard] 包起来，
//      确保 Burst 编译下不产生异常路径。
//
// 【Burst 兼容设计】
//   💡 GetByName__Unmanaged(category, byte*, int) 是 Burst shadow 入口，Burst 编译器自动选取。
//   💡 ProfilerRecorderSample 纯数据字段（long/value/count/refValue），无方法，适合 I/O 密集。
//
// 【生命周期】
//   Create → Control(Start) → GetLastValue/CopyTo → Control(Stop) → Dispose
//   ⚠️ 必须显式调用 Dispose()，否则泄漏 native 句柄。handle = 0 标记已释放。
// ====================================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Profiling.LowLevel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Profiling.LowLevel.Unsafe
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public readonly unsafe struct ProfilerRecorderDescription
    {
        [FieldOffset(0)] readonly ProfilerCategory category;
        [FieldOffset(2)] readonly MarkerFlags flags;
        [FieldOffset(4)] readonly ProfilerMarkerDataType dataType;
        [FieldOffset(5)] readonly ProfilerMarkerDataUnit unitType;
        [FieldOffset(8)] readonly int reserved0;
        [FieldOffset(12)] readonly int nameUtf8Len;
        [FieldOffset(16)] readonly byte* nameUtf8;

        public ProfilerCategory Category => category;
        public MarkerFlags Flags => flags;
        public ProfilerMarkerDataType DataType => dataType;
        public ProfilerMarkerDataUnit UnitType => unitType;
        public int NameUtf8Len => nameUtf8Len;
        public byte* NameUtf8 => nameUtf8;
        public string Name => ProfilerUnsafeUtility.Utf8ToString(nameUtf8, nameUtf8Len);
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct ProfilerRecorderHandle
    {
        const ulong k_InvalidHandle = ~0x0ul;

        [FieldOffset(0)]
        internal readonly ulong handle;

        internal ProfilerRecorderHandle(ulong handle)
        {
            this.handle = handle;
        }

        public bool Valid => handle != 0 && handle != k_InvalidHandle;

        internal static ProfilerRecorderHandle Get(ProfilerMarker marker)
        {
            return new ProfilerRecorderHandle((ulong)marker.Handle.ToInt64());
        }

        internal static ProfilerRecorderHandle Get(ProfilerCategory category, string statName)
        {
            if (string.IsNullOrEmpty(statName))
                throw new ArgumentException("String must be not null or empty", nameof(statName));

            return GetByName(category, statName);
        }

        public static ProfilerRecorderDescription GetDescription(ProfilerRecorderHandle handle)
        {
            if (!handle.Valid)
                throw new ArgumentException("ProfilerRecorderHandle is not initialized or is not available", nameof(handle));

            return GetDescriptionInternal(handle);
        }

        [NativeMethod(IsThreadSafe = true)]
        public static extern void GetAvailable([NotNull] List<ProfilerRecorderHandle> outRecorderHandleList);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern ProfilerRecorderHandle GetByName(ProfilerCategory category, string name);
        // Burst shadow
        [NativeMethod(IsThreadSafe = true)]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe ProfilerRecorderHandle GetByName__Unmanaged(ProfilerCategory category, byte* name, int nameLen);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        internal static unsafe ProfilerRecorderHandle GetByName(ProfilerCategory category, char* name, int nameLen)
        {
            return GetByName_Unsafe(category, name, nameLen);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe ProfilerRecorderHandle GetByName_Unsafe(ProfilerCategory category, char* name, int nameLen);

        [NativeMethod(IsThreadSafe = true)]
        static extern ProfilerRecorderDescription GetDescriptionInternal(ProfilerRecorderHandle handle);
    }
}

namespace Unity.Profiling
{
    [Flags]
    public enum ProfilerRecorderOptions
    {
        None = 0,
        StartImmediately = 1 << 0,
        KeepAliveDuringDomainReload = 1 << 1,
        CollectOnlyOnCurrentThread = 1 << 2,
        WrapAroundWhenCapacityReached = 1 << 3,
        SumAllSamplesInFrame = 1 << 4,
        GpuRecorder = 1 << 6,

        Default = WrapAroundWhenCapacityReached | SumAllSamplesInFrame
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Value = {Value}; Count = {Count}")]
    public struct ProfilerRecorderSample
    {
        long value;
        long count;
        long refValue;

        public long Value => value;
        public long Count => count;
    }

    [UsedByNativeCode]
    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerRecorder.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    [DebuggerTypeProxy(typeof(ProfilerRecorderDebugView))]
    public struct ProfilerRecorder : IDisposable
    {
        internal ulong handle;

        internal enum ControlOptions
        {
            Start = 0,
            Stop = 1,
            Reset = 2,
            Release = 4,
            SetFilterToCurrentThread = 5,
            SetToCollectFromAllThreads = 6,
        }

        internal const ProfilerRecorderOptions SharedRecorder = (ProfilerRecorderOptions)(1 << 7);

        internal enum CountOptions
        {
            Count = 0,
            MaxCount = 1,
        }

        public ProfilerRecorder(string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
            : this(ProfilerCategory.Any, statName, capacity, options)
        {
        }

        public ProfilerRecorder(string categoryName, string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
            : this(new ProfilerCategory(categoryName), statName, capacity, options)
        {
        }

        public unsafe ProfilerRecorder(ProfilerCategory category, string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            LowLevel.Unsafe.ProfilerRecorderHandle statHandle;
            statHandle = LowLevel.Unsafe.ProfilerRecorderHandle.GetByName(category, statName);
            this = Create(statHandle, capacity, options);
        }

        public unsafe ProfilerRecorder(ProfilerCategory category, char* statName, int statNameLen, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            var statHandle = LowLevel.Unsafe.ProfilerRecorderHandle.GetByName(category, statName, statNameLen);
            this = Create(statHandle, capacity, options);
        }

        public ProfilerRecorder(ProfilerMarker marker, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            this = Create(LowLevel.Unsafe.ProfilerRecorderHandle.Get(marker), capacity, options);
        }

        public ProfilerRecorder(LowLevel.Unsafe.ProfilerRecorderHandle statHandle, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            this = Create(statHandle, capacity, options);
        }

        public static unsafe ProfilerRecorder StartNew(ProfilerCategory category, string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            fixed(char* c = statName)
            {
                return new ProfilerRecorder(category, c, statName.Length, capacity, options | ProfilerRecorderOptions.StartImmediately);
            }
        }

        public static ProfilerRecorder StartNew(ProfilerMarker marker, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            return new ProfilerRecorder(marker, capacity, options | ProfilerRecorderOptions.StartImmediately);
        }

        internal static ProfilerRecorder StartNew()
        {
            return Create(new LowLevel.Unsafe.ProfilerRecorderHandle(), 0, ProfilerRecorderOptions.StartImmediately);
        }

        public bool Valid => handle != 0 && GetValid(this);

        public ProfilerMarkerDataType DataType
        {
            get
            {
                CheckInitializedAndThrow();
                return GetValueDataType(this);
            }
        }

        public ProfilerMarkerDataUnit UnitType
        {
            get
            {
                CheckInitializedAndThrow();
                return GetValueUnitType(this);
            }
        }

        public void Start()
        {
            CheckInitializedAndThrow();
            Control(this, ControlOptions.Start);
        }

        public void Stop()
        {
            CheckInitializedAndThrow();
            Control(this, ControlOptions.Stop);
        }

        public void Reset()
        {
            CheckInitializedAndThrow();
            Control(this, ControlOptions.Reset);
        }

        public long CurrentValue
        {
            get
            {
                CheckInitializedAndThrow();
                return GetCurrentValue(this);
            }
        }

        public double CurrentValueAsDouble
        {
            get
            {
                CheckInitializedAndThrow();
                return GetCurrentValueAsDouble(this);
            }
        }

        public long LastValue
        {
            get
            {
                CheckInitializedAndThrow();
                return GetLastValue(this);
            }
        }

        public double LastValueAsDouble
        {
            get
            {
                CheckInitializedAndThrow();
                return GetLastValueAsDouble(this);
            }
        }

        public int Capacity
        {
            get
            {
                CheckInitializedAndThrow();
                return GetCount(this, CountOptions.MaxCount);
            }
        }

        public int Count
        {
            get
            {
                CheckInitializedAndThrow();
                return GetCount(this, CountOptions.Count);
            }
        }

        public bool IsRunning
        {
            get
            {
                CheckInitializedAndThrow();
                return GetRunning(this);
            }
        }

        public bool WrappedAround
        {
            get
            {
                CheckInitializedAndThrow();
                return GetWrapped(this);
            }
        }

        public ProfilerRecorderSample GetSample(int index)
        {
            CheckInitializedAndThrow();
            return GetSampleInternal(this, index);
        }

        public void CopyTo(List<ProfilerRecorderSample> outSamples, bool reset = false)
        {
            if (outSamples == null)
                throw new ArgumentNullException(nameof(outSamples));
            CheckInitializedAndThrow();
            CopyTo_List(this, outSamples, reset);
        }

        public unsafe int CopyTo(ProfilerRecorderSample* dest, int destSize, bool reset = false)
        {
            CheckInitializedWithParamsAndThrow(dest);
            return CopyTo_Pointer(this, dest, destSize, reset);
        }

        public unsafe ProfilerRecorderSample[] ToArray()
        {
            CheckInitializedAndThrow();

            var count = Count;
            var array = new ProfilerRecorderSample[count];
            fixed(ProfilerRecorderSample* p = array)
            {
                _ = CopyTo_Pointer(this, p, count, false);
            }

            return array;
        }

        internal void FilterToCurrentThread()
        {
            CheckInitializedAndThrow();
            Control(this, ControlOptions.SetFilterToCurrentThread);
        }

        internal void CollectFromAllThreads()
        {
            CheckInitializedAndThrow();
            Control(this, ControlOptions.SetToCollectFromAllThreads);
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        static extern ProfilerRecorder Create(LowLevel.Unsafe.ProfilerRecorderHandle statHandle, int maxSampleCount, ProfilerRecorderOptions options);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        static extern void Control(ProfilerRecorder handle, ControlOptions options);

        [NativeMethod(IsThreadSafe = true)]
        static extern ProfilerMarkerDataUnit GetValueUnitType(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern ProfilerMarkerDataType GetValueDataType(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern long GetCurrentValue(ProfilerRecorder handle);
        [NativeMethod(IsThreadSafe = true)]
        static extern double GetCurrentValueAsDouble(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern long GetLastValue(ProfilerRecorder handle);
        [NativeMethod(IsThreadSafe = true)]
        static extern double GetLastValueAsDouble(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern int GetCount(ProfilerRecorder handle, CountOptions countOptions);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool GetValid(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool GetWrapped(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool GetRunning(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        static extern ProfilerRecorderSample GetSampleInternal(ProfilerRecorder handle, int index);

        [NativeMethod(IsThreadSafe = true)]
        static extern void CopyTo_List(ProfilerRecorder handle, List<ProfilerRecorderSample> outSamples, bool reset);

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe int CopyTo_Pointer(ProfilerRecorder handle, ProfilerRecorderSample* outSamples, int outSamplesSize, bool reset);

        public void Dispose()
        {
            if (handle == 0)
                return;

            Control(this, ControlOptions.Release);
            handle = 0;
        }

        [BurstDiscard]
        unsafe void CheckInitializedWithParamsAndThrow(ProfilerRecorderSample* dest)
        {
            if (handle == 0)
                throw new InvalidOperationException("ProfilerRecorder object is not initialized or has been disposed.");
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));
        }

        [BurstDiscard]
        void CheckInitializedAndThrow()
        {
            if (handle == 0)
                throw new InvalidOperationException("ProfilerRecorder object is not initialized or has been disposed.");
        }
    }

    sealed class ProfilerRecorderDebugView
    {
        ProfilerRecorder m_Recorder;

        public ProfilerRecorderDebugView(ProfilerRecorder r)
        {
            m_Recorder = r;
        }

        public ProfilerRecorderSample[] Items => m_Recorder.ToArray();
    }
}
