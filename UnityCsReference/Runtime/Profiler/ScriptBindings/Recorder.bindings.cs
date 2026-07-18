// ====================================================================================================
// 🎯 Recorder —— 传统 Profiler 采样记录器（class，GC 分配）
//
// 【功能定位】
//   旧式 Profiler 记录器，封装 ProfilerRecorder 提供更友好的类 API。
//   新代码建议直接使用 ProfilerRecorder（struct，零分配），Recorder 仅为兼容旧 API 保留。
//
// 【内部实现】
//   💡 内部持有两个 ProfilerRecorder（CPU + GPU），由 ProfilerRecorderHandle 创建。
//   💡 如果 MarkerFlags 不包含 SampleGPU，m_RecorderGPU 保持无效状态（handle == 0）。
//   💡 s_RecorderDefaultOptions = SharedRecorder | WrapAround | SumAllSamples | StartImmediately
//
// 【核心字段】
//   📌 elapsedNanoseconds  —— CPU 端上次采样耗时（纳秒）
//   📌 gpuElapsedNanoseconds —— GPU 端上次采样耗时（纳秒），仅标记了 SampleGPU 时有效
//   📌 sampleBlockCount     —— CPU 采样命中次数
//   📌 gpuSampleBlockCount  —— GPU 采样命中次数
//
// 【设计要点】
//   ⚠️ 引用类型 class，有 GC 分配；使用 Recorder.Get(name) 查找已有标记。
//   ⚠️ 终结器 ~Recorder() 中调用 Dispose()，但应显式管理生命周期。
//   ⚠️ s_InvalidRecorder 是单例标记，[NoAutoStaticsCleanup] 可在重载后复用。
//   📌 Recorder → ProfilerRecorderHandle → ProfilerRecorder（内部两层的间接调用链）
// ====================================================================================================
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Profiling
{
    [UsedByNativeCode]
    public sealed class Recorder
    {
        const ProfilerRecorderOptions s_RecorderDefaultOptions =
            ProfilerRecorder.SharedRecorder |
            ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
            ProfilerRecorderOptions.SumAllSamplesInFrame |
            ProfilerRecorderOptions.StartImmediately;
        [NoAutoStaticsCleanup] // s_InvalidRecorder is a marker value for invalid recorders. It can be reused across domain reloads, and does not reference user code
        static internal Recorder s_InvalidRecorder = new Recorder();

        ProfilerRecorder m_RecorderCPU;
        ProfilerRecorder m_RecorderGPU;

        // This class can't be explicitly created
        internal Recorder()
        {
        }

        internal Recorder(ProfilerRecorderHandle handle)
        {
            if (!handle.Valid)
                return;

            m_RecorderCPU = new ProfilerRecorder(handle, 1, s_RecorderDefaultOptions);

            var description = ProfilerRecorderHandle.GetDescription(handle);
            if ((description.Flags & MarkerFlags.SampleGPU) != 0)
                m_RecorderGPU = new ProfilerRecorder(handle, 1, s_RecorderDefaultOptions | ProfilerRecorderOptions.GpuRecorder);
        }

        ~Recorder()
        {
            m_RecorderCPU.Dispose();
            m_RecorderGPU.Dispose();
        }

        public static Recorder Get(string samplerName)
        {
            var handler = ProfilerRecorderHandle.Get(ProfilerCategory.Any, samplerName);
            if (!handler.Valid)
                return s_InvalidRecorder;
            return new Recorder(handler);
        }

        public bool isValid
        {
            get { return m_RecorderCPU.handle != 0; }
        }

        public bool enabled
        {
            get { return m_RecorderCPU.IsRunning; }
            set { SetEnabled(value); }
        }

        public long elapsedNanoseconds
        {
            get
            {
                if (!m_RecorderCPU.Valid)
                    return 0;
                return m_RecorderCPU.LastValue;
            }
        }

        public long gpuElapsedNanoseconds
        {
            get
            {
                if (!m_RecorderGPU.Valid)
                    return 0;
                return m_RecorderGPU.LastValue;
            }
        }

        public int sampleBlockCount
        {
            get
            {
                if (!m_RecorderCPU.Valid)
                    return 0;
                if (m_RecorderCPU.Count != 1)
                    return 0;
                return (int)m_RecorderCPU.GetSample(0).Count;
            }
        }

        public int gpuSampleBlockCount
        {
            get
            {
                if (!m_RecorderGPU.Valid)
                    return 0;
                if (m_RecorderGPU.Count != 1)
                    return 0;
                return (int)m_RecorderGPU.GetSample(0).Count;
            }
        }

        public void FilterToCurrentThread()
        {
            if (!m_RecorderCPU.Valid)
                return;
            m_RecorderCPU.FilterToCurrentThread();
        }

        public void CollectFromAllThreads()
        {
            if (!m_RecorderCPU.Valid)
                return;
            m_RecorderCPU.CollectFromAllThreads();
        }

        private void SetEnabled(bool state)
        {
            if (state)
            {
                m_RecorderCPU.Start();
                if (m_RecorderGPU.Valid)
                    m_RecorderGPU.Start();
            }
            else
            {
                m_RecorderCPU.Stop();
                if (m_RecorderGPU.Valid)
                    m_RecorderGPU.Stop();
            }
        }
    }
}
