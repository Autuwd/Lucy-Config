// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 NavLowLevelTypes — 低级寻路类型定义
//
// 📌 包含低级 NavMesh 查询中使用的基础枚举：
//
// 🔑 NavQueryStatus（查询状态 — 按位标记）：
//   Success/Failure/InProgress: 高层状态
//   InvalidParameter:           参数无效
//   MoreDataAvailable:          结果缓冲区太小
//   MaxNodesToVisitExceeded:    A* 节点访问上限
//   PartialResult:              未到终点，返回最佳推测
//
// 🔑 NavNodeType（节点类型）：
//   Polygon: 普通地面的多边形节点
//   Link:    OffMeshLink 连接节点
//
// 💡 NavQueryStatus 是 Flags 枚举，
//   可同时返回多个状态（如 Failure | InvalidParameter）。
//   A* 分阶段查询通过 InProgress 追踪进度。
// ==============================================================

using System;

namespace Unity.AI.Navigation.LowLevel;

// ==============================================================
// 🎯 NavQueryStatus — 查询状态（按位标记）
//
// 📌 指示低级 NavMesh 查询的结果状态。
//   A* 分阶段查询（BeginFindPath/ContinueFindPath/EndFindPath）
//   通过 Success/Failure/InProgress 追踪进度。
//
// ⚡ PartialResult 表示目标不可达，
//   但返回了最近的可达路径（类似 NavMeshPathStatus.PathPartial）。
// ==============================================================
// Keep in sync with the values in NavMeshTypes.h
[Flags]
public enum NavQueryStatus
{
    // High level status.
    Failure = 1 << 31,
    Success = 1 << 30,
    InProgress = 1 << 29,

    // Detail information for status.
    StatusDetailMask = 0x0ffffff,
    InvalidParameter = 1 << 3, // An input parameter was invalid.
    MoreDataAvailable = 1 << 4, // Result buffer for the query was too small to store all results.
    MaxNodesToVisitExceeded = 1 << 5, // Query ran out of nodes during search.
    PartialResult = 1 << 6 // Query did not reach the end location, returning best guess.
}

// ==============================================================
// 🎯 NavNodeType — 节点类型
//
// 📌 区分 NavMesh 上的节点是普通地面多边形
//   还是 OffMeshLink 连接点。
//   通过 NavWorld.GetNodeType() 查询。
// ==============================================================
// Flags describing node properties. Keep in sync with the enum declared in NavMesh.h
public enum NavNodeType
{
    Undefined = -1,
    Polygon = 0, // Regular ground polygons.
    Link = 1 // Off-mesh connections.
}
