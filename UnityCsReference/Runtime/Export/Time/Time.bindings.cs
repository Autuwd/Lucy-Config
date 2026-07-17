// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 Time — Unity 引擎时间系统核心接口
// ================================================================
//
// 【概述】
// Time 类是 Unity 提供的全局时间信息接口，所有成员均为 static。
// 它是游戏循环的心脏 —— 驱动 Update、FixedUpdate、物理模拟、动画、
// 协程等一切基于时间的系统。
//
// 【三条时间线】
// Unity 内部维护三条独立的时间线，理解它们是掌握 Time API 的关键：
//
//   1. 游戏时间（Game Time）= time / deltaTime / fixedTime
//      - 受 timeScale 影响
//      - timeScale=0 时冻结，常用于暂停菜单
//      - deltaTime 是上一帧到当前帧的间隔（用于 Update）
//      - fixedDeltaTime 是物理步进间隔（用于 FixedUpdate）
//
//   2. 非缩放时间（Unscaled Time）= unscaledTime / unscaledDeltaTime
//      - 不受 timeScale 影响，始终真实流逝
//      - 用途：暂停菜单的 UI 动画、倒计时器
//      - 在 timeScale=0 时依然递增
//
//   3. 真实时间（Real Time）= realtimeSinceStartup
//      - 完全不受任何控制，反映系统时钟
//      - 即使编辑器暂停/应用切后台也在流逝（某些平台除外）
//      - 用途：性能计时、网络同步基准
//
// 【时间线关系图】
//
//   真实时间（realtimeSinceStartup）─────── 不受控，始终流逝
//        │
//        ├── × timeScale ──→ 游戏时间（time）
//        │                      ├── deltaTime（帧间隔）
//        │                      └── fixedDeltaTime（物理间隔）
//        │
//        └──────────────────→ 非缩放时间（unscaledTime）
//                               ├── unscaledDeltaTime
//                               └── fixedUnscaledDeltaTime
//
// 【帧循环中的时间流动】
//
//   ┌─ FixedUpdate（固定频率，默认 50Hz / 0.02s）─────────────┐
//   │  一帧内可能调用 0~N 次 FixedUpdate                        │
//   │  每次 FixedUpdate 使用 fixedDeltaTime（恒定值）            │
//   └──────────────────────────────────────────────────────────┘
//                        │
//   ┌─ Update（每帧一次，频率不固定）─────────────────────────┐
//   │  deltaTime = 本帧耗时（波动值）                          │
//   │  smoothDeltaTime = deltaTime 的平滑滤波值                │
//   └──────────────────────────────────────────────────────────┘
//                        │
//   ┌─ LateUpdate（每帧一次，在 Update 之后）────────────────┐
//   │  同样使用 deltaTime                                     │
//   └──────────────────────────────────────────────────────────┘
//
// 【常见使用模式】
//   - 移动：transform.position += speed * Time.deltaTime;  （帧率无关）
//   - 物理：rb.AddForce(force * Time.fixedDeltaTime);     （物理步长相关）
//   - 暂停UI动画：Time.unscaledDeltaTime                   （不受 timeScale 录响）
//   - 录屏定帧：Time.captureFramerate = 30;                （锁定输出帧率）
//   - 性能计时：var sw = Stopwatch.StartNew(); ... sw.Elapsed
//               或 realtimeSinceStartup 做差值
//
// 【对应回调】
//   FixedUpdate()  ← fixedDeltaTime / fixedTime
//   Update()      ← deltaTime / time
//   LateUpdate()  ← deltaTime / time
//   coroutine     ← deltaTime / yield return WaitForSeconds
//
// 📍 对应 C++ 头文件：Runtime/Input/TimeManager.h
// ================================================================

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // ================================================================
    // Time — Unity 时间查询接口（全部 static 成员）
    // ================================================================
    //
    // Time 是 Unity 引擎的时间信息中枢，所有属性和方法都是静态的，
    // 可以在任何地方通过 Time.xxx 直接访问。
    //
    // 【底层实现】
    // - C++ 端由 TimeManager 管理所有时间状态
    // - [StaticAccessor("GetTimeManager()")] 获取全局单例指针
    // - 所有 extern 属性直接读取 C++ 内存，零托管开销
    //
    // 【属性分类速查表】
    //
    //   ┌──────────────────┬──────────────────────────────────────┐
    //   │ 类别             │ 属性                                 │
    //   ├──────────────────┼──────────────────────────────────────┤
    //   │ 帧计时           │ time, deltaTime, smoothDeltaTime    │
    //   │                  │ frameCount, timeSinceLevelLoad       │
    //   ├──────────────────┼──────────────────────────────────────┤
    //   │ 固定步进         │ fixedTime, fixedDeltaTime            │
    //   │                  │ inFixedTimeStep                      │
    //   ├──────────────────┼──────────────────────────────────────┤
    //   │ 非缩放时间       │ unscaledTime, unscaledDeltaTime      │
    //   │                  │ fixedUnscaledTime, fixedUnscaledDt   │
    //   ├──────────────────┼──────────────────────────────────────┤
    //   │ 时间控制         │ timeScale, maximumDeltaTime          │
    //   │                  │ maximumParticleDeltaTime             │
    //   ├──────────────────┼──────────────────────────────────────┤
    //   │ 真实时间/录屏    │ realtimeSinceStartup                 │
    //   │                  │ captureDeltaTime, captureFramerate   │
    //   └──────────────────┴──────────────────────────────────────┘
    //
    // 【设计哲学】
    // Unity 将时间抽象为多个维度（游戏时间/非缩放/真实），
    // 让开发者可以在不同时刻选择正确的时间基准：
    //   - 游戏逻辑 → time / deltaTime（可暂停）
    //   - UI 动画  → unscaledDeltaTime（暂停时仍动）
    //   - 性能度量 → realtimeSinceStartup（绝对真实）
    //
    // 【⚠️ 常见陷阱】
    // 1. deltaTime 在第一帧可能为 0 或异常大，需要做 clamp 处理
    // 2. fixedDeltaTime 仅在 Project Settings 中配置，不要在运行时随意修改
    // 3. timeScale=0 不会停止 FixedUpdate，只是 fixedTime 不再递增
    // 4. realtimeSinceStartup 在编辑器暂停时的行为因平台而异
    //
    // 官方文档：https://docs.unity3d.com/ScriptReference/Time.html
    // ================================================================
    [NativeHeader("Runtime/Input/TimeManager.h")]
    [StaticAccessor("GetTimeManager()", StaticAccessorType.Dot)]
    // The interface to get time information from Unity.
    public class Time
    {
        // ================================================================
        // 📌 帧计时（Frame Timing）— 游戏时间维度
        // ================================================================
        // 这组属性提供基于"游戏时间"的帧级时间信息。
        // 游戏时间受 timeScale 控制，timeScale=0 时这些值冻结。
        //
        // 【与 Update 的关系】
        // Unity 主循环每帧执行一次 Update()：
        //   Update() {
        //       // 此时 Time.time = 本帧开始时的游戏时间
        //       // 此时 Time.deltaTime = 本帧耗时（秒）
        //       // 此时 Time.frameCount = 已渲染的总帧数
        //   }
        //
        // 【deltaTime vs smoothDeltaTime】
        // - deltaTime：本帧实际耗时，波动较大（掉帧时突增）
        // - smoothDeltaTime：deltaTime 的指数移动平均，更平滑
        //   适合对平滑度敏感的场景（如摄像机跟随）
        //
        // ⚡ 性能提示：在 Update 中使用 Time.deltaTime 做帧率无关运动，
        //    而非假设固定帧率（如 60fps）。这样无论目标平台帧率如何，
        //    运动速度都保持一致。
        //
        // 💡 典型用法：
        //   // 帧率无关的匀速移动
        //   transform.Translate(Vector3.forward * speed * Time.deltaTime);
        //
        //   // 使用 smoothDeltaTime 的平滑摄像机跟随
        //   float step = speed * Time.smoothDeltaTime;
        //   transform.position = Vector3.Lerp(transform.position, target, step);
        //
        // ⚠️ 注意：第一帧的 deltaTime 可能为 0 或异常大（资源加载），
        //    建议 clamp：float dt = Mathf.Min(Time.deltaTime, 0.1f);
        // ================================================================

        // The time this frame has started (RO). This is the time in seconds since the start of the game.
        //
        // 🎯 time — 游戏启动以来经过的秒数（受 timeScale 影响）
        //    这是 Unity 中最基础的时间参考点。
        //    从第一帧开始累计，随 timeScale 缩放。
        //    float 精度在长时间运行（>4小时）后会出现精度丢失，
        //    长期运行项目请使用 timeAsDouble。
        [NativeProperty("CurTime")]
        public static extern float time { get; }

        // The time this frame has started (RO). This is the time in seconds since the start of the game. Double precision version of time, please prefer to use it instead of single precision (float).
        [NativeProperty("CurTime")]
        public static extern double timeAsDouble { get; }

        [NativeProperty("CurTimeRational")]
        public static extern Unity.IntegerTime.RationalTime timeAsRational { get; }

        // The time this frame has started (RO). This is the time in seconds since the last level has been loaded.
        //
        // 🎯 timeSinceLevelLoad — 当前场景加载以来经过的秒数
        //    每次 SceneManager.LoadScene() 后自动重置为 0。
        //    用途：场景加载后的过场计时、加载画面显示时长控制。
        [NativeProperty("TimeSinceSceneLoad")]
        public static extern float timeSinceLevelLoad { get; }

        // The time this frame has started (RO). This is the time in seconds since the last level has been loaded. Double precision version of timeSinceLevelLoad, please prefer to use it instead of single precision (float).
        [NativeProperty("TimeSinceSceneLoad")]
        public static extern double timeSinceLevelLoadAsDouble { get; }

        // The time in seconds it took to complete the last frame (RO).
        //
        // 🎯 deltaTime — 上一帧到当前帧的时间间隔（秒）
        //    这是 Unity 中使用频率最高的 Time 属性。
        //    在 Update() 中使用，实现帧率无关的运动和逻辑。
        //
        //    公式：newPos = oldPos + direction * speed * deltaTime
        //    无论帧率是 30fps（dt≈0.033）还是 120fps（dt≈0.008），
        //    每秒移动的距离都等于 speed 值。
        //
        // ⚠️ 注意：受 timeScale 影响。timeScale=0 时 deltaTime=0，
        //    所有基于 deltaTime 的运动都会停止。这正是暂停功能的原理。
        public static extern float deltaTime { get; }

        // ================================================================
        // 📌 固定步进（Fixed Timestep）— 物理时间维度
        // ================================================================
        // 固定步进系统以恒定频率调用 FixedUpdate()，与渲染帧率解耦。
        // 默认 fixedDeltaTime = 0.02s（即 50Hz），可在 Project Settings
        // → Time → Fixed Timestep 中配置。
        //
        // 【与 FixedUpdate 的关系】
        // Unity 主循环在一个渲染帧内可能调用 0~N 次 FixedUpdate：
        //
        //   渲染帧 1（耗时 33ms，60fps 游戏出现掉帧）：
        //     FixedUpdate × 1（dt=0.02）→ 剩余 13ms 未用
        //     FixedUpdate × 0（累积不够 0.02s）
        //     Update()
        //     LateUpdate()
        //     渲染
        //
        //   渲染帧 2（耗时 8ms，帧率恢复）：
        //     FixedUpdate × 1（dt=0.02）→ 剩余 -13ms → 补偿！
        //     FixedUpdate × 1（dt=0.02）→ 继续补偿
        //     Update()
        //     LateUpdate()
        //     渲染
        //
        // 【物理步进的补偿机制】
        // 如果一帧耗时超过 fixedDeltaTime，Unity 会在下一帧
        // 补偿执行多次 FixedUpdate，确保物理模拟时间一致。
        // 但 maximumDeltaTime 限制了最大补偿量（默认 0.1s），
        // 防止"螺旋死亡"（spiral of death）。
        //
        // 💡 典型用法：
        //   void FixedUpdate() {
        //       // 在固定步进中使用 fixedDeltaTime
        //       rb.AddForce(Vector3.down * gravity * Time.fixedDeltaTime);
        //   }
        //
        // ⚠️ 不要在 FixedUpdate 中使用 deltaTime！
        //    deltaTime 是渲染帧间隔（波动值），fixedDeltaTime 才是物理间隔（恒定值）。
        //    混用会导致物理行为不稳定。
        // ================================================================

        // The time the latest MonoBehaviour::pref::FixedUpdate has started (RO). This is the time in seconds since the start of the game.
        public static extern float fixedTime { get; }

        // The time the latest MonoBehaviour::pref::FixedUpdate has started (RO). This is the time in seconds since the start of the game.
        // Double precision version of unscaledTime, please prefer to use it instead of single precision (float).
        [NativeProperty("FixedTime")]
        public static extern double fixedTimeAsDouble { get; }

        // ================================================================
        // 📌 非缩放时间（Unscaled Time）— 不受 timeScale 影响的时间
        // ================================================================
        // 非缩放时间始终真实流逝，不受 timeScale 缩放。
        // 即使 timeScale=0（游戏暂停），unscaledTime 和
        // unscaledDeltaTime 仍然正常递增。
        //
        // 【什么时候需要非缩放时间？】
        // 1. 暂停菜单的 UI 动画（按钮滑入、文字淡入）
        // 2. 暂停时的倒计时器（如：暂停后 5 秒自动退出）
        // 3. 过场动画中的字幕计时
        // 4. 任何在"游戏暂停"时仍需运行的视觉效果
        //
        // 💡 典型用法：
        //   void Update() {
        //       // 暂停时仍可运行的 UI 动画
        //       panel.alpha = Mathf.Lerp(panel.alpha, 1f,
        //           Time.unscaledDeltaTime * fadeInSpeed);
        //   }
        //
        // ⚡ 与 realtimeSinceStartup 的区别：
        //    unscaledTime 受平台暂停/切后台影响会暂停，
        //    realtimeSinceStartup 不受这些影响（更"真实"）。
        //    一般 UI 动画用 unscaledTime 即可。
        // ================================================================

        // The cached real time (realTimeSinceStartup) at the start of this frame
        public static extern float unscaledTime { get; }

        // The cached real time (realTimeSinceStartup) at the start of this frame. Double precision version of unscaledTime, please prefer to use it instead of single precision (float).
        [NativeProperty("UnscaledTime")]
        public static extern double unscaledTimeAsDouble { get; }

        // The real time corresponding to this fixed frame
        public static extern float fixedUnscaledTime { get; }

        // The real time corresponding to this fixed frame. Double precision version of unscaledTime, please prefer to use it instead of single precision (float).
        [NativeProperty("FixedUnscaledTime")]
        public static extern double fixedUnscaledTimeAsDouble { get; }

        // The delta time based upon the realTime
        public static extern float unscaledDeltaTime { get; }

        // The delta time based upon the realTime
        public static extern float fixedUnscaledDeltaTime { get; }

        // ================================================================
        // 📌 时间控制（Time Control）— 缩放与限制
        // ================================================================
        // 这组属性控制时间的流逝速度和最大帧耗时限制。
        //
        // 【timeScale — 时间缩放系数】
        // timeScale 是一个乘数因子，影响所有"游戏时间"属性：
        //   timeScale = 1.0  → 正常速度
        //   timeScale = 0.5  → 半速（慢动作效果）
        //   timeScale = 2.0  → 双速（快进效果）
        //   timeScale = 0.0  → 暂停（deltaTime=0，FixedUpdate 不执行）
        //
        // 受影响的属性：time, deltaTime, fixedTime, fixedDeltaTime
        // 不受影响的属性：unscaledTime, realtimeSinceStartup
        //
        // 【暂停实现】
        // 最简单的暂停方式：Time.timeScale = 0;
        // 恢复：Time.timeScale = 1f;
        //
        // ⚠️ 暂停时的注意事项：
        //    - FixedUpdate 不会被调用（fixedTime 不递增）
        //    - Update 仍会被调用，但 deltaTime=0
        //    - 协程中的 WaitForSeconds 也会暂停
        //    - 如需暂停时仍执行某些逻辑，用 unscaledDeltaTime
        //
        // 【maximumDeltaTime — 帧耗时上限】
        // 限制单帧最大耗时，防止"螺旋死亡"。
        // 当一帧超过此限制时，物理和动画会被截断。
        // 默认 0.1s（100ms），即最低保证 10fps 的物理更新。
        //
        // 💡 典型用法：
        //   // 慢动作效果（2 秒恢复正常）
        //   IEnumerator SlowMotion() {
        //       Time.timeScale = 0.3f;
        //       yield return new WaitForSecondsRealtime(2f);
        //       Time.timeScale = 1f;
        //   }
        //
        // ⚡ 注意：使用 WaitForSecondsRealtime 而非 WaitForSeconds，
        //    因为 timeScale=0 时 WaitForSeconds 会无限等待。
        // ================================================================

        // The interval in seconds at which physics and other fixed frame rate updates (like MonoBehaviour's MonoBehaviour::pref::FixedUpdate) are performed.
        //
        // 🎯 fixedDeltaTime — FixedUpdate 的固定时间间隔
        //    恒定值，不受帧率波动影响。默认 0.02s（50Hz）。
        //    可读可写，但通常只在 Project Settings 中配置。
        //    修改此值会影响物理模拟精度和性能的平衡。
        public static extern float fixedDeltaTime  { get; set; }

        // The maximum time a frame can take. Physics and other fixed frame rate updates (like MonoBehaviour's MonoBehaviour::pref::FixedUpdate)
        //
        // 🎯 maximumDeltaTime — 单帧最大耗时上限
        //    防止卡顿时物理模拟"螺旋死亡"（一帧累积太多步进→更卡→更多步进）。
        //    默认 0.1s（100ms），超出部分的物理步进会被丢弃。
        //    可在 Project Settings → Time → Maximum Allowed Timestep 中配置。
        public static extern float maximumDeltaTime  { get; set; }

        // A smoothed out Time.deltaTime (RO).
        //
        // 🎯 smoothDeltaTime — deltaTime 的平滑滤波版本
        //    使用指数移动平均平滑 deltaTime 的波动。
        //    适合对帧率波动敏感的场景（如摄像机平滑跟随）。
        //    与 deltaTime 的区别：smoothDeltaTime 不会突变，更稳定。
        public static extern float smoothDeltaTime { get; }

        // The maximum time a frame can spend on particle updates. If the frame takes longer than this, then updates are split into multiple smaller updates.
        //
        // 🎯 maximumParticleDeltaTime — 粒子系统单帧最大更新时间
        //    限制粒子系统在一帧内的最大更新量。
        //    超出时粒子更新被拆分为多个子步骤。
        //    防止卡顿时粒子堆积导致更大的性能问题。
        public static extern float maximumParticleDeltaTime  { get; set; }

        // The scale at which the time is passing. This can be used for slow motion effects.
        //
        // 🎯 timeScale — 时间缩放乘数（影响所有"游戏时间"属性）
        //    1.0 = 正常速度，0.5 = 半速，2.0 = 双速，0.0 = 暂停。
        //    设置为 0 是最简单的暂停实现。
        //    ⚠️ 修改此值会影响 deltaTime、fixedDeltaTime 等多个属性。
        public static extern float timeScale { get; set; }

        // The total number of frames that have passed (RO).
        //
        // 🎯 frameCount — 已渲染的总帧数
        //    从游戏启动开始累计，每渲染一帧 +1。
        //    可用于帧率计算、周期性任务（如每 N 帧执行一次）。
        public static extern int frameCount { get; }

        //*undocumented*
        [NativeProperty("RenderFrameCount")]
        public static extern int renderedFrameCount { get; }

        // ================================================================
        // 📌 真实时间与录屏帧率（Real Time & Capture）
        // ================================================================
        // 这组属性提供不受任何控制的"绝对真实时间"，
        // 以及用于录屏/GIF录制的帧率锁定功能。
        //
        // 【realtimeSinceStartup — 真实启动时间】
        // 从游戏启动至今的秒数，完全不受 timeScale 影响。
        // 与 unscaledTime 的区别：
        //   - unscaledTime：编辑器暂停时会冻结
        //   - realtimeSinceStartup：编辑器暂停时仍在流逝（某些平台）
        //
        // 💡 用途：
        //   // 性能计时
        //   float startTime = Time.realtimeSinceStartup;
        //   DoExpensiveOperation();
        //   float elapsed = Time.realtimeSinceStartup - startTime;
        //   Debug.Log($"耗时: {elapsed * 1000:F1}ms");
        //
        //   // 网络同步基准时间
        //   double serverTime = GetServerTimestamp();
        //   double localTime = Time.realtimeSinceStartupAsDouble;
        //
        // 【captureFramerate / captureDeltaTime — 录屏帧率锁定】
        // 用于录制游戏画面（如 GIF、视频录制）时锁定输出帧率。
        // 设置 captureFramerate = 30 后，Unity 强制以 30fps 渲染，
        // 无论实际硬件性能如何。游戏逻辑也以 30fps 的步长运行。
        //
        // 工作原理：
        //   captureFramerate = 30
        //   → captureDeltaTime = 1/30 ≈ 0.0333s
        //   → deltaTime 被锁定为 0.0333s（不再波动）
        //   → 每帧渲染输出 1/30 秒的内容
        //
        // ⚡ captureDeltaTime 是底层属性，captureFramerate 是便捷封装：
        //   captureFramerate = 30  ⟺  captureDeltaTime = 1/30
        //   captureFramerate = 0   ⟺  captureDeltaTime = 0（恢复自动帧率）
        //
        // 🎯 典型录屏流程：
        //   // 开始录制
        //   Time.captureFramerate = 30;
        //   // ... 游戏运行 ...
        //   // 停止录制
        //   Time.captureFramerate = 0;
        //
        // 💡 替代方案：Unity 的 Recorder 包提供了更强大的录制功能，
        //    支持多种格式和后处理，推荐使用 Recorder 而非手动 captureFramerate。
        // ================================================================

        // The real time in seconds since the game started (RO).
        [NativeProperty("Realtime")]
        public static extern float realtimeSinceStartup { get; }

        // The real time in seconds since the game started (RO). Double precision version of realtimeSinceStartup, please prefer to use it instead of single precision (float).
        [NativeProperty("Realtime")]
        public static extern double realtimeSinceStartupAsDouble { get; }

        // If /captureDeltaTime/ is set to a value larger than 0, time will advance by that increment.
        //
        // 🎯 captureDeltaTime — 录屏帧间隔（底层属性）
        //    设置后锁定 deltaTime 为固定值，实现定帧录制。
        //    0 表示不限制（默认），>0 表示每帧间隔。
        //    通常通过 captureFramerate 间接设置。
        public static extern float captureDeltaTime { get; set; }

        public static extern Unity.IntegerTime.RationalTime captureDeltaTimeRational { get; set; }

        // /captureFramerate/ is a convenience (and backwards compatible) accessor for the reciprocal of /captureDeltaTime/ rounded to the nearest integer.
        //
        // 🎯 captureFramerate — 录屏帧率（便捷属性，非 extern）
        //    设置目标帧率，Unity 会锁定 deltaTime 为 1/fps。
        //    0 = 恢复自动帧率，30 = 30fps 定帧录制。
        //    底层通过 captureDeltaTime 实现：fps=30 → dt=1/30。
        public static int captureFramerate
        {
            get
            {
                return captureDeltaTime == 0.0f ? 0 : (int)Mathf.Round(1.0f / captureDeltaTime);
            }
            set
            {
                captureDeltaTime = value == 0 ? 0.0f : 1.0f / value;
            }
        }

        // Returns true if inside a fixed time step callback such as FixedUpdate, otherwise false.
        //
        // 🎯 inFixedTimeStep — 当前是否在 FixedUpdate 回调内
        //    返回 true 表示当前代码正在 FixedUpdate() 中执行。
        //    用途：在共享方法中区分调用来源（Update vs FixedUpdate），
        //    选择使用 deltaTime 还是 fixedDeltaTime。
        public static extern bool inFixedTimeStep
        {
            [NativeName("IsUsingFixedTimeStep")]
            get;
        }
    }
}
