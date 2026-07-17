// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AnimatorControllerParameter —— 动画控制器参数
//
// 【概述】
// 对应 Animator Controller 中定义的参数（Parameter）。
// 用于在状态机中驱动条件转换（Transition）和混合树（Blend Tree）。
//
// 【参数类型】
// - Float：连续值参数（速度、方向等）
// - Int：整数参数（状态编号等）
// - Bool：布尔参数（是否奔跑、是否死亡等）
// - Trigger：触发器（一次性的，触发后自动重置）
//
// 【注意】
// 此类标记为 [NativeAsStruct]，C# 侧是托管包装，
// 底层数据存储在 C++ 的 AnimatorControllerParameter 中。
// ============================================================

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    /// AnimatorControllerParameter —— 动画控制器中定义的参数。
    /// 用于状态机条件判断和混合树驱动。
    /// </summary>
    [NativeHeader("Modules/Animation/AnimatorControllerParameter.h")]
    [NativeAsStruct]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public class AnimatorControllerParameter
    {
        /// <summary>参数名称。</summary>
        public string name
        {
            get { return m_Name; }
            set {   m_Name = value;     }
        }

        /// <summary>参数名称的哈希值（用于 Animator 高性能查询）。</summary>
        public int nameHash
        {
            get { return Animator.StringToHash(m_Name); }
        }

        /// <summary>参数类型（Float / Int / Bool / Trigger）。</summary>
        public AnimatorControllerParameterType type { get { return m_Type; } set { m_Type = value; } }
        /// <summary>Float 类型参数的默认值。</summary>
        public float defaultFloat { get { return m_DefaultFloat; } set { m_DefaultFloat = value; } }
        /// <summary>Int 类型参数的默认值。</summary>
        public int defaultInt { get { return m_DefaultInt; } set { m_DefaultInt = value; } }
        /// <summary>Bool 类型参数的默认值。</summary>
        public bool defaultBool { get { return m_DefaultBool; } set { m_DefaultBool = value; } }

        internal string m_Name = "";
        internal AnimatorControllerParameterType m_Type;
        internal float m_DefaultFloat;
        internal int m_DefaultInt;
        internal bool m_DefaultBool;

        public override bool Equals(object o)
        {
            AnimatorControllerParameter other = o as AnimatorControllerParameter;
            return other != null && m_Name == other.m_Name && m_Type == other.m_Type && m_DefaultFloat == other.m_DefaultFloat && m_DefaultInt == other.m_DefaultInt && m_DefaultBool == other.m_DefaultBool;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }
}
