# Exceptional C++ 系列学习笔记

> **作者**：Herb Sutter
> **地位**：问题驱动的深入学习

---

## 📋 目录

1. [Exceptional C++](#exceptional-c)
2. [More Exceptional C++](#more-exceptional-c)
3. [Exceptional C++ Style](#exceptional-c-style)
4. [难点解析](#难点解析)
5. [Unity 对照](#unity-对照)

---

## Exceptional C++

### 问题 1：何时编写自定义异常类？

```cpp
// ✅ 自定义异常类
class WidgetException : public std::exception {
public:
    WidgetException(const std::string &msg) : message(msg) {}
    
    const char* what() const noexcept override {
        return message.c_str();
    }
    
private:
    std::string message;
};

// ✅ 使用
void Function() {
    throw WidgetException("Widget failed");
}

// ✅ 捕获
try {
    Function();
} catch (const WidgetException &e) {
    std::cerr << e.what() << std::endl;
}

// 💡 何时编写自定义异常？
// 1. 需要携带额外信息
// 2. 需要特定的错误处理
// 3. 需要区分不同类型的错误
```

### 问题 2：为什么要使用构造函数初始化列表？

```cpp
// ❌ 赋值
class Widget {
public:
    Widget(int x) {
        this->x = x;  // 先默认构造，再赋值
    }
    
private:
    int x;
};

// ✅ 初始化列表
class Widget {
public:
    Widget(int x) : x(x) {}  // 直接构造
    
private:
    int x;
};

// 💡 原因
// 1. 效率：避免默认构造再赋值
// 2. 必须：const 成员、引用成员必须初始化
// 3. 顺序：按声明顺序初始化
```

### 问题 3：copy 构造函数和赋值操作符的区别

```cpp
// ✅ Copy 构造函数
Widget w1;
Widget w2 = w1;  // 调用拷贝构造函数

// ✅ 赋值操作符
Widget w1;
Widget w2;
w2 = w1;  // 调用赋值操作符

// 💡 区别
// 1. 拷贝构造函数：初始化新对象
// 2. 赋值操作符：修改已存在对象
// 3. 拷贝构造函数可以初始化 const 成员
```

### 问题 4：自引用（Self-Reference）

```cpp
// ✅ 赋值操作符的自引用
Widget& Widget::operator=(const Widget &other) {
    if (this != &other) {  // 证同测试
        // 赋值
    }
    return *this;
}

// 💡 为什么需要证同测试？
// 1. 避免不必要的操作
// 2. 避免自我赋值导致的问题
// 3. 提高效率
```

### 问题 5：数组（Array）与指向数据（Data）的指针

```cpp
// ✅ 数组
int arr[10];  // 固定大小数组

// ✅ 指针
int *p = new int[10];  // 动态数组

// ✅ std::vector
std::vector<int> v(10);  // 动态数组

// 💡 区别
// 1. 数组：固定大小，栈上分配
// 2. 指针：动态大小，堆上分配
// 3. std::vector：动态大小，自动管理内存
```

### 问题 6：函数指针（Function Pointer）

```cpp
// ✅ 函数指针
void Function() {
    std::cout << "Function" << std::endl;
}

void (*funcPtr)() = &Function;
funcPtr();  // 调用函数

// ✅ std::function
std::function<void()> f = Function;
f();

// ✅ Lambda
auto f2 = []() { std::cout << "Function" << std::endl; };
f2();

// 💡 函数指针 vs std::function vs Lambda
// 1. 函数指针：最底层，最高效
// 2. std::function：类型擦除，更灵活
// 3. Lambda：匿名函数，最简洁
```

### 问题 7：内存管理（Memory Management）

```cpp
// ❌ 手动管理内存
int *p = new int[100];
// ... 可能抛出异常
delete[] p;  // 可能不会执行

// ✅ 使用智能指针
std::unique_ptr<int[]> p(new int[100]);
// ... 即使抛出异常，也会自动删除

// ✅ 使用容器
std::vector<int> v(100);
// ... 自动管理内存

// 💡 原则
// 1. 使用 RAII 管理资源
// 2. 使用智能指针
// 3. 使用容器
```

### 问题 8：名称隐藏（Name Hiding）

```cpp
// ✅ 名称隐藏
class Base {
public:
    void Function() {
        std::cout << "Base" << std::endl;
    }
};

class Derived : public Base {
public:
    void Function() {  // 遮掩 Base::Function
        std::cout << "Derived" << std::endl;
    }
};

Derived d;
d.Function();  // 输出 "Derived"

Base *p = &d;
p->Function();  // 输出 "Base"（非虚函数）

// ✅ 使用 using 声明
class Derived : public Base {
public:
    using Base::Function;  // 引入 Base 的 Function
    
    void Function(int x) {  // 新版本
        std::cout << "Derived" << std::endl;
    }
};

// 💡 原则
// 1. 非虚函数是静态绑定的
// 2. 不要重新定义继承而来的非虚函数
// 3. 使用 using 声明引入基类函数
```

---

## More Exceptional C++

### 问题 1：泛型编程（Generic Programming）

```cpp
// ✅ 模板
template <typename T>
T Max(T a, T b) {
    return (a > b) ? a : b;
}

// ✅ 使用
int x = Max(10, 20);
double y = Max(3.14, 2.71);

// ✅ 特化
template <>
const char* Max<const char*>(const char* a, const char* b) {
    return (strcmp(a, b) > 0) ? a : b;
}

// 💡 泛型编程
// 1. 编写与类型无关的代码
// 2. 提高代码复用
// 3. 编译时多态
```

### 问题 2：临时对象（Temporary Object）

```cpp
// ✅ 临时对象
Widget Function() {
    return Widget();  // 创建临时对象
}

// ✅ RVO（返回值优化）
Widget Function() {
    return Widget();  // 编译器优化掉临时对象
}

// ✅ NRVO（命名返回值优化）
Widget Function() {
    Widget w;
    return w;  // 编译器优化掉临时对象
}

// 💡 临时对象的问题
// 1. 性能开销
// 2. 生命周期管理
// 3. 引用悬空
```

### 问题 3：异常安全（Exception Safety）

```cpp
// ✅ 异常安全保证
// 1. 基本保证：操作失败时，对象处于有效状态
// 2. 强保证：操作失败时，对象保持原状态
// 3. 不抛保证：操作不会抛出异常

// ✅ 实现强保证
class Widget {
public:
    Widget& operator=(const Widget &other) {
        Widget temp(other);  // 创建临时对象
        Swap(temp);          // 交换内容
        return *this;
    }
    
    void Swap(Widget &other) noexcept {
        std::swap(data, other.data);
    }
};

// 💡 原则
// 1. 优先实现强保证
// 2. 使用 copy-and-swap 技术
// 3. 使用 RAII 管理资源
```

### 问题 4：const 正确性（Const Correctness）

```cpp
// ✅ const 成员函数
class Widget {
public:
    int GetHealth() const {  // 承诺不修改对象
        return health;
    }
    
    void TakeDamage(int amount) {
        health -= amount;
    }
    
private:
    int health;
};

// ✅ const 引用
void Print(const Widget &w) {
    // 只读访问
}

// ✅ const 指针
const Widget *p = &w;  // 指向常量的指针

// 💡 原则
// 1. 尽可能使用 const
// 2. const 成员函数可以操作 const 对象
// 3. const 是契约，编译器会检查
```

### 问题 5：类型安全（Type Safety）

```cpp
// ❌ 不安全的类型转换
int x = 3.14;  // 隐式转换，可能丢失精度

// ✅ 安全的类型转换
int x = static_cast<int>(3.14);  // 显式转换

// ✅ 使用类型安全的接口
enum class Month { Jan=1, Feb, Mar, ... };

Date d(Month::Aug, 30, 2023);  // ✅ 类型安全

// 💡 原则
// 1. 避免隐式类型转换
// 2. 使用强类型
// 3. 使用类型安全的接口
```

### 问题 6：资源管理（Resource Management）

```cpp
// ✅ RAII
class FileHandle {
public:
    FileHandle(const char *filename) {
        file = fopen(filename, "r");
    }
    
    ~FileHandle() {
        if (file) fclose(file);
    }
    
private:
    FILE *file;
};

// ✅ 使用智能指针
std::unique_ptr<int> p = std::make_unique<int>(10);

// ✅ 使用容器
std::vector<int> v(100);

// 💡 原则
// 1. 使用 RAII 管理资源
// 2. 使用智能指针
// 3. 使用容器
```

---

## Exceptional C++ Style

### 问题 1：命名风格（Naming Convention）

```cpp
// ✅ 命名风格
// 1. 类名：PascalCase
// 2. 函数名：PascalCase
// 3. 变量名：camelCase
// 4. 常量：UPPER_SNAKE_CASE

class Player {
public:
    int GetHealth() const {
        return health_;
    }
    
    void SetHealth(int health) {
        health_ = health;
    }
    
private:
    int health_;
};

// 💡 原则
// 1. 保持一致性
// 2. 使用有意义的名字
// 3. 避免缩写
```

### 问题 2：代码组织（Code Organization）

```cpp
// ✅ 头文件组织
// widget.h
#ifndef WIDGET_H
#define WIDGET_H

class Widget {
public:
    Widget(int x);
    void Function();
    
private:
    int x;
};

#endif

// ✅ 源文件组织
// widget.cpp
#include "widget.h"

Widget::Widget(int x) : x(x) {}

void Widget::Function() {
    // 实现
}

// 💡 原则
// 1. 头文件声明，源文件实现
// 2. 使用头文件保护
// 3. 避免循环依赖
```

### 问题 3：错误处理（Error Handling）

```cpp
// ✅ 错误处理策略
// 1. 返回错误码
int Function() {
    if (error) {
        return -1;  // 错误码
    }
    return 0;  // 成功
}

// 2. 抛出异常
void Function() {
    if (error) {
        throw std::runtime_error("Error");
    }
}

// 3. 使用 std::optional
std::optional<int> Function() {
    if (error) {
        return std::nullopt;
    }
    return 42;
}

// 💡 原则
// 1. 选择一种策略并坚持
// 2. 文档化错误处理策略
// 3. 不要混合使用多种策略
```

### 问题 4：性能优化（Performance Optimization）

```cpp
// ✅ 性能优化技术
// 1. 使用引用避免拷贝
void Function(const Widget &w) {
    // 只读访问，避免拷贝
}

// 2. 使用移动语义转移资源
Widget Function() {
    Widget w;
    return std::move(w);  // 移动而不是拷贝
}

// 3. 使用 RVO 优化返回值
Widget Function() {
    return Widget();  // 编译器优化
}

// 💡 原则
// 1. 先保证正确性
// 2. 使用性能分析工具
// 3. 只优化热点代码
```

### 问题 5：模板编程（Template Programming）

```cpp
// ✅ 模板
template <typename T>
class Container {
public:
    void Push(const T &elem) {
        elements_.push_back(elem);
    }
    
    T Pop() {
        T elem = elements_.back();
        elements_.pop_back();
        return elem;
    }
    
private:
    std::vector<T> elements_;
};

// ✅ 模板特化
template <>
class Container<bool> {
    // 针对 bool 的特化版本
};

// 💡 原则
// 1. 编写与类型无关的代码
// 2. 使用模板特化优化特定类型
// 3. 避免模板膨胀
```

### 问题 6：宏（Macro）

```cpp
// ❌ 宏
#define SQUARE(x) ((x) * (x))

// ✅ 内联函数
inline int Square(int x) {
    return x * x;
}

// ✅ 模板
template <typename T>
T Square(T x) {
    return x * x;
}

// 💡 原则
// 1. 避免使用宏
// 2. 使用内联函数或模板
// 3. 宏只用于条件编译和包含保护
```

---

## 难点解析

### 🔴 难点 1：异常安全保证

```cpp
// ✅ 基本保证
// 操作失败时，对象处于有效状态
// 可能改变了状态

// ✅ 强保证
// 操作失败时，对象保持原状态
// 要么完全成功，要么完全失败

// ✅ 不抛保证
// 操作不会抛出异常
// 通常用于析构函数和 swap

// 💡 如何实现强保证？
// 1. 使用 copy-and-swap
// 2. 使用 RAII
// 3. 使用智能指针
```

### 🔴 难点 2：const 正确性

```cpp
// ✅ const 成员函数
class Widget {
public:
    int GetHealth() const {
        return health;
    }
    
    void TakeDamage(int amount) {
        health -= amount;
    }
};

// ✅ const 对象
const Widget w;
w.GetHealth();  // ✅ 可以调用 const 成员函数
w.TakeDamage(10);  // ❌ 不能调用非 const 成员函数

// 💡 原则
// 1. 尽可能使用 const
// 2. const 是契约，编译器会检查
// 3. const 成员函数可以操作 const 对象
```

### 🔴 难点 3：资源管理

```cpp
// ✅ RAII
class FileHandle {
public:
    FileHandle(const char *filename) {
        file = fopen(filename, "r");
    }
    
    ~FileHandle() {
        if (file) fclose(file);
    }
    
private:
    FILE *file;
};

// ✅ 智能指针
std::unique_ptr<int> p = std::make_unique<int>(10);

// ✅ 容器
std::vector<int> v(100);

// 💡 原则
// 1. 使用 RAII 管理资源
// 2. 使用智能指针
// 3. 使用容器
```

### 🔴 难点 4：类型安全

```cpp
// ❌ 不安全的类型转换
int x = 3.14;  // 隐式转换，可能丢失精度

// ✅ 安全的类型转换
int x = static_cast<int>(3.14);  // 显式转换

// ✅ 使用类型安全的接口
enum class Month { Jan=1, Feb, Mar, ... };

Date d(Month::Aug, 30, 2023);  // ✅ 类型安全

// 💡 原则
// 1. 避免隐式类型转换
// 2. 使用强类型
// 3. 使用类型安全的接口
```

---

## Unity 对照

### 概念映射

| Exceptional C++ 原则 | Unity 对应 | 说明 |
|---------------------|------------|------|
| 异常安全 | Unity 错误处理 | 健壮性 |
| RAII | Unity 组件系统 | 资源管理 |
| const 正确性 | Unity readonly | 不可变性 |
| 类型安全 | Unity 类型系统 | 安全性 |

### Unity 中的应用

```cpp
// 1. 异常安全
// Unity 的 C# 有异常处理
// 但 Unity 不建议在 Update 中使用异常
// 理解异常安全可以帮助编写健壮的 Unity 代码

// 2. 资源管理
// Unity 的组件系统 ≈ RAII
// Unity 的 Destroy() ≈ 析构函数
// 理解 RAII 可以帮助理解 Unity 的资源管理

// 3. 类型安全
// Unity 的类型系统是类型安全的
// 但理解 C++ 的类型安全可以帮助编写更好的代码

// 💡 学习建议
// 1. 先理解 Exceptional C++ 原则
// 2. 对比 Unity 的实现方式
// 3. 思考为什么这些原则重要
// 4. 将原则应用到 Unity 开发中
```

### 代码对比

```cpp
// Exceptional C++：RAII
class FileHandle {
public:
    FileHandle(const char *filename) {
        file = fopen(filename, "r");
    }
    
    ~FileHandle() {
        if (file) fclose(file);
    }
    
private:
    FILE *file;
};

// Unity C#：IDisposable
public class FileHandle : IDisposable {
    private FileStream stream;
    
    public FileHandle(string filename) {
        stream = File.OpenRead(filename);
    }
    
    public void Dispose() {
        stream?.Dispose();
    }
}

// 💡 区别
// C++：RAII 自动释放资源
// Unity：IDisposable 手动释放资源
// C++：析构函数自动调用
// Unity：需要手动调用 Dispose()
```

---

## 📝 学习建议

### 阅读顺序
1. 先读 Exceptional C++（基础问题）
2. 再读 More Exceptional C++（进阶问题）
3. 最后读 Exceptional C++ Style（风格问题）

### 实践方法
1. 每学一个问题，写一个示例
2. 对比 Unity 的实现方式
3. 思考为什么这些问题重要
4. 将解决方案应用到项目中

### 常见错误
1. 忘记异常安全
2. 不理解 const 正确性
3. 资源管理错误
4. 类型不安全

### 推荐练习
1. 实现一个异常安全的类
2. 实现一个 RAII 资源管理类
3. 实现一个类型安全的接口
4. 实现一个 const 正确的类

---

*本文件持续更新中...*
