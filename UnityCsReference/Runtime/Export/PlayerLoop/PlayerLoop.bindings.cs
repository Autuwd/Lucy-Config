// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// PlayerLoop — Unity 引擎主循环定义
//
// 什么是 PlayerLoop？
//   PlayerLoop（播放器循环）是 Unity 引擎每帧执行的系统序列。
//   它定义了 Unity 在每一帧中「按什么顺序执行什么子系统」。
//
// 执行顺序（从上到下）：
//
//   1. TimeUpdate          → 时间同步（等候上一帧呈现完成，更新时间）
//   2. Initialization      → 初始化（Profile 标记、摄像机运动向量、Director 采样等）
//   3. EarlyUpdate         → 早期更新（网络轮询、输入同步、流式加载、纹理管理等）
//   4. FixedUpdate         → ★ 固定时间步长更新（物理系统核心！Physics / 2D Physics）
//   5. PreUpdate           → 预更新（物理结果应用、AI、输入事件派发）
//   6. Update              → ★★★ 主更新（MonoBehaviour.Update() 就在这里！）
//   7. PreLateUpdate       → 晚期更新前（动画更新、粒子更新、UI Elements 更新）
//   8. PostLateUpdate      → ★ 晚期更新（渲染、UI 画布、音频、后处理、呈现）
//
// 这个执行顺序是 Unity 一切行为的基础：
// - 为什么 FixedUpdate 在 Update 之前？→ 物理模拟需要先于逻辑更新
// - 为什么 LateUpdate 在 Update 之后？→ 跟随摄像机、状态同步等在 Update 完成后执行
// - 为什么渲染在最后？→ 需要所有更新完成后才开始绘制
//
// 自定义 PlayerLoop：
//   从 Unity 2019.3 开始，可以通过 PlayerLoopSystem API 自定义主循环。
//   这允许用户插入/删除/重新排序 PlayerLoop 阶段。
//   在 UnityEngine.LowLevel 命名空间中有 PlayerLoop 类提供此能力。
//
// 设计模式（空结构体）：
//   这里的每个 struct 都是「标记结构体」，本身不包含任何字段。
//   它们只是作为 PlayerLoopSystem 的 type 标识符，让引擎知道
//   在哪个位置执行哪种系统。实际的执行逻辑在 C++ 原生侧。
//   结构体之间的包含关系（嵌套 struct）表示父子层级关系。
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Bindings;

namespace UnityEngine.PlayerLoop
{
    // ============================================================
    // 1. TimeUpdate — 时间更新阶段
    //
    // 功能：同步呈现和更新的时间基线。
    // 这是 PlayerLoop 的第一个阶段，确保时间和呈现状态同步。
    // 在旧版 Unity 中 ProfilerStartFrame 曾在这里（现已移至 Initialization）。
    // ============================================================

    [RequiredByNativeCode]
    public struct TimeUpdate
    {
        /// <summary>
        ///   等待上一帧呈现完成并更新时间。
        ///   确保在开始新一帧之前，上一帧已经呈现完毕，
        ///   然后更新时间信息（Time.deltaTime / Time.time 等）。
        /// </summary>
        [RequiredByNativeCode]
        public struct WaitForLastPresentationAndUpdateTime {}

        /// <summary>
        ///   【已废弃】ProfilerStartFrame 已移至 Initialization 类别。
        /// </summary>
        [Obsolete("ProfilerStartFrame 已移至 Initialization 类别。 (UnityUpgradable) -> UnityEngine.PlayerLoop.Initialization/ProfilerStartFrame", true)]
        public struct ProfilerStartFrame {}
    }

    // ============================================================
    // 2. Initialization — 初始化阶段
    //
    // 功能：每一帧开始时执行的初始化操作。
    // 包括：性能分析标记、摄像机运动向量、Director 时间采样、
    //       异步纹理上传、输入状态同步、XR 早期更新等。
    // ============================================================

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.Experimental.PlayerLoop")]
    public struct Initialization
    {
        /// <summary>
        ///   性能分析器帧开始标记。记录 Profiler 帧起点。
        /// </summary>
        [RequiredByNativeCode]
        public struct ProfilerStartFrame {}

        /// <summary>
        ///   【已废弃】PlayerUpdateTime 已移至 TimeUpdate 类别。
        /// </summary>
        [Obsolete("PlayerUpdateTime 已移至 TimeUpdate 类别。 (UnityUpgradable) -> UnityEngine.PlayerLoop.TimeUpdate/WaitForLastPresentationAndUpdateTime", true)]
        public struct PlayerUpdateTime {}

        /// <summary>
        ///   更新摄像机运动向量（Motion Vector）。
        ///   用于 TAA（时序抗锯齿）、运动模糊等后处理效果。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateCameraMotionVectors {}

        /// <summary>
        ///   Director（Playable / Timeline）时间采样。
        ///   更新 Timeline 和 Playable 系统的时间信息。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorSampleTime {}

        /// <summary>
        ///   异步上传的时间切片更新。
        ///   将纹理上传到 GPU 的操作分配在每一帧的时间片中执行。
        /// </summary>
        [RequiredByNativeCode]
        public struct AsyncUploadTimeSlicedUpdate {}

        /// <summary>
        ///   同步状态。更新各种需要每帧同步的内部状态。
        /// </summary>
        [RequiredByNativeCode]
        public struct SynchronizeState {}

        /// <summary>
        ///   同步输入。初始化输入系统状态，准备处理本帧输入。
        /// </summary>
        [RequiredByNativeCode]
        public struct SynchronizeInputs {}

        /// <summary>
        ///   XR 早期更新。在 XR 设备帧开始前进行初始化。
        /// </summary>
        [RequiredByNativeCode]
        public struct XREarlyUpdate {}
    }

    // ============================================================
    // 3. EarlyUpdate — 早期更新阶段
    //
    // 功能：在主线 Update 之前执行的各种系统维护工作。
    // 这是 PlayerLoop 中最「忙」的阶段之一，几乎所有的引擎子系统
    // 都在这里执行它们的早期帧更新：
    //
    // 关键子阶段：
    //   - 网络轮询（PollPlayerConnection / PollHtcsPlayerConnection）
    //   - UnityWebRequest 更新
    //   - 流式加载管理（UpdateStreamingManager / UpdateContentLoading）
    //   - 纹理流送（UpdateTextureStreamingManager）
    //   - 输入管理（UpdateInputManager / ProcessRemoteInput）
    //   - XR 更新
    //   - AR/VR 更新（ARCoreUpdate）
    //   - 物理 2D 早期更新（Physics2DEarlyUpdate）
    //   - 精灵图集管理（SpriteAtlasManagerUpdate）
    //   - Canvas RectTransform 更新
    // ============================================================

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.Experimental.PlayerLoop")]
    public struct EarlyUpdate
    {
        [RequiredByNativeCode]
        public struct PollPlayerConnection {}

        /// <summary>
        ///   【已废弃】ProfilerStartFrame 已移至 Initialization 类别。
        /// </summary>
        [Obsolete("ProfilerStartFrame player loop component has been moved to the Initialization category. (UnityUpgradable) -> UnityEngine.PlayerLoop.Initialization/ProfilerStartFrame", true)]
        public struct ProfilerStartFrame {}

        [RequiredByNativeCode]
        public struct PollHtcsPlayerConnection {}

        [RequiredByNativeCode]
        public struct GpuTimestamp {}

        [RequiredByNativeCode]
        public struct AnalyticsCoreStatsUpdate {}

        [RequiredByNativeCode]
        public struct InsightsUpdate {}

        /// <summary>
        ///   UnityWebRequest 异步请求的更新处理。
        ///   在此阶段检查和派发 UnityWebRequest 的回调和进度。
        /// </summary>
        [RequiredByNativeCode]
        public struct UnityWebRequestUpdate {}

        /// <summary>
        ///   流式加载管理器更新。处理场景和资源的流式加载状态。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateStreamingManager {}

        [RequiredByNativeCode]
        public struct ExecuteMainThreadJobs {}

        [RequiredByNativeCode]
        public struct ProcessMouseInWindow {}

        [RequiredByNativeCode]
        public struct ClearIntermediateRenderers {}

        [RequiredByNativeCode]
        public struct ClearLines {}

        /// <summary>
        ///   在更新前进行呈现（Present）。
        ///   某些平台/配置下需要在更新前执行 SwapBuffers。
        /// </summary>
        [RequiredByNativeCode]
        public struct PresentBeforeUpdate {}

        [RequiredByNativeCode]
        public struct ResetFrameStatsAfterPresent {}

        /// <summary>
        ///   更新异步回读管理器。
        ///   处理 GPU 异步回读请求（如 AsyncGPUReadback）的状态。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateAsyncReadbackManager {}

        /// <summary>
        ///   更新纹理流送管理器。
        ///   根据摄像机位置动态加载/卸载纹理的 Mip 级别。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateTextureStreamingManager {}

        /// <summary>
        ///   更新预加载（Preload）状态。
        ///   处理 Resource.Load / AssetBundle 等预加载请求。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdatePreloading {}

        /// <summary>
        ///   更新内容加载。处理 Addressables 等异步加载请求的调度。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateContentLoading{ }

        /// <summary>
        ///   更新异步实例化。处理资源的异步实例化请求。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateAsyncInstantiate{ }

        [RequiredByNativeCode]
        public struct RendererNotifyInvisible {}

        [RequiredByNativeCode]
        public struct PlayerCleanupCachedData {}

        [RequiredByNativeCode]
        public struct UpdateMainGameViewRect {}

        /// <summary>
        ///   更新 Canvas 的 RectTransform。
        ///   在 UI 布局更新前重新计算 RectTransform 属性。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateCanvasRectTransform {}

        /// <summary>
        ///   更新输入管理器。处理键盘、鼠标、游戏手柄等输入设备的状态。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateInputManager {}

        /// <summary>
        ///   处理远程输入。用于编辑器远程调试（Unity Remote）等场景。
        /// </summary>
        [RequiredByNativeCode]
        public struct ProcessRemoteInput {}

        /// <summary>
        ///   XR 更新。更新 XR 设备的跟踪状态和输入。
        /// </summary>
        [RequiredByNativeCode]
        public struct XRUpdate {}

        /// <summary>
        ///   执行延迟的启动帧脚本（Start 方法延迟执行部分）。
        /// </summary>
        [RequiredByNativeCode]
        public struct ScriptRunDelayedStartupFrame {}

        [RequiredByNativeCode]
        public struct UpdateKinect {}

        [RequiredByNativeCode]
        public struct DeliverIosPlatformEvents {}

        [RequiredByNativeCode]
        public struct DispatchEventQueueEvents {}

        [RequiredByNativeCode]
        public struct PhysicsCore2DEarlyUpdate { }

        /// <summary>
        ///   2D 物理早期更新（Physics2D）。
        ///   在 2D 物理计算前进行准备工作。
        /// </summary>
        [RequiredByNativeCode]
        public struct Physics2DEarlyUpdate {}

        [RequiredByNativeCode]
        public struct PhysicsResetInterpolatedTransformPosition {}

        /// <summary>
        ///   精灵图集管理器更新。处理精灵图集的加载和卸载。
        /// </summary>
        [RequiredByNativeCode]
        public struct SpriteAtlasManagerUpdate {}

        /// <summary>
        ///   【已废弃】Tango 更新（Google Tango AR 平台）。
        ///   替代方案：使用 ARCoreUpdate。
        /// </summary>
        [RequiredByNativeCode]
        [Obsolete("TangoUpdate 已废弃。请使用 ARCoreUpdate (UnityUpgradable) -> UnityEngine.PlayerLoop.EarlyUpdate/ARCoreUpdate", false)]
        public struct TangoUpdate {}

        /// <summary>
        ///   ARCore 更新（Google ARCore 平台）。
        ///   更新 ARCore 的跟踪、环境理解等状态。
        /// </summary>
        [RequiredByNativeCode]
        public struct ARCoreUpdate {}

        [RequiredByNativeCode]
        public struct PerformanceAnalyticsUpdate {}

        /// <summary>
        ///   Tilemap 渲染器早期更新。在 Tilemap 渲染前更新数据。
        /// </summary>
        [RequiredByNativeCode]
        public struct TilemapRendererEarlyUpdate {}
    }

    // ============================================================
    // 4. FixedUpdate — ★ 固定时间步长更新
    //
    // 功能：以固定时间步长（Time.fixedDeltaTime）执行的更新。
    //       这是「物理系统」的核心阶段！
    //
    // 为什么需要 FixedUpdate？
    //   物理模拟需要确定性的时间步长才能稳定计算结果。
    //   如果帧率波动，普通 Update 的时间间隔会变化，
    //   导致物理模拟不稳定。FixedUpdate 保证了一致的模拟步长。
    //
    // 执行顺序：物理引擎（3D Physics → 2D Physics）→ 脚本 FixedUpdate
    //
    // 重要子阶段：
    //   - ScriptRunBehaviourFixedUpdate → MonoBehaviour.FixedUpdate()
    //   - PhysicsFixedUpdate → PhysX 引擎物理模拟
    //   - Physics2DFixedUpdate → Box2D 物理模拟
    //   - DirectorFixedUpdate → Timeline 固定步长更新
    //   - AudioFixedUpdate → 音频固定步长更新
    // ============================================================

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.Experimental.PlayerLoop")]
    public struct FixedUpdate
    {
        [RequiredByNativeCode]
        public struct ClearLines {}

        /// <summary>
        ///   Director 固定时间采样。
        ///   Timeline / Playable 系统的固定步长时间采样。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorFixedSampleTime {}

        /// <summary>
        ///   音频系统固定步长更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct AudioFixedUpdate {}

        /// <summary>
        ///   ★ MonoBehaviour.FixedUpdate() 就在这里执行。
        ///   这是脚本中 FixedUpdate 方法的实际调用位置。
        /// </summary>
        [RequiredByNativeCode]
        public struct ScriptRunBehaviourFixedUpdate {}

        /// <summary>
        ///   Director 固定步长更新。
        ///   Timeline 在固定时间步长下的更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorFixedUpdate {}

        /// <summary>
        ///   旧版动画系统（Legacy Animation）固定步长更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct LegacyFixedAnimationUpdate {}

        /// <summary>
        ///   XR 固定步长更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct XRFixedUpdate {}

        /// <summary>
        ///   ★★★ 3D 物理引擎（PhysX）固定步长更新！
        ///   物理碰撞检测、刚体模拟、约束求解等都在这里执行。
        ///   这是 FixedUpdate 阶段最核心的子阶段。
        /// </summary>
        [RequiredByNativeCode]
        public struct PhysicsFixedUpdate {}

        [RequiredByNativeCode]
        public struct PhysicsCore2DFixedUpdate { }

        /// <summary>
        ///   2D 物理引擎（Box2D）固定步长更新。
        ///   2D 碰撞检测、刚体模拟等。
        /// </summary>
        [RequiredByNativeCode]
        public struct Physics2DFixedUpdate {}

        [RequiredByNativeCode]
        struct PhysicsClothFixedUpdate {}

        /// <summary>
        ///   Director 在物理更新后的固定步长更新。
        ///   执行需要在物理模拟之后运行的 Timeline 更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorFixedUpdatePostPhysics {}

        /// <summary>
        ///   延迟执行固定帧率脚本。
        ///   通过 StartCoroutine 在 FixedUpdate 中延迟执行的回调。
        /// </summary>
        [RequiredByNativeCode]
        public struct ScriptRunDelayedFixedFrameRate {}

        /// <summary>
        ///   新输入系统（Input System Package）的 FixedUpdate。
        /// </summary>
        [RequiredByNativeCode]
        public struct NewInputFixedUpdate {}
    }

    // ============================================================
    // 5. PreUpdate — 预更新阶段
    //
    // 功能：在 Update 之前执行的最后准备步骤。
    //
    // 关键子阶段：
    //   - PhysicsUpdate → 物理模拟结果的应用（将刚体变换写回 Transform）
    //   - Physics2DUpdate → 2D 物理结果应用
    //   - AIUpdate → NavMesh Agent 等 AI 系统的更新
    //   - SendMouseEvents → 鼠标事件派发（OnMouseDown 等）
    //   - IMGUISendQueuedEvents → IMGUI 事件处理
    //   - NewInputUpdate / InputForUIUpdate → 输入系统更新
    // ============================================================

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.Experimental.PlayerLoop")]
    public struct PreUpdate
    {
        /// <summary>
        ///   3D 物理更新。将物理引擎的计算结果应用到 Transform 上。
        ///   FixedUpdate 中物理模拟产生的位移/旋转在此处写回游戏对象。
        /// </summary>
        [RequiredByNativeCode]
        public struct PhysicsUpdate {}

        [RequiredByNativeCode]
        public struct PhysicsCore2DUpdate { }

        /// <summary>
        ///   2D 物理更新。将 2D 物理引擎的计算结果应用到 Transform。
        /// </summary>
        [RequiredByNativeCode]
        public struct Physics2DUpdate {}

        [RequiredByNativeCode]
        internal struct PhysicsClothUpdate {}

        [RequiredByNativeCode]
        public struct CheckTexFieldInput {}

        /// <summary>
        ///   IMGUI 事件派发。发送 OnGUI 队列中的事件。
        /// </summary>
        [RequiredByNativeCode]
        public struct IMGUISendQueuedEvents {}

        /// <summary>
        ///   ★ 鼠标事件派发。
        ///   将鼠标输入转换为 MonoBehaviour 的鼠标事件消息：
        ///   OnMouseDown / OnMouseDrag / OnMouseEnter / OnMouseExit 等。
        /// </summary>
        [RequiredByNativeCode]
        public struct SendMouseEvents {}

        /// <summary>
        ///   AI 系统更新（NavMesh Agent）。
        ///   更新 NavMesh Agent 的寻路状态和移动决策。
        /// </summary>
        [RequiredByNativeCode]
        public struct AIUpdate {}

        /// <summary>
        ///   风区域组件（WindZone）更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct WindUpdate {}

        /// <summary>
        ///   视频播放更新。更新 VideoPlayer 组件。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateVideo {}

        /// <summary>
        ///   新输入系统（Input System Package）更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct NewInputUpdate {}

        /// <summary>
        ///   为 UI 模块更新的输入。新输入系统中的 UI 输入处理。
        /// </summary>
        [RequiredByNativeCode]
        public struct InputForUIUpdate { }
    }

    // ============================================================
    // 6. ★★★ Update — ★★★ 主更新阶段 ★★★
    //
    // 功能：这是 Unity PlayerLoop 中最重要的阶段！
    //       MonoBehaviour.Update() 方法就在这里被调用。
    //
    // 这就是为什么你的 Update() 代码在 FixedUpdate() 之后执行：
    //   FixedUpdate（物理）→ PreUpdate（物理结果应用）→ Update（你的代码）
    //
    // 子阶段：
    //   - ScriptRunBehaviourUpdate → ★ MonoBehaviour.Update()
    //   - DirectorUpdate → Timeline Update
    //   - ScriptRunDelayedDynamicFrameRate → 延迟的动态帧率回调
    //   - ScriptRunDelayedTasks → 延迟任务执行
    // ============================================================

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.Experimental.PlayerLoop")]
    public struct Update
    {
        /// <summary>
        ///   ★★★ MonoBehaviour.Update() 就在这里执行！★★★
        ///   所有继承 MonoBehaviour 且当前激活的对象，
        ///   它们的 Update() 方法在此子阶段被逐一调用。
        /// </summary>
        [RequiredByNativeCode]
        public struct ScriptRunBehaviourUpdate {}

        /// <summary>
        ///   Director（Timeline / Playable）更新。
        ///   Timeline 的普通（非固定步长）更新处理。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorUpdate {}

        /// <summary>
        ///   延迟执行的动态帧率脚本。
        /// </summary>
        [RequiredByNativeCode]
        public struct ScriptRunDelayedDynamicFrameRate {}

        /// <summary>
        ///   延迟任务执行。
        /// </summary>
        [RequiredByNativeCode]
        public struct ScriptRunDelayedTasks {}
    }

    // ============================================================
    // 7. PreLateUpdate — 晚期更新前阶段
    //
    // 功能：在 LateUpdate 和渲染之前执行的更新。
    //
    // 关键子阶段：
    //   - ScriptRunBehaviourLateUpdate → ★ MonoBehaviour.LateUpdate()
    //   - DirectorUpdateAnimationBegin/End → Timeline 动画更新
    //   - LegacyAnimationUpdate → 旧版动画更新
    //   - PhysicsLateUpdate → 物理晚期更新（如布料模拟）
    //   - UIElementsUpdatePanels → UI Toolkit 面板更新
    //   - ParticleSystemBeginUpdateAll → 粒子系统更新
    //   - AIUpdatePostScript → 脚本后的 AI 更新
    // ============================================================

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.Experimental.PlayerLoop")]
    public struct PreLateUpdate
    {
        [RequiredByNativeCode]
        public struct PhysicsCore2DLateUpdate { }

        /// <summary>
        ///   2D 物理晚期更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct Physics2DLateUpdate {}

        /// <summary>
        ///   3D 物理晚期更新（如布料模拟的更新）。
        /// </summary>
        [RequiredByNativeCode]
        public struct PhysicsLateUpdate {}

        /// <summary>
        ///   AI 系统脚本后更新。
        ///   在调用完脚本的 Update/LateUpdate 后更新 AI 状态。
        /// </summary>
        [RequiredByNativeCode]
        public struct AIUpdatePostScript {}

        /// <summary>
        ///   Director 动画更新开始。
        ///   Timeline 驱动的动画开始更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorUpdateAnimationBegin {}

        /// <summary>
        ///   旧版动画系统（Legacy Animation）更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct LegacyAnimationUpdate {}

        /// <summary>
        ///   Director 动画更新结束。
        ///   Timeline 驱动的动画更新完成。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorUpdateAnimationEnd {}

        /// <summary>
        ///   Director 延迟求值。
        ///   对需要延迟执行的 Timeline 求值操作。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorDeferredEvaluate {}

        [RequiredByNativeCode]
        public struct AccessibilityUpdate {}

        /// <summary>
        ///   UI Toolkit（UIElements）面板更新。
        ///   更新 UI Toolkit 的视觉元素面板状态。
        /// </summary>
        [RequiredByNativeCode]
        public struct UIElementsUpdatePanels {}

        [RequiredByNativeCode]
        public struct UpdateNetworkManager {}

        [RequiredByNativeCode]
        public struct UpdateMasterServerInterface {}

        /// <summary>
        ///   脚本 Update 完成后的图形作业结束同步。
        /// </summary>
        [RequiredByNativeCode]
        public struct EndGraphicsJobsAfterScriptUpdate {}

        /// <summary>
        ///   粒子系统本帧更新开始。
        ///   在 LateUpdate 之前更新所有粒子系统。
        /// </summary>
        [RequiredByNativeCode]
        public struct ParticleSystemBeginUpdateAll {}

        /// <summary>
        ///   ★ MonoBehaviour.LateUpdate() 就在这里执行。
        ///   LateUpdate 在 Update 之后、渲染之前执行。
        ///   常用于：第三人称摄像机跟随、状态同步等。
        ///   注意：LateUpdate 的执行顺序与 Update 不同，
        ///   不能保证所有 LateUpdate 在所有对象 Update 之后执行。
        /// </summary>
        [RequiredByNativeCode]
        public struct ScriptRunBehaviourLateUpdate {}

        /// <summary>
        ///   约束管理器更新（如 Position Constraint / Rotation Constraint 等）。
        /// </summary>
        [RequiredByNativeCode]
        public struct ConstraintManagerUpdate {}
    }

    // ============================================================
    // 8. ★ PostLateUpdate — ★ 晚期更新阶段
    //
    // 功能：PlayerLoop 中最后执行的大阶段。
    //       所有渲染、UI、音频、后处理、呈现操作都在这里完成。
    //
    // 这是 PlayerLoop 的最高潮：一切更新完成后，终于把画面画出来！
    //
    // 关键子阶段按执行顺序：
    //
    //   [帧开始通知]
    //   → PlayerSendFrameStarted        → 通知 Profiler 等工具帧开始
    //
    //   [UI 更新]
    //   → UpdateRectTransform           → 更新 UI RectTransform
    //   → PlayerUpdateCanvases          → ★ 更新所有 Canvas（UGUI 渲染！）
    //   → PlayerEmitCanvasGeometry      → 生成 Canvas 几何数据
    //
    //   [音频/视频]
    //   → UpdateAudio                   → 更新音频系统
    //   → UpdateVideo                   → 更新视频播放
    //
    //   [渲染准备]
    //   → UpdateAllRenderers            → ★ 更新所有渲染器（可见性剔除等）
    //   → UpdateLightProbeProxyVolumes  → 更新光照探针代理体
    //   → UpdateAllSkinnedMeshes        → 更新所有蒙皮网格（骨骼动画！）
    //   → SortingGroupsUpdate           → 更新 Sorting Group
    //
    //   [后处理与特效]
    //   → VFXUpdate                     → Visual Effect Graph 更新
    //   → ParticleSystemEndUpdateAll    → 粒子系统更新结束
    //   → DirectorRenderImage           → Timeline 渲染后处理
    //
    //   [呈现]
    //   → FinishFrameRendering          → ★ 完成帧渲染（提交渲染命令）
    //   → PresentAfterDraw              → ★ 呈现到屏幕（SwapBuffers！）
    //
    //   [帧结束清理]
    //   → PlayerSendFrameComplete       → 通知帧完成
    //   → GUIClearEvents                → 清除 GUI 事件
    //   → MemoryFrameMaintenance        → 内存帧维护
    //   → ProfilerEndFrame              → Profiler 帧结束
    //   → TriggerEndOfFrameCallbacks    → 触发协程 EndOfFrame
    // ============================================================

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.Experimental.PlayerLoop")]
    public struct PostLateUpdate
    {
        /// <summary>
        ///   通知 Profiler 等工具帧开始。
        ///   这是 PostLateUpdate 的第一个子阶段。
        /// </summary>
        [RequiredByNativeCode]
        public struct PlayerSendFrameStarted {}

        /// <summary>
        ///   更新 UI RectTransform。
        ///   在 Canvas 更新前重新计算 UI 元素的布局。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateRectTransform {}

        [RequiredByNativeCode]
        public struct UpdateCanvasRectTransform {}

        /// <summary>
        ///   ★ 更新所有 UGUI Canvas！
        ///   触发 Canvas 中所有 Graphic 组件的网格重建和布局更新。
        ///   UI 的实际渲染命令在此子阶段被构建。
        /// </summary>
        [RequiredByNativeCode]
        public struct PlayerUpdateCanvases {}

        [RequiredByNativeCode]
        public struct AccessibilityLateUpdate {}

        /// <summary>
        ///   UI Toolkit 重绘面板（内部使用）。
        /// </summary>
        [RequiredByNativeCode]
        internal struct UIElementsRepaintPanels {}

        /// <summary>
        ///   更新音频系统。
        ///   处理 AudioSource 的播放状态、3D 音频衰减计算等。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateAudio {}

        /// <summary>
        ///   更新视频播放器。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateVideo {}

        /// <summary>
        ///   Director（Timeline）晚期更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorLateUpdate {}

        [RequiredByNativeCode]
        public struct ScriptRunDelayedDynamicFrameRate {}

        /// <summary>
        ///   Visual Effect Graph 更新。
        ///   更新 VFX Graph 粒子系统。
        /// </summary>
        [RequiredByNativeCode]
        public struct VFXUpdate {}

        /// <summary>
        ///   粒子系统更新结束。在渲染前完成所有粒子更新。
        /// </summary>
        [RequiredByNativeCode]
        public struct ParticleSystemEndUpdateAll {}

        /// <summary>
        ///   图形作业结束时同步（LateUpdate 后）。
        /// </summary>
        [RequiredByNativeCode]
        public struct EndGraphicsJobsAfterScriptLateUpdate {}

        [RequiredByNativeCode]
        public struct UpdateSubstance {}

        /// <summary>
        ///   更新自定义渲染纹理（Custom Render Texture）。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateCustomRenderTextures {}

        /// <summary>
        ///   XR 晚期更新处理。
        /// </summary>
        [RequiredByNativeCode]
        public struct XRPostLateUpdate {}

        /// <summary>
        ///   ★ 更新所有渲染器！
        ///   执行可见性剔除（Frustum Culling / Occlusion Culling），
        ///   更新每个渲染器的 LOD 级别和可见性状态。
        ///   这是渲染管线开始前的重要准备步骤。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateAllRenderers {}

        /// <summary>
        ///   更新光照探针代理体积（Light Probe Proxy Volume）。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateLightProbeProxyVolumes {}

        /// <summary>
        ///   更新 Enlighten 实时全局光照运行时数据。
        /// </summary>
        [RequiredByNativeCode]
        public struct EnlightenRuntimeUpdate {}

        /// <summary>
        ///   ★ 更新所有蒙皮网格！（骨骼动画）
        ///   执行所有 SkinnedMeshRenderer 的骨骼变换和蒙皮计算。
        ///   通常在渲染前最后一步更新骨骼动画。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateAllSkinnedMeshes {}

        [RequiredByNativeCode]
        public struct ProcessWebSendMessages {}

        [RequiredByNativeCode]
        public struct RenderAs2DUpdate { }

        /// <summary>
        ///   更新 Sorting Group（排序组），确定渲染顺序。
        /// </summary>
        [RequiredByNativeCode]
        public struct SortingGroupsUpdate {}

        /// <summary>
        ///   更新视频纹理。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateVideoTextures {}

        /// <summary>
        ///   Director 渲染图像。执行 Timeline 驱动的后处理效果。
        /// </summary>
        [RequiredByNativeCode]
        public struct DirectorRenderImage {}

        /// <summary>
        ///   生成 Canvas 几何数据。
        ///   将 UGUI 的顶点数据提交到渲染线程。
        /// </summary>
        [RequiredByNativeCode]
        public struct PlayerEmitCanvasGeometry {}

        [RequiredByNativeCode]
        internal struct UIElementsRenderBatchModeOffscreen {}

        /// <summary>
        ///   ★ 完成帧渲染！
        ///   提交所有渲染命令到渲染线程，等待渲染完成。
        /// </summary>
        [RequiredByNativeCode]
        public struct FinishFrameRendering {}

        /// <summary>
        ///   批处理模式更新（用于 Unity 命令行构建等无窗口模式）。
        /// </summary>
        [RequiredByNativeCode]
        public struct BatchModeUpdate {}

        /// <summary>
        ///   通知帧渲染完成。
        /// </summary>
        [RequiredByNativeCode]
        public struct PlayerSendFrameComplete {}

        [RequiredByNativeCode]
        public struct UpdateCaptureScreenshot {}

        /// <summary>
        ///   ★ 呈现到屏幕！SwapBuffers！
        ///   将渲染好的帧缓冲区交换到显示器。
        ///   这是每一帧的最后一步。
        /// </summary>
        [RequiredByNativeCode]
        public struct PresentAfterDraw {}

        /// <summary>
        ///   清除立即模式的渲染器数据。
        /// </summary>
        [RequiredByNativeCode]
        public struct ClearImmediateRenderers {}

        /// <summary>
        ///   XR 后呈现处理。
        /// </summary>
        [RequiredByNativeCode]
        public struct XRPostPresent {}

        /// <summary>
        ///   更新屏幕分辨率设置。
        ///   Screen.SetResolution 的延迟应用。
        /// </summary>
        [RequiredByNativeCode]
        public struct UpdateResolution {}

        /// <summary>
        ///   输入系统帧结束处理。清空本帧的输入数据。
        /// </summary>
        [RequiredByNativeCode]
        public struct InputEndFrame {}

        /// <summary>
        ///   清除 GUI 事件队列。
        /// </summary>
        [RequiredByNativeCode]
        public struct GUIClearEvents {}

        /// <summary>
        ///   Shader 错误处理。检测并报告 Shader 编译错误。
        /// </summary>
        [RequiredByNativeCode]
        public struct ShaderHandleErrors {}

        /// <summary>
        ///   重置输入轴。清除本帧的输入轴数据。
        /// </summary>
        [RequiredByNativeCode]
        public struct ResetInputAxis {}

        [RequiredByNativeCode]
        public struct ThreadedLoadingDebug {}

        /// <summary>
        ///   同步 Profiler 统计数据。
        /// </summary>
        [RequiredByNativeCode]
        public struct ProfilerSynchronizeStats {}

        /// <summary>
        ///   内存帧维护。执行每帧的内存管理操作（如延迟释放）。
        /// </summary>
        [RequiredByNativeCode]
        public struct MemoryFrameMaintenance {}

        /// <summary>
        ///   执行 Game Center 回调。
        /// </summary>
        [RequiredByNativeCode]
        public struct ExecuteGameCenterCallbacks {}

        [RequiredByNativeCode]
        public struct XRPreEndFrame {}

        /// <summary>
        ///   ★ Profiler 帧结束标记。
        ///   记录本帧的性能数据供 Profiler 窗口和使用。
        /// </summary>
        [RequiredByNativeCode]
        public struct ProfilerEndFrame {}

        [RequiredByNativeCode]
        public struct GraphicsStateCollectionWarmup {}

        /// <summary>
        ///   预热预加载的 Shader 变体。
        /// </summary>
        [RequiredByNativeCode]
        public struct GraphicsWarmupPreloadedShaders {}

        /// <summary>
        ///   呈现后通知发送。
        /// </summary>
        [RequiredByNativeCode]
        public struct PlayerSendFramePostPresent {}

        [RequiredByNativeCode]
        public struct PhysicsSkinnedClothBeginUpdate {}

        [RequiredByNativeCode]
        public struct PhysicsSkinnedClothFinishUpdate {}

        /// <summary>
        ///   ★ 触发协程 EndOfFrame 回调！
        ///   执行由 `yield return new WaitForEndOfFrame()` 等待的协程。
        ///   这是在所有渲染完成后最后执行的回调之一。
        /// </summary>
        [RequiredByNativeCode]
        public struct TriggerEndOfFrameCallbacks {}

        [RequiredByNativeCode]
        public struct ObjectDispatcherPostLateUpdate { }
    }
}

// ============================================================
// UnityEngine.LowLevel — 自定义 PlayerLoop API
//
// 从 Unity 2019.3 开始，用户可以通过这些类自定义 PlayerLoop！
//
// 使用场景：
//   - 插入自定义的阶段到 Update 和 FixedUpdate 之间
//   - 移除不需要的引擎阶段（如 ProfilerEndFrame）以提升性能
//   - 为 ECS/DOTS 系统创建自定义执行顺序
//   - 实现自己的帧率控制逻辑
//
// 使用示例：
//   var loop = PlayerLoop.GetDefaultPlayerLoop();
//   // 修改 loop 的子系统列表...
//   PlayerLoop.SetPlayerLoop(loop);
//
// 注意：修改 PlayerLoop 是高危操作，错误的修改可能导致引擎行为异常。
// ============================================================

namespace UnityEngine.LowLevel
{
    /// <summary>
    ///   PlayerLoop 系统内部表示（引擎内部使用）。
    ///   这是 C# 侧与 C++ PlayerLoop 引擎之间的数据结构桥梁。
    /// </summary>
    [NativeHeader("Runtime/Misc/PlayerLoop.h")]
    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.Experimental.LowLevel")]
    struct PlayerLoopSystemInternal
    {
        public Type type;                                    // 标识类型（对应上面的 struct 类型）
        public PlayerLoopSystem.UpdateFunction updateDelegate; // C# 更新委托
        public IntPtr updateFunction;                         // C++ 原生更新函数指针
        public IntPtr loopConditionFunction;                  // 循环条件函数指针（控制是否执行）
        public int numSubSystems;                             // 子系统数量（包括自身）
    }

    /// <summary>
    ///   PlayerLoop 系统结构体。
    ///   表示 PlayerLoop 中的一个节点，可以是阶段、子阶段或自定义系统。
    ///   用户可以构建自己的 PlayerLoopSystem 树来修改 Unity 的执行序列。
    /// </summary>
    [MovedFrom("UnityEngine.Experimental.LowLevel")]
    public struct PlayerLoopSystem
    {
        /// <summary>
        ///   标识类型。通常使用 UnityEngine.PlayerLoop 命名空间下的 struct 类型。
        /// </summary>
        public Type type;

        /// <summary>
        ///   子系统列表。每个子系统本身也是一个 PlayerLoopSystem，形成树形结构。
        /// </summary>
        public PlayerLoopSystem[] subSystemList;

        /// <summary>
        ///   更新函数委托。此阶段要执行的 C# 回调。
        ///   如果为 null，则只执行子系统列表中的系统。
        /// </summary>
        public UpdateFunction updateDelegate;

        /// <summary>
        ///   C++ 原生更新函数指针（内部使用）。
        /// </summary>
        public IntPtr updateFunction;

        /// <summary>
        ///   循环条件函数指针。
        ///   控制是否执行此系统及其子系统。
        ///   例如 PhysicsFixedUpdate 会在 FixedUpdate 时间累积足够时才执行。
        /// </summary>
        public IntPtr loopConditionFunction;

        /// <summary>
        ///   更新函数委托类型。
        /// </summary>
        public delegate void UpdateFunction();

        public override string ToString() => type.Name;
    }

    /// <summary>
    ///   PlayerLoop 控制类。
    ///   提供获取/设置 Unity 主循环的静态 API。
    ///   
    ///   使用方式：
    ///   1. PlayerLoop.GetDefaultPlayerLoop() → 获取默认的 PlayerLoop 层次结构
    ///   2. 修改返回的 PlayerLoopSystem 树
    ///   3. PlayerLoop.SetPlayerLoop(modifiedLoop) → 应用修改
    ///   
    ///   重置方式：调用 SetPlayerLoop(GetDefaultPlayerLoop()) 恢复默认。
    /// </summary>
    [MovedFrom("UnityEngine.Experimental.LowLevel")]
    public class PlayerLoop
    {
        /// <summary>
        ///   获取默认的 PlayerLoop 层次结构。
        ///   返回一个 PlayerLoopSystem 树，包含引擎默认的所有阶段和子阶段。
        /// </summary>
        public static PlayerLoopSystem GetDefaultPlayerLoop()
        {
            var intSys = GetDefaultPlayerLoopInternal();
            var offset = 0;
            return InternalToPlayerLoopSystem(intSys, ref offset);
        }

        /// <summary>
        ///   获取当前的 PlayerLoop 层次结构。
        ///   如果之前通过 SetPlayerLoop 修改过，则返回修改后的结构。
        /// </summary>
        public static PlayerLoopSystem GetCurrentPlayerLoop()
        {
            var intSys = GetCurrentPlayerLoopInternal();
            var offset = 0;
            return InternalToPlayerLoopSystem(intSys, ref offset);
        }

        /// <summary>
        ///   设置新的 PlayerLoop 层次结构。
        ///   传入自定义的 PlayerLoopSystem 树来替换当前的主循环。
        ///   注意：修改后无法通过 GetCurrentPlayerLoop 以外的 API 恢复，
        ///   请务必保留 GetDefaultPlayerLoop 的引用以便恢复。
        /// </summary>
        /// <param name="loop">新的 PlayerLoop 层次结构</param>
        public static void SetPlayerLoop(PlayerLoopSystem loop)
        {
            var intSys = new List<PlayerLoopSystemInternal>();
            PlayerLoopSystemToInternal(loop, ref intSys);
            SetPlayerLoopInternal(intSys.ToArray());
        }

        /// <summary>
        ///   将 C# PlayerLoopSystem 树扁平化为内部 PlayerLoopSystemInternal[] 数组。
        ///   因为 C++ 侧需要的是一个平面数组而非树形结构，
        ///   子系统通过 numSubSystems 字段来界定范围。
        /// </summary>
        static int PlayerLoopSystemToInternal(PlayerLoopSystem sys, ref List<PlayerLoopSystemInternal> internalSys)
        {
            var idx = internalSys.Count;
            var newSys = new PlayerLoopSystemInternal
            {
                type = sys.type,
                updateDelegate = sys.updateDelegate,
                updateFunction = sys.updateFunction,
                loopConditionFunction = sys.loopConditionFunction,
                numSubSystems = 0
            };
            internalSys.Add(newSys);
            if (sys.subSystemList != null)
            {
                for (int i = 0; i < sys.subSystemList.Length; ++i)
                {
                    newSys.numSubSystems += PlayerLoopSystemToInternal(sys.subSystemList[i], ref internalSys);
                }
            }
            internalSys[idx] = newSys;
            return newSys.numSubSystems + 1;
        }

        /// <summary>
        ///   将内部 PlayerLoopSystemInternal[] 数组还原为 C# PlayerLoopSystem 树。
        ///   通过递归读取 offset 指针和 numSubSystems 来重建层级结构。
        /// </summary>
        static PlayerLoopSystem InternalToPlayerLoopSystem(PlayerLoopSystemInternal[] internalSys, ref int offset)
        {
            var sys = new PlayerLoopSystem
            {
                type = internalSys[offset].type,
                updateDelegate = internalSys[offset].updateDelegate,
                updateFunction = internalSys[offset].updateFunction,
                loopConditionFunction = internalSys[offset].loopConditionFunction,
                subSystemList = null
            };

            var idx = offset++;
            if (internalSys[idx].numSubSystems > 0)
            {
                var subsys = new List<PlayerLoopSystem>();
                while (offset <= idx + internalSys[idx].numSubSystems)
                    subsys.Add(InternalToPlayerLoopSystem(internalSys, ref offset));
                sys.subSystemList = subsys.ToArray();
            }

            return sys;
        }

        /// <summary>
        ///   C++ 侧：获取默认的 PlayerLoop 内部表示。
        /// </summary>
        [NativeMethod(IsFreeFunction = true)]
        private static extern PlayerLoopSystemInternal[] GetDefaultPlayerLoopInternal();

        /// <summary>
        ///   C++ 侧：获取当前的 PlayerLoop 内部表示。
        /// </summary>
        [NativeMethod(IsFreeFunction = true)]
        private static extern PlayerLoopSystemInternal[] GetCurrentPlayerLoopInternal();

        /// <summary>
        ///   C++ 侧：设置 PlayerLoop 内部表示。
        /// </summary>
        [NativeMethod(IsFreeFunction = true)]
        private static extern void SetPlayerLoopInternal(PlayerLoopSystemInternal[] loop);
    }
}
