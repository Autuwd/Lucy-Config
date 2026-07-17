// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 MonoBehaviour — 用户脚本的基类（你每写一个脚本都在用这个类）
//
// 📌 作用：
//   这是所有 Unity 用户脚本必须继承的基类。
//   它提供了 Unity 生命周期回调的入口点：
//     Awake → OnEnable → Start → FixedUpdate → Update
//     → LateUpdate → OnDisable → OnDestroy
//   以及协程系统、Invoke 机制等。
//
// 🏗 继承链：
//   Object → Component → Behaviour → MonoBehaviour
//
// ⚡ C++ 对应：
//   实际的"魔法"发生在 C++ 端 Runtime/Mono/MonoBehaviour.h
//   C++ 端负责在每帧正确的时机调用这些回调方法。
//   C# 端只提供了回调方法的声明和基础工具（协程、Invoke）。
// ==============================================================

using System;
using System.Collections;
using System.Threading;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngineInternal;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    // ==============================================================
    // MonoBehaviour — 核心特征
    //
    // 🔑 Unity 生命周期（由 C++ 引擎驱动）：
    //
    //   创建时：
    //     Awake()        → 对象被创建时立即调用
    //     OnEnable()     → 对象变为激活时调用
    //     Start()        → 第一帧 Update 之前调用
    //
    //   每帧：
    //     FixedUpdate()  → 固定时间步长（物理更新）
    //     Update()       → 每帧调用
    //     LateUpdate()   → 所有 Update 之后调用
    //
    //   销毁时：
    //     OnDisable()    → 对象变为非激活时
    //     OnDestroy()    → 对象被销毁时
    //
    // 💡 关键理解：
    //   这些方法并不是虚函数（virtual）让你 Override，
    //   而是由 C++ 端通过反射在适当时机调用的。
    //   实际上 MonoBehaviour 中根本没有声明 Awake/Start/Update！
    //   它们是通过 Unity 的"消息系统"（Message System）触发的。
    //   参考：Runtime/Export/Scripting/Coroutines.cs 中的消息系统
    //
    // 🆕 新增特性（6000.x）：
    //   - destroyCancellationToken: 对象销毁时自动取消的令牌
    //   - Awaitable 支持：可以用 await 替代协程
    // ==============================================================
    [RequiredByNativeCode]
    [ExtensionOfNativeClass]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
    [NativeHeader("Runtime/Scripting/DelayedCallUtility.h")]
    public class MonoBehaviour : Behaviour
    {
        // ==============================================================
        // MonoBehaviour() — 构造函数
        //
        // 每个 MonoBehaviour 创建时都会调用 ConstructorCheck(this)。
        // 这是 C++ 端的检查，用于确保脚本可以被正确实例化。
        // 如果你的脚本在 Console 中报 "Can't add script behaviour"，
        // 很可能就是 ConstructorCheck 失败了。
        // ==============================================================
        public MonoBehaviour()
        {
            ConstructorCheck(this);
        }

        // ==============================================================
        // destroyCancellationToken — Unity 6000+ 新增的特性
        //
        // 🎯 用途：
        //   当你需要异步操作时，用它来在对象销毁时自动取消。
        //   替代了旧版中需要手动在 OnDestroy 中 Cancel 的做法。
        //
        // 💡 使用示例：
        //   async void Start() {
        //       await SomeTask(destroyCancellationToken);
        //   }
        //
        // 🔄 工作流程：
        //   1. 首次访问时创建 CancellationTokenSource
        //   2. 对象销毁时 C++ 端调用 RaiseCancellation()
        //   3. 所有使用此 Token 的异步操作自动取消
        // ==============================================================
        private CancellationTokenSource m_CancellationTokenSource;
        public CancellationToken destroyCancellationToken
        {
            get
            {
                if (this == null)
                    throw new MissingReferenceException("DestroyCancellation token should be called atleast once before destroying the monobehaviour object");
                if (m_CancellationTokenSource == null)
                {
                    m_CancellationTokenSource = new CancellationTokenSource();
                    OnCancellationTokenCreated();
                }
                return m_CancellationTokenSource.Token;
            }
        }

        // ⚡ 由 C++ 端调用，当 MonoBehaviour 被销毁时触发取消
        [RequiredByNativeCode]
        private void RaiseCancellation()
        {
            m_CancellationTokenSource?.Cancel();
        }

        // ==============================================================
        // Invoke / InvokeRepeating — 延时调用系统
        //
        // 🎯 用途：
        //   在指定时间后调用指定名称的方法。
        //   不需要协程，也不需要 Update 中的计时器。
        //
        // 💡 注意：
        //   方法名通过字符串指定（反射调用），
        //   所以改方法名时需要同步更新字符串参数。
        //
        // ⚠️ 性能提示：
        //   频繁的 Invoke 调用会产生 GC 分配。
        //   高频率场景建议使用协程或 Update 中的计时器替代。
        // ==============================================================

        // 是否有任何 Invoke 待处理
        public bool IsInvoking()
        {
            return Internal_IsInvokingAll(this);
        }

        // 取消所有 Invoke
        public void CancelInvoke()
        {
            Internal_CancelInvokeAll(this);
        }

        // 在 time 秒后调用 methodName 方法（只执行一次）
        public void Invoke(string methodName, float time)
        {
            InvokeDelayed(this, methodName, time, 0.0f);
        }

        // 在 time 秒后开始，每隔 repeatRate 秒重复调用
        // 例如：InvokeRepeating("SpawnEnemy", 2f, 1f) → 2秒后开始每秒生成敌人
        public void InvokeRepeating(string methodName, float time, float repeatRate)
        {
            if (repeatRate <= 0.00001f && repeatRate != 0.0f)
                throw new UnityException("Invoke repeat rate has to be larger than 0.00001F");

            InvokeDelayed(this, methodName, time, repeatRate);
        }

        // 取消特定名称的 Invoke
        public void CancelInvoke(string methodName)
        {
            CancelInvoke(this, methodName);
        }

        // 检查特定名称的 Invoke 是否待处理
        public bool IsInvoking(string methodName)
        {
            return IsInvoking(this, methodName);
        }

        // ==============================================================
        // Coroutine System — 协程系统
        //
        // 🎯 用途：
        //   协程是 Unity 中处理时间延迟、序列动画、异步操作的轻量级方案。
        //   通过 IEnumerator 和 yield 语句实现"暂停-继续"的执行流。
        //
        // 💡 协程的核心机制：
        //   1. StartCoroutine 接收一个 IEnumerator
        //   2. 每帧（或指定条件满足时）MoveNext() 推进枚举器
        //   3. yield return 的对象决定了何时继续执行
        //      例如：yield return null → 下一帧继续
        //            yield return new WaitForSeconds(1) → 1秒后继续
        //            yield return new WaitForEndOfFrame() → 帧末继续
        //
        // 🏗 实现位置：
        //   C# 端通过 StartCoroutineManaged2 将 IEnumerator 传递给 C++，
        //   C++ 端负责实际的"推进"（在适当的时机调用 MoveNext）。
        //   Coroutine 类的定义在 Coroutine.bindings.cs 中。
        //
        // 📌 注意：
        //   协程是 MonoBehaviour 的实例方法，
        //   当 MonoBehaviour 被 Disable/Destroy 时协程自动停止。
        // ==============================================================

        // 通过方法名启动协程（不传参数）
        [uei.ExcludeFromDocs]
        public Coroutine StartCoroutine(string methodName)
        {
            object value = null;
            return StartCoroutine(methodName, value);
        }

        // 通过方法名启动协程（可传一个参数）
        public Coroutine StartCoroutine(string methodName, [uei.DefaultValue("null")] object value)
        {
            if (string.IsNullOrEmpty(methodName))
                throw new NullReferenceException("methodName is null or empty");

            if (!IsObjectMonoBehaviour(this))
                throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

            return StartCoroutineManaged(methodName, value);
        }

        // ★ 最常用的重载 — 通过 IEnumerator 启动协程
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            if (routine == null)
                throw new NullReferenceException("routine is null");

            if (!IsObjectMonoBehaviour(this))
                throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

            return StartCoroutineManaged2(routine);
        }

        [Obsolete("StartCoroutine_Auto has been deprecated. Use StartCoroutine instead (UnityUpgradable) -> StartCoroutine([mscorlib] System.Collections.IEnumerator)", false)]
        public Coroutine StartCoroutine_Auto(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        // 停止指定协程（通过 IEnumerator 引用）
        public void StopCoroutine(IEnumerator routine)
        {
            if (routine == null)
                throw new NullReferenceException("routine is null");

            if (!IsObjectMonoBehaviour(this))
                throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

            StopCoroutineFromEnumeratorManaged(routine);
        }

        // 停止指定协程（通过 Coroutine 对象）
        public void StopCoroutine(Coroutine routine)
        {
            if (routine == null)
                throw new NullReferenceException("routine is null");

            if (!IsObjectMonoBehaviour(this))
                throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

            StopCoroutineManaged(routine);
        }

        // 通过方法名停止协程
        public extern void StopCoroutine(string methodName);

        // 停止所有协程
        public extern void StopAllCoroutines();

        // ==============================================================
        // useGUILayout — 是否启用 GUI 布局系统
        //
        // 如果设为 false，MonoBehaviour 不会在 OnGUI 时自动调用
        // GUI.BeginGroup / GUI.EndGroup 等布局方法。
        // 在需要完全控制 GUI 绘制时使用。
        // ==============================================================
        public extern bool useGUILayout { get; set; }

        // ==============================================================
        // didStart / didAwake — 生命周期回调状态标记
        //
        // 由引擎内部使用，标记该 MonoBehaviour 是否已经执行了
        // Start() 和 Awake() 方法。
        // 可用于检测生命周期回调的执行状态。
        // ==============================================================
        public extern bool didStart { get; }
        public extern bool didAwake { get; }

        // ==============================================================
        // runInEditMode — 编辑器模式下运行
        //
        // 默认 MonoBehaviour 只在 Play Mode 下运行。
        // 将此设为 true 后，脚本的 Update/FixedUpdate 等会在
        // Editor 的 Edit Mode 下也执行。
        // 常用于编辑器工具、Gizmo 绘制等场景。
        // ==============================================================
        public extern bool runInEditMode { get; set; }
        internal extern bool allowPrefabModeInPlayMode { get; }

        // ==============================================================
        // print — 快捷打印日志
        //
        // MonoBehaviour.print("消息") 等价于 Debug.Log("消息")
        // 只是更短更方便。使用相同的方式输出到 Console 窗口。
        // ==============================================================
        public static void print(object message)
        {
            Debug.Log(message);
        }

        [NativeMethod(IsThreadSafe = true)]
        extern static void ConstructorCheck([Writable] Object self);

        [FreeFunction("CancelInvoke")]
        extern static void Internal_CancelInvokeAll([NotNull] MonoBehaviour self);

        [FreeFunction("IsInvoking")]
        extern static bool Internal_IsInvokingAll([NotNull] MonoBehaviour self);

        [FreeFunction]
        extern static void InvokeDelayed([NotNull] MonoBehaviour self, string methodName, float time, float repeatRate);

        [FreeFunction]
        extern static void CancelInvoke([NotNull] MonoBehaviour self, string methodName);

        [FreeFunction]
        extern static bool IsInvoking([NotNull] MonoBehaviour self, string methodName);

        [FreeFunction]
        extern static bool IsObjectMonoBehaviour([NotNull] Object obj);

        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern Coroutine StartCoroutineManaged(string methodName, object value);

        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern Coroutine StartCoroutineManaged2(IEnumerator enumerator);

        extern void StopCoroutineManaged(Coroutine routine);

        extern void StopCoroutineFromEnumeratorManaged(IEnumerator routine);

        extern internal string GetScriptClassName();

        extern void OnCancellationTokenCreated();
    }
}
