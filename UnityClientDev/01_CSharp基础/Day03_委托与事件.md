# Day 3：委托与事件 — 从函数指针到事件驱动架构

## 0. 为什么需要委托？

看这个场景：

```csharp
// 一个伤害系统：造成伤害后可能需要做多件事
void DealDamage(Player target, int dmg)
{
    target.HP -= dmg;

    // 目前硬编码了三件事...
    UpdateHealthUI(dmg);
    PlayHitSound();
    CheckAchievement(dmg);

    // 但如果以后要加：闪屏效果？震屏？弹出伤害数字？
    // 每加一个功能都要改 DealDamage 函数！
}
```

**委托（Delegate）** 解决了这个问题——你传一个"函数"给另一个函数，让它来决定做什么。相比于 C++ 的函数指针，C# 委托是类型安全的、面向对象的、支持多播的。

---

## 1. 委托（Delegate）的定义

### 什么是委托？

委托是**类型安全的函数指针**——一个变量可以"指向"一个或多个方法，然后通过委托变量调用这些方法。

```
内存中的委托对象：
┌──────────────────────────────┐
│    delegate void Callback(int) │
├──────────────────────────────┤
│  目标对象 (_target)           │ ← 调用方法的对象实例
│  方法指针 (_methodPtr)        │ ← 指向方法的函数地址
│  委托链 (_invocationList)    │ ← 多播委托的链表
└──────────────────────────────┘
```

### 声明与使用

```csharp
// 1. 声明委托类型（定义签名）
delegate void DamageCallback(int amount);

public class Player
{
    // 2. 声明委托字段
    public DamageCallback OnDamage;

    public void TakeDamage(int dmg)
    {
        // 3. 调用委托（如果非空）
        if (OnDamage != null)
            OnDamage(dmg);
        // 或者：OnDamage?.Invoke(dmg);  // C# 6+ 空条件运算符
    }
}

public class UIManager
{
    void Start()
    {
        Player player = GetComponent<Player>();

        // 4. 绑定方法到委托
        player.OnDamage = UpdateHealthBar;  // 注意：没有括号！

        // 5. 调用 TakeDamage 时会触发 UpdateHealthBar
    }

    void UpdateHealthBar(int dmg)
    {
        Debug.Log($"HP changed by {dmg}");
    }
}
```

### 对比 C++

```cpp
// C++ 函数指针
void (*callback)(int);
callback = &UpdateHealthBar;
callback(10);  // 调用

// C++ std::function（更接近 C# delegate）
std::function<void(int)> callback;
callback = &UpdateHealthBar;
callback(10);
```

**差异：**

| | C++ 函数指针 | C# delegate |
|--|------------|------------|
| 类型安全 | 签名匹配即可，无运行时检查 | 严格类型匹配 |
| 目标对象 | 需要额外参数传递 this | 自动捕获目标对象 |
| 多播 | 手动实现链表 | 内置支持 |
| 空值安全 | 需要手动检查 | `?.Invoke()` 语法 |
| 协变/逆变 | 不支持 | 支持 |

---

## 2. 多播委托——一个委托调用多个方法

```csharp
public class Player
{
    public delegate void DamageDelegate(int dmg);
    public DamageDelegate OnDamage;

    public void TakeDamage(int dmg)
    {
        OnDamage?.Invoke(dmg);  // 调用所有绑定的方法
    }
}

// 使用：
Player player = new Player();

// += 添加（不是赋值！）
player.OnDamage += UpdateHealthBar;  // 第一个
player.OnDamage += PlayHitSound;     // 第二个
player.OnDamage += CheckAchievement; // 第三个

player.TakeDamage(10);
// 输出：
// "HP changed by 10"
// "Playing hit sound"
// "Achievement check: 10"

// -= 移除
player.OnDamage -= PlayHitSound;  // 现在只剩 UpdateHealthBar 和 CheckAchievement
```

### 多播委托的底层实现

```
player.OnDamage += UpdateHealthBar;
player.OnDamage += PlayHitSound;
player.OnDamage += CheckAchievement;

内存中的委托链：
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│ Delegate          │    │ Delegate          │    │ Delegate          │
│ _target = player  │───→│ _target = player  │───→│ _target = player  │
│ _methodPtr =      │    │ _methodPtr =      │    │ _methodPtr =      │
│   UpdateHealthBar │    │   PlayHitSound    │    │   CheckAchievement│
│ Next = ──────────→│    │ Next = ──────────→│    │ Next = null       │
└──────────────────┘    └──────────────────┘    └──────────────────┘
```

`OnDamage?.Invoke(10)` 遍历整个链表，依次调用每个方法。

**注意：+= 可以重复添加同一个方法！**
```csharp
player.OnDamage += UpdateHealthBar;
player.OnDamage += UpdateHealthBar;  // 绑定了两次！
// UpdateHealthBar 会被调用两次！
```

---

## 3. 内置委托类型（Action / Func）

C# 提供了通用的委托类型，不需要每次都自己声明：

```csharp
// Action — 无返回值
Action                // void ()
Action<int>           // void (int)
Action<int, string>   // void (int, string)
Action<int, float, bool>  // ...最多 16 个参数

// Func — 有返回值，最后一个类型总是返回值
Func<int>             // int ()
Func<int, bool>       // bool (int) — 输入 int，返回 bool
Func<int, int, float> // float (int, int) — 输入两个 int，返回 float

// Predicate — 返回 bool 的特殊委托
Predicate<int>        // bool (int)
// 等价于 Func<int, bool>
```

### 什么时候用 Action/Func，什么时候自定义 delegate？

```csharp
// ✅ 简单的回调用 Action/Func
public void DoSomething(Action onComplete)
{
    // ...
    onComplete?.Invoke();
}

// ✅ 简单的判断用 Predicate
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };
numbers.FindAll(x => x > 3);  // Predicate<int>

// ❌ 当方法名需要表达意义时，用自定义 delegate
public delegate void PlayerDeathHandler(Player player, DeathCause cause);
// 比 Action<Player, DeathCause> 更清晰
```

---

## 4. 事件（Event）——安全版的委托

### 事件 vs 委托：核心区别

```csharp
public class Player
{
    // 委托字段——外部可以随意操作
    public Action<int> OnDamagePublic;

    // 事件——外部只能 += / -=
    public event Action<int> OnDamageEvent;

    public void TakeDamage(int dmg)
    {
        OnDamagePublic?.Invoke(dmg);   // 可以
        OnDamageEvent?.Invoke(dmg);    // 可以（只能在类内部 Invoke）
    }
}

// 使用：
Player player = new Player();

// 委托——可以做任何事
player.OnDamagePublic += UpdateHealthBar;  // OK
player.OnDamagePublic = SomeOtherMethod;   // OK（直接覆盖！危险！）
player.OnDamagePublic.Invoke(10);          // OK（从外部触发！危险！）
player.OnDamagePublic = null;              // OK（清空所有！危险！）

// 事件——只能 += 和 -=
player.OnDamageEvent += UpdateHealthBar;   // OK
player.OnDamageEvent = SomeOtherMethod;    // ❌ 编译错误！
player.OnDamageEvent.Invoke(10);           // ❌ 编译错误！
player.OnDamageEvent = null;               // ❌ 编译错误！
```

### 事件编译器生成的代码

```csharp
public event Action<int> OnDamageEvent;
```

编译器展开为：
```csharp
private Action<int> _onDamageEvent;  // 实际的私有委托字段

// add 访问器（事件 += 调用）
public void add_OnDamageEvent(Action<int> value)
{
    // 线程安全地合并委托
    Action<int> current = _onDamageEvent;
    Action<int> newDelegate;
    do
    {
        newDelegate = (Action<int>)Delegate.Combine(current, value);
    }
    while (Interlocked.CompareExchange(ref _onDamageEvent, newDelegate, current) != current);
}

// remove 访问器（事件 -= 调用）
public void remove_OnDamageEvent(Action<int> value)
{
    // 线程安全地移除委托
    Action<int> current = _onDamageEvent;
    Action<int> newDelegate;
    do
    {
        newDelegate = (Action<int>)Delegate.Remove(current, value);
    }
    while (Interlocked.CompareExchange(ref _onDamageEvent, newDelegate, current) != current);
}
```

**所以：** 外部代码的 `+=` 实际上调用 `add_OnDamageEvent`，`-=` 调用 `remove_OnDamageEvent`。直接赋值和调用被编译器阻止。

### 事件的内存泄漏问题

```csharp
public class UIManager : MonoBehaviour
{
    public Player player;

    void Start()
    {
        // 订阅事件
        player.OnDamageEvent += UpdateHealthBar;
    }

    void UpdateHealthBar(int dmg) { }

    // ❌ 问题：如果 UIManager 被销毁，但没有取消订阅
    // player 仍然持有对 UpdateHealthBar 的引用
    // UIManager 无法被 GC 回收！= 内存泄漏！

    void OnDestroy()
    {
        // ✅ 必须取消订阅
        player.OnDamageEvent -= UpdateHealthBar;
    }
}
```

**为什么？** 事件源（Player）持有了事件订阅者（UIManager）的引用。只要 Player 还活着，UIManager 就不会被 GC 回收。

---

## 5. UnityEvent——Unity 的序列化委托

```csharp
using UnityEngine;
using UnityEngine.Events;

public class DamageTrigger : MonoBehaviour
{
    // UnityEvent 可以在 Inspector 中可视化绑定
    public UnityEvent<int> OnPlayerEnter;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 触发所有在 Inspector 和代码中绑定的回调
            OnPlayerEnter?.Invoke(10);
        }
    }
}
```

### UnityEvent vs C# event

| | C# event | UnityEvent |
|--|---------|------------|
| 序列化 | ❌ 不能序列化 | ✅ 可以序列化 |
| Inspector 绑定 | ❌ 不行 | ✅ 拖拽绑定 |
| 动态绑定 | ✅ += 语法 | ✅ AddListener() |
| 性能 | 高（直接 IL 调用） | 较低（反射调用） |
| 用途 | 代码内部 | 暴露给 Inspector |

```csharp
// 代码绑定 UnityEvent：
public class GameManager : MonoBehaviour
{
    public DamageTrigger trigger;

    void Start()
    {
        // 方式 1：Inspector 拖拽（推荐——非程序员也可以用）
        // 方式 2：代码绑定
        trigger.OnPlayerEnter.AddListener(OnPlayerHit);
    }

    void OnPlayerHit(int dmg)
    {
        Debug.Log($"Player hit for {dmg}");
    }

    void OnDestroy()
    {
        trigger.OnPlayerEnter.RemoveListener(OnPlayerHit);
    }
}
```

---

## 6. Lambda 表达式——匿名函数

### 语法

```csharp
// 完整形式：(参数列表) => { 语句块 }
player.OnDamage += (int dmg) => {
    Debug.Log($"Took {dmg} damage");
};

// 省略参数类型（编译器推断）
player.OnDamage += (dmg) => {
    Debug.Log($"Took {dmg} damage");
};

// 单参数可以省略括号
player.OnDamage += dmg => Debug.Log($"Took {dmg} damage");

// 多参数
player.OnDamage += (dmg, source) => Debug.Log($"{source} dealt {dmg}");

// 无参数
Action sayHello = () => Debug.Log("Hello!");
```

### 对比 C++

```csharp
// C# Lambda: (参数) => 表达式
Action<int> callback = (x) => Debug.Log(x);

// C++ Lambda: [捕获](参数){语句}
// auto callback = [](int x) { std::cout << x; };
```

**差异：** C# Lambda 不需要写捕获列表 `[]`，编译器自动处理闭包。

### Lambda 的闭包机制——编译器生成了一个类

```csharp
int bonus = 10;  // 外部变量
player.OnDamage += dmg => {
    int total = dmg + bonus;  // 捕获了 bonus！
    Debug.Log($"Total damage: {total}");
};
```

编译器生成：
```csharp
// 编译器生成的辅助类
[CompilerGenerated]
private sealed class <>c__DisplayClass0_0
{
    public int bonus;  // 捕获的变量变成了字段

    public void <Main>b__0(int dmg)
    {
        int total = dmg + this.bonus;
        Debug.Log($"Total damage: {total}");
    }
}

// 使用：
<>c__DisplayClass0_0 closure = new <>c__DisplayClass0_0();
closure.bonus = 10;  // 捕获变量
player.OnDamage += closure.<Main>b__0;
```

**陷阱：** 因为闭包捕获的是变量引用（不是值），所以：

```csharp
// ❌ 经典陷阱
for (int i = 0; i < 5; i++)
{
    button.onClick.AddListener(() => Debug.Log(i));
    // 所有按钮点击都输出 5！（循环结束时 i = 5）
}

// ✅ 修正：复制局部变量
for (int i = 0; i < 5; i++)
{
    int copy = i;  // 每次循环创建一个新变量
    button.onClick.AddListener(() => Debug.Log(copy));
}
```

---

## 7. Unity 中的事件驱动架构

### 组件通信的多种方式

```csharp
// 方式 1：GetComponent——直接调用（耦合高）
public class Health : MonoBehaviour
{
    void OnCollisionEnter(Collision col)
    {
        col.gameObject.GetComponent<Player>().TakeDamage(10);
    }
}

// 方式 2：事件——解耦
public class Health : MonoBehaviour
{
    public event Action<int> OnDamageTaken;

    void OnCollisionEnter(Collision col)
    {
        int dmg = CalculateDamage(col);
        OnDamageTaken?.Invoke(dmg);
    }
}

// 方式 3：UnityEvent——可视化解耦
public class Health : MonoBehaviour
{
    public UnityEvent<int> OnDamageTaken;

    void OnCollisionEnter(Collision col)
    {
        int dmg = CalculateDamage(col);
        OnDamageTaken?.Invoke(dmg);
    }
}
```

### 事件总线的概念

```csharp
// 一个全局事件系统——任何脚本都可以订阅/触发
public static class EventBus
{
    private static Dictionary<Type, Delegate> events = new Dictionary<Type, Delegate>();

    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        Type type = typeof(T);
        if (events.ContainsKey(type))
            events[type] = Delegate.Combine(events[type], handler);
        else
            events[type] = handler;
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        Type type = typeof(T);
        if (events.ContainsKey(type))
            events[type] = Delegate.Remove(events[type], handler);
    }

    public static void Publish<T>(T eventData) where T : struct
    {
        Type type = typeof(T);
        if (events.ContainsKey(type))
            (events[type] as Action<T>)?.Invoke(eventData);
    }
}

// 定义事件数据类型
public struct PlayerDeathEvent
{
    public Player player;
    public DeathCause cause;
}

// 使用：
// 订阅者
EventBus.Subscribe<PlayerDeathEvent>(OnPlayerDied);

void OnPlayerDied(PlayerDeathEvent e)
{
    Debug.Log($"{e.player.name} died by {e.cause}");
}

// 发布者
EventBus.Publish(new PlayerDeathEvent { player = this, cause = DeathCause.Fall });
```

---

## 练习：完整的事件系统

```csharp
using UnityEngine;
using System;

public class Day03_DelegateEvent : MonoBehaviour
{
    void Start()
    {
        // ─── 练习 1：Action 委托 ───
        Action sayHello = () => Debug.Log("Hello!");
        sayHello();  // 输出 "Hello!"

        // ─── 练习 2：Func 委托 ───
        Func<int, int, int> add = (a, b) => a + b;
        int result = add(3, 5);  // result = 8
        Debug.Log($"3 + 5 = {result}");

        // ─── 练习 3：多播委托 ───
        Action multi = () => Debug.Log("A");
        multi += () => Debug.Log("B");
        multi += () => Debug.Log("C");
        multi?.Invoke();  // 依次输出 A, B, C

        // ─── 练习 4：事件系统 ───
        Player player = new Player();
        player.OnDeath += () => Debug.Log("Player died!");
        player.OnDeath += () => Debug.Log("Game Over!");

        player.TakeDamage(999);
        // 输出：
        // "Player died!"
        // "Game Over!"
    }
}

public class Player
{
    public event Action OnDeath;
    private int hp = 100;

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        Debug.Log($"HP: {hp}");

        if (hp <= 0)
        {
            OnDeath?.Invoke();  // 广播死亡事件
        }
    }
}
```

---

---

## C++/Raylib 对照总结

| 概念 | C++ | C# |
|------|-----|-----|
| 函数指针 | `void (*cb)(int)` | `delegate void Callback(int)` |
| 类型安全回调 | `std::function<void(int)>` | `Action<int>` / `Func<int,bool>` |
| 多播 | 手动 `vector<function>` | `+=` 内置多播 |
| 事件 | 无内置支持 | `event` 关键字 |
| Lambda | `[capture](params){body}` | `(params) => { body }` |
| 闭包捕获 | 按值 `[=]` 或按引用 `[&]` | 自动按引用捕获 |
| 函数对象 | `struct Functor { void operator()(); };` | `delegate` / lambda |

## 停靠点

> `delegate` = 类型安全的函数指针，支持多播（+= 链式调用）。
> `event` = 限制外部只能 +=/-= 的委托，不能从外部 Invoke。
> `UnityEvent` = 可序列化的 event，可以在 Inspector 中拖拽绑定。
> Lambda 表达式 `=>` 编译时会生成闭包类来捕获外部变量——注意 for 循环陷阱。
> **事件不取消订阅 = 内存泄漏！** 永远在 OnDestroy 中 -= 或 RemoveListener。

