// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Light — Unity 光照系统的核心组件
//
// 📌 作用：
//   Light 组件为场景提供光源，影响物体的颜色、亮度和阴影。
//   每个光源都有类型（方向光/点光/聚光/面光）、颜色、强度等属性。
//   Light 同时支持实时渲染和烘焙（Baked）两种光照模式。
//
// 🏗 核心概念：
//   - LightType（光源类型）：Directional / Point / Spot / Area
//   - 光照烘焙（Baking）：Realtime（实时）/ Baked（烘焙）/ Mixed（混合）
//   - 阴影系统：Shadow Mapping（阴影贴图）技术
//   - Cookie（光影投射纹理）：可投射特定形状的光影图案
//   - CommandBuffer：可在光照事件点注入自定义渲染命令
//
// 💡 光源类型详解：
//   1. Directional（方向光）：模拟太阳光，平行照射，无衰减
//      - 无限远的光源，所有光线方向一致
//      - 不需要 range（范围），使用 shadow cascades（阴影级联）
//   2. Point（点光源）：从一个点向四周发射光线
//      - 有 range（衰减范围），强度随距离衰减
//      - 使用 6 面立方体贴图渲染阴影
//   3. Spot（聚光灯）：从一个点沿锥形区域发射光线
//      - 有 spotAngle（锥角）和 range（射程）
//      - 最常用的局部光源（手电筒、路灯等）
//   4. Area（面光源）：从一个矩形区域发射光线
//      - 只支持烘焙模式（不能实时计算）
//      - areaSize 定义矩形尺寸，产生柔和阴影
//
// ⚡ 性能提示：
//   - 实时光源越多，渲染开销越大（每个光源多一个渲染 Pass）
//   - 烘焙光源（Baked）预计算到光照贴图，运行时零开销
//   - Mixed 光源结合实时直接光和烘焙间接光
//   - Shadow Resolution 和 Shadow Strength 影响阴影质量和性能
//
// 📍 对应 C++ 头文件：Runtime/Camera/SharedLightData.h
// ==============================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    // ==============================================================
    // LightBakingOutput — 光照烘焙输出数据
    //
    // 💡 存储光源烘焙后的信息，包括：
    //   - probeOcclusionLightIndex：探针遮挡光源索引
    //   - lightmapBakeType：光照贴图烘焙类型（Realtime/Baked/Mixed）
    //   - mixedLightingMode：混合光照模式（Shadowmask/Distance Shadowmask）
    //   - isBaked：是否已完成烘焙
    //
    // 📌 典型用途：
    //   在自定义渲染管线中读取烘焙光照的元数据。
    // ==============================================================
    [NativeHeader("Runtime/Camera/SharedLightData.h")]
    public struct LightBakingOutput
    {
        public int probeOcclusionLightIndex;
        public int occlusionMaskChannel;
        [NativeName("lightmapBakeMode.lightmapBakeType")]
        public LightmapBakeType lightmapBakeType;
        [NativeName("lightmapBakeMode.mixedLightingMode")]
        public MixedLightingMode mixedLightingMode;
        public bool isBaked;
    }

    [NativeHeader("Runtime/Camera/SharedLightData.h")]
    public enum LightShadowCasterMode
    {
        Default = 0,
        [Obsolete("This has been deprecated. Use ShadowMask instead. (UnityUpgradable) -> ShadowMask")] NonLightmappedOnly = 1,
        ShadowMask = 1,
        [Obsolete("This has been deprecated. Use DistanceShadowMaskMode instead. (UnityUpgradable) -> DistanceShadowMask")] Everything = 2,
        DistanceShadowMask = 2
    }

    // ==============================================================
    // Light — 光源组件（挂载到 GameObject 上提供光照）
    //
    // 🎯 继承链：Light → Behaviour → Component → Object
    //
    // 🔑 关键属性：
    //   - type：光源类型（Directional/Point/Spot/Area）
    //   - color/intensity：颜色和强度
    //   - range：影响范围（Point/Spot 有效）
    //   - spotAngle/innerSpotAngle：聚光灯锥角
    //   - shadows/shadowStrength：阴影类型和强度
    //   - cookie：投影纹理（Cookie）
    //   - bounceIntensity：间接反弹光强度
    //
    // 🔑 关键方法：
    //   - Reset()：重置为默认值
    //   - AddCommandBuffer()：在光照事件点注入渲染命令
    //   - SetLightDirty()：标记光源数据需要更新
    //
    // 🔑 烘焙相关：
    //   - lightmapBakeType：控制此光源的烘焙模式
    //   - bakingOutput：烘焙后的元数据（由光照烘焙系统填充）
    //   - shadowCasterMode：阴影投射模式（Shadowmask/DistanceShadowmask）
    //
    // ⚠️ SRP 兼容性：
    //   多个属性（shadowResolution, renderMode 等）仅与 Built-in RP 兼容，
    //   在 SRP（URP/HDRP）中会输出警告。
    // ==============================================================
    // Script interface for [[wiki:class-Light|light components]].
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Export/Graphics/Light.bindings.h")]
    public sealed partial class Light : Behaviour
    {
        extern public void Reset();

        // ==============================================================
        // 阴影系统（Shadow Settings）
        //
        // ⚡ 阴影类型（LightShadows）：
        //   - None：不投射阴影
        //   - Hard：硬阴影（边缘锐利）
        //   - Soft：软阴影（边缘模糊，使用 PCF 滤波）
        //
        // 📌 阴影参数：
        //   - shadowStrength：0=无阴影，1=全黑阴影
        //   - shadowResolution：阴影贴图分辨率（低/中/高/很高）
        //   - shadowBias：深度偏移（消除阴影痤疮 Shadow Acne）
        //   - shadowNormalBias：法线偏移（消除彼得潘效应 Peter Panning）
        //   - shadowNearPlane：阴影近裁面
        //   - shadowCustomResolution：自定义阴影贴图大小
        //
        // 💡 Shadow Cascades（阴影级联）：
        //   方向光使用级联阴影，将视锥体分成多级，
        //   近处高分辨率、远处低分辨率，平衡质量和性能。
        // ==============================================================

        // How this light casts shadows?
        extern public LightShadows shadows
        {
            [NativeMethod("GetShadowType")] get;
            [FreeFunction("Light_Bindings::SetShadowType", HasExplicitThis = true, ThrowsException = true)] set;
        }

        // Strength of light's shadows
        extern public float shadowStrength
        {
            get;
            [FreeFunction("Light_Bindings::SetShadowStrength", HasExplicitThis = true)] set;
        }

        // Shadow resolution
        public LightShadowResolution shadowResolution
        {
            get => ShadowResolution;
            set
            {
                if (GraphicsSettings.isScriptableRenderPipelineEnabled)
                    LogWarningOnlyBuiltIn();
                ShadowResolution = value;
            }
        }

        static void LogWarningOnlyBuiltIn([CallerMemberName] string propertyName = "")
        {
            Debug.LogWarning($"Light.{propertyName} is compatible only with the Built-In Render Pipeline.");
        }

        extern LightShadowResolution ShadowResolution
        {
            get;
            [FreeFunction("Light_Bindings::SetShadowResolution", HasExplicitThis = true, ThrowsException = true)] set;
        }

        extern public float[] layerShadowCullDistances
        {
            [FreeFunction("Light_Bindings::GetLayerShadowCullDistances", HasExplicitThis = true, ThrowsException = false)]
            get;
            [FreeFunction("Light_Bindings::SetLayerShadowCullDistances", HasExplicitThis = true, ThrowsException = true)]
            set;
        }

        extern public Vector2 cookieSize2D { get; set; }

        // ==============================================================
        // Cookie（光影投射纹理）
        //
        // 💡 Cookie 是一张投射到场景中的纹理，用于产生特定光影形状。
        //   - 点光源：使用 Cubemap 纹理（6 面）
        //   - 聚光灯/方向光：使用 2D 纹理
        //   - 常见效果：窗户光影、树叶间隙光斑
        //   - cookieSize2D：Cookie 纹理的投射尺寸
        // ==============================================================

        // The cookie texture projected by the light.
        extern public Texture cookie { get; set; }

        // How to render the light.
        extern public LightRenderMode renderMode
        {
            get;
            [FreeFunction("Light_Bindings::SetRenderMode", HasExplicitThis = true, ThrowsException = true)] set;
        }

        // ==============================================================
        // 面光源（Area Light）设置
        //
        // 📌 areaSize：面光源的矩形尺寸（宽 × 高）
        //   - 仅对 Area 类型光源有效
        //   - 只支持烘焙模式（不能实时渲染）
        //   - 产生柔和的面光源阴影效果
        //
        // 💡 enableSpotReflector：
        //   聚光灯的反射器优化，减少光线浪费。
        //   默认开启，聚光灯只向锥形区域投射光线。
        // ==============================================================

        // The size of the area light.
        extern public Vector2 areaSize { get; set; }

        // ==============================================================
        // 光照烘焙类型（LightmapBakeType）
        //
        // 🎯 控制此光源的烘焙行为：
        //   - Realtime：完全实时计算，不烘焙到光照贴图
        //   - Baked：完全烘焙到光照贴图，运行时零开销
        //   - Mixed：实时直接光 + 烘焙间接光（混合模式）
        //
        // ⚡ Mixed 模式子类型（mixedLightingMode）：
        //   - Shadowmask：使用 Shadowmask 进行远处阴影
        //   - DistanceShadowmask：近距离实时阴影 + 远距离烘焙阴影
        //
        // 📌 注意：此属性仅在编辑器中可设置，运行时为只读。
        // ==============================================================

        // Lightmapping mode. Editor only.
        extern public LightmapBakeType lightmapBakeType
        {
            [NativeMethod("GetBakeType")] get;
            [NativeMethod("SetBakeType")] set;
        }

        // ==============================================================
        // CommandBuffer — 光照事件渲染命令注入
        //
        // ⚡ 允许在光照渲染管线的特定事件点注入自定义渲染命令：
        //   - LightEvent.BeforeShadowMap：阴影贴图渲染前
        //   - LightEvent.AfterShadowMap：阴影贴图渲染后
        //   - LightEvent.BeforeShadowMapSplit：级联阴影分段前
        //   - LightEvent.AfterShadowMapSplit：级联阴影分段后
        //
        // 📌 用途：
        //   - 自定义阴影渲染效果
        //   - 在阴影渲染中添加额外 Pass
        //   - 使用 CommandBuffer 实现高级阴影技术
        //
        // ⚠️ 此功能仅与 Built-in Render Pipeline 兼容。
        // ==============================================================

        extern public void SetLightDirty();

        public void AddCommandBuffer(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer)
        {
            AddCommandBuffer(evt, buffer, UnityEngine.Rendering.ShadowMapPass.All);
        }


        public void AddCommandBuffer(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask)
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
                LogWarningOnlyBuiltIn();
            AddCommandBufferInternal(evt, buffer, shadowPassMask);
        }

        [FreeFunction("Light_Bindings::AddCommandBuffer", HasExplicitThis = true)]
        internal extern void AddCommandBufferInternal(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask);

        public void AddCommandBufferAsync(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ComputeQueueType queueType)
        {
            AddCommandBufferAsync(evt, buffer, UnityEngine.Rendering.ShadowMapPass.All, queueType);
        }

        public void AddCommandBufferAsync(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask, UnityEngine.Rendering.ComputeQueueType queueType)
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            AddCommandBufferAsyncInternal(evt, buffer, shadowPassMask, queueType);
        }

        [FreeFunction("Light_Bindings::AddCommandBufferAsync", HasExplicitThis = true)]
        internal extern void AddCommandBufferAsyncInternal(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask, UnityEngine.Rendering.ComputeQueueType queueType);

        public void RemoveCommandBuffer(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer)
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            RemoveCommandBufferInternal(evt, buffer);
        }
        [NativeMethod("RemoveCommandBuffer")]extern internal void RemoveCommandBufferInternal(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer);

        public void RemoveCommandBuffers(UnityEngine.Rendering.LightEvent evt)
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            RemoveCommandBuffersInternal(evt);
        }
        [NativeMethod("RemoveCommandBuffers")] extern internal void RemoveCommandBuffersInternal(UnityEngine.Rendering.LightEvent evt);

        public void RemoveAllCommandBuffers()
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            RemoveAllCommandBuffersInternal();
        }
        [NativeMethod("RemoveAllCommandBuffers")] extern internal void RemoveAllCommandBuffersInternal();

        public UnityEngine.Rendering.CommandBuffer[] GetCommandBuffers(UnityEngine.Rendering.LightEvent evt)
        {
            if(GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            return GetCommandBuffersInternal(evt);
        }
        [FreeFunction("Light_Bindings::GetCommandBuffers", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal UnityEngine.Rendering.CommandBuffer[] GetCommandBuffersInternal(UnityEngine.Rendering.LightEvent evt);

        extern public int commandBufferCount { get; }

        [NativeProperty("LightType")]         // ==============================================================
        // 核心光照属性（Core Light Properties）
        //
        // 🎯 type：光源类型（LightType 枚举）
        //   - Directional：方向光（太阳光），平行光线，无范围概念
        //   - Point：点光源，从中心向四周发射
        //   - Spot：聚光灯，锥形投射
        //   - Area：面光源，矩形区域发射（仅支持烘焙）
        //
        // 📌 颜色与强度：
        //   - color：光源颜色（默认白色）
        //   - intensity：光照强度（方向光典型值 1-2）
        //   - bounceIntensity：间接反弹光倍率（默认 1）
        //   - colorTemperature：色温（需 useColorTemperature=true）
        //   - lightUnit：光照单位（Lux/Lumen/LumenPerSquareMeter）
        //
        // 📌 范围与形状：
        //   - range：光源影响范围（Point/Spot 有效）
        //   - spotAngle：聚光灯外锥角（度）
        //   - innerSpotAngle：聚光灯内锥角（度，内锥内为全亮）
        //   - shapeRadius：光源形状半径（柔化阴影边缘）
        // ==============================================================

        extern public LightType type { get; set; }

        extern public float spotAngle { get; set; }
        extern public float innerSpotAngle { get; set; }
        extern public Color color { get; set; }
        extern public float colorTemperature { get; set; }
        extern public bool useColorTemperature { get; set; }
        extern public float intensity { get; set; }
        extern public float bounceIntensity { get; set; }
        extern public LightUnit lightUnit { get; set; }
        extern public float luxAtDistance { get; set; }
        extern public bool enableSpotReflector { get; set; }

        // ==============================================================
        // 阴影高级设置（Advanced Shadow Settings）
        //
        // 📌 shadowBias / shadowNormalBias：
        //   - shadowBias：沿光线方向偏移（消除 Shadow Acne 自遮挡）
        //   - shadowNormalBias：沿法线方向偏移（减少 Peter Panning 脱影）
        //   - 两者需要根据场景尺度手动调优
        //
        // 📌 useBoundingSphereOverride / boundingSphereOverride：
        //   覆盖阴影投射的包围球范围，用于优化大型光源的阴影。
        //
        // 📌 useViewFrustumForShadowCasterCull：
        //   是否使用视锥体剔除阴影投射体（优化性能）。
        //
        // 📌 useShadowMatrixOverride / shadowMatrixOverride：
        //   自定义阴影变换矩阵，用于高级阴影技术。
        // ==============================================================

        extern public bool useBoundingSphereOverride { get; set; }
        extern public Vector4 boundingSphereOverride { get; set; }

        extern public bool useViewFrustumForShadowCasterCull { get; set; }
        extern public bool forceVisible { get; set; }
        extern public int shadowCustomResolution { get; set; }
        extern public float shadowBias { get; set; }
        extern public float shadowNormalBias { get; set; }
        extern public float shadowNearPlane { get; set; }
        extern public bool useShadowMatrixOverride { get; set; }
        extern public Matrix4x4 shadowMatrixOverride { get; set; }

        // ==============================================================
        // 其他属性（Miscellaneous Properties）
        //
        // 📌 cullingMask：控制光源影响哪些层的 GameObject
        // 📌 renderingLayerMask：渲染层掩码（用于选择性接受光照）
        // 📌 lightShadowCasterMode：阴影投射模式
        //   - Default：默认行为
        //   - ShadowMask：使用 Shadowmask 贴图
        //   - DistanceShadowmask：使用距离阴影掩码
        // 📌 flare：镜头光晕（Lens Flare）资源
        // 📌 bakingOutput：烘焙输出数据（由烘焙系统填充）
        // 📌 shadowAngle：聚光灯阴影的偏转角度
        // ==============================================================

        extern public float range { get; set; }
        extern public float dilatedRange { get; }
        extern public Flare flare { get; set; }

        extern public LightBakingOutput bakingOutput { get; set; }
        extern public int cullingMask { get; set; }
        extern public int renderingLayerMask { get; set; }
        extern public LightShadowCasterMode lightShadowCasterMode { get; set; }
        extern public float shapeRadius { get; set; }

        extern public float shadowAngle { get; set; }
    }
}
