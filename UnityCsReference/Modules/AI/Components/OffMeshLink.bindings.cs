// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 OffMeshLink — 网格外链接（手动版）
//
// 📌 作用：
//   在 NavMesh 的"缺口"处架设连接（跳跃、梯子、传送门）。
//   有两种创建方式：
//   1. OffMeshLink 组件（手动放置，本文件）
//   2. NavMeshLinkData（通过 NavMesh.AddLink 动态创建）
//
// 🔑 OffMeshLinkType（链接类型）：
//   LinkTypeManual:     手动定义（最常见）
//   LinkTypeDropDown:   垂直下落（从高处到低处）
//   LinkTypeJumpAcross: 水平跳跃（跨越间隙）
//
// 💡 双向 vs 单向：
//   OffMeshLink 组件默认是双向的。
//   NavMeshLinkData 的 bidirectional 字段控制：
//     true: 两端都可通行（双向跳跃）
//     false: 仅从 start 到 end（单向传送/掉落）
//
// 📌 OffMeshLinkData 结构：
//   当 Agent 正在/即将穿越 OffMeshLink 时，
//   通过 currentOffMeshLinkData / nextOffMeshLinkData 获取。
//   包含：两端位置、链接类型、所有者对象等。
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // ==============================================================
    // 🎯 OffMeshLinkType — 链接类型
    //
    // 📌 描述 OffMeshLink 的物理表现类型：
    //   LinkTypeManual:     手动定义（编辑器拖拽放置）
    //   LinkTypeDropDown:   垂直下落（悬崖边跳下）
    //   LinkTypeJumpAcross: 水平跨越（跳过裂缝）
    //
    // 💡 类型影响 Agent 穿越时的动画/行为选择。
    // ==============================================================
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    [MovedFrom("UnityEngine")]
    public enum OffMeshLinkType
    {
        // Manually specified type of link.
        LinkTypeManual = 0,

        // Vertical drop.
        LinkTypeDropDown = 1,

        // Horizontal jump.
        LinkTypeJumpAcross = 2
    }

    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    // State of OffMeshLink.
    // ==============================================================
    // 🎯 OffMeshLinkData — Agent 穿越链接时的数据快照
    //
    // 📌 当 Agent 在 OffMeshLink 上时，提供链接的详细信息：
    //   - valid:      链接是否有效
    //   - activated:  链接是否激活
    //   - linkType:   链接类型
    //   - startPos/endPos: 两端世界坐标
    //   - owner:      创建此链接的对象
    //
    // 💡 通过 NavMeshAgent.currentOffMeshLinkData
    //   和 nextOffMeshLinkData 获取。
    // ==============================================================
    [MovedFrom("UnityEngine")]
    [NativeHeader("Modules/AI/Components/OffMeshLink.bindings.h")]
    public partial struct OffMeshLinkData
    {
        internal int m_Valid;
        internal int m_Activated;
        internal EntityId m_InstanceID;
        internal OffMeshLinkType m_LinkType;
        internal Vector3 m_StartPos;
        internal Vector3 m_EndPos;

        // Is link valid (RO).
        public bool valid => m_Valid != 0;

        // Is link active (RO).
        public bool activated => m_Activated != 0;

        // Link type specifier (RO).
        public OffMeshLinkType linkType => m_LinkType;

        // Link start world position (RO).
        public Vector3 startPos => m_StartPos;

        // Link end world position (RO).
        public Vector3 endPos => m_EndPos;

        // The object that created this link instance if the link type is a manually placed [[Offmeshlink]] or [[NavMeshLinkData]] (RO).
        public Object owner => GetLinkOwnerInternal(m_InstanceID);

        [FreeFunction("OffMeshLinkScriptBindings::GetLinkOwnerInternal")]
        static extern Object GetLinkOwnerInternal(EntityId instanceID);
    }
}
