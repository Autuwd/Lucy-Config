// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 NavLocation — 导航位置（位置 + 节点对）
//
// 📌 作用：
//   将世界坐标（Vector3）映射到 NavMesh 上的具体节点（NavNode）。
//   是低级寻路 API 的基本输入/输出单位。
//
// 🔑 构成：
//   - position: 世界坐标
//   - node:     对应的 NavMesh 多边形节点引用
//
// 💡 通过 NavWorld.MapLocation() 将世界坐标映射为 NavLocation。
//   NavLocation 是低级 API（BeginFindPath/Raycast）的起点和终点。
//   IEquatable 实现支持基于 node + position 的相等比较。
// ==============================================================

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.AI.Navigation.LowLevel;

// ==============================================================
// 🎯 NavLocation — 位置 + 节点引用
//
// 📌 MapLocation 将世界坐标映射到 NavMesh 上最近的节点。
//   所有低级寻路查询都基于 NavLocation，
//   而不是直接使用 Vector3。
// ==============================================================
public readonly struct NavLocation : IEquatable<NavLocation>
{
    public NavNode node { get; }
    public Vector3 position { get; }

    internal NavLocation(Vector3 position, NavNode node)
    {
        this.position = position;
        this.node = node;
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavLocation left, NavLocation right)
    {
        return left.node.Equals(right.node) && left.position.Equals(right.position);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavLocation left, NavLocation right)
    {
        return !left.node.Equals(right.node) || !left.position.Equals(right.position);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavLocation other)
    {
        return node.Equals(other.node) && position.Equals(other.position);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavLocation other && node.Equals(other.node) && position.Equals(other.position);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + node.GetHashCode();
            hash = (hash * 31) + position.GetHashCode();
            return hash;
        }
    }
}
