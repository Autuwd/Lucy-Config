// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GlobalKeyword — 全局着色器关键字（跨所有 Shader 生效）
//
// 📌 作用：
//   全局关键字影响当前场景中所有使用该关键字的 Shader。
//   通过 Shader.EnableKeyword / DisableKeyword 控制。
//
// 💡 应用场景：
//   - 全局渲染功能开关（如 _FOG_ON、_SHADOWS_SOFT）
//   - 平台特性适配（如 _DIRECTIONAL_PCF3）
//   - 品质等级切换（如 _HDR_ON）
//
// ⚠️ 注意事项：
//   全局关键字会触发所有受影响材质的 Shader 变体切换，
//   频繁开关可能导致性能开销。
// ==============================================================

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h")]
    public readonly struct GlobalKeyword
    {
        [FreeFunction("ShaderScripting::GetGlobalKeywordCount")] extern private static uint GetGlobalKeywordCount();
        [FreeFunction("ShaderScripting::GetGlobalKeywordIndex")] extern private static uint GetGlobalKeywordIndex(string keyword);
        [FreeFunction("ShaderScripting::CreateGlobalKeyword")] extern private static void CreateGlobalKeyword(string keyword);
        [FreeFunction("ShaderScripting::GetGlobalKeywordName")] extern private static string GetGlobalKeywordName(uint keywordIndex);

        public static GlobalKeyword Create(string name)
        {
            CreateGlobalKeyword(name);
            return new GlobalKeyword(name);
        }

        public GlobalKeyword(string name)
        {
            m_Index = GetGlobalKeywordIndex(name);
            if (m_Index >= GetGlobalKeywordCount())
                Debug.LogErrorFormat("Global keyword {0} doesn't exist.", name);
        }

        public string name => GetGlobalKeywordName(m_Index);

        public override string ToString() => name;

        internal readonly uint m_Index;
    }
}
