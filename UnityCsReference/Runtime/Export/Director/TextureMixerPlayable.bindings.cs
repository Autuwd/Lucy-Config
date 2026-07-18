// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 TextureMixerPlayable — 纹理混合 Playable
//
// 📌 作用：
//   TextureMixerPlayable 用于在 PlayableGraph 中混合
//   多个纹理输入。它接收多个纹理输入源，根据权重
//   混合后输出最终的纹理结果。
//
// 💡 使用场景：
//   在 Timeline 中实现纹理的过渡和混合效果。
//   通过连接多个纹理源到 Mixer，可以实现渐变过渡。
//
// 🏗 架构模式：
//   这是一个典型的"混合器"Playable：
//   1. 多个输入 → 加权混合 → 单个输出
//   2. 输入权重通过 PlayableHandle.SetInputWeight() 控制
//   3. 隐式类型转换：TextureMixerPlayable ↔ Playable
//
// 📍 对应 C++ 头文件：Runtime/Graphics/Director/TextureMixerPlayable.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Experimental.Playables
{
    // ==============================================================
    // TextureMixerPlayable — 纹理混合器 Playable
    //
    // 🔑 关键字段：
    //   m_Handle (PlayableHandle) — 包装的底层句柄
    //
    // 🔑 核心操作：
    //   Create(graph) — 在图中创建纹理混合器
    //
    // 🏗 混合器架构模式：
    //   TextureMixerPlayable 是一个"混合器节点"：
    //   多个纹理输入 → 加权混合 → 单个输出
    //   输入端数通过 PlayableHandle.SetInputCount() 控制
    //   混合权重通过 PlayableHandle.SetInputWeight() 控制
    //
    // 💡 使用场景：
    //   在 Timeline 中实现纹理的渐变过渡和混合效果。
    //   通常与 TexturePlayableOutput 配合使用形成完整链路：
    //   纹理源 A →┤
    //              ├→ TextureMixerPlayable → TexturePlayableOutput → RenderTexture
    //   纹理源 B →┤
    // ==============================================================
    [NativeHeader("Runtime/Export/Director/TextureMixerPlayable.bindings.h")]
    [NativeHeader("Runtime/Graphics/Director/TextureMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("TextureMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public partial struct TextureMixerPlayable : IPlayable, IEquatable<TextureMixerPlayable>
    {
        PlayableHandle m_Handle;

        // ==============================================================
        // Create — 创建纹理混合器 Playable
        //
        // 🎯 在指定图中创建一个纹理混合节点
        // ==============================================================
        public static TextureMixerPlayable Create(PlayableGraph graph)
        {
            var handle = CreateHandle(graph);
            return new TextureMixerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateTextureMixerPlayableInternal(ref graph, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        // ==============================================================
        // 构造函数 — 带类型安全检查的 Handle 包装
        //
        // ⚡ 确保传入的 Handle 是 TextureMixerPlayable 类型
        // ==============================================================
        internal TextureMixerPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<TextureMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an TextureMixerPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(TextureMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator TextureMixerPlayable(Playable playable)
        {
            return new TextureMixerPlayable(playable.GetHandle());
        }

        public bool Equals(TextureMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateTextureMixerPlayableInternal(ref PlayableGraph graph, ref PlayableHandle handle);
    }
}
