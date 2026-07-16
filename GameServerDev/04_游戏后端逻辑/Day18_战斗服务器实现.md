# Day 18：战斗服务器实现

## 一、服务器在战斗中的角色

### 端游 vs 手游的服务器角色

| 模式 | 服务器角色 | 客户端角色 | 例 |
|------|-----------|-----------|-----|
| 帧同步 | 广播帧输入，最终验证 | 渲染+表现同步 | 格斗游戏、RTS |
| 状态同步 | 计算所有状态变化 | 表现+操作上报 | MMO、RPG |
| 权威服务器 | 全权计算战斗逻辑 | 只负责表现 | 几乎所有商业游戏 |

**权威服务器模式**：服务器是战斗的"法官"，客户端只能"建议"。所有伤害、技能效果必须由服务器计算和批准。

---

## 二、技能释放流程

### 完整流程

```
Client → 释放技能请求(技能ID, 目标)
    │
    ├── 服务器验证：
    │   ├── 技能是否冷却中？
    │   ├── 消耗是否足够（MP/怒气/道具）？
    │   ├── 目标是否在范围内？
    │   ├── 目标是否有效（存活/合法）？
    │   └── 玩家状态是否允许（眩晕/沉默中）？
    │
    ├── 扣除消耗（MP/道具等）
    ├── 设置技能冷却
    │
    ├── 计算技能效果：
    │   ├── 伤害数值
    │   ├── 是否暴击/闪避
    │   ├── Buff 添加/移除
    │   └── 特殊效果（击退/眩晕）
    │
    └── 广播结果给所有相关客户端
        ├── 释放者：看到完整效果
        ├── 目标：看到受到伤害
        └── 附近玩家：看到技能动画
```

### 代码实现

```csharp
public class SkillService
{
    private readonly ISkillRepository _skillRepo;
    private readonly IBuffService _buffService;
    private readonly IDamageCalculator _damageCalc;

    // 技能释放请求
    public async Task<SkillResult> CastSkill(CastSkillRequest req)
    {
        // 1. 获取技能模板
        var skillDef = await _skillRepo.GetSkillDef(req.SkillId);
        if (skillDef == null)
            return SkillResult.Failed("技能不存在");

        // 2. 获取释放者状态
        var caster = await GetCombatUnit(req.CasterId);
        if (caster.IsDead)
            return SkillResult.Failed("已死亡");

        // 3. 冷却检查
        var cooldown = await GetCooldown(req.CasterId, req.SkillId);
        if (cooldown > 0)
            return SkillResult.Failed($"冷却中，剩余 {cooldown}ms");

        // 4. 消耗检查
        if (caster.Mp < skillDef.MpCost)
            return SkillResult.Failed("MP 不足");

        // 5. 获取目标
        var target = await GetCombatUnit(req.TargetId);
        if (target == null || target.IsDead)
            return SkillResult.Failed("目标无效");

        // 6. 范围检查
        float distance = Vector3.Distance(caster.Position, target.Position);
        if (distance > skillDef.Range)
            return SkillResult.Failed("超出范围");

        // 7. 状态检查（是否被控制）
        if (caster.HasBuff(BuffType.Stun) || caster.HasBuff(BuffType.Silence))
            return SkillResult.Failed("无法释放技能");

        // 8. 扣除消耗
        caster.Mp -= skillDef.MpCost;
        await UpdateCombatUnit(caster);

        // 9. 设置冷却
        await SetCooldown(req.CasterId, req.SkillId, skillDef.CooldownMs);

        // 10. 计算伤害
        var damage = _damageCalc.Calculate(caster, target, skillDef);

        // 11. 应用伤害
        target.Hp -= damage.FinalDamage;
        if (target.Hp <= 0)
        {
            target.Hp = 0;
            target.IsDead = true;
            await OnUnitDeath(target, caster);
        }
        await UpdateCombatUnit(target);

        // 12. 应用 Buff
        foreach (var buffDef in skillDef.Buffs)
        {
            await _buffService.ApplyBuff(target, caster, buffDef);
        }

        // 13. 返回结果
        return new SkillResult
        {
            Success = true,
            CasterId = req.CasterId,
            TargetId = req.TargetId,
            SkillId = req.SkillId,
            Damage = damage.FinalDamage,
            IsCrit = damage.IsCrit,
            IsDodge = damage.IsDodge,
            CasterRemainHp = caster.Hp,
            TargetRemainHp = target.Hp,
            Effects = damage.Effects,
        };
    }

    // 获取冷却
    private async Task<long> GetCooldown(long unitId, int skillId)
    {
        string key = $"cooldown:{unitId}:{skillId}";
        var ttl = await _redis.KeyTimeToLiveAsync(key);
        return ttl?.TotalMilliseconds ?? 0;
    }

    // 设置冷却
    private async Task SetCooldown(long unitId, int skillId, long cooldownMs)
    {
        string key = $"cooldown:{unitId}:{skillId}";
        await _redis.StringSetAsync(key, "1", TimeSpan.FromMilliseconds(cooldownMs));
    }
}
```

---

## 三、伤害计算公式

### 通用公式

```csharp
public class DamageCalculator
{
    // 基础物理伤害
    public int CalculatePhysicalDamage(CombatUnit attacker, CombatUnit defender, SkillDef skill)
    {
        // 基础攻击力
        float baseAttack = attacker.Attack;
        
        // 技能倍率
        float skillMultiplier = skill.DamageMultiplier;
        
        // 防御减免
        float defense = defender.Defense;
        float defenseFactor = 100f / (100f + defense); // 递增递减公式
        
        // 浮动（90%~110%）
        float randomFactor = 0.9f + (float)Random.Shared.NextDouble() * 0.2f;
        
        // 基础伤害
        float rawDamage = baseAttack * skillMultiplier * defenseFactor * randomFactor;
        
        return (int)Math.Max(1, rawDamage); // 至少 1 点伤害
    }

    // 魔法伤害
    public int CalculateMagicDamage(CombatUnit attacker, CombatUnit defender, SkillDef skill)
    {
        float baseMagic = attacker.MagicAttack;
        float skillMultiplier = skill.DamageMultiplier;
        float resist = defender.MagicResist;
        float resistFactor = 100f / (100f + resist);
        float randomFactor = 0.9f + (float)Random.Shared.NextDouble() * 0.2f;

        float rawDamage = baseMagic * skillMultiplier * resistFactor * randomFactor;
        return (int)Math.Max(1, rawDamage);
    }

    // 完整计算（含暴击/闪避/增伤减伤）
    public DamageResult Calculate(CombatUnit attacker, CombatUnit defender, SkillDef skill)
    {
        var result = new DamageResult();

        // 1. 闪避判定
        float dodgeChance = defender.Dodge - attacker.Hit;
        if (Random.Shared.NextDouble() < dodgeChance)
        {
            result.IsDodge = true;
            result.FinalDamage = 0;
            result.DamageType = DamageType.Miss;
            return result;
        }

        // 2. 计算基础伤害
        int baseDamage = skill.DamageType == DamageType.Physical
            ? CalculatePhysicalDamage(attacker, defender, skill)
            : CalculateMagicDamage(attacker, defender, skill);

        // 3. 暴击判定
        float critChance = attacker.Crit - defender.CritResist;
        bool isCrit = Random.Shared.NextDouble() < critChance;

        int finalDamage = baseDamage;
        if (isCrit)
        {
            finalDamage = (int)(baseDamage * attacker.CritDamage);
            result.IsCrit = true;
        }

        // 4. 增伤/减伤修正
        finalDamage = (int)(finalDamage * attacker.DamageIncrease);
        finalDamage = (int)(finalDamage * (1 - defender.DamageReduction));

        // 5. 最终伤害
        result.FinalDamage = finalDamage;
        result.DamageType = skill.DamageType;
        return result;
    }
}

public class DamageResult
{
    public int FinalDamage { get; set; }
    public bool IsCrit { get; set; }
    public bool IsDodge { get; set; }
    public DamageType DamageType { get; set; }
    public List<EffectInfo> Effects { get; set; } = new();
}

public enum DamageType
{
    Physical,
    Magic,
    True,       // 真实伤害（无视防御）
    Miss        // 未命中
}
```

---

## 四、Buff 系统

```csharp
public class BuffService
{
    private readonly IDatabase _redis;

    // 应用 Buff
    public async Task ApplyBuff(CombatUnit target, CombatUnit caster, BuffDef buffDef)
    {
        // 检查是否可叠加
        if (buffDef.MaxStack > 1)
        {
            // 叠加：增加层数
            string stackKey = $"buff:stack:{target.Id}:{buffDef.Id}";
            await _redis.StringIncrementAsync(stackKey);
            await _redis.KeyExpireAsync(stackKey, TimeSpan.FromMilliseconds(buffDef.DurationMs));
        }
        else if (!buffDef.CanOverlap)
        {
            // 不可叠加，检查是否已有
            string key = $"buff:{target.Id}:{buffDef.Id}";
            bool exists = await _redis.KeyExistsAsync(key);
            if (exists) return; // 已有，跳过
        }

        // 设置 Buff
        string buffKey = $"buff:{target.Id}:{buffDef.Id}";
        var buffData = new BuffInstance
        {
            BuffId = buffDef.Id,
            CasterId = caster.Id,
            StartTime = Environment.TickCount64,
            Duration = buffDef.DurationMs
        };
        await _redis.StringSetAsync(buffKey,
            JsonSerializer.Serialize(buffData),
            TimeSpan.FromMilliseconds(buffDef.DurationMs));

        // 立即应用即时效果
        ApplyImmediateEffect(target, buffDef);

        // 如果是周期性效果，注册定时器
        if (buffDef.TickInterval > 0)
        {
            RegisterTickEffect(target, buffDef);
        }
    }

    // 移除 Buff
    public async Task RemoveBuff(CombatUnit unit, int buffId)
    {
        string key = $"buff:{unit.Id}:{buffId}";
        var data = await _redis.StringGetAsync(key);
        if (!data.HasValue) return;

        var buff = JsonSerializer.Deserialize<BuffInstance>(data);
        await _redis.KeyDeleteAsync(key);

        // 应用移除效果
        OnBuffRemove(unit, buff);
    }

    // 检查是否有特定类型的 Buff
    public bool HasBuff(CombatUnit unit, BuffType type)
    {
        // 遍历当前所有 Buff
        var keys = // 从 Redis 或内存中获取
        return keys.Any(k => k.Type == type);
    }

    // 周期性效果（如中毒）
    private async void TickEffect(object state)
    {
        var tickInfo = (BuffTickInfo)state;
        var unit = await GetCombatUnit(tickInfo.UnitId);

        if (unit == null || unit.IsDead) return;

        // 应用每跳效果
        unit.Hp -= tickInfo.TickDamage;
        await UpdateCombatUnit(unit);
    }

    // Buff 类型
    public enum BuffType
    {
        None = 0,
        Stun,       // 眩晕
        Silence,    // 沉默
        Poison,     // 中毒
        Heal,       // 持续回血
        Shield,     // 护盾
        SpeedUp,    // 加速
        SpeedDown,  // 减速
        Invincible, // 无敌
    }
}

public class BuffDef
{
    public int Id { get; set; }
    public BuffType Type { get; set; }
    public int DurationMs { get; set; }
    public int MaxStack { get; set; } = 1;
    public bool CanOverlap { get; set; } = true;
    public int TickInterval { get; set; } // 毫秒
    public int TickDamage { get; set; }
}

public class BuffInstance
{
    public int BuffId { get; set; }
    public long CasterId { get; set; }
    public long StartTime { get; set; }
    public long Duration { get; set; }
}
```

---

## 五、状态同步 vs 帧同步

### 状态同步

```
服务器每帧/间隔计算所有玩家的状态
发送变化给客户端

优点：
  - 服务器完全信任
  - 反外挂容易
  - 网络波动影响小
  
缺点：
  - 服务器负载高
  - 大量状态广播
  
场景：MMORPG
```

### 帧同步

```
服务器只转发玩家的操作指令
每个客户端用相同的逻辑计算相同的状态

优点：
  - 服务器负载轻
  - 带宽占用小
  - 精确回放

缺点：
  - 反外挂困难
  - 必须确定性逻辑
  - 网络要求高

场景：MOBA、格斗、RTS
```

```csharp
// 帧同步
class FrameSyncServer
{
    private List<PlayerInput> _inputBuffer = new();
    private int _frameNumber = 0;

    // 接收玩家输入（每秒 15 帧）
    public void OnPlayerInput(long playerId, byte[] input)
    {
        lock (_inputBuffer)
        {
            _inputBuffer.Add(new PlayerInput
            {
                PlayerId = playerId,
                Frame = _frameNumber,
                Input = input
            });
        }
    }

    // 每 66ms 生成一帧
    public async Task GenerateFrame()
    {
        byte[][] allInputs;
        lock (_inputBuffer)
        {
            allInputs = _inputBuffer
                .Where(i => i.Frame == _frameNumber)
                .Select(i => i.Input)
                .ToArray();
            _inputBuffer.RemoveAll(i => i.Frame == _frameNumber);
        }

        // 广播第 N 帧的所有输入给所有玩家
        var framePacket = new FramePacket
        {
            FrameNumber = _frameNumber,
            Inputs = allInputs
        };

        await BroadcastToAll(framePacket);
        _frameNumber++;
    }
}
```

---

## 六、战斗日志

```csharp
class BattleLogger
{
    private readonly Channel<BattleLog> _logChannel =
        Channel.CreateBounded<BattleLog>(10000);

    public void LogDamage(long attackerId, long defenderId,
        int skillId, int damage, bool isCrit)
    {
        _logChannel.Writer.TryWrite(new BattleLog
        {
            Type = LogType.Damage,
            Timestamp = DateTime.UtcNow,
            AttackerId = attackerId,
            DefenderId = defenderId,
            SkillId = skillId,
            Value = damage,
            IsCrit = isCrit
        });
    }

    public void LogBuff(long targetId, long casterId,
        int buffId, BuffAction action)
    {
        _logChannel.Writer.TryWrite(new BattleLog
        {
            Type = LogType.Buff,
            Timestamp = DateTime.UtcNow,
            AttackerId = casterId,
            DefenderId = targetId,
            SkillId = buffId,
            IntValue = (int)action
        });
    }

    // 后台批量写入
    public async Task FlushLoop(CancellationToken ct)
    {
        var batch = new List<BattleLog>(100);

        while (!ct.IsCancellationRequested)
        {
            // 等待 5 秒或满 100 条
            await Task.Delay(5000, ct);

            while (batch.Count < 100 &&
                   _logChannel.Reader.TryRead(out var log))
            {
                batch.Add(log);
            }

            if (batch.Count > 0)
            {
                await BulkWriteToDb(batch);
                batch.Clear();
            }
        }
    }
}
```

---

## 七、练习

1. **技能释放**：实现一个完整的技能释放验证流程
2. **伤害公式**：实现带暴击/闪避/防御的伤害计算
3. **Buff 系统**：实现眩晕/沉默/中毒三种 Buff
4. **帧同步**：实现 15FPS 的帧同步服务器逻辑
5. **战斗日志**：实现批量写入战斗日志（每 3 秒 flush 一次）

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 权威服务器 | 服务器全权计算战斗结果，客户端只有表现 |
| 技能验证 | 释放前检查冷却/消耗/范围/状态 |
| 伤害公式 | 攻击×技能倍率×防御减免×随机浮动 |
| Buff | 状态效果（眩晕/中毒/护盾），可叠加/不可叠加 |
| 帧同步 | 广播输入不广播状态，需要确定性逻辑 |
| 状态同步 | 广播计算结果，服务器验证一切 |
