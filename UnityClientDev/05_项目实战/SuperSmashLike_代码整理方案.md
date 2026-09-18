# SuperSmashLike 代码整理方案（注释 / Debug / 方法排序）

> 生成日期：2026-09-19
> 依据：`Assets/_Game` 下 37 个自研脚本（5533 行）实读
> 【注意】按项目协作模式，本文只给**方案 + 代码要点 + 验收标准**，具体改动由主人动手。
> 完成后 Lucy 负责 review。

---

## 总则：三项任务的共同前置约定

| 约定 | 说明 |
|---|---|
| 一次只动一个文件 | 改完 → Play 验证 → git commit。绝不多文件同时改 |
| 不改任何逻辑 | 本方案是**纯整理**（注释/开关/顺序）。任何行为变化都要单独提出来做 |
| 提交信息格式 | `整理(FighterController): 注释规范化 + 方法重排` |
| 顺序建议 | 先做第 2 项（Debug）→ 再做第 1 项（注释）→ 最后做第 3 项（重排）。理由：Debug 改造会大面积改动调用点，先做避免注释白写；重排是纯位移，放最后风险最低 |
| 验证基线 | 改前先 Play 一遍记录"当前能做什么"，改后逐项对照 |

---

## 第 1 项：注释规范化

### 1.1 现状分析

**已有的好注释（保留并作为模板）**

`FighterStateMachine.cs` / `DamageSystem.cs` / `Hitbox.cs` 的类头是标准范本：

```csharp
// ============================================================
// FighterStateMachine — 角色状态机
// 职责：
//   1. 管理斗士的所有行为状态
//   2. 定义状态转换规则
//   3. 提供辅助判断方法
// 架构位置：Character 层，被 FighterController 持有和使用
// 关系：FighterController 每帧决定 → 调用 TransitionTo
// ============================================================
```

**问题清单**

| 问题 | 典型位置 | 数量级 |
|---|---|---|
| 方法完全没有注释 | `FighterController.UpdateTimers` / `UpdateGravity` 内部各段、`MatchManager` 的 handler、`CameraManager` 全部方法、`UIFlowController` 全部方法 | 约 60+ 个方法 |
| 大方法无分段，一坨 100+ 行 | `FighterController.Update`(110行) / `UpdateTimers`(130行) / `ApplyDamage`(39行) / `PerformThrow`(58行) / `MatchManager.MatchFlow`(60行) | 8 处 |
| 冗余注释（描述代码字面意思） | `// 设置速度为 5`、`// 原点`、`// 定义变量` | 约 30 处 |
| 历史残留注释（已失效） | `FighterController` 里的 `（原第 169-177 行整段替换）`、`（原第 188-196 行，target 判空之后）` | 5 处 |
| 注释掉的死代码 | `InputManager` 里整块 `//private AttackData GetContextualAttack()`（28 行） | 1 处大块 + 若干小行 |
| 重复注释 | 类头已说明"被 FighterController 调用"，方法上又写一遍 | 约 15 处 |

### 1.2 三层注释标准（统一格式）

#### 第 1 层：类头（所有类必须有，格式统一）

```csharp
// ============================================================
// <类名> — <一句话定位>
// 职责：
//   1. ...
//   2. ...
// 架构位置：<层名>，<谁持有 / 谁创建>
// 依赖：<它依赖谁>
// 被谁使用：<谁调它>
// 【注意】<关键约束 / 易踩坑>
// ============================================================
```

#### 第 2 层：方法头（所有方法必须有，含 private）

统一用中文块注释，**四个可选要素按需写**：

```csharp
// 【做什么】一句话说明这个方法干什么
// 【参数】xxx = 什么含义（有非直觉参数时必写）
// 【副作用】会改哪些状态 / 会触发哪些事件（有副作用时必写）
// 【注意】为什么这么写 / 有什么坑（有坑时必写）
public void TryAttack(AttackData data)
```

【重点】**不要求每个方法都写满 4 项**。只写"看代码看不出来"的部分。
如果一个方法叫 `GetRandomSpawnPoint()`，写一句"从 spawnPoints 随机取一个出生点"就够了，不用凑格式。

#### 第 3 层：方法内分段（超过 30 行的方法必须有）

```csharp
private void UpdateTimers()
{
    // ===== 1. 蓄力计时 =====
    ...

    // ===== 2. 攻击兜底计时（防动画事件链断）=====
    ...

    // ===== 3. 连段推进 =====
    ...

    // ===== 4. 无敌计时 =====
    ...
}
```

分段名用"名词短语"，不要用"第一步/第二步"这种无语义编号。
**编号只是给人看的顺序提示，重点是短语本身能说明这段干什么。**

### 1.3 删除标准（红线）

| 删 | 例 |
|---|---|
| 描述代码字面意思的 | `// 设置速度为 5` → 代码就是 `speed = 5f` |
| 已被代码取代的历史注释 | `//（原第 169-177 行整段替换）` |
| 注释掉的死代码块 | `InputManager` 里 28 行的 `//private GetContextualAttack()` |
| 类头已说、方法上重复的 | 类头写了"被 FighterController 调用"，方法上不再写 |
| 空注释 / 占位注释 | `// TODO` 但没有任何后续说明的 |

| 必留 | 例 |
|---|---|
| 解释"为什么"的 | `// 必须用 WaitForSecondsRealtime，因为 timeScale=0 时普通 Wait 不走` |
| 根因记录 | `// 根因：PlayerInput 的 InputUser 关联会污染整个资产导致绑定解析失败` |
| 防回归警告 | `// 改这里必须同步 DamageSystem.CalculateHitstun 的 maxDur` |
| 数值来源 / 调参意图 | `// 1.2s 覆盖所有攻击动画时长` |
| 大乱斗规则对照 | `// 大乱斗规则：硬直结束必给一跳回场` |

【重点】判断标准：**删掉这条注释，后来的人会不会踩坑？** 会 → 留；不会 → 删。

### 1.4 具体执行清单（按文件）

| 文件 | 要做的事 |
|---|---|
| `FighterController.cs` | ① 类头补"8 大职责"清单 ② 约 25 个方法补方法头 ③ `Update` / `UpdateTimers` / `UpdateGravity` / `ApplyDamage` / `ApplyKnockback` / `PerformThrow` / `TryGrab` / `Respawn` 加分段 ④ 删 5 处历史残留注释 + 2 处描述性注释 |
| `MatchManager.cs` | ① 类头补"事件订阅/退订对称"说明 ② 5 个 handler 补方法头 ③ `MatchFlow` / `SpawnAndBindHUD` / `RespawnAfterDelay` 加分段 ④ 删 Debug 噪音（见第 2 项） |
| `InputManager.cs` | ① 类头保留（写得很好）② 补 `GetImmediateAttack` / `GetTiltAttack` / `GetSmashAttack` / `GetContextualSpecial` 方法头 ③ 删 28 行注释掉的死代码 ④ `Update` 加分段（跳跃/攻击/判定窗/蓄力/特殊/抓取/防御） |
| `CameraManager.cs` | 全部方法（`Shake` / `FlashWhite` / 跟随逻辑）补方法头 + 主循环分段 |
| `UIFlowController.cs` | 全部方法补方法头 |
| `DamageDisplay.cs` / `StockDisplay.cs` | 补方法头 + 说明"由 MatchManager 通过 playerID 索引调用" |
| `GameManager.cs` | 已较规范，只需统一类头格式 |
| `Hitbox.cs` / `Hurtbox.cs` / `DamageSystem.cs` / `FighterStateMachine.cs` | 已达标，只做格式统一（`// ===` 分隔线长度） |
| 其余小脚本 | 类头补齐 + 方法头补齐 |

### 1.5 验收标准

- [ ] 每个 `.cs` 文件都有统一格式的类头（职责 / 架构位置 / 依赖）
- [ ] 每个方法（含 private）都有方法头，且**不出现"凑格式"的空话**
- [ ] 所有超过 30 行的方法都有 `// ===== 名词短语 =====` 分段
- [ ] 全文搜索 `//private` / `// public` 无注释掉的死代码残留
- [ ] 全文搜索 `原第` 无历史行号残留
- [ ] **Play 一遍，行为与改前完全一致**

---

## 第 2 项：Debug 打印可视化开关

### 2.1 现状分析

| 项 | 现状 |
|---|---|
| 打印总数 | 约 86 处 `Debug.Log/LogWarning/LogError`，分布在 20 个脚本 |
| 开关 | **只有 `FrameMeter.debugLog` 一个** bool |
| 热路径无条件打印 | `FighterController.ApplyDamage`（每次命中打 4 条）、`ApplyKnockback`、`UpdateTimers`（每次连段）、`TryGrab` |
| 定时刷屏 | `MatchManager.MatchFlow` 每 60 帧打印全部玩家状态（每秒 3-5 条） |
| 诊断专用打印 | `FighterController` 里 `[受身诊断]` / `[D] 举盾!` / `[D] 命中!` / `[D] 进盾分支` / `[D] Parry触发!`（带 Log0~Log3 编号，是排障临时加的） |
| Editor 工具打印 | `DemoSetupWizard`(12) / `AttackDataEventSync`(8) / `ExtractClips`(2) —— 这些是**工具反馈，应保留无条件** |

**问题**：测试时想只看某个模块的日志做不到；不打日志时又在热路径上白跑字符串插值。

### 2.2 推荐方案：分组开关 SO + 静态门面 + 运行时面板

#### 组件 1：`DebugSettings : ScriptableObject`（新建）

路径建议：`Assets/_Game/Scripts/Data/DebugSettings.cs`
资产路径建议：`Assets/_Game/ScriptableObjects/DS_Debug.asset`

```csharp
[CreateAssetMenu(menuName = "SuperSmashLike/Debug Settings", fileName = "DS_Debug")]
public class DebugSettings : ScriptableObject
{
    [Header("模块开关")]
    public bool combat;      // 命中 / 伤害 / 击飞 / 盾 / 盾反
    public bool state;       // 状态切换
    public bool input;       // 输入决策（选了哪个招、判定窗）
    public bool grab;        // 抓取 / 挣扎 / 投掷
    public bool movement;    // 移动 / 跳跃 / 落地
    public bool match;       // 比赛流程 / 倒计时 / 结算
    public bool ui;          // HUD 刷新
    public bool camera;      // 相机

    [Header("总开关与粒度")]
    public bool master = true;        // 一键全开/全关
    public bool onlyWarnings;         // 只显示 Warning 及以上

    [Header("运行时面板")]
    public bool showOnScreenPanel;    // 游戏内按 F1 呼出的面板是否显示
    public KeyCode toggleKey = KeyCode.F1;
}
```

#### 组件 2：`SmashDebug` 静态门面（新建）

路径建议：`Assets/_Game/Scripts/Core/SmashDebug.cs`

```csharp
public enum DebugChannel { Combat, State, Input, Grab, Movement, Match, UI, Camera }

public static class SmashDebug
{
    public static DebugSettings Settings;

    public static bool IsOn(DebugChannel ch)
    {
        if (Settings == null || !Settings.master) return false;
        return ch switch
        {
            DebugChannel.Combat   => Settings.combat,
            DebugChannel.State    => Settings.state,
            DebugChannel.Input    => Settings.input,
            DebugChannel.Grab     => Settings.grab,
            DebugChannel.Movement => Settings.movement,
            DebugChannel.Match    => Settings.match,
            DebugChannel.UI       => Settings.ui,
            DebugChannel.Camera   => Settings.camera,
            _ => false,
        };
    }

    public static void Log(DebugChannel ch, string msg)
    {
        if (!IsOn(ch)) return;
        Debug.Log($"[{ch}] {msg}");
    }

    public static void LogWarn(DebugChannel ch, string msg)
    {
        if (!IsOn(ch)) return;
        Debug.LogWarning($"[{ch}] {msg}");
    }
}
```

**初始化**：在 `GameManager.Awake()` 里加一行

```csharp
public DebugSettings debugSettings;   // Inspector 拖入

private void Awake()
{
    SmashDebug.Settings = debugSettings;   // 必须在其他 Start 之前
    ...
}
```

#### 组件 3：运行时可视化面板（新建）

路径建议：`Assets/_Game/Scripts/Core/DebugPanel.cs`

```csharp
// 挂到 GameManager 同一个 GameObject 上
public class DebugPanel : MonoBehaviour
{
    [SerializeField] private DebugSettings settings;

    private void Update()
    {
        if (settings == null) return;
        if (Input.GetKeyDown(settings.toggleKey))
            settings.showOnScreenPanel = !settings.showOnScreenPanel;
    }

    private void OnGUI()
    {
        if (settings == null || !settings.showOnScreenPanel) return;
        // 左上角绘制一组 Toggle：master / combat / state / input / grab / ...
        // 直接改 settings 的 bool，改完立刻生效（不需要重启）
    }
}
```

【重点】用 **IMGUI (`OnGUI`)** 而不是 UGUI —— 面板本身不能依赖被调试的 UI 系统（否则 UI 出问题时面板也挂了）。

#### 组件 4：Editor 菜单（可选，推荐）

路径建议：`Assets/_Game/Scripts/Editor/DebugPanelWindow.cs`

```csharp
[MenuItem("Tools/SuperSmashLike/Debug 开关面板")]
public static void Open() { ... }   // EditorWindow，改 DebugSettings 资产
```

好处：编辑期不运行也能改开关，且改的是**资产**（持久化），运行期的 OnGUI 面板改的是**内存值**（Play 结束就还原）。

### 2.3 调用点改造规则

| 原写法 | 新写法 |
|---|---|
| `Debug.Log($"[D] 命中! ...")` | `SmashDebug.Log(DebugChannel.Combat, $"命中! ...")` |
| `Debug.Log($"Countdown: {i}")` | `SmashDebug.Log(DebugChannel.Match, $"倒计时 {i}")` |
| `Debug.LogWarning($"[StockCheck] GAME OVER!")` | `SmashDebug.LogWarn(DebugChannel.Match, ...)` |
| `Debug.LogError("[MatchManager] hudPrefab 未赋值！")` | **保留无条件 `Debug.LogError`** —— 错误必须永远可见 |
| `if (debugLog) Debug.Log($"...")` (FrameMeter) | 改成 `SmashDebug.Log(DebugChannel.UI, ...)`，删掉 `debugLog` 字段（并入统一开关） |
| Editor 工具里的 `Debug.Log` | **保留不动**（工具反馈，不是运行时噪音） |

**分组归属速查**

| 通道 | 收哪些打印 |
|---|---|
| Combat | `ApplyDamage` 的 Log0-3、`ApplyKnockback` 的 `[击飞]`、盾/盾反/破盾、Hitstop |
| State | `FighterStateMachine` 的"切换被拒"、状态异常 |
| Input | InputManager 的初始化日志、输入分组隔离、判定窗决策 |
| Grab | `[Grab] 抓住`、`[Grab] 超时自动松开`、`[Escape] 挣扎`、投掷 |
| Movement | 落地、跳跃、`[受身诊断]`、快速下落 |
| Match | `Countdown`、`Match Started!`、`[StockCheck]`、`[OnPlayerOutOfBounds]`、`[RespawnAfterDelay]` 全系列 |
| UI | `[HUD] P0 Damage →`、`[HUD] P0 Stock →`、FrameMeter |
| Camera | 相机异常（当前无打印，预留给第 1 项新增） |

### 2.4 【重点】性能注意

**当前写法（推荐保持）**

```csharp
if (SmashDebug.IsOn(DebugChannel.Combat))
    SmashDebug.Log(DebugChannel.Combat, $"伤害={dmg}");
```
`if` 包住了整条语句 → 字符串插值不会执行 → 关掉时零成本。

**错误写法（会产生 GC）**

```csharp
SmashDebug.Log(DebugChannel.Combat, $"伤害={dmg}");   // 插值先算了，才发现不用打
```
只有在 `Log()` 内部判断，**参数已经求值完毕** —— 每帧调用的地方会持续产生字符串垃圾。

**进阶（可选）**：给 `SmashDebug.Log` 加 `[Conditional("UNITY_EDITOR")]`，
发布版编译器会把整个调用点删掉（连方法调用都不存在）。但这会让**真机调试也看不到日志**，
建议只在打包前临时加上，不要长期保留。

### 2.5 实施步骤

1. 新建 `DebugSettings.cs` + 创建 `DS_Debug.asset`
2. 新建 `SmashDebug.cs`
3. `GameManager` 加 `debugSettings` 字段 + Awake 里赋 `SmashDebug.Settings`
4. 新建 `DebugPanel.cs`，挂到 GameManager 所在物体
5. **逐文件替换调用点**（一次一个文件）：
   - 先做 `MatchManager.cs`（打印最多、收益最大）
   - 再做 `FighterController.cs`
   - 然后 `InputManager.cs` / `Hitbox.cs` / `FrameMeter.cs`
   - 最后扫一遍剩下的
6. 新建 Editor 菜单窗口（可选）
7. 把"新增 Debug 一律走 SmashDebug + 归到某个 Channel"写进项目 AGENTS.md

### 2.6 验收标准

- [ ] `DS_Debug.asset` 里能按模块勾选，勾上后对应日志出现，取消后**完全消失**
- [ ] 游戏内按 F1 能呼出/收起面板，面板上改开关**立刻生效**
- [ ] 全部关闭时，Console 里只剩 `LogError` 和 Editor 工具输出
- [ ] 全部关闭时 Play 一局，Profiler 里 GC Alloc 无异常增长
- [ ] 新增的 Debug 走 `SmashDebug.Log(Channel, ...)`，不再出现裸 `Debug.Log`

---

## 第 3 项：方法位置调整（排序规范）

### 3.1 现状分析

以 `FighterController.cs`（1236 行）为例，当前顺序是：

```
1-142   Inspector 字段（17 个 [Header]，中间混着 private 字段）
        Configuration / References / Ground Detection / 运行时状态 /
        Jump / Attack / Charge / Combo / Knockback / Feel / Shield /
        Parry / Tech / Grab / Grab State / Respawn / Stock
        （中间夹着 escapeMashCount / grabTimer / grabWhiff / hasLeftGroundInKnockback /
          hitboxActivatedThisAttack / knockbackToken / stunTechable 等私有字段）
143-148 事件声明
149-165 Awake
167-175 Start
177-288 Update（110 行）
290-313 FixedUpdate
315-445 UpdateTimers（130 行）
447-492 UpdateGravity
494-528 UpdateMovement
530-566 UpdateAnimation
568-594 SetShielding          ← 公开方法开始
596-605 SetFacing            ← 却是 private
607-611 SetAttackAnim        ← private
613-632 StartCharge
634-643 TryJump
645-659 TryPerformJump       ← private，夹在两个 public 中间
661-728 TryAttack
730-745 TrySpecial
747-800 TryGrab
802-812 DropThroughPlatform
814-820 IsInComboWindow      ← private
822-828 GetChargeIndex        ← private
830-838 GetComboAttack        ← private
840-861 GetThrowDirection     ← private
863-904 ApplyDamage
906-925 ApplyKnockback
927-955 EndHitstunAfter       ← private 协程
957-969 PerformParry
971-991 PerformTech
993-1051 PerformThrow
1053-1059 RestoreCollisionAfterThrow  ← private 协程
1061-1087 ReleaseGrab         ← private
1089-1113 ReleaseCharge
1115-1119 EnterStun
1121-1128 BreakShield
1130-1158 Respawn
1160-1176 Kill
1178-1191 OnMashGrabEscape
1193-1218 CloneAttackData     ← private 工具
1220-1229 OnDrawGizmosSelected
1231-1234 OnDestroy
```

**问题**：public / private 交错（`SetFacing` / `TryPerformJump` / `EndHitstunAfter` 混在公开方法里）；
工具方法和业务方法混在一起；生命周期方法被 `OnDrawGizmos` 打断；找 `ReleaseGrab` 要翻到 1061 行。

### 3.2 排序规范（9 个 region）

所有脚本统一按这个顺序排：

```csharp
#region 1. 常量与静态字段
#region 2. Inspector 配置（[Header] 分组，只放 public / [SerializeField]）
#region 3. 运行时状态（public 属性 → private 字段 → 缓存引用）
#region 4. 事件与委托
#region 5. Unity 生命周期（Awake → OnEnable → Start → Update → FixedUpdate → LateUpdate → OnDisable → OnDestroy）
#region 6. 公开 API（外部调用入口）
#region 7. 核心私有逻辑（Update 主循环的子方法，按 Update 里的调用顺序排）
#region 8. 私有工具方法（纯计算 / 无副作用，按字母或重要性）
#region 9. 调试可视化（OnDrawGizmos / OnGUI / 调试辅助）
```

**四条排序原则**

1. **读的顺序 = 跑的顺序**：生命周期在最前，被它调用的方法紧随其后
2. **接口优先**：public 在 private 前（外部读者先看能调什么）
3. **就近原则**：A 调用 B，B 就排在 A 后面（不要隔 500 行）
4. **同类聚拢**：所有协程放一起、所有 `Get*` 工具放一起、所有 `Perform*` 放一起

### 3.3 `FighterController` 重排清单（逐方法）

| 目标 region | 方法（按此顺序） |
|---|---|
| 2. Inspector 配置 | `isFacingRight` `fighterData` `playerID` → References → Ground Detection → Jump → Attack → Charge → Combo → Knockback → Feel → Shield → Parry → Tech → Grab → Respawn → Stock |
| 3. 运行时状态 | `StateMachine` `CurrentDamage` `CurrentKnockbackSpeed` `velocity` `MoveInput` `IsGrounded` `lastAnimState` `TechInputHeld` + 所有 private 字段（`jumpBufferTimer` `coyoteTimer` `isRunning` `lastTapTime` `lastTapDirection` `lastTapDir` `shieldStartFrame` `stunTimer` `escapeMashCount` `grabTimer` `grabWhiff` `hasLeftGroundInKnockback` `knockbackToken` `stunTechable` `_isKilling`）+ 枚举 `ThrowDir` |
| 4. 事件 | `OnStateChanged` `OnDamaged` `OnKilled` `OnParrySuccess` |
| 5. 生命周期 | `Awake` → `Start` → `Update` → `FixedUpdate` → `OnDestroy` |
| 6. 公开 API | **按调用方分组，组内按调用频率**：<br>【InputManager 调用】`TryJump` `TryAttack` `TrySpecial` `TryGrab` `SetShielding` `StartCharge` `ReleaseCharge` `DropThroughPlatform`<br>【Hitbox 调用】`ApplyDamage`<br>【其他 Fighter / 系统调用】`ApplyKnockback` `PerformParry` `PerformTech` `PerformThrow` `EnterStun` `BreakShield` `Respawn` `Kill` `OnMashGrabEscape` |
| 7. 核心私有逻辑 | 按 Update 调用顺序：`UpdateTimers` → `UpdateGravity` → `UpdateMovement` → `UpdateAnimation` → 朝向逻辑（可从 Update 抽出成 `UpdateFacing()`）→ `TryPerformJump` → `EndHitstunAfter` → `RestoreCollisionAfterThrow` → `ReleaseGrab` |
| 8. 私有工具 | `SetFacing` `SetAttackAnim` `IsInComboWindow` `GetChargeIndex` `GetComboAttack` `GetThrowDirection` `CloneAttackData` |
| 9. 调试 | `OnDrawGizmosSelected` |

【重点】重排**不改任何一行代码内容**，只移动位置 + 加 `#region`。
如果发现"顺手能改好"的地方，**记下来单独做**，不要混在重排里。

### 3.4 其他文件的重排要点

| 文件 | 要点 |
|---|---|
| `MatchManager.cs` | 生命周期 → 公开 API（`StartMatch` `EndMatch` `RespawnPlayer` `OnPlayerOutOfBounds`）→ 协程（`MatchFlow` `RespawnAfterDelay`）→ 事件 handler（`OnFighterDamagedHandler` `OnFighterKilledHandler` `OnGameStateChangedHandler` `OnTimerUpdatedHandler` `OnGameOverHandler`）→ 订阅管理（`SubscribeFighterEvents` `UnsubscribeFighterEvents` `RebindFighterEvents`）→ HUD（`SpawnAndBindHUD`）→ 工具 |
| `InputManager.cs` | 生命周期 → Action 回调（按 Action 名顺序）→ `Update` → 招式选择工具（`GetImmediateAttack` `GetTiltAttack` `GetSmashAttack` `GetContextualSpecial`） |
| `CameraManager.cs` | 生命周期（含 LateUpdate）→ 公开 API（`Shake` `FlashWhite`）→ 私有跟随逻辑 |
| `Hitbox.cs` | 生命周期 → 动画事件入口（`Activate` `Deactivate` `AttackFinished`）→ 命中检测（`OnTriggerEnter2D`）→ 协程 → 调试 |
| `DamageSystem.cs` | 已按"击飞 → 方向 → 硬直 → 盾伤"排好，只需加 region |
| 小脚本 | 生命周期 → 公开 API → 私有 → 调试，统一即可 |

### 3.5 实施步骤

1. **先做 `FighterStateMachine.cs`**（最小、134 行）→ 验证 region 风格是否顺手
2. 再做 `DamageSystem.cs` / `Hitbox.cs` / `Hurtbox.cs`（中等）
3. 再做 `MatchManager.cs` / `InputManager.cs` / `CameraManager.cs`
4. **最后做 `FighterController.cs`**（最大、风险最高）
5. 每个文件改完立即 Play 验证 + commit

### 3.6 验收标准

- [ ] 每个脚本都有 9 个 `#region`（没有内容的 region 可以省略）
- [ ] public 方法与 private 方法不再交错
- [ ] 生命周期方法连续排列，不被其他方法打断
- [ ] 调用者与被调用者相邻（`Update` 后面紧跟 `Update*` 系列）
- [ ] 每个 region 可以折叠，折叠后从 region 名就能知道里面有什么
- [ ] **Play 一遍，行为与改前完全一致**

---

## 实施总顺序建议

```
第 1 周：第 2 项（Debug 可视化）
   ├─ 建 DebugSettings + SmashDebug + DebugPanel（半天）
   ├─ MatchManager 调用点替换（半天）
   ├─ FighterController 调用点替换（半天）
   └─ 剩余文件 + Editor 菜单 + 写进 AGENTS.md（半天）

第 2 周：第 1 项（注释规范化）
   ├─ 小文件批量处理（FighterStateMachine/DamageSystem/Hitbox/UI 系列）
   ├─ MatchManager / InputManager / CameraManager
   └─ FighterController（最花时间，单独排一天）

第 3 周：第 3 项（方法重排）
   ├─ 小文件 → 中文件
   └─ FighterController（单独排一天，改完立即全量回归）
```

【注意】三项都做完后，**必须跑一次完整回归**：
双人 Play → 移动/跳跃/攻击/连段/蓄力/防御/盾反/抓取/投掷/受身/出界/重生/结算，
逐项对照改前记录的行为基线。

---

*本方案由 Lucy 基于代码实读生成。实施中遇到问题随时来问，改完我来 review。*
