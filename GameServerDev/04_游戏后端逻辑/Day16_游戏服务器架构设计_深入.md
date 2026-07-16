# Day 16：游戏服务器架构设计 — 进阶深入

## 一、微服务拆分的游戏领域实践

### 按游戏域拆分的服务粒度

大型游戏不适合按技术层拆分（Controller/Service/DAO），应该按游戏业务域拆分：

```
游戏微服务边界示例：

LoginService      — 账号注册、认证、Token管理
PlayerService     — 角色创建、属性、升级、装备
SceneService      — 地图、AOI、NPC、怪物AI
BattleService     — 战斗结算、伤害计算、Buff
SocialService     — 好友、公会、聊天、邮件
MatchService      — 匹配队列、ELO、天梯
RankService       — 排行榜、赛季结算
ShopService       — 商城、充值、订单
LogService        — 行为日志、统计分析
```

### 服务间调用策略

```csharp
// 场景1: 同步调用（玩家升级需要通知排行榜）
// 用 gRPC 直连，超时短（<500ms）
public class PlayerService
{
    private readonly RankService.RankServiceClient _rankClient;

    public async Task LevelUp(long playerId)
    {
        var player = await GetPlayer(playerId);
        player.Level++;
        player.Exp = 0;

        // 同步通知排行榜更新
        await _rankClient.UpdateRankAsync(new RankUpdateRequest
        {
            PlayerId = playerId,
            Score = CalculateRankScore(player)
        });
    }
}

// 场景2: 异步事件（装备强化需要日志记录）
// 用消息队列解耦
public class EquipService
{
    private readonly IMessageProducer _producer;

    public async Task UpgradeEquip(long playerId, int equipId)
    {
        // 强化逻辑...

        // 发布事件，不关心谁消费
        await _producer.PublishAsync("equip.upgraded", new EquipUpgradedEvent
        {
            PlayerId = playerId,
            EquipId = equipId,
            NewLevel = newLevel,
            Cost = cost
        });
    }
}

// 日志服务异步消费
public class LogConsumer
{
    [EventHandler("equip.upgraded")]
    public async Task OnEquipUpgraded(EquipUpgradedEvent evt)
    {
        await _db.ExecuteAsync(
            "INSERT INTO equip_upgrade_logs VALUES (@P, @E, @L, @C, NOW())",
            new { P = evt.PlayerId, E = evt.EquipId, L = evt.NewLevel, C = evt.Cost });
    }
}
```

### 服务拆分原则

```
拆分决策树：
  1. 是否有独立的伸缩需求？
     是 → 拆（场景服需要扩缩容，账号服不需要）
  2. 是否可以用不同的技术栈？
     是 → 拆（聊天服务可以用Node.js，战斗服必须C#）
  3. 数据是否独立？
     是 → 拆（每个服务有自己的DB schema）
  4. 失败是否应该隔离？
     是 → 拆（聊天服务挂了不能影响战斗）
  5. 调用延迟是否可以容忍？
     否 → 合（战斗内紧密逻辑不拆分）
```

---

## 二、网关服务器：连接迁移与会话交接

### 连接迁移（Connection Migration）

玩家断线重连时，网关需要将连接从旧的网关节点迁移到新节点：

```csharp
public class SessionManager
{
    private readonly IDatabase _redis;
    private readonly ConcurrentDictionary<string, GameSession> _localSessions = new();

    // 会话数据结构（存Redis，跨网关共享）
    public class SessionState
    {
        public long PlayerId { get; set; }
        public long AccountId { get; set; }
        public string AuthToken { get; set; }
        public int GameServerId { get; set; }
        public int SceneId { get; set; }
        public string LastGatewayId { get; set; }
        public DateTime CreatedAt { get; set; }
        public long ExpireAt { get; set; }
    }

    // 创建会话（登录成功时）
    public async Task<string> CreateSession(long playerId, long accountId, int gameServerId)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var state = new SessionState
        {
            PlayerId = playerId,
            AccountId = accountId,
            GameServerId = gameServerId,
            LastGatewayId = _gatewayId,
            CreatedAt = DateTime.UtcNow,
            ExpireAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        };

        // Redis 存会话状态（TTL 5分钟，允许断线重连）
        await _redis.StringSetAsync(
            $"session:{sessionId}",
            JsonSerializer.Serialize(state),
            TimeSpan.FromMinutes(5));

        // 记录玩家当前使用的会话
        await _redis.StringSetAsync(
            $"player_session:{playerId}",
            sessionId,
            TimeSpan.FromMinutes(5));

        return sessionId;
    }

    // 重连时恢复会话
    public async Task<GameSession> ReconnectSession(long playerId, string newConnectionId)
    {
        // 1. 获取旧的会话ID
        var sessionId = await _redis.StringGetAsync($"player_session:{playerId}");
        if (!sessionId.HasValue)
            return null;

        // 2. 获取会话状态
        var stateJson = await _redis.StringGetAsync($"session:{sessionId}");
        if (!stateJson.HasValue)
            return null;

        var state = JsonSerializer.Deserialize<SessionState>(stateJson);

        // 3. 更新网关ID（迁移到当前网关）
        state.LastGatewayId = _gatewayId;
        await _redis.StringSetAsync(
            $"session:{sessionId}",
            JsonSerializer.Serialize(state),
            TimeSpan.FromMinutes(5));

        // 4. 通知游戏服务器：玩家已重连，恢复状态推送
        await NotifyGameServerReconnect(state.GameServerId, playerId, newConnectionId);

        // 5. 创建本地会话
        var session = new GameSession
        {
            SessionId = sessionId,
            PlayerId = playerId,
            AccountId = state.AccountId,
            GameServerId = state.GameServerId,
            ConnectionId = newConnectionId
        };
        _localSessions[sessionId] = session;

        return session;
    }

    // 通知游戏服务器重新绑定连接
    private async Task NotifyGameServerReconnect(int gameServerId, long playerId, string connId)
    {
        var packet = new GamePacket
        {
            MsgId = MsgId.PlayerReconnect,
            PlayerId = playerId,
            Body = Encoding.UTF8.GetBytes(connId)
        };
        // 通过内部RPC发送
        await _rpcClient.SendAsync($"game:{gameServerId}", packet);
    }
}
```

### 优雅关闭与连接排空

```csharp
public class GatewayShutdownHandler
{
    private readonly SessionManager _sessionManager;
    private readonly IConnectionManager _connectionManager;

    public async Task GracefulShutdown()
    {
        Log.Information("网关开始优雅关闭，当前连接数: {Count}",
            _connectionManager.ActiveConnectionCount);

        // 1. 从负载均衡器注销（停止接收新连接）
        await DeregisterFromLoadBalancer();

        // 2. 通知所有游戏服务器：本网关即将关闭
        await NotifyAllGameServers("gateway_shutting_down", _gatewayId);

        // 3. 逐一向在线玩家推送重连信息
        foreach (var session in _sessionManager.GetAllSessions())
        {
            try
            {
                // 推送"网关切换"消息，客户端自动重连到新网关
                await PushReconnectInfo(session);
            }
            catch
            {
                // 客户端已经断线，忽略
            }
        }

        // 4. 等待所有连接处理完毕（最多等30秒）
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _connectionManager.WaitForDrain(cts.Token);

        // 5. 关闭内部RPC通道
        await _rpcClient.CloseAsync();

        Log.Information("网关关闭完成");
    }

    // 向Nginx/Consul发送下线请求
    private async Task DeregisterFromLoadBalancer()
    {
        // Consul注销
        await _consulClient.Agent.ServiceDeregister(_gatewayServiceId);

        // 等待DNS缓存/健康检查过期
        await Task.Delay(5000);
    }
}
```

---

## 三、场景服务器扩容：空间分区

### 网格分区（Grid Partitioning）

大型MMO世界需要将地图切成多个区域，每个场景服务器管理一个区域：

```csharp
public class SpatialPartitioner
{
    // 将地图划分为 N x N 的网格
    private readonly int _gridSize; // 每个格子的大小（米）
    private readonly int _gridCountX;
    private readonly int _gridCountY;

    // 场景服务器 => 管理的格子集合
    private readonly Dictionary<int, HashSet<(int gx, int gy)>> _serverGrids = new();

    public SpatialPartitioner(int mapWidth, int mapHeight, int gridSize)
    {
        _gridSize = gridSize;
        _gridCountX = mapWidth / gridSize;
        _gridCountY = mapHeight / gridSize;
    }

    // 玩家移动时检测是否跨格子
    public List<(int gx, int gy)> GetPlayerGrids(Vector3 position, float viewRadius)
    {
        var grids = new List<(int gx, int gy)>();

        int cx = (int)(position.X / _gridSize);
        int cy = (int)(position.Y / _gridSize);

        // 玩家视野可能覆盖9宫格
        int range = (int)Math.Ceiling(viewRadius / _gridSize);
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int gx = cx + dx;
                int gy = cy + dy;
                if (gx >= 0 && gx < _gridCountX && gy >= 0 && gy < _gridCountY)
                {
                    grids.Add((gx, gy));
                }
            }
        }
        return grids;
    }

    // 获取处理某个格子的场景服务器
    public int GetServerForGrid(int gx, int gy)
    {
        foreach (var (serverId, grids) in _serverGrids)
        {
            if (grids.Contains((gx, gy)))
                return serverId;
        }
        return -1; // 无服务器处理该区域
    }

    // 负载均衡：重新分配格子到服务器
    public async Task Rebalance(List<ServerLoadInfo> serverLoads)
    {
        // 计算每个服务器的目标格子数
        int totalGrids = _gridCountX * _gridCountY;
        long totalLoad = serverLoads.Sum(s => s.PlayerCount);
        int serverCount = serverLoads.Count;

        int gridsPerServer = totalGrids / serverCount;
        int remainder = totalGrids % serverCount;

        int serverIndex = 0;
        int gridIndex = 0;

        foreach (var (gx, gy) in GetAllGrids())
        {
            // 分配到当前服务器
            _serverGrids[serverLoads[serverIndex].ServerId].Add((gx, gy));
            gridIndex++;

            // 判断是否切换到下一个服务器
            int target = gridsPerServer + (serverIndex < remainder ? 1 : 0);
            if (gridIndex >= target)
            {
                gridIndex = 0;
                serverIndex++;
            }
        }

        Log.Information("空间分区重平衡完成，{Count}个服务器各管理{Grids}个格子",
            serverCount, gridsPerServer);
    }
}
```

### 跨服传送：玩家从一个场景服迁移到另一个

```csharp
public class CrossServerTeleport
{
    public async Task TeleportPlayer(long playerId, int targetMapId, Vector3 targetPos)
    {
        var player = await GetPlayer(playerId);
        int sourceServerId = player.SceneServerId;

        // 1. 锁定玩家（防止传送过程中收到移动等请求）
        await AcquirePlayerLock(playerId);

        // 2. 序列化玩家战斗状态（血量、Buff、技能CD）
        var snapshot = new PlayerBattleSnapshot
        {
            PlayerId = playerId,
            HpPercent = player.Hp / (float)player.MaxHp,
            Buffs = player.ActiveBuffs.Select(b => new BuffSnapshot
            {
                BuffId = b.BuffId,
                RemainingMs = b.RemainingMs,
                StackCount = b.StackCount
            }).ToList(),
            SkillCooldowns = player.SkillCooldowns.Select(c => new CooldownSnapshot
            {
                SkillId = c.SkillId,
                RemainingMs = c.RemainingMs
            }).ToList()
        };

        // 3. 从源场景服务器移除
        var leavePacket = new PlayerLeaveScenePacket
        {
            PlayerId = playerId,
            Snapshot = JsonSerializer.Serialize(snapshot)
        };
        await _rpcClient.SendAsync($"scene:{sourceServerId}", leavePacket);

        // 4. 确定目标场景服务器（目标地图的哪个实例）
        int targetServerId = _sceneAllocator.GetSceneServer(targetMapId);

        // 5. 发送到目标场景服务器
        var enterPacket = new PlayerEnterScenePacket
        {
            PlayerId = playerId,
            MapId = targetMapId,
            Position = targetPos,
            Snapshot = JsonSerializer.Serialize(snapshot)
        };
        var response = await _rpcClient.CallAsync<EnterSceneResponse>(
            $"scene:{targetServerId}", enterPacket, TimeSpan.FromSeconds(5));

        // 6. 更新网关路由
        await _gatewayRouter.UpdateRoute(playerId, targetServerId);

        // 7. 解锁
        await ReleasePlayerLock(playerId);

        // 8. 通知客户端切换场景
        await SendToClient(playerId, MsgId.SceneChanged, new
        {
            MapId = targetMapId,
            Position = targetPos,
            SceneServerId = targetServerId
        });
    }
}
```

---

## 四、Service Mesh 在游戏服务器的应用

### 为什么需要 Service Mesh

```
传统方式的问题：
  - 每个服务都要实现重试、超时、熔断、限流
  - 服务发现逻辑耦合在业务代码中
  - 监控和追踪需要手动埋点

Service Mesh 方案：
  - Sidecar 代理接管网络通信
  - 业务代码零感知获得：负载均衡、重试、熔断
  - 统一流量管理（灰度发布、A/B测试）
```

### Istio 在游戏服中的配置示例

```yaml
# Istio DestinationRule：按玩家ID哈希保证粘性，连接池限流
apiVersion: networking.istio.io/v1beta1
kind: DestinationRule
metadata:
  name: scene-service
spec:
  host: scene-service
  trafficPolicy:
    connectionPool:
      tcp:
        maxConnections: 10000
        connectTimeout: 3s
      http:
        http2MaxRequests: 1000
        maxRequestsPerConnection: 10
    loadBalancer:
      consistentHash:
        httpHeaderName: "x-player-id"  # 同玩家到同一实例
    outlierDetection:
      consecutive5xxErrors: 5
      interval: 30s
      baseEjectionTime: 60s
---
# VirtualService：5% 灰度流量到 canary 版本
apiVersion: networking.istio.io/v1beta1
kind: VirtualService
metadata:
  name: scene-service-routing
spec:
  hosts:
  - scene-service
  http:
  - route:
    - destination:
        host: scene-service
        subset: stable
      weight: 95
    - destination:
        host: scene-service
        subset: canary
      weight: 5
```

### 游戏服不适合 Service Mesh 的场景

```
注意：游戏服务器的长连接（WebSocket/TCP）不适合 Envoy 的七层代理。
Envoy 主要优化 HTTP/gRPC 短连接。

建议方案：
  - 网关到游戏服之间用 gRPC（适合Mesh）
  - 客户端到网关外部流量用传统负载均衡（Nginx/LVS）
  - 内部微服务间通信用 Istio 管理
```

---

## 五、事件驱动 vs 请求响应

### 两种模式的适用场景

```csharp
// 请求-响应模式：适合"我需要立即知道结果"
// 例：购买道具、升级装备、技能释放
public class ShopService
{
    public async Task<BuyResult> BuyItem(long playerId, int itemId, int count)
    {
        // 调用方必须等待结果
        var result = await ProcessPurchase(playerId, itemId, count);
        return result;
    }
}

// 事件驱动模式：适合"发生了某事，谁关心谁处理"
// 例：玩家升级、怪物死亡、活动开始
public class GameEventBus
{
    // 内存中的事件总线（单进程内高性能）
    private readonly Channel<GameEvent> _eventChannel =
        Channel.CreateBounded<GameEvent>(new BoundedChannelOptions(100000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    public async Task Publish<T>(T gameEvent) where T : GameEvent
    {
        await _eventChannel.Writer.WriteAsync(gameEvent);
    }

    // 事件处理循环
    public async Task ProcessLoop(CancellationToken ct)
    {
        await foreach (var evt in _eventChannel.Reader.ReadAllAsync(ct))
        {
            switch (evt)
            {
                case PlayerLevelUpEvent levelUp:
                    // 同步处理：通知任务系统、成就系统、排行榜
                    await _questSystem.OnPlayerLevelUp(levelUp);
                    await _achievementSystem.OnPlayerLevelUp(levelUp);
                    await _rankSystem.OnPlayerLevelUp(levelUp);
                    break;

                case MonsterKilledEvent killed:
                    // 处理掉落、经验、任务计数
                    await _dropSystem.OnMonsterKilled(killed);
                    await _questSystem.OnMonsterKilled(killed);
                    break;

                case ActivityStartEvent activity:
                    // 活动开始：广播全服
                    await _broadcastService.BroadcastActivity(activity);
                    break;
            }
        }
    }
}
```

### 混合架构

```
实际大型游戏都是混合使用：

  玩家操作 → [请求-响应] → 保证实时反馈
     ↓ 操作产生事件
  事件总线 → [异步处理] → 解耦非关键路径
     ↓ 事件落地
  消息队列 → [跨服务] → 最终一致性
  
例子：玩家击杀BOSS
  1. 请求-响应：扣血、判定死亡（玩家必须立即看到结果）
  2. 事件驱动：触发成就检查、掉落计算（可以异步）
  3. 消息队列：同步到跨服排行榜、公会日志（可以延迟）
```

---

## 六、服务器热更新不断线

### 两种热更新策略

```csharp
// 策略1: 代码热替换（AssemblyLoadContext）
public class HotReloadManager
{
    public async Task ReloadGameLogic()
    {
        if (_isReloading) return;
        _isReloading = true;
        try
        {
            await PauseMessageProcessing(TimeSpan.FromSeconds(10));
            var ctx = new AssemblyLoadContext("hotreload", isCollectible: true);
            var asm = ctx.LoadFromAssemblyPath("GameLogic_Hotfix.dll");
            foreach (var (svc, impl) in GetReplaceableServices())
            {
                var hotfix = asm.GetType(impl.FullName + "_Hotfix");
                if (hotfix != null) _serviceRegistry.Replace(svc, hotfix);
            }
            foreach (var old in _contexts.Values) old.Unload();
            _contexts["current"] = ctx;
        }
        finally { _isReloading = false; ResumeMessageProcessing(); }
    }

    private async Task PauseMessageProcessing(TimeSpan timeout)
    {
        _processingPaused = true;
        using var cts = new CancellationTokenSource(timeout);
        while (_activeRequestCount > 0 && !cts.IsCancellationRequested) await Task.Delay(100);
    }
}

// 策略2: 蓝绿部署
public class BlueGreenDeployer
{
    public async Task SwitchVersion()
    {
        await StartNewVersion("blue");
        await Task.Delay(TimeSpan.FromHours(2));
        await ForceMigrateRemaining();
        await ShutdownOldVersion("green");
    }
}
```

---

## 七、容灾与多区域部署

### 同城双活 vs 两地三中心

```
容灾等级（从低到高）：
  
  Level 0: 无备份
    - 单机房，单数据库
    - 宕机 = 停服

  Level 1: 冷备
    - 每天备份数据库到异地
    - RTO（恢复时间）: 24小时
    - RPO（丢失数据）: 24小时

  Level 2: 热备
    - 主从数据库，从库跨机房
    - RTO: 30分钟
    - RPO: < 5秒

  Level 3: 同城双活
    - 两个机房同时提供服务
    - 数据库双向同步
    - RTO: < 1分钟
    - RPO: 0（无丢失）

  Level 4: 异地多活
    - 跨地域多机房
    - 按玩家地域路由
    - 需要业务层处理数据冲突
```

### 游戏异地多活的挑战

```csharp
public class CrossRegionManager
{
    // 按玩家ID哈希固定区域，避免全量迁移
    public string GetPlayerRegion(long playerId) => (playerId % 100) switch
    {
        < 40 => "china-east", < 70 => "china-south", _ => "us-west"
    };

    // 跨区域数据同步 → 发布到跨区域消息队列
    public async Task SyncCrossRegionData(long playerId, string dataType, string jsonData)
    {
        await _crossRegionQueue.PublishAsync("cross_region_sync", new CrossRegionSyncPacket
        {
            PlayerId = playerId, DataType = dataType, JsonData = jsonData,
            SourceRegion = _currentRegion, Timestamp = DateTime.UtcNow
        });
    }
}
```

### 故障转移（Failover）

```csharp
public class FailoverManager
{
    // 健康检查循环
    public async Task HealthCheckLoop()
    {
        while (true)
        {
            foreach (var service in GetAllServices())
            {
                bool healthy = await PingService(service);
                if (!healthy)
                {
                    await HandleServiceFailure(service);
                }
            }
            await Task.Delay(5000);
        }
    }

    // 处理服务故障
    private async Task HandleServiceFailure(ServiceInstance failedService)
    {
        Log.Warning("检测到服务故障: {Type} {Id}",
            failedService.Type, failedService.InstanceId);

        // 1. 从服务发现中移除
        await _serviceRegistry.Deregister(failedService);

        // 2. 如果是场景服务器，迁移玩家
        if (failedService.Type == "scene")
        {
            var affectedPlayers = await GetPlayersOnServer(failedService.InstanceId);
            foreach (var player in affectedPlayers)
            {
                await _teleportService.TeleportPlayer(
                    player.PlayerId,
                    player.MapId,
                    _spawnPoints.GetSpawnPoint(player.MapId));
            }
        }

        // 3. 如果是网关，标记玩家为"断线等待重连"
        if (failedService.Type == "gateway")
        {
            var affectedSessions = await GetSessionsOnGateway(failedService.InstanceId);
            foreach (var session in affectedSessions)
            {
                // 增加会话过期时间，给玩家重连窗口
                await _sessionManager.ExtendSession(session, TimeSpan.FromMinutes(5));
            }
        }

        // 4. 尝试重新启动
        await _orchestrator.RestartService(failedService);
    }
}
```

---

## 八、架构决策总结

```
架构选型速查表：

| 需求 | 推荐方案 | 理由 |
|------|---------|------|
| 千万DAU | 微服务 + 消息队列 | 独立扩缩容 |
| 50万DAU | 多进程单体 | 运维简单够用 |
| 5万DAU | 单进程多线程 | 一台机器搞定 |
| 弱网地区 | 状态同步 + 可靠性UDP | 容错高 |
| 电竞级同步 | 帧同步 + TCP | 确定性要求高 |
| 全球化部署 | 按区域分区 + 异步同步 | 合规+低延迟 |
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 微服务拆分 | 按游戏域拆分，独立部署独立伸缩 |
| 连接迁移 | Redis存会话状态，网关挂了也能重连到其他网关 |
| 空间分区 | 将地图网格化，每个场景服管理若干格子 |
| Service Mesh | Sidecar接管网络，业务零感知获得流量管理 |
| 事件驱动 | 解耦非关键路径，适合日志/成就/排行榜更新 |
| 热更新 | AssemblyLoadContext实现代码热替换不断线 |
| 异地多活 | 按地域分片，数据最终一致性同步 |
| 故障转移 | 健康检查→服务摘除→玩家迁移→自动重启 |
