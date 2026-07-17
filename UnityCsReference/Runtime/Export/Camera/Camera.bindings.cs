// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using uei = UnityEngine.Internal;

using OpaqueSortMode = UnityEngine.Rendering.OpaqueSortMode;
using CameraEvent = UnityEngine.Rendering.CameraEvent;
using CommandBuffer = UnityEngine.Rendering.CommandBuffer;
using ComputeQueueType = UnityEngine.Rendering.ComputeQueueType;

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/Camera.h")]
    [NativeHeader("Runtime/Camera/RenderManager.h")]
    [NativeHeader("Runtime/GfxDevice/GfxDeviceTypes.h")]
    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    [NativeHeader("Runtime/Graphics/CommandBuffer/RenderingCommandBuffer.h")]
    [NativeHeader("Runtime/Misc/GameObjectUtility.h")]
    [NativeHeader("Runtime/Shaders/Shader.h")]
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    public sealed partial class Camera : Behaviour
    {
        /// <summary>
        /// 最小允许的光圈值（物理相机属性）。
        /// 对应真实相机的最小 f-stop 值，用于物理相机模式下的景深和曝光计算。
        /// </summary>
        public const float kMinAperture = 0.7f;

        /// <summary>
        /// 最大允许的光圈值（物理相机属性）。
        /// 对应真实相机的最大 f-stop 值，数值越大进光量越少。
        /// </summary>
        public const float kMaxAperture = 32f;

        /// <summary>
        /// 光圈叶片数的最小值。
        /// 叶片数影响散景（Bokeh）形状的多边形效果，最少 3 片产生三角形散景。
        /// </summary>
        public const int kMinBladeCount = 3;

        /// <summary>
        /// 光圈叶片数的最大值。
        /// 叶片越多，散景越接近圆形，最多 11 片产生接近完美的圆形散景。
        /// </summary>
        public const int kMaxBladeCount = 11;

        public Camera() {}

        /// <summary>
        /// 近裁剪面距离。相机将渲染从该距离到 farClipPlane 之间的物体。
        /// 值越小，近处的物体越能被看到；值过大可能导致近处物体被裁剪掉。
        /// 注意：过小的值（如 0.01）可能导致深度精度问题（z-fighting）。
        /// </summary>
        [NativeProperty("Near")] extern public float nearClipPlane { get; set; }

        /// <summary>
        /// 远裁剪面距离。相机将渲染从 nearClipPlane 到该距离之间的物体。
        /// 值越大，能看到的远处物体越多，但会降低深度缓冲区的精度。
        /// </summary>
        [NativeProperty("Far")]  extern public float farClipPlane  { get; set; }

        /// <summary>
        /// 相机的视野（Field of View），单位为度。
        /// 在透视模式下，控制相机可以看到的垂直角度范围。
        /// 典型值：60-90 度（FPS 游戏常用 60-75，赛车游戏常用 80-90）。
        /// </summary>
        [NativeProperty("VerticalFieldOfView")]  extern public float fieldOfView   { get; set; }

        /// <summary>
        /// 渲染路径（Rendering Path），决定 Unity 如何渲染场景。
        /// - UsePlayerSettings: 使用项目设置中的默认值
        /// - Forward: 前向渲染，每个物体逐像素计算光照
        /// - DeferredShading: 延迟渲染，将光照计算延迟到屏幕空间
        /// 注意：SRP（可编程渲染管线）下此设置可能被覆盖。
        /// </summary>
        extern public RenderingPath renderingPath { get; set; }

        /// <summary>
        /// 相机实际使用的渲染路径（只读）。
        /// 可能与 renderingPath 不同，因为某些平台或设置不支持所选的路径，
        /// Unity 会自动降级到可用的渲染路径。
        /// </summary>
        extern public RenderingPath actualRenderingPath {[NativeName("CalculateRenderingPath")] get;  }

        /// <summary>
        /// 重置相机参数到默认值。
        /// </summary>
        extern public void Reset();

        /// <summary>
        /// 是否允许 HDR（高动态范围）渲染。
        /// 启用后，相机使用高精度浮点格式的渲染纹理，保留更丰富的亮部和暗部细节。
        /// 需要与 Tone Mapping（色调映射）配合使用才能正确显示。
        /// </summary>
        extern public bool allowHDR { get; set; }

        /// <summary>
        /// 是否允许 MSAA（多重采样抗锯齿）。
        /// 通过采样多个子像素位置并平均来减少锯齿边缘。
        /// 注意：延迟渲染路径不支持 MSAA，HDR 开启时 MSAA 可能被禁用。
        /// </summary>
        extern public bool allowMSAA { get; set; }

        /// <summary>
        /// 是否允许动态分辨率渲染。
        /// 启用后，Unity 可以根据性能负载动态调整渲染分辨率，
        /// 以维持目标帧率。常用于主机和移动平台的性能优化。
        /// </summary>
        extern public bool allowDynamicResolution { get; set; }

        /// <summary>
        /// 是否强制将相机渲染到 RenderTexture。
        /// 启用后，即使没有设置 targetTexture，相机也会渲染到临时的 RenderTexture，
        /// 而不是直接渲染到屏幕。这对于后处理效果和 SRP 是必要的。
        /// </summary>
        [NativeProperty("ForceIntoRT")] extern public bool forceIntoRenderTexture { get; set; }

        /// <summary>
        /// 正交相机的投影大小（视口高度的一半）。
        /// 仅在 orthographic = true 时有效。值越大，相机看到的范围越广。
        /// 2D 游戏中常用此属性来控制可见区域。
        /// </summary>
        extern public float orthographicSize { get; set; }

        /// <summary>
        /// 是否使用正交投影（Orthographic Projection）。
        /// true = 2D 正交模式（无透视效果，物体大小不随距离变化）；
        /// false = 3D 透视模式（有近大远小效果）。
        /// </summary>
        extern public bool  orthographic { get; set; }

        /// <summary>
        /// 不透明物体的排序模式。
        /// - Default: 默认模式（通常是从前到后，以优化 overdraw）
        /// - FrontToBack: 从前到后渲染（减少 overdraw，提高性能）
        /// - NoDistanceSort: 不按距离排序（按提交顺序渲染）
        /// </summary>
        extern public OpaqueSortMode opaqueSortMode { get; set; }

        /// <summary>
        /// 透明物体的排序模式。
        /// - Default: 默认模式
        /// - Perspective: 按透视距离排序
        /// - Orthographic: 按正交距离排序
        /// - CustomAxis: 按自定义轴排序
        /// 透明物体需要从远到近渲染才能得到正确的混合效果。
        /// </summary>
        extern public TransparencySortMode transparencySortMode { get; set; }

        /// <summary>
        /// 透明物体排序的自定义轴。
        /// 当 transparencySortMode = CustomAxis 时使用。
        /// </summary>
        extern public Vector3 transparencySortAxis { get; set; }

        /// <summary>
        /// 重置透明排序设置为默认值。
        /// </summary>
        extern public void ResetTransparencySortSettings();

        /// <summary>
        /// 相机的深度值，用于控制多相机的渲染顺序。
        /// 深度值较小的相机先渲染，深度值较大的相机后渲染（覆盖在先渲染的画面上）。
        /// 常用于 UI 相机（深度较大）和主游戏相机（深度较小）的叠加。
        /// </summary>
        extern public float depth { get; set; }

        /// <summary>
        /// 相机的宽高比（宽度 / 高度）。
        /// 默认情况下自动匹配屏幕或 targetTexture 的宽高比。
        /// 修改此值可以产生类似宽银幕电影的效果。
        /// </summary>
        extern public float aspect { get; set; }

        /// <summary>
        /// 重置宽高比为默认值（匹配屏幕或 targetTexture）。
        /// </summary>
        extern public void ResetAspect();

        /// <summary>
        /// 相机的速度向量（只读），用于运动模糊等效果。
        /// 表示相机在当前帧的移动速度和方向。
        /// </summary>
        extern public Vector3 velocity { get; }

        /// <summary>
        /// 相机的剔除遮罩（Culling Mask），以 LayerMask 形式控制渲染哪些层。
        /// 这是一个 32 位整数，每一位对应一个 Layer（0-31）。
        /// 例如：cullingMask = 1 << 8 表示只渲染 Layer 8 上的物体。
        /// 常用技巧：cullingMask = ~0 渲染所有层；cullingMask = 0 不渲染任何物体。
        /// </summary>
        extern public int cullingMask { get; set; }

        /// <summary>
        /// 事件遮罩（Event Mask），控制哪些层上的物体可以接收鼠标事件。
        /// 与 cullingMask 类似，但用于事件系统而非渲染。
        /// </summary>
        extern public int eventMask { get; set; }
        public bool layerCullSpherical
        {
            get { return layerCullSphericalInternal; }
            set
            {
                if (GraphicsSettings.isScriptableRenderPipelineEnabled)
                {
                    Debug.LogWarning("Your project uses a scriptable render pipeline. You can use Camera.layerCullSpherical only with the built-in renderer.");
                }

                layerCullSphericalInternal = value;
            }
        }
        [NativeProperty("LayerCullSpherical")]extern internal bool layerCullSphericalInternal { get; set; }

        /// <summary>
        /// 相机类型，标识相机的用途。
        /// - Game: 游戏相机（主相机）
        /// - SceneView: 编辑器场景视图相机
        /// - Preview: 预览相机（如材质预览）
        /// - VR: VR 相机
        /// - Reflection: 反射探针相机
        /// 这是一个 Flags 枚举，可以组合使用。
        /// </summary>
        extern public CameraType cameraType { get; set; }

        extern internal Material skyboxMaterial { get; }

        [NativeConditional("UNITY_EDITOR")]
        extern public ulong  overrideSceneCullingMask     { get; set; }

        [NativeConditional("UNITY_EDITOR")]
        extern internal ulong sceneCullingMask { get; }

        [NativeConditional("UNITY_EDITOR")]
        extern internal bool useInteractiveLightBakingData { get; set; }

        [FreeFunction("CameraScripting::GetLayerCullDistances", HasExplicitThis = true)] extern private float[] GetLayerCullDistances();
        [FreeFunction("CameraScripting::SetLayerCullDistances", HasExplicitThis = true)] extern private void SetLayerCullDistances([NotNull] float[] d);

        /// <summary>
        /// 每层的剔除距离数组，长度为 32（对应 32 个 Layer）。
        /// 可以单独控制每个 Layer 上物体的最大可见距离。
        /// 例如：设置 Layer 8 的距离为 50，则该层上超过 50 米的物体将被剔除。
        /// 用于性能优化，让远处的细节层物体不被渲染。
        /// </summary>
        public float[] layerCullDistances
        {
            get { return GetLayerCullDistances(); }
            set
            {
                if (value.Length != 32)
                    throw new UnityException("Array needs to contain exactly 32 floats for layerCullDistances.");
                SetLayerCullDistances(value);
            }
        }

        [Obsolete("PreviewCullingLayer is obsolete. Use scene culling masks instead.", false)]
        internal static int PreviewCullingLayer { get { return 31; } } // Return 31 because this used to be the PreviewCullingLayer stored in kPreviewLayer in Camera.h

        /// <summary>
        /// 是否使用遮挡剔除（Occlusion Culling）。
        /// 启用后，被其他物体完全遮挡的物体不会被渲染，从而提高性能。
        /// 需要预先烘焙遮挡数据（Occlusion Data）。
        /// </summary>
        extern public bool useOcclusionCulling { get; set; }

        /// <summary>
        /// 用于剔除的矩阵。定义了相机用于可见性测试的视锥体变换。
        /// 通常与投影矩阵和视图矩阵相关，但可以单独设置以实现特殊剔除效果。
        /// </summary>
        extern public Matrix4x4 cullingMatrix { get; set; }

        /// <summary>
        /// 重置剔除矩阵为默认值（基于相机的投影和视图矩阵）。
        /// </summary>
        extern public void ResetCullingMatrix();

        /// <summary>
        /// 相机的背景色。当 clearFlags 设置为 Color（或 SolidColor）时，
        /// 相机在渲染前会用此颜色清空屏幕。
        /// </summary>
        extern public Color backgroundColor { get; set; }

        /// <summary>
        /// 相机的清除标志，决定相机在渲染前如何清除屏幕内容。
        /// - Skybox: 使用天空盒清除（默认）
        /// - Color/SolidColor: 使用纯色清除（backgroundColor）
        /// - Depth: 只清除深度缓冲区
        /// - Nothing: 不清除任何内容（叠加多个相机时使用）
        /// </summary>
        extern public CameraClearFlags clearFlags { get; set; }

        /// <summary>
        /// 深度纹理模式，控制相机生成哪些深度相关的纹理。
        /// - None: 不生成
        /// - Depth: 生成深度纹理（_CameraDepthTexture）
        /// - DepthNormals: 生成深度+法线纹理（_CameraDepthNormalsTexture）
        /// - MotionVectors: 生成运动向量纹理（_CameraMotionVectorsTexture）
        /// 这些纹理可以在 Shader 中用于各种后处理效果。
        /// </summary>
        extern public DepthTextureMode depthTextureMode { get; set; }

        /// <summary>
        /// 是否在光照 Pass 之后清除模板缓冲区。
        /// 用于延迟渲染路径中，在光照计算完成后清除模板数据。
        /// </summary>
        extern public bool clearStencilAfterLightingPass { get; set; }

        /// <summary>
        /// 设置替换着色器（Replacement Shader）。
        /// 所有物体将使用指定的着色器渲染，而不是它们自己的着色器。
        /// replacementTag 参数可以进一步筛选使用替换着色器的 Pass。
        /// 常用于特殊效果（如 X 光透视、高亮显示、深度图生成等）。
        /// </summary>
        extern public void SetReplacementShader(Shader shader, string replacementTag);

        /// <summary>
        /// 重置替换着色器，恢复物体使用自己的着色器渲染。
        /// </summary>
        extern public void ResetReplacementShader();

        internal enum ProjectionMatrixMode{ Explicit, Implicit, PhysicalPropertiesBased }

        extern internal ProjectionMatrixMode projectionMatrixMode { get; }

        /// <summary>
        /// 门适配模式（Gate Fit Mode），用于物理相机。
        /// 当传感器的宽高比与显示设备的宽高比不匹配时，
        /// 决定如何适配：Vertical（垂直适配）、Horizontal（水平适配）、
        /// Fill（填充）、Overscan（过扫描）、None（不适配）。
        /// </summary>
        public enum GateFitMode{ Vertical = 1 , Horizontal = 2, Fill = 3, Overscan = 4, None = 0 }

        /// <summary>
        /// 是否使用物理相机属性。
        /// 启用后，可以使用真实的相机参数（光圈、快门速度、ISO、焦距等）
        /// 来控制曝光和景深效果，模拟真实相机的行为。
        /// </summary>
        extern public bool usePhysicalProperties { get; set; }

        /// <summary>
        /// ISO 感光度（物理相机属性）。控制相机对光的敏感度。
        /// 与 aperture 和 shutterSpeed 共同决定最终曝光。
        /// 典型值：100-6400。
        /// </summary>
        extern public int iso  { get; set; }

        /// <summary>
        /// 快门速度（物理相机属性），单位为秒。
        /// 控制传感器曝光时间。较慢的快门（如 1/30）进光更多但可能产生运动模糊。
        /// 典型值：1/30（0.033s）到 1/2000（0.0005s）。
        /// </summary>
        extern public float shutterSpeed  { get; set; }

        /// <summary>
        /// 光圈值（物理相机属性），即 f-stop 值。
        /// 控制镜头进光量，也影响景深效果。
        /// 小光圈（如 f/1.4）进光多、景深浅；大光圈（如 f/16）进光少、景深深。
        /// 范围：kMinAperture(0.7) 到 kMaxAperture(32)。
        /// </summary>
        extern public float aperture  { get; set; }

        /// <summary>
        /// 对焦距离（物理相机属性），单位为米。
        /// 控制景深效果中对焦平面的位置。
        /// 在此距离上的物体最清晰，远离此距离的物体逐渐模糊。
        /// </summary>
        extern public float focusDistance  { get; set; }

        /// <summary>
        /// 焦距（物理相机属性），单位为毫米。
        /// 控制镜头的视角：短焦距（如 24mm）视野广，长焦距（如 200mm）视野窄。
        /// 与 sensorSize 共同决定实际的视野角度。
        /// </summary>
        extern public float focalLength  { get; set; }

        /// <summary>
        /// 光圈叶片数（物理相机属性）。
        /// 影响散景（Bokeh）的形状。叶片越少，散景越呈多边形；
        /// 叶片越多，散景越接近圆形。范围：kMinBladeCount(3) 到 kMaxBladeCount(11)。
        /// </summary>
        extern public int bladeCount  { get; set; }

        /// <summary>
        /// 镜头曲率（物理相机属性），控制散景的像散效果。
        /// X 分量控制水平方向的曲率，Y 分量控制垂直方向的曲率。
        /// </summary>
        extern public Vector2 curvature  { get; set; }

        /// <summary>
        /// 桶形裁剪（物理相机属性），控制镜头暗角效果。
        /// 值越大，图像边缘越暗，模拟真实镜头的渐晕（Vignetting）现象。
        /// </summary>
        extern public float barrelClipping  { get; set; }

        /// <summary>
        /// 变形宽银幕拉伸（物理相机属性）。
        /// 控制水平方向的拉伸比例，模拟变形镜头（Anamorphic Lens）的效果。
        /// 正值产生水平拉伸，负值产生垂直拉伸。
        /// </summary>
        extern public float anamorphism  { get; set; }

        /// <summary>
        /// 传感器尺寸（物理相机属性），单位为毫米。
        /// X = 宽度，Y = 高度。不同的传感器尺寸影响视野和景深。
        /// 常见尺寸：全画幅（36x24mm）、APS-C（22.2x14.8mm）等。
        /// </summary>
        extern public Vector2 sensorSize  { get; set; }

        /// <summary>
        /// 镜头偏移（物理相机属性），单位为毫米。
        /// 模拟移轴镜头（Tilt-Shift Lens）的效果，控制传感器相对于镜头光轴的偏移。
        /// 用于校正透视畸变或创建特殊效果。
        /// </summary>
        extern public Vector2 lensShift  { get; set; }

        /// <summary>
        /// 门适配模式（物理相机属性）。
        /// 当传感器宽高比与渲染目标宽高比不一致时的适配策略。
        /// </summary>
        extern public GateFitMode gateFit  { get; set; }

        /// <summary>
        /// 视野轴枚举，指定 fieldOfView 是垂直还是水平视野。
        /// </summary>
        public enum FieldOfViewAxis { Vertical, Horizontal }

        /// <summary>
        /// 获取门适配后的实际视野角度。
        /// 当 gateFit 模式改变视口大小时，实际的 FOV 可能发生变化。
        /// </summary>
        extern public float GetGateFittedFieldOfView();

        /// <summary>
        /// 获取门适配后的实际镜头偏移。
        /// </summary>
        extern public Vector2 GetGateFittedLensShift();

        extern internal Vector3 GetLocalSpaceAim();

        /// <summary>
        /// 相机的视口矩形（归一化坐标）。
        /// 定义相机渲染到屏幕上的位置和大小，所有值在 0-1 之间。
        /// 例如：new Rect(0, 0, 0.5f, 1) 表示相机只渲染左半屏。
        /// 常用于分屏游戏（Split-Screen）和小地图（Minimap）。
        /// </summary>
        [NativeProperty("NormalizedViewportRect")] extern public Rect rect      { get; set; }

        /// <summary>
        /// 相机的视口矩形（像素坐标）。
        /// 与 rect 类似，但使用像素值而非归一化值。
        /// 例如：new Rect(0, 0, 960, 1080) 表示渲染到屏幕左半部分。
        /// </summary>
        [NativeProperty("ScreenViewportRect")]     extern public Rect pixelRect { get; set; }

        /// <summary>
        /// 相机渲染目标的像素宽度（只读）。
        /// 如果 targetTexture 不为 null，则返回 RenderTexture 的宽度；
        /// 否则返回屏幕宽度。
        /// </summary>
        extern public int pixelWidth  {[FreeFunction("CameraScripting::GetPixelWidth",  HasExplicitThis = true)] get; }

        /// <summary>
        /// 相机渲染目标的像素高度（只读）。
        /// 如果 targetTexture 不为 null，则返回 RenderTexture 的高度；
        /// 否则返回屏幕高度。
        /// </summary>
        extern public int pixelHeight {[FreeFunction("CameraScripting::GetPixelHeight", HasExplicitThis = true)] get; }

        /// <summary>
        /// 缩放后的像素宽度（只读）。
        /// 当使用动态分辨率或渲染缩放时，返回实际渲染分辨率宽度。
        /// </summary>
        extern public int scaledPixelWidth  {[FreeFunction("CameraScripting::GetScaledPixelWidth",  HasExplicitThis = true)] get; }

        /// <summary>
        /// 缩放后的像素高度（只读）。
        /// 当使用动态分辨率或渲染缩放时，返回实际渲染分辨率高度。
        /// </summary>
        extern public int scaledPixelHeight {[FreeFunction("CameraScripting::GetScaledPixelHeight", HasExplicitThis = true)] get; }

        /// <summary>
        /// 渲染目标纹理（RenderTexture）。
        /// 设置后，相机将渲染到该纹理而非屏幕。
        /// 常用于：安全摄像头画面、小地图、反射效果、后处理链等。
        /// 设置为 null 则恢复渲染到屏幕。
        /// </summary>
        extern public RenderTexture targetTexture { get; set; }

        /// <summary>
        /// 相机当前实际渲染到的纹理（只读）。
        /// 可能不同于 targetTexture，例如在编辑器中可能渲染到预览纹理。
        /// </summary>
        extern public RenderTexture activeTexture {[NativeName("GetCurrentTargetTexture")] get; }

        /// <summary>
        /// 目标显示器的索引。
        /// 在多显示器设置中，指定相机渲染到哪个显示器。
        /// 0 = 主显示器，1 = 第二个显示器，以此类推。
        /// </summary>
        extern public int targetDisplay { get; set; }

        [FreeFunction("CameraScripting::SetTargetBuffers",  HasExplicitThis = true)] extern private void SetTargetBuffersImpl(RenderBuffer color, RenderBuffer depth);

        /// <summary>
        /// 设置相机的颜色缓冲区和深度缓冲区。
        /// 允许将相机渲染到自定义的 RenderBuffer，而不是完整的 RenderTexture。
        /// 常用于高级渲染技术，如自定义的 MRT（多渲染目标）设置。
        /// </summary>
        public void SetTargetBuffers(RenderBuffer colorBuffer, RenderBuffer depthBuffer) { SetTargetBuffersImpl(colorBuffer, depthBuffer); }

        [FreeFunction("CameraScripting::SetTargetBuffers",  HasExplicitThis = true)] extern private void SetTargetBuffersMRTImpl(RenderBuffer[] color, RenderBuffer depth);

        /// <summary>
        /// 设置相机的多个颜色缓冲区（MRT）和一个深度缓冲区。
        /// 允许同时渲染到多个 RenderBuffer，用于延迟渲染等需要多输出 Pass 的技术。
        /// </summary>
        public void SetTargetBuffers(RenderBuffer[] colorBuffer, RenderBuffer depthBuffer) { SetTargetBuffersMRTImpl(colorBuffer, depthBuffer); }

        extern internal string[] GetCameraBufferWarnings();

        /// <summary>
        /// 相机到世界的变换矩阵（只读）。
        /// 将点从相机局部空间变换到世界空间。
        /// 等价于 worldToCameraMatrix 的逆矩阵。
        /// </summary>
        extern public Matrix4x4 cameraToWorldMatrix { get; }

        /// <summary>
        /// 世界到相机的变换矩阵（视图矩阵）。
        /// 将点从世界空间变换到相机局部空间（观察空间）。
        /// 这个矩阵包含了相机的旋转和平移信息。
        /// </summary>
        extern public Matrix4x4 worldToCameraMatrix { get; set; }

        /// <summary>
        /// 投影矩阵。将点从相机空间变换到裁剪空间（NDC）。
        /// 透视投影产生近大远小效果，正交投影产生等大效果。
        /// 修改此矩阵可以实现特殊效果（如反射、斜视锥体等）。
        /// </summary>
        extern public Matrix4x4 projectionMatrix    { get; set; }

        /// <summary>
        /// 未添加抖动（Jitter）的投影矩阵。
        /// 用于 TAA（时序抗锯齿）等需要知道原始投影矩阵的技术。
        /// 抖动是每帧对投影矩阵施加微小偏移以进行子像素采样。
        /// </summary>
        extern public Matrix4x4 nonJitteredProjectionMatrix { get; set; }

        /// <summary>
        /// 是否对透明物体使用抖动投影矩阵进行渲染。
        /// 启用后，透明物体也会使用添加了抖动的投影矩阵，
        /// 以保持与不透明物体在 TAA 下的一致性。
        /// </summary>
        [NativeProperty("UseJitteredProjectionMatrixForTransparent")] extern public bool useJitteredProjectionMatrixForTransparentRendering { get; set; }

        /// <summary>
        /// 上一帧的视图投影矩阵（只读）。
        /// 用于运动向量计算，通过比较上一帧和当前帧的 VP 矩阵
        /// 来计算像素在屏幕上的运动方向和速度。
        /// </summary>
        extern public Matrix4x4 previousViewProjectionMatrix { get; }

        /// <summary>
        /// 重置世界到相机矩阵为默认值（基于 Transform 组件）。
        /// </summary>
        extern public void ResetWorldToCameraMatrix();

        /// <summary>
        /// 重置投影矩阵为默认值（基于 fieldOfView、nearClipPlane 等参数）。
        /// </summary>
        extern public void ResetProjectionMatrix();

        /// <summary>
        /// 计算斜视锥体矩阵（Oblique Matrix）。
        /// 通过自定义裁剪平面来修改投影矩阵，常用于：
        /// - 水面反射相机（裁剪掉水面以上的物体）
        /// - 视锥体裁剪优化
        /// clipPlane 参数是一个四维向量，表示裁剪平面方程。
        /// </summary>
        [FreeFunction("CameraScripting::CalculateObliqueMatrix", HasExplicitThis = true)] extern public Matrix4x4 CalculateObliqueMatrix(Vector4 clipPlane);

        /// <summary>
        /// 将世界坐标转换为屏幕坐标。
        /// 返回的 Vector3 中，x 和 y 是屏幕像素坐标（左下角为原点），
        /// z 是距离相机的深度值（单位为世界单位）。
        /// 常用于：UI 元素跟随 3D 物体、屏幕标记等。
        /// </summary>
        extern public Vector3 WorldToScreenPoint(Vector3 position, MonoOrStereoscopicEye eye);

        /// <summary>
        /// 将世界坐标转换为视口坐标。
        /// 返回的 Vector3 中，x 和 y 是归一化视口坐标（0-1），
        /// z 是距离相机的深度值。
        /// (0,0) 为视口左下角，(1,1) 为右上角。
        /// </summary>
        extern public Vector3 WorldToViewportPoint(Vector3 position, MonoOrStereoscopicEye eye);

        /// <summary>
        /// 将视口坐标转换为世界坐标。
        /// 输入 (x, y, z) 中，x 和 y 是归一化视口坐标（0-1），
        /// z 是距离相机的深度值（世界单位）。
        /// 常用于：根据鼠标位置计算 3D 空间中的点。
        /// </summary>
        extern public Vector3 ViewportToWorldPoint(Vector3 position, MonoOrStereoscopicEye eye);

        /// <summary>
        /// 将屏幕坐标转换为世界坐标。
        /// 输入 (x, y, z) 中，x 和 y 是屏幕像素坐标，
        /// z 是距离相机的深度值（世界单位）。
        /// 注意：z 值必须在 nearClipPlane 和 farClipPlane 之间才能得到有效结果。
        /// </summary>
        extern public Vector3 ScreenToWorldPoint(Vector3 position, MonoOrStereoscopicEye eye);

        /// <summary>
        /// 将世界坐标转换为屏幕坐标（单目模式，默认左眼）。
        /// </summary>
        public Vector3 WorldToScreenPoint(Vector3 position) { return WorldToScreenPoint(position, MonoOrStereoscopicEye.Mono); }

        /// <summary>
        /// 将世界坐标转换为视口坐标（单目模式）。
        /// </summary>
        public Vector3 WorldToViewportPoint(Vector3 position) { return WorldToViewportPoint(position, MonoOrStereoscopicEye.Mono); }

        /// <summary>
        /// 将视口坐标转换为世界坐标（单目模式）。
        /// </summary>
        public Vector3 ViewportToWorldPoint(Vector3 position) { return ViewportToWorldPoint(position, MonoOrStereoscopicEye.Mono); }

        /// <summary>
        /// 将屏幕坐标转换为世界坐标（单目模式）。
        /// </summary>
        public Vector3 ScreenToWorldPoint(Vector3 position) { return ScreenToWorldPoint(position, MonoOrStereoscopicEye.Mono); }

        /// <summary>
        /// 将屏幕坐标转换为视口坐标。
        /// 屏幕坐标以像素为单位，视口坐标以归一化值（0-1）表示。
        /// 常用于：将鼠标位置从像素转换为归一化值。
        /// </summary>
        extern public Vector3 ScreenToViewportPoint(Vector3 position);

        /// <summary>
        /// 将视口坐标转换为屏幕坐标。
        /// 视口坐标（0-1）转换为屏幕像素坐标。
        /// </summary>
        extern public Vector3 ViewportToScreenPoint(Vector3 position);

        extern internal Vector2 GetFrustumPlaneSizeAt(float distance);

        extern private Ray ViewportPointToRay(Vector2 pos, MonoOrStereoscopicEye eye);

        /// <summary>
        /// 从视口坐标发射一条射线。
        /// 输入 (x, y, z) 中，x 和 y 是归一化视口坐标（0-1），
        /// z 被忽略（射线从近裁剪面出发）。
        /// 返回的 Ray 包含射线的起点和方向。
        /// 常用于：根据鼠标位置进行拾取（Picking）检测。
        /// </summary>
        public Ray ViewportPointToRay(Vector3 pos, MonoOrStereoscopicEye eye) { return ViewportPointToRay((Vector2)pos, eye); }

        /// <summary>
        /// 从视口坐标发射射线（单目模式）。
        /// </summary>
        public Ray ViewportPointToRay(Vector3 pos) { return ViewportPointToRay(pos, MonoOrStereoscopicEye.Mono); }

        extern private Ray ScreenPointToRay(Vector2 pos, MonoOrStereoscopicEye eye);

        /// <summary>
        /// 从屏幕坐标发射一条射线。
        /// 输入 (x, y, z) 中，x 和 y 是屏幕像素坐标，
        /// z 被忽略（射线从近裁剪面出发）。
        /// 这是最常用的射线检测方法，通常与 Input.mousePosition 配合使用。
        /// 示例：Camera.main.ScreenPointToRay(Input.mousePosition)
        /// </summary>
        public Ray ScreenPointToRay(Vector3 pos, MonoOrStereoscopicEye eye) { return ScreenPointToRay((Vector2)pos, eye); }

        /// <summary>
        /// 从屏幕坐标发射射线（单目模式）。
        /// </summary>
        public Ray ScreenPointToRay(Vector3 pos) { return ScreenPointToRay(pos, MonoOrStereoscopicEye.Mono); }

        [FreeFunction("CameraScripting::CalculateViewportRayVectors", HasExplicitThis = true)]
        extern private void CalculateFrustumCornersInternal(Rect viewport, float z, MonoOrStereoscopicEye eye, [Out] Vector3[] outCorners);

        /// <summary>
        /// 计算视锥体在指定深度处的四个角的世界坐标。
        /// 用于：手动构建视锥体、自定义剔除、阴影计算等。
        /// outCorners 数组至少需要 4 个元素，按顺序返回：
        /// 左下、右下、左上、右上（在指定深度处的世界坐标）。
        /// </summary>
        public void CalculateFrustumCorners(Rect viewport, float z, MonoOrStereoscopicEye eye, Vector3[] outCorners)
        {
            if (outCorners == null)     throw new ArgumentNullException("outCorners");
            if (outCorners.Length < 4)  throw new ArgumentException("outCorners minimum size is 4", "outCorners");
            CalculateFrustumCornersInternal(viewport, z, eye, outCorners);
        }

        /// <summary>
        /// 门适配参数结构体，用于物理相机模式下控制传感器适配行为。
        /// </summary>
        public struct GateFitParameters
        {
            /// <summary>门适配模式</summary>
            public GateFitMode mode {get; set; }
            /// <summary>目标宽高比</summary>
            public float aspect {get; set; }

            public GateFitParameters(GateFitMode mode, float aspect)
            {
                this.mode = mode;
                this.aspect = aspect;
            }
        }

        [NativeName("CalculateProjectionMatrixFromPhysicalProperties")]
        extern private static void CalculateProjectionMatrixFromPhysicalPropertiesInternal(out Matrix4x4 output, float focalLength, Vector2 sensorSize, Vector2 lensShift, float nearClip, float farClip, float gateAspect, GateFitMode gateFitMode);

        /// <summary>
        /// 根据物理相机参数计算投影矩阵。
        /// 使用焦距、传感器尺寸、镜头偏移等真实相机参数来生成投影矩阵。
        /// 当 usePhysicalProperties = true 时，Unity 内部使用此方法。
        /// </summary>
        public static void CalculateProjectionMatrixFromPhysicalProperties(out Matrix4x4 output, float focalLength, Vector2 sensorSize, Vector2 lensShift, float nearClip, float farClip, GateFitParameters gateFitParameters = default(GateFitParameters))
        {
            CalculateProjectionMatrixFromPhysicalPropertiesInternal(out output, focalLength, sensorSize, lensShift, nearClip, farClip, gateFitParameters.aspect, gateFitParameters.mode);
        }

        /// <summary>
        /// 将焦距转换为视野角度。
        /// 焦距越短，视野越宽；焦距越长，视野越窄。
        /// sensorSize 是传感器在对应轴上的尺寸（毫米）。
        /// </summary>
        [NativeName("FocalLengthToFieldOfView_Safe")]
        extern public static float FocalLengthToFieldOfView(float focalLength, float sensorSize);

        /// <summary>
        /// 将视野角度转换为焦距。
        /// FocalLengthToFieldOfView 的逆运算。
        /// </summary>
        [NativeName("FieldOfViewToFocalLength_Safe")]
        extern public static float FieldOfViewToFocalLength(float fieldOfView, float sensorSize);

        /// <summary>
        /// 将水平视野转换为垂直视野。
        /// 根据给定的水平视野角度和宽高比计算对应的垂直视野角度。
        /// </summary>
        [NativeName("HorizontalToVerticalFieldOfView_Safe")]
        extern public static float HorizontalToVerticalFieldOfView(float horizontalFieldOfView, float aspectRatio);

        /// <summary>
        /// 将垂直视野转换为水平视野。
        /// HorizontalToVerticalFieldOfView 的逆运算。
        /// </summary>
        extern public static float VerticalToHorizontalFieldOfView(float verticalFieldOfView, float aspectRatio);

        /// <summary>
        /// 主相机（只读）。场景中第一个被启用的、Tag 为 "MainCamera" 的相机。
        /// 如果没有找到符合条件的相机，返回 null。
        /// 每个场景有且只有一个主相机。
        /// </summary>
        extern public static Camera main {[FreeFunction("FindMainCamera")] get; }

        /// <summary>
        /// 当前正在渲染的相机（只读）。
        /// 在 OnPreRender、OnPostRender 等相机事件中访问此属性，
        /// 可以获取当前正在执行渲染的相机实例。
        /// </summary>
        public static Camera current {
            get
            {
                return currentInternal;
            }
        }
        extern private static Camera currentInternal { [FreeFunction("GetCurrentCameraPPtr")] get; }

        /// <summary>
        /// 相机所属的场景（Scene）。
        /// 可以获取或设置相机所在的场景，用于多场景编辑。
        /// </summary>
        extern public UnityEngine.SceneManagement.Scene scene
        {
            [FreeFunction("CameraScripting::GetScene", HasExplicitThis = true)] get;
            [FreeFunction("CameraScripting::SetScene", HasExplicitThis = true)] set;
        }

        /// <summary>
        /// 立体眼睛枚举，用于 VR/AR 渲染。
        /// Left = 左眼，Right = 右眼。
        /// </summary>
        public enum StereoscopicEye { Left, Right }

        /// <summary>
        /// 单目或立体眼睛枚举。
        /// Left = 左眼，Right = 右眼，Mono = 单目（非 VR 模式）。
        /// </summary>
        public enum MonoOrStereoscopicEye { Left, Right, Mono }

        /// <summary>
        /// 是否启用了立体渲染（VR/AR）（只读）。
        /// 当 VR 设备已连接且相机配置为立体渲染时返回 true。
        /// </summary>
        extern public bool stereoEnabled
        {
            [NativeMethod("GetStereoEnabledForBuiltInOrSRP")]
            get;
        }

        /// <summary>
        /// 立体渲染时左右眼之间的分离距离（视差距离）。
        /// 值越大，立体感越强，但过大会导致视觉不适。
        /// 通常设置为 0.02-0.064（米）。
        /// </summary>
        extern public float stereoSeparation  { get; set; }

        /// <summary>
        /// 立体渲染的会聚距离。
        /// 控制左右眼图像的会聚点，即立体效果中物体"出现"在屏幕平面的距离。
        /// </summary>
        extern public float stereoConvergence { get; set; }

        /// <summary>
        /// 左右眼视图矩阵是否在单次剔除容差内（只读）。
        /// 如果左右眼的位置足够接近，可以只进行一次视锥体裁剪，
        /// 然后将结果用于两只眼睛，从而提高性能。
        /// </summary>
        extern public bool  areVRStereoViewMatricesWithinSingleCullTolerance {[NativeName("AreVRStereoViewMatricesWithinSingleCullTolerance")] get; }

        /// <summary>
        /// 立体渲染的目标眼睛遮罩。
        /// - None: 不渲染到任何眼睛
        /// - Left: 只渲染到左眼
        /// - Right: 只渲染到右眼
        /// - Both: 渲染到双眼（默认）
        /// 注意：SRP 下此属性不可用，会发出警告。
        /// </summary>
        public StereoTargetEyeMask stereoTargetEye
        {
            get { return stereoTargetEyeInternal; }
            set
            {
                if (GraphicsSettings.isScriptableRenderPipelineEnabled)
                {
                    Debug.LogWarning("Your project uses a scriptable render pipeline. You can use Camera.stereoTargetEye only with the built-in renderer.");
                }

                stereoTargetEyeInternal = value;
            }
        }
        [NativeProperty("StereoTargetEye")]extern internal StereoTargetEyeMask stereoTargetEyeInternal { get; set; }

        /// <summary>
        /// 当前活跃的立体眼睛（只读）。
        /// 在 VR 渲染过程中，返回当前正在渲染的是左眼还是右眼。
        /// </summary>
        extern public MonoOrStereoscopicEye stereoActiveEye {[FreeFunction("CameraScripting::GetStereoActiveEye", HasExplicitThis = true)] get; }

        /// <summary>
        /// 获取指定眼睛的未抖动投影矩阵。
        /// 用于 TAA 等需要原始投影矩阵的立体渲染技术。
        /// </summary>
        extern public Matrix4x4 GetStereoNonJitteredProjectionMatrix(StereoscopicEye eye);

        /// <summary>
        /// 获取指定眼睛的视图矩阵。
        /// </summary>
        [FreeFunction("CameraScripting::GetStereoViewMatrix", HasExplicitThis = true)]
        extern public Matrix4x4 GetStereoViewMatrix(StereoscopicEye eye);

        /// <summary>
        /// 将 VR 设备的投影矩阵复制到非抖动投影矩阵。
        /// 用于保持与设备原生投影矩阵的一致性。
        /// </summary>
        extern public void CopyStereoDeviceProjectionMatrixToNonJittered(StereoscopicEye eye);

        /// <summary>
        /// 获取指定眼睛的投影矩阵。
        /// </summary>
        [FreeFunction("CameraScripting::GetStereoProjectionMatrix", HasExplicitThis = true)]
        extern public Matrix4x4 GetStereoProjectionMatrix(StereoscopicEye eye);

        /// <summary>
        /// 设置指定眼睛的投影矩阵。
        /// </summary>
        extern public void SetStereoProjectionMatrix(StereoscopicEye eye, Matrix4x4 matrix);

        /// <summary>
        /// 重置所有立体投影矩阵为默认值。
        /// </summary>
        extern public void ResetStereoProjectionMatrices();

        /// <summary>
        /// 设置指定眼睛的视图矩阵。
        /// </summary>
        extern public void SetStereoViewMatrix(StereoscopicEye eye, Matrix4x4 matrix);

        /// <summary>
        /// 重置所有立体视图矩阵为默认值。
        /// </summary>
        extern public void ResetStereoViewMatrices();

        [FreeFunction("CameraScripting::GetAllCamerasCount")] extern private static int GetAllCamerasCount();
        [FreeFunction("CameraScripting::GetAllCameras")] extern private static int GetAllCamerasImpl([Out][NotNull] Camera[] cam);

        /// <summary>
        /// 场景中所有相机的数量（只读）。
        /// </summary>
        public static int allCamerasCount { get { return GetAllCamerasCount(); } }

        /// <summary>
        /// 场景中所有相机的数组（只读）。
        /// 每次访问都会创建一个新数组，频繁调用时建议缓存。
        /// </summary>
        public static Camera[] allCameras
        {
            get { Camera[] cam = new Camera[allCamerasCount]; GetAllCamerasImpl(cam); return cam; }
        }

        /// <summary>
        /// 获取场景中所有相机并填充到指定数组。
        /// 相比 allCameras 属性，此方法可以避免分配新数组（使用预分配数组）。
        /// 数组长度必须 >= allCamerasCount。
        /// </summary>
        public static int GetAllCameras(Camera[] cameras)
        {
            if (cameras == null)
                throw new NullReferenceException();

            if (cameras.Length < allCamerasCount)
                throw new ArgumentException("Passed in array to fill with cameras is to small to hold the number of cameras. Use Camera.allCamerasCount to get the needed size.");
            return GetAllCamerasImpl(cameras);
        }

        [FreeFunction("CameraScripting::RenderToCubemap", HasExplicitThis = true)] extern private bool RenderToCubemapImpl(Texture tex, [uei.DefaultValue("63")] int faceMask);

        /// <summary>
        /// 将相机渲染到 Cubemap（立方体贴图）。
        /// 用于：动态反射探针、环境贴图捕获等。
        /// faceMask 控制渲染哪些面（默认 63 = 全部 6 个面）。
        /// </summary>
        public bool RenderToCubemap(Cubemap cubemap, int faceMask)          { return RenderToCubemapImpl(cubemap, faceMask); }

        /// <summary>
        /// 将相机渲染到 Cubemap（渲染所有 6 个面）。
        /// </summary>
        public bool RenderToCubemap(Cubemap cubemap)                        { return RenderToCubemapImpl(cubemap, 63); }

        /// <summary>
        /// 将相机渲染到 RenderTexture 格式的 Cubemap。
        /// </summary>
        public bool RenderToCubemap(RenderTexture cubemap, int faceMask)    { return RenderToCubemapImpl(cubemap, faceMask); }

        /// <summary>
        /// 将相机渲染到 RenderTexture 格式的 Cubemap（渲染所有 6 个面）。
        /// </summary>
        public bool RenderToCubemap(RenderTexture cubemap)                  { return RenderToCubemapImpl(cubemap, 63); }

        public enum SceneViewFilterMode
        {
            Off = 0,
            ShowFiltered = 1
        }

        [NativeConditional("UNITY_EDITOR")]
        extern private int GetFilterMode();

        [NativeConditional("UNITY_EDITOR")]
        public SceneViewFilterMode sceneViewFilterMode
        {
            get
            {
                return (SceneViewFilterMode)GetFilterMode();
            }
        }

        [NativeConditional("UNITY_EDITOR")]
        extern public bool renderCloudsInSceneView { get; set; }


        // TODO: it should be collapsed with others
        [NativeName("RenderToCubemap")] extern private bool RenderToCubemapEyeImpl(RenderTexture cubemap, int faceMask, MonoOrStereoscopicEye stereoEye);

        /// <summary>
        /// 将相机渲染到 Cubemap（支持指定立体眼睛）。
        /// 用于 VR 环境下的反射探针捕获。
        /// </summary>
        public bool RenderToCubemap(RenderTexture cubemap, int faceMask, MonoOrStereoscopicEye stereoEye)
        {
            return RenderToCubemapEyeImpl(cubemap, faceMask, stereoEye);
        }

        /// <summary>
        /// 手动触发相机渲染。
        /// 调用此方法会立即执行一次完整的渲染流程，不受相机是否启用的影响。
        /// 常用于：在需要时捕获特定相机的画面。
        /// </summary>
        [FreeFunction("CameraScripting::Render", HasExplicitThis = true)]            extern public void Render();

        /// <summary>
        /// 使用替换着色器渲染相机。
        /// 所有物体将使用指定的着色器渲染，replacementTag 控制使用哪个 Pass。
        /// 常用于：生成深度图、法线图、高亮显示等特殊效果。
        /// </summary>
        [FreeFunction("CameraScripting::RenderWithShader", HasExplicitThis = true)]  extern public void RenderWithShader(Shader shader, string replacementTag);

        /// <summary>
        /// 渲染相机但不恢复状态。
        /// 与 Render() 类似，但渲染完成后不恢复相机的状态。
        /// 用于需要连续渲染多个相机且不需要中间状态恢复的性能优化场景。
        /// </summary>
        [FreeFunction("CameraScripting::RenderDontRestore", HasExplicitThis = true)] extern public void RenderDontRestore();

        /// <summary>
        /// 提交渲染请求（Render Request）。
        /// 这是 SRP 中用于请求特定类型渲染的通用接口。
        /// 支持 StandardRequest（标准渲染）和 ObjectIdRequest（对象 ID 渲染）等。
        /// 需要当前渲染管线支持该请求类型。
        /// </summary>
        public void SubmitRenderRequest<RequestData>(RequestData renderRequest)
        {
            if (renderRequest == null)
                throw new ArgumentException($"{nameof(SubmitRenderRequest)} is invoked with invalid renderRequests");

            if (renderRequest is ObjectIdRequest objectIdRequest)
            {
                if (objectIdRequest.destination.depthStencilFormat == Experimental.Rendering.GraphicsFormat.None)
                {
                    Debug.LogWarning("ObjectId Render Request submitted without a depth stencil, which can produce results that are not depth tested correctly");
                }
                if (GraphicsSettings.currentRenderPipeline == null || !RenderPipelineManager.currentPipeline.IsRenderRequestSupported(this, objectIdRequest))
                {
                    // 如果渲染管线支持对象 ID 渲染，让管线处理；
                    // 否则使用内置的"魔法"支持（在编辑器中同样适用于 HDRP/URP 着色器）
                    HandleBuiltInObjectIDRenderRequest(objectIdRequest);
                    return;
                }
            }
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Debug.LogWarning("Trying to invoke 'SubmitRenderRequest' when no SRP is set. A scriptable render pipeline is needed for this function call");
                return;
            }
            SubmitRenderRequestsInternal(renderRequest);
        }

        void HandleBuiltInObjectIDRenderRequest(ObjectIdRequest renderRequest)
        {
            UnityEngine.Object[] objects;
            objects = SubmitBuiltInObjectIDRenderRequest(
                renderRequest.destination,
                renderRequest.mipLevel,
                renderRequest.face,
                renderRequest.slice);
            renderRequest.result = new ObjectIdResult(objects);
        }

        [FreeFunction("CameraScripting::SubmitRenderRequests", HasExplicitThis = true)]  extern private void SubmitRenderRequestsInternal(object requests);
        [FreeFunction("CameraScripting::SubmitBuiltInObjectIDRenderRequest", HasExplicitThis = true)] [NativeConditional("UNITY_EDITOR")] 
        extern private UnityEngine.Object[] SubmitBuiltInObjectIDRenderRequest(
            RenderTexture target,
            int mipLevel,
            CubemapFace cubemapFace,
            int depthSlice);

        /// <summary>
        /// 相机是否正在处理渲染请求（只读）。
        /// 在 SubmitRenderRequest 的渲染过程中为 true。
        /// </summary>
        extern public bool isProcessingRenderRequest
        {
            [NativeMethod("IsProcessingRenderRequest")]
            get;
        }

        /// <summary>
        /// 设置当前相机（静态方法）。
        /// 用于在 Camera.current 中指定当前活动的相机。
        /// 通常在自定义渲染循环中手动设置。
        /// </summary>
        [FreeFunction("CameraScripting::SetupCurrent")] extern public static void SetupCurrent(Camera cur);

        /// <summary>
        /// 从另一个相机复制所有属性到当前相机。
        /// 相当于相机的"克隆"操作，但不会创建新相机实例。
        /// </summary>
        [FreeFunction("CameraScripting::CopyFrom", HasExplicitThis = true)] extern public void CopyFrom(Camera other);

        /// <summary>
        /// 当前相机上绑定的 CommandBuffer 数量（只读）。
        /// </summary>
        extern public int  commandBufferCount { get; }
        [NativeName("RemoveCommandBuffers")] extern void RemoveCommandBuffersImpl(CameraEvent evt);
        [NativeName("RemoveAllCommandBuffers")] extern void RemoveAllCommandBuffersImpl();

        static void LogWarningOnlyBuiltIn([CallerMemberName] string memberName = "")
        {
            Debug.LogWarning($"Your project uses a scriptable render pipeline. You can use Camera.{memberName} only with the built-in renderer.");
        }

        /// <summary>
        /// 移除指定相机事件上的所有 CommandBuffer。
        /// 注意：SRP 下此方法不可用，会发出警告。
        /// </summary>
        public void RemoveCommandBuffers(CameraEvent evt)
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            else
            {
                m_NonSerializedVersion++;
                RemoveCommandBuffersImpl(evt);
            }
        }

        /// <summary>
        /// 移除当前相机上的所有 CommandBuffer。
        /// 注意：SRP 下此方法不可用，会发出警告。
        /// </summary>
        public void RemoveAllCommandBuffers()
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            else
            {
			    m_NonSerializedVersion++;
                RemoveAllCommandBuffersImpl();
            }
        }

        // 在旧的绑定中，这些函数的代码是这样的：
        //   self->AddCommandBuffer(evt, &*buffer);
        // 这种解引用会产生空引用异常（与普通的"参数为空"异常不同）
        // 我们希望保留这种行为

        // extern public void AddCommandBuffer(CameraEvent evt, [NotNull] CommandBuffer buffer);
        // extern public void RemoveCommandBuffer(CameraEvent evt, [NotNull] CommandBuffer buffer);
        [NativeName("AddCommandBuffer")]      extern private void AddCommandBufferImpl(CameraEvent evt, [NotNull] CommandBuffer buffer);
        [NativeName("AddCommandBufferAsync")] extern private void AddCommandBufferAsyncImpl(CameraEvent evt, [NotNull] CommandBuffer buffer, ComputeQueueType queueType);
        [NativeName("RemoveCommandBuffer")]   extern private void RemoveCommandBufferImpl(CameraEvent evt, [NotNull] CommandBuffer buffer);

        /// <summary>
        /// 在相机的指定渲染阶段添加一个 CommandBuffer。
        /// CommandBuffer 是一系列渲染命令的列表，可以在相机渲染的特定时刻执行。
        /// 例如：在 AfterSkybox 阶段添加后处理效果。
        /// 注意：SRP 下此方法不可用，会发出警告。
        /// </summary>
        public void AddCommandBuffer(CameraEvent evt, CommandBuffer buffer)
        {
            if (!Rendering.CameraEventUtils.IsValid(evt))
                throw new ArgumentException(string.Format(@"Invalid CameraEvent value ""{0}"".", (int)evt), "evt");
            if (buffer == null) throw new NullReferenceException("buffer is null");

            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            else
            {
                AddCommandBufferImpl(evt, buffer);
                m_NonSerializedVersion++;
            }
        }

        /// <summary>
        /// 异步添加 CommandBuffer（支持指定计算队列类型）。
        /// 与 AddCommandBuffer 类似，但允许在异步计算队列上执行。
        /// 用于 GPU 异步计算（Compute Shader）与渲染管线的同步。
        /// 注意：SRP 下此方法不可用，会发出警告。
        /// </summary>
        public void AddCommandBufferAsync(CameraEvent evt, CommandBuffer buffer, ComputeQueueType queueType)
        {
            if (!Rendering.CameraEventUtils.IsValid(evt))
                throw new ArgumentException(string.Format(@"Invalid CameraEvent value ""{0}"".", (int)evt), "evt");
            if (buffer == null) throw new NullReferenceException("buffer is null");

            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            else
            {
                AddCommandBufferAsyncImpl(evt, buffer, queueType);
                m_NonSerializedVersion++;
            }
        }

        /// <summary>
        /// 从相机的指定渲染阶段移除一个特定的 CommandBuffer。
        /// 注意：SRP 下此方法不可用，会发出警告。
        /// </summary>
        public void RemoveCommandBuffer(CameraEvent evt, CommandBuffer buffer)
        {
            if (!Rendering.CameraEventUtils.IsValid(evt))
                throw new ArgumentException(string.Format(@"Invalid CameraEvent value ""{0}"".", (int)evt), "evt");
            if (buffer == null) throw new NullReferenceException("buffer is null");

            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }
            else
            {
                RemoveCommandBufferImpl(evt, buffer);
                m_NonSerializedVersion++;
            }
        }

        /// <summary>
        /// 获取指定相机事件上的所有 CommandBuffer。
        /// 注意：SRP 下此方法会发出警告，但仍会返回结果。
        /// </summary>
        public UnityEngine.Rendering.CommandBuffer[] GetCommandBuffers(UnityEngine.Rendering.CameraEvent evt)
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                LogWarningOnlyBuiltIn();
            }

            return GetCommandBuffersImpl(evt);
        }

        [FreeFunction("CameraScripting::GetCommandBuffers", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal UnityEngine.Rendering.CommandBuffer[] GetCommandBuffersImpl(UnityEngine.Rendering.CameraEvent evt);
	    internal uint m_NonSerializedVersion;
    }

    public partial class Camera
    {
        // 在相机剔除场景之前调用。
        // void OnPreCull();

        // 在相机开始渲染场景之前调用。
        // void OnPreRender();

        // 在相机完成场景渲染之后调用。
        // void OnPostRender();

        // 在所有渲染完成后调用，用于渲染图像效果。
        // void OnRenderImage(RenderTexture source, RenderTexture destination);

        // 在相机渲染场景后调用。
        // void OnRenderObject();

        // 如果对象可见，为每个相机调用一次。
        // void OnWillRenderObject();

        /// <summary>
        /// 相机回调委托类型。
        /// 用于 onPreCull、onPreRender、onPostRender 等静态事件。
        /// </summary>
        public delegate void CameraCallback(Camera cam);

        /// <summary>
        /// 在所有相机执行剔除（Culling）之前触发的全局事件。
        /// 注意：这是静态事件，所有相机都会触发。
        /// </summary>
        public static CameraCallback onPreCull;

        /// <summary>
        /// 在所有相机执行渲染（Render）之前触发的全局事件。
        /// 注意：这是静态事件，所有相机都会触发。
        /// </summary>
        public static CameraCallback onPreRender;

        /// <summary>
        /// 在所有相机完成渲染之后触发的全局事件。
        /// 注意：这是静态事件，所有相机都会触发。
        /// </summary>
        public static CameraCallback onPostRender;

        [RequiredByNativeCode]
        private static void FireOnPreCull(Camera cam)
        {
            if (onPreCull != null)
                onPreCull(cam);
        }

        [RequiredByNativeCode]
        private static void FireOnPreRender(Camera cam)
        {
            if (onPreRender != null)
                onPreRender(cam);
        }

        [RequiredByNativeCode]
        private static void FireOnPostRender(Camera cam)
        {
            if (onPostRender != null)
                onPostRender(cam);
        }

        [RequiredByNativeCode]
        private static void BumpNonSerializedVersion(Camera cam)
        {
            cam.m_NonSerializedVersion++;
        }

        // 这两个空的内部方法（总是会被剥离）用于确保 EmptyBuildGotStrippedEnough 测试通过。
        internal void OnlyUsedForTesting1()
        {
        }

        internal void OnlyUsedForTesting2()
        {
        }

        /// <summary>
        /// 尝试获取相机的剔除参数（ScriptableCullingParameters）。
        /// 用于 SRP 中自定义剔除逻辑。返回 true 表示成功获取。
        /// 这些参数包含了视锥体信息、LOD 参数等，可用于自定义的 CullingGroup。
        /// </summary>
        public unsafe bool TryGetCullingParameters(out Rendering.ScriptableCullingParameters cullingParameters)
        {
            return GetCullingParameters_Internal(this, false, out cullingParameters, sizeof(Rendering.ScriptableCullingParameters));
        }

        /// <summary>
        /// 尝试获取相机的剔除参数（支持立体感知）。
        /// stereoAware 参数控制是否考虑 VR 立体渲染的双眼剔除。
        /// 在 VR 中设置为 true 可以生成包含双眼信息的剔除参数。
        /// </summary>
        public unsafe bool TryGetCullingParameters(bool stereoAware, out Rendering.ScriptableCullingParameters cullingParameters)
        {
            return GetCullingParameters_Internal(this, stereoAware, out cullingParameters, sizeof(Rendering.ScriptableCullingParameters));
        }

        [NativeHeader("Runtime/Export/RenderPipeline/ScriptableRenderPipeline.bindings.h")]
        [FreeFunction("ScriptableRenderPipeline_Bindings::GetCullingParameters_Internal")]
        extern private static bool GetCullingParameters_Internal(Camera camera, bool stereoAware, out Rendering.ScriptableCullingParameters cullingParameters, int managedCullingParametersSize);
    }
}
