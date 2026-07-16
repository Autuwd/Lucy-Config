# Day 15：游戏数据库设计

## 一、游戏数据的特点

| 特性 | 说明 | 设计影响 |
|------|------|---------|
| 高并发读 | 排行榜、玩家信息频繁读取 | 缓存层必不可少 |
| 写多 | 战斗日志、操作行为流 | 批量写入，异步落盘 |
| 数据关联 | 玩家→道具→任务 | 合理的外键/索引设计 |
| 冷热分离 | 活跃数据 vs 历史数据 | 分表/归档策略 |
| 一致性要求 | 经济系统容错低 | 事务+补偿 |

---

## 二、实体关系设计

### 核心实体

```
Account (1) ──→ Player (1) ──→ Inventory (N)
    │                │              └── Item (模板)
    │                ├── Quest (N)
    │                ├── Skill (N)
    │                ├── Friend (N)
    │                └── Mail (N)
    │
    └── Recharge (N)
```

### 完整表结构

```sql
-- ====================
-- 账号系统
-- ====================
CREATE TABLE `accounts` (
    `id`            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `username`      VARCHAR(64) NOT NULL COMMENT '用户名',
    `password_hash` VARCHAR(128) NOT NULL COMMENT '密码哈希',
    `salt`          VARCHAR(32) NOT NULL COMMENT '加盐',
    `status`        TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT '1正常 2冻结 3封禁',
    `register_ip`   VARCHAR(45) DEFAULT NULL COMMENT '注册IP',
    `register_at`   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `last_login_at` DATETIME DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_username` (`username`),
    KEY `idx_register_at` (`register_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='账号表';

-- ====================
-- 玩家系统
-- ====================
CREATE TABLE `players` (
    `id`            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `account_id`    BIGINT UNSIGNED NOT NULL,
    `server_id`     INT UNSIGNED NOT NULL COMMENT '所属服务器',
    `name`          VARCHAR(32) NOT NULL,
    `level`         INT UNSIGNED NOT NULL DEFAULT 1,
    `exp`           BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `vip_level`     INT UNSIGNED NOT NULL DEFAULT 0,
    `vip_exp`       BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `gold`          BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `diamond`       BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `total_recharge` BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '累计充值',
    `fight_power`   BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '战斗力',
    `avatar`        INT UNSIGNED NOT NULL DEFAULT 1 COMMENT '头像ID',
    `title`         VARCHAR(64) DEFAULT NULL COMMENT '称号',
    `guild_id`      BIGINT UNSIGNED DEFAULT NULL COMMENT '公会ID',
    `last_login_at` DATETIME DEFAULT NULL,
    `last_logout_at` DATETIME DEFAULT NULL,
    `created_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_account_server` (`account_id`, `server_id`),
    UNIQUE KEY `uk_name` (`name`, `server_id`),
    KEY `idx_level` (`level`),
    KEY `idx_fight_power` (`fight_power`),
    KEY `idx_guild_id` (`guild_id`),
    KEY `idx_last_login` (`last_login_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='玩家表';

-- ====================
-- 道具系统
-- ====================
CREATE TABLE `inventory` (
    `id`          BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `player_id`   BIGINT UNSIGNED NOT NULL,
    `item_id`     INT UNSIGNED NOT NULL COMMENT '道具模板ID',
    `count`       INT UNSIGNED NOT NULL DEFAULT 1,
    `is_equipped` TINYINT(1) NOT NULL DEFAULT 0,
    `bind_type`   TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0不绑定 1拾取绑定 2装备绑定',
    `quality`     TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT '1白 2绿 3蓝 4紫 5橙',
    `expire_at`   DATETIME DEFAULT NULL,
    `created_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_player_id` (`player_id`),
    KEY `idx_player_item` (`player_id`, `item_id`),
    KEY `idx_expire` (`expire_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='道具表';

-- ====================
-- 技能系统
-- ====================
CREATE TABLE `skills` (
    `id`          BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `player_id`   BIGINT UNSIGNED NOT NULL,
    `skill_id`    INT UNSIGNED NOT NULL COMMENT '技能模板ID',
    `level`       INT UNSIGNED NOT NULL DEFAULT 1,
    `slot`        INT UNSIGNED DEFAULT NULL COMMENT '装备的技能槽位',
    `exp`         BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '技能经验',
    `is_unlocked` TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (`id`),
    KEY `idx_player_id` (`player_id`),
    UNIQUE KEY `uk_player_skill` (`player_id`, `skill_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='技能表';

-- ====================
-- 任务系统
-- ====================
CREATE TABLE `quests` (
    `id`           BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `player_id`    BIGINT UNSIGNED NOT NULL,
    `quest_id`     INT UNSIGNED NOT NULL,
    `status`       TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0未接 1进行中 2可提交 3已完成',
    `progress`     INT UNSIGNED NOT NULL DEFAULT 0,
    `target`       INT UNSIGNED NOT NULL,
    `accepted_at`  DATETIME DEFAULT NULL,
    `completed_at` DATETIME DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_player_status` (`player_id`, `status`),
    KEY `idx_player_quest` (`player_id`, `quest_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='任务表';

-- ====================
-- 好友系统
-- ====================
CREATE TABLE `friends` (
    `id`            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `player_id`     BIGINT UNSIGNED NOT NULL COMMENT '玩家',
    `friend_id`     BIGINT UNSIGNED NOT NULL COMMENT '好友',
    `relationship`  TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0好友 1黑名单 2申请中',
    `intimacy`      INT UNSIGNED NOT NULL DEFAULT 0 COMMENT '亲密度',
    `created_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_player_friend` (`player_id`, `friend_id`),
    KEY `idx_player_id` (`player_id`),
    KEY `idx_friend_id` (`friend_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='好友表';

-- ====================
-- 公会系统
-- ====================
CREATE TABLE `guilds` (
    `id`           BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `name`         VARCHAR(32) NOT NULL,
    `owner_id`     BIGINT UNSIGNED NOT NULL COMMENT '会长',
    `level`        INT UNSIGNED NOT NULL DEFAULT 1,
    `member_count` INT UNSIGNED NOT NULL DEFAULT 1,
    `max_members`  INT UNSIGNED NOT NULL DEFAULT 50,
    `notice`       TEXT COMMENT '公告',
    `created_at`   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='公会表';

-- ====================
-- 邮件系统
-- ====================
CREATE TABLE `mails` (
    `id`          BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `from_id`     BIGINT UNSIGNED DEFAULT NULL COMMENT '发送者（NULL=系统）',
    `to_id`       BIGINT UNSIGNED NOT NULL COMMENT '接收者',
    `title`       VARCHAR(128) NOT NULL,
    `content`     TEXT,
    `attachments` JSON COMMENT '附件道具 [{itemId, count}]',
    `is_read`     TINYINT(1) NOT NULL DEFAULT 0,
    `is_claimed`  TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否已领取附件',
    `expire_at`   DATETIME NOT NULL COMMENT '过期时间',
    `created_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_to_id` (`to_id`),
    KEY `idx_to_unread` (`to_id`, `is_read`),
    KEY `idx_expire` (`expire_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='邮件表';

-- ====================
-- 充值记录
-- ====================
CREATE TABLE `recharge_logs` (
    `id`            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `account_id`    BIGINT UNSIGNED NOT NULL,
    `player_id`     BIGINT UNSIGNED NOT NULL,
    `order_id`      VARCHAR(64) NOT NULL COMMENT '平台订单号',
    `product_id`    VARCHAR(64) NOT NULL COMMENT '商品ID',
    `amount`        DECIMAL(10, 2) NOT NULL COMMENT '金额',
    `diamond`       INT UNSIGNED NOT NULL COMMENT '获得钻石',
    `bonus_diamond` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT '赠送钻石',
    `channel`       VARCHAR(32) NOT NULL COMMENT '支付渠道',
    `status`        TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT '1成功 2退款 3异常',
    `created_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_order_id` (`order_id`),
    KEY `idx_account_id` (`account_id`),
    KEY `idx_player_id` (`player_id`),
    KEY `idx_created_at` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='充值记录表';
```

---

## 三、范式与反范式

### 第一范式 (1NF)

```
字段不可再分
❌ address = "广东省广州市天河区"
✅ province = "广东", city = "广州", district = "天河区"
```

### 第二范式 (2NF)

```
非主键字段必须完全依赖主键
❌ inventory(id, player_id, item_id, player_name)
   player_name 只依赖 player_id，不是完全依赖主键
✅ 拆成 inventory(id, player_id, item_id) 和 players(id, name)
```

### 第三范式 (3NF)

```
非主键字段不能传递依赖
❌ players(id, guild_id, guild_name)  ← guild_name 通过 guild_id 传递依赖
✅ 拆成 players(id, guild_id) 和 guilds(id, name)
```

### 反范式设计（游戏服务器常用）

```sql
-- 反范式 1：玩家表冗余 guild_name
-- 理由：查看玩家信息时 99% 需要公会名，连表查询代价大
-- 代价：公会改名时需要更新所有成员
CREATE TABLE players (
    guild_id   BIGINT UNSIGNED,
    guild_name VARCHAR(32)  -- 冗余字段
);

-- 反范式 2：统计字段冗余
-- 理由：每次查询 COUNT 影响性能
CREATE TABLE guilds (
    member_count INT UNSIGNED NOT NULL DEFAULT 1  -- 冗余
);
```

### 反范式原则

```
何时反范式：
  1. 读极多，写极少（如公会名）
  2. 统计值（member_count、online_count）
  3. 连表成本高

代价：
  1. 更新时需要多表同步
  2. 可能数据不一致
  3. 占用更多存储

建议：先按规范设计，性能瓶颈处再反范式
```

---

## 四、分表策略

### 按 UID 分表

```sql
-- 玩家数据按 player_id 分表 256 张
-- 分表规则：player_id >> 20（前 20 位用来区分表，后 44 位用来区分行）

-- 分表函数
CREATE FUNCTION GetPlayerShard(player_id BIGINT) RETURNS INT
DETERMINISTIC
BEGIN
    RETURN player_id % 256;
END;

-- 路由
SELECT * FROM players_47 WHERE id = 1001;  -- 1001 % 256 = 47
```

### 冷热分离

```sql
-- 热数据表（活跃玩家，< 30 天）
players_hot (id, name, level, exp, gold, ...)

-- 冷数据表（历史玩家，>= 30 天未登录）
players_cold (id, name, level, exp, gold, ...)

-- 定时迁移（每天凌晨）
INSERT INTO players_cold SELECT * FROM players_hot
WHERE last_login_at < DATE_SUB(NOW(), INTERVAL 30 DAY);

DELETE FROM players_hot
WHERE last_login_at < DATE_SUB(NOW(), INTERVAL 30 DAY);
```

### 每天/每周分表

```sql
-- 日志类数据按时间分表
CREATE TABLE battle_logs_20260715 (
    id BIGINT AUTO_INCREMENT,
    attacker_id BIGINT,
    defender_id BIGINT,
    damage INT,
    created_at DATETIME
);

-- 路由：根据日期选择表
public string GetLogTable(DateTime date)
{
    return $"battle_logs_{date:yyyyMMdd}";
}
```

---

## 五、数据迁移与回滚

```csharp
// 版本化迁移
class DatabaseMigrator
{
    private readonly string _connectionString;

    public async Task Migrate()
    {
        int currentVersion = await GetCurrentVersion();
        int targetVersion = GetTargetVersion();

        for (int v = currentVersion + 1; v <= targetVersion; v++)
        {
            Log.Information("执行迁移 V{v}", v);

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                await ApplyMigration(conn, tx, v);
                await SetVersion(conn, tx, v);
                await tx.CommitAsync();

                Log.Information("迁移 V{v} 完成", v);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Log.Error(ex, "迁移 V{v} 失败，已回滚", v);
                throw;
            }
        }
    }

    private async Task ApplyMigration(MySqlConnection conn,
        MySqlTransaction tx, int version)
    {
        string sql = version switch
        {
            1 => "ALTER TABLE players ADD COLUMN title VARCHAR(64) AFTER name;",
            2 => "CREATE INDEX idx_guild_id ON players(guild_id);",
            3 => @"CREATE TABLE `recharge_logs` (
                    `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                    `player_id` BIGINT UNSIGNED NOT NULL,
                    `amount` DECIMAL(10,2),
                    PRIMARY KEY (`id`)
                );",
            _ => throw new NotSupportedException($"未知版本 {version}")
        };

        await conn.ExecuteAsync(sql, tx);
    }

    // 迁移脚本文件
    // /migrations/V001__add_title.sql
    // /migrations/V002__add_index.sql
    // /migrations/V003__create_recharge_logs.sql
}
```

---

## 六、对比客户端数据存储

| 特性 | 客户端 (Unity) | 服务端 (MySQL) |
|------|---------------|---------------|
| 数据量 | 玩家自己的存档 | 所有玩家 × 所有数据 |
| 存储 | PlayerPrefs / SQLite / 文件 | MySQL 集群 |
| 查询 | 加载全部到内存 | SQL 查询，索引优化 |
| 一致性 | 本地单用户 | ACID 事务 |
| 安全性 | 可能被篡改 | 服务器信任模型 |
| 备份 | 不重要 | 灾备必须 |
| 迁移 | 版本兼容本地 | DDL 迁移脚本 |

---

## 七、练习

1. **画 ER 图**：设计一个 MMO 游戏的完整 ER 图（至少 8 个实体）
2. **建表**：根据 ER 图写 CREATE TABLE（含索引和外键约束）
3. **反范式分析**：找出上述设计中的反范式机会并说明理由
4. **迁移脚本**：写 3 个版本迁移 SQL 脚本（含回滚）
5. **数据缓存策略**：设计玩家数据的缓存架构（MySQL + Redis + 本地缓存）

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 范式 | 减少数据冗余，降低不一致风险 |
| 反范式 | 用冗余换查询性能 |
| 冷热分离 | 活跃数据和历史数据分开存储 |
| 分表 | 按 ID/时间拆大表，控制单表数据量 |
| 迁移脚本 | 版本化管理数据库变更 |
| 回滚 | 迁移失败能还原到上一版本 |
