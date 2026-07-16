# Day 02：高性能异步 IO 与线程模型

## 一、IO 模型概述

### 五种 IO 模型 (Unix)
1. **阻塞 IO** — 调用 recv 时线程休眠直到数据到达
2. **非阻塞 IO** — 立即返回，轮询直到数据就绪
3. **IO 多路复用** — select/poll/epoll 同时监控多个 fd
4. **信号驱动 IO** — SIGIO 通知
5. **异步 IO** — 内核完成所有操作后通知

### Windows 上的 IO 模型对比

| 模型 | 对应 .NET API | 线程模型 |
|------|--------------|---------|
| 同步阻塞 | Socket.Receive | 每个操作一个线程 |
| 异步 APM | BeginReceive/EndReceive | 线程池回调 |
| 事件异步 | SocketAsyncEventArgs | IOCP (推荐) |
| async/await | NetworkStream.ReadAsync | 线程池 + 状态机 |
| IO Completion Ports | (底层系统机制) | 完成端口 |

---

## 二、IOCP (I/O Completion Ports)

### 原理
IOCP 是 Windows 最高效的 IO 模型，核心思想：
- **并发内核对象** + **线程池** = 少量线程处理大量 IO
- 线程数 = CPU 核数（或 2x），避免上下文切换
- IO 完成后，系统自动将完成包投递到完成端口队列
- 工作线程从队列取完成包处理

### 工作流程
```
应用程序                 内核                    设备
   |                     |                      |
   |-- 创建 IOCP ------>|                      |
   |-- 关联 Socket ---->|                      |
   |-- 投递 Receive --->|                  发起 IO
   |                     |--- IRP -----------> 网卡
   |                     |                      |
   |                     |<-- 中断/完成 ------- 网卡
   |<-- 完成包入队 -----|                      |
   |-- GetQueued... ---->|                      |
   |<-- 处理完成包 -----|                      |
```

### 为什么 IOCP 比其他模型快

| 模型 | 线程数 | 上下文切换 | 适合场景 |
|------|-------|-----------|---------|
| 同步每连接一线程 | N 个连接 = N 个线程 | 极高 | < 100 连接 |
| select/poll | 1 个 + 处理逻辑 | 中 | < 1000 连接 |
| epoll (Linux) | N 个 worker | 低 | < 10000 连接 |
| IOCP (Windows) | CPU 核数个 | 最低 | 万级连接 |

### C# 中的 IOCP

```csharp
// SocketAsyncEventArgs 内部调用 Win32 API
// CreateIoCompletionPort → 创建 IOCP
// WSARecv → 投递异步接收
// GetQueuedCompletionStatus → 等待完成

// .NET 内部实现 (简化伪代码)
internal static extern SafeFileHandle CreateIoCompletionPort(
    SafeFileHandle FileHandle,
    SafeFileHandle ExistingCompletionPort,
    UIntPtr CompletionKey,
    uint NumberOfConcurrentThreads);

internal static extern unsafe int GetQueuedCompletionStatus(
    SafeFileHandle CompletionPort,
    out uint lpNumberOfBytes,
    out UIntPtr lpCompletionKey,
    out NativeOverlapped* lpOverlapped,
    uint dwMilliseconds);
```

---

## 三、生产者-消费者模式

服务器中最常见的线程协作模式。

### 接收队列

```csharp
class MessageQueue<T>
{
    private Queue<T> _queue = new Queue<T>();
    private readonly object _lock = new object();
    private SemaphoreSlim _semaphore = new SemaphoreSlim(0);

    // 生产者：网络线程收到消息后
    public void Enqueue(T message)
    {
        lock (_lock)
        {
            _queue.Enqueue(message);
        }
        _semaphore.Release(); // 通知消费者
    }

    // 消费者：主逻辑线程调用
    public bool TryDequeue(out T message, int timeoutMs = 100)
    {
        message = default;

        if (!_semaphore.Wait(timeoutMs))
            return false;

        lock (_lock)
        {
            message = _queue.Dequeue();
            return true;
        }
    }

    // 批量出队（减少锁竞争）
    public int TryDequeueBatch(out List<T> messages, int maxBatch = 100)
    {
        messages = new List<T>(maxBatch);

        lock (_lock)
        {
            while (_queue.Count > 0 && messages.Count < maxBatch)
                messages.Add(_queue.Dequeue());
            return messages.Count;
        }
    }
}

// 使用示例
class GameServer
{
    private MessageQueue<Packet> _packetQueue = new();
    private volatile bool _running = true;

    public void Start()
    {
        // 网络线程：一个或多个 Worker 接收数据
        Thread networkThread = new Thread(NetworkLoop);
        networkThread.Start();

        // 逻辑线程：主游戏逻辑
        Thread logicThread = new Thread(LogicLoop);
        logicThread.Start();
    }

    private void NetworkLoop()
    {
        while (_running)
        {
            // IOCP 回调中收到数据
            var packet = ReceiveFromSocket();
            _packetQueue.Enqueue(packet);
        }
    }

    private void LogicLoop()
    {
        while (_running)
        {
            if (_packetQueue.TryDequeue(out var packet, 16))
            {
                ProcessPacket(packet);
            }
            else
            {
                UpdateGameLogic(16); // 16ms 帧
            }
        }
    }
}
```

### 多生产者多消费者

```csharp
class ConcurrentBatchProcessor<T>
{
    private ConcurrentQueue<T> _queue = new();
    private AutoResetEvent _signal = new(false);
    private int _maxBatchSize;

    public ConcurrentBatchProcessor(int maxBatch = 100)
    {
        _maxBatchSize = maxBatch;
    }

    public void Add(T item)
    {
        _queue.Enqueue(item);
        _signal.Set();
    }

    public List<T> WaitAndDrain(int timeoutMs = 100)
    {
        _signal.WaitOne(timeoutMs);
        List<T> batch = new List<T>(_maxBatchSize);
        while (batch.Count < _maxBatchSize && _queue.TryDequeue(out var item))
            batch.Add(item);
        return batch;
    }
}
```

---

## 四、Actor 模型在游戏服务器中的使用

### 什么是 Actor
Actor 模型是一种并发编程模型，每个 Actor 是一个独立计算单元：
- 有自己的状态（私有数据）
- 通过消息通信（无共享内存）
- 单线程处理消息（无需锁）

### 为什么适合游戏服务器

| 问题 | Actor 解决方案 |
|------|-------------|
| 共享数据加锁 | 每个 Actor 私有状态，无需锁 |
| 死锁 | 无共享资源竞争 |
| 并发复杂度 | 顺序消息处理，天然事件驱动 |
| 调试困难 | 消息流可追踪 |

### C# Actor 框架简化实现

```csharp
abstract class Actor
{
    private Mailbox _mailbox = new Mailbox();
    private CancellationTokenSource _cts = new();
    private Task _processingTask;
    private string _id;

    public string Id => _id;

    protected Actor(string id)
    {
        _id = id;
        _processingTask = Task.Run(ProcessLoop);
    }

    public void Post(IMessage message)
    {
        _mailbox.Enqueue(message);
    }

    private async Task ProcessLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            IMessage message = await _mailbox.DequeueAsync(_cts.Token);
            await HandleMessage(message);
        }
    }

    protected abstract Task HandleMessage(IMessage message);

    public virtual void Stop()
    {
        _cts.Cancel();
    }
}

class Mailbox
{
    private Channel<IMessage> _channel =
        Channel.CreateUnbounded<IMessage>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });

    public void Enqueue(IMessage message)
    {
        _channel.Writer.TryWrite(message);
    }

    public async Task<IMessage> DequeueAsync(CancellationToken ct)
    {
        return await _channel.Reader.ReadAsync(ct);
    }
}

// 具体 Actor：玩家
class PlayerActor : Actor
{
    private PlayerData _data;
    private InventoryComponent _inventory;

    public PlayerActor(string playerId) : base(playerId)
    {
        _data = LoadFromDb(playerId);
        _inventory = new InventoryComponent(_data);
    }

    protected override async Task HandleMessage(IMessage message)
    {
        switch (message)
        {
            case MoveCommand cmd:
                await HandleMove(cmd);
                break;
            case UseItemCommand cmd:
                await HandleUseItem(cmd);
                break;
        }
    }

    private async Task HandleMove(MoveCommand cmd)
    {
        // Actor 内部状态可以直接读写，无需锁
        _data.Position = cmd.NewPosition;
        // 通知场景 Actor
        ActorSystem.Send("SceneServer", new PositionUpdate(Id, cmd.NewPosition));
    }
}

// Actor 系统：管理所有 Actor
class ActorSystem
{
    private static ConcurrentDictionary<string, Actor> _actors = new();

    public static T GetOrCreate<T>(string id) where T : Actor
    {
        return (T)_actors.GetOrAdd(id, _ =>
        {
            var actor = (T)Activator.CreateInstance(typeof(T), id);
            return actor;
        });
    }

    public static void Send(string actorId, IMessage message)
    {
        if (_actors.TryGetValue(actorId, out var actor))
            actor.Post(message);
    }
}
```

### Actor 模型的权衡
- **优点**：天然无锁，消息驱动，易于分布式
- **缺点**：跨 Actor 操作慢（消息复制+队列延迟），不适合 CPU 密集计算
- **适用场景**：MMO 服务器中每个玩家/场景是一个 Actor

---

## 五、async/await 在服务器中的使用

### 避免 async void

```csharp
// 错误：async void 异常会直接崩溃进程
public async void HandlePacket(Packet p)
{
    await Process(p); // 如果抛异常，进程崩溃！
}

// 正确：返回 Task，异常被捕获
public async Task HandlePacket(Packet p)
{
    await Process(p);
}

// 如果不关心 await，也要用 Task.Run 包装
public void FireAndForget(Func<Task> action)
{
    Task.Run(async () =>
    {
        try { await action(); }
        catch (Exception e) { LogError(e); }
    });
}
```

### SynchronizationContext 陷阱

```csharp
// ASP.NET Core / Console 应用的 SynchronizationContext 是 null
// 所以 ConfigureAwait(false) 在这里不需要（默认就无上下文）
// 但 Unity 的 UnitySynchronizationContext 需要

// 推荐：写服务器代码一律用 ConfigureAwait(false)
await someTask.ConfigureAwait(false);
```

### ValueTask 优化

```csharp
// Task 是引用类型，每次分配堆对象
async Task<int> GetIntAsync() { return 42; }

// ValueTask 是值类型，可能不分配堆
async ValueTask<int> GetIntFastAsync()
{
    // 如果结果已经缓存，同步返回
    if (_cached) return _cachedValue;
    return await LoadFromDbAsync();
}

// 游戏服务器中大量短期异步操作时用 ValueTask 减少 GC
```

---

## 六、线程池调优

### .NET 线程池参数

```csharp
// 查看当前线程池设置
ThreadPool.GetMinThreads(out int minWorker, out int minIOCP);
ThreadPool.GetMaxThreads(out int maxWorker, out int maxIOCP);

Console.WriteLine($"Worker: min={minWorker}, max={maxWorker}");
Console.WriteLine($"IOCP: min={minIOCP}, max={maxIOCP}");

// 调整（在服务器启动时设置）
ThreadPool.SetMinThreads(4, 4);     // 至少 4 个工作线程 + 4 个 IOCP 线程
ThreadPool.SetMaxThreads(64, 64);   // 最多 64

// 建议：
// 工作线程数 = CPU 核数 * 2
// IOCP 线程数 = CPU 核数
```

### 线程池饥饿

```csharp
// 饥饿：所有线程都在等待 Task 完成
for (int i = 0; i < 100; i++)
{
    Task.Run(async () =>
    {
        await Task.Delay(1000); // 线程释放回池
        // 但下面的操作要等线程池分配线程
        await SomeWorkAsync();
    });
}

// 解决方案：增加最小线程数
ThreadPool.SetMinThreads(64, 64);
```

---

## 七、对比客户端 Unity 线程模型

| | Unity (客户端) | 游戏服务器 |
|--|--------------|-----------|
| 主线程 | 渲染 + MonoBehaviour | 游戏逻辑循环 |
| IO 线程 | WebRequest、Addressables | 网络收发（IOCP/epoll） |
| 工作线程 | Job System、异步加载 | 业务逻辑处理 |
| 同步上下文 | UnitySynchronizationContext | 无 (null) |
| 锁竞争 | 低（大部分在主线程） | 高（多线程访问数据） |

---

## 八、练习

1. **消息队列实现**：实现一个多生产者单消费者的锁安全队列
2. **Actor 聊天室**：用 Actor 模型实现一个聊天室，每个用户是一个 Actor
3. **async/await 陷阱检测**：给出 5 个错误的 async 用法，让主人找问题
4. **IOCP vs epoll 对比报告**：阅读资料，写一个对比表（Windows IOCP vs Linux epoll）
5. **线程池压测**：写一个 Server，分别用同步/async/SAEA 接收 1000 个并发连接，对比 CPU/内存

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| IOCP | Windows 完成端口模型，少量线程处理大量 IO |
| SocketAsyncEventArgs | .NET 对 IOCP 的封装，高性能服务器基石 |
| 生产者-消费者 | 网络线程入队 → 逻辑线程出队，解耦两线程 |
| Actor 模型 | 每个实体是独立消息驱动的单元，天然无锁 |
| async/await | 语法糖，服务器中用 ConfigureAwait(false) |
| 线程池调优 | 最小线程数避免饥饿，最大线程数限制资源 |
