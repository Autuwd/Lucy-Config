// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 GameObject — Unity 场景中的基本实体
//
// 📌 作用：
//   GameObject 是 Unity 场景中所有实体的容器。
//   它本身没有行为，而是通过挂载 Component 来获得功能。
//   类似于一个"空盒子"，你往里面放各种组件来构建游戏对象。
//
// 🏗 核心概念：
//   GameObject = 实体容器
//   Component  = 容器中的功能模块
//   Transform  = 每个 GameObject 都必须有的位置组件（由引擎自动管理）
//
// 💡 理解关键：
//   GameObject 继承自 Object，所以它也有 EntityId 和伪 null 机制。
//   GameObject 是 sealed class，不能被继承。
//   所有 GameObject 在创建时自动附带一个 Transform 组件。
//
// 📍 对应 C++ 头文件：Runtime/Export/Scripting/GameObject.bindings.h
// ==============================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.SceneManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace UnityEngine
{
    // ==============================================================
    // GameObject — Unity 场景中的基本实体单元
    //
    // 🔑 关键属性：
    //   - transform:  每个 GameObject 自动附带的 Transform 组件
    //   - scene:      对象所在的场景
    //   - activeSelf/activeInHierarchy: 激活状态
    //   - layer/tag:  层级和标签（用于碰撞检测、渲染筛选）
    //
    // 🔑 关键方法：
    //   - GetComponent<T>() / AddComponent<T>()：组件管理
    //   - SetActive()：激活/停用
    //   - SendMessage/BroadcastMessage：消息传递
    //
    // 💡 GameObject 与 Component 的关系：
    //   GameObject 是"容器"，Component 是"内容"。
    //   你可以把 GameObject 想象成一个架子，上面可以放各种工具（组件）。
    //   Transform 是每个架子上默认自带的工具。
    // ==============================================================
    [ExcludeFromPreset]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Export/Scripting/GameObject.bindings.h")]
    public sealed partial class GameObject : Object
    {
        [FreeFunction("GameObjectBindings::CreatePrimitive")]
        public extern static GameObject CreatePrimitive(PrimitiveType type);

        // ==============================================================
        // GetComponent<T>() — 获取指定类型的组件（泛型版本）
        //
        // 🔧 内部机制：
        //   1. 调用 C++ 端的 GetComponentFastPath 快速查找
        //   2. 使用 UnsafeUtility.As 进行类型转换
        //      （因为 C# 泛型没有约束 T 必须是 class）
        //   3. 如果值类型传入，UnsafeUtility.As 可以安全处理
        //
        // ⚡ 性能优化：
        //   相比 GetComponent(typeof(T)) 的泛型重载，
        //   这个版本通过 UnsafeUtility.As 避免了装箱和类型转换开销。
        // ==============================================================
        [System.Security.SecuritySafeCritical]
        public unsafe T GetComponent<T>()
        {
            var component = GetComponentFastPath(typeof(T));
            // Because there is no constraint on T, a user could pass a value type that is larger than a reference
            // If so we need to ensure that the ref we pass to UnsafeUtility.As is larger enough so we don't read
            // random stack data. In that case we can assume that component will null, so we will return a zero'd T
            var tuple = (component, default(T));
            return UnsafeUtility.As<Component, T>(ref tuple.component);
        }

        // ==============================================================
        // GetComponent(Type) — 通过 System.Type 获取组件
        //
        // 🎯 这是所有 GetComponent 重载最终调用的 Native 方法。
        // 直接在 C++ 端的 GameObject 组件列表中查找匹配的组件。
        // [ThrowsException = true] 表示如果类型不存在会抛异常。
        // ==============================================================
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction(Name = "GameObjectBindings::GetComponentFromType", HasExplicitThis = true, ThrowsException = true)]
        public extern Component GetComponent(Type type);

        // ⚡ 快速路径 — 通过运行时类型句柄查找，跳过一些编辑器特定检查
        [FreeFunction(Name = "GameObjectBindings::GetComponentFastPath", HasExplicitThis = true, ThrowsException = true)]
        internal extern Component GetComponentFastPath(Type type);

        // 通过字符串名称查找组件（不区分大小写）
        [FreeFunction(Name = "Scripting::GetScriptingWrapperOfComponentOfGameObject", HasExplicitThis = true)]
        internal extern Component GetComponentByName(string type);

        // 通过字符串名称查找组件（可指定是否大小写敏感）
        [FreeFunction(Name = "Scripting::GetScriptingWrapperOfComponentOfGameObjectWithCase", HasExplicitThis = true)]
        internal extern Component GetComponentByNameWithCase(string type, bool caseSensitive);

        public Component GetComponent(string type)
        {
            return GetComponentByName(type);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction(Name = "GameObjectBindings::GetComponentInChildren", HasExplicitThis = true, ThrowsException = true)]
        public extern Component GetComponentInChildren(Type type, bool includeInactive);

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponentInChildren(Type type)
        {
            return GetComponentInChildren(type, false);
        }

        [uei.ExcludeFromDocs]
        public T GetComponentInChildren<T>()
        {
            bool includeInactive = false;
            return GetComponentInChildren<T>(includeInactive);
        }

        public T GetComponentInChildren<T>([uei.DefaultValue("false")] bool includeInactive)
        {
            return (T)(object)GetComponentInChildren(typeof(T), includeInactive);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction(Name = "GameObjectBindings::GetComponentInParent", HasExplicitThis = true, ThrowsException = true)]
        public extern Component GetComponentInParent(Type type, bool includeInactive);

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponentInParent(Type type)
        {
            return GetComponentInParent(type, false);
        }

        [uei.ExcludeFromDocs]
        public T GetComponentInParent<T>()
        {
            bool includeInactive = false;
            return GetComponentInParent<T>(includeInactive);
        }

        public T GetComponentInParent<T>([uei.DefaultValue("false")] bool includeInactive)
        {
            return (T)(object)GetComponentInParent(typeof(T), includeInactive);
        }

        [FreeFunction(Name = "GameObjectBindings::GetComponentsInternal", HasExplicitThis = true, ThrowsException = true)]
        private extern System.Array GetComponentsInternal(Type type, bool useSearchTypeAsArrayReturnType, bool recursive, bool includeInactive, bool reverse, object resultList);

        private System.Array GetComponentsInternal<T>(bool useSearchTypeAsArrayReturnType, bool recursive, bool includeInactive, bool reverse, [Out] List<T> resultList)
        {
            // Use UnsafeUtility.As to cast the list to the appropriate type
            // This could be a problem if native code provides us a type that does not match
            return GetComponentsInternal(typeof(T), useSearchTypeAsArrayReturnType, recursive, includeInactive, reverse, UnsafeUtility.As<List<Component>>(resultList));
        }

        public Component[] GetComponents(Type type)
        {
            return (Component[])GetComponentsInternal(type, false, false, true, false, null);
        }

        public T[] GetComponents<T>()
        {
            return (T[])GetComponentsInternal<T>(true, false, true, false, null);
        }

        public void GetComponents(Type type, List<Component> results)
        {
            GetComponentsInternal(type, false, false, true, false, results);
        }

        public void GetComponents<T>(List<T> results)
        {
            GetComponentsInternal<T>(true, false, true, false, results);
        }

        [uei.ExcludeFromDocs]
        public Component[] GetComponentsInChildren(Type type)
        {
            bool includeInactive = false;
            return GetComponentsInChildren(type, includeInactive);
        }

        public Component[] GetComponentsInChildren(Type type, [uei.DefaultValue("false")] bool includeInactive)
        {
            return (Component[])GetComponentsInternal(type, false, true, includeInactive, false, null);
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            return (T[])GetComponentsInternal<T>(true, true, includeInactive, false, null);
        }

        public void GetComponentsInChildren<T>(bool includeInactive, List<T> results)
        {
            GetComponentsInternal<T>(true, true, includeInactive, false, results);
        }

        public T[] GetComponentsInChildren<T>()
        {
            return GetComponentsInChildren<T>(false);
        }

        public void GetComponentsInChildren<T>(List<T> results)
        {
            GetComponentsInChildren<T>(false, results);
        }

        [uei.ExcludeFromDocs]
        public Component[] GetComponentsInParent(Type type)
        {
            bool includeInactive = false;
            return GetComponentsInParent(type, includeInactive);
        }

        public Component[] GetComponentsInParent(Type type, [uei.DefaultValue("false")] bool includeInactive)
        {
            return (Component[])GetComponentsInternal(type, false, true, includeInactive, true, null);
        }

        public void GetComponentsInParent<T>(bool includeInactive, List<T> results)
        {
            GetComponentsInternal<T>(true, true, includeInactive, true, results);
        }

        public T[] GetComponentsInParent<T>(bool includeInactive)
        {
            return (T[])GetComponentsInternal<T>(true, true, includeInactive, true, null);
        }

        public T[] GetComponentsInParent<T>()
        {
            return GetComponentsInParent<T>(false);
        }

        [System.Security.SecuritySafeCritical]
        public unsafe bool TryGetComponent<T>(out T component)
        {
            var componentFound = TryGetComponentFastPath(typeof(T));
            // Because there is no constraint on T, a user could pass a value type that is larger than a reference
            // If so we need to ensure that the ref we pass to UnsafeUtility.As is larger enough so we don't read
            // random stack data. In that case we can assume that component will null, so we will return a zero'd T
            var tuple = (componentFound, default(T));
            component = UnsafeUtility.As<Component, T>(ref tuple.componentFound);
            return component != null;

        }

        public bool TryGetComponent(Type type, out Component component)
        {
            component = TryGetComponentInternal(type);
            return component != null;
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction(Name = "GameObjectBindings::TryGetComponentFromType", HasExplicitThis = true, ThrowsException = true)]
        internal extern Component TryGetComponentInternal(Type type);

        [FreeFunction(Name = "GameObjectBindings::TryGetComponentFastPath", HasExplicitThis = true, ThrowsException = true)]
        internal extern Component TryGetComponentFastPath(Type type);

        public static GameObject FindWithTag(string tag)
        {
            return FindGameObjectWithTag(tag);
        }

        [FreeFunction(Name = "GameObjectBindings::FindGameObjectsWithTagForListInternal", ThrowsException = true)]
        private static extern void FindGameObjectsWithTagForListInternal(string tag, [Out, NotNull] List<GameObject> results);

        public static void FindGameObjectsWithTag(string tag, List<GameObject> results)
        {
            FindGameObjectsWithTagForListInternal(tag, results);
        }

        public void SendMessageUpwards(string methodName, SendMessageOptions options)
        {
            SendMessageUpwards(methodName, null, options);
        }

        public void SendMessage(string methodName, SendMessageOptions options)
        {
            SendMessage(methodName, null, options);
        }

        public void BroadcastMessage(string methodName, SendMessageOptions options)
        {
            BroadcastMessage(methodName, null, options);
        }

        [FreeFunction(Name = "MonoAddComponent", HasExplicitThis = true)]
        internal extern Component AddComponentInternal(string className);

        [FreeFunction(Name = "MonoAddComponentWithType", HasExplicitThis = true)]
        private extern Component Internal_AddComponentWithType(Type componentType);

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component AddComponent(Type componentType)
        {
            return Internal_AddComponentWithType(componentType);
        }

        public T AddComponent<T>() where T : Component
        {
            return AddComponent(typeof(T)) as T;
        }

        public extern int GetComponentCount();

        [NativeName("QueryComponentAtIndex<Unity::Component>")]
        internal extern Component QueryComponentAtIndex(int index);

        public Component GetComponentAtIndex(int index)
        {
            if (index < 0 || index >= GetComponentCount()) throw new ArgumentOutOfRangeException(nameof(index), "Valid range is 0 to GetComponentCount() - 1.");
            return QueryComponentAtIndex(index);
        }

        public T GetComponentAtIndex<T>(int index) where T : Component
        {
            T component = (T)GetComponentAtIndex(index);
            if(component == null) throw new InvalidCastException();
            return component;
        }

        public extern int GetComponentIndex(Component component);


        // ==============================================================
        // transform — 获取当前 GameObject 的 Transform 组件
        //
        // 💡 每个 GameObject 都"自带"一个 Transform 组件，
        //    这是引擎自动创建和管理的，不能被删除。
        //    这个属性是获取它的最快捷方式。
        //
        // 🆕 TransformHandle — EntityId 驱动的轻量级 Transform 引用
        //    用于 Job System 中安全地访问 Transform 数据。
        // ==============================================================
        public extern Transform transform
        {
            [FreeFunction("GameObjectBindings::GetTransform", HasExplicitThis = true)]
            get;
        }

        public extern TransformHandle transformHandle
        {
            [FreeFunction("GameObjectBindings::GetTransformHandle", HasExplicitThis = true)]
            get;
        }

        // ==============================================================
        // layer — 游戏对象的渲染/物理层级
        //
        // 用于：
        //   - 摄像机 Culling Mask（只渲染某些层）
        //   - 物理碰撞检测 Layer Collision Matrix
        //   - 光线投射只检测特定层
        // 值范围：0-31（32 个层）
        // ==============================================================
        public extern int layer { get; set; }

        // ==============================================================
        // SetActive() — 激活/停用 GameObject
        //
        // 🌟 这是 Unity 中控制对象是否"工作"的核心方法。
        //
        // 🔄 影响链：
        //   SetActive(false)
        //   → activeSelf = false
        //   → activeInHierarchy = false（如果有父级未激活，也为 false）
        //   → 所有 Component 的 OnEnable() 被调用（激活时）
        //   → 所有 Component 的 OnDisable() 被调用（停用时）
        //
        // 💡 activeSelf vs activeInHierarchy:
        //   activeSelf = 自己的 SetActive 设置的
        //   activeInHierarchy = 自己 AND 所有父级都 active
        // ==============================================================
        [NativeMethod(Name = "SetSelfActive")]
        public extern void SetActive(bool value);

        // 自己本身的激活状态（不受父级影响）
        public extern bool activeSelf
        {
            [NativeMethod(Name = "IsSelfActive")]
            get;
        }

        // 在层级中的最终激活状态（受父级影响）
        public extern bool activeInHierarchy
        {
            [NativeMethod(Name = "IsActive")]
            get;
        }

        // ==============================================================
        // isStatic — 静态标记（用于静态批处理、光照贴图等）
        //
        // ⚠️ 性能优化标记：
        //   标记为 static 的 GameObject 运行时不会移动，
        //   引擎可以对其进行静态合并优化（Static Batching）。
        //   已废弃，建议使用 Static Editor Flags 替代。
        // ==============================================================
        public extern bool isStatic
        {
            [NativeMethod(Name = "GetIsStaticDeprecated")]
            get;
            [NativeMethod(Name = "SetIsStaticDeprecated")]
            set;
        }

        // 静态批处理内部使用
        internal extern bool isStaticBatchable
        {
            [NativeMethod(Name = "IsStaticBatchable")]
            get;
        }

        // ==============================================================
        // tag — 游戏对象的标签
        //
        // 用于快速标识和查找对象。
        // Tag 在 Project Settings 中预定义，不是任意字符串。
        // 常见用法：
        //   - GameObject.FindWithTag("Player")
        //   - 碰撞检测时比较 tag
        //   - 区分同类对象的不同角色
        // ==============================================================
        public extern string tag
        {
            [FreeFunction("GameObjectBindings::GetTag", HasExplicitThis = true)]
            get;
            [FreeFunction("GameObjectBindings::SetTag", HasExplicitThis = true)]
            set;
        }

        // 比较 tag（字符串版本 vs TagHandle 版本）
        // TagHandle 是轻量级的 tag 比较方式，避免字符串分配
        public bool CompareTag(string tag) => CompareTag_Internal(tag);
        public bool CompareTag(TagHandle tag) => CompareTagHandle_Internal(tag);

        [FreeFunction(Name = "GameObjectBindings::CompareTag", HasExplicitThis = true)]
        private extern bool CompareTag_Internal(string tag);

        [FreeFunction(Name = "GameObjectBindings::CompareTagHandle", HasExplicitThis = true)]
        private extern bool CompareTagHandle_Internal(TagHandle tag);

        // 通过 Tag 查找 GameObjects（场景中所有带该 Tag 的对象）
        [FreeFunction(Name = "GameObjectBindings::FindGameObjectWithTag", ThrowsException = true)]
        public static extern GameObject FindGameObjectWithTag(string tag);

        [FreeFunction(Name = "GameObjectBindings::FindGameObjectsWithTag", ThrowsException = true)]
        public static extern GameObject[] FindGameObjectsWithTag(string tag);

        [FreeFunction(Name = "Scripting::SendScriptingMessageUpwards", HasExplicitThis = true)]
        extern public void SendMessageUpwards(string methodName, [uei.DefaultValue("null")] object value, [uei.DefaultValue("SendMessageOptions.RequireReceiver")] SendMessageOptions options);

        [uei.ExcludeFromDocs]
        public void SendMessageUpwards(string methodName, object value)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            SendMessageUpwards(methodName, value, options);
        }

        [uei.ExcludeFromDocs]
        public void SendMessageUpwards(string methodName)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            object value = null;
            SendMessageUpwards(methodName, value, options);
        }

        [FreeFunction(Name = "Scripting::SendScriptingMessage", HasExplicitThis = true)]
        extern public void SendMessage(string methodName, [uei.DefaultValue("null")] object value, [uei.DefaultValue("SendMessageOptions.RequireReceiver")] SendMessageOptions options);

        [uei.ExcludeFromDocs]
        public void SendMessage(string methodName, object value)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            SendMessage(methodName, value, options);
        }

        [uei.ExcludeFromDocs]
        public void SendMessage(string methodName)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            object value = null;
            SendMessage(methodName, value, options);
        }

        [FreeFunction(Name = "Scripting::BroadcastScriptingMessage", HasExplicitThis = true)]
        extern public void BroadcastMessage(string methodName, [uei.DefaultValue("null")] object parameter, [uei.DefaultValue("SendMessageOptions.RequireReceiver")] SendMessageOptions options);

        [uei.ExcludeFromDocs]
        public void BroadcastMessage(string methodName, object parameter)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            BroadcastMessage(methodName, parameter, options);
        }

        [uei.ExcludeFromDocs]
        public void BroadcastMessage(string methodName)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            object parameter = null;
            BroadcastMessage(methodName, parameter, options);
        }

        public GameObject(string name)
        {
            Internal_CreateGameObject(this, name);
        }

        public GameObject()
        {
            Internal_CreateGameObject(this, null);
        }

        public GameObject(string name, params Type[] components)
        {
            Internal_CreateGameObject(this, name);
            foreach (Type t in components)
                AddComponent(t);
        }

        [FreeFunction(Name = "GameObjectBindings::Internal_CreateGameObject")]
        static extern void Internal_CreateGameObject([Writable] GameObject self, string name);

        [FreeFunction(Name = "GameObjectBindings::Find")]
        public static extern GameObject Find(string name);

        [FreeFunction(Name = "GameObjectBindings::SetGameObjectsActiveByInstanceID")]
        extern private static void SetGameObjectsActive(IntPtr instanceIds, int instanceCount, bool active);

        [Obsolete("Obsolete. Please use GameObject.SetGameObjectsActive(NativeArray<EntityId>, bool) instead.", true)]
        public static unsafe void SetGameObjectsActive(NativeArray<int> instanceIDs, bool active)
            => throw new NotImplementedException("Please use SetGameObjectsActive(NativeArray<EntityId>, bool) instead.");

        public static unsafe void SetGameObjectsActive(NativeArray<EntityId> entityIds, bool active)
        {
            if (!entityIds.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(entityIds));

            if (entityIds.Length == 0)
                return;

            SetGameObjectsActive((IntPtr)entityIds.GetUnsafeReadOnlyPtr(), entityIds.Length, active);
        }

        [Obsolete("Obsolete. Please use GameObject.SetGameObjectsActive(ReadOnlySpan<EntityId>, bool) instead.", true)]
        public static unsafe void SetGameObjectsActive(ReadOnlySpan<int> instanceIDs, bool active) => throw new NotImplementedException("Deprecated. Please use SetGameObjectsActive(ReadOnlySpan<EntityId>, bool) instead.");

        public static unsafe void SetGameObjectsActive(ReadOnlySpan<EntityId> entityIds, bool active)
        {
            if (entityIds.Length == 0)
                return;

            fixed (EntityId* instanceIDsPtr = entityIds)
            {
                SetGameObjectsActive((IntPtr)instanceIDsPtr, entityIds.Length, active);
            }
        }

        [FreeFunction("GameObjectBindings::InstantiateGameObjectsByInstanceID")]
        extern private static void InstantiateGameObjects(EntityId sourceInstanceID, IntPtr newInstanceIDs, IntPtr newTransformInstanceIDs, int count, Scene destinationScene);

        [Obsolete("Obsolete. Please use GameObject.InstantiateGameObjects(EntityId, int, NativeArray<EntityId>, NativeArray<EntityId>, Scene) instead.", true)]
        public static unsafe void InstantiateGameObjects(int sourceInstanceID, int count, NativeArray<int> newInstanceIDs, NativeArray<int> newTransformInstanceIDs, Scene destinationScene = default)
            => throw new NotImplementedException("Deprecated. Please use GameObject.InstantiateGameObjects(EntityId, int, NativeArray<EntityId>, NativeArray<EntityId>, Scene) instead.");

        public static unsafe void InstantiateGameObjects(EntityId sourceEntityId, int count, NativeArray<EntityId> newEntityIds, NativeArray<EntityId> newTransformEntityIds, Scene destinationScene = default)
        {
            if (!newEntityIds.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(newEntityIds));
            if (!newTransformEntityIds.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(newTransformEntityIds));
            if (count == 0)
                return;
            if ((count != newEntityIds.Length) || (count != newTransformEntityIds.Length))
                throw new ArgumentException("Size mismatch! Both arrays must already be the size of count.");

            InstantiateGameObjects(sourceEntityId, (IntPtr)newEntityIds.GetUnsafeReadOnlyPtr(), (IntPtr)newTransformEntityIds.GetUnsafeReadOnlyPtr(), newEntityIds.Length, destinationScene);
        }

        [FreeFunction(Name = "GameObjectBindings::GetSceneByEntityId")]
        static extern Scene GetSceneInternal(EntityId entityId);
        public static Scene GetScene(EntityId entityId) => GetSceneInternal(entityId);

        public extern Scene scene
        {
            [FreeFunction("GameObjectBindings::GetScene", HasExplicitThis = true)]
            get;
        }

        public extern ulong sceneCullingMask
        {
            [FreeFunction(Name = "GameObjectBindings::GetSceneCullingMask", HasExplicitThis = true)]
            get;
        }

        [FreeFunction(Name = "GameObjectBindings::CalculateBounds", HasExplicitThis = true)]
        internal extern Bounds CalculateBounds();

        internal extern int IsMarkedVisible();

        public GameObject gameObject { get { return this; } }

        public extern bool IsDestroying();
    }
}
