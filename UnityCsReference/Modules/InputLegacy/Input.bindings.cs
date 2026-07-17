// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 Input — Unity 传统输入系统（Legacy Input System）
// ================================================================
//
// 【概述】
// Input 类是 Unity 最早期的输入 API，自 Unity 1.0 时代即存在。
// 它提供了一套基于轮询（Polling）的静态接口，用于读取键盘、鼠标、
// 触摸屏、手柄、加速度计、陀螺仪等设备的输入状态。
//
// 【架构分层 — 从 C# 到硬件】
//
//   ┌─────────────────────────────────────────────────────┐
//   │  你的 C# 脚本（Input.GetKey / GetAxis 等）           │
//   └────────────────────┬────────────────────────────────┘
//                        │ P/Invoke (extern 方法)
//   ┌────────────────────▼────────────────────────────────┐
//   │  InputBindings.cpp — C++ 胶水层                       │
//   │  （Runtime/Input/InputBindings.h）                    │
//   └────────────────────┬────────────────────────────────┘
//                        │ 调用平台抽象层
//   ┌────────────────────▼────────────────────────────────┐
//   │  Platform Abstraction Layer（PAL）                    │
//   │  各平台有自己的输入采集实现：                           │
//   │  - Windows: Win32 API / Raw Input / XInput           │
//   │  - macOS:   IOKit / Game Controller framework        │
//   │  - Linux:   evdev / libevdev                         │
//   │  - Android: Android KeyEvent / MotionEvent NDK       │
//   │  - iOS:     UIKit touchesBegan/Moved/Ended           │
//   │  - WebGL:   HTML5 Event Listeners (keydown 等)       │
//   │  - Consoles: 各平台 SDK（PS/Xbox/Nintendo）           │
//   └─────────────────────────────────────────────────────┘
//
// 【轮询模型 vs 事件模型】
//
//   Input 类采用 **轮询模型**：你在 Update() 中主动调用
//   GetKey / GetAxis 来"查询"当前帧的输入状态。
//
//   优点：简单直观，适合大多数游戏场景
//   缺点：每帧都在查询，无法区分"谁触发了事件"
//
//   ⚡ 新 Input System 包（com.unity.inputsystem）采用 **事件驱动模型**，
//   通过 InputAction / InputActionAsset 声明式地绑定输入，
//   支持组合键、重映射、多设备自动切换等高级功能。
//
// 【InputManager.asset — 虚拟轴系统】
//
//   GetAxis / GetButton 系列方法并不直接读取物理按键，
//   而是通过 "InputManager.asset" 中定义的 **虚拟轴（Virtual Axis）** 来工作。
//
//   虚拟轴配置项（在 Edit → Project Settings → Input Manager 中设置）：
//   - Name：轴名称（如 "Horizontal"、"Vertical"）
//   - Type：Key/Mouse Movement（键盘映射）或 Mouse Movement（鼠标移动）
//   - Gravity：松手后回归零点的速度（越大越快回零）
//   - Sensitivity：按下时到达极值的速度（越大响应越灵敏）
//   - Dead：死区值（避免手柄漂移产生微小输入）
//   - Snap：是否在反方向按键时立即归零再反向
//   - Positive/Negative Button：映射到哪个物理按键
//   - Axis：映射到手柄的哪个轴
//
//   💡 这就是为什么 GetAxis("Horizontal") 能同时响应键盘 WASD 和手柄摇杆：
//   它不是绑定某个物理键，而是读取一个"虚拟轴"的配置值。
//
// 【GetAxis vs GetAxisRaw — 平滑 vs 原始】
//
//   GetAxis(name)      → 经过平滑处理，返回 -1~1 之间的渐变值
//                         内部使用了 Gravity（减速）和 Sensitivity（加速）参数
//                         适合角色移动，手感更自然
//
//   GetAxisRaw(name)   → 直接返回物理状态，只有 -1、0、1 三个值
//                         未经平滑，适合需要精确判断方向的场景
//
// 【新旧 Input System 关系】
//
//   ⚠️ 传统 Input 类（本文件）与新的 Input System 包可以共存，但需要注意：
//   1. 两者同时启用时，默认行为可能互相干扰
//   2. 可在 Player Settings → Active Input Handling 中选择：
//      - Input Manager (Old)：仅用传统系统
//      - Input System Package (New)：仅用新系统
//      - Both：两者同时启用（推荐过渡期使用）
//   3. 新系统的 InputAction 读取方式完全不同，建议新项目直接用新系统
//   4. 本文件属于 InputLegacy 模块，新系统代码在 com.unity.inputsystem 包中
//
// 【与 InputUnsafeUtility 的关系】
//
//   本文件中的 Input 类方法大多是对 InputUnsafeUtility 的薄封装。
//   InputUnsafeUtility 位于 UnityEngine.Internal 命名空间，
//   提供了 Burst Compiler 兼容的 unsafe 版本（__Unmanaged 后缀），
//   使得输入查询可以在 Burst 编译的 Job 中使用。
//
// 【线程安全注意事项】
//   ⚠️ Input 类的所有方法都必须在主线程调用！
//   因为底层直接读取主线程缓存的输入状态，跨线程调用会导致未定义行为。
//
// 📍 对应 C++ 头文件：Runtime/Input/InputBindings.h
// ================================================================

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // ================================================================
    // 📌 InputLegacy 模块 — 传统输入系统的数据类型定义
    // ================================================================
    // 本命名空间包含传统输入系统所需的枚举、结构体和核心类：
    //
    // 枚举类型：
    //   TouchPhase        — 触摸阶段（Began/Moved/Stationary/Ended/Canceled）
    //   IMECompositionMode — 输入法组合模式控制
    //   TouchType          — 触摸类型（直接/间接/触控笔）
    //   PenStatus          — 触控笔状态（接触/笔杆/反转/橡皮擦）
    //   PenEventType       — 触控笔事件类型
    //   DeviceOrientation  — 设备物理朝向
    //   LocationServiceStatus — 定位服务状态
    //
    // 结构体类型：
    //   Touch             — 单次触摸事件的完整数据
    //   PenData           — 触控笔数据
    //   AccelerationEvent — 加速度传感器事件
    //   LocationInfo      — GPS 定位信息
    //
    // 核心类：
    //   Input             — 传统输入系统的主入口（静态 API）
    //   Gyroscope         — 陀螺仪数据读取
    //   LocationService   — GPS 定位服务
    //   Compass           — 电子罗盘
    // ================================================================

    // ================================================================
    // TouchPhase — 触摸事件的生命周期阶段
    // ================================================================
    // 描述一次触摸从开始到结束的各个阶段。
    //
    // 📌 典型的触摸生命周期：
    //   Began → Moved → ... → Moved → Ended
    //                    或
    //   Began → Moved → ... → Canceled（如来电中断）
    //
    // 💡 使用场景：
    //   - Began：初始化触摸相关的 UI/游戏逻辑
    //   - Moved：拖拽操作、滑动手势
    //   - Stationary：触摸但未移动（如长按检测）
    //   - Ended：正常结束，清理状态
    //   - Canceled：系统取消（如弹出对话框、来电）
    //
    // ⚡ 注意：Stationary 阶段不一定每帧都报告，
    //    取决于平台和设备的触摸采样率。
    // ================================================================
    public enum TouchPhase
    {
        Began = 0,
        Moved = 1,
        Stationary = 2,
        Ended = 3,
        Canceled = 4
    }

    // ================================================================
    // IMECompositionMode — 输入法（IME）组合模式
    // ================================================================
    // 控制 Unity 如何处理输入法组合输入（如中文、日文、韩文输入）。
    //
    // 📌 各模式含义：
    //   Auto：由平台自动决定何时启用 IME
    //   On：强制启用 IME（输入框获得焦点时）
    //   Off：强制禁用 IME
    //
    // 💡 中文输入法的工作流程：
    //   用户按键 → IME 弹出候选框 → 用户选词 → compositionString 返回最终文字
    //   通过 compositionString 属性可以获取当前正在组合的字符串。
    //   compositionCursorPos 可控制候选框的显示位置。
    //
    // ⚠️ 仅在需要自定义中文/日文输入行为时才需要设置此属性。
    //    大多数情况下 Auto 模式就够了。
    // ================================================================
    public enum IMECompositionMode
    {
        Auto = 0,
        On = 1,
        Off = 2
    }

    // ================================================================
    // TouchType — 触摸的物理类型
    // ================================================================
    // 区分触摸输入的物理来源方式。
    //
    // 📌 类型说明：
    //   Direct：直接触摸 — 手指直接接触屏幕表面（最常见的触摸方式）
    //   Indirect：间接触摸 — 如 Apple TV 遥控器的触摸板、鼠标模拟触摸
    //   Stylus：触控笔 — 使用手写笔/触控笔进行输入（如 iPad + Apple Pencil）
    //
    // 💡 用途：
    //   通过 Touch.type 属性判断触摸来源，可以实现不同的交互逻辑。
    //   例如：触控笔可以启用更精细的绘图模式，
    //   而手指触摸使用更粗犷的 UI 交互。
    // ================================================================
    public enum TouchType
    {
        Direct,
        Indirect,
        Stylus
    }

    // ================================================================
    // Touch — 单次触摸事件的完整数据结构
    // ================================================================
    // 每个 Touch 结构体代表一根手指在某一时刻的触摸状态。
    // 通过 Input.GetTouch(index) 或 Input.touches 获取。
    //
    // 【关键字段】
    //   fingerId         — 手指唯一标识（从 0 开始，可能被复用）
    //   position         — 当前触摸位置（屏幕像素坐标，左下角为原点）
    //   rawPosition      — 本帧未经平滑的原始位置
    //   deltaPosition    — 与上一帧的位置差值（像素）
    //   deltaTime        — 距离上次更新的时间间隔
    //   tapCount         — 快速连续点击次数（如双击为 2）
    //   phase            — 当前触摸阶段（TouchPhase）
    //   type             — 触摸物理类型（直接/间接/触控笔）
    //   pressure         — 触摸压力值（0~1，需要设备支持）
    //   maximumPossiblePressure — 设备支持的最大压力值
    //   radius           — 触摸点的椭圆半径
    //   radiusVariance   — 半径的不确定性范围
    //   altitudeAngle    — 触控笔倾斜角（仅触控笔）
    //   azimuthAngle     — 触控笔方位角（仅触控笔）
    //
    // ⚠️ 注意：
    //   - position 是屏幕坐标，不是世界坐标！需用 Camera.ScreenToWorldPoint 转换
    //   - fingerId 可能被系统复用，不要跨帧假设同一个 fingerId 是同一根手指
    //   - 多点触摸时，触摸点的顺序不保证与手指物理顺序一致
    // ================================================================
    {
        private int m_FingerId;
        private Vector2 m_Position;
        private Vector2 m_RawPosition;
        private Vector2 m_PositionDelta;
        private float m_TimeDelta;
        private int m_TapCount;
        private TouchPhase m_Phase;
        private TouchType m_Type;
        private float m_Pressure;
        private float m_maximumPossiblePressure;
        private float m_Radius;
        private float m_RadiusVariance;
        private float m_AltitudeAngle;
        private float m_AzimuthAngle;

        public int fingerId { get { return m_FingerId; } set { m_FingerId = value; } }
        public Vector2 position { get { return m_Position; } set { m_Position = value; }  }
        public Vector2 rawPosition { get { return m_RawPosition; } set { m_RawPosition = value; }  }
        public Vector2 deltaPosition { get { return m_PositionDelta; } set { m_PositionDelta = value; }  }
        public float deltaTime { get { return m_TimeDelta; } set { m_TimeDelta = value; }  }
        public int tapCount { get { return m_TapCount; } set { m_TapCount = value; }  }
        public TouchPhase phase { get { return m_Phase; } set { m_Phase = value; }  }
        public float pressure { get { return m_Pressure; } set { m_Pressure = value; }  }
        public float maximumPossiblePressure { get { return m_maximumPossiblePressure; } set { m_maximumPossiblePressure = value; }  }

        public TouchType type { get { return m_Type; } set { m_Type = value; }  }
        public float altitudeAngle { get { return m_AltitudeAngle; } set { m_AltitudeAngle = value; }  }
        public float azimuthAngle { get { return m_AzimuthAngle; } set { m_AzimuthAngle = value; }  }
        public float radius { get { return m_Radius; } set { m_Radius = value; }  }
        public float radiusVariance { get { return m_RadiusVariance; } set { m_RadiusVariance = value; }  }
    }

    // ================================================================
    // PenStatus — 触控笔状态标志
    // ================================================================
    // 使用位标志（Flags）记录触控笔的多个同时状态。
    // 对应 C++ 端的 PenData::PenStatusEnum。
    //
    // 📌 各标志位：
    //   None    (0x0) — 无接触
    //   Contact (0x1) — 笔尖接触屏幕
    //   Barrel  (0x2) — 笔杆按钮被按下
    //   Inverted(0x4) — 笔被翻转（如用橡皮擦端）
    //   Eraser  (0x8) — 橡皮擦端接触
    //
    // 💡 这些标志可以组合使用，例如 Contact | Barrel 表示"笔尖接触且按住笔杆按钮"。
    //    通过 (penStatus & PenStatus.Barrel) != 0 来检测某个状态。
    // ================================================================
    // Matches PenData::PenStatusEnum in native code
    [Flags]
    public enum PenStatus
    {
        None = 0x0,
        Contact = 0x1,
        Barrel = 0x2,
        Inverted = 0x4,
        Eraser = 0x8,
    }

    // ================================================================
    // PenEventType — 触控笔接触事件类型
    // ================================================================
    // 描述触控笔与屏幕的接触状态变化。
    //
    // 📌 事件类型：
    //   NoContact — 笔未接触屏幕
    //   PenDown   — 笔刚接触屏幕（按下）
    //   PenUp     — 笔刚离开屏幕（抬起）
    //
    // 💡 与鼠标事件类似：PenDown ≈ MouseDown，PenUp ≈ MouseUp。
    //    通过 Input.GetPenEvent(index) 获取具体的 PenData。
    // ================================================================
    public enum PenEventType
    {
        NoContact,
        PenDown,
        PenUp
    }

    // ================================================================
    // PenData — 触控笔数据结构
    // ================================================================
    // 存储触控笔的完整输入状态，包括位置、倾斜角度、压力等。
    // 通过 Input.GetPenEvent(index) 或 Input.GetLastPenContactEvent() 获取。
    //
    // 📌 字段说明：
    //   position     — 笔尖在屏幕上的位置（像素坐标）
    //   tilt         — 笔的倾斜方向（X/Y 分量表示倾斜角度）
    //   penStatus    — 触控笔状态标志（接触/按钮/橡皮擦等）
    //   twist        — 笔的扭转角度（绕笔轴旋转）
    //   pressure     — 笔尖压力（0~1）
    //   contactType  — 接触事件类型（PenDown/PenUp/NoContact）
    //   deltaPos     — 与上一帧的位置差值
    //
    // 💡 用途场景：
    //   - 绘图应用：利用 position + pressure + tilt 实现压感绘图
    //   - 手写识别：利用 position + tilt + twist 进行笔迹分析
    //   - 橡皮擦切换：检测 PenStatus.Eraser 标志
    // ================================================================
    public struct PenData
    {
        public Vector2 position;
        public Vector2 tilt;
        public PenStatus penStatus;
        public float twist;
        public float pressure;
        public PenEventType contactType;
        public Vector2 deltaPos;
    }

    // ================================================================
    // DeviceOrientation — 设备物理朝向
    // ================================================================
    // 描述移动设备（手机/平板）相对于重力方向的物理朝向。
    // 通过 Input.deviceOrientation 属性读取。
    //
    // 📌 各朝向值：
    //   Unknown            (0) — 未知朝向（通常在平放时）
    //   Portrait           (1) — 竖屏，Home 键在底部
    //   PortraitUpsideDown (2) — 竖屏倒置，Home 键在顶部
    //   LandscapeLeft      (3) — 横屏，Home 键在右侧
    //   LandscapeRight     (4) — 横屏，Home 键在左侧
    //   FaceUp             (5) — 屏幕朝上（平放）
    //   FaceDown           (6) — 屏幕朝下（倒扣）
    //
    // ⚠️ 注意：
    //   - 这是设备的物理朝向，不是应用的渲染朝向
    //   - 某些设备可能不会报告所有朝向（取决于硬件传感器配置）
    //   - 面朝上/面朝下只在设备有陀螺仪时才可靠
    //   - 与 Screen.orientation（应用锁定的渲染方向）是不同的概念
    // ================================================================
    public enum DeviceOrientation
    {
        Unknown = 0,
        Portrait = 1,
        PortraitUpsideDown = 2,
        LandscapeLeft = 3,
        LandscapeRight = 4,
        FaceUp = 5,
        FaceDown = 6
    }

    // ================================================================
    // AccelerationEvent — 加速度传感器事件
    // ================================================================
    // 存储设备加速度计在某一时间片内的采样数据。
    // 通过 Input.GetAccelerationEvent(index) 或 Input.accelerationEvents 获取。
    //
    // 📌 字段说明：
    //   acceleration — 三轴加速度向量（X/Y/Z，单位 m/s²）
    //                  包含重力加速度（约 9.81 m/s²）
    //   deltaTime    — 该采样与上一采样的时间间隔
    //
    // 💡 典型用途：
    //   - 体感游戏：读取 acceleration 实现倾斜控制
    //   - 摇一摇检测：检测加速度突变
    //   - 屏幕朝向辅助：结合 deviceOrientation 判断设备姿态
    //
    // ⚠️ 注意：
    //   - acceleration 值包含重力分量，静止时约 (0, 9.81, 0)
    //   - 如果只需要用户运动产生的加速度，使用 gyro.userAcceleration
    //   - accelerateEventCount 可能为 0（取决于帧率和传感器采样率）
    // ================================================================
    public struct AccelerationEvent
    {
        internal float x, y, z;
        internal float m_TimeDelta;

        public Vector3 acceleration { get { return new Vector3(x, y, z); } }
        public float deltaTime { get { return m_TimeDelta; } }
    }

    // ================================================================
    // Gyroscope — 陀螺仪数据读取类
    // ================================================================
    // 封装设备陀螺仪的传感器数据，通过 Input.gyro 属性获取实例。
    // 底层调用 C++ 的 GetInput.h 中的传感器接口。
    //
    // 【关键属性】
    //   rotationRate        — 角速度（包含漂移误差）
    //   rotationRateUnbiased — 去漂移后的角速度（更准确但可能有延迟）
    //   gravity             — 重力方向向量（约 9.81 m/s²）
    //   userAcceleration    — 用户运动产生的加速度（不含重力）
    //   attitude            — 设备姿态四元数（旋转方向）
    //   enabled             — 启用/禁用陀螺仪
    //   updateInterval      — 更新频率（秒），越小越频繁
    //
    // 💡 使用示例：
    //   Input.gyro.enabled = true;
    //   Input.gyro.updateInterval = 0.02f; // 50Hz
    //   Vector3 rot = Input.gyro.rotationRate;
    //
    // ⚠️ 注意：
    //   - rotationRate 会随时间累积漂移误差，长时间使用需校准
    //   - rotationRateUnbiased 使用了传感器融合算法消除漂移
    //   - attitude 返回的是设备坐标系到世界坐标系的旋转
    //   - 不是所有设备都支持陀螺仪，使用 SystemInfo.supportsGyroscope 检查
    //
    // 📍 对应 C++ 头文件：Runtime/Input/GetInput.h
    // ================================================================
    [NativeHeader("Runtime/Input/GetInput.h")]
    public class Gyroscope
    {
        internal Gyroscope(int index)
        {
            m_GyroIndex = index;
        }

        private int m_GyroIndex;

        [FreeFunction("GetGyroRotationRate")]
        extern private static Vector3 rotationRate_Internal(int idx);
        [FreeFunction("GetGyroRotationRateUnbiased")]
        extern private static Vector3 rotationRateUnbiased_Internal(int idx);
        [FreeFunction("GetGravity")]
        extern private static Vector3 gravity_Internal(int idx);
        [FreeFunction("GetUserAcceleration")]
        extern private static Vector3 userAcceleration_Internal(int idx);
        [FreeFunction("GetAttitude")]
        extern private static Quaternion attitude_Internal(int idx);
        [FreeFunction("IsGyroEnabled")]
        extern private static bool getEnabled_Internal(int idx);
        [FreeFunction("SetGyroEnabled")]
        extern private static void setEnabled_Internal(int idx, bool enabled);
        [FreeFunction("GetGyroUpdateInterval")]
        extern private static float getUpdateInterval_Internal(int idx);
        [FreeFunction("SetGyroUpdateInterval")]
        extern private static void setUpdateInterval_Internal(int idx, float interval);

        public Vector3 rotationRate { get { return rotationRate_Internal(m_GyroIndex); } }
        public Vector3 rotationRateUnbiased { get { return rotationRateUnbiased_Internal(m_GyroIndex); } }
        public Vector3 gravity { get { return gravity_Internal(m_GyroIndex); } }
        public Vector3 userAcceleration { get { return userAcceleration_Internal(m_GyroIndex); } }
        public Quaternion attitude { get { return attitude_Internal(m_GyroIndex); } }
        public bool enabled { get { return getEnabled_Internal(m_GyroIndex); } set { setEnabled_Internal(m_GyroIndex, value); } }
        public float updateInterval { get { return getUpdateInterval_Internal(m_GyroIndex); } set { setUpdateInterval_Internal(m_GyroIndex, value); } }
    }

    // ================================================================
    // LocationInfo — GPS 定位信息
    // ================================================================
    // 存储设备的地理定位数据，通过 Input.location.lastData 获取。
    //
    // 📌 字段说明：
    //   latitude          — 纬度（度，南纬为负）
    //   longitude         — 经度（度，西经为负）
    //   altitude          — 海拔高度（米）
    //   horizontalAccuracy — 水平精度（米，越小越精确）
    //   verticalAccuracy  — 垂直精度（米）
    //   timestamp         — 定位时间戳（Unix 时间）
    //
    // 💡 用途：
    //   - AR 游戏中的地理位置定位（如 Pokémon GO 风格）
    //   - 基于位置的服务（LBS）
    //   - 运动轨迹记录
    //
    // ⚠️ 注意：
    //   - 需要先调用 Input.location.Start() 启动定位服务
    //   - horizontalAccuracy 越大表示精度越低，建议设置阈值过滤
    //   - 首次定位可能需要较长时间（冷启动）
    //   - 在编辑器中测试时，定位数据可能不准确
    // ================================================================
    public struct LocationInfo
    {
        internal double m_Timestamp;
        internal float m_Latitude;
        internal float m_Longitude;
        internal float m_Altitude;
        internal float m_HorizontalAccuracy;
        internal float m_VerticalAccuracy;

        public float latitude { get { return m_Latitude; } }
        public float longitude { get { return m_Longitude; } }
        public float altitude { get { return m_Altitude; } }
        public float horizontalAccuracy { get { return m_HorizontalAccuracy; } }
        public float verticalAccuracy { get { return m_VerticalAccuracy; } }
        public double timestamp { get { return m_Timestamp; } }
    }

    // ================================================================
    // LocationServiceStatus — 定位服务状态
    // ================================================================
    // 描述 GPS 定位服务的当前运行状态。
    //
    // 📌 状态说明：
    //   Stopped      (0) — 定位服务未启动或已停止
    //   Initializing (1) — 正在初始化（等待首次定位）
    //   Running      (2) — 定位服务正常运行中（可以读取数据）
    //   Failed       (3) — 定位服务启动失败（如用户拒绝权限）
    //
    // 💡 正确的使用流程：
    //   Input.location.Start(accuracy, distance);
    //   if (Input.location.status == LocationServiceStatus.Initializing)
    //       // 等待...
    //   if (Input.location.status == LocationServiceStatus.Running)
    //       var data = Input.location.lastData;
    //   if (Input.location.status == LocationServiceStatus.Failed)
    //       // 权限被拒绝或硬件不支持
    // ================================================================
    public enum LocationServiceStatus
    {
        Stopped = 0,
        Initializing = 1,
        Running = 2,
        Failed = 3
    }

    // ================================================================
    // LocationService — GPS 定位服务
    // ================================================================
    // 提供设备地理位置的启动、停止和数据读取功能。
    // 通过 Input.location 静态属性获取单例实例。
    //
    // 【使用流程】
    //   1. Input.location.Start(accuracy, distance) — 启动定位
    //   2. 等待 status 变为 Running
    //   3. 通过 lastData 读取定位数据
    //   4. Input.location.Stop() — 停止定位（节省电量）
    //
    // 【参数说明】
    //   desiredAccuracyInMeters — 期望精度（米），越小越耗电
    //   updateDistanceInMeters  — 最小更新距离（米），移动超过此距离才更新
    //
    // 💡 性能提示：
    //   - 精度设置为 10m 通常够用，设置为 1m 会显著增加耗电
    //   - 不使用时务必调用 Stop() 停止定位
    //   - 定位权限需要在 AndroidManifest.xml / Info.plist 中声明
    //
    // ⚠️ 注意：
    //   - 编辑器中运行时可能返回模拟数据或无法定位
    //   - 首次使用需用户授权定位权限
    //   - 室内定位精度可能很差（GPS 信号被遮挡）
    // ================================================================
    [NativeHeader("Runtime/Input/LocationService.h")]
    [NativeHeader("Runtime/Input/InputBindings.h")]
    public class LocationService
    {
        internal struct HeadingInfo
        {
            public float magneticHeading;
            public float trueHeading;
            public float headingAccuracy;
            public Vector3 raw;
            public double timestamp;
        }

        [FreeFunction("LocationService::IsServiceEnabledByUser")]
        internal extern static bool IsServiceEnabledByUser();
        [FreeFunction("LocationService::GetLocationStatus")]
        internal extern static LocationServiceStatus GetLocationStatus();
        [FreeFunction("LocationService::GetLastLocation")]
        internal extern static LocationInfo GetLastLocation();
        [FreeFunction("LocationService::SetDesiredAccuracy")]
        internal extern static void SetDesiredAccuracy(float value);
        [FreeFunction("LocationService::SetDistanceFilter")]
        internal extern static void SetDistanceFilter(float value);
        [FreeFunction("LocationService::StartUpdatingLocation")]
        internal extern static void StartUpdatingLocation();
        [FreeFunction("LocationService::StopUpdatingLocation")]
        internal extern static void StopUpdatingLocation();
        [FreeFunction("LocationService::GetLastHeading")]
        internal extern static HeadingInfo GetLastHeading();
        [FreeFunction("LocationService::IsHeadingUpdatesEnabled")]
        internal extern static bool IsHeadingUpdatesEnabled();
        [FreeFunction("LocationService::SetHeadingUpdatesEnabled")]
        internal extern static void SetHeadingUpdatesEnabled(bool value);

        public bool isEnabledByUser { get { return IsServiceEnabledByUser(); } }
        public LocationServiceStatus status { get { return GetLocationStatus(); } }
        public LocationInfo lastData
        {
            get
            {
                if (status != LocationServiceStatus.Running)
                    Debug.Log("Location service updates are not enabled. Check LocationService.status before querying last location.");

                return GetLastLocation();
            }
        }

        public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            SetDesiredAccuracy(desiredAccuracyInMeters);
            SetDistanceFilter(updateDistanceInMeters);
            StartUpdatingLocation();
        }

        public void Start(float desiredAccuracyInMeters)
        {
            Start(desiredAccuracyInMeters, 10f);
        }

        public void Start()
        {
            Start(10f, 10f);
        }

        public void Stop()
        {
            StopUpdatingLocation();
        }
    }

    // ================================================================
    // Compass — 电子罗盘（磁力计）
    // ================================================================
    // 提供设备的磁北方向数据，通过 Input.compass 静态属性获取。
    // 底层复用 LocationService 的 HeadingInfo 数据。
    //
    // 【关键属性】
    //   magneticHeading — 磁北方向角度（0~360°，正北为 0°）
    //   trueHeading     — 真北方向角度（已校正磁偏角）
    //   headingAccuracy — 方向精度（度，越小越精确）
    //   rawVector       — 原始磁场向量（XYZ 三轴）
    //   timestamp       — 数据时间戳
    //   enabled         — 启用/禁用罗盘更新
    //
    // 💡 典型用途：
    //   - AR 游戏中的方向指引（如指南针 UI）
    //   - 导航应用的方向指示
    //   - 结合 GPS 实现朝向感知的 AR 内容
    //
    // ⚠️ 注意：
    //   - 磁北 ≠ 真北，两者之间存在磁偏角（因地区而异）
    //   - 附近有金属物体或电子设备时，磁力计读数可能不准
    //   - 建议在使用前调用 Input.compass.enabled = true 启用更新
    // ================================================================
    public class Compass
    {
        public float magneticHeading
        {
            get { return LocationService.GetLastHeading().magneticHeading; }
        }
        public float trueHeading
        {
            get { return LocationService.GetLastHeading().trueHeading; }
        }
        public float headingAccuracy
        {
            get { return LocationService.GetLastHeading().headingAccuracy; }
        }
        public Vector3 rawVector
        {
            get { return LocationService.GetLastHeading().raw; }
        }
        public double timestamp
        {
            get { return LocationService.GetLastHeading().timestamp; }
        }
        public bool enabled
        {
            get { return LocationService.IsHeadingUpdatesEnabled(); }
            set { LocationService.SetHeadingUpdatesEnabled(value); }
        }
    }

    // Burst-compatible unmanaged string calls can not be in UnityEngine namespace (UnityEngine.Internal is okay)
    namespace Internal
    {
        // ================================================================
        // InputUnsafeUtility — Burst Compiler 兼容的输入查询工具
        // ================================================================
        // 位于 UnityEngine.Internal 命名空间，提供 unsafe 版本的输入查询方法。
        //
        // 【为什么需要这个类？】
        //   Input 类的标准方法接收 string 参数（如 GetKey("space")），
        //   但 Burst Compiler 不支持托管字符串（managed string）的跨边界传递。
        //   因此为每个方法提供了 __Unmanaged 变体：
        //     - 接收 byte* + int 长度（非托管内存中的字符串）
        //     - 标记 [RequiredMember] 防止被链接器裁剪
        //     - 仅被 Burst 生成的代码引用
        //
        // 【与 Input 类的关系】
        //   Input 类的静态方法大多直接转发到此工具类：
        //     Input.GetAxis("Horizontal") → InputUnsafeUtility.GetAxis("Horizontal")
        //
        // ⚠️ 这是内部实现类，不要在用户代码中直接调用！
        //    请使用 Input 类的公共 API。
        // ================================================================
        [NativeHeader("Runtime/Input/InputBindings.h")]
        internal static class InputUnsafeUtility
        {
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetKeyString(string name);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe bool GetKeyString__Unmanaged(byte* name, int nameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetKeyUpString(string name);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe bool GetKeyUpString__Unmanaged(byte* name, int nameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetKeyDownString(string name);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe bool GetKeyDownString__Unmanaged(byte* name, int nameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static float GetAxis(string axisName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]
            internal extern static unsafe float GetAxis__Unmanaged(byte* axisName, int axisNameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static float GetAxisRaw(string axisName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe float GetAxisRaw__Unmanaged(byte* axisName, int axisNameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetButton(string buttonName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe bool GetButton__Unmanaged(byte* buttonName, int buttonNameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetButtonDown(string buttonName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe byte GetButtonDown__Unmanaged(byte* buttonName, int buttonNameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetButtonUp(string buttonName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]
            internal extern static unsafe bool GetButtonUp__Unmanaged(byte* buttonName, int buttonNameLen);
            internal extern static bool IsJoystickPreconfigured(string joystickName);
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]
            internal extern static unsafe bool IsJoystickPreconfigured__Unmanaged(byte* joystickName, int joystickNameLen);
        }
    }

    // ================================================================
    // Input — Unity 传统输入系统主入口（静态 API）
    // ================================================================
    //
    // 【类概述】
    // Input 是 Unity 最经典的输入 API，所有方法和属性都是静态的。
    // 它在 Update() 中被轮询调用，读取当前帧的输入状态。
    //
    // 【方法分组】
    //   虚拟轴查询：GetAxis / GetAxisRaw
    //   虚拟按钮查询：GetButton / GetButtonDown / GetButtonUp
    //   物理按键查询：GetKey / GetKeyDown / GetKeyUp（KeyCode 枚举）
    //   字符串按键查询：GetKey(string) / GetKeyDown(string)
    //   鼠标查询：mousePosition / GetMouseButton 系列
    //   触摸查询：GetTouch / touches / touchCount
    //   触控笔查询：GetPenEvent / penEventCount
    //   加速度计：acceleration / accelerationEvents
    //   陀螺仪：gyro（返回 Gyroscope 实例）
    //   定位服务：location（返回 LocationService 实例）
    //   电子罗盘：compass（返回 Compass 实例）
    //   IME 输入法：imeCompositionMode / compositionString
    //   设备信息：mousePresent / touchSupported / deviceOrientation
    //
    // 【底层实现】
    //   本类的大部分方法转发到 InputUnsafeUtility（Burst 兼容层），
    //   后者通过 P/Invoke 调用 C++ 端的 InputBindings.cpp，
    //   最终由各平台的 Platform Abstraction Layer (PAL) 采集硬件输入。
    //
    // 【与 KeyCode 枚举的关系】
    //   GetKey(KeyCode) 使用(KeyCode)枚举值直接查询物理按键状态，
    //   不经过 InputManager 的虚拟轴配置。
    //   而 GetAxis("Horizontal") 读取的是 InputManager 中定义的虚拟轴，
    //   可以映射到多个物理按键/手柄轴。
    //
    // 【平台差异提示】
    //   - mousePosition 在 WebGL 中可能不准确（浏览器安全策略）
    //   - touchCount 在 PC 编辑器中始终为 0（除非启用模拟触摸）
    //   - joystickNames 在某些平台可能返回空数组
    //   - backButtonLeavesApp 仅在 Android 上有效
    //
    // 📍 对应 C++ 头文件：Runtime/Input/InputBindings.h
    // ================================================================
    [NativeHeader("Runtime/Input/InputBindings.h")]
    public partial class Input
    {
        // ================================================================
        // 🎯 虚拟轴查询 — GetAxis / GetAxisRaw
        // ================================================================
        // 根据 InputManager.asset 中定义的虚拟轴名称获取轴值。
        //
        // GetAxis(name)  → 返回经过平滑处理的值（-1.0 ~ 1.0）
        //   - 内部使用 Gravity（回零速度）和 Sensitivity（响应速度）进行插值
        //   - 松手后值不会立即归零，而是逐渐衰减（由 Gravity 控制）
        //   - 按下时值不会立即到达极值，而是逐渐加速（由 Sensitivity 控制）
        //   - 适合角色移动、相机控制等需要手感平滑的场景
        //
        // GetAxisRaw(name) → 返回未经平滑的原始值（-1、0、1 三档）
        //   - 直接映射物理输入状态，无插值处理
        //   - 适合需要精确响应的场景（如格斗游戏的方向判定）
        //
        // 💡 常见虚拟轴名称：
        //   "Horizontal" — 左右方向（A/D 或 ←/→ 或手柄左摇杆 X）
        //   "Vertical"   — 上下方向（W/S 或 ↑/↓ 或手柄左摇杆 Y）
        //   "Fire1"      — 开火按钮（左 Ctrl 或鼠标左键或手柄按钮 0）
        //   "Jump"       — 跳跃（空格键或手柄按钮 1）
        //
        // ⚠️ 轴名称区分大小写，必须与 InputManager.asset 中的配置完全一致
        // ================================================================
        public static float GetAxis(string axisName) => Internal.InputUnsafeUtility.GetAxis(axisName);
        public static float GetAxisRaw(string axisName) => Internal.InputUnsafeUtility.GetAxisRaw(axisName);

        // ================================================================
        // 🎯 虚拟按钮查询 — GetButton / GetButtonDown / GetButtonUp
        // ================================================================
        // 根据 InputManager.asset 中定义的虚拟按钮名称查询按钮状态。
        //
        // GetButton(name)      → 按钮是否被持续按住（整个按住期间返回 true）
        // GetButtonDown(name)  → 按钮是否在本帧被按下（仅按下瞬间返回 true）
        // GetButtonUp(name)    → 按钮是否在本帧被抬起（仅抬起瞬间返回 true）
        //
        // 💡 典型用法：
        //   GetButton("Fire1")    → 持续射击（如机枪扫射）
        //   GetButtonDown("Fire1") → 单次射击（如狙击枪）
        //   GetButtonUp("Jump")   → 松开跳跃键（如蓄力跳跃的松手时刻）
        //
        // ⚠️ 注意：
        //   - 按钮名称必须与 InputManager.asset 中的配置匹配
        //   - GetButton 在按钮被按住的每一帧都返回 true
        //   - 如果只用 GetButton 做射击，会导致每帧都发射，需要自己加冷却
        // ================================================================
        public static bool GetButton(string buttonName) => Internal.InputUnsafeUtility.GetButton(buttonName);
        public static bool GetButtonDown(string buttonName) => Internal.InputUnsafeUtility.GetButtonDown(buttonName);
        public static bool GetButtonUp(string buttonName) => Internal.InputUnsafeUtility.GetButtonUp(buttonName);

        // ================================================================
        // ⚡ 物理按键查询（公开方法）— GetKey / GetKeyDown / GetKeyUp
        // ================================================================
        // 直接查询物理按键状态，不经过 InputManager 虚拟轴配置。
        //
        // 📌 两种重载：
        //   GetKey(KeyCode key)     — 使用 KeyCode 枚举（如 KeyCode.Space）
        //   GetKey(string name)     — 使用字符串名称（如 "space"，不区分大小写）
        //
        // 返回值含义：
        //   GetKey     → 按键是否被持续按住
        //   GetKeyDown → 按键是否在本帧被按下（仅按下瞬间）
        //   GetKeyUp   → 按键是否在本帧被抬起（仅抬起瞬间）
        //
        // 💡 使用建议：
        //   - KeyCode 枚举版本性能略好（避免字符串查找）
        //   - 字符串版本更灵活（可以从配置文件读取按键名称）
        //   - 两者底层都调用 C++ 的 GetKeyInt / GetKeyString 方法
        //
        // ⚠️ 注意：
        //   - GetKeyDown 在快速连续按键时可能跳帧（取决于帧率和按键速度）
        //   - 在 OnGUI() 中不要使用这些方法，应使用 Event.current
        // ================================================================

        public static bool GetKey(KeyCode key) => GetKeyInt(key);
        public static bool GetKey(string name) => Internal.InputUnsafeUtility.GetKeyString(name);
        public static bool GetKeyUp(KeyCode key) => GetKeyUpInt(key);
        public static bool GetKeyUp(string name) => Internal.InputUnsafeUtility.GetKeyUpString(name);
        public static bool GetKeyDown(KeyCode key) => GetKeyDownInt(key);
        public static bool GetKeyDown(string name) => Internal.InputUnsafeUtility.GetKeyDownString(name);

        // ================================================================
        // 🖱️ 编辑器触摸模拟 — 仅在 UNITY_EDITOR 中生效
        // ================================================================
        // SimulateTouch 方法允许在编辑器中模拟触摸事件，
        // 用于测试触摸逻辑而无需真机。标记了 [Conditional("UNITY_EDITOR")]，
        // 在发布版本中调用会被编译器完全移除。
        // ================================================================
        [Conditional("UNITY_EDITOR")]
        internal static void SimulateTouch(Touch touch)
        {
            SimulateTouchInternal(touch, DateTime.Now.Ticks);
        }

        [Conditional("UNITY_EDITOR")]
        [NativeConditional("UNITY_EDITOR")]
        [FreeFunction("SimulateTouch")]
        private extern static void SimulateTouchInternal(Touch touch, long timestamp);

        // ================================================================
        // 🖱️ 鼠标输入查询 — mousePosition / GetMouseButton 系列
        // ================================================================
        // 提供鼠标位置、移动、滚轮和按钮状态的查询。
        //
        // 【位置查询】
        //   mousePosition      — 鼠标在屏幕上的像素坐标（Vector3，z 始终为 0）
        //   mousePositionDelta — 鼠标与上一帧的位置差值（像素）
        //   mouseScrollDelta   — 鼠标滚轮的滚动量（Vector2，y 为垂直滚动）
        //
        // 【按钮查询】
        //   GetMouseButton(int button)      — 鼠标按钮是否持续按住
        //   GetMouseButtonDown(int button)  — 鼠标按钮是否在本帧被按下
        //   GetMouseButtonUp(int button)    — 鼠标按钮是否在本帧被抬起
        //
        // 按钮编号：
        //   0 = 左键（Mouse0）
        //   1 = 右键（Mouse1）
        //   2 = 中键（Mouse2）
        //   3 = 赢家键（Mouse3）
        //   4 = 赢家键（Mouse4）
        //
        // ⚠️ 注意：
        //   - mousePosition 是屏幕坐标，不是世界坐标！
        //   - 转换到世界坐标：Camera.main.ScreenToWorldPoint(Input.mousePosition)
        //   - WebGL 中 mousePosition 可能不准确（浏览器安全限制）
        //   - simulateMouseWithTouches 可控制触摸是否模拟鼠标事件
        // ================================================================
        public extern static bool simulateMouseWithTouches { get; set; }

        // ================================================================
        // 📌 全局输入状态属性
        // ================================================================
        //   anyKey     — 是否有任何按键/按钮/触摸在本帧被按住
        //   anyKeyDown — 是否有任何按键/按钮/触摸在本帧刚被按下
        //   inputString — 本帧输入的字符序列（用于文本输入检测）
        //
        // 💡 inputString 包含本帧所有按键产生的字符：
        //   - 按下 'A' 键 → inputString 包含 "a"（或 "A" 如果 Shift 按住）
        //   - 按下空格 → inputString 包含 " "
        //   - 方向键不会产生字符
        //
        // ⚠️ anyKey 不区分具体按键，只判断"有没有任何输入"
        //    适合用来检测"玩家是否开始操作"
        // ================================================================
        [NativeMethod(ThrowsException = true)]
        public extern static bool anyKey { get; }
        [NativeMethod(ThrowsException = true)]
        public extern static bool anyKeyDown { get; }
        [NativeMethod(ThrowsException = true)]
        public extern static string inputString { get; }
        [NativeMethod(ThrowsException = true)]
        public extern static Vector3 mousePosition { get; }
        [NativeMethod(ThrowsException = true)]
        public extern static Vector3 mousePositionDelta { get; }
        [NativeMethod(ThrowsException = true)]
        public extern static Vector2 mouseScrollDelta { get; }

        // ================================================================
        // 📝 IME 输入法控制
        // ================================================================
        // 控制中文/日文/韩文等输入法（IME）的行为。
        //
        // imeCompositionMode   — 设置 IME 的启用模式（Auto/On/Off）
        // compositionString    — 当前正在组合的字符串（用户输入但未确认的文字）
        // imeIsSelected        — IME 是否处于激活状态
        // compositionCursorPos — IME 候选框的显示位置（屏幕坐标）
        //
        // 💡 中文输入法工作流：
        //   用户按下按键 → IME 弹出候选窗口 → compositionString 显示正在组合的文字
        //   → 用户选择确认 → 最终文字通过 inputString 或 OnGUI 事件返回
        //
        // ⚠️ 大多数情况下不需要手动设置 imeCompositionMode（Auto 模式足够）
        //    只有在需要自定义输入法行为时才需要修改
        // ================================================================
        public extern static IMECompositionMode imeCompositionMode { get; set; }
        public extern static string compositionString { get; }
        public extern static bool imeIsSelected { get; }
        public extern static Vector2 compositionCursorPos { get; set; }
        [Obsolete("eatKeyPressOnTextFieldFocus property is deprecated, and only provided to support legacy behavior.")]
        public extern static bool eatKeyPressOnTextFieldFocus { get; set; }

        [AutoStaticsCleanupOnCodeReload]
        internal static bool simulateTouchEnabled { get; set; }

        // ================================================================
        // 📱 设备能力检测
        // ================================================================
        // 查询当前设备的输入能力，用于适配不同平台。
        //
        // mousePresent   — 是否有鼠标设备（编辑器中模拟触摸时为 false）
        // touchSupported — 是否支持触摸输入（模拟触摸时始终为 true）
        //
        // 💡 典型用法：根据设备类型切换 UI 交互方式
        //   if (Input.touchSupported)
        //       // 显示触摸虚拟摇杆
        //   if (Input.mousePresent)
        //       // 显示鼠标光标
        // ================================================================

        [FreeFunction("IsTouchSupported")]
        private extern static bool GetTouchSupportedInternal();

        public static bool mousePresent => !simulateTouchEnabled && GetMousePresentInternal();
        public static bool touchSupported => simulateTouchEnabled || GetTouchSupportedInternal();

        // ================================================================
        // ✏️ 触控笔和触摸查询属性
        // ================================================================
        // penEventCount        — 当前帧的触控笔事件数量
        // touchCount           — 当前帧的触摸点数量（活跃的手指数）
        // touchPressureSupported — 设备是否支持触摸压力感应
        // stylusTouchSupported   — 设备是否支持触控笔
        // multiTouchEnabled      — 是否启用多点触摸（可读写）
        //
        // 💡 典型触摸查询循环：
        //   for (int i = 0; i < Input.touchCount; i++)
        //   {
        //       Touch touch = Input.GetTouch(i);
        //       if (touch.phase == TouchPhase.Began)
        //           // 处理新触摸
        //   }
        //
        // ⚠️ 注意：
        //   - touchCount 和 GetTouch(i) 必须在同一帧内使用
        //   - 不要在循环中调用 Input.touches（每次都创建新数组，GC 压力大）
        //   - multiTouchEnabled 默认为 true，某些情况下可设为 false 禁用多点触摸
        // ================================================================

        public extern static int penEventCount
        {
            [FreeFunction("GetPenEventCount")]
            get;
        }

        public extern static int touchCount
        {
            [FreeFunction("GetTouchCount")]
            get;
        }
        public extern static bool touchPressureSupported
        {
            [FreeFunction("IsTouchPressureSupported")]
            get;
        }
        public extern static bool stylusTouchSupported
        {
            [FreeFunction("IsStylusTouchSupported")]
            get;
        }
        public extern static bool multiTouchEnabled
        {
            [FreeFunction("IsMultiTouchEnabled")]
            get;
            [FreeFunction("SetMultiTouchEnabled")]
            set;
        }
        // ================================================================
        // 📡 传感器和设备状态
        // ================================================================
        // isGyroAvailable      — [已废弃] 陀螺仪是否可用，请用 SystemInfo.supportsGyroscope
        // deviceOrientation    — 设备物理朝向（DeviceOrientation 枚举）
        // acceleration         — 设备加速度计读数（包含重力分量）
        // compensateSensors    — 是否启用传感器补偿（自动校正重力方向）
        // accelerationEventCount — 本帧的加速度事件数量
        // backButtonLeavesApp  — Android 返回按钮是否退出应用（而非返回上一级）
        //
        // 💡 加速度计读数说明：
        //   - 静止时 acceleration ≈ (0, 9.81, 0)（重力方向沿 Y 轴）
        //   - 设备水平放置时 acceleration ≈ (0, 0, 9.81)（重力沿 Z 轴）
        //   - 具体值取决于设备的默认朝向和 compensateSensors 设置
        //
        // ⚠️ 注意：
        //   - isGyroAvailable 已废弃，请使用 SystemInfo.supportsGyroscope
        //   - backButtonLeavesApp 仅在 Android 平台有效
        //   - 加速度计数据可能有噪声，建议做低通滤波处理
        // ================================================================
        [Obsolete("isGyroAvailable property is deprecated. Please use SystemInfo.supportsGyroscope instead.")]
        public extern static bool isGyroAvailable
        {
            [FreeFunction("IsGyroAvailable")]
            get;
        }
        public extern static DeviceOrientation deviceOrientation
        {
            [FreeFunction("GetDeviceOrientation")]
            get;
        }
        public extern static Vector3 acceleration
        {
            [FreeFunction("GetAcceleration")]
            get;
        }
        public extern static bool compensateSensors
        {
            [FreeFunction("IsCompensatingSensors")]
            get;
            [FreeFunction("SetCompensatingSensors")]
            set;
        }
        public extern static int accelerationEventCount
        {
            [FreeFunction("GetAccelerationCount")]
            get;
        }
        public extern static bool backButtonLeavesApp
        {
            [FreeFunction("GetBackButtonLeavesApp")]
            get;
            [FreeFunction("SetBackButtonLeavesApp")]
            set;
        }

        // ================================================================
        // 🌍 定位服务、罗盘、陀螺仪 — 惰性单例访问器
        // ================================================================
        // 这三个属性使用惰性初始化模式，首次访问时创建单例实例。
        // 标记了 [AutoStaticsCleanupOnCodeReload]，
        // 在脚本热重载（域重载）时会自动清理并重新初始化。
        //
        // Input.location → LocationService 实例（GPS 定位）
        // Input.compass   → Compass 实例（电子罗盘）
        // Input.gyro     → Gyroscope 实例（陀螺仪）
        //
        // 💡 使用前需要先启用对应服务：
        //   Input.location.Start();
        //   Input.compass.enabled = true;
        //   Input.gyro.enabled = true;
        // ================================================================
        public static LocationService location
        {
            get
            {
                if (locationServiceInstance == null)
                    locationServiceInstance = new LocationService();
                return locationServiceInstance;
            }
        }
        [AutoStaticsCleanupOnCodeReload]
        private static Compass compassInstance;
        public static Compass compass
        {
            get
            {
                if (compassInstance == null)
                    compassInstance = new Compass();
                return compassInstance;
            }
        }
        [FreeFunction("GetGyro")]
        private extern static int GetGyroInternal();
        [AutoStaticsCleanupOnCodeReload]
        private static Gyroscope s_MainGyro;
        public static Gyroscope gyro
        {
            get
            {
                if (s_MainGyro == null)
                    s_MainGyro = new Gyroscope(GetGyroInternal());
                return s_MainGyro;
            }
        }

        // ================================================================
        // 📋 集合属性 — touches / accelerationEvents
        // ================================================================
        // 便捷属性：一次性获取所有触摸/加速度事件的数组。
        //
        // ⚠️ 性能警告：
        //   每次访问都创建新的数组！在 Update() 中频繁使用会产生 GC 分配。
        //   推荐使用更低级的 API：
        //     for (int i = 0; i < Input.touchCount; i++)
        //         Touch t = Input.GetTouch(i);  // 无 GC 分配
        //   而不是：
        //     Touch[] touches = Input.touches;   // 每帧分配新数组！
        //
        // 💡 如果必须使用数组，可以预分配缓存并在循环中填充：
        //   if (Input.touchCount > 0 && cachedTouches.Length < Input.touchCount)
        //       cachedTouches = new Touch[Input.touchCount];
        //   for (int i = 0; i < Input.touchCount; i++)
        //       cachedTouches[i] = Input.GetTouch(i);
        // ================================================================
        public static Touch[] touches
        {
            get
            {
                int count = touchCount;
                Touch[] touches = new Touch[count];
                for (int q = 0; q < count; ++q)
                    touches[q] = GetTouch(q);
                return touches;
            }
        }

        public static AccelerationEvent[] accelerationEvents
        {
            get
            {
                int count = accelerationEventCount;
                AccelerationEvent[] events = new AccelerationEvent[count];
                for (int q = 0; q < count; ++q)
                    events[q] = GetAccelerationEvent(q);
                return events;
            }
        }

        internal extern static bool CheckDisabled();
    }
}
