# SuperSmashLike 项目架构与实现汇总

> 生成日期：2026-09-19
> 依据：逐脚本通读 `Assets/_Game` 下 37 个自研 C# 脚本（共 5533 行）
> 【注意】本文以**代码实际状态**为准。`Docs/01_系统架构设计.md` 里画的 `MovementHandler` / `AttackManager` / `DefenseManager` / `CombatSystem` 等类**在代码中并不存在** —— 那些职责全部塞在 `FighterController` 一个类里。

---

## 一、项目定位与当前进度

| 项 | 内容 |
|---|---|
| 引擎 | Unity 2022.3 LTS |
| 类型 | 2D 平台格斗（Smash-like） |
| 输入 | Unity Input System（手动订阅 + JSON 克隆资产，绕开 InputUser 污染） |
| 物理 | Rigidbody2D + Collider2D（Trigger 做判定） |
| 动画 | Animator 双层：Base Layer + Action Layer（索引 1，权重由代码控制） |
| 代码规模 | 37 个自研脚本 / 5533 行（不含三方插件） |
| 里程碑 | M1 防御体系达成（2026-09-18），M2 待启动 |
| 备份 | GitHub `Autuwd/SuperSmashLike`（master，与本地 0/0 同步） |

---

## 二、脚本清单（按目录）

### Core（核心底座）

| 脚本 | 行 | 职责 |
|---|---|---|
| `GameManager.cs` | 130 | 单例。游戏阶段状态机（Boot/Title/CharacterSelect/Battle/Paused/Result）、活跃斗士注册表、持有 GameSettings |
| `ObjectPooler.cs` | 108 | 单例。对象池（Pool 列表 + Spawn/Despawn），供投射物复用 |

### Character（角色）

| 脚本 | 行 | 职责 |
|---|---|---|
| `FighterController.cs` | **1236** | **项目核心上帝类**。整合输入/物理/状态机/动画/战斗/防御/抓投/重生 |
| `FighterStateMachine.cs` | 134 | 纯 C# 状态机。13 个状态 + 转换规则表 + 三个查询方法（IsInAir/CanAct/IsVulnerable） |
| `ShieldVisual.cs` | 57 | 护盾视觉：SpriteRenderer 开关 + 透明度随耐久 0.65→0.15 |
| `VisualRootMotion.cs` | 53 | 抵消动画的根位移（Root Motion），`keepVerticalFor` 白名单内的招式保留 Y |

### Combat（战斗）

| 脚本 | 行 | 职责 |
|---|---|---|
| `Hitbox.cs` | 129 | 攻击判定框。动画事件控制开关，OnTriggerEnter2D 命中 → ApplyDamage + 震屏 + 特效 + Hitstop |
| `Hurtbox.cs` | 48 | 受击判定框。只持有 `owner` 引用，判定逻辑全在 Hitbox 侧 |
| `HitboxEventRelay.cs` | 47 | **转发器**。动画事件挂在 Visual 上，转发给真正的 Hitbox |
| `DamageSystem.cs` | 101 | **静态纯计算类**。击飞速度公式 / 击飞方向 / 硬直时长 / 盾伤 / 最终伤害 |
| `Projectile.cs` | 49 | 投射物。静态 `Spawn()` 走对象池，飞行/超时/命中后回收 |
| `HitboxPreview.cs` | 53 | Editor 可视化：Scene 里画指定招式的判定框 |
| `HitboxSceneSync.cs` | 54 | 判定框可视化编辑：把 Scene 里拖好的框写回 AttackData 资产 |

### Input（输入）

| 脚本 | 行 | 职责 |
|---|---|---|
| `InputManager.cs` | 394 | 输入总入口。克隆 InputActionAsset、按 playerID 做 bindingMask 隔离、Action 回调写字段、Update 里消费并调用 FighterController |
| `InputBuffer.cs` | 67 | 纯 C# 帧号输入缓冲（5 帧窗口），攻击提前按下时缓存，窗口到了自动消费 |

### Managers（管理器，场景内实例）

| 脚本 | 行 | 职责 |
|---|---|---|
| `MatchManager.cs` | 422 | 比赛流程协程（倒计时→进行→结束）、规则判定（时间制/命数制）、重生调度、HUD 生成与事件绑定 |
| `CameraManager.cs` | 229 | 多目标跟随（LateUpdate + SmoothDamp）、自适应缩放、`Shake()` 震屏、`FlashWhite()` 闪白 |

### Stage（舞台）

| 脚本 | 行 | 职责 |
|---|---|---|
| `BlastZone.cs` | 72 | 出界判定。Trigger 命中 → 广播 `OnPlayerOutOfBounds` |
| `Platform.cs` | 74 | 单向平台。`DropThrough()` 临时禁用碰撞实现"落穿" |

### UI

| 脚本 | 行 | 职责 |
|---|---|---|
| `FrameMeter.cs` | 228 | 帧数表显示。60 格三段色（前摇黄/判定红/后摇蓝）+ 游标 + 硬直差，预设体驱动 |
| `UIFlowController.cs` | 116 | UI 流程总控（标题→选人→战斗→结算的面板切换） |
| `DamageDisplay.cs` | 79 | 伤害百分比显示（含淡入淡出动画） |
| `StockDisplay.cs` | 90 | 命数图标 + 计时器 |
| `CharacterSelectPanel.cs` | 67 | 选人面板 |
| `TitlePanel.cs` | 24 | 标题面板 |
| `ResultsPanel.cs` | 44 | 结算面板 |

### Data（ScriptableObject 配置层）

| 脚本 | 行 | 职责 |
|---|---|---|
| `FighterData.cs` | 126 | 角色数据资产。移动参数 + 23 个 AttackData 槽位 + `AttackData` 内嵌类定义 |
| `GameSettings.cs` | 68 | 全局规则资产。比赛模式/命数/时长/伤害比例/护盾/重生/Hitstop + 选人结果跨场景缓存 |
| `StageData.cs` | 40 | 舞台数据资产（**当前未被运行时读取，见第九节**） |

### Editor（编辑器工具，不进包体）

| 脚本 | 行 | 职责 |
|---|---|---|
| `DemoSetupWizard.cs` | 576 | 一键生成全套资产/预制体/场景（GameSettings、FighterData、InputActions、Animator、Fighter 预制体、舞台、Demo 场景） |
| `AttackDataEventSync.cs` | 291 | **帧数表与动画事件同步工具**。以 clip 的 Activate/Deactivate/AttackFinished 时间为唯一权威源，回写 startup/active/recovery，并校验 SAR == clip 长度 |
| `ExtractClips.cs` | 35 | 把 FBX 只读子资产 clip 导出为独立 `.anim` |
| `FighterDataEditor.cs` | 92 | FighterData 自定义 Inspector |
| `HitboxSceneSyncEditor.cs` | 50 | HitboxSceneSync 的自定义 Inspector |
| `FrameMeterPrefabBuilder.cs` | 116 | 一键生成 FrameMeter 预设体 |
| `PlayerPrefsCleaner.cs` | 20 | 清空 PlayerPrefs 的调试菜单 |

### Test

| 脚本 | 行 | 职责 |
|---|---|---|
| `TestEndMatch.cs` | 14 | 调试用：开局 2 秒强制结束比赛（**易残留，用完必删**） |

---

## 三、分层与依赖方向

```
┌─────────────────────────────────────────────────────────┐
│  Input 层    InputManager ──调用公共方法──┐              │
└──────────────────────────────────────────┼──────────────┘
                                           ▼
┌─────────────────────────────────────────────────────────┐
│  Gameplay 层   FighterController（上帝类，1236 行）      │
│                  └─ FighterStateMachine（纯逻辑）        │
│                  └─ ShieldVisual / VisualRootMotion      │
└───────┬──────────────────────────────┬──────────────────┘
        │ 直接调用                      │ 事件广播
        ▼                              ▼
┌──────────────────────┐   ┌──────────────────────────────┐
│ Combat 层            │   │ Manager 层                   │
│  Hitbox / Hurtbox    │   │  MatchManager                │
│  HitboxEventRelay    │   │  CameraManager               │
│  DamageSystem(静态)  │   │  （订阅 OnDamaged/OnKilled）  │
│  Projectile          │   └──────────────────────────────┘
└──────────────────────┘   ┌──────────────────────────────┐
┌──────────────────────┐   │ Core 层                      │
│ Stage 层             │   │  GameManager(单例)           │
│  BlastZone/Platform  │   │  ObjectPooler(单例)          │
└──────────────────────┘   └──────────────────────────────┘
┌─────────────────────────────────────────────────────────┐
│  Data 层   FighterData(SO) / GameSettings(SO) / AttackData│
└─────────────────────────────────────────────────────────┘
```

**依赖方向总原则**：上层知道下层，下层不知道上层；跨层靠**事件**回调，同级靠**直接引用**。

**核心事实（必须知道）**：`FighterController` 是唯一的中枢，1236 行里塞了 8 大职责：
计时器 / 重力 / 移动 / 动画同步 / 攻击与连段 / 防御与盾反 / 抓取与投掷 / 受击与重生。
**没有** `MovementHandler`、`AttackManager`、`DefenseManager` —— 文档里的这些类从未被创建。

---

## 四、模块间通信方式（5 种）

### 4.1 单例直取（全局唯一资源）

```csharp
GameManager.Instance.gameSettings      // 读规则
GameManager.Instance.ActivePlayers     // 读所有活跃斗士
GameManager.Instance.RegisterFighter() // 注册
ObjectPooler.Instance.Spawn()          // 取对象
```

使用者：`FighterController`、`MatchManager`、`Hitbox`、`InputManager`、`CameraManager`。

### 4.2 Inspector 序列化引用（同一场景内固定关系）

| 持有方 | 字段 | 指向 |
|---|---|---|
| FighterController | `fighterData` | FighterData 资产 |
| FighterController | `animator` / `rb` / `mainCollider` / `hurtbox` / `visual` | 自身组件与子物体 |
| FighterController | `groundCheck` + `groundLayer` | 地面检测点与层遮罩 |
| FighterController | `projectilePrefab` | 投射物预制体 |
| MatchManager | `cameraManager` / `spawnPoints[]` / `hudPrefab` | 场景对象与 UI 预制体 |
| HitboxEventRelay | `target` | 真正的 Hitbox |
| HitboxSceneSync | `fighter` | 所属角色 |
| ShieldVisual | `fighter` | 所属角色 |
| HitboxPreview | `data` + `attackField` | 角色数据 + 招式字段名 |

`FighterController.Awake()` 里对 `rb/animator/hurtbox/mainCollider` 都有 `GetComponent` 兜底，
并会把所有子 `Hitbox.owner` 强制设为自己（**防自伤的关键**）。

### 4.3 C# 事件 / Action 委托（跨层解耦，本项目最主流的通信方式）

| 事件 | 声明处 | 谁订阅 | 用途 |
|---|---|---|---|
| `OnStateChanged(prev, next)` | FighterStateMachine → 转发到 FighterController | 外部（UI/诊断） | 状态变化通知 |
| `OnDamaged(damage, attacker)` | FighterController | **MatchManager** | 刷新伤害 UI |
| `OnKilled(victim)` | FighterController | **MatchManager** | 扣命 UI + 判断重生 |
| `OnParrySuccess(attacker)` | FighterController | （预留，暂无订阅） | 盾反反馈 |
| `OnGameStateChanged(state)` | GameManager | **MatchManager**、UI | 阶段切换驱动 |
| `OnTimerUpdated(remaining)` | MatchManager | 自身 handler → StockDisplay | 计时刷新 |
| `OnGameOver(winnerID)` | MatchManager | 自身 handler | 结束处理 |
| `OnPlayerScored(id)` | MatchManager | **无（死事件）** | 未实现 |
| `OnPlayerOutOfBounds(fighter)` | BlastZone | （由 MatchManager 直接调方法） | 出界通知 |
| `OnHit(fighter)` | Hitbox | （预留） | 命中回调 |
| `OnStateChanged` | FighterController（对外） | （预留） | 状态广播 |

**订阅纪律**：`MatchManager` 用 `Dictionary<FighterController, Action>` 缓存委托，
退订时精确移除（`_damagedHandlers` / `_killedHandlers`），避免闭包无法退订的经典坑。

### 4.4 Animation Event（动画 → 代码的跨界通道）

```
动画时间轴上的事件
      │  （挂在 Visual 子物体上）
      ▼
HitboxEventRelay.Activate()  / Deactivate() / AttackFinished()
      │  转发
      ▼
Hitbox.Activate() / Deactivate() / AttackFinished()
```

- `Activate()` → 开判定框 + 置 `owner.hitboxActivatedThisAttack = true`
- `Deactivate()` → 关判定框
- `AttackFinished()` → 置 `owner.isAttacking = false` + 状态机回 Idle

【注意】动画事件是**攻击判定的唯一权威时间源**，
`AttackDataEventSync` 工具就是用它的时间来反向校准 `startup/active/recovery` 的。

### 4.5 双向引用（仅抓取系统）

```csharp
FighterController grabTarget;  // 我抓着谁
FighterController grabber;     // 谁抓着我
```
必须**成对设置、对称清理**（`ReleaseGrab()` 同时处理"我是抓取方"和"我是被抓方"两个分支）。

---

## 五、单模块数据传输（数据怎么流）

### 5.1 输入数据流

```
InputAction 回调（事件驱动，任意时刻）
   │  只写字段：MoveInput / JumpPressed / AttackPressed / AttackHeld /
   │            AttackPressTime / ShieldHeld / GrabPressed / SpecialPressed
   ▼
InputManager.Update()（每帧消费）
   │  ├─ 无条件写 fighterController.MoveInput = MoveInput
   │  ├─ 调公共方法：TryJump / TryAttack / StartCharge / ReleaseCharge /
   │  │            TrySpecial / TryGrab / SetShielding / DropThroughPlatform
   │  └─ 写 fighterController.TechInputHeld = ShieldHeld
   ▼
FighterController 内部状态
```

**设计要点**：回调只写"原始意图"，Update 里做"语义决策"（选哪个招、是否蓄力）。

### 5.2 攻击数据流（最关键的一条链）

```
FighterData(SO) 里的 AttackData 实例（jab1/tiltUp/smashSide/... 共 23 个槽）
   │  InputManager 根据「地面/空中 + 方向 + 短按/长按」选出 AttackData
   ▼
FighterController.TryAttack(data) / StartCharge(baseData)
   │  ├─ attackData = data
   │  ├─ animator.SetInteger("AttackType", data.animIndex)   ← 动画靠这个整数选 clip
   │  ├─ foreach (hb in GetComponentsInChildren<Hitbox>()) hb.attackData = data
   │  └─ StateMachine.TransitionTo(Attack)
   ▼
Hitbox.attackData（每个判定框都持有同一份引用）
   │  动画事件 Activate() → OnTriggerEnter2D 命中
   ▼
hurtbox.owner.ApplyDamage(hitbox.attackData, hitbox.owner)
```

【重点】`AttackData` 是 **class（引用类型）**，所以
- 广播给 Hitbox 时是**传引用**，改一处全改；
- 蓄力时**必须 `CloneAttackData()` 深拷贝**再改数值，否则会永久污染 ScriptableObject 资产。

### 5.3 伤害与击飞数据流

```
ApplyDamage(attack, attacker)
   │  ① 无敌 / 死亡 → return
   │  ② isShielding → Parry 窗口判断 / 扣盾 / 破盾 → return（不掉血不击飞）
   │  ③ CurrentDamage += attack.damage * GameSettings.damageRatio
   │  ④ OnDamaged?.Invoke(伤害, 攻击者)          → MatchManager 刷 UI
   ▼
DamageSystem.CalculateKnockbackVelocity(attack, 目标当前%, 目标weight) → 速度标量
DamageSystem.CalculateKnockbackDirection(attack, 位置差)             → 方向向量
   ▼
ApplyKnockback(dir, speed)
   │  knockbackToken++            ← 令牌，作废旧硬直协程
   │  knockbackVelocity = dir * speed
   │  isInKnockback = true
   │  StateMachine → Knockback
   │  StartCoroutine(EndHitstunAfter(硬直时长))
   ▼
FixedUpdate: rb.velocity = knockbackVelocity（每帧 ×0.98 衰减）
   ▼
硬直结束 → 空中：惯性交接给 velocity + 恢复 1 次跳跃 → Fall
         → 地面：直接回 Idle
```

### 5.4 状态数据流（单一真相源）

```
FighterStateMachine.CurrentState   ← 唯一真相
   │  变化时
   ▼
OnStateChanged(prev, next)
   │  FighterController.Awake 里转发给自己的 OnStateChanged
   ▼
FighterController.UpdateAnimation()
   │  仅在 CurrentState != lastAnimState 时写：
   │  animator.SetInteger("State", (int)CurrentState)
   ▼
Animator 状态机（AnyState 转换靠 State 整数比较）
```

【重点】`State` 参数**只在变化时写**，每帧写会触发 AnyState 重入循环。
【重点】Action Layer（索引 1）权重是"总开关"：
`(isAttacking || isShielding) ? 1 : 0`，进攻击立即抬到 1，退出时 Lerp 平滑回落。

### 5.5 UI 数据流

```
FighterController.OnDamaged / OnKilled
   │  MatchManager 订阅（按 fighter 缓存委托）
   ▼
OnFighterDamagedHandler(victim, dmg, attacker)
   │  用 victim.playerID 当数组下标
   ▼
_damageDisplays[id].SetPercent(...)  /  _stockDisplays[id].SetStock(...)
```

【注意】UI 靠 `playerID` 索引，所以 HUD 预设体的层级命名必须是
`P1_HUD/DamageDisplay`、`P2_HUD/StockDisplay` 这种约定式路径（代码用 `transform.Find` 找）。

---

## 六、核心流程逐条拆解

> 【说明】本章是**概览版**，用来快速定位"某个功能在哪一段代码里"。
> 需要**代码级逐行拆解 + 为什么这么写**，直接看：
> - **第十一章**：完整执行链路（从按键到击飞，13 个阶段逐阶段拆代码）
> - **第十二章**：代码编写原理与设计意图（这个项目为什么长这样）

### 6.1 启动链路

```
GameManager.Awake()          单例 + DontDestroyOnLoad
GameManager.Start()          skipToBattle ? StartMatch() : SwitchState(Title)
                             StartMatch() = SwitchState(GameState.Battle)
MatchManager.Start()         读 GameSettings → MatchTimeRemaining
                             SpawnAndBindHUD()  ← 实例化 UI_HUD 预设体并绑定
FighterController.Start()    满盾 / 满跳跃 / 满命数 → GameManager.RegisterFighter(this)
GameManager.OnGameStateChanged == Battle
                             → MatchManager.OnGameStateChangedHandler
                             → StartMatch()（用 _matchStarted 防重入）
                             → 重新绑定所有 Fighter 事件
```

### 6.2 输入 → 动作决策（`InputManager.Update`，优先级从上到下）

```
① MoveInput 无条件写入 fighterController.MoveInput

② 跳跃：
   空中按下推 + 在地面 → DropThroughPlatform()（落穿平台）
   否则                → TryJump()

③ 攻击（AttackPressed 的 4 分支，顺序敏感）：
   a. 状态 == Grabbed  → OnMashGrabEscape()（挣扎）
   b. 状态 == Grab     → 只消费按键，交给 TryAttack 内部拦截去投掷
                          【注意】绝不能掉进下面的判定窗，否则长按会 StartCharge
                          把 Grab 顶成 Attack → 投掷失效 + 双方死锁
   c. 空中 或 地面无方向 → GetImmediateAttack() → TryAttack()（零延迟）
   d. 地面 + 有方向     → pendingAttackDir = MoveInput（方向快照）
                          chargePending = true  → 等 0.15s 判定窗

④ 判定窗（每帧查）：
   held >= longPressThreshold(0.15s) → StartCharge(GetSmashAttack(方向快照))
   松手且未到阈值                     → TryAttack(GetTiltAttack(方向快照))

⑤ 蓄力中松手 → ReleaseCharge()

⑥ Special → TrySpecial(GetContextualSpecial())

⑦ Grab：已抓人 → TryAttack(jab1)（内部拦截转投掷）；未抓 → TryGrab()

⑧ SetShielding(ShieldHeld)   ← 每帧调用，内部自己做边沿判断
⑨ TechInputHeld = ShieldHeld ← 受身输入复用护盾键
```

【重点】方向必须用**按下瞬间的快照** `pendingAttackDir`，不能用当前 `MoveInput`，
否则玩家"按攻击→推方向"的操作会串味。

### 6.3 `FighterController.Update()` 主循环

```
1. IsGrounded = Physics2D.OverlapCircle(groundCheck, radius, groundLayer)
2. 土狼时间倒计时 / 跳跃缓冲倒计时
3. 状态 == Dead  → 关 visual + 关 mainCollider + 关 hurtbox，return
4. 状态 == Grabbed → Lerp 到抓取者面前 + 清速度 + 写 State 参数，return
5. UpdateTimers()   蓄力/攻击兜底/连段/无敌/护盾/眩晕/抓取超时
6. UpdateGravity()  重力 + 受身判定 + 落地处理 + 下落速度上限
7. 快速下落（Fall 中按住下 + 速度 < -4 → 强制提速）
8. 双击方向键检测 → isRunning
9. UpdateMovement() 选速度（跑/走/空） + 切 Idle/Run/Jump/Fall
10. UpdateAnimation()
11. 朝向：有输入按输入方向，无输入自动面向对手
```

### 6.4 `FighterController.FixedUpdate()`

```
Dead → return
isInKnockback → rb.velocity = knockbackVelocity；并 ×0.98 衰减
否则          → rb.velocity = velocity
```

【重点】物理赋值必须放 FixedUpdate，逻辑决策放 Update —— 这是物理稳定性的前提。

### 6.5 攻击命中链路（含打击感三件套）

```
动画播到判定帧 → Animation Event
   → HitboxEventRelay.Activate()
   → Hitbox.Activate()：
        IsActive = true
        owner.hitboxActivatedThisAttack = true
        collider.enabled = false; collider.enabled = true;   ← 强制 off→on 翻转
        （原因：Unity 的 OnTriggerEnter2D 是"边沿触发"，不翻转则重复攻击不触发）

OnTriggerEnter2D(other)
   → other.GetComponent<Hurtbox>()，且 hurtbox.owner != owner（防自伤）
   → hurtbox.owner.ApplyDamage(attackData, owner)
   → CameraManager.Shake(0.3f, 0.15f)                  ← 打击感 1：震屏
   → Instantiate(hitEffectPrefab)，0.1s 后销毁            ← 打击感 2：特效
   → HitstopRoutine()：Time.timeScale = 0               ← 打击感 3：卡帧
        WaitForSecondsRealtime(duration)   ← 必须用 Realtime，timeScale=0 时普通 Wait 不走
        Time.timeScale = 1
```

### 6.6 击飞与硬直

```
ApplyKnockback(dir, speed, hitstunOverride = -1)
   knockbackToken++                       ← 令牌，让上一次的硬直协程失效
   knockbackVelocity = dir.normalized * speed
   isInKnockback = true
   hasLeftGroundInKnockback = false
   hitstun = (override >= 0) ? override : DamageSystem.CalculateHitstun(speed)
   StateMachine → Knockback
   StartCoroutine(EndHitstunAfter(hitstun))

EndHitstunAfter(duration)
   yield WaitForSeconds(duration)
   if (token != knockbackToken) yield break      ← 期间又被击飞 → 本次作废
   if (isInKnockback)
       空中 → velocity = knockbackVelocity（惯性交接）
              remainingJumps = Max(1, jumpCount - 1)   ← 大乱斗规则：必给一跳回场
              → Fall
       地面 → 清击飞速度 → Idle
```

### 6.7 死亡 / 重生 / 淘汰

```
BlastZone.OnTriggerEnter2D
   → MatchManager.OnPlayerOutOfBounds(fighter, side)
        fighter.Kill()
            剩余命数 = Max(0, stocks - 1)
            StateMachine → Dead
            OnKilled?.Invoke(this)     → MatchManager 刷 Stock UI
        if (Stock 模式 && 剩余命 > 0) → StartCoroutine(RespawnAfterDelay(fighter))
        else                        → 淘汰，不重生

RespawnAfterDelay(3s)
   重新注册到 GameManager（_registeredFighters 去重）
   RebindFighterEvents(f)              ← 重新订阅（先退订再订阅，防重复）
   fighter.Respawn(随机出生点)
      位置复位 / 伤害归零 / 清速度 / 无敌(respawnTime) / 满盾 / 满跳跃
      StateMachine.Initialize(Idle)   ← 必须 Initialize，Dead 状态无法 TransitionTo(Idle)
      恢复 visual / mainCollider / hurtbox
   同步 Stock UI + Damage UI（归零 + ResetDisplay）

比赛结束判定（MatchFlow 协程内，命数制每帧查）
   playersInGame = ActivePlayers.FindAll(p => 状态 != Dead || (Dead && stocks > 0))
   if (playersInGame.Count <= 1) → 等 3s → EndMatch(winnerID)
```

### 6.8 抓取 / 挣扎 / 投掷

```
TryGrab()
   守卫：击飞中/死亡/攻击中/已在抓/正被抓 → return
   前方圆检测：center = 自身位置 + 朝向 × grabRange
              Physics2D.OverlapCircleAll(center, grabRange)
   筛选可抓目标：Hurtbox.owner != 自己
                 且 (CanAct() 或 状态 == Shield)     ← 大乱斗规则：盾可以被抓
                 且 非无敌、非已被抓
   没找到 → grabWhiff = true，进 Grab 播挥空动作，0.3s 后回 Idle
   找到   → 双方 isShielding = false（防止"隐形盾"残留）
            grabTarget = victim；victim.grabber = this
            自身 → Grab 状态；victim → Grabbed 状态
            Physics2D.IgnoreCollision(双方主碰撞体, true)   ← 抓取期间不互相推挤

抓取中（UpdateTimers 里计时）
   grabWhiff      → 0.3s 后回 Idle
   grabTimer > 5s → 自动松开（先存 victim 引用，再 ReleaseGrab，最后恢复碰撞）

挣扎（Grabbed 时按攻击键）
   escapeMashCount++；达到 escapeMashThreshold(8) → ReleaseGrab + 恢复碰撞

投掷（Grab 时按攻击/抓取键）
   InputManager → TryAttack(...) → 被 TryAttack 开头拦截
   → PerformThrow(GetThrowDirection())
        GetThrowDirection：垂直优先（ay > ax 且 > 0.5）→ 上/下投
                           水平 → 与朝向同向 = 前投，反向 = 后投
                           无方向 → 默认前投（防御性兜底）
        选 AttackData（throwForward/Back/Up/Down）
        ReleaseGrab()                      ← 先解除抓取，让对方状态干净
        播投掷动画 + 进 Attack 状态
        伤害照常累加 + OnDamaged
        【注意】方向不能复用 CalculateKnockbackDirection（那是"被打过来的方向"）
                必须按自身朝向构造：前/后 = 朝向向量，上/下 = Vector2.up/down
        瞬移受害人：victim.rb.position = rb.position + throwDir * 1.5f
                    【注意】必须用 rb.position 而非 transform.position，
                            物理系统会把 transform 的值"拉回"校正
        计算击飞速度 → victim.ApplyKnockback()
        0.6s 后恢复碰撞（等 victim 飞出身体范围）
        ReleaseGrab()（第二次，作为唯一出口）

ReleaseGrab() —— 对称清理，两个分支都写
   我是抓取方：清 grabTarget、清对方 grabber、重置对方挣扎计数、
              对方 Grabbed → Idle、自己 Grab → Idle
   我是被抓方：清 grabber、清对方 grabTarget、对方 Grab → Idle、自己 Grabbed → Idle
```

### 6.9 防御 / 盾反 / 破盾

```
SetShielding(active)   ← InputManager 每帧调用，内部判边沿
   守卫：击飞中/死亡/眩晕/盾硬直/抓取中/被抓中 → return
   active && 状态 != Shield  → shieldStartFrame = Time.frameCount（窗口起点）
                               isShielding = true → Shield 状态
   !active && 状态 == Shield → isShielding = false → Idle

ApplyDamage 里的盾分支
   isShielding 时：
       if (Time.frameCount - shieldStartFrame <= parryWindowFrames(10))
            → PerformParry(attacker)
                 攻击方 EnterStun(parryStunDuration 1.2s)
                 CameraManager.FlashWhite(0.15s)
                 OnParrySuccess 事件
                 return（自己无伤、不掉盾）
       else → currentShieldHP -= DamageSystem.CalculateShieldDamage(attack, 盾HP)
              if (盾HP <= 0) BreakShield()
              return（关键：盾防走完直接返回，不掉血不击飞）

护盾耐久（UpdateTimers）
   isShielding          → 每帧 -= shieldRegenPerSecond * dt
   未举盾且未满         → 每帧 += shieldRegenPerSecond * dt
   【注意】字段名叫 shieldRegenPerSecond（恢复速度），但实际被当作"消耗速度"复用

BreakShield() → EnterStun(shieldBreakStunDuration 1.5s)
```

### 6.10 蓄力（Smash，长按机制）

```
InputManager：地面 + 有方向 + 按住 >= 0.15s
   → StartCharge(smashSide/Up/Down)
        isCharging = true；chargeTimer = 0
        isAttacking = true（锁移动）；attackFallbackTimer = 1.2s
        状态 → Attack（复用攻击态）
        animator.SetInteger("AttackType", 200/210/220)

UpdateTimers 每帧：chargeTimer += dt
   满 maxChargeTime(0.8s) → 钳住 + 自动 ReleaseCharge()
   被打断（状态 != Attack）→ isCharging = false（蓄力作废）

松手 → ReleaseCharge()
   lvl = Clamp01(chargeTimer / maxChargeTime)
   final = CloneAttackData(chargeBaseData)     ← 必须深拷贝！
   final.damage        *= Lerp(1, 1.25, lvl)
   final.knockbackBase *= Lerp(1, 1.15, lvl)
   正常起手（复刻 TryAttack 后半段）
```

### 6.11 连段（Jab 三连）

```
进入条件：attackData 是 jab1/jab2/jab3 之一，comboStep < 2，
          inputBuffer 有缓冲 或 当帧按下，
          且 IsInComboWindow()（Action Layer 的 normalizedTime 在 0.3~1.0）
          且 hitboxActivatedThisAttack（本次攻击判定框已开过，防提前取消吞判定）

推进：comboStep++；animator.SetInteger("ComboStep", comboStep)
      attackData = GetComboAttack(comboStep)
      重新广播给所有 Hitbox
      状态机再 TransitionTo(Attack)（Attack → Attack 是允许的，即连段取消）

提前按太早 → inputBuffer.BufferAttack() 缓存，窗口到了自动消费
```

### 6.12 受身（Tech）

```
触发点：UpdateGravity 里 isInKnockback && IsGrounded 时
        TechInputHeld（= 护盾键）按住 → PerformTech()
        或 hasLeftGroundInKnockback 为真（真击飞落地）→ 进入可受身硬直

PerformTech()
   CameraManager.FlashWhite(0.2s)
   清击飞速度 + isInKnockback = false
   velocity.y = techBounceSpeed(6)     ← 小反弹
   isInvincible = true；respawnTimer = techInvincibleTime(0.5s)   ← 复用重生无敌计时器
   状态 → Idle / Fall

【注意】hasLeftGroundInKnockback 标记是必须的 ——
没有它，"贴地击飞"永远不满足"离过地"条件，受身永远不触发。
```

### 6.13 UI 数据流

```
MatchManager.SpawnAndBindHUD()
   Instantiate(hudPrefab) → "UI_HUD (Runtime)"
   按约定路径 transform.Find("P1_HUD/DamageDisplay") 等取组件
   SetFighterId(i) 注入下标
   StockDisplay.Init(stockCount, matchTimeSeconds)
   订阅自己的 OnTimerUpdated / OnGameOver
   订阅所有 Fighter 的 OnDamaged / OnKilled（Dictionary 缓存委托）
   订阅 GameManager.OnGameStateChanged
```

### 6.14 相机

```
CameraManager（LateUpdate 时序）
   收集 GameManager.ActivePlayers 的位置
   算包围盒 → 中心点 + 需要的正交尺寸
   尺寸夹在 [minZoom, maxZoom]，SmoothDamp 平滑
   Shake(intensity, duration)：叠加随机偏移
   FlashWhite(duration)：全屏白闪（盾反 / 受身用）
```

【注意】必须 LateUpdate —— 要在所有角色移动完成后才算相机位置，否则抖动。

---

## 七、数据结构一览

### FighterData（ScriptableObject，每个角色一份）

| 分组 | 字段 |
|---|---|
| Identity | `fighterName` `portraitIcon` |
| Movement | `weight` `walkSpeed` `runSpeed` `airSpeed` `jumpForce` `doubleJumpForce` `jumpCount` `airDodgeCount` `fallSpeed` `fastFallSpeed` |
| Visual | `weightClass` `uiColor` |
| Attack Data | `jab1/2/3`、`tiltSide/Up/Down`、`smashSide/Up/Down`、`aerialNeutral/Forward/Back/Up/Down`、`specialNeutral/Side/Up/Down`、`grab`、`throwForward/Back/Up/Down`（**共 23 个槽**） |

### AttackData（内嵌 class，非 SO）

| 分组 | 字段 |
|---|---|
| Animation | `animIndex`（→ Animator 的 AttackType 整数） |
| Damage | `attackName` `damage` `shieldDamage` |
| Knockback | `knockbackAngle` `knockbackBase` `knockbackGrowth` `hitstunOverride` |
| Timing | `startupTime` `activeTime` `recoveryTime`（秒）；`TotalDuration` 只读属性 |
| Hitstop | `hitstopDuration` |
| Cancel | `cancelIntoAttacks[]` `canJumpCancel` `canSpecialCancel`（**全部未被读取**） |
| Visual | `hitEffectPrefab` `hitSound` |
| Hitbox | `hitboxOffset` `hitboxSize` |

**AnimIndex 编号规约**（从代码与工具反推）：

| 区间 | 招式 |
|---|---|
| 0-2 | Jab 1/2/3 |
| 10-12 | Tilt 侧/上/下 |
| 20-22 | Smash 侧/上/下 |
| 30-34 | 空中 N/F/B/U/D |
| 40-43 | 必杀 N/S/U/D |
| 50-53 | 投技 F/B/U/D |
| 200 / 210 / 220 | 蓄力 侧/上/下 |

### GameSettings（ScriptableObject，全局一份）

| 分组 | 字段 | 是否被运行时读取 |
|---|---|---|
| Match Rules | `matchMode` `stockCount` `matchTimeSeconds` `damageRatio` | 是 |
| Match Rules | `enableTeamMode` | **否** |
| Spawn | `respawnTime` | 是 |
| Spawn | `blastZoneWidth/Height` `respawnInvincibilityFrames` | **否** |
| Combat | `hitstopScale` `shieldMaxHP` `shieldRegenPerSecond` | 是 |
| Combat | `shieldSize` `enableFriendlyFire` | **否** |
| Items | `enableItems` `itemSpawnInterval` `maxItemsOnField` | **否** |
| Character Select | `selectableFighters` + `SetSelection/GetSelection`（跨场景保持选人结果） | 是 |

### 输入参数表（Animator 参数，代码写 / 动画读）

| 参数 | 类型 | 写入处 |
|---|---|---|
| `State` | int | 状态变化时写 `(int)CurrentState` |
| `AttackType` | int | `SetAttackAnim()` 写 `data.animIndex` |
| `ComboStep` | int | 连段推进 / 起手重置 / 兜底重置 |
| `Speed` | float | `Abs(velocity.x)` |
| `IsGrounded` | bool | `IsGrounded` |
| `AirFactor` | float | 地面 0 / 空中 1（地面/空中攻击变体） |
| `VerticalSpeed` | float | `velocity.y` |
| `Damage` | float | `CurrentDamage` |

---

## 八、已实现 vs 未实现

### 已实现（可用）

- 移动 / 跑步（双击）/ 跳跃（二段 + 土狼时间 + 跳跃缓冲）/ 快速下落 / 落穿平台
- 地面攻击：Jab 三连、Tilt 三向、Smash 三向（长按蓄力）
- 空中攻击：空 N / 空前 / 空后 / 空上 / 空下
- 必杀技：N / S / U / D 四向数据槽（投射物型已接通）
- 防御 / 盾反（10 帧窗口）/ 破盾眩晕
- 抓取 / 挣扎逃脱 / 四向投掷
- 受击 / 击飞 / 硬直 / 受身（Tech）
- 出界 / 扣命 / 重生 / 无敌 / 淘汰判定
- 命数制 + 时间制两种比赛规则
- HUD（伤害百分比 / 命数 / 计时器）
- 相机多目标跟随 + 自适应缩放 + 震屏 + 闪白
- 打击感三件套（震屏 / 特效 / Hitstop）
- 帧数表 FrameMeter（60 格三段色）
- Editor 工具链（一键搭 Demo、帧数表同步、clip 导出、判定框可视化编辑）

### 未实现 / 死代码（重要）

| 项 | 状态 |
|---|---|
| `AttackData.cancelIntoAttacks / canJumpCancel / canSpecialCancel` | **死字段**。只在 `CloneAttackData` 里被复制，无任何逻辑读取 → 取消窗口系统未实现 |
| `FighterController.attackTimer` | **只写不读**。5 处赋值、0 处读取（FrameMeter 明确注释"自记，不依赖 attackTimer"） |
| `DamageSystem.CalculateFinalDamage()` | **死方法**。FighterController 里是手写内联的 `attack.damage * damageRatio` |
| `MatchManager.OnPlayerScored` | **死事件**。声明后从未 Invoke |
| `InputManager.TauntPressed` | **写入但从不消费** → 嘲讽功能未实现 |
| `StageData` | **整个类未被运行时读取**（只有 DemoSetupWizard 创建它） |
| `GameSettings` 的 items / team / friendlyFire / shieldSize / blastZone 尺寸 / 重生无敌帧 | **配置了但运行时无人读** |
| `FighterData.doubleJumpForce` / `airDodgeCount` | **未被逻辑使用**（二段跳用的是 `jumpForce * 0.85` 硬编码） |
| `Attack_Smash*` 与 `Attack_Tilt*` | 共用同一 clip，靠 Transition Offset 区分（**当前全为 0，未设**） |
| `Grab` 状态 | 错指 `Grabbed.anim`（`Grab.anim` 是孤儿资产） |
| `Attack_Special*` 的 Motion | 全为空 |
| 空中闪避 | 未实现（字段有，逻辑无） |
| 道具系统 / 团队模式 / 友军伤害 | 未实现（配置有，逻辑无） |
| 音效 | 未接入（多处 `// TODO(M8)` 占位） |
| 联机 | 未实现（有 `Docs/14_局域网联机扩展方案.md` 方案文档） |

---

## 九、上手路径建议（给不熟悉项目的你）

### 第 1 步：跑起来
1. 打开 `Assets/_Game/Scenes/BattleTest.unity`
2. 确认 `Project Settings → Player → Active Input Handling = Both`
3. Play，用 P1 键盘（WASD + J/K/L/U）操作

### 第 2 步：读代码的顺序（按理解成本递增）

```
1. FighterStateMachine.cs（134 行）    ← 先懂"有哪些状态、怎么转"
2. FighterData.cs + GameSettings.cs    ← 再懂"数据长什么样"
3. DamageSystem.cs（101 行）           ← 纯公式，最好懂
4. InputManager.cs（394 行）           ← 懂"输入怎么变成动作"
5. FighterController.cs（1236 行）     ← 核心，按方法分组读：
      Awake/Start → Update → FixedUpdate
      UpdateTimers / UpdateGravity / UpdateMovement / UpdateAnimation
      SetShielding / TryJump / TryAttack / TryGrab
      ApplyDamage / ApplyKnockback / PerformThrow / Respawn
6. Hitbox.cs + Hurtbox.cs              ← 懂"命中怎么发生"
7. MatchManager.cs                     ← 懂"一局怎么跑完"
```

### 第 3 步：改代码前必读的"陷阱清单"

| 陷阱 | 原因 |
|---|---|
| 改 AttackData 数值前先 `CloneAttackData()` | 它是引用类型，直接改会污染 ScriptableObject 资产 |
| 物理赋值只在 FixedUpdate | Update 里改 transform 会被物理系统校正/抖动 |
| 瞬移 Rigidbody2D 用 `rb.position` | 用 `transform.position` 会被物理系统拉回 |
| `State` 参数只在变化时写 | 每帧写会触发 AnyState 重入循环 |
| Action Layer 权重是总开关 | `m_DefaultWeight = 0`，不主动 `SetLayerWeight` 就完全不显示 |
| 动画事件是攻击判定的唯一权威时间源 | 改 clip 后必须跑 `Tools/SuperSmashLike/帧数表同步` |
| Hitbox 激活要 `off→on` 强制翻转 | Unity 的 OnTriggerEnter2D 是边沿触发，不翻转则重复攻击不触发 |
| 抓取期间必须 `IgnoreCollision` | 否则被抓者会被"拖着走" |
| ReleaseGrab 前先存 victim 引用 | 它会把 `grabTarget` 清空 |
| Awake 里所有组件引用都要 `GetComponent` 兜底 | 漏拖一个就 NRE，且异常冒泡会让上层逻辑错乱 |
| 缓存的委托退订要精确移除 | 闭包每次 `+=` 都是新实例，退不掉会内存泄漏 + 重复触发 |

---

## 十、与旧文档的差异说明

`Docs/` 下的 01/02 号文档写于项目早期（2026-07），描述的是**设计意图**，与当前实现有偏差：

| 旧文档写的 | 实际情况 |
|---|---|
| `MovementHandler` / `AttackManager` / `DefenseManager` / `AnimationController` | **不存在**，全部在 FighterController 内 |
| `CombatSystem` | 不存在，实际是 `Hitbox` + `Hurtbox` + `DamageSystem` |
| `KnockbackSystem` | 不存在，实际是 `FighterController.ApplyKnockback` + `DamageSystem` |
| `HUDManager` / `AudioMgr` / `ItemSystem` | 不存在（HUD 由 MatchManager 直接管） |
| `PlayerInputComponent` | 实际是 `InputManager`（手动订阅，非 PlayerInput 组件） |
| 攻击用帧数（startupFrames） | 实际用**秒**（startupTime），帧数只在 FrameMeter 显示时换算 |

---

## 十一、完整执行链路：从按键到击飞（代码级逐阶段拆解）

> 本章是第六章的**代码级详版**。每个阶段给：触发时机 / 真实代码 / 为什么这么写 / 改哪里会出问题。
> 场景设定：P1 在地面、没推方向、按下 J 键、对手在正前方。

### 11.0 先建立时间轴概念（Unity 主循环约定）

读任何 Unity 项目，先搞清楚"哪段代码什么时候跑"。本项目的分工：

| 时机 | 频率 | 本项目用它做什么 |
|---|---|---|
| `Awake` | 对象创建时一次 | 取组件引用、建立从属关系（谁是谁的 owner） |
| `OnEnable` | 每次启用 | 订阅事件、启用输入地图 |
| `Start` | 第一帧前一次 | 读配置、注册自己、初始化数值 |
| `Update` | 每渲染帧 | **所有决策**（读输入、判状态、开计时器） |
| `FixedUpdate` | 固定步长（默认 0.02s） | **只写物理**（`rb.velocity = ...`） |
| `LateUpdate` | 所有 Update 之后 | 相机跟随、帧数表刷新（依赖别人位置算完） |
| 协程 `yield` | 按条件恢复 | 延时逻辑（硬直结束、延迟重生） |
| 物理回调 `OnTriggerEnter2D` | 引擎内部 | 命中检测、出界检测 |
| Animation Event | 动画时间轴 | 判定框开关、攻击结束 |

【重点】这张表是理解一切的前提。**同一个逻辑写在不同时机里，行为完全不同**：
- 把 `rb.velocity = v` 写进 `Update` → 物理抖动、穿墙
- 把相机跟随写进 `Update` → 镜头抖（角色还没动完就拍照了）
- 把"状态只在变化时写"这条忘掉 → Animator 每帧被写，AnyState 反复重入

### 11.1 阶段 0：开局注册链（一次性，先建立骨架）

```
① GameManager.Awake()
     SmashDebug.Settings = debugSettings;      // 必须最先，其他模块都要用
     if (Instance != null && Instance != this) { Destroy(gameObject); return; }
     Instance = this;
     DontDestroyOnLoad(gameObject);

② FighterController.Awake()
     if (rb == null) rb = GetComponent<Rigidbody2D>();            // 兜底
     if (animator == null) animator = GetComponentInChildren<Animator>();
     if (hurtbox == null) hurtbox = GetComponentInChildren<Hurtbox>();
     if (mainCollider == null) mainCollider = GetComponent<Collider2D>();
     if (hurtbox != null) hurtbox.owner = this;
     foreach (var hb in GetComponentsInChildren<Hitbox>(true)) hb.owner = this;   // 防自伤
     StateMachine.OnStateChanged += (prev, next) => OnStateChanged?.Invoke(prev, next);

③ FighterController.Start()
     StateMachine.Initialize(FighterState.Idle);
     currentShieldHP = GameManager.Instance.gameSettings.shieldMaxHP;
     remainingJumps  = fighterData.jumpCount;
     remainingStocks = GameManager.Instance.gameSettings.stockCount;
     GameManager.Instance.RegisterFighter(this);

④ MatchManager.Start()
     settings = GameManager.Instance.gameSettings;
     MatchTimeRemaining = settings.matchTimeSeconds;
     SpawnAndBindHUD();

⑤ MatchManager.OnGameStateChangedHandler(GameState.Battle)
     if (!_matchStarted) { StartMatch(); _matchStarted = true; }
     foreach (var f in ActivePlayers) RebindFighterEvents(f);
```

**为什么这么写**

| 设计 | 原因 |
|---|---|
| 第 ② 步所有引用都 `GetComponent` 兜底 | 漏拖一个就 NRE，而 **NRE 冒泡会让上层逻辑错乱**。历史踩坑：`mainCollider` 为空 → `IgnoreCollision(null)` 抛异常 → 输入处理链断 → 表现成"抓取键变成击退" |
| 第 ② 步强制设 `Hitbox.owner` | `Hitbox` 靠 `hurtbox.owner != owner` 排除自己。owner 没设 = 自己打自己 |
| 第 ③ 步注册放 `Start` 不放 `Awake` | `MatchManager` 也订阅了事件，要保证"所有对象 Awake 完再注册"，否则事件发出去没人接 |
| 第 ⑤ 步用 `_matchStarted` 防重入 | `OnGameStateChanged` 可能被多次触发（暂停→恢复），不加守卫会开两场比赛 |
| `MatchManager` 用 `Dictionary` 缓存委托 | 闭包每次 `+=` 都是**新委托实例**，不缓存就退订不掉 → 内存泄漏 + 事件重复触发 |

### 11.2 阶段 1：输入采集（事件驱动，发生在任意一帧）

```csharp
// InputManager.OnAttackCtx —— InputAction "Attack" 的 started 回调
private void OnAttackCtx(InputAction.CallbackContext ctx)
{
    AttackPressed   = true;
    AttackHeld      = true;
    AttackPressTime = Time.time;
}
```

**为什么这么写**

- 回调**只写字段，不做决策**。因为回调不知道上下文（在地面还是空中？有没有推方向？），把这些判断集中到 `Update` 里，逻辑只有一处、好调试。
- `AttackPressed` 是"边沿"（按下瞬间），`AttackHeld` 是"电平"（是否按住）。这两个语义分开，是为了支持"短按 Tilt / 长按 Smash"。
- `AttackPressTime` 记的是 `Time.time`（受 `timeScale` 影响）。Hitstop 时 `timeScale = 0`，时间冻结 —— 这正好让"长按判定"在卡帧期间也不会误推进。

【补充】本项目**不用** `PlayerInput` 组件的 Send Messages 模式，而是禁用组件 + JSON 克隆一份独立 `InputActionAsset` 手动订阅。原因见 `InputManager` 类头注释：`InputUser` 会给整个资产加 scheme 过滤，双人场景下会导致绑定解析全部失败（控件数=0）。

### 11.3 阶段 2：输入决策（`InputManager.Update`，每帧消费）

```csharp
if (AttackPressed)
{
    FighterState st = fighterController.StateMachine.CurrentState;

    // 分支 1：被抓 → 挣扎
    if (st == FighterState.Grabbed) { fighterController.OnMashGrabEscape(); chargePending = false; }

    // 分支 2：抓取中 → 只消费（投掷交给 TryAttack 内部拦截）
    else if (st == FighterState.Grab) { AttackPressed = false; }

    // 分支 3：空中 或 地面无方向 → 立即出招（零延迟）
    else if (fighterController.StateMachine.IsInAir()
             || (Mathf.Abs(MoveInput.x) <= 0.5f && Mathf.Abs(MoveInput.y) <= 0.5f))
    {
        AttackData attack = GetImmediateAttack();
        if (attack != null) fighterController.TryAttack(attack);
    }

    // 分支 4：地面 + 有方向 → 进判定窗（等 0.15s 分短按/长按）
    else { pendingAttackDir = MoveInput; chargePending = true; }

    AttackPressed = false;   // 消费
}
```

**为什么这么写**

| 点 | 原因 |
|---|---|
| **分支顺序敏感**，不能调换 | 分支 2 必须在 3/4 之前。否则"抓取中长按"会掉进判定窗 → `StartCharge` 把 `Grab` 状态顶成 `Attack` → **投掷失效 + 双方死锁**（真踩过的坑） |
| 分支 3 用 `IsInAir()` 而不是 `!IsGrounded` | `IsInAir()` 包含 `Knockback` 状态；`!IsGrounded` 会把"贴地滑行"也当成空中 |
| 分支 4 存 `pendingAttackDir` 快照 | 判定窗要等 0.15s。如果那时再读 `MoveInput`，玩家"先按攻击再推方向"的操作会串味。**快照按下瞬间的方向**才对 |
| `AttackPressed = false` 在最后统一消费 | 保证每个分支都只处理一次按键 |

判定窗（同帧后续）：

```csharp
if (chargePending)
{
    float held = Time.time - AttackPressTime;
    if (held >= longPressThreshold)          // 长按 → 蓄力
    { chargePending = false; fighterController.StartCharge(GetSmashAttack(pendingAttackDir)); }
    else if (!AttackHeld)                    // 松手且没到阈值 → 短按 → Tilt
    { chargePending = false; fighterController.TryAttack(GetTiltAttack(pendingAttackDir)); }
    // 都不到 → 继续等下一帧
}
```

### 11.4 阶段 3：攻击起手（`FighterController.TryAttack`）

```csharp
// 前置拦截：正抓着人 → 转投掷
if (grabTarget != null && StateMachine.CurrentState == FighterState.Grab)
{ PerformThrow(GetThrowDirection()); return; }

// 守卫
if (isInKnockback || isShielding || StateMachine.CurrentState == FighterState.Dead) return;

// 正在攻击中 → 连段分支
if (isAttacking)
{
    bool isJabChainNow = attackData == fighterData.jab1 || ... jab2 || ... jab3;
    if (isJabChainNow && comboStep < 2)
    {
        if (IsInComboWindow() && hitboxActivatedThisAttack) AdvanceCombo();
        else inputBuffer.BufferAttack();     // 按太早 → 缓冲，窗口到了自动消费
    }
    return;
}

// ===== 正常起手 =====
inputBuffer.Clear();
comboStep = 0;
SetAttackAnim(data);                          // animator.SetInteger("AttackType", data.animIndex)
animator.SetInteger("ComboStep", 0);
attackData = data;
foreach (var hb in GetComponentsInChildren<Hitbox>(true)) hb.attackData = attackData;  // 广播
isAttacking = true;
hitboxActivatedThisAttack = false;
attackTimer         = data.startupTime + data.activeTime + data.recoveryTime;
attackFallbackTimer = 1.2f;                   // 兜底：动画事件链断了也能收招
StateMachine.TransitionTo(FighterState.Attack);
```

**为什么这么写**

| 点 | 原因 |
|---|---|
| **广播 `attackData` 给所有子 Hitbox** | `Hitbox` 是独立组件，它不知道你选了哪个招。而且 `AttackData` 是 **class（引用类型）**，广播是传引用 —— 所以后面连段/蓄力换数据时**必须重新广播**，否则 Hitbox 还拿着旧数据 |
| 用 `attackFallbackTimer` 兜底 | 正常收招靠动画事件 `AttackFinished`。若事件链断（状态机重组、Motion 未配、relay target 空），`isAttacking` 会永远为 true → 状态机卡死在 `Attack`（`CanAct()==false` 不能动）。兜底 1.2s 覆盖所有动画时长 |
| `hitboxActivatedThisAttack = false` | 连段守卫。连段推进要求"本次攻击的判定框已经开过"，防止玩家按太快、判定还没出来就取消，把判定吞掉 |
| `inputBuffer.Clear()` 只在起手时清 | 连段分支里不清，因为缓冲正是给连段用的 |
| `TiltDown` 附带小跳 | 放在 `TransitionTo(Attack)` 之前、`IsGrounded = false` 强制离地 —— 否则同一帧后面的落地逻辑会把状态覆盖回 `Idle` |

### 11.5 阶段 4：状态 → 动画（分两步，中间隔 1~2 帧）

```csharp
// 第一步：状态机内部
public void TransitionTo(FighterState newState)
{
    if (!CanTransitionTo(newState))
    {
        if (SmashDebug.IsOn(DebugChannel.State))
            SmashDebug.Log(DebugChannel.State, $"切换被拒: {CurrentState} → {newState}");
        return;                              // 静默拒绝，只记日志
    }
    PreviousState = CurrentState;
    CurrentState = newState;
    OnStateChanged?.Invoke(PreviousState, newState);
}

// 第二步：下一帧的 UpdateAnimation()
if (StateMachine.CurrentState != lastAnimState)          // 只在变化时写
{
    animator.SetInteger("State", (int)StateMachine.CurrentState);   // Attack = 4
    lastAnimState = StateMachine.CurrentState;
}
animator.SetFloat("Speed", Mathf.Abs(velocity.x));
animator.SetBool("IsGrounded", IsGrounded);
animator.SetFloat("AirFactor", IsGrounded ? 0f : 1f);
animator.SetFloat("VerticalSpeed", velocity.y);
animator.SetFloat("Damage", CurrentDamage);

// Action Layer（索引 1）权重 = 总开关
float targetWeight = (isAttacking || isShielding) ? 1f : 0f;
float cur = animator.GetLayerWeight(1);
animator.SetLayerWeight(1, targetWeight > cur ? targetWeight : Mathf.Lerp(cur, targetWeight, 10f * Time.deltaTime));
```

Animator 侧：`AnyState` 的转换条件 `State == 4 && AttackType == 0` → 播 `Attack_Jab1`。

**为什么这么写**

| 点 | 原因 |
|---|---|
| `State` 参数**只在变化时写** | 每帧写会触发 AnyState 反复重入（动画抽搐）。`lastAnimState` 就是干这个的 |
| Action Layer 权重用"上升立即、下降 Lerp" | 进攻击要**零延迟**抬权重（否则起手姿势和 Idle 混合，看着软）；退出攻击要**平滑回落**（保留收招过渡） |
| 权重是 `(isAttacking \|\| isShielding)` | 这是历史踩坑点：`StartCharge` 忘了设 `isAttacking = true` → 权重恒 0 → 蓄力姿势完全不显示（状态机明明切了）。**层权重是总开关** |
| `animator.speed` 攻击时锁 1.0 | `animator.speed` 是全局缩放（影响所有层）。空中攻击时水平速度低，不锁的话动画被压到 0.4 倍速，"跳起翻转劈砍"被慢放成"奔跑翻转再攻击" |
| `TransitionTo` 被拒只记日志不报错 | 很多"看起来没生效"的问题根源就在这。**排障第一步：打开 State 通道看有没有"切换被拒"** |

【对照】你 KB 里 Q48.5 那题讲的就是这个：**分层动画里"状态机状态"与"画面显示"是两件事**，四层递进排查 = 状态真切了吗 → 该层权重多少 → 参数写进去了吗 → clip 有事件吗。

### 11.6 阶段 5：动画事件 → 判定框

```
动画时间轴上的 Activate 事件（挂在 clip 上）
   │  事件只能调用 Animator 所在物体（Visual）上组件的方法
   ▼
HitboxEventRelay.Activate()
   ├ 若 attackData.hitboxSize == 0 → 告警一次（配置错误，攻击会静默失效）
   ├ if (target == null || target.attackData == null || target.owner == null) return;
   ├ float dir = target.owner.isFacingRight ? 1f : -1f;
   ├ target.transform.localPosition = new Vector3(hitboxOffset.x * dir, hitboxOffset.y, 0);
   ├ if (target.GetComponent<Collider2D>() is BoxCollider2D box) box.size = hitboxSize;
   └ target.Activate();
```

```csharp
// Hitbox.Activate()
public void Activate()
{
    IsActive = true;
    if (owner != null) owner.hitboxActivatedThisAttack = true;   // 连段守卫
    if (hitCollider != null)
    {
        hitCollider.enabled = false;
        hitCollider.enabled = true;      // 强制 off→on 边沿
    }
}
```

**为什么这么写**

| 点 | 原因 |
|---|---|
| **为什么要 `HitboxEventRelay` 这层转发** | Animation Event 只能调 Animator 所在物体上的方法。但判定框必须挂在武器附近才能跟随挥动 → 中间加一层转发器 |
| **`collider.enabled = false; = true;` 不能删** | Unity 的 `OnTriggerEnter2D` 是**边沿触发**。碰撞体一直开着，第二次攻击不会再次触发。必须 off→on 造一次新的进入沿 |
| **判定框位置/大小由代码按 `AttackData` 摆** | 而不是在场景里手工摆。这样"数据即判定"，配合 `HitboxSceneSync` 工具实现"Scene 里拖 → 写回资产"的可视化编辑 |
| **判定时机来自动画事件，不来自 `AttackData`** | 动画事件是**唯一权威时间源**。`AttackData` 里的 `startup/active/recovery` 只是"帧数表显示的副本"，靠 `AttackDataEventSync` 工具从事件反向回写 |
| `owner.hitboxActivatedThisAttack = true` | 连段推进的条件之一。没这个标记，玩家按太快就能取消掉还没出判定的攻击 |

### 11.7 阶段 6：命中检测（物理引擎回调）

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (!IsActive) return;                                   // 判定框没开 → 不算

    Hurtbox hurtbox = other.GetComponent<Hurtbox>();
    if (hurtbox == null || hurtbox.owner == owner) return;    // 防自伤

    hurtbox.owner.ApplyDamage(attackData, owner);             // 走统一伤害入口
    // ... 打击感三件套见 11.11
}
```

**为什么这么写**

- `Hurtbox` 是**纯标记组件**，只持有 `owner` 引用，没有任何逻辑。所有判定逻辑都在 `Hitbox` 侧 —— 职责单一，改一处不用改两处。
- 用 `hurtbox.owner != owner` 而不是比 `gameObject`：因为 `Hurtbox` 可能挂在子物体（不同部位），必须比"属于哪个角色"。
- **走统一的 `ApplyDamage` 入口**：不管是近战命中、投射物命中、还是投掷，全部收敛到同一个方法。护盾/无敌/盾反逻辑只写一遍。

### 11.8 阶段 7：伤害结算（`FighterController.ApplyDamage`）

```csharp
public void ApplyDamage(AttackData attack, FighterController attacker)
{
    if (isInvincible || StateMachine.CurrentState == FighterState.Dead) return;

    // ===== 举盾：伤害全由护盾承受，不掉血不击飞 =====
    if (isShielding)
    {
        if (Time.frameCount - shieldStartFrame <= parryWindowFrames)   // 10 帧窗口
        { PerformParry(attacker); return; }                            // 无伤、不掉盾

        currentShieldHP -= DamageSystem.CalculateShieldDamage(attack, currentShieldHP);
        if (currentShieldHP <= 0f) BreakShield();
        return;                                                        // 关键：直接返回
    }

    // ===== 正常结算 =====
    float finalDamage = attack.damage * GameManager.Instance.gameSettings.damageRatio;
    CurrentDamage += finalDamage;
    OnDamaged?.Invoke(finalDamage, attacker);                          // 广播给 UI

    float knockSpeed = DamageSystem.CalculateKnockbackVelocity(attack, CurrentDamage, fighterData.weight);
    Vector2 knockDir = DamageSystem.CalculateKnockbackDirection(attack, transform.position - attacker.transform.position);
    ApplyKnockback(knockDir, knockSpeed);
}
```

击飞速度公式（`DamageSystem`，纯静态无状态）：

```
knockSpeed = (baseKB + bonusKB) × weightFactor × 1.4 + 18
             bonusKB      = dmg×0.1 + dmg×knockbackGrowth×0.05
             weightFactor = 100 / max(1, 体重)
knockSpeed ×= knockbackGrowth / 100
```

**三个输入**：攻击数据（base/growth）、目标当前伤害%、目标体重。

**为什么这么写**

| 点 | 原因 |
|---|---|
| **盾反用帧号不用秒** | `Time.frameCount - shieldStartFrame`。帧号不受 `timeScale`/Hitstop 影响，格斗游戏的帧窗口必须用帧 |
| **盾防分支必须 `return`** | 不 return 会继续往下走"累加伤害 + 击飞"，盾就白举了 |
| **`DamageSystem` 做成静态纯函数类** | 给定输入必定相同输出，方便单独验证数值；也避免"伤害逻辑散落在各处" |
| `OnDamaged` 用事件广播而不是直接调 MatchManager | `FighterController` 不认识 `MatchManager`。谁想听谁订阅 —— 这是跨层解耦的核心 |
| 击飞方向用 `transform.position - attacker.transform.position` | 水平方向由"攻击者在哪边"决定，配合 `knockbackAngle` 的 cos/sin 分解 |

### 11.9 阶段 8：击飞应用（`ApplyKnockback`）

```csharp
public void ApplyKnockback(Vector2 direction, float speed, float hitstunOverride = -1f)
{
    knockbackToken++;                        // 令牌：让上一次的硬直协程作废
    CurrentKnockbackSpeed = speed;
    knockbackVelocity = direction.normalized * speed;
    isInKnockback = true;
    hasLeftGroundInKnockback = false;

    float hitstunDuration = hitstunOverride >= 0f ? hitstunOverride
                                                  : DamageSystem.CalculateHitstun(speed);
    StateMachine.TransitionTo(FighterState.Knockback);
    StartCoroutine(EndHitstunAfter(hitstunDuration));
}
```

硬直曲线（`CalculateHitstun`）：速度 10 → 0.15s，速度 70+ → 0.833s，中间 `InverseLerp` 线性映射。

**为什么这么写**

| 点 | 原因 |
|---|---|
| **`knockbackToken++`** | 协程是"启动后不管"的。连续被击飞时，上一次的 `EndHitstunAfter` 还在等 —— 没有令牌它到点就把硬直提前结束了。令牌 = 版本号，旧版本作废 |
| `hasLeftGroundInKnockback = false` | 受身窗口的判据。必须"真离过地"才允许受身，否则贴地击飞会立刻触发受身 |
| **硬直上限对齐动画时长** | 0.833s 正好是 `Knockback.anim` 的长度。**换动画必须同步这个值**，否则出现"硬直结束但动画还在播" |
| `hitstunOverride` 参数 | 投掷等特殊场景需要手动指定硬直，不走公式 |

### 11.10 阶段 9：物理表现（`FixedUpdate`，固定步长）

```csharp
private void FixedUpdate()
{
    if (StateMachine.CurrentState == FighterState.Dead) return;

    if (isInKnockback)
    {
        if (!IsGrounded) hasLeftGroundInKnockback = true;   // 标记"真离地"
        rb.velocity = knockbackVelocity;
        knockbackVelocity.x *= 0.98f;                       // 空气阻力
        knockbackVelocity.y *= 0.98f;
    }
    else
    {
        rb.velocity = velocity;
    }
}
```

**为什么这么写**

- **物理赋值只在 `FixedUpdate`**。固定步长保证物理稳定；放 `Update` 会因帧率波动导致抖动、穿墙。
- 决策（`velocity.x = MoveInput.x * speed`）在 `Update`，赋值在 `FixedUpdate` —— **决策与执行分离**。
- `hasLeftGroundInKnockback` 在物理步里标记，因为它依赖 `IsGrounded`（`Update` 里算的），而击飞期间 `UpdateGravity` 会提前 return。

### 11.11 阶段 10：打击感三件套

```csharp
// 接在 ApplyDamage 之后（同一帧）
Camera cam = Camera.main;
if (cam != null && cam.TryGetComponent<CameraManager>(out var camMgr))
    camMgr.Shake(0.3f, 0.15f);                                        // ① 震屏

if (attackData.hitEffectPrefab != null)                               // ② 特效
{
    GameObject fx = Instantiate(attackData.hitEffectPrefab, other.transform.position, Quaternion.identity);
    Destroy(fx, 0.1f);
}

OnHit?.Invoke(hurtbox.owner);

if (GameManager.Instance.gameSettings.hitstopScale > 0f)              // ③ 卡帧
    StartCoroutine(HitstopRoutine());

// ---
private System.Collections.IEnumerator HitstopRoutine()
{
    Time.timeScale = 0f;
    float duration = attackData.hitstopDuration * GameManager.Instance.gameSettings.hitstopScale;
    yield return new WaitForSecondsRealtime(duration);   // 必须 Realtime
    Time.timeScale = 1f;
}
```

**为什么这么写**

| 点 | 原因 |
|---|---|
| 震屏用 `unscaledDeltaTime` 衰减 | Hitstop 把 `timeScale` 冻结时，用 `deltaTime` 的震屏会卡住不动 |
| **Hitstop 用 `WaitForSecondsRealtime`** | `timeScale = 0` 时 `WaitForSeconds` 永远等不到（它按缩放时间计时）→ 游戏永久卡死 |
| `hitstopScale` 挂在 `GameSettings` | 全局手感参数，0 = 关闭。方便整体调手感或做"无卡帧模式" |
| 用 `TryGetComponent` 而不是 `GetComponent` | 避免 `Camera.main` 上没有 `CameraManager` 时产生 GC（`GetComponent` 失败也会分配） |

### 11.12 阶段 11：UI 刷新（事件订阅方，与上面并行发生）

```csharp
// MatchManager.OnFighterDamagedHandler
private void OnFighterDamagedHandler(FighterController victim, float damage, FighterController attacker)
{
    int id = victim.playerID;                                     // 用玩家 ID 当数组下标
    if (id < 0 || id >= _damageDisplays.Length || _damageDisplays[id] == null) return;
    _damageDisplays[id].SetPercent(Mathf.RoundToInt(victim.CurrentDamage));
}
```

**为什么这么写**

| 点 | 原因 |
|---|---|
| 订阅时用 `Dictionary` 缓存委托 | 见 11.1 的说明。退订必须精确移除同一个委托实例 |
| 用 `playerID` 当数组下标 | 不用"列表下标"。`ActivePlayers` 的填充顺序 = `Start()` 注册顺序，场景层级一变就错位。`playerID` 是预制体上的固定值，永远可靠 |
| HUD 靠 `transform.Find("P1_HUD/DamageDisplay")` 找 | **约定式路径**。所以 Prefab 层级命名不能随便改 |
| UI 层不做业务判断 | `DamageDisplay.SetPercent` 只负责显示，百分比由 `MatchManager` 算好传进来 |

### 11.13 阶段 12：硬直结束（协程）

```csharp
private System.Collections.IEnumerator EndHitstunAfter(float duration)
{
    int token = knockbackToken;
    yield return new WaitForSeconds(duration);

    if (token != knockbackToken) yield break;     // 期间又被击飞 → 本次作废
    if (!isInKnockback) yield break;

    if (!IsGrounded)
    {
        velocity = knockbackVelocity;             // 惯性交接：击飞速度转成正常速度
        knockbackVelocity = Vector2.zero;
        isInKnockback = false;
        remainingJumps = Mathf.Max(1, fighterData.jumpCount - 1);   // 大乱斗规则：必给一跳回场
        StateMachine.TransitionTo(FighterState.Fall);
    }
    else
    {
        knockbackVelocity = Vector2.zero;
        isInKnockback = false;
        StateMachine.TransitionTo(FighterState.Idle);
    }
}
```

**为什么这么写**

| 点 | 原因 |
|---|---|
| `remainingJumps = Max(1, jumpCount - 1)` | 大乱斗规则：硬直结束必给一跳回场。**不能给 `jumpCount`** —— 因为"第一跳"要求贴地或土狼时间内，空中给满次数反而跳不起来（会被 `TryPerformJump` 的 `groundOk` 卡死） |
| 空中和地面分开处理 | 空中要"惯性交接"（继续飞）；贴地击飞直接回 `Idle`（没有被推离地面） |
| 令牌校验放在最前 | 令牌变了说明这次硬直已经被新的击飞接管，直接退出 |

### 11.14 阶段 13：出界 → 扣命 → 重生

```csharp
// BlastZone.OnTriggerEnter2D → MatchManager
public void OnPlayerOutOfBounds(FighterController fighter, BlastZone.Side side)
{
    if (fighter.StateMachine.CurrentState == FighterState.Dead) return;
    fighter.Kill();                                                    // 扣命 + 广播 OnKilled
    if (settings.matchMode == GameSettings.MatchMode.Stock && fighter.remainingStocks > 0)
        StartCoroutine(RespawnAfterDelay(fighter));
}

// FighterController.Kill()
public void Kill()
{
    if (StateMachine.CurrentState == FighterState.Dead) return;
    if (_isKilling) return; _isKilling = true;                         // 防重入
    remainingStocks = Mathf.Max(0, remainingStocks - 1);
    StateMachine.TransitionTo(FighterState.Dead);
    OnKilled?.Invoke(this);
    _isKilling = false;
}

// RespawnAfterDelay(3s)
重新注册 GameManager（_registeredFighters 去重）
RebindFighterEvents(fighter)              // 先退订旧委托再订阅新的
fighter.Respawn(随机出生点)
    位置复位 / 伤害归零 / 清速度 / 无敌 respawnTime / 满盾 / 满跳
    StateMachine.Initialize(FighterState.Idle);      // 必须 Initialize
    恢复 visual / mainCollider / hurtbox
同步 Stock UI + Damage UI 归零
```

**为什么这么写**

| 点 | 原因 |
|---|---|
| `_isKilling` 防重入 | 角色可能同时碰到两个 `BlastZone`（角落），不加守卫会扣两次命 |
| `BlastZone._triggered` + 0.5s 重置 | 同上，避免同一帧多次触发 |
| **重生用 `Initialize` 不用 `TransitionTo`** | `Dead` 状态在转换规则里被限制为"只能切到 `Idle`"，但 `Initialize` 直接赋值更干脆，绕开规则表 |
| 重生后重新注册 + 重绑事件 | `EndMatch` 时把所有 Fighter 从 `ActivePlayers` 移除了；重生必须加回来，否则比赛判定会漏掉这个玩家 |
| 用 `_registeredFighters`（HashSet）去重 | 防止重复注册导致 `ActivePlayers` 里出现同一个角色两份 |

### 11.15 完整调用时序图（一条链路全貌）

```
[玩家按 J]
   │
   ▼ InputAction.started
InputManager.OnAttackCtx()                    ← 只写 3 个字段
   │
   ▼ 同一帧或下一帧
InputManager.Update()
   ├ 4 分支决策 → GetImmediateAttack() → jab1
   ▼
FighterController.TryAttack(jab1)
   ├ 广播 attackData 给所有 Hitbox
   ├ animator.SetInteger("AttackType", 0)
   └ StateMachine.TransitionTo(Attack)
   │
   ▼ 下一帧
FighterController.UpdateAnimation()
   ├ animator.SetInteger("State", 4)
   └ SetLayerWeight(1, 1f)                    ← Action Layer 点亮
   │
   ▼ Animator 播 Attack_Jab1
[动画播到判定帧]
   │
   ▼ Animation Event: Activate
HitboxEventRelay.Activate()
   ├ 按 attackData 摆位置/大小
   └ Hitbox.Activate()                        ← collider off→on
   │
   ▼ 物理引擎
Hitbox.OnTriggerEnter2D(hurtboxCollider)
   ├ 排除自己
   ▼
FighterController.ApplyDamage(attackData, attacker)
   ├ 无敌/死亡 → return
   ├ isShielding → 盾反/扣盾/破盾 → return
   ├ CurrentDamage += damage × damageRatio
   ├ OnDamaged?.Invoke()  ────────────────────────┐
   ├ DamageSystem 算 knockSpeed / knockDir        │
   └ ApplyKnockback()                             │
        ├ knockbackToken++                        │
        ├ StateMachine.TransitionTo(Knockback)    │
        └ StartCoroutine(EndHitstunAfter)         │
   │                                              │
   ▼ 同帧继续（回到 Hitbox）                       │
CameraManager.Shake() / Instantiate(特效) / Hitstop │
   │                                              │
   ▼ FixedUpdate                                  ▼ 事件订阅方
rb.velocity = knockbackVelocity × 0.98        MatchManager.OnFighterDamagedHandler
   │                                              └ DamageDisplay.SetPercent()
   ▼ 硬直时间到
EndHitstunAfter()  →  Fall（空中）/ Idle（地面）
   │
   ▼ 若飞出边界
BlastZone → MatchManager.OnPlayerOutOfBounds → Kill() → RespawnAfterDelay → Respawn()
```

### 11.16 本链路涉及的跨模块接口清单（改代码前先看这张表）

| 接口 | 定义处 | 调用方 | 改动风险 |
|---|---|---|---|
| `TryAttack / TryJump / TryGrab / SetShielding / StartCharge / ReleaseCharge / TrySpecial / DropThroughPlatform` | `FighterController` | `InputManager` | 改签名要同步 `InputManager.Update` |
| `ApplyDamage` | `FighterController` | `Hitbox`、`Projectile` | 所有伤害来源都走它，改要全回归 |
| `ApplyKnockback` | `FighterController` | 自身、`PerformThrow` | — |
| `OnDamaged / OnKilled` | `FighterController` | `MatchManager` 订阅 | 改签名要同步订阅方与 `Dictionary` 缓存类型 |
| `Activate / Deactivate / AttackFinished` | `Hitbox` | `HitboxEventRelay`（动画事件） | **动画 clip 上的事件名不能改**，改了要同步 clip |
| `CalculateKnockbackVelocity / Direction / Hitstun / ShieldDamage` | `DamageSystem` | `FighterController`、`FrameMeter` | 纯函数，改要重测手感 |
| `Spawn / Despawn` | `ObjectPooler` | `Projectile` | — |
| `OnPlayerOutOfBounds` | `MatchManager` | `BlastZone` | — |
| `SetPercent / SetStock / SyncTime / Init` | `DamageDisplay` / `StockDisplay` | `MatchManager` | — |

---

## 十二、代码编写原理与设计意图（为什么这么写）

> 第十一章讲"代码怎么跑"，本章讲"**为什么这么写**"。
> 这一章是**可迁移的**：换任何 Unity 项目，这些原理都成立。

### 12.1 主循环纪律：三种 Update 各有其职

```
Update       每渲染帧      做"决策"：读输入、判状态、开计时器、改 velocity 变量
FixedUpdate  固定步长      做"执行"：把 velocity 写进 rb.velocity
LateUpdate   全部 Update 后 做"跟随"：相机、帧数表（依赖别人算完）
```

**违反会怎样**

| 错误写法 | 后果 |
|---|---|
| 在 `Update` 里 `rb.velocity = v` | 物理步长与渲染帧率不匹配 → 抖动、穿透 |
| 在 `Update` 里算相机位置 | 角色还没移动完就拍照 → 镜头抖 |
| 在 `FixedUpdate` 里读 `Input` | 输入按帧采样，物理帧可能一帧收不到、下一帧收到两次 → 丢输入 |

【对照】你在 Raylib 里的主循环是 `PollInput → Update → Draw`，一个循环干完。
Unity 把它拆成三个回调，是因为**物理和渲染是解耦的两个时钟**。这是引擎层面的取舍，不是冗余。

### 12.2 决策与执行分离

本项目到处是这个模式：

```csharp
// 决策（Update）—— 只改"意图变量"
velocity.x = MoveInput.x * speed;
isRunning = true;
isShielding = true;

// 执行（FixedUpdate）—— 只做物理赋值
rb.velocity = velocity;
```

**为什么**：决策可以每帧重算（廉价），执行只做一次（有代价）。
另外，决策代码容易测试（纯变量运算），执行代码依赖引擎状态。

### 12.3 事件驱动 vs 轮询：怎么选

| 场景 | 选什么 | 本项目例子 |
|---|---|---|
| 一对多、接收方可能不存在、跨层 | **事件** | `OnDamaged` / `OnKilled` / `OnGameStateChanged` |
| 一对一、确定存在、同层 | **直接引用调用** | `Hitbox → ApplyDamage`、`InputManager → TryAttack` |
| 需要每帧检查状态 | **轮询** | `IsGrounded` 检测、`UpdateTimers` |

**判断口诀**：**"我不认识你，但你可能想听我说话" → 用事件。**

`FighterController` 不认识 `MatchManager`，但它受伤时有人可能想更新 UI → 广播 `OnDamaged`。
`Hitbox` 认识 `FighterController`（同一次攻击的上下文），直接调用即可。

【注意】事件的代价：**订阅/退订必须严格对称**。退订不掉就是内存泄漏 + 重复触发（见 12.13）。

### 12.4 引用类型陷阱：`AttackData` 是 class

```csharp
// AttackData 定义在 FighterData.cs
[System.Serializable]
public class AttackData { public float damage; ... }     // class！引用类型
```

这意味着：

```csharp
// 【错误】直接改 = 永久污染 ScriptableObject 资产
chargeBaseData.damage *= 1.25f;
// 下次进游戏，这个招式的伤害就是改过的值了

// 【正确】先深拷贝
AttackData final = CloneAttackData(chargeBaseData);
final.damage = chargeBaseData.damage * Mathf.Lerp(1f, 1.25f, lvl);
```

**通用规则**：ScriptableObject 里持有的**引用类型字段**，运行时想改必须先克隆。
`CloneAttackData` 里逐字段拷贝是"手写深拷贝"——**新增 `AttackData` 字段时这里必须同步补充**，否则克隆会丢字段（静默 bug）。

【对照】C++ 里 `struct` 是值语义、赋值即拷贝；C# 的 `class` 是引用语义，`=` 只是复制指针。
Unity 的 `[System.Serializable]` **不改变**语义，只影响"能不能在 Inspector 里展开"。

### 12.5 边沿触发陷阱：`OnTriggerEnter2D`

```csharp
// 【必须的写法】
hitCollider.enabled = false;
hitCollider.enabled = true;      // 造一次新的 on→off→on 边沿
```

**为什么**：`OnTriggerEnter2D` 是**进入沿**触发，不是"每帧检测重叠"。
碰撞体一直开着，第二次攻击不会再次触发 —— 因为"已经在里面了"，没有新的"进入"。

**同类陷阱**：`InputAction` 的 `started` / `performed` 也是边沿；`Update` 里读 `GetKeyDown` 是边沿、`GetKey` 是电平。**边沿只来一次，电平每帧都有**。

### 12.6 令牌机制：协程竞态的解药

```csharp
knockbackToken++;                                  // 启动时：版本号 +1
StartCoroutine(EndHitstunAfter(hitstun));

IEnumerator EndHitstunAfter(float d)
{
    int token = knockbackToken;                    // 记下启动时的版本号
    yield return new WaitForSeconds(d);
    if (token != knockbackToken) yield break;      // 版本变了 → 我被顶替了 → 退出
    ...
}
```

**为什么需要**：协程是"启动后不管"的。连续被击飞两次，第一次的协程还在等 —— 它到点就会把硬直提前结束。
令牌 = 版本号，让旧协程自杀。

**同类场景**：任何"延时任务可能被新事件取代"的地方都要这个模式。本项目里 `_isKilling`、`_isMatchEnding`、`_triggered`、`_matchStarted` 都是同族（防重入/防重复）。

### 12.7 协程 vs 计时器 vs `Invoke`

| 手段 | 本项目用在哪 | 选它的理由 |
|---|---|---|
| **协程** | 硬直结束、投掷后恢复碰撞、延迟重生、闪白、Hitstop | 需要"等待 + 中间做几件事"或需要参数 |
| **计时器字段** | 攻击兜底、无敌、护盾、抓取超时、眩晕、蓄力 | 需要每帧可读、可被外部打断/查询 |
| **`Invoke`** | 只有 `TestEndMatch` 用 | **不推荐**：无法取消、无法传参、方法名是字符串（重构不安全） |

【重点】协程里等时间要区分 `WaitForSeconds` 和 `WaitForSecondsRealtime`：
- 普通延时 → `WaitForSeconds`
- **Hitstop / 暂停期间也要走完的** → `WaitForSecondsRealtime`（否则 `timeScale = 0` 时永远等不到，游戏卡死）

### 12.8 状态机设计：集中式转换规则表

本项目把转换规则**集中在一个方法**里：

```csharp
public bool CanTransitionTo(FighterState target)
{
    if (CurrentState == Dead && target != Idle) return false;
    if (CurrentState == Attack && target == Attack) return true;      // 连段
    if (CurrentState == Knockback) return target is Fall or Dead or Idle or Hit;
    if (CurrentState is Hit or Stun) return target is Idle or Fall or Knockback or Dead;
    if (CurrentState == Shield) return target is Idle or ShieldStun or Grab or Stun or Grabbed;
    return true;
}
```

**为什么集中**：状态转换是"规则"，规则散落各处就无法推理。
所有"为什么这个状态切不过去"的问题，只需要看这一个方法。

**代价**：状态多了以后这个方法会变长。到 20+ 状态时应该改成"转换表数据结构 + 查表"。

【重点】被拒绝时**只记日志不报错**（`SmashDebug` 的 State 通道）。因为"切换被拒"在正常游戏里是常态（比如击飞中想攻击）。但排障时这是第一现场。

### 12.9 对象池：为什么投射物必须用

```csharp
ObjectPooler.Instance.Spawn(prefab, pos, rot);       // 取
ObjectPooler.Instance.Despawn(obj, delay);           // 还（只是 SetActive(false)）
```

**为什么**：Unity 的 `Instantiate` / `Destroy` 会触发 GC。特殊攻击是高频操作，每次都 new 会周期性掉帧。

**本项目的实现细节**：
- 池以**预制体名**为 key（所以不同预制体不能重名）
- `Spawn` 后对象**仍在队列里**，靠 `SetActive` 控制可见性
- 池空时自动扩容

【对照】这就是你 C++ 里写过的内存池 / 对象池。区别是 C# 有 GC，Unity 的 `Instantiate` 走的是"序列化反序列化重建对象图"，比 `new` 还贵。

### 12.10 ScriptableObject 数据驱动

```
FighterData (SO)  ──  一个角色的所有数值
GameSettings (SO) ──  全局规则
DebugSettings (SO) ──  调试开关
```

**为什么用 SO 而不是硬编码/JSON**：

| 优势 | 说明 |
|---|---|
| 策划/自己可在 Inspector 调 | 不用改代码、不用重编译 |
| 能被场景/预制体引用 | `FighterController.fighterData` 直接拖 |
| 天然支持"多套配置" | 复制一份资产就是新角色/新规则 |
| 运行时只读 | 想改数值必须先克隆（见 12.4） |

**注意 SO 的坑**：`SetSelection/GetSelection` 这种"运行时状态"存在 SO 里，靠的是"SO 是资产，跨场景不销毁"。这是**借用了资产的生命周期**，不是 SO 的设计用途 —— 能work，但要知道自己在做什么。

### 12.11 防御性编程四原则

| 原则 | 本项目体现 | 违反的后果 |
|---|---|---|
| **引用兜底** | `Awake` 里所有组件引用都 `if (x == null) x = GetComponent<...>()` | 漏拖一个就 NRE，且**异常冒泡会让上层逻辑错乱**（历史：`mainCollider` 空 → 抓取变击退） |
| **对称清理** | `ReleaseGrab()` 同时处理"我是抓取方"和"我是被抓方" | 单向清理 → 幽灵引用、状态残留（隐形盾） |
| **幂等** | `ReleaseCharge()` 开头 `if (!isCharging) return;` | 重复调用出错（被打断后松手误触发） |
| **防重入** | `_isKilling` / `_isMatchEnding` / `_triggered` / `_matchStarted` | 角落同时碰两个 BlastZone → 扣两次命 |

【重点】**"只在 `if (x != null)` 里出现过的字段"是"可能没拖"的强信号**。看到这种写法就要警觉。

### 12.12 Inspector 参数暴露原则

本项目把大量数值挂成 `public` 字段（`gravityScale`、`parryWindowFrames`、`techBounceSpeed`...）。

**什么时候该暴露**：
- 手感调参（重力、跳跃力、硬直时长、震屏强度）
- 环境相关（`groundLayer`、`spawnPoints`、`hudPrefab`）

**什么时候不该暴露**：
- 内部状态（`isRunning`、`lastTapTime`）→ 应该是 `private`
- 派生值（`attackTimer` 其实是 `startup+active+recovery` 算出来的）

**本项目的现状**：`FighterController` 有 60+ 个 public 字段，其中一部分是内部状态（应该 private），这是**待还的技术债**（见 12.16）。

### 12.13 委托订阅的对称性

```csharp
// 【错误】这样退订不掉
f.OnDamaged += (dmg, atk) => OnFighterDamagedHandler(f, dmg, atk);
f.OnDamaged -= (dmg, atk) => OnFighterDamagedHandler(f, dmg, atk);   // 这是另一个实例！

// 【正确】缓存起来
System.Action<float, FighterController> dmgHandler = (dmg, atk) => OnFighterDamagedHandler(f, dmg, atk);
f.OnDamaged += dmgHandler;
_damagedHandlers[f] = dmgHandler;
// 退订时
if (_damagedHandlers.TryGetValue(f, out var dmg)) { f.OnDamaged -= dmg; _damagedHandlers.Remove(f); }
```

**为什么**：C# 的委托是**值类型语义的引用** —— 每次 `+=` 一个 lambda 都会创建一个**新对象**。不缓存就退订不掉。

**后果**：内存泄漏（委托持有闭包引用）+ 重复触发（重生后事件被订阅多次 → UI 刷新多次）。

【对照】这相当于 C 里注册回调时，`free` 的时候必须传回**同一个函数指针**。传一个"长得一样的函数"是没用的。

### 12.14 时间源唯一性

本项目明确规定：**动画事件是攻击判定的唯一权威时间源**。

```
Animation Event 时间  ──(AttackDataEventSync 工具)──►  AttackData.startup/active/recovery
                                                              │
                                                              ▼
                                                        FrameMeter 显示
```

**为什么不让 `AttackData` 当权威**：判定框的开关是 `HitboxEventRelay` 的动画事件驱动的，**零依赖 `AttackData`**。所以 `AttackData` 里的三段时长只是"给帧数表看的副本"。两者必须一致，靠工具强制校验（`S+A+R == clip 长度`，不合格拒写）。

**推广**：任何"两份数据描述同一件事"的地方，必须明确**谁是源、谁是副本**，并写工具校验。否则迟早不一致。

### 12.15 命名与组织规范（本次整理后确立）

```
每个 .cs 固定 9 段：
  #region 1. 常量与静态字段
  #region 2. Inspector 配置          （public / [SerializeField]，按 [Header] 分组）
  #region 3. 运行时状态              （public 属性 → private 字段 → 缓存引用）
  #region 4. 事件与委托
  #region 5. Unity 生命周期           （Awake → OnEnable → Start → Update → FixedUpdate → LateUpdate → OnDisable → OnDestroy）
  #region 6. 公开 API                （外部调用入口，按调用方分组）
  #region 7. 核心私有逻辑             （Update 调用的子方法，按调用顺序）
  #region 8. 私有工具                （纯计算 / 无副作用）
  #region 9. 调试可视化              （Gizmos / OnGUI）
```

四条排序原则：

1. **读的顺序 = 跑的顺序**（生命周期在前，被调用的紧随其后）
2. **接口优先**（public 在 private 前）
3. **就近原则**（A 调 B，B 就排在 A 后面，不要隔 500 行）
4. **同类聚拢**（所有协程一起、所有 `Get*` 工具一起）

注释三层：

| 层 | 格式 | 只写什么 |
|---|---|---|
| 类头 | `// 职责 / 架构位置 / 依赖 / 被谁使用 / 【注意】` | 这个类在整个项目里的位置 |
| 方法头 | `// 【做什么】/【参数】/【副作用】/【注意】` | **看代码看不出来的部分** |
| 方法内 | `// ===== 1. 名词短语 =====` | 30 行以上方法必须分段 |

**删除标准**：删掉这条注释，后来的人会不会踩坑？会 → 留；不会 → 删。
所以"描述代码字面意思"的注释（`// 设置速度为 5`）必删，"为什么这么写"的注释（`// 必须用 Realtime，否则 timeScale=0 时卡死`）必留。

### 12.16 这个项目的技术债与改进方向

按"改动收益 / 风险"排序：

| # | 问题 | 影响 | 建议 |
|---|---|---|---|
| 1 | `FighterController` 1236 行上帝类，8 大职责 | 改任何功能都要动它；冲突高；难测试 | 按职责拆：`MovementController` / `AttackController` / `DefenseController` / `GrabController`。**但拆分要一次一个、每次全回归**，别一次拆完 |
| 2 | 60+ 个 public 字段，内部状态也暴露 | Inspector 噪音；容易被外部误改 | 内部状态改 `private` + 只读属性 |
| 3 | `attackTimer` 只写不读 | 误导 | 删掉，或让 `FrameMeter` 真的用它 |
| 4 | 取消窗口三字段（`cancelIntoAttacks` / `canJumpCancel` / `canSpecialCancel`）全是死字段 | 以为实现了其实没有 | 要么实现，要么删 |
| 5 | `StageData` / `GameSettings` 里一批字段配置了但没人读 | 改了没效果，容易误判 | 逐条确认：要么接上，要么标注"未实现" |
| 6 | 硬编码数值：`1.5f`（投掷瞬移）、`0.3f`（抓空回 Idle）、`5f`（抓取超时）、`0.85f`（二段跳） | 调参要改代码 | 提到 `Inspector` 或 `GameSettings` |
| 7 | `StockDisplay.UpdateTimerText` 里 `fontSize` 的 Lerp 写法会无限放大 | 启用计时器时会暴露 | 记下目标字号，用固定值 Lerp |
| 8 | 4 个脚本在全局命名空间（`FrameMeter` / `DamageDisplay` / `StockDisplay` / `VisualRootMotion` / `HitboxSceneSync` / `TestEndMatch` / Editor 工具们） | 不一致；易重名 | 统一归到 `SuperSmashLike.*`（改的时候同步改 Editor 引用） |
| 9 | `MatchManager` 直接管 HUD（`transform.Find` 找子物体） | 层级改名就崩 | 抽一个 `HudBinder`，或改用序列化引用 |

---

*本文件由 Lucy 基于代码实读生成。若代码有更新，请重新核对。*
