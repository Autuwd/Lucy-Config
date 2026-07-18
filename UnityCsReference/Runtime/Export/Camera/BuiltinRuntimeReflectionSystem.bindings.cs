// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    //=============================================================================
    // 🎯 BuiltinRuntimeReflectionSystem —— 内置运行时反射探针系统
    //
    // 设计说明:
    //   实现了 IScriptableRuntimeReflectionSystem 接口，作为 Unity 内置的
    //   默认实时反射探针更新系统。当用户未设置自定义反射系统时使用此实现。
    //
    // 💡 TickRealtimeProbes():
    //   每帧由引擎调用，通过 BuiltinUpdate() 更新所有标记为"实时"的反射探针。
    //   返回 true 表示系统仍在运行，返回 false 表示系统已停止（触发切换）。
    //
    // 📌 Internal_BuiltinRuntimeReflectionSystem_New:
    //   标记 [RequiredByNativeCode]，由 C++ 侧在初始化时调用创建实例。
    //   Unity 在 Domain Reload 后自动将此类设置为默认系统。
    //=============================================================================

    [NativeHeader("Runtime/Camera/ReflectionProbes.h")]
    class BuiltinRuntimeReflectionSystem : IScriptableRuntimeReflectionSystem
    {
        public bool TickRealtimeProbes()
        {
            return BuiltinUpdate();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
        }

        [StaticAccessor("GetReflectionProbes()", Type = StaticAccessorType.Dot)]
        static extern bool BuiltinUpdate();

        [RequiredByNativeCode]
        static BuiltinRuntimeReflectionSystem Internal_BuiltinRuntimeReflectionSystem_New()
        {
            return new BuiltinRuntimeReflectionSystem();
        }
    }
}
