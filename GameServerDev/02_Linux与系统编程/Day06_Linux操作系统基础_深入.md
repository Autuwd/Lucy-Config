# Day 06：Linux 操作系统基础 — 进阶深入

## 一、进程调度：CFS 完全公平调度器

### CFS 核心原理

Linux 默认调度器 CFS (Completely Fair Scheduler) 不是给每个进程分配时间片，而是维护一个 **vruntime** (虚拟运行时间)：

```
vruntime = 实际运行时间 × (NICE_0_LOAD / 进程权重)
```

- 权重越高的进程，vruntime 增长越慢
- CFS 用红黑树按 vruntime 排序，每次选最左边的（最小 vruntime）运行
- 保证了所有进程按权重公平分享 CPU

```c
// 内核中 vruntime 的计算逻辑（简化）
// sched/fair.c
static void update_curr(struct cfs_rq *cfs_rq) {
    struct sched_entity *curr = cfs_rq->curr;
    u64 delta_exec = current_time() - curr->exec_start;

    // 根据进程优先级（权重）缩放 vruntime
    curr->vruntime += delta_exec * (NICE_0_LOAD / curr->load.weight);
}
```

### nice 值与权重对照

| nice 值 | 权重 | 相对优先级 |
|---------|------|-----------|
| -20 | 88761 | 最高优先级（游戏核心线程） |
| -10 | 29154 | 高优先级 |
| 0 | 1024 | 默认（普通线程） |
| 10 | 335 | 低优先级 |
| 19 | 15 | 最低（后台日志、备份） |

```bash
# 查看进程调度策略和优先级
chrt -p 1234

# 设置实时 FIFO 调度（优先级 99）
# 游戏服务器音频线程可用此策略保证低延迟
chrt -f -p 99 $(pgrep game-server)

# 设置 SCHED_BATCH 调度（后台批量任务）
chrt -b -p 0 $(pgrep log-flusher)

# 批量查看游戏服务器各线程调度策略
ps -eLo pid,tid,cls,pri,ni,comm | grep game-server
# cls: TS=SCHED_OTHER, FF=SCHED_FIFO, RR=SCHED_RR, B=SCHED_BATCH
```

### 游戏服务器线程调度策略

```cpp
// C++ 设置线程调度策略
#include <pthread.h>
#include <sched.h>

void set_realtime_priority(pthread_t thread, int priority) {
    struct sched_param param;
    param.sched_priority = priority;  // 1-99

    // SCHED_FIFO: 实时先进先出（直到被更高优先级抢占或主动让出）
    // 适合网络收发包线程，保证最低延迟
    int ret = pthread_setschedparam(thread, SCHED_FIFO, &param);
    if (ret != 0) {
        // 需要 CAP_SYS_NICE 权限，容器内通常不可用
        // 降级到 SCHED_OTHER + 负 nice
        setpriority(PRIO_PROCESS, 0, -10);
    }
}

// C# 中不能直接改调度策略
// 但可以通过容器 cgroup 控制 CPU 份额
// dotnet 游戏服务器靠的是异步 IO 而非实时调度
```

> **对比 C#**: .NET 线程调度完全交给 OS，C# 开发者不需要操心调度策略。但对于 C++ 游戏服务器，网络线程设置 SCHED_FIFO 可以将延迟从微秒级再降低。容器环境下通常用 cgroup 的 cpu.weight 代替。

---

## 二、文件系统底层：inode 与日志

### inode 结构

文件系统中每个文件都有一个 inode（索引节点），存的是元数据，不包含文件名：

```
inode 结构:
├── 文件类型 (普通文件/目录/符号链接)
├── 权限 (rwxr-xr-x)
├── 所有者 UID/GID
├── 文件大小
├── 时间戳 (atime/mtime/ctime)
├── 引用计数 (硬链接数)
└── 数据块指针 (直接/间接/双重间接/三重间接)
```

```bash
# 查看 inode 信息
stat server.log
#   File: server.log
#   Size: 24576           Blocks: 48         Block size: 4096
#   Device: 801h/2049d    Inode: 1234567     Links: 1
#   Access: 2026-07-15 22:00:00

# 查看文件系统 inode 使用情况
df -i /data
# Filesystem     Inodes  IUsed   IFree  IUse%
# /dev/sda1      65536   45231   20305  69%

# inode 耗尽导致无法创建新文件（即使磁盘还有空间）
# 游戏日志服务器需监控 inode 使用率
```

### ext4 vs xfs vs btrfs 选型

| 特性 | ext4 | xfs | btrfs |
|------|------|-----|-------|
| 最大文件 | 16TB | 8EB | 16EB |
| 最大分区 | 1EB | 8EB | 16EB |
| 日志模式 | ordered/data/writeback | metadata only | copy-on-write |
| 碎片问题 | 严重（需定期 defrag） | 好 | COW 避免 |
| 快照 | 不支持 | 不支持（xfs_fsr 仅碎片整理） | 原生支持 |
| 压缩 | 不支持 | 不支持 | zstd/lzo/zlib |
| 适用场景 | 通用、小文件多 | 大文件、高并发 IO | 快照需求、容器存储 |

```bash
# 查看文件系统类型
df -Th /data
# Filesystem     Type   Size  Used Avail Use%
# /dev/sda1      xfs    500G  120G  380G  24%

# 游戏服务器日志目录推荐 xfs
# MySQL/Redis 数据目录推荐 ext4 或 xfs
# 需要快照的容器存储推荐 btrfs

# 检查碎片率（xfs 无需碎片整理）
xfs_db -c frag -r /dev/sda1

# ext4 碎片整理
e2fsck -fn /dev/sda1   # 只检查，不修复
```

### 日志文件系统写模式

```bash
# ext4 日志模式（直接影响性能和数据安全）
# /etc/fstab 配置:
/dev/sda1 /data ext4 defaults,data=ordered 0 0

# data=ordered (默认): 先写数据，再写日志元数据
#   性能和数据安全平衡，大部分场景推荐
# data=writeback: 数据和日志顺序无关
#   最快但崩溃后可能文件内有垃圾数据
# data=journal: 数据和元数据都先写日志
#   最安全但慢 2-3 倍

# 游戏服务器建议:
# 数据库文件 → data=ordered (默认)
# 日志文件 → data=writeback (丟几行日志无所谓，性能重要)
# 配置文件 → data=ordered
```

---

## 三、内存管理：OOM Killer 与 cgroups

### OOM Killer 规则

当系统内存耗尽，内核调用 OOM Killer 选择一个进程杀掉：

```bash
# 每个进程的 oom_score（越大越容易被杀）
cat /proc/1234/oom_score
# 范围: 0-1000

# 查看 oom_score_adj（手动调整被杀的倾向）
cat /proc/1234/oom_score_adj
# -1000 表示禁用 OOM killer（对游戏服务器主进程设置此值）
# 0 默认
# +1000 表示总是被杀

# 保护游戏服务器不被 OOM 杀掉
echo -1000 > /proc/$(pgrep game-server)/oom_score_adj

# 或通过 systemd 单元配置
# [Service]
# OOMScoreAdjust=-1000
```

### cgroups v2 资源限制

现代 Linux 用 cgroups v2 做精细化资源控制，是 Docker 容器隔离的底层技术：

```bash
# 检查 cgroups 版本
mount | grep cgroup
# cgroup2 on /sys/fs/cgroup type cgroup2 ...  ← v2

# 创建自己的 cgroup 控制游戏服务器进程
mkdir -p /sys/fs/cgroup/game-server/

# 限制 CPU（100000 = 1 核）
echo "50000 100000" > /sys/fs/cgroup/game-server/cpu.max
# 解释: 每 100ms 最多用 50ms CPU = 0.5 核

# 限制内存
echo 4G > /sys/fs/cgroup/game-server/memory.max
echo 2G > /sys/fs/cgroup/game-server/memory.swap.max

# 将游戏进程加入 cgroup
echo $(pgrep game-server) > /sys/fs/cgroup/game-server/cgroup.procs

# IO 限制（限制磁盘写入）
echo "8:0 wbps=10485760" > /sys/fs/cgroup/game-server/io.max
# 限制主设备 8:0 的写带宽为 10MB/s
```

### C++ 内存监控与 cgroup 通知

```cpp
// 从 cgroup 读取内存使用情况
#include <fstream>
#include <string>

struct MemoryStats {
    uint64_t used_bytes;
    uint64_t limit_bytes;
    uint64_t oom_kills;
};

MemoryStats read_cgroup_memory() {
    MemoryStats stats{};
    std::ifstream current("/sys/fs/cgroup/memory.current");
    current >> stats.used_bytes;

    std::ifstream max("/sys/fs/cgroup/memory.max");
    max >> stats.limit_bytes;

    // 当内存使用超过 limit 的 90% 触发告警
    double usage_ratio = (double)stats.used_bytes / stats.limit_bytes;
    if (usage_ratio > 0.9) {
        // 触发内存清理或连接拒绝
        trigger_memory_pressure_handler();
    }

    return stats;
}

// C# 等效写法（注意 C# 容器内读同一 cgroup 文件）
// var memUsed = File.ReadAllText("/sys/fs/cgroup/memory.current");
// var memLimit = File.ReadAllText("/sys/fs/cgroup/memory.max");
//
// 但 C# 更推荐用 GC.GetTotalMemory(false) + GCNotification
```

### namespaces: 进程隔离的基石

Docker 容器靠 6 种 namespace 实现隔离：

| namespace | 隔离内容 | 游戏服务器影响 |
|-----------|---------|--------------|
| PID | 进程号 | 容器内 PID 从 1 开始 |
| Network | 网络栈 | 独立 IP、端口空间 |
| Mount | 挂载点 | 独立文件系统视图 |
| UTS | 主机名 | hostname 不同 |
| IPC | 进程间通信 | 共享内存、信号量隔离 |
| User | 用户 ID | UID 映射（root 在容器内 = 普通用户在外） |

```bash
# 查看进程所属的 namespaces
ls -la /proc/1234/ns/
# lrwxrwxrwx ... ipc -> ipc:[4026531839]
# lrwxrwxrwx ... net -> net:[4026531993]   ← 和外部不同，说明在容器内
# lrwxrwxrwx ... pid -> pid:[4026531836]
```

---

## 四、网络 Namespace 与多租户隔离

### 创建网络 namespace 隔离游戏实例

```bash
# 创建网络 namespace
ip netns add game-instance-1

# 在该 namespace 中启动游戏服务器进程
ip netns exec game-instance-1 ./game-server --port 8888

# 查看隔离的网络栈（该进程看到完全独立的网络设备）
ip netns exec game-instance-1 ip addr
# 1: lo: <LOOPBACK> mtu 65536  ← 只有 lo，没有 eth0

# 创建 veth pair 连接 namespace 到根命名空间
ip link add veth0 type veth peer name veth1
ip link set veth1 netns game-instance-1

# 配置 IP
ip addr add 10.0.1.1/24 dev veth0
ip link set veth0 up
ip netns exec game-instance-1 ip addr add 10.0.1.2/24 dev veth1
ip netns exec game-instance-1 ip link set veth1 up

# 现在 game-instance-1 中的服务器可以通过 10.0.1.2 通信
# 每个游戏实例有独立端口空间，互不干扰
```

### 游戏服务器多区隔离方案

```cpp
// 在 C++ 中不能直接创建 namespace（需要 CAP_SYS_ADMIN）
// 但可以通过 clone() 带 CLONE_NEWNET 标志创建隔离子进程
// 实际生产中这类操作用 Docker 容器完成
//
// C# 场景: 在同一个 Linux 主机上跑多个游戏区
// 每个区一个 Docker 容器 = 自动获得独立网络 namespace
//
// 端口空间隔离的好处:
// 区 A 和 区 B 都可以用 8888 端口，互不冲突
// 通过 Docker bridge 网络和端口映射对外暴露不同端口
```

---

## 五、systemd 进阶：Socket 激活与 Watchdog

### Socket 激活（按需启动）

systemd 可以在连接到达时才启动服务，游戏管理后台适用：

```ini
# /etc/systemd/system/game-admin.socket
[Unit]
Description=Game Admin Socket

[Socket]
ListenStream=0.0.0.0:9999
Accept=yes          # 每个连接 fork 一个进程

[Install]
WantedBy=sockets.target
```

```ini
# /etc/systemd/system/game-admin@.service
[Unit]
Description=Game Admin Connection

[Service]
ExecStart=/opt/game-server/bin/admin-handler
StandardInput=socket  # 从 socket 读取数据
StandardOutput=socket # 输出直接写到 socket
```

```bash
# 启用 socket 激活
systemctl enable game-admin.socket
systemctl start game-admin.socket

# 连接时自动启动 handler，无连接时进程数为 0
# 对游戏服务器主逻辑不适用（需要常驻），但管理后台很适合
```

### Watchdog 监控

```ini
# /etc/systemd/system/game-server.service
[Service]
# 启用 watchdog
WatchdogSec=30

# 如果主进程 30 秒内没有发通知，systemd 认为死锁
# 自动执行: SIGABRT → 生成 core dump → 重启

# 通知 systemd 自己还活着
# ExecStart 中的进程需要定期调用 sd_notify
```

```cpp
// C++ 程序发送 watchdog 通知
#include <systemd/sd-daemon.h>

// 游戏主循环中
while (g_running) {
    // 处理网络事件
    int nfds = epoll_wait(epfd, events, 1024, 5000); // 5 秒超时

    // 告诉 systemd 我还活着
    sd_notify(0, "WATCHDOG=1");

    // 处理连接 ...
}

// 如果主循环卡死超过 30 秒 → 没发 WATCHDOG=1
// → systemd 发送 SIGABRT → 生成 core dump → 重启
```

```csharp
// C# 通过环境变量检测是否在 systemd 下运行
// 然后通过 Unix Domain Socket 通知 systemd
// dotnet 的 Microsoft.Extensions.Hosting.Systemd 包自动处理
//
// builder.Host.UseSystemd();  // 自动发送 watchdog 通知
// 游戏服务器用 ASP.NET Core 时，一行代码即可集成
```

---

## 六、sysctl 内核参数调优

```bash
# 所有参数优化前先备份
sysctl -a > /etc/sysctl.backup.$(date +%Y%m%d)

# 游戏服务器推荐配置
cat >> /etc/sysctl.d/99-game-server.conf << 'EOF'

# ── 网络栈优化 ──

# 最大连接数 backlog
net.core.somaxconn = 65535

# 本地端口范围（客户端连接其他服务用）
net.ipv4.ip_local_port_range = 1024 65535

# TIME_WAIT 复用（1 秒后重用）
net.ipv4.tcp_tw_reuse = 1
# 注意: tcp_tw_recycle 在 Linux 4.12+ 已移除（NAT 环境有问题）

# 默认发送/接收缓冲区
net.core.wmem_default = 131072
net.core.rmem_default = 131072

# 最大发送/接收缓冲区（自动调整）
net.core.wmem_max = 16777216
net.core.rmem_max = 16777216

# TCP 自动缓冲区调整范围（min, default, max）
net.ipv4.tcp_wmem = 4096 65536 16777216
net.ipv4.tcp_rmem = 4096 87380 16777216

# keepalive 检测
net.ipv4.tcp_keepalive_time = 120      # 2 分钟开始检测
net.ipv4.tcp_keepalive_intvl = 10      # 10 秒间隔
net.ipv4.tcp_keepalive_probes = 3      # 3 次失败断开

# ── 内存压力 ──

# 虚拟内存行为（0=不 swap，100=积极 swap）
vm.swappiness = 10   # 游戏服务器尽量少用 swap

# OOM 时候不杀最耗内存的进程，而是发信号让进程自己处理
vm.overcommit_memory = 1  # 允许超额分配

# ── 进程 ──

# PID 上限
kernel.pid_max = 4194304

# core dump 文件名格式
kernel.core_pattern = /data/coredumps/core-%e-%p-%t
EOF

# 立即生效
sysctl --system
```

### 验证调优效果

```bash
# 查看连接状态分布
ss -tan | awk '{print $1}' | sort | uniq -c
# 正常情况 ESTAB 占绝大多数，TIME_WAIT 控制在一定比例

# 查看当前 TCP 缓冲区实际使用
cat /proc/net/sockstat
# TCP: inuse 234 orphan 0 tw 56 alloc 456 mem 12890

# 查看丢包和重传
netstat -s | grep -E "retransmit|dropped|overflow"
# 如果重传率 > 0.5% 需要检查网络或调参
```

---

## 七、/proc 和 /sys 虚拟文件系统深度诊断

```bash
# ── /proc 诊断 ──

# 进程所有打开的文件描述符（游戏服务器 FD 泄漏排查）
ls /proc/$(pgrep game-server)/fd/ | wc -l
# 正常: 几百到几千，异常: 持续增长

# 查看事件 epoll 监听器数量
ls /proc/$(pgrep game-server)/fdinfo/ | head -1 | xargs cat
# pos:    0
# flags:  02000002
# mnt_id: 14
# tfd:    0 events:19     ← EPOLLIN|EPOLLET 的数值
# tfd:    1 events:1
# ...

# 进程内存映射
cat /proc/$(pgrep game-server)/maps | head -20
# 可以看到 mmap 的文件区域

# 网络连接对应进程
cat /proc/net/tcp
# 查看所有 TCP 连接（raw 格式，IP 和端口用十六进制）

# ── /sys 诊断 ──

# CPU 信息
cat /sys/devices/system/cpu/cpu0/cache/index0/size  # L1 缓存大小

# NUMA 节点拓扑
cat /sys/devices/system/node/node0/cpulist
# 游戏服务器绑定 NUMA node 0 的 CPU 核可获得更低延迟

# 磁盘队列深度（影响 IO 性能）
cat /sys/block/sda/queue/nr_requests
# 增大此值可提高吞吐但增加延迟
echo 512 > /sys/block/sda/queue/nr_requests
```

---

## 八、strace / ltrace / ftrace 系统调用追踪

### strace 实战

```bash
# 追踪游戏服务器启动过程
strace -f -o /tmp/startup.trace ./game-server

# 查看启动了哪些线程
grep clone /tmp/startup.trace

# 查看打开的文件和配置
grep -E 'open|connect|bind' /tmp/startup.trace

# 附加到运行中的进程（性能影响大，生产慎用）
strace -p $(pgrep game-server) -e trace=network -c
# -c: 汇总统计，只输出 syscall 计数，不是逐条
# 输出: 每秒钟调用了几次 accept/read/write

# 追踪特定系统调用
strace -p $(pgrep game-server) -e trace=epoll_wait,write,read
# -e trace=network: 只显示网络相关
# -e trace=file: 只显示文件操作
# -e trace=signal: 只显示信号

# 统计耗时
strace -p $(pgrep game-server) -T -e trace=write 2>&1 | head -20
# <... write resumed> ) = 4096 <0.000035>  ← 每次写入耗时 35 微秒
```

### ltrace（库调用追踪）

```bash
# 追踪动态库调用（比 strace 级别更高）
ltrace -p $(pgrep game-server) -e malloc+free
# 查看内存分配模式

# 统计耗时最长库函数
ltrace -c -p $(pgrep game-server)
# % time     seconds  usecs/call     calls      function
# ------ ----------- ----------- --------- --------------------
#  45.23    12.3456        12.3   1000000    malloc
#  30.12     8.2345         8.2   1000000    free
#  15.67     4.2765         4.3    500000    send
```

### ftrace（内核函数追踪，零开销）

```bash
# ftrace 在内核层面追踪，对目标进程几乎零性能影响

# 设置 ftrace
cd /sys/kernel/tracing

# 选择追踪器
echo function_graph > current_tracer

# 设置追踪的函数（只追踪 epoll 相关）
echo epoll_wait > set_ftrace_filter

# 设置追踪的进程
echo $(pgrep game-server) > set_ftrace_pid

# 开始追踪
echo 1 > tracing_on

# 等待几秒
sleep 5

# 停止
echo 0 > tracing_on

# 查看结果
cat trace | head -50

# 清理
echo nop > current_tracer
```

> **对比 C#**: dotnet 中对应诊断工具是 `dotnet-trace`（EventPipe, 类似 ftrace）、`dotnet-counters`（性能计数器，类似 /proc）、`dotnet-dump`（类似 strace 的核心转储分析）。Linux 知识对 C# 开发者仍然有用，因为 ASP.NET Core 运行在 Linux 上，底层还是这些机制。

---

## 九、实战：游戏服务器启动诊断

```bash
#!/bin/bash
# diagnose.sh - 游戏服务器启动诊断脚本
set -euo pipefail

SERVER_PID=$(pgrep game-server || echo "")

if [ -z "$SERVER_PID" ]; then
    echo "游戏服务器未运行"
    exit 1
fi

echo "═══════════════════════════════════"
echo "游戏服务器诊断报告 (PID: $SERVER_PID)"
echo "═══════════════════════════════════"

echo ""
echo "── 进程信息 ──"
ps -p $SERVER_PID -o pid,ppid,%cpu,%mem,rss,vsz,etime,cls,pri,nlwp

echo ""
echo "── 调度策略 ──"
chrt -p $SERVER_PID 2>/dev/null || echo "chrt 不可用"

echo ""
echo "── 线程数 ──"
ls /proc/$SERVER_PID/task/ | wc -l

echo ""
echo "── 文件描述符数 ──"
FD_COUNT=$(ls /proc/$SERVER_PID/fd/ | wc -l)
echo "打开 FD: $FD_COUNT"
echo "FD 限制: $(cat /proc/$SERVER_PID/limits | grep 'open files')"

echo ""
echo "── 网络连接 ──"
ss -tlnp | grep -E "$SERVER_PID|game-server"
echo "ESTAB 连接数: $(ss -tn state established | grep game-server | wc -l)"

echo ""
echo "── 内存 ──"
cat /proc/$SERVER_PID/status | grep -E "VmRSS|VmSize|VmPeak|Threads"

echo ""
echo "── OOM 评分 ──"
echo "oom_score: $(cat /proc/$SERVER_PID/oom_score)"
echo "oom_score_adj: $(cat /proc/$SERVER_PID/oom_score_adj)"

echo ""
echo "── cgroup ──"
cat /proc/$SERVER_PID/cgroup
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| CFS 调度 | 红黑树按 vruntime 排序，权重高的进程 CPU 份额多 |
| inode | 文件元数据存储结构，不包含文件名 |
| ext4 vs xfs | ext4 小文件好，xfs 大文件好，btrfs 快照好 |
| OOM Killer | 内存耗尽时内核选择进程杀掉，主进程应设 oom_score_adj=-1000 |
| cgroups v2 | 精细控制 CPU/内存/IO 上限，Docker 底层基础 |
| namespaces | 6 种命名空间实现容器隔离 |
| systemd watchdog | 主循环定期通知，超时自动重启 |
| sysctl | 内核参数调优，游戏服务器必须优化网络栈 |
| /proc | 进程级虚拟文件系统，实时诊断依据 |
| strace/ftrace | 用户态/内核态系统调用追踪，定位性能瓶颈 |
