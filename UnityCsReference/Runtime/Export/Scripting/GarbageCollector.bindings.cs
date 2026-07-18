// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Scripting
{
    //=============================================================================
    // 🎯 GarbageCollector —— 垃圾回收器控制
    //
    // 设计说明:
    //   提供对 Unity 增量式垃圾回收器（Incremental GC）的控制能力。
    //   增量式 GC 将一次完整的 GC 停顿分散到多帧执行，减少单帧卡顿。
    //
    // 💡 GCMode（GC 模式）:
    //   Disabled  — 完全禁用自动 GC（需要手动调 Collect）
    //   Enabled   — 启用自动 GC（默认行为）
    //   Manual    — 仅增量式模式，不自动触发完整 GC
    //
    // ⚠️ isIncremental（增量式 GC）:
    //   启用后，GC 在每个时间切片（incrementalTimeSliceNanoseconds）内
    //   执行有限量的回收工作。适合对帧率敏感的游戏。
    //   CollectIncremental() 可手动触发指定时间长度的增量回收。
    //
    // 📌 使用场景:
    //   内存敏感的游戏（如移动端、主机）建议启用增量式 GC，
    //   并在关键帧（如载入界面）手动触发 CollectIncremental 来分散 GC 压力。
    //=============================================================================

    [NativeHeader("Runtime/Scripting/GarbageCollector.h")]
    public static class GarbageCollector
    {
        public enum Mode
        {
            Disabled = 0,
            Enabled = 1,
            Manual =  2,
        }

        public static event Action<Mode> GCModeChanged;

        public static Mode GCMode
        {
            get
            {
                return GetMode();
            }

            set
            {
                if (value == GetMode())
                    return;

                SetMode(value);

                if (GCModeChanged != null)
                    GCModeChanged(value);
            }
        }

        [NativeMethod(ThrowsException = true)]
        extern static void SetMode(Mode mode);
        extern static Mode GetMode();

        public extern static bool isIncremental { [NativeMethod("GetIncrementalEnabled")] get; }

        public extern static ulong incrementalTimeSliceNanoseconds { get; set; }

        [NativeMethod("CollectIncrementalWrapper", ThrowsException = true)]
        public extern static bool CollectIncremental(ulong nanoseconds = 0);
    }
}
