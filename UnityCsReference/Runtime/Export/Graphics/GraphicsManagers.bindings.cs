// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GraphicsManagers — 渲染设置与质量等级管理
//
// 📌 本文件包含三个核心类：
//   1. RenderSettings：场景级渲染设置（雾效、环境光、天空盒等）
//   2. QualitySettings：质量等级配置管理（阴影、LOD、抗锯齿等）
//   3. TextureMipmapLimitGroups：纹理 Mipmap 限制分组
//
// 🏗 核心概念：
//   - RenderSettings 控制场景的"氛围"：雾、环境光、反射、天空盒
//   - QualitySettings 管理多个质量等级，每个等级有独立的渲染参数
//   - 运行时可通过 SetQualityLevel 切换质量等级
//
// 💡 理解关键：
//   - RenderSettings 是全局单例，通过 GetRenderSettings() 获取
//   - QualitySettings 同样是全局单例，通过 GetQualitySettings() 获取
//   - 切换质量等级时可以触发 applyExpensiveChanges（重新加载纹理、重编 Shader 等）
//
// 📍 对应 C++ 头文件：Runtime/Camera/RenderSettings.h, Runtime/Graphics/QualitySettings.h
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

using AmbientMode = UnityEngine.Rendering.AmbientMode;
using ReflectionMode = UnityEngine.Rendering.DefaultReflectionMode;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    // ==============================================================
    // TerrainQualityOverrides — 地形质量覆盖标志位
    //
    // 🎯 用于 QualitySettings 中按质量等级覆盖地形渲染参数
    //   每个标志位控制一个独立的地形参数是否被当前等级覆盖
    // ==============================================================
    [Flags]
    public enum TerrainQualityOverrides
    {
        None = 0,
        PixelError = 1,
        BasemapDistance = 2,
        DetailDensity = 4,
        DetailDistance = 8,
        TreeDistance = 16,
        BillboardStart = 32,
        FadeLength = 64,
        MaxTrees = 128
    }

    [NativeHeader("Runtime/Camera/RenderSettings.h")]
    [NativeHeader("Runtime/Graphics/QualitySettingsTypes.h")]
    [StaticAccessor("GetRenderSettings()", StaticAccessorType.Dot)]
    public sealed partial class RenderSettings : Object
    {
        private RenderSettings() {}

        [NativeProperty("UseFog")]         extern public static bool  fog              { get; set; }
        [NativeProperty("LinearFogStart")] extern public static float fogStartDistance { get; set; }
        [NativeProperty("LinearFogEnd")]   extern public static float fogEndDistance   { get; set; }
        extern public static FogMode fogMode    { get; set; }
        extern public static Color   fogColor   { get; set; }
        extern public static float   fogDensity { get; set; }

        extern public static AmbientMode ambientMode   { get; set; }
        extern public static Color ambientSkyColor     { get; set; }
        extern public static Color ambientEquatorColor { get; set; }
        extern public static Color ambientGroundColor  { get; set; }
        extern public static float ambientIntensity    { get; set; }
        [NativeProperty("AmbientSkyColor")] extern public static Color ambientLight { get; set; }

        extern public static Color subtractiveShadowColor { get; set; }

        [NativeProperty("SkyboxMaterial")] extern static public Material skybox { get; set; }
        extern public static Light sun { get; set; }
        extern public static Rendering.SphericalHarmonicsL2 ambientProbe { [NativeMethod("GetFinalAmbientProbe")] get; set; }

        [System.Obsolete(@"RenderSettings.customReflection has been deprecated in favor of RenderSettings.customReflectionTexture.", false)]
        public static Cubemap customReflection
        {
            get
            {
                if (!(customReflectionTexture is Cubemap cube))
                {
                    throw new ArgumentException("RenderSettings.customReflection is currently not referencing a cubemap.");
                }
                return cube;
            }
            set => customReflectionTexture = value;
        }
        [NativeProperty("CustomReflection")] extern public static Texture customReflectionTexture { get; [NativeMethod(ThrowsException = true)] set; }

        extern public static float          reflectionIntensity         { get; set; }
        extern public static int            reflectionBounces           { get; set; }

        [NativeProperty("GeneratedSkyboxReflection")]
        extern internal static Cubemap      defaultReflection           { get; }
        extern public static ReflectionMode defaultReflectionMode       { get; set; }
        extern public static int            defaultReflectionResolution { get; set; }

        extern public static float haloStrength   { get; set; }
        extern public static float flareStrength  { get; set; }
        extern public static float flareFadeSpeed { get; set; }

        [FreeFunction("GetRenderSettings")] extern internal static Object GetRenderSettings();
        [StaticAccessor("RenderSettingsScripting", StaticAccessorType.DoubleColon)] extern internal static void Reset();

        [NativeProperty("DefaultSpotCookie")]
        extern internal static Texture2D spotCookieTexture { get; set; }

        extern internal static Texture2D haloTexture { get; set; }

        extern internal static bool WasUsingAutoEnvironmentBakingWithNonDefaultSettings();
    }

    // ==============================================================
    // TextureMipmapLimitSettings — 纹理 Mipmap 限制设置
    //
    // 🎯 控制特定纹理分组的 Mipmap 偏移量
    //   - limitBiasMode：偏移模式（基于层级/基于全局限制）
    //   - limitBias：Mipmap 偏移值（正值 = 使用更高分辨率 mip，负值 = 更低）
    // ==============================================================
    // Keep in sync with MipmapLimitSettings in Runtime\Graphics\Texture.h
    public struct TextureMipmapLimitSettings
    {
        public TextureMipmapLimitBiasMode limitBiasMode { get; set; }
        public int limitBias { get; set; }
    };

    // ==============================================================
    // TextureMipmapLimitGroups — 纹理 Mipmap 限制分组管理
    //
    // 🎯 允许按名称分组管理纹理的 Mipmap 偏移
    //   不同分组的纹理可以有不同的 Mipmap 限制策略
    //   例如：UI 纹理用高分辨率，远景纹理用低分辨率
    // ==============================================================
    [NativeHeader("Runtime/Graphics/QualitySettings.h")]
    [StaticAccessor("GetQualitySettings()", StaticAccessorType.Dot)]
    public static class TextureMipmapLimitGroups
    {
        [NativeName("CreateTextureMipmapLimitGroup")]
        [NativeMethod(ThrowsException = true)]
        extern public static void CreateGroup([NotNull] string groupName);

        [NativeName("RemoveTextureMipmapLimitGroup")]
        [NativeMethod(ThrowsException = true)]
        extern public static void RemoveGroup([NotNull] string groupName);

        [NativeName("GetTextureMipmapLimitGroupNames")]
        extern public static string[] GetGroups();

        [NativeName("HasTextureMipmapLimitGroup")]
        extern public static bool HasGroup([NotNull] string groupName);
    }

    // ==============================================================
    // QualitySettings — 质量等级配置管理类
    //
    // 📌 作用：
    //   管理项目中定义的多个质量等级（Quality Levels），
    //   每个等级包含阴影、LOD、纹理、抗锯齿等渲染参数。
    //
    // 🎯 关键属性分类：
    //
    //   【阴影系统】
    //   - shadows：阴影质量（None/HardOnly/All）
    //   - shadowDistance：最大阴影距离
    //   - shadowCascades：阴影级联数（0/2/4）
    //   - shadowResolution：阴影贴图分辨率
    //
    //   【LOD 系统】
    //   - lodBias：LOD 偏移（越大越倾向使用高精度模型）
    //   - maximumLODLevel：最大允许 LOD 级别
    //   - enableLODCrossFade：LOD 交叉淡入淡出
    //
    //   【纹理与抗锯齿】
    //   - globalTextureMipmapLimit：全局纹理 Mipmap 限制
    //   - anisotropicFiltering：各向异性过滤模式
    //   - antiAliasing：MSAA 采样数（0/2/4/8）
    //
    //   【同步与上传】
    //   - vSyncCount：垂直同步计数（0=关闭，1=每帧同步，2=每两帧）
    //   - asyncUploadTimeSlice / asyncUploadBufferSize：异步纹理上传配置
    //
    // ⚡ 性能提示：
    //   - SetQualityLevel 的 applyExpensiveChanges 参数控制是否立即应用
    //     高开销变更（如纹理重载、Shader 重编译）
    //   - ForEach 方法可在不改变当前等级的情况下遍历所有等级设置
    // ==============================================================
    [NativeHeader("Runtime/Graphics/QualitySettings.h")]
    [StaticAccessor("GetQualitySettings()", StaticAccessorType.Dot)]
    public sealed partial class QualitySettings : Object
    {
        public static void ForEach(Action callback)
        {
            if (callback == null)
                return;

            int currentQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.count; ++i)
                {
                    QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                    callback();
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(currentQuality, applyExpensiveChanges: false);
            }
        }

        public static void ForEach(Action<int, string> callback)
        {
            if (callback == null)
                return;

            int currentQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.count; ++i)
                {
                    QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                    callback(i, names[i]);
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(currentQuality, applyExpensiveChanges: false);
            }
        }

        private QualitySettings() {}

        extern public static int pixelLightCount { get; set; }

        [NativeProperty("ShadowQuality")] extern public static ShadowQuality shadows { get; set; }
        extern public static ShadowProjection shadowProjection      { get; set; }
        extern public static int              shadowCascades        { get; set; }
        extern public static float            shadowDistance        { get; set; }
        [NativeProperty("ShadowResolution")] extern public static ShadowResolution shadowResolution      { get; set; }
        [NativeProperty("ShadowmaskMode")] extern public static ShadowmaskMode   shadowmaskMode        { get; set; }
        extern public static float            shadowNearPlaneOffset { get; set; }
        extern public static float            shadowCascade2Split   { get; set; }
        extern public static Vector3          shadowCascade4Split   { get; set; }

        [NativeProperty("LODBias")] extern public static float lodBias { get; set; }
        [NativeProperty("MeshLODThreshold")] extern public static float meshLodThreshold { get; set; }
        [NativeProperty("AnisotropicTextures")] extern public static AnisotropicFiltering anisotropicFiltering { get; set; }

        [Obsolete("masterTextureLimit has been deprecated. Use globalTextureMipmapLimit instead (UnityUpgradable) -> globalTextureMipmapLimit", false)]
        [NativeProperty("GlobalTextureMipmapLimit")] extern public static int   masterTextureLimit    { get; set; }
        extern public static int   globalTextureMipmapLimit { get; set; }
        extern public static int   maximumLODLevel       { get; set; }
        extern public static bool  enableLODCrossFade    { get; set; }
        extern public static int   particleRaycastBudget { get; set; }
        extern public static bool  softParticles         { get; set; }
        extern public static bool  softVegetation        { get; set; }
        extern public static int   vSyncCount            { get; set; }
        extern public static int   realtimeGICPUUsage    { get; set; }
        extern public static int   antiAliasing          { get; set; }
        extern public static int   asyncUploadTimeSlice  { get; set; }
        extern public static int   asyncUploadBufferSize { get; set; }
        extern public static bool  asyncUploadPersistentBuffer { get; set; }

        [NativeName("SetLODSettings")]
        extern public static void SetLODSettings(float lodBias, int maximumLODLevel, bool setDirty = true);

        [NativeName("SetTextureMipmapLimitSettings")]
        [NativeMethod(ThrowsException = true)]
        extern public static void SetTextureMipmapLimitSettings(string groupName, TextureMipmapLimitSettings textureMipmapLimitSettings);

        [NativeName("GetTextureMipmapLimitSettings")]
        [NativeMethod(ThrowsException = true)]
        extern public static TextureMipmapLimitSettings GetTextureMipmapLimitSettings(string groupName);

        extern public static bool  realtimeReflectionProbes         { get; set; }
        extern public static bool  billboardsFaceCameraPosition     { get; set; }
        extern public static bool  useLegacyDetailDistribution      { get; set; }
        extern public static float resolutionScalingFixedDPIFactor  { get; set; }

        extern public static TerrainQualityOverrides terrainQualityOverrides { get; set; }
        extern public static float terrainPixelError { get; set; }
        extern public static float terrainDetailDensityScale { get; set; }
        extern public static float terrainBasemapDistance { get; set; }
        extern public static float terrainDetailDistance { get; set; }
        extern public static float terrainTreeDistance { get; set; }
        extern public static float terrainBillboardStart { get; set; }
        extern public static float terrainFadeLength { get; set; }
        extern public static float terrainMaxTrees { get; set; }

        [NativeName("RenderPipeline")] extern private static ScriptableObject INTERNAL_renderPipeline { get; set; }
        public static RenderPipelineAsset renderPipeline
        {
            get { return INTERNAL_renderPipeline as RenderPipelineAsset; }
            set
            {
                INTERNAL_renderPipeline = value;
            }
        }

        [NativeName("GetRenderPipelineAssetAt")]
        extern internal static ScriptableObject InternalGetRenderPipelineAssetAt(int index);
        public static RenderPipelineAsset GetRenderPipelineAssetAt(int index)
        {
            if (index < 0 || index >= names.Length)
                throw new IndexOutOfRangeException($"{nameof(index)} is out of range [0..{names.Length}[");

            return InternalGetRenderPipelineAssetAt(index) as RenderPipelineAsset;
        }

        [Obsolete("blendWeights is obsolete. Use skinWeights instead (UnityUpgradable) -> skinWeights", true)]
        extern public static BlendWeights blendWeights
        {
            [NativeName("GetSkinWeights")] get;
            [NativeMethod(ThrowsException = true)]
            [NativeName("SetSkinWeights")] set; }

        extern public static SkinWeights skinWeights
        {
            get;
            [NativeMethod(ThrowsException = true)] set;
        }

        extern public static int count { [NativeName("GetQualitySettingsCount")] get; }

        extern internal static int GetStrippedMaximumLODLevel();
        extern internal static void SetStrippedMaximumLODLevel(int maximumLODLevel);

        extern public static bool streamingMipmapsActive { get; set; }
        extern public static float streamingMipmapsMemoryBudget { get; set; }
        extern public static int streamingMipmapsRenderersPerFrame { get; set; }
        extern public static int streamingMipmapsMaxLevelReduction { get; set; }
        extern public static bool streamingMipmapsAddAllCameras { get; set; }
        extern public static int streamingMipmapsMaxFileIORequests { get; set; }

        [StaticAccessor("QualitySettingsScripting", StaticAccessorType.DoubleColon)] extern public static int maxQueuedFrames { get; set; }

        [NativeName("GetCurrentIndex")] extern public static int  GetQualityLevel();
        [FreeFunction] extern public static Object GetQualitySettings();
        [NativeName("SetCurrentIndex")] extern public static void SetQualityLevel(int index, [uei.DefaultValue("true")] bool applyExpensiveChanges);

        [NativeProperty("QualitySettingsNames")] extern public static string[] names { get; }

        [NativeName("IsTextureResReducedOnAnyPlatform")] extern internal static bool IsTextureResReducedOnAnyPlatform();

        [NativeName("IsPlatformIncluded")] extern public static bool IsPlatformIncluded(string buildTargetGroupName, int index);
        [NativeName("IncludePlatform")] extern internal static void IncludePlatformAt(string buildTargetGroupName, int index);
        [NativeName("ExcludePlatform")] extern internal static void ExcludePlatformAt(string buildTargetGroupName, int index);
        public static bool TryIncludePlatformAt(string buildTargetGroupName, int index, out Exception error)
        {
            if (index < 0 || index >= count)
            {
                error = new ArgumentOutOfRangeException($"{nameof(index)} must be greater than 0 and lower than {count}");
                return false;
            }

            error = null;
            IncludePlatformAt(buildTargetGroupName, index);
            return true;
        }

        public static bool TryExcludePlatformAt(string buildTargetGroupName, int index, out Exception error)
        {
            if (index < 0 || index >= count)
            {
                error = new ArgumentOutOfRangeException($"{nameof(index)} must be greater than 0 and lower than {count}");
                return false;
            }

            error = null;
            ExcludePlatformAt(buildTargetGroupName, index);
            return true;
        }

        [NativeName("GetActiveQualityLevelsForPlatform")] extern public static int[] GetActiveQualityLevelsForPlatform(string buildTargetGroupName);
        [NativeName("GetActiveQualityLevelsForPlatformCount")] extern public static int GetActiveQualityLevelsForPlatformCount(string buildTargetGroupName);
        [NativeName("GetDefaultQualityForPlatform")] extern internal static int GetDefaultQualityForPlatform(string platformName);

        [NativeName("GetRenderPipelineAssetsForPlatform")] extern internal static ScriptableObject[] InternalGetRenderPipelineAssetsForPlatform(string buildTargetGroupName);
        public static void GetRenderPipelineAssetsForPlatform<T>(string buildTargetGroupName, out HashSet<T> uniqueRenderPipelineAssets)
            where T : RenderPipelineAsset
        {
            var scriptableObjects = InternalGetRenderPipelineAssetsForPlatform(buildTargetGroupName);
            uniqueRenderPipelineAssets = new HashSet<T>(scriptableObjects.Length);
            for (int i = 0; i < scriptableObjects.Length; ++i)
            {
                if (scriptableObjects[i] is T rpAsset)
                    uniqueRenderPipelineAssets.Add(rpAsset);
            }
        }

        public static void GetRenderPipelineAssetsForPlatform<T>(string buildTargetGroupName, out HashSet<T> uniqueRenderPipelineAssets, out bool allLevelsAreOverridden)
            where T : RenderPipelineAsset
        {
            allLevelsAreOverridden = true;
            var scriptableObjects = InternalGetRenderPipelineAssetsForPlatform(buildTargetGroupName);
            uniqueRenderPipelineAssets = new HashSet<T>(scriptableObjects.Length);
            for (int i = 0; i < scriptableObjects.Length; ++i)
            {
                if (scriptableObjects[i] is T rpAsset)
                    uniqueRenderPipelineAssets.Add(rpAsset);
                else
                    allLevelsAreOverridden = false;
            }
        }

        public static void GetAllRenderPipelineAssetsForPlatform(string buildTargetGroupName, ref List<RenderPipelineAsset> renderPipelineAssets)
        {
            if (renderPipelineAssets == null)
                renderPipelineAssets = new List<RenderPipelineAsset>();

            var scriptableObjects = InternalGetRenderPipelineAssetsForPlatform(buildTargetGroupName);
            for (int i = 0; i < scriptableObjects.Length; ++i)
            {
                if (scriptableObjects[i] is RenderPipelineAsset rpAsset)
                    renderPipelineAssets.Add(rpAsset);
                else
                    renderPipelineAssets.Add(GraphicsSettings.defaultRenderPipeline);
            }

            if (renderPipelineAssets.Count == 0 && GraphicsSettings.defaultRenderPipeline != null)
                renderPipelineAssets.Add(GraphicsSettings.defaultRenderPipeline);
        }

        static HashSet<Type> s_RenderPipelineAssetsTypes = new();
        static List<RenderPipelineAsset> s_RenderPipelineAssets = new();

        internal static bool SamePipelineAssetsForPlatform(string buildTargetGroupName)
        {
            s_RenderPipelineAssetsTypes.Clear();
            s_RenderPipelineAssets.Clear();

            GetAllRenderPipelineAssetsForPlatform(buildTargetGroupName, ref s_RenderPipelineAssets);

            for (int i = 0; i < s_RenderPipelineAssets.Count; i++)
            {
                if (s_RenderPipelineAssets[i] != null)
                    s_RenderPipelineAssetsTypes.Add(s_RenderPipelineAssets[i].GetType());
                else
                    s_RenderPipelineAssetsTypes.Add(null);
            }

            return s_RenderPipelineAssetsTypes.Count == 1;
        }
    }

    // ==============================================================
    // QualitySettings — 颜色空间设置（partial 扩展）
    //
    // 🎯 desiredColorSpace / activeColorSpace：查询项目配置的颜色空间
    //   Linear：物理正确的光照计算（推荐）
    //   Gamma：传统颜色空间（兼容旧项目）
    // ==============================================================
    // both desiredColorSpace/activeColorSpace should be deprecated
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    public sealed partial class QualitySettings : Object
    {
        extern public static ColorSpace desiredColorSpace
        {
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)][NativeName("GetColorSpace")] get;
        }
        extern public static ColorSpace activeColorSpace
        {
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)][NativeName("GetColorSpace")] get;
        }
    }
}

namespace UnityEngine.Experimental.GlobalIllumination
{
    [NativeHeader("Runtime/Camera/RenderSettings.h")]
    [StaticAccessor("GetRenderSettings()", StaticAccessorType.Dot)]
    public partial class RenderSettings
    {
        extern public static bool useRadianceAmbientProbe { get; set; }
    }
}
