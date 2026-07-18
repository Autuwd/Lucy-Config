// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 MaterialEffectPlayable — Timeline 材质效果 Playable
//
// 📌 作用：
//   MaterialEffectPlayable 允许在 Timeline 中控制材质效果。
//   可以通过时间线精确控制 Material 的 Pass 切换，实现
//   类似于"时间线驱动的后期处理效果"。
//
// 🔑 核心操作：
//   Create(graph, material, pass) — 创建材质效果 Playable
//   GetMaterial()/SetMaterial() — 控制目标材质
//   GetPass()/SetPass() — 控制材质渲染 Pass
//
// 💡 使用场景：
//   在 Timeline 中制作过场动画时，可以在特定时间点切换
//   材质的渲染 Pass，实现画面特效的精确时间控制。
//
// 📍 对应 C++ 头文件：Runtime/Shaders/Director/MaterialEffectPlayable.h
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
    // MaterialEffectPlayable — Timeline 材质效果 Playable
    //
    // 🔑 关键字段：
    //   m_Handle (PlayableHandle) — 包装的底层句柄
    //
    // 🔑 核心操作：
    //   Create(graph, material, pass) — 创建材质效果 Playable
    //   GetMaterial()/SetMaterial() — 控制目标材质
    //   GetPass()/SetPass() — 控制渲染 Pass
    //
    // 💡 使用场景：
    //   在 Timeline 中实现"时间线驱动的材质特效切换"。
    //   例如，在过场动画的特定时间点切换材质的渲染 Pass，
    //   实现画面特效变化。
    // ==============================================================
    [NativeHeader("Runtime/Export/Director/MaterialEffectPlayable.bindings.h")]
    [NativeHeader("Runtime/Shaders/Director/MaterialEffectPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("MaterialEffectPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct MaterialEffectPlayable : IPlayable, IEquatable<MaterialEffectPlayable>
    {
        PlayableHandle m_Handle;

        // ==============================================================
        // Create — 创建材质效果 Playable
        //
        // 🎯 在指定图中创建材质控制节点
        // 📌 pass = -1 表示使用材质的默认 Pass
        // ==============================================================
        public static MaterialEffectPlayable Create(PlayableGraph graph, Material material, int pass = -1)
        {
            var handle = CreateHandle(graph, material, pass);
            return new MaterialEffectPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, Material material, int pass)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateMaterialEffectPlayable(ref graph, material, pass, ref handle))
                return PlayableHandle.Null;
            return handle;
        }

        // ==============================================================
        // 构造函数 — 带类型安全检查的 Handle 包装
        //
        // ⚡ 确保传入的 Handle 是 MaterialEffectPlayable 类型
        // ==============================================================
        internal MaterialEffectPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<MaterialEffectPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an MaterialEffectPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(MaterialEffectPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator MaterialEffectPlayable(Playable playable)
        {
            return new MaterialEffectPlayable(playable.GetHandle());
        }

        public bool Equals(MaterialEffectPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // ==============================================================
        // GetMaterial / SetMaterial — 控制目标材质
        // ==============================================================
        public Material GetMaterial()
        {
            return GetMaterialInternal(ref m_Handle);
        }

        public void SetMaterial(Material value)
        {
            SetMaterialInternal(ref m_Handle, value);
        }

        // ==============================================================
        // GetPass / SetPass — 控制材质的渲染 Pass
        // ==============================================================
        public int GetPass()
        {
            return GetPassInternal(ref m_Handle);
        }

        public void SetPass(int value)
        {
            SetPassInternal(ref m_Handle, value);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static Material GetMaterialInternal(ref PlayableHandle hdl);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetMaterialInternal(ref PlayableHandle hdl, Material material);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetPassInternal(ref PlayableHandle hdl);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetPassInternal(ref PlayableHandle hdl, int pass);

        [NativeMethod(ThrowsException = true)]
        extern private static bool InternalCreateMaterialEffectPlayable(ref PlayableGraph graph, Material material, int pass, ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static bool ValidateType(ref PlayableHandle hdl);
    }
}
