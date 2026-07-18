// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 NavNode — NavMesh 多边形节点引用
//
// 📌 作用：
//   封装对 NavMesh 中一个多边形（Polygon）的引用。
//   底层使用 64 位 polyRef（多边形引用 ID）唯一标识一个节点。
//
// 🔑 polyRef（Polygon Reference）：
//   64 位无符号整数，编码了：
//   - 所属 NavMesh 表面（高位移）
//   - 瓦片索引
//   - 多边形在瓦片内的索引
//
// 💡 NavNode 是低级 API 的核心构建块：
//   - 寻路时路径由 NavNode[] 数组表示
//   - 通过 NavNode.IsNull() 检查是否有效
//   - GetNodeType() 区分多边形节点和链接节点
//   - GetAgentTypeIdForNode() 获取所属 Agent 类型
// ==============================================================

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.AI.Navigation.LowLevel;

// ==============================================================
// 🎯 NavNode — 多边形引用（polyRef 封装）
//
// 📌 64 位 polyRef 是 Detour 库中的标准节点标识方式。
//   IsNull() 检查 polyRef == 0。
// ==============================================================
public struct NavNode : IEquatable<NavNode>
{
    internal ulong m_PolyRef;

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavNode left, NavNode right) { return left.m_PolyRef == right.m_PolyRef; }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavNode left, NavNode right) { return left.m_PolyRef != right.m_PolyRef; }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavNode other) { return m_PolyRef == other.m_PolyRef; }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavNode other && m_PolyRef == other.m_PolyRef;
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode() { return m_PolyRef.GetHashCode(); }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool IsNull() { return m_PolyRef == 0; }
}
