# Day 6：生命周期与组件 — Unity 引擎的帧循环架构

## 0. 为什么需要生命周期？

在 Raylib/C++ 中，游戏循环是你手写的：

```cpp
// Raylib 的典型帧循环
InitWindow(800, 600, "Game");
while (!WindowShouldClose())
{
    float dt = GetFrameTime();    // 1. 获取帧时间
    UpdatePlayer(dt);             // 2. 更新逻辑
    UpdateEnemies(dt);
    BeginDrawing();               // 3. 渲染
    ClearBackground(RAYWHITE);
    DrawPlayer();
    DrawEnemies();
    EndDrawing();
}
CloseWindow();
```

Unity 将这个循环内置到引擎中——你**不需要写 while 循环**。Engine 为你提供了**回调函数**（Awake、Start、Update...），你只需在回调中写逻辑，引擎决定什么时候调用。

---

## 1. Unity 引擎的 PlayerLoop——C++ 层的核心

Unity 引擎核心是用 C++ 写的。主循环叫做 **PlayerLoop**，它在每帧执行一系列系统回调：

```
PlayerLoop 的简化结构（每帧执行顺序）：
┌─────────────────────────────────────────┐
│ 1. Initialization (初始化)               │
│    - 处理场景加载队列                     │
│    - 调用新创建对象的 Awake               │
├─────────────────────────────────────────┤
│ 2. EarlyUpdate (预更新)                  │
│    - 输入系统更新 (Input.GetKey 的数据来源)│
│    - 物理引擎的预处理                       │
├─────────────────────────────────────────┤
│ 3. FixedUpdate (固定时间步物理)           │
│    - 调用 MonoBehaviour.FixedUpdate()    │
│    - 物理引擎模拟 (PhysX)                │
│    - 可执行多次（如果帧时间 > 固定步长）   │
├─────────────────────────────────────────┤
│ 4. Update (逻辑更新)                     │
│    - 调用 MonoBehaviour.Update()         │
│    - 协程的 MoveNext() 调度              │
├─────────────────────────────────────────┤
│ 5. LateUpdate (后处理更新)               │
│    - 调用 MonoBehaviour.LateUpdate()     │
│    - 摄像机跟随通常放这里                 │
├─────────────────────────────────────────┤
│ 6. Rendering (渲染)                      │
│    - 摄像机渲染                           │
│    - 调用 OnWillRenderObject             │
│    - 执行渲染管线 (SRP/URP/HDRP)         │
│    - WaitForEndOfFrame 的协程恢复         │
├─────────────────────────────────────────┤
│ 7. EndOfFrame (帧结束)                   │
│    - 销毁队列中的对象 (Destroy 延迟执行)   │
│    - 调用 OnDestroy                      │
└─────────────────────────────────────────┘
```

**关键：** Unity 的所有生命周期回调都是 PlayerLoop 的一部分。你不需要控制循环，引擎控制——你只需要在正确的阶段做正确的事。

---

## 2. MonoBehaviour 生命周期顺序

```csharp
public class LifecycleDemo : MonoBehaviour
{
    // ─── 阶段 1：对象创建 ───

    void Awake()
    {
        // 对象被实例化时立即调用（即使脚本 disabled）
        // 类似构造函数——用于初始化内部引用
        // 在对象整个生命周期中只调用一次
        // 如果一个 GameObject 被 SetActive(false)，Awake 仍然会先执行
        Debug.Log("1. Awake");
    }

    void OnEnable()
    {
        // 每次对象启用时调用
        // 第一次调用在 Awake 之后、Start 之前
        // SetActive(true) 时也会再次触发
        // 用于订阅事件、重置状态
        Debug.Log("2. OnEnable");
    }

    void Start()
    {
        // 第一帧 Update 之前调用
        // 注意：如果脚本 disabled，要在 OnEnable 之后才调用
        // 用于初始化数据（因为此时所有 Awake 已完成）
        // 用于获取外部对象的引用（因为其他对象的 Awake 已执行）
        Debug.Log("3. Start");
    }

    // ─── 阶段 2：帧循环 ───

    void FixedUpdate()
    {
        // 固定时间步调用（默认 0.02 秒 = 50 FPS）
        // 和帧率无关！如果你的游戏跑 30 FPS，FixedUpdate 每帧可能跑不到一次
        // 如果你的游戏跑 200 FPS，FixedUpdate 可能每帧跑不到一次（跳帧）
        // 用于物理计算：Rigidbody 移动、AddForce
        // 物理引擎的步进在这里发生
        Debug.Log("4. FixedUpdate");
    }

    void Update()
    {
        // 每帧调用一次（帧率越高调用越频繁）
        // 用于多数逻辑：输入检测、状态更新、非物理移动
        // 频率取决于 Time.deltaTime（上一帧耗时）
        Debug.Log("5. Update");
    }

    void LateUpdate()
    {
        // 在所有 Update 完成后调用
        // 用于摄像机跟随（确保目标已移动）
        // 用于需要 Update 结果的计算
        Debug.Log("6. LateUpdate");
    }

    // ─── 阶段 3：对象销毁 ───

    void OnDisable()
    {
        // 对象禁用时调用
        // SetActive(false) 时触发
        // 对象销毁时也会先触发 OnDisable，再触发 OnDestroy
        // 用于取消事件订阅、清理资源
        Debug.Log("7. OnDisable");
    }

    void OnDestroy()
    {
        // 对象销毁时调用
        // 场景卸载时调用
        // 用于清理非托管资源、取消事件订阅
        Debug.Log("8. OnDestroy");
    }
}
```

### 执行顺序验证

```
创建一个 GameObject 挂载该脚本：
Awake → OnEnable → Start → FixedUpdate → Update → LateUpdate

禁用对象 (SetActive(false))：
OnDisable

重新启用 (SetActive(true))：
OnEnable → Start (注意：Awake 不会再次执行)

销毁对象 (Destroy)：
OnDisable → OnDestroy
```

### 多个脚本的执行顺序

```
情况：一个 GameObject 上有两个脚本 ScriptA 和 ScriptB

默认顺序：
ScriptA.Awake → ScriptB.Awake (顺序不确定！)
ScriptA.Start → ScriptB.Start (与 Awake 顺序一致)
Update 和 LateUpdate 同理

控制顺序：
Edit → Project Settings → Script Execution Order
可以设置脚本的执行优先级
数字越小越先执行
```

---

## 3. GetComponent<T>()——组件系统的核心

### GameObject 的组件存储

```
每个 GameObject 内部维护一个组件列表：
┌─────────────────────────────────┐
│ GameObject "Player"              │
├─────────────────────────────────┤
│ Transform (必须有一个)            │
│ MeshRenderer                    │
│ PlayerScript (你的脚本)           │
│ Rigidbody                       │
│ Collider                        │
└─────────────────────────────────┘
```

### GetComponent 的查找逻辑（简化）

```csharp
public Component GetComponent(Type type)
{
    // 内部是线性遍历组件数组
    for (int i = 0; i < components.Count; i++)
    {
        if (components[i].GetType() == type ||
            type.IsAssignableFrom(components[i].GetType()))
        {
            return components[i];
        }
    }
    return null;  // 没找到
}
```

**性能：** 普通 `GetComponent<T>()` 是 O(n) 查找，小规模组件没有性能问题。但应**避免在 Update 中每帧调用**。

### 缓存模式——最佳实践

```csharp
public class Player : MonoBehaviour
{
    // ❌ 坏：每帧调用 GetComponent
    void Update()
    {
        Rigidbody rb = GetComponent<Rigidbody>();  // 每帧分配 + 查找！
        rb.velocity = ...;
    }
}

public class Player : MonoBehaviour
{
    // ✅ 好：Awake 中缓存
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();  // 一次查找，永久使用
    }

    void Update()
    {
        rb.velocity = ...;  // 直接使用缓存，零查找
    }
}
```

### 相关方法

```csharp
// 获取自身组件
GetComponent<T>()           // 获取一个组件
GetComponents<T>()          // 获取所有同类型组件

// 获取子对象组件（包含自身）
GetComponentInChildren<T>()           // 第一个找到的
GetComponentsInChildren<T>()          // 所有

// 获取父对象组件（包含自身）
GetComponentInParent<T>()             // 第一个找到的
GetComponentsInParent<T>()            // 所有

// 特殊：在自身或子对象中查找，不包括自身
GetComponentInChildren<T>(false);     // includeInactive = false
```

---

## 4. Update、FixedUpdate、LateUpdate 的时序关系

### Update——逻辑帧

```csharp
void Update()
{
    // 每帧调用一次
    // Time.deltaTime 是上一帧到这一帧的时间
    // 如果帧率是 60 FPS，deltaTime ≈ 0.0167 秒
    // 如果帧率是 30 FPS，deltaTime ≈ 0.0333 秒
    // 如果帧率是 10 FPS，deltaTime ≈ 0.1 秒
    // 所以：用 deltaTime 做乘法来保持帧率无关

    transform.position += Vector3.right * speed * Time.deltaTime;
    // 无论帧率是 60 还是 30，每秒移动的距离一样
}
```

### FixedUpdate——物理帧

```csharp
void FixedUpdate()
{
    // 固定时间步调用，和时间无关
    // 默认：0.02 秒 = 50 Hz
    // Time.fixedDeltaTime 是固定值（默认 0.02）
    // 物理引擎在这个时间步中模拟

    // 物理移动用 fixedDeltaTime
    rb.MovePosition(transform.position + move * Time.fixedDeltaTime);
}
```

**Update vs FixedUpdate 的关键区别：**

```
情况 1：游戏跑 60 FPS
- Update: 每秒 60 次 (每 0.0167 秒)
- FixedUpdate: 每秒 50 次 (每 0.02 秒)
- 有些帧可能没有 FixedUpdate，有些帧一次，有些两次

情况 2：游戏跑 30 FPS
- Update: 每秒 30 次
- FixedUpdate: 仍然是每秒 50 次
- 每帧 FixedUpdate 可能执行 1~2 次

情况 3：游戏跑 200 FPS
- Update: 每秒 200 次
- FixedUpdate: 仍然是每秒 50 次
- 很多帧没有 FixedUpdate
```

### LateUpdate——后处理

```csharp
public class CameraFollow : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        // 在所有 Update 完成后才执行
        // 此时 target 已经移动完毕
        // 避免：Camera 在 Player 移动前就更新了位置
        transform.position = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );
    }
}
```

---

## 5. 生命周期事件的常见用途

| 方法 | 用途 | 说明 |
|------|------|------|
| Awake | 组件缓存、引用初始化 | 即使脚本禁用也执行 |
| OnEnable | 订阅事件、重置状态 | 每次启用都触发 |
| Start | 初始化数据、依赖其他组件 | 所有 Awake 之后 |
| FixedUpdate | 物理计算 | 固定时间步 |
| Update | 输入检测、非物理逻辑 | 每帧 |
| LateUpdate | 摄像机跟随、后处理 | Update 之后 |
| OnDisable | 取消事件订阅 | 禁用时 |
| OnDestroy | 清理资源 | 销毁时 |

---

## 练习：完整的帧循环应用

```csharp
using UnityEngine;

public class Day06_Lifecycle : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("References")]
    private Rigidbody rb;
    private Renderer rend;

    // ─── 初始化阶段 ───

    void Awake()
    {
        Debug.Log("[Awake] Caching components...");
        // 缓存组件——在 Awake 中完成，确保 Start 可以直接使用
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();

        if (rb == null)
            Debug.LogError("Rigidbody not found! Add one in Inspector.");
    }

    void Start()
    {
        Debug.Log("[Start] Initializing state...");
        // 此时所有对象的 Awake 已执行完毕
        // 可以安全地访问其他对象
    }

    // ─── 帧循环阶段 ───

    void Update()
    {
        // 输入检测——每帧都需要响应
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[Update] Jump pressed, applying force in FixedUpdate...");
            isJumpRequested = true;
        }

        // 颜色闪烁演示
        if (Input.GetKeyDown(KeyCode.F))
        {
            rend.material.color = Random.ColorHSV();
        }
    }

    private bool isJumpRequested;

    void FixedUpdate()
    {
        // 物理移动——帧率无关
        float h = Input.GetAxis("Horizontal");
        Vector3 velocity = rb.velocity;
        velocity.x = h * moveSpeed;
        rb.velocity = velocity;

        // 跳跃请求在 FixedUpdate 中处理
        if (isJumpRequested)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumpRequested = false;
        }
    }

    void LateUpdate()
    {
        // 简单的摄像机跟随
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 pos = mainCam.transform.position;
            pos.x = transform.position.x;
            mainCam.transform.position = pos;
        }
    }

    // ─── 销毁阶段 ───

    void OnDisable()
    {
        Debug.Log("[OnDisable] Object disabled");
    }

    void OnDestroy()
    {
        Debug.Log("[OnDestroy] Object destroyed");
    }
}
```

---

---

## C++/Raylib 对照总结

| Raylib (C++) | Unity (C#) | 说明 |
|-------------|-----------|------|
| `InitWindow()` | `Awake()` | 初始化 |
| while 前准备 | `Start()` | 第一帧前准备 |
| `while (!WindowShouldClose())` | `Update()`（每帧） | 帧循环主体 |
| `GetFrameTime()` | `Time.deltaTime` | 帧时间 |
| 固定步长物理 | `FixedUpdate()` + `Time.fixedDeltaTime` | 物理帧 |
| — | `LateUpdate()` | 后处理更新 |
| `BeginDrawing()`/`EndDrawing()` | 渲染管线自动 | 渲染 |
| `CloseWindow()` | `OnDestroy()` | 清理 |
| 手动 AABB | `Collider` + `OnCollisionEnter` | Unity 自动碰撞检测 |
| 手动对象管理 | `GetComponent<T>()` | 组件式访问 |

## 停靠点

> Unity 的 PlayerLoop = 帮你封装好的 while 循环。你在回调中写逻辑，引擎决定何时调用。
> `Update` = 逻辑帧（帧率相关），`FixedUpdate` = 物理帧（固定时间步），`LateUpdate` = 后处理。
> `Awake` = 初始化组件引用（不管脚本是否启用），`Start` = 初始化数据。
> `GetComponent` 是 O(n) 查找——在 Awake 中缓存避免每帧调用。
> Destroy 不会立即删除——当前帧结束后才真正执行。

