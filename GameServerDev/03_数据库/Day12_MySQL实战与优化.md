# Day 12：MySQL 实战与优化

## 一、游戏服务器表结构设计

### 玩家表

```sql
CREATE TABLE `players` (
    `id`            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '玩家ID',
    `account_id`    BIGINT UNSIGNED NOT NULL COMMENT '账号ID',
    `server_id`     INT UNSIGNED NOT NULL DEFAULT 1 COMMENT '服务器ID',
    `name`          VARCHAR(32) NOT NULL COMMENT '角色名',
    `level`         INT UNSIGNED NOT NULL DEFAULT 1 COMMENT '等级',
    `exp`           BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '经验',
    `vip_level`     INT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'VIP等级',
    `gold`          BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '金币',
    `diamond`       BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '钻石',
    `last_login_at` DATETIME DEFAULT NULL COMMENT '最后登录时间',
    `last_logout_at` DATETIME DEFAULT NULL COMMENT '最后登出时间',
    `created_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_account_server` (`account_id`, `server_id`),
    UNIQUE KEY `uk_name_server` (`name`, `server_id`),
    KEY `idx_level` (`level`),
    KEY `idx_last_login` (`last_login_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='玩家表';
```

### 道具表

```sql
CREATE TABLE `inventory` (
    `id`          BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `player_id`   BIGINT UNSIGNED NOT NULL COMMENT '玩家ID',
    `item_id`     INT UNSIGNED NOT NULL COMMENT '道具模板ID',
    `count`       INT UNSIGNED NOT NULL DEFAULT 1 COMMENT '数量',
    `is_equipped` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否装备',
    `expire_at`   DATETIME DEFAULT NULL COMMENT '过期时间',
    `created_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_player_id` (`player_id`),
    KEY `idx_player_item` (`player_id`, `item_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='道具表';
```

### 任务表

```sql
CREATE TABLE `quests` (
    `id`          BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `player_id`   BIGINT UNSIGNED NOT NULL,
    `quest_id`    INT UNSIGNED NOT NULL COMMENT '任务模板ID',
    `status`      TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0进行中 1已完成 2已领取',
    `progress`    INT UNSIGNED NOT NULL DEFAULT 0 COMMENT '当前进度',
    `target`      INT UNSIGNED NOT NULL COMMENT '目标值',
    `accepted_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `completed_at` DATETIME DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_player_status` (`player_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='任务表';
```

---

## 二、查询优化

### EXPLAIN 解读

```sql
EXPLAIN SELECT p.name, p.level, COUNT(i.id) as item_count
FROM players p
LEFT JOIN inventory i ON p.id = i.player_id
WHERE p.level > 10 AND p.vip_level > 0
GROUP BY p.id
ORDER BY p.level DESC
LIMIT 100\G

-- 输出解读:
-- id: 1
-- select_type: SIMPLE
-- table: p
-- type: range              ← 范围扫描（不是全表）
-- possible_keys: idx_level
-- key: idx_level            ← 实际使用的索引
-- key_len: 4
-- ref: NULL
-- rows: 50000              ← 预估扫描行数
-- filtered: 33.33          ← 过滤后剩余百分比
-- Extra: Using where; Using index; Using temporary; Using filesort
--        ↑ 回表查询        ↑ 临时表          ↑ 文件排序
```

### 优化目标

```sql
-- 核心目标：type 达到 range/ref/eq_ref，避免 ALL (全表扫描)
-- Extra 避免：Using temporary, Using filesort, Using join buffer

-- 1. 查询具体行时用主键或唯一索引
EXPLAIN SELECT * FROM players WHERE id = 1001;
-- type: const (最快)

-- 2. 范围查询用索引
EXPLAIN SELECT * FROM players WHERE level BETWEEN 10 AND 20;
-- type: range
-- 如果 level 没索引 → type: ALL (100 万行全扫)

-- 3. 排序用索引（避免 filesort）
EXPLAIN SELECT * FROM players ORDER BY level DESC LIMIT 10;
-- 如果 level 有索引 → Using index
-- 如果 level 没索引 → Using filesort (内存或磁盘排序)

-- 4. 覆盖索引（不回表）
EXPLAIN SELECT level, name FROM players WHERE level = 10;
-- Extra: Using index (表示在索引树上就完成了)
```

### 常见慢查询优化

```sql

-- 场景 1：分页偏移大
-- 坏：
SELECT * FROM players ORDER BY id LIMIT 100000, 20;
-- 扫描 100020 行再丢弃前 100000 行

-- 好：
SELECT * FROM players WHERE id > 100000 ORDER BY id LIMIT 20;
-- 或记录上次最后 id
SELECT * FROM players WHERE id > :last_id ORDER BY id LIMIT 20;

-- 场景 2：COUNT 大表
-- 坏：
SELECT COUNT(*) FROM players; -- InnoDB 要全表扫描！(MyISAM 维护了行数)

-- 好：用 EXPLAIN 的行数近似值，或 Redis 维护计数

-- 场景 3：模糊查询
-- 坏：
SELECT * FROM players WHERE name LIKE '%张%'; -- 无法使用索引

-- 好：
SELECT * FROM players WHERE name LIKE '张%'; -- 前缀匹配可用索引
-- 或引入 Elasticsearch 做全文搜索

-- 场景 4：大量 IN 查询
-- 坏：
SELECT * FROM inventory WHERE player_id IN (1,2,3,...,10000);

-- 好：分拆多次查询或用临时表
CREATE TEMPORARY TABLE tmp_ids (id BIGINT PRIMARY KEY);
INSERT INTO tmp_ids VALUES ...;
SELECT i.* FROM inventory i JOIN tmp_ids t ON i.player_id = t.id;
```

---

## 三、连接池

### 为什么需要连接池

```csharp
// 每次新建连接需要 TCP 三次握手 + MySQL 认证
var conn = new MySqlConnection(connectionString); // ~10ms
await conn.OpenAsync();                             // ~20ms
// 总计约 30ms 开销

// 连接池复用已建立的连接，省掉这 30ms
// 在每秒处理千次请求的服务器上，30ms * 1000 = 30s 的差距
```

### 连接池配置

```csharp
// 连接字符串中的连接池参数
var connectionString = "Server=mysql;Port=3306;Database=game_db;" +
    "User=root;Password=root123;" +
    "Pooling=true;" +                          // 启用连接池（默认 true）
    "MinimumPoolSize=5;" +                     // 最小连接数（预热）
    "MaximumPoolSize=100;" +                   // 最大连接数（防止打爆 DB）
    "ConnectionIdleTimeout=300;" +             // 空闲超时 5 分钟
    "ConnectionLifeTime=1800;" +               // 连接最大生命周期 30 分钟
    "ConnectionReset=true;" +                  // 复用前重置
    "Load Balance=RoundRobin;";                // 读写分离负载均衡

// 使用 Dapper 时自动使用连接池
using var conn = new MySqlConnection(connectionString);
// conn 来自连接池（或新建后归还到池）
```

### 连接池管理

```csharp
class DbConnectionPool
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _semaphore;
    private int _poolSize;

    public DbConnectionPool(string connectionString, int poolSize = 100)
    {
        _connectionString = connectionString;
        _poolSize = poolSize;
        _semaphore = new SemaphoreSlim(poolSize);

        // 预热连接
        PreWarm(Math.Min(poolSize, 10));
    }

    private void PreWarm(int count)
    {
        var tasks = Enumerable.Range(0, count).Select(_ =>
        {
            return Task.Run(async () =>
            {
                var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await conn.CloseAsync(); // 归还到 MySqlConnectionPool
            });
        });
        Task.WhenAll(tasks).Wait();
    }

    public async Task<T> ExecuteAsync<T>(Func<MySqlConnection, Task<T>> action)
    {
        await _semaphore.WaitAsync();
        using var conn = new MySqlConnection(_connectionString);
        try
        {
            await conn.OpenAsync();
            return await action(conn);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // 监控
    public PoolStats GetStats()
    {
        return new PoolStats
        {
            Available = _semaphore.CurrentCount,
            MaxPoolSize = _poolSize,
            WaitingCount = _poolSize - _semaphore.CurrentCount
        };
    }
}

public class PoolStats
{
    public int Available { get; set; }
    public int MaxPoolSize { get; set; }
    public int WaitingCount { get; set; }
}
```

---

## 四、分库分表

### 为什么要分

```sql
-- 单表 1 亿行，查询已经很慢（索引 4~5 层，IO 压力大）
-- 单库 10 万 QPS，DB 到达瓶颈

-- 解决方案：
-- 1. 分表：把大表拆成多个小表（垂直/水平）
-- 2. 分库：把数据分散到多个数据库实例
```

### 水平分表（按玩家 ID）

```sql
-- 分表策略：player_id % 16，分成 16 张表
-- players_0, players_1, ..., players_15

-- 应用层路由
public class ShardingManager
{
    private const int ShardCount = 16;

    public static string GetPlayerTable(long playerId)
    {
        int shard = (int)(playerId % ShardCount);
        return $"players_{shard}";
    }

    public static string GetInventoryTable(long playerId)
    {
        int shard = (int)(playerId % ShardCount);
        return $"inventory_{shard}";
    }

    public static async Task<PlayerData> GetPlayer(MySqlConnection conn, long playerId)
    {
        string table = GetPlayerTable(playerId);
        return await conn.QueryFirstOrDefaultAsync<PlayerData>(
            $"SELECT * FROM {table} WHERE id = @Id",
            new { Id = playerId });
    }
}

-- 创建分表
CREATE TABLE players_0 LIKE players_template;
CREATE TABLE players_1 LIKE players_template;
-- ... 16 张表

-- 查询路由到对应表
SELECT * FROM players_7 WHERE id = 1007;  -- 1007 % 16 = 15? 不对是 1007
-- player_id = 1007, 1007 % 16 = 15 → players_15
```

### 垂直分库（按功能模块）

```sql
-- 玩家相关数据 → PlayerDB (players, inventory, quests)
-- 社交相关数据 → SocialDB (friends, guilds, chat)
-- 日志相关数据 → LogDB (battle_logs, item_logs, login_logs)

-- 优点：
-- - 各库独立扩展
-- - 慢查询不影响其他模块
-- - 可针对不同类型选择不同存储引擎
```

### 分库中间件

```csharp
class ShardedDbManager
{
    // 按玩家 ID 路由到不同数据库连接
    private readonly MySqlConnection[] _connections;

    public ShardedDbManager(string[] connectionStrings)
    {
        _connections = connectionStrings
            .Select(cs => new MySqlConnection(cs))
            .ToArray();
    }

    private MySqlConnection GetConnection(long playerId)
    {
        int dbIndex = (int)(playerId % _connections.Length);
        return _connections[dbIndex];
    }

    public async Task<PlayerData> GetPlayer(long playerId)
    {
        using var conn = GetConnection(playerId);
        await conn.OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<PlayerData>(
            "SELECT * FROM players WHERE id = @Id",
            new { Id = playerId });
    }
}
```

---

## 五、主从复制与读写分离

### 架构

```
         ┌──────────┐
         │   Client  │
         └─────┬─────┘
               │
         ┌─────▼─────┐
         │   Proxy   │ (读写分离代理 / MaxScale / ProxySQL)
         └─────┬─────┘
               │
    ┌──────────┼──────────┐
    │          │          │
┌───▼───┐  ┌───▼───┐  ┌───▼───┐
│ Master │  │ Slave │  │ Slave │
│ 写     │  │ 读    │  │ 读    │
└────────┘  └───────┘  └───────┘
   │
   └──→ Binlog → Slave 1 & Slave 2
```

### 代码实现

```csharp
class ReadWriteSplitting
{
    private readonly string _masterConn;
    private readonly string[] _slaveConns;
    private int _roundRobin = 0;

    // 写操作 → Master
    public async Task<int> ExecuteWriteAsync(string sql, object param = null)
    {
        using var conn = new MySqlConnection(_masterConn);
        await conn.OpenAsync();
        return await conn.ExecuteAsync(sql, param);
    }

    // 读操作 → Slave（轮询负载均衡）
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
    {
        string connStr = GetSlaveConnection();
        using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();
        return await conn.QueryAsync<T>(sql, param);
    }

    private string GetSlaveConnection()
    {
        int idx = Interlocked.Increment(ref _roundRobin) % _slaveConns.Length;
        return _slaveConns[Math.Abs(idx)];
    }
}
```

### 主从延迟问题

```sql
-- 问题：写入 Master 后立即读从库，可能读不到
-- 解决方案：

-- 1. 强制读主库（关键数据）
if (needImmediate) {
    readFromMaster();
}

-- 2. 写后等待
await Task.Delay(100); // 等主从同步

-- 3. 检查 Seconds_Behind_Master
SHOW SLAVE STATUS\G
-- Seconds_Behind_Master: 0  (如果不为 0，说明有延迟)
```

---

## 六、SQL 注入防范

```csharp
// ❌ 危险：字符串拼接
string sql = $"SELECT * FROM players WHERE name = '{userInput}'";
// userInput = "'; DROP TABLE players; --"  → 灾难！

// ✅ 安全：参数化查询
using var cmd = new MySqlCommand(
    "SELECT * FROM players WHERE name = @name", conn);
cmd.Parameters.AddWithValue("@name", userInput);

// Dapper 自动参数化
await conn.QueryAsync<PlayerData>(
    "SELECT * FROM players WHERE name = @Name",
    new { Name = userInput });

// 原则：永远不要拼接 SQL！
```

---

## 七、练习

1. **建表与索引**：设计一个完整的游戏数据库（5 张表以上），加上所有必要的索引
2. **EXPLAIN 分析**：写 5 个不同的查询，用 EXPLAIN 分析是否用到索引
3. **慢查询优化**：给出一句慢查询，分析原因并优化
4. **连接池实现**：实现一个简单的连接池（可以不用真正连接 MySQL）
5. **分表路由**：实现按玩家 ID 路由的分表逻辑

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| EXPLAIN | 查询执行计划，优化 SQL 的第一工具 |
| type | 访问类型：const > ref > range > index > ALL |
| Extra | Using filesort/temporary 是坏信号 |
| 连接池 | 复用 TCP 连接，避免三次握手开销 |
| 水平分表 | 按 ID 拆分大表，每个分表数据量可控 |
| 读写分离 | Master 写 Slave 读，分散 DB 压力 |
| 参数化查询 | 防止 SQL 注入的唯一正确方法 |
