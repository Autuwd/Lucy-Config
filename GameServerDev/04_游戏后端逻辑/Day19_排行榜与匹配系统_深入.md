# Day 19：排行榜与匹配系统 — 进阶深入

## 一、Redis Sorted Set 排行榜进阶：破平策略

### 时间戳破平

当多名玩家分数相同时，需要二次排序。常见做法是将时间戳编码进 score：

```csharp
public class TieBreakingRanking
{
    // 方案1: 时间戳倒序编码（先到先得，先达到该分数的人排前面）
    // score = 实际分数 * 10^N + (MAX_TIMESTAMP - 到达时间)
    // 注意：double 只有 53 位精度，需要控制位数

    private const long TimestampMax = 1_000_000_000_000L; // 约 2001-09-09 的时间戳
    private const long ScoreMultiplier = 1_000_000_000_000L; // 12位

    public double EncodeScoreWithTime(long score, long achieveTimestamp)
    {
        // 高分在前，同时同分先达到的在前
        return score * ScoreMultiplier + (TimestampMax - achieveTimestamp);
    }

    // 用法：升级/战力变化时记录当前时间戳
    public async Task UpdateLevelRanking(long playerId, int level, long exp)
    {
        long achieveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        double encoded = EncodeScoreWithTime(
            level * 1_000_000L + exp, // 等级+经验合并
            achieveTimestamp);

        await _redis.SortedSetAddAsync("rank:level", playerId.ToString(), encoded);
    }

    // 方案2: 使用 ZSet 的词典排序做二次排序
    // 分数相同的情况下，ZSet 按 member 的字典序排列
    // 可以利用这个特性，把时间编码到 member 中
    public string EncodeMemberWithTime(long playerId, long timestamp)
    {
        // member 格式: "时间戳_PlayerId"
        // 时间戳越大（越晚）越靠后（但我们要越早到达越靠前）
        long inverted = long.MaxValue - timestamp;
        return $"{inverted:D20}_{playerId:D10}";
    }

    // 解码
    public (long playerId, long timestamp) DecodeMember(string member)
    {
        var parts = member.Split('_');
        long inverted = long.Parse(parts[0]);
        long playerId = long.Parse(parts[1]);
        long timestamp = long.MaxValue - inverted;
        return (playerId, timestamp);
    }
}
```

### 分段排行榜（Top-N 性能优化）

当总玩家数量极大（百万级），维护一个全服 Sorted Set 性能会下降。可以用分段策略：

```csharp
public class TieredLeaderboard
{
    // 分段：青铜 <1000, 白银 <3000, 黄金 <5000, 钻石 <8000, 王者 8000+
    private readonly Dictionary<string, (double min, double max)> _tiers = new()
    {
        ["bronze"] = (0, 999),
        ["silver"] = (1000, 2999),
        ["gold"] = (3000, 4999),
        ["diamond"] = (5000, 7999),
        ["king"] = (8000, double.MaxValue)
    };

    private const string GlobalKey = "rank:global";
    private const int TopCacheSize = 100;

    // 更新分数时，决定是否要更新 Top 缓存
    public async Task UpdateScore(long playerId, double newScore)
    {
        var oldScore = await _redis.SortedSetScoreAsync(GlobalKey, playerId.ToString());

        // 更新主排行榜
        await _redis.SortedSetAddAsync(GlobalKey, playerId.ToString(), newScore);

        // 检查是否影响 Top 100
        if (oldScore.HasValue)
        {
            long? oldRank = await _redis.SortedSetRankAsync(
                GlobalKey, playerId.ToString(), Order.Descending);
            if (oldRank < TopCacheSize)
            {
                // 更新 Top 缓存
                await RefreshTopCache();
            }
        }
        else if (newScore > 0)
        {
            // 新玩家，检查是否进入 Top
            long? newRank = await _redis.SortedSetRankAsync(
                GlobalKey, playerId.ToString(), Order.Descending);
            if (newRank < TopCacheSize)
            {
                await RefreshTopCache();
            }
        }
    }

    // 分段查询：只查自己分段内的排行
    public async Task<List<RankEntry>> GetTierRanking(
        long playerId, int tierMin, int tierMax)
    {
        // 使用 ZSet 的分数范围查询
        var entries = await _redis.SortedSetRangeByScoreWithScoresAsync(
            GlobalKey, tierMin, tierMax,
            Exclude.None, Order.Descending);

        return entries.Select((e, i) => new RankEntry
        {
            PlayerId = long.Parse(e.Element),
            Score = e.Score,
            Rank = i + 1
        }).ToList();
    }

    // 定期刷新 Top 缓存到本地内存
    private List<RankEntry> _cachedTop100 = new();
    private DateTime _lastCacheRefresh = DateTime.MinValue;

    private async Task RefreshTopCache()
    {
        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(
            GlobalKey, 0, TopCacheSize - 1, Order.Descending);

        _cachedTop100 = entries.Select((e, i) => new RankEntry
        {
            PlayerId = long.Parse(e.Element),
            Score = e.Score,
            Rank = i + 1
        }).ToList();

        _lastCacheRefresh = DateTime.UtcNow;
    }

    // 优先读缓存
    public Task<List<RankEntry>> GetTop100Cached()
    {
        if (DateTime.UtcNow - _lastCacheRefresh < TimeSpan.FromSeconds(30))
        {
            return Task.FromResult(_cachedTop100);
        }
        return GetTop100();
    }
}
```

---

## 二、Glicko-2 评分系统

### ELO 的局限

```
ELO 的问题：
  1. 不衡量置信度：100局玩家和10局玩家都用同一个评分
  2. 不稳定：新手连赢几局分数可能过高
  3. 不适用于多人和组队

Glicko-2 的改进：
  1. 每个玩家有 3 个参数：Rating (r), RD (偏差度), Volatility (波动率)
  2. RD 越小表示评分越可信
  3. 新手 RD 高，赢输都会大幅调整
  4. 老手 RD 低，调整幅度小
```

```csharp
public class Glicko2Calculator
{
    // Glicko-2 参数
    private const double Tau = 0.5;       // 系统波动常数（推荐 0.3-1.2）
    private const double DefaultRating = 1500.0;
    private const double DefaultRd = 350.0;  // 新手 RD = 350
    private const double DefaultVol = 0.06;
    private const double ConvergenceThreshold = 0.000001;

    public class Glicko2Rating
    {
        public double Rating { get; set; } = DefaultRating;
        public double Rd { get; set; } = DefaultRd;
        public double Volatility { get; set; } = DefaultVol;
    }

    // Glicko-2 的核心计算
    public (Glicko2Rating winner, Glicko2Rating loser) Update(
        Glicko2Rating winner, Glicko2Rating loser)
    {
        // Step 1: 转换到 Glicko-2 尺度
        double mu1 = (winner.Rating - DefaultRating) / 173.7178;
        double phi1 = winner.Rd / 173.7178;
        double sigma1 = winner.Volatility;

        double mu2 = (loser.Rating - DefaultRating) / 173.7178;
        double phi2 = loser.Rd / 173.7178;
        double sigma2 = loser.Volatility;

        // Step 2: 计算对手方的 g 和 E
        double gPhi2 = G(phi2);
        double eMu2 = E(mu1, mu2, phi2);

        double gPhi1 = G(phi1);
        double eMu1 = E(mu2, mu1, phi1);

        // Step 3: 更新胜者
        double v1 = 1.0 / (gPhi2 * gPhi2 * eMu2 * (1 - eMu2));
        double delta1 = v1 * gPhi2 * (1.0 - eMu2); // score=1 胜

        var (newPhi1, newSigma1) = UpdateSingle(phi1, sigma1, delta1, v1);
        double newMu1 = mu1 + newPhi1 * newPhi1 * gPhi2 * (1.0 - eMu2);

        // Step 4: 更新败者
        double v2 = 1.0 / (gPhi1 * gPhi1 * eMu1 * (1 - eMu1));
        double delta2 = v2 * gPhi1 * (0.0 - eMu1); // score=0 负

        var (newPhi2, newSigma2) = UpdateSingle(phi2, sigma2, delta2, v2);
        double newMu2 = mu2 + newPhi2 * newPhi2 * gPhi1 * (0.0 - eMu1);

        // Step 5: 转回原始尺度
        return (
            new Glicko2Rating
            {
                Rating = newMu1 * 173.7178 + DefaultRating,
                Rd = newPhi1 * 173.7178,
                Volatility = newSigma1
            },
            new Glicko2Rating
            {
                Rating = newMu2 * 173.7178 + DefaultRating,
                Rd = newPhi2 * 173.7178,
                Volatility = newSigma2
            }
        );
    }

    // 辅助函数
    private static double G(double phi)
    {
        return 1.0 / Math.Sqrt(1.0 + 3.0 * phi * phi / (Math.PI * Math.PI));
    }

    private static double E(double mu, double muJ, double phiJ)
    {
        return 1.0 / (1.0 + Math.Exp(-G(phiJ) * (mu - muJ)));
    }

    // 单方更新（含波动率计算）
    private static (double newPhi, double newSigma) UpdateSingle(
        double phi, double sigma, double delta, double v)
    {
        // 迭代求解新的波动率 sigma'
        double a = Math.Log(sigma * sigma);
        double deltaSq = delta * delta;

        // 二分法求 sigma'
        double b;
        if (deltaSq > phi * phi + v)
        {
            b = Math.Log(deltaSq - phi * phi - v);
        }
        else
        {
            double k = 1;
            while (F(a - k * Tau, a, delta, phi, v, deltaSq) < 0)
            {
                k += 1;
            }
            b = a - k * Tau;
        }

        double fa = F(a, a, delta, phi, v, deltaSq);
        double fb = F(b, a, delta, phi, v, deltaSq);

        // 二分迭代
        while (Math.Abs(b - a) > ConvergenceThreshold)
        {
            double c = a + (a - b) * fa / (fb - fa);
            double fc = F(c, a, delta, phi, v, deltaSq);

            if (fc * fb <= 0)
            {
                a = b;
                fa = fb;
            }
            else
            {
                fa = fa / 2.0;
            }

            b = c;
            fb = fc;
        }

        double newSigma = Math.Exp(a / 2.0);

        // 计算新的 phi*
        double phiStar = Math.Sqrt(phi * phi + newSigma * newSigma);

        // 计算新的 phi'
        double newPhi = 1.0 / Math.Sqrt(1.0 / (phiStar * phiStar) + 1.0 / v);

        return (newPhi, newSigma);
    }

    private static double F(double x, double a, double delta,
        double phi, double v, double deltaSq)
    {
        double ex = Math.Exp(x);
        double phiSq = phi * phi;
        double vSq = v * v;
        double dSq = deltaSq;

        double numerator = ex * (dSq - phiSq - v - ex) / (2.0 * (phiSq + v + ex) * (phiSq + v + ex));
        double denominator = (x - a) / (Tau * Tau);

        return numerator - denominator;
    }
}
```

### Glicko-2 存储与查询

```csharp
public class Glicko2Repository
{
    private readonly IDatabase _redis;

    // 每个玩家存为 Hash
    // glicko:{playerId} => { rating, rd, volatility }

    public async Task<Glicko2Calculator.Glicko2Rating> GetRating(long playerId)
    {
        var hash = await _redis.HashGetAllAsync($"glicko:{playerId}");
        if (hash.Length == 0)
            return new Glicko2Calculator.Glicko2Rating();

        return new Glicko2Calculator.Glicko2Rating
        {
            Rating = double.Parse(hash.First(h => h.Name == "rating").Value),
            Rd = double.Parse(hash.First(h => h.Name == "rd").Value),
            Volatility = double.Parse(hash.First(h => h.Name == "vol").Value)
        };
    }

    public async Task SaveRating(long playerId, Glicko2Calculator.Glicko2Rating rating)
    {
        await _redis.HashSetAsync($"glicko:{playerId}", new HashEntry[]
        {
            new("rating", rating.Rating),
            new("rd", rating.Rd),
            new("vol", rating.Volatility)
        });
    }
}
```

---

## 三、多维匹配系统

### 匹配因素权重

```csharp
public class MultiFactorMatchmaker
{
    // 匹配维度及其权重
    public class MatchDimension
    {
        public string Name { get; set; }
        public double Weight { get; set; }       // 权重（总和=1.0）
        public double MaxDiff { get; set; }       // 最大允许差异
    }

    private readonly MatchDimension[] _dimensions =
    {
        new() { Name = "rating", Weight = 0.50, MaxDiff = 500 },
        new() { Name = "level", Weight = 0.20, MaxDiff = 10 },
        new() { Name = "region", Weight = 0.15, MaxDiff = 1 },  // 同区域=0，不同=1
        new() { Name = "win_rate", Weight = 0.15, MaxDiff = 0.3 }
    };

    public class MatchCandidate
    {
        public long PlayerId { get; set; }
        public double Rating { get; set; }
        public int Level { get; set; }
        public int RegionId { get; set; }
        public double WinRate { get; set; }
        public DateTime JoinTime { get; set; }
    }

    // 计算两个玩家的匹配得分（越低越匹配）
    public double CalculateMatchScore(MatchCandidate a, MatchCandidate b)
    {
        double score = 0;

        // 各维度归一化差异 × 权重
        var diffs = new (string name, double value)[]
        {
            ("rating", Math.Abs(a.Rating - b.Rating) / _dimensions[0].MaxDiff),
            ("level", Math.Abs(a.Level - b.Level) / (double)_dimensions[1].MaxDiff),
            ("region", a.RegionId == b.RegionId ? 0 : 1),
            ("win_rate", Math.Abs(a.WinRate - b.WinRate) / _dimensions[3].MaxDiff)
        };

        for (int i = 0; i < _dimensions.Length; i++)
        {
            double normalizedDiff = Math.Min(1.0, diffs[i].value);
            score += normalizedDiff * _dimensions[i].Weight;
        }

        // 等待时间补偿：等待越久，评分越宽松
        double waitMinutes = (DateTime.UtcNow - a.JoinTime).TotalMinutes;
        double waitBonus = Math.Min(0.3, waitMinutes * 0.02); // 每分钟减2%匹配难度
        score = Math.Max(0, score - waitBonus);

        return score;
    }

    // 批量匹配（每2秒执行一次）
    public async Task<List<MatchPair>> BatchMatch(List<MatchCandidate> pool)
    {
        if (pool.Count < 2) return new List<MatchPair>();

        var pairs = new List<MatchPair>();
        var matched = new HashSet<long>();

        // 贪心匹配：遍历每个玩家，找最匹配的对手
        for (int i = 0; i < pool.Count; i++)
        {
            if (matched.Contains(pool[i].PlayerId)) continue;

            double bestScore = double.MaxValue;
            int bestJ = -1;

            for (int j = i + 1; j < pool.Count; j++)
            {
                if (matched.Contains(pool[j].PlayerId)) continue;

                double score = CalculateMatchScore(pool[i], pool[j]);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestJ = j;
                }
            }

            if (bestJ >= 0 && bestScore <= 0.6) // 允许 0.6 分以下匹配
            {
                pairs.Add(new MatchPair
                {
                    PlayerA = pool[i],
                    PlayerB = pool[bestJ],
                    MatchQuality = 1.0 - bestScore
                });
                matched.Add(pool[i].PlayerId);
                matched.Add(pool[bestJ].PlayerId);
            }
        }

        // 根据匹配质量排序（最优的匹配先确认）
        pairs.Sort((a, b) => b.MatchQuality.CompareTo(a.MatchQuality));

        return pairs;
    }
}
```

### 等待时间阶梯

```csharp
public class WaitTimeAwareMatchmaker
{
    // 等待时间阶梯：等待越久，匹配范围越大
    private readonly (int seconds, double rangeMultiplier)[] _tiers =
    {
        (0, 1.0),      // 0s: 同分段
        (15, 1.5),     // 15s: 扩大1.5倍
        (30, 2.0),     // 30s: 扩大2倍
        (60, 3.0),     // 60s: 扩大3倍
        (120, 5.0),    // 2min: 扩大5倍
        (300, 10.0),   // 5min: 不限制
    };

    public double GetRatingRange(long playerId)
    {
        var player = GetQueuedPlayer(playerId);
        double elapsed = (DateTime.UtcNow - player.JoinTime).TotalSeconds;

        double multiplier = 1.0;
        for (int i = _tiers.Length - 1; i >= 0; i--)
        {
            if (elapsed >= _tiers[i].seconds)
            {
                multiplier = _tiers[i].rangeMultiplier;
                break;
            }
        }

        // 基础范围 = 玩家的 RD * 2（Glicko 的偏差度）
        double baseRange = player.Rd * 2;
        return baseRange * multiplier;
    }
}
```

---

## 四、赛季重置策略

### 软重置 vs 硬重置

```csharp
public class SeasonResetService
{
    // 软重置：向 1500 靠拢，增大 RD 让评分更快调整
    public async Task SoftReset(string seasonId)
    {
        foreach (var batch in (await GetAllRatedPlayers()).Chunk(1000))
        {
            await Task.WhenAll(batch.Select(async player =>
            {
                var g = await _glickoRepo.GetRating(player);
                double old = g.Rating;
                g.Rating = (old + 1500) / 2.0;
                g.Rd = Math.Min(350, g.Rd * 1.5);
                await _glickoRepo.SaveRating(player, g);
                await SaveSeasonRecord(seasonId, player, old, g.Rating);
            }));
        }
    }

    public async Task HardReset(string seasonId)
    {
        await _redis.KeyDeleteAsync("rank:elo");
        await SaveSeasonSnapshot(seasonId, await GetTopN(1000));
    }

    public async Task ApplyDecay()
    {
        foreach (var pid in await _db.QueryAsync<long>("SELECT player_id FROM glicko_ratings WHERE last_match_time < DATE_SUB(NOW(), INTERVAL 14 DAY) AND rating > 1500"))
        {
            var g = await _glickoRepo.GetRating(pid);
            double days = (DateTime.UtcNow - GetLastMatchTime(pid)).TotalDays;
            double decay = Math.Min(g.Rating - 1500, days * 10);
            if (decay > 0) { g.Rating -= decay; g.Rd = Math.Min(350, g.Rd + decay * 2); await _glickoRepo.SaveRating(pid, g); }
        }
    }
}
```

---

## 五、邮件系统

### 拉模式 vs 推模式

```csharp
public class MailService
{
    // 混合模式：全服邮件批量SQL写入 + 在线玩家Redis实时通知 + 拉取时缓存
    // CREATE TABLE mails (id BIGINT AUTO_INCREMENT PK, receiver_id, title, content TEXT,
    //     attachments JSON, status TINYINT DEFAULT 0, expired_at DATETIME, INDEX(receiver_id, status))

    public async Task SendMail(Mail mail, List<long> receivers)
    {
        for (int i = 0; i < receivers.Count; i += 500)
        {
            var batch = receivers.Skip(i).Take(500);
            var values = batch.Select(r => $"({mail.SenderId},{r},'{mail.Title}','{mail.Content}','{JsonSerializer.Serialize(mail.Attachments)}',0,NOW(),'{mail.ExpiredAt:yyyy-MM-dd HH:mm:ss}')");
            await _db.ExecuteAsync("INSERT INTO mails (sender_id,receiver_id,title,content,attachments,status,created_at,expired_at) VALUES " + string.Join(",", values));
        }
        foreach (var r in receivers.Where(IsOnline))
            await _redis.PublishAsync($"mail:new:{r}", mail.Title);
    }

    public async Task<List<Mail>> GetPlayerMails(long playerId, int page, int pageSize)
    {
        string key = $"mails:{playerId}:ids";
        var ids = await _redis.ListRangeAsync(key);
        if (ids.Length == 0)
        {
            var dbIds = (await _db.QueryAsync<long>("SELECT id FROM mails WHERE receiver_id = @P AND created_at > DATE_SUB(NOW(), INTERVAL 30 DAY) ORDER BY created_at DESC LIMIT 100", new { P = playerId })).ToList();
            if (dbIds.Any()) { await _redis.ListRightPushAsync(key, dbIds.Select(i => (RedisValue)i.ToString()).ToArray()); await _redis.KeyExpireAsync(key, TimeSpan.FromMinutes(10)); }
            ids = dbIds.Select(i => (RedisValue)i.ToString()).ToArray();
        }
        var pageIds = ids.Skip((page - 1) * pageSize).Take(pageSize).Select(v => (long)v).ToList();
        if (!pageIds.Any()) return new();
        return (await _db.QueryAsync<Mail>("SELECT * FROM mails WHERE id IN @Ids ORDER BY created_at DESC", new { Ids = pageIds })).ToList();
    }

    public async Task<bool> ClaimAttachment(long playerId, long mailId)
    {
        var mail = await _db.QueryFirstOrDefaultAsync<Mail>("SELECT * FROM mails WHERE id = @Id AND receiver_id = @P AND status < 2 AND (expired_at IS NULL OR expired_at > NOW()) FOR UPDATE", new { Id = mailId, P = playerId });
        if (mail == null) return false;
        using var tx = await _db.BeginTransactionAsync();
        try
        {
            foreach (var item in JsonSerializer.Deserialize<List<ItemAttachment>>(mail.Attachments))
                await GrantItem(playerId, item.ItemId, item.Count, tx);
            await _db.ExecuteAsync("UPDATE mails SET status = 2 WHERE id = @Id", new { Id = mailId }, tx);
            await tx.CommitAsync(); return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }
}
```

---

## 六、分布式定时任务

### Cron 与分布式锁

```csharp
public class DistributedScheduler
{
    // Redis 分布式锁保证定时任务只在一个节点执行
    public async Task ScheduleWithLock(string jobName, TimeSpan interval, Func<Task> job)
    {
        while (true)
        {
            var nextRun = DateTime.UtcNow.Add(interval);
            string lockKey = $"scheduler:lock:{jobName}";
            if (await _redis.LockTakeAsync(lockKey, _instanceId, interval))
            {
                try { await job(); }
                catch (Exception ex) { Log.Error(ex, "任务 {Job} 失败", jobName); }
                finally { await _redis.LockReleaseAsync(lockKey, _instanceId); }
            }
            var delay = nextRun - DateTime.UtcNow;
            if (delay > TimeSpan.Zero) await Task.Delay(delay);
        }
    }

    public async Task StartAllSchedulers()
    {
        foreach (var (name, interval, job) in new[] {
            ("rank:arena_daily", TimeSpan.FromDays(1), (Func<Task>)ArenaDailySettlement),
            ("rank:decay", TimeSpan.FromDays(1), ApplyRankDecay),
            ("mail:cleanup", TimeSpan.FromHours(1), CleanupExpiredMails),
            ("anti_addiction", TimeSpan.FromMinutes(30), CheckAntiAddiction)
        }) _ = ScheduleWithLock(name, interval, job);
    }

    public async Task ArenaDailySettlement()
    {
        if (!await _redis.LockTakeAsync("scheduler:lock:arena_settlement", _instanceId, TimeSpan.FromMinutes(5))) return;
        try
        {
            var rankings = await _rankingService.GetTop100();
            foreach (var r in rankings)
                await SendMail(new Mail { SenderId = 0, ReceiverId = r.PlayerId, Title = "竞技场结算", Content = $"排名第{r.Rank}", Attachments = JsonSerializer.Serialize(GetRankReward(r.Rank)), ExpiredAt = DateTime.UtcNow.AddDays(30) }, new List<long> { r.PlayerId });
            await TakeRankingSnapshot();
        }
        finally { await _redis.LockReleaseAsync("scheduler:lock:arena_settlement", _instanceId); }
    }
}
```

---

## 七、防沉迷进阶

### 分布式时长追踪

```csharp
public class AntiAddictionTracker
{
    // Redis 追踪未成年玩家每日时长 + 每月充值上限
    public async Task<PlayTimeStatus> TrackPlayTime(long playerId)
    {
        var realName = await GetRealNameInfo(playerId);
        if (realName == null) return PlayTimeStatus.NotVerified;
        if (!realName.IsMinor) return PlayTimeStatus.Allowed;
        if (!IsAllowedPlayTime(DateTime.UtcNow)) return PlayTimeStatus.TimeRestricted;

        string todayKey = $"playtime:{playerId}:{DateTime.UtcNow:yyyyMMdd}";
        if (!await _redis.KeyExistsAsync(todayKey))
        { await _redis.StringSetAsync(todayKey, "0", TimeSpan.FromDays(2)); }

        int minutesPlayed = int.Parse(await _redis.StringGetAsync(todayKey) ?? "0");
        if (minutesPlayed >= 60) return PlayTimeStatus.ExceededDailyLimit;

        string sessionKey = $"session:{playerId}";
        var sessionStart = await _redis.StringGetAsync(sessionKey);
        if (sessionStart.HasValue && (DateTime.UtcNow - DateTime.Parse(sessionStart)).TotalMinutes >= 30)
            return PlayTimeStatus.NeedsBreak;
        if (!sessionStart.HasValue)
            await _redis.StringSetAsync(sessionKey, DateTime.UtcNow.ToString("O"), TimeSpan.FromHours(3));

        return PlayTimeStatus.Allowed;
    }

    public async Task TickPlayTime(long playerId) =>
        await _redis.StringIncrementAsync($"playtime:{playerId}:{DateTime.UtcNow:yyyyMMdd}");

    public async Task<bool> CheckRechargeLimit(long playerId, decimal amount)
    {
        var realName = await GetRealNameInfo(playerId);
        if (realName == null || !realName.IsMinor) return true;
        string monthKey = $"recharge:{playerId}:{DateTime.UtcNow:yyyyMM}";
        decimal total = decimal.TryParse(await _redis.StringGetAsync(monthKey), out var v) ? v : 0;
        if (realName.Age < 16) { if (amount > 50 || total + amount > 200) return false; }
        else { if (amount > 100 || total + amount > 400) return false; }
        await _redis.StringIncrementAsync(monthKey, (long)(amount * 100));
        return true;
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 时间戳破平 | 将到达时间编码到 ZSet score 中，同分按到达顺序排 |
| 分段排行榜 | 按分数段分桶 + Top 缓存，解决百万级玩家性能 |
| Glicko-2 | 三参数评分：评分、偏差度、波动率，比 ELO 更准确 |
| 多维匹配 | 评分+等级+区域+胜率加权，等待时间补偿放宽约束 |
| 软重置 | 赛季评分向均值靠拢，保留玩家实力信息 |
| 分布式锁 | Redis 锁保证定时任务只在一个节点执行 |
| 邮件批量发送 | 批量 INSERT + 推送在线通知 + 缓存邮件ID列表 |
| 防沉迷追踪 | Redis 记录每日时长/月累计充值，分钟级在线更新 |
