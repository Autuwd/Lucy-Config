# C++17/20 Complete Guide 学习笔记

> **作者**：Nicolai M. Josuttis
> **地位**：C++17/20 新特性详解

---

## 📋 目录

1. [C++17 核心特性](#c17-核心特性)
2. [C++17 标准库](#c17-标准库)
3. [C++20 核心特性](#c20-核心特性)
4. [C++20 标准库](#c20-标准库)
5. [难点解析](#难点解析)
6. [Unity 对照](#unity-对照)

---

## C++17 核心特性

### 结构化绑定

```cpp
// ✅ 结构化绑定
auto [x, y, z] = std::tuple{1, 2.0, "hello"};
std::cout << x << ", " << y << ", " << z << std::endl;

// ✅ 用于 pair
std::map<std::string, int> m = {{"a", 1}, {"b", 2}};
for (auto [key, value] : m) {
    std::cout << key << ": " << value << std::endl;
}

// ✅ 用于数组
int arr[] = {1, 2, 3};
auto [a, b, c] = arr;

// ✅ 用于结构体
struct Point {
    float x, y;
};

Point p{1.0f, 2.0f};
auto [x, y] = p;

// 💡 结构化绑定的优点
// 1. 更清晰的代码
// 2. 避免使用 .first/.second
// 3. 更安全的解构
```

### if 初始化语句

```cpp
// ✅ if 初始化语句
if (auto result = Compute(); result > 0) {
    std::cout << "Success: " << result << std::endl;
} else {
    std::cout << "Failed" << std::endl;
}

// ✅ 用于错误处理
if (auto result = OpenFile(); !result) {
    return result.error();
}

// ✅ 用于循环
for (auto it = container.begin(); it != container.end(); ++it) {
    if (auto value = *it; value > threshold) {
        // 处理
    }
}

// 💡 if 初始化语句的优点
// 1. 变量作用域受限
// 2. 代码更清晰
// 3. 避免变量泄漏
```

### constexpr if

```cpp
// ✅ constexpr if
template <typename T>
auto Function(T value) {
    if constexpr (std::is_integral_v<T>) {
        return value * 2;
    } else if constexpr (std::is_floating_point_v<T>) {
        return value * 3.14;
    } else {
        return value;
    }
}

// ✅ 用于编译时条件
template <typename T>
void Process(T value) {
    if constexpr (std::is_pointer_v<T>) {
        // 处理指针
    } else {
        // 处理非指针
    }
}

// 💡 constexpr if 的优点
// 1. 编译时条件分支
// 2. 减少代码膨胀
// 3. 更清晰的代码
```

### 类模板参数推导（CTAD）

```cpp
// ✅ CTAD
std::pair p{1, 2.0};          // std::pair<int, double>
std::vector v{1, 2, 3};       // std::vector<int>
std::tuple t{1, 2.0, "hello"};  // std::tuple<int, double, const char*>

// ✅ 推导指引
template <typename T>
std::vector(T) -> std::vector<T>;

std::vector v{1, 2, 3};  // 推导为 std::vector<int>

// ✅ 用于自定义类型
template <typename T>
class Container {
public:
    Container(T value) : value_(value) {}
    
private:
    T value_;
};

Container c{42};  // 推导为 Container<int>

// 💡 CTAD 的优点
// 1. 减少冗余
// 2. 更易读
// 3. 更安全
```

### 折叠表达式

```cpp
// ✅ 折叠表达式
template <typename... Args>
auto Sum(Args... args) {
    return (args + ...);  // 一元右折叠
}

int result = Sum(1, 2, 3, 4, 5);  // 15

// ✅ 一元左折叠
template <typename... Args>
void Print(Args... args) {
    (std::cout << ... << args) << std::endl;
}

Print(1, 2, 3, 4, 5);  // 输出：1 2 3 4 5

// ✅ 二元折叠
template <typename... Args>
void Print(Args... args) {
    int dummy[] = {(std::cout << args << " ", 0)...};
    std::cout << std::endl;
}

// 💡 折叠表达式类型
// 1. 一元左折叠：(... op pack)
// 2. 一元右折叠：(pack op ...)
// 3. 二元左折叠：(init op ... op pack)
// 4. 二元右折叠：(pack op ... op init)
```

### 内联变量

```cpp
// ✅ 内联变量
inline int global_var = 42;
inline const double pi = 3.14159265358979323846;

// ✅ 用于头文件
// widget.h
inline int counter = 0;

// ✅ 用于类成员
class Widget {
public:
    inline static int count = 0;
};

// 💡 内联变量的优点
// 1. 可以在头文件中定义全局变量
// 2. 避免重复定义
// 3. 更安全的全局状态
```

### std::optional

```cpp
#include <optional>

// ✅ std::optional
std::optional<int> FindUser(int id) {
    if (id == 1) {
        return 42;
    }
    return std::nullopt;
}

// ✅ 使用
auto result = FindUser(1);
if (result) {
    std::cout << *result << std::endl;  // 42
}

// ✅ 带默认值
int value = result.value_or(0);

// ✅ 用于函数返回值
std::optional<std::string> ReadFile(const char *filename) {
    std::ifstream file(filename);
    if (!file) {
        return std::nullopt;
    }
    
    std::string content;
    std::getline(file, content);
    return content;
}

// 💡 std::optional 的优点
// 1. 表示可能缺失的值
// 2. 避免使用 nullptr 或特殊值
// 3. 更安全的错误处理
```

### std::variant

```cpp
#include <variant>

// ✅ std::variant
std::variant<int, double, std::string> v;

v = 42;           // int
v = 3.14;         // double
v = "hello";      // std::string

// ✅ 访问
std::cout << std::get<int>(v) << std::endl;  // 42

// ✅ 安全访问
if (auto *p = std::get_if<int>(&v)) {
    std::cout << *p << std::endl;
}

// ✅ 用于多态
class Circle {
public:
    void Draw() { std::cout << "Circle" << std::endl; }
};

class Rectangle {
public:
    void Draw() { std::cout << "Rectangle" << std::endl; }
};

using Shape = std::variant<Circle, Rectangle>;

Shape s = Circle{};
std::visit([](auto &shape) { shape.Draw(); }, s);

// 💡 std::variant 的优点
// 1. 类型安全的联合体
// 2. 编译时类型检查
// 3. 支持多态
```

### std::any

```cpp
#include <any>

// ✅ std::any
std::any a;

a = 42;           // int
a = 3.14;         // double
a = "hello";      // const char*
a = std::string("hello");  // std::string

// ✅ 访问
int value = std::any_cast<int>(a);

// ✅ 安全访问
if (auto *p = std::any_cast<int>(&a)) {
    std::cout << *p << std::endl;
}

// ✅ 类型检查
std::cout << a.type().name() << std::endl;

// 💡 std::any 的优点
// 1. 存储任意类型
// 2. 类型安全
// 3. 运行时类型信息
```

### std::string_view

```cpp
#include <string_view>

// ✅ std::string_view
std::string_view sv = "Hello, World!";

// ✅ 不拥有数据
const char *str = "Hello, World!";
std::string_view sv2 = str;  // ✅ 不复制

// ✅ 子字符串
std::string_view sub = sv.substr(0, 5);  // "Hello"

// ✅ 比较
if (sv == "Hello, World!") {
    std::cout << "Equal" << std::endl;
}

// ✅ 用于函数参数
void Print(std::string_view sv) {
    std::cout << sv << std::endl;
}

Print("Hello");  // ✅ 不需要创建 std::string
Print(std::string("Hello"));  // ✅ 可以接受 std::string

// 💡 std::string_view 的优点
// 1. 零拷贝字符串查看
// 2. 不拥有数据，更安全
// 3. 性能更好
```

### 文件系统库

```cpp
#include <filesystem>

namespace fs = std::filesystem;

// ✅ 目录操作
fs::create_directory("test");
fs::create_directories("a/b/c");  // 递归创建

// ✅ 文件操作
fs::copy("source.txt", "dest.txt");
fs::rename("old.txt", "new.txt");
fs::remove("file.txt");

// ✅ 遍历目录
for (const auto &entry : fs::directory_iterator(".")) {
    std::cout << entry.path() << std::endl;
}

// ✅ 递归遍历
for (const auto &entry : fs::recursive_directory_iterator(".")) {
    std::cout << entry.path() << std::endl;
}

// ✅ 文件信息
fs::path p = "file.txt";
std::cout << fs::exists(p) << std::endl;
std::cout << fs::file_size(p) << std::endl;
std::cout << fs::is_directory(p) << std::endl;

// 💡 文件系统库的优点
// 1. 跨平台文件操作
// 2. 类型安全的路径
// 3. 丰富的操作
```

---

## C++20 核心特性

### Concepts

```cpp
// ✅ 定义 concept
template <typename T>
concept Addable = requires(T a, T b) {
    { a + b } -> std::convertible_to<T>;
};

// ✅ 使用 concept
template <Addable T>
T Add(T a, T b) {
    return a + b;
}

// ✅ C++20 语法
Addable auto Add(Addable auto a, Addable auto b) {
    return a + b;
}

// ✅ 约束
template <typename T>
    requires Addable<T>
T Add(T a, T b) {
    return a + b;
}

// 💡 concepts 的优点
// 1. 更好的错误信息
// 2. 更清晰的代码
// 3. 编译时检查
```

### Ranges

```cpp
#include <ranges>
#include <vector>

// ✅ Ranges 基本用法
std::vector<int> v = {1, 2, 3, 4, 5};

auto result = v | std::views::filter([](int x) { return x > 2; })
               | std::views::transform([](int x) { return x * 2; });

for (int x : result) {
    std::cout << x << " ";  // 6 8 10
}

// ✅ 管道操作
auto evens = v | std::views::filter([](int x) { return x % 2 == 0; });
auto doubled = v | std::views::transform([](int x) { return x * 2; });

// ✅ 懒惰求值
auto lazy = v | std::views::filter([](int x) {
    std::cout << "Filtering " << x << std::endl;
    return x > 2;
});

// 只有在遍历时才会执行 filter
for (int x : lazy) {
    // 才会打印 "Filtering..."
}

// 💡 Ranges 的优点
// 1. 函数式编程风格
// 2. 懒惰求值
// 3. 更清晰的代码
```

### Coroutines

```cpp
#include <coroutine>

// ✅ 协程基本结构
generator<int> CountTo(int n) {
    for (int i = 1; i <= n; i++) {
        co_yield i;  // 产生值
    }
}

// ✅ 使用
auto gen = CountTo(5);
for (int x : gen) {
    std::cout << x << " ";  // 1 2 3 4 5
}

// ✅ 异步协程
task<int> AsyncFunction() {
    co_await some_async_operation();
    co_return 42;
}

// 💡 协程的优点
// 1. 异步编程更简单
// 2. 生成器模式
// 3. 可中断的函数
```

### Modules

```cpp
// ✅ 模块定义
// math.cppm
export module math;

export int Add(int a, int b) {
    return a + b;
}

export int Multiply(int a, int b) {
    return a * b;
}

// ✅ 使用模块
// main.cpp
import math;

int main() {
    int result = Add(1, 2);
    return 0;
}

// 💡 模块的优点
// 1. 更快的编译
// 2. 更好的封装
// 3. 避免头文件问题
```

### 三路比较（太空船运算符）

```cpp
#include <compare>

// ✅ 三路比较
class Point {
public:
    float x, y;
    
    auto operator<=>(const Point &other) const = default;
};

// ✅ 使用
Point p1{1.0f, 2.0f};
Point p2{3.0f, 4.0f};

if (p1 < p2) {
    std::cout << "p1 < p2" << std::endl;
}

// ✅ 自定义比较
class Widget {
public:
    int value;
    
    auto operator<=>(const Widget &other) const {
        return value <=> other.value;
    }
};

// 💡 三路比较的优点
// 1. 简化比较运算符定义
// 2. 自动生成所有比较运算符
// 3. 更安全的比较
```

---

## C++20 标准库

### std::format

```cpp
#include <format>

// ✅ std::format
std::string s = std::format("Hello, {}! You are {} years old.", "Alice", 25);
std::cout << s << std::endl;

// ✅ 格式化数字
std::string s1 = std::format("{:.2f}", 3.14159);  // "3.14"
std::string s2 = std::format("{:010d}", 42);      // "0000000042"

// ✅ 对齐和填充
std::string s3 = std::format("{:>10}", "hello");    // "     hello"
std::string s4 = std::format("{:<10}", "hello");    // "hello     "
std::string s5 = std::format("{:^10}", "hello");    // "  hello   "

// ✅ 自定义类型
struct Point {
    float x, y;
};

template <>
struct std::formatter<Point> : std::formatter<std::string> {
    auto format(const Point &p, auto &ctx) {
        return std::format_to(ctx.out(), "({}, {})", p.x, p.y);
    }
};

// 💡 std::format 的优点
// 1. 类型安全
// 2. 更快的性能
// 3. 更清晰的语法
```

### std::span

```cpp
#include <span>

// ✅ std::span
void Process(std::span<int> data) {
    for (int x : data) {
        std::cout << x << " ";
    }
}

// ✅ 使用
std::vector<int> v = {1, 2, 3, 4, 5};
Process(v);  // ✅ 可以接受 vector

int arr[] = {1, 2, 3, 4, 5};
Process(arr);  // ✅ 可以接受数组

// ✅ 子区间
std::span<int> sub(v.data() + 1, 3);  // {2, 3, 4}

// 💡 std::span 的优点
// 1. 非拥有视图
// 2. 类型安全的数组视图
// 3. 避免指针和大小传递
```

### std::jthread

```cpp
#include <thread>

// ✅ std::jthread
std::jthread t([](std::stop_token token) {
    while (!token.stop_requested()) {
        // 工作...
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }
});

// 自动 join
t.request_stop();

// 💡 std::jthread 的优点
// 1. 自动 join
// 2. 内置停止令牌
// 3. 更安全的线程管理
```

### std::latch 和 std::barrier

```cpp
#include <latch>
#include <barrier>

// ✅ std::latch
std::latch latch(3);  // 计数器为 3

std::thread t1([&latch]() {
    // 工作...
    latch.count_down();  // 计数减 1
});

std::thread t2([&latch]() {
    // 工作...
    latch.count_down();
});

std::thread t3([&latch]() {
    // 工作...
    latch.count_down();
});

latch.wait();  // 等待计数器为 0

// ✅ std::barrier
std::barrier barrier(3, []() {
    // 所有线程到达屏障后执行
});

std::thread t1([&barrier]() {
    // 工作...
    barrier.arrive_and_wait();  // 到达并等待
});

std::thread t2([&barrier]() {
    // 工作...
    barrier.arrive_and_wait();
});

std::thread t3([&barrier]() {
    // 工作...
    barrier.arrive_and_wait();
});

// 💡 同步原语
// 1. std::latch：一次性同步
// 2. std::barrier：可重用同步
// 3. 更灵活的线程同步
```

---

## 难点解析

### 🔴 难点 1：结构化绑定的限制

```cpp
// ✅ 结构化绑定的限制
// 1. 不能用于引用
auto [x, y] = std::pair{1, 2};  // ✅
auto &[x, y] = std::pair{1, 2};  // ❌ 编译错误

// 2. 不能用于 const
const auto [x, y] = std::pair{1, 2};  // ✅
auto [x, y] = const std::pair{1, 2};  // ❌ 编译错误

// 3. 不能修改绑定的变量
auto [x, y] = std::pair{1, 2};
x = 10;  // ❌ 编译错误

// ✅ 解决方案
auto [x, y] = std::pair{1, 2};
int x_val = x;  // ✅ 可以修改副本
x_val = 10;
```

### 🔴 难点 2：constexpr if 的陷阱

```cpp
// ✅ constexpr if 的陷阱
template <typename T>
auto Function(T value) {
    if constexpr (std::is_integral_v<T>) {
        return value * 2;
    } else if constexpr (std::is_floating_point_v<T>) {
        return value * 3.14;
    } else {
        return value;  // ❌ 可能导致编译错误
    }
}

// ✅ 解决方案
template <typename T>
auto Function(T value) {
    if constexpr (std::is_integral_v<T>) {
        return value * 2;
    } else if constexpr (std::is_floating_point_v<T>) {
        return value * 3.14;
    } else {
        static_assert(std::is_same_v<T, void>, "Unsupported type");
    }
}
```

### 🔴 难点 3：Ranges 的懒惰求值

```cpp
// ✅ 懒惰求值
std::vector<int> v = {1, 2, 3, 4, 5};

auto lazy = v | std::views::filter([](int x) {
    std::cout << "Filtering " << x << std::endl;
    return x > 2;
});

// 此时不会打印任何内容
std::cout << "Before iteration" << std::endl;

// 只有在遍历时才会执行 filter
for (int x : lazy) {
    std::cout << x << " ";
}

// 输出：
// Before iteration
// Filtering 1
// Filtering 2
// Filtering 3
// 3
// Filtering 4
// 4
// Filtering 5
// 5

// 💡 懒惰求值的优点
// 1. 避免不必要的计算
// 2. 可以处理无限序列
// 3. 更高效的管道操作
```

### 🔴 难点 4：Coroutines 的复杂性

```cpp
// ✅ 协程的基本结构
generator<int> CountTo(int n) {
    for (int i = 1; i <= n; i++) {
        co_yield i;  // 产生值
    }
}

// ✅ 协程的状态
// 1. 初始状态：未开始执行
// 2. 挂起状态：co_yield 或 co_await 暂停
// 3. 就绪状态：可以继续执行
// 4. 结束状态：执行完毕

// ✅ 协程的类型
// 1. 生成器：产生值
// 2. 任务：异步执行
// 3. 异步生成器：异步产生值

// 💡 协程的复杂性
// 1. 需要理解状态机
// 2. 需要理解生命周期
// 3. 需要理解取消机制
```

---

## Unity 对照

### 概念映射

| C++17/20 特性 | Unity 对应 | 说明 |
|---------------|------------|------|
| 结构化绑定 | C# 解构 | 类型解构 |
| std::optional | Unity 可空类型 | 可能缺失的值 |
| std::variant | Unity 多态 | 类型安全联合体 |
| std::any | Unity 动态类型 | 任意类型 |
| std::string_view | Unity 字符串切片 | 零拷贝字符串 |
| Concepts | Unity 泛型约束 | 类型约束 |
| Ranges | Unity LINQ | 函数式编程 |
| Coroutines | Unity 协程 | 异步编程 |
| Modules | Unity 程序集 | 模块化 |

### Unity 中的应用

```cpp
// 1. C++17 特性
// Unity 的 C# 8.0 支持类似特性
// 例如：可空类型、模式匹配等

// 2. C++20 特性
// Unity 的 C# 支持类似特性
// 例如：协程、LINQ 等

// 3. 性能优化
// 理解 C++17/20 特性可以帮助优化 Unity 性能
// 例如：使用 std::string_view 避免拷贝

// 💡 学习建议
// 1. 先理解 C++17/20 特性
// 2. 对比 Unity 的实现方式
// 3. 思考为什么 Unity 要这样设计
// 4. 将 C++ 知识应用到 Unity 开发中
```

### 代码对比

```cpp
// C++17 std::optional
std::optional<int> FindUser(int id) {
    if (id == 1) {
        return 42;
    }
    return std::nullopt;
}

// Unity C# 可空类型
int? FindUser(int id) {
    if (id == 1) {
        return 42;
    }
    return null;
}

// 💡 区别
// C++17：std::optional 是值类型
// Unity：int? 是值类型
// 两者都表示可能缺失的值
```

---

## 📝 学习建议

### 阅读顺序
1. 先读 C++17 核心特性
2. 再读 C++17 标准库
3. 然后读 C++20 核心特性
4. 最后读 C++20 标准库

### 实践方法
1. 每学一个特性，写一个示例
2. 对比 Unity 的实现方式
3. 思考为什么 C++ 要引入这些特性
4. 将新特性应用到项目中

### 常见错误
1. 结构化绑定的限制
2. constexpr if 的陷阱
3. Ranges 的懒惰求值
4. Coroutines 的复杂性

### 推荐练习
1. 使用结构化绑定简化代码
2. 使用 std::optional 处理可选值
3. 使用 Ranges 实现函数式编程
4. 使用 Coroutines 实现异步编程

---

*本文件持续更新中...*
