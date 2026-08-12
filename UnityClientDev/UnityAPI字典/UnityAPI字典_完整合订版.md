# Unity API 字典（完整合订版）

> 本文件为 **UnityAPI字典/** 目录下全部文档的合订版，便于单文件通读与检索。分章版本仍保留在目录中，按章查阅请回到各独立文件。

---


---

# 第 1 章 核心物件体系

> **本章管辖**：Unity 里"一切皆对象"的根基——`GameObject`、`Component`、`Object`、`MonoBehaviour` 以及生命周期回调。
> **一句话**：`GameObject` 是"空壳"，`Component` 是"能力"，`MonoBehaviour` 是"你写的逻辑"，`Object` 是所有 Unity 物体的祖先。
> **前置**：建议先看 [README](../README.md) 了解词条模板。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 在场景里创建一个空物体 | `new GameObject()` / `new GameObject(name)` |
| 给物体挂个脚本组件 | `gameObject.AddComponent<T>()` |
| 拿到物体上的某个组件 | `GetComponent<T>()` |
| 找到场景里一个物体 | `GameObject.Find(name)` |
| 发送消息/广播给组件 | `SendMessage` / `BroadcastMessage` |
| 让物体暂时消失/出现 | `gameObject.SetActive(bool)` |
| 销毁一个物体 | `Destroy(obj)` / `DestroyImmediate(obj)` |
| 让物体跨场景保留（不被销毁） | `DontDestroyOnLoad(obj)`；销毁用 `Destroy(obj)` |
| 判断"物体其实不存在了" | `if (obj == null)`（Unity 的空对象陷阱） |
| 拿到自己在世界的位置 | `transform.position`（见第 2 章） |
| 物体带逻辑，我要写脚本 | 继承 `MonoBehaviour` |

---

## 1.1 GameObject（游戏对象）

### 1.1.1 `class GameObject : Object`

**【是什么】** 一切场景元素的根容器。它自己**不存储任何数据**，只负责"装"一组 `Component`，并通过组件提供能力（渲染、物理、逻辑……）。可以理解为 C++ 里的"空壳对象" + 组件列表。

**【用途】** 场景里每个可见/可交互的东西，本质都是一个 `GameObject`。你要创建、查找、开关、销毁物体时都用它。

```
GameObject (壳)
 ├── Transform (位置/旋转/缩放 — 每个 GameObject 必有)
 ├── MeshRenderer (渲染)
 ├── Rigidbody (物理)
 └── 你的脚本组件 (MyScript:MonoBehaviour)
```

**【名称含义】** `GameObject` = **Game**（游戏）+ **Object**（对象）。注意是"游戏对象"不是"游戏物品"。

**【参数/构造】**
- `new GameObject()`：只创建空物体（自动带 `Transform`）。
- `new GameObject(string name, params Type[] components)`：创建时指定名字，可顺带挂初始组件（如摄像机、灯光）。

**【代码示例】**
```csharp
// 创建空物体
GameObject empty = new GameObject("我的空物体");

// 创建时即挂上"摄像机"组件（等价于默认 Camera）
GameObject cam = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
Camera c = cam.GetComponent<Camera>();

// 设置父物体（核心属性 transform）
empty.transform.SetParent(cam.transform, false);
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `new GameObject()` 与 `Instantiate()` | `new` 创建全新空物体；`Instantiate` 用于克隆已有 Prefab/物体（见第 7 章） |
| `GameObject` 与 `Transform` | GameObject 是容器；Transform 是它自己的位置组件，二者通过 `gameObject.transform` / `transform.gameObject` 互取 |

【⚠️ 注意】Unity 是单线程的，`Awake` 在对象实例化时被**同步**调用；不要在 `Awake` 里依赖"场景已完全加载完毕"的状态（某些对象可能还没 `Awake`），需要跨对象通信时用 `Start` 或订阅事件。

---

### 1.1.2 `GameObject.name` 属性

**【是什么】** 物体在 Hierarchy 面板里的显示名字（字符串）。

```csharp
string name = go.name;   // 读
go.name = "改名了";       // 写
```

**【用途】** 调试、查找。
**【函数名（名称含义）】** `name` = 名字。

---

### 1.1.3 `GameObject.Find(string name)` 静态方法

**【是什么】** 按名字从**整个活动场景**中查找 GameObject。
**【用途】** 快速查找物体（运行时）。但有成本，不推荐在 `Update` 里反复调用。

**【参数说明】** `name`：目标物体的 `name`（支持路径：`"父/子"`，支持 `/`）。

**【返回值】** 找到的 `GameObject`；找不到返回 `null`。

**【代码示例】**
```csharp
GameObject player = GameObject.Find("/Root/Player"); // 用完整路径最快
if (player == null) Debug.LogWarning("没找到");
```

**【相似 API 区别】** `Find` 与 `FindObjectOfType<T>`：
- `Find` 按名字找物体（瘦对象）；
- `FindObjectOfType<T>` 按组件类型找（返回第一个带该组件的物体），详见 7 章。
- 注意：`Find` 从未被弃用，可一直用；而 `FindObjectOfType<T>` 系列（按类型找）在 `Unity 2023.1` 起**被弃用**，推荐改用性能更好的 `Object.FindFirstObjectByType<T>()` / `FindAnyObjectByType<T>()`（该系列在 `2021.3.18+` 已可用）。

---

### 1.1.4 `GameObject.SetActive(bool)` 与 `activeSelf` / `activeInHierarchy`

**【是什么】** 控制物体在场景中的**激活状态**。

- `go.SetActive(bool value)`：开启或关闭物体（连同其所有子物体一起不可见/不运行逻辑）。
- `activeSelf`：**只读自身是否激活**（不看父级）。
- `activeInHierarchy`：该物体是否"真地"处于激活状态（自身激活 **且** 所有父级都激活）。

**【用途】** UI 显隐、敌人隐藏、优化（关闭不用的 GameObject）。

**【代码示例】**
```csharp
enemy.SetActive(false);          // 隐藏敌方（不渲染、Update 不跑）
if (enemy.activeInHierarchy) {...} // 判断它在这个层级是否真的有效
```

**【相似区别】** `SetActive` vs `Destroy`：
- `SetActive(false)`：只是停用，随时可 `SetActive(true)` 恢复，内存仍在。
- `Destroy`：真正移除对象，无法恢复。

---

### 1.1.5 `GameObject.SetActive(false)` / 生命周期联动

- 关闭物体后，挂在它身上的 `MonoBehaviour` 的 `Update` 不再被调用。
- 重新开启时 `OnEnable` 会被再次调用。

---

## 1.2 Object（UnityEngine.Object）—— 所有 Unity 内置对象的基类

### 1.2.1 `class Object` — 基类

**【是什么】** 所有 Unity 对象（`GameObject`、`Component`、`MonoBehaviour`、材质、纹理、脚本资源……）都继承自** `UnityEngine.Object`**（注意它和 C# 的 `System.Object` 是两个东西！）。

**【用途】** 提供**实例管理 + 预定义比较 + 通用工具**。

**【经典陷阱🚨】**：`if (obj == null)` 用的是 **Unity 的 == 运算符**（C++ 底层帮你判断，不是真正的 C# 引用为 null）。所以一个"被销毁的"对象，在 C# 看来 `== null` 为 true，但 `??` 之类运算会有区别。**一定要用 `== null` 判断"被销毁"**，不要用 `.Equals(null)`。

**【代码示例】**
```csharp
Destroy(box);
if (box == null) Debug.Log("box 已被销毁");  // ✅ 正确：Unity 陷阱

// ❌ 错误示范：很多人以为 box 还是有效的
// if (!box.Equals(null))  // 不是标准做法
```

**【功能】UnityEngine.Object 主要成员**
- `name`：物体名。
- `Object.Destroy`：销毁（延迟到帧末）。
- `Object.DestroyImmediate`：立即销毁（编辑器常用，运行时小心）。
- `Object.DontDestroyOnLoad`：切场景时不销毁该对象。
- `Object.Instantiate`：克隆资源。

---

### 1.2.2 `Object.Destroy(Object obj)` 静态方法

**【是什么】** 在帧末销毁指定对象。**这是运行时销毁对象的正确方式**，不会立即删除（让内存安全释放，等该帧结束）。

**【参数】** `obj`：要销毁的对象（可为组件或 GameObject 自己）。

**【代码示例】**
```csharp
Destroy(gameObject);                        // 销毁自身（整棵）
Destroy(otherGameObject);                   // 销毁别的物体
Destroy(GetComponent<Collider>());          // 销毁组件
```

**【返回值】** 无（void）。

【⚠️ 注意】在 `Update` 中调用它不会立即执行销毁逻辑，因为销毁发生在帧末。如果想**立即销毁**（编辑器工具）用 `DestroyImmediate`。

---

### 1.2.3 `Object.DontDestroyOnLoad(Object target)`

**【是什么】** 让一个对象在**场景切换时不被销毁**（常用来保存玩家信息管理器 / 单例管理器）。

```csharp
public static void DontDestroyOnLoad(Object target)
```

**【参数】** `target`：要"为了跨场景保留"的对象（通常传 `gameObject`）。

**【用途】** 全局管理器、跨场景数据。

**【代码示例】**
```csharp
// 单例管理器，跨场景不销毁
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // ✅ 关键：切场景不销毁自己
    }
}
```

---

## 1.3 Component（组件）

### 1.3.1 `class Component` — 所有 MonoBehaviour 组件的基类

**【是什么】** `Component` 是挂载在 `GameObject` 上的**逻辑/能力单元**。所有可挂到物体上的脚本（`MonoBehaviour`）都继承自 `Component`。

**Object → Component → MonoBehaviour → 你的脚本**

**【用途】** 拿到和操纵组件、与其他组件通信、读取所在物体属性。

**【核心成员】**
- `gameObject`：返回当前组件所挂的 GameObject。
- `transform`：返回当前组件的 `Transform`。
- `GetComponent<T>()`：拿到同一物体上的指定组件。
- `SendMessage` / `BroadcastMessage`：把消息发给自身/子物体（不推荐，类型不安全）。

**【代码示例】**
```csharp
// 脚本里访问自己所在的物体和位置
public class MyComponent : MonoBehaviour
{
    void Start()
    {
        GameObject myGo = this.gameObject;
        Transform myT = this.transform;
    }
}
```

---

### 1.3.2 `GetComponent<T>()` / `TryGetComponent<T>`

**【是什么】** 返回调用者`GameObject/Component` 上的一个组件实例。

```csharp
T GetComponent<T>();            // 找不到返回 null
bool TryGetComponent<T>(out T result);   // 不报异常，失败返回 false
```

**【参数说明】** `<T>` 即组件类型（泛型）。

**【返回值】** 
- `GetComponent` 找到组件则返回该类型组件，找不到返回 `null`（可能 NRE，注意判空）。
- `TryGetComponent`：找到返回 `true`，并通过 `out result` 传出；找不到返回 `false` + `result=null`。

**【代码示例】**
```csharp
// 方式一（简单，注意判空）
Rigidbody rb = GetComponent<Rigidbody>();
if (rb != null) rb.AddForce(Vector3.up);

// 方式二（推荐，避免反复查找）
if (TryGetComponent<Rigidbody>(out var rb2))
    rb2.AddForce(Vector3.forward);

// 直接拿子物体上的组件
Transform child = transform.Find("Child");
child.GetComponent<MeshRenderer>();
```

**【相似 API 区别】**
| API | 差异 |
|-----|------|
| `GetComponent<T>()` | 在**自己**身上找，找不到抛空引用风险 |
| `GetComponentInChildren<T>()` | 在自己 + **所有子物体**上找（可选含本/不含本） |
| `GetComponentInParent<T>()` | 在**父级链**上往上找 |
| `TryGetComponent<T>()` | 更安全：不抛异常，用 `out` |

---

## 1.4 MonoBehaviour —— 你写脚本的基类

### 1.4.1 `class MonoBehaviour : Behaviour`

**【是什么】** 你所有挂在物体上的脚本都必须继承它。只有继承 `MonoBehaviour` 的类才能：
- 作为组件挂到 `GameObject`；
- 接收生命周期回调（`Awake/Start/Update/...`）；
- 启动协程（`StartCoroutine`）。

**【名称含义】** `Mono`（托管 C#/Mono 运行时的）+ `Behaviour`（行为）。意思是"由 Mono 运行时驱动的行为脚本"。

**【代码示例】**
```csharp
using UnityEngine;
public class Player : MonoBehaviour   // 必须继承
{
    // 生命周期回调会自动被引擎调用
}
```

---

### 1.4.2 MonoBehaviour 生命周期回调（顺序）

引擎在特定时机调用这些虚方法/回调。**顺序**：

```
Awake → OnEnable → Start → (FixedUpdate / Update / LateUpdate) *每帧 → OnDisable → OnDestroy
```

| 回调 | 触发时机 | 用途 |
|------|---------|------|
| `Awake()` | 对象被实例化时立即调用（即使未启用） | 初始化内部引用 |
| `OnEnable()` | 每次启用时 | 订阅事件、重置 |
| `Start()` | 第一帧 Update 前、Awake 之后 | 拿外部引用、初始化数据 |
| `FixedUpdate()` | 固定时间步（物理） | 物理移动、Rigidbody |
| `Update()` | 每帧一次 | 常规逻辑 |
| `LateUpdate()` | Update 后 | 摄像机跟随等 |
| `OnDisable()` | 停用时 | 退订事件 |
| `OnDestroy()` | 销毁时/切场景时 | 清理资源 |

**C++/Raylib 类比**：这就是引擎帮你接管了 `while(WindowShouldClose())` 主循环；你只需在回调里填逻辑。

【面试常考】完整顺序：`Awake`→`OnEnable`→`Start`；每一帧内 `FixedUpdate`（可能多/少次）→`Update`→`LateUpdate`；切关 `OnDisable`→`OnDestroy`。注意 **Awake 先于 Start，且都只在对象初始化时执行一次**。

---

### 1.4.3 `enabled` 属性（来自 Behaviour）

```csharp
bool e = myComponent.enabled;   // 是否启用该脚本（注意不等于组件是否存在）
myComponent.enabled = true/false;
```
- 如果 `enabled=false`，`Update` 不再被调用，且会触发 `OnDisable`；再次 `enabled=true` 才会触发 `OnEnable`。

---

### 1.4.4 `gameObject` / `transform` / `transform` 快捷属性

在 `MonoBehaviour` 中：
```csharp
this.gameObject   // GameObject
this.transform    // Transform
```
不用 `GetComponent<Transform>()`——因为 Unity 给 `Behaviour/Component` 预置了 `transform` 快捷属性。

---

## 1.5 组件通信

### 1.5.1 `SendMessage(string methodName, object value)`

**【是什么】** 调用当前物体上**所有组件**里同名的方法。

**【⚠️ 不推荐】** 用字符串传递，编辑期无法检查、性能差、还可能炸错消息。找不到接收者时默认会报 `MissingMethodException`（错误级）；传 `SendMessageOptions.DontRequireReceiver` 才可避免报错。

**【代码示例】**
```csharp
gameObject.SendMessage("OnHit", 10);           // 调用所有组件的 OnHit(int)
gameObject.SendMessage("OnHit", 10, SendMessageOptions.DontRequireReceiver);
```

### 1.5.2 `BroadcastMessage` / `SendMessageUpwards`

- `BroadcastMessage`：调用**自身及所有子物体**的该名方法。
- `SendMessageUpwards`：往**父级方向（含自身）**找该方法。

**相似区别**：都是"消息式"，用字符串；都不如下面的方案替代：
- **方案1（推荐）**：`GetComponent` + 直接调用；
- **方案2**：UnityEvent / C# 事件 / 委托回调。

---

## 1.6 本章高频「相似 API」对比总表

| 想做的事 | 用哪个 | 别用哪个/注意 |
|---------|--------|--------------|
| 找物体 | `Transform.Find` (速度) | `GameObject.Find` 每帧慎用 |
| 找组件 | `GetComponent<T>` | `SendMessage` 字符串消息（性能/安全） |
| 隐藏物体 | `SetActive(false)` | `Destroy`（那个是销毁） |
| 关闭脚本 | `enabled=false` | `SetActive(false)` 会关掉整个物体 |
| 跨场景存活 | `DontDestroyOnLoad` | 不藏的话切场景即销毁 |
| 判断对象销毁 | `obj == null`（**用==**） | 别用 C# 的 `ReferenceEquals` |
| 销毁 | `Destroy`（运行时） | `DestroyImmediate`（编辑器） |

---

## 1.7 版本标记（本章涉及的版本化 API）

| API | 标记 |
|-----|------|
| `Object.FindObjectsByType` / `FindFirstObjectByType` | ✅ Unity 2021.3.18 引入（新查找 API，替代旧的数组/单例 `FindObject*` 系列，后者在 Unity 2023.1 起弃用） |
| `Object.Destroy` | 老 API（稳定可用） |
| `Input`（旧） | ⚠️ 旧，推荐 `Input System` |
| `Resources.Load` | ⚠️ 仍可用但推荐 `Addressables` |

> 具体版本细节以官方手册为准，这里只给推理方向。

---

## 1.8 本章小结

- `GameObject`= 容器，`Component`= 能力，`MonoBehaviour`= 你的逻辑脚本。
- `Object` 是 Unity 对象根类，注意 `== ` 的空引用陷阱。
- 生命周期：`Awake→OnEnable→Start → (Fixed/Update/Late) → OnDisable→OnDestroy`。
- 组件通信首选 `GetComponent` + 直接调用，慎用 SendMessage。
- 用 `SetActive(false)` 停用、`Destroy` 销毁、`DontDestroyOnLoad` 跨场景保存。

---

﻿# 第 2 章 Transform 与坐标系

> **本章管辖**：位置/旋转/缩放 → `Transform`，以及世界坐标 vs 本地坐标、父子层级、坐标转换。
> **一句话**：**每个** GameObject 身上都必挂一个 `Transform`（第一个组件，无法删除），它决定了物体在哪、朝哪、多大。
> **前置**：了解 [第 1 章](第01章_核心物件体系.md) 的 GameObject/Component。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 移动物体 | `transform.position` 直接赋值 / `Translate` (增量) |
| 绕轴转 | `transform.Rotate` / `transform.rotation` |
| 缩放大/小 | `transform.localScale` / `Scale` |
| 面向某点/某方向 | `LookAt` / `LookRotation` |
| 设父子关系 | `SetParent` / `parent` |
| 按路径找子物体 | `transform.Find(path)` |
| 世界坐标↔本地坐标 | `TransformPoint` / `InverseTransformPoint` |
| 方向转换 | `TransformDirection` / `InverseTransformDirection` |
| 非等比缩放的安全置换 | `TransformVector` |

---

## 2.1 `class Transform : Component`

**【是什么】** Unity 最常用的组件，存储**局部**位置/旋转/缩放，并维护与父物体的层级关系。**每个 GameObject 有且仅有一个 Transform，不能删除。**

**【用途】** 你操作物体的位置、旋转、缩放；处理父子关系；在局部/世界坐标系之间换算。

**【核心属性】**

| 属性 | 含义 | 世界 or 局部 |
|------|------|-------------|
| `position` | 世界坐标位置 | **世界** |
| `localPosition` | 相对父物体的位置 | **局部** |
| `rotation` | 世界旋转（四元数） | **世界** |
| `localRotation` | 相对父旋转 | **局部** |
| `localScale` | 相对父缩放 | **局部** |
| `eulerAngles` | 旋转（欧拉角常见于开销小但易万向锁） | 世界 |
| `localEulerAngles` | 局部欧拉角 | 局部 |

【⚠️ 注意】**为什么不要直接用 `gameObject.transform.position.x = 5`**：因为 `position` 返回的是 `Vector3`（值类型），直接改 `.x` 是改一个临时副本，不会生效。**务必整体赋值：**

```csharp
// ❌ 编译错误！CS1612「无法修改非变量表达式返回值」
transform.position.x = 5;            // position 是值类型（Vector3），.x 改的是临时副本，编译都不让过

// ✅
Vector3 p = transform.position;      
p.x = 5;
transform.position = p;               // 整体赋值才生效

// ✅ 或一行
transform.position += new Vector3(5, 0, 0);
```

---

## 2.2 位置相关 API

### 2.2.1 `position` / `localPosition`

```csharp
transform.position    // Vector3：世界坐标（无父=等于 localPosition）
transform.localPosition // Vector3：相对父
```
- **世界 vs 局部**：`localPosition` 是相对父物体的坐标；`position` 是"绝对的世界坐标"。二者在**没有父物体**时相等。
- 用**局部**操作子物体（游戏里摆子物品），用**世界**做全局运算（如角色在世界中移动）。

---

### 2.2.2 `Translate(Vector3 translation, Space relativeTo = Space.Self)`

**【是什么】** 沿指定方向移动物体（原地平移）。

**【参数说明】**
- `translation`：位移量（每帧一次的增量）。
- `relativeTo`：`Space.Self`（本地，沿自身轴）或 `Space.World`（世界轴）。

**【代码示例】**
```csharp
transform.Translate(0, 0, speed * Time.deltaTime);                 // 沿自身 Z 轴前进
transform.Translate(speed, 0, 0, Space.World);                     // 沿世界 X 轴
transform.Translate(Vector3.forward * speed * Time.deltaTime);     // 向前
```

【⚠️ 注意】真正物理移动用 `Rigidbody`（见第 5 章），不要用 `Transform.Translate` 直接操作有 `Rigidbody` 的物体。

---

## 2.3 旋转相关 API

### 2.3.1 `rotation` / `localRotation`（Quaternion 四元数）

```csharp
transform.rotation = Quaternion.Euler(0, 90, 0);   // ✅ 转 90 度常用写法
```

- `rotation`：世界旋转；`localRotation`：局部。
- 赋值用 **Quaternion**（不能直接赋 Vector3！）。

---

### 2.3.2 `Rotate(Vector3 eulers, Space relativeTo = Space.Self)`

**【是什么】** 相对旋转（每帧增量旋转）。
```csharp
// Rotate(Vector3.up, 90*dt)：第一个参数是旋转轴指示器，第二是角度（度）
// 不传 Space 时默认 Space.Self，绕**自身本地 Y 轴**（不是世界 Y 轴）
transform.Rotate(Vector3.up, 90 * Time.deltaTime);               // 绕本地Y轴每秒90度
transform.Rotate(0, 10, 0, Space.Self);                          // 绕自身y轴
transform.Rotate(0, 10, 0, Space.World);                         // 绕世界y轴
```

> 轴重载说明：`Rotate(Vector3 axis, float angle, Space relativeTo)` 才是"绕指定轴转过 angle 度"。示例里 `Vector3.up` 作为 axis 传入，在 `Space.Self` 下绕的是当地 y 轴。

【面试常考】`Rotate(归一化的轴, 角度)`：第一个参数是旋转轴向量，第二个是角度（度）。

---

### 2.3.3 `LookAt(Transform/Vector3 target, Vector3 up = Vector3.up)`

**【是什么】** 让物体**的 Z 轴朝向目标**（常用于敌人面向玩家、摄像头看目标）。
```csharp
transform.LookAt(player.transform);
```

**【参数】** target：目标（世界位置或物体）；可选 `up`：保持向上的轴向。

---

### 2.3.4 `Quaternion.LookRotation(Vector3 forward, Vector3 upwards)`（静态）

**【是什么】** 用两个方向向量生成一个"面向该方向的旋转四元数"。与 `transform.LookAt` 类似但不改自己，而是返回值让你赋值/叠加。
```csharp
Vector3 dir = targetPos - transform.position;
Quaternion rot = Quaternion.LookRotation(dir);
transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5);
```

**相似区别：**
| | `LookAt` | `LookRotation` |
|--|----------|----------------|
| 作用 | 直接让物体转向目标 | 返回一个四元数，你来赋值 |
| 用感 | 一步转向 | 可配合 Slerp 平滑转向 |

---

## 2.4 缩放

### 2.4.1 `localScale (Vector3)`

```csharp
transform.localScale = new Vector3(2, 2, 2);    // 放大2倍
```
【⚠️ 注意】只有 `localScale`，没有 `scale` 属性。父缩放会被继承，局部缩放是"相对父"的。

---

## 2.5 父子关系

### 2.5.1 `parent` 属性 & `SetParent(Transform parent, bool worldPositionStays = true)`

```csharp
transform.SetParent(parentGo.transform);              // 同理但是否保留世界位置由参数决定
transform.parent = parentGo.transform;               // 直接赋值（较少用）

// 挂载时保持世界位置：
transform.SetParent(parent, true);    // 世界位置不变（默认）
transform.SetParent(parent, false);   // 局部位置不变
```

**【参数说明】**
- `parent`：新的父物体 Transform。
- `worldPositionStays`（默认 `true`）：若为 true，**世界坐标保持不变**（只是把父子关系挂上）；若 false，**局部坐标不变**（物体会随父移动改变世界位置）。

【⚠️ 注意】`SetParent(null)` 会脱离父物体，成为根级物体。

---

### 2.5.2 `Find(string path)` — 按路径找子物体

```csharp
Transform child = transform.Find("Body/Head");    // 相对本物体找子物体，支持斜杠路径
```

**【返回】** 找到则返回子 `Transform`；没有返回 `null`。
**【区别】** `transform.Find` 找**子物体**，速度快；`GameObject.Find` 找整个场景，慢。

---

### 2.5.3 `childCount` & `GetChild(int i)`

```csharp
int n = transform.childCount;                    // 子物体数量
Transform first = transform.GetChild(0);         // 第0个子物体
for(int i=0;i<transform.childCount;i++)
{
    Transform c = transform.GetChild(i);
    // ...
}
```

---

## 2.6 坐标/方向转换（世界 ↔ 局部）

这些方法把"点/向量/方向"在**物体自身坐标系**与**世界坐标系**之间做变换。最容易混的一堆，务必集中记忆：

| 方法 | 含义（局部→世界 或 世界→局部） | 典型用途 |
|------|--------------------------------|----------|
| `TransformPoint(Vector3 local)` | 局部坐标点 → 世界坐标点 | 拿物体局部某个点在世界里的位置 |
| `InverseTransformPoint(Vector3 world)` | 世界 → 局部点 | 把世界坐标变成"相对物体"的坐标 |
| `TransformDirection` | 局部**方向** → 世界（不缩放） | 局部前进方向转世界 |
| `InverseTransformDirection` | 世界方向 → 局部方向 |  |
| `TransformVector` | 局部**带缩放的向量** → 世界 | 带缩放的位移转换 |
| `InverseTransformVector` | 世界带缩放向量 → 局部 |  |

**【代码示例：把局部点转世界】**
```csharp
// 物体的局部坐标 (1,0,0) 在世界坐标系中的位置
Vector3 worldPos = transform.TransformPoint(new Vector3(1,0,0));
// 鼠标屏幕的世界点转换到局部（常用于在物体上点击检测）
Vector3 local = transform.InverseTransformPoint(worldClickPos);
```

**【相似区别：TransDirection vs TransformVector】**
- `TransformDirection`：只转换方向（忽略缩放、偏移），不改变向量长度。
- `TransformVector`：转换"带距离的向量"（考虑缩放），保「矢量真实大小」。

---

## 2.7 其他常用函数

- `DetachChildren()`：把所有子物体变成根级（取消父关系）。
- `SetAsFirstSibling()/SetAsLastSibling()/SetSiblingIndex()`：调整兄弟顺序（UI/渲染顺序相关）。

---

## 2.8 高频对比总表

| 想做的事 | 用这个 | 备注 |
|---------|--------|------|
| 世界位置 | `transform.position` | 没有直接 `.x=` 谨慎 |
| 局部位置 | `transform.localPosition` | 有父时和 position 不同 |
| 世界旋转 | `transform.rotation` | 四元数 |
| 局部旋转 | `transform.localRotation` |  |
| 可直接看的角度 | `transform.eulerAngles` | 有万向锁风险 |
| 平移 | `Translate` | 物理的可移动用 Rigidbody |
| 指向目标 | `LookAt`（自身转向） | 要旋转四元数用 `Quaternion.LookRotation` |
| 设定父层 | `SetParent` | `parent` 直接赋值 |

> 下一章接上：第 3 章会详解这里用到的 `Vector3` `Quaternion` `Euler` 这些数学类型。

---

# 第 3 章 数学与向量

> **本章管辖**：游戏里最常用的数学类型 — `Vector2`、`Vector3`、`Vector4`、`Quaternion`（四元数）、`Mathf`（Math 的浮点版）、欧拉角。
> **一句话**：没有数学你几乎什么也做不了（移动、旋转、朝向、距离、插值都靠它们）。Unity 帮你封装成好用的静态方法。
> **前置**：第 2 章的 Transform 用到的数学都在这里。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 定义 x/y/(z) 坐标 | `new Vector3(x, y, z)` |
| 两点的距离 | `Vector3.Distance(a, b)` |
| 两点方向 | `b - a`（得到向量） |
| 单位方向（归一化） | `Vector3.Normalize` / `.(direction).normalized` |
| 朝向某处的旋转 | `Quaternion.LookRotation(dir)` |
| 两角平滑过渡 | `Quaternion.Slerp(from, to, t)` |
| 位置间平滑过渡 | `Vector3.Lerp/ MoveTowards / Slerp` |
| 限制在范围内 | `Mathf.Clamp` / `Mathf.Clamp01` |
| 求角度转弧度 | `Mathf.Deg2Rad` / `Rad2Deg` |
| 两点之间某个分点 | `Vector3.Lerp` |
| 得到"朝向角" | `Mathf.Atan2` |

---

## 3.1 Vector2 / Vector3 / Vector4（向量结构体）

**【是什么】** 数学上"有大小有方向"的量，在 Unity 里是 `struct`（值类型），存储多个 float 分量。
- `Vector2(x, y)`：二维（UI、屏幕）
- `Vector3(x, y, z)`：三维（物体位置、方向、**世界坐标**）
- `Vector4(x, y, z, w)`：四维（一般少直接用来存坐标；用于 RGBA 颜色、齐次坐标等）

**【名称含义】** Vector = 向量/矢量。

---

### 3.1.1 `Vector3` 字段 & 常量

```csharp
Vector3 up = Vector3.up;        // (0,1,0) 世界正上
Vector3 down = Vector3.down;    // (0,-1,0)
Vector3 left = Vector3.left;    // (-1,0,0)
Vector3 right = Vector3.right;  // (1,0,0)
Vector3 forward = Vector3.forward; // (0,0,1)
Vector3 back = Vector3.back;    // (0,0,-1)
Vector3 zero = Vector3.zero;    // (0,0,0)
Vector3 one = Vector3.one;      // (1,1,1)
```

---

### 3.1.2 向量运算（加减、乘除、点积、叉积、模长）

```csharp
Vector3 a = new Vector3(1,2,3);
Vector3 b = new Vector3(4,5,6);

Vector3 sum = a + b;        // (5,7,9) 加
Vector3 diff = a - b;        // (-3,-3,-3) 减（结果的方向：从 b 指向 a）
Vector3 scaled = a * 2f;     // 数乘放大
Vector3 scaled2 = a * 0.5f;  // 缩小

float mag = a.magnitude;     // 向量长度（模） float
Vector3 n = a.normalized;    // 单位向量（长度为1的方向）——**不要改a本身**
float dot = Vector3.Dot(a, b);        // 点积 (标量)
Vector3 cross = Vector3.Cross(a, b);  // 叉积 (向量，垂直于a和b)
```

**【用途】**：
- 减法 `b - a` = 从 a 指向 b 的**方向向量**。
- `normalized`：只留方向，保留单位长度 → 用于**越来越走1单位/秒的移动**。
- `Dot` 判断朝向（同向>0、垂直=0、反向<0）。
- `Cross` 求垂直线轴。

**【相似区别】** `.magnitude`（长度） vs `.sqrMagnitude`（长度平方）：
- 比较距离时用 `sqrMagnitude` 更快（省开方）。
```csharp
float d1 = (a - b).magnitude;         // 精确距离
float d2 = (a - b).sqrMagnitude;      // 无开方的平方距离，比较用更快
if (d2 < 4f) { }   // 等于距离<2
```

---

### 3.1.3 `Vector3.Distance(a, b)`（静态）

```csharp
float d = Vector3.Distance(transform.position, target.position);
```
【返回】两点间直线距离。

---

### 3.1.4 归一化：`.normalized` 与 `Vector3.Normalize`

- `v.normalized`：返回**新**的单位向量（**不改变原向量**，不报错）。
- `Vector3.Normalize(v)`（静态）：传入的 `v` 作为**参数**，返回一个新的单位向量，**不修改原向量**。
- `v.Normalize()`（实例方法）：原地规范化，修改 v 本身，返回 void。两种要分清。
```csharp
Vector3 dir = (target - transform.position).normalized;   // ✅ 常用：单位方向

Vector3 n1 = Vector3.Normalize(v);   // 静态：不改 v，返回新向量
v.Normalize();                        // 实例：直接改 v（v 变单位向量）
```

---

## 3.2 插值（Lerp / MoveTowards / Slerp）—— 平滑移动的关键

### 3.2.1 `Vector3.Lerp(Vector3 a, Vector3 b, float t)`

**【是什么】** 线性插值：在 a 和 b 之间按 `t`（0~1）取一个中间点。`t=0` 返回 a，`t=1` 返回 b。

```csharp
Vector3 mid = Vector3.Lerp(start, end, 0.5f);   // 中点
// 平滑跟随（帧独立见 6 章 Time.deltaTime）
transform.position = Vector3.Lerp(transform.position, target, 0.1f);
```

**【面试常考】** `t` 不该是固定的 `0.1`，而应结合 `Time.deltaTime` 才能稳定，或用 `.MoveTowards`。用固定 0.1 会在低帧率下"移动太慢、永远追不上" → 正确：
```csharp
transform.position = Vector3.Lerp(transform.position, target,
                     1 - Mathf.Pow(0.95f, Time.deltaTime * 60f));
```

---

### 3.2.2 `Vector3.MoveTowards(current, target, maxDistanceDelta)`

**【是什么】** 以**恒定速度**向目标移动，**每步最多走 maxDistanceDelta**，不会 overshoot。
- 与 `Lerp` 不同：`MoveTowards` 是匀速，`Lerp` 是指数渐近(越近越慢)。

```csharp
transform.position = Vector3.MoveTowards(transform.position,
                      target.position, speed * Time.deltaTime);
```

---

### 3.2.3 `Vector3.Slerp(a, b, t)`（球面插值）

- 沿球面上的插值，得到**弯曲的路径**，适合圆弧移动、让运动更自然（转圈）。

---

**对比总表：**
| 方法 | 行为 | t 是 | 速度 | 适用 |
|------|------|------|------|------|
| `Lerp` | 线性插值 | 0~1 | 渐近 | 平滑跟随 |
| `MoveTowards` | 匀速移动 | 步距 | 恒定 | 追逐/移动 |
| `Slerp` | 球面（曲线）| 0~1 | 渐近 | 圆弧/旋转轨迹 |

---

## 3.3 Quaternion（四元数）—— 旋转的底层存储

### 3.3.1 `struct Quaternion`

**【是什么】** 旋转的推荐存储形式（四元数）。四个分量 `(x,y,z,w)` 通常**不要手动改**，而是通过方法生成/变换。避免欧拉角的**万向锁**问题。

**【核心静态方法与属性】**

| API | 含义 |
|-----|------|
| `Quaternion.Euler(x, y, z)` | 从欧拉角（度）创建旋转 |
| `Quaternion.LookRotation(forward)` | 让 Z 朝向 `forward` 的旋转 |
| `Quaternion.Slerp(q1, q2, t)` | 两个旋转间平滑插值 |
| `Quaternion.Lerp` | 线性四元数插值 |
| `Quaternion.identity`（属性，小写） | 无旋转的单位四元数 `(0,0,0,1)` |
| `Quaternion.Angle(a, b)` | 两旋转夹角（度） |
| `Quaternion * Vector3` | 把向量按该旋转旋转 |
| `Quaternion.Inverse(q)` | 反旋转（求逆，等价反向旋转；**不是** `Conjugate`——后者不是公开常用 API） |

**【代码示例：常用的组合】**
```csharp
// 1. 用欧拉角给物体旋转
transform.rotation = Quaternion.Euler(0, yaw, 0);

// 2. 两个目标间平滑转向
Quaternion targetRot = Quaternion.LookRotation(direction);
transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);

// 3. 旋转一个方向向量
Vector3 rotated = rotation * Vector3.forward;   // 把 forward 按当前旋转转出来
```

【⚠️ 注意】**欧拉角 `eulerAngles` 与四元数 `rotation` 是同一个旋转的两种表示**。多次叠加欧拉角容易触发万向锁，旋转尽量用四元数。

---

### 3.3.2 `Quaternion * Vector3`：把方向旋转

最常见的"让物体面向/按本地轴"：
```csharp
// 把"本地的forward"变到世界方向
Vector3 worldForward = transform.rotation * Vector3.forward;
```

---

## 3.4 Mathf（Math 函数的 float 版本）

### 3.4.1 `class Mathf`

- Unity 提供的一堆 **float** 数学函数（C# 的 `Math` 是 double，Unity 常用 `Mathf` 因为 float 更快）。
- 全部是**静态**方法。

| Mathf | 含义 |
|-------|------|
| `Abs(x)` | 绝对值 |
| `Clamp(v, min, max)` | 限制在范围 |
| `Clamp01(v)` | 限制在 [0,1] |
| `Lerp(a, b, t)` | float 线性插值 |
| `LerpUnclamped(a,b,t)` | 不夹紧 t 的插值 |
| `MoveTowards` | 移动方向 |
| `Repeat` / `PingPong` | 周期值 |
| `Max/Min` | 最大/最小 |
| `Pow` | 幂 |
| `Sqrt` | 开方 |
| `Sign` | 符号（1或-1） |
| `Floor/Ceil/Round` | 下取整/上取整/四舍五入 |
| `Infinity/NegativeInfinity` | 正负无穷用于比较 |

```csharp
float hp = Mathf.Clamp(currentHp, 0f, 100f);   // 修正范围
int sign = Mathf.Sign(speed);                   // 方向
float t = Mathf.Lerp(0f, 1f, progress);          // 值插值
float spd = Mathf.MoveTowards(speed, targetSpeed, accel * Time.deltaTime);
```

---

### 3.4.2 角度与弧度：`Deg2Rad` / `Rad2Deg` / `Atan2`

```csharp
float rad = 90f * Mathf.Deg2Rad;      // 角度→弧度 (π/2)
float deg = 1.5708f * Mathf.Rad2Deg;  // 弧度→角度 (90)

// 由 x,y 求夹角（常用于"让物体朝向鼠标/目标"）
float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;  // 弧度→度
```
【面试常考】`Atan2(dy, dx)`：第一参数是 y，第二是 x；返回弧度。

---

## 3.5 向量 to 方向角（高频组合）

**让 2D 物体"面向目标"（即便已顺，绕自身 z 轴）：**
```csharp
// 2D：物体朝鼠标
Vector3 dir = mouseWorldPos - transform.position;
float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
transform.rotation = Quaternion.Euler(0, 0, angle);
```

---

## 3.6 本章高频对比总表

| 想做什么 | Vector 方法 | Mathf 方法 |
|---------|-------------|-----------|
| 两点距离 | `Vector3.Distance` | 无 |
| 线性插值 | `Vector3.Lerp` | `Mathf.Lerp` (标量) |
| 恒定速度到达 | `Vector3.MoveTowards` | `Mathf.MoveTowards` (标量) |
| 限制范围 | `Vector3.ClampMagnitude` | `Mathf.Clamp` |
| 平滑旋转 | `Quaternion.Slerp` | — |

---

## 3.7 版本标记

- `Vector3.Slerp`、`Quaternion` 均为长期稳定 API。
- 底部 `Mathf.Clamp` 长期稳定。
- 新版：`Unity. 2021+ `Vector3` 到 `Mathf` 无变化，可放心用。

---

## 3.8 小结

- `Vector3` 描述位置/方向；`Lerp/MoveTowards/Slerp` 是平滑移动三件套。
- `Quaternion.Euler/LookRotation/Slerp` 处理旋转；别自己改 4 元组。
- `Mathf` 提供浮点数学：`Clamp/Lerp/MoveTowards/Atan2`。
- 判断朝向用 `Dot`，取垂直用 `Cross`，求距离用 `magnitude` 或更快的 `sqrMagnitude`。

---

# 第 4 章 输入系统

> **本章管辖**：Unity 里"怎么读玩家的手"——键盘、鼠标、触摸、手柄、加速度计。核心是**两代 API**：老版 `UnityEngine.Input` 类，与新版 `Input System` 包。
> **一句话**：老 `Input` 简单直接但绑死"每帧轮询"；新 `Input System` 灵活、可配置、支持手柄和触摸，是 2019.1+ 新项目的官方推荐。
> **前置**：建议先看 [README](../README.md) 了解词条模板，以及第 1 章 `MonoBehaviour` 生命周期（输入读取常在 `Update` 里做）。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 读键盘按键（按住/按下/抬起） | `Input.GetKey` / `GetKeyDown` / `GetKeyUp`（旧） |
| 读鼠标按键 | `Input.GetMouseButton` / `GetMouseButtonDown` / `GetMouseButtonUp`（旧） |
| 拿鼠标在屏幕上的位置 | `Input.mousePosition`（旧） |
| 读一个"轴"（如 WASD 水平/垂直） | `Input.GetAxis` / `GetAxisRaw`（旧） |
| 读虚拟按钮（如 Jump） | `Input.GetButton` / `GetButtonDown` / `GetButtonUp`（旧） |
| 读触摸屏触摸 | `Input.touchCount` / `Input.GetTouch`（旧） |
| 读手机加速度计 | `Input.acceleration`（旧） |
| 新项目统一读输入 | 用 **Input System 包**（`UnityEngine.InputSystem`） |
| 用组件方式接收输入 | `PlayerInput` 组件 |
| 定义一个"动作"（如移动/跳跃） | `InputAction` 类 |
| 把动作分组管理 | `InputActionMap` |
| 直接读键盘当前状态 | `Keyboard.current`（新） |
| 直接读鼠标当前状态 | `Mouse.current`（新） |
| 直接读手柄当前状态 | `Gamepad.current`（新） |
| 监听动作触发（按下/持续/抬起） | Action Callbacks：`started` / `performed` / `canceled` |
| 把按键映射到动作 | `Binding`（绑定） |
| 在回调里拿输入数据 | `InputAction.CallbackContext` |

---

## 4.1 两代输入系统总览（先看这个）

### 4.1.1 为什么有两套？

Unity 早期只有一套 `UnityEngine.Input` 静态类，简单但**写死**：按键名是字符串、每帧只能轮询、不支持异步、编辑器里改绑定要改代码。从 **Unity 2019.1** 起官方推出 **Input System 包**（`UnityEngine.InputSystem`），作为新一代输入方案。

| 维度 | 老 `Input` 类 | 新 `Input System` 包 |
|------|--------------|---------------------|
| 命名空间 | `UnityEngine` | `UnityEngine.InputSystem` |
| 引入时间 | 远古就有 | ✅ Unity 2019.1+（包） |
| 读取方式 | 每帧轮询（`Update` 里查） | 事件回调 + 轮询都支持 |
| 按键绑定 | 字符串写死（`"Jump"`） | 可视化 Binding，可改可存 |
| 触摸/手柄 | 支持但简陋 | 统一、完善 |
| 性能 | 每帧全量查询 | 按需、可裁剪 |
| 新项目推荐 | ❌ 不推荐 | ✅ 官方推荐 |

> 【⚠️ 注意】老 `Input` 类**不会消失**，仍可用。但新项目、尤其是要上手机/手柄/多平台的，强烈建议用 Input System。

### 4.1.2 如何启用 Input System（关键步骤）

新包默认**不启用**，需要手动打开：

1. 菜单 `Window → Package Manager`，搜索并安装 **Input System** 包。
2. 菜单 `Edit → Project Settings → Player → Other Settings → Active Input Handling`。
3. 选 `**Input Manager (Old)**`（只用老）、`**Input System Package (New)**`（只用新）、或 `**Both**`（两套共存，推荐过渡期）。
4. 改完会提示**重启编辑器**。

> 【⚠️ 注意】选 `Both` 时两套 API 都能用，但会有轻微性能开销。选 `New` 后老 `Input` 类会**报错不可用**。

---

## 4.2 老版输入：`UnityEngine.Input` 类

> 【版本标记】⚠️ **老 API**（Unity 2019.1 前主流）。新项目推荐用 Input System 包。以下词条仍可用，但请优先看 4.3 节。

### 4.2.1 `Input.GetKey` / `GetKeyDown` / `GetKeyUp`

**【是什么】** 三个静态方法，轮询键盘按键状态。

```csharp
public static bool GetKey(KeyCode key);        // 按住期间每帧 true
public static bool GetKeyDown(KeyCode key);     // 按下那一帧 true（只一帧）
public static bool GetKeyUp(KeyCode key);       // 松开那一帧 true（只一帧）
```

**【用途】** 读键盘输入。`GetKey` 适合"持续按住"（如移动），`GetKeyDown` 适合"按一下触发一次"（如跳跃、开枪）。

**【名称含义】** `Get`（获取）+ `Key`（按键）+ `Down`（按下）/`Up`（抬起）。

**【参数说明】** `key`：`KeyCode` 枚举，如 `KeyCode.W`、`KeyCode.Space`、`KeyCode.LeftArrow`。

**【返回值】** `bool`：`true` 表示满足条件。

**【代码示例】**
```csharp
void Update()
{
    // 持续按住 W 前进
    if (Input.GetKey(KeyCode.W)) transform.Translate(Vector3.forward * Time.deltaTime);

    // 刚按下空格触发跳跃（只触发一次）
    if (Input.GetKeyDown(KeyCode.Space)) Jump();

    // 松开时打印
    if (Input.GetKeyUp(KeyCode.Space)) Debug.Log("松开了空格");
}
```

**【相似 API 区别】**
| API | 触发时机 |
|-----|---------|
| `GetKey` | 按住期间**每帧** true |
| `GetKeyDown` | 仅**按下那一帧** true |
| `GetKeyUp` | 仅**松开那一帧** true |

> 【C++/Raylib 类比】Raylib 的 `IsKeyDown`/`IsKeyPressed`/`IsKeyReleased` 与这三者一一对应。

---

### 4.2.2 `Input.GetMouseButton` / `GetMouseButtonDown` / `GetMouseButtonUp`

**：** 轮询鼠标按键。

```csharp
public static bool GetMouseButton(int button);
public static bool GetMouseButtonDown(int button);
public static bool GetMouseButtonUp(int button);
```

**【参数说明】** `button`：`0`=左键，`1`=右键，`2`=中键。

**【代码示例】**
```csharp
void Update()
{
    if (Input.GetMouseButtonDown(0)) Debug.Log("左键按下");   // 射击/选中
    if (Input.GetMouseButton(1)) Debug.Log("右键按住");        // 持续瞄准
    if (Input.GetMouseButtonUp(2)) Debug.Log("中键松开");
}
```

**【相似 API 区别】** 与 `GetKey` 系列完全同构，只是参数从 `KeyCode` 换成 `int` 鼠标键号。

---

### 4.2.3 `Input.mousePosition`

**【是什么】** 鼠标在**屏幕坐标**中的位置（只读属性）。

```csharp
public static Vector3 mousePosition { get; }
```

**【返回值】** `Vector3`：`x`/`y` 是屏幕像素坐标（左下角为原点），`z` 恒为 0。

**【用途】** 鼠标拾取、UI 跟随、瞄准线。

**【代码示例】**
```csharp
// 把鼠标位置转成世界坐标（配合摄像机）
Vector3 screenPos = Input.mousePosition;
screenPos.z = 10f; // 距摄像机距离
Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
```

> 【⚠️ 注意】`z` 恒为 0，做 `ScreenToWorldPoint` 前必须手动给 `z` 赋值，否则结果在摄像机位置。

---

### 4.2.4 `Input.GetAxis` / `GetAxisRaw`

**【是什么】** 读取一个"轴"（axis）的平滑值。轴在 `Project Settings → Input Manager` 里配置（如 `Horizontal`、`Vertical`）。

```csharp
public static float GetAxis(string axisName);
public static float GetAxisRaw(string axisName);
```

**【用途】** WASD/方向键移动、摇杆。`GetAxis` 返回**平滑过渡**的值（有加速度/减速度），`GetAxisRaw` 返回**原始**值（-1/0/1，无平滑）。

**【名称含义】** `Axis`（轴）+ `Raw`（原始的）。

**【参数说明】** `axisName`：轴名，内置有 `"Horizontal"`、`"Vertical"`、`"Mouse X"`、`"Mouse Y"` 等（注意：`"Fire1"` 等是**按钮**（Button）不是轴，默认情况下对它调 `GetAxis` 恒为 0——但若你在 Input Manager 里手动定义了同名轴，就会返回该轴的值，所以统一用 `GetButton` 更稳妥）。

**【返回值】** `float`：范围通常 `-1`（左/下）到 `1`（右/上）。

**【代码示例】**
```csharp
// WASD 移动（经典写法）
float h = Input.GetAxis("Horizontal");   // A/D 或 ←/→
float v = Input.GetAxis("Vertical");     // W/S 或 ↑/↓
Vector3 move = new Vector3(h, 0, v);
transform.Translate(move * speed * Time.deltaTime);

// 需要"立即响应"用 Raw（无平滑，适合精确控制）
float raw = Input.GetAxisRaw("Horizontal");
```

**【相似 API 区别】**
| API | 平滑 | 典型用途 |
|-----|------|---------|
| `GetAxis` | 有（带加减速） | 平滑移动、摇杆 |
| `GetAxisRaw` | 无（-1/0/1） | 精确、即时响应 |

> 【面试常考】`GetAxis` 有平滑过渡，`GetAxisRaw` 无平滑、直接返回 -1/0/1。

---

### 4.2.5 `Input.GetButton` / `GetButtonDown` / `GetButtonUp`

**【是什么】** 读取一个"按钮"（虚拟按键，在 Input Manager 里配置，如 `"Jump"`）。

```csharp
public static bool GetButton(string buttonName);
public static bool GetButtonDown(string buttonName);
public static bool GetButtonUp(string buttonName);
```

**【用途】** 读配置好的虚拟按钮，比直接写 `KeyCode` 更灵活（可换键）。

**【代码示例】**
```csharp
if (Input.GetButtonDown("Jump")) rb.AddForce(Vector3.up * jumpForce);
```

**【相似 API 区别】** `GetButton` 系列与 `GetKey` 系列行为一致，区别是 `GetButton` 用**字符串名**（可在 Input Manager 里改绑定），`GetKey` 用 `KeyCode` 枚举（写死）。

---

### 4.2.6 `Input.touchCount` / `Input.GetTouch`

**【是什么】** 读取触摸屏的触摸信息。

```csharp
public static int touchCount { get; }          // 当前触摸点数
public static Touch GetTouch(int index);       // 取第 index 个触摸
```

**【返回值】** `touchCount` 为 `int`；`GetTouch` 返回 `Touch` 结构体（含 `position`、`phase`、`deltaPosition` 等）。

**【用途】** 手机多点触控、滑动、缩放。

**【代码示例】**
```csharp
void Update()
{
    if (Input.touchCount > 0)
    {
        Touch t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began) Debug.Log("手指按下");
        if (t.phase == TouchPhase.Moved) Debug.Log("手指移动");
    }
}
```

> 【⚠️ 注意】`TouchPhase` 枚举：`Began`（按下）、`Moved`（移动）、`Stationary`（静止）、`Ended`（抬起）、`Canceled`（取消）。

---

### 4.2.7 `Input.acceleration`

**【是什么】** 读取手机加速度计（重力感应）数据。

```csharp
public static Vector3 acceleration { get; }
```

**【返回值】** `Vector3`：设备加速度，单位约 `g`（重力加速度）。静止时约 `(0, -1, 0)`（取决于设备朝向）。

**【用途】** 手机倾斜控制、摇一摇。

**【代码示例】**
```csharp
// 用手机倾斜控制物体左右移动
Vector3 acc = Input.acceleration;
transform.Translate(acc.x * speed * Time.deltaTime, 0, 0);
```

---

## 4.3 新版 Input System 包（`UnityEngine.InputSystem`）

> 【版本标记】✅ **Unity 2019.1+ 引入的新包**，官方推荐替代老 `Input`。需先安装包并启用（见 4.1.2）。

### 4.3.1 `PlayerInput` 组件

**【是什么】** 一个 `MonoBehaviour` 组件，把输入动作和游戏对象绑定起来，是 Input System 的"高层入口"。

**【用途】** 在 Inspector 里配置 `InputActionAsset`（动作资产），自动把动作事件转发给脚本里的方法。

**【名称含义】** `Player`（玩家）+ `Input`（输入）：代表"某个玩家"的输入。

**【代码示例】**（配合动作资产）
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // 由 PlayerInput 组件自动调用（方法名 = 动作名）
    public void OnMove(InputValue value)
    {
        Vector2 dir = value.Get<Vector2>();
        transform.Translate(dir * Time.deltaTime);
    }

    public void OnJump()
    {
        Debug.Log("跳！");
    }
}
```

> 【⚠️ 注意】`PlayerInput` 通过**方法名匹配**动作名（`OnMove` 对应动作 `Move`），方法签名可带 `InputValue` 参数。

---

### 4.3.2 `InputAction` 类

**【是什么】** 一个"动作"的抽象：把一组按键绑定映射成一个有意义的输入（如 `Move`、`Jump`）。

**【用途】** 定义"玩家想做什么"，而不是"玩家按了哪个键"。动作可被轮询或订阅回调。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class ActionDemo : MonoBehaviour
{
    public InputAction moveAction;   // 在 Inspector 里配置绑定

    void OnEnable()  { moveAction.Enable(); }
    void OnDisable() { moveAction.Disable(); }

    void Update()
    {
        Vector2 v = moveAction.ReadValue<Vector2>();   // 轮询读取
        transform.Translate(v * Time.deltaTime);
    }
}
```

**【相似 API 区别】**
| API | 角色 |
|-----|------|
| `InputAction` | 单个动作（一个语义） |
| `InputActionMap` | 一组动作的集合（如"移动组""UI 组"） |
| `InputActionAsset` | 整个动作资产文件（含多个 Map） |

---

### 4.3.3 `InputActionMap`

**【是什么】** 一组 `InputAction` 的容器，可整体启用/禁用。

**【用途】** 按"上下文"分组：游戏进行时启用 `Gameplay` 组，打开菜单时启用 `UI` 组、禁用 `Gameplay` 组。

**【代码示例】**
```csharp
using UnityEngine.InputSystem;

public InputActionMap gameplayMap;
public InputActionMap uiMap;

void OpenMenu()
{
    gameplayMap.Disable();   // 停用游戏操作
    uiMap.Enable();          // 启用 UI 操作
}
```

---

### 4.3.4 静态单例：`Keyboard.current` / `Mouse.current` / `Gamepad.current`

**【是什么】** 当前连接的键盘/鼠标/手柄的**静态单例**（`current` 属性）。

**【用途】** 直接、快速读设备状态，无需配置动作。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

void Update()
{
    // 键盘
    if (Keyboard.current.wKey.isPressed) moveForward();
    if (Keyboard.current.spaceKey.wasPressedThisFrame) jump();

    // 鼠标
    Vector2 mousePos = Mouse.current.position.ReadValue();
    if (Mouse.current.leftButton.wasPressedThisFrame) shoot();

    // 手柄
    if (Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame)
        Debug.Log("按了 A 键");
}
```

**【相似 API 区别】**
| 设备 | 静态单例 |
|------|---------|
| 键盘 | `Keyboard.current` |
| 鼠标 | `Mouse.current` |
| 手柄 | `Gamepad.current` |
| 触摸 | `Touchscreen.current` |

> 【⚠️ 注意】`current` 可能为 `null`（设备未连接），使用前判空。

---

### 4.3.5 Action Callbacks：`started` / `performed` / `canceled`

**【是什么】** `InputAction` 的三个事件回调，对应动作的三种状态变化。

| 回调 | 触发时机 |
|------|---------|
| `started` | 动作**开始**（如按键按下） |
| `performed` | 动作**完成/触发**（如按键按下并达到阈值） |
| `canceled` | 动作**取消/结束**（如按键松开） |

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class CallbackDemo : MonoBehaviour
{
    public InputAction jumpAction;

    void OnEnable()
    {
        jumpAction.Enable();
        jumpAction.started   += ctx => Debug.Log("开始");
        jumpAction.performed += ctx => Debug.Log("触发！跳！");
        jumpAction.canceled  += ctx => Debug.Log("取消");
    }
    void OnDisable() { jumpAction.Disable(); }
}
```

> 【面试常考】`performed` 是"动作真正生效"的时刻，`started` 是"开始"，`canceled` 是"结束"。对按钮类动作，`performed` 最常用。

---

### 4.3.6 `Binding`（绑定）

**【是什么】** 把动作映射到具体设备按键的配置（如 `Move` 绑定到 `WASD` 和左摇杆）。

**【用途】** 可视化配置按键、支持玩家自定义按键、多设备复用同一动作。

**【代码示例】**（代码里创建绑定）
```csharp
using UnityEngine.InputSystem;

var action = new InputAction("Jump", binding: "<Keyboard>/space");
action.AddBinding("<Gamepad>/buttonSouth");   // 再加一个手柄绑定
action.Enable();
```

> 【⚠️ 注意】绑定字符串格式：`<设备>/<控件>`，如 `<Keyboard>/w`、`<Mouse>/leftButton`、`<Gamepad>/buttonSouth`。

---

### 4.3.7 `InputAction.CallbackContext`

**【是什么】** 回调函数收到的参数，封装了本次动作触发的上下文信息。

**【用途】** 在回调里读取动作值、判断阶段、拿控件。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class CtxDemo : MonoBehaviour
{
    public InputAction moveAction;

    void OnEnable()
    {
        moveAction.Enable();
        moveAction.performed += OnMove;
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 v = ctx.ReadValue<Vector2>();   // 读动作值
        Debug.Log($"阶段={ctx.phase} 值={v}");
    }
}
```

**【常用成员】**
| 成员 | 说明 |
|------|------|
| `ctx.phase` | 当前阶段（`Started`/`Performed`/`Canceled`） |
| `ctx.ReadValue<T>()` | 读动作值（`Vector2`/`float`/`bool`…） |
| `ctx.control` | 触发动作的具体控件 |
| `ctx.action` | 对应的 `InputAction` |

---

## 4.4 旧版 vs 新版彻底对比

### 4.4.1 何时用哪个

| 场景 | 推荐 |
|------|------|
| 老项目、快速原型、只想简单读键 | 老 `Input`（省事） |
| 新项目、多平台、手柄/触摸/自定义按键 | **Input System** |
| 需要异步/事件驱动 | **Input System** |
| 需要可视化绑定、玩家改键 | **Input System** |

### 4.4.2 性能与编辑器绑定区别

| 维度 | 老 `Input` | 新 `Input System` |
|------|-----------|-------------------|
| 性能 | 每帧全量轮询，开销固定 | 按需订阅，可裁剪，更省 |
| 编辑器绑定 | 字符串写死，改键要改代码 | 可视化 Binding，可改可重 |
| 多设备 | 支持有限 | 统一抽象，天然支持 |
| 异步/事件 | 无 | 有（回调） |
| 触摸 | 简陋 | 完善 |

### 4.4.3 对应关系速查

| 老 API | 新 API |
|--------|--------|
| `Input.GetKey(KeyCode.W)` | `Keyboard.current.wKey` |
| `Input.GetMouseButton(0)` | `Mouse.current.leftButton` |
| `Input.GetAxis("Horizontal")` | `InputAction` + `ReadValue<Vector2>()` |
| `Input.GetButtonDown("Jump")` | `InputAction.performed` 回调 |
| `Input.mousePosition` | `Mouse.current.position` |
| `Input.touchCount` | `Touchscreen.current.touches` |

---

## 4.5 触摸输入（两版对比）

### 4.5.1 老版触摸（`Input.GetTouch`）

```csharp
void Update()
{
    if (Input.touchCount > 0)
    {
        Touch t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began) Debug.Log("按下");
    }
}
```

### 4.5.2 新版触摸（`Touchscreen.current`）

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

void Update()
{
    var touch = Touchscreen.current;
    if (touch == null) return;   // 无触摸屏

    if (touch.primaryTouch.press.isPressed)
    {
        Vector2 pos = touch.primaryTouch.position.ReadValue();
        Debug.Log($"触摸位置：{pos}");
    }
}
```

**【相似 API 区别】**
| 维度 | 老 `Input.GetTouch` | 新 `Touchscreen.current` |
|------|--------------------|--------------------------|
| 命名空间 | `UnityEngine` | `UnityEngine.InputSystem` |
| 读取 | 轮询 `touchCount` | 事件 + 轮询 |
| 多指 | `GetTouch(i)` | `touchs` 数组 / `primaryTouch` |

---

## 4.6 本章高频「相似 API」对比总表

| 想做的事 | 用哪个 | 别用哪个/注意 |
|---------|--------|--------------|
| 读按键 | `Input.GetKey`（旧）/ `Keyboard.current`（新） | 新项目别用老 `Input` |
| 读轴 | `Input.GetAxis`（平滑）/ `GetAxisRaw`（即时） | 别混用，看需求 |
| 读鼠标 | `Input.GetMouseButton` / `Mouse.current` | 注意 `mousePosition.z` 恒 0 |
| 读触摸 | `Input.GetTouch`（旧）/ `Touchscreen.current`（新） | 新项目用新 |
| 事件驱动 | `InputAction.performed`（新） | 老 `Input` 无事件 |
| 自定义按键 | `InputAction` + Binding（新） | 老 `Input` 写死 |

---

## 4.7 版本标记（本章涉及的版本化 API）

| API | 标记 |
|-----|------|
| `UnityEngine.Input`（`GetKey`/`GetAxis`/`GetTouch`…） | ⚠️ 老 API（2019.1 前主流），新项目不推荐 |
| `UnityEngine.InputSystem`（`PlayerInput`/`InputAction`/`Keyboard.current`…） | ✅ Unity 2019.1+ 引入，官方推荐 |
| `Active Input Handling` 设置 | 需在 Project Settings 手动启用（Both/New） |

> 具体版本细节以官方手册为准，这里只给推理方向。

---

## 4.8 本章小结

- 输入有两代：老 `Input` 类（轮询、写死）与新 `Input System` 包（事件、可配置）。
- 新项目**优先用 Input System**，需在 `Project Settings → Player → Active Input Handling` 启用。
- 老 `Input` 核心：`GetKey`/`GetAxis`/`GetMouseButton`/`GetTouch`/`acceleration`。
- 新 `Input System` 核心：`PlayerInput`、`InputAction`、`InputActionMap`、`Keyboard.current`、`Mouse.current`、`started/performed/canceled`、`CallbackContext`。
- 触摸两版都有：老 `Input.GetTouch`，新 `Touchscreen.current`。
- 选型口诀：**简单原型用老，正式新项目用新**。

---

# 第 5 章 物理与碰撞

> **本章管辖**：Unity 的物理体系——基于 **PhysX**（NVIDIA 物理引擎）的刚体、碰撞体、触发器、射线检测与关节。
> **一句话**：`Rigidbody` 让物体"会动"，`Collider` 让物体"有体积"，`Trigger` 让物体"能感知"，`Raycast` 让物体"能看见"。
> **前置**：建议先看 [第 1 章](第01章_核心物件体系.md) 了解 `GameObject`/`Component`/`MonoBehaviour`，再看 [第 3 章](../第03章_数学与向量.md) 了解 `Vector3`。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 让物体受重力/受力运动 | `Rigidbody` + `AddForce` / `useGravity` |
| 让物体不参与物理、只被脚本控制 | `isKinematic = true` |
| 给物体一个瞬间速度 | `rb.velocity` / `rb.AddForce(..., ForceMode.Impulse)` |
| 让物体旋转 | `rb.AddTorque` / `rb.angularVelocity` |
| 让物体休眠/唤醒 | `rb.Sleep()` / `rb.Awake()` |
| 制造爆炸冲击 | `rb.AddExplosionForce` |
| 给物体一个"形状"用于碰撞 | `BoxCollider` / `SphereCollider` / `CapsuleCollider` / `MeshCollider` |
| 只检测不阻挡（穿过去） | `Collider.isTrigger = true` |
| 设置弹力/摩擦力 | `PhysicsMaterial`（`bounciness` / `friction`） |
| 检测"有东西进来了" | `OnTriggerEnter` |
| 检测"撞上了"（要物理响应） | `OnCollisionEnter` |
| 从某点发射一条射线 | `Physics.Raycast` |
| 检测一个球形范围内的物体 | `Physics.OverlapSphere` / `Physics.SphereCast` |
| 只检测指定图层 | `LayerMask` |
| 拿到射线命中的信息 | `RaycastHit`（`point` / `normal` / `collider` / `transform` / `distance`） |
| 把两个物体铰链/弹簧/固定连接 | `HingeJoint` / `SpringJoint` / `FixedJoint` / `CharacterJoint` |
| 设置物理步长/重力 | `Time.fixedDeltaTime` / `Physics.gravity` |

---

## 5.1 Rigidbody（刚体）

### 5.1.1 `class Rigidbody : Component` — 三种刚体类型

**【是什么】** 让 `GameObject` 参与物理模拟的组件。只有挂了 `Rigidbody`，物体才会受重力、力、碰撞的物理影响。它由 PhysX 引擎驱动。

**【用途】** 任何需要真实物理运动的物体（角色、箱子、子弹、可推动物）都要挂它。

**【名称含义】** `Rigid`（刚硬的）+ `body`（物体）——"刚体"，即**不会变形**的物体（区别于软体/布料）。

**【三种刚体类型（面试常考）】**

| 类型 | 设置 | 行为 | 典型用途 |
|------|------|------|---------|
| **Dynamic（动态）** | 默认（`isKinematic=false`） | 受重力、力、碰撞影响，由物理引擎驱动 | 箱子、掉落物、可推动物 |
| **Kinematic（运动学）** | `isKinematic=true` | 不受力/重力影响，但**能推动**动态物体；由脚本/动画控制位置 | 移动平台、门、NPC、玩家（用 `transform` 控制） |
| **Static（静态）** | 不挂 Rigidbody（只挂 Collider） | 完全不动，作为"地面/墙" | 地面、墙壁、障碍物 |

> **面试常考**：Kinematic 刚体**不会**被力推动，但**会**推动 Dynamic 刚体；Static 刚体本质是"没有 Rigidbody 的 Collider"，引擎把它当无限质量处理。

**【代码示例】**
```csharp
// 动态刚体：受重力，可被推动
Rigidbody rb = GetComponent<Rigidbody>();
rb.useGravity = true;      // 受重力
rb.isKinematic = false;    // 动态

// 运动学刚体：脚本控制位置，但能推动别人
rb.isKinematic = true;
transform.position += Vector3.forward * Time.deltaTime; // 用 transform 移动
```

---

### 5.1.2 `Rigidbody.AddForce(Vector3 force, ForceMode mode)`

**【是什么】** 给刚体施加一个**力**（持续作用，产生加速度）。

**【参数说明】**
- `force`：力的方向和大小（`Vector3`）。
- `mode`（`ForceMode` 枚举，可选）：
  - `ForceMode.Force`：持续力（默认），质量越大加速度越小。
  - `ForceMode.Acceleration`：加速度，忽略质量。
  - `ForceMode.Impulse`：瞬间冲量（适合跳跃、爆炸、射击击退）。
  - `ForceMode.VelocityChange`：瞬间速度改变，忽略质量。

**【返回值】** 无（void）。

**【代码示例】**
```csharp
// 跳跃：用 Impulse 给一个向上的瞬间冲量
public class PlayerJump : MonoBehaviour
{
    public float jumpForce = 5f;
    private Rigidbody rb;

    void Start() { rb = GetComponent<Rigidbody>(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // 瞬间跳起
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `AddForce` | 施加**力**，产生加速度，受质量影响（除非用 Acceleration/Velocity 模式） |
| `rb.velocity = ...` | 直接**设置速度**，绕过物理计算，更"硬" |
| `AddTorque` | 施加**旋转**力（力矩） |

---

### 5.1.3 `Rigidbody.AddTorque(Vector3 torque, ForceMode mode)`

**【是什么】** 给刚体施加**旋转力矩**，让它绕轴旋转。

**【用途】** 让物体翻滚、旋转、被击飞时转起来。

**【代码示例】**
```csharp
// 让物体持续翻滚
rb.AddTorque(Vector3.right * 10f); // 绕 X 轴旋转
```

---

### 5.1.4 `Rigidbody.velocity` / `Rigidbody.angularVelocity`

**【是什么】**
- `velocity`：刚体当前的**线速度**（`Vector3`，米/秒）。
- `angularVelocity`：刚体当前的**角速度**（`Vector3`，弧度/秒）。

**【用途】** 读取当前运动状态，或直接"设定"速度（常用于玩家移动、击退）。

**【代码示例】**
```csharp
// 读取速度判断是否在移动
float speed = rb.velocity.magnitude;
if (speed > 0.1f) Debug.Log("在移动");

// 直接设置速度（击退）
rb.velocity = Vector3.back * 10f;
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `velocity` | 直接设速度，绕过物理计算，立即生效 |
| `AddForce` | 施加力，由物理引擎逐步改变速度（更真实） |

---

### 5.1.5 `Rigidbody.useGravity` / `mass` / `drag` / `angularDrag`

**【是什么】** 刚体的基础物理属性。

| 属性 | 含义 | 默认值 |
|------|------|--------|
| `useGravity` | 是否受重力影响（`bool`） | `true` |
| `mass` | 质量（`float`），影响受力加速度 | `1` |
| `drag` | 线阻力（`float`），越大减速越快 | `0` |
| `angularDrag` | 角阻力（`float`），越大旋转越慢 | `0.05` |

**【代码示例】**
```csharp
rb.useGravity = false;   // 关闭重力（如太空场景）
rb.mass = 10f;           // 变重，更难被推动
rb.drag = 2f;            // 加阻力，移动后很快停下
```

---

### 5.1.6 `Rigidbody.Sleep()` / `Rigidbody.Awake()`

**【是什么】**
- `Sleep()`：让刚体**休眠**（停止物理模拟，省性能）。
- `Awake()`：唤醒刚体（恢复物理模拟）。

**【用途】** 性能优化：静止的物体自动休眠；需要时手动唤醒。

**【代码示例】**
```csharp
rb.Sleep();   // 强制休眠
rb.Awake();   // 唤醒
```

> **面试常考**：刚体长时间静止会自动进入休眠状态，这是 PhysX 的性能优化。`IsSleeping()` 可查询是否在休眠。

---

### 5.1.7 `Rigidbody.AddExplosionForce(float force, Vector3 explosionPos, float radius, float upwardModifier, ForceMode mode)`

**【是什么】** 模拟**爆炸冲击波**：以某点为圆心，把半径内所有刚体向外推。

**【参数说明】**
- `force`：爆炸力大小。
- `explosionPos`：爆炸中心点。
- `radius`：影响半径。
- `upwardModifier`：向上抬升的额外力（让物体飞起来）。
- `mode`：`ForceMode`（默认 `Force`）。

**【代码示例】**
```csharp
// 手雷爆炸：把周围刚体炸飞
public void Explode(Vector3 center, float radius, float power)
{
    Collider[] hits = Physics.OverlapSphere(center, radius);
    foreach (var hit in hits)
    {
        if (hit.TryGetComponent<Rigidbody>(out var rb))
            rb.AddExplosionForce(power, center, radius, 1f, ForceMode.Impulse);
    }
}
```

---

## 5.2 Collider（碰撞体）

### 5.2.1 `class Collider` — 碰撞体基类

**【是什么】** 定义物体的**物理形状**（体积），用于碰撞检测和触发检测。`Rigidbody` 决定"会不会动"，`Collider` 决定"长什么样、能不能被撞到"。

**【用途】** 让物体有体积感，能被射线命中、能被碰撞、能触发事件。

**【名称含义】** `Collide`（碰撞）+ `er`（者）——"碰撞者"。

**【常见类型】**

| 类型 | 形状 | 性能 | 用途 |
|------|------|------|------|
| `BoxCollider` | 长方体 | 快 | 箱子、门、墙 |
| `SphereCollider` | 球体 | 快 | 球、圆形区域 |
| `CapsuleCollider` | 胶囊体 | 快 | 角色、柱子 |
| `MeshCollider` | 任意网格 | **慢** | 复杂地形、不规则物体 |

> **面试常考**：`MeshCollider` 性能差，因为要精确匹配网格。**优先用基本几何体（Box/Sphere/Capsule）组合**，只有地形等复杂形状才用 MeshCollider。且 MeshCollider 默认 `convex=false`，**不能**与另一个 MeshCollider 碰撞（除非勾选 `convex`）。

---

### 5.2.2 `Collider.isTrigger`

**【是什么】** 布尔属性。设为 `true` 后，该碰撞体变成 **Trigger（触发器）**：**不阻挡物体**，只负责**检测**（触发 `OnTrigger*` 事件）。

**【用途】** 拾取区、检测区、门禁、陷阱区域。

**【代码示例】**
```csharp
// 在 Inspector 勾选 isTrigger，或用代码
GetComponent<Collider>().isTrigger = true;
```

**【相似 API 区别】**
| 状态 | 行为 |
|------|------|
| `isTrigger = false` | 实心碰撞体，阻挡物体，触发 `OnCollision*` |
| `isTrigger = true` | 触发器，不阻挡，触发 `OnTrigger*` |

---

### 5.2.3 `Collider.sharedMaterial`（PhysicsMaterial）

**【是什么】** 给碰撞体指定一个 `PhysicsMaterial`（物理材质），控制**弹力**和**摩擦力**。

**【属性】**
- `bounciness`：弹性（0~1），越大越弹。
- `friction`：摩擦力（0~1），越大越难滑动。

**【代码示例】**
```csharp
// 创建物理材质并设置弹性
PhysicsMaterial mat = new PhysicsMaterial("Bouncy");
mat.bounciness = 0.8f;   // 高弹性
mat.friction = 0.1f;     // 低摩擦

GetComponent<Collider>().sharedMaterial = mat; // 挂到碰撞体
```

---

## 5.3 Trigger 与 Collision 事件

### 5.3.1 `OnTriggerEnter` / `OnTriggerStay` / `OnTriggerExit`

**【是什么】** 触发器事件。当**本物体**（带 Trigger）与**其他物体**（带 Collider）发生接触时触发。**前提**：至少一方是 Trigger，且**至少一方有 Rigidbody**。

| 回调 | 触发时机 |
|------|---------|
| `OnTriggerEnter(Collider other)` | 进入触发区域时（一次） |
| `OnTriggerStay(Collider other)` | 在触发区域内（每帧） |
| `OnTriggerExit(Collider other)` | 离开触发区域时（一次） |

**【用途】** **检测**（不产生物理阻挡）：拾取物品、进入区域、检测敌人。

**【代码示例】**
```csharp
// 拾取物品：玩家进入物品的 Trigger 区域
public class Pickup : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("拾取到物品！");
            Destroy(gameObject);   // 拾取后销毁
        }
    }
}
```

---

### 5.3.2 `OnCollisionEnter` / `OnCollisionStay` / `OnCollisionExit`

**【是什么】** 碰撞事件。当**两个实心碰撞体**（都非 Trigger）发生物理碰撞时触发。**前提**：至少一方有 Rigidbody。

**【回调】**
| 回调 | 触发时机 |
|------|---------|
| `OnCollisionEnter(Collision collision)` | 碰撞开始（一次） |
| `OnCollisionStay(Collision collision)` | 碰撞持续中（每帧） |
| `OnCollisionExit(Collision collision)` | 碰撞结束（一次） |

**【用途】** **碰撞响应**（有物理阻挡）：受击、落地、撞墙。

**【代码示例】**
```csharp
// 受击：玩家撞到敌人
public class Player : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("玩家受击！");
            // 扣血、播放动画等
        }
    }
}
```

**【相似 API 区别（面试常考）】**
| 对比项 | `OnTrigger*` | `OnCollision*` |
|--------|-------------|---------------|
| 前提 | 一方是 Trigger | 双方都是实心碰撞体 |
| 是否阻挡 | 不阻挡（穿透） | 阻挡（物理响应） |
| 参数类型 | `Collider other` | `Collision collision` |
| 用途 | **检测**（拾取/区域） | **碰撞响应**（受击/落地） |
| 需要 Rigidbody | 至少一方有 | 至少一方有 |

> **面试常考**：**检测用 Trigger，碰撞响应用 Collision**。Trigger 不产生物理阻挡，适合拾取、区域检测；Collision 有真实物理阻挡，适合受击、落地。

---

## 5.4 Raycast（射线检测）

### 5.4.1 `Physics.Raycast` — 射线检测

**【是什么】** 从某点沿某方向发射一条**射线**，检测它是否命中碰撞体。

**【用途】** 射击检测、点击拾取、视线检测、寻路。

**【参数说明】**
- `origin`：射线起点（`Vector3`）。
- `direction`：射线方向（`Vector3`）。
- `maxDistance`：最大距离（`float`）。
- `layerMask`：只检测指定图层（`LayerMask`）。
- `hit`：输出命中信息（`out RaycastHit`）。

**【返回值】** `bool`：是否命中。

**【代码示例】**
```csharp
// 射击检测：从枪口发射射线
public class Gun : MonoBehaviour
{
    public float range = 100f;
    public LayerMask targetLayer;

    void Shoot()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, targetLayer))
        {
            Debug.Log("命中：" + hit.collider.name);
            // 造成伤害等
        }
    }
}
```

---

### 5.4.2 `RaycastHit` 结构

**【是什么】** 射线/球形检测的**命中结果**结构体。

**【核心字段】**
| 字段 | 含义 |
|------|------|
| `point` | 命中点的世界坐标 |
| `normal` | 命中表面的法线（垂直方向） |
| `collider` | 命中的碰撞体 |
| `transform` | 命中碰撞体所属的 Transform |
| `distance` | 从起点到命中点的距离 |

**【代码示例】**
```csharp
if (Physics.Raycast(ray, out RaycastHit hit, 100f))
{
    Vector3 hitPoint = hit.point;          // 命中点
    Vector3 normal = hit.normal;           // 表面法线
    Collider c = hit.collider;             // 命中的碰撞体
    Transform t = hit.transform;           // 命中的物体
    float dist = hit.distance;             // 距离
}
```

---

### 5.4.3 `Physics.SphereCast` / `Physics.BoxCast`

**【是什么】** 用**球形/盒形**代替细射线做检测，能检测到"有一定体积"的物体。

**【用途】** 角色移动碰撞检测（用 SphereCast 代替 Raycast 更真实）、宽物体检测。

**【代码示例】**
```csharp
// 球形检测：从角色位置向前检测
if (Physics.SphereCast(transform.position, 0.5f, transform.forward,
                       out RaycastHit hit, 2f))
{
    Debug.Log("球形检测命中：" + hit.collider.name);
}
```

**【相似 API 区别】**
| API | 形状 | 用途 |
|-----|------|------|
| `Physics.Raycast` | 细射线 | 精确点检测（射击、视线） |
| `Physics.SphereCast` | 球体 | 有体积的检测（角色移动） |
| `Physics.BoxCast` | 盒体 | 有体积的检测（宽物体） |

---

### 5.4.4 `Physics.RaycastAll` — 返回所有命中

**【是什么】** 返回射线命中的**所有**碰撞体（数组），而不是只返回第一个。

**【用途】** 穿透检测、一次命中多个目标。

**【代码示例】**
```csharp
RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
foreach (var hit in hits)
{
    Debug.Log("命中：" + hit.collider.name);
}
```

---

### 5.4.5 `Physics.OverlapSphere` — 球形范围检测

**【是什么】** 检测一个**球形区域**内所有碰撞体（不发射射线，直接查范围）。

**【用途】** 爆炸范围、拾取范围、检测周围敌人。

**【代码示例】**
```csharp
// 检测周围敌人
Collider[] enemies = Physics.OverlapSphere(transform.position, 5f);
foreach (var col in enemies)
{
    if (col.CompareTag("Enemy"))
        Debug.Log("发现敌人：" + col.name);
}
```

**【相似 API 区别】**
| API | 行为 |
|-----|------|
| `Physics.Raycast` | 沿方向检测第一个 |
| `Physics.SphereCast` | 沿方向检测（有体积） |
| `Physics.OverlapSphere` | 检测**球形范围**内所有（不沿方向） |

---

### 5.4.6 `LayerMask` — 图层过滤

**【是什么】** 用于**过滤**射线/检测只命中指定图层。避免检测到无关物体。

**【代码示例】**
```csharp
// 只检测 "Enemy" 图层
int enemyLayer = LayerMask.GetMask("Enemy");
if (Physics.Raycast(ray, out RaycastHit hit, 100f, enemyLayer))
{
    // 只命中敌人
}
```

**【相似 API 区别】**
- `LayerMask.GetMask("LayerName")`：按名字取图层掩码。
- `LayerMask.NameToLayer("LayerName")`：取图层索引（int）。

---

## 5.5 关节 Joint

### 5.5.1 关节总览

**【是什么】** 关节（`Joint`）把两个刚体**连接**起来，限制它们的相对运动。

**【用途】** 门、摆锤、弹簧、布娃娃、机械结构。

| 关节 | 行为 | 用途 |
|------|------|------|
| `HingeJoint` | 铰链：绕一个轴旋转 | 门、摆锤 |
| `SpringJoint` | 弹簧：把两物体拉在一起 | 弹簧、绳索 |
| `FixedJoint` | 固定：两物体焊死 | 连接部件 |
| `CharacterJoint` | 角色关节：限制旋转范围 | 布娃娃、角色 |

**【代码示例】**
```csharp
// 给物体加一个铰链关节
HingeJoint hinge = gameObject.AddComponent<HingeJoint>();
hinge.connectedBody = otherRigidbody;   // 连接到另一个刚体
```

---

## 5.6 物理材质 2D 与 3D 区别

**【是什么】** Unity 有**两套物理体系**：3D（PhysX）和 2D（Box2D）。物理材质也分两套。

| 项 | 3D | 2D |
|----|----|----|
| 物理引擎 | PhysX | Box2D |
| 刚体 | `Rigidbody` | `Rigidbody2D` |
| 碰撞体 | `Collider` | `Collider2D` |
| 物理材质 | `PhysicsMaterial` | `PhysicsMaterial2D` |
| 事件 | `OnCollision*` / `OnTrigger*` | `OnCollision2D*` / `OnTrigger2D*` |

> **注意**：2D 和 3D 组件**不能混用**。2D 物体用 `Rigidbody2D` + `Collider2D`，3D 物体用 `Rigidbody` + `Collider`。混用会导致物理失效。

**【代码示例】**
```csharp
// 2D 物理材质
PhysicsMaterial2D mat2D = new PhysicsMaterial2D();
mat2D.bounciness = 0.5f;   // 弹性
mat2D.friction = 0.2f;     // 摩擦
```

---

## 5.7 物理配置

### 5.7.1 `Time.fixedDeltaTime` 与 `FixedUpdate`

**【是什么】** 物理引擎使用**固定时间步**（`fixedDeltaTime`）更新，而不是每帧。`FixedUpdate` 在固定时间步被调用。

**【用途】** 物理计算必须在 `FixedUpdate` 里做（而不是 `Update`），保证物理稳定。

**【代码示例】**
```csharp
void FixedUpdate()
{
    // 物理移动：用固定时间步
    rb.AddForce(Vector3.forward * speed);
}
```

> **面试常考**：`FixedUpdate` 固定频率（默认 0.02 秒，即 50 次/秒），`Update` 每帧一次。**物理相关代码放 FixedUpdate**，否则物理不稳定。

---

### 5.7.2 `Physics.gravity` 与最大碰撞速度

**【是什么】**
- `Physics.gravity`：全局重力（`Vector3`，默认 `(0, -9.81, 0)`）。
- 最大碰撞速度：`Physics.defaultMaxDepenetrationVelocity` 等配置项。

**【代码示例】**
```csharp
// 修改全局重力（如月球场景）
Physics.gravity = new Vector3(0, -1.6f, 0);
```

---

## 5.8 本章高频「相似 API」对比总表

| 想做的事 | 用哪个 | 别用哪个/注意 |
|---------|--------|--------------|
| 检测（拾取/区域） | `OnTrigger*` | `OnCollision*`（那是碰撞响应） |
| 碰撞响应（受击/落地） | `OnCollision*` | `OnTrigger*`（不阻挡） |
| 精确点检测 | `Physics.Raycast` | `OverlapSphere`（那是范围） |
| 有体积检测 | `Physics.SphereCast` | `Raycast`（太细） |
| 检测周围所有 | `Physics.OverlapSphere` | `Raycast`（只一条线） |
| 让物体受物理 | `Rigidbody`（Dynamic） | `isKinematic=true`（不受力） |
| 只做检测不阻挡 | `isTrigger=true` | 实心 Collider（会阻挡） |
| 复杂形状碰撞 | `MeshCollider`（慎用） | 优先用 Box/Sphere/Capsule 组合 |

---

## 5.9 版本标记（本章涉及的版本化 API）

| API | 标记 |
|-----|------|
| `Physics.Raycast` 等 | 老 API（稳定可用） |
| `Rigidbody.AddForce` | 老 API（稳定可用） |
| `PhysicsMaterial` | 老 API（稳定可用） |
| `Physics.gravity` | 老 API（稳定可用） |
| 2D 物理（`Rigidbody2D` 等） | 独立体系，与 3D 不混用 |

> 具体版本细节以官方手册为准，这里只给推理方向。

---

## 5.10 本章小结

- `Rigidbody` 让物体"会动"，分 Dynamic（受物理）、Kinematic（脚本控制）、Static（不动）三种。
- `Collider` 给物体形状，`isTrigger=true` 变触发器（只检测不阻挡）。
- **检测用 Trigger，碰撞响应用 Collision**。
- `Raycast` 做精确点检测，`SphereCast`/`BoxCast` 做有体积检测，`OverlapSphere` 做范围检测。
- 关节（`Joint`）连接两个刚体，模拟门、弹簧、布娃娃。
- 2D 物理（Box2D）与 3D 物理（PhysX）是两套体系，不能混用。
- 物理计算放 `FixedUpdate`，用 `Time.fixedDeltaTime` 固定步。

---

# 第 6 章 时间与异步

> **本章管辖**：所有"和时间打交道"的事——帧率与时间量、延时、协程、async/await、Job System、计时。
> **一句话**：`Time.deltaTime` 让移动与帧率无关；协程是把一段代码切成多段、按时间点续跑；async/await 和 Job System 是进阶的异步与并行手段。
> **前置**：第 1 章 `MonoBehaviour` 生命周期；具备基础 C# 语法（方法、委托、泛型、`IEnumerator` 概念）。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 每帧移动且与帧率无关 | `Time.deltaTime` |
| 暂停 / 慢放整个游戏 | `Time.timeScale = 0`（配合 `unscaled` 系列） |
| 拿不受暂停影响的"真实经过时间" | `Time.realtimeSinceStartup` / `Time.unscaledTime` |
| 等一帧再执行 | 协程 `yield return null` |
| 延迟 n 秒后执行 | 协程 `WaitForSeconds` / 异步 `Task.Delay` |
| 到帧末（渲染完成后）执行 | 协程 `WaitForEndOfFrame` |
| 等一个物理固定步进 | 协程 `WaitForFixedUpdate` |
| 启动 / 停止协程 | `StartCoroutine` / `StopCoroutine` / `StopAllCoroutines` |
| 协程里循环计时做延迟攻击 | `IEnumerator` + `WaitForSeconds` |
| 异步方法（可等待、可传播异常） | `async Task<T>`（返回 `async void` 只用于事件） |
| 原生等待 Unity 异步对象（场景、异步加载） | `await AsyncOperation`（Unity 2023.1+ 直接 await）；`Awaitable`（Unity 2022.2+）用于官方异步原语 |
| 轻量零 GC 的异步（第三方） | `UniTask`（Cysharp，第三方） |
| 多线程并行计算（进阶） | `IJob` + `JobHandle.Schedule` |
| 测量"这段代码跑了多久" | `Stopwatch` / 两次 `Time.time` 差值 |

---

## 6.1 时间系统总览：帧与时间的关系

### 6.1.1 为什么需要 `deltaTime`（帧独立移动的关键）

**【是什么】** `deltaTime` = "这一帧和上一帧之间经过了多少秒"。它是理解整个 Unity 时间系统的一把钥匙。

**【用途】** 让"每帧执行一次"的代码，其**速度不再受帧率影响**。

**【为什么是关键🚨】** 假设你要让物体每秒移动 1 米：

```csharp
// ❌ 帧相关写法：60 FPS 时每秒走 60，30 FPS 时每秒只走 30，低配电脑慢、高刷屏快
transform.position += Vector3.right * 1f;

// ✅ 帧独立写法：无论 30 还是 120 FPS，都是每秒 1 米
// 速度 1 米/秒 × 这一帧过了多少秒 = 这一帧该走的距离
transform.position += Vector3.right * 1f * Time.deltaTime;
```

**【推理】** 1 秒内跑 N 帧，每帧 `deltaTime ≈ 1/N`。把 N 帧的位移加起来：`N × v × (1/N) = v`，正好每秒 `v` 米。帧率只影响**计算的次数**，不影响**累计的结果**。

**【C++/Raylib 类比】** 手写游戏循环里常见的 `float dt = GetFrameTime();`（Raylib）或 `GetDeltaTime()`，就是同一个东西。区别只是 Unity 帮你把这个值预置好了，不用自己测。

【⚠️ 注意】`deltaTime` 在 `Update` 里取的是"上一帧到这一帧"的时间；在 `FixedUpdate` 里它恒等于 `fixedDeltaTime`（固定值）。

---

### 6.1.2 `class Time` — 静态时间类

**【是什么】** `UnityEngine.Time` 是一个**静态类**，不用实例化，全局只有一个"游戏时钟"。它提供两大类信息：

1. **缩放类**：受 `timeScale` 影响（`time`、`deltaTime`…）；
2. **真实类**：不受 `timeScale` 影响（`realtimeSinceStartup`、`unscaledTime`、`unscaledDeltaTime`…）。
3. **固定步进类**：`fixedDeltaTime` 的值**不受** `timeScale` 影响（恒为 Project Settings 里配置的 Fixed Timestep，默认 0.02），但物理模拟的**实际节奏**随 `timeScale` 缩放。

**【用途】** 移动、计时、倒计时、暂停菜单、动画驱动。

**【名称含义】** `Time` = 时间。

**【核心成员一览】**
| 成员 | 类型 | 受 `timeScale` 影响 | 一句话 |
|------|------|:---:|--------|
| `Time.deltaTime` | `float` | ✅ | 本帧经过的（缩放后）秒数 |
| `Time.fixedDeltaTime` | `float` | ❌ 值不变（恒为固定步长），但物理节奏随 timeScale 缩放 | 物理固定步进时长（默认 0.02 秒），不随 timeScale 变值 |
| `Time.time` | `float` | ✅ | 游戏开始后累计时间（缩放后） |
| `Time.unscaledTime` | `float` | ❌ | 忽略缩放的累计时间 |
| `Time.unscaledDeltaTime` | `float` | ❌ | 忽略缩放的帧间秒数 |
| `Time.timeScale` | `float` | — | 时间缩放系数（0 即暂停） |
| `Time.realtimeSinceStartup` | `float` | ❌ | 从引擎启动起的真实秒数 |

---

### 6.1.3 `Time.deltaTime` 属性

**【是什么】** 自上一帧以来经过的时间（秒），`float`。**所有帧独立移动的基石**。

**【用途】** 在 `Update` 里做与速度相乘的位移、推进倒计时、插值。

**【名称含义】** `delta`（希腊字母 Δ）= "增量/差值"，`Time` = 时间 → "时间增量"。

**【代码示例】**
```csharp
void Update()
{
    // 帧独立移动：每秒向前 5 米
    transform.position += Vector3.forward * 5f * Time.deltaTime;

    // 帧独立转动：每秒转 90 度
    transform.Rotate(Vector3.up, 90f * Time.deltaTime);

    // 倒计时（不依赖帧率）
    countdown -= Time.deltaTime;
    if (countdown <= 0f) DoTimeout();
}
```

**【返回值】** 上一帧到本帧的秒数（`float`）。数值很小（约 `0.016` @60FPS），所以永远要"乘上它"而不是"直接用它"。

【⚠️ 注意】在 `Awake`/`Start` 里读它没有意义（此时还没有"上一帧"）。也别在 `FixedUpdate` 里用它做物理——那里应该用 `Time.fixedDeltaTime`。

---

### 6.1.4 `Time.fixedDeltaTime` 属性

**【是什么】** 物理系统每一次固定更新的间隔，**默认 0.02 秒（50Hz）**。

**【用途】** `FixedUpdate` 里给 `Rigidbody` 施加速度/力，做物理相关的时间补偿。

**【名称含义】** `fixed` = 固定的 → "固定的增量时间"。因为物理步进是**固定频率**的，不随帧率波动。

**【代码示例】**
```csharp
void FixedUpdate()
{
    // FixedUpdate 里 Time.deltaTime 恒等于 Time.fixedDeltaTime（默认 0.02）
    rb.velocity += Vector3.down * 9.8f * Time.fixedDeltaTime; // 简单重力
}
```

**【相似 API 区别】**
| API | 频率 | 用途 |
|-----|------|------|
| `Time.deltaTime` | 每帧一次，值随帧率变 | `Update` 里的视觉移动、计时 |
| `Time.fixedDeltaTime` | 固定 50Hz，值恒定 | `FixedUpdate` 里的物理计算 |

**【参数（配置）】** 在 `Edit → Project Settings → Time` 面板里的 **Fixed Timestep** 可改，但一般不推荐改。

---

### 6.1.5 `Time.time` 属性

**【是什么】** 游戏开始（进入 Play）后累计的秒数，受 `timeScale` 影响。

**【用途】** 记录"游戏内经过多久"、做时间戳、判断冷却是否结束。

**【名称含义】** `time` = 时间，就是"游戏时间"本身。

**【代码示例】**
```csharp
float startTime;

void Start() { startTime = Time.time; }

void Update()
{
    // 开局 3 秒后才允许攻击
    if (Time.time - startTime > 3f) canAttack = true;
}
```

【⚠️ 注意】`Time.time` 是 `float`，精度有限；跑几小时后误差会累积，**长时间统计别用 float 累计**，可用 `Time.realtimeSinceStartup` 或 `double` 自己累加。

---

### 6.1.6 `Time.unscaledDeltaTime` 属性

**【是什么】** 不受 `timeScale` 影响的帧间增量，**暂停菜单的命根子**。

**【用途】** 暂停游戏时 UI 仍要动、手柄震动手感仍要播放、BGM 淡出等"暂停期间也要跑"的计时。

**【代码示例】**
```csharp
// 按下 ESC 暂停：timeScale=0 会让普通 Update 计时全部冻结
void TogglePause()
{
    paused = !paused;
    Time.timeScale = paused ? 0f : 1f;
}

// 但暂停菜单里的动画用 unscaled 依然会动
void Update()
{
    menuCursorBlink += Time.unscaledDeltaTime; // 暂停时依然闪烁
}
```

**【相似 API 区别】**
| API | 缩放？ | 语义 |
|-----|:---:|------|
| `Time.deltaTime` | ✅ 被缩放 | 暂停时为 0，所有用它推进的东西全停 |
| `Time.unscaledDeltaTime` | ❌ 不缩放 | 暂停时依然有值，UI/音效等继续走 |

**【版本标记】** 长期 API（很早就存在，稳定）。

---

### 6.1.7 `Time.timeScale` 属性

**【是什么】** 全局时间缩放系数。`1` 正常，`0` 完全暂停，`0.5` 慢动作，`2` 快进。

**【用途】** 暂停菜单、子弹时间慢动作、过场动画。

**【名称含义】** `time` = 时间，`Scale` = 缩放 → "时间缩放"。

**【代码示例】**
```csharp
// 暂停
Time.timeScale = 0f;
// 恢复
Time.timeScale = 1f;
// 慢动作特写（0.3 倍速），视觉、动画、物理全都变慢
Time.timeScale = 0.3f;
```

【⚠️ 注意】`timeScale` 影响 `Update` 的 `deltaTime`、`WaitForSeconds`、`Animator`、`Rigidbody` 物理等**几乎所有时间驱动的东西**。想让"某一块"不受影响，必须用 `unscaled` 系列，或挂到不受缩放的 UI/管理器上。另外 `timeScale = 0` 时**协程的 `WaitForSeconds` 永远不会结束**，这是面试高频坑。

---

### 6.1.8 `Time.realtimeSinceStartup` 属性

**【是什么】** 从引擎启动（进程开始）到现在的**真实墙钟秒数**，不受 `timeScale`、不受暂停、不受场景切换影响。

**【用途】** 性能统计（Profiler 常拿它当基准）、帧耗时测量、**和外部时间对齐**、判断卡顿。

**【代码示例】**
```csharp
// 测量一帧真实耗时（含卡顿）
float t0 = Time.realtimeSinceStartup;
HeavyJob(); // 一些重量级计算
float elapsedMs = (Time.realtimeSinceStartup - t0) * 1000f;
Debug.Log($"本帧真实耗时 {elapsedMs:F1} ms");
```

**【相似 API 区别】**
| API | 起点 | 受缩放 | 用途 |
|-----|------|:---:|------|
| `Time.time` | 进入 Play | ✅ | 游戏内计时（可暂停） |
| `Time.realtimeSinceStartup` | 引擎启动 | ❌ | 真实耗时、性能测量 |
| `System.Environment.TickCount` / `Stopwatch` | 进程/任意 | ❌ | C# 层面测量（不依赖 Unity） |

【⚠️ 注意】`realtimeSinceStartup` 是 `float`，单位秒，长时间运行精度有限。做精确毫秒级测量建议用 `System.Diagnostics.Stopwatch`（见 6.5 节）。

---

### 6.1.9 时间量对比总表（本章第一张必背表）

| 想表达的时间 | 用这个 | 暂停时会怎样 |
|------------|--------|:---:|
| 这一帧的增量 | `Time.deltaTime` | 变 0 |
| 这一帧的增量（不受暂停） | `Time.unscaledDeltaTime` | 照常 |
| 游戏开始以来的累计 | `Time.time` | 停住 |
| 累计（不受暂停） | `Time.unscaledTime` | 照常走 |
| 物理步进 | `Time.fixedDeltaTime` | 值不变（恒 0.02），但物理步进本身停走 |
| 引擎启动以来的真实秒 | `Time.realtimeSinceStartup` | 照常走 |
| 暂停/慢放开关 | `Time.timeScale` | — |

【面试常考】**"为什么用 deltaTime 移动？"** 答：移动速度 = `v × deltaTime`，帧率只影响每帧调用次数，不影响每秒累计位移，实现帧率无关。**"timeScale=0 后什么还在走？"** 答：`unscaledTime/unscaledDeltaTime/realtimeSinceStartup` 以及用它们驱动的逻辑。

---

## 6.2 协程 Coroutine

### 6.2.1 协程是什么（与普通方法的区别）

**【是什么】** 协程（Coroutine）是一种**能暂停执行、把控制权交回引擎、到指定时间/条件后再从暂停点继续**的方法。它不是多线程，依然跑在主线程上。

**【用途】** 延时、顺序动画、分帧加载、倒计时、简单的"状态机"。

**【和普通方法的本质区别】**
| | 普通方法 | 协程 |
|---|---------|------|
| 执行 | 从头到尾一口气跑完 | 遇到 `yield` 就暂停，之后继续 |
| 返回 | `void` / 值 | `IEnumerator` |
| 谁调它 | 直接调用 | `StartCoroutine(...)` |
| 运行时 | 调用即执行 | 调用后在下一次引擎"合适的时机"逐步续跑 |

**【名称含义】** `Co-`（共同）+ `routine`（例程/流程）= "协同的流程"，即多个流程可以交替推进。

**【C++/Raylib 类比】** 类似你手写的 `enum { IDLE, ATTACK, DEAD } state;` 状态机 + `timer` 判断——协程把"定时切换状态"这件事写成了线性代码，读起来更像"顺序脚本"。

**【代码示例：最小协程】**
```csharp
public class AttackSequence : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        Debug.Log("起手动画");
        yield return new WaitForSeconds(0.3f); // 暂停 0.3 秒
        Debug.Log("伤害判定");
        yield return new WaitForSeconds(0.2f); // 再暂停 0.2 秒
        Debug.Log("收招");
    }
}
```

---

### 6.2.2 `IEnumerator` — 协程的返回类型（含义）

**【是什么】** 协程方法必须返回 `IEnumerator`（`System.Collections`）。它不是一个"结果值"，而是一个**迭代器对象**，里面记录了"执行到哪了"。

**【返回值含义】** 返回 `IEnumerator` 不是返回数据，而是**返回一段可暂停/可继续执行的代码**。引擎每次推进它：拿到当前 `yield` 出来的东西（如 `WaitForSeconds`），到时间后调用 `MoveNext()` 继续跑下一段。

**【代码示例：理解本质】**
```csharp
// 协程方法其实就是一个生成器：
IEnumerator MyRoutine()
{
    Debug.Log("第 1 段");              // 第一次 MoveNext 执行到这里
    yield return null;                 // 暂停，交出 null
    Debug.Log("第 2 段");              // 下一帧 MoveNext 继续
    yield return null;
    Debug.Log("结束");                 // 最后一次 MoveNext 跑到末尾，协程结束
}
```

【⚠️ 注意】`yield return` 后面**跟着什么，决定了"什么时候继续"**：
- `null` → 下一帧继续；
- `WaitForSeconds(2f)` → 2 秒后继续；
- 一个子协程 `IEnumerator` → 等那个协程跑完；
- `WaitUntil(() => cond)` → 条件为真时继续；
- `WaitForEndOfFrame` → 本帧渲染结束后继续。

---

### 6.2.3 `StartCoroutine(IEnumerator routine)` 方法

**【是什么】** 启动一个协程。`MonoBehaviour` 的方法，必须挂在活动物体上才能跑。

**【参数说明】**
- `IEnumerator routine`：协程方法**调用后**的返回值（注意要写 `StartCoroutine(MyRoutine())`，带括号，不能只传方法名）。
- 重载：`StartCoroutine(string methodName)`（按名字启动，不推荐，性能差且没法传自定义 yield）。

**【返回值】** `Coroutine` 对象（可用于 `StopCoroutine` 精确停止）。

**【代码示例】**
```csharp
void Start()
{
    // ✅ 标准写法：传方法调用后的 IEnumerator
    Coroutine c = StartCoroutine(DelayedAttack());

    // ❌ 常见错误：漏了括号
    // StartCoroutine(DelayedAttack);  // 编译报错，IEnumerator 和 method group 不匹配
}

IEnumerator DelayedAttack()
{
    yield return new WaitForSeconds(1f);
    Debug.Log("攻击命中");
}
```

【⚠️ 注意】挂协程的 `MonoBehaviour` 如果 `enabled=false`，协程**继续跑**（不受影响）；但 `gameObject.SetActive(false)` 或销毁物体后协程会**停止**。

---

### 6.2.4 `yield return null`（等一帧）

**【是什么】** 暂停当前协程，**下一帧（下一次 Update 周期）**继续。

**【用途】** 分帧处理（大循环拆开防卡顿）、等待某一帧之后的组件状态、逐帧动画序列。

**【代码示例】**
```csharp
// 把 10000 个对象的逻辑分摊到多帧，避免一帧卡死
IEnumerator SpawnMany()
{
    for (int i = 0; i < 10000; i++)
    {
        Instantiate(prefab, Random.insideUnitSphere * 10f, Quaternion.identity);
        if (i % 200 == 0) yield return null; // 每生成 200 个就让出本帧
    }
}
```

**【相似 API 区别】**
| yield 目标 | 什么时候继续 |
|-----------|------------|
| `yield return null` | 下一帧（`Update` 周期） |
| `yield return new WaitForEndOfFrame()` | 本帧**所有渲染结束后**（同帧更晚） |
| `yield return new WaitForFixedUpdate()` | 下一次 `FixedUpdate` 时 |

---

### 6.2.5 `yield return WaitForSeconds(float seconds)`

**【是什么】** 暂停协程 `seconds` 秒后继续。**受 `timeScale` 影响**：暂停（scale=0）时永不结束。

**【用途】** 延迟攻击、技能 CD、定时刷怪、UI 提示消失。

**【代码示例：延迟攻击】**
```csharp
public class Swordsman : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(AttackCycle());
    }

    IEnumerator AttackCycle()
    {
        while (true) // 无限循环的"战斗节奏"
        {
            yield return new WaitForSeconds(1.2f); // 前摇 1.2 秒
            Debug.Log("挥剑！");
            // 也可以用固定数量的实例而不是循环
            yield return new WaitForSeconds(0.8f); // 后摇 0.8 秒
        }
    }
}
```

**【参数说明】** `seconds`：等待的秒数（`float`）。受 `timeScale` 影响；被 `timeScale = 0` 暂停时永远不继续。

**【优化小技巧】** 频繁 `new WaitForSeconds` 会产生 GC 分配。可以把实例缓存复用：

```csharp
WaitForSeconds wait01 = new WaitForSeconds(0.1f); // 提前建好

IEnumerator Spam()
{
    while (true)
    {
        DoTick();
        yield return wait01; // 复用同一个实例，零新增分配
    }
}
```

---

### 6.2.6 `yield return WaitForEndOfFrame`

**【是什么】** 等**本帧的渲染（Camera 渲染、UI 绘制）全部结束后**再继续。

**【用途】** 截图（`ScreenCapture`/`ReadPixels`）必须在帧末，否则截到的是上一帧；配合 `OnGUI` 相关操作。

**【代码示例】**
```csharp
IEnumerator TakeScreenshot()
{
    yield return new WaitForEndOfFrame(); // 帧末再截
    Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
    // ... 保存
}
```

【⚠️ 注意】这是协程里"最晚"的暂停点之一（比 `null` 晚一整段渲染）。如果是跑在编辑器未运行时或非渲染环境，行为可能不符合预期。

---

### 6.2.7 `yield return WaitForFixedUpdate`

**【是什么】** 等**下一次 `FixedUpdate`（物理步进）**时继续。

**【用途】** 和物理系统同步的时序：等刚体落稳再做判定、物理事件后的后续逻辑。

**【代码示例】**
```csharp
IEnumerator CheckLanding()
{
    rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);
    yield return new WaitForFixedUpdate(); // 等物理步进一次
    Debug.Log($"当前速度：{rb.velocity}");
}
```

---

### 6.2.8 自定义 yield（`IEnumerator` / `CustomYieldInstruction` / `WaitUntil`）

**【是什么】** `yield return` 后面的东西不限于预置类。你可以：
1. `yield return SomeOtherCoroutine()` —— 等另一个协程完成（协程嵌套）；
2. `yield return new WaitUntil(() => 条件)` / `new WaitWhile(() => 条件)` —— 等条件成立/成立期间等待；
3. 继承 `CustomYieldInstruction` 写自己的等待指令。

**【用途】** 等待加载完成、等 AI 状态、等动画播放完。

**【代码示例】**
```csharp
// 协程嵌套：先播攻击动画（子协程），再播收招
IEnumerator FullAttack()
{
    yield return StartCoroutine(PlayAnim("attack")); // 等攻击动画协程跑完
    yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
    yield return StartCoroutine(PlayAnim("idle"));
}
```

---

### 6.2.9 `StopCoroutine` / `StopAllCoroutines`

**【是什么】**
- `StopCoroutine(Coroutine c)` / `StopCoroutine(IEnumerator)`：停止**指定**协程。
- `StopAllCoroutines()`：停止当前 `MonoBehaviour` 上**所有**协程。

**【用途】** 取消延时、打断攻击、玩家死亡时清掉所有计时行为。

**【代码示例】**
```csharp
Coroutine active;

void Start() { active = StartCoroutine(Charge()); }

void StopCharge() => StopCoroutine(active);   // 精确停掉蓄力
void OnDeath()   => StopAllCoroutines();      // 死亡时全部停

IEnumerator Charge()
{
    while (true)
    {
        power += Time.deltaTime * 5f;
        yield return null;
    }
}
```

【⚠️ 注意】
- 用字符串启动的协程要用字符串停：`StopCoroutine("Name")`。
- `StopAllCoroutines` 只停**这个脚本**上的协程，不会影响其他脚本。
- 协程结束时（跑完或被停），其内部资源由引擎自动清理。

---

### 6.2.10 协程 vs 普通方法 vs Task 对比总表

| 维度 | 普通方法 | 协程 | `async Task` |
|------|---------|------|-------------|
| 是否可暂停续跑 | ❌ | ✅ | ✅ |
| 暂停语法 | — | `yield return ...` | `await ...` |
| 谁调度 | 调用者 | Unity 引擎（帧循环） | C# 线程池 / 同步上下文 |
| 返回值类型 | `void`/任意 | `IEnumerator` | `Task<T>` / `void` |
| 是否跑主线程 | ✅ | ✅（始终主线程） | 续跑可能跨线程（注意） |
| 异常处理 | try/catch 正常 | try/catch 正常 | `try/catch` + `async void` 的异常会炸 |
| 适合 | 同步逻辑 | 帧/时间驱动的时序 | IO、网络、跨线程 |

---

## 6.3 async/await 异步编程（进阶）

### 6.3.1 async/await 在 Unity 的现状

**【是什么】** C# 原生的异步语法。`async` 标记方法可暂停，`await` 等待一个 `Task`/`Awaitable` 完成后继续。

**【在 Unity 中的注意点】** Unity 主线程安装了自己的 `SynchronizationContext`，所以从主线程发起的 `await Task.Delay()` 续跑**会回到主线程**（线程池线程上触发完成，但续跑经同步上下文切回主线程，在**下一帧**的某次回调里执行）。真正的坑有两处：
1. **时序**：`Task` 续跑不在 `await` 那帧立即执行，而是延迟到下一帧/同步上下文执行点，可能错过 Update 节奏。
2. **起始线程**：若异步链在半途用 `ConfigureAwait(false)` 或在后台线程起步，续跑就不在主线程，此时碰 `transform` 才会报"只能在主线程访问"。
> 所以要"安全的、立即续跑的回主线程"，官方推荐用 `Awaitable`（2022.2+，同帧内续跑），或用 UniTask；原生 `Task` 需配合 `MainThreadDispatcher` 等才能保证。

**三条主流路线：**
| 路线 | 说明 | 版本 |
|------|------|------|
| 原生 `Task` + 手动回主线程 | 用 `await Task.Run(...)` 后手动切回，繁琐 | C# 5（Unity 2017+ 可用） |
| `Awaitable` | Unity 官方原生异步类型，自动回主线程 | ✅ Unity 2022.2+ |
| `UniTask` | 第三方库，零 GC、性能好、主线程保证 | 第三方，需导入 |

**【名称含义】** `async` = 异步；`await` = 等待。合起来："这个方法是异步的，跑到 `await` 先让出，好了再回来"。

**【代码示例：Console 级别理解】**
```csharp
// 纯 C# 层面：await Task.Delay 模拟网络/耗时
using System.Threading.Tasks;

async Task<bool> CheckLoginAsync(string name)
{
    Debug.Log("开始登录");
    await Task.Delay(500);      // 模拟 0.5 秒网络往返
    Debug.Log("登录完成");
    return name == "admin";
}

// 调用（注意：不是每处都要 await）
async void OnLoginButtonClick()   // 事件回调通常 async void
{
    bool ok = await CheckLoginAsync("admin");
    if (ok) Debug.Log("欢迎回来");
}
```

---

### 6.3.2 `async void` vs `async Task`（面试必考）

**【是什么】** 异步方法的两种返回类型，区别在**异常怎么传**和**能不能被 await**。

| 返回类型 | 可被 await？ | 异常 | 适用场景 |
|---------|:---:|------|---------|
| `async Task<T>` | ✅ | 异常被封装进 `Task`，可由调用方捕获 | **几乎所有业务方法** |
| `async Task` | ✅ | 同上（无返回值版本） | 不需要返回值的异步操作 |
| `async void` | ❌ | **异常直接抛到同步上下文，没人能接住，进程可能崩** | 仅限事件处理器/回调 |

**【代码示例】**
```csharp
// ✅ 推荐：返回 Task，调用方可 await / catch
async Task SaveAsync()
{
    await Task.Delay(100);
    throw new Exception("保存失败"); // 这个异常可以被调用方捕获
}

async void OnSaveButton()
{
    try
    {
        await SaveAsync();
    }
    catch (Exception e)
    {
        Debug.LogError($"保存失败：{e.Message}"); // ✅ 能接住
    }
}

// ❌ 危险：async void 的异常无人接住
async void BadSave()
{
    await Task.Delay(100);
    throw new Exception("boom"); // 直接炸掉，调用方无法 catch
}
```

**【规则一句话】** **能用 `Task` 就用 `Task`，`async void` 只留给事件处理器**（如 `Button.onClick.AddListener`）。这几乎是 Unity/C# 面试的必问题。

---

### 6.3.3 在 MonoBehaviour 里 await 协程 / 场景切换

**【是什么】** 原生 `Task` 无法直接 `await` 一个协程或 `AsyncOperation`，需要**手动封装**（`TaskCompletionSource`）。这是面试常考的"桥接"写法。

**【用途】** 在异步流程里等待"场景加载完成"、"动画播放完"、"协程跑完"。

**【代码示例：await 一个 AsyncOperation（场景切换）】**
```csharp
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

// 把 Unity 的 AsyncOperation 封装成可 await 的 Task
Task AwaitSceneLoad(AsyncOperation op)
{
    var tcs = new TaskCompletionSource<bool>();
    op.completed += _ => tcs.SetResult(true); // 加载完成回调里放行
    return tcs.Task;
}

async Task GoToBattleSceneAsync()
{
    AsyncOperation op = SceneManager.LoadSceneAsync("Battle");
    await AwaitSceneLoad(op);   // 切场景会打断 MonoBehaviour，但异步方法能继续
    Debug.Log("战斗场景加载完成");
}
```

【⚠️ 注意】**场景切换会销毁 MonoBehaviour**。`await` 续跑时如果脚本已随旧场景销毁，访问 `transform` 会报错。所以跨场景异步要么把代码放在 `DontDestroyOnLoad` 的单例上，要么用 `SceneManager.LoadSceneAsync` 完成回调继续。

---

### 6.3.4 `Awaitable`（✅ Unity 2022.2+ 引入，较新）

**【是什么】** Unity **官方原生的异步类型**，专门解决原生 `Task` 不自动回主线程、性能一般的问题。它自动在**主线程**续跑，可以直接 `await` 引擎对象。

**【名称含义】** `Awa`it（等待）+ `able`（可…的）= "可等待的东西"。

**【代码示例】**
```csharp
using UnityEngine;

// 直接用 Awaitable 等待 1 秒（主线程续跑，可安全访问 transform）
async void Demo()
{
    Debug.Log("起手");
    await Awaitable.WaitForSecondsAsync(1f);   // ✅ 官方延迟
    transform.Rotate(0f, 90f, 0f);             // 主线程，安全
    Debug.Log("1 秒后旋转完成");
}

// 等待场景加载（直接 await AsyncOperation，需 Unity 2023.1+；2023.1 前用 TaskCompletionSource 桥接）
async void LoadSceneAsync()
{
    await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Level2");
    Debug.Log("场景加载完，直接继续逻辑");
}
```

> ⚠️ 注意：直接 `await SceneManager.LoadSceneAsync(...)` 依赖 **`AsyncOperation` 实现 `GetAwaiter()`（Unity 2023.1 起提供）**，与 6.3 里 `Awaitable` 无关——`Awaitable`（2022.2+）只提供 `WaitForSecondsAsync`/`NextFrameAsync`/`EndOfFrameAsync` 等原语，**不会帮你 await 一个 AsyncOperation**。2023.1 前直接用 `op.completed += ...` 或协程 `yield return op`。

**【相似 API 区别】**
| | `Task.Delay` | `WaitForSeconds`（协程） | `Awaitable.WaitForSecondsAsync` |
|--|:---:|:---:|:---:|
| 语法 | `await` | `yield return` | `await` |
| 回主线程？ | ✅ 经同步上下文回主线程（下一帧执行） | ✅（始终主线程） | ✅（同帧立即续跑） |
| 受 `timeScale` 影响？ | ❌ 不受 | ✅ 受 | ❌ 不受 |
| 版本 | C# 通用 | 长期 | ✅ Unity 2022.2+ |

**【版本标记】** `Awaitable` 是 **Unity 2022.2 引入**的相对较新 API。2022 LTS 起可用，旧项目（2019/2020）不支持，需用 UniTask 或协程。

---

### 6.3.5 `UniTask`（第三方库）

**【是什么】** Cysharp 出品的**第三方**异步库，语法和 `Task` 几乎一样，但**零 GC、性能好、强制主线程、可替代协程**。Unity 社区事实标准。

**【怎么用】** 先通过 Package Manager 或 UPM 导入 `com.cysharp.unitask`。

**【代码示例】**
```csharp
using Cysharp.Threading.Tasks;

async UniTask AttackAsync()
{
    await UniTask.Delay(300);            // 类似 WaitForSeconds，主线程续跑
    Hit();
    await UniTask.Yield();               // 让出当前帧
    CheckCombo();
}

// 事件里可以用 async void 风格调 UniTask
void Start()
{
    AttackAsync().Forget();              // Forget()：不等待、不产生警告
}
```

**【名称含义】** `Uni`（统一/Unity 的）+ `Task`（任务）→ "Unity 特化的任务"。

**【版本标记】** 第三方，非官方内置。导入方式见官方 README。较新项目（2020+）配合 UniTask 是主流做法；2022.2+ 也可考虑原生 `Awaitable`。

---

### 6.3.6 用 `Task.Delay` 模拟延迟（Console 场景）

**【是什么】** 不依赖 Unity 引擎的纯 C# 延迟方式。适合**单元测试、控制台逻辑、编辑器工具**里模拟耗时。

**【代码示例】**
```csharp
// 在编辑器菜单/控制台工具里：
using System.Diagnostics;
using System.Threading.Tasks;

static async Task<int> FakeNetworkCallAsync()
{
    await Task.Delay(800);      // 模拟 800ms 网络延迟
    return 42;
}

static async Task RunAsync()
{
    var sw = Stopwatch.StartNew();
    int r = await FakeNetworkCallAsync();
    sw.Stop();
    Console.WriteLine($"结果 {r}，耗时 {sw.ElapsedMilliseconds}ms");
}
```

【⚠️ 注意】`Task.Delay` **不受 `Time.timeScale` 影响**；续跑经同步上下文回主线程（下一帧）。Unity 项目里做"游戏内延迟"首选比较合时机的 `Awaitable`/`UniTask` 或协程；`Task.Delay` 多用于编辑器/测试/纯逻辑。

---

## 6.4 C# Job System 简述（进阶）

### 6.4.1 `IJob` / `JobHandle` / `Schedule`

**【是什么】** Unity 的 C# Job System 让你**在多个工作线程上并行执行数据密集型计算**，配合 `Burst` 编译器可达到接近原生性能。

**【用途】** 大规模物理、海量单位寻路、粒子模拟、地形处理等"每个元素做同样计算"的场景。

**【核心成员】**
- `IJob`：定义一个"任务"（实现 `Execute()`）；
- `JobHandle`：任务的"把手"，用 `Schedule()` 调度后拿它；
- `Schedule()` / `Complete()`：派发任务 / 等待任务完成。

**【代码示例：并行移动 10000 个位置】**
```csharp
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// 1. 定义一个 Job（值类型，数据放字段里）
struct MoveJob : IJob
{
    public NativeArray<Vector3> positions;
    public float step;

    public void Execute()
    {
        for (int i = 0; i < positions.Length; i++)
            positions[i] += Vector3.right * step; // 纯数据操作，无 Unity API 调用
    }
}

void ScheduleParallelMove()
{
    var positions = new NativeArray<Vector3>(10000, Allocator.TempJob);
    var job = new MoveJob { positions = positions, step = 0.1f };

    JobHandle handle = job.Schedule(); // 2. 派发到工作线程
    handle.Complete();                 // 3. 等待完成（回到主线程后安全访问）
    // 用完后必须 Dispose，否则内存泄漏
    positions.Dispose();
}
```

**【名称含义】** `Job` = 任务，`Handle` = 句柄/把手，`Schedule` = 排程/调度。

**【版本标记】** C# Job System 于 **Unity 2018.1 引入**（配合 ECS 的 DOTS 生态）。属于**进阶**内容，新项目若不用 DOTS 可不深入，知道"并行 + 数据先行"的思想即可。

---

### 6.4.2 线程安全注意事项（必记）

**【核心规则】** Job 在工作线程上跑，**不能直接碰主线程的东西**：

| ❌ 不能在 Job 里做 | ✅ 应该怎么做 |
|-------------------|------------|
| 调用 `transform` / `GetComponent` | 只读写 `NativeArray` 等纯数据 |
| 调用 `Instantiate` / `Destroy` | 在 Job 里算结果，回主线程再实例化 |
| 调用 `Physics.Raycast`（普通版） | 用 `Physics.RaycastCommand` 等 Job 兼容版本 |
| 访问 Unity 对象（`GameObject`…） | 只传 `struct` 值数据，不传引用 |
| 多个 Job 同时写同一块数据 | 用 `NativeArray` 不同区段 / `IJobParallelFor` 分区 |

**【代码示例：并行读写同一数组的正确姿势】**
```csharp
// IJobParallelFor：每个元素一个线程槽位，天然避免写冲突
struct ParallelMoveJob : IJobParallelFor
{
    public NativeArray<Vector3> positions;
    public float step;

    public void Execute(int i)
    {
        positions[i] += Vector3.right * step; // 只写自己的槽位
    }
}
```

【⚠️ 注意】`NativeArray` 用 `Allocator.TempJob` 必须在主线程 `Dispose()`；忘释放会报 `LeakDetection` 警告甚至崩溃。Job 里永远不要 `Debug.Log`（跨线程打日志会卡且乱序）。

---

## 6.5 计时与耗时测量

### 6.5.1 `System.Diagnostics.Stopwatch` — 精确测耗时

**【是什么】** C# 自带的高精度秒表，基于高精度性能计数器，**与 Unity 帧率、timeScale 完全无关**。

**【用途】** 性能分析、单元测试断言、Profile 热点定位、编辑器工具计时。

**【名称含义】** `Stop`（停）+ `watch`（表）= 秒表。

**【代码示例】**
```csharp
using System.Diagnostics;

void MeasureSomething()
{
    Stopwatch sw = Stopwatch.StartNew(); // 创建并直接开跑

    HeavyComputation(); // 被测逻辑

    sw.Stop(); // 停表
    Debug.Log($"耗时：{sw.ElapsedMilliseconds} ms");
    // 更精细：sw.Elapsed.TotalMilliseconds / sw.ElapsedTicks
}
```

**【返回值/属性】**
- `sw.Elapsed` → `TimeSpan`（`TotalSeconds/TotalMilliseconds`…）；
- `sw.ElapsedMilliseconds` → `long`，毫秒；
- `sw.ElapsedTicks` → `long`，最细粒度。

---

### 6.5.2 用 `Time.time` 差值判断"用了多少时长"

**【是什么】** 简单场景下（游戏内、可容忍帧级精度），用两次 `Time.time` 相减即可测时长。**游戏内倒计时/冷却的通用做法**。

**【代码示例】**
```csharp
float lastAttackTime;

bool CanAttack()
{
    return Time.time - lastAttackTime >= 1.5f; // 冷却 1.5 秒
}

void OnAttack() { lastAttackTime = Time.time; }
```

**【相似 API 区别：三种测时长方式】**
| 方式 | 精度 | 受暂停影响 | 适用 |
|------|------|:---:|------|
| `Stopwatch` | 微秒级 | ❌ | 性能测量、调试 |
| `Time.time` 差值 | 帧级（~16ms） | ✅ | 游戏内冷却、计时 |
| `Time.realtimeSinceStartup` 差值 | 帧级 | ❌ | 真实经过时间（含暂停期间） |

---

### 6.5.3 老旧/易混方法说明：`WaitForSeconds` 与"RealTime"系列

**【是什么】** 这里澄清三个常被混淆的"时间"概念：

1. **`WaitForSeconds`（协程）**：老牌延时方法，稳定但**受 `timeScale` 影响**、每次 `new` 有 GC。新代码想"不受暂停"用 `WaitForSecondsRealtime`（同样老但不受缩放）。
2. **`WaitForSecondsRealtime`**：`WaitForSeconds` 的"真实时间"版本，不受 `timeScale` 影响。
3. **`Time.realtimeSinceStartup`**：真实秒数，性能测量常用（见 6.1.8）。

**【代码示例：两者的暂停差异】**
```csharp
// 暂停游戏（timeScale=0）后：
IEnumerator ScaledTimer()
{
    yield return new WaitForSeconds(1f);           // ❌ 永不结束（时间被冻结）
    Debug.Log("不会到这行");
}

IEnumerator RealtimeTimer()
{
    yield return new WaitForSecondsRealtime(1f);   // ✅ 真实 1 秒后照常继续
    Debug.Log("暂停中也能到，1 秒后执行");
}
```

**【版本标记】** `WaitForSeconds`/`WaitForSecondsRealtime`/`Time` 系列都是**长期稳定 API**，没有版本淘汰风险，面试直接引用即可。

---

## 6.6 本章高频「相似 API」对比总表

| 想做的事 | 用哪个 | 别用/注意 |
|---------|--------|----------|
| 帧独立移动 | `Time.deltaTime` | 别直接加固定值（帧相关） |
| 暂停整个游戏 | `Time.timeScale = 0` | 暂停后 `WaitForSeconds` 会卡住 |
| 暂停期间 UI 也要动 | `Time.unscaledDeltaTime` | `deltaTime` 暂停时为 0 |
| 延迟 n 秒 | 协程 `WaitForSeconds` | `Task.Delay` 不受 timeScale 影响 |
| 等一帧 | `yield return null` | `WaitForEndOfFrame` 更晚 |
| 异步方法 | `async Task` | `async void` 只用于事件，异常会炸 |
| 等待场景加载 | `SceneManager.LoadSceneAsync` + await（2023.1+ 可直接 await） | 协程也可，别在异步里碰已销毁的 transform |
| 高性能延迟/异步 | UniTask（第三方）/ `Awaitable`（2022.2+） | 原生 `Task` 续跑有帧延迟，`Awaitable` 同帧续跑更精准 |
| 并行计算 | `IJob` + `Schedule` | Job 里禁止碰 Unity 对象 |
| 测耗时 | `Stopwatch` | `Time.time` 是 float，长跑精度不够 |

---

## 6.7 版本标记（本章涉及的版本化 API）

| API | 标记 |
|-----|------|
| `Time` 系列（`deltaTime`/`time`/`timeScale`…） | 长期稳定 API |
| 协程（`StartCoroutine`/`WaitForSeconds`…） | 长期稳定 API（2017 前就存在） |
| `WaitUntil` / `WaitWhile` / `CustomYieldInstruction` | Unity 5.3+ 引入，长期稳定 |
| `async`/`await`（原生 `Task`） | 依赖 C#（Unity 2017 起 .NET 4.x 可用） |
| `Awaitable` | ✅ **Unity 2022.2 引入**（较新，2022 LTS 可用） |
| `UniTask` | 第三方库（非官方，需导入 Cysharp/UniTask） |
| C# Job System（`IJob`/`JobHandle`） | ✅ **Unity 2018.1 引入**（进阶/DOTS 生态） |
| `WaitForSecondsRealtime` | 长期稳定（和 `WaitForSeconds` 同代） |

> 具体版本细节以官方手册为准，这里只给推理方向。凡标 ✅ 的，面试时可强调"较新能力"。

---

## 6.8 本章小结

- **帧独立移动** = 用 `Time.deltaTime` 乘速度；帧率只影响调用次数，不影响累计位移。
- **暂停**用 `Time.timeScale = 0`；暂停期间想继续跑的东西用 `unscaled`/`realtimeSinceStartup`。
- **协程** = 用 `yield` 切成多段、由引擎按时间/条件续跑，始终主线程，适合游戏内延时与顺序逻辑。
- **async/await**：方法默认返回 `Task`；`async void` 只留给事件。原生 `Task` 续跑会回到主线程但有**下一帧延迟**，要**同帧精准续跑**用 `Awaitable`（2022.2+）或 UniTask（第三方）。
- **Job System** 是进阶并行：`IJob` + `JobHandle.Schedule`，只读写数据、不碰 Unity 对象。
- **测时长**：性能用 `Stopwatch`，游戏内冷却用 `Time.time` 差值，真实墙钟用 `realtimeSinceStartup`。

---

# 第 7 章 资源与场景

> **本章管辖**：Unity 里"资源从哪来、场景怎么切"——`Resources`、`AssetDatabase`、`Addressables`、`SceneManager`、`Object.Instantiate`。
> **一句话**：`Resources` 是"老式方便加载"，`AssetDatabase` 是"编辑器专用"，`Addressables` 是"官方新方案"，`SceneManager` 管场景切换，`Instantiate` 负责把资源克隆成活的物体。
> **前置**：建议先看 [README](../README.md) 了解词条模板，以及 [第 1 章](./第01章_核心物件体系.md) 的 `Object` 基类（`Instantiate`/`Destroy` 都来自它）。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 从 Resources 文件夹加载一个资源 | `Resources.Load<T>(path)` |
| 加载 Resources 下某目录的全部资源 | `Resources.LoadAll<T>(path)` |
| 卸载不再用的资源 | `Resources.UnloadUnusedAssets()` |
| 编辑器里按路径加载资源 | `AssetDatabase.LoadAssetAtPath<T>(path)` |
| 编辑器里按条件搜索资源 | `AssetDatabase.FindAssets(filter)` |
| 编辑器里创建资源文件 | `AssetDatabase.CreateAsset(obj, path)` |
| 编辑器里保存资源改动 | `AssetDatabase.SaveAssets()` |
| 同步加载场景 | `SceneManager.LoadScene(name)` |
| 异步加载场景（带进度条） | `SceneManager.LoadSceneAsync(name)` |
| 拿到当前活动场景 | `SceneManager.GetActiveScene()` |
| 场景加载完成时收到通知 | `SceneManager.sceneLoaded` 事件 |
| 用地址异步加载资源 | `Addressables.LoadAssetAsync<T>(key)` |
| 用地址异步实例化预制体 | `Addressables.InstantiateAsync(key)` |
| 释放 Addressables 加载的资源 | `Addressables.Release(handle)` |
| 克隆一个预制体/物体 | `Object.Instantiate(original)` |
| 克隆并指定位置旋转 | `Object.Instantiate(original, pos, rot)` |
| 克隆并挂到某父物体下 | `Object.Instantiate(original, parent)` |

---

## 7.1 Resources 系统（老 API，方便但打包全随包）

> **一句话**：把资源放进 `Assets/Resources` 文件夹，运行时就能用 `Resources.Load` 按路径加载。**方便，但打包时所有 Resources 里的东西都会打进包里**，无法按需下载，所以新项目官方推荐用 `Addressables`。

### 7.1.1 `Resources.Load<T>(string path)` 静态方法

**【是什么】** 从 `Assets/Resources`（或其子目录）按路径加载一个资源，返回指定类型 `T`。

**【用途】** 运行时加载预制体、材质、Sprite、TextAsset 等。适合小项目、原型、配置类资源。

**【名称含义】** `Resources` = 资源；`Load` = 加载。`<T>` 是你要的资源类型。

**【参数说明】**
- `path`：相对 `Resources` 文件夹的路径，**不带扩展名**（如 `"Enemies/Orc"`，不要写 `.prefab`）。路径用 `/` 分隔。

**【返回值】** 加载到的资源对象（类型 `T`）；路径不存在或类型不匹配返回 `null`。

**【代码示例】**
```csharp
using UnityEngine;

public class ResourceLoader : MonoBehaviour
{
    void Start()
    {
        // 加载预制体（路径不含扩展名）
        GameObject orcPrefab = Resources.Load<GameObject>("Enemies/Orc");
        if (orcPrefab != null)
        {
            Instantiate(orcPrefab, Vector3.zero, Quaternion.identity);
        }

        // 加载 Sprite
        Sprite icon = Resources.Load<Sprite>("UI/Icons/Heart");

        // 加载文本配置
        TextAsset config = Resources.Load<TextAsset>("Configs/GameConfig");
        Debug.Log(config.text);
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Resources.Load<T>` | 老 API，路径寻址，打包全随包，运行时可用 |
| `Addressables.LoadAssetAsync<T>` | 新方案，地址寻址，可远程/按需，异步（见 7.4） |

【⚠️ 注意】`Resources.Load` 是**同步**的，会阻塞主线程；且 `Resources` 文件夹里的东西**全部打进包**，项目一大就臃肿。新项目优先 `Addressables`。

---

### 7.1.2 `Resources.LoadAll<T>(string path)` 静态方法

**【是什么】** 加载 `Resources` 下某个路径的**全部**指定类型资源，返回数组。

**【用途】** 批量加载同一类资源（如所有敌人、所有关卡配置）。

**【参数说明】** `path`：`Resources` 下的目录路径（不含扩展名）；传空字符串 `""` 表示加载整个 `Resources` 根目录。

**【返回值】** `T[]` 数组；没有则返回空数组（不是 null）。

**【代码示例】**
```csharp
// 加载 Resources/Enemies 下所有预制体
GameObject[] enemies = Resources.LoadAll<GameObject>("Enemies");
foreach (var e in enemies)
{
    Debug.Log("敌人：" + e.name);
}
```

**【相似 API 区别】** `Load` 加载单个，`LoadAll` 加载一批；`LoadAll` 常用于"配置表/图集"批量读取。

---

### 7.1.3 `Resources.UnloadUnusedAssets()` 静态方法

**【是什么】** 卸载当前**没有被任何引用**的已加载资源，释放内存。

**【用途】** 内存优化，配合场景切换 / 资源释放使用。

**【返回值】** 返回 `AsyncOperation`（异步操作，可 `yield return` 等待完成）。

**【代码示例】**
```csharp
IEnumerator Cleanup()
{
    yield return Resources.UnloadUnusedAssets();   // 等它卸载完
    Debug.Log("未使用资源已清理");
}
```

【⚠️ 注意】它只卸载"确实没被引用"的资源；被 `Instantiate` 出来的物体、被脚本持有的引用都不会被卸载。别指望它解决所有内存问题。

---

### 7.1.4 隐藏 Resources 目录（Assets/Resources）

**【是什么】** `Resources` 是一个**特殊文件夹**，名字固定为 `Resources`，必须放在 `Assets` 下（`Assets/Resources`）。Unity 会把它的内容识别为"运行时可用资源"。

**【用途】** 让资源能被 `Resources.Load` 访问。

**【代码示例】** 目录结构：
```
Assets/
 └── Resources/            ← 特殊目录，运行时可用 Resources.Load 访问
     ├── Enemies/
     │   └── Orc.prefab
     └── UI/
         └── Icon.png
```

【⚠️ 注意】`Resources` 目录里的资源**全部打进包**，且**不能**用 `AssetDatabase` 之外的方式在编辑器外修改。隐藏/改名该目录会导致 `Resources.Load` 全部失效。

---

## 7.2 AssetDatabase（仅编辑器）

> **一句话定位**：`AssetDatabase` 是**编辑器专用**的资产数据库 API，用来在编辑器脚本（`Editor` 类、`MenuItem` 菜单）里加载、搜索、创建、保存资源。**运行时（打包后）不可用**。

### 7.2.1 `AssetDatabase.LoadAssetAtPath<T>(string path)` 静态方法

**【是什么】** 按**项目内完整路径**（含扩展名）加载一个资源。

**【用途】** 编辑器工具里加载资源（如批量处理、菜单工具）。

**【参数说明】** `path`：项目内路径，**含扩展名**，如 `"Assets/Enemies/Orc.prefab"`。

**【返回值】** 资源对象；路径无效返回 `null`。

**【代码示例】**
```csharp
using UnityEditor;

public class MyEditorTool
{
    [MenuItem("Tools/加载资源")]
    static void LoadAsset()
    {
        // 注意：路径含扩展名，且只能在编辑器里用
        GameObject orc = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Orc.prefab");
        if (orc != null) Debug.Log("加载到：" + orc.name);
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `AssetDatabase.LoadAssetAtPath` | 编辑器专用，路径含扩展名，可加载任意资源 |
| `Resources.Load` | 运行时可用，路径不含扩展名，只能加载 Resources 目录 |

---

### 7.2.2 `AssetDatabase.FindAssets(string filter)` 静态方法

**【是什么】** 按过滤条件搜索项目里的资源，返回 GUID 数组。

**【用途】** 编辑器工具里批量查找资源（如找所有带某标签的预制体）。

**【参数说明】** `filter`：搜索过滤串，如 `"t:Prefab"`（类型）、`"l:Tag"`（标签）、`"名字"`（名字关键字）。

**【返回值】** `string[]`，每个元素是一个资源的 **GUID**（不是路径）。

**【代码示例】**
```csharp
using UnityEditor;

[MenuItem("Tools/找所有预制体")]
static void FindPrefabs()
{
    string[] guids = AssetDatabase.FindAssets("t:Prefab");
    foreach (var guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);   // GUID → 路径
        Debug.Log(path);
    }
}
```

【⚠️ 注意】返回的是 GUID，要拿路径需用 `AssetDatabase.GUIDToAssetPath(guid)` 转换。

---

### 7.2.3 `AssetDatabase.CreateAsset(Object obj, string path)` 静态方法

**【是什么】** 在项目里创建一个资源文件（如 ScriptableObject、材质、动画）。

**【用途】** 编辑器工具里生成配置资源。

**【参数说明】**
- `obj`：要保存的资源对象（如 `ScriptableObject` 实例）。
- `path`：目标路径，含扩展名，如 `"Assets/Configs/MyConfig.asset"`。

**【返回值】** 无（void）。

**【代码示例】**
```csharp
using UnityEditor;

[MenuItem("Tools/创建配置")]
static void CreateConfig()
{
    var config = ScriptableObject.CreateInstance<MyConfig>();
    AssetDatabase.CreateAsset(config, "Assets/Configs/MyConfig.asset");
    AssetDatabase.SaveAssets();   // 保存
    AssetDatabase.Refresh();      // 刷新资源数据库
}
```

---

### 7.2.4 `AssetDatabase.SaveAssets()` 静态方法

**【是什么】** 保存所有未保存的资产改动到磁盘。

**【用途】** 在编辑器工具修改资源后调用，确保改动落盘。

**【返回值】** 无（void）。

**【代码示例】**
```csharp
AssetDatabase.SaveAssets();   // 保存所有改动
AssetDatabase.Refresh();      // 刷新，让改动在编辑器里生效
```

【⚠️ 注意】`AssetDatabase` 全家桶**只能在编辑器代码里用**（`using UnityEditor`），打包后的游戏运行时调用会报错。运行时加载资源请用 `Resources` 或 `Addressables`。

---

## 7.3 Scene 与场景加载（SceneManager）

> **一句话定位**：`SceneManager` 负责场景的加载、卸载、切换，以及场景加载事件。同步加载会卡住主线程，异步加载适合带进度条的大场景。

### 7.3.1 `SceneManager.LoadScene(string name)` 同步加载

**【是什么】** **同步**加载一个场景，加载期间主线程阻塞，加载完立即切换。

**【用途】** 小场景、加载很快的场景、简单切换。

**【参数说明】** `name`：场景名（Build Settings 里注册的场景名）或场景路径。

**【返回值】** 无（void）。

**【代码示例】**
```csharp
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene("Level2");   // 同步加载，会卡一下
        }
    }
}
```

【⚠️ 注意】同步加载会**阻塞主线程**，大场景会明显卡顿。加载前记得把场景加进 **Build Settings**，否则运行时找不到。

---

### 7.3.2 `SceneManager.LoadSceneAsync(string name)` 异步加载

**【是什么】** **异步**加载场景，返回 `AsyncOperation`，可配合进度条、`allowSceneActivation` 控制切换时机。

**【用途】** 大场景、带加载进度条、需要"加载完再切换"的场合。

**【参数说明】** `name`：场景名或路径。

**【返回值】** `AsyncOperation`（异步操作句柄）。

**【代码示例】**
```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AsyncLoader : MonoBehaviour
{
    public Slider progressBar;          // 进度条
    public Text progressText;

    void Start()
    {
        StartCoroutine(LoadLevel("Level3"));
    }

    IEnumerator LoadLevel(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;   // 先不自动切换，等进度到 100% 再切

        while (!op.isDone)
        {
            // progress 在 0~0.9 之间，最后 0.1 留给 allowSceneActivation
            float p = Mathf.Clamp01(op.progress / 0.9f);
            progressBar.value = p;
            progressText.text = (p * 100).ToString("F0") + "%";

            if (op.progress >= 0.9f)
            {
                // 加载完成，等玩家按任意键再真正切换
                if (Input.anyKeyDown)
                {
                    op.allowSceneActivation = true;   // 触发切换
                }
            }
            yield return null;
        }
    }
}
```

**【AsyncOperation 关键成员】**
- `isDone`：是否完成（`bool`）。
- `progress`：进度（`float`，0~1，但异步场景加载到 0.9 就停，最后 0.1 留给 `allowSceneActivation`）。
- `allowSceneActivation`：设为 `false` 时加载到 90% 暂停，等设为 `true` 才真正切换场景。

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `LoadScene` | 同步，阻塞主线程，加载完立即切换 |
| `LoadSceneAsync` | 异步，不阻塞，可做进度条、可控制切换时机 |

---

### 7.3.3 `SceneManager.GetActiveScene()` 静态方法

**【是什么】** 返回当前**活动场景**（`Scene` 结构体）。

**【用途】** 获取当前场景名、判断当前场景、场景内物体管理。

**【返回值】** `Scene` 结构体。

**【代码示例】**
```csharp
Scene current = SceneManager.GetActiveScene();
Debug.Log("当前场景：" + current.name);
Debug.Log("场景索引：" + current.buildIndex);
```

---

### 7.3.4 `SceneManager.sceneLoaded` 事件

**【是什么】** 场景加载完成时触发的事件（`UnityEngine.Events.UnityAction<Scene, LoadSceneMode>`）。

**【用途】** 场景加载后统一初始化、订阅回调。

**【代码示例】**
```csharp
void OnEnable()
{
    SceneManager.sceneLoaded += OnSceneLoaded;
}

void OnDisable()
{
    SceneManager.sceneLoaded -= OnSceneLoaded;
}

void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    Debug.Log("场景加载完成：" + scene.name + "，模式：" + mode);
}
```

【⚠️ 注意】记得在 `OnDisable` 里退订事件，避免重复订阅导致回调多次执行。

---

### 7.3.5 `SceneManager.sceneUnloaded` 事件

**【是什么】** 场景卸载完成时触发的事件。

**【用途】** 场景卸载后清理资源、重置状态。

**【代码示例】**
```csharp
SceneManager.sceneUnloaded += OnSceneUnloaded;
void OnSceneUnloaded(Scene scene)
{
    Debug.Log("场景已卸载：" + scene.name);
}
```

---

## 7.4 Addressables（官方新方案，地址寻址资源包）

> **一句话定位**：`Addressables`（Unity 2018.2+ 预览 / 2019.1+ 稳定，官方推荐）用**地址**（字符串 key）寻址资源，支持**按需加载、远程更新、依赖管理**。适合中大型项目、热更新、资源分包。需要安装 `Addressables` 包。

### 7.4.1 `Addressables.LoadAssetAsync<T>(object key)` 静态方法

**【是什么】** 按地址（key）**异步**加载一个资源，返回 `AsyncOperationHandle<T>`。

**【用途】** 运行时按地址加载资源（预制体、Sprite、TextAsset 等），支持远程资源。

**【参数说明】** `key`：资源的地址（Addressable 地址）或资源引用。

**【返回值】** `AsyncOperationHandle<T>`，通过 `.Completed` 回调或 `await` 拿结果。

**【代码示例】**
```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableLoader : MonoBehaviour
{
    async void Start()
    {
        // 异步加载，await 拿到结果
        AsyncOperationHandle<GameObject> handle =
            Addressables.LoadAssetAsync<GameObject>("Enemies/Orc");
        GameObject orcPrefab = await handle.Task;

        Instantiate(orcPrefab, Vector3.zero, Quaternion.identity);
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Resources.Load<T>` | 老 API，同步，打包全随包，路径寻址 |
| `Addressables.LoadAssetAsync<T>` | 新方案，异步，按需加载，可远程更新，地址寻址 |

---

### 7.4.2 `Addressables.InstantiateAsync<T>(string key)` 静态方法

**【是什么】** 异步实例化一个 Addressable 预制体，返回实例句柄。

**【用途】** 生成敌人、子弹、特效等需要克隆的物体。

**【参数说明】** `key`：预制体的地址。

**【返回值】** `AsyncOperationHandle<GameObject>`，`.Result` 是实例化的物体。

**【代码示例】**
```csharp
using UnityEngine.AddressableAssets;

public class EnemySpawner : MonoBehaviour
{
    async void SpawnEnemy()
    {
        var handle = Addressables.InstantiateAsync("Enemies/Orc", transform.position, Quaternion.identity);
        GameObject enemy = await handle.Task;
        enemy.name = "Orc_" + Time.frameCount;
    }
}
```

---

### 7.4.3 `Addressables.Release(AsyncOperationHandle handle)` 静态方法

**【是什么】** 释放 Addressables 加载/实例化的资源，归还引用计数。

**【用途】** 资源用完后释放，避免内存泄漏。

**【参数说明】** `handle`：之前 `LoadAssetAsync` / `InstantiateAsync` 返回的句柄。

**【返回值】** 无（void）。

**【代码示例】**
```csharp
AsyncOperationHandle<GameObject> handle;
GameObject loadedObj;

async void LoadAndRelease()
{
    handle = Addressables.LoadAssetAsync<GameObject>("Enemies/Orc");
    loadedObj = await handle.Task;
    // ... 使用 loadedObj ...

    // 用完后释放
    Addressables.Release(handle);
}
```

【⚠️ 注意】`InstantiateAsync` 实例化的物体，销毁时要用 `Addressables.ReleaseInstance(handle)` 而不是直接 `Destroy`，否则引用计数不归还。

---

### 7.4.4 `Addressables.Remote` 远程资源

**【是什么】** Addressables 支持把资源放到**远程服务器**（CDN），运行时按需下载，实现**热更新 / 分包**。

**【用途】** 大项目资源分包、版本更新、减少首包体积。

**【代码示例】**（配置层面，示意）
```
// 在 Addressables Groups 里把某个 Group 的 Load Path 设为远程 URL
// 运行时 Addressables 会自动下载并缓存远程资源
Addressables.LoadAssetAsync<GameObject>("RemoteGroup/NewWeapon");
```

**【何时用 Addressables】**
- 项目资源多、需要按需加载；
- 需要远程更新 / 热更新；
- 需要资源分包、控制首包体积；
- 需要引用计数管理、避免内存泄漏。

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Resources` | 老 API，全随包，同步，简单但臃肿 |
| `Addressables` | 新方案，按需/远程，异步，官方推荐 |

---

## 7.5 Object.Instantiate 与复制 Prefab

> **一句话定位**：`Object.Instantiate` 是**克隆**已有物体/预制体的核心方法，配合 `Prefab` 理解"模板 → 实例"的关系。

### 7.5.1 `Object.Instantiate(Object original)` 静态方法

**【是什么】** 克隆一个物体或预制体，生成一个**独立副本**。

**【用途】** 生成敌人、子弹、特效、UI 元素等。

**【参数说明】** `original`：要克隆的原始对象（预制体或场景里的物体）。

**【返回值】** 克隆出的新对象（类型与 `original` 相同）。

**【代码示例】**
```csharp
public GameObject enemyPrefab;   // 在 Inspector 里拖入预制体

void SpawnEnemy()
{
    GameObject enemy = Instantiate(enemyPrefab);   // 克隆一个敌人
    enemy.transform.position = new Vector3(0, 0, 5);
}
```

---

### 7.5.2 `Object.Instantiate(Object original, Vector3 position, Quaternion rotation)` 静态方法

**【是什么】** 克隆物体，并指定**位置和旋转**。

**【参数说明】**
- `original`：要克隆的物体。
- `position`：克隆体的世界坐标位置。
- `rotation`：克隆体的世界旋转。

**【返回值】** 克隆出的新物体。

**【代码示例】**
```csharp
public GameObject bulletPrefab;

void Fire()
{
    // 在枪口位置、朝枪口方向生成子弹
    GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
}
```

---

### 7.5.3 `Object.Instantiate(Object original, Transform parent)` 静态方法

**【是什么】** 克隆物体并挂到指定**父物体**下。

**【参数说明】** `parent`：父 `Transform`。

**【返回值】** 克隆出的新物体。

**【代码示例】**
```csharp
public GameObject itemPrefab;
public Transform inventoryPanel;   // UI 父物体

void AddItem()
{
    // 克隆到 inventoryPanel 下，作为其子物体
    GameObject item = Instantiate(itemPrefab, inventoryPanel);
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Instantiate` | 克隆已有物体/预制体，运行时生成实例 |
| `AssetBundle` | 打包加载资源（旧方案），不负责克隆，只负责加载 |

【⚠️ 注意】`Instantiate` 克隆的是**模板**，克隆出的副本与模板**互不影响**（改副本不会改模板）。配合 `Prefab` 理解：Prefab 是"模板"，`Instantiate` 是"按模板造实例"。

---

## 7.6 本章高频「相似 API」对比总表

| 想做的事 | 用哪个 | 别用哪个/注意 |
|---------|--------|--------------|
| 运行时加载资源 | `Resources.Load`（老）/ `Addressables`（新） | `AssetDatabase`（仅编辑器） |
| 编辑器加载资源 | `AssetDatabase.LoadAssetAtPath` | `Resources.Load`（运行时） |
| 加载场景 | `LoadScene`（同步）/ `LoadSceneAsync`（异步） | 同步会卡主线程 |
| 克隆物体 | `Object.Instantiate` | `new GameObject`（那是新建空壳） |
| 资源按需/远程 | `Addressables` | `Resources`（全随包） |
| 卸载资源 | `Resources.UnloadUnusedAssets` / `Addressables.Release` | 直接 `Destroy` 不归还引用计数 |

---

## 7.7 版本标记（本章涉及的版本化 API）

| API | 标记 |
|-----|------|
| `Resources.Load` / `LoadAll` / `UnloadUnusedAssets` | ⚠️ 老 API，方便但打包全随包，推荐 `Addressables` |
| `AssetDatabase` 系列 | 仅编辑器，稳定 |
| `SceneManager` 系列 | ✅ 稳定，长期可用 |
| `Addressables` 系列 | ✅ Unity 2018.2+（预览）/ 2019.1+（稳定）官方推荐资源管理方案 |
| `Object.Instantiate` | 老 API，稳定可用 |

> 具体版本细节以官方手册为准，这里只给推理方向。

---

## 7.8 本章小结

- `Resources`：老 API，方便但打包全随包，推荐 `Addressables`。
- `AssetDatabase`：**仅编辑器**，加载/搜索/创建/保存资源。
- `SceneManager`：`LoadScene` 同步、`LoadSceneAsync` 异步（带进度条、`allowSceneActivation`）。
- `Addressables`：官方新方案，地址寻址、按需/远程、异步加载。
- `Object.Instantiate`：克隆预制体/物体，配合 Prefab 理解"模板 + 实例"。
- 资源加载选型：小项目 `Resources`，中大型项目 `Addressables`。

---

## 7.9 Scene 结构体详解

> **一句话定位**：`Scene` 是一个**结构体**（不是类），描述"一个场景"的元信息：名字、路径、是否已加载、buildIndex 等。它**不持有场景里的物体**，只是场景的"身份证"。

### 7.9.1 `Scene.name` 属性

**【是什么】** 场景的名字（`string`），即场景文件名去掉 `.unity` 后缀。

**【用途】** 判断当前在哪个场景、日志输出场景名。

**【代码示例】**
```csharp
Scene current = SceneManager.GetActiveScene();
Debug.Log("当前场景名：" + current.name);   // 例如 "MainMenu"
```

---

### 7.9.2 `Scene.path` 属性

**【是什么】** 场景在项目里的完整路径（`string`），含扩展名，如 `"Assets/Scenes/Main.unity"`。

**【用途】** 需要按路径定位场景时用（如配合 `SceneManager.LoadScene(path)`）。

**【代码示例】**
```csharp
Scene current = SceneManager.GetActiveScene();
Debug.Log("场景路径：" + current.path);   // Assets/Scenes/Main.unity
```

---

### 7.9.3 `Scene.isLoaded` 属性

**【是什么】** 该场景是否**已加载**（`bool`，只读）。多场景同时加载时很有用。

**【用途】** 判断某个场景是否已经加载，避免重复加载。

**【代码示例】**
```csharp
Scene scene = SceneManager.GetSceneByName("Level2");
if (!scene.isLoaded)
{
    SceneManager.LoadScene("Level2", LoadSceneMode.Additive);   // 追加加载
}
```

---

### 7.9.4 `SceneManager.sceneCount` 静态属性

**【是什么】** 当前**已加载**的场景总数（`int`，只读）。注意它统计的是"已加载的场景"，不是 Build Settings 里注册的场景数。

**【用途】** 遍历所有已加载场景、判断多场景状态。

**【代码示例】**
```csharp
Debug.Log("当前已加载场景数：" + SceneManager.sceneCount);
for (int i = 0; i < SceneManager.sceneCount; i++)
{
    Scene s = SceneManager.GetSceneAt(i);
    Debug.Log("第 " + i + " 个：" + s.name);
}
```

---

### 7.9.5 `SceneManager.GetSceneAt(int index)` 静态方法

**【是什么】** 按索引取回**已加载**场景列表中的第 `index` 个 `Scene`。

**【参数说明】** `index`：0 到 `sceneCount - 1` 的索引。

**【返回值】** `Scene` 结构体；索引越界返回无效 Scene（`IsValid()` 为 `false`）。

**【代码示例】**
```csharp
// 遍历所有已加载场景，找到名为 "UI" 的那个
for (int i = 0; i < SceneManager.sceneCount; i++)
{
    Scene s = SceneManager.GetSceneAt(i);
    if (s.name == "UI")
    {
        Debug.Log("找到 UI 场景，路径：" + s.path);
    }
}
```

---

### 7.9.6 `SceneManager.SetActiveScene(Scene scene)` 静态方法

**【是什么】** 把指定场景设为**活动场景**（active scene）。活动场景决定新 `Instantiate` 的物体默认挂到哪个场景。

**【用途】** 多场景加载时，把新生成的物体放进目标场景。

**【代码示例】**
```csharp
Scene target = SceneManager.GetSceneByName("Gameplay");
SceneManager.SetActiveScene(target);   // 之后 Instantiate 的物体默认进 Gameplay

GameObject enemy = Instantiate(enemyPrefab);   // 会挂到 Gameplay 场景
```

【⚠️ 注意】`SetActiveScene` 传入的场景必须**已加载**，否则报错。活动场景通常只有一个。

---

## 7.10 AsyncOperation 详解（异步操作句柄）

> **一句话定位**：`AsyncOperation` 是 Unity 异步操作的**通用句柄**，`LoadSceneAsync`、`Resources.UnloadUnusedAssets`、`AssetBundle.LoadFromFileAsync` 等都返回它。它既能 `yield return` 等待，也能用 `completed` 事件回调。

### 7.10.1 `isDone` 属性

**【是什么】** 操作是否**完全完成**（`bool`，只读）。

**【用途】** 轮询判断异步操作是否结束。

### 7.10.2 `progress` 属性

**【是什么】** 操作进度（`float`，0~1，只读）。

**【注意】** 场景异步加载时 `progress` 到 `0.9` 就停住，最后 `0.1` 留给 `allowSceneActivation` 触发切换。所以做进度条要 `Mathf.Clamp01(op.progress / 0.9f)` 归一化。

### 7.10.3 `allowSceneActivation` 属性

**【是什么】** 是否允许场景在加载完成后**自动激活**（`bool`，可写，默认 `true`）。

**【用途】** 设为 `false` 时加载到 90% 暂停，等玩家确认（如按任意键）再设回 `true` 真正切换。**只有场景加载的 AsyncOperation 有这个属性**，其他异步操作没有。

### 7.10.4 `priority` 属性

**【是什么】** 异步操作的**优先级**（`int`，可写）。数值越大越优先执行。

**【用途】** 多个异步操作同时进行时，让重要的先完成。

**【代码示例】**
```csharp
AsyncOperation op = SceneManager.LoadSceneAsync("Level3");
op.priority = 100;   // 提高优先级
```

### 7.10.5 `completed` 事件（现代写法，代替 yield）

**【是什么】** 操作完成时触发的事件（`Action<AsyncOperation>`）。**这是比 `yield return` 更现代的写法**，不用协程也能拿到完成回调。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class AsyncWithEvent : MonoBehaviour
{
    void Start()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("Level3");
        op.completed += OnLoadDone;   // 订阅完成事件
    }

    void OnLoadDone(AsyncOperation op)
    {
        Debug.Log("场景加载完成！");
        // 这里做加载后的初始化
    }
}
```

**【相似 API 区别】**
| 写法 | 区别 |
|------|------|
| `yield return op` | 协程写法，需要 `IEnumerator` + `StartCoroutine` |
| `op.completed += 回调` | 事件写法，不用协程，适合非 MonoBehaviour 或回调风格 |
| `await op`（配合 Task） | 异步方法写法，需要 `async` 上下文；**直接 `await` 需 Unity 2023.1+**（AsyncOperation 实现 GetAwaiter），更早需 TaskCompletionSource 桥接 |

---

## 7.11 Addressables 事件回调与 AssetReference

### 7.11.1 `AsyncOperationHandle<T>.Completed` 事件

**【是什么】** Addressables 异步操作完成时触发的事件，回调参数是 `AsyncOperationHandle<T>`，通过 `.Result` 拿结果、`.Status` 判断成功失败。

**【用途】** 不用 `await` 也能拿到加载结果，适合回调风格、事件驱动代码。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableCallback : MonoBehaviour
{
    void Start()
    {
        // 用 onComplete 回调加载地址资源
        Addressables.LoadAssetAsync<GameObject>("Enemies/Orc").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject orc = handle.Result;
                Instantiate(orc, Vector3.zero, Quaternion.identity);
            }
            else
            {
                Debug.LogError("加载失败：" + handle.OperationException);
            }
        };
    }
}
```

### 7.11.2 `AsyncOperationHandle.IsValid()` 方法

**【是什么】** 判断这个句柄是否**仍然有效**（`bool`）。句柄被 `Release` 后 `IsValid()` 返回 `false`。

**【用途】** 释放前检查，避免对已释放句柄操作报错。

**【代码示例】**
```csharp
AsyncOperationHandle<GameObject> handle;

void Update()
{
    // 句柄有效才继续使用
    if (handle.IsValid())
    {
        // ... 使用 handle.Result ...
    }
}
```

### 7.11.3 `AssetReference` 字段（Inspector 拖拽绑定）

**【是什么】** `AssetReference` 是一个**可序列化字段类型**，在 Inspector 里**直接拖拽**一个 Addressable 资源就能绑定，不用手写地址字符串。

**【用途】** 避免硬编码地址字符串，改资源时不用改代码，更安全。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;

public class WeaponSpawner : MonoBehaviour
{
    // 在 Inspector 里把 Addressable 预制体拖进来
    public AssetReference weaponRef;

    async void Spawn()
    {
        // 用 AssetReference 加载，不用写字符串地址
        var handle = weaponRef.LoadAssetAsync<GameObject>();
        GameObject weapon = await handle.Task;
        Instantiate(weapon, transform.position, transform.rotation);
    }
}
```

【⚠️ 注意】`AssetReference` 拖拽的资源**必须已标记为 Addressable**，否则 Inspector 里拖不进去或加载失败。

---

## 7.12 `Resources.UnloadAsset` 与 `Resources.UnloadUnusedAssets` 的区别

| API | 区别 |
|-----|------|
| `Resources.UnloadAsset(Object asset)` | 卸载**单个指定**资源（如一个 Texture、Mesh），立即释放 |
| `Resources.UnloadUnusedAssets()` | 卸载**所有**没被引用的资源，异步，返回 `AsyncOperation` |

**【代码示例】**
```csharp
// 卸载单个资源（同步，立即）
Texture2D tex = Resources.Load<Texture2D>("UI/Icons/Heart");
Resources.UnloadAsset(tex);   // 只卸载这一个

// 卸载所有未引用资源（异步，等它完成）
IEnumerator CleanupAll()
{
    yield return Resources.UnloadUnusedAssets();
    Debug.Log("全部未引用资源已清理");
}
```

【⚠️ 注意】`UnloadAsset` 只能卸载**从 Resources 加载**的资源，且**不能**卸载场景里的物体、`Instantiate` 出来的实例、或 `Sprite`（Sprite 是 `Object` 但 `UnloadAsset` 对它有特殊限制，通常用 `UnloadUnusedAssets`）。`UnloadUnusedAssets` 是异步的，会扫描整个场景，频繁调用有性能开销。

---

## 7.13 AssetBundle 与 Addressables / Resources 对比

> **一句话**：`AssetBundle` 是 Unity 最**底层**的资源打包方案，`Addressables` 是建立在它之上的**高层封装**，`Resources` 是最简单的老方案。三者关系：`Addressables` 内部就是用 `AssetBundle` 实现的。

| 维度 | `Resources` | `AssetBundle` | `Addressables` |
|------|-------------|---------------|----------------|
| 加载方式 | 同步 `Load` | 同步/异步 `LoadFromFile` | 异步 `LoadAssetAsync` |
| 寻址方式 | 路径（`Resources` 目录内） | 无内置寻址，自己管路径/依赖 | 地址（字符串 key） |
| 打包 | 全随包，无法按需 | 手动分包，可远程 | 自动分包，可远程/热更新 |
| 依赖管理 | 无 | **手动**处理依赖（很麻烦） | **自动**处理依赖 |
| 引用计数 | 无 | 手动 `Unload` | 自动 `Release` |
| 学习成本 | 低 | 高（依赖、变体、加载时机） | 中 |
| 适用场景 | 小项目/原型 | 老项目/深度定制 | 中大型项目/热更新 |

**【AssetBundle 手动加载示例】**
```csharp
using UnityEngine;

public class BundleLoader : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadFromBundle());
    }

    IEnumerator LoadFromBundle()
    {
        // 从本地文件异步加载 AssetBundle
        AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, "mybundle"));
        yield return req;

        AssetBundle bundle = req.assetBundle;
        if (bundle == null) yield break;

        // 从 bundle 里加载资源
        AssetBundleRequest assetReq = bundle.LoadAssetAsync<GameObject>("Orc");
        yield return assetReq;

        GameObject orc = assetReq.asset as GameObject;
        Instantiate(orc);

        // 用完后卸载（注意：卸载会销毁已加载的实例引用）
        bundle.Unload(false);
    }
}
```

【⚠️ 注意】`AssetBundle` 的**依赖管理**是最大痛点：一个 bundle 依赖另一个 bundle 时，必须手动 `LoadFromFile` 所有依赖，顺序错了就加载失败。`Addressables` 正是为了解决这个痛点而生的，所以**新项目直接用 `Addressables`，别自己造 AssetBundle 轮子**。

---

## 7.14 完整代码示例集（综合实战）

### 7.14.1 地址资源加载 + onComplete 回调 + 释放

```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManager : MonoBehaviour
{
    private AsyncOperationHandle<GameObject> _handle;

    public void LoadEnemy(string address)
    {
        // 用 onComplete 回调加载，不阻塞主线程
        _handle = Addressables.LoadAssetAsync<GameObject>(address);
        _handle.Completed += OnEnemyLoaded;
    }

    private void OnEnemyLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("加载失败：" + handle.OperationException);
            return;
        }

        GameObject enemy = handle.Result;
        Instantiate(enemy, Vector3.zero, Quaternion.identity);
    }

    private void OnDestroy()
    {
        // 场景销毁时释放，归还引用计数
        if (_handle.IsValid())
        {
            Addressables.Release(_handle);
        }
    }
}
```

### 7.14.2 AsyncOperation 异步加载场景 + 进度条

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public Slider progressBar;
    public Text progressText;

    public void LoadLevel(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;   // 先不切换，等进度满

        // ⚠️ 注意：allowSceneActivation=false 时 op.isDone 恒为 false，
        //    completed 事件不会触发！所以这里不要用 completed 收尾，
        //    而是用协程轮询 isDone（见下）。进度满后调用 op.allowSceneActivation = true 真正切换。
        StartCoroutine(UpdateProgress(op));
    }

    private IEnumerator UpdateProgress(AsyncOperation op)
    {
        while (!op.isDone)
        {
            // 归一化进度（0.9 是加载完成，最后 0.1 留给激活）
            float p = Mathf.Clamp01(op.progress / 0.9f);
            progressBar.value = p;
            progressText.text = (p * 100).ToString("F0") + "%";
            if (p >= 1f)
            {
                // 进度到顶 → 触发场景切换
                op.allowSceneActivation = true;
            }
            yield return null;
        }
        // 到这里场景已切换完成
        Debug.Log("场景加载并激活完成");
    }
}
```

> 如果想用 `completed` 事件，就不要设 `allowSceneActivation = false`（也就是让 `op` 正常自动激活），否则 `isDone` 永远为 false、事件永不触发。

### 7.14.3 AssetReference 拖拽绑定 + 实例化 + 释放实例

```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Spawner : MonoBehaviour
{
    // Inspector 里直接拖 Addressable 预制体
    public AssetReference enemyRef;

    private GameObject _spawned;

    public async void Spawn()
    {
        // 用 AssetReference 加载并实例化
        var handle = enemyRef.InstantiateAsync(transform.position, Quaternion.identity);
        _spawned = await handle.Task;

        // 用完后释放实例（不是直接 Destroy）
        Addressables.ReleaseInstance(handle);
    }
}
```

---

## 7.15 本章高频「相似 API」对比总表（补充）

| 想做的事 | 用哪个 | 别用哪个/注意 |
|---------|--------|--------------|
| 判断场景是否已加载 | `Scene.isLoaded` | `SceneManager.GetActiveScene`（只拿活动场景） |
| 遍历所有已加载场景 | `SceneManager.sceneCount` + `GetSceneAt` | 别用 `GetActiveScene`（只有一个） |
| 设置新物体挂载场景 | `SceneManager.SetActiveScene` | 目标场景必须已加载 |
| 异步操作完成回调 | `op.completed` 事件 | 老写法 `yield return op` |
| 卸载单个资源 | `Resources.UnloadAsset` | `UnloadUnusedAssets`（那是全量异步） |
| 资源分包/热更新 | `Addressables` | 手写 `AssetBundle`（依赖管理太麻烦） |
| Inspector 绑定地址资源 | `AssetReference` 字段 | 硬编码字符串地址 |

---

﻿# 第 8 章 渲染与视觉

> **本章管辖**：Unity 里"怎么把东西画出来"的整条链路——`Renderer`（谁负责画）、`Material`（用什么画）、`Camera`（从哪看）、`Light`（怎么照亮）、`Shader`（画成什么样）、后期处理（画完再加工）。
> **一句话**：`Renderer` 决定"画不画、画在哪"，`Material` 决定"用什么参数画"，`Shader` 决定"怎么算像素"，`Camera` 决定"从哪个角度拍"，`Light` 决定"亮不亮"。
> **前置**：建议先看 [第 1 章 核心物件体系](第01章_核心物件体系.md) 了解 `GameObject`/`Component` 基础，再看 [第 2 章 Transform 与坐标系](第02章_Transform与坐标系.md) 理解坐标转换。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 让物体可见/不可见 | `renderer.enabled` |
| 拿到物体渲染的包围盒 | `renderer.bounds` |
| 判断物体是否在相机视野内 | `renderer.isVisible` / `OnBecameVisible` / `OnBecameInvisible` |
| 改材质颜色/贴图（不改共享） | `renderer.material.SetColor(...)` |
| 改材质且想所有实例一起变 | `renderer.sharedMaterial` |
| 高性能批量改材质参数 | `MaterialPropertyBlock` |
| 新建一个材质 | `new Material(shader)` |
| 按名字找 Shader | `Shader.Find("...")` |
| 屏幕坐标转世界射线（拾取） | `camera.ScreenPointToRay(Input.mousePosition)` |
| 世界坐标 → 屏幕坐标 | `camera.WorldToScreenPoint(worldPos)` |
| 世界坐标 → 视口坐标 | `camera.WorldToViewportPoint(worldPos)` |
| 拿到主相机 | `Camera.main` |
| 只渲染某些层 | `camera.cullingMask` |
| 切换正交/透视 | `camera.orthographic` |
| 控制视野角度 | `camera.fieldOfView` |
| 加一盏灯 | `Light` 组件 + `light.type` |
| 后期处理（模糊/泛光/色调） | URP 的 `Volume` + `VolumeProfile` |

---

## 8.1 Renderer（渲染器基类）

### 8.1.1 `class Renderer : Component` — 渲染器基类

**【是什么】** 所有"负责把物体画到屏幕上"的组件的**基类**。它自己不直接画，而是定义了一套"怎么画"的公共接口（材质、可见性、包围盒、排序），具体画法由子类决定。

**【用途】** 只要你想控制"这个物体画不画、用什么材质、占多大空间、在不在视野里"，都通过 `Renderer` 或其子类操作。

**【名称含义】** `Renderer` = **Render**（渲染/绘制）+ **-er**（做…的东西）→ "负责渲染的那个组件"。

**【核心成员】**
- `material` / `sharedMaterial`：材质（见 8.1.3）。
- `enabled`：是否渲染。
- `bounds`：世界空间包围盒。
- `isVisible`：是否被任何相机看到。
- `sharedMaterials`：多材质数组（一个网格可挂多个材质）。

**【代码示例】**
```csharp
// 拿到物体上的渲染器（MeshRenderer / SpriteRenderer / SkinnedMeshRenderer 都继承它）
Renderer r = GetComponent<Renderer>();
r.enabled = false;          // 隐藏但不销毁
Debug.Log(r.bounds.size);   // 世界空间包围盒尺寸
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Renderer`（基类） | 通用接口，不关心是网格还是精灵 |
| `MeshRenderer` | 画**静态网格**（Mesh），配 `MeshFilter` |
| `SkinnedMeshRenderer` | 画**骨骼动画网格**（角色），顶点随骨骼变形 |
| `SpriteRenderer` | 画**2D 精灵**（Sprite），配 `Sprite` 资源 |

---

### 8.1.2 `Renderer.enabled` 属性

**【是什么】** 布尔属性，控制该渲染器是否参与渲染。

**【用途】** 让物体"看不见"但**逻辑仍在运行**（区别于 `SetActive(false)` 会连 `Update` 一起停掉）。

**【代码示例】**
```csharp
Renderer r = GetComponent<Renderer>();
r.enabled = false;   // 物体消失，但脚本照常跑
r.enabled = true;    // 重新显示
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `renderer.enabled = false` | 只关渲染，逻辑照跑，性能开销小 |
| `gameObject.SetActive(false)` | 整个物体停用，`Update` 也不跑（见第 1 章） |

---

### 8.1.3 `Renderer.material` 与 `Renderer.sharedMaterial`

**【是什么】** 两个都返回材质，但**语义完全不同**：
- `material`：返回一个**该渲染器专属的材质实例**。第一次访问时 Unity 会**自动克隆**一份共享材质给你，之后你改它只影响这一个物体。
- `sharedMaterial`：返回**所有实例共享的那份材质**。你改它，所有用这份材质的物体一起变。

**【用途】** 想"只改这一个物体"用 `material`；想"批量改所有同材质物体"用 `sharedMaterial`。

**【名称含义】** `shared` = 共享的。`material` 是"我的副本"，`sharedMaterial` 是"大家共用的那份"。

**【代码示例】**
```csharp
Renderer r = GetComponent<Renderer>();

// 只改自己：material 会自动克隆，不影响别人
r.material.color = Color.red;

// 改所有人：sharedMaterial 是共享的，所有用这份材质的物体一起变红
r.sharedMaterial.color = Color.blue;
```

**【相似 API 区别】**
| API | 行为 | 性能 | 适用 |
|-----|------|------|------|
| `renderer.material` | 首次访问**自动克隆**，改它只影响本物体 | 每次访问可能产生新实例（GC 压力） | 单个物体独立变色 |
| `renderer.sharedMaterial` | 直接改**共享材质**，影响所有使用者 | 无克隆开销 | 批量统一改、编辑器调参 |

【⚠️ 注意】`material` 每次访问都可能触发克隆，**不要在 `Update` 里反复读 `material`**，否则每帧产生新材质实例，内存和 GC 都会爆炸。要改就缓存一份引用。

---

### 8.1.4 `Renderer.bounds` 属性

**【是什么】** 返回该渲染器在**世界空间**的轴对齐包围盒（AABB），类型 `Bounds`。

**【用途】** 判断物体大小、做剔除、做范围检测（比如"物体是否进入某区域"）。

**【代码示例】**
```csharp
Renderer r = GetComponent<Renderer>();
Bounds b = r.bounds;
Vector3 center = b.center;   // 包围盒中心（世界坐标）
Vector3 size   = b.size;     // 包围盒尺寸
float halfH   = b.extents.y; // 半高
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `renderer.bounds` | 世界空间包围盒，随物体移动/缩放实时更新 |
| `collider.bounds` | 碰撞器包围盒（见第 5 章），与渲染盒可能不同 |

---

### 8.1.5 `Renderer.isVisible` 与 `OnBecameVisible` / `OnBecameInvisible`

**【是什么】**
- `isVisible`：布尔属性，表示该渲染器**是否被任何相机看到**（在任一相机的剔除范围内）。
- `OnBecameVisible()` / `OnBecameInvisible()`：**生命周期回调**，当物体进入/离开所有相机视野时被引擎调用。

**【用途】** 视野剔除优化：物体离开视野时暂停昂贵逻辑（如 AI、粒子），进入时恢复。

**【代码示例】**
```csharp
public class CullLogic : MonoBehaviour
{
    void OnBecameVisible()   { Debug.Log("进入视野，恢复逻辑"); }
    void OnBecameInvisible() { Debug.Log("离开视野，暂停逻辑"); }
}
```

【⚠️ 注意】`isVisible` 依赖相机剔除，**编辑器 Scene 视图也算一个相机**，所以编辑器里可能一直为 true。运行时以 Game 视图为准。

---

### 8.1.6 `MeshRenderer` / `SkinnedMeshRenderer` / `SpriteRenderer` 对比

**【是什么】** 三个最常用的 `Renderer` 子类，分别服务不同渲染场景。

| 子类 | 画什么 | 配什么 | 典型用途 |
|------|--------|--------|---------|
| `MeshRenderer` | 静态网格（顶点固定） | `MeshFilter` + `Mesh` | 场景模型、道具、建筑 |
| `SkinnedMeshRenderer` | 骨骼动画网格（顶点随骨骼动） | 网格（Mesh） + `Animator` | 角色、怪物、会动的生物 |
| `SpriteRenderer` | 2D 精灵图 | `Sprite` 资源 | 2D 游戏角色、UI 元素、粒子 |

**【代码示例】**
```csharp
// 三种渲染器都继承 Renderer，都能用 material / enabled / bounds
MeshRenderer mr = GetComponent<MeshRenderer>();
SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
SpriteRenderer sr = GetComponent<SpriteRenderer>();

// SpriteRenderer 特有：排序
sr.sortingOrder = 10;   // 2D 里控制绘制先后（见 8.7）
```

**【版本标记】** 三者均为**稳定老 API**，各版本通用。

---

## 8.2 Material（材质）

### 8.2.1 `class Material : Object` — 材质

**【是什么】** 材质是"Shader + 一组参数"的打包体。它告诉渲染器：用哪个 Shader，以及这个 Shader 需要的颜色、贴图、数值等参数。

**【名称含义】** `Material` = 材质。在 Unity 里它**不是**"物理材质"（那是 `PhysicMaterial`，见第 5 章），而是"渲染材质"。

**【用途】** 控制物体外观：颜色、贴图、金属度、粗糙度、发光等。

**【代码示例】**
```csharp
// 用 Shader.Find 找到内置 Shader 并新建材质
Shader shader = Shader.Find("Universal Render Pipeline/Lit");
Material mat = new Material(shader);
mat.color = Color.green;   // 设置主色
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Material` | 渲染材质，控制外观 |
| `PhysicMaterial` | 物理材质，控制摩擦/弹性（见第 5 章） |

---

### 8.2.2 `new Material(Shader shader)` 构造

**【是什么】** 用指定 Shader 创建一个新的材质实例。

**【参数说明】** `shader`：要使用的 `Shader` 对象（可用 `Shader.Find` 获取）。

**【代码示例】**
```csharp
// 方式一：先 Find 再 new
Shader s = Shader.Find("Universal Render Pipeline/Lit");
Material m = new Material(s);

// 方式二：直接一步到位
Material m2 = new Material(Shader.Find("Universal Render Pipeline/Lit"));
```

**【返回值】** 新建的 `Material` 实例。

---

### 8.2.3 `Material.SetColor` / `SetFloat` / `SetVector` / `SetTexture` / `SetMatrix`

**【是什么】** 一组"给材质参数赋值"的方法，参数名对应 Shader 里声明的属性名。

| 方法 | 设置类型 | 典型属性名 |
|------|---------|-----------|
| `SetColor(name, Color)` | 颜色 | `"_Color"` |
| `SetFloat(name, float)` | 浮点数 | `"_Glossiness"` `"_Metallic"` |
| `SetVector(name, Vector4)` | 四维向量 | `"_EmissionColor"` |
| `SetTexture(name, Texture)` | 贴图 | `"_MainTex"` |
| `SetMatrix(name, Matrix4x4)` | 矩阵 | 自定义 Shader 用 |

**【名称含义】** `Set` = 设置；`Color/Float/Vector/Texture/Matrix` = 数据类型。属性名以 `_` 开头是 Unity Shader 的**命名约定**。

**【代码示例】**
```csharp
Renderer r = GetComponent<Renderer>();
Material m = r.material;   // 注意：用 material 拿副本，别用 sharedMaterial 改全局

m.SetColor("_Color", Color.yellow);          // 改主色
m.SetFloat("_Metallic", 0.8f);               // 改金属度
m.SetTexture("_MainTex", myTexture);         // 换主贴图
m.SetVector("_EmissionColor", new Vector4(1, 0.5f, 0, 1)); // 发光色
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `material.SetColor("_Color", c)` | 通用写法，属性名是字符串 |
| `material.color` | 快捷属性，等价于 `SetColor("_Color", ...)`，只对 `_Color` 有效 |

---

### 8.2.4 材质属性命名约定（`_Color` `_MainTex`）

**【是什么】** Shader 里每个可调参数都有一个**属性名**，Unity 约定以 `_` 开头。代码里用字符串引用它。

**【常用内置属性名】**
| 属性名 | 含义 |
|--------|------|
| `_Color` | 主颜色 |
| `_MainTex` | 主贴图 |
| `_Metallic` | 金属度（PBR） |
| `_Glossiness` | 光滑度/粗糙度 |
| `_EmissionColor` | 自发光颜色 |
| `_BumpMap` | 法线贴图 |

**【代码示例】**
```csharp
// 属性名是字符串，写错不会报编译错，但运行时无效
m.SetColor("_Color", Color.white);   // ✅ 正确
m.SetColor("_Colour", Color.white); // ❌ 拼错，静默失败
```

【⚠️ 注意】属性名是字符串，**拼写错误不会编译报错**，只会运行时无效。建议把属性名定义为常量。

---

### 8.2.5 `MaterialPropertyBlock`（性能优化）

**【是什么】** 一个"批量设置材质参数"的容器，**不创建新材质实例**，而是把参数直接传给 GPU 的渲染批次。

**【用途】** 当你有**大量同材质物体**（比如 1000 棵树、1000 个敌人），每个都要不同颜色时，用 `MaterialPropertyBlock` 避免为每个物体克隆材质，从而**保住合批（Batching）**，大幅提升性能。

**【名称含义】** `Property`（属性）+ `Block`（块）→ "一组属性块"，一次性打包传给渲染器。

**【代码示例】**
```csharp
// 用 MaterialPropertyBlock 给每个物体独立颜色，而不克隆材质
MaterialPropertyBlock block = new MaterialPropertyBlock();

foreach (var tree in allTrees)
{
    block.SetColor("_Color", Random.ColorHSV());
    tree.SetPropertyBlock(block);   // 把参数块塞给渲染器
}
```

**【相似 API 区别】**
| 对比 | `renderer.material` | `MaterialPropertyBlock` |
|------|--------------------|------------------------|
| 是否克隆材质 | 是，每物体一份 | 否，共享同一材质 |
| 内存 | 高（材质多） | 低 |
| 合批 | 破坏合批 | 保留合批 |
| 适用 | 少量物体独立外观 | 大量物体独立外观 |

> **【版本标记】** `MaterialPropertyBlock` 是**稳定老 API**（Unity 5.0 即存在），相对"每物体克隆材质"的老做法是批量渲染大量同材质物体的**强烈推荐**方案。

---

## 8.3 Camera（相机）

### 8.3.1 `class Camera : Behaviour` — 相机

**【是什么】** 相机决定"从哪个位置、哪个角度、用什么投影方式"把场景画到屏幕上。场景里可以有多个相机，每个相机渲染到不同目标。

**【名称含义】** `Camera` = 相机/摄像机。

**【代码示例】**
```csharp
Camera cam = GetComponent<Camera>();
cam.fieldOfView = 60f;      // 透视相机视野角度
cam.orthographic = false;    // 透视投影（3D 默认）
```

---

### 8.3.2 `Camera.main` 静态属性

**【是什么】** 返回场景中**标记为 `MainCamera`（Tag）** 的相机。

**【用途】** 快速拿到"主相机"，不用 `FindObjectOfType`。

**【代码示例】**
```csharp
Camera main = Camera.main;
if (main != null)
    Debug.Log(main.transform.position);
```

**【返回值】** 主相机 `Camera`；没有标记 `MainCamera` 的相机时返回 `null`。

【⚠️ 注意】`Camera.main` 依赖 Tag 为 `MainCamera`。如果场景里没有标记，返回 `null`，**要判空**。新相机默认 Tag 就是 `MainCamera`。

---

### 8.3.3 `Camera.ScreenPointToRay(Vector3 screenPos)`

**【是什么】** 把**屏幕坐标**（像素）转换成一条从相机出发的**世界空间射线**。

**【用途】** 鼠标点击拾取：把鼠标位置转成射线，再 `Physics.Raycast` 检测点到了什么物体。

**【参数说明】** `screenPos`：屏幕坐标（像素），通常用 `Input.mousePosition`。

**【返回值】** `Ray`（射线，含 `origin` 起点和 `direction` 方向）。

**【代码示例】**
```csharp
// 鼠标点击拾取：点击屏幕 → 射线 → 检测物体
void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.Log("点中了：" + hit.collider.name);
        }
    }
}
```

**【相似 API 区别】**
| API | 方向 | 用途 |
|-----|------|------|
| `ScreenPointToRay` | 屏幕 → 世界射线 | 拾取、点击检测 |
| `WorldToScreenPoint` | 世界 → 屏幕坐标 | 把 3D 位置显示到 UI 上 |

---

### 8.3.4 `Camera.WorldToScreenPoint(Vector3 worldPos)`

**【是什么】** 把**世界坐标**转换成**屏幕坐标**（像素）。

**【用途】** 把 3D 物体的位置映射到屏幕，用于在 UI 上显示血条、名字、标记。

**【参数说明】** `worldPos`：世界空间坐标。

**【返回值】** `Vector3`：屏幕坐标。`x/y` 是像素位置，`z` 是**到相机的距离**（深度）。

**【代码示例】**
```csharp
// 把 3D 物体位置转成屏幕坐标，用于 UI 跟随
Vector3 worldPos = target.transform.position;
Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

// 如果物体在相机前方（z>0），才显示 UI
if (screenPos.z > 0)
{
    uiText.transform.position = screenPos;
}
```

**【相似 API 区别】**
| API | 返回 | 范围 | 用途 |
|-----|------|------|------|
| `WorldToScreenPoint` | 像素坐标（0~屏幕宽高） | 屏幕像素 | UI 定位、血条 |
| `WorldToViewportPoint` | 归一化坐标（0~1） | 视口 | 判断是否在屏幕内、比例定位 |

---

### 8.3.5 `Camera.orthographic` 与 `perspective`

**【是什么】** 相机的投影方式：
- `orthographic = true`：**正交投影**，无透视，物体大小不随距离变化（2D 用）。
- `orthographic = false`：**透视投影**，近大远小（3D 用）。

**【代码示例】**
```csharp
Camera cam = GetComponent<Camera>();
cam.orthographic = true;    // 切到正交（2D）
cam.orthographicSize = 5f;  // 正交时：视野半高（世界单位）
```

**【相似 API 区别】**
| 投影 | 特点 | 适用 |
|------|------|------|
| 透视（perspective） | 近大远小，有 `fieldOfView` | 3D 游戏 |
| 正交（orthographic） | 无透视，用 `orthographicSize` | 2D 游戏、UI |

---

### 8.3.6 `Camera.cullingMask`（剔除层）

**【是什么】** 位掩码，决定相机**渲染哪些 Layer** 的物体。

**【用途】** 让相机只渲染特定层（比如 UI 相机只渲染 UI 层，主相机不渲染 UI）。

**【代码示例】**
```csharp
Camera cam = GetComponent<Camera>();

// 只渲染 Layer 8（假设是 UI 层）
cam.cullingMask = 1 << 8;

// 渲染除 Layer 8 外的所有层
cam.cullingMask = ~(1 << 8);

// 渲染所有层
cam.cullingMask = ~0;
```

**【参数说明】** `cullingMask`：位掩码（int），每一位对应一个 Layer。

---

### 8.3.7 `Camera.nearClipPlane` / `farClipPlane` / `fieldOfView`

**【是什么】**
- `nearClipPlane`：近裁剪面距离，小于此距离的物体不渲染。
- `farClipPlane`：远裁剪面距离，大于此距离的物体不渲染。
- `fieldOfView`：垂直视野角度（透视相机用，单位度）。

**【用途】** 控制可见范围、性能（远裁剪面越小渲染越少）。

**【代码示例】**
```csharp
Camera cam = GetComponent<Camera>();
cam.nearClipPlane = 0.3f;   // 太近的物体不渲染
cam.farClipPlane  = 500f;   // 太远的物体不渲染
cam.fieldOfView   = 60f;    // 视野角度
```

**【版本标记】** `Camera` 相关 API 均为**稳定老 API**。

---

## 8.4 Light 与光照

### 8.4.1 `class Light : Behaviour` — 灯光组件

**【是什么】** 场景里的光源组件，负责照亮物体。Unity 内置管线支持多种光源类型。

**【名称含义】** `Light` = 光/灯光。

**【代码示例】**
```csharp
Light light = GetComponent<Light>();
light.type = LightType.Directional;  // 平行光
light.color = Color.white;
light.intensity = 1f;
```

---

### 8.4.2 `Light.type`（光源类型）

**【是什么】** 枚举，决定光源的发光方式。

| 类型 | 含义 | 特点 |
|------|------|------|
| `LightType.Directional` | 平行光 | 无限远，方向一致，模拟太阳 |
| `LightType.Point` | 点光源 | 从一点向四周发光，有 `range` |
| `LightType.Spot` | 聚光灯 | 锥形光束，有 `range` + `spotAngle` |
| `LightType.Area` | 区域光（仅烘焙） | 面状光源，只用于**烘焙**光照贴图，不参与实时渲染 |

**【代码示例】**
```csharp
Light light = GetComponent<Light>();
light.type = LightType.Point;   // 点光源
light.range = 10f;              // 照亮半径
```

---

### 8.4.3 `Light.color` / `intensity` / `range` / `spotAngle`

**【是什么】** 光源的核心参数：
- `color`：光颜色。
- `intensity`：光强度（亮度）。
- `range`：光照范围（点光/聚光用）。
- `spotAngle`：聚光灯锥角（度）。

**【代码示例】**
```csharp
Light light = GetComponent<Light>();
light.color = new Color(1f, 0.8f, 0.6f);  // 暖色光
light.intensity = 2f;                     // 更亮
light.range = 5f;                         // 照亮 5 米
light.spotAngle = 45f;                    // 聚光锥角 45 度
```

**【版本标记】** `Light` 为稳定老 API。

---

## 8.5 Shader 基础

### 8.5.1 `class Shader : Object` — 着色器

**【是什么】** Shader 是**运行在 GPU 上的小程序**，决定每个顶点/像素最终显示成什么颜色。它是"画成什么样"的最终执行者。

**【名称含义】** `Shader` = 着色器。

**【用途】** 定义材质的外观算法。材质是"参数"，Shader 是"算法"。

**【Material 与 Shader 关系】**
```
Material（参数：颜色、贴图、数值）
   └── 引用一个 Shader（算法：怎么用这些参数画）
```

**【代码示例】**
```csharp
// 拿到一个 Shader
Shader s = Shader.Find("Universal Render Pipeline/Lit");
```

---

### 8.5.2 `Shader.Find(string name)` 静态方法

**【是什么】** 按名字查找一个已加载的 Shader。

**【参数说明】** `name`：Shader 的名字（如 `"Universal Render Pipeline/Lit"`）。

**【返回值】** 找到的 `Shader`；找不到返回 `null`。

**【代码示例】**
```csharp
Shader s = Shader.Find("Universal Render Pipeline/Lit");
if (s != null)
{
    Material m = new Material(s);   // 用找到的 Shader 建材质
    GetComponent<Renderer>().material = m;
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Shader.Find` | 运行时按名字找 Shader，找不到返回 null |
| `Resources.Load<Shader>` | 从 Resources 加载，需 Shader 在 Resources 目录 |

---

### 8.5.3 内置管线 vs URP 中的 Material 用法

**【是什么】** Unity 有**内置渲染管线（Built-in）**和**通用渲染管线（URP）**。两者 Shader 名字不同，但 `Material` 的 API 用法基本一致。

**【代码示例】**
```csharp
// 内置管线：用 Standard Shader
Material m1 = new Material(Shader.Find("Standard"));

// URP 管线：用 URP 的 Lit Shader
Material m2 = new Material(Shader.Find("Universal Render Pipeline/Lit"));
```

> **版本标记**：URP 是**较新的渲染方案**（相对内置管线）。新项目推荐用 URP，但 `Material` 的 `SetColor`/`SetTexture` 等 API 两者通用。

---

## 8.6 后期处理 Post Processing

### 8.6.1 后期处理（Post Processing）概念

**【是什么】** 在场景渲染完成后，对整张画面再做一次加工：泛光（Bloom）、色调映射（Tone Mapping）、景深（Depth of Field）、色彩校正（Color Grading）、模糊等。

**【用途】** 提升画面质感、营造氛围。

**【URP 中的实现】** URP 用 **Volume** 系统做后期处理：
- `Volume`：挂在场景里的一个组件，定义"后期处理生效的区域"。
- `VolumeProfile`：存放具体后期效果的参数（如 Bloom 强度）。
- 在 Volume 里添加 `Bloom`、`Color Adjustments` 等 Override 组件。

**【代码示例】**
```csharp
// URP 后期处理：给场景加一个全局 Volume
// 1. 场景里创建空物体，挂 Volume 组件
// 2. Volume 的 Profile 里添加 Bloom / Color Adjustments 等
// 3. 运行时可通过代码调整参数
Volume volume = GetComponent<Volume>();
if (volume.profile.TryGet<Bloom>(out var bloom))
{
    bloom.intensity.value = 1.5f;   // 调泛光强度
}
```

> **版本标记**：后期处理（Post Processing）是**较新方案**，URP 用 Volume 系统实现，内置管线用 `Post Processing Stack` 包。

---

## 8.7 渲染顺序与 RenderQueue

### 8.7.1 `Material.renderQueue`（渲染队列）

**【是什么】** 控制材质在渲染时的**绘制顺序**。数值越小越先画。

**【用途】** 处理透明、背景、UI 等绘制顺序问题。

**【常见队列值】**
| 值 | 含义 |
|----|------|
| `1000` | 背景（Background） |
| `2000` | 不透明物体（Geometry，默认） |
| `3000` | 透明物体（Transparent） |
| `4000` | 覆盖层（Overlay） |

**【代码示例】**
```csharp
Material m = GetComponent<Renderer>().material;
m.renderQueue = 3000;   // 强制按透明队列渲染
```

---

### 8.7.2 `SpriteRenderer.sortingOrder`（排序）

**【是什么】** 2D 精灵的绘制顺序。数值越大越**后**绘制（越靠前显示）。

**【用途】** 控制 2D 角色、UI 元素的遮挡关系。

**【代码示例】**
```csharp
SpriteRenderer sr = GetComponent<SpriteRenderer>();
sr.sortingOrder = 10;   // 数值大，画在上面
```

**【相似 API 区别】**
| API | 适用 | 作用 |
|-----|------|------|
| `renderQueue` | 3D 材质 | 控制渲染批次顺序 |
| `sortingOrder` | 2D SpriteRenderer | 控制精灵绘制先后 |

---

## 8.8 本章高频「相似 API」对比总表

| 想做的事 | 用哪个 | 别用哪个/注意 |
|---------|--------|--------------|
| 只改一个物体外观 | `renderer.material` | `sharedMaterial` 会改所有人 |
| 批量改同材质 | `renderer.sharedMaterial` | `material` 每对象克隆，破坏合批 |
| 大量物体独立变色 | `MaterialPropertyBlock` | 别用 `material`（克隆爆炸） |
| 屏幕点击拾取 | `ScreenPointToRay` + `Raycast` | `WorldToScreenPoint` 是反方向 |
| 世界坐标 → UI 位置 | `WorldToScreenPoint` | `WorldToViewportPoint` 是归一化 |
| 隐藏物体 | `renderer.enabled=false` | `SetActive(false)` 会停逻辑 |
| 2D 遮挡 | `sortingOrder` | `renderQueue` 是 3D 材质用 |

---

## 8.9 版本标记（本章涉及的版本化 API）

| API | 标记 |
|-----|------|
| `MaterialPropertyBlock` | ✅ 稳定老 API（Unity 5.0 起），批量渲染推荐 |
| URP（Universal Render Pipeline） | ✅ 较新渲染方案，新项目推荐 |
| 后期处理（Volume/Post Processing） | ✅ 较新方案（URP 用 Volume） |
| `Camera` / `SpriteRenderer` / `Light` / `Renderer` | 稳定老 API |
| `Shader.Find` / `new Material` | 稳定老 API |

> 具体版本细节以官方手册为准，这里只给推理方向。

---

## 8.10 本章小结

- `Renderer` 决定"画不画、用什么材质"，`enabled` 隐藏、`bounds` 包围盒、`isVisible` 视野剔除。
- `material` 会克隆（只改自己），`sharedMaterial` 共享（改所有人），大量物体用 `MaterialPropertyBlock` 优化。
- `Camera.main` 拿主相机，`ScreenPointToRay` 做拾取，`WorldToScreenPoint` 做 UI 跟随。
- `Light` 有 Directional/Point/Spot 三种，配 `color`/`intensity`/`range`/`spotAngle`。
- `Shader` 是 GPU 算法，`Material` 是参数，`new Material(Shader.Find(...))` 建材质。
- URP 用 Volume 做后期处理，`renderQueue` 管 3D 顺序，`sortingOrder` 管 2D 顺序。

---

# 第 9 章 UI 系统

> **本章管辖**：Unity 里"做界面"的全部方案——`RectTransform` 布局、`Canvas` 渲染、UGUI 控件、`EventSystem` 事件、以及官方新方案 UI Toolkit。
> **一句话**：UGUI 是"现在最普及的界面方案"（Canvas + RectTransform + Button 等控件），UI Toolkit 是"官方的新一代方案"（2019+ 出现、2021 逐渐成熟），IMGUI 只在编辑器里常见。
> **前置**：建议先看第 1 章（GameObject/Component 体系）、第 2 章（Transform 与坐标系）、第 8 章（渲染与 Camera），本章大量用到这三章概念。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 在屏幕上放一个可点击按钮 | `Button`（UGUI）+ `onClick.AddListener` |
| 让 UI 跟随 3D 物体 | `Camera.WorldToScreenPoint` + `RectTransformUtility.ScreenPointToLocalPointInRectangle` |
| 把屏幕像素坐标转成 UI 局部坐标 | `RectTransformUtility.ScreenPointToLocalPointInRectangle` |
| 把 UI 坐标转回屏幕坐标 | `RectTransformUtility.WorldToScreenPoint` |
| 让 UI 适配不同分辨率 | `CanvasScaler`（按参考分辨率缩放） |
| 做一个可拖动/缩放的布局区域 | `RectTransform` 的 anchor + pivot |
| 让 UI 铺满父物体/对齐到角 | 改 `anchorMin` / `anchorMax` / `anchoredPosition` |
| 在 UI 上响应"点了一下" | `IPointerClickHandler` 接口，或 `EventTrigger` 组件 |
| 监听 UI 的点击/拖拽/悬停事件 | `EventTrigger`（可视化配置）或各 `IPointer*Handler` 接口 |
| 场景里必须有的事件驱动中心 | `EventSystem`（UGUI 事件全靠它派发） |
| 用新版方案做 UI | `UIDocument` + `VisualElement`（UI Toolkit，2019+/2021 成熟） |
| 给 UI Toolkit 写样式 | `.uss` 样式表（USS，类似 CSS） |
| 编辑器里画调试界面 | `OnGUI()` + `GUILayout`（IMGUI，仅编辑器/运行时调试） |
| 用代码动态创建 UI 控件 | `new GameObject` + `AddComponent<Button>` + `AddComponent<Image>` |

---

## 9.1 三大 UI 方案总览

> 面试必问：**"Unity 有几种 UI 方案？区别是什么？"**

| 方案 | 全名 | 地位 | 技术形态 | 用在哪 | 版本 |
|------|------|------|---------|--------|------|
| **UGUI** | Unity UI（`UnityEngine.UI`） | 旧但**最普及** | 场景内 GameObject + `Canvas` + `RectTransform` + 控件组件 | 游戏内界面（血条、背包、商店） | 2014.2+ 稳定，至今主流 |
| **UI Toolkit** | UITK（`UnityEngine.UIElements`） | **较新**，官方新方案 | 类 Web 前端：`UIDocument` + `VisualElement` + USS 样式 | 运行时 UI、编辑器扩展 | 2019.1 出现，2021 逐渐成熟，2022/2023 主推 |
| **IMGUI** | Immediate Mode GUI（`OnGUI`） | 老、即时模式 | 每帧用代码声明式绘制（`OnGUI`） | 编辑器调试工具、临时 UI | 很老，编辑器专用 |

**UGUI 与 UI Toolkit 的关系（一句话）**：UGUI 是"被官方逐步替代、但存量最大"的方案；UI Toolkit 是官方**推荐新项目使用**的新方案（模仿 Web 的 DOM + CSS），但生态和第三方组件目前仍不如 UGUI 全。**两者互不兼容**（UGUI 用 RectTransform/Canvas，UITK 用 VisualElement/USS），一个项目里可混用但通常不混。

**【相似 API 区别】三种方案怎么选**

| 维度 | UGUI | UI Toolkit | IMGUI |
|------|------|-----------|-------|
| 学习资料/面试占比 | ★★★★★ 最多 | ★★★ 增长中 | ★（会读就行） |
| 适合新手先学 | ✅ 推荐先学 | 可作为进阶 | 不必深究 |
| 运行时游戏 UI | ✅ 最常用 | ✅ 可以（2021+） | ❌ 不推荐 |
| 编辑器扩展 UI | ❌ | ✅ 官方新标准 | ✅ 老标准（仍大量在用） |
| 布局模型 | anchor/pivot（锚点） | Flexbox（弹性盒，CSS 思维） | 无（即时绘制） |

---

## 9.2 RectTransform —— UI 的"位置组件"

### 9.2.1 `class RectTransform : Transform`

**【是什么】** UI 元素的 Transform 专用子类。普通 `Transform` 只管 position/rotation/scale（世界坐标）；`RectTransform` 在此基础上加了**锚点（anchor）、枢轴（pivot）、矩形（rect）**三套概念，让你能"相对父 UI 对齐"。

**【用途】** 每一个 UGUI 元素（Canvas、Button、Image…）的 `transform` 实际都是 `RectTransform`。摆 UI、做适配、算坐标全靠它。

**【与 Transform 的核心区别】**

| 概念 | `Transform`（3D 物体） | `RectTransform`（UI 元素） |
|------|----------------------|---------------------------|
| 定位依据 | 世界坐标 `position` | 相对锚点（父 UI）的 `anchoredPosition` |
| 尺寸 | 无（由 Mesh/模型决定） | 有 `rect`（宽高矩形） |
| 对齐 | 无 | anchor + pivot 决定对齐方式 |
| 屏幕坐标换算 | `Camera.WorldToScreenPoint` | `RectTransformUtility.ScreenPointToLocalPointInRectangle` |

**【名称含义】** `Rect`（矩形）+ `Transform`（变换）。"带矩形信息的变换组件"。

**【代码示例】** 拿到并读取：
```csharp
using UnityEngine;
using UnityEngine.UI;

public class RectTransformDemo : MonoBehaviour
{
    public RectTransform uiRect;   // 在 Inspector 拖一个 UI 元素

    void Start()
    {
        Rect r = uiRect.rect;                  // 本元素矩形（宽高，单位与 Canvas 一致）
        Debug.Log($"宽={r.width} 高={r.height}");

        Debug.Log(uiRect.anchoredPosition);    // 相对锚点的偏移
        Debug.Log(uiRect.pivot);               // 枢轴 (0,0)左下~(1,1)右上，默认(0.5,0.5)
    }
}
```

---

### 9.2.2 `anchorMin` / `anchorMax`（锚点，核心中的核心）

**【是什么】** 两个 `Vector2`，定义本元素**在父 RectTransform 上的两个锚点角**。父矩形归一化后：`(0,0)` = 左下角，`(1,1)` = 右上角。
- `anchorMin`：左下锚点。
- `anchorMax`：右上锚点。
- **两者相等** = 锚在"父矩形内一个点"（如 (0.5,0.5) 中心点）。
- **两者不等** = 锚在"父矩形内一个区域"，元素会随父元素**拉伸**。

**【用途】** 做自适应布局：让按钮固定左上角、让面板铺满全屏、让血条随父容器拉伸。

**【代码示例】**
```csharp
// 让 uiRect 铺满父元素（全拉伸）
uiRect.anchorMin = Vector2.zero;   // 左下角对齐父左下角
uiRect.anchorMax = Vector2.one;    // 右上角对齐父右上角
uiRect.offsetMin = Vector2.zero;   // 距父左下的留白
uiRect.offsetMax = Vector2.zero;   // 距父右上的留白

// 让 uiRect 固定到父元素的右上角（锚成一个点）
uiRect.anchorMin = new Vector2(1, 1);
uiRect.anchorMax = new Vector2(1, 1);
uiRect.pivot = new Vector2(1, 1);   // 枢轴也对到右上角，位置才"完全钉死"
uiRect.anchoredPosition = new Vector2(-20, -20); // 距右上角向内 20 单位
```

> 注意：RectTransform 的正规属性名是 `anchorMin` 与 `anchorMax`（注意是 **anchor 单数**，没有复数 `anchorsMin`/`anchorsMax`，那是常见的拼写错误）。

---

### 9.2.3 `pivot`（枢轴）

**【是什么】** `Vector2`，指定"本元素自身的哪个点作为变换原点"。`(0,0)` 左下，`(1,1)` 右上，默认 `(0.5,0.5)` 正中心。

**【用途】** 决定 `anchoredPosition`、缩放、旋转绕着元素上哪个点发生。血条常设 `pivot=(0,0.5)` 让血量从左边往右缩。

**【代码示例】**
```csharp
Image hpBar;                      // 血条 Image
hpBar.rectTransform.pivot = new Vector2(0, 0.5f); // 枢轴在左边中点
// 之后改 fillAmount（Image）或拉宽 sizeDelta，血条从左往右增减
```

---

### 9.2.4 `anchoredPosition`（锚定位置）

**【是什么】** `Vector2`，本元素**相对锚点**的偏移量。

**【用途】** 运行时移动 UI（上下弹窗、拖动图标）。注意它**不是**世界坐标。

**【代码示例】**
```csharp
uiRect.anchoredPosition = new Vector2(100, 0);   // 在锚点基础上向右移 100
uiRect.anchoredPosition += Vector2.up * 10;      // 每帧向上浮 10（单位：UI 像素/单位）
```

**【相似 API 区别】`anchoredPosition` vs `position` vs `localPosition`**
| API | 含义 | 何时用 |
|-----|------|--------|
| `anchoredPosition` | 相对锚点的偏移 | 摆 UI 位置（推荐） |
| `localPosition` | 相对父物体原点的偏移 | 3D 思维，UI 里易混乱 |
| `position` | 世界坐标 | UI 世界空间（WorldSpace）下用 |

---

### 9.2.5 `sizeDelta` / `SetSizeWithCurrentAnchors`

**【是什么】**
- `sizeDelta`：`Vector2`。当锚是一个点（min==max）时 = 本元素宽高；当锚是区域时 = 本元素尺寸**相对锚区域的差值**（可为负）。宽 = `sizeDelta.x`，高 = `sizeDelta.y`。
- `SetSizeWithCurrentAnchors(axis, size)`：**保持当前锚方式**，直接设置宽（axis=0）或高（axis=1）。

**【用途】** 动态改 UI 尺寸（血条变化、物品栏展开）。

**【代码示例】**
```csharp
// 方案一：直接改 sizeDelta（锚为单点时最直观）
uiRect.sizeDelta = new Vector2(300, 100);   // 宽 300 高 100

// 方案二：官方提供的 SetSizeWithCurrentAnchors
uiRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300); // 设宽
uiRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);   // 设高
```

> 注意：RectTransform **没有** `widthDelta`/`heightDelta` 属性（历史上也没有，别被旧教程误导）。宽高分量直接读 `sizeDelta.x`/`.y`，或通过 `rect.width`/`rect.height` 读当前宽高（只读）。

---

### 9.2.6 `rect`（矩形）

**【是什么】** 只读的 `Rect` 属性：本元素在当前**尺寸/枢轴**下的局部矩形（含 x/y 偏移与 width/height）。

**【用途】** 读取宽高、做 AABB 碰撞、判断点是否落在元素内。

**【代码示例】**
```csharp
Rect r = uiRect.rect;
bool inside = r.Contains(localPoint);  // localPoint 是"UI 局部坐标"下的点
```

---

### 9.2.7 `localScale`（局部缩放）

**【是什么】** `Vector3`，本元素相对父元素的缩放（继承自 Transform）。UI 里缩放**围绕 pivot 点**进行。

**【用途】** 按钮按下变小、弹窗弹出动画、抖动特效。

**【代码示例】**
```csharp
// 按钮按下反馈：缩小再弹回
public class PressScale : MonoBehaviour
{
    void OnEnable() { GetComponent<Button>().onClick.AddListener(() =>
        transform.localScale = Vector3.one * 0.9f); }
}
// 注意：UI 用 localScale 缩放不会影响布局（布局只看 rect），可放心用于特效
```

---

### 9.2.8 `RectTransformUtility` 屏幕坐标转换

#### 9.2.8.1 `RectTransformUtility.ScreenPointToLocalPointInRectangle`

**【是什么】** 静态方法。把**屏幕像素坐标**转换成**某 UI 元素的局部坐标**（以该元素 pivot 为原点的坐标系）。

```csharp
public static bool ScreenPointToLocalPointInRectangle(
    RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint)
```

**【参数说明】**
- `rect`：目标 UI 元素（转换基准）。
- `screenPoint`：屏幕坐标（像素，如鼠标 `Input.mousePosition`）。
- `cam`：摄像机。**Overlay 模式传 `null`**；ScreenSpace-Camera / WorldSpace 模式传对应 `Camera`。
- `localPoint`：`out` 返回转换结果。

**【返回值】** `bool`：点是否落在该 UI 元素的 `rect` 范围内（注意：`true` 只代表在矩形内，不是"成功转换"，一般仍可作命中判断用）。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickToLocal : MonoBehaviour
{
    public RectTransform targetUI;   // 要判断的 UI 元素

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetUI, Input.mousePosition, null, out local))
            {
                Debug.Log($"鼠标落在 targetUI 内，局部坐标 = {local}");
            }
        }
    }
}
```

**【相似 API 区别】坐标转换三兄弟（面试常考）**

| API | 输入 → 输出 | 用途 |
|-----|------------|------|
| `Camera.ScreenToWorldPoint` | 屏幕像素 → 世界坐标 | 屏幕点击处生成 3D 物体 |
| `Camera.WorldToScreenPoint` | 世界坐标 → 屏幕像素 | 让 UI 跟随 3D 物体（见 9.7） |
| `RectTransformUtility.ScreenPointToLocalPointInRectangle` | 屏幕像素 → **某 UI 局部坐标** | 判断点击落在哪个 UI 元素、取局部坐标 |
| `RectTransformUtility.ScreenPointToWorldPointInRectangle` | 屏幕像素 → 世界坐标（以 UI 平面为基准） | WorldSpace Canvas 上做射线命中 |

#### 9.2.8.2 `RectTransformUtility.WorldToScreenPoint`

**【是什么】** 静态方法。把世界坐标转成屏幕像素坐标（含 UI 使用）：
```csharp
public static Vector2 WorldToScreenPoint(Camera cam, Vector3 worldPoint)
```

**【用途】** 3D 物体位置 → 屏幕坐标，再配合 `ScreenPointToLocalPointInRectangle` 转成 UI 坐标。详见 9.7 的"UI 跟随 3D 物体"。

---

## 9.3 Canvas（画布）

### 9.3.1 `class Canvas : Behaviour`

**【是什么】** UI 的"画布"根组件。**所有 UGUI 元素必须挂在某个 Canvas 之下**（直接或间接子物体），否则不显示。Canvas 决定整个 UI 树用哪种方式渲染到屏幕。

**【用途】** UI 树的根；配置渲染模式、缩放因子。

**【名称含义】** `Canvas` = 画布。想象成一块"布"，UI 都画在这块布上。

**【参数/属性】**
- `renderMode`：渲染模式（见下方三选一对比）。
- `scaleFactor`：画布缩放倍数（手动模式）。
- `sortingOrder`：多个 Canvas 的显示层级（数字大的在上）。
- `worldCamera`：`ScreenSpace-Camera` 模式时指定渲染用摄像机。

**【相似 API 区别】`canvas.renderMode` 三种模式（必考对比表）**

| 模式 | 枚举值 | 渲染特点 | 用在哪 |
|------|--------|---------|--------|
| 屏幕空间-覆盖 | `ScreenSpaceOverlay` | UI 永远盖在画面上，无需摄像机，始终在 3D 之上 | 菜单、HUD、血量条（最常用） |
| 屏幕空间-摄像机 | `ScreenSpaceCamera` | UI 由指定摄像机渲染，可做 3D 遮挡/景深效果 | 需要"UI 被 3D 物体挡住"或跟随画面效果 |
| 世界空间 | `WorldSpace` | UI 作为世界里的平面物体，可有 3D 位置/旋转 | 3D 世界里的告示牌、血条浮在角色头顶、VR 界面 |

**代码切换示例：**
```csharp
Canvas cv = GetComponent<Canvas>();
cv.renderMode = RenderMode.ScreenSpaceCamera;   // 切成"摄像机模式"
cv.worldCamera = Camera.main;                    // 必须指定摄像机
```

---

### 9.3.2 `canvas.scaleFactor`

**【是什么】** `float`，画布的整体缩放倍数。值 = 1 时 UI 像素 = 屏幕像素（Overlay 模式）。配合不同分辨率时让 UI 等比放大/缩小。

**【用途】** 手动控制 UI 大小（一般交给 `CanvasScaler` 自动算，见下）。

**【代码示例】**
```csharp
GetComponent<Canvas>().scaleFactor = 2f;   // UI 放大 2 倍
```

---

### 9.3.3 `CanvasScaler`（画布缩放器，自适应核心）

**【是什么】** 挂在 Canvas 上的组件，根据**屏幕实际分辨率**自动算出 `scaleFactor`，让 UI 在不同设备上**保持设计尺寸**，不会在 4K 上小得看不见。

**【主要属性】**
- `uiScaleMode`：缩放策略。
  - `ScaleWithScreenSize`：按参考分辨率缩放（**最常用**）。
  - `ConstantPixelSize`：不缩放（就是 scaleFactor）。
  - `ConstantPhysicalSize`：按物理尺寸（英寸），移动端适配屏幕距离用。
- `referenceResolution`：设计稿分辨率（如 1920×1080）。
- `matchWidthOrHeight`：0~1 之间，0 偏宽匹配、1 偏高匹配（0.5 取中）。
- `referencePixelsPerUnit`：精灵每单位像素数（默认 100，影响 Image 显示尺寸）。

**【代码示例】** 代码里配置（正常在 Inspector 里设）：
```csharp
using UnityEngine;
using UnityEngine.UI;

public class SetupCanvasScaler : MonoBehaviour
{
    void Start()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // 按 1080p 设计稿
        scaler.matchWidthOrHeight = 0.5f;                     // 宽高各占一半权重
    }
}
```

> 记忆：`CanvasScaler` = "帮 `Canvas.scaleFactor` 自动算值的管家"。面试问"UI 怎么适配不同分辨率"，答 CanvasScaler + ScaleWithScreenSize + referenceResolution 即可。

---

## 9.4 UGUI 常用控件

> 所有 UGUI 控件都是"组件"：先有 GameObject + `RectTransform`（通常 `Image`/`Text` 提供显示），再挂控件组件（`Button`/`Slider`/`InputField`…）。

### 9.4.1 `Button`（按钮）

**【是什么】** 可点击的 UI 控件组件。需要 `Image`（或其它 Graphic）显示外观，点击时触发 `onClick` 事件。

**【参数/成员】**
- `onClick`：`Button.ButtonClickedEvent`，**UnityEvent**，用 `AddListener` 订阅点击回调。
- `interactable`：是否可交互（false = 灰掉不可点）。
- `transition`：过渡效果（ColorTint / SpriteSwap / Animation）。

**【代码示例】** 三种订阅 onClick 的方式：
```csharp
using UnityEngine;
using UnityEngine.UI;

public class ButtonDemo : MonoBehaviour
{
    public Button startBtn;          // Inspector 拖引用

    void Start()
    {
        // 方式一：AddListener（推荐，代码里最常用）
        startBtn.onClick.AddListener(OnStartClick);

        // 方式二：Lambda
        startBtn.onClick.AddListener(() => Debug.Log("点击了开始"));

        // 方式三：只执行一次
        startBtn.onClick.AddListener(OnOnce);
    }

    void OnStartClick()
    {
        Debug.Log("游戏开始！");
    }

    void OnOnce()
    {
        startBtn.interactable = false;   // 点完置灰，防止重复
        startBtn.onClick.RemoveListener(OnOnce);
    }
}
```

**动态创建 Button（纯代码生成 UI，面试手写题常考）：**
```csharp
using UnityEngine;
using UnityEngine.UI;

public class CreateButtonByCode : MonoBehaviour
{
    public Canvas canvas;   // 拖入场景中的 Canvas

    void Start()
    {
        // 1. 造 GameObject，挂 Image（提供底色/点击区域）和 Button
        GameObject go = new GameObject("MyButton", typeof(RectTransform),
                                       typeof(Image), typeof(Button));
        go.transform.SetParent(canvas.transform, false);   // false=不继承世界变换

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;   // 放到画布中心
        rt.sizeDelta = new Vector2(200, 80);  // 200×80

        // 2. 给 Button 加文字
        GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(go.transform, false);
        Text label = txtGo.GetComponent<Text>();
        label.text = "点我";
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtGo.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);

        // 3. 绑定点击
        go.GetComponent<Button>().onClick.AddListener(() => Debug.Log("动态按钮被点了"));
    }
}
```

**【相似 API 区别】UGUI `Button` vs `EventTrigger`（对比表）**

| 维度 | `Button` | `EventTrigger` |
|------|----------|----------------|
| 触发事件 | 只有 `onClick`（点击完成） | 所有指针/拖拽/滚轮事件（开始按下、移入、移出、拖动…） |
| 配置方式 | 组件上直接拖 UnityEvent | 在列表里选事件类型 + 配回调 |
| 适用范围 | 普通按钮 | 需要"按下瞬间""移入高亮"等细粒度反馈 |
| 本质 | 专用封装 | 通用事件中转（内部用 `IPointerClickHandler` 等接口） |

**事件选择建议**：只响应"完整点击"→ `Button`；要"按下开始拖""鼠标移入显示提示"→ `EventTrigger`。

---

### 9.4.2 `Text`（文本）

**【是什么】** 显示文字的 UI 控件（`UnityEngine.UI.Text`）。老版 UGUI 文本组件。**TextMeshPro（TMP）自 Unity 2018 起内置**成为官方推荐，2022.2+ 新建 UI 更是**默认改用 TMP**；但老 `Text` 组件仍可用。

**【参数/成员】**
- `text`：显示内容。
- `fontSize`：字号。
- `color`：颜色。
- `alignment`：对齐（`TextAnchor` 枚举）。
- `font`：字体（默认 `LegacyRuntime.ttf`）。
- `supportRichText`：是否支持富文本（`<b>` `<color>`）。

**【代码示例】**
```csharp
Text t = GetComponent<Text>();
t.text = $"金币：{coins}";
t.fontSize = 32;
t.color = Color.yellow;
t.alignment = TextAnchor.MiddleCenter;
t.supportRichText = true;
t.text = "<b>加粗</b><color=#FF0000>红色</color>";
```

**【版本标记】** ⚠️ 老 `Text` 组件已不推荐新项目用；官方推荐 `TextMeshPro`（TMP，2018+ 内置），字号、排版、中文字体支持远强于老 Text。老项目/老教程里仍大量见到 `UnityEngine.UI.Text`。

---

### 9.4.3 `Image`（图片）

**【是什么】** 显示 Sprite 的 UI 控件（`UnityEngine.UI.Image`）。既是显示组件，也是**点击命中区域**（Button 的点击范围就是 Image 的矩形）。

**【参数/成员】**
- `sprite`：显示的精灵。
- `type`：显示类型（`Simple` 普通 / `Sliced` 九宫格 / `Tiled` 平铺 / `Filled` 填充）。
- `color`：颜色叠加（半透明、染色）。
- `fillAmount`（0~1）：`Filled` 类型下显示比例（血条/进度条最爱用）。
- `raycastTarget`：是否作为射线命中目标（**不影响显示但影响点击**；纯装饰图可关掉省性能）。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    public Image fill;   // 前景条 Image，type 设为 Filled，fillMethod=Horizontal

    public void SetHP(float ratio)
    {
        fill.fillAmount = Mathf.Clamp01(ratio);   // 0=空 1=满
    }
}
```

**【相似 API 区别】`Image` vs `RawImage`**
| API | 显示 | 九宫格 | 常用场景 |
|-----|------|--------|---------|
| `Image` | `Sprite`（可九宫格/填充） | ✅ | 按钮、血条、图标 |
| `RawImage` | `Texture2D`（无裁剪） | ❌ | 显示贴图/视频帧/动态纹理 |

---

### 9.4.4 `Slider`（滑动条）

**【是什么】** 拖动/点击可调数值的 UI 控件。由背景轨道、填充条、Handle（把手）组成。

**【参数/成员】**
- `minValue` / `maxValue`：取值范围。
- `value`：当前值（改它同时触发事件）。
- `wholeNumbers`：是否整数值。
- `onValueChanged`：值变化时触发的 `SliderEvent`（`UnityEvent<float>`）。
- `fillRect` / `handleRect`：拖入对应 RectTransform。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    public Slider slider;   // min=0 max=100 wholeNumbers=true

    void Start()
    {
        slider.onValueChanged.AddListener(OnVolumeChanged);
        slider.value = 50;   // 初始值
    }

    void OnVolumeChanged(float v)
    {
        Debug.Log($"音量 = {v}");
        // AudioListener.volume = v / 100f;
    }
}
```

---

### 9.4.5 `InputField`（输入框）

**【是什么】** 可输入文字的 UI 控件（老版 UGUI `InputField`；推荐新版用 `TMP_InputField`）。

**【参数/成员】**
- `text`：当前内容。
- `onValueChanged`：内容变化事件（`UnityEvent<string>`）。
- `onEndEdit`：输入结束（回车/失焦）事件。
- `contentType`：限制类型（数字/密码/邮箱等）。
- `characterLimit`：最大长度（-1 不限）。
- `caretColor` / `textComponent`：外观引用。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.UI;

public class NameInput : MonoBehaviour
{
    public InputField input;

    void Start()
    {
        input.onValueChanged.AddListener(v => Debug.Log($"正在输入：{v}"));
        input.onEndEdit.AddListener(v => Debug.Log($"确认输入：{v}"));
        input.contentType = InputField.ContentType.Standard;  // 普通文本
        input.characterLimit = 20;
    }
}
```

**【版本标记】** 老 `InputField` 稳定可用；新项目推荐 `TMP_InputField`（TextMeshPro 版本，排版与中文字体更好）。

---

### 9.4.6 `ScrollRect`（滚动区域）

**【是什么】** 带滚动/拖拽的容器控件。内容放进 `content` 子物体，超出 `viewport` 的部分可拖动查看。

**【参数/成员】**
- `content`：可滚动内容（RectTransform，尺寸通常大于视口）。
- `viewport`：可视窗口（RectTransform）。
- `horizontal` / `vertical`：是否允许横向/纵向滚动。
- `movementType`：`Unrestricted` / `Elastic`（弹性回弹，默认）/ `Clamped`（钳制）。
- `onValueChanged`：滚动位置变化事件（`UnityEvent<Vector2>`，位置为归一化 0~1）。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.UI;

public class ScrollLog : MonoBehaviour
{
    public ScrollRect scroll;

    void Start()
    {
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.vertical = true;
        scroll.horizontal = false;

        scroll.onValueChanged.AddListener(OnScrolled);
    }

    void OnScrolled(Vector2 pos)
    {
        Debug.Log($"滚动位置（归一化）：x={pos.x} y={pos.y}");
        // pos.y==0 表示滚到底，可用来做"加载更多"
        if (pos.y <= 0.01f) Debug.Log("到底了，加载下一页");
    }

    public void GoBottom()
    {
        scroll.verticalNormalizedPosition = 0f;  // 0=底部，1=顶部
    }
}
```

---

### 9.4.7 `EventTrigger`（事件触发器）

**【是什么】** 通用 UI 事件组件。不用写接口、不用写代码，在 Inspector 里**可视化配置**各种指针事件（点击、按下、移入、拖拽、滚轮）。

**【主要事件类型（枚举）】**
- `PointerClick`（完整点击）、`PointerDown`（按下）、`PointerUp`（抬起）
- `PointerEnter` / `PointerExit`（移入/移出）
- `BeginDrag` / `Drag` / `EndDrag`（拖拽三阶段）
- `Scroll`（滚轮）、`UpdateSelected` 等

**【代码示例】** 代码动态添加 EventTrigger：
```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class AddEventTriggerByCode : MonoBehaviour
{
    void Start()
    {
        EventTrigger trigger = gameObject.AddComponent<EventTrigger>();

        // 建一个条目："移入"事件
        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener(e => Debug.Log("鼠标移入！"));
        trigger.triggers.Add(enter);

        // 再建一条："按下"事件
        EventTrigger.Entry down = new EventTrigger.Entry();
        down.eventID = EventTriggerType.PointerDown;
        down.callback.AddListener(e =>
        {
            if (e is PointerEventData ped) Debug.Log($"按下，位置 {ped.position}");
        });
        trigger.triggers.Add(down);
    }
}
```

**【相似 API 区别】** 见 9.4.1 的 `Button` vs `EventTrigger` 对比表。一句话：Button 只给"点击"，EventTrigger 给"一切指针/拖拽事件"。

---

## 9.5 EventSystem（事件系统）

> UGUI 的点击、拖拽**不是**靠 `OnMouseDown`（那是 3D 的），而是靠场景里的 `EventSystem` + 各输入模块 + `GraphicRaycaster` 协作派发。**没有 EventSystem，UI 全都点不了。**

### 9.5.1 `class EventSystem : UIBehaviour`

**【是什么】** 场景级单例（每个场景放一个）。它是 UI 事件的总调度中心：每帧收集输入模块的数据，用 Raycaster 判断命中了哪个 UI，再把事件派发给目标（Button/接口实现者）。

**【成员】**
- `EventSystem.current`：静态属性，返回当前场景的 EventSystem 实例。
- `IsPointerOverGameObject()`：鼠标/触摸是否在 UI 上（常用！）。
- `currentInputModule`：当前输入模块。
- `SetSelectedGameObject`：设置被选中的对象（键盘导航）。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHelper : MonoBehaviour
{
    public bool IsMouseOnUI()
    {
        // 判断鼠标是否悬停/点在 UI 上（屏蔽 UI 后面的 3D 点击时常用）
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }
}
```

**【相似 API 区别】`IsPointerOverGameObject()` 注意点**
| 输入方式 | 调用方式 |
|---------|---------|
| 鼠标（Input Manager 旧版） | `IsPointerOverGameObject()`（无参） |
| 触摸 / 多指（Input System） | `IsPointerOverGameObject(int pointerId)`，传指针 id（-1 是鼠标） |

---

### 9.5.2 `StandaloneInputModule`（独立输入模块）

**【是什么】** EventSystem 默认带的输入模块（`UnityEngine.EventSystems.StandaloneInputModule`），负责把"鼠标/键盘/触摸"的原始输入转成 UI 事件（按下、抬起、拖拽）。

**【主要属性】**
- `horizontalAxis` / `verticalAxis`：键盘导航用的轴名（默认 "Horizontal"/"Vertical"）。
- `submitButton` / `cancelButton`：确认/取消按钮名（默认 "Submit"/"Cancel"）。
- `input`：可替换的输入源（方便测试/模拟输入）。

**【代码示例】** 运行时动态搭建（一般不用，Editor 里"新建 EventSystem"会自动建好）：
```csharp
GameObject es = new GameObject("EventSystem", typeof(EventSystem),
                               typeof(StandaloneInputModule));
// 一条命令搞定：EventSystem + 输入模块
```

> 面试考点：新版输入方案里对应组件是 `InputSystemUIInputModule`（Input System 包提供），功能类似，只是数据来自新的输入系统。旧 `StandaloneInputModule` 只管旧 `Input`。

---

### 9.5.3 `GraphicRaycaster`（图形射线投射器）

**【是什么】** 挂在 **Canvas** 上的组件，让这个 Canvas 下的 UGUI 元素**能被事件系统命中**。没有它，EventSystem 射线"打不中"任何 UI。

**【主要属性】**
- `ignoreReversedGraphics`：是否忽略背向摄像机的 UI（默认 true）。
- `blockingObjects` / `blockingMask`：可被 3D 物体挡住的设置（配合 ScreenSpaceCamera）。

**【代码示例】**
```csharp
Canvas cv = GetComponent<Canvas>();
GraphicRaycaster ray = cv.gameObject.AddComponent<GraphicRaycaster>(); // 手动加
```

> 面试常考三件套：**UI 可点击 = Canvas（渲染） + GraphicRaycaster（可命中） + EventSystem（调度事件）**。三者缺一不可。

---

### 9.5.4 UGUI 事件接口（`IPointerClickHandler` 等）

**【是什么】** 一系列 C# 接口，你的脚本实现它们，EventSystem 就会在对应事件发生时回调你。挂在**被点击的那个 UI 元素**上（或它的子元素上，事件会向上冒泡到父级）。

**【常用接口一览】**
| 接口 | 回调方法 | 触发时机 |
|------|---------|---------|
| `IPointerClickHandler` | `OnPointerClick(PointerEventData)` | 点击完成 |
| `IPointerDownHandler` | `OnPointerDown(PointerEventData)` | 按下瞬间 |
| `IPointerUpHandler` | `OnPointerUp(PointerEventData)` | 抬起瞬间 |
| `IPointerEnterHandler` | `OnPointerEnter(PointerEventData)` | 指针移入 |
| `IPointerExitHandler` | `OnPointerExit(PointerEventData)` | 指针移出 |
| `IBeginDragHandler` / `IDragHandler` / `IEndDragHandler` | `OnBeginDrag` / `OnDrag` / `OnEndDrag` | 拖拽三阶段 |
| `IScrollHandler` | `OnScroll` | 滚轮 |
| `IDropHandler` | `OnDrop` | 拖拽放下（配合拖拽） |

**【代码示例】** 实现拖拽一个 UI 元素：
```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 _offset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 记录按下点与元素位置的偏移，避免元素"跳"到鼠标下
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(), eventData.position, null, out _offset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 让元素跟着鼠标（Overlay 模式下 cam 传 null）
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform, eventData.position, null, out local);
        GetComponent<RectTransform>().anchoredPosition = local - _offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("拖拽结束");
    }
}
```

**【相似 API 区别】三种响应 UI 事件的方式（面试高频）**
| 方式 | 写法 | 场景 |
|------|------|------|
| `Button.onClick` | Inspector 拖/代码 AddListener | 只关心"点了按钮" |
| `EventTrigger` | 可视化配置列表 | 要细粒度事件又不爱写接口 |
| `IPointer*Handler` 接口 | 实现接口 + 回调方法 | 拖拽、多事件、纯代码 UI |

---

### 9.5.5 完整链路（面试串讲）

```
鼠标点击
  → StandaloneInputModule 采集输入，生成 PointerEventData
  → EventSystem 派发射线查询
  → GraphicRaycaster（挂在 Canvas 上）返回命中的 UI 元素
  → EventSystem 沿命中元素向上找处理者（Button / IPointerClickHandler 实现者）
  → 触发回调（onClick / OnPointerClick）
```

**面试一句话**：EventSystem 是"调度中心"，StandaloneInputModule 是"输入翻译器"，GraphicRaycaster 是"命中探测器"，三者协作让 UI 事件跑起来。

---

## 9.6 UI Toolkit（UITK）简介

### 9.6.1 方案总览

**【是什么】** 官方新一代 UI 方案（`UnityEngine.UIElements`），把 **Web 前端那套"结构 + 样式 + 事件"**搬进 Unity：
- 结构：`UIDocument`（挂在 GameObject 上）加载 `.uxml`（界面布局，类似 HTML）。
- 样式：`.uss` 文件（类似 CSS）。
- 逻辑：C# 访问 `VisualElement` 并订阅事件。

**【与 UGUI 的关键差异】**
| 维度 | UGUI | UI Toolkit |
|------|------|-----------|
| 结构 | GameObject + 组件 | `VisualElement` 树（不占场景物体） |
| 布局 | anchor / pivot | Flexbox（flex 弹性布局，CSS 思维） |
| 样式 | 逐个组件设属性 | USS 集中写样式（类似 CSS） |
| 编辑器扩展 | ❌ 不行 | ✅ 官方新标准（`EditorWindow` + UIElements） |
| 成熟度 | 稳定 | 2019 出现、2021 逐渐成熟、新版本主推 |

**【版本标记】** ⚠️ **较新方案**：Unity 2019.1 引入（当时叫 UIElements，主要给编辑器）；**Unity 2021 左右开始支持运行时游戏 UI 并逐渐成熟**；2023+ 官方在编辑器扩展上全面推行。**面试/简历上写"会用 UI Toolkit"是加分项**，但 UGUI 仍是主流项目主力。

**【代码示例】** 运行时用代码建 UITK 界面：
```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class UITKExample : MonoBehaviour
{
    public UIDocument doc;   // 场景里放一个 UIDocument（挂 UXML 可省略）

    void Start()
    {
        VisualElement root = doc.rootVisualElement;   // 拿到 UI 根节点

        var btn = new Button();
        btn.text = "UITK 按钮";
        btn.style.width = 120;                         // 内联样式（CSS 风格）
        btn.style.height = 40;
        btn.clicked += () => Debug.Log("UITK 按钮被点击");  // 事件订阅

        root.Add(btn);                                // 挂到界面树上
    }
}
```

### 9.6.2 `UIDocument`（UI 文档组件）

**【是什么】** 挂在 GameObject 上，把一张 `UXML` 文档显示为运行时 UI，并暴露 `rootVisualElement` 供 C# 操作。

**【参数/成员】**
- `visualTreeAsset`：`VisualTreeAsset`（.uxml 资源），要显示的界面布局。
- `rootVisualElement`：只读，该文档的根 `VisualElement`。

**【代码示例】**
```csharp
// 场景中：新 GameObject → Add Component → UI Document
// 然后赋值：UIDocument.visualTreeAsset = 你的 .uxml 资源
```

### 9.6.3 `VisualElement`（视觉元素）

**【是什么】** UITK 里一切 UI 的基类（相当于 UGUI 的 GameObject + RectTransform 合体）。按钮、标签、容器都是它或它的子类。

**【常用子类】**
- `Button`、`Label`（文字）、`TextField`（输入）、`Slider`、`ScrollView`
- `VisualElement` 本身可当容器（布局用）

**【代码示例】**
```csharp
var label = new Label("血量：100");
label.style.color = Color.red;                 // 颜色
label.style.marginTop = 10;                    // 边距（flexbox 属性）

root.Add(label);
```

### 9.6.4 USS 样式表

**【是什么】** `.uss` 文件（UI Toolkit 的样式表，语法类似 CSS）。用"类名/类型选择器"批量定义样式，UI 与逻辑分离。

**【代码示例】** `MyStyle.uss`：
```css
/* 类选择器：任何带 .btn-main 的元素 */
.btn-main {
    background-color: #3498db;
    color: white;
    border-radius: 6px;
    padding: 8px 16px;
}

/* 类型选择器：所有 Button */
Button:hover {           /* 悬停伪类 */
    background-color: #2ecc71;
}
```

对应 C#：
```csharp
doc.rootVisualElement.AddToClassList("btn-main");   // 给元素加类
// 或：在 UIDocument 的 PanelSettings 里关联 .uss 文件，全文档生效
```

**【相似 API 区别】`Button`（UGUI）vs `Button`（UITK）**
| 维度 | UGUI `Button` | UITK `Button` |
|------|---------------|---------------|
| 所在命名空间 | `UnityEngine.UI` | `UnityEngine.UIElements` |
| 事件 | `onClick.AddListener(...)` | `clicked += ...` |
| 定位方式 | RectTransform（anchor/pivot） | Flexbox 布局 + style 属性 |
| 依赖场景 | 需要 Canvas + EventSystem | 需要 UIDocument + PanelSettings |

---

## 9.7 屏幕坐标与 UI 坐标转换（实战）

> 核心链路：**世界坐标 → 屏幕像素坐标（`Camera.WorldToScreenPoint`）→ UI 局部坐标（`RectTransformUtility.ScreenPointToLocalPointInRectangle`）**。反过来做屏幕点击拾取：屏幕像素 → 射线（`Camera.ScreenPointToRay`）→ 世界坐标。

### 9.7.1 屏幕 → 世界（`Camera.ScreenToWorldPoint` / `ScreenPointToRay`）

**【是什么】**
- `ScreenToWorldPoint(Vector3 screenPosition)`：把屏幕坐标（x,y,z 中 z=到相机的深度）转成世界坐标。
- `ScreenPointToRay(Vector3 screenPos)`：从相机发出过该屏幕点的一条射线（Raycast 拾取 3D 物体用）。注意参数类型是 `Vector3`（`Input.mousePosition` 恰为 Vector3）。

**【代码示例】**
```csharp
using UnityEngine;

public class TapToSpawn : MonoBehaviour
{
    public Camera cam;
    public GameObject prefab;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 方式一：定距生成（z 填目标深度）
            Vector3 world = cam.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
            Instantiate(prefab, world, Quaternion.identity);

            // 方式二：射线命中（更常用）
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                Instantiate(prefab, hit.point, Quaternion.identity);
        }
    }
}
```

### 9.7.2 世界 → 屏幕（`Camera.WorldToScreenPoint`）

**【是什么】** 把世界坐标转成屏幕像素坐标（原点在左下，y 向上）。

**【相似 API 区别】`ScreenPointToLocalPointInRectangle` vs `WorldToScreenPoint`**

| API | 方向 | 返回 | 用途 |
|-----|------|------|------|
| `Camera.WorldToScreenPoint` | 世界 → 屏幕像素 | `Vector3`（x,y 像素，z=深度） | 拿到 3D 物体在屏幕上的像素位置 |
| `RectTransformUtility.ScreenPointToLocalPointInRectangle` | 屏幕像素 → **某 UI 局部坐标** | `Vector2`（out）+ bool | 把上面得到的像素位再翻译成 UI 元素坐标系 |
| `RectTransformUtility.WorldToScreenPoint` | 世界 → 屏幕像素（UI 版本，需传相机） | `Vector2` | 功能与 Camera 版等价，只是为 UI 封装 |

> 记忆：**像素 → UI 局部** 必须过 `ScreenPointToLocalPointInRectangle`；**世界 → 像素** 用 `WorldToScreenPoint`。UI 跟随 3D 物体就是把两者串起来。

### 9.7.3 实战：UI 跟随 3D 物体（血条跟随角色头顶）

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.UI;

public class HPBarFollow3D : MonoBehaviour
{
    public Transform target;        // 3D 角色（头顶锚点，如子物体）
    public RectTransform hpBarUI;   // 血条 UI（挂在 Canvas 下）
    public Canvas canvas;           // 所在 Canvas
    public Camera cam;              // 渲染相机的 Camera

    void LateUpdate()
    {
        // 1. 世界坐标 → 屏幕像素
        Vector3 screenPos = cam.WorldToScreenPoint(target.position);

        // 2. 剔除：物体在相机背后时不要显示 UI
        if (screenPos.z < 0)
        {
            hpBarUI.gameObject.SetActive(false);
            return;
        }
        hpBarUI.gameObject.SetActive(true);

        // 3. 屏幕像素 → Canvas 的局部坐标
        //    Overlay 模式传 null；ScreenSpaceCamera 模式传 canvas.worldCamera
        Camera uiCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : canvas.worldCamera;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform, screenPos, uiCam, out local);

        // 4. 把血条摆到该局部坐标（再往上抬一点，让它浮在头顶上方）
        hpBarUI.anchoredPosition = local + new Vector2(0, 20);
    }
}
```

> 要点回顾：① `WorldToScreenPoint` 拿像素；② `z<0` 表示在相机背后要隐藏；③ `ScreenPointToLocalPointInRectangle` 转回 UI 坐标系；④ Overlay 模式 cam 传 **null**，其它模式传对应 Camera，这个坑面试经常问。

---

## 9.8 本章高频「相似 API」对比总表

| 想做的事 | 用哪个 | 别用哪个/注意 |
|---------|--------|--------------|
| 摆 UI 位置 | `RectTransform.anchoredPosition` | `localPosition`/`position`（坐标系不同，易错） |
| 改 UI 尺寸 | `sizeDelta` 或 `SetSizeWithCurrentAnchors` | 直接改 `transform.localScale`（那是缩放，不占布局） |
| 响应完整点击 | `Button.onClick` | `OnMouseDown`（那是 3D 的，UI 上不触发） |
| 响应拖拽/悬停 | `IPointer*Handler` / `EventTrigger` | Button 只有 onClick，不够用 |
| 判断鼠标在不在 UI 上 | `EventSystem.current.IsPointerOverGameObject()` | 自己算屏幕坐标（易漏边界） |
| 点击生成 3D 物体 | `Camera.ScreenPointToRay` + `Raycast` | `ScreenToWorldPoint`（要自己填深度） |
| UI 跟随 3D 物体 | `WorldToScreenPoint` + `ScreenPointToLocalPointInRectangle` | 只用其中一个（坐标对不上） |
| 新项目写界面 | 先 UGUI（资料多），进阶学 UI Toolkit | IMGUI 只用于编辑器 |

---

## 9.9 版本标记（本章涉及的版本化 API）

| API | 标记 |
|-----|------|
| UGUI（`UnityEngine.UI`） | ✅ 稳定、最普及，可放心用 |
| `UnityEngine.UI.Text` | ⚠️ 老组件，新项目推荐 `TMP_Text`（TextMeshPro） |
| UI Toolkit（`UnityEngine.UIElements`） | ⚠️ **较新**：2019.1 引入；2021 左右运行时 UI 逐渐成熟；2023+ 官方主推编辑器扩展 |
| `UIDocument` | ⚠️ 2019+（随 UI Toolkit）；运行时 UI 建议 2021+ |
| `EventSystem` + `StandaloneInputModule` | ✅ 稳定；新输入方案对应 `InputSystemUIInputModule`（Input System 包） |
| `sizeDelta` / `SetSizeWithCurrentAnchors` | ✅ 稳定；宽高分量用 `sizeDelta.x` / `.y`（没有 `widthDelta`/`heightDelta`，防拼写错） |

> 具体版本细节以官方手册为准，这里只给推理方向。

---

## 9.10 本章小结

- 三大方案：**UGUI**（旧但普及，先学）、**UI Toolkit**（新方案，2019+/2021 成熟）、**IMGUI**（编辑器专用）。
- `RectTransform` = UI 的 Transform：`anchorMin/anchorMax` 定锚点，`pivot` 定原点，`anchoredPosition` 定偏移，`sizeDelta` 定尺寸。
- `Canvas` 决定渲染方式（Overlay/Camera/WorldSpace），`CanvasScaler` 负责分辨率适配。
- UGUI 控件：Button/Text/Image/Slider/InputField/ScrollRect/EventTrigger，事件用 `onClick.AddListener` 或事件接口。
- UI 事件三件套：`EventSystem`（调度）+ `StandaloneInputModule`（输入）+ `GraphicRaycaster`（命中）。
- 坐标转换链路：世界 `WorldToScreenPoint` → 像素 → `ScreenPointToLocalPointInRectangle` → UI 坐标；Overlay 模式 cam 传 null。
- UI Toolkit 用 `UIDocument` + `VisualElement` + USS（CSS 风格），`clicked +=` 订阅事件。

---

# 第 10 章 音频、动画与回调

> **本章管辖**：Unity 里"让东西响起来、动起来、以及让代码互相通知"的三块能力——音频（`AudioSource`/`AudioClip`）、动画（`Animator`/`Animation`）、以及事件/回调/委托机制（`UnityEvent`/`delegate`/`event`）。
> **一句话**：`AudioSource` 是"播放器"，`AudioClip` 是"声音文件"；`Animator` 是"基于状态机的动画控制器"（主流），`Animation` 是"老版直接播片段"（旧）；`UnityEvent` 是"能在 Inspector 里拖挂的事件"，`delegate/event` 是"纯 C# 的事件系统"。
> **前置**：建议先看第 1 章（`MonoBehaviour` 生命周期、`GetComponent`）、第 6 章（协程/异步，动画与音频常配合协程用）。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 播放一段背景音乐/音效 | `AudioSource.Play()` + `AudioSource.clip` |
| 播放一个"不打断当前"的一次性音效 | `AudioSource.PlayOneShot(clip)` |
| 暂停/继续/停止当前音频 | `AudioSource.Pause()` / `UnPause()` / `Stop()` |
| 让声音循环播放 | `AudioSource.loop = true` |
| 调音量/音调 | `AudioSource.volume` / `AudioSource.pitch` |
| 做 2D 音效（不随距离衰减） | `AudioSource.spatialBlend = 0` |
| 做 3D 音效（随距离/方位变化） | `AudioSource.spatialBlend = 1` |
| 在某个位置临时播一声 | `AudioSource.PlayClipAtPoint(clip, pos)`（静态） |
| 全局混音/分组调音量 | `AudioMixer`（`AudioMixerGroup`） |
| 加载一个音频文件 | `Resources.Load<AudioClip>("...")` 或拖拽到字段 |
| 运行时动态生成一段音频 | `AudioClip.Create(...)` |
| 拿到音频时长 | `AudioClip.length` |
| 播放状态机动画 | `Animator`（`AnimatorController`） |
| 设置动画参数（开关/数值/触发/整数） | `Animator.SetBool/SetFloat/SetTrigger/SetInteger` |
| 拿到当前正在播的动画状态 | `Animator.GetCurrentAnimatorStateInfo` |
| 直接播某个状态/平滑过渡 | `Animator.Play()` / `Animator.CrossFade()` |
| 调动画播放速度 | `Animator.speed` |
| 判断动画控制器有没有某个参数 | `Animator.HasParameterOfType`（较新） |
| 用老式方式播一段动画 | `Animation.Play()` / `Animation.CrossFade()`（旧） |
| 在动画时间轴上挂回调 | `AnimationEvent` |
| 在 Inspector 里可视化挂事件 | `UnityEvent` + `UnityAction` |
| 用纯 C# 自建事件系统 | `delegate` / `event` / `Action` / `Func` |
| 写匿名回调/闭包 | lambda 表达式 `=>` |
| 做全局事件中心（观察者模式） | `EventManager` 单例 |

---

## 10.1 音频 Audio

### 10.1.1 `class AudioSource : Behaviour` —— 音频播放器

**【是什么】** 挂在 `GameObject` 上的"播放器"组件。它本身**不存声音数据**，只负责"播放、暂停、调音量、做 3D 空间化"。真正的声音数据存在 `AudioClip` 里，通过 `AudioSource.clip` 喂给它。

**【用途】** 场景里任何要发声的物体（背景音乐、脚步声、开枪声、环境音）都挂一个 `AudioSource`。

**【名称含义】** `Audio`（音频）+ `Source`（源/来源）。"声音的来源"。

**【代码示例】**
```csharp
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    public AudioSource source;      // 在 Inspector 拖一个 AudioSource 组件
    public AudioClip hitClip;       // 拖一个音频文件

    void Start()
    {
        source.clip = hitClip;      // 指定要播的声音
        source.Play();              // 播放
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `AudioSource` 与 `AudioClip` | AudioSource 是"播放器"（组件）；AudioClip 是"声音数据"（资源）。一个 AudioSource 同一时刻只能播一个 `clip`，但可叠加 `PlayOneShot` |
| `AudioSource` 与 `AudioListener` | AudioSource 是"发声端"；AudioListener 是"听声端"（通常挂在主摄像机，场景里只能有一个） |

---

### 10.1.2 `AudioSource.clip` 属性

**【是什么】** 指定这个播放器要播的 `AudioClip`（声音资源）。

**【用途】** 播放前先赋值，或运行时切换背景音乐。

**【代码示例】**
```csharp
source.clip = Resources.Load<AudioClip>("Music/bgm"); // 从 Resources 加载
source.Play();
```

**【相似 API 区别】** `clip` 与 `PlayOneShot`：`clip` 是"当前主音轨"，`PlayOneShot` 是"临时叠加音效"，不改变 `clip`。

---

### 10.1.3 `Play()` / `Stop()` / `Pause()` / `UnPause()`

**【是什么】** 控制播放器状态的核心方法。

| 方法 | 作用 |
|------|------|
| `Play()` | 从头播放 `clip`（若已在播则从头重播） |
| `Stop()` | 停止播放，回到开头 |
| `Pause()` | 暂停（停在当前位置） |
| `UnPause()` | 从暂停处继续 |

**【代码示例】**
```csharp
source.Play();      // 开始播
source.Pause();     // 暂停（保留进度）
source.UnPause();   // 继续
source.Stop();      // 停止并回到开头
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Pause()` 与 `Stop()` | Pause 保留播放进度可继续；Stop 彻底停止、进度归零 |
| `Play()` 与 `PlayOneShot()` | Play 播主 clip（会打断当前）；PlayOneShot 叠加播一次，不打断主音轨 |

---

### 10.1.4 `PlayOneShot(AudioClip clip, float volumeScale = 1f)`

**【是什么】** 播放一个**一次性音效**，与当前正在播的主音轨**叠加**，互不打断。适合"开枪、脚步声、命中"这类高频短音效。

**【参数说明】**
- `clip`：要播的音频。
- `volumeScale`（可选，默认 `1f`）：本次播放的音量倍率（相对 `volume`）。

**【返回值】** `void`。

**【代码示例】**
```csharp
using UnityEngine;

public class Gun : MonoBehaviour
{
    public AudioSource source;
    public AudioClip shootClip;

    void Fire()
    {
        // 每次开枪都叠加播一声，不打断背景音乐
        source.PlayOneShot(shootClip, 0.8f);
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Play()` | 播 `clip` 主音轨，会打断当前播放；适合背景音乐 |
| `PlayOneShot()` | 叠加播一次，不打断主音轨；适合高频短音效 |

---

### 10.1.5 `loop` / `volume` / `pitch` 属性

**【用途】** 三个最常用的播放参数。

| 属性 | 类型 | 作用 |
|------|------|------|
| `loop` | `bool` | 是否循环播放（背景音乐用 `true`） |
| `volume` | `float` | 音量，`0`（静音）~ `1`（最大），默认 `1` |
| `pitch` | `float` | 音调/播放速度，`1` 正常，`>1` 变快变尖，`<1` 变慢变沉 |

**【代码示例】**
```csharp
source.loop = true;          // 背景音乐循环
source.volume = 0.5f;        // 音量一半
source.pitch = 1.2f;         // 稍微加快（子弹时间/加速效果常用）
```

---

### 10.1.6 `spatialBlend`（2D / 3D 音效开关）

**【是什么】** `float`，范围 `0`~`1`，决定声音是 **2D**（不随距离衰减）还是 **3D**（随距离/方向变化）。
- `0` = 纯 2D（UI 音效、背景音乐，音量恒定）。
- `1` = 纯 3D（脚步声、枪声，离得越远越小声）。
- 中间值 = 混合。

**【代码示例】**
```csharp
source.spatialBlend = 0f;   // 2D：背景音乐
source.spatialBlend = 1f;   // 3D：场景里的脚步声
```

**【相似 API 区别】** `spatialBlend` 与 `spatialize`：`spatialBlend` 是"2D/3D 混合比例"；`spatialize` 是"是否启用 HRTF 空间化插件"（更高级的 3D 定位，见 10.1.13）。

---

### 10.1.7 `playOnAwake` 属性

**【用途】** `bool`，默认 `true`。物体激活时是否**自动播放**。设为 `false` 后，需要手动调用 `Play()` 才播。

**【代码示例】**
```csharp
source.playOnAwake = false;   // 不自动播，等代码触发
```

---

### 10.1.8 `AudioSource.PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f)` 静态方法

**【是什么】** 不需要挂组件，直接在**某个世界坐标位置**播一声 3D 音效。Unity 内部会临时创建一个带 `AudioSource` 的物体，播完自动销毁。

**【参数说明】**
- `clip`：要播的音频。
- `position`：世界坐标（`Vector3`）。
- `volume`（可选）：音量，默认 `1`。

**【代码示例】**
```csharp
// 在敌人死亡位置播一声爆炸
AudioSource.PlayClipAtPoint(explosionClip, enemy.transform.position, 1f);
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `PlayClipAtPoint` | 静态、临时、一次性，适合"不关心播放器"的场合 |
| `AudioSource.Play()` | 需要先有 AudioSource 组件，可长期控制（暂停/循环/调音量） |

---

### 10.1.9 `AudioMixer`（混音器，简述）

**【是什么】** 一个"调音台"资源（`AudioMixer`），里面可建多个 `AudioMixerGroup`（如 Music、SFX、Master），把不同 `AudioSource` 归到不同组，统一控制音量、加效果。

**【用途】** 做"音乐/音效/语音"分轨，方便一键静音、调平衡。

**【代码示例】**
```csharp
// 把某个 AudioSource 挂到名为 "SFX" 的混音组
source.outputAudioMixerGroup = sfxGroup;   // sfxGroup 是 AudioMixerGroup 引用
```

> 面试常考：`AudioMixer` 的 `SetFloat("Volume", value)` 可动态调组音量，配合 `AudioMixerGroup` 实现"设置里调音乐/音效音量"。

---

### 10.1.10 `AudioClip` —— 声音数据资源

**【是什么】** 音频文件在 Unity 里的资源类型（`AudioClip`）。它只存"声音数据"，不负责播放（播放交给 `AudioSource`）。

**【加载方式】**
1. **拖拽**：在 Inspector 里把音频文件拖到 `public AudioClip` 字段。
2. **Resources**：`Resources.Load<AudioClip>("路径")`（需放在 `Resources` 文件夹）。
3. **代码生成**：`AudioClip.Create`（见下）。

**【代码示例】**
```csharp
public AudioClip bgm;   // 方式1：Inspector 拖拽

// 方式2：从 Resources 加载
AudioClip clip = Resources.Load<AudioClip>("Sounds/coin");
```

---

### 10.1.11 `AudioClip.Create(...)` 静态方法

**【用途】** 在**运行时用代码生成**一段音频（如程序化音效、波形合成）。

**【参数说明】** 常用重载：
- `name`：音频名。
- `lengthSamples`：采样数（总时长 = 采样数 / 采样率）。
- `channels`：声道数（1 单声道 / 2 立体声）。
- `frequency`：采样率（如 `44100`）。
- `stream`：是否流式加载。

**【代码示例】**
```csharp
// 生成 1 秒、44100Hz、单声道的正弦波音效
int sampleRate = 44100;
int samples = sampleRate;   // 1 秒
AudioClip clip = AudioClip.Create("sine", samples, 1, sampleRate, false);

float[] data = new float[samples];
for (int i = 0; i < samples; i++)
    data[i] = Mathf.Sin(2 * Mathf.PI * 440 * i / sampleRate); // 440Hz 正弦波

clip.SetData(data, 0);      // 把波形写进 clip
source.clip = clip;
source.Play();
```

---

### 10.1.12 `AudioClip.length` 属性

**【用途】** 音频**时长**（秒，`float`）。常用于"播完自动销毁""进度条"。

**【代码示例】**
```csharp
float seconds = clip.length;   // 音频总时长（秒）
```

---

### 10.1.13 3D 音效：`spatialize` / `spread` / `dopplerLevel`（简述）

**【是什么】** 3D 音效的进阶参数，让声音更"真实"。

| 参数 | 作用 |
|------|------|
| `spatialize` | `bool`，是否启用 HRTF 空间化插件（更真实的头部相关传输函数，需插件支持） |
| `spread` | `float`，声源"扩散角"（0 指向性，360 全向），影响立体声宽度 |
| `dopplerLevel` | `float`，多普勒效应强度（声源靠近/远离时音调变化，如警笛） |

**【代码示例】**
```csharp
source.spatialize = true;   // 启用空间化
source.spread = 180f;       // 半扩散
source.dopplerLevel = 1f;   // 多普勒强度
```

---

## 10.2 动画 Animator

### 10.2.1 `class Animator : Behaviour` —— 基于状态机的动画控制器

**一句话**：`Animator` 是 Unity **主流**的动画组件，它**不是直接播一个片段**，而是驱动一个 **`AnimatorController`（状态机）**：状态（Idle/跑/跳/攻击）之间通过**参数**和**过渡条件**切换。

**【是什么】** 挂在 `GameObject` 上，负责播放/切换由 `AnimatorController` 定义的动画状态机。

**【用途】** 角色移动、攻击、受击、死亡等所有"有状态切换"的动画。

**【名称含义】** `Animator`（动画师/动画器）。"驱动动画状态机的组件"。

**【代码示例】**
```csharp
using UnityEngine;

public class PlayerAnim : MonoBehaviour
{
    public Animator anim;   // Inspector 拖 Animator 组件

    void Update()
    {
        // 用参数驱动状态机
        anim.SetFloat("Speed", Input.GetAxis("Horizontal"));
        if (Input.GetKeyDown(KeyCode.Space))
            anim.SetTrigger("Jump");
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Animator` 与 `Animation` | Animator 是"状态机"（主流）；Animation 是"直接播片段"（旧，见 10.2.7） |

---

### 10.2.2 `SetBool` / `SetFloat` / `SetTrigger` / `SetInteger` 参数设置

**【是什么】** 四个方法，往状态机里写参数，驱动状态切换。

| 方法 | 参数类型 | 用途 |
|------|---------|------|
| `SetBool(string name, bool value)` | `bool` | 开关型（是否跑步、是否死亡） |
| `SetFloat(string name, float value)` | `float` | 数值型（移动速度、血量） |
| `SetTrigger(string name)` | 无值（触发一次） | 一次性触发（出招、跳跃、受击） |
| `SetInteger(string name, int value)` | `int` | 整数型（状态编号、方向） |

**【代码示例】**
```csharp
anim.SetBool("IsRunning", true);
anim.SetFloat("Speed", 5.2f);
anim.SetTrigger("Attack");      // 出招触发
anim.SetInteger("Direction", 1);
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `SetBool` / `SetFloat` / `SetInteger` | 是"持续状态"，值一直保留 |
| `SetTrigger` | 是"一次性脉冲"，触发后自动复位，适合"按一下打一下" |

---

### 10.2.3 `GetCurrentAnimatorStateInfo(int layerIndex)`

**【用途】** 拿到当前正在播的**状态信息**（`AnimatorStateInfo`），用于判断当前动画、进度、是否播完。

**【参数说明】** `layerIndex`：层索引（默认 `0`）。

**【返回值】** `AnimatorStateInfo`，常用成员：
- `IsName(string name)`：当前状态是否叫这个名字。
- `normalizedTime`：播放进度（0~1，`>=1` 表示播完一遍）。

**【代码示例】**
```csharp
AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

if (info.IsName("Attack"))          // 正在播攻击动画
    Debug.Log("攻击中");

if (info.normalizedTime >= 1f)      // 攻击动画播完
    Debug.Log("攻击结束");
```

---

### 10.2.4 `Play()` / `CrossFade()`

**【用途】** 直接播一个动画状态，或平滑过渡到另一个。

| 方法 | 作用 |
|------|------|
| `Play(string stateName)` | 直接切到指定状态（无过渡，硬切） |
| `CrossFade(string stateName, float transitionDuration)` | 平滑过渡（淡入淡出）到指定状态 |

**【代码示例】**
```csharp
anim.Play("Idle");                          // 硬切到 Idle
anim.CrossFade("Run", 0.2f);                // 0.2 秒平滑过渡到 Run
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Play()` | 立即切换，无过渡 |
| `CrossFade()` | 带过渡时长，更平滑 |

---

### 10.2.5 `animator.speed` 属性

**【用途】** 动画播放速度倍率。`1` 正常，`2` 两倍速，`0` 暂停。

**【代码示例】**
```csharp
anim.speed = 2f;    // 加速（如子弹时间/慢动作用 <1）
anim.speed = 0f;    // 冻结动画
```

---

### 10.2.6 `HasParameterOfType(string name, AnimatorControllerParameterType type)`（较新）

**【用途】** 判断动画控制器里**是否存在**某个指定类型的参数。避免 `SetTrigger` 一个不存在的参数导致警告。

**【参数说明】**
- `name`：参数名。
- `type`：参数类型（`Bool` / `Float` / `Trigger` / `Int`）。

**【返回值】** `bool`。

**【代码示例】**
```csharp
if (anim.HasParameterOfType("Attack", AnimatorControllerParameterType.Trigger))
    anim.SetTrigger("Attack");
```

**【版本标记】** ✅ 较新 API（`Unity 2019.1` 引入），老项目可能没有。

---

### 10.2.7 `class Animation : Behaviour` —— 老版动画组件

**【是什么】** Unity **旧版**动画组件。它不依赖状态机，直接 `Play` 一个 `AnimationClip`。**官方推荐新项目用 `Animator`（状态机），但 `Animation` 组件本身从未被废弃**——2023 LTS 及以后仍完全可用，老项目可继续使用（被移除的只是 `Component.animation` 快捷属性，需改用 `GetComponent<Animation>()`）。

**【代码示例】**
```csharp
public Animation anim;   // 老版 Animation 组件

anim.Play("Walk");       // 直接播名为 Walk 的片段
anim.CrossFade("Run", 0.3f);   // 平滑过渡
```

**【相似 API 区别】Animator vs Animation（面试必考）**

| 维度 | `Animator`（主流） | `Animation`（旧） |
|------|-------------------|-------------------|
| 机制 | 状态机（`AnimatorController`） | 直接播片段 |
| 状态切换 | 参数 + 过渡条件 | 代码直接 Play |
| 复杂动画 | ✅ 支持（多状态、混合树） | ❌ 弱 |
| 地位 | **主流，新项目用** | 旧版，官方不推荐新项目用（未被废弃） |
| 参数驱动 | `SetBool/SetFloat/SetTrigger` | 无 |

---

### 10.2.8 `AnimationClip` 与 `Animator` 绑定

**【是什么】** `AnimationClip` 是"一段动画数据"（关键帧）。它本身不播，需要被 `AnimatorController` 引用（作为状态机里的一个状态），或挂在老版 `Animation` 上。

**【绑定方式】** 在 `AnimatorController` 里把 `AnimationClip` 拖进某个状态（如 Idle 状态），状态机就负责播它。

**【代码示例】**
```csharp
// 运行时拿到 Animator 当前状态对应的 clip（只读）
AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
if (clips.Length > 0)
    Debug.Log("当前动画片段: " + clips[0].clip.name);
```

---

### 10.2.9 `AnimationEvent` —— 在动画时间轴上挂回调

**【是什么】** 在 `AnimationClip` 的**时间轴某个时刻**挂一个回调，动画播到那一刻自动调用指定方法。常用于"攻击动画播到第 0.3 秒时真正造成伤害""脚步声踩到地面时播音效"。

**【注册方式】** 两种：
1. **编辑器**：选中 AnimationClip，在 Animation 窗口时间轴上右键添加事件，填方法名。
2. **代码**：`AnimationEvent` + `AddEvent`。

**【代码示例】**
```csharp
using UnityEngine;

public class AttackEvent : MonoBehaviour
{
    public AnimationClip attackClip;   // 攻击动画

    void Start()
    {
        // 在 0.3 秒处挂一个事件，调用 OnHitFrame
        AnimationEvent evt = new AnimationEvent();
        evt.time = 0.3f;
        evt.functionName = "OnHitFrame";
        attackClip.AddEvent(evt);
    }

    // 动画播到 0.3 秒时自动调用
    public void OnHitFrame()
    {
        Debug.Log("攻击判定帧！造成伤害");
    }
}
```

**【相似注意点】** `AnimationEvent` 与 `UnityEvent`：`AnimationEvent` 是"动画时间轴触发"；`UnityEvent` 是"任意时机手动触发"（见 10.3）。

---

## 10.3 回调 / 委托 / 事件机制（Unity 方式）

### 10.3.1 `UnityAction` 与 `UnityEvent`（可序列化、可在 Inspector 挂事件）

**【是什么】** Unity 自己的一套事件系统：
- `UnityAction`：一个**无返回值**的委托（`delegate void UnityAction()`），可带参数。
- `UnityEvent`：一个**可序列化**的事件容器，能装多个 `UnityAction`，**能在 Inspector 面板里可视化拖挂**（这是它和 C# 原生 `event` 最大的区别）。

**【用途】** UI 按钮点击、动画事件、自定义可拖挂事件。

**【代码示例】**
```csharp
using UnityEngine;
using UnityEngine.Events;

public class MyEvent : MonoBehaviour
{
    // 在 Inspector 里能看到并拖挂方法
    public UnityEvent onPlayerDied;

    void Die()
    {
        onPlayerDied.Invoke();   // 触发所有挂上去的方法
    }
}
```

**Inspector 拖挂**：把 `onPlayerDied` 字段暴露后，在 Inspector 里点 `+`，把任意物体上的方法拖进去即可，无需写代码。

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `UnityEvent` | 可序列化、可在 Inspector 拖挂、可多播 |
| C# 原生 `event` | 纯代码、不可在 Inspector 拖挂 |

---

### 10.3.2 `UnityEvent` vs C# 原生 `event`（对比）

| 维度 | `UnityEvent` | C# `event` |
|------|-------------|-----------|
| 可在 Inspector 拖挂 | ✅ | ❌ |
| 可序列化 | ✅ | ❌ |
| 性能 | 略慢（反射/序列化） | 快 |
| 多播 | ✅ | ✅ |
| 适用 | UI 按钮、可视化配置 | 纯代码事件系统 |

---

### 10.3.3 C# 委托 `delegate` / 事件 `event` / `Action` / `Func`

**【是什么】** C# 原生的事件/回调机制，Unity 里大量使用。

| 类型 | 说明 |
|------|------|
| `delegate` | 自定义委托类型（方法签名） |
| `event` | 基于委托的事件（只能 `+=`/`-=`，外部不能直接 `Invoke`） |
| `Action` | 无返回值委托（`Action` 无参，`Action<T>` 带参） |
| `Func<T,R>` | 有返回值的委托（最后一个泛型是返回值） |

**【代码示例】**
```csharp
using System;

public class Player : MonoBehaviour
{
    // 1. 自定义委托
    public delegate void HealthChanged(int newHealth);
    public event HealthChanged OnHealthChanged;   // 事件

    // 2. 用内置 Action 更省事
    public event Action OnDied;

    int health = 100;

    void TakeDamage(int dmg)
    {
        health -= dmg;
        OnHealthChanged?.Invoke(health);   // 通知订阅者
        if (health <= 0) OnDied?.Invoke();
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `delegate` | 定义方法签名，可声明变量/字段 |
| `event` | 委托的"受保护"封装，外部只能订阅/退订，不能直接触发 |
| `Action` | 无返回值的现成委托，省去自定义 |
| `Func` | 有返回值的现成委托 |

---

### 10.3.4 lambda 与闭包

**【是什么】** lambda（`=>`）是**匿名函数**的简写，用来快速写回调。**闭包**指 lambda 能捕获并记住外层局部变量。

**【代码示例】**
```csharp
using System;

public class LambdaDemo : MonoBehaviour
{
    void Start()
    {
        // lambda 作为回调
        Action sayHi = () => Debug.Log("你好");
        sayHi();

        // 带参数
        Action<int> print = (n) => Debug.Log(n);
        print(42);

        // 闭包：捕获外层变量 count
        int count = 0;
        Action increment = () => count++;   // 记住 count
        increment();
        increment();
        Debug.Log(count);   // 输出 2
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| 命名方法 | 有名字，可复用 |
| lambda | 匿名、就地写、可捕获闭包变量，适合一次性回调 |

---

### 10.3.5 事件调度 `EventManager` 单例实现

**【用途】** 全局事件中心（观察者模式）：任何脚本都能"发事件"，任何脚本都能"订阅事件"，实现**解耦**（A 发消息，B/C/D 各自响应，互不认识）。

**【代码示例】**
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    // 事件名 -> 回调列表
    private Dictionary<string, Action> events = new Dictionary<string, Action>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // 跨场景存活
    }

    // 订阅
    public void AddListener(string eventName, Action callback)
    {
        if (!events.ContainsKey(eventName)) events[eventName] = null;
        events[eventName] += callback;
    }

    // 退订
    public void RemoveListener(string eventName, Action callback)
    {
        if (events.ContainsKey(eventName)) events[eventName] -= callback;
    }

    // 触发
    public void Trigger(string eventName)
    {
        if (events.ContainsKey(eventName)) events[eventName]?.Invoke();
    }
}
```

**使用示例：**
```csharp
// 订阅方（如 UI 血条）
EventManager.Instance.AddListener("OnPlayerDied", () => ShowGameOver());

// 触发方（如玩家死亡）
EventManager.Instance.Trigger("OnPlayerDied");
```

**【面试常考】** 这就是**观察者模式**：发布者（Trigger）与订阅者（AddListener）解耦，通过事件名通信。

---

### 10.3.6 回调函数作为参数传递（参数回调 / `Action`）

**【用途】** 把"一段逻辑"当作参数传给另一个方法，让它在合适时机调用（回调）。常用于异步、协程结束、加载完成。

**【代码示例】**
```csharp
using System;
using UnityEngine;

public class CallbackDemo : MonoBehaviour
{
    // 参数是一个回调（Action）
    void DoSomething(Action onDone)
    {
        Debug.Log("做事中...");
        onDone?.Invoke();   // 完成后调用回调
    }

    void Start()
    {
        // 传一个 lambda 作为回调
        DoSomething(() => Debug.Log("完成！"));
    }
}
```

**【相似 API 区别】**
| API | 区别 |
|-----|------|
| `Action` 回调 | 作为参数传递，函数内调用 |
| `event` | 声明在类上，外部订阅 |

---

## 10.4 本章高频「相似 API」对比总表

| 想做的事 | 用哪个 | 别用哪个/注意 |
|---------|--------|--------------|
| 播背景音乐 | `AudioSource.Play()` + `loop=true` | `PlayOneShot`（那是叠加音效） |
| 播一次性音效 | `AudioSource.PlayOneShot` | `Play()`（会打断主音轨） |
| 2D 音效 | `spatialBlend = 0` | 3D 会随距离衰减 |
| 3D 音效 | `spatialBlend = 1` | 2D 音量恒定 |
| 主流动画 | `Animator`（状态机） | `Animation`（旧） |
| 动画参数 | `SetBool/SetFloat/SetTrigger` | `SetTrigger` 是一次性 |
| 动画时间轴回调 | `AnimationEvent` | `UnityEvent`（任意时机） |
| Inspector 可视化事件 | `UnityEvent` | C# `event`（纯代码） |
| 纯代码事件 | `delegate`/`event`/`Action` | `UnityEvent`（性能略慢） |
| 全局解耦通信 | `EventManager` 单例 | 直接 `GetComponent` 硬引用 |

---

## 10.5 版本标记（本章涉及的版本化 API）

| API | 标记 |
|-----|------|
| `Animator` | ✅ 主流，新项目用 |
| `Animation`（老版） | ⚠️ 旧版，官方推荐 `Animator`，未被废弃，老项目可继续用 |
| `Animator.SetBool/SetFloat/SetTrigger/SetInteger` | ✅ 稳定，长期可用 |
| `Animator.HasParameterOfType` | ✅ 较新（`Unity 2019.1` 引入） |
| `Animator.enabled` | ✅ 继承自 `Behaviour` 的基础属性（非序列化专有用法），始终可用 |
| `AudioSource.PlayClipAtPoint` | ✅ 稳定 |
| `AudioMixer` | ✅ 稳定，主流混音方案 |
| `Resources.Load<AudioClip>` | ⚠️ 仍可用，但推荐 `Addressables` |

> 具体版本细节以官方手册为准，这里只给推理方向。

---

## 10.6 本章小结

- **音频**：`AudioSource` 是播放器，`AudioClip` 是声音数据；`Play` 播主音轨，`PlayOneShot` 叠加音效；`spatialBlend` 决定 2D/3D。
- **动画**：`Animator` 是**基于状态机的动画控制器**（主流），用 `SetBool/SetFloat/SetTrigger/SetInteger` 驱动；`Animation` 是旧版直接播片段。
- **动画事件**：`AnimationEvent` 在时间轴挂回调，适合"攻击判定帧"。
- **回调/事件**：`UnityEvent` 可序列化、可在 Inspector 拖挂；C# `delegate/event/Action/Func` 是纯代码事件；`EventManager` 单例实现观察者模式做全局解耦通信。
- **回调传参**：`Action` 可作为函数参数传递，实现异步/完成回调。

---

# 第 11 章 编辑器扩展

> **本章管辖**：Unity 编辑器（Editor）脚本——写工具、菜单、自定义 Inspector、Gizmos。注意：编辑器代码需放在任意名为 `Editor` 的文件夹下（如 `Assets/Editor`，或各子目录下的 `Editor` 文件夹），且**只在编辑器中运行，不打包进玩家版本**；也可在任意脚本中用 `#if UNITY_EDITOR` 条件编译包住编辑器专属逻辑。
> **一句话**：让 Unity 编辑器按照你的工作流来，而不是你按编辑器的工作流来。
> **前置**：会 C# 即可，建议熟悉 [第 1 章](第01章_核心物件体系.md)。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 加个菜单项 | `[MenuItem("Tools/MyTool")]` + 静态方法 |
| 在编辑器里点一下跑个函数 | `MenuItem` 或 `CustomEditor` 按钮 |
| 自定义组件 Inspector | `[CustomEditor(typeof(MyComponent))]` + `Editor` 子类 |
| 在 Scene 视图画辅助线/球 | `OnDrawGizmos` / `Gizmos.Draw*` |
| 读取项目里的资源 | `AssetDatabase.LoadAssetAtPath<T>` |
| 在编辑器里执行保存/刷新 | `AssetDatabase.SaveAssets()` / `AssetDatabase.Refresh()` |
| 预览 Selection 选中的物体 | `Selection.activeObject` |

---

## 11.1 编辑器脚本基础

### 11.1.1 放置与命名空间

```csharp
// 需放在任意名为 Editor 的文件夹下（如 Assets/Editor），或用 #if UNITY_EDITOR 包裹；
// 不放在 Editor 文件夹不会"报错"，但编辑器 API（如 UnityEditor.*）若直接引用会编译错误
// 引入编辑器命名空间
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif
```

- 编辑器脚本用 `using UnityEditor;`。
- 用 `#if UNITY_EDITOR` 包裹是保险做法（防止打包时编译器去找 Editor 相关类型）。

---

### 11.1.2 `[MenuItem("...")]` 属性 — 加菜单

**【是什么】** 给静态方法加一个菜单项（顶部菜单栏）。
**【用途】** 做一键工具：批量改名、一键生成、数据检查等。

**【代码示例】**
```csharp
using UnityEditor;
using UnityEngine;

public class MyEditorTools : MonoBehaviour
{
    [MenuItem("Tools/打印选中物体名")]
    static void PrintSelected()
    {
        foreach (var obj in Selection.gameObjects)
            Debug.Log(obj.name);
    }

    // 二级菜单：带分隔线、快捷键
    [MenuItem("Tools/批量重置位置 %r")]   // % = Ctrl, # = Shift, & = Alt
    static void ResetAllPosition()
    {
        foreach (var go in Selection.gameObjects)
            go.transform.position = Vector3.zero;
    }
}
```

**【参数说明】** `MenuItem("路径/子路径 快捷键")`：
- `/` 分隔菜单层级；空格后的 `%r` 是快捷键（Ctrl+R）。

【⚠️ 注意】方法**必须 static**。

---

### 11.1.3 `[CustomEditor]` — 自定义 Inspector

**【是什么】** 让你接管某个组件的 Inspector 绘制。
**【用途】** 把一堆字段整理成按钮/滑块/可视化调试面板。

**【代码示例】**
```csharp
using UnityEditor;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int hp = 100;
    public int maxHp = 100;
}

[CustomEditor(typeof(PlayerStats))]
public class PlayerStatsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PlayerStats stats = (PlayerStats)target;

        // 画默认字段
        DrawDefaultInspector();

        // 加一个血条
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("HP");
        EditorGUILayout.IntSlider(ref stats.hp, 0, stats.maxHp);
        EditorGUILayout.EndHorizontal();

        // 一键按钮
        if (GUILayout.Button("回满血"))
        {
            stats.hp = stats.maxHp;
            EditorUtility.SetDirty(stats);   // 标记需要保存
        }
    }
}
```

**【相似区别】** `Editor`（自定义 Inspector） vs `PropertyDrawer`：前者管整个组件；后者管单个字段（`[CustomPropertyDrawer]`）。

---

## 11.2 Gizmos —— 场景可视化

### 11.2.1 `OnDrawGizmos()` / `Gizmos.Draw*`

**【是什么】** 在 Scene 视图里画辅助线（调试可视化），**不影响游戏运行时渲染**。
**【用途】** 画攻击范围、巡逻路径、碰撞盒示意。

**【代码示例】**
```csharp
public class AttackRange : MonoBehaviour
{
    public float radius = 2f;

    // 每帧在 Scene 视图重绘（只在编辑器/开发模式下有）
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void OnDrawGizmosSelected()
    {
        // 选中时才绘制（Scene 里点了这个物体才出现）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius + 0.5f);
    }
}
```

**【相似区别】** `OnDrawGizmos`（一直显示） vs `OnDrawGizmosSelected`（选中才显示）。另外 `Gizmos`（Scene 视图） vs `Debug.DrawLine`（Game 视图也要显示用后者，且颜色为参数）。

---

## 11.3 AssetDatabase —— 编辑器资源操作

> 详细词条见 [第 7 章](第07章_资源与场景.md) 7.2。此处只补编辑器特有场景：

```csharp
using UnityEditor;

[MenuItem("Tools/创建脚本资源")]
static void CreateAsset()
{
    // 创建一个可存储的 ScriptableObject 资源
    var data = ScriptableObject.CreateInstance<MyData>();
    AssetDatabase.CreateAsset(data, "Assets/Data/MyData.asset");
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();   // 让 Unity 识别新文件
}
```

---

## 11.4 版本标记

| API | 标记 |
|-----|------|
| `MenuItem` / `CustomEditor` / `Gizmos` | 长期稳定 |
| `Selection.gameObjects` | 长期稳定 |
| UI Toolkit 版 Editor（`VisualElement` 自定义 Inspector） | ✅ 较新（Unity 2019+），UI Toolkit 相关见 [第 9 章](第09章_UI系统.md) |

---

## 11.5 小结

- 编辑器脚本 → `Assets/Editor` 目录 + `#if UNITY_EDITOR`。
- `[MenuItem]` 做菜单工具；`[CustomEditor]` 接管 Inspector；`Gizmos` 画调试可视化。
- 资源操作走 `AssetDatabase`；改完记得 `SaveAssets` + `Refresh`。

---

# 第 12 章 游戏循环与帧管理

> **本章管辖**：Unity 引擎的"上帝时间表"——`PlayerLoop`、生命周期、`Application`（应用状态）、`QualitySettings`（画质）、`Screen`（屏幕参数）、`Display`、`Coroutine` 调度时机。
> **一句话**：理解"一帧里到底发生了什么"，是优化和排查诡异 bug 的根本。
> **前置**：[第 1 章](第01章_核心物件体系.md) 生命周期 + [第 6 章](第06章_时间与异步.md)。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 知道一帧内执行顺序 | 看下方 PlayerLoop 图 |
| 读应用信息（版本/是否后台） | `Application.*` |
| 退出应用 | `Application.Quit()` |
| 设置目标帧率 | `Application.targetFrameRate` |
| 后台运行时是否暂停 | `Application.runInBackground` |
| 判断是否在编辑器/真机 | `Application.isEditor` / `Application.isPlaying` |
| 获取平台 | `Application.platform` |
| 打开网页/路径 | `Application.OpenURL` |
| 设置画质选项 | `QualitySettings.*` |

---

## 12.1 主循环 PlayerLoop —— "一帧"发生什么

**【是什么】** Unity 引擎的核心是 C++ 写的 **PlayerLoop**，每帧按固定顺序调用一串系统。你的脚本回调只是其中几个环节。

**一帧的简化顺序（面试背这个）：**

```
PlayerLoop 每帧：
 ① 初始化 Initialization       (引擎内部资源准备；Awake 不在这里——它在场景加载/对象实例化时同步调用，早于任何帧)
 ② 预更新 EarlyUpdate          (Input 数据扫描)
 ③ 固定更新 FixedUpdate        (物理，可多次，固定步长)
 ④ 更新 Update                 (你的逻辑、协程 MoveNext)
 ⑤ 后更新 LateUpdate           (摄像机跟随等)
 ⑥ 渲染 Rendering              (SRP/URP/HDRP 执行、WaitForEndOfFrame)
 ⑦ 帧末 EndOfFrame             (销毁队列、OnDestroy)
```

**【用途】** 知道"哪一步该做什么"：物理放 FixedUpdate，渲染前想对齐数据用 LateUpdate。

---

## 12.2 生命周期回调补充（第二课 Depth）

更完整顺序与每个回调的分组，见 [第 1 章 1.4](第01章_核心物件体系.md)。此处补充：

- `OnApplicationFocus(bool)`：应用获得/失去焦点（切窗口）。
- `OnApplicationPause(bool)`：移动端切后台。
- `OnApplicationQuit()`：应用退出时（移动端**不保证**调用）。
- `OnDisable`/`OnDestroy`：同样也在**场景卸载/退出时**触发。

---

## 12.3 Application —— 应用级控制

### 12.3.1 核心静态属性

| 属性 | 含义 |
|------|------|
| `Application.isPlaying` | 是否运行中（编辑器下也 true） |
| `Application.isEditor` | 是否在编辑器内运行 |
| `Application.platform` | 当前平台（`RuntimePlatform.*`） |
| `Application.targetFrameRate` | 目标帧率（默认 -1=不限制） |
| `Application.runInBackground` | 失去焦点是否继续运行 |
| `Application.persistentDataPath` | **可写持久化数据目录**（存档放这） |
| `Application.streamingAssetsPath` | StreamingAssets 目录 |
| `Application.unityVersion` | 引擎版本 |
| `Application.isMobilePlatform` | 是否移动平台 |

**【代码示例：设置帧率 + 存档路径】**
```csharp
Application.targetFrameRate = 60;               // 锁 60 帧
Debug.Log(Application.persistentDataPath);      // C:/Users/xxx/AppData/LocalLow/公司名/产品名

// 存文件到持久化目录
System.IO.File.WriteAllText(
    Path.Combine(Application.persistentDataPath, "save.json"), "{}");
```

### 12.3.2 `Application.Quit()` 与 `Quit(int exitCode)`

```csharp
Application.Quit();          // 退出游戏（桌面/移动端本地）
```
【⚠️ 注意】`Quit` **不会立刻退出**，只是发出退出请求；在**编辑器**里调用它不会关闭编辑器本身，只会退出当前 Play 模式（停止运行）。桌面/移动端构建后可正常退出进程。WebGL 无效。

---

## 12.4 QualitySettings 与帧率

| API | 含义 |
|-----|------|
| `QualitySettings.vSyncCount` | 垂直同步（0=关，1~2=开） |
| `QualitySettings.GetQualityLevel()` | 当前画质级别 |
| `QualitySettings.SetQualityLevel(i)` | 设置画质级别 |
| `QualitySettings.lodBias` / `pixelLightCount` 等 | 画质参数 |

```csharp
QualitySettings.vSyncCount = 0;          // 关垂直同步
Application.targetFrameRate = 60;         // 再用 targetFrameRate 控帧率
```

---

## 12.5 帧调度：协程与 PlayerLoop 的配合

- `yield return null` → 恢复于下一帧（在 Update 阶段之间）。
- `yield return new WaitForEndOfFrame()` → 恢复到**帧末**（渲染后、截图前）。
- 协程本质是编译成状态机的 `IEnumerator`，由引擎在合适时机调用 `MoveNext()`。

> 更完整协同异步体系见 [第 6 章](第06章_时间与异步.md)。

---

## 12.6 版本标记

| API | 标记 |
|-----|------|
| `PlayerLoop` | 内部引擎概念（C++ 层） |
| `Application` / `QualitySettings` | 长期稳定 |
| `Application.targetFrameRate` | 长期稳定 |

---

## 12.7 小结

- 理解 PlayerLoop：`LateUpdate` 在 Update 后、EndOfFrame 最末，渲染最后。
- `Application` 管应用级状态（路径、平台、退出、帧率）。
- `QualitySettings` 管画质/垂直同步。
- 把"该什么时候做"想清楚，就能少踩很多时序雷。