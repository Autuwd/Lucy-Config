# Day 19：排行榜与匹配系统

## 一、排行榜设计

### 排行榜类型

| 类型 | 更新频率 | 数据量 | 技术方案 |
|------|---------|--------|---------|
| 战力榜 | 玩家属性变化时更新 | 全服玩家 | Redis ZSet |
| 等级榜 | 升级时更新 | 全服玩家 | Redis ZSet |
| 竞技场 | 战斗结束更新 | 前 N 名 | Redis ZSet |
| 活动榜 | 活动期间频繁更新 | 活跃玩家 | Redis ZSet + 定时器 |
| 全服历史 | 每天结算一次 | 历史数据 | MySQL 存储 |

### ZSet 排行榜实现

```csharp
public class RankingService
{
    private readonly IDatabase _redis;
    private const string LeaderboardKey = "rank:fight_power";

    // 更新玩家战斗力
    public async Task UpdateFightPower(long playerId, long fightPower)
    {
        await _redis.SortedSetAddAsync(LeaderboardKey,
            playerId.ToString(), fightPower);
    }

    // 获取玩家排名（第几名）
    public async Task<RankInfo> GetPlayerRank(long playerId)
    {
        long? rank = await _redis.SortedSetRankAsync(
            LeaderboardKey, playerId.ToString(), Order.Descending);

        double? score = await _redis.SortedSetScoreAsync(
            LeaderboardKey, playerId.ToString());

        if (rank == null || score == null)
            return null;

        return new RankInfo
        {
            PlayerId = playerId,
            Rank = (int)rank.Value + 1,
            Score = (long)score.Value,
            TotalPlayers = await _redis.SortedSetLengthAsync(LeaderboardKey)
        };
    }

    // 获取 Top 100
    public async Task<List<RankInfo>> GetTop100()
    {
        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(
            LeaderboardKey, 0, 99, Order.Descending);

        return entries.Select((e, i) => new RankInfo
        {
            PlayerId = long.Parse(e.Element),
            Rank = i + 1,
            Score = (long)e.Score
        }).ToList();
    }

    // 获取周围排名（前后各 50）
    public async Task<List<RankInfo>> GetAroundPlayer(long playerId, int radius = 50)
    {
        long? rank = await _redis.SortedSetRankAsync(
            LeaderboardKey, playerId.ToString(), Order.Descending);

        if (rank == null) return new List<RankInfo>();

        long start = Math.Max(0, rank.Value - radius);
        long end = Math.Min(rank.Value + radius,
            await _redis.SortedSetLengthAsync(LeaderboardKey) - 1);

        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(
            LeaderboardKey, start, end, Order.Descending);

        return entries.Select((e, i) => new RankInfo
        {
            PlayerId = long.Parse(e.Element),
            Rank = (int)start + i + 1,
            Score = (long)e.Score
        }).ToList();
    }

    // 排行榜变化通知
    private readonly CancellationTokenSource _cts = new();

    public async Task RankChangeNotifier()
    {
        long lastTopScore = 0;

        while (!_cts.IsCancellationRequested)
        {
            await Task.Delay(60000); // 每分钟检查

            var top = await GetTop100();
            if (top.Count > 0 && top[0].Score > lastTopScore)
            {
                lastTopScore = top[0].Score;
                // 通知全服：有人登顶了！
                await BroadcastRankChange(top[0]);
            }
        }
    }
}

public class RankInfo
{
    public long PlayerId { get; set; }
    public int Rank { get; set; }
    public long Score { get; set; }
    public int TotalPlayers { get; set; }
}
```

### 多条件排行榜（等级相同按经验排）

```csharp
// ZSet 的 score 是 double，可以编码多个条件
// 高位放等级，低位放经验

public double EncodeRankScore(int level, long exp)
{
    // 等级占高 32 位，经验占低 32 位
    // 等级最高 100000 级，经验最大 999999999
    return level * 1_000_000_000.0 + exp;
}

public (int level, long exp) DecodeRankScore(double score)
{
    int level = (int)(score / 1_000_000_000.0);
    long exp = (long)(score % 1_000_000_000.0);
    return (level, exp);
}

// 更新
await _redis.SortedSetAddAsync("rank:level",
    playerId.ToString(),
    EncodeRankScore(player.Level, player.Exp));

// 排第一的是等级最高的，同等经验下经验也最高
```

---

## 二、ELO 匹配系统

### ELO 原理

```
每个玩家有一个 ELO 分数（天梯分）
匹配时找分数接近的玩家
战斗后根据预期胜率调整分数

预期胜率：
  Ea = 1 / (1 + 10^((Rb - Ra) / 400))

分数变化：
  Ra_new = Ra + K * (Sa - Ea)
  其中 Sa = 1(胜) / 0.5(平) / 0(负)
  K = 32 (新手高) / 24 (老手低)
```

### ELO 计算

```csharp
public class EloCalculator
{
    // 计算预期胜率
    public static double ExpectedScore(double ratingA, double ratingB)
    {
        return 1.0 / (1.0 + Math.Pow(10, (ratingB - ratingA) / 400.0));
    }

    // 计算分数变化
    public static (double changeA, double changeB) CalculateChange(
        double ratingA, double ratingB, double scoreA)
    {
        double expectedA = ExpectedScore(ratingA, ratingB);
        double expectedB = 1 - expectedA;

        // 根据当前分段调整 K 值
        double kA = GetKFactor(ratingA);
        double kB = GetKFactor(ratingB);

        double changeA = kA * (scoreA - expectedA);
        double changeB = kB * ((1 - scoreA) - expectedB);

        return (changeA, changeB);
    }

    private static double GetKFactor(double rating)
    {
        if (rating < 1400) return 40;   // 新手，快速调整
        if (rating < 2000) return 32;   // 普通
        if (rating < 2400) return 24;   // 高分段
        return 16;                       // 顶尖选手
    }
}

// ELO 排行榜
public class EloRankingService
{
    private readonly IDatabase _redis;
    private const string EloKey = "rank:elo";

    // 获取玩家 ELO
    public async Task<double> GetElo(long playerId)
    {
        var score = await _redis.SortedSetScoreAsync(EloKey, playerId.ToString());
        return score ?? 1000; // 初始 1000 分
    }

    // 更新 ELO（战斗结束后）
    public async Task UpdateElo(long winnerId, long loserId)
    {
        double winnerElo = await GetElo(winnerId);
        double loserElo = await GetElo(loserId);

        var (winnerChange, loserChange) = EloCalculator.CalculateChange(
            winnerElo, loserElo, 1.0);

        var batch = _redis.CreateBatch();
        batch.SortedSetIncrementAsync(EloKey, winnerId.ToString(), winnerChange);
        batch.SortedSetIncrementAsync(EloKey, loserId.ToString(), loserChange);
        batch.Execute();

        // 记录战绩
        await RecordMatchResult(winnerId, loserId, winnerChange, loserChange);
    }
}
```

---

## 三、匹配队列

### 匹配池设计

```csharp
public class MatchService
{
    // 匹配队列（按 ELO 分数分桶）
    private readonly ConcurrentDictionary<int, ConcurrentQueue<MatchPlayer>> _queues = new();
    private readonly SemaphoreSlim _matchSignal = new(0);
    private volatile bool _running = true;

    // 加入匹配
    public async Task<bool> JoinMatchQueue(long playerId, int eloScore, List<int> teamMembers = null)
    {
        // 检查是否已经在匹配中
        if (IsInMatch(playerId))
            return false;

        // 计算分桶（每 100 分一个桶）
        int bucket = eloScore / 100 * 100;

        var queue = _queues.GetOrAdd(bucket, _ => new ConcurrentQueue<MatchPlayer>());

        queue.Enqueue(new MatchPlayer
        {
            PlayerId = playerId,
            EloScore = eloScore,
            JoinTime = DateTime.UtcNow,
            TeamMembers = teamMembers ?? new List<int> { playerId }
        });

        _matchSignal.Release();
        return true;
    }

    // 取消匹配
    public async Task<bool> LeaveMatchQueue(long playerId)
    {
        // 从所有队列中移除（实际用更高效的结构）
        return true;
    }

    // 匹配主循环
    public async Task MatchLoop()
    {
        while (_running)
        {
            await _matchSignal.WaitAsync();

            // 尝试匹配
            var match = TryMatch();
            if (match != null)
            {
                // 通知双方匹配成功
                await NotifyMatchResult(match);

                // 创建房间/战斗
                await CreateBattleRoom(match);
            }
        }
    }

    // 匹配逻辑
    private MatchResult TryMatch()
    {
        // 按分桶扫描，从高分到低分
        foreach (var bucket in _queues.Keys.OrderByDescending(k => k))
        {
            if (_queues.TryGetValue(bucket, out var queue) && queue.Count >= 2)
            {
                // 从队列中取出两个玩家
                if (queue.TryDequeue(out var p1) && queue.TryDequeue(out var p2))
                {
                    return new MatchResult
                    {
                        PlayerA = p1,
                        PlayerB = p2,
                        EstimatedElo = (p1.EloScore + p2.EloScore) / 2
                    };
                }
            }
        }

        // 尝试相邻桶匹配（扩大范围）
        return TryCrossBucketMatch();
    }

    // 搜索时间延长逻辑（超过 30 秒扩大匹配范围）
    private MatchResult TryCrossBucketMatch()
    {
        var allPlayers = new List<MatchPlayer>();

        foreach (var queue in _queues.Values)
        {
            while (queue.TryPeek(out var p))
            {
                if ((DateTime.UtcNow - p.JoinTime).TotalSeconds > 30)
                {
                    queue.TryDequeue(out p);
                    allPlayers.Add(p);
                }
                else break;
            }
        }

        // 超时玩家中找分数最接近的一对
        if (allPlayers.Count >= 2)
        {
            allPlayers.Sort((a, b) => a.EloScore.CompareTo(b.EloScore));

            int bestDiff = int.MaxValue;
            int bestIndex = 0;

            for (int i = 0; i < allPlayers.Count - 1; i++)
            {
                int diff = Math.Abs(allPlayers[i].EloScore - allPlayers[i + 1].EloScore);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestIndex = i;
                }
            }

            return new MatchResult
            {
                PlayerA = allPlayers[bestIndex],
                PlayerB = allPlayers[bestIndex + 1],
                MatchedWithTimeout = true
            };
        }

        return null;
    }
}

public class MatchPlayer
{
    public long PlayerId { get; set; }
    public int EloScore { get; set; }
    public DateTime JoinTime { get; set; }
    public List<int> TeamMembers { get; set; }
}

public class MatchResult
{
    public MatchPlayer PlayerA { get; set; }
    public MatchPlayer PlayerB { get; set; }
    public int EstimatedElo { get; set; }
    public bool MatchedWithTimeout { get; set; }
}
```

---

## 四、定时任务

### 定时排行榜结算

```csharp
public class ScheduledTaskService
{
    private readonly IServiceProvider _services;

    // 每日 21:00 结算竞技场排行榜
    public async Task DailyArenaSettlement()
    {
        var now = DateTime.Now;
        var nextRun = now.Date.AddHours(21);

        if (now > nextRun)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        await Task.Delay(delay);

        Log.Information("开始每日竞技场结算");

        using var scope = _services.CreateScope();
        var ranking = scope.ServiceProvider.GetRequiredService<IRankingService>();

        // 1. 锁定排行榜
        var top100 = await ranking.GetTop100();

        // 2. 发放奖励
        foreach (var entry in top100)
        {
            var reward = GetRankReward(entry.Rank);
            await GrantReward(entry.PlayerId, reward);
        }

        // 3. 记录历史
        await SaveRankingSnapshot(top100);

        // 4. 重置竞技场次数
        await ResetArenaCount();

        Log.Information("竞技场结算完成，共 {Count} 名玩家获得奖励", top100.Count);
    }

    // 每日刷新（05:00）
    public async Task DailyReset()
    {
        await WaitUntilTime(5, 0);

        Log.Information("开始每日重置");

        // 重置日常任务进度
        // 重置每日购买次数
        // 重置活跃度
        // 清理过期邮件

        Log.Information("每日重置完成");
    }

    // 每周刷新（周一 05:00）
    public async Task WeeklyReset()
    {
        while (true)
        {
            var now = DateTime.Now;
            var nextMonday = now.AddDays((7 - (int)now.DayOfWeek + 1) % 7)
                .Date.AddHours(5);

            if (nextMonday <= now)
                nextMonday = nextMonday.AddDays(7);

            await Task.Delay(nextMonday - now);

            // 重置周常任务
            // 重置周限购
            // 结算公会战
        }
    }

    private async Task WaitUntilTime(int hour, int minute)
    {
        var now = DateTime.Now;
        var target = now.Date.AddHours(hour).AddMinutes(minute);

        if (target <= now)
            target = target.AddDays(1);

        await Task.Delay(target - now);
    }
}
```

### Quartz.NET 框架

```csharp
// dotnet add package Quartz

public class GameJobFactory
{
    public async Task SetupScheduler()
    {
        var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
        await scheduler.Start();

        // 每日结算
        var dailyJob = JobBuilder.Create<DailySettlementJob>()
            .WithIdentity("dailySettlement", "rank")
            .Build();

        var dailyTrigger = TriggerBuilder.Create()
            .WithIdentity("dailyTrigger", "rank")
            .WithCronSchedule("0 0 21 * * ?") // 每天 21:00
            .Build();

        await scheduler.ScheduleJob(dailyJob, dailyTrigger);

        // 每周一结算
        var weeklyTrigger = TriggerBuilder.Create()
            .WithIdentity("weeklyTrigger", "rank")
            .WithCronSchedule("0 0 5 ? * MON") // 每周一 05:00
            .Build();

        await scheduler.ScheduleJob(dailyJob, weeklyTrigger);

        // 每 5 分钟清理过期邮件
        var mailJob = JobBuilder.Create<CleanupExpiredMailJob>()
            .WithIdentity("cleanupMail", "maintenance")
            .Build();

        var mailTrigger = TriggerBuilder.Create()
            .WithIdentity("mailTrigger", "maintenance")
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(5)
                .RepeatForever())
            .Build();

        await scheduler.ScheduleJob(mailJob, mailTrigger);
    }
}

public class DailySettlementJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // 执行每日结算逻辑
        Log.Information("竞技场结算任务开始");
    }
}
```

---

## 五、练习

1. **排行榜**：用 ZSet 实现全服战力排行榜，支持 Top 50 + 个人排名
2. **ELO 匹配**：实现完整的匹配队列 + ELO 计算
3. **多条件排序**：等级相同的按经验排，实现编码/解码
4. **定时任务**：用 Quartz.NET 实现每日任务重置
5. **超时匹配扩大**：实现等待超过 30 秒后逐步扩大匹配范围

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| ZSet 排行榜 | Redis Sorted Set 天然支持排行榜 |
| 多条件排序 | 用 double 编码多个排序条件（等级+经验） |
| ELO | 预期胜率 + K 系数调整，匹配公平性 |
| 分桶匹配 | 按分数分桶，桶内找对手 |
| 超时扩大 | 等太久就放宽匹配条件 |
| Quartz.NET | 游戏服务器定时任务的工业级方案 |
