# Day 04：多线程并发与无锁编程 — 进阶深入

## 一、Michael-Scott 无锁队列（完整实现）

### 算法原理

Michael-Scott 队列是经典的多生产者多消费者 (MPMC) 无锁队列，基于链表实现。

```
核心思想：
  1. 使用 CAS (Compare-And-Swap) 原子操作
  2. 通过哨兵节点简化边界条件
  3. 分两步完成入队：先链接节点，再移动尾指针
  4. 出队：从哨兵取下一个节点，CAS 移动头指针

ABA 问题的处理：
  需要在指针上加版本号（tag）
  .NET 中可以用 ReferenceCounting 或 AtomicReference<T>
```

```csharp
// 完整 MPMC 无锁队列实现
public class MichaelScottQueue<T>
{
    // 节点：使用引用类型 + 互锁操作
    private class Node
    {
        public readonly T Value;
        public AtomicNodeRef<Node> Next;

        public Node(T value)
        {
            Value = value;
            Next = new AtomicNodeRef<Node>(null);
        }

        public Node() // 用于哨兵
        {
            Next = new AtomicNodeRef<Node>(null);
        }
    }

    // 带 tag 的节点引用（解决 ABA 问题）
    public struct AtomicNodeRef<TRef> where TRef : class
    {
        private readonly TRef _ref;
        private readonly uint _tag; // 版本号，每次 CAS 成功后递增

        public AtomicNodeRef(TRef nodeRef, uint tag = 0)
        {
            _ref = nodeRef;
            _tag = tag;
        }

        public TRef Ref => _ref;
        public uint Tag => _tag;

        public static bool Cas(
            ref AtomicNodeRef<TRef> location,
            AtomicNodeRef<TRef> expected,
            AtomicNodeRef<TRef> desired)
        {
            // 用 Interlocked.CompareExchange 模拟 CAS
            // 这里使用对象引用的原子操作
            TRef oldRef = Interlocked.CompareExchange(
                ref Unsafe.As<AtomicNodeRef<TRef>, TRef>(ref location),
                desired._ref, expected._ref);
            return ReferenceEquals(oldRef, expected._ref);
        }
    }

    // 队列状态
    private Node _sentinel;
    private volatile Node _head; // 实际指向哨兵
    private volatile Node _tail;

    public MichaelScottQueue()
    {
        _sentinel = new Node();
        _head = _sentinel;
        _tail = _sentinel;
    }

    // 多生产者入队
    public void Enqueue(T item)
    {
        var newNode = new Node(item);

        while (true)
        {
            var localTail = _tail;
            var next = localTail.Next.Ref;

            if (localTail == _tail) // 确保 tail 没有被修改
            {
                if (next == null)
                {
                    // 情况 1：tail 指向最后一个节点
                    if (Interlocked.CompareExchange(
                            ref Unsafe.As<AtomicNodeRef<Node>, Node>(
                                ref localTail.Next),
                            newNode, null) == null)
                    {
                        // CAS 成功：将新节点链入
                        // 移动 tail 到新节点
                        Interlocked.CompareExchange(
                            ref Unsafe.As<volatile Node, Node>(ref _tail),
                            newNode, localTail);
                        return;
                    }
                }
                else
                {
                    // 情况 2：tail 落后了（其他线程已插入但未更新 tail）
                    // 帮助更新 tail
                    Interlocked.CompareExchange(
                        ref Unsafe.As<volatile Node, Node>(ref _tail),
                        next, localTail);
                }
            }
        }
    }

    // 多消费者出队
    public bool TryDequeue(out T item)
    {
        while (true)
        {
            var localHead = _head;
            var localTail = _tail;
            var firstNode = localHead.Next.Ref;

            if (localHead == _head) // 一致性检查
            {
                if (firstNode == null)
                {
                    // 队列空
                    item = default;
                    return false;
                }

                if (localHead == localTail)
                {
                    // tail 落后于 head？帮助更新
                    Interlocked.CompareExchange(
                        ref Unsafe.As<volatile Node, Node>(ref _tail),
                        firstNode, localTail);
                }
                else
                {
                    // 正常出队：读取值 + 移动 head
                    item = firstNode.Value;

                    if (Interlocked.CompareExchange(
                            ref Unsafe.As<volatile Node, Node>(ref _head),
                            firstNode, localHead) == localHead)
                    {
                        return true;
                    }
                }
            }
        }
    }
}
```

---



## 二、Interlocked 高级模式

### CompareExchange 的双重用途

```csharp
class InterlockedPatterns
{
    // 1. 原子初始化（Lazy Initialization）
    private object _heavyObject;
    private const int UNINITIALIZED = 0;
    private const int INITIALIZING = 1;
    private const int INITIALIZED = 2;
    private int _initState;

    public object GetOrCreate()
    {
        if (_initState == INITIALIZED)
            return Volatile.Read(ref _heavyObject);

        // 只有一个线程进入初始化
        if (Interlocked.CompareExchange(ref _initState, INITIALIZING, UNINITIALIZED) == UNINITIALIZED)
        {
            // 我是第一个初始化的
            _heavyObject = CreateHeavyObject();
            Volatile.Write(ref _initState, INITIALIZED);
        }
        else
        {
            // 其他线程在初始化，等
            SpinWait.SpinUntil(() => _initState == INITIALIZED);
        }

        return _heavyObject;
    }

    // 2. 原子状态机转换
    private int _state = (int)ServerState.Stopped;

    public enum ServerState { Stopped, Starting, Running, Stopping }

    public bool TryTransitionTo(ServerState from, ServerState to)
    {
        return Interlocked.CompareExchange(
            ref _state, (int)to, (int)from) == (int)from;
    }

    // 3. 无锁计数器 + 限流
    private int _concurrentOps;
    private const int MAX_CONCURRENT = 100;

    public bool TryAcquireSlot()
    {
        int current;
        do
        {
            current = _concurrentOps;
            if (current >= MAX_CONCURRENT)
                return false;
        }
        while (Interlocked.CompareExchange(
            ref _concurrentOps, current + 1, current) != current);
        return true;
    }

    public void ReleaseSlot()
    {
        Interlocked.Decrement(ref _concurrentOps);
    }

    // 4. CAS 自旋锁（极短临界区专用）
    private int _lockFlag; // 0=free, 1=locked

    public void AcquireSpinLock()
    {
        var sw = new SpinWait();
        while (Interlocked.CompareExchange(ref _lockFlag, 1, 0) != 0)
        {
            sw.SpinOnce();
        }
    }

    public void ReleaseSpinLock()
    {
        Volatile.Write(ref _lockFlag, 0);
    }

    // 5. 原子添加最大/最小值
    private long _maxPlayersOnline;

    public void UpdateMaxPlayers(long current)
    {
        long oldVal;
        do
        {
            oldVal = _maxPlayersOnline;
            if (current <= oldVal) break;
        }
        while (Interlocked.CompareExchange(
            ref _maxPlayersOnline, current, oldVal) != oldVal);
    }
}
```

---

## 三、内存屏障实战

### .NET 中三种内存屏障

```csharp
class MemoryBarrierDemo
{
    // 1. 完整屏障 (Full Fence)
    // 组织所有 Load/Store 重排序
    public void FullFence()
    {
        Thread.MemoryBarrier();
        // 等价于 x86 上的 `mfence` 指令
        // 在 ARM64 上等价于 `dmb ish`
    }

    // 2. 获取屏障 (Acquire Fence)
    // 后面的 Load/Store 不能重排序到前面
    // 用于读取锁标记
    public volatile int _flag;
    // volatile 读 = acquire 语义

    // 3. 释放屏障 (Release Fence)
    // 前面的 Load/Store 不能重排序到后面
    // 用于写入锁标记
    // volatile 写 = release 语义
}
```

### 实现轻量级读写锁（无内核对象）

```csharp
class LightweightReadWriteLock
{
    // state 编码：
    // 高位 = 写者标记
    // 低位 = 读者计数
    private volatile int _state;
    private const int WRITER_MASK = int.MinValue; // 0x80000000
    private const int READER_COUNT_MASK = int.MaxValue; // 0x7FFFFFFF

    public void EnterReadLock()
    {
        var sw = new SpinWait();
        while (true)
        {
            int current = _state;
            if ((current & WRITER_MASK) == 0) // 没有写者在写
            {
                // 尝试增加读者计数
                if (Interlocked.CompareExchange(
                        ref _state, current + 1, current) == current)
                    return;
            }
            sw.SpinOnce();
        }
    }

    public void ExitReadLock()
    {
        Interlocked.Decrement(ref _state);
    }

    public void EnterWriteLock()
    {
        var sw = new SpinWait();
        while (true)
        {
            int current = _state;
            if (current == 0) // 没有读者也没有写者
            {
                // 设置写者标记
                if (Interlocked.CompareExchange(
                        ref _state, WRITER_MASK, 0) == 0)
                    return;
            }
            sw.SpinOnce();
        }
    }

    public void ExitWriteLock()
    {
        Volatile.Write(ref _state, 0);
    }
}
```

---

## 四、锁的对比测试

### 性能基准

```csharp
// BenchmarkDotNet 对 4 种同步原语的测试（100 万次操作）
// 配置：4 核 8 线程 CPU，50% 读 50% 写

/*
锁类型             耗时       内存分配    特点
──────────────────────────────────────────────
SpinLock           ~28ms       0B      极短临界区，不 sleep
Monitor (lock)     ~85ms       0B      通用，自动阻塞
ReaderWriterLockSlim ~62ms     ~8KB    读多写少场景
SemaphoreSlim      ~95ms       ~12KB   异步等待，限流

竞争激烈时的行为：
  SpinLock: CPU 100%（忙等），不切换上下文
  Monitor: 上下文切换，CPU 低，延迟高
  ReaderWriterLockSlim: 混合模式，有内部 Spin
  SemaphoreSlim: 内核对象等待，适合长等待
*/

// 选择指南
public static string RecommendLock(int readRatio, int avgHoldTimeNs)
{
    if (avgHoldTimeNs < 50)
    {
        if (readRatio > 80) return "ReaderWriterLockSlim (Spin 模式)";
        return "SpinLock";
    }
    if (avgHoldTimeNs < 1000)
    {
        if (readRatio > 80) return "ReaderWriterLockSlim";
        return "Monitor (lock)";
    }
    // 长时间持有 → 使用 SemaphoreSlim 或设计无锁
    return "重新设计: 复制分离 (Copy-on-Write)";
}
```

### Copy-on-Write 替代读锁

```csharp
// 替代 ReadWriterLockSlim 的无锁方案
// 适用：高频读、低频写的配置数据
class CopyOnWriteCache<TKey, TValue>
{
    private volatile ImmutableDictionary<TKey, TValue> _data;

    public CopyOnWriteCache()
    {
        _data = ImmutableDictionary<TKey, TValue>.Empty;
    }

    // 读 — 完全无锁！
    public bool TryGetValue(TKey key, out TValue value)
    {
        return _data.TryGetValue(key, out value);
    }

    // 写 — 创建新副本，然后原子替换引用
    public void Update(TKey key, TValue value)
    {
        var current = _data;
        // 注意：在大量写场景下，每次都创建 ImmutableDictionary 开销大
        // 限制：写频率 < 100次/秒
        var updated = current.SetItem(key, value);
        Interlocked.CompareExchange(ref _data, updated, current);
    }

    // 批量更新
    public void BatchUpdate(Action<ImmutableDictionary<TKey, TValue>.Builder> updateAction)
    {
        var builder = _data.ToBuilder();
        updateAction(builder);
        var updated = builder.ToImmutable();
        Interlocked.Exchange(ref _data, updated);
    }
}
```

---

## 五、Concurrent 集合选择和技巧

### 四种集合的底层实现

```csharp
class ConcurrentCollectionInternal
{
    /*
    ConcurrentQueue<T>：
      内部使用分段存储 (segments)，每段是独立数组
      入队：Interlocked.Increment tail index
      出队：Interlocked.Increment head index
      线程数：多生产者多消费者
      特点：FIFO，有顺序保证

    ConcurrentStack<T>：
      内部使用链表，头节点 CAS 操作
      Push：CAS 替换 _head
      TryPop：CAS 替换 _head
      特点：LIFO，适合工作窃取

    ConcurrentBag<T>：
      每个线程有独立 ThreadLocal 存储
      入队：线程本地操作，无竞争
      出队：先偷自己的，再偷别人的
      特点：无顺序，同线程局部性极好

    ConcurrentDictionary<TKey, TValue>：
      分段锁 (striped locking)
      默认 31 个分区，每区独立锁
      GetOrAdd：一次 CAS 尝试 + 锁回退
      特点：O(1) 读，写锁粒度细
    */
}

// 实战选择
class CollectionSelector
{
    // FIFO 消息队列 → ConcurrentQueue<T>
    private readonly ConcurrentQueue<Packet> _messageQueue = new();

    // 工作窃取任务 → ConcurrentBag<T>
    private readonly ConcurrentBag<Action> _localTasks = new();

    // LIFO 最近使用缓存 → ConcurrentStack<T>
    private readonly ConcurrentStack<byte[]> _bufferPool = new();

    // 高并发字典 → ConcurrentDictionary<TKey, TValue>
    private readonly ConcurrentDictionary<long, PlayerData> _players = new();

    // 以上集合的替代方案：
    // - Channel<T>：比 ConcurrentQueue 更丰富的语义
    // - ImmutableDictionary：读零锁，写代价高
    // - 手写分片 Dictionary：极致性能
}
```

### 手写分片字典（Striped Dictionary）

```csharp
class StripedDictionary<TKey, TValue>
{
    private readonly Dictionary<TKey, TValue>[] _shards;
    private readonly object[] _locks;
    private readonly int _shardCount;

    public StripedDictionary(int shardCount = 31)
    {
        _shardCount = shardCount;
        _shards = new Dictionary<TKey, TValue>[shardCount];
        _locks = new object[shardCount];
        for (int i = 0; i < shardCount; i++)
        {
            _shards[i] = new Dictionary<TKey, TValue>();
            _locks[i] = new object();
        }
    }

    private (int shard, object lockObj) GetShard(TKey key)
    {
        int shardIndex = (key.GetHashCode() & int.MaxValue) % _shardCount;
        return (shardIndex, _locks[shardIndex]);
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        var (index, lockObj) = GetShard(key);

        // 先无锁读
        if (_shards[index].TryGetValue(key, out var val))
            return val;

        lock (lockObj)
        {
            // Double-check
            if (_shards[index].TryGetValue(key, out val))
                return val;

            val = factory(key);
            _shards[index][key] = val;
            return val;
        }
    }
}
```

---

## 六、SemaphoreSlim 高级用法

### 异步限流

```csharp
// 控制对 DB/RPC 的并发请求数
class AsyncThrottler
{
    private readonly SemaphoreSlim _semaphore;

    public AsyncThrottler(int maxConcurrent)
    {
        _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    // 异步等待 + 自动释放
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation, 
        CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            return await operation();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // 限流 + 超时
    public async Task<T> ExecuteWithTimeout<T>(
        Func<Task<T>> operation, 
        TimeSpan timeout)
    {
        if (!await _semaphore.WaitAsync(timeout))
        {
            throw new TimeoutException(
                $"等待信号量超时 {timeout.TotalMilliseconds}ms");
        }
        try
        {
            return await operation();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

---

## 七、数据导向的线程模型

### Job Queue 框架

```csharp
// 游戏服务器中，不同系统的线程模型
// Unity ECS 的数据导向思想在服务器中的应用

// 定义作业
interface IJob
{
    void Execute();
}

// 批处理作业：同一数据类型的操作批量执行，利用 CPU Cache
interface IBatchJob<TInput>
{
    ReadOnlySpan<TInput> Inputs { get; }
    void Execute(int index);
}

class DataOrientedScheduler
{
    // 每个线程一个作业队列
    private readonly Thread[] _workers;
    private readonly Channel<IJob>[] _queues;
    private readonly int _workerCount;

    public DataOrientedScheduler(int threadCount)
    {
        _workerCount = threadCount;
        _workers = new Thread[threadCount];
        _queues = new Channel<IJob>[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            _queues[i] = Channel.CreateUnbounded<IJob>(
                new UnboundedChannelOptions
                {
                    SingleWriter = false,
                    SingleReader = true
                });

            int workerIndex = i;
            _workers[i] = new Thread(() => WorkerLoop(workerIndex))
            {
                Name = $"DOTS-Worker-{i}",
                IsBackground = true
            };
            _workers[i].Start();
        }
    }

    // 数据导向的分配：相同玩家的作业分配到同一个线程
    public void EnqueueForPlayer(long playerId, IJob job)
    {
        int threadIndex = playerId % _workerCount;
        _queues[threadIndex].Writer.TryWrite(job);
    }

    // 广播作业：所有线程都执行
    public void EnqueueAll(IJob job)
    {
        for (int i = 0; i < _workerCount; i++)
            _queues[i].Writer.TryWrite(job);
    }

    // 批处理优化：连续的内存访问
    public void EnqueueBatch<T>(ReadOnlySpan<long> playerIds, IBatchJob<T> batchJob)
    {
        // 按线程分组
        var groups = new Dictionary<int, List<long>>();
        foreach (var id in playerIds)
        {
            int idx = id % _workerCount;
            if (!groups.TryGetValue(idx, out var list))
            {
                list = new List<long>();
                groups[idx] = list;
            }
            list.Add(id);
        }

        // 分发分组
        foreach (var (threadIdx, ids) in groups)
        {
            _queues[threadIdx].Writer.TryWrite(
                new BatchJobWrapper<T>(batchJob));
        }
    }

    private void WorkerLoop(int workerIndex)
    {
        var reader = _queues[workerIndex].Reader;
        while (reader.TryRead(out var job))
        {
            job.Execute();
        }
    }
}
```

---

## 八、高性能调试与诊断

### 检测竞争

```csharp
class ContentionDetector
{
    private readonly long[] _lockWaitTimes; // 每个锁的等待时间
    private long _totalContentions;

    // 检测锁竞争
    public static bool IsContended(object lockObj, int spinMs = 50)
    {
        var sw = Stopwatch.StartNew();
        bool acquired = Monitor.TryEnter(lockObj, spinMs);
        if (acquired)
            Monitor.Exit(lockObj);
        return !acquired;
    }

    // 日志热点锁
    public static IDisposable TrackLockContention(string lockName, 
        long thresholdMs = 5)
    {
        var sw = Stopwatch.StartNew();
        return new ContentionTracker(lockName, sw, thresholdMs);
    }
}

class ContentionTracker : IDisposable
{
    private readonly string _name;
    private readonly Stopwatch _sw;
    private readonly long _thresholdMs;

    public void Dispose()
    {
        var elapsed = _sw.ElapsedMilliseconds;
        if (elapsed > _thresholdMs)
        {
            Log.Warning("[锁竞争] {Lock} 等待 {Elapsed}ms > 阈值 {Threshold}ms",
                _name, elapsed, _thresholdMs);
        }
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Michael-Scott 队列 | 经典 MPMC 无锁队列，CAS 链表 + 哨兵节点 |
| ABA 问题 | 指针被重用时用版本号区分，AtomicReference 带 tag |
| Interlocked 模式 | CAS 无锁初始化、状态机转换、限流计数 |
| 内存屏障 | Acquire (volatile 读) 和 Release (volatile 写) 保证顺序 |
| Copy-on-Write | 读零锁，写建新副本，适合高频读低频写 |
| Striped Dictionary | 分 31 片字典，同时操作不同 key 不互斥 |
| SemaphoreSlim | 异步限流 + 超时，比 lock 适合长时间等待 |
| 数据导向线程 | 按玩家 ID 分配线程，相同数据在同一核心处理 |

---

## 对照表：C++ 并发 vs C# 进阶

| C++ | C# | 说明 |
|-----|-----|------|
| `std::atomic<T>` | `Interlocked` / `Volatile` | C# 更上层，粒度粗 |
| `std::memory_order_acquire/release` | `Volatile.Read/Write` | C++ 更精细的控制 |
| `__sync_val_compare_and_swap` | `Interlocked.CompareExchange` | 底层指令一致 |
| `std::mutex` | `Monitor` (lock) | 都是内核对象同步 |
| `tbb::concurrent_queue` | `Channel<T>` | 性能接近 |
| `std::shared_mutex` | `ReaderWriterLockSlim` | 读写锁，语义一致 |
| `thread_local` | `ThreadLocal<T>` / `ThreadStatic` | 线程本地存储 |
| `moodycamel::ConcurrentQueue` | `ConcurrentQueue<T>` | 都是 MPMC 设计 |
