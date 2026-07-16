# Day 08：HTTP 协议与 WebSocket — 进阶深入

## 一、HTTP/2 多路复用详解

### 流、帧、消息三层

HTTP/2 将 HTTP 拆为三层结构：

```
连接 (Connection)          TCP 连接
  ├── 流 (Stream)          虚拟通道，每请求/响应一对流
  │    ├── 帧 (Frame)      最小通信单元
  │    │   ├── HEADERS     请求头/响应头
  │    │   ├── DATA        请求体/响应体
  │    │   ├── SETTINGS    连接参数协商
  │    │   ├── PRIORITY    流优先级
  │    │   ├── RST_STREAM  取消流
  │    │   └── GOAWAY      连接关闭
  │    └── ...
  └── ...
```

```csharp
// C# 中创建 HTTP/2 连接（需要 TLS）
// HTTP/2 在不安全的连接上不能被协商
// 大多数浏览器和服务端只支持 TLS 上的 HTTP/2

var client = new HttpClient(new HttpClientHandler
{
    // 默认启用 HTTP/2
    // .NET 7+ 也支持 HTTP/3
});
// 或者手动指定版本
var request = new HttpRequestMessage(HttpMethod.Get, url);
request.Version = new Version(2, 0);  // HTTP/2
request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

var response = await client.SendAsync(request);
```

### 流优先级与依赖

```csharp
// HTTP/2 可以告诉服务器哪个资源更重要
// 游戏资源加载顺序:
// 1. login.proto（最高优先级）
// 2. config.json（中优先级）
// 3. assets.zip（低优先级）

// 服务器端在 Kestrel 中:
// Kestrel 自动处理 HTTP/2 流优先级
// 但 C# 开发者通常不需要关心
// 底层实现不会真的因为优先级而抢占带宽

// 实际上 HTTP/2 优先级在浏览器外很少用到
// 游戏服务器内部通信更常用 gRPC
```

### HPACK 头部压缩

```csharp
// HTTP/2 用 HPACK 压缩头部，减少冗余
// 游戏服务器 REST API 的头部开销:

// HTTP/1.1 请求头（约 300 字节）:
// GET /api/player/10086 HTTP/1.1
// Host: game.example.com
// User-Agent: UnityPlayer/2022.3
// Authorization: Bearer xxxx...xxx
// Accept: application/json

// HTTP/2 HPACK 压缩后（约 50 字节）:
// 静态表预定义 Host、:method GET 等
// 动态表缓存 Authorization

// 对游戏服务器意义:
// HTTP/1.1 请求头每请求 300 字节
// 10000 RPS = 3MB/s 纯头部开销
// HTTP/2 压缩到 50 字节 ≈ 0.5MB/s

// 这也是 gRPC 用 HTTP/2 的原因之一
```

### HTTP/2 Server Push

```csharp
// HTTP/2 服务器推送（已废弃）
// Google 在 2022 年宣布 Chrome 移除 Server Push 支持
// 原因: 推送的资源经常被缓存，浪费带宽

// 现代替代方案:
// 1. 103 Early Hints（服务端推荐资源）
// 2. 资源预加载链接头（Link preload header）

// 对游戏服务器: 不要用 Server Push
// 用 WebSocket 或 gRPC stream 推送更可靠
```

---

## 二、HTTP/3：QUIC 协议概述

### QUIC 解决了什么

| 问题 | TCP (HTTP/2) | QUIC / HTTP/3 |
|------|-------------|---------------|
| 连接建立 | 1.5 RTT (TCP+TLS) | 0 RTT（有缓存）/ 1 RTT |
| 队头阻塞 | 丢包阻塞所有流 | 流独立，丢包只影响自己 |
| 连接迁移 | 换 IP/WiFi 断连 | 连接 ID 不变，无缝迁移 |
| 握手延迟 | 每次新连接慢 | 缓存后秒连 |

```
TCP 连接迁移问题:
客户端 (WiFi) ──→ 服务器
   ↓ 切换到 4G
客户端 (4G) ──×  新的 IP，TCP 连接断开
需要重新建立 TCP + TLS → 至少 2 RTT 延迟

QUIC 连接迁移:
客户端 (WiFi) ──→ 服务器 (连接 ID: ABC)
   ↓ 切换到 4G
客户端 (4G) ──→ 服务器 (同样的连接 ID: ABC)
连接不中断，零延迟切换
```

### 游戏服务器的潜力

```csharp
// HTTP/3 对移动游戏特别有价值:
// - WiFi 切 4G 连接不中断
// - 弱网环境丢包不影响其他流
// - 0-RTT 重新连接

// C# 目前通过 .NET 7+ HttpClient 支持 HTTP/3
var handler = new SocketsHttpHandler
{
    // 必须显式启用 HTTP/3
    EnableMultipleHttp3Connections = true
};

var client = new HttpClient(handler);
var request = new HttpRequestMessage(HttpMethod.Get, url);
request.Version = new Version(3, 0);
request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

// 注意: 需要安装 msquic 或系统支持 QUIC
// Linux: apt install libmsquic
// 目前生产环境 HTTP/3 部署还不多
```

---

## 三、WebSocket permessage-deflate 压缩

### 手动实现压缩帧

```csharp
// WebSocket 帧可以有选择性压缩（permessage-deflate）
// 需要在握手协商:
// Sec-WebSocket-Extensions: permessage-deflate;
//   client_max_window_bits=15; server_max_window_bits=15

// 协商成功后，每个消息帧可以标记为压缩的

// ASP.NET Core 中启用压缩:
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebSockets(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    // ASP.NET Core 8+ 原生支持 permessage-deflate
    options.DangerousEnableCompression = true;
});

// 手动压缩大消息（游戏状态同步包 > 1KB）
using System.IO.Compression;

byte[] CompressMessage(string jsonMessage)
{
    var input = Encoding.UTF8.GetBytes(jsonMessage);
    using var output = new MemoryStream();

    // 使用 DeflateStream（不带 zlib 头）
    // WebSocket permessage-deflate 用 raw deflate
    using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
    {
        deflate.Write(input, 0, input.Length);
    }

    return output.ToArray();
}

// 解压
string DecompressMessage(byte[] compressed)
{
    using var input = new MemoryStream(compressed);
    using var output = new MemoryStream();
    using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
    {
        deflate.CopyTo(output);
    }
    return Encoding.UTF8.GetString(output.ToArray());
}
```

// 注意: 小消息（< 64 字节）压缩反而更大，只压缩 > 512 字节的消息

---

## 四、WebSocket Ping / Pong 与 Close 手握手

### Ping/Pong 心跳机制

```csharp
// WebSocket 控制帧:
// 0x9: Ping（服务端/客户端都可以发）
// 0xA: Pong（收到 Ping 自动回复，不经过应用层）

// 服务端设置心跳:
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebSockets(options =>
{
    // Kestrel 会自动发送 Ping 帧
    // 如果对端在超时内不回复 Pong → 断开
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
});

// 客户端自定义心跳:
using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://game-server/ws"), CancellationToken.None);

// 自己实现应用层心跳（双保险）
_ = Task.Run(async () =>
{
    while (ws.State == WebSocketState.Open)
    {
        // 每 10 秒发送应用层心跳
        var heartbeat = Encoding.UTF8.GetBytes("{\"type\":\"ping\"}");
        await ws.SendAsync(
            new ArraySegment<byte>(heartbeat),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);

        // 同时使用 WebSocket 层 Ping（C# ClientWebSocket 支持）
        await ws.SendAsync(
            new ArraySegment<byte>(Array.Empty<byte>()),
            WebSocketMessageType.Binary,
            true,
            CancellationToken.None);

        await Task.Delay(10000);
    }
});

// 注意: ClientWebSocket.SendAsync 不直接暴露 Ping 帧
// 上面的 Binary 消息会被当成数据而非 Ping
// 如果需要发送真正的 Ping 帧，得用底层 WebSocket 实现
```

### 关闭手握手

```csharp
// WebSocket 关闭需要 3 步握手:
// 1. A 发送 Close 帧 (opcode=0x8)
// 2. B 回复 Close 帧
// 3. A 关闭 TCP 连接

// 正确的关闭流程:
async Task CloseConnection(WebSocket socket, string reason)
{
    try
    {
        // 发送关闭帧，等待对端确认
        await socket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            reason,
            CancellationToken.None);
        // CloseAsync 内部完成完整关闭握手
    }
    catch (WebSocketException ex)
    {
        // 对端可能已经先关了
        Log.Warn($"WebSocket 关闭异常: {ex.Message}");
    }
    finally
    {
        socket.Dispose();
    }
}

// 关闭状态码:
// 1000: Normal Closure（正常关闭）
// 1001: Going Away（服务器关闭/客户端离开）
// 1002: Protocol Error（协议错误）
// 1003: Unsupported Data
// 1008: Policy Violation
// 1009: Message Too Big（消息太大）
// 1011: Internal Error（服务端内部错误）
```

---

## 五、gRPC 流模式深入

### 四种流模式

```protobuf
syntax = "proto3";

service GameService {
    // ── 一元 RPC ──
    // 标准请求-响应，类似 HTTP POST
    // 适用: 登录、注册、购买
    rpc Login(LoginRequest) returns (LoginResponse);

    // ── 服务端流 ──
    // 客户端发一个请求，服务端持续推送
    // 适用: 地图事件订阅、全服公告
    rpc SubscribeEvents(EventRequest) returns (stream GameEvent);

    // ── 客户端流 ──
    // 客户端持续发数据，服务端最终返回一个响应
    // 适用: 上传战斗录像、批量操作
    rpc UploadLogs(stream LogEntry) returns (UploadResponse);

    // ── 双向流 ──
    // 双方独立收发，全双工
    // 适用: 实时对战、MMO 位置同步
    rpc GameStream(stream ClientMessage) returns (stream ServerMessage);
}
```

### 双向流完整实现

```csharp
// 游戏服务器双向流: 每个玩家一个 stream
// 类似 WebSocket 但走 HTTP/2

public class GameServiceImpl : GameService.GameServiceBase
{
    private readonly PlayerManager _players;

    public override async Task GameStream(
        IAsyncStreamReader<ClientMessage> requestStream,
        IServerStreamWriter<ServerMessage> responseStream,
        ServerCallContext context)
    {
        // 获取玩家 ID（从 metadata 或第一个消息）
        string playerId = context.RequestHeaders
            .GetValue("player-id") ?? "unknown";

        // 注册玩家
        var player = _players.AddPlayer(playerId, responseStream);

        try
        {
            // 处理客户端发来的消息
            // 用 CancellationToken 支持超时断开
            var cts = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(30)); // 30 分钟超时

            // 异步双工处理:
            // 用 Task.Run 同时处理读和写

            // 读任务: 处理客户端消息
            var readTask = HandleClientMessages(requestStream, player, cts.Token);
            // 写任务: 由 _players 推送的事件（在外面触发）
            // 不需要在这里管

            await readTask;
        }
        catch (OperationCanceledException)
        {
            Log.Info($"玩家 {playerId} 连接超时");
        }
        catch (IOException ex)
        {
            Log.Warn($"玩家 {playerId} 连接断开: {ex.Message}");
        }
        finally
        {
            _players.RemovePlayer(playerId);
        }
    }

    private async Task HandleClientMessages(
        IAsyncStreamReader<ClientMessage> reader,
        Player player,
        CancellationToken ct)
    {
        await foreach (var message in reader.ReadAllAsync(ct))
        {
            switch (message.Type)
            {
                case MessageType.Move:
                    player.Position = message.Move.Position;
                    break;
                case MessageType.Chat:
                    _players.Broadcast(playerId, message.Chat.Text);
                    break;
                case MessageType.Heartbeat:
                    player.LastHeartbeat = DateTime.UtcNow;
                    break;
            }
        }
    }
}

// 在其他地方向玩家推送事件:
await _players.SendToPlayer(playerId, new ServerMessage
{
    Event = new GameEvent { Type = "monster_spawn", Data = monsterData }
});
```

### gRPC vs WebSocket 选型

```csharp
// 场景                           推荐
// ──────────────────────────────────────────────
// 实时对战（帧同步）              WebSocket（延迟最低）
// MMO 位置同步                   WebSocket（自定义协议）
// 聊天系统                        WebSocket（简单）
// 微服务间通信                    gRPC（强类型）
// 内部 RPC（服务调用）             gRPC（HTTP/2 多路复用）
// 移动端到服务器                  gRPC-Web / WebSocket

// 关键区别:
// - gRPC 基于 HTTP/2，所有流复用同一连接
//   适合微服务间通信（一个 Pod 内）
// - WebSocket 是独立 TCP 连接
//   适合客户端场景（需要穿透 NAT、代理）

// 游戏服务器典型架构:
// 客户端 ─WebSocket→ 网关（C++/C#）
// 网关  ──gRPC──→ 逻辑服（C#）
// 逻辑服 ──gRPC──→ DB 服（C#）
```

---

## 六、Kestrel 连接中间件管管道

### Kestrel 内部架构

```csharp
// Kestrel 是 ASP.NET Core 的 Web 服务器
// 它的连接管道比 HTTP 中间件更低层

// 连接中间件: 在 HTTP 请求解析之前处理
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // 监听配置
    options.Listen(IPAddress.Any, 8888, listenOptions =>
    {
        // 连接中间件（Connection Middleware）
        // 在 TLS 握手之后、HTTP 解析之前执行

        // 示例: 连接限流
        listenOptions.Use((next) =>
        {
            return async context =>
            {
                var remoteIp = context.RemoteEndPoint.Address;
                var connectionId = context.ConnectionId;

                Log.Info($"新连接: {remoteIp}:{context.RemoteEndPoint.Port}");

                // 可以在这里做 IP 白名单检查
                // 或连接速率限制

                await next(context);

                Log.Info($"连接关闭: {connectionId}");
            };
        });
    });

    // 连接级配置
    options.Limits.MaxConcurrentConnections = 10000;
    options.Limits.MaxConcurrentUpgradedConnections = 5000; // WebSocket
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});
```

### 自定义连接限制器

```csharp
// Kestrel 连接级限制，比应用层限流更早
// 在 connection middleware 中记录 IP → 连接数
// 超过 MaxPerIp 则拒绝新连接

var limiter = new ConcurrentDictionary<string, int>();

listenOptions.Use(async (context, next) =>
{
    var ip = context.RemoteEndPoint.ToString();
    limiter.AddOrUpdate(ip, 1, (_, c) => c + 1);
    try { await next(context); }
    finally { limiter.AddOrUpdate(ip, 0, (_, c) => Math.Max(0, c - 1)); }
});
```

---

## 七、HTTP 速率限制

### 固定窗口 vs 滑动窗口

```csharp
// ASP.NET Core 8+ 内置速率限制
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    // ── 全局策略: 每 10 秒最多 100 请求 ──
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(10)
            }));

    // ── 自定义策略: 不同 API 不同限制 ──
    // 在控制器上加 [EnableRateLimiting("game-api")]

    // 返回 429 Too Many Requests
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfter = context.Lease.TryGetMetadata(
            MetadataName.RetryAfter, out var retryAfterValue)
            ? retryAfterValue.TotalSeconds
            : 10;

        context.HttpContext.Response.Headers["Retry-After"] =
            ((int)retryAfterValue.TotalSeconds).ToString();

        Log.Warn($"速率限制触发: {context.HttpContext.Connection.RemoteIpAddress}");

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "请求太频繁",
            retryAfterSeconds = (int)retryAfterValue.TotalSeconds
        });
    };
});

var app = builder.Build();
app.UseRateLimiter();  // 在路由之前

// 按 API 分组限流
builder.Services.AddRateLimiter(options =>
{
    // 登录接口: 每 IP 每分钟 10 次
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    // 数据查询: 每 IP 每 30 秒 100 次
    options.AddFixedWindowLimiter("query", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromSeconds(30);
    });

    // GM 接口: 每 IP 每 10 秒 5 次（管理后台）
    options.AddFixedWindowLimiter("admin", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(10);
    });
});

// 在控制器上:
[EnableRateLimiting("login")]
[HttpPost("login")]
public async Task<ActionResult> Login(LoginRequest req) { ... }
```

### 令牌桶算法

```csharp
// 令牌桶: 每秒填充 100 个令牌，桶容量 200
// 突发 200 请求可以一次消化，之后速率降到 100/s
builder.Services.AddRateLimiter(options =>
{
    options.AddTokenBucketLimiter("game-api", opt =>
    {
        opt.TokenLimit = 200;
        opt.TokensPerPeriod = 100;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        opt.QueueLimit = 20;
        opt.AutoReplenishment = true;
    });
});

// 游戏服务器推荐:
// 登录: 令牌桶（允许突发），接口: 固定窗口（稳定），WebSocket 不限流
```

---

## 八、C++ gRPC 服务端

```cpp
// proto 生成 C++ 代码后用 grpc::Service 实现
// C++ gRPC 用 CompletionQueue 轮询（类似 epoll 的 cq 机制）

class GameServiceImpl final : public GameService::Service {
    grpc::Status GameStream(
        grpc::ServerContext*,
        grpc::ServerReaderWriter<ServerMessage, ClientMessage>* stream
    ) override {
        ClientMessage request;
        while (stream->Read(&request)) {
            ServerMessage reply;
            reply.mutable_event()->set_type("player_move");
            stream->Write(reply);
        }
        return grpc::Status::OK;
    }
};

// C++ vs C#:
// C++: CompletionQueue 轮询，手动管理线程
// C#: async/await 自动处理，Task 异步模型更方便
// 都基于 HTTP/2，协议兼容

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| HTTP/2 多路复用 | 一个 TCP 连接同时传输多个请求/响应 |
| HPACK | 静态+动态表压缩头部，减少冗余 |
| QUIC/HTTP/3 | UDP 上的 HTTP，0-RTT，无队头阻塞 |
| permessage-deflate | WebSocket 消息压缩，大消息省带宽 |
| Ping/Pong | WebSocket 心跳，框架自动回复 |
| gRPC 双向流 | 全双工流式通信，微服务间实时数据交换 |
| Kestrel 连接中间件 | HTTP 解析前的底层连接处理 |
| 速率限制 | 固定窗口/令牌桶，防御滥用 |
| 429 Retry-After | 通知客户端等待后重试 |

