// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// =================================================================================================
// 🎯 Cloth — 布料模拟系统
// =================================================================================================
// Unity 的布料模拟基于位置动力学（PBD, Position Based Dynamics），作用于
// SkinnedMeshRenderer 的顶点上，支持碰撞、风力和自碰撞。
//
// 📌 核心模拟参数
//   - bendingStiffness / stretchingStiffness: 弯曲/拉伸刚度（0-1）
//   - damping: 运动阻尼，数值越大布料越"沉"
//   - externalAcceleration / randomAcceleration: 外部力场（风/爆炸等）
//   - useGravity: 是否受重力影响
//   - friction / collisionMassScale: 碰撞摩擦与碰撞粒子质量缩放
//
// 💡 碰撞系统
//   - ClothSphereColliderPair: 支持胶囊体碰撞的球对定义
//   - sphereColliders / capsuleColliders: 球体和胶囊体碰撞体列表
//   - enableContinuousCollision: 连续碰撞检测（CCD），防止高速穿透
//   - selfCollisionDistance / selfCollisionStiffness: 自碰撞参数
//   - useVirtualParticles: 每个三角形添加虚拟粒子改善碰撞稳定性
//
// ⚡ 性能关键
//   - clothSolverFrequency: 求解器更新频率（Hz），越高越精确也越耗CPU
//   - GetSelfAndInterCollisionIndices: 获取参与碰撞的索引（调试/自定义碰撞用）
//   - ClearTransformMotion: 清除变换运动历史，用于瞬移后防止布料"拖尾"
//   - SetEnabledFading: 渐隐过渡，避免启用/禁用时的视觉突兀
// =================================================================================================

using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine
{
    [NativeHeader("Modules/Cloth/Cloth.h")]
    [UsedByNativeCode]
    public struct ClothSphereColliderPair
    {
        public SphereCollider first { get; set; }
        public SphereCollider second { get; set; }

        public ClothSphereColliderPair(SphereCollider a)
        {
            // initialize internal fields so that compiler does not complain about using properties before "this" is ready
            first = a;
            second = null;
        }

        public ClothSphereColliderPair(SphereCollider a, SphereCollider b)
        {
            // initialize internal fields so that compiler does not complain about using properties before "this" is ready
            first = a;
            second = b;
        }
    }

    // The ClothSkinningCoefficient struct is used to set up how a [[Cloth]] component is allowed to move with respect to the [[SkinnedMeshRenderer]] it is attached to.
    [UsedByNativeCode]
    public struct ClothSkinningCoefficient
    {
        //Distance a vertex is allowed to travel from the skinned mesh vertex position.
        public float maxDistance;

        //Definition of a sphere a vertex is not allowed to enter. This allows collision against the animated cloth.
        public float collisionSphereDistance;
    }

    [RequireComponent(typeof(Transform), typeof(SkinnedMeshRenderer))]
    [NativeHeader("Modules/Cloth/Cloth.h")]
    [NativeClass("Unity::Cloth")]
    public sealed partial class Cloth : Component
    {
        extern public Vector3[] vertices {[NativeName("GetPositions")] get; }
        extern public Vector3[] normals {[NativeName("GetNormals")] get; }
        extern public ClothSkinningCoefficient[] coefficients {[NativeName("GetCoefficients")] get; [NativeName("SetCoefficients")] set; }
        extern public CapsuleCollider[] capsuleColliders {[NativeName("GetCapsuleColliders")] get; [NativeName("SetCapsuleColliders")] set; }
        extern public ClothSphereColliderPair[] sphereColliders {[NativeName("GetSphereColliders")] get; [NativeName("SetSphereColliders")] set; }
        extern public float sleepThreshold { get; set; }

        // Bending stiffness of the cloth.
        extern public float bendingStiffness { get; set; }

        // Stretching stiffness of the cloth.
        extern public float stretchingStiffness { get; set; }

        // Damp cloth motion.
        extern public float damping { get; set; }

        // A constant, external acceleration applied to the cloth.
        extern public Vector3 externalAcceleration { get; set; }

        // A random, external acceleration applied to the cloth.
        extern public Vector3 randomAcceleration { get; set; }

        // Should gravity affect the cloth simulation?
        extern public bool useGravity { get; set; }

        // Is this cloth enabled?
        extern public bool enabled { get; set; }

        // The friction of the cloth when colliding with the character.
        extern public float friction { get; set; }

        // How much to increase mass of colliding particles
        extern public float collisionMassScale { get; set; }

        // Enable continuous collision to improve collision stability
        extern public bool enableContinuousCollision { get; set; }

        // Add 1 virtual particle per triangle to improve collision stability
        extern public float useVirtualParticles { get; set; }

        // How much world-space movement of the character will affect cloth vertices.
        extern public float worldVelocityScale { get; set; }

        // How much world-space acceleration of the character will affect cloth vertices.
        extern public float worldAccelerationScale { get; set; }

        extern public float clothSolverFrequency { get; set; }

        extern public bool useTethers { get; set; }

        extern public float stiffnessFrequency { get; set; }

        extern public float selfCollisionDistance { get; set; }

        extern public float selfCollisionStiffness { get; set; }

        extern public void ClearTransformMotion();

        extern public void GetSelfAndInterCollisionIndices([NotNull] List<uint> indices);

        extern public void SetSelfAndInterCollisionIndices([NotNull] List<uint> indices);

        extern public void GetVirtualParticleIndices([NotNull] List<uint> indicesOutList);

        extern public void SetVirtualParticleIndices([NotNull] List<uint> indicesIn);

        extern public void GetVirtualParticleWeights([NotNull] List<Vector3> weightsOutList);

        extern public void SetVirtualParticleWeights([NotNull] List<Vector3> weights);

        extern public void SetEnabledFading(bool enabled, float interpolationTime);

        [ExcludeFromDocs]
        public void SetEnabledFading(bool enabled)
        {
            SetEnabledFading(enabled, 0.5f);
        }
    }
}
