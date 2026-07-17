// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 ParticleSystem 模块系统 —— 粒子特效的功能引擎
//
// 📌 本文件职责：
//   定义 ParticleSystem 的所有功能模块（Module）。
//   每个模块控制粒子生命周期中的一个特定方面：
//     外观 → 大小、颜色、旋转、拖尾、灯光
//     运动 → 速度、力、噪声、重力
//     发射 → 速率、Burst 爆发、形状
//     物理 → 碰撞、触发器
//     组合 → 子发射器（粒子触发粒子）
//
// 🔑 模块化架构的核心思想：
//   与普通的 Component 组件不同，ParticleSystem 的模块
//   不是挂在 GameObject 上的独立 Component，而是 ParticleSystem
//   内部的"功能开关"。每个模块可以独立启用/禁用。
//
// 💡 模块是 struct（值类型），每次访问 property 会创建新实例。
//   但 struct 只持有对原生 ParticleSystem 的引用（一个 IntPtr），
//   不涉及堆内存分配，所以频繁访问的性能开销很小。
//
// ⚡ 所有模块共享一个模式：
//   - enabled 属性：开启/关闭该模块
//   - 内部持有 m_ParticleSystem 引用
//   - 属性直接映射到 C++ 原生端的数据
//
// 🎮 模块使用示例：
//   var ps = GetComponent<ParticleSystem>();
//
//   // 主模块 —— 控制基础粒子属性
//   ps.main.startSpeed = 10f;
//   ps.main.startLifetime = 2f;
//   ps.main.maxParticles = 1000;
//
//   // 发射模块 —— 控制发射速率
//   ps.emission.rateOverTime = 50f;
//   ps.emission.SetBursts(new[] { new Burst(0f, 100) });  // 第 0 秒爆发 100 个
//
//   // 形状模块 —— 控制发射区域
//   ps.shape.shapeType = ParticleSystemShapeType.Cone;
//   ps.shape.angle = 25f;
//
//   // 碰撞模块 —— 让粒子可以反弹
//   ps.collision.enabled = true;
//   ps.collision.type = ParticleSystemCollisionType.World;
//   ps.collision.bounce = 0.8f;
//
// ⚠️ 注意事项：
//   - 禁用模块（enabled = false）不会销毁数据，只是跳过计算
//   - 模块之间有隐式依赖（如 VelocityOverLifetime 可能受 Shape 影响）
//   - 修改模块参数通常不需要重启播放，实时生效
//
// 📍 对应 C++ 头文件：
//   Modules/ParticleSystem/ScriptBindings/ParticleSystemModulesScriptBindings.h
// ================================================================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemModulesScriptBindings.h")]
    // ================================================================
    // 🎯 ParticleSystem partial class —— 模块定义部分
    //
    // 📌 这是 ParticleSystem 类的 partial 声明（与 bindings 文件合并）。
    //   所有模块以内部 struct 的形式定义在此文件中。
    //
    // 🔑 模块设计模式：
    //   每个模块 struct 都遵循相同的结构：
    //     1. 构造函数接收 ParticleSystem 引用
    //     2. 内部存储 m_ParticleSystem 字段
    //     3. 提供 enabled 属性控制开关
    //     4. 提供与模块功能相关的属性和方法
    //
    // 💡 MinMaxCurve / MinMaxGradient —— 粒子系统的核心数据类型：
    //   MinMaxCurve  → 在 min/max 之间随机或沿曲线变化的浮点值
    //   MinMaxGradient → 在 min/max 之间随机或沿渐变变化的颜色值
    //   这两个类型是粒子系统"丰富表现力"的基础：
    //   每个粒子的属性都可以在一个范围内随机取值。
    // ================================================================
    public partial class ParticleSystem : Component
    {
        // ================================================================
        // 🎯 MainModule — 主模块（粒子系统的核心配置）
        //
        // 📌 这是最关键的模块，控制粒子的基础属性：
        //
        //   🎯 发射器行为：
        //     duration        → 播放持续时间（一个循环的长度）
        //     loop            → 是否循环播放
        //     prewarm         → 是否预热（跳过初始静默期）
        //     playOnAwake     → 是否在唤醒时自动播放
        //     startDelay      → 开始延迟（延迟多久后开始发射）
        //
        //   🎯 粒子初始属性（MinMaxCurve 类型 = 可随机范围）：
        //     startLifetime   → 粒子存活时间
        //     startSpeed      → 初始速度
        //     startSize       → 初始大小（支持 3D 非均匀缩放）
        //     startRotation   → 初始旋转（支持 3D 旋转）
        //     startColor      → 初始颜色
        //     gravityModifier → 重力倍率（负值 = 反重力）
        //
        //   🎯 模拟空间（simulationSpace）：
        //     Local    → 粒子跟随发射器移动（发射后受父级影响）
        //     World    → 粒子在世界空间中独立存在（发射后不受发射器影响）
        //     Custom   → 粒子跟随指定的 Transform 移动
        //
        //   🎯 其他重要属性：
        //     scalingMode          → 缩放模式（Shape/Hierarchy/Local/ShapeHierarchy）
        //     maxParticles         → 最大粒子数上限（超过时停止发射）
        //     simulationSpeed      → 模拟速度倍率（快进/慢放）
        //     useUnscaledTime      → 是否忽略 Time.timeScale
        //     emitterVelocityMode → 发射器速度计算模式
        //     ringBufferMode       → 环形缓冲模式（粒子死亡后重新使用）
        //     cullingMode          → 剔除模式（不可见时是否继续模拟）
        //
    // 💡 startDelay/startLifetime/startSpeed 等都有 Multiplier 后缀的属性。
    //    Multiplier 是一个简单的乘数因子，可以动态调整曲线/常量的整体缩放。
    //    例如 startLifetime 设置了曲线，startLifetimeMultiplier 可以全局缩放。
    //
    // ⚠️ maxParticles 是性能的安全阀：
    //   设为无限大可能导致内存和性能问题，
    //   建议根据实际需求设置合理的上限（通常 100~10000）。
    // ================================================================
    // Modules
    public partial struct MainModule
        {
            internal MainModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public Vector3 emitterVelocity { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float duration { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool loop { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool prewarm { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startDelay { get => startDelayBlittable; set => startDelayBlittable = value; }
            [NativeName("StartDelay")] private extern MinMaxCurveBlittable startDelayBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startDelayMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startLifetime { get => startLifetimeBlittable; set => startLifetimeBlittable = value; }
            [NativeName("StartLifetime")] private extern MinMaxCurveBlittable startLifetimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startLifetimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSpeed { get => startSpeedBlittable; set => startSpeedBlittable = value; }
            [NativeName("StartSpeed")] private extern MinMaxCurveBlittable startSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool startSize3D { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSize { get => startSizeBlittable; set => startSizeBlittable = value; }
            [NativeName("StartSizeX")] private extern MinMaxCurveBlittable startSizeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("StartSizeXMultiplier")]
            extern public float startSizeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSizeX { get => startSizeXBlittable; set => startSizeXBlittable = value; }
            [NativeName("StartSizeX")] private extern MinMaxCurveBlittable startSizeXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startSizeXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSizeY { get => startSizeYBlittable; set => startSizeYBlittable = value; }
            [NativeName("StartSizeY")] private extern MinMaxCurveBlittable startSizeYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startSizeYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSizeZ { get => startSizeZBlittable; set => startSizeZBlittable = value; }
            [NativeName("StartSizeZ")] private extern MinMaxCurveBlittable startSizeZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startSizeZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool startRotation3D { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startRotation { get => startRotationBlittable; set => startRotationBlittable = value; }
            [NativeName("StartRotationZ")] private extern MinMaxCurveBlittable startRotationBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("StartRotationZMultiplier")]
            extern public float startRotationMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startRotationX { get => startRotationXBlittable; set => startRotationXBlittable = value; }
            [NativeName("StartRotationX")] private extern MinMaxCurveBlittable startRotationXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startRotationXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startRotationY { get => startRotationYBlittable; set => startRotationYBlittable = value; }
            [NativeName("StartRotationY")] private extern MinMaxCurveBlittable startRotationYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startRotationYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startRotationZ { get => startRotationZBlittable; set => startRotationZBlittable = value; }
            [NativeName("StartRotationZ")] private extern MinMaxCurveBlittable startRotationZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startRotationZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float flipRotation { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient startColor { get => startColorBlittable; set => startColorBlittable = value; }
            [NativeName("StartColor")] private extern MinMaxGradientBlittable startColorBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public ParticleSystemGravitySource gravitySource { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve gravityModifier { get => gravityModifierBlittable; set => gravityModifierBlittable = value; }
            [NativeName("GravityModifier")] private extern MinMaxCurveBlittable gravityModifierBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float gravityModifierMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemSimulationSpace simulationSpace { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Transform customSimulationSpace { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float simulationSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useUnscaledTime { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemScalingMode scalingMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool playOnAwake { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int maxParticles { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemEmitterVelocityMode emitterVelocityMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemStopAction stopAction { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemRingBufferMode ringBufferMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 ringBufferLoopRange { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemCullingMode cullingMode { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 EmissionModule — 发射模块（控制粒子产生速率）
        //
        // 📌 控制粒子系统的发射行为：
        //
        //   🎯 持续发射：
        //     rateOverTime    → 每秒发射多少个粒子（基于时间）
        //     rateOverDistance → 每移动一单位距离发射多少个粒子（基于距离）
        //
        //   🎯 Burst 爆发发射：
        //     在指定时间点瞬间发射大量粒子（如爆炸效果）
        //     SetBursts() / GetBursts() → 批量设置/获取 Burst 数组
        //     burstCount → Burst 事件的数量
        //
        // 💡 Burst 结构体：
        //   new Burst(time, minCount, maxCount, cycleCount, repeatInterval)
        //   - time: 在播放循环中的哪个时间点触发
        //   - minCount/maxCount: 发射粒子数的随机范围
        //   - cycleCount: 重复次数（-1 = 无限重复）
        //   - repeatInterval: 重复间隔时间
        //
        // 🎮 Burst 示例：
        //   // 第 0 秒爆发 20~30 个粒子，之后每 0.5 秒重复 3 次
        //   new Burst(0f, 20, 30, 3, 0.5f)
        //
        // ⚡ rateOverDistance 的妙用：
        //   适合拖尾类效果（如飞机尾迹、赛车轮胎印），
        //   粒子密度与运动速度成正比，慢速时不会过度堆积。
        // ================================================================
        public partial struct EmissionModule
        {
            internal EmissionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve rateOverTime { get => rateOverTimeBlittable; set => rateOverTimeBlittable = value; }
            [NativeName("RateOverTime")] private extern MinMaxCurveBlittable rateOverTimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float rateOverTimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve rateOverDistance { get => rateOverDistanceBlittable; set => rateOverDistanceBlittable = value; }
            [NativeName("RateOverDistance")] private extern MinMaxCurveBlittable rateOverDistanceBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float rateOverDistanceMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public void SetBursts(Burst[] bursts)
            {
                SetBursts(bursts, bursts.Length);
            }

            public void SetBursts(Burst[] bursts, int size)
            {
                burstCount = size;
                for (int i = 0; i < size; i++)
                    SetBurst(i, bursts[i]);
            }

            public int GetBursts(Burst[] bursts)
            {
                int returnValue = burstCount;
                for (int i = 0; i < returnValue; i++)
                    bursts[i] = GetBurst(i);
                return returnValue;
            }

            [NativeMethod(ThrowsException = true)]
            extern public void SetBurst(int index, Burst burst);
            [NativeMethod(ThrowsException = true)]
            extern public Burst GetBurst(int index);
            extern public int burstCount { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 ShapeModule — 形状模块（控制粒子发射区域和方向）
        //
        // 📌 定义粒子从哪里发射、朝哪个方向发射：
        //
        //   🎯 发射形状（shapeType）：
        //     Sphere          → 球体表面/内部
        //     Hemisphere      → 半球表面/内部
        //     Cone            → 锥体（最常用，如喷泉、火焰）
        //     Box             → 盒体表面/内部
        //     Circle          → 圆形/环形
        //     Mesh            → 从 3D 网格表面发射
        //     MeshRenderer    → 从 MeshRenderer 的网格发射
        //     SkinnedMeshRenderer → 从蒙皮网格发射（角色特效）
        //     Donut           → 甜甜圈形状
        //     Rectangle       → 矩形（2D 粒子常用）
    //     SingleSidedEdge → 单边线段
    //     ConeMesh        → 从锥体网格发射
    //     CircleEdge      → 圆形边缘
    //     ConeVolumetric  → 锥体体积内发射
    //
    //   🎯 关键属性：
    //     radius/thickness    → 半径和厚度
    //     angle               → 锥体角度
    //     arc                 → 发射弧度（0~360°，限制发射方向范围）
    //     alignToDirection    → 粒子朝向是否对齐发射方向
    //     randomDirectionAmount → 发射方向的随机偏移量
    //     mesh/sprite         → 用于网格/精灵形状的资源
    //
    //   🎯 纹理发射器：
    //     texture             → 使用纹理亮度作为发射概率
    //     textureClipThreshold → 裁剪阈值（低于此亮度不发射）
    //     textureColorAffectsParticles → 纹理颜色影响粒子颜色
    //
    //   🎯 形状变换：
    //     position/rotation/scale → 形状的局部偏移/旋转/缩放
    //     （不影响粒子行为，只影响发射区域的变换）
    //
    // 💡 arc 模式的妙处：
    //   arcMode 支持 Random/Loop/PingPong/LifetimeSpread：
    //   - Random: 在弧度范围内随机选择方向
    //   - Loop: 按顺序遍历弧度方向
    //   - 这可以让粒子呈螺旋或扫射等模式发射
    //
    // ⚡ 网格发射器性能提示：
    //   使用 Mesh 发射形状比简单形状（Sphere/Cone）开销更大，
    //   因为需要对网格三角面进行采样。
    //   MeshRenderer/SkinnedMeshRenderer 甚至需要在 CPU 端缓存网格数据。
    // ================================================================
        public partial struct ShapeModule
        {
            internal ShapeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeType shapeType { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float randomDirectionAmount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float sphericalDirectionAmount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float randomPositionAmount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool alignToDirection { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radius { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeMultiModeValue radiusMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radiusSpread { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve radiusSpeed { get => radiusSpeedBlittable; set => radiusSpeedBlittable = value; }
            [NativeName("RadiusSpeed")] private extern MinMaxCurveBlittable radiusSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float radiusSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radiusThickness { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float angle { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float length { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector3 boxThickness { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemMeshShapeType meshShapeType { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Mesh mesh { get; [NativeMethod(ThrowsException = true)] set; }
            extern public MeshRenderer meshRenderer { get; [NativeMethod(ThrowsException = true)] set; }
            extern public SkinnedMeshRenderer skinnedMeshRenderer { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Sprite sprite { get; [NativeMethod(ThrowsException = true)] set; }
            extern public SpriteRenderer spriteRenderer { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useMeshMaterialIndex { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int meshMaterialIndex { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useMeshColors { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float normalOffset { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeMultiModeValue meshSpawnMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float meshSpawnSpread { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve meshSpawnSpeed { get => meshSpawnSpeedBlittable; set => meshSpawnSpeedBlittable = value; }
            [NativeName("MeshSpawnSpeed")] private extern MinMaxCurveBlittable meshSpawnSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float meshSpawnSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float arc { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeMultiModeValue arcMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float arcSpread { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve arcSpeed { get => arcSpeedBlittable; set => arcSpeedBlittable = value; }
            [NativeName("ArcSpeed")] private extern MinMaxCurveBlittable arcSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float arcSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float donutRadius { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector3 position { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector3 rotation { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector3 scale { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Texture2D texture { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeTextureChannel textureClipChannel { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float textureClipThreshold { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool textureColorAffectsParticles { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool textureAlphaAffectsParticles { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool textureBilinearFiltering { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int textureUVChannel { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 VelocityOverLifetimeModule — 生命周期内速度变化模块
        //
        // 📌 控制粒子在存活期间速度如何随时间变化：
        //
        //   🎯 线性速度分量（x/y/z）：
        //     在粒子空间中沿各轴施加额外速度。
        //     值随粒子生命周期变化（通过 MinMaxCurve 曲线控制）。
        //
        //   🎯 轨道速度（orbitalX/Y/Z）：
        //     让粒子绕指定轴做轨道运动。
        //     例如 orbitalY = 360 可以让粒子绕 Y 轴旋转飞行。
        //     orbitalOffset 为轨道运动的偏移量。
        //
    //   🎯 径向速度（radial）：
    //     让粒子沿径向（远离/靠近发射器中心）加速或减速。
    //
    //   🎯 速度修改器（speedModifier）：
    //     全局速度倍率曲线，统一缩放所有速度分量。
    //
    // 💡 space 属性：
    //   Local → 速度在发射器局部空间中应用（粒子跟随发射器旋转）
    //   World → 速度在世界空间中应用（粒子独立运动）
    //
    // 🎮 常见用法：
    //   - orbitalY = 180 → 粒子绕 Y 轴旋转飞行（漩涡效果）
    //   - radial 随时间增大 → 粒子逐渐向外扩散
    //   - speedModifier 从 1 降到 0 → 粒子逐渐减速停止
    // ================================================================
        public partial struct VelocityOverLifetimeModule
        {
            internal VelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalX { get => orbitalXBlittable; set => orbitalXBlittable = value; }
            [NativeName("OrbitalX")] private extern MinMaxCurveBlittable orbitalXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalY { get => orbitalYBlittable; set => orbitalYBlittable = value; }
            [NativeName("OrbitalY")] private extern MinMaxCurveBlittable orbitalYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalZ { get => orbitalZBlittable; set => orbitalZBlittable = value; }
            [NativeName("OrbitalZ")] private extern MinMaxCurveBlittable orbitalZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float orbitalXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float orbitalYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float orbitalZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalOffsetX { get => orbitalOffsetXBlittable; set => orbitalOffsetXBlittable = value; }
            [NativeName("OrbitalOffsetX")] private extern MinMaxCurveBlittable orbitalOffsetXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalOffsetY { get => orbitalOffsetYBlittable; set => orbitalOffsetYBlittable = value; }
            [NativeName("OrbitalOffsetY")] private extern MinMaxCurveBlittable orbitalOffsetYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalOffsetZ { get => orbitalOffsetZBlittable; set => orbitalOffsetZBlittable = value; }
            [NativeName("OrbitalOffsetZ")] private extern MinMaxCurveBlittable orbitalOffsetZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float orbitalOffsetXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float orbitalOffsetYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float orbitalOffsetZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve radial { get => radialBlittable; set => radialBlittable = value; }
            [NativeName("Radial")] private extern MinMaxCurveBlittable radialBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float radialMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve speedModifier { get => speedModifierBlittable; set => speedModifierBlittable = value; }
            [NativeName("SpeedModifier")] private extern MinMaxCurveBlittable speedModifierBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float speedModifierMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemSimulationSpace space { get; [NativeMethod(ThrowsException = true)] set; }
        }


        // ================================================================
        // 🎯 LimitVelocityOverLifetimeModule — 速度限制模块
        //
        // 📌 限制粒子的最大速度，超过限制的速度会被衰减：
        //
        //   🎯 速度限制：
        //     limit/limitX/Y/Z → 各轴或总速度的上限值
        //     separateAxes     → 是否分轴限制（false = 限制总速度）
        //     dampen           → 超过限制时的衰减系数（0=不衰减，1=完全衰减）
        //
        //   🎯 阻力（drag）：
        //     drag → 持续施加的空气阻力，让粒子逐渐减速
        //     multiplyDragByParticleSize   → 阻力是否与粒子大小成正比
        //     multiplyDragByParticleVelocity → 阻力是否与粒子速度成正比
        //
    // 💡 与 VelocityOverLifetime 的区别：
    //   VelocityOverLifetime → "施加"速度（加速/轨道运动）
    //   LimitVelocityOverLifetime → "限制"速度（减速/制动）
    //   两者可以配合使用：先加速再限制最高速度。
    //
    // 🎮 常见用法：
    //   - dampen > 0 且 limit 较小 → 粒子达到一定速度后减速（如烟雾扩散）
    //   - drag > 0 → 模拟空气阻力，粒子逐渐停下来
    //   - 使用 Local 空间可以让速度限制跟随发射器移动
    // ================================================================
        public partial struct LimitVelocityOverLifetimeModule
        {
            internal LimitVelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve limitX { get => limitXBlittable; set => limitXBlittable = value; }
            [NativeName("LimitX")] private extern MinMaxCurveBlittable limitXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float limitXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve limitY { get => limitYBlittable; set => limitYBlittable = value; }
            [NativeName("LimitY")] private extern MinMaxCurveBlittable limitYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float limitYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve limitZ { get => limitZBlittable; set => limitZBlittable = value; }
            [NativeName("LimitZ")] private extern MinMaxCurveBlittable limitZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float limitZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve limit { get => limitBlittable; set => limitBlittable = value; }
            [NativeName("Magnitude")] private extern MinMaxCurveBlittable limitBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("MagnitudeMultiplier")]
            extern public float limitMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float dampen { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemSimulationSpace space { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve drag { get => dragBlittable; set => dragBlittable = value; }
            [NativeName("Drag")] private extern MinMaxCurveBlittable dragBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float dragMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyDragByParticleSize { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyDragByParticleVelocity { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 InheritVelocityModule — 速度继承模块
        //
        // 📌 让新发射的粒子继承发射器当前的运动速度：
        //
    //   mode → 继承模式：
    //     Current   → 每次发射时继承当前发射器速度
    //     Initial   → 只在粒子系统首次播放时获取发射器速度
    //     PerFrame  → 每帧更新继承的速度
    //
    //   curve/curveMultiplier → 继承速度的倍率曲线
    //
    // 💡 典型场景：
    //   - 移动的汽车排放尾气：烟雾粒子应继承汽车的速度，
    //     这样烟雾才会自然地向后飘散。
    //   - 不使用此模块时，粒子一出生就静止在发射位置。
    // ================================================================
        public partial struct InheritVelocityModule
        {
            internal InheritVelocityModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemInheritVelocityMode mode { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve curve { get => curveBlittable; set => curveBlittable = value; }
            [NativeName("Curve")] private extern MinMaxCurveBlittable curveBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float curveMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 LifetimeByEmitterSpeedModule — 根据发射器速度调整生命周期
        //
        // 📌 根据发射器的运动速度动态调整粒子的存活时间：
        //
    //   curve/curveMultiplier → 速度-生命周期映射曲线
    //   range → 速度的有效范围（映射到曲线的 0~1）
    //
    // 💡 使用场景：
    //   - 快速移动时粒子存活更久（如高速赛车的轮胎烟雾更长）
    //   - 慢速时粒子很快消亡（避免静态时粒子堆积）
    //   - 让粒子行为与发射器的运动状态动态关联
    //
    // ⚠️ 注意：此模块依赖发射器的速度数据，
    //   如果发射器静止不动，曲线将始终映射到 range 的起始值。
    // ================================================================
        public partial struct LifetimeByEmitterSpeedModule
        {
            internal LifetimeByEmitterSpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve curve { get => curveBlittable; set => curveBlittable = value; }
            [NativeName("Curve")] private extern MinMaxCurveBlittable curveBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float curveMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 ForceOverLifetimeModule — 生命周期内力场模块
        //
        // 📌 在粒子存活期间施加持续的力（加速度）：
        //
    //   x/y/z → 各轴方向的力值（通过 MinMaxCurve 随时间变化）
    //   space → 力的作用空间（Local/World）
    //   randomized → 每帧是否重新随机力值
    //
    // 💡 Force vs VelocityOverLifetime：
    //   ForceOverLifetime → 施加"加速度"（力越大，速度持续增加）
    //   VelocityOverLifetime → 施加"速度"（直接叠加到速度上）
    //   物理上：Force 类似持续施力，Velocity 类似初始速度偏移
    //
    // 🎮 常见用法：
    //   - x/y/z 设置为负值 → 模拟重力（替代 MainModule 的 gravityModifier）
    //   - randomized = true → 每帧随机方向的力（如风的湍流效果）
    //   - 在 Local 空间中使用 → 力随发射器旋转而旋转
    // ================================================================
        public partial struct ForceOverLifetimeModule
        {
            internal ForceOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemSimulationSpace space { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool randomized { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 ColorOverLifetimeModule — 生命周期内颜色变化
        //
        // 📌 控制粒子颜色如何随时间变化：
    //
    //   color → MinMaxGradient 类型的颜色渐变
    //     - 可以使用渐变（Gradient）：从一种颜色渐变到另一种
    //     - 可以使用两个渐变之间随机选取（MinMaxGradient）
    //     - 渐变的 Time 从 0（出生）到 1（死亡）
    //
    // 💡 渐变中的 Alpha 通道非常有用：
    //   通常将渐变尾部的 Alpha 设为 0，实现粒子淡出效果。
    //   这比在 SizeOverLifetime 中缩小粒子更自然。
    //
    // 🎮 常见渐变配置：
    //   火焰：黄 → 橙 → 红 → 透明
    //   烟雾：灰 → 深灰 → 透明
    //   魔法：亮蓝 → 淡紫 → 透明
    //   爆炸：白 → 黄 → 橙 → 透明
    // ================================================================
        public partial struct ColorOverLifetimeModule
        {
            internal ColorOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient color { get => colorBlittable; set => colorBlittable = value; }
            [NativeName("Color")] private extern MinMaxGradientBlittable colorBlittable { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 ColorBySpeedModule — 根据速度变化颜色
        //
        // 📌 根据粒子当前速度映射到颜色渐变：
        //
    //   color → 速度到颜色的映射渐变
    //   range → 速度的有效范围（min/max 映射到渐变的 0~1）
    //
    // 💡 与 ColorOverLifetime 的区别：
    //   ColorOverLifetime → 颜色随"时间"变化（基于粒子年龄）
    //   ColorBySpeed → 颜色随"速度"变化（基于粒子当前速度）
    //   两者可以同时启用，最终颜色是叠加效果。
    //
    // 🎮 典型用法：
    //   - 快粒子显示为蓝色，慢粒子显示为红色（速度可视化）
    //   - 碰撞后速度降低，颜色从白变暗（能量消耗的视觉反馈）
    // ================================================================
        public partial struct ColorBySpeedModule
        {
            internal ColorBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient color { get => colorBlittable; set => colorBlittable = value; }
            [NativeName("Color")] private extern MinMaxGradientBlittable colorBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 SizeOverLifetimeModule — 生命周期内大小变化
        //
        // 📌 控制粒子大小如何随时间变化：
        //
    //   size/x/y/z → 大小曲线（默认 uniform，可分轴控制）
    //   separateAxes → 是否分轴控制大小（true = 可以做拉伸效果）
    //
    // 💡 常见曲线配置：
    //   - 从 0 到 1 的曲线 → 粒子从小到大（弹出效果）
    //   - 从 1 到 0 的曲线 → 粒子从大到小（消散效果）
    //   - 常与 ColorOverLifetime 的 Alpha 淡出配合使用
    //
    // ⚡ 分轴缩放（separateAxes = true）：
    //   可以让粒子在某个方向上拉伸（如流星尾迹），
    //   或在某个方向上压扁（如冲击波扩散）。
    // ================================================================
        public partial struct SizeOverLifetimeModule
        {
            internal SizeOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve size { get => sizeBlittable; set => sizeBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable sizeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 SizeBySpeedModule — 根据速度变化大小
        //
        // 📌 根据粒子当前速度映射到大小值：
        //
    //   size/x/y/z → 速度到大小的映射曲线
    //   separateAxes → 是否分轴控制
    //   range → 速度的有效范围（映射到曲线的 0~1）
    //
    // 💡 与 SizeOverLifetime 的区别：
    //   SizeOverLifetime → 大小随"时间"变化
    //   SizeBySpeed → 大小随"速度"变化
    //   两者可以同时启用，最终大小是乘积关系。
    //
    // 🎮 典型用法：
    //   - 快速粒子更大（模拟运动模糊拖影效果）
    //   - 速度越快粒子越小（高速粒子被"压缩"的视觉效果）
    // ================================================================
        public partial struct SizeBySpeedModule
        {
            internal SizeBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve size { get => sizeBlittable; set => sizeBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable sizeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 RotationOverLifetimeModule — 生命周期内旋转变化
        //
        // 📌 控制粒子在存活期间如何旋转：
        //
    //   x/y/z → 各轴的旋转速度曲线（单位：度/秒）
    //   separateAxes → 是否分轴控制
    //
    // 💡 常见用法：
    //   - z 轴设置正值 → 粒子持续顺时针旋转（纸片飘落效果）
    //   - x/y/z 同时设置 → 粒子翻滚旋转（碎片飞散效果）
    //   - 曲线从高到低 → 旋转逐渐减速
    //
    // ⚠️ 注意：这是旋转速度，不是旋转角度。
    //   曲线的值表示"每秒旋转多少度"，不是"当前旋转到多少度"。
    // ================================================================
        public partial struct RotationOverLifetimeModule
        {
            internal RotationOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 RotationBySpeedModule — 根据速度变化旋转
        //
        // 📌 根据粒子当前速度映射到旋转速度：
        //
    //   x/y/z → 速度到旋转速度的映射曲线
    //   separateAxes → 是否分轴控制
    //   range → 速度的有效范围
    //
    // 💡 与 RotationOverLifetime 的区别：
    //   RotationOverLifetime → 旋转随"时间"变化
    //   RotationBySpeed → 旋转随"速度"变化
    //
    // 🎮 典型用法：
    //   - 速度越快旋转越快（如旋转飞镖）
    //   - 静止时不旋转，运动时才开始旋转
    // ================================================================
        public partial struct RotationBySpeedModule
        {
            internal RotationBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 ExternalForcesModule — 外部力场模块
        //
        // 📌 让粒子受 ParticleSystemForceField 组件影响：
        //
    //   multiplier / multiplierCurve → 力场影响的强度倍率
    //   influenceFilter → 影响过滤方式（按层/按列表）
    //   influenceMask → 层级掩码（按层过滤时使用）
    //
    //   🔧 力场管理方法：
    //     AddInfluence() / RemoveInfluence() → 添加/移除力场影响
    //     SetInfluence() / GetInfluence()    → 替换/获取力场引用
    //     RemoveAllInfluences()              → 移除所有力场影响
    //     influenceCount → 当前影响的力场数量
    //     IsAffectedBy() → 检查是否受指定力场影响
    //
    // 💡 ParticleSystemForceField 组件：
    //   可以创建各种力场效果（如引力场、涡旋场、风场、
    //   升降场、阻力场等），多个粒子系统可以共享同一个力场。
    //
    // 🎮 使用示例：
    //   // 1. 创建引力场
    //   var forceField = go.AddComponent<ParticleSystemForceField>();
    //   forceField.shape = ParticleSystemForceFieldShape.Sphere;
    //   forceField.gravity = 5f;
    //   // 2. 粒子系统自动检测场景中的力场（开启此模块即可）
    //   ps.externalForces.enabled = true;
    // ================================================================
        public partial struct ExternalForcesModule
        {
            internal ExternalForcesModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float multiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve multiplierCurve { get => multiplierCurveBlittable; set => multiplierCurveBlittable = value; }
            [NativeName("MultiplierCurve")] private extern MinMaxCurveBlittable multiplierCurveBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public ParticleSystemGameObjectFilter influenceFilter { get; [NativeMethod(ThrowsException = true)] set; }
            extern public LayerMask influenceMask { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public int influenceCount { get; }

            extern public bool IsAffectedBy(ParticleSystemForceField field);

            [NativeMethod(ThrowsException = true)]
            extern public void AddInfluence([NotNull] ParticleSystemForceField field);

            [NativeMethod(ThrowsException = true)]
            extern private void RemoveInfluenceAtIndex(int index);
            public void RemoveInfluence(int index) { RemoveInfluenceAtIndex(index); }

            [NativeMethod(ThrowsException = true)]
            extern public void RemoveInfluence([NotNull] ParticleSystemForceField field);
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveAllInfluences();
            [NativeMethod(ThrowsException = true)]
            extern public void SetInfluence(int index, [NotNull] ParticleSystemForceField field);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemForceField GetInfluence(int index);
        }

        // ================================================================
        // 🎯 NoiseModule — 噪声扰动模块（湍流/漩涡效果）
        //
        // 📌 使用 Perlin 噪声为粒子添加随机扰动：
        //
    //   🎯 强度控制：
    //     strength/strengthX/Y/Z → 噪声对粒子的影响力
    //     separateAxes → 是否分轴施加不同强度的噪声
    //
    //   🎯 噪声参数：
    //     frequency → 噪声频率（越高越"密集"，越低越"平滑"）
    //     damping  → 是否衰减（true = 高频分量减弱）
    //     octaveCount → 噪声叠加层数（越多细节越丰富，但更消耗性能）
    //     octaveMultiplier → 每层噪声的强度倍率
    //     octaveScale → 每层噪声的频率缩放
    //     quality → 噪声质量（Low/Medium/High）
    //
    //   🎯 滚动效果：
    //     scrollSpeed → 噪声纹理滚动速度（产生流动效果）
    //
    //   🎯 重映射（Remap）：
    //     remapEnabled → 启用后将噪声值重新映射到指定范围
    //     remap/remapX/Y/Z → 重映射曲线
    //
    //   🎯 影响范围控制：
    //     positionAmount → 噪声对位置的影响程度
    //     rotationAmount → 噪声对旋转的影响程度
    //     sizeAmount → 噪声对大小的影响程度
    //
    // 💡 噪声是创造自然运动的关键：
    //   - 低频 + 高强度 → 大幅度缓慢运动（如云彩飘动）
    //   - 高频 + 低强度 → 小幅度快速抖动（如火焰闪烁）
    //   - scrollSpeed > 0 → 噪声场在流动（如水面波纹）
    //
    // ⚡ octaveCount 的性能影响：
    //   每增加一层八度（octave），计算量几乎翻倍。
    //   通常 2~3 层就够了，除非需要非常精细的噪声细节。
    // ================================================================
        public partial struct NoiseModule
        {
            internal NoiseModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve strength { get => strengthBlittable; set => strengthBlittable = value; }
            [NativeName("StrengthX")] private extern MinMaxCurveBlittable strengthBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("StrengthXMultiplier")]
            extern public float strengthMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve strengthX { get => strengthXBlittable; set => strengthXBlittable = value; }
            [NativeName("StrengthX")] private extern MinMaxCurveBlittable strengthXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float strengthXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve strengthY { get => strengthYBlittable; set => strengthYBlittable = value; }
            [NativeName("StrengthY")] private extern MinMaxCurveBlittable strengthYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float strengthYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve strengthZ { get => strengthZBlittable; set => strengthZBlittable = value; }
            [NativeName("StrengthZ")] private extern MinMaxCurveBlittable strengthZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float strengthZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float frequency { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool damping { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int octaveCount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float octaveMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float octaveScale { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemNoiseQuality quality { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve scrollSpeed { get => scrollSpeedBlittable; set => scrollSpeedBlittable = value; }
            [NativeName("ScrollSpeed")] private extern MinMaxCurveBlittable scrollSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float scrollSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool remapEnabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve remap { get => remapBlittable; set => remapBlittable = value; }
            [NativeName("RemapX")] private extern MinMaxCurveBlittable remapBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("RemapXMultiplier")]
            extern public float remapMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve remapX { get => remapXBlittable; set => remapXBlittable = value; }
            [NativeName("RemapX")] private extern MinMaxCurveBlittable remapXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float remapXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve remapY { get => remapYBlittable; set => remapYBlittable = value; }
            [NativeName("RemapY")] private extern MinMaxCurveBlittable remapYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float remapYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve remapZ { get => remapZBlittable; set => remapZBlittable = value; }
            [NativeName("RemapZ")] private extern MinMaxCurveBlittable remapZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float remapZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve positionAmount { get => positionAmountBlittable; set => positionAmountBlittable = value; }
            [NativeName("PositionAmount")] private extern MinMaxCurveBlittable positionAmountBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve rotationAmount { get => rotationAmountBlittable; set => rotationAmountBlittable = value; }
            [NativeName("RotationAmount")] private extern MinMaxCurveBlittable rotationAmountBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve sizeAmount { get => sizeAmountBlittable; set => sizeAmountBlittable = value; }
            [NativeName("SizeAmount")] private extern MinMaxCurveBlittable sizeAmountBlittable { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 CollisionModule — 碰撞模块（粒子与世界的物理交互）
        //
        // 📌 让粒子可以与场景中的 Collider 发生碰撞：
        //
    //   🎯 碰撞类型：
    //     type → World（与场景 Collider 碰撞）或 Planes（与平面碰撞）
    //     mode → 3D 物理碰撞 或 2D 物理碰撞
    //
    //   🎯 碰撞物理参数：
    //     dampen → 碰撞后的速度衰减系数（0=不衰减，1=完全停止）
    //     bounce → 反弹系数（0=不反弹，1=完全弹性碰撞）
    //     lifetimeLoss → 每次碰撞损失的生命周期比例
    //     minKillSpeed / maxKillSpeed → 速度低于/高于此值时粒子被销毁
    //
    //   🎯 碰撞形状管理（Planes 模式）：
    //     AddPlane() / RemovePlane() / SetPlane() / GetPlane()
    //     planeCount → 碰撞平面数量
    //     （Planes 模式使用 Transform 作为无限平面）
    //
    //   🎯 碰撞设置：
    //     collidesWith → 碰撞检测的层级掩码
    //     maxCollisionShapes → 最大碰撞形状数量限制
    //     quality → 碰撞检测精度（Low/Medium/High）
    //     voxelSize → 体素化精度（用于 World 模式碰撞检测）
    //     radiusScale → 粒子碰撞半径的缩放
    //     enableDynamicColliders → 是否检测动态 Collider
    //
    //   🎯 碰撞反馈：
    //     sendCollisionMessages → 是否发送碰撞消息（OnParticleCollision 回调）
    //     colliderForce → 碰撞时对 Collider 施加的力
    //     multiplyColliderForceByCollisionAngle → 力是否受碰撞角度影响
    //     multiplyColliderForceByParticleSpeed → 力是否受粒子速度影响
    //     multiplyColliderForceByParticleSize → 力是否受粒子大小影响
    //
    // 💡 碰撞 vs 触发器（Trigger）：
    //   CollisionModule → 粒子会反弹/衰减（物理交互）
    //   TriggerModule → 粒子穿过触发器（仅检测进入/离开/停留）
    //
    // ⚠️ 性能警告：
    //   粒子碰撞检测非常消耗性能！
    //   - 建议降低 maxCollisionShapes 和 voxelSize
    //   - 使用简单的碰撞体（球体/盒体优先）
    //   - 限制碰撞的层级范围（collidesWith）
    //   - Low 品质模式使用更粗略的检测（更快但不精确）
    // ================================================================
        public partial struct CollisionModule
        {
            internal CollisionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemCollisionType type { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemCollisionMode mode { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve dampen { get => dampenBlittable; set => dampenBlittable = value; }
            [NativeName("Dampen")] private extern MinMaxCurveBlittable dampenBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float dampenMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve bounce { get => bounceBlittable; set => bounceBlittable = value; }
            [NativeName("Bounce")] private extern MinMaxCurveBlittable bounceBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float bounceMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve lifetimeLoss { get => lifetimeLossBlittable; set => lifetimeLossBlittable = value; }
            [NativeName("LifetimeLoss")] private extern MinMaxCurveBlittable lifetimeLossBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float lifetimeLossMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float minKillSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float maxKillSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            extern public LayerMask collidesWith { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool enableDynamicColliders { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int maxCollisionShapes { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemCollisionQuality quality { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float voxelSize { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radiusScale { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool sendCollisionMessages { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float colliderForce { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyColliderForceByCollisionAngle { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyColliderForceByParticleSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyColliderForceByParticleSize { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public void AddPlane(Transform transform);
            [NativeMethod(ThrowsException = true)]
            extern public void RemovePlane(int index);
            public void RemovePlane(Transform transform) { RemovePlaneObject(transform); }
            [NativeMethod(ThrowsException = true)]
            extern private void RemovePlaneObject(Transform transform);
            [NativeMethod(ThrowsException = true)]
            extern public void SetPlane(int index, Transform transform);
            [NativeMethod(ThrowsException = true)]
            extern public Transform GetPlane(int index);
            [NativeMethod(ThrowsException = true)]
            extern public int planeCount { get; }

            [Obsolete("enableInteriorCollisions property is deprecated and is no longer required and has no effect on the particle system.", false)]
            extern public bool enableInteriorCollisions { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 TriggerModule — 触发器模块（检测粒子进出区域）
        //
        // 📌 检测粒子与 Trigger Collider 的交互状态：
        //
    //   🎯 四种交互状态及其动作（ParticleSystemOverlapAction）：
    //     inside  → 粒子在触发器内部时的动作
    //     outside → 粒子在触发器外部时的动作
    //     enter   → 粒子刚进入触发器时的动作
    //     exit    → 粒子刚离开触发器时的动作
    //
    //   动作类型（ParticleSystemOverlapAction）：
    //     Ignore  → 不做任何处理
    //     Kill    → 销毁粒子
    //     Callback → 触发 OnParticleTrigger 回调
    //     Eat      → "吞噬"粒子（阻止其通过）
    //
    //   🎯 碰撞器管理：
    //     AddCollider() / RemoveCollider() / SetCollider() / GetCollider()
    //     colliderCount → 触发器碰撞器数量
    //     colliderQueryMode → 如何查询碰撞器信息
    //     radiusScale → 粒子在触发器中的有效半径缩放
    //
    // 💡 与 CollisionModule 的区别：
    //   CollisionModule → 物理反弹（粒子速度改变方向）
    //   TriggerModule → 逻辑检测（进入/离开区域时执行代码）
    //
    // 🎮 使用示例：
    //   // 在 Inspector 中设置 enter = Callback
    //   // 然后在脚本中监听事件：
    //   void OnParticleTrigger()
    //   {
    //       var particles = new List<ParticleSystem.Particle>();
    //       int entered = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, particles);
    //       for (int i = 0; i < entered; i++)
    //       {
    //           // 处理进入触发器的粒子...
    //       }
    //   }
    // ================================================================
        public partial struct TriggerModule
        {
            internal TriggerModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemOverlapAction inside { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemOverlapAction outside { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemOverlapAction enter { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemOverlapAction exit { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemColliderQueryMode colliderQueryMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radiusScale { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public void AddCollider(Component collider);
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveCollider(int index);
            public void RemoveCollider(Component collider) { RemoveColliderObject(collider); }
            [NativeMethod(ThrowsException = true)]
            extern private void RemoveColliderObject(Component collider);
            [NativeMethod(ThrowsException = true)]
            extern public void SetCollider(int index, Component collider);
            [NativeMethod(ThrowsException = true)]
            extern public Component GetCollider(int index);
            [NativeMethod(ThrowsException = true)]
            extern public int colliderCount { get; }
        }

        // ================================================================
        // 🎯 SubEmittersModule — 子发射器模块（粒子触发粒子）
        //
        // 📌 让一个粒子系统的粒子可以触发另一个粒子系统的发射：
        //
    //   🎯 触发类型（ParticleSystemSubEmitterType）：
    //     Birth      → 子粒子出生时触发（最常用）
    //     Collision  → 父粒子碰撞时触发
    //     Death      → 父粒子死亡时触发
    //
    //   🎯 子发射器属性（ParticleSystemSubEmitterProperties）：
    //     InheritNothing   → 子粒子完全独立
    //     InheritVelocity  → 子粒子继承父粒子速度
    //     InheritLifetime  → 子粒子继承父粒子剩余生命周期
    //     InheritSize      → 子粒子继承父粒子大小
    //     （可以组合使用，如 InheritVelocity | InheritSize）
    //
    //   🎯 管理方法：
    //     AddSubEmitter() → 添加子发射器
    //     RemoveSubEmitter() → 移除子发射器
    //     SetSubEmitterSystem/Type/Properties/EmitProbability → 修改子发射器参数
    //     GetSubEmitterSystem/Type/Properties/EmitProbability → 查询子发射器参数
    //     subEmittersCount → 子发射器数量
    //
    // 💡 emitProbability（发射概率 0~1）：
    //   不是每个父粒子都会触发子发射器，而是按概率触发。
    //   设为 0.1 表示只有 10% 的父粒子会触发子发射器。
    //
    // 🎮 经典子发射器链：
    //   1. 爆炸粒子系统（Birth → 子发射器 A）
    //   2. 子发射器 A：碎片粒子（Collision → 子发射器 B）
    //   3. 子发射器 B：地面火花（Death → 子发射器 C）
    //   4. 子发射器 C：烟雾消散
    //   形成"爆炸→碎片→碰撞火花→烟雾"的视觉链。
    //
    // ⚠️ 子发射器嵌套层数有限制，过深的嵌套会影响性能。
    // ================================================================
        public partial struct SubEmittersModule
        {
            internal SubEmittersModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int subEmittersCount { get; }

            [NativeMethod(ThrowsException = true)]
            extern public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties, float emitProbability);
            public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties) { AddSubEmitter(subEmitter, type, properties, 1.0f); }
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveSubEmitter(int index);
            public void RemoveSubEmitter(ParticleSystem subEmitter) { RemoveSubEmitterObject(subEmitter); }
            [NativeMethod(ThrowsException = true)]
            extern private void RemoveSubEmitterObject(ParticleSystem subEmitter);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterSystem(int index, ParticleSystem subEmitter);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterType(int index, ParticleSystemSubEmitterType type);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterProperties(int index, ParticleSystemSubEmitterProperties properties);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterEmitProbability(int index, float emitProbability);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystem GetSubEmitterSystem(int index);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemSubEmitterType GetSubEmitterType(int index);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemSubEmitterProperties GetSubEmitterProperties(int index);
            [NativeMethod(ThrowsException = true)]
            extern public float GetSubEmitterEmitProbability(int index);
        }

        // ================================================================
        // 🎯 TextureSheetAnimationModule — 纹理图集动画模块（翻页动画）
        //
        // 📌 让粒子播放逐帧的精灵动画（Flipbook 动画）：
        //
    //   🎯 动画模式：
    //     mode → Grid（网格划分）或 Sprites（自定义精灵列表）
    //     numTilesX / numTilesY → 纹理图集的行列数
    //     fps → 播放帧率
    //     animation → WholeSheet（整张图集）或 SingleRow（只播放单行）
    //     rowMode → Random（随机行）或 Custom（指定行）
    //
    //   🎯 帧控制：
    //     frameOverTime → 随粒子生命周期播放的帧（曲线控制速度）
    //     startFrame → 起始帧（随机范围）
    //     cycleCount → 动画循环次数
    //     timeMode → 基于粒子年龄 或 基于粒子速度 来驱动动画
    //     speedRange → 速度驱动模式下的速度映射范围
    //
    //   🎯 Sprites 模式：
    //     AddSprite() / RemoveSprite() / SetSprite() / GetSprite()
    //     spriteCount → 自定义精灵数量
    //     （可以使用任意精灵，不必遵循网格布局）
    //
    // 💡 翻页动画是粒子特效的重要组成部分：
    //   - 火焰精灵序列 → 真实的火焰燃烧动画
    //   - 爆炸精灵序列 → 爆炸效果的逐帧播放
    //   - 烟雾精灵序列 → 翻滚的烟雾效果
    //   - 魔法符号序列 → 施法时的魔法符号旋转
    //
    // 🎮 使用技巧：
    //   frameOverTime 设为常量 0 → 所有粒子播放相同的动画帧
    //   frameOverTime 从 0 到 1 → 动画在粒子一生中完整播放一次
    //   cycleCount = 3 → 动画在粒子一生中重复 3 次
    // ================================================================
        public partial struct TextureSheetAnimationModule
        {
            internal TextureSheetAnimationModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemAnimationMode mode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemAnimationTimeMode timeMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float fps { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int numTilesX { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int numTilesY { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemAnimationType animation { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemAnimationRowMode rowMode { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve frameOverTime { get => frameOverTimeBlittable; set => frameOverTimeBlittable = value; }
            [NativeName("FrameOverTime")] private extern MinMaxCurveBlittable frameOverTimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float frameOverTimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startFrame { get => startFrameBlittable; set => startFrameBlittable = value; }
            [NativeName("StartFrame")] private extern MinMaxCurveBlittable startFrameBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startFrameMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int cycleCount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int rowIndex { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Rendering.UVChannelFlags uvChannelMask { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int spriteCount { get; }
            extern public Vector2 speedRange { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public void AddSprite(Sprite sprite);
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveSprite(int index);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSprite(int index, Sprite sprite);
            [NativeMethod(ThrowsException = true)]
            extern public Sprite GetSprite(int index);
        }

        // ================================================================
        // 🎯 LightsModule — 灯光模块（粒子携带光源）
        //
        // 📌 让部分粒子自动携带动态光源：
        //
    //   light → 使用的 Light 组件模板
    //   ratio → 有多大比例的粒子携带光源（0~1）
    //   useRandomDistribution → 是否随机选择哪些粒子带光源
    //   useParticleColor → 是否使用粒子颜色作为光源颜色
    //   sizeAffectsRange → 粒子大小是否影响光照范围
    //   alphaAffectsIntensity → 粒子透明度是否影响光照强度
    //
    //   range / intensity → 光照范围和强度曲线
    //   maxLights → 最大光源数量上限
    //
    // 💡 灯光粒子的性能影响非常大：
    //   每个动态光源都会增加渲染开销（尤其是前向渲染路径下）。
    //   maxLights 是重要的性能阀值，通常设为较小值（如 4~8）。
    //
    // 🎮 常见用法：
    //   - 火焰粒子 → 携带橙色点光源，照亮周围环境
    //   - 魔法粒子 → 携带紫色光源，产生魔法辉光
    //   - 闪电粒子 → 搭配高强度光源模拟闪光效果
    //
    // ⚠️ 性能警告：
    //   在延迟渲染（Deferred Rendering）路径中，
    //   动态光源的开销比前向渲染小很多。
    //   移动端/低端设备上慎用此模块。
    // ================================================================
        public partial struct LightsModule
        {
            internal LightsModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float ratio { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useRandomDistribution { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Light light { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useParticleColor { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool sizeAffectsRange { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool alphaAffectsIntensity { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve range { get => rangeBlittable; set => rangeBlittable = value; }
            [NativeName("Range")] private extern MinMaxCurveBlittable rangeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float rangeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve intensity { get => intensityBlittable; set => intensityBlittable = value; }
            [NativeName("Intensity")] private extern MinMaxCurveBlittable intensityBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float intensityMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int maxLights { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 TrailModule — 拖尾模块（粒子留下尾迹）
        //
        // 📌 让粒子在运动时留下可见的尾迹轨迹：
        //
    //   🎯 拖尾控制：
    //     mode → Particles（每个粒子独立拖尾）或 Ribbon（所有粒子连成丝带）
    //     ratio → 有多大比例的粒子产生拖尾（0~1）
    //     lifetime → 拖尾的存活时间（拖尾长度的控制因素）
    //     minVertexDistance → 拖尾顶点的最小间距（控制精度）
    //
    //   🎯 拖尾外观：
    //     widthOverTrail → 拖尾宽度曲线（从粒子端到末端的宽度变化）
    //     colorOverTrail → 拖尾颜色曲线
    //     colorOverLifetime → 拖尾颜色随粒子年龄变化
    //     inheritParticleColor → 是否继承粒子颜色
    //     dieWithParticles → 粒子死亡时拖尾是否也消失
    //     sizeAffectsWidth → 粒子大小是否影响拖尾宽度
    //     sizeAffectsLifetime → 粒子大小是否影响拖尾寿命
    //
    //   🎯 纹理与光照：
    //     textureMode → 拖尾的纹理映射方式
    //     textureScale → 拖尾纹理缩放
    //     generateLightingData → 是否生成法线/切线数据用于光照
    //
    //   🎯 Ribbon（丝带）模式特有：
    //     ribbonCount → 丝带数量
    //     splitSubEmitterRibbons → 子发射器丝带是否分离
    //     attachRibbonsToTransform → 丝带是否附着到 Transform
    //     worldSpace → 拖尾是否在世界空间中生成
    //
    // 💡 Particles vs Ribbon：
    //   Particles 模式：每个粒子独立产生自己的拖尾（如萤火虫尾迹）
    //   Ribbon 模式：将所有粒子连成一条丝带（如光剑效果、技能轨迹）
    //
    // 🎮 常见用法：
    //   - 火焰粒子 + 拖尾 → 火炬效果
    //   - 速度极快的粒子 + 拖尾 → 流星/激光效果
    //   - Ribbon 模式 → 龙卷风/光剑/丝带特效
    //
    // ⚡ 拖尾的渲染开销：
    //   拖尾会生成额外的网格几何体（三角形），
    //   minVertexDistance 太小会导致大量顶点，
    //   建议设置为粒子大小的 1/4~1/2。
    // ================================================================
        public partial struct TrailModule
        {
            internal TrailModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemTrailMode mode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float ratio { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve lifetime { get => lifetimeBlittable; set => lifetimeBlittable = value; }
            [NativeName("Lifetime")] private extern MinMaxCurveBlittable lifetimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float lifetimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float minVertexDistance { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemTrailTextureMode textureMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 textureScale { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool worldSpace { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool dieWithParticles { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool sizeAffectsWidth { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool sizeAffectsLifetime { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool inheritParticleColor { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient colorOverLifetime { get => colorOverLifetimeBlittable; set => colorOverLifetimeBlittable = value; }
            [NativeName("ColorOverLifetime")] private extern MinMaxGradientBlittable colorOverLifetimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve widthOverTrail { get => widthOverTrailBlittable; set => widthOverTrailBlittable = value; }
            [NativeName("WidthOverTrail")] private extern MinMaxCurveBlittable widthOverTrailBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float widthOverTrailMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient colorOverTrail { get => colorOverTrailBlittable; set => colorOverTrailBlittable = value; }
            [NativeName("ColorOverTrail")] private extern MinMaxGradientBlittable colorOverTrailBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public bool generateLightingData { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int ribbonCount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float shadowBias { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool splitSubEmitterRibbons { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool attachRibbonsToTransform { get; [NativeMethod(ThrowsException = true)] set; }
        }

        // ================================================================
        // 🎯 CustomDataModule — 自定义数据模块（Shader 数据桥接）
        //
        // 📌 为每个粒子附加自定义的 Vector4 数据，可通过 Shader 读取：
        //
    //   🔧 两个数据流：
    //     Custom1 → 第一组自定义数据（最多 4 个 float 分量）
    //     Custom2 → 第二组自定义数据（最多 4 个 float 分量）
    //
    //   🔧 每个流的配置：
    //     SetMode() → 数据填充模式
    //       ParticleSystemCustomDataMode.Stream → 按曲线自动填充
    //       ParticleSystemCustomDataMode.Constant → 使用固定值
    //     SetVectorComponentCount() → 使用多少个分量（1~4）
    //     SetVector() / GetVector() → 设置/获取各分量的曲线
    //     SetColor() / GetColor() → 颜色模式的设置/获取
    //
    // 💡 工作原理：
    //   每帧引擎根据你设置的曲线/颜色自动填充每个粒子的数据，
    //   然后这些数据作为自定义顶点属性传递给 Shader。
    //   你在 Shader 中通过TEXCOORD 通道读取这些数据。
    //
    // 🎮 典型用法：
    //   - 传递"年龄百分比"给 Shader，控制 UV 动画
    //   - 传递自定义扭曲强度，让 Shader 做顶点偏移
    //   - 传递随机值给 Shader，让每个粒子有独特的视觉效果
    //   - 配合 ParticleSystemRenderer 的 CustomData 属性使用
    //
    // ⚡ 这是连接粒子系统和 Shader 的强大桥梁：
    //   通过 CustomData，你可以在不修改粒子系统代码的情况下，
    //   将任意数据从 C# 传递到 GPU Shader 中。
    // ================================================================
        public partial struct CustomDataModule
        {
            internal CustomDataModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public void SetMode(ParticleSystemCustomData stream, ParticleSystemCustomDataMode mode);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemCustomDataMode GetMode(ParticleSystemCustomData stream);
            [NativeMethod(ThrowsException = true)]
            extern public void SetVectorComponentCount(ParticleSystemCustomData stream, int count);
            [NativeMethod(ThrowsException = true)]
            extern public int GetVectorComponentCount(ParticleSystemCustomData stream);

            public void SetVector(ParticleSystemCustomData stream, int component, MinMaxCurve curve)
            {
                SetVectorInternal(stream, component, MinMaxCurveBlittable.FromMixMaxCurve(curve));
            }

            [NativeMethod(ThrowsException = true)]
            private extern void SetVectorInternal(ParticleSystemCustomData stream, int component, MinMaxCurveBlittable curve);

            public MinMaxCurve GetVector(ParticleSystemCustomData stream, int component)
            {
                return MinMaxCurveBlittable.ToMinMaxCurve(GetVectorInternal(stream, component));
            }
            [NativeMethod(ThrowsException = true)]
            private extern MinMaxCurveBlittable GetVectorInternal(ParticleSystemCustomData stream, int component);

            public void SetColor(ParticleSystemCustomData stream, MinMaxGradient gradient)
            {
                SetColorInternal(stream, MinMaxGradientBlittable.FromMixMaxGradient(gradient));
            }
            [NativeMethod(ThrowsException = true)]
            private extern void SetColorInternal(ParticleSystemCustomData stream, MinMaxGradientBlittable gradient);

            public MinMaxGradient GetColor(ParticleSystemCustomData stream)
            {
                return MinMaxGradientBlittable.ToMinMaxGradient(GetColorInternal(stream));
            }
            [NativeMethod(ThrowsException = true)]
            extern private MinMaxGradientBlittable GetColorInternal(ParticleSystemCustomData stream);
        }

        // ================================================================
        // 🎯 模块访问器 —— 所有模块的统一入口
        //
        // 📌 通过这些属性可以访问 ParticleSystem 的所有功能模块：
        //
        // 📌 模块完整列表（共 23 个模块）：
        //   ┌────────────────────────────┬─────────────────────────────────────┐
        //   │ 模块属性名                    │ 功能说明                            │
        //   ├────────────────────────────┼─────────────────────────────────────┤
        //   │ main                       │ 主模块：基础粒子属性                  │
        //   │ emission                   │ 发射模块：速率和 Burst               │
        //   │ shape                      │ 形状模块：发射区域                    │
        //   │ velocityOverLifetime       │ 速度随时间变化                       │
        //   │ limitVelocityOverLifetime  │ 速度限制/阻力                        │
        //   │ inheritVelocity            │ 继承发射器速度                       │
        //   │ lifetimeByEmitterSpeed     │ 根据发射器速度调整生命周期             │
        //   │ forceOverLifetime          │ 生命期内持续施力                      │
        //   │ colorOverLifetime          │ 颜色随时间变化                       │
        //   │ colorBySpeed               │ 颜色随速度变化                       │
        //   │ sizeOverLifetime           │ 大小随时间变化                       │
        //   │ sizeBySpeed                │ 大小随速度变化                       │
        //   │ rotationOverLifetime       │ 旋转随时间变化                       │
        //   │ rotationBySpeed            │ 旋转随速度变化                       │
        //   │ externalForces             │ 外部力场影响                         │
        //   │ noise                      │ 噪声扰动                            │
        //   │ collision                  │ 碰撞                                │
        //   │ trigger                    │ 触发器检测                           │
        //   │ subEmitters                │ 子发射器                             │
        //   │ textureSheetAnimation      │ 纹理图集动画（翻页）                  │
        //   │ lights                     │ 粒子灯光                             │
        //   │ trails                     │ 拖尾                                │
        //   │ customData                 │ 自定义数据（Shader 桥接）              │
        //   └────────────────────────────┴─────────────────────────────────────┘
        //
        // 💡 每次访问属性会创建一个新的 struct 实例，
        //   但因为 struct 只持有引用指针，这是轻量操作。
        //   不需要缓存模块引用（不像 Component 那样需要缓存）。
        //
        // ⚡ 模块间的数据流：
        //   Emission → Shape → Main（初始参数） → Velocity/Force/Noise（运动）→ 
        //   Size/Color/Rotation（外观变化）→ Collision/Trigger（物理交互）→ 
        //   SubEmitters（链式触发）→ Renderer（最终渲染）
        // ================================================================
        // Module Accessors
        public MainModule main { get { return new MainModule(this); } }
        public EmissionModule emission { get { return new EmissionModule(this); } }
        public ShapeModule shape { get { return new ShapeModule(this); } }
        public VelocityOverLifetimeModule velocityOverLifetime { get { return new VelocityOverLifetimeModule(this); } }
        public LimitVelocityOverLifetimeModule limitVelocityOverLifetime { get { return new LimitVelocityOverLifetimeModule(this); } }
        public InheritVelocityModule inheritVelocity { get { return new InheritVelocityModule(this); } }
        public LifetimeByEmitterSpeedModule lifetimeByEmitterSpeed { get { return new LifetimeByEmitterSpeedModule(this); } }
        public ForceOverLifetimeModule forceOverLifetime { get { return new ForceOverLifetimeModule(this); } }
        public ColorOverLifetimeModule colorOverLifetime { get { return new ColorOverLifetimeModule(this); } }
        public ColorBySpeedModule colorBySpeed { get { return new ColorBySpeedModule(this); } }
        public SizeOverLifetimeModule sizeOverLifetime { get { return new SizeOverLifetimeModule(this); } }
        public SizeBySpeedModule sizeBySpeed { get { return new SizeBySpeedModule(this); } }
        public RotationOverLifetimeModule rotationOverLifetime { get { return new RotationOverLifetimeModule(this); } }
        public RotationBySpeedModule rotationBySpeed { get { return new RotationBySpeedModule(this); } }
        public ExternalForcesModule externalForces { get { return new ExternalForcesModule(this); } }
        public NoiseModule noise { get { return new NoiseModule(this); } }
        public CollisionModule collision { get { return new CollisionModule(this); } }
        public TriggerModule trigger { get { return new TriggerModule(this); } }
        public SubEmittersModule subEmitters { get { return new SubEmittersModule(this); } }
        public TextureSheetAnimationModule textureSheetAnimation { get { return new TextureSheetAnimationModule(this); } }
        public LightsModule lights { get { return new LightsModule(this); } }
        public TrailModule trails { get { return new TrailModule(this); } }
        public CustomDataModule customData { get { return new CustomDataModule(this); } }
    }
}
