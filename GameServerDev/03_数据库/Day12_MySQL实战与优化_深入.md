# Day 12：MySQL 实战与优化 — 进阶深入

> 目标读者：正在为游戏服务器优化 MySQL 性能的开发者
> 前置知识：EXPLAIN 基本用法、索引类型、读写分离概念

---

## 一、EXPLAIN FORMAT=JSON 深度分析

### 1.1 JSON 格式的优势

```sql
-- 基础 EXPLAIN
EXPLAIN SELECT * FROM players WHERE level > 50;

-- JSON 格式（包含更详细信息）
EXPLAIN FORMAT=JSON
SELECT p.id, p.name, i.item_name
FROM players p
JOIN items i ON p.id = i.player_id
WHERE p.server_id = 1 AND p.level > 50\G
```

### 1.2 关键字段解读

```json
{
  "query_block": {
    "select_id": 1,
    "cost_info": {
      "query_cost": "2584.67"        ← 总代价（磁盘 IO + CPU）
    },
    "table": {
      "table_name": "p",
      "access_type": "ref",           ← 索引访问类型
      "possible_keys": ["idx_server_level", "idx_server_id"],
      "key": "idx_server_level",      ← 实际使用的索引
      "used_key_parts": ["server_id"], ← 使用的索引前缀
      "key_length": "4",
      "rows_examined_per_scan": 1250, ← InnoDB 估算的扫描行数
      "rows_produced_per_join": 125,
      "filtered": "10.00",            ← 满足 WHERE 条件的百分比
      "index_condition": "((`p`.`server_id` = 1) and (`p`.`level` > 50))", ← ICP
      "cost_info": {
        "read_cost": "125.00",        ← IO 代价
        "eval_cost": "25.00",         ← 计算代价
        "prefix_cost": "150.00",      ← 累计代价
        "data_read_per_join": "2M"    ← 读取数据量
      },
      "used_columns": ["id", "name", "server_id", "level"],
      "attached_condition": "(`p`.`level` > 50)"  ← 剩余的 WHERE 条件
    }
  }
}
```

### 1.3 关注的关键指标

```
1. query_cost        ← 总代价，比较不同执行计划的成本
2. rows_examined     ← 估计扫描行数（越小越好）
3. filtered          ← 过滤比例（100% 表示全部满足，越低说明索引选择性越好）
4. read_cost         ← IO 代价（磁盘读取开销）
5. using_index       ← 是否使用覆盖索引
6. using_where       ← 是否使用了 WHERE 过滤
7. using_temporary   ← 是否使用临时表（⚠ 危险信号）
8. using_filesort    ← 文件排序（⚠ 危险信号）
```

### 1.4 慢查询定位流程

```sql
-- 1. 开启慢查询日志
SET GLOBAL slow_query_log = ON;
SET GLOBAL long_query_time = 0.1;        -- 100ms 以上记录
SET GLOBAL log_queries_not_using_indexes = ON;

-- 2. 分析慢查询
mysqldumpslow -s t -t 10 /var/log/mysql/slow.log

-- 3. 对高频慢查询抓取 JSON 执行计划
EXPLAIN FORMAT=JSON SELECT ...\G

-- 4. 关注 "rows_examined" 和 "filtered"
-- 如果 rows_examined 很大但 filtered 很小 → 索引选择性不好
-- 如果使用了 "Using temporary" → 需要优化 ORDER BY 或 GROUP BY 的索引
```

---

## 二、覆盖索引与索引条件下推（ICP）

### 2.1 覆盖索引（Covering Index）

覆盖索引指一个索引包含查询需要的所有列，不需要回表访问聚簇索引。

```
无覆盖索引的查询流程：
  idx_server_level 找到主键 id → 回表读聚簇索引 → 获取所有列
                                  ↓
                             一次回表 = 一次随机 IO

有覆盖索引的查询流程：
  idx_server_level 包含 (server_id, level, name)
  查询只需要这 3 列 → 直接返回 → 零回表
```

```sql
-- 创建覆盖索引
CREATE INDEX idx_cover ON players (server_id, level, name);

-- 这个查询可以用覆盖索引（全部列在索引中）
EXPLAIN
SELECT server_id, level, name FROM players
WHERE server_id = 1 AND level BETWEEN 10 AND 20;
-- Extra: Using index  ← 表示使用覆盖索引

-- 这个查询需要回表（name 列不在索引中，需要 id 查询）
EXPLAIN
SELECT * FROM players
WHERE server_id = 1 AND level BETWEEN 10 AND 20;
-- Extra: NULL  ← 需要回表
```

### 2.2 索引条件下推（ICP，Index Condition Pushdown）

MySQL 5.6+ 支持：将 WHERE 条件中**可以使用索引判断的部分**下推到存储引擎层过滤。

```
无 ICP：
  存储引擎通过索引找出所有满足 server_id=1 的记录 → 返回给 Server 层
  → Server 层判断 level>50（需要回表检查）

有 ICP：
  存储引擎使用索引判断 server_id=1 AND level>50（在索引层面过滤）
  → 只返回满足条件的结果给 Server 层
```

```sql
-- 复合索引 (server_id, level)
CREATE INDEX idx_sl ON players (server_id, level);

-- ICP 生效的查询
EXPLAIN SELECT * FROM players WHERE server_id=1 AND level>50;
-- Extra: Using index condition  ← ICP 被使用

-- Extra 含义：
--   Using index           → 覆盖索引（无需回表）
--   Using index condition → ICP（需要回表，但在引擎层先过滤）
--   Using where           → Server 层过滤（最低效）
```

### 2.3 游戏开发中的索引策略

```sql
-- 玩家登录查询（最频繁）→ 覆盖索引
CREATE INDEX idx_login ON players (account_id, server_id, id, last_login_time);

-- 排行榜查询 → 覆盖索引
CREATE INDEX idx_rank ON players (server_id, level DESC, exp DESC, id);

-- 道具列表查询 → 减少回表
CREATE INDEX idx_items ON items (player_id, slot, item_id, quantity);
```

---

## 三、Query Cache（MySQL 8.0 已移除）与替代方案

### 3.1 Query Cache 为什么被移除

```
MySQL 8.0 之前的 Query Cache 问题：
  1. 全局互斥锁：每次查询都需要加锁检查缓存
  2. 表更新即失效：对表的任何修改都会清空该表的所有缓存
  3. 多核扩展性差：缓存争用随 CPU 核心数增加而恶化
  4. 收益有限：写多读少场景基本没有命中

基准测试显示：关闭 Query Cache 在大多数场景下有 10%-30% 的性能提升。
```

### 3.2 游戏场景的缓存层次

```
+---------------------+
|   客户端缓存         |  ← CDN、本地存储（配置表等静态数据）
+---------------------+
|   应用层缓存         |  ← Redis/Memcached（热点数据、排行榜）
+---------------------+
|   代理层缓存         |  ← ProxySQL、HAProxy（查询结果缓存）
+---------------------+
|   MySQL Buffer Pool |  ← InnoDB 自带的行缓存
+---------------------+
```

### 3.3 Proxy 层缓存方案（ProxySQL）

```sql
-- ProxySQL 配置查询缓存
INSERT INTO mysql_query_rules
(rule_id, active, match_pattern, cache_ttl, apply)
VALUES (10, 1, '^SELECT player_.*$', 60000, 1);
-- 匹配 SELECT player_* 的查询缓存 60 秒

-- 缓存的查询示例（全服公告等变化不频繁的数据）
SELECT * FROM system_config WHERE `key` = 'server_notice';
```

---

## 四、分区策略

### 4.1 RANGE 分区（按时间范围）

```sql
-- 按月份的日志表分区
CREATE TABLE `log_player_action` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `player_id` BIGINT NOT NULL,
  `action_type` SMALLINT NOT NULL,
  `log_time` DATETIME NOT NULL,
  `detail` JSON,
  PRIMARY KEY (`id`, `log_time`)  -- 分区列必须包含在主键中
) ENGINE=InnoDB
PARTITION BY RANGE (TO_DAYS(`log_time`)) (
  PARTITION p202606 VALUES LESS THAN (TO_DAYS('2026-07-01')),
  PARTITION p202607 VALUES LESS THAN (TO_DAYS('2026-08-01')),
  PARTITION p202608 VALUES LESS THAN (TO_DAYS('2026-09-01')),
  PARTITION p_future VALUES LESS THAN MAXVALUE
);

-- 查询时自动分区裁剪（Partition Pruning）
EXPLAIN SELECT * FROM log_player_action
WHERE log_time BETWEEN '2026-07-10' AND '2026-07-20';
-- 只扫描 p202607 分区
```

### 4.2 HASH 分区（按玩家 ID）

```sql
-- 按 player_id 的哈希分 16 个区
CREATE TABLE `player_items` (
  `player_id` BIGINT NOT NULL,
  `item_id` INT NOT NULL,
  `quantity` INT NOT NULL,
  `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_id`, `item_id`)
) ENGINE=InnoDB
PARTITION BY HASH(player_id) PARTITIONS 16;

-- 优点：数据均匀分布
-- 缺点：增加/减少分区需要重建表
```

### 4.3 KEY 分区（类似 HASH，但用 MySQL 内部哈希函数）

```sql
-- KEY 分区使用 MySQL 的 PASSWORD() 哈希函数
-- 比 HASH 分区更均匀
CREATE TABLE `friend_relations` (
  `player_id` BIGINT NOT NULL,
  `friend_id` BIGINT NOT NULL,
  `status` TINYINT NOT NULL DEFAULT 0,
  PRIMARY KEY (`player_id`, `friend_id`)
) ENGINE=InnoDB
PARTITION BY KEY(`player_id`) PARTITIONS 32;
```

### 4.4 分区 vs 分表选择

```
分区优势：
  - 对应用透明
  - 分区裁剪（Partition Pruning）能自动过滤
  - 删除历史分区非常快（DROP PARTITION）

分表优势：
  - 可以分布在不同物理磁盘/机器
  - 没有跨分区查询限制
  - 可以在应用层做更灵活的路由

游戏推荐：
  - 日志表 → 用分区（按月）
  - 玩家数据 → 用分表（按 player_id 哈希）
```

---

## 五、分片策略

### 5.1 垂直分片

```
垂直分片 → 按业务模块拆分表到不同的数据库

游戏场景：
  +------------------+  +------------------+  +------------------+
  | game_db_player   |  | game_db_guild    |  | game_db_mail     |
  |  players         |  |  guilds          |  |  mails           |
  |  player_items    |  |  guild_members   |  |  mail_attachments|
  |  player_quests   |  |  guild_wars      |  +------------------+
  +------------------+  +------------------+
```

### 5.2 水平分片（Sharding）

```sql
-- 分片键选择：player_id % shard_count

-- 路由层伪代码
function getShard(playerId, totalShards) {
    return playerId % totalShards;
}

-- 分片 0：game_db_shard_0
CREATE TABLE `player_items_0` ...;

-- 分片 1：game_db_shard_1
CREATE TABLE `player_items_1` ...;

-- 查询时必须带上 shard key
SELECT * FROM player_items WHERE player_id = ? AND item_id = ?;
-- ✓ 能定位到具体分片

SELECT * FROM player_items WHERE item_id = ?;
-- ✗ 不能定位分片，需要广播查询（这是分片的主要限制）
```

### 5.3 分片键选择原则

```
好的分片键：
  - 数据分布均匀（player_id 比 account_name 更均匀）
  - 大多数查询带分片键（分片后 95%+ 的查询可路由到单个分片）
  - 不可变（player_id 创建后不变）

不好的分片键：
  - server_id（服务器迁移时会变化）
  - guild_id（公会数据大小不均匀，热点公会可能超过单机容量）
  - timestamp（时间范围查询集中，新数据永远写最后一个分片）
```

### 5.4 跨分片查询

```sql
-- 场景：全服邮件、跨服排行榜、运营后台统计

-- 方案 1：汇总库
-- 所有分片的 MySQL -> Canal -> Kafka -> 汇总库（ClickHouse/MySQL）
-- 适合：全服统计、运营报表

-- 方案 2：广播查询（应用层并发查询）
-- 应用同时发 N 个 SQL 到所有分片，合并结果
-- 适合：跨服排行榜（在应用层排序归并）

-- 方案 3：中间件方案（ShardingSphere/MyCAT）
-- 透明路由，但增加复杂度
```

---

## 六、Online DDL 与 pt-online-schema-change

### 6.1 MySQL Online DDL

```sql
-- MySQL 5.6+ 支持 Online DDL，但不同操作的锁策略不同

-- 允许并发读写（推荐）
ALTER TABLE players ADD COLUMN vip_level INT DEFAULT 0,
  ALGORITHM=INPLACE, LOCK=NONE;

-- 允许并发查询，阻塞写入（次选）
ALTER TABLE players MODIFY COLUMN name VARCHAR(128),
  ALGORITHM=INPLACE, LOCK=SHARED;

-- 阻塞所有并发操作（不推荐，除非小表）
ALTER TABLE players DROP PRIMARY KEY,
  ALGORITHM=COPY, LOCK=EXCLUSIVE;
```

### 6.2 各类操作的锁级别

```
操作类型                   | INPLACE | COPY | 并发DML
---------------------------+---------+------+---------
ADD COLUMN（非自增）        |   ✓    |  ✓   |   ✓
DROP COLUMN                |   ✓    |  ✓   |   ✓
ADD INDEX                  |   ✓    |  ✓   |   ✓
DROP INDEX                 |   ✓    |  ✓   |   ✓
MODIFY COLUMN（改变类型）   |   ✗    |  ✓   |   ✗
RENAME COLUMN              |   ✓    |  ✓   |   ✓
ADD FOREIGN KEY            |   ✓    |  ✓   |   ✓
CHANGE COLUMN（重命名）     |   ✗    |  ✓   |   ✗
ADD PRIMARY KEY            |   ✓    |  ✓   |   ✓
OPTIMIZE TABLE             |   ✓    |  ✓   |   ✗
```

### 6.3 pt-online-schema-change

```bash
# Percona Toolkit 的核心工具：无锁修改表结构

# 原理：
# 1. 创建一张影子表（与原表结构相同+新变更）
# 2. 在原表上建触发器（INSERT/UPDATE/DELETE 同步到影子表）
# 3. 分批拷贝原表数据到影子表（chunk by chunk）
# 4. 完成拷贝后，RENAME 替换原表

# 使用示例
pt-online-schema-change \
  h=localhost,u=root,p=password,D=game_db,t=players \
  --alter "ADD COLUMN vip_exp BIGINT DEFAULT 0, ADD INDEX idx_vip(vip_exp)" \
  --chunk-size=1000 \
  --max-load="Threads_running=20" \
  --critical-load="Threads_running=50" \
  --execute

# 游戏业务使用场景
# 1. 给毫秒级表增加索引（不锁表）
# 2. 大表分表迁移（pt-archiver + pt-online-schema-change 配合）
```

---

## 七、连接池模式（HikariCP）

### 7.1 连接池 vs 无连接池

```
无连接池：
  每次数据库操作：
    → TCP 三次握手（网络 0.5ms - 5ms）
    → MySQL 认证握手（0.3ms - 1ms）
    → 执行 SQL
    → TCP 四次挥手
  总耗时：1ms - 10ms 额外开销

连接池：
  启动时创建 N 个连接
  使用时从池中取（零额外开销）
  用后放回
```

### 7.2 HikariCP 核心配置

```java
// 游戏服务器的 HikariCP 配置（Java 示例）
HikariConfig config = new HikariConfig();
config.setJdbcUrl("jdbc:mysql://localhost:3306/game_db");

// 核心参数
config.setMaximumPoolSize(20);       // 最大连接数（不是越大越好！）
config.setMinimumIdle(5);             // 最小空闲连接
config.setConnectionTimeout(5000);    // 获取连接超时(ms)
config.setIdleTimeout(300000);        // 空闲超时(ms)
config.setMaxLifetime(600000);        // 连接最大寿命(ms)

// MySQL 优化参数
config.addDataSourceProperty("cachePrepStmts", "true");
config.addDataSourceProperty("prepStmtCacheSize", "250");
config.addDataSourceProperty("prepStmtCacheSqlLimit", "2048");
config.addDataSourceProperty("useServerPrepStmts", "true");
config.addDataSourceProperty("useLocalSessionState", "true");
config.addDataSourceProperty("rewriteBatchedStatements", "true");

// 游戏服务器建议
config.setMaximumPoolSize(
    Runtime.getRuntime().availableProcessors() * 2 + 1
);  // 经验公式
// 实际游戏场景：20-50 就足够了，过大反而增加数据库 CPU 开销
```

### 7.3 连接池大小误区

```
误区："连接池越大越好"

真相：
  MySQL 每秒能处理的事务数是固定的（IO 瓶颈而非 CPU 瓶颈）
  过多的连接会导致上下文切换开销和锁争用

     吞吐量
       ↑
   max  |    * ← 最佳点
       |   / \
       |  /   \
       | /     \
       |/        \ ← 过多连接导致性能下降
       +----------------→ 连接数

经验值：最大连接数 = (CPU核心数 × 2) + 磁盘数
游戏服务器一般 20-40 就足够了。
```

---

## 八、MySQL 复制

### 8.1 异步复制与半同步复制

```
异步复制（默认）：
  Master: 提交事务 → 立即返回客户端 → Binlog Dump 线程异步发送
  Slave: 可能延迟几秒甚至更多 → 主库崩了会丢数据

半同步复制（rpl_semi_sync_master）：
  Master: 提交事务 → 等待至少一个 Slave 确认（ACK）→ 返回客户端
  Slave: 收到 Binlog 并写入 Relay Log 后回复 ACK
  牺牲一点写入延迟（增加 RTT）换取数据不丢失

性能影响：
  同机房半同步：增加 0.5-2ms（可接受）
  跨机房半同步：增加 30-100ms（不推荐跨机房写）
```

### 8.2 GTID 复制

```sql
-- 启用 GTID（MySQL 5.6+）
-- my.cnf 配置
gtid_mode = ON
enforce_gtid_consistency = ON

-- GTID 格式：server_uuid:transaction_id
-- 示例：3e11fa47-71ca-11e8-9e0c-000c291e1e32:12345

-- GTID 故障切换（比基于文件位置的方式简单）
-- 主库宕机，将从库提升为主库
-- 传统方式需要找到 MASTER_LOG_FILE 和 MASTER_LOG_POS
-- GTID 方式自动同步
CHANGE MASTER TO MASTER_HOST='new_master', MASTER_USER='repl',
  MASTER_PASSWORD='password', MASTER_AUTO_POSITION=1;
START SLAVE;
```

### 8.3 并行复制

```sql
-- MySQL 5.7+ 支持基于逻辑时钟的并行回放
-- 设置并行从库线程数
SET GLOBAL slave_parallel_workers = 4;  -- 根据 CPU 核数设置
SET GLOBAL slave_parallel_type = 'LOGICAL_CLOCK';

-- 原理：同一时间点准备提交的事务没有冲突，可以并行回放
--          ↓ 时间线 →
-- Master: Tx1 commit | Tx2 commit | Tx3 commit
--                     ↑ 同一时间窗口，3 个事务没有锁冲突
-- Slave:  可并行回放 Tx1, Tx2, Tx3
```

### 8.4 游戏服务器复制架构

```
推荐架构（一主多从 + 分层）：

                    +---------+
                    | Master  | ← 写入操作（玩家数据变更）
                    +----+----+
                         |
              +----------+----------+
              |          |          |
         +----+---+ +---+----+ +---+----+
         | Slave1 | | Slave2 | | Slave3 |
         | 实时从库 | | 报表从库| | 备份从库|
         +--------+ +--------+ +--------+
              |                    |
        读取玩家数据           延迟 1h 备份
        （轮询或一致性哈希）     用于数据恢复

读写分离路由：
  - 登录验证 → 读从库（允许一定延迟）
  - 玩家信息加载 → 读主库（需要最新数据）
  - 排行榜 → 读从库（容忍延迟）
  - 充值确认 → 读主库 + 半同步（绝对不丢数据）
```

---

## 停靠点

| 知识点 | 一句话总结 | 游戏开发启示 |
|--------|-----------|--------------|
| EXPLAIN FORMAT=JSON | 提供查询代价、扫描行数、过滤比例等详细信息 | 优化前先用 JSON 格式确认瓶颈 |
| 覆盖索引 | 索引包含查询的全部列，无需回表 | 玩家登录查询建立复合覆盖索引 |
| ICP | 将索引条件推向存储引擎层过滤 | 减少回表次数，复合索引多列条件下生效 |
| Query Cache | MySQL 8.0 已移除，替代用 ProxySQL/Redis | 不要依赖 MySQL 缓存，Redis 才是正解 |
| RANGE 分区 | 按月分区日志表，自动裁剪 | 日志表首选方案 |
| HASH 分区 | 按 player_id 哈希均匀分布 | 数据量大时优先考虑，但分区数固定 |
| 垂直分片 | 按业务模块分不同的数据库 | 游戏初期首选，架构简单清晰 |
| 水平分片 | 按 player_id 取模分库 | 扩容时需要考虑数据搬迁 |
| Online DDL | ALGORITHM=INPLACE,LOCK=NONE 可在线加列 | 大表改结构时必须用 |
| pt-osc | 通过触发器+影子表实现无锁 DDL | 生产环境改大表的保底方案 |
| 连接池 | HikariCP 20-40 连接足够 | 连接数不是越大越好 |
| 半同步复制 | 至少一个从库确认再返回 | 充值、重要数据必须用半同步 |
| GTID | 基于全局事务 ID 的复制 | 简化主从切换流程 |
| 并行复制 | 基于逻辑时钟的并行回放 | 降低从库延迟的关键 |


> 进阶方向：阅读 MySQL 官方手册的 "Optimization" 章节，学习 Performance Schema 和 Sys Schema 监控。
