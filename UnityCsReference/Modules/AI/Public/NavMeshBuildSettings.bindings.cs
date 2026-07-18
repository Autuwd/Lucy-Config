// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 NavMeshBuildSettings — NavMesh 烘焙参数配置
//
// 📌 控制 NavMesh Baking 的质量和行为。
//   这些参数直接决定 Agent 能走哪里、怎么走。
//
// 🔑 核心参数：
//
// 🧑 Agent 尺寸：
//   - agentRadius:   Agent 半径（决定通道宽度）
//   - agentHeight:   Agent 高度（决定通道高度）
//   - agentSlope:    Agent 可爬最大坡度
//   - agentClimb:    Agent 可跨越最大台阶高度
//
// 📐 体素化参数：
//   - voxelSize:     体素大小（越小越精确，越慢）
//   - overrideVoxelSize: 是否覆盖默认值
//   - tileSize:      瓦片大小（分块构建用）
//
// 🗺 区域参数：
//   - minRegionArea:  最小区域面积（过滤噪声区域）
//   - ledgeDropHeight: 边缘掉落高度
//   - maxJumpAcrossDistance: 最大跨越距离
//
// 🛠 其他：
//   - buildHeightMesh: 是否构建高度网格（精确高度查询）
//   - preserveTilesOutsideBounds: 是否保留边界外瓦片
//   - debug: 调试绘制标志
//
// 💡 ValidationReport() 验证参数有效性。
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.AI
{
    // Keep this struct in sync with the one defined in "NavMeshBuildSettings.h"
    // ==============================================================
    // 🎯 NavMeshBuildSettings — 烘焙设置结构体
    //
    // 📌 由 NavMesh.CreateSettings() 创建，
    //   或从 NavMesh.GetSettingsByIndex/ID 获取已有设置。
    //
    // ⚡ 参数调整影响：
    //   agentRadius 调小 → 窄通道可通行，但 NavMesh 更复杂（性能下降）
    //   voxelSize 调小 → 更精确，但烘焙时间大幅增加
    //   agentClimb 调大 → Agent 能爬更高台阶
    //   agentSlope 调大 → Agent 能爬更陡坡
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/Public/NavMeshBuildSettings.h")]
    public struct NavMeshBuildSettings
    {
        public int agentTypeID { get { return m_AgentTypeID; } set { m_AgentTypeID = value; } }
        public float agentRadius { get { return m_AgentRadius; } set { m_AgentRadius = value; } }
        public float agentHeight { get { return m_AgentHeight; } set { m_AgentHeight = value; } }
        public float agentSlope { get { return m_AgentSlope; } set { m_AgentSlope = value; } }
        public float agentClimb { get { return m_AgentClimb; } set { m_AgentClimb = value; } }
        public float ledgeDropHeight { get { return m_LedgeDropHeight; } set { m_LedgeDropHeight = value; } }
        public float maxJumpAcrossDistance { get { return m_MaxJumpAcrossDistance; } set { m_MaxJumpAcrossDistance = value; } }
        public float minRegionArea { get { return m_MinRegionArea; } set { m_MinRegionArea = value; } }
        public bool overrideVoxelSize { get { return m_OverrideVoxelSize != 0; } set { m_OverrideVoxelSize = value ? 1 : 0; } }
        public float voxelSize { get { return m_VoxelSize; } set { m_VoxelSize = value; } }
        public bool overrideTileSize { get { return m_OverrideTileSize != 0; } set { m_OverrideTileSize = value ? 1 : 0; } }
        public int tileSize { get { return m_TileSize; } set { m_TileSize = value; } }
        public uint maxJobWorkers { get { return m_MaxJobWorkers; } set { m_MaxJobWorkers = value; } }
        public bool preserveTilesOutsideBounds { get { return m_PreserveTilesOutsideBounds != 0; } set { m_PreserveTilesOutsideBounds = value ? 1 : 0; } }
        public bool buildHeightMesh { get { return m_BuildHeightMesh != 0; } set { m_BuildHeightMesh = value ? 1 : 0; } }
        public NavMeshBuildDebugSettings debug { get { return m_Debug; } set { m_Debug = value; } }

        int m_AgentTypeID;
        float m_AgentRadius;
        float m_AgentHeight;
        float m_AgentSlope;
        float m_AgentClimb;
        float m_LedgeDropHeight;
        float m_MaxJumpAcrossDistance;
        float m_MinRegionArea;
        int m_OverrideVoxelSize;
        float m_VoxelSize;
        int m_OverrideTileSize;
        int m_TileSize;
        int m_BuildHeightMesh;
        uint m_MaxJobWorkers;
        int m_PreserveTilesOutsideBounds;

        NavMeshBuildDebugSettings m_Debug;

        public String[] ValidationReport(Bounds buildBounds)
        {
            return InternalValidationReport(this, buildBounds);
        }

        [FreeFunction]
        [NativeHeader("Modules/AI/Public/NavMeshBuildSettings.h")]
        static extern String[] InternalValidationReport(NavMeshBuildSettings buildSettings, Bounds buildBounds);

        // Consider exposing a "Validate" method to modify the BuildSettings in-place
    }

    // ==============================================================
    // 🎯 NavMeshBuildDebugSettings — 烘焙调试设置
    //
    // 📌 控制 NavMesh 构建过程的调试可视化。
    //   flags 按位组合 NavMeshBuildDebugFlags 枚举值。
    //
    // 💡 在 Editor 中通过 Navigation 窗口的 Debug 面板配置。
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/Public/NavMeshBuildDebugSettings.h")]
    public struct NavMeshBuildDebugSettings
    {
        public NavMeshBuildDebugFlags flags { get { return (NavMeshBuildDebugFlags)m_Flags; } set { m_Flags = (byte)value; } }

        byte m_Flags;
    }
}
