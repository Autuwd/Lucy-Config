// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    //=============================================================================
    // 🎯 ScriptableRuntimeReflectionSystemSettings —— 可编程运行时反射系统设置
    //
    // 设计说明:
    //   允许用户替换 Unity 内置的实时反射探针更新系统（BuiltinRuntimeReflectionSystem）
    //   为自定义实现，提供更灵活的反射探针更新策略（如按需更新、LOD 更新等）。
    //
    // 💡 system 属性:
    //   设置时自动释放旧的系统实例，仅允许一个活跃系统。
    //   如果新系统与 BuiltinRuntimeReflectionSystem 不同且已有非内置系统，
    //   会发出警告提示多次分配。
    //
    // 📌 默认行为:
    //   Domain Reload 后自动设置为 BuiltinRuntimeReflectionSystem。
    //   Addressables 等资源管理系统可以通过此接口接管反射探针更新。
    //=============================================================================

    [RequiredByNativeCode]
    [NativeHeader("Runtime/Camera/ScriptableRuntimeReflectionSystem.h")]
    public static class ScriptableRuntimeReflectionSystemSettings
    {
        public static IScriptableRuntimeReflectionSystem system
        {
            get { return Internal_ScriptableRuntimeReflectionSystemSettings_system; }
            set
            {
                if (value == null || value.Equals(null))
                {
                    Debug.LogError("'null' cannot be assigned to ScriptableRuntimeReflectionSystemSettings.system");
                    return;
                }
                // We always allow the BuiltinRuntimeReflectionSystem, it is set by Unity on domain reload
                // However, we issue a warning when multiple different IScriptableRuntimeReflectionSystem have been assigned.
                else if (!(system is BuiltinRuntimeReflectionSystem)
                         && !(value is BuiltinRuntimeReflectionSystem)
                         && system != value
                )
                    Debug.LogWarningFormat("ScriptableRuntimeReflectionSystemSettings.system is assigned more than once. Only a the last instance will be used. (Last instance {0}, New instance {1})", system, value);

                Internal_ScriptableRuntimeReflectionSystemSettings_system = value;
            }
        }

        static IScriptableRuntimeReflectionSystem Internal_ScriptableRuntimeReflectionSystemSettings_system
        {
            get { return s_Instance.implementation; }
            [RequiredByNativeCode]
            set
            {
                if (s_Instance.implementation != value)
                {
                    if (s_Instance.implementation != null)
                        s_Instance.implementation.Dispose();
                }
                s_Instance.implementation = value;
            }
        }

        static ScriptableRuntimeReflectionSystemWrapper s_Instance = new ScriptableRuntimeReflectionSystemWrapper();

        static ScriptableRuntimeReflectionSystemWrapper Internal_ScriptableRuntimeReflectionSystemSettings_instance
        {
            [RequiredByNativeCode]
            get { return s_Instance; }
        }

#pragma warning disable RS0030 // This [RuntimeInitializeOnLoadMethod] is in the CoreModule and has existed for a very long time.  It also doesn't have much in the way of dependencies since it's an extern.  Not worth the effort to remove this one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        [StaticAccessor("ScriptableRuntimeReflectionSystem", StaticAccessorType.DoubleColon)]
        static extern void ScriptingDirtyReflectionSystemInstance();
#pragma warning restore RS0030
    }
}
