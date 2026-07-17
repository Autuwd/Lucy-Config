// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GraphicsSettings — 全局图形设置管理类
//
// 📌 作用：
//   GraphicsSettings 管理项目级别的渲染配置，包括：
//   - 当前使用的渲染管线资产（SRP/HDRP/URP）
//   - 默认材质和着色器
//   - 光照、阴影、透明度排序等全局渲染行为
//
// 🏗 核心概念：
//   - currentRenderPipeline：当前激活的可编程渲染管线（SRP）资产
//   - defaultRenderPipeline：默认渲染管线资产（QualitySettings 可覆盖）
//   - isScriptableRenderPipelineEnabled：是否启用了 SRP
//
// 💡 理解关键：
//   GraphicsSettings 是连接 Project Settings 和渲染管线的桥梁。
//   在 SRP 环境下，通过 currentRenderPipeline 属性可以获取当前活跃的
//   RenderPipelineAsset，进而获取其默认材质、着色器等。
//
// ⚠️  注意：直接修改 defaultRenderPipeline 会影响整个项目渲染行为。
//       通常通过 Project Settings > Graphics 进行配置。
//
// 📍 对应 C++ 头文件：Runtime/Camera/GraphicsSettings.h
// ==============================================================

using System;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    // ==============================================================
    // DefaultMaterialType — Unity 内置默认材质类型枚举
    //
    // 🎯 定义不同用途的默认材质类型：
    //   Default、Particle、Line、Terrain、Sprite、UGUI 等
    //   每种类型由当前 SRP 的 RenderPipelineAsset 提供对应材质
    // ==============================================================
    [VisibleToOtherModules]
    internal enum DefaultMaterialType
    {
        Default = 0,
        Particle = 1,
        Line = 2,
        Terrain = 3,
        Sprite = 4,
        SpriteMask = 5,
        UGUI = 6,
        UGUI_Overdraw = 7,
        UGUI_ETC1Supported = 8,
    }
    
    // ==============================================================
    // DefaultShaderType — Unity 内置默认着色器类型枚举
    //
    // 🎯 定义不同用途的默认着色器：
    //   Default、AutodeskInteractive 系列、TerrainDetail 系列、SpeedTree 系列
    //   同样由当前 SRP 的 RenderPipelineAsset 提供
    // ==============================================================
    [VisibleToOtherModules]
    internal enum DefaultShaderType
    {
        Default = 0,
        AutodeskInteractive = 1,
        AutodeskInteractiveTransparent = 2,
        AutodeskInteractiveMasked = 3,
        TerrainDetailLit = 4,
        TerrainDetailGrass = 5,
        TerrainDetailGrassBillboard = 6,
        SpeedTree7 = 7,
        SpeedTree8 = 8,
        SpeedTree9 = 9,
    }

    // ==============================================================
    // GraphicsSettings — 全局图形设置类
    //
    // 🔑 关键属性：
    //   - currentRenderPipeline：当前激活的 SRP 资产（只读）
    //   - defaultRenderPipeline：默认 SRP 资产（可写，QualitySettings 可覆盖）
    //   - isScriptableRenderPipelineEnabled：是否启用了 SRP
    //   - lightsUseLinearIntensity：光照是否使用线性强度（HDR 管线推荐开启）
    //   - lightsUseColorTemperature：光照是否支持色温
    //   - useScriptableRenderPipelineBatching：SRP 批处理
    //   - HasShaderDefine：查询当前 Shader 预定义宏
    //
    // 📌 GetDefaultMaterial / GetDefaultShader：
    //   由 C++ 引擎在运行时调用，根据当前 SRP 提供默认材质/着色器。
    //   switch 表达式使用 pattern matching 简洁地分发到 SRP 资产。
    // ==============================================================
    [NativeHeader("Runtime/Camera/GraphicsSettings.h")]
    [StaticAccessor("GetGraphicsSettings()", StaticAccessorType.Dot)]
    public sealed partial class GraphicsSettings : Object
    {
        private GraphicsSettings() {}

        extern public static TransparencySortMode   transparencySortMode { get; set; }
        extern public static Vector3                transparencySortAxis { get; set; }
        extern public static bool realtimeDirectRectangularAreaLights { get; set; }
        extern public static bool lightsUseLinearIntensity   { get; set; }
        extern public static bool lightsUseColorTemperature  { get; set; }
        [Obsolete ($"This property is obsolete. Use {nameof(RenderingLayerMask)} API and Tags & Layers project settings instead. #from(23.3)")]
        extern public static uint defaultRenderingLayerMask { get; set; }
        extern public static Camera.GateFitMode defaultGateFitMode { get; set; }
        extern public static bool useScriptableRenderPipelineBatching { get; set; }
        extern public static bool logWhenShaderIsCompiled { get; set; }
        extern public static bool disableBuiltinCustomRenderTextureUpdate { get; set; }
        extern public static VideoShadersIncludeMode videoShadersIncludeMode
        {
            get;
            set;
        }
        extern public static LightProbeOutsideHullStrategy lightProbeOutsideHullStrategy { get; set; }

        extern public static bool HasShaderDefine(GraphicsTier tier, BuiltinShaderDefine defineHash);
        public static bool HasShaderDefine(BuiltinShaderDefine defineHash)
        {
            return HasShaderDefine(Graphics.activeTier, defineHash);
        }

        [NativeName("CurrentRenderPipeline")] extern private static ScriptableObject INTERNAL_currentRenderPipeline { get; }
        public static RenderPipelineAsset currentRenderPipeline
        {
            get { return INTERNAL_currentRenderPipeline as RenderPipelineAsset; }
        }

        public static bool isScriptableRenderPipelineEnabled => INTERNAL_currentRenderPipeline != null;

        public static Type currentRenderPipelineAssetType => isScriptableRenderPipelineEnabled ? INTERNAL_currentRenderPipeline.GetType() : null;

        [Obsolete("renderPipelineAsset has been deprecated. Use defaultRenderPipeline instead (UnityUpgradable) -> defaultRenderPipeline", false)]
        public static RenderPipelineAsset renderPipelineAsset
        {
            get { return defaultRenderPipeline; }
            set { defaultRenderPipeline = value; }
        }

        [NativeName("DefaultRenderPipeline")] extern private static ScriptableObject INTERNAL_defaultRenderPipeline { get; set; }
        public static RenderPipelineAsset defaultRenderPipeline
        {
            get { return INTERNAL_defaultRenderPipeline as RenderPipelineAsset; }
            set
            {
                INTERNAL_defaultRenderPipeline = value;
            }
        }

        extern static private ScriptableObject[] GetAllConfiguredRenderPipelines();

        public static RenderPipelineAsset[] allConfiguredRenderPipelines
        {
            get
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return GetAllConfiguredRenderPipelines().Cast<RenderPipelineAsset>().ToArray();
#pragma warning restore UA2001
            }
        }

        [FreeFunction] extern public static Object GetGraphicsSettings();

        [NativeName("SetShaderModeScript")]   extern static public void                 SetShaderMode(BuiltinShaderType type, BuiltinShaderMode mode);
        [NativeName("GetShaderModeScript")]   extern static public BuiltinShaderMode    GetShaderMode(BuiltinShaderType type);

        [NativeName("SetCustomShaderScript")] extern static public void     SetCustomShader(BuiltinShaderType type, Shader shader);
        [NativeName("GetCustomShaderScript")] extern static public Shader   GetCustomShader(BuiltinShaderType type);

        extern public static bool cameraRelativeLightCulling { get; set; }
        extern public static bool cameraRelativeShadowCulling { get; set; }

        [RequiredByNativeCode]
        [VisibleToOtherModules]
        internal static Shader GetDefaultShader(DefaultShaderType type)
        {
            var rp = currentRenderPipeline;
            if (currentRenderPipeline == null)
                return null;

            return type switch
            {
                DefaultShaderType.Default => rp.defaultShader,
                DefaultShaderType.AutodeskInteractive => rp.autodeskInteractiveShader,
                DefaultShaderType.AutodeskInteractiveTransparent => rp.autodeskInteractiveTransparentShader,
                DefaultShaderType.AutodeskInteractiveMasked => rp.autodeskInteractiveMaskedShader,
                DefaultShaderType.TerrainDetailLit => rp.terrainDetailLitShader,
                DefaultShaderType.TerrainDetailGrass => rp.terrainDetailGrassShader,
                DefaultShaderType.TerrainDetailGrassBillboard => rp.terrainDetailGrassBillboardShader,
                DefaultShaderType.SpeedTree7 => rp.defaultSpeedTree7Shader,
                DefaultShaderType.SpeedTree8 => rp.defaultSpeedTree8Shader,
                DefaultShaderType.SpeedTree9 => rp.defaultSpeedTree9Shader,
                _ => throw new NotImplementedException($"DefaultShaderType {type} not implemented")
            };
        }

        [RequiredByNativeCode]
        [VisibleToOtherModules]
        internal static Material GetDefaultMaterial(DefaultMaterialType type)
        {
            var rp = currentRenderPipeline;
            if (currentRenderPipeline == null)
                return null;

            return type switch
            {
                DefaultMaterialType.Default => rp.defaultMaterial,
                DefaultMaterialType.Particle => rp.defaultParticleMaterial,
                DefaultMaterialType.Line => rp.defaultLineMaterial,
                DefaultMaterialType.Terrain => rp.defaultTerrainMaterial,
                DefaultMaterialType.Sprite => rp.default2DMaterial,
                DefaultMaterialType.SpriteMask => rp.default2DMaskMaterial,
                DefaultMaterialType.UGUI => rp.defaultUIMaterial,
                DefaultMaterialType.UGUI_Overdraw => rp.defaultUIOverdrawMaterial,
                DefaultMaterialType.UGUI_ETC1Supported => rp.defaultUIETC1SupportedMaterial,
                _ => throw new NotImplementedException($"DefaultMaterialType {type} not implemented")
            };
        }
    }
}
