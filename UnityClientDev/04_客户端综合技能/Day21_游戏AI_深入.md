# Day 21：游戏 AI — GOAP、Utility AI、导航网格进阶与群体模拟

## 0. 为什么需要更高级的 AI？

基础篇介绍了 FSM 和行为树的实现。但商业游戏中的 AI 面临更复杂的挑战：

```
FSM 失效的场景：
- 一个敌人要有 30 种状态（巡逻/追击/攻击/逃跑/呼叫/搜索/掩护...）
- 状态之间两两相连 → 600 条转换 → 不可维护

行为树能缓解，但仍有问题：
- 所有行为写死在树结构中
- 修改行为需要改树结构
- 无法根据环境动态调整

高级 AI 方案：
1. GOAP（目标导向动作规划）— 动态决策
2. Utility AI（效用 AI）— 数值化决策
3. 影响地图 — 空间态势感知
4. 群体模拟 — 千人以上 NPC 移动
```

---

## 1. GOAP（Goal-Oriented Action Planning）

### 核心思想

```
行为树：策划告诉你"什么条件下做什么"
GOAP：AI 自己决定"做什么才能达成目标"

类比：
行为树 = 菜单（C++ switch-case）：如果玩家近 → 攻击，如果血少 → 逃跑
GOAP = 计划（路径搜索）：AI 在动作空间中搜索最优解

GOAP 的 3 个组件：
1. 世界状态（World State）：AI 对环境的知识
   - 玩家在视野内？子弹数量？血量？
2. 动作（Action）：AI 能做的事
   - 每个动作有前提条件（Preconditions）和效果（Effects）
3. 目标（Goal）：AI 想要的最终状态
   - "消灭玩家" = 玩家血量 ≤ 0
```

### GOAP 数据定义

```csharp
// 世界状态（用位掩码或字典）
public class WorldState
{
    public Dictionary<string, bool> states = new();

    public bool HasState(string key) => states.ContainsKey(key) && states[key];
    public void SetState(string key, bool value) => states[key] = value;

    // 克隆（用于搜索过程中的状态预测）
    public WorldState Clone()
    {
        var clone = new WorldState();
        clone.states = new Dictionary<string, bool>(states);
        return clone;
    }

    // 是否满足前提条件
    public bool Satisfies(Dictionary<string, bool> conditions)
    {
        foreach (var kv in conditions)
        {
            if (!states.TryGetValue(kv.Key, out bool value) || value != kv.Value)
                return false;
        }
        return true;
    }
}

// 动作定义
public class GOAPAction
{
    public string name;
    public float cost = 1f;  // 执行成本（A* 搜索用）

    // 前提条件（必须满足才能执行）
    public Dictionary<string, bool> preconditions = new();

    // 效果（执行后的世界状态变化）
    public Dictionary<string, bool> effects = new();

    // 执行动作
    public virtual bool Execute(GOAPAgent agent)
    {
        Debug.Log($"执行动作：{name}");
        return true;  // 成功
    }

    // 检查前提是否满足
    public bool IsAchievable(WorldState state)
        => state.Satisfies(preconditions);

    // 应用效果到世界状态
    public WorldState ApplyEffects(WorldState state)
    {
        WorldState newState = state.Clone();
        foreach (var kv in effects)
            newState.SetState(kv.Key, kv.Value);
        return newState;
    }
}
```

### A* 规划搜索

```csharp
// GOAP 的核心：在动作空间中搜索最优计划
public class GOAPPlanner
{
    // A* 搜索最优动作序列
    public Queue<GOAPAction> Plan(WorldState currentState, WorldState goalState,
                                   List<GOAPAction> availableActions)
    {
        // A* 搜索节点
        class PlanNode
        {
            public PlanNode parent;
            public GOAPAction action;
            public WorldState state;
            public float g;  // 累计成本
            public float h;  // 启发式距离
            public float F => g + h;
        }

        // 优先队列（按 F 值排序）
        List<PlanNode> openList = new();
        HashSet<string> closedSet = new();

        PlanNode start = new PlanNode { state = currentState, g = 0, h = 0 };
        openList.Add(start);

        while (openList.Count > 0)
        {
            // 选 F 值最小的
            PlanNode current = openList[0];
            openList.RemoveAt(0);

            // 检查是否达到目标
            if (stateSatisfiesGoal(current.state, goalState))
            {
                return ReconstructPlan(current);
            }

            string stateKey = SerializeState(current.state);
            if (closedSet.Contains(stateKey)) continue;
            closedSet.Add(stateKey);

            // 扩展可用动作
            foreach (var action in availableActions)
            {
                if (!action.IsAchievable(current.state)) continue;

                // 应用效果
                WorldState newState = action.ApplyEffects(current.state);

                // 检查是否已经探索过
                string newKey = SerializeState(newState);
                if (closedSet.Contains(newKey)) continue;

                // 计算启发式：不满足的目标条件数量
                float h = CountUnsatisfiedGoals(newState, goalState);

                PlanNode node = new PlanNode
                {
                    parent = current,
                    action = action,
                    state = newState,
                    g = current.g + action.cost,
                    h = h
                };

                openList.Add(node);
                openList.Sort((a, b) => a.F.CompareTo(b.F));
            }
        }

        return null;  // 无法规划
    }

    private Queue<GOAPAction> ReconstructPlan(PlanNode node)
    {
        Stack<GOAPAction> plan = new();
        while (node.parent != null)
        {
            plan.Push(node.action);
            node = node.parent;
        }
        return new Queue<GOAPAction>(plan);
    }

    private bool stateSatisfiesGoal(WorldState state, WorldState goal)
    {
        return state.Satisfies(goal.states);
    }

    private float CountUnsatisfiedGoals(WorldState state, WorldState goal)
    {
        int count = 0;
        foreach (var kv in goal.states)
        {
            if (!state.states.TryGetValue(kv.Key, out bool val) || val != kv.Value)
                count++;
        }
        return count;
    }

    private string SerializeState(WorldState state)
    {
        // 把世界状态转为字符串 key（用于 closedSet 去重）
        return string.Join(",", state.states.OrderBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}
```

### GOAP 完整示例

```csharp
public class GOAPEnemy : MonoBehaviour
{
    private GOAPAgent agent;
    private Queue<GOAPAction> currentPlan;

    void Start()
    {
        agent = new GOAPAgent();

        // 定义可用动作
        agent.AddAction(new ShootAction { cost = 5f });
        agent.AddAction(new ReloadAction { cost = 2f });
        agent.AddAction(new MoveToCoverAction { cost = 3f });
        agent.AddAction(new SearchAmmoAction { cost = 4f });
        agent.AddAction(new PatrolAction { cost = 1f });

        // 定义当前状态
        agent.currentState.SetState("hasAmmo", true);
        agent.currentState.SetState("hasTarget", true);
        agent.currentState.SetState("nearCover", false);
        agent.currentState.SetState("hp", 100);

        // 定义目标
        agent.goalState.SetState("hasTarget", false);  // 消灭目标
    }

    void Update()
    {
        // 如果没有计划或计划完成，重新规划
        if (currentPlan == null || currentPlan.Count == 0)
        {
            currentPlan = agent.Plan();
        }

        // 执行当前动作
        if (currentPlan != null && currentPlan.Count > 0)
        {
            GOAPAction nextAction = currentPlan.Peek();
            if (nextAction.Execute(agent))
                currentPlan.Dequeue();
        }
    }
}

// 具体动作定义
public class ShootAction : GOAPAction
{
    public ShootAction()
    {
        name = "射击";
        preconditions["hasAmmo"] = true;
        preconditions["hasTarget"] = true;
        effects["hasTarget"] = false;
        cost = 5f;
    }
}

public class ReloadAction : GOAPAction
{
    public ReloadAction()
    {
        name = "装弹";
        preconditions["nearAmmo"] = true;
        effects["hasAmmo"] = true;
        cost = 2f;
    }
}

public class MoveToCoverAction : GOAPAction
{
    public MoveToCoverAction()
    {
        name = "找掩体";
        preconditions["underFire"] = true;
        effects["nearCover"] = true;
        cost = 3f;
    }
}
```

---

## 2. Utility AI（效用 AI）

### 核心思想

```
GOAP 搜索最优计划，Utility AI 给每个候选行为打分：
- 每个候选行为计算一个"效用值"
- 选效用最高的执行
- 效用值由一系列因素决定（距离、血量、时间...）

Utility AI = 死亡竞赛（谁分数高谁上）
GOAP = 象棋（走一步算几步）
行为树 = 菜谱（按步骤来）

示例——NPC 决定做什么：
┌──────────────┬─────────────┬──────────────┐
│ 候选行为      │ 计算公式     │ 当前分数      │
├──────────────┼─────────────┼──────────────┤
│ 攻击          │ 1 / (距离+1) │ 0.33         │
│ 治疗          │ (1 - hp/100) │ 0.70 ← 最高   │
│ 逃跑          │ 队友阵亡?    │ 0.0          │
│ 巡逻          │ 默认 0.1     │ 0.1          │
└──────────────┴─────────────┴──────────────┘
→ 当前执行"治疗"（因为血量低）
```

### Utility AI 实现

```csharp
public abstract class Consideration
{
    public string name;

    // 计算"这个考虑的得分"（0~1）
    public abstract float GetScore(UnitContext context);

    // 曲线变换（把原始输入映射到合理的效用曲线）
    // 例如：距离越近 → 攻击欲望越强（但不是线性的）
    public static float ApplyCurve(float input, AnimationCurve curve)
    {
        return curve.Evaluate(Mathf.Clamp01(input));
    }

    // 常用的效用曲线
    public static float Linear(float x) => x;
    public static float Inverse(float x) => 1 - x;
    public static float Exponential(float x) => x * x;           // 陡峭增长
    public static float Logistic(float x) => 1 / (1 + Mathf.Exp(-10 * (x - 0.5f)));  // S 曲线
}

// 距离考虑
public class DistanceConsideration : Consideration
{
    public float maxRange = 20f;

    public override float GetScore(UnitContext context)
    {
        float dist = Vector3.Distance(context.self.position, context.target.position);
        float normalized = dist / maxRange;
        // 距离越近分数越高
        return Inverse(Mathf.Clamp01(normalized));
    }
}

// 血量考虑
public class HealthConsideration : Consideration
{
    public override float GetScore(UnitContext context)
    {
        float hpRatio = context.self.hp / context.self.maxHp;
        // 血越少越需要治疗
        return Inverse(Mathf.Clamp01(hpRatio));
    }
}

// 决策（选最高分的）
public class UtilityAIDecision
{
    public string name;
    public List<Consideration> considerations = new();
    public System.Action<UnitContext> action;

    public float Evaluate(UnitContext context)
    {
        float totalScore = 1f;
        foreach (var consider in considerations)
        {
            float score = consider.GetScore(context);
            totalScore *= score;  // 乘法组合（一个 0 分就否决）
        }
        return totalScore;
    }
}

// 单位上下文
public class UnitContext
{
    public Transform self;
    public Transform target;
    public float hp;
    public float maxHp;
    public List<Transform> allies;
    public List<Transform> enemies;
}

// 完整 Utility AI 控制器
public class UtilityAIController : MonoBehaviour
{
    public List<UtilityAIDecision> decisions = new();

    void Start()
    {
        // 配置决策
        decisions.Add(new UtilityAIDecision
        {
            name = "攻击",
            considerations = {
                new DistanceConsideration { maxRange = 15f },
                new AmmoConsideration()
            },
            action = ctx => Attack(ctx.target)
        });

        decisions.Add(new UtilityAIDecision
        {
            name = "治疗",
            considerations = {
                new HealthConsideration()
            },
            action = ctx => Heal(ctx.self)
        });
    }

    void Update()
    {
        var context = BuildContext();

        UtilityAIDecision best = null;
        float bestScore = 0;

        foreach (var decision in decisions)
        {
            float score = decision.Evaluate(context);
            if (score > bestScore)
            {
                bestScore = score;
                best = decision;
            }
        }

        best?.action?.Invoke(context);
    }
}
```

### 各种 AI 对比

| 维度 | FSM | 行为树 | GOAP | Utility AI |
|------|-----|--------|------|------------|
| 可预测性 | ★★★★★ | ★★★★ | ★★★ | ★★ |
| 灵活性 | ★ | ★★★ | ★★★★★ | ★★★★★ |
| 实现复杂度 | ★ | ★★ | ★★★★★ | ★★★ |
| 配置成本 | 低 | 中 | 高 | 中高 |
| 调试难度 | 低 | 中 | 高 | 中 |

---

## 3. NavMesh 高级用法

### NavMeshSurface — 动态烘焙

```csharp
using UnityEngine.AI;

// NavMeshSurface 可以在运行时动态生成 NavMesh
// 适合动态场景（可破坏的墙壁、生成的地形）

public class DynamicNavMesh : MonoBehaviour
{
    public NavMeshSurface surface;

    void Start()
    {
        // 初始化时烘焙
        surface.BuildNavMesh();
    }

    // 场景变化后重新烘焙
    public void RebuildAfterDestruction()
    {
        // 清空旧数据
        surface.RemoveData();

        // 重新烘焙（异步）
        surface.BuildNavMeshAsync();
    }
}
```

### NavMeshLink — 跳跃/爬梯

```csharp
// NavMeshLink 连接两个分离的 NavMesh 区域
// 用于：跳跃、爬梯子、传送门

public class NavMeshLinkExample : MonoBehaviour
{
    public NavMeshAgent agent;

    void Start()
    {
        // 设置 Link 成本
        // 跳跃 Link 成本高（AI 不优先选择）
        // 梯子 Link 成本正常
    }

    public void JumpAcross(NavMeshLinkData link)
    {
        if (agent.isOnOffMeshLink)
        {
            // 在 OffMeshLink 上 → 执行跳跃动画
            StartCoroutine(AnimateJump());
        }
    }

    IEnumerator AnimateJump()
    {
        // 播放跳跃动画
        animator.SetTrigger("Jump");

        // 等待动画完成
        yield return new WaitForSeconds(0.5f);

        // 完成 Link 穿越
        agent.CompleteOffMeshLink();
    }
}
```

### NavMesh 的 Carving（动态避障）

```csharp
// NavMeshObstacle + Carving 可以在运行时修改 NavMesh
// 其他 AI 会绕着障碍物走

public class DynamicObstacle : MonoBehaviour
{
    private NavMeshObstacle obstacle;

    void Start()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        obstacle.carving = true;  // 开启雕刻
        obstacle.carveOnlyStationary = true;  // 静止时才雕刻
        obstacle.carvingTimeToStationary = 0.5f;
    }
}
```

---

## 4. 群体模拟与 Steering Behaviors

### Steering Behaviors

```csharp
// Steering Behaviors（转向行为）：让 AI 看起来像"真的在移动"
// 不是直接设置目标位置，而是根据受力计算移动

public class SteeringAgent : MonoBehaviour
{
    public Vector3 velocity;
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    public float mass = 1f;

    // 最终受力（来自所有 behavior 的合力）
    private Vector3 totalForce;

    void Update()
    {
        // 清空受力
        totalForce = Vector3.zero;

        // 累加各种转向力
        totalForce += Seek(transform.position + Vector3.forward * 10f) * 1f;
        totalForce += AvoidObstacles() * 2f;
        totalForce += SeparateFromNeighbors() * 1.5f;

        // 应用物理
        Vector3 acceleration = totalForce / mass;
        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        transform.position += velocity * Time.deltaTime;
        if (velocity.magnitude > 0.1f)
            transform.forward = velocity.normalized;
    }

    // Seek（追逐）：向目标移动
    private Vector3 Seek(Vector3 target)
    {
        Vector3 desired = (target - transform.position).normalized * maxSpeed;
        return desired - velocity;
    }

    // Flee（逃跑）：远离目标
    private Vector3 Flee(Vector3 threat)
    {
        Vector3 desired = (transform.position - threat).normalized * maxSpeed;
        return desired - velocity;
    }

    // 分离（Separation）：和其他 AI 保持距离
    private Vector3 SeparateFromNeighbors()
    {
        Vector3 force = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(transform.position, 2f);

        foreach (var neighbor in neighbors)
        {
            if (neighbor.gameObject == gameObject) continue;

            Vector3 away = transform.position - neighbor.transform.position;
            float dist = away.magnitude;
            if (dist < 2f && dist > 0.01f)
            {
                // 距离越近排斥力越大
                force += away.normalized / dist;
            }
        }
        return force;
    }

    // 避障
    private Vector3 AvoidObstacles()
    {
        Vector3 force = Vector3.zero;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 3f))
        {
            Vector3 avoidDir = Vector3.Cross(hit.normal, Vector3.up).normalized;
            force = avoidDir * maxForce;
        }
        return force;
    }

    // Pursuit（追捕）：预测目标位置（不是当前）
    private Vector3 Pursuit(Transform target, Vector3 targetVelocity)
    {
        // 预测未来位置
        float lookAhead = Vector3.Distance(transform.position, target.position) / maxSpeed;
        Vector3 futurePos = target.position + targetVelocity * lookAhead;
        return Seek(futurePos);
    }
}
```

### 群组行为（Flocking）

```csharp
// Flocking（群组行为）= Alignment + Cohesion + Separation
// 模拟鸟群/鱼群的群体运动

public class FlockAgent : SteeringAgent
{
    public float neighborRadius = 5f;

    void Update()
    {
        totalForce = Vector3.zero;

        // 三个基本群组力的组合
        totalForce += Alignment() * 1f;      // 对齐：和邻居方向一致
        totalForce += Cohesion() * 1f;       // 聚合：向邻居中心靠拢
        totalForce += SeparateFromNeighbors() * 2f;  // 分离：别撞太紧

        // 边界约束
        totalForce += StayInBounds() * 3f;

        ApplySteering(totalForce);
    }

    // Alignment（对齐）
    private Vector3 Alignment()
    {
        Vector3 averageVelocity = Vector3.zero;
        int count = 0;

        Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborRadius);
        foreach (var n in neighbors)
        {
            var agent = n.GetComponent<FlockAgent>();
            if (agent != null && agent != this)
            {
                averageVelocity += agent.velocity;
                count++;
            }
        }

        if (count > 0)
        {
            averageVelocity /= count;
            return (averageVelocity - velocity).normalized * maxForce;
        }
        return Vector3.zero;
    }

    // Cohesion（聚合）
    private Vector3 Cohesion()
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborRadius);
        foreach (var n in neighbors)
        {
            var agent = n.GetComponent<FlockAgent>();
            if (agent != null && agent != this)
            {
                center += agent.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            center /= count;
            return Seek(center);
        }
        return Vector3.zero;
    }

    private Vector3 StayInBounds()
    {
        const float BOUNDS = 20f;
        Vector3 pos = transform.position;

        if (Mathf.Abs(pos.x) > BOUNDS) return Seek(Vector3.zero);
        if (Mathf.Abs(pos.z) > BOUNDS) return Seek(Vector3.zero);

        return Vector3.zero;
    }
}
```

---

## 5. 影响地图（Influence Maps）

### 核心概念

```
影响地图是 AI 的"态势感知"工具：
每个位置有一个影响力值，AI 根据数值做决策。

例如：
- 玩家走过的地方 → 温度升高（追踪）
- 队友阵亡的地方 → 危险值上升（回避）
- 资源点 → 吸引力增强（采集）
- 炮火覆盖区域 → 威胁值爆表（快跑）

影响地图 = 游戏世界的"热力图"
```

```csharp
public class InfluenceMap
{
    // 影响地图 = 二维网格
    public struct Cell
    {
        public float threat;       // 威胁值
        public float attraction;   // 吸引力
        public float lastVisitTime;  // 最后访问时间
    }

    private Cell[,] grid;
    private int width, height;
    private float cellSize;
    private Vector3 origin;

    public InfluenceMap(int width, int height, float cellSize, Vector3 origin)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.origin = origin;
        grid = new Cell[width, height];
    }

    // 添加影响力（高斯分布）
    public void AddInfluence(Vector3 worldPos, float threat, float attraction, float radius)
    {
        Vector2Int center = WorldToGrid(worldPos);
        int cellRadius = Mathf.CeilToInt(radius / cellSize);

        for (int x = Mathf.Max(0, center.x - cellRadius); x < Mathf.Min(width, center.x + cellRadius); x++)
        {
            for (int y = Mathf.Max(0, center.y - cellRadius); y < Mathf.Min(height, center.y + cellRadius); y++)
            {
                Vector3 cellWorld = GridToWorld(x, y);
                float dist = Vector3.Distance(worldPos, cellWorld);

                if (dist < radius)
                {
                    // 高斯衰减（中心强，边缘弱）
                    float falloff = Mathf.Exp(-dist * dist / (radius * radius));
                    grid[x, y].threat += threat * falloff;
                    grid[x, y].attraction += attraction * falloff;
                }
            }
        }
    }

    // 衰减（每帧调用）
    public void Decay(float factor)
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            grid[x, y].threat *= factor;
            grid[x, y].attraction *= factor;
        }
    }

    // 根据影响力选择目标位置
    public Vector3 GetBestPosition(Func<Cell, float> scoreFunc)
    {
        float bestScore = float.MinValue;
        Vector3 bestPos = origin;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            float score = scoreFunc(grid[x, y]);
            if (score > bestScore)
            {
                bestScore = score;
                bestPos = GridToWorld(x, y);
            }
        }

        return bestPos;
    }

    // 寻找安全位置（低威胁 + 高吸引力）
    public Vector3 FindSafePosition()
        => GetBestPosition(cell => cell.attraction - cell.threat);

    // 坐标转换
    private Vector2Int WorldToGrid(Vector3 world)
    {
        int x = Mathf.FloorToInt((world.x - origin.x) / cellSize);
        int z = Mathf.FloorToInt((world.z - origin.z) / cellSize);
        return new Vector2Int(x, z);
    }

    private Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(origin.x + x * cellSize + cellSize * 0.5f, 0, origin.z + y * cellSize + cellSize * 0.5f);
    }
}
```

---

## 6. AI 性能优化

### 大规模 AI 优化策略

```
1000 个 Agent 同时跑 A* 寻路？CPU 冒烟。

优化策略：
1. 更新频率分层：
   - 近处 AI（玩家附近 20m）：每帧更新
   - 中距离（20~50m）：每 5 帧更新
   - 远处（50m+）：每 10 帧更新 / 禁用 AI

2. 寻路缓存：
   - 路径共享（同种 AI 走相同路径）
   - 路径点缓存（不每帧计算）

3. LOD 系统（Level of Detail）：
   - 远距离 AI：只有简单转向，无寻路
   - 中等距离 AI：简单 FSM
   - 近处 AI：完整行为树
```

```csharp
public class AILODManager : MonoBehaviour
{
    public Transform player;
    private AIAgent[] allAgents;

    [System.Serializable]
    public class AILODConfig
    {
        public float maxDistance;
        public int updateInterval;  // 多少帧更新一次
        public bool enablePathfinding;
        public bool enableBehaviorTree;
    }

    public AILODConfig[] lodLevels;

    void Update()
    {
        foreach (var agent in allAgents)
        {
            float dist = Vector3.Distance(player.position, agent.transform.position);
            AILODConfig lod = GetLOD(dist);

            if (Time.frameCount % lod.updateInterval == 0)
            {
                agent.SetAIEnabled(true);
                agent.SetPathfindingEnabled(lod.enablePathfinding);
                agent.SetBehaviorTreeEnabled(lod.enableBehaviorTree);
                agent.UpdateAI();
            }
            else
            {
                agent.SetAIEnabled(false);
            }
        }
    }
}
```

---

## 7. C++/Raylib 对比

| 概念 | C++/Raylib | Unity/C# |
|------|-----------|----------|
| GOAP | 自实现（A* + 动作空间） | 同上思路 |
| Utility AI | 自实现 | 同上思路 |
| NavMesh | Recast/Detour 库 | 内置 NavMesh 组件 |
| Steering | 自实现 | 同上思路 |
| Flocking | Craig Reynolds 算法 | 同上算法 |
| 影响地图 | 2D 数组 | 同上 |
| Job System 优化 | std::thread + 共享队列 | Unity 的 IJobParallelFor |

**GOAP 本质是 AI 领域的规划器**，它的 A* 搜索和寻路 A* 是同一种算法——只是寻路是在"空间"中搜索路径，GOAP 是在"动作空间"中搜索计划。二者都遵循 F = G + H 的核心公式。

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| GOAP | 在动作空间中用 A* 搜索最优计划 |
| Utility AI | 给每个行为打分，选最高分执行 |
| NavMeshSurface | 运行时动态烘焙 NavMesh |
| NavMeshLink | 连接分离的 NavMesh 区域（跳跃/爬梯） |
| Steering | 基于受力的移动控制（Seek/Flee/Pursuit） |
| Flocking | 对齐 + 聚合 + 分离 = 群体运动 |
| 影响地图 | 2D 网格存储威胁/吸引力，辅助决策 |
| AI LOD | 根据距离降低 AI 更新频率 |

**对比 C++：** Recation/Detour（C++ NavMesh 库）是 Unity NavMesh 的底层实现。Steering Behaviors 不依赖引擎，C# 实现和 C++ 完全一样。GOAP 最早出现在《FEAR》游戏中，它的 C# 实现和 C++ 实现逻辑相同。
