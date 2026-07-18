// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 AABBUtility — 轴对齐包围盒工具
//
// 📌 作用：
//   提供 GameObject 层级包围盒（AABB）的静态计算方法。
//   不依赖于物理引擎，直接通过几何数据计算包围盒。
//
// 💡 两个主要方法：
//   - CalculateLocalAABBFromGameObject：计算单个物体的本地 AABB
//   - CalculateCombinedAABBFromHierarchy：计算包含子物体的合并 AABB
//
// ⚡ 内部类，用于编辑器工具和批处理场景。
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Geometry/AABBUtility.h")]
    internal class AABBUtility
    {
        [StaticAccessor("", StaticAccessorType.DoubleColon)]
        extern public static bool CalculateLocalAABBFromGameObject(GameObject go, ref Bounds bounds);

        [StaticAccessor("", StaticAccessorType.DoubleColon)]
        extern public static bool CalculateCombinedAABBFromHierarchy(GameObject go, ref Bounds bounds, bool includeRoot = false);
    }
}
