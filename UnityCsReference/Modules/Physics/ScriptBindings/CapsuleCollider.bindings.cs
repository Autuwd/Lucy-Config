// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    //=============================================================================
    // CapsuleCollider - 胶囊体碰撞器
    //=============================================================================
    // 胶囊体由两个半球体 + 一个圆柱体组成，是最常用的角色碰撞器形状。
    // 适用于角色控制器、柱子、树木等细长物体。
    //
    // 参数：
    //   center：胶囊体中心的局部坐标
    //   radius：胶囊体半径（半球和圆柱的半径相同）
    //   height：胶囊体总高度（包括两端的半球）
    //   direction：胶囊体的轴向（0=X轴，1=Y轴，2=Z轴）
    //
    // 常见用途：
    //   - 玩家/角色碰撞器（通常 direction = 1，Y 轴向上）
    //   - 高瘦物体的碰撞检测
    //=============================================================================
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/CapsuleCollider.h")]
    public class CapsuleCollider : Collider
    {
        /// <summary>胶囊体中心的局部坐标偏移。</summary>
        extern public Vector3 center { get; set; }

        /// <summary>胶囊体的半径（两端半球和中间圆柱使用同一半径）。</summary>
        extern public float radius { get; set; }

        /// <summary>
        ///   胶囊体的总高度。
        ///   包括两端半球和中间圆柱。实际总高度 = height × Transform.lossyScale 在轴向的分量。
        /// </summary>
        extern public float height { get; set; }

        /// <summary>
        ///   胶囊体的延伸方向。
        ///   0 = X 轴方向
        ///   1 = Y 轴方向（默认，适合角色）
        ///   2 = Z 轴方向
        /// </summary>
        extern public int direction { get; set; }
    }
}
