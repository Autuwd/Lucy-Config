# Day 09：C++ Linux 服务端编程

## 一、为什么服务器还要学 C++

虽然有 C# ASP.NET Core 等，但很多核心游戏服务器仍用 C++：
- **性能敏感**：实时对战、帧同步
- **内存控制**：GC 不可控场景
- **已有代码**：老项目多为 C++
- **跨平台**：Android/iOS Native 层

作为后端开发者，**至少能看懂 C++ 服务器代码**。

---

## 二、进程管理

### fork 创建子进程

```cpp
#include <unistd.h>
#include <sys/wait.h>

int main() {
    pid_t pid = fork();

    if (pid == 0) {
        // 子进程
        printf("子进程: PID=%d, PPID=%d\n", getpid(), getppid());
        execl("./worker", "worker", "--id", "1", nullptr);
        // execl 替换子进程为 worker 程序
    } else if (pid > 0) {
        // 父进程
        printf("父进程: 子进程 PID=%d\n", pid);

        int status;
        waitpid(pid, &status, 0); // 等待子进程结束
        printf("子进程退出: status=%d\n", WEXITSTATUS(status));
    } else {
        perror("fork");
        return 1;
    }
    return 0;
}
```

### 守护进程 (Daemon)

```cpp
#include <unistd.h>
#include <sys/stat.h>
#include <fcntl.h>

void daemonize() {
    // 1. 创建子进程，父进程退出
    pid_t pid = fork();
    if (pid > 0) exit(0);  // 父进程退出
    if (pid < 0) exit(1);

    // 2. 创建新会话（脱离终端）
    setsid();

    // 3. 防止获取终端
    signal(SIGHUP, SIG_IGN);
    pid = fork();
    if (pid > 0) exit(0);

    // 4. 设置工作目录
    chdir("/opt/game-server");

    // 5. 重设文件掩码
    umask(0);

    // 6. 关闭标准 IO
    close(STDIN_FILENO);
    close(STDOUT_FILENO);
    close(STDERR_FILENO);

    // 7. 重定向到 /dev/null 或日志文件
    int fd = open("/dev/null", O_RDWR);
    dup2(fd, STDIN_FILENO);
    dup2(fd, STDOUT_FILENO);
    dup2(fd, STDERR_FILENO);
}
```

### 信号处理

```cpp
#include <signal.h>
#include <sys/signalfd.h>

volatile sig_atomic_t g_running = 1;

void signal_handler(int sig) {
    switch (sig) {
    case SIGINT:
    case SIGTERM:
        printf("收到关闭信号\n");
        g_running = 0;
        break;
    case SIGHUP:
        printf("收到重载配置信号\n");
        reload_config();
        break;
    case SIGPIPE:
        // 写已关闭的 Socket 会触发此信号
        // 通常忽略它，让写操作返回 -1
        break;
    }
}

int main() {
    signal(SIGINT, signal_handler);
    signal(SIGTERM, signal_handler);
    signal(SIGHUP, signal_handler);
    signal(SIGPIPE, SIG_IGN); // 忽略 SIGPIPE

    while (g_running) {
        // 主循环
        epoll_wait(...);
    }

    // 清理资源
    cleanup();
    return 0;
}
```

### signalfd（结合 epoll）

```cpp
#include <sys/signalfd.h>

// 通过 epoll 统一管理信号
void setup_signalfd(int epfd) {
    sigset_t mask;
    sigemptyset(&mask);
    sigaddset(&mask, SIGINT);
    sigaddset(&mask, SIGTERM);
    sigaddset(&mask, SIGHUP);
    sigprocmask(SIG_BLOCK, &mask, nullptr);

    int sfd = signalfd(-1, &mask, SFD_NONBLOCK);

    struct epoll_event ev;
    ev.events = EPOLLIN;
    ev.data.fd = sfd;
    epoll_ctl(epfd, EPOLL_CTL_ADD, sfd, &ev);
}

// 在 epoll_wait 循环中
void handle_signal(int sfd) {
    struct signalfd_siginfo info;
    read(sfd, &info, sizeof(info));

    switch (info.ssi_signo) {
    case SIGINT:
    case SIGTERM:
        g_running = false;
        break;
    case SIGHUP:
        reload_config();
        break;
    }
}
```

---

## 三、进程间通信 (IPC)

### 共享内存

```cpp
#include <sys/shm.h>

// 创建/获取共享内存
int shmid = shmget(IPC_PRIVATE, sizeof(GameState), IPC_CREAT | 0666);

// 映射到进程地址空间
GameState* state = (GameState*)shmat(shmid, nullptr, 0);

// 使用
state->player_count = 100;
state->current_frame++;

// 解除映射
shmdt(state);

// 删除
shmctl(shmid, IPC_RMID, nullptr);

// 现代方式: mmap 匿名映射
void* shared_mem = mmap(nullptr, sizeof(GameState),
                        PROT_READ | PROT_WRITE,
                        MAP_SHARED | MAP_ANONYMOUS,
                        -1, 0);
GameState* state = (GameState*)shared_mem;
```

### 信号量

```cpp
#include <semaphore.h>

// 命名信号量（可用于不同进程）
sem_t* sem = sem_open("/game_server_sem", O_CREAT, 0666, 1);

sem_wait(sem);   // P 操作（减 1，如果为 0 则阻塞）
// 临界区
sem_post(sem);   // V 操作（加 1）

sem_close(sem);
sem_unlink("/game_server_sem");

// 匿名信号量（可用于线程间）
sem_t mutex;
sem_init(&mutex, 0, 1); // 第二个参数 0 = 线程间

sem_destroy(&mutex);
```

### 管道

```cpp
// 无名管道（父子进程间）
int pipefd[2];
pipe(pipefd);

pid_t pid = fork();
if (pid == 0) {
    // 子进程：读
    close(pipefd[1]);
    char buf[256];
    read(pipefd[0], buf, sizeof(buf));
} else {
    // 父进程：写
    close(pipefd[0]);
    write(pipefd[1], "hello", 5);
}
```

---

## 四、RAII 与资源管理

### 为什么 RAII 重要

C++ 中资源管理容易犯错（内存泄漏、句柄泄漏、锁未释放）。RAII (Resource Acquisition Is Initialization) 将资源生命周期绑定到对象生命周期。

```cpp
// 坏：手动管理
void bad_example() {
    int* data = new int[1000];
    FILE* file = fopen("save.dat", "w");
    // ... 操作 ...
    // 如果中间抛出异常，下面两行不会执行！
    delete[] data;
    fclose(file);
}

// 好：RAII 封装
class FileGuard {
    FILE* file_;
public:
    FileGuard(const char* path, const char* mode) {
        file_ = fopen(path, mode);
        if (!file_) throw std::runtime_error("无法打开文件");
    }
    ~FileGuard() { if (file_) fclose(file_); }
    FILE* get() const { return file_; }
    // 禁止拷贝
    FileGuard(const FileGuard&) = delete;
    FileGuard& operator=(const FileGuard&) = delete;
};

void good_example() {
    auto data = std::make_unique<int[]>(1000);
    FileGuard file("save.dat", "w");
    // ... 操作 ... 异常安全！
} // 自动释放
```

### 网络编程中的 RAII

```cpp
class SocketGuard {
    int fd_;
public:
    SocketGuard() {
        fd_ = socket(AF_INET, SOCK_STREAM, 0);
        if (fd_ < 0) throw std::system_error(errno, std::generic_category());
    }
    ~SocketGuard() {
        if (fd_ >= 0) close(fd_);
    }
    int get() const { return fd_; }
    SocketGuard(SocketGuard&& other) : fd_(other.fd_) { other.fd_ = -1; }
};

class EPOLLGuard {
    int epfd_;
public:
    EPOLLGuard() {
        epfd_ = epoll_create1(0);
        if (epfd_ < 0) throw std::system_error(errno, std::generic_category());
    }
    ~EPOLLGuard() { if (epfd_ >= 0) close(epfd_); }
    int get() const { return epfd_; }
};
```

---

## 五、C++ 网络库对比

### Boost.Asio / standalone Asio

```cpp
#include <boost/asio.hpp>

namespace asio = boost::asio;
using asio::ip::tcp;

class Server {
    asio::io_context io_context_;
    tcp::acceptor acceptor_;

public:
    Server(int port) : acceptor_(io_context_, tcp::endpoint(tcp::v4(), port)) {
        start_accept();
    }

    void run() { io_context_.run(); }

private:
    void start_accept() {
        auto socket = std::make_shared<tcp::socket>(io_context_);
        acceptor_.async_accept(*socket, [this, socket](auto error) {
            if (!error) {
                std::cout << "新连接\n";
                start_read(socket);
            }
            start_accept(); // 继续接受下一个
        });
    }

    void start_read(std::shared_ptr<tcp::socket> socket) {
        auto buffer = std::make_shared<std::vector<char>>(1024);
        socket->async_read_some(asio::buffer(*buffer),
            [this, socket, buffer](auto error, size_t length) {
                if (!error) {
                    std::cout << "收到 " << length << " 字节\n";
                    start_read(socket);
                }
            });
    }
};

int main() {
    try {
        Server server(8888);
        server.run();
    } catch (std::exception& e) {
        std::cerr << e.what() << '\n';
    }
}
```

### libuv (Node.js 底层)

```cpp
#include <uv.h>

uv_loop_t* loop;
uv_tcp_t server;

void on_connect(uv_stream_t* server, int status) {
    auto client = new uv_tcp_t;
    uv_tcp_init(loop, client);
    uv_accept(server, (uv_stream_t*)client);

    uv_read_start((uv_stream_t*)client, alloc_buffer, on_read);
}

void on_read(uv_stream_t* client, ssize_t nread, const uv_buf_t* buf) {
    if (nread > 0) {
        printf("收到: %.*s\n", (int)nread, buf->base);
    }
    free(buf->base);
}

int main() {
    loop = uv_default_loop();

    uv_tcp_init(loop, &server);
    struct sockaddr_in addr;
    uv_ip4_addr("0.0.0.0", 8888, &addr);
    uv_tcp_bind(&server, (const struct sockaddr*)&addr, 0);
    uv_listen((uv_stream_t*)&server, SOMAXCONN, on_connect);

    uv_run(loop, UV_RUN_DEFAULT);
    return 0;
}
```

### 网络库对比

| 库 | 底层 | 平台 | 特点 | 适合 |
|----|------|------|------|------|
| Boost.Asio | epoll/IOCP | 跨平台 | C++ 标准提案 | 大型 C++ 项目 |
| libuv | epoll/IOCP/kqueue | 跨平台 | Node.js 底层 | 看重跨平台 |
| libevent | epoll/poll/select | 跨平台 | 轻量，事件驱动 | 传统 C 项目 |
| seastar | DPDK + epoll | Linux | 无共享架构 | 高性能微服务 |
| Workflow | epoll | Linux | 腾讯开源 | 国内大厂项目 |

---

## 六、GDB 调试

### 基础命令

```bash
# 编译带调试信息
g++ -g -O0 server.cpp -o server

# 启动调试
gdb ./server

# 常用命令
run               # 运行
bt                # 查看堆栈
frame 3           # 切换到第 3 帧
info locals       # 查看局部变量
print var         # 打印变量
list              # 显示源代码
break main.cpp:42 # 断点
continue          # 继续运行
next              # 下一行（跳过函数）
step              # 进入函数
finish            # 运行到函数返回
watch var         # 观察变量变化

# 调试 core dump
ulimit -c unlimited
./server
gdb ./server core.12345
```

### 附加到运行中进程

```bash
# 找到 PID
ps aux | grep server

# 附加
gdb -p 12345

# 在 GDB 中设置断点后继续
(gdb) break handle_packet
(gdb) continue

# 调试线程
(gdb) info threads
(gdb) thread 2
(gdb) bt
```

---

## 七、对比 C# 服务端

| C# | C++ | 说明 |
|---|-----|------|
| `GC` 自动回收 | `RAII` / `shared_ptr` / 手动 | 内存管理 |
| `async/await` | `asio::co_spawn` (C++20) | 异步编程 |
| `Task` | `std::future` / `asio::any_io_executor` | 异步结果 |
| `ConcurrentDictionary` | `std::map + mutex` | 并发容器 |
| `SocketAsyncEventArgs` | `struct PER_IO_CONTEXT + OVERLAPPED` | IO 完成回调 |
| `lock` | `std::lock_guard` | 锁 |
| `Interlocked.Increment` | `__sync_fetch_and_add` / `std::atomic` | 原子操作 |
| `Thread.Sleep` | `std::this_thread::sleep_for` | 线程休眠 |
| `Console.WriteLine` | `std::cout / printf / spdlog` | 日志输出 |
| `Exception` | `std::exception` | 异常处理 |
| 运行时不崩溃 | 出错可能段错误 | 稳定性保障 |

---

## 八、练习

1. **RAII 封装**：将 epoll fd、socket fd 用 RAII 类封装
2. **进程管理**：实现一个 gateway 主进程，fork 出 3 个 worker 子进程
3. **共享内存**：用共享内存实现一个跨进程计数器
4. **Asio Echo 服务器**：用 Boost.Asio 写一个 TCP Echo 服务器
5. **GDB 调试**：写一个带 bug 的服务器，用 GDB 定位段错误

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| fork | 创建子进程，子进程完全复制父进程 |
| Daemon | 脱离终端的后台进程 |
| RAII | 资源绑定到对象生命周期，异常安全 |
| 共享内存 | 多个进程访问同一块物理内存 |
| 信号量 | P/V 操作同步进程间访问 |
| Boost.Asio | C++ 异步网络编程标准库 |
| GDB | Linux C++ 调试的唯一标准 |
