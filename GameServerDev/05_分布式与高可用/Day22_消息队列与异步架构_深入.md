# Day 22：消息队列与异步架构 — 进阶深入

## 一、Kafka 分区与消费者组

### 分区策略对游戏的影响

```
Kafka 核心概念：
  Topic → 多个 Partition（分区）→ 每个分区内有序
  消费者组内每个分区只会被一个消费者消费

游戏场景的分区策略：
  - 玩家 ID: 按 hash(uid) % N 保证同一个玩家的消息有序
  - 服务器 ID: 按服务器分区分区保证独立处理
  - 事件类型：不同类型（战斗/社交/交易）可分配到不同分区
```

```csharp
// 游戏事件分区生产者
public class GameEventProducer
{
    private readonly IProducer<string, byte[]> _producer;

    public GameEventProducer(string brokers)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = brokers,
            // 启用幂等性（防止重复发送）
            EnableIdempotence = true,
            // ACK 策略：all（等待所有副本确认）
            Acks = Acks.All,
            // 压缩（游戏消息压缩比高）
            CompressionType = CompressionType.Snappy,
            // 重试设置
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100
        };
        _producer = new ProducerBuilder<string, byte[]>(config).Build();
    }

    // 按玩家 ID 分区：保证同一玩家的所有事件有序
    public async Task PublishPlayerEvent(long playerId, GameEvent evt)
    {
        // 分区键 = 玩家 ID
        // 同一个玩家的事件必然进入同一个分区
        var message = new Message<string, byte[]>
        {
            Key = playerId.ToString(),
            Value = SerializeEvent(evt),
            Headers = new Headers
            {
                { "event_type", Encoding.UTF8.GetBytes(evt.Type) },
                { "timestamp", BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) }
            }
        };

        var result = await _producer.ProduceAsync("game-events", message);

        if (result.Status != PersistenceStatus.Persisted)
            Log.Warning("消息投递状态异常: {Status}", result.Status);
    }

    // 跨服广播事件（不分区，随机分发）
    public async Task PublishBroadcast(GameEvent evt)
    {
        // 不使用 Key，Kafka 自动轮询分配分区
        var message = new Message<string, byte[]>
        {
            Value = SerializeEvent(evt),
            Partition = Partition.Any
        };
        await _producer.ProduceAsync("game-broadcast", message);
    }
}
```

### 消费者组重平衡处理

```csharp
// 重平衡监听器：当分区重新分配时保存/恢复状态
public class GameRebalanceListener : IConsumerRebalanceListener
{
    private readonly PlayerStateManager _stateManager;

    // 分区被撤回前：保存当前处理的状态
    public void PartitionsRevoked(IConsumer<string, byte[]> consumer, List<TopicPartitionOffset> partitions)
    {
        Log.Warning("分区被撤回: {Partitions}", string.Join(",", partitions));

        foreach (var partition in partitions)
        {
            // 保存该分区内正在处理的玩家状态
            _stateManager.FlushPartitionState(partition.Partition.Value);
            // 提交当前 offset（确保不丢消息）
            consumer.Commit(new[] { partition });
        }
    }

    // 分配到新分区：加载该分区的状态
    public void PartitionsAssigned(IConsumer<string, byte[]> consumer, List<TopicPartitionOffset> partitions)
    {
        Log.Information("分配到新分区: {Partitions}", string.Join(",", partitions));

        foreach (var partition in partitions)
        {
            // 从上次保存的 offset 继续消费
            _stateManager.LoadPartitionState(partition.Partition.Value);
        }

        // 指定从最新 offset 开始消费（避免消费旧的重复数据）
        consumer.Seek(partitions.Last());
    }
}

// 优雅关闭消费者
public class GracefulConsumerShutdown
{
    private readonly IConsumer<string, byte[]> _consumer;

    public async Task Shutdown()
    {
        Log.Information("开始优雅关闭消费者...");

        // 停止消费循环（设置 CancellationToken）
        _cancellationSource.Cancel();

        // 等待当前批次处理完成
        await Task.Delay(TimeSpan.FromSeconds(5));

        // 提交所有未提交的 offset
        _consumer.Commit();

        // 关闭消费者
        _consumer.Close();

        Log.Information("消费者已优雅关闭");
    }
}
```

### Kafka vs RabbitMQ 选择

```
Kafka 的场景：
  - 高吞吐量日志采集（战斗日志/行为日志）
  - 需要消息重放（Event Sourcing）
  - 需要长时间保存消息（天级别的回溯消费）
  - 数据管道（实时 ETL）

RabbitMQ 的场景：
  - 复杂路由（多级 Topic Exchange）
  - 需要死信队列和延迟队列
  - 消息 TTL 和优先级
  - 运维团队熟悉 AMQP 协议
```

---

## 二、RabbitMQ Quorum 队列

```yaml
# Quorum 队列配置（RabbitMQ 3.8+）
# 基于 Raft 协议，比 Classic Mirrored 更可靠

# 声明 Quorum 队列
rabbitmqctl set_policy quorum-policy "^game\." \
  '{"ha-mode":"exactly","ha-params":3,"queue-type":"quorum"}' \
  --apply-to queues

# Quorum 队列特性：
#   - 数据存储在多数节点上（容忍少数节点故障）
#   - 自动 Leader 选举
#   - 比镜像队列更少的网络开销
#   - 不支持 transient 消息（必须持久化）
```

```csharp
// 游戏中使用 Quorum 队列
public class GameQuorumQueue
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public GameQuorumQueue(string host)
    {
        var factory = new ConnectionFactory
        {
            HostName = host,
            // 启用自动恢复
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            // 心跳检测
            RequestedHeartbeat = TimeSpan.FromSeconds(30)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void DeclareQuorumQueue(string queueName)
    {
        var args = new Dictionary<string, object>
        {
            // 指定 Quorum 类型
            { "x-queue-type", "quorum" },
            // 副本数
            { "x-quorum-initial-group-size", 3 },
            // 投递限制（防止消息堆积过多）
            { "x-max-length", 100000 },
            { "x-overflow", "reject-publish" }
        };

        _channel.QueueDeclare(
            queue: queueName,
            durable: true,      // Quorum 必须是持久队列
            exclusive: false,
            autoDelete: false,
            arguments: args);
    }
}
```

---

## 三、死信队列模式

```csharp
// 游戏事件死信队列完整实现
public class DeadLetterPipeline
{
    private readonly IModel _channel;

    public void SetupDeadLetterQueue(string mainQueue)
    {
        // 死信队列
        string dlq = $"{mainQueue}.dlq";
        _channel.QueueDeclare(dlq, durable: true, exclusive: false, autoDelete: false);

        // 主队列：设置死信转发
        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "" },
            { "x-dead-letter-routing-key", dlq },
            // 消息 TTL（超过时间自动变死信）
            { "x-message-ttl", 3600000 },    // 1 小时
            // 最大重试次数（通过消息头部控制）
            { "x-delivery-limit", 3 }
        };

        _channel.QueueDeclare(mainQueue, durable: true, exclusive: false, autoDelete: false, arguments: args);

        // 启动死信处理消费者
        var dlqConsumer = new EventingBasicConsumer(_channel);
        dlqConsumer.Received += (model, ea) =>
        {
            var retryCount = GetRetryCount(ea.BasicProperties);

            if (retryCount < 3)
            {
                // 重试：重新投递回主队列
                var props = _channel.CreateBasicProperties();
                props.Headers = new Dictionary<string, object>
                {
                    { "x-retry-count", retryCount + 1 }
                };
                _channel.BasicPublish("", mainQueue, props, ea.Body.ToArray());
                Log.Warning("死信消息重试第 {Retry} 次", retryCount + 1);
            }
            else
            {
                // 超过最大重试次数：记录到错误日志并丢弃
                Log.Error("消息处理失败（已重试 {Retry} 次），消息内容: {Body}",
                    retryCount, Encoding.UTF8.GetString(ea.Body.ToArray()));
                // 可以存到 MongoDB 或 Elasticsearch 供人工分析
            }

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(dlq, autoAck: false, consumer: dlqConsumer);
    }

    private int GetRetryCount(IBasicProperties props)
    {
        if (props.Headers?.TryGetValue("x-retry-count", out var count) == true)
            return Convert.ToInt32(count);
        return 0;
    }
}

// 游戏场景：跨服战斗结果处理
// 战斗结果投递失败 → 死信队列 → 自动重试3次 → 人工排查
```

---

## 四、Outbox 模式：双写一致性

### 问题场景

```
问题：向数据库写入 + 发送消息，这两步不是原子的
要么：先写 DB 再发消息 → DB 成功消息没发（丢数据）
要么：先发消息再写 DB → 消息发了 DB 失败（脏数据）

Outbox 模式解决：将消息先写入同一数据库的 outbox 表
然后后台进程读取 outbox 表并投递消息
```

```sql
-- Outbox 表结构
CREATE TABLE message_outbox (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    aggregate_type VARCHAR(64) NOT NULL COMMENT '聚合类型(player/battle/guild)',
    aggregate_id VARCHAR(128) NOT NULL COMMENT '聚合ID',
    event_type VARCHAR(64) NOT NULL COMMENT '事件类型',
    payload JSON NOT NULL COMMENT '事件数据(JSON)',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at DATETIME DEFAULT NULL COMMENT '投递时间',
    retry_count INT DEFAULT 0 COMMENT '重试次数',
    status VARCHAR(16) DEFAULT 'pending' COMMENT 'pending/processing/completed/failed',
    KEY idx_status_created (status, created_at),
    KEY idx_aggregate (aggregate_type, aggregate_id)
) ENGINE=InnoDB;
```

```csharp
// Outbox 模式实现
public class OutboxService
{
    private readonly IDbConnection _db;
    private readonly IProducer<string, byte[]> _producer;

    // 写业务数据 + 写 outbox（同一事务）
    public async Task ExecuteWithOutbox(Func<IDbTransaction, Task> businessAction, OutboxMessage message)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // 1. 执行业务逻辑
            await businessAction(tx);

            // 2. 写入 outbox（同一事务保证原子性）
            await conn.ExecuteAsync(
                @"INSERT INTO message_outbox (aggregate_type, aggregate_id, event_type, payload)
                  VALUES (@AggregateType, @AggregateId, @EventType, @Payload)",
                new
                {
                    message.AggregateType,
                    message.AggregateId,
                    message.EventType,
                    Payload = JsonSerializer.Serialize(message.Payload)
                }, tx);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // Outbox 发送器（后台进程轮询）
    public async Task ProcessOutbox(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // 批量取待发送的消息
            var messages = await _db.QueryAsync<OutboxRecord>(
                @"SELECT * FROM message_outbox
                  WHERE status = 'pending' AND retry_count < 5
                  ORDER BY created_at
                  LIMIT 100 FOR UPDATE");

            foreach (var msg in messages)
            {
                try
                {
                    // 标记为处理中
                    await _db.ExecuteAsync(
                        "UPDATE message_outbox SET status = 'processing' WHERE id = @Id",
                        new { msg.Id });

                    // 发送到 Kafka
                    var result = await _producer.ProduceAsync("game-events",
                        new Message<string, byte[]>
                        {
                            Key = msg.AggregateId,
                            Value = Encoding.UTF8.GetBytes(msg.Payload)
                        });

                    // 标记完成
                    await _db.ExecuteAsync(
                        "UPDATE message_outbox SET status = 'completed', processed_at = NOW() WHERE id = @Id",
                        new { msg.Id });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Outbox 发送失败 (ID: {Id})", msg.Id);
                    await _db.ExecuteAsync(
                        @"UPDATE message_outbox
                          SET retry_count = retry_count + 1,
                              status = CASE WHEN retry_count >= 4 THEN 'failed' ELSE 'pending' END
                          WHERE id = @Id",
                        new { msg.Id });
                }
            }

            await Task.Delay(1000, ct);
        }
    }
}
```

---

## 五、Event Sourcing：游戏状态重建

### 核心思想

```
Event Sourcing ≠ 只存当前状态
  当前状态：UPDATE player SET level = level + 1 WHERE id = 123
  Event Sourcing：INSERT INTO events VALUES ('player_level_up', {playerId:123})
  
  重建：读取所有 events 并按顺序重放得到当前状态

游戏中的优势：
  - 完整历史：可以回放到任意时间点
  - 调试能力：知道每一步状态变化的原因
  - 审计友好：不可变的事件日志
  - 时空回溯：查 BUG 时可以精确重现玩家状态
```

```csharp
// 游戏事件溯源框架
public class GameEventStore
{
    private readonly IDbConnection _db;

    // 追加事件（不可变，只追加）
    public async Task AppendEvent(string streamId, long expectedVersion, GameEventData evt)
    {
        // 乐观并发控制：检查版本号
        int affected = await _db.ExecuteAsync(
            @"INSERT INTO event_store (stream_id, version, event_type, data, created_at)
              SELECT @StreamId, @Version, @EventType, @Data, NOW()
              WHERE NOT EXISTS (
                  SELECT 1 FROM event_store
                  WHERE stream_id = @StreamId AND version >= @ExpectedVersion
              )",
            new
            {
                StreamId = streamId,
                Version = expectedVersion + 1,
                evt.EventType,
                Data = JsonSerializer.Serialize(evt),
                ExpectedVersion = expectedVersion
            });

        if (affected == 0)
            throw new ConcurrencyException($"版本冲突: stream={streamId}, expected={expectedVersion}");
    }

    // 读取事件流（重建状态）
    public async Task<List<GameEventData>> ReadEvents(string streamId, int fromVersion = 0)
    {
        var events = await _db.QueryAsync<EventRecord>(
            @"SELECT * FROM event_store
              WHERE stream_id = @StreamId AND version > @FromVersion
              ORDER BY version",
            new { StreamId = streamId, FromVersion = fromVersion });

        return events.Select(e => JsonSerializer.Deserialize<GameEventData>(e.Data)).ToList();
    }
}

// 状态重建器
public class PlayerStateRebuilder
{
    private readonly GameEventStore _eventStore;

    // 从事件流重建玩家状态
    public async Task<PlayerState> RebuildPlayerState(long playerId)
    {
        string streamId = $"player-{playerId}";
        var events = await _eventStore.ReadEvents(streamId);

        var state = new PlayerState(); // 初始状态

        foreach (var evt in events)
        {
            // 根据事件类型更新状态
            state.Apply(evt);
        }

        return state;
    }
}

// 快照优化（防止事件太多重建太慢）
public class SnapshotService
{
    // 每 100 个事件创建一个快照
    // 重建时从最近的快照开始，只重放之后的少量事件
    public async Task<PlayerState> RebuildWithSnapshot(long playerId)
    {
        var snapshot = await GetLatestSnapshot(playerId);

        if (snapshot != null)
        {
            // 从快照版本之后的事件开始重放
            var events = await _eventStore.ReadEvents($"player-{playerId}", snapshot.Version);
            foreach (var evt in events)
                snapshot.State.Apply(evt);
            return snapshot.State;
        }

        return await RebuildPlayerState(playerId);
    }
}
```

---

## 六、消息顺序与 Schema 进化

### 分区键策略

```csharp
// 不同消息类型的分区键选择
public class PartitionKeyStrategy
{
    // 类型 1：玩家操作（必须有顺序）
    // 分区分键 = playerId → 同一玩家所有操作有序
    public string PlayerOperationKey(long playerId)
        => playerId.ToString();

    // 类型 2：服务器事件（同一服务器的事件有序）
    public string ServerEventKey(int serverId)
        => $"server:{serverId}";

    // 类型 3：全局广播（不需要顺序）
    // 不设 Key，Kafka 随机分配

    // 类型 4：公会事件（同公会消息有序，跨公会无关）
    public string GuildEventKey(long guildId)
        => $"guild:{guildId}";

    // 注意事项：
    // 1. 分区键要有足够的基数（避免所有消息打到一个分区）
    // 2. 分区数一旦设置一般不能减少（只能增加）
    // 3. 消费者数 ≤ 分区数（否则有消费者闲置）
}
```

### Schema 进化（Protobuf 兼容性）

```protobuf
syntax = "proto3";

// 原始版本 v1
message PlayerEvent {
    int64 player_id = 1;
    string event_type = 2;
    int32 param_int = 3;       // 后来发现不够用
}

// 进化后的版本 v2（兼容 v1）
message PlayerEventV2 {
    int64 player_id = 1;       // 字段号不变
    string event_type = 2;     // 字段号不变
    // 删除了 param_int (3 号字段不再使用，但保留不用)
    int64 param_value = 4;     // 新字段用新号码
    string param_string = 5;   // 另一新字段
    map<string, string> extras = 6; // 扩展字段

    // Protobuf 进化规则：
    // ✅ 新增字段用新号码（不要重用旧号码）
    // ✅ 可以删除字段但保留号码（用 reserved）
    // ✅ 可以修改字段类型（但必须兼容：int32→int64 兼容）
    // ❌ 不要修改字段号码
    // ❌ 不要删除一个字段后又用同一号码添加新字段
}
```

---

## 七、背压处理

```csharp
// 有界消费者 + 背压控制
public class BackpressureConsumer
{
    private readonly Channel<GameEvent> _inboundChannel;
    private readonly SemaphoreSlim _concurrencyLimit;

    public BackpressureConsumer(int maxQueueSize = 10000, int maxConcurrency = 100)
    {
        // 控制等待处理的事件数
        _inboundChannel = Channel.CreateBounded<GameEvent>(
            new BoundedChannelOptions(maxQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait,  // 队列满则等待（反压到上游）
            });

        // 控制并发处理数
        _concurrencyLimit = new SemaphoreSlim(maxConcurrency);
    }

    // 消息队列消费者（从 RabbitMQ/Kafka 读取）
    public async Task ConsumeFromBroker(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // 从 broker 读取一个批次
            var batch = await ReadBatchFromBroker();

            foreach (var evt in batch)
            {
                // 写入通道（如果队列满则等待）
                // 这个等待会减慢从 broker 读取的速度
                await _inboundChannel.Writer.WriteAsync(evt, ct);
            }
        }
    }

    // 实际处理器
    public async Task ProcessEvents(CancellationToken ct)
    {
        await foreach (var evt in _inboundChannel.Reader.ReadAllAsync(ct))
        {
            // 用 SemaphoreSlim 控制并发数
            await _concurrencyLimit.WaitAsync(ct);

            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleEvent(evt);
                }
                finally
                {
                    _concurrencyLimit.Release();
                }
            }, ct);
        }
    }

    // 监控背压状态
    public BackpressureStatus GetStatus()
    {
        return new BackpressureStatus
        {
            QueueLength = _inboundChannel.Reader.Count,
            CurrentConcurrency = _concurrencyLimit.CurrentCount,
            IsBackpressured = _inboundChannel.Reader.Count > 5000
        };
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 分区键 | 按 playerId 分区保证玩家事件有序 |
| 重平衡 | 消费者增减时触发分区重新分配 + 状态迁移 |
| Quorum 队列 | RabbitMQ 基于 Raft 的高可用队列 |
| 死信队列 | 失败消息的自动重试 + 最终归档 |
| Outbox | 同事务写 outbox 表解决双写一致性 |
| Event Sourcing | 事件日志不可变，支持状态重建和回溯 |
| Schema 进化 | Protobuf 兼容性规则：不重用字段号 |
| 背压 | 有界队列 + 并发控制反压到消息源 |
