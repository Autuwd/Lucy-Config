# Day 13：Redis 核心原理

## 一、Redis 是什么

Redis (Remote Dictionary Server) 是一个**内存数据结构存储系统**，常用于：
- 缓存（减少 MySQL 压力）
- 排行榜（Sorted Set）
- 分布式锁
- 消息队列
- 实时统计

### Redis 为什么快

| 原因 | 说明 |
|------|------|
| 纯内存操作 | 内存寻址 ~100ns，磁盘 ~10ms = 快 10 万倍 |
| 单线程模型 | 无锁竞争，无上下文切换 |
| IO 多路复用 | 单线程处理万级连接 |
| 数据结构高效 | 哈希表 O(1)、跳表 O(log N) |
| 编码优化 | 短字符串/小整数用 int 编码，省内存 |

### 单线程模型细节

```
Redis 6.0+ 网络 IO 用了多线程，但命令执行仍然是单线程

流程：
  Client → 网络读(多线程) → 命令排队(单线程) → 命令执行(单线程) → 网络写(多线程)

单线程的好处：
  - 所有操作原子，不需要事务锁
  - 无竞态条件
  - 调试简单

单线程的局限：
  - 单个命令不能执行太久（如 KEYS、SLOWLOG）
  - 复杂度高的命令阻塞后续所有操作
```

---

## 二、数据结构底层实现

### String

```redis
SET key "hello"
GET key

# 底层：SDS (Simple Dynamic String)
# 结构：
struct sdshdr {
    int len;        // 已用长度
    int free;       // 剩余空间
    char buf[];     // 字节数组
};
# 优点：O(1) 获取长度，二进制安全，预分配减少内存分配
```

### List

```redis
LPUSH list a b c    # [c, b, a]
RPUSH list d e        # [c, b, a, d, e]
LPOP list             # c
LRANGE list 0 -1     # [b, a, d, e]

# 底层 3.2+：quicklist (压缩链表 + 双向链表)
# 元素少时用 ziplist（连续内存，省空间）
# 元素多时切分成多个 ziplist 通过双向链表连接
```

### Hash

```redis
HSET player:1001 name "Alice"
HSET player:1001 level 10
HGETALL player:1001
# 1) "name"
# 2) "Alice"
# 3) "level"
# 4) "10"

# 底层：ziplist (小) 或 hashtable (大)
# 阈值：field 数 < 512 且 单 field 长度 < 64 字节 → ziplist
# 超过则转为 hashtable
```

### Set

```redis
SADD online:1 user:1001 user:1002 user:1003
SMEMBERS online:1
SISMEMBER online:1 user:1001  # 1 (在集合中)
SINTER set1 set2              # 交集

# 底层：intset (整数集合) 或 hashtable
# 所有元素是整数且数量 < 512 → intset
# intset 是排序的整数数组，二分查找 O(log N)
```

### ZSet (Sorted Set) — 跳表

```redis
ZADD leaderboard 1000 "PlayerA"
ZADD leaderboard 2000 "PlayerB"
ZADD leaderboard 1500 "PlayerC"
ZRANK leaderboard "PlayerA"   # 0 (排第 0)
ZREVRANK leaderboard "PlayerA" # 2 (倒排第 2)
ZRANGE leaderboard 0 -1 WITHSCORES
# 1) "PlayerA"  → 1000
# 2) "PlayerC"  → 1500
# 3) "PlayerB"  → 2000

# 底层：跳表 (skiplist) + 哈希表
# 跳表是概率平衡的链表，插入/删除 O(log N)
# 哈希表存 member→score 映射，ZSCORE O(1)
```

### 跳表原理

```
Level 4:  [1]─────────────────────→[9]
Level 3:  [1]──────────→[5]──────→[9]
Level 2:  [1]────→[3]──→[5]──→[7]→[9]
Level 1:  [1]→[2]→[3]→[5]→[6]→[7]→[9]→[10]

查询 6：
  从 Level 4 开始：1 → 9 (6 < 9, 下降)
  Level 3: 1 → 5 (6 ≥ 5, 继续) → 9 (6 < 9, 下降)
  Level 2: 5 → 7 (6 < 7, 下降)
  Level 1: 5 → 6 (找到!)
  
  每次随机决定是否上升层级（1/2 概率），
  期望查找复杂度 O(log N)
```

---

## 三、持久化

### RDB (快照)

```redis
# 配置 save 900 1     # 900 秒内至少 1 个 key 改变
        save 300 10    # 300 秒内至少 10 个 key 改变
        save 60 10000  # 60 秒内至少 10000 个 key 改变

# 手动触发
SAVE     # 阻塞，不推荐
BGSAVE   # fork 子进程后台保存（推荐）

# RDB 文件：dump.rdb

# 优点：体积小，恢复快
# 缺点：可能丢数据（最后一次 save 之后的数据）
```

### AOF (Append Only File)

```redis
# 配置 appendonly yes
# appendfsync always     # 每条命令都 fsync（最安全，最慢）
appendfsync everysec    # 每秒 fsync（折中，推荐）
# appendfsync no        # 由 OS 决定（最快，易丢数据）

# AOF 重写（压缩 AOF 文件）
BGREWRITEAOF

# 优点：最多丢 1 秒数据
# 缺点：文件大，恢复慢
```

### RDB + AOF 混合

```redis
# Redis 4.0+ 支持混合持久化
aof-use-rdb-preamble yes

# BGREWRITEAOF 时：
# 先把当前内存快照写为 RDB 格式
# 再追加增量写命令为 AOF 格式

# 恢复时：
# 加载 RDB 部分（快）
# 执行 AOF 部分（增量）
```

---

## 四、过期策略

```redis
# 设置过期时间
SET key value EX 3600      # 1 小时后过期
SETEX key 3600 value       # 同上
EXPIRE key 3600            # 对已有 key 设置
TTL key                     # 查看剩余时间

# 过期删除策略
# 1. 惰性删除：访问 key 时检查是否过期
# 2. 定期删除：每 100ms 随机抽查 20 个 key，删过期部分
#    - 如果过期 >= 25%，继续循环
#    - 最多执行 25ms，防止卡死
```

### 内存淘汰策略

```redis
# 配置 maxmemory 4gb

# 淘汰策略（8 种）：
# noeviction:      内存满时返回错误（默认）
# allkeys-lru:     淘汰最近最少使用的 key（推荐）
# volatile-lru:    淘汰设置了过期时间的 LRU key
# allkeys-random:  随机淘汰
# volatile-random: 在设置了过期时间的 key 中随机淘汰
# volatile-ttl:    淘汰快过期的 key
# allkeys-lfu:     淘汰最少频率使用的 key (Redis 4.0+)
# volatile-lfu:    在设置了过期时间的 key 中淘汰 LFU

# 游戏服务器建议：allkeys-lru
# 缓存数据：最近不用的淘汰掉
# 持久数据：不要 set 过期时间，避免被淘汰
```

---

## 五、C# 使用 Redis

### 安装与基础

```csharp
// dotnet add package StackExchange.Redis

using StackExchange.Redis;

var conn = ConnectionMultiplexer.Connect("redis:6379");
var db = conn.GetDatabase(0); // 选择第 0 个数据库

// String
await db.StringSetAsync("player:1001:name", "Alice");
string name = await db.StringGetAsync("player:1001:name");

// Hash
await db.HashSetAsync("player:1001", new HashEntry[]
{
    new("level", 10),
    new("exp", 5000)
});
var level = await db.HashGetAsync("player:1001", "level");

// List
await db.ListLeftPushAsync("queue:login", "1001");
await db.ListRightPopAsync("queue:login");

// Set
await db.SetAddAsync("online:server1", "1001");
bool isOnline = await db.SetContainsAsync("online:server1", "1001");

// Sorted Set
await db.SortedSetAddAsync("leaderboard", "Alice", 1000);
var rank = await db.SortedSetRankAsync("leaderboard", "Alice", Order.Descending);
var top10 = await db.SortedSetRangeByRankAsync("leaderboard", 0, 9, Order.Descending);

// Key 操作
await db.KeyExpireAsync("cache:data", TimeSpan.FromMinutes(5));
bool exists = await db.KeyExistsAsync("player:1001");
await db.KeyDeleteAsync("temp:data");
```

### 管道 (Pipeline)

```csharp
// 批量操作，减少网络往返
var batch = db.CreateBatch();
var task1 = batch.StringSetAsync("k1", "v1");
var task2 = batch.StringGetAsync("k2");
var task3 = batch.HashGetAsync("h1", "field1");
batch.Execute(); // 一次性发送 3 个命令

var result1 = await task1;
var result2 = await task2;
var result3 = await task3;
```

### Lua 脚本

```csharp
// 原子性：整个脚本作为一个命令执行
const string LuaIncrExp = @"
    local current = redis.call('HGET', KEYS[1], 'exp')
    if current and tonumber(current) + tonumber(ARGV[1]) >= tonumber(ARGV[2]) then
        redis.call('HSET', KEYS[1], 'exp', 0)
        redis.call('HINCRBY', KEYS[1], 'level', 1)
        return 1  -- 升级了
    else
        redis.call('HINCRBY', KEYS[1], 'exp', ARGV[1])
        return 0  -- 未升级
    end
";

var result = await db.ScriptEvaluateAsync(LuaIncrExp,
    new RedisKey[] { "player:1001" },
    new RedisValue[] { 100, 1000 });

bool leveledUp = (int)result == 1;

// 脚本缓存：Redis 缓存已执行过的脚本 SHA
```

---

## 六、对比 C++ Redis

```cpp
// C++ 用 hiredis
#include <hiredis/hiredis.h>

redisContext* c = redisConnect("127.0.0.1", 6379);

// 执行命令
redisReply* reply = (redisReply*)redisCommand(c, "SET key %s", "value");
freeReplyObject(reply);

// 获取结果
reply = (redisReply*)redisCommand(c, "GET key");
printf("GET key: %s\n", reply->str);
freeReplyObject(reply);

// 管道
redisAppendCommand(c, "SET key1 val1");
redisAppendCommand(c, "GET key2");
redisAppendCommand(c, "INCR counter");

redisReply* r1, *r2, *r3;
redisGetReply(c, (void**)&r1);
redisGetReply(c, (void**)&r2);
redisGetReply(c, (void**)&r3);

// 异步（基于 libuv/ae）
redisAsyncContext* ac = redisAsyncConnect("127.0.0.1", 6379);
redisAsyncCommand(ac, on_connect_cb, NULL, "AUTH %s", password);
```

---

## 七、练习

1. **数据结构实验**：用 redis-cli 创建 String/List/Hash/Set/ZSet，观察底层编码
2. **C# 操作 Redis**：用 StackExchange.Redis 实现玩家数据缓存（读写 Hash）
3. **管道性能**：对比 1000 次单命令写入 vs 1 次管道写入的耗时
4. **Lua 脚本**：写一个 Lua 脚本实现"扣钻石加道具"的原子操作
5. **持久化测试**：先用 BGSAVE，再写入新数据，然后 kill Redis，重启后看到什么？

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| SDS | Redis String 底层，O(1) 长度，二进制安全 |
| 跳表 | ZSet 底层，概率平衡，O(log N) 增删查 |
| ziplist | 小数据量紧凑存储，减少内存碎片 |
| 单线程 | 命令执行单线程，无需锁，但复杂命令会阻塞 |
| RDB | 快照持久化，恢复快但可能丢数据 |
| AOF | 写命令日志持久化，最多丢 1 秒数据 |
| LRU/LFU | 内存满了淘汰最近最少/最不常用数据 |
