# Day 14：Redis 实战应用 — 进阶深入

> 目标读者：游戏服务端开发者，了解 Redis 基本命令
> 前置知识：缓存概念、分布式系统基础、Redis 主从模式

---

## 一、缓存模式深度对比

### 1.1 Cache-Aside（旁路缓存）

最常用的模式：应用程序同时管理缓存和数据库。

```
读流程：
  1. 应用读取缓存 key
  2. 缓存命中 → 直接返回
  3. 缓存未命中 → 从数据库读取 → 写入缓存 → 返回

写流程：
  方案 A（先更新数据库，再删除缓存）← 推荐
  方案 B（先删除缓存，再更新数据库）← 不推荐，有并发问题
```

```python
# Cache-Aside 读实现（Python 伪代码）
def get_player_profile(player_id):
    key = f"player:profile:{player_id}"

    # 1. 查缓存
    profile = redis.get(key)
    if profile:
        return deserialize(profile)

    # 2. 缓存未命中，查数据库
    profile = db.query("SELECT * FROM players WHERE id = ?", player_id)

    # 3. 写入缓存（设置过期时间，防止缓存雪崩）
    redis.setex(key, 3600, serialize(profile))

    return profile

# Cache-Aside 写实现
def update_player_profile(player_id, data):
    # 1. 先更新数据库
    db.execute("UPDATE players SET name = ? WHERE id = ?",
               data['name'], player_id)

    # 2. 再删除缓存（不是更新缓存！）
    redis.delete(f"player:profile:{player_id}")
    # 为什么是删除而不是更新？
    # 因为删除更简单，避免并发写导致的数据不一致
    # 下次读的时候再重新加载
```

### 1.2 Read-Through（穿透读）

```
与 Cache-Aside 的区别：
  Cache-Aside：应用程序代码管理缓存逻辑
  Read-Through：缓存库（如 Redis 模块）自动处理缓存未命中

读流程：
  应用请求 → 缓存代理（如 RedisJSON 模块）→ 命中返回
  → 未命中 → 自动从数据库加载 → 返回

游戏场景：ProxySQL + Redis 作为缓存层，对应用透明。
依赖专门的缓存中间件，不常用在游戏里。
```

### 1.3 Write-Through（穿透写）

```
写流程：
  应用写入缓存 → 缓存同步写入数据库（同步阻塞）

优点：数据强一致
缺点：写入延迟 = 缓存延迟 + 数据库延迟（翻倍）

游戏场景：几乎不用，因为写性能太差。
```

### 1.4 Write-Behind（异步写回）

```
写流程：
  应用只写缓存 → 立即返回 → 后台异步刷到数据库

                +-----------+
  应用 ──→ Redis 缓存      │
        │    │             │
        │    +─────→ 定时/批量刷到 MySQL
        │                  │
        │←─── 立即返回     │
        +------------------+

优点：写入性能极高（只写内存）
缺点：Redis 宕机会丢数据（如果数据还没刷到 MySQL）

游戏场景：
  - 玩家位置上报（丢了也就丢了）
  - 登录日志（可以批量写入）
  - 排行榜缓存（定期快照到数据库）
```

### 1.5 游戏缓存策略选择

```
| 业务场景          | 推荐模式          | 原因                         |
|-------------------|------------------|------------------------------|
| 玩家资料查询       | Cache-Aside      | 读多写少，缓存穿透可控         |
| 登录 Token/会话   | Cache-Aside      | 写入频繁但只读一次             |
| 排行榜            | Write-Behind     | 写频繁，定期持久化             |
| 在线状态          | Write-Behind     | 丢失可接受，要求高性能         |
| 活动配置          | Cache-Aside      | 变化极少，缓存时间长           |
| 聊天消息          | Write-Behind     | 高吞吐，容忍丢失               |
| 充值记录          | Write-Through    | 不能丢！同步写数据库           |
```

---

## 二、分布式锁：Redlock 算法及其争议

### 2.1 简单锁的问题

```python
# 初级分布式锁（单 Redis 节点）
# 问题：主从切换时可能丢失锁
def acquire_lock(lock_name, ttl_ms):
    key = f"lock:{lock_name}"
    # SET NX + PX 是原子操作
    return redis.set(key, "1", nx=True, px=ttl_ms)

def release_lock(lock_name):
    key = f"lock:{lock_name}"
    # 需要用 Lua 保证原子性（检查持有者后删除）
    redis.delete(key)

"""
问题场景：
  1. 客户端 A 在 Master 上获取了锁
  2. Master 宕机，锁还没同步到 Slave
  3. Slave 晋升为 Master
  4. 客户端 B 也能获取到同一个锁（两个客户端同时持有锁！）
"""
```

### 2.2 Redlock 算法

```
Redlock（Redis 官方分布式锁算法）步骤：

前提：需要 N 个独立的 Redis 节点（推荐 5 个，互为主从）

加锁：
  1. 获取当前时间戳 T1
  2. 依次向 N 个节点发送 SET key value NX PX ttl
     - 每个请求设置超时时间（远小于 ttl，如 5ms）
     - 如果超时或失败，跳过该节点
  3. 统计成功加锁的节点数
  4. 如果成功节点 >= N/2 + 1（≥3个），并且
     总耗时 (当前时间 - T1) < ttl → 锁有效
  5. 否则，认为加锁失败，向所有节点发送解锁

解锁：
  向所有 N 个节点发送解锁请求（用 Lua 脚本保证原子性）
```

```python
# Redlock 简化实现（Python 伪代码）
class Redlock:
    def __init__(self, nodes):
        self.nodes = nodes  # [redis1, redis2, redis3, redis4, redis5]
        self.quorum = len(nodes) // 2 + 1  # 至少 3 个

    def lock(self, resource, ttl_ms):
        key = f"lock:{resource}"
        value = str(uuid.uuid4())  # 唯一标识，防止误解锁
        start = time.time()
        acquired = 0

        for node in self.nodes:
            try:
                # 每个节点的超时时间 = ttl 的一小部分
                node.client.set(key, value, nx=True, px=ttl_ms,
                                socket_timeout=ttl_ms // 10)
                acquired += 1
            except:
                continue

        elapsed = (time.time() - start) * 1000

        if acquired >= self.quorum and elapsed < ttl_ms:
            # 加锁成功
            return (resource, value)
        else:
            # 加锁失败，释放所有节点上的锁
            for node in self.nodes:
                node.client.delete(key)
            return None

    def unlock(self, lock_info):
        if not lock_info:
            return
        resource, value = lock_info
        key = f"lock:{resource}"
        # 用 Lua 脚本：只有 value 匹配才删除
        script = """
        if redis.call("GET", KEYS[1]) == ARGV[1] then
            return redis.call("DEL", KEYS[1])
        end
        return 0
        """
        for node in self.nodes:
            node.client.eval(script, 1, key, value)
```

### 2.3 Redlock 的争议

```
Martin Kleppmann（《DDIA》作者）的批评：
  1. 依赖系统时钟：如果某节点系统时钟发生跳跃（NTP 同步），ttl 计算会出错
  2. 没有 fencing token：不能保证互斥（没有递增的 token 机制来隔离旧锁）

Redis 作者 antirez 的反驳：
  1. Redis 节点不应使用不稳定的 NTP 配置
  2. 时钟跳跃是极小概率事件
  3. Redlock 满足绝大多数实际场景需求

游戏场景建议：
  - 普通业务（如公会战匹配锁）：SET NX 就足够了
  - 对安全性要求高的场景（如充值防重复）：用 Redlock
  - 需要绝对互斥：用 ZooKeeper 的临时顺序节点（强制 fencing token）
```

---

## 三、Redis Cluster：哈希槽与数据迁移

### 3.1 哈希槽分配

```
Redis Cluster 将 key 空间划分为 16384 个哈希槽（hash slot）。

槽位计算：
  slot = CRC16(key) % 16384

槽位分配示例（3 节点集群）：
  Node A: 0 - 5460
  Node B: 5461 - 10922
  Node C: 10923 - 16383

key 定位流程：
  客户端 → CRC16("player:1001") % 16384 = 12345
  → 槽 12345 在 Node C → 直接请求 Node C

注意：
  没有一致性哈希！Redis Cluster 用固定哈希槽 + 槽迁移代替一致性哈希
  优势：精确控制数据分布，迁移粒度更细（从"节点级别"降到"槽级别"）
```

### 3.2 槽迁移（Resharding）

```
在线扩容流程（新增一个节点）：

1. 新节点加入集群
   CLUSTER MEET 192.168.1.4 6379

2. 从现有节点迁移槽到新节点
   redis-cli --cluster reshard 192.168.1.1:6379

3. 迁移内部流程（以槽 1234 从 A → B 为例）：
   a. 目标节点 B: CLUSTER SETSLOT 1234 IMPORTING src-node-A
   b. 源节点 A: CLUSTER SETSLOT 1234 MIGRATING dst-node-B
   c. 从 A 获取槽中的 key: CLUSTER GETKEYSINSLOT 1234 100
   d. 对每个 key 执行 MIGRATE 命令（原子性迁移）
       MIGRATE B_host B_port key 0 5000
   e. 迁移完成后，所有节点 SETSLOT 1234 NODE B

迁移期间的访问处理：
  当槽正在迁移时，客户端访问：
  - 请求 A: 检查 key 是否还在 A → 在就处理
  - 如果 key 已迁移到 B: 返回 ASK 重定向
    客户端收到 ASK → 发送 ASKING 命令 → 请求 B
```

### 3.3 MOVED 与 ASK 重定向

```
MOVED（永久重定向）：
  客户端请求的槽不在当前节点
  返回：MOVED <slot> <ip>:<port>
  客户端：更新本地槽位缓存，直接请求目标节点

ASK（临时重定向）：
  槽正在迁移中，key 已经从源节点移走
  返回：ASK <slot> <ip>:<port>
  客户端：先发 ASKING（跳过检查），再发命令

区别：
  MOVED → 更新缓存，以后直接去新节点
  ASK → 只在这次迁移过程中使用，不更新缓存
```

### 3.4 集群限制

```
Redis Cluster 的限制（游戏开发必须知道）：

1. 不支持多 key 操作（除非所有 key 在同一个槽）
   ❌ SUNION {key1} {key2}  ← 不同槽报错
   ✅ 使用 hash tag 强制放在同一槽
      SUNION {player:1001}:items {player:1002}:items
      → {player:1001} 和 {player:1002} 在同一个槽

2. 事务（MULTI/EXEC）只支持单个 slot 内的 key

3. Pipeline 中的命令必须都在同一节点（不同槽需分多个 pipeline）

4. Lua 脚本中所有 key 必须在同一节点（使用 hash tag）

游戏场景集群设计：
  player:{player_id}:profile
  player:{player_id}:bag
  player:{player_id}:quests
  使用 hash tag {player_id} 让同玩家的数据在同一个节点
```

---

## 四、Redis Sentinel 故障转移

### 4.1 Sentinel 架构

```
+------------------+  +------------------+  +------------------+
| Sentinel A       |  | Sentinel B       |  | Sentinel C       |
| (监控+决策)       |  | (监控+决策)       |  | (监控+决策)       |
+------------------+  +------------------+  +------------------+
          |                    |                    |
          +───────────┬───────┴───────────┬────────+
                      |                    |
               +------+------+     +------+------+
               | Master M1   |     | Slave S1   |
               | 可读可写     |     | 只读        |
               +------+------+     +------+------+
                      |
               +------+------+
               | Slave S2   |
               | 只读        |
               +-------------+
```

### 4.2 故障转移流程

```
主观下线（Subjectively Down, SDOWN）：
  单个 Sentinel 节点发现 Master 没有在规定时间内回复 PING
  sentinel down-after-milliseconds 5000  ← 5 秒无响应

客观下线（Objectively Down, ODOWN）：
  多个 Sentinel 节点（quorum 配置）都认为 Master 已下线
  sentinel monitor mymaster 127.0.0.1 6379 2
  → 2 个 Sentinel 同意才认为 ODOWN

Leader 选举（Raft 类似算法）：
  1. 每个 Sentinel 都有资格成为 Leader
  2. 发起投票（类似 Raft 的 RequestVote）
  3. 获得多数票的 Sentinel 成为 Leader
  4. 由 Leader 执行故障转移

故障转移步骤（Leader 执行）：
  1. 从 Slave 列表中选出一个新 Master
     - 优先级最高（slave-priority）
     - 复制偏移量最大（数据最新）
     - 运行 ID 最小（选最早启动的）

  2. 让选中的 Slave 执行 SLAVEOF NO ONE → 成为新 Master

  3. 修改其他 Slave 的复制目标为新的 Master

  4. 设置旧 Master 的见证（旧 Master 恢复后变成新 Master 的 Slave）

  5. 通知客户端（通过 Pub/Sub 或客户端轮询）
```

### 4.3 游戏场景的 Sentinel 配置

```conf
# sentinel.conf
sentinel monitor game-master 192.168.1.10 6379 2
sentinel down-after-milliseconds game-master 3000     # 3 秒（敏感些）
sentinel failover-timeout game-master 30000           # 30 秒超时
sentinel parallel-syncs game-master 1                 # 同时只同步一个 Slave
sentinel auth-pass game-master MyRedisPass            # 密码认证

# 游戏业务建议
# down-after-milliseconds = 3000，比默认 30 秒敏感
# 因为游戏玩家等待恢复的时间容忍度低
```

---

## 五、Stream 消费组与事件溯源

### 5.1 Stream 结构

```
Key: "game:events"
+----------+----------+----------+----------+
| 12345-0  | 12346-0  | 12347-0  | 12348-0  |
| player:1 | player:2 | player:1 | player:3 |
| login    | levelup  | buy_item | battle   |
+----------+----------+----------+----------+
    ↑                         ↑
 第一个消息               最新消息（默认从这开始读）

每个消息的 ID = <毫秒时间戳>-<序号>
```

### 5.2 消费组

```python
# Stream 消费组（Python 示例）
# 场景：游戏事件异步处理

# 1. 创建消费组
redis.xgroup_create("game:events", "analytics_group", id="0", mkstream=True)
redis.xgroup_create("game:events", "mail_group", id="0", mkstream=True)

# 2. 生产者（游戏服写入事件）
def on_player_login(player_id):
    redis.xadd("game:events", {
        "type": "login",
        "player_id": player_id,
        "timestamp": time.time()
    })

# 3. 消费者（分析服务处理）
def process_analytics():
    while True:
        # 阻塞读取，每次取 10 条
        results = redis.xreadgroup(
            "analytics_group", "consumer_1",
            {"game:events": ">"},  # ">" = 只读新消息
            count=10, block=2000
        )
        for stream, messages in results:
            for msg_id, msg in messages:
                process_event(msg)
                # 确认处理完成
                redis.xack("game:events", "analytics_group", msg_id)
```

### 5.3 事件溯源（Event Sourcing）

```
事件溯源模式：用 Stream 记录所有玩家操作，既用于实时处理，也用于回放恢复。

示例：玩家经验值变更

数据库中的玩家表：
  player_id=1001, level=50, exp=12345

Stream 中的事件序列：
  1. 经验增加 500（打怪）
  2. 经验增加 300（任务完成）
  3. 等级提升，经验重置

回放恢复：
  如果玩家数据丢失 → 从 Stream 重放所有事件 → 恢复最终状态

游戏场景：
  - 排行榜回档恢复
  - 充值流水对账
  - 玩家行为分析
```

---

## 六、Lua 脚本原子性

### 6.1 原子性保证

```lua
-- Redis Lua 脚本在单线程中执行，保证原子性
-- 脚本执行期间，其他命令会被阻塞

-- 场景：安全的物品转移（扣减+增加，原子操作）
local SCRIPT_DECR_INCR = `
    local from_key = KEYS[1]      -- 源玩家背包
    local to_key = KEYS[2]        -- 目标玩家背包
    local item_id = ARGV[1]       -- 物品 ID
    local count = tonumber(ARGV[2]) -- 数量

    -- 检查源玩家是否有足够物品
    local from_qty = redis.call("HGET", from_key, item_id)
    if not from_qty or tonumber(from_qty) < count then
        return -1  -- 物品不足
    end

    -- 扣减源玩家
    redis.call("HINCRBY", from_key, item_id, -count)

    -- 增加目标玩家
    redis.call("HINCRBY", to_key, item_id, count)

    -- 清理空字段（可选）
    if tonumber(redis.call("HGET", from_key, item_id)) <= 0 then
        redis.call("HDEL", from_key, item_id)
    end

    return 0  -- 成功
`

# 在 Python 中调用
redis.eval(SCRIPT_DECR_INCR, 2, "bag:1001", "bag:2001", "item_101", "5")
```

### 6.2 Lua 脚本注意事项

```
限制：
  1. 不能调用非确定性函数（TIME、RANDOMKEY 等）
     → 主从复制时保证结果一致
  2. 默认 5 秒超时（最好控制在 10ms 以内）
     lua-time-limit 5000
  3. 脚本中使用的所有 key 必须在 KEYS 数组中声明
     → 方便 Redis Cluster 路由到同一个节点
  4. 不要做太久的计算 → 会阻塞其他命令

游戏场景：
  - 物品交易（扣减+增加的原子性）
  - 排行榜原子更新（ZADD + ZREMRANGEBYRANK）
  - 抽卡逻辑（扣代币+生成物品+更新统计，全部在一个 Lua 里）
```

---

## 七、Redis 时间序列实现

### 7.1 使用 Sorted Set

```python
# 使用 ZSET 实现时间序列

# 记录玩家等级变化
def record_level_history(player_id, level, timestamp=None):
    key = f"level_history:{player_id}"
    if timestamp is None:
        timestamp = time.time()
    # score = 时间戳, member = 等级值
    redis.zadd(key, {str(level): timestamp})

# 查询等级变化历史
def get_level_history(player_id, start_ts, end_ts):
    key = f"level_history:{player_id}"
    return redis.zrangebyscore(key, start_ts, end_ts,
                               withscores=True)

# 使用时间戳作为 score 的优点：
# 1. ZRANGEBYSCORE 可以按时间范围查询
# 2. ZREMRANGEBYSCORE 可以清理过期数据
# 3. 自动按时间排序
```

### 7.2 时间序列注意事项

```python
# 内存控制：每个时间点存一条记录
# 问题：频繁上报（如位置）会消耗大量内存

# 方案：聚合存储（只存变化时刻）
def record_player_level(uid, new_level):
    key = f"level:{uid}"
    last_level = redis.get(key)  # 获取旧值
    if last_level and int(last_level) == new_level:
        return  # 等级没变，不记录

    redis.set(key, new_level)

    # 记录变更日志（时间序列）
    ts_key = f"level_history:{uid}"
    redis.zadd(ts_key, {new_level: time.time()})

    # 保留最近 100 条记录，防内存溢出
    redis.zremrangebyrank(ts_key, 0, -101)
```

### 7.3 RedisTimeSeries 模块

```bash
# RedisTimeSeries 模块（官方模块，v6.0+）
# 安装：redis-cli MODULE LOAD redisearch.so

# 创建时间序列
TS.CREATE player:online:series RETENTION 86400000
  LABELS type "online" server "s1"

# 添加数据点
TS.ADD player:online:series * 12345

# 聚合查询（按小时聚合在线人数）
TS.RANGE player:online:series 0 -1
  AGGREGATION AVG 3600000

# 优势：自动降采样、内存压缩、比 ZSET 方案节省 80% 内存
```

---

## 八、热 Key 检测与解决方案

### 8.1 热 Key 的产生

```
游戏中的热 Key 场景：
  1. 世界 Boss：所有玩家同时查询 Boss 血量
  2. 全服公告：所有玩家同时拉取公告列表
  3. 排行榜：大量玩家同时查看 Top 100
  4. 活动入口：活动开启瞬间大量并发读取活动配置
```

### 8.2 热 Key 检测

```python
# 方案 1：Redis 4.0+ hotkey 检测
redis-cli --hotkeys
# 扫描所有 key 的访问频率，找出热点

# 方案 2：代理层统计（如 Codis、Twemproxy）
# 代理统计每个 key 的请求次数

# 方案 3：客户端统计
from collections import Counter
import threading

class HotKeyDetector:
    def __init__(self, threshold=100, window=10):
        self.counter = Counter()
        self.threshold = threshold  # 阈值：10 秒内 100 次
        self.window = window        # 窗口秒数
        self.lock = threading.Lock()
        self._start_cleanup()

    def record(self, key):
        with self.lock:
            self.counter[key] += 1
            if self.counter[key] > self.threshold:
                print(f"[WARN] Hot key detected: {key}")
                # 触发降级策略

    def _cleanup(self):
        while True:
            time.sleep(self.window)
            with self.lock:
                self.counter.clear()
```

### 8.3 解决方案

```
┌─────────────────────────────────────────────────────┐
│                 热 Key 解决方案武器库                  │
├─────────────────────────────────────────────────────┤
│ 1. 本地缓存（Local Cache）                            │
│    在应用服务器内存中缓存热点数据，减少 Redis 请求       │
│    方案：Caffeine（Java）、LRU Cache、Guava Cache     │
│    适用：公告、活动配置、Boss 血量                     │
├─────────────────────────────────────────────────────┤
│ 2. 读写分离（Replica Reads）                          │
│    热 Key 的读请求分散到多个从库                        │
│    方案：Redis 从节点 + 轮询/随机读                    │
│    适用：排行榜、公共数据                              │
├─────────────────────────────────────────────────────┤
│ 3. Key 打散（Key Sharding）                           │
│    一个热 Key 拆成多个子 Key，分散到不同节点            │
│    方案：hotkey:0, hotkey:1, hotkey:2 ...            │
│    适用：读多写少的静态数据                            │
├─────────────────────────────────────────────────────┤
│ 4. 请求合并（Request Coalescing）                     │
│    同一时间窗口内的相同请求合并为一次                   │
│    适用：实时性要求不高的数据                          │
├─────────────────────────────────────────────────────┤
│ 5. 限流降级                                           │
│    超出处理能力的请求直接返回默认值或空                 │
│    适用：保护缓存层不被击穿                            │
└─────────────────────────────────────────────────────┘
```

```python
# 方案 3 示例：Key 打散

# 原始热 Key
# SET world_boss_hp 1000000

# 打散为 N 个 Key
SHARD_COUNT = 128

def get_world_boss_hp():
    # 从所有分片读取并汇总
    total = 0
    for i in range(SHARD_COUNT):
        total += int(redis.get(f"boss_hp:{i}") or 0)
    return total

def set_world_boss_hp(total_hp):
    # 均分到所有分片
    per_shard = total_hp // SHARD_COUNT
    pipe = redis.pipeline()
    for i in range(SHARD_COUNT):
        pipe.set(f"boss_hp:{i}",
                 per_shard + (1 if i < total_hp % SHARD_COUNT else 0))
    pipe.execute()
```

### 8.4 缓存穿透、击穿、雪崩

```
缓存穿透（查询不存在的数据）：
  场景：恶意攻击，持续请求不存在的玩家 ID
  方案：布隆过滤器（Bloom Filter），或缓存空值（短 TTL）

缓存击穿（热点 Key 过期）：
  场景：排行榜缓存刚好过期，大量请求打到数据库
  方案：互斥锁（只让一个线程重建缓存），或永不过期+异步刷新

缓存雪崩（大量 Key 同时过期）：
  场景：所有缓存同时设置了相同的过期时间
  方案：过期时间增加随机值（base_ttl + random(0, 300)）
```

---

## 停靠点

| 知识点 | 一句话总结 | 游戏开发启示 |
|--------|-----------|--------------|
| Cache-Aside | 先更新 DB 再删缓存，读时触发回填 | 游戏中最常用的缓存模式 |
| Write-Behind | 只写缓存，异步刷库 | 位置上报、在线状态优先使用 |
| Redlock | 5 节点 + 多数同意 + 时钟校验 | 充值防重复时使用，普通业务用 SET NX |
| Cluster 槽迁移 | 16384 个槽，CRC16 分配 | 使用 hash tag 确保同玩家数据同节点 |
| MOVED/ASK | MOVED 永久重定向，ASK 迁移中临时 | 客户端 SDK 自动处理 |
| Sentinel 切换 | SDOWN → ODOWN → Leader 选举 → 切换 | 配置 3 秒检测，适应游戏敏感度 |
| Stream 消费组 | 持久化消息队列，支持 ACK 和重试 | 事件溯源、异步任务处理 |
| Lua 脚本 | 脚本内原子执行，阻塞其他命令 | 物品交易、抽卡等需要原子性的场景 |
| 时间序列 | ZSET 按时间戳排序，或用 RedisTimeSeries | 等级变化、在线人数统计 |
| 热 Key 打散 | 拆分为多个子 Key，分散到不同节点 | Boss 血量、全服公告必须处理 |


> 进阶方向：阅读 Redis 官方文档的 "Redis Cluster Specification" 和 "Redis Stream" 章节。
