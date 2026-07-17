// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Loading;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.SceneManagement
{
    //=============================================================================
    // 场景管理系统 (Scene Management System)
    //
    // 功能概述:
    //   Unity 的场景管理系统是游戏世界的核心容器管理单元。一个"场景"(Scene)包含
    //   了游戏对象(GameObjects)、灯光、摄像机、导航网格等所有游戏运行时所需的元素。
    //   场景管理系统负责场景的:
    //     - 加载(Loading) 与 卸载(Unloading)
    //     - 创建(Creation) 与 合并(Merging)
    //     - 激活(Activation) 与 切换(Switching)
    //     - 查询(Query): 按名称、路径、索引获取场景引用
    //
    // 架构层次:
    //   C++ Native Layer (SceneManager.h)
    //       ↕ P/Invoke 绑定
    //   SceneManagerAPIInternal (内部静态类, 直接封装 Native 调用)
    //       ↕ 委托调用
    //   SceneManagerAPI (可被用户 override 的 API 层, 支持自定义加载行为)
    //       ↕ 默认回退
    //   SceneManager (公有 API, 开发者直接调用的入口)
    //
    // 设计亮点:
    //   - SceneManagerAPI 的可 override 设计允许 Addressables 等资源管理系统
    //     完全接管场景加载行为, 而不需要修改 SceneManager 的公有接口
    //   - s_AllowLoadScene 机制提供了一种在特定生命周期内禁止加载/卸载的安全闸门
    //   - 同步加载(LoadScene)本质上是异步加载(mustCompleteNextFrame=true)的包装,
    //     通过强制下一帧完成来实现"同步"效果
    //=============================================================================

    /// <summary>
    /// 场景管理器的底层 Native API 封装。
    /// 这是最底层的 C# 绑定层，通过 P/Invoke 直接调用 C++ 侧的 SceneManagerBindings 静态方法。
    /// 所有实际的场景加载、卸载、查询操作都在此层完成。
    /// 该类的设计目的是作为内部工具使用，对外不公开。
    /// </summary>
    [NativeHeader("Runtime/Export/SceneManager/SceneManager.bindings.h")]
    [NativeHeader("Runtime/SceneManager/SceneManager.h")]
    [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
    internal static class SceneManagerAPIInternal
    {
        /// <summary>
        /// 获取 Build Settings 中注册的场景数量。
        /// 注意: 在 Slim Player（精简运行时）中不包含场景数据，
        /// Build Settings 的值会不正确，需要通过 AssetBundle 的场景名称查找来重新映射。
        /// </summary>
        public static extern int GetNumScenesInBuildSettings();

        /// <summary>
        /// 根据 Build Settings 中的索引获取场景。
        /// 如果索引越界或场景无效，会抛出异常（ThrowsException = true）。
        /// </summary>
        /// <param name="buildIndex">Build Settings 中的场景索引</param>
        /// <returns>场景引用</returns>
        [NativeMethod(ThrowsException = true)]
        public static extern Scene GetSceneByBuildIndex(int buildIndex);

        /// <summary>
        /// 按名称或构建索引加载场景的内部方法（异步）。
        /// 这是所有场景加载的最终底层入口，C++ 侧处理实际的资源流式加载、场景实例化等操作。
        /// </summary>
        /// <param name="sceneName">场景名称（sceneBuildIndex 有效时可传 null）</param>
        /// <param name="sceneBuildIndex">构建索引（sceneName 有效时可传 -1）</param>
        /// <param name="parameters">加载参数（加载模式、物理模式等）</param>
        /// <param name="mustCompleteNextFrame">如果为 true，强制在下一帧完成加载，
        ///    即实现"同步加载"效果。实际仍走异步路径，但设置为最高优先级立即完成。</param>
        /// <returns>异步操作对象，可用于追踪加载进度</returns>
        [NativeMethod(ThrowsException = true)]
        public static extern AsyncOperation LoadSceneAsyncNameIndexInternal(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame);

        /// <summary>
        /// 按名称或构建索引卸载场景的内部方法。
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="sceneBuildIndex">构建索引</param>
        /// <param name="immediately">是否立即卸载（同步模式）</param>
        /// <param name="options">卸载选项（如是否卸载内嵌子场景对象）</param>
        /// <param name="outSuccess">输出参数，指示卸载是否成功</param>
        /// <returns>异步操作对象</returns>
        [NativeMethod(ThrowsException = true)]
        public static extern AsyncOperation UnloadSceneNameIndexInternal(string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options, out bool outSuccess);
    }

    /// <summary>
    /// 场景管理器 API 层。此类是可扩展的，允许外部系统（如 Addressables）
    /// 通过设置 overrideAPI 完全接管场景加载的生命周期管理。
    ///
    /// 设计模式: 策略模式（Strategy Pattern）
    ///   - SceneManagerAPI 定义了场景加载卸载的策略接口
    ///   - s_DefaultAPI 是默认策略，直接调用 SceneManagerAPIInternal 的本地方法
    ///   - overrideAPI 可被替换为自定义策略，实现自定义加载行为
    ///
    /// 使用场景:
    ///   - Addressables 系统会设置 overrideAPI，使其场景管理功能与 SceneManager
    ///     的公有 API 无缝集成，开发者无需修改代码即可切换加载方式。
    ///   - 自定义资源管理系统可以通过继承此类并重写虚方法来扩展加载行为。
    /// </summary>
    public class SceneManagerAPI
    {
        /// <summary>
        /// 默认 API 实例。当未设置 overrideAPI 时，所有操作回退到此默认实现。
        /// </summary>
        static SceneManagerAPI s_DefaultAPI = new SceneManagerAPI();

        /// <summary>
        /// 获取当前活跃的 API 实例。
        /// 内部代码必须使用此属性而非直接使用 overrideAPI，以确保在未设置 overrideAPI 时
        /// 能正确回退到默认 API 处理。
        /// </summary>
        internal static SceneManagerAPI ActiveAPI => overrideAPI ?? s_DefaultAPI;

        /// <summary>
        /// 获取或设置自定义的 SceneManagerAPI 覆写实例。
        /// 设置此属性后，所有场景加载/卸载操作将路由到自定义实现中。
        /// 设置为 null 可恢复为默认行为。
        /// </summary>
        public static SceneManagerAPI overrideAPI { get; set; }

        /// <summary>
        /// 受保护内部的构造函数，确保此类只能从继承链中实例化或创建默认实例。
        /// </summary>
        protected internal SceneManagerAPI() {}

        /// <summary>获取 Build Settings 中的场景数量（虚方法，可重写）。</summary>
        protected internal virtual int GetNumScenesInBuildSettings() => SceneManagerAPIInternal.GetNumScenesInBuildSettings();

        /// <summary>按构建索引获取场景（虚方法，可重写）。</summary>
        protected internal virtual Scene GetSceneByBuildIndex(int buildIndex) => SceneManagerAPIInternal.GetSceneByBuildIndex(buildIndex);

        /// <summary>
        /// 按名称或索引异步加载场景（虚方法，可重写）。
        /// 这是子类自定义加载行为时最常重写的方法。
        /// </summary>
        protected internal virtual AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame) =>
            SceneManagerAPIInternal.LoadSceneAsyncNameIndexInternal(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);

        /// <summary>
        /// 按名称或索引卸载场景（虚方法，可重写）。
        /// </summary>
        protected internal virtual AsyncOperation UnloadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options, out bool outSuccess) =>
            SceneManagerAPIInternal.UnloadSceneNameIndexInternal(sceneName, sceneBuildIndex, immediately, options, out outSuccess);

        /// <summary>
        /// 加载首个场景（虚方法，可重写）。
        /// 在播放模式启动时由引擎调用，用于加载初始场景。
        /// 默认实现返回 null，表示不由 API 层处理，由引擎默认行为完成。
        /// </summary>
        /// <param name="mustLoadAsync">是否必须异步加载</param>
        /// <returns>异步操作对象，或 null 表示使用默认行为</returns>
        protected internal virtual AsyncOperation LoadFirstScene(bool mustLoadAsync) => null;
    }

    /// <summary>
    /// 场景管理器 —— Unity 场景系统的核心 API 入口。
    /// 这是开发者最常使用的场景管理类，提供了场景加载、卸载、创建、查询、合并等所有操作。
    ///
    /// 此类是一个 partial class，另一部分定义在 SceneManager.cs 中（包含事件系统）。
    ///
    /// 线程安全: 所有方法必须在主线程调用，场景操作不是线程安全的。
    /// </summary>
    [NativeHeader("Runtime/Export/SceneManager/SceneManager.bindings.h")]
    [RequiredByNativeCode]
    public partial class SceneManager
    {
        /// <summary>
        /// 场景加载允许开关。
        /// 当为 false 时，所有的 LoadScene/UnloadScene 操作将被静默忽略（返回 null 或 false）。
        /// 
        /// 用途: 在场景切换的关键生命周期中（如 DontDestroyOnLoad 对象处理期间），
        /// 引擎会临时关闭此开关以防止递归加载导致的栈溢出或状态不一致。
        /// 
        /// 这是一个内部机制，外部代码不应该修改此字段。
        /// </summary>
        static internal bool s_AllowLoadScene = true;

        /// <summary>
        /// 当前场景管理器中所有已加载场景的总数。
        /// 包括所有已加载的场景（无论是通过 Build Settings 还是通过 AssetBundle 加载的），
        /// 但不包括已卸载的场景。
        /// 对应 C++ 侧 SceneManager::GetSceneCount()。
        /// </summary>
        public static extern int sceneCount
        {
            [NativeHeader("Runtime/SceneManager/SceneManager.h")]
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetSceneCount")]
            get;
        }

        /// <summary>
        /// 当前已完全加载（加载状态为 Loaded）的场景数量。
        /// 与 sceneCount 的区别: sceneCount 包含所有已加载的场景（含正在加载中的），
        /// 而 loadedSceneCount 只计数已经完成加载的场景。
        /// 对应 C++ 侧 SceneManager::GetLoadedSceneCount()。
        /// </summary>
        public extern static int loadedSceneCount
        {
            [NativeHeader("Runtime/SceneManager/SceneManager.h")]
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetLoadedSceneCount")]
            get;
        }

        /// <summary>
        /// Build Settings（构建设置）中注册的场景数量。
        /// 注意: 此值通过 SceneManagerAPI 获取，可能被自定义 API 实现（如 Addressables）覆写。
        /// 在 Slim Player（无场景的精简运行时）中可能返回不正确的值。
        /// </summary>
        public static int sceneCountInBuildSettings
        {
            get { return SceneManagerAPI.ActiveAPI.GetNumScenesInBuildSettings(); }
        }

        /// <summary>
        /// 检查场景是否可以被设为激活场景。
        /// 内部使用，在 SetActiveScene 之前验证场景状态。
        /// </summary>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        internal static extern bool CanSetAsActiveScene(Scene scene);

        /// <summary>
        /// 获取当前激活的场景（Active Scene）。
        /// 激活场景是当前接收新游戏对象实例化的场景，
        /// 也是灯光、摄像机等场景级设置的参考场景。
        /// 始终只有一个场景是激活的。
        /// </summary>
        /// <returns>当前激活的场景引用</returns>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetActiveScene();

        /// <summary>
        /// 设置指定场景为激活场景。
        /// 激活场景的变化会触发 activeSceneChanged 事件。
        /// 场景必须已加载且有效才能被设激活。
        /// </summary>
        /// <param name="scene">要设为激活的场景</param>
        /// <returns>是否设置成功</returns>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern bool SetActiveScene(Scene scene);

        /// <summary>
        /// 根据场景文件的路径获取场景引用。
        /// 路径格式示例: "Assets/Scenes/MainScene.unity"
        /// 场景必须已加载才有效；如果场景未加载或路径无效，返回无效场景（IsValid == false）。
        /// </summary>
        /// <param name="scenePath">场景文件的项目路径</param>
        /// <returns>场景引用</returns>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetSceneByPath(string scenePath);

        /// <summary>
        /// 根据场景名称获取场景引用。
        /// 场景名称是文件名（不含路径和扩展名），例如 "MainScene"。
        /// 场景必须已加载才有效；如果有多个同名场景已加载，返回第一个匹配的场景。
        /// </summary>
        /// <param name="name">场景名称</param>
        /// <returns>场景引用</returns>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetSceneByName(string name);

        /// <summary>
        /// 通过 LoadableSceneId 获取场景引用。
        /// LoadableSceneId 是 Addressables 等可加载资源系统使用的场景标识符，
        /// 用于在底层关联场景与其资源加载句柄。
        /// </summary>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetSceneByLoadableSceneId(LoadableSceneId loadableSceneId);

        /// <summary>
        /// 根据 Build Settings 中的构建索引获取已加载的场景引用。
        /// 构建索引在 File → Build Settings 中定义，从 0 开始。
        /// 场景必须已加载；此方法通过 SceneManagerAPI 调用，可被自定义 API 覆写。
        /// </summary>
        /// <param name="buildIndex">构建索引</param>
        /// <returns>场景引用（如果未加载则无效）</returns>
        public static Scene GetSceneByBuildIndex(int buildIndex)
        {
            return SceneManagerAPI.ActiveAPI.GetSceneByBuildIndex(buildIndex);
        }

        /// <summary>
        /// 根据索引获取已加载的场景。
        /// 索引范围: 0 到 sceneCount - 1。
        /// 返回的场景始终是有效的（已加载的）。
        /// 索引越界会抛出异常。
        /// </summary>
        /// <param name="index">已加载场景列表中的索引</param>
        /// <returns>场景引用</returns>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern Scene GetSceneAt(int index);

        /// <summary>
        /// 创建一个新的空场景。
        /// 新场景会立即被添加到场景管理器的已加载场景列表中，
        /// 并包含一个默认的根游戏对象（场景根）。
        /// 创建后可向场景中添加游戏对象，或使用 MergeScenes 合并场景。
        /// </summary>
        /// <param name="sceneName">新场景的名称</param>
        /// <param name="parameters">创建参数（如是否包含物理世界）</param>
        /// <returns>新创建的场景引用</returns>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern Scene CreateScene([NotNull] string sceneName, CreateSceneParameters parameters);

        /// <summary>
        /// 卸载场景的同步内部方法。
        /// 此方法会立即卸载场景，可能在卸载过程中产生 GC 压力或导致卡顿。
        /// 已标记为 Obsolete，建议使用异步版本 UnloadSceneAsync。
        /// </summary>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        private static extern bool UnloadSceneInternal(Scene scene, UnloadSceneOptions options);

        /// <summary>
        /// 卸载场景的异步内部方法。
        /// 返回 AsyncOperation 对象，可通过其 completed 事件或协程来获知卸载完成。
        /// 异步卸载避免了同步卸载可能导致的帧率卡顿。
        /// </summary>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        private static extern AsyncOperation UnloadSceneAsyncInternal(Scene scene, UnloadSceneOptions options);

        /// <summary>
        /// 按名称或索引异步加载场景的内部封装方法。
        /// 这是所有 LoadScene / LoadSceneAsync 重载最终调用的核心方法。
        ///
        /// 设计要点:
        ///   1. 检查 s_AllowLoadScene 开关，若为 false 则静默忽略（返回 null）
        ///   2. 路由到 SceneManagerAPI.ActiveAPI，支持自定义加载行为
        ///   3. mustCompleteNextFrame 参数控制同步/异步模式:
        ///      - true: 同步模式（LoadScene），强制在下一帧完成加载
        ///      - false: 异步模式（LoadSceneAsync），通过 AsyncOperation 报告进度
        ///
        /// 异步加载机制:
        ///   Unity 的场景加载采用"分帧加载"策略，将繁重的 I/O 操作分散到多个帧中执行，
        ///   以减少单帧卡顿。AsyncOperation.progress 反映加载进度（0.0 ~ 1.0），
        ///   当 progress 达到 0.9 时场景已可激活，可通过 allowSceneActivation 控制激活时机。
        /// </summary>
        /// <param name="sceneName">场景名称（按名称加载时使用）</param>
        /// <param name="sceneBuildIndex">构建索引（按索引加载时使用）</param>
        /// <param name="parameters">加载参数</param>
        /// <param name="mustCompleteNextFrame">是否强制下一帧完成（同步模式）</param>
        /// <returns>异步操作对象，同步模式下返回的也可追踪</returns>
        private static AsyncOperation LoadSceneAsyncNameIndexInternal(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
        {
            if (!s_AllowLoadScene)
                return null;

            return SceneManagerAPI.ActiveAPI.LoadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);
        }

        /// <summary>
        /// 按名称或索引卸载场景的内部封装方法。
        /// 与 LoadSceneAsyncNameIndexInternal 对应的卸载操作。
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="sceneBuildIndex">构建索引</param>
        /// <param name="immediately">是否立即卸载（同步模式）</param>
        /// <param name="options">卸载选项</param>
        /// <param name="outSuccess">输出卸载是否成功的标志</param>
        /// <returns>异步操作对象</returns>
        private static AsyncOperation UnloadSceneNameIndexInternal(string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options, out bool outSuccess)
        {
            if (!s_AllowLoadScene)
            {
                outSuccess = false;
                return null;
            }

            return SceneManagerAPI.ActiveAPI.UnloadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, immediately, options, out outSuccess);
        }

        /// <summary>
        /// 通过 LoadableSceneId 异步加载场景。
        /// LoadableSceneId 是由 Addressables 等资源管理系统生成的场景标识符，
        /// 用于在运行时唯一标识一个可加载的场景资源。
        /// </summary>
        /// <param name="loadableSceneId">可加载场景的标识符</param>
        /// <param name="parameters">加载参数</param>
        /// <returns>异步操作对象</returns>
        public static AsyncOperation LoadSceneAsync(LoadableSceneId loadableSceneId, LoadSceneParameters parameters = new LoadSceneParameters())
        {
            if (!s_AllowLoadScene)
                return null;

            return LoadSceneByLoadableSceneIdAsync(loadableSceneId, parameters, false);
        }

        /// <summary>
        /// 通过 LoadableSceneId 异步加载场景的内部 Native 方法。
        /// </summary>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        private static extern AsyncOperation LoadSceneByLoadableSceneIdAsync(LoadableSceneId loadableSceneId, LoadSceneParameters parameters, bool mustCompleteNextFrame);

        /// <summary>
        /// 合并场景 —— 将源场景中的所有游戏对象移动到目标场景中，然后卸载源场景。
        ///
        /// 行为细节:
        ///   - 源场景中的所有根游戏对象会被重新父级到目标场景的根中
        ///   - 源场景在合并完成后被自动卸载
        ///   - 合并后源场景的 Scene 引用变为无效
        ///   - 如果源场景是 DontDestroyOnLoad 场景，则不能合并
        ///
        /// 典型用途:
        ///   将使用 CreateScene 创建的临时场景内容合并到主场景中，
        ///   避免场景碎片化，提高场景管理效率。
        /// </summary>
        /// <param name="sourceScene">源场景（会被卸载）</param>
        /// <param name="destinationScene">目标场景（接收所有对象）</param>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern void MergeScenes(Scene sourceScene, Scene destinationScene);

        /// <summary>
        /// 将单个游戏对象移动到指定场景中。
        /// 移动后，该游戏对象成为目标场景的根游戏对象。
        ///
        /// 注意事项:
        ///   - 游戏对象的 Transform 层级关系保持不变（子对象会跟随移动）
        ///   - 不能将游戏对象移动到 DontDestroyOnLoad 场景
        ///   - 不能将 DontDestroyOnLoad 场景中的对象移出
        ///   - 目标场景必须已加载
        /// </summary>
        /// <param name="go">要移动的游戏对象</param>
        /// <param name="scene">目标场景</param>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern void MoveGameObjectToScene([NotNull] GameObject go, Scene scene);

        /// <summary>
        /// 通过实例 ID 批量移动多个游戏对象到指定场景的底层方法。
        /// 使用 IntPtr 传递非托管内存中的实例 ID 数组，降低托管与非托管边界的编组开销。
        /// </summary>
        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        private extern static void MoveGameObjectsToSceneByInstanceId(IntPtr instanceIds, int instanceCount, Scene scene);

        /// <summary>
        /// 批量移动游戏对象到指定场景（已废弃的 int 版本）。
        /// 已标记为 Obsolete(true)，使用时会编译错误。
        /// 请改用 EntityId 版本。
        /// </summary>
        [System.Obsolete("Please use MoveGameObjectsToScene(NativeArray<EntityId>, Scene scene) with the EntityId parameter type instead.", true)]
        public static unsafe void MoveGameObjectsToScene(NativeArray<int> instanceIDs, Scene scene) =>
            throw new NotImplementedException("Please use MoveGameObjectsToScene(NativeArray<EntityId>, Scene scene) with the EntityId parameter type instead.");

        /// <summary>
        /// 批量移动游戏对象到指定场景。
        /// 使用 NativeArray&lt;EntityId&gt; 作为参数，支持 Burst/RECS 系统中的批量操作。
        /// 
        /// 内部通过 MoveGameObjectsToSceneByInstanceId 实现，
        /// 直接传递非托管内存指针以避免数组拷贝。
        /// </summary>
        /// <param name="entityIds">要移动的游戏对象的 EntityId 数组</param>
        /// <param name="scene">目标场景</param>
        public static unsafe void MoveGameObjectsToScene(NativeArray<EntityId> entityIds, Scene scene)
        {
            if (!entityIds.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(entityIds));

            if (entityIds.Length == 0)
                return;

            MoveGameObjectsToSceneByInstanceId((IntPtr)entityIds.GetUnsafeReadOnlyPtr(), entityIds.Length, scene);
        }

        /// <summary>
        /// 加载初始场景的内部方法。
        /// 由引擎在播放模式启动时通过 [RequiredByNativeCode] 回调调用。
        /// 实际加载行为委托给 SceneManagerAPI.ActiveAPI.LoadFirstScene()。
        /// 
        /// 如果自定义 API 返回了 AsyncOperation，引擎会等待其完成；
        /// 如果返回 null，引擎会使用默认流程加载 Build Settings 中的第一个场景。
        /// </summary>
        /// <param name="async">是否以异步方式加载</param>
        /// <returns>异步操作对象，或 null 表示使用默认行为</returns>
        [RequiredByNativeCode]
        internal static AsyncOperation LoadFirstScene_Internal(bool async)
        {
            return SceneManagerAPI.ActiveAPI.LoadFirstScene(async);
        }
    }
}
