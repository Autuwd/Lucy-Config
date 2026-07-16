# Day 1：C# 类型系统 — 从 CLR 内存模型到高层抽象

## 0. 为什么需要类型系统？

类型系统是编程语言的**内存布局规范**。你在代码中写 `int x = 5`，编译器需要知道：
- 分配多少内存？（32 位 = 4 字节）
- 如何解释这 4 字节的二进制数据？（补码表示的有符号整数）
- 可以对它做什么操作？（加减乘除，不能做字符串操作）

C# 的类型系统建立在 **CLR（Common Language Runtime）** 之上——类似 C++ 程序运行在操作系统之上，C# 程序运行在 CLR 这个"虚拟 OS"中。

---

## 1. CLR 内存模型：栈 vs 托管堆

C# 程序的内存分为两大区域：

```
高地址
┌──────────────────────┐
│      栈 (Stack)       │  ← 每个线程独立，自动管理
│  - 值类型实例         │
│  - 方法参数           │
│  - 局部变量           │
│  - 返回地址           │
├──────────────────────┤
│       ↓ 增长方向       │
│                      │
│  ─ ─ ─ ─ ─ ─ ─ ─ ─  │
│                      │
│       ↑ 增长方向       │
├──────────────────────┤
│  托管堆 (Managed Heap) │  ← 所有线程共享，GC 管理
│  - 引用类型实例        │
│  - 字符串常量          │
│  - 静态字段            │
│  - 类型对象指针         │
│                      │
│  ┌────────────────┐   │
│  │ 第 0 代 (最新)  │   │
│  ├────────────────┤   │
│  │ 第 1 代        │   │
│  ├────────────────┤   │
│  │ 第 2 代 (最老)  │   │
│  │ 大对象堆 LOH    │   │
│  └────────────────┘   │
└──────────────────────┘
低地址
```

### 栈（Stack）
- 每个线程**独立拥有**自己的栈
- 方法调用时压入栈帧（参数、局部变量、返回地址）
- 方法返回时**自动弹出**——无需手动管理
- **容量有限**（默认 1MB），递归过深会 StackOverflow

### 托管堆（Managed Heap）
- 所有线程**共享**同一托管堆
- 引用类型的实例**必须**在堆上分配
- GC 负责回收不再使用的对象
- **大对象堆（LOH）** 用于 85,000 字节以上的对象，不压缩

### 对比 C++

| 概念 | C++ | C# |
|------|-----|-----|
| 栈分配 | `int x = 5;`（局部变量） | `int x = 5;`（值类型） |
| 堆分配 | `int* p = new int(5);` 手动 delete | `object o = new object();` GC 自动回收 |
| 控制权 | 程序员全权控制 | CLR 托管，程序员无手动释放（除非非托管资源） |

---

## 2. 值类型（Value Type）

### 定义
值类型的变量**直接包含数据**。赋值时复制整个数据。

```
内存中的 int x = 42:
┌──────────────┐
│    42        │  ← 4 字节，直接存值
│  0x0000002A  │
└──────────────┘
```

### 哪些是值类型？
- 所有数值类型：`int`, `float`, `double`, `byte`, `short`, `long`
- `bool`, `char`
- `struct`（自定义结构体）
- `enum`（枚举）
- `decimal`

### 值类型的 IL 层面

C# 代码：
```csharp
int a = 10;
int b = a;  // 复制值
b = 20;     // a 还是 10
```

对应的 IL（中间语言）：
```cil
// int a = 10;
ldc.i4.s   10        // 将常量 10 压入栈
stloc.0              // 弹出并存到局部变量 0 (a)

// int b = a;
ldloc.0              // 将 a 的值压入栈
stloc.1              // 弹出并存到局部变量 1 (b) ← 复制值

// b = 20;
ldc.i4.s   20        // 将常量 20 压入栈
stloc.1              // 覆盖 b ← a 不受影响
```

**关键：** `stloc.1` 存储的是**值本身**，不是引用——所以 `b = 20` 不会改变 `a`。

### 值类型的 struct 内存布局

```csharp
struct Point
{
    public int x;  // 4 字节
    public int y;  // 4 字节
}
```

`Point` 的内存布局：
```
偏移 0: x (4 字节)
偏移 4: y (4 字节)
总计: 8 字节（栈上连续）
```

CLR 默认使用 **Sequential 布局**（按声明顺序排列），可以用 `[StructLayout]` 属性控制：

```csharp
[StructLayout(LayoutKind.Explicit)]
struct ExplicitLayout
{
    [FieldOffset(0)] public int intValue;
    [FieldOffset(0)] public float floatValue;  // 和 intValue 共用 4 字节
}
```
这类似于 C++ 的 union——两种类型共用同一块内存。

### 赋值行为

```csharp
Point p1 = new Point { x = 1, y = 2 };
Point p2 = p1;    // 复制整个 8 字节
p2.x = 99;        // p1.x 还是 1

// 内存状态：
// p1: [1][2]  ← 栈上的 8 字节
// p2: [99][2] ← 完全独立的 8 字节
```

和 C++ 的 struct 赋值**完全一致**：
```cpp
// C++
struct Point { int x; int y; };
Point p1 = {1, 2};
Point p2 = p1;   // memcpy 逐字节复制
p2.x = 99;       // p1.x 不变
```

### Boxing / Unboxing——值类型转引用类型的代价

**boxing（装箱）：** 将值类型包装成引用类型，放到堆上。

```csharp
int x = 42;
object obj = x;  // boxing！分配堆内存
```

IL 层面：
```cil
ldloc.0          // 加载 x 的值
box    [mscorlib]System.Int32  // 装箱！在堆上创建 Int32 对象
stloc.1          // obj 引用指向堆上的 Int32
```

装箱后的内存布局：
```
栈上:               托管堆上:
┌─────────┐        ┌────────────────┐
│ obj     │──────→ │ SyncBlock      │  ← 同步块索引
└─────────┘        │ TypeHandle     │  ← 指向 Int32 类型
                   │ 42             │  ← 实际值
                   └────────────────┘
```

**unboxing（拆箱）：** 从堆上的对象提取回值类型。

```csharp
int y = (int)obj;  // unboxing
```

**boxing 的性能代价：**
1. 在堆上分配内存（GC 压力）
2. 复制数据到堆
3. 拆箱时类型检查（isinst 指令）
4. 拆箱后需要 GC 回收

**常见装箱陷阱：**
```csharp
// ❌ 坏：每次循环装箱
int sum = 0;
ArrayList list = new ArrayList();  // ArrayList 存储 object
for (int i = 0; i < 1000; i++)
{
    list.Add(i);  // 每次 Add 都是装箱！
}

// ✅ 好：用泛型避免装箱
List<int> list2 = new List<int>();  // List<int> 专门存 int
for (int i = 0; i < 1000; i++)
{
    list2.Add(i);  // 无装箱，直接存值
}
```

---

## 3. 引用类型（Reference Type）

### 定义
引用类型的变量**存储的是对象的地址（引用）**，而不是数据本身。赋值时复制引用（地址），两个引用指向同一个对象。

```
内存中的 Player p = new Player():
栈上:                   托管堆上:
┌─────────────┐        ┌────────────────┐
│ p (引用)     │──────→ │ SyncBlock      │  ← 同步块索引（用于 lock）
│ 0x00A3F82C  │        │ TypeHandle     │  ← 指向 Player 类型元数据
└─────────────┘        │ 实例字段...      │
                       │  - int hp      │
                       │  - string name │
                       └────────────────┘
```

### 哪些是引用类型？
- `class`（自定义类）
- `string`
- 数组（`int[]`, `string[]` 等）
- `delegate`（委托）
- `interface`
- `object`（所有类型的基类）

### 赋值行为——与 C++ 指针相同

```csharp
class Player
{
    public int hp;
    public string name;
}

Player p3 = new Player { hp = 100, name = "Alice" };
Player p4 = p3;       // 复制引用，指向同一个对象
p4.hp = 50;           // p3.hp 也变成 50！
```

对应的 C++ 概念：
```cpp
// C++ 指针版（等价）
Player* p3 = new Player{100, "Alice"};
Player* p4 = p3;      // 复制指针
p4->hp = 50;          // p3->hp 也是 50
```

### 托管堆上的对象结构

每个引用类型对象在堆上包含三个部分：

```
偏移 0: SyncBlock (4/8 字节)  ← 用于线程同步（lock 关键字）
偏移 4/8: TypeHandle (4/8 字节) ← 指向类型的方法表
偏移 8/16: 实例字段开始...

类型方法表（Type Method Table）：
┌──────────────────────┐
│ 基类信息              │
│ 接口映射              │
│ 虚方法表 (vtable)     │
│  - Object.ToString()  │
│  - Player.TakeDamage()│
│  - Player.GetHP()     │
│ 静态字段               │
└──────────────────────┘
```

每次通过引用访问对象，CLR 都会通过 TypeHandle 做**类型安全检查**——这就是 C# 称为"安全代码"的原因，不存在 C++ 的 dangling pointer 问题。

### null 的底层

```csharp
Player p = null;  // 引用为 0
// 内存: p = 0x00000000

p.hp = 100;  // NullReferenceException!
// CLR 检测到引用是 0，立即抛异常
```

`null` = 引用为 0x00000000。C++ 的 `nullptr` 也是 0。

与 C++ 不同，C# 中访问 null 引用**保证崩溃并报告错误位置**，不会像 C++ 那样产生未定义行为。

---

## 4. string 类型深度分析

### string 是引用类型，为什么行为像值类型？

```csharp
string a = "hello";
string b = a;       // 复制引用（地址）
b = "world";        // 创建新字符串，a 不变

// a 还是 "hello"——看起来像值类型的行为
```

**原理：string 的不可变性（Immutability）**

```
步骤 1: string a = "hello";

栈:                  托管堆:
a ────────────────→ "hello" (第1个字符串对象)

步骤 2: string b = a;

栈:                  托管堆:
a ────────────────→ "hello"
b ────────────────→ "hello" (指向同一个对象)

步骤 3: b = "world";

栈:                  托管堆:
a ────────────────→ "hello"
b ───→ "world" (新建第2个字符串对象)
```

`b = "world"` **不是修改已有字符串**，而是：
1. 在堆上创建新的字符串对象 `"world"`
2. 将 b 的引用指向新对象
3. 原来的 `"hello"` 不变

这就是为什么 string 行为像值类型——每次"修改"都创建新对象。

### 字符串池（Intern Pool）

CLR 维护一个全局的字符串池，避免重复字符串：

```csharp
string s1 = "hello";
string s2 = "hello";

// s1 和 s2 指向同一个堆对象！
// 因为编译期相同的字符串字面量被驻留到字符串池
```

```csharp
// 运行时动态生成的字符串不会自动驻留
string s1 = "hello";
string s2 = "hel" + "lo";  // 编译期常量折叠，还是同一个

string s3 = "hel";
string s4 = s3 + "lo";     // 运行时拼接，新对象！

// s1 == s4 是 false（引用不同）
// s1.Equals(s4) 是 true（值相同）
```

**性能影响：** string 拼接创建大量临时对象

```csharp
// ❌ 坏：每次 += 创建新 string 对象
string s = "";
for (int i = 0; i < 1000; i++)
{
    s += i.ToString();  // 创建 1000 个临时字符串！
}

// ✅ 好：StringBuilder 内部用 char[] 缓冲区
System.Text.StringBuilder sb = new System.Text.StringBuilder();
for (int i = 0; i < 1000; i++)
{
    sb.Append(i);  // 修改缓冲区，不创建新对象
}
string result = sb.ToString();  // 最后生成一次
```

---

## 5. 特殊类型

### 可空类型（Nullable<T>）

```csharp
int? x = null;    // 语法糖
```

底层实现：
```csharp
// 编译器将 int? 转换为：
Nullable<int> x = new Nullable<int>();
```

`Nullable<T>` 结构体的定义：
```csharp
public struct Nullable<T> where T : struct
{
    private T value;       // 实际值
    private bool hasValue; // 是否有值

    public bool HasValue => hasValue;
    public T Value
    {
        get
        {
            if (!hasValue) throw new InvalidOperationException();
            return value;
        }
    }
}
```

**?? 运算符的 IL 层：**
```csharp
int y = x ?? 0;  // x 有值则取 x，否则取 0
```
等价于：
```csharp
int y = x.HasValue ? x.Value : 0;
```

### enum（枚举）

```csharp
enum Direction { North, South, East, West }
// 默认 int 类型：North=0, South=1, East=2, West=3

enum DirectionExplicit : byte  // 指定底层类型
{
    North = 1,
    South = 2,
    East = 4,
    West = 8
}
```

enum 是值类型，底层是整数。`Direction.North` 在内存中就是一个 `0`。

**常见陷阱：**
```csharp
Direction d = (Direction)99;  // 不会报错！enum 不检查值范围
Debug.Log(d);  // 输出 "99"

// 安全做法：
if (Enum.IsDefined(typeof(Direction), d)) { }
```

---

## 6. 类型转换与检查

### 隐式转换——编译器自动处理

```csharp
int i = 42;
long l = i;          // int → long，安全（不会溢出）
float f = i;         // int → float，可能精度损失但范围安全
```

### 显式转换——需要强制转换运算符

```csharp
long l = 100L;
int i = (int)l;      // 可能溢出，需要显式转换

float f = 3.14f;
int i2 = (int)f;     // 丢弃小数部分，i2 = 3
```

### is 运算符——类型检查

```csharp
object obj = "hello";
if (obj is string)  // isinst 指令
{
    Console.WriteLine("It's a string!");
}
```

IL 层面：`is` 编译为 `isinst` 指令，如果类型匹配返回非空引用，否则返回 null。

### as 运算符——安全类型转换

```csharp
object obj = "hello";
string s = obj as string;  // 成功 → s = "hello"

object obj2 = 42;
string s2 = obj2 as string;  // 失败 → s2 = null（不抛异常）
```

`as` 和 `is` 的区别：
```csharp
// as = is + 转换，一步完成
string s = obj as string;

// 等价于：
// if (obj is string)
//     s = (string)obj;
// else
//     s = null;
```

### GetType() vs typeof

```csharp
// 编译时类型信息
Type t1 = typeof(int);         // System.Int32
Type t2 = typeof(List<int>);   // System.Collections.Generic.List`1[System.Int32]

// 运行时类型信息
int x = 42;
Type t3 = x.GetType();         // System.Int32（运行时确定）

// typeof 是编译期常量，GetType 是运行时虚方法
```

---

## 7. Unity 中的类型陷阱（深入分析）

### Vector3 是值类型（struct）

```csharp
Vector3 v1 = new Vector3(1, 2, 3);
Vector3 v2 = v1;     // 赋值整个 struct（12 字节）
v2.x = 99;           // v1.x 不变

// 内存中：
// v1: [1.0f][2.0f][3.0f] (栈上 12 字节)
// v2: [99f][2.0f][3.0f] (独立的 12 字节)
```

为什么 Vector3 是值类型？因为：
- 经常大量创建（每帧上千次的向量运算）
- 值类型在栈上分配，无 GC 压力
- 向量运算通常是暂时的，不需要在堆上长期存活

### Transform 是引用类型（class）

```csharp
Transform t1 = gameObject.transform;
Transform t2 = t1;          // 复制引用
t2.position = Vector3.zero; // t1.position 也变了

// t1 和 t2 指向同一个 C++ 对象（Transform 的 C++ 层实现）
```

Transform 为什么是引用类型？因为：
- 一个 GameObject 只有一个 Transform
- 多个脚本需要共享访问同一个 Transform
- Transform 需要维护层级树（父子关系），需要引用语义

### 常见的陷阱：gameObject == null

```csharp
// 在场景中有一个 GameObject "Cube"

void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Destroy(gameObject);  // 在帧结束时销毁
        Debug.Log(gameObject == null);  // ??? 猜猜输出什么
    }
}
```

**答案是 false！（或 true，取决于 Destroy 的执行时机）**

```
Destroy 的工作原理：
1. 调用 Destroy(gameObject)
2. Unity 没有立即删除对象，而是标记为 "pending destroy"
3. 当前帧的 Update 继续执行——对象还在内存中
4. 当前帧结束时 Unity 真正删除
5. 所以 Destroy 后的同一帧内，gameObject == null 返回 false！
```

但 Unity 重写了 `==` 运算符：
```csharp
// Unity 的 Object 类重写了 ==
// 即使对象被标记销毁，== 也会返回 true
// 而真正的 C# null 检查 (?? ReferenceEquals) 返回 false
```

**安全做法：**
```csharp
if (this == null)   // Unity 自己重写过的 null 检查
if (ReferenceEquals(this, null))  // CLR 级的 null 检查（绕过 Unity 的重写）
```

### 浮点数比较的陷阱

```csharp
float a = 0.1f + 0.2f;  // 实际可能是 0.30000001192...
if (a == 0.3f) { }      // 可能为 false！

// 二进制表示：
// 0.1 在二进制中是无限循环小数：0.00011001100110011...
// float 只有 23 位有效位，精度损失不可避免

// ✅ 正确做法：
if (Mathf.Approximately(a, 0.3f)) { }

// Mathf.Approximately 的底层：
// Mathf.Approximately(a, b) => Mathf.Abs(a - b) < Mathf.Max(1E-06f, Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)) * 1E-06f)
// 不是简单的 ==，而是允许一个很小的误差范围
```

---

## 8. var 关键字——类型推断

```csharp
var x = 10;              // 编译器推断为 int
var list = new List<int>();  // 推断为 List<int>
var result = players.Where(p => p.Level > 5).First();  // 推断为 Player
```

**重要：** `var` **不是动态类型！** 编译时就已经确定了。

```csharp
var x = 10;
x = "hello";  // ❌ 编译错误！x 已经是 int 了，不能赋值为 string
```

`var` 的 IL 层面——完全等价的：
```csharp
// var x = 10;
int x = 10;
// 两行代码生成完全相同的 IL！
```

`var` 只是为了**代码可读性**，避免写出冗长的类型名。对比 C++ 的 `auto`。

---

---

## C++/Raylib 对照总结

| 概念 | C++ | C# |
|------|-----|-----|
| 栈分配 | `int x = 5;`（局部变量） | `int x = 5;`（值类型） |
| 堆分配 | `int* p = new int(5);` + `delete` | `object o = new object();` + GC 自动回收 |
| struct | 值类型，语义同 C# | 值类型，语义同 C++ |
| class | 默认 private，手动 new/delete | 默认 public，GC 管理 |
| nullptr | `nullptr` | `null` |
| 类型转换 | `dynamic_cast` / `static_cast` | `as` / `is` / 显式转换 |
| 函数指针 | `void (*cb)(int)` | `delegate` / `Action<int>` |
| C++ auto | `auto x = 10;` | `var x = 10;` |
| 内存管理 | 手动，RAII | GC，IDisposable |
| 浮点数比较 | 手动 epsilon | `Mathf.Approximately()` |

## 停靠点

> C# 的两大类型家族：值类型（栈上存值）和引用类型（栈上存地址，堆上存数据）。
> string 是引用类型但不可变，所以"看起来"像值类型。
> boxing 是值类型最常见的性能陷阱——泛型可以避免。
> Unity 的 Vector3/Quaternion/Color 是 struct（无 GC），Transform/GameObject 是 class。
> 浮点数永远不要用 `==`，用 `Mathf.Approximately`。
> Destroy 不是立即删除——同一帧内还能访问对象。
