// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    //=============================================================================
    // 🎯 PhysicsCommon2D —— 2D 物理核心枚举定义
    //
    // 设计说明:
    //   定义 2D 物理系统的核心枚举类型，这些枚举在 Physics2D 的整个 API 中广泛使用。
    //   必须与 C++ 侧的 2D 物理引擎枚举保持同步。
    //
    // 📌 SimulationMode2D（2D 物理模拟模式）:
    //   FixedUpdate — 在 FixedUpdate 中更新（默认，与物理计时器同步）
    //   Update      — 在 Update 中更新（帧率相关，适合非物理游戏）
    //   Script      — 手动调用 Physics2D.Simulate() 控制（自定义步长）
    //
    // 📌 RigidbodyType2D（刚体类型）:
    //   Dynamic    — 受力和碰撞影响（全物理模拟）
    //   Kinematic  — 用户控制运动，影响其他物体（速度驱动）
    //   Static     — 不受力影响，不可移动（性能最优）
    //
    // 📌 RigidbodyConstraints2D（刚体约束）:
    //   标志位枚举，可组合约束 X/Y 轴位置和 Z 轴旋转。
    //
    // 📌 PhysicsMaterialCombine2D（物理材质组合模式）:
    //   当两个碰撞体接触时，它们的物理材质属性（摩擦、弹性）
    //   通过此模式组合: Average / Mean / Multiply / Minimum / Maximum
    //=============================================================================

    public enum SimulationMode2D
    {
        FixedUpdate = 0,
        Update = 1,
        Script = 2
    }

    public enum RigidbodyType2D
    {
        // Dynamic body.
        Dynamic = 0,

        // Kinematic body.
        Kinematic = 1,

        // Static body.
        Static = 2,
    }

    [Flags]
    public enum RigidbodyConstraints2D
    {
        // No constraints
        None = 0,

        // Freeze motion along the X-axis.
        FreezePositionX = 1 << 0,

        // Freeze motion along the Y-axis.
        FreezePositionY = 1 << 1,

        // Freeze rotation along the Z-axis.
        FreezeRotation = 1 << 2,

        // Freeze motion along all axes.
        FreezePosition = FreezePositionX | FreezePositionY,

        // Freeze rotation and motion along all axes.
        FreezeAll = FreezePosition | FreezeRotation,
    }

    // The method used to combine both material values.
    public enum PhysicsMaterialCombine2D
    {
        // The average of both material values.
        Average = 0,

        // The geometric mean of both material values.
        Mean,

        // The product of both material values.
        Multiply,

        // The minium of both material values.
        Minimum,

        // The maximum of both material values.
        Maximum
    }
}
