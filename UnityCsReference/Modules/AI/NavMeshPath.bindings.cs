// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 NavMeshPath — 寻路路径
//
// 📌 作用：
//   存储 A* 寻路计算的结果路径。
//   由一系列拐点（corners）和路径状态（status）组成。
//
// 🔑 路径状态（NavMeshPathStatus）：
//   PathComplete: ✅ 路径完整到达目的地
//   PathPartial:  ⚠️ 无法完全到达，返回最近可达点
//     → 原因：目标点在 NavMesh 外、被不可通行区域隔离等
//   PathInvalid:  ❌ 路径不可用
//     → 原因：起点不在 NavMesh 上、起点终点在同一不可通行区域等
//
// 💡 corners（路径拐点数组）：
//   Agent 实际行走的路径由一系列拐点构成。
//   第一个拐点是起点附近，最后一个是目标点。
//   拐点数量影响寻路质量。
//
// ⚡ GetCornersNonAlloc：
//   预分配数组的无 GC 版本，减少内存分配。
//   适合频繁寻路的高性能场景。
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // ==============================================================
    // 🎯 NavMeshPathStatus — 路径状态枚举
    //
    // 📌 表示寻路结果的完整程度：
    //   PathComplete:  路径完整（到达目标）
    //   PathPartial:   部分路径（最近可达点）
    //   PathInvalid:   无效（不可达）
    //
    // 💡 每次寻路后应检查 pathStatus，
    //   PathPartial 时可以引导 Agent 走到最近点再重新寻路。
    // ==============================================================
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    [MovedFrom("UnityEngine")]
    public enum NavMeshPathStatus
    {
        PathComplete = 0,   // The path terminates at the destination.
        PathPartial = 1,    // The path cannot reach the destination.
        PathInvalid = 2     // The path is invalid.
    }

    // Path navigation.
    // ==============================================================
    // 🎯 NavMeshPath — 路径对象
    //
    // 📌 关键成员：
    //   - corners:     路径拐点数组（Agent 实际行走的路线）
    //   - status:      路径状态（Complete/Partial/Invalid）
    //   - ClearCorners(): 清空路径数据（CalculatePath 前自动调用）
    //   - GetCornersNonAlloc(): 零分配获取拐点
    //
    // 💡 NavMeshPath 不会被引擎自动缓存。
    //   每次调用 CalculatePath 需要传入新路径对象。
    //   BindingsMarshaller 提供 IntPtr 给 C++ 端使用。
    // ==============================================================
    [NativeHeader("Modules/AI/NavMeshPath.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine")]
    public sealed class NavMeshPath
    {
        internal IntPtr m_Ptr;
        internal Vector3[] m_Corners;

        public NavMeshPath()
        {
            m_Ptr = InitializeNavMeshPath();
        }

        ~NavMeshPath()
        {
            DestroyNavMeshPath(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }

        [FreeFunction("NavMeshPathScriptBindings::InitializeNavMeshPath")]
        static extern IntPtr InitializeNavMeshPath();

        [FreeFunction("NavMeshPathScriptBindings::DestroyNavMeshPath", IsThreadSafe = true)]
        static extern void DestroyNavMeshPath(IntPtr ptr);

        [FreeFunction("NavMeshPathScriptBindings::GetCornersNonAlloc", HasExplicitThis = true)]
        public extern int GetCornersNonAlloc([Out] Vector3[] results);

        [FreeFunction("NavMeshPathScriptBindings::CalculateCornersInternal", HasExplicitThis = true)]
        extern Vector3[] CalculateCornersInternal();

        [FreeFunction("NavMeshPathScriptBindings::ClearCornersInternal", HasExplicitThis = true)]
        extern void ClearCornersInternal();

        // Erase all corner points from path.
        public void ClearCorners()
        {
            ClearCornersInternal();
            m_Corners = null;
        }

        void CalculateCorners()
        {
            if (m_Corners == null)
                m_Corners = CalculateCornersInternal();
        }

        // Corner points of path. (RO)
        public Vector3[] corners { get { CalculateCorners(); return m_Corners; } }

        // Status of the path. (RO)
        public extern NavMeshPathStatus status { get; }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(NavMeshPath navMeshPath) => navMeshPath.m_Ptr;
        }
    }
}
