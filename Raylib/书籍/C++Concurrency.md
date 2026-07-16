# C++ Concurrency in Action 学习笔记

> **作者**：Anthony Williams
> **地位**：C++ 多线程编程的权威指南

---

## 📋 目录

1. [线程管理](#线程管理)
2. [共享数据](#共享数据)
3. [同步并发操作](#同步并发操作)
4. [C++ 内存模型和原子操作](#c-内存模型和原子操作)
5. [并发操作设计](#并发操作设计)
6. [高级线程管理](#高级线程管理)
7. [并发代码设计](#并发代码设计)
8. [难点解析](#难点解析)
9. [Unity 对照](#unity-对照)

---

## 线程管理

### 启动线程

```cpp
#include <thread>
#include <iostream>

// ✅ 基本线程启动
void Function() {
    std::cout << "Hello from thread!" << std::endl;
}

int main() {
    std::thread t(Function);
    t.join();  // 等待线程结束
    return 0;
}

// ✅ Lambda 启动线程
int main() {
    std::thread t([]() {
        std::cout << "Hello from Lambda!" << std::endl;
    });
    t.join();
    return 0;
}

// ✅ 带参数的线程
void Function(int x, std::string s) {
    std::cout << x << ": " << s << std::endl;
}

int main() {
    std::thread t(Function, 42, "Hello");
    t.join();
    return 0;
}

// ⚠️ 重要：始终 join 或 detach
// 否则程序终止时会调用 std::terminate()
```

### 等待线程完成

```cpp
// ✅ join：等待线程结束
std::thread t(Function);
t.join();  // 阻塞直到线程结束

// ✅ detach：分离线程
std::thread t(Function);
t.detach();  // 线程在后台运行

// ✅ 检查线程是否可 join
std::thread t(Function);
if (t.joinable()) {
    t.join();
}

// ✅ 超时等待（C++11 不支持，C++20 支持）
// std::jthread 自动 join
std::jthread t(Function);  // 析构时自动 join
```

### 特殊情况下的线程管理

```cpp
// ✅ 后台线程
void BackgroundTask() {
    // 长时间运行的任务
}

int main() {
    std::thread t(BackgroundTask);
    t.detach();  // 分离线程
    
    // 主线程继续执行
    return 0;
}

// ✅ 中断线程
std::atomic<bool> stop{false};

void InterruptibleTask() {
    while (!stop) {
        // 工作...
    }
}

int main() {
    std::thread t(InterruptibleTask);
    
    // 请求停止
    stop = true;
    t.join();
    
    return 0;
}

// ✅ C++20：使用 std::jthread
std::jthread t([](std::stop_token token) {
    while (!token.stop_requested()) {
        // 工作...
    }
});

// 自动 join
```

---

## 共享数据

### 互斥锁

```cpp
#include <mutex>
#include <vector>

std::mutex mtx;
std::vector<int> data;

// ✅ 使用 std::lock_guard
void AddData(int value) {
    std::lock_guard<std::mutex> lock(mtx);  // RAII 锁
    data.push_back(value);
}

// ✅ 使用 std::unique_lock
void ProcessData() {
    std::unique_lock<std::mutex> lock(mtx);
    
    if (data.empty()) {
        lock.unlock();  // 临时解锁
        // 处理其他事情...
        lock.lock();    // 重新加锁
    }
    
    // 处理数据...
}

// ⚠️ 死锁
// 两个线程互相等待对方释放锁
// 解决方案：总是以相同顺序加锁
```

### 死锁预防

```cpp
// ❌ 死锁示例
std::mutex mtx1, mtx2;

void Thread1() {
    std::lock_guard<std::mutex> lock1(mtx1);
    std::lock_guard<std::mutex> lock2(mtx2);
    // 工作...
}

void Thread2() {
    std::lock_guard<std::mutex> lock1(mtx2);  // 顺序相反！
    std::lock_guard<std::mutex> lock2(mtx1);
    // 工作...
}

// ✅ 解决方案 1：固定加锁顺序
void Thread1() {
    std::lock_guard<std::mutex> lock1(mtx1);
    std::lock_guard<std::mutex> lock2(mtx2);
}

void Thread2() {
    std::lock_guard<std::mutex> lock1(mtx1);  // 相同顺序
    std::lock_guard<std::mutex> lock2(mtx2);
}

// ✅ 解决方案 2：同时加锁
void Thread1() {
    std::lock(mtx1, mtx2);  // 同时加锁
    std::lock_guard<std::mutex> lock1(mtx1, std::adopt_lock);
    std::lock_guard<std::mutex> lock2(mtx2, std::adopt_lock);
}

// ✅ 解决方案 3：使用 std::scoped_lock（C++17）
void Thread1() {
    std::scoped_lock lock(mtx1, mtx2);  // 自动处理死锁
}
```

### 条件变量

```cpp
#include <condition_variable>

std::mutex mtx;
std::condition_variable cv;
bool ready = false;

// ✅ 生产者-消费者模式
void Producer() {
    std::unique_lock<std::mutex> lock(mtx);
    ready = true;
    cv.notify_one();  // 通知消费者
}

void Consumer() {
    std::unique_lock<std::mutex> lock(mtx);
    cv.wait(lock, []() { return ready; });  // 等待条件
    // 处理数据...
}

// ✅ 使用 wait_for
void WaitForData() {
    std::unique_lock<std::mutex> lock(mtx);
    if (cv.wait_for(lock, std::chrono::seconds(1), []() { return ready; })) {
        // 条件满足
    } else {
        // 超时
    }
}
```

---

## 同步并发操作

### 期望值和承诺

```cpp
#include <future>

// ✅ std::promise 和 std::future
std::promise<int> promise;
std::future<int> future = promise.get_future();

std::thread t([&promise]() {
    promise.set_value(42);  // 设置结果
});

int value = future.get();  // 获取结果（阻塞）
std::cout << value << std::endl;  // 42

t.join();

// ✅ std::async
std::future<int> f = std::async(std::launch::async, []() {
    return 42;
});

int result = f.get();  // 获取结果

// ✅ std::packaged_task
std::packaged_task<int(int, int)> task([](int a, int b) {
    return a + b;
});

std::future<int> result = task.get_future();
task(1, 2);  // 执行任务

std::cout << result.get() << std::endl;  // 3
```

### 任务包装器和异常

```cpp
// ✅ 异常处理
std::promise<int> promise;
std::future<int> future = promise.get_future();

std::thread t([&promise]() {
    try {
        throw std::runtime_error("Error");
    } catch (...) {
        promise.set_exception(std::current_exception());  // 传递异常
    }
});

try {
    future.get();  // 重新抛出异常
} catch (const std::exception &e) {
    std::cerr << e.what() << std::endl;
}

t.join();

// ✅ std::async 的异常处理
auto f = std::async(std::launch::async, []() {
    throw std::runtime_error("Error");
});

try {
    f.get();
} catch (const std::exception &e) {
    std::cerr << e.what() << std::endl;
}
```

### 等待条件

```cpp
// ✅ 条件变量
std::mutex mtx;
std::condition_variable cv;
bool ready = false;

void Producer() {
    std::unique_lock<std::mutex> lock(mtx);
    ready = true;
    cv.notify_one();
}

void Consumer() {
    std::unique_lock<std::mutex> lock(mtx);
    cv.wait(lock, []() { return ready; });
}

// ✅ 超时等待
void WaitForData() {
    std::unique_lock<std::mutex> lock(mtx);
    if (cv.wait_for(lock, std::chrono::seconds(1), []() { return ready; })) {
        // 条件满足
    } else {
        // 超时
    }
}
```

### 期望值和承诺的高级用法

```cpp
// ✅ 获取多个结果
std::promise<int> p1, p2;
std::future<int> f1 = p1.get_future();
std::future<int> f2 = p2.get_future();

std::thread t1([&p1]() { p1.set_value(1); });
std::thread t2([&p2]() { p2.set_value(2); });

// 使用 std::when_all（C++17）
// 或者手动管理多个 future

t1.join();
t2.join();

// ✅ 链式调用
std::future<int> f = std::async(std::launch::async, []() {
    return 42;
});

// C++17：std::future::then
// f.then([](std::future<int> f) {
//     return f.get() * 2;
// });
```

---

## C++ 内存模型和原子操作

### 内存模型

```cpp
// ✅ 内存模型基础
// 1. 顺序一致性：所有线程看到相同的执行顺序
// 2. 松弛一致性：不同线程可能看到不同的执行顺序

// ✅ 原子操作
std::atomic<int> counter{0};

void Increment() {
    counter++;  // 原子操作
}

// ✅ 非原子操作
int counter = 0;

void Increment() {
    counter++;  // 非原子操作，可能竞争
}

// ⚠️ 数据竞争
// 两个线程同时访问同一数据，至少一个写入
// 结果未定义
```

### 原子操作

```cpp
#include <atomic>

// ✅ 基本原子操作
std::atomic<int> a{0};
std::atomic<bool> b{false};
std::atomic<double> c{0.0};

// ✅ 原子操作
a++;                    // 原子递增
a.fetch_add(1);         // 原子加
a.load();               // 原子读取
a.store(10);            // 原子存储

// ✅ CAS 操作
int expected = 0;
int desired = 1;
a.compare_exchange_strong(expected, desired);  // 比较交换

// ✅ 原子标志
std::atomic_flag flag = ATOMIC_FLAG_INIT;

void SetFlag() {
    flag.test_and_set();  // 设置标志
}

void Wait() {
    while (!flag.test_and_set()) {
        // 等待
    }
}
```

### 内存序

```cpp
// ✅ 内存序类型
// 1. memory_order_relaxed：宽松序
// 2. memory_order_acquire：获取序
// 3. memory_order_release：发布序
// 4. memory_order_acq_rel：获取-发布序
// 5. memory_order_seq_cst：顺序一致序（默认）

// ✅ 使用示例
std::atomic<int> data{0};
std::atomic<bool> ready{false};

// 发布者
data.store(42, std::memory_order_release);
ready.store(true, std::memory_order_release);

// 获取者
while (!ready.load(std::memory_order_acquire)) {
    // 等待
}
int value = data.load(std::memory_order_acquire);  // 42

// 💡 内存序选择
// 1. 默认使用 seq_cst
// 2. 需要性能时，考虑其他内存序
// 3. 总是进行正确的内存序分析
```

### 原子操作的同步

```cpp
// ✅ 同步关系
// 1. 同步：一个操作发生在另一个操作之前
// 2. 获取-发布：发布操作同步于获取操作
// 3. 顺序一致：所有线程看到相同的执行顺序

// ✅ 使用示例
std::atomic<int> data{0};
std::atomic<bool> ready{false};

// 发布者
data.store(42, std::memory_order_release);  // 发布
ready.store(true, std::memory_order_release);  // 发布

// 获取者
while (!ready.load(std::memory_order_acquire)) {  // 获取
    // 等待
}
int value = data.load(std::memory_order_acquire);  // 获取

// 💡 同步规则
// 1. 发布-获取：发布操作同步于获取操作
// 2. 顺序一致：所有线程看到相同的执行顺序
// 3. 松弛：没有同步关系
```

---

## 并发操作设计

### 线程池

```cpp
#include <thread>
#include <queue>
#include <functional>
#include <mutex>
#include <condition_variable>

class ThreadPool {
public:
    ThreadPool(size_t num_threads) {
        for (size_t i = 0; i < num_threads; i++) {
            workers_.emplace_back([this]() {
                while (true) {
                    std::function<void()> task;
                    
                    {
                        std::unique_lock<std::mutex> lock(mtx_);
                        cv_.wait(lock, [this]() {
                            return stop_ || !tasks_.empty();
                        });
                        
                        if (stop_ && tasks_.empty()) {
                            return;
                        }
                        
                        task = std::move(tasks_.front());
                        tasks_.pop();
                    }
                    
                    task();
                }
            });
        }
    }
    
    ~ThreadPool() {
        {
            std::lock_guard<std::mutex> lock(mtx_);
            stop_ = true;
        }
        
        cv_.notify_all();
        
        for (auto &worker : workers_) {
            worker.join();
        }
    }
    
    template <typename F>
    void Enqueue(F &&f) {
        {
            std::lock_guard<std::mutex> lock(mtx_);
            tasks_.emplace(std::forward<F>(f));
        }
        cv_.notify_one();
    }
    
private:
    std::vector<std::thread> workers_;
    std::queue<std::function<void()>> tasks_;
    std::mutex mtx_;
    std::condition_variable cv_;
    bool stop_ = false;
};

// ✅ 使用
ThreadPool pool(4);

for (int i = 0; i < 8; i++) {
    pool.Enqueue([i]() {
        std::cout << "Task " << i << " running on thread " 
                  << std::this_thread::get_id() << std::endl;
    });
}

std::this_thread::sleep_for(std::chrono::seconds(1));
```

### 并行算法

```cpp
#include <algorithm>
#include <vector>
#include <thread>

// ✅ 并行 for_each
template <typename Iterator, typename Func>
void ParallelForEach(Iterator first, Iterator last, Func f) {
    unsigned long distance = std::distance(first, last);
    unsigned long num_threads = std::thread::hardware_concurrency();
    
    if (distance < num_threads * 2) {
        std::for_each(first, last, f);
        return;
    }
    
    std::vector<std::thread> threads;
    Iterator block_start = first;
    
    for (unsigned long i = 0; i < num_threads - 1; i++) {
        Iterator block_end = block_start;
        std::advance(block_end, distance / num_threads);
        
        threads.emplace_back([block_start, block_end, f]() {
            std::for_each(block_start, block_end, f);
        });
        
        block_start = block_end;
    }
    
    std::for_each(block_start, last, f);
    
    for (auto &thread : threads) {
        thread.join();
    }
}

// ✅ 使用
std::vector<int> v = {1, 2, 3, 4, 5, 6, 7, 8};
ParallelForEach(v.begin(), v.end(), [](int &x) {
    x *= 2;
});
```

### 并行归约

```cpp
#include <numeric>
#include <vector>
#include <thread>

// ✅ 并行 reduce
template <typename Iterator, typename T>
T ParallelReduce(Iterator first, Iterator last, T init) {
    unsigned long distance = std::distance(first, last);
    unsigned long num_threads = std::thread::hardware_concurrency();
    
    if (distance < num_threads * 2) {
        return std::accumulate(first, last, init);
    }
    
    std::vector<T> results(num_threads);
    std::vector<std::thread> threads;
    Iterator block_start = first;
    
    for (unsigned long i = 0; i < num_threads - 1; i++) {
        Iterator block_end = block_start;
        std::advance(block_end, distance / num_threads);
        
        threads.emplace_back([block_start, block_end, &results, i]() {
            results[i] = std::accumulate(block_start, block_end, T{});
        });
        
        block_start = block_end;
    }
    
    threads.emplace_back([block_start, last, &results, num_threads - 1]() {
        results[num_threads - 1] = std::accumulate(block_start, last, T{});
    });
    
    for (auto &thread : threads) {
        thread.join();
    }
    
    return std::accumulate(results.begin(), results.end(), init);
}

// ✅ 使用
std::vector<int> v = {1, 2, 3, 4, 5, 6, 7, 8};
int sum = ParallelReduce(v.begin(), v.end(), 0);
```

---

## 高级线程管理

### 线程本地存储

```cpp
#include <thread>

// ✅ thread_local 变量
thread_local int thread_id = 0;

void Function() {
    thread_id++;
    std::cout << "Thread ID: " << thread_id << std::endl;
}

int main() {
    std::thread t1(Function);
    std::thread t2(Function);
    
    t1.join();
    t2.join();
    
    return 0;
}

// 💡 线程本地存储
// 每个线程有自己的副本
// 线程之间不共享
// 适用于线程特定的数据
```

### 线程中断

```cpp
#include <thread>
#include <atomic>

std::atomic<bool> stop{false};

void InterruptibleTask() {
    while (!stop) {
        // 工作...
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }
}

int main() {
    std::thread t(InterruptibleTask);
    
    // 运行一段时间后停止
    std::this_thread::sleep_for(std::chrono::seconds(1));
    stop = true;
    
    t.join();
    return 0;
}

// ✅ C++20：使用 std::jthread
std::jthread t([](std::stop_token token) {
    while (!token.stop_requested()) {
        // 工作...
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }
});

// 请求停止
t.request_stop();

// 自动 join
```

### 线程取消

```cpp
#include <thread>
#include <atomic>

std::atomic<bool> cancelled{false};

void CancellableTask() {
    while (!cancelled) {
        // 检查点
        if (cancelled) {
            break;
        }
        
        // 工作...
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }
}

int main() {
    std::thread t(CancellableTask);
    
    // 请求取消
    cancelled = true;
    
    t.join();
    return 0;
}
```

---

## 并发代码设计

### 基于锁的并发数据结构

```cpp
#include <mutex>
#include <vector>

// ✅ 线程安全的栈
template <typename T>
class ThreadSafeStack {
public:
    void Push(T value) {
        std::lock_guard<std::mutex> lock(mtx_);
        data_.push_back(value);
    }
    
    T Pop() {
        std::lock_guard<std::mutex> lock(mtx_);
        if (data_.empty()) {
            throw std::runtime_error("Stack is empty");
        }
        
        T value = data_.back();
        data_.pop_back();
        return value;
    }
    
    bool Empty() const {
        std::lock_guard<std::mutex> lock(mtx_);
        return data_.empty();
    }
    
private:
    std::vector<T> data_;
    mutable std::mutex mtx_;
};
```

### 无锁并发数据结构

```cpp
#include <atomic>

// ✅ 无锁栈
template <typename T>
class LockFreeStack {
public:
    void Push(T value) {
        Node *new_node = new Node(value);
        new_node->next = head_.load();
        
        while (!head_.compare_exchange_weak(new_node->next, new_node)) {
            // 重试
        }
    }
    
    T Pop() {
        Node *old_head = head_.load();
        
        while (old_head && !head_.compare_exchange_weak(old_head, old_head->next)) {
            // 重试
        }
        
        if (old_head) {
            T value = old_head->data;
            return value;
        }
        
        throw std::runtime_error("Stack is empty");
    }
    
private:
    struct Node {
        T data;
        Node *next;
        
        Node(T value) : data(value), next(nullptr) {}
    };
    
    std::atomic<Node *> head_{nullptr};
};
```

---

## 难点解析

### 🔴 难点 1：死锁

```cpp
// ❌ 死锁示例
std::mutex mtx1, mtx2;

void Thread1() {
    std::lock_guard<std::mutex> lock1(mtx1);
    std::lock_guard<std::mutex> lock2(mtx2);
}

void Thread2() {
    std::lock_guard<std::mutex> lock1(mtx2);  // 顺序相反！
    std::lock_guard<std::mutex> lock2(mtx1);
}

// ✅ 解决方案 1：固定加锁顺序
void Thread1() {
    std::lock_guard<std::mutex> lock1(mtx1);
    std::lock_guard<std::mutex> lock2(mtx2);
}

void Thread2() {
    std::lock_guard<std::mutex> lock1(mtx1);  // 相同顺序
    std::lock_guard<std::mutex> lock2(mtx2);
}

// ✅ 解决方案 2：同时加锁
void Thread1() {
    std::lock(mtx1, mtx2);
    std::lock_guard<std::mutex> lock1(mtx1, std::adopt_lock);
    std::lock_guard<std::mutex> lock2(mtx2, std::adopt_lock);
}

// ✅ 解决方案 3：使用 std::scoped_lock（C++17）
void Thread1() {
    std::scoped_lock lock(mtx1, mtx2);
}
```

### 🔴 难点 2：数据竞争

```cpp
// ❌ 数据竞争
int counter = 0;

void Increment() {
    counter++;  // 非原子操作
}

std::thread t1(Increment);
std::thread t2(Increment);

// 两个线程同时执行 counter++
// 结果未定义

// ✅ 解决方案 1：使用互斥锁
std::mutex mtx;
int counter = 0;

void Increment() {
    std::lock_guard<std::mutex> lock(mtx);
    counter++;
}

// ✅ 解决方案 2：使用原子操作
std::atomic<int> counter{0};

void Increment() {
    counter++;  // 原子操作
}
```

### 🔴 难点 3：内存序

```cpp
// ✅ 内存序类型
// 1. memory_order_relaxed：宽松序
// 2. memory_order_acquire：获取序
// 3. memory_order_release：发布序
// 4. memory_order_acq_rel：获取-发布序
// 5. memory_order_seq_cst：顺序一致序（默认）

// ✅ 使用示例
std::atomic<int> data{0};
std::atomic<bool> ready{false};

// 发布者
data.store(42, std::memory_order_release);  // 发布
ready.store(true, std::memory_order_release);  // 发布

// 获取者
while (!ready.load(std::memory_order_acquire)) {  // 获取
    // 等待
}
int value = data.load(std::memory_order_acquire);  // 获取

// 💡 内存序选择
// 1. 默认使用 seq_cst
// 2. 需要性能时，考虑其他内存序
// 3. 总是进行正确的内存序分析
```

### 🔴 难点 4：条件变量虚假唤醒

```cpp
// ❌ 虚假唤醒
std::mutex mtx;
std::condition_variable cv;
bool ready = false;

void Wait() {
    std::unique_lock<std::mutex> lock(mtx);
    cv.wait(lock);  // ❌ 可能虚假唤醒
    // 处理数据...
}

// ✅ 使用谓词
void Wait() {
    std::unique_lock<std::mutex> lock(mtx);
    cv.wait(lock, []() { return ready; });  // ✅ 使用谓词
    // 处理数据...
}

// 💡 原则
// 1. 总是使用谓词
// 2. 谓词应该是无副作用的
// 3. 谓词应该快速返回
```

---

## Unity 对照

### 概念映射

| C++ Concurrency | Unity 对应 | 说明 |
|-----------------|------------|------|
| std::thread | Unity Jobs | 并发执行 |
| std::mutex | Unity JobHandle | 同步机制 |
| std::atomic | Unity Burst Compiler | 原子操作 |
| std::condition_variable | Unity 事件系统 | 条件等待 |
| 线程池 | Unity Job System | 线程管理 |

### Unity 中的应用

```cpp
// 1. Unity Jobs System
// Unity 的 Jobs System 是基于 C++ 的并发 API
// 理解 C++ 并发 API 可以帮助理解 Unity Jobs

// 2. Burst Compiler
// Unity 的 Burst Compiler 将 C# 编译成优化的机器码
// 底层使用 C++ 原子操作和内存模型

// 3. 多线程渲染
// Unity 的渲染是多线程的
// 理解 C++ 并发 API 可以帮助理解渲染管线

// 💡 学习建议
// 1. 先理解 C++ 并发 API
// 2. 对比 Unity Jobs System
// 3. 思考为什么 Unity 要这样设计
// 4. 将 C++ 知识应用到 Unity 开发中
```

### 代码对比

```cpp
// C++ 并发
std::thread t([]() {
    // 工作...
});
t.join();

// Unity C# Jobs
IJob job = new MyJob();
JobHandle handle = job.Schedule();
handle.Complete();

// 💡 区别
// C++：手动管理线程
// Unity：Job System 自动管理线程
// Unity：Burst Compiler 优化性能
```

---

## 📝 学习建议

### 阅读顺序
1. 先读线程管理（基本概念）
2. 再读共享数据（互斥锁和条件变量）
3. 然后读原子操作和内存模型
4. 最后读高级主题

### 实践方法
1. 每学一个概念，写一个示例
2. 对比 Unity 的实现方式
3. 思考为什么并发编程这么重要
4. 将并发知识应用到项目中

### 常见错误
1. 忘记 join 或 detach
2. 死锁
3. 数据竞争
4. 虚假唤醒

### 推荐练习
1. 实现一个线程安全的栈
2. 实现一个线程池
3. 实现一个并行算法
4. 实现一个无锁数据结构

---

*本文件持续更新中...*
