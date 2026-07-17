// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 ParticleSystem — Unity 粒子系统核心绑定
//
// 📌 本文件职责：
//   这是 ParticleSystem 组件的 C# 绑定层（bindings），
//   将 C++ 原生粒子系统引擎的功能暴露给 C# 脚本层。
//
// 🔑 核心架构：
//   粒子系统的实际计算完全在 C++ 原生端运行（高性能），
//   C# 层仅提供属性读写和方法调用的桥接接口。
//   这就是为什么大量成员标记为 extern —— 它们直接调用 C++ 实现。
//
// 💡 ParticleSystem vs ParticleSystemModules.bindings.cs：
//   本文件：粒子系统根类 —— 播放控制、粒子数据读写、子发射器触发
//   Modules 文件：各功能模块 —— Main/Emission/Shape/Velocity/Collision 等
//   两者共同组成 ParticleSystem 的完整 API（partial class 拆分）。
//
// 🎮 粒子系统能做什么：
//   烟雾、火焰、爆炸、魔法特效、雨雪、喷泉、
//   拖尾光效、碎片飞溅、技能特效等等几乎所有视觉特效。
//
// ⚡ 性能关键点：
//   - GetParticles/SetParticles 会跨 C#/C++ 边界，频繁调用有开销
//   - 原生端粒子模拟使用 SIMD 和多线程优化
//   - 大量粒子时优先使用 NativeArray 重载（避免 GC 分配）
//   - 使用 Jobs System 可以在 C# 侧安全地批量操作粒子
//
// 📍 对应 C++ 头文件：
//   Modules/ParticleSystem/ParticleSystem.h
//   Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h
// ================================================================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.ParticleSystemJobs;

namespace UnityEngine
{
    // ================================================================
    // 🎯 ParticleSystem — 粒子系统组件（C# 绑定层）
    //
    // 📌 继承关系：
    //   ParticleSystem -> Component -> Object
    //   它是一个挂载在 GameObject 上的组件，自带 Transform。
    //
    // 🔑 核心设计理念 —— 模块化架构：
    //   ParticleSystem 本身只负责播放控制和粒子数据访问。
    //   所有视觉/物理行为由独立的"模块"（Module）控制：
    //     MainModule           → 基础属性（速度/大小/颜色/重力/生命周期）
    //     EmissionModule       → 发射速率、Burst 爆发
    //     ShapeModule          → 发射形状（球体/锥体/盒体/网格/贴图...）
    //     VelocityOverLifetime → 速度变化
    //     SizeOverLifetime     → 大小变化
    //     ColorOverLifetime    → 颜色变化
    //     CollisionModule      → 碰撞反弹
    //     SubEmittersModule    → 子发射器（粒子触发粒子）
    //     TrailModule          → 拖尾
    //     NoiseModule          → 噪声扰动
    //     ... 等等
    //
    // ⚡ 模块访问方式：
    //   ParticleSystem ps = GetComponent<ParticleSystem>();
    //   var main = ps.main;              // 获取 MainModule
    //   main.startSpeed = 5f;            // 设置初始速度
    //   var emission = ps.emission;      // 获取 EmissionModule
    //   emission.SetBursts(new Burst[] { new Burst(0f, 30) });
    //
    // 💡 模块是 struct（值类型），每次访问属性都会创建新实例。
    //    但它们内部只是持有对 ParticleSystem 的引用（一个 IntPtr），
    //    操作都是直接读写原生内存，开销极小。
    //
    // ⚠️ 重要：sealed class
    //   ParticleSystem 被标记为 sealed，不能被继承。
    //   所有扩展通过模块和 C# 扩展方法实现。
    //
    // 🎮 常见使用模式：
    //   1. 编辑器中预设参数 → 运行时 Play()/Stop() 控制
    //   2. 运行时动态修改模块参数（如变色、变速）
    //   3. 通过 Emit() 手动发射粒子
    //   4. 通过 GetParticles() 自定义粒子行为（需要 Jobs 或手动更新）
    //   5. 子发射器实现链式特效（如爆炸→碎片→火花）
    // ================================================================
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemGeometryJob.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    public sealed partial class ParticleSystem : Component
    {
        // ================================================================
        // 🎯 粒子系统状态属性
        //
        // 📌 这些属性反映粒子系统当前的播放状态。
        //   isPlaying  → 正在播放（可能暂停中）
        //   isEmitting  → 正在发射新粒子
        //   isStopped   → 已停止（粒子可能还在渲染但不再发射）
        //   isPaused    → 已暂停
        //
        // 💡 SyncJobs(false) 的含义：
        //   表示读取属性前不强制同步 Job 系统的异步计算。
        //   这是"快速但可能略有延迟"的读取方式。
        //   对于状态检查来说，这个延迟完全可以接受。
        //
        // ⚡ particleCount —— 当前存活的粒子总数
        //   非常有用的运行时统计，可用于性能监控和动态调整。
        //   ⚠️ 注意：此属性每次调用都遍历原生数据，不要在 Update 中频繁调用。
        // ================================================================
        // Properties
        extern public bool isPlaying
        {
            [NativeName("SyncJobs(false)->IsPlaying")] get;
        }
        extern public bool isEmitting
        {
            [NativeName("SyncJobs(false)->IsEmitting")] get;
        }
        extern public bool isStopped
        {
            [NativeName("SyncJobs(false)->IsStopped")] get;
        }
        extern public bool isPaused
        {
            [NativeName("SyncJobs(false)->IsPaused")] get;
        }
        extern public int particleCount
        {
            [NativeName("SyncJobs(false)->GetParticleCount")] get;
        }

        // ================================================================
        // 🎯 时间控制与随机种子
        //
        // 📌 time —— 当前播放时间（秒）
        //   可读写，允许你快进、倒退或重置粒子系统的播放位置。
        //   例如：ps.time = 0f 等同于从头播放。
        //
        // 📌 totalTime —— 播放总时长（只读）
        //   等于 MainModule.duration，即一个完整循环的时长。
        //
        // 📌 randomSeed —— 随机种子
        //   控制粒子系统的随机行为（发射位置偏移、速度变化等）。
        //   固定种子 = 每次播放效果一致。
        //   自动种子 = 每次播放效果略有不同（更自然）。
        //
        // 💡 useAutoRandomSeed：
        //   true 时引擎自动为每个实例生成唯一随机种子，
        //   false 时使用你手动设置的 randomSeed 值。
        //   在多人同步场景中，固定种子可以确保所有客户端看到相同的特效。
        // ================================================================
        extern public float time
        {
            [NativeName("SyncJobs(false)->GetSecPosition")]
            get;
            [NativeName("SyncJobs(false)->SetSecPosition")]
            set;
        }

        extern public float totalTime
        {
            [NativeName("SyncJobs(false)->GetTotalSecPosition")]
            get;
        }

        extern public UInt32 randomSeed
        {
            [NativeName("GetRandomSeed")]
            get;
            [NativeName("SyncJobs(false)->SetRandomSeed")]
            set;
        }

        extern public bool useAutoRandomSeed
        {
            [NativeName("GetAutoRandomSeed")]
            get;
            [NativeName("SyncJobs(false)->SetAutoRandomSeed")]
            set;
        }

        // proceduralSimulationSupported — 是否支持程序化模拟
        // 💡 程序化模拟 = 不依赖预计算的发射时间表，
        //    而是根据外部输入（如速度）实时计算粒子行为。
        //    用于 VR/AR 中的交互式粒子效果。
        extern public bool proceduralSimulationSupported
        {
            get;
        }

        // ================================================================
        // 🎯 粒子数据辅助方法（内部使用）
        //
        // 📌 这些方法获取单个粒子的当前状态值。
        //   它们是 internal 方法，主要供 ParticleSystemRenderer 等
        //   内部组件调用，开发者通常不需要直接使用。
        //
        // 💡 获取粒子当前大小/颜色时，需要综合考虑：
        //   - 初始值（startSize/startColor）
        //   - 随时间变化（SizeOverLifetime/ColorOverLifetime）
        //   - 随速度变化（SizeBySpeed/ColorBySpeed）
        //   这些辅助方法已经完成了所有模块的叠加计算。
        // ================================================================
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleCurrentSize", HasExplicitThis = true)]
        extern internal float GetParticleCurrentSize(ref Particle particle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleCurrentSize3D", HasExplicitThis = true)]
        extern internal Vector3 GetParticleCurrentSize3D(ref Particle particle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleCurrentColor", HasExplicitThis = true)]
        extern internal Color32 GetParticleCurrentColor(ref Particle particle);

        // Mesh index helper
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleMeshIndex", HasExplicitThis = true)]
        extern internal int GetParticleMeshIndex(ref Particle particle);

        // ================================================================
        // 🎯 粒子数据读写 —— GetParticles / SetParticles
        //
        // 📌 这是与粒子系统交互的核心 API：
        //   GetParticles() → 将所有存活粒子的数据读入数组
        //   SetParticles() → 将数组中的粒子数据写回系统
        //
        // ⚡ 性能建议：
        //   - 数组版本：Particle[] 会触发 GC 分配（每帧调用有压力）
        //   - NativeArray 版本：零 GC 分配，适合高频读写
        //   - 推荐在 Start() 中预分配数组，复用避免 GC
        //
        // 💡 Particle 结构体包含每个粒子的完整数据：
        //   position, velocity, lifetime, startLifetime,
        //   startSize, startColor, rotation, angularVelocity,
        //   randomSeed, startLifetime 等等
        //
        // 🎮 使用示例：
        //   ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
        //   int count = ps.GetParticles(particles);
        //   for (int i = 0; i < count; i++)
        //   {
        //       particles[i].velocity += customForce;  // 自定义力
        //   }
        //   ps.SetParticles(particles, count);
        // ================================================================
        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticles", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetParticles([In, Out] Particle[] particles, int size, int offset);
        public void SetParticles([Out] Particle[] particles, int size) { SetParticles(particles, size, 0); }
        public void SetParticles([Out] Particle[] particles) { SetParticles(particles, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticlesWithNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetParticlesWithNativeArray(IntPtr particles, int particlesLength, int size, int offset);
        public void SetParticles([Out] NativeArray<Particle> particles, int size, int offset) { unsafe { SetParticlesWithNativeArray((IntPtr)particles.GetUnsafeReadOnlyPtr(), particles.Length, size, offset); } }
        public void SetParticles([Out] NativeArray<Particle> particles, int size) { SetParticles(particles, size, 0); }
        public void SetParticles([Out] NativeArray<Particle> particles) { SetParticles(particles, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticles", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetParticles([NotNull][Out] Particle[] particles, int size, int offset);
        public int GetParticles([Out] Particle[] particles, int size) { return GetParticles(particles, size, 0); }
        public int GetParticles([Out] Particle[] particles) { return GetParticles(particles, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticlesWithNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetParticlesWithNativeArray(IntPtr particles, int particlesLength, int size, int offset);
        public int GetParticles([Out] NativeArray<Particle> particles, int size, int offset) { unsafe { return GetParticlesWithNativeArray((IntPtr)particles.GetUnsafePtr(), particles.Length, size, offset); } }
        public int GetParticles([Out] NativeArray<Particle> particles, int size) { return GetParticles(particles, size, 0); }
        public int GetParticles([Out] NativeArray<Particle> particles) { return GetParticles(particles, -1); }

        // ================================================================
        // 🎯 自定义粒子数据（Custom Data）
        //
        // 📌 允许你为每个粒子附加额外的 Vector4 数据（最多 4 个流）。
        //   这些数据可以通过 Shader 读取，实现高度自定义的视觉效果。
        //
        // 💡 典型用法：
        //   - 传递自定义参数到 Shader（如扭曲强度、发光值）
        //   - 存储粒子的"年龄百分比"供 Shader 使用
        //   - 结合 CustomDataModule 模块自动填充数据
        //
        // 🎮 ParticleSystemCustomData.Custom1 / Custom2 是两个可用的流。
        // ================================================================
        // Set/get custom particle data
        [FreeFunction(Name = "ParticleSystemScriptBindings::SetCustomParticleData", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetCustomParticleData([NotNull] List<Vector4> customData, ParticleSystemCustomData streamIndex);
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetCustomParticleData", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetCustomParticleData([NotNull] List<Vector4> customData, ParticleSystemCustomData streamIndex);

        // ================================================================
        // 🎯 播放状态序列化（PlaybackState）与拖尾数据（Trails）
        //
        // 📌 PlaybackState：
        //   将粒子系统的完整内部状态打包为可序列化的结构体。
        //   用于保存/恢复粒子系统的状态（如暂停恢复、存档）。
        //
        // 📌 Trails（拖尾数据）：
        //   获取/设置粒子拖尾的几何数据（位置、生命周期、颜色等）。
        //   拖尾是由 TrailModule 生成的，在 GPU 上渲染为条带网格。
        //   SetParticlesAndTrails 允许同时更新粒子和拖尾数据。
        //
        // 💡 在粒子特效存档或网络同步场景中，
        //    可以序列化 PlaybackState 来精确还原粒子系统状态。
        // ================================================================
        extern public PlaybackState GetPlaybackState();
        extern public void SetPlaybackState(PlaybackState playbackState);

        // Set/get the trail data
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetTrailData", HasExplicitThis = true)]
        extern private void GetTrailDataInternal(ref Trails trailData);
        public Trails GetTrails()
        {
            var result = new Trails();
            result.Allocate();
            GetTrailDataInternal(ref result);
            return result;
        }

        public int GetTrails(ref Trails trailData)
        {
            trailData.Allocate();
            GetTrailDataInternal(ref trailData);
            return trailData.positions.Count;
        }

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticlesAndTrailData", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetParticlesAndTrails([NotNull, Out] Particle[] particles, Trails trailData, int size, int offset);
        public void SetParticlesAndTrails([Out] Particle[] particles, Trails trailData, int size) { SetParticlesAndTrails(particles, trailData, size, 0); }
        public void SetParticlesAndTrails([Out] Particle[] particles, Trails trailData) { SetParticlesAndTrails(particles, trailData, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticlesAndTrailDataWithNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetParticlesAndTrailsWithNativeArray(IntPtr particles, Trails trailData, int particlesLength, int size, int offset);
        public void SetParticlesAndTrails([Out] NativeArray<Particle> particles, Trails trailData, int size, int offset) { unsafe { SetParticlesAndTrailsWithNativeArray((IntPtr)particles.GetUnsafeReadOnlyPtr(), trailData, particles.Length, size, offset); } }
        public void SetParticlesAndTrails([Out] NativeArray<Particle> particles, Trails trailData, int size) { SetParticlesAndTrails(particles, trailData, size, 0); }
        public void SetParticlesAndTrails([Out] NativeArray<Particle> particles, Trails trailData) { SetParticlesAndTrails(particles, trailData, -1); }         

        // ================================================================
        // 🎯 播放控制 —— Play / Pause / Stop / Clear / Simulate / IsAlive
        //
        // 📌 粒子系统的生命周期控制方法：
        //
        //   Play()        → 开始播放（从当前 time 位置继续）
        //   Pause()       → 暂停播放（粒子冻结在当前位置）
        //   Stop()        → 停止播放（可选：是否让已发射粒子消亡）
        //   Clear()       → 立即清除所有存活粒子
        //   Simulate(t)   → 模拟指定时间（不渲染，用于预热效果）
        //   IsAlive()     → 是否还"活着"（有粒子或还在发射）
        //
        // ⚡ withChildren 参数：
        //   true（默认）→ 同时控制所有子粒子系统（递归）
        //   false → 只控制当前粒子系统
        //
        // 💡 Stop() 的 stopBehavior 参数：
        //   StopEmitting（默认）→ 停止发射，但已发射的粒子继续运动到消亡
        //   StopEmittingAndClear → 停止发射并立即清除所有粒子
        //
        // 🎮 常见使用模式：
        //   ps.Play();                           // 开始播放
        //   ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);  // 优雅停止
        //   while (ps.IsAlive()) yield return null;  // 等待粒子全部消亡
        //
        // 🎯 Simulate 的妙用：
        //   Simulate(duration, false, false)  → 预热粒子系统（跳过开头效果）
        //   Simulate(0, false, true)         → 重置到第 0 帧
        // ================================================================

        // Playback
        [FreeFunction(Name = "ParticleSystemScriptBindings::Simulate", HasExplicitThis = true)]
        extern public void Simulate(float t, [DefaultValue("true")] bool withChildren, [DefaultValue("true")] bool restart, [DefaultValue("true")] bool fixedTimeStep);
        public void Simulate(float t, [DefaultValue("true")] bool withChildren, [DefaultValue("true")] bool restart) { Simulate(t, withChildren, restart, true); }
        public void Simulate(float t, [DefaultValue("true")] bool withChildren) { Simulate(t, withChildren, true); }
        public void Simulate(float t) { Simulate(t, true); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::Play", HasExplicitThis = true)]
        extern public void Play([DefaultValue("true")] bool withChildren);
        public void Play() { Play(true);  }

        [FreeFunction(Name = "ParticleSystemScriptBindings::Pause", HasExplicitThis = true)]
        extern public void Pause([DefaultValue("true")] bool withChildren);
        public void Pause() { Pause(true); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::Stop", HasExplicitThis = true)]
        extern public void Stop([DefaultValue("true")] bool withChildren, [DefaultValue("ParticleSystemStopBehavior.StopEmitting")] ParticleSystemStopBehavior stopBehavior);
        public void Stop([DefaultValue("true")] bool withChildren) { Stop(withChildren, ParticleSystemStopBehavior.StopEmitting); }
        public void Stop() { Stop(true); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::Clear", HasExplicitThis = true)]
        extern public void Clear([DefaultValue("true")] bool withChildren);
        public void Clear() { Clear(true); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::IsAlive", HasExplicitThis = true)]
        extern public bool IsAlive([DefaultValue("true")] bool withChildren);
        public bool IsAlive() { return IsAlive(true); }

        // ================================================================
        // 🎯 粒子发射控制 —— Emit 与 TriggerSubEmitter
        //
        // 📌 Emit 方法：
        //   Emit(int count) → 立即发射指定数量的粒子（使用默认参数）
        //   Emit(EmitParams, count) → 使用自定义参数发射粒子
        //     EmitParams 允许你指定每个粒子的初始位置、速度、颜色、大小等
        //
        // 💡 手动发射 vs 自动发射：
        //   自动发射由 EmissionModule 控制（按速率或 Burst 自动产生粒子）
        //   手动发射通过 Emit() 实现，适合需要精确控制时机的场景
        //
        // 🎮 手动发射示例（如点击生成火花）：
        //   var emitParams = new ParticleSystem.EmitParams();
        //   emitParams.position = clickPoint;
        //   emitParams.startColor = Color.yellow;
        //   emitParams.startSize = 0.5f;
        //   ps.Emit(emitParams, 10);  // 在点击位置发射 10 个黄色粒子
        //
        // 🎯 TriggerSubEmitter —— 子发射器触发
        //   子发射器是"粒子触发粒子"的机制：
        //   - 父粒子死亡时触发子发射器
        //   - 父粒子碰撞时触发子发射器
        //   - 通过此方法手动触发
        //
        //   TriggerSubEmitter(index)                    → 对所有粒子触发
        //   TriggerSubEmitter(index, ref particle)      → 对单个粒子触发
        //   TriggerSubEmitter(index, particleList)       → 对指定粒子列表触发
        // ================================================================
        // Emission
        [RequiredByNativeCode]
        public void Emit(int count) { Emit_Internal(count); }
        [NativeName("SyncJobs()->Emit")]
        extern private void Emit_Internal(int count);

        [NativeName("SyncJobs()->EmitParticlesExternal")]
        extern public void Emit(EmitParams emitParams, int count);

        [NativeName("SyncJobs()->EmitParticleExternal")]
        extern private void EmitOld_Internal(ref ParticleSystem.Particle particle);

        // ================================================================
        // 🎯 GPU 缓冲区管理与 Job System 集成
        //
        // 📌 ResetPreMappedBufferMemory / SetMaximumPreMappedBufferCounts：
        //   控制粒子系统预映射的 GPU 缓冲区（顶点/索引缓冲区）。
        //   预映射缓冲区避免了频繁的 GPU 内存分配，提升性能。
        //   在大量粒子系统同时存在时，可能需要调整上限。
        //
        // 📌 AllocateAxisOfRotationAttribute / AllocateMeshIndexAttribute：
        //   为粒子分配自定义属性槽位：
        //   - 轴旋转属性：告诉渲染器粒子绕哪个轴旋转
        //   - 网格索引属性：当使用网格渲染时，指定每个粒子使用哪个网格
        //
        // 📌 Job System 集成：
        //   ParticleSystem 支持 Unity Jobs System，允许在 C# 侧
        //   使用多线程安全地修改粒子数据（位置、速度等）。
        //   GetManagedJobData/Handle/SetManagedJobHandle 是内部桥接方法。
        //
        // ⚡ 使用 Jobs 的优势：
        //   - 多线程并行处理粒子，充分利用 CPU 多核
        //   - 与渲染管线的 Job 形成无锁流水线
        //   - 不会阻塞主线程
        // ================================================================
        public void TriggerSubEmitter(int subEmitterIndex)
        {
            TriggerSubEmitterForAllParticles(subEmitterIndex);
        }

        public void TriggerSubEmitter(int subEmitterIndex, ref ParticleSystem.Particle particle)
        {
            TriggerSubEmitterForParticle(subEmitterIndex, particle);
        }

        public void TriggerSubEmitter(int subEmitterIndex, List<ParticleSystem.Particle> particles)
        {
            if (particles == null)
                TriggerSubEmitterForAllParticles(subEmitterIndex);
            else
                TriggerSubEmitterForParticles(subEmitterIndex, particles);
        }

        [FreeFunction(Name = "ParticleSystemScriptBindings::TriggerSubEmitterForParticle", HasExplicitThis = true)]
        extern internal void TriggerSubEmitterForParticle(int subEmitterIndex, ParticleSystem.Particle particle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::TriggerSubEmitterForParticles", HasExplicitThis = true)]
        extern private void TriggerSubEmitterForParticles(int subEmitterIndex, List<ParticleSystem.Particle> particles);

        [FreeFunction(Name = "ParticleSystemScriptBindings::TriggerSubEmitterForAllParticles", HasExplicitThis = true)]
        extern private void TriggerSubEmitterForAllParticles(int subEmitterIndex);

        [FreeFunction(Name = "ParticleSystemGeometryJob::ResetPreMappedBufferMemory")]
        extern public static void ResetPreMappedBufferMemory();

        [FreeFunction(Name = "ParticleSystemGeometryJob::SetMaximumPreMappedBufferCounts")]
        extern public static void SetMaximumPreMappedBufferCounts(int vertexBuffersCount, int indexBuffersCount);

        [NativeName("SetUsesAxisOfRotation")]
        extern public void AllocateAxisOfRotationAttribute();
        [NativeName("SetUsesMeshIndex")]
        extern public void AllocateMeshIndexAttribute();
        [NativeName("SetUsesCustomData")]
        extern public void AllocateCustomDataAttribute(ParticleSystemCustomData stream);

        extern public bool has3DParticleRotations { [NativeName("Has3DParticleRotations")] get; }
        extern public bool hasNonUniformParticleSizes { [NativeName("HasNonUniformParticleSizes")] get; }

        unsafe extern internal void* GetManagedJobData();
        extern internal JobHandle GetManagedJobHandle();
        extern internal void SetManagedJobHandle(JobHandle handle);
        [FreeFunction("ScheduleManagedJob", ThrowsException = true)]
        unsafe internal static extern JobHandle ScheduleManagedJob(ref JobsUtility.JobScheduleParameters parameters, void* additionalData);
        [NativeMethod(IsThreadSafe = true)]
        unsafe internal static extern void CopyManagedJobData(void* systemPtr, out NativeParticleData particleData);
        internal static extern bool UserJobCanBeScheduled();


        [FreeFunction(Name = "ParticleSystemEditor::SetupDefaultParticleSystemType", HasExplicitThis = true)]
        extern internal void SetupDefaultType(ParticleSystemSubEmitterType type);

        [NativeProperty("GetState()->localToWorld", TargetType = TargetType.Field)]
        extern internal Matrix4x4 localToWorldMatrix { get; }

        [NativeName("GetNoiseModule().GeneratePreviewTexture")]
        extern internal void GenerateNoisePreviewTexture(Texture2D dst);

        extern internal void CalculateEffectUIData(ref int particleCount, ref float fastestParticle, ref float slowestParticle);

        extern internal int GenerateRandomSeed();

        [FreeFunction(Name = "ParticleSystemScriptBindings::CalculateEffectUISubEmitterData", HasExplicitThis = true)]
        extern internal bool CalculateEffectUISubEmitterData(ref int particleCount, ref float fastestParticle, ref float slowestParticle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::CheckVertexStreamsMatchShader")]
        extern internal static bool CheckVertexStreamsMatchShader(bool hasTangent, bool hasColor, int texCoordChannelCount, Material material, ref bool tangentError, ref bool colorError, ref bool uvError);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetMaxTexCoordStreams")]
        extern internal static int GetMaxTexCoordStreams();

    }

    public partial struct ParticleCollisionEvent
    {
        [FreeFunction(Name = "ParticleSystemScriptBindings::InstanceIDToColliderComponent")]
        extern static private Component InstanceIDToColliderComponent(EntityId entityId);
    }

    // ================================================================
    // 🎯 ParticleSystemExtensionsImpl — 粒子物理事件的内部实现
    //
    // 📌 这个内部类封装了粒子碰撞和触发器事件的原生调用。
    //   它是 ParticleSystemCollisionEvent 和触发器事件的桥接层。
    //
    // 💡 为什么用内部类 + 扩展方法的两层结构？
    //   1. ExtensionsImpl 是 internal 类，直接调用 [FreeFunction] 原生方法
    //   2. ParticlePhysicsExtensions 是 public 静态类，提供友好的扩展方法
    //   3. 这种模式将原生绑定与公共 API 分离，便于维护
    //
    // ⚠️ 注意：
    //   粒子碰撞事件需要在 CollisionModule 中开启 sendCollisionMessages，
    //   否则 GetCollisionEvents 会返回空结果。
    //   碰撞检测有性能开销，大量粒子碰撞时需谨慎使用。
    // ================================================================
    internal class ParticleSystemExtensionsImpl
    {
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetSafeCollisionEventSize")]
        extern internal static int GetSafeCollisionEventSize([NotNull] ParticleSystem ps);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetCollisionEventsDeprecated")]
        extern internal static int GetCollisionEventsDeprecated([NotNull] ParticleSystem ps, GameObject go, [Out] ParticleCollisionEvent[] collisionEvents);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetSafeTriggerParticlesSize")]
        extern internal static int GetSafeTriggerParticlesSize([NotNull] ParticleSystem ps, int type);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetCollisionEvents")]
        extern internal static int GetCollisionEvents([NotNull] ParticleSystem ps, [NotNull] GameObject go, [NotNull] List<ParticleCollisionEvent> collisionEvents);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetTriggerParticles")]
        extern internal static int GetTriggerParticles([NotNull] ParticleSystem ps, int type, [NotNull] List<ParticleSystem.Particle> particles);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetTriggerParticlesWithData")]
        extern internal static int GetTriggerParticlesWithData([NotNull] ParticleSystem ps, int type, [NotNull] List<ParticleSystem.Particle> particles, out ParticleSystem.ColliderData colliderData);

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetTriggerParticles")]
        extern internal static void SetTriggerParticles([NotNull] ParticleSystem ps, int type, [NotNull] List<ParticleSystem.Particle> particles, int offset, int count);
    }

    // ================================================================
    // 🎯 ParticlePhysicsExtensions — 粒子物理扩展方法
    //
    // 📌 以 C# 扩展方法的形式提供粒子碰撞和触发器查询：
    //
    //   GetSafeCollisionEventSize()   → 获取碰撞事件缓冲区大小
    //   GetCollisionEvents()          → 获取粒子与某 GameObject 的碰撞事件列表
    //   GetSafeTriggerParticlesSize() → 获取触发器粒子缓冲区大小
    //   GetTriggerParticles()         → 获取处于触发区域内的粒子
    //   SetTriggerParticles()         → 设置触发器区域内的粒子状态
    //
    // 💡 碰撞事件（Collision Events）：
    //   当粒子与 Collider 碰撞时产生 ParticleCollisionEvent，
    //   包含碰撞点位置、法线方向、粒子数据和碰撞的 Collider 组件。
    //   典型用法：粒子碰到墙壁时产生溅射效果。
    //
    // 💡 触发器事件（Trigger Events）：
    //   当粒子进入/离开/停留在 Trigger Collider 内时触发。
    //   4 种触发类型：
    //     Inside  → 粒子在触发器内部
    //     Outside → 粒子在触发器外部
    //     Enter   → 粒子刚进入触发器
    //     Exit    → 粒子刚离开触发器
    //   典型用法：检测粒子是否落入某个区域并触发逻辑。
    //
    // 🎮 碰撞事件使用示例：
    //   void OnParticleCollision(GameObject other)
    //   {
    //       var events = new List<ParticleCollisionEvent>();
    //       int count = ps.GetCollisionEvents(other, events);
    //       for (int i = 0; i < count; i++)
    //       {
    //           Instantiate(sparkPrefab, events[i].intersection, Quaternion.LookRotation(events[i].normal));
    //       }
    //   }
    // ================================================================
    public static partial class ParticlePhysicsExtensions
    {
        public static int GetSafeCollisionEventSize(this ParticleSystem ps)
        {
            return ParticleSystemExtensionsImpl.GetSafeCollisionEventSize(ps);
        }

        public static int GetCollisionEvents(this ParticleSystem ps, GameObject go, List<ParticleCollisionEvent> collisionEvents)
        {
            return ParticleSystemExtensionsImpl.GetCollisionEvents(ps, go, collisionEvents);
        }

        public static int GetSafeTriggerParticlesSize(this ParticleSystem ps, ParticleSystemTriggerEventType type)
        {
            return ParticleSystemExtensionsImpl.GetSafeTriggerParticlesSize(ps, (int)type);
        }

        public static int GetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles)
        {
            return ParticleSystemExtensionsImpl.GetTriggerParticles(ps, (int)type, particles);
        }

        public static int GetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles, out ParticleSystem.ColliderData colliderData)
        {
            if (type == ParticleSystemTriggerEventType.Exit)
                throw new InvalidOperationException("Querying the collider data for the Exit event is not currently supported.");
            else if (type == ParticleSystemTriggerEventType.Outside)
                throw new InvalidOperationException("Querying the collider data for the Outside event is not supported, because when a particle is outside the collision volume, it is always outside every collider.");

            colliderData = new ParticleSystem.ColliderData();
            return ParticleSystemExtensionsImpl.GetTriggerParticlesWithData(ps, (int)type, particles, out colliderData);
        }

        public static void SetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles, int offset, int count)
        {
            if (particles == null) throw new ArgumentNullException("particles");
            if (offset >= particles.Count) throw new ArgumentOutOfRangeException("offset", "offset should be smaller than the size of the particles list.");
            if ((offset + count) >= particles.Count) throw new ArgumentOutOfRangeException("count", "offset+count should be smaller than the size of the particles list.");

            ParticleSystemExtensionsImpl.SetTriggerParticles(ps, (int)type, particles, offset, count);
        }

        public static void SetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles)
        {
            ParticleSystemExtensionsImpl.SetTriggerParticles(ps, (int)type, particles, 0, particles.Count);
        }
    }
}
