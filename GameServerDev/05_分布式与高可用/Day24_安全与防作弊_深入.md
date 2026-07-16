# Day 24：安全与防作弊 — 进阶深入

## 一、服务端权威模型深度实现

### 关键逻辑全在服务端

```
服务端权威模型三原则：
  1. 任何客户端声明都必须被服务端验证
  2. 所有游戏数值必须在服务端计算
  3. 客户端只负责展示和上报操作意图

游戏世界状态的所有权：
  客户端视角 → 输入操作意图（移动方向、使用技能）
  服务端视角 → 计算最终结果（最终位置、伤害数值）
```

```csharp
// 完整的服务端权威战斗系统
public class ServerAuthoritativeBattle
{
    // 客户端发送的只是"操作意图"，不是结果
    public async Task<BattleResult> ProcessClientAction(long playerId, ClientAction action)
    {
        // Step 1: 验证操作合法性
        if (!ValidateActionBasics(playerId, action))
            return BattleResult.Invalid("非法操作");

        var player = await GetPlayerState(playerId);
        var target = await GetTargetState(action.TargetId);

        // Step 2: 验证冷却时间（绝不信任客户端）
        if (!CheckCooldown(player, action.SkillId))
        {
            Log.Warning("玩家 {PlayerId} 尝试在冷却中使用技能 {SkillId}", playerId, action.SkillId);
            return BattleResult.Invalid("技能冷却中");
        }

        // Step 3: 验证资源消耗（绝不信任客户端）
        if (player.CurrentMp < GetSkillMpCost(action.SkillId))
        {
            Log.Warning("玩家 {PlayerId} MP不足仍尝试使用技能", playerId);
            return BattleResult.Invalid("MP不足");
        }

        // Step 4: 验证位置合法性（防穿墙）
        if (!IsPositionReachable(player.Position, action.TargetPosition, action.SkillId))
        {
            Log.Warning("玩家 {PlayerId} 位置异常: {Pos} → {Target}",
                playerId, player.Position, action.TargetPosition);
            // 强制拉回合法位置
            return BattleResult.TeleportTo(player.Position);
        }

        // Step 5: 服务器计算伤害（不信任客户端传来的伤害值）
        int serverDamaged = DamageCalculator.Calculate(
            attacker: player,
            defender: target,
            skillId: action.SkillId,
            // 使用服务器时间戳，不信任客户端的 timing
            serverTimestamp: Environment.TickCount64
        );

        // Step 6: 应用伤害
        target.Hp -= serverDamaged;

        // Step 7: 广播给所有相关客户端（不仅仅是施法者）
        await BroadcastBattleResult(playerId, target.Id, serverDamaged, action.SkillId);

        return BattleResult.Success(serverDamaged);
    }

    // 位置验证：防止传送/穿墙
    private bool IsPositionReachable(Vector3 currentPos, Vector3 targetPos, int skillId)
    {
        float distance = Vector3.Distance(currentPos, targetPos);
        float maxRange = GetSkillRange(skillId);

        if (distance > maxRange * 1.1f) // 允许 10% 偏差（网络延迟导致）
        {
            Log.Warning("技能距离异常: 技能范围={Range}, 实际距离={Dist}",
                maxRange, distance);
            return false;
        }

        // 穿墙检测（AOI 服务器有地图碰撞数据）
        if (HasObstacleBetween(currentPos, targetPos))
        {
            Log.Warning("检测到穿墙攻击");
            return false;
        }

        return true;
    }

    // 速度验证：防变速
    private bool ValidateMoveSpeed(long playerId, Vector3 oldPos, Vector3 newPos, long elapsedMs)
    {
        float distanceMoved = Vector3.Distance(oldPos, newPos);
        float speed = distanceMoved / (elapsedMs / 1000f); // 米/秒

        float maxSpeed = GetPlayerMaxSpeed(playerId);
        // 考虑网络延迟，允许一定浮动
        if (speed > maxSpeed * 1.5f)
        {
            Log.Error("玩家 {PlayerId} 速度异常: {Speed}m/s (最大 {Max})",
                playerId, speed, maxSpeed);
            return false;
        }
        return true;
    }
}
```

---

## 二、协议加密：AES-GCM + ECDH

### 为什么选择 AES-GCM

```
AES-GCM 的优势：
  - 认证加密（AEAD）：同时保证机密性和完整性
  - 不需要额外的 HMAC 签名（GCM 内置认证标签）
  - 比 AES-CBC + HMAC 更简单、更安全
  - 现代 CPU 有硬件加速（AES-NI 指令集）

AES-CBC 的问题：
  - 需要填充（Padding Oracle 攻击风险）
  - 需要额外的 HMAC 确保完整性
  - 容易实现错误
```

```csharp
// AES-256-GCM 加密实现
public class AesGcmPacketEncryption
{
    private readonly byte[] _key;
    private const int NonceSize = 12;  // GCM 推荐 96-bit nonce
    private const int TagSize = 16;    // GCM 认证标签

    public AesGcmPacketEncryption(byte[] key)
    {
        _key = key;
    }

    // 加密（nonce + ciphertext + tag）
    public byte[] Encrypt(byte[] plaintext)
    {
        byte[] nonce = GenerateNonce();
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(_key);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // 组包: nonce(12) + ciphertext(N) + tag(16)
        var packet = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, packet, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, packet, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, packet, NonceSize + ciphertext.Length, TagSize);

        return packet;
    }

    // 解密
    public byte[] Decrypt(byte[] packet)
    {
        if (packet.Length < NonceSize + TagSize)
            throw new CryptographicException("包太短");

        byte[] nonce = packet[..NonceSize];
        byte[] ciphertext = packet[NonceSize..^TagSize];
        byte[] tag = packet[^TagSize..];
        byte[] plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    private byte[] GenerateNonce()
    {
        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);
        return nonce;
    }
}

// ECDH 密钥交换
public class EcdhKeyExchange
{
    // 服务端持有长期私钥，客户端连接时协商会话密钥
    public async Task<byte[]> ServerHandshake(long playerId, byte[] clientPublicKey)
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        byte[] serverPublicKey = ecdh.PublicKey.ExportSubjectPublicKeyInfo();

        // 计算共享密钥
        byte[] sharedSecret = ecdh.DeriveKeyMaterial(
            ECDiffieHellmanPublicKey.Create(clientPublicKey));

        // 派生 AES 密钥（使用 HKDF）
        byte[] sessionKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            sharedSecret,
            salt: Encoding.UTF8.GetBytes($"game-session-{playerId}"),
            info: Encoding.UTF8.GetBytes("aes-256-gcm-key"),
            outputLength: 32);

        // 返回服务端公钥给客户端
        await SendToClient(serverPublicKey);

        return sessionKey;
    }
}
```

---

## 三、防重放攻击

```csharp
// 时间戳 + Nonce 防重放
public class ReplayAttackPrevention
{
    private readonly IDatabase _redis;
    private readonly long _maxTimeDriftMs = 30000; // 30 秒
    private readonly TimeSpan _nonceExpiry = TimeSpan.FromMinutes(5);

    // 验证请求是否合法（防止重放）
    public async Task<bool> ValidateRequest(long playerId, long timestamp, string nonce, byte[] signature)
    {
        // 1. 时间戳在合理范围内（防时间篡改）
        long serverNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long timeDiff = Math.Abs(serverNow - timestamp);

        if (timeDiff > _maxTimeDriftMs)
        {
            Log.Warning("请求时间戳偏差过大: diff={Diff}ms (max={Max})",
                timeDiff, _maxTimeDriftMs);
            return false;
        }

        // 2. Nonce 未使用过（防重放）
        string nonceKey = $"nonce:{playerId}:{nonce}";
        bool nonceUsed = await _redis.StringSetAsync(nonceKey, "1", _nonceExpiry, When.NotExists);
        if (!nonceUsed)
        {
            Log.Warning("检测到重放攻击: player={PlayerId}, nonce={Nonce}", playerId, nonce);
            return false;
        }

        // 3. 时间戳单调递增（防有序重放）
        string lastTsKey = $"last_ts:{playerId}";
        string lastTsStr = await _redis.StringGetAsync(lastTsKey);
        if (lastTsStr.HasValue)
        {
            long lastTs = long.Parse(lastTsStr);
            if (timestamp <= lastTs)
            {
                Log.Warning("时间戳不递增: new={New} <= last={Last}", timestamp, lastTs);
                return false;
            }
        }
        await _redis.StringSetAsync(lastTsKey, timestamp.ToString(), TimeSpan.FromMinutes(10));

        return true;
    }
}

// 游戏协议包完整结构
/*
Packet {
    Header {
        PlayerId:   uint64       // 玩家 ID
        Timestamp:  uint64       // 客户端时间戳（毫秒）
        Nonce:      byte[16]     // 随机数
        MsgId:      uint16       // 消息类型 ID
    }
    Body {
        ... message-specific data ...
    }
    Signature: byte[32]          // HMAC-SHA256(Header + Body, sessionKey)
}

服务器验证流程：
  1. 解析 Header
  2. 验证 Timestamp 在合理时间窗口内
  3. 检查 Nonce 是否已使用（Redis SET NX）
  4. 用 sessionKey 验证 Signature
  5. 反序列化 Body 执行逻辑
*/
```

---

## 四、统计异常检测

### 玩家行为画像

```csharp
public class StatisticalAnomalyDetector
{
    private readonly IDatabase _redis;

    // 为每个玩家建立行为基线
    public async Task BuildPlayerBaseline(long playerId)
    {
        // 收集过去 7 天的数据
        string key = $"stats:{playerId}:daily";

        var baseline = new PlayerBaseline
        {
            AvgDailyPlayTime = await CalculateAvg(playerId, "play_time", 7),
            StdDevDailyPlayTime = await CalculateStdDev(playerId, "play_time", 7),
            AvgExpPerHour = await CalculateAvg(playerId, "exp_per_hour", 7),
            StdDevExpPerHour = await CalculateStdDev(playerId, "exp_per_hour", 7),
            AvgGoldPerHour = await CalculateAvg(playerId, "gold_per_hour", 7),
            StdDevGoldPerHour = await CalculateStdDev(playerId, "gold_per_hour", 7),
            AvgActionPerMinute = await CalculateAvg(playerId, "actions_per_min", 7),
            StdDevActionPerMinute = await CalculateStdDev(playerId, "actions_per_min", 7)
        };

        // 存入 Redis（24 小时过期）
        await _redis.StringSetAsync($"baseline:{playerId}",
            JsonSerializer.Serialize(baseline), TimeSpan.FromDays(1));
    }

    // 实时检测异常
    public async Task<AnomalyResult> DetectAnomaly(long playerId, string metricName, double currentValue)
    {
        var baselineJson = await _redis.StringGetAsync($"baseline:{playerId}");
        if (!baselineJson.HasValue)
            return AnomalyResult.InsufficientData;

        var baseline = JsonSerializer.Deserialize<PlayerBaseline>(baselineJson);

        // z-score = (当前值 - 平均值) / 标准差
        double mean = GetMean(baseline, metricName);
        double std = GetStd(baseline, metricName);

        if (std < 0.001) std = 0.001; // 避免除零

        double zScore = Math.Abs((currentValue - mean) / std);

        if (zScore > 5.0)
        {
            Log.Error("玩家 {PlayerId} {Metric} 严重异常: Z-Score={ZScore:F1}, 当前={Cur}, 均值={Mean}",
                playerId, metricName, zScore, currentValue, mean);
            return AnomalyResult.HighlySuspicious;
        }

        if (zScore > 3.0)
        {
            Log.Warning("玩家 {PlayerId} {Metric} 异常: Z-Score={ZScore:F1}", playerId, metricName, zScore);
            return AnomalyResult.Suspicious;
        }

        return AnomalyResult.Normal;
    }
}
```

### 常见外挂检测模式

```csharp
public class CheatDetector
{
    // 速度外挂检测
    public async Task<bool> DetectSpeedHack(long playerId, MovementReport movement)
    {
        // 1. 直线速度检查
        double speed = movement.Distance / movement.ElapsedSeconds;
        if (speed > _config.MaxMoveSpeed * 1.5)
        {
            await RecordCheatEvidence(playerId, "speed_hack", $"速度 {speed:F1}m/s");
            return true;
        }

        // 2. 单位时间内移动距离检查
        string key = $"move:{playerId}:{DateTime.Now:yyyyMMddHHmm}";
        long totalDistThisMin = await _redis.StringIncrementAsync(key,
            (long)(movement.Distance * 100));
        await _redis.KeyExpireAsync(key, TimeSpan.FromMinutes(2));

        if (totalDistThisMin > _config.MaxMoveDistancePerMin * 100)
        {
            await RecordCheatEvidence(playerId, "speed_hack_distance",
                $"分钟移动距离 {totalDistThisMin / 100:F1}m");
            return true;
        }

        return false;
    }

    // 伤害外挂检测
    public async Task<bool> DetectDamageHack(long playerId, DamageReport damage)
    {
        // 服务器重算伤害后对比
        int serverCalculated = DamageCalculator.Calculate(
            damage.Attacker, damage.Defender, damage.SkillId);

        // 客户端声明的伤害 vs 服务器计算
        double ratio = (double)damage.ClaimedDamage / serverCalculated;

        if (ratio > 1.1) // 10% 以上差异
        {
            await RecordCheatEvidence(playerId, "damage_hack",
                $"声称 {damage.ClaimedDamage} vs 实际 {serverCalculated}");
            return true;
        }

        return false;
    }

    // 传送外挂检测
    public async Task<bool> DetectTeleportHack(long playerId, PositionReport pos)
    {
        var lastValidPos = await GetLastValidPosition(playerId);

        if (lastValidPos != null)
        {
            double distance = Vector3.Distance(lastValidPos.Value, pos.NewPosition);

            // 正常移动最大距离（考虑技能传送）
            double maxDistance = GetMaxPossibleMoveDistance(
                lastValidPos.Value, pos.NewPosition,
                pos.TimeSinceLastReport);

            if (distance > maxDistance && !IsSkillTeleport(playerId, pos))
            {
                await RecordCheatEvidence(playerId, "teleport_hack",
                    $"跳跃 {distance:F1}m (允许 {maxDistance:F1}m)");
                return true;
            }
        }

        await SavePosition(playerId, pos.NewPosition);
        return false;
    }

    // 冷却时间绕过检测
    public async Task<bool> DetectCooldownBypass(long playerId, int skillId)
    {
        string key = $"cooldown:{playerId}:{skillId}";
        var remaining = await _redis.StringGetAsync(key);

        if (remaining.HasValue && long.Parse(remaining) > Environment.TickCount64)
        {
            await RecordCheatEvidence(playerId, "cooldown_bypass",
                $"技能 {skillId} 在冷却中");
            return true;
        }

        return false;
    }

    private async Task RecordCheatEvidence(long playerId, string cheatType, string detail)
    {
        var evidence = new CheatEvidence
        {
            PlayerId = playerId,
            CheatType = cheatType,
            Detail = detail,
            Timestamp = DateTime.UtcNow,
            ServerId = _serverId
        };

        // 写证据库
        await _db.ExecuteAsync(
            @"INSERT INTO cheat_evidence (player_id, cheat_type, detail, server_id, created_at)
              VALUES (@PlayerId, @CheatType, @Detail, @ServerId, @CreatedAt)",
            evidence);

        // 累计异常分数
        await _redis.StringIncrementAsync($"cheat_score:{playerId}");

        // 达到阈值自动封禁
        string scoreStr = await _redis.StringGetAsync($"cheat_score:{playerId}");
        if (int.TryParse(scoreStr, out int score) && score >= 5)
        {
            await AutoBan(playerId, "自动封禁: 多次异常检测触发");
        }
    }
}
```

---

## 五、数据完整性校验

```csharp
// 关键数据的 HMAC 保护
public class DataIntegrityGuard
{
    private readonly byte[] _hmacKey;

    // 用 HMAC 保护玩家存档完整性
    public string ProtectSaveData(PlayerSaveData data)
    {
        var json = JsonSerializer.Serialize(data);

        using var hmac = new HMACSHA256(_hmacKey);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));

        // 返回 "数据.签名"
        return $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(json))}." +
               $"{Convert.ToHexString(hash)}";
    }

    // 加载时验证完整性
    public PlayerSaveData LoadAndVerify(string protectedData)
    {
        var parts = protectedData.Split('.');
        if (parts.Length != 2)
            throw new IntegrityException("数据格式错误");

        byte[] jsonBytes = Convert.FromBase64String(parts[0]);
        string receivedSig = parts[1];

        using var hmac = new HMACSHA256(_hmacKey);
        string computedSig = Convert.ToHexString(hmac.ComputeHash(jsonBytes));

        // 常量时间比较（防时序攻击）
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(receivedSig),
                Encoding.UTF8.GetBytes(computedSig)))
        {
            throw new IntegrityException("数据签名不匹配，可能被篡改");
        }

        return JsonSerializer.Deserialize<PlayerSaveData>(
            Encoding.UTF8.GetString(jsonBytes));
    }
}

// 检查玩家返回给客户端的敏感数据
// 不要在客户端响应中包含: playerId（防遍历）、internal_flags、作弊标记
```

---

## 六、审计日志深入

```csharp
// 敏感操作分类
public enum AuditOperation
{
    // 玩家管理
    BanPlayer,
    UnbanPlayer,
    MutePlayer,
    GrantItem,
    RemoveItem,
    GrantCurrency,
    ModifyLevel,
    ResetPassword,

    // 经济操作
    ManualRefund,
    ModifyRecharge,
    CreateCoupon,

    // 系统操作
    ServerRestart,
    ServerMaintenance,
    ConfigChange,
    ActivityToggle,

    //GM 管理
    CreateAdmin,
    ModifyPermission,
    DeleteAdmin
}

// 不可篡改的审计日志
public class TamperProofAuditLog
{
    // 使用链表哈希（每个日志条目包含前一条的哈希）
    // 保证日志不可篡改
    public async Task AppendLog(AuditEntry entry)
    {
        // 获取上一条的哈希
        string prevHash = await _db.QueryFirstOrDefaultAsync<string>(
            "SELECT hash FROM audit_chain ORDER BY id DESC LIMIT 1");

        entry.PrevHash = prevHash ?? "GENESIS";
        entry.Hash = ComputeHash(entry);

        await _db.ExecuteAsync(
            @"INSERT INTO audit_chain (admin_id, operation, target_id, detail, ip, prev_hash, hash, created_at)
              VALUES (@AdminId, @Operation, @TargetId, @Detail, @Ip, @PrevHash, @Hash, @CreatedAt)",
            entry);

        // 同时发送到单独的审计 Kafka Topic（独立存储，防止 DB 被篡改）
        await _auditProducer.ProduceAsync("audit-log",
            new Message<string, byte[]>
            {
                Key = entry.Id.ToString(),
                Value = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry))
            });
    }

    private string ComputeHash(AuditEntry entry)
    {
        var data = $"{entry.PrevHash}|{entry.AdminId}|{entry.Operation}|{entry.TargetId}|{entry.CreatedAt.Ticks}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data)));
    }

    // 验证审计链的完整性
    public async Task<bool> VerifyChain()
    {
        var entries = await _db.QueryAsync<AuditEntry>(
            "SELECT * FROM audit_chain ORDER BY id");

        string prevHash = "GENESIS";
        foreach (var entry in entries)
        {
            if (entry.PrevHash != prevHash)
                return false;

            string computedHash = ComputeHash(entry);
            if (entry.Hash != computedHash)
                return false;

            prevHash = entry.Hash;
        }
        return true;
    }
}
```

---

## 七、OWASP Top 10 游戏版

```yaml
# OWASP Top 10 在游戏服务器中的攻击面

# 1. 注入 (Injection)
#    攻击: SQL 注入通过聊天/昵称
#    防护: 参数化查询，ORM，禁止拼接 SQL
#    代码: await db.QueryAsync("SELECT * FROM players WHERE id = @Id", new { Id })

# 2. 失效的身份认证 (Broken Authentication)
#    攻击: Session 劫持、弱 Token
#    防护: JWT + 短过期时间 + Redis 黑名单

# 3. 敏感数据泄露 (Sensitive Data Exposure)
#    攻击: 客户端内存 dump 获取密钥
#    防护: 客户端不存密钥，动态下发会话密钥

# 4. XML 外部实体 (XXE)
#    攻击: 上传 XML 配置文件读取服务器文件
#    防护: 禁用 XML 外部实体，仅使用 JSON/Protobuf

# 5. 失效的访问控制 (Broken Access Control)
#    攻击: 修改请求中的 playerId 操作他人账号
#    防护: 每次请求验证 owner，不信任客户端 ID

# 6. 安全配置错误 (Security Misconfiguration)
#    攻击: 404 页面泄露服务器信息
#    防护: 最小权限原则，关闭调试端点

# 7. 跨站脚本 (XSS)
#    攻击: 聊天框注入 JS
#    防护: HTML 编码所有用户输入

# 8. 不安全的反序列化 (Insecure Deserialization)
#    攻击: 发送恶意序列化数据
#    防护: 使用 Protobuf（模式严格），验证输入

# 9. 使用含有已知漏洞的组件
#    攻击: 已知 CVE 的库版本
#    防护: 定期扫描依赖，保持更新

# 10. 日志与监控不足
#     攻击: 长期潜伏不被发现
#     防护: 完整审计日志 + 异常检测 + 告警
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 服务端权威 | 所有计算在服务端，客户端只发操作意图 |
| AES-GCM | 认证加密同时保证机密性和完整性 |
| ECDH | 椭圆曲线密钥交换，动态协商会话密钥 |
| Nonce 防重放 | 每个请求唯一 nonce，Redis SET NX 去重 |
| Z-Score 检测 | 基于玩家行为基线的统计异常检测 |
| 速度/伤害/传送检测 | 服务端校验位置距离、伤害计算、冷却时间 |
| HMAC 完整性 | 关键数据签名验证，防止篡改 |
| 审计链 | 链表哈希保证审计日志不可篡改 |
| OWASP Top 10 | 安全基线，游戏服务器也必须遵守 |
