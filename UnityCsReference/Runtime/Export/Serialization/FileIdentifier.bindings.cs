// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 FileIdentifier — 文件标识符
//
// 📌 作用：
//   标识 Unity 资源的来源类型，用于序列化系统中
//   区分不同来源的资源引用。
//
// 💡 FileIdentifierType 枚举：
//   - NonAsset(0)：非资源
//   - SourceAsset(2)：源资源（项目中）
//   - PrimaryArtifact(3)：主要产物（导入后）
//
// ⚡ 用于 Addressables 和资源管理系统。
// ==============================================================

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public enum FileIdentifierType
    {
        NonAsset = 0,
        SourceAsset = 2,
        PrimaryArtifact = 3
    };
}
