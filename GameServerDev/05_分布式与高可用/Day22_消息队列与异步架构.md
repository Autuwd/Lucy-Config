# Day 22：消息队列与异步架构

## 一、为什么游戏服务器需要消息队列

### 同步调用的痛点

```csharp
// 玩家登录时，需要加载：
// 1. 玩家基本信息
// 2. 背包道具
// 3. 技能
// 4. 任务进度
// 5. 好友列表
// 6. 公会信息
// 7. 邮件

// 同步串行：
var player = await db.GetPlayerAsync(playerId);          // 50ms
var inventory = await db.GetInventoryAsync(playerId);     // 30ms
var skills = await db.GetSkillsAsync(playerId);           // 20ms
var quests = await db.GetQuestsAsync(playerId);           // 30ms
// 总计: 130ms（串行，大部分时间在等待）
```

### 消息队列解耦

```csharp
// 异步并行：
var playerTask = db.GetPlayerAsync(playerId);
var inventoryTask = db.GetInventoryAsync(playerId);
var skillsTask = db.GetSkillsAsync(playerId);
var questsTask = db.GetQuestsAsync(playerId);

await Task.WhenAll(playerTask, inventoryTask, skillsTask, questsTask);
// 总计: ~50ms（并行，最慢的那一个）
```

### 游戏服务器中的消息队列场景

| 场景 | 方案 | 说明 |
|------|------|------|
| 跨服通信 | RabbitMQ / Redis Stream | 场景 A → 场景 B 玩家转移 |
| 日志写入 | 内部 Channel + 批量写库 | 战斗日志、操作日志 |
| 异步任务 | Redis Stream / Channel | 邮件发送、奖励发放 |
| 事件分发 | 事件总线 (EventBus) | 升级触发加属性等连锁反应 |

---

## 二、进程内消息队列 (Channel)

### Channel 基础

```csharp
// System.Threading.Channels
// 无锁、高性能的进程内消息队列

// 创建
var channel = Channel.CreateUnbounded<GameEvent>(new UnboundedChannelOptions
{
    SingleWriter = false,  // 多生产者
    SingleReader = true    // 单消费者
});

// 有限队列（背压）
var bounded = Channel.CreateBounded<GameEvent>(new BoundedChannelOptions(10000)
{
    FullMode = BoundedChannelFullMode.Wait,  // 满时等待
    // DropWrite: 满时丢弃
    // DropNewest: 丢弃最新的
    // DropOldest: 丢弃最旧的
});
```

### EventBus 实现

```csharp
// 游戏事件系统
public class GameEvent
{
    public string EventType { get; set; } // "PlayerLevelUp", "MonsterKilled"
    public Dictionary<string, object> Data { get; set; } = new();
}

public class EventBus : IDisposable
{
    private readonly Channel<GameEvent> _channel;
    private readonly Dictionary<string, List<Func<GameEvent, Task>>> _handlers = new();
    private readonly CancellationTokenSource _cts = new();

    public EventBus()
    {
        _channel = Channel.CreateUnbounded<GameEvent>();
        _ = ProcessLoop();
    }

    // 注册事件处理器
    public void Subscribe(string eventType, Func<GameEvent, Task> handler)
    {
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Func<GameEvent, Task>>();
        _handlers[eventType].Add(handler);

        Log.Debug("事件订阅: {EventType}", eventType);
    }

    // 发布事件
    public void Publish(string eventType, Dictionary<string, object> data = null)
    {
        var evt = new GameEvent
        {
            EventType = eventType,
            Data = data ?? new Dictionary<string, object>()
        };

        // 即使消费者慢了也不阻塞发布者
        if (!_channel.Writer.TryWrite(evt))
        {
            Log.Warning("事件队列满，丢弃事件: {EventType}", eventType);
        }
    }

    // 处理循环
    private async Task ProcessLoop()
    {
        var reader = _channel.Reader;

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                // 等待事件
                var evt = await reader.ReadAsync(_cts.Token);

                // 分发
                if (_handlers.TryGetValue(evt.EventType, out var handlers))
                {
                    // 并行执行所有处理器
                    var tasks = handlers.Select(h => h(evt));
                    await Task.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "事件处理异常");
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _channel.Writer.TryComplete();
    }
}

// 使用
class BattleService
{
    private readonly EventBus _eventBus;

    public BattleService(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task OnMonsterKilled(long playerId, long monsterId, int exp)
    {
        // 怪物死亡事件
        _eventBus.Publish("MonsterKilled", new Dictionary<string, object>
        {
            ["PlayerId"] = playerId,
            ["MonsterId"] = monsterId,
            ["Exp"] = exp
        });
    }
}

// 事件订阅者
class LevelService
{
    public LevelService(EventBus eventBus)
    {
        eventBus.Subscribe("MonsterKilled", OnMonsterKilled);
    }

    private async Task OnMonsterKilled(GameEvent evt)
    {
        long playerId = (long)evt.Data["PlayerId"];
        int exp = (int)evt.Data["Exp"];

        // 增加经验
        await AddExp(playerId, exp);
    }
}

class QuestService
{
    public QuestService(EventBus eventBus)
    {
        eventBus.Subscribe("MonsterKilled", OnMonsterKilled);
    }

    private async Task OnMonsterKilled(GameEvent evt)
    {
        // 更新任务进度
        long playerId = (long)evt.Data["PlayerId"];
        long monsterId = (long)evt.Data["MonsterId"];
        await UpdateQuestProgress(playerId, monsterId);
    }
}
```

---

## 三、Redis Stream 实现跨服通信

### 为什么跨服通信用 Stream 而不是 Pub/Sub

```
Pub/Sub: 消费者离线丢消息，无确认机制
Stream:  消息持久化，消费者组，ACK 确认
```

### 跨服场景通信

```csharp
public class CrossServerEventBus
{
    private readonly IDatabase _redis;
    private const string StreamKey = "stream:cross_server";

    // 发布跨服事件
    public async Task Publish(CrossServerEvent evt)
    {
        var fields = new NameValueEntry[]
        {
            new("event_type", evt.EventType),
            new("source_server", evt.SourceServer),
            new("data", JsonSerializer.Serialize(evt.Data)),
            new("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())
        };

        // 写入 Stream
        await _redis.StreamAddAsync(StreamKey, fields, maxLength: 100000);
    }

    // 消费者组消费（每个服务作为一个消费者组）
    public async Task InitConsumerGroup()
    {
        try
        {
            await _redis.StreamCreateConsumerGroupAsync(
                StreamKey, "scene_servers", "0-0");
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // 组已存在
        }
    }

    // 消费消息
    public async Task Consume(string groupName, string consumerId,
        Func<CrossServerEvent, Task> handler)
    {
        while (true)
        {
            var messages = await _redis.StreamReadGroupAsync(
                StreamKey,
                groupName,
                consumerId,
                ">", // 只读未投递的消息
                count: 10);

            foreach (var msg in messages)
            {
                try
                {
                    var evt = ParseEvent(msg.Values);
                    await handler(evt);

                    // 处理成功后 ACK
                    await _redis.StreamAcknowledgeAsync(
                        StreamKey, groupName, msg.Id);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "处理跨服事件失败");

                    // 如果失败超过 3 次，移到死信队列
                    var info = await _redis.StreamPendingExAsync(
                        StreamKey, groupName, count: 1, consumerId);
                    if (info.Length > 0 && info[0].DeliveryCount > 3)
                    {
                        await MoveToDeadLetter(msg);
                        await _redis.StreamAcknowledgeAsync(
                            StreamKey, groupName, msg.Id);
                    }
                }
            }

            // 没有消息时等待
            if (messages.Length == 0)
                await Task.Delay(100);
        }
    }

    private CrossServerEvent ParseEvent(NameValueEntry[] entries)
    {
        return new CrossServerEvent
        {
            EventType = entries.First(e => e.Name == "event_type").Value,
            SourceServer = entries.First(e => e.Name == "source_server").Value,
            Data = JsonSerializer.Deserialize<Dictionary<string, object>>(
                entries.First(e => e.Name == "data").Value)
        };
    }

    private async Task MoveToDeadLetter(StreamEntry msg)
    {
        var deadLetterKey = $"{StreamKey}:dead";
        await _redis.StreamAddAsync(deadLetterKey, msg.Values);
    }
}

public class CrossServerEvent
{
    public string EventType { get; set; }
    public string SourceServer { get; set; }
    public Dictionary<string, object> Data { get; set; }
}
```

---

## 四、异步写入日志

```csharp
class AsyncLogWriter
{
    private readonly Channel<LogEntry> _channel =
        Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(50000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    // 生产者
    public void WriteLog(LogEntry entry)
    {
        if (!_channel.Writer.TryWrite(entry))
        {
            // 队列满了，丢弃（防止拖慢主逻辑）
            Interlocked.Increment(ref _droppedCount);
        }
    }

    // 消费者（后台单线程批量写入）
    public async Task FlushLoop(CancellationToken ct)
    {
        var batch = new List<LogEntry>(500);
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));

        while (await timer.WaitForNextTickAsync(ct))
        {
            // Drain the channel
            while (batch.Count < 500 && _channel.Reader.TryRead(out var entry))
            {
                batch.Add(entry);
            }

            if (batch.Count > 0)
            {
                try
                {
                    await BulkWriteToDb(batch);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "批量写入日志失败，丢失 {Count} 条日志", batch.Count);
                    // 可考虑写入本地文件作为兜底
                }
                batch.Clear();
            }
        }
    }

    private async Task BulkWriteToDb(List<LogEntry> batch)
    {
        // 使用 Dapper 批量插入
        using var conn = new MySqlConnection(_connectionString);
        await conn.ExecuteAsync(
            "INSERT INTO battle_logs (player_id, event_type, data, created_at) " +
            "VALUES (@PlayerId, @EventType, @Data, @CreatedAt)",
            batch);

        Interlocked.Add(ref _writtenCount, batch.Count);
    }

    private long _writtenCount;
    private long _droppedCount;

    public (long written, long dropped) GetStats()
    {
        return (Interlocked.Read(ref _writtenCount), Interlocked.Read(ref _droppedCount));
    }
}
```

---

## 五、RabbitMQ 基础

### 为什么可能需要 RabbitMQ

```
Redis Stream 够用场景：
  - 单机或少量服务器
  - 简单的跨服通信
  - 不需要复杂的路由规则

RabbitMQ 适用场景：
  - 复杂路由（Topic Exchange）
  - 消息持久化要求高
  - 跨语言/跨平台通信
  - 已有的运维基础设施
```

### RabbitMQ 集成

```csharp
// dotnet add package RabbitMQ.Client

public class RabbitMqEventBus
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqEventBus(string host = "localhost")
    {
        var factory = new ConnectionFactory { HostName = host };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // 声明 Exchange
        _channel.ExchangeDeclare("game.events", ExchangeType.Topic, durable: true);
    }

    // 发布事件到 Exchange
    public void Publish(string routingKey, byte[] message)
    {
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true; // 持久化
        properties.Timestamp = new AmqpTimestamp(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: "game.events",
            routingKey: routingKey,
            basicProperties: properties,
            body: message);
    }

    // 消费队列（每个路由键绑定到不同队列）
    public void Subscribe(string queueName, string routingKey,
        Action<byte[]> handler)
    {
        // 声明队列
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // 绑定 Exchange 和 Queue
        _channel.QueueBind(
            queue: queueName,
            exchange: "game.events",
            routingKey: routingKey);

        // 消费
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                handler(ea.Body.ToArray());
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch
            {
                _channel.BasicNack(ea.DeliveryTag, false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}

// 路由键设计
// game.scene.player_enter    → 场景服务器订阅 scene.*
// game.social.guild_create   → 社交服务器订阅 social.*
// game.battle.battle_end     → 战斗服务器订阅 battle.*
```

---

## 六、练习

1. **Channel 事件总线**：实现进程内事件总线，支持升级触发多个系统响应
2. **异步日志**：实现 Channel + 批量写入的战斗日志系统
3. **Redis Stream 跨服通信**：实现两个场景服务器之间的玩家切换
4. **RabbitMQ 集成**：用 RabbitMQ Topic Exchange 实现消息分发
5. **对比例表**：比较 Channel / Redis Stream / RabbitMQ 在游戏服务器中的适用场景

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Channel | 进程内无锁队列，适合异步解耦 |
| EventBus | 事件驱动的观察者模式，模块间解耦 |
| Redis Stream | 持久化跨服消息，支持消费者组和 ACK |
| 异步写入 | 批量写日志不阻塞主逻辑 |
| RabbitMQ | 复杂路由的跨服消息队列 |
| 背压 | 队列满时阻塞/丢弃策略平衡生产消费 |
