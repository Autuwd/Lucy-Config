// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 CameraPlayable — Timeline 相机切换 Playable
//
// 📌 作用：
//   CameraPlayable 允许在 Timeline 中控制相机的切换。
//   通过将 CameraPlayable 连接到 PlayableGraph，可以在
//   时间线上精确控制哪个相机处于活动状态。
//
// 💡 理解关键：
//   CameraPlayable 是一个 struct（值类型），内部持有 PlayableHandle。
//   它实现了 IPlayable 接口，所以可以参与 Playables 图拓扑。
//   Create(PlayableGraph, Camera) 在指定图中创建对应的 Native 节点。
//
// 🔑 核心操作：
//   GetCamera()/SetCamera() — 获取/设置关联的 Camera 对象
//   隐式类型转换：CameraPlayable ↔ Playable（通过 PlayableHandle）
//
// 🏗 架构模式：
//   CameraPlayable 是"具体 Playable 类型"的典型实现：
//   1. 内部持有 PlayableHandle m_Handle
//   2. 实现 IPlayable 接口
//   3. 提供类型安全的 Create 工厂方法
//   4. 提供类型特定的功能（GetCamera/SetCamera）
//
// 📍 对应 C++ 头文件：Runtime/Camera/Director/CameraPlayable.h
// ==============================================================

using System;
using System.ComponentModel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Experimental.Playables
{
    // ==============================================================
    // CameraPlayable — Timeline 相机切换 Playable
    //
    // 🔑 关键字段：
    //   m_Handle (PlayableHandle) — 包装的底层句柄
    //
    // 🔑 核心操作：
    //   Create(graph, camera) — 在图中创建相机控制 Playable
    //   GetCamera()/SetCamera() — 控制目标相机
    //
    // 🏗 典型"具体 Playable 类型"的实现模式：
    //   1. struct 值类型，实现 IPlayable + IEquatable<T>
    //   2. 内部持有 PlayableHandle m_Handle
    //   3. 静态 Create 工厂方法 → 调用 Native 创建句柄
    //   4. 类型安全的隐式/显式转换运算符
    //   5. 类型特定的 Get/Set 方法
    // ==============================================================
    [NativeHeader("Runtime/Export/Director/CameraPlayable.bindings.h")]
    [NativeHeader("Runtime/Camera//Director/CameraPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("CameraPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct CameraPlayable : IPlayable, IEquatable<CameraPlayable>
    {
        PlayableHandle m_Handle;

        // ==============================================================
        // Create — 在 PlayableGraph 中创建相机控制 Playable
        //
        // 🎯 创建成功后，CameraPlayable 可以连接到图中控制相机切换
        // 📌 内部调用 InternalCreateCameraPlayable（C++ Native 方法）
        // ==============================================================
        public static CameraPlayable Create(PlayableGraph graph, Camera camera)
        {
            var handle = CreateHandle(graph, camera);
            return new CameraPlayable(handle);
        }

        // 💡 创建失败时返回 PlayableHandle.Null
        private static PlayableHandle CreateHandle(PlayableGraph graph, Camera camera)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateCameraPlayable(ref graph, camera, ref handle))
                return PlayableHandle.Null;
            return handle;
        }

        // ==============================================================
        // 构造函数 — 从 PlayableHandle 构造 CameraPlayable
        //
        // ⚡ 类型安全检查：通过 IsPlayableOfType<CameraPlayable>()
        //   确保传入的 Handle 确实是 CameraPlayable 类型
        // ==============================================================
        internal CameraPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<CameraPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an CameraPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        // 💡 隐式转换：CameraPlayable → Playable（向上转型）
        public static implicit operator Playable(CameraPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        // 💡 显式转换：Playable → CameraPlayable（向下转型）
        public static explicit operator CameraPlayable(Playable playable)
        {
            return new CameraPlayable(playable.GetHandle());
        }

        // 🎯 相等比较：基于底层 Handle 的版本号比较
        public bool Equals(CameraPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // ==============================================================
        // GetCamera / SetCamera — 控制关联的 Camera 对象
        //
        // 🎯 获取或设置 CameraPlayable 控制的相机引用
        // 📌 直接转发到 C++ 端的 Native 方法
        // ==============================================================
        public Camera GetCamera()
        {
            return GetCameraInternal(ref m_Handle);
        }

        public void SetCamera(Camera value)
        {
            SetCameraInternal(ref m_Handle, value);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static Camera GetCameraInternal(ref PlayableHandle hdl);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetCameraInternal(ref PlayableHandle hdl, Camera camera);

        [NativeMethod(ThrowsException = true)]
        extern private static bool InternalCreateCameraPlayable(ref PlayableGraph graph, Camera camera, ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static bool ValidateType(ref PlayableHandle hdl);
    }
}
