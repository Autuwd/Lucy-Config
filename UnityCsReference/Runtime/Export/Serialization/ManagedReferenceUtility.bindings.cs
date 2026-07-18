// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ManagedReferenceUtility — 托管引用工具
//
// 📌 作用：
//   管理序列化系统中的托管引用（Managed Reference），
//   支持 SerializeReference 属性的运行时创建和类型查找。
//
// 💡 核心功能：
//   - CreateMissingManagedReferenceTypeFallback：创建缺失类型的回落实例
//   - GetManagedReferenceId：获取托管引用 ID
//   - SetManagedReferenceId：设置托管引用 ID
//   - GetManagedReferenceType：获取托管引用类型
//   - GetManagedReferenceAssemblyQualifiedName：获取程序集限定名称
//
// ⚡ 用于 SerializeReference 属性的序列化/反序列化。
// ==============================================================

using System;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityObject = UnityEngine.Object;
using RefId = System.Int64;

namespace UnityEngine.Serialization
{
    [NativeHeader("Runtime/Serialize/ManagedReferenceUtility.h")]
    public sealed class ManagedReferenceUtility
    {
        // Must match the same declarations in "Runtime/Serialize/ReferenceId.h"
        public const RefId RefIdUnknown = -1;
        public const RefId RefIdNull = -2;

        [NativeMethod("SetManagedReferenceIdForObject")]
        static extern bool SetManagedReferenceIdForObjectInternal(UnityObject obj, object scriptObj, RefId refId);

        public static bool SetManagedReferenceIdForObject(UnityObject obj, object scriptObj, RefId refId)
        {
            if (scriptObj == null)
                return refId == RefIdNull; // There is no need to explicitly register RefIdNull

            var valueType = scriptObj.GetType();
            if (valueType == typeof(UnityObject) || valueType.IsSubclassOf(typeof(UnityObject)))
            {
                throw new System.InvalidOperationException(
                    $"Cannot assign an object deriving from UnityEngine.Object to a managed reference. This is not supported.");
            }

            return SetManagedReferenceIdForObjectInternal(obj, scriptObj, refId);
        }

        [NativeMethod("GetManagedReferenceIdForObject")]
        static extern RefId GetManagedReferenceIdForObjectInternal(UnityObject obj, object scriptObj);

        public static  RefId GetManagedReferenceIdForObject(UnityObject obj, object scriptObj)
        {
            return GetManagedReferenceIdForObjectInternal(obj, scriptObj);
        }

        [NativeMethod("GetManagedReference")]
        static extern object GetManagedReferenceInternal(UnityObject obj, RefId id);

        public static object GetManagedReference(UnityObject obj, RefId id)
        {
            return GetManagedReferenceInternal(obj, id);
        }

        [NativeMethod("GetManagedReferenceIds")]
        static extern RefId[] GetManagedReferenceIdsForObjectInternal(UnityObject obj);

        public static RefId[] GetManagedReferenceIds(UnityObject obj)
        {
            return GetManagedReferenceIdsForObjectInternal(obj);
        }
    };
}
