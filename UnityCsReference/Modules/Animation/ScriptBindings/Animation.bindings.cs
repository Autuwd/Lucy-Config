// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// 旧版 Animation 组件（Legacy Animation System）
//
// 【概述】
// 这是 Unity 早期的动画系统组件，在 Mecanim 系统引入之前使用。
// 每个 Animation 组件可以管理多个 AnimationState（动画状态），
// 每个状态关联一个 AnimationClip，并可以设置权重、混合等。
//
// 【与 Mecanim (Animator) 的区别】
// 1. Animation 组件无状态机，通过脚本直接控制播放哪个动画
// 2. 不支持 Avatar / IK / Root Motion 等高级功能
// 3. 使用 AnimationState 而非 AnimatorStateInfo
// 4. 更适合简单对象（门、灯光、UI 元素）而非复杂角色
// 5. 支持队列播放（PlayQueued / CrossFadeQueued）
//
// 【何时使用】
// - 简单对象的动画（门开、灯闪烁）
// - 不需要状态机逻辑的场合
// - 维护旧项目
//
// 【注意】
// 此组件标记为 [NativeHeader] 由 C++ 核心实现，
// C# 侧只是绑定层。新项目建议使用 Animator 组件。
// ============================================================

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    /// <summary>
    /// 播放模式枚举。
    /// 用于 Animation.Play 方法，控制播放新动画时如何处理已有的动画。
    /// </summary>
    public enum PlayMode
    {
        /// <summary>停止同一层的所有动画。这是默认行为。</summary>
        StopSameLayer = 0,
        /// <summary>停止此组件上所有已启动的动画。</summary>
        StopAll = 4,
    }

    /// <summary>
    /// 队列模式枚举。
    /// 用于 Animation.PlayQueued / CrossFadeQueued 方法。
    /// </summary>
    public enum QueueMode
    {
        /// <summary>等待其他所有动画播放完毕后再开始播放。</summary>
        CompleteOthers = 0,
        /// <summary>立即开始播放。</summary>
        PlayNow = 2
    }

    /// <summary>
    /// 动画混合模式枚举。
    /// </summary>
    public enum AnimationBlendMode
    {
        /// <summary>标准混合（权重叠加）。</summary>
        Blend = 0,
        /// <summary>叠加模式（动画在现有基础上附加）。适合附加动作如呼吸、摇摆。</summary>
        Additive = 1
    }

    /// <summary>（已弃用）旧的动画播放模式枚举。</summary>
    public enum AnimationPlayMode { Stop = 0, Queue = 1, Mix = 2 }

    /// <summary>
    /// 旧版 Animation 组件的裁剪类型枚举。
    /// 控制组件在渲染器不可见时的行为。
    /// </summary>
    public enum AnimationCullingType
    {
        /// <summary>禁用裁剪 — 即使屏幕外也更新动画。</summary>
        AlwaysAnimate = 0,
        /// <summary>当渲染器不可见时禁用动画。</summary>
        BasedOnRenderers = 1,

        [System.Obsolete("Enum member AnimatorCullingMode.BasedOnClipBounds has been deprecated. Use AnimationCullingType.AlwaysAnimate or AnimationCullingType.BasedOnRenderers instead")]
        BasedOnClipBounds = 2,
        [System.Obsolete("Enum member AnimatorCullingMode.BasedOnUserBounds has been deprecated. Use AnimationCullingType.AlwaysAnimate or AnimationCullingType.BasedOnRenderers instead")]
        BasedOnUserBounds = 3
    }

    /// <summary>
    /// 旧版 Animation 组件的更新模式枚举。
    /// </summary>
    public enum AnimationUpdateMode
    {
        /// <summary>在 Update 中更新（受 Time.timeScale 影响）。</summary>
        Normal = 0,
        /// <summary>在 FixedUpdate 中更新（适合与物理交互）。</summary>
        Fixed = 1
    }

    /// <summary>
    /// 动画事件来源枚举（内部使用）。
    /// 区分事件是由旧版 Animation 还是新版 Animator 触发的。
    /// </summary>
    internal enum AnimationEventSource
    {
        NoSource = 0,
        Legacy = 1,
        Animator = 2,
    }

    /// <summary>
    /// Animation 组件 — 旧版动画系统的核心组件。
    ///
    /// 每个 Animation 组件可以包含多个 AnimationClip，通过名称引用播放。
    /// 支持播放、淡入淡出、队列播放、混合等功能。
    ///
    /// 【使用示例】
    /// Animation anim = GetComponent&lt;Animation&gt;();
    /// anim.Play("idle");
    /// anim.CrossFade("walk", 0.3f);
    /// anim["walk"].speed = 2.0f;
    /// </summary>
    [NativeHeader("Modules/Animation/Animation.h")]
    public sealed class Animation : Behaviour, IEnumerable
    {
        /// <summary>默认动画剪辑。Play() 不带参数时播放此剪辑。</summary>
        public extern AnimationClip clip { get; set; }
        /// <summary>是否在 Awake 后自动播放 clip。</summary>
        public extern bool playAutomatically { get; set; }
        /// <summary>默认循环模式。</summary>
        public extern WrapMode wrapMode { get; set; }

        /// <summary>停止所有动画。</summary>
        public extern void Stop();
        /// <summary>停止指定名称的动画。</summary>
        public void Stop(string name) { StopNamed(name); }
        [NativeName("Stop")] private extern void StopNamed(string name);
        /// <summary>倒带到起始位置。</summary>
        public extern void Rewind();
        /// <summary>倒带指定名称的动画。</summary>
        public void Rewind(string name) { RewindNamed(name); }
        [NativeName("Rewind")] private extern void RewindNamed(string name);

        /// <summary>采样动画到当前时间（不驱动播放）。</summary>
        public extern void Sample();
        /// <summary>是否有动画正在播放。</summary>
        public extern bool isPlaying { [NativeName("IsPlaying")] get; }
        /// <summary>指定名称的动画是否在播放。</summary>
        public extern bool IsPlaying(string name);

        /// <summary>通过名称访问 AnimationState（索引器）。例如 animation["walk"].speed = 2.0f。</summary>
        public AnimationState this[string name] { get { return GetState(name); } }

        /// <summary>播放默认动画（StopSameLayer 模式）。</summary>
        [uei.ExcludeFromDocs] public bool Play() { return Play(PlayMode.StopSameLayer); }
        /// <summary>
        /// 播放默认动画（可指定 PlayMode）。
        /// 内部调用 PlayDefaultAnimation -> C++ Play(Animation*)
        /// </summary>
        public bool Play([uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode) { return PlayDefaultAnimation(mode); }
        [NativeName("Play")] extern private bool PlayDefaultAnimation(PlayMode mode);

        /// <summary>按名称播放动画。</summary>
        [uei.ExcludeFromDocs] public bool Play(string animation) { return Play(animation, PlayMode.StopSameLayer); }
        extern public bool Play(string animation, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        /// <summary>淡入动画（默认淡入时长 0.3s）。</summary>
        [uei.ExcludeFromDocs] public void CrossFade(string animation) { CrossFade(animation, 0.3f); }
        [uei.ExcludeFromDocs] public void CrossFade(string animation, float fadeLength) { CrossFade(animation, fadeLength, PlayMode.StopSameLayer); }
        extern public void CrossFade(string animation, [uei.DefaultValue("0.3F")] float fadeLength, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        /// <summary>将动画混合到指定权重。</summary>
        [uei.ExcludeFromDocs] public void Blend(string animation) { Blend(animation, 1.0f); }
        [uei.ExcludeFromDocs] public void Blend(string animation, float targetWeight) { Blend(animation, targetWeight, 0.3f); }
        extern public void Blend(string animation, [uei.DefaultValue("1.0F")] float targetWeight, [uei.DefaultValue("0.3F")] float fadeLength);

        /// <summary>队列淡入 — 当前动画播放完毕后自动淡入指定动画。</summary>
        [uei.ExcludeFromDocs] public AnimationState CrossFadeQueued(string animation) { return CrossFadeQueued(animation, 0.3F); }
        [uei.ExcludeFromDocs] public AnimationState CrossFadeQueued(string animation, float fadeLength) { return CrossFadeQueued(animation, fadeLength, QueueMode.CompleteOthers); }
        [uei.ExcludeFromDocs] public AnimationState CrossFadeQueued(string animation, float fadeLength, QueueMode queue) { return CrossFadeQueued(animation, queue, PlayMode.StopSameLayer); }
        [FreeFunction("AnimationBindings::CrossFadeQueuedImpl", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public AnimationState CrossFadeQueued(string animation, [uei.DefaultValue("0.3F")] float fadeLength, [uei.DefaultValue("QueueMode.CompleteOthers")] QueueMode queue, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        /// <summary>队列播放 — 当前动画播放完毕后自动播放指定动画。</summary>
        [uei.ExcludeFromDocs] public AnimationState PlayQueued(string animation) { return PlayQueued(animation, QueueMode.CompleteOthers); }
        [uei.ExcludeFromDocs] public AnimationState PlayQueued(string animation, QueueMode queue) { return PlayQueued(animation, queue, PlayMode.StopSameLayer); }
        [FreeFunction("AnimationBindings::PlayQueuedImpl", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public AnimationState PlayQueued(string animation, [uei.DefaultValue("QueueMode.CompleteOthers")] QueueMode queue, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        /// <summary>添加剪辑并重命名。可指定帧范围。</summary>
        public void AddClip(AnimationClip clip, string newName) { AddClip(clip, newName, Int32.MinValue, Int32.MaxValue); }
        [uei.ExcludeFromDocs] public void AddClip(AnimationClip clip, string newName, int firstFrame, int lastFrame) { AddClip(clip, newName, firstFrame, lastFrame, false); }
        extern public void AddClip([NotNull] AnimationClip clip, string newName, int firstFrame, int lastFrame, [uei.DefaultValue("false")] bool addLoopFrame);

        /// <summary>移除指定剪辑引用。</summary>
        extern public void RemoveClip([NotNull] AnimationClip clip);

        /// <summary>按名称移除剪辑。</summary>
        public void RemoveClip(string clipName) { RemoveClipNamed(clipName); }
        [NativeName("RemoveClip")] extern private void RemoveClipNamed(string clipName);

        /// <summary>获取已添加的剪辑数量。</summary>
        extern public int GetClipCount();

        [System.Obsolete("use PlayMode instead of AnimationPlayMode.")]
        public bool Play(AnimationPlayMode mode) { return PlayDefaultAnimation((PlayMode)mode); }
        [System.Obsolete("use PlayMode instead of AnimationPlayMode.")]
        public bool Play(string animation, AnimationPlayMode mode) { return Play(animation, (PlayMode)mode); }

        /// <summary>同步指定层的动画时间（用于多动画同步）。</summary>
        extern public void SyncLayer(int layer);

        /// <summary>获取枚举器，支持 foreach 遍历所有 AnimationState。</summary>
        public IEnumerator GetEnumerator() { return new Animation.Enumerator(this); }

        /// <summary>内部枚举器 — 遍历所有 AnimationState。</summary>
        private sealed partial class Enumerator : IEnumerator
        {
            Animation m_Outer;
            int m_CurrentIndex = -1;

            internal Enumerator(Animation outer) { m_Outer = outer; }
            public object Current
            {
                get { return m_Outer.GetStateAtIndex(m_CurrentIndex); }
            }
            public bool MoveNext()
            {
                int childCount = m_Outer.GetStateCount();
                m_CurrentIndex++;
                return m_CurrentIndex < childCount;
            }

            public void Reset() { m_CurrentIndex = -1; }
        }

        /// <summary>按名称获取 AnimationState（内部）。</summary>
        [FreeFunction("AnimationBindings::GetState", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal AnimationState GetState(string name);

        /// <summary>按索引获取 AnimationState（内部）。</summary>
        [FreeFunction("AnimationBindings::GetStateAtIndex", HasExplicitThis = true, ThrowsException = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal AnimationState GetStateAtIndex(int index);

        /// <summary>获取 AnimationState 总数（内部）。</summary>
        [NativeName("GetAnimationStateCount")] extern internal int GetStateCount();

        /// <summary>按名称获取 AnimationClip。</summary>
        public AnimationClip GetClip(string name)
        {
            AnimationState state = GetState(name);
            if (state)
                return state.clip;
            else
                return null;
        }

        /// <summary>[已弃用] 是否在 FixedUpdate 中更新动画（用 updateMode 替代）。</summary>
        extern public bool animatePhysics { get; set; }

        /// <summary>更新模式：Normal（Update）或 Fixed（FixedUpdate）。</summary>
        extern public AnimationUpdateMode updateMode { get; set; }

        [System.Obsolete("Use cullingType instead")]
        public extern bool animateOnlyIfVisible
        {
            [FreeFunction("AnimationBindings::GetAnimateOnlyIfVisible", HasExplicitThis = true)]
            get;
            [FreeFunction("AnimationBindings::SetAnimateOnlyIfVisible", HasExplicitThis = true)]
            set;
        }

        /// <summary>裁剪类型 — 控制屏幕外时是否更新动画。</summary>
        extern public AnimationCullingType cullingType { get; set; }
        /// <summary>局部空间包围盒（用于裁剪计算）。</summary>
        extern public Bounds localBounds { [NativeName("GetLocalAABB")] get; [NativeName("SetLocalAABB")] set; }
    }

    /// <summary>
    /// AnimationState —— 旧版动画系统的单个动画状态。
    ///
    /// 每个 AnimationState 关联一个 AnimationClip，控制其播放参数（速度、权重、混合等）。
    /// 通过 Animation["name"] 获取。
    ///
    /// 【注意】
    /// 继承自 TrackedReference（非 MonoBehaviour），不被 GC 跟踪，需手动管理生命周期。
    /// </summary>
    [NativeHeader("Modules/Animation/AnimationState.h")]
    [UsedByNativeCode]
    public sealed class AnimationState : TrackedReference
    {
        /// <summary>是否启用此动画状态。</summary>
        extern public bool enabled { get; set; }
        /// <summary>混合权重（0-1）。多个动画同时播放时控制影响程度。</summary>
        extern public float weight { get; set; }
        /// <summary>循环模式。</summary>
        extern public WrapMode wrapMode { get; set; }
        /// <summary>当前时间（秒）。</summary>
        extern public float time { get; set; }
        /// <summary>归一化时间（0~1，循环模式下会超过1）。</summary>
        extern public float normalizedTime { get; set; }
        /// <summary>播放速度倍率。负值可倒放。</summary>
        extern public float speed { get; set; }
        /// <summary>归一化速度（基于剪辑时长）。</summary>
        extern public float normalizedSpeed { get; set; }
        /// <summary>剪辑长度（秒，只读）。</summary>
        extern public float length { get; }
        /// <summary>动画层索引。</summary>
        extern public int layer { get; set; }
        /// <summary>关联的 AnimationClip（只读）。</summary>
        extern public AnimationClip clip { get; }
        /// <summary>状态名称。</summary>
        extern public string name { get; set; }
        /// <summary>混合模式：标准叠加（Blend）或增量叠加（Additive）。</summary>
        extern public AnimationBlendMode blendMode { get; set; }

        /// <summary>添加混合变换 — 将此动画限制到指定骨骼上（无参数时递归到所有子骨骼）。</summary>
        [uei.ExcludeFromDocs] public void AddMixingTransform(Transform mix) { AddMixingTransform(mix, true); }
        extern public void AddMixingTransform([NotNull] Transform mix, [uei.DefaultValue("true")] bool recursive);

        /// <summary>移除混合变换。</summary>
        extern public void RemoveMixingTransform([NotNull] Transform mix);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(AnimationState animationState) => animationState.m_Ptr;
        }
    }

    /// <summary>
    /// AnimationEventInfo —— 动画事件的只读快照结构体（ref struct）。
    ///
    /// 当动画事件被触发时，C++ 侧传递此结构体给 C# 回调。
    /// 用于 AnimationEvent 回调函数的参数。
    ///
    /// 【与 AnimationEvent 的区别】
    /// - AnimationEventInfo 是只读 ref struct（栈上分配，无 GC 压力）
    /// - AnimationEvent 是可写的 class（序列化存储用）
    /// - AnimationEventInfo 在回调中使用更高效
    /// </summary>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public ref struct AnimationEventInfo
    {
        private IntPtr m_EventPtr;       // 指向 C++ 侧 AnimationEvent 对象
        private float m_Time;            // 事件触发时间
        private float m_FloatParameter;  // float 参数
        private int m_IntParameter;      // int 参数

        private int m_MessageOptions;                   // SendMessageOptions
        private AnimationEventSource m_Source;          // 触发来源
        private AnimatorStateInfo m_AnimatorStateInfo;  // 触发时的状态信息（仅 Animator）
        private AnimatorClipInfo m_AnimatorClipInfo;    // 触发时的剪辑信息（仅 Animator）
        
        /// <summary>字符串参数（从 C++ 侧获取）。</summary>
        public string stringParameter => GetStringParameterInternal(m_EventPtr);
        /// <summary>float 参数。</summary>
        public float floatParameter => m_FloatParameter;
        /// <summary>int 参数。</summary>
        public int intParameter => m_IntParameter;
        /// <summary>Object 参数（从 C++ 侧获取）。</summary>
        public Object objectReferenceParameter => GetObjectReferenceParameterInternal(m_EventPtr);
        /// <summary>回调函数名称（从 C++ 侧获取）。</summary>
        public string functionName => GetFunctionNameParameter(m_EventPtr);
        /// <summary>事件触发时间（秒）。</summary>
        public float time => m_Time;

        /// <summary>是否由旧版 Animation 组件触发。</summary>
        public bool isFiredByLegacy => m_Source == AnimationEventSource.Legacy;
        /// <summary>是否由 Mecanim Animator 组件触发。</summary>
        public bool isFiredByAnimator => m_Source == AnimationEventSource.Animator;

        /// <summary>获取触发事件的 AnimationState（仅旧版 Animation 可用）。</summary>
        public AnimationState animationState
        {
            get
            {
                if (!isFiredByLegacy)
                    Debug.LogError("AnimationEvent was not fired by Animation component, you shouldn't use AnimationEvent.animationState");


                return GetStateSenderInternal(m_EventPtr);
            }
        }

        [FreeFunction("AnimationBindings::GetEventStringParameter")]
        extern static string GetStringParameterInternal(IntPtr eventPtr);


        [FreeFunction("AnimationBindings::GetEventFunctionName")]
        extern static string GetFunctionNameParameter(IntPtr eventPtr);


        [FreeFunction("AnimationBindings::GetEventObjectReferenceParameter")]
        extern static Object GetObjectReferenceParameterInternal(IntPtr eventPtr);

        [FreeFunction("AnimationBindings::GetEventAnimationState")]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern static AnimationState GetStateSenderInternal(IntPtr eventPtr);

        /// <summary>获取触发时的 Animator 状态信息（仅 Mecanim 可用）。</summary>
        public AnimatorStateInfo animatorStateInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorStateInfo");
                return m_AnimatorStateInfo;
            }
        }

        /// <summary>获取触发时的 Animator 剪辑信息（仅 Mecanim 可用）。</summary>
        public AnimatorClipInfo animatorClipInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorClipInfo");
                return m_AnimatorClipInfo;
            }
        }

       
    }

    /// <summary>
    /// AnimationEvent —— 可序列化的动画事件类。
    ///
    /// 在动画剪辑的时间轴上添加的事件标记。播放到指定时间时，
    /// Unity 会调用指定 GameObject 上挂载的脚本方法。
    ///
    /// 【使用方式】
    /// 1. 在 AnimationClip 的 Inspector 中添加事件
    /// 2. 或通过脚本动态添加：AnimationEvent evt = new AnimationEvent();
    ///    evt.time = 1.5f;
    ///    evt.functionName = "OnFootstep";
    ///    clip.AddEvent(evt);
    ///
    /// 【数据结构】
    /// 序列化时包含：时间、方法名、最多 4 个参数（string/float/int/Object）。
    /// 触发后会附带触发上下文（AnimationState / AnimatorStateInfo / AnimatorClipInfo）。
    /// </summary>
    [System.Serializable]
    [RequiredByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Animation/AnimationEvent.h")]
    public sealed class AnimationEvent
    {
        [NativeName("time")]
        internal float m_Time;
        [NativeName("functionName")]
        internal string m_FunctionName;
        [NativeName("stringParameter")]
        internal string m_StringParameter;
        [NativeName("objectReferenceParameter")]
        internal Object m_ObjectReferenceParameter;
        [NativeName("floatParameter")]
        internal float m_FloatParameter;
        [NativeName("intParameter")]
        internal int m_IntParameter;

        [NativeName("messageOptions")]
        internal int m_MessageOptions;
        [NativeName("source")]
        internal AnimationEventSource m_Source;
        [NativeName("stateSender")]
        [UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        internal AnimationState m_StateSender;
        [NativeName("animatorStateInfo")]
        internal AnimatorStateInfo m_AnimatorStateInfo;
        [NativeName("animatorClipInfo")]
        internal AnimatorClipInfo m_AnimatorClipInfo;

        /// <summary>创建 AnimationEvent 实例，默认值为空。</summary>
        public AnimationEvent()
        {
            m_Time = 0.0f;
            m_FunctionName = "";
            m_StringParameter = "";
            m_ObjectReferenceParameter = null;
            m_FloatParameter = 0.0f;
            m_IntParameter = 0;
            m_MessageOptions = 0;
            m_Source = AnimationEventSource.NoSource;
            m_StateSender = null;
        }

        [System.Obsolete("Use stringParameter instead")]
        public string data { get { return m_StringParameter; } set { m_StringParameter = value; } }

        /// <summary>字符串参数。</summary>
        public string stringParameter { get { return m_StringParameter; } set { m_StringParameter = value; } }
        /// <summary>float 参数。</summary>
        public float floatParameter { get { return m_FloatParameter; } set { m_FloatParameter = value; } }
        /// <summary>int 参数。</summary>
        public int intParameter { get { return m_IntParameter; } set { m_IntParameter = value; } }
        /// <summary>Object 参数（如 AudioClip、AnimationClip 等资源引用）。</summary>
        public Object objectReferenceParameter { get { return m_ObjectReferenceParameter; } set { m_ObjectReferenceParameter = value; } }
        /// <summary>回调函数名称。播放到此时间时，Unity 会在挂载此组件的 GameObject 上调用此方法。</summary>
        public string functionName { get { return m_FunctionName; } set { m_FunctionName = value; } }
        /// <summary>事件触发时间（秒），位于动画剪辑的时间线上。</summary>
        public float time { get { return m_Time; } set { m_Time = value; } }
        /// <summary>消息发送选项（是否需要接收方在脚本中实现该方法）。</summary>
        public SendMessageOptions messageOptions { get { return (SendMessageOptions)m_MessageOptions; } set { m_MessageOptions = (int)value; } }

        /// <summary>此事件是否由旧版 Animation 组件触发。</summary>
        public bool isFiredByLegacy { get { return m_Source == AnimationEventSource.Legacy; } }
        /// <summary>此事件是否由新版 Animator 组件触发。</summary>
        public bool isFiredByAnimator { get { return m_Source == AnimationEventSource.Animator; } }

        /// <summary>获取触发此事件的 AnimationState（仅旧版系统可用）。</summary>
        public AnimationState animationState
        {
            get
            {
                if (!isFiredByLegacy)
                    Debug.LogError("AnimationEvent was not fired by Animation component, you shouldn't use AnimationEvent.animationState");
                return m_StateSender;
            }
        }

        /// <summary>获取触发此事件的 Animator 状态信息（仅 Mecanim 系统可用）。</summary>
        public AnimatorStateInfo animatorStateInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorStateInfo");
                return m_AnimatorStateInfo;
            }
        }

        /// <summary>获取触发此事件的 Animator 剪辑信息（仅 Mecanim 系统可用）。</summary>
        public AnimatorClipInfo animatorClipInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorClipInfo");
                return m_AnimatorClipInfo;
            }
        }

        /// <summary>计算哈希值（用于事件去重/比较）。</summary>
        internal int GetHash()
        {
            unchecked
            {
                int hash = 0;
                hash = functionName.GetHashCode();
                hash = 33 * hash + time.GetHashCode();
                return hash;
            }
        }

        /// <summary>由 C++ 侧调用的工厂方法 — 创建事件实例。</summary>
        [RequiredByNativeCode]
        internal static AnimationEvent CreateAnimationEvent(
            float time,
            string functionName,
            string stringParameter,
            Object objectReferenceParameter,
            float floatParameter,
            int intParameter,
            int messageOptions,
            AnimationEventSource source,
            AnimationState stateSender,
            AnimatorStateInfo animatorStateInfo,
            AnimatorClipInfo animatorClipInfo)
        {
            return new AnimationEvent
            {
                m_Time = time,
                m_FunctionName = functionName,
                m_StringParameter = stringParameter,
                m_ObjectReferenceParameter = objectReferenceParameter,
                m_FloatParameter = floatParameter,
                m_IntParameter = intParameter,
                m_MessageOptions = messageOptions,
                m_Source = source,
                m_StateSender = stateSender,
                m_AnimatorStateInfo = animatorStateInfo,
                m_AnimatorClipInfo = animatorClipInfo
            };
        }
    }
}
