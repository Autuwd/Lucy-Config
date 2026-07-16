# Day 8：输入与物理 — 从按键检测到物理引擎模拟

## 0. 为什么需要输入和物理系统？

在 Raylib/C++ 中，你需要自己处理所有底层细节：

```cpp
// Raylib：手写输入检测和碰撞
if (IsKeyDown(KEY_D)) player.x += speed * dt;

// 手写 AABB 碰撞检测
bool CheckCollision(Vector2 aPos, Vector2 aSize, Vector2 bPos, Vector2 bSize)
{
    return aPos.x < bPos.x + bSize.x && aPos.x + aSize.x > bPos.x
        && aPos.y < bPos.y + bSize.y && aPos.y + aSize.y > bPos.y;
}

// 手写物理：速度、重力、反弹
velocity.y += gravity * dt;
position.x += velocity.x * dt;
position.y += velocity.y * dt;
```

Unity 把输入抽象成**虚拟轴**，把物理交给 **PhysX 引擎**处理。你只需要声明"我希望做什么"，引擎负责实现。

---

## 1. Unity 输入系统架构

### 旧版 Input Manager（了解即可，Unity 正在迁移）

Unity 的输入系统是**由事件驱动的轮询系统**：

```
硬件输入（键盘/鼠标/手柄）
    ↓
操作系统驱动程序
    ↓
Unity 的输入管理器 (C++ 层)
    ↓ 每帧更新输入状态
Input 类（C# API）
    ↓
你的脚本调用 Input.GetKey() 等
```

### 两种输入检测方式

```csharp
// 方式 1：轮询（Polling）——在 Update 中主动查询
void Update()
{
    // 每帧检查按键状态
    if (Input.GetKey(KeyCode.Space))     // 按住
    if (Input.GetKeyDown(KeyCode.Space)) // 按下瞬间
    if (Input.GetKeyUp(KeyCode.Space))   // 松开瞬间
}
```

**GetKey 和 GetKeyDown 的区别：**
```
时间轴：    frame1    frame2    frame3    frame4
按键：      ───■────────────────────■───
             按下                   松开

GetKey:     F    T    T    T    T    F    F
             ↑遇到"按下"状态后一直为 true，直到"松开"

GetKeyDown: T    F    F    F    F    F    F
             ↑只在"按下"的同一帧为 true

GetKeyUp:   F    F    F    F    F    T    F
                                        ↑只在"松开"的同一帧为 true
```

### 虚拟轴——Unity 比 Raylib 多做的

```csharp
void Update()
{
    // GetAxis：带平滑过渡和死区的虚拟轴
    float h = Input.GetAxis("Horizontal");  // 范围 [-1, 1]，带缓动
    // 按下 → 逐渐增加到 1，松开 → 逐渐回到 0
    // 适合角色移动

    // GetAxisRaw：无平滑的虚拟轴
    float hRaw = Input.GetAxisRaw("Horizontal");  // 瞬间跳变到 -1/0/1
    // 适合格子移动、菜单选择
}
```

**虚拟轴的内部处理：**

```
原始按键输入：
  A 键按下 → rawValue = -1
  D 键按下 → rawValue = 1
  都不按  → rawValue = 0

GetAxis 的处理流程：
1. 读取原始输入 (rawValue: -1/0/1)
2. 应用死区（Dead Zone）：小幅度输入被忽略（防止摇杆漂移）
3. 应用灵敏度（Sensitivity）：控制达到极值的速度
4. 应用缓动（Gravity）：控制松开后回到 0 的速度
5. 输出平滑后的值

对比：
GetAxisRaw = 只执行步骤 1
GetAxis    = 执行全部 5 步
```

### 新输入系统（Input System Package）

```csharp
// Unity 的新输入系统（需要从 Package Manager 安装）
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerControls controls;

    void Awake()
    {
        controls = new PlayerControls();

        // 事件驱动——不再需要 Update 中轮询！
        controls.Gameplay.Move.performed += ctx =>
        {
            Vector2 move = ctx.ReadValue<Vector2>();
            Debug.Log($"Move: {move}");
        };

        controls.Gameplay.Jump.performed += ctx => Jump();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();
}
```

---

## 2. 鼠标输入

```csharp
void Update()
{
    // 鼠标位置（像素坐标）
    Vector3 mousePos = Input.mousePosition;
    // (0, 0) = 屏幕左下角
    // (Screen.width, Screen.height) = 屏幕右上角

    // 鼠标在世界空间的位置
    Vector3 worldPos = Camera.main.ScreenToWorldPoint(
        Input.mousePosition + Vector3.forward * 10f);
    // + forward * 10 = 指定 Z 深度（在摄像机前方 10 单位）

    // 鼠标移动量
    float mouseX = Input.GetAxis("Mouse X");  // 横向移动量
    float mouseY = Input.GetAxis("Mouse Y");  // 纵向移动量

    // 鼠标滚轮
    float scroll = Input.GetAxis("Mouse ScrollWheel");

    // 鼠标按键
    if (Input.GetMouseButton(0))      // 左键按住
    if (Input.GetMouseButtonDown(0))  // 左键按下瞬间
    if (Input.GetMouseButtonUp(0))    // 左键松开瞬间
    // 0 = 左键, 1 = 右键, 2 = 中键
}
```

---

## 3. Unity 物理引擎（PhysX）架构

### 物理引擎是什么？

Unity 使用 NVIDIA 的 **PhysX** 作为物理引擎（2D 使用 Box2D）。物理引擎做的事情：

```
每帧（FixedUpdate 中）：

1. 碰撞检测 Broad Phase（粗检测）
   用空间分割算法找出可能碰撞的对象对
   - 3D：Sweep and Prune (SAP)
   - 2D：动态 AABB 树

2. 碰撞检测 Narrow Phase（精确检测）
   对可能的碰撞对做精确的几何检测
   - 球体 vs 球体：距离计算
   - 立方体 vs 立方体：分离轴定理 (SAT)
   - 网格碰撞体 vs 碰撞体：三角形面片检测

3. 求解约束（Solve Constraints）
   计算碰撞响应：反弹力、摩擦力
   求解关节（Joint）约束

4. 积分（Integrate）
   velocity += acceleration * dt
   position += velocity * dt

5. 睡眠管理
   长时间不动的刚体进入睡眠状态（跳过模拟）
```

### Rigidbody（刚体）

```csharp
// Rigidbody 让 GameObject 受物理引擎控制
// 没有 Rigidbody 就没有物理——只有碰撞检测

public class Player : MonoBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // 物理移动——应该在 FixedUpdate 中
        float h = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector3(h * moveSpeed, rb.velocity.y, 0);
    }
}
```

**Rigidbody 的重要属性：**

| 属性 | 说明 | 典型值 |
|------|------|--------|
| mass | 质量，影响碰撞响应 | 1 (默认) |
| drag | 空气阻力，减缓速度 | 0 (太空), 1 (地面) |
| angularDrag | 旋转阻力 | 0.05 (默认) |
| useGravity | 是否受重力影响 | true / false |
| isKinematic | 运动学模式，不受物理力影响，但能推动其他物体 | false |
| constraints | 冻结位置/旋转轴 | FreezePositionZ |

### Collider（碰撞体）

```csharp
// Collider 定义物体的"形状"——用于碰撞检测

// 3D Collider:
// BoxCollider    → 盒子
// SphereCollider → 球体
// CapsuleCollider → 胶囊体（适合角色）
// MeshCollider   → 网格形状（精确但昂贵）
// TerrainCollider → 地形

// 2D Collider:
// BoxCollider2D
// CircleCollider2D
// PolygonCollider2D
// EdgeCollider2D
```

### Collision（碰撞） vs Trigger（触发器）

```csharp
// Collider 分为两种模式：
// 1. 普通碰撞（默认）——物理碰撞，会产生反弹
// 2. 触发器 (IsTrigger = true) ——只检测，不产生物理响应

public class CollisionDemo : MonoBehaviour
{
    // ─── 普通碰撞回调 ───
    void OnCollisionEnter(Collision col)
    {
        // 撞到物体时调用
        Debug.Log($"Collided with: {col.gameObject.name}");
        Debug.Log($"Contact point: {col.contacts[0].point}");
        Debug.Log($"Relative velocity: {col.relativeVelocity}");
    }

    void OnCollisionStay(Collision col)
    {
        // 持续接触时每帧调用
    }

    void OnCollisionExit(Collision col)
    {
        // 离开时调用
    }

    // ─── 触发器回调 ───
    void OnTriggerEnter(Collider other)
    {
        // 进入触发器区域时调用
        // 不会产生物理反弹——适合：
        // - 拾取道具
        // - 区域检测
        // - 陷阱触发
        Debug.Log($"Trigger entered: {other.gameObject.name}");
    }

    void OnTriggerStay(Collider other) { }
    void OnTriggerExit(Collider other) { }
}
```

### 碰撞检测的底层流程

```
OnCollisionEnter 的触发流程：

1. PhysX 检测到两个 Collider 发生了重叠
2. 检查两者是否都有 Rigidbody（至少一个非 kinematic）
3. 执行碰撞响应（反弹、摩擦）
4. Unity 生成 Collision 数据（碰撞点、法线、速度）
5. 在下一个 FixedUpdate 中调用 OnCollisionEnter

注意：OnCollisionEnter 是在 FixedUpdate 中调用的！
所以不要在 OnCollisionEnter 中依赖 Update 的时序。
```

### Raycast（射线检测）

```csharp
// 射线检测：从某点沿某方向发射一条射线，检测撞到什么

void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        // 从摄像机发射射线到鼠标点击位置
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 检测射线是否撞到物体
        if (Physics.Raycast(ray, out hit, 100f))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");
            Debug.Log($"Point: {hit.point}");
            Debug.Log($"Normal: {hit.normal}");
            Debug.Log($"Distance: {hit.distance}");

            // 在命中位置生成特效
            Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }
}

// 2D 射线检测
void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null)
        {
            Debug.Log($"2D Hit: {hit.collider.name}");
        }
    }
}
```

**Physics.Raycast 的性能：**
- 普通的 Raycast 是 O(n) 检测，n 是所有碰撞体的数量
- PhysX 使用空间分区（如 BVH）加速——但大量射线仍需注意性能
- 频繁的 Raycast 可以用 `RaycastCommand` 做批量处理

---

## 4. Update vs FixedUpdate 的物理选择

```csharp
public class Movement : MonoBehaviour
{
    private Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    void Update()
    {
        // Update 中处理输入检测（每帧都响应）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            shouldJump = true;
        }
    }

    private bool shouldJump;

    void FixedUpdate()
    {
        // FixedUpdate 中处理物理（固定时间步，帧率无关）
        float h = Input.GetAxis("Horizontal");

        // ✅ 物理移动：用 velocity 或 AddForce
        rb.velocity = new Vector3(h * speed, rb.velocity.y, 0);

        // ✅ 跳跃：用 AddForce 加冲量
        if (shouldJump)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            shouldJump = false;
        }
    }
}
```

**为什么物理操作要在 FixedUpdate 中？**

```
帧率 60 FPS：
时间:    0ms    16ms    33ms    50ms    66ms
Update:  U0      U1      U2      U3      U4
Fixed:   F0      F0      F1      F1      F2
(物理步长 20ms，有些帧执行一次 FixedUpdate，有些帧两次)

如果物理在 Update 中：
- 60 FPS 时物理步长 16.6ms（和帧率绑定）
- 30 FPS 时物理步长 33.3ms（物理变粗糙，可能穿模）
- 物理行为不一致！

如果物理在 FixedUpdate 中：
- 永远固定步长 20ms
- 无论帧率如何，物理行为一致
- 物理引擎的稳定性有保证
```

---

## 练习：物理版打砖块（对比 Raylib 07）

```csharp
// ─── Ball.cs ───
public class Ball : MonoBehaviour
{
    public float startSpeed = 5f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 给球一个初始速度
        rb.velocity = new Vector2(startSpeed, startSpeed);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Brick"))
        {
            Destroy(col.gameObject);  // 撞到砖块就销毁
        }
    }
}

// ─── Paddle.cs ───
public class Paddle : MonoBehaviour
{
    public float speed = 8f;
    private Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(h * speed, 0);
    }
}

// ─── Brick.cs ───
public class Brick : MonoBehaviour
{
    public int hp = 1;

    void OnCollisionEnter2D(Collision2D col)
    {
        hp--;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}
```

**对比 Raylib 版：**
- Raylib：手写 AABB 碰撞检测 + 手写速度/反弹计算 + 手写砖块销毁
- Unity：`Rigidbody2D` + `Collider2D` + `OnCollisionEnter2D` = 引擎帮你做了所有底层

---

---

## C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity (C#) |
|------|-------------|-----------|
| 按住检测 | `IsKeyDown(KEY_D)` | `Input.GetKey(KeyCode.D)` |
| 按下瞬间 | `IsKeyPressed(KEY_SPACE)` | `Input.GetKeyDown(KeyCode.Space)` |
| 鼠标位置 | `GetMousePosition()` | `Input.mousePosition` |
| 鼠标移动 | `GetMouseDelta()` | `Input.GetAxis("Mouse X")` |
| 虚拟轴 | 无（手写平滑） | `Input.GetAxis("Horizontal")`（内置平滑） |
| 碰撞检测 | 手写 `CheckCollisionRecs()` | `Collider` + `OnCollisionEnter` |
| 物理 | 手写速度/重力/反弹 | `Rigidbody` + PhysX 引擎 |
| 触发器 | 手写区域检测 | `IsTrigger=true` + `OnTriggerEnter` |
| 射线检测 | 手写线段相交 | `Physics.Raycast()` |
| 固定时间步 | 手写 dt 累加 | `FixedUpdate()` 内置 |

## 停靠点

> Unity 输入系统：`GetKey`（按住）vs `GetKeyDown`（按下瞬间）vs `GetKeyUp`（松开瞬间）。
> 虚拟轴（`GetAxis`）带有平滑和死区处理——比 Raylib 的 `IsKeyDown` 更高级。
> 物理引擎（PhysX）自动处理碰撞检测和物理模拟。
> **物理操作必须放在 FixedUpdate 中**（固定时间步），输入检测放在 Update 中。
> `OnCollisionEnter` = 物理碰撞响应，`OnTriggerEnter` = 纯检测不反弹。

