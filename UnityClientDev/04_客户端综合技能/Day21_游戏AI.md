# Day 21：游戏 AI — 从状态机到行为树

## 0. 为什么需要游戏 AI？

游戏 AI 不是让电脑"变聪明"，而是让**行为看起来合理且有变化**。

```
没有 AI 的敌人：
一直朝玩家走 → 撞墙也走 → 和另一个敌人重叠在一起

有简单 AI 的敌人：
巡逻 → 发现玩家 → 追踪 → 攻击 → 玩家跑远 → 继续巡逻
```

Raylib 中你可能做过简单的状态切换（如 09 ScreenManager），游戏 AI 是把这种"状态切换"系统化、工程化。

**游戏 AI 的三个层次：**

```
感知层（Perception）：  敌人怎么"看到"和"听到"玩家？
决策层（Decision）：    接下来该做什么？（巡逻？追踪？攻击？）
行动层（Action）：      怎么做？（移动、播放动画、攻击动画）
```

---

## 1. 有限状态机（FSM）— 最常用的 AI 架构

### 什么是状态机？

```
一个状态 = 一个行为模式
一个转换 = 触发条件

状态图：
┌──────────┐   发现玩家   ┌──────────┐
│   Patrol  │ ──────────▶ │   Chase   │
│  (巡逻)   │              │  (追踪)   │
└──────────┘ ◀─────────── └──────────┘
   ▲   玩家跑远                │  进入攻击范围
   │                          ▼
   │                     ┌──────────┐
   └─────────────────────│  Attack   │
       玩家跑远           │  (攻击)   │
                          └──────────┘
```

### C# 实现 FSM

```csharp
// 状态基类
public abstract class FSMState
{
    public abstract void OnEnter();      // 进入状态时执行一次
    public abstract void OnUpdate();     // 每帧执行
    public abstract void OnExit();       // 离开状态时执行一次
}

// 具体状态
public class PatrolState : FSMState
{
    private Enemy enemy;
    private Vector3 targetPos;

    public PatrolState(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public override void OnEnter()
    {
        // 选择一个随机巡逻点
        targetPos = enemy.GetRandomPatrolPoint();
    }

    public override void OnUpdate()
    {
        // 朝巡逻点移动
        enemy.MoveToward(targetPos);

        // 到达目标后选下一个点
        if (Vector3.Distance(enemy.transform.position, targetPos) < 0.5f)
            targetPos = enemy.GetRandomPatrolPoint();
    }

    public override void OnExit()
    {
        // 清理
    }
}

public class ChaseState : FSMState
{
    private Enemy enemy;

    public ChaseState(Enemy enemy) => this.enemy = enemy;

    public override void OnEnter()
    {
        enemy.animator.SetBool("isRunning", true);
    }

    public override void OnUpdate()
    {
        enemy.MoveToward(enemy.player.position);
    }

    public override void OnExit()
    {
        enemy.animator.SetBool("isRunning", false);
    }
}

public class AttackState : FSMState
{
    private Enemy enemy;
    private float attackTimer;

    public AttackState(Enemy enemy) => this.enemy = enemy;

    public override void OnEnter()
    {
        attackTimer = 0;
    }

    public override void OnUpdate()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= enemy.attackInterval)
        {
            enemy.Attack();
            attackTimer = 0;
        }
    }

    public override void OnExit() { }
}

// 状态机管理器
public class StateMachine
{
    private Dictionary<System.Type, FSMState> states = new();
    private FSMState currentState;

    public void AddState(FSMState state)
        => states[state.GetType()] = state;

    public void ChangeState<T>() where T : FSMState
    {
        if (currentState?.GetType() == typeof(T))
            return; // 已经在目标状态，避免重复 Enter/Exit

        currentState?.OnExit();
        currentState = states[typeof(T)];
        currentState.OnEnter();
    }

    public void Update()
    {
        currentState?.OnUpdate();
    }

    public bool IsInState<T>() where T : FSMState
        => currentState is T;
}
```

### 完整敌人

```csharp
public class Enemy : MonoBehaviour
{
    public Transform player;
    public Animator animator;
    public float patrolRange = 10f;
    public float detectRange = 8f;
    public float attackRange = 2f;
    public float attackInterval = 1.5f;

    private StateMachine fsm;

    void Start()
    {
        fsm = new StateMachine();
        fsm.AddState(new PatrolState(this));
        fsm.AddState(new ChaseState(this));
        fsm.AddState(new AttackState(this));
        fsm.ChangeState<PatrolState>();
    }

    void Update()
    {
        // 状态转换逻辑
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer <= attackRange)
            fsm.ChangeState<AttackState>();
        else if (distToPlayer <= detectRange)
            fsm.ChangeState<ChaseState>();
        else
            fsm.ChangeState<PatrolState>();

        fsm.Update();
    }

    public void MoveToward(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * 3f * Time.deltaTime;
        transform.forward = dir; // 面朝移动方向
    }

    public Vector3 GetRandomPatrolPoint()
    {
        Vector2 random = Random.insideUnitCircle * patrolRange;
        return transform.position + new Vector3(random.x, 0, random.y);
    }

    public void Attack()
    {
        animator.SetTrigger("Attack");
        // 伤害逻辑
        player.GetComponent<Player>().TakeDamage(10);
    }

    // Gizmo 可视化视野范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
```

### FSM 的核心问题

```
FSM 的敌人数量增加后：
- 巡逻 + 战斗 + 受伤 + 逃跑 + 呼叫同伴 → 5 种状态
- 每种状态都可能和其他状态组合 → 状态数量指数增长
- 状态转换逻辑混在 Update 的条件判断里 → 难以维护

这就是行为树要解决的问题
```

---

## 2. 从 FSM 到行为树（BT）

### 行为树结构

```
Selector（选择器/优先级）：按顺序尝试子节点，一个成功就返回
Sequence（序列）：按顺序执行子节点，全部成功才成功
Action（动作）：执行具体行为，返回 Success/Failure
Condition（条件）：检查某个条件，返回 Success/Failure

行为树示例：
Root
├── Selector（主循环，优先级从高到低）
│   ├── Sequence（战斗）
│   │   ├── Condition 玩家在攻击范围内？
│   │   └── Action 攻击
│   ├── Sequence（追踪）
│   │   ├── Condition 玩家在检测范围内？
│   │   └── Action 追踪玩家
│   └── Sequence（巡逻）
│       ├── Action 选巡逻点
│       └── Action 移动到巡逻点
```

### 完整的行为树实现

```csharp
public enum BTResult { Success, Failure, Running }

public abstract class BTNode
{
    public abstract BTResult Execute();
}

// 条件节点
public class ConditionNode : BTNode
{
    private System.Func<bool> condition;

    public ConditionNode(System.Func<bool> condition) => this.condition = condition;

    public override BTResult Execute() => condition() ? BTResult.Success : BTResult.Failure;
}

// 动作节点
public class ActionNode : BTNode
{
    private System.Func<BTResult> action;

    public ActionNode(System.Func<BTResult> action) => this.action = action;

    public override BTResult Execute() => action();
}

// Sequence（序列节点）：按顺序执行，全部成功才成功
public class Sequence : BTNode
{
    private List<BTNode> children = new();
    private int currentIndex = 0;

    public void Add(BTNode node) => children.Add(node);

    public override BTResult Execute()
    {
        while (currentIndex < children.Count)
        {
            BTResult result = children[currentIndex].Execute();

            if (result == BTResult.Running)
                return BTResult.Running; // 等下一次执行

            if (result == BTResult.Failure)
            {
                currentIndex = 0; // 重置
                return BTResult.Failure;
            }

            currentIndex++;
        }

        currentIndex = 0;
        return BTResult.Success;
    }
}

// Selector（选择器）：一个成功就成功
public class Selector : BTNode
{
    private List<BTNode> children = new();
    private int currentIndex = 0;

    public void Add(BTNode node) => children.Add(node);

    public override BTResult Execute()
    {
        while (currentIndex < children.Count)
        {
            BTResult result = children[currentIndex].Execute();

            if (result == BTResult.Running)
                return BTResult.Running;

            if (result == BTResult.Success)
            {
                currentIndex = 0;
                return BTResult.Success;
            }

            currentIndex++;
        }

        currentIndex = 0;
        return BTResult.Failure;
    }
}
```

### 使用行为树的敌人

```csharp
public class BTEnemy : MonoBehaviour
{
    public Transform player;
    public float attackRange = 2f;
    public float detectRange = 8f;

    private BTNode root;
    private Vector3 patrolTarget;

    void Start()
    {
        root = new Selector();
        var rootSel = (Selector)root;

        // 攻击序列（最高优先级）
        var attackSeq = new Sequence();
        attackSeq.Add(new ConditionNode(() => IsInRange(attackRange)));
        attackSeq.Add(new ActionNode(() => { Attack(); return BTResult.Success; }));
        rootSel.Add(attackSeq);

        // 追踪序列
        var chaseSeq = new Sequence();
        chaseSeq.Add(new ConditionNode(() => IsInRange(detectRange)));
        chaseSeq.Add(new ActionNode(() => { ChasePlayer(); return BTResult.Running; }));
        rootSel.Add(chaseSeq);

        // 巡逻序列（最低优先级）
        var patrolSeq = new Sequence();
        patrolSeq.Add(new ActionNode(() => { PickPatrolTarget(); return BTResult.Success; }));
        patrolSeq.Add(new ActionNode(() => { MoveToPatrol(); return BTResult.Running; }));
        rootSel.Add(patrolSeq);
    }

    void Update() => root.Execute();

    private bool IsInRange(float range)
        => Vector3.Distance(transform.position, player.position) <= range;

    private void ChasePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * 5f * Time.deltaTime;
    }

    private void Attack()
    {
        Debug.Log("攻击！");
    }

    private void PickPatrolTarget()
    {
        patrolTarget = transform.position + Random.insideUnitSphere * 5f;
    }

    private void MoveToPatrol()
    {
        Vector3 dir = (patrolTarget - transform.position).normalized;
        transform.position += dir * 2f * Time.deltaTime;

        if (Vector3.Distance(transform.position, patrolTarget) < 0.5f)
            PickPatrolTarget(); // 到了就选下一个
    }
}
```

### FSM vs 行为树

```
FSM：
简单直接、性能开销小                 ← 优点
状态多了难以维护、逻辑分散在各状态中   ← 缺点
适合：简单 AI（3~5 个状态）

行为树：
逻辑集中、可扩展、可视化编辑         ← 优点
实现复杂、性能开销略大               ← 缺点
适合：复杂 AI（10+ 个行为）
```

---

## 3. 感知系统——AI 的"眼睛"和"耳朵"

感知系统是 AI 的第一个环节——AI 需要感知周围环境才能做决策。

```csharp
public class PerceptionSystem : MonoBehaviour
{
    [Header("视觉")]
    public float viewDistance = 15f;
    public float viewAngle = 120f;  // 视野角度（度）

    [Header("听觉")]
    public float hearDistance = 20f;

    [Header("记忆")]
    public float memoryDuration = 5f;  // 看到玩家后还能记得多久
    private float lastKnownTime;
    private Vector3 lastKnownPosition;

    private Transform player;

    void Start() => player = GameObject.FindGameObjectWithTag("Player").transform;

    // 更新感知信息（每帧调用）
    public void UpdatePerception()
    {
        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;

        // 视觉检测
        if (dist <= viewDistance)
        {
            float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
            if (angle <= viewAngle / 2)
            {
                // 视线检测（射线，防止穿墙）
                if (Physics.Raycast(transform.position, toPlayer.normalized, out RaycastHit hit, viewDistance))
                {
                    if (hit.transform.CompareTag("Player"))
                    {
                        // 看到玩家了！
                        lastKnownPosition = player.position;
                        lastKnownTime = Time.time;
                        Debug.Log("我看到你了！");
                    }
                }
            }
        }

        // 听觉检测
        var noiseSource = FindObjectOfType<NoiseSource>();
        if (noiseSource != null && noiseSource.isMakingNoise)
        {
            float noiseDist = Vector3.Distance(transform.position, noiseSource.transform.position);
            if (noiseDist <= hearDistance)
            {
                lastKnownPosition = noiseSource.transform.position;
                lastKnownTime = Time.time;
            }
        }
    }

    // 判断是否还记得玩家的位置
    public bool KnowsPlayerPosition()
        => Time.time - lastKnownTime <= memoryDuration;

    // 获取记忆中玩家最后出现的位置
    public Vector3 GetLastKnownPosition() => lastKnownPosition;
}
```

---

## 4. A* 寻路算法

### 核心思想

```
A* 是在地图上找最短路径的算法：
1. 从起点开始
2. 每次选"代价最小的格子"扩展
3. 直到到达终点

代价公式：
F = G + H
G = 从起点到当前格子的实际代价
H = 从当前格子到终点的预估代价（曼哈顿距离或欧几里得距离）
```

### 简化实现

```csharp
public class AStar
{
    public class Node
    {
        public int x, y;
        public int g, h;
        public int F => g + h;
        public Node parent;
    }

    public static List<Vector2Int> FindPath(bool[,] grid, Vector2Int start, Vector2Int end)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        List<Node> openList = new();
        HashSet<Vector2Int> closedSet = new();

        Node startNode = new Node { x = start.x, y = start.y };
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // 找 F 值最小的节点
            Node current = openList[0];
            for (int i = 1; i < openList.Count; i++)
                if (openList[i].F < current.F)
                    current = openList[i];

            openList.Remove(current);
            closedSet.Add(new Vector2Int(current.x, current.y));

            // 到达终点
            if (current.x == end.x && current.y == end.y)
                return ReconstructPath(current);

            // 扩展邻居
            foreach (var dir in new Vector2Int[] {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right })
            {
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;

                Vector2Int nPos = new Vector2Int(nx, ny);

                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (!grid[nx, ny]) continue; // 障碍物
                if (closedSet.Contains(nPos)) continue;

                int newG = current.g + 1;

                var existing = openList.Find(n => n.x == nx && n.y == ny);
                if (existing != null)
                {
                    if (newG < existing.g)
                    {
                        existing.g = newG;
                        existing.parent = current;
                    }
                }
                else
                {
                    openList.Add(new Node
                    {
                        x = nx, y = ny,
                        g = newG,
                        h = Mathf.Abs(nx - end.x) + Mathf.Abs(ny - end.y),
                        parent = current
                    });
                }
            }
        }

        return null; // 没有路径
    }

    private static List<Vector2Int> ReconstructPath(Node node)
    {
        List<Vector2Int> path = new();
        while (node != null)
        {
            path.Add(new Vector2Int(node.x, node.y));
            node = node.parent;
        }
        path.Reverse();
        return path;
    }
}
```

---

## 5. Unity NavMesh — 现成的寻路方案

如果你**不需要手动实现寻路**，Unity 提供了 NavMesh 系统：

```csharp
using UnityEngine.AI;

public class NavMeshEnemy : MonoBehaviour
{
    private NavMeshAgent agent;
    public Transform player;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        agent.SetDestination(player.position);
    }
}
```

### NavMesh 参数说明

```
NavMeshAgent 关键参数：
- Speed:        移动速度
- Angular Speed: 旋转速度
- Acceleration: 加速度
- Stopping Distance: 离目标多远停下（攻击距离）
- Radius:        代理半径（影响能否通过狭窄通道）
- Height:        代理高度
- Obstacle Avoidance: 障碍物躲避等级
```

### NavMesh 工作流

```
1. 选中场景 → Window → AI → Navigation
2. 将地面设为 Navigation Static
3. 将障碍物设为 Not Walkable
4. Bake（烘焙）→ 生成 NavMesh 数据
5. 给敌人挂 NavMeshAgent 组件
6. 调用 agent.SetDestination()
```

---

## 6. 群体行为

多个 AI 之间的协调：

```csharp
public class GroupAI : MonoBehaviour
{
    // 最简单的群体行为：协作包围
    public void FlankPlayer(Transform player, List<Enemy> group)
    {
        // 根据敌人数量分配包围位置
        for (int i = 0; i < group.Count; i++)
        {
            float angle = (360f / group.Count) * i;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * 3f;
            Vector3 flankPos = player.position + offset;

            group[i].SetTarget(flankPos);
        }
    }
}
```

---

## 7. 练习

### 练习 1：巡逻 + 追踪 AI

```csharp
// 实现一个 FSM 敌人：
// 1. Patrol：沿路径点巡逻
// 2. Chase：发现玩家后追踪（跑得比巡逻快）
// 3. Attack：近战攻击
// 4. 玩家跑远后回到 Patrol
// 要求：用 Gizmos 画出巡逻路径和检测范围
```

### 练习 2：行为树敌人

```csharp
// 用行为树实现一个更复杂的敌人：
// 1. 保持距离射击（不靠近玩家）
// 2. 血量低时逃跑
// 3. 逃跑后找掩体恢复
// 4. 有队友被击杀时呼叫支援
// 
// 要求：行为树结构清晰，节点可复用
```

### 练习 3：A* 寻路可视化

```csharp
// 实现一个简单的 10×10 网格
// 1. 随机放置障碍物
// 2. 点击设置起点和终点
// 3. 用 Gizmos 或 Debug.DrawLine 画出 A* 找到的路径
// 4. 显示 OpenList 和 ClosedList 的探索过程
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| FSM | 状态 → 转换 → 行为，适合简单 AI |
| 行为树 | Selector/Sequence 编排行为，可扩展性强 |
| A* | F = G + H，每次选代价最小的格子扩展 |
| NavMesh | Unity 内置寻路，Bake 即用 |
| 感知系统 | AI 通过视觉/听觉获取环境信息 |
| 群体行为 | 多个 AI 之间的协作与分工 |

**对比 Raylib：** Raylib 09 的 ScreenManager 就是一个简单的 FSM（游戏状态切换），但游戏 AI 的 FSM 更工程化（每个状态有 Enter/Update/Exit）。行为树在 Raylib 中没有直接对应——你可以理解为"用一种树状结构替代一大堆 if-else"。A* 是标准算法，在任何语言中实现都一样（OpenList 用优先队列优化是通用技巧）。
