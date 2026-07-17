// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    /// <summary>
    /// 物理材质摩擦力和弹性的组合模式枚举。
    /// 当两个碰撞器各有不同的 PhysicsMaterial 时，需要一种策略来合并两者的摩擦力和弹性系数。
    /// 此枚举控制合并方式，底层对应 PhysX 的 PxCombineMode。
    /// </summary>
    public enum PhysicsMaterialCombine
    {
        /// <summary>
        /// 平均值模式（枚举值 = 0）：取两个材质的平均值。
        /// 对应 PhysX 的 eAVERAGE。
        /// </summary>
        Average = 0,

        /// <summary>
        /// 乘积模式（枚举值 = 1）：取两个材质的乘积。
        /// 对应 PhysX 的 eMULTIPLY。
        /// </summary>
        Multiply,

        /// <summary>
        /// 最小值模式（枚举值 = 2）：取两个材质中的较小值。
        /// 对应 PhysX 的 eMIN。
        /// </summary>
        Minimum,

        /// <summary>
        /// 最大值模式（枚举值 = 3）：取两个材质中的较大值。
        /// 对应 PhysX 的 eMAX。
        /// </summary>
        Maximum,
    }

    /// <summary>
    /// 物理材质（PhysicsMaterial），定义碰撞表面的物理属性。
    /// 底层封装 PhysX 的 PxMaterial，控制摩擦力、弹性和组合方式。
    ///
    /// 使用方式：在场景中创建 PhysicsMaterial 资源，拖拽到 Collider.material 属性上。
    /// PhysX 约束：动摩擦力必须 ≤ 静摩擦力；弹性系数范围 [0, 1]。
    /// </summary>
    [NativeHeader("Modules/Physics/PhysicsMaterial.h")]
    public class PhysicsMaterial : UnityEngine.Object
    {
        /// <summary>创建一个名为"DynamicMaterial"的默认物理材质。</summary>
        public PhysicsMaterial() { Internal_CreateDynamicsMaterial(this, "DynamicMaterial"); }

        /// <summary>创建一个指定名称的物理材质。</summary>
        /// <param name="name">材质名称。</param>
        public PhysicsMaterial(string name) { Internal_CreateDynamicsMaterial(this, name); }

        /// <summary>内部方法：在 C++ 端创建 PhysX PxMaterial 对象。</summary>
        extern private static void Internal_CreateDynamicsMaterial([Writable] PhysicsMaterial mat, string name);

        /// <summary>
        /// 弹性系数 [0, 1]，0 表示无弹性（碰撞后速度完全损失），
        /// 1 表示完全弹性（碰撞后速度无损反弹）。
        /// 底层对应 PhysX PxMaterial::setRestitution。
        /// </summary>
        extern public float bounciness { get; set; }

        /// <summary>
        /// 动摩擦系数 [0, 通常 ≤ 1]，控制表面滑动时的摩擦力大小。
        /// 值越大摩擦力越强。动摩擦 ≤ 静摩擦是 PhysX 的推荐约束。
        /// 底层对应 PhysX PxMaterial::setDynamicFriction。
        /// </summary>
        extern public float dynamicFriction { get; set; }

        /// <summary>
        /// 静摩擦系数 [0, 通常 ≤ 1]，控制表面从静止到开始运动所需的最小力。
        /// 值越大越难推动。
        /// 底层对应 PhysX PxMaterial::setStaticFriction。
        /// </summary>
        extern public float staticFriction { get; set; }

        /// <summary>
        /// 两个材质摩擦力的合并模式。
        /// 当两个碰撞器有各自材质时，PhysX 使用此策略决定最终使用的摩擦力值。
        /// 底层对应 PhysX PxMaterial::setFrictionCombineMode。
        /// </summary>
        extern public PhysicsMaterialCombine frictionCombine { get; set; }

        /// <summary>
        /// 两个材质弹性的合并模式。
        /// 当两个碰撞器有各自材质时，PhysX 使用此策略决定最终使用的弹性值。
        /// 底层对应 PhysX PxMaterial::setRestitutionCombineMode。
        /// </summary>
        extern public PhysicsMaterialCombine bounceCombine { get; set; }
    }
}
