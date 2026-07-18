// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 AsyncOperation — 异步操作基类（协程驱动）
//
// 📌 作用：
//   SceneManager.Load、AssetBundle.Load、Resources.Load 等异步操作的基类
//
// 💡 异步完成模式：
//   - isDone: 操作是否完成
//   - progress: [0, 1] 进度值
//   - allowSceneActivation: 场景加载中，是否允许激活新场景
//     （设为 false 时 progress 卡在 0.9，准备好后设为 true）
//   - priority: 异步操作的执行优先级
//
// 🎯 继承自 YieldInstruction，可用 yield return 等待
// ⚠️ 自定义类型继承时需通过 IntPtr 构造 + BindingsMarshaller
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/Export/Scripting/AsyncOperation.bindings.h")]
    [NativeHeader("Runtime/Misc/AsyncOperation.h")]
    public partial class AsyncOperation : YieldInstruction
    {
        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor("AsyncOperationBindings", StaticAccessorType.DoubleColon)]
        private static extern void InternalDestroy(IntPtr ptr);

        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor("AsyncOperationBindings", StaticAccessorType.DoubleColon)]
        private static extern void InternalSetManagedObject(IntPtr ptr, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] AsyncOperation self);

        public AsyncOperation() {}

        protected AsyncOperation(IntPtr ptr)
        {
            if(ptr == IntPtr.Zero)
                return;

            InternalSetManagedObject(ptr, this);
            m_Ptr = ptr; 
        }

        public extern bool isDone
        {
            [NativeMethod("IsDone")]
            get;
        }

        public extern float progress
        {
            [NativeMethod("GetProgress")]
            get;
        }

        public extern int priority
        {
            [NativeMethod("GetPriority")]
            get;
            [NativeMethod("SetPriority")]
            set;
        }

        public extern bool allowSceneActivation
        {
            [NativeMethod("GetAllowSceneActivation")]
            get;
            [NativeMethod("SetAllowSceneActivation")]
            set;
        }

        internal static class BindingsMarshaller
        {
            public static AsyncOperation ConvertToManaged(IntPtr ptr) => new AsyncOperation(ptr);
            public static IntPtr ConvertToNative(AsyncOperation asyncOperation) => asyncOperation.m_Ptr;
        }
    }
}
