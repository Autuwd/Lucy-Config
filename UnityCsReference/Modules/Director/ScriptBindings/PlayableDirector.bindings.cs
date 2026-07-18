// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PlayableDirector — PlayableGraph 的运行时控制器
//
// 📌 作用：
//   PlayableDirector 是场景中的 MonoBehaviour 组件，管理 Timeline
//   或自定义 PlayableAsset 驱动的 PlayableGraph 生命周期。
//
// 🔑 核心职责：
//   - 创建和管理 PlayableGraph
//   - 控制 Play/Pause/Stop/Evaluate
//   - 管理 Track 到场景对象的绑定（Binding）
//   - 提供外观属性表（IExposedPropertyTable）支持
//
// 💡 与 Timeline 的关系：
//   PlayableDirector 是 Timeline 的"播放器"。TimelineAsset 是
//   PlayableAsset，PlayableDirector 解析 Asset 构建 PlayableGraph，
//   然后驱动它播放。没有 Director，Graph 无法自动运行。
//
// 📍 对应 C++ 头文件：Modules/Director/PlayableDirector.h
// ==============================================================

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Playables
{
    // ==============================================================
    // PlayableDirector — Timeline/Playable 的 MonoBehaviour 控制器
    //
    // 🎯 功能：
    //   作为场景中的"指挥家"，统筹 PlayableGraph 的创建、播放和绑定。
    //
    // 🔑 关键属性：
    //   - state:            播放状态（Playing / Paused / Stopped）
    //   - playableGraph:    自动管理的 PlayableGraph 实例
    //   - playableAsset:    当前分配的时间线资产（TimelineAsset 等）
    //   - time:             当前播放时间
    //   - extrapolationMode:播放结束后行为（Loop / Hold / None）
    //   - playOnAwake:      Awake 时自动播放
    //
    // 🔑 关键方法：
    //   - Play():           开始播放
    //   - Pause()/Stop():   暂停/停止
    //   - Evaluate():       立即求值到当前时间
    //   - RebuildGraph():   重建 Graph（Asset 改变时调用）
    //   - SetGenericBinding(): 绑定 Track 到场景物体
    //
    // 💡 事件回调：
    //   - played / paused / stopped 事件由 C++ 端 Native 代码触发
    //   - 通过 [RequiredByNativeCode] 标记的 SendOn* 方法回调 C#
    //
    // 📍 对应 C++ 头文件：Modules/Director/PlayableDirector.h
    // ==============================================================
    [NativeHeader("Modules/Director/PlayableDirector.h")]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
    [RequiredByNativeCode]
    public partial class PlayableDirector : Behaviour, IExposedPropertyTable
    {
        // ==============================================================
        // state — 播放状态
        //
        // 📌 只读属性，状态由 Play/Pause/Stop 控制。
        //    PlayState 枚举：Playing / Paused / Stopped
        // ==============================================================
        public PlayState state
        {
            get { return GetPlayState(); }
        }

        // ==============================================================
        // extrapolationMode — 播放结束行为
        //
        // 📌 控制当播放超出 duration 时的行为：
        //   - Loop：循环播放
        //   - Hold：停在最后一帧
        //   - None：不处理
        // ==============================================================
        public DirectorWrapMode extrapolationMode
        {
            set { SetWrapMode(value); }
            get { return GetWrapMode(); }
        }

        // ==============================================================
        // playableAsset — 当前播放的 PlayableAsset
        //
        // 🎯 这是 Director 的"乐谱"。
        //   TimelineAsset 是 PlayableAsset 最常见的形式。
        //   赋值新 Asset 会触发 Graph 重建。
        // ==============================================================
        public PlayableAsset playableAsset
        {
            get { return Internal_GetPlayableAsset() as PlayableAsset; }
            set { SetPlayableAsset(value as ScriptableObject); }
        }

        // ==============================================================
        // playableGraph — 自动管理的 PlayableGraph
        //
        // 💡 这是 Director 的私有 Graph，由引擎自动管理。
        //   也可以通过 PlayableGraph.Create() 手动创建 Graph，
        //   但 Director 自动管理的版本更方便。
        // ==============================================================
        public PlayableGraph playableGraph
        {
            get { return GetGraphHandle(); }
        }

        // ==============================================================
        // playOnAwake — 启动时自动播放
        //
        // 📌 如果设置为 true，Director 在 Awake 时会自动调用 Play()
        // ==============================================================
        public bool playOnAwake
        {
            get { return GetPlayOnAwake(); }
            set { SetPlayOnAwake(value); }
        }

        // ==============================================================
        // DeferredEvaluate — 下一帧求值
        //
        // 📌 标记下一帧执行 Evaluate()，用于延迟求值场景。
        // ==============================================================
        public void DeferredEvaluate()
        {
            EvaluateNextFrame();
        }

        internal void Play(FrameRate frameRate) => PlayOnFrame(frameRate);

        // ==============================================================
        // Play(asset) — 设置 Asset 并播放
        //
        // 🎯 便捷方法：一次设置 asset 和 extrapolationMode 并播放。
        // ==============================================================
        public void Play(PlayableAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");

            Play(asset, extrapolationMode);
        }

        public void Play(PlayableAsset asset, DirectorWrapMode mode)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");

            playableAsset = asset;
            extrapolationMode = mode;
            Play();
        }

        // ==============================================================
        // SetGenericBinding — 绑定 Track 到场景对象
        //
        // 🎯 将 Timeline 中的 Track 绑定到具体的场景物体。
        //   key:   Track 在 Asset 中的标识对象
        //   value: 场景中目标 Animator / AudioSource 等
        //
        // 💡 示例：将 "AnimationTrack" 绑定到角色 Animator
        //   director.SetGenericBinding(animationTrack, characterAnimator);
        // ==============================================================
        public void SetGenericBinding(Object key, Object value)
        {
            Internal_SetGenericBinding(key, value);
        }

        // ==============================================================
        // 📌 基本属性（C++ extern 属性）
        //   timeUpdateMode:   时间更新模式（GameTime / Unscaled / Manual）
        //   time:             当前播放时间
        //   initialTime:      初始播放时间
        //   duration:         总时长（由 PlayableAsset 决定）
        // ==============================================================
        extern public DirectorUpdateMode timeUpdateMode { set; get; }
        extern public double time { set; get; }
        extern public double initialTime { set; get; }
        extern public double duration { get; }

        // ==============================================================
        // 📌 播放控制方法
        //   Evaluate():   立即求值（不等待下一帧）
        //   Play():       开始/继续播放
        //   Stop():       停止并重置时间
        //   Pause():      暂停（保持当前时间）
        //   Resume():     从暂停处继续
        //   RebuildGraph(): 重建整个 PlayableGraph
        // ==============================================================
        [NativeMethod(ThrowsException = true)]
        extern public void Evaluate();
        [NativeMethod(ThrowsException = true)]
        extern private void PlayOnFrame(FrameRate frameRate);
        [NativeMethod(ThrowsException = true)]
        extern public void Play();
        extern public void Stop();
        extern public void Pause();
        extern public void Resume();
        [NativeMethod(ThrowsException = true)]
        extern public void RebuildGraph();

        // ==============================================================
        // 📌 外观属性表（IExposedPropertyTable）
        //   Set/Get/ClearReferenceValue：管理 Asset 中暴露的属性引用
        //   用于 Premake（预生成）时的属性绑定
        // ==============================================================
        extern public void ClearReferenceValue(PropertyName id);
        extern public void SetReferenceValue(PropertyName id, UnityEngine.Object value);
        extern public UnityEngine.Object GetReferenceValue(PropertyName id, out bool idValid);

        // ==============================================================
        // 📌 绑定管理
        //   GetGenericBinding：    获取指定 Track 的绑定对象
        //   ClearGenericBinding：  清除指定 Track 的绑定
        //   RebindPlayableGraphOutputs：重新绑定所有 Output
        // ==============================================================
        [NativeMethod("GetBindingFor")]
        extern public Object GetGenericBinding(Object key);
        [NativeMethod("ClearBindingFor")]
        extern public void ClearGenericBinding(Object key);
        [NativeMethod(ThrowsException = true)]
        extern public void RebindPlayableGraphOutputs();

        extern internal void ProcessPendingGraphChanges();
        [NativeMethod("HasBinding")]
        extern internal bool HasGenericBinding(Object key);

        extern private PlayState GetPlayState();
        extern private void SetWrapMode(DirectorWrapMode mode);
        extern private DirectorWrapMode GetWrapMode();
        [NativeMethod(ThrowsException = true)]
        extern private void EvaluateNextFrame();
        extern private PlayableGraph GetGraphHandle();
        extern private void SetPlayOnAwake(bool on);
        extern private bool GetPlayOnAwake();
        [NativeMethod(ThrowsException = true)]
        extern private void Internal_SetGenericBinding(Object key, Object value);
        extern private void SetPlayableAsset(ScriptableObject asset);
        extern private ScriptableObject Internal_GetPlayableAsset();

        // ==============================================================
        // 🎯 事件回调（Native → Managed 通信）
        //
        // 💡 这些事件由 C++ 端的 PlayableDirector 在状态变化时调用：
        //   - played:  开始播放时触发
        //   - paused:  暂停时触发
        //   - stopped: 停止时触发
        //
        // 📌 SendOn* 方法通过 [RequiredByNativeCode] 标记，
        //    确保 IL2CPP/AOT 保留这些方法不被裁剪。
        // ==============================================================
        public event Action<PlayableDirector> played;
        public event Action<PlayableDirector> paused;
        public event Action<PlayableDirector> stopped;

        // ==============================================================
        // ⚡ 内部 Director 管理器 API
        //   ResetFrameTiming：重置帧计时（用于编辑器预览等场景）
        // ==============================================================
        [NativeHeader("Runtime/Director/Core/DirectorManager.h")]
        [StaticAccessor("GetDirectorManager()", StaticAccessorType.Dot)]
        internal extern static void ResetFrameTiming();

        [RequiredByNativeCode]
        void SendOnPlayableDirectorPlay()
        {
            if (played != null)
                played(this);
        }

        [RequiredByNativeCode]
        void SendOnPlayableDirectorPause()
        {
            if (paused != null)
                paused(this);
        }

        [RequiredByNativeCode]
        void SendOnPlayableDirectorStop()
        {
            if (stopped != null)
                stopped(this);
        }
    }
}
