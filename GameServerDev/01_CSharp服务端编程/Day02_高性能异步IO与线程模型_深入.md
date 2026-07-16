# Day 02：高性能异步 IO 与线程模型 — 进阶深入

## 一、IOCP vs epoll 性能对决

### 架构层级差异

```
IOCP (Windows)                    epoll (Linux)
─────────────                     ────────────
应用程序                          应用程序
  │                                │
  ▼                                ▼
IOCP 完成端口                    epoll 实例 (epoll_create)
  │                                │
  ▼                                ▼
IOCP 线程池                      epoll_wait 线程
  │                                │
  ▼                                ▼
异步过程调用 (APC)              事件就绪通知
  │                                │
  ▼                                ▼
设备驱动 (DMA)                  设备驱动 (DMA)
```

### 关键性能差异点

```csharp
// IOCP 的优势：零系统调用
// epoll_wait 每次返回后，需要至少一次 recv/read 系统调用
// IOCP 在投递时就传入了缓冲区，完成后数据已在用户空间

// IOCP 单次 IO 路径：
//   1. 用户调用 WSARecv → 2. 内核 DMA 到用户 buffer → 3. 入队完成包
//   系统调用次数：1（投递）+ 1（取完成包）= 2

// epoll 单次 IO 路径：
//   1. epoll_wait → 2. recv 从内核 buffer 拷贝到用户 buffer
//   系统调用次数：1（epoll_wait）+ 1（recv）= 2
//   但如果有多个事件，epoll_wait + N × recv = 1 + N

// 结论：高并发下 IOCP 的系统调用数 ≈ 事件数
//                    epoll 的系统调用数 > 事件数（多一个 epoll_wait）
```

### 基准测试对比（模拟数据，基于业界基准）

| 场景 | IOCP (Windows Server 2022) | epoll (Linux 6.x) | 胜出方 |
|------|---------------------------|-------------------|--------|
| 1000 连接，10000 包/秒 | ~12μs/包 | ~10μs/包 | epoll |
| 10000 连接，1000 包/秒 | ~8μs/包 | ~9μs/包 | IOCP |
| 100000 连接，10 包/秒 | ~5μs/包 | ~7μs/包 | IOCP |
| 大文件传输 (1MB+) | ~2ms/传输 | ~1.5ms/传输 | epoll |
| 极端高并发（C10M 尝试） | 不适用 | epoll + SO_REUSEPORT | epoll |

### .NET 跨平台实现

```csharp
// .NET 5+ 使用统一的 SocketAsyncEventArgs API
// 内部在 Windows 上用 IOCP，Linux 上用 epoll
// 开发者不需要关心底层差异

// 判断当前平台使用的 IO 模型
public static string GetIoModel()
{
    if (OperatingSystem.IsWindows())
        return "IOCP (完成端口)";
    else if (OperatingSystem.IsLinux())
        return "epoll (事件轮询)";
    else if (OperatingSystem.IsMacOS())
        return "kqueue (内核事件队列)";
    else
        return "select (回退模式)";
}

// 注意：虽然 API 相同，但 IOCP 和 epoll 的线程模型差异
// 可能导致在不同平台上出现不同性能特征
```

---

## 二、Kestrel 服务器内部架构演进

### libuv 时代（ASP.NET Core 1.x-2.x）

```
Kestrel → libuv → IOCP/epoll
  libuv 提供跨平台异步 IO 抽象层
  代价：额外一层调用，性能损耗约 5-10%
```

### 原生 Socket 时代（ASP.NET Core 3.0+）

```csharp
// 去掉 libuv，直接使用 .NET Socket
// 性能提升 15-20%，得益于：

// 1. 减少间接调用层数
// 之前：Kestrel → libuv → Sockets
// 现在：Kestrel → Sockets (直接)

// 2. 减少内存分配
// libuv 内部有独立的 buffer 管理
// 现在直接用 SocketAsyncEventArgs 共享 buffer

// 3. 更好的 IOCP/epoll 利用
// libuv 封装了一些平台特性，去除了非通用优化
```

### Kestrel 传输层抽象（可插拔）

```csharp
// Kestrel 的 IConnectionListener 接口
// 这就是为什么可以替换底层传输

interface IConnectionListener
{
    ValueTask<ConnectionContext> AcceptAsync();
    ValueTask UnbindAsync();
}

// 游戏服务器可以借鉴：抽象传输层
interface IGameTransport
{
    // 屏蔽 IOCP/epoll/自定义传输 的差异
    ValueTask SendAsync(long connectionId, ReadOnlyMemory<byte> data);
    ValueTask<ReceiveResult> ReceiveAsync(long connectionId);
    void Disconnect(long connectionId);
}

// TCP 实现
class TcpTransport : IGameTransport { /* SocketAsyncEventArgs */ }
// UDP 实现（用于实时同步）
class UdpTransport : IGameTransport { /* Socket.Udp */ }
// 内存传输（单进程测试）
class LoopbackTransport : IGameTransport { /* Channel */ }
```

---

## 三、Channel&lt;T&gt;：高性能生产者-消费者

### Channel 的内部实现原理

```csharp
// Channel<T> 在内部使用无锁 SPSC 环形缓冲区（bounded mode）
// 或 ConcurrentQueue（unbounded mode）

// 相比 ConcurrentQueue + AutoResetEvent 的手工实现：
// - Channel 内部使用信号量（SemaphoreSlim）等待，不忙等
// - 支持异步等待（WaitToReadAsync/WaitToWriteAsync）
// - 内部无锁设计，减少上下文切换

class ChannelExample
{
    // unbounded channel：生产者永远不会阻塞
    Channel<Packet> _unbounded = Channel.CreateUnbounded<Packet>(
        new UnboundedChannelOptions
        {
            SingleWriter = false,  // 多生产者
            SingleReader = true    // 单消费者（游戏主循环）
        });

    // bounded channel：生产者受控，防止消费跟不上
    Channel<Packet> _bounded = Channel.CreateBounded<Packet>(
        new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait,  // 满时等待
            SingleWriter = false,
            SingleReader = true
        });

    // 生产者（网络线程）
    public void OnPacketReceived(Packet p)
    {
        // unbounded 模式永远不会失败
        _unbounded.Writer.TryWrite(p);

        // bounded 模式可能失败（满了）
        while (!_bounded.Writer.TryWrite(p))
        {
            // 满了，可以丢弃 or 等待
            // BoundedChannelFullMode.DropOldest 自动丢弃最老的
            // BoundedChannelFullMode.DropWrite 丢弃当前
            // BoundedChannelFullMode.Wait 阻塞
        }
    }

    // 消费者（游戏主循环）
    public async ValueTask DrainPacketsAsync(CancellationToken ct)
    {
        var reader = _unbounded.Reader;

        // 批量读取（单个 Consumer 最高效方式）
        await foreach (var packet in reader.ReadAllAsync(ct))
        {
            ProcessPacket(packet);
        }
    }
}
```

### Channel 内部三个关键优化

```
1. 无锁写（SingleWriter=true）
   - 跳过 CAS，直接写 tail 指针
   - 用在游戏主循环写就是单写者

2. 批量读取 (TryRead 批量)
   - 一次锁获取可以读多个元素
   - 减少原子操作次数

3. 异步等待信号量
   - 避免 Thread.Sleep 轮询
   - 不消耗 CPU 等待
```

### Channel 线程模型适配

```csharp
// 游戏服务器的典型管道：
// 网络线程 (SAEA) → Channel<Packet> → 逻辑线程 → Channel<SendTask> → 发送线程

class GamePipeline
{
    private readonly Channel<Packet> _inbound = Channel.CreateUnbounded<Packet>(
        new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
    private readonly Channel<SendTask> _outbound = Channel.CreateBounded<SendTask>(
        new BoundedChannelOptions(5000) { FullMode = BoundedChannelFullMode.DropOldest });

    // 网络线程：收到数据后
    public void OnReceive(Packet p)
    {
        _inbound.Writer.TryWrite(p);
    }

    // 逻辑线程：处理消息
    public async Task LogicLoop(CancellationToken ct)
    {
        var reader = _inbound.Reader;
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16)); // 60fps

        while (await timer.WaitForNextTickAsync(ct))
        {
            // 一帧内处理所有待处理消息
            while (reader.TryRead(out var packet))
            {
                await ProcessPacket(packet);
            }
            UpdateGameState();
        }
    }

    // 逻辑线程发送数据
    public void Send(long connId, byte[] data)
    {
        _outbound.Writer.TryWrite(new SendTask(connId, data));
    }
}
```

---

## 四、ValueTask 在服务器中的正确使用

### 值类型 vs 引用类型

```csharp
// Task 是 class（引用类型），每次创建都在堆上分配
// ValueTask 是 struct（值类型），可以完全在栈上

// 什么情况下 ValueTask 真正节省分配？
class ValueTaskOptimization
{
    // ✅ 适用：经常同步完成的操作
    // 例：缓存命中、内存操作、条件判断后直接返回值
    private int _cachedPlayerCount;
    private readonly Database _db;

    // 如果缓存已存在，直接返回，零分配
    public ValueTask<int> GetPlayerCountAsync()
    {
        if (_cachedPlayerCount > 0)
            return new ValueTask<int>(_cachedPlayerCount); // 零分配
        return new ValueTask<int>(LoadPlayerCountAsync());
    }

    private async Task<int> LoadPlayerCountAsync()
    {
        _cachedPlayerCount = await _db.QueryAsync<int>("SELECT COUNT(*) FROM players");
        return _cachedPlayerCount;
    }

    // ❌ 不适用：几乎总是异步完成的操作
    // 网络 IO、数据库 IO → 基本不会同步完成
    // 用 ValueTask 没有意义，因为绝大多数情况还是会分配 Task

    // ❌ 不适用：await 多次
    // ValueTask 设计为只 await 一次
    public async Task BadUse()
    {
        ValueTask<int> vt = GetCountAsync();
        int r1 = await vt;
        // int r2 = await vt; // 不允许！ValueTask 只能消费一次
    }
}
```

### 游戏服务器 ValueTask 最佳实践

```csharp
// 在网络层使用 ValueTask 减少 GC
class NetworkManager
{
    // 手动池化 ValueTaskSource
    // 用于 Socket 异步操作，避免每次分配 Task
    private readonly ObjectPool<ManualResetValueTaskSourceCore<bool>> _pool;

    public ValueTask<int> SendAsync(long connId, ReadOnlyMemory<byte> data)
    {
        // 如果数据量小，可以同步尝试发送
        if (data.Length < 256 && TrySendImmediate(connId, data.Span))
        {
            return new ValueTask<int>(data.Length); // 零分配
        }

        // 需要异步发送
        var vts = _pool.Get();
        QueueForAsyncSend(connId, data, vts);
        return new ValueTask<int>(vts, vts.Version);
    }
}
```

---

## 五、自定义线程池：为游戏逻辑定制

### 为什么需要自定义线程池

```csharp
// .NET ThreadPool 的局限性：
// 1. 全局共享，网路 IO 和游戏逻辑共用工作线程
// 2. 线程注入策略不适合游戏帧率控制
// 3. 无法设置线程优先级（网络线程应高于 IO 线程）

// 游戏服务器需要的线程模型：
// [网络线程池]  — 处理 IOCP 完成，只做最轻量级数据解析
//    ↓ Channel
// [主逻辑线程]  — 游戏世界更新，单线程确保状态一致性
//    ↓ Channel
// [DB 线程池]   — 数据库操作，异步等待
//    ↓ Channel
// [发送线程池]  — 将结果写回网络 Socket
```

### 自定义 DedicatedThreadPool 实现

```csharp
class DedicatedThreadPool
{
    private readonly Thread[] _threads;
    private readonly Channel<Action> _workQueue;
    private readonly string _poolName;
    private volatile bool _stopped;

    public DedicatedThreadPool(int threadCount, string name,
        int queueCapacity = 10000, ThreadPriority priority = ThreadPriority.Normal)
    {
        _poolName = name;
        _workQueue = Channel.CreateBounded<Action>(
            new BoundedChannelOptions(queueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = false
            });

        _threads = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            _threads[i] = new Thread(WorkerLoop)
            {
                Name = $"{name}-{i}",
                IsBackground = true,
                Priority = priority
            };
            _threads[i].Start();
        }
    }

    // 游戏逻辑专用：支持分组提交，提升缓存亲和性
    public ValueTask Enqueue(Action work)
    {
        if (_stopped) return ValueTask.CompletedTask;
        _workQueue.Writer.TryWrite(work);
        return ValueTask.CompletedTask;
    }

    // 批量提交（同一帧的多个任务）
    public void EnqueueBatch(IEnumerable<Action> works)
    {
        var writer = _workQueue.Writer;
        foreach (var w in works)
            writer.TryWrite(w);
    }

    private void WorkerLoop()
    {
        // 设置线程亲缘性（NUMA node）
        // TrySetProcessorAffinity(processorId);

        var reader = _workQueue.Reader;
        while (!_stopped)
        {
            try
            {
                if (reader.TryRead(out var work))
                {
                    work();
                }
                else
                {
                    // 无任务时短暂休眠，避免 100% CPU
                    Thread.SpinWait(100);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[{PoolName}] 工作线程异常", _poolName);
            }
        }
    }

    public void Stop()
    {
        _stopped = true;
        foreach (var t in _threads)
        {
            if (t.IsAlive)
                t.Join(5000);
        }
    }
}

// 使用示例：游戏服务器线程模型
class GameServerWithCustomPools
{
    private readonly DedicatedThreadPool _networkPool;
    private readonly DedicatedThreadPool _dbPool;
    private readonly Thread _gameLoop;

    public GameServerWithCustomPools()
    {
        _networkPool = new DedicatedThreadPool(
            Environment.ProcessorCount, "Network",
            priority: ThreadPriority.AboveNormal);
        _dbPool = new DedicatedThreadPool(
            Math.Max(2, Environment.ProcessorCount / 2), "DB",
            priority: ThreadPriority.BelowNormal);
        _gameLoop = new Thread(GameLoop) { Name = "GameLogic" };
    }
}
```

---

## 六、线程亲缘性与 NUMA 感知

### NUMA 架构示意图

```
NUMA Node 0                    NUMA Node 1
┌──────────────────┐          ┌──────────────────┐
│ CPU 0  CPU 1     │          │ CPU 2  CPU 3     │
│ L1/L2 Cache      │          │ L1/L2 Cache      │
│      L3 Cache    │  互联    │      L3 Cache    │
│     内存控制器   │ ←──────→ │     内存控制器   │
│ 内存 (本地 32GB)  │          │ 内存 (本地 32GB)  │
└──────────────────┘          └──────────────────┘
 访问本地内存: ~100ns         访问远端内存: ~150ns
```

### C# 中设置线程亲缘性

```csharp
class NumaAwareScheduler
{
    // 设置线程到特定核心
    public static void PinCurrentThread(int coreIndex)
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        // 设置进程的处理器亲和性
        process.ProcessorAffinity = (IntPtr)(1L << coreIndex);
    }

    // NUMA 感知的任务分配
    public static int GetPreferredNode()
    {
        // 获取当前线程所在 NUMA 节点
        if (OperatingSystem.IsWindows())
        {
            // 通过 P/Invoke 调用 GetCurrentProcessorNumber
            return GetCurrentProcessorNumber() / Environment.ProcessorCount;
        }
        return 0;
    }

    // 使用 P/Invoke 获得精确的处理器编号
    [DllImport("kernel32.dll")]
    private static extern int GetCurrentProcessorNumber();

    // 游戏服务器推荐：
    // 网络线程绑定到 CPU 0-1（处理中断 + IOCP）
    // 主逻辑线程绑定到 CPU 2（独占）
    // DB 线程绑定到 CPU 3（跨 NUMA 通信最少）
    public static void BindGameThreads()
    {
        // 网络 IOCP 线程：CPU 0-1
        var networkMask = (1L << 0) | (1L << 1);

        // 主逻辑线程：CPU 2
        var logicMask = 1L << 2;

        // DB 线程：CPU 3
        var dbMask = 1L << 3;
    }
}
```

---

## 七、async/await 在游戏服务器中的陷阱

### 状态机开销

```csharp
// 陷阱 1：每次 await 都生成状态机
public async Task ProcessMessage(Message msg)
{
    // 这个 await 生成一个状态机
    var data = await _db.LoadData(msg.PlayerId);
    // 之后的状态恢复也需要执行

    // 如果不需要等待，不要 async
    // 修复：同步版本 + 仅在真正需要时 await
}

// 陷阱 2：捕获闭包变量
public async Task ProcessWithClosure(int playerId)
{
    // playerId 被捕获到状态机的字段中
    // 如果 ProcessMessage 在多个地方调用
    // 每次调用的状态机都包含 playerId 的副本

    await _db.LoadData(playerId);
    // 编译器生成的代码大致：
    // struct StateMachine { int playerId; ... }
}

// 陷阱 3：同步路径上的分配
public async ValueTask<bool> TryUpdateCache(long playerId, PlayerData data)
{
    // 如果 _cache.TryGetValue 返回 true，
    // ValueTask 包装已完成的任务，不分配
    // 但 async 方法即使同步完成也会分配状态机
    if (_cache.TryGetValue(playerId, out var existing))
    {
        existing.Update(data);
        return true; // 这里其实也分配了状态机！
    }

    // 真正需要异步
    await LoadFromDbAsync(playerId);
    return false;
}
```

### 避免状态机分配的技巧

```csharp
// 技巧 1：手动拆分同步/异步路径
public ValueTask<bool> TryUpdateEfficient(long playerId, PlayerData data)
{
    // 没有 async 关键字 → 不生成状态机
    if (_cache.TryGetValue(playerId, out var existing))
    {
        existing.Update(data);
        return new ValueTask<bool>(true); // 栈上 ValueTask
    }

    // 真正需要异步时返回 Task
    return new ValueTask<bool>(LoadAndUpdateAsync(playerId, data));
}

// 技巧 2：'async' 方法中避免被多次 await
// 把异步操作集中在同一个方法中，减少状态机数量
```

### async/await 在 TickLoop 中的使用

```csharp
class GameTickLoop
{
    // ❌ 错误：async void + await 导致主循环失控
    public async void Tick()
    {
        await ProcessNetwork(); // 这里等待 → 主循环停了
        UpdateGameState();
    }

    // ✅ 正确：同步 Tick + 异步操作排队
    public void Tick()
    {
        ProcessNetworkSync();   // Channel 读取，不等待
        UpdateGameState();
        _asyncOperations.ExecuteReady(); // 执行已经完成的操作
    }
}
```

---

## 八、Lock-free 环形缓冲区生产实践

### 完整 SPSC 无锁队列

```csharp
// 单生产者单消费者无锁队列
// 用于网络线程 → 逻辑线程的数据传递
public sealed class SpscRingBuffer<T> where T : unmanaged
{
    private readonly T[] _buffer;
    private readonly int _capacity;
    private readonly int _mask;

    // 使用 Padding 避免 False Sharing
    private Padding64 _pad1;
    private long _head;       // 读位置（只有消费者写）
    private Padding64 _pad2;
    private long _tail;       // 写位置（只有生产者写）
    private Padding64 _pad3;

    public SpscRingBuffer(int capacity)
    {
        // 必须是 2 的幂
        capacity = (int)BitOperations.RoundUpToPowerOf2((uint)capacity);
        _capacity = capacity;
        _mask = capacity - 1;
        _buffer = GC.AllocateUninitializedArray<T>(capacity, pinned: true);
    }

    public bool TryWrite(T item)
    {
        long tail = _tail;
        long head = Volatile.Read(ref _head); // 获取屏障

        if ((tail - head) >= _capacity)
            return false; // 队列满

        _buffer[tail & _mask] = item;
        Volatile.Write(ref _tail, tail + 1); // 释放屏障
        return true;
    }

    public bool TryRead(out T item)
    {
        long head = _head;
        long tail = Volatile.Read(ref _tail); // 获取屏障

        if (head >= tail)
        {
            item = default;
            return false; // 队列空
        }

        item = _buffer[head & _mask];
        Volatile.Write(ref _head, head + 1); // 释放屏障
        return true;
    }

    public int Count => (int)(_tail - _head);
    public bool IsEmpty => _head >= _tail;
    public bool IsFull => (_tail - _head) >= _capacity;
}

// 解决 False Sharing（伪共享）
// 当两个变量在同一个 Cache Line (64B) 时
// 一个核心修改 head 会 invalidate 另一个核心的 tail
struct Padding64
{
#pragma warning disable CS0169
    private long _p1, _p2, _p3, _p4, _p5, _p6, _p7;
#pragma warning restore CS0169
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| IOCP vs epoll | IOCP 完成通知零系统调用读，epoll 就绪通知需要额外 recv |
| Kestrel 演进 | libuv → 原生 Socket，性能提升 15-20% |
| Channel&lt;T&gt; | 高性能无锁生产者-消费者，替代手写 ConcurrentQueue + 信号量 |
| ValueTask | 值类型减少 GC，适合经常同步完成的操作 |
| 自定义线程池 | 网络/逻辑/DB 使用独立的 DedicatedThreadPool，避免互相干扰 |
| NUMA 感知 | 绑定线程到特定 CPU/内存节点，减少跨节点访问延迟 |
| async 陷阱 | 状态机分配、闭包捕获、TickLoop 中误用 |
| SPSC RingBuffer | 无锁环形缓冲 + Padding 防伪共享，极限性能 |

---

## 对照表：IOCP/线程模型 C++ vs C#

| C++ | C# | 说明 |
|-----|-----|------|
| `GetQueuedCompletionStatus` | `SocketAsyncEventArgs.Completed` | 事件驱动，C# 更上层 |
| `CreateIoCompletionPort` | `ThreadPool.BindHandle` | IOCP 创建，底层一致 |
| `pthread_setaffinity_np` | `Process.ProcessorAffinity` | 线程亲缘性设置 |
| 手动实现 SPSC 队列 | `System.Threading.Channels.Channel<T>` | C# 内置 |
| `numa_set_preferred` | P/Invoke `NumaSetNodeProcessorMask` | 都需要 P/Invoke |
| `boost::lockfree::spsc_queue` | 手写或用 `Channel<T>` | .NET 生态更成熟 |
