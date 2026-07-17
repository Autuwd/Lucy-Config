// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// Resources 系统 — Unity 内置资源管理入口
//
// 功能概述：
//   Resources 是 Unity 最基础的资源加载 API，提供从 "Resources" 文件夹
//   加载资源的统一入口。所有放在 "Resources" 及子目录下的资源都会被
//   打包到主程序包中，运行时通过 Resources.Load / LoadAsync 等 API 加载。
//
// 核心设计：
//   - Resources.Load()            — 同步加载单个资源
//   - Resources.LoadAsync()       — 异步加载单个资源（返回 ResourceRequest）
//   - Resources.LoadAll()         — 加载指定路径下的所有资源
//   - Resources.UnloadUnusedAssets() — 自动卸载未使用的资源
//   - Resources.FindObjectsOfTypeAll() — 查找所有类型的对象
//
// 注意事项：
//   - Resources 文件夹仅用于"必须随包发布"的资源（如启动界面 Shader、
//     默认材质等），滥用会导致包体膨胀和加载时间增加。
//   - 推荐使用 Addressables 或 AssetBundle 替代大规模资源管理。
//   - Resources 资源会始终驻留在内存中，无法被增量更新。
//
// 相关文档：
//   https://docs.unity3d.com/Manual/LoadingResourcesatRuntime.html
// ============================================================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    /// <summary>
    /// 异步资源加载请求对应 C# 托管端实现。
    /// 对应 C++ 端的 ResourceRequestScripting（ResourceManagerUtility.h）。
    /// 继承自 AsyncOperation，提供异步加载进度追踪和完成回调能力。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class ResourceRequest : AsyncOperation
    {
        /// <summary>资源在 Resources 文件夹中的相对路径（不含扩展名）</summary>
        internal string m_Path;

        /// <summary>请求加载的资源类型</summary>
        internal Type m_Type;

        /// <summary>
        /// 获取最终加载的资源对象。
        /// 默认实现直接调用 Resources.Load 同步加载。
        /// 子类（如 AssetBundleRequest）可重写以提供不同行为。
        /// </summary>
        protected virtual Object GetResult()
        {
            return Resources.Load(m_Path, m_Type);
        }

        /// <summary>获取请求加载的资源对象。访问此属性会阻塞直到加载完成。</summary>
        public Object asset { get { return GetResult(); } }

        public ResourceRequest() { }

        protected ResourceRequest(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static ResourceRequest ConvertToManaged(IntPtr ptr) => new ResourceRequest(ptr);
        }
    }

    /// <summary>
    /// Resources 系统内部 API。
    /// 这是 C# 对 C++ 端资源管理函数的直接绑定层。
    /// 所有方法均通过 [FreeFunction] 属性映射到原生函数。
    /// 外部代码应使用 ResourcesAPI 或 Resources 类。
    /// </summary>
    [NativeHeader("Runtime/Export/Resources/Resources.bindings.h")]
    [NativeHeader("Runtime/Misc/ResourceManagerUtility.h")]
    internal static class ResourcesAPIInternal
    {
        /// <summary>
        /// 获取指定类型的所有已加载对象（包括场景中的、Resources 中的、DontDestroyOnLoad 中的）。
        /// 对应 C++ 端 Resources_Bindings::FindObjectsOfTypeAll。
        /// 注意：这会返回场景中所有 Component 挂载的 GameObject 等，开销较大。
        /// </summary>
        [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
        [FreeFunction("Resources_Bindings::FindObjectsOfTypeAll")]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public extern static Object[] FindObjectsOfTypeAll(Type type);

        /// <summary>通过名称查找 Shader（使用 Shader 名称注册表）</summary>
        [FreeFunction("GetShaderNameRegistry().FindShader")]
        public extern static Shader FindShaderByName(string name);

        /// <summary>
        /// 从 Resources 文件夹同步加载资源。
        /// 路径相对于任何 Resources 文件夹，不包含扩展名。
        /// ThrowsException = true 表示加载失败会抛出异常。
        /// </summary>
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        [FreeFunction("Resources_Bindings::Load", ThrowsException = true)]
        public extern static Object Load(string path, [NotNull] Type systemTypeInstance);

        /// <summary>
        /// 从 Resources 文件夹加载指定路径下的所有资源。
        /// 如果 path 指向文件夹，则加载该文件夹下的所有资源；
        /// 如果指向具体文件（不包含扩展名的路径），则加载该文件及其子资源。
        /// </summary>
        [FreeFunction("Resources_Bindings::LoadAll", ThrowsException = true)]
        public extern static Object[] LoadAll([NotNull] string path, [NotNull] Type systemTypeInstance);

        /// <summary>获取指定路径下的所有可用路径。用于编辑器中的资源浏览。</summary>
        [FreeFunction("Resources_Bindings::GetAllPaths")]
        public extern static string[] GetAllPaths([NotNull] string path);

        /// <summary>异步加载资源的内部实现，返回 ResourceRequest 用于追踪加载进度。</summary>
        [FreeFunction("Resources_Bindings::LoadAsyncInternal")]
        extern internal static ResourceRequest LoadAsyncInternal(string path, Type type);

        /// <summary>
        /// 从内存中卸载指定的资源对象。
        /// 对应 C++ 端 Scripting::UnloadAssetFromScripting。
        /// </summary>
        [FreeFunction("Scripting::UnloadAssetFromScripting")]
        public extern static void UnloadAsset(Object assetToUnload);

        /// <summary>
        /// ECS（Entities）系统使用的资源 GC 根注册机制。
        /// 用于在资源 GC 回收时，将 ECS 中的 InstanceID 标记为根引用，
        /// 防止被 ECS 引用的资源被错误卸载。
        /// </summary>
        internal static class EntitiesAssetGC
        {
            [FreeFunction("Resources_Bindings::MarkInstanceIDsAsRoot")]
            internal extern static void MarkInstanceIDsAsRoot(IntPtr instanceIDs, int count, IntPtr state);

            [FreeFunction("Resources_Bindings::EnableEntitiesAssetGCCallback")]
            internal extern static void EnableEntitiesAssetGCCallback();

            internal delegate void AdditionalRootsHandlerDelegate(IntPtr state);
            internal static AdditionalRootsHandlerDelegate AdditionalRootsHandler;

            /// <summary>
            /// 注册 ECS 额外根处理器。
            /// 只有一个处理器可以被注册，多次注册会触发警告。
            /// 处理器会在资源 GC 标记阶段被调用，用于将 ECS 持有的资源标记为活跃根。
            /// </summary>
            internal static void RegisterAdditionalRootsHandler(AdditionalRootsHandlerDelegate newAdditionalRootsHandler)
            {
                if(AdditionalRootsHandler == null)
                {
                    EnableEntitiesAssetGCCallback();
                    AdditionalRootsHandler = newAdditionalRootsHandler;
                }
                else
                    UnityEngine.Debug.LogWarning("Attempting to register more than one AdditionalRootsHandlerDelegate! Only one may be registered at a time.");
            }

            [RequiredByNativeCode]
            private static void GetAdditionalRoots(IntPtr state)
            {
                if(AdditionalRootsHandler != null)
                    AdditionalRootsHandler(state);
            }
        }
    }

    /// <summary>
    /// Resources API 的可扩展抽象层。
    /// 采用策略模式（Strategy Pattern）设计，允许通过 overrideAPI 替换默认实现。
    /// 内部代码应使用 ActiveAPI 而非直接引用 overrideAPI，以确保在没有 override 时
    /// 能正确回退到默认 API 处理。
    ///
    /// 这种设计允许测试框架或自定义资源管理系统拦截 Resources 调用。
    /// </summary>
    public class ResourcesAPI
    {
        static ResourcesAPI s_DefaultAPI = new ResourcesAPI();

        /// <summary>
        /// 获取当前活跃的 API 实例。
        /// 如果设置了 overrideAPI，则使用它；否则回退到默认实现。
        /// </summary>
        internal static ResourcesAPI ActiveAPI => overrideAPI ?? s_DefaultAPI;

        /// <summary>
        /// 可被替换的 Resources API 实现。
        /// 设置此属性可全局替换 Resources 的行为（用于测试或自定义资源管理）。
        /// </summary>
        public static ResourcesAPI overrideAPI { get; set; }

        protected internal ResourcesAPI() {}
        protected internal virtual Object[] FindObjectsOfTypeAll(Type systemTypeInstance) => ResourcesAPIInternal.FindObjectsOfTypeAll(systemTypeInstance);
        protected internal virtual Shader FindShaderByName(string name) => ResourcesAPIInternal.FindShaderByName(name);
        protected internal virtual Object Load(string path, Type systemTypeInstance) => ResourcesAPIInternal.Load(path, systemTypeInstance);
        protected internal virtual Object[] LoadAll(string path, Type systemTypeInstance) => ResourcesAPIInternal.LoadAll(path, systemTypeInstance);

        /// <summary>
        /// 异步加载资源。
        /// 创建 ResourceRequest 后需设置 m_Path 和 m_Type，以便后续通过 GetResult() 获取结果。
        /// </summary>
        protected internal virtual ResourceRequest LoadAsync(string path, Type systemTypeInstance)
        {
            var req = ResourcesAPIInternal.LoadAsyncInternal(path, systemTypeInstance);
            req.m_Path = path;
            req.m_Type = systemTypeInstance;
            return req;
        }

        protected internal virtual void UnloadAsset(Object assetToUnload) => ResourcesAPIInternal.UnloadAsset(assetToUnload);
    }

    /// <summary>
    /// Resources 类 — 运行时资源加载的核心入口。
    ///
    /// 功能：提供从 "Resources" 文件夹加载资源的静态方法。
    /// 包括同步/异步加载、批量加载、资源卸载和对象查找。
    ///
    /// 设计说明：
    /// - 所有方法均通过 ResourcesAPI.ActiveAPI 转发，支持运行时替换实现。
    /// - 类型安全泛型版本（Load&lt;T&gt;）自动进行类型转换。
    /// - 异步版本返回 ResourceRequest，可通过 yield 或 completed 回调等待。
    ///
    /// 最佳实践：
    /// - 避免在 Update 中频繁调用 Resources.Load，考虑使用引用计数缓存。
    /// - 对于大型项目，建议使用 Addressables 替代 Resources。
    /// - Resources.UnloadUnusedAssets 开销较大，在场景切换时调用为宜。
    /// </summary>
    [NativeHeader("Runtime/Export/Resources/Resources.bindings.h")]
    [NativeHeader("Runtime/Misc/ResourceManagerUtility.h")]
    public sealed partial class Resources
    {
        /// <summary>
        /// 将 Object[] 数组转换为指定类型的泛型数组。
        /// 用于泛型方法如 LoadAll&lt;T&gt; 的返回值转换。
        /// </summary>
        internal static T[] ConvertObjects<T>(Object[] rawObjects) where T : Object
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        /// <summary>
        /// 查找所有已加载的指定类型的对象。
        /// 包括：场景中的对象、Resources 中的对象、DontDestroyOnLoad 中的对象。
        ///
        /// 注意：此方法返回所有实例，包括场景中未激活的对象。
        /// 开销较大，不建议在性能敏感处频繁调用。
        /// </summary>
        public static Object[] FindObjectsOfTypeAll(Type type)
        {
            return ResourcesAPI.ActiveAPI.FindObjectsOfTypeAll(type);
        }

        /// <summary>泛型版本的 FindObjectsOfTypeAll。</summary>
        public static T[] FindObjectsOfTypeAll<T>() where T : Object
        {
            return ConvertObjects<T>(FindObjectsOfTypeAll(typeof(T)));
        }

        /// <summary>
        /// 从 Resources 文件夹同步加载指定路径的资源。
        /// 路径相对于任何 "Resources" 文件夹，不包含文件扩展名。
        /// 例如："Prefabs/MyPrefab"
        ///
        /// 如果多个 Resources 文件夹中有同名资源，返回第一个找到的。
        /// 加载失败会返回 null。
        /// </summary>
        /// <param name="path">资源路径（相对于 Resources 文件夹，不含扩展名）</param>
        /// <returns>加载到的资源对象</returns>
        public static Object Load(string path)
        {
            return Load(path, typeof(Object));
        }

        /// <summary>泛型版本的 Load，避免调用者手动类型转换。</summary>
        public static T Load<T>(string path) where T : Object
        {
            return (T)Load(path, typeof(T));
        }

        /// <summary>指定类型的 Load 重载。</summary>
        public static Object Load(string path, Type systemTypeInstance)
        {
            return ResourcesAPI.ActiveAPI.Load(path, systemTypeInstance);
        }

        /// <summary>
        /// 异步加载资源，返回 ResourceRequest。
        /// 可通过协程 yield return 等待，或注册 completed 回调。
        /// </summary>
        public static ResourceRequest LoadAsync(string path)
        {
            return LoadAsync(path, typeof(Object));
        }

        /// <summary>泛型版本的 LoadAsync。</summary>
        public static ResourceRequest LoadAsync<T>(string path) where T : Object
        {
            return LoadAsync(path, typeof(T));
        }

        /// <summary>指定类型的 LoadAsync 重载。</summary>
        public static ResourceRequest LoadAsync(string path, Type type)
        {
            return ResourcesAPI.ActiveAPI.LoadAsync(path, type);
        }

        /// <summary>
        /// 从 Resources 文件夹加载指定路径下的所有资源。
        /// 如果路径指向一个文件夹，则加载该文件夹内所有资源；
        /// 如果指向一个资源路径，则加载该资源及其所有子资源（如包含多个 Sprite 的 Texture）。
        /// </summary>
        public static Object[] LoadAll(string path, Type systemTypeInstance)
        {
            return ResourcesAPI.ActiveAPI.LoadAll(path, systemTypeInstance);
        }

        /// <summary>不指定类型重载，返回 Object[]。</summary>
        public static Object[] LoadAll(string path)
        {
            return LoadAll(path, typeof(Object));
        }

        /// <summary>泛型版本的 LoadAll。</summary>
        public static T[] LoadAll<T>(string path) where T : Object
        {
            return ConvertObjects<T>(LoadAll(path, typeof(T)));
        }

        /// <summary>
        /// 获取 Unity 内置资源（如内置 Shader、默认材质等）。
        /// 通过 "Resources/unity_builtin_extra" 索引访问。
        /// </summary>
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction("GetScriptingBuiltinResource", ThrowsException = true)]
        extern public static Object GetBuiltinResource([NotNull] Type type, string path);

        /// <summary>泛型版本的 GetBuiltinResource。</summary>
        public static T GetBuiltinResource<T>(string path) where T : Object
        {
            return (T)GetBuiltinResource(typeof(T), path);
        }

        /// <summary>
        /// 从内存中卸载指定的资源对象。
        /// 仅能卸载通过 Resources.Load 加载的资产（如 Texture、Material 等非场景对象）。
        /// 场景对象（GameObject、Component）不能通过此方法卸载。
        /// </summary>
        public static void UnloadAsset(Object assetToUnload)
        {
            ResourcesAPI.ActiveAPI.UnloadAsset(assetToUnload);
        }

        [FreeFunction("Scripting::UnloadAssetFromScripting")]
        extern static void UnloadAssetImplResourceManager(Object assetToUnload);

        /// <summary>
        /// 卸载所有未被引用的资源。
        /// 执行完整的资源 GC 回收，标记-清除所有不再被引用的资产。
        /// 返回 AsyncOperation，可在场景切换等时机调用。
        ///
        /// 注意：此操作开销较大（会遍历所有对象引用），建议在场景加载完成后调用。
        /// 频繁调用会导致性能问题。
        /// </summary>
        [FreeFunction("Resources_Bindings::UnloadUnusedAssets")]
        extern public static AsyncOperation UnloadUnusedAssets();

        /// <summary>通过 EntityId 获取对应的 Object（用于 ECS 互操作）。</summary>
        [FreeFunction("Resources_Bindings::InstanceIDToObject")]
        public extern static Object EntityIdToObject(EntityId entityId);

        [Obsolete("InstanceIDToObject is obsolete. Use EntityIdToObject instead.", true)]
        public static Object InstanceIDToObject(int instanceID)
        {
            return EntityIdToObject(instanceID);
        }

        /// <summary>检查指定 EntityId 对应的对象是否已加载到内存中。</summary>
        [FreeFunction("Resources_Bindings::IsInstanceLoaded")]
        internal extern static bool IsObjectLoaded(EntityId entityId);

        internal static bool IsInstanceLoaded(EntityId entityId)
        {
            return IsObjectLoaded(entityId);
        }

        /// <summary>
        /// 将一批 InstanceID 转换为对应的 Object 数组。
        /// 线程安全版本（IsThreadSafe = true），可在 Job 系统中使用。
        /// </summary>
        [FreeFunction("Resources_Bindings::InstanceIDToObjectArray", IsThreadSafe = true)]
        extern private static void InstanceIDToObjectArray(IntPtr instanceIDs, int instanceCount, [Out, NotNull] Object[] objects);

        /// <summary>将 NativeArray&lt;EntityId&gt; 转换为 Object 数组。</summary>
        internal static unsafe void InstanceIDToObjectArray(NativeArray<EntityId> instanceIDs, Object[] objects)
        {
            if (!instanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(instanceIDs));
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));
            if (objects.Length < instanceIDs.Length)
                throw new ArgumentException("Output array is too small.", nameof(objects));

            if (instanceIDs.Length == 0)
                return;

            InstanceIDToObjectArray((IntPtr)instanceIDs.GetUnsafeReadOnlyPtr(), instanceIDs.Length, objects);
        }

        /// <summary>将 EntityId 数组转换为 Object 列表（线程安全）。</summary>
        [FreeFunction("Resources_Bindings::InstanceIDToObjectList", IsThreadSafe = true)]
        extern private static void EntityIdsToObjectList(IntPtr entityIds, int instanceCount, [Out,NotNull] List<Object> objects);

        /// <summary>将 NativeArray&lt;EntityId&gt; 转换为 List&lt;Object&gt;。</summary>
        public static unsafe void EntityIdsToObjectList(NativeArray<EntityId> entityIds, List<Object> objects)
        {
            if (!entityIds.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(entityIds));
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));
            if (entityIds.Length == 0)
            {
                objects.Clear();
                return;
            }
            EntityIdsToObjectList((IntPtr)entityIds.GetUnsafeReadOnlyPtr(), entityIds.Length, objects);
        }

        [Obsolete("InstanceIDToObjectList is obsolete. Use EntityIdsToObjectList instead.", true)]
        public static unsafe void InstanceIDToObjectList(NativeArray<int> instanceIDs, List<Object> objects) => throw new NotImplementedException("InstanceIDToObjectList is deprecated. Use InstanceIDToObjectList instead.");

        /// <summary>判断指定的一组实例 ID 对应的对象是否有效（线程安全）。</summary>
        [FreeFunction("Resources_Bindings::InstanceIDsToValidArray", IsThreadSafe = true)]
        private static extern unsafe void InstanceIDsToValidArray_Internal(IntPtr instanceIDs, int instanceCount, IntPtr validArray, int validArrayCount);

        /// <summary>检查 EntityId 是否有效（对象是否仍然存在）。</summary>
        [FreeFunction("Resources_Bindings::DoesObjectWithInstanceIDExist", IsThreadSafe = true)]
        public static extern bool EntityIdIsValid(EntityId entityId);

        [Obsolete("InstanceIDIsValid is obsolete. Use EntityIdIsValid instead.", true)]
        public static bool InstanceIDIsValid(int instanceId)
        {
            return EntityIdIsValid(instanceId);
        }

        [Obsolete("InstanceIDsToValidArray is obsolete. Use EntityIdsToValidArray instead.", true)]
        public static unsafe void InstanceIDsToValidArray(NativeArray<int> instanceIDs, NativeArray<bool> validArray) => throw new NotImplementedException("InstanceIDsToValidArray is deprecated. Use InstanceIDsToValidArray instead.");

        /// <summary>批量检查多个 EntityId 的有效性，将结果写入 validArray。</summary>
        public static unsafe void EntityIdsToValidArray(NativeArray<EntityId> entityIDs, NativeArray<bool> validArray)
        {
            if (!entityIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(entityIDs));
            if (!validArray.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(validArray));
            if (entityIDs.Length != validArray.Length)
                throw new ArgumentException("Size mismatch! Both arrays must be the same length.");
            if(entityIDs.Length == 0)
                return;

            InstanceIDsToValidArray_Internal((IntPtr)entityIDs.GetUnsafeReadOnlyPtr(), entityIDs.Length, (IntPtr)validArray.GetUnsafePtr(), validArray.Length);
        }

        [Obsolete("InstanceIDsToValidArray is obsolete. Use EntityIdsToValidArray instead.", true)]
        public static unsafe void InstanceIDsToValidArray(ReadOnlySpan<int> instanceIDs, Span<bool> validArray) =>
            throw new NotImplementedException("InstanceIDsToValidArray is deprecated. Use InstanceIDsToValidArray instead.");

        /// <summary>使用 ReadOnlySpan 版本的批量有效性检查。</summary>
        public static unsafe void EntityIdsToValidArray(ReadOnlySpan<EntityId> entityIds, Span<bool> validArray)
        {
            if(entityIds.Length != validArray.Length)
                throw new ArgumentException("Size mismatch! Both arrays must be the same length.");
            if(entityIds.Length == 0)
                return;

            fixed(EntityId* entityIdsPtr = entityIds)
            fixed(bool* validArrayPtr = validArray)
            {
                InstanceIDsToValidArray_Internal((IntPtr)entityIdsPtr, entityIds.Length, (IntPtr)validArrayPtr, validArray.Length);
            }
        }
    }
}
