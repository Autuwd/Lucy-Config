# Day 26：日志分析与数据运营 — 进阶深入

## 一、ELK 管道性能调优

### 瓶颈分析与配置优化

```yaml
# Elasticsearch 性能调优（游戏日志场景）

# 场景: 每日 500 亿条战斗日志，每条 ~500 字节
# 总数据量: ~250GB/天

# 索引策略
# 按天分索引: game-logs-2026-07-15
# 每天 1 个索引，自动删除 30 天前的索引
# 每个索引 3 个主分片 + 1 个副本

# elasticsearch.yml
cluster.max_shards_per_node: 1000
indices.memory.index_buffer_size: 20%      # 索引缓冲区大小
indices.fielddata.cache.size: 20%           # 字段缓存
indices.query.bool.max_clause_count: 4096   # 最大查询子句数

# 索引模板（针对游戏日志优化）
PUT _index_template/game-logs-template
{
  "index_patterns": ["game-logs-*"],
  "template": {
    "settings": {
      "number_of_shards": 3,
      "number_of_replicas": 1,
      "refresh_interval": "30s",           # 减少刷新频率提升写入
      "translog.durability": "async",      # 异步 translog
      "translog.sync_interval": "5s",
      "indexing.max_docvalue_fields_search": 200
    },
    "mappings": {
      "properties": {
        "@timestamp": { "type": "date" },
        "player_id":  { "type": "long" },
        "event_type": {
          "type": "keyword",
          "ignore_above": 64
        },
        "message": {
          "type": "text",
          "index": false,                  # 不索引原始消息（节省空间）
          "doc_values": false
        },
        "fields": {
          "type": "object",
          "dynamic": true,
          "enabled": false                # 游戏自定义字段不索引
        },
        "server_id": { "type": "keyword" },
        "level": { "type": "keyword" }
      }
    }
  }
}

# Logstash 调优
# pipeline.batch.size: 2000      # 每批处理 2000 条事件
# pipeline.batch.delay: 50       # 等待 50ms 凑批
# pipeline.workers: 8            # 工作线程数 = CPU 核数
```

```csharp
// 日志生产端的优化：批量写而不是逐条写
public class OptimizedLogProducer
{
    private readonly Channel<LogEntry> _channel =
        Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(100000)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

    // 后台批量发送到 Kafka
    public async Task BatchSendLoop(CancellationToken ct)
    {
        var batch = new List<LogEntry>(2000);
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        while (await timer.WaitForNextTickAsync(ct))
        {
            while (batch.Count < 2000 && _channel.Reader.TryRead(out var entry))
            {
                batch.Add(entry);
            }

            if (batch.Count > 0)
            {
                using var producer = new ProducerBuilder<Null, string>(_config).Build();
                foreach (var entry in batch)
                {
                    await producer.ProduceAsync("game-logs",
                        new Message<Null, string>
                        {
                            Value = JsonSerializer.Serialize(entry)
                        });
                }
                batch.Clear();
            }
        }
    }
}
```

---

## 二、Kibana 可视化进阶

```
游戏运营 Kibana 面板设计:

核心 KPI 行（最上面，一眼能看到）:
  - DAU: 日活跃用户数（单值 + 环比箭头）
  - 新增用户: 当日新注册用户
  - ARPU: 每用户平均收入
  - 付费率: 当日付费用户 / 活跃用户
  - 错误率: 服务器错误数 / 请求总数

分析面板:
  - DAU 7 天趋势（折线图 + 上周对比）
  - 留存趋势: Day1/Day7/Day30 留存率折线
  - 用户漏斗: 访问 → 注册 → 创角 → 充值
  - 等级分布: 柱状图（x 轴等级，y 轴人数）
  - 付费金额分布: 按区间（0, 1-100, 100-1000, 1000+）

异常面板:
  - 实时错误日志（最近 100 条）
  - 异常峰值检测（突然的流量/错误上涨）
  - 慢查询日志（> 500ms 的查询）
```

### Kibana 查询 DSL

```jsonc
// 计算 DAU（过去 24 小时不同的 player_id 数）
{
  "query": {
    "bool": {
      "filter": [
        { "range": { "@timestamp": { "gte": "now-24h" } } },
        { "term": { "event_type": "login" } }
      ]
    }
  },
  "aggs": {
    "dau": {
      "cardinality": {
        "field": "player_id",
        "precision_threshold": 40000
      }
    }
  }
}

// 留存计算（查询 Day1 活跃用户在 Day7 是否活跃）
// 用 Elasticsearch Scripted Metric 聚合
{
  "query": {
    "bool": {
      "filter": [
        { "term": { "event_type": "login" } },
        { "range": { "@timestamp": { "gte": "now-7d", "lt": "now-6d" } } }
      ]
    }
  },
  "aggs": {
    "day1_users": {
      "terms": {
        "field": "player_id",
        "size": 100000
      }
    }
  }
}

// 付费转换漏斗
{
  "size": 0,
  "aggs": {
    "funnel": {
      "filters": {
        "filters": {
          "注册": { "term": { "event_type": "register" } },
          "创角": { "term": { "event_type": "create_character" } },
          "充值": { "term": { "event_type": "first_recharge" } }
        }
      }
    }
  }
}
```

---

## 三、玩家生命周期分析

```sql
-- 玩家留存率计算（SQL 版）
WITH install_cohort AS (
    SELECT
        DATE(install_time) AS install_date,
        player_id
    FROM players
    WHERE install_time >= DATE_SUB(NOW(), INTERVAL 30 DAY)
),
daily_active AS (
    SELECT
        DATE(login_time) AS active_date,
        player_id
    FROM login_logs
    WHERE login_time >= DATE_SUB(NOW(), INTERVAL 30 DAY)
)
SELECT
    i.install_date,
    COUNT(DISTINCT i.player_id) AS new_users,
    -- Day 1 留存
    COUNT(DISTINCT CASE
        WHEN d.active_date = DATE_ADD(i.install_date, INTERVAL 1 DAY)
        THEN d.player_id END) AS day1_users,
    -- Day 7 留存
    COUNT(DISTINCT CASE
        WHEN d.active_date = DATE_ADD(i.install_date, INTERVAL 7 DAY)
        THEN d.player_id END) AS day7_users,
    -- Day 30 留存
    COUNT(DISTINCT CASE
        WHEN d.active_date = DATE_ADD(i.install_date, INTERVAL 30 DAY)
        THEN d.player_id END) AS day30_users,
    -- 留存率
    ROUND(COUNT(DISTINCT CASE
        WHEN d.active_date = DATE_ADD(i.install_date, INTERVAL 1 DAY)
        THEN d.player_id END) / COUNT(DISTINCT i.player_id) * 100, 2) AS retention_day1,
    ROUND(COUNT(DISTINCT CASE
        WHEN d.active_date = DATE_ADD(i.install_date, INTERVAL 7 DAY)
        THEN d.player_id END) / COUNT(DISTINCT i.player_id) * 100, 2) AS retention_day7
FROM install_cohort i
LEFT JOIN daily_active d ON i.player_id = d.player_id
GROUP BY i.install_date
ORDER BY i.install_date DESC;
```

### 流失预测模型

```python
# 简单的流失预测（使用逻辑回归）
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import classification_report, roc_auc_score

# 加载玩家特征数据
features = pd.read_csv('player_features.csv')
# 特征说明:
#   - days_since_last_login: 上次登录至今的天数
#   - avg_session_min: 平均游戏时长（分钟）
#   - total_recharge: 总充值金额
#   - max_level: 最大等级
#   - friend_count: 好友数量
#   - guild_member: 是否公会成员 (0/1)
#   - quest_completion_rate: 任务完成率 (0~1)
#   - pvp_battle_count: PVP 战斗次数（近 7 天）
#   - churned: 是否流失 (1=流失, 0=留存) <-- 标签

X = features.drop('churned', axis=1)
y = features['churned']

# 分割训练集和测试集
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42
)

# 训练逻辑回归模型
model = LogisticRegression(class_weight='balanced')
model.fit(X_train, y_train)

# 预测
y_pred = model.predict(X_test)
y_prob = model.predict_proba(X_test)[:, 1]

print(classification_report(y_test, y_pred))
print(f"AUC: {roc_auc_score(y_test, y_prob):.3f}")

# 特征重要性
feature_importance = pd.DataFrame({
    'feature': X.columns,
    'importance': abs(model.coef_[0])
}).sort_values('importance', ascending=False)

print("\nTop 10 流失预测特征:")
print(feature_importance.head(10))

# 预测结果: 对每个玩家输出流失概率
# 概率 > 0.7 的玩家自动标记为"高危"，推送给运营做召回活动
```

---

## 四、同期群分析 (Cohort Analysis)

```python
# 同期群分析：按首次充值月份分群，追踪后续每月充值
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns

# 加载数据
recharges = pd.read_csv('recharge_records.csv')
recharges['recharge_month'] = pd.to_datetime(
    recharges['recharge_time']).dt.to_period('M')
recharges['first_recharge_month'] = pd.to_datetime(
    recharges['first_recharge']).dt.to_period('M')

# 计算每个玩家每月的充值金额
cohort_data = recharges.groupby(
    ['first_recharge_month', 'recharge_month']
)['revenue'].sum().reset_index()

# 计算月份偏移（Month 0 = 首次充值的当月）
cohort_data['month_offset'] = (
    cohort_data['recharge_month'] - cohort_data['first_recharge_month']
).apply(lambda x: x.n)

# 计算每个同期群的初始用户数
initial_counts = recharges.groupby('first_recharge_month')['player_id'].nunique()

# 构建透视表
cohort_pivot = cohort_data.pivot_table(
    index='first_recharge_month',
    columns='month_offset',
    values='revenue',
    aggfunc='sum'
)

# 标准化为百分比（Month 0 = 100%）
cohort_pct = cohort_pivot.divide(
    cohort_pivot[0], axis=0
) * 100

# 热力图可视化
plt.figure(figsize=(12, 8))
sns.heatmap(cohort_pct, annot=True, fmt='.0f',
    cmap='YlOrRd', cbar_kws={'label': '留存率 (%)'})
plt.title('充值同期群分析 (Cohort Analysis)')
plt.xlabel('距离首次充值的月数')
plt.ylabel('首次充值月份')
plt.tight_layout()
plt.savefig('cohort_analysis.png')

# 解读:
# 纵向看: 同一批用户在各月份的留存率
# 横向看: 不同批用户在同一个月的留存率差异
# 对角线: 逐月衰减趋势
```

---

## 五、A/B 测试框架

```csharp
// 游戏功能 A/B 测试框架
public class ABTestService
{
    private readonly IDatabase _redis;

    // 分配实验分组
    public async Task<string> GetAssignment(long playerId, string experimentName)
    {
        string cacheKey = $"abtest:{experimentName}:{playerId}";

        // 1. 检查是否已分配过
        string cached = await _redis.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return cached;

        // 2. 获取实验配置
        var config = await GetExperimentConfig(experimentName);
        if (config == null || !config.IsActive)
            return "control"; // 默认控制组

        // 3. 根据 playerId 分配（保证同一个玩家始终同组）
        int hash = Math.Abs(HashCode.Combine(playerId, experimentName));
        int remainder = hash % 100;

        int cumulative = 0;
        foreach (var variant in config.Variants.OrderBy(v => v.TrafficPercent))
        {
            cumulative += variant.TrafficPercent;
            if (remainder < cumulative)
            {
                // 缓存分配结果（防刷）
                await _redis.StringSetAsync(cacheKey, variant.Name,
                    TimeSpan.FromDays(90));
                return variant.Name;
            }
        }

        return "control";
    }

    // 记录实验事件
    public async Task TrackEvent(long playerId, string experimentName,
        string variant, string eventName, Dictionary<string, object> properties = null)
    {
        var evt = new ExperimentEvent
        {
            PlayerId = playerId,
            ExperimentName = experimentName,
            Variant = variant,
            EventName = eventName,
            Properties = properties ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        };

        // 写入 Kafka（后续用 Spark/Flink 做分析）
        await _kafkaProducer.ProduceAsync("abtest-events",
            new Message<string, byte[]>
            {
                Key = $"{experimentName}:{playerId}",
                Value = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt))
            });
    }
}

// 实验配置
public class ExperimentConfig
{
    public string Name { get; set; }           // 实验名称
    public string Description { get; set; }    // 描述
    public bool IsActive { get; set; }         // 是否运行中
    public List<VariantConfig> Variants { get; set; } = new();
    public List<string> Metrics { get; set; } = new();  // 核心指标
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class VariantConfig
{
    public string Name { get; set; }           // control / variant_a / variant_b
    public int TrafficPercent { get; set; }    // 流量百分比
    public Dictionary<string, object> Params { get; set; } = new(); // 实验参数
}
```

---

## 六、实时 vs 批处理

```yaml
# 实时数据管道（秒级延迟）
# 用途:
#   - 在线监控仪表板
#   - 异常检测告警
#   - 实时运营活动效果
# 技术栈:
#   Kafka → Flink → Redis → WebSocket 推送

# 批处理数据管道（小时/天级延迟）
# 用途:
#   - DAU / 留存率 / ARPU 报表
#   - 同期群分析
#   - 机器学习特征计算
# 技术栈:
#   Spark / Hive → 数据仓库 → BI 报表

# 混合架构 Lambda Architecture:
#   速度层 (Speed Layer): 实时处理最新数据
#   批处理层 (Batch Layer): 全量数据处理
#   服务层 (Serving Layer): 合并结果对外查询
```

```sql
-- 批处理: 每日数据汇总表
CREATE TABLE daily_game_summary (
    report_date DATE NOT NULL,
    dau INT NOT NULL COMMENT '日活跃用户',
    wau INT COMMENT '周活跃',
    mau INT COMMENT '月活跃',
    new_users INT COMMENT '新增用户',
    new_players INT COMMENT '新增创角',
    revenue DECIMAL(12,2) COMMENT '总收入',
    paying_users INT COMMENT '付费人数',
    avg_online INT COMMENT '平均在线',
    peak_online INT COMMENT '峰值在线',
    total_battles INT COMMENT '总战斗场次',
    avg_session_min DECIMAL(5,1) COMMENT '平均在线时长(分钟)',
    crash_count INT COMMENT '崩溃次数',
    PRIMARY KEY (report_date)
);

-- 数据汇总存储过程
DELIMITER //
CREATE PROCEDURE sp_generate_daily_report(IN report_date DATE)
BEGIN
    INSERT INTO daily_game_summary
    SELECT
        report_date,
        COUNT(DISTINCT player_id) AS dau,
        NULL AS wau,
        NULL AS mau,
        COUNT(DISTINCT CASE WHEN is_new = 1 THEN player_id END) AS new_users,
        COUNT(DISTINCT CASE WHEN created_char = 1 THEN player_id END) AS new_players,
        SUM(revenue) AS revenue,
        COUNT(DISTINCT CASE WHEN revenue > 0 THEN player_id END) AS paying_users,
        AVG(online_count) AS avg_online,
        MAX(online_count) AS peak_online,
        SUM(battle_count) AS total_battles,
        AVG(session_min) AS avg_session_min,
        SUM(crash_count) AS crash_count
    FROM daily_events
    WHERE event_date = report_date
    ON DUPLICATE KEY UPDATE
        dau = VALUES(dau), revenue = VALUES(revenue);
END//
```

---

## 七、数据仓库设计：星型模型

```sql
-- 星型模型: 事实表 + 维度表

-- 维度表: 时间
CREATE TABLE dim_date (
    date_key INT PRIMARY KEY COMMENT 'YYYYMMDD',
    full_date DATE NOT NULL,
    year SMALLINT NOT NULL,
    month TINYINT NOT NULL,
    day TINYINT NOT NULL,
    day_of_week TINYINT NOT NULL,
    is_weekend BOOLEAN NOT NULL,
    is_holiday BOOLEAN DEFAULT FALSE,
    season TINYINT COMMENT '1=春 2=夏 3=秋 4=冬'
);

-- 维度表: 玩家
CREATE TABLE dim_player (
    player_key BIGINT AUTO_INCREMENT PRIMARY KEY,
    player_id BIGINT NOT NULL,
    install_date DATE NOT NULL,
    first_recharge_date DATE,
    channel VARCHAR(32) COMMENT '渠道(应用宝/华为/AppStore)',
    device_type VARCHAR(32) COMMENT '设备类型',
    os_version VARCHAR(16) COMMENT '系统版本',
    country VARCHAR(8) COMMENT '国家',
    first_level TINYINT,
    is_paid_user BOOLEAN DEFAULT FALSE,
    lifetime_value DECIMAL(12,2) DEFAULT 0,
    effective_date DATE NOT NULL,
    expiry_date DATE DEFAULT '9999-12-31',
    is_current BOOLEAN DEFAULT TRUE
);

-- 维度表: 服务器
CREATE TABLE dim_server (
    server_key INT PRIMARY KEY AUTO_INCREMENT,
    server_id VARCHAR(32) NOT NULL,
    server_type VARCHAR(16) COMMENT 'scene/gate/match/chat',
    region VARCHAR(16) COMMENT '区域',
    open_date DATE COMMENT '开服日期',
    is_merged BOOLEAN DEFAULT FALSE COMMENT '是否合服'
);

-- 事实表: 每日玩家活动
CREATE TABLE fact_player_daily (
    date_key INT NOT NULL,
    player_key BIGINT NOT NULL,
    server_key INT NOT NULL,
    -- 指标
    login_count INT DEFAULT 0,
    session_min_total INT DEFAULT 0,
    battle_count INT DEFAULT 0,
    battle_win_count INT DEFAULT 0,
    pvp_count INT DEFAULT 0,
    quest_completed INT DEFAULT 0,
    exp_gained BIGINT DEFAULT 0,
    gold_earned BIGINT DEFAULT 0,
    gold_spent BIGINT DEFAULT 0,
    item_used INT DEFAULT 0,
    chat_messages INT DEFAULT 0,
    friend_added INT DEFAULT 0,
    is_paid BOOLEAN DEFAULT FALSE,
    revenue DECIMAL(10,2) DEFAULT 0,
    crash_count INT DEFAULT 0,
    PRIMARY KEY (date_key, player_key, server_key),
    FOREIGN KEY (date_key) REFERENCES dim_date(date_key),
    FOREIGN KEY (player_key) REFERENCES dim_player(player_key),
    FOREIGN KEY (server_key) REFERENCES dim_server(server_key)
);

-- 查询示例: 按渠道和日期聚合 DAU 和收入
SELECT
    d.full_date,
    p.channel,
    COUNT(DISTINCT f.player_key) AS dau,
    SUM(f.revenue) AS revenue,
    SUM(f.battle_count) AS total_battles
FROM fact_player_daily f
JOIN dim_date d ON f.date_key = d.date_key
JOIN dim_player p ON f.player_key = p.player_key
WHERE d.full_date >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)
  AND p.is_current = TRUE
GROUP BY d.full_date, p.channel
ORDER BY d.full_date, p.channel;
```

---

## 八、GDPR 与数据匿名化

```csharp
// 玩家数据匿名化
public class DataAnonymizer
{
    // 敏感字段列表
    private static readonly string[] SensitiveFields =
    {
        "email", "phone", "idfa", "imei", "oaid", "ip_address",
        "real_name", "id_number", "device_id"
    };

    // 对查询结果匿名化
    public Dictionary<string, object> Anonymize(Dictionary<string, object> data)
    {
        var result = new Dictionary<string, object>(data);

        foreach (var field in SensitiveFields)
        {
            if (result.ContainsKey(field))
            {
                result[field] = MaskValue(field, result[field]?.ToString());
            }
        }

        return result;
    }

    private string MaskValue(string field, string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        return field switch
        {
            "email" => MaskEmail(value),
            "phone" => MaskPhone(value),
            "ip_address" => MaskIp(value),
            "idfa" or "imei" or "oaid" => "***已匿名***",
            "real_name" => value[0] + "**",
            "id_number" => value[..3] + "********" + value[^4..],
            _ => value
        };
    }

    private string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        return $"{parts[0][0]}***@{parts[1]}";
    }

    private string MaskPhone(string phone)
    {
        if (phone.Length < 7) return phone;
        return phone[..3] + "****" + phone[^4..];
    }

    private string MaskIp(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length == 4)
            return $"{parts[0]}.{parts[1]}.***.***";
        return ip;
    }

    // GDPR 数据删除（被遗忘权）
    public async Task ForgetPlayer(long playerId)
    {
        // 1. 删除个人身份字段（保留游戏记录）
        await _db.ExecuteAsync(
            @"UPDATE players SET
                email = NULL,
                phone = NULL,
                device_id = 'DELETED',
                idfa = 'DELETED',
                ip_address = NULL
              WHERE player_id = @PlayerId",
            new { PlayerId = playerId });

        // 2. 记录删除操作
        await _auditLogger.LogAudit("GDPR", "FORGET", playerId, "玩家请求数据删除");
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| ELK 调优 | 按天分索引、异步 translog、减少刷新频率 |
| Kibana 面板 | 核心 KPI 行 + 分析面板 + 异常面板三层 |
| 留存率 | Day1/Day7/Day30 新增用户留存比例 |
| 流失预测 | 用逻辑回归基于特征预测玩家流失概率 |
| 同期群分析 | 按首次充值月份分群追踪后续充值表现 |
| A/B 测试 | 按 playerId 一致性分组，Kafka 采集事件 |
| Lambda 架构 | 实时 + 批处理混合的数据管道 |
| 星型模型 | 事实表 + 维度表的数据仓库设计 |
| GDPR | 匿名化敏感字段 + 支持被遗忘权 |
