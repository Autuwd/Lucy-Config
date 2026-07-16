# C++ Primer (第5版) 学习笔记

> **作者**：Stanley B. Lippman, Josee Lajoie, Barbara E. Moo
> **版本**：第5版（覆盖 C++11）
> **地位**：C++ 入门最经典的教材，没有之一

---

## 📋 目录

1. [基础篇](#基础篇)
2. [类和对象](#类和对象)
3. [泛型编程](#泛型编程)
4. [动态内存与智能指针](#动态内存与智能指针)
5. [移动语义与右值引用](#移动语义与右值引用)
6. [Lambda 表达式](#lambda-表达式)
7. [标准库容器](#标准库容器)
8. [IO 类](#io-类)
9. [难点解析](#难点解析)
10. [Unity 对照](#unity-对照)

---

## 基础篇

### 变量和基本类型

```cpp
// ✅ 基本类型大小（64位系统）
int:     4 字节
long:    4 或 8 字节（平台相关）
long long: 8 字节
float:   4 字节
double:  8 字节
char:    1 字节
bool:    1 字节

// ⚠️ 注意：不要假设类型的大小！
// 使用 sizeof() 获取实际大小
std::cout << sizeof(int) << std::endl;  // 输出 4
```

### const 限定符

```cpp
// ✅ const 的核心：声明后不能再修改
const int MAX_SIZE = 100;  // 运行时常量
MAX_SIZE = 200;  // ❌ 编译错误！

// 💡 const vs #define
#define MAX_SIZE 100  // 旧式宏，没有类型检查
const int MAX_SIZE = 100;  // ✅ 推荐：有类型检查

// 🔴 难点：const 指针
const int *p1;      // 指向常量的指针：*p1 不能改，p1 能改
int *const p2 = &x; // 常量指针：p2 不能改，*p2 能改
const int *const p3 = &x;  // 指向常量的常量指针：都不能改
```

### 引用

```cpp
// ✅ 引用是变量的别名
int x = 10;
int &ref = x;  // ref 是 x 的别名
ref = 20;      // x 也变成 20

// ⚠️ 引用必须初始化
int &bad;  // ❌ 编译错误！

// 💡 引用 vs 指针
// 引用：必须初始化，不能重新绑定，更安全
// 指针：可以不初始化，可以重新指向，更灵活

// 🔴 Unity 对照：组件引用
// Unity 中 Transform 组件就是引用的概念
// this.transform ≈ 引用（不能为 null）
// GetComponent<Transform>() ≈ 指针（可能为 null）
```

### 函数

```cpp
// ✅ 函数重载
void print(int x);
void print(double x);
void print(const std::string &s);

// ✅ 默认参数
void draw(int x, int y, int width = 100, int height = 100);

// ⚠️ 默认参数必须从右到左
void bad(int x = 10, int y);  // ❌ 编译错误！

// 🔴 Unity 对照
// Unity 的组件方法就是函数重载
// Instantiate(prefab) 和 Instantiate(prefab, position, rotation)
```

---

## 类和对象

### 类的定义

```cpp
class Player {
private:
    std::string name;
    int health;
    
public:
    // 构造函数
    Player(const std::string &n, int h) : name(n), health(h) {}
    
    // 成员函数
    void TakeDamage(int amount) {
        health -= amount;
        if (health < 0) health = 0;
    }
    
    // getter
    int GetHealth() const { return health; }
};

// ✅ const 成员函数：承诺不修改对象
// 声明：int GetHealth() const;
// 定义：int Player::GetHealth() const { return health; }
```

### 构造函数

```cpp
class Vector2D {
public:
    float x, y;
    
    // ✅ 默认构造函数
    Vector2D() : x(0.0f), y(0.0f) {}
    
    // ✅ 带参数的构造函数
    Vector2D(float x, float y) : x(x), y(y) {}
    
    // ✅ 拷贝构造函数
    Vector2D(const Vector2D &other) : x(other.x), y(other.y) {}
    
    // ✅ 移动构造函数（C++11）
    Vector2D(Vector2D &&other) noexcept : x(other.x), y(other.y) {
        other.x = 0;
        other.y = 0;
    }
    
    // ✅ 构造函数初始化列表（推荐）
    Vector2D(float x, float y) : x(x), y(y) {
        // 构造函数体（效率低）
        // this->x = x;  // 不推荐
    }
};

// 💡 初始化列表 vs 赋值
// 初始化列表：直接构造成员（高效）
// 赋值：先默认构造，再赋值（低效）

// 🔴 Unity 对照
// Unity 的 MonoBehaviour 没有构造函数！
// Start() / Awake() ≈ 析构函数的作用
```

### 析构函数

```cpp
class Texture {
private:
    unsigned int id;
    
public:
    Texture(const char *path) {
        // 加载纹理...
        id = LoadTexture(path);
    }
    
    // ✅ 析构函数：释放资源
    ~Texture() {
        UnloadTexture(id);
    }
};

// ⚠️ 析构函数调用顺序
// 栈对象：后构造的先析构（LIFO）
// 堆对象：需要手动 delete

// 🔴 Unity 对照
// Unity 有垃圾回收，不需要手动析构
// 但在 C++ 中必须手动管理资源！
```

### 运算符重载

```cpp
class Vector2D {
public:
    float x, y;
    
    // ✅ 成员函数重载
    Vector2D operator+(const Vector2D &other) const {
        return Vector2D(x + other.x, y + other.y);
    }
    
    // ✅ 复合赋值运算符
    Vector2D& operator+=(const Vector2D &other) {
        x += other.x;
        y += other.y;
        return *this;
    }
    
    // ✅ 比较运算符
    bool operator==(const Vector2D &other) const {
        return x == other.x && y == other.y;
    }
    
    // ✅ 流输出运算符（非成员函数）
    friend std::ostream& operator<<(std::ostream &os, const Vector2D &v) {
        os << "(" << v.x << ", " << v.y << ")";
        return os;
    }
};

// 💡 运算符重载原则
// 1. 保持运算符原有的语义
// 2. 成员函数重载：左侧是 *this
// 3. 非成员函数重载：对称操作符（如 ==、+）
```

---

## 泛型编程

### 函数模板

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

// ✅ 模板特化
template <>
const char* Max<const char*>(const char* a, const char* b) {
    return (strcmp(a, b) > 0) ? a : b;
}
```

### 类模板

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
Stack<int> intStack;
Stack<std::string> stringStack;

// 💡 模板的实例化
// 编译器会为每个使用的类型生成一份代码
// Stack<int> 和 Stack<string> 是两个不同的类
```

### 类型推导

```cpp
// ✅ auto（C++11）
auto x = 10;          // int
auto y = 3.14;        // double
auto z = "hello";     // const char*

// ✅ decltype（C++11）
int a = 10;
decltype(a) b = 20;   // int

// ✅ 返回类型推导（C++14）
auto Add(int a, int b) {
    return a + b;  // 返回 int
}

// 💡 使用场景
// auto：变量类型明显时
// decltype：需要精确控制类型时
```

---

## 动态内存与智能指针

### 动态内存

```cpp
// ✅ new 和 delete
int *p = new int(10);     // 分配单个对象
int *arr = new int[100];  // 分配数组

delete p;      // 释放单个对象
delete[] arr;  // 释放数组

// 🔴 常见错误
// 1. 内存泄漏：忘记 delete
// 2. 重复释放：delete 同一块内存两次
// 3. 悬空指针：delete 后继续使用
```

### 智能指针（C++11）

```cpp
#include <memory>

// ✅ unique_ptr：独占所有权
std::unique_ptr<int> up1 = std::make_unique<int>(10);
std::unique_ptr<int> up2 = up1;  // ❌ 编译错误！独占

// ✅ shared_ptr：共享所有权
std::shared_ptr<int> sp1 = std::make_shared<int>(10);
std::shared_ptr<int> sp2 = sp1;  // ✅ 可以共享

// ✅ weak_ptr：弱引用（不增加引用计数）
std::weak_ptr<int> wp = sp1;
if (auto sp = wp.lock()) {
    // sp 是有效的 shared_ptr
    std::cout << *sp << std::endl;
}

// 💡 最佳实践
// 1. 优先使用 unique_ptr（性能最好）
// 2. 需要共享时使用 shared_ptr
// 3. 避免循环引用（用 weak_ptr 打破）
// 4. 不要混用智能指针和原始指针

// 🔴 Unity 对照
// Unity 的 GameObject 引用 ≈ shared_ptr
// Unity 没有 weak_ptr 的概念
// Unity 的 Destroy() ≈ 手动 delete
```

### 循环引用问题

```cpp
// ❌ 循环引用导致内存泄漏
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

// 🔴 这就是为什么 Unity 使用弱引用
// Unity 的 GetComponent 返回可能为 null 的指针
// 避免了循环引用问题
```

---

## 移动语义与右值引用

### 右值引用

```cpp
// ✅ 左值 vs 右值
int x = 10;     // x 是左值
int y = x + 10; // (x + 10) 是右值

// ✅ 右值引用
int &&rref = 10;  // rref 绑定到右值 10
rref = 20;        // 可以修改右值引用

// ✅ 移动构造函数
class Buffer {
private:
    int *data;
    size_t size;
    
public:
    // 移动构造函数
    Buffer(Buffer &&other) noexcept 
        : data(other.data), size(other.size) {
        other.data = nullptr;
        other.size = 0;
    }
};

// 💡 移动语义的意义
// 传统拷贝：深拷贝整个对象（慢）
// 移动：只转移资源所有权（快）
```

### std::move

```cpp
#include <utility>

// ✅ std::move：将左值转换为右值引用
std::vector<int> v1 = {1, 2, 3, 4, 5};
std::vector<int> v2 = std::move(v1);  // 移动，不是拷贝

// v1 现在处于有效但未指定的状态
// v2 现在拥有数据

// ✅ 完美转发
template <typename T>
void Wrapper(T &&arg) {
    // T && 是万能引用，可以绑定左值和右值
    Function(std::forward<T>(arg));  // 保持原始值类别
}

// 🔴 Unity 对照
// Unity 的 Instantiate 方法会复制对象
// C++ 的移动语义可以避免不必要的复制
```

---

## Lambda 表达式

### 基本语法

```cpp
// ✅ Lambda 表达式
auto lambda = [](int x, int y) { return x + y; };
int result = lambda(10, 20);  // result = 30

// ✅ 捕获列表
int a = 10, b = 20;
auto lambda1 = [a, b]() { return a + b; };     // 值捕获
auto lambda2 = [&a, &b]() { a++; b++; };        // 引用捕获
auto lambda3 = [=]() { return a + b; };         // 值捕获所有
auto lambda4 = [&]() { a++; b++; };             // 引用捕获所有

// ✅ Lambda 作为参数
std::vector<int> v = {5, 3, 1, 4, 2};
std::sort(v.begin(), v.end(), [](int a, int b) {
    return a < b;
});

// ✅ Lambda 作为返回值
auto MakeMultiplier(int factor) {
    return [factor](int x) { return x * factor; };
}

auto double_it = MakeMultiplier(2);
int result = double_it(5);  // result = 10
```

### Lambda 捕获详解

```cpp
// ✅ 值捕获（只读）
int x = 10;
auto lambda1 = [x]() { 
    std::cout << x << std::endl;  // ✅ 可以读
    // x = 20;  // ❌ 不能修改
};

// ✅ 可变值捕获（mutable）
auto lambda2 = [x]() mutable {
    x++;  // ✅ 可以修改
    std::cout << x << std::endl;
};

// ✅ 引用捕获（可读写）
auto lambda3 = [&x]() {
    x++;  // ✅ 可以修改原变量
};

// ⚠️ 悬空引用问题
auto BadLambda() {
    int x = 10;
    return [&x]() { return x; };  // ❌ 危险！x 已销毁
}

auto GoodLambda() {
    int x = 10;
    return [x]() { return x; };  // ✅ 值捕获，安全
}

// 🔴 Unity 对照
// Unity 的事件/委托 ≈ Lambda
// Unity 的闭包 ≈ Lambda 捕获
// Unity 的协程 ≈ Lambda + yield return
```

---

## 标准库容器

### 序列容器

```cpp
// ✅ vector：动态数组
std::vector<int> v = {1, 2, 3, 4, 5};
v.push_back(6);        // 添加元素
v.pop_back();          // 删除最后一个
v[0] = 10;             // 随机访问
v.at(0);               // 带边界检查的访问

// ✅ deque：双端队列
std::deque<int> dq = {1, 2, 3};
dq.push_front(0);      // 前端添加
dq.push_back(4);       // 后端添加

// ✅ list：双向链表
std::list<int> lst = {1, 2, 3};
lst.push_back(4);
lst.push_front(0);
lst.remove(2);         // 删除值为 2 的元素

// ✅ forward_list：单向链表
std::forward_list<int> flst = {1, 2, 3};
flst.push_after(flst.begin(), 0);  // 在第一个元素后插入

// ✅ array：固定大小数组（C++11）
std::array<int, 5> arr = {1, 2, 3, 4, 5};
arr[0] = 10;
```

### 关联容器

```cpp
// ✅ set：有序集合
std::set<int> s = {5, 3, 1, 4, 2};
s.insert(6);           // 添加元素
s.erase(3);            // 删除元素
if (s.find(4) != s.end()) {
    std::cout << "Found 4" << std::endl;
}

// ✅ map：有序键值对
std::map<std::string, int> m;
m["health"] = 100;
m["attack"] = 50;
std::cout << m["health"] << std::endl;

// ✅ unordered_set：无序集合
std::unordered_set<int> us = {5, 3, 1, 4, 2};
us.insert(6);

// ✅ unordered_map：无序键值对
std::unordered_map<std::string, int> um;
um["health"] = 100;

// 💡 选择指南
// 需要有序 → set / map
// 需要快速查找 → unordered_set / unordered_map
// 需要保持插入顺序 → vector / deque
```

### 容器操作

```cpp
// ✅ 遍历
std::vector<int> v = {1, 2, 3, 4, 5};

// 传统 for 循环
for (int i = 0; i < v.size(); i++) {
    std::cout << v[i] << std::endl;
}

// 范围 for 循环（C++11）
for (const auto &elem : v) {
    std::cout << elem << std::endl;
}

// ✅ 算法
#include <algorithm>

std::vector<int> v = {5, 3, 1, 4, 2};
std::sort(v.begin(), v.end());           // 排序
auto it = std::find(v.begin(), v.end(), 3);  // 查找
bool found = std::binary_search(v.begin(), v.end(), 3);  // 二分查找

// ✅ Lambda + 算法
std::vector<int> v = {5, 3, 1, 4, 2};
std::sort(v.begin(), v.end(), [](int a, int b) {
    return a > b;  // 降序排序
});

// 🔴 Unity 对照
// Unity 的 List<T> ≈ std::vector
// Unity 的 Dictionary<K,V> ≈ std::map
// Unity 的 HashSet<T> ≈ std::set
```

---

## IO 类

### 文件 IO

```cpp
#include <fstream>
#include <iostream>

// ✅ 写入文件
std::ofstream outfile("data.txt");
outfile << "Health: 100" << std::endl;
outfile << "Attack: 50" << std::endl;
outfile.close();

// ✅ 读取文件
std::ifstream infile("data.txt");
std::string line;
while (std::getline(infile, line)) {
    std::cout << line << std::endl;
}
infile.close();

// ✅ 二进制文件
std::ofstream outfile("data.bin", std::ios::binary);
int health = 100;
outfile.write(reinterpret_cast<char*>(&health), sizeof(int));
outfile.close();

// ⚠️ 错误检查
std::ifstream infile("nonexistent.txt");
if (!infile) {
    std::cerr << "Failed to open file!" << std::endl;
}

// 🔴 Unity 对照
// Unity 的 PlayerPrefs ≈ 简化的文件 IO
// Unity 的 JSONUtility ≈ 文件序列化
// Unity 的 Addressables ≈ 高级资源加载
```

---

## 难点解析

### 🔴 难点 1：const 成员函数

```cpp
class Player {
public:
    int health;
    
    // ✅ const 成员函数：承诺不修改对象状态
    int GetHealth() const {
        // health = 100;  // ❌ 编译错误！const 函数不能修改
        return health;
    }
    
    // ✅ 非 const 成员函数：可以修改
    void TakeDamage(int amount) {
        health -= amount;
    }
};

// 💡 为什么需要 const？
// 1. 允许 const 对象调用
// 2. 表达意图：这个函数不会修改对象
// 3. 编译器可以优化
```

### 🔴 难点 2：模板实例化

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

### 🔴 难点 3：移动语义 vs 拷贝语义

```cpp
class Buffer {
public:
    int *data;
    size_t size;
    
    // ✅ 拷贝构造函数
    Buffer(const Buffer &other) {
        size = other.size;
        data = new int[size];
        std::copy(other.data, other.data + size, data);
    }
    
    // ✅ 移动构造函数
    Buffer(Buffer &&other) noexcept {
        size = other.size;
        data = other.data;
        other.data = nullptr;
        other.size = 0;
    }
};

// 💡 什么时候用移动？
// 1. 临时对象（右值）
// 2. 显式 std::move
// 3. 返回局部对象

// 🔴 陷阱：不要移动 const 对象
// std::move(const_obj) 实际上是拷贝，不是移动！
```

### 🔴 难点 4：Lambda 捕获陷阱

```cpp
// ❌ 陷阱 1：悬空引用
auto BadLambda() {
    int x = 10;
    return [&x]() { return x; };  // x 已销毁！
}

// ❌ 陷阱 2：隐式拷贝
auto Lambda = [x]() { return x; };
// 实际上 x 被拷贝了，修改原 x 不会影响 Lambda

// ❌ 陷阱 3：可变 Lambda 的陷阱
int count = 0;
auto Lambda = [count]() mutable {
    count++;  // 修改的是拷贝，不是原变量
};
Lambda();
std::cout << count << std::endl;  // 输出 0，不是 1！

// ✅ 正确做法
int count = 0;
auto Lambda = [&count]() {
    count++;  // 引用捕获，修改原变量
};
Lambda();
std::cout << count << std::endl;  // 输出 1
```

---

## Unity 对照

### 概念映射

| C++ 概念 | Unity 对应 | 说明 |
|----------|------------|------|
| 类 (class) | MonoBehaviour | Unity 的组件基类 |
| 构造函数 | Awake() | 初始化对象 |
| 析构函数 | OnDestroy() | 清理资源 |
| new | Instantiate() | 创建对象 |
| delete | Destroy() | 销毁对象 |
| 指针 | GetComponent<T>() | 获取组件引用 |
| 引用 | this.transform | 组件引用（不可为 null） |
| 模板 | 泛型 | 类型参数化 |
| Lambda | 事件/委托 | 匿名函数 |
| 智能指针 | GC 引用 | 自动内存管理 |
| 文件 IO | PlayerPrefs | 持久化存储 |

### 代码对比

```cpp
// C++ 风格
class Player {
public:
    int health;
    void TakeDamage(int amount) {
        health -= amount;
    }
};

// Unity C# 风格
public class Player : MonoBehaviour {
    public int health;
    public void TakeDamage(int amount) {
        health -= amount;
    }
}

// ⚠️ 关键区别
// 1. C++ 手动管理内存，Unity 有垃圾回收
// 2. C++ 有指针和引用，Unity 只有引用
// 3. C++ 模板更强大，Unity 泛型较简单
// 4. C++ 可以重载运算符，Unity 不行
```

### Unity 中的 C++ 知识应用

```cpp
// 1. 理解 Unity 的底层
// Unity 的 C# 代码最终会被 JIT 编译成机器码
// 理解 C++ 可以帮助理解 Unity 的性能优化

// 2. Unity Native 插件
// Unity 可以调用 C++ 编写的原生插件
// 理解 C++ 可以帮助编写高性能的原生代码

// 3. Unity DOTS/ECS
// Unity DOTS 使用 Burst Compiler 将 C# 编译成优化的机器码
// Burst Compiler 的底层就是 C++ 优化技术

// 💡 学习建议
// 1. 先理解 C++ 基础概念
// 2. 对比 Unity 的实现方式
// 3. 思考为什么 Unity 要这样设计
// 4. 将 C++ 知识应用到 Unity 开发中
```

---

## 📝 学习建议

### 阅读顺序
1. 先读基础篇（变量、函数、类）
2. 再读类和对象（构造、析构、运算符）
3. 然后读泛型编程（模板）
4. 最后读智能指针和移动语义

### 实践方法
1. 每学一个概念，写一个小例子
2. 对比 Unity 的实现方式
3. 思考为什么 C++ 要这样设计
4. 将 C++ 知识应用到 Unity 开发中

### 常见错误
1. 忘记 delete（内存泄漏）
2. 不理解 const 的含义
3. 混淆拷贝和移动语义
4. Lambda 捕获错误

### 推荐练习
1. 实现一个简单的 Vector2D 类
2. 实现一个简单的智能指针
3. 实现一个简单的容器类
4. 实现一个简单的文件 IO 类

---

*本文件持续更新中...*
