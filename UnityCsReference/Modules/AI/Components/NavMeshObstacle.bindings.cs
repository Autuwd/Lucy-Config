// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 NavMeshObstacle — 导航障碍物组件
//
// 📌 作用：
//   在 NavMesh 上放置动态障碍物，Agent 会自动绕行。
//   支持 Box 和 Capsule 两种形状。
//
// 🔑 Carving vs Moving 策略：
//
// 🪚 Carving（雕刻模式）— carving=true
//   障碍物在 NavMesh 上"挖洞"，改变寻路数据。
//   ✅ Agent 能精确绕过障碍物
//   ❌ 有性能开销（需要重新计算受影响的 NavMesh 区域）
//   💡 carveOnlyStationary=true（推荐）：
//     障碍物移动时禁用 Carving（Agent 用避让绕过），
//     障碍物停下后才挖洞更新 NavMesh。
//     → 结合了精度和性能的最佳实践
//
// 🏃 Moving（移动模式）— carving=false
//   障碍物不改变 NavMesh，Agent 通过避让绕过。
//   ✅ 零 NavMesh 重算开销
//   ❌ 精确度较低，Agent 可能贴边
//   💡 适合移动频繁的障碍物
//
// ⚡ carvingMoveThreshold / carvingTimeToStationary：
//   控制何时认为障碍物"静止"并触发 Carving 更新。
//   太频繁 = 性能开销大，太慢 = Agent 反应迟钝。
// ==============================================================

using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // ==============================================================
    // 🎯 NavMeshObstacleShape — 障碍物形状
    //
    // 📌 决定 Carving 时在 NavMesh 上挖出的孔洞形状：
    //   Capsule: 胶囊体（适合圆柱/角色形状的障碍物）
    //   Box:     立方体（适合方形障碍物）
    // ==============================================================
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    [MovedFrom("UnityEngine")]
    public enum NavMeshObstacleShape
    {
        // Capsule shaped obstacle.
        Capsule = 0,
        // Box shaped obstacle.
        Box = 1,
    }

    // Navigation mesh obstacle.
    // ==============================================================
    // 🎯 NavMeshObstacle — 导航障碍物组件
    //
    // 📌 关键属性：
    //   - carving:              是否启用雕刻模式
    //   - carveOnlyStationary:  仅静止时雕刻（推荐！）
    //   - carvingMoveThreshold: 移动触发阈值
    //   - carvingTimeToStationary: 判定静止的等待时间
    //   - shape/center/size:    形状和碰撞体参数
    //   - velocity:             障碍物自身速度
    //
    // 💡 最佳实践：
    //   carving=true + carveOnlyStationary=true
    //   → 障碍物移动时零性能开销，停下时精确挖洞。
    //   大多数情况下这是最优配置。
    // ==============================================================
    [MovedFrom("UnityEngine")]
    [NativeHeader("Modules/AI/Components/NavMeshObstacle.bindings.h")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshObstacle.html")]
    public sealed class NavMeshObstacle : Behaviour
    {
        // Obstacle height.
        public extern float height { get; set; }

        // Obstacle radius.
        public extern float radius { get; set; }

        // Obstacle velocity.
        public extern Vector3 velocity { get; set; }

        // Enable carving
        public extern bool carving { get; set; }

        // When carving enabled, carve only when obstacle is stationary, moving obstacles are avoided dynamically.
        public extern bool carveOnlyStationary { get; set; }

        // Update carving if moved at least this distance, or if carveWhenStationary if moved at least this distance, the obstacle is considered moving.
        [NativeProperty("MoveThreshold")]
        public extern float carvingMoveThreshold { get; set; }

        // If carveWhenStationary is set, the obstacle is considered stationary if it has not moved during this long period.
        [NativeProperty("TimeToStationary")]
        public extern float carvingTimeToStationary { get; set; }

        // Shape of the obstacle, NavMeshObstacleShape.Box or NavMeshObstacleShape.Capsule.
        public extern NavMeshObstacleShape shape { get; set; }

        public extern Vector3 center { get; set; }

        public extern Vector3 size
        {
            [FreeFunction("NavMeshObstacleScriptBindings::GetSize", HasExplicitThis = true)]
            get;
            [FreeFunction("NavMeshObstacleScriptBindings::SetSize", HasExplicitThis = true)]
            set;
        }

        [VisibleToOtherModules("UnityEditor.AIModule")]
        [FreeFunction("NavMeshObstacleScriptBindings::FitExtents", HasExplicitThis = true)]
        internal extern void FitExtents();
    }
}
