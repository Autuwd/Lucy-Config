// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 LineUtility — 线条简化工具
//
// 📌 作用：
//   使用 Ramer-Douglas-Peucker 算法简化线段点集
//
// 💡 核心方法：
//   - GeneratePointsToKeep: 返回应保留的点的索引列表
//   - GenerateSimplifiedPoints: 直接返回简化后的点列表
//
// 🎯 tolerance 参数控制简化程度：
//   值越大 → 简化越多（点更少），值越小 → 越接近原始曲线
//
// 💡 支持 2D (Vector2) 和 3D (Vector3) 点集简化
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Graphics/LineUtility.bindings.h")]
    public sealed partial class LineUtility
    {
        [FreeFunction("LineUtility_Bindings::GeneratePointsToKeep3D", IsThreadSafe = true)]
        extern internal static void GeneratePointsToKeep3D(ReadOnlySpan<Vector3> points, float tolerance, List<int> pointsToKeepList);

        [FreeFunction("LineUtility_Bindings::GeneratePointsToKeep2D", IsThreadSafe = true)]
        extern internal static void GeneratePointsToKeep2D(ReadOnlySpan<Vector2> points, float tolerance, List<int> pointsToKeepList);

        [FreeFunction("LineUtility_Bindings::GenerateSimplifiedPoints3D", IsThreadSafe = true)]
        extern internal static void GenerateSimplifiedPoints3D(ReadOnlySpan<Vector3> points, float tolerance, List<Vector3> simplifiedPoints);

        [FreeFunction("LineUtility_Bindings::GenerateSimplifiedPoints2D", IsThreadSafe = true)]
        extern internal static void GenerateSimplifiedPoints2D(ReadOnlySpan<Vector2> points, float tolerance, List<Vector2> simplifiedPoints);
    }
}
