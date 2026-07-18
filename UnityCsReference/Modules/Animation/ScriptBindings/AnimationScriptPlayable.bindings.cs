// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 AnimationScriptPlayable — 自定义动画 Job Playable
//
// 📌 作用：
//   允许用户通过实现 IAnimationJob 接口编写自定义动画逻辑，
//   在 Job System 中并行处理骨骼数据。这是 Unity 动画系统的
//   高级扩展点。
//
// 🔑 核心概念：
//   - IAnimationJob：用户实现的动画 Job 接口
//   - ProcessAnimationJobStruct<T>：Job 的反射数据，用于 C++ 端识别
//   - GetJobData()/SetJobData()：通过 UnsafeUtility 在 C# 和 C++ 之间
//     传递 Job 数据（struct 值类型，零拷贝）
//   - SetProcessInputs()：控制 Job 是否处理输入 Pose
//
// 💡 自定义 Job 流程：
//   1. 定义一个 struct 实现 IAnimationJob（含 ProcessAnimation 方法）
//   2. 调用 AnimationScriptPlayable.Create(graph, jobData) 创建
//   3. Job 在 C++ 端的动画线程中执行 ProcessAnimation()
//   4. 通过 GetJobData/SetJobData 在运行时更新 Job 参数
//
// 📍 对应 C++ 头文件：
//   Modules/Animation/ScriptBindings/AnimationScriptPlayable.bindings.h
// ==============================================================

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Animations
{
    // ==============================================================
    // AnimationScriptPlayable — 自定义动画 Job Playable struct
    //
    // 🎯 功能：
    //   将用户定义的 IAnimationJob 集成到 PlayableGraph 中。
    //   Job 在动画系统的 Native 线程中执行，不阻塞主线程。
    //
    // 🔑 关键 API：
    //   - Create<T>():   创建并传入 Job 数据（值类型 struct）
    //   - GetJobData():  读取当前 Job 数据（用于运行时参数更新）
    //   - SetJobData():  更新 Job 数据（零拷贝，直接写入 Native 内存）
    //   - SetProcessInputs(): 控制是否处理输入 Pose
    //
    // ⚠️ 注意：
    //   - T 必须是值类型 struct（避免 GC 分配）
    //   - Job 类型在创建时确定，后续不能更改
    //   - CheckJobTypeValidity() 在 Get/SetJobData 时做类型检查
    //
    // 📍 对应 C++ 头文件：
    //   Modules/Animation/ScriptBindings/AnimationScriptPlayable.bindings.h
    // ==============================================================
    [MovedFrom("UnityEngine.Experimental.Animations")]
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationScriptPlayable.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationScriptPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationScriptPlayable : IAnimationJobPlayable, IEquatable<AnimationScriptPlayable>
    {
        private PlayableHandle m_Handle;

        static readonly AnimationScriptPlayable m_NullPlayable = new AnimationScriptPlayable(PlayableHandle.Null);
        public static AnimationScriptPlayable Null { get { return m_NullPlayable; } }

        // ==============================================================
        // Create<T> — 创建自定义动画 Job Playable
        //
        // 🎯 步骤：
        //   1. 调用 ProcessAnimationJobStruct<T>.GetJobReflectionData()
        //      获取 Job 类型的反射数据（告知 C++ 端 Job 的布局）
        //   2. C++ 端 CreateHandleInternal 创建原生 Playable
        //   3. SetJobData() 将 Job 数据拷贝到 Native 内存
        //
        // 💡 泛型参数 T 必须实现 IAnimationJob（struct 约束）
        //    这样 Job 数据可以直接拷贝，无需 GC 分配。
        // ==============================================================
        public static AnimationScriptPlayable Create<T>(PlayableGraph graph, T jobData, int inputCount = 0)
            where T : struct, IAnimationJob
        {
            var handle = CreateHandle<T>(graph, inputCount);
            var playable = new AnimationScriptPlayable(handle);
            playable.SetJobData(jobData);
            return playable;
        }

        private static PlayableHandle CreateHandle<T>(PlayableGraph graph, int inputCount)
            where T : struct, IAnimationJob
        {
            IntPtr jobReflectionData = ProcessAnimationJobStruct<T>.GetJobReflectionData();

            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle, jobReflectionData))
                return PlayableHandle.Null;

            handle.SetInputCount(inputCount);

            return handle;
        }

        internal AnimationScriptPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationScriptPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationScriptPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        // ==============================================================
        // CheckJobTypeValidity — 验证 Job 类型一致性
        //
        // ⚠️ 安全检查：确保 Get/SetJobData 时使用的 T 与创建时一致。
        //    Job 类型一旦创建就不能更改，否则会导致内存布局不匹配。
        // ==============================================================
        private void CheckJobTypeValidity<T>()
        {
            var jobType = GetHandle().GetJobType();
            if (jobType != typeof(T))
                throw new ArgumentException(string.Format("Wrong type: the given job type ({0}) is different from the creation job type ({1}).", typeof(T).FullName, jobType.FullName));
        }

        // ==============================================================
        // GetJobData / SetJobData — Job 数据的运行时读写
        //
        // 💡 使用 UnsafeUtility 直接在 Native 内存上操作：
        //   - GetJobData：将 Native 内存中的数据拷贝到 C# struct
        //   - SetJobData：将 C# struct 直接写入 Native 内存
        //
        // ⚡ 零 GC 分配：因为是值类型拷贝，不产生托管对象。
        //    但要注意线程安全——Job 可能正在另一个线程上执行。
        // ==============================================================
        public unsafe T GetJobData<T>()
            where T : struct, IAnimationJob
        {
            CheckJobTypeValidity<T>();

            T data;
            UnsafeUtility.CopyPtrToStructure<T>((void*)GetHandle().GetJobData(), out data);
            return data;
        }

        public unsafe void SetJobData<T>(T jobData)
            where T : struct, IAnimationJob
        {
            CheckJobTypeValidity<T>();

            UnsafeUtility.CopyStructureToPtr(ref jobData, (void*)GetHandle().GetJobData());
        }

        public static implicit operator Playable(AnimationScriptPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationScriptPlayable(Playable playable)
        {
            return new AnimationScriptPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationScriptPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        public void SetProcessInputs(bool value)
        {
            SetProcessInputsInternal(GetHandle(), value);
        }

        public bool GetProcessInputs()
        {
            return GetProcessInputsInternal(GetHandle());
        }

        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle, IntPtr jobReflectionData);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetProcessInputsInternal(PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetProcessInputsInternal(PlayableHandle handle);
    }
}
