# Day 6：生命周期与组件 — 深入篇：自定义 PlayerLoop、属性系统与高级生命周期

## 0. 引言：基础之外

上一章我们学习了 Unity 的生命周期回调（Awake → Start → Update → LateUpdate → OnDestroy）。但这些只是引擎暴露给你的"默认插槽"。Unity 提供了更底层的接口让你操作循环本身。好比 Raylib 中你可以完全控制 while 循环，Unity 也允许你插入自定义系统——只是入口藏得比较深。

本文从底层 PlayerLoop 到高级属性系统，构建完整的生命周期知识图谱。

---

## 1. PlayerLoop 自定义——在引擎循环中插入你的系统

### PlayerLoop 的本质

上一章提到 PlayerLoop 是 C++ 层的帧循环。实际上，Unity 将每个阶段暴露为一个 **PlayerLoopSystem**，你可以读取、修改、甚至替换整个循环。

```csharp
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public static class PlayerLoopDebug
{
    [RuntimeInitializeOnLoadMethod]
    static void PrintPlayerLoop()
    {
        PlayerLoopSystem current = PlayerLoop.GetCurrentPlayerLoop();
        PrintSystem(current, 0);
    }

    static void PrintSystem(PlayerLoopSystem system, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}{system.type?.Name ?? "Root"}");

        if (system.subSystemList != null)
        {
            foreach (var sub in system.subSystemList)
            {
                PrintSystem(sub, depth + 1);
            }
        }
    }
}
```

输出示例（简化）：
```
Root
  Initialization
    PlayerUpdateTime
    AsyncUpload
  EarlyUpdate
    PollInputSystem
    InputSystem_Update
  FixedUpdate
    PhysicsFixedUpdate
    ScriptRunBehaviourFixedUpdate
  Update
    ScriptRunBehaviourUpdate
  LateUpdate
    ScriptRunBehaviourLateUpdate
```

### 自定义插入——在 Update 前执行你的系统

```csharp
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public class CustomLoopInstaller
{
    [RuntimeInitializeOnLoadMethod]
    static void InstallCustomSystem()
    {
        PlayerLoopSystem current = PlayerLoop.GetCurrentPlayerLoop();
        
        // 在 Update 阶段之前插入自定义系统
        InsertSystem<Update>(ref current, new PlayerLoopSystem
        {
            type = typeof(MyPreUpdateSystem),
            updateDelegate = MyPreUpdate
        });

        PlayerLoop.SetPlayerLoop(current);
    }

    static void InsertSystem<T>(ref PlayerLoopSystem root, PlayerLoopSystem system)
    {
        if (root.type == typeof(T))
        {
            // 在现有子系统中追加
            var subs = root.subSystemList;
            Array.Resize(ref subs, subs.Length + 1);
            subs[^1] = system;
            root.subSystemList = subs;
            return;
        }
        if (root.subSystemList != null)
        {
            for (int i = 0; i < root.subSystemList.Length; i++)
            {
                InsertSystem<T>(ref root.subSystemList[i], system);
            }
        }
    }

    static void MyPreUpdate()
    {
        // 这会在所有 MonoBehaviour.Update 之前执行
        // 适合：自定义网络同步、帧率限频器
    }
}
```

**C++/Raylib 对照：**
```
Raylib while 循环：你可以任意排列 Init→Update→Draw 的顺序
Unity PlayerLoop：系统按固定顺序执行，但你可以插入自定义阶段
自定义 PlayerLoopSystem ≈ 在 while 循环中插入一个函数调用
```

### 警告：修改 PlayerLoop 的影响

```
正确场景：
- 需要极低延迟的自定义物理管线
- 网络同步必须在 Update 前
- 第三方 SDK 的帧循环插入

错误场景：
- 代替 MonoBehaviour（不要这样做——会失去编辑器的调试支持）
- 频繁修改 PlayerLoop（只应在启动时修改一次）
- 阻塞式操作（会卡死整个引擎循环）
```

---

## 2. Script Execution Order——深入理解调度策略

### 默认顺序的问题

上一章提到多个脚本的 Awake/Start 顺序不确定。Unity 使用 **Script Execution Order** 解决——但你有没有想过内部是怎么实现的？

```csharp
// Unity 内部（简化）处理流程：
// 1. 将所有 MonoBehaviour 按 Order 排序（默认 Order = 0）
// 2. 同 Order 的脚本按编译顺序（AOT 编译顺序不确定）
// 3. 同一 GameObject 上同 Order 的脚本按添加顺序

// Time：无法控制同 Order 的执行顺序
// 解决方案：用 Awake/Start 的时机差来解耦
public class LateStart : MonoBehaviour
{
    private bool initialized = false;

    void Start()
    {
        // 延时初始化——等所有 Start 执行完毕
        StartCoroutine(DelayedInit());
    }

    IEnumerator DelayedInit()
    {
        yield return null;  // 等一帧
        // 此时所有对象的 Start 都执行完了
        FindObjectOfType<GameManager>().RegisterPlayer(this);
        initialized = true;
    }
}
```

### 执行顺序的冲突解决策略

```
场景：A 需要 B 先初始化，B 需要 A 先初始化——循环依赖

Unity 的做法：
1. 计算所有脚本的 Order 排序
2. 同 Order 按编译程序集顺序（Assembly-CSharp 先于 Assembly-CSharp-firstpass）
3. 循环依赖 = 运行时错误 + 控制台警告

最佳实践：
- 用 Execution Order 解决"明确的先后关系"（如：输入 → 角色 → UI）
- 用 Awake 做内部初始化，Start 做外部依赖
- 用事件解耦，不要依赖执行顺序
```

---

## 3. [RuntimeInitializeOnLoadMethod]——在 Awake 之前执行

这个属性允许你定义一个**静态方法**，在场景加载完成时自动执行——比任何 MonoBehaviour 的 Awake 都要早。

```csharp
using UnityEngine;

public static class GameBootstrapper
{
    // 在场景加载后立刻执行——早于所有 Awake
    [RuntimeInitializeOnLoadMethod]
    static void OnGameLoaded()
    {
        Debug.Log("Game loaded! Setting up global state...");
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        
        // 初始化全局管理器
        // 注意：此时你还不能访问场景中的 GameObject
        // 因为 Awake 还没执行
    }

    // 带加载类型参数
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
        .BeforeSceneLoad)]  // 场景加载前执行
    static void BeforeSceneLoad()
    {
        // 比 OnGameLoaded 更早
        // 适合：SDK 初始化、日志系统、崩溃报告
        Debug.Log("Before any scene loads...");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
        .AfterSceneLoad)]  // 场景加载后执行（默认）
    static void AfterSceneLoad()
    {
        // 等同于无参数版本
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
        .SubsystemRegistration)]  // 最早期
    static void SubsystemRegistration()
    {
        // 在 Subsystem 注册阶段执行
        // 比 BeforeSceneLoad 更早
        // 适合：注册 XR 子系统、自定义渲染管线
    }
}
```

### 执行顺序总结

```
生命周期时间线：

SubsystemRegistration（极早期）
  ↓
BeforeSceneLoad（场景加载前）
  ↓
[场景加载：反序列化场景数据]
  ↓
Awake（所有 MonoBehaviour）
  ↓
Start（所有 MonoBehaviour）
  ↓
AfterSceneLoad（默认无参版本）

这比任何 MonoBehaviour 的回调都早——
相当于 C++ 中在 main() 开始前执行的静态构造函数
```

---

## 4. 自定义 Yield Instruction——协程的底层

### CustomYieldInstruction 详解

Unity 的协程默认支持 `yield return null`、`yield return new WaitForSeconds(1f)` 等。这些内置指令本质上是继承自 `CustomYieldInstruction` 的类。

```csharp
using UnityEngine;
using System.Collections;

// 自定义等待：直到某个条件满足
public class WaitForCondition : CustomYieldInstruction
{
    private System.Func<bool> condition;

    public WaitForCondition(System.Func<bool> condition)
    {
        this.condition = condition;
    }

    // 每帧检查——只有返回 true 时才继续
    public override bool keepWaiting => !condition();
}

// 使用示例
public class CustomYieldDemo : MonoBehaviour
{
    IEnumerator Start()
    {
        Debug.Log("Waiting for space key...");
        
        // 等待空格键按下
        yield return new WaitForCondition(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("Space pressed! Continuing...");

        // 等待 5 帧
        yield return new WaitForFrames(5);
        
        Debug.Log("5 frames passed!");

        // 等待自定义事件
        yield return new WaitForEvent();
    }
}

// 等待指定帧数
public class WaitForFrames : CustomYieldInstruction
{
    private int targetFrame;

    public WaitForFrames(int frames)
    {
        targetFrame = Time.frameCount + frames;
    }

    public override bool keepWaiting 
        => Time.frameCount < targetFrame;
}

// 等待事件触发
public class WaitForEvent : CustomYieldInstruction
{
    private bool eventTriggered = false;

    public void Trigger() => eventTriggered = true;

    public override bool keepWaiting => !eventTriggered;
}
```

### CustomYieldInstruction 的内部原理

```
每帧协程调度器的工作：

1. 遍历所有活跃的协程（Coroutine）
2. 对每个协程：检查当前的 yield return 对象
3. 如果是 null：立即继续（yield return null）
4. 如果是 WaitForSeconds：检查时间是否到了
5. 如果是 CustomYieldInstruction：调用 keepWaiting 属性
   - true → 继续等待
   - false → 恢复执行

自定义 Yield Instruction 比协程本身更高效：
- 没有闭包分配（不会产生 Lambda 闭包）
- 没有额外的 MoveNext 调用
- 纯属性调用，零 GC
```

### WaitUntil 和 WaitWhile——内置的条件等待

```csharp
// Unity 5.3+ 内置了条件等待——不需要自定义

IEnumerator Start()
{
    // 等待条件为 true
    yield return new WaitUntil(() => health > 0);
    
    // 等待条件为 false
    yield return new WaitWhile(() => isInvincible);
    
    // 注意：Lambda 会捕获变量，产生闭包分配
    // 高频场景用 CustomYieldInstruction 更优
}
```

---

## 5. [AlwaysUpdateNonNull] 与 Unity 属性系统

### Unity 的重要属性（Attributes）

Unity 的 attribute 系统控制着编辑器和运行时行为。

```csharp
using UnityEngine;
using UnityEngine.Scripting;

public class AttributeDemo : MonoBehaviour
{
    [Header("=== Core Settings ===")]
    [SerializeField]  // 强制序列化 private 字段
    private int secretValue;

    [Tooltip("This is the player's movement speed")]
    public float speed = 10f;

    [Range(0, 100)]  // 生成滑动条
    public int health = 50;

    [Min(0)]  // 最小值约束
    public float jumpForce = 5f;

    [Space(10)]
    [TextArea(3, 5)]  // 多行文本输入框
    public string description;

    [Multiline]  // 另一种多行
    public string notes;

    // 在 Inspector 中隐藏（但可以被序列化）
    [HideInInspector]
    public int hiddenValue;

    // 非序列化——不保存
    [System.NonSerialized]
    public int tempValue;

    [RequiredMember]  // Unity 2022+：告诉代码裁剪器这个成员是必需的
    [AlwaysUpdateNonNull]  // 非官方，Unity 内部用于确保引用不为 null
    public GameObject criticalRef;

    void Start()
    {
        // [AlwaysUpdateNonNull] 的实际作用：确保字段不被 IL2CPP 裁剪
        // 以及编辑器下持续检查引用不为 null
    }
}
```

### 序列化控制属性

```csharp
// [FormerlySerializedAs] —— 重命名字段不丢失数据
// 当你重命名了一个已序列化的字段时使用

[Serializable]
public class PlayerData
{
    [FormerlySerializedAs("hp")]  // 以前叫 "hp"
    public float health;            // 现在叫 "health"

    [FormerlySerializedAs("maxHp")]
    [FormerlySerializedAs("maxHealth")]  // 可以链式多个旧名
    public float maximumHealth;
    
    // 原理：Unity 序列化时同时写入新旧名字
    // 反序列化时优先读新名，找不到读旧名
}
```

### 防止代码裁剪的属性

```csharp
using Unity.IL2CPP.CompilerServices;

public class PreserveAttributeDemo : MonoBehaviour
{
    // IL2CPP 代码裁剪器会删除"未使用"的代码
    // 这些属性可以防止关键方法被删除

    [Preserve]  // 保留这个方法不移除
    void CriticalMethod() { }

    // 另一种方式
    [UsedImplicitly]  // JetBrains 注解（需要 using JetBrains.Annotations）
    void UsedByReflection() { }
}
```

---

## 6. DontDestroyOnLoad——高级模式与陷阱

上一章介绍了基础的 DontDestroyOnLoad 单例模式。这里深入讨论它的边界情况和正确使用方式。

### 常见陷阱

```csharp
public class DDOLManager : MonoBehaviour
{
    private static DDOLManager instance;

    void Awake()
    {
        // 陷阱 1：DontDestroyOnLoad 在场景中必须有父对象
        if (transform.parent != null)
        {
            // 如果有个父对象，父对象被销毁时你也会被销毁
            Debug.LogError("DontDestroyOnLoad should be root!");
            transform.SetParent(null);  // 先解除父子关系
        }
        
        DontDestroyOnLoad(gameObject);

        // 陷阱 2：多次场景加载会创建多个实例
        if (instance != null && instance != this)
        {
            Destroy(gameObject);  // 删除重复的
            return;
        }
        instance = this;

        // 陷阱 3：场景中的引用会丢失
        // 如果引用了场景中的对象，场景卸载后引用变成 null
        // 不要缓存场景对象的引用！用事件或全局 ID
    }

    // 陷阱 4：DontDestroyOnLoad 的对象不会收到 OnSceneLoaded
    // 需要手动订阅 SceneManager 事件
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene {scene.name} loaded!");
        // 重新建立场景引用
    }

    void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"Scene {scene.name} unloaded!");
    }
}
```

### DontDestroyOnLoad 的正确架构

```
❌ 错误架构：
DDOL_Manager (root)
  └── SceneObject (引用场景中的对象)
       └── 场景卸载后 → 引用悬空

✅ 正确架构：
DDOL_Manager (root——唯一全局)
  ├── 不引用任何场景对象
  ├── 用 ScriptableObject 存全局数据
  └── 每次场景加载后重建场景引用

更好的方案：用 ScriptableObject 代替 DontDestroyOnLoad
┌────────────────────────┐
│ GameSettingsSO         │ ← 资源文件，不是场景对象
│ playerScore: int       │    不会被场景加载影响
│ gameState: enum        │    整个项目生命周期都存在
└────────────────────────┘
```

---

## 7. 场景生命周期——SceneManager 回调

```csharp
using UnityEngine.SceneManagement;

public class SceneLifecycle : MonoBehaviour
{
    void OnEnable()
    {
        // 场景加载完成
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            Debug.Log($"Loaded: {scene.name}, mode: {mode}");
            
            if (mode == LoadSceneMode.Additive)
            {
                // 附加加载——不卸载当前场景
                // 适合：加载 UI、加载子系统
            }
            else
            {
                // 单场景加载——卸载前一个
                // 此时旧场景的对象已被销毁
            }
        };

        // 场景卸载时
        SceneManager.sceneUnloaded += (scene) =>
        {
            Debug.Log($"Unloaded: {scene.name}");
            // 清理静态引用、重置状态
        };

        // 场景加载进度（异步加载时）
        SceneManager.sceneLoadProgress += (progress) =>
        {
            // 显示加载进度条
        };

        // Active 场景变化
        SceneManager.activeSceneChanged += (prev, next) =>
        {
            Debug.Log($"Active changed: {prev.name} → {next.name}");
        };
    }
}
```

---

## 8. FindObjectOfType vs FindObjectsByType——Unity 2023+

Unity 2023.1 引入了新的查找 API，替代了老的 FindObjectOfType。

```csharp
using UnityEngine;
using System.Collections.Generic;

public class FindAdvanced : MonoBehaviour
{
    void Start()
    {
        // ─── 旧的 API（仍然可用，但效率低） ───

        // O(n) 查找，n = 所有 GameObject
        Camera cam = FindObjectOfType<Camera>();
        
        // 返回数组（堆分配）
        Camera[] cams = FindObjectsOfType<Camera>();

        // ─── 新的 API（Unity 2023+） ───

        // FindObjectsByType——更高效，支持排序
        // 1. 不排序（最快）
        List<Camera> cameras = new List<Camera>();
        FindObjectsByType<Camera>(FindObjectsSortMode.None, cameras);
        
        // 2. 按层级顺序排序
        Camera[] sorted = FindObjectsByType<Camera>(
            FindObjectsSortMode.InstanceID);
        // 按 InstanceID 排序 = 创建顺序

        // 3. 按 Name 排序
        Camera[] byName = FindObjectsByType<Camera>(
            FindObjectsSortMode.Name);

        // ─── 性能对比 ───

        // FindObjectsOfType (old)：
        // - 内部调用 GetAllScenes + GetAllObjects
        // - 返回数组（每次都 new）
        // - 无法传入 List 复用

        // FindObjectsByType (new)：
        // - 支持传入 List<T> 复用（零 GC）
        // - 可选排序模式
        // - 内部使用原生内存查询
    }

    // 最佳实践：缓存查找结果
    // 不要每帧调用 FindObjectsByType
    void Update()
    {
        // ❌ 坏：每帧查找
        var players = FindObjectsByType<Player>(
            FindObjectsSortMode.None);
        foreach (var p in players) { /* ... */ }

        // ✅ 好：用单例或管理器注册
        // 在 Awake 中注册，在 OnDestroy 中注销
    }
}

// 更好的方案：注册表模式
public class PlayerRegistry : MonoBehaviour
{
    private static List<Player> players = new List<Player>();

    public static IReadOnlyList<Player> Players => players;

    public static void Register(Player p)
    {
        if (!players.Contains(p))
            players.Add(p);
    }

    public static void Unregister(Player p)
    {
        players.Remove(p);
    }
}
```

---

## 9. 应用程序生命周期回调

```csharp
public class AppLifecycle : MonoBehaviour
{
    // 当应用获得/失去焦点（Alt+Tab、手机切后台）
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Debug.Log("App gained focus");
            Time.timeScale = 1;  // 恢复游戏
        }
        else
        {
            Debug.Log("App lost focus");
            Time.timeScale = 0;  // 暂停游戏
        }
    }

    // 当应用暂停/恢复（手机来电、按 Home 键）
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("App paused");
            SaveGame();  // 保存进度
        }
        else
        {
            Debug.Log("App resumed");
        }
    }

    // 应用退出时
    void OnApplicationQuit()
    {
        Debug.Log("App quitting");
        SaveGame();
        // 注意：在 OnApplicationQuit 中不要创建新的对象
        // 此时引擎已经开始销毁流程
    }

    void SaveGame()
    {
        // 保存游戏数据
        PlayerPrefs.Save();
    }
}
```

### 生命周期完整时序

```
应用启动：
[RuntimeInitializeOnLoadMethod.SubsystemRegistration]
  → BeforeSceneLoad
  → 场景加载
  → Awake → OnEnable → Start
  → AfterSceneLoad
  
运行中：
  Update/LateUpdate/FixedUpdate 循环
  → OnApplicationFocus(true)
  → OnApplicationPause(true) → ... → OnApplicationPause(false)
  → OnApplicationFocus(false)
  
应用退出：
  → OnApplicationQuit
  → OnDisable → OnDestroy
  → 所有 DontDestroyOnLoad 的 OnDestroy
```

---

## C++/Raylib 对照总结

| 高级概念 | Raylib (C++) | Unity (C#) |
|---------|-------------|-----------|
| 自定义循环阶段 | 手写 while 布局 | `PlayerLoopSystem` 自定义插入 |
| 预初始化 | 无（main 之前） | `[RuntimeInitializeOnLoadMethod]` |
| 条件等待 | 手写状态机 | `CustomYieldInstruction` |
| 序列化字段重命名 | 无版本管理 | `[FormerlySerializedAs]` |
| 代码裁剪保护 | 编译器特性 | `[Preserve]` / `[UsedImplicitly]` |
| 全局查找 | 手动数组遍历 | `FindObjectsByType`（排序 + 低 GC） |
| 应用生命周期 | `WindowShouldClose` | `OnApplicationFocus/Pause/Quit` |

## 停靠点

> `[RuntimeInitializeOnLoadMethod]` 是最早的执行点——比任何 Awake 都早，适合 SDK 初始化。
> `CustomYieldInstruction` 通过重写 `keepWaiting` 属性实现零 GC 的自定义等待条件。
> DontDestroyOnLoad 的三大陷阱：必须有根层级、多次场景加载创建重复实例、场景引用悬空。
> Unity 2023+ 的 `FindObjectsByType` 支持排序和 List 复用，替代旧的 `FindObjectOfType`。
> 自定义 PlayerLoop 能插入你自己的阶段到引擎循环——但只在必要的时候用。
