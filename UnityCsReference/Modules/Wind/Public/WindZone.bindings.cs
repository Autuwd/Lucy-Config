// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// =================================================================================================
// 🎯 WindZone — 风力区域系统
// =================================================================================================
// WindZone 影响草（Terrain Detail）、树（Tree）、布料（Cloth）和粒子系统的运动。
//
// 📌 WindZoneMode 模式
//   - Directional: 方向风，无限范围，所有受影响对象朝同一方向摆动
//   - Spherical:    球状风，以 Transform.position 为中心，radius 为半径衰减
//
// 💡 风力参数
//   - windMain: 主风力大小（基础风速），直接影响树的弯曲幅度
//   - windTurbulence: 湍流强度，决定风的随机扰动幅度
//   - windPulseMagnitude: 脉冲幅度，模拟阵风的周期性增强
//   - windPulseFrequency: 脉冲频率（Hz），控制阵风间隔
//
// ⚡ 受风影响的系统
//   - Terrain: 草和树的弯曲程度 = windMain × windTurbulence × 脉冲
//   - Cloth: externalAcceleration 中的风力分量由 WindZone 贡献
//   - ParticleSystem: External Forces 模块受 WindZone 影响
// =================================================================================================

using System;

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Internal;

namespace UnityEngine
{
    public enum WindZoneMode
    {
        Directional,
        Spherical
    }

    [NativeHeader("Modules/Wind/Public/Wind.h")]
    public class WindZone : Component
    {
        extern public WindZoneMode mode {get; set; }
        extern public float radius {get; set; }
        extern public float windMain {get; set; }
        extern public float windTurbulence {get; set; }
        extern public float windPulseMagnitude  {get; set; }
        extern public float windPulseFrequency {get; set; }
    }
}
