// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PhysicsSpace — 空间分区加速结构（Broad-Phase）
//
// 📌 作用：
//   PhysicsSpace 是空间分区（Spatial Partition）组件，
//   使用 BVH（层次包围盒树）加速碰撞检测的粗相位阶段。
//   将场景中的物理代理（Proxy）按 AABB 组织在树中，
//   快速筛选出可能碰撞的对象对。
//
// 🏗 架构位置：World → Space（可选的加速结构）
//   PhysicsSpace 的代理与 PhysicsShape 关联，
//   Overlap/Cast 查询通过 Space 快速定位候选对象
//
// 💡 关键能力：
//   - Create/Destroy Proxy：在空间中注册/注销碰撞代理
//   - SetProxyAABB/Categories：更新代理的包围盒和类别
//   - OverlapAABB：AABB 重叠查询
//   - CastRay：空间射线检测
//   - CastShape：空间形状投射
//   - Clone()：将空间克隆到物理世界
//   - GetRootAABB：获取整个空间的根包围盒
//
// 📌 Proxy 构成：
//   - AABB：轴对齐包围盒
//   - Categories：物理类别掩码（用于过滤）
//   - UserHandle：用户自定义句柄
// ==============================================================

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Bindings;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    [NativeHeader("Modules/PhysicsCore2D/Core/PhysicsSpace2D.h")]
    [StaticAccessor("PhysicsSpace2D", StaticAccessorType.DoubleColon)]
    static class PhysicsSpaceScripting2D
    {
        [NativeMethod(Name = "Create", IsThreadSafe = true)] extern internal static PhysicsSpace PhysicsSpace_Create();
        [NativeMethod(Name = "Destroy", IsThreadSafe = true)] extern internal static bool PhysicsSpace_Destroy(PhysicsSpace space);
        [NativeMethod(Name = "DestroyAll", IsThreadSafe = true)] extern internal static void PhysicsSpace_DestroyAll();
        [NativeMethod(Name = "IsValid", IsThreadSafe = true)] extern internal static bool Space_IsValid(PhysicsSpace space);
        [NativeMethod(Name = "GetSpaces", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_GetSpaces(Allocator allocator);
        [NativeMethod(Name = "Clone", IsThreadSafe = true)] extern internal static int PhysicsSpace_Clone(PhysicsSpace space, PhysicsWorld world, PhysicsQuery.QueryFilter filter, PhysicsAABB aabb, bool useAABB, bool destroyExistingProxies);
        [NativeMethod(Name = "CreateProxy", IsThreadSafe = true)] extern internal static PhysicsSpace.ProxyHandle PhysicsSpace_CreateProxy(PhysicsSpace space, PhysicsAABB aabb, PhysicsMask categories, PhysicsHandle userHandle);
        [NativeMethod(Name = "DestroyProxy", IsThreadSafe = true)] extern internal static bool PhysicsSpace_DestroyProxy(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle);
        [NativeMethod(Name = "ClearProxies", IsThreadSafe = true)] extern internal static bool PhysicsSpace_ClearProxies(PhysicsSpace space);
        [NativeMethod(Name = "GetProxyCount", IsThreadSafe = true)] extern internal static int PhysicsSpace_GetProxyCount(PhysicsSpace space);
        [NativeMethod(Name = "GetMemoryAllocated", IsThreadSafe = true)] extern internal static int PhysicsSpace_GetMemoryAllocated(PhysicsSpace space);
        [NativeMethod(Name = "GetRootAABB", IsThreadSafe = true)] extern internal static PhysicsAABB PhysicsSpace_GetRootAABB(PhysicsSpace space);
        [NativeMethod(Name = "SetProxyAABB", IsThreadSafe = true)] extern internal static void PhysicsSpace_SetProxyAABB(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle, PhysicsAABB aabb, bool updateAncestors);
        [NativeMethod(Name = "GetProxyAABB", IsThreadSafe = true)] extern internal static PhysicsAABB PhysicsSpace_GetProxyAABB(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle);
        [NativeMethod(Name = "SetProxyCategories", IsThreadSafe = true)] extern internal static void PhysicsSpace_SetProxyCategories(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle, PhysicsMask categories);
        [NativeMethod(Name = "GetProxyCategories", IsThreadSafe = true)] extern internal static PhysicsMask PhysicsSpace_GetProxyCategories(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle);
        [NativeMethod(Name = "SetProxyUserHandle", IsThreadSafe = true)] extern internal static void PhysicsSpace_SetProxyUserHandle(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle, PhysicsHandle userHandle);
        [NativeMethod(Name = "GetProxyUserHandle", IsThreadSafe = true)] extern internal static PhysicsHandle PhysicsSpace_GetProxyUserHandle(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle);
        [NativeMethod(Name = "OverlapAABB", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_OverlapAABB(PhysicsSpace space, PhysicsAABB aabb, PhysicsMask categories, Allocator allocator);
        [NativeMethod(Name = "CastRay", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_CastRay(PhysicsSpace space, PhysicsQuery.CastRayInput input, PhysicsMask categories, Allocator allocator);
        [NativeMethod(Name = "CastShape", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_CastShape(PhysicsSpace space, PhysicsQuery.CastShapeInput input, PhysicsMask categories, Allocator allocator);
    }
}
