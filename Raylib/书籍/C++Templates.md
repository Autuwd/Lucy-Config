# C++ Templates: The Complete Guide 学习笔记

> **作者**：David Vandevoorde, Nicolai M. Josuttis, Douglas Gregor
> **版本**：第2版（覆盖 C++17/20）
> **地位**：模板编程的百科全书

---

## 📋 目录

1. [函数模板](#函数模板)
2. [类模板](#类模板)
3. [非类型模板参数](#非类型模板参数)
4. [可变参数模板](#可变参数模板)
5. [特化和重载](#特化和重载)
6. [SFINAE](#sfinae)
7. [Concepts (C++20)](#concepts-c20)
8. [难点解析](#难点解析)
9. [Unity 对照](#unity-对照)

---

## 函数模板

### 基本语法

```cpp
// ✅ 函数模板
template <typename T>
T Max(T a, T b) {
    return (a > b) ? a : b;
}

// ✅ 使用
int main() {
    int x = Max(10, 20);          // T = int
    double y = Max(3.14, 2.71);   // T = double
    std::string s = Max("hello", "world");  // T = const char*
}

// ✅ 显式指定类型
int x = Max<int>(10, 20);  // 显式指定 T = int

// ✅ 模板参数推导
template <typename T>
void Function(T a, T b) {
    // T 从参数推导
}

Function(10, 20);      // T = int
Function(3.14, 2.71);  // T = double
```

### 类型推导

```cpp
// ✅ 模板参数推导规则
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

### 重载解析

```cpp
// ✅ 函数模板重载
template <typename T>
void Function(T a) {
    std::cout << "Template: " << a << std::endl;
}

void Function(int a) {
    std::cout << "Non-template: " << a << std::endl;
}

// ✅ 调用
Function(10);    // Non-template: 10（非模板优先）
Function(3.14);  // Template: 3.14（模板匹配）

// ✅ 显式指定类型
Function<int>(10);  // Template: 10（显式指定模板）

// 💡 重载解析规则
// 1. 非模板函数优先
// 2. 更特化的模板优先
// 3. 显式指定类型优先
```

---

## 类模板

### 基本语法

```cpp
// ✅ 类模板
template <typename T>
class Stack {
private:
    std::vector<T> elements;
    
public:
    void Push(const T &elem) {
        elements.push_back(elem);
    }
    
    T Pop() {
        T elem = elements.back();
        elements.pop_back();
        return elem;
    }
    
    bool Empty() const {
        return elements.empty();
    }
};

// ✅ 使用
int main() {
    Stack<int> intStack;
    Stack<std::string> stringStack;
    
    intStack.Push(10);
    stringStack.Push("hello");
}
```

### 模板成员函数

```cpp
// ✅ 类模板的成员函数
template <typename T>
class Stack {
private:
    std::vector<T> elements;
    
public:
    void Push(const T &elem);
    T Pop();
    bool Empty() const;
};

// ✅ 成员函数定义
template <typename T>
void Stack<T>::Push(const T &elem) {
    elements.push_back(elem);
}

template <typename T>
T Stack<T>::Pop() {
    T elem = elements.back();
    elements.pop_back();
    return elem;
}

template <typename T>
bool Stack<T>::Empty() const {
    return elements.empty();
}

// 💡 成员函数是隐式内联的
// 只有在调用时才会实例化
```

### 模板特化

```cpp
// ✅ 类模板特化
template <typename T>
class Stack {
    // 通用版本
};

// ✅ 特化为 bool 类型
template <>
class Stack<bool> {
    // 针对 bool 的特化版本
};

// ✅ 偏特化
template <typename T>
class Stack<T*> {
    // 针对指针类型的特化版本
};

// ✅ 使用
Stack<int> s1;          // 通用版本
Stack<bool> s2;         // 特化版本
Stack<int*> s3;         // 偏特化版本

// 💡 特化规则
// 1. 先匹配全特化
// 2. 再匹配偏特化
// 3. 最后匹配通用模板
```

---

## 非类型模板参数

### 基本用法

```cpp
// ✅ 非类型模板参数
template <typename T, int N>
class Array {
private:
    T data[N];
    
public:
    T& operator[](int index) {
        return data[index];
    }
    
    int Size() const {
        return N;
    }
};

// ✅ 使用
int main() {
    Array<int, 10> arr;
    arr[0] = 10;
    std::cout << arr.Size() << std::endl;  // 10
}

// ✅ 非类型模板参数的类型
// 1. 整型（int, long, size_t 等）
// 2. 指针
// 3. 引用
// 4. 枚举

// ❌ 不支持的类型
// 1. 浮点数
// 2. 类对象
// 3. 字符串
```

### 非类型模板参数的推导

```cpp
// ✅ C++17：类模板参数推导（CTAD）
template <typename T, int N>
class Array {
    // ...
};

Array<int, 10> arr1;  // 旧式：显式指定
Array arr2{1, 2, 3};  // C++17：自动推导

// ✅ C++17：推导指引
template <typename T, int N>
Array(T (&)[N]) -> Array<T, N>;

int arr[] = {1, 2, 3};
Array a(arr);  // 推导为 Array<int, 3>

// 💡 CTAD 的优点
// 1. 减少冗余
// 2. 更易读
// 3. 更安全
```

---

## 可变参数模板

### 基本语法

```cpp
// ✅ 可变参数模板
template <typename... Args>
void Function(Args... args) {
    // args 是参数包
}

// ✅ 使用
Function();           // 空参数包
Function(10);         // 一个参数
Function(10, 3.14);   // 两个参数

// ✅ sizeof... 获取参数数量
template <typename... Args>
void Function(Args... args) {
    std::cout << sizeof...(args) << std::endl;
}

// ✅ 递归展开
template <typename T>
void Print(T value) {
    std::cout << value << std::endl;
}

template <typename T, typename... Args>
void Print(T first, Args... rest) {
    std::cout << first << " ";
    Print(rest...);
}

Print(1, 2, 3, 4, 5);  // 输出：1 2 3 4 5
```

### 折叠表达式

```cpp
// ✅ 折叠表达式（C++17）
template <typename... Args>
auto Sum(Args... args) {
    return (args + ...);  // 一元右折叠
}

int result = Sum(1, 2, 3, 4, 5);  // 15

// ✅ 其他折叠表达式
template <typename... Args>
void Print(Args... args) {
    (std::cout << ... << args) << std::endl;  // 一元左折叠
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

### 完美转发

```cpp
// ✅ 完美转发
template <typename... Args>
void Wrapper(Args&&... args) {
    Function(std::forward<Args>(args)...);
}

// ✅ 使用
Wrapper(10, 3.14, "hello");

// 💡 完美转发的原则
// 1. 使用万能引用（T&&）
// 2. 使用 std::forward 转发
// 3. 保持原始值类别
```

---

## 特化和重载

### 函数模板特化

```cpp
// ✅ 函数模板特化
template <typename T>
T Max(T a, T b) {
    return (a > b) ? a : b;
}

// ✅ 特化为 const char*
template <>
const char* Max<const char*>(const char* a, const char* b) {
    return (strcmp(a, b) > 0) ? a : b;
}

// ✅ 使用
int x = Max(10, 20);          // 通用版本
const char* s = Max("hello", "world");  // 特化版本

// 💡 特化规则
// 1. 特化版本必须匹配通用版本
// 2. 特化版本可以有不同的实现
// 3. 特化版本在调用时优先匹配
```

### 类模板特化

```cpp
// ✅ 全特化
template <typename T>
class Stack {
    // 通用版本
};

template <>
class Stack<bool> {
    // 针对 bool 的特化版本
};

// ✅ 偏特化
template <typename T>
class Stack<T*> {
    // 针对指针类型的特化版本
};

// ✅ 偏特化为引用
template <typename T>
class Stack<T&> {
    // 针对引用类型的特化版本
};

// ✅ 偏特化为数组
template <typename T, int N>
class Stack<T[N]> {
    // 针对数组类型的特化版本
};

// 💡 特化顺序
// 1. 全特化
// 2. 偏特化
// 3. 通用模板
```

### 部分排序

```cpp
// ✅ 部分排序规则
// 1. 更特化的版本优先
// 2. 编译器选择最匹配的版本

// ✅ 示例
template <typename T>
void Function(T a) {
    std::cout << "通用版本" << std::endl;
}

template <typename T>
void Function(T* a) {
    std::cout << "指针版本" << std::endl;
}

template <typename T>
void Function(T& a) {
    std::cout << "引用版本" << std::endl;
}

int x = 10;
Function(x);      // 通用版本
Function(&x);     // 指针版本
Function(x);      // 引用版本

// 💡 部分排序
// 1. 指针版本比通用版本更特化
// 2. 引用版本比通用版本更特化
// 3. 编译器选择最匹配的版本
```

---

## SFINAE

### 基本概念

```cpp
// ✅ SFINAE（Substitution Failure Is Not An Error）
// 如果模板参数替换失败，不是错误
// 而是尝试下一个可用的模板

// ✅ 示例：检测类型是否有某个成员函数
template <typename T>
class HasFunction {
private:
    typedef char Yes[1];
    typedef char No[2];
    
    template <typename C>
    static Yes& Test(decltype(&C::Function));
    
    template <typename C>
    static No& Test(...);
    
public:
    static const bool value = sizeof(Test<T>(0)) == sizeof(Yes);
};

// ✅ 使用
std::cout << HasFunction<int>::value << std::endl;  // false
std::cout << HasFunction<Widget>::value << std::endl;  // true
```

### SFINAE 应用

```cpp
// ✅ SFINAE 在模板中的应用
template <typename T>
typename std::enable_if<std::is_integral<T>::value, T>::type
Function(T a) {
    return a * 2;
}

template <typename T>
typename std::enable_if<std::is_floating_point<T>::value, T>::type
Function(T a) {
    return a * 3.14;
}

// ✅ 使用
int x = Function(10);      // 整型版本
double y = Function(3.14);  // 浮点版本

// ✅ C++17：if constexpr
template <typename T>
auto Function(T a) {
    if constexpr (std::is_integral_v<T>) {
        return a * 2;
    } else if constexpr (std::is_floating_point_v<T>) {
        return a * 3.14;
    }
}
```

### std::enable_if

```cpp
// ✅ std::enable_if 基本用法
template <typename T>
typename std::enable_if<std::is_integral<T>::value, T>::type
Function(T a) {
    return a * 2;
}

// ✅ C++14：简化语法
template <typename T>
std::enable_if_t<std::is_integral_v<T>, T>
Function(T a) {
    return a * 2;
}

// ✅ C++17：if constexpr 替代
template <typename T>
auto Function(T a) {
    if constexpr (std::is_integral_v<T>) {
        return a * 2;
    } else {
        return a * 3.14;
    }
}

// 💡 std::enable_if 的用途
// 1. 条件编译模板
// 2. 类型特征检查
// 3. SFINAE 技术
```

### 概念（Concepts）预览

```cpp
// ✅ C++20 Concepts 预览
template <typename T>
concept Integral = std::is_integral_v<T>;

template <typename T>
concept FloatingPoint = std::is_floating_point_v<T>;

// ✅ 使用 concepts
template <Integral T>
T Function(T a) {
    return a * 2;
}

template <FloatingPoint T>
T Function(T a) {
    return a * 3.14;
}

// ✅ C++20 语法
template <typename T>
    requires Integral<T>
T Function(T a) {
    return a * 2;
}

// ✅ 简化语法
Integral auto Function(Integral auto a) {
    return a * 2;
}
```

---

## Concepts (C++20)

### 基本概念

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

### 标准 concepts

```cpp
#include <concepts>

// ✅ 标准 concepts
std::integral auto x = 10;        // 整型
std::floating_point auto y = 3.14;  // 浮点型
std::copyable auto z = 10;       // 可复制
std::movable auto w = 10;        // 可移动

// ✅ 组合 concepts
template <typename T>
concept Printable = std::integral<T> || std::floating_point<T>;

// ✅ 使用
template <Printable T>
void Print(T value) {
    std::cout << value << std::endl;
}

// ✅ 约束 auto
Printable auto x = 10;
Printable auto y = 3.14;

// 💡 标准 concepts
// 1. std::integral：整型
// 2. std::floating_point：浮点型
// 3. std::copyable：可复制
// 4. std::movable：可移动
// 5. std::equality_comparable：可比较相等
```

### requires 表达式

```cpp
// ✅ requires 表达式
template <typename T>
concept HasFunction = requires(T t) {
    t.Function();
};

template <typename T>
concept HasValue = requires(T t) {
    { t.value } -> std::convertible_to<int>;
};

// ✅ 使用
template <HasFunction T>
void CallFunction(T obj) {
    obj.Function();
}

template <HasValue T>
void AccessValue(T obj) {
    int value = obj.value;
}

// ✅ 组合 requirements
template <typename T>
concept HasBoth = requires(T t) {
    t.Function();
    { t.value } -> std::convertible_to<int>;
};

// 💡 requires 表达式
// 1. 检查类型是否有特定成员
// 2. 检查表达式是否有效
// 3. 组合多个要求
```

---

## 难点解析

### 🔴 难点 1：模板实例化

```cpp
// ✅ 模板实例化过程
template <typename T>
T Max(T a, T b) {
    return (a > b) ? a : b;
}

int main() {
    int x = Max(10, 20);      // 编译器生成 Max<int>
    double y = Max(3.14, 2.71);  // 编译器生成 Max<double>
}

// 💡 实例化时机
// 1. 编译时实例化
// 2. 为每个使用的类型生成独立的代码
// 3. 可能导致代码膨胀

// ✅ 显式实例化（避免重复）
template int Max<int>(int, int);
template double Max<double>(double, double);
```

### 🔴 难点 2：模板偏特化

```cpp
// ✅ 全特化
template <>
class Widget<int> {
    // 针对 int 的特化版本
};

// ✅ 偏特化
template <typename T>
class Widget<T*> {
    // 针对指针类型的特化版本
};

// ✅ 成员偏特化
template <typename T>
class Widget {
public:
    template <typename U>
    void Function(U value);
};

// ✅ 成员全特化
template <>
template <>
void Widget<int>::Function<double>(double value) {
    // 针对 Widget<int> 和 Function<double> 的特化
}

// 💡 特化规则
// 1. 先匹配全特化
// 2. 再匹配偏特化
// 3. 最后匹配通用模板
```

### 🔴 难点 3：SFINAE 详解

```cpp
// ✅ SFINAE 原理
// 如果模板参数替换失败，不是错误
// 而是尝试下一个可用的模板

// ✅ 示例：检测类型是否有某个成员函数
template <typename T>
class HasFunction {
private:
    typedef char Yes[1];
    typedef char No[2];
    
    template <typename C>
    static Yes& Test(decltype(&C::Function));
    
    template <typename C>
    static No& Test(...);
    
public:
    static const bool value = sizeof(Test<T>(0)) == sizeof(Yes);
};

// 💡 SFINAE 的应用
// 1. 编译时类型检查
// 2. 条件模板实例化
// 3. 模板元编程
```

### 🔴 难点 4：模板参数推导

```cpp
// ✅ 模板参数推导规则
template <typename T>
void Function(T param);

// 1. 传入引用
int x = 10;
Function(x);   // T = int, param = int

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

---

## Unity 对照

### 概念映射

| C++ Templates | Unity C# 泛型 | 说明 |
|---------------|---------------|------|
| 模板 | 泛型 | 类型参数化 |
| 特化 | 泛型约束 | 类型约束 |
| SFINAE | 泛型约束 | 条件编译 |
| Concepts | 泛型约束 | 更强的约束 |
| 可变参数模板 | params 关键字 | 可变参数 |

### Unity 中的应用

```cpp
// 1. Unity 的泛型
// Unity 的 C# 泛型比 C++ 模板简单
// 但原理相似

// 2. Unity 的泛型约束
// Unity 的泛型约束 ≈ C++ 的特化
// Unity 的 where T : class ≈ C++ 的 std::is_class

// 3. Unity 的 Unity.Collections
// Unity 的 NativeArray<T> 使用泛型
// 理解 C++ 模板可以帮助理解 Unity 泛型

// 💡 学习建议
// 1. 先理解 C++ 模板
// 2. 对比 Unity 泛型
// 3. 思考为什么 Unity 泛型比 C++ 模板简单
// 4. 将 C++ 模板知识应用到 Unity 开发中
```

### 代码对比

```cpp
// C++ 模板
template <typename T>
class Stack {
    std::vector<T> elements;
public:
    void Push(const T &elem);
    T Pop();
};

// Unity C# 泛型
public class Stack<T> {
    private List<T> elements;
    public void Push(T elem);
    public T Pop();
}

// 💡 区别
// C++：编译时实例化，可能代码膨胀
// Unity：运行时实例化，共享代码
// C++：更强大，更复杂
// Unity：更简单，更易用
```

---

## 📝 学习建议

### 阅读顺序
1. 先读函数模板和类模板
2. 再读非类型模板参数
3. 然后读可变参数模板
4. 最后读 SFINAE 和 Concepts

### 实践方法
1. 每学一个概念，写一个示例
2. 对比 Unity 的实现方式
3. 思考为什么模板这么强大
4. 将模板知识应用到项目中

### 常见错误
1. 模板实例化错误
2. SFINAE 陷阱
3. 模板偏特化错误
4. 模板参数推导错误

### 推荐练习
1. 实现一个简单的容器类
2. 实现一个简单的智能指针
3. 实现一个简单的函数对象
4. 实现一个简单的模板元编程

---

*本文件持续更新中...*
