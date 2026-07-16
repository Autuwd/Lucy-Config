# Day 05：日志系统与配置管理

## 一、游戏服务器日志要求

### 为什么需要日志
- **问题排查**：崩溃挂起时还原现场
- **战斗审计**：玩家反馈"我明明打中了"时查看日志
- **运营监控**：DAU、在线人数趋势
- **安全审计**：检测异常操作（刷道具、外挂）

### 日志要求

| 要求 | 说明 | 重要性 |
|------|------|--------|
| 高性能 | 日志不能成为性能瓶颈 | ★★★★★ |
| 异步写入 | 日志 IO 不阻塞主逻辑 | ★★★★★ |
| 分级过滤 | Debug/Info/Warn/Error/Fatal | ★★★★★ |
| 文件滚动 | 按时间或大小自动切分 | ★★★★ |
| 结构化 | 机器可读的键值对格式 | ★★★★ |
| 低丢失 | 进程崩溃尽量保留 | ★★★ |

---

## 二、Serilog 结构化日志

### 安装与基础配置

```csharp
// dotnet add package Serilog
// dotnet add package Serilog.Sinks.Console
// dotnet add package Serilog.Sinks.File
// dotnet add package Serilog.Sinks.Async

using Serilog;
using Serilog.Formatting.Json;

// 配置
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.Async(a => a.File(
        path: "logs/gameserver-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
        rollOnFileSizeLimit: true
    ))
    .CreateLogger();

// 使用
Log.Debug("玩家 {PlayerId} 登录，角色名 {Name}", playerId, name);
Log.Warning("数据库连接失败，重试 {RetryCount}/3", retry);
Log.Error(ex, "处理玩家 {PlayerId} 的消息 {MsgId} 时异常", playerId, msgId);
Log.Fatal("服务器启动失败，端口 {Port} 被占用", port);
```

### 结构化日志 vs 字符串拼接

```csharp
// 错误：字符串拼接
Log.Warning($"玩家 {playerId} 等级 {level} 经验 {exp}");
// 问题：即使 LogLevel 过滤掉 Warning，字符串仍然拼接了

// 正确：模板 + 参数
Log.Warning("玩家 {PlayerId} 等级 {Level} 经验 {Exp}", playerId, level, exp);
// 优点：不记录时零开销，支持结构化查询
```

### 自定义 Sink

```csharp
// 将 Error 及以上日志写入单独的审计表
public class DatabaseSink : ILogEventSink
{
    private readonly string _connectionString;

    public DatabaseSink(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Error)
            return;

        // 异步写入数据库
        Task.Run(async () =>
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "INSERT INTO ErrorLogs (Timestamp, Level, Message, Exception) VALUES (@Ts, @Lv, @Msg, @Ex)",
                new
                {
                    Ts = logEvent.Timestamp,
                    Lv = logEvent.Level.ToString(),
                    Msg = logEvent.RenderMessage(),
                    Ex = logEvent.Exception?.ToString()
                });
        });
    }
}

// 注册
Log.Logger = new LoggerConfiguration()
    .WriteTo.Sink(new DatabaseSink("Server=..."))
    .CreateLogger();
```

---

## 三、日志级别设计

### 游戏服务器日志级别规范

```csharp
public enum LogLevel
{
    Trace = 0,    // 详细调试（消息内容、变量值）
    Debug = 1,    // 调试信息（函数进入/退出）
    Info = 2,     // 常规信息（玩家登录/登出、任务完成）
    Warn = 3,     // 警告（重试、降级、配置缺失使用默认值）
    Error = 4,    // 错误（操作失败、异常但可恢复）
    Fatal = 5     // 致命（进程即将退出）
}

class GameLogger
{
    // 运行时动态调整日志级别
    private static LogLevel _minLevel = LogLevel.Debug;

    public static void SetLevel(LogLevel level)
    {
        _minLevel = level;
    }

    public static bool IsEnabled(LogLevel level)
    {
        return level >= _minLevel;
    }

    // 使用条件判断避免不必要的计算
    public static void Debug(PlayerData player, string format, params object[] args)
    {
        if (!IsEnabled(LogLevel.Debug)) return;
        var message = string.Format(format, args);
        Write(LogLevel.Debug, $"[{player.PlayerId}] {message}");
    }
}
```

### 游戏关键事件日志

```csharp
// 战斗审计日志
Log.Information("[Battle] Player {Attacker} 攻击 {Target} 伤害 {Damage} 暴击 {IsCrit}",
    attackerId, targetId, damage, isCritical);

Log.Information("[Battle] Player {Player} 释放技能 {SkillId} 目标 {Target}",
    playerId, skillId, targetId);

// 经济日志
Log.Information("[Economy] Player {Player} 获得道具 {ItemId} x{Count} 来源 {Source}",
    playerId, itemId, count, source);

Log.Information("[Economy] Player {Player} 消耗 {CurrencyType} {Amount} 用途 {Reason}",
    playerId, currencyType, amount, reason);

// GM 操作
Log.Warning("[GM] Admin {AdminId} 对 Player {PlayerId} 执行 {Operation} 参数 {Params}",
    adminId, playerId, operation, paramString);
```

---

## 四、日志性能优化

### 异步写入

```csharp
// Serilog 异步 Sink
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a =>
    {
        a.File("logs/server-.log", rollingInterval: RollingInterval.Day);
        a.Console();
    }, bufferSize: 10000)
    .CreateLogger();

// 如果缓冲区满时的处理策略
// - Block：阻塞生产者（默认，保证消息不丢）
// - Drop：丢弃新消息
// - OverflowAction.Block: 阻塞直到写入完成
```

### 避免产生垃圾的日志

```csharp
// 坏：即使不记录也创建了对象
Log.Debug("玩家背包: {Items}", inventory.GetAllItems()); // GetAllItems() 创建了 List！

// 好：条件判断 + 延迟执行
if (Log.IsEnabled(LogEventLevel.Debug))
{
    Log.Debug("玩家背包: {Items}", inventory.DumpDebugString());
}

// 更好：使用 Delegate
Log.Logger.BindProperty("Items", inventory.DumpDebugString(), false, out var prop);
```

### 批量写入

```csharp
class BatchLogger
{
    private Channel<string> _channel = Channel.CreateBounded<string>(
        new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = true
        });

    private StreamWriter _writer;
    private int _batchSize = 100;
    private int _flushInterval = 1000; // 1s

    public BatchLogger(string filePath)
    {
        _writer = new StreamWriter(filePath, append: true);
        Task.Run(FlushLoop);
    }

    public void Log(string message)
    {
        // 非阻塞，满了就丢弃旧消息
        _channel.Writer.TryWrite(message);
    }

    private async Task FlushLoop()
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_flushInterval));

        while (await timer.WaitForNextTickAsync())
        {
            var batch = new List<string>(_batchSize);
            while (batch.Count < _batchSize && _channel.Reader.TryRead(out var msg))
                batch.Add(msg);

            if (batch.Count > 0)
            {
                await _writer.WriteAsync(string.Join("\n", batch) + "\n");
                await _writer.FlushAsync();
            }
        }
    }
}
```

---

## 五、崩溃日志与堆栈捕获

```csharp
// 全局异常捕获
class CrashGuard
{
    public static void Register()
    {
        // 托管异常
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = (Exception)args.ExceptionObject;
            Log.Fatal(ex, "未处理的异常，IsTerminating={IsTerminating}", args.IsTerminating);

            // 紧急写入（绕过异步队列）
            EmergencyWrite(ex.ToString());

            if (args.IsTerminating)
            {
                Thread.Sleep(1000); // 等日志写完
            }
        };

        // Task 未观察到的异常
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Error(args.Exception, "Task 未观察异常");
            args.SetObserved();
        };

        // 非托管异常 (SEH)
        System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += ctx =>
        {
            Log.Information("AssemblyLoadContext 卸载");
        };
    }

    private static void EmergencyWrite(string message)
    {
        try
        {
            // 直接同步写入，不使用异步框架
            string path = $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            File.WriteAllText(path, message);
        }
        catch { }
    }
}
```

---

## 六、配置管理

### JSON 配置

```csharp
// appsettings.json
{
  "Server": {
    "Name": "S1-测试服",
    "Port": 8888,
    "MaxConnections": 10000,
    "DB": {
      "Host": "127.0.0.1",
      "Port": 3306,
      "Database": "game_s1",
      "User": "root",
      "Password": "***"
    },
    "Redis": {
      "Connection": "127.0.0.1:6379"
    },
    "Log": {
      "Level": "Debug",
      "File": "logs/server-.log"
    }
  }
}

// 配置类
public class ServerConfig
{
    public string Name { get; set; } = "Default";
    public int Port { get; set; } = 8888;
    public int MaxConnections { get; set; } = 10000;
    public DbConfig DB { get; set; } = new();
    public RedisConfig Redis { get; set; } = new();
    public LogConfig Log { get; set; } = new();
}

public class DbConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "";
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
}

// 配置加载
public static ServerConfig LoadConfig(string path = "appsettings.json")
{
    if (!File.Exists(path))
    {
        Log.Warning("配置文件 {Path} 不存在，使用默认配置", path);
        return new ServerConfig();
    }

    string json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<ServerConfig>(json)
           ?? throw new InvalidDataException("配置解析失败");
}
```

### 配置热更新

```csharp
class ConfigManager
{
    private FileSystemWatcher _watcher;
    private ServerConfig _config;
    private readonly object _lock = new();

    public ServerConfig Config
    {
        get { lock (_lock) return _config; }
    }

    public ConfigManager(string path = "appsettings.json")
    {
        LoadConfig(path);

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(path))
        {
            Filter = Path.GetFileName(path),
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _watcher.Changed += (s, e) =>
        {
            Thread.Sleep(500); // 等待文件写入完成
            LoadConfig(path);
            Log.Information("配置已热更新");
        };
    }

    private void LoadConfig(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            var newConfig = JsonSerializer.Deserialize<ServerConfig>(json);
            if (newConfig != null)
            {
                lock (_lock)
                    _config = newConfig;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "配置加载失败（使用旧配置）");
        }
    }
}

// 使用
var configManager = new ConfigManager();
int port = configManager.Config.Port; // 运行时配置变了，下次读取自动更新
```

---

## 七、性能计数器

```csharp
class ServerMetrics
{
    private long _totalConnections;
    private long _currentConnections;
    private long _totalPacketsReceived;
    private long _totalPacketsSent;
    private long _totalProcessTimeMs;

    // 连接统计
    public void OnConnect()
    {
        Interlocked.Increment(ref _totalConnections);
        Interlocked.Increment(ref _currentConnections);
    }

    public void OnDisconnect()
    {
        Interlocked.Decrement(ref _currentConnections);
    }

    // 处理时间（微秒）
    public void RecordProcessTime(long microseconds)
    {
        Interlocked.Add(ref _totalProcessTimeMs, microseconds / 1000);
        Interlocked.Increment(ref _totalPacketsReceived);
    }

    // 性能数据
    public MetricSnapshot GetSnapshot()
    {
        return new MetricSnapshot
        {
            CurrentConnections = Interlocked.Read(ref _currentConnections),
            TotalPacketsReceived = Interlocked.Read(ref _totalPacketsReceived),
            AvgProcessTimeMs = _totalPacketsReceived > 0
                ? (double)_totalProcessTimeMs / _totalPacketsReceived
                : 0
        };
    }

    // 定时输出（每 60s）
    public void StartReportLoop()
    {
        var lastPackets = 0L;
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        Task.Run(async () =>
        {
            while (await timer.WaitForNextTickAsync())
            {
                var snap = GetSnapshot();
                var packetDiff = snap.TotalPacketsReceived - lastPackets;
                lastPackets = snap.TotalPacketsReceived;

                Log.Information(
                    "[Metrics] 连接 {Current} | 每分钟包 {Packets/min} | 平均耗时 {AvgMs:F2}ms",
                    snap.CurrentConnections,
                    packetDiff,
                    snap.AvgProcessTimeMs);
            }
        });
    }
}

public class MetricSnapshot
{
    public long CurrentConnections;
    public long TotalPacketsReceived;
    public double AvgProcessTimeMs;
}

// 使用
var metrics = new ServerMetrics();
metrics.OnConnect(); // 连接时调用
metrics.RecordProcessTime(stopwatch.Elapsed.TotalMicroseconds); // 处理完消息
```

---

## 八、对比 C++ 日志

```cpp
// C++ 常用日志库: spdlog
#include <spdlog/spdlog.h>
#include <spdlog/sinks/rotating_file_sink.h>

// 配置
auto logger = spdlog::rotating_logger_mt("server", "logs/server.log", 1048576 * 100, 30);
logger->set_level(spdlog::level::debug);
logger->set_pattern("[%Y-%m-%d %H:%M:%S.%e] [%^%l%$] %v");

// 使用
logger->info("玩家 {} 登录", playerId);
logger->warn("重试 {}/3", retry);
logger->error("处理消息 {} 异常", msgId);

// 性能对比
// spdlog: ~5ns/条件判断, ~50ns/格式化
// Serilog: ~10ns/条件判断, ~100ns/格式化 (异步 Sink 后 < 50ns)
```

### 对照表

| C# (Serilog) | C++ (spdlog) | 概念 |
|-------------|-------------|------|
| `Log.Information` | `logger->info` | 日志级别 |
| `{PlayerId}` 模板 | `{}` fmt 模板 | 格式化占位符 |
| `.WriteTo.File()` | `rotating_logger_mt` | 文件日志 |
| `.WriteTo.Async()` | `async_logger` | 异步写入 |
| `.MinimumLevel.Debug()` | `set_level(debug)` | 最小级别 |
| `retainedFileCountLimit: 30` | `max_files = 30` | 保留文件数 |
| `Log.CloseAndFlush()` | `spdlog::shutdown()` | 关闭刷新 |

---

## 九、练习

1. **Serilog 配置**：搭建一个带文件滚动、异步写入、每日分割的日志系统
2. **自定义 Sink**：将 Error 级别日志写入一个新的文本文件 error.log
3. **热更新**：实现配置热更新，端口改变后重新 listen
4. **性能计数器**：实现实时 PPS (Packet Per Second) 统计
5. **崩溃日志**：模拟一个空引用异常，看能否被 CrashGuard 捕获

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 结构化日志 | 用键值对模板代替字符串拼接，支持查询 |
| 异步 Sink | 日志写入不阻塞主线程 |
| 文件滚动 | 按时间/大小自动切分，防止单文件无限大 |
| 配置热更新 | 监听文件变化，运行时更新配置 |
| 性能计数器 | 实时监控服务器状态（连接/包量/耗时） |
| 崩溃日志 | 全局异常捕获，进程终结前紧急写入 |
