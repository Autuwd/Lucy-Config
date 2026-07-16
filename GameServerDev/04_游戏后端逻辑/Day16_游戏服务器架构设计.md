# Day 16：游戏服务器架构设计

## 一、游戏服务器架构演进

### 单进程架构（简单游戏）

```
                     [Game Server]
                    /              \
        Client 1 ──┘              └── Client N
        (tcp/ws)                   (tcp/ws)
        
 特点：所有逻辑在一个进程
 优点：简单，无网络开销
 缺点：不能扩容，崩了全部下线
 适合：简单卡牌、单服游戏
```

### 多进程架构（MMO 标准）

```
                    [Gateway]
                   /    |    \
                  /     |     \
          [Game1]  [Game2]  [Game3]  ← 场景/房间服务器
                  \     |     /
                   \    |    /
                    [DBServer] ← 数据库服务
                       |
                    [MySQL] [Redis]
  
 特点：按功能拆分进程
 优点：可独立扩容，模块隔离
 缺点：进程间通信需要序列化
 适合：MMO、MOBA、吃鸡
```

### 微服务架构（大规模分布式）

```
                     [Load Balancer]
                            |
                      [Gateway Cluster]
                     /    |     |    \
              [Login] [Scene] [Social] [Match]
                 |       |       |       |
              [DB]     [DB]    [DB]     [DB]
                 \      |       |      /
                  [Message Queue / Redis]
                       
 特点：每个服务独立部署
 优点：弹性伸缩，独立发布
 缺点：运维复杂，延迟增加
 适合：千万级 DAU 的大作
```

---

## 二、网关服务器

### 网关的作用

```
网关承担四个职责：
1. 连接管理 — 维护海量 TCP/WebSocket 连接
2. 协议转换 — 外部协议 → 内部 RPC
3. 负载均衡 — 转发到合适的游戏服务器
4. 安全防护 — 封包校验、频率限制、DDoS 防护
```

### 网关实现

```csharp
class GatewayServer
{
    private readonly Dictionary<long, ServerSession> _gameServers = new();
    private readonly ConcurrentDictionary<long, ClientSession> _clients = new();

    // 注册游戏服务器
    public void RegisterGameServer(int serverId, string host, int port)
    {
        var session = new ServerSession(serverId, host, port);
        _gameServers[serverId] = session;
        session.OnDisconnect += () => _gameServers.Remove(serverId);
    }

    // 客户端连接
    public async Task OnClientConnected(WebSocket ws)
    {
        var client = new ClientSession(ws);
        string clientId = Guid.NewGuid().ToString();

        // 等待客户端发送登录认证
        var loginPacket = await client.ReceivePacketAsync();
        var playerId = await Authenticate(loginPacket);

        client.PlayerId = playerId;
        _clients.TryAdd(playerId, client);

        // 将客户端绑定到游戏服务器
        var gameServer = SelectGameServer(playerId);
        client.BindServer(gameServer);

        // 开始转发
        _ = ForwardClientToServer(client);
        _ = ForwardServerToClient(client);
    }

    // 转发客户端消息到游戏服务器
    private async Task ForwardClientToServer(ClientSession client)
    {
        while (client.Connected)
        {
            var packet = await client.ReceivePacketAsync();
            if (packet == null) break;

            // 统一添加玩家 ID
            packet.SetPlayerId(client.PlayerId);
            await client.BoundServer.SendAsync(packet);
        }
    }

    // 选择游戏服务器（按玩家 ID 哈希）
    private ServerSession SelectGameServer(long playerId)
    {
        int index = (int)(playerId % _gameServers.Count);
        return _gameServers.Values.ElementAt(index);
    }

    // 限流检查
    public bool CheckRateLimit(long playerId)
    {
        var client = _clients.GetValueOrDefault(playerId);
        return client?.CheckRateLimit() ?? false;
    }
}
```

### 网关 vs 直连

| | 直连游戏服务器 | 通过网关 |
|--|-------------|---------|
| 客户端配置 | 需要知道游戏服务器 IP | 只知道网关 IP |
| 隐藏后端 | 暴露内部服务器 | 网关统一入口 |
| 水平扩展 | 客户端需要重新连接 | 网关透明转接 |
| 切换服务器 | 重连 | 网关内部转发 |
| 流量整形 | 无 | 可以限流/过滤 |
| 维护 | 维护时影响用户 | 网关支持无损维护 |

---

## 三、消息路由与转发

### 消息类型

```csharp
public enum MessageRoute
{
    ClientToServer,   // 客户端 → 网关 → 游戏服务器
    ServerToClient,   // 游戏服务器 → 网关 → 客户端
    ServerToServer,   // 游戏服务器 ← → 游戏服务器
    GameToDB,        // 游戏服务器 → 数据库服务器
}

// 消息包装
public class GamePacket
{
    public ushort MsgId { get; set; }
    public long PlayerId { get; set; }
    public long SeqId { get; set; }
    public RouteTarget Target { get; set; }
    public byte[] Body { get; set; }
}

public class RouteTarget
{
    public ServerType ServerType { get; set; } // Game/Scene/Social/DB
    public int ServerId { get; set; }          // 具体服务器 ID
    public long PlayerId { get; set; }         // 目标玩家（可选）
}
```

### 消息路由实现

```csharp
class MessageRouter
{
    private readonly Channel<GamePacket> _internalQueue =
        Channel.CreateUnbounded<GamePacket>();

    public async Task Dispatch(GamePacket packet)
    {
        switch (packet.Target.ServerType)
        {
            case ServerType.Game:
                // 路由到具体的游戏服务器
                await RouteToGameServer(packet);
                break;

            case ServerType.Scene:
                await RouteToSceneServer(packet);
                break;

            case ServerType.DB:
                await _internalQueue.Writer.WriteAsync(packet);
                break;

            case ServerType.Client:
                await RouteToClient(packet);
                break;
        }
    }

    private async Task RouteToGameServer(GamePacket packet)
    {
        // 确定目标游戏服务器
        int serverId = packet.Target.ServerId > 0
            ? packet.Target.ServerId
            : (int)(packet.PlayerId % _gameServers.Count);

        if (_gameServers.TryGetValue(serverId, out var server))
        {
            await server.SendAsync(packet);
        }
    }
}
```

---

## 四、场景/房间服务器

### 为什么需要场景服务器

```
MMO 中玩家分布在各个地图/副本
每个场景的玩家只关心场景内的消息

场景服务器的职责：
- 玩家移动同步
- NPC/AI 更新
- 战斗结算
- 场景内广播
```

### 场景服务器设计

```csharp
class SceneServer
{
    public int SceneId { get; }
    public int MapId { get; }
    public List<Player> Players { get; } = new();

    // AOI (Area Of Interest) 兴趣区域管理
    private readonly AoiManager _aoi;

    public void Update(float deltaTime)
    {
        // 更新场景内所有实体
        foreach (var player in Players)
        {
            player.Update(deltaTime);
        }

        // 更新 NPC
        UpdateNPCs(deltaTime);

        // AOI 检测（玩家附近的其他玩家/NPC）
        var aoiChanges = _aoi.Update();
        foreach (var change in aoiChanges)
        {
            if (change.Type == AoiChangeType.Enter)
            {
                // 通知玩家有新实体进入视野
                SendSpawnPacket(change.Entity);
            }
            else if (change.Type == AoiChangeType.Leave)
            {
                // 通知玩家有实体离开视野
                SendDespawnPacket(change.Entity);
            }
        }
    }

    // 场景内广播（附近的玩家收到）
    public void Broadcast(byte[] packet, Vector3 position, float radius)
    {
        var nearby = _aoi.Query(position, radius);
        foreach (var player in nearby)
        {
            player.Send(packet);
        }
    }
}
```

### 玩家切换场景

```csharp
class SceneManager
{
    private readonly ConcurrentDictionary<int, SceneServer> _scenes = new();

    public async Task SwitchScene(long playerId, int fromSceneId, int toSceneId)
    {
        // 1. 离开旧场景
        if (_scenes.TryGetValue(fromSceneId, out var oldScene))
        {
            await oldScene.RemovePlayer(playerId);
        }

        // 2. 加载玩家数据（可能有跨服数据）
        var playerData = await LoadPlayerData(playerId);

        // 3. 进入新场景
        if (_scenes.TryGetValue(toSceneId, out var newScene))
        {
            await newScene.AddPlayer(playerId, playerData);

            // 通知客户端切换成功
            SendToClient(playerId, MsgId.SceneEnterResponse, new
            {
                SceneId = toSceneId,
                Position = newScene.GetSpawnPoint(),
                NearbyPlayers = newScene.GetPlayerList(playerId)
            });
        }

        // 4. 释放旧场景资源
        if (oldScene?.PlayerCount == 0)
        {
            // 空场景可以考虑卸载
        }
    }
}
```

---

## 五、DB 服务器

### 为什么需要独立的 DB 服务

```
问题：游戏服务器直接操作 MySQL，连接数爆炸
      每个游戏服务器都开连接池，DB 连接数 = 服务数 × 池大小
      
方案：DB 服务器统一管理数据库操作
      游戏服务器通过 RPC 请求 DB 操作
```

### DB 服务实现

```csharp
class DbServer
{
    private readonly Channel<DbRequest> _requestQueue =
        Channel.CreateBounded<DbRequest>(10000);

    // 游戏服务器调用的方法
    public async Task<T> QueryAsync<T>(string sql, object param)
    {
        var tcs = new TaskCompletionSource<T>();
        var request = new DbRequest
        {
            Sql = sql,
            Param = param,
            TaskSource = tcs
        };

        await _requestQueue.Writer.WriteAsync(request);
        return await tcs.Task;
    }

    // DB 工作线程（单线程处理，避免并发问题）
    public async Task Run(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var request = await _requestQueue.Reader.ReadAsync(ct);

            try
            {
                // 等待上一个操作完成（数据库操作排队）
                _lastOperation = _lastOperation.ContinueWith(_ =>
                {
                    using var conn = new MySqlConnection(_connectionString);
                    conn.Open();
                    var result = conn.Query(request.Sql, request.Param);
                    ((TaskCompletionSource)request.TaskSource).SetResult(result);
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DB 操作失败: {Sql}", request.Sql);
                // 重试或返回错误
                ((TaskCompletionSource)request.TaskSource).SetException(ex);
            }
        }
    }

    // 批量写入优化
    private readonly List<DbRequest> _batchBuffer = new();
    private Timer _flushTimer;

    public async Task FlushBatch()
    {
        List<DbRequest> batch;
        lock (_batchBuffer)
        {
            batch = new List<DbRequest>(_batchBuffer);
            _batchBuffer.Clear();
        }

        if (batch.Count == 0) return;

        // 合并 INSERT 语句
        // INSERT INTO log_table VALUES (...), (...), ...
        var combined = CombineInserts(batch);
        using var conn = new MySqlConnection(_connectionString);
        await conn.ExecuteAsync(combined);
    }
}
```

---

## 六、架构对比

| 架构 | 连接数上限 | 可扩展性 | 运维复杂度 | 典型游戏 |
|------|----------|---------|-----------|---------|
| 单进程 | ~5000 | 不可扩 | 低 | 单服卡牌 |
| 网关+多场景 | ~50000 | 场景维可扩 | 中 | 普通 MMO |
| 微服务集群 | 百万+ | 全维度可扩 | 高 | 王者/原神 |

### 游戏服务器硬件参考

```yaml
小游戏 (< 1000 同时在线):
  CPU: 4 核
  内存: 8GB
  带宽: 100Mbps

中型 MMO (< 10000 同时在线):
  网关: 4C8G × 2
  场景服: 8C16G × 4
  DB 服: 8C32G × 2
  MySQL: 16C64G

大型 MMO (< 50000 同时在线):
  网关: 4C8G × 8
  场景服: 8C32G × 20
  微服务: 各自集群
  MySQL: 主从集群
  Redis: Cluster
```

---

## 七、练习

1. **网关设计**：实现一个简单的网关，转发客户端消息到后端服务器
2. **场景服设计**：设计 AOI（兴趣区域）算法，决定玩家应该收到哪些实体的更新
3. **消息路由**：设计消息 ID 到处理器的方法路由
4. **服务器心跳**：实现网关对后端服务器的健康检查
5. **架构对比**：分析你玩的某一款游戏可能使用的服务器架构

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 网关 | 连接管理+协议转换+负载均衡 |
| 场景服务器 | 管理一个地图/副本内的所有玩家和 NPC |
| AOI | 只给玩家发送附近实体的更新 |
| DB 服务器 | 统一管理数据库操作，减少连接 |
| 消息路由 | 根据消息类型分发到正确的处理服务 |
| 水平扩展 | 加服务器实例提高并发能力 |
