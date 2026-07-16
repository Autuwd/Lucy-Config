# Day 14：Redis 实战应用

## 一、缓存策略

### 缓存模式

#### Cache-Aside (旁路缓存)

```csharp
// 游戏服务器最常见的模式
async Task<PlayerData> GetPlayerAsync(long playerId)
{
    // 1. 查缓存
    var cacheKey = $"player:{playerId}";
    var cached = await _redis.StringGetAsync(cacheKey);

    if (cached.HasValue)
    {
        // 缓存命中
        return JsonSerializer.Deserialize<PlayerData>(cached);
    }

    // 2. 缓存未命中，查数据库
    var player = await _db.GetPlayerAsync(playerId);

    if (player != null)
    {
        // 3. 写入缓存（60 分钟过期）
        await _redis.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(player),
            TimeSpan.FromMinutes(60));
    }

    return player;
}

// 更新数据（先写 DB，再删缓存）
async Task UpdatePlayerAsync(PlayerData player)
{
    // 1. 先写数据库（保证持久化）
    await _db.UpdatePlayerAsync(player);

    // 2. 再删缓存（下次读时重建）
    await _redis.KeyDeleteAsync($"player:{player.PlayerId}");
}
```

#### Read-Through (穿透读)

```csharp
// 缓存层直接负责数据库查询
class CacheAsideProvider<T>
{
    private readonly IDatabase _redis;
    private readonly Func<string, Task<T>> _dbLoader;
    private readonly TimeSpan _ttl;

    public async Task<T> GetAsync(string key)
    {
        var cached = await _redis.StringGetAsync(key);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<T>(cached);

        // 缓存不存在，从数据库加载
        T value = await _dbLoader(key);

        // 写入缓存
        await _redis.StringSetAsync(key,
            JsonSerializer.Serialize(value),
            _ttl);

        return value;
    }
}
```

### 三大缓存问题

#### 缓存穿透

```
问题：查询一个不存在的数据（如不存在的玩家 ID）
      每次都穿透缓存查 DB，DB 压力大
攻击：恶意用不存在的 ID 大量请求

解决方案：
  1. 缓存空值（即使 DB 返回 null 也缓存，短 TTL）
  2. 布隆过滤器（Bloom Filter）
```

```csharp
// 方案 1：缓存空值
async Task<PlayerData> GetPlayerAsync(long playerId)
{
    var cacheKey = $"player:{playerId}";
    var cached = await _redis.StringGetAsync(cacheKey);

    if (cached.HasValue)
    {
        if (cached == EmptyMarker) return null; // 空值标记
        return JsonSerializer.Deserialize<PlayerData>(cached);
    }

    var player = await _db.GetPlayerAsync(playerId);

    // 不管存不存在都缓存
    await _redis.StringSetAsync(cacheKey,
        player != null
            ? JsonSerializer.Serialize(player)
            : EmptyMarker,
        player != null
            ? TimeSpan.FromMinutes(60)
            : TimeSpan.FromMinutes(5)); // 空值短 TTL

    return player;
}

// 方案 2：布隆过滤器（检查 ID 是否存在）
class BloomFilter
{
    private readonly IBloomFilter _filter;

    public bool MightExist(long playerId)
    {
        // 如果返回 false，一定不存在→直接返回 null
        // 如果返回 true，可能不存在→继续查缓存/DB
        return _filter.MightContain(playerId);
    }
}
```

#### 缓存击穿

```
问题：热点 key 过期瞬间，大量请求同时穿透到 DB
场景：服务器配置（非常热门），过期一瞬间所有请求都去查 DB

解决方案：
  1. 互斥锁 (Mutex) — 只让一个请求去 DB 加载
  2. 逻辑过期 — key 不过期，由应用层更新
```

```csharp
// 方案 1：互斥锁
async Task<ServerConfig> GetServerConfigAsync()
{
    const string cacheKey = "config:server";
    var cached = await _redis.StringGetAsync(cacheKey);

    if (cached.HasValue)
        return JsonSerializer.Deserialize<ServerConfig>(cached);

    // 尝试获取锁（只让一个请求去加载）
    const string lockKey = "lock:config:server";
    bool locked = await _redis.LockTakeAsync(lockKey, "1", TimeSpan.FromSeconds(10));

    if (!locked)
    {
        // 没拿到锁，等一会再试
        await Task.Delay(100);
        return await GetServerConfigAsync(); // 递归重试
    }

    try
    {
        // 双重检查（可能别的线程已经加载了）
        cached = await _redis.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<ServerConfig>(cached);

        // 从数据库加载
        var config = await _db.LoadServerConfigAsync();
        await _redis.StringSetAsync(cacheKey,
            JsonSerializer.Serialize(config),
            TimeSpan.FromMinutes(10));
        return config;
    }
    finally
    {
        await _redis.LockReleaseAsync(lockKey, "1");
    }
}
```

#### 缓存雪崩

```
问题：大量 key 同时过期，或 Redis 宕机
      导致所有请求打到 DB，DB 压力暴增

解决方案：
  1. 过期时间加随机值（避免同时过期）
  2. 多级缓存（本地缓存 + Redis）
  3. 限流/降级
  4. Redis 高可用（主从 + Sentinel/Cluster）
```

```csharp
// 过期时间加随机值
await _redis.StringSetAsync(key, value,
    TimeSpan.FromMinutes(60) + TimeSpan.FromSeconds(Random.Shared.Next(300)));

// 本地缓存（二级缓存）
class TwoLevelCache
{
    private MemoryCache _local = new(new MemoryCacheOptions
    {
        SizeLimit = 1000,
        ExpirationScanFrequency = TimeSpan.FromSeconds(30)
    });

    public async Task<T> GetAsync<T>(string key, Func<Task<T>> loader)
    {
        // 1. 查本地缓存
        if (_local.TryGetValue(key, out T cached))
            return cached;

        // 2. 查 Redis
        var redisValue = await _redis.StringGetAsync(key);
        if (redisValue.HasValue)
        {
            var result = JsonSerializer.Deserialize<T>(redisValue);
            _local.Set(key, result, TimeSpan.FromSeconds(30)); // 短 TTL
            return result;
        }

        // 3. 查数据库
        var data = await loader();

        // 写入 Redis（长 TTL）和本地缓存（短 TTL）
        await _redis.StringSetAsync(key,
            JsonSerializer.Serialize(data),
            TimeSpan.FromMinutes(10));
        _local.Set(key, data, TimeSpan.FromSeconds(30));

        return data;
    }
}
```

---

## 二、分布式锁

### 为什么需要分布式锁

```csharp
// 场景：玩家购买限时礼包，只有 100 份
// 多个服务器同时处理，需要跨进程互斥

// ❌ 问题：没有锁
async Task BuyGiftPacket(long playerId)
{
    var stock = await _redis.StringGetAsync("gift:stock");
    if (int.Parse(stock) > 0)
    {
        // 两个服务器同时到这里
        await _redis.StringDecrementAsync("gift:stock"); // 可能扣到 -1
        await GrantGift(playerId);
    }
}
```

### 基于 Redis 的分布式锁

```csharp
// Redis SET NX 实现
async Task<bool> AcquireLockAsync(string key, string token, TimeSpan expiry)
{
    // NX: not exists 时才设置
    // PX: 过期时间（毫秒）
    var result = await _redis.StringSetAsync(key, token, expiry, When.NotExists);
    return result;
}

async Task ReleaseLockAsync(string key, string token)
{
    // Lua 脚本保证原子性：只有持有锁的人才能释放
    const string LuaRelease = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end
    ";
    await _redis.ScriptEvaluateAsync(LuaRelease,
        new RedisKey[] { key },
        new RedisValue[] { token });
}

// 使用
async Task BuyGiftPacket(long playerId)
{
    const string lockKey = "lock:gift:buy";
    string token = Guid.NewGuid().ToString();

    try
    {
        // 尝试获取锁（最多等 3 秒）
        bool acquired = false;
        for (int i = 0; i < 30 && !acquired; i++)
        {
            acquired = await AcquireLockAsync(lockKey, token, TimeSpan.FromSeconds(10));
            if (!acquired) await Task.Delay(100);
        }

        if (!acquired)
            throw new TimeoutException("获取锁超时");

        // 执行业务逻辑
        var stock = await _redis.StringGetAsync("gift:stock");
        if (int.Parse(stock) <= 0)
            throw new Exception("库存不足");

        await _redis.StringDecrementAsync("gift:stock");
        await GrantGift(playerId);
    }
    finally
    {
        await ReleaseLockAsync(lockKey, token);
    }
}
```

### RedLock 算法

```csharp
// Redis 官方推荐的多节点分布式锁
// 在 N/2+1 个 Redis 节点上同时加锁才算成功
// 防止单节点故障导致锁不可靠
```

---

## 三、排行榜实现

### 实时排行榜

```csharp
class LeaderboardService
{
    private readonly IDatabase _redis;
    private const string LeaderboardKey = "leaderboard:level";

    // 更新玩家等级（有并列）
    public async Task UpdateScore(long playerId, int level, long exp)
    {
        // ZSet 的 score 是同级的，用浮点数组合等级+经验
        // score = level + exp / 1000000.0 （经验占小数部分）
        double score = level + exp / 1_000_000.0;
        await _redis.SortedSetAddAsync(LeaderboardKey, playerId.ToString(), score);
    }

    // 获取排名（第几名）
    public async Task<int> GetRank(long playerId)
    {
        long? rank = await _redis.SortedSetRankAsync(
            LeaderboardKey, playerId.ToString(), Order.Descending);
        return rank.HasValue ? (int)rank.Value + 1 : -1;
    }

    // 获取 Top 100
    public async Task<List<LeaderboardEntry>> GetTop100()
    {
        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(
            LeaderboardKey, 0, 99, Order.Descending);

        return entries.Select((e, i) => new LeaderboardEntry
        {
            Rank = i + 1,
            PlayerId = long.Parse(e.Element),
            Score = e.Score
        }).ToList();
    }

    // 获取自己附近排名（前后 10 名）
    public async Task<List<LeaderboardEntry>> GetAroundMe(long playerId, int radius = 10)
    {
        long? rank = await _redis.SortedSetRankAsync(
            LeaderboardKey, playerId.ToString(), Order.Descending);
        if (!rank.HasValue) return new List<LeaderboardEntry>();

        long start = Math.Max(0, rank.Value - radius);
        long end = rank.Value + radius;

        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(
            LeaderboardKey, start, end, Order.Descending);

        return entries.Select((e, i) => new LeaderboardEntry
        {
            Rank = (int)start + i + 1,
            PlayerId = long.Parse(e.Element),
            Score = e.Score
        }).ToList();
    }
}
```

### 天梯分匹配 (ELO)

```csharp
class EloService
{
    // 计算 ELO 变化
    public (double winnerChange, double loserChange) CalculateElo(
        double winnerElo, double loserElo)
    {
        // 预期胜率
        double Ew = 1.0 / (1.0 + Math.Pow(10, (loserElo - winnerElo) / 400.0));
        double El = 1.0 - Ew;

        // K 因子（高分局变化小）
        double K = winnerElo < 2000 ? 32 :
                   winnerElo < 2400 ? 24 : 16;

        double winnerChange = K * (1 - Ew);
        double loserChange = K * (0 - El);

        return (winnerChange, loserChange);
    }
}

// Redis 更新
public async Task UpdateElo(long playerId, double change)
{
    await _redis.SortedSetIncrementAsync("elo:rating",
        playerId.ToString(), change);
}
```

---

## 四、消息队列

### List 实现

```csharp
// List 作为消息队列（LPUSH + BRPOP）
// 优点：简单，支持阻塞读
// 缺点：没有 ACK 机制，消费失败消息丢失

// 生产者（逻辑服务器）
async Task EnqueueMail(long playerId, MailMessage mail)
{
    string json = JsonSerializer.Serialize(mail);
    await _redis.ListLeftPushAsync("queue:mail", json);
}

// 消费者（邮件服务器）
async Task MailConsumerLoop(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        // BRPOP：阻塞弹出，最多等 5 秒
        var item = await _redis.ListRightPopAsync("queue:mail", TimeSpan.FromSeconds(5));

        if (item.HasValue)
        {
            try
            {
                var mail = JsonSerializer.Deserialize<MailMessage>(item);
                await SendMail(mail);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "邮件发送失败");
                // 消息已丢失！解决方案：重新入队
                // await _redis.ListRightPushAsync("queue:mail:failed", item);
            }
        }
    }
}
```

### Stream 实现（推荐）

```csharp
// Redis 5.0+ Stream
// 优点：消费者组、ACK 确认、消息持久化
// 缺点：比 List 复杂

// 生产者
async Task ProduceMailAsync(MailMessage mail)
{
    var fields = new NameValueEntry[]
    {
        new("to", mail.ToPlayerId.ToString()),
        new("title", mail.Title),
        new("content", mail.Content)
    };

    await _redis.StreamAddAsync("stream:mail", fields);
}

// 消费者组（确保消息被处理一次）
async Task InitConsumerGroup()
{
    try
    {
        // 创建消费者组
        await _redis.StreamCreateConsumerGroupAsync(
            "stream:mail", "mail-group", "0-0");
    }
    catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
    {
        // 组已存在
    }
}

// 消费者
async Task ConsumeMailAsync()
{
    var messages = await _redis.StreamReadGroupAsync(
        "stream:mail",          // stream key
        "mail-group",           // 消费者组
        "consumer-1",          // 消费者名
        ">",                    // > 表示只读未投递的消息
        count: 10);             // 一次最多取 10 条

    foreach (var msg in messages)
    {
        try
        {
            var mail = ParseMail(msg.Values);
            await SendMail(mail);

            // 处理成功后 ACK
            await _redis.StreamAcknowledgeAsync("stream:mail", "mail-group", msg.Id);
        }
        catch
        {
            // 不 ACK，消息会重新投递
            // 可以检查失败次数，超过 3 次移到死信队列
        }
    }
}
```

---

## 五、游戏在线玩家管理

```csharp
class OnlineManager
{
    private readonly IDatabase _redis;
    private const string OnlineSetKey = "online:players";
    private const string PlayerPrefix = "player:";

    // 玩家上线
    public async Task OnPlayerLogin(long playerId)
    {
        var batch = _redis.CreateBatch();

        // 添加到在线集合
        batch.SetAddAsync(OnlineSetKey, playerId.ToString());

        // 设置玩家数据缓存
        string cacheKey = $"{PlayerPrefix}{playerId}";
        batch.StringSetAsync(cacheKey, "{}", TimeSpan.FromMinutes(30));

        // 记录最后在线时间
        batch.SortedSetAddAsync("online:lastlogin", playerId.ToString(),
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        batch.Execute();
    }

    // 玩家下线
    public async Task OnPlayerLogout(long playerId)
    {
        var batch = _redis.CreateBatch();
        batch.SetRemoveAsync(OnlineSetKey, playerId.ToString());
        batch.KeyDeleteAsync($"{PlayerPrefix}{playerId}");
        batch.Execute();
    }

    // 检查是否在线
    public async Task<bool> IsOnline(long playerId)
    {
        return await _redis.SetContainsAsync(OnlineSetKey, playerId.ToString());
    }

    // 在线人数
    public async Task<long> OnlineCount()
    {
        return await _redis.SetLengthAsync(OnlineSetKey);
    }

    // 在线玩家列表
    public async Task<List<long>> GetOnlinePlayers()
    {
        var members = await _redis.SetMembersAsync(OnlineSetKey);
        return members.Select(m => long.Parse(m)).ToList();
    }
}
```

---

## 六、练习

1. **缓存穿透防御**：用布隆过滤器 + 空值缓存实现防御方案
2. **分布式锁**：实现一个安全的分布式锁，加自动续期
3. **排行榜**：用 ZSet 实现实时战斗力排行榜，支持 Top 50
4. **消息队列**：用 Stream 实现公会邮件群发
5. **缓存改造**：将某个玩家数据查询从直接查 DB 改为 Cache-Aside 模式

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Cache-Aside | 先查缓存，miss 再查 DB，更新时删缓存 |
| 缓存穿透 | 查不存在的数据防不住 → 布隆过滤器/空值缓存 |
| 缓存击穿 | 热点 key 过期瞬间 → 互斥锁/逻辑过期 |
| 缓存雪崩 | 大量 key 同时过期 → 随机过期时间/多级缓存 |
| 分布式锁 | SET NX + Lua 释放，跨进程互斥 |
| Sorted Set | ZSet 天然适合排行榜 |
| Stream | Redis 5.0+ 消息队列，支持消费者组 |
