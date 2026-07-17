# 🎮 Unity 6000.7.0a2 C# 参考源码

> 📖 **中文学习注释版** — 核心文件已添加详细中文注释帮助学习引擎架构

## 目录结构

```
├── Runtime/Export/       ← ★ 核心引擎 API（最重要的阅读入口）
├── Modules/              ← 引擎各功能模块（159个）
├── Editor/               ← 编辑器代码
├── Projects/CSharp/      ← 解决方案文件
├── External/             ← 外部依赖
└── 学习指南_Unity源码架构地图.md ← ★ 学习路线图
```

## 📚 学习路线

1. **先读** → `学习指南_Unity源码架构地图.md`（了解架构全貌）
2. **再读** → `Runtime/Export/Scripting/`（核心类：Object → GameObject → Component → Behaviour → MonoBehaviour）
3. **深入** → 按指南中的 Phase 2-4 逐模块学习

## 🔑 核心类继承链（必读文件）

| 类 | 文件路径 | 建议先读 |
|---|---------|:-------:|
| `Object` | `Runtime/Export/Scripting/UnityEngineObject.bindings.cs` | ⭐ ① |
| `GameObject` | `Runtime/Export/Scripting/GameObject.bindings.cs` | ⭐ ② |
| `Component` | `Runtime/Export/Scripting/Component.bindings.cs` | ⭐ ③ |
| `Behaviour` | `Runtime/Export/Scripting/Behaviour.bindings.cs` | ⭐ ④ |
| `MonoBehaviour` | `Runtime/Export/Scripting/MonoBehaviour.bindings.cs` | ⭐ ⑤ |
| `Transform` | `Runtime/Transform/ScriptBindings/Transform.bindings.cs` | ⭐ ⑥ |
| `ScriptableObject` | `Runtime/Export/Scripting/ScriptableObject.bindings.cs` | ⭐ ⑦ |

## 🔧 使用方式

### 在 Visual Studio 中浏览
1. 打开 `Projects/CSharp/UnityReferenceSource.sln`
2. 使用 **Ctrl+/** 注释/取消注释
3. **F12** 转到定义，追踪 API 调用链

### 学习顺序建议
```
第1周：核心类继承链（Object → MonoBehaviour）
第2周：场景管理、Application、PlayerLoop
第3周：渲染系统、摄像机
第4周：物理系统、UI系统、动画系统
第5周：资源管理、协程、Job System
```

## 📝 注释说明

本仓库的核心 C# 文件已添加了详细的中文注释，
帮助你直观了解每行代码的作用和设计意图。

注释风格：
```csharp
// ==============================================================
// 🎯 功能标题
//
// 📌 作用：简短说明
//
// 🔑 关键点：详细解释
//
// 💡 理解关键：有助于理解的背景知识
// ==============================================================
```

## 🏗 架构快速理解

```
你的脚本 (MonoBehaviour)
    ↓ 继承
Unity C# API (本仓库)
    ↓ [FreeFunction] extern 绑定
C++ 原生引擎 (未开源)
    ↓ 系统调用
操作系统/GPU/PhysX/...
```

## 官方说明

The C# part of the Unity engine and editor source code.
May be used for reference purposes only.

For terms of use, see
https://unity3d.com/legal/licenses/Unity_Reference_Only_License

The repository includes third-party code subject to [third-party
notices](third-party-notices.txt).

The terms of use do not permit you to modify or redistribute the C#
code (in either source or binary form). If you want to modify Unity's
source code (C# and C++), contact Unity sales for a commercial source
code license: https://store.unity.com/contact?type=source-code

We do not take pull requests at this time. But if you find
something that looks like a bug, file it using the Unity Bug Reporter.

The C# solution is in `Projects/CSharp/UnityReferenceSource.sln`
The folder layout matches the Unity source tree layout.