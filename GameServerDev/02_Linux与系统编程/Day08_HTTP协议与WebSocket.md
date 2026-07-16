# Day 08：HTTP 协议与 WebSocket

## 一、HTTP 协议基础

### HTTP 请求-响应模型

```
Client → Server: GET /index.html HTTP/1.1
                 Host: www.example.com
                 
Server → Client: HTTP/1.1 200 OK
                 Content-Type: text/html
                 Content-Length: 128
                 
                 <html>...
```

### HTTP 方法

| 方法 | 用途 | 幂等 | 安全 |
|------|------|------|------|
| GET | 获取资源 | 是 | 是 |
| POST | 创建资源 | 否 | 否 |
| PUT | 全量更新 | 是 | 否 |
| PATCH | 部分更新 | 否 | 否 |
| DELETE | 删除资源 | 是 | 否 |
| HEAD | 仅获取头部 | 是 | 是 |
| OPTIONS | 查询支持方法 | 是 | 是 |

### HTTP 状态码

```
1xx: 信息 (101 Switching Protocols)
2xx: 成功 (200 OK, 201 Created, 206 Partial Content)
3xx: 重定向 (301 Moved, 302 Found, 304 Not Modified)
4xx: 客户端错误 (400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found)
5xx: 服务端错误 (500 Internal Server Error, 502 Bad Gateway, 503 Service Unavailable)
```

### HTTP 版本对比

| 版本 | 特点 | 连接复用 | 队头阻塞 | 二进制？ |
|------|------|---------|---------|---------|
| 1.0 | 每次请求新建连接 | 否 | 有 | 否 |
| 1.1 | Keep-Alive 长连接 | 是（串行） | 有 | 否 |
| 2.0 | 多路复用、HPACK 压缩 | 是（并行流） | 有（TCP 层） | 是 |
| 3.0 | QUIC (UDP) | 是 | 无 | 是 |

---

## 二、ASP.NET Core WebAPI

### 最小 API

```csharp
// dotnet new webapi

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// GET
app.MapGet("/api/players/{id}", (long id) =>
{
    var player = playerService.GetPlayer(id);
    return player is null ? Results.NotFound() : Results.Ok(player);
});

// POST
app.MapPost("/api/players", (CreatePlayerRequest req) =>
{
    var player = playerService.CreatePlayer(req);
    return Results.Created($"/api/players/{player.Id}", player);
});

// PUT
app.MapPut("/api/players/{id}", (long id, UpdatePlayerRequest req) =>
{
    playerService.UpdatePlayer(id, req);
    return Results.NoContent();
});

// DELETE
app.MapDelete("/api/players/{id}", (long id) =>
{
    playerService.DeletePlayer(id);
    return Results.NoContent();
});

app.Run();
```

### 控制器模式

```csharp
[ApiController]
[Route("api/[controller]")]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayerController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet("{id:long}")]
    public ActionResult<PlayerDto> Get(long id)
    {
        var player = _playerService.GetById(id);
        if (player == null)
            return NotFound(new { Message = "玩家不存在" });

        return Ok(player);
    }

    [HttpPost]
    public ActionResult<PlayerDto> Create([FromBody] CreatePlayerRequest req)
    {
        // 模型验证自动处理
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var player = _playerService.Create(req);
        return CreatedAtAction(nameof(Get), new { id = player.Id }, player);
    }

    [HttpPut("{id:long}/level")]
    public ActionResult LevelUp(long id, [FromBody] LevelUpRequest req)
    {
        _playerService.LevelUp(id, req.ExpGained);
        return NoContent();
    }
}
```

### 中间件 Pipeline

```csharp
var app = builder.Build();

// 中间件执行顺序
app.UseExceptionHandler();     // 1. 异常捕获（最外层）
app.UseAuthentication();       // 2. 身份认证
app.UseAuthorization();        // 3. 授权
app.UseRateLimiter();          // 4. 限流
app.MapControllers();          // 5. 路由到控制器

// 自定义中间件
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next(context);
    sw.Stop();

    Log.Information("{Method} {Path} {StatusCode} {Elapsed}ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        sw.ElapsedMilliseconds);
});
```

### 游戏服务器的 Web 接口

```csharp
[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    [HttpGet("status")]
    public ActionResult GetServerStatus()
    {
        return Ok(new
        {
            OnlinePlayers = Metrics.CurrentConnections,
            Uptime = (DateTime.Now - _startTime).ToString(@"dd\.hh\:mm\:ss"),
            MemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
            Tps = Metrics.GetTps()
        });
    }

    [HttpPost("shutdown")]
    public ActionResult Shutdown()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            Environment.Exit(0);
        });
        return Ok(new { Message = "服务器将在 1 秒后关闭" });
    }

    [HttpPost("broadcast")]
    public ActionResult Broadcast([FromBody] BroadcastRequest req)
    {
        ChatSystem.Broadcast(req.Message);
        return Ok(new { Count = Metrics.CurrentConnections });
    }

    [HttpGet("players/{id}/inventory")]
    public ActionResult GetPlayerInventory(long id)
    {
        // 从 Redis 获取在线玩家数据
        var inventory = _cache.GetPlayerInventory(id);
        return Ok(inventory);
    }
}
```

---

## 三、WebSocket 协议

### WebSocket 握手

WebSocket 通过 HTTP Upgrade 机制建立连接：

```
Client → Server:
GET /ws HTTP/1.1
Host: game.example.com
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==   ← 随机 base64
Sec-WebSocket-Version: 13

Server → Client:
HTTP/1.1 101 Switching Protocols
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=  ← 计算得到的
```

### 握手验证计算

```csharp
// 服务端验证 Sec-WebSocket-Key
public static string ComputeAcceptKey(string webSocketKey)
{
    // 固定 GUID
    const string MagicGuid = "258EAFA5-E914-47DA-95CA-5AB9DC11B85B";

    string combined = webSocketKey + MagicGuid;

    using var sha1 = System.Security.Cryptography.SHA1.Create();
    byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(combined));

    return Convert.ToBase64String(hash);
}
```

### WebSocket 帧格式

```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-------+-+-------------+-------------------------------+
|F|R|R|R| opcode|M| Payload len |    Extended payload length    |
|I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
|N|V|V|V|       |S|             |   (if payload len==126/127)   |
| |1|2|3|       |K|             |                               |
+-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - -+
|     Extended payload length continued, if payload len == 127  |
+ - - - - - - - - - - - - - - -+-------------------------------+
|                               |Masking-key, if MASK set to 1  |
+-------------------------------+-------------------------------+
| Masking-key (continued)       |          Payload Data         |
+-------------------------------+ - - - - - - - - - - - - - - -+
:                     Payload Data continued ...                :
+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
```

### OpCode

| OpCode | 含义 |
|--------|------|
| 0x0 | 继续帧（分段消息） |
| 0x1 | 文本帧 |
| 0x2 | 二进制帧 |
| 0x8 | 关闭连接 |
| 0x9 | Ping |
| 0xA | Pong |

---

## 四、ASP.NET Core WebSocket

### 服务端

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 启用 WebSocket
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// 连接管理器
class WebSocketConnectionManager
{
    private ConcurrentDictionary<string, WebSocket> _sockets = new();

    public string Add(WebSocket socket)
    {
        string id = Guid.NewGuid().ToString();
        _sockets.TryAdd(id, socket);
        return id;
    }

    public void Remove(string id)
    {
        _sockets.TryRemove(id, out _);
    }

    public async Task BroadcastAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);

        foreach (var (id, socket) in _sockets)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(segment,
                        WebSocketMessageType.Text,
                        true, CancellationToken.None);
                }
                catch
                {
                    Remove(id);
                }
            }
        }
    }

    public int Count => _sockets.Count;
}

var wsManager = new WebSocketConnectionManager();

app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("需要 WebSocket 连接");
        return;
    }

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    string clientId = wsManager.Add(socket);

    Log.Information("WebSocket 连接: {Id}", clientId);

    try
    {
        var buffer = new byte[4096];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);

            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Log.Information("WS 消息: {Msg}", msg);
                    // 处理游戏消息
                    await ProcessGameMessage(socket, msg);
                    break;

                case WebSocketMessageType.Binary:
                    // 二进制协议消息
                    byte[] data = new byte[result.Count];
                    Array.Copy(buffer, data, result.Count);
                    await ProcessBinaryMessage(socket, data);
                    break;

                case WebSocketMessageType.Close:
                    Log.Information("WebSocket 关闭: {Id}", clientId);
                    break;
            }
        }
    }
    finally
    {
        wsManager.Remove(clientId);
        if (socket.State != WebSocketState.Closed)
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "连接关闭",
                CancellationToken.None);
        }
    }
});

app.Run();
```

### 客户端 (C#)

```csharp
using System.Net.WebSockets;

class GameClient
{
    private ClientWebSocket _ws;
    private CancellationTokenSource _cts = new();

    public async Task ConnectAsync(string url)
    {
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri(url), _cts.Token);
        Console.WriteLine("WebSocket 连接成功");

        // 启动接收循环
        _ = ReceiveLoop();
    }

    public async Task SendAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await _ws.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            _cts.Token);
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[4096];
        try
        {
            while (_ws.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cts.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"收到: {msg}");
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "客户端关闭",
                        CancellationToken.None);
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"WebSocket 异常: {ex.Message}");
        }
    }

    public async Task DisconnectAsync()
    {
        _cts.Cancel();
        if (_ws?.State == WebSocketState.Open)
        {
            await _ws.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "主动断开",
                CancellationToken.None);
        }
    }
}
```

---

## 五、gRPC 基础

### 为什么游戏服务器可能需要 gRPC

- **服务器间通信**：网关→逻辑服→DB 服 之间 RPC
- **强类型接口**：.proto 文件定义，代码自动生成
- **HTTP/2 多路复用**：减少连接数
- **流式通信**：服务器流、客户端流、双向流

### .proto 定义

```protobuf
syntax = "proto3";

service GameService {
  // 一元 RPC
  rpc Login (LoginRequest) returns (LoginResponse);

  // 服务端流（推送）
  rpc SubscribeEvents (EventRequest) returns (stream GameEvent);

  // 双向流（实时通信）
  rpc GameStream (stream ClientMessage) returns (stream ServerMessage);
}
```

### C# 服务端

```csharp
// dotnet add package Grpc.AspNetCore

public class GameServiceImpl : GameService.GameServiceBase
{
    public override async Task<LoginResponse> Login(
        LoginRequest request, ServerCallContext context)
    {
        var player = await _playerService.Authenticate(
            request.Account, request.Password);

        return new LoginResponse
        {
            Token = player.Token,
            PlayerInfo = new PlayerInfo
            {
                PlayerId = player.Id,
                Name = player.Name,
                Level = player.Level
            }
        };
    }

    public override async Task SubscribeEvents(
        EventRequest request,
        IServerStreamWriter<GameEvent> responseStream,
        ServerCallContext context)
    {
        // 持续推送事件
        while (!context.CancellationToken.IsCancellationRequested)
        {
            var evt = await _eventQueue.DequeueAsync(
                context.CancellationToken);
            await responseStream.WriteAsync(evt);
        }
    }
}
```

---

## 六、游戏服务器 HTTP/WebSocket 对比

| | REST API | WebSocket | gRPC |
|--|---------|-----------|------|
| 协议 | HTTP/1.1 | WS | HTTP/2 |
| 双向 | 否（请求-响应） | 是（全双工） | 是（流） |
| 实时性 | 差（轮询） | 极好（推） | 好（流） |
| 序列化 | JSON | 自定义/Protobuf | Protobuf |
| 适用场景 | GM 管理后台 | 客户端-服务器实时通信 | 微服务间通信 |
| 连接数 | 短连接 | 长连接 | 长连接 |

**游戏服务器典型架构**：
```
客户端 ←WebSocket→ 网关服务器 ←gRPC→ 逻辑服务器 ←gRPC→ DB 服务器
                       ↓ HTTP
                  GM 管理后台 (Admin API)
```

---

## 七、对比 C++

```cpp
// C++ 用 Boost.Beast 实现 WebSocket
#include <boost/beast/websocket.hpp>

namespace beast = boost::beast;
namespace websocket = beast::websocket;

// 服务端
tcp::acceptor acceptor{ioc};
websocket::stream<tcp::socket> ws{ioc};
acceptor.accept(ws.next_layer());
ws.accept();

// 接收
beast::flat_buffer buffer;
ws.read(buffer);
std::cout << beast::buffers_to_string(buffer.data());

// 发送
ws.write(boost::asio::buffer("Hello"));

// C++ gRPC
class GameServiceImpl final : public GameService::Service {
    grpc::Status Login(grpc::ServerContext* context,
                       const LoginRequest* request,
                       LoginResponse* reply) override {
        reply->set_token("abc123");
        return grpc::Status::OK;
    }
};
```

### 对照表

| C# | C++ | 概念 |
|---|-----|------|
| `ASP.NET Core` | `Boost.Beast` | WebSocket 库 |
| `WebSocket` | `websocket::stream` | WebSocket 流 |
| `ClientWebSocket` | `beast::websocket::stream` (客户端) | 客户端 WebSocket |
| `Grpc.AspNetCore` | `grpc++` | gRPC 框架 |
| `MapGet/MapPost` | `beast::http::response` | HTTP 路由 |
| `WebSocketOptions.KeepAliveInterval` | `ws.auto_fragment` | WebSocket 保活 |

---

## 八、练习

1. **WebAPI 编写**：用 ASP.NET Core 写玩家 CRUD REST API
2. **WebSocket 聊天**：实现 WebSocket 聊天室，支持房间创建和加入
3. **握手分析**：用 Wireshark 抓 WebSocket 握手包，分析 Sec-WebSocket-Key 验证
4. **gRPC 通信**：实现一个 gRPC 双向流，模拟场景服务器推送
5. **协议对比报告**：比较在游戏服务器中使用 TCP Socket vs WebSocket vs gRPC 的优缺点

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| HTTP | 请求-响应协议，适合 GM 管理后台 |
| REST | 用 HTTP 方法操作资源 |
| WebSocket | HTTP 升级为全双工长连接，游戏实时通信首选 |
| gRPC | HTTP/2 + Protobuf，微服务间通信 |
| Upgrade | WebSocket 通过 HTTP 101 状态码建立 |
| 帧格式 | WebSocket 数据封装在帧里，支持分片和掩码 |
