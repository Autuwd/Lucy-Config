# Day 23：容错与高可用设计

## 一、故障类型

### 游戏服务器常见故障

| 故障类型 | 影响 | 频率 |
|---------|------|------|
| 进程崩溃 | 该服务上的玩家掉线 | 偶发 |
| 网络分区 | 服务器间通信中断 | 少见 |
| DB 挂掉 | 所有需要 DB 的操作失败 | 罕见 |
| 慢查询 | DB 响应慢导致连锁反应 | 常见 |
| 内存泄漏 | 性能下降，最终 OOM | 常见 |
| 无限循环 | CPU 100%，该进程卡死 | 偶发 |

### 故障域

```
单个进程崩溃 → 只影响该进程的玩家
单台服务器 → 影响该服务器的所有进程
整个机房   → 影响所有玩家
```

---

## 二、熔断器 (Circuit Breaker)

### 工作原理

```
Closed (关闭) → 正常，请求正常通过
    ↓ 失败次数超过阈值
Open (打开) → 快速失败，不执行请求
    ↓ 超时时间到
Half-Open (半开) → 尝试放行一个请求
    ↓ 成功 → Closed
    ↓ 失败 → Open
```

### 实现

```csharp
public class CircuitBreaker
{
    private enum State { Closed, Open, HalfOpen }

    private State _state = State.Closed;
    private int _failureCount;
    private int _failureThreshold = 5;
    private DateTime _lastFailureTime;
    private TimeSpan _openTimeout = TimeSpan.FromSeconds(30);
    private readonly object _lock = new();

    // 执行受保护的调用
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<Task<T>> fallback = null)
    {
        // 检查是否打开
        if (_state == State.Open)
        {
            // 检查是否过了重试时间
            if (DateTime.UtcNow - _lastFailureTime > _openTimeout)
            {
                lock (_lock)
                {
                    if (_state == State.Open)
                    {
                        _state = State.HalfOpen;
                        Log.Warning("熔断器进入半开状态");
                    }
                }
            }
            else
            {
                // 快速失败，调用降级函数
                Log.Warning("熔断器已打开，快速失败");
                return fallback != null ? await fallback() : default;
            }
        }

        try
        {
            var result = await action();

            // 成功 → 重置
            if (_state == State.HalfOpen)
            {
                lock (_lock)
                {
                    _state = State.Closed;
                    _failureCount = 0;
                    Log.Information("熔断器恢复为关闭状态");
                }
            }
            else if (_failureCount > 0)
            {
                Interlocked.Exchange(ref _failureCount, 0);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "调用失败");

            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_failureCount >= _failureThreshold)
                {
                    _state = State.Open;
                    Log.Error("熔断器打开 (连续失败 {Count} 次)", _failureCount);
                }
            }

            return fallback != null ? await fallback() : throw;
        }
    }
}

// 使用
class DbHealthProxy
{
    private readonly CircuitBreaker _breaker = new();

    public async Task<PlayerData> GetPlayer(long playerId)
    {
        return await _breaker.ExecuteAsync(
            // 正常调用
            async () => await _db.GetPlayerAsync(playerId),
            // 降级：从缓存获取（可能过期）
            async () =>
            {
                var cached = await _cache.GetAsync($"player:{playerId}");
                return cached != null
                    ? JsonSerializer.Deserialize<PlayerData>(cached)
                    : null;
            });
    }
}
```

---

## 三、限流

### 令牌桶算法

```csharp
public class TokenBucketRateLimiter
{
    private readonly int _capacity;      // 桶容量
    private readonly double _fillRate;   // 每秒填充数
    private double _tokens;
    private DateTime _lastFillTime;

    public TokenBucketRateLimiter(int capacity, double fillRate)
    {
        _capacity = capacity;
        _fillRate = fillRate;
        _tokens = capacity;
        _lastFillTime = DateTime.UtcNow;
    }

    public bool TryConsume(int tokens = 1)
    {
        lock (this)
        {
            // 填充令牌
            var now = DateTime.UtcNow;
            double elapsed = (now - _lastFillTime).TotalSeconds;
            _tokens = Math.Min(_capacity, _tokens + elapsed * _fillRate);
            _lastFillTime = now;

            // 检查是否有足够令牌
            if (_tokens >= tokens)
            {
                _tokens -= tokens;
                return true;
            }

            return false;
        }
    }
}

// 使用
class RateLimitService
{
    // 每台服务器限制 10000 QPS
    private readonly TokenBucketRateLimiter _serverLimiter = new(10000, 10000);
    // 每个玩家每秒最多 50 个包
    private readonly ConcurrentDictionary<long, TokenBucketRateLimiter> _playerLimiters = new();

    public bool AllowRequest(long playerId, int cost = 1)
    {
        // 服务器级限流
        if (!_serverLimiter.TryConsume(cost))
            return false;

        // 玩家级限流
        var playerLimiter = _playerLimiters.GetOrAdd(playerId,
            _ => new TokenBucketRateLimiter(50, 50));
        if (!playerLimiter.TryConsume(cost))
            return false;

        return true;
    }
}
```

### 漏桶算法

```csharp
public class LeakyBucket
{
    private readonly int _capacity;      // 桶容量
    private readonly int _leakRate;      // 每秒漏出
    private int _water;
    private DateTime _lastLeakTime;

    public LeakyBucket(int capacity, int leakRate)
    {
        _capacity = capacity;
        _leakRate = leakRate;
        _water = 0;
        _lastLeakTime = DateTime.UtcNow;
    }

    public bool TryAdd()
    {
        lock (this)
        {
            // 漏水
            var now = DateTime.UtcNow;
            int leaked = (int)((now - _lastLeakTime).TotalSeconds * _leakRate);
            _water = Math.Max(0, _water - leaked);
            _lastLeakTime = now;

            // 检查是否溢出
            if (_water < _capacity)
            {
                _water++;
                return true;
            }

            return false; // 桶满，丢弃
        }
    }
}
```

---

## 四、超时与重试

### 超时控制

```csharp
public class TimeoutService
{
    // 带超时的数据库查询
    public async Task<T> QueryWithTimeoutAsync<T>(Func<CancellationToken, Task<T>> query,
        int timeoutMs = 3000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);

        try
        {
            return await query(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log.Warning("数据库查询超时 ({Timeout}ms)", timeoutMs);
            throw new TimeoutException($"查询超时 ({timeoutMs}ms)");
        }
    }
}

// 使用
var player = await _timeoutService.QueryWithTimeoutAsync(
    async ct => await _db.GetPlayerAsync(playerId),
    timeoutMs: 2000);
```

### 重试策略

```csharp
public class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly int _baseDelayMs;
    private readonly HashSet<Type> _retryableExceptions;

    public RetryPolicy(int maxRetries = 3, int baseDelayMs = 100)
    {
        _maxRetries = maxRetries;
        _baseDelayMs = baseDelayMs;
        _retryableExceptions = new HashSet<Type>
        {
            typeof(TimeoutException),
            typeof(IOException),
            typeof(SocketException)
        };
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        int retry = 0;

        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (_retryableExceptions.Contains(ex.GetType()))
            {
                retry++;
                if (retry > _maxRetries)
                    throw;

                // 指数退避 + 随机抖动
                int delay = _baseDelayMs * (int)Math.Pow(2, retry - 1);
                delay += Random.Shared.Next(0, 50); // 抖动

                Log.Warning(ex, "操作失败（第 {Retry}/{MaxRetries} 次重试），等待 {Delay}ms",
                    retry, _maxRetries, delay);

                await Task.Delay(delay);
            }
        }
    }
}

// 使用
var result = await _retryPolicy.ExecuteAsync(async () =>
{
    return await _db.QueryAsync<PlayerData>("SELECT * FROM players WHERE id = @Id",
        new { Id = playerId });
});
```

### 重试注意事项

```csharp
// 幂等性检查（防止重复处理）
class IdempotentHandler
{
    private readonly IDatabase _redis;

    // 检查是否已处理过（重复请求去重）
    public async Task<bool> TryProcess(string requestId, Func<Task> process)
    {
        string key = $"processed:{requestId}";

        // SET NX：不存在才设置
        bool firstTime = await _redis.StringSetAsync(key, "1",
            TimeSpan.FromMinutes(10), When.NotExists);

        if (!firstTime)
        {
            Log.Warning("重复请求已忽略: {RequestId}", requestId);
            return false; // 已处理过
        }

        await process();
        return true;
    }
}
```

---

## 五、服务降级

### 降级策略

```csharp
public class DegradationManager
{
    private readonly Dictionary<string, bool> _features = new();
    private readonly IConfiguration _config;

    // 运行时检查功能是否可用
    public bool IsFeatureEnabled(string featureName)
    {
        // 1. 从配置中心检查（可以动态关闭）
        string configKey = $"FeatureFlags:{featureName}";
        string configValue = _config[configKey];

        if (configValue != null)
            return bool.Parse(configValue);

        // 2. 默认开启
        return true;
    }

    // 自动降级（检测到异常率过高时）
    public void AutoDegrade(string featureName)
    {
        Log.Warning("自动降级: {FeatureName}", featureName);
        _features[featureName] = false;
    }

    public void Restore(string featureName)
    {
        Log.Information("功能恢复: {FeatureName}", featureName);
        _features[featureName] = true;
    }
}

// 降级示例
class BattleService
{
    private readonly DegradationManager _degradation;

    // 排行榜功能降级
    public async Task<List<RankInfo>> GetRanking()
    {
        if (!_degradation.IsFeatureEnabled("Ranking"))
        {
            // 降级：返回缓存数据
            return _cachedRanking;
        }

        try
        {
            var ranking = await _rankingService.GetTop100();
            _cachedRanking = ranking;
            return ranking;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "排行榜服务异常，降级使用缓存");
            _degradation.AutoDegrade("Ranking");
            return _cachedRanking ?? new List<RankInfo>();
        }
    }

    // 聊天功能降级
    public async Task<ChatResult> SendMessage(ChatMessage msg)
    {
        if (!_degradation.IsFeatureEnabled("Chat"))
        {
            return ChatResult.Failed("聊天功能暂时关闭");
        }

        // ...
    }
}
```

---

## 六、练习

1. **熔断器**：实现带 Closed/Open/HalfOpen 状态的熔断器
2. **令牌桶**：实现令牌桶限流器，支持每秒和每分钟限流
3. **超时+重试**：实现带指数退避的数据库查询重试
4. **服务降级**：设计一个功能开关系统，支持动态关闭/开启功能
5. **全链路容错**：设计一个场景：DB 挂了但玩家仍然可以操作（不能保存，但能玩）

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 熔断器 | 连续失败超过阈值后快速失败并降级 |
| 令牌桶 | 控制请求速率，允许突发流量 |
| 漏桶 | 强制平滑流量，超出丢弃 |
| 超时 | 防止请求无限等待 |
| 重试 | 失败后延时重试，指数退避+抖动 |
| 降级 | 非核心功能关闭，保证核心功能可用 |
| 幂等 | 重试不产生副作用 |
