// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//Keep this namespace to be compatible with visual effect graph package 7.0.1
//There was an unexpected useless "using UnityEngine.Experimental.VFX;" in VFXMotionVector.cs
// ================================================================
// VFXManager —— VFX 全局设置与相机集成
//
// 🎯 VFXGraph 的全局管理器，控制更新频率、相机集成、批处理等
// 💡 通过 VFXManager 类访问全局设置，通过 VFXManager（Experimental）
//    提供向下兼容的命名空间占位
// ⚡ 核心功能：
//   - fixedTimeStep / maxDeltaTime：粒子更新步长与最大增量
//   - PrepareCamera / ProcessCameraCommand：将 VFX 注入渲染管线
//   - GetBatchedEffectInfo：查询实例化批处理状态
//   - FlushEmptyBatches：清理空批次释放 GPU 内存
// ================================================================

namespace UnityEngine.Experimental.VFX
{
    internal static class VFXManager
    {
    }
}

namespace UnityEngine.VFX
{
    // ================================================================
    // VFXCameraXRSettings —— VFX XR 相机设置
    //
    // 🎯 为 VR/AR 应用配置 VFX 的多视图渲染参数
    // 📌 viewTotal/viewCount/viewOffset 控制 XR 视口分配
    // ================================================================
    [RequiredByNativeCode]
    public struct VFXCameraXRSettings
    {
        public uint viewTotal;
        public uint viewCount;
        public uint viewOffset;
    }

    // ================================================================
    // VFXBatchedEffectInfo —— VFX 批处理效果信息
    //
    // 🎯 查询指定 VisualEffectAsset 的 GPU Instancing 批处理状态
    // 💡 包含活跃批次/非活跃批次/实例数/GPU内存等诊断数据
    // 📌 通过 GetBatchedEffectInfo() / GetBatchedEffectInfos() 获取
    // ================================================================
    [RequiredByNativeCode]
    public struct VFXBatchedEffectInfo
    {
        public VisualEffectAsset vfxAsset;
        public uint activeBatchCount;
        public uint inactiveBatchCount;
        public uint activeInstanceCount;
        public uint unbatchedInstanceCount;
        public uint totalInstanceCapacity;
        public uint maxInstancePerBatchCapacity;
        public ulong totalGPUSizeInBytes;
        public ulong totalCPUSizeInBytes;
    }

    // ================================================================
    // VFXBatchInfo —— VFX 单批次详细信息
    //
    // 🎯 查询指定 VisualEffectAsset 中单个 batch 的容量和活跃数
    // 💡 内部使用，通过 GetBatchInfo() 获取
    // ================================================================
    [RequiredByNativeCode]
    internal struct VFXBatchInfo
    {
        public uint capacity;
        public uint activeInstanceCount;
    }

    // ================================================================
    // VFXManager —— VFXGraph 全局管理器（主类）
    //
    // 🎯 管理 VFX 的全局设置、相机集成、批处理查询
    // 💡 全局设置：
    //   - fixedTimeStep：粒子物理步长（默认 0.02s = 50FPS）
    //   - maxDeltaTime：单帧最大增量（防止粒子"爆炸"）
    // 📌 相机集成流程：
    //   - PrepareCamera() → ProcessCameraCommand() 两步走
    //   - 支持 CullingResults 实现视锥体裁剪
    // ⚡ 批处理管理：
    //   - GetBatchedEffectInfos()：查询所有 VFX Asset 的实例化状态
    //   - FlushEmptyBatches()：清理空批次
    // ================================================================
    [RequiredByNativeCode]
    [NativeHeader("Modules/VFX/Public/VFXManager.h")]
    [NativeHeader("Modules/VFX/Public/ScriptBindings/VFXManagerBindings.h")]
    [StaticAccessor("GetVFXManager()", StaticAccessorType.Dot)]
    public static partial class VFXManager
    {
        extern public static VisualEffect[] GetComponents();
        extern internal static ScriptableObject runtimeResources { get; }

        extern public static float fixedTimeStep { get; set; }
        extern public static float maxDeltaTime { get; set; }

        extern internal static uint maxCapacity { get; set; }
        extern internal static float maxScrubTime { get; set; }
        extern internal static string renderPipeSettingsPath { get; }

        extern internal static uint batchEmptyLifetime { get; set; }

        extern internal static ScriptableObject editorResources { get; }
        extern internal static void ResyncMaterials([NotNull] VisualEffectAsset asset);
        extern internal static bool renderInSceneView { get; set; }
        // Re-initialized only on code reload (VisualEffectAssetEditorUtility static ctor).
        [AutoStaticsCleanupOnCodeReload]
        internal static bool activateVFX { get; set; }

        extern internal static void CleanupEmptyBatches(bool force = false);

        public static void FlushEmptyBatches()
        {
            CleanupEmptyBatches(true);
        }

        extern public static VFXBatchedEffectInfo GetBatchedEffectInfo([NotNull] VisualEffectAsset vfx);

        [FreeFunction(Name = "VFXManagerBindings::GetBatchedEffectInfos", HasExplicitThis = false)]
        extern public static void GetBatchedEffectInfos([NotNull][Out] List<VFXBatchedEffectInfo> infos);

        extern internal static VFXBatchInfo GetBatchInfo(VisualEffectAsset vfx, uint batchIndex);

        private static readonly VFXCameraXRSettings kDefaultCameraXRSettings = new VFXCameraXRSettings { viewTotal = 1, viewCount = 1, viewOffset = 0 };

        [Obsolete("Use explicit PrepareCamera and ProcessCameraCommand instead")]
        public static void ProcessCamera(Camera cam)
        {
            PrepareCamera(cam, kDefaultCameraXRSettings);
            Internal_ProcessCameraCommand(cam, null, kDefaultCameraXRSettings, IntPtr.Zero, IntPtr.Zero);
        }

        public static void PrepareCamera(Camera cam)
        {
            PrepareCamera(cam, kDefaultCameraXRSettings);
        }

        extern public static void PrepareCamera([NotNull] Camera cam, VFXCameraXRSettings camXRSettings);

        [Obsolete("Use ProcessCameraCommand with CullingResults to allow culling of VFX per camera")]
        public static void ProcessCameraCommand(Camera cam, CommandBuffer cmd)
        {
            Internal_ProcessCameraCommand(cam, cmd, kDefaultCameraXRSettings, IntPtr.Zero, IntPtr.Zero);
        }

        [Obsolete("Use ProcessCameraCommand with CullingResults to allow culling of VFX per camera")]
        public static void ProcessCameraCommand(Camera cam, CommandBuffer cmd, VFXCameraXRSettings camXRSettings)
        {
            Internal_ProcessCameraCommand(cam, cmd, camXRSettings, IntPtr.Zero, IntPtr.Zero);
        }

        public static void ProcessCameraCommand(Camera cam, CommandBuffer cmd, VFXCameraXRSettings camXRSettings, Rendering.CullingResults results)
        {
            Internal_ProcessCameraCommand(cam, cmd, camXRSettings, results.ptr, IntPtr.Zero);
        }

        public static void ProcessCameraCommand(Camera cam, CommandBuffer cmd, VFXCameraXRSettings camXRSettings, Rendering.CullingResults results, Rendering.CullingResults customPassResults)
        {
            Internal_ProcessCameraCommand(cam, cmd, camXRSettings, results.ptr, customPassResults.ptr);
        }

        extern private static void Internal_ProcessCameraCommand([NotNull] Camera cam, CommandBuffer cmd, VFXCameraXRSettings camXRSettings, IntPtr cullResults, IntPtr customPassCullResults);
        extern public static VFXCameraBufferTypes IsCameraBufferNeeded([NotNull] Camera cam);
        extern public static void SetCameraBuffer([NotNull] Camera cam, VFXCameraBufferTypes type, Texture buffer, int x, int y, int width, int height);

        extern public static void SetRayTracingEnabled(bool enabled);
        extern public static void RequestRtasAabbConstruction();
    }
}
