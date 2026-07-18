// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.SceneManagement
{
    //=============================================================================
    // 🎯 SceneUtility —— 场景工具类
    //
    // 设计说明:
    //   SceneUtility 提供了场景名称与 Build Settings 索引之间的双向映射查找。
    //   不涉及场景加载/卸载，仅做路径 ↔ 构建索引的转换，是纯查询工具。
    //
    // 💡 核心方法:
    //   GetScenePathByBuildIndex:  从 Build Settings 索引 → 场景文件路径
    //   GetBuildIndexByScenePath:  从场景文件路径 → Build Settings 索引
    //
    // ⚠️ 注意:
    //   场景路径必须与 Build Settings 中注册的路径完全匹配（含 Assets/ 前缀）。
    //   Build Settings 中未注册的场景无法通过此 API 查找。
    //=============================================================================

    [NativeHeader("Runtime/Export/SceneManager/SceneUtility.bindings.h")]
    public static partial class SceneUtility
    {
        [StaticAccessor("SceneUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static string GetScenePathByBuildIndex(int buildIndex);

        [StaticAccessor("SceneUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static int GetBuildIndexByScenePath(string scenePath);
    }
}
