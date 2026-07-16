# Day 22：战斗与技能系统 — ECS 战斗、网络同步、连招系统与回放

## 0. 为什么需要更深入的战斗系统？

基础篇的战斗系统是单机 MVP（最小可行产品）。商业项目的战斗系统要处理：

```
单机战斗 vs 网络战斗的差异：

单机战斗（基础篇）：
玩家按 1 → 计算伤害 → 播放特效 → 结束
全部在本地执行，没有一致性问题

网络战斗（深入篇）：
玩家按 1 → 客户端预测 → 发送消息 → 服务器判定 → 广播 → 其他客户端表现
一致性！公平性！作弊防护！
```

```
战斗系统面临的 4 个核心挑战：
1. 架构选择：用 ECS 还是 OOP？
2. 同步方案：Lockstep 还是状态同步？
3. 技能表现：连招怎么做？输入缓冲怎么处理？
4. 回溯：战斗回放怎么实现？
```

---

## 1. ECS 战斗架构

### 为什么用 ECS？

```
传统 OOP 战斗的问题：
- Unit 类越来越庞大（攻击、受击、Buff、移动、属性...）
- 多继承 / 多层继承 → 组合爆炸
- 性能问题：cache miss、虚方法调用

ECS 方案：
Entity（实体）= 空壳，只包含 Component 列表
Component（组件）= 纯数据，无方法
System（系统）= 纯逻辑，操作 Component

战斗中的 ECS 组件拆分：
Position, Velocity, Health, BuffList,
SkillCooldown, AttackTarget, DamageModifier...
```

### ECS 战斗实现

```csharp
// 使用 Unity.Entities 包
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

// === 组件定义（纯数据，无方法）===

// 血量组件
public struct Health : IComponentData
{
    public float current;
    public float maxHP;
    public bool isDead => current <= 0;
}

// 攻击组件
public struct Attack : IComponentData
{
    public float baseDamage;
    public float attackRange;
    public float attackInterval;
    public float cooldownTimer;
    public Entity target;  // 目标实体
}

// 移动组件
public struct Movement : IComponentData
{
    public float speed;
    public float3 targetPosition;
}

// Buff 缓冲组件（动态缓冲）
public struct BuffBuffer : IBufferElementData
{
    public int buffId;
    public float remainingTime;
    public float tickInterval;
    public float tickTimer;
    public float effectValue;
}

// === 系统（纯逻辑）===

// 伤害计算系统
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 查询所有有 Health 和 Attack 的实体
        foreach (var (health, attack) in
                 SystemAPI.Query<RefRW<Health>, RefRO<Attack>>())
        {
            // 检查冷却
            var atk = attack.ValueRO;
            if (atk.cooldownTimer > 0)
            {
                // 减少冷却
                var mutating = SystemAPI.GetComponentRW<Attack>(atk.target);
                mutating.ValueRW.cooldownTimer -= SystemAPI.Time.DeltaTime;
                continue;
            }

            // 造成伤害
            float damage = atk.baseDamage;
            health.ValueRW.current -= damage;

            // 重置冷却
            var mutable = SystemAPI.GetComponentRW<Attack>(atk.target);
            mutable.ValueRW.cooldownTimer = atk.attackInterval;
        }
    }
}

// Buff 系统
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct BuffSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (health, buffBuffer) in
                 SystemAPI.Query<RefRW<Health>, DynamicBuffer<BuffBuffer>>())
        {
            for (int i = buffBuffer.Length - 1; i >= 0; i--)
            {
                var buff = buffBuffer[i];
                buff.remainingTime -= SystemAPI.Time.DeltaTime;

                // DOT 效果
                if (buff.tickInterval > 0)
                {
                    buff.tickTimer -= SystemAPI.Time.DeltaTime;
                    if (buff.tickTimer <= 0)
                    {
                        health.ValueRW.current -= buff.effectValue;
                        buff.tickTimer = buff.tickInterval;
                    }
                }

                // 过期移除
                if (buff.remainingTime <= 0)
                    buffBuffer.RemoveAt(i);
            }
        }
    }
}
```

### ECS vs OOP 战斗对比

| 维度 | OOP | ECS |
|------|-----|-----|
| 数据局部性 | 差（对象分散） | 好（连续内存） |
| 缓存命中率 | 低 | 高 |
| 性能（10k 单位） | 慢 | 快 10~100 倍 |
| 代码组织 | 按对象 | 按逻辑横切 |
| 学习成本 | 低 | 高 |
| 适用规模 | <100 单位 | 1000+ 单位 |

---

## 2. 状态同步与 Lockstep

### 两种同步方案

```
状态同步（State Sync）：
服务器每帧发送所有实体的位置/状态给客户端
┌─────────┐       ┌─────────┐
│ 客户端 A  │ ───▶ │  服务器   │ ◀─── ┌─────────┐
│ 发送输入  │       │ 权威模拟 │       │ 客户端 B  │
└─────────┘       └─────────┘       └─────────┘
                      │
                      └── 广播世界状态给所有客户端

适用：FPS、MMORPG（延迟容忍 50~200ms）
优点：实现简单，客户端可以预测
缺点：带宽高（每帧要发所有实体状态）

Lockstep：
所有客户端执行相同的输入序列，保证结果一致
┌─────────┐       ┌─────────┐
│ 客户端 A  │ ───▶ │  服务器   │ ◀─── ┌─────────┐
│ 发送输入  │       │ 转发输入 │       │ 客户端 B  │
└─────────┘       └─────────┘       └─────────┘
     │                                  │
     │   所有客户端执行相同序列           │
     └──────────── 帧同步 ───────────────┘

适用：RTS、格斗游戏（需要精确同步）
优点：带宽低（只需要同步输入）
缺点：实现复杂，不能容忍延迟
```

### 状态同步实现

```csharp
public class CombatStateSync : MonoBehaviour
{
    [System.Serializable]
    public struct CombatEntityState
    {
        public int entityId;
        public Vector3 position;
        public float hp;
        public float maxHp;
        public int animationState;  // 0:idle, 1:run, 2:attack, 3:hit
        public float animNormalizedTime;
        public uint timestamp;
    }

    // 发送玩家状态到服务器（每帧）
    public CombatEntityState BuildPlayerState()
    {
        return new CombatEntityState
        {
            entityId = localPlayerId,
            position = transform.position,
            hp = unit.currentHp,
            maxHp = unit.maxHp,
            animationState = (int)currentAnimState,
            animNormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
            timestamp = (uint)(Time.time * 1000)  // ms 时间戳
        };
    }

    // 收到服务器广播的状态更新
    public void OnReceiveStates(CombatEntityState[] states)
    {
        foreach (var state in states)
        {
            // 找到对应的远程实体
            if (remoteEntities.TryGetValue(state.entityId, out var entity))
            {
                // 用插值更新（不是硬设置）
                entity.PushState(state);
            }
        }
    }
}
```

### Lockstep 帧同步实现

```csharp
// Lockstep 的核心：确认帧 + 输入缓冲

public class LockstepManager : MonoBehaviour
{
    public const int TICK_RATE = 20;  // 20 帧/秒
    public const float TICK_INTERVAL = 1f / TICK_RATE; // 50ms

    // 输入队列
    private Queue<PlayerInput> inputBuffer = new();

    // 当前帧号（所有客户端同步）
    private uint currentFrame;

    // 已经确认的帧号（N-2 帧之前的所有客户端都到达）
    private uint confirmedFrame;

    // 网络管理
    private NetworkManager network;

    void FixedUpdate()
    {
        // 1. 收集本帧输入
        PlayerInput input = GatherInput();
        input.frame = currentFrame;

        // 2. 发送给服务器
        network.SendInput(input);

        // 3. 执行到确认帧的输入（回放）
        SimulateUpToFrame(confirmedFrame);

        currentFrame++;
    }

    // 收到服务器确认
    public void OnFrameConfirmed(uint frame, PlayerInput[] allInputs)
    {
        // 所有玩家在这一帧的输入
        inputBuffer.Enqueue(allInputs[localPlayerId]);
        confirmedFrame = frame;

        // 丢弃已确认的前面输入
        while (inputBuffer.Count > 0 && inputBuffer.Peek().frame < frame)
            inputBuffer.Dequeue();
    }

    // 确定性模拟（输入相同 → 结果相同）
    private void SimulateUpToFrame(uint targetFrame)
    {
        // 从上次模拟位置继续
        while (lastSimulatedFrame < targetFrame)
        {
            if (inputs.TryGetValue(lastSimulatedFrame + 1, out var input))
            {
                DeterministicTick(input);
                lastSimulatedFrame++;
            }
            else
            {
                break;  // 等后续输入
            }
        }
    }

    // 确定性 tick（必须使用定点数数学！）
    private void DeterministicTick(PlayerInput input)
    {
        // 确定性的位置更新
        // 使用 FixedPoint（定点数），不用 float
        FixedPoint dt = FixedPoint.FromFloat(TICK_INTERVAL);

        if (input.up) position.y += FixedPoint.One * dt;
        if (input.down) position.y -= FixedPoint.One * dt;
        if (input.left) position.x -= FixedPoint.One * dt;
        if (input.right) position.x += FixedPoint.One * dt;
    }
}

public struct PlayerInput
{
    public uint frame;
    public bool up, down, left, right;
    public bool attack;
    public bool skill1, skill2;
    public uint inputHash;  // 输入校验
}
```

### 同步方案选择

```
状态同步 vs Lockstep 选择指南：

做 FPS/TPS：状态同步
- 每帧发位置，延迟补偿
- 客户端可预测
- 代表游戏：使命召唤、守望先锋

做 RTS/格斗：Lockstep
- 每帧只同步输入
- 必须确定性模拟
- 代表游戏：星际争霸、魔兽争霸

做 MMORPG：状态同步
- 大量实体，状态随变随发
- 不用每帧都同步
- 代表游戏：魔兽世界

做 MOBA：状态同步
- 位置频繁变化
- 需要精确的延迟补偿
- 代表游戏：DOTA、LOL
```

---

## 3. 技能系统中的行为树

### 技能 = 一段可编排的逻辑

```csharp
// 技能不再是一段硬编码，而是基于节点的高层描述

// 技能行为树节点
public abstract class SkillNode
{
    public abstract SkillResult Execute(SkillContext context);
}

public class SkillContext
{
    public Unit caster;
    public Unit target;
    public Vector3 targetPosition;
    public SkillConfig config;
    public float deltaTime;
    public float totalTime;     // 技能已执行时间
    public float progress;      // 技能进度 0~1
    public BuffManager buffManager;
    public GameObject vfxParent;
}

// 具体节点

// 伤害节点
public class DamageNode : SkillNode
{
    public float damageMultiplier = 1f;
    public float baseDamage;

    public override SkillResult Execute(SkillContext ctx)
    {
        if (ctx.target == null) return SkillResult.Failure;

        float damage = baseDamage + ctx.caster.attack * damageMultiplier;
        ctx.target.TakeDamage(damage);

        return SkillResult.Success;
    }
}

// 弹道节点
public class ProjectileNode : SkillNode
{
    public GameObject prefab;
    public float speed = 20f;

    public override SkillResult Execute(SkillContext ctx)
    {
        // 实例化弹道
        GameObject proj = Object.Instantiate(prefab,
            ctx.caster.transform.position, Quaternion.identity);
        proj.GetComponent<Projectile>().Initialize(ctx.caster, ctx.target, speed);
        return SkillResult.Running;  // 弹道飞行中，持续返回 Running
    }
}

// Buff 节点
public class BuffNode : SkillNode
{
    public BuffConfig buffConfig;

    public override SkillResult Execute(SkillContext ctx)
    {
        ctx.target.BuffSystem.AddBuff(buffConfig);
        return SkillResult.Success;
    }
}

// 延迟节点
public class DelayNode : SkillNode
{
    public float delayTime;

    public override SkillResult Execute(SkillContext ctx)
    {
        return ctx.totalTime >= delayTime
            ? SkillResult.Success
            : SkillResult.Running;
    }
}

// AOE 节点
public class AoeNode : SkillNode
{
    public float radius;
    public SkillNode effectNode;  // 对范围内每个目标执行的效果

    public override SkillResult Execute(SkillContext ctx)
    {
        Collider[] hits = Physics.OverlapSphere(ctx.targetPosition, radius);
        foreach (var hit in hits)
        {
            var unit = hit.GetComponent<Unit>();
            if (unit != null && unit != ctx.caster)
            {
                var targetCtx = new SkillContext
                {
                    caster = ctx.caster,
                    target = unit,
                    config = ctx.config
                };
                effectNode.Execute(targetCtx);
            }
        }
        return SkillResult.Success;
    }
}
```

### 技能编排示例

```csharp
// 火球术 = 延迟0.3秒 → 生成弹道 → 命中后伤害 + Buff
public class FireballSkill : MonoBehaviour
{
    public SkillNode BuildFireball()
    {
        // 前摇 0.3 秒
        var castDelay = new DelayNode { delayTime = 0.3f };

        // 弹道
        var projectile = new ProjectileNode
        {
            prefab = fireballPrefab,
            speed = 15f
        };

        // 命中效果
        var onHit = new Sequence();
        onHit.Add(new DamageNode { damageMultiplier = 2f, baseDamage = 50 });
        onHit.Add(new BuffNode { buffConfig = burnBuff });

        // 完整技能
        var skill = new Sequence();
        skill.Add(castDelay);
        skill.Add(projectile);
        // onHit 由弹道击中时触发
        projectile.onHitNode = onHit;

        return skill;
    }
}
```

---

## 4. 连招系统与输入缓冲

### 输入缓冲

```
玩家：A → A → A（快速按三下攻击键）
问题：如果每个攻击动画 0.5 秒，玩家按得比动画快
      → 第二次按键被丢掉了 → 玩家认为"我按了但没反应"

解决方案：输入缓冲
- 每次攻击时，在窗口期内（例：0.2 秒）检测下一次输入
- 如果检测到，把下一段攻击"排队"
- 当前攻击结束后自动衔接
```

```csharp
public class InputBuffer
{
    private struct BufferedInput
    {
        public int skillId;
        public float timestamp;
    }

    private Queue<BufferedInput> buffer = new();
    private const float BUFFER_TIME = 0.2f;  // 200ms 缓冲窗口

    // 收到输入时
    public void PushInput(int skillId)
    {
        buffer.Enqueue(new BufferedInput
        {
            skillId = skillId,
            timestamp = Time.time
        });
    }

    // 检查是否有有效输入
    public bool TryGetNextInput(out int skillId)
    {
        while (buffer.Count > 0)
        {
            var input = buffer.Peek();

            // 超时的输入扔掉
            if (Time.time - input.timestamp > BUFFER_TIME)
            {
                buffer.Dequeue();
                continue;
            }

            skillId = input.skillId;
            buffer.Dequeue();
            return true;
        }

        skillId = -1;
        return false;
    }
}

// 连招系统
public class ComboSystem : MonoBehaviour
{
    private InputBuffer inputBuffer = new();
    private int currentComboStep;    // 当前连招第几段
    private float lastAttackTime;
    private const float COMBO_TIMEOUT = 1.0f;  // 超过 1 秒没按 → 重置连招

    // 连招定义
    [System.Serializable]
    public class Combo
    {
        public string comboName;
        public int[] skillIds;  // 连招序列，如 [轻击1, 轻击2, 重击]
    }

    public Combo[] combos;

    void Update()
    {
        // 检测输入
        if (Input.GetKeyDown(KeyCode.Mouse0))
            inputBuffer.PushInput(SkillID.LightAttack);

        if (Input.GetKeyDown(KeyCode.Mouse1))
            inputBuffer.PushInput(SkillID.HeavyAttack);

        // 连招超时重置
        if (Time.time - lastAttackTime > COMBO_TIMEOUT)
            currentComboStep = 0;

        // 尝试从缓冲取输入
        if (inputBuffer.TryGetNextInput(out int skillId))
        {
            TryExecuteCombo(skillId);
        }
    }

    private void TryExecuteCombo(int skillId)
    {
        // 检查当前是否匹配某个连招的下一步
        foreach (var combo in combos)
        {
            if (currentComboStep < combo.skillIds.Length &&
                combo.skillIds[currentComboStep] == skillId)
            {
                // 匹配！执行连招的这一步
                ExecuteSkill(combo.skillIds[currentComboStep]);
                currentComboStep++;
                lastAttackTime = Time.time;

                // 连招完成 → 重置
                if (currentComboStep >= combo.skillIds.Length)
                {
                    Debug.Log($"连招完成：{combo.comboName}");
                    currentComboStep = 0;
                }
                return;
            }
        }

        // 不匹配 → 从头开始
        currentComboStep = 0;
        ExecuteSkill(skillId);
    }

    private void ExecuteSkill(int skillId)
    {
        animator.SetInteger("AttackID", skillId);
        animator.SetTrigger("Attack");
    }
}
```

### 动画事件触发攻击判定

```csharp
// 在攻击动画的特定帧调用 Animation Event
public class AttackHitDetection : MonoBehaviour
{
    private Collider attackCollider;

    void Awake()
    {
        // 攻击碰撞体默认关闭
        attackCollider = GetComponent<Collider>();
        attackCollider.enabled = false;
    }

    // 动画事件：攻击判定开始
    public void OnAttackStart()
    {
        attackCollider.enabled = true;
    }

    // 动画事件：攻击判定结束
    public void OnAttackEnd()
    {
        attackCollider.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        var unit = other.GetComponent<Unit>();
        if (unit != null && unit != GetComponent<Unit>())
        {
            // 通知战斗系统这次碰撞
            CombatSystem.Instance.OnHitDetected(
                GetComponent<Unit>(), unit);
        }
    }
}
```

---

## 5. 伤害数字浮动的 UI 优化

```csharp
// 伤害数字不要"硬邦邦"地显示
// 需要随机浮动 + 动画

public class DamageNumber : MonoBehaviour
{
    public TextMeshProUGUI text;
    public AnimationCurve xCurve;     // 水平漂移
    public AnimationCurve yCurve;     // 垂直漂移（上升）
    public AnimationCurve scaleCurve; // 缩放
    public AnimationCurve alphaCurve; // 淡出

    private Vector3 startPos;
    private float duration = 0.8f;
    private float elapsed;

    // 初始化
    public void Show(int damage, bool isCritical, Vector3 worldPos)
    {
        // 世界坐标 → 屏幕坐标
        startPos = Camera.main.WorldToScreenPoint(worldPos);

        // 添加随机偏移（防止数字叠加）
        startPos += new Vector3(
            Random.Range(-20f, 20f),
            Random.Range(-10f, 10f),
            0
        );

        // 暴击特效
        if (isCritical)
        {
            text.text = $"暴击！{damage}";
            text.fontSize = 48;
            text.color = Color.yellow;
            transform.localScale = Vector3.one * 1.5f;
        }
        else
        {
            text.text = damage.ToString();
            text.fontSize = 36;
            text.color = Color.white;
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // 位置曲线
        float x = startPos.x + xCurve.Evaluate(t) * 30f;
        float y = startPos.y + yCurve.Evaluate(t) * 50f;
        transform.position = new Vector3(x, y, 0);

        // 缩放曲线
        transform.localScale = Vector3.one * scaleCurve.Evaluate(t);

        // 透明度
        text.color = new Color(text.color.r, text.color.g, text.color.b,
            alphaCurve.Evaluate(t));
    }
}

// 对象池优化（大量伤害数字时）
public class DamageNumberPool : MonoBehaviour
{
    public DamageNumber prefab;
    private Queue<DamageNumber> pool = new();
    private const int INITIAL_SIZE = 30;

    void Start()
    {
        for (int i = 0; i < INITIAL_SIZE; i++)
        {
            var dn = Instantiate(prefab, transform);
            dn.gameObject.SetActive(false);
            pool.Enqueue(dn);
        }
    }

    public void ShowDamage(int damage, bool critical, Vector3 pos)
    {
        DamageNumber dn;
        if (pool.Count > 0)
            dn = pool.Dequeue();
        else
            dn = Instantiate(prefab, transform);

        dn.gameObject.SetActive(true);
        dn.Show(damage, critical, pos);
        StartCoroutine(ReturnToPool(dn, 0.8f));
    }

    private IEnumerator ReturnToPool(DamageNumber dn, float delay)
    {
        yield return new WaitForSeconds(delay);
        dn.gameObject.SetActive(false);
        pool.Enqueue(dn);
    }
}
```

---

## 6. 战斗回放系统

### 核心思路

```
回放的两种方案：

1. 录视频（文件大，不能交互）
   直接录屏 → 存视频文件

2. 记录输入/状态（文件小，可以交互）
   记录每帧的输入序列 → 重放时模拟
   类似 Lockstep 的思路

回放系统 = 确定性模拟 + 时间轴控制
```

```csharp
public class CombatReplaySystem
{
    [System.Serializable]
    public class ReplayFrame
    {
        public int frame;
        public float timestamp;
        public List<EntitySnapshot> snapshots;
    }

    [System.Serializable]
    public class EntitySnapshot
    {
        public int entityId;
        public Vector3 position;
        public Quaternion rotation;
        public float hp;
        public int animState;
        public float animTime;
    }

    private List<ReplayFrame> replayData = new();
    private bool isRecording;
    private bool isPlaying;
    private int currentPlaybackFrame;

    // 开始录制
    public void StartRecording()
    {
        replayData.Clear();
        isRecording = true;
    }

    // 每帧录制
    public void RecordFrame(int frame, List<Unit> allUnits)
    {
        if (!isRecording) return;

        var replayFrame = new ReplayFrame
        {
            frame = frame,
            timestamp = Time.time,
            snapshots = new List<EntitySnapshot>()
        };

        foreach (var unit in allUnits)
        {
            replayFrame.snapshots.Add(new EntitySnapshot
            {
                entityId = unit.gameObject.GetInstanceID(),
                position = unit.transform.position,
                rotation = unit.transform.rotation,
                hp = unit.currentHp,
                animState = (int)unit.currentAnimState,
                animTime = unit.animator.GetCurrentAnimatorStateInfo(0).normalizedTime
            });
        }

        replayData.Add(replayFrame);
    }

    // 停止录制并保存
    public void StopAndSave(string fileName)
    {
        isRecording = false;
        string json = JsonConvert.SerializeObject(replayData);
        File.WriteAllText(Application.persistentDataPath + "/" + fileName, json);
    }

    // 加载回放
    public void LoadReplay(string fileName)
    {
        string json = File.ReadAllText(
            Application.persistentDataPath + "/" + fileName);
        replayData = JsonConvert.DeserializeObject<List<ReplayFrame>>(json);
    }

    // 播放回放
    public void PlaybackUpdate(List<Unit> allUnits)
    {
        if (!isPlaying || currentPlaybackFrame >= replayData.Count) return;

        var frame = replayData[currentPlaybackFrame];

        foreach (var snapshot in frame.snapshots)
        {
            // 找到对应的实体
            var unit = allUnits.Find(u =>
                u.gameObject.GetInstanceID() == snapshot.entityId);

            if (unit != null)
            {
                // 还原位置/状态
                unit.transform.position = snapshot.position;
                unit.transform.rotation = snapshot.rotation;
                unit.currentHp = snapshot.hp;
            }
        }

        currentPlaybackFrame++;
    }

    // 回放控制
    public void Play() => isPlaying = true;
    public void Pause() => isPlaying = false;
    public void Stop()
    {
        isPlaying = false;
        currentPlaybackFrame = 0;
    }
    public void SeekTo(float time)
    {
        // 跳转到指定时间
        currentPlaybackFrame = replayData.FindIndex(f => f.timestamp >= time);
    }
}
```

---

## 7. C++/Raylib 对比

| 概念 | C++ | Unity/C# |
|------|-----|----------|
| ECS | entt / flecs | Entities 包 |
| 状态同步 | 手动实现 + UDP | 同上思路 |
| Lockstep | 定点数 + 确定性模拟 | 同样需要定点数 |
| 技能行为树 | 自实现节点系统 | 同上思路 |
| 输入缓冲 | 环形队列 | Queue<T> 实现 |
| 连招 | 状态机 + 计时器 | Animator Layer + 逻辑 |
| 回放 | 记录输入/状态 | 序列化到 JSON/二进制 |

**Lockstep 最关键的要求是确定性**——同样的输入序列必须产生完全一样的结果。这意味着：

1. 不能使用 float（不同平台浮点精度不同） → 用定点数（FixedPoint）
2. 随机数必须使用确定性随机（用固定 seed 的伪随机）
3. 物理必须用确定性物理（不用 Unity Physics，自己实现）

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| ECS 战斗 | 数据组件化 + 系统处理，适用于大量单位 |
| 状态同步 | 服务器发状态，客户端插值/预测 |
| Lockstep | 同步输入序列，客户端独立模拟 |
| 输入缓冲 | 200ms 窗口保留下次输入，防止连招断档 |
| 技能行为树 | 把技能拆成可编排节点（延迟→弹道→伤害） |
| 连招系统 | 按序列匹配输入，超时重置 |
| 伤害数字 | 曲线动画 + 随机浮动 + 对象池 |
| 战斗回放 | 记录每帧快照，按时间轴重放 |
| 确定性模拟 | 定点数 + 确定性随机，Lockstep 的基础 |

**对比 C++：** 战斗系统的核心问题是"逻辑组织"和"数据一致性"，不是语言特性。行为树编排技能的方式在 C++ 和 C# 中一致。ECS 方面，C++ 有 ennt 和 flecs 库，Unity 有 Entities 包，思路相同。Lockstep 的确定性要求在 C++ 中也需要定点数库（如 libfixmath）。
