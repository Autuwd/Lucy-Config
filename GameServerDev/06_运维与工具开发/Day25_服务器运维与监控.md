# Day 25：服务器运维与监控

## 一、进程管理

### Supervisor

```ini
# /etc/supervisor/conf.d/game-server.conf
[program:game-server]
command=/opt/game-server/bin/server --config /opt/game-server/config/server.json
directory=/opt/game-server
user=game
autostart=true
autorestart=true
startretries=3
stopwaitsecs=10
stopasgroup=true
killasgroup=true

stdout_logfile=/var/log/game-server/stdout.log
stdout_logfile_maxbytes=50MB
stdout_logfile_backups=5
stderr_logfile=/var/log/game-server/stderr.log
stderr_logfile_maxbytes=50MB
stderr_logfile_backups=5

environment=ASPNETCORE_ENVIRONMENT=Production
```

```bash
# Supervisor 命令
supervisorctl status                    # 查看所有进程状态
supervisorctl start game-server         # 启动
supervisorctl stop game-server          # 停止
supervisorctl restart game-server       # 重启
supervisorctl reread                    # 重新加载配置
supervisorctl update                    # 更新配置并重启修改的服务
```

### Systemd (如果不用 Supervisor)

```ini
# /etc/systemd/system/game-server.service
[Unit]
Description=Game Server
After=network.target

[Service]
Type=simple
User=game
WorkingDirectory=/opt/game-server
ExecStart=/opt/game-server/bin/server
Restart=on-failure
RestartSec=3
LimitNOFILE=100000

[Install]
WantedBy=multi-user.target
```

---

## 二、监控指标

### 关键指标

```csharp
public class ServerMetrics
{
    // 业务指标
    public long CurrentConnections;
    public long OnlinePlayers;
    public long TotalPacketsPerSecond;
    public double AvgProcessTimeMs;

    // 系统指标
    public double CpuUsage;
    public long MemoryUsedMB;
    public long MemoryTotalMB;
    public long GcTotalMemory;
    public int ThreadPoolWorkerThreads;
    public int ThreadPoolCompletionPortThreads;

    // 业务指标
    public long DbQueryCount;
    public double DbAvgQueryTimeMs;
    public long RedisCommandCount;
    public double RedisAvgCommandTimeMs;
}
```

### 指标收集

```csharp
public class MetricsCollector
{
    private readonly PerformanceCounter _cpuCounter;
    private long _lastPackets;
    private DateTime _lastMeasureTime;

    public MetricsCollector()
    {
        if (OperatingSystem.IsWindows())
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }
    }

    public async Task<ServerMetrics> CollectAsync()
    {
        var metrics = new ServerMetrics();

        // 业务指标
        metrics.CurrentConnections = Interlocked.Read(ref _currentConnections);
        metrics.OnlinePlayers = Interlocked.Read(ref _onlinePlayers);

        // PPS (Packet Per Second)
        var now = DateTime.UtcNow;
        long currentPackets = Interlocked.Read(ref _totalPackets);
        double elapsed = (now - _lastMeasureTime).TotalSeconds;
        if (elapsed > 0)
        {
            metrics.TotalPacketsPerSecond = (long)((currentPackets - _lastPackets) / elapsed);
        }
        _lastPackets = currentPackets;
        _lastMeasureTime = now;

        // 系统指标
        if (OperatingSystem.IsWindows())
        {
            metrics.CpuUsage = _cpuCounter.NextValue();
        }
        else
        {
            metrics.CpuUsage = await GetLinuxCpuUsage();
        }

        metrics.MemoryUsedMB = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
        metrics.GcTotalMemory = GC.GetTotalMemory(false) / 1024 / 1024;

        metrics.ThreadPoolWorkerThreads = ThreadPool.ThreadCount;
        ThreadPool.GetAvailableThreads(out int worker, out int completion);
        metrics.ThreadPoolAvailableWorkerThreads = worker;
        metrics.ThreadPoolAvailableCompletionPortThreads = completion;

        return metrics;
    }

    private async Task<double> GetLinuxCpuUsage()
    {
        try
        {
            // 读取 /proc/stat
            string stat = await File.ReadAllTextAsync("/proc/stat");
            var lines = stat.Split('\n');
            var cpuLine = lines.First(l => l.StartsWith("cpu "));
            var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            long user = long.Parse(parts[1]);
            long nice = long.Parse(parts[2]);
            long system = long.Parse(parts[3]);
            long idle = long.Parse(parts[4]);

            long total = user + nice + system + idle;
            long deltaTotal = total - _lastCpuTotal;
            long deltaIdle = idle - _lastCpuIdle;

            _lastCpuTotal = total;
            _lastCpuIdle = idle;

            return deltaTotal > 0 ? (1.0 - (double)deltaIdle / deltaTotal) * 100 : 0;
        }
        catch
        {
            return 0;
        }
    }

    private long _lastCpuTotal;
    private long _lastCpuIdle;
}
```

---

## 三、Prometheus + Grafana 监控

### Prometheus 指标暴露

```csharp
// dotnet add package prometheus-net.AspNetCore

using Prometheus;

public class MetricsExporter
{
    private static readonly Gauge CurrentConnections = Metrics
        .CreateGauge("game_server_connections", "当前连接数");

    private static readonly Gauge OnlinePlayers = Metrics
        .CreateGauge("game_server_online_players", "在线玩家数");

    private static readonly Counter TotalPackets = Metrics
        .CreateCounter("game_server_packets_total", "总消息处理数",
            new CounterConfiguration { LabelNames = new[] { "type" } });

    private static readonly Histogram PacketProcessTime = Metrics
        .CreateHistogram("game_server_packet_process_ms", "消息处理耗时 ms",
            new HistogramConfiguration
            {
                LabelNames = new[] { "msg_id" },
                Buckets = new[] { 1.0, 5.0, 10.0, 25.0, 50.0, 100.0, 500.0 }
            });

    public static void RecordConnection(bool connected)
    {
        if (connected)
            CurrentConnections.Inc();
        else
            CurrentConnections.Dec();
    }

    public static void RecordPacket(string type)
    {
        TotalPackets.WithLabels(type).Inc();
    }

    public static void RecordProcessTime(string msgId, long ms)
    {
        PacketProcessTime.WithLabels(msgId).Observe(ms);
    }
}

// Program.cs 注册
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 暴露 /metrics 端点给 Prometheus 抓取
app.MapMetrics();
app.Run();
```

### prometheus.yml

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'game-server'
    static_configs:
      - targets:
        - '192.168.1.10:8888'
        - '192.168.1.11:8888'
        - '192.168.1.12:8888'
    metrics_path: /metrics

  - job_name: 'node'
    static_configs:
      - targets:
        - '192.168.1.10:9100'  # node_exporter
        - '192.168.1.11:9100'
        - '192.168.1.12:9100'

  - job_name: 'mysql'
    static_configs:
      - targets:
        - '192.168.1.10:9104'  # mysqld_exporter
```

### Grafana 面板设置

```
建议的监控面板：
  服务器总览：
    - 在线玩家数（折线图）
    - 总连接数
    - PPS (Packet Per Second)
    - CPU 使用率
    - 内存使用率
  
  性能：
    - 消息处理耗时 P50/P95/P99
    - 消息吞吐量
    - GC 频率
    - 线程池状态
  
  DB：
    - MySQL QPS
    - 慢查询数
    - Redis 命令数
    - 缓存命中率
  
  告警：
    - 在线玩家数 = 0 → 服务器挂了
    - CPU > 80% → 需要扩容
    - P99 > 500ms → 有性能问题
    - 连接数突降 → 网络异常
```

---

## 四、告警规则

```yaml
# Prometheus 告警规则
groups:
  - name: game-server-alerts
    rules:
      - alert: ServerDown
        expr: up{job="game-server"} == 0
        for: 1m
        annotations:
          summary: "游戏服务器 {{ $labels.instance }} 已下线"

      - alert: HighCPU
        expr: game_server_cpu > 80
        for: 5m
        annotations:
          summary: "服务器 {{ $labels.instance }} CPU 使用率 {{ $value }}%"

      - alert: HighMemory
        expr: (game_server_memory_used / game_server_memory_total) > 0.9
        for: 5m
        annotations:
          summary: "服务器 {{ $labels.instance }} 内存使用率 {{ $value | humanizePercentage }}"

      - alert: HighPacketLatency
        expr: histogram_quantile(0.99, game_server_packet_process_ms_bucket) > 500
        for: 5m
        annotations:
          summary: "服务器 {{ $labels.instance }} 消息处理 P99 > 500ms"

      - alert: SuddenConnectionDrop
        expr: game_server_connections < 0.5 * game_server_connections offset 5m
        for: 1m
        annotations:
          summary: "服务器 {{ $labels.instance }} 连接数骤降！"
```

---

## 五、灰度发布

### 灰度策略

```csharp
public class GrayRelease
{
    private readonly IDatabase _redis;

    // 判断玩家是否在灰度中
    public async Task<bool> IsInGrayRelease(long playerId)
    {
        // 按玩家 ID 百分比灰度
        // 配置灰度比例 10%
        int grayPercent = await GetGrayPercent("new_battle_system");

        // 取玩家 ID 后 2 位
        int hash = (int)(playerId % 100);
        return hash < grayPercent;
    }

    // 按白名单灰度
    public async Task<bool> IsInWhitelist(long playerId)
    {
        return await _redis.SetContainsAsync("gray:whitelist", playerId.ToString());
    }

    // 灰度版本控制
    public async Task<string> GetPlayerVersion(long playerId)
    {
        if (await IsInGrayRelease(playerId))
            return "v2.0.0-gray";     // 灰度版本
        return "v1.2.0-stable";       // 稳定版本
    }
}
```

### 蓝绿部署

```yaml
# docker-compose.blue.yml
services:
  game-server-blue:
    image: game-server:1.2.0  # 稳定版
    ports:
      - "8881:8888"

# docker-compose.green.yml  
services:
  game-server-green:
    image: game-server:1.3.0  # 新版
    ports:
      - "8882:8888"

# 网关根据灰度标记路由到 blue 或 green
```

---

## 六、练习

1. **进程管理**：配置 Supervisor 管理游戏服务器进程
2. **指标收集**：实现 PPS (Packet Per Second) 和平均处理耗时
3. **Prometheus 接入**：用 prometheus-net 暴露 /metrics 端点
4. **Grafana 面板**：创建一个显示在线玩家、CPU、内存的面板
5. **灰度发布**：实现按玩家 ID 百分比的灰度路由

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Supervisor | 进程管理：自启、崩溃重启、日志管理 |
| PPS | Packet Per Second，服务器吞吐量核心指标 |
| Prometheus | 指标采集 + 报警，云原生监控标准 |
| Grafana | 可视化面板，展示指标变化趋势 |
| P50/P95/P99 | 百分位延迟，衡量性能的关键指标 |
| 灰度发布 | 部分玩家先体验新版本，发现问题回滚 |
