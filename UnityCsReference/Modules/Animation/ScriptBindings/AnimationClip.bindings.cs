// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AnimationClip —— 动画剪辑资源
//
// 【概述】
// AnimationClip 是 Unity 中存储关键帧动画的核心资源类型。
// 它继承自 Motion，是 Animator 和 Animation 组件播放动画的数据源。
// 每个剪辑包含一组 Curve（曲线），驱动对象的属性变化。
//
// 【数据结构】
// - 位置/旋转/缩放曲线（Transform 动画）
// - 普通属性曲线（float、Color、Vector4 等）
// - 事件标记（AnimationEvent）
// - Root Motion 数据（Mecanim）
//
// 【生命周期】
// 1. 设计时：在 Unity Editor 中创建和编辑（或导入的 FBX 自动生成）
// 2. 构建时：序列化为资源文件（.anim 或包含在 FBX 中）
// 3. 运行时：加载到内存后由 Animation / Animator 播放
//
// 【注意】
// AnimationClip 是一个资源（Asset），在场景中可被多个对象引用，
// 属于只读数据，运行时可通过 SetCurve/AddEvent 动态修改。
// ============================================================

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
namespace UnityEngine
{
    /// <summary>
    /// AnimationClip —— 存储关键帧动画的剪辑资源。
    /// 继承自 Motion，包含驱动对象属性的曲线数据和事件标记。
    /// </summary>
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationClip.bindings.h")]
    [NativeHeader("Modules/Animation/AnimationClip.h")]
    public sealed class AnimationClip : Motion
    {
        /// <summary>创建空的 AnimationClip 实例。</summary>
        public AnimationClip()
        {
            Internal_CreateAnimationClip(this);
        }

        [FreeFunction("AnimationClipBindings::Internal_CreateAnimationClip")]
        extern private static void Internal_CreateAnimationClip([Writable] AnimationClip self);

        /// <summary>
        /// 在指定 GameObject 上采样此动画剪辑（快照模式）。
        /// 直接将动画应用到对象属性上，不经过 Animation/Animator 组件。
        /// 此方法放在此处是为了避免 GameObject 或 Core 依赖 Animation 模块。
        /// </summary>
        /// <param name="go">目标 GameObject</param>
        /// <param name="time">采样时间点（秒）</param>
        public void SampleAnimation(GameObject go, float time)
        {
            SampleAnimation(go, this, time, this.wrapMode);
        }

        [NativeHeader("Modules/Animation/AnimationUtility.h")]
        [FreeFunction]
        extern internal static void SampleAnimation([NotNull] GameObject go, [NotNull] AnimationClip clip, float inTime, WrapMode wrapMode);

        /// <summary>剪辑时长（秒，只读）。</summary>
        [NativeProperty("Length", false, TargetType.Function)]
        public extern float length { get; }

        /// <summary>剪辑的起始时间（内部使用）。</summary>
        [NativeProperty("StartTime", false, TargetType.Function)]
        internal extern float startTime { get; }

        /// <summary>剪辑的结束时间（内部使用）。</summary>
        [NativeProperty("StopTime", false, TargetType.Function)]
        internal extern float stopTime { get; }

        /// <summary>帧率 — 关键帧采样的速率。默认 60 FPS。</summary>
        [NativeProperty("SampleRate", false, TargetType.Function)]
        public extern float frameRate { get; set; }

        /// <summary>
        /// 设置属性曲线。
        /// 可以通过此方法在运行时动态修改动画剪辑。
        /// </summary>
        /// <param name="relativePath">相对于根对象的路径（空字符串表示对象自身）</param>
        /// <param name="type">组件类型</param>
        /// <param name="propertyName">属性名称（如 "m_LocalPosition.x"）</param>
        /// <param name="curve">动画曲线</param>
        [FreeFunction("AnimationClipBindings::Internal_SetCurve", HasExplicitThis = true)]
        public extern void SetCurve([NotNull] string relativePath, [NotNull] Type type, [NotNull] string propertyName, AnimationCurve curve);

        /// <summary>确保四元数旋转连续性（解决万向锁/插值问题）。</summary>
        public extern void EnsureQuaternionContinuity();

        /// <summary>清除剪辑中的所有曲线。</summary>
        public extern void ClearCurves();

        /// <summary>默认包裹模式（循环、往复等）。</summary>
        [NativeProperty("WrapMode", false, TargetType.Function)]
        public extern WrapMode wrapMode { get; set; }

        /// <summary>剪辑在 Animation 组件局部空间中的包围盒。</summary>
        [NativeProperty("Bounds", false, TargetType.Function)]
        public extern Bounds localBounds { get; set; }

        /// <summary>是否为旧版动画剪辑（Legacy）。</summary>
        extern public new bool legacy
        {
            [NativeMethod("IsLegacy")]
            get;
            [NativeMethod("SetLegacy")]
            set;
        }

        /// <summary>是否为人形动画剪辑。</summary>
        extern public bool humanMotion
        {
            [NativeMethod("IsHumanMotion")]
            get;
        }

        /// <summary>剪辑是否为空（没有任何曲线）。</summary>
        extern public bool empty
        {
            [NativeMethod("IsEmpty")]
            get;
        }

        /// <summary>是否包含通用根变换曲线（非人形角色的位置/旋转/缩放）。</summary>
        extern public bool hasGenericRootTransform
        {
            [NativeMethod("HasGenericRootTransform")]
            get;
        }

        /// <summary>是否包含运动浮点曲线（Additive 动画相关）。</summary>
        extern public bool hasMotionFloatCurves
        {
            [NativeMethod("HasMotionFloatCurves")]
            get;
        }

        /// <summary>是否包含运动曲线（根运动 + 动画曲线）。</summary>
        extern public bool hasMotionCurves
        {
            [NativeMethod("HasMotionCurves")]
            get;
        }

        /// <summary>是否包含根运动曲线。</summary>
        extern public bool hasRootCurves
        {
            [NativeMethod("HasRootCurves")]
            get;
        }

        /// <summary>内部 — 是否包含根运动数据（用于 Mecanim 的 Root Motion）。</summary>
        internal extern bool hasRootMotion
        {
            [FreeFunction(Name = "AnimationClipBindings::Internal_GetHasRootMotion", HasExplicitThis = true)]
            get;
        }

        /// <summary>添加动画事件。</summary>
        /// <param name="evt">事件对象（包含时间、函数名、参数）</param>
        public void AddEvent(AnimationEvent evt)
        {
            if (evt == null)
                throw new ArgumentNullException("evt");
            AddEventInternal(evt);
        }

        [FreeFunction(Name = "AnimationClipBindings::AddEventInternal", HasExplicitThis = true)]
        extern private void AddEventInternal([NotNull] AnimationEvent evt);

        /// <summary>获取或设置剪辑关联的所有动画事件。</summary>
        public AnimationEvent[] events
        {
            get => GetEventsInternal();
            set => SetEventsInternal(value);
        }
        [FreeFunction(Name = "AnimationClipBindings::SetEventsInternal", HasExplicitThis = true)]
        extern private void SetEventsInternal(AnimationEvent[] events);
        [FreeFunction(Name = "AnimationClipBindings::GetEventsInternal", HasExplicitThis = true)]
        extern private AnimationEvent[] GetEventsInternal();
    }

    unsafe class GCHandlePool
    {
        GCHandle[] m_handles;
        int m_current;

        public GCHandlePool()
        {
            m_handles = new GCHandle[128];
        }

        public GCHandle Alloc()
        {
            if (m_current > 0)
            {
                return m_handles[--m_current];
            }

            return GCHandle.Alloc(null);
        }

        public GCHandle Alloc(object o)
        {
            if (m_current > 0)
            {
                var handle = m_handles[--m_current];

                handle.Target = o;

                return handle;
            }

            return GCHandle.Alloc(o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr AllocHandleIfNotNull(object o)
        {
            if (o == null)
                return IntPtr.Zero;

            return (IntPtr)Alloc(o);
        }

        public void Free(GCHandle h)
        {
            if (m_current == m_handles.Length)
            {
                var newLength = m_handles.Length * 2;
                var newHandles = new GCHandle[newLength];
                Array.Copy(m_handles, newHandles, m_handles.Length);

                m_handles = newHandles;
            }

            h.Target = null;

            m_handles[m_current++] = h;
        }
    }
}
