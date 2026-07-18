// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 TexturePlayableOutput — 纹理渲染输出
//
// 📌 作用：
//   TexturePlayableOutput 是 Playables 系统的纹理渲染输出端。
//   它将 PlayableGraph 的纹理处理结果输出到指定的 RenderTexture，
//   实现 Timeline 驱动的视频/纹理渲染管线。
//
// 🏗 核心链路：
//   Timeline/Playable → TextureMixerPlayable（混合处理）
//     → TexturePlayableOutput（输出端）
//       → RenderTexture（渲染目标）
//
// 💡 使用场景：
//   - VideoPlayer 的 Timeline 集成
//   - 纹理动画和过渡效果
//   - 实时纹理合成输出到 RenderTexture
//
// 🔑 核心操作：
//   Create(graph, name, target) — 创建纹理输出
//   GetTarget()/SetTarget() — 控制渲染目标 RenderTexture
//
// 📍 对应 C++ 头文件：Runtime/Graphics/Director/TexturePlayableOutput.h
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Experimental.Playables
{
    // ==============================================================
    // TexturePlayableOutput — 纹理渲染输出
    //
    // 🔑 关键字段：
    //   m_Handle (PlayableOutputHandle) — 包装的底层 Output 句柄
    //
    // 🔑 核心操作：
    //   Create(graph, name, target) — 创建纹理输出
    //   GetTarget()/SetTarget() — 控制渲染目标 RenderTexture
    //
    // 🏗 完整纹理 Playables 管线：
    //   纹理源（Camera/Video/纹理动画）
    //     → TextureMixerPlayable（混合处理）
    //       → TexturePlayableOutput（输出端）
    //         → RenderTexture（渲染目标）
    //
    // 💡 与 GameObject 的关联：
    //   TexturePlayableOutput 的输出目标是 RenderTexture，
    //   RenderTexture 可以被材质使用，最终显示在 GameObject 上。
    // ==============================================================
    [NativeHeader("Runtime/Export/Director/TexturePlayableOutput.bindings.h")]
    [NativeHeader("Runtime/Graphics/Director/TexturePlayableOutput.h")]
    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    [StaticAccessor("TexturePlayableOutputBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct TexturePlayableOutput : IPlayableOutput
    {
        private PlayableOutputHandle m_Handle;

        // ==============================================================
        // Create — 创建纹理输出
        //
        // 🎯 在 PlayableGraph 中创建纹理输出节点
        // 📌 创建后自动调用 SetTarget 绑定渲染目标
        // ⚡ 创建失败时返回 TexturePlayableOutput.Null
        // ==============================================================
        public static TexturePlayableOutput Create(PlayableGraph graph, string name, RenderTexture target)
        {
            PlayableOutputHandle handle;
            if (!TexturePlayableGraphExtensions.InternalCreateTextureOutput(ref graph, name, out handle))
                return TexturePlayableOutput.Null;

            TexturePlayableOutput output = new TexturePlayableOutput(handle);
            output.SetTarget(target);

            return output;
        }

        // ==============================================================
        // 构造函数 — 带类型安全检查的 Handle 包装
        //
        // ⚡ 确保传入的 Handle 是 TexturePlayableOutput 类型
        // ==============================================================
        internal TexturePlayableOutput(PlayableOutputHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOutputOfType<TexturePlayableOutput>())
                    throw new InvalidCastException("Can't set handle: the playable is not an TexturePlayableOutput.");
            }

            m_Handle = handle;
        }

        // 🎯 Null 实例：默认的空输出
        public static TexturePlayableOutput Null
        {
            get { return new TexturePlayableOutput(PlayableOutputHandle.Null); }
        }

        public PlayableOutputHandle GetHandle()
        {
            return m_Handle;
        }

        // 💡 隐式转换：TexturePlayableOutput → PlayableOutput（向上转型）
        public static implicit operator PlayableOutput(TexturePlayableOutput output)
        {
            return new PlayableOutput(output.GetHandle());
        }

        // 💡 显式转换：PlayableOutput → TexturePlayableOutput（向下转型）
        public static explicit operator TexturePlayableOutput(PlayableOutput output)
        {
            return new TexturePlayableOutput(output.GetHandle());
        }

        // ==============================================================
        // GetTarget / SetTarget — 控制渲染目标 RenderTexture
        //
        // 🎯 纹理输出管线的最终输出目标
        // ==============================================================
        public RenderTexture GetTarget()
        {
            return InternalGetTarget(ref m_Handle);
        }

        public void SetTarget(RenderTexture value)
        {
            InternalSetTarget(ref m_Handle, value);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static RenderTexture InternalGetTarget(ref PlayableOutputHandle output);

        [NativeMethod(ThrowsException = true)]
        extern private static void InternalSetTarget(ref PlayableOutputHandle output, RenderTexture target);
    }
}
