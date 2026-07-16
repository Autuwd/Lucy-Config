# Day 07：Linux 高并发网络编程 — 进阶深入

## 一、epoll_create1 vs epoll_create

### 历史演变

```c
// 旧 API（Linux 2.5.44, 2003）
int epfd = epoll_create(int size);
// size 参数在 2.6.8 后已被忽略，但必须 > 0
// 原因: 内核内部自动调整大小

// 新 API（Linux 2.6.27, 2008）
int epfd = epoll_create1(int flags);

// 推荐: 总是用 epoll_create1
int epfd = epoll_create1(EPOLL_CLOEXEC);
```

### EPOLL_CLOEXEC 的作用

```c
// 没有 EPOLL_CLOEXEC 时的问题:
int epfd = epoll_create(256);  // 旧方式

pid_t pid = fork();
if (pid == 0) {
    // 子进程继承了这个 epfd
    // 如果子进程 exec() 执行其他程序
    // epfd 没有被关闭 → 泄漏文件描述符
    execl("./worker", "worker", NULL);
    // 执行 exec 后 epfd 仍然打开！
}

// 有 EPOLL_CLOEXEC:
int epfd = epoll_create1(EPOLL_CLOEXEC);
// exec() 时自动关闭，不会泄漏
```

> **C# 类比**: dotnet 中 `Process.Start` 默认不继承句柄，类似 EPOLL_CLOEXEC 效果。但 C++ 手动管理 fork/exec 时必须显式设置。

### 游戏服务器中 fork 场景

```cpp
// 预 fork 模型：监听进程 fork 多个子进程
// 每个子进程都有 epoll fd，但不需要 exec
// 所以可以用 epoll_create1(0)（不需要 CLOEXEC）

// 但如果子进程为热更新 exec 新版本
// 就必须 EPOLL_CLOEXEC 防止旧 epoll fd 泄漏

// 最佳实践: 不管你 fork 不 fork，永远用 EPOLL_CLOEXEC
int epfd = epoll_create1(EPOLL_CLOEXEC);
```

---

## 二、EPOLLEXCLUSIVE：防止惊群效应

### 惊群问题

```c
// 多个线程/进程同时 epoll_wait 同一个 listen fd
// 一个连接到达 → 所有线程被唤醒 → 只有一个 accept 成功
// 其他线程空转（浪费 CPU）

// 这是经典的 thundering herd 问题
```

### EPOLLEXCLUSIVE 解决方案

```c
// Linux 4.5+ 引入 EPOLLEXCLUSIVE
// 只唤醒一个等待者，避免惊群

struct epoll_event ev;
ev.events = EPOLLIN | EPOLLEXCLUSIVE;
// 注意: EPOLLEXCLUSIVE 不能和 EPOLLET 同时使用！
ev.data.fd = listen_fd;
epoll_ctl(epfd, EPOLL_CTL_ADD, listen_fd, &ev);

// 多个 worker 线程/进程都注册同一个 listen fd
// 新连接到来时 → 只唤醒一个 worker
// 这个 worker accept → 其他 worker 继续保持睡眠
```

### 预 fork + EPOLLEXCLUSIVE 完整模式

```c
// 预 fork + epoll 服务器（Nginx 风格）
#define WORKER_COUNT 4

void worker_process(int epfd, int listen_fd) {
    struct epoll_event events[1024];

    // 注册 listen fd 带 EPOLLEXCLUSIVE
    struct epoll_event ev;
    ev.events = EPOLLIN | EPOLLEXCLUSIVE;
    ev.data.fd = listen_fd;
    epoll_ctl(epfd, EPOLL_CTL_ADD, listen_fd, &ev);

    while (1) {
        int nfds = epoll_wait(epfd, events, 1024, -1);
        for (int i = 0; i < nfds; i++) {
            if (events[i].data.fd == listen_fd) {
                // 只有这个 worker 被唤醒
                struct sockaddr_in addr;
                socklen_t addrlen = sizeof(addr);
                int client = accept(listen_fd, (struct sockaddr*)&addr, &addrlen);
                handle_client(epfd, client);
            }
        }
    }
}

int main() {
    int listen_fd = socket(AF_INET, SOCK_STREAM | SOCK_NONBLOCK, 0);
    // ... bind, listen ...

    for (int i = 0; i < WORKER_COUNT; i++) {
        pid_t pid = fork();
        if (pid == 0) {
            // 子进程
            int epfd = epoll_create1(EPOLL_CLOEXEC);
            worker_process(epfd, listen_fd);
            exit(0);
        }
    }

    // 父进程等待子进程
    wait(NULL);
}
```

> **对比 C#**: dotnet 的 Kestrel 内部使用 `SocketAsyncEventArgs` + IOCP（Windows）或 epoll（Linux）。多个线程从同一个 IOCP/event loop 取事件，没有惊群问题，因为完成端口天然保证一个 IO 完成只唤醒一个线程。

---

## 三、EPOLLRDHUP vs EPOLLHUP vs EPOLLERR

### 事件区别

```c
// ─── EPOLLRDHUP (Linux 2.6.17+) ───
// 对端关闭连接（对等方发送 FIN）
// 可以继续读缓冲区剩余数据
// 比 EPOLLHUP 更早触发

// ─── EPOLLHUP ───
// 本端挂断（一般配合 EPOLLRDHUP 出现）
// 表示连接彻底关闭，不能再读也不能再写

// ─── EPOLLERR ───
// 连接发生错误（如 RST 收到）
// tcp 的 RST 包会导致 EPOLLERR

// 典型回调处理:
void handle_event(uint32_t events, int fd) {
    // EPOLLRDHUP 先发生（对端优雅关闭）
    if (events & EPOLLRDHUP) {
        // 对端关闭了发送通道
        // 但还可以读剩余数据
        printf("对端关闭（EPOLLRDHUP），读取最后数据\n");
        drain_buffer(fd);  // 读完剩余数据
        shutdown(fd, SHUT_RD);  // 停止读
        // 不立刻 close，等自己发送完
        return;
    }

    // EPOLLIN 正常读事件
    if (events & EPOLLIN) {
        handle_read(fd);
        return;
    }

    // EPOLLHUP 完整关闭
    if (events & EPOLLHUP) {
        printf("连接关闭\n");
        close(fd);
        return;
    }

    // EPOLLERR 错误
    if (events & EPOLLERR) {
        int err;
        socklen_t errlen = sizeof(err);
        getsockopt(fd, SOL_SOCKET, SO_ERROR, &err, &errlen);
        printf("Socket 错误: %s\n", strerror(err));
        close(fd);
        return;
    }
}
```

### 游戏服务器连接管理

```cpp
// EPOLLRDHUP 对游戏服务器非常有用
// 可以快速检测客户端异常关闭（拔网线、程序崩溃）

// 不用 EPOLLRDHUP 只能靠:
// 1. 应用层心跳检测（慢，30-60 秒）
// 2. TCP keepalive（默认 2 小时）
// 3. read 返回 0（但需要发数据才能发现）

// 注册 RDHUP 事件:
struct epoll_event ev;
ev.events = EPOLLIN | EPOLLRDHUP | EPOLLET;
ev.data.ptr = my_connection;  // 用指针而不是 fd
epoll_ctl(epfd, EPOLL_CTL_ADD, fd, &ev);

// 收到 EPOLLRDHUP 后:
// 可以立刻关闭连接，不需要等应用层心跳超时
// 这对需要快速释放游戏服务器资源的场景很重要
```

---

## 四、边缘触发 vs 水平触发：深入选型

### 什么时候用 LT

```c
// LT 场景 1: 你不确定对端会发多少数据
// 例如 HTTP 请求头，不知道大小
void handle_http_request_LT(int fd) {
    char buf[4096];
    int n = read(fd, buf, sizeof(buf));
    if (n > 0) {
        // 没读完 → 下轮 epoll_wait 还会通知
        // 不用循环读，简单安全
        process_http(buf, n);
    }
}
```

### 什么时候必须用 ET

```c
// ET 场景 1: 高吞吐量文件传输
// LT 模式下内核反复通知可读/可写，浪费 CPU

// ET 场景 2: 数据量大且须一次读完
void handle_large_data_ET(int fd) {
    set_nonblock(fd);  // ET 必须非阻塞
    char buf[65536];
    int total = 0;
    while (1) {
        int n = read(fd, buf + total, sizeof(buf) - total);
        if (n > 0) {
            total += n;
            if (total >= (int)sizeof(buf)) {
                // 缓冲区满了，先处理
                process_data(buf, total);
                total = 0;
            }
        } else if (n == 0) {
            // 对端关闭
            break;
        } else {
            if (errno == EAGAIN) {
                // 数据读完了，退出循环
                break;
            }
            // 真实错误
            close(fd);
            return;
        }
    }
    if (total > 0) {
        process_data(buf, total);
    }
}
```

### 游戏服务器推荐策略

```cpp
// 混合方案:
// ── 监听 socket: ET ──
//    在同一时间所有连接一次 accept 完
// ── 客户端 socket: LT ──
//    不容易丢事件，代码简单
// ── 或者: 全 ET + 数据缓冲 ──
//    每个连接关联一个 recv buffer
//    ET 触发后读尽所有数据到 buffer

// 全 ET 方案:

struct Connection {
    int fd;
    char recv_buf[65536];   // 接收缓冲区
    int recv_offset;         // 已接收数据偏移
    char send_buf[65536];    // 发送缓冲区
    int send_offset;         // 已发送偏移
    int send_pending;        // 待发送数据量
};

// ET 读取: 读到 EAGAIN 为止
// ET 写入: 循环写到 EAGAIN
// 不需要 epoll 反复通知，减少系统调用次数

// 性能对比（10000 连接并发测试）:
// LT: 约 120000 epoll_wait 返回/秒
// ET: 约 30000 epoll_wait 返回/秒（少了很多通知）
// 但每次 ET 处理的数据量更大，总吞吐量 ET 高 20-30%
```

---

## 五、epoll 与 io_uring 对比

### io_uring 简介

Linux 5.1+ 引入的异步 IO 框架，解决了 epoll 的两个痛点：

```c
// epoll 的问题:
// 1. 每次操作都要系统调用（epoll_ctl, read, write）
// 2. 系统调用有开销（上下文切换、权限检查）

// io_uring 的方案:
// 1. 共享内存环形队列（mmap 出来的）
// 2. 提交 SQ → 内核处理 → 完成 CQ
// 3. 批量提交、批量收割，大大减少系统调用

#include <liburing.h>

struct io_uring ring;

// 初始化
io_uring_queue_init(4096, &ring, 0);

// 提交一个 read 操作（不需要系统调用，写 SQ）
struct io_uring_sqe* sqe = io_uring_get_sqe(&ring);
io_uring_prep_read(sqe, fd, buf, sizeof(buf), offset);
io_uring_submit(&ring);  // 一次系统调用批量提交

// 等待完成
struct io_uring_cqe* cqe;
io_uring_wait_cqe(&ring, &cqe);
// 处理完成的事件
io_uring_cqe_seen(&ring, cqe);
```

### epoll vs io_uring 对比

| 特性 | epoll | io_uring |
|------|-------|----------|
| 引入版本 | Linux 2.5.44 | Linux 5.1 |
| 系统调用 | 每次操作 1 次 | 批量操作 1 次 |
| 读/写 | 分开 read/write syscall | 合并提交 |
| 磁盘 IO | 不支持（需要 AIO） | 原生支持 |
| 网络 IO | 成熟稳定 | 持续完善(Linux 5.6+) |
| 社区采用 | 所有框架 | 逐渐增加 |
| 编程难度 | 较低 | 较高 |

```c
// io_uring 收网络数据（Linux 5.6+）
// 内核需要配置 CONFIG_IO_URING

void io_uring_echo_server() {
    struct io_uring ring;
    io_uring_queue_init(4096, &ring, 0);

    int listen_fd = setup_listen_socket();
    add_accept(&ring, listen_fd);

    while (1) {
        io_uring_submit_and_wait(&ring);

        struct io_uring_cqe* cqe;
        unsigned head;
        io_uring_for_each_cqe(&ring, head, cqe) {
            struct conn_data* conn = (struct conn_data*)cqe->user_data;
            if (cqe->res < 0) {
                // 错误处理
                close(conn->fd);
                free(conn);
                continue;
            }

            if (conn->type == ACCEPT) {
                // accept 完成
                conn->type = READ;
                io_uring_prep_recv(&ring, conn->fd, conn->buf, 4096, 0);
                add_accept(&ring, listen_fd);  // 继续 accept
            } else if (conn->type == READ) {
                // read 完成，echo 回去
                io_uring_prep_send(&ring, conn->fd, conn->buf, cqe->res, 0);
            } else if (conn->type == WRITE) {
                // write 完成，继续读
                io_uring_prep_recv(&ring, conn->fd, conn->buf, 4096, 0);
            }
        }
        io_uring_cq_advance(&ring, io_uring_cqes(&ring));
    }
}
```

> **C# 建议**: dotnet 仍用 epoll（通过 SocketAsyncEventArgs）。io_uring 在 .NET 8+ 有实验性支持，但生产环境目前还是 epoll。对于新游戏服务器，除非你写 C++ 并且需要极致性能，否则等生态成熟。

---

## 六、多线程 epoll 架构模式

### 模式一：单 Reactor（单线程全处理）

```
单线程:
┌─────────────────────┐
│   epoll_wait        │
│   ├─ accept         │
│   ├─ read → 解包    │
│   ├─ 业务逻辑       │
│   └─ write          │
└─────────────────────┘
```

```c
// 优点: 无锁，简单
// 缺点: 一个连接慢（如 DB 操作）会影响其他连接
// 适合: CPU 密集型、无阻塞操作的小型服务器

// 游戏场景: 仅适合同服帧同步逻辑（所有玩家等同一帧）
// 不适合: 数据库查询、HTTP 调用等耗时操作
```

### 模式二：单 Reactor + Worker 线程池

```
主线程 (Reactor):          Worker 线程池:
┌─────────────────┐      ┌─────────────────┐
│  epoll_wait     │      │  thread 1       │
│  ├─ accept      │ ──→  │  消息解码 + 处理│
│  ├─ read 入队   │      │  thread 2       │
│  └─ write 出队  │      │  消息解码 + 处理│
└─────────────────┘      └─────────────────┘
```

```c
// 主线程: 只处理 IO
void reactor_thread(int epfd) {
    while (1) {
        int nfds = epoll_wait(epfd, events, 1024, -1);
        for (int i = 0; i < nfds; i++) {
            if (events[i].data.fd == listen_fd) {
                accept_client(epfd);
            } else if (events[i].events & EPOLLIN) {
                // 收到数据，投递到 worker 队列
                struct Connection* conn = (struct Connection*)events[i].data.ptr;
                int n = read(conn->fd, conn->read_buf, 4096);
                if (n > 0) {
                    conn->read_len = n;
                    worker_queue_push(conn);  // 线程安全队列
                }
            } else if (events[i].events & EPOLLOUT) {
                // 发送数据（由 worker 填好发送缓冲区后触发）
                write_to_connection(events[i].data.ptr);
            }
        }
    }
}

// Worker 线程: 只处理业务逻辑
void worker_thread() {
    while (1) {
        struct Connection* conn = worker_queue_pop();  // 阻塞
        process_game_message(conn->read_buf, conn->read_len);
        // 处理后把响应塞入发送队列
        // 通知 reactor 可以发送
    }
}
```

### 模式三：多 Reactor（one-loop-per-thread）

```
Thread 1:                  Thread 2:                  Thread 3:
┌──────────────┐          ┌──────────────┐          ┌──────────────┐
│ Main Reactor │          │ Sub Reactor 1│          │ Sub Reactor 2│
│ accept + LB  │ ─follow→ │ epoll_wait   │          │ epoll_wait   │
│ 到 sub      │  连接    │ read/write   │          │ read/write   │
└──────────────┘          └──────────────┘          └──────────────┘
```

```cpp
// 每个线程一个 epoll，一个连接绑定到一个线程
// 连接生命周期内不切换线程（利用线程局部性）

// 主 reactor: 只 accept，然后通过 fd 传递或 SO_REUSEPORT
// 子 reactor: 负责连接的读写

// 利用 SO_REUSEPORT 让内核做负载均衡（Linux 3.9+）
int reuse = 1;
setsockopt(listen_fd, SOL_SOCKET, SO_REUSEPORT, &reuse, sizeof(reuse));

// 每个线程创建自己的 listening socket（同一端口）
// 内核自动分发连接给各个线程的 listen fd

// 优点: 无锁、缓存亲和性好、连接间完全隔离
// 缺点: 连接数不均可能某线程负载高
```

### 游戏服务器选型建议

| 模式 | 复杂度 | 吞吐量 | 延迟 | 推荐场景 |
|------|--------|--------|------|---------|
| 单 Reactor | 低 | 低 | 低 | 原型、帧同步游戏 |
| Reactor + Worker | 中 | 中 | 中 | MMORPG 逻辑服 |
| 多 Reactor | 高 | 高 | 低 | 网关、代理、高并发连接 |

---

## 七、写缓冲区管理与背压

### 背压（Backpressure）问题

```c
// 问题: 服务端处理得快，客户端消费得慢
// 写缓冲区不断累积，最终 OOM

// 错误的写法:
void handle_write(int fd, const char* data, size_t len) {
    // 不检查 EAGAIN，直接 write
    // 如果 socket 缓冲区满，数据积压在内核态
    // 积压到内核缓冲区上限 → write 阻塞 || 返回 EAGAIN
    // 但如果没有正确的背压机制，应用层缓冲区继续膨胀
}
```

### 正确的写缓冲区管理

```cpp
class WriteBuffer {
    static const size_t MAX_BUFFER_SIZE = 4 * 1024 * 1024;  // 4MB
    static const size_t HIGH_WATER_MARK = 3 * 1024 * 1024;  // 3MB

    int fd_;
    std::vector<char> buffer_;       // 待发送数据
    size_t pending_;                 // 未发送字节数
    bool writing_;                   // 是否在写入中

public:
    // 添加数据到写缓冲区
    bool Write(const char* data, size_t len) {
        if (buffer_.size() + len > MAX_BUFFER_SIZE) {
            // 超过最大缓冲 → 触发背压
            // 关闭连接 or 丢弃数据 or 通知上游降速
            LOG_ERROR("Write buffer overflow on fd=%d", fd_);
            // 对于游戏服务器，通常关闭连接
            // 客户端会重连
            return false;
        }

        buffer_.insert(buffer_.end(), data, data + len);
        pending_ += len;

        // 高水位告警
        if (buffer_.size() > HIGH_WATER_MARK) {
            LOG_WARN("Write buffer high water mark: %zu bytes", buffer_.size());
        }

        if (!writing_) {
            writing_ = true;
            Flush();
        }
        return true;
    }

    void Flush() {
        while (!buffer_.empty()) {
            int n = write(fd_, buffer_.data(), buffer_.size());
            if (n > 0) {
                buffer_.erase(buffer_.begin(), buffer_.begin() + n);
                pending_ -= n;
            } else if (n == -1) {
                if (errno == EAGAIN) {
                    // 内核缓冲区满，注册 EPOLLOUT 等待通知
                    // 当内核缓冲区可写时通知
                    break;
                }
                // 真实错误
                Close();
                return;
            }
        }

        if (buffer_.empty() && writing_) {
            writing_ = false;
            // 不关注 EPOLLOUT（减少不必要唤醒）
        }
    }
};
```

> **C# 类比**: `NetworkStream` 的写入没有内置背压。Kestrel 内部有 `Pipe` 机制做流量控制。ASP.NET Core 的 `Pipe.Writer.FlushAsync` 在缓冲区满时等待。C# 开发者通常不需要手动管理写缓冲区。

---

## 八、零拷贝：sendfile / splice

### 传统文件发送路径

```
传统方式（4 次内核/用户态拷贝）:
磁盘 → 内核缓冲区 (DMA)
内核缓冲区 → 用户缓冲区 (read)
用户缓冲区 → socket 缓冲区 (write)
socket 缓冲区 → 网卡 (DMA)

零拷贝（sendfile，2 次拷贝）:
磁盘 → 内核缓冲区 (DMA)
内核缓冲区 → socket 缓冲区 (MMU 映射，不需要 CPU)
socket 缓冲区 → 网卡 (DMA)
```

### sendfile 实现

```c
// 场景: 游戏服务器发送静态文件（补丁包、资源文件）

#include <sys/sendfile.h>

int send_file_to_client(int client_fd, int file_fd, off_t file_size) {
    off_t offset = 0;
    while (offset < file_size) {
        ssize_t n = sendfile(client_fd, file_fd, &offset, file_size - offset);
        if (n > 0) {
            offset += n;
        } else if (n == -1) {
            if (errno == EAGAIN) {
                // 非阻塞，下次再发
                break;
            }
            return -1;
        }
    }
    return 0;
}

// 优势:
// - 数据不经过用户空间缓冲区
// - 减少一次内存拷贝
// - 适合大文件传输（补丁包 > 100MB）
```

### splice 实现两个 fd 间零拷贝

```c
// splice: 在两个文件描述符间移动数据，不经过用户空间
// 场景: 代理服务器转发数据

int proxy_forward(int from_fd, int to_fd) {
    struct pipe_buf {
        int pipefd[2];
        pipe_buf() { pipe2(pipefd, O_NONBLOCK); }
        ~pipe_buf() { close(pipefd[0]); close(pipefd[1]); }
    } pipe;

    while (1) {
        // from_fd → pipe（内核空间）
        ssize_t n = splice(from_fd, nullptr,
                          pipe.pipefd[1], nullptr,
                          65536, SPLICE_F_MOVE);
        if (n <= 0) break;

        // pipe → to_fd（内核空间）
        splice(pipe.pipefd[0], nullptr,
               to_fd, nullptr,
               n, SPLICE_F_MOVE);
    }
    return 0;
}

// 注意: splice 在 Linux 2.6.17+ 可用
// pipe 缓冲区默认 65536 字节
```

### 游戏服务器应用

```c
// 游戏资源下载服务器用 sendfile 减少 CPU 开销
// 网关服务器转发用 splice 减少内存拷贝
// 但游戏实时逻辑数据（玩家位置、战斗）数据量小
// 零拷贝优势不明显，普通 write 就够了

// 对比 C#:
// dotnet 中 FileStream.WriteAsync + Socket.SendAsync
// OS 层面也会做某些优化，但不如 sendfile 直接
// .NET 5+ 有 FileStream.SendFileToSocket 实验性 API
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| epoll_create1(EPOLL_CLOEXEC) | 防止 fork+exec 后 epoll fd 泄漏 |
| EPOLLEXCLUSIVE | 避免多线程/进程惊群，只唤醒一个等待者 |
| EPOLLRDHUP | 对端关闭信号，快速检测连接断开 |
| ET vs LT | ET 高效但复杂必须非阻塞，LT 简单但可能有冗余通知 |
| io_uring | 共享内存环形队列，减少系统调用次数 |
| 多 Reactor | 每个线程独立 epoll，无锁架构适合高并发网关 |
| 背压 | 写缓冲区上限控制，防止 OOM |
| sendfile / splice | 内核态零拷贝，大文件传输省 CPU |

