# Day 24：安全与防作弊

## 一、游戏服务器安全模型

### 基本原则

```
1. 服务器是权威，客户端只是建议
   - 所有重要判断在服务器执行
   - 客户端传来的数据都不可信

2. 最小信任原则
   - 不信任客户端的任何数据
   - 不信任其他服务的默认状态

3. 防御深度
   - 多层防护，不依赖单点
```

### 信任边界

```
可信区域：
  ┌────────────────────────────────────┐
  │  游戏服务器 (GameServer)           │
  │  ┌──────────┐  ┌──────────┐       │
  │  │ 逻辑服务器 │  │ 数据库服务器 │       │
  │  └──────────┘  └──────────┘       │
  │       └──────────┬──────────┘       │
  └──────────────────┼─────────────────┘
                      │ gRPC / TCP (内部)
  ┌──────────────────┼─────────────────┐
  │  网关 (Gateway)   │                 │
  │       ┌──────────┘                 │
  │       │ 协议转换 + 限流 + 校验       │
  └───────┼────────────────────────────┘
          │ WebSocket / TCP (外部)
  ┌───────┴────────────────────────────┐
  │  客户端 (Client)           不可信！  │
  │  - 可能修改内存/注入代码            │
  │  - 可能修改请求内容                │
  │  - 可能加速/减速/暂停游戏           │
  └────────────────────────────────────┘
```

---

## 二、数据校验

### 服务端验证所有数据

```csharp
public class ServerValidation
{
    // ❌ 错误：相信客户端发送的伤害值
    public async Task ApplyDamage(long attackerId, long targetId, int damage)
    {
        var target = await GetPlayer(targetId);
        target.Hp -= damage; // 如果客户端发送了 999999 伤害？
    }

    // ✅ 正确：服务器重新计算伤害
    public async Task<DamageResult> ApplyDamage(DamageRequest clientRequest)
    {
        // 忽略客户端传来的伤害值，服务器重新算
        var attacker = await GetCombatUnit(clientRequest.AttackerId);
        var target = await GetCombatUnit(clientRequest.TargetId);
        var skillDef = await GetSkillDef(clientRequest.SkillId);

        // 服务器计算伤害
        var damage = DamageCalculator.Calculate(attacker, target, skillDef);

        // 服务器计算 Buff
        // 服务器计算暴击/闪避

        return damage;
    }
}
```

### 频率限制

```csharp
public class FrequencyValidator
{
    private readonly IDatabase _redis;

    // 检查操作频率是否合理
    public async Task<bool> ValidateActionFrequency(long playerId, string action)
    {
        string key = $"freq:{playerId}:{action}";

        long count = await _redis.StringIncrementAsync(key);
        if (count == 1)
            await _redis.KeyExpireAsync(key, TimeSpan.FromSeconds(1));

        // 每秒最多 10 次相同操作
        if (count > 10)
        {
            Log.Warning("玩家 {PlayerId} 操作 {Action} 过于频繁 ({Count}/s)",
                playerId, action, count);
            return false;
        }

        return true;
    }

    // 检查异常操作模式
    public async Task<bool> DetectAnomaly(long playerId, string action)
    {
        // 记录操作到滑动窗口
        string key = $"anomaly:{playerId}";
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _redis.SortedSetAddAsync(key, $"{action}:{Guid.NewGuid()}", now);
        await _redis.SortedSetRemoveRangeByScoreAsync(key, 0, now - 60); // 保留 60 秒
        await _redis.KeyExpireAsync(key, TimeSpan.FromSeconds(120));

        // 检查 60 秒内操作数
        long count = await _redis.SortedSetLengthAsync(key);
        if (count > 100)
        {
            Log.Error("玩家 {PlayerId} 异常操作 ({Count}/min)", playerId, count);
            return false;
        }

        return true;
    }
}
```

### 数值范围校验

```csharp
public class RangeValidator
{
    // 所有从客户端收到的数值都必须校验范围
    public bool ValidateMoveDirection(Vector2 direction)
    {
        // 移动方向必须是单位向量
        float length = (float)Math.Sqrt(
            direction.X * direction.X + direction.Y * direction.Y);
        return Math.Abs(length - 1.0f) < 0.01f;
    }

    public bool ValidatePosition(Vector3 position)
    {
        // 位置必须在合理范围内
        return position.X >= _mapMinX && position.X <= _mapMaxX
            && position.Y >= _mapMinY && position.Y <= _mapMaxY
            && position.Z >= _mapMinZ && position.Z <= _mapMaxZ;
    }

    public bool ValidateSpeed(float speed)
    {
        // 速度不可能超过最大值
        return speed >= 0 && speed <= _maxMoveSpeed;
    }

    // 行为合理性检查
    public bool ValidatePlayerAction(long playerId, PlayerAction action)
    {
        // 冷却时间检查
        var cooldown = GetCooldown(playerId, action.ActionId);
        if (cooldown > 0)
        {
            Log.Warning("玩家 {PlayerId} 在冷却中({Cooldown}ms)使用了技能 {ActionId}",
                playerId, cooldown, action.ActionId);
            return false;
        }

        // 资源检查
        var player = GetPlayer(playerId);
        if (player.CurrentMp < action.MpCost)
        {
            Log.Warning("玩家 {PlayerId} MP 不足({Have}/{Need})使用了技能",
                playerId, player.CurrentMp, action.MpCost);
            return false;
        }

        return true;
    }
}
```

---

## 三、协议加密

### 加密层次

```
网络层加密：TLS (wss://)
  ↓ 防止窃听和篡改
协议层加密：AES + 动态密钥
  ↓ 防止协议分析和重放
数据层签名：HMAC
  ↓ 防篡改关键数据
```

### 对称加密

```csharp
public class PacketEncryption
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    // 使用 AES-256-CBC 加密协议数据
    public byte[] Encrypt(byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

        cs.Write(plaintext, 0, plaintext.Length);
        cs.FlushFinalBlock();

        return ms.ToArray();
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();

        cs.CopyTo(result);
        return result.ToArray();
    }
}
```

### 动态密钥交换

```csharp
public class KeyExchange
{
    // 使用 ECDH 或 RSA 交换密钥
    // 不直接在配置中写死密钥

    // 简化版：基于时间的一次性密钥
    public byte[] GenerateSessionKey(long playerId)
    {
        uint timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        uint interval = timestamp / 300; // 每 5 分钟变化

        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(_masterKey));
        byte[] hash = hmac.ComputeHash(
            BitConverter.GetBytes(interval)
            .Concat(BitConverter.GetBytes(playerId))
            .ToArray());

        // 取前 16 字节作为 AES 密钥
        return hash.Take(16).ToArray();
    }
}
```

### 消息完整性 (HMAC)

```csharp
public class PacketSigner
{
    public byte[] Sign(byte[] packet, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] signature = hmac.ComputeHash(packet);

        // 将签名附加到消息末尾
        return packet.Concat(signature).ToArray();
    }

    public bool Verify(byte[] signedPacket, byte[] key)
    {
        if (signedPacket.Length < 32)
            return false;

        int msgLen = signedPacket.Length - 32;
        byte[] message = signedPacket.Take(msgLen).ToArray();
        byte[] signature = signedPacket.Skip(msgLen).ToArray();

        using var hmac = new HMACSHA256(key);
        byte[] expected = hmac.ComputeHash(message);

        return CryptographicOperations.FixedTimeEquals(signature, expected);
    }
}
```

---

## 四、反外挂策略

### 常见的游戏外挂类型

| 外挂类型 | 原理 | 服务器防御 |
|---------|------|-----------|
| 变速齿轮 | 修改游戏时间 | 服务器时间戳校验 |
| 全图秒杀 | 修改伤害包 | 服务器计算伤害 |
| 透视/穿墙 | 修改场景数据 | AOI 服务端控制视野 |
| 自动操作 | 脚本模拟操作 | 行为模式检测 |
| 内存修改 | 修改客户端变量 | 关键计算在服务器 |
| 封包修改 | 篡改网络包 | 协议加密 + 签名 |

### 时间戳校验（防加速）

```csharp
public class TimeValidation
{
    private const long MaxTimeDriftMs = 5000; // 允许 5 秒偏差

    public bool ValidateActionTimestamp(long playerId, long clientTimestamp)
    {
        var previous = _playerLastTimestamps.GetOrAdd(playerId, _ => 0L);

        long serverNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 检查时间戳是否在合理范围内
        if (Math.Abs(serverNow - clientTimestamp) > MaxTimeDriftMs)
        {
            Log.Warning("玩家 {PlayerId} 时间戳异常: client={Client}, server={Server}",
                playerId, clientTimestamp, serverNow);
            return false;
        }

        // 检查时间戳是否在递增（防重放）
        if (clientTimestamp <= previous)
        {
            Log.Warning("玩家 {PlayerId} 时间戳不递增: current={Cur}, previous={Prev}",
                playerId, clientTimestamp, previous);
            return false;
        }

        _playerLastTimestamps[playerId] = clientTimestamp;
        return true;
    }

    // 服务端游戏逻辑使用服务器时间，不依赖客户端
    public void UpdateCooldown(long playerId, int skillId, int cooldownMs)
    {
        long serverTime = Environment.TickCount64;
        string key = $"cooldown:{playerId}:{skillId}";
        _redis.StringSetAsync(key, serverTime.ToString(),
            TimeSpan.FromMilliseconds(cooldownMs));
    }
}
```

### 行为模式检测

```csharp
public class BehaviorDetector
{
    private readonly IDatabase _redis;

    // 检测自动脚本
    public async Task<bool> IsAutomationSuspected(long playerId)
    {
        // 收集玩家操作统计
        var stats = await GetPlayerStats(playerId);

        // 1. 操作间隔过于均匀（人做不到）
        if (stats.ActionIntervals.All(i => Math.Abs(i - stats.AverageInterval) < 5))
        {
            Log.Warning("玩家 {PlayerId} 操作间隔过于均匀，疑似脚本", playerId);
            return true;
        }

        // 2. 鼠标/触摸位置太精确
        // 3. 响应时间太快（人类反应 > 100ms）
        if (stats.AverageReactionTime < 50)
        {
            Log.Warning("玩家 {PlayerId} 反应时间异常 ({Time}ms)", playerId, stats.AverageReactionTime);
            return true;
        }

        return false;
    }

    // 检测机器人（自动练级）
    public async Task<bool> IsGoldFarmingSuspected(long playerId)
    {
        // 统计 24 小时在线时长
        string key = $"playtime:{playerId}:{DateTime.Now:yyyyMMdd}";
        var playTime = await _redis.StringGetAsync(key);

        if (int.TryParse(playTime, out int minutes) && minutes > 1440) // > 24h
        {
            Log.Error("玩家 {PlayerId} 24 小时在线 {Minutes} 分钟，疑似挂机", playerId, minutes);
            return true;
        }

        // 检测重复路径
        var movementPattern = await GetMovementPattern(playerId);
        if (HasRepeatingPattern(movementPattern))
        {
            Log.Warning("玩家 {PlayerId} 移动路径高度重复", playerId);
            return true;
        }

        return false;
    }
}
```

### 操作回滚 (Detect & Rollback)

```csharp
public class AntiCheatRollback
{
    // 检测到异常操作后回滚
    public async Task RollbackPlayer(long playerId, DateTime since)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // 1. 记录该玩家数据快照
            var snapshot = await TakeSnapshot(playerId);

            // 2. 回滚玩家数据到上一个合法状态
            await RestoreSnapshot(snapshot);

            // 3. 记录操作日志
            await conn.ExecuteAsync(
                "INSERT INTO anti_cheat_logs (player_id, action, created_at) " +
                "VALUES (@P, 'ROLLBACK', NOW())",
                new { P = playerId }, tx);

            await tx.CommitAsync();

            Log.Error("玩家 {PlayerId} 数据已回滚至 {Time}", playerId, since);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
```

---

## 五、审计日志

```csharp
public class AuditLogger
{
    // 敏感操作必须记录审计日志
    public async Task LogAudit(string adminName, string action,
        long targetPlayerId, string details)
    {
        var entry = new AuditEntry
        {
            AdminName = adminName,
            Action = action,
            TargetPlayerId = targetPlayerId,
            Details = details,
            IpAddress = GetCallerIp(),
            Timestamp = DateTime.UtcNow
        };

        // 写入专门的审计日志表（不能修改）
        await _db.ExecuteAsync(
            "INSERT INTO audit_logs (admin_name, action, target_player_id, details, ip, created_at) " +
            "VALUES (@AdminName, @Action, @TargetPlayerId, @Details, @Ip, @Timestamp)",
            entry);

        // 同时记录到 Elasticsearch 方便搜索
        await _elasticsearch.IndexAsync(entry, idx => idx.Index("audit-logs"));
    }
}

// 审计日志表结构
/*
CREATE TABLE audit_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    admin_name VARCHAR(64) NOT NULL,
    action VARCHAR(32) NOT NULL,      -- GRANT_ITEM, BAN_PLAYER, SEND_MAIL
    target_player_id BIGINT,
    details TEXT,
    ip VARCHAR(45),
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    KEY idx_admin (admin_name),
    KEY idx_action (action),
    KEY idx_created (created_at)
) ENGINE=InnoDB;
*/
```

---

## 六、练习

1. **服务端验证**：找出一个常见的"相信客户端"的漏洞并修复
2. **数据加密**：为游戏协议实现 AES 加密 + HMAC 签名
3. **频次检测**：实现 5 秒内同一技能最多使用 3 次的检查
4. **异常检测**：实现"玩家每分钟获得经验超过阈值"的检测器
5. **审计日志**：设计 GM 工具操作的审计日志方案

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 服务器权威 | 客户端数据不可信，所有计算在服务端 |
| 频率限制 | 阻止自动脚本的暴力操作 |
| 协议加密 | AES + 动态密钥，防止封包修改 |
| HMAC 签名 | 验证消息完整性，防篡改 |
| 时间戳校验 | 防变速齿轮和服务端时间攻击 |
| 行为检测 | 分析操作模式识别自动脚本 |
| 审计日志 | 所有敏感操作可追溯 |
