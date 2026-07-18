// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 BakeProgressState — 烘焙进度状态
//
// 📌 作用：
//   跟踪光照贴图烘焙（Lightmap Baking）的进度状态。
//   在 Unity 的轻量级传输（LightTransport）管线中使用。
//
// 💡 核心功能：
//   - IsCompleted：是否完成
//   - IsCanceled：是否被取消
//   - CancelRequested：是否请求取消（线程安全）
//   - Progress：当前进度（0~1）
//   - OverallProgress：总体进度（多阶段烘焙用）
//   - Cancel/CancelAndWait：取消操作
//   - Reset：重置状态
//   - UpdateProgress/UpdateOverallProgress：更新进度（线程安全）
//
// ⚡ 内部类，属于 UnityEngine.LightTransport 命名空间。
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.LightTransport
{
    [NativeHeader("Runtime/Export/GI/BakeProgressState.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class BakeProgressState : IDisposable
    {
        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr Internal_Create();

        [NativeMethod(IsThreadSafe = true)]
        static extern void Internal_Destroy(IntPtr ptr);

        internal IntPtr m_Ptr;
        internal bool m_OwnsPtr;

        public BakeProgressState()
        {
            m_Ptr = Internal_Create();
            m_OwnsPtr = true;
        }
        private BakeProgressState(IntPtr ptr)
        {
            m_Ptr = ptr;
            m_OwnsPtr = false;
        }
        ~BakeProgressState()
        {
            Destroy();
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        void Destroy()
        {
            if (m_OwnsPtr && m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(BakeProgressState obj) => obj.m_Ptr;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern void Cancel();

        [NativeMethod(IsThreadSafe = true)]
        public extern float Progress();

        [NativeMethod(IsThreadSafe = true)]
        public extern void SetTotalWorkSteps(UInt64 total);

        [NativeMethod(IsThreadSafe = true)]
        public extern void IncrementCompletedWorkSteps(UInt64 steps);

        [NativeMethod(IsThreadSafe = true)]
        public extern bool WasCancelled();
    }
}
