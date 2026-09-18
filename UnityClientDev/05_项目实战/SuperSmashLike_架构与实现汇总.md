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

*本文件由 Lucy 基于代码实读生成。若代码有更新，请重新核对。*
