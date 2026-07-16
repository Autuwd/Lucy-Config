# Day 25：服务器运维与监控 — 进阶深入

## 一、Prometheus 指标深度解析

### 四种指标类型

```csharp
// 1. Counter（计数器）— 只增不减
//    用途: 请求总数、错误总数、玩家登录总数
//    注意: 不要用 Counter 测量会减少的值
public static readonly Counter TotalRequests = Metrics
    .CreateCounter("game_server_requests_total", "总请求数",
        new CounterConfiguration
        {
            LabelNames = new[] { "server_id", "region", "status" }
        });

// 2. Gauge（仪表盘）— 可增可减
//    用途: 在线人数、连接数、内存使用率、队列深度
public static readonly Gauge OnlinePlayers = Metrics
    .CreateGauge("game_server_online_players", "当前在线玩家数",
        new GaugeConfiguration
        {
            LabelNames = new[] { "server_id", "scene_type" }
        });

// 3. Histogram（直方图）— 分位数统计
//    用途: 请求延迟、处理耗时
//    注意: 选择合理的 bucket 范围
public static readonly Histogram RequestDuration = Metrics
    .CreateHistogram("game_server_request_duration_ms", "请求处理耗时",
        new HistogramConfiguration
        {
            LabelNames = new[] { "api_name", "server_id" },
            // Bucket 设计（毫秒）:
            // 覆盖 P50(10ms) P90(50ms) P95(100ms) P99(500ms)
            Buckets = new[] { 1.0, 5.0, 10.0, 25.0, 50.0, 100.0, 250.0, 500.0, 1000.0, 5000.0 }
        });

// 4. Summary（摘要）— 预计算的分位数
//    用途: 类似 Histogram 但直接在客户端计算分位数
//    注意: Summary 的分位数是近似值，不能聚合
public static readonly Summary PacketSize = Metrics
    .CreateSummary("game_server_packet_size_bytes", "消息包大小",
        new SummaryConfiguration
        {
            LabelNames = new[] { "msg_type" },
            Objectives = new[]
            {
                new QuantileEpsilonPair(0.5, 0.05),   // P50 ±5%
                new QuantileEpsilonPair(0.9, 0.01),   // P90 ±1%
                new QuantileEpsilonPair(0.99, 0.001)  // P99 ±0.1%
            }
        });
```

### 游戏专用指标

```csharp
public class GameMetricsRegistry
{
    // 在线相关
    private static readonly Gauge OnlinePlayers =
        Metrics.CreateGauge("game_online_players", "在线玩家",
            new GaugeConfiguration { LabelNames = new[] { "server_id" } });

    // 匹配相关
    private static readonly Counter MatchStarted =
        Metrics.CreateCounter("game_match_started_total", "匹配开始");
    private static readonly Gauge MatchQueueLength =
        Metrics.CreateGauge("game_match_queue_length", "匹配队列长度");
    private static readonly Histogram MatchTime =
        Metrics.CreateHistogram("game_match_time_seconds", "匹配耗时",
            new HistogramConfiguration
            {
                Buckets = new[] { 1.0, 5.0, 10.0, 30.0, 60.0, 120.0, 300.0 }
            });

    // 战斗相关
    private static readonly Counter BattleCompleted =
        Metrics.CreateCounter("game_battle_completed_total", "战斗完成数",
            new CounterConfiguration { LabelNames = new[] { "mode", "result" } });
    private static readonly Histogram BattleDuration =
        Metrics.CreateHistogram("game_battle_duration_seconds", "战斗时长",
            new HistogramConfiguration { Buckets = new[] { 30, 60, 120, 300, 600, 1800 } });

    // 经济相关
    private static readonly Counter CurrencyEarned =
        Metrics.CreateCounter("game_currency_earned_total", "货币产出",
            new CounterConfiguration { LabelNames = new[] { "currency_type", "source" } });
    private static readonly Counter CurrencySpent =
        Metrics.CreateCounter("game_currency_spent_total", "货币消耗",
            new CounterConfiguration { LabelNames = new[] { "currency_type", "source" } });

    // 错误率
    private static readonly Counter ErrorCount =
        Metrics.CreateCounter("game_error_total", "错误计数",
            new CounterConfiguration { LabelNames = new[] { "server_id", "error_type" } });
    private static readonly Counter CrashCount =
        Metrics.CreateCounter("game_crash_total", "崩溃计数",
            new CounterConfiguration { LabelNames = new[] { "server_id" } });

    // 业务健康
    private static readonly Gauge DbConnectionPoolUsage =
        Metrics.CreateGauge("game_db_connection_pool_usage", "DB 连接池使用率");
    private static readonly Gauge EventQueueDepth =
        Metrics.CreateGauge("game_event_queue_depth", "事件队列深度",
            new GaugeConfiguration { LabelNames = new[] { "queue_name" } });

    public static void RecordMatch(string serverId, TimeSpan matchTime)
    {
        MatchStarted.Inc();
        MatchTime.Observe(matchTime.TotalSeconds);
    }

    public static void RecordBattleEnd(string mode, string result, TimeSpan duration)
    {
        BattleCompleted.WithLabels(mode, result).Inc();
        BattleDuration.Observe(duration.TotalSeconds);
    }

    public static void RecordCurrency(string type, string source, long amount, bool isEarned)
    {
        if (isEarned)
            CurrencyEarned.WithLabels(type, source).Inc(amount);
        else
            CurrencySpent.WithLabels(type, source).Inc(amount);
    }
}
```

---

## 二、Grafana 面板设计

```
Game Ops Dashboard 设计原则：
  1. 从上到下：宏观 → 微观
  2. 左到右：时序 → 分布 → 实时值
  3. 颜色编码：绿（正常）黄（警告）红（危险）

推荐面板布局（三行）：
```

```jsonc
// Grafana Dashboard JSON Model（关键部分）
{
  "title": "Game Server Ops Overview",
  "panels": [
    // Row 1: 宏观健康
    {
      "title": "在线玩家 & 匹配",
      "panels": [
        { "title": "在线玩家（按服务器）", "type": "graph", "targets": [/* ... */] },
        { "title": "匹配队列深度", "type": "graph" },
        { "title": "匹配耗时 P50/P95/P99", "type": "graph" },
        { "title": "战斗完成数（按模式）", "type": "bargauge" }
      ]
    },
    // Row 2: 性能
    {
      "title": "系统性能",
      "panels": [
        { "title": "CPU/内存/网络", "type": "graph" },
        { "title": "请求延迟 P50/P95/P99", "type": "graph" },
        { "title": "GC 频率 & 暂停时间", "type": "graph" },
        { "title": "线程池状态", "type": "graph" }
      ]
    },
    // Row 3: 业务
    {
      "title": "经济 & 错误",
      "panels": [
        { "title": "货币产出 vs 消耗", "type": "graph" },
        { "title": "错误率（按类型）", "type": "graph" },
        { "title": "崩溃率", "type": "singlestat" },
        { "title": "异常玩家数", "type": "singlestat" }
      ]
    }
  ]
}
```

### PromQL 查询案例

```promql
# 在线玩家数（按服务器分组）
sum by (server_id) (game_online_players)

# 匹配耗时 P99（过去 5 分钟）
histogram_quantile(0.99,
  sum by (le) (rate(game_match_time_seconds_bucket[5m]))
)

# 货币净产出（产出 - 消耗）
sum(rate(game_currency_earned_total[1m])) -
sum(rate(game_currency_spent_total[1m]))

# 崩溃率（每小时每服务器）
rate(game_crash_total[1h])

# 错误率占比
sum(rate(game_error_total[5m])) /
sum(rate(game_server_requests_total[5m])) * 100

# P95 请求延迟
histogram_quantile(0.95,
  sum by (le) (rate(game_server_request_duration_ms_bucket[5m]))
)

# 连接数变化率（检测突然掉线）
deriv(game_server_connections[1m])
```

---

## 三、告警哲学：防疲劳

```yaml
# 告警级别定义

# P0 (Critical) — 立即处理，5 分钟响应
#   游戏服务器完全不可用
#   玩家无法登录
#   付费功能故障
#   数据丢失风险

# P1 (Major) — 紧急，30 分钟响应  
#   部分服务器不可用
#   匹配功能故障
#   充值延迟超过 5 分钟
#   P99 延迟 > 5 秒

# P2 (Warning) — 重要，2 小时响应
#   在线人数异常波动
#   错误率上升 > 3x 基线
#   CPU > 85% 持续 10 分钟
#   队列积压 > 阈值

# P3 (Info) — 通知，次日处理
#   配置变更
#   服务器重启
#   版本发布

# 告警疲劳的解决方案
# 1. 聚合告警：同类告警合并（1 次发 1 条，不刷屏）
# 2. 抑制规则：已知故障不重复告警
# 3. 静默期：同一服务 30 分钟内不重复触发同级别告警
# 4. 升级策略：P2 持续 30 分钟 → 自动升级为 P1
# 5. 值班表：工作时间 → 机器人，非工作时间 → 电话
```

```yaml
# Prometheus 告警规则（防疲劳优化版）
groups:
  - name: game-server-alerts
    rules:
      # 在线人数消失（考虑可能是正常维护）
      - alert: NoOnlinePlayers
        expr: game_online_players == 0
        for: 5m              # 持续 5 分钟才告警（防止短时中断误报）
        annotations:
          summary: "所有服务器在线玩家数为 0（持续 5 分钟）"

      # 错误率比率告警（比固定阈值更智能）
      - alert: HighErrorRate
        expr: |
          (
            rate(game_error_total[5m])
            /
            rate(game_server_requests_total[5m])
          ) > 0.05
        for: 3m
        labels:
          severity: warning
        annotations:
          summary: "错误率 { { $value | humanizePercentage } }"

      # 慢调用告警（相对基线）
      - alert: SlowRequests
        expr: |
          histogram_quantile(0.99,
            rate(game_server_request_duration_ms_bucket[5m])
          ) > 1000
        for: 5m
        labels:
          severity: major

      # 连接数骤降（检测网络故障）
      - alert: ConnectionDrop
        expr: |
          game_server_connections
          /
          avg(game_server_connections offset 10m)
          < 0.5
        for: 1m
        labels:
          severity: critical

      # 匹配队列暴涨（可能是匹配服务挂了）
      - alert: MatchQueueGrowing
        expr: |
          rate(game_match_queue_length[5m]) > 0
          and
          rate(game_match_completed_total[5m]) < 1
        for: 2m
        labels:
          severity: major
```

---

## 四、结构化日志到 stdout

```csharp
// 统一结构化日志格式（JSON 输出到 stdout）
public static class StructuredLogging
{
    // 使用 Serilog 输出结构化日志到 stdout
    // dotnet add package Serilog.Sinks.Console
    // dotnet add package Serilog.Formatting.Elasticsearch

    public static ILogger CreateLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("service", "game-server")
            .Enrich.WithProperty("server_id", Environment.GetEnvironmentVariable("SERVER_ID"))
            .Enrich.WithProperty("environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            .WriteTo.Console(new ElasticsearchJsonFormatter())
            .CreateLogger();
    }
}

// 日志输出示例（每行 JSON）:
// {
//   "@timestamp": "2026-07-15T22:00:01.234Z",
//   "level": "Warning",
//   "messageTemplate": "玩家 {PlayerId} 操作异常",
//   "fields": {
//     "PlayerId": 10001,
//     "action": "speed_hack",
//     "service": "game-server",
//     "server_id": "gs-01",
//     "environment": "production"
//   }
// }

// 这种格式可以直接被：
//   - Filebeat 读取并转发到 Elasticsearch
//   - Logstash 直接解析 JSON
//   - Fluentd 采集到各种后端
```

---

## 五、配置管理：功能开关

```csharp
// 集中式配置管理（基于 etcd/Consul）
public class DynamicConfig
{
    private readonly IConfiguration _configuration;
    private readonly IProducer<string, byte[]> _producer;
    private readonly ConcurrentDictionary<string, object> _localCache = new();

    // 功能开关（支持运行时修改，无需重启）
    public bool IsFeatureEnabled(string featureName)
    {
        // 1. 检查本地缓存
        if (_localCache.TryGetValue($"feature:{featureName}", out var cached))
            return (bool)cached;

        // 2. 从配置中心读取
        string configKey = $"features/{featureName}";
        string value = _configuration[configKey];

        bool enabled = bool.TryParse(value, out var result) && result;

        // 3. 缓存 60 秒（减少对配置中心的请求）
        _localCache.TryAdd($"feature:{featureName}", enabled);
        _ = DelayedInvalidate($"feature:{featureName}", TimeSpan.FromSeconds(60));

        return enabled;
    }

    // A/B 测试配置
    public string GetExperimentVariant(long playerId, string experimentName)
    {
        string configKey = $"experiments/{experimentName}";

        // 获取实验配置
        var config = _configuration.GetSection(configKey);
        var variants = config.GetSection("variants").Get<Dictionary<string, int>>();

        // 根据 playerId 分配分组（保证一致性）
        int hash = Math.Abs(HashCode.Combine(playerId, experimentName.GetHashCode())) % 100;
        int cumulative = 0;

        foreach (var variant in variants.OrderBy(v => v.Value))
        {
            cumulative += variant.Value;
            if (hash < cumulative)
                return variant.Key;
        }

        return "control"; // 默认控制组
    }

    // 运行时修改配置（通知所有服务器热更新）
    public async Task SetConfig(string key, string value)
    {
        // 写配置中心
        await _producer.ProduceAsync("config-changes",
            new Message<string, byte[]>
            {
                Key = key,
                Value = Encoding.UTF8.GetBytes(value)
            });

        // 通知所有服务器刷新
        _localCache.TryRemove(key, out _);
    }
}
```

---

## 六、容量规划与压测

```python
# k6 脚本：游戏服务器压测
# 安装: winget install k6 或 choco install k6

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';

// 自定义指标
const loginDuration = new Trend('login_duration');
const battleDuration = new Trend('battle_duration');
const errorRate = new Counter('game_errors');

// 配置
export const options = {
    stages: [
        { duration: '2m', target: 100 },   // 爬坡到 100 虚拟用户
        { duration: '5m', target: 500 },   // 爬坡到 500
        { duration: '5m', target: 1000 },  // 峰值 1000
        { duration: '2m', target: 0 },     // 回落
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'],  // 95% 的请求 < 500ms
        login_duration: ['p(95)<2000'],    // 登录耗时 < 2s
        game_errors: ['count<100'],        // 错误 < 100 次
    },
};

// 模拟玩家登录
function simulateLogin(playerId) {
    let start = Date.now();

    let res = http.post('http://game-server:8888/api/login', JSON.stringify({
        playerId: playerId,
        token: `test_token_${playerId}`,
    }), { headers: { 'Content-Type': 'application/json' } });

    loginDuration.add(Date.now() - start);

    check(res, {
        '登录成功': (r) => r.status === 200,
        '返回玩家数据': (r) => r.json('playerId') !== undefined,
    });

    if (res.status !== 200) {
        errorRate.add(1);
    }
}

// 主循环
export default function () {
    let playerId = Math.floor(Math.random() * 1000000);
    simulateLogin(playerId);
    sleep(Math.random() * 3 + 1); // 模拟思考时间
}
```

```yaml
# 容量规划指南（基于压测结果）

# 单台游戏服务器参考容量:
#   场景服务器:    2000 人/台 (MMO)
#   战斗服务器:    100 场/台 (实时对战)
#   网关服务器:    5000 连接/台
#   DB 服务器:    取决于查询模式，一般 2000 QPS

# 扩容触发条件:
#   - CPU > 80% 持续 10 分钟
#   - P99 延迟 > 500ms 持续 5 分钟
#   - 玩家排队 > 100 人
#   - 内存使用 > 85%

# 压测步骤:
#   1. 基准测试: 50 并发 → 获取 P50/P95/P99
#   2. 负载测试: 逐步增加到目标并发
#   3. 压力测试: 超过目标 2x → 找拐点
#   4. 稳定性测试: 目标负载持续 24 小时
```

---

## 七、部署策略深入

```yaml
# 游戏服务器部署策略对比

# 滚动更新 (Rolling Update)
#   流程: 一批一批替换 Pod（每批 10%）
#   优点: 资源利用率高
#   缺点: 新旧版本共存，需保证协议兼容
#   游戏场景: 无状态服务（网关、匹配）

# 蓝绿部署 (Blue-Green)
#   流程: 新版本全量部署 → 切换流量 → 旧版本保留
#   优点: 瞬间切换，回滚极快
#   缺点: 双倍资源
#   游戏场景: 重要版本更新，数据库 schema 变更

# 金丝雀部署 (Canary)
#   流程: 5% → 20% → 50% → 100%
#   优点: 逐步验证，影响面小
#   缺点: 需要精细的流量路由
#   游戏场景: 游戏逻辑更新，新功能上线
```

```yaml
# K8s 滚动更新配置
apiVersion: apps/v1
kind: Deployment
spec:
  replicas: 50
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 5         # 最多多启动 5 个新 Pod
      maxUnavailable: 2   # 最多允许 2 个 Pod 不可用
  template:
    spec:
      terminationGracePeriodSeconds: 30  # 优雅关闭等待 30 秒
      containers:
        - name: game-server
          lifecycle:
            preStop:
              exec:
                command: ["/bin/sh", "-c", "sleep 10"]
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Counter | 只增不减的累计指标（请求数、错误总数） |
| Gauge | 可增可减的实时值（在线人数、内存） |
| Histogram | 分桶延迟统计（P50/P95/P99） |
| Summary | 客户端预计算的分位数（不可聚合） |
| PromQL | Prometheus 查询语言，聚合指标数据 |
| 告警级别 | P0-P3 四级，防告警疲劳 |
| 结构化日志 | JSON 格式到 stdout，Filebeat 采集 |
| 功能开关 | 运行时热更新配置，无需重启 |
| 金丝雀部署 | 逐步放量的游戏服务器部署策略 |
| 容量规划 | 基于压测结果计算每台服务器的承载上限 |
