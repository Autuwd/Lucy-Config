# Day 4：LINQ 与集合深入 — 异步流、不可变集合与零分配迭代

## 0. 为什么需要深入集合？

Day04 讲了 List、Dictionary、LINQ 的基础用法。但大型项目中，你会发现这些集合在某些场景下不够用：

- 数据量太大要用**并行查询**（PLINQ）
- 流式数据要用**异步枚举**（IAsyncEnumerable）
- 多线程共享数据要用**不可变集合**
- 高性能场景要用**结构体枚举器**避免 GC

这一篇深入四个方向：**异步流处理**、**并行数据查询**、**内存安全的高性能集合**、**池化内存管理**。

---

## 1. IAsyncEnumerable&lt;T&gt; 和 await foreach

### 问题：普通 IEnumerable 不能等待

```csharp
// 当你从网络或数据库获取数据时，普通 IEnumerable 会阻塞线程

// ❌ 不能这样：
// IEnumerable<Texture2D> LoadTextures(List<string> urls)
// {
//     foreach (var url in urls)
//     {
//         var tex = await LoadFromNetwork(url);  // 不能在迭代器中使用 await！
//         yield return tex;
//     }
// }
```

### IAsyncEnumerable——异步版本的 IEnumerable

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

// 异步流：可以边下载边处理
public async IAsyncEnumerable<Texture2D> LoadTexturesAsync(
    List<string> urls,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    foreach (var url in urls)
    {
        // 每次 await 都会释放当前线程，不阻塞
        var tex = await LoadFromNetworkAsync(url, ct);
        yield return tex;  // yield 在 async 方法中也可以！
    }
}

// 消费端
public async AwaitableVoid UseAsyncStream()
{
    await foreach (var tex in LoadTexturesAsync(urls))
    {
        // 每下载完一个就立即处理——不需要等全部下载完
        ApplyTexture(tex);
    }
}
```

### 与协程的结合

```csharp
// Unity 中结合协程和 IAsyncEnumerable
// 场景：分帧处理大量 UI 元素

public IAsyncEnumerable<GameObject> InstantiateUIAsync(
    List<UIElementData> elements,
    MonoBehaviour coroutineRunner)
{
    return InstantiateInternal(elements, coroutineRunner);
}

private async IAsyncEnumerable<GameObject> InstantiateInternal(
    List<UIElementData> elements,
    MonoBehaviour runner)
{
    int processedThisFrame = 0;

    foreach (var data in elements)
    {
        var go = Object.Instantiate(data.Prefab);
        // 配置 UI...
        yield return go;

        processedThisFrame++;
        if (processedThisFrame >= 10)
        {
            // 每帧最多实例化 10 个，等下一帧
            await Awaitable.NextFrameAsync();
            processedThisFrame = 0;
        }
    }
}
```

### 使用 Channel&lt;T&gt; 实现异步流

```csharp
// IAsyncEnumerable 的底层数据源可以是 Channel<T>
// 生产者-消费者模式

public class TextureLoader : MonoBehaviour
{
    private Channel<Texture2D> _textureChannel
        = Channel.CreateUnbounded<Texture2D>();

    async void Start()
    {
        // 消费者：异步流式处理
        await foreach (var tex in ReadAllAsync())
        {
            ApplyTexture(tex);
        }
    }

    public async IAsyncEnumerable<Texture2D> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var tex in _textureChannel.Reader.ReadAllAsync(ct))
        {
            yield return tex;
        }
    }

    public void EnqueueTexture(Texture2D tex)
    {
        _textureChannel.Writer.TryWrite(tex);
    }
}
```

---

## 2. Parallel LINQ（PLINQ）——AsParallel()

### 什么时候用 PLINQ？

```csharp
// 普通 LINQ：单线程顺序执行
var result = data
    .Where(x => ExpensiveCheck(x))
    .Select(x => ExpensiveTransform(x))
    .ToList();

// PLINQ：多线程并行执行
var result = data
    .AsParallel()  // ← 加上这个！
    .Where(x => ExpensiveCheck(x))
    .Select(x => ExpensiveTransform(x))
    .ToList();
```

### PLINQ 的工作机制

```
序列数据              PLINQ 分区
┌───┬───┬───┐      ┌───────────┐
│ 0 │ 1 │ 2 │      │ 分区 0    │  → Thread 1
├───┼───┼───┤      ├───────────┤
│ 3 │ 4 │ 5 │  →   │ 分区 1    │  → Thread 2
├───┼───┼───┤      ├───────────┤
│ 6 │ 7 │ 8 │      │ 分区 2    │  → Thread 3
├───┼───┼───┤      ├───────────┤
│ 9 │10 │11 │      │ 分区 3    │  → Thread 4
└───┴───┴───┘      └───────────┘
                         ↓
                   结果合并（Merge）
                    ┌──────────┐
                    │ 最终结果  │
                    └──────────┘
```

### PLINQ 在 Unity 中的限制

```csharp
// Unity 主线程不能调用 Unity API 从 PLINQ！
// PLINQ 在后台线程执行

// ❌ 危险：PLINQ 中调用 Unity API
var results = data.AsParallel().Select(x =>
{
    // 主线程要求！在后台线程调用会崩溃
    // GameObject go = new GameObject();
    // return go;
});

// ✅ 安全：PLINQ 只做纯数据计算
var results = data.AsParallel().Select(x =>
{
    // 纯数学计算——完全安全
    return ProcessMathData(x);
});

// Unity 主线程拿到结果后再操作游戏对象
foreach (var r in results)
{
    ApplyToGameObject(r);
}
```

### PLINQ 的聚合操作

```csharp
// PLINQ 可以提供比普通 LINQ 更好的聚合性能

// 大规模碰撞检测
List<Collider> allColliders = GetAllColliders();

// 并行检测最近的碰撞对
var nearestPair = allColliders
    .AsParallel()
    .SelectMany((c1, i) => allColliders
        .Skip(i + 1)
        .Select(c2 => (c1, c2, Distance: Vector3.Distance(
            c1.transform.position, c2.transform.position))))
    .OrderBy(pair => pair.Distance)
    .First();

// 指定并行度——避免淹没 CPU
var controlled = data
    .AsParallel()
    .WithDegreeOfParallelism(Environment.ProcessorCount / 2)  // 只用一半核心
    .Select(ExpensiveOperation)
    .ToList();

// 处理顺序——需要排序结果
var ordered = data
    .AsParallel()
    .AsOrdered()  // 保持原始顺序（牺牲一点性能）
    .Select(x => x * 2)
    .ToList();
```

---

## 3. 不可变集合——线程安全共享数据

### 为什么需要不可变集合？

```csharp
// 多线程问题：一个线程在迭代集合，另一个线程在修改
Dictionary<string, int> scores = new Dictionary<string, int>();

// 线程 A
foreach (var kv in scores) { }  // 正在遍历

// 线程 B
scores["Alice"] = 100;  // 修改集合

// → 抛出 InvalidOperationException：集合已被修改！

// 锁定可以解决，但降低性能
lock (lockObj)
{
    scores["Alice"] = 100;
}
```

### ImmutableDictionary——不可变的哈希表

```csharp
using System.Collections.Immutable;

// 创建不可变字典——内容初始化后就不能变了
ImmutableDictionary<string, int> scores =
    ImmutableDictionary<string, int>.Empty
        .Add("Alice", 100)
        .Add("Bob", 85)
        .Add("Eve", 92);

// "修改"会返回新实例，旧实例不变
ImmutableDictionary<string, int> updated = scores.SetItem("Alice", 110);
// scores["Alice"] 仍然是 100
// updated["Alice"] 是 110

// 这种"修改复制"的内部机制：不变 AVL 树变体
// 修改时只复制被影响的节点，大部分节点共享
//
//               Root
//       ┌───────┴───────┐
//     Node A           Node B
//   ┌───┴───┐       ┌───┴───┐
//  Alice   Bob     Carol   Dave
//    ↓ 修改 Alice
//               Root' (新根)
//       ┌───────┴───────┐
//     Node A'          Node B (共享!)
//   ┌───┴───┐       ┌───┴───┐
//  Alice'  Bob     Carol   Dave
//  (新节点) (共享)
```

### ImmutableList——不变的动态数组

```csharp
// List 的可变版本——O(1) 索引，O(n) 插入
// ImmutableList——平衡树实现

ImmutableList<int> list = ImmutableList<int>.Empty;
list = list.Add(1);   // 返回新实例
list = list.Add(2);
list = list.Add(3);

// 索引访问——O(log n)
int second = list[1];  // 2

// 插入——O(log n)，比 List 的 O(n) 好
list = list.Insert(1, 99);

// 快照特性：所有版本共存
ImmutableList<int> v1 = list;
list = list.Add(4);  // list 变了，v1 不变

// 遍历——无锁安全
foreach (var item in list)
{
    // 即使其他线程同时修改 list，这里也安全
}
```

### Unity 中的不可变集合

```csharp
// 场景：游戏配置数据，运行时不需要修改
public class GameBalanceData
{
    // 不可变字典存储静态配置
    public ImmutableDictionary<int, WeaponStats> Weapons { get; }
    public ImmutableDictionary<int, EnemyStats> Enemies { get; }

    public GameBalanceData(GameBalanceConfig config)
    {
        var weaponBuilder = ImmutableDictionary.CreateBuilder<int, WeaponStats>();

        foreach (var w in config.weaponList)
        {
            weaponBuilder.Add(w.id, new WeaponStats
            {
                Name = w.name,
                Damage = w.damage,
                Range = w.range
            });
        }

        // 构建不可变版本
        Weapons = weaponBuilder.ToImmutable();
    }
}

// 场景：多线程 AI 决策读取数据
public class AISystem
{
    private ImmutableDictionary<int, float> _threatLevels;

    public void UpdateThreats(IEnumerable<Enemy> enemies)
    {
        var builder = ImmutableDictionary.CreateBuilder<int, float>();
        foreach (var e in enemies)
        {
            builder[e.Id] = CalculateThreat(e);
        }
        // 原子替换——其他线程读旧版本，安全
        _threatLevels = builder.ToImmutable();
    }

    public float GetThreat(int enemyId)
    {
        // 无锁读取——数据永不改变
        return _threatLevels.TryGetValue(enemyId, out var t) ? t : 0f;
    }
}
```

### 不可变集合的 Internal 结构

| 集合 | 内部结构 | 修改复杂度 | 空间开销 |
|------|---------|-----------|---------|
| ImmutableArray&lt;T&gt; | 普通数组 | O(n) 全量复制 | 最低 |
| ImmutableList&lt;T&gt; | AVL 树 | O(log n) | 中 |
| ImmutableDictionary&lt;K,V&gt; | 不可变哈希树 | O(log n) | 中 |
| ImmutableHashSet&lt;T&gt; | 不变哈希树 | O(log n) | 中 |
| ImmutableSortedDictionary&lt;K,V&gt; | 红黑树 | O(log n) | 中 |

---

## 4. Span 替代 LINQ——零分配查询

### 为什么需要替代 LINQ？

```csharp
// LINQ 的问题：GC 分配

int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8 };

// 每次调用下面这些方法，都可能分配内存：
var even = numbers.Where(n => n % 2 == 0);          // 分配迭代器对象
var doubled = numbers.Select(n => n * 2);           // 分配迭代器对象
var result = numbers.Where(n => n > 3).ToArray();   // 分配数组 + 迭代器

// 在 Update 中这样写会触发 GC！
```

### 使用 Span 手动实现零分配查询

```csharp
using System;
using System.Buffers;

// 零分配的筛选
public static class SpanExtensions
{
    // 筛选到提供的 Span 中——零分配！
    public static int Filter<T>(
        this ReadOnlySpan<T> source,
        Span<T> destination,
        Func<T, bool> predicate)
    {
        int writeIndex = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                destination[writeIndex++] = source[i];
            }
        }
        return writeIndex;  // 返回实际写入的元素数量
    }

    // 映射到 Span——零分配！
    public static int Map<TIn, TOut>(
        this ReadOnlySpan<TIn> source,
        Span<TOut> destination,
        Func<TIn, TOut> transform)
    {
        for (int i = 0; i < source.Length; i++)
        {
            destination[i] = transform(source[i]);
        }
        return source.Length;
    }
}

// 使用：
void ProcessFrame(ReadOnlySpan<int> frameData, Span<int> output)
{
    // 零分配！所有内存预先分配好
    int count = frameData.Filter(output, x => x > 0);
    output.Slice(0, count).Sort();
}
```

### System.Memory 命名空间的辅助方法

```csharp
using System.Memory;

// .NET 提供了 MemoryExtensions 作为 Span 的 LINQ
ReadOnlySpan<char> text = "hello world";

// 这些操作是零分配的：
int index = text.IndexOf('o');              // O(n)
int last = text.LastIndexOf('l');           // O(n)
bool startsWith = text.StartsWith("hello"); // O(n)
ReadOnlySpan<char> trimmed = text.Trim();   // 零分配！返回新 Span
ReadOnlySpan<char> upper = text.ToString().ToUpper().AsSpan(); // 要分配...

// 大小写比较——零分配
bool equal = text.Equals("HELLO WORLD", StringComparison.OrdinalIgnoreCase);
```

### 自定义结构的 LINQ 替代

```csharp
// 用 struct 枚举器实现零分配 LINQ
// 避免 foreach 在引用类型集合上的分配

public ref struct SpanEnumerator<T>
{
    private readonly ReadOnlySpan<T> _span;
    private int _index;

    public SpanEnumerator(ReadOnlySpan<T> span)
    {
        _span = span;
        _index = -1;
    }

    public T Current => _span[_index];
    public bool MoveNext() => ++_index < _span.Length;
    public void Reset() => _index = -1;
    public SpanEnumerator<T> GetEnumerator() => this;  // 支持 foreach
}

// 使用——foreach 在 ref struct 上，零 GC！
void ProcessSpan(ReadOnlySpan<int> data)
{
    var enumerator = new SpanEnumerator<int>(data);
    foreach (var item in enumerator)
    {
        // 处理 item
    }
}
```

---

## 5. ArrayPool&lt;T&gt; 和 MemoryPool&lt;T&gt;

### 为什么需要数组池？

```csharp
// 临时缓冲区——传统做法每次都 new

void ProcessFrame(byte[] rawData)
{
    byte[] buffer = new byte[4096];  // 每次调用都分配！
    // 处理数据...
    // 方法结束 → buffer 变成垃圾 → GC 压力
}
```

### ArrayPool&lt;T&gt;——数组复用

```csharp
using System.Buffers;

// 共享数组池——单例
ArrayPool<byte> pool = ArrayPool<byte>.Shared;

// 借一个数组
byte[] buffer = pool.Rent(4096);
// pool.Rent(minSize) 返回的数组长度可能 >= 4096

try
{
    // 使用 buffer
    ProcessData(buffer);

    // 注意：只使用 buffer[0..requiredLength]
    // 返回的数组可能比要求的大
}
finally
{
    // 归还数组——可以被复用
    pool.Return(buffer);
    // 归还后不要再使用 buffer！
    // 建议：buffer = null;
}

// 整个操作零 GC 分配！
```

### 内存池的底层机制

```
ArrayPool.Shared 的内部（简化）：

┌──────────────────────────────────────────────┐
│ ArrayPool<T> 内部维护多个桶（buckets）        │
│                                               │
│ 桶 [0]: 长度 16 的数组集合                    │
│ 桶 [1]: 长度 32 的数组集合                    │
│ 桶 [2]: 长度 64 的数组集合                    │
│ ...                                           │
│ 桶 [n]: 长度 2^(n+4) 的数组集合               │
│                                               │
│ Rent(100) → 找到 128 的桶，借出或新建         │
│ Return(arr) → 把数组放回对应桶                 │
│                                               │
│ 注意：归还的数组不会立即清除                   │
│ 如果你在归还后继续读——可能读到之前的数据！     │
└──────────────────────────────────────────────┘
```

### MemoryPool&lt;T&gt;——Memory 版本的池

```csharp
// MemoryPool 管理 Memory<T> 的池化
MemoryPool<byte> memPool = MemoryPool<byte>.Shared;

using (IMemoryOwner<byte> owner = memPool.Rent(4096))
{
    Memory<byte> memory = owner.Memory;

    // 使用 memory
    await ProcessDataAsync(memory);

    // IMemoryOwner 实现了 IDisposable
    // using 块结束时自动归还
}
```

### Unity 中的 ArrayPool 实践

```csharp
// 每帧物理拾取——零分配
void Update()
{
    // 借用存储 RaycastHit 的数组
    // 可能同时有最多 128 个碰撞
    RaycastHit[] hits = ArrayPool<RaycastHit>.Shared.Rent(128);

    try
    {
        int count = Physics.RaycastNonAlloc(ray, hits, 100f);

        for (int i = 0; i < count; i++)
        {
            ProcessHit(hits[i]);
        }
    }
    finally
    {
        ArrayPool<RaycastHit>.Shared.Return(hits);
    }
}
```

---

## 6. Frozen Collections（.NET 8）——只读集合的极致性能

### FrozenDictionary——比 ImmutableDictionary 更快

```csharp
using System.Collections.Frozen;

// FrozenDictionary：创建后只读，优化查找性能
// 适合"配置数据"、"查找表"等场景

// 创建
Dictionary<string, int> source = new()
{
    ["Alice"] = 100,
    ["Bob"] = 85,
    ["Eve"] = 92
};

// 冻结——转换为优化查找的只读字典
FrozenDictionary<string, int> frozen = source.ToFrozenDictionary();

// 查找——比普通 Dictionary 还快！
int score = frozen["Alice"];  // O(1)，且内联优化更好

// 创建时的优化：
// - 小的字典（<10 项）：用线性扫描（branchless）
// - 大的字典：用完美哈希或确定性哈希
```

### FrozenSet

```csharp
HashSet<string> tags = new()
{
    "Player", "Enemy", "Bullet", "Pickup"
};

// 冻结——运行时只读
FrozenSet<string> frozenTags = tags.ToFrozenSet();

// Contains 检查——极快
bool isPlayer = frozenTags.Contains("Player");

// 用 FrozenSet 做标签查询
public class TagQuery : MonoBehaviour
{
    private static readonly FrozenSet<string> EnemyTags =
        new HashSet<string> { "Enemy", "Boss", "Minion" }.ToFrozenSet();

    void OnTriggerEnter(Collider other)
    {
        if (EnemyTags.Contains(other.tag))
        {
            // 非常快的标签检查
        }
    }
}
```

### Frozen vs Immutable vs Regular

| | Dictionary | ImmutableDictionary | FrozenDictionary |
|--|-----------|-------------------|----------------|
| 可变 | 可 | 不可（修改=新实例） | 不可（创建后锁定） |
| 查找速度 | 快 | 略慢（树结构） | 极快（优化过） |
| 创建开销 | 低 | 中 | 较高（优化需要时间） |
| 内存 | 标准 | 较高 | 最低（紧凑布局） |
| 线程安全 | 否 | 是 | 是 |
| 最佳场景 | 运行时数据 | 需要快照 | 只读查找表 |

---

## 7. 自定义枚举器结构体——避免 GC

### 传统 foreach 的 GC 问题

```csharp
// 当你 foreach 一个类时：
// List<T>.Enumerator 是 struct
// 但如果被装箱（比如非泛型 IEnumerable），就会 GC！

// 问题 1：非泛型集合
ArrayList list = new ArrayList { 1, 2, 3 };
foreach (int i in list)
{
    // ArrayList.GetEnumerator() 返回 IEnumerator（引用类型）
    // 每次 foreach 都分配！
}

// 问题 2：值类型枚举器被 IEnumerator<T> 接口引用
List<int> nums = new List<int> { 1, 2, 3 };
IEnumerator<int> e = nums.GetEnumerator();  // struct 被装箱！
// 用 List<int>.Enumerator 直接使用——零分配
List<int>.Enumerator e2 = nums.GetEnumerator();  // 仍然是 struct
```

### 编写自己的 Struct 枚举器

```csharp
// 实现值类型的枚举器——零 GC 分配

public struct PlayerCollection : IEnumerable<Player>
{
    private Player[] _players;

    public PlayerCollection(Player[] players)
    {
        _players = players;
    }

    // 返回结构体枚举器——零分配
    public Enumerator GetEnumerator() => new Enumerator(_players);

    // 显式接口实现——会装箱！但 foreach 不会走这里
    IEnumerator<Player> IEnumerable<Player>.GetEnumerator()
        => GetEnumerator();  // 这里会装箱 Enumerator

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    // 值类型的枚举器
    public struct Enumerator : IEnumerator<Player>
    {
        private readonly Player[] _players;
        private int _index;

        internal Enumerator(Player[] players)
        {
            _players = players;
            _index = -1;
        }

        public Player Current => _players[_index];

        // 显式接口实现——避免在 foreach 时触发
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < _players.Length;
        }

        public void Reset() => _index = -1;
        public void Dispose() { }
    }
}

// 使用：
PlayerCollection players = new PlayerCollection(allPlayers);
foreach (var p in players)
{
    // 编译器看到 public Enumerator GetEnumerator()
    // 直接使用结构体枚举器——零 GC 分配！
}
```

### Unity 中的 struct 枚举器

```csharp
// Unity 的常用集合已经做了这个优化
// List<T>.Enumerator, Dictionary<K,V>.Enumerator 都是 struct

// foreach List 是零分配的：
List<int> list = new List<int> { 1, 2, 3 };
foreach (int i in list)
{
    // 编译器展开为：
    // List<int>.Enumerator e = list.GetEnumerator();
    // while (e.MoveNext()) { int i = e.Current; ... }
    // e.Dispose();
    // 全程无 GC 分配！
}

// 但如果你用接口类型声明变量：
IEnumerable<int> enumerable = list;
foreach (int i in enumerable)
{
    // 这里会调用 IEnumerator<int> GetEnumerator() → 装箱！
    // 所以：尽量用具体类型，不要用接口
}
```

---

## C++/Raylib 对照总结

| 概念 | C++ | C# |
|------|-----|-----|
| 异步枚举 | `co_yield` / `generator<T>` | `IAsyncEnumerable<T>` + `await foreach` |
| 并行处理 | `std::execution::par` | PLINQ `AsParallel()` |
| 不可变数据 | `const` + 手写 COW | `ImmutableDictionary` / `ImmutableList` |
| 零分配查询 | 手写循环 + 指针 | `Span<T>` + struct 枚举器 |
| 内存池 | 自定义内存池 | `ArrayPool<T>` / `MemoryPool<T>` |
| 只读查找表 | `const` 数组 + 排序 | `FrozenDictionary` / `FrozenSet` |
| 迭代器 | 模板迭代器（零开销） | struct 枚举器（可零分配） |

## 停靠点

> IAsyncEnumerable&lt;T&gt; 让 foreach 可以 await——边下载边处理。Channel&lt;T&gt; 是它的底层实现机制。
> PLINQ 用 `AsParallel()` 让查询并行执行。Unity 中只能做纯数据计算，不能调用 Unity API。
> 不可变集合（ImmutableDictionary、ImmutableList）修改时返回新实例，旧实例不变。适合多线程共享数据。
> Span&lt;T&gt; 可以替代 LINQ 做零分配的数组操作。配合 struct 枚举器可以做到 foreach 零 GC。
> ArrayPool&lt;T&gt; 复用临时数组，避免频繁分配释放。在 Update 中处理临时数据时非常有用。
> FrozenDictionary（.NET 8）是只读字典的最优化实现，查找速度比普通 Dictionary 更快。
> 自定义 struct 枚举器让 foreach 零 GC——编译器优先选择 public GetEnumerator()，不经过接口装箱。
