# Day 2：面向对象深入 — 泛型协变逆变、模式匹配与现代 C# 特性

## 0. 为什么还需要深入 OOP？

Day02 讲了 class、interface、继承等基础 OOP 概念。但 C# 的 OOP 在最近几个版本中大幅进化：**Record** 让数据对象有了值语义，**模式匹配** 让你扔掉一堆 if-else，**默认接口方法** 让接口可以演进，**协变/逆变** 让泛型使用更灵活。

这一篇不是复习 OOP 基础——而是讲 C# 8/9/10/11 中**改变你写代码方式**的 OOP 新特性。

---

## 1. 协变（Covariance）与逆变（Contravariance）——in/out 关键字

### 问题：为什么 List&lt;Cat&gt; 不是 List&lt;Animal&gt;？

```csharp
public class Animal { }
public class Cat : Animal { }

List<Cat> cats = new List<Cat>();
// List<Animal> animals = cats;  // 编译错误！
```

直觉上 `Cat` 是 `Animal` 的子类，`List<Cat>` 也应该是 `List<Animal>` 的子类型。但**不是**——因为如果你能这样赋值，就可以往 `List<Animal>` 中添加 `Dog`，而实际类型是 `List<Cat>`：

```csharp
List<Cat> cats = new List<Cat>();
List<Animal> animals = cats;
animals.Add(new Dog());  // 如果允许，就破坏了类型安全！
// 现在 cats 里有了一只 Dog...
```

### IEnumerable&lt;out T&gt;——协变

```csharp
// IEnumerable<T> 的定义中有 out 关键字
public interface IEnumerable<out T>
{
    IEnumerator<T> GetEnumerator();
}

// 因为 out T = T 只从接口输出（作为返回值），不输入（不作为参数）
// 所以把 Cat 作为 Animal 使用是安全的

IEnumerable<Cat> cats = new List<Cat>();
IEnumerable<Animal> animals = cats;  // 可以！协变

// 为什么安全？因为 IEnumerable 只读，不能 Add：
// animals.Add(new Dog());  // 编译错误！IEnumerable 没有 Add
```

**out 关键字：T 只能出现在输出位置（返回值、只读属性）。**

### Action&lt;in T&gt;——逆变

```csharp
// Action<T> 的定义中有 in 关键字
public delegate void Action<in T>(T obj);

// 因为 in T = T 只从接口输入（作为参数），不输出
// 所以你可以把接受 Animal 的委托当作接受 Cat 的委托

Action<Animal> handleAnimal = (a) => a.Sleep();
Action<Cat> handleCat = handleAnimal;  // 可以！逆变

// 为什么安全？
// 调用：handleCat(new Cat());
// 实际上调用：handleAnimal(new Cat()); —— Cat 是 Animal，完全安全

// 反过来不行：
// Action<Cat> meow = (c) => c.Meow();
// Action<Animal> action = meow;  // 编译错误！Animal 没有 Meow()
```

**in 关键字：T 只能出现在输入位置（参数、只写属性）。**

### Func&lt;in T, out TResult&gt;——协变逆变同时存在

```csharp
// Func<T, TResult> = in T 逆变 + out TResult 协变
public delegate TResult Func<in T, out TResult>(T arg);

Func<Cat, string> describe = (c) => $"{c.Name} is a cat";

// 逆变输入：可以把接受 Animal 的委托赋给接受 Cat 的
Func<Animal, string> describeAnimal = (a) => $"{a.GetType()}";
Func<Cat, string> catDesc = describeAnimal;  // 逆变（参数）

// 协变输出：可以把返回 Cat 的委托赋给返回 Animal 的
Func<Cat, Cat> getCat = (c) => c;
Func<Cat, Animal> getAnimal = getCat;  // 协变（返回值）
```

### 在自定义接口中使用 in/out

```csharp
// ❌ 没有 in/out：不支持协变/逆变
public interface IRepository<T>
{
    T Get();
    void Set(T item);
}
// IRepository<Cat> 不能转 IRepository<Animal>

// ✅ 协变接口：只读
public interface IReadOnlyRepository<out T>
{
    T Get();
    IEnumerable<T> GetAll();
}
// IReadOnlyRepository<Cat> → IReadOnlyRepository<Animal>

// ✅ 逆变接口：只写
public interface IWriteOnlyRepository<in T>
{
    void Save(T item);
    void SaveAll(IEnumerable<T> items);
}
// IWriteOnlyRepository<Animal> → IWriteOnlyRepository<Cat>
```

### Unity 中的实际场景

```csharp
// 协变的实用例子——处理不同 Component 类型
public interface IUnityComponent<out T> where T : Component
{
    T Component { get; }
    bool IsActive { get; }
}

public class PlayerUI : IUnityComponent<UIBehaviour>
{
    public UIBehaviour Component => GetComponent<UIBehaviour>();
    public bool IsActive => gameObject.activeSelf;
}

// 协变使得：
IUnityComponent<UIBehaviour> uiComp = new PlayerUI();
IUnityComponent<Component> comp = uiComp;  // 可以！

// 逆变处理不同类型的事件
public interface IEventHandler<in T> where T : EventArgs
{
    void Handle(object sender, T args);
}

public class DamageHandler : IEventHandler<DamageArgs>
{
    public void Handle(object sender, DamageArgs args) { }
}

// 可以处理所有 EventArgs 的处理器
IEventHandler<EventArgs> generic = new SomeGenericHandler();
IEventHandler<DamageArgs> damageHandler = generic;  // 逆变：可以！
```

### 对比 C++ 模板

```cpp
// C++ 模板：没有协变/逆变概念
// std::vector<Cat*> 和 std::vector<Animal*> 是不同类型
// 需要手动转换或使用指针

// C#：协变/逆变让类型关系更自然
// IEnumerable<Cat> → IEnumerable<Animal> （安全）
```

---

## 2. 模式匹配——丢掉 if-else 链

### is 表达式及其演进

```csharp
// C# 7.0 之前：基础 is 检查
if (obj is Cat) { }

// C# 7.0：带变量的 is
if (obj is Cat cat)
{
    cat.Meow();  // 模式匹配 + 变量声明
}

// C# 9.0：否定模式
if (obj is not null)
{
    Process(obj);
}

// 组合模式
if (obj is Cat { Age: > 5 } oldCat)
{
    // 只匹配 Age > 5 的 Cat
    oldCat.GiveSeniorDiscount();
}
```

### switch 表达式（C# 8.0）

```csharp
// 传统 switch 语句
string GetAnimalSound(Animal animal)
{
    switch (animal)
    {
        case Cat c:
            return "Meow";
        case Dog d when d.BarkVolume > 10:
            return "WOOF!";
        case Dog d:
            return "Woof";
        case null:
            return "Silence";
        default:
            return "Unknown";
    }
}

// switch 表达式（简洁得多！）
string GetAnimalSound(Animal animal) => animal switch
{
    Cat c => "Meow",
    Dog { BarkVolume: > 10 } => "WOOF!",
    Dog => "Woof",
    null => "Silence",
    _ => "Unknown"  // default
};
```

### 属性模式——按属性匹配

```csharp
// 匹配对象的属性，而不是类型
public string GetTaxInfo(Player player) => player switch
{
    { Level: >= 50 } => "Veteran player",
    { Level: >= 20, Class: "Warrior" } => "Mid-game warrior",
    { IsVip: true } => "VIP player",
    _ => "Regular player"
};
```

### 元组模式——同时匹配多个值

```csharp
// 传统的状态机
string GetCombatResult(string attack, string defense)
{
    if (attack == "Rock" && defense == "Scissors") return "Win";
    if (attack == "Rock" && defense == "Paper") return "Lose";
    // ...更多组合
    return "Draw";
}

// 元组模式——直观！
string GetCombatResult(string attack, string defense)
    => (attack, defense) switch
{
    ("Rock", "Scissors") => "Win",
    ("Rock", "Paper")    => "Lose",
    ("Scissors", "Paper") => "Win",
    ("Scissors", "Rock")  => "Lose",
    ("Paper", "Rock")    => "Win",
    ("Paper", "Scissors") => "Lose",
    _ => "Draw"
};
```

### 列表模式（C# 11）

```csharp
// 检查数组/集合的匹配模式
int[] scores = { 100, 90, 80 };

string description = scores switch
{
    [100, 90, 80] => "Perfect sequence!",
    [100, _, _] => "Starts with perfect score",
    [> 80, > 80, > 80] => "All above 80",
    [.. var all] when all.Average() > 85 => "Good average",
    _ => "Regular scores"
};

// 在 Unity 中检查技能序列
string GetSkillCombo(string[] inputs) => inputs switch
{
    ["Light", "Light", "Heavy"] => "Combo attack!",
    ["Light", "Heavy", "Light"] => "Spin attack!",
    ["Jump", "Heavy"] => "Downward strike!",
    [.. var seq] when seq.Length > 3 => "Custom combo",
    _ => "Simple attack"
};
```

### 对比 C++

```cpp
// C++ 的模式匹配（C++17 if constexpr）：
// if constexpr (std::is_same_v<T, Cat>) { ... }
// 只能在编译期使用

// C++23 的 std::visit + variant：
std::visit(overloaded {
    [](Cat& c) { c.Meow(); },
    [](Dog& d) { d.Bark(); },
    [](auto&) {}
}, animal);

// C#：
var result = animal switch
{
    Cat c => c.Meow(),
    Dog d => d.Bark(),
    _ => "Unknown"
};
```

---

## 3. Records——值语义的数据对象

### Record vs Class vs Struct

```csharp
// class：引用类型，引用相等（比地址）
public class PlayerClass
{
    public string Name { get; set; }
    public int Level { get; set; }
}

// struct：值类型，逐字段相等
public struct PlayerStruct
{
    public string Name;
    public int Level;
}

// record：引用类型，但值相等（比字段内容）
public record PlayerRecord(string Name, int Level);

// 使用：
var a = new PlayerClass { Name = "Alice", Level = 10 };
var b = new PlayerClass { Name = "Alice", Level = 10 };
Console.WriteLine(a == b);  // false（不同引用）

var c = new PlayerStruct { Name = "Alice", Level = 10 };
var d = new PlayerStruct { Name = "Alice", Level = 10 };
Console.WriteLine(c == d);  // true（值相等）

var e = new PlayerRecord("Alice", 10);
var f = new PlayerRecord("Alice", 10);
Console.WriteLine(e == f);  // true（值相等！record 重写了 Equals）
```

### Record 的编译器生成代码

```csharp
public record PlayerRecord(string Name, int Level);
```

编译器展开为（简化）：

```csharp
public class PlayerRecord : IEquatable<PlayerRecord>
{
    public string Name { get; init; }  // init-only 属性
    public int Level { get; init; }

    public PlayerRecord(string Name, int Level)
    {
        this.Name = Name;
        this.Level = Level;
    }

    // 值相等
    public virtual bool Equals(PlayerRecord? other)
    {
        return other != null
            && EqualityComparer<string>.Default.Equals(Name, other.Name)
            && EqualityComparer<int>.Default.Equals(Level, other.Level);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Level);
    }

    // 非破坏性修改
    public PlayerRecord With(string? Name = null, int? Level = null)
    {
        // 编译器生成解构
        return new PlayerRecord(
            Name ?? this.Name,
            Level ?? this.Level
        );
    }

    // 解构
    public void Deconstruct(out string Name, out int Level)
    {
        Name = this.Name;
        Level = this.Level;
    }

    // ToString
    public override string ToString()
        => $"PlayerRecord {{ Name = {Name}, Level = {Level} }";
}
```

### with 表达式——非破坏性修改

```csharp
var original = new PlayerRecord("Alice", 10);

// ❌ 不能修改：
// original.Level = 15;  // 编译错误！init-only 属性不能在声明后修改

// ✅ with 表达式——创建新实例，修改指定字段
var leveledUp = original with { Level = 15 };
// original 不变：("Alice", 10)
// leveledUp: ("Alice", 15)

// 批量修改
var renamed = original with { Name = "Alicia", Level = 20 };
```

### Record 的继承

```csharp
// 基类 record
public record Character(string Name, int HP);

// 派生 record
public record PlayerRecord(string Name, int HP, string Class)
    : Character(Name, HP);

// 使用
var player = new PlayerRecord("Alice", 100, "Warrior");
var copy = player with { Class = "Mage" };

// 值相等包含所有字段
var p1 = new PlayerRecord("Alice", 100, "Warrior");
var p2 = new PlayerRecord("Alice", 100, "Mage");
Console.WriteLine(p1 == p2);  // false（Class 不同）
```

### Unity 中的 Record 使用

```csharp
// Unity 中 MonoBehaviour 是 class（引用类型），不适合用 record
// 但数据对象（DTO、配置、事件数据）非常适合 record

// 游戏事件数据
public record DamageEvent(int Amount, string Source, DamageType Type);
public record HealEvent(int Amount, string HealerName);

// 配置数据
public record WeaponStats(string Name, int Damage, float Range, float FireRate);

// 状态数据——适合 with 表达式做不可变修改
public record GameState(int Score, int Lives, float TimeLeft);

public class GameManager : MonoBehaviour
{
    private GameState _state = new GameState(0, 3, 300f);

    public void OnEnemyKilled(int points)
    {
        // 不可变更新
        _state = _state with { Score = _state.Score + points };
    }
}
```

---

## 4. Init-only 属性与 Required 成员

### Init-only——创建后只读

```csharp
public class PlayerConfig
{
    public string Name { get; init; }    // 只能在初始化时设置
    public int Level { get; init; }
    public string Class { get; init; }
}

// 使用对象初始化器设置——只能一次
var config = new PlayerConfig
{
    Name = "Alice",
    Level = 10,
    Class = "Warrior"
};

// config.Name = "Bob";  // 编译错误！init-only
```

**与 readonly 的区别：**

| | readonly 字段 | init-only 属性 |
|--|------------|---------------|
| 位置 | 字段 | 属性 |
| 构造函数赋值 | 可以 | 可以 |
| 对象初始化器赋值 | 不能 | 可以 |
| 之后修改 | 不能 | 不能 |

### Required——必须赋值的属性（C# 11）

```csharp
public class GameSettings
{
    public required string PlayerName { get; set; }  // 必须提供
    public required int Difficulty { get; set; }     // 必须提供
    public float Volume { get; set; } = 0.8f;        // 可选，默认值
}

// 使用——编译器强制必须赋值
var settings = new GameSettings
{
    PlayerName = "Alice",  // 不写就编译错误！
    Difficulty = 2         // 不写就编译错误！
};

// ❌ 缺少 Required 成员：
// var bad = new GameSettings { PlayerName = "Bob" };
// 编译错误：'Required member GameSettings.Difficulty must be set'
```

### Unity 中的实践

```csharp
public class WeaponConfig : ScriptableObject
{
    public required string WeaponName;
    public required int BaseDamage;
    public float Cooldown = 1.0f;
    public GameObject ProjectilePrefab;

    // 在 OnEnable 中验证
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(WeaponName))
            Debug.LogError("WeaponName is required!");
    }
}

// 或者在构造时强制要求
public class DamageCalculator
{
    public required Func<int, int> DamageFormula { get; init; }
    public required int BaseValue { get; init; }

    public int Calculate(int level)
    {
        return DamageFormula(BaseValue + level);
    }
}
```

---

## 5. 默认接口方法（C# 8.0+）

### 问题：接口一旦发布就不能加方法

```csharp
// 1.0 版本：定义接口
public interface ILogger
{
    void Log(string message);
}

// 很多类实现了它
public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine(message);
}

// 2.0 版本：想加一个新方法
// public interface ILogger
// {
//     void Log(string message);
//     void LogWarning(string message);  // ← 这会破坏所有实现！
// }
```

### 解决方案：默认实现

```csharp
// 2.0 版本：用默认实现
public interface ILogger
{
    void Log(string message);

    // 默认实现——现有代码不需要修改！
    void LogWarning(string message)
    {
        Log($"[WARNING] {message}");
    }

    void LogError(string message)
    {
        Log($"[ERROR] {message}");
    }
}

// 现有实现——不需要改！
public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine(message);
    // LogWarning 和 LogError 使用默认实现
}

// 使用
ILogger logger = new ConsoleLogger();
logger.LogWarning("Something is wrong");  // 调用默认实现
```

### 接口中的状态？

```csharp
public interface ICounter
{
    // 接口可以定义静态字段（C# 11 前不能有实例字段）
    static int TotalInstances = 0;

    void Increment()
    {
        TotalInstances++;
    }

    static int GetTotal() => TotalInstances;
}

// 注意：接口不能有实例字段（引用状态）
// 因为一个类可以实现多个接口，如果用同一个字段会有歧义
```

---

## 6. 静态抽象接口方法（C# 11）——泛型数学/工厂模式

### 问题：泛型不能调用运算符

```csharp
// 想写一个通用的加法
T Add<T>(T a, T b)
{
    // return a + b;  // 编译错误！泛型类型不能保证有 + 运算符
}
```

### 解决方案：静态抽象接口方法

```csharp
// 定义一个支持加法的接口
public interface IAddable<TSelf> where TSelf : IAddable<TSelf>
{
    static abstract TSelf operator +(TSelf left, TSelf right);
}

// 让 int 实现（实际上 CLR 已经内置）
// 但你可以为自己的类型实现：

public struct Vector2D : IAddable<Vector2D>
{
    public float X, Y;

    public static Vector2D operator +(Vector2D left, Vector2D right)
    {
        return new Vector2D { X = left.X + right.X, Y = left.Y + right.Y };
    }
}

// 现在可以通用地加了！
T AddAny<T>(T a, T b) where T : IAddable<T>
{
    return a + b;  // ✅ 可以了！
}
```

### Unity 实用场景——泛型工厂

```csharp
// 泛型工厂模式：任何类型只要实现这个接口就能被工厂创建
public interface IFactory<out T>
{
    static abstract T CreateDefault();
}

public class Bullet : IFactory<Bullet>
{
    public int Damage;
    public float Speed;

    public static Bullet CreateDefault()
    {
        return new Bullet { Damage = 10, Speed = 20f };
    }
}

public class Enemy : IFactory<Enemy>
{
    public int HP;
    public float MoveSpeed;

    public static Enemy CreateDefault()
    {
        return new Enemy { HP = 100, MoveSpeed = 5f };
    }
}

// 通用工厂方法
public static class ObjectFactory
{
    public static T CreateDefault<T>() where T : IFactory<T>
    {
        return T.CreateDefault();  // 调用静态抽象接口方法
    }
}

// 使用：
Bullet b = ObjectFactory.CreateDefault<Bullet>();
Enemy e = ObjectFactory.CreateDefault<Enemy>();
```

---

## 7. 分部属性与分部索引器（C# 11）

### Partial Properties——分部类中的属性

```csharp
// 一个类可以分散在多个文件中
// File 1: Player.Part1.cs
public partial class Player
{
    // 声明分部属性（只有签名）
    public partial int Health { get; set; }
}

// File 2: Player.Part2.cs
public partial class Player
{
    // 实现分部属性（可以用自动属性语法糖）
    private int _health;

    public partial int Health
    {
        get => _health;
        set => _health = Mathf.Clamp(value, 0, 100);
    }
}
```

### 对 Unity 开发的意义

```csharp
// Unity 项目经常有大型 MonoBehaviour 脚本
// Partial Classes + Partial Properties 可以拆分文件

// Player.cs —— 核心逻辑
public partial class Player : MonoBehaviour
{
    public partial int Health { get; }
    public partial void TakeDamage(int dmg);
}

// Player.Combat.cs —— 战斗逻辑
public partial class Player
{
    private int _health = 100;

    public partial int Health => _health;

    public partial void TakeDamage(int dmg)
    {
        _health -= dmg;
        if (_health <= 0) Die();
    }
}

// Player.Animation.cs —— 动画逻辑
public partial class Player
{
    public partial void TakeDamage(int dmg)
    {
        // 注意：同一个分部方法只能有一个实现
        // 这里只是演示不同关注点可以拆分到不同文件
    }
}
```

---

## C++/Raylib 对照总结

| 概念 | C++ | C# |
|------|-----|-----|
| 协变/逆变 | 不支持模板变体 | `out`/`in` 关键字 |
| 模式匹配 | `if constexpr` / `std::visit` | `is` / `switch` 表达式 |
| 值对象 | `struct` 手写 operator== | `record` 自动生成 |
| 不可变对象 | `const` 成员函数 | `init` 属性 / `record` |
| 默认接口方法 | 抽象类提供默认实现 | 接口中直接写方法体 |
| 静态多态 | 模板特化 | 静态抽象接口方法 |
| 分部类 | 无原生支持（可以 #include） | `partial` 关键字 |

## 停靠点

> 协变（out）让 IEnumerable&lt;Cat&gt; 可以变成 IEnumerable&lt;Animal&gt;。逆变（in）让 Action&lt;Animal&gt; 可以变成 Action&lt;Cat&gt;。
> 模式匹配让你用 switch 表达式代替 if-else 链——尤其是属性模式和元组模式非常实用。
> Record 是引用类型但值相等——编译器自动生成 Equals/GetHashCode/ToString/Deconstruct。with 表达式做非破坏性修改。
> init 属性只能在对象初始化器中赋值一次。required 属性强制调用者必须赋值。
> 默认接口方法让接口可以演进而不破坏已有实现。
> 静态抽象接口方法让泛型可以调用运算符和静态方法——开启泛型数学和工厂模式。
