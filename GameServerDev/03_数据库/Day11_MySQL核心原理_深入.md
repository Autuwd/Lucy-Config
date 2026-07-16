# Day 11：MySQL 核心原理 — 进阶深入

> 目标读者：游戏服务端开发者
> 前置知识：MySQL 基本 CRUD、索引概念、事务 ACID
> 本篇深度：InnoDB 存储引擎内部机制

---

## 一、InnoDB Buffer Pool 内部机制

### 1.1 Buffer Pool 架构

Buffer Pool 是 InnoDB 在内存中缓存数据页和索引页的核心区域。它直接决定了数据库的读写性能。

```
+-------------------------------------------------------------+
|                    Buffer Pool Instance                       |
|  +-----------+  +-----------+  +-----------+  +-----------+  |
|  |  Page 1   |  |  Page 2   |  |  Page 3   |  |  Page 4   |  |
|  +-----------+  +-----------+  +-----------+  +-----------+  |
|  |  ...      |  |  ...      |  |  ...      |  |  ...      |  |
|  +-----------+  +-----------+  +-----------+  +-----------+  |
|                                                               |
|  +------------------- LRU List ---------------------------+  |
|  | [young] -> [young] -> [old] -> [old] -> [old] -> ...  |  |
|  +--------------------------------------------------------+  |
|  +------------------- Free List --------------------------+  |
|  | [free] -> [free] -> [free] -> ...                      |  |
|  +--------------------------------------------------------+  |
|  +------------------- Flush List -------------------------+  |
|  | [dirty] -> [dirty] -> [dirty] -> ...                   |  |
|  +--------------------------------------------------------+  |
+-------------------------------------------------------------+
```

关键参数：
- `innodb_buffer_pool_size`：通常设置为物理内存的 60%-80%
- `innodb_buffer_pool_instances`：多实例减少锁竞争
- `innodb_old_blocks_pct`：LRU 链表老年代比例（默认 37%）
- `innodb_old_blocks_time`：老年代数据页被访问后移入年轻代的等待时间（默认 1000ms）

### 1.2 改进型 LRU 算法

InnoDB 没有使用标准 LRU，而是采用 **分代 LRU（Midpoint Insertion Strategy）**：

```
标准 LRU 问题：全表扫描会污染缓存
  扫描 100 万行 → 把所有热点数据挤出 → 缓存命中率骤降

改进 LRU 策略：
  1. 新读入的页不放在头部，而是插入到 LRU 中部的 midpoint
  2. midpoint 位置由 innodb_old_blocks_pct 控制
  3. 在 innodb_old_blocks_time 时间内再次访问才移入 young 区
  4. 全表扫描的数据只会停留在 old 区，很快被淘汰
```

### 1.3 Adaptive Hash Index (AHI)

InnoDB 会**自动**为频繁访问的索引页建立哈希索引，加速等值查询。

```sql
-- 查看 AHI 使用情况
SHOW ENGINE INNODB STATUS\G
-- 搜索 "Hash table size" 和 "hash searches/s"
```

AHI 构建条件：
- 页面的访问模式必须是等值查询（range 查询不会触发）
- 页面的访问频率超过一定阈值
- AHI 只对 B+ Tree 叶节点建立，不覆盖所有查询

> 游戏场景适用：玩家 ID 等值查询（`WHERE player_id = ?`）频繁时，AHI 会自然加速，无需干预。

### 1.4 Change Buffer

Change Buffer 用于缓存**对二级索引页的修改操作**（INSERT、UPDATE、DELETE），当目标页不在 Buffer Pool 时，先记录变更而不是立刻读入磁盘。

```
二级索引修改流程（无 Change Buffer）：
  INSERT → 检查二级索引页是否在 BP → 不在 → 磁盘读入 → 修改

二级索引修改流程（有 Change Buffer）：
  INSERT → 检查二级索引页是否在 BP → 不在 → 写入 Change Buffer
       → 后续读取该页时再合并（Merge）
```

配置参数：
```sql
-- 查看 Change Buffer 大小
SHOW VARIABLES LIKE 'innodb_change_buffer_max_size%';  -- 默认 25%（占 BP 的 %）

-- 查看 Change Buffer 合并统计
SHOW STATUS LIKE '%change_buffer%';
```

> 游戏场景价值：玩家频繁创建/删除角色时，二级索引（如 `server_id`, `guild_id`）的修改可以批量合并。

---

## 二、B+ Tree 页面分裂与合并

### 2.1 页面结构

```
InnoDB 数据页（默认 16KB）：
+---------------------------------------------------+
| File Header (38B)     ← 页号、前驱页号、后继页号    |
| Page Header (56B)     ← 页类型、记录数、偏移量      |
| Infimum + Supremum    ← 虚拟最小/最大记录          |
| User Records          ← 实际存储的行记录            |
| Free Space            ← 空闲空间                   |
| Page Directory        ← 记录指针数组（二分查找用）   |
| File Trailer (8B)     ← 校验和 + LSN              |
+---------------------------------------------------+
```

### 2.2 页面分裂（Page Split）

当插入导致页满时，InnoDB 会分配新页并分裂。

```
分裂前（叶节点已满）：
  [页 10: 1,3,5,7,9,11,13,15, 满了!]

分裂过程：
  1. 分配新页（页 11）
  2. 取页 10 的中间记录 (9) 提升到父节点
  3. 左半部分留在页 10: [1,3,5,7]
  4. 右半部分移到页 11: [9,11,13,15]
  5. 更新父节点指针和双向链表链接

分裂后：
  父节点: ... [7]→[9] ...
              ↓     ↓
  页 10: [1,3,5,7] ←→ 页 11: [9,11,13,15]
```

**分裂代价极高**：涉及加锁（page X latch）、空间分配、父节点修改、可能递归分裂。

### 2.3 页面合并（Page Merge）

当删除导致页利用率低于 `MERGE_THRESHOLD`（默认 50%）时，InnoDB 会尝试与前后兄弟页合并。

```
合并条件：
  - 页利用率 < innodb_merge_threshold （默认 50%）
  - 相邻页有足够空间容纳当前页的记录
  - 合并后不违反 B+ Tree 约束

游戏场景：玩家退出公会、删除好友等批量删除操作后，合并可以减少索引空间占用。
```

### 2.4 优化策略

```sql
-- 监控索引页分裂
SHOW GLOBAL STATUS LIKE '%page_splits%';

-- 合并阈值设置
SET GLOBAL innodb_merge_threshold = 40;

-- 对于顺序插入（如自增主键），分裂极少发生
-- 对于随机插入（如 UUID 主键），分裂非常频繁 → 推荐使用自增主键
```

> 游戏开发启示：主键尽量使用自增 BIGINT，不要用 UUID。玩家 ID 生成器应保证趋势递增。

---

## 三、Redo Log vs Undo Log vs Binlog

### 3.1 三种日志对比

```
+----------------+-------------------+-------------------+--------------------+
|    特性         | Redo Log          | Undo Log          | Binlog             |
+----------------+-------------------+-------------------+--------------------+
| 层级            | InnoDB 引擎层      | InnoDB 引擎层      | MySQL Server 层     |
| 作用            | 崩溃恢复，保障持久性 | 事务回滚 + MVCC    | 主从复制 + 恢复     |
| 记录内容         | 物理变更（页级别）   | 逻辑反转操作        | SQL 语句或行变更     |
| 写入时机         | 事务执行中持续写入   | 事务执行中持续写入   | 事务提交时写入       |
| 清理时机         | 循环写，Checkpoint | purge 线程清理      | 保留期自动清理       |
| 文件格式         | ib_logfile0/1     | ibdata1（系统表空间）| mysql-bin.000001    |
+----------------+-------------------+-------------------+--------------------+
```

### 3.2 WAL（Write-Ahead Logging）保证

```
事务提交时的写入顺序：

  1. 事务执行 → 修改内存中的 Buffer Pool（脏页）
  2. 生成 Redo Log 条目（记录在 redo log buffer）
  3. 事务 COMMIT → Redo Log 持久化到磁盘（fsync）← 关键步骤
  4. Binlog 写入（此时事务对 binlog 可见）
  5. 返回客户端 "提交成功"
  6. 未来某个 Checkpoint 或脏页刷新时 → 真正的数据页写入磁盘

崩溃恢复流程：
  - 扫描 Redo Log 找到最后一个 Checkpoint
  - 重放 Checkpoint 后的日志变更（Redo 阶段）
  - 回滚未提交的事务（利用 Undo Log）
  - 恢复完成
```

### 3.3 两阶段提交（XA）

为了保证 Redo Log 和 Binlog 的一致性，InnoDB 使用两阶段提交：

```
Prepare 阶段：
  - 写入 Redo Log，状态设为 PREPARE
  - Redo Log fsync

Commit 阶段：
  - 写入 Binlog
  - Binlog fsync
  - 写入 Redo Log，状态设为 COMMIT

崩溃恢复判断：
  如果崩溃时处于 PREPARE 状态：
    - Binlog 已写入 → 事务提交（因为 Binlog 已成功，从库可能已应用）
    - Binlog 未写入 → 事务回滚
```

### 3.4 游戏数据恢复场景

```sql
-- 误删数据恢复（利用 Binlog）
-- 1. 找到误操作前的 Binlog 位置
SHOW BINLOG EVENTS IN 'mysql-bin.000123' FROM 123456 LIMIT 10;

-- 2. 恢复到指定时间点
mysqlbinlog --stop-datetime="2026-07-14 21:59:59" \
  mysql-bin.000123 | mysql -u root -p game_db

-- 注意：游戏运行中不要轻易做 point-in-time recovery，应该先搭建从库再恢复
```

---

## 四、MVCC Read View 实现细节

### 4.1 Read View 结构

每个读事务（或每个 SQL 语句）在开始时创建一个 Read View，包含以下信息：

```
Read View 组成：
  + low_limit_id     ← 当前最大事务 ID（下一个要分配的事务 ID）
  + up_limit_id      ← 当前活跃事务中的最小事务 ID
  + creator_trx_id   ← 创建该 Read View 的事务 ID
  + m_ids            ← 当前活跃事务 ID 列表（未提交的）
  + m_low_limit_id   ← 低水位（用于判断事务可见性）
```

### 4.2 可见性判断规则

```
判断一行记录的事务 ID（trx_id）是否可见：

  1. trx_id == creator_trx_id  → 可见（自己修改的）
  2. trx_id < up_limit_id      → 可见（已提交的旧事务）
  3. trx_id >= low_limit_id    → 不可见（未来的事务）
  4. trx_id IN m_ids           → 不可见（活跃的并发事务）
  5. 其他                     → 可见（已经提交的事务）
```

### 4.3 Undo Log 版本链

```
行记录(最新): [name="Alice", age=25]  ← 当前版本（在聚簇索引中）
    ↑
Undo Log V2: [name="Alice", age=24]   ← 历史版本
    ↑
Undo Log V1: [name="Alice", age=22]   ← 更早的版本
    ↑
Undo Log V0: [name="Bob", age=22]     ← 初始版本
```

当 Read View 需要读取版本时，沿着 Undo 链回滚到可见版本：

```
示例：事务 A（trx_id=100）在 Read View（up=95, low=105, m_ids=[98,102]）下读取
  → 当前版本 trx_id=103 > up_limit_id（95）
  → 103 >= low_limit_id(105)? 不是
  → 103 IN m_ids([98,102])? 不是
  → 所以 trx_id=103 是已提交的，可见 → 直接返回当前版本
```

### 4.4 RC vs RR 差异

```
READ COMMITTED（行级，每次 SQL 创建新的 Read View）：
  - 每条 SQL 执行前重新创建 Read View
  - 可以看到其他事务已提交的变更（不可重复读）

REPEATABLE READ（事务级，首次读创建 Read View）：
  - 事务中第一条 SQL 创建 Read View，后续复用
  - 同一事务中多次读取结果一致
  - MySQL InnoDB 默认隔离级别
```

---

## 五、Gap Lock 与 Next-Key Lock

### 5.1 Record Lock + Gap Lock + Next-Key Lock

```
三种锁的覆盖范围：

  Record Lock:         锁定索引中的单条记录
  Gap Lock:            锁定一个范围（不包含记录本身），防止幻读
  Next-Key Lock:       Record Lock + Gap Lock 的组合（左开右闭区间）
```

### 5.2 Gap Lock 示例

```sql
-- 表结构
CREATE TABLE `players` (
  `id` INT PRIMARY KEY,
  `level` INT NOT NULL,
  `name` VARCHAR(32),
  INDEX `idx_level` (`level`)
) ENGINE=InnoDB;

-- 数据
INSERT INTO players VALUES (1, 10, 'Alice'), (3, 20, 'Bob'), (5, 30, 'Charlie');

-- 事务 A
BEGIN;
SELECT * FROM players WHERE level BETWEEN 15 AND 25 FOR UPDATE;
-- 锁定范围：level(10, 20] 和 level(20, 30) 之间的间隙

-- 事务 B（阻塞！）
INSERT INTO players (id, 2, level, 'David') VALUES (2, 17, 'David');
-- level=17 落在 gap (10,20) 内 → 等待 Gap Lock 释放
```

### 5.3 Next-Key Lock 示例（防止幻读）

```
假设表中有 id=10,30,50 三条记录

查询：SELECT * FROM table WHERE id > 15 AND id < 40 FOR UPDATE;

Next-Key Lock 锁定的区间：
  (-∞, 10]     ← 超出行锁，next-key lock
  (10, 30]     ← 锁定区间，防止插入 id=20
  (30, 50]     ← 锁定区间，防止插入 id=40
  (50, +∞)     ← 超出行锁，但 gap lock 锁定 (50, +∞)

所以插入 id=20 或 id=40 都会被阻塞。
```

### 5.4 降低锁冲突的游戏优化

```sql
-- 不推荐：范围锁定，容易引起锁冲突
SELECT * FROM items WHERE player_id = ? AND slot BETWEEN 1 AND 20 FOR UPDATE;

-- 推荐：明确主键等值查询，只锁目标行
SELECT * FROM items WHERE id = ? FOR UPDATE;

-- 场景：排行榜结算时，使用排他锁保护玩家排行数据
-- 可用 REM 而不是行锁（Redis 分布式锁代替 DB 锁）
```

---

## 六、死锁检测与预防

### 6.1 死锁检测机制

InnoDB 通过 **等待图（Wait-For Graph）** 检测死锁：

```
事务 A 持有锁 L1，等待锁 L2
事务 B 持有锁 L2，等待锁 L1

等待图：
  A ──(等待 L2)──→ B
  A ←──(等待 L1)── B

存在环 → 死锁 → 选中一个牺牲者回滚
```

```sql
-- 查看最近的一次死锁
SHOW ENGINE INNODB STATUS\G
-- 搜索 "LATEST DETECTED DEADLOCK"

-- 查看当前正在等待锁的事务
SELECT * FROM performance_schema.data_lock_waits\G
```

### 6.2 典型死锁场景（游戏）

```sql
-- 场景：两个玩家交换物品（经典 AB-BA 死锁）

-- 时间线：
-- T1: 事务 A 更新道具表 WHERE player_id=100 AND item_id=1    ← 锁住行 R1
-- T2: 事务 B 更新道具表 WHERE player_id=200 AND item_id=5    ← 锁住行 R2
-- T3: 事务 A 更新道具表 WHERE player_id=200 AND item_id=5    ← 等待 R2（被 B 持有）
-- T4: 事务 B 更新道具表 WHERE player_id=100 AND item_id=1    ← 等待 R1（被 A 持有）
--      → 死锁！InnoDB 选择回滚其中一个事务
```

### 6.3 预防模式

```sql
-- 模式 1：固定顺序访问（推荐）
-- 所有事务按照 player_id 升序加锁
-- 事务 A → 先 player_id=100 再 player_id=200
-- 事务 B → 先 player_id=100 再 player_id=200
-- 永不出现 AB-BA

-- 模式 2：一次锁定
-- 需要锁定的资源一次性全部获取
SELECT * FROM items WHERE player_id IN (100, 200) FOR UPDATE;

-- 模式 3：超时回退
SET innodb_lock_wait_timeout = 5;  -- 默认 50 秒，游戏业务建议 3-5 秒

-- 模式 4：使用乐观锁代替悲观锁（适合读多写少）
UPDATE items SET quantity = quantity - 1
WHERE id = ? AND quantity > 0;  -- 通过 WHERE 条件实现乐观锁
```

---

## 七、Change Buffer 对二级索引的优化

### 7.1 工作原理

```
无 Change Buffer（二级索引页不在 BP 时）：
  INSERT → 需要维护二级索引 → 目标页不在内存
         → 从磁盘读入 16KB 页 → 修改 → 标记脏页
         → 代价：一次随机 IO

有 Change Buffer（二级索引页不在 BP 时）：
  INSERT → 需要维护二级索引 → 目标页不在内存
         → 将变更记录到 Change Buffer（内存中，少量空间）
         → 未来在读取该页时合并（Merge）
         → 代价：内存写入（零 IO）
```

### 7.2 适用场景

```
Change Buffer 适合：
  ✓ 二级索引比例高的表
  ✓ 大量 INSERT/UPDATE 操作
  ✓ 二级索引页在内存中命中率低

Change Buffer 不适合：
  ✗ 唯一二级索引（需要立即检查唯一性，无法缓存）
  ✗ 写少读多的业务（缓存了也没用）
  ✗ SSD 磁盘（随机 IO 相对较快，Change Buffer 优势降低）

游戏场景：
  - 日志表（大量 INSERT，极少 UPDATE）→ 非常适合
  - 玩家道具表（频繁 INSERT/DELETE）→ 适合
  - 排行榜表（频繁 UPDATE 排名）→ 普通索引适合，唯一索引不适合
```

---

## 八、Adaptive Flushing 算法

### 8.1 脏页刷新策略

InnoDB 根据 Redo Log 生成速度和脏页比例动态调整刷新频率：

```
刷新触发条件：
  1. Redo Log 使用率达到 innodb_max_dirty_pages_pct_lwm（默认 10%）
  2. 全局脏页比例超过 innodb_max_dirty_pages_pct（默认 90%）
  3. Redo Log 空间即将写满，需要推进 Checkpoint

自适应公式：
  目标刷新率 = 最近 N 秒的 Redo Log 生成速率 × 脏页比例系数
```

### 8.2 刷新参数

```sql
-- 关键参数
innodb_io_capacity = 2000          -- SSD 建议 2000-10000
innodb_io_capacity_max = 4000      -- 最大突发刷新能力
innodb_flush_neighbors = 0         -- SSD 设置为 0（不需要预读相邻页）
innodb_adaptive_flushing = ON      -- 自适应刷新（默认启用）
innodb_adaptive_flushing_lwm = 10  -- Redo Log 使用率低水位线
```

### 8.3 游戏服务器调优建议

```
读多写少业务（玩家登录验证场景）：
  innodb_max_dirty_pages_pct = 90   -- 允许较多脏页，提高写吞吐
  innodb_io_capacity = 5000          -- 充分利用 SSD

写密集型业务（日志、排行榜结算）：
  innodb_max_dirty_pages_pct = 50   -- 减少脏页积压
  innodb_flush_log_at_trx_commit = 2 -- 每秒刷 Redo Log（牺牲一点持久性换性能）
  innodb_io_capacity = 10000         -- 提高刷新能力
```

---

## 停靠点

| 知识点 | 一句话总结 | 游戏开发启示 |
|--------|-----------|--------------|
| Buffer Pool 分代 LRU | 新页插入 midpoint，防止全表扫描污染缓存 | 避免大范围不带 WHERE 的查询 |
| AHI | 自动为热点页建立哈希索引 | 玩家 ID 等值查询自动加速 |
| Change Buffer | 缓存二级索引的修改，合并后写 | 玩家道具日志 INSERT 密集场景收益大 |
| B+ Tree 分裂 | 页满时分裂为两半，产生碎片 | 自增主键减少分裂频率 |
| Redo Log | 物理日志保障持久性 | WAL 是事务安全的根基 |
| Undo Log | 逻辑日志用于回滚和 MVCC | RR 级别下事务内一致性读 |
| Binlog | 逻辑日志用于复制 | 数据恢复必须依赖 Binlog |
| MVCC Read View | 基于事务 ID 区间的可见性判断 | 同一事务多次读取结果一致 |
| Gap/Next-Key Lock | 锁定间隙防止幻读 | RR 下 `FOR UPDATE` 会锁范围 |
| 死锁检测 | 等待图检测环，选低成本事务回滚 | 统一访问顺序是最佳预防 |

> 进阶方向：阅读 MySQL 官方手册的 InnoDB 架构章节，关注 `INFORMATION_SCHEMA.INNODB_METRICS` 监控表。
