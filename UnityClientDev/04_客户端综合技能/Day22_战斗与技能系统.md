# Day 22：战斗与技能系统 — 从配置表到运行时计算

## 0. 为什么需要战斗系统？

战斗是游戏的核心交互之一。一个结构清晰的战斗系统让你：

```
好系统：
加一个新技能 → 写一行配置 → 完成
改一个技能伤害 → 改一个数字 → 完成

烂系统：
加一个新技能 → 复制粘贴代码 → 改 5 处 → 可能引入 BUG
改一个技能伤害 → 找到所有用到的地方 → 改 10 处
```

目标：**数据驱动**——技能逻辑用配置表控制，不需要为每个技能单独写代码。

---

## 1. 战斗系统的架构

```
战斗系统的三层结构：

┌─────────────────────────────────┐
│  表现层（VFX、音效、动画）       │
│  - 播放受击特效                 │
│  - 播放技能动画                 │
│  - 飘字（伤害数值）             │
│  - 屏幕震动                     │
├─────────────────────────────────┤
│  逻辑层（伤害计算、Buff 管理）    │
│  - 计算最终伤害                 │
│  - 应用 Buff                    │
│  - 触发事件（击杀、连击）        │
│  - 目标选择                     │
├─────────────────────────────────┤
│  数据层（配置表、属性）           │
│  - 技能配置（来源于 Excel）       │
│  - 角色属性                     │
│  - Buff 模板                    │
│  - 伤害公式                     │
└─────────────────────────────────┘
```

---

## 2. Unit（角色）类设计

所有参与战斗的实体（玩家、敌人、NPC）共享的基础属性：

```csharp
public class Unit : MonoBehaviour
{
    [Header("基础属性")]
    public float maxHp = 100;
    public float currentHp;
    public float attack = 10;
    public float defense = 5;
    public float moveSpeed = 5f;

    [Header("进阶属性")]
    public float criticalRate = 0.1f;    // 暴击率 10%
    public float criticalDamage = 1.5f;  // 暴击伤害 150%
    public float damageBonus = 0f;       // 增伤加成

    [Header("组件")]
    public BuffManager BuffSystem { get; private set; }
    public SkillData[] skillCooldowns;   // 技能冷却数组

    public bool isDead => currentHp <= 0;

    protected virtual void Awake()
    {
        currentHp = maxHp;
        BuffSystem = new BuffManager();
    }

    public virtual void TakeDamage(float damage)
    {
        currentHp -= damage;
        Debug.Log($"{name} 受到 {damage} 伤害，剩余 HP：{currentHp}");

        if (isDead)
            OnDeath();
    }

    protected virtual void OnDeath()
    {
        Debug.Log($"{name} 死亡");
        Destroy(gameObject, 1f); // 1 秒后移除
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(currentHp + amount, maxHp);
    }
}
```

---

## 3. 配置表系统

### Excel → ScriptableObject 管线

```csharp
// 1. 定义技能配置
[CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Skill")]
public class SkillConfig : ScriptableObject
{
    [Header("基础信息")]
    public int skillId;
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;
    public SkillType type;
    public SkillTarget target;

    [Header("数值")]
    public float damageMultiplier;   // 攻击力倍率
    public float baseDamage;         // 基础伤害
    public float cooldown;           // 冷却时间（秒）
    public float castRange;          // 施法范围
    public float castTime;           // 施法前摇（秒）
    public float cost;               // 消耗（法力/能量）

    [Header("效果")]
    public SkillEffectType effectType;
    public float effectRadius;       // AOE 半径（仅 AOE 类型）
    public BuffConfig[] applyBuffs;  // 施放后附加的 Buff
    public GameObject projectilePrefab; // 弹道预制体
    public GameObject hitEffect;     // 击中特效
    public GameObject castEffect;    // 施法特效
    public AudioClip castSound;      // 施法音效
}

public enum SkillType { Active, Passive, Trigger }
public enum SkillTarget { Self, EnemySingle, EnemyAOE, Ally }
public enum SkillEffectType { Instant, Projectile, AreaOfEffect, BuffOnly }
```

### 运行时数据

```csharp
public class SkillData
{
    public SkillConfig config;
    public float currentCooldown;
    public bool isReady => currentCooldown <= 0;

    public void TickCooldown(float dt)
    {
        if (currentCooldown > 0)
            currentCooldown -= dt;
    }

    public void StartCooldown() => currentCooldown = config.cooldown;
}
```

### 技能数据库

```csharp
public class SkillDatabase : MonoBehaviour
{
    public static SkillDatabase Instance { get; private set; }

    public SkillConfig[] allSkills;

    private Dictionary<int, SkillConfig> skillMap;

    void Awake()
    {
        Instance = this;
        skillMap = new();
        foreach (var skill in allSkills)
            skillMap[skill.skillId] = skill;
    }

    public SkillConfig GetSkill(int id)
        => skillMap.TryGetValue(id, out var skill) ? skill : null;
}
```

---

## 4. 伤害计算系统

### 完整的伤害公式

```csharp
public class DamageCalculator
{
    // 多段伤害公式，每段独立计算
    public struct DamageResult
    {
        public int finalDamage;
        public bool isCritical;
        public bool isKill;
    }

    // 最终伤害 = 基础伤害 × 攻击力倍率 × (1 + 增伤加成) × 随机浮动 × 防御减免
    public static DamageResult CalculateDamage(Unit attacker, Unit defender, SkillConfig skill)
    {
        DamageResult result = new();

        // 1. 基础伤害 = 技能基础 + 攻击力 × 倍率
        float damage = skill.baseDamage + attacker.attack * skill.damageMultiplier;

        // 2. Buff 加成
        damage *= (1 + attacker.damageBonus + attacker.BuffSystem.GetStatModifier(BuffEffectType.AttackModify));

        // 3. 随机浮动（±5%）
        float randomFactor = Random.Range(0.95f, 1.05f);
        damage *= randomFactor;

        // 4. 暴击判定
        result.isCritical = Random.value < attacker.criticalRate;
        if (result.isCritical)
            damage *= attacker.criticalDamage;

        // 5. 防御减免（非线性公式，避免堆防御溢出）
        float defenseReduction = defender.defense / (defender.defense + 100f);
        damage *= (1 - defenseReduction);

        // 6. 最终取整，至少 1 点伤害
        result.finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage));

        return result;
    }
}
```

### 伤害流程事件系统

```csharp
public class CombatEvent
{
    public Unit attacker;
    public Unit defender;
    public SkillConfig skill;
    public int rawDamage;
    public int finalDamage;
    public bool isCritical;
    public bool isKill;
    public Vector3 hitPoint;
}

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance { get; private set; }

    public System.Action<CombatEvent> OnPreDamage;   // 伤害应用前（可以修改伤害值）
    public System.Action<CombatEvent> OnPostDamage;  // 伤害应用后

    void Awake() => Instance = this;

    public void ExecuteSkill(Unit attacker, Unit defender, SkillConfig skill)
    {
        var result = DamageCalculator.CalculateDamage(attacker, defender, skill);

        CombatEvent evt = new()
        {
            attacker = attacker,
            defender = defender,
            skill = skill,
            rawDamage = result.finalDamage,
            finalDamage = result.finalDamage,
            isCritical = result.isCritical,
            hitPoint = defender.transform.position
        };

        // 1. 前置事件（可以修改伤害）
        OnPreDamage?.Invoke(evt);

        // 2. 应用伤害
        defender.TakeDamage(evt.finalDamage);
        evt.isKill = defender.isDead;

        // 3. 后置事件
        OnPostDamage?.Invoke(evt);

        // 4. 应用 Buff
        foreach (var buff in skill.applyBuffs)
            defender.BuffSystem.AddBuff(buff);
    }
}
```

---

## 5. 技能效果系统

不同技能有不同的效果表现：

### 瞬间伤害

```csharp
public class InstantEffect : MonoBehaviour
{
    public void Apply(Unit attacker, Unit defender, SkillConfig skill)
    {
        // 直接造成伤害
        CombatSystem.Instance.ExecuteSkill(attacker, defender, skill);

        // 播放特效
        Instantiate(skill.hitEffect, defender.transform.position, Quaternion.identity);
    }
}
```

### 弹射物技能

```csharp
public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    private Unit attacker;
    private Unit target;
    private SkillConfig skill;

    public void Initialize(Unit attacker, Unit target, SkillConfig skill)
    {
        this.attacker = attacker;
        this.target = target;
        this.skill = skill;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 飞向目标
        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // 到达目标
        if (Vector3.Distance(transform.position, target.transform.position) < 0.5f)
        {
            CombatSystem.Instance.ExecuteSkill(attacker, target, skill);
            Instantiate(skill.hitEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
```

### AOE 范围技能

```csharp
public class AoeEffect : MonoBehaviour
{
    public void Apply(Unit attacker, Vector3 center, SkillConfig skill)
    {
        // 找到范围内所有敌人
        Collider[] hits = Physics.OverlapSphere(center, skill.effectRadius);

        foreach (var hit in hits)
        {
            Unit defender = hit.GetComponent<Unit>();
            if (defender != null && defender != attacker)
            {
                CombatSystem.Instance.ExecuteSkill(attacker, defender, skill);
            }
        }

        // AOE 特效
        Instantiate(skill.hitEffect, center, Quaternion.identity);
    }
}
```

---

## 6. Buff 系统

### Buff 设计

```csharp
[CreateAssetMenu(fileName = "NewBuff", menuName = "Game/Buff")]
public class BuffConfig : ScriptableObject
{
    public int buffId;
    public string buffName;
    public Sprite icon;
    public float duration;      // 持续时间（0 = 永久）
    public int maxStack;        // 最大层数
    public bool isDebuff;

    [Header("效果")]
    public BuffEffectType effectType;
    public float effectValue;   // 每 tick 的效果值
    public float tickInterval;  // 多少秒触发一次效果（0 = 不 tick）
}

public enum BuffEffectType
{
    DamageOverTime,     // 持续伤害（DOT）
    HealOverTime,       // 持续治疗（HOT）
    SpeedModify,        // 速度修改
    AttackModify,       // 攻击力修改
    DefenseModify,      // 防御修改
    Stun,               // 眩晕
    Silence,            // 沉默
}
```

### 运行时 Buff 实例

```csharp
public class BuffInstance
{
    public BuffConfig config;
    public float remainingTime;
    public int currentStack;
    public float tickTimer;
    private Unit owner;

    public bool IsExpired => remainingTime <= 0 && config.duration > 0;

    public BuffInstance(BuffConfig config, Unit owner)
    {
        this.config = config;
        this.owner = owner;
        this.remainingTime = config.duration;
        this.currentStack = 1;
        this.tickTimer = config.tickInterval;
    }

    public void Update(float dt)
    {
        if (config.duration > 0)
            remainingTime -= dt;

        if (config.tickInterval > 0)
        {
            tickTimer -= dt;
            while (tickTimer <= 0)
            {
                OnTick();
                tickTimer += config.tickInterval;
            }
        }

        // 眩晕效果——播放眩晕动画
        if (config.effectType == BuffEffectType.Stun)
            owner.GetComponent<Animator>()?.SetBool("isStunned", remainingTime > 0);
    }

    private void OnTick()
    {
        switch (config.effectType)
        {
            case BuffEffectType.DamageOverTime:
                owner.TakeDamage(config.effectValue * currentStack);
                break;

            case BuffEffectType.HealOverTime:
                owner.Heal(config.effectValue * currentStack);
                break;
        }
    }
}
```

### BuffManager

```csharp
public class BuffManager
{
    private List<BuffInstance> buffs = new();
    private Unit owner;
    public System.Action<BuffConfig> OnBuffAdded;
    public System.Action<BuffConfig> OnBuffRemoved;

    public BuffManager(Unit owner = null) => this.owner = owner;

    public void AddBuff(BuffConfig config)
    {
        // 检查是否已有同类 Buff
        var existing = buffs.Find(b => b.config.buffId == config.buffId);

        if (existing != null)
        {
            // 有同类 Buff → 叠层或重置时间
            existing.currentStack = Mathf.Min(existing.currentStack + 1, config.maxStack);
            existing.remainingTime = config.duration;
        }
        else
        {
            var instance = new BuffInstance(config, owner);
            buffs.Add(instance);
            OnBuffAdded?.Invoke(config);
        }
    }

    public void Update(float dt)
    {
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            buffs[i].Update(dt);
            if (buffs[i].IsExpired)
            {
                OnBuffRemoved?.Invoke(buffs[i].config);
                buffs.RemoveAt(i);
            }
        }
    }

    public float GetStatModifier(BuffEffectType type)
    {
        float total = 0;
        foreach (var buff in buffs)
            if (buff.config.effectType == type)
                total += buff.config.effectValue * buff.currentStack;
        return total;
    }

    public bool HasBuff(BuffEffectType type)
        => buffs.Exists(b => b.config.effectType == type);

    public bool IsStunned => HasBuff(BuffEffectType.Stun);
    public bool IsSilenced => HasBuff(BuffEffectType.Silence);
}
```

### Buff 组合示例

```
中毒（DOT） + 减速 + 易伤（攻击力降低）：

连招流程：
玩家施放"毒镖" → 敌人获得 3 个 Buff：
  - 中毒：每 2 秒掉 10 点血，持续 6 秒
  - 减速：移速降低 30%，持续 4 秒
  - 易伤：攻击力降低 20%，持续 6 秒

在 BuffManager 里就是：
AddBuff(中毒配置) → 3 个独立的 BuffInstance 在 Update 中各自计时
```

---

## 7. 完整战斗流程——玩家释放技能

```csharp
public class PlayerCombat : MonoBehaviour
{
    public int[] skillIds = new int[4]; // 技能栏

    private Unit unit;
    private SkillData[] cooldowns;

    void Start()
    {
        unit = GetComponent<Unit>();
        cooldowns = new SkillData[skillIds.Length];
        for (int i = 0; i < skillIds.Length; i++)
            cooldowns[i] = new SkillData();
    }

    void Update()
    {
        // 冷却计时
        foreach (var cd in cooldowns)
            cd.TickCooldown(Time.deltaTime);

        // 技能按键 1~4
        for (int i = 0; i < skillIds.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                TryUseSkill(i);
        }
    }

    void TryUseSkill(int slotIndex)
    {
        var skill = SkillDatabase.Instance.GetSkill(skillIds[slotIndex]);

        // 1. 检查是否被沉默
        if (unit.BuffSystem.IsSilenced)
        {
            Debug.Log("被沉默，无法施放技能！");
            return;
        }

        // 2. 检查冷却
        if (!cooldowns[slotIndex].isReady)
        {
            Debug.Log("技能冷却中");
            return;
        }

        // 3. 找目标
        Unit target = FindTarget(skill.target, skill.castRange);
        if (target == null && skill.target != SkillTarget.Self)
        {
            Debug.Log("没有目标");
            return;
        }

        // 4. 检查是否被眩晕
        if (unit.BuffSystem.IsStunned)
        {
            Debug.Log("被眩晕，无法行动！");
            return;
        }

        // 5. 执行技能效果
        switch (skill.effectType)
        {
            case SkillEffectType.Instant:
                CombatSystem.Instance.ExecuteSkill(unit, target, skill);
                break;

            case SkillEffectType.Projectile:
                var proj = Instantiate(skill.projectilePrefab, transform.position, Quaternion.identity)
                    .GetComponent<Projectile>();
                proj.Initialize(unit, target, skill);
                break;

            case SkillEffectType.AreaOfEffect:
                Vector3 targetPos = target != null ? target.transform.position : GetMouseWorldPosition();
                FindObjectOfType<AoeEffect>().Apply(unit, targetPos, skill);
                break;
        }

        // 6. 进入冷却
        cooldowns[slotIndex].StartCooldown();

        // 7. 播放表现
        if (skill.castEffect != null)
            Instantiate(skill.castEffect, transform.position, Quaternion.identity);
        if (skill.castSound != null)
            AudioSource.PlayClipAtPoint(skill.castSound, transform.position);
    }

    private Unit FindTarget(SkillTarget targetType, float range)
    {
        // 简化实现：自动搜索最近的敌人/友军
        switch (targetType)
        {
            case SkillTarget.Self:
                return unit;

            case SkillTarget.EnemySingle:
                // 寻找最近的敌人
                var enemies = FindObjectsOfType<Enemy>();
                return FindClosest(enemies, range);

            case SkillTarget.Ally:
                var allies = FindObjectsOfType<PlayerCombat>();
                return FindClosest(allies, range);

            default:
                return null;
        }
    }

    private Unit FindClosest(Unit[] candidates, float range)
    {
        float minDist = range;
        Unit closest = null;

        foreach (var c in candidates)
        {
            if (c == unit) continue;
            float dist = Vector3.Distance(transform.position, c.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = c;
            }
        }
        return closest;
    }
}
```

---

## 8. 练习

### 练习 1：实现一个伤害公式

```csharp
// 设计并实现一个包含以下因素的伤害公式：
// 1. 攻击力（Attack）
// 2. 防御力（Defense）——带穿透效果
// 3. 等级差修正
// 4. 属性克制（火 > 水 > 风 > 火）
// 5. 随机波动
// 6. 最小伤害保证
```

### 练习 2：技能系统

```csharp
// 创建 3 个技能：
// 1. 火球术：发射弹射物，命中后附加灼烧 DOT
// 2. 治疗术：瞬间治疗，附加 HOT
// 3. 冰霜新星：AOE 伤害 + 减速 Buff
//
// 每个技能用 SkillConfig 定义，用脚本控制效果逻辑
```

### 练习 3：Buff 连锁

```csharp
// 实现一个 Buff 连锁系统：
// 1. "灼烧"Buff 持续 6 秒，每 2 秒造成伤害
// 2. "引爆"技能：对灼烧中的目标造成双倍伤害，移除灼烧
// 3. "传染"技能：把目标的灼烧复制到附近敌人
//
// 要求：所有逻辑基于已有 BuffManager 和 CombatSystem
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Unit | 参与战斗的实体基类，管理 HP/属性 |
| 配置表驱动 | 技能行为由数据（ScriptableObject）控制 |
| 伤害公式 | 攻击 × 倍率 × 增伤 × 随机 × 防御减免 |
| 事件驱动 | OnPreDamage/OnPostDamage 让各系统响应 |
| 技能效果 | Instant/Projectile/AOE 三种常见的技能形式 |
| Buff 系统 | DOT/HOT/眩晕/减速，支持叠层和 tick 计时 |

**对比 Raylib/C++：** Raylib 中你可能直接用 if-else 处理每种攻击。对于小游戏够了，但商业项目需要"数据驱动"——加新技能不需要改代码，只改配置表。C# 的 ScriptableObject 让配置和代码分离变得非常自然。事件系统（Action）和 C++ 的回调函数/信号槽（Boost.Signal）思路一致。
