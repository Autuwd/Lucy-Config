# Day 4：LINQ 与集合 — 从数组到声明式查询

## 0. 为什么需要集合类？

C++ 有 STL（标准模板库），C# 有 **System.Collections.Generic**——两者解决同样的问题：管理一组对象。

在 Raylib/C++ 中，你可能用数组或 `std::vector` 管理对象。C# 提供了更丰富的集合类型，而 LINQ 让你用类似 SQL 的方式查询它们。

---

## 1. 集合的接口层级

C# 集合的接口体系（从基础到具体）：

```
IEnumerable<T>          ← 核心！只要能 foreach 遍历就实现这个
    │
ICollection<T>          ← 添加、删除、计数
    │
    ├── IList<T>        ← 有序、索引访问
    │       └── List<T>
    │
    └── IDictionary<K,V> ← 键值对
            └── Dictionary<K,V>
```

### IEnumerable<T>——一切集合的基石

```csharp
// IEnumerable<T> 只要求一件事：能遍历
public interface IEnumerable<out T>
{
    IEnumerator<T> GetEnumerator();
}

// IEnumerator<T> 定义遍历状态
public interface IEnumerator<T>
{
    T Current { get; }           // 当前元素
    bool MoveNext();             // 移动到下一个
    void Reset();                // 重置到开始
    void Dispose();              // 释放资源
}
```

**foreach 的 IL 层面：**

```csharp
foreach (var item in collection)
{
    Console.WriteLine(item);
}
```

编译器展开为：
```csharp
IEnumerator<int> enumerator = collection.GetEnumerator();
try
{
    while (enumerator.MoveNext())
    {
        int item = enumerator.Current;
        Console.WriteLine(item);
    }
}
finally
{
    enumerator?.Dispose();  // 确保释放！
}
```

所以任何实现了 `IEnumerable<T>` 的类型都能用 `foreach` 遍历。

---

## 2. List<T>——动态数组

### 内部实现

`List<T>` 内部是一个 **动态数组**（dynamic array），和 C++ 的 `std::vector` 完全一样。

```
List<int> list = new List<int>();
内部状态：
┌──────────────────────┐
│ _items: T[]         │ ← 内部数组（初始容量 4）
│ _size: 0            │ ← 当前元素数量
│ _version: 0         │ ← 版本号（用于检测遍历时修改）
└──────────────────────┘

内存：
_items → [0][0][0][0]  ← 4 个元素的空间
_size = 0                ← 还没有元素
```

### 添加元素

```csharp
list.Add(1);
// _items = [1][0][0][0], _size = 1

list.Add(2);
// _items = [1][2][0][0], _size = 2

list.Add(3); list.Add(4);
// _items = [1][2][3][4], _size = 4

list.Add(5);
// 容量不够！触发扩容：
// 1. 创建新数组，容量翻倍（4 → 8）
// 2. 复制旧元素到新数组
// 3. 释放旧数组
// _items = [1][2][3][4][5][0][0][0], _size = 5
```

### 扩容策略

```csharp
// List<T> 的扩容逻辑（简化版）：
private void EnsureCapacity(int min)
{
    if (_items.Length < min)
    {
        int newCapacity = _items.Length * 2;  // 翻倍！
        if (newCapacity < min) newCapacity = min;
        T[] newItems = new T[newCapacity];
        Array.Copy(_items, newItems, _size);  // 复制
        _items = newItems;
    }
}
```

**时间复杂度：**
| 操作 | 时间复杂度 | 说明 |
|------|-----------|------|
| Add（末尾） | O(1) 均摊 | 大多数情况直接赋值，偶尔扩容 |
| Insert（中间） | O(n) | 需要后移元素 |
| Remove（中间） | O(n) | 需要前移元素 |
| 索引访问 `[i]` | O(1) | 直接数组访问 |
| Contains | O(n) | 线性查找 |
| Sort | O(n log n) | 快速排序实现 |

### 对比 C++

```csharp
// C# List<T>
List<int> list = new List<int> { 3, 1, 4 };
list.Add(1);
list.Sort();
list.Remove(3);
int first = list[0];

// C++ std::vector
// std::vector<int> vec = {3, 1, 4};
// vec.push_back(1);
// std::sort(vec.begin(), vec.end());
// vec.erase(std::remove(vec.begin(), vec.end(), 3), vec.end());
// int first = vec[0];
```

---

## 3. Dictionary<K,V>——哈希表

### 内部实现

`Dictionary<K,V>` 使用**哈希表**（Hash Table），内部由**桶（Bucket）数组**和**链表**组成。

```
Dictionary<string, int> scores = new Dictionary<string, int>();

内部结构（简化）：
┌──────────────────────────────────────┐
│ buckets: int[]                       │ ← 桶数组，每个桶存链表的头索引
│ entries: Entry[]                     │ ← 所有键值对存储在这里
│ freeCount: int                       │ ← 空闲条目数
│ freeList: int                        │ ← 空闲链表头
│ comparer: IEqualityComparer<string>  │ ← 哈希比较器
└──────────────────────────────────────┘

每个 Entry 的结构：
struct Entry {
    int hashCode;       // key 的哈希码（取模后确定桶位置）
    int next;           // 链表的下一个条目索引
    K key;              // 键
    V value;            // 值
}
```

### 插入过程

```csharp
scores["Alice"] = 100;

// 1. 计算 "Alice" 的哈希码：hashCode = "Alice".GetHashCode()
// 2. 取模确定桶索引：bucketIndex = hashCode % buckets.Length
// 3. 检查该桶是否有相同 key 的条目（比较 hash 和 Equals）
// 4. 如果没有，创建新 Entry 存入 entries 数组
// 5. 将新 Entry 插入到桶的链表头部
```

### 哈希冲突解决

`Dictionary<K,V>` 使用**链地址法**（Separate Chaining）：

```
假设 buckets.Length = 7，两个 key 的哈希码 mod 7 都等于 3：

buckets[3] ──→ entries[0] ("Alice", 100)
                  next = -1 (结尾)

添加另一个哈希冲突的 key：
buckets[3] ──→ entries[1] ("Bob", 85) ──→ entries[0] ("Alice", 100)
                  next = 0                    next = -1
```

### 扩容

当元素数量超过桶数组容量的某个比例（负载因子）时，`Dictionary` 会：
1. 创建更大的桶数组（当前容量 × 2 的质数）
2. 重新计算所有 key 的哈希码在新桶数组中的位置
3. 重新插入所有条目

**时间复杂度：**
| 操作 | 平均 | 最差 |
|------|------|------|
| `[key]` 取值 | O(1) | O(n)（所有 key 哈希冲突） |
| Add | O(1) | O(n) |
| ContainsKey | O(1) | O(n) |

### 对比 C++

```csharp
// C# Dictionary
Dictionary<string, int> dict = new Dictionary<string, int>();
dict["Alice"] = 100;
dict.TryGetValue("Bob", out int score);

// C++ std::unordered_map
// std::unordered_map<std::string, int> dict;
// dict["Alice"] = 100;
// auto it = dict.find("Bob");
// if (it != dict.end()) int score = it->second;
```

---

## 4. 其他常用集合

### HashSet<T>——无重复集合

```csharp
HashSet<int> set = new HashSet<int> { 1, 2, 3, 3, 3 };
// set 只有 {1, 2, 3} — 重复的 3 被忽略

set.Add(4);           // 添加
set.Contains(2);      // true，O(1)
set.Remove(1);        // 删除
set.UnionWith(other); // 并集
set.IntersectWith(other); // 交集
```

内部和 `Dictionary<K,V>` 几乎一样，但只存储 key，不存 value。

### Queue<T>——队列（FIFO）

```csharp
Queue<GameObject> pool = new Queue<GameObject>();

pool.Enqueue(obj);   // 入队（添加到队尾）
GameObject o = pool.Dequeue(); // 出队（从队头移除并返回）
GameObject peek = pool.Peek(); // 查看队头但不移除
```

内部实现：**循环数组**（Circular Buffer），和 C++ `std::queue` 的底层 deque 不同。

### Stack<T>——栈（LIFO）

```csharp
Stack<string> undoStack = new Stack<string>();
undoStack.Push("State 1");
undoStack.Push("State 2");
string last = undoStack.Pop();   // "State 2"
string top = undoStack.Peek();   // 查看不移除
```

---

## 5. LINQ——语言集成查询

### 什么是 LINQ？

LINQ（Language Integrated Query）让你用**声明式语法**查询集合——你**描述想要什么**，而不是**描述怎么做**。

```csharp
// 命令式：描述怎么做（for 循环）
List<Player> result = new List<Player>();
for (int i = 0; i < players.Count; i++)
{
    if (players[i].Level > 5)
    {
        result.Add(players[i]);
    }
}
// 慢慢读才能理解这是"筛选等级大于 5 的玩家"

// 声明式：描述想要什么（LINQ）
var result = players.Where(p => p.Level > 5);
// 直接读："从哪里，满足什么条件"
```

### 扩展方法——LINQ 的基石

LINQ 方法（`Where`, `Select`, `OrderBy` 等）是**扩展方法**：

```csharp
// IEnumerable<T> 的扩展方法
public static class Enumerable
{
    public static IEnumerable<TSource> Where<TSource>(
        this IEnumerable<TSource> source,       // this 关键字 = 扩展方法
        Func<TSource, bool> predicate)
    {
        foreach (TSource item in source)
        {
            if (predicate(item))
                yield return item;  // yield return = 迭代器
        }
    }
}
```

`this IEnumerable<TSource>` 的意思是：这个方法是 `IEnumerable<T>` 类型的"扩展"，任何实现了 `IEnumerable<T>` 的类都可以调用。

```csharp
// 扩展方法让你能这样写：
players.Where(p => p.Level > 5);  // 就像 Where 是 players 的方法一样
```

### 延迟执行（Deferred Execution） vs 立即执行

LINQ 的**核心设计**：大多数操作是延迟执行的——定义查询时**不执行**，遍历结果时才执行。

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

// 定义查询（尚未执行！）
var query = numbers.Where(x => {
    Debug.Log($"Checking {x}");
    return x > 3;
});

Debug.Log("Query defined, not executed yet");

// 执行查询（遍历时执行）
foreach (var n in query)
{
    Debug.Log($"Got {n}");
}
// 输出：
// "Query defined, not executed yet"
// "Checking 1"  ← 遍历时才真正执行 Where
// "Checking 2"
// "Checking 3"
// "Checking 4"
// "Got 4"
// "Checking 5"
// "Got 5"
```

**哪些操作是延迟执行的？**
```csharp
// 延迟执行（定义时不执行）
Where, Select, SelectMany, OrderBy, GroupBy, Distinct, Take, Skip

// 立即执行（调用时立即执行）
ToList(), ToArray(), First(), FirstOrDefault(), Count(), Any(), All(), Max(), Min()
```

### 链式调用——组合查询

```csharp
var result = players
    .Where(p => p.IsAlive)           // 筛选活着的
    .OrderByDescending(p => p.Level) // 按等级降序
    .Select(p => p.Name)             // 只取名字
    .Take(3);                        // 取前 3 个

// 执行流程（延迟执行）：
// 遍历 players → 检查 IsAlive → 排序 → 取 Name → 只取前 3
// 整个过程只有一次遍历！
```

### LINQ 的迭代器模式底层

```csharp
// 当你写：
var query = players.Where(p => p.Level > 5).Select(p => p.Name);

// 实际生成的是嵌套迭代器：
// SelectWhereIterator（从外到内）
//   → Select 迭代器
//     → Where 迭代器
//       → List<T> 的遍历器

// 每次 MoveNext():
// SelectIterator.MoveNext()
//   → WhereIterator.MoveNext()
//     → ListEnumerator.MoveNext()  // 原始数据源
//     → 检查条件，不满足继续下一个
//   → 用 Select 转换
```

---

## 6. 常用 LINQ 方法

### Where——筛选（C++ `std::copy_if`）

```csharp
var alive = players.Where(p => p.HP > 0);
// SQL: SELECT * FROM players WHERE HP > 0
```

### Select——映射（C++ `std::transform`）

```csharp
var names = players.Select(p => p.Name);
// SQL: SELECT Name FROM players

var doubled = numbers.Select(n => n * 2);  // {1,2,3} → {2,4,6}

// Select 的索引重载：
var indexed = players.Select((p, index) => $"{index}: {p.Name}");
```

### OrderBy / OrderByDescending——排序（C++ `std::sort`）

```csharp
var sorted = players.OrderBy(p => p.Level);
var sortedDesc = players.OrderByDescending(p => p.Level);

// 多级排序：
var multiSorted = players
    .OrderByDescending(p => p.Level)  // 先按等级
    .ThenBy(p => p.Name);             // 同等级按名字
```

### Any / All——判断

```csharp
bool anyoneAlive = players.Any(p => p.HP > 0);  // 至少有一个活的？
bool allDead = players.All(p => p.HP <= 0);      // 全部死了？
```

### First / FirstOrDefault——取一个

```csharp
var first = players.First();                    // 取第一个（没有则抛异常）
var firstAlive = players.First(p => p.HP > 0);  // 取第一个活的（没有则抛异常）
var maybeAlive = players.FirstOrDefault(p => p.HP > 0);  // 没有就返回 null
```

### Count / Sum / Average——聚合

```csharp
int count = players.Count(p => p.IsAlive);  // 活着的数量
float totalHP = players.Sum(p => p.HP);     // 总血量
float avgLevel = players.Average(p => p.Level);  // 平均等级
int maxLevel = players.Max(p => p.Level);   // 最高等级
```

### GroupBy——分组

```csharp
// 按等级分组
var groups = players.GroupBy(p => p.Level);

foreach (var group in groups)
{
    Debug.Log($"Level {group.Key}: {group.Count()} players");
    foreach (var player in group)
    {
        Debug.Log($"  - {player.Name}");
    }
}
```

---

## 7. LINQ 表达式树——高级话题

```csharp
// LINQ to Objects 用委托（编译时执行）
Func<Player, bool> predicate = p => p.Level > 5;
// 编译为 IL 代码

// LINQ to SQL / Entities 用表达式树（运行时解析）
System.Linq.Expressions.Expression<Func<Player, bool>> expr = p => p.Level > 5;
// 编译为表达式树数据结构，运行时解析为 SQL
```

```sql
-- 上面的 expr 被解析为 SQL:
-- SELECT * FROM Players WHERE Level > 5
```

```csharp
// 表达式树的内部结构：
//
//             Lambda
//           /        \
//       Parameter    GreaterThan
//        (p)         /        \
//                 Member     Constant
//                 (p.Level)    (5)
```

---

## 练习：LINQ 实战

```csharp
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class Day04_LINQ : MonoBehaviour
{
    private class Player
    {
        public string Name;
        public int Level;
        public int HP;
        public string Class;  // "Warrior", "Mage", "Rogue"
        public bool IsAlive => HP > 0;
    }

    void Start()
    {
        var players = new List<Player>
        {
            new Player { Name = "Alice", Level = 10, HP = 100, Class = "Mage" },
            new Player { Name = "Bob",   Level = 5,  HP = 0,   Class = "Warrior" },
            new Player { Name = "Eve",   Level = 8,  HP = 30,  Class = "Rogue" },
            new Player { Name = "Dave",  Level = 3,  HP = 50,  Class = "Warrior" },
            new Player { Name = "Carol", Level = 10, HP = 80,  Class = "Mage" },
        };

        // 练习 1：活着的玩家
        var alive = players.Where(p => p.IsAlive);
        Debug.Log($"Alive: {string.Join(", ", alive.Select(p => p.Name))}");
        // Alice, Eve, Dave, Carol

        // 练习 2：按等级降序
        var ranked = players.OrderByDescending(p => p.Level).Select(p => p.Name);
        Debug.Log($"Ranked: {string.Join(", ", ranked)}");
        // Alice, Carol, Eve, Bob, Dave

        // 练习 3：按职业分组，计算每组平均等级
        var classAvg = players
            .GroupBy(p => p.Class)
            .Select(g => $"{g.Key}: avg level {g.Average(p => p.Level):F1}");

        Debug.Log($"By class: {string.Join(", ", classAvg)}");
        // Mage: avg level 10.0, Warrior: avg level 4.0, Rogue: avg level 8.0

        // 练习 4：找出每个职业中等级最高的玩家
        var topPerClass = players
            .GroupBy(p => p.Class)
            .Select(g => g.OrderByDescending(p => p.Level).First())
            .Select(p => $"{p.Class} top: {p.Name} (Lv.{p.Level})");

        Debug.Log($"Top per class: {string.Join(", ", topPerClass)}");

        // 练习 5：计算统计数据
        Debug.Log($"Total players: {players.Count}");
        Debug.Log($"Average level: {players.Average(p => p.Level):F1}");
        Debug.Log($"Total alive: {players.Count(p => p.IsAlive)}");
        Debug.Log($"Any dead mage? {players.Any(p => !p.IsAlive && p.Class == "Mage")}");
    }
}
```

---

---

## C++/Raylib 对照总结

| 概念 | C++ | C# |
|------|-----|-----|
| 动态数组 | `std::vector<T>` | `List<T>` |
| 键值对 | `std::map<K,V>` / `std::unordered_map` | `Dictionary<K,V>` |
| 集合 | `std::set<T>` | `HashSet<T>` |
| 队列 | `std::queue<T>` | `Queue<T>` |
| 栈 | `std::stack<T>` | `Stack<T>` |
| 筛选 | `std::copy_if` | `Where()` |
| 映射 | `std::transform` | `Select()` |
| 排序 | `std::sort` | `OrderBy()` / `OrderByDescending()` |
| 聚合 | `std::accumulate` | `Sum()` / `Average()` / `Count()` |
| 迭代器 | `begin()` / `end()` | `GetEnumerator()` / `foreach` |
| 范围 for | `for (auto& x : vec)` | `foreach (var x in list)` |

## 停靠点

> `List<T>` = 动态数组（`std::vector`），`Dictionary<K,V>` = 哈希表（`std::unordered_map`）。
> LINQ 是声明式查询——说"要什么"而不是"怎么做"。
> 延迟执行：Where/Select/OrderBy 定义时不执行，遍历时才执行。
> 立即执行：ToList/Count/First 调用时立即执行。
> LINQ 链式调用 = 嵌套迭代器，整个链只有一次遍历。

