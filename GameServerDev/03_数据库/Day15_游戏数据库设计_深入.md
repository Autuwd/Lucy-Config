# Day 15：游戏数据库设计 — 进阶深入

> 目标读者：正在设计或重构游戏数据库的服务端开发者
> 前置知识：ER 模型、MySQL 基本表设计、数据库 ACID

---

## 一、玩家数据表结构演进

### 1.1 版本化表结构

游戏运营过程中，玩家数据结构几乎一定会变化。Migration 是关键。

```sql
-- 表结构版本控制
CREATE TABLE `schema_version` (
  `version` INT PRIMARY KEY,      -- 版本号（时间戳或递增数字）
  `description` VARCHAR(255),     -- 变更描述
  `sql_md5` CHAR(32) NOT NULL,    -- SQL 的 MD5 校验（防篡改）
  `applied_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `status` TINYINT DEFAULT 0      -- 0=成功, 1=失败, 2=回滚
);

-- 实际例子：玩家表 v1 → v2 迁移

-- v1: 初始版本
CREATE TABLE `players_v1` (
  `player_id` BIGINT PRIMARY KEY,
  `account_id` VARCHAR(64) NOT NULL,
  `name` VARCHAR(32) NOT NULL,
  `level` INT DEFAULT 1,
  `exp` BIGINT DEFAULT 0,
  `gold` BIGINT DEFAULT 0,
  `vip_level` INT DEFAULT 0,
  `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `last_login_time` DATETIME
);

-- v2: 增加成就系统字段，删除废弃的 honor 字段
ALTER TABLE `players`
  ADD COLUMN `achievement_points` INT DEFAULT 0,
  ADD COLUMN `title_id` INT DEFAULT 0,
  DROP COLUMN `honor`;  -- 废弃字段

-- v3: 增加 JSON 扩展字段（应对频繁新增小功能）
ALTER TABLE `players`
  ADD COLUMN `ext_data` JSON DEFAULT NULL;
```

### 1.2 扩展字段策略

```sql
-- 方案 1：提前预留字段（不推荐）
CREATE TABLE `players` (
  ...
  `reserve1` INT DEFAULT 0,       -- ❌ 语义不明，容易误用
  `reserve2` VARCHAR(255),        -- ❌ 类型和业务不匹配
  `reserve3` VARCHAR(255)
);

-- 方案 2：JSON 扩展字段（推荐 MySQL 5.7+）
CREATE TABLE `players` (
  `player_id` BIGINT PRIMARY KEY,
  `ext_data` JSON DEFAULT NULL    -- 所有小功能字段放这里
);

-- JSON 字段查询
SELECT player_id,
       JSON_EXTRACT(ext_data, '$.achievement.total_score') AS total_score,
       JSON_EXTRACT(ext_data, '$.collection.completed') AS collection_cnt
FROM players
WHERE JSON_EXTRACT(ext_data, '$.achievement.total_score') > 1000;

-- JSON 字段索引（虚拟列 + 索引）
ALTER TABLE players ADD COLUMN
  total_score INT GENERATED ALWAYS AS (ext_data->>'$.achievement.total_score') STORED;
CREATE INDEX idx_total_score ON players(total_score);

-- 方案 3：扩展表（适合字段量大的情况）
CREATE TABLE `player_ext` (
  `player_id` BIGINT NOT NULL,
  `attr_key` VARCHAR(32) NOT NULL,  -- 属性名：'achievement_score', 'title_id'
  `attr_value` TEXT,
  `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_id`, `attr_key`)
);
-- 类似 EAV 模式，但读取性能差（需要多次 JOIN 或 GROUP BY）
```

### 1.3 游戏数据迁移最佳实践

```python
# 迁移脚本最佳实践（Python 伪代码）

def migrate_v1_to_v2():
    """
    迁移说明：players 表 v1 → v2
    变更：增加 achievement_points, title_id, 删除 honor
    影响玩家数：50 万
    预估时间：2 分钟
    """

    # Step 1: 备份（必须！）
    execute("CREATE TABLE players_bak_20260715 LIKE players")
    execute("INSERT INTO players_bak_20260715 SELECT * FROM players")

    # Step 2: 执行 DDL（Online DDL）
    execute("""
        ALTER TABLE players
        ADD COLUMN achievement_points INT DEFAULT 0,
        ADD COLUMN title_id INT DEFAULT 0,
        ALGORITHM=INPLACE, LOCK=NONE
    """)

    # Step 3: 数据初始化（新字段默认值不够时）
    execute("""
        UPDATE players
        SET achievement_points = 0, title_id = 0
        WHERE achievement_points IS NULL
    """)

    # Step 4: 删除废弃字段（低峰期操作）
    execute("""
        ALTER TABLE players
        DROP COLUMN honor,
        ALGORITHM=INPLACE, LOCK=NONE
    """)

    # Step 5: 验证
    # 比较原表和备份表的数据量、关键字段哈希
    # 不一致 → 立即回滚
```

---

## 二、道具系统表设计

### 2.1 三种设计模式对比

```
+----------------+--------------+---------------+------------------+
|                | EAV 模式     | 固定列模式    | JSON 模式         |
+----------------+--------------+---------------+------------------+
| 扩展性         | ★★★★★ 极好   | ★ 差          | ★★★★ 好          |
| 查询性能       | ★★ 差       | ★★★★★ 最好    | ★★★ 中等         |
| 索引支持       | ★ 基本没有   | ★★★★★ 强      | ★★★ 虚拟列       |
| ORM 友好度     | ★ 差        | ★★★★★ 好      | ★★★ 一般         |
| 游戏场景       | 少用         | 背包、装备     | 天赋、Buff       |
+----------------+--------------+---------------+------------------+
```

### 2.2 玩家背包设计（固定列 + 行扩展）

```sql
-- 核心背包表（每行一个道具实例）
CREATE TABLE `player_items` (
  `id` BIGINT AUTO_INCREMENT PRIMARY KEY,           -- 道具实例 ID（全局唯一）
  `player_id` BIGINT NOT NULL,                      -- 玩家 ID（分片键）
  `item_template_id` INT NOT NULL,                  -- 道具模板 ID（关联配置表）
  `quantity` INT NOT NULL DEFAULT 1,                -- 数量（堆叠道具）
  `slot_index` INT NOT NULL DEFAULT -1,             -- 背包格子序号
  `bag_type` TINYINT NOT NULL DEFAULT 0,            -- 背包类型（0=背包, 1=仓库, 2=装备栏）
  `is_locked` TINYINT NOT NULL DEFAULT 0,           -- 是否锁定（防止误操作）
  `bound_type` TINYINT NOT NULL DEFAULT 0,          -- 绑定类型（0=不绑定, 1=装备绑定, 2=获取绑定）
  `expire_time` DATETIME DEFAULT NULL,               -- 过期时间（限时道具）
  `enhance_level` INT DEFAULT 0,                    -- 强化等级（装备专用）
  `ext_attrs` JSON DEFAULT NULL,                     -- 扩展属性（随机词条等）
  `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `uk_player_slot` (`player_id`, `bag_type`, `slot_index`),
  KEY `idx_player_id` (`player_id`),
  KEY `idx_template_id` (`item_template_id`)
) ENGINE=InnoDB;
```

### 2.3 道具操作原子性

```sql
-- 消耗道具（扣减 + 检查可消耗性，原子操作）
-- 使用 UPDATE ... WHERE 做乐观锁
UPDATE player_items
SET quantity = quantity - 1
WHERE id = ?
  AND player_id = ?
  AND quantity >= 1;   -- ← 行级乐观锁，防止超扣

-- 如果 ROWS_AFFECTED = 0 → 道具不足或不存在

-- 删除空堆叠
DELETE FROM player_items
WHERE id = ? AND quantity <= 0;

-- 批量操作（使用事务）
START TRANSACTION;
-- 扣货币
UPDATE player_currencies
SET gold = gold - 100
WHERE player_id = ? AND gold >= 100;

-- 加道具
INSERT INTO player_items (player_id, item_template_id, quantity, slot_index)
VALUES (?, ?, 1, ?);

COMMIT;
```

### 2.4 道具配置表设计

```sql
-- 道具模板配置表（从配置表直接加载，DB 和内存都有）
CREATE TABLE `item_templates` (
  `item_id` INT PRIMARY KEY,           -- 道具 ID
  `name` VARCHAR(64) NOT NULL,          -- 道具名称
  `item_type` TINYINT NOT NULL,         -- 类型（0=消耗品, 1=装备, 2=材料, 3=任务道具）
  `max_stack` INT DEFAULT 999,          -- 最大堆叠数
  `quality` TINYINT DEFAULT 1,          -- 品质（1=白, 2=绿, 3=蓝, 4=紫, 5=橙）
  `sell_price` INT DEFAULT 0,           -- 出售价格
  `use_effect` JSON DEFAULT NULL,       -- 使用效果（JSON 配置，如 {"type":"add_exp","value":100}）
  `bind_on_pickup` TINYINT DEFAULT 0,   -- 拾取绑定
  `expire_seconds` INT DEFAULT 0,       -- 过期秒数（0=永不过期）
  `max_enhance` INT DEFAULT 0           -- 最大强化等级
) ENGINE=InnoDB;
```

---

## 三、邮件系统设计

### 3.1 邮件表结构

```sql
-- 邮件表的挑战：大量过期邮件、大量玩家、附件领取

-- 主表：邮件头（一个邮件可以被多个玩家收到）
CREATE TABLE `mail_headers` (
  `mail_id` BIGINT AUTO_INCREMENT PRIMARY KEY,
  `sender_type` TINYINT NOT NULL DEFAULT 0,  -- 0=系统, 1=GM, 2=玩家
  `sender_id` BIGINT DEFAULT NULL,            -- 发送者 ID（玩家发信用）
  `title` VARCHAR(128) NOT NULL,
  `content` TEXT,
  `priority` TINYINT DEFAULT 0,               -- 优先级（0=普通, 1=重要, 2=紧急）
  `send_time` DATETIME NOT NULL,
  `expire_time` DATETIME NOT NULL,            -- 过期时间
  `attachment_template` JSON DEFAULT NULL,    -- 附件模板（如 [{"item_id":101,"qty":5}]）
  PRIMARY KEY (`mail_id`),
  KEY `idx_expire` (`expire_time`),
  KEY `idx_send_time` (`send_time`)
) ENGINE=InnoDB;

-- 收件表（每个玩家一条）
CREATE TABLE `mail_recipients` (
  `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
  `player_id` BIGINT NOT NULL,               -- 分片键
  `mail_id` BIGINT NOT NULL,                  -- 关联 mail_headers
  `status` TINYINT DEFAULT 0,                 -- 0=未读, 1=已读, 2=已领取, 3=已删除
  `read_time` DATETIME DEFAULT NULL,
  `claim_time` DATETIME DEFAULT NULL,
  `is_attachment_claimed` TINYINT DEFAULT 0,  -- 附件是否已领取
  UNIQUE KEY `uk_player_mail` (`player_id`, `mail_id`),
  KEY `idx_player_status` (`player_id`, `status`),
  KEY `idx_mail_id` (`mail_id`)
) ENGINE=InnoDB;
```

### 3.2 分页查询

```sql
-- 邮件列表分页（游标分页，不使用 OFFSET）
-- 推荐方案：基于 id 的游标分页

-- 第一页（最新 20 封）
SELECT mh.mail_id, mh.title, mh.send_time, mr.status, mr.is_attachment_claimed
FROM mail_recipients mr
JOIN mail_headers mh ON mr.mail_id = mh.mail_id
WHERE mr.player_id = 1001
  AND mr.status < 3              -- 未删除
  AND (mr.status > 0 OR mh.expire_time > NOW())  -- 未读且未过期
ORDER BY mh.priority DESC, mh.send_time DESC, mh.mail_id DESC
LIMIT 20;

-- 第二页（传入上次最后一个 mail_id = 500）
SELECT mh.mail_id, mh.title, mh.send_time, mr.status, mr.is_attachment_claimed
FROM mail_recipients mr
JOIN mail_headers mh ON mr.mail_id = mh.mail_id
WHERE mr.player_id = 1001
  AND mr.status < 3
  AND (mr.status > 0 OR mh.expire_time > NOW())
  AND (mh.priority < (SELECT priority FROM mail_headers WHERE mail_id = 500)
       OR (mh.priority = (SELECT priority FROM mail_headers WHERE mail_id = 500)
           AND (mh.send_time < (SELECT send_time FROM mail_headers WHERE mail_id = 500)
                OR (mh.send_time = (SELECT send_time FROM mail_headers WHERE mail_id = 500)
                    AND mh.mail_id < 500))))
ORDER BY mh.priority DESC, mh.send_time DESC, mh.mail_id DESC
LIMIT 20;

-- 简化版：直接用 mail_id 做游标（省略优先级排序）
SELECT ... WHERE mr.player_id = 1001 AND mr.id < ? ORDER BY mr.id DESC LIMIT 20;
```

### 3.3 附件领取

```sql
-- 附件领取（原子操作）
START TRANSACTION;

-- 1. 检查是否已领取
SELECT is_attachment_claimed FROM mail_recipients
WHERE player_id = ? AND mail_id = ? FOR UPDATE;

-- 2. 领取附件
UPDATE mail_recipients
SET is_attachment_claimed = 1, status = 2, claim_time = NOW()
WHERE player_id = ? AND mail_id = ? AND is_attachment_claimed = 0;

-- 3. 生成道具到背包
INSERT INTO player_items (player_id, item_template_id, quantity)
SELECT ?, JSON_UNQUOTE(JSON_EXTRACT(attachment_template, '$[0].item_id')),
       JSON_UNQUOTE(JSON_EXTRACT(attachment_template, '$[0].qty'))
FROM mail_headers WHERE mail_id = ?;

COMMIT;

-- 优化：封装成存储过程或应用层 Lua 脚本
```

### 3.4 过期邮件清理

```sql
-- 每日定时清理脚本
-- 删除过期邮件 + 已读且超过 30 天的邮件

DELETE FROM mail_headers
WHERE expire_time < DATE_SUB(NOW(), INTERVAL 1 DAY)
  AND mail_id NOT IN (
    SELECT DISTINCT mail_id FROM mail_recipients
    WHERE is_attachment_claimed = 0
  );
-- 注意：含未领取附件的邮件不能删除！

-- 收件表清理（软删除的可以直接物理删除）
DELETE FROM mail_recipients
WHERE status = 3 AND create_time < DATE_SUB(NOW(), INTERVAL 7 DAY);

-- 高效清理策略：分区表（按月分区）
-- 删除一个分区比 DELETE 快得多
ALTER TABLE mail_recipients DROP PARTITION p202605;
```

---

## 四、公会/联盟数据模型

### 4.1 公会表结构

```sql
CREATE TABLE `guilds` (
  `guild_id` INT AUTO_INCREMENT PRIMARY KEY,
  `name` VARCHAR(32) NOT NULL UNIQUE,           -- 公会名（唯一索引）
  `owner_player_id` BIGINT NOT NULL,            -- 会长
  `level` INT DEFAULT 1,
  `exp` BIGINT DEFAULT 0,
  `member_count` INT DEFAULT 0,                 -- 反范式：实时统计
  `max_members` INT DEFAULT 50,
  `declaration` VARCHAR(256) DEFAULT '',         -- 公会宣言
  `icon_id` INT DEFAULT 0,
  `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `guild_data` JSON DEFAULT NULL,                -- 扩展数据（公会科技等）
  KEY `idx_level` (`level`),
  KEY `idx_owner` (`owner_player_id`)
) ENGINE=InnoDB;

-- 公会成员表
CREATE TABLE `guild_members` (
  `guild_id` INT NOT NULL,
  `player_id` BIGINT NOT NULL,
  `role` TINYINT NOT NULL DEFAULT 0,            -- 0=成员, 1=精英, 2=副会长, 3=会长
  `title` VARCHAR(32) DEFAULT '',                -- 自定义头衔
  `contribution` INT DEFAULT 0,                  -- 个人贡献
  `join_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `last_active_time` DATETIME,
  PRIMARY KEY (`guild_id`, `player_id`),
  KEY `idx_player` (`player_id`),
  KEY `idx_role` (`guild_id`, `role`)
) ENGINE=InnoDB;
```

### 4.2 公会战匹配（图关系查询）

```sql
-- 公会关系表（同盟/敌对）
CREATE TABLE `guild_relations` (
  `guild_id_a` INT NOT NULL,
  `guild_id_b` INT NOT NULL,
  `relation_type` TINYINT NOT NULL,             -- 0=同盟, 1=敌对, 2=宣战
  `established_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`guild_id_a`, `guild_id_b`),
  KEY `idx_guild_b` (`guild_id_b`)
);

-- 查询某个公会的所有同盟公会（双向关系）
SELECT *
FROM guild_relations
WHERE (guild_id_a = ? OR guild_id_b = ?)
  AND relation_type = 0;

-- 公会战匹配：根据活跃度排名筛选对手
SELECT g.*, gm.member_count
FROM guilds g
WHERE g.guild_id != ?
  AND g.level BETWEEN ? AND ?      -- 等级范围
  AND g.member_count >= ?           -- 最少人数
  AND g.guild_id NOT IN (           -- 排除已经在交战的
    SELECT CASE WHEN guild_id_a = ? THEN guild_id_b ELSE guild_id_a END
    FROM guild_relations
    WHERE (guild_id_a = ? OR guild_id_b = ?)
      AND relation_type = 2
  )
ORDER BY ABS(g.level - ?) ASC      -- 找等级相近的
LIMIT 5;
```

### 4.3 公会操作的事务

```sql
-- 玩家加入公会（跨表事务）
START TRANSACTION;

-- 1. 检查公会人数
SELECT member_count, max_members FROM guilds
WHERE guild_id = ? FOR UPDATE;

-- 2. 更新公会人数（反范式）
UPDATE guilds SET member_count = member_count + 1 WHERE guild_id = ?;

-- 3. 插入成员记录
INSERT INTO guild_members (guild_id, player_id, role, join_time)
VALUES (?, ?, 0, NOW());

-- 4. 更新玩家信息的公会 ID
UPDATE players SET guild_id = ? WHERE player_id = ?;

COMMIT;

-- 注意：member_count 是反范式字段，但能避免每次 COUNT(*)
-- 缺点是必须保证事务一致性
```

---

## 五、好友列表设计

### 5.1 双向关系 vs 单向关系

```
单向关系（关注/粉丝模式）：
  适用：微博、直播平台的关注
  存储：A 关注 B（存一条记录）
  查询：A 的关注列表、B 的粉丝列表
  好友判断：需要查两次（A→B 和 B→A）

双向关系（真正的好友）：
  适用：游戏好友、微信
  存储：A-B（存一条记录，但双方确认）
  查询：A 的好友列表
  好友判断：只需查一次（A-B存在且状态=好友）
```

### 5.2 好友表设计（双向模式）

```sql
-- 好友关系表（双向确认）
CREATE TABLE `friend_relations` (
  `player_id` BIGINT NOT NULL,          -- 玩家 A
  `friend_id` BIGINT NOT NULL,          -- 玩家 B
  `status` TINYINT NOT NULL DEFAULT 0,  -- 0=申请中, 1=已确认, 2=已拉黑, 3=已删除
  `apply_msg` VARCHAR(128) DEFAULT '',
  `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_id`, `friend_id`),
  KEY `idx_friend` (`friend_id`, `status`),
  KEY `idx_player_status` (`player_id`, `status`)
) ENGINE=InnoDB;

-- 好友申请（A 申请加 B 为好友）
INSERT INTO friend_relations (player_id, friend_id, status, apply_msg)
VALUES (1001, 2001, 0, '一起打副本吧！')
ON DUPLICATE KEY UPDATE status = 0, apply_msg = VALUES(apply_msg);

-- B 同意好友申请
UPDATE friend_relations SET status = 1
WHERE player_id = 1001 AND friend_id = 2001;

-- 查询 A 的好友列表（双向关系）
SELECT
  CASE WHEN player_id = 1001 THEN friend_id ELSE player_id END AS friend_id,
  CASE WHEN player_id = 1001 THEN '我加的他' ELSE '他加的我' END AS direction,
  status
FROM friend_relations
WHERE (player_id = 1001 OR friend_id = 1001)
  AND status = 1;
```

### 5.3 好友列表优化

```sql
-- 好友列表中需要显示好友的在线状态和职业等级
-- 使用 JOIN 或缓存

-- 1. 查询好友列表 + 基本信息（JOIN，性能允许的话）
SELECT
  fr.friend_id,
  p.name,
  p.level,
  p.class_id,
  p.online_status,
  fr.create_time AS friend_since
FROM friend_relations fr
JOIN players p ON fr.friend_id = p.player_id
WHERE fr.player_id = 1001 AND fr.status = 1
ORDER BY p.online_status DESC, p.level DESC
LIMIT 50;

-- 2. 或者拆成两步（大量好友时更优）
-- Step 1: 查好友 ID 列表（从 Redis 的 Set 中取）
SMEMBERS friend:1001
-- Step 2: 批量查好友信息
-- 应用层用 MGET 或管道批量查询

-- 在线状态不宜实时 JOIN，应该用 Redis 缓存
-- 好友列表在 Redis 中的缓存结构：
--   SET friend:1001 = {2001, 3001, 4001, ...}
--   HASH player:1001:online = {status: 1, last_online: 20260715123000}
```

---

## 六、战斗记录存储

### 6.1  replay 数据结构

```sql
-- 战斗回放表
CREATE TABLE `battle_replays` (
  `battle_id` BIGINT AUTO_INCREMENT PRIMARY KEY,
  `battle_type` TINYINT NOT NULL,               -- 战斗类型（0=PvE, 1=PvP, 2=公会战）
  `winner_id` BIGINT DEFAULT NULL,               -- 获胜者
  `loser_id` BIGINT DEFAULT NULL,                -- 失败者
  `duration_ms` INT DEFAULT 0,                   -- 战斗时长
  `battle_version` INT NOT NULL,                 -- 战斗逻辑版本（兼容回放）
  `replay_data` MEDIUMBLOB NOT NULL,             -- 回放数据（压缩后的二进制）
  `data_size` INT NOT NULL,                      -- 压缩前大小
  `compressed_size` INT NOT NULL,                -- 压缩后大小
  `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `expire_time` DATETIME DEFAULT NULL,           -- 过期时间
  KEY `idx_players` (`winner_id`, `loser_id`),
  KEY `idx_create_time` (`create_time`),
  KEY `idx_expire` (`expire_time`)
) ENGINE=InnoDB;
```

### 6.2 数据压缩策略

```python
# 战斗回放数据压缩（Python 伪代码）

# 原始战斗数据（JSON）
battle_data = {
    "version": 2,
    "seed": 12345,           # 随机种子（确定性逻辑可复现）
    "players": [
        {"id": 1001, "team": 0, "class": 3, "skills": [1, 3, 5]},
        {"id": 2001, "team": 1, "class": 5, "skills": [2, 4, 6]}
    ],
    "actions": [
        {"t": 0, "a": "move", "p": 1001, "x": 10.5, "y": 20.3},
        {"t": 1, "a": "skill", "p": 1001, "s": 3, "t": 2001, "d": 150},
        {"t": 2, "a": "move", "p": 2001, "x": 15.2, "y": 18.7},
        # ... 上百条甚至上千条 action
    ]
}

# 压缩策略 1：Protocol Buffers（二进制格式，推荐）
# .proto 定义
message BattleReplay {
    int32 version = 1;
    int32 seed = 2;
    repeated PlayerInfo players = 3;
    repeated Action actions = 4;
}
# 压缩率：JSON 5KB → ProtoBuf 1.5KB（约 70% 压缩率）

# 压缩策略 2：差值编码（Action 序列优化）
# 原始：每个 action 完整记录
# 优化后：只记录变化差值

# 原始（每个 action 都带完整时间戳）：
# [{"t": 100, "a": "move", "x": 10, "y": 20},
#  {"t": 150, "a": "move", "x": 11, "y": 20},
#  {"t": 200, "a": "move", "x": 12, "y": 20}]

# 优化（相对时间 + 增量坐标）：
# [{"dt": 100, "a": "move", "dx": 10, "dy": 20},
#  {"dt": 50, "a": "move", "dx": 1},
#  {"dt": 50, "a": "move", "dx": 1}]
# 压缩率：可以减少 30%-50% 重复字段

# 策略 3：通用压缩（zlib/lz4）
import zlib
compressed = zlib.compress(json.dumps(battle_data).encode(), level=6)
# level=6：平衡压缩率和速度
# 压缩率：通常 5:1 到 10:1
```

### 6.3 战斗记录保留策略

```sql
-- 保留策略：
--   PvP 普通场：保留 7 天
--   PvP 排位赛：保留 30 天
--   公会战：保留整个赛季
--   运营需要：归档到 HDFS/OSS

-- 分区清理（按周分区）
CREATE TABLE `battle_replays` (
  ...
) ENGINE=InnoDB
PARTITION BY RANGE (TO_DAYS(create_time)) (
  PARTITION p20260714 VALUES LESS THAN (TO_DAYS('2026-07-21')),
  PARTITION p20260721 VALUES LESS THAN (TO_DAYS('2026-07-28')),
  PARTITION p_future VALUES LESS THAN MAXVALUE
);

-- 删除旧分区
ALTER TABLE battle_replays DROP PARTITION p20260714;
```

---

## 七、赛季数据重置策略

### 7.1 三种重置模式

```
模式 1：全量重置（赛季结束，所有玩家数据清空）
  适用：天梯排名、竞技场积分
  实现：TRUNCATE TABLE / DROP PARTITION

模式 2：继承重置（保留部分数据，重置其他）
  适用：段位保留初始分，重置额外奖励次数
  实现：UPDATE + 备份

模式 3：增量保留（数据不断累积）
  适用：成就点数、图鉴收集进度
  实现：不清除
```

### 7.2 赛季重置实现

```sql
-- 天梯排行榜设计
CREATE TABLE `arena_rank` (
  `player_id` BIGINT NOT NULL,
  `season_id` INT NOT NULL,             -- 赛季 ID
  `rank_score` INT DEFAULT 1000,        -- 赛季积分
  `highest_score` INT DEFAULT 1000,     -- 赛季最高分
  `battle_count` INT DEFAULT 0,         -- 赛季战斗场次
  `win_count` INT DEFAULT 0,            -- 胜场
  `rank` INT DEFAULT 0,                 -- 排名（定时计算）
  `reward_claimed` TINYINT DEFAULT 0,   -- 结算奖励是否已领取
  PRIMARY KEY (`player_id`, `season_id`),
  KEY `idx_rank` (`season_id`, `rank`)
) ENGINE=InnoDB;

-- 赛季结算流程

-- Step 1: 锁定当前赛季
UPDATE season_config SET status = 2 WHERE season_id = ? AND status = 1;
-- status: 0=未开始, 1=进行中, 2=结算中, 3=已结束

-- Step 2: 计算排名（定时任务或 SQL）
SET @rank = 0;
UPDATE arena_rank
SET rank = (@rank := @rank + 1)
WHERE season_id = ?
ORDER BY rank_score DESC, win_count DESC;

-- Step 3: 发送赛季奖励邮件（见邮件系统）

-- Step 4: 重置为新赛季（模式 2：继承初始分）
INSERT INTO arena_rank (player_id, season_id, rank_score)
SELECT player_id, new_season_id,
       GREATEST(800, rank_score / 2)  -- 初始分 = 旧分的一半，不低于 800
FROM arena_rank
WHERE season_id = old_season_id;
-- 使用 INSERT ... SELECT 批量创建新赛季数据

-- Step 5: 归档旧赛季数据
RENAME TABLE arena_rank TO arena_rank_season_old;
CREATE TABLE arena_rank LIKE arena_rank_template;
```

### 7.3 排行榜缓存

```python
# Redis 排行榜缓存 + 赛季切换

def get_arena_ranklist(season_id, top_n=100):
    """获取赛季排行榜"""
    key = f"arena:ranklist:{season_id}"

    # 尝试从缓存取
    cached = redis.zrevrange(key, 0, top_n - 1, withscores=True)
    if cached:
        return cached

    # 缓存未命中，从 MySQL 加载
    rows = db.query("""
        SELECT player_id, rank_score FROM arena_rank
        WHERE season_id = ? ORDER BY rank_score DESC LIMIT ?
    """, season_id, top_n)

    # 写入 Redis ZSET
    pipe = redis.pipeline()
    for row in rows:
        pipe.zadd(key, {row['player_id']: row['rank_score']})
    pipe.expire(key, 300)  # 5 分钟过期
    pipe.execute()

    return rows

def get_player_rank(player_id, season_id):
    """获取单个玩家排名"""
    key = f"arena:ranklist:{season_id}"
    rank = redis.zrevrank(key, player_id)
    if rank is not None:
        return rank + 1  # 0-based → 1-based
    # 缓存没有，查 MySQL
    row = db.query_one("""
        SELECT rank FROM arena_rank
        WHERE player_id = ? AND season_id = ?
    """, player_id, season_id)
    return row['rank'] if row else None
```

---

## 八、跨分片查询与聚合

### 8.1 跨分片查询类型

```
类型 1：跨分片 JOIN（最棘手）
  玩家表和道具表按 player_id 分片 → 同分片内 JOIN 没问题
  玩家表和公会表按不同键分片 → 需要两次查询

类型 2：全局聚合
  全服玩家统计、全服排行榜
  需要汇总所有分片的数据

类型 3：跨玩家操作
  A 给 B 发送邮件、A 和 B 交易
  如果 A 和 B 在不同分片 → 分布式事务
```

### 8.2 解决方案

```sql
-- 方案 1：汇总库（推荐，最简单）
-- 通过 Canal/Binlog 同步到单独的汇总库
-- 用于：全服排行榜、全局统计

-- MySQL -> Binlog -> Canal -> Kafka -> 汇总库

-- 汇总库表结构（按时间分批）
CREATE TABLE `global_daily_stats` (
  `stat_date` DATE NOT NULL,
  `dau` INT DEFAULT 0,
  `new_players` INT DEFAULT 0,
  `total_recharge` DECIMAL(10,2) DEFAULT 0,
  `avg_online_time` INT DEFAULT 0,
  PRIMARY KEY (`stat_date`)
);

-- 方案 2：分片广播查询
-- 应用层同时查询所有分片，合并结果

def get_global_ranklist(top_n=100):
    """
    全服排行榜（跨分片聚合）
    """
    # 1. 并行查询所有分片
    shards = [shard1, shard2, shard3, shard4]

    def query_shard(shard):
        return shard.query("""
            SELECT player_id, rank_score
            FROM arena_rank
            WHERE season_id = ?
            ORDER BY rank_score DESC
            LIMIT ?
        """, current_season, top_n)

    # 并行执行
    with ThreadPoolExecutor(4) as executor:
        results = list(executor.map(query_shard, shards))

    # 2. 合并排序（多路归并）
    import heapq
    merged = heapq.merge(*results,
                         key=lambda x: -x['rank_score'])
    # 3. 取 Top 100
    return list(merged)[:100]

-- 方案 3：分片友好的数据分布
-- 通过设计避免跨分片查询

-- 好友系统：同服好友尽量在同一分片
-- 分片策略：shard = (server_id * 10000 + player_id_in_server) % total_shards
-- 同一服务器的玩家集中到几个分片

-- 公会系统：公会主线数据走 guild_id 分片
-- 公会成员独立存玩家分片（读多写少，允许延迟）
```

### 8.3 分布式事务

```python
# 跨分片交易（两个玩家在不同分片）

class ShardTradeService:
    """
    跨分片物品交易（两阶段提交风格）
    """

    def trade(self, from_player_id, to_player_id, item_id, count):
        from_shard = self.get_shard(from_player_id)
        to_shard = self.get_shard(to_player_id)

        # 同分片处理（不需要分布式事务）
        if from_shard == to_shard:
            return self.same_shard_trade(from_shard, ...)

        # 跨分片：使用预扣+提交模式
        trade_id = self.create_trade_record(from_player_id, to_player_id)

        try:
            # 1. 在源分片预扣
            from_shard.exec("""
                UPDATE player_items
                SET quantity = quantity - ?, status = 'locked_for_trade'
                WHERE player_id = ? AND item_id = ? AND quantity >= ?
            """, count, from_player_id, item_id, count)

            if affected_rows == 0:
                self.rollback_trade(trade_id)
                return False

            # 2. 在目标分片增加
            to_shard.exec("""
                INSERT INTO player_items (player_id, item_template_id, quantity)
                VALUES (?, ?, ?)
            """, to_player_id, item_id, count)

            # 3. 提交交易（更新交易状态）
            self.commit_trade(trade_id)

            # 4. 清理锁定状态
            from_shard.exec("""
                UPDATE player_items SET status = 'normal'
                WHERE player_id = ? AND item_id = ? AND status = 'locked_for_trade'
            """, from_player_id, item_id)

            return True

        except Exception as e:
            # 4. 补偿事务（回滚）
            self.rollback_trade(trade_id)
            # 恢复源分片
            from_shard.exec("""
                UPDATE player_items SET quantity = quantity + ?, status = 'normal'
                WHERE player_id = ? AND item_id = ? AND status = 'locked_for_trade'
            """, count, from_player_id, item_id)
            return False

    def create_trade_record(self, from_id, to_id):
        """在交易日志中创建记录（独立数据库或同一个事务日志表）"""
        return db_trade_log.insert({
            "from_player": from_id,
            "to_player": to_id,
            "status": "pending",
            "create_time": datetime.now()
        })

    def commit_trade(self, trade_id):
        db_trade_log.update(trade_id, {"status": "committed"})

    def rollback_trade(self, trade_id):
        db_trade_log.update(trade_id, {"status": "rollback"})
```

---

## 停靠点

| 知识点 | 一句话总结 | 游戏开发启示 |
|--------|-----------|--------------|
| Schema Migration | 版本化控制表结构变更 | 使用数据库迁移工具（Flyway/Alembic）管理 |
| JSON 扩展字段 | MySQL 5.7+ 支持，用于存放不固定的小功能字段 | 避免频繁改表，但核心字段不要用 JSON |
| 道具系统 | 固定列存核心，JSON 存随机属性 | 用 UPDATE ... WHERE quantity>=? 做乐观锁 |
| 邮件系统 | 头表+收件表分离，游标分页 | 附件未领取的邮件不能物理删除 |
| 公会模型 | 双向关系表，反范式 member_count 减少 COUNT | 公会战匹配要排除正在交战中的对手 |
| 好友关系 | 双向存储，用 UNION 或 CASE 查询双向数据 | 好友列表配合 Redis Set + Hash 做缓存 |
| 战斗回放 | ProtoBuf + zlib 压缩，分区表按周清理 | 种子+Action 序列比全量帧存储节省 90% |
| 赛季重置 | INSERT ... SELECT 批量创建新赛季数据 | 赛季切换先用 Redis 缓存过渡 |
| 跨分片查询 | 汇总库或广播查询+合并排序 | 尽量通过设计避免跨分片操作 |
| 分布式事务 | 预扣+提交+补偿（Saga 模式） | 游戏业务容忍最终一致性，不要强 XA |


> 进阶方向：阅读《DDIA》的 "Data-Intensive Applications" 分区和事务章节，理解分布式数据系统的权衡。
