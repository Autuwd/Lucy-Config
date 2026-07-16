## 一、核心数据结构类及常用方法

### 1. 数组 `T[]`

数组长度固定，元素可读写，是使用最频繁的底层结构。

csharp

```csharp
int[] arr = new int[5];          // 创建一个长度为5的数组，默认值为0
int[] arr2 = new int[] {1,2,3}; // 创建并初始化数组（方式一）
int[] arr3 = {1,2,3};           // 创建并初始化数组（方式二，语法糖）

// 属性与方法
int len = arr.Length;            // 获取数组长度
Array.Sort(arr);                 // 对数组进行升序排序，直接修改原数组，无返回值
Array.Sort(arr, (a,b) => b.CompareTo(a)); // 使用自定义比较器进行降序排序
Array.Reverse(arr);              // 反转数组元素顺序，直接修改原数组，无返回值
int idx = Array.BinarySearch(arr, target); 
// 二分查找：若找到返回索引；否则返回负数（按位取反可得应插入位置），前提是数组已排序
Array.Copy(src, dest, length);   // 从 src 复制 length 个元素到 dest，dest 必须足够大
Array.Fill(arr, value);          // 用指定值填充整个数组
int[] sub = arr[1..3];           // 范围语法：截取索引1到2（含头不含尾），返回新数组
int[] clone = (int[])arr.Clone(); // 浅拷贝数组（值类型就是深拷贝效果）
foreach (int item in arr) { }    // 使用 foreach 遍历数组每个元素
```



### 2. `List<T>` 动态数组

csharp

```csharp
List<int> list = new List<int>() {1,2,3};          // 创建并初始化动态数组
List<int> list2 = new List<int>(capacity);         // 可预分配容量以提升性能

list.Add(4);                 // 在末尾添加一个元素 O(1)
list.AddRange(arr);          // 批量添加多个元素
list.Insert(index, item);    // 在指定索引处插入元素 O(n) 慎用
list.Remove(item);           // 移除第一个匹配到的元素 O(n)
list.RemoveAt(index);        // 移除指定索引处的元素 O(n)
list.Sort();                 // 升序排序，直接修改原列表
list.Sort((a,b)=>b.CompareTo(a)); // 使用 Lambda 自定义排序规则（此处降序）
list.Reverse();              // 反转列表元素顺序
int count = list.Count;      // 获取当前元素个数
bool has = list.Contains(item); // 判断是否包含指定元素 O(n)
int idx = list.IndexOf(item);   // 查找元素第一次出现的索引，未找到返回 -1 O(n)
int idx2 = list.BinarySearch(item); // 二分查找，需先排序，找到返回索引，否则返回负数
list.Clear();                // 清空所有元素，容量不变
list.TrimExcess();           // 将容量缩减至实际元素数量，回收多余内存
int[] arr = list.ToArray();  // 将 List 转换为数组
```



### 3. `Dictionary<TKey,TValue>` 哈希映射

csharp

```csharp
var dict = new Dictionary<int, string>();          // 创建哈希映射（键值对字典）
dict.Add(key, value);            // 添加键值对，key 已存在会抛异常
dict[key] = value;               // 索引器：key 不存在则添加，已存在则更新值
bool exist = dict.ContainsKey(key); // 判断是否包含指定键 O(1)
bool existV = dict.ContainsValue(value); // 判断是否包含指定值 O(n) 慎用
string val;
if (dict.TryGetValue(key, out val)) { } // 尝试获取值，存在返回 true 并赋值 val，推荐写法避免两次查找
dict.Remove(key);                // 移除指定键值对，成功返回 true
int cnt = dict.Count;            // 获取键值对数量
foreach (var kv in dict) {       // 遍历字典，kv.Key 访问键，kv.Value 访问值
}
dict.Keys;   // 获取所有键的集合（可用于迭代）
dict.Values; // 获取所有值的集合（可用于迭代）
// 高级：获取或添加
var v = dict.GetValueOrDefault(key);                 // 获取值，key 不存在返回 default(TValue)
var v2 = dict.GetValueOrDefault(key, defaultValue);  // 获取值，key 不存在返回指定的 defaultValue (.NET Core 2.0+)
```



### 4. `HashSet<T>` 哈希集合

csharp

```csharp
var set = new HashSet<int>();        // 创建哈希集合（元素不重复，无序）
set.Add(item);              // 添加元素，返回 true 表示添加成功（原先不存在该元素）
bool has = set.Contains(item); // 判断是否包含指定元素 O(1)
set.Remove(item);           // 移除指定元素，成功返回 true
int cnt = set.Count;        // 获取集合中元素的数量
set.IntersectWith(other);   // 取交集：仅保留也存在于 other 中的元素，修改当前集合
set.UnionWith(other);       // 取并集：添加 all other 中的元素，修改当前集合
set.ExceptWith(other);      // 取差集：移除也存在于 other 中的元素，修改当前集合
set.Overlaps(other);        // 判断是否有交集（存在公共元素）
set.SetEquals(other);       // 判断两个集合是否相等（元素完全相同）
// 遍历 foreach (var x in set)  // 用 foreach 遍历集合所有元素
```



### 5. `Stack<T>` 栈 (LIFO)

csharp

```csharp
Stack<int> stack = new Stack<int>(); // 创建栈（后进先出 LIFO）
stack.Push(item);       // 将元素压入栈顶
int top = stack.Peek();     // 查看栈顶元素但不弹出，栈空会抛异常
int popped = stack.Pop();   // 弹出栈顶元素并返回其值，栈空会抛异常
bool ok = stack.TryPeek(out top);   // 尝试查看栈顶元素，成功返回 true 不抛异常 (.NET 5+)
bool ok2 = stack.TryPop(out popped); // 尝试弹出栈顶元素，成功返回 true 不抛异常
int cnt = stack.Count;      // 获取栈中元素数量
```



### 6. `Queue<T>` 队列 (FIFO)

csharp

```csharp
Queue<int> q = new Queue<int>(); // 创建队列（先进先出 FIFO）
q.Enqueue(item);    // 将元素加入队尾
int head = q.Peek();        // 查看队首元素但不移除，队空会抛异常
int dequeued = q.Dequeue(); // 移除并返回队首元素，队空会抛异常
bool ok = q.TryPeek(out head);      // 尝试查看队首元素，不抛异常 (.NET 5+)
bool ok2 = q.TryDequeue(out dequeued); // 尝试移除队首元素，不抛异常
int cnt = q.Count;          // 获取队列中元素数量
```



### 7. `LinkedList<T>` 双向链表

插入删除 O(1)，但随机访问 O(n)，遍历用节点迭代。

csharp

```csharp
LinkedList<int> ll = new LinkedList<int>();         // 创建双向链表
LinkedListNode<int> node1 = ll.AddFirst(item);      // 在链表头部添加元素，返回新节点
LinkedListNode<int> node2 = ll.AddLast(item);       // 在链表尾部添加元素，返回新节点
ll.AddAfter(existingNode, newItem);   // 在指定节点之后插入新元素
ll.AddBefore(existingNode, newItem);  // 在指定节点之前插入新元素
ll.Remove(node);               // 删除指定节点 O(1)
ll.RemoveFirst();              // 删除头节点
ll.RemoveLast();               // 删除尾节点
LinkedListNode<int> first = ll.First;  // 获取第一个节点（属性，非方法）
LinkedListNode<int> last = ll.Last;    // 获取最后一个节点
int val = node.Value;          // 获取或设置节点的值
```



### 8. `SortedSet<T>` 有序集合（红黑树，无重复）

csharp

```csharp
SortedSet<int> ss = new SortedSet<int>() {3,1,2}; // 创建有序集合（红黑树），插入时自动排序
ss.Add(item);          // 添加元素，成功（原先不存在）返回 true
ss.Remove(item);       // 移除指定元素，成功返回 true
bool has = ss.Contains(item); // 判断是否包含指定元素 O(log n)
int min = ss.Min;      // 获取最小值（属性），.NET Core 2.0+
int max = ss.Max;      // 获取最大值
// 范围查找
var view = ss.GetViewBetween(lo, hi); // 获取 [lo, hi] 范围内的子集视图，与原集合同步变化
// 寻找大于等于 t 的最小元素
if (ss.TryGetValue(t, out int actual))  // 尝试获取等于 t 的元素（精确匹配），.NET 6+
// 通用实现：ss.FirstOrDefault(x => x >= t)  // LINQ 方式查找大于等于 t 的最小元素
```



### 9. `SortedDictionary<TKey,TValue>` 有序字典（按键排序）

csharp

```csharp
var sdict = new SortedDictionary<int, string>(); // 创建有序字典，按键自动排序（红黑树）
sdict[key] = value;               // 添加或更新键值对
// API 类似 Dictionary，额外有 .Keys .Values 按序迭代（按键排序顺序）
// 获取小于等于 key 的最大键等需要自定义逻辑或 LINQ（低效），一般用二分搜索配合 List 代替
```



### 10. `PriorityQueue<TElement,TPriority>` (.NET 6+)

小顶堆（默认），优先级小的先出队。

csharp

```csharp
var pq = new PriorityQueue<string, int>();  // 创建优先队列（小顶堆），元素 + 优先级
pq.Enqueue("task1", 2);    // 将元素按指定优先级加入队列
pq.Enqueue("task2", 1);    // 优先级数字越小越优先出队（task2 优先级更高）
string top = pq.Peek();    // 查看优先级最高的元素（不出队），此处返回 task2
string deq = pq.Dequeue(); // 取出优先级最高的元素并返回，此处返回 task2
int cnt = pq.Count;        // 获取队列中元素数量
// 大顶堆实现：优先级取负数
pq.Enqueue(item, -priority); // 通过传入优先级负值实现大顶堆
// 自定义比较器：
var pq2 = new PriorityQueue<string, int>(Comparer<int>.Create((a,b) => b.CompareTo(a))); // 传入自定义比较器，改为降序实现大顶堆
```



### 11. `SortedList<TKey,TValue>`

基于两个数组的有序字典，内存占用小，按键排序，插入删除 O(n)，不适合频繁修改，但按索引访问 O(1)。

csharp

```csharp
var sl = new SortedList<int, string>(); // 创建有序列表（基于双数组，按键排序）
sl.Add(1,"a");            // 添加键值对，按键自动排序
sl[2]="b";                // 索引器：添加或更新键值对
string s = sl.Values[0];  // 通过值列表的索引直接访问第 0 个值 O(1)
int key = sl.Keys[0];     // 通过键列表的索引直接访问第 0 个键 O(1)
int idx = sl.IndexOfKey(2);  // 二分查找指定键的索引 O(log n)，未找到返回 -1
sl.RemoveAt(0);           // 移除指定索引处的键值对 O(n)
sl.TryGetValue(1, out string val); // 尝试获取指定键的值，成功返回 true
```



------

## 二、结构体（`struct`）的使用

在刷题时，结构体适合**封装简单不可变数据**、**提升数组操作的局部性**（值类型连续内存分配），或用作字典键时需要实现相等性。

### 定义与实例化

csharp

```csharp
public struct Point          // 定义结构体（值类型）
{
    public int X;            // 公开字段
    public int Y;
    
    // 构造函数（必须对所有字段赋值）
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"({X}, {Y})"; // 重写 ToString 方便输出
}

// 使用
Point p1 = new Point(1, 2);  // 调用构造函数创建
Point p2 = default;          // 默认值 X=0, Y=0，不调用构造函数
```



### 结构体作为字典键

必须重写 `Equals` 和 `GetHashCode`，并建议实现 `IEquatable<Point>` 以避免装箱。

csharp

```csharp
public readonly struct Point : IEquatable<Point>  // 只读结构体，实现 IEquatable 避免装箱
{
    public int X { get; }   // 只读属性
    public int Y { get; }
    
    public Point(int x, int y) { X = x; Y = y; }

    public bool Equals(Point other) => X == other.X && Y == other.Y; // 强类型 Equals
    public override bool Equals(object obj) => obj is Point p && Equals(p); // 重写 object.Equals
    public override int GetHashCode() => HashCode.Combine(X, Y);     // 重写哈希码
}

var dict = new Dictionary<Point, string>();  // 将结构体作为字典键
dict[new Point(1,2)] = "A";                 // 使用自定义 Point 作为键
```



### 常用场景

- 坐标点 `(int row, int col)` 或 `(int x, int y)`。
- 作为 `ValueTuple` 的替代，语义更明确。
- 性能优化：大量临时对象避免 GC 压力（但注意大结构体的复制成本）。
- 只读结构体 (`readonly struct`)：防止修改，保证不变性，帮助编译器优化。

### 注意事项

- 结构体是值类型，赋值和传参会复制整个数据。对于较大的结构体，考虑传递 `in`（只读引用）或 `ref`。
- 不能定义无参构造函数（C# 10 之前）。
- 避免定义可变结构体，以免意外行为。

------

## 三、字符串与文本处理利器

### `string` 常用方法

字符串是不可变的，所有“修改”方法均返回新字符串。

csharp

```csharp
string s = " Hello,World! ";      // 字符串字面量（不可变类型）
int len = s.Length;               // 获取字符串长度（字符数）
string trim = s.Trim();           // 去除首尾空白字符，还有 TrimStart, TrimEnd 变体
string sub = s.Substring(start);  // 截取从指定索引到末尾的子串
string sub2 = s.Substring(2, 5);  // 从索引2开始取5个字符
int idx = s.IndexOf(',');         // 查找指定字符第一次出现的索引，未找到返回 -1
int idx2 = s.LastIndexOf('o');    // 查找指定字符最后一次出现的索引
bool starts = s.StartsWith("He"); // 判断是否以指定字符串开头
bool ends = s.EndsWith("!");      // 判断是否以指定字符串结尾
bool contains = s.Contains("llo");// 判断是否包含指定子串
string[] parts = s.Split(',');    // 按指定分隔符拆分为字符串数组，可传多个分隔符
string joined = string.Join(",", arr); // 用指定分隔符连接数组元素为一个字符串
string rep = s.Replace("World", "C#"); // 替换所有匹配的子串为新字符串
string upper = s.ToUpper();       // 转换为大写
string lower = s.ToLower();       // 转换为小写
char[] chars = s.ToCharArray();   // 将字符串转为字符数组
// 格式化
string formatted = $"x={x}, y={y}"; // 字符串插值格式化（推荐使用）
```



### `StringBuilder` 高效拼接大量字符串

csharp

```csharp
var sb = new StringBuilder();  // 创建可变字符串（高效拼接大量字符串时使用）
sb.Append("Hello");       // 在末尾追加字符串
sb.AppendLine(", World"); // 追加字符串并自动换行
sb.AppendFormat("x={0}", x); // 格式化追加（类似 string.Format）
sb.Insert(5, " INSERTED");   // 在指定索引处插入字符串
sb.Remove(0, 2);             // 从指定索引开始移除指定长度的字符
sb.Replace('l', 'L');        // 替换所有匹配的字符（也可替换子串）
string result = sb.ToString(); // 将 StringBuilder 转为不可变字符串
int cap = sb.Capacity;       // 获取或设置当前容量
int length = sb.Length;      // 获取当前字符长度
sb.Clear();                  // 清空所有字符，重置长度 (.NET Core 2.0+)
```



### `char` 静态方法判断字符类型

csharp

```csharp
char.IsDigit(c);          // 判断字符是否为十进制数字 (0-9)
char.IsLetter(c);         // 判断字符是否为字母
char.IsLetterOrDigit(c);  // 判断字符是否为字母或数字
char.IsLower(c);          // 判断字符是否为小写字母
char.IsUpper(c);          // 判断字符是否为大写字母
char.IsWhiteSpace(c);     // 判断字符是否为空白字符
char.ToLower(c);          // 将字符转换为小写
char.ToUpper(c);          // 将字符转换为大写
```



------

## 四、数学与数值操作

csharp

```csharp
Math.Abs(val);           // 返回绝对值
Math.Max(a,b);           // 返回两个值中的较大值
Math.Min(a,b);           // 返回两个值中的较小值
Math.Sqrt(x);            // 计算平方根
Math.Pow(base, exp);     // 计算幂运算（base 的 exp 次方）
Math.Log(x);             // 计算自然对数（e 为底）
Math.Log10(x);           // 计算以10为底的对数
Math.Ceiling(d);         // 向上取整（天花板函数）
Math.Floor(d);           // 向下取整（地板函数）
Math.Round(d);           // 四舍五入（可通过 MidpointRounding 参数指定舍入规则）
Math.Clamp(value, min, max);  // 将值限制在 [min, max] 区间内 (.NET Core 2.0+)
Math.Sign(x);            // 返回数字的符号：负数返回 -1，零返回 0，正数返回 1

// 整数溢出检查
int result = checked(a + b); // 在 checked 上下文计算，溢出时抛 OverflowException
```



### `BigInteger` 大整数

csharp

```csharp
BigInteger bi = BigInteger.Parse("12345678901234567890"); // 从字符串解析大整数
BigInteger bi2 = BigInteger.Pow(2, 100);  // 计算大整数幂运算
BigInteger sum = bi + bi2;  // 大整数支持常规算术运算符
bi.CompareTo(bi2);  // 比较大小：返回 <0（bi 小）、0（相等）、>0（bi 大）
```



------

## 五、LINQ 常用方法

在不需要极致性能时，LINQ 可大幅简化代码。注意大部分返回 `IEnumerable<T>`，需要立马求值可加 `.ToArray()` 或 `.ToList()`。

csharp

```csharp
int[] nums = {1,2,3,4,5};

// 过滤
var evens = nums.Where(x => x%2==0);     // 筛选满足条件的元素（返回 IEnumerable）
// 映射
var squares = nums.Select(x => x*x);     // 对每个元素投影转换
// 排序
var ordered = nums.OrderBy(x => x);           // 升序排序
var descending = nums.OrderByDescending(x => x); // 降序排序
// 分组
var groups = nums.GroupBy(x => x%2);   // 按键分组，每组有 Key 属性
// 聚合
int sum = nums.Sum();        // 求和
int count = nums.Count();    // 计数
double avg = nums.Average(); // 求平均值
int max = nums.Max();        // 求最大值
int min = nums.Min();        // 求最小值
// 第一/最后/唯一
int first = nums.First();                     // 取第一个元素，序列为空抛异常
int firstOrDefault = nums.FirstOrDefault(x => x > 10); // 取第一个满足条件的元素，未找到返回默认值
int last = nums.Last();                       // 取最后一个元素
int single = nums.Single(x => x == 3);        // 取唯一匹配的元素，不唯一或不存在抛异常
int el = nums.ElementAt(2); // 取指定索引处的元素
// 任何/全部
bool any = nums.Any(x => x > 0);  // 是否有任意元素满足条件
bool all = nums.All(x => x > 0);  // 是否所有元素满足条件
// 前后部分
var skip = nums.Skip(2);  // 跳过前 N 个元素
var take = nums.Take(3);  // 取前 N 个元素
// 连接（可用于二维遍历）
var query = from x in nums
            from y in nums
            select new { x, y, sum = x+y };  // LINQ 语法：两个序列的笛卡尔积
// 集合操作
var distinct = nums.Distinct();    // 去重
var union = a.Union(b);            // 并集
var intersect = a.Intersect(b);    // 交集
var except = a.Except(b);          // 差集（在 a 中但不在 b 中的元素）
// 索引的 Select
var withIdx = nums.Select((val, idx) => new { val, idx }); // 同时获取元素和索引
// 反转（注意：是 IEnumerable 扩展，会生成新序列）
var reversed = nums.Reverse(); // 反转序列顺序，返回新序列而非修改原序列
```



------

## 六、高效内存操作：`Span<T>` 与 `Memory<T>`

减少分配，处理子数组/子字符串切片极快（`Span`是只读/可读写的 ref struct 不能放堆上）。

csharp

```csharp
// 数组切片不复制
int[] arr = {1,2,3,4,5};
Span<int> span = arr.AsSpan(1, 3);  // 创建 arr[1..3] 的视图（不复制），内容 {2,3,4}
span[0] = 99;                       // 通过 Span 修改会直接影响原数组
Span<int> stack = stackalloc int[3];// 在栈上分配小数组（无需堆分配）

// 字符串切片不分配
ReadOnlySpan<char> ros = "Hello World".AsSpan(6, 5);  // 只读字符串切片 "World"，不分配新字符串

// 常用方法
span.Slice(start, length);   // 切片：从 start 开始取 length 个元素，返回新 Span
span.CopyTo(destSpan);       // 将元素复制到目标 Span
span.SequenceEqual(otherSpan); // 按元素序列比较两个 Span 是否相等
span.ToArray();              // 将 Span 转为数组（这是唯一产生堆分配的操作）
int idx = span.IndexOf(value); // 查找指定值的第一个索引，未找到返回 -1
```



------

## 七、自定义排序与比较器

### `Comparison<T>` 委托（用于 List.Sort 等方法）

csharp

```csharp
list.Sort((a, b) => a.Value.CompareTo(b.Value));  // 使用 Lambda 表达式自定义 List 排序规则
Array.Sort(arr, (a, b) => b.CompareTo(a));        // 对数组自定义排序（此处 b 在前为降序）
```



### `IComparer<T>` 接口

csharp

```csharp
class DescendingComparer : IComparer<int>  // 实现 IComparer<T> 接口自定义比较器
{
    public int Compare(int x, int y) => y.CompareTo(x); // 返回 y-x 实现降序
}
Array.Sort(arr, new DescendingComparer());  // 将自定义比较器传入排序方法
// 或使用 Comparer 工厂
var comparer = Comparer<int>.Create((x, y) => y.CompareTo(x)); // 用工厂方法快速创建比较器
```



### 多键排序（字典、list等按复合条件）

csharp

```csharp
// 利用 LINQ ThenBy 实现多键排序
list.OrderBy(x => x.LastName).ThenByDescending(x => x.FirstName); // 先按 LastName 升序，再按 FirstName 降序
// 或 Sort 比较器内实现（自定义比较器中实现多条件比较逻辑）
```



------

## 八、元组 `ValueTuple` / `Tuple`

轻量数据组合，无需定义类或结构体，支持解构。

csharp

```csharp
(int, int) pair = (1, 2);        // 创建值元组，无命名（通过 Item1, Item2 访问）
var point = (x: 3, y: 4);       // 带命名的值元组，通过字段名访问
int x = point.x;                 // 通过字段名访问元组元素
// 解构
(int a, int b) = pair;          // 将元组解构为多个变量
// 方法返回多值
public (int min, int max) FindMinMax(int[] arr) { return (min, max); }  // 方法返回元组
var res = FindMinMax(arr);       // 接收返回的元组
Console.WriteLine(res.min);      // 访问元组中的命名元素
```



元组可用作字典键，但需注意 `ValueTuple` 实现了 `IEquatable`，可以安全使用：

csharp

```csharp
var dict = new Dictionary<(int, int), string>();  // 用值元组作为字典键
dict[(1,2)] = "path";  // ValueTuple 实现了 IEquatable，可安全用作键
```



------

## 九、其他高频知识点

### 递归与栈模拟

递归深度过大可能栈溢出，可以用显式栈迭代。

csharp

```csharp
// DFS 迭代
Stack<T> stack = new Stack<T>();  // 用栈模拟递归（DFS 迭代实现）
stack.Push(root);        // 将起始节点压入栈
while (stack.Count > 0) { var node = stack.Pop(); ... }  // 循环弹出并处理节点
```



### BFS 与队列经典模板

csharp

```csharp
Queue<T> queue = new Queue<T>();  // BFS 层序遍历经典模板
queue.Enqueue(start);   // 将起始节点加入队列
while (queue.Count > 0) {
    int size = queue.Count;   // 记录当前层的节点数（用于分层遍历）
    for (int i=0; i<size; i++) { ... }  // 逐层处理
}
```



### 位运算常用函数

csharp

```csharp
int count = BitOperations.PopCount((uint)n); // .NET Core 3.0+ 内置方法：统计二进制中1的个数
// 手写 Brian Kernighan 算法：统计二进制中1的个数
while (n != 0) { n &= (n-1); cnt++; }  // 每次消除最低位的1，循环次数即1的个数
// 开关位操作
n |= (1 << k);          // 将第 k 位置为1
n &= ~(1 << k);         // 将第 k 位清为0
n ^= (1 << k);          // 翻转第 k 位（0变1，1变0）
bool isSet = (n & (1 << k)) != 0; // 判断第 k 位是否为1
// 最低位1
int lowbit = n & -n;    // 提取最低位的1（只保留最低位1，其余位清0）
```



### 二维数组与交错数组

csharp

```csharp
int[,] matrix = new int[3,4];    // 创建二维矩形数组 3行x4列
int val = matrix[1,2];           // 访问第1行第2列的元素
int rows = matrix.GetLength(0);  // 获取行数（第0维长度）
int cols = matrix.GetLength(1);  // 获取列数（第1维长度）

// 交错数组（数组的数组）：每一行长度可以不同
int[][] jag = new int[3][];      // 创建3行的交错数组
jag[0] = new int[] {1,2};       // 第一行分配长度为2的数组
```



### 算法常用模式

- **滑动窗口**：双指针，用 Dictionary/HashSet 统计字符频次。
- **前缀和**：`prefix[i+1] = prefix[i] + nums[i]` 用于 O(1) 区间和。
- **单调栈**：用 Stack 解决下一个更大元素等问题。
- **并查集**：通常自己实现 `int[] parent` 和 `Find/Union` 方法。
- **动态规划**：多用数组 `int[] dp` 或二维 `int[,] dp`。

### 不可变记录 `record` (C# 9+)

可用作数据载体，自动实现值相等。

csharp

```csharp
public record Point(int X, int Y);  // 定义记录类型，自动实现值相等性、ToString、解构等
var p1 = new Point(1,2);            // 创建记录实例
var p2 = p1 with { X = 3 };         // 非破坏性变异：基于 p1 创建新记录，仅修改指定成员
```



### `System.Memory` 扩展

- `ArrayPool<T>`：临时大数组租用，减少 GC。

csharp

```csharp
int[] buffer = ArrayPool<int>.Shared.Rent(minSize); // 从共享池租借一个数组（长度 ≥ minSize），减少 GC 压力
// 使用 buffer ... ...
ArrayPool<int>.Shared.Return(buffer);               // 将数组归还到池中，供后续复用（用完后必须归还）
```



------

## 十、常用组合与建议

| 场景                    | 推荐结构                               |
| :---------------------- | :------------------------------------- |
| 频繁随机访问、固定长度  | `T[]`                                  |
| 动态增删、尾部操作多    | `List<T>`                              |
| 快速查找、映射          | `Dictionary<K,V>`                      |
| 去重、集合运算          | `HashSet<T>`                           |
| 后进先出（DFS/单调栈）  | `Stack<T>`                             |
| 先进先出（BFS/层序）    | `Queue<T>`                             |
| 需要最大/最小值频繁取出 | `PriorityQueue<T,P>` 或 `SortedSet<T>` |
| 有序无重复、范围查询    | `SortedSet<T>`                         |
| 字符串频繁拼接          | `StringBuilder`                        |
| 零分配子数组/子串操作   | `Span<T>`, `ReadOnlySpan<T>`           |
| 临时大量数据            | ArrayPool 租借                         |
| 代码简洁聚合操作        | LINQ                                   |

熟练掌握以上 API 和模式，足以应对绝大多数算法题。建议刻意练习其中不熟悉的部分，边刷题边查阅，熟悉到能自然写出最佳实践为止。