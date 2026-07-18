// ====================================================================================================
// 🎯 ProfilerUnsafeUtility —— Profiler 底层 unsafe 原生绑定层（静态类）
//
// 【功能定位】
//   所有 ProfilerMarker / ProfilerRecorder / Counter 等现代 Profiler API 的底层基础设施。
//   直接对接 C++ 端 ProfilerUnsafeUtility.bindings.h，提供托管 ↔ 原生桥接。
//
// 【核心服务】
//   📌 Marker 管理：CreateMarker / GetMarker / SetMarkerMetadata（含 Burst shadow __Unmanaged 入口）
//   📌 采样控制：BeginSample / EndSample / BeginSampleWithMetadata / SingleSampleWithMetadata
//   📌 类别管理：CreateCategory / GetCategoryByName / GetCategoryDescription / GetCategoryColor
//   📌 计数器系统：CreateCounterValue / FlushCounterValue（支持 Burst）
//   📌 时间工具：Timestamp 属性 + TimestampToNanosecondsConversionRatio 实现高精度时间转换
//   📌 流跟踪：CreateFlow / FlowEvent 支持异步流追踪（ProfilerFlowEventType）
//
// 【Burst 双入口模式】
//   💡 每个 API 提供两组原生入口：
//     1. 托管入口：接受 string / char* 参数，适合常规 C# 调用
//     2. __Unmanaged 入口：接受 byte* + int 参数，Burst 编译时自动替代
//   ⚠️ __Unmanaged 方法标记 [RequiredMember]，确保 IL2CPP 不会剥离。
//   💡 托管方法通过 [MethodImpl(256)] + char* 转发到 _Unsafe 内部 extern，减少 P/Invoke 路径差异。
//
// 【数据结构】
//   📌 ProfilerMarkerData (显式布局 16 字节) —— 元数据载荷 {Type, Size, Ptr}
//   📌 ProfilerCategoryDescription (显式布局 24 字节) —— 类别描述 {Id, Flags, Color, Name}
//   📌 TimestampConversionRatio —— 时间戳 → 纳秒转换比率 {Numerator, Denominator}
//
// 【类别常量】
//   内置类别从 CategoryRender(0) 到 CategoryAny(0xFFFF)，涵盖渲染/脚本/GUI/物理/动画等全部子系统。
//   ⚠️ CategoryLightning → CategoryLighting 更名标记 [Obsolete]。
// ====================================================================================================
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace Unity.Profiling.LowLevel.Unsafe
{
    // Metadata parameter.
    // Must be in sync with UnityProfilerMarkerData!
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct ProfilerMarkerData
    {
        [FieldOffset(0)] public byte Type;
        [FieldOffset(1)] readonly byte reserved0;
        [FieldOffset(2)] readonly ushort reserved1;
        [FieldOffset(4)] public uint Size;
        [FieldOffset(8)] public void* Ptr;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public readonly unsafe struct ProfilerCategoryDescription
    {
        [FieldOffset(0)]  public readonly ushort Id;
        [FieldOffset(2)]  public readonly ushort Flags;
        [FieldOffset(4)]  public readonly Color32 Color;
        [FieldOffset(8)]  readonly int reserved0;
        [FieldOffset(12)] public readonly int NameUtf8Len;
        [FieldOffset(16)] public readonly byte* NameUtf8;

        public string Name => ProfilerUnsafeUtility.Utf8ToString(NameUtf8, NameUtf8Len);
    }

    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerUnsafeUtility.bindings.h")]
    [UsedByNativeCode]
    [IgnoredByDeepProfiler]
    public static class ProfilerUnsafeUtility
    {
        // Built-in profiler categories.
        // Must be in sync with profiling::BuiltinCategory!
        public const ushort CategoryRender = 0;
        public const ushort CategoryScripts = 1;
        public const ushort CategoryGUI = 4;
        public const ushort CategoryPhysics = 5;
        public const ushort CategoryAnimation = 6;
        public const ushort CategoryAi = 7;
        public const ushort CategoryAudio = 8;
        public const ushort CategoryVideo = 11;
        public const ushort CategoryParticles = 12;
        public const ushort CategoryLighting = 13;
        [Obsolete("CategoryLightning has been renamed. Use CategoryLighting instead (UnityUpgradable) -> CategoryLighting", false)]
        public const ushort CategoryLightning = 13;
        public const ushort CategoryNetwork = 14;
        public const ushort CategoryLoading = 15;
        public const ushort CategoryOther = 16;
        public const ushort CategoryVr = 22;
        public const ushort CategoryAllocation = 23;
        public const ushort CategoryInternal = 24;
        public const ushort CategoryFileIO = 25;
        public const ushort CategoryInput = 30;
        public const ushort CategoryVirtualTexturing = 31;
        internal const ushort CategoryGPU = 32;
        public const ushort CategoryPhysics2D = 33;
        public const ushort CategoryU2D = 39;
        public const ushort CategoryUIToolkit = 40;
        internal const ushort CategoryAny = 0xFFFF;

        [NativeMethod(IsThreadSafe = true)]
        internal static extern ushort CreateCategory(string name, ProfilerCategoryColor colorIndex);

        // Burst shadow
        [NativeMethod(IsThreadSafe = true)]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe ushort CreateCategory__Unmanaged(byte* name, int nameLen, ProfilerCategoryColor colorIndex);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe ushort CreateCategory(char* name, int nameLen, ProfilerCategoryColor colorIndex)
        {
            return CreateCategory_Unsafe(name, nameLen, colorIndex);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe ushort CreateCategory_Unsafe(char* name, int nameLen, ProfilerCategoryColor colorIndex);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe ushort GetCategoryByName(char* name, int nameLen)
        {
            return GetCategoryByName_Unsafe(name, nameLen);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe ushort GetCategoryByName_Unsafe(char* name, int nameLen);

        [NativeMethod(IsThreadSafe = true)]
        public static extern ProfilerCategoryDescription GetCategoryDescription(ushort categoryId);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern Color32 GetCategoryColor(ProfilerCategoryColor colorIndex);

        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr CreateMarker(string name, ushort categoryId, MarkerFlags flags, int metadataCount);
        // Burst shadow
        [NativeMethod(IsThreadSafe = true)]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe IntPtr CreateMarker__Unmanaged(byte* name, int nameLen, ushort categoryId, MarkerFlags flags, int metadataCount);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe IntPtr CreateMarker(char* name, int nameLen, ushort categoryId, MarkerFlags flags, int metadataCount)
        {
            return CreateMarker_Unsafe(name, nameLen, categoryId, flags, metadataCount);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe IntPtr CreateMarker_Unsafe(char* name, int nameLen, ushort categoryId, MarkerFlags flags, int metadataCount);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern IntPtr GetMarker(string name);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetMarkerMetadata(IntPtr markerPtr, int index, string name, byte type, byte unit);
        // Burst shadow
        [NativeMethod(IsThreadSafe = true)]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe void SetMarkerMetadata__Unmanaged(IntPtr markerPtr, int index, byte* name, int nameLen, byte type, byte unit);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe void SetMarkerMetadata(IntPtr markerPtr, int index, char* name, int nameLen, byte type, byte unit)
        {
            SetMarkerMetadata_Unsafe(markerPtr, index, name, nameLen, type, unit);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe void SetMarkerMetadata_Unsafe(IntPtr markerPtr, int index, char* name, int nameLen, byte type, byte unit);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void BeginSample(IntPtr markerPtr);

        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void BeginSampleWithMetadata(IntPtr markerPtr, int metadataCount, void* metadata);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void EndSample(IntPtr markerPtr);

        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void SingleSampleWithMetadata(IntPtr markerPtr, int metadataCount, void* metadata);

        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void* CreateCounterValue(out IntPtr counterPtr, string name, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions);
        // Burst shadow
        [NativeMethod(IsThreadSafe = true)]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe void* CreateCounterValue__Unmanaged(out IntPtr counterPtr, byte* name, int nameLen, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe void* CreateCounterValue(out IntPtr counterPtr, char* name, int nameLen, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions)
        {
            return CreateCounterValue_Unsafe(out counterPtr, name, nameLen, categoryId, flags, dataType, dataUnit, dataSize, counterOptions);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe void* CreateCounterValue_Unsafe(out IntPtr counterPtr, char* name, int nameLen, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions);

        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void FlushCounterValue(void* counterValuePtr);

        internal static unsafe string Utf8ToString(byte* chars, int charsLen)
        {
            if (chars == null)
                return null;

            var arr = new byte[charsLen];
            Marshal.Copy((IntPtr)chars, arr, 0, charsLen);
            return Encoding.UTF8.GetString(arr, 0, charsLen);
        }

        [NativeMethod(IsThreadSafe = true)]
        public static extern uint CreateFlow(ushort categoryId);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void FlowEvent(uint flowId, ProfilerFlowEventType flowEventType);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void Internal_BeginWithObject(IntPtr markerPtr, UnityEngine.Object contextUnityObject);

        [NativeConditional("ENABLE_PROFILER")]
        internal static extern string Internal_GetName(IntPtr markerPtr);

        public static extern long Timestamp
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public struct TimestampConversionRatio
        {
            public long Numerator;
            public long Denominator;
        }

        public static extern TimestampConversionRatio TimestampToNanosecondsConversionRatio
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        internal static extern IntPtr GetOrCreateMemLabel(string areaName, string objectName);

        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe IntPtr GetOrCreateMemLabel__Unmanaged(byte* areaName, int areaNameLen, byte* objectName, int objectNameLen);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        internal static extern long GetMemLabelRelatedMemorySize(IntPtr label);

    }
}
