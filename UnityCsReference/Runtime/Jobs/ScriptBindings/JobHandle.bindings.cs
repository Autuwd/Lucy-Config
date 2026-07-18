// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// =================================================================================================
// 🎯 JobHandle — 作业调度句柄与依赖管理
// =================================================================================================
//
// JobHandle 是整个 C# Job System 的"未来"（future/promise）原语：
//   ✅ 表示一个已调度但可能尚未完成的作业
//   ✅ 通过 CombineDependencies 构建任意 DAG 依赖图
//   ✅ 调用 Complete() 确保作业完成并触发已批处理作业
//
// 💡 调度与完成流程
//   1. IJob.Schedule() 返回 JobHandle → 作业进入"批处理队列"
//   2. JobHandle.ScheduleBatchedJobs() 一次性将所有已批处理作业刷入原生系统
//   3. JobHandle.Complete() 等待指定作业及其所有依赖完成
//   4. CompleteAll() 批量等待多个作业完成（stackalloc 优化，无 GC 分配）
//
// ⚡ 依赖链串联
//   - jobA.Schedule(jobB) → jobA 等待 jobB 完成后执行
//   - CombineDependencies(h1, h2) → 返回依赖 h1 和 h2 的联合句柄
//   - 底层通过 jobGroup（ulong）和 version 字段追踪依赖关系
//
// 📌 性能关键细节
//   - ScheduleBatchedJobs 批量提交减少原生调用开销
//   - CompleteAll 使用 stackalloc 避免堆分配
//   - CheckFenceIsDependencyOrDidSyncFence 验证栅栏依赖完整性
// =================================================================================================

using System;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Jobs
{
    [NativeHeader("ManagedKernel/Jobs/ScriptBindings/JobsBindings.h")]
    public struct JobHandle : IEquatable<JobHandle>
    {
        internal ulong jobGroup;
        internal int   version; // maps to isManual internally. Remove in 2023

        internal int    debugVersion;
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr debugInfo;

        public void Complete()
        {
            if (jobGroup == 0)
                return;

            ScheduleBatchedJobsAndComplete(ref this);
        }

        unsafe public static void CompleteAll(ref JobHandle job0, ref JobHandle job1)
        {
            JobHandle* jobs = stackalloc JobHandle[2];
            jobs[0] = job0;
            jobs[1] = job1;
            ScheduleBatchedJobsAndCompleteAll(jobs, 2);

            job0 = new JobHandle();
            job1 = new JobHandle();
        }

        unsafe public static void CompleteAll(ref JobHandle job0, ref JobHandle job1, ref JobHandle job2)
        {
            JobHandle* jobs = stackalloc JobHandle[3];
            jobs[0] = job0;
            jobs[1] = job1;
            jobs[2] = job2;
            ScheduleBatchedJobsAndCompleteAll(jobs, 3);

            job0 = new JobHandle();
            job1 = new JobHandle();
            job2 = new JobHandle();
        }

        public unsafe static void CompleteAll(NativeArray<JobHandle> jobs)
        {
            ScheduleBatchedJobsAndCompleteAll(jobs.GetUnsafeReadOnlyPtr(), jobs.Length);
        }

        public bool IsCompleted { get { return ScheduleBatchedJobsAndIsCompleted(ref this); } }

        [NativeMethod("ScheduleBatchedScriptingJobs", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern  void ScheduleBatchedJobs();

        [NativeMethod("ScheduleBatchedScriptingJobsAndComplete", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern void      ScheduleBatchedJobsAndComplete(ref JobHandle job);

        [NativeMethod("ScheduleBatchedScriptingJobsAndIsCompleted", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern bool      ScheduleBatchedJobsAndIsCompleted(ref JobHandle job);

        [NativeMethod("ScheduleBatchedScriptingJobsAndCompleteAll", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void ScheduleBatchedJobsAndCompleteAll(void* jobs, int count);


        public static JobHandle CombineDependencies(JobHandle job0, JobHandle job1)
        {
            return CombineDependenciesInternal2(ref job0, ref job1);
        }

        public static JobHandle CombineDependencies(JobHandle job0, JobHandle job1, JobHandle job2)
        {
            return CombineDependenciesInternal3(ref job0, ref job1, ref job2);
        }

        unsafe public static JobHandle CombineDependencies(NativeArray<JobHandle> jobs)
        {
            return CombineDependenciesInternalPtr(jobs.GetUnsafeReadOnlyPtr(), jobs.Length);
        }

        unsafe public static JobHandle CombineDependencies(NativeSlice<JobHandle> jobs)
        {
            return CombineDependenciesInternalPtr(jobs.GetUnsafeReadOnlyPtr(), jobs.Length);
        }

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern JobHandle CombineDependenciesInternal2(ref JobHandle job0, ref JobHandle job1);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern JobHandle CombineDependenciesInternal3(ref JobHandle job0, ref JobHandle job1, ref JobHandle job2);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        internal static extern unsafe JobHandle CombineDependenciesInternalPtr(void* jobs, int count);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
        public static extern bool CheckFenceIsDependencyOrDidSyncFence(JobHandle jobHandle, JobHandle dependsOn);

        public bool Equals(JobHandle other)
        {
            return jobGroup == other.jobGroup;
        }

        public override bool Equals(Object obj)
        {
            return obj is JobHandle && this == (JobHandle)obj;
        }

        public static bool operator ==(JobHandle a, JobHandle b)
        {
            return a.jobGroup == b.jobGroup;
        }

        public static bool operator !=(JobHandle a, JobHandle b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return jobGroup.GetHashCode();
        }
    }
}

namespace Unity.Jobs.LowLevel.Unsafe
{
    public static class JobHandleUnsafeUtility
    {
        unsafe public static JobHandle CombineDependencies(JobHandle* jobs, int count)
        {
            return JobHandle.CombineDependenciesInternalPtr(jobs, count);
        }
    }
}

