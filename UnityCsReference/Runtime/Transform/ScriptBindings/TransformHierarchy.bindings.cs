// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Jobs;

namespace UnityEngine
{
    //=============================================================================
    // 📌 TransformHierarchy —— Transform 层级结构原生绑定
    //
    // 设计说明:
    //   TransformHierarchy 是 C# 侧访问原生 Transform 层级树的内部接口。
    //   它通过 P/Invoke 直接调用 C++ TransformHierarchyBindings 上的静态方法，
    //   提供层级遍历（GetParentIndex、GetChildCount、GetChildEntities）和
    //   结构变更（CreateHierarchy、SetParent）能力。
    //
    // 🎯 线程安全说明:
    //   遍历方法标记了 IsThreadSafe = true，因为:
    //   1. 这些函数只读取层级数组（parentIndices、childIndices 等）
    //   2. 结构变更（SetParent、DetachChildren）会在修改层级前等待所有 Job 完成
    //   3. AtomicSafetyHandle 保证并发读写不受冲突
    //
    // ⚠️ 限制:
    //   当前尚不支持在子线程安全地遍历含 GameObject 的层级（DOTS-10269）。
    //   纯 Entity 层级（无 GameObject 包装）可在 Job 中安全遍历。
    //=============================================================================

    internal struct TransformHierarchy
    {
        [FreeFunction("TransformHierarchyBindings::SetUpdateTransformUnionsCallback", HasExplicitThis = false)]
        internal static extern void SetUpdateTransformUnionsCallback(IntPtr callback);

        [FreeFunction("TransformHierarchyBindings::CreateNewHierarchy", HasExplicitThis = false)]
        internal static extern UnsafeTransformAccess CreateHierarchy(IntPtr entityComponentStore,
            Vector3 worldPosition, Quaternion rotation, Vector3 scale, UInt64 entity, uint capacity);

        [FreeFunction("TransformHierarchyBindings::SetParent_Internal_WithoutHierarchy", HasExplicitThis = false)]
        internal static extern UnsafeTransformAccess SetParent_Internal_WithoutHierarchy(IntPtr entityComponentStore,
            UnsafeTransformAccess unsafeTransformAccess,
            Vector3 worldPosition, Quaternion rotation, Vector3 scale, UInt64 childEntity);

        [FreeFunction("TransformHierarchyBindings::SetParent_Internal_WithHierarchy", HasExplicitThis = false)]
        internal static extern UnsafeTransformAccess SetParent_Internal_WithHierarchy(IntPtr entityComponentStore,
            UnsafeTransformAccess parentTransformAccess,
            UnsafeTransformAccess childTransformAccess);

        internal static UnsafeTransformAccess SetParent(IntPtr entityComponentStore,
            UnsafeTransformAccess unsafeTransformAccess,
            Vector3 worldPosition, Quaternion rotation, Vector3 scale, UInt64 childEntity)
        {
            return SetParent_Internal_WithoutHierarchy(entityComponentStore,
                unsafeTransformAccess,
                worldPosition, rotation, scale, childEntity);
        }

        internal static UnsafeTransformAccess SetParent(IntPtr entityComponentStore,
            UnsafeTransformAccess parentTransformAccess,
            UnsafeTransformAccess childTransformAccess)
        {
            return SetParent_Internal_WithHierarchy(entityComponentStore, parentTransformAccess, childTransformAccess);
        }

        // Hierarchy traversal functions for TransformRef
        // These are marked IsThreadSafe=true to allow calling from jobs. Thread safety is ensured by:
        // 1. These functions only read from the hierarchy arrays (parentIndices, childIndices, mainThreadOnlyEntityReferences)
        // 2. Structural changes (SetParent, DetachChildren) complete all pending jobs before modifying the hierarchy
        // 3. The AtomicSafetyHandle on TransformTypeHandle prevents concurrent read/write access to the component data
        // Note: these are _not_ safe to use with hierarchies containing GameObjects off the main thread yet (DOTS-10269).
        [FreeFunction("TransformHierarchyBindings::GetParentIndex", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern int GetParentIndex(UnsafeTransformAccess access);

        [FreeFunction("TransformHierarchyBindings::GetParentEntityReference", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern ulong GetParentEntityReference(UnsafeTransformAccess access);

        [FreeFunction("TransformHierarchyBindings::GetChildCount", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern int GetChildCount(UnsafeTransformAccess access);

        [FreeFunction("TransformHierarchyBindings::GetChildIndex", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern int GetChildIndex(UnsafeTransformAccess access, int childPosition);

        [FreeFunction("TransformHierarchyBindings::GetEntityReferenceAtIndex", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern ulong GetEntityReferenceAtIndex(UnsafeTransformAccess access, int index);

        // Batch function to get all child entities at once (more efficient than iterating)
        [FreeFunction("TransformHierarchyBindings::GetChildEntities", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern unsafe int GetChildEntities(UnsafeTransformAccess access, ulong* outChildEntities, int maxCount);
    }
}
