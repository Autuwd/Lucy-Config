# Day 13：Redis 核心原理 — 进阶深入

> 目标读者：游戏服务端开发者，已掌握 Redis 基本操作
> 前置知识：Redis 5 种基本数据类型、过期策略、持久化基础概念

---

## 一、SDS（Simple Dynamic String）设计原理

### 1.1 SDS 结构

Redis 没有直接使用 C 语言的 `char*`，而是自己实现了 SDS。

```c
// C 字符串 char*：
// 问题：O(n) 获取长度、二进制不安全、缓冲区溢出

// Redis 3.2 前的 SDS 结构：
struct sdshdr {
    int len;     // 已使用长度（O(1) 获取字符串长度）
    int free;    // 空闲长度
    char buf[];  // 字节数组
};

// Redis 3.2 后的 SDS 有 5 种类型（根据字符串长度选择）：
// sdshdr5（2^5=32B）、sdshdr8（2^8=256B）
// sdshdr16（64KB）、sdshdr32（4GB）、sdshdr64（2^64）
// 通过选择不同 Header 大小，节省内存（小字符串不浪费 8 字节长度字段）
```

### 1.2 SDS vs C 字符串对比

```
特性                  | C 字符串 char*       | Redis SDS
----------------------+---------------------+----------------------------
获取长度              | O(n) strlen()       | O(1) 直接读 len 字段
二进制安全            | 遇到 '\0' 截断       | 按 len 长度读取，完全二进制安全
缓冲区溢出            | 不检查，strcat 可能   | 自动扩容，free 用尽则申请新空间
                       | 覆盖相邻内存         |
空间预分配            | 无                  | 每次扩容额外分配 free（翻倍策略）
惰性空间释放          | 无                  | 缩短时记录 free，复用空间
函数兼容性            | 兼容 C 标准库        | 兼容大部分 C 字符串函数
```

### 1.3 空间预分配策略

```
扩容规则：
  1. 如果新字符串长度 < 1MB → free = len（翻倍预分配）
  2. 如果新字符串长度 ≥ 1MB → free = 1MB（固定预分配 1MB）
  3. 内存不够时 → sdsMakeRoomFor() 重新分配

示例：
  sds str = "hello";    // len=5, free=0
  str = sdscat(str, " world");
  // 新长度 = 11，小于 1MB → free = 11
  // 实际分配 buf = 11+11+1(空字符) = 23 字节
  // len=11, free=11

再次追加：
  str = sdscat(str, "!");
  // 新长度 = 12，free=11 够用 → 不分配，直接写
  // len=12, free=10
```

> 游戏启示：Redis Key 和 Value 的 SDS 空间预分配对频繁追加操作的场景（如日志收集）有显著性能提升。

---

## 二、Skip List（跳跃表）设计原理

### 2.1 为什么选择 Skip List 而不是平衡树

Redis 的 ZSET（有序集合）使用 Skip List + Hash Table 实现。

```
Skip List vs 平衡树（AVL/红黑树）的取舍：

维度           | Skip List            | 平衡树（红黑树）
---------------+----------------------+---------------------------
实现复杂度     | 简单（约 200 行代码）  | 复杂（旋转、颜色调整）
插入/删除      | 无需 rebalance       | 需要旋转/变色
范围查询       | 双向链表遍历           | 需要中序遍历
内存占用       | 节点平均 1.33 个指针   | 每个节点 2 个指针 + 颜色位
并发友好度     | 对锁粒度更友好         | 树结构重平衡复杂

Redis 选择 Skip List 的核心原因：
  1. 代码简单，Bug 少（C 语言实现复杂数据结构容易出内存问题）
  2. 范围查询效率高（ZRANGEBYSCORE 就是跳表遍历）
  3. 插入删除不需要像平衡树那样做全局旋转
```

### 2.2 Skip List 结构

```
Level 3:  head ────────────────────────────────────────→ tail
                     ↓                                    ↓
Level 2:  head ──────────→ [55] ────────────────────────→ tail
                     ↓        ↓                             ↓
Level 1:  head ───→ [21] ──→ [55] ──→ [78] ─────────────→ tail
                     ↓       ↓        ↓                     ↓
Level 0:  head ─→ [3] ─→ [21] ─→ [55] ─→ [78] ─→ [99] ─→ tail
                     (实际数据行)

查找过程（例如查找 78）：
  1. 从 head 的最高层（L3）开始，next 是 tail → 下降一层
  2. L2: next is [55], 55 < 78 → 跳到 [55]
  3. 从 [55] 的 L2: next is tail → 下降一层
  4. L1: next is [78], 78 == 78 → 找到！
  期望时间复杂度：O(log n)
```

### 2.3 随机层高

```c
// 每个新节点的层高随机生成
// Redis 使用幂次分布（Power Law），p=1/4
// 层高 1 的概率 = 3/4
// 层高 2 的概率 = 3/4 * 1/4
// 层高 3 的概率 = 3/4 * (1/4)^2
// 最高层限制：ZSKIPLIST_MAXLEVEL = 64

// 这是一个典型的"抛硬币"算法：
int zslRandomLevel(void) {
    int level = 1;
    while ((random() & 0xFFFF) < (ZSKIPLIST_P * 0xFFFF))
        level += 1;
    return (level < ZSKIPLIST_MAXLEVEL) ? level : ZSKIPLIST_MAXLEVEL;
}
// ZSKIPLIST_P = 0.25，所以每层上升概率为 25%
```

---

## 三、IntSet（整数集合）升级机制

### 3.1 IntSet 结构

```c
typedef struct intset {
    uint32_t encoding;  // 编码方式：INTSET_ENC_INT16/INT32/INT64
    uint32_t length;    // 元素数量
    int8_t contents[];  // 柔性数组，存储所有整数
};
```

### 3.2 自动升级（Upgrade）

```
初始状态（encoding = INT16，2 字节）：
  contents: [1, 2, 3]  ← 每个元素占 2 字节

插入 65536（超出 INT16 范围）：
  Step 1: encoding 升级到 INT32（4 字节）
  Step 2: 重新分配内存（3*2 → 4*4 = 16 字节）
  Step 3: 从后往前迁移数据（保持有序）
  Step 4: 插入新值 65536

升级后（encoding = INT32）：
  contents: [1, 2, 3, 65536]  ← 每个元素占 4 字节

特性：
  - 升级不可逆（删除大值后不会降级）
  - 升级触发阈值：int16_max=32767, int32_max=2147483647
  - 适合：玩家 ID 列表等纯整数集合
```

---

## 四、Ziplist → Listpack 演进

### 4.1 Ziplist 结构

```
Ziplist 是 Redis 为小数据量场景设计的内存紧凑型数据结构。

+---------+----------+--------+----------+-------+---------+
| zlbytes | zltail   | zllen  | entry1  |entry2 | zlend   |
| 4字节   | 4字节    | 2字节  | 变长     | 变长  | 1字节   |
+---------+----------+--------+----------+-------+---------+

每个 Entry 的结构：
+--------+----------+-----------+
| prevlen| encoding | content   |
| 变长   | 变长     | 变长      |
+--------+----------+-----------+

prevlen 编码：
  - 如果前一个 entry 长度 < 254 字节 → 1 字节存储
  - 如果前一个 entry 长度 >= 254 字节 → 5 字节存储（第一个字节 0xFE + 4 字节长度）

连锁更新（Cascade Update）问题：
  当某个 entry 变大（prevlen 从 1 字节变成 5 字节）→
  后续 entry 的 prevlen 都要更新 →
  可能导致 O(n^2) 的写放大！

示例：
  正常: [200B] [254B] [254B] [254B] ...
  修改第一个 entry 从 200B → 300B：
  [300B] [5B prevlen...] ← 这个 entry 从 1B 变成 5B
         [???] ← 下一个 entry 的 prevlen 也要从 1B 变成 5B...
                [???] ← 继续传播...
```

### 4.2 Listpack（Redis 5.0+ 替代方案）

```
Listpack 解决了 Ziplist 的连锁更新问题：

每个 Entry 的结构（Listpack）：
+--------+----------+------+
| encoding| content  | len |
| 变长    | 变长     | 变长 |
+--------+----------+------+

关键改进：
  - 不再存储 prevlen，断开前后依赖
  - 每个 entry 末尾存储自己的长度 len
  - 从后往前遍历时，根据当前 entry 的 len 跳到前一个
  - 修改一个 entry 不影响其他 entry 的内存布局

             ↓ 从后往前遍历
  [entry1: enc+data, 15] [entry2: enc+data, 20] [entry3: enc+data, 12]
                              ↑ 从 entry3 的末尾 12 字节向前跳
                                就能找到 entry2 的开头
```

### 4.3 数据类型何时使用 Ziplist/Listpack

```
Redis 对象编码策略（内部）：
  - Hash: 如果 field-value 对 < 512 且每个 value < 64B → ziplist
  - List: 如果元素数 < 512 且每个元素 < 64B → ziplist (7.0+ → listpack)
  - Zset: 如果元素数 < 128 且每个元素 < 64B → ziplist
  - Set (整数): 如果都是整数且元素数 < 512 → intset

修改阈值（建议不要改）：
  hash-max-ziplist-entries 512
  hash-max-ziplist-value 64
  zset-max-ziplist-entries 128
  zset-max-ziplist-value 64

游戏场景：玩家的背包数据（Hash，field=物品ID, value=数量）
  如果物品种类很少（< 512），适合 ziplist，节省大量内存。
```

---

## 五、Redis 单线程模型与 I/O 多线程（6.0+）

### 5.1 单线程核心架构

```
Redis 6.0 之前：完全单线程（一个主线程处理所有请求）

优势：
  1. 无锁竞争：所有操作串行执行
  2. 没有上下文切换开销
  3. 没有 CPU 缓存失效问题
  4. 实现简单，Bug 少

限制：
  - 只能利用一个 CPU 核心
  - 大 Value 操作会阻塞所有请求（如 KEYS、SMEMBERS、大对象的序列化）

为什么单线程还能快：
  - 内存操作（ns 级别），磁盘 IO 由子进程处理
  - 网络 IO 用多路复用（epoll）
  - 瓶颈通常是网络/内存带宽，不是 CPU
```

### 5.2 epoll 多路复用工作流

```
主线程事件循环：
  while (1) {
      // epoll_wait 等待事件（阻塞，最多 100 万并发连接）
      events = epoll_wait();

      // 串行处理每个事件
      for (event in events) {
          if (event == 新连接) {
              accept() → 注册到 epoll
          }
          if (event == 可读) {
              read() → 解析命令 → 执行命令 → 缓冲输出
          }
          if (event == 可写) {
              write() → 发送结果给客户端
          }
      }
  }

6.0 之前：读、解析、执行、写全是主线程干
6.0 之后：读、写可以交给 I/O 线程，但命令执行仍然单线程
```

### 5.3 I/O 多线程（Redis 6.0+）

```
6.0+ I/O 多线程解决的问题：
  - 网络 IO 成为瓶颈（尤其是大 Value 的读写）
  - 主线程花太多时间在 read()/write() 上

配置：
  io-threads 4          ← I/O 线程数（建议 CPU 核心数减 1）
  io-threads-do-reads yes  ← 读 IO 也使用多线程

工作流：
  主线程：解析命令 → 执行命令（仍然是单线程，保证原子性）
  I/O 线程：处理网络读写（read/write 在主线程和 I/O 线程之间分配）

    主线程                                    IO 线程
  +--------+                               +--------+
  | accept |─── 分配连接给 I/O 线程 ───→    | read   |
  | 解析命令 | ←─── 读取完成后 ────         | read   |
  | 执行命令 |                               | write  |
  | 结果写回 |─── 分配连接给 I/O 线程 ───→    | write  |
  +--------+                               +--------+
```

---

## 六、RDB fork() COW 机制

### 6.1 Copy-On-Write 原理

```
自动快照模式（save 900 1）或手动 BGSAVE：

1. 主进程 fork() 创建子进程
   - fork() 瞬间：父子进程共享所有内存页（标记为只读）
   - 子进程获得父进程的页表副本

2. 子进程开始写 RDB 文件
   - 读取共享内存页
   - 写入临时 RDB 文件

3. 主进程继续处理请求
   - 读操作：直接访问共享页（无开销）
   - 写操作：触发缺页中断 → 复制该页（只有写到的页才复制）

   写操作前的内存状态：
     物理页 [A] ← 父进程指向 ← 子进程指向

   写操作后的内存状态（只复制被修改的页）：
     物理页 [A 旧数据] ← 子进程指向（需要读旧数据写 RDB）
     物理页 [A' 新数据] ← 父进程指向（写入了新值）

4. 子进程写完 RDB 后：
   - 用临时 RDB 文件覆盖旧文件（原子 rename）
   - 通知父进程完成
   - 子进程退出
```

### 6.2 COW 的内存开销

```
COW 导致的内存膨胀：
  如果一个 10GB 的 Redis 实例，在 BGSAVE 期间
  有 20% 的页面被修改 → 需要额外复制 10GB * 20% ≈ 2GB 内存

需要注意：
  - 系统需要有足够的空闲内存容纳 COW 导致的膨胀
  - 如果系统内存不足 → 使用 swap → 性能雪崩
  - 监控 /proc/<redis_pid>/smaps 中的 "Dirty" 页数

调优建议：
  # Linux 内核参数：减少大页的 COW 开销
  echo never > /sys/kernel/mm/transparent_hugepage/enabled
  # 透明大页（THP）会导致 fork() 后 COW 粒度从 4KB 变成 2MB
  # 大幅增加内存开销
```

### 6.3 RDB 配置策略

```conf
# 游戏服务器建议
save 900 1         # 15分钟至少1次写
save 300 10        # 5分钟至少10次写
save 60 1000       # 1分钟至少1000次写（高峰期频繁保存）

# 关键优化
stop-writes-on-bgsave-error yes  # RDB 失败时拒绝写入（防止数据丢失）
rdbcompression yes                # LZF 压缩，减少磁盘空间
rdbchecksum yes                   # 校验和验证
```

---

## 七、AOF 重写（bgrewriteaof）

### 7.1 AOF 文件格式

```
AOF 文件是 Redis 命令的追加日志：

*3\r\n$3\r\nSET\r\n$5\r\nmykey\r\n$7\r\nmyvalue\r\n
*2\r\n$3\r\nGET\r\n$5\r\nmykey\r\n
*5\r\n$4\r\nRPUSH\r\n$5\r\nmylist\r\n$1\r\na\r\n$1\r\nb\r\n$1\r\nc\r\n

问题：AOF 文件会不断增长，需要重写压缩。
```

### 7.2 重写流程

```
AOF 重写（bgrewriteaof）原理（类似 RDB 的 COW）：

1. 主进程 fork() 子进程
2. 子进程遍历当前内存中的数据集，生成最小命令集
   比如：对一个 key 做了 100 次 INCR，重写为 SET key 100（一条命令）
3. 重写期间，主进程的写操作同时追加到两个地方：
   a. 旧 AOF 文件（保证安全）
   b. AOF 重写缓冲区（记录重写开始后的新命令）
4. 子进程重写完成 → 发送信号给父进程
5. 父进程将重写缓冲区的命令追加到新 AOF 文件
6. 原子 rename 新 AOF 文件覆盖旧文件

内存开销：
  - AOF 重写期间 fork()，同样有 COW 内存开销
  - 加上 AOF 重写缓冲区（重写期间的写操作积累）
```

### 7.3 AOF 配置策略

```conf
# 三种 appendfsync 模式
appendfsync always      # 每次写都 fsync（最安全，最慢，约 1000 ops/s）
appendfsync everysec    # 每秒 fsync（推荐，约 100000 ops/s，最多丢 1 秒数据）
appendfsync no          # 由操作系统刷盘（可能丢 30+ 秒数据）

# AOF 自动重写
auto-aof-rewrite-percentage 100    # 文件增长 100% 时触发
auto-aof-rewrite-min-size 64mb     # 最小 64MB 才触发重写

# 游戏场景建议
appendfsync everysec               # 默认即可，游戏可接受 1 秒丢数据
auto-aof-rewrite-percentage 50     # 更频繁重写，控制文件大小
no-appendfsync-on-rewrite yes      # 重写期间不 fsync，防止磁盘 IO 争抢
```

---

## 八、Redis 内存碎片（jemalloc）

### 8.1 内存碎片的产生

```
Redis 使用 jemalloc（默认）或 tcmalloc 作为内存分配器。

碎片产生原因：
  1. 频繁的更新操作：SET key 一个 32 字节的值 → SET key 一个 512 字节的值
     → 原来的 32 字节无法复用，变成碎片
  2. 不同大小的 key-value 混合存储
  3. 内存分配器的内碎片（分配 33 字节实际占 64 字节）

查看碎片率：
  redis-cli INFO memory | grep mem_fragmentation_ratio
  mem_fragmentation_ratio: 1.5   ← 每分配 1 字节实际占用 1.5 字节
```

### 8.2 jemalloc 的内存分配策略

```
jemalloc 将内存按大小分成多个 class：
  8, 16, 32, 48, 64, 80, 96, 112, 128, ...
  256, 512, 768, 1024, 1280, ...
  2KB, 4KB, 8KB, ... 一直到 4MB

分配 33 字节：
  → jemalloc 在 32 字节 class 找不到空闲块
  → 从 48 字节 class 分配（碎片 15 字节）

同一 size class 的内存回收后可以复用。
不同 size class 之间容易出现碎片。
```

### 8.3 碎片整理

```bash
# 查看内存碎片率
redis-cli> INFO memory
# used_memory: 实际存储的数据大小
# used_memory_rss: 进程占用的物理内存
# mem_fragmentation_ratio = used_memory_rss / used_memory

# 正常范围：1.0 - 1.5
# 小于 1.0：内存被交换到磁盘（危险!）
# 大于 1.5：碎片较多，需要整理

# 自动碎片整理（Redis 4.0+）
CONFIG SET activedefrag yes
CONFIG SET active-defrag-threshold-lower 10    # 碎片超过 10% 开始整理
CONFIG SET active-defrag-threshold-upper 100   # 碎片超过 100% 尽最大努力
CONFIG SET active-defrag-cycle-min 25          # 最小 CPU 占用
CONFIG SET active-defrag-cycle-max 75          # 最大 CPU 占用

# 手动触发碎片整理
DEBUG SET-ACTIVE-DEFRAG 1

# 游戏场景注意：
# 排行榜频繁更新排名（ZADD/DEL），碎片可能偏高
# 建议凌晨低峰期重启实例或执行碎片整理
```

### 8.4 内存碎片优化策略

```
游戏开发中的内存优化建议：

1. 使用 Hash 代替 String（对象压缩）
   ❌ SET player:1001:name "Alice"
      SET player:1001:level 50
   ✅ HMSET player:1001 name "Alice" level 50
   → 一个 Hash 对象比多个 String 节省内存（减少 key 开销）

2. 使用 ziplist 编码的 Hash
   → field-value 对少时大幅节省内存

3. 批量操作
   ❌ 1000 次 SET
   ✅ MSET/管道（减少网络 IO + 减少 SDS 分配次数）

4. 合理设置 maxmemory
   maxmemory 4gb
   maxmemory-policy allkeys-lru  # 游戏缓存场景
```

---

## 停靠点

| 知识点 | 一句话总结 | 游戏开发启示 |
|--------|-----------|--------------|
| SDS | Redis 自建动态字符串，O(1) 长度，二进制安全 | 存序列化数据时完全不需要担心 '\0' |
| Skip List | 概率平衡树，范围查询高效，实现简单 | ZSET 的底层，排行榜场景核心结构 |
| IntSet 升级 | 整数集合自动扩展编码长度，节省空间 | 纯 ID 列表场景自动优化 |
| Ziplist→Listpack | 解决连锁更新问题，内存更紧凑 | 小对象 Hash 用 ziplist 节省内存 |
| 单线程+epoll | 无锁串行处理，100万并发 | 游戏常规使用不需担心并发 |
| I/O 多线程 | 6.0+ 网络 IO 并行化，命令执行仍单线程 | 大 Value 场景（如公会成员列表）提升吞吐 |
| RDB COW | fork() 后共享内存，写时复制 | BGSAVE 期间需额外 20%-30% 内存 |
| AOF 重写 | 子进程遍历内存生成最小命令集 | 每天低峰期做一次重写 |
| jemalloc 碎片 | 分配器按 size class 分配 | 用 Hash 替代 String、合理对齐 key 大小 |
| activedefrag | 4.0+ 自动在线整理碎片 | 高更新频率场景（排行榜）启用 |


> 进阶方向：阅读 Redis 源码的 src/ziplist.c、src/t_zset.c、src/sds.c，理解核心数据结构实现。
