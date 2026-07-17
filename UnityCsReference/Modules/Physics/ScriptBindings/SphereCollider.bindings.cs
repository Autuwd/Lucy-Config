// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    //=============================================================================
    // SphereCollider - 球体碰撞器
    //=============================================================================
    // 使用球体作为碰撞体积。球体是最简单、性能最高的碰撞器形状。
    // 适用于球类、圆形物体、粒子效果等。
    //
    // 参数：
    //   center：球心的局部坐标
    //   radius：球体半径
    //
    // 性能说明：球体碰撞检测的计算量最小，
    // 仅需比较距离是否小于半径之和。
    //=============================================================================
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/SphereCollider.h")]
    public class SphereCollider : Collider
    {
        /// <summary>球心的局部坐标偏移。</summary>
        extern public Vector3 center { get; set; }

        /// <summary>球体半径（世界空间单位）。</summary>
        extern public float radius { get; set; }
    }
}
