// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// 🎯 IntegratedSubsystem —— 集成子系统的抽象基类
//     作为 AR/VR 等原生子系统的托管端抽象
// 💡 ISubsystem 接口：Start / Stop / Destroy 生命周期
// 💡 IntegratedSubsystem<TSubsystemDescriptor> 泛型变体
//     通过 subsystemDescriptor 属性获取描述符
// 💡 valid：检查 m_Ptr 非空
// 💡 running：valid && IsRunning()
// ⚡ Destroy 通过 SubsystemManager.RemoveIntegratedSubsystemByPtr
//     先清理托管引用，再销毁原生对象
// 📌 BindingsMarshaller 提供 Ptr 转换
// ============================================================
using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Subsystems/Subsystem.h")]
    public class IntegratedSubsystem : ISubsystem
    {
        [VisibleToOtherModules("UnityEngine.XRModule")]
        internal IntPtr m_Ptr;

        internal ISubsystemDescriptor m_SubsystemDescriptor;

        extern internal void SetHandle([UnityMarshalAs(NativeType.ScriptingObjectPtr)] IntegratedSubsystem subsystem);
        extern public void Start();
        extern public void Stop();
        public void Destroy()
        {
            IntPtr removedPtr = m_Ptr;
            SubsystemManager.RemoveIntegratedSubsystemByPtr(m_Ptr);
            SubsystemBindings.DestroySubsystem(removedPtr);
            m_Ptr = IntPtr.Zero;
        }

        public bool running => valid && IsRunning();

        internal bool valid => m_Ptr != IntPtr.Zero;

        extern internal bool IsRunning();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(IntegratedSubsystem integratedSubsystem) => integratedSubsystem.m_Ptr;
        }
    }

    [UsedByNativeCode("Subsystem_TSubsystemDescriptor")]
    public partial class IntegratedSubsystem<TSubsystemDescriptor> : IntegratedSubsystem
        where TSubsystemDescriptor : ISubsystemDescriptor
    {
        public TSubsystemDescriptor subsystemDescriptor => (TSubsystemDescriptor)m_SubsystemDescriptor;
    }

    internal static class SubsystemBindings
    {
        internal static extern void DestroySubsystem(IntPtr nativePtr);
    }
}
