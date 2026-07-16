# Effective Modern C++ 学习笔记

> **作者**：Scott Meyers
> **版本**：针对 C++11 和 C++14
> **地位**：现代 C++ 实战指南，42条实用建议

---

## 📋 目录

1. [Deduction](#deduction)
2. [auto](#auto)
3. [移动语义](#移动语义)
4. [智能指针](#智能指针)
5. [Lambda 表达式](#lambda-表达式)
6. [并发 API](#并发-api)
7. [其他](#其他)
8. [难点解析](#难点解析)
9. [Unity 对照](#unity-对照)

---

## Deduction

### 条款 1：理解模板类型推导

```cpp
// ✅ 模板类型推导规则
template <typename T>
void Function(T param);

// 1. 传入引用
int x = 10;
Function(x);   // T = int, param = int
Function<int>(x);  // 显式指定 T = int

// 2. 传入 const
const int x = 10;
Function(x);   // T = int（const 被忽略）

// 3. 传入数组
const char name[] = "Hello";
Function(name);  // T = const char*, param = const char*

// 4. 传入函数
void Func(int);
Function(Func);  // T = void(*)(int), param = void(*)(int)

// 💡 推导规则
// 1. 引用传入时，保留引用
// 2. 非引用传入时，忽略 const/volatile
// 3. 数组退化为指针
// 4. 函数退化为函数指针
```

### 条款 2：理解 auto 的类型推导

```cpp
// ✅ auto 推导规则
auto x = 10;        // int
const auto y = 10;  // const int
auto &z = x;        // int&
const auto &w = x;  // const int&

// ✅ auto 在容器遍历中
std::vector<int> v = {1, 2, 3, 4, 5};

for (auto elem : v) {        // 值拷贝
    std::cout << elem << std::endl;
}

for (const auto &elem : v) { // const 引用（推荐）
    std::cout << elem << std::endl;
}

for (auto &&elem : v) {      // 右值引用
    std::cout << elem << std::endl;
}

// 💡 auto 的优缺点
// 优点：
// 1. 减少冗余
// 2. 避免类型不匹配
// 3. 更容易重构
// 缺点：
// 1. 可能推导出意外的类型
// 2. 代码可读性可能降低
```

### 条款 3：理解 decltype

```cpp
// ✅ decltype 基本用法
int x = 10;
decltype(x) y = 20;  // int

// ✅ decltype 保留 const
const int x = 10;
decltype(x) y = 20;  // const int

// ✅ decltype 在模板中
template <typename Container, typename Index>
auto Access(Container &c, Index i) -> decltype(c[i]) {
    return c[i];
}

// ✅ C++14：直接返回类型推导
template <typename Container, typename Index>
auto Access(Container &c, Index i) {
    return c[i];
}

// ✅ decltype(auto)（C++14）
int x = 10;
decltype(auto) y = x;  // int（保留引用）

// 💡 decltype vs auto
// auto：根据初始化表达式推导类型
// decltype：根据表达式本身推导类型
```

### 条款 4：知道如何查看推导的类型

```cpp
// ✅ 使用 IDE 查看
// Visual Studio：鼠标悬停
// CLion：Alt+Enter

// ✅ 使用编译器错误信息
template <typename T>
void Function(T param) {
    // 故意写错，查看编译器错误信息
    NonExistentFunction(param);
}

// ✅ 使用 typeid
template <typename T>
void Function(T param) {
    std::cout << typeid(T).name() << std::endl;
}

// ✅ 使用 Boost.TypeIndex
#include <boost/type_index.hpp>

template <typename T>
void Function(T param) {
    std::cout << boost::typeindex::type_id_with_cvr<T>().pretty_name() 
              << std::endl;
}
```

---

## auto

### 条款 5：优先使用 auto，而非显式类型声明

```cpp
// ❌ 不好：显式类型
std::map<std::string, std::vector<int>> m;
auto it = m.find("key");  // ✅ auto

// ✅ 好：使用 auto
auto x = 10;
auto y = 3.14;
auto z = "Hello";
auto v = std::vector<int>{1, 2, 3};

// 💡 使用场景
// 1. 类型明显时
// 2. 避免冗余时
// 3. 避免类型不匹配时

// ❌ 不要使用 auto
// 1. 类型不明显时
// 2. 需要精确控制类型时
// 3. 函数返回类型
```

### 条款 6：当 auto 推导出非预期类型时，使用显式类型声明

```cpp
// ❌ 问题：std::vector<bool>
std::vector<bool> v = {true, false, true};
auto elem = v[0];  // 不是 bool！是 std::vector<bool>::reference

// ✅ 解决方案
std::vector<bool> v = {true, false, true};
bool elem = v[0];  // ✅ 显式类型

// ❌ 问题：初始化列表
auto x = {1, 2, 3};  // std::initializer_list<int>

// ✅ 解决方案
std::vector<int> x = {1, 2, 3};  // ✅ 显式类型

// 💡 原则
// 1. 当 auto 推导出非预期类型时
// 2. 使用显式类型声明
// 3. 或者使用 static_cast
```

---

## 移动语义

### 条款 7：在重载时区分右值引用和通用引用

```cpp
// ✅ 右值引用
void Function(int &&x) {  // 只绑定右值
    std::cout << "Right value" << std::endl;
}

// ✅ 通用引用
template <typename T>
void Function(T &&x) {  // 可以绑定左值和右值
    std::cout << "Universal reference" << std::endl;
}

// ✅ 使用示例
int x = 10;
Function(x);   // 左值，调用通用引用版本
Function(10);  // 右值，调用右值引用版本

// 💡 区分规则
// 1. 右值引用：显式类型
// 2. 通用引用：模板类型推导 + &&
// 3. 通用引用可以接受左值和右值
```

### 条款 8：使用 std::forward 转发通用引用

```cpp
// ✅ 完美转发
template <typename T>
void Wrapper(T &&arg) {
    // std::forward 保持原始值类别
    Function(std::forward<T>(arg));
}

// ✅ 示例
template <typename T>
void Wrapper(T &&arg) {
    Function(std::forward<T>(arg));  // 保持左值/右值
}

// 💡 std::forward vs std::move
// std::forward：转发通用引用（保持原始值类别）
// std::move：将左值转换为右值（不保持）
```

### 条款 9：使用 std::move 转发右值引用

```cpp
// ✅ 使用 std::move
class Widget {
public:
    Widget(Widget &&other) noexcept 
        : data(other.data) {
        other.data = nullptr;
    }
};

// ✅ 使用示例
Widget CreateWidget() {
    Widget w;
    return std::move(w);  // ✅ 转发右值
}

// ❌ 不要返回局部变量的右值引用
Widget BadFunction() {
    Widget w;
    return std::move(w);  // ❌ 危险！w 已销毁
}

// 💡 原则
// 1. 使用 std::move 转发右值引用
// 2. 不要返回局部变量的右值引用
// 3. 优先返回局部变量（编译器会自动移动）
```

### 条款 10：按 std::move 移动，按 std::forward 转发

```cpp
// ✅ std::move 的使用场景
class Widget {
public:
    void SetName(std::string &&name) {
        name_ = std::move(name);  // 移动
    }
    
    void SetName(const std::string &name) {
        name_ = name;  // 拷贝
    }
};

// ✅ std::forward 的使用场景
template <typename T>
void Wrapper(T &&arg) {
    Function(std::forward<T>(arg));  // 转发
}

// 💡 原则
// 1. std::move：将左值转换为右值（不保持）
// 2. std::forward：转发通用引用（保持原始值类别）
// 3. 不要混用
```

### 条款 11：不要在构造函数中使用 std::make_shared 或 std::make_unique

```cpp
// ❌ 问题
class Widget {
public:
    Widget(std::shared_ptr<Widget> self) : self_(self) {}
    
    static std::shared_ptr<Widget> Create() {
        return std::make_shared<Widget>(shared_ptr<Widget>(this));
    }
    
private:
    std::shared_ptr<Widget> self_;
};

// ✅ 解决方案：使用 enable_shared_from_this
class Widget : public std::enable_shared_from_this<Widget> {
public:
    static std::shared_ptr<Widget> Create() {
        return std::make_shared<Widget>();
    }
    
    std::shared_ptr<Widget> GetSelf() {
        return shared_from_this();
    }
};

// 💡 原则
// 1. 不要在构造函数中创建 shared_ptr
// 2. 使用 enable_shared_from_this
// 3. 确保对象被 shared_ptr 管理
```

### 条款 12：使用 std::make_unique 而非直接 new

```cpp
// ❌ 不好
std::unique_ptr<int> p1(new int(10));

// ✅ 好
auto p2 = std::make_unique<int>(10);

// 💡 优点
// 1. 异常安全：new 可能抛出异常
// 2. 效率：一次内存分配
// 3. 可读性：更清晰

// ❌ make_unique 的限制
// 1. 不支持自定义删除器
// 2. 不支持数组
// 3. 不支持花括号初始化

// ✅ 解决方案
std::unique_ptr<int[]> arr(new int[100]);  // ✅ 数组
std::unique_ptr<int, decltype(&Delete)> p(new int(10), Delete);  // ✅ 自定义删除器
```

### 条款 13：使用 std::make_shared 时的陷阱

```cpp
// ✅ std::make_shared 的优点
// 1. 一次内存分配
// 2. 异常安全
// 3. 更高效

// ✅ std::make_shared 的陷阱
// 1. 不支持自定义删除器
// 2. 不支持数组
// 3. 内存释放延迟

// 💡 延迟释放问题
auto sp = std::make_shared<int>(10);
std::weak_ptr<int> wp = sp;
sp.reset();  // 内存不会释放！
// 只有当 wp.lock() 失败时才会释放

// ✅ 解决方案
auto sp = std::shared_ptr<int>(new int(10));
std::weak_ptr<int> wp = sp;
sp.reset();  // 内存立即释放
```

### 条款 14：使用 nullptr 而非 0 或 NULL

```cpp
// ❌ 不好
int *p1 = 0;
int *p2 = NULL;

// ✅ 好
int *p3 = nullptr;

// 💡 优点
// 1. 类型安全：nullptr 是指针类型
// 2. 可读性：明确表示空指针
// 3. 避免歧义：不会被解释为整数

// ❌ NULL 的问题
void Function(int x);
void Function(int *p);

Function(NULL);  // 可能调用 Function(int)
Function(nullptr);  // 调用 Function(int*)
```

### 条款 15：使用 constexpr 声明编译期常量

```cpp
// ❌ 旧式
#define MAX_SIZE 100

// ✅ C++11
constexpr int MAX_SIZE = 100;

// ✅ constexpr 函数
constexpr int Square(int x) {
    return x * x;
}

constexpr int result = Square(10);  // 编译期计算

// ✅ constexpr 变量
constexpr double PI = 3.14159265358979323846;

// 💡 constexpr vs const
// const：运行时常量
// constexpr：编译期常量
// constexpr 更强大，但有更多限制
```

### 条款 16：使用 constexpr 函数

```cpp
// ✅ constexpr 函数
constexpr int Factorial(int n) {
    return (n <= 1) ? 1 : n * Factorial(n - 1);
}

constexpr int result = Factorial(5);  // 120

// ✅ constexpr 函数的优点
// 1. 编译期计算
// 2. 类型安全
// 3. 可调试

// ❌ constexpr 函数的限制
// 1. C++11：只能有一个 return 语句
// 2. C++14：支持循环、局部变量
// 3. 不能有副作用
```

### 条款 17：理解 constexpr 和 const 的区别

```cpp
// ✅ const：运行时常量
const int x = 10;  // 运行时常量

// ✅ constexpr：编译期常量
constexpr int y = 20;  // 编译期常量

// ✅ constexpr 变量可以用于数组大小
constexpr int size = 10;
int arr[size];  // ✅ 可以

// ❌ const 变量不能用于数组大小
const int size = 10;
int arr[size];  // ❌ 可能不行

// 💡 原则
// 1. 需要编译期常量时，使用 constexpr
// 2. 需要运行时常量时，使用 const
// 3. constexpr 更强大，但有更多限制
```

---

## 智能指针

### 条款 18：使用 std::unique_ptr 管理独占所有权

```cpp
// ✅ std::unique_ptr 的特点
// 1. 独占所有权
// 2. 不可复制，但可移动
// 3. 轻量级，无额外开销

// ✅ 使用示例
std::unique_ptr<int> p1 = std::make_unique<int>(10);
std::unique_ptr<int> p2 = std::move(p1);  // ✅ 可以移动

// ❌ 不可复制
std::unique_ptr<int> p3 = p1;  // ❌ 编译错误

// ✅ 自定义删除器
auto Deleter = [](int *p) {
    std::cout << "Deleting " << *p << std::endl;
    delete p;
};

std::unique_ptr<int, decltype(&Deleter)> p(new int(10), Deleter);

// 💡 使用场景
// 1. 工厂函数返回值
// 2. 类成员变量
// 3. 局部变量
```

### 条款 19：使用 std::shared_ptr 管理共享所有权

```cpp
// ✅ std::shared_ptr 的特点
// 1. 共享所有权
// 2. 可复制，可移动
// 3. 引用计数管理生命周期

// ✅ 使用示例
auto sp1 = std::make_shared<int>(10);
auto sp2 = sp1;  // ✅ 可以复制

std::cout << sp1.use_count() << std::endl;  // 2

sp1.reset();
std::cout << sp2.use_count() << std::endl;  // 1

// ✅ 引用计数
std::cout << sp1.use_count() << std::endl;  // 引用计数

// ✅ 控制块
// shared_ptr 内部有一个控制块
// 包含：引用计数、弱引用计数、自定义删除器等

// 💡 使用场景
// 1. 需要共享所有权
// 2. 需要弱引用
// 3. 需要自定义删除器
```

### 条款 20：使用 std::weak_ptr 替代智能指针的旁观者

```cpp
// ✅ std::weak_ptr 的特点
// 1. 不增加引用计数
// 2. 可以观察 shared_ptr 管理的对象
// 3. 需要 lock() 才能访问对象

// ✅ 使用示例
std::weak_ptr<int> wp;
{
    auto sp = std::make_shared<int>(10);
    wp = sp;  // ✅ 可以赋值
    
    std::cout << wp.expired() << std::endl;  // false
    
    auto sp2 = wp.lock();  // ✅ 获取 shared_ptr
    if (sp2) {
        std::cout << *sp2 << std::endl;  // 10
    }
}
// sp 已销毁

std::cout << wp.expired() << std::endl;  // true
auto sp3 = wp.lock();  // ❌ 返回空 shared_ptr

// 💡 使用场景
// 1. 打破循环引用
// 2. 缓存
// 3. 观察者模式
```

### 条款 21：优先使用 std::make_unique 或 std::make_shared

```cpp
// ❌ 不好
std::unique_ptr<int> p1(new int(10));
std::shared_ptr<int> p2(new int(10));

// ✅ 好
auto p3 = std::make_unique<int>(10);
auto p4 = std::make_shared<int>(10);

// 💡 优点
// 1. 异常安全：new 可能抛出异常
// 2. 效率：一次内存分配
// 3. 可读性：更清晰

// ❌ make_shared 的陷阱
// 1. 不支持自定义删除器
// 2. 内存释放延迟
// 3. 不支持数组
```

### 条款 22：使用 std::enable_shared_from_this 管理对象

```cpp
// ❌ 问题
class Widget {
public:
    std::shared_ptr<Widget> GetSelf() {
        return std::shared_ptr<Widget>(this);  // ❌ 危险！
    }
};

// ✅ 解决方案
class Widget : public std::enable_shared_from_this<Widget> {
public:
    std::shared_ptr<Widget> GetSelf() {
        return shared_from_this();  // ✅ 安全
    }
};

// ✅ 使用
auto w = std::make_shared<Widget>();
auto self = w->GetSelf();  // ✅ 安全

// 💡 原则
// 1. 如果需要返回自身的 shared_ptr
// 2. 继承 enable_shared_from_this
// 3. 使用 shared_from_this()
```

---

## Lambda 表达式

### 条款 23：理解 Lambda 表达式的捕获列表

```cpp
// ✅ Lambda 捕获方式
int x = 10;
int y = 20;

// 值捕获
auto l1 = [x, y]() { return x + y; };

// 引用捕获
auto l2 = [&x, &y]() { x++; y++; };

// 隐式值捕获
auto l3 = [=]() { return x + y; };

// 隐式引用捕获
auto l4 = [&]() { x++; y++; };

// 混合捕获
auto l5 = [x, &y]() { return x + y++; };

// C++14：初始化捕获
auto l6 = [z = x + y]() { return z; };

// C++14：泛型 Lambda
auto l7 = [](auto x, auto y) { return x + y; };

// 💡 捕获规则
// 1. 值捕获：只读，不能修改
// 2. 引用捕获：可读写，但可能悬空
// 3. mutable：允许修改值捕获的变量
```

### 条款 24：理解 Lambda 的类型

```cpp
// ✅ Lambda 类型
auto l = [](int x) { return x * x; };
// 类型是：int (*)(int)（函数指针）

// ✅ 带捕获的 Lambda
int factor = 2;
auto l2 = [factor](int x) { return x * factor; };
// 类型是：匿名类

// ✅ 使用 std::function
std::function<int(int)> func = [](int x) { return x * x; };
int result = func(5);  // 25

// ✅ Lambda 作为模板参数
template <typename Func>
void Call(Func f) {
    f(5);
}

Call([](int x) { return x * x; });

// 💡 原则
// 1. Lambda 类型是匿名的
// 2. 使用 auto 存储 Lambda
// 3. 使用 std::function 存储可调用对象
```

### 条款 25：使用 std::function 存储可调用对象

```cpp
// ✅ std::function 的特点
// 1. 可以存储任何可调用对象
// 2. 函数、Lambda、函数指针、仿函数等
// 3. 类型擦除

// ✅ 使用示例
std::function<int(int, int)> add = [](int a, int b) { return a + b; };
std::function<int(int, int)> mul = [](int a, int b) { return a * b; };

std::cout << add(1, 2) << std::endl;  // 3
std::cout << mul(1, 2) << std::endl;  // 2

// ✅ 存储不同类型的可调用对象
std::function<void()> func;

func = []() { std::cout << "Lambda" << std::endl; };
func();

func = std::bind([](int x) { std::cout << x << std::endl; }, 10);
func();

// 💡 std::function vs auto
// auto：类型安全，编译时确定
// std::function：类型擦除，运行时确定
```

### 条款 26：使用 std::bind 替代 Lambda

```cpp
// ✅ std::bind 的使用
auto add = [](int a, int b) { return a + b; };
auto add10 = std::bind(add, 10, std::placeholders::_1);

std::cout << add10(5) << std::endl;  // 15

// ✅ Lambda 替代
auto add10 = [](int b) { return 10 + b; };

// ✅ Lambda 更清晰
auto add10 = [](int b) { return 10 + b; };
auto mul = [](int a, int b) { return a * b; };

// 💡 std::bind vs Lambda
// 1. Lambda 更清晰，更易读
// 2. std::bind 更灵活，可重用
// 3. 优先使用 Lambda
```

### 条款 27：理解 Lambda 捕获的陷阱

```cpp
// ❌ 陷阱 1：悬空引用
auto BadLambda() {
    int x = 10;
    return [&x]() { return x; };  // ❌ 危险！x 已销毁
}

// ❌ 陷阱 2：隐式拷贝
int count = 0;
auto Lambda = [count]() mutable {
    count++;  // 修改的是拷贝
};
Lambda();
std::cout << count << std::endl;  // 0，不是 1

// ✅ 解决方案：使用引用捕获
int count = 0;
auto Lambda = [&count]() {
    count++;  // 修改原变量
};
Lambda();
std::cout << count << std::endl;  // 1

// ❌ 陷阱 3：this 捕获
class Widget {
public:
    void DoSomething() {
        auto Lambda = [this]() {
            // this 被捕获
        };
    }
};

// ✅ C++17：显式 this 捕获
auto Lambda = [*this]() {  // 拷贝 this
    // ...
};
```

---

## 并发 API

### 条款 28：理解 std::thread 的资源管理

```cpp
// ✅ std::thread 的基本用法
std::thread t([]() {
    std::cout << "Hello from thread!" << std::endl;
});

t.join();  // 等待线程结束

// ✅ std::thread 的资源管理
std::thread t([]() {
    // 工作...
});

// ❌ 忘记 join 或 detach 会导致程序终止
// t.join();

// ✅ 使用 std::jthread（C++20）
std::jthread t([](std::stop_token token) {
    while (!token.stop_requested()) {
        // 工作...
    }
});
// 自动 join

// 💡 原则
// 1. 始终 join 或 detach 线程
// 2. 优先使用 std::jthread
// 3. 使用 RAII 管理线程资源
```

### 条款 29：理解 std::thread 的移动语义

```cpp
// ✅ std::thread 是可移动的
std::thread t1([]() {
    std::cout << "Thread 1" << std::endl;
});

std::thread t2 = std::move(t1);  // ✅ 移动

t2.join();  // 等待线程结束

// ❌ std::thread 不可复制
std::thread t3 = t2;  // ❌ 编译错误

// ✅ 使用 std::thread 的场景
// 1. 并行算法
// 2. 异步任务
// 3. 后台任务

// 💡 原则
// 1. std::thread 是移动语义
// 2. 使用 std::move 转移所有权
// 3. 始终 join 或 detach
```

### 条款 30：使用 std::atomic 进行并发编程

```cpp
// ✅ std::atomic 的特点
// 1. 原子操作
// 2. 无锁编程
// 3. 内存序控制

// ✅ 使用示例
std::atomic<int> counter{0};

std::thread t1([&counter]() {
    for (int i = 0; i < 1000; i++) {
        counter++;  // ✅ 原子操作
    }
});

std::thread t2([&counter]() {
    for (int i = 0; i < 1000; i++) {
        counter++;  // ✅ 原子操作
    }
});

t1.join();
t2.join();

std::cout << counter << std::endl;  // 2000

// ✅ 内存序
std::atomic<int> flag{0};
std::atomic<int> data{0};

// 发布-获取序
data.store(42, std::memory_order_release);
int value = data.load(std::memory_order_acquire);

// 💡 原子操作 vs 互斥锁
// 原子操作：无锁，性能高
// 互斥锁：有锁，简单但慢
// 优先使用原子操作
```

### 条款 31：使用 std::mutex 进行并发编程

```cpp
// ✅ std::mutex 的基本用法
std::mutex mtx;
int shared_data = 0;

std::thread t1([&mtx, &shared_data]() {
    std::lock_guard<std::mutex> lock(mtx);  // RAII 锁
    shared_data++;
});

std::thread t2([&mtx, &shared_data]() {
    std::lock_guard<std::mutex> lock(mtx);  // RAII 锁
    shared_data++;
});

t1.join();
t2.join();

// ✅ std::unique_lock
std::unique_lock<std::mutex> lock(mtx);
if (condition) {
    lock.unlock();
    // ...
}

// ✅ std::recursive_mutex
std::recursive_mutex rmtx;
std::lock_guard<std::recursive_mutex> lock1(rmtx);
std::lock_guard<std::recursive_mutex> lock2(rmtx);  // ✅ 可以递归加锁

// 💡 互斥锁类型
// 1. std::mutex：基本互斥锁
// 2. std::recursive_mutex：递归互斥锁
// 3. std::timed_mutex：带超时的互斥锁
// 4. std::recursive_timed_mutex：递归带超时的互斥锁
```

### 条款 32：使用 std::future 和 std::promise 进行并发编程

```cpp
// ✅ std::promise 和 std::future
std::promise<int> promise;
std::future<int> future = promise.get_future();

std::thread t([&promise]() {
    // 计算结果
    int result = 42;
    promise.set_value(result);  // 设置结果
});

int value = future.get();  // 获取结果（阻塞）
std::cout << value << std::endl;  // 42

t.join();

// ✅ std::async
std::future<int> f = std::async(std::launch::async, []() {
    return 42;
});

int result = f.get();  // 获取结果（阻塞）

// ✅ std::packaged_task
std::packaged_task<int(int, int)> task([](int a, int b) {
    return a + b;
});

std::future<int> result = task.get_future();
task(1, 2);  // 执行任务

std::cout << result.get() << std::endl;  // 3

// 💡 使用场景
// 1. 异步任务
// 2. 并行计算
// 3. 跨线程通信
```

### 条款 33：使用 std::condition_variable 进行并发编程

```cpp
// ✅ std::condition_variable 的基本用法
std::mutex mtx;
std::condition_variable cv;
bool ready = false;

std::thread producer([&mtx, &cv, &ready]() {
    std::unique_lock<std::mutex> lock(mtx);
    ready = true;
    cv.notify_one();  // 通知一个等待线程
});

std::thread consumer([&mtx, &cv, &ready]() {
    std::unique_lock<std::mutex> lock(mtx);
    cv.wait(lock, [&ready]() { return ready; });  // 等待条件
    std::cout << "Consumer ready" << std::endl;
});

producer.join();
consumer.join();

// ✅ std::condition_variable_any
std::condition_variable_any cv;
std::mutex mtx;

cv.wait(mtx, [&]() { return ready; });

// 💡 条件变量的使用模式
// 1. 等待条件：cv.wait(lock, predicate)
// 2. 通知：cv.notify_one() 或 cv.notify_all()
// 3. 总是与互斥锁一起使用
```

### 条款 34：使用 std::async 进行并发编程

```cpp
// ✅ std::async 的基本用法
auto f1 = std::async(std::launch::async, []() {
    return 42;
});

auto f2 = std::async(std::launch::async, []() {
    return "Hello";
});

// 获取结果
int result1 = f1.get();
std::string result2 = f2.get();

// ✅ std::launch 策略
// std::launch::async：异步执行
// std::launch::deferred：延迟执行
// std::launch::async | std::launch::deferred：自动选择

// ✅ 错误处理
auto f = std::async(std::launch::async, []() {
    throw std::runtime_error("Error");
});

try {
    f.get();  // 重新抛出异常
} catch (const std::exception &e) {
    std::cerr << e.what() << std::endl;
}

// 💡 std::async vs std::thread
// 1. std::async：自动管理线程
// 2. std::thread：手动管理线程
// 3. 优先使用 std::async
```

### 条款 35：理解 std::future 的阻塞和非阻塞行为

```cpp
// ✅ std::future 的阻塞方法
std::future<int> f = std::async(std::launch::async, []() {
    return 42;
});

int value = f.get();  // 阻塞，等待结果

// ✅ std::future 的非阻塞方法
std::future_status status = f.wait_for(std::chrono::seconds(1));

if (status == std::future_status::ready) {
    int value = f.get();  // 已准备好
} else if (status == std::future_status::timeout) {
    // 超时
} else if (status == std::future_status::deferred) {
    // 延迟执行
}

// 💡 使用场景
// 1. 需要结果：使用 get()（阻塞）
// 2. 检查状态：使用 wait_for()（非阻塞）
// 3. 超时处理：使用 wait_for() + 超时
```

### 条款 36：理解 std::atomic 的内存序

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

---

## 其他

### 条款 37：使用 std::move 传递参数

```cpp
// ✅ 完美转发
template <typename T>
void Function(T &&arg) {
    // 使用 std::forward 转发
}

// ✅ 使用 std::move
class Widget {
public:
    void SetName(std::string &&name) {
        name_ = std::move(name);  // 移动
    }
    
    void SetName(const std::string &name) {
        name_ = name;  // 拷贝
    }
};

// 💡 原则
// 1. 使用 std::forward 转发通用引用
// 2. 使用 std::move 移动右值引用
// 3. 不要混用
```

### 条款 38：避免使用默认捕获

```cpp
// ❌ 不好：默认捕获
int x = 10;
int y = 20;

auto l1 = [=]() { return x + y; };  // 值捕获所有
auto l2 = [&]() { x++; y++; };     // 引用捕获所有

// ✅ 好：显式捕获
auto l3 = [x, y]() { return x + y; };     // 显式值捕获
auto l4 = [&x, &y]() { x++; y++; };       // 显式引用捕获

// 💡 原则
// 1. 避免使用 = 或 & 默认捕获
// 2. 显式列出所有捕获的变量
// 3. 提高代码可读性
```

### 条款 39：使用 Lambda 表达式替代 std::bind

```cpp
// ❌ 不好
auto add = [](int a, int b) { return a + b; };
auto add10 = std::bind(add, 10, std::placeholders::_1);

// ✅ 好
auto add10 = [](int b) { return 10 + b; };

// ✅ Lambda 更清晰
auto print = [](int x) { std::cout << x << std::endl; };
auto print10 = [](int x) { std::cout << x << std::endl; };

// 💡 原则
// 1. Lambda 更清晰，更易读
// 2. 优先使用 Lambda
// 3. std::bind 保留用于特殊场景
```

### 条款 40：使用 std::atomic 进行位操作

```cpp
// ✅ 原子位操作
std::atomic<int> flags{0};

flags.fetch_or(1 << 3, std::memory_order_relaxed);  // 设置位
flags.fetch_and(~(1 << 3), std::memory_order_relaxed);  // 清除位
flags.fetch_xor(1 << 3, std::memory_order_relaxed);  // 翻转位

// ✅ 使用示例
std::atomic<int> status{0};

// 设置位
status |= (1 << 0);

// 清除位
status &= ~(1 << 0);

// 检查位
bool is_set = (status & (1 << 0)) != 0;

// 💡 使用场景
// 1. 标志位操作
// 2. 位掩码
// 3. 无锁位操作
```

### 条款 41：使用 std::atomic 进行比较交换操作

```cpp
// ✅ CAS 操作
std::atomic<int> counter{0};

int expected = 0;
int desired = 1;

if (counter.compare_exchange_strong(expected, desired)) {
    // 成功：counter 从 0 变为 1
} else {
    // 失败：expected 被更新为 counter 的当前值
}

// ✅ CAS 的使用模式
// 1. 无锁数据结构
// 2. 原子更新
// 3. 比较交换循环

// ✅ compare_exchange_weak vs compare_exchange_strong
// weak：可能虚假失败，性能更好
// strong：保证不会虚假失败

// 💡 使用场景
// 1. 无锁队列
// 2. 无锁栈
// 3. 原子更新
```

### 条款 42：理解 std::atomic 的局限性

```cpp
// ✅ std::atomic 的局限性
// 1. 不支持浮点类型
// 2. 不支持自定义类型
// 3. 不支持复合操作

// ✅ 解决方案：使用互斥锁
class Float {
public:
    void Add(float value) {
        std::lock_guard<std::mutex> lock(mtx);
        data += value;
    }
    
private:
    std::mutex mtx;
    float data{0};
};

// 💡 原则
// 1. 原子操作适合简单类型
// 2. 复杂类型使用互斥锁
// 3. 需要复合操作时使用互斥锁
```

---

## 难点解析

### 🔴 难点 1：右值引用和通用引用的区别

```cpp
// ✅ 右值引用
void Function(int &&x);  // 只绑定右值
Function(10);  // ✅ 可以
int x = 10;
Function(x);  // ❌ 不可以

// ✅ 通用引用
template <typename T>
void Function(T &&x);  // 可以绑定左值和右值
Function(10);  // ✅ 右值
Function(x);   // ✅ 左值

// 💡 区分规则
// 1. 右值引用：显式类型，只绑定右值
// 2. 通用引用：模板类型推导 + &&，可以绑定左值和右值
// 3. 通用引用需要模板参数推导
```

### 🔴 难点 2：移动语义 vs 拷贝语义

```cpp
// ✅ 拷贝语义
std::vector<int> v1 = {1, 2, 3, 4, 5};
std::vector<int> v2 = v1;  // 拷贝

// ✅ 移动语义
std::vector<int> v3 = {1, 2, 3, 4, 5};
std::vector<int> v4 = std::move(v3);  // 移动

// 💡 何时使用移动？
// 1. 临时对象（右值）
// 2. 显式 std::move
// 3. 返回局部对象

// ❌ 陷阱：移动后的对象
std::vector<int> v5 = {1, 2, 3, 4, 5};
std::vector<int> v6 = std::move(v5);

// v5 现在处于有效但未指定的状态
// 不要依赖 v5 的值
```

### 🔴 难点 3：智能指针的循环引用

```cpp
// ❌ 循环引用
class Node {
public:
    std::shared_ptr<Node> next;
};

auto node1 = std::make_shared<Node>();
auto node2 = std::make_shared<Node>();
node1->next = node2;
node2->next = node1;  // 循环引用！

// ✅ 解决方案：使用 weak_ptr
class Node {
public:
    std::weak_ptr<Node> next;  // 弱引用
};

auto node1 = std::make_shared<Node>();
auto node2 = std::make_shared<Node>();
node1->next = node2;
node2->next = node1;  // ✅ 不会循环引用

// 💡 原则
// 1. weak_ptr 不增加引用计数
// 2. 用于打破循环引用
// 3. 使用 lock() 访问对象
```

### 🔴 难点 4：Lambda 捕获陷阱

```cpp
// ❌ 陷阱 1：悬空引用
auto BadLambda() {
    int x = 10;
    return [&x]() { return x; };  // ❌ 危险！x 已销毁
}

// ❌ 陷阱 2：隐式拷贝
int count = 0;
auto Lambda = [count]() mutable {
    count++;  // 修改的是拷贝
};
Lambda();
std::cout << count << std::endl;  // 0，不是 1

// ✅ 解决方案：使用引用捕获
int count = 0;
auto Lambda = [&count]() {
    count++;  // 修改原变量
};
Lambda();
std::cout << count << std::endl;  // 1

// 💡 原则
// 1. 避免悬空引用
// 2. 理解值捕获和引用捕获的区别
// 3. 使用 mutable 时要小心
```

---

## Unity 对照

### 概念映射

| Modern C++ 概念 | Unity 对应 | 说明 |
|----------------|------------|------|
| auto | var | 类型推导 |
| Lambda | 匿名方法/Lambda | C# Lambda |
| std::unique_ptr | Unity 所有权系统 | 独占所有权 |
| std::shared_ptr | GC 引用 | 共享所有权 |
| std::weak_ptr | Unity 弱引用 | 打破循环引用 |
| constexpr | const | 编译期常量 |
| 移动语义 | 无直接对应 | C++ 特有 |
| 原子操作 | Unity Jobs System | 并发编程 |

### Unity 中的应用

```cpp
// 1. 智能指针
// Unity 使用垃圾回收，不需要智能指针
// 但理解智能指针可以帮助理解内存管理

// 2. 移动语义
// Unity 没有移动语义
// 但理解移动语义可以帮助理解性能优化

// 3. Lambda 表达式
// Unity 的 C# 支持 Lambda
// C++ Lambda 和 C# Lambda 语法相似

// 4. 并发编程
// Unity 的 Jobs System 是并发编程
// 理解 C++ 并发 API 可以帮助理解 Unity Jobs
```

### 代码对比

```cpp
// Modern C++：智能指针
auto p = std::make_unique<int>(10);
auto sp = std::make_shared<int>(20);

// Unity C#：GC 引用
object obj = new object();
GameObject go = new GameObject();

// 💡 区别
// C++：手动管理内存，智能指针自动释放
// Unity：垃圾回收，自动管理内存
```

---

## 📝 学习建议

### 阅读顺序
1. 先读 Deduction 和 auto（条款 1-6）
2. 再读移动语义（条款 7-17）
3. 然后读智能指针（条款 18-22）
4. 最后读 Lambda 和并发（条款 23-42）

### 实践方法
1. 每学一个条款，写一个示例
2. 对比 Unity 的实现方式
3. 思考为什么现代 C++ 要这样设计
4. 将现代 C++ 特性应用到项目中

### 常见错误
1. 混淆右值引用和通用引用
2. 不理解移动语义
3. Lambda 捕获错误
4. 智能指针循环引用

### 推荐练习
1. 实现一个 RAII 资源管理类
2. 实现一个智能指针
3. 实现一个简单的容器类
4. 实现一个并发安全的类

---

*本文件持续更新中...*
