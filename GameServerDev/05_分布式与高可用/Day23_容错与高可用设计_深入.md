# Day 23：容错与高可用设计 — 进阶深入

## 一、熔断器进阶：成功率复位策略

### 基于滑动窗口的熔断

```
基础熔断器的局限：
  - 固定阈值（连续失败 N 次），不够敏感
  - 不能区分瞬时故障和持续故障
  - 没有考虑成功率百分比

进阶熔断器：
  - 基于滑动窗口的成功率
  - 支持 Half-Open 状态下渐进放量
  - 区分慢调用和失败调用
```

```csharp
// 进阶熔断器：基于滑动窗口成功率
public class SlidingWindowCircuitBreaker
{
    private readonly int _windowSizeMs;      // 窗口大小（10 秒）
    private readonly int _bucketCount;        // 桶数（10 个桶，每个 1 秒）
    private readonly double _failureRateThreshold; // 失败率阈值（50%）
    private readonly int _minRequestCount;    // 最少请求数（避免样本太少）

    private readonly Bucket[] _buckets;
    private volatile CircuitState _state = CircuitState.Closed;
    private DateTime _lastStateChange = DateTime.UtcNow;
    private readonly TimeSpan _halfOpenTimeout = TimeSpan.FromSeconds(5);
    private int _halfOpenSuccessCount;
    private const int HalfOpenSuccessThreshold = 3; // 半开状态连续成功 N 次才关闭

    public SlidingWindowCircuitBreaker()
    {
        _windowSizeMs = 10000;
        _bucketCount = 10;
        _failureRateThreshold = 0.5;
        _minRequestCount = 10;
        _buckets = new Bucket[_bucketCount];
        for (int i = 0; i < _bucketCount; i++)
            _buckets[i] = new Bucket();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<Task<T>> fallback = null)
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastStateChange > _halfOpenTimeout)
            {
                Log.Information("熔断器从 Open 变为 Half-Open");
                _state = CircuitState.HalfOpen;
                _halfOpenSuccessCount = 0;
                _lastStateChange = DateTime.UtcNow;
            }
            else
            {
                return fallback != null ? await fallback() : default;
            }
        }

        var startTime = DateTime.UtcNow;
        try
        {
            var result = await action();
            RecordSuccess();

            if (_state == CircuitState.HalfOpen)
            {
                _halfOpenSuccessCount++;
                if (_halfOpenSuccessCount >= HalfOpenSuccessThreshold)
                {
                    Log.Information("熔断器从 Half-Open 恢复为 Closed");
                    _state = CircuitState.Closed;
                    _lastStateChange = DateTime.UtcNow;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            RecordFailure();

            if (_state == CircuitState.HalfOpen)
            {
                Log.Warning("Half-Open 请求失败，回到 Open 状态");
                _state = CircuitState.Open;
                _lastStateChange = DateTime.UtcNow;
            }
            else
            {
                // 检查是否需要打开熔断器
                var stats = GetWindowStats();
                if (stats.TotalCount >= _minRequestCount &&
                    stats.FailureRate >= _failureRateThreshold)
                {
                    Log.Error("熔断器打开: 失败率 {Rate:P2} (阈值 {Threshold:P2})",
                        stats.FailureRate, _failureRateThreshold);
                    _state = CircuitState.Open;
                    _lastStateChange = DateTime.UtcNow;
                }
            }

            return fallback != null ? await fallback() : throw;
        }
    }

    private void RecordSuccess()
    {
        var bucket = GetCurrentBucket();
        Interlocked.Increment(ref bucket.SuccessCount);
    }

    private void RecordFailure()
    {
        var bucket = GetCurrentBucket();
        Interlocked.Increment(ref bucket.FailureCount);
    }

    private Bucket GetCurrentBucket()
    {
        int index = (Environment.TickCount / (_windowSizeMs / _bucketCount)) % _bucketCount;
        var bucket = _buckets[index];
        if (bucket.IsExpired)
        {
            lock (bucket)
            {
                if (bucket.IsExpired)
                    bucket.Reset();
            }
        }
        return bucket;
    }

    // 滑动窗口统计数据
    private (long TotalCount, long FailureCount, double FailureRate) GetWindowStats()
    {
        long total = 0, failures = 0;
        foreach (var b in _buckets)
        {
            total += b.SuccessCount + b.FailureCount;
            failures += b.FailureCount;
        }
        return (total, failures, total > 0 ? (double)failures / total : 0);
    }

    private class Bucket
    {
        public long SuccessCount;
        public long FailureCount;
        public readonly long ExpireAt;

        public Bucket() => ExpireAt = Environment.TickCount + 1000;
        public bool IsExpired => Environment.TickCount > ExpireAt;
        public void Reset()
        {
            SuccessCount = 0;
            FailureCount = 0;
        }
    }

    private enum CircuitState { Closed, Open, HalfOpen }
}
```

---

## 二、限流进阶：滑动窗口与高级令牌桶

### 滑动窗口计数器

```csharp
// 滑动窗口限流（比固定窗口更平滑）
public class SlidingWindowRateLimiter
{
    private readonly int _windowSizeMs;
    private readonly int _bucketCount;
    private readonly int _limitPerWindow;
    private readonly int[] _counters;
    private long _lastResetTime;

    public SlidingWindowRateLimiter(int limitPerSecond)
    {
        _windowSizeMs = 1000;
        _bucketCount = 10;         // 每个桶 100ms
        _limitPerWindow = limitPerSecond;
        _counters = new int[_bucketCount];
        _lastResetTime = Environment.TickCount64;
    }

    public bool TryAcquire()
    {
        long now = Environment.TickCount64;
        int elapsedMs = (int)(now - _lastResetTime);

        // 过期桶重置
        if (elapsedMs >= _windowSizeMs)
        {
            Array.Clear(_counters, 0, _bucketCount);
            _lastResetTime = now;
            elapsedMs = 0;
        }

        // 计算当前桶索引
        int currentBucket = elapsedMs / (_windowSizeMs / _bucketCount);
        // 清理过期桶（超过窗口宽度的桶）
        int cutoffBucket = Math.Max(0, currentBucket - _bucketCount + 1);

        int total = 0;
        for (int i = currentBucket; i >= cutoffBucket; i--)
        {
            total += _counters[i];
        }

        if (total >= _limitPerWindow)
            return false;

        Interlocked.Increment(ref _counters[currentBucket]);
        return true;
    }
}

// 游戏场景：限制每个玩家每 10 秒最多发起 5 次战斗
public class PlayerActionRateLimiter
{
    private readonly ConcurrentDictionary<long, SlidingWindowRateLimiter> _limiters = new();

    public bool AllowAction(long playerId, string actionType)
    {
        var limiter = _limiters.GetOrAdd(playerId,
            _ => new SlidingWindowRateLimiter(5));

        if (!limiter.TryAcquire())
        {
            Log.Warning("玩家 {PlayerId} 操作 {ActionType} 被限流", playerId, actionType);
            return false;
        }
        return true;
    }

    // 定期清理不活跃玩家的限流器
    public Task CleanupInactive()
    {
        // 每分钟清理一次，释放内存
        _limiters.Clear();
        return Task.CompletedTask;
    }
}
```

### 分布式令牌桶（Redis 实现）

```lua
-- Lua 脚本：分布式令牌桶（在 Redis 中运行）
-- KEYS[1]: 限流 key
-- ARGV[1]: 桶容量
-- ARGV[2]: 每秒填充速率
-- ARGV[3]: 请求消耗的令牌数
-- ARGV[4]: 当前时间戳（秒）

local key = KEYS[1]
local capacity = tonumber(ARGV[1])
local fillRate = tonumber(ARGV[2])
local cost = tonumber(ARGV[3])
local now = tonumber(ARGV[4])

-- 获取当前状态
local data = redis.call('HMGET', key, 'tokens', 'lastRefill')
local tokens = tonumber(data[1]) or capacity
local lastRefill = tonumber(data[2]) or now

-- 计算需要补充的令牌
local elapsed = now - lastRefill
tokens = math.min(capacity, tokens + elapsed * fillRate)

if tokens >= cost then
    tokens = tokens - cost
    redis.call('HMSET', key, 'tokens', tokens, 'lastRefill', now)
    redis.call('EXPIRE', key, 10)  -- 10 秒自动过期
    return 1  -- 允许
else
    redis.call('HMSET', key, 'tokens', tokens, 'lastRefill', now)
    return 0  -- 拒绝
end
```

---

## 三、Bulkhead 隔离模式

### 游戏服务器隔离层次

```
Bulkhead（舱壁隔离）：将资源分组隔离，防止一个组的问题拖垮全局

游戏服务器隔离维度：
  1. 玩家隔离：每个玩家一个 actor/fiber
  2. 模块隔离：战斗 / 社交 / 交易 用不同线程池
  3. 连接隔离：网关按服务类型分配连接池
  4. 数据库隔离：读库 / 写库 / 不同业务库连接池分离
```

```csharp
// 基于 SemaphoreSlim 的 Bulkhead 实现
public class Bulkhead
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxQueueSize;
    private readonly Channel<Func<Task>> _queue;
    private readonly string _name;

    public Bulkhead(string name, int maxConcurrent, int maxQueueSize = 100)
    {
        _name = name;
        _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        _maxQueueSize = maxQueueSize;
        _queue = Channel.CreateBounded<Func<Task>>(maxQueueSize);
        _ = ProcessQueue();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        // 尝试获取信号量（不等待）
        if (!_semaphore.Wait(0))
        {
            // 尝试排队
            var tcs = new TaskCompletionSource<T>();
            var queued = _queue.Writer.TryWrite(async () =>
            {
                try
                {
                    await _semaphore.WaitAsync();
                    var result = await action();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    _semaphore.Release();
                }
            });

            if (!queued)
            {
                Log.Warning("Bulkhead {Name} 队列已满，拒绝请求", _name);
                throw new BulkheadFullException($"Bulkhead {_name} is full");
            }

            // 带超时的等待
            if (await Task.WhenAny(tcs.Task, Task.Delay(5000)) == tcs.Task)
                return await tcs.Task;
            else
                throw new TimeoutException($"Bulkhead {_name} queue timeout");
        }

        try
        {
            return await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessQueue()
    {
        await foreach (var task in _queue.Reader.ReadAllAsync())
        {
            await Task.Run(task);
        }
    }
}

// 游戏中使用 Bulkhead
public class GameBulkheads
{
    public readonly Bulkhead BattleBulkhead = new("Battle", maxConcurrent: 50, maxQueueSize: 200);
    public readonly Bulkhead SocialBulkhead = new("Social", maxConcurrent: 20, maxQueueSize: 100);
    public readonly Bulkhead DbBulkhead = new("Database", maxConcurrent: 10, maxQueueSize: 50);

    public async Task<BattleResult> ProcessBattle(long playerId, BattleAction action)
    {
        // 战斗逻辑使用独立的 Bulkhead
        // 不会因为社交模块大量请求而影响战斗
        return await BattleBulkhead.ExecuteAsync(async () =>
        {
            return await _battleService.HandleAction(playerId, action);
        });
    }
}
```

---

## 四、重试策略：去抖的数学

### Decorrelated Jitter（去相关抖动）

```
指数退避的问题：
  - 重试时间间隔固定模式容易被下游检测到
  - 多个客户端同时重试可能造成"雷群"（thundering herd）

Decorrelated Jitter:
  sleep = min(cap, random(base, sleep * 3))
  其中 sleep 从 base 开始，每次重试在上一次延迟的 1~3 倍之间随机
```

```csharp
public class AdvancedRetryPolicy
{
    private readonly int _maxRetries;
    private readonly int _baseDelayMs;
    private readonly int _maxDelayMs;

    public AdvancedRetryPolicy(int maxRetries = 3, int baseDelayMs = 100, int maxDelayMs = 10000)
    {
        _maxRetries = maxRetries;
        _baseDelayMs = baseDelayMs;
        _maxDelayMs = maxDelayMs;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        Func<Exception, bool> shouldRetry = null,
        CancellationToken ct = default)
    {
        int retry = 0;
        int sleepMs = _baseDelayMs;

        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (retry < _maxRetries)
            {
                retry++;

                if (shouldRetry != null && !shouldRetry(ex))
                    throw;

                // Decorrelated Jitter
                sleepMs = Math.Min(_maxDelayMs,
                    Random.Shared.Next(_baseDelayMs, sleepMs * 3));

                Log.Warning("重试 {Retry}/{MaxRetries}，等待 {Delay}ms，异常: {Ex}",
                    retry, _maxRetries, sleepMs, ex.Message);

                try
                {
                    await Task.Delay(sleepMs, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }
    }

    // 游戏重试策略示例
    public static bool ShouldRetryDbOperation(Exception ex)
    {
        return ex is TimeoutException          // 超时可重试
            || ex is SqlException sqlex when (
                sqlex.Number == 1205           // 死锁
                || sqlex.Number == -2           // 超时
                || sqlex.Number == 1204         // 锁
            );
    }

    public static bool ShouldRetryRpcCall(Exception ex)
    {
        return ex is RpcException re
            && re.StatusCode == StatusCode.Unavailable  // 服务不可用
            || ex is IOException;                         // 网络问题
    }
}
```

---

## 五、健康检查体系

### 深度 vs 浅度检查

```yaml
# 健康检查分层

# 浅度检查（每 10 秒）
#   - 进程是否存活
#   - TCP 端口是否监听
#   - /healthz 端点返回 200

# 深度检查（每 30 秒）
#   - 数据库连接是否正常
#   - Redis 是否能读写
#   - 消息队列是否可达
#   - 当前队列积压是否正常
#   - 关键依赖服务是否可访问

# 就绪检查（Readiness Probe）
#   - 服务器是否准备好接收流量
#   - 刚启动时返回 503，等缓存预热完成才返回 200

# 存活检查（Liveness Probe）
#   - 服务器是否健康运行
#   - 如果死锁或 OOM 返回 503，触发重启
```

```csharp
public class HealthCheckSystem
{
    private readonly List<IHealthCheck> _shallowChecks = new();
    private readonly List<IHealthCheck> _deepChecks = new();
    private HealthStatus _lastDeepStatus = HealthStatus.Healthy;
    private DateTime _lastDeepCheck = DateTime.MinValue;

    // 浅度检查（快速）
    public HealthReport PerformShallow()
    {
        var results = new Dictionary<string, HealthCheckResult>();
        foreach (var check in _shallowChecks)
        {
            try
            {
                var result = check.Check();
                results[check.Name] = result;
            }
            catch
            {
                results[check.Name] = HealthCheckResult.Unhealthy("检查异常");
            }
        }

        return new HealthReport(results);
    }

    // 深度检查（慢）
    public async Task<HealthReport> PerformDeep()
    {
        // 缓存 30 秒，防止频繁深度检查
        if (DateTime.UtcNow - _lastDeepCheck < TimeSpan.FromSeconds(30))
            return new HealthReport(new Dictionary<string, HealthCheckResult>
            {
                ["cached"] = HealthCheckResult.Healthy("最近检查结果")
            });

        var results = new Dictionary<string, HealthCheckResult>();
        // 并行执行所有深度检查
        var tasks = _deepChecks.Select(async check =>
        {
            try
            {
                var result = await check.CheckAsync();
                results[check.Name] = result;
            }
            catch
            {
                results[check.Name] = HealthCheckResult.Unhealthy("检查异常");
            }
        });

        await Task.WhenAll(tasks);
        _lastDeepCheck = DateTime.UtcNow;

        return new HealthReport(results);
    }
}

// 游戏专用健康检查
public class GameHealthChecks
{
    // 场景服务器健康检查
    public class SceneServerHealth : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckAsync()
        {
            var issues = new List<string>();

            if (_onlinePlayers > _maxCapacity * 0.9)
                issues.Add("玩家负载超过 90%");

            if (_tickInterval > 100) // 毫秒
                issues.Add("主循环延迟过高");

            if (_pendingEvents > 10000)
                issues.Add("待处理事件积压");

            if (issues.Count == 0)
                return HealthCheckResult.Healthy("场景服务器运行正常");

            if (issues.Count <= 2)
                return HealthCheckResult.Degraded(string.Join("; ", issues));

            return HealthCheckResult.Unhealthy(string.Join("; ", issues));
        }
    }
}
```

---

## 六、混沌工程

```
原则：
  1. 在生产环境或准生产环境进行
  2. 从较小爆炸半径开始
  3. 自动化 + 可观测 + 可回滚

游戏服务器的混沌实验：
  - 随机杀掉一个场景服务器进程 → 观察玩家重连流程
  - 注入网络延迟（50ms） → 观察跨服通信 timeout 处理
  - Redis 主从切换 → 观察缓存失效恢复
  - MySQL 查询限速 → 观察降级逻辑是否触发
  - 磁盘 IO 限速 → 观察日志写入阻塞后的行为
```

```csharp
// 简单的混沌实验框架
public class ChaosExperiment
{
    private readonly IHttpClientFactory _httpClientFactory;

    // 注入网络延迟
    public async Task InjectLatency(string targetHost, int latencyMs, int jitterMs, TimeSpan duration)
    {
        Log.Warning("混沌实验: 在 {Host} 注入 +{Ms}ms 延迟", targetHost, latencyMs);

        // Windows: 使用 netsh 模拟网络延迟
        string cmd = $"netsh advfirewall firewall add rule name=\"chaos_latency_{targetHost}\" " +
                     $"dir=in protocol=tcp remoteip={targetHost} action=block";

        // Linux: 使用 tc
        // tc qdisc add dev eth0 root netem delay {latencyMs}ms {jitterMs}ms

        await Task.Delay(duration);

        // 恢复
        // netsh advfirewall firewall delete rule name=\"chaos_latency_{targetHost}\"
        Log.Information("混沌实验: 恢复 {Host} 网络正常", targetHost);
    }

    // 终止进程
    public async Task KillProcess(string serviceName)
    {
        Log.Warning("混沌实验: 杀掉进程 {Service}", serviceName);
        // 实际生产环境会有 Chaos Mesh / Litmus 等工具
    }
}
```

---

## 七、优雅降级：分级服务

```csharp
// 优先级特征降级
public enum FeatureTier
{
    Tier0_Core,     // 战斗、登录、核心玩法（永不降级）
    Tier1_Important,// 社交、好友、公会（压力大时降级）
    Tier2_Normal,   // 排行榜、邮件（中等压力降级）
    Tier3_Cosmetic  // 外观、称号、广播（高压力时降级）
}

public class GracefulDegradation
{
    private FeatureTier _currentTier = FeatureTier.Tier0_Core;
    private readonly ServerMetrics _metrics;

    // 每分钟检查一次，自动调整降级级别
    public async Task AutoDegradeLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);

            var cpu = _metrics.CpuUsage;
            var mem = _metrics.MemoryUsage;
            var queueDepth = _metrics.EventQueueDepth;

            if (cpu > 90 || mem > 90 || queueDepth > 50000)
            {
                _currentTier = FeatureTier.Tier2_Normal;
                Log.Warning("自动降级至 Tier2: CPU={Cpu}% 内存={Mem}% 队列={Queue}",
                    cpu, mem, queueDepth);
            }

            if (cpu > 95 || mem > 95 || queueDepth > 100000)
            {
                _currentTier = FeatureTier.Tier3_Cosmetic;
                Log.Error("自动降级至 Tier3: 仅保留核心功能");
            }

            // 恢复：压力缓解后逐步恢复
            if (cpu < 60 && mem < 70 && queueDepth < 10000)
            {
                _currentTier = FeatureTier.Tier0_Core;
                Log.Information("压力缓解，恢复全功能");
            }
        }
    }

    public bool IsFeatureAvailable(FeatureTier featureTier)
    {
        return featureTier >= _currentTier;
    }
}
```

---

## 八、部署策略：蓝绿 vs 金丝雀

### 游戏服务器的选择

```
蓝绿部署
  适用场景：重大版本更新、数据库变更
  优点：瞬间切换，回滚简单
  缺点：双倍资源成本，游戏玩家需要在切换时过渡
  游戏特殊要求：绿环境准备好后，先让少量玩家验证
    
金丝雀部署（推荐）
  适用场景：常规版本更新、功能上线
  流程：1% → 5% → 20% → 50% → 100%
  游戏特殊要求：
    - 先把新玩家路由到新版本
    - 新服使用新版本
    - 老服逐步灰度
```

```yaml
# K8s 金丝雀部署
apiVersion: apps/v1
kind: Deployment
metadata:
  name: game-server-stable
spec:
  replicas: 100
  selector:
    matchLabels:
      app: game-server
---
apiVersion: apps/v1
kind: Deployment  
metadata:
  name: game-server-canary
spec:
  replicas: 5   # 5% 金丝雀
  selector:
    matchLabels:
      app: game-server
      track: canary
---
# 网关层按玩家 ID 路由
# playerId % 100 < 5 → canary
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 滑动窗口熔断 | 基于滑动窗口成功率而非固定失败次数的智能熔断 |
| 分布式令牌桶 | Redis Lua 实现跨进程的精确速率限制 |
| Bulkhead | 按功能/连接/线程池隔离防止级联故障 |
| Decorrelated Jitter | 随机化重试间隔避免雷群效应 |
| 深度健康检查 | 不仅检查存活还检查依赖服务健康度 |
| 混沌工程 | 故意注入故障验证系统韧性 |
| 优雅降级 | 按优先级逐层关闭非核心功能 |
| 金丝雀部署 | 游戏服务器推荐逐步放量的部署策略 |
