# Day 09：C++ Linux 服务端编程 — 进阶深入

## 一、Asio 异步模型深入

### Proactor vs Reactor

Asio 默认实现 Proactor 模式，底层在 Linux 上用 epoll（Reactor）：

```
Proactor 语义: 发起操作 → 完成时通知
Reactor 语义: 就绪通知 → 手动发起操作

Asio (Linux): Proactor API → epoll (Reactor) 模拟
Windows: Proactor API → IOCP (原生 Proactor)
```

### 链式异步调用

```cpp
#include <boost/asio.hpp>
#include <memory>

namespace asio = boost::asio;
using asio::ip::tcp;

// 异步链: 读 → 处理 → 写 → 读（永不阻塞）
class Session : public std::enable_shared_from_this<Session> {
    tcp::socket socket_;
    std::vector<char> read_buf_{4096};
    std::vector<char> write_buf_;

public:
    Session(tcp::socket socket) : socket_(std::move(socket)) {}
    void Start() { DoRead(); }

private:
    void DoRead() {
        // shared_from_this() 保证 Session 在异步操作期间不被销毁
        socket_.async_read_some(
            asio::buffer(read_buf_),
            [self = shared_from_this()](auto error, size_t length) {
                if (!error) {
                    self->ProcessMessage(length);
                    self->DoWrite();
                }
            });
    }

    void ProcessMessage(size_t length) {
        write_buf_ = BuildResponse(read_buf_.data(), length);
    }

    void DoWrite() {
        // 写入完成后再读（回压控制，防止缓冲区无限膨胀）
        asio::async_write(socket_, asio::buffer(write_buf_),
            [self = shared_from_this()](auto error, size_t) {
                if (!error) self->DoRead();
            });
    }
};

// 异步接受连接
asio::io_context io;
tcp::acceptor acceptor(io, tcp::endpoint(tcp::v4(), 8888));

std::function<void()> AcceptNext = [&]() {
    acceptor.async_accept(
        [&](auto error, tcp::socket socket) {
            if (!error)
                std::make_shared<Session>(std::move(socket))->Start();
            AcceptNext();
        });
};
AcceptNext();
io.run();
```

### io_context + thread pool + strand

```cpp
// 多线程共享 io_context: 4 个线程跑事件循环
asio::io_context io;
auto work = asio::make_work_guard(io);

std::vector<std::thread> threads;
for (int i = 0; i < 4; i++)
    threads.emplace_back([&io] { io.run(); });

// strand 保证同一连接的处理器串行执行
class Session {
    tcp::socket socket_;
    asio::io_context::strand strand_;

public:
    Session(tcp::socket socket, asio::io_context& io)
        : socket_(std::move(socket)), strand_(io) {}

    void DoRead() {
        socket_.async_read_some(asio::buffer(buf_),
            asio::bind_executor(strand_,
                [self = shared_from_this()](auto error, size_t n) {
                    // 始终在 strand 上串行执行
                }));
    }
};
```

> **C# 类比**: C# 中 `async/await` 默认在 `SynchronizationContext` 上恢复，类似 strand 效果。但 C# 的 Task 不需要手动管理生命周期。

---

## 二、libuv 事件循环

libuv 是 Node.js 的事件循环底层，内部 epoll 封装：

```
uv_run() 六阶段:
┌─ timers  →  setTimeout
├─ pending →  上一轮回调
├─ idle    →  空闲执行
├─ poll    →  epoll_wait (核心, 阻塞等待 IO)
├─ check   →  setImmediate
└─ close   →  关闭回调
```

```cpp
#include <uv.h>

uv_loop_t* loop = uv_default_loop();
uv_tcp_t server;

uv_tcp_init(loop, &server);

struct sockaddr_in addr;
uv_ip4_addr("0.0.0.0", 8888, &addr);
uv_tcp_bind(&server, (const struct sockaddr*)&addr, 0);

// 监听连接
uv_listen((uv_stream_t*)&server, 128,
    [](uv_stream_t* server, int status) {
        auto client = new uv_tcp_t;
        uv_tcp_init(uv_default_loop(), client);
        if (uv_accept(server, (uv_stream_t*)client) == 0) {
            // 开始读客户端数据
            uv_read_start((uv_stream_t*)client,
                [](uv_handle_t*, size_t suggested, uv_buf_t* buf) {
                    buf->base = new char[suggested];
                    buf->len = suggested;
                },
                [](uv_stream_t* stream, ssize_t nread, const uv_buf_t* buf) {
                    if (nread > 0) { /* 处理数据 */ }
                    delete[] buf->base;
                    if (nread < 0)
                        uv_close((uv_handle_t*)stream,
                            [](uv_handle_t* h) { delete (uv_tcp_t*)h; });
                });
        }
    });

uv_run(loop, UV_RUN_DEFAULT);  // 启动事件循环
uv_loop_close(loop);
```

> **对比**: Asio 是 C++ 风格 RAII + 模板；libuv 是 C 风格函数指针 + 裸指针。游戏服务器选 Asio 更常见（类型安全、异常处理）。

---

## 三、自定义内存池 + mmap

### 为什么游戏服务器需要内存池

```
- 玩家上下线频繁分配/释放小对象
- 网络消息收发产生大量临时缓冲区
- malloc 慢（用户态锁、syscall 长）
- 碎片化严重

C# 对比: .NET GC 自动管理，不操心碎片
C++ 自己管理内存，控制权 = 责任
```

### 基于 mmap 的内存池实现

```cpp
#include <sys/mman.h>

class MemoryPool {
    static const size_t BLOCK_SIZE = 64 * 1024;   // 每个 mmap 块 64KB
    static const size_t MIN_ALLOC = 64;            // 最小粒度

    struct FreeNode { FreeNode* next; size_t size; };

    std::vector<void*> blocks_;
    FreeNode* free_list_ = nullptr;
    size_t total_size_ = 0;

    void AddBlock() {
        void* addr = mmap(nullptr, BLOCK_SIZE,
                          PROT_READ | PROT_WRITE,
                          MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
        if (addr == MAP_FAILED) throw std::bad_alloc();
        blocks_.push_back(addr);
        total_size_ += BLOCK_SIZE;
        // 加入空闲链表
        auto* node = static_cast<FreeNode*>(addr);
        node->next = free_list_;
        node->size = BLOCK_SIZE;
        free_list_ = node;
    }

public:
    MemoryPool() { AddBlock(); }

    ~MemoryPool() {
        for (auto* addr : blocks_)
            munmap(addr, BLOCK_SIZE);
    }

    void* Allocate(size_t size) {
        size = (size + MIN_ALLOC - 1) & ~(MIN_ALLOC - 1);
        if (size < MIN_ALLOC) size = MIN_ALLOC;

        FreeNode** prev = &free_list_;
        FreeNode* curr = free_list_;
        while (curr) {
            if (curr->size >= size) {
                if (curr->size >= size + MIN_ALLOC + sizeof(FreeNode)) {
                    // 分割空闲块
                    auto* remain = (FreeNode*)((char*)curr + size);
                    remain->size = curr->size - size;
                    remain->next = curr->next;
                    *prev = remain;
                    curr->size = size;
                } else {
                    *prev = curr->next;  // 整块分配
                }
                return curr;
            }
            prev = &curr->next;
            curr = curr->next;
        }
        AddBlock();  // 无空闲块，新增
        return Allocate(size);
    }

    void Deallocate(void* ptr, size_t size) {
        size = (size + MIN_ALLOC - 1) & ~(MIN_ALLOC - 1);
        if (size < MIN_ALLOC) size = MIN_ALLOC;
        auto* node = static_cast<FreeNode*>(ptr);
        node->size = size;
        node->next = free_list_;
        free_list_ = node;
    }

    void PrintStats() {
        size_t free = 0;
        for (auto* n = free_list_; n; n = n->next) free += n->size;
        printf("Pool: total=%zu KB, free=%zu KB, usage=%.1f%%\n",
               total_size_ / 1024, free / 1024,
               100.0 * (total_size_ - free) / total_size_);
    }
};
```

---

## 四、RAII 封装扩展

### ScopedFD

```cpp
// 通用文件描述符 RAII 封装
class ScopedFD {
    int fd_;
public:
    explicit ScopedFD(int fd = -1) : fd_(fd) {}
    ~ScopedFD() { Reset(); }

    ScopedFD(ScopedFD&& other) noexcept : fd_(other.fd_) { other.fd_ = -1; }
    ScopedFD& operator=(ScopedFD&& other) noexcept {
        if (this != &other) { Reset(); fd_ = other.fd_; other.fd_ = -1; }
        return *this;
    }

    void Reset(int fd = -1) { if (fd_ >= 0) close(fd_); fd_ = fd; }
    int Get() const { return fd_; }
    bool IsValid() const { return fd_ >= 0; }
    int Release() { int fd = fd_; fd_ = -1; return fd; }

    ScopedFD(const ScopedFD&) = delete;
    ScopedFD& operator=(const ScopedFD&) = delete;
};

// 使用: 函数结束自动 close
void handle_client() {
    ScopedFD epfd(epoll_create1(EPOLL_CLOEXEC));
    ScopedFD client_fd(accept(listen_fd, nullptr, nullptr));
    // 异常安全: 即使中途 throw，析构函数也会 close
}

// EventFD 封装
class EventFD {
    ScopedFD fd_;
public:
    EventFD() { fd_.Reset(eventfd(0, EFD_NONBLOCK | EFD_CLOEXEC)); }
    void Notify() { uint64_t v = 1; write(fd_.Get(), &v, sizeof(v)); }
    void Drain()  { uint64_t v; read(fd_.Get(), &v, sizeof(v)); }
    int Get() const { return fd_.Get(); }
};
```

### 自旋锁（适合极短临界区）

```cpp
class SpinLock {
    std::atomic_flag flag_ = ATOMIC_FLAG_INIT;
public:
    void Lock() {
        while (flag_.test_and_set(std::memory_order_acquire))
            _mm_pause();  // 忙等待，适合 < 100ns 临界区
    }
    void Unlock() { flag_.clear(std::memory_order_release); }

    class Guard {
        SpinLock& lock_;
    public:
        explicit Guard(SpinLock& l) : lock_(l) { lock_.Lock(); }
        ~Guard() { lock_.Unlock(); }
    };
};
```

---

## 五、多线程信号处理

### 问题: 信号不可预测

```cpp
// 多线程中信号处理非常危险:
// 1. 信号可能在任何线程中触发
// 2. 信号处理器中只能调 async-signal-safe 函数
// 3. printf / malloc / lock 都不是安全的!

// 错误写法:
std::mutex g_mutex;
void signal_handler(int sig) {
    std::lock_guard<std::mutex> lock(g_mutex);  // 危险! 可能死锁
    printf("sig %d\n", sig);  // printf 不是 async-signal-safe
}
```

### 正确方案: signalfd + epoll

```cpp
#include <sys/signalfd.h>

class SignalManager {
    ScopedFD sfd_;
public:
    // 构造函数中: 阻塞信号 → 创建 signalfd → 注册 epoll
    SignalManager(int epfd) {
        sigset_t mask;
        sigemptyset(&mask);
        sigaddset(&mask, SIGINT);
        sigaddset(&mask, SIGTERM);
        sigaddset(&mask, SIGHUP);   // 重载配置
        sigaddset(&mask, SIGUSR1);  // 自定义

        // 必须在所有线程创建前阻塞信号
        pthread_sigmask(SIG_BLOCK, &mask, nullptr);

        sfd_.Reset(signalfd(-1, &mask, SFD_NONBLOCK | SFD_CLOEXEC));

        struct epoll_event ev;
        ev.events = EPOLLIN;
        ev.data.fd = sfd_.Get();
        epoll_ctl(epfd, EPOLL_CTL_ADD, sfd_.Get(), &ev);
    }

    bool HandleSignal() {
        struct signalfd_siginfo info;
        ssize_t n = read(sfd_.Get(), &info, sizeof(info));
        if (n != sizeof(info)) return false;

        switch (info.ssi_signo) {
        case SIGINT:
        case SIGTERM:
            printf("收到关闭信号, 准备退出\n");
            return true;  // 通知主循环退出
        case SIGHUP:
            printf("重载配置\n");
            ReloadConfig();
            return false;
        case SIGUSR1:
            PrintServerStatus();
            return false;
        }
        return false;
    }
};

// 在主循环中使用:
volatile bool running = true;
while (running) {
    int nfds = epoll_wait(epfd, events, 1024, -1);
    for (int i = 0; i < nfds; i++) {
        if (events[i].data.fd == sigman.GetSignalFD()) {
            if (sigman.HandleSignal()) running = false;
        } else {
            // 正常 IO 处理
        }
    }
}
```

---

## 六、Core Dump 与 GDB 调试

### 生成配置

```bash
# 1. 设置 core dump 大小
ulimit -c unlimited

# 2. 配置路径格式
echo "kernel.core_pattern=/data/coredumps/core-%e-%s-%p-%t" >> /etc/sysctl.conf
sysctl -p
# core-game-server-11-12345-1690000000
#    ↑进程名      ↑信号 ↑PID  ↑时间戳
```

### GDB 实战

```bash
# 加载 core 文件
gdb /opt/game-server/bin/server /data/coredumps/core-server-11-1234-1690000000

# 关键命令序列:
(gdb) bt                  # 看调用堆栈（找崩溃位置）
# 0  raise () from libc
# 1  abort () from libc
# 2  __assert_fail ()     ← 断言失败
# 3  Player::TakeDamage() at player.cpp:123  ← 出问题的地方

(gdb) frame 3             # 切换到第 3 帧
(gdb) info locals         # 查看局部变量
(gdb) print *player       # 打印玩家对象所有字段
# {id = 10086, hp = -100} ← hp 为负数! 类型用错或没检查下限

(gdb) frame 4             # 看上层调用
(gdb) list                # 显示源码上下文

(gdb) thread apply all bt # 所有线程的堆栈
# 网络线程在 epoll_wait，逻辑线程在跑战斗
```

### 常见段错误原因

```cpp
// 1. 悬空指针
delete player;
player->TakeDamage(10);  // 使用已释放对象

// 2. 栈溢出（无限递归）
void Process(Packet* p) { Process(p); }  // 忘记递归终止条件

// 3. 缓冲区溢出
char buf[64];
sprintf(buf, "玩家名: %s", name);  // name > 64 字节

// 4. 数据竞争（一个线程写，另一个线程读，没有 mutex）
```

---

## 七、ASAN / TSAN 使用

### AddressSanitizer 检测内存错误

```cmake
# CMakeLists.txt
set(CMAKE_CXX_FLAGS_ASAN
    "-fsanitize=address -fsanitize=undefined -fno-omit-frame-pointer")
set(CMAKE_EXE_LINKER_FLAGS_ASAN
    "-fsanitize=address -fsanitize=undefined")

# 构建: cmake -DCMAKE_BUILD_TYPE=ASAN ..
```

```bash
# ASAN 输出示例:
$ ./server
# ============================================================
# ERROR: AddressSanitizer: heap-use-after-free on address ...
# READ of size 4 at Player::GetHp() player.cpp:42
# Previously freed by operator delete at PlayerManager::Remove() ...
# ============================================================

# 精确指出了:
# - 错误: heap-use-after-free
# - 读取位置: Player::GetHp() 第 42 行
# - 释放位置: PlayerManager::Remove()
```

### ThreadSanitizer 检测数据竞争

```cmake
# TSAN (不能与 ASAN 同时使用)
set(CMAKE_CXX_FLAGS_TSAN "-fsanitize=thread -fno-omit-frame-pointer")
set(CMAKE_EXE_LINKER_FLAGS_TSAN "-fsanitize=thread")
```

```bash
$ ./server
# WARNING: ThreadSanitizer: data race
#   Write of size 4 by T1: Player::SetHp(int) player.cpp:50
#   Previous read of size 4 by T2: Player::GetHp() player.cpp:45
```

### CI 集成

```yaml
# 在 CI 中跑 ASAN 构建的测试:
# 1. cmake -DCMAKE_BUILD_TYPE=ASAN
# 2. make -j$(nproc)
# 3. ./server --test-mode &
# 4. ./test_client --stress     # 压力测试触发 bug
# 5. ASAN 在发现错误时自动终止进程并输出报告
```

---

## 八、Valgrind Massif 内存分析

### Massif 使用

```bash
# 堆内存分析，找出谁分配了最多内存
valgrind --tool=massif --massif-out-file=massif.out ./server

# 查看报告
ms_print massif.out | head -100
```

### ms_print 输出解读

```
  MB
3.1^                                                    :
   |::                                                  :
   |:::                                                ::
   |:::::                                             :::
   |::::::                                            ::::
   |::::::                                           :::::
   +------------------------------------------------------>s
   0                                                  196.4

峰值内存: 3.1 MB (快照 12)

快照 12 详情:
─ 100.00% 排在最前:
─ 60.00% PlayerManager::CreatePlayer (player_mgr.cpp:80)
─ 20.00% PacketBuffer::Allocate (network.cpp:200)
─ 20.00% 其他
```

### 实战: 内存泄漏定位

```bash
# 场景: 服务器运行 24 小时后内存从 500MB 涨到 2GB

# 1. Massif 找到峰值分配点
valgrind --tool=massif --time-unit=ms ./server

# 2. 对比不同时间点的快照
ms_print massif.out | grep -A 10 "snapshot.*\(20\|30\|40\)"

# 3. 如果 PlayerManager 持续增长:
#    → 玩家断开连接时 Player 对象未释放
#    → 检查 RemovePlayer 是否真正释放了内存
#    → 检查 shared_ptr 循环引用
```

```cpp
// 修复循环引用导致的内存泄漏:
class Player : public std::enable_shared_from_this<Player> {
    // 错误: 互相持有 shared_ptr 导致永远不释放
    std::shared_ptr<Session> session_;
    // 正确: 一方用 weak_ptr
    // std::weak_ptr<Session> session_;
};

// 使用 weak_ptr 打破循环:
if (auto s = session_.lock()) {  // 提升为 shared_ptr
    s->Send(data);
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Asio Proactor | 底层 epoll 模拟异步操作，跨平台 |
| strand | 保证同一连接的多线程串行执行 |
| libuv | Node.js 底层事件循环，六阶段轮询 |
| mmap 内存池 | 大块 mmap + 空闲链表，替代 malloc |
| ScopedFD | RAII 封装文件描述符，异常安全 |
| signalfd | 信号统一到 epoll，避免信号处理器限制 |
| core dump | gdb + core 文件定位段错误 |
| ASAN | 编译插桩检测堆内存错误 |
| TSAN | 编译插桩检测数据竞争 |
| Massif | 堆内存分配追踪，定位泄漏点 |

