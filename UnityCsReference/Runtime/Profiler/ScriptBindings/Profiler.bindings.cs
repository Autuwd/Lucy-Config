// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ====================================================================================================
// 🎯 Profiler —— Unity 引擎性能分析器核心类
//
// 【功能定位】
//   静态密封类，提供 Profiler 系统的全局开关、区域控制、内存查询和采样元数据发射。
//   是 Profiler 的"传统入口"——大多数方法标记为 [Conditional("ENABLE_PROFILER")]，
//   在 Release 编译时会被完全剥离，零运行时开销。
//
// 【设计要点】
//   💡 BeginSample / EndSample 已标记 [Obsolete]，官方推荐升级到 CustomSampler / ProfilerMarker。
//   💡 所有 GetXxxMemoryLong() 方法解决了 4GB 上限问题（原 GetXxxMemory() 已废弃）。
//   💡 EmitFrameMetaData / EmitSessionMetaData 支持 Array / List<T> / NativeArray<T> 三重重载，
//      但要求数据类型必须是 blittable（通过 UnsafeUtility.IsBlittable 校验）。
//   💡 Internal_EmitGlobalMetaData_Span / _Native 两个 extern 入口嫁接 native ProfilerBindings 模块。
//   ⚠️ 内存系列方法（Allocated/Reserved/Fragmentation）仅在 ENABLE_MEMORY_MANAGER 下生效。
//   ⚠️ ProfilerArea 是旧式区域枚举；新式 ProfilerCategory 是 ushort 值类型（见 ProfilerCategory.cs）。
//
// 【关系网络】
//   ProfilerCategory.cs       -> 新的类别系统（ushort 值类型）
//   ProfilerMarker.bindings.cs -> 推荐的零分配标记替代方案
//   ProfilerUnsafeUtility      -> 底层原生绑定层
// ====================================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using Unity.Profiling;

namespace UnityEngine.Profiling
{
    public enum ProfilerArea
    {
        CPU,
        GPU,
        Rendering,
        Memory,
        Audio,
        Video,
        Physics,
        Physics2D,
        NetworkMessages,
        NetworkOperations,
        UI,
        UIDetails,
        GlobalIllumination,
        VirtualTexturing,
    }

    [UsedByNativeCode]
    [IgnoredByDeepProfiler]
    [MovedFrom("UnityEngine")]
    [NativeHeader("NativeKernel/Allocator/MemoryManager.h")]
    [NativeHeader("NativeKernel/Profiler/MemoryProfiler.h")]
    [NativeHeader("Runtime/Profiler/Profiler.h")]
    [NativeHeader("Runtime/Profiler/ScriptBindings/Profiler.bindings.h")]
    [NativeHeader("Scripting/ScriptingBackend/ScriptingApi.h")]
    [NativeHeader("NativeKernel/Utilities/MemoryUtilities.h")]
    public sealed class Profiler
    {
        internal const uint invalidProfilerArea = ~0u;

        // This class can't be explicitly created
        private Profiler() {}

        // *undocumented*
        public extern static bool supported
        {
            [NativeMethod(Name = "profiler_is_available", IsFreeFunction = true)]
            get;
        }

        // Sets profiler output file in built players.
        [StaticAccessor("ProfilerBindings", StaticAccessorType.DoubleColon)]
        public extern static string logFile
        {
            get;
            set;
        }

        // Sets profiler output file in built players.
        public extern static bool enableBinaryLog
        {
            [NativeMethod(Name = "ProfilerBindings::IsBinaryLogEnabled", IsFreeFunction = true)]
            get;
            [NativeMethod(Name = "ProfilerBindings::SetBinaryLogEnabled", IsFreeFunction = true)]
            set;
        }

        public extern static int maxUsedMemory
        {
            [NativeMethod(Name = "ProfilerBindings::GetMaxUsedMemory", IsFreeFunction = true)]
            get;
            [NativeMethod(Name = "ProfilerBindings::SetMaxUsedMemory", IsFreeFunction = true)]
            set;
        }

        // Enables the Profiler.
        public extern static bool enabled
        {
            [NativeConditional("ENABLE_PROFILER")]
            [NativeMethod(Name = "profiler_is_enabled", IsFreeFunction = true, IsThreadSafe = true)]
            get;

            [NativeMethod(Name = "ProfilerBindings::SetProfilerEnabled", IsFreeFunction = true)]
            set;
        }

        public extern static bool enableAllocationCallstacks
        {
            [NativeMethod(Name = "ProfilerBindings::IsAllocationCallstackCaptureEnabled", IsFreeFunction = true)]
            get;
            [NativeMethod(Name = "ProfilerBindings::SetAllocationCallstackCaptureEnabled", IsFreeFunction = true)]
            set;
        }


        [Conditional("ENABLE_PROFILER")]
        [FreeFunction("ProfilerBindings::profiler_set_area_enabled")]
        public extern static void SetAreaEnabled(ProfilerArea area, bool enabled);

        // TODO
        //[Obsolete("", false)]
        public static int areaCount
        {
            get
            {
                return Enum.GetNames(typeof(ProfilerArea)).Length;
            }
        }


        [NativeConditional("ENABLE_PROFILER")]
        [FreeFunction("ProfilerBindings::profiler_is_area_enabled")]
        public extern static bool GetAreaEnabled(ProfilerArea area);

        // Displays the recorded profiledata in the profiler.
        [Conditional("UNITY_EDITOR")]
        public static void AddFramesFromFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                Debug.LogError("AddFramesFromFile: Invalid or empty path");
                return;
            }

            AddFramesFromFile_Internal(file, true);
        }
        
        [NativeMethod(Name = "ProfilerBindings::SetScreenshotCaptureFrameInterval", IsFreeFunction = true)]
        public extern static void SetScreenshotCaptureFrameInterval(int frames);

        [NativeHeader("Modules/ProfilerEditor/Public/ProfilerSession.h")]
        [NativeConditional("ENABLE_PROFILER && UNITY_EDITOR")]
        [NativeMethod(Name = "LoadFromFile")]
        [StaticAccessor("profiling::GetProfilerSessionPtr()", StaticAccessorType.Arrow)]
        private extern static void AddFramesFromFile_Internal(string file, bool keepExistingFrames);

        [Conditional("ENABLE_PROFILER")]
        public static void BeginThreadProfiling(string threadGroupName, string threadName)
        {
            if (string.IsNullOrEmpty(threadGroupName))
                throw new ArgumentException("Argument should be a valid string", "threadGroupName");
            if (string.IsNullOrEmpty(threadName))
                throw new ArgumentException("Argument should be a valid string", "threadName");

            BeginThreadProfilingInternal(threadGroupName, threadName);
        }

        [NativeConditional("ENABLE_PROFILER")]
        [NativeMethod(Name = "ProfilerBindings::BeginThreadProfiling", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static void BeginThreadProfilingInternal(string threadGroupName, string threadName);

        [NativeConditional("ENABLE_PROFILER")]
        public static void EndThreadProfiling() {}

        // Begin profiling a piece of code with a custom label.
        // TODO: make obsolete
        //OBSOLETE warning Profiler.BeginSample method is deprecated. Please use faster CustomSampler.Begin method instead.
        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public static void BeginSample(string name)
        {
            ValidateArguments(name);
            BeginSampleImpl(name, null);
        }

        // Begin profiling a piece of code with a custom label.
        // TODO: make obsolete
        //OBSOLETE warning Profiler.BeginSample method is deprecated. Please use faster CustomSampler.Begin method instead.
        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public static void BeginSample(string name, Object targetObject)
        {
            ValidateArguments(name);
            BeginSampleImpl(name, targetObject);
        }

        [MethodImpl(256)]
        static void ValidateArguments(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Argument should be a valid string.", "name");
            }
        }

        [NativeMethod(Name = "ProfilerBindings::BeginSample", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static void BeginSampleImpl(string name, Object targetObject);

        // End profiling a piece of code with a custom label.
        // TODO: make obsolete
        //OBSOLETE warning Profiler.EndSample method is deprecated. Please use faster CustomSampler.End method instead.
        [Conditional("ENABLE_PROFILER")]
        [NativeMethod(Name = "ProfilerBindings::EndSample", IsFreeFunction = true, IsThreadSafe = true)]
        public extern static void EndSample();

        [Obsolete("maxNumberOfSamplesPerFrame has been depricated. Use maxUsedMemory instead")]
        public static int maxNumberOfSamplesPerFrame
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        [Obsolete("usedHeapSize has been deprecated since it is limited to 4GB. Please use usedHeapSizeLong instead.")]
        public static uint usedHeapSize
        {
            get { return (uint)usedHeapSizeLong; }
        }

        // Heap size used by the program
        public extern static long usedHeapSizeLong
        {
            [NativeMethod(Name = "GetUsedHeapSize", IsFreeFunction = true)]
            get;
        }

        // Returns the runtime memory usage of the resource.

        [Obsolete("GetRuntimeMemorySize has been deprecated since it is limited to 2GB. Please use GetRuntimeMemorySizeLong() instead.")]
        public static int GetRuntimeMemorySize(Object o)
        {
            return (int)GetRuntimeMemorySizeLong(o);
        }

        [NativeMethod(Name = "ProfilerBindings::GetRuntimeMemorySizeLong", IsFreeFunction = true)]
        public extern static long GetRuntimeMemorySizeLong([NotNull] Object o);

        [Obsolete("GetMonoHeapSize has been deprecated since it is limited to 4GB. Please use GetMonoHeapSizeLong() instead.")]
        public static uint GetMonoHeapSize()
        {
            return (uint)GetMonoHeapSizeLong();
        }

        // Returns the size of the mono heap
        [NativeMethod(Name = "scripting_gc_get_heap_size", IsFreeFunction = true)]
        public extern static long GetMonoHeapSizeLong();

        [Obsolete("GetMonoUsedSize has been deprecated since it is limited to 4GB. Please use GetMonoUsedSizeLong() instead.")]
        public static uint GetMonoUsedSize()
        {
            return (uint)GetMonoUsedSizeLong();
        }

        // Returns the used size from mono
        [NativeMethod(Name = "scripting_gc_get_used_size", IsFreeFunction = true)]
        public extern static long GetMonoUsedSizeLong();

        // Sets the size of the MainThread's StackAllocator which is used for temp allocs
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static bool SetTempAllocatorRequestedSize(uint size);

        // Gets the size of the MainThread's StackAllocator which is used for temp allocs
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static uint GetTempAllocatorSize();

        [Obsolete("GetTotalAllocatedMemory has been deprecated since it is limited to 4GB. Please use GetTotalAllocatedMemoryLong() instead.")]
        public static uint GetTotalAllocatedMemory()
        {
            return (uint)GetTotalAllocatedMemoryLong();
        }

        [NativeMethod(Name = "GetTotalAllocatedMemory")]
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static long GetTotalAllocatedMemoryLong();

        [Obsolete("GetTotalUnusedReservedMemory has been deprecated since it is limited to 4GB. Please use GetTotalUnusedReservedMemoryLong() instead.")]
        public static uint GetTotalUnusedReservedMemory()
        {
            return (uint)GetTotalUnusedReservedMemoryLong();
        }

        [NativeMethod(Name = "GetTotalUnusedReservedMemory")]
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static long GetTotalUnusedReservedMemoryLong();

        [Obsolete("GetTotalReservedMemory has been deprecated since it is limited to 4GB. Please use GetTotalReservedMemoryLong() instead.")]
        public static uint GetTotalReservedMemory()
        {
            return (uint)GetTotalReservedMemoryLong();
        }

        [NativeMethod(Name = "GetTotalReservedMemory")]
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static long GetTotalReservedMemoryLong();

        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public static unsafe long GetTotalFragmentationInfo(NativeArray<int> stats)
        {
            return InternalGetTotalFragmentationInfo((IntPtr)stats.GetUnsafePtr(), stats.Length);
        }

        [NativeMethod(Name = "GetTotalFragmentationInfo")]
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        private extern static long InternalGetTotalFragmentationInfo(IntPtr pStats, int count);

        [NativeMethod(Name = "GetRegisteredGFXDriverMemory", IsThreadSafe = true)]
        [StaticAccessor("MemoryProfiler", StaticAccessorType.DoubleColon)]
        [NativeConditional("ENABLE_PROFILER")]
        public extern static long GetAllocatedMemoryForGraphicsDriver();


        [Conditional("ENABLE_PROFILER")]
        public static unsafe void EmitFrameMetaData(Guid id, int tag, Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var elementType = data.GetType().GetElementType();
            if (!UnsafeUtility.IsBlittable(elementType))
                throw new ArgumentException(string.Format("{0} type must be blittable", elementType));

            var elemSize = UnsafeUtility.SizeOf(elementType);
            int dataLen = data.Length; 
            Internal_EmitGlobalMetaData_Span(&id, 16, tag, UnsafeUtility.GetByteSpanFromArray(data, dataLen, elemSize), dataLen, elemSize, true);
        }

        [Conditional("ENABLE_PROFILER")]
        public static unsafe void EmitFrameMetaData<T>(Guid id, int tag, List<T> data) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var elementType = typeof(T);
            if (!UnsafeUtility.IsBlittable(typeof(T)))
                throw new ArgumentException(string.Format("{0} type must be blittable", elementType));

            Internal_EmitGlobalMetaData_Span(&id, 16, tag, UnsafeUtility.GetByteSpanFromList(data), data.Count, UnsafeUtility.SizeOf(elementType), true);
        }

        [Conditional("ENABLE_PROFILER")]
        public static unsafe void EmitFrameMetaData<T>(Guid id, int tag, Unity.Collections.NativeArray<T> data) where T : struct
        {
            Internal_EmitGlobalMetaData_Native(&id, 16, tag, (IntPtr)data.GetUnsafeReadOnlyPtr(), data.Length, UnsafeUtility.SizeOf<T>(), true);
        }

        // Non-[Conditional] entry point for module code: module assemblies are compiled without
        // ENABLE_PROFILER defined, which would silently strip every call to EmitFrameMetaData. The
        // native binding is still gated on ENABLE_PROFILER via [NativeConditional], so this is a
        // no-op in non-profiler native builds.
        [VisibleToOtherModules("UnityEngine.U2DRuntimeModule")]
        internal static unsafe void EmitFrameMetaDataInternal<T>(Guid id, int tag, Unity.Collections.NativeArray<T> data) where T : struct
        {
            Internal_EmitGlobalMetaData_Native(&id, 16, tag, (IntPtr)data.GetUnsafeReadOnlyPtr(), data.Length, UnsafeUtility.SizeOf<T>(), true);
        }

        [Conditional("ENABLE_PROFILER")]
        public static unsafe void EmitSessionMetaData(Guid id, int tag, Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var elementType = data.GetType().GetElementType();
            if (!UnsafeUtility.IsBlittable(elementType))
                throw new ArgumentException(string.Format("{0} type must be blittable", elementType));

            var elemSize = UnsafeUtility.SizeOf(elementType);
            int dataLen = data.Length;
            Internal_EmitGlobalMetaData_Span(&id, 16, tag, UnsafeUtility.GetByteSpanFromArray(data, dataLen, elemSize), dataLen, elemSize, false);
        }

        [Conditional("ENABLE_PROFILER")]
        public static unsafe void EmitSessionMetaData<T>(Guid id, int tag, List<T> data) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var elementType = typeof(T);
            if (!UnsafeUtility.IsBlittable(typeof(T)))
                throw new ArgumentException(string.Format("{0} type must be blittable", elementType));

            Internal_EmitGlobalMetaData_Span(&id, 16, tag, UnsafeUtility.GetByteSpanFromList(data), data.Count, UnsafeUtility.SizeOf(elementType), false);
        }

        [Conditional("ENABLE_PROFILER")]
        public static unsafe void EmitSessionMetaData<T>(Guid id, int tag, Unity.Collections.NativeArray<T> data) where T : struct
        {
            Internal_EmitGlobalMetaData_Native(&id, 16, tag, (IntPtr)data.GetUnsafeReadOnlyPtr(), data.Length, UnsafeUtility.SizeOf<T>(), false);
        }

        [NativeMethod(Name = "ProfilerBindings::Internal_EmitGlobalMetaData_Span", IsFreeFunction = true, IsThreadSafe = true)]
        [NativeConditional("ENABLE_PROFILER")]
        static extern unsafe void Internal_EmitGlobalMetaData_Span(void* id, int idLen, int tag, Span<byte> data, int count, int elementSize, bool frameData);

        [NativeMethod(Name = "ProfilerBindings::Internal_EmitGlobalMetaData_Native", IsFreeFunction = true, IsThreadSafe = true)]
        [NativeConditional("ENABLE_PROFILER")]
        static extern unsafe void Internal_EmitGlobalMetaData_Native(void* id, int idLen, int tag, IntPtr data, int count, int elementSize, bool frameData);

        [Conditional("ENABLE_PROFILER")]
        public static void SetCategoryEnabled(ProfilerCategory category, bool enabled)
        {
            if (category == ProfilerCategory.Any)
                throw new ArgumentException("Argument should be a valid category", "category");

            Internal_SetCategoryEnabled((ushort)category, enabled);
        }

        public static bool IsCategoryEnabled(ProfilerCategory category)
        {
            if (category == ProfilerCategory.Any)
                throw new ArgumentException("Argument should be a valid category", "category");

            return Internal_IsCategoryEnabled((ushort)category);
        }

        [NativeHeader("Runtime/Profiler/ProfilerManager.h")]
        [NativeMethod(Name = "GetCategoriesCount")]
        [StaticAccessor("profiling::GetProfilerManagerPtr()", StaticAccessorType.Arrow)]
        [NativeConditional("ENABLE_PROFILER")]
        public static extern uint GetCategoriesCount();

        [Conditional("ENABLE_PROFILER")]
        public static void GetAllCategories(ProfilerCategory[] categories)
        {
            for (int i = 0; i < Math.Min(GetCategoriesCount(), categories.Length); i++)
                categories[i] = new ProfilerCategory((ushort)i);
        }

        [Conditional("ENABLE_PROFILER")]
        public static void GetAllCategories(NativeArray<ProfilerCategory> categories)
        {
            for (int i = 0; i < Math.Min(GetCategoriesCount(), categories.Length); i++)
                categories[i] = new ProfilerCategory((ushort)i);
        }

        [NativeMethod(Name = "profiler_set_category_enable", IsFreeFunction = true, IsThreadSafe = true)]
        [NativeConditional("ENABLE_PROFILER")]
        private extern static void Internal_SetCategoryEnabled(ushort categoryId, bool enabled);

        [NativeMethod(Name = "profiler_is_category_enabled", IsFreeFunction = true, IsThreadSafe = true)]
        [NativeConditional("ENABLE_PROFILER")]
        private extern static bool Internal_IsCategoryEnabled(ushort categoryId);
    }
}
