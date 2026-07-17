# 🗺️ Unity 引擎源码架构学习指南

> 基于 UnityCsReference **6000.7.0a2** C# 参考源码
> 本指南帮助你从源码角度理解 Unity 引擎的完整架构

---

## 一、源码目录结构总览

```
UnityCsReference/
├── Runtime/           ← ★ 核心引擎运行时（最重要的目录）
│   └── Export/        ← 公开 API 定义（C# 接口层）
├── Modules/           ← 引擎各功能模块（共 159 个模块）
├── Editor/            ← Unity Editor 编辑器代码
├── External/          ← 外部依赖库
├── Projects/          ← 解决方案文件 (.sln / .csproj)
└── Tools/             ← 开发工具
```

---

## 二、引擎核心架构：4 层模型

理解 Unity 源码的关键是掌握以下 **4 层架构**：

```
┌──────────────────────────────────────────┐
│  ▲ 第1层: C# 用户脚本层                    │  ← 你写的 MonoBehaviour 脚本
│    你的自定义 MonoBehaviour 脚本           │
├──────────────────────────────────────────┤
│  ▲ 第2层: C# 引擎 API 层                  │  ← 本仓库的核心内容
│    Runtime/Export/Scripting/              │
│    → Object, Component, GameObject,       │
│      MonoBehaviour, Transform 等          │
├──────────────────────────────────────────┤
│  ▲ 第3层: C# Native Bindings 层            │  ← C# ←→ C++ 桥接
│    ScriptBindings/ 目录                    │
│    → 使用 [FreeFunction] [NativeMethod]    │
│      等 Attribute 声明内部调用              │
├──────────────────────────────────────────┤
│  ▲ 第4层: C++ 原生引擎层                   │  ← 未开源（商业许可）
│    Runtime/ 下的 .h/.cpp 文件              │
│    → 真正的性能关键逻辑、渲染、物理等       │
└──────────────────────────────────────────┘
```

### 🔑 关键理解

| 层 | 代码位置 | 作用 | 是否可看 |
|----|---------|------|---------|
| 用户脚本 | 你的项目 | 游戏逻辑 | ✅ |
| C# API | `Runtime/Export/*/` | Unity 提供的 C# 接口 | ✅ **本仓库** |
| Native Bindings | `Modules/*/ScriptBindings/` | C# 调用 C++ 的桥接 | ✅ **本仓库** |
| C++ 引擎 | `Runtime/` (C++部分) | 实际渲染/物理/内存管理 | ❌ 需商业许可 |

---

## 三、核心类继承链（最重要！）

这是 Unity 的**类继承体系**，所有引擎功能建立在此之上：

```
UnityEngine.Object                          ← 一切对象的基类
├── GameObject                              ← 场景中的游戏对象（容器）
├── Component                               ← 可挂载到 GameObject 上的组件
│   ├── Behaviour                           ← 可启用/禁用的组件
│   │   ├── MonoBehaviour                   ← ★ 用户脚本的基类
│   │   ├── Light                           ← 光照组件
│   │   └── CanvasRenderer                  ← UI 渲染组件
│   ├── Transform                           ← ★ 位置/旋转/缩放（每个GameObject必有）
│   ├── Collider                            ← 碰撞器基类
│   │   ├── BoxCollider
│   │   ├── SphereCollider
│   │   └── MeshCollider
│   ├── Renderer                            ← 渲染器基类
│   │   ├── MeshRenderer
│   │   └── SkinnedMeshRenderer
│   └── Camera                              ← 摄像机
└── ScriptableObject                        ← 数据资产基类
```

### 📍 对应源码位置

| 类 | 源码文件 | 关键理解 |
|----|---------|---------|
| `Object` | `Runtime/Export/Scripting/UnityEngineObject.bindings.cs` | 所有引擎对象的根，管理 EntityId、name、Destroy/Instantiate |
| `GameObject` | `Runtime/Export/Scripting/GameObject.bindings.cs` | 组件容器，管理 active/layer/tag/组件增删查 |
| `Component` | `Runtime/Export/Scripting/Component.bindings.cs` | 可挂载到 GameObject 的基类，提供 GetComponent 系列方法 |
| `Behaviour` | `Runtime/Export/Scripting/Behaviour.bindings.cs` | 引入 enabled 属性，控制组件的启用/禁用 |
| `MonoBehaviour` | `Runtime/Export/Scripting/MonoBehaviour.bindings.cs` | 用户脚本基类，包含 Unity 生命周期回调 + 协程系统 |
| `Transform` | `Runtime/Transform/ScriptBindings/Transform.bindings.cs` | 位置/旋转/缩放 + 层级父子关系 |
| `ScriptableObject` | `Runtime/Export/Scripting/ScriptableObject.bindings.cs` | 数据保存的基类 |

---

## 四、15 大核心模块源码速查

### 4.1 引擎生命周期系统

| 文件路径 | 内容 |
|---------|------|
| `Runtime/Export/PlayerLoop/PlayerLoop.bindings.cs` | **主循环** - Unity 每帧执行的底层循环 |
| `Runtime/Export/Application/Application.bindings.cs` | Application 类的 Native 绑定（退出、焦点、后台运行） |
| `Runtime/Export/Application/Application.cs` | Application 的 C# 实现（平台判断、运行时数据路径） |
| `Runtime/Scripting/LifecycleManagement/` | **Domain Reload** - 域重载生命周期管理 |
| `Runtime/Export/SceneManager/SceneManager.bindings.cs` | 场景管理 Native 接口 |
| `Runtime/Export/SceneManager/SceneManager.cs` | 场景管理 C# 实现（LoadScene/UnloadScene 事件） |
| `Runtime/Export/SceneManager/Scene.bindings.cs` | Scene 结构体 |
| `Runtime/Export/SceneManager/Scene.cs` | Scene 的 C# 扩展方法 |

### 4.2 核心运行时

| 文件路径 | 内容 |
|---------|------|
| `Runtime/Export/Scripting/Object.bindings.cs` | 基类 Object |
| `Runtime/Export/Scripting/GameObject.bindings.cs` | GameObject |
| `Runtime/Export/Scripting/Component.bindings.cs` | Component |
| `Runtime/Export/Scripting/Behaviour.bindings.cs` | Behaviour |
| `Runtime/Export/Scripting/MonoBehaviour.bindings.cs` | MonoBehaviour |
| `Runtime/Export/Scripting/ScriptableObject.bindings.cs` | ScriptableObject |
| `Runtime/Export/Scripting/Coroutine.bindings.cs` | 协程定义 |
| `Runtime/Transform/ScriptBindings/Transform.bindings.cs` | Transform |
| `Runtime/Transform/ScriptBindings/RectTransform.bindings.cs` | RectTransform (UI 变换) |

### 4.3 时间 & 输入

| 文件路径 | 内容 |
|---------|------|
| `Runtime/Export/Time/` | Time 类（deltaTime, timeScale, frameCount） |
| `Runtime/Export/Input/` | Input 类（旧版输入系统） |
| `Modules/Input/ScriptBindings/` | 新版 Input System 绑定 |
| `Modules/InputLegacy/` | 旧版输入系统模块 |
| `Runtime/Export/TouchScreenKeyboard/` | 触摸键盘 |

### 4.4 渲染系统

| 文件路径 | 内容 |
|---------|------|
| `Runtime/Export/Camera/` | Camera 类 |
| `Runtime/Export/Rendering/` | 通用渲染 API |
| `Runtime/Export/Graphics/` | Graphics 绘制 API |
| `Runtime/Export/RenderPipeline/` | SRP (Scriptable Render Pipeline) |
| `Runtime/Export/Shaders/` | Shader 相关 API |
| `Runtime/Export/GI/` | 全局光照 |
| `Runtime/Export/2D/` | 2D 渲染 |
| `Modules/VFX/` | Visual Effect Graph |

### 4.5 物理系统

| 文件路径 | 内容 |
|---------|------|
| `Modules/Physics/ScriptBindings/` | 3D 物理绑定 (PhysX) |
| `Modules/Physics/Managed/` | 3D 物理 C# 实现 |
| `Modules/Physics2D/` | 2D 物理系统 (Box2D) |
| `Modules/Cloth/` | 布料模拟 |
| `Modules/Vehicles/` | 车辆系统 (WheelCollider) |
| `Modules/TerrainPhysics/` | 地形物理 |

### 4.6 UI 系统

| 文件路径 | 内容 |
|---------|------|
| `Modules/UI/ScriptBindings/` | uGUI 系统绑定 |
| `Modules/UI/Managed/` | uGUI C# 实现（Canvas, Graphic, Button 等） |
| `Modules/UIElements/` | UI Toolkit (新 UI 系统) |
| `Modules/IMGUI/` | 即时模式 GUI (OnGUI) |
| `Runtime/Export/IMGUI/` | IMGUI 核心 API |

### 4.7 动画系统

| 文件路径 | 内容 |
|---------|------|
| `Modules/Animation/ScriptBindings/` | Animator / Animation 绑定 |
| `Modules/Animation/Managed/` | 动画 C# 实现 |
| `Runtime/Export/Animation/` | 动画核心 API |
| `Modules/Director/` | Timeline (Playable API) |

### 4.8 资源管理

| 文件路径 | 内容 |
|---------|------|
| `Runtime/Export/Resources/` | Resources API（Resources.Load） |
| `Modules/AssetDatabase/` | AssetDatabase（编辑器用） |
| `Modules/AssetBundle/` | AssetBundle 系统 |
| `Modules/ContentLoad/` | 内容加载系统 |
| `Modules/Streaming/` | Streaming 管理 |
| `Runtime/Export/Caching/` | 缓存系统 |

### 4.9 序列化 & 数据

| 文件路径 | 内容 |
|---------|------|
| `Runtime/Export/Serialization/` | 序列化 API |
| `Modules/JSONSerialize/` | JSON 序列化 |
| `Runtime/Export/Properties/` | Property 系统 |
| `Runtime/Export/PropertyName/` | PropertyName |

### 4.10 音频系统

| 文件路径 | 内容 |
|---------|------|
| `Modules/Audio/ScriptBindings/` | AudioSource/AudioListener 绑定 |
| `Runtime/Export/Audio/` | 音频核心 API |
| `Modules/DSPGraph/` | DSP 音频图 |

### 4.11 粒子系统

| 文件路径 | 内容 |
|---------|------|
| `Modules/ParticleSystem/ScriptBindings/` | ParticleSystem 绑定 |
| `Modules/ParticleSystem/Managed/` | 粒子系统 C# 实现 |

### 4.12 Job System & Burst

| 文件路径 | 内容 |
|---------|------|
| `Runtime/Jobs/ScriptBindings/` | IJob / JobHandle 绑定 |
| `Runtime/Jobs/Managed/` | Job System C# 实现 |
| `Runtime/Export/Jobs/` | 公开 Job API |
| `Modules/Burst/` | Burst 编译器 |

### 4.13 网络系统

| 文件路径 | 内容 |
|---------|------|
| `Modules/UnityWebRequest/` | UnityWebRequest HTTP 请求 |
| `Modules/UNet/` | 旧版 UNet 网络 |
| `Runtime/Export/Networking/` | 网络核心 API |
| `Modules/Multiplayer/` | 多人游戏系统 |
| `Modules/GameCenter/` | Apple Game Center |

### 4.14 AI & 导航

| 文件路径 | 内容 |
|---------|------|
| `Modules/AI/` | NavMesh 导航系统 |
| `Runtime/Export/NavMesh/` | NavMesh 核心 API |

### 4.15 XR & 平台相关

| 文件路径 | 内容 |
|---------|------|
| `Modules/XR/` | XR 子系统 |
| `Runtime/Export/XR/` | XR 核心 API |
| `Runtime/Export/Apple/` | Apple 平台特性 |
| `Runtime/Export/Windows/` | Windows 平台特性 |
| `Runtime/Export/iOS/` | iOS 平台特性 |
| `Runtime/Export/Lumin/` | Magic Leap 平台 |

---

## 五、推荐学习路径（由浅入深）

### Phase 1: 引擎根基（第 1-7 天）
```
Day 1: Object 类 ← 一切之基
  → Runtime/Export/Scripting/UnityEngineObject.bindings.cs

Day 2: GameObject ← 场景中的实体容器
  → Runtime/Export/Scripting/GameObject.bindings.cs

Day 3: Component ← 可挂载组件基类
  → Runtime/Export/Scripting/Component.bindings.cs

Day 4: Behaviour + MonoBehaviour ← 你写的每行代码都继承自这里
  → Runtime/Export/Scripting/Behaviour.bindings.cs
  → Runtime/Export/Scripting/MonoBehaviour.bindings.cs

Day 5: Transform ← 每个 GameObject 都有的位置信息
  → Runtime/Transform/ScriptBindings/Transform.bindings.cs

Day 6: Scene Management ← 场景如何加载卸载
  → Runtime/Export/SceneManager/

Day 7: PlayerLoop ← Unity 的主循环
  → Runtime/Export/PlayerLoop/PlayerLoop.bindings.cs
```

### Phase 2: 核心系统（第 8-14 天）
```
Day 8:  Application + Time
  → Runtime/Export/Application/
  → Runtime/Export/Time/

Day 9:  Camera + Rendering
  → Runtime/Export/Camera/
  → Runtime/Export/Rendering/

Day 10: Graphics + RenderPipeline (SRP)
  → Runtime/Export/Graphics/
  → Runtime/Export/RenderPipeline/

Day 11: Physics 3D
  → Modules/Physics/

Day 12: Physics 2D
  → Modules/Physics2D/

Day 13: Animation
  → Modules/Animation/

Day 14: Audio
  → Modules/Audio/
```

### Phase 3: 系统深入（第 15-21 天）
```
Day 15: UI (uGUI)
  → Modules/UI/

Day 16: UI Toolkit
  → Modules/UIElements/

Day 17: Particle System
  → Modules/ParticleSystem/

Day 18: Asset Management (Resources/AssetBundle)
  → Runtime/Export/Resources/
  → Modules/AssetBundle/

Day 19: Serialization
  → Runtime/Export/Serialization/
  → Modules/JSONSerialize/

Day 20: Job System + Burst
  → Runtime/Jobs/

Day 21: AI/NavMesh
  → Modules/AI/
```

### Phase 4: 进阶主题（第 22-28 天）
```
Day 22: UnityWebRequest
  → Modules/UnityWebRequest/

Day 23: Scriptable Render Pipeline
  → Runtime/Export/RenderPipeline/

Day 24: Visual Effect Graph
  → Modules/VFX/

Day 25: Timeline
  → Modules/Director/

Day 26: Input System
  → Modules/Input/

Day 27: XR 系统
  → Modules/XR/

Day 28: 回顾与总结
```

---

## 六、源码阅读技巧

### 6.1 理解 Bindings 模式

Unity 源码中最常见的模式：

```csharp
// 声明一个外部(C++原生)方法
[FreeFunction("GameObjectBindings::Find")]
public static extern GameObject Find(string name);

// [FreeFunction] → 声明这是一个自由函数（非成员函数）
// "GameObjectBindings::Find" → C++ 端的实际函数名
// extern → 表示实现由外部提供
```

### 6.2 常用 Attribute 含义

| Attribute | 含义 |
|-----------|------|
| `[FreeFunction("X::Y")]` | 绑定到 C++ 自由函数 X::Y |
| `[NativeHeader("path.h")]` | 对应的 C++ 头文件路径 |
| `[NativeMethod(Name="X")]` | 绑定到 C++ 成员方法 |
| `[RequiredByNativeCode]` | 该方法会被 C++ 端反向调用 |
| `[UsedByNativeCode]` | 该类/方法在 C++ 中有引用 |
| `[VisibleToOtherModules]` | 允许其他模块访问（internal 但跨程序集） |
| `[TypeInferenceRule(...)]` | 帮助 C# 编译器推断泛型类型 |

### 6.3 寻找对应关系

```
C# 属性 → extern get/set
  例如: public extern string name { get; set; }

C# 方法 → [FreeFunction] + extern
  例如: [FreeFunction("GameObjectBindings::SetActive")]
         public extern void SetActive(bool value);

C# 委托事件 → C++ 回调
  例如: SceneManager.sceneLoaded 事件
```

### 6.4 调试技巧

在反编译的帮助下理解完整流程：
1. 在 VS 中打开 `Projects/CSharp/UnityReferenceSource.sln`
2. 使用"转到定义"(F12) 追踪 API 调用链
3. 搜索 `internal extern` 找到 Native Bindings
4. 搜索 `[RequiredByNativeCode]` 找到被 C++ 调用的 C# 方法

---

## 七、架构模式总结

### Unity 的 5 大设计模式

| 模式 | 应用位置 | 说明 |
|------|---------|------|
| **组件模式** | Component → GameObject | 实体-组件架构，通过组合实现功能 |
| **观察者模式** | Events, UnityAction | 事件系统，如场景加载事件 |
| **对象池模式** | `Runtime/Export/ObjectPool/` | 对象池，减少 GC |
| **生命周期回调** | MonoBehaviour 的 Awake/Start/Update | 模板方法模式 |
| **双缓冲** | `PlayerLoop` | 主循环中 FixedUpdate/Update 分离 |

### EntityId 系统（最新架构）

Unity 6000+ 引入了 `EntityId` 替代旧的 `InstanceID`：

```
EntityId 布局: [Version:24 | TypeId:12 | Index:28] = 64位
- Index: 低 28 位，对象索引
- TypeId: 中间 12 位，类型标识
- Version: 高 24 位，版本号（防止悬垂引用）
```

---

## 八、快速定位表

| 你想找什么 | 在哪个目录 |
|-----------|----------|
| 所有引擎 API 入口 | `Runtime/Export/` |
| MonoBehaviour 回调 | `Runtime/Export/Scripting/MonoBehaviour.bindings.cs` |
| GameObject 相关 | `Runtime/Export/Scripting/GameObject.bindings.cs` |
| Transform | `Runtime/Transform/ScriptBindings/Transform.bindings.cs` |
| 物理系统 | `Modules/Physics/` |
| 物理 2D | `Modules/Physics2D/` |
| uGUI UI | `Modules/UI/` |
| UI Toolkit | `Modules/UIElements/` |
| 动画/Animator | `Modules/Animation/` |
| 粒子系统 | `Modules/ParticleSystem/` |
| 音频 | `Modules/Audio/` |
| 摄像机 | `Runtime/Export/Camera/` |
| 渲染管线 | `Runtime/Export/RenderPipeline/` |
| 场景管理 | `Runtime/Export/SceneManager/` |
| 资源加载 | `Runtime/Export/Resources/` |
| AssetBundle | `Modules/AssetBundle/` |
| 协程 | `Runtime/Export/Scripting/Coroutine.bindings.cs` |
| 时间 Time | `Runtime/Export/Time/` |
| 输入 Input | `Runtime/Export/Input/` |
| 序列化 | `Runtime/Export/Serialization/` |
| JSON | `Modules/JSONSerialize/` |
| Job System | `Runtime/Export/Jobs/`, `Runtime/Jobs/` |
| 对象池 | `Runtime/Export/ObjectPool/` |
| 编辑器扩展 | `Editor/Mono/` |
| 场景视图 | `Modules/SceneView/` |

---

> 💡 **提示**：本仓库源码附带大量中文注释在核心文件中，帮助你理解每行代码的作用。
> 建议按照 Phase 1 的顺序，每天阅读对应的注释源码文件。
