# Day 1：类型系统深入 — 零分配内存操作与 GC 核心机制

## 0. 为什么需要深入类型系统？

Day01 讲了值类型和引用类型的基本划分。但实际 C# 开发中，真正影响性能的是**内存分配的细节**。你写 `string.Substring()`，每次调用都在堆上分配一个新字符串。这在 GC 压力敏感的游戏中是致命的。

这一篇深入四个方向：**零分配内存操作**（Span/Memory）、**栈上数据安全**（ref struct/stackalloc）、**GC 内部原理**（代龄回收的底层工作机制）、**对象生命周期的弱引用管理**。

---

## 1. Span&lt;T&gt; / ReadOnlySpan&lt;T&gt; / Memory&lt;T&gt;——零分配切片

### 传统问题：字符串/数组切片 = 堆分配

```csharp
string full = "Hello, Unity World!";
string part = full.Substring(7, 5);  // "Unity"
// 底层：堆上分配了新字符串！复制了 5 个字符
```

每当你从字符串、数组或 List 中"取一部分"，传统做法都会**分配新内存**。这在 C++ 中你不会接受（一个指针 + 长度就搞定了），在 C# 中同样不应该接受。

### Span&lt;T&gt;——栈上的"指针+长度"

```csharp
Span<int> numbers = stackalloc int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
Span<int> slice = numbers.Slice(2, 3);  // 零分配！{3, 4, 5}

slice[0] = 99;  // 修改 slice 也会影响原始数据
Console.WriteLine(numbers[2]);  // 输出 99
```

内存布局：
```
               numbers (Span, 栈上 8 字节)
               ┌──────────────────────┐
               │ ptr → 栈上的数组数据  │
               │ length = 8           │
               └──────────────────────┘
                      │
               Slice(2, 3)
                      ↓
               slice (Span, 栈上 8 字节)
               ┌──────────────────────┐
               │ ptr → numbers[2]     │  ← 指向原始数组的第 3 个元素
               │ length = 3           │
               └──────────────────────┘
```

**关键：** Span 是一个**栈上结构体**，只包含一个指针和一个长度。`Slice()` 只是调整指针和长度的值——**没有内存分配**。

### Span&lt;T&gt; 的约束——为什么只能存在于栈上？

```csharp
// ❌ Span 不能作为类的字段
public class Player
{
    private Span<int> _data;  // 编译错误！Span 是 ref struct
}

// ❌ Span 不能装箱
Span<int> s = stackalloc int[10];
object o = s;  // 编译错误！

// ❌ Span 不能在 Lambda 中捕获
int[] arr = { 1, 2, 3 };
Action a = () =>
{
    Span<int> s = arr;  // 编译错误！
};
```

这些限制的原因是 `ref struct` 的**逃逸分析**（Escape Analysis）——CLR 必须保证 ref struct 永远不会逃逸到堆上。一旦在堆上，ref struct 内部的引用就可能指向已回收的栈内存。

### ReadOnlySpan&lt;T&gt;——只读切片

```csharp
ReadOnlySpan<char> text = "Hello Unity".AsSpan();
ReadOnlySpan<char> engine = text.Slice(6, 5);  // "Unity"
// 不能修改：engine[0] = 'X';  // 编译错误
```

Unity 中的典型用法——解析 JSON 或 CSV 而不分配字符串：

```csharp
// 传统方式：每段解析都分配子串
string csvData = "100,Warrior,Alice";
string[] fields = csvData.Split(',');  // 分配 3 个新字符串！
int id = int.Parse(fields[0]);         // 又一个分配

// 零分配方式
ReadOnlySpan<char> data = "100,Warrior,Alice".AsSpan();
int comma1 = data.IndexOf(',');
int id2 = int.Parse(data.Slice(0, comma1));  // 不分配堆内存！
```

### Memory&lt;T&gt;——Span 的"堆友好"版本

```csharp
// Memory<T> 是普通 struct，可以存在于堆上
public class NetworkBuffer
{
    public Memory<byte> Buffer { get; set; }  // 没问题，Memory 不是 ref struct
}

Memory<byte> mem = new byte[1024];
// Memory 可以转 Span：
Span<byte> span = mem.Span;  // 获取安全的 Span 视图
```

| 特性 | Span&lt;T&gt; | Memory&lt;T&gt; |
|------|-------------|----------------|
| 类型 | ref struct | struct |
| 堆上存储 | 不能 | 可以 |
| 性能 | 最高（纯栈） | 略低（需要pin） |
| 使用场景 | 同步处理，栈局部 | 异步处理，作为字段 |
| 底层 | 托管指针 + 长度 | object + offset + 长度 |

### 对比 C++

| 概念 | C++ | C# |
|------|-----|-----|
| 指针+长度视图 | `std::string_view` / `gsl::span` | `ReadOnlySpan<char>` / `Span<T>` |
| 栈数组 | `int arr[5]` | `Span<int> s = stackalloc int[5]` |
| 偏移访问 | `ptr + offset` | `span.Slice(offset, length)` |
| 堆数据引用 | `std::string_view` 可指向堆 | `Memory<T>` 指向堆数据 |
| 生命周期保证 | 程序员手动管理 | 编译器逃逸分析 |

---

## 2. ref struct vs 普通 struct——逃逸分析

### ref struct 的限制来源

`ref struct` 是 C# 7.2 引入的。基本规则：ref struct 的实例**只能存在于栈上**。

```csharp
// 合法的 ref struct
ref struct Matrix3x3
{
    public float M11, M12, M13;
    public float M21, M22, M23;
    public float M31, M32, M33;
}

// 可以：栈上局部变量
Matrix3x3 mat;
mat.M11 = 1f;

// 可以：方法参数
void Process(ref Matrix3x3 m) { }

// 可以：方法返回值（编译器检查不逃逸到堆）
ref struct Result { }
Result Compute() { return new Result(); }

// 不能：类的字段
class Wrapper { Matrix3x3 _data; }  // 编译错误！

// 不能：数组元素
Matrix3x3[] arr = new Matrix3x3[10];  // 编译错误！

// 不能：类型参数
List<Matrix3x3> list = new List<Matrix3x3>();  // 编译错误！
```

### 为什么 Unity 没有大量使用 ref struct？

Unity 的很多结构（Vector3, Quaternion, Matrix4x4）是**普通 struct**。原因：

- 普通 struct 可以做类的字段、可以做数组元素
- ref struct 的限制太多，不适合泛型容器
- Unity 的 API 历史比 ref struct 早得多

```csharp
// Unity 的 Vector3 是普通 struct：
// 可以：作为 Transform 的字段
public class Transform
{
    private Vector3 m_LocalPosition;  // 没问题
}

// 如果 Vector3 是 ref struct：
// Vector3[] positions = new Vector3[10];  // 不行！
// List<Vector3> posList = new List<Vector3>();  // 不行！
```

### 什么时候该用 ref struct？

```csharp
// ✅ 好的场景：临时缓冲区
ref struct BufferWriter
{
    private Span<byte> _buffer;
    private int _position;

    public void Write(int value)
    {
        // 直接操作栈上缓冲区，零分配
        Span<byte> slice = _buffer.Slice(_position);
        // 写入逻辑...
        _position += 4;
    }
}

// ✅ 好的场景：高性能解析器
ref struct JsonParser
{
    private ReadOnlySpan<char> _input;
    private int _index;

    public int ReadInt() { /* 零分配解析 */ }
}
```

---

## 3. stackalloc——栈上分配数组

### 基本用法

```csharp
// 在栈上分配 int 数组（等价于 C++ int arr[100]）
Span<int> buffer = stackalloc int[100];
// 不会触发 GC，自动回收

// 用 stackalloc 处理临时数据
int sum = 0;
Span<int> temps = stackalloc int[] { 1, 2, 3, 4, 5 };
foreach (var t in temps) sum += t;
```

### stackalloc 的限制

```csharp
// 限制 1：只能在 unsafe 或使用 Span 的上下文中
int* ptr = stackalloc int[10];  // 需要 unsafe 上下文

// 推荐方式（无需 unsafe）：
Span<int> safe = stackalloc int[10];  // span 包装

// 限制 2：栈空间有限（默认 1MB）
// ❌ 危险：尝试分配 100 万个 int（4MB），超出栈容量
Span<int> tooBig = stackalloc int[1_000_000];  // StackOverflowException!

// ✅ 安全：小批量使用
Span<int> fine = stackalloc int[256];  // 1KB，完全安全

// 限制 3：不能返回 stackalloc 的 Span（会逃逸）
Span<int> BadAlloc()
{
    return stackalloc int[10];  // 编译错误！返回后栈数据被销毁
}

// ✅ 正确：通过 ref 参数传出
void GoodAlloc(out Span<int> buffer)
{
    buffer = stackalloc int[10];  // 调用者负责确保不逃逸
}
```

### Unity 中的 stackalloc 实践

```csharp
// 在 Update 中处理临时数据——零分配！
void Update()
{
    // 需要 4 个临时 RaycastHit 来做批处理
    Span<RaycastHit> hits = stackalloc RaycastHit[4];
    int count = Physics.RaycastNonAlloc(ray, hits);

    for (int i = 0; i < count; i++)
    {
        ProcessHit(hits[i]);
    }
    // 方法结束 → 栈空间自动回收，零 GC！
}
```

### 对比 C++

```cpp
// C++：栈上数组
int buffer[256];             // 栈上 1KB
int* dynamic = new int[256]; // 堆上

// C#：
Span<int> buffer = stackalloc int[256];  // 栈上（等价于 C++ int buffer[256]）
int[] dynamic = new int[256];            // 堆上（等价于 C++ new int[256] + delete[])
```

---

## 4. GC 代龄回收内部机制——Gen0/Gen1/Gen2 的底层工作

### 堆的物理分段

Day01 介绍了代龄的概念，这里深入挖掘**每代回收的具体流程**。

```
托管堆布局（非 LOH 部分，简化为一个连续段）：

Gen0 空间（~16MB，现代 .NET）：
┌──────────────────────────────────────────────────┐
│ 新分配的对象  │ 新分配的对象  │        │            │
│ (第一次 GC 前)  │  (未满)       │  空闲   │    空闲     │
└──────────────────────────────────────────────────┘
↑                                                  ↑
分配指针 (alloc ptr)                            Gen0 边界

当分配指针到达 Gen0 边界 → GC 触发！
```

### Gen0 GC 流程

```
触发条件：Gen0 空间已满（最常见）

步骤：
1. 暂停所有线程（Stop The World）
   ↓
2. 标记阶段（Mark Phase）
   - 从 GC 根出发：栈引用、静态字段、GC Handle、finalize 队列
   - 广度优先遍历所有可达对象
   - 用 Mark Bitmap（每个对象 1 bit）标记存活对象
   ↓
3. 计算迁移地址（Plan Phase）
   - 遍历 Gen0 对象，决定哪些存活对象要移到 Gen1
   - 计算每个对象在压缩后的新地址
   ↓
4. 重定位（Relocate Phase）
   - 更新所有引用指向新地址
   - 遍历所有存活对象，修正其内部的引用字段
   ↓
5. 压缩（Compact Phase）
   - 按新地址复制对象，将存活对象移动到 Gen1 区域
   - 移动空闲空间到 Gen0 区域
   ↓
6. 恢复线程执行

耗时分布（典型）：
  Mark:    40%  ← 最费时，要遍历所有存活对象
  Plan:    10%
  Relocate: 20%
  Compact:  30%
```

### Gen1 GC——Gen0 已经不够了

```
触发条件：Gen0 满 + Gen1 在之前 GC 中也积累了垃圾

Gen1 GC = Gen0 GC + Gen1 GC

Gen1 GC 比 Gen0 慢的原因：
1. Gen1 空间更大（~128MB），需要标记更多对象
2. Gen1 的对象可能引用 Gen2 的对象——必须扫描 Gen2 的部分引用
3. 卡表（Card Table）优化：GC 不需要扫描整个 Gen2
```

### 卡表（Card Table）——让 Gen2 不成为瓶颈

```
Gen0/Gen1 GC 时需要知道：Gen2 中哪些对象引用了 Gen0/Gen1？
如果扫整个 Gen2 → 代价太大！

优化：卡表 (Card Table)
┌──────────────────────────────────────┐
│ Gen2 内存段分成 1024 字节的卡片      │
├──────────────────────────────────────┤
│ Card 0 │ Card 1 │ Card 2 │ ...       │
│ [0]    │ [1]    │ [0]    │           │
│ 脏标记: 卡片内有对象引用了 Gen0/Gen1 │
└──────────────────────────────────────┘

当代码写：
gen2Obj.someField = gen0Obj;  // 写入时，JIT 自动标记对应卡片为脏

GC 时只需要扫描标记为"脏"的卡片——而不是整个 Gen2！
```

### Gen2 GC——最昂贵的回收

```
触发条件：
1. Gen1 GC 后仍然内存不足
2. 大对象堆（LOH）满
3. 系统内存压力
4. GC.Collect() 显式调用

Gen2 GC = 全量回收整个托管堆

特点：
- 暂停时间最长（可能数百毫秒）
- 压缩 Gen0 + Gen1 + Gen2（LOH 不压缩）
- 频率很低（服务器应用可能数小时一次）
- Unity 游戏中尽量避免触发 Gen2 GC
```

### Unity 中的 GC 触发分析

```csharp
// 不同类型的分配对 GC 的影响：

// Gen0 分配：最轻
byte[] small = new byte[100];
// 100 字节 → Gen0
// 即使回收也只暂停 <1ms

// LOH 分配：触发 Gen2
byte[] big = new byte[100_000];  // >85KB，进入 LOH
// LOH 满了 → 触发 Gen2 GC，暂停数十到数百毫秒

// 对象池的作用就是减少 GC 触发
public class Bullet
{
    // 每实例化一颗子弹就是一次 Gen0 分配
    // 如果频繁创建/销毁 → Gen0 快速填满 → 频繁 GC
}

// 优化后的对象池版本：
ObjectPool<Bullet> pool = new ObjectPool<Bullet>();
Bullet b = pool.Get();  // 从池中取，不分配
b.Shoot();
pool.Release(b);        // 归还池，不产生垃圾
```

---

## 5. GC 延迟模式——在暂停和吞吐量之间平衡

### 三种模式

```csharp
// .NET 提供三种 GC 延迟模式
// 只能对 Server GC 或 Workstation GC 的 Background GC 模式设置

// 模式 1：默认（Interactive）
// - 平衡响应时间和吞吐量
// - 适合大多数应用，包括 Unity 编辑器

// 模式 2：LowLatency（低延迟）
// - 抑制 Gen2 GC，只做 Gen0/Gen1
// - 适合短时性能关键的操作
GC latencyMode = GCLatencyMode.LowLatency;
// - 如果内存压力过大，CLR 可能自动升级回 Interactive

// 模式 3：SustainedLowLatility（持续低延迟）
// - 更激进地抑制 Gen2 GC
// - 适合长时间低延迟需求（如游戏关键帧）
// .NET Framework 专属
```

### Unity 中的实际应用

```csharp
// Unity 在加载场景时使用
// 因为加载时产生大量垃圾，触发 GC 会导致卡顿

void OnLoadingScreen()
{
    // 进入加载模式——允许更频繁的 GC（反正用户在看加载画面）
    // 释放压力，为游戏运行做准备
    GC.Collect(2, GCCollectionMode.Optimized);
}

void OnCriticalGameplay()
{
    // 即将进入战斗——尝试降低 GC 干扰
    // 注意：Net 下才能设置延迟模式
    try
    {
        System.Runtime.GCSettings.LatencyMode =
            GCLatencyMode.LowLatency;

        // 战斗逻辑...
        PerformBossFight();

        // 战斗结束后恢复
        System.Runtime.GCSettings.LatencyMode =
            GCLatencyMode.Interactive;

        // 战斗结束后做一次回收
        GC.Collect();
    }
    catch (InvalidOperationException)
    {
        // 某些 GC 配置不支持切换模式
        Debug.LogWarning("Cannot change GC latency mode");
    }
}
```

### 手动触发 GC 的时机

```csharp
// 知道什么时候触发 GC 比手动触发更重要

// ❌ 坏：每帧检查并触发
void Update()
{
    if (Time.frameCount % 60 == 0)
        GC.Collect();  // 每 60 帧触发一次 GC！卡顿！
}

// ✅ 较好：场景切换后
void OnSceneUnloaded(Scene scene)
{
    // 场景卸载后，大量对象变成垃圾
    GC.Collect();
}

// ✅ 最好：让 GC 自己管理
// 大多数时候不需要手动 GC.Collect()
```

---

## 6. WeakReference 和 ConditionalWeakTable

### WeakReference——不阻止 GC 回收的引用

普通引用（强引用）阻止 GC 回收。WeakReference 让你引用对象但不阻止回收。

```csharp
public class TextureCache
{
    // 弱引用缓存——缓存内容不会阻止 GC
    private Dictionary<string, WeakReference<Texture2D>> _cache
        = new Dictionary<string, WeakReference<Texture2D>>();

    public Texture2D GetTexture(string path)
    {
        if (_cache.TryGetValue(path, out WeakReference<Texture2D> wr))
        {
            // 尝试获取目标——可能已被 GC 回收
            if (wr.TryGetTarget(out Texture2D tex))
                return tex;  // 缓存命中
            else
                _cache.Remove(path);  // 已被回收，移除条目
        }
        return null;
    }

    public void CacheTexture(string path, Texture2D tex)
    {
        _cache[path] = new WeakReference<Texture2D>(tex);
    }
}
```

**什么时候用？**
- 缓存大对象（Texture、Mesh），系统内存不足时可以自动释放
- 实现"可选缓存"——有缓存就快，没有也能工作
- 避免由于缓存导致的长期内存占用

### ConditionalWeakTable——为对象附加数据

```csharp
// 普通 Dictionary：key 存活时阻止 GC 回收
Dictionary<object, string> dict = new Dictionary<object, string>();
object key = new object();
dict[key] = "data";
// 即使 key 不再被使用，dict 持有强引用 → 不能回收

// ConditionalWeakTable：key 被回收时自动移除条目
ConditionalWeakTable<object, string> table
    = new ConditionalWeakTable<object, string>();

table.Add(key, "extra data");
// key 被回收后 → 条目自动移除！
```

Unity 中的实际应用——为无法修改的类附加数据：

```csharp
// 假设 Transform 是 Unity 内置类，不能加字段
// 但你想为每个 Transform 关联一个额外数据

public static class TransformExtensions
{
    private static ConditionalWeakTable<Transform, TransformData>
        _extraData = new ConditionalWeakTable<Transform, TransformData>();

    public static TransformData GetOrCreateData(this Transform t)
    {
        return _extraData.GetOrCreateValue(t);
    }
}

public class TransformData
{
    public Vector3 OriginalPosition;
    public string CustomTag;
    public float SpeedMultiplier;
}

// 使用：
Transform t = GetComponent<Transform>();
TransformData data = t.GetOrCreateData();
data.OriginalPosition = t.position;
// Transform 销毁后，TransformData 自动被回收
```

### WeakReference vs ConditionalWeakTable

| | WeakReference | ConditionalWeakTable |
|--|-------------|---------------------|
| 关系 | 单向弱引用 | 双向弱关联 |
| 生命周期 | 目标被回收，WeakReference 存活 | key 被回收，value 自动移除 |
| 用途 | 缓存大对象 | 为已有对象附加数据 |
| 性能 | 略高 | 略低（线程安全） |

---

## 7. 引用程序集 vs 实现程序集

### 概念

```csharp
// 当你引用一个 DLL 时，你实际上引用的是它的引用程序集（Reference Assembly）
// 引用程序集只包含元数据（API 签名），不包含 IL 代码

// 运行时加载的是实现程序集（Implementation Assembly）
// 实现程序集包含实际的 IL 代码

// 为什么这么做？
// 1. 编译更快（不需要加载全部 IL）
// 2. 不同平台可以有不同实现（.NET Standard vs .NET 6）
// 3. Unity 的 API 兼容层使用这个机制
```

### Unity 中的应用

```csharp
// Unity 编辑器中的引用程序集：

// UnityEngine.dll（引用程序集）→ 编译时使用
//   - 只有 API 签名
//   - 在所有平台编译时都引用同一个
//
// UnityEngine.dll（实现程序集）→ 运行时使用
//   - 包含实际 IL 代码
//   - 不同的平台有不同版本

// 这就是为什么 Unity 中有些 API 编译可以通过但运行时报错
// 因为你编译时用的是引用程序集，运行时用的是实现程序集

// 例子：
// 编译时：可以写 UnityEditor 命名空间的代码
// 运行时：在 Build 版本中 UnityEditor 不存在 → MissingMethodException
```

### 引用程序集对性能的影响

```csharp
// 引用程序集只包含 public 类型
// 所以 internal 方法在引用程序集中不存在

// 这意味着：
// - 编译时无法访问 internal 成员（理所当然）
// - 通过反射可以调用 internal 方法（运行时加载实现程序集）

// 性能影响：
// - 引用程序集更小，加载更快
// - 编译器只需要解析元数据，不需要加载 IL
// - 大型项目中编译速度可以提升 30-50%
```

---

## C++/Raylib 对照总结

| 概念 | C++ | C# |
|------|-----|-----|
| 栈切片 | `std::string_view` | `ReadOnlySpan<char>` |
| 栈数组 | `int arr[256]` | `Span<int> s = stackalloc int[256]` |
| 堆引用视图 | 原始指针 | `Memory<T>` |
| 弱引用 | `std::weak_ptr` | `WeakReference<T>` |
| 附加数据 | 侵入式字段 | `ConditionalWeakTable` |
| 内存管理 | new/delete | GC 代龄回收 |
| 引用程序集 | 头文件 + .lib | 引用程序集 + 实现程序集 |

## 停靠点

> Span&lt;T&gt; 是栈上的"指针+长度"结构，Slice 零分配。ref struct 不能逃逸到堆上，这是编译器强制保证的。
> stackalloc 在栈上分配数组，适合临时小缓存，默认 1MB 栈空间限制要记住。
> GC 代龄回收：Gen0 回收最快最频繁（<1ms），Gen2 最慢（数百 ms）。卡表优化让 Gen2 不成为 Gen0/Gen1 的瓶颈。
> GC 延迟模式可以在关键时刻抑制 Gen2 GC 来避免卡顿。
> WeakReference 不阻止 GC 回收，适合缓存大对象。ConditionalWeakTable 自动清理 key 被回收的条目。
> 引用程序集只含 API 签名不含 IL，是 Unity 跨平台编译的基础机制。
