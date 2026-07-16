# Day 26：日志分析与数据运营

## 一、游戏运营指标

### 核心指标

```csharp
// DAU (Daily Active Users) 日活跃用户
public class DailyMetrics
{
    public DateTime Date { get; set; }
    public long DAU { get; set; }          // 日活跃
    public long WAU { get; set; }          // 周活跃
    public long MAU { get; set; }          // 月活跃
    public long NewUsers { get; set; }     // 新增用户
    public long NewPlayers { get; set; }   // 新增创角

    // 付费
    public decimal Revenue { get; set; }   // 总收入
    public int PayingUsers { get; set; }   // 付费人数
    public double ARPU => DAU > 0 ? (double)Revenue / DAU : 0;           // 每用户收入
    public double ARPPU => PayingUsers > 0 ? (double)Revenue / PayingUsers : 0;  // 每付费用户收入
    public double LTV { get; set; }        // 生命周期价值

    // 留存
    public double RetentionDay1 { get; set; }  // 次日留存
    public double RetentionDay7 { get; set; }  // 7 日留存
    public double RetentionDay30 { get; set; } // 30 日留存
}
```

### 指标计算

```csharp
public class AnalyticsService
{
    private readonly IDatabase _redis;

    // 计算 DAU
    public async Task<long> CalculateDAU(DateTime date)
    {
        string key = $"dau:{date:yyyyMMdd}";
        return await _redis.SetLengthAsync(key);
    }

    // 记录用户活跃
    public async Task RecordActivity(long playerId)
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        string weekKey = $"wau:{GetWeekKey()}";
        string monthKey = $"mau:{DateTime.Now:yyyyMM}";

        var batch = _redis.CreateBatch();
        batch.SetAddAsync($"dau:{today}", playerId.ToString());
        batch.KeyExpireAsync($"dau:{today}", TimeSpan.FromDays(31));

        batch.SetAddAsync(weekKey, playerId.ToString());
        batch.KeyExpireAsync(weekKey, TimeSpan.FromDays(62));

        batch.SetAddAsync(monthKey, playerId.ToString());
        batch.KeyExpireAsync(monthKey, TimeSpan.FromDays(366));

        batch.Execute();
    }

    // 计算留存率
    public async Task<double> CalculateRetention(int daysAfter)
    {
        DateTime installDate = DateTime.Now.AddDays(-daysAfter);
        string installKey = $"new_users:{installDate:yyyyMMdd}";
        string todayKey = $"dau:{DateTime.Now:yyyyMMdd}";

        var day0Users = await _redis.SetMembersAsync(installKey);
        var todayUsers = await _redis.SetMembersAsync(todayKey);

        var retained = day0Users.Intersect(todayUsers).Count();
        return day0Users.Length > 0
            ? (double)retained / day0Users.Length * 100
            : 0;
    }

    private string GetWeekKey()
    {
        var now = DateTime.Now;
        var weekStart = now.AddDays(-(int)now.DayOfWeek);
        return weekStart.ToString("yyyyMMdd");
    }
}
```

---

## 二、ELK 栈

### Elasticsearch + Filebeat + Kibana

```yaml
# docker-compose.elk.yml
version: '3.8'

services:
  elasticsearch:
    image: elasticsearch:8.12.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - ES_JAVA_OPTS=-Xms4g -Xmx4g
    volumes:
      - es-data:/usr/share/elasticsearch/data
    ports:
      - "9200:9200"

  kibana:
    image: kibana:8.12.0
    ports:
      - "5601:5601"
    depends_on:
      - elasticsearch

  filebeat:
    image: elastic/filebeat:8.12.0
    volumes:
      - ./filebeat.yml:/usr/share/filebeat/filebeat.yml
      - /opt/game-server/logs:/var/log/game-server:ro
    depends_on:
      - elasticsearch
```

### Filebeat 配置

```yaml
# filebeat.yml
filebeat.inputs:
  - type: log
    enabled: true
    paths:
      - /var/log/game-server/*.log
    multiline:
      pattern: '^\['  # 以 [ 开头为新行
      negate: true
      match: after

output.elasticsearch:
  hosts: ["elasticsearch:9200"]
  index: "game-logs-%{+yyyy.MM.dd}"

setup.template.settings:
  index.number_of_shards: 1
```

### 结构化日志查询

```csharp
// 使用 Elasticsearch 查询日志
public class LogQueryService
{
    private readonly ElasticClient _client;

    public async Task<List<BattleLog>> QueryBattleLogs(long playerId, DateTime from, DateTime to)
    {
        var response = await _client.SearchAsync<BattleLog>(s => s
            .Index("game-logs-*")
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Field(f => f.PlayerId).Value(playerId)),
                        m => m.Term(t => t.Field("event_type").Value("battle")),
                        m => m.DateRange(r => r.Field("@timestamp")
                            .GreaterThanOrEqual(from)
                            .LessThanOrEqual(to))
                    )
                )
            )
            .Sort(so => so.Descending("@timestamp"))
            .Size(100)
        );

        return response.Documents.ToList();
    }

    // 查询异常日志
    public async Task<long> CountErrors(int lastMinutes = 60)
    {
        var since = DateTime.UtcNow.AddMinutes(-lastMinutes);

        var response = await _client.CountAsync<LogEntry>(c => c
            .Index("game-logs-*")
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Field("level").Value("Error")),
                        m => m.DateRange(r => r.Field("@timestamp")
                            .GreaterThanOrEqual(since))
                    )
                )
            )
        );

        return response.Count;
    }
}
```

---

## 三、Pandas 数据处理（Python）

```python
# 安装: pip install pandas openpyxl matplotlib

import pandas as pd
import matplotlib.pyplot as plt

# 从 CSV 加载数据
df = pd.read_csv('game_logs.csv', parse_dates=['timestamp'])

# 按日期分组统计
daily_stats = df.groupby(df['timestamp'].dt.date).agg({
    'player_id': 'nunique',    # DAU
    'revenue': 'sum',          # 总收入
    'is_new': 'sum'            # 新增
}).rename(columns={
    'player_id': 'DAU',
    'revenue': 'Revenue',
    'is_new': 'NewUsers'
})

daily_stats['ARPU'] = daily_stats['Revenue'] / daily_stats['DAU']
daily_stats['ARPPU'] = daily_stats['Revenue'] / daily_stats['PayingUsers']

print(daily_stats)

# 留存率计算
def retention_cohort(df, install_date):
    """计算指定安装日期的新用户留存"""
    day0_users = df[df['install_date'] == install_date]['player_id'].unique()

    retentions = {}
    for day in [1, 3, 7, 14, 30]:
        date = install_date + pd.Timedelta(days=day)
        active = df[df['timestamp'].dt.date == date.date()]['player_id'].unique()
        retained = len(set(day0_users) & set(active))
        retentions[f'Day{day}'] = retained / len(day0_users) if len(day0_users) > 0 else 0

    return retentions

# 玩家行为分析
def player_behavior_analysis(df):
    """分析玩家行为模式"""
    # 在线时长分布
    df['session_length'] = (df['logout_time'] - df['login_time']).dt.total_seconds() / 60

    # 按玩家聚合
    player_stats = df.groupby('player_id').agg({
        'session_length': 'mean',
        'revenue': 'sum',
        'level': 'max',
        'quest_completed': 'sum'
    })

    # 相关性分析
    print(player_stats.corr())

    # 付费玩家 vs 非付费玩家
    paying = player_stats[player_stats['revenue'] > 0]
    non_paying = player_stats[player_stats['revenue'] == 0]

    print(f"付费玩家数: {len(paying)}, 平均在线: {paying['session_length'].mean():.1f}min")
    print(f"非付费玩家数: {len(non_paying)}, 平均在线: {non_paying['session_length'].mean():.1f}min")
```

---

## 四、玩家行为分析

```csharp
public class PlayerBehaviorAnalyzer
{
    private readonly IDatabase _redis;

    // 漏斗分析（注册→创角→打1级→充值）
    public async Task<FunnelData> GetConversionFunnel(DateTime date)
    {
        var funnel = new FunnelData();

        // 每一步的用户集合
        funnel.TotalVisits = await _redis.SetLengthAsync($"dau:{date:yyyyMMdd}");
        funnel.Registered = await _redis.SetLengthAsync($"registered:{date:yyyyMMdd}");
        funnel.CreatedCharacter = await _redis.SetLengthAsync($"created_char:{date:yyyyMMdd}");
        funnel.ReachedLevel5 = await _redis.SetLengthAsync($"level5:{date:yyyyMMdd}");
        funnel.FirstRecharge = await _redis.SetLengthAsync($"recharge:{date:yyyyMMdd}");

        return funnel;
    }

    // 玩家分层
    public async Task<PlayerSegmentation> SegmentPlayers()
    {
        var seg = new PlayerSegmentation();

        // 按等级分层
        seg.LowLevel = await CountPlayersByLevel(1, 20);
        seg.MidLevel = await CountPlayersByLevel(21, 50);
        seg.HighLevel = await CountPlayersByLevel(51, 100);

        // 按付费分层
        seg.Whales = await CountPlayersByRecharge(10000, null);  // 大 R
        seg.Dolphins = await CountPlayersByRecharge(1000, 9999); // 中 R
        seg.Minis = await CountPlayersByRecharge(1, 999);       // 小 R
        seg.FreePlayers = await CountPlayersByRecharge(0, 0);    // 免费

        return seg;
    }

    // 流失预警
    public async Task<List<long>> GetChurningPlayers(int notLoggedInDays = 7)
    {
        // 7 天前活跃，但最近 3 天没来的玩家
        var active7DaysAgo = await _redis.SetMembersAsync(
            $"dau:{DateTime.Now.AddDays(-7):yyyyMMdd}");
        var activeToday = await _redis.SetMembersAsync(
            $"dau:{DateTime.Now:yyyyMMdd}");

        return active7DaysAgo
            .Select(long.Parse)
            .Where(id => !activeToday.Contains(id))
            .Take(100)
            .ToList();
    }
}

public class FunnelData
{
    public long TotalVisits { get; set; }
    public long Registered { get; set; }
    public long CreatedCharacter { get; set; }
    public long ReachedLevel5 { get; set; }
    public long FirstRecharge { get; set; }

    public double RegisterRate => (double)Registered / TotalVisits * 100;
    public double CreateCharRate => (double)CreatedCharacter / Registered * 100;
    public double Level5Rate => (double)ReachedLevel5 / CreatedCharacter * 100;
    public double RechargeRate => (double)FirstRecharge / ReachedLevel5 * 100;
}

public class PlayerSegmentation
{
    public long LowLevel { get; set; }
    public long MidLevel { get; set; }
    public long HighLevel { get; set; }

    public long Whales { get; set; }
    public long Dolphins { get; set; }
    public long Minis { get; set; }
    public long FreePlayers { get; set; }

    public long Total => LowLevel + MidLevel + HighLevel;
}
```

---

## 五、运营数据看板

```csharp
public class OperationalDashboard
{
    // 每日运营报告
    public async Task<DailyReport> GenerateDailyReport()
    {
        var yesterday = DateTime.Now.AddDays(-1);

        var report = new DailyReport
        {
            Date = yesterday,
            DAU = await _analytics.CalculateDAU(yesterday),
            NewUsers = await GetNewUsers(yesterday),
            Revenue = await GetRevenue(yesterday),
            PayingUsers = await GetPayingUsers(yesterday),
            AvgOnlineTime = await GetAvgOnlineTime(yesterday),
        };

        report.ARPU = report.DAU > 0 ? report.Revenue / report.DAU : 0;
        report.ARPPU = report.PayingUsers > 0 ? report.Revenue / report.PayingUsers : 0;

        // 对比上周同期
        var lastWeek = yesterday.AddDays(-7);
        report.LastWeekDAU = await _analytics.CalculateDAU(lastWeek);
        report.DAUChange = report.LastWeekDAU > 0
            ? (report.DAU - report.LastWeekDAU) / (double)report.LastWeekDAU * 100
            : 0;

        return report;
    }
}

public class DailyReport
{
    public DateTime Date { get; set; }
    public long DAU { get; set; }
    public long LastWeekDAU { get; set; }
    public double DAUChange { get; set; }
    public long NewUsers { get; set; }
    public decimal Revenue { get; set; }
    public int PayingUsers { get; set; }
    public double ARPU { get; set; }
    public double ARPPU { get; set; }
    public double AvgOnlineTime { get; set; } // 分钟
    public double RetentionDay1 { get; set; }
    public double RetentionDay7 { get; set; }
}
```

---

## 六、练习

1. **DAU 计算**：用 Redis Bitmap 或 Set 实现 DAU
2. **留存率**：计算次日/7 日留存率
3. **ELK 搭建**：用 Docker Compose 搭建 ELK 查看实时日志
4. **玩家分析**：分析 DAU 趋势、付费率、等级分布
5. **指标看板**：设计一个显示核心指标的运营看板

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| DAU/WAU/MAU | 日/周/月活跃用户数 |
| ARPU/ARPPU | 每用户/每付费用户平均收入 |
| LTV | 用户生命周期价值 |
| 留存率 | 新增用户第二天/7 天/30 天后还在的比例 |
| 漏斗分析 | 从访问到付费每一步的转化率 |
| ELK | Elasticsearch + Logstash + Kibana 日志系统 |
| 流失预警 | 根据登录间隔预测可能流失的玩家 |
