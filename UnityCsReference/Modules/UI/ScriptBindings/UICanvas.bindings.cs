// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    // =====================================================
    // Canvas 渲染模式枚举
    // =====================================================
    // 控制 UI 元素在屏幕上的渲染方式：覆盖全屏、通过相机渲染、或作为世界空间对象。
    // ScreenSpaceOverlay — 直接覆盖在屏幕上（最常用），无需 Camera
    // ScreenSpaceCamera   — 通过指定 Camera 渲染，支持景深效果
    // WorldSpace          — 作为 3D 世界中的对象，可被其他物体遮挡
    // =====================================================
    public enum RenderMode
    {
        /// <summary>
        /// 屏幕空间 - 覆盖模式。UI 元素直接渲染在屏幕最上层，不依赖 Camera。
        /// Canvas 自动适配屏幕大小，UI 始终在最前面显示。性能最好，最常用。
        /// </summary>
        ScreenSpaceOverlay = 0,

        /// <summary>
        /// 屏幕空间 - 相机模式。UI 元素通过指定的 worldCamera 渲染。
        /// 支持透视效果（如 3D UI 景深），可以和其他 3D 物体产生前后关系。
        /// planeDistance 控制 UI 平面与相机的距离。
        /// </summary>
        ScreenSpaceCamera = 1,

        /// <summary>
        /// 世界空间。UI 元素像普通 3D 物体一样放置在场景中。
        /// 常用于 HUD 面板、血量条等需要跟随 3D 对象的 UI。
        /// RectTransform 决定了 UI 在世界中的位置和大小。
        /// </summary>
        WorldSpace = 2
    }

    /// <summary>
    /// 独立平台下的 Canvas RectTransform 更新策略（桌面端 Standalone 平台使用）。
    /// 控制当屏幕大小变化时，是否自动重新计算 Canvas 的 RectTransform。
    /// </summary>
    public enum StandaloneRenderResize
    {
        /// <summary>启用自动重算，桌面窗口调整大小时自动更新 Canvas 布局</summary>
        Enabled = 0,
        /// <summary>禁用自动重算，需要手动触发布局更新</summary>
        Disabled = 1
    }

    /// <summary>
    /// 附加 Shader 通道枚举（位标记）。
    /// 当 UI 使用需要额外顶点数据（如法线、切线、多套 UV）的自定义 Shader 时，
    /// 需要通过此枚举开启对应的通道，否则 Shader 无法获取到所需的顶点数据。
    ///
    /// 例如：使用需要法线方向的特效 Shader 时，需要开启 Normal 通道。
    /// </summary>
    [Flags]
    public enum AdditionalCanvasShaderChannels
    {
        /// <summary>不附加任何额外通道（默认）</summary>
        None = 0,
        /// <summary>启用第 2 套 UV 坐标 (UV1)，常用于光照贴图</summary>
        TexCoord1 = 1 << 0,
        /// <summary>启用第 3 套 UV 坐标 (UV2)</summary>
        TexCoord2 = 1 << 1,
        /// <summary>启用第 4 套 UV 坐标 (UV3)</summary>
        TexCoord3 = 1 << 2,
        /// <summary>启用法线数据，用于需要法线方向的 Shader 效果</summary>
        Normal = 1 << 3,
        /// <summary>启用切线数据，用于需要切线空间的 Shader 效果（如法线贴图）</summary>
        Tangent = 1 << 4
    }

    // =====================================================
    // Canvas 组件
    // =====================================================
    // Canvas（画布）是 Unity uGUI 系统的核心组件，负责管理和渲染所有 UI 元素。
    //
    // 核心功能：
    // 1. 渲染模式控制 —— 决定 UI 如何呈现在屏幕上
    // 2. 排序管理 —— 通过 sortingLayerID、sortingOrder 控制 UI 元素的上下层级
    // 3. 像素完美 —— pixelPerfect 确保 UI 渲染不出现像素偏移
    // 4. 缩放管理 —— scaleFactor 和 referencePixelsPerUnit 控制 UI 自适应
    //
    // 层级结构：
    // - Canvas 必须挂载在带有 RectTransform 的 GameObject 上（RequireComponent）
    // - 子 Canvas（嵌套 Canvas）会覆盖父 Canvas 的部分设置
    // - isRootCanvas 判断是否为场景中最顶层、不受父 Canvas 影响的 Canvas
    //
    // 渲染管线：
    // Canvas 收集其下所有 UI 元素的网格数据 → 合批优化 → 提交给 GPU 渲染
    // CanvasManager 统一管理场景中的所有 Canvas 实例
    // =====================================================

    [RequireComponent(typeof(RectTransform)),
     NativeClass("UI::Canvas"),
     NativeHeader("Modules/UI/Canvas.h"),
     NativeHeader("Modules/UI/CanvasManager.h"),
     NativeHeader("Modules/UI/UIStructs.h")]
    [UIModuleHelpURL("class-Canvas")]
    public sealed partial class Canvas : Behaviour
    {
        // =====================================================
        // Canvas 批处理更新策略
        // =====================================================
        // 控制 Canvas 是否每帧都重新合批渲染数据。
        // GatedByRendering — 由 OnDemandRendering 控制更新频率，
        //                   当渲染负载高时跳过合批以节省 CPU
        // AlwaysUpdate    — 总是更新合批数据，保证 UI 即使不变化也保持流畅
        // 与 C++ 端的 CanvasManager::s_BatchingInterval 保持同步
        // =====================================================
        public enum BatchingInterval
        {
            /// <summary>由渲染器门控，按需更新（性能优化模式，降低 CPU 开销）</summary>
            GatedByRendering = 0,
            /// <summary>总是更新，不跳过任何帧（流畅优先模式）</summary>
            AlwaysUpdate = 1
        }

        /// <summary>
        /// 获取或设置 Canvas 的批处理更新策略。
        /// 当设置为 AlwaysUpdate 时，Canvas 每帧都会重新合批，
        /// 适用于 UI 内容频繁变化的场景（如动画、实时数据刷新）。
        /// 默认为 GatedByRendering，与 OnDemandRendering 协同工作以节省 CPU。
        /// </summary>
        public static BatchingInterval batchingInterval
        {
            get => (BatchingInterval)Internal_GetBatchingInterval();
            set
            {
                int intValue = (int)value;

                if (!Enum.IsDefined(typeof(BatchingInterval), intValue))
                {
                    // 传入无效值时回退到默认策略，保证不会因为错误的值导致渲染异常
                    intValue = 0;
                    Debug.LogWarning($"Invalid value for Canvas.batchingInterval: {value}. Defaulting to BatchingInterval.GatedByRendering.");
                }

                Internal_SetBatchingInterval(intValue);
            }
        }

        [FreeFunction("UI::CanvasManager::SetBatchingInterval")]
        internal static extern void Internal_SetBatchingInterval(int value);

        [FreeFunction("UI::CanvasManager::GetBatchingInterval")]
        internal static extern int Internal_GetBatchingInterval();

        /// <summary>
        /// 渲染回调委托 —— 在 Canvas 即将渲染前调用。
        /// 可用于在 UI 渲染前执行自定义逻辑。
        /// </summary>
        public delegate void WillRenderCanvases();

        /// <summary>
        /// 预渲染事件 —— 在 willRenderCanvases 之前触发。
        /// 常用于需要在所有 Canvas 渲染前执行的一轮准备工作。
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event WillRenderCanvases preWillRenderCanvases;

        /// <summary>
        /// 将渲染事件 —— 在所有 Canvas 即将渲染时触发。
        /// 这是更新 UI 状态的推荐时机（如更新文本、动画等），
        /// 在此事件中修改 UI 不会引起额外的性能开销。
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event WillRenderCanvases willRenderCanvases;

        // ============================================================
        // Canvas 核心属性
        // ============================================================
        // 以下属性通过 extern 声明，实际实现在 C++ 端（Native代码）：
        // - Modules/UI/Canvas.h
        // - Modules/UI/CanvasManager.h
        // ============================================================

        /// <summary>渲染模式：ScreenSpaceOverlay / ScreenSpaceCamera / WorldSpace</summary>
        public extern RenderMode renderMode { get; set; }

        /// <summary>
        /// 是否为根 Canvas。
        /// 根 Canvas 是场景中最顶层、不受父 Canvas 影响的 Canvas。
        /// 如果此 Canvas 没有父 Canvas（或父对象上没有 Canvas 组件），则为 true。
        /// </summary>
        public extern bool isRootCanvas { get; }

        /// <summary>
        /// Canvas 的像素矩形区域。
        /// 返回 Canvas 在屏幕像素空间中的实际大小和位置（相对于屏幕左下角）。
        /// 对于 ScreenSpaceOverlay 模式，等于 Screen.width × Screen.height。
        /// 对于 ScreenSpaceCamera 模式，取决于相机的视口设置。
        /// </summary>
        public extern Rect pixelRect { get; }

        /// <summary>
        /// Canvas 的缩放因子。
        /// 用于实现高 DPI 自适应或 UI 整体缩放。
        /// 由 Canvas Scaler 组件自动计算，也可手动设置。
        /// 例如：在 4K 分辨率下设置为 2，UI 元素尺寸放大两倍。
        /// </summary>
        public extern float scaleFactor { get; set; }

        /// <summary>
        /// 每 Unity 单位对应的参考像素数。
        /// 与 Canvas Scaler 配合使用，控制 UI 在不同分辨率下的缩放行为。
        /// 默认值为 100，即 1 Unity 单位 = 100 像素。
        /// 调整此值可以改变 UI 整体的"参考分辨率"。
        /// </summary>
        public extern float referencePixelsPerUnit { get; set; }

        /// <summary>
        /// 是否覆盖 pixelPerfect 设置。
        /// 当为 true 时，此 Canvas 的 pixelPerfect 设置独立计算，
        /// 不受父 Canvas 的影响。
        /// </summary>
        public extern bool overridePixelPerfect { get; set; }

        /// <summary>
        /// 顶点颜色是否始终使用 Gamma 色彩空间。
        /// 在线性色彩空间下，UI 顶点颜色默认会进行色彩空间转换。
        /// 开启此选项可强制顶点颜色始终在 Gamma 空间下处理，
        /// 避免某些特效的色彩偏差。
        /// </summary>
        public extern bool vertexColorAlwaysGammaSpace { get; set; }

        /// <summary>
        /// Canvas 是否使用反射探针。
        /// 仅 WorldSpace 模式下有效，控制 UI 是否受场景反射探针影响。
        /// </summary>
        public extern bool useReflectionProbes { get; set; }

        /// <summary>
        /// 像素完美模式。
        /// 开启后，Canvas 中的 UI 元素位置会被对齐到整数像素边界，
        /// 消除子像素渲染导致的模糊效果。
        /// 适用于像素风格的游戏或需要清晰边角的 UI。
        /// </summary>
        public extern bool pixelPerfect { get; set; }

        /// <summary>
        /// UI 平面到相机的距离（仅在 ScreenSpaceCamera 模式下有效）。
        /// 控制 UI 在相机 Z 轴方向上的位置。
        /// 值越小，UI 越靠近相机（渲染在更前面）；
        /// 调整此值可以和 3D 场景中的物体产生穿插效果。
        /// </summary>
        public extern float planeDistance { get; set; }

        /// <summary>
        /// Canvas 的渲染顺序索引。
        /// 由 CanvasManager 自动分配，值越大渲染越靠后。
        /// 此值由系统管理，用于同层级 Canvas 之间的排序。
        /// </summary>
        public extern int renderOrder { get; }

        /// <summary>
        /// 是否覆盖父 Canvas 的排序设置。
        /// 当为 true 时，此 Canvas 使用自己的 sortingOrder 和 sortingLayerID。
        /// 用于嵌套 Canvas 的场景，允许子 Canvas 独立控制渲染层级。
        /// </summary>
        public extern bool overrideSorting { get; set; }

        /// <summary>
        /// Canvas 在 sorting layer 内部的排序序号。
        /// 值越大渲染越靠前（显示在更上层）。
        /// 在同一 sorting layer 内，通过此值控制多个 Canvas 的叠放顺序。
        /// </summary>
        public extern int sortingOrder { get; set; }

        /// <summary>
        /// Canvas 渲染到的目标显示器索引（多显示器支持）。
        /// 0 = 主显示器，1+ = 扩展显示器。
        /// </summary>
        public extern int targetDisplay { get; set; }

        /// <summary>
        /// Canvas 所属的 sorting layer 的 ID。
        /// Sorting Layer 是 Unity 渲染排序的第一维度，
        /// 用于在不同类型的渲染对象之间建立渲染顺序（如 UI 在 3D 物体之上）。
        /// ID 对应 TagManager 中 Sorting Layers 列表的索引。
        /// 比 sortingOrder 有更高优先级：先按 sorting layer 排序，同 layer 内再按 order 排序。
        /// </summary>
        public extern int sortingLayerID { get; set; }

        /// <summary>
        /// 缓存的 sorting layer 值（只读），由 Unity 内部维护。
        /// 用于快速排序比较，避免每帧重新计算 layer 值。
        /// </summary>
        public extern int cachedSortingLayerValue { get; }

        /// <summary>
        /// 附加 Shader 通道。
        /// 当 UI 材质需要使用额外的顶点数据（法线、切线、多套 UV）时，
        /// 通过此属性开启对应的通道。默认不开启以节省顶点数据带宽。
        /// </summary>
        public extern AdditionalCanvasShaderChannels additionalShaderChannels { get; set; }

        /// <summary>
        /// Canvas 所属的 sorting layer 名称。
        /// 与 sortingLayerID 对应，提供字符串形式的访问方式。
        /// 通过 TagManager 中定义的 Sorting Layers 列表获取合法名称。
        /// </summary>
        public extern string sortingLayerName { get; set; }

        /// <summary>
        /// 获取此 Canvas 的根 Canvas。
        /// 如果是根 Canvas 则返回自身；如果是子 Canvas 则返回顶层祖先 Canvas。
        /// </summary>
        public extern Canvas rootCanvas { get; }

        /// <summary>
        /// Canvas 的渲染输出尺寸（像素）。
        /// 对于 ScreenSpace 模式，等于屏幕分辨率。
        /// 对于 WorldSpace 模式，等于 Canvas 在世界空间中的像素等效尺寸。
        /// </summary>
        public extern Vector2 renderingDisplaySize { get; }

        /// <summary>
        /// 独立平台下是否在屏幕大小变化时更新 Canvas RectTransform。
        /// 仅在 Standalone 平台（Windows/Mac/Linux）有效。
        /// 开启后，当窗口大小改变时自动调整 Canvas 的 RectTransform。
        /// </summary>
        public extern StandaloneRenderResize updateRectTransformForStandalone { get; set; }

        // ============================================================
        // UI Toolkit（UIElements）集成
        // ============================================================
        // 以下回调用于 UI Toolkit 与 uGUI Canvas 的交互，
        // 支持 UI Toolkit 面板在 uGUI Canvas 上层进行叠加渲染。
        // 通过这三个回调控制叠加渲染的生命周期：开始前→中间插入→结束后
        // ============================================================

        /// <summary>UI Toolkit 叠加渲染开始回调</summary>
        [AutoStaticsCleanupOnCodeReload]
        internal static Action<int> externBeginRenderOverlays
        {
            get;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            set;
        }

        /// <summary>UI Toolkit 叠加渲染中间插入回调（在指定 sorting order 前渲染）</summary>
        [AutoStaticsCleanupOnCodeReload]
        internal static Action<int, int> externRenderOverlaysBefore
        {
            get;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            set;
        }

        /// <summary>UI Toolkit 叠加渲染结束回调</summary>
        [AutoStaticsCleanupOnCodeReload]
        internal static Action<int> externEndRenderOverlays
        {
            get;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            set;
        }

        /// <summary>
        /// 启用/禁用外部 Canvas（用于 UI Toolkit 集成）。
        /// 当禁用时，UI Toolkit 面板不会叠加渲染到 uGUI Canvas 上。
        /// </summary>
        [FreeFunction("UI::CanvasManager::SetExternalCanvasEnabled")]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void SetExternalCanvasEnabled(bool enabled);

        /// <summary>
        /// 获取或设置 Canvas 关联的 worldCamera（ScreenSpaceCamera 或 WorldSpace 模式）。
        /// 决定 UI 通过哪个 Camera 进行渲染。
        /// ScreenSpaceOverlay 模式下此属性为 null。
        /// </summary>
        [NativeProperty("Camera", false, TargetType.Function)] public extern Camera worldCamera { get; set; }

        /// <summary>
        /// 排序桶归一化网格大小（高级排序功能）。
        /// 用于控制 Canvas 在排序桶中的粒度，影响同层级 Canvas 的排序精度。
        /// </summary>
        [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)] public extern float normalizedSortingGridSize { get; set; }

        [Obsolete("Setting normalizedSize via a int is not supported. Please use normalizedSortingGridSize", false)]
        [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)] public extern int sortingGridNormalizedSize { get; set; }

        [Obsolete("Shared default material now used for text and general UI elements, call Canvas.GetDefaultCanvasMaterial()", false)]
        [FreeFunction("UI::GetDefaultUIMaterial")] public static extern Material GetDefaultCanvasTextMaterial();

        /// <summary>
        /// 获取 Canvas 使用的默认材质。
        /// 返回 Unity 内置的 UI 默认材质（"UI/Default"）。
        /// 这是所有 UI 元素在没有自定义材质时使用的默认材质。
        /// </summary>
        [FreeFunction("UI::GetDefaultUIMaterial")] public static extern Material GetDefaultCanvasMaterial();

        /// <summary>
        /// 获取支持 ETC1 纹理压缩的 Canvas 材质。
        /// ETC1 是 Android 平台的强制纹理压缩格式，
        /// 此材质可以配合独立的 Alpha 贴图使用（分割 RGB 和 Alpha 通道）。
        /// </summary>
        [FreeFunction("UI::GetETC1SupportedCanvasMaterial")] public static extern Material GetETC1SupportedCanvasMaterial();

        /// <summary>
        /// 内部方法：更新 Canvas 的 RectTransform（与相机对齐）。
        /// ScreenSpaceCamera 模式下，将 Canvas 的位置对齐到相机视口。
        /// </summary>
        internal extern void UpdateCanvasRectTransform(bool alignWithCamera);

        /// <summary>
        /// 内部属性：Canvas 的渲染阶段优先级（用于多显示器的渲染排序）。
        /// </summary>
        internal extern byte stagePriority { get; set; }

        /// <summary>
        /// 强制更新所有 Canvas。
        /// 调用后会触发 preWillRenderCanvases 和 willRenderCanvases 事件，
        /// 然后立即执行所有 Canvas 的渲染数据更新（网格重建、合批等）。
        /// 通常在布局或内容发生变化后调用，确保 UI 立即更新。
        /// </summary>
        public static void ForceUpdateCanvases()
        {
            SendPreWillRenderCanvases();
            SendWillRenderCanvases();
        }

        /// <summary>
        /// [由 Native 代码调用] 发送预渲染事件。
        /// CanvasManager 在每帧渲染前通过此方法触发 preWillRenderCanvases 事件。
        /// </summary>
        [RequiredByNativeCode]
        private static void SendPreWillRenderCanvases()
        {
            preWillRenderCanvases?.Invoke();
        }

        /// <summary>
        /// [由 Native 代码调用] 发送即将渲染事件。
        /// CanvasManager 在每帧渲染前通过此方法触发 willRenderCanvases 事件。
        /// 这是 Unity UI 生命周期中的关键回调点。
        /// </summary>
        [RequiredByNativeCode]
        private static void SendWillRenderCanvases()
        {
            willRenderCanvases?.Invoke();
        }

        /// <summary>
        /// [由 Native 代码调用] 开始渲染额外叠加层。
        /// 用于 UI Toolkit 面板叠加渲染的生命周期管理。
        /// </summary>
        [RequiredByNativeCode]
        private static void BeginRenderExtraOverlays(int displayIndex)
        {
            externBeginRenderOverlays?.Invoke(displayIndex);
        }

        /// <summary>
        /// [由 Native 代码调用] 在指定 sorting order 位置插入叠加渲染。
        /// 允许 UI Toolkit 面板插入到 uGUI 渲染层的指定层级之间。
        /// </summary>
        [RequiredByNativeCode]
        private static void RenderExtraOverlaysBefore(int displayIndex, int sortingOrder)
        {
            externRenderOverlaysBefore?.Invoke(displayIndex, sortingOrder);
        }

        /// <summary>
        /// [由 Native 代码调用] 结束渲染额外叠加层。
        /// 标记 UI Toolkit 叠加渲染完成。
        /// </summary>
        [RequiredByNativeCode]
        private static void EndRenderExtraOverlays(int displayIndex)
        {
            externEndRenderOverlays?.Invoke(displayIndex);
        }
    }

    // =====================================================
    // UI 系统性能分析 API
    // =====================================================
    // 提供用于 Profiler 性能分析的工具方法。
    // 用于标记 UI 系统中的布局和渲染阶段，
    // 方便在 Unity Profiler 中定位 UI 性能瓶颈。
    // IgnoredByDeepProfiler 特性表示此 API 在 Deep Profiling 模式下不展开。
    // =====================================================

    [IgnoredByDeepProfiler]
    [NativeHeader("Modules/UI/Canvas.h"),
     StaticAccessor("UI::SystemProfilerApi", StaticAccessorType.DoubleColon)]
    public static class UISystemProfilerApi
    {
        /// <summary>
        /// 采样类型：Layout（布局计算）或 Render（渲染提交）
        /// Layout — 标记 UI 布局计算的起止（RectTransform 重算、Layout Group 布局等）
        /// Render  — 标记 UI 渲染数据提交的起止（网格重建、合批等）
        /// </summary>
        public enum SampleType
        {
            Layout,
            Render
        }

        /// <summary>开始一个 UI 性能分析采样段</summary>
        public static extern void BeginSample(SampleType type);
        /// <summary>结束一个 UI 性能分析采样段</summary>
        public static extern void EndSample(SampleType type);
        /// <summary>在 Profiler 中添加一个自定义标记点</summary>
        public static extern void AddMarker(string name, Object obj);
    }
}
