// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    //=============================================================================
    // BoxCollider - 盒子碰撞器
    //=============================================================================
    // 最基本的碰撞器形状之一，使用轴对齐或旋转对齐的立方体作为碰撞体积。
    // 适用于墙壁、地板、箱子、建筑等规则的方形物体。
    //
    // 参数：
    //   center：盒子的局部坐标中心点
    //   size：盒子的尺寸（在世界空间中的长、宽、高）
    //
    // 注意：如果 Transform 有缩放，size 是在本地空间定义的，
    // 最终的世界空间尺寸 = size × Transform.lossyScale。
    //=============================================================================
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/BoxCollider.h")]
    public partial class BoxCollider : Collider
    {
        /// <summary>盒子的中心位置（相对于 GameObject 局部坐标）。</summary>
        extern public Vector3 center { get; set; }

        /// <summary>
        ///   盒子的尺寸（局部空间的长度、宽度、高度）。
        ///   实际世界空间尺寸受 Transform 缩放影响。
        /// </summary>
        extern public Vector3 size { get; set; }
    }
}
