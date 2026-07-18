// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// 🎯 SubsystemDescriptorStore —— 子系统描述符存储器
//     管理 IntegratedSubsystemDescriptor 的注册与清理
// 💡 InitializeManagedDescriptor：注册描述符到托管列表
// 💡 ClearManagedDescriptors：清理所有描述符（Domain Reload 时）
// ⚡ [RequiredByNativeCode] 标记，由原生代码回调
// 📌 static partial class，实现分散在多个文件中
// ============================================================
using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.SubsystemsImplementation
{
    [NativeHeader("Modules/Subsystems/SubsystemManager.h")]
    public static partial class SubsystemDescriptorStore
    {
        [RequiredByNativeCode]
        internal static void InitializeManagedDescriptor(IntPtr ptr, IntegratedSubsystemDescriptor desc)
        {
            desc.m_Ptr = ptr;
            s_IntegratedDescriptors.Add(desc);
        }

        [RequiredByNativeCode]
        internal static void ClearManagedDescriptors()
        {
            foreach (var descriptor in s_IntegratedDescriptors)
                descriptor.m_Ptr = IntPtr.Zero;

            s_IntegratedDescriptors.Clear();
        }

        static extern void ReportSingleSubsystemAnalytics(string id);
    }
}
