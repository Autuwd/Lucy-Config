# Day 04：多线程并发与无锁编程

## 一、游戏服务器的并发问题

### 典型竞态条件

```csharp
// 问题：两个线程同时给玩家加经验
class PlayerData
{
    public long Exp;
    public int Level;
}

// 线程1：打怪获得经验
void OnKillMonster(PlayerData player, int exp)
{
    player.Exp += exp; // 不是原子操作！
    player.Level = CalculateLevel(player.Exp);
}

// 线程2：完成任务获得经验
void OnCompleteQuest(PlayerData player, int exp)
{
    player.Exp += exp; // 和上面的 += 交错执行
    player.Level = CalculateLevel(player.Exp);
}
```

```
线程交错（EXP = 100，加 50 和 30）：
  Thread1: 读取 EXP = 100
  Thread2: 读取 EXP = 100
  Thread1: 写入 EXP = 150
  Thread2: 写入 EXP = 130  ← 丢失了 20 点经验！
```

### 解决方案层级

| 层级 | 方法 | 性能 | 难度 |
|------|------|------|------|
| 应用层 | 锁 (lock/Mutex) | 中 | 低 |
| 框架层 | Actor 模型（无共享） | 高 | 中 |
| 数据层 | 原子操作 (Interlocked) | 极高 | 中 |
| 内存层 | 内存屏障/Volatile | 极高 | 高 |
| 硬件层 | CAS 指令 | 最高 | 极高 |

---

## 二、锁 (Lock)

### Monitor (lock 语句)

```csharp
class SafePlayerData
{
    private readonly object _lock = new();
    private long _exp;
    private int _level;

    public void AddExp(long amount)
    {
        lock (_lock) // 等价于 Monitor.Enter/Exit
        {
            _exp += amount;
            _level = CalculateLevel(_exp);
        }
    }

    public (long exp, int level) GetSnapshot()
    {
        lock (_lock)
        {
            return (_exp, _level);
        }
    }
}
```

### lock 的代价

```csharp
// 锁竞争的代价
class LockBenchmark
{
    private object _lock = new();
    private int _counter;
    private const int Iterations = 10_000_000;

    public void SingleThread()
    {
        for (int i = 0; i < Iterations; i++)
            _counter++;               // 15ms
    }

    public void WithLock()
    {
        for (int i = 0; i < Iterations; i++)
            lock (_lock) _counter++;  // 800ms (53x 慢)
    }

    public void InterlockedIncrement()
    {
        for (int i = 0; i < Iterations; i++)
            Interlocked.Increment(ref _counter); // 300ms (20x 慢)
    }
}
```

### 减少锁竞争的策略

1. **缩小锁范围** — 只在必要时加锁
2. **读写分离** — ReaderWriterLockSlim
3. **无锁数据结构** — ConcurrentDictionary
4. **减少锁持有时间** — 不要在锁内做 IO
5. **分片** — Striped Locking

```csharp
// 分片锁示例
class StripedLock<T>
{
    private object[] _locks;
    private const int ConcurrencyLevel = 64;

    public StripedLock()
    {
        _locks = new object[ConcurrencyLevel];
        for (int i = 0; i < ConcurrencyLevel; i++)
            _locks[i] = new object();
    }

    public object GetLock(T key)
    {
        int hash = key.GetHashCode() & int.MaxValue;
        return _locks[hash % ConcurrencyLevel];
    }

    public void Execute(T key, Action action)
    {
        lock (GetLock(key))
        {
            action();
        }
    }
}

// 使用：同时操作不同玩家不互斥
var playerLocks = new StripedLock<long>();
// 玩家 1 和 玩家 2 操作不冲突
playerLocks.Execute(player1Id, () => player1.AddExp(100));
playerLocks.Execute(player2Id, () => player2.AddExp(100));
```

---

## 三、ReaderWriterLockSlim

读多写少的场景（排行榜读、配置表读，定时刷新）：

```csharp
class PlayerCache
{
    private ReaderWriterLockSlim _rwLock = new();
    private Dictionary<long, PlayerData> _cache = new();

    // 多个读可以同时进行
    public PlayerData Get(long playerId)
    {
        _rwLock.EnterReadLock();
        try
        {
            _cache.TryGetValue(playerId, out var data);
            return data;
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    // 写是独占的
    public void Update(long playerId, PlayerData data)
    {
        _rwLock.EnterWriteLock();
        try
        {
            _cache[playerId] = data;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    // 可升级锁：先读，如果需要写再升级
    public PlayerData GetOrCreate(long playerId)
    {
        _rwLock.EnterUpgradeableReadLock();
        try
        {
            if (_cache.TryGetValue(playerId, out var data))
                return data;

            // 升级为写锁（此时阻塞其他读）
            _rwLock.EnterWriteLock();
            try
            {
                _cache[playerId] = new PlayerData(playerId);
                return _cache[playerId];
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }
        finally
        {
            _rwLock.ExitUpgradeableReadLock();
        }
    }

    public void Dispose()
    {
        _rwLock.Dispose();
    }
}
```

### 性能对比

| 锁类型 | 空竞争 | 高竞争 | 适用场景 |
|--------|--------|--------|---------|
| `lock` | ~50ns | ~1μs+ | 写频繁 |
| `ReaderWriterLockSlim` 读 | ~70ns | ~100ns | 读多写少 |
| `ReaderWriterLockSlim` 写 | ~100ns | ~5μs+ | 写需要异常 |
| `SpinLock` | ~20ns | 无 | 极短临界区 |

---

## 四、原子操作 (Interlocked)

```csharp
class AtomicStats
{
    private long _totalExpGained;
    private long _totalDamageDealt;
    private long _playerOnlineCount;

    // 原子自增/自减
    public long AddExp(long amount)
    {
        return Interlocked.Add(ref _totalExpGained, amount);
    }

    // 原子读
    public long TotalExp => Interlocked.Read(ref _totalExpGained);

    // CAS — Compare And Swap
    private int _maxOnlinePlayers;

    public bool TryUpdateMaxOnline(int current)
    {
        int oldMax = _maxOnlinePlayers;
        if (current > oldMax)
        {
            // 如果 _maxOnlinePlayers 还是 oldMax，就更新为 current
            Interlocked.CompareExchange(ref _maxOnlinePlayers, current, oldMax);
            return true;
        }
        return false;
    }

    // 自旋锁（基于 CAS）
    private int _spinLock = 0; // 0=未锁定, 1=已锁定

    public void SpinLockExample()
    {
        // 忙等待直到获得锁
        while (Interlocked.CompareExchange(ref _spinLock, 1, 0) != 0)
        {
            Thread.SpinWait(10); // 建议 CPU 等待
        }

        try
        {
            // 临界区
            Console.WriteLine("获得自旋锁");
        }
        finally
        {
            Interlocked.Exchange(ref _spinLock, 0); // 释放锁
        }
    }
}
```

### Interlocked 的方法

| 方法 | 效果 | 对应硬件指令 |
|------|------|-------------|
| `Increment(ref x)` | x++ | `LOCK INC` |
| `Decrement(ref x)` | x-- | `LOCK DEC` |
| `Add(ref x, n)` | x += n | `LOCK ADD` / `LOCK XADD` |
| `Exchange(ref x, v)` | x = v + 返回旧值 | `LOCK XCHG` |
| `CompareExchange(ref x, v, c)` | if x==c then x=v | `LOCK CMPXCHG` |
| `Read(ref x)` | 原子读 64 位值 | 内存屏障 |

---

## 五、内存模型与内存屏障

### CPU 重排序

```csharp
// 初始状态: x = 0, y = 0, flag = false

// 线程1
x = 1;              // Store
flag = true;        // Store  → CPU 可能重排：先把 flag 写出去

// 线程2
if (flag)           // Load
    Console.WriteLine(x); // Load → 可能读到 x = 0！
```

因为 CPU 和编译器会**重排序**指令。

### Volatile

```csharp
class VolatileExample
{
    private volatile bool _running = true;
    // volatile 防止：读/写重排序到此字段的前后
    // 保证：对 _running 的读/写不会被 CPU 重排

    public void Stop()
    {
        _running = false; // 写入立即对其他线程可见（释放语义）
    }

    public void Loop()
    {
        while (_running) // 读取最新值（获取语义）
        {
            // 处理逻辑
        }
    }
}
```

### MemoryBarrier

```csharp
class MemoryBarrierExample
{
    private int x;
    private bool flag;

    public void Writer()
    {
        x = 42;
        Thread.MemoryBarrier(); // 保证 x 的写入在 flag 之前
        flag = true;
        Thread.MemoryBarrier();
    }

    public void Reader()
    {
        Thread.MemoryBarrier(); // 保证 flag 的读取在 x 之前
        if (flag)
        {
            Thread.MemoryBarrier(); // 保证 x 读到最新值
            Console.WriteLine(x); // 保证得到 42
        }
    }
}
```

### .NET 内存模型保证

- **同一线程内的依赖关系**：不重排
- **lock 边界**：自动插入完整内存屏障
- **Interlocked 操作**：有 full fence 语义
- **volatile**：单 CPU 足够，多 CPU 需要全屏障

---

## 六、无锁队列

### 无锁单生产者单消费者队列 (SPSC)

```csharp
class SpscQueue<T>
{
    private T[] _buffer;
    private volatile int _head;    // 读位置
    private volatile int _tail;    // 写位置
    private readonly int _mask;

    public SpscQueue(int capacity)
    {
        // 容量必须是 2 的幂
        capacity = NextPowerOfTwo(capacity);
        _buffer = new T[capacity];
        _mask = capacity - 1;
    }

    public bool TryEnqueue(T item)
    {
        int tail = _tail;
        int nextTail = (tail + 1) & _mask;

        if (nextTail == _head) // 队列满
            return false;

        _buffer[tail] = item;
        // 释放屏障：保证 item 写入先于 tail 更新
        Volatile.Write(ref _tail, nextTail);
        return true;
    }

    public bool TryDequeue(out T item)
    {
        int head = _head;
        if (head == Volatile.Read(ref _tail)) // 获取屏障
        {
            item = default;
            return false;
        }

        item = _buffer[head];
        _buffer[head] = default; // 帮助 GC
        _head = (head + 1) & _mask;
        return true;
    }

    private static int NextPowerOfTwo(int n)
    {
        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        return n + 1;
    }
}
```

### 无锁多生产者单消费者队列 (MPSC)

```csharp
// 基于链表
class MpscLinkedQueue<T>
{
    class Node
    {
        public T Item;
        public volatile Node Next;
    }

    private volatile Node _head;
    private Node _tail;          // 只有消费者线程访问

    public MpscLinkedQueue()
    {
        Node sentinel = new Node();
        _head = sentinel;
        _tail = sentinel;
    }

    // 多线程调用
    public void Enqueue(T item)
    {
        Node newNode = new Node { Item = item };

        Node prevHead = Interlocked.Exchange(ref _head, newNode);
        prevHead.Next = newNode; // 通过 CAS 保证只有一个生产者能看到 prevHead
    }

    // 单消费者调用
    public bool TryDequeue(out T item)
    {
        Node first = _tail.Next;
        if (first == null)
        {
            item = default;
            return false;
        }

        item = first.Item;
        _tail = first;
        return true;
    }
}
```

---

## 七、C# 线程安全集合

```csharp
// ConcurrentDictionary — 分段锁
ConcurrentDictionary<long, PlayerData> players = new();
players.TryAdd(playerId, new PlayerData());
players.TryGetValue(playerId, out var data);
players.TryUpdate(playerId, newData, oldData);
players.GetOrAdd(playerId, id => new PlayerData(id));
players.AddOrUpdate(playerId, addFactory, updateFactory);

// ConcurrentQueue — 无锁（多生产者多消费者）
ConcurrentQueue<Packet> packetQueue = new();
packetQueue.Enqueue(packet);
packetQueue.TryDequeue(out var p);

// ConcurrentBag — 无序集合，线程本地缓存
ConcurrentBag<byte[]> bufferPool = new();
bufferPool.Add(buffer);
bufferPool.TryTake(out var b);

// BlockingCollection — 生产者消费者模式
BlockingCollection<Packet> blockingQueue = new(new ConcurrentQueue<Packet>(), 10000);
blockingQueue.Add(packet);           // 满时阻塞
var p = blockingQueue.Take();        // 空时阻塞
// 支持取消: blockingQueue.GetConsumingEnumerable(cancellationToken)
```

### 性能对比 (Benchmark)

| 操作 | ConcurrentQueue | lock(Queue) | 无锁 SPSC |
|------|----------------|-------------|-----------|
| Enqueue | ~80ns | ~60ns (高竞争 ~1μs) | ~30ns |
| Dequeue | ~80ns | ~60ns | ~30ns |
| 内存分配 | 有 | 有 | 无（预分配） |

---

## 八、对比客户端 Unity 多线程

| | Unity 客户端 | 游戏服务器 |
|--|-------------|-----------|
| 主线程 | Unity 主线程（渲染） | 游戏逻辑主线程 |
| 多线程 | Job System (安全) | 手动线程（高风险） |
| 锁使用 | 少（大部分单线程） | 频繁 |
| 无锁编程 | 不需要 | 性能关键路径需要 |
| 数据结构 | List/Array | 大量 Concurrent 集合 |

---

## 九、练习

1. **锁竞争压测**：用 4 个线程频繁 lock 一个变量 vs `Interlocked.Increment`，测 1 亿次耗时
2. **读写锁应用**：实现一个玩家配置表，启动时加载完，运行时只有读操作
3. **无锁队列实现**：实现 SPSC 环形队列并验证正确性（多线程极端测试）
4. **查找死锁**：给出一段死锁代码，让主人分析原因和解决方案
5. **ConcurrentDictionary 场景**：实现玩家数据管理器，支持 10000 玩家并发登录/登出

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 竞态条件 | 多线程同时修改共享数据导致不一致 |
| lock | 最通用的同步手段，但竞争时慢 |
| ReaderWriterLockSlim | 读不互斥，适合读多写少 |
| Interlocked | 原子操作，比锁快 10x |
| 内存屏障 | 防止 CPU 重排序，保证可见性 |
| 无锁队列 | CAS 实现，比有锁快但设计复杂 |
| ConcurrentDictionary | 分段锁，高并发下比普通 Dictionary+lock 快 |
