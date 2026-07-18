// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    //=============================================================================
    // 📌 ScriptingRuntime —— 脚本运行时查询
    //
    // 设计说明:
    //   提供对 Unity 脚本运行时（Mono/IL2CPP）的元信息查询。
    //   当前只有一个公开方法 GetAllUserAssemblies()。
    //
    // 🎯 GetAllUserAssemblies():
    //   返回项目中所有用户程序集的文件名数组（不含系统/引擎程序集）。
    //   用于反射扫描和代码生成工具场景。
    //=============================================================================

    [NativeHeader("Runtime/Export/Scripting/ScriptingRuntime.h")]
    [VisibleToOtherModules]
    internal partial class ScriptingRuntime
    {
        public static extern string[] GetAllUserAssemblies();
    }
}
