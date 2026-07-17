// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AnimatorControllerPlayable —— 可播放图形 Animator 控制器
//
// 【概述】
// AnimatorControllerPlayable 是 Animator Controller 在 Playables API
// 系统中的实现。它允许在 Timeline、Custom Playable 或 PlayableGraph
// 中使用 Animator Controller，而不绑定传统的 Animator 组件。
//
// 【与 Animator 的区别】
// 1. Animator 是 Component（挂载在 GameObject 上）
// 2. AnimatorControllerPlayable 是 struct + PlayableHandle
//    （在 PlayableGraph 中使用）
//
// 【使用场景】
// - Timeline 中控制动画
// - 自定义 PlayableGraph 架构
// - 需要多个 Animator Controller 相互交互
//
// 【功能】
// - 参数控制：Get/Set Float/Bool/Integer/Trigger
// - 状态查询：GetCurrentAnimatorStateInfo / IsInTransition
// - 播放控制：Play / CrossFade / CrossFadeInFixedTime
// - 层管理：GetLayerCount / GetLayerWeight / SetLayerWeight
// - 剪辑信息：GetCurrentAnimatorClipInfo
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;
using System.Runtime.InteropServices;

namespace UnityEngine.Animations
{
    /// <summary>
    /// AnimatorControllerPlayable —— 基于 Playables API 的 Animator Controller。
    /// 提供与 Animator 组件相同的参数/状态/播放控制功能，
    /// 但适用于 PlayableGraph 架构。
    /// </summary>
    [NativeHeader("Modules/Animation/ScriptBindings/AnimatorControllerPlayable.bindings.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animator.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimatorControllerPlayable.h")]
    [NativeHeader("Modules/Animation/RuntimeAnimatorController.h")]
    [NativeHeader("Modules/Animation/AnimatorInfo.h")]
    [StaticAccessor("AnimatorControllerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public partial struct AnimatorControllerPlayable : IPlayable, IEquatable<AnimatorControllerPlayable>
    {
        // 底层 PlayableHandle —— 指向 C++ 侧的 Playable 对象
        PlayableHandle m_Handle;

        /// <summary>空（Null）Playable 实例。</summary>
        static readonly AnimatorControllerPlayable m_NullPlayable = new AnimatorControllerPlayable(PlayableHandle.Null);
        public static AnimatorControllerPlayable Null { get { return m_NullPlayable; } }

        /// <summary>在指定 PlayableGraph 中创建 AnimatorControllerPlayable。</summary>
        /// <param name="graph">所属的 PlayableGraph</param>
        /// <param name="controller">需要播放的 RuntimeAnimatorController</param>
        /// <returns>创建的 AnimatorControllerPlayable 实例</returns>
        public static AnimatorControllerPlayable Create(PlayableGraph graph, RuntimeAnimatorController controller)
        {
            var handle = CreateHandle(graph, controller);
            return new AnimatorControllerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, RuntimeAnimatorController controller)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, controller, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        /// <summary>内部构造 — 从已有的 PlayableHandle 创建。</summary>
        internal AnimatorControllerPlayable(PlayableHandle handle)
        {
            m_Handle = PlayableHandle.Null;
            SetHandle(handle);
        }

        /// <summary>获取此 Playable 的底层 PlayableHandle。</summary>
        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        /// <summary>设置此 Playable 的底层 PlayableHandle（只能在创建时调用一次）。</summary>
        public void SetHandle(PlayableHandle handle)
        {
            if (m_Handle.IsValid())
                throw new InvalidOperationException("Cannot call IPlayable.SetHandle on an instance that already contains a valid handle.");

            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimatorControllerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimatorControllerPlayable.");
            }

            m_Handle = handle;
        }

        /// <summary>隐式转换为 Playable 基类。</summary>
        public static implicit operator Playable(AnimatorControllerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        /// <summary>从 Playable 显式转换回 AnimatorControllerPlayable。</summary>
        public static explicit operator AnimatorControllerPlayable(Playable playable)
        {
            return new AnimatorControllerPlayable(playable.GetHandle());
        }

        /// <summary>比较两个 AnimatorControllerPlayable 是否相同（同一 PlayableHandle）。</summary>
        public bool Equals(AnimatorControllerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // ================================================================
        // 参数控制（Parameter Control）
        // 每个参数都有两种重载：string 名称 和 int 哈希
        // 使用整数哈希性能更好（避免字符串解析）
        // ================================================================

        /// <summary>获取 float 类型参数的值。</summary>
        public float GetFloat(string name) { return GetFloatString(ref m_Handle, name); }
        /// <summary>通过哈希 ID 获取 float 参数值（更高效）。</summary>
        public float GetFloat(int id) { return GetFloatID(ref m_Handle, id); }

        /// <summary>设置 float 类型参数的值。</summary>
        public void SetFloat(string name, float value) { SetFloatString(ref m_Handle, name, value); }
        /// <summary>带阻尼的 float 参数设置（平滑过渡，适合速度等连续值）。</summary>
        public void SetFloat(string name, float value, float dampTime, float deltaTime) { SetFloatStringDamp(ref m_Handle, name, value, dampTime, deltaTime); }
        /// <summary>通过哈希 ID 设置 float 参数值。</summary>
        public void SetFloat(int id, float value) { SetFloatID(ref m_Handle, id, value); }
        /// <summary>通过哈希 ID 设置带阻尼的 float 参数。</summary>
        public void SetFloat(int id, float value, float dampTime, float deltaTime) { SetFloatIDDamp(ref m_Handle, id, value, dampTime, deltaTime); }

        /// <summary>获取 bool 类型参数的值。</summary>
        public bool GetBool(string name) { return GetBoolString(ref m_Handle, name); }
        /// <summary>通过哈希 ID 获取 bool 参数值。</summary>
        public bool GetBool(int id) { return GetBoolID(ref m_Handle, id); }
        /// <summary>设置 bool 类型参数的值。</summary>
        public void SetBool(string name, bool value) { SetBoolString(ref m_Handle, name, value); }
        /// <summary>通过哈希 ID 设置 bool 参数值。</summary>
        public void SetBool(int id, bool value) { SetBoolID(ref m_Handle, id, value); }

        /// <summary>获取 int 类型参数的值。</summary>
        public int GetInteger(string name) { return GetIntegerString(ref m_Handle, name); }
        /// <summary>通过哈希 ID 获取 int 参数值。</summary>
        public int GetInteger(int id) { return GetIntegerID(ref m_Handle, id); }
        /// <summary>设置 int 类型参数的值。</summary>
        public void SetInteger(string name, int value) { SetIntegerString(ref m_Handle, name, value); }
        /// <summary>通过哈希 ID 设置 int 参数值。</summary>
        public void SetInteger(int id, int value) { SetIntegerID(ref m_Handle, id, value); }

        /// <summary>激活 Trigger 参数（设置后自动重置）。</summary>
        public void SetTrigger(string name) { SetTriggerString(ref m_Handle, name); }
        /// <summary>通过哈希 ID 激活 Trigger 参数。</summary>
        public void SetTrigger(int id) { SetTriggerID(ref m_Handle, id); }
        /// <summary>重置 Trigger 参数。</summary>
        public void ResetTrigger(string name) { ResetTriggerString(ref m_Handle, name); }
        /// <summary>通过哈希 ID 重置 Trigger 参数。</summary>
        public void ResetTrigger(int id) { ResetTriggerID(ref m_Handle, id); }

        /// <summary>判断参数是否由动画曲线控制（而非脚本）。</summary>
        public bool IsParameterControlledByCurve(string name) { return IsParameterControlledByCurveString(ref m_Handle, name); }
        /// <summary>通过哈希 ID 判断参数是否由动画曲线控制。</summary>
        public bool IsParameterControlledByCurve(int id) { return IsParameterControlledByCurveID(ref m_Handle, id); }

        // ================================================================
        // 层管理（Layer Management）
        // ================================================================

        /// <summary>获取 Animator Controller 的层数量。</summary>
        public int GetLayerCount() { return GetLayerCountInternal(ref m_Handle); }
        /// <summary>获取指定层的名称。</summary>
        public string GetLayerName(int layerIndex) { return GetLayerNameInternal(ref m_Handle, layerIndex); }
        /// <summary>通过层名称获取层索引。</summary>
        public int GetLayerIndex(string layerName) { return GetLayerIndexInternal(ref m_Handle, layerName); }
        /// <summary>获取指定层的权重。</summary>
        public float GetLayerWeight(int layerIndex) { return GetLayerWeightInternal(ref m_Handle, layerIndex); }
        /// <summary>设置指定层的权重。</summary>
        public void SetLayerWeight(int layerIndex, float weight) { SetLayerWeightInternal(ref m_Handle, layerIndex, weight); }

        // ================================================================
        // 状态信息查询（State Info）
        // ================================================================

        /// <summary>获取当前状态的信息。</summary>
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex) { return GetCurrentAnimatorStateInfoInternal(ref m_Handle, layerIndex); }
        /// <summary>获取下一个状态的信息（过渡中时有效）。</summary>
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex) { return GetNextAnimatorStateInfoInternal(ref m_Handle, layerIndex); }
        /// <summary>获取过渡信息。</summary>
        public AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex) { return GetAnimatorTransitionInfoInternal(ref m_Handle, layerIndex); }

        // ================================================================
        // 剪辑信息（Clip Info）
        // ================================================================

        /// <summary>获取当前状态播放的剪辑信息数组。</summary>
        public AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex) { return GetCurrentAnimatorClipInfoInternal(ref m_Handle, layerIndex); }
        /// <summary>获取当前状态播放的剪辑信息列表（推荐，避免分配数组）。</summary>
        public void GetCurrentAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");
            GetAnimatorClipInfoInternal(ref m_Handle, layerIndex, true, clips);
        }
        /// <summary>获取下一个状态播放的剪辑信息列表。</summary>
        public void GetNextAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");
            GetAnimatorClipInfoInternal(ref m_Handle, layerIndex, false, clips);
        }
        /// <summary>获取当前状态播放的剪辑数量。</summary>
        public int GetCurrentAnimatorClipInfoCount(int layerIndex) { return GetAnimatorClipInfoCountInternal(ref m_Handle, layerIndex, true); }
        /// <summary>获取下一个状态播放的剪辑数量。</summary>
        public int GetNextAnimatorClipInfoCount(int layerIndex) { return GetAnimatorClipInfoCountInternal(ref m_Handle, layerIndex, false); }
        /// <summary>获取下一个状态播放的剪辑信息数组。</summary>
        public AnimatorClipInfo[] GetNextAnimatorClipInfo(int layerIndex) { return GetNextAnimatorClipInfoInternal(ref m_Handle, layerIndex); }

        /// <summary>是否正在过渡中。</summary>
        public bool IsInTransition(int layerIndex) { return IsInTransitionInternal(ref m_Handle, layerIndex); }

        /// <summary>获取参数总数。</summary>
        public int GetParameterCount() { return GetParameterCountInternal(ref m_Handle); }
        /// <summary>通过索引获取参数信息。</summary>
        public AnimatorControllerParameter GetParameter(int index)
        {
            var parameter = GetParameterInternal(ref m_Handle, index);
            if ((int)parameter.m_Type == AnimatorControllerParameterTypeConstants.InvalidType)
                throw new IndexOutOfRangeException("Invalid parameter index.");
            return parameter;
        }

        // ================================================================
        // 播放控制（Playback Control）
        // CrossFadeInFixedTime —— 按绝对时间淡入
        // CrossFade —— 按归一化时间淡入
        // PlayInFixedTime —— 按绝对时间播放
        // Play —— 按归一化时间播放
        // ================================================================

        /// <summary>按绝对时间淡入指定状态。</summary>
        public void CrossFadeInFixedTime(string stateName, float transitionDuration) { CrossFadeInFixedTimeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, -1, 0.0f); }
        public void CrossFadeInFixedTime(string stateName, float transitionDuration, int layer) { CrossFadeInFixedTimeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, 0.0f); }
        public void CrossFadeInFixedTime(string stateName, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("0.0f")] float fixedTime) { CrossFadeInFixedTimeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, fixedTime); }
        public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration) { CrossFadeInFixedTimeInternal(ref m_Handle, stateNameHash, transitionDuration, -1, 0.0f); }
        public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration, int layer) { CrossFadeInFixedTimeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, 0.0f); }
        public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("0.0f")] float fixedTime) { CrossFadeInFixedTimeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, fixedTime); }

        /// <summary>按归一化时间淡入指定状态。</summary>
        public void CrossFade(string stateName, float transitionDuration) { CrossFadeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, -1, float.NegativeInfinity); }
        public void CrossFade(string stateName, float transitionDuration, int layer) { CrossFadeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, float.NegativeInfinity); }
        public void CrossFade(string stateName, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime) { CrossFadeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, normalizedTime); }
        public void CrossFade(int stateNameHash, float transitionDuration) { CrossFadeInternal(ref m_Handle, stateNameHash, transitionDuration, -1, float.NegativeInfinity); }
        public void CrossFade(int stateNameHash, float transitionDuration, int layer) { CrossFadeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, float.NegativeInfinity); }
        public void CrossFade(int stateNameHash, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime) { CrossFadeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, normalizedTime); }

        /// <summary>按绝对时间播放指定状态。</summary>
        public void PlayInFixedTime(string stateName) { PlayInFixedTimeInternal(ref m_Handle, StringToHash(stateName), -1, float.NegativeInfinity); }
        public void PlayInFixedTime(string stateName, int layer) { PlayInFixedTimeInternal(ref m_Handle, StringToHash(stateName), layer, float.NegativeInfinity); }
        public void PlayInFixedTime(string stateName, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float fixedTime) { PlayInFixedTimeInternal(ref m_Handle, StringToHash(stateName), layer, fixedTime); }
        public void PlayInFixedTime(int stateNameHash) { PlayInFixedTimeInternal(ref m_Handle, stateNameHash, -1, float.NegativeInfinity); }
        public void PlayInFixedTime(int stateNameHash, int layer) { PlayInFixedTimeInternal(ref m_Handle, stateNameHash, layer, float.NegativeInfinity); }
        public void PlayInFixedTime(int stateNameHash, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float fixedTime) { PlayInFixedTimeInternal(ref m_Handle, stateNameHash, layer, fixedTime); }

        /// <summary>按归一化时间播放指定状态。</summary>
        public void Play(string stateName) { PlayInternal(ref m_Handle, StringToHash(stateName), -1, float.NegativeInfinity); }
        public void Play(string stateName, int layer) { PlayInternal(ref m_Handle, StringToHash(stateName), layer, float.NegativeInfinity); }
        public void Play(string stateName, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime) { PlayInternal(ref m_Handle, StringToHash(stateName), layer, normalizedTime); }
        public void Play(int stateNameHash) { PlayInternal(ref m_Handle, stateNameHash, -1, float.NegativeInfinity); }
        public void Play(int stateNameHash, int layer) { PlayInternal(ref m_Handle, stateNameHash, layer, float.NegativeInfinity); }
        public void Play(int stateNameHash, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime) { PlayInternal(ref m_Handle, stateNameHash, layer, normalizedTime); }

        /// <summary>重置控制器状态（可选重置参数为默认值）。</summary>
        public void ResetControllerState([UnityEngine.Internal.DefaultValue("true")] bool resetParameters = true) { ResetControllerStateInternal(ref m_Handle, resetParameters); }

        /// <summary>指定层中是否存在特定状态。</summary>
        public bool HasState(int layerIndex, int stateID) { return HasStateInternal(ref m_Handle, layerIndex, stateID); }

        /// <summary>内部 — 将哈希值解析为状态名称字符串。</summary>
        internal string ResolveHash(int hash) { return ResolveHashInternal(ref m_Handle, hash); }

        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateHandleInternal(PlayableGraph graph, RuntimeAnimatorController controller, ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static RuntimeAnimatorController GetAnimatorControllerInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetLayerCountInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static string GetLayerNameInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetLayerIndexInternal(ref PlayableHandle handle, string layerName);

        [NativeMethod(ThrowsException = true)]
        extern private static float GetLayerWeightInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetLayerWeightInternal(ref PlayableHandle handle,  int layerIndex, float weight);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorStateInfo GetCurrentAnimatorStateInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorStateInfo GetNextAnimatorStateInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorTransitionInfo GetAnimatorTransitionInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorClipInfo[] GetCurrentAnimatorClipInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetAnimatorClipInfoCountInternal(ref PlayableHandle handle, int layerIndex, bool current);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorClipInfo[] GetNextAnimatorClipInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static string ResolveHashInternal(ref PlayableHandle handle, int hash);

        [NativeMethod(ThrowsException = true)]
        extern private static bool IsInTransitionInternal(ref PlayableHandle handle, int layerIndex);
        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorControllerParameter GetParameterInternal(ref PlayableHandle handle, int index);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetParameterCountInternal(ref PlayableHandle handle);

        [NativeMethod(IsThreadSafe = true)]
        extern private static int StringToHash(string name);

        [NativeMethod(ThrowsException = true)]
        extern private static void CrossFadeInFixedTimeInternal(ref PlayableHandle handle, int stateNameHash, float transitionDuration, int layer, float fixedTime);


        [NativeMethod(ThrowsException = true)]
        extern private static void CrossFadeInternal(ref PlayableHandle handle, int stateNameHash, float transitionDuration, int layer, float normalizedTime);

        [NativeMethod(ThrowsException = true)]
        extern private static void PlayInFixedTimeInternal(ref PlayableHandle handle, int stateNameHash, int layer, float fixedTime);

        [NativeMethod(ThrowsException = true)]
        extern private static void PlayInternal(ref PlayableHandle handle, int stateNameHash, int layer, float normalizedTime);

        [NativeMethod(ThrowsException = true)]
        extern private static void ResetControllerStateInternal(ref PlayableHandle handle, bool resetParameters);

        [NativeMethod(ThrowsException = true)]
        extern private static bool HasStateInternal(ref PlayableHandle handle, int layerIndex, int stateID);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetFloatString(ref PlayableHandle handle, string name, float value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetFloatID(ref PlayableHandle handle, int id, float value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetFloatStringDamp(ref PlayableHandle handle, string name, float value, float dampTime, float deltaTime);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetFloatIDDamp(ref PlayableHandle handle, int id, float value, float dampTime, float deltaTime);

        [NativeMethod(ThrowsException = true)]
        extern private static float GetFloatString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static float GetFloatID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetBoolString(ref PlayableHandle handle, string name, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetBoolID(ref PlayableHandle handle, int id, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetBoolString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetBoolID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetIntegerString(ref PlayableHandle handle, string name, int value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetIntegerID(ref PlayableHandle handle, int id, int value);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetIntegerString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetIntegerID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetTriggerString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetTriggerID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static void ResetTriggerString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static void ResetTriggerID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static bool IsParameterControlledByCurveString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static bool IsParameterControlledByCurveID(ref PlayableHandle handle, int id);
    }
}
