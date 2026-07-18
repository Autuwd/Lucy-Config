// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 NavMeshBuilder — 导航网格构建器
//
// 📌 作用：
//   提供 NavMesh 数据的构建（Baking）API。
//   支持同步（BuildNavMeshData）和异步（UpdateNavMeshDataAsync）方式。
//
// 🔑 核心流程：
//   1. CollectSources(): 从场景中收集几何数据
//      - 可以按 Bounds（世界包围盒）或 Transform（层级根节点）收集
//      - 支持 RenderMeshes 和 PhysicsColliders 两种几何来源
//      - 通过 markups 精细控制每个物体的烘焙参数
//   2. BuildNavMeshData(): 使用收集的数据构建 NavMeshData
//   3. NavMesh.AddNavMeshData(): 将数据加载到寻路系统
//
// 💡 异步更新：
//   UpdateNavMeshDataAsync() 返回 AsyncOperation，
//   不会阻塞主线程，适合运行时动态更新 NavMesh。
//
// ⚡ CollectSources 参数：
//   - includedLayerMask: 只收集指定层的物体
//   - geometry: RenderMeshes（渲染网格）或 PhysicsColliders（碰撞体）
//   - defaultArea: 未标记物体默认所属区域
//   - markups: 覆盖特定物体的烘焙参数
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.AI
{
    // ==============================================================
    // 🎯 NavMeshBuilder — 静态构建工具类
    //
    // 📌 API 概览：
    //   - CollectSources():          从场景收集 NavMesh 构建源数据
    //   - BuildNavMeshData():        同步构建 NavMeshData
    //   - UpdateNavMeshData():       同步更新已有 NavMeshData
    //   - UpdateNavMeshDataAsync():  异步更新（不阻塞主线程）
    //   - Cancel():                  取消正在进行的异步构建
    //
    // 💡 与 NavMesh.Bake 的区别：
    //   NavMeshBuilder 适合运行时动态构建，
    //   编辑器中的 NavMesh Baking 本质也是调用这套 API。
    // ==============================================================
    [NativeHeader("Modules/AI/Builder/NavMeshBuilder.bindings.h")]
    [StaticAccessor("NavMeshBuilderBindings", StaticAccessorType.DoubleColon)]
    public static class NavMeshBuilder
    {
        public static void CollectSources(
            Bounds includedWorldBounds, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, bool generateLinksByDefault,
            List<NavMeshBuildMarkup> markups, bool includeOnlyMarkedObjects, List<NavMeshBuildSource> results)
        {
            if (markups == null)
                throw new ArgumentNullException(nameof(markups));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            // Ensure strictly positive extents
            includedWorldBounds.extents = Vector3.Max(includedWorldBounds.extents, 0.001f * Vector3.one);
            var resultsArray = CollectSourcesInternal(
                includedLayerMask, includedWorldBounds, null, true, geometry, defaultArea, generateLinksByDefault,
                markups.ToArray(), includeOnlyMarkedObjects);
            results.Clear();
            results.AddRange(resultsArray);
        }

        public static void CollectSources(
            Bounds includedWorldBounds, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea,
            List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
        {
            CollectSources(includedWorldBounds, includedLayerMask, geometry, defaultArea, false, markups, false, results);
        }

        public static void CollectSources(
            Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, bool generateLinksByDefault,
            List<NavMeshBuildMarkup> markups, bool includeOnlyMarkedObjects, List<NavMeshBuildSource> results)
        {
            if (markups == null)
                throw new ArgumentNullException(nameof(markups));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            // root == null is a valid argument

            var empty = new Bounds();
            var resultsArray = CollectSourcesInternal(
                includedLayerMask, empty, root, false, geometry, defaultArea, generateLinksByDefault,
                markups.ToArray(), includeOnlyMarkedObjects);
            results.Clear();
            results.AddRange(resultsArray);
        }

        public static void CollectSources(
            Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea,
            List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
        {
            CollectSources(root, includedLayerMask, geometry, defaultArea, false, markups, false, results);
        }

        static extern NavMeshBuildSource[] CollectSourcesInternal(
            int includedLayerMask, Bounds includedWorldBounds, Transform root, bool useBounds,
            NavMeshCollectGeometry geometry, int defaultArea, bool generateLinksByDefault,
            NavMeshBuildMarkup[] markups, bool includeOnlyMarkedObjects);

        // Immediate NavMeshData building
        public static NavMeshData BuildNavMeshData(
            NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources,
            Bounds localBounds, Vector3 position, Quaternion rotation)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var data = new NavMeshData(buildSettings.agentTypeID)
            {
                position = position,
                rotation = rotation
            };

            UpdateNavMeshDataListInternal(data, buildSettings, NoAllocHelpers.CreateReadOnlySpan(sources), localBounds);
            return data;
        }

        // Immediate NavMeshData updating
        public static bool UpdateNavMeshData(
            NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            return UpdateNavMeshDataListInternal(data, buildSettings, NoAllocHelpers.CreateReadOnlySpan(sources), localBounds);
        }

        static extern bool UpdateNavMeshDataListInternal(
            NavMeshData data, NavMeshBuildSettings buildSettings, ReadOnlySpan<NavMeshBuildSource> sources, Bounds localBounds);

        // Async NavMeshData updating
        public static AsyncOperation UpdateNavMeshDataAsync(
            NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            return UpdateNavMeshDataAsyncListInternal(data, buildSettings, NoAllocHelpers.CreateReadOnlySpan(sources), localBounds);
        }

        [NativeHeader("Modules/AI/NavMeshManager.h")]
        [StaticAccessor("GetNavMeshManager().GetNavMeshBuildManager()", StaticAccessorType.Arrow)]
        [NativeMethod("Purge")]
        public static extern void Cancel(NavMeshData data);

        static extern AsyncOperation UpdateNavMeshDataAsyncListInternal(
            NavMeshData data, NavMeshBuildSettings buildSettings, ReadOnlySpan<NavMeshBuildSource> sources, Bounds localBounds);
    }
}
