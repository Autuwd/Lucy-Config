# Day 01：Socket 与 TCP 网络编程 — 进阶深入

## 一、Nagle 算法与 TCP_NODELAY 优化

### Nagle 算法原理

Nagle 算法是为了解决小包问题（发送端每次 1 字节的数据包导致网络拥塞）。规则如下：

- 如果上次发送的包还没有 ACK，且有新数据要发，且新数据长度 < MSS，则延迟发送
- 延迟到收到 ACK 或积累到足够大的数据包

```
场景：游戏每帧发送位置更新（平均 30-60 字节位置包）
  无 Nagle：每帧立即发送 → 小包泛滥，网络利用率低
  有 Nagle：合并为更大的包发送 → 增加延迟，FPS 类游戏不可接受
```

### C# 中正确配置 TCP_NODELAY

```csharp
Socket serverSocket = new Socket(AddressFamily.InterNetwork,
    SocketType.Stream, ProtocolType.Tcp);

// 游戏服务器必须关闭 Nagle！减少延迟
serverSocket.NoDelay = true; // 等价于 setsockopt TCP_NODELAY

// 也可以按连接类型差异化配置
public class ConnectionConfig
{
    // 高频小包类型（位置同步、操作指令）→ NoDelay
    public static Socket ConfigureForRealtime(Socket s)
    {
        s.NoDelay = true;
        s.SendBufferSize = 8192;  // 小缓冲区减少延迟积压
        return s;
    }

    // 大包类型（地图加载、道具列表）→ 不强制 NoDelay
    public static Socket ConfigureForBulk(Socket s)
    {
        s.NoDelay = false; // 允许 Nagle 合并大包
        s.SendBufferSize = 65536; // 64KB 大缓冲区
        return s;
    }
}
```

### TCP_CORK（Linux）与 NoDelay 的对称关系

| 特性 | TCP_NODELAY | TCP_CORK |
|------|-------------|----------|
| 效果 | 禁止延迟发送 | 强制延迟发送直到"塞子"拔出 |
| 适用 | 交互式/实时应用 | 批量数据传输 |
| 设置 | `socket.NoDelay = true` | Linux 专用，C# 无直接封装 |
| C# 等价 | `SocketOptionName.NoDelay` | 通过 `setsockopt` P/Invoke |

### Socket Buffer 调优

```csharp
// 缓冲区调优经验值
class SocketBufferTuning
{
    // 发送缓冲区：通常 8KB-64KB
    // 接收缓冲区：通常 16KB-256KB
    public static void ApplyRecommendedBuffer(Socket s, bool isGameClient)
    {
        if (isGameClient)
        {
            // 客户端：小缓冲区，低延迟优先
            s.SendBufferSize = 8192;    // 8KB
            s.ReceiveBufferSize = 16384; // 16KB
        }
        else
        {
            // 服务器：大缓冲区，吞吐量优先
            s.SendBufferSize = 65536;    // 64KB
            s.ReceiveBufferSize = 262144; // 256KB
        }
    }

    // 检测缓冲区是否合适
    public static void CheckBufferUsage(Socket s)
    {
        // 通过 ioctl 获取缓冲区中待发送/待接收数据量
        int pendingSend = (int)s.GetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.SendBuffer);
        int pendingReceive = (int)s.GetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReceiveBuffer);

        // 如果待发送数据经常接近 BufferSize，说明需要增大
        // 如果待接收数据经常接近 0，说明可以减小
    }
}
```

---

## 二、IOCP 完成端口模型深度剖析

### Windows IOCP 实现原理

IOCP 不是简单的"IO 完成时回调"，它是一个内核对象 + 调度器的组合：

```
IOCP 核心数据结构：
  1. 完成队列（FIFO）—— 已完成的 IO 操作
  2. 等待线程队列 —— 调用 GetQueuedCompletionStatus 的线程
  3. 释放线程计数 —— 控制并发数

关键参数：NumberOfConcurrentThreads
  - 默认 = CPU 核数
  - 这个计数控制同时活跃的线程数
  - 多余线程被挂起，不会消耗 CPU 时间
```

```csharp
// .NET IOCP 内部机制（概念性代码，非精确实现）
internal class IocpThreadPool
{
    private readonly int _concurrencyLevel; // = Environment.ProcessorCount
    private int _currentRunning;            // 当前执行中的线程数

    // 入口：IOCP 完成包到达
    public void OnIoCompletion(CompletedIoPacket packet)
    {
        if (_currentRunning < _concurrencyLevel)
        {
            // 并发度未满，立即处理
            Interlocked.Increment(ref _currentRunning);
            ThreadPool.QueueUserWorkItem(() =>
            {
                ProcessPacket(packet);
                Interlocked.Decrement(ref _currentRunning);
            });
        }
        else
        {
            // 并发度已满，排队等待
            _pendingQueue.Enqueue(packet);
        }
    }
}
```

### 为什么 IOCP 比 epoll 在 Windows 上更高效

```
IOCP vs epoll 架构差异：

IOCP（Windows）：
  IO 请求 → 内核处理 → 完成包入队列 → 工作线程取出
  特点：应用程序只投递请求和接收完成，中间步骤完全由内核完成
  优势：真正异步，零用户态轮询

epoll（Linux）：
  IO 请求 → epoll_wait 返回就绪事件 → 应用程序发起读写 → IO 完成
  特点：事件通知是同步的（告诉"可以读"而不是"已经读完"）
  差异：epoll 是就绪通知模型，IOCP 是完成通知模型
```

| 特性 | IOCP (Windows) | epoll (Linux) |
|------|---------------|---------------|
| 通知类型 | 完成通知 | 就绪通知 |
| 数据拷贝 | 内核自动完成 | 用户态需要调用 read/write |
| 线程模型 | 并发度控制（精确） | 需要应用自己控制 |
| 内存管理 | 投递 buffer 由内核管理 | 应用管理 buffer |
| 零拷贝支持 | 是（TransmitFile） | 是（sendfile） |
| 连接数扩展性 | 万级到十万级 | 万级到百万级 |
| .NET 封装 | SocketAsyncEventArgs | SocketAsyncEventArgs + epoll |

---

## 三、Proactor vs Reactor 模式对比

### Reactor（反应器）模式

```csharp
// Reactor：IO 就绪时通知，用户自己读写
// 对应 epoll、select、poll
class ReactorPattern
{
    // 特点：
    // 1. 事件循环等待 IO 就绪
    // 2. 就绪后回调用户代码执行读写
    // 3. 读写操作仍同步进行

    public void EventLoop()
    {
        while (true)
        {
            var readyEvents = WaitForReadyEvents(); // epoll_wait
            foreach (var evt in readyEvents)
            {
                // 事件说"可以读了"，但还要自己读
                byte[] buffer = new byte[4096];
                int read = evt.Socket.Receive(buffer); // 同步读
                ProcessData(buffer, read);
            }
        }
    }
}
```

### Proactor（主动器）模式

```csharp
// Proactor：IO 完成后通知，用户直接处理数据
// 对应 IOCP
class ProactorPattern
{
    // 特点：
    // 1. 投递异步 IO 请求（WSARecv/ReadFile）
    // 2. 内核完成 IO 后将数据写入缓冲区
    // 3. 完成端口通知用户"数据已就绪"
    // 4. 用户直接处理缓冲区

    public void PostReceive(SocketAsyncEventArgs args)
    {
        // 投递异步读请求
        // 内核自己读取数据到 args.Buffer
        // 读完成后通过 IOCP 通知
        _socket.ReceiveAsync(args);
    }

    // 回调时数据已经在 Buffer 里了，直接处理
    public void OnReceiveCompleted(SocketAsyncEventArgs args)
    {
        // 无需再调 Receive()，数据已就绪
        ProcessMessage(args.Buffer, args.Offset, args.BytesTransferred);
    }
}
```

### 游戏服务器选择建议

| 场景 | 推荐模式 | 原因 |
|------|---------|------|
| Windows 专用游戏服 | Proactor (IOCP/SAEA) | 系统原生支持的异步模型 |
| 跨平台服务器 | Reactor (epoll/kqueue) | Linux 生态成熟 |
| 混合架构 | 上层 Reactor + 下层封装 | Kestrel 的做法 |
| Unity 客户端 | Reactor (async/await) | 主线程驱动 |

---

## 四、连接池管理的高级实现

### 连接对象池化

```csharp
// 不仅仅是 SocketAsyncEventArgs 池化
// 而是整个"连接上下文"池化
class ConnectionPool
{
    private readonly ConcurrentBag<ConnectionContext> _pool = new();
    private int _activeCount;
    private readonly int _maxConnections;

    public ConnectionPool(int maxConnections)
    {
        _maxConnections = maxConnections;
        PreWarm(100); // 预热 100 个连接上下文
    }

    private void PreWarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var ctx = new ConnectionContext
            {
                Buffer = GC.AllocateArray<byte>(4096, pinned: true), // 固定内存
                Args = new SocketAsyncEventArgs()
            };
            ctx.Args.SetBuffer(ctx.Buffer, 0, ctx.Buffer.Length);
            _pool.Add(ctx);
        }
    }

    public bool TryAcquire(out ConnectionContext ctx)
    {
        if (_pool.TryTake(out ctx))
        {
            Interlocked.Increment(ref _activeCount);
            ctx.Reset();
            return true;
        }

        // 池空但未达上限，创建新连接
        if (_activeCount < _maxConnections)
        {
            ctx = CreateNew();
            Interlocked.Increment(ref _activeCount);
            return true;
        }

        return false; // 连接数达上限
    }
}

class ConnectionContext
{
    public long ConnectionId;
    public Socket Socket;
    public byte[] Buffer;          // 固定缓冲区
    public SocketAsyncEventArgs Args;
    public DateTime LastActivity;
    public PlayerState PlayerData; // 上层状态
    private int _disposed;

    public void Reset()
    {
        ConnectionId = Interlocked.Increment(ref _nextId);
        LastActivity = DateTime.UtcNow;
        PlayerData = null;
        _disposed = 0;
    }

    // 无锁状态标记，避免并发 Dispose
    public bool TryMarkDisposed()
    {
        return Interlocked.CompareExchange(ref _disposed, 1, 0) == 0;
    }

    private static long _nextId;
}
```

---

## 五、TCP 优雅关闭

### 半关闭（Half-Close）与 Shutdown

```csharp
class GracefulShutdown
{
    private Socket _socket;
    private int _pendingWrites; // 待发送数据计数

    public void StartShutdown()
    {
        // 1. 停止接收新数据
        _socket.Shutdown(SocketShutdown.Receive);

        // 2. 等待剩余数据发送完成
        WaitForPendingWrites(TimeSpan.FromSeconds(5));

        // 3. 完全关闭
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }

    private void WaitForPendingWrites(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (Interlocked.Read(ref _pendingWrites) > 0 &&
               DateTime.UtcNow < deadline)
        {
            Thread.Sleep(10);
        }
    }

    // 服务端判断客户端是否已关闭
    public static bool IsConnectionClosed(Socket s)
    {
        try
        {
            // 通过 0 字节 Receive 检测连接状态
            return s.Poll(1000, SelectMode.SelectRead) && s.Available == 0;
        }
        catch (SocketException)
        {
            return true;
        }
    }
}
```

### Linger 选项

```csharp
// LingerOption 控制 Close 时的行为
// 默认情况下，Close 立即返回，但 TCP 在后台继续发送剩余数据

// 1. 优雅关闭：最多等待 5 秒
_socket.LingerState = new LingerOption(true, 5);

// 2. 强制关闭：立即丢弃未发送数据
_socket.LingerState = new LingerOption(true, 0);

// 3. 默认行为（LingerState = null）：后台继续发送
// 问题：进程退出时可能丢失数据

// 游戏服务器推荐：优雅关闭
public static void SafeClose(Socket s)
{
    if (s == null || !s.Connected) return;

    try
    {
        s.LingerState = new LingerOption(true, 3); // 最多等 3 秒
        s.Shutdown(SocketShutdown.Both);
        s.Close(3000); // Close 超时 3 秒
    }
    catch (Exception)
    {
        // 强制关闭
        s.LingerState = new LingerOption(false, 0);
        s.Close(0);
    }
}
```

---

## 六、TCP Keepalive vs 应用层心跳深度对比

### 应用层心跳的完整设计

```csharp
// 高级心跳管理器：自适应间隔 + 指数退避
class AdvancedHeartbeatManager
{
    private readonly ConcurrentDictionary<long, HeartbeatState> _states = new();
    private readonly int _baseInterval = 5000;   // 5秒基础间隔
    private readonly int _timeoutThreshold = 30000; // 30秒无响应断开
    private readonly int _maxMissed = 3;          // 连续丢失 3 次断开

    public void RecordHeartbeat(long connId)
    {
        if (_states.TryGetValue(connId, out var state))
        {
            state.MarkReceived(DateTime.UtcNow);
        }
    }

    public void CheckTimeouts()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _states)
        {
            var state = kv.Value;
            if (now - state.LastReceive > TimeSpan.FromMilliseconds(_timeoutThreshold))
            {
                // 指数退避：5s → 10s → 20s → 超时
                int missedCount = state.IncrementMissed();
                if (missedCount >= _maxMissed)
                {
                    Log.Warning("连接 {ConnId} 心跳超时，断开", kv.Key);
                    Disconnect(kv.Key);
                }
                else
                {
                    // 加快下一次心跳检测间隔
                    state.NextCheckInterval = _baseInterval * (1 << missedCount);
                }
            }
        }
    }
}

class HeartbeatState
{
    public DateTime LastReceive;
    public DateTime LastSend;
    public int MissedCount;
    public int NextCheckInterval = 5000;

    public void MarkReceived(DateTime time)
    {
        LastReceive = time;
        MissedCount = 0;          // 重置丢失计数
        NextCheckInterval = 5000; // 恢复基础间隔
    }

    public int IncrementMissed()
    {
        return Interlocked.Increment(ref MissedCount);
    }
}
```

### Keepalive vs 心跳选择决策树

```
连接是否需要心跳检测？
├── 是，且控制权在应用层 → 应用层心跳（推荐）
│   ├── 优点：完全可控、数据可携带额外信息、跨平台一致
│   └── 缺点：消耗带宽、需要协议支持
├── 是，但不想改协议 → TCP Keepalive
│   ├── 优点：系统层、零成本、协议无关
│   └── 缺点：参数依赖 OS、检测慢、不灵活
└── 否，短连接/可容忍延迟 → 都不需要
```

---

## 七、Socket 异步模式终极对比

### 三种异步模式实现

```csharp
// === 模式 1：APM（Begin/End）— .NET Framework 遗产 ===
class ApmServer
{
    private Socket _socket;

    public void StartReceive()
    {
        byte[] buffer = new byte[4096];
        // IAsyncResult 是对象，堆分配
        _socket.BeginReceive(buffer, 0, buffer.Length,
            SocketFlags.None, OnReceive, buffer);
    }

    private void OnReceive(IAsyncResult ar)
    {
        try
        {
            byte[] buffer = (byte[])ar.AsyncState;
            int bytesRead = _socket.EndReceive(ar);
            // 回调在同一 IOCP 线程执行
            Process(buffer, bytesRead);
            StartReceive(); // 继续接收
        }
        catch (ObjectDisposedException) { }
    }
}

// === 模式 2：TPL（Task）— 语法糖但非零开销 ===
class TplServer
{
    private Socket _socket;

    public async Task StartReceiveAsync()
    {
        byte[] buffer = new byte[4096];
        while (true)
        {
            // 每次调用生成 Task + 状态机
            int bytesRead = await _socket.ReceiveAsync(
                new ArraySegment<byte>(buffer), SocketFlags.None);
            // 这里继续执行，需要状态机恢复

            if (bytesRead == 0) break;
            Process(buffer, bytesRead);
        }
    }
}

// === 模式 3：SocketAsyncEventArgs — 零分配 ===
class SaeaServer
{
    private Socket _socket;
    // SAEA 在循环中复用，零分配
    private readonly SocketAsyncEventArgs _args;

    public void StartReceive()
    {
        _args.SetBuffer(0, 4096);
        if (!_socket.ReceiveAsync(_args))
        {
            // 同步完成，直接处理
            Process();
        }
        // 异步完成走 Completed 事件
    }

    private void Process()
    {
        if (_args.BytesTransferred > 0)
        {
            Process(_args.Buffer, _args.Offset, _args.BytesTransferred);
            if (!_socket.ReceiveAsync(_args))
                Process(); // 继续
        }
    }
}
```

### 性能与开销对比

| 维度 | APM (Begin/End) | TPL (async/await) | SocketAsyncEventArgs |
|------|----------------|-------------------|---------------------|
| 每次 IO 分配 | IAsyncResult (~48B) | Task (~56B) + 状态机 | 零分配（复用） |
| GC 压力 | 中 | 高 | 低 |
| 代码复杂度 | 回调嵌套 | 线性（await） | 事件驱动 |
| 异常处理 | 手动 EndXxx | try/catch 无缝 | 事件内处理 |
| IOCP 利用 | 是 | 间接 | 直接 |
| 适合场景 | 遗留代码维护 | 中等并发服务器 | 高性能游戏服务器 |

### 混合模式：SAEA + async/await 桥接

```csharp
// 在 SAEA 上层封装 awaitable 接口
class AwaitableSocketArgs : SocketAsyncEventArgs
{
    private Action _continuation;
    private ExecutionContext _context;

    protected override void OnCompleted(SocketAsyncEventArgs e)
    {
        var c = _continuation;
        if (c != null)
        {
            // 在原始执行上下文恢复 continuation
            _context?.Run(_ => c(), null);
        }
    }

    public SocketAwaitable ReceiveAsync(Socket socket)
    {
        _continuation = null;
        _context = ExecutionContext.Capture();

        if (!socket.ReceiveAsync(this))
        {
            // 同步完成，返回已完成 awaitable
            return SocketAwaitable.Completed(this);
        }
        return new SocketAwaitable(this);
    }
}

struct SocketAwaitable : INotifyCompletion
{
    private readonly AwaitableSocketArgs _args;

    public bool IsCompleted => _args.BytesTransferred > 0;
    public SocketAwaitable GetAwaiter() => this;
    public int GetResult() => _args.BytesTransferred;
    public void OnCompleted(Action continuation) =>
        _args._continuation = continuation;
}
```

---

## 八、高阶 Socket 技巧

### 接收缓冲区零拷贝

```csharp
// 使用 ArrayPool<byte> 避免临时分配
class ZeroCopyReceiver
{
    private readonly byte[] _buffer;
    private int _offset;

    // 从 SocketAsyncEventArgs 直接引用数据，
    // 不创建新的 byte[]，减少 GC
    public ReadOnlySpan<byte> GetData(SocketAsyncEventArgs args)
    {
        return new ReadOnlySpan<byte>(
            args.Buffer, args.Offset, args.BytesTransferred);
    }
}
```

### SO_REUSEADDR 与端口复用

```csharp
// 允许快速重启服务器（避免 TIME_WAIT 占用端口）
class PortReuse
{
    public static void Apply(Socket listenSocket)
    {
        // 允许绑定到 TIME_WAIT 状态的端口
        listenSocket.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress, true);
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| TCP_NODELAY | 关闭 Nagle 算法，游戏服务器必须设 true |
| IOCP 并发度 | 控制同时活跃线程数 = CPU 核数，避免过度切换 |
| Proactor vs Reactor | Windows IOCP 是 Proactor（完成通知），Linux epoll 是 Reactor（就绪通知） |
| 连接池化 | 不仅 SAEA 要池化，整个连接上下文都要预热和复用 |
| TCP 优雅关闭 | Shutdown(Receive) → 等发送完 → Close，配合 LingerOption |
| Keepalive 与心跳 | 应用层心跳做主力检测，TCP Keepalive 做保底 |
| 零分配 | SAEA 预分配缓冲区并复用，避免每次 IO 创建对象 |

---

## 对照表：C++ 游戏服务器 vs C# 进阶概念

| C++ 高性能服务器技巧 | C# 等价方案 | 差异点 |
|---------------------|-------------|-------|
| `setsockopt(TCP_NODELAY)` | `Socket.NoDelay = true` | 语法简化，效果相同 |
| `CreateIoCompletionPort` | `SocketAsyncEventArgs` | .NET 封装了所有 P/Invoke |
| 内存池 (tcmbench/jemalloc) | `ArrayPool<byte>` + SAEA 复用 | C# GC 管理内存，但大对象池化仍必要 |
| `accept()` 一次取一个 | `AcceptAsync` 连续投递 | C# 自动处理 accept 队列 |
| 线程亲缘性 (SetThreadAffinityMask) | 无直接等价 | 需要 P/Invoke 调用 Win32 API |
| `TransmitFile` 零拷贝 | `Socket.SendPacketsAsync` | 封装差异，底层相同 |
