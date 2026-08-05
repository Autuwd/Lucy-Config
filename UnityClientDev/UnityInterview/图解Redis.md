# 图解 Redis（Illustrated Redis）—— 深度学习笔记

> **作者**：小林 coding  
> **来源**：[https://xiaolincoding.com/redis/](https://xiaolincoding.com/redis/)  
> **说明**：本笔记面向**二次学习**，覆盖《图解 Redis》全部核心章节，包含：底层数据结构原理、线程模型演进、持久化机制、高可用架构、缓存设计、⚠️ 易混淆点、💡 核心理解、🔧 面试高频追问、📌 复习检查项。

---

## 目录

1. [Redis 基础](#01-redis-基础)
2. [数据类型与底层数据结构](#02-数据类型与底层数据结构)
3. [底层数据结构详解](#03-底层数据结构详解)
4. [Redis 线程模型](#04-redis-线程模型)
5. [持久化机制](#05-持久化机制)
6. [过期删除与内存淘汰](#06-过期删除与内存淘汰)
7. [主从复制](#07-主从复制)
8. [哨兵（Sentinel）](#08-哨兵sentinel)
9. [Redis Cluster（集群）](#09-redis-cluster集群)
10. [缓存设计与问题](#10-缓存设计与问题)
11. [分布式锁](#11-分布式锁)
12. [附录：面试速查表](#12-附录面试速查表)

---

## 01 Redis 基础

### 1.1 Redis 是什么？

Redis（Remote Dictionary Server）是一款开源的**内存型键值数据库**，支持多种数据结构，具备持久化、主从复制、哨兵、集群等企业级特性。

**核心特点**：
- **纯内存操作**：读写速度极快（10 万 QPS 级别），延迟 us 级
- **单线程命令执行**：避免锁竞争，简化并发模型（Redis 6.0 后 I/O 多线程）
- **丰富的数据类型**：String、Hash、List、Set、ZSet 及 BitMap、HyperLogLog、GEO、Stream
- **原子操作**：所有命令原子执行，不存在并发竞争
- **支持持久化**：RDB 快照 + AOF 日志，防止数据丢失

### 1.2 Redis vs Memcached

| 对比项 | Redis | Memcached |
|--------|-------|-----------|
| 数据类型 | 丰富（5+种） | 仅 String |
| 持久化 | ✅ RDB/AOF | ❌ |
| 事务 | ✅（弱事务） | ❌ |
| 发布订阅 | ✅ | ❌ |
| Lua 脚本 | ✅ | ❌ |
| 集群 | ✅ 原生支持 | 客户端分片 |
| 内存效率 | 略低（数据结构开销） | 高（纯 key-value）|
| 多线程 | 6.0+ I/O 多线程 | 原生多线程 |

> ⚠️ **为什么选 Redis 而不是 Memcached？**  
> 绝大多数场景（排行榜、消息队列、会话存储、地理位置等）都需要多种数据结构，Memcached 只有 String 类型根本无法满足。另外 Redis 有持久化保证，重启后数据不会全丢。

### 1.3 为什么用 Redis 作为 MySQL 的缓存？

```
客户端 ——请求——→ 业务服务器
                    │
              ┌─────┴─────┐
              ↓           ↓（缓存未命中）
           Redis         MySQL
          （内存，微秒级） （磁盘，毫秒级）
```

- 内存读写速度比磁盘快 **数万倍**，减轻 MySQL 的 QPS 压力
- 常用场景：热点数据缓存、会话存储（Session）、排行榜（ZSet）、计数器（INCR）、消息队列（List/Stream）、分布式锁

📌 **复习检查**：
- [ ] 能说出 Redis 与 Memcached 的核心差异（数据类型/持久化/集群）
- [ ] 能说清为什么 Redis 单线程命令执行可以达到高吞吐

---

## 02 数据类型与底层数据结构

### 2.1 五种基础数据类型总览

#### 底层结构对应关系（新旧版本演进）

| 数据类型 | Redis 3.0 底层结构 | Redis 7.0 底层结构 |
|---------|-----------------|-----------------|
| **String** | SDS | SDS（int/embstr/raw 编码）|
| **List** | ziplist + linkedlist | **quicklist**（3.2+）|
| **Hash** | ziplist + hashtable | **listpack + hashtable**（7.0+）|
| **Set** | intset + hashtable | **listpack + hashtable**（7.0+）|
| **ZSet** | ziplist + skiplist+dict | **listpack + skiplist+dict**（7.0+）|

> ⚠️ **重要！** Redis 7.0 已用 **listpack** 替换了压缩列表（ziplist），面试时提到"压缩列表"与"listpack"都是正确的（根据版本区分）。

---

### 2.2 String（字符串）

**应用场景**：缓存对象、计数器、分布式 Session、分布式锁、限流

**常用命令**：
```redis
SET key value [EX seconds] [NX]   # NX：不存在才设置（分布式锁核心）
GET key
MSET k1 v1 k2 v2                   # 批量设置
INCR key / INCRBY key delta        # 原子递增（计数器）
SETNX key value                    # SET if Not eXists（旧版分布式锁）
TTL key / EXPIRE key seconds       # 过期时间
```

**三种内部编码**：
- **int**：整数值且在 long 范围内，直接用整型存储
- **embstr**：字符串长度 ≤ 44 字节，SDS 与 redisObject 分配在连续内存（一次 malloc）
- **raw**：字符串长度 > 44 字节，SDS 与 redisObject 分开分配（两次 malloc）

> ⚠️ **embstr vs raw 的分界线为什么是 44 字节？**  
> `redisObject` 固定 16 字节 + SDS 头部 3 字节 + 1 字节 null终止符 = 20 字节；Redis 默认 jemalloc 分配 64 字节的内存块，64 - 20 = **44 字节**正好放完 SDS 内容部分，超出就要 raw。

---

### 2.3 List（列表）

**应用场景**：消息队列（LPUSH + BRPOP）、最新文章列表、活动日志

**常用命令**：
```redis
LPUSH key v1 v2    # 从左侧插入
RPUSH key v1 v2    # 从右侧插入
LPOP key           # 从左侧弹出
RPOP key           # 从右侧弹出
BRPOP key timeout  # 阻塞弹出（实现阻塞消息队列）
LRANGE key 0 -1    # 查看所有元素
LLEN key           # 获取长度
```

**底层：quicklist（Redis 3.2+）**

```
quicklist
  ├── node1 [listpack: e1, e2, e3 ...]
  ├── node2 [listpack: e4, e5, e6 ...]
  └── node3 [listpack: e7, e8, e9 ...]
```

- quicklist = 双向链表 + 每个节点是一个 listpack（紧凑内存）
- 兼顾了双向链表的快速增删和 listpack 的内存紧凑性
- 通过 `list-max-listpack-size` 控制每个节点 listpack 的最大大小

> ⚠️ **List 作消息队列的缺陷**：  
> 1. 消息不能持久化（进程重启丢失消费进度）  
> 2. 不支持消费组（多个消费者各自消费全量消息，而不是分担消息）  
> → 生产中推荐用 **Stream** 类型，支持消费组和全局唯一消息 ID

---

### 2.4 Hash（哈希）

**应用场景**：缓存对象（用户信息）、购物车（商品ID → 数量）

**常用命令**：
```redis
HSET key field value    # 设置字段
HGET key field          # 获取字段
HMSET key f1 v1 f2 v2   # 批量设置
HGETALL key             # 获取所有字段和值
HINCRBY key field delta # 字段原子递增
HDEL key field          # 删除字段
```

**编码切换逻辑**（Redis 7.0）：
- 元素数量 ≤ `hash-max-listpack-entries`（默认 128）且所有值长度 ≤ `hash-max-listpack-value`（默认 64 字节）→ 使用 **listpack**（内存紧凑）
- 超过任一阈值 → 转换为 **hashtable**（O(1) 查找）

> ⚠️ **Hash vs String 存对象，哪个更好？**  
> - Hash：每个字段单独操作，方便部分更新，节省内存（listpack 编码时）
> - String：存 JSON，更新时需整体读取反序列化 → 序列化修改 → 重新写入  
> → 字段经常单独访问/更新 → Hash；整体读取不分字段 → String（JSON）

---

### 2.5 Set（集合）

**应用场景**：标签系统、共同好友（SINTER）、随机抽奖（SRANDMEMBER/SPOP）、去重计数

**常用命令**：
```redis
SADD key v1 v2 v3   # 添加元素
SCARD key           # 元素数量
SISMEMBER key v     # 是否存在
SMEMBERS key        # 所有元素
SINTER k1 k2        # 交集（共同关注）
SUNION k1 k2        # 并集
SDIFF k1 k2         # 差集
SRANDMEMBER key n   # 随机取 n 个（不删除）
SPOP key            # 随机弹出一个（删除）
```

**编码切换逻辑**（Redis 7.0）：
- 全是整数且元素数量 ≤ `set-max-intset-entries`（默认 512）→ **intset**（有序整数数组，二分查找 O(log n)）
- 非纯整数但元素少 → **listpack**
- 超出阈值 → **hashtable**

---

### 2.6 ZSet（有序集合）

**应用场景**：排行榜、延迟队列（score = 执行时间戳）、带权重的消息队列

**常用命令**：
```redis
ZADD key score member          # 添加带分数的元素
ZSCORE key member              # 获取分数
ZRANK key member               # 从小到大排名（0 开始）
ZREVRANK key member            # 从大到小排名
ZRANGE key 0 -1 WITHSCORES    # 按分数从小到大获取所有元素
ZREVRANGE key 0 2              # 按分数从大到小获取前 3 名
ZRANGEBYSCORE key min max      # 按分数范围查询
ZINCRBY key delta member       # 递增分数
ZREM key member                # 删除元素
```

**编码切换逻辑**（Redis 7.0）：
- 元素数量 ≤ `zset-max-listpack-entries`（默认 128）且值长度 ≤ `zset-max-listpack-value`（默认 64 字节）→ **listpack**
- 超出阈值 → **skiplist + hashtable**（双数据结构冗余存储！）

> ⚠️ **ZSet 为什么同时用跳表和哈希表？**  
> - **哈希表（dict）**：O(1) 时间查询 member → score（如 ZSCORE 命令）
> - **跳表（skiplist）**：O(log N) 时间按 score 排序、范围查询（如 ZRANGE 命令）
> - 两者内存共享 member 字符串（引用），并非两份拷贝，内存开销可控

---

### 2.7 四种特殊数据类型

#### BitMap（位图）
- 本质是 **String 类型**，按二进制位操作
- 应用：用户签到（用位偏移量表示天数）、在线用户统计（bitcount）
```redis
SETBIT key offset 1      # 设置第 offset 位为 1
GETBIT key offset        # 获取第 offset 位的值
BITCOUNT key             # 统计值为 1 的位数（签到天数）
BITOP AND/OR dest k1 k2  # 位运算（统计连续签到）
```

#### HyperLogLog（基数统计）
- 用于**大规模去重计数**，误差约 0.81%，固定占用约 **12 KB** 内存
- 应用：UV（独立访客数）统计，网站 PV 去重
```redis
PFADD key e1 e2 e3    # 添加元素
PFCOUNT key           # 估算基数
PFMERGE dest k1 k2    # 合并多个 HLL
```

> ⚠️ **HyperLogLog vs Set 统计基数**：  
> - Set：精确，但百亿元素需要几十 GB 内存  
> - HyperLogLog：误差 < 1%，固定 12 KB，适用于不需要精确值的超大规模 UV 统计

#### GEO（地理位置）
- 基于 **ZSet** 实现，将经纬度编码为 geohash 字符串作为 score
- 应用：附近的人（GEORADIUS）、滴滴叫车、外卖距离排序
```redis
GEOADD key longitude latitude member    # 添加坐标
GEODIST key m1 m2 [unit]               # 两点距离
GEORADIUS key lon lat radius unit      # 圆形范围查找
GEOPOS key member                      # 获取坐标
```

#### Stream（流）
- Redis 5.0 新增，专为**消息队列**设计
- 核心优势：自动生成全局唯一消息 ID、消费组（Consumer Group）、消息确认（ACK）、未确认消息可重新消费
```redis
XADD stream * field value              # 生产消息（* 表示自动生成 ID）
XREAD COUNT 10 BLOCK 0 STREAMS s1 $   # 消费（阻塞）
XGROUP CREATE s1 group1 0             # 创建消费组
XREADGROUP GROUP g1 consumer1 COUNT 1 STREAMS s1 >   # 消费组消费
XACK s1 g1 msgid                      # 确认消息
XPENDING s1 g1 - + 10                 # 查看未确认消息
```

---

## 03 底层数据结构详解

### 3.1 SDS（Simple Dynamic String，简单动态字符串）

Redis 自己实现的字符串结构，代替 C 语言原生 `char*`：

```c
struct sdshdr {
    int len;      // 已使用的字节数（O(1) 获取长度）
    int alloc;    // 分配的总字节数
    char flags;   // 编码类型（sdshdr5/8/16/32/64）
    char buf[];   // 字节数组
};
```

**SDS 的四大优势**：

| 优势 | 说明 |
|------|------|
| **O(1) 获取长度** | `len` 字段直接记录，C 字符串需 O(n) 遍历 |
| **二进制安全** | `len` 字段判断结尾，C 字符串用 `\0` 判断（无法存含 `\0` 的二进制数据）|
| **杜绝缓冲区溢出** | 修改前先检查/扩容，C 字符串 `strcat` 不检查长度 |
| **内存预分配 + 惰性释放** | 扩容时多分配空间；缩短时不立即释放，避免频繁 malloc/free |

> 💡 **记忆要点**：SDS = 长度字段 + 预分配 + 二进制安全，这三点解决了 C 字符串所有致命缺陷。

---

### 3.2 链表（双向链表）

Redis 3.2 前用于 List，现已被 quicklist 取代（仍在 Pub/Sub、LRU 等内部使用）：

```
list
 head ─→ [prev|val|next] ←→ [prev|val|next] ←→ [prev|val|next] ←─ tail
```

**特点**：无环双向链表，O(1) 头尾插入删除，O(n) 访问中间元素，每个节点独立 malloc（内存不连续，碎片化）。

---

### 3.3 压缩列表（ziplist）与 listpack

#### ziplist（旧，Redis < 7.0）

```
[zlbytes | zltail | zllen | entry1 | entry2 | ... | zlend(0xFF)]
entry = [prevlen | encoding | data]
```

- `prevlen`：记录前一个节点长度，支持**逆向遍历**
- **致命缺陷：连锁更新（Cascade Update）**  
  → 若某节点 data 增大使 prevlen 从 1 字节变为 5 字节，下一个节点的 prevlen 字段也需更新 → 可能级联触发后续所有节点更新 → **最坏 O(n²)**

#### listpack（Redis 7.0 替代 ziplist）

```
[total-bytes | num-elements | entry1 | entry2 | ... | end(0xFF)]
entry = [encoding-type | data | backlen]
```

- `backlen`：记录**当前节点**的长度（而非前一节点），逆向遍历通过 backlen 计算前节点起始位置
- **关键改进**：不依赖前一节点长度，**彻底消除了连锁更新问题**

> ⚠️ **面试易混淆**：
> - ziplist：`prevlen` 记录前一节点长度 → 连锁更新
> - listpack：`backlen` 记录当前节点长度 → 无连锁更新

---

### 3.4 哈希表（dict / hashtable）

Redis 哈希表采用**链式寻址**解决哈希冲突：

```c
typedef struct dictht {
    dictEntry **table;     // 哈希桶数组
    unsigned long size;    // 桶数量（2^n）
    unsigned long used;    // 已使用节点数
} dictht;

typedef struct dict {
    dictht ht[2];          // 两张哈希表（用于 rehash）
    long rehashidx;        // -1 表示没有在 rehash
} dict;
```

#### 渐进式 rehash（核心机制！）

**触发条件**：
- 负载因子（used / size）≥ 1 且没有执行 bgsave/bgrewriteaof → 扩容（新 size = 2 倍）
- 负载因子 ≥ 5（强制扩容，即使在持久化）
- 负载因子 ≤ 0.1 → 缩容

**渐进式 rehash 流程**：
1. 分配 `ht[1]`（新哈希表，大小为 `ht[0].used` × 2 的下一个 2^n）
2. `rehashidx = 0`，标记开始 rehash
3. **每次读写操作**（CRUD），顺带将 `ht[0]` 中 `rehashidx` 索引的桶迁移到 `ht[1]`，然后 `rehashidx++`
4. 全部迁移完毕后，`ht[1]` 成为新的 `ht[0]`，`rehashidx = -1`

**渐进式期间的读写规则**：
- **读**：先查 `ht[0]`，未命中再查 `ht[1]`
- **写（新增）**：只写入 `ht[1]`，保证 `ht[0]` 只减不增，加速迁移完成

> ⚠️ **为什么要"渐进式"而不是一次性 rehash？**  
> 一次性 rehash 在数据量大时会阻塞 Redis 主线程数百毫秒，造成卡顿。渐进式将开销分摊到每次请求，单次增量极小，对客户端无感知。

---

### 3.5 跳表（skiplist）

跳表是 Redis ZSet 的核心排序数据结构，面试高频！

**跳表结构**（多层索引链表）：

```
Level 3:  ──1──────────────────────→9
Level 2:  ──1──────→3──────────────→9
Level 1:  ──1────→2─→3────→5────→8→9
Level 0:  ──1─→2─→3─→4─→5─→6─→8─→9  （底层原始链表）
```

- 每个节点随机生成层数（最高 64 层），平均层数 log(n)
- 查找时从最高层开始，快速跳过大段数据，平均复杂度 **O(log N)**
- 插入/删除也是 **O(log N)**（需找到前驱节点更新指针）

**Redis 跳表节点结构**：
```c
typedef struct zskiplistNode {
    sds ele;             // 成员（字符串）
    double score;        // 分数
    struct zskiplistNode *backward;  // 后退指针（支持逆向遍历）
    struct zskiplistLevel {
        struct zskiplistNode *forward;  // 前进指针
        unsigned long span;              // 跨度（用于计算 rank）
    } level[];           // 柔性数组，层数随机
} zskiplistNode;
```

#### 为什么 ZSet 用跳表而不用平衡树（AVL/红黑树）？

| 对比维度 | 跳表 | 平衡树（红黑树） |
|---------|------|--------------|
| 实现复杂度 | 简单，代码量少 | 旋转操作复杂 |
| 范围查询 | ✅ O(log N) 定位后直接顺序遍历 | ❌ 中序遍历，需额外操作 |
| 内存占用 | 略高（指针多） | 略低 |
| 锁粒度（并发） | 跳表锁粒度更小（链表节点级） | 树旋转影响多个节点 |

> 💡 **核心记忆**：跳表的 ZRANGE（范围查询）是 Redis ZSet 最常用操作，跳表在范围遍历时**优于平衡树**。这是 Redis 选跳表的最主要理由。

---

## 04 Redis 线程模型

### 4.1 Redis 为什么用单线程？

Redis 命令执行（读写内存 + 数据结构操作）是**单线程**的，原因：
1. **避免锁竞争**：多线程访问内存数据结构需要加锁，增加实现复杂度和上下文切换开销
2. **内存操作极快**：CPU 不是瓶颈（10 万 QPS 时 CPU 利用率很低），网络 I/O 才是瓶颈
3. **简化代码**：单线程逻辑简单，Bug 少

> ⚠️ **"Redis 是单线程"这句话是不准确的！**  
> Redis **命令执行**（网络请求处理 + 键值操作）是单线程，但 Redis **整体进程**是多线程的：
> - 主线程：处理网络 I/O + 执行命令（核心）
> - **bgsave 子进程**：RDB 持久化
> - **bgrewriteaof 子进程**：AOF 重写
> - **后台线程（4.0+）**：异步释放大 key（lazyfree）、AOF 刷盘、关闭文件描述符

### 4.2 Redis 6.0 引入 I/O 多线程

**背景**：网络硬件越来越快，网络 I/O（解析请求、发送响应）成为瓶颈

**多线程处理网络 I/O**：
```
多个 I/O 线程：读 socket → 解析请求 → 放入任务队列
主线程（单线程）：顺序执行命令（保证原子性）
多个 I/O 线程：写 socket → 发送响应
```

- **命令执行仍是单线程**，多线程只用于 I/O 读写（不影响数据一致性）
- 配置：`io-threads 4`，`io-threads-do-reads yes`
- 实测提升约 1~2 倍吞吐量

### 4.3 Redis 的事件驱动模型

Redis 使用 **I/O 多路复用**（epoll/kqueue）监听大量 Socket：

```
epoll 等待事件 → 分发给事件处理器 → 文件事件（网络I/O）+ 时间事件（定时任务）
```

- **文件事件**：处理客户端 socket 的读写事件
- **时间事件**：ServerCron 定时任务（100ms 周期），执行：过期键检测、持久化触发、主从心跳等

### 4.4 大 Key 问题与解决

**大 Key 定义**：String 类型 value > 10 KB；Hash/List/Set/ZSet 元素数量 > 10000

**大 Key 危害**：
- 主线程执行删除（DEL）阻塞 → 命令延迟飙升
- 主从复制时传输 RDB 时间长
- 内存碎片化严重

**解决方案**：
- 用 `UNLINK`（异步删除）代替 `DEL`（同步删除）→ 4.0+ lazyfree 后台线程处理
- 生产前拆分大 Key（Hash 可按 ID 分桶存储）
- 用 `SCAN` 扫描找出大 Key（`redis-cli --bigkeys`）

---

## 05 持久化机制

### 5.1 RDB（Redis Database，内存快照）

**原理**：将 Redis **某一时刻的内存数据**全部快照到 `.rdb` 文件中（二进制格式，极度紧凑）。

**触发方式**：
- `SAVE`：主线程**阻塞**执行，生产禁用！
- **`BGSAVE`**：fork 出子进程，子进程执行快照，主线程继续响应请求
- 自动触发：`save 900 1`（900秒内有1次修改）、`save 300 10`、`save 60 10000`

**BGSAVE 的写时复制（Copy-On-Write，COW）**：

```
主进程 ──fork()──→ 子进程（共享主进程内存页面表，页面设置为只读）
                     │
          [快照期间主进程接到写请求]
                     │
          主进程：复制被修改的内存页 → 在副本上写入（新数据）
          子进程：仍看到 fork 时的旧页面（快照数据正确）
```

- COW 保证子进程看到的是 fork 时刻的内存快照，同时主进程可以继续写入
- **极端情况**：若快照期间大量写入，会导致内存使用翻倍

**RDB 优缺点**：

| 优点 | 缺点 |
|------|------|
| 文件紧凑，恢复速度快（直接加载内存镜像）| **数据丢失风险**：两次快照之间的写入全部丢失 |
| fork 后对主线程无阻塞 | fork 本身可能耗时（内存越大越慢，可能 100ms+）|
| 适合全量备份/灾难恢复 | 不适合追求数据完整性的场景 |

---

### 5.2 AOF（Append-Only File，追加写日志）

**原理**：将每条**写命令**以文本格式追加到 `.aof` 文件中，重启时回放所有命令恢复数据。

**AOF 写入流程**：
```
客户端写命令
    ↓
Redis 执行命令（先执行！）
    ↓
将命令写入 AOF 缓冲区（server.aof_buf）
    ↓
按 fsync 策略刷入磁盘
```

> ⚠️ **AOF 是先执行命令，再写日志**（与 WAL 相反）。  
> 优点：不对命令做语法检查；写日志不阻塞当前命令。  
> 缺点：执行后宕机，日志未写入，数据丢失。

**三种 fsync 策略**（`appendfsync` 配置）：

| 策略 | 行为 | 数据安全 | 性能 |
|------|------|---------|------|
| `always` | 每条命令立即 fsync | 最安全（最多丢 1 条）| 最差（大量磁盘 I/O）|
| **`everysec`（默认）** | 每秒 fsync 一次（后台线程）| 最多丢 1 秒数据 | 均衡 |
| `no` | 由 OS 决定何时 fsync | 最差（OS 崩溃可能丢几秒）| 最好 |

#### AOF 重写（bgrewriteaof）

**问题**：AOF 文件越写越大（一个 key 经过 100 次修改会有 100 条命令）

**重写原理**：
1. fork 子进程，子进程遍历内存中所有键值，生成最小化命令集（如 `SET k v` 代替 100 条 INCR）
2. 主进程在重写期间的新写命令同时写入：**AOF 重写缓冲区**（server.aof_rewrite_buf_blocks）
3. 子进程完成重写后，主进程将重写缓冲区的增量命令追加到新 AOF 文件，替换旧文件

```
fork 子进程 → 子进程读内存生成新 AOF
同时 → 主进程新写命令 → [AOF 缓冲区] + [AOF 重写缓冲区]
子进程完成 → 主进程将重写缓冲区追加到新文件 → 原子替换旧 AOF 文件
```

> ⚠️ **两个缓冲区的区别**：
> - `aof_buf`：正常写命令缓冲，刷到**现有** AOF 文件（重写期间继续运作）
> - `aof_rewrite_buf`：重写期间新命令的专属缓冲，重写完成后追加到**新** AOF 文件
> 这样设计保证了无论重写是否成功，当前 AOF 文件始终是完整的。

**AOF 优缺点**：

| 优点 | 缺点 |
|------|------|
| 数据更安全（everysec 最多丢 1 秒）| 文件比 RDB 大（文本格式）|
| 可读性强（文本命令）| 恢复速度慢（需回放所有命令）|
| 增量追加，无大文件读写 | 高写入负载下 I/O 压力大 |

---

### 5.3 混合持久化（Redis 4.0+）

**原理**：AOF 文件的**开头**不再是命令文本，而是一个 **RDB 快照**，后面跟着**增量 AOF 命令**：

```
[RDB 格式的全量快照] + [重写后增量的 AOF 命令]
```

**优势**：
- 恢复时先加载 RDB（快速），再回放少量增量 AOF 命令（完整）
- 文件比纯 AOF 小，恢复比纯 RDB 少丢数据

**配置**：`aof-use-rdb-preamble yes`（Redis 4.0+ 默认开启）

> ⚠️ **重启时优先级**：若同时开启 AOF 和 RDB，**优先加载 AOF 文件**（因为 AOF 数据更完整）。

### 5.4 持久化方案选择

| 场景 | 推荐方案 |
|------|---------|
| 追求极致性能，容忍数据丢失 | 关闭持久化 |
| 允许丢少量数据（如缓存） | RDB（定期快照）|
| 不能丢失数据（如订单缓存） | AOF everysec |
| 兼顾恢复速度和数据安全 | **混合持久化（推荐）**|

📌 **复习检查**：
- [ ] 能说清 bgsave 的 fork + COW 机制
- [ ] 能说出 AOF 三种 fsync 策略的数据安全级别
- [ ] 能说清 AOF 重写的两个缓冲区各自的作用
- [ ] 知道混合持久化文件格式：RDB 头 + AOF 增量

---

## 06 过期删除与内存淘汰

### 6.1 过期键如何存储？

Redis 用一张独立的哈希表（`expires dict`）存储键的过期时间：
```
key → 过期时间戳（Unix timestamp，毫秒精度）
```

### 6.2 过期删除策略

#### 惰性删除（Lazy Expiration）
- **时机**：每次**访问**一个 key 时，检查是否已过期，若过期则立即删除
- **优点**：对 CPU 友好（不主动扫描）
- **缺点**：过期 key 不被访问则永远不会删除 → **内存泄漏**！

#### 定期删除（Active Expiration）
- **时机**：Redis 每隔 100ms，在 `expires dict` 中**随机采样** 20 个 key，删除其中已过期的
- 若删除比例超过 25%，立即再次采样（直到比例 < 25% 或时间片耗尽）
- **优点**：防止惰性删除造成的内存积压
- **缺点**：可能删除不及时（仍有漏网之鱼）

> 💡 Redis 选用「**惰性删除 + 定期删除**」组合，以求在 CPU 开销和内存浪费之间取得平衡。

---

### 6.3 内存淘汰策略（当内存满时）

配置项：`maxmemory-policy`，共 **8 种**策略：

**针对设置了过期时间的 key（volatile-*）**：

| 策略 | 说明 |
|------|------|
| `volatile-lru` | 最近最少使用的 key（近似 LRU）|
| `volatile-lfu` | 最不常使用的 key（近似 LFU，4.0+）|
| `volatile-random` | 随机淘汰 |
| `volatile-ttl` | TTL 最短的 key（快要过期的优先淘汰）|

**针对所有 key（allkeys-*）**：

| 策略 | 说明 |
|------|------|
| `allkeys-lru` | **最常用推荐**，全局近似 LRU |
| `allkeys-lfu` | 全局近似 LFU（4.0+）|
| `allkeys-random` | 全局随机 |
| `noeviction` | **默认**，不淘汰，内存满时写操作返回错误 |

#### Redis 的近似 LRU 实现

Redis 不维护完整的 LRU 链表（代价太高），而是在每个 `redisObject` 中记录 **24 位的 LRU 时间戳**（精度秒级）。

淘汰时：随机采样 N 个（默认 5 个）key，选其中 LRU 时间最旧的淘汰。

#### Redis 的 LFU 实现

- `redisObject` 的 24 位：高 16 位 = 上次访问的分钟时间戳；低 8 位 = 访问频率计数（**Morris Counter，对数计数器**）
- Morris Counter：频率越高，每次访问使计数器 +1 的概率越低（防止短期爆款永远驻留）
- 定期衰减：每次访问时，根据"距上次访问的时间"减少计数（防止历史热点永远不被淘汰）

> ⚠️ **LRU vs LFU**：
> - LRU：最近最少使用，关注**时间维度**，适合热点数据时效性强的场景
> - LFU：最不常使用，关注**频率维度**，适合热点数据相对稳定的场景（排行榜等）

---

## 07 主从复制

### 7.1 为什么需要主从复制？

- **读写分离**：主节点处理写请求，从节点处理读请求，提升 QPS
- **数据备份**：从节点是主节点的实时备份
- **高可用基础**：主节点宕机时，哨兵可以将从节点提升为新主节点

### 7.2 全量同步（第一次建立复制）

```
从节点                              主节点
   |—— PSYNC ? -1 ───────────────→|   （? = 不知道主节点runID, -1 = 首次同步）
   |                                |
   |← FULLRESYNC <runID> <offset>  |   主节点: 执行 bgsave 生成 RDB
   |                                |
   |← [RDB 文件] ─────────────────|   传输 RDB
   |                                |
   |  [清空从节点数据，载入 RDB]     |
   |                                |
   |← [replication buffer 增量命令]|   传输期间主节点新收到的写命令
   |                                |
   |  [执行增量命令]                |
   |≡≡≡ 主从数据一致 ≡≡≡≡≡≡≡≡≡≡≡≡|
```

**关键字段**：
- **runID**：主节点的随机 ID，每次重启会改变。从节点用它判断是否连的是同一个主节点
- **offset（复制进度）**：主节点已发送的字节偏移量，从节点记录已接收的偏移量

### 7.3 增量同步（断线重连后）

```
从节点断线重连后，发送 PSYNC <runID> <offset>
主节点检查：
  ├── runID 匹配 + offset 在 repl_backlog_buffer 范围内 → 增量同步（发送差异命令）
  └── runID 不匹配 OR offset 已被覆盖 → 全量同步
```

**repl_backlog_buffer（复制积压缓冲区）**：
- 一个**循环写入**的固定大小缓冲区（默认 1 MB），记录主节点最新的写命令
- 从节点断线时间越长，offset 越可能超出范围，导致全量同步
- **调优**：`repl-backlog-size` = 主节点每秒写入量 × 断线时间估计 × 2

### 7.4 两个 Buffer 的区别（重要！）

| 名称 | 类型 | 作用 | 数量 |
|------|------|------|------|
| **replication buffer** | 每个从节点独立 | 传输 RDB 期间和正常同步时缓存要发给该从节点的命令 | 每从节点一个 |
| **repl_backlog_buffer** | 全局共享 | 固定大小循环缓冲，记录最近一段时间所有写命令，供断线重连时增量同步用 | 一个主节点只有一个 |

> ⚠️ **二者混淆是面试常见失误！** 前者是给特定从节点的命令队列，后者是全局环形缓冲做增量同步的依据。

---

## 08 哨兵（Sentinel）

### 8.1 哨兵的三大功能

1. **监控（Monitoring）**：持续检查主从节点是否正常（每秒 PING）
2. **通知（Notification）**：发现故障时通知管理员或其他应用
3. **自动故障转移（Auto Failover）**：主节点宕机时，自动选出新主节点，并通知客户端

### 8.2 主观下线 vs 客观下线

**主观下线（SDOWN，Subjectively Down）**：
- 单个 Sentinel 在 `down-after-milliseconds` 毫秒内没有收到主节点的有效回复 → 标记为主观下线
- 可能是假故障（网络抖动）

**客观下线（ODOWN，Objectively Down）**：
- Sentinel 向其他 Sentinel 询问"你认为主节点下线了吗？"（`SENTINEL is-master-down-by-addr`）
- 得到 **≥ quorum**（法定人数，配置项）个 Sentinel 认为主节点下线 → 标记为客观下线
- **只有主节点有 ODOWN**（从节点/Sentinel 只有 SDOWN）

### 8.3 领导者 Sentinel 选举（Raft 变种）

确认 ODOWN 后，多个 Sentinel 需选出**一个领导者**来执行故障转移（避免多人同时操作）：

1. 每个 Sentinel 向其他 Sentinel 发送 `SENTINEL is-master-down-by-addr`，同时附带自己的 `runID` 请求支持
2. 每个 Sentinel 在一轮选举中只能支持一个（先到先得）
3. 获得 **max(quorum, (Sentinel数量/2)+1)** 票的 Sentinel 成为领导者

### 8.4 故障转移流程

**选出新主节点的规则**（按优先级排序）：
1. 过滤掉：5 秒内无心跳的从节点、与主节点断线时间超过 `down-after-milliseconds × 10` 的从节点
2. 优先选 **replica-priority 最小**（数值越小优先级越高，0 表示不参与选举）
3. priority 相同 → 选**复制进度（offset）最大**的（数据最接近主节点）
4. offset 相同 → 选 **runID 字典序最小**的（随机决定）

**通知客户端**：Sentinel 更新配置，发布 `+switch-master` 事件，客户端重新连接新主节点。

> ⚠️ **哨兵无法解决数据容量问题（Scale Out）**，只解决高可用（HA）问题。若要水平扩容，需要 Redis Cluster。

---

## 09 Redis Cluster（集群）

### 9.1 为什么需要 Cluster？

- **单机容量限制**：单台 Redis 内存有限（几十 GB）
- **单点 QPS 瓶颈**：哨兵仍然是单点写入，海量写入场景无法满足
- **Cluster 目标**：数据自动分片到多个节点，实现**水平扩展**

### 9.2 哈希槽（Hash Slot）

Redis Cluster 将 key 空间划分为 **16384 个哈希槽（Hash Slot）**（0 ~ 16383）。

**槽位分配**：
```
HASH_SLOT = CRC16(key) % 16384
```

每个集群节点负责一部分槽：
```
节点 A: 0 ~ 5460
节点 B: 5461 ~ 10922
节点 C: 10923 ~ 16383
```

> ⚠️ **为什么是 16384 个槽而不是更多？**  
> - 16384 个槽的集群心跳消息 bitmap 只需 16384/8 = **2KB**（gossip 消息头部需携带槽位信息）
> - Redis 作者认为超过 1000 个节点的集群不现实，16384 个槽足够分配
> - CRC16 最大值 65535，但更大的槽数会导致心跳包过大

### 9.3 重定向（MOVED 和 ASK）

客户端请求的 key 可能不在当前节点：

**MOVED 重定向（永久）**：
```
客户端: GET foo → 节点 A
节点 A: MOVED 1234 节点B:6379   （槽 1234 在节点 B，永久告知）
客户端: GET foo → 节点 B（并更新本地槽位缓存）
```

**ASK 重定向（迁移中临时）**：
```
迁移期间，槽 1234 正在从节点 A 迁移到节点 B（部分 key 已迁，部分未迁）
客户端: GET foo → 节点 A（槽未完全迁移到 B）
节点 A: ASK 1234 节点B:6379    （仅这一次去 B 查，不更新客户端缓存）
客户端: ASKING → GET foo → 节点 B
```

> ⚠️ **MOVED vs ASK**：
> - MOVED：槽已完全迁移到目标节点，客户端**永久更新**本地槽位表
> - ASK：槽迁移进行中（临时），客户端**仅本次**去目标节点查，不更新槽位表

### 9.4 Cluster 故障转移

- 每个节点有自己的主从（每个分片独立的主从）
- 节点间通过 **gossip 协议**互相交换状态（PING/PONG 消息）
- 故障检测：
  - 一个节点无法联系目标节点 → 标记 **PFAIL（probable fail）**
  - 超过半数主节点认为其 PFAIL → 标记 **FAIL**
- 故障转移：该分片的从节点触发选举，获得多数节点同意后提升为新主节点

### 9.5 集群局限性

1. **多 key 命令限制**：MSET/MGET、SUNION 等跨 key 命令只能在同一个槽的 key 上执行
2. **事务跨槽不支持**：MULTI/EXEC 中多个 key 必须在同一槽
3. **批量操作**：Pipeline 中所有命令的 key 必须在同一节点（可用 Hash Tag `{tag}` 强制同槽）
4. **最少 3 主节点**：集群需要 ≥ 3 个主节点

**Hash Tag**（解决 key 分散问题）：
```redis
SET {user:1}:name "Alice"
SET {user:1}:age  30
# CRC16 只计算 {} 内的部分（user:1），保证这两个 key 在同一槽
```

---

## 10 缓存设计与问题

### 10.1 缓存雪崩（Cache Avalanche）

**定义**：大量缓存 key **同时过期** 或 **Redis 宕机**，导致所有请求打到数据库，引发数据库崩溃。

**两种场景**：

**场景 1：大量 key 同时过期**

| 解决方案 | 说明 |
|---------|------|
| **TTL 加随机值** | `EXPIRE key (基础TTL + random(0, 300))`，避免集中到期 |
| **互斥锁** | 缓存失效时加锁，只允许一个请求重建缓存，其余等待 |
| **后台线程异步更新** | 缓存不设 TTL，由后台线程定时刷新（需处理淘汰后的空窗期）|
| **多级缓存** | 本地缓存（Caffeine）+ Redis，Redis 故障时本地缓存兜底 |

**场景 2：Redis 宕机**

| 解决方案 | 说明 |
|---------|------|
| **服务熔断** | 暂停对缓存和数据库的访问，直接返回错误（牺牲可用性保护 DB）|
| **请求限流** | 只允许少量请求通过到达数据库 |
| **Redis 高可用集群** | 主从 + 哨兵 / Cluster，避免单点故障 |

---

### 10.2 缓存击穿（Cache Breakdown / Hotspot Invalid）

**定义**：**某个热点 key** 缓存过期，瞬间大量请求同时穿过缓存打到数据库。

**与雪崩的区别**：雪崩是大量 key 同时失效，击穿是**单个热点 key** 失效。

**解决方案**：

**方案 1：互斥锁（Mutex Lock）**
```
if Redis.get(key) == null:
    if lock.tryLock():              # 只有一个请求能加锁
        data = DB.query(key)
        Redis.set(key, data, TTL)
        lock.unlock()
    else:
        sleep(50ms) 然后重试 Redis.get(key)  # 等待锁释放后读缓存
```
- 优点：保证数据一致性
- 缺点：加锁期间其他请求等待或返回 null，性能有损

**方案 2：逻辑过期（Logical Expiry）**
```
Value 中除了数据本身，还存储一个 "逻辑过期时间"：
{data: "...", expire_time: 1690000000}
```
- 读取时发现已逻辑过期 → 异步提交重建任务 → 本次**返回旧数据**（不阻塞）
- 优点：不阻塞请求，性能好
- 缺点：短暂返回旧数据（弱一致性），适合对一致性要求不高的热点数据

> ⚠️ **热点 key 的终极解决方案**：对**超级热点 key**（如秒杀商品详情），可以在**本地内存（JVM 堆）**中缓存，彻底不走 Redis，支撑极限 QPS。但需注意本地缓存的一致性更新问题。

---

### 10.3 缓存穿透（Cache Penetration）

**定义**：查询的数据**既不在 Redis 中，也不在数据库中**（如攻击者用不存在的 ID 刷接口），导致每次都要查数据库。

**与击穿/雪崩的区别**：击穿和雪崩的数据存在于 DB，只是缓存失效；穿透的数据根本不存在。

**解决方案**：

**方案 1：缓存空值**
```
data = DB.query(id)
if data == null:
    Redis.set(key, "NULL", TTL=5min)   # 缓存空值，防止下次再查 DB
```
- 优点：简单
- 缺点：恶意攻击者使用不同的不存在 key → 缓存大量空值，浪费内存

**方案 2：布隆过滤器（Bloom Filter）**

布隆过滤器结构：**位图数组（初始全 0）** + **N 个哈希函数**

**写入数据时**：对 key 计算 N 次哈希，将对应位图位置设为 1  
**查询时**：计算 N 次哈希，若所有对应位都是 1 → **可能存在**；若任一位为 0 → **一定不存在**

```
位图: [0,0,0,0,0,0,0,0]
写入 key="user:1"，3个哈希函数结果: 1,4,6
位图: [0,1,0,0,1,0,1,0]

查询 key="user:999"，3个哈希函数结果: 1,3,6
位图第3位是0 → 不存在 → 直接返回，不查DB ✅
```

> ⚠️ **布隆过滤器的误判**：布隆过滤器可能将不存在的 key 判断为"可能存在"（哈希碰撞），但不会将存在的 key 判断为"不存在"。  
> 即：**查到存在 → 不一定真存在；查到不存在 → 一定不存在**。

---

### 10.4 数据库与缓存双写一致性

**核心问题**：更新数据时，如何保证 Redis 缓存和 MySQL 数据库的数据一致？

#### 四种方案对比

**方案 A：先更新缓存，再更新数据库** ❌
- 并发写时：A 先更新缓存为 1，B 更新缓存为 2，B 更新数据库为 2，A 更新数据库为 1
- 结果：DB=1（旧），Cache=2（新）→ **不一致**

**方案 B：先更新数据库，再更新缓存** ❌
- 并发写时：A 更新 DB 为 1，B 更新 DB 为 2 并更新 Cache 为 2，A 更新 Cache 为 1
- 结果：DB=2，Cache=1 → **不一致**
- 另外：缓存频繁更新但不一定被读到（写多读少场景浪费）

**方案 C：先删除缓存，再更新数据库** ❌
- 删 Cache → 读请求来，Cache Miss → 读 DB（旧值）→ 写入 Cache → 更新 DB（新值）
- 结果：Cache=旧值，DB=新值 → **不一致**（读写并发时）

**方案 D：先更新数据库，再删除缓存** ✅（推荐）
```
1. 更新 DB（新值）
2. 删除 Cache（让下次读时重建）
```
- 最常见不一致场景（可能性极低）：  
  ① Cache Miss，读 DB 得旧值（DB 正好在更新前）  
  ② 另一请求更新 DB 并删除 Cache  
  ③ ① 再把旧值写入 Cache  
  → 需要 ③ 在 ② 之后才会有问题，而 "读DB + 写Cache" 通常远快于"写DB"，概率极低

> ⚠️ **方案 D 的致命弱点：删除 Cache 失败！**  
> 解决：**重试机制** + **消息队列**（删除失败放入 MQ，消费端异步重试）  
> 更优雅的方案：**订阅 MySQL binlog**（Canal 伪装成从节点，监听 binlog）→ 异步删除 Cache

#### 延迟双删（Double Delete）

```
1. 先删除 Cache
2. 更新 DB
3. sleep(N ms)       # 等待可能的读操作完成
4. 再删除 Cache（消除步骤 1-2 期间写入的旧缓存）
```

- sleep 时间要大于"读 DB + 写 Cache"的时间（通常几十毫秒）
- 步骤 4 的再次删除可异步执行（放入后台线程）

#### Canal 订阅 binlog 方案（最优雅）

```
MySQL → binlog → Canal（伪装成从节点）→ 解析 binlog → 发送 MQ → 消费者删除/更新 Cache
```
- 与业务代码解耦
- 即使业务代码崩溃，binlog 变更也不会丢失
- 适合对一致性要求较高且业务复杂的系统

---

### 10.5 如何处理热点 Key 问题

| 方案 | 说明 | 适用场景 |
|------|------|---------|
| 本地缓存 | JVM 堆内缓存，完全不走 Redis | 超高 QPS（秒杀）|
| 读写分离 | 热点 key 多从节点读 | 读多写少 |
| 多副本分散 | 将热点 key 拆分为 key#0~key#N，分散到多个节点 | 极端热点 |
| 逻辑过期 | 返回旧值，后台异步更新 | 允许短暂不一致 |

---

## 11 分布式锁

### 11.1 Redis 分布式锁的基本实现

**核心命令**：
```redis
SET lock_key unique_value NX EX 30
# NX：不存在才设置（互斥）
# EX 30：30 秒后自动过期（防止死锁）
```

**释放锁**（必须用 Lua 脚本保证原子性！）：
```lua
-- 验证是自己的锁才删除，防止误删他人的锁
if redis.call("GET", KEYS[1]) == ARGV[1] then
    return redis.call("DEL", KEYS[1])
else
    return 0
end
```

> ⚠️ **为什么释放锁必须用 Lua 脚本？**  
> GET + DEL 是两步操作，非原子。若 GET 成功后锁恰好过期（被其他客户端重新加锁），再执行 DEL 会误删他人的锁。Lua 脚本在 Redis 中原子执行，保证 GET 和 DEL 之间不会被打断。

### 11.2 常见问题与解决

**问题 1：锁过期后业务还没执行完（锁续期）**
- 解决：**看门狗（Watchdog）机制**（Redisson 实现）
- 默认锁过期 30 秒，每 10 秒检查业务是否结束，未结束则自动续期 30 秒

**问题 2：客户端崩溃无法释放锁（死锁）**
- 解决：SET NX EX 命令设置自动过期时间

**问题 3：Redis 主节点宕机，锁数据未同步到从节点，从节点升级为主节点，另一客户端可以重新加锁（锁丢失）**
- 解决：**Redlock（红锁）**

### 11.3 Redlock（红锁）

Redis 作者（antirez）提出，用于解决**主从架构下的锁不可靠**问题：

**算法流程**（假设 5 个独立 Redis 节点，无主从关系）：
1. 获取当前时间戳 t1
2. 依次向 5 个节点发送 `SET lock_key uuid NX PX 30000`（30 秒超时）
3. 收到 **≥ 3 个**（过半）节点的成功响应 → 加锁成功
4. 锁实际有效时间 = 设定过期时间 - (t2 - t1)（减去获取锁消耗的时间）
5. 若加锁失败（未过半），向所有节点发送 DEL 释放已成功的锁

> ⚠️ **Redlock 的争议**：分布式系统专家 Martin Kleppmann 认为 Redlock 在时钟漂移、GC 停顿等场景下仍不安全，建议使用基于 ZooKeeper 的 fencing token 方案。antirez 也有回应。  
> 实际使用中：**大多数场景（非金融级强一致性）用单机 Redis + Watchdog（Redisson）即可**；金融级别需要 ZooKeeper 或 etcd。

---

## 12 附录：面试速查表

### Redis 基础高频考点

| 问题 | 一句话答案 |
|------|-----------|
| Redis 为什么快？ | 纯内存 + 单线程无锁 + I/O 多路复用 + 高效数据结构 |
| Redis 单线程如何处理并发？ | epoll 事件驱动，多路复用监听多个 socket，命令串行执行 |
| Redis 6.0 多线程做什么？ | 只处理**网络 I/O（读写 socket）**，命令执行仍是单线程 |
| 大 key 如何安全删除？ | `UNLINK` 异步删除，避免主线程阻塞 |

### 数据结构高频考点

| 问题 | 一句话答案 |
|------|-----------|
| ZSet 为什么用跳表不用红黑树？ | 跳表实现简单 + 范围查询（ZRANGE）直接顺序遍历，优于红黑树 |
| 渐进式 rehash 如何保证不丢数据？ | 读先查 ht[0] 再查 ht[1]；新增写入 ht[1]；每次操作迁移一个桶 |
| listpack 为什么能解决连锁更新？ | backlen 记录**当前节点**长度，不依赖相邻节点，修改当前节点不影响其他 |
| ZSet 为什么同时用跳表和哈希表？ | 跳表支持范围查询 O(log N)，哈希表支持 O(1) 单点查询 |
| embstr 和 raw 的分界为什么是 44 字节？ | jemalloc 分配 64 字节块，redisObject(16B) + SDS头(4B) = 20B，64-20=44B |

### 持久化高频考点

| 问题 | 一句话答案 |
|------|-----------|
| RDB 如何不阻塞主线程？ | fork 子进程执行 bgsave，父子进程通过 COW 共享内存页 |
| AOF 三种 fsync 的安全性？ | always > everysec（最多丢1秒）> no；生产推荐 everysec |
| AOF 重写期间新命令如何处理？ | 同时写入 aof_buf（维护旧文件）和 aof_rewrite_buf（追加到新文件）|
| 混合持久化文件格式？ | [RDB 全量快照] + [增量 AOF 命令]，恢复时先加载 RDB 再回放 AOF |
| 重启时 AOF 和 RDB 哪个优先？ | **优先加载 AOF**（数据更完整）|

### 高可用高频考点

| 问题 | 一句话答案 |
|------|-----------|
| 主从全量同步触发条件？ | 第一次连接 OR runID 不匹配 OR repl_backlog_buffer offset 超范围 |
| replication buffer vs repl_backlog？ | 前者是给单个从节点传命令的缓冲；后者是所有从节点共用的环形缓冲，用于增量同步 |
| 哨兵如何选新主节点？ | replica-priority → offset 最大（数据最新）→ runID 最小 |
| Cluster 为什么是 16384 个槽？ | gossip 心跳包携带槽位 bitmap，16384/8=2KB 合理；作者认为不超1000节点 |
| MOVED vs ASK 重定向？ | MOVED：槽已迁移完，永久更新客户端；ASK：迁移中，仅本次去新节点，不更新缓存 |

### 缓存问题高频考点

| 问题 | 一句话答案 |
|------|-----------|
| 缓存雪崩如何预防？ | TTL 加随机值 + 多级缓存 + Redis 高可用集群 |
| 缓存击穿如何解决？ | 互斥锁（强一致）或逻辑过期（高性能，弱一致）|
| 缓存穿透如何解决？ | 布隆过滤器（拦截不存在的 key）或缓存空值（简单但占内存）|
| 数据库缓存一致性最佳方案？ | **先更新 DB，再删除 Cache**；删除失败通过 MQ 重试或订阅 binlog 异步删除 |
| 布隆过滤器的误判方向？ | 只会误判"不存在的 key 为存在"；"判断不存在则一定不存在" |

### 分布式锁高频考点

| 问题 | 一句话答案 |
|------|-----------|
| 加锁命令？ | `SET key uuid NX EX 30`（原子操作，NX互斥，EX防死锁）|
| 释放锁为什么用 Lua 脚本？ | GET 和 DEL 非原子，中间锁可能过期被别人抢，Lua 脚本在 Redis 中原子执行 |
| 锁续期如何实现？ | Redisson 的看门狗（Watchdog），每 10 秒续期 30 秒 |
| Redlock 解决什么问题？ | 主从切换导致锁数据不同步的安全问题；过半节点（3/5）加锁成功才生效 |
