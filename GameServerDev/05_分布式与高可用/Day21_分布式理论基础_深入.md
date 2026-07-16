# Day 21：分布式理论基础 — 进阶深入

## 一、CAP 理论的游戏实战取舍

### 为什么游戏服务器选 AP 多于 CP

```
核心矛盾：玩家体验 vs 数据一致性

AP 阵营（多数游戏）：
  - 玩家操作不等待，即使跨服数据暂不同步
  - 最终一致性即可：好友列表、公会数据、排行榜
  - 分区恢复后通过补偿机制修复

CP 阵营（经济系统）：
  - 充值、扣钻石、拍卖行必须强一致
  - 用 Redis Lua 脚本或分布式锁保证原子性
  - 宁可失败也不能出错
```

```csharp
// 场景一：商城购买 — AP 友好
// 玩家 A 和 B 在各自分区的服务器购买同一限量道具
// 允许短暂超卖，后台异步对账时回滚多余订单
public class ShopService
{
    public async Task<BuyResult> PurchaseItem(long playerId, int itemId, int price)
    {
        // 本地扣款（AP 策略：先响应，后对账）
        bool deducted = await DeductCurrencyLocal(playerId, price);
        if (!deducted) return BuyResult.InsufficientFunds;

        // 异步发送购买事件到中心队列
        await _eventBus.Publish("purchase", new PurchaseEvent
        {
            PlayerId = playerId,
            ItemId = itemId,
            Price = price,
            Timestamp = DateTimeOffset.UtcNow
        });

        // 立即返回成功给玩家
        return BuyResult.Success;
    }

    // 后台对账任务：检测超卖并回滚
    public async Task ReconcileInventory()
    {
        // 检查限量道具的实际售卖数量
        // 超出部分自动退款
    }
}

// 场景二：公会战排名 — 最终一致性足够
// 不同战区的结果通过消息队列汇总，允许几分钟延迟
```

### 分区恢复后的数据修复策略

```csharp
public class PartitionRecoveryService
{
    // 分区恢复后，对比两个分区的数据差异
    public async Task RepairAfterPartition(string regionA, string regionB)
    {
        // 1. 标记冲突数据
        var conflicts = await FindConflicts(regionA, regionB);

        foreach (var conflict in conflicts)
        {
            switch (conflict.DataType)
            {
                case "player_property":
                    // 取最后写入者胜利（LWW）
                    await ResolveByLastWrite(conflict);
                    break;
                case "inventory":
                    // 取并集（两个分区的道具都保留）
                    await MergeInventory(conflict);
                    break;
                case "friend_relation":
                    // 取并集（好友关系不能丢）
                    await MergeFriends(conflict);
                    break;
            }
        }
    }
}
```

---

## 二、一致哈希进阶：虚拟节点与负载边界

### 解决数据倾斜

```
问题：少量热门玩家 ID 打在同一虚拟节点上造成热点
方案：
  1. 增加虚拟节点数（100→1000）
  2. Bounded Load（负载边界）：如果目标节点负载超过平均值的 1.25 倍，跳到下一个
  3. 加权虚拟节点：给性能强的物理节点分配更多虚拟节点
```

```csharp
// 带负载边界的一致性哈希
public class BoundedLoadConsistentHash<T>
{
    private readonly SortedDictionary<uint, T> _ring = new();
    private readonly int _virtualNodes;
    private readonly double _loadBoundFactor; // 负载边界系数
    private readonly Dictionary<T, int> _nodeLoad = new();

    public BoundedLoadConsistentHash(int virtualNodes = 200, double loadBoundFactor = 1.25)
    {
        _virtualNodes = virtualNodes;
        _loadBoundFactor = loadBoundFactor;
    }

    public T GetNode(string key)
    {
        uint hash = ComputeHash(key);

        // 先找到哈希环上的位置
        var candidates = FindCandidates(hash);

        // 选一个负载在边界内的节点
        int avgLoad = _nodeLoad.Count > 0
            ? _nodeLoad.Values.Sum() / _nodeLoad.Count
            : 1;
        int maxLoad = (int)(avgLoad * _loadBoundFactor);

        foreach (var node in candidates)
        {
            if (_nodeLoad.GetValueOrDefault(node, 0) < maxLoad)
            {
                _nodeLoad[node] = _nodeLoad.GetValueOrDefault(node, 0) + 1;
                return node;
            }
        }

        // 如果所有节点都过载，选负载最小的
        return candidates.OrderBy(n => _nodeLoad.GetValueOrDefault(n, 0)).First();
    }

    private List<T> FindCandidates(uint hash)
    {
        var candidates = new List<T>();
        // 从 hash 位置开始，收集接下来的几个节点
        var view = _ring.SkipWhile(kv => kv.Key < hash).Take(3);
        foreach (var kv in view)
            candidates.Add(kv.Value);

        if (candidates.Count == 0)
            candidates.Add(_ring.First().Value);

        return candidates;
    }

    // 游戏服务器场景：路由玩家到场景服务器
    // 如果某台服务器 CPU > 80%，自动提高其负载计数
    public void ReportNodeLoad(T node, int currentLoad)
    {
        _nodeLoad[node] = currentLoad;
    }
}
```

### 扩容时的数据迁移策略

```
传统一致性哈希：只迁移相邻节点的数据
优化策略：
  1. 先加虚拟节点（不迁移数据），逐渐把流量引过去
  2. 数据双写：新旧节点同时写一段时间
  3. 双读 + 对比修复：读旧节点，对比新节点的正确性
```

---

## 三、CRDT：无冲突数据类型

### 为什么 CRDT 适合游戏

```
场景：多个游戏服务器同时修改同一份玩家数据
传统方案：分布式锁（性能差，容易死锁）
CRDT 方案：每个副本独立修改，合并时自动解决冲突

常见 CRDT 类型：
  - G-Counter (Grow-only Counter): 只能增加，合并取 max
  - PN-Counter (Positive-Negative Counter): 支持增减
  - G-Set (Grow-only Set): 只能添加
  - OR-Set (Observed-Remove Set): 支持添加和删除
  - LWW-Register (Last-Writer-Wins Register): 最后写入者胜出
```

```csharp
// PN-Counter 实现（适用于玩家经验值、积分等）
public class PNCounter
{
    // 正计数器（各节点独立）
    private readonly Dictionary<string, long> _pos = new();
    // 负计数器
    private readonly Dictionary<string, long> _neg = new();
    private readonly string _nodeId;

    public PNCounter(string nodeId)
    {
        _nodeId = nodeId;
    }

    public void Increment(long delta = 1)
    {
        _pos[_nodeId] = _pos.GetValueOrDefault(_nodeId, 0) + delta;
    }

    public void Decrement(long delta = 1)
    {
        _neg[_nodeId] = _neg.GetValueOrDefault(_nodeId, 0) + delta;
    }

    public long Value => _pos.Values.Sum() - _neg.Values.Sum();

    // 合并两个副本（CRDT 核心操作）
    public static PNCounter Merge(PNCounter a, PNCounter b)
    {
        var result = new PNCounter("merged");
        // 合并正计数器：取每个节点的最大值
        foreach (var key in a._pos.Keys.Union(b._pos.Keys))
        {
            result._pos[key] = Math.Max(
                a._pos.GetValueOrDefault(key, 0),
                b._pos.GetValueOrDefault(key, 0));
        }
        // 合并负计数器同理
        foreach (var key in a._neg.Keys.Union(b._neg.Keys))
        {
            result._neg[key] = Math.Max(
                a._neg.GetValueOrDefault(key, 0),
                b._neg.GetValueOrDefault(key, 0));
        }
        return result;
    }
}

// 游戏应用：跨服世界 Boss 伤害统计
// 每个场景服务器独立记录玩家伤害
// 合并时 CRDT 自动解决冲突
public class BossDamageTracker
{
    private readonly Dictionary<long, PNCounter> _damageMap = new();

    public void RecordDamage(long playerId, long damage, string serverId)
    {
        if (!_damageMap.ContainsKey(playerId))
            _damageMap[playerId] = new PNCounter(serverId);
        _damageMap[playerId].Increment(damage);
    }

    // 跨服排行榜汇总
    public void MergeFrom(BossDamageTracker other)
    {
        foreach (var kv in other._damageMap)
        {
            if (_damageMap.ContainsKey(kv.Key))
                _damageMap[kv.Key] = PNCounter.Merge(_damageMap[kv.Key], kv.Value);
            else
                _damageMap[kv.Key] = kv.Value;
        }
    }
}
```

### CRDT 的局限

```
不适合场景：
  - 需要严格一致性的操作（扣款、装备唯一性）
  - 有总量上限的资源（限量道具的库存）
  - 需要跨节点事务的场景

在游戏中的典型应用：
  - 跨服好友列表（OR-Set）
  - 跨服聊天室成员（G-Set）
  - 跨服排行榜分数（PN-Counter）
  - 玩家成就状态（LWW-Register）
```

---

## 四、Raft 共识算法与游戏应用

### Raft 核心概念

```
Raft 将共识问题分解为三个子问题：
  1. Leader Election（领导者选举）
  2. Log Replication（日志复制）
  3. Safety（安全性）

角色：
  - Leader: 处理所有客户端请求，管理日志复制
  - Follower: 被动接收 Leader 的日志
  - Candidate: 选举阶段的临时角色
```

```csharp
// Raft 状态机简化实现（游戏应用中用于配置同步）
public class RaftNode
{
    public enum Role { Follower, Candidate, Leader }

    private Role _role = Role.Follower;
    private int _currentTerm;
    private string _votedFor;
    private readonly List<LogEntry> _log = new();
    private int _commitIndex;
    private int _lastApplied;
    private readonly Dictionary<string, int> _nextIndex = new();
    private readonly Dictionary<string, int> _matchIndex = new();
    private readonly Random _random = new();
    private readonly int _nodeId;

    // 选举超时（150-300ms 随机，防止多个 Candidate 同时发起竞选）
    private int _electionTimeout;

    public RaftNode(int nodeId)
    {
        _nodeId = nodeId;
        ResetElectionTimeout();
    }

    private void ResetElectionTimeout()
    {
        _electionTimeout = _random.Next(150, 300);
    }

    // 收到 AppendEntries RPC（Leader 的心跳或日志复制）
    public AppendEntriesResult HandleAppendEntries(AppendEntriesRequest req)
    {
        if (req.Term < _currentTerm)
            return new AppendEntriesResult { Term = _currentTerm, Success = false };

        if (req.Term > _currentTerm)
        {
            _currentTerm = req.Term;
            _role = Role.Follower;
            _votedFor = null;
        }

        ResetElectionTimeout();

        // 日志一致性检查
        if (_log.Count > req.PrevLogIndex)
        {
            if (_log[req.PrevLogIndex].Term != req.PrevLogTerm)
            {
                // 日志冲突，截断
                _log.RemoveRange(req.PrevLogIndex, _log.Count - req.PrevLogIndex);
                return new AppendEntriesResult { Term = _currentTerm, Success = false };
            }
        }

        // 追加新日志
        _log.AddRange(req.Entries);
        if (req.LeaderCommit > _commitIndex)
            _commitIndex = Math.Min(req.LeaderCommit, _log.Count - 1);

        return new AppendEntriesResult { Term = _currentTerm, Success = true };
    }

    // 触发选举
    public void StartElection()
    {
        _role = Role.Candidate;
        _currentTerm++;
        _votedFor = _nodeId.ToString();
        // 向其他节点发送 RequestVote RPC
    }
}

// 游戏中的 Raft 应用场景
// 1. 选举 Game Master 服务器（分布式首领）
// 2. 跨服活动状态同步（只有 Leader 可修改活动配置）
// 3. 全局配置管理（配置变更通过 Raft 达成一致）
```

### Raft 在游戏中的实际使用建议

```
注意：
  1. Raft 不适合高频写入场景（游戏状态变更太快）
  2. Raft 适合配置同步和协调（低频、高可靠性）
  3. 不要自己实现 Raft，用 etcd/Consul 的内置实现

推荐架构：
  - etcd 作为 Raft 集群管理配置
  - 每个游戏服务器从 etcd 读取配置
  - 配置变更通过 etcd Watch 机制通知
  
  etcd 不适合：
  - 玩家实时状态（用 Redis 或内存数据库）
  - 高频日志写入（用消息队列）
```

---

## 五、服务发现深度对比

```yaml
# 方案对比（游戏服务器视角）

# etcd — 推荐用于配置同步
#   + 原生 Watch 机制，变更推送快
#   + 简单 K-V 模型，容易理解
#   + 支持 TTL Lease 自动过期
#   - 无内置健康检查
#   - 大规模服务注册可能成为瓶颈

# Consul — 推荐用于服务发现
#   + 内置健康检查（HTTP/TCP/gRPC）
#   + 支持多数据中心
#   + DNS 接口（可通过域名直接访问服务）
#   - 健康检查的流量开销
#   - 比 etcd 重

# ZooKeeper — 历史方案，不推荐新项目用
#   + 成熟的 Watcher 机制
#   + 顺序节点支持分布式协调
#   - Java 生态，运维成本高
#   - 处理大量临时节点时性能下降
```

```csharp
// gRPC + Consul 集成（生产级）
public class GrpcServiceDiscovery
{
    private readonly ConsulClient _consul;
    private readonly Dictionary<string, ChannelBase> _channelCache = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // 通过服务名获取 gRPC 客户端连接
    public async Task<TClient> GetGrpcClient<TClient>(string serviceName)
        where TClient : ClientBase
    {
        // 1. 从 Consul 获取可用服务实例
        var services = await _consul.Health.Service(serviceName, true);

        // 2. 负载均衡：随机选择一个健康的实例
        var instance = services.Response
            .Where(s => s.Checks.All(c => c.Status == HealthStatus.Passing))
            .OrderBy(_ => Random.Shared.Next())
            .FirstOrDefault();

        if (instance == null)
            throw new ServiceNotFoundException($"没有可用的 {serviceName} 实例");

        var address = $"{instance.Service.Address}:{instance.Service.Port}";

        // 3. 缓存 gRPC Channel（复用连接）
        if (!_channelCache.TryGetValue(address, out var channel))
        {
            channel = new Channel(address, ChannelCredentials.Insecure);
            _channelCache[address] = channel;
        }

        // 4. 通过反射创建 gRPC 客户端
        return (TClient)Activator.CreateInstance(typeof(TClient), channel);
    }

    // 监听服务变化（自动刷新缓存）
    public async Task WatchServiceChanges(string serviceName)
    {
        var lastIndex = 0UL;
        while (true)
        {
            // Consul 长轮询（blocking query）
            var result = await _consul.Health.Service(serviceName, true,
                new QueryOptions { WaitIndex = lastIndex });

            if (result.LastIndex > lastIndex)
            {
                lastIndex = result.LastIndex;
                // 清理缓存，下次请求重新选择
                await _semaphore.WaitAsync();
                try
                {
                    _channelCache.Clear();
                    Log.Information("服务 {Service} 实例变化，已刷新连接缓存", serviceName);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
    }
}
```

---

## 六、Saga 模式：游戏中的分布式事务

### Saga 实现

```csharp
// Saga：将长事务拆分为多个本地事务 + 补偿操作
// 适用于：跨服转帐、跨服交易、多步奖励发放

public class SagaOrchestrator
{
    private readonly List<ISagaStep> _steps = new();
    private readonly Stack<int> _executedSteps = new();

    public void AddStep(ISagaStep step)
    {
        _steps.Add(step);
    }

    // 执行整个 Saga
    public async Task<bool> Execute()
    {
        for (int i = 0; i < _steps.Count; i++)
        {
            try
            {
                await _steps[i].Execute();
                _executedSteps.Push(i);
                Log.Information("Saga Step {Step} 执行成功", i);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Saga Step {Step} 执行失败，开始回滚", i);
                await Rollback();
                return false;
            }
        }
        return true;
    }

    // 回滚：逆序执行补偿操作
    private async Task Rollback()
    {
        while (_executedSteps.Count > 0)
        {
            int stepIndex = _executedSteps.Pop();
            try
            {
                await _steps[stepIndex].Compensate();
                Log.Information("Saga Step {Step} 补偿成功", stepIndex);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Saga Step {Step} 补偿失败，需要人工介入", stepIndex);
                // 记录到死信队列等待人工处理
            }
        }
    }
}

public interface ISagaStep
{
    Task Execute();       // 正向操作
    Task Compensate();    // 补偿操作
}

// 游戏案例：跨服转服流程（涉及多个系统）
public class TransferPlayerSaga
{
    public SagaOrchestrator BuildTransferSaga(long playerId, string fromServer, string toServer)
    {
        var saga = new SagaOrchestrator();

        // Step 1: 冻结源服数据
        saga.AddStep(new FreezePlayerStep(playerId, fromServer));

        // Step 2: 导出玩家数据
        saga.AddStep(new ExportPlayerDataStep(playerId, fromServer));

        // Step 3: 导入目标服
        saga.AddStep(new ImportPlayerDataStep(playerId, toServer));

        // Step 4: 解冻目标服数据
        saga.AddStep(new UnfreezePlayerStep(playerId, toServer));

        // Step 5: 删除源服数据（如果删除失败，补偿：目标服也删除）
        saga.AddStep(new CleanupSourceStep(playerId, fromServer));

        return saga;
    }
}
```

### 幂等性：重试安全的关键

```csharp
// 游戏操作幂等键实现
public class IdempotencyGuard
{
    private readonly IDatabase _redis;

    // 每个请求携带幂等键（由客户端生成或服务器分配）
    // 同一个幂等键多次提交只执行一次
    public async Task<T> ExecuteIdempotent<T>(
        string idempotentKey,
        Func<Task<T>> operation,
        TimeSpan ttl = default)
    {
        if (ttl == default) ttl = TimeSpan.FromHours(24);

        string lockKey = $"idempotent:{idempotentKey}";
        string resultKey = $"idempotent_result:{idempotentKey}";

        // SET NX: 如果不存在才设置（防止并发重复提交）
        bool acquired = await _redis.StringSetAsync(lockKey, "processing", ttl, When.NotExists);
        if (!acquired)
        {
            // 等待另一线程完成并获取结果
            var existingResult = await _redis.StringGetAsync(resultKey);
            if (existingResult.HasValue)
                return JsonSerializer.Deserialize<T>(existingResult);

            // 等待重试（另一线程还在处理中）
            await Task.Delay(100);
            return await ExecuteIdempotent(idempotentKey, operation, ttl);
        }

        try
        {
            var result = await operation();

            // 缓存结果，这样即使重复执行也能返回相同结果
            await _redis.StringSetAsync(resultKey,
                JsonSerializer.Serialize(result), ttl);

            return result;
        }
        catch
        {
            await _redis.KeyDeleteAsync(lockKey);
            throw;
        }
    }
}

// 支付回调（幂等键 = 商户订单号）
// GM工具补偿操作（幂等键 = adminId + operationId + timestamp）
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| AP 优先 | 游戏服务器多数场景选可用性，最终一致性 |
| CRDT | 无冲突并发合并，适合跨服计数器/集合 |
| Raft | 共识算法，适合配置同步而非高频状态 |
| etcd | 推荐做游戏配置管理（Watch + TTL） |
| Saga | 分布式事务的补偿模式，解决跨服操作一致性 |
| 幂等键 | 幂等键 + SET NX 保证重试安全 |
| Bounded Load | 一致性哈希中避免热点节点的负载边界策略 |
