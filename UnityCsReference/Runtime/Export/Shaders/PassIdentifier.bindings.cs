// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PassIdentifier — Shader Pass 的精确定位标识
//
// 📌 作用：
//   通过 SubshaderIndex + PassIndex 唯一定位 Shader 中的某个 Pass。
//   用于 ScriptableRenderPass 精确指定要执行的渲染 Pass。
//
// 💡 Shader 结构层级：
//   Shader → SubShader（多个，按 GPU 能力选择）→ Pass（多个）
//   PassIdentifier = (SubshaderIndex, PassIndex)
//
// ⚡ 使用场景：
//   在 DrawRenderers 中通过 PassIdentifier 替代 Pass 名称，
//   避免字符串查找开销，提升 CPU 端性能。
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/PassIdentifier.h")]
    public readonly struct PassIdentifier : IEquatable<PassIdentifier>
    {
        public uint SubshaderIndex { get { return m_SubShaderIndex; } }
        public uint PassIndex { get { return m_PassIndex; } }

        public PassIdentifier(uint subshaderIndex, uint passIndex)
        {
            m_SubShaderIndex = subshaderIndex;
            m_PassIndex = passIndex;
        }

        public override bool Equals(object o)
        {
            return o is PassIdentifier other && this.Equals(other);
        }

        public bool Equals(PassIdentifier rhs)
        {
            return m_SubShaderIndex == rhs.m_SubShaderIndex && m_PassIndex == rhs.m_PassIndex;
        }

        public static bool operator==(PassIdentifier lhs, PassIdentifier rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(PassIdentifier lhs, PassIdentifier rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return m_SubShaderIndex.GetHashCode() ^ m_PassIndex.GetHashCode();
        }

        internal readonly uint m_SubShaderIndex;
        internal readonly uint m_PassIndex;
    }
}
