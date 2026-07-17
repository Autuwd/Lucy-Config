// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// SortingGroup — Unity 2D 排序组组件
//
// 🎯 核心作用：
//   SortingGroup 用于统一管理其子层级中所有 Renderer 的排序行为。
//   当多个精灵需要作为一个整体进行排序时（比如一个角色由
//   多个 SpriteRenderer 组成），SortingGroup 确保它们的相对
//   排序顺序不会被其他对象打断。
//
// 📌 工作原理：
//   1. SortingGroup 会覆盖子级 Renderer 的独立排序设置
//   2. 所有子级 Renderer 按 SortingGroup 的 sortingLayer 和 sortingOrder 排序
//   3. 子级之间保持相对于 SortingGroup 的排序层级关系
//
// 💡 使用场景：
//   - 角色：身体/武器/特效由多个 SpriteRenderer 组成，
//     需要整体排序不被其他角色穿插
//   - 建筑/载具：多层精灵需要统一前后关系
//   - 粒子系统与精灵混合排序
//
// 🔑 关键属性：
//   - sortingLayerName/sortingLayerID：排序层设置
//   - sortingOrder：排序优先级（数值越大越靠前）
//   - sortAtRoot：是否在根级参与排序（影响层级排序行为）
//   - sort3DAs2D：将 3D 空间中的对象按 2D 方式排序
//
// 🔗 继承链：
//   SortingGroup -> Behaviour -> Component -> Object
//
// 📍 对应 C++ 头文件：Runtime/2D/Sorting/SortingGroup.h
// ==============================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/2D/Sorting/SortingGroup.h")]
    // ==============================================================
    // SortingGroup — 排序组组件
    //
    // 🎯 核心功能：
    //   统一控制子级 Renderer 的排序层和排序顺序，
    //   确保组内对象作为一个整体参与排序。
    //
    // 📌 与 SortingLayer 的关系：
    //   SortingGroup 设置的 sortingLayer/sortingOrder 会覆盖
    //   子级 Renderer 各自的排序设置。子级之间的相对排序
    //   仍然保持，但整体优先级由 SortingGroup 决定。
    //
    // 💡 sortAtRoot 的含义：
    //   - true：SortingGroup 在根级 Transform 处参与排序
    //   - false：SortingGroup 在自身 Transform 处参与排序
    //   这影响了当 SortingGroup 嵌套在其他 Transform 下时的排序行为。
    //
    // ⚡ 内部机制：
    //   - sortingGroupID：引擎内部为每个 SortingGroup 分配的唯一 ID
    //   - sortingKey：排序键值，由 sortingGroupID + sortingOrder 组合
    //   - sort3DAs2D：启用后，3D 空间中 Z 轴距离也参与排序计算
    //   - UpdateAllSortingGroups()：强制刷新所有 SortingGroup 排序
    // ==============================================================
    public sealed partial class SortingGroup : Behaviour
    {
        [StaticAccessor("SortingGroup", StaticAccessorType.DoubleColon)]
        internal extern static int invalidSortingGroupID { get; }

        [StaticAccessor("SortingGroup", StaticAccessorType.DoubleColon)]
        public extern static void UpdateAllSortingGroups();

        [StaticAccessor("SortingGroup", StaticAccessorType.DoubleColon)]
        internal extern static SortingGroup GetSortingGroupByIndex(int index);

        public extern string sortingLayerName { get; set; }
        public extern int sortingLayerID { get; set; }
        public extern int sortingOrder { get; set; }
        public extern bool sortAtRoot { get; set; }
        internal extern int sortingGroupID { get; }
        internal extern int sortingGroupOrder { get; }
        internal extern int index { get; }
        internal extern uint sortingKey { get; }

        internal extern bool sort3DAs2D { get; }
    }

}
