// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 LocalKeyword — 局部着色器关键字（单个 Shader/Material 生效）
//
// 📌 作用：
//   局部关键字仅影响特定 Shader 或 ComputeShader 上的关键字状态。
//   通过 Material.EnableKeyword / DisableKeyword 控制。
//
// 📌 与 GlobalKeyword 的区别：
//   GlobalKeyword → 全局生效，影响所有材质
//   LocalKeyword  → 单个 Shader/Material 级别，更精细的控制
//
// 📌 shader_feature vs multi_compile：
//   shader_feature：未使用的变体在构建时会被剥离（减少体积）
//   multi_compile：所有变体都会被保留（适合运行时切换）
//   对应关键字用 LocalKeyword + Shader Variant Collection 管理。
//
// 💡 LocalKeyword 包含 SpaceInfo（所属 Shader 的关键字空间）
//   和 Index（在该空间中的位置），用于快速查找和比较。
// ==============================================================

using System;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h")]
    public readonly struct LocalKeyword : IEquatable<LocalKeyword>
    {
        [FreeFunction("keywords::IsKeywordDynamic")] extern private static bool IsDynamic(LocalKeyword kw);
        [FreeFunction("keywords::IsKeywordOverridable")] extern private static bool IsOverridable(LocalKeyword kw);
        [FreeFunction("ShaderScripting::GetKeywordCount")] extern private static uint GetShaderKeywordCount(Shader shader);
        [FreeFunction("ShaderScripting::GetKeywordIndex")] extern private static uint GetShaderKeywordIndex(Shader shader, string keyword);
        [FreeFunction("ShaderScripting::GetKeywordCount")] extern private static uint GetComputeShaderKeywordCount(ComputeShader shader);
        [FreeFunction("ShaderScripting::GetKeywordIndex")] extern private static uint GetComputeShaderKeywordIndex(ComputeShader shader, string keyword);
        [FreeFunction("ShaderScripting::GetKeywordCount")] extern private static uint GetRayTracingShaderKeywordCount(RayTracingShader shader);
        [FreeFunction("ShaderScripting::GetKeywordIndex")] extern private static uint GetRayTracingShaderKeywordIndex(RayTracingShader shader, string keyword);
        [FreeFunction("keywords::GetKeywordType")] extern private static ShaderKeywordType GetKeywordType(LocalKeywordSpace spaceInfo, uint keyword);
        [FreeFunction("keywords::IsKeywordValid")] extern private static bool IsValid(LocalKeywordSpace spaceInfo, uint keyword);

        public string name { get { return m_Name; } }
        public bool isDynamic { get { return IsDynamic(this); } }
        public bool isOverridable { get { return IsOverridable(this); } }
        public bool isValid { get { return IsValid(m_SpaceInfo, m_Index); } }
        public ShaderKeywordType type { get { return GetKeywordType(m_SpaceInfo, m_Index); } }

        public LocalKeyword(Shader shader, string name)
        {
            if (shader == null)
                Debug.LogError("Cannot initialize a LocalKeyword with a null Shader.");
            m_SpaceInfo = shader.keywordSpace;
            m_Name = name;
            m_Index = GetShaderKeywordIndex(shader, name);
            if (m_Index >= GetShaderKeywordCount(shader))
                Debug.LogErrorFormat("Local keyword {0} doesn't exist in the shader.", name);
        }

        public LocalKeyword(ComputeShader shader, string name)
        {
            if (shader == null)
                Debug.LogError("Cannot initialize a LocalKeyword with a null ComputeShader.");
            m_SpaceInfo = shader.keywordSpace;
            m_Name = name;
            m_Index = GetComputeShaderKeywordIndex(shader, name);
            if (m_Index >= GetComputeShaderKeywordCount(shader))
                Debug.LogErrorFormat("Local keyword {0} doesn't exist in the compute shader.", name);
        }

        public LocalKeyword(RayTracingShader shader, string name)
        {
            if (shader == null)
                Debug.LogError("Cannot initialize a LocalKeyword with a null RayTracingShader.");
            m_SpaceInfo = shader.keywordSpace;
            m_Name = name;
            m_Index = GetRayTracingShaderKeywordIndex(shader, name);
            if (m_Index >= GetRayTracingShaderKeywordCount(shader))
                Debug.LogErrorFormat("Local keyword {0} doesn't exist in the ray tracing shader.", name);
        }

        public override string ToString() { return m_Name; }

        public override bool Equals(object o)
        {
            return o is LocalKeyword other && this.Equals(other);
        }

        public bool Equals(LocalKeyword rhs)
        {
            return m_SpaceInfo == rhs.m_SpaceInfo && m_Index == rhs.m_Index;
        }

        public static bool operator==(LocalKeyword lhs, LocalKeyword rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(LocalKeyword lhs, LocalKeyword rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return m_Index.GetHashCode() ^ m_SpaceInfo.GetHashCode();
        }

        internal readonly LocalKeywordSpace m_SpaceInfo;
        internal readonly string m_Name;
        internal readonly uint m_Index;
    }
}
