// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.ExceptionServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    //=============================================================================
    // 🎯 Awaitable —— 零分配异步操作（C++ 回调节点到 C# async/await）
    //
    // 设计说明:
    //   Awaitable 是 Unity 实现的零 GC 分配异步原语，允许 C++ 侧的回调事件
    //   （如 AsyncOperation 完成、渲染事件、动画回调等）直接被 C# 的
    //   async/await 模式消费，无需额外分配 Task 或 Coroutine 包装。
    //
    // 💡 工作流程:
    //   1. C++ 侧创建原生 Awaitable 对象，持有对应的 GCHandle 指向 C# 实例
    //   2. C# 侧 await 该 Awaitable 时，_continuation 被设置为编译器生成的续行动作
    //   3. C++ 回调完成时调用 RunContinuation():
    //      - 在 _spinLock 保护下取出 _continuation
    //      - 执行续行，恢复 async 方法的执行
    //   4. 异常通过 SetExceptionFromNative() 传递到 C# 侧的 ExceptionDispatchInfo
    //
    // 📌 相比 Coroutine 的优势:
    //   - 零 GC 分配（Coroutine 每次 yield 都会产生装箱）
    //   - 可被 async/await 直接消费（无需 StartCoroutine）
    //   - 与 UniTask 等 ValueTask 方案兼容
    //   - C++ 侧直接回调，无 Mono 协程调度开销
    //=============================================================================

    [NativeHeader("Runtime/Mono/Awaitable.h")]
    public partial class Awaitable
    {

        [RequiredByNativeCode(GenerateProxy = true)]
        private void SetExceptionFromNative(Exception ex)
        {
            bool lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                _exceptionToRethrow = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                if (lockTaken)
                {
                    _spinLock.Exit();
                }
            }
        }


        [RequiredByNativeCode(GenerateProxy = true)]
        private void RunContinuation()
        {
            Action continuation = null;
            bool lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                continuation = _continuation;
                _continuation = null;
            }
            finally
            {
                if (lockTaken)
                {
                    _spinLock.Exit();
                }
            }
            continuation?.Invoke();
        }


        [FreeFunction("Scripting::Awaitables::AttachManagedWrapper", IsThreadSafe = true)]
        private static extern void AttachManagedGCHandleToNativeAwaitable(IntPtr nativeAwaitable, UIntPtr gcHandle);

        [FreeFunction("Scripting::Awaitables::Release", IsThreadSafe = true)]
        private static extern void ReleaseNativeAwaitable(IntPtr nativeAwaitable);

        [FreeFunction("Scripting::Awaitables::Cancel", IsThreadSafe = true)]
        private static extern void CancelNativeAwaitable(IntPtr nativeAwaitable);

        [FreeFunction("Scripting::Awaitables::IsCompleted", IsThreadSafe = true)]
        private static extern int IsNativeAwaitableCompleted(IntPtr nativeAwaitable);
    }
}
