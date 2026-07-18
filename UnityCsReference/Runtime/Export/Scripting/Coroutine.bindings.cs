// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    //=============================================================================
    // 🎯 Coroutine —— 协程句柄
    //
    // 设计说明:
    //   Coroutine 是 StartCoroutine() 返回的引用句柄，用于追踪和控制协程生命周期。
    //   它不包含协程的执行体，只是 C++ 侧 Coroutine 对象在 C# 侧的 IntPtr 包装。
    //
    // 💡 Yield Instruction 内部机制:
    //   协程的核心是 yield 指令（YieldInstruction）系统:
    //   1. MoveNext() 返回 true → 遇到 yield return，挂起等待
    //   2. 引擎在每帧更新时检查所有活跃的 yield 条件:
    //      - null / 0        → 下一帧继续
    //      - WaitForSeconds  → 定时器触发后继续
    //      - WaitForEndOfFrame → 帧结束渲染后继续
    //      - WaitForFixedUpdate → 固定时间步后继续
    //      - AsyncOperation  → 异步操作完成时继续
    //      - CustomYieldInstruction → keepWaiting 返回 false 时继续
    //   3. 条件满足时，MoveNext() 被重新调用执行到下一个 yield
    //   4. 迭代器结束或 yield break → 协程自然终止
    //
    // ⚠️ 注意:
    //   Coroutine 的终结器（~Coroutine）会调用 ReleaseCoroutine 释放 C++ 侧的协程对象。
    //   如果 MonoBehaviour 被销毁，其启动的所有协程也会自动停止。
    //   Awaitable（UniTask 等）是协程的零 GC 替代方案。
    //=============================================================================

    // MonoBehaviour.StartCoroutine returns a Coroutine. Instances of this class are only used to reference these coroutines and do not hold any exposed properties or functions.
    [NativeHeader("Runtime/Mono/Coroutine.h")]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public sealed class Coroutine : YieldInstruction
    {
        internal IntPtr m_Ptr;
        Coroutine() {}

        ~Coroutine()
        {
            ReleaseCoroutine(m_Ptr);
        }

        [FreeFunction("Coroutine::CleanupCoroutineGC", true)]
        extern static void ReleaseCoroutine(IntPtr ptr);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(Coroutine coroutine) => coroutine.m_Ptr;
        }
    }
}
