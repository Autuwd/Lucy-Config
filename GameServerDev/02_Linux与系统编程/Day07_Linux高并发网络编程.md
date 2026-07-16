# Day 07：Linux 高并发网络编程

## 一、Linux IO 模型演进

```
select(max fd = 1024)
    ↓ FD 集合拷贝、内核线性扫描
poll(链表、无上限、仍线性扫描)
    ↓ 性能问题：每次 0(n) 扫描 fd
epoll(事件驱动、变化时才通知)
    ↓ 现代 Linux 上最高效的网络模型
```

### 为什么需要 epoll

| 指标 | select | poll | epoll |
|------|--------|------|-------|
| 最大连接数 | 1024 | 无限制 | 无限制 |
| 扫描方式 | 全部扫描 O(n) | 全部扫描 O(n) | 事件通知 O(1) |
| 内存拷贝 | 每次全量拷贝 | 每次全量拷贝 | 共享内存（mmap） |
| 触发方式 | 水平触发 | 水平触发 | 水平/边缘触发 |
| 平台 | 几乎全平台 | POSIX | Linux only |

---

## 二、epoll 核心 API

### 三级函数

```c
#include <sys/epoll.h>

// 1. 创建 epoll 实例（返回 fd）
int epfd = epoll_create1(0);  // 现代方式
// int epfd = epoll_create(256); // 旧方式

// 2. 注册/修改/删除关注的事件
struct epoll_event ev;
ev.events = EPOLLIN;       // 关注可读事件
ev.data.fd = client_fd;
epoll_ctl(epfd, EPOLL_CTL_ADD, client_fd, &ev);
epoll_ctl(epfd, EPOLL_CTL_DEL, client_fd, NULL);
epoll_ctl(epfd, EPOLL_CTL_MOD, client_fd, &ev); // 修改

// 3. 等待事件（阻塞直到有事件或超时）
struct epoll_event events[1024];
int nfds = epoll_wait(epfd, events, 1024, -1); // -1 = 无限等待
```

### 事件类型

| 标志 | 含义 | 说明 |
|------|------|------|
| `EPOLLIN` | 可读 | 数据到达 |
| `EPOLLOUT` | 可写 | 缓冲区可发送 |
| `EPOLLERR` | 错误 | 连接异常 |
| `EPOLLHUP` | 挂断 | 连接关闭 |
| `EPOLLET` | 边缘触发 | 状态变化才通知（需配合非阻塞） |
| `EPOLLONESHOT` | 一次性 | 事件触发后自动移除（防竞争） |
| `EPOLLRDHUP` | 对端关闭 | 比 EPOLLHUP 更精细 |

---

## 三、完整 epoll 服务器实现 (C)

```c
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/epoll.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <fcntl.h>

#define MAX_EVENTS 1024
#define BUFFER_SIZE 4096
#define PORT 8888

// 连接状态
typedef struct {
    int fd;
    char buffer[BUFFER_SIZE];
    int offset;
} Connection;

// 设置非阻塞
int set_nonblock(int fd) {
    int flags = fcntl(fd, F_GETFL, 0);
    return fcntl(fd, F_SETFL, flags | O_NONBLOCK);
}

// 添加事件
void add_event(int epfd, int fd, uint32_t events) {
    struct epoll_event ev;
    ev.events = events;
    ev.data.fd = fd;
    epoll_ctl(epfd, EPOLL_CTL_ADD, fd, &ev);
}

// 修改事件
void mod_event(int epfd, int fd, uint32_t events) {
    struct epoll_event ev;
    ev.events = events;
    ev.data.fd = fd;
    epoll_ctl(epfd, EPOLL_CTL_MOD, fd, &ev);
}

// 接受新连接
void accept_client(int epfd, int listen_fd) {
    struct sockaddr_in client_addr;
    socklen_t addr_len = sizeof(client_addr);

    // 循环 accept（非阻塞 + ET，一次把所有连接收完）
    while (1) {
        int client_fd = accept(listen_fd,
                               (struct sockaddr*)&client_addr,
                               &addr_len);
        if (client_fd == -1) {
            if (errno == EAGAIN || errno == EWOULDBLOCK)
                break;  // 所有连接已处理完
            perror("accept");
            break;
        }

        set_nonblock(client_fd);

        // 注册 EPOLLIN | EPOLLET（边缘触发 + 非阻塞）
        add_event(epfd, client_fd, EPOLLIN | EPOLLET);

        char ip[INET_ADDRSTRLEN];
        inet_ntop(AF_INET, &client_addr.sin_addr, ip, sizeof(ip));
        printf("新连接: %s:%d (fd=%d)\n",
               ip, ntohs(client_addr.sin_port), client_fd);
    }
}

// 接收数据
void handle_read(int epfd, int fd) {
    char buffer[BUFFER_SIZE];
    int bytes;

    // ET 模式下必须循环读直到 EAGAIN
    while (1) {
        bytes = read(fd, buffer, sizeof(buffer));
        if (bytes > 0) {
            // 处理收到的数据
            printf("收到 %d 字节: %.*s\n", bytes, bytes, buffer);

            // 改为关注写事件，准备发送
            mod_event(epfd, fd, EPOLLOUT | EPOLLET);
            break;
        } else if (bytes == 0) {
            // 客户端关闭
            printf("客户端断开: fd=%d\n", fd);
            close(fd);
            break;
        } else {
            if (errno == EAGAIN || errno == EWOULDBLOCK)
                break;  // 数据读完了
            // 错误
            perror("read");
            close(fd);
            break;
        }
    }
}

// 发送数据
void handle_write(int epfd, int fd) {
    const char* response = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nOK";
    int len = strlen(response);
    int sent = 0;

    while (sent < len) {
        int n = write(fd, response + sent, len - sent);
        if (n > 0) {
            sent += n;
        } else if (n == -1) {
            if (errno == EAGAIN || errno == EWOULDBLOCK)
                break;  // 缓冲区满，下次再写
            close(fd);
            return;
        }
    }

    // 改回关注读事件
    mod_event(epfd, fd, EPOLLIN | EPOLLET);
}

int main() {
    // 创建监听 Socket
    int listen_fd = socket(AF_INET, SOCK_STREAM, 0);
    if (listen_fd == -1) {
        perror("socket");
        return 1;
    }

    // 允许地址重用（解决 TIME_WAIT 问题）
    int reuse = 1;
    setsockopt(listen_fd, SOL_SOCKET, SO_REUSEADDR, &reuse, sizeof(reuse));

    struct sockaddr_in addr;
    memset(&addr, 0, sizeof(addr));
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = INADDR_ANY;
    addr.sin_port = htons(PORT);

    if (bind(listen_fd, (struct sockaddr*)&addr, sizeof(addr)) == -1) {
        perror("bind");
        return 1;
    }

    if (listen(listen_fd, SOMAXCONN) == -1) {
        perror("listen");
        return 1;
    }

    set_nonblock(listen_fd);

    // 创建 epoll
    int epfd = epoll_create1(0);
    if (epfd == -1) {
        perror("epoll_create1");
        return 1;
    }

    // 注册监听 fd（边缘触发）
    add_event(epfd, listen_fd, EPOLLIN | EPOLLET);

    printf("服务器启动，监听端口 %d\n", PORT);

    struct epoll_event events[MAX_EVENTS];

    while (1) {
        int nfds = epoll_wait(epfd, events, MAX_EVENTS, -1);
        if (nfds == -1) {
            if (errno == EINTR) continue; // 被信号中断
            perror("epoll_wait");
            break;
        }

        for (int i = 0; i < nfds; i++) {
            if (events[i].data.fd == listen_fd) {
                // 新连接
                accept_client(epfd, listen_fd);
            } else if (events[i].events & EPOLLIN) {
                // 可读
                handle_read(epfd, events[i].data.fd);
            } else if (events[i].events & EPOLLOUT) {
                // 可写
                handle_write(epfd, events[i].data.fd);
            } else if (events[i].events & (EPOLLERR | EPOLLHUP)) {
                // 错误/挂断
                printf("连接异常: fd=%d\n", events[i].data.fd);
                close(events[i].data.fd);
            }
        }
    }

    close(listen_fd);
    close(epfd);
    return 0;
}
```

---

## 四、水平触发 vs 边缘触发

### LT (Level Triggered) — 水平触发
```
读事件：fd 有数据可读 → 触发 → 你只读了一部分 → 下次 epoll_wait 继续触发
写事件：fd 可写 → 触发 → 不写也一直触发
```

### ET (Edge Triggered) — 边缘触发
```
读事件：fd 从不可读变成可读 → 触发一次 → 你必须一次读完
写事件：fd 从不可写变成可写 → 触发一次
```

### 代码区别

```c
// LT - 不用循环，因为 epoll_wait 还会再通知
// LT 可以阻塞 IO
n = read(fd, buf, sizeof(buf));
// 处理数据

// ET - 必须循环读到 EAGAIN，且必须非阻塞
set_nonblock(fd);
while ((n = read(fd, buf, sizeof(buf))) > 0) {
    // 处理数据
}
if (n == -1 && errno != EAGAIN) {
    // 真实错误
}
```

### 如何选择

| 模式 | 优点 | 缺点 | 场景 |
|------|------|------|------|
| LT | 编程简单，不易丢数据 | 重复通知，性能略低 | 初学者/简单服务器 |
| ET | 通知次数少，吞吐量高 | 必须非阻塞+循环读写，代码复杂 | 高并发服务器 |

**游戏服务器建议**：监听 fd 用 ET，客户端 fd 用 LT（不易丢数据，代码简单），或者全部 ET 加非阻塞模式。

---

## 五、Reactor 模式

### 什么是 Reactor

Reactor 是一种事件驱动设计模式：
- **Reactor**：事件循环，监听 fd，分发事件
- **Handler**：具体的事件处理器

```
                  ┌──────────────┐
                  │   Reactor    │
                  │  (epoll)     │
                  └──────┬───────┘
                         │
          ┌──────────────┼──────────────┐
          │              │              │
    ┌─────▼──────┐ ┌────▼──────┐ ┌─────▼──────┐
    │ Accept     │ │ Read      │ │ Write      │
    │ Handler    │ │ Handler   │ │ Handler    │
    └────────────┘ └───────────┘ └────────────┘
```

### C++ Reactor 框架框架简化

```cpp
#include <functional>
#include <map>
#include <memory>

class EventHandler {
public:
    virtual void HandleEvent(uint32_t events) = 0;
    virtual int GetFd() = 0;
};

class Acceptor : public EventHandler {
    int listen_fd_;
    std::function<void(int)> on_accept_;

public:
    Acceptor(int port) {
        listen_fd_ = socket(AF_INET, SOCK_STREAM, 0);
        // ... bind, listen ...
    }

    void HandleEvent(uint32_t events) override {
        if (events & EPOLLIN) {
            sockaddr_in client_addr;
            socklen_t addr_len = sizeof(client_addr);
            int client_fd = accept(listen_fd_,
                                   (sockaddr*)&client_addr,
                                   &addr_len);
            if (client_fd > 0 && on_accept_)
                on_accept_(client_fd);
        }
    }

    int GetFd() override { return listen_fd_; }
    void SetAcceptCallback(std::function<void(int)> cb) { on_accept_ = cb; }
};

class Reactor {
    int epfd_;
    std::map<int, std::shared_ptr<EventHandler>> handlers_;

public:
    Reactor() { epfd_ = epoll_create1(0); }

    void Register(std::shared_ptr<EventHandler> handler, uint32_t events) {
        handlers_[handler->GetFd()] = handler;
        struct epoll_event ev;
        ev.events = events;
        ev.data.fd = handler->GetFd();
        epoll_ctl(epfd_, EPOLL_CTL_ADD, handler->GetFd(), &ev);
    }

    void Run() {
        struct epoll_event events[1024];
        while (true) {
            int nfds = epoll_wait(epfd_, events, 1024, -1);
            for (int i = 0; i < nfds; i++) {
                auto it = handlers_.find(events[i].data.fd);
                if (it != handlers_.end())
                    it->second->HandleEvent(events[i].events);
            }
        }
    }
};
```

---

## 六、对比 Windows IOCP

| 特性 | Linux epoll | Windows IOCP |
|------|-------------|-------------|
| 内核版本 | Linux 2.5.44+ | Windows NT 3.5+ |
| 线程模型 | 单线程或多线程 epoll_wait | 线程池 + 完成端口 |
| 事件通知 | 事件就绪通知 | 操作完成通知 |
| 读写操作 | read/write 分开调用 | 一次投递，完成回调 |
| 缓冲区管理 | 应用程序管理 | 投递缓冲区，内核管理 |
| 零拷贝 | sendfile/splice | TransmitFile |
| 非阻塞要求 | epoll ET 必须非阻塞 | 不需要（IOCP 本身异步） |
| 公平性 | 无特别保证 | 有优先级和公平性保障 |

### 概念映射

| epoll | IOCP | 说明 |
|-------|------|------|
| `epoll_create1` | `CreateIoCompletionPort` | 创建完成端口 |
| `epoll_ctl` | `CreateIoCompletionPort` (关联) | 注册 fd 到端口 |
| `epoll_wait` | `GetQueuedCompletionStatus` | 等待完成通知 |
| `EPOLLIN/EPOLLOUT` | `WSARecv/WSASend` | 投递 IO 操作 |
| `EPOLLET` | 无直接对应 | IOCP 天生 ET 类似 |
| `accept` | `AcceptEx` | 异步接受连接 |
| `read` → `EPOLLIN` | `WSARecv` → 完成回调 | 一个步进，一个投递 |
| O_NONBLOCK + ET 循环 | 自动处理 | IO 完成方式不同 |

---

## 七、游戏服务器的 epoll 实践

### 混合 IO 线程模型

```c
// 线程 1: epoll 主线程（接收 + 分发）
while (1) {
    nfds = epoll_wait(epfd, events, MAX_EVENTS, -1);
    for (i = 0; i < nfds; i++) {
        if (events[i].data.fd == listen_fd)
            accept_and_distribute();
        else
            dispatch_to_worker(events[i]);
    }
}

// 线程 2-N: Worker 线程（处理消息逻辑）
while (1) {
    packet = recv_queue.dequeue(block);
    process_packet(packet);
    send_queue.enqueue(response);
    eventfd_write(epfd_event_fd, 1); // 通知主线程可写
}
```

### eventfd（epoll 线程间通知）

```c
#include <sys/eventfd.h>

int wake_fd = eventfd(0, EFD_NONBLOCK);

// Worker 线程发送完成，通知 epoll 线程
void notify_epoll() {
    uint64_t one = 1;
    write(wake_fd, &one, sizeof(one));
}

// epoll 线程监听 wake_fd
struct epoll_event ev;
ev.events = EPOLLIN;
ev.data.fd = wake_fd;
epoll_ctl(epfd, EPOLL_CTL_ADD, wake_fd, &ev);

// 在 epoll_wait 的循环中
if (events[i].data.fd == wake_fd) {
    uint64_t val;
    read(wake_fd, &val, sizeof(val)); // 清空
    // 处理发送队列
    flush_send_queue();
}
```

---

## 八、练习

1. **epoll Echo 服务器**：用 C 写一个完整的 epoll Echo 服务器（LT 模式）
2. **ET 改造**：将 LT 改为 ET 模式，注意循环读取的改动
3. **Reactor 框架**：用 C++ 或 C 实现一个简单的 Reactor 模式
4. **压测对比**：分别用 epoll LT / ET + 1000 并发客户端，对比 CPU 使用率
5. **多线程 epoll**：实现一个 main reactor + worker 线程池的模型

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| epoll | Linux 高并发核心，事件驱动，O(1) 通知 |
| epoll_create1 | 创建 epoll 实例 |
| epoll_ctl | 注册/修改/删除关注事件 |
| epoll_wait | 阻塞等待事件发生 |
| ET vs LT | ET 只通知状态变化（高效），LT 一直通知（简单） |
| Reactor | 事件分发模式，epoll 的天然范式 |
| eventfd | 线程间通过 epoll 通信的标准方式 |
