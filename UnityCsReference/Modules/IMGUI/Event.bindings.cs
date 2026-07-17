// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Event — IMGUI 事件系统（所有用户输入的抽象）
//
// 📌 作用：
//   Event 是 IMGUI 事件系统的核心类。
//   所有用户输入（鼠标、键盘、触摸、拖拽等）
//   都被封装为 Event 对象传递给 OnGUI 方法。
//
// 🔄 IMGUI 事件循环（每帧执行）：
//   Unity 引擎每帧会为每个 GUI 调用触发一系列 EventType：
//
//   1. EventType.Layout   — 布局阶段（计算控件位置和大小）
//   2. EventType.Repaint  — 重绘阶段（实际渲染控件）
//   3. EventType.MouseDown — 鼠标按下
//   4. EventType.MouseUp  — 鼠标释放
//   5. EventType.MouseMove — 鼠标移动
//   6. EventType.KeyDown  — 按键按下
//   7. EventType.KeyUp    — 按键释放
//   8. EventType.ScrollWheel — 滚轮
//   9. EventType.DragUpdated / PerformDrop — 拖放
//   10. EventType.Ignore — 被其他系统消费的事件
//
// 💡 OnGUI 方法签名：
//   void OnGUI() {
//       Event e = Event.current;  // 获取当前事件
//       // 或者 Event e = Event.KeyboardEvent("a");
//   }
//
// ⚡ EventType vs rawType：
//   rawType — 原始事件类型（如 MouseDown）
//   type    — 处理后的事件类型（如 MouseDown 可能变为 Used）
//
// 🔑 ControlID 与事件分发：
//   GetTypeForControl(controlID) — 获取指定控件的事件类型。
//   同一个原始事件对不同控件可能有不同的类型。
//   例如 MouseDown 事件：被点击的控件得到 MouseDown，
//   其他控件得到 EventType.Ignore。
//
// 📍 对应 C++ 头文件：Modules/IMGUI/Event.bindings.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // ==============================================================
    // Event — IMGUI 事件对象（C++ 绑定部分）
    //
    // 🔑 关键属性：
    //   - type/rawType: 事件类型（Mouse/Key/Layout/Repaint 等）
    //   - mousePosition: 鼠标位置（GUI 坐标）
    //   - delta: 鼠标/滚轮增量
    //   - button: 鼠标按钮（0=左, 1=右, 2=中）
    //   - keyCode: 按键码
    //   - character: 输入字符
    //   - modifiers: 修饰键（Shift/Ctrl/Alt）
    //
    // 🔑 关键方法：
    //   - Use(): 标记事件已处理（防止被其他控件处理）
    //   - GetTypeForControl(id): 获取指定控件的事件类型
    //   - PopEvent(): 从事件队列取出事件
    //
    // 💡 StaticAccessor("GUIEvent") — 所有静态方法调用
    //    都路由到 C++ 端的 GUIEvent 单例。
    // ==============================================================
    [NativeHeader("Modules/IMGUI/Event.bindings.h"),
     StaticAccessor("GUIEvent", StaticAccessorType.DoubleColon)]
    partial class Event
    {
        // ==============================================================
        // 鼠标/指针事件属性
        //
        // 🎯 rawType — 原始事件类型（只读）。
        //   与 type 的区别：type 可能被修改（如 Used），
        //   rawType 始终返回原始的事件类型。
        //
        // 🎯 mousePosition — 鼠标在 GUI 坐标系中的位置。
        //   ⚠️ 注意：这是 GUI 坐标，不是屏幕坐标！
        //   左下角原点，与 Screen 坐标可能有偏移。
        //
        // 🎯 delta — 鼠标移动增量或滚轮滚动量。
        //   MouseMove 时 = 鼠标位移
        //   ScrollWheel 时 = 滚轮滚动量（y>0 向下滚）
        //
        // 🎯 pointerType — 指针类型（Mouse/Touch/Pen）。
        //   区分输入设备类型，支持手写板等设备。
        // ==============================================================
        [NativeProperty("type", false, TargetType.Field)] public extern EventType rawType { get; }
        [NativeProperty("mousePosition", false, TargetType.Field)] public extern Vector2 mousePosition { get; set; }
        [NativeProperty("delta", false, TargetType.Field)] public extern Vector2 delta { get; set; }
        [NativeProperty("pointerType", false, TargetType.Field)] public extern PointerType pointerType { get; set; }

        // ==============================================================
        // 鼠标按钮与修饰键
        //
        // 🎯 button — 鼠标按钮编号。
        //   0 = 左键, 1 = 右键, 2 = 中键
        //   对于 MouseDown/MouseUp 事件有意义。
        //
        // 🎯 modifiers — 修饰键组合（位掩码）。
        //   EventModifiers.Shift — Shift 键
        //   EventModifiers.Ctrl  — Ctrl 键 (macOS 上是 Cmd)
        //   EventModifiers.Alt   — Alt/Option 键
        //   EventModifiers.Command — Command 键 (macOS)
        //
        // 💡 判断修饰键：
        //   if ((e.modifiers & EventModifiers.Shift) != 0) { ... }
        // ==============================================================
        [NativeProperty("button", false, TargetType.Field)] public extern int button { get; set; }
        [NativeProperty("modifiers", false, TargetType.Field)] public extern EventModifiers modifiers { get; set; }

        // ==============================================================
        // 手写笔/触摸事件属性
        //
        // 🎯 pressure — 手写笔压力值（0~1）。
        // 🎯 twist — 手写笔扭转角度。
        // 🎯 tilt — 手写笔倾斜角度（2D 向量）。
        // 🎯 penStatus — 手写笔状态（Normal/Barrel/Eraser）。
        // 🎯 clickCount — 点击次数（用于检测双击）。
        //
        // 💡 这些属性让 IMGUI 支持专业手写板设备。
        //    游戏开发中主要用鼠标/触摸，手写笔属性较少使用。
        // ==============================================================
        [NativeProperty("pressure", false, TargetType.Field)] public extern float pressure { get; set; }
        [NativeProperty("twist", false, TargetType.Field)] public extern float twist { get; set; }
        [NativeProperty("tilt", false, TargetType.Field)] public extern Vector2 tilt { get; set; }
        [NativeProperty("penStatus", false, TargetType.Field)] public extern PenStatus penStatus { get; set; }
        [NativeProperty("clickCount", false, TargetType.Field)] public extern int clickCount { get; set; }

        // ==============================================================
        // 键盘事件属性
        //
        // 🎯 character — 输入的字符（适用于文本输入）。
        //   只在 EventType.KeyDown 时有值。
        //   空字符 '\0' 表示非字符按键（如方向键）。
        //
        // 🎯 keyCode — 按键码（KeyCode 枚举）。
        //
        // ⚡ keyCode 智能映射：
        //   - 鼠标事件 → 自动映射为 KeyCode.Mouse0/1/2...
        //   - 滚轮事件 → 根据滚动方向映射为 WheelUp/WheelDown
        //   - 键盘事件 → 直接返回物理按键码
        //
        // 💡 这样的设计让开发者可以用统一的 keyCode 来判断
        //    任何输入设备的按键，而不需要区分鼠标/键盘。
        // ==============================================================
        [NativeProperty("character", false, TargetType.Field)] public extern char character { get; set; }
        [NativeProperty("keycode", false, TargetType.Field)] extern KeyCode Internal_keyCode { get; set; }
        public KeyCode keyCode
        {
            get
            {
                var key = isMouse ? KeyCode.Mouse0 + button : Internal_keyCode;

                if(isScrollWheel)
                    key = delta.y < 0 || delta.y == 0 && delta.x < 0 ? KeyCode.WheelUp : KeyCode.WheelDown;

                return key;
            }
            set => Internal_keyCode = value;
        }

        // ==============================================================
        // 显示器索引
        //
        // 📌 displayIndex — 事件所在的显示器编号。
        //    多显示器环境下，用于区分事件来自哪个屏幕。
        // ==============================================================
        [NativeProperty("displayIndex", false, TargetType.Field)] public extern int displayIndex { get; set; }

        // ==============================================================
        // 事件类型（处理后版本）
        //
        // 🎯 type — 事件类型（可读写）。
        //   与 rawType 不同，type 可以被修改。
        //   当事件被控件处理后，type 会被设为 EventType.Used。
        //
        // 💡 读取时通过 FreeFunction 调用 C++ 端的 Get/Set 方法，
        //    因为 type 属性需要一些额外的处理逻辑。
        // ==============================================================
        public extern EventType type
        {
            [FreeFunction("GUIEvent::GetType", HasExplicitThis = true)] get;
            [FreeFunction("GUIEvent::SetType", HasExplicitThis = true)] set;
        }

        // ==============================================================
        // 命令名称
        //
        // 🎯 commandName — 命令事件的名称字符串。
        //   用于 EventType.ExecuteCommand 事件。
        //   例如 "Copy", "Paste", "SelectAll" 等系统命令。
        //   也用于自定义命令（如编辑器菜单项触发的命令）。
        // ==============================================================
        public extern string commandName
        {
            [FreeFunction("GUIEvent::GetCommandName", HasExplicitThis = true)] get;
            [FreeFunction("GUIEvent::SetCommandName", HasExplicitThis = true)] set;
        }

        // ==============================================================
        // 🎯 Event.Use() — 标记事件已处理（核心方法！）
        //
        // ⚡ 每当一个控件消费了事件后，必须调用 Use()。
        //   这会将事件类型设为 EventType.Used，
        //   防止其他控件重复处理同一个事件。
        //
        // 💡 典型用法：
        //   Event e = Event.current;
        //   if (e.type == EventType.MouseDown) {
        //       // 处理点击...
        //       e.Use(); // 标记已处理
        //   }
        //
        // ⚠️ 如果不调用 Use()，事件会被多个控件处理，
        //    导致重复响应（如按钮被点一次但触发两次）。
        // ==============================================================
        [NativeMethod("Use")]
        private extern void Internal_Use();

        // ==============================================================
        // 原生对象生命周期
        //
        // 📌 Event 对象在 C++ 端维护事件数据。
        //   Internal_Create  — 创建新的 Event（可指定显示器）
        //   Internal_Destroy — 释放 Event 内存
        //   Internal_Copy    — 复制 Event 数据
        //
        // 💡 Event 对象可以复用：
        //   Event.current 每帧被重用（s_MasterEvent），
        //   不需要每帧创建新对象。
        // ==============================================================
        [FreeFunction("GUIEvent::Internal_Create", IsThreadSafe = true)]
        private static extern IntPtr Internal_Create(int displayIndex);

        [FreeFunction("GUIEvent::Internal_Destroy", IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        [FreeFunction("GUIEvent::Internal_Copy", IsThreadSafe = true)]
        private static extern IntPtr Internal_Copy(IntPtr otherPtr);

        // ==============================================================
        // 🎯 GetTypeForControl — 获取指定控件的事件类型
        //
        // ⚡ 这是 IMGUI 事件分发的核心机制：
        //   同一个原始事件（如 MouseDown）对不同控件有不同的含义。
        //
        //   控件 A（被点击的）：GetTypeForControl(idA) → MouseDown
        //   控件 B（未点击的）：GetTypeForControl(idB) → Ignore
        //   控件 C（被禁用的）：GetTypeForControl(idC) → None
        //
        // 💡 在 GUI 控件内部实现中使用：
        //   int id = GUIUtility.GetControlID(...);
        //   EventType e = Event.current.GetTypeForControl(id);
        //   if (e == EventType.MouseDown) { /* 处理点击 */ }
        //
        // 📌 CopyFromPtr — 从原生指针复制事件数据（内部使用）。
        //    UIElementsModule 需要此方法来桥接事件。
        // ==============================================================
        [FreeFunction("GUIEvent::GetTypeForControl", HasExplicitThis = true)]
        public extern EventType GetTypeForControl(int controlID);

        [VisibleToOtherModules("UnityEngine.UIElementsModule"),
         FreeFunction("GUIEvent::CopyFromPtr", IsThreadSafe = true, HasExplicitThis = true)]
        internal extern void CopyFromPtr(IntPtr ptr);

        // ==============================================================
        // 🎯 事件队列管理
        //
        // PopEvent(outEvent) — 从全局事件队列中取出一个事件。
        //   返回 true 表示成功取出，false 表示队列为空。
        //
        // QueueEvent(outEvent) — 将事件放回队列。
        //   用于在事件未处理时传递给下一个监听者。
        //
        // GetEventAtIndex(index, outEvent) — 按索引访问事件。
        //   InputForUIModule 用于读取事件但不消费。
        //
        // GetEventCount — 获取队列中剩余事件数量。
        //
        // ClearEvents — 清空事件队列。
        //
        // 💡 事件队列是一个先进先出的管道：
        //   Input 系统产生事件 → Queue → OnGUI 消费事件 (Pop)
        //   未处理的事件可以 Queue 回去给下一个系统处理。
        // ==============================================================
        public static extern bool PopEvent([NotNull] Event outEvent);
        internal static extern void QueueEvent([NotNull] Event outEvent);
        [VisibleToOtherModules("UnityEngine.InputForUIModule")]
        internal static extern void GetEventAtIndex(int index, [NotNull] Event outEvent);
        public static extern int GetEventCount();
        internal static extern void ClearEvents();

        // ==============================================================
        // 主事件（Master Event）管理
        //
        // 🎯 Internal_SetNativeEvent — 将 C# Event 关联到 C++ 原生事件。
        //
        // 🎯 Internal_MakeMasterEventCurrent — 创建/设置主事件。
        //
        // ⚡ 主事件机制：
        //   s_MasterEvent 是全局单例 Event 对象，
        //   每帧被重用而不重新创建（避免 GC 分配）。
        //   Event.current 始终指向 s_MasterEvent。
        //
        // 🔄 每帧流程：
        //   1. 引擎调用 Internal_MakeMasterEventCurrent(displayIndex)
        //   2. s_MasterEvent 的 displayIndex 被更新
        //   3. s_Current = s_MasterEvent（设置当前事件）
        //   4. Internal_SetNativeEvent 将 C++ 事件数据同步到 C#
        //   5. OnGUI 中通过 Event.current 访问事件
        // ==============================================================
        private static extern void Internal_SetNativeEvent(IntPtr ptr);

        [RequiredByNativeCode]
        internal static void Internal_MakeMasterEventCurrent(int displayIndex)
        {
            if (s_MasterEvent == null)
                s_MasterEvent = new Event(displayIndex);
            s_MasterEvent.displayIndex = displayIndex;
            s_Current = s_MasterEvent;
            Internal_SetNativeEvent(s_MasterEvent.m_Ptr);
        }

        // ==============================================================
        // 双击检测
        //
        // 📌 GetDoubleClickTime — 获取双击判定的时间间隔（毫秒）。
        //    如果两次点击间隔小于此值，则视为双击。
        //    用于 Event.clickCount >= 2 的判断。
        // ==============================================================
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.InputForUIModule")]
        internal static extern int GetDoubleClickTime();

        // ==============================================================
        // C#/C++ 对象编组（Marshalling）
        //
        // 📌 BindingsMarshaller — 将 C# Event 对象转换为 C++ 指针。
        //    用于跨语言调用时传递 Event 对象。
        // ==============================================================
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(Event e) => e.m_Ptr;
        }
    }
}
