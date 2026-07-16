# Day 11：MySQL 核心原理

## 一、为什么游戏服务器用 MySQL

| 需求 | MySQL 解决方案 |
|------|--------------|
| 持久化 | 磁盘存储，掉电不丢 |
| 关系数据 | 玩家、道具、任务之间的关系 |
| 事务 | 保证数据一致性（扣钱同时加道具） |
| 查询 | 灵活的 SQL，支持复杂筛选 |
| 成熟度 | 20+ 年生产验证，社区庞大 |

**游戏服务器典型用法**：MySQL 存最终数据 + Redis 做缓存。

---

## 二、存储引擎

### InnoDB vs MyISAM

| 特性 | InnoDB | MyISAM |
|------|--------|--------|
| 事务 | ✅ (ACID) | ❌ |
| 外键 | ✅ | ❌ |
| 行锁 | ✅ (MVCC) | ❌ (表锁) |
| 崩溃恢复 | ✅ (Redo Log) | ❌ |
| 全文索引 | ✅ (5.6+) | ✅ |
| 压缩 | ✅ (透明) | ✅ |
| 内存占用 | 较大 | 较小 |
| 速度 (读) | 略慢 | 快 |
| 速度 (写) | 快 (行锁) | 慢 (表锁) |

**结论**：游戏服务器**只用 InnoDB**。

---

## 三、B+ Tree 索引原理

### B+ Tree 结构

```
                  [50]
                 /    \
           [20, 30]    [70, 90]
          /    |   \    /   |   \
       [1-19] [21-29] [31-49] [51-69] [71-89] [91-+]
        (叶子节点，存放完整行数据)
```

### B+ Tree 特点

1. **非叶子节点只存索引**（key），不存数据
2. **叶子节点存完整行数据**（聚簇索引）
3. **叶子节点通过链表连接**（范围查询快）
4. **树高度通常 3~4 层**（百万级数据 3 次 IO）

### 为什么 B+ Tree 快

```sql
-- 表有 1000 万行，主键查询
SELECT * FROM players WHERE id = 9999999;

-- 假设每页 16KB，可存约 1000 个 key
-- 3 层 B+ Tree = 3 次磁盘 IO ≈ 30ms
-- 如果全表扫描 = 1000 万次 IO = 不可接受
```

### 索引类型

```sql
-- 聚簇索引 (Clustered Index)
-- InnoDB 自动为主键创建，叶子节点存整行数据
-- 一个表只能有一个聚簇索引

-- 辅助索引 (Secondary Index)
-- 叶子节点存主键值，需要回表查询
CREATE INDEX idx_level ON players(level);
-- 查询流程：
-- 1. 在 idx_level 找到对应主键
-- 2. 用主键回聚簇索引查完整数据

-- 复合索引 (Composite Index)
CREATE INDEX idx_level_exp ON players(level, exp);
-- 最左前缀原则：WHERE level=? 可用，WHERE exp=? 不可用

-- 覆盖索引 (Covering Index)
-- 索引包含所有需要查询的字段，不需要回表
CREATE INDEX idx_level_exp_name ON players(level, exp, name);
SELECT name FROM players WHERE level = 10; -- 覆盖索引，不回表
```

---

## 四、事务与隔离级别

### ACID

| 特性 | 含义 | MySQL 实现 |
|------|------|-----------|
| Atomicity | 全部成功或全部回滚 | Undo Log |
| Consistency | 数据约束一致 | 约束+应用逻辑 |
| Isolation | 事务之间隔离 | MVCC + 锁 |
| Durability | 提交后永久保存 | Redo Log |

### 隔离级别

```sql
-- 4 种隔离级别（从低到高）
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;  -- 读未提交（有脏读）
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;    -- 读已提交（默认，PG, SQL Server）
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;   -- 可重复读（MySQL 默认）
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;       -- 串行化（最低并发）
```

| 隔离级别 | 脏读 | 不可重复读 | 幻读 |
|---------|------|-----------|------|
| 读未提交 | 可能 | 可能 | 可能 |
| 读已提交 | 安全 | 可能 | 可能 |
| 可重复读 | 安全 | 安全 | MySQL 可重复读+间隙锁=安全 |
| 串行化 | 安全 | 安全 | 安全 |

### MVCC (Multi-Version Concurrency Control)

```sql
-- InnoDB 通过 MVCC 实现高并发读
-- 每行记录有隐藏列：
--   DB_TRX_ID: 最后修改的事务 ID
--   DB_ROLL_PTR: Undo Log 指针（用于回滚和快照）

-- READ VIEW 结构：
--   creator_trx_id: 创建该 Read View 的事务 ID
--   m_ids: 活跃事务 ID 列表
--   min_trx_id: m_ids 最小值
--   max_trx_id: m_ids 最大值 + 1

-- 判断可见性：
--   trx_id < min_trx_id  → 已提交，可见
--   trx_id >= max_trx_id → 未来事务，不可见
--   trx_id in m_ids      → 未提交，不可见（RC 每次新快照）
```

---

## 五、锁机制

### 行锁

```sql
-- InnoDB 使用行锁，不是表锁
-- 行锁是基于索引的！

-- 共享锁 (S Lock)：允许其他事务读
SELECT * FROM players WHERE id = 1 LOCK IN SHARE MODE;

-- 排他锁 (X Lock)：不允许其他事务读或写
SELECT * FROM players WHERE id = 1 FOR UPDATE;
UPDATE players SET level = 10 WHERE id = 1; -- 自动 X 锁
```

### 间隙锁 (Gap Lock)

```sql
-- MySQL 可重复读下，为了防止幻读，引入了间隙锁
-- 锁住索引记录之间的间隙，不让插入新记录

-- 表数据：id 有 1, 3, 5, 7
-- 间隙锁范围: (-∞,1), [1,3), [3,5), [5,7), [7,+∞)

-- 事务 A：
BEGIN;
SELECT * FROM players WHERE level BETWEEN 10 AND 20 FOR UPDATE;
-- 锁住 level=10 和 level=20 之间的所有间隙

-- 事务 B：
INSERT INTO players (level) VALUES (15);
-- ❌ 阻塞！因为间隙锁阻止了插入

-- 注意：行锁+间隙锁 = Next-Key Lock
```

### 死锁

```sql
-- 死锁示例
-- 事务 A：
UPDATE players SET exp = 100 WHERE id = 1;  -- 锁住 id=1
UPDATE players SET exp = 200 WHERE id = 2;  -- 等待 id=2 锁 → 死锁！

-- 事务 B：
UPDATE players SET exp = 300 WHERE id = 2;  -- 锁住 id=2
UPDATE players SET exp = 400 WHERE id = 1;  -- 等待 id=1 锁 → 死锁！

-- MySQL 会自动检测死锁，回滚一个事务
-- 查看死锁日志：SHOW ENGINE INNODB STATUS\G

-- 预防死锁：
-- 1. 按固定顺序访问资源（如按 id 升序更新）
-- 2. 缩短事务时间
-- 3. 使用 READ COMMITTED 减少间隙锁
```

---

## 六、SQL 与 ORM

### ADO.NET 基础

```csharp
using System.Data;
using MySqlConnector; // dotnet add package MySqlConnector

class PlayerRepository
{
    private readonly string _connectionString;

    public PlayerRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // 查询
    public async Task<PlayerData> GetPlayer(long playerId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(
            "SELECT id, name, level, exp FROM players WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", playerId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PlayerData
            {
                PlayerId = reader.GetInt64("id"),
                Name = reader.GetString("name"),
                Level = reader.GetInt32("level"),
                Exp = reader.GetInt64("exp")
            };
        }
        return null;
    }

    // 写入（事务）
    public async Task<bool> UpdatePlayerWithItems(long playerId,
        int deltaExp, List<int> newItems)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // 更新经验
            using var cmd1 = new MySqlCommand(
                "UPDATE players SET exp = exp + @delta WHERE id = @id",
                conn, tx);
            cmd1.Parameters.AddWithValue("@delta", deltaExp);
            cmd1.Parameters.AddWithValue("@id", playerId);
            await cmd1.ExecuteNonQueryAsync();

            // 加道具
            foreach (var itemId in newItems)
            {
                using var cmd2 = new MySqlCommand(
                    "INSERT INTO inventory (player_id, item_id) VALUES (@pid, @iid)",
                    conn, tx);
                cmd2.Parameters.AddWithValue("@pid", playerId);
                cmd2.Parameters.AddWithValue("@iid", itemId);
                await cmd2.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }
}
```

### Dapper (轻量 ORM)

```csharp
// dotnet add package Dapper
using Dapper;

class PlayerRepositoryDapper
{
    private readonly string _connectionString;

    // 查询单条
    public async Task<PlayerData> GetPlayer(long id)
    {
        using var conn = new MySqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<PlayerData>(
            "SELECT * FROM players WHERE id = @Id", new { Id = id });
    }

    // 查询列表
    public async Task<IEnumerable<PlayerData>> GetTopPlayers(int limit)
    {
        using var conn = new MySqlConnection(_connectionString);
        return await conn.QueryAsync<PlayerData>(
            "SELECT * FROM players ORDER BY level DESC LIMIT @Limit",
            new { Limit = limit });
    }

    // 批量插入
    public async Task<int> BatchInsertItems(IEnumerable<InventoryItem> items)
    {
        using var conn = new MySqlConnection(_connectionString);
        return await conn.ExecuteAsync(
            "INSERT INTO inventory (player_id, item_id, count) " +
            "VALUES (@PlayerId, @ItemId, @Count)", items);
    }

    // 事务
    public async Task<bool> SavePlayerWithItems(PlayerData player, List<int> items)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            await conn.ExecuteAsync(
                "UPDATE players SET exp = @Exp WHERE id = @Id",
                player, tx);

            await conn.ExecuteAsync(
                "INSERT INTO inventory (player_id, item_id) VALUES (@Pid, @Iid)",
                items.Select(i => new { Pid = player.PlayerId, Iid = i }), tx);

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }
}
```

---

## 七、对比 C++ MySQL

```cpp
// C++ 用 mysql-connector-cpp
#include <mysqlx/xdevapi.h>

using namespace mysqlx;

Session session("localhost", 3306, "root", "password");
Schema db = session.getSchema("game_db");
Table players = db.getTable("players");

// 查询
RowResult result = players.select("id", "name", "level")
    .where("level > :level")
    .bind("level", 10)
    .execute();

Row row;
while ((row = result.fetchOne())) {
    int id = row[0];
    std::string name = row[1];
    int level = row[2];
}

// 插入
players.insert("id", "name", "level")
    .values(1001, "Alice", 10)
    .execute();
```

---

## 八、练习

1. **建表**：设计玩家表、道具表、任务表，加上合适的索引
2. **SQL 练习**：写 10 条查询（JOIN、子查询、聚合、分组）
3. **事务测试**：开两个 MySQL 终端，测试不同隔离级别下的脏读/不可重复读/幻读
4. **EXPLAIN 分析**：对一个查询用 EXPLAIN，观察 type/key/rows/Extra
5. **死锁制造**：制造一个死锁并观察 MySQL 的处理

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| InnoDB | MySQL 唯一推荐存储引擎，支持事务和行锁 |
| B+ Tree | 索引的数据结构，3~4 层满足千万级查询 |
| 聚簇索引 | 主键索引，叶子存完整行数据 |
| MVCC | 多版本并发控制，实现高并发读不阻塞写 |
| 间隙锁 | MySQL 可重复读下防止幻读 |
| 行锁 | InnoDB 基于索引的行级锁 |
| Dapper | 轻量 ORM，比 ADO.NET 好用 10 倍 |
