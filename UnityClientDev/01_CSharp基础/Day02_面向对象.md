# Day 2：C# 面向对象 — 从 CLR 对象模型到高级特性

## 0. 为什么需要面向对象？

面向对象编程（OOP）是一种**数据组织方式**——将数据和操作数据的方法打包在一起。CLR 的整个类型系统就是建立在 OOP 之上的。

C# 的 OOP 和 C++ 本质上做同一件事，但 C# 在语言层面做了大量简化：没有多继承的复杂性、不需要手动管理内存、属性和事件是语言一等公民。

---

## 1. CLR 对象模型：类在内存中是什么样子？

```csharp
public class Player
{
    public int hp;
    public string name;
    public void TakeDamage(int dmg) { hp -= dmg; }
}
```

当 `new Player()` 执行时，CLR 在托管堆上分配的内存结构：

```
托管堆上的 Player 对象：
偏移 0: ┌─────────────────────────────┐
         │ SyncBlock (4/8 字节)        │ ← 用于 lock 关键字、哈希码等
偏移 4/8: ├─────────────────────────────┤
         │ TypeHandle (4/8 字节)       │ ← 指向 Player 类型的方法表
偏移 8/16: ├─────────────────────────────┤
         │ int hp (4 字节)             │ ← 实例字段
         │ string name (引用, 4/8 字节) │
         └─────────────────────────────┘

Player 类型的方法表（Type Method Table）：
┌─────────────────────────────────────┐
│ 基类信息 (Object)                    │
│ 实现的接口列表                        │
│ 虚方法表 (Virtual Method Table):     │
│   [0] Object.ToString()             │ ← 虚方法
│   [1] Object.Equals()               │
│   [2] Object.GetHashCode()          │
│   [3] Player.TakeDamage(int)        │ ← 自定义虚方法
│   [4] Player.GetHP()                │
│ 静态字段                             │
│ 静态方法                             │
└─────────────────────────────────────┘
```

**关键区别：** C++ 的类在内存中只有字段（vtable 指针 + 成员变量）。C# 的每个对象还有 SyncBlock 和 TypeHandle——这就是 C# 能实现 `lock`、`GetType()`、反射等功能的底层原因。

---

## 2. 类定义——C++ vs C#

### 默认访问级别

```cpp
// C++
class Player {       // 默认 private
    int hp;          // private
public:
    int GetHP() { return hp; }
};
```

```csharp
// C#
public class Player {  // 默认 public（class 本身）
    private int hp;    // 需要显式写 private
    public int GetHP() { return hp; }
}
```

**C# class 默认 public 的哲学原因：** C# 设计目标是"快速开发"，大多数时候你要公开类。而 C++ 设计目标是"零开销抽象"，默认 private 更安全。这是两种语言优先级不同的体现。

### 构造函数

```csharp
public class Player
{
    private int hp;
    private string name;

    // 无参构造函数
    public Player()
    {
        hp = 100;
        name = "Unknown";
    }

    // 带参构造函数
    public Player(string name, int hp)
    {
        this.name = name;  // this 区分参数和字段
        this.hp = hp;
    }

    // 构造函数链：调用另一个构造函数
    public Player(string name) : this(name, 100)  // 调用上面的 2 参构造函数
    {
    }
}
```

### 对象初始化器——C# 3.0 语法糖

```csharp
// 传统方式
Player p1 = new Player();
p1.hp = 100;
p1.name = "Alice";

// 对象初始化器（编译器自动生成上面的代码）
Player p2 = new Player { hp = 100, name = "Alice" };

// 集合初始化器
List<int> nums = new List<int> { 1, 2, 3, 4, 5 };
// 编译器展开为：nums.Add(1); nums.Add(2); ...
```

### 析构函数——和 C++ 完全不同

```csharp
public class Player
{
    ~Player()  // 析构函数（Finalize 方法的语法糖）
    {
        // 这里在 GC 回收时才会调用，时间不确定！
        Debug.Log("Player finalized");
    }
}
```

**C# 没有确定性析构！** 你无法控制对象何时销毁。GC 决定何时回收。

析构函数的 IL 层面——编译器生成：
```csharp
// 编译器将 ~Player() 转换为：
protected override void Finalize()
{
    try { /* 你的清理代码 */ }
    finally { base.Finalize(); }
}
```

**C# 的资源清理模式：IDisposable**
```csharp
public class Player : IDisposable
{
    private FileStream file;  // 非托管资源

    public void Dispose()
    {
        // 手动释放非托管资源
        file?.Close();
        // 告诉 GC 不要调用 Finalize
        GC.SuppressFinalize(this);
    }
}

// 使用方式：
using (var player = new Player())
{
    // 用完后自动调用 Dispose()
}
// using 块编译为 try/finally，finally 中调用 Dispose()
```

### 对比 C++

| 概念 | C++ | C# |
|------|-----|-----|
| 构造函数 | `Player::Player()` | `public Player()` |
| 初始化 | 初始化列表 `: hp(100)` | `this.hp = 100` 或属性初始化器 |
| 析构 | `~Player()` 确定时机 | `~Player()` 由 GC 决定时间 |
| 清理模式 | RAII（资源获取即初始化） | `IDisposable` + `using` |
| new | 返回指针 `Player* p = new Player()` | 直接返回引用 `Player p = new Player()` |
| delete | `delete p;` | GC 自动回收 |

---

## 3. Property（属性）——C# 独有的语法糖

### 为什么需要 Property？

C++ 中你需要手动写 getter/setter：

```cpp
// C++
class Player {
    int hp;
public:
    int GetHP() const { return hp; }
    void SetHP(int v) { hp = v; }
};
```

C# 的 Property 让编译器自动生成这些方法：

```csharp
public class Player
{
    // 自动属性——编译器生成私有字段 + get/set 方法
    public string Name { get; set; }
}
```

### Property 的 IL 层面

```csharp
public string Name { get; set; }
```

编译器生成的代码（反编译后）：
```csharp
public class Player
{
    // 编译器生成的私有字段
    [CompilerGenerated]
    private string <Name>k__BackingField;

    // 编译器生成的 get 方法
    [CompilerGenerated]
    public string get_Name()
    {
        return this.<Name>k__BackingField;
    }

    // 编译器生成的 set 方法
    [CompilerGenerated]
    public void set_Name(string value)
    {
        this.<Name>k__BackingField = value;
    }
}
```

所以 `player.Name = "Alice"` 编译为 `player.set_Name("Alice")`。

### 完整属性——加逻辑

```csharp
public class Player
{
    private int _hp;

    public int HP
    {
        get
        {
            return _hp;
        }
        set
        {
            _hp = Mathf.Clamp(value, 0, 100);  // 加逻辑！
        }
    }
}
```

### 只读属性

```csharp
public class Player
{
    public int HP { get; private set; }  // 外部只读，内部可写

    public bool IsAlive => _hp > 0;      // 表达式体属性（只读）
    // 等价于：public bool IsAlive { get { return _hp > 0; } }

    public string FullName => $"{FirstName} {LastName}";

    // C# 6+：只读自动属性可以在构造函数中赋值
    public int Id { get; }  // 只能构造函数中设置

    public Player(int id)
    {
        Id = id;  // OK
    }
}
// 之后 Player.Id 只能读不能写
```

### Property vs 字段的选择

```csharp
// 什么情况下用字段（field）？
// - 真正的常量：public const int MaxHP = 100;
// - 只在本类内部使用

// 什么情况下用属性（property）？
// - 公开给外部访问的几乎所有情况
// - 需要加验证逻辑时
// - 需要做数据绑定时（UI 绑定只能绑定 Property）
```

**经验法则：** 所有公开数据都用 Property。Property 是接口的一部分，将来可以加逻辑而不破坏二进制兼容性。字段是内部实现细节。

---

## 4. 继承——virtual / override / abstract

### virtual——虚方法

```csharp
public class Character
{
    public virtual void TakeDamage(int dmg)
    {
        Debug.Log($"Character took {dmg} damage");
    }
}
```

### override——覆写

```csharp
public class Player : Character
{
    public override void TakeDamage(int dmg)
    {
        // base 调用父类实现
        base.TakeDamage(dmg);  // 调用 Character.TakeDamage
        Debug.Log("Player specific damage logic");
    }
}
```

### new——隐藏（和 override 完全不同）

```csharp
public class Player : Character
{
    // 用 new 隐藏父类方法——不是覆写！
    public new void TakeDamage(int dmg)
    {
        Debug.Log("This hides, not overrides");
    }
}

// 区别：
Character c = new Player();
c.TakeDamage(10);  // 调用 Character.TakeDamage（不是 Player 的！）

Player p = (Player)c;
p.TakeDamage(10);  // 调用 Player.TakeDamage
```
- `override`：运行时调度，不论变量类型是什么，都调用实际类型的版本
- `new`：编译时调度，根据变量类型决定调用哪个版本

### abstract——抽象类

```csharp
public abstract class Character
{
    public string Name { get; set; }

    // 抽象方法——没有实现，子类必须覆写
    public abstract void TakeDamage(int dmg);

    // 普通方法——有实现，子类可以选择覆写或继承
    public virtual void Heal(int amount)
    {
        Debug.Log($"{Name} healed {amount}");
    }
}

public class Player : Character
{
    // 必须实现抽象方法
    public override void TakeDamage(int dmg)
    {
        Debug.Log($"{Name} took {dmg} damage");
    }
}

// 不能实例化抽象类：
// Character c = new Character();  // ❌ 编译错误！
```

### sealed——阻止继承

```csharp
public sealed class FinalClass : Character
{
    // 不能再有子类
}
// public class SubClass : FinalClass { }  // ❌ 编译错误！

public class Player : Character
{
    public sealed override void TakeDamage(int dmg)
    {
        // 子类覆写后 sealed，阻止再往下继承
    }
}
```

### virtual/override 的 CLR 调度机制

```
Character c = new Player();
c.TakeDamage(10);  // 运行时决定调用 Player.TakeDamage

调度流程：
1. CLR 通过 c 的 TypeHandle 获取 Player 方法表
2. 在方法表中查找 TakeDamage 的虚方法槽
3. 调用 Player.TakeDamage（不是 Character.TakeDamage）
```

与 C++ vtable 的对比：
| | C++ vtable | CLR vtable |
|--|-----------|-----------|
| 存储位置 | 对象头部的 vptr 指针 | TypeHandle 中的方法表 |
| 类型信息 | RTTI（需开启） | 每个对象都有完整类型信息 |
| 接口调度 | 需要多继承链搜索 | 方法表中有接口映射表 |
| 性能 | 最小开销 | 略高（需要更多安全检查） |

---

## 5. interface（接口）

### 什么是接口？

接口定义了一组**契约**——实现接口的类必须提供这些方法。

```csharp
public interface IDamageable
{
    void TakeDamage(int dmg);
}

public interface IHealable
{
    void Heal(int amount);
}

// 一个类可以实现多个接口！
public class Player : MonoBehaviour, IDamageable, IHealable
{
    public void TakeDamage(int dmg) { hp -= dmg; }
    public void Heal(int amount) { hp += amount; }
}
```

### 显式接口实现

```csharp
public interface IAttack
{
    void Execute();
}

public interface IAction
{
    void Execute();  // 同名方法！
}

public class Player : MonoBehaviour, IAttack, IAction
{
    // 显式实现——通过接口调用
    void IAttack.Execute()
    {
        Debug.Log("Attack!");
    }

    // 显式实现
    void IAction.Execute()
    {
        Debug.Log("Action!");
    }

    // 公开的 Execute
    public void Execute()
    {
        Debug.Log("Default");
    }
}

// 使用：
Player p = new Player();
p.Execute();                    // "Default"
((IAttack)p).Execute();        // "Attack!"
((IAction)p).Execute();        // "Action!"
```

### abstract class vs interface

| | abstract class | interface |
|--|---------------|-----------|
| 继承数量 | 只能继承一个类 | 可以实现多个接口 |
| 字段 | 可以有 | 不能有（C# 8 前） |
| 实现 | 可以有默认实现 | C# 8+ 可以有默认实现 |
| 构造函数 | 可以有 | 不能有 |
| 访问修饰符 | 所有 | 默认 public |
| 语义 | "是一个"（is-a） | "能做"（can-do） |

**什么时候选什么？**
```csharp
// abstract class：有共同的状态（字段）和行为
public abstract class Character
{
    public int HP { get; set; }
    public virtual void TakeDamage(int dmg) { HP -= dmg; }
}

// interface：定义能力
public interface IAttackable
{
    void Attack(IDamageable target);
}

// Player "is-a" Character，而且 "can-do" Attack
public class Player : Character, IAttackable
{
    public void Attack(IDamageable target)
    {
        target.TakeDamage(10);
    }
}
```

---

## 6. static 关键字

### 静态字段——所有实例共享

```csharp
public class Player
{
    public static int TotalPlayers = 0;  // 所有 Player 实例共享

    public Player()
    {
        TotalPlayers++;  // 每次 new 增加
    }
}

// 静态字段在类型初始化时分配，存活到程序结束
// 存储在托管堆上的"高频堆"（High-Frequency Heap）
// 不依赖任何实例，通过类名访问：Player.TotalPlayers
```

### 静态构造函数——类型初始化

```csharp
public class DatabaseManager
{
    public static readonly string ConnectionString;

    // 静态构造函数：在第一次访问该类前自动调用一次
    static DatabaseManager()
    {
        ConnectionString = "Server=...";
        Debug.Log("DatabaseManager initialized");
    }
}

// 触发时机：
// 1. 创建第一个实例时
// 2. 访问第一个静态成员时
// 保证只执行一次，且线程安全
```

### 静态类——工具类

```csharp
public static class MathUtils
{
    // 静态类只能包含静态成员
    // 不能实例化，不能继承
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}

// 使用：
float result = MathUtils.Lerp(0f, 1f, 0.5f);
```

Unity 中的静态类例子：`Mathf`、`Debug`、`Physics`、`Input`。

---

## 7. C# 垃圾回收（GC）——深度原理

### 为什么需要 GC？

C++ 中 `new` 后必须 `delete`——忘记 delete 导致内存泄漏。C# 的 GC 自动追踪并回收不再使用的对象。

### 代龄回收（Generational GC）

```
托管堆：
┌──────────────┐
│ 第 0 代       │ ← 新分配的对象（回收最频繁）
│ 大小 ~256KB  │
├──────────────┤
│ 第 1 代       │ ← 经过一次 GC 幸存的对象
│ 大小 ~2MB    │
├──────────────┤
│ 第 2 代       │ ← 经过多次 GC 幸存的对象（很少回收）
│ 大小 ~10MB+  │
├──────────────┤
│ 大对象堆 LOH  │ ← >85KB 的对象（不压缩）
└──────────────┘
```

**为什么分代？** 研究表明：
- 90% 的对象在第一次 GC 前就变成垃圾
- 幸存下来的对象很可能长期存活
- 所以：频繁回收第 0 代，很少回收第 2 代

### GC 触发条件

```
GC 在以下情况触发：
1. 第 0 代已满（最常见）
2. 大对象分配（LOH 满）
3. GC.Collect() 手动调用
4. 系统内存压力
```

### GC 流程：Mark → Sweep → Compact

```
阶段 1: Mark（标记）
从 GC 根出发，标记所有可达对象
GC 根包括：
  - 栈上的引用变量
  - 静态字段
  - CPU 寄存器中的引用
  - GC Handle 表（GCHandle）

阶段 2: Sweep（清扫）
遍历所有对象，回收未被标记的对象
被回收对象占用的空间变成"空闲块"

阶段 3: Compact（压缩）
将幸存对象移动到一起，合并空闲内存
（注意：大对象堆 LOH 不压缩！）
```

**GC 暂停时间：** GC 执行时，所有线程暂停（Stop The World）。这就是为什么频繁 GC 会导致游戏卡顿。

### 如何减少 GC 压力

```csharp
// 1. 避免在 Update 中分配内存
void Update()
{
    // ❌ 坏：每帧分配新字符串
    Debug.Log("HP: " + hp.ToString());

    // ✅ 好：复用
    Debug.Log($"HP: {hp}");  // 编译器优化
}

// 2. 缓存
void Update()
{
    // ❌ 坏：每帧 GetComponent
    GetComponent<Rigidbody>().velocity = ...;

    // ✅ 好：缓存到字段
    rb.velocity = ...;
}

// 3. 对象池——见 Day09
// 4. 用 struct 替代 class 减少堆分配
```

---

## 8. MonoBehaviour——Unity 的继承链

```csharp
// Unity 脚本的完整继承链：

// UnityEngine.Object    → Unity 对象的根，实现 == null 重写
//   UnityEngine.Component → 可以挂载到 GameObject 上
//     UnityEngine.Behaviour → 可以启用/禁用（enabled 属性）
//       UnityEngine.MonoBehaviour → 有生命周期回调
//         YourScript
```

### 为什么 MonoBehaviour 不能 new？

```csharp
public class Player : MonoBehaviour
{
    public int hp;
}

// ❌ 不能直接 new
Player p = new Player();  // 编译错误！

// ✅ 必须挂载到 GameObject
GameObject go = new GameObject("Player");
Player p = go.AddComponent<Player>();

// 或拖拽到 Inspector 中
```

**原因：** MonoBehaviour 的构造函数内部调用了 Unity 引擎的 C++ 层初始化——必须通过 `AddComponent` 让引擎在 C++ 层创建对应的原生对象。

### 为什么不要在 MonoBehaviour 中使用构造函数？

```csharp
public class Player : MonoBehaviour
{
    // ❌ 不要用构造函数
    public Player()
    {
        hp = 100;  // 可能不会按预期工作！
    }

    // ✅ 用 Awake 或 Start
    void Awake()
    {
        hp = 100;
    }

    private int hp;
}
```

Unity 在创建 `MonoBehaviour` 时会先调用 C++ 代码，然后调用 C# 构造函数——但此时对象可能还没有完全初始化。所有初始化逻辑放在 `Awake()` 中。

---

## 练习：将 Raylib Player 翻译为 C#（深入版）

```csharp
// Raylib 版 (C++):
// struct Player {
//     Vector2 position;
//     float speed;
//     float hp;
//     void Update() { ... }
// };
// Player player = { {100, 200}, 5.0f, 100 };

// C# Unity 版：
using UnityEngine;

public class Player : MonoBehaviour
{
    // ─── 属性 ───
    [SerializeField] private float speed = 5f;  // 序列化到 Inspector
    public float HP { get; private set; }       // 外部只读属性
    public float MaxHP { get; private set; } = 100f;  // 属性初始化器

    // ─── 组件缓存 ───
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // ─── Unity 生命周期 ───
    void Awake()
    {
        // 缓存组件——避免每帧 GetComponent
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // 初始化数据——类似 Raylib 的 InitWindow 之后
        HP = MaxHP;
    }

    void Update()
    {
        // 等价于 Raylib 的 while 循环体内
        HandleInput();
    }

    // ─── 方法 ───
    void HandleInput()
    {
        // = Raylib: IsKeyDown(KEY_D) - IsKeyDown(KEY_A)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 move = new Vector2(h, v) * speed * Time.deltaTime;
        rb.velocity = move;  // 物理系统处理移动
    }

    public void TakeDamage(int dmg)
    {
        HP = Mathf.Max(0, HP - dmg);  // 钳制到 0
        if (HP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // 触发死亡动画、音效等
        Destroy(gameObject);  // 销毁自身
    }
}
```

**对照总结：**

| Raylib (C++) | Unity (C#) | 底层差异 |
|-------------|-----------|---------|
| `struct Player { Vector2 pos; }` | `class Player : MonoBehaviour` | Unity 用 class（引用类型）+ 组件挂载 |
| `player.Update(dt)` | Unity 自动调用 `Update()` | PlayerLoop 调度 |
| `DrawTexturePro(player.texture, ...)` | SpriteRenderer 自动渲染 | 引擎自动 Draw Call |
| `player.hp -= damage;` | 属性封装 + 逻辑验证 | Property 语法糖 |
| 手动 `new/delete` | GC 自动回收 | 代龄 GC 管理 |

---

---

## C++/Raylib 对照总结

| 概念 | C++ | C# |
|------|-----|-----|
| 类默认访问 | private | public |
| Getter/Setter | 手写 `GetHP()`/`SetHP()` | Property `{ get; set; }` |
| 虚函数 | `virtual` + `override`（C++11） | `virtual` + `override` |
| 纯虚函数 | `virtual void f() = 0` | `abstract void f()` |
| 接口 | 抽象类（无单独关键字） | `interface` 关键字 |
| 多继承 | 支持（菱形问题） | 不支持类多继承，支持多接口 |
| 析构 | 确定性 `~Class()` | GC 决定，`IDisposable` 模式 |
| 对象创建 | `Player* p = new Player()` | `Player p = new Player()` |
| vtable | 对象头部 vptr | TypeHandle 中的方法表 |
| 类型信息 | RTTI（需开启） | 每个对象都有 `GetType()` |

## 停靠点

> C# 的 `Property` = C++ getter/setter 的编译器合成。`override` 运行时调度，`new` 编译时隐藏——完全不同。
> abstract class 定义"是什么"，interface 定义"能做什么"。
> 静态成员不属于实例，属于类型本身。
> GC 是代龄回收——新对象容易死，老对象活得久。避免在 Update 中分配内存。
> MonoBehaviour 不能 new——必须通过 AddComponent。

