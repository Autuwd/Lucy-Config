# Day 8：输入与物理 — 深入篇：New Input System、批量物理查询与高级碰撞

## 0. 引言：从轮询到事件驱动

上一章我们学习了旧版 Input Manager 和 PhysX 的基础。但 Unity 正在全面迁移到 **New Input System**（Input System Package），同时物理引擎提供了大量底层 API。

在 Raylib/C++ 中，输入检测是纯轮询（`IsKeyDown`），物理是手算。Unity 的新输入系统则是**事件驱动的**——不需要每帧轮询，注册一次回调即可。

本文深入新的输入架构和物理引擎的高级查询能力。

---

## 1. New Input System 架构

### 系统层级

```
硬件设备（键盘/鼠标/手柄/触屏/VR 控制器）
    ↓
设备接口层（Input Devices）
    ↓ 原生设备事件
Input System 运行时（C++ 层）
    ↓ 处理原始事件、去重、插值
Input Action Asset（你的配置）
    ├── Action Maps（不同情境的映射）
    │   ├── Gameplay（游戏操作）
    │   │   ├── Move (Vector2)
    │   │   ├── Jump (Button)
    │   │   └── Fire (Button)
    │   └── UI（UI 操作）
    │       ├── Navigate (Vector2)
    │       └── Submit (Button)
    │
    └── Bindings（按键到 Action 的映射）
         ├── Move: WASD → Vector2 composite
         ├── Move: 左摇杆 → Vector2
         └── Jump: Space / A 键
```

### 代码生成——C# 绑定

```csharp
// 推荐方式：创建 InputActionAsset → 启用 Generate C# Class
// 选择你的 .inputactions 文件 → Inspector → Generate C# Class

// 自动生成一个 PlayerControls 类：
using UnityEngine;
using UnityEngine.InputSystem;

public class NewInputDemo : MonoBehaviour
{
    // 自动生成的类（由 .inputactions 文件生成）
    private PlayerControls controls;

    // 缓存的 Action 引用
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction fireAction;

    void Awake()
    {
        controls = new PlayerControls();

        // 保存引用（避免每帧字符串查找）
        moveAction = controls.Gameplay.Move;
        jumpAction = controls.Gameplay.Jump;
        fireAction = controls.Gameplay.Fire;
    }

    void OnEnable()
    {
        // 启用所有 Gameplay Action（输入开始生效）
        controls.Gameplay.Enable();

        // 注册事件回调（事件驱动——不需要 Update 轮询）
        jumpAction.performed += OnJump;
        fireAction.performed += OnFire;
        fireAction.started += OnFireStart;    // 按下瞬间
        fireAction.canceled += OnFireCancel;  // 松开瞬间
    }

    void OnDisable()
    {
        // 取消注册
        jumpAction.performed -= OnJump;
        fireAction.performed -= OnFire;
        fireAction.started -= OnFireStart;
        fireAction.canceled -= OnFireCancel;

        // 禁用输入
        controls.Gameplay.Disable();
    }

    void Update()
    {
        // 轮询仍然可以——但这是事件驱动的补充
        // Vector2 move = moveAction.ReadValue<Vector2>();

        // 但如果已经在 Performed 回调中处理了
        // Update 中就不需要再读了
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        // ctx.performed = 触发（按钮按下）
        Debug.Log($"Jump! frame: {Time.frameCount}");
    }

    void OnFire(InputAction.CallbackContext ctx)
    {
        // 适合：射击（按住连发）
        Debug.Log("Fire performed!");
    }

    void OnFireStart(InputAction.CallbackContext ctx)
    {
        // 适合：单发射击（只射一次）
        Debug.Log("Fire started!");
    }

    void OnFireCancel(InputAction.CallbackContext ctx)
    {
        Debug.Log("Fire released!");
    }
}
```

### 手动创建 Action——不用 .inputactions 文件

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class ManualInput : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction lookAction;

    void Awake()
    {
        // 创建一个 Move Action（Vector2，用 WASD 控制）
        moveAction = new InputAction(
            "Move",
            InputActionType.Value,          // 持续值
            "<Keyboard>/w",                  // 默认绑定
            interactions: "1DAxis"           // 交互修饰
        );

        // 更复杂的绑定——组合键
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // 添加备用绑定
        moveAction.AddBinding("<Gamepad>/leftStick");

        // 注册回调
        moveAction.performed += ctx =>
        {
            Vector2 move = ctx.ReadValue<Vector2>();
            Debug.Log($"Move: {move}");
        };

        moveAction.Enable();

        // 创建一个 Look Action（鼠标）
        lookAction = new InputAction(
            "Look",
            InputActionType.PassThrough,     // 透传——每次输入变化都触发
            "<Mouse>/delta"                   // 鼠标移动增量
        );
    }
}
```

### CallbackContext 的完整用法

```csharp
void OnActionTriggered(InputAction.CallbackContext ctx)
{
    // 阶段（Phase）
    switch (ctx.phase)
    {
        case InputActionPhase.Disabled:
            break;
        case InputActionPhase.Waiting:
            break;
        case InputActionPhase.Started:
            // 按钮按下的第一帧
            // 合成动作的开始
            break;
        case InputActionPhase.Performed:
            // 动作触发
            // 按钮按下、值变化超出阈值
            break;
        case InputActionPhase.Canceled:
            // 动作取消
            // 按钮松开、值回到默认
            break;
    }

    // 读取值
    float val = ctx.ReadValue<float>();
    Vector2 vec = ctx.ReadValue<Vector2>();
    // Vector3, Quaternion, Button, 自定义类型...

    // 元数据
    double time = ctx.time;             // 触发时间
    double startTime = ctx.startTime;   // 开始时间
    InputControl control = ctx.control; // 哪个设备触发的
    int tapCount = ctx.tapCount;        // 连击次数（如果配置了 MultiTap）
}
```

---

## 2. 输入绑定高级用法

### 组合绑定（Composite）

```
2D Vector 组合——将四个方向映射到一个 Vector2：

W  ↑ (0, 1)
S  ↓ (0, -1)
A  ← (-1, 0)
D  → (1, 0)

按键组合——同时按多个键触发一个 Action：
"Reload" = Keyboard.r + Gamepad.x
"Special" = LeftShift + Space（Shift 作为修饰键）
```

### 交互（Interactions）——控制触发行为

```csharp
// 在 .inputactions 编辑器中配置，或代码中设置：

// Hold——长按触发
// Hold(duration: 0.5) → 按住 0.5 秒后触发
InputAction holdAction = new InputAction(
    "SuperJump",
    interactions: "Hold(duration=0.5)"
);

// Tap——轻触
// Tap → 快速按下松开触发
InputAction tapAction = new InputAction(
    "Tap",
    interactions: "Tap"
);

// MultiTap——多次敲击
// MultiTap(tapCount: 2, tapTime: 0.5)
// → 0.5 秒内按两次触发
InputAction doubleTap = new InputAction(
    "DoubleTap",
    interactions: "MultiTap(tapCount=2,tapTime=0.5)"
);

// SlowTap——缓慢按下
// SlowTap(duration: 0.3) → 按住超过 0.3 秒才触发
InputAction slowTap = new InputAction(
    "SlowTap",
    interactions: "SlowTap(duration=0.3)"
);
```

### 输入路径（Binding Path）

```
完整语法：<设备>/<控件>#[修饰]

举例：
<Keyboard>/space                  → 空格键
<Keyboard>/w                      → W 键
<Mouse>/leftButton                → 鼠标左键
<Mouse>/delta                     → 鼠标移动
<Gamepad>/leftStick               → 左摇杆
<Gamepad>/buttonSouth             → A/X 按钮
<XInputController>/leftTrigger    → Xbox 左扳机
<Touchscreen>/primaryTouch/press  → 触摸按下

设备通配符：
<Keyboard>/*                       → 所有键盘按键
<Gamepad>/*                        → 所有手柄控制
<XRController>/*                   → VR 控制器
```

---

## 3. 2D 物理优化

### PhysicsMaterial2D——摩擦力与弹性

```csharp
using UnityEngine;

public class Physics2DMaterialDemo : MonoBehaviour
{
    // 创建 PhysicsMaterial2D（Assets → Create → Physics2D Material）
    // 然后挂到 Collider2D 上

    // 关键属性：
    // - friction: 摩擦力（0 = 无摩擦，1 = 最大）
    // - bounciness: 弹性（0 = 不弹，1 = 完美反弹）
    
    // 代码中设置：
    void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        
        PhysicsMaterial2D mat = new PhysicsMaterial2D();
        mat.friction = 0.3f;
        mat.bounciness = 0.5f;
        
        col.sharedMaterial = mat;  // 注意：用 sharedMaterial 不新建实例
        // 如果只需要这个对象不同，用 col.material = mat（新建实例）
    }
}
```

### Physics2D 设置优化

```csharp
// Edit → Project Settings → Physics 2D

// 关键优化参数：

/*
1. Velocity Threshold（速度阈值）
   默认：1
   调高：小速度物体更快进入睡眠（节省 CPU）
   调低：小速度物体保持活跃（更精确）

2. Position Iterations（位置迭代次数）
   默认：8
   减少：更快但精度低（穿模风险）
   增加：更精确但更慢

3. Velocity Iterations（速度迭代次数）
   默认：8
   同上

4. Auto Sync Transforms（自动同步变换）
   默认：true
   关闭：手动调用 Physics2D.SyncTransforms()
   适合：大量物理物体移动时手动控制同步时机

5. Auto Simulation（自动模拟）
   默认：true
   关闭：手动调用 Physics2D.Simulate()
   适合：自定义物理时间步、帧冻结效果
*/
```

### Physics2D.SyncTransforms——手动控制变换同步

```csharp
public class SyncTransformsDemo : MonoBehaviour
{
    void Start()
    {
        // 关闭自动同步（Project Settings → Physics 2D → Auto Sync Transforms）
        // 然后在代码中手动管理

        // 当你批量移动大量对象时：
        void Update()
        {
            // 移动 100 个对象
            for (int i = 0; i < 100; i++)
            {
                transforms[i].position += 
                    Vector3.right * Time.deltaTime;
            }

            // 一次性同步——只触发一次物理空间重建
            // 而不是每移动一个对象触发一次
            Physics2D.SyncTransforms();
            // 3000: 手动同步后，物理引擎知道所有变换的最新位置
            // 此时发射的射线/碰撞检测才准确

            // 如果忘了调用：
            // 物理引擎还在用"旧的"变换数据
            // → 射线检测不到正确的位置
        }
    }
}
```

---

## 4. 物理调试可视化

```csharp
using UnityEngine;

public class PhysicsDebug : MonoBehaviour
{
    // 启用物理调试可视化
    void Start()
    {
        // 方式 1：Scene 视图中的调试按钮
        // Gizmos 菜单 → Physics Debugger

        // 方式 2：代码控制
        PhysicsVisualizationSettings.activateUseCase(
            PhysicsVisualizationSettings.PhysicsVisualizationUseCase
                .CollisionDetection
        );
        // 显示碰撞检测的包围盒
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 手动绘制 Collider 包围盒
        Collider[] colliders = GetComponents<Collider>();
        foreach (var col in colliders)
        {
            Bounds bounds = col.bounds;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        // 绘图显示 OverlapSphere
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, 5f);
    }
}
```

### Physics Debugger（内置调试窗口）

```
Window → Analysis → Physics Debugger

功能：
1. 碰撞体包围盒显示
2. 刚体睡眠状态（绿色 = 睡眠，红色 = 活跃）
3. 接触点显示
4. 关节约束显示
5. 物理帧时间分析

快捷键：Toggle 时按 Gizmos 菜单中的 Physics 开关
```

---

## 5. RaycastCommand——批量并⾏射线检测

当需要发射大量射线（如 AI 视野、射击判定、雷达扫描）时，单条 Raycast 效率太低。**RaycastCommand** 将射线检测提交为 Job。

```csharp
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class BatchRaycast : MonoBehaviour
{
    public int rayCount = 1000;
    private NativeArray<RaycastCommand> commands;
    private NativeArray<RaycastHit> results;

    void Start()
    {
        // 预分配原生数组
        commands = new NativeArray<RaycastCommand>(rayCount, Allocator.Persistent);
        results = new NativeArray<RaycastHit>(rayCount, Allocator.Persistent);
    }

    void Update()
    {
        // 准备射线数据
        for (int i = 0; i < rayCount; i++)
        {
            Vector3 origin = transform.position;
            Vector3 direction = Random.onUnitSphere;
            commands[i] = new RaycastCommand(
                origin,
                direction,
                maxDistance: 100f,
                layerMask: Physics.DefaultRaycastLayers,
                maxHits: 1
            );
        }

        // 调度批量 Job——所有射线并⾏执行
        JobHandle handle = RaycastCommand.ScheduleBatch(
            commands,
            results,
            batchSize: 32,       // 每批 32 条射线
            default(JobHandle)
        );

        // 等待完成
        handle.Complete();

        // 处理结果
        int hitCount = 0;
        for (int i = 0; i < rayCount; i++)
        {
            if (results[i].collider != null)
            {
                hitCount++;
                // 处理碰撞信息
            }
        }
        // Debug.Log($"Hit rate: {(float)hitCount / rayCount:P}");
    }

    void OnDestroy()
    {
        // 释放原生内存
        if (commands.IsCreated) commands.Dispose();
        if (results.IsCreated) results.Dispose();
    }
}
```

### 性能对比

```
单条 Physics.Raycast (x1000)：
- 每条射线 → 一次 C++ 函数调用 + 空间查询
- 主线程串行执行
- 大约 5-10ms（1000 条射线）

RaycastCommand.ScheduleBatch (x1000)：
- 所有射线 → 一次 Job 调度
- 工作线程并⾏执行
- 大约 1-2ms（1000 条射线）
- 快 5-10 倍
```

---

## 6. NonAlloc 查询——零 GC 碰撞检测

```csharp
public class NonAllocDemo : MonoBehaviour
{
    [Header("Query Settings")]
    public float checkRadius = 5f;
    public LayerMask targetLayers;

    // 预分配缓存（避免每帧分配）
    private Collider[] colliderCache = new Collider[32];
    private RaycastHit[] hitCache = new RaycastHit[32];

    void Update()
    {
        // ─── OverlapSphereNonAlloc ───
        // 检测球体范围内的所有 Collider
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            checkRadius,
            colliderCache,
            targetLayers
        );

        // count = 实际碰撞到的数量（≤ 32）
        for (int i = 0; i < count; i++)
        {
            Collider col = colliderCache[i];
            // 处理碰撞体...
        }

        // ─── SphereCastNonAlloc ───
        // 球体推进式检测
        int hitCount = Physics.SphereCastNonAlloc(
            transform.position,
            radius: 1f,
            direction: Vector3.forward,
            results: hitCache,
            maxDistance: 50f,
            layerMask: targetLayers
        );

        // ─── BoxCastNonAlloc ───
        int boxHits = Physics.BoxCastNonAlloc(
            center: transform.position,
            halfExtents: Vector3.one * 0.5f,
            direction: Vector3.forward,
            results: hitCache
        );

        // ─── CapsuleCastNonAlloc ───
        Vector3 point1 = transform.position + Vector3.up;
        Vector3 point2 = transform.position - Vector3.up;
        int capsuleHits = Physics.CapsuleCastNonAlloc(
            point1: point1,
            point2: point2,
            radius: 0.5f,
            direction: Vector3.forward,
            results: hitCache
        );

        // ─── CheckSphere ───
        // 只检测是否有碰撞（不关心谁）
        bool isBlocked = Physics.CheckSphere(
            transform.position, 1f, targetLayers
        );
    }
}

// 对比：
// Physics.OverlapSphere → 每次返回新的 Collider[]（堆分配 → GC）
// Physics.OverlapSphereNonAlloc → 填入已有数组（零分配）
// 高频检测（每帧）一定用 NonAlloc 版本！
```

### 自定义检测区域

```csharp
// 手动构建检测区域——不仅限于球体和盒子

public class CustomQuery : MonoBehaviour
{
    void Update()
    {
        // 用多个 Overlap 组合成复杂形状
        // 扇形检测 = 球体检测 + 角度过滤

        Collider[] hits = new Collider[32];
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, 10f, hits
        );

        for (int i = 0; i < count; i++)
        {
            Vector3 dirToTarget = hits[i].transform.position 
                                - transform.position;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            
            if (angle < 45f)  // 只保留前方 90° 扇形
            {
                // 在扇形内
            }
        }
    }
}
```

---

## 7. Physics.defaultPhysicsScene——自定义物理场景

Unity 2022+ 引入了 **PhysicsScene**，允许在同一个场景中运行多个独立的物理世界。

```csharp
using UnityEngine;

public class PhysicsSceneDemo : MonoBehaviour
{
    private PhysicsScene customScene;
    private Scene simulationScene;

    void Start()
    {
        // 创建独立的物理场景
        simulationScene = SceneManager.CreateScene("PhysicsSimulation");
        customScene = simulationScene.GetPhysicsScene();
        
        // 在这个场景中创建物理对象
        GameObject simObj = new GameObject("SimCube");
        SceneManager.MoveGameObjectToScene(simObj, simulationScene);
        
        Rigidbody rb = simObj.AddComponent<Rigidbody>();
        BoxCollider col = simObj.AddComponent<BoxCollider>();
        simObj.transform.position = new Vector3(0, 10, 0);

        // 注意：这个对象和主场景不在同一个物理世界
        // 所以不会和主场景的物理对象碰撞
    }

    void FixedUpdate()
    {
        if (customScene.IsValid())
        {
            // 手动模拟自定义物理场景
            // 步长和主场景相同
            customScene.Simulate(Time.fixedDeltaTime);
            
            // 优势：你可以在这个场景中做"物理预测"
            // 不干扰主场景的现实物理
        }
    }

    // 应用场景：预测性物理
    // 比如：预判篮球是否进框
    // 主场景：显示真实游戏
    // 辅助场景：模拟"如果现在投篮"的结果
}
```

---

## 8. 物理材质与碰撞交互

```csharp
// PhysicMaterial（3D）——控制物理表面属性

public class SurfacePhysics : MonoBehaviour
{
    void Start()
    {
        Collider col = GetComponent<Collider>();
        
        // 创建物理材质
        PhysicMaterial mat = new PhysicMaterial();
        
        // 摩擦力
        mat.dynamicFriction = 0.6f;    // 动摩擦（滑动时的阻力）
        mat.staticFriction = 0.8f;     // 静摩擦（开始移动的阻力）
        
        // 摩擦力组合模式
        mat.frictionCombine = PhysicMaterialCombine.Average;
        // Average: 两个材质的摩擦取平均
        // Minimum: 取最小值（模拟冰面）
        // Maximum: 取最大值（模拟沙地）
        // Multiply: 相乘（很少用）
        
        // 弹性
        mat.bounciness = 0.3f;
        mat.bounceCombine = PhysicMaterialCombine.Average;
        
        col.material = mat;  // 创建新实例
        // col.sharedMaterial = mat;  // 共享实例（多个 Collider 共用）
    }
}
```

---

## 9. 接触点修改——Contact Modification

```csharp
// 实现 IDynamicContactModifier 接口
// 在接触被物理引擎处理之前修改接触数据

public class CustomContactHandler : MonoBehaviour, 
    IDynamicContactModifier
{
    public void ModifyDynamicContact(ref dynamic contact)
    {
        // 修改接触点：改变法线方向
        // 可以用来实现：
        // - 自定义弹射方向
        // - 传送带效果
        // - 弹跳平台

        // contact.normal = ...;  // 改变反弹方向
        // contact.separation = ...; // 改变分离速度

        // 更常见的做法是用 OnCollisionEnter
    }
}
```

---

## C++/Raylib 对照总结

| 高级概念 | Raylib (C++) | Unity (C#) |
|---------|-------------|-----------|
| 事件驱动输入 | 无（纯轮询） | `InputAction.performed/started/canceled` |
| 组合键 | 手动多个 `IsKeyDown` | `CompositeBinding("2DVector")` |
| 输入路径 | 无 | `<Keyboard>/w`, `<Gamepad>/leftStick` |
| 批量射线 | 手写循环 | `RaycastCommand.ScheduleBatch` |
| 零分配查询 | 手写池 | `.NonAlloc` 系列方法 |
| 独立物理世界 | 无 | `PhysicsScene` + `Simulate()` |
| 接触修改 | 无 | `IDynamicContactModifier` |
| 手动变换同步 | 无 | `Physics2D.SyncTransforms()` |

## 停靠点

> New Input System 是**事件驱动**的——注册 `performed` 回调，不用在 Update 中轮询。
> 组合绑定（2DVector）将四个按键映射为一个 Vector2——比旧版 GetAxis 更灵活。
> `RaycastCommand.ScheduleBatch` + Job System 实现并⾏射线检测——单条 Raycast 的 5-10 倍性能。
> 高频物理查询必须用 `NonAlloc` 版本——避免每帧 GC 分配。
> `PhysicsScene` 允许运行独立的"预演"物理世界——不影响主场景。
> `Physics2D.SyncTransforms` 在批量移动物体后手动触发——避免每帧同步开销。
