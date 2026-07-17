// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Loading;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.SceneManagement
{
    //=============================================================================
    // Scene 结构体 —— 场景句柄的 C# 值类型包装
    //
    // 设计说明:
    //   Scene 是一个轻量级的值类型（struct），本质上是 SceneHandle 的包装器。
    //   它不包含场景的实际数据，所有数据都存储在 C++ 侧的场景管理器中。
    //   这使得 Scene 可以像"指针"一样被传递和比较，而不会产生大量数据复制。
    //
    //   SceneHandle 是底层句柄，封装了一个 EntityId，用于在 C++ 侧唯一标识一个场景实例。
    //   这种设计是 Unity 的"句柄模式"(Handle Pattern) 的典型应用：
    //   - 托管侧（C#）：传递轻量级句柄
    //   - 非托管侧（C++）：通过句柄查找实际数据
    //
    // 值类型语义:
    //   Scene 是值类型，意味着:
    //   1. 按值传递，不会产生 GC 压力
    //   2. 比较两个 Scene 变量就是比较它们的句柄值（m_Handle）
    //   3. 可以使用 default(Scene) 创建"空"场景引用
    //   4. 始终需要通过 IsValid() 检查场景引用是否有效
    //=============================================================================

    /// <summary>
    /// 场景句柄的绑定方法容器。
    /// 所有与 Scene 相关的 C++ 原生接口都定义在此，通过 P/Invoke 绑定到 SceneBindings 类。
    /// 每个方法都以 SceneHandle 作为核心参数来索引具体场景。
    /// </summary>
    [NativeHeader("Runtime/Export/SceneManager/Scene.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Scene
    {
        //===========================================================================
        // 下面所有方法都是静态的 extern 方法，通过 Native Bindings 调用 C++ 代码。
        // 每个方法都接收 SceneHandle 作为第一个参数，用于在 C++ 侧定位场景实例。
        //===========================================================================

        /// <summary>检查场景句柄是否有效的内部方法</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsValidInternal(SceneHandle sceneHandle);

        /// <summary>获取场景文件路径的内部方法（如 "Assets/Scenes/MainScene.unity"）</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static string GetPathInternal(SceneHandle sceneHandle);

        /// <summary>设置场景路径和 GUID 的内部方法（用于自定义场景）</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetPathAndGUIDInternal(SceneHandle sceneHandle, string path, string guid);

        /// <summary>获取场景名称的内部方法（不含路径和扩展名的文件名）</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static string GetNameInternal(SceneHandle sceneHandle);

        /// <summary>设置场景名称的内部方法</summary>
        [NativeMethod(ThrowsException = true)]
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetNameInternal(SceneHandle sceneHandle, string name);

        /// <summary>获取场景 GUID 的内部方法</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static string GetGUIDInternal(SceneHandle sceneHandle);

        /// <summary>获取场景的 LoadableSceneId（可加载场景标识符）</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static LoadableSceneId GetLoadableSceneIdInternal(SceneHandle sceneHandle);

        /// <summary>检查场景是否为子场景（SubScene）</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsSubScene(SceneHandle sceneHandle);

        /// <summary>设置场景是否为子场景</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetIsSubScene(SceneHandle sceneHandle, bool value);

        /// <summary>获取场景是否已完全加载</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool GetIsLoadedInternal(SceneHandle sceneHandle);

        /// <summary>获取场景的加载状态（NotLoading/Loading/Loaded/Unloading 等）</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static Scene.LoadingState GetLoadingStateInternal(SceneHandle sceneHandle);

        /// <summary>检查场景是否有未保存的修改</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool GetIsDirtyInternal(SceneHandle sceneHandle);

        /// <summary>获取场景的脏标记 ID（用于编辑器撤消/重做系统）</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static int GetDirtyID(SceneHandle sceneHandle);

        /// <summary>获取场景在 Build Settings 中的索引</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static int GetBuildIndexInternal(SceneHandle sceneHandle);

        /// <summary>获取场景根游戏对象的数量</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static int GetRootCountInternal(SceneHandle sceneHandle);

        /// <summary>以 List 形式获取场景的所有根游戏对象</summary>
        [NativeMethod("GetRootGameObjectsInternal")]
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void GetRootGameObjectsInternalList(SceneHandle sceneHandle, [Out] List<GameObject> resultRootList);

        /// <summary>以数组形式获取场景的所有根游戏对象</summary>
        [NativeMethod("GetRootGameObjectsInternal")]
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void GetRootGameObjectsInternalArray(SceneHandle sceneHandle, [Out] GameObject[] resultRootArray);

        /// <summary>获取场景的默认父节点 EntityId（ECS 相关）</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static EntityId GetDefaultParent(SceneHandle sceneHandle);

        /// <summary>设置场景的默认父节点 EntityId（ECS 相关）</summary>
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetDefaultParent(SceneHandle sceneHandle, EntityId value);
    }

    /// <summary>
    /// 场景句柄 —— 对底层 EntityId 的轻量级值类型包装。
    /// 
    /// SceneHandle 是 C++ 侧 UnitySceneHandle 在 C# 侧的对应结构，
    /// 用于在托管代码和非托管代码之间传递场景引用。
    /// 
    /// 设计要点:
    ///   1. 序列化友好: 标记为 [Serializable]，可在编辑器状态下序列化
    ///   2. 值类型语义: 实现了 IEquatable，支持 == / != 比较
    ///   3. 向后兼容: 保留了隐式类型转换操作符（已废弃），迁移到 GetRawData()/FromRawData() API
    ///   4. ECS 集成: 底层使用 EntityId 存储，可与 Unity ECS 系统互操作
    ///
    /// 注意:
    ///   在 Unity 的 DOTS/ECS 架构迁移过程中，SceneHandle 从 int 升级为 EntityId 存储，
    ///   以适应未来的 64 位场景 ID 需求和 ECS 集成。
    ///   因此旧的 int/uint 隐式转换已被标记为 Obsolete(true)。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [Serializable]
    [NativeHeader("Runtime/SceneManager/UnitySceneHandle.h")]
    [NativeClass("UnitySceneHandle")]
    public struct SceneHandle : IEquatable<SceneHandle>, IFormattable
    {
        /// <summary>
        /// 底层存储的实体 ID。
        /// 该字段是 SceneHandle 的核心数据，直接映射到 C++ 侧的 UnitySceneHandle。
        /// 在内存布局中，SceneHandle 的内存布局完全等同于 EntityId。
        /// </summary>
        internal EntityId m_Value;

        /// <summary>获取一个空的（无效的）SceneHandle 实例。</summary>
        public static SceneHandle None => default;

        /// <summary>从 EntityId 创建 SceneHandle 的内部工厂方法。</summary>
        internal static SceneHandle From(EntityId entityId) => new() { m_Value = entityId };

        // === 相等性比较 ===
        public override bool Equals(object obj) => obj is SceneHandle other && Equals(other);
        public bool Equals(SceneHandle other) => m_Value == other.m_Value;
        public static bool operator ==(SceneHandle left, SceneHandle right) => left.Equals(right);
        public static bool operator !=(SceneHandle left, SceneHandle right) => !left.Equals(right);

        // === 废弃的类型转换（从 int/uint 隐式转换） ===
        // 这些操作符已废弃，因为 SceneHandle 从 int 升级为 EntityId（64 位兼容），
        // 隐式转换到 int 会丢失精度。
        // 请使用 GetRawData() 获取原始 ulong 值，或使用 FromRawData() 构造。
        [Obsolete("Implicit conversion from SceneHandle to int is deprecated. Use SceneHandle.GetRawData() instead", true)]
        public static implicit operator int(SceneHandle handle) => handle.m_Value;
        [Obsolete("Implicit conversion from int to SceneHandle is deprecated. Use SceneHandle.FromRawData(ulong) instead", true)]
        public static implicit operator SceneHandle(int handle) => FromRawData((ulong)handle);
        [Obsolete("Implicit conversion from SceneHandle to uint is deprecated. Use SceneHandle.GetRawData() instead", true)]
        public static implicit operator uint(SceneHandle handle) => (uint)(int)handle.m_Value;
        [Obsolete("Implicit conversion from uint to SceneHandle is deprecated. Use SceneHandle.FromRawData(ulong) instead", true)]
        public static implicit operator SceneHandle(uint handle) => FromRawData(handle);

        // === 哈希与字符串 ===
        public override int GetHashCode() => m_Value.GetHashCode();
        public override string ToString() => m_Value.ToString();
        public string ToString(string format) => m_Value.ToString(format);
        public string ToString(string format, IFormatProvider formatProvider) => m_Value.ToString(format, formatProvider);

        // === 数据访问 ===
        internal EntityId ToEntityId() => m_Value;

        /// <summary>
        /// 获取原始数据（ulong 格式）。
        /// 用于序列化、网络传输或底层存储。
        /// </summary>
        public ulong GetRawData() => EntityId.ToULong(m_Value);

        /// <summary>
        /// 从原始 ulong 数据重建 SceneHandle。
        /// 与 GetRawData() 对应，用于反序列化场景句柄。
        /// </summary>
        /// <param name="rawdata">之前通过 GetRawData() 获取的 ulong 值</param>
        /// <returns>重建的 SceneHandle</returns>
        public static SceneHandle FromRawData(ulong rawdata) => new() { m_Value = EntityId.FromULong(rawdata) };
    }

    /// <summary>
    /// SceneHandle 的扩展方法集合，提供数组/列表在不同类型之间的零拷贝转换。
    /// 
    /// 核心技巧: 利用 .NET 的 StructLayout(LayoutKind.Explicit) + FieldOffset(0) 实现
    /// 内存层面的类型重解释转换（type punning），在不复制数据的情况下将
    /// int[] ↔ SceneHandle[] 或 EntityId[] ↔ SceneHandle[] 互相转换。
    /// 
    /// 因为 SceneHandle 的底层字段 m_Value 在内存中的布局与 int/EntityId 完全一致，
    /// 所以这种转换是安全的且零开销的。
    /// </summary>
    internal static class SceneHandleExtensions

    internal static class SceneHandleExtensions
    {
        /// <summary>
        /// Convert an array of <see langword="int"/> to an array of <see cref="SceneHandle"/>.
        /// </summary>
        public static SceneHandle[] ToSceneHandleArray(this int[] integers) => (SceneHandleToIntArray)integers;

        /// <summary>
        /// Convert an array of <see cref="SceneHandle"/> to an array of <see langword="int"/>.
        /// </summary>
        public static int[] ToIntArray(this SceneHandle[] sceneHandles) => (SceneHandleToIntArray)sceneHandles;

        /// <summary>
        /// Convert an array of <see cref="EntityId"/> to an array of <see cref="SceneHandle"/>.
        /// </summary>
        public static SceneHandle[] ToSceneHandleArray(this EntityId[] entityIds) => (SceneHandleToEntityIdArray)entityIds;

        /// <summary>
        /// Convert an array of <see cref="SceneHandle"/> to an array of <see cref="EntityId"/>.
        /// </summary>
        public static EntityId[] ToEntityIdArray(this SceneHandle[] sceneHandles) => (SceneHandleToEntityIdArray)sceneHandles;

        /// <summary>
        /// Convert a list of <see langword="int"/> to a list of <see cref="SceneHandle"/>.
        /// </summary>
        public static List<SceneHandle> ToSceneHandleList(this List<int> integers) => (SceneHandleToIntList)integers;

        /// <summary>
        /// Convert a list of <see cref="SceneHandle"/> to a list of <see langword="int"/>.
        /// </summary>
        public static List<int> ToIntList(this List<SceneHandle> sceneHandles) => (SceneHandleToIntList)sceneHandles;

        /// <summary>
        /// Convert a list of <see cref="EntityId"/> to a list of <see cref="SceneHandle"/>.
        /// </summary>
        public static List<SceneHandle> ToSceneHandleList(this List<EntityId> entityIds) => (SceneHandleToEntityIdList)entityIds;

        /// <summary>
        /// Convert a list of <see cref="SceneHandle"/> to a list of <see cref="EntityId"/>.
        /// </summary>
        public static List<EntityId> ToEntityIdList(this List<SceneHandle> sceneHandles) => (SceneHandleToEntityIdList)sceneHandles;

        /// <summary>
        /// int[] 与 SceneHandle[] 之间零拷贝互转的重解释转换结构体。
        /// 利用内存布局相同的特点（两者底层都是 4/8 字节的整数），
        /// 通过 Explicit 布局的 FieldOffset(0) 实现类型双关（type punning），
        /// 避免数组遍历和元素拷贝的开销。
        /// 
        /// 注意: 当 EntityId 变为 64 位时，此转换机制需要改为使用 long 类型。
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct SceneHandleToIntArray
        {
            [FieldOffset(0)] int[] _integers;
            [FieldOffset(0)] SceneHandle[] _sceneHandles;

            public static implicit operator SceneHandleToIntArray(int[] integers) => new() { _integers = integers };
            public static implicit operator SceneHandleToIntArray(SceneHandle[] sceneHandles) => new() { _sceneHandles = sceneHandles };
            public static implicit operator int[](SceneHandleToIntArray value) => value._integers;
            public static implicit operator SceneHandle[](SceneHandleToIntArray value) => value._sceneHandles;
        }

        /// <summary>
        /// EntityId[] 与 SceneHandle[] 之间零拷贝互转的重解释转换结构体。
        /// 由于 SceneHandle 底层存储就是 EntityId（m_Value 字段），
        /// 两者的内存布局完全一致，因此可以安全地进行零拷贝转换。
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct SceneHandleToEntityIdArray
        {
            [FieldOffset(0)] EntityId[] _entityIds;
            [FieldOffset(0)] SceneHandle[] _sceneHandles;

            public static implicit operator SceneHandleToEntityIdArray(EntityId[] entityIds) => new() { _entityIds = entityIds };
            public static implicit operator SceneHandleToEntityIdArray(SceneHandle[] sceneHandles) => new() { _sceneHandles = sceneHandles };
            public static implicit operator EntityId[](SceneHandleToEntityIdArray value) => value._entityIds;
            public static implicit operator SceneHandle[](SceneHandleToEntityIdArray value) => value._sceneHandles;
        }

        /// <summary>
        /// List&lt;int&gt; 与 List&lt;SceneHandle&gt; 之间零拷贝互转的重解释转换结构体。
        /// 与数组版本原理相同，用于泛型列表场景。
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct SceneHandleToIntList
        {
            [FieldOffset(0)] List<int> _integers;
            [FieldOffset(0)] List<SceneHandle> _sceneHandles;

            public static implicit operator SceneHandleToIntList(List<int> integers) => new() { _integers = integers };
            public static implicit operator SceneHandleToIntList(List<SceneHandle> sceneHandles) => new() { _sceneHandles = sceneHandles };
            public static implicit operator List<int>(SceneHandleToIntList value) => value._integers;
            public static implicit operator List<SceneHandle>(SceneHandleToIntList value) => value._sceneHandles;
        }

        /// <summary>
        /// List&lt;EntityId&gt; 与 List&lt;SceneHandle&gt; 之间零拷贝互转的重解释转换结构体。
        /// 用于 ECS 系统中 EntityId 集合与场景句柄集合的互操作。
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct SceneHandleToEntityIdList
        {
            [FieldOffset(0)] List<EntityId> _entityIds;
            [FieldOffset(0)] List<SceneHandle> _sceneHandles;

            public static implicit operator SceneHandleToEntityIdList(List<EntityId> entityIds) => new() { _entityIds = entityIds };
            public static implicit operator SceneHandleToEntityIdList(List<SceneHandle> sceneHandles) => new() { _sceneHandles = sceneHandles };
            public static implicit operator List<EntityId>(SceneHandleToEntityIdList value) => value._entityIds;
            public static implicit operator List<SceneHandle>(SceneHandleToEntityIdList value) => value._sceneHandles;
        }
    }
}
