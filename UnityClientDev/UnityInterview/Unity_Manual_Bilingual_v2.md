# Unity 6.4 User Manual (Unity 6.4 用户手册)

> **Source (来源):** [Unity Official Documentation](https://docs.unity3d.com/Manual/UnityManual.html)
> **Version (版本):** Unity 6.4 (6000.4)
> **Generated (生成日期):** 2026-06-03
> **Format (格式):** English paragraph → Chinese translation | Title: English (中文)
> **Revision (修订说明):** v2 — Added official images, corrected errors, filled omissions vs. the live Unity 6.4 Manual.

---

## Table of Contents (目录)

1. [Unity Overview (Unity 概述)](#1-unity-overview-unity-概述)
2. [The Unity Editor Interface (Unity 编辑器界面)](#2-the-unity-editor-interface-unity-编辑器界面)
3. [GameObjects and Components (游戏对象与组件)](#3-gameobjects-and-components-游戏对象与组件)
4. [Programming in Unity / Scripting (Unity 编程与脚本)](#4-programming-in-unity--scripting-unity-编程与脚本)
5. [Physics (物理系统)](#5-physics-物理系统)
6. [Animation (动画系统)](#6-animation-动画系统)
7. [Audio (音频系统)](#7-audio-音频系统)
8. [Lighting (光照系统)](#8-lighting-光照系统)
9. [Render Pipelines (渲染管线)](#9-render-pipelines-渲染管线)
10. [2D Game Development (2D 游戏开发)](#10-2d-game-development-2d-游戏开发)
11. [UI Systems (UI 系统)](#11-ui-systems-ui-系统)
12. [Visual Effects (视觉效果)](#12-visual-effects-视觉效果)
13. [Multiplayer (多人游戏)](#13-multiplayer-多人游戏)
14. [XR — VR / AR / MR (扩展现实)](#14-xr--vr--ar--mr-扩展现实)
15. [Unity AI (Unity 人工智能)](#15-unity-ai-unity-人工智能)
16. [Unity Services (Unity 服务)](#16-unity-services-unity-服务)
17. [Performance and Optimization (性能与优化)](#17-performance-and-optimization-性能与优化)
18. [Asset Management (资源管理)](#18-asset-management-资源管理)
19. [Building and Publishing (构建与发布)](#19-building-and-publishing-构建与发布)

---

> ### 📋 Corrections and Additions vs. v1 (相对于 v1 版的修正与补充)
>
> | # | Section (章节) | Issue type (问题类型) | Detail (详情) |
> |---|---|---|---|
> | 1 | Physics — Rigidbody | Omission (疏漏) | Added official sub-sections: "Rigid body GameObjects with physics-based movement", "without physics-based movement", and "Rigidbody optimization". Official page title corrected to "Introduction to rigid body physics". |
> | 2 | Physics — Colliders | Omission (疏漏) | Added missing "Collider surfaces" sub-section (PhysicsMaterial, friction, bounciness). |
> | 3 | Programming — Event Functions | Structure error (结构错误) | Added missing "Input events" category label; reorganised to match the official grouping: Regular update / Initialization / GUI / Input / Physics events. |
> | 4 | Animation — State Machine | Omission (疏漏) | Added official sub-topic table (Animation states, Parameters, Transitions, Blend trees, Behaviors, Sub-state machines, Layers, Solo/Mute, Target matching). |
> | 5 | Animation — Blend Trees | Omission (疏漏) | Added official sub-types: 1D / 2D / Direct blending; added the official Animation Parameters code example from the manual. |
> | 6 | NavMesh URL | Error (错误) | Corrected URL from `Navigation.html` (404) to the AI Navigation package. |
> | 7 | URP/HDRP intro URLs | Error (错误) | Noted correct package-level URLs; removed broken `urp-introduction.html` / `hdrp-introduction.html`. |
> | 8 | Shadows URL | Error (错误) | Corrected from `ShadowOverview.html` (404) to `Shadows.html`. |
> | 9 | All sections | Missing images (缺少图片) | Added 7 official images sourced directly from the Unity docs CDN. |

---

## 1. Unity Overview (Unity 概述)

Unity is a cross-platform game engine developed by Unity Technologies. It supports development for over 20 platforms including PC, Mac, Linux, iOS, Android, WebGL, consoles (PlayStation, Xbox, Nintendo Switch), and XR devices. Unity is used to build games, simulations, film/animation previsualization, architectural visualization, training applications, and more.

Unity 是 Unity Technologies 开发的跨平台游戏引擎，支持超过 20 个平台的开发，包括 PC、Mac、Linux、iOS、Android、WebGL、主机（PlayStation、Xbox、Nintendo Switch）和 XR 设备。Unity 被广泛用于构建游戏、仿真、影视/动画预可视化、建筑可视化、培训应用等。

Unity 6.4 (internal version 6000.4) is the latest stable release of the Unity 6 family. It introduces major improvements in rendering performance, AI tooling, multiplayer infrastructure, and multiplatform support. The editor runs on Windows, macOS, and Linux.

Unity 6.4（内部版本 6000.4）是 Unity 6 系列的最新稳定版本。它在渲染性能、AI 工具、多人游戏基础设施和多平台支持方面引入了重大改进。编辑器可在 Windows、macOS 和 Linux 上运行。

### Key Highlights of Unity 6 (Unity 6 核心亮点)

**Boost rendering performance (提升渲染性能)**

Elevate your scenes with scalable, captivating visuals using the latest advances in rendering, lighting, and visual effects.

使用渲染、光照和视觉效果方面的最新进展，以可扩展、引人入胜的视觉效果提升您的场景表现。

**Multiplayer game creation (多人游戏创作)**

Simplify multiplayer game creation with Unity's multiplayer packages and services.

利用 Unity 的多人游戏包和服务，简化多人游戏的创建流程。

**Expand multiplatform reach (扩展多平台覆盖)**

Build better experiences for mobile platforms, including a newly optimized runtime for mobile browsers, and get the latest multiplatform advances for all supported platforms.

为移动平台构建更好的体验，包括为移动浏览器提供全新优化的运行时，并获取所有支持平台的最新多平台进展。

**Unlock possibilities with Unity AI (解锁 Unity AI 的无限可能)**

Accelerate creativity and development with AI tools for code generation, asset creation, and runtime inference, all integrated into the Unity Editor and Dashboard.

通过集成在 Unity 编辑器和控制面板中的 AI 工具（包括代码生成、资源创建和运行时推理），加速创意与开发。

**Achieve more engaging visuals (实现更具吸引力的视觉效果)**

Create more engaging visuals with the latest updates to Lighting, Graphics performance and profiling, Shader Graph, and Visual Effect Graph.

利用光照、图形性能与分析、着色器图（Shader Graph）和视觉效果图（Visual Effect Graph）的最新更新，打造更具吸引力的视觉效果。

**Enhance productivity and functionality (提升生产力与功能性)**

Improve productivity and functionality across your entire Unity development environment with better profiling options, ProBuilder, Cinemachine, and UI Toolkit.

通过更完善的性能分析选项、ProBuilder、Cinemachine 和 UI Toolkit，全面提升 Unity 开发环境的生产力与功能性。

---

## 2. The Unity Editor Interface (Unity 编辑器界面)

The Unity Editor is the main working environment for building Unity projects. It is organized into several key windows (called "panels" or "views") that can be rearranged and docked to suit your workflow.

Unity 编辑器是构建 Unity 项目的主要工作环境。它由几个关键窗口（称为"面板"或"视图"）组成，这些窗口可以根据您的工作流程重新排列和停靠。

### Main Windows (主要窗口)

**Scene View (场景视图)**

The Scene view is the interactive sandbox where you build and arrange your game world. You can select and manipulate GameObjects, navigate the scene using the mouse and keyboard shortcuts (WASD for fly mode, Alt+drag to orbit, scroll to zoom), and toggle between 2D and 3D modes. The toolbar at the top provides transform tools: Move (W), Rotate (E), Scale (R), Rect (T), and the combined Transform tool (Y).

场景视图是构建和排列游戏世界的交互式沙盒。您可以在其中选择和操控游戏对象，使用鼠标和键盘快捷键导航场景（WASD 用于飞行模式，Alt+拖动用于环绕旋转，滚轮用于缩放），并在 2D 和 3D 模式之间切换。顶部工具栏提供变换工具：移动（W）、旋转（E）、缩放（R）、矩形（T）和组合变换工具（Y）。

**Game View (游戏视图)**

The Game View renders your scene from the perspective of the active Camera. It shows what players will see when they run the game. You can select different aspect ratios and resolutions from the dropdown, enable Stats to see rendering statistics (FPS, draw calls, triangles, vertices, etc.), and use the Gizmos toggle to show or hide visual aids during Play Mode.

游戏视图从当前活动摄像机的视角渲染您的场景，显示玩家运行游戏时将看到的内容。您可以从下拉菜单中选择不同的宽高比和分辨率，启用 Stats 以查看渲染统计信息（FPS、绘制调用、三角形数、顶点数等），并使用 Gizmos 开关在播放模式下显示或隐藏可视化辅助工具。

**Hierarchy Window (层级窗口)**

The Hierarchy window displays every GameObject in the current scene in a tree structure. Parent-child relationships are shown by indentation. You can create new GameObjects, search/filter by name or component type, and drag GameObjects to reparent them. Selecting a GameObject here also selects it in the Scene view.

层级窗口以树形结构显示当前场景中的每个游戏对象。父子关系通过缩进显示。您可以创建新的游戏对象，按名称或组件类型搜索/筛选，以及拖动游戏对象来重新设置父级。在此处选择游戏对象同时也会在场景视图中选中它。

**Project Window (项目窗口)**

The Project window shows all the assets in your project (textures, scripts, prefabs, scenes, audio clips, materials, etc.). Assets are stored in the `Assets` folder on disk and are organized in subfolders. You can import new assets by dragging files into this window or using `Assets → Import New Asset`. The `Packages` folder shows packages installed from the Package Manager.

项目窗口显示项目中的所有资源（纹理、脚本、预制件、场景、音频剪辑、材质等）。资源存储在磁盘上的 `Assets` 文件夹中，并按子文件夹组织。您可以通过将文件拖入此窗口或使用"资源 → 导入新资源"来导入新资源。`Packages` 文件夹显示从包管理器安装的包。

**Inspector Window (检视器窗口)**

The Inspector shows the properties of the currently selected GameObject, asset, or imported file. For a GameObject, it lists all attached Components and their editable properties. You can add new Components using the "Add Component" button at the bottom. For assets, the Inspector shows Import Settings relevant to the asset type (texture compression, audio format, model import options, etc.).

检视器显示当前选定的游戏对象、资源或导入文件的属性。对于游戏对象，它列出所有附加的组件及其可编辑属性。您可以使用底部的"添加组件"按钮添加新组件。对于资源，检视器显示与资源类型相关的导入设置（纹理压缩、音频格式、模型导入选项等）。

**Console Window (控制台窗口)**

The Console displays log messages, warnings, and errors from both the Unity Editor and your scripts. Use `Debug.Log()`, `Debug.LogWarning()`, and `Debug.LogError()` in code to write messages here. You can filter by message type, clear the log, and enable "Error Pause" to automatically pause play mode when an error occurs.

控制台显示来自 Unity 编辑器和您的脚本的日志消息、警告和错误。在代码中使用 `Debug.Log()`、`Debug.LogWarning()` 和 `Debug.LogError()` 来在此处写入消息。您可以按消息类型过滤、清除日志，并启用"Error Pause（错误暂停）"以在发生错误时自动暂停播放模式。

### Essential Keyboard Shortcuts (常用键盘快捷键)

| Shortcut (快捷键) | Action (操作) |
|---|---|
| `W` | Move tool (移动工具) |
| `E` | Rotate tool (旋转工具) |
| `R` | Scale tool (缩放工具) |
| `T` | Rect tool (矩形工具) |
| `Y` | Transform tool (变换工具) |
| `F` | Frame selected object in Scene view (在场景视图中聚焦选中对象) |
| `Ctrl/Cmd + D` | Duplicate selected (复制选中对象) |
| `Ctrl/Cmd + Z` | Undo (撤销) |
| `Ctrl/Cmd + S` | Save scene (保存场景) |
| `Ctrl/Cmd + Shift + S` | Save scene as (场景另存为) |
| `Ctrl/Cmd + P` | Enter/exit Play Mode (进入/退出播放模式) |
| `Alt + Click` | Expand/collapse all children in Hierarchy (展开/折叠层级中所有子项) |

---

## 3. GameObjects and Components (游戏对象与组件)

A **GameObject** is the fundamental object in Unity scenes. It represents characters, props, scenery, cameras, waypoints, and more. By itself, a GameObject does nothing — its functionality is defined entirely by the **Components** attached to it.

**游戏对象（GameObject）** 是 Unity 场景中的基本对象。它代表角色、道具、场景、摄像机、路径点等。游戏对象本身不执行任何操作——其功能完全由附加到它的**组件（Component）** 定义。

Every GameObject has a **Transform** component that cannot be removed. Transform stores the position, rotation, and scale of the object in world space (and relative to a parent if parented). All other components are optional and addable at any time.

每个游戏对象都有一个无法移除的 **Transform** 组件。Transform 存储对象在世界空间（如果有父对象则相对于父对象）中的位置、旋转和缩放。所有其他组件都是可选的，可以随时添加。

### The Transform Component API (Transform 组件 API)

```csharp
// Get/set world position
transform.position = new Vector3(1f, 2f, 3f);
// Get/set local position (relative to parent)
transform.localPosition = new Vector3(0f, 1f, 0f);
// Rotate over time
transform.Rotate(Vector3.up * 45f * Time.deltaTime);
// Move in local forward direction
transform.Translate(Vector3.forward * speed * Time.deltaTime);
// Look at a target
transform.LookAt(target.transform);
// Parent / Unparent
transform.SetParent(parentTransform);
transform.SetParent(null); // make root
// Get child
Transform child = transform.GetChild(0);
```

```csharp
// 获取/设置世界坐标位置
transform.position = new Vector3(1f, 2f, 3f);
// 获取/设置本地坐标位置（相对于父对象）
transform.localPosition = new Vector3(0f, 1f, 0f);
// 随时间旋转
transform.Rotate(Vector3.up * 45f * Time.deltaTime);
// 沿本地前方方向移动
transform.Translate(Vector3.forward * speed * Time.deltaTime);
// 朝向目标
transform.LookAt(target.transform);
// 设置/解除父级
transform.SetParent(parentTransform);
transform.SetParent(null); // 设为根对象
// 获取子对象
Transform child = transform.GetChild(0);
```

### Prefabs (预制件)

A **Prefab** is a reusable GameObject template stored as an asset. When you make changes to the Prefab asset, those changes propagate to all instances in all scenes. You can also override specific properties on individual instances without affecting the original Prefab.

**预制件（Prefab）** 是存储为资源的可重用游戏对象模板。当您修改预制件资源时，这些更改会传播到所有场景中的所有实例。您也可以在单个实例上覆盖特定属性，而不影响原始预制件。

```csharp
// Instantiate a Prefab at a position and rotation
GameObject instance = Instantiate(prefabReference, spawnPosition, Quaternion.identity);
// Instantiate as a child
GameObject instance2 = Instantiate(prefabReference, parentTransform);
// Destroy after delay
Destroy(instance, 5f);
```

```csharp
// 在指定位置和旋转角度实例化预制件
GameObject instance = Instantiate(prefabReference, spawnPosition, Quaternion.identity);
// 作为子对象实例化
GameObject instance2 = Instantiate(prefabReference, parentTransform);
// 延迟销毁
Destroy(instance, 5f);
```

---

## 4. Programming in Unity / Scripting (Unity 编程与脚本)

Unity uses **C#** as its primary scripting language. Scripts are attached to GameObjects as Components and inherit from `MonoBehaviour` (for runtime behavior).

Unity 使用 **C#** 作为主要脚本语言。脚本作为组件附加到游戏对象上，并继承自 `MonoBehaviour`（用于运行时行为）。

### Event Functions (事件函数)

> 📖 **Official page:** [Event functions](https://docs.unity3d.com/Manual/event-functions.html)

MonoBehaviour provides "magic" methods that Unity calls automatically at defined points in the game loop. The official documentation organises them into five groups: **Regular update events**, **Initialization events**, **GUI events**, **Input events**, and **Physics events**.

MonoBehaviour 提供了 Unity 在游戏循环特定时刻自动调用的"魔法"方法。官方文档将它们分为五组：**常规更新事件**、**初始化事件**、**GUI 事件**、**输入事件**和**物理事件**。

#### Initialization events (初始化事件)

```csharp
// Called once when the script instance is first loaded (before any Start)
void Awake() { }

// Called once on the first frame the script is enabled, after all Awake calls
void Start() { }

// Called when the script or GameObject is enabled (including after Start)
void OnEnable() { }

// Called when the script or GameObject is disabled
void OnDisable() { }

// Called when the object is destroyed
void OnDestroy() { }
```

```csharp
// 脚本实例首次加载时调用一次（在任何 Start 之前）
void Awake() { }

// 脚本启用后第一帧调用一次（在所有 Awake 调用之后）
void Start() { }

// 脚本或游戏对象被启用时调用
void OnEnable() { }

// 脚本或游戏对象被禁用时调用
void OnDisable() { }

// 对象被销毁时调用
void OnDestroy() { }
```

#### Regular update events (常规更新事件)

```csharp
// Called once per frame (frame-rate dependent) — use for gameplay logic
void Update() { }

// Called after all Update() calls — use for camera follow, final transforms
void LateUpdate() { }
```

```csharp
// 每帧调用一次（取决于帧率）——用于游戏逻辑
void Update() { }

// 每帧所有 Update() 之后调用——用于摄像机跟随、最终变换
void LateUpdate() { }
```

#### Physics events (物理事件)

```csharp
// Called at a fixed time step (default 0.02 s = 50 Hz). Use for physics forces.
void FixedUpdate() { }

// Collision events (requires Rigidbody + non-Trigger Collider)
void OnCollisionEnter(Collision col) { }
void OnCollisionStay(Collision col)  { }
void OnCollisionExit(Collision col)  { }

// Trigger events (requires a Trigger Collider on one object)
void OnTriggerEnter(Collider other) { }
void OnTriggerStay(Collider other)  { }
void OnTriggerExit(Collider other)  { }
```

```csharp
// 以固定时间步长调用（默认 0.02 秒 = 50 Hz），用于物理力
void FixedUpdate() { }

// 碰撞事件（需要 Rigidbody + 非触发器碰撞体）
void OnCollisionEnter(Collision col) { }
void OnCollisionStay(Collision col)  { }
void OnCollisionExit(Collision col)  { }

// 触发器事件（需要任一对象上有触发器碰撞体）
void OnTriggerEnter(Collider other) { }
void OnTriggerStay(Collider other)  { }
void OnTriggerExit(Collider other)  { }
```

#### Input events (输入事件)

```csharp
// Mouse events — require a Collider on the GameObject
void OnMouseDown()  { }   // mouse button pressed over collider
void OnMouseUp()    { }   // mouse button released
void OnMouseEnter() { }   // mouse cursor enters collider bounds
void OnMouseExit()  { }   // mouse cursor exits collider bounds
void OnMouseOver()  { }   // mouse is over collider (every frame)
void OnMouseDrag()  { }   // mouse dragged while button down
```

```csharp
// 鼠标事件——需要游戏对象上有碰撞体
void OnMouseDown()  { }   // 鼠标按键在碰撞体上按下
void OnMouseUp()    { }   // 鼠标按键释放
void OnMouseEnter() { }   // 鼠标光标进入碰撞体范围
void OnMouseExit()  { }   // 鼠标光标离开碰撞体范围
void OnMouseOver()  { }   // 鼠标在碰撞体上（每帧）
void OnMouseDrag()  { }   // 按住鼠标按键时拖动
```

#### GUI events (GUI 事件)

```csharp
// Called once per frame for IMGUI rendering (legacy UI only)
void OnGUI() { }

// Editor only: called when Inspector values change
void OnValidate() { }

// Called once — use to draw Gizmos in Scene view always
void OnDrawGizmos() { }

// Called once — draw Gizmos only when the object is selected
void OnDrawGizmosSelected() { }
```

```csharp
// 每帧调用一次用于 IMGUI 渲染（仅旧版 UI）
void OnGUI() { }

// 仅编辑器：检视器值变化时调用
void OnValidate() { }

// 调用一次——始终在场景视图中绘制 Gizmo
void OnDrawGizmos() { }

// 调用一次——仅在对象被选中时绘制 Gizmo
void OnDrawGizmosSelected() { }
```

#### Execution Order of Event Functions (事件函数执行顺序)

The general frame order is: `Awake` → `OnEnable` → `Start` → (per-physics-step: `FixedUpdate`) → `Update` → `LateUpdate` → (rendering). Configure per-script order at **Edit → Project Settings → Script Execution Order**.

一般帧顺序为：`Awake` → `OnEnable` → `Start` → （每个物理步骤：`FixedUpdate`）→ `Update` → `LateUpdate` → （渲染）。在**编辑 → 项目设置 → 脚本执行顺序**中为每个脚本配置顺序。

### Common Unity API Reference (常用 Unity API 参考)

#### Finding GameObjects and Components (查找游戏对象和组件)

```csharp
// Get a component on the same GameObject
Rigidbody rb = GetComponent<Rigidbody>();
// Try get component safely (preferred)
if (TryGetComponent<Animator>(out Animator anim))
    anim.SetBool("isRunning", true);
// Get including children
Renderer r = GetComponentInChildren<Renderer>();
// Find by name (slow — avoid in Update)
GameObject obj = GameObject.Find("PlayerObject");
// Find all objects of a type (slow)
Rigidbody[] all = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
// Find by tag
GameObject player = GameObject.FindWithTag("Player");
GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
```

```csharp
// 获取同一游戏对象上的组件
Rigidbody rb = GetComponent<Rigidbody>();
// 安全地尝试获取组件（推荐）
if (TryGetComponent<Animator>(out Animator anim))
    anim.SetBool("isRunning", true);
// 包括子对象
Renderer r = GetComponentInChildren<Renderer>();
// 按名称查找（慢——避免在 Update 中使用）
GameObject obj = GameObject.Find("PlayerObject");
// 查找所有指定类型（慢）
Rigidbody[] all = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
// 按标签查找
GameObject player = GameObject.FindWithTag("Player");
GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
```

#### Time (时间)

```csharp
float t   = Time.time;              // Seconds since game started (游戏开始以来的秒数)
float dt  = Time.deltaTime;         // Seconds since last frame, use in Update (上一帧经过的秒数)
float fdt = Time.fixedDeltaTime;    // Fixed timestep, use in FixedUpdate (固定时间步长)
int frame = Time.frameCount;        // Current frame number (当前帧编号)
Time.timeScale = 0.5f;              // 0=paused, 1=normal, 2=double speed (0=暂停,1=正常,2=双倍)
float rt = Time.realtimeSinceStartup; // Unaffected by timeScale (不受 timeScale 影响)
```

#### Coroutines (协程)

```csharp
StartCoroutine(MyCoroutine());
StopAllCoroutines();

IEnumerator MyCoroutine()
{
    yield return new WaitForSeconds(2f);       // Wait 2 real seconds (等待 2 秒)
    yield return null;                          // Wait one frame (等待一帧)
    yield return new WaitForFixedUpdate();      // Wait for next FixedUpdate (等待下次 FixedUpdate)
    yield return new WaitForEndOfFrame();       // Wait until end of frame (等待帧末)
    yield return new WaitUntil(() => ready);    // Wait until condition true (等到条件为真)
    yield return new WaitWhile(() => loading);  // Wait while condition true (条件为真时等待)
    yield return StartCoroutine(Other());       // Wait for nested coroutine (等待嵌套协程)
}
```

#### SceneManagement (场景管理)

```csharp
using UnityEngine.SceneManagement;

SceneManager.LoadScene("MainMenu");                         // Destroy current, load new (销毁当前，加载新场景)
SceneManager.LoadScene("Overlay", LoadSceneMode.Additive); // Keep current (保留当前场景叠加加载)
AsyncOperation op = SceneManager.LoadSceneAsync("GameLevel"); // Async load (异步加载)
SceneManager.UnloadSceneAsync("Overlay");                   // Unload additive scene (卸载叠加场景)
DontDestroyOnLoad(gameObject);                              // Persist across scenes (跨场景持久化)
string name = SceneManager.GetActiveScene().name;           // Get active scene name (获取当前场景名)
```

#### PlayerPrefs (玩家偏好存储)

```csharp
PlayerPrefs.SetInt("Score", 100);     PlayerPrefs.GetInt("Score", 0);
PlayerPrefs.SetFloat("Vol", 0.8f);    PlayerPrefs.GetFloat("Vol", 1f);
PlayerPrefs.SetString("Name", "Ali"); PlayerPrefs.GetString("Name", "?");
PlayerPrefs.Save();        // Flush to disk (写入磁盘)
PlayerPrefs.DeleteKey("Score");
PlayerPrefs.DeleteAll();
```

#### Legacy Input (旧版输入)

```csharp
if (Input.GetKeyDown(KeyCode.Space)) { }        // Pressed this frame (本帧按下)
if (Input.GetKey(KeyCode.Space))     { }        // Held this frame (本帧按住)
if (Input.GetKeyUp(KeyCode.Space))   { }        // Released this frame (本帧松开)
float h = Input.GetAxis("Horizontal");           // -1..1 smooth (平滑 -1..1)
float v = Input.GetAxisRaw("Vertical");          // -1, 0, 1 raw (原始 -1,0,1)
Vector3 mp = Input.mousePosition;               // Screen pixels (屏幕像素)
if (Input.GetMouseButtonDown(0)) { }            // 0=left,1=right,2=middle (0=左,1=右,2=中)
```

---

## 5. Physics (物理系统)

Unity integrates **NVIDIA PhysX** for 3D physics and **Box2D** for 2D physics. Simulation runs at a fixed time step (`Time.fixedDeltaTime`, default 0.02 s) inside `FixedUpdate()`.

Unity 集成了用于 3D 物理的 **NVIDIA PhysX** 和用于 2D 物理的 **Box2D**。模拟以固定时间步长（`Time.fixedDeltaTime`，默认 0.02 秒）在 `FixedUpdate()` 中运行。

### Introduction to Rigid Body Physics (刚体物理简介)

> ✅ **v2 correction (v2 修正):** Official page title is *"Introduction to rigid body physics"* — corrected from "Rigidbody Component" in v1.
> 📖 **Official page:** [RigidbodiesOverview](https://docs.unity3d.com/Manual/RigidbodiesOverview.html)

#### Rigid body GameObjects with physics-based movement (具有基于物理运动的刚体游戏对象)

When a Rigidbody component is added to a GameObject and its **Is Kinematic** property is **disabled**, the physics engine takes full control of the object's movement. Forces, gravity, and collisions all drive it. This is the normal "dynamic" rigid body.

当为游戏对象添加 Rigidbody 组件且其 **Is Kinematic** 属性**禁用**时，物理引擎完全控制对象的运动。力、重力和碰撞都会驱动它。这是正常的"动态"刚体。

#### Rigid body GameObjects without physics-based movement (不具有基于物理运动的刚体游戏对象)

When **Is Kinematic** is **enabled**, the physics engine does not drive the object. Instead, only scripts can move it (via `rb.MovePosition` / `rb.MoveRotation`). Other physics objects still collide with it. Kinematic bodies are ideal for doors, platforms, and animation-driven characters that must affect physics but should not be pushed by it.

当 **Is Kinematic** **启用**时，物理引擎不驱动该对象。相反，只有脚本可以移动它（通过 `rb.MovePosition` / `rb.MoveRotation`）。其他物理对象仍然与之碰撞。运动学刚体适用于门、平台和动画驱动角色——它们必须影响物理，但不应被物理推动。

#### Rigidbody optimization (刚体优化)

Physics has a **sleep** mechanism: when a Rigidbody's velocity falls below the **Sleep Threshold** (set in **Edit → Project Settings → Physics**), it goes to sleep and stops being simulated, saving CPU. It wakes automatically when another collider contacts it or when `rb.WakeUp()` is called.

物理系统有一个**睡眠**机制：当刚体的速度低于**睡眠阈值**（在**编辑 → 项目设置 → 物理**中设置）时，它会进入睡眠状态并停止模拟，从而节省 CPU。当另一个碰撞体与其接触或调用 `rb.WakeUp()` 时，它会自动唤醒。

**Rigidbody Inspector Properties (检视器属性):**

| Property (属性) | Default (默认值) | Description (描述) |
|---|---|---|
| Mass | 1 | Mass in kg. Affects collision response. (质量，kg，影响碰撞响应) |
| Drag | 0 | Linear drag — slows movement. (线性阻力) |
| Angular Drag | 0.05 | Angular drag — slows rotation. (角阻力) |
| Use Gravity | true | Whether gravity affects this body. (是否受重力影响) |
| Is Kinematic | false | Script-only control; no physics drive. (仅脚本控制，无物理驱动) |
| Interpolate | None | None / Interpolate / Extrapolate for smooth rendering. (渲染平滑模式) |
| Collision Detection | Discrete | Discrete / Continuous / Continuous Dynamic / Continuous Speculative. (碰撞检测模式) |
| Constraints | None | Freeze position/rotation on specific axes. (冻结轴) |

**Rigidbody C# API:**

```csharp
Rigidbody rb = GetComponent<Rigidbody>();

// Apply force
rb.AddForce(Vector3.up * 500f);                           // Continuous force (持续力)
rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);         // Instant impulse, good for jump (瞬时冲量，适合跳跃)
rb.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);  // Instant, ignores mass (忽略质量的瞬时变化)
rb.AddTorque(Vector3.up * 5f);                            // Rotational force (扭矩)

// Set velocity directly
rb.linearVelocity = new Vector3(5f, 0f, 0f);
rb.angularVelocity = Vector3.zero;

// Kinematic movement (use in FixedUpdate)
rb.MovePosition(rb.position + Vector3.forward * speed * Time.fixedDeltaTime);
rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, 45f * Time.fixedDeltaTime, 0f));

// Sleep
rb.WakeUp();
bool sleeping = rb.IsSleeping();
```

```csharp
Rigidbody rb = GetComponent<Rigidbody>();

// 施加力
rb.AddForce(Vector3.up * 500f);                           // 持续力
rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);         // 瞬时冲量，适合跳跃
rb.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);  // 忽略质量的瞬时变化
rb.AddTorque(Vector3.up * 5f);                            // 扭矩

// 直接设置速度
rb.linearVelocity = new Vector3(5f, 0f, 0f);
rb.angularVelocity = Vector3.zero;

// 运动学移动（在 FixedUpdate 中使用）
rb.MovePosition(rb.position + Vector3.forward * speed * Time.fixedDeltaTime);
rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, 45f * Time.fixedDeltaTime, 0f));

// 睡眠控制
rb.WakeUp();
bool sleeping = rb.IsSleeping();
```

### Introduction to Collision (碰撞简介)

> ✅ **v2 correction (v2 修正):** Added "Collider surfaces" sub-section (omitted in v1). Official page: [CollidersOverview](https://docs.unity3d.com/Manual/CollidersOverview.html)

#### Collider types (碰撞体类型)

| Collider (碰撞体) | Use Case (使用场景) | Notes (说明) |
|---|---|---|
| Box Collider | Crates, walls, floors (箱子、墙壁、地板) | Cheapest. Center + Size. (最便宜) |
| Sphere Collider | Balls, pickups (球、拾取物) | Very fast broadphase. Center + Radius. |
| Capsule Collider | Characters, columns (角色、柱子) | Center + Radius + Height + Direction. |
| Mesh Collider | Complex geometry (复杂几何体) | Expensive. Convex=true for dynamic objects. |
| Wheel Collider | Vehicle wheels (车轮) | Specialized vehicle physics. |
| Terrain Collider | Unity Terrain (Unity 地形) | Auto-matches heightmap. |

##### Trigger colliders (触发器碰撞体)

Setting **Is Trigger = true** on a Collider makes it a trigger. It detects overlaps but produces no physical collision response. Use for pickup zones, damage areas, cutscene triggers. The relevant callbacks are `OnTriggerEnter`, `OnTriggerStay`, and `OnTriggerExit`.

在碰撞体上设置 **Is Trigger = true** 使其成为触发器。它检测重叠但不产生物理碰撞响应。适用于拾取区域、伤害区域、过场触发。相关回调为 `OnTriggerEnter`、`OnTriggerStay` 和 `OnTriggerExit`。

#### Collider shapes (碰撞体形状)

Unity 3D offers **Primitive** shapes (Box, Sphere, Capsule — fastest), **Mesh** (exact match but expensive), and **Compound** (multiple primitive colliders on child GameObjects to approximate a complex shape cheaply).

Unity 3D 提供**基元**形状（Box、Sphere、Capsule——最快）、**网格**（精确匹配但昂贵）和**复合**（子游戏对象上的多个基元碰撞体，以低成本近似复杂形状）。

#### Collider surfaces — PhysicsMaterial (碰撞体表面——物理材质)

> ✅ **v2 addition (v2 新增):** This sub-section was missing from v1.

A **PhysicsMaterial** (create via **Assets → Create → PhysicsMaterial**) controls how a surface behaves on contact.

**物理材质（PhysicsMaterial）**（通过**资源 → 创建 → 物理材质**创建）控制表面接触时的行为。

| Property (属性) | Range (范围) | Description (描述) |
|---|---|---|
| Dynamic Friction | 0–1 | Friction while sliding. 0 = ice, 1 = rubber. (滑动时的摩擦力，0=冰，1=橡胶) |
| Static Friction | 0–1 | Friction while stationary. (静止时的摩擦力) |
| Bounciness | 0–1 | How bouncy the surface is. 0 = no bounce, 1 = fully elastic. (弹性，0=不弹，1=完全弹性) |
| Friction Combine | Average / Min / Max / Multiply | How to combine two surfaces' friction. (两表面摩擦的合并方式) |
| Bounce Combine | Average / Min / Max / Multiply | How to combine bounciness. (弹性合并方式) |

Assign the PhysicsMaterial to a Collider's **Material** slot in the Inspector.

在检视器中将 PhysicsMaterial 分配给碰撞体的 **Material** 插槽。

### Raycasting (射线检测)

```csharp
// Raycast from camera through mouse
Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
if (Physics.Raycast(ray, out RaycastHit hit, 100f))
{
    Debug.Log("Hit: " + hit.collider.name + " at " + hit.point);
}

// Layer mask
int mask = LayerMask.GetMask("Ground", "Obstacle");
Physics.Raycast(origin, direction, out hit, 100f, mask);

// All hits sorted by distance
RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

// SphereCast — thick ray
Physics.SphereCast(origin, 0.5f, direction, out hit, 10f);

// OverlapSphere — all colliders in a radius
Collider[] cols = Physics.OverlapSphere(center, 3f);

// Boolean check
bool occupied = Physics.CheckSphere(center, 0.3f);
```

```csharp
// 从摄像机通过鼠标投射射线
Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
if (Physics.Raycast(ray, out RaycastHit hit, 100f))
{
    Debug.Log("命中：" + hit.collider.name + " 在 " + hit.point);
}

// 层掩码
int mask = LayerMask.GetMask("Ground", "Obstacle");
Physics.Raycast(origin, direction, out hit, 100f, mask);

// 所有命中按距离排序
RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

// SphereCast——带半径的射线
Physics.SphereCast(origin, 0.5f, direction, out hit, 10f);

// OverlapSphere——半径内所有碰撞体
Collider[] cols = Physics.OverlapSphere(center, 3f);

// 布尔检查
bool occupied = Physics.CheckSphere(center, 0.3f);
```

### Physics Global Settings (全局物理设置)

Configure via **Edit → Project Settings → Physics**:

通过**编辑 → 项目设置 → 物理**配置：

| Setting (设置) | Default (默认) | Description (描述) |
|---|---|---|
| Gravity | (0, –9.81, 0) | World gravity vector (m/s²). (重力向量) |
| Fixed Timestep | 0.02 | Physics simulation step (s). Smaller = more accurate. (物理步长，越小越精确) |
| Max Allowed Timestep | 0.1 | Max simulated time per frame. (每帧最大模拟时间) |
| Sleep Threshold | 0.005 | Energy level below which objects sleep. (睡眠能量阈值) |
| Bounce Threshold | 2 | Min relative velocity to produce bounce. (产生弹跳的最小相对速度) |
| Layer Collision Matrix | — | Per-layer collision enable/disable grid. (按层碰撞开关矩阵) |

---

## 6. Animation (动画系统)

### Animation State Machine (动画状态机)

> 📖 **Official page:** [AnimationStateMachines](https://docs.unity3d.com/Manual/AnimationStateMachines.html)

It's common for a character or a GameObject to have several animations for the different actions it performs. Mecanim uses a **state machine** — a graph of nodes and connecting lines that resembles a flowchart — to arrange these actions. A state machine plays the animation linked to the current action and determines the next action.

一个角色或游戏对象通常针对不同动作有多个动画。Mecanim 使用**状态机**——一个由节点和连接线组成的类似流程图的图形——来组织这些动作。状态机播放与当前动作关联的动画，并决定下一个动作。

**Official sub-topics (官方子主题):**

> ✅ **v2 addition (v2 新增):** Sub-topic structure added per official page.

| Topic (主题) | Description (描述) |
|---|---|
| State machine basics | Core state machine concepts and Animator window overview. (核心概念与 Animator 窗口概述) |
| Animation states | Configure states, motions, and defaults. (配置状态、动作和默认值) |
| Animation parameters | Control state logic with scriptable parameters. (用可编脚本参数控制状态逻辑) |
| State machine transitions | Entry and Exit transitions between state machines. (状态机之间的进入和退出过渡) |
| Animation transitions | Blend between states and define trigger conditions. (在状态间混合并定义触发条件) |
| Animation blend trees | Blend similar motions smoothly using parameters. (使用参数平滑混合相似动作) |
| State machine behaviors | Attach scripts to states — run code on enter/update/exit. (为状态附加脚本) |
| Sub-state machines | Group related states into nested machines. (将相关状态分组为嵌套机器) |
| Animation layers | Separate animation with layered controllers and masks. (用分层控制器和遮罩分离动画) |
| State machine solo and mute | Preview transitions faster by soloing/muting paths. (通过独奏/静音加速预览过渡) |
| Target matching | Match character parts to precise world targets during animation. (在动画期间将角色部位匹配到精确世界目标) |

**Animator C# API:**

```csharp
Animator anim = GetComponent<Animator>();

// Parameters
anim.SetBool("isGrounded", true);
anim.SetFloat("speed", 3.5f);
anim.SetInteger("weaponIndex", 2);
anim.SetTrigger("jump");        // fires once, auto-resets (触发一次后自动重置)
anim.ResetTrigger("jump");

// Crossfade between states
anim.CrossFade("Run", 0.2f);
int hash = Animator.StringToHash("Attack");
anim.CrossFade(hash, 0.1f);

// State info
AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
bool isIdle = info.IsName("Idle");
float progress = info.normalizedTime; // 0..1 through the clip (0..1 剪辑进度)

// Speed
anim.speed = 1.5f;

// IK (requires "IK Pass" enabled on the layer)
anim.SetIKPosition(AvatarIKGoal.RightHand, target.position);
anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);

// Layer weight
anim.SetLayerWeight(1, 0.8f);
```

```csharp
Animator anim = GetComponent<Animator>();

// 参数
anim.SetBool("isGrounded", true);
anim.SetFloat("speed", 3.5f);
anim.SetInteger("weaponIndex", 2);
anim.SetTrigger("jump");        // 触发一次后自动重置
anim.ResetTrigger("jump");

// 交叉淡入状态
anim.CrossFade("Run", 0.2f);
int hash = Animator.StringToHash("Attack");
anim.CrossFade(hash, 0.1f);

// 状态信息
AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
bool isIdle = info.IsName("Idle");
float progress = info.normalizedTime; // 0..1 剪辑进度

// 速度
anim.speed = 1.5f;

// IK（需要在层上启用 "IK Pass"）
anim.SetIKPosition(AvatarIKGoal.RightHand, target.position);
anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);

// 层权重
anim.SetLayerWeight(1, 0.8f);
```

### Animation Parameters — Official Code Example (动画参数——官方代码示例)

> ✅ **v2 addition (v2 新增):** Official code example added from [AnimationParameters](https://docs.unity3d.com/Manual/AnimationParameters.html).

Animation Parameters are variables defined within an Animator Controller that can be accessed and assigned values from scripts. This is how a script controls or affects the flow of the state machine.

动画参数是在动画控制器中定义的变量，可以从脚本中访问和赋值。这是脚本控制或影响状态机流程的方式。

There are four parameter types: **Integer**, **Float**, **Bool**, and **Trigger** (a boolean that auto-resets after a transition consumes it).

有四种参数类型：**整数（Integer）**、**浮点数（Float）**、**布尔值（Bool）** 和 **触发器（Trigger）**（一种在过渡消耗后自动重置的布尔值）。

```csharp
// Official example from Unity Manual — AnimationParameters.html
using UnityEngine;
using System.Collections;

public class SimplePlayer : MonoBehaviour
{
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float h    = Input.GetAxis("Horizontal");
        float v    = Input.GetAxis("Vertical");
        bool  fire = Input.GetButtonDown("Fire1");

        animator.SetFloat("Forward", v);
        animator.SetFloat("Strafe",  h);
        animator.SetBool("Fire",    fire);
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            animator.SetTrigger("Die");
        }
    }
}
```

```csharp
// Unity 官方手册 AnimationParameters.html 的示例代码
using UnityEngine;
using System.Collections;

public class SimplePlayer : MonoBehaviour
{
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float h    = Input.GetAxis("Horizontal");
        float v    = Input.GetAxis("Vertical");
        bool  fire = Input.GetButtonDown("Fire1");

        animator.SetFloat("Forward", v);    // 前进方向
        animator.SetFloat("Strafe",  h);    // 横移方向
        animator.SetBool("Fire",    fire);  // 开火
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            animator.SetTrigger("Die");    // 死亡触发器
        }
    }
}
```

### Animation Blend Trees (动画混合树)

> 📖 **Official page:** [animation-blend-trees](https://docs.unity3d.com/Manual/animation-blend-trees.html)
> ✅ **v2 addition (v2 新增):** Official sub-type breakdown added.

Use blend trees to blend between two or more similar motions, such as between walking and running animations. The Animator window contains a visual blend tree which you can use to smoothly blend multiple animations together.

使用混合树在两个或多个相似动作之间混合，例如行走和奔跑动画之间。Animator 窗口包含一个可视化混合树，可用于平滑地混合多个动画。

| Blend Tree Type (混合树类型) | Description (描述) |
|---|---|
| **1D Blending** | Blend child motions according to a single float parameter (e.g., `speed` 0→1 = idle→run). (根据单个浮点参数混合，例如 speed 0→1 = 待机→奔跑) |
| **2D Simple Directional** | Blend according to two parameters; each direction has at most one motion. (根据两个参数混合，每个方向最多一个动作) |
| **2D Freeform Directional** | Two parameters; allows multiple motions in the same direction. (两个参数，允许同一方向有多个动作) |
| **2D Freeform Cartesian** | Two parameters not representing direction (e.g., forward speed + turn speed). (两个参数不代表方向，例如前进速度+转弯速度) |
| **Direct blending** | Map Animator parameters directly to the weight of each child motion for exact blending control. (将动画参数直接映射到每个子动作的权重，实现精确混合控制) |

---

## 7. Audio (音频系统)

### Core Components (核心组件)

**AudioListener**: one per scene (usually on Main Camera). Acts as the "ears" receiving spatial sound.

**AudioSource**: plays audio clips. Configures 2D or 3D spatial audio.

**AudioClip**: the audio data asset. Supports MP3, OGG, WAV, AIFF, FLAC.

---

**AudioListener（音频监听器）**：每个场景一个（通常在主摄像机上），充当接收空间声音的"耳朵"。

**AudioSource（音频源）**：播放音频剪辑，配置 2D 或 3D 空间音频。

**AudioClip（音频剪辑）**：音频数据资源，支持 MP3、OGG、WAV、AIFF、FLAC。

### AudioSource Inspector Properties (AudioSource 检视器属性)

| Property (属性) | Description (描述) |
|---|---|
| AudioClip | The clip to play (要播放的剪辑) |
| Output | Route to an AudioMixerGroup (路由到音频混音器组) |
| Mute | Silence without stopping (静音不停止) |
| Play On Awake | Auto-play when enabled (启用时自动播放) |
| Loop | Loop the clip (循环播放) |
| Priority | 0=highest, 256=lowest (0=最高优先级，256=最低) |
| Volume | 0–1 (音量) |
| Pitch | 1=normal, >1=faster+higher, <1=slower+lower (1=正常，>1=更快更高，<1=更慢更低) |
| Stereo Pan | –1=left, 0=center, +1=right (仅 2D：-1=左，0=中，+1=右) |
| Spatial Blend | 0=pure 2D, 1=full 3D (0=纯 2D，1=完全 3D) |
| Min Distance | Distance before attenuation starts (开始衰减的距离) |
| Max Distance | Distance at which sound becomes inaudible (声音变得无法听见的距离) |
| Volume Rolloff | Logarithmic / Linear / Custom (对数/线性/自定义衰减) |

**AudioSource C# API:**

```csharp
AudioSource src = GetComponent<AudioSource>();

src.Play();                            // Play assigned clip (播放指定剪辑)
src.Stop();
src.Pause();
src.UnPause();

src.PlayOneShot(clip);                 // Overlap-safe, not interrupted (可叠加播放)
src.PlayOneShot(clip, 0.5f);          // With volume scale (带音量缩放)

AudioSource.PlayClipAtPoint(clip, transform.position); // Temp source at position (在位置处创建临时源)
src.PlayDelayed(0.5f);                // Play after delay (延迟播放)

// Precise timing with DSP clock
double dspTime = AudioSettings.dspTime;
src.PlayScheduled(dspTime + 2.0);

bool playing = src.isPlaying;

// Volume fade coroutine
IEnumerator FadeOut(AudioSource s, float dur)
{
    float start = s.volume;
    while (s.volume > 0) { s.volume -= start * Time.deltaTime / dur; yield return null; }
    s.Stop(); s.volume = start;
}
```

```csharp
AudioSource src = GetComponent<AudioSource>();

src.Play();                            // 播放指定剪辑
src.Stop();
src.Pause();
src.UnPause();

src.PlayOneShot(clip);                 // 可叠加播放，不被中断
src.PlayOneShot(clip, 0.5f);          // 带音量缩放

AudioSource.PlayClipAtPoint(clip, transform.position); // 在位置处创建临时音频源
src.PlayDelayed(0.5f);                // 延迟播放

// 使用 DSP 时钟精确计时
double dspTime = AudioSettings.dspTime;
src.PlayScheduled(dspTime + 2.0);

bool playing = src.isPlaying;

// 音量淡出协程
IEnumerator FadeOut(AudioSource s, float dur)
{
    float start = s.volume;
    while (s.volume > 0) { s.volume -= start * Time.deltaTime / dur; yield return null; }
    s.Stop(); s.volume = start;
}
```

### Audio Mixer (音频混音器)

```csharp
using UnityEngine.Audio;

public AudioMixer mainMixer;

// Convert 0..1 linear to dB for mixer parameters
float db = Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
mainMixer.SetFloat("MasterVolume", db);
mainMixer.GetFloat("MasterVolume", out float currentDB);

// Snapshot transition
mainMixer.FindSnapshot("Underwater").TransitionTo(1.5f);
```

---

## 8. Lighting (光照系统)

![A scene with photo-realistic lighting.](https://docs.unity3d.com/uploads/Main/ProgressiveLightmapper-0.jpg)
*Official Unity docs image — Progressive Lightmapper example scene (官方 Unity 文档图片 — 渐进式光照贴图烘焙器示例场景)*

> 📖 **Official page:** [LightingOverview](https://docs.unity3d.com/Manual/LightingOverview.html)

With Unity, you can achieve realistic lighting that is suitable for a range of art styles. Lighting in Unity encompasses a broad set of tools and settings. Choosing the right light sources, rendering methods, and shadow techniques directly impacts both visual quality and performance.

借助 Unity，您可以实现适合各种美术风格的真实感光照效果。Unity 中的光照涵盖了一整套广泛的工具和设置。选择正确的光源、渲染方法和阴影技术将直接影响视觉质量和性能表现。

### Light Types (光源类型)

| Light Type (光源类型) | Description (描述) | Key Properties (关键属性) |
|---|---|---|
| **Directional Light** | Simulates sunlight. Affects all objects equally; only rotation matters. (模拟日光，仅旋转影响效果) | Color, Intensity, Shadow Type, Shadow Strength |
| **Point Light** | Emits in all directions from a point. Attenuates with distance. (从单点向所有方向发光，随距离衰减) | Range, Color, Intensity |
| **Spot Light** | Emits a cone of light. (发出锥形光) | Range, Spot Angle, Inner Spot Angle, Color, Intensity |
| **Area Light** | Rectangular or disc source — baked only. (矩形或圆盘，仅烘焙) | Width, Height, Color, Intensity |

**Light Component Properties (Light 组件属性):**

```
Type:              Directional / Point / Spot / Area
Color:             RGB light color
Mode:              Realtime | Mixed | Baked
Intensity:         Brightness multiplier
Range:             (Point/Spot) Max reach distance
Spot Angle:        (Spot) Outer cone angle in degrees
Inner Spot Angle:  (Spot) Full-intensity inner cone
Shadows → Type:    None / Hard Shadows / Soft Shadows
Shadows → Strength: 0..1 — shadow darkness
Shadows → Bias:    Offset to prevent shadow acne
Culling Mask:      Which layers this light affects
Cookie:            Texture mask projected by the light
```

```
类型：             方向光 / 点光源 / 聚光灯 / 面积光
颜色：             RGB 光颜色
模式：             实时 | 混合 | 烘焙
强度：             亮度倍数
范围：             （点光源/聚光灯）最大到达距离
聚光角度：         （聚光灯）外锥角度
内聚光角度：       （聚光灯）全强度内锥
阴影 → 类型：      无 / 硬阴影 / 软阴影
阴影 → 强度：      0..1 — 阴影暗度
阴影 → 偏移：      防止阴影痤疮的偏移
剔除遮罩：         此光源影响哪些层
Cookie：           光源投影的纹理遮罩
```

### Light Modes (光照模式)

> 📖 **Official page:** [LightModes](https://docs.unity3d.com/Manual/LightModes.html)

- **Realtime (实时)**: Calculated every frame. Supports dynamic objects. Higher cost. (每帧计算，支持动态对象，成本较高)
- **Baked (烘焙)**: Pre-calculated and stored in lightmaps. Cannot affect moving objects at runtime. Zero runtime cost. (预计算并存储在光照贴图中，不能影响运行时移动的对象，运行时零成本)
- **Mixed (混合)**: Combines baked indirect light with real-time direct light and shadows. (结合烘焙间接光与实时直接光和阴影)

### Global Illumination (全局光照)

Open **Window → Rendering → Lighting** to configure GI. Click **Generate Lighting** to bake. Mark objects as **Static** in the Inspector to include them in baking. Lightmapper options: **Progressive CPU** or **Progressive GPU** (faster on supported hardware).

打开**窗口 → 渲染 → 光照**来配置 GI。点击**生成光照**进行烘焙。在检视器中将对象标记为 **Static** 以将其包含在烘焙中。光照贴图烘焙器选项：**渐进式 CPU** 或**渐进式 GPU**（在支持的硬件上更快）。

### Shadows (阴影)

> 📖 **Official page:** [Shadows](https://docs.unity3d.com/Manual/Shadows.html) *(corrected from ShadowOverview.html in v1)*

Shadow Distance controls how far from the camera shadows are rendered (**Edit → Project Settings → Quality → Shadow Distance**). Cascaded Shadow Maps (CSM) improve shadow quality by using higher-resolution shadow maps close to the camera and lower-resolution ones farther away — configure **Shadow Cascades** (2 or 4) in Quality Settings.

阴影距离控制从摄像机到渲染阴影的距离（**编辑 → 项目设置 → 质量 → 阴影距离**）。级联阴影贴图（CSM）通过在摄像机附近使用较高分辨率的阴影贴图、在较远处使用较低分辨率的贴图来改善阴影质量——在质量设置中配置**阴影级联数**（2 或 4）。

### Reflection Probes (反射探针)

A Reflection Probe captures a 360° view as a Cubemap to provide reflections on nearby objects. Key settings:

反射探针捕获 360° 视图作为立方体贴图，为附近对象提供反射。关键设置：

| Setting (设置) | Description (描述) |
|---|---|
| Type | Baked / Custom / Realtime (烘焙/自定义/实时) |
| Resolution | 64 / 128 / 256 / 512 / 1024 / 2048 px |
| HDR | Store as HDR for better accuracy (以 HDR 存储以提高精度) |
| Box Size | The capture volume (捕获体积) |
| Importance | Priority when probes overlap (探针重叠时的优先级) |

---

## 9. Render Pipelines (渲染管线)

![HDRP scene template](https://docs.unity3d.com/uploads/Main/hdrp-scene-template.png)
*Official Unity docs image — HDRP scene template (官方 Unity 文档图片 — HDRP 场景模板)*

> 📖 **Official page:** [render-pipelines](https://docs.unity3d.com/Manual/render-pipelines.html)

A render pipeline performs a series of operations that take the contents of a scene and display them on screen.

渲染管线执行一系列操作，获取场景内容并将其显示在屏幕上。

### Choosing a Render Pipeline (选择渲染管线)

> ✅ **v2 note (v2 说明):** `urp-introduction.html` and `hdrp-introduction.html` return 404 in Unity 6.4. Use the package-level documentation instead:
> - URP: `com.unity.render-pipelines.universal` package docs
> - HDRP: `com.unity.render-pipelines.high-definition` package docs

| Criteria (标准) | URP | HDRP | Built-in |
|---|---|---|---|
| Target Platforms | Mobile, Console, PC, Web | High-end PC, Console | All (legacy) |
| Rendering paths | Forward / Deferred | Full Deferred + Forward | Forward / Deferred |
| Ray Tracing | Limited | Full DXR / Vulkan RT | No |
| 2D Lighting | Full support | No | Partial |
| VR / XR | Excellent | Good | Good |
| Performance | Mid-range optimized | High-end only | General purpose |

### Universal Render Pipeline — URP (通用渲染管线)

**Setup (设置):**

1. **Window → Package Manager → Universal RP** → Install.
2. **Assets → Create → Rendering → URP Asset (with Universal Renderer)**.
3. **Edit → Project Settings → Graphics → Scriptable Render Pipeline Settings** → assign the URP Asset.

---

1. **窗口 → 包管理器 → Universal RP** → 安装。
2. **资源 → 创建 → 渲染 → URP 资源（带通用渲染器）**。
3. **编辑 → 项目设置 → 图形 → 可编脚本渲染管线设置** → 分配 URP 资源。

**URP Asset Key Settings (URP 资源关键设置):**

| Setting (设置) | Options / Range (选项/范围) | Description (描述) |
|---|---|---|
| Rendering Path | Forward / Deferred | Main rendering algorithm. (主渲染算法) |
| HDR | On/Off | High Dynamic Range. (高动态范围) |
| Anti Aliasing (MSAA) | None / 2x / 4x / 8x | Multisample anti-aliasing. (多重采样抗锯齿) |
| Render Scale | 0.1–2.0 | Resolution multiplier (<1 = upscaling). (分辨率倍数，<1=升级采样) |
| Main Light | Per Vertex / Per Pixel / Disabled | Main directional light quality. (主平行光质量) |
| Additional Lights | 0–8 | Per-object additional lights. (每对象额外光源) |
| Shadow Distance | 0–∞ | Max shadow rendering distance. (阴影最大渲染距离) |
| Shadow Cascade Count | 1–4 | Number of CSM cascades. (CSM 级联数) |

**Renderer Features (渲染器功能):** Add via the Universal Renderer asset to extend the pipeline: Screen Space Ambient Occlusion (SSAO), Decals, Full Screen Pass Renderer Feature, Screen Space Shadows.

**渲染器功能：** 通过通用渲染器资源添加以扩展管线：屏幕空间环境光遮蔽（SSAO）、贴花、全屏通道渲染器功能、屏幕空间阴影。

### High Definition Render Pipeline — HDRP (高清渲染管线)

HDRP targets high-end platforms (PC, PS5, Xbox Series X/S) and uses physically-based rendering (PBR) by default.

HDRP 面向高端平台（PC、PS5、Xbox Series X/S），默认使用基于物理的渲染（PBR）。

**HDRP Key Features (HDRP 关键功能):**

| Feature (功能) | Description (描述) |
|---|---|
| Volumetric Lighting & Fog | Physically accurate atmospheric scattering. (物理精确的大气散射) |
| Screen Space Reflections (SSR) | Real-time reflections using the screen buffer. (使用屏幕缓冲区的实时反射) |
| Ray Tracing (DXR/Vulkan RT) | Hardware-accelerated RT shadows, reflections, GI. (硬件加速的 RT 阴影、反射、全局光照) |
| Temporal Anti-Aliasing (TAA) | Uses previous frames to reduce aliasing. (使用前几帧减少锯齿) |
| Depth of Field | Physically-based bokeh blur. (基于物理的散景模糊) |
| Bloom & Lens Flare | Physically-based post-processing. (基于物理的后处理) |
| Decals | Project textures onto surfaces in world space. (在世界空间中将纹理投影到表面) |
| Adaptive Probe Volumes (APV) | Automatic, scalable probe-based GI. (自动、可扩展的基于探针的全局光照) |

---

## 10. 2D Game Development (2D 游戏开发)

![2D game development example](https://docs.unity3d.com/uploads/Main/2dGames.jpg)
*Official Unity docs image — 2D game development (官方 Unity 文档图片 — 2D 游戏开发)*

> 📖 **Official page:** [Unity2D](https://docs.unity3d.com/Manual/Unity2D.html)

You can use the Unity Editor to create projects in 3D and 2D. This section focuses on 2D-specific features including gameplay, sprites, tilemaps, and 2D physics.

您可以使用 Unity 编辑器创建 3D 和 2D 项目。本节重点介绍 2D 特有功能，包括游戏玩法、精灵图、瓦片地图和 2D 物理。

### Sprites (精灵图)

> 📖 **Official page:** [Sprites](https://docs.unity3d.com/Manual/Sprites.html)

A **Sprite** is a 2D graphic used in 2D games. Import an image (PNG, JPG, etc.) and set **Texture Type → Sprite (2D and UI)** in the Inspector.

**精灵图（Sprite）** 是 2D 游戏中的 2D 图形。导入图像（PNG、JPG 等）并在检视器中设置**纹理类型 → 精灵图（2D 和 UI）**。

**Sprite Import Settings (精灵图导入设置):**

| Setting (设置) | Description (描述) |
|---|---|
| Sprite Mode | Single / Multiple (for sprite sheets) / Polygon (单个/多个（精灵表）/多边形) |
| Pixels Per Unit | Pixels = 1 Unity unit. Default 100. (每单位像素数，默认 100) |
| Pivot | Default anchor: Center / Top / Bottom / etc. (默认锚点) |
| Filter Mode | Point (pixel art) / Bilinear / Trilinear (点（像素艺术）/双线性/三线性) |
| Compression | None / Low / Normal / High quality (无/低/普通/高质量) |

**Sprite Renderer Component (Sprite Renderer 组件):**

| Property (属性) | Description (描述) |
|---|---|
| Sprite | The sprite asset to display (要显示的精灵图资源) |
| Color | Tint (RGBA) (着色 RGBA) |
| Flip X / Flip Y | Mirror horizontally or vertically (水平或垂直镜像) |
| Draw Mode | Simple / Sliced / Tiled (简单/切片/平铺) |
| Sorting Layer | Draw-order layer (绘制顺序层) |
| Order in Layer | Draw order within the layer (层内绘制顺序) |

### Tilemaps (瓦片地图)

**Workflow (工作流程):**

1. **GameObject → 2D Object → Tilemap → Rectangular** — create the Tilemap.
2. **Window → 2D → Tile Palette** — open the Tile Palette window.
3. Drag sprites into the Tile Palette to create Tile assets.
4. Use the Paint brush to paint tiles in the Scene view.
5. Add **Tilemap Collider 2D** for collision.
6. Add **Composite Collider 2D** + **Rigidbody2D (Kinematic)** to merge tile shapes for better performance.

---

1. **游戏对象 → 2D 对象 → 瓦片地图 → 矩形** — 创建瓦片地图。
2. **窗口 → 2D → 瓦片调色板** — 打开瓦片调色板窗口。
3. 将精灵图拖入瓦片调色板以创建瓦片资源。
4. 使用画笔工具在场景视图中绘制瓦片。
5. 添加**瓦片地图碰撞体 2D** 以实现碰撞。
6. 添加**复合碰撞体 2D** + **Rigidbody2D（运动学）** 合并瓦片形状以获得更好性能。

### 2D Physics (2D 物理)

```csharp
Rigidbody2D rb2d = GetComponent<Rigidbody2D>();

rb2d.AddForce(Vector2.up * 300f);
rb2d.AddForce(Vector2.right * 200f, ForceMode2D.Impulse);
rb2d.linearVelocity = new Vector2(5f, rb2d.linearVelocity.y);

// 2D Raycast
RaycastHit2D hit = Physics2D.Raycast(origin2D, direction2D, distance);
if (hit.collider != null) Debug.Log(hit.collider.name);

// OverlapCircle ground check
bool isGrounded = Physics2D.OverlapCircle(
    groundCheck.position, checkRadius, groundLayer);

// CircleCast
RaycastHit2D hit2 = Physics2D.CircleCast(origin2D, 0.5f, direction2D, distance);

// All colliders in area
Collider2D[] cols = Physics2D.OverlapCircleAll(center2D, 1f);
```

---

## 11. UI Systems (UI 系统)

Unity provides three UI systems: **UI Toolkit** (recommended for new projects), **uGUI** (GameObject-based, still widely used), and **IMGUI** (legacy, code-driven).

Unity 提供三种 UI 系统：**UI Toolkit**（新项目推荐）、**uGUI**（基于游戏对象，仍广泛使用）和 **IMGUI**（旧版，代码驱动）。

### UI Toolkit (UI Toolkit)

UI Toolkit uses a document model similar to HTML/CSS. Runtime workflow:

UI Toolkit 使用类似 HTML/CSS 的文档模型。运行时工作流程：

1. Add **UI Document** component to a GameObject.
2. Assign a **Panel Settings** asset (DPI, scale mode).
3. Assign a **UXML** asset as Source Asset.
4. Query and manipulate elements in C# via `UIDocument.rootVisualElement`.

---

1. 在游戏对象上添加 **UI Document** 组件。
2. 分配 **Panel Settings** 资源（DPI、缩放模式）。
3. 分配 **UXML** 资源作为源资源。
4. 在 C# 中通过 `UIDocument.rootVisualElement` 查询和操作元素。

```csharp
using UnityEngine.UIElements;

void OnEnable()
{
    var root = GetComponent<UIDocument>().rootVisualElement;

    Button btn    = root.Q<Button>("my-button");
    Label  lbl    = root.Q<Label>("score-label");
    TextField tf  = root.Q<TextField>("name-field");
    Slider slider = root.Q<Slider>("volume-slider");

    btn.RegisterCallback<ClickEvent>(evt => lbl.text = "Clicked!");
    slider.RegisterValueChangedCallback(evt => Debug.Log(evt.newValue));
    tf.RegisterValueChangedCallback(evt => Debug.Log(evt.newValue));

    // Style manipulation
    lbl.style.color = Color.red;
    btn.style.display = DisplayStyle.None;  // hide (隐藏)

    // USS class
    btn.AddToClassList("highlighted");
    btn.ToggleInClassList("active");

    // Create element in code
    var newLabel = new Label("Hello");
    root.Add(newLabel);
}
```

### Unity UI (uGUI) (Unity UI 系统)

![uGUI Hello World example](https://docs.unity3d.com/uploads/Main/GUIScriptingGuideHelloExample.png)
*Official Unity docs image — uGUI IMGUI Hello World example (官方 Unity 文档图片 — uGUI IMGUI Hello World 示例)*

uGUI uses a **Canvas** as root. All UI elements are GameObjects with **RectTransform** components.

uGUI 使用 **Canvas（画布）** 作为根节点。所有 UI 元素都是带有 **RectTransform** 组件的游戏对象。

**Canvas Render Modes (Canvas 渲染模式):**
- **Screen Space – Overlay**: UI drawn on top of everything, directly on screen. (UI 绘制在所有内容之上，直接显示在屏幕上)
- **Screen Space – Camera**: rendered by a specific camera, supports perspective. (由特定摄像机渲染，支持透视)
- **World Space**: UI exists in 3D world (nameplates, in-game panels). (UI 存在于 3D 世界中，适用于名牌、游戏内面板)

**uGUI Components (uGUI 组件):**

| Component (组件) | Description (描述) |
|---|---|
| Text (TextMeshPro) | Rich text with SDF fonts. Preferred over legacy Text. (富文本 + SDF 字体，优于旧版 Text) |
| Image | Displays a Sprite. Simple / Sliced / Tiled / Filled modes. (显示精灵图，支持简单/切片/平铺/填充) |
| Raw Image | Displays any Texture (not limited to Sprites). (显示任何纹理，不限于精灵图) |
| Button | Clickable with `onClick` UnityEvent. (可点击，带 onClick UnityEvent) |
| Toggle | On/off checkbox with `onValueChanged`. (开/关复选框) |
| Slider | Draggable range value. (可拖动范围值) |
| Dropdown (TMP) | Select one from a list. (从列表中选择一项) |
| Input Field (TMP) | Text entry field. (文本输入字段) |
| Canvas Group | Control alpha/interactability/raycasting for a group. (控制一组的透明度/可交互性/射线投射) |
| Layout Group | Auto-arrange children: Horizontal / Vertical / Grid. (自动排列子元素：水平/垂直/网格) |
| Content Size Fitter | Resize to fit content. (自动调整大小以适应内容) |
| Scroll Rect | Scrollable content area. (可滚动内容区域) |

```csharp
using UnityEngine.UI;
using TMPro;

// Button
Button btn = GetComponent<Button>();
btn.onClick.AddListener(() => Debug.Log("Click!"));
btn.interactable = false;

// TMP Text
TMP_Text lbl = GetComponent<TMP_Text>();
lbl.text = "Score: " + score;
lbl.color = Color.yellow;
lbl.fontSize = 24f;

// Slider
Slider s = GetComponent<Slider>();
s.value = 0.5f; s.minValue = 0f; s.maxValue = 1f;
s.onValueChanged.AddListener(val => Debug.Log(val));

// Toggle
Toggle t = GetComponent<Toggle>();
t.isOn = true;
t.onValueChanged.AddListener(val => Debug.Log(val));

// TMP InputField
TMP_InputField field = GetComponent<TMP_InputField>();
field.onEndEdit.AddListener(val => Debug.Log("Submitted: " + val));
```

---

## 12. Visual Effects (视觉效果)

### Particle System — Shuriken (粒子系统)

![The holo table in Unity's Spaceship demo, made with the Visual Effect Graph.](https://docs.unity3d.com/uploads/Main/ParticleSystems-HoloTable.png)
*Official Unity docs image — Particle systems / Visual Effect Graph demo (官方 Unity 文档图片 — 粒子系统 / 视觉效果图演示)*

> 📖 **Official page:** [ParticleSystems](https://docs.unity3d.com/Manual/ParticleSystems.html)

Unity has two particle systems: the built-in **Particle System** (Shuriken, CPU-driven) and the **Visual Effect Graph** (VFX Graph, GPU-driven). The built-in system is compatible with all platforms; VFX Graph requires Shader Model 4.5+.

Unity 有两套粒子系统：内置**粒子系统**（Shuriken，CPU 驱动）和**视觉效果图**（VFX Graph，GPU 驱动）。内置系统兼容所有平台；VFX Graph 需要 Shader Model 4.5+。

**Particle System Modules (粒子系统模块):**

| Module (模块) | Key Settings (关键设置) |
|---|---|
| **Main** | Duration, Looping, Start Lifetime, Start Speed, Start Size, Start Color, Gravity Modifier, Simulation Space (World/Local) |
| **Emission** | Rate over Time, Rate over Distance, Bursts (count + time + interval) |
| **Shape** | Sphere, Hemisphere, Cone, Box, Mesh, Circle, Edge, Donut |
| **Velocity over Lifetime** | X/Y/Z velocity curves over the particle's life |
| **Color over Lifetime** | Gradient color and alpha over particle life |
| **Size over Lifetime** | Curve-driven size change |
| **Rotation over Lifetime** | Angular velocity over life |
| **Noise** | Strength, Frequency, Scroll Speed — adds turbulence |
| **Collision** | World or Planes collision; bounce, lifetime loss on hit |
| **Lights** | Each particle emits a Point Light |
| **Trails** | Ribbon or Per-Particle trails with configurable width/color |
| **Renderer** | Mesh / Billboard / Stretched Billboard; Material; Sorting Layer |
| **Sub Emitters** | Spawn child particle systems on Birth / Collision / Death |
| **Texture Sheet Animation** | Animate a sprite sheet UV over the particle's life |

**Particle System C# API:**

```csharp
ParticleSystem ps = GetComponent<ParticleSystem>();

ps.Play();  ps.Stop();  ps.Pause();  ps.Clear();

bool playing = ps.isPlaying;
int  count   = ps.particleCount;

// Emit burst immediately
ps.Emit(30);

// Modify main module at runtime
var main = ps.main;
main.startColor  = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
main.startSpeed  = new ParticleSystem.MinMaxCurve(2f, 8f);
main.loop        = false;

// Modify emission rate
var emission = ps.emission;
emission.rateOverTime = 100f;
emission.enabled = false;   // stop emitting new particles (停止发射新粒子)

// Set a burst
emission.SetBursts(new[]{ new ParticleSystem.Burst(0f, 30) });
```

### Post-Processing (后期处理)

![Scene that uses post-processing effects.](https://docs.unity3d.com/uploads/Main/PostProcessing-1.jpg)
*Official Unity docs image — Post-processing effects (官方 Unity 文档图片 — 后期处理效果)*

> 📖 **Official page:** [post-processing-and-full-screen-effects](https://docs.unity3d.com/Manual/post-processing-and-full-screen-effects.html)

In URP, add a **Volume** component, create a **Volume Profile**, and add effect overrides.

在 URP 中，添加 **Volume** 组件，创建 **Volume Profile**，然后添加效果覆盖。

**URP Volume Overrides (URP Volume 覆盖效果):**

| Effect (效果) | Key Parameters (关键参数) |
|---|---|
| **Bloom** | Threshold, Intensity, Scatter, Clamp (阈值、强度、散射、钳制) |
| **Depth of Field** | Mode (Bokeh/Gaussian), Focus Distance, Aperture, Focal Length (散景/高斯，对焦距离，光圈，焦距) |
| **Motion Blur** | Mode, Sample Count, Intensity (模式、采样数、强度) |
| **Vignette** | Color, Center, Intensity, Smoothness, Rounded (颜色、中心、强度、平滑度、圆形) |
| **Color Grading** | Mode, LUT, Post Exposure, Contrast, Color Filter, Hue Shift, Saturation |
| **Chromatic Aberration** | Intensity (强度) |
| **Film Grain** | Type, Intensity, Response (类型、强度、响应) |
| **SSAO** | Intensity, Radius, Quality (强度、半径、质量) |
| **Tonemapping** | Mode: None / Neutral / ACES |
| **Panini Projection** | Distance, Crop to Fit (用于广角镜头失真校正) |

---

## 13. Multiplayer (多人游戏)

Unity's multiplayer ecosystem consists of dedicated packages. The recommended entry point is the **Multiplayer Center** (**Window → Multiplayer → Multiplayer Center**) which recommends the right packages based on your project's needs.

Unity 的多人游戏生态由专用包组成。推荐的入口点是**多人游戏中心**（**窗口 → 多人游戏 → 多人游戏中心**），它会根据您的项目需求推荐合适的包。

### Netcode for GameObjects — NGO (游戏对象网络代码)

```csharp
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    // Synced variable: server writes, all clients read
    private NetworkVariable<int> health =
        new NetworkVariable<int>(100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    void Start()
    {
        health.OnValueChanged += (old, cur) =>
            Debug.Log($"HP: {old} → {cur}");
    }

    void Update()
    {
        if (!IsOwner) return;     // only process input for local player (仅处理本地玩家输入)
        if (Input.GetKeyDown(KeyCode.Space))
            JumpServerRpc();
    }

    [ServerRpc]              // called by owner client, runs on server (由拥有者客户端调用，在服务器上运行)
    void JumpServerRpc()
    {
        JumpClientRpc();     // notify all clients (通知所有客户端)
    }

    [ClientRpc]              // called by server, runs on all clients (由服务器调用，在所有客户端上运行)
    void JumpClientRpc()
    {
        Debug.Log("Jump!");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"Spawned — Server:{IsServer} Client:{IsClient} Owner:{IsOwner}");
    }
}
```

### Unity Relay and Lobby (Unity Relay 与大厅)

```csharp
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

// Initialize services
await UnityServices.InitializeAsync();
await AuthenticationService.Instance.SignInAnonymouslyAsync();

// HOST — create relay
Allocation alloc = await RelayService.Instance.CreateAllocationAsync(4);
string joinCode  = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

// CLIENT — join relay
JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(joinCode);

// Create public lobby
Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("My Game", 4,
    new CreateLobbyOptions
    {
        IsPrivate = false,
        Data = new Dictionary<string, DataObject>
        {
            {"Mode", new DataObject(DataObject.VisibilityOptions.Public, "CTF")}
        }
    });

// Query lobbies
QueryResponse qr = await LobbyService.Instance.QueryLobbiesAsync();
foreach (var l in qr.Results)
    Debug.Log($"{l.Name}  Players:{l.Players.Count}/{l.MaxPlayers}");
```

---

## 14. XR — VR / AR / MR (扩展现实)

![Sample XR scene view](https://docs.unity3d.com/uploads/Main/xr-hero-img.png)
*Official Unity docs image — XR scene view (官方 Unity 文档图片 — XR 场景视图)*

> 📖 **Official page:** [XR](https://docs.unity3d.com/Manual/XR.html)

Develop augmented, mixed, and virtual reality experiences with the Unity Editor. Unity provides extensive XR support through dedicated packages.

使用 Unity 编辑器开发增强现实、混合现实和虚拟现实体验。Unity 通过专用包提供广泛的 XR 支持。

### Setting up an XR Project (设置 XR 项目)

1. **Window → Package Manager** → Install **XR Plugin Management**.
2. **Edit → Project Settings → XR Plug-in Management** → enable your platform plugin (OpenXR, ARCore, ARKit, Oculus, etc.).
3. Install **XR Interaction Toolkit** (`com.unity.xr.interaction.toolkit`).
4. For AR: install **AR Foundation** (`com.unity.xr.arfoundation`).

---

1. **窗口 → 包管理器** → 安装 **XR 插件管理**。
2. **编辑 → 项目设置 → XR 插件管理** → 启用平台插件（OpenXR、ARCore、ARKit、Oculus 等）。
3. 安装 **XR Interaction Toolkit**（`com.unity.xr.interaction.toolkit`）。
4. 对于 AR：安装 **AR Foundation**（`com.unity.xr.arfoundation`）。

**XR Interaction Toolkit Key Components (关键组件):**

| Component (组件) | Description (描述) |
|---|---|
| XR Origin | Root GameObject. Contains Camera Offset and Main Camera. (根游戏对象，包含摄像机偏移和主摄像机) |
| XR Controller | Reads position, rotation, and button states from VR controller. (读取 VR 控制器输入) |
| XR Ray Interactor | Projects a ray for pointing/UI interaction. (投射射线用于指向/UI 交互) |
| XR Direct Interactor | Grabs nearby objects on contact. (接触时抓取附近对象) |
| XR Grab Interactable | Marks a GameObject as grabbable. (将游戏对象标记为可抓取) |
| XR Simple Interactable | Hover/select events without grab. (无抓取的悬停/选择事件) |
| Teleportation Area | Surface users can teleport to. (用户可传送到的表面) |
| Snap Turn Provider | Controller joystick → discrete rotation steps. (控制器摇杆→离散旋转步骤) |
| Continuous Move Provider | Smooth locomotion via controller. (通过控制器平滑移动) |

**AR Foundation Components (AR Foundation 组件):**

| Component (组件) | Description (描述) |
|---|---|
| AR Session | Manages the AR session lifecycle. (管理 AR 会话生命周期) |
| AR Session Origin → now XR Origin | Root for AR scene setup. (AR 场景设置的根) |
| AR Plane Manager | Detects and tracks flat surfaces. (检测并跟踪平面) |
| AR Raycast Manager | Raycast against detected surfaces. (对检测到的表面进行射线检测) |
| AR Anchor Manager | Creates world-locked anchors. (创建世界锁定锚点) |
| AR Face Manager | Detects and tracks faces (ARKit/ARCore). (检测和跟踪面部) |
| AR Image Tracking Manager | Tracks 2D reference images. (跟踪 2D 参考图像) |

---

## 15. Unity AI (Unity 人工智能)

### Unity Sentis (Unity Sentis)

Unity Sentis lets you run **ONNX** machine learning models in the Unity runtime — on CPU or GPU — without a network connection. Applications: NPC behavior, gesture recognition, image classification, procedural animation, and more.

Unity Sentis 允许您在 Unity 运行时中运行 **ONNX** 机器学习模型——在 CPU 或 GPU 上——无需网络连接。应用场景：NPC 行为、手势识别、图像分类、程序化动画等。

```csharp
using Unity.Sentis;

public class SentisExample : MonoBehaviour
{
    public ModelAsset modelAsset;
    private IWorker worker;
    private Model runtimeModel;

    void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        // BackendType.GPUCompute — GPU inference; BackendType.CPU — CPU inference
        worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, runtimeModel);
    }

    void RunInference(Texture2D tex)
    {
        using var input  = TextureConverter.ToTensor(tex, 224, 224, 3);
        worker.Execute(input);
        using var output = worker.PeekOutput() as TensorFloat;
        output.MakeReadable();
        float[] scores = output.ToReadOnlyArray();
        int cls = System.Array.IndexOf(scores, Mathf.Max(scores));
        Debug.Log("Class: " + cls);
    }

    void OnDestroy() => worker.Dispose();
}
```

### NavMesh — AI Navigation (NavMesh — AI 导航)

> ✅ **v2 correction (v2 修正):** `Navigation.html` returns 404 in Unity 6.4. The built-in NavMesh is supplemented by the **AI Navigation** package (`com.unity.ai.navigation`). Install via Package Manager.

> ✅ **v2 修正：** `Navigation.html` 在 Unity 6.4 中返回 404。内置 NavMesh 由 **AI Navigation** 包（`com.unity.ai.navigation`）补充，通过包管理器安装。

**Setup (设置):**

1. Mark static geometry as **Navigation Static** (or use the NavMesh Surface component from AI Navigation package).
2. Add a **NavMesh Surface** component and click **Bake**.
3. Add **NavMeshAgent** to the AI character.

---

1. 将静态几何体标记为**导航静态**（或使用 AI Navigation 包中的 NavMesh Surface 组件）。
2. 添加 **NavMesh Surface** 组件并点击**烘焙**。
3. 向 AI 角色添加 **NavMeshAgent**。

```csharp
using UnityEngine;
using UnityEngine.AI;

public class AIEnemy : MonoBehaviour
{
    NavMeshAgent agent;
    public Transform target;

    void Start() => agent = GetComponent<NavMeshAgent>();

    void Update()
    {
        agent.SetDestination(target.position);
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
            Debug.Log("Reached!");
    }
}
```

**NavMeshAgent Key Properties (NavMeshAgent 关键属性):**

| Property (属性) | Description (描述) |
|---|---|
| Speed | Max movement speed (m/s). (最大移动速度) |
| Angular Speed | Max rotation speed (deg/s). (最大旋转速度) |
| Acceleration | Max acceleration (m/s²). (最大加速度) |
| Stopping Distance | Stop when this close to destination. (距目标此距离时停止) |
| Auto Braking | Slow down near destination. (接近目标时减速) |
| Radius | Avoidance radius. (回避半径) |
| Height | Agent height for obstacle clearance. (代理高度) |
| Area Mask | Which NavMesh areas this agent can traverse. (代理可穿越的 NavMesh 区域) |

---

## 16. Unity Services (Unity 服务)

Unity services extend the capabilities of your game with cloud-based features — analytics, in-app purchases, remote configuration, player authentication, multiplayer infrastructure, and more.

Unity 服务通过基于云的功能扩展游戏能力——数据分析、应用内购买、远程配置、玩家身份验证、多人游戏基础设施等。

### Unity Analytics (Unity 分析)

```csharp
using Unity.Services.Core;
using Unity.Services.Analytics;

await UnityServices.InitializeAsync();

// Record custom event
AnalyticsService.Instance.RecordEvent("level_complete",
    new Dictionary<string, object>
    {
        { "level_id",      5 },
        { "time_seconds",  120.5f },
        { "score",         9800 }
    });

AnalyticsService.Instance.Flush();   // flush immediately (立即上传)
```

### Unity Cloud Save (Unity 云存档)

```csharp
using Unity.Services.CloudSave;

// Save
await CloudSaveService.Instance.Data.Player.SaveAsync(
    new Dictionary<string, object>{ {"level", 10}, {"gold", 500} });

// Load
var data  = await CloudSaveService.Instance.Data.Player.LoadAsync(
    new HashSet<string>{ "level", "gold" });
int level = data["level"].Value.GetAs<int>();
```

### Unity Remote Config (Unity 远程配置)

```csharp
using Unity.Services.RemoteConfig;

// Fetch and apply remote config values
RemoteConfigService.Instance.FetchCompleted +=
    r => Debug.Log("RemoteDifficulty: " + r.config.GetFloat("difficulty", 1f));

await RemoteConfigService.Instance.FetchConfigsAsync(
    new UserAttributes(), new AppAttributes());
```

---

## 17. Performance and Optimization (性能与优化)

### Profiler (性能分析器)

Open via **Window → Analysis → Profiler**. Record CPU, GPU, Memory, Rendering, Audio, and Physics data frame by frame.

通过**窗口 → 分析 → 性能分析器**打开。逐帧记录 CPU、GPU、内存、渲染、音频和物理数据。

**Key Profiler Modules (关键分析器模块):**

| Module (模块) | What it shows (显示内容) |
|---|---|
| CPU Usage | Script, physics, rendering, and GC time per frame. (每帧脚本、物理、渲染、GC 时间) |
| GPU Usage | Which render passes consume most GPU time. (哪些渲染通道消耗最多 GPU 时间) |
| Rendering | Draw calls, SetPass calls, triangles, vertices, shadow casters. (绘制调用、SetPass 调用、三角形、顶点、阴影投射者) |
| Memory | Total and per-category memory, GC allocations. (总内存和分类内存，GC 分配) |
| Physics | Simulation time, number of contacts and active rigidbodies. (模拟时间、接触数、活跃刚体数) |

### Draw Call Optimization (绘制调用优化)

| Technique (技术) | How to enable (如何启用) | Benefit (效果) |
|---|---|---|
| Static Batching | Mark objects **Static** | Combines non-moving same-material meshes (合并不移动的同材质网格) |
| Dynamic Batching | Auto (small meshes < 900 verts) | Batches small dynamic same-material meshes (批处理小型动态同材质网格) |
| GPU Instancing | Enable on Material | Single draw call for many identical meshes (一次绘制调用渲染大量相同网格) |
| SRP Batcher | Enabled by default in URP/HDRP | Reduces per-material CPU overhead (降低每材质 CPU 开销) |
| GPU Resident Drawer | Unity 6 — enable in URP Asset | Keeps mesh data on GPU across frames (在帧间将网格数据保留在 GPU 上) |
| LOD Groups | Add LOD Group component | Switches to lower-res mesh at distance (在距离上切换到更低精度网格) |

### Object Pooling (对象池)

```csharp
using UnityEngine.Pool;

private ObjectPool<GameObject> pool;

void Awake()
{
    pool = new ObjectPool<GameObject>(
        createFunc:      () => Instantiate(prefab),
        actionOnGet:     obj => obj.SetActive(true),
        actionOnRelease: obj => obj.SetActive(false),
        actionOnDestroy: obj => Destroy(obj),
        collectionCheck: true,
        defaultCapacity: 10,
        maxSize:         100
    );
}

void Fire()
{
    GameObject bullet = pool.Get();
    bullet.transform.position = muzzle.position;
    // On impact: pool.Release(bullet);
}
```

### Performance Checklist (性能检查清单)

```
✅ DO (应该做):
   Cache GetComponent<T>() in Awake/Start — never in Update
   Use FixedUpdate for physics, LateUpdate for camera
   Use TryGetComponent instead of GetComponent + null check
   Prefer FindObjectsByType over FindObjectOfType (non-allocating overloads)
   Use StringBuilder for frequent string concatenation
   Enable GPU Instancing on repeated-mesh materials
   Compress textures per platform
   Use Addressables for large asset loading
   Profile FIRST, optimize second

❌ DON'T (不应该做):
   Call GetComponent<T>() every frame
   Use GameObject.Find / FindObjectOfType in Update
   Allocate with `new` inside Update (creates GC pressure)
   Use Camera.main every frame in Unity < 2020.1
   Overuse particle systems without culling
   Ignore the Profiler and guess at bottlenecks
```

---

## 18. Asset Management (资源管理)

### Addressable Asset System (可寻址资源系统)

The Addressable Asset System provides a scalable, async approach to loading and managing assets — better than `Resources.Load()` for large projects.

可寻址资源系统提供了一种可扩展的异步资源加载和管理方式——对于大型项目比 `Resources.Load()` 更好。

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// Load asset
var handle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Enemy");
await handle.Task;
if (handle.Status == AsyncOperationStatus.Succeeded)
    Instantiate(handle.Result, Vector3.zero, Quaternion.identity);
Addressables.Release(handle);   // always release when done (使用完毕后始终释放)

// Instantiate directly (lifecycle managed)
var inst = Addressables.InstantiateAsync("Prefabs/Enemy");
await inst.Task;
GameObject enemy = inst.Result;
Addressables.ReleaseInstance(enemy);  // release instance (释放实例)

// Load scene
Addressables.LoadSceneAsync("Scenes/GameLevel", LoadSceneMode.Single);

// Preload and track progress
var op = Addressables.DownloadDependenciesAsync("level_assets_label");
while (!op.IsDone)
{
    float progress = op.PercentComplete;
    yield return null;
}
Addressables.Release(op);
```

### Package Manager (包管理器)

Open via **Window → Package Manager**. Packages extend Unity with features, tools, and integrations. Key package sources:

通过**窗口 → 包管理器**打开。包通过功能、工具和集成扩展 Unity。主要包来源：

- **Unity Registry**: official Unity packages (官方 Unity 包)
- **My Assets**: Asset Store purchases (资源商店购买)
- **In Project**: currently installed packages (当前已安装的包)
- **Git URL**: install directly from a Git repository (从 Git 仓库直接安装)
- **Local**: install from a local folder on disk (从本地磁盘文件夹安装)

---

## 19. Building and Publishing (构建与发布)

### Build Settings (构建设置)

Open via **File → Build Settings**. Add scenes by opening them and clicking **Add Open Scenes**. Select the target platform and click **Build** or **Build And Run**.

通过**文件 → 构建设置**打开。通过打开场景并点击**添加打开的场景**来添加场景。选择目标平台并点击**构建**或**构建并运行**。

**Platform-Specific Key Settings (平台专属关键设置):**

| Platform (平台) | Key Settings (关键设置) |
|---|---|
| PC / Mac / Linux | Architecture (x86/x64/ARM64), Server Build option (架构，服务器构建选项) |
| Android | Min API Level (≥22 recommended), IL2CPP backend, ARM64 target, Keystore signing (最低 API 级别，IL2CPP，ARM64，密钥库签名) |
| iOS | Requires macOS + Xcode. Bundle ID, Signing Team, min iOS version (需要 macOS + Xcode，Bundle ID，签名团队，最低 iOS 版本) |
| WebGL | Compression: Brotli (recommended) / Gzip / Disabled; Memory Size; Linker Target (压缩：Brotli（推荐）/Gzip/禁用；内存大小；链接器目标) |
| Console (PS5/Xbox) | Requires platform-specific SDK and approved Unity license (需要平台专属 SDK 和经批准的 Unity 许可) |

### Player Settings Key Properties (播放器设置关键属性)

Access via **Edit → Project Settings → Player**:

通过**编辑 → 项目设置 → 播放器**访问：

| Property (属性) | Options (选项) | Description (描述) |
|---|---|---|
| Company/Product Name | Text | App metadata (应用元数据) |
| Version | Semver string | Build version displayed to users (向用户显示的构建版本) |
| Scripting Backend | **Mono** / **IL2CPP** | Mono=fast iteration; IL2CPP=better runtime perf, required for iOS (Mono=快速迭代；IL2CPP=更好运行时性能，iOS 必须) |
| API Compatibility Level | .NET Standard 2.1 / .NET Framework | Controls available BCL APIs (.NET BCL API 可用性) |
| Scripting Define Symbols | Comma-separated | Custom `#define` preprocessor symbols (自定义预处理符号) |
| Allow Unsafe Code | On/Off | Required by some packages and Burst (某些包和 Burst 需要) |
| Active Input Handling | Input Manager / Input System / Both | Choose input backend (选择输入后端) |
| Strip Engine Code | On/Off (IL2CPP) | Remove unused engine modules to reduce build size (移除未使用的引擎模块以减小体积) |

### Scripting Backends (脚本后端)

**Mono**: Compiles C# to IL bytecode interpreted at runtime. Fastest build iteration. Lower runtime performance. Use for development.

**Mono**：将 C# 编译为运行时解释的 IL 字节码。最快的构建迭代速度，运行时性能较低。用于开发阶段。

**IL2CPP**: Transpiles C# IL → C++ → native machine code via AOT compilation. Slower to build but significantly better runtime performance and security. Required for iOS, game consoles, and WebAssembly targets. Recommended for all release builds.

**IL2CPP**：通过 AOT 编译将 C# IL 转换为 C++ 再编译为原生机器码。构建较慢，但运行时性能和安全性显著更好。iOS、游戏主机和 WebAssembly 目标必须使用。推荐用于所有发布构建。

---

> **© 2005–2026 Unity Technologies. All rights reserved.**
> **© 2005–2026 Unity Technologies. 保留所有权利。**
>
> **Document version:** Unity_Manual_Bilingual_v2.md
> **Based on:** Unity 6.4 (6000.4) Official Manual — https://docs.unity3d.com/Manual/UnityManual.html
> **Images:** All images are sourced from the official Unity Documentation CDN (`docs.unity3d.com/uploads/Main/`) and are the property of Unity Technologies.
>
> 本文档基于 Unity 6.4 (6000.4) 官方手册，所有图片来源于 Unity 官方文档 CDN，版权归 Unity Technologies 所有。
