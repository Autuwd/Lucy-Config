// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PlayableGraph — Playables 系统的核心图容器
//
// 📌 作用：
//   PlayableGraph 是整个 Playables 系统的"舞台"。
//   它管理 Playable（可播放节点）和 PlayableOutput（输出端）的
//   生命周期，控制图拓扑的连接与断开，驱动图的更新求值。
//
// 🏗 核心架构：
//   PlayableGraph ──管理──→ PlayableHandle（包装 IntPtr + Version）
//                     ├── PlayableOutputHandle（对称的 Output 端句柄）
//                     ├── Connect / Disconnect（拓扑连接管理）
//                     ├── Evaluate（图求值驱动）
//                     └── DirectorUpdateMode（更新模式：DSPClock/GameTime/Manual）
//
// 💡 理解关键：
//   PlayableGraph 是 struct 而不是 class，所以它是值类型。
//   它内部持有 m_Handle (IntPtr) 和 m_Version (UInt32)，
//   通过这对组合实现"句柄 + 版本号"的安全引用模式。
//   当 Native 端的图被销毁时，版本号会变化，C# 端能检测到失效。
//
// 💡 图拓扑模式：
//   Connect(source, sourcePort, dest, destPort) 创建边
//   Disconnect(playable, port) 移除边
//   图是一个 DAG（有向无环图），不支持循环连接。
//   Root Playable 是没有输入节点的起始节点。
//
// 📍 对应 C++ 头文件：Runtime/Director/Core/HPlayableGraph.h
// ==============================================================

using System;
using System.ComponentModel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    // ==============================================================
    // DirectorUpdateMode — PlayableGraph 的更新方式
    //
    // 🔑 枚举值含义：
    //   DSPClock       = 0 — 使用音频 DSP 时钟（精确同步）
    //   GameTime       = 1 — 使用 GameTime（受 Time.timeScale 影响）
    //   UnscaledGameTime = 2 — 使用不受缩放的真实时间
    //   Manual         = 3 — 手动控制，只有调用 Evaluate() 时才更新
    //
    // ⚡ 性能提示：
    //   对于 UI 和菜单动画，推荐 Manual 模式以减少不必要的图求值。
    //   对于需要音频同步的过场动画，推荐 DSPClock 模式。
    //
    // 📍 必须与 C++ 端 Runtime/Director/Core/PlayableTypes.h 同步
    // ==============================================================
    public enum DirectorUpdateMode
    {
        DSPClock = 0,
        GameTime = 1,
        UnscaledGameTime = 2,
        Manual = 3
    }

    // ==============================================================
    // PlayableGraph — Playables 系统的核心图容器
    //
    // 🔑 关键字段：
    //   m_Handle  (IntPtr)  — 指向 C++ 端核心图的指针
    //   m_Version (UInt32)  — 版本号，用于检测图是否已被销毁
    //
    // 🔑 核心操作：
    //   Create()           — 创建新图
    //   Connect/Disconnect — 拓扑编辑
    //   Play/Stop/Evaluate — 生命周期控制
    //   Destroy()          — 销毁图及其所有节点
    //
    // 📌 PlayableGraph 作为所有 Playable 的容器，支持：
    //   - 多路复用：一个图可以包含多个 Playable 和 Output
    //   - 拓扑连接：通过端口号精确连接节点
    //   - 子图销毁：DestroySubgraph 递归销毁一个 Playable 及其所有子节点
    //
    // ⚠️ 注意：
    //   PlayableGraph 是 struct，赋值操作会复制句柄和版本号。
    //   赋值后的两个 struct 指向同一个 Native 图。
    // ==============================================================
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [NativeHeader("Runtime/Export/Director/PlayableGraph.bindings.h")]
    [UsedByNativeCode]
    public struct PlayableGraph
    {
        internal IntPtr m_Handle;
        internal UInt32 m_Version;

        // ==============================================================
        // GetRootPlayable — 获取根 Playable
        //
        // 🎯 根 Playable 是图中没有输入连接的节点。
        // 图的求值从根节点开始遍历到叶子节点。
        // 📌 索引 index 范围为 0 到 GetRootPlayableCount()-1
        // ==============================================================
        public Playable GetRootPlayable(int index)
        {
            PlayableHandle handle = GetRootPlayableInternal(index);
            return new Playable(handle);
        }

        // ==============================================================
        // Connect — 在图中创建一条连接边
        //
        // 🎯 源(source)的输出端口 → 目标(destination)的输入端口
        // 📌 端口号从 0 开始
        // 💡 这是 Playables 图拓扑的核心操作。
        //    内部调用 C++ 端的 PlayableGraphBindings::ConnectInternal
        // ⚡ 连接创建后，数据从 source 流向 destination
        // ==============================================================
        public bool Connect<U, V>(U source, int sourceOutputPort, V destination, int destinationInputPort)
            where U : struct, IPlayable
            where V : struct, IPlayable
        {
            return ConnectInternal(source.GetHandle(), sourceOutputPort, destination.GetHandle(), destinationInputPort);
        }

        // ==============================================================
        // Disconnect — 断开图中的一条连接边
        //
        // 💡 断开指定 Playable 在指定输入端口上的所有连接
        // ==============================================================
        public void Disconnect<U>(U input, int inputPort)
            where U : struct, IPlayable
        {
            DisconnectInternal(input.GetHandle(), inputPort);
        }

        // ==============================================================
        // DestroyPlayable — 销毁单个 Playable 节点
        //
        // ⚡ 只销毁指定节点，不销毁其子节点
        // ==============================================================
        public void DestroyPlayable<U>(U playable)
            where U : struct, IPlayable
        {
            DestroyPlayableInternal(playable.GetHandle());
        }

        // ==============================================================
        // DestroySubgraph — 销毁 Playable 及其所有子节点
        //
        // ⚡ 递归销毁，清理整个子树
        // ⚠️ 确保没有其他 Output 引用被销毁的节点
        // ==============================================================
        public void DestroySubgraph<U>(U playable)
            where U : struct, IPlayable
        {
            DestroySubgraphInternal(playable.GetHandle());
        }

        // ==============================================================
        // DestroyOutput — 销毁 Output 节点
        // ==============================================================
        public void DestroyOutput<U>(U output)
            where U : struct, IPlayableOutput
        {
            DestroyOutputInternal(output.GetHandle());
        }

        // ==============================================================
        // GetOutputCountByType — 获取指定类型的 Output 数量
        // ==============================================================
        public int GetOutputCountByType<T>()
            where T : struct, IPlayableOutput
        {
            return GetOutputCountByTypeInternal(typeof(T));
        }

        // ==============================================================
        // GetOutput — 按索引获取 Output
        //
        // 💡 返回 PlayableOutput.Null 表示索引超出范围
        // ==============================================================
        public PlayableOutput GetOutput(int index)
        {
            PlayableOutputHandle handle;
            if (!GetOutputInternal(index, out handle))
                return PlayableOutput.Null;
            return new PlayableOutput(handle);
        }

        // ==============================================================
        // GetOutputByType — 按类型和索引获取 Output
        // ==============================================================
        public PlayableOutput GetOutputByType<T>(int index)
            where T : struct, IPlayableOutput
        {
            PlayableOutputHandle handle;
            if (!GetOutputByTypeInternal(typeof(T), index, out handle))
                return PlayableOutput.Null;
            return new PlayableOutput(handle);
        }

        // ==============================================================
        // Evaluate — 驱动图执行求值
        //
        // 🎯 当图处于 Manual 模式时，必须手动调用 Evaluate 来驱动更新
        // 💡 deltaTime=0 时执行增量求值
        // ==============================================================
        public void Evaluate()
        {
            Evaluate(0);
        }

        // ==============================================================
        // Create — 创建新的 PlayableGraph
        //
        // 🎯 这是 Playables 系统的入口点。
        //    所有 Playable 和 Output 都必须属于某个 Graph。
        // 💡 Create() 无参重载等价于 Create(null)
        // ==============================================================
        public static PlayableGraph Create()
        {
            return Create(null);
        }

        // ==============================================================
        // Create(string) — 带名称的图创建
        //
        // 📌 名称用于编辑器显示和调试
        // ==============================================================
        extern public static PlayableGraph Create(string name);

        [FreeFunction("PlayableGraphBindings::Destroy", HasExplicitThis = true, ThrowsException = true)]
        extern public void Destroy();

        extern public bool IsValid();

        [FreeFunction("PlayableGraphBindings::IsPlaying", HasExplicitThis = true, ThrowsException = true)]
        extern public bool IsPlaying();

        [FreeFunction("PlayableGraphBindings::IsDone", HasExplicitThis = true, ThrowsException = true)]
        extern public bool IsDone();

        [FreeFunction("PlayableGraphBindings::Play", HasExplicitThis = true, ThrowsException = true)]
        extern public void Play();

        [FreeFunction("PlayableGraphBindings::Stop", HasExplicitThis = true, ThrowsException = true)]
        extern public void Stop();

        [FreeFunction("PlayableGraphBindings::Evaluate", HasExplicitThis = true, ThrowsException = true)]
        extern public void Evaluate([DefaultValue("0")] float deltaTime);

        [FreeFunction("PlayableGraphBindings::GetTimeUpdateMode", HasExplicitThis = true, ThrowsException = true)]
        extern public DirectorUpdateMode GetTimeUpdateMode();

        [FreeFunction("PlayableGraphBindings::SetTimeUpdateMode", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetTimeUpdateMode(DirectorUpdateMode value);

        [FreeFunction("PlayableGraphBindings::GetResolver", HasExplicitThis = true, ThrowsException = true)]
        extern public IExposedPropertyTable GetResolver();

        [FreeFunction("PlayableGraphBindings::SetResolver", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetResolver(IExposedPropertyTable value);

        [FreeFunction("PlayableGraphBindings::GetPlayableCount", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetPlayableCount();

        [FreeFunction("PlayableGraphBindings::GetRootPlayableCount", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetRootPlayableCount();

        [FreeFunction("PlayableGraphBindings::SynchronizeEvaluation", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SynchronizeEvaluation(PlayableGraph playable);

        [FreeFunction("PlayableGraphBindings::GetOutputCount", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetOutputCount();

        [FreeFunction("PlayableGraphBindings::CreatePlayableHandle", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableHandle CreatePlayableHandle();

        [FreeFunction("PlayableGraphBindings::CreateScriptOutputInternal", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool CreateScriptOutputInternal(string name, out PlayableOutputHandle handle);

        [FreeFunction("PlayableGraphBindings::GetRootPlayableInternal", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableHandle GetRootPlayableInternal(int index);

        [FreeFunction("PlayableGraphBindings::DestroyOutputInternal", HasExplicitThis = true, ThrowsException = true)]
        extern internal void DestroyOutputInternal(PlayableOutputHandle handle);

        [FreeFunction("PlayableGraphBindings::IsMatchFrameRateEnabled", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool IsMatchFrameRateEnabled();

        [FreeFunction("PlayableGraphBindings::EnableMatchFrameRate", HasExplicitThis = true, ThrowsException = true)]
        extern internal void EnableMatchFrameRate(FrameRate frameRate);

        [FreeFunction("PlayableGraphBindings::DisableMatchFrameRate", HasExplicitThis = true, ThrowsException = true)]
        extern internal void DisableMatchFrameRate();

        [FreeFunction("PlayableGraphBindings::GetFrameRate", HasExplicitThis = true, ThrowsException = true)]
        extern internal FrameRate GetFrameRate();

        [FreeFunction("PlayableGraphBindings::GetOutputInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private bool GetOutputInternal(int index, out PlayableOutputHandle handle);

        [FreeFunction("PlayableGraphBindings::GetOutputCountByTypeInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetOutputCountByTypeInternal(Type outputType);

        [FreeFunction("PlayableGraphBindings::GetOutputByTypeInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private bool GetOutputByTypeInternal(Type outputType, int index, out PlayableOutputHandle handle);

        [FreeFunction("PlayableGraphBindings::ConnectInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private bool ConnectInternal(PlayableHandle source, int sourceOutputPort, PlayableHandle destination, int destinationInputPort);

        [FreeFunction("PlayableGraphBindings::DisconnectInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private void DisconnectInternal(PlayableHandle playable, int inputPort);

        [FreeFunction("PlayableGraphBindings::DestroyPlayableInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private void DestroyPlayableInternal(PlayableHandle playable);

        [FreeFunction("PlayableGraphBindings::DestroySubgraphInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private void DestroySubgraphInternal(PlayableHandle playable);

        [FreeFunction("PlayableGraphBindings::GetEditorName", HasExplicitThis = true, ThrowsException = true)]
        extern public string GetEditorName();
    }
}
