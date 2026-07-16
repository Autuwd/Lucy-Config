# Day 05：日志系统与配置管理 — 进阶深入

## 一、Serilog 深度特性

### 自定义 Enricher

Enricher 是 Serilog 中向每条日志添加额外属性的机制。比基础的日志模板更灵活。

```csharp
using Serilog.Core;
using Serilog.Events;

// 自定义 Enricher：添加服务器实例信息
class ServerInfoEnricher : ILogEventEnricher
{
    private readonly string _serverId;
    private readonly string _environment;
    private readonly LogEventProperty _processId;

    public ServerInfoEnricher(string serverId, string environment)
    {
        _serverId = serverId;
        _environment = environment;
        _processId = new LogEventProperty("ProcessId",
            new ScalarValue(Environment.ProcessId));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        // 添加服务器 ID（结构化的，可以搜索过滤）
        logEvent.AddPropertyIfAbsent(new LogEventProperty("ServerId",
            new ScalarValue(_serverId)));

        logEvent.AddPropertyIfAbsent(new LogEventProperty("Environment",
            new ScalarValue(_environment)));

        logEvent.AddPropertyIfAbsent(_processId);

        // 添加线程 ID
        logEvent.AddPropertyIfAbsent(factory.CreateProperty(
            "ThreadId", Environment.CurrentManagedThreadId));

        // 添加关联 ID（如果异步上下文中有）
        if (AsyncLocalCorrelationId.Current != null)
        {
            logEvent.AddPropertyIfAbsent(factory.CreateProperty(
                "CorrelationId", AsyncLocalCorrelationId.Current));
        }
    }
}

// AsyncLocal 关联 ID 传递
class AsyncLocalCorrelationId
{
    private static readonly AsyncLocal<string> _correlationId = new();

    public static string Current => _correlationId.Value;

    public static IDisposable Begin(string correlationId)
    {
        var previous = _correlationId.Value;
        _correlationId.Value = correlationId;
        return new DisposableAction(() => _correlationId.Value = previous);
    }
}

// 配置中使用
Log.Logger = new LoggerConfiguration()
    .Enrich.With(new ServerInfoEnricher("S1-Primary", "Production"))
    .WriteTo.Console()
    .CreateLogger();
```

### Destructuring（结构化展开）

```csharp
// 默认情况下，Serilog 对复杂对象调用 .ToString()
// Destructuring 控制如何展开对象

class PlayerDataDestructuring
{
    // 1. DestructureAsScalar：当作标量，不展开
    // 适合：简单对象，只显示 .ToString()
    // 配置：.Destructure.AsScalar<PlayerId>()

    // 2. DestructureAsDictionary：当字典展开
    // 适合：字典类型，按 Key-Value 显示
    // 配置：.Destructure.AsDictionary<PlayerData>()

    // 3. Tokenized：隐藏敏感信息
    public class PasswordHider : IDestructuringPolicy
    {
        public bool TryDestructure(object value,
            ILogEventPropertyValueFactory factory,
            out LogEventPropertyValue result)
        {
            if (value is string password && password.Length > 0)
            {
                result = new ScalarValue(
                    password[..1] + new string('*', password.Length - 1));
                return true;
            }
            result = null;
            return false;
        }
    }
}

// 使用 Destructuring
Log.Logger = new LoggerConfiguration()
    .Destructure.With(new PasswordHider())
    .Destructure.AsScalar<PlayerId>()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("玩家登录: {@Player}", new
{
    Account = "alice",
    Password = "supersecret123",
    PlayerId = "1001"
});
// 输出时 Password 会被隐藏
```

---



## 二、结构化日志最佳实践

### 属性命名规范

```csharp
class StructuredLoggingConventions
{
    // 游戏服务器日志属性命名规则：

    // ❌ 坏习惯：动态拼接
    Log.Information($"Player {playerId} killed monster {monsterId}");

    // ✅ 好习惯：模板 + 结构化参数
    Log.Information("Player {PlayerId} killed monster {MonsterId}",
        playerId, monsterId);

    // 命名约定：
    // - 实体类型 + Id：PlayerId, MonsterId, ItemId, MapId
    // - 数字量：Damage, Hp, Mp, Count, Amount
    // -布尔值：HasPrefix, IsCrit, IsHit
    // - 标识类型 + Name：SkillName, ItemName

    // @ 操作符：展开复杂对象（序列化）
    var playerState = new { Hp = 100, Mp = 50, Pos = new { X = 10, Y = 20 } };
    Log.Information("State: {@State}", playerState);

    // $ 操作符：字符串化（调用 ToString）
    Log.Information("Item: {$Item}", someItem);
}
```

### 日志位置标注

```csharp
class LogPosition
{
    // C# 10 CallerArgumentExpression
    // 自动记录日志产生的文件和行号

    public static void LogWithPosition(string message,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string member = "")
    {
        Log.ForContext("SourceFile", Path.GetFileName(file))
           .ForContext("SourceLine", line)
           .ForContext("SourceMember", member)
           .Information(message);
    }

    // 使用
    void SomeMethod()
    {
        LogWithPosition("玩家登录");
        // 自动记录：SourceFile: GameService.cs, SourceLine: 42, SourceMember: SomeMethod
    }

    // 也可以用 Serilog 的 SourceContext
    // 通过 Logger.ForContext<Type>() 自动添加类名
    private static readonly ILogger _log = Log.ForContext<LoginService>();
    // 每条日志自动带 SourceContext: "GameServer.Services.LoginService"
}
```

---

## 三、高级异步文件 Sink

### 批量刷新的深度优化

```csharp
class AsyncBatchFileSink : ILogEventSink, IDisposable
{
    private readonly string _basePath;
    private readonly int _maxFileSize;
    private readonly int _maxRetainedFiles;
    private readonly int _batchSize;
    private readonly int _flushIntervalMs;

    private readonly Channel<LogEvent> _channel;
    private readonly Task _flushTask;
    private readonly CancellationTokenSource _cts = new();
    private StreamWriter _currentWriter;
    private string _currentFilePath;
    private long _currentFileSize;
    private int _fileIndex;

    public AsyncBatchFileSink(string basePath,
        int maxFileSize = 100 * 1024 * 1024, // 100MB
        int maxRetainedFiles = 30,
        int batchSize = 100,
        int flushIntervalMs = 1000)
    {
        _basePath = basePath;
        _maxFileSize = maxFileSize;
        _maxRetainedFiles = maxRetainedFiles;
        _batchSize = batchSize;
        _flushIntervalMs = flushIntervalMs;

        // Bounded channel：防止日志把内存撑爆
        _channel = Channel.CreateBounded<LogEvent>(
            new BoundedChannelOptions(50000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                // 日志满了宁可丢也不能阻塞游戏逻辑
            });

        _flushTask = Task.Run(FlushLoop);
    }

    public void Emit(LogEvent logEvent)
    {
        // 非阻塞写，满则丢
        _channel.Writer.TryWrite(logEvent);
    }

    private async Task FlushLoop()
    {
        var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(_flushIntervalMs));

        var buffer = new List<string>(_batchSize);
        var writer = _channel.Reader;

        while (await timer.WaitForNextTickAsync(_cts.Token))
        {
            buffer.Clear();

            // 批量读取
            while (buffer.Count < _batchSize &&
                   writer.TryRead(out var logEvent))
            {
                buffer.Add(FormatLogEvent(logEvent));
            }

            if (buffer.Count == 0) continue;

            try
            {
                await EnsureWriter();
                foreach (var line in buffer)
                {
                    await _currentWriter.WriteLineAsync(line);
                }
                await _currentWriter.FlushAsync();
            }
            catch (Exception ex)
            {
                // 日志写入失败，尝试写 stderr
                try
                {
                    Console.Error.WriteLine(
                        $"日志写入失败: {ex.Message}");
                }
                catch { }
            }
        }
    }

    private async Task EnsureWriter()
    {
        if (_currentWriter == null || _currentFileSize >= _maxFileSize)
        {
            // 关闭旧文件
            if (_currentWriter != null)
            {
                await _currentWriter.DisposeAsync();
                _currentWriter = null;
            }

            // 创建新文件
            _fileIndex++;
            _currentFilePath = Path.Combine(
                Path.GetDirectoryName(_basePath),
                $"{Path.GetFileNameWithoutExtension(_basePath)}" +
                $"-{_fileIndex:000}{Path.GetExtension(_basePath)}");

            Directory.CreateDirectory(Path.GetDirectoryName(_basePath));
            _currentWriter = new StreamWriter(
                _currentFilePath, append: true,
                encoding: Encoding.UTF8,
                bufferSize: 65536); // 64KB 写缓冲区

            _currentFileSize = 0;

            // 清理旧文件
            CleanupOldFiles();
        }
    }

    private void CleanupOldFiles()
    {
        if (_maxRetainedFiles <= 0) return;

        try
        {
            var dir = Path.GetDirectoryName(_basePath);
            var pattern = $"{Path.GetFileNameWithoutExtension(_basePath)}-*" +
                          $"{Path.GetExtension(_basePath)}";
            var oldFiles = Directory.GetFiles(dir, pattern)
                .OrderByDescending(f => f)
                .Skip(_maxRetainedFiles);

            foreach (var file in oldFiles)
            {
                try { File.Delete(file); }
                catch { }
            }
        }
        catch { }
    }

    private string FormatLogEvent(LogEvent logEvent)
    {
        // JSON 格式，便于机器解析
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            t = logEvent.Timestamp.ToString("O"),
            l = logEvent.Level.ToString(),
            m = logEvent.RenderMessage(),
            p = GetProperties(logEvent),
            e = logEvent.Exception?.ToString()
        });
    }

    private Dictionary<string, object> GetProperties(LogEvent logEvent)
    {
        var props = new Dictionary<string, object>();
        foreach (var (key, value) in logEvent.Properties)
        {
            props[key] = value.ToString();
        }
        return props;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _flushTask.Wait(5000);
        _currentWriter?.Dispose();
        _cts.Dispose();
    }
}
```

---

## 四、运行时日志级别动态调整

### IOptionsMonitor + 配置文件热加载

```csharp
// Serilog 的动态级别切换
// 通过 IOptionsMonitor 监听配置变化

class LogLevelManager
{
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly IOptionsMonitor<LogSettings> _settingsMonitor;
    private IDisposable _changeToken;

    public LogLevelManager(IOptionsMonitor<LogSettings> settingsMonitor)
    {
        _levelSwitch = new LoggingLevelSwitch();

        // 初始级别
        _levelSwitch.MinimumLevel = ParseLevel(
            settingsMonitor.CurrentValue.Level);

        // 监听配置变化
        _changeToken = settingsMonitor.OnChange(settings =>
        {
            var newLevel = ParseLevel(settings.Level);
            if (newLevel != _levelSwitch.MinimumLevel)
            {
                Log.Information("日志级别已更改: {OldLevel} → {NewLevel}",
                    _levelSwitch.MinimumLevel, newLevel);
                _levelSwitch.MinimumLevel = newLevel;
            }
        });
    }

    public LoggingLevelSwitch Switch => _levelSwitch;

    // 也可以远程 API 控制
    public void SetLevelFromRemote(string levelStr)
    {
        var level = ParseLevel(levelStr);
        Log.Warning("[远程] 日志级别被管理员更改: {NewLevel}", level);
        _levelSwitch.MinimumLevel = level;
    }

    private static LogEventLevel ParseLevel(string level)
    {
        return level?.ToLower() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}

// 配置类
class LogSettings
{
    public string Level { get; set; } = "Information";
    public string FilePath { get; set; } = "logs/server-.log";
    public int MaxFileSizeMB { get; set; } = 100;
    public int RetainedFileCount { get; set; } = 30;
}

// 配套的 IOptionsMonitor 注册
// services.Configure<LogSettings>(config.GetSection("Log"));
// services.AddSingleton<LogLevelManager>();
```

---

## 五、高性能日志：LoggerMessage.Define

### 零分配日志

```csharp
// LoggerMessage.Define 在编译时就生成日志模板
// 不记录时完全零开销，记录时只分配参数

class HighPerformanceLogger
{
    // 1. 定义日志事件的委托（静态字段，一次创建永久复用）
    private static readonly Action<ILogger, long, string, Exception>
        _playerLogin = LoggerMessage.Define<long, string>(
            LogLevel.Information,
            new EventId(1001, "PlayerLogin"),
            "玩家 {PlayerId} 从 {IP} 登录");

    private static readonly Action<ILogger, long, int, Exception>
        _playerLevelUp = LoggerMessage.Define<long, int>(
            LogLevel.Information,
            new EventId(1002, "PlayerLevelUp"),
            "玩家 {PlayerId} 升级到 {NewLevel}");

    private static readonly Action<ILogger, long, long, int, Exception>
        _playerGetItem = LoggerMessage.Define<long, long, int>(
            LogLevel.Debug,
            new EventId(2001, "PlayerGetItem"),
            "玩家 {PlayerId} 获得道具 {ItemId}x{Count}");

    // 2. 使用这些定义好的委托（不产生额外分配）
    private readonly ILogger _logger;

    public HighPerformanceLogger(ILogger<HighPerformanceLogger> logger)
    {
        _logger = logger;
    }

    public void LogPlayerLogin(long playerId, string ip)
    {
        _playerLogin(_logger, playerId, ip, null);
    }

    public void LogPlayerLevelUp(long playerId, int newLevel)
    {
        // 如果日志级别不够，直接跳过，零开销
        _playerLevelUp(_logger, playerId, newLevel, null);
    }

    public void LogPlayerGetItem(long playerId, long itemId, int count)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            // 由于 LoggerMessage.Define 不会检查级别
            // 手动 IsEnabled 可以减少方法调用开销
            _playerGetItem(_logger, playerId, itemId, count, null);
        }
    }
}

// 性能对比（100 万次日志调用，不记录的情况）：
// Serilog.Log.Information:    ~15ns
// LoggerMessage.Define:        ~2ns  (接近空方法调用)
// Serilog 带 if IsEnabled:     ~3ns
```

---

## 六、ETW（EventSource）高性能追踪

### 使用 EventSource 代替日志

```csharp
// .NET 的 EventSource 是 Windows ETW 的托管封装
// 它对性能的影响极低：不监听时不产生任何开销
// 适合记录高频事件（每包处理时间、GC 事件、锁竞争）

[EventSource(Name = "GameServer-Performance")]
class GameServerEventSource : EventSource
{
    // 单例模式（EventSource 需要）
    public static readonly GameServerEventSource Log = new();

    // 定义事件：消息处理耗时
    // - 不监听时，这个方法变为空调用（零开销）
    // - 监听时，参数自动序列化到 ETW 流
    [Event(1, Message = "消息处理: {0} 耗时 {1}ms 包大小 {2}",
           Level = EventLevel.Informational)]
    public void PacketProcessed(
        string msgType,
        long elapsedMs,
        int packetSize)
    {
        WriteEvent(1, msgType, elapsedMs, packetSize);
    }

    // 定义事件：GC 发生后记录
    [Event(2, Level = EventLevel.Warning)]
    public void GarbageCollection(int gen, long pauseMs)
    {
        WriteEvent(2, gen, pauseMs);
    }

    // 定义事件：连接数变化
    [Event(3, Level = EventLevel.Verbose)]
    public void ConnectionCountChanged(int currentCount)
    {
        WriteEvent(3, currentCount);
    }

    // 定义事件：锁竞争
    [Event(4, Level = EventLevel.Warning)]
    public void LockContention(string lockName, long waitMs)
    {
        WriteEvent(4, lockName, waitMs);
    }

    // 关键：WriteEvent 的泛型版本避免装箱
    // 但参数类型必须是 int/long/string 等 EventSource 原生支持的
}

// 使用 ETW 日志
class PerformanceTracer
{
    public void TrackPacket(string msgType, int size, Action processAction)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            processAction();
        }
        finally
        {
            sw.Stop();
            // 即使不监听，这个方法开销 ≈ 空方法
            GameServerEventSource.Log.PacketProcessed(
                msgType, sw.ElapsedMilliseconds, size);
        }
    }
}

// GC 事件监听
class GcMonitor
{
    // 注册 GC 通知（每 2 次 GC 采样一次）
    public GcMonitor()
    {
        GC.RegisterForFullGCNotification(25, 25);
        Task.Run(MonitorGc);
    }

    private void MonitorGc()
    {
        while (true)
        {
            GC.WaitForFullGCApproach();
            var sw = Stopwatch.StartNew();
            GC.WaitForFullGCComplete();
            sw.Stop();

            // 记录 GC 停顿
            GameServerEventSource.Log.GarbageCollection(
                2, // Gen 2 GC
                sw.ElapsedMilliseconds);

            Log.Warning("Full GC 发生，暂停 {PauseMs}ms", sw.ElapsedMilliseconds);
            Thread.Sleep(1000);
        }
    }
}
```

### ETW 与 Serilog 配合

```csharp
// 使用场景混合：
// - ETW：用于高频、高性能关键路径（跟踪、profile）
//   → 不监听时零开销
// - Serilog：用于业务日志、审计日志
//   → 需要日志级别的灵活性

// ETW 的监听工具：
// - PerfView (Windows)：微软的性能分析工具
// - dotnet-counters：CLI 监控
// - dotnet-trace：CLI 追踪
```

---

## 七、内存映射文件日志轮转

### 高性能轮转方案

```csharp
// 使用内存映射文件 (MMF) 实现日志轮转，避免频繁 FileStream 操作
class MappedLogRotator
{
    private MemoryMappedFile _currentMmf;
    private MemoryMappedViewAccessor _currentAccessor;
    private long _currentOffset;
    private int _fileIndex;
    private readonly long _maxFileSize;
    private readonly int _maxBackup;
    private readonly string _logDir;

    public MappedLogRotator(string logDir,
        long maxFileSize = 100 * 1024 * 1024, int maxBackup = 10)
    {
        _logDir = logDir;
        _maxFileSize = maxFileSize;
        _maxBackup = maxBackup;
        Directory.CreateDirectory(logDir);
        Rotate();
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (_currentOffset + data.Length > _maxFileSize) Rotate();
        _currentAccessor?.WriteArray(
            _currentOffset, data.ToArray(), 0, data.Length);
        _currentOffset += data.Length;
    }

    private void Rotate()
    {
        _currentAccessor?.Dispose();
        _currentMmf?.Dispose();
        _fileIndex++;
        var path = Path.Combine(_logDir,
            $"server-{DateTime.Now:yyyyMMdd}-{_fileIndex:000}.log");
        _currentMmf = MemoryMappedFile.CreateFromFile(
            path, FileMode.Create, null, _maxFileSize,
            MemoryMappedFileAccess.ReadWrite);
        _currentAccessor = _currentMmf.CreateViewAccessor(
            0, _maxFileSize, MemoryMappedFileAccess.ReadWrite);
        _currentOffset = 0;
        // 清理旧文件
        foreach (var f in Directory.GetFiles(_logDir, "server-*.log")
            .OrderByDescending(f => f).Skip(_maxBackup))
        { try { File.Delete(f); } catch { } }
    }

    public void Dispose()
    {
        _currentAccessor?.Flush();
        _currentAccessor?.Dispose();
        _currentMmf?.Dispose();
    }
}
```

---

## 八、配置管理：IOptionsMonitor 深度

### IOptions vs IOptionsSnapshot vs IOptionsMonitor

```csharp
class OptionsComparison
{
    /*
    IOptions<T>：
      - 单例注册
      - 启动时加载一次，永不更新
      - 适合全局不变的配置

    IOptionsSnapshot<T>：
      - Scoped 注册
      - 每次请求重新加载
      - 适合 Web API 场景

    IOptionsMonitor<T>：
      - 单例注册
      - 配置变化时通知
      - 适合游戏服务器（持续运行）
      - 子依赖自动更新
    */

    // 游戏服务器最佳实践：
    // 使用 IOptionsMonitor + 手动刷新
    
    private readonly IOptionsMonitor<ServerConfig> _configMonitor;
    private IDisposable _changeListener;

    public ConfigDrivenService(IOptionsMonitor<ServerConfig> monitor)
    {
        _configMonitor = monitor;

        // 监听变更
        _changeListener = monitor.OnChange(newConfig =>
        {
            // 处理每个需要热更新的配置项
            if (newConfig.Log.Level != currentLogLevel)
            {
                UpdateLogLevel(newConfig.Log.Level);
            }

            if (newConfig.Server.Port != currentPort)
            {
                // 端口变更需要重启监听
                Task.Run(() => RestartListener(newConfig.Server.Port));
            }

            if (newConfig.Database.ConnectionString != currentDbString)
            {
                // 数据库连接变更需要重建连接池
                RebuildDbPool(newConfig.Database.ConnectionString);
            }
        });
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Enricher | 给每条日志加固定属性（ServerId, ThreadId, 关联 ID） |
| Destructuring | 控制 Serilog 展开对象的深度和方式 |
| 结构化命名 | {PlayerId} {ItemId} 模板，避免字符串拼接 |
| Async Batch Sink | Channel + 定时批量写入，非阻塞高性能 |
| LoggerMessage.Define | 预编译日志模板，不记录时 ≈ 空调用 |
| 动态级别 | LoggingLevelSwitch + IOptionsMonitor 运行时切换 |
| ETW EventSource | 高频追踪的零开销方案，不监听时零性能损失 |
| 内存映射日志 | MMF 直接写文件区域，避免 FileStream 开销 |
| IOptionsMonitor | 配置热更新 + 自动通知依赖方 |

---

## 对照表：C++ 日志 vs C# 进阶

| C++ | C# 等价 | 差异 |
|-----|---------|------|
| spdlog 自定义 sink | `ILogEventSink` | 接口设计相似 |
| spdlog 异步 logger | `Serilog.Sinks.Async` | 都是批量队列 |
| fmt 格式化 | `LoggerMessage.Define` | C# 可以编译时预定义 |
| ETW (C++) | `EventSource` | 完全对应，API 不同 |
| mmap 日志 | `MemoryMappedFile` | 底层实现一致 |
| 日志级别运行时改 | `LoggingLevelSwitch` | C# 有 IOptionsMonitor 支持 |
| 结构化日志 (spdlog v2) | Serilog | C# 结构化原生支持更好 |
