// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 TypeTreeStoreManager — 类型树存储管理器
//
// 📌 作用：
//   管理 Unity 序列化系统中的类型树（TypeTree）缓存。
//   类型树是 Unity 用于跨版本序列化兼容性的关键机制。
//
// 💡 核心功能：
//   - EnableTypeTreeForCurrentThread：为当前线程启用类型树
//   - DisableTypeTreeForCurrentThread：禁用类型树
//
// ⚡ 类型树用于：
//   1. 跨 Unity 版本的序列化兼容性
//   2. 在反序列化时重建缺失类型
//   3. 调试和编辑器功能
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Serialize/TypeTreeStoreManager.h")]
    [StaticAccessor("GetTypeTreeStoreManager()", StaticAccessorType.Dot)]
    public static class TypeTreeStoreManager
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SourceHandle
        {
            UInt64 m_Handle;
        }
        public extern static SourceHandle AddTypeTreeSourceFromFile(string path);
        public extern static bool RemoveTypeTreeSource(SourceHandle handle);

        [StructLayout(LayoutKind.Sequential)]
        internal struct DiagnosticCacheInfo
        {
            public int typeTreeMemoryUsage;
            public Hash128[] typeTreeHashes; 
        }
        internal extern static DiagnosticCacheInfo GetTypeTreeCacheDiagnosticInfo();
    }
}
