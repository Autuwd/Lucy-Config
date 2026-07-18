// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PlayableOutputHandle — Playable 输出端的底层句柄
//
// 📌 作用：
//   与 PlayableHandle 对称，PlayableOutputHandle 是输出端
//   （Output）的通用句柄。每个 Output 类型（AnimationPlayableOutput、
//   TexturePlayableOutput、DataPlayableOutput 等）内部持有此句柄。
//
// 🏗 核心架构（句柄 + 版本号）：
//   m_Handle  (IntPtr)  — 指向 C++ 端 PlayableOutput 对象的指针
//   m_Version (UInt32)  — 版本号，与 PlayableHandle 相同的安全模式
//
// 💡 理解关键：
//   PlayableOutputHandle 是 PlayableHandle 在输出端的镜像。
//   Input 端（PlayableHandle）管理数据接收和处理的节点，
//   Output 端（PlayableOutputHandle）管理数据输出到具体目标。
//
// 🔑 PlayableOutputHandle 提供的能力：
//   - 引用控制：GetReferenceObject/SetReferenceObject（引用的场景对象）
//   - 源连接：GetSourcePlayable/SetSourcePlayable（关联到哪个 Playable）
//   - 权重控制：GetWeight/SetWeight（混合权重）
//   - 通知系统：PushNotification/GetNotificationReceivers
//
// 📍 对应 C++ 头文件：Runtime/Director/Core/HPlayableOutput.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.Playables
{
    // ==============================================================
    // PlayableOutputHandle — 所有 PlayableOutput 的通用底层句柄
    //
    // 🔑 关键字段：
    //   m_Handle  (IntPtr)  — 指向 C++ 端 PlayableOutput 对象的指针
    //   m_Version (UInt32)  — 版本号安全校验
    //
    // 🔑 核心方法分类：
    //   输出目标：GetReferenceObject/SetReferenceObject（场景对象引用）
    //   图连接：GetSourcePlayable/SetSourcePlayable（关联源 Playable）
    //   权重控制：GetWeight/SetWeight（混合权重）
    //   通知系统：PushNotification/GetNotificationReceivers
    //
    // 💡 与 PlayableHandle 的对称设计：
    //   PlayableHandle = 输入/处理端
    //   PlayableOutputHandle = 输出端
    //   两者使用完全相同的"句柄 + 版本号"安全模式，
    //   都实现了 IEquatable<T>，都提供了 Null 静态实例。
    // ==============================================================
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [NativeHeader("Runtime/Export/Director/PlayableOutputHandle.bindings.h")]
    [UsedByNativeCode]
    public struct PlayableOutputHandle : IEquatable<PlayableOutputHandle>
    {
        internal IntPtr m_Handle;
        internal UInt32 m_Version;

        // 🎯 Null 实例：default(PlayableOutputHandle)，安全值
        static readonly PlayableOutputHandle m_Null = new PlayableOutputHandle();
        public static PlayableOutputHandle Null
        {
            get { return m_Null; }
        }

        // 🎯 类型检查：判断当前的 Output 是否为泛型 T 类型
        [VisibleToOtherModules]
        internal bool IsPlayableOutputOfType<T>()
        {
            return GetPlayableOutputType() == typeof(T);
        }

        // 🎯 HashCode = Handle 的 Hash XOR Version 的 Hash
        public override int GetHashCode()
        {
            return m_Handle.GetHashCode() ^ m_Version.GetHashCode();
        }

        // 🎯 == 运算符：比较 m_Handle 和 m_Version 是否完全一致
        public static bool operator==(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return CompareVersion(lhs, rhs);
        }

        public static bool operator!=(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return !CompareVersion(lhs, rhs);
        }

        public override bool Equals(object p)
        {
            return p is PlayableOutputHandle && Equals((PlayableOutputHandle)p);
        }

        public bool Equals(PlayableOutputHandle other)
        {
            return CompareVersion(this, other);
        }

        // 💡 版本号比较核心逻辑：Handle 和 Version 都必须相同
        static internal bool CompareVersion(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return (lhs.m_Handle == rhs.m_Handle) && (lhs.m_Version == rhs.m_Version);
        }

        // 🎯 检查 Output 是否为空
        [VisibleToOtherModules]
        extern internal bool IsNull();

        // 🎯 检查 Output 是否有效（句柄有效且版本号匹配）
        [VisibleToOtherModules]
        extern internal bool IsValid();

        // 🎯 获取 Output 的类型
        [FreeFunction("PlayableOutputHandleBindings::GetPlayableOutputType", HasExplicitThis = true, ThrowsException = true)]
        extern internal Type GetPlayableOutputType();

        // ==============================================================
        // 引用对象 — Output 关联的场景对象
        //
        // 🎯 GetReferenceObject / SetReferenceObject
        //   控制 Output 输出的目标场景对象（如 Animator、Camera）
        // ==============================================================
        [FreeFunction("PlayableOutputHandleBindings::GetReferenceObject", HasExplicitThis = true, ThrowsException = true)]
        extern internal Object GetReferenceObject();

        [FreeFunction("PlayableOutputHandleBindings::SetReferenceObject", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetReferenceObject(Object target);

        // ==============================================================
        // 用户数据 — Output 关联的自定义数据
        // ==============================================================
        [FreeFunction("PlayableOutputHandleBindings::GetUserData", HasExplicitThis = true, ThrowsException = true)]
        extern internal Object GetUserData();

        [FreeFunction("PlayableOutputHandleBindings::SetUserData", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetUserData([Writable] Object target);

        // ==============================================================
        // 源 Playable — Output 从哪个 Playable 获取数据
        //
        // 🎯 GetSourcePlayable — 获取输出源的 PlayableHandle
        // 🎯 SetSourcePlayable — 设置输出源和输出端口
        // 🎯 GetSourceOutputPort — 获取输出源使用的端口号
        // ==============================================================
        [FreeFunction("PlayableOutputHandleBindings::GetSourcePlayable", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableHandle GetSourcePlayable();

        [FreeFunction("PlayableOutputHandleBindings::SetSourcePlayable", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetSourcePlayable(PlayableHandle target, int port);

        [FreeFunction("PlayableOutputHandleBindings::GetSourceOutputPort", HasExplicitThis = true, ThrowsException = true)]
        extern internal int GetSourceOutputPort();

        // ==============================================================
        // 权重控制 — 混合权重
        // ==============================================================
        [FreeFunction("PlayableOutputHandleBindings::GetWeight", HasExplicitThis = true, ThrowsException = true)]
        extern internal float GetWeight();

        [FreeFunction("PlayableOutputHandleBindings::SetWeight", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetWeight(float weight);

        // ==============================================================
        // 通知系统 — INotification 的推送与接收
        //
        // 🎯 PushNotification — 向 Output 推送通知
        // 🎯 GetNotificationReceivers — 获取所有通知接收器
        // 🎯 AddNotificationReceiver — 添加通知接收器
        // 🎯 RemoveNotificationReceiver — 移除通知接收器
        // ==============================================================
        [FreeFunction("PlayableOutputHandleBindings::PushNotification", HasExplicitThis = true, ThrowsException = true)]
        extern internal void PushNotification(PlayableHandle origin, INotification notification, object context);

        [FreeFunction("PlayableOutputHandleBindings::GetNotificationReceivers", HasExplicitThis = true, ThrowsException = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal INotificationReceiver[] GetNotificationReceivers();

        [FreeFunction("PlayableOutputHandleBindings::AddNotificationReceiver", HasExplicitThis = true, ThrowsException = true)]
        extern internal void AddNotificationReceiver(INotificationReceiver receiver);

        [FreeFunction("PlayableOutputHandleBindings::RemoveNotificationReceiver", HasExplicitThis = true, ThrowsException = true)]
        extern internal void RemoveNotificationReceiver(INotificationReceiver receiver);

        // 🎯 获取编辑器中显示的输出名称
        [FreeFunction("PlayableOutputHandleBindings::GetEditorName", HasExplicitThis = true, ThrowsException = true)]
        extern internal string GetEditorName();
    }
}
