// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 UnityEngine.Object — Unity 引擎一切对象的基类
//
// 📌 作用：
//   这是 Unity 中所有引擎对象（GameObject、Component、Asset 等）
//   的根基类。它提供了：
//   - 对象的生命周期管理（Instantiate/Destroy）
//   - 对象标识（EntityId 系统，替代旧版 InstanceID）
//   - 对象查询（FindObjectsByType 系列）
//   - 名称/标签/隐藏状态管理
//
// 🏗 架构位置：C# API 层 → Native Bindings 层 → C++ 引擎层
//   C# 代码 → [FreeFunction] extern → C++ Object 类
//
// 🔗 C++ 对应头文件：Runtime/BaseClasses/BaseObject.h
// ==============================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngineInternal;
using uei = UnityEngine.Internal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using NotNullWhenAttribute = System.Diagnostics.CodeAnalysis.NotNullWhenAttribute;
using MaybeNullWhenAttribute = System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    // ==============================================================
    // HideFlags — 控制对象的隐藏、保存和编辑行为
    //
    // 这是一个位标志枚举，可以组合使用。
    // 例如：HideFlags.HideAndDontSave 会让对象在 Hierarchy 中隐藏
    // 且不会保存到场景中。
    // ==============================================================
    [Flags]
    public enum HideFlags
    {
        // A normal, visible object. This is the default.
        None = 0,

        // The object will not appear in the hierarchy and will not show up in the project view if it is stored in an asset.
        HideInHierarchy = 1,

        // It is not possible to view it in the inspector
        HideInInspector = 2,

        // The object will not be saved to the scene.
        DontSaveInEditor = 4,

        // The object is not be editable in the inspector
        NotEditable = 8,

        // The object will not be saved when building a player
        DontSaveInBuild = 16,

        // The object will not be unloaded by UnloadUnusedAssets
        DontUnloadUnusedAsset = 32,

        DontSave = DontSaveInEditor | DontSaveInBuild | DontUnloadUnusedAsset,

        // A combination of not shown in the hierarchy and not saved to to scenes.
        HideAndDontSave = HideInHierarchy | DontSaveInEditor | NotEditable | DontSaveInBuild | DontUnloadUnusedAsset
    }

    // ==============================================================
    // FindObjectsSortMode — FindObjectsByType 的排序模式（已废弃）
    //
    // 历史背景（源码注释原文翻译）：
    // 用 Volvo Test Track 和 Gigaya 项目分析发现，
    // FindObjectsOfType() 中约 95% 的时间花在排序上！
    //   Volvo: 203ms 总时间, 190ms 在排序 (93.6%)
    //   Gigaya: 496ms 总时间, 461ms 在排序 (92.9%)
    // 因此引入 FindObjectsByType() 让用户选择是否排序。
    //
    // 从 6000.x 开始，InstanceID 被 EntityId 取代，
    // 排序模式也不再维护，所以这个枚举也废弃了。
    // ==============================================================
    [Obsolete("FindObjectsSortMode has been deprecated. Use the FindObjectsByType overloads that do not take a FindObjectsSortMode parameter.", false)]
    public enum FindObjectsSortMode
    {
        None = 0,
        InstanceID = 1
    }

    // ==============================================================
    // FindObjectsInactive — 是否包含未激活对象
    //
    // 控制 FindObjectsByType 是否返回未激活（inactive）的对象。
    // 默认为 Exclude，只返回 active 的对象。
    // ==============================================================
    public enum FindObjectsInactive
    {
        Exclude = 0,
        Include = 1
    }

    public struct InstantiateParameters
    {
        public Transform parent;
        public Scene scene;
        public bool worldSpace;
        public bool originalImmutable;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    [Serializable]
    [Obsolete("Obsolete - Please use EntityId instead.", true)]
    public struct InstanceID : IEquatable<InstanceID>, IComparable<InstanceID>, IFormattable
    {
        [SerializeField]
        int m_index;

        [SerializeField]
        int m_version;

        public static InstanceID None => default;
        public override bool Equals(object obj) => obj is InstanceID other && Equals(other);
        public bool Equals(InstanceID other) => m_index == other.m_index;
        public int CompareTo(InstanceID other) => m_index.CompareTo(other.m_index);
        public static bool operator ==(InstanceID left, InstanceID right) => left.Equals(right);
        public static bool operator !=(InstanceID left, InstanceID right) => !left.Equals(right);

        public static bool operator <(InstanceID left, InstanceID right)  => left.m_index < right.m_index;
        public static bool operator >(InstanceID left, InstanceID right)  => left.m_index > right.m_index;
        public static bool operator <=(InstanceID left, InstanceID right) => left.m_index <= right.m_index;
        public static bool operator >=(InstanceID left, InstanceID right)  => left.m_index >= right.m_index;

        public override int GetHashCode()
        {
            // We only want the lower bits, which is the Index
            uint a = (uint)m_index;

            // Same Int hash as in the engine
            a = (a + 0x7ed55d16) + (a << 12);
            a = (a ^ 0xc761c23c) ^ (a >> 19);
            a = (a + 0x165667b1) + (a << 5);
            a = (a + 0xd3a2646c) ^ (a << 9);
            a = (a + 0xfd7046c5) + (a << 3);
            a = (a ^ 0xb55a4f09) ^ (a >> 16);

            return (int)a;
        }

        public bool IsValid()
        {
            return this != InstanceID.None;
        }

        public bool Equals(int other) => m_index == (int)other;

        public static implicit operator int(InstanceID entityId) => entityId.m_index;
        public static implicit operator InstanceID(int intValue) => new InstanceID {m_index = intValue};

        public static implicit operator EntityId(InstanceID entityId) => (int)entityId;
        public static implicit operator InstanceID(EntityId entityId) => new InstanceID {m_index = (int)entityId};

        public override string ToString() => m_index.ToString();
        public string ToString(string format) => m_index.ToString(format);
        public string ToString(string format, IFormatProvider formatProvider) => m_index.ToString(format, formatProvider);
    }

#pragma warning disable 612, 618
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    [UsedByNativeCode]
    [Serializable]
    [NativeClass("EntityId")]
    [NativeHeader("Runtime/BaseClasses/BaseObject.h")]
    [NativeHeader("Runtime/BaseClasses/EntityIdStore.h")]
    public struct EntityId : IEquatable<EntityId>, IComparable<EntityId>, IFormattable
    {
        [SerializeField]
        ulong m_rawData;

        public static EntityId None => new EntityId { m_rawData = 0 };
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public bool Equals(EntityId other)
        {
            return m_rawData == other.m_rawData;
        }
        public int CompareTo(EntityId other) => m_rawData.CompareTo(other.m_rawData);
        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);

        public static bool operator <(EntityId left, EntityId right)  => left.m_rawData < right.m_rawData;
        public static bool operator >(EntityId left, EntityId right)  => left.m_rawData > right.m_rawData;
        public static bool operator <=(EntityId left, EntityId right) => left.m_rawData <= right.m_rawData;
        public static bool operator >=(EntityId left, EntityId right) => left.m_rawData >= right.m_rawData;

        public override int GetHashCode()
        {
            // Mirrors native EntityId::CalculateHash (Fibonacci hash, 2^64 / phi multiplier,
            // with a high-to-low fold so callers that mask off only the low bits of the
            // hash still see the full avalanche from Version bits).
            unchecked // No-op under the assembly's default /checked- build; documents that the multiply is meant to wrap.
            {
                const ulong kKnuth64 = 0x9E3779B97F4A7C15UL;
                uint hash = (uint)((m_rawData * kKnuth64) >> 32);
                return (int)(hash ^ (hash >> 16));
            }
        }

        public bool IsValid()
        {
            return this != EntityId.None;
        }

        // Bit layout matches native EntityId (see Modules/NativeKernel/Include/NativeKernel/BaseClasses/EntityID.h):
        //   [Version:24 | TypeId:12 | Index:28]
        //   Index:   bits  0–27 (mask 0x0FFFFFFF)
        //   TypeId:  bits 28–39
        //   Version: bits 40–63
        internal uint Index   => (uint)(m_rawData & 0x0FFFFFFFUL);
        internal uint Version => (uint)((m_rawData >> 40) & 0xFFFFFFUL);


        [Obsolete("EntityId will not be representable by an int in the future. This equals will be removed in a future version.", true)]
        public bool Equals(int other) => throw new NotImplementedException();

        [Obsolete("EntityId will not be representable by an int in the future. This casting operator will be removed in a future version.", true)]
        public static implicit operator int(EntityId entityId) => throw new NotImplementedException();

        [Obsolete("EntityId will not be representable by an int in the future. This casting operator will be removed in a future version.", true)]
        public static implicit operator EntityId(int intValue) => throw new NotImplementedException();

        public override string ToString() => $"{((int)(m_rawData & 0xFFFFFFFF)).ToString(CultureInfo.InvariantCulture)}:{(int)(m_rawData >> 32)}";
        public string ToString(string format) => $"{((int)(m_rawData & 0xFFFFFFFF)).ToString(format, CultureInfo.InvariantCulture)}:{(int)(m_rawData >> 32)}";
        public string ToString(string format, IFormatProvider formatProvider) => $"{((int)(m_rawData & 0xFFFFFFFF)).ToString(format, formatProvider)}:{((int)(m_rawData >> 32)).ToString(format, formatProvider)}";


        internal static unsafe EntityId AllocateEntityId()
        {
            EntityId id;
            EntityIdStore.AllocateEntityIds(&id, 1);
            return id;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static EntityId Parse(string input)
        {
            if (string.IsNullOrEmpty(input))
                return EntityId.None;

            // Same as native StringToEntityId: full raw UInt64, no reinterpretation (see EntityID.cpp).
            if (!ulong.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongResult))
            {
                var colonIndex = input.IndexOf(':');
                if (colonIndex == -1 || colonIndex == 0 || colonIndex == input.Length - 1)
                    return EntityId.None;
                var indexStr = input.Substring(0, colonIndex);
                var versionStr = input.Substring(colonIndex + 1);
                if (!int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entityIndex))
                    return EntityId.None;
                if (!int.TryParse(versionStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entityVersion))
                    return EntityId.None;
                ulongResult = ((ulong)entityVersion << 32) | (uint)entityIndex;
            }

            return EntityId.FromULong(ulongResult);
        }

        [Obsolete("Please use EntityId.ToULong(EntityId) instead.", false)]
        public ulong GetRawData() => m_rawData;

        public static EntityId FromULong(ulong input) => new EntityId { m_rawData = input };
        public static ulong ToULong(EntityId entityId) => entityId.m_rawData;
    }
    #pragma warning restore 612, 618

    class AssetGCFilterTypeAttribute : Attribute
    {
        public AssetGCFilterTypeAttribute() {}
    }

    // ==============================================================
    // 🎯 Object — Unity 引擎的"上帝基类"
    //
    // 在 Unity 中，所有引擎对象（包括 GameObject、Component、
    // ScriptableObject、资源等）都继承自这个类。
    //
    // 🔑 两个核心字段：
    //   m_CachedPtr — C++ 侧对象的内存指针（IntPtr）
    //   m_EntityId  — 64 位唯一标识（替代了旧版的 InstanceID）
    //
    // 💡 理解关键：
    //   Object 的 C# 对象只是一个"代理"（Wrapper），
    //   真正的引擎数据存储在 C++ 侧。
    //   m_CachedPtr 就是连接 C# 代理和 C++ 实体的桥梁。
    //   → 这就是为什么 Destroy 后 C# 对象还存在，但 m_CachedPtr 为 null
    //     而 Object 隐式转换为 bool 时会返回 false（伪 null 模式）
    //
    // 🆚 新旧对比：
    //   - 旧版：InstanceID（32 位 int，可重用，可能悬垂引用）
    //   - 新版：EntityId（64 位，包含 Version 防止悬垂引用）
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode(GenerateProxy = false)]
    [NativeHeader("Runtime/Export/Scripting/UnityEngineObject.bindings.h")]
    [NativeHeader("Runtime/GameCode/CloneObject.h")]
    [NativeHeader("Runtime/SceneManager/SceneManager.h")]
    [AssetGCFilterType]
    public partial class Object
    {

#pragma warning disable 649
        // ⚡ m_CachedPtr — C++ 引擎对象的原始内存指针
        // 这是 C# 代理和 C++ 对象之间的桥梁。
        // 当对象被 Destroy 后，此指针被置为 IntPtr.Zero。
        // 这就是 Unity 中"伪 null"（fake null）机制的底层实现：
        //   即使 C# 引用不为 null，但 m_CachedPtr == 0 就视为 null
        IntPtr   m_CachedPtr;

        // 🆔 m_EntityId — 64 位全局唯一标识符
        // 替代了旧版的 32 位 InstanceID。
        // 布局：[Version:24 | TypeId:12 | Index:28]
        // 使用 64 位可以分配 2.68 亿个对象而不会耗尽版本号。
        private EntityId m_EntityId;
#pragma warning disable 169
        // ⚠️ m_UnityRuntimeErrorString — 运行时错误信息
        // 当 C++ 侧反序列化时遇到类型不匹配的对象引用，
        // 会将错误信息暂存于此，在恰当时机抛出异常。
        private string m_UnityRuntimeErrorString;
#pragma warning restore 169

#pragma warning disable 414
#pragma warning restore 414
#pragma warning restore 649

        const string objectIsNullMessage = "The Object you want to instantiate is null.";
        const string cloneDestroyedMessage = "Instantiate failed because the clone was destroyed during creation. This can happen if DestroyImmediate is called in MonoBehaviour.Awake.";

        // ==============================================================
        // GetEntityId() — 获取对象的 64 位唯一标识符
        //
        // 🚨 线程安全：
        //   Unity 要求大部分引擎 API 在主线程调用。
        //   如果在非主线程调用此方法，会抛出异常。
        //   这是为了在编辑器阶段就发现线程问题，而不是在 Player 运行时才暴露。
        //
        // 🔄 迁移历史：
        //   GetInstanceID() [旧] → GetEntityId() [新]
        //   旧版使用 32 位 int 作为实例 ID，新版使用 64 位 EntityId。
        // ==============================================================
        [System.Security.SecuritySafeCritical]
        public unsafe EntityId GetEntityId()
        {
            //Because in the player we dissalow calling GetInstanceID() on a non-mainthread, we're also
            //doing this in the editor, so people notice this problem early. even though technically in the editor,
            //it is a threadsafe operation.
            EnsureRunningOnMainThread();
            return m_EntityId;
        }

        // ==============================================================
        // GetEntityIdForSerializationUnchecked() — 跳过主线程检查的获取 EntityId
        //
        // ⚡ 内联优化：
        //   调用者都是序列化路径（clone/Instantiate），这些路径必定在主线程运行。
        //   跳过 EnsureRunningOnMainThread() 检查以提高性能。
        //   如果未来有非主线程调用者，需要恢复线程安全检查。
        // ==============================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe EntityId GetEntityIdForSerializationUnchecked()
        {
            return m_EntityId;
        }

        [Obsolete("Calling MemberwiseClone on a UnityEngine.Object will result in a corrupt object, use Instantiate or InstantiateAsync instead.", true)]
        new protected object MemberwiseClone()
        {
            throw new NotImplementedException();
        }

        [Obsolete("GetInstanceID is deprecated. Use GetEntityId instead. This will be removed in a future version.", true)]
        [System.Security.SecuritySafeCritical]
        public unsafe int GetInstanceID() => (int)(GetEntityId().GetRawData() & 0x00000000FFFFFFFF);

        // ==============================================================
        // GetHashCode() — 基于 EntityId 的哈希码
        //
        // 🎯 设计意图：
        //   在编辑器中，可能有多个 C# 对象指向同一个 C++ 对象（边界情况）。
        //   这种情况下，我们希望这些对象在 GetHashCode() 和 Equals() 中
        //   被视为相等的。
        //   所以使用 m_EntityId.GetHashCode() 而不是 m_CachedPtr.GetHashCode()。
        // ==============================================================
        public override int GetHashCode()
        {
            //in the editor, we store the m_EntityId in the c# objects. It's actually possible to have multiple c# objects
            //pointing to the same c++ object in some edge cases, and in those cases we'd like GetHashCode() and Equals() to treat
            //these objects as equals.
            return m_EntityId.GetHashCode();
        }

        // ==============================================================
        // Equals() — 比较两个 Object 是否指向同一个引擎对象
        //
        // ⚠️ Unity 特有的"伪 null"机制：
        //   在 Unity 中，被 Destroy 的对象不等于 null（C# 引用还在），
        //   但 == 运算符会返回 true（Object == null 为 true）。
        //   这就是为什么 Unity 中使用 if (obj == null) 而不是 if (obj != null)。
        // ==============================================================
        public override bool Equals(object other)
        {
            Object otherAsObject = other as Object;
            // A UnityEngine.Object can only be equal to another UnityEngine.Object - or null if it has been destroyed.
            // Make sure other is a UnityEngine.Object if "as Object" fails. The explicit "is" check is required since the == operator
            // in this class treats destroyed objects as equal to null
            if (otherAsObject == null && other != null && !(other is Object))
                return false;

            return CompareBaseObjects(this, otherAsObject);
        }

        // ==============================================================
        // implicit operator bool — Unity 的"伪 null"机制核心
        //
        // 💡 这就是为什么你在 Unity 中写 if (gameObject) 会工作的原因。
        //
        // 即使 C# 引用不为 null（未被 GC 回收），
        // 但如果它指向的 C++ 对象已被 Destroy，则认为它为 false。
        //
        // 这是 Unity 特有的设计：
        //   普通 C# 对象：  obj == null 检查的是引用
        //   Unity Object： obj 会检查引用的 C++ 对象是否存活
        // ==============================================================
        public static implicit operator bool([NotNullWhen(true)] [MaybeNullWhen(false)] Object exists)
        {
            return !CompareBaseObjects(exists, null);
        }

        // ==============================================================
        // CompareBaseObjects() — Unity 对象比较的核心逻辑
        //
        // ⚡ 这里的逻辑决定了 Unity 中 Object 的比较行为：
        //
        //   情况 1：两边 C# 引用都为 null → 相等
        //   情况 2：一边为 null，另一边看 IsNativeObjectAlive
        //   情况 3：两边都在 → 比较 EntityId
        //
        // 🔑 "伪 null"机制示例：
        //   GameObject go = new GameObject("test");
        //   Object.DestroyImmediate(go);
        //   bool r1 = (go == null);        // true！C++ 对象已销毁
        //   bool r2 = (go.Equals(null));   // true
        //   bool r3 = (go is GameObject);  // true！C# 引用还在
        //   bool r4 = ((object)go != null); // true！C# 层面引用不为空
        // ==============================================================
        static bool CompareBaseObjects(UnityEngine.Object lhs, UnityEngine.Object rhs)
        {
            bool lhsNull = ((object)lhs) == null;
            bool rhsNull = ((object)rhs) == null;

            if (rhsNull && lhsNull) return true;

            if (rhsNull) return !IsNativeObjectAlive(lhs);
            if (lhsNull) return !IsNativeObjectAlive(rhs);

            return lhs.m_EntityId == rhs.m_EntityId;
        }

        private void EnsureRunningOnMainThread()
        {
            if (!CurrentThreadIsMainThread())
                throw new System.InvalidOperationException("EnsureRunningOnMainThread can only be called from the main thread");
        }

        // ==============================================================
        // IsNativeObjectAlive() — 检查 C++ 引擎对象是否还存活
        //
        // 💡 这是 Unity "伪 null"机制的底层实现：
        //
        // 第一步：检查 m_CachedPtr 是否为空
        //   → 不为空说明 C++ 对象还在 → 存活
        //
        // 第二步：处理资源"复活"情况（仅编辑器）
        //   在编辑器中，资源（如 Material）可能会：
        //     删除 → 重新导入 → 恢复相同的 InstanceID
        //   这种情况下，我们希望旧的 C# 包装器仍然"可用"。
        //
        // ⚠️ 例外：MonoBehaviour 和 ScriptableObject
        //   对于这两种类型，"资源就是 C# 对象本身"，
        //   不能假装旧包装器指向新对象。
        //   所以 MonoBehaviour 和 ScriptableObject 被 Destroy 后
        //   即使 EntityId 还存在，也算作"已死亡"。
        // ==============================================================
        static bool IsNativeObjectAlive(UnityEngine.Object o)
        {
            if (o.GetCachedPtr() != IntPtr.Zero)
                return true;

            //Ressurection of assets is complicated.
            //For almost all cases, if you have a c# wrapper for an asset like a material,
            //if the material gets moved, or deleted, and later placed back, the persistentmanager
            //will ensure it will come back with the same instanceid.
            //in this case, we want the old c# wrapper to still "work".
            //we only support this behaviour in the editor, even though there
            //are some cases in the player where this could happen too. (when unloading things from assetbundles)
            //supporting this makes all operator== slow though, so we decided to not support it in the player.
            //
            //we have an exception for assets that "are" a c# object, like a MonoBehaviour in a prefab, and a ScriptableObject.
            //in this case, the asset "is" the c# object,  and you cannot actually pretend
            //the old wrapper points to the new c# object. this is why we make an exception in the operator==
            //for this case. If we had a c# wrapper to a persistent monobehaviour, and that one gets
            //destroyed, and placed back with the same instanceID,  we still will say that the old
            //c# object is null.
            if (o is MonoBehaviour || o is ScriptableObject)
                return false;

            return DoesObjectWithInstanceIDExist(o.GetEntityId());
        }

        [RequiredByNativeCode]
        internal System.IntPtr GetCachedPtr()
        {
            return m_CachedPtr;
        }

        [RequiredByNativeCode]
        void SetCachedPtr(System.IntPtr ptr)
        {
            m_CachedPtr = ptr;
        }

        [RequiredByNativeCode]
        internal void GetEntityIdFast(out EntityId v) { v = m_EntityId; }

        [RequiredByNativeCode]
        internal void SetEntityId(EntityId v) { m_EntityId = v; }

        [RequiredByNativeCode]
        internal void SetUnityRuntimeErrorString(string errorString) { m_UnityRuntimeErrorString = errorString; }

        // UUM-143556: lets the native game-release writer read back the marker the reference
        // deserializer stamps on a type-mismatched reference, so it can drop it (write fileID 0).
        [RequiredByNativeCode]
        internal string GetUnityRuntimeErrorString() { return m_UnityRuntimeErrorString; }

        [RequiredByNativeCode]
        internal void BindNativeObject(System.IntPtr cachedPtr, EntityId v)
        {
            m_CachedPtr = cachedPtr;
            m_EntityId = v;
            m_UnityRuntimeErrorString = null;
        }

        // The name of the object.
        public string name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        // Clones the object /original/ and returns the clone.
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Transform parent) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, position, rotation, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Transform parent, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, position, rotation, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, position, rotation, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Quaternion> rotations) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, positions, rotations, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, position, rotation, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, Vector3 position, Quaternion rotation, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, position, rotation, new InstantiateParameters{ worldSpace = true, parent = parent }, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Quaternion> rotations) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, positions, rotations, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Quaternion> rotations, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, positions, rotations, new InstantiateParameters{ worldSpace = true, parent = parent }, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, 1, parameters, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, ReadOnlySpan<Vector3>.Empty,  ReadOnlySpan<Quaternion>.Empty, parameters, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Vector3 position, Quaternion rotation, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, 1, position, rotation, parameters, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Vector3 position, Quaternion rotation, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            unsafe
            {
                return InstantiateAsync(original, count, new ReadOnlySpan<Vector3>(&position, 1),  new ReadOnlySpan<Quaternion>(&rotation, 1), parameters, cancellationToken);
            }
        }

        // Use the value directly to support netstandard
        // MethodImplOptions.AggressiveInlining = 256
        // MethodImplOptions.AggressiveOptimization = 512
        [MethodImpl(256 | 512)]
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Quaternion> rotations, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            CheckNullArgument(original, objectIsNullMessage);

            if (count <= 0)
            {
                throw new ArgumentException("Cannot call instantiate multiple with count less or equal to zero");
            }

                if (original is ScriptableObject)
                    throw new ArgumentException("Cannot call instantiate multiple for a ScriptableObject");

            unsafe
            {
                fixed(Vector3* positionsPtr = positions)
                fixed(Quaternion* rotationsPtr = rotations)
                {
                    return new AsyncInstantiateOperation<T>(Internal_InstantiateAsyncWithParams(original, count, parameters, (IntPtr)positionsPtr, positions.Length, (IntPtr)rotationsPtr, rotations.Length), cancellationToken);
                }
            }
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation)
        {
            CheckNullArgument(original, objectIsNullMessage);

            if (original is ScriptableObject)
                throw new ArgumentException("Cannot instantiate a ScriptableObject with a position and rotation");

            var obj = Internal_InstantiateSingle(original, position, rotation);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (parent == null)
                return Instantiate(original, position, rotation);

            CheckNullArgument(original, objectIsNullMessage);
            if (parent.gameObject.IsDestroying())
                ThrowArgumentExceptionForParentBeingDestroyed(original.name, parent.name, nameof(parent));

            var obj = Internal_InstantiateSingleWithParent(original, parent, position, rotation);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original)
        {
            CheckNullArgument(original, objectIsNullMessage);
            var obj = Internal_CloneSingle(original);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Scene scene)
        {
            CheckNullArgument(original, objectIsNullMessage);
            var obj = Internal_CloneSingleWithScene(original, scene);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        public static T Instantiate<T>(T original, InstantiateParameters parameters) where T : UnityEngine.Object
        {
            CheckNullArgument(original, objectIsNullMessage);

            if (parameters.parent != null && parameters.parent.gameObject.IsDestroying())
                ThrowArgumentExceptionForParentBeingDestroyed(original.name, parameters.parent.name, nameof(parameters.parent));

            var obj = (T)Internal_CloneSingleWithParams(original, parameters);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, InstantiateParameters parameters) where T : UnityEngine.Object
        {
            CheckNullArgument(original, objectIsNullMessage);

            if (parameters.parent != null && parameters.parent.gameObject.IsDestroying())
                ThrowArgumentExceptionForParentBeingDestroyed(original.name, parameters.parent.name, nameof(parameters.parent));

            var obj = (T)Internal_InstantiateSingleWithParams(original, position, rotation, parameters);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Transform parent)
        {
            return Instantiate(original, parent, false);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Transform parent, bool instantiateInWorldSpace)
        {
            if (parent == null)
                return Instantiate(original);

            CheckNullArgument(original, objectIsNullMessage);
            if (parent.gameObject.IsDestroying())
                ThrowArgumentExceptionForParentBeingDestroyed(original.name, parent.name, nameof(parent));

            var obj = Internal_CloneSingleWithParent(original, parent, instantiateInWorldSpace);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        public static T Instantiate<T>(T original) where T : UnityEngine.Object
        {
            CheckNullArgument(original, objectIsNullMessage);
            var obj = (T)Internal_CloneSingle(original);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return (T)Instantiate((Object)original, position, rotation);
        }

        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : UnityEngine.Object
        {
            return (T)Instantiate((Object)original, position, rotation, parent);
        }

        public static T Instantiate<T>(T original, Transform parent) where T : UnityEngine.Object
        {
            return Instantiate<T>(original, parent, false);
        }

        public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays) where T : UnityEngine.Object
        {
            return (T)Instantiate((Object)original, parent, worldPositionStays);
        }

        // ==============================================================
        // Destroy() — 延迟销毁对象
        //
        // 🎯 这是 Unity 中销毁对象的标准方式。
        //
        // 💡 Destroy vs DestroyImmediate:
        //
        //   Destroy(obj)          → 在当前帧结束前销毁（安全，推荐）
        //   Destroy(obj, 3f)      → 3 秒后销毁
        //   DestroyImmediate(obj) → 立即销毁（⚠️ 危险，可能导致引用丢失）
        //
        // 🔄 Destroy 的工作原理：
        //   1. 将对象标记为"待销毁"
        //   2. 当帧结束时，引擎统一执行销毁
        //   3. 销毁前调用所有组件的 OnDestroy()
        //   4. 销毁后将 m_CachedPtr 置为 IntPtr.Zero
        //
        // ⚠️ 为什么推荐 Destroy 而不是 DestroyImmediate：
        //   - Destroy 可避免"迭代中删除集合元素"的问题
        //   - DestroyImmediate 可能导致 MonoBehaviour.OnDestroy()
        //     中访问其他已销毁的对象
        //   - DestroyImmediate 只在确定需要立即释放资源时使用
        //     （如编辑器代码中）
        //
        // 🆚 与 C# 中 Dispose/析构函数的区别：
        //   Destroy 释放的是 C++ 侧的内存，
        //   C# 侧的代理对象由 GC 收集（仍是 .NET 管理）。
        //   因此 Destroy 后 C# 对象还存在，但变成了"伪 null"。
        // ==============================================================
        [NativeMethod(Name = "Scripting::DestroyObjectFromScripting", IsFreeFunction = true, ThrowsException = true)]
        public extern static void Destroy(Object obj, [uei.DefaultValue("0.0F")] float t);

        [uei.ExcludeFromDocs]
        public static void Destroy(Object obj)
        {
            float t = 0.0F;
            Destroy(obj, t);
        }

        // ==============================================================
        // DestroyImmediate() — 立即销毁对象
        //
        // ⚠️ 警告：仅在绝对必要时使用！
        //
        // 适用场景：
        //   - 编辑器代码中（如 Editor 脚本）
        //   - 需要立即释放资源时
        //
        // allowDestroyingAssets 参数：
        //   - false（默认）: 不允许销毁资源文件
        //   - true: 允许销毁 Assets（危险！可能导致数据丢失）
        // ==============================================================
        [NativeMethod(Name = "Scripting::DestroyObjectFromScriptingImmediate", IsFreeFunction = true, ThrowsException = true)]
        public extern static void DestroyImmediate(Object obj, [uei.DefaultValue("false")]  bool allowDestroyingAssets);

        [uei.ExcludeFromDocs]
        public static void DestroyImmediate(Object obj)
        {
            bool allowDestroyingAssets = false;
            DestroyImmediate(obj, allowDestroyingAssets);
        }

        /*
         * Profiling enter/exit playmode with the Volvo Test Track project and Gigaya has shown that ~95% of the time spent in FindObjectsOfType() is spent sorting the array by InstanceID even though in almost all cases this is not thought to be necessary.
         * In the Volvo project(2022.1) during a single enter/exit playmode cycle 203ms was spent in Object::FindObjectsOfType() of which 190ms was in the sorting(93.6%)
         * In Gigaya(2021.3) during a single enter/exit playmode cycle 496ms was spent in Object::FindObjectsOfType() of which 461ms was in the sorting(92.9%)
         * There has been a lengthy discussion in #devs-scripting about possible solutions to this (https://unity.slack.com/archives/C06TPSM32/p1651840563109579), the consensus is to deprecate FindObjectsOfType() and replace it with FindObjectsByType()
         * which lets the user choose whether to perform the sort or not
         * Note it is considered undesirable to have the API updater automatically convert FindObjectsOfType() to FindObjectsByType(FindObjectsSortMode.InstanceID) as we really want users to assess their usage on a case by case basis and only choose
         * sorting when necessary to maximise the performance gain
         * The plan is:
         *   2023.1 :
         *     FindObjectsOfType() Obsolete(warning), direct users to FindObjectsByType
         *     FindObjectOfType() Obsolete(warning), direct users to FindFirstObjectByType and FindAnyObjectByType
         *   2023.2
         *     FindObjectsOfType() Obsolete(error), direct users to FindObjectsByType
         *     FindObjectOfType() Obsolete(error), direct users to FindFirstObjectByType and FindAnyObjectByType
         *   2024.2
         *     FindObjectsOfType() deleted
         *     FindObjectOfType() deleted
         * This work is captured in https://jira.unity3d.com/browse/COPT-854
         *
         * As an update to this (12/17/2025), we are deprecated FindObject(s) methods that depend on sort order.
         * This is due to the move from InstanceID to EntityId - where we cannot depend on maintaining a meaningful sort order.
         * Additionally new FindObjectsByType methods have been introduced that do not take in a sort order parameter,
         * and we are steering users to these new methods.
         * */

        // Returns a list of all active loaded objects of Type /type/. Results are sorted by InstanceID
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID, but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        public static Object[] FindObjectsOfType(Type type)
        {
            return FindObjectsOfType(type, false);
        }

        // Returns a list of all loaded objects of Type /type/. Results are sorted by InstanceID
        [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsOfType")]
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public extern static Object[] FindObjectsOfType(Type type, bool includeInactive);

        // Returns a list of all active loaded objects of Type /type/.
        [Obsolete("FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType(Type) or FindObjectsByType(Type, FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.", false)]
        public static Object[] FindObjectsByType(Type type, FindObjectsSortMode sortMode)
        {
            return FindObjectsByType(type, FindObjectsInactive.Exclude, sortMode);
        }

        // Returns a list of all loaded objects of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsByType")]
        [Obsolete("FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType(Type) or FindObjectsByType(Type, FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.", false)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public extern static Object[] FindObjectsByType(Type type, FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode);

        // Returns a list of all active loaded objects of Type /type/.
        public static Object[] FindObjectsByType(Type type)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Returns a list of all loaded objects of Type /type/.
        public static Object[] FindObjectsByType(Type type, FindObjectsInactive findObjectsInactive)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return FindObjectsByType(type, findObjectsInactive, FindObjectsSortMode.None);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Allocates a batch of EntityIds
        [FreeFunction("AllocateEntityIds")]
        internal extern static EntityId[] AllocateEntityIds(int count);

        // Makes the object /target/ not be destroyed automatically when loading a new scene.
        [FreeFunction("GetSceneManager().DontDestroyOnLoad", ThrowsException = true)]
        public extern static void DontDestroyOnLoad([NotNull] Object target);

        // // Should the object be hidden, saved with the scene or modifiable by the user?
        public extern HideFlags hideFlags { get; set; }

        //*undocumented* deprecated
        // We cannot properly deprecate this in C# right now, since the optional parameter creates
        // another method calling this, which creates compiler warnings when deprecated.
        [Obsolete("use Object.Destroy instead.")]
        public static void DestroyObject(Object obj, [uei.DefaultValue("0.0F")]  float t)
        {
            Destroy(obj, t);
        }

        [Obsolete("use Object.Destroy instead.")]
        [uei.ExcludeFromDocs]
        public static void DestroyObject(Object obj)
        {
            float t = 0.0F;
            Destroy(obj, t);
        }

        //*undocumented* DEPRECATED
        [Obsolete("Object.FindSceneObjectsOfType has been deprecated, Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindSceneObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        public static Object[] FindSceneObjectsOfType(Type type)
        {
            return FindObjectsOfType(type);
        }

        //*undocumented*  DEPRECATED
        [Obsolete("use Resources.FindObjectsOfTypeAll instead.")]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsOfTypeIncludingAssets")]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public extern static Object[] FindObjectsOfTypeIncludingAssets(Type type);

        // Returns a list of all loaded objects of Type /type/. Results are sorted by InstanceID
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        public static T[] FindObjectsOfType<T>() where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsOfType(typeof(T), false));
        }

        // Returns a list of all loaded objects of Type /type/
        [Obsolete("FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType<T>() or FindObjectsByType<T>(FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.", false)]
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsByType(typeof(T), FindObjectsInactive.Exclude, sortMode));
        }

        // Returns a list of all loaded objects of Type /type/. Results are sorted by InstanceID
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        public static T[] FindObjectsOfType<T>(bool includeInactive) where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsOfType(typeof(T), includeInactive));
        }

        // Returns a list of all loaded objects of Type /type/. Order of results is not guaranteed to be consistent between calls
        [Obsolete("FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType<T>() or FindObjectsByType<T>(FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.", false)]
        public static T[] FindObjectsByType<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsByType(typeof(T), findObjectsInactive, sortMode));
        }


        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindAnyObjectByType instead.", false)]
        public static T FindObjectOfType<T>() where T : Object
        {
            return (T)FindObjectOfType(typeof(T), false);
        }

        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindAnyObjectByType instead.", false)]
        public static T FindObjectOfType<T>(bool includeInactive) where T : Object
        {
            return (T)FindObjectOfType(typeof(T), includeInactive);
        }

        [Obsolete("FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.", false)]
        public static T FindFirstObjectByType<T>() where T : Object
        {
            return (T)FindFirstObjectByType(typeof(T), FindObjectsInactive.Exclude);
        }

        public static T FindAnyObjectByType<T>() where T : Object
        {
            return (T)FindAnyObjectByType(typeof(T), FindObjectsInactive.Exclude);
        }

        [Obsolete("FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.", false)]
        public static T FindFirstObjectByType<T>(FindObjectsInactive findObjectsInactive) where T : Object
        {
            return (T)FindFirstObjectByType(typeof(T), findObjectsInactive);
        }

        public static T FindAnyObjectByType<T>(FindObjectsInactive findObjectsInactive) where T : Object
        {
            return (T)FindAnyObjectByType(typeof(T), findObjectsInactive);
        }

        public static T[] FindObjectsByType<T>() where T : Object
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return Resources.ConvertObjects<T>(FindObjectsByType(typeof(T), FindObjectsInactive.Exclude, FindObjectsSortMode.None));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static T[] FindObjectsByType<T>(FindObjectsInactive findObjectsInactive) where T : Object
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return Resources.ConvertObjects<T>(FindObjectsByType(typeof(T), findObjectsInactive, FindObjectsSortMode.None));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [System.Obsolete("Please use Resources.FindObjectsOfTypeAll instead")]
        public static Object[] FindObjectsOfTypeAll(Type type)
        {
            return Resources.FindObjectsOfTypeAll(type);
        }

        static private void CheckNullArgument(object arg, string message)
        {
            if (arg == null)
                throw new System.ArgumentException(message);
        }

        static private void ThrowArgumentExceptionForParentBeingDestroyed(string nameOfObjectToInstantiate, string parentName, string parameterName)
        {
            throw new ArgumentException($"Trying to instantiate '{nameOfObjectToInstantiate}' as child of '{parentName}', but that parent is currently being destroyed. Check IsDestroying() on the parent GameObject before using it.", parameterName);
        }

        // Returns the first active loaded object of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindAnyObjectByType instead.", false)]
        public static Object FindObjectOfType(System.Type type)
        {
            Object[] objects = FindObjectsOfType(type, false);
            if (objects.Length > 0)
                return objects[0];
            else
                return null;
        }

        [Obsolete("FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.", false)]
        public static Object FindFirstObjectByType(System.Type type)
        {
            Object[] objects = FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            return (objects.Length > 0) ? objects[0] : null;
        }

        public static Object FindAnyObjectByType(System.Type type)
        {
            Object[] objects = FindObjectsByType(type, FindObjectsInactive.Exclude);
            return (objects.Length > 0) ? objects[0] : null;
        }

        // Returns the first active loaded object of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindAnyObjectByType instead.", false)]
        public static Object FindObjectOfType(System.Type type, bool includeInactive)
        {
            Object[] objects = FindObjectsOfType(type, includeInactive);
            if (objects.Length > 0)
                return objects[0];
            else
                return null;
        }

        [Obsolete("FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.", false)]
        public static Object FindFirstObjectByType(System.Type type, FindObjectsInactive findObjectsInactive)
        {
            Object[] objects = FindObjectsByType(type, findObjectsInactive, FindObjectsSortMode.InstanceID);
            return (objects.Length > 0) ? objects[0] : null;
        }

        public static Object FindAnyObjectByType(System.Type type, FindObjectsInactive findObjectsInactive)
        {
            Object[] objects = FindObjectsByType(type, findObjectsInactive);
            return (objects.Length > 0) ? objects[0] : null;
        }

        // Returns the name of the game object.
        public override string ToString()
        {
            return ToString(this);
        }

        public static bool operator==(Object x, Object y) { return CompareBaseObjects(x, y); }

        public static bool operator!=(Object x, Object y) { return !CompareBaseObjects(x, y); }

        [NativeMethod(Name = "Object::GetOffsetOfInstanceIdMember", IsFreeFunction = true, IsThreadSafe = true)]
        extern static int GetOffsetOfInstanceIDInCPlusPlusObject();

        [NativeMethod(Name = "CurrentThreadIsMainThread", IsFreeFunction = true, IsThreadSafe = true)]
        extern static bool CurrentThreadIsMainThread();

        [NativeMethod(Name = "CloneObject", IsFreeFunction = true, ThrowsException = true)]
        extern static Object Internal_CloneSingle([NotNull] Object data);

        [FreeFunction("CloneObjectToScene")]
        extern static Object Internal_CloneSingleWithScene([NotNull] Object data, Scene scene);

        [FreeFunction("CloneObjectWithParams")]
        extern static Object Internal_CloneSingleWithParams([NotNull] Object data, InstantiateParameters parameters);
        [FreeFunction("InstantiateObjectWithParams")]
        extern static Object Internal_InstantiateSingleWithParams([NotNull] Object data, Vector3 position, Quaternion rotation, InstantiateParameters parameters);

        [FreeFunction("CloneObject")]
        extern static Object Internal_CloneSingleWithParent([NotNull] Object data, [NotNull] Transform parent, bool worldPositionStays);

        [FreeFunction("InstantiateAsyncObjects")]
        extern static IntPtr Internal_InstantiateAsyncWithParams([NotNull] Object original, int count, InstantiateParameters parameters, IntPtr positions, int positionsCount, IntPtr rotations, int rotationsCount);

        [FreeFunction("InstantiateObject")]
        extern static Object Internal_InstantiateSingle([NotNull] Object data, Vector3 pos, Quaternion rot);

        [FreeFunction("InstantiateObject")]
        extern static Object Internal_InstantiateSingleWithParent([NotNull] Object data, [NotNull] Transform parent, Vector3 pos, Quaternion rot);

        [FreeFunction("UnityEngineObjectBindings::ToString")]
        extern static string ToString(Object obj);

        [FreeFunction("UnityEngineObjectBindings::GetName", HasExplicitThis = true)]
        extern string GetName();

        [FreeFunction("UnityEngineObjectBindings::IsPersistent")]
        internal extern static bool IsPersistent([NotNull] Object obj);

        [FreeFunction("UnityEngineObjectBindings::SetName", HasExplicitThis = true)]
        extern void SetName(string name);

        [NativeMethod(Name = "UnityEngineObjectBindings::DoesObjectWithInstanceIDExist", IsFreeFunction = true, IsThreadSafe = true)]
        internal extern static bool DoesObjectWithInstanceIDExist(EntityId instanceID);

        [VisibleToOtherModules]
        [FreeFunction("UnityEngineObjectBindings::FindObjectFromInstanceID")]
        internal extern static Object FindObjectFromInstanceID(EntityId instanceID);

        [VisibleToOtherModules]
        [FreeFunction("UnityEngineObjectBindings::FindObjectFromInstanceIDThreadSafe", IsThreadSafe = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        internal extern static Object FindObjectFromInstanceIDThreadSafe(EntityId instanceID);

        [FreeFunction("UnityEngineObjectBindings::GetPtrFromInstanceID")]
        private extern static IntPtr GetPtrFromInstanceID(EntityId instanceID, Type objectType, out bool isMonoBehaviour);

        [VisibleToOtherModules]
        [FreeFunction("UnityEngineObjectBindings::ForceLoadFromInstanceID")]
        internal extern static Object ForceLoadFromInstanceID(EntityId instanceID);
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static Object CreateMissingReferenceObject(EntityId instanceID)
        {
            return new Object { m_EntityId = instanceID };
        }

        [FreeFunction("UnityEngineObjectBindings::MarkObjectDirty", HasExplicitThis = true)]
        internal extern void MarkDirty();

        [VisibleToOtherModules]
        internal static class MarshalledUnityObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static IntPtr Marshal<T>(T obj) where T : Object
            {
                // Do not to an == null or .Equals(null) check in here or anything that would make an icall
                // This may be called during AppDomain shutdown and there is code called during shutdown
                // that relies on the SCRIPTINGAPI_THREAD_AND_SERIALIZATION_CHECK throwing on shutdown
                // So this code can't call any icalls marked as ThreadSafe (e.g. DoesObjectWithInstanceIDExist)
                if (ReferenceEquals(obj, null))
                    return IntPtr.Zero;
                return MarshalNotNull(obj);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static IntPtr MarshalNotNull<T>(T obj) where T : Object
            {
                // obj has already been checked and is guaranteed to not be null
                if (obj.m_CachedPtr != IntPtr.Zero)
                    return obj.m_CachedPtr;
                return MarshalFromInstanceId(obj);
            }

            private static IntPtr MarshalFromInstanceId<T>(T obj) where T : Object
            {
                if (obj.m_EntityId == EntityId.None)
                    return IntPtr.Zero;

                var retPtr = GetPtrFromInstanceID(obj.m_EntityId, typeof(T), out var isNativeInstanceMonoBehaviour);
                if (retPtr == IntPtr.Zero)
                    return IntPtr.Zero;

                if (!isNativeInstanceMonoBehaviour)
                    return retPtr;

                if (IsMonoBehaviourOrScriptableObjectOrParentClass(obj))
                    return retPtr;

                return IntPtr.Zero;
            }

            static bool IsMonoBehaviourOrScriptableObjectOrParentClass(Object obj)
            {
                // There might be multiple C# objects pointing to the same C++ object. This is not safe for
                // MonoBehaviour/ScriptableObject _derived_ classes that might have additional state in their C# objects,
                // as the C# objects would get out of sync.
                // However, it is safe for multiple MonoBehaviour/ScriptableObject (and parent classes) C# objects to point to
                // the same C++ object, because all the state reachable from a C# reference with such a type is stored in the C++ object
                // and they will therefore always be in sync.

                var objClass = obj.GetType();

                if (objClass == typeof(Object) || objClass == typeof(MonoBehaviour) || objClass == typeof(ScriptableObject))
                    return true;

                return Array.IndexOf(m_MonoBehaviorBaseClasses, objClass) >= 0;
            }

            [NoAutoStaticsCleanup] // m_MonoBehaviourBaseClasses can be reused accross code reloads
            private static readonly Type[] m_MonoBehaviorBaseClasses;

            static MarshalledUnityObject()
            {
                var baseClassList = new List<Type>();
                var baseClass = typeof(MonoBehaviour).BaseType;
                while (baseClass != typeof(Object))
                {
                    baseClassList.Add(baseClass);
                    baseClass = baseClass.BaseType;
                }
                baseClass = typeof(ScriptableObject).BaseType;
                while (baseClass != typeof(Object))
                {
                    baseClassList.Add(baseClass);
                    baseClass = baseClass.BaseType;
                }
                m_MonoBehaviorBaseClasses = baseClassList.ToArray();
            }

            public static void TryThrowEditorNullExceptionObject(Object unityObj) => TryThrowEditorNullExceptionObject(unityObj, null);

            public static void TryThrowEditorNullExceptionObject(Object unityObj, string parameterName)
            {
                string error = unityObj.m_UnityRuntimeErrorString ?? "";
                if (unityObj.m_EntityId != EntityId.None && !error.StartsWith($"{nameof(MissingReferenceException)}:"))
                {
                    error = $"The object of type '{unityObj.GetType().FullName}' has been destroyed but you are still trying to access it.\n" +
                        "Your script should either check if it is null or you should not destroy the object.";

                    if (!string.IsNullOrEmpty(parameterName))
                        error += $" Parameter name: {parameterName}";

                    throw new MissingReferenceException(error);
                }

                var splitIndex = error.IndexOf(':');
                if (splitIndex > 0)
                {
                    var exceptionTypeString = error.Substring(0, splitIndex);
                    error = error.Substring(splitIndex + 1);
                    if (!string.IsNullOrEmpty(parameterName))
                        error += $" Parameter name: {parameterName}";

                    var exceptionType = Type.GetType($"UnityEngine.{exceptionTypeString}", false);
                    if (exceptionType != null)
                        throw (Exception)Activator.CreateInstance(exceptionType, error);
                }
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Unmarshal<T>(IntPtr gcHandlePtr) where T : UnityEngine.Object
            {
                if (gcHandlePtr == IntPtr.Zero)
                    return null;

                var gcHandle = FromIntPtrUnsafe(gcHandlePtr);
                var target = (T)gcHandle.Target;

            // This is to handle the MonoObjectNULL case
            // If the instance ID is zero then this is a fake null object and there is no native
            // object that owns this handle.  It was created in UnityEngineMarshalling.h and
            // needs to be freed here
            if (target.GetEntityId() == EntityId.None)
                gcHandle.Free();
                return target;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static GCHandle FromIntPtrUnsafe(IntPtr gcHandle)
            {
                // Use a unsafe memory cast to avoid the overhead of checking the
                // IntPtr value for validity in the current domain. The Mono class
                // library does this which introduces meaningful overhead. We assume
                // the GC handle we hold is valid for the current domain. There
                // is no such validity check when constructing and retrieving GC
                // handles in C++, and we want to avoid the overhead of the check
                // when doing the equivalent in C#.
                return UnsafeUtility.As<IntPtr, GCHandle>(ref gcHandle);
            }

            public unsafe static void Marshal<T, TCollectionAccessor>(in TCollectionAccessor collectionAccessor, ref MarshalledArray marshalledArray)
                where T : UnityEngine.Object
                where TCollectionAccessor : struct, ICollectionMarshallingAccessor<T>
            {
                // In the editor we may need to send the instanceID's to native.
                // There are some cases where we have a valid instanceID but, but the native pointer will be null.
                // See IsMonoBehaviourOrScriptableObjectOrParentClass
                marshalledArray.size = marshalledArray.size / 2; // If preallocated we're only preallocated for the size of an IntPtr
                MarshalledArray.Allocate<T, TCollectionAccessor>(collectionAccessor, ref marshalledArray, sizeof(IntPtr) + sizeof(EntityId), elementCleanupRequired: false);

                var pointerSpan = marshalledArray.AsSpan<IntPtr>();
                var entityIdSpan = new Span<EntityId>((IntPtr*)marshalledArray.data + pointerSpan.Length, pointerSpan.Length);

                for (int i = 0; i < pointerSpan.Length; i++)
                {
                    var obj = collectionAccessor[i];
                    if (ReferenceEquals(obj, null))
                    {
                        pointerSpan[i] = IntPtr.Zero;
                        entityIdSpan[i] = EntityId.None;
                    }
                    else
                    {
                        pointerSpan[i] = MarshalNotNull(obj);
                        entityIdSpan[i] = obj.m_EntityId;
                    }
                }
            }

            public static void Unmarshal<T, TCollectionAccessor>(in MarshalledArray marshalledArray, ref TCollectionAccessor collectionAccessor)
                where T : UnityEngine.Object
                where TCollectionAccessor : struct, ICollectionMarshallingAccessor<T>
            {
                var span = marshalledArray.GetDataForUnmarshal<T, IntPtr, TCollectionAccessor>(ref collectionAccessor);

                for (int i = 0; i < span.Length; i++)
                    collectionAccessor[i] = Unmarshal<T>(span[i]);
            }

            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            public static void ThrowArgumentNullException(object obj, string parameterName)
            {
                if (obj is UnityEngine.Object unityObj)
                    TryThrowEditorNullExceptionObject(unityObj, parameterName);
                throw new ArgumentNullException(parameterName);
            }

            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            public static void ThrowNullReferenceException(object obj)
            {
                if (obj is UnityEngine.Object unityObj)
                    TryThrowEditorNullExceptionObject(unityObj, null);
                throw new NullReferenceException();
            }
        }
    }
}
