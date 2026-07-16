# Effective C++ (第3版) 学习笔记

> **作者**：Scott Meyers
> **版本**：第3版（覆盖 C++03，部分 C++11）
> **地位**：C++ 进阶必读，55条改善程序设计的建议

---

## 📋 目录

1. [习惯 C++](#习惯-c)
2. [构造/析构/赋值](#构造析构赋值)
3. [资源管理](#资源管理)
4. [设计与声明](#设计与声明)
5. [实现](#实现)
6. [继承与面向对象设计](#继承与面向对象设计)
7. [模板与泛型编程](#模板与泛型编程)
8. [定制 new 和 delete](#定制-new-和-delete)
9. [杂项讨论](#杂项讨论)
10. [难点解析](#难点解析)
11. [Unity 对照](#unity-对照)

---

## 习惯 C++

### 条款 1：理解 C++ 是一个语言联邦

```
C++ 由四个子语言组成：
1. C：C 遗留的部分
2. Object-Oriented C++：类、封装、继承、多态
3. Template C++：泛型编程
4. STL：模板库

💡 每个子语言有自己的规则，不要混用！
```

### 条款 2：尽量用 const、enum、inline 替换 #define

```cpp
// ❌ 旧式宏
#define MAX_SIZE 100
#define SQUARE(x) ((x) * (x))

// ✅ 推荐方式
const int MAX_SIZE = 100;           // 类型安全
inline int Square(int x) {          // 类型安全，调试友好
    return x * x;
}

// ✅ 类作用域内的常量
class Player {
public:
    static const int MAX_HEALTH = 100;  // 声明
};

// ✅ 枚举作为整型常量
class Player {
public:
    enum { MAX_HEALTH = 100 };  // 枚举成员
};

// 💡 为什么？
// 1. 宏没有类型检查
// 2. 宏可能产生意外的副作用
// 3. 宏不利于调试
```

### 条款 3：尽量用 const

```cpp
// ✅ const 修饰指针
const int *p1;      // 指向常量的指针
int *const p2;      // 常量指针
const int *const p3;  // 指向常量的常量指针

// ✅ const 修饰引用
void Print(const std::string &s);  // 只读引用

// ✅ const 修饰成员函数
class Player {
public:
    int GetHealth() const {  // 承诺不修改对象
        return health;
    }
};

// 💡 const 的好处
// 1. 编译器可以检查错误
// 2. 提高代码可读性
// 3. 允许操作 const 对象
```

### 条款 4：确定对象被使用前已初始化

```cpp
// ❌ 未定义行为
int x;              // x 可能是任意值
std::string s;      // s 是空字符串（已初始化）

// ✅ 使用初始化列表
class Player {
public:
    int health;
    std::string name;
    
    // ✅ 初始化列表（高效）
    Player(int h, const std::string &n) : health(h), name(n) {}
    
    // ❌ 构造函数体内赋值（低效）
    Player(int h, const std::string &n) {
        health = h;      // 先默认构造，再赋值
        name = n;
    }
};

// ✅ 使用 std::make_shared 初始化智能指针
auto sp = std::make_shared<int>(10);  // ✅ 推荐
std::shared_ptr<int> sp2(new int(10));  // ⚠️ 可能泄漏
```

---

## 构造/析构/赋值

### 条款 5：了解 C++ 自动生成的成员函数

```cpp
// ✅ 编译器自动生成的函数
class Player {
    // 1. 默认构造函数
    Player() {}
    
    // 2. 析构函数
    ~Player() {}
    
    // 3. 拷贝构造函数
    Player(const Player &other) {}
    
    // 4. 拷贝赋值运算符
    Player& operator=(const Player &other) {}
    
    // 5. 移动构造函数（C++11）
    Player(Player &&other) noexcept {}
    
    // 6. 移动赋值运算符（C++11）
    Player& operator=(Player &&other) noexcept {}
};

// 💡 自动生成条件
// 只有当类没有显式声明时才会生成
// 如果你声明了任何一个，其他可能不会生成
```

### 条款 6：若不想使用编译器自动生成的函数，就该明确拒绝

```cpp
// ❌ 旧式方法（C++03）
class Player {
private:
    Player(const Player &other);           // 声明为 private
    Player& operator=(const Player &other);
    
public:
    Player() {}
};

// ✅ C++11 方法：使用 = delete
class Player {
public:
    Player(const Player &other) = delete;
    Player& operator=(const Player &other) = delete;
    
    Player() {}
};

// 💡 使用场景
// 1. 单例模式
// 2. 禁止拷贝的资源管理类
// 3. 禁止赋值的不可变对象
```

### 条款 7：为多态基类声明虚析构函数

```cpp
// ❌ 错误示例
class Base {
public:
    // 没有虚析构函数
};

class Derived : public Base {
public:
    int *data;
    Derived() { data = new int[100]; }
    ~Derived() { delete[] data; }  // 不会调用！
};

Base *p = new Derived();
delete p;  // ❌ 内存泄漏！Derived 的析构函数不会调用

// ✅ 正确示例
class Base {
public:
    virtual ~Base() {}  // 虚析构函数
};

class Derived : public Base {
public:
    int *data;
    Derived() { data = new int[100]; }
    ~Derived() { delete[] data; }  // ✅ 会调用
};

Base *p = new Derived();
delete p;  // ✅ 正确释放

// 🔴 Unity 对照
// Unity 的 MonoBehaviour 没有虚析构函数
// Unity 使用垃圾回收，不需要手动析构
```

### 条款 8：别让异常逃离析构函数

```cpp
// ❌ 危险示例
class Database {
public:
    ~Database() {
        CloseConnection();  // 可能抛出异常！
    }
};

// ✅ 安全示例
class Database {
public:
    ~Database() {
        try {
            CloseConnection();
        } catch (...) {
            // 吞掉异常，或者记录日志
            // 析构函数不应该抛出异常
        }
    }
};

// 💡 为什么？
// 1. 析构函数在异常处理期间可能被调用
// 2. 如果析构函数抛出异常，可能导致程序终止
// 3. 应该在析构函数中处理所有异常
```

### 条款 9：绝不在构造和析构过程中调用虚函数

```cpp
// ❌ 错误示例
class Base {
public:
    Base() {
        VirtualFunction();  // 调用虚函数
    }
    
    virtual void VirtualFunction() {
        std::cout << "Base" << std::endl;
    }
};

class Derived : public Base {
public:
    virtual void VirtualFunction() {
        std::cout << "Derived" << std::endl;
    }
};

Derived d;  // 输出 "Base"，不是 "Derived"！

// 💡 为什么？
// 构造期间，对象类型还是 Base
// Derived 的构造函数还没执行
// 所以调用的是 Base::VirtualFunction()
```

### 条款 10：令 operator= 返回一个 reference to *this

```cpp
// ✅ 链式赋值
class Player {
public:
    int health;
    int attack;
    
    Player& operator=(const Player &other) {
        if (this != &other) {
            health = other.health;
            attack = other.attack;
        }
        return *this;  // ✅ 返回引用
    }
};

// 使用
Player p1, p2, p3;
p1 = p2 = p3;  // ✅ 链式赋值
```

### 条款 11：在 operator= 中处理自我赋值

```cpp
// ❌ 不安全示例
Player& operator=(const Player &other) {
    delete[] data;          // 如果 this == &other，数据被删除
    data = new int[other.size];  // 然后访问已删除的数据
    // ...
}

// ✅ 安全示例 1：证同测试
Player& operator=(const Player &other) {
    if (this == &other) return *this;  // 证同测试
    delete[] data;
    data = new int[other.size];
    return *this;
}

// ✅ 安全示例 2：copy-and-swap
Player& operator=(Player other) {  // 注意：按值传递
    std::swap(data, other.data);
    std::swap(size, other.size);
    return *this;
}
```

### 条款 12：复制对象时勿忘其每一个成分

```cpp
// ❌ 不完整示例
class Player {
public:
    std::string name;
    int *data;
    
    Player(const Player &other) : name(other.name) {
        // 忘记复制 data！
    }
};

// ✅ 完整示例
class Player {
public:
    std::string name;
    int *data;
    size_t size;
    
    Player(const Player &other) 
        : name(other.name), size(other.size) {
        data = new int[size];
        std::copy(other.data, other.data + size, data);
    }
    
    Player& operator=(const Player &other) {
        if (this != &other) {
            name = other.name;
            size = other.size;
            delete[] data;
            data = new int[size];
            std::copy(other.data, other.data + size, data);
        }
        return *this;
    }
};
```

---

## 资源管理

### 条款 13：以对象管理资源

```cpp
// ❌ 裸指针容易泄漏
void Process() {
    Investment *p = CreateInvestment();
    // ... 可能抛出异常
    delete p;  // 可能不会执行
}

// ✅ 使用智能指针
void Process() {
    std::unique_ptr<Investment> p(CreateInvestment());
    // ... 即使抛出异常，也会自动删除
}
// 离开作用域时自动删除

// ✅ 使用自定义 RAII 类
class Investment {
public:
    static Investment* Create() {
        return new Investment();
    }
};

// RAII：资源获取即初始化
// 1. 在构造函数中获取资源
// 2. 在析构函数中释放资源
// 3. 利用栈对象的自动析构特性
```

### 条款 14：在资源管理类中小心 copying 行为

```cpp
// ✅ 禁止复制
class Lock {
public:
    Lock(const std::string &mutex) : mutex(mutex) {
        LockMutex(mutex);
    }
    
    ~Lock() {
        UnlockMutex(mutex);
    }
    
    Lock(const Lock&) = delete;
    Lock& operator=(const Lock&) = delete;
};

// ✅ 深拷贝
class FileHandle {
public:
    FileHandle(const char *filename) {
        file = fopen(filename, "r");
    }
    
    ~FileHandle() {
        if (file) fclose(file);
    }
    
    FileHandle(const FileHandle &other) {
        // 复制文件内容
        file = CopyFile(other.file);
    }
};

// ✅ 转移所有权
class Mutex {
public:
    Mutex(const Mutex&) = delete;
    Mutex& operator=(const Mutex&) = delete;
    
    Mutex(Mutex &&other) noexcept : lock(other.lock) {
        other.lock = nullptr;
    }
};
```

### 条款 15：在资源管理类中提供对原始资源的访问

```cpp
// ✅ 使用 get() 访问原始指针
std::unique_ptr<int> up = std::make_unique<int>(10);
int *raw = up.get();  // 获取原始指针

// ✅ 使用显式转换
class Lock {
public:
    explicit Lock(const std::string &mutex) : mutex(mutex) {
        LockMutex(mutex);
    }
    
    ~Lock() {
        UnlockMutex(mutex);
    }
    
    // ✅ 显式转换
    explicit operator const std::string&() const {
        return mutex;
    }
    
private:
    std::string mutex;
};

// 💡 为什么需要访问原始资源？
// 1. 与 C API 交互
// 2. 性能优化
// 3. 传递给期望原始资源的函数
```

### 条款 16：成对使用 new 和 delete 时要采取相同形式

```cpp
// ❌ 不匹配
int *p1 = new int[100];
delete p1;      // ❌ 应该用 delete[]

int *p2 = new int;
delete[] p2;    // ❌ 应该用 delete

// ✅ 匹配
int *p1 = new int[100];
delete[] p1;    // ✅ 数组形式

int *p2 = new int;
delete p2;      // ✅ 单个对象形式

// 💡 使用智能指针避免问题
std::vector<int> v(100);  // ✅ 推荐
```

### 条款 17：以独立语句将 newed 对象置入智能指针

```cpp
// ❌ 危险示例
ProcessWidget(std::shared_ptr<Widget>(new Widget), ComputePriority());

// 可能的执行顺序：
// 1. new Widget
// 2. ComputePriority()
// 3. shared_ptr 构造

// 如果 2 抛出异常，1 的内存会泄漏！

// ✅ 安全示例
auto pw = std::make_shared<Widget>();
ProcessWidget(pw, ComputePriority());

// 💡 原则
// 1. 使用 make_shared 或 make_unique
// 2. 不要在同一个语句中混合 new 和智能指针
```

---

## 设计与声明

### 条款 18：让接口容易被正确使用，不易被误用

```cpp
// ❌ 容易误用
Date(int month, int day, int year);

Date d(30, 8, 2023);  // 顺序错误！

// ✅ 使用类型安全的接口
enum class Month { Jan=1, Feb, Mar, ... };

Date d(Month::Aug, 30, 2023);  // ✅ 清晰

// ✅ 使用工厂函数
std::shared_ptr<Date> CreateDate(Month m, int d, int y);

// 💡 设计原则
// 1. 使用强类型
// 2. 避免类型转换
// 3. 提供有意义的错误信息
```

### 条款 19：设计 class 犹如设计 type

```cpp
// ✅ 设计一个新类型时考虑：
// 1. 构造和析构函数
// 2. 拷贝和移动操作
// 3. 运算符重载
// 4. 成员函数
// 5. 访问控制

class Vector2D {
public:
    // 构造和析构
    Vector2D(float x = 0, float y = 0);
    ~Vector2D() = default;
    
    // 拷贝和移动
    Vector2D(const Vector2D&) = default;
    Vector2D(Vector2D&&) noexcept = default;
    Vector2D& operator=(const Vector2D&) = default;
    Vector2D& operator=(Vector2D&&) noexcept = default;
    
    // 运算符
    Vector2D operator+(const Vector2D& other) const;
    Vector2D operator*(float scalar) const;
    bool operator==(const Vector2D& other) const;
    
    // 成员函数
    float Length() const;
    Vector2D Normalized() const;
    float Dot(const Vector2D& other) const;
    
    // 友元
    friend std::ostream& operator<<(std::ostream& os, const Vector2D& v);
    
private:
    float x, y;
};

// 💡 问自己
// 1. 这个类型需要什么接口？
// 2. 这个类型需要什么数据？
// 3. 这个类型和其他类型有什么关系？
```

### 条款 20：宁以 const& 传值取代传值

```cpp
// ❌ 效率低
void Print(std::string s);  // 拷贝整个字符串

// ✅ 高效
void Print(const std::string &s);  // 只传递引用

// ✅ 对于内置类型
void Print(int x);  // ✅ 直接传递值更高效

// 💡 规则
// 1. 内置类型（int, double, 指针）：传值
// 2. 用户定义类型：传 const&
// 3. 不要传递空引用或空指针
```

### 条款 21：必须返回对象时，别妄想返回其 reference

```cpp
// ❌ 危险示例
const Vector2D& operator+(const Vector2D &a, const Vector2D &b) {
    Vector2D result(a.x + b.x, a.y + b.y);
    return result;  // ❌ 返回局部变量的引用！
}

// ✅ 安全示例
Vector2D operator+(const Vector2D &a, const Vector2D &b) {
    return Vector2D(a.x + b.x, a.y + b.y);  // 返回值
}

// 💡 规则
// 1. 如果函数返回局部变量，必须返回值
// 2. 不要返回局部变量的引用或指针
// 3. 返回 const 引用可以防止意外修改
```

### 条款 22：将成员变量声明为 private

```cpp
// ❌ 不好
class Player {
public:
    int health;  // 可以直接访问，无法控制
};

// ✅ 好
class Player {
public:
    int GetHealth() const { return health; }
    void SetHealth(int h) { 
        if (h < 0) h = 0;
        if (h > maxHealth) h = maxHealth;
        health = h; 
    }
    
private:
    int health;  // 通过接口访问
    int maxHealth;
};

// 💡 优点
// 1. 可以控制访问权限
// 2. 可以改变实现而不影响接口
// 3. 可以添加约束和验证
```

### 条款 23：宁以 non-member、non-friend 替换 member 函数

```cpp
// ❌ Member 函数
class WebBrowser {
public:
    void ClearCache();
    void ClearHistory();
    void ClearCookies();
    
    void ClearEverything() {
        ClearCache();
        ClearHistory();
        ClearCookies();
    }
};

// ✅ Non-member 函数
class WebBrowser {
public:
    void ClearCache();
    void ClearHistory();
    void ClearCookies();
};

void ClearBrowser(WebBrowser &browser) {
    browser.ClearCache();
    browser.ClearHistory();
    browser.ClearCookies();
}

// 💡 优点
// 1. 封装性更好
// 2. 可扩展性更强
// 3. 可以放在单独的头文件中
```

### 条款 24：若所有参数皆需类型转换，请为此采用 non-member 函数

```cpp
// ❌ Member 函数
class Rational {
public:
    Rational(int numerator = 0, int denominator = 1);
    
    const Rational operator*(const Rational &rhs) const {
        return Rational(numerator * rhs.numerator, 
                       denominator * rhs.denominator);
    }
    
private:
    int numerator, denominator;
};

Rational r1(1, 2);
Rational r2 = r1 * 2;    // ✅ 可以
Rational r3 = 2 * r1;    // ❌ 错误！2 不能隐式转换为 Rational

// ✅ Non-member 函数
class Rational {
public:
    Rational(int numerator = 0, int denominator = 1);
    
    int Numerator() const { return numerator; }
    int Denominator() const { return denominator; }
    
private:
    int numerator, denominator;
};

const Rational operator*(const Rational &lhs, const Rational &rhs) {
    return Rational(lhs.Numerator() * rhs.Numerator(),
                   lhs.Denominator() * rhs.Denominator());
}

Rational r1(1, 2);
Rational r2 = r1 * 2;    // ✅ 可以
Rational r3 = 2 * r1;    // ✅ 可以
```

### 条款 25：考虑实现 swap 的异常安全性

```cpp
// ✅ 标准 swap 实现
class Widget {
public:
    void Swap(Widget &other) noexcept {
        using std::swap;
        swap(data, other.data);
        swap(size, other.size);
    }
    
private:
    int *data;
    size_t size;
};

// ✅ 使用 swap 实现异常安全的赋值
Widget& Widget::operator=(Widget other) {
    Swap(other);  // 交换内容
    return *this;
}

// 💡 异常安全保证
// 1. 基本保证：状态有效，但可能改变
// 2. 强保证：要么完全成功，要么保持原状态
// 3. 不抛保证：不会抛出异常
```

### 条款 26：尽可能延后变量定义式的出现

```cpp
// ❌ 不好
void Process(const std::string &filename) {
    std::string s;  // 定义但未使用
    
    std::ifstream file(filename);
    // ... 可能抛出异常
    
    s = ReadData(file);  // 定义时应该初始化
}

// ✅ 好
void Process(const std::string &filename) {
    std::ifstream file(filename);
    std::string s = ReadData(file);  // 定义时立即初始化
}

// 💡 优点
// 1. 减少不必要的构造和析构
// 2. 避免未定义行为
// 3. 提高可读性
```

### 条款 27：尽量少做转型动作

```cpp
// ❌ C 风格转型
double x = 3.14;
int y = (int)x;  // 不安全

// ✅ C++ 风格转型
double x = 3.14;
int y = static_cast<int>(x);  // 明确意图

// 🔴 常见问题
// 1. const_cast：去除 const（危险）
// 2. dynamic_cast：运行时类型检查（慢）
// 3. reinterpret_cast：重新解释内存（极度危险）
// 4. static_cast：编译时类型转换（相对安全）

// 💡 规则
// 1. 尽量避免转型
// 2. 如果必须使用，使用 C++ 风格
// 3. 不要使用 const_cast
```

### 条款 28：避免返回 handles 指向对象内部成分

```cpp
// ❌ 返回引用
class Player {
public:
    std::string name;
    
    std::string& GetName() {
        return name;  // 返回内部引用
    }
};

// 危险示例
Player player;
std::string &name = player.GetName();
player.name = "Changed";  // name 也被修改！

// ✅ 返回值
class Player {
public:
    std::string GetName() const {
        return name;  // 返回副本
    }
};

// 💡 原则
// 1. 不要返回内部成员的引用或指针
// 2. 返回值会创建副本，更安全
// 3. 如果需要性能，考虑返回 const 引用
```

### 条款 29：为"异常安全"而努力是值得的

```cpp
// ❌ 不安全示例
class GUI {
public:
    void AddWidget(Widget *w) {
        widgets.push_back(w);
        // 如果 push_back 抛出异常，w 可能泄漏
    }
};

// ✅ 安全示例
class GUI {
public:
    void AddWidget(std::shared_ptr<Widget> w) {
        widgets.push_back(w);  // 强异常安全
    }
};

// 💡 异常安全保证
// 1. 基本保证：操作失败时，对象处于有效状态
// 2. 强保证：操作失败时，对象保持原状态
// 3. 不抛保证：操作不会抛出异常

// 🔴 Unity 对照
// Unity 的 C# 有异常处理机制
// 但 Unity 不建议在 Update 中使用异常
// 应该避免异常，使用条件判断代替
```

---

## 实现

### 条款 30：透彻了解 inlining 的里里外外

```cpp
// ✅ inline 函数
inline int Square(int x) {
    return x * x;
}

// 💡 inline 的优缺点
// 优点：
// 1. 避免函数调用开销
// 2. 编译器可以优化
// 缺点：
// 1. 代码膨胀
// 2. 可能降低缓存命中率

// 💡 使用场景
// 1. 小型函数（1-5 行）
// 2. 频繁调用的函数
// 3. 简单的 getter/setter

// ❌ 不要 inline
// 1. 大型函数
// 2. 复杂的循环
// 3. 递归函数
```

### 条款 31：将文件间的编译依赖关系降至最低

```cpp
// ❌ 不好：头文件包含太多
// widget.h
#include <string>
#include <vector>
#include "SomeClass.h"

class Widget {
public:
    // ...
private:
    std::string name;
    std::vector<int> data;
    SomeClass sc;
};

// ✅ 好：使用前向声明
// widget.h
class SomeClass;  // 前向声明

class Widget {
public:
    // ...
private:
    std::string name;
    std::vector<int> data;
    SomeClass *sc;  // 指针或引用
};

// 💡 原则
// 1. 使用指针或引用
// 2. 前向声明代替 #include
// 3. 使用 Pimpl 惯用法
```

---

## 继承与面向对象设计

### 条款 32：确定你的 public 继承塑模出 is-a 关系

```cpp
// ✅ 正确的 is-a 关系
class Animal {
public:
    virtual void Eat() = 0;
};

class Dog : public Animal {
public:
    void Eat() override {
        // 狗吃东西
    }
};

// ❌ 错误的 is-a 关系
class Square : public Rectangle {
    // 正方形 is-a 矩形？
    // 但正方形的宽和高必须相等！
};

// 💡 原则
// 1. public 继承表示 is-a 关系
// 2. 子类必须能替代父类
// 3. 不要违反 Liskov 替换原则
```

### 条款 33：避免遮掩继承而来的名称

```cpp
// ❌ 名称遮掩
class Base {
public:
    void Function() { std::cout << "Base" << std::endl; }
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
```

### 条款 34：区分接口继承和实现继承

```cpp
// ✅ 纯接口继承
class Shape {
public:
    virtual void Draw() const = 0;  // 纯虚函数
    virtual double Area() const = 0;
};

// ✅ 接口 + 实现继承
class Circle : public Shape {
public:
    void Draw() const override {
        // 绘制圆形
    }
    
    double Area() const override {
        return 3.14 * radius * radius;
    }
    
private:
    double radius;
};

// 💡 原则
// 1. 纯虚函数：只定义接口
// 2. 虚函数：定义接口和默认实现
// 3. 非虚函数：定义接口和强制实现
```

### 条款 35：考虑 virtual 函数以外的其他选择

```cpp
// ✅ NVI（Non-Virtual Interface）惯用法
class Base {
public:
    // 公共接口（非虚函数）
    void Function() {
        DoFunction();  // 调用私有虚函数
    }
    
private:
    // 私有实现（虚函数）
    virtual void DoFunction() {
        // 默认实现
    }
};

// ✅ 使用函数指针
class Base {
public:
    using FunctionPtr = void (*)();
    
    Base(FunctionPtr ptr = DefaultFunction) : func(ptr) {}
    
    void Function() {
        func();  // 调用函数指针
    }
    
private:
    FunctionPtr func;
    
    static void DefaultFunction() {
        // 默认实现
    }
};

// 💡 优点
// 1. NVI：分离接口和实现
// 2. 函数指针：运行时可替换
// 3. 避免虚函数的开销
```

### 条款 36：绝不重新定义继承而来的非虚函数

```cpp
// ❌ 错误示例
class Base {
public:
    void Function() {  // 非虚函数
        std::cout << "Base" << std::endl;
    }
};

class Derived : public Base {
public:
    void Function() {  // 重新定义非虚函数
        std::cout << "Derived" << std::endl;
    }
};

Derived d;
Base *p = &d;
p->Function();  // 输出 "Base"（不是 "Derived"！）

// 💡 为什么？
// 非虚函数是静态绑定的
// 编译时根据指针类型决定调用哪个函数
// 与对象的实际类型无关
```

### 条款 37：绝不重新定义继承而来的缺省参数值

```cpp
// ❌ 错误示例
class Base {
public:
    virtual void Function(int x = 10) {
        std::cout << "Base: " << x << std::endl;
    }
};

class Derived : public Base {
public:
    void Function(int x = 20) override {  // 重新定义默认参数
        std::cout << "Derived: " << x << std::endl;
    }
};

Derived d;
Base *p = &d;
p->Function();  // 输出 "Derived: 10"（不是 20！）

// 💡 为什么？
// 默认参数是静态绑定的
// 对象类型是 Derived，但指针类型是 Base*
// 所以使用 Base 的默认参数 10

// ✅ 正确做法
class Base {
public:
    virtual void Function(int x = 10) {
        DoFunction(x);
    }
    
protected:
    virtual void DoFunction(int x) {
        std::cout << "Base: " << x << std::endl;
    }
};

class Derived : public Base {
protected:
    void DoFunction(int x) override {
        std::cout << "Derived: " << x << std::endl;
    }
};
```

### 条款 38：通过复合塑模出 has-a 或 is-implemented-in-terms-of 关系

```cpp
// ✅ Has-a 关系
class Car {
private:
    Engine engine;  // Car has-a Engine
    std::vector<Wheel> wheels;
};

// ✅ Is-implemented-in-terms-of 关系
class Stack {
private:
    std::vector<int> data;  // Stack 使用 vector 实现
};

// 💡 区别
// Has-a：组合关系，整体和部分
// Is-implemented-in-terms-of：实现细节，不暴露

// 🔴 Unity 对照
// Unity 的组件系统就是 Has-a 关系
// GameObject has-a Transform
// GameObject has-a Renderer
```

### 条款 39：明智而审慎地使用 private 继承

```cpp
// ❌ 不好：private 继承
class Derived : private Base {
    // Derived 不是 Base 的子类
    // 只是复用 Base 的实现
};

// ✅ 好：使用组合
class Derived {
private:
    Base base;  // 组合关系
};

// 💡 什么时候用 private 继承？
// 1. 需要访问 protected 成员
// 2. 需要重写虚函数
// 3. 需要优化（空基类优化）

// ⚠️ 一般情况下，优先使用组合
```

### 条款 40：明智而审慎地使用多重继承

```cpp
// ❌ 菱形继承问题
class A {
public:
    int data;
};

class B : public A {};
class C : public A {};
class D : public B, public C {};  // D 有两份 A::data

// ✅ 使用虚继承
class A {
public:
    int data;
};

class B : public virtual A {};  // 虚继承
class C : public virtual A {};
class D : public B, public C {};  // D 只有一份 A::data

// 💡 原则
// 1. 尽量避免多重继承
// 2. 如果必须使用，考虑虚继承
// 3. 使用接口类（纯虚基类）
```

---

## 模板与泛型编程

### 条款 41：了解隐式接口和编译期多态

```cpp
// ✅ 显式接口
class Widget {
public:
    void Function();  // 显式定义的接口
};

// ✅ 隐式接口（模板）
template <typename T>
void Process(T &obj) {
    obj.Function();  // 隐式接口：T 必须有 Function()
    obj.data = 10;   // 隐式接口：T 必须有 data 成员
}

// 💡 区别
// 显式接口：编译时检查
// 隐式接口：编译时检查，但更灵活

// 🔴 Unity 对照
// Unity 的泛型约束就是显式接口
// Unity 的 duck typing 就是隐式接口
```

### 条款 42：了解 typename 的双重意义

```cpp
// ✅ typename 表示类型参数
template <typename T>
class Widget {
    T data;  // T 是类型
};

// ✅ typename 告诉编译器这是类型
template <typename T>
void Function() {
    typename T::value_type x;  // 告诉编译器这是类型
}

// ❌ 不用 typename
template <typename T>
void Function() {
    T::value_type x;  // 编译器可能认为这是静态成员
}

// 💡 规则
// 1. 在模板参数列表中，typename 和 class 相同
// 2. 在模板定义中，typename 表示类型
```

### 条款 43：学习处理模板化基类内的名称

```cpp
// ❌ 问题
template <typename T>
class Base {
public:
    void Function() {}
};

template <typename T>
class Derived : public Base<T> {
public:
    void Call() {
        Function();  // ❌ 找不到 Function
    }
};

// ✅ 解决方案
template <typename T>
class Derived : public Base<T> {
public:
    void Call() {
        this->Function();  // ✅ 使用 this->
    }
};

// 💡 三种解决方案
// 1. 使用 this->
// 2. 使用 using 声明
// 3. 使用 Base<T>::Function()
```

### 条款 44：将与参数无关的代码抽离 templates

```cpp
// ❌ 不好：代码膨胀
template <typename T>
class Matrix {
public:
    void Multiply(const Matrix &other) {
        // 通用矩阵乘法
        // 每个类型 T 都会生成一份代码
    }
};

// ✅ 好：抽离公共代码
template <typename T>
class Matrix {
public:
    void Multiply(const Matrix &other) {
        MultiplyImpl(other);  // 调用非模板函数
    }
    
private:
    void MultiplyImpl(const Matrix &other) {
        // 通用矩阵乘法
    }
};

// 💡 原则
// 1. 将与类型无关的代码抽离
// 2. 使用非模板函数处理通用逻辑
// 3. 减少代码膨胀
```

### 条款 45：运用成员函数模板接受所有兼容类型

```cpp
// ✅ 成员函数模板
class Widget {
public:
    template <typename U>
    Widget(const Widget<U> &other) {
        // 从其他类型的 Widget 拷贝
    }
    
    template <typename U>
    Widget& operator=(const Widget<U> &other) {
        // 从其他类型的 Widget 赋值
        return *this;
    }
};

// ✅ 使用
Widget<int> w1;
Widget<double> w2 = w1;  // ✅ 可以转换

// 💡 原则
// 1. 使用成员函数模板实现类型转换
// 2. 仍然需要显式声明拷贝构造函数和赋值运算符
```

### 条款 46：需要类型转换时请为模板定义非成员函数

```cpp
// ❌ 问题
template <typename T>
class Rational {
public:
    Rational(int numerator = 0, int denominator = 1);
    
    const Rational operator*(const Rational &rhs) const {
        return Rational(numerator * rhs.numerator,
                       denominator * rhs.denominator);
    }
    
private:
    int numerator, denominator;
};

Rational<int> r1(1, 2);
Rational<int> r2 = r1 * 2;    // ✅ 可以
Rational<int> r3 = 2 * r1;    // ❌ 错误！

// ✅ 解决方案：非成员函数
template <typename T>
class Rational {
public:
    Rational(int numerator = 0, int denominator = 1);
    
    int Numerator() const { return numerator; }
    int Denominator() const { return denominator; }
    
private:
    int numerator, denominator;
};

template <typename T>
const Rational<T> operator*(const Rational<T> &lhs,
                           const Rational<T> &rhs) {
    return Rational<T>(lhs.Numerator() * rhs.Numerator(),
                      lhs.Denominator() * rhs.Denominator());
}

Rational<int> r1(1, 2);
Rational<int> r2 = r1 * 2;    // ✅ 可以
Rational<int> r3 = 2 * r1;    // ✅ 可以
```

### 条款 47：使用 traits classes 表现类型信息

```cpp
// ✅ Traits class
template <typename T>
struct TypeTraits {
    static const bool isPointer = false;
};

template <typename T>
struct TypeTraits<T*> {
    static const bool isPointer = true;
};

// ✅ 使用
template <typename T>
void Function(T value) {
    if (TypeTraits<T*>::isPointer) {
        // 指针类型
    } else {
        // 非指针类型
    }
}

// 💡 用途
// 1. 编译时类型检查
// 2. 条件编译
// 3. 模板特化
```

### 条款 48：认识 template 元编程

```cpp
// ✅ 编译时计算
template <int N>
struct Factorial {
    static const int value = N * Factorial<N - 1>::value;
};

template <>
struct Factorial<0> {
    static const int value = 1;
};

// 使用
int x = Factorial<5>::value;  // x = 120

// ✅ 编译时条件
template <bool B, typename T, typename F>
struct If {
    typedef T type;
};

template <typename T, typename F>
struct If<false, T, F> {
    typedef F type;
};

// 💡 元编程
// 1. 在编译时计算
// 2. 生成模板代码
// 3. 编译时类型检查
```

---

## 定制 new 和 delete

### 条款 49：了解 new-handler 的行为

```cpp
// ✅ 设置 new-handler
void OutOfMemory() {
    std::cerr << "Out of memory!" << std::endl;
    std::abort();
}

std::set_new_handler(OutOfMemory);

// ✅ 类特定的 new-handler
class Widget {
public:
    static void *operator new(size_t size) {
        Widget *p = static_cast<Widget*>(::operator new(size));
        return p;
    }
    
    static void operator delete(void *p) noexcept {
        ::operator delete(p);
    }
};

// 💡 new-handler
// 1. 当 new 失败时调用
// 2. 可以尝试释放内存
// 3. 可以抛出异常或终止程序
```

### 条款 50：了解定制 new 和 delete 的时机和场境

```cpp
// ✅ 何时需要定制？
// 1. 性能优化
// 2. 调试内存问题
// 3. 处理特殊内存需求

// ✅ 示例：内存池
class Widget {
public:
    static void *operator new(size_t size) {
        if (size != sizeof(Widget)) {
            return ::operator new(size);
        }
        
        // 使用内存池
        return AllocateFromPool();
    }
    
    static void operator delete(void *p, size_t size) noexcept {
        if (size != sizeof(Widget)) {
            ::operator delete(p);
            return;
        }
        
        // 归还内存池
        ReturnToPool(p);
    }
};
```

---

## 杂项讨论

### 条款 51：编写 new 和 delete 时要固守常规

```cpp
// ✅ operator new 规则
void *operator new(size_t size) {
    // 1. 检查大小
    if (size == 0) {
        size = 1;
    }
    
    // 2. 尝试分配
    while (true) {
        void *p = malloc(size);
        if (p) return p;
        
        // 3. 调用 new-handler
        NewHandler handler = GetNewHandler();
        if (handler) {
            handler();
        } else {
            throw std::bad_alloc();
        }
    }
}

// ✅ operator delete 规则
void operator delete(void *p) noexcept {
    if (p) {
        free(p);
    }
}
```

### 条款 52：写了 placement new 也要写 placement delete

```cpp
// ✅ Placement new
void *operator new(size_t size, void *p) noexcept {
    return p;
}

// ✅ Placement delete
void operator delete(void *p, void *place) noexcept {
    // 什么都不做
}

// 💡 原则
// 1. 如果定义了 placement new，也要定义对应的 placement delete
// 2. 否则可能导致内存泄漏
```

### 条款 53：不要轻忽编译器的警告

```cpp
// ❌ 忽略警告
class Base {
public:
    virtual void Function() {}
};

class Derived : public Base {
public:
    void Function() {}  // 编译器可能警告：缺少 override
};

// ✅ 修复警告
class Derived : public Base {
public:
    void Function() override {}  // ✅ 显式 override
};

// 💡 原则
// 1. 以最高警告级别编译
// 2. 修复所有警告
// 3. 不要依赖警告消息
```

### 条款 54：让自己熟悉包括 TR1 在内的标准程序库

```cpp
// ✅ C++11 标准库
#include <vector>
#include <map>
#include <set>
#include <algorithm>
#include <memory>
#include <functional>

// ✅ C++11 新特性
// 1. 智能指针：unique_ptr, shared_ptr, weak_ptr
// 2. 容器：array, forward_list, unordered_map
// 3. 算法：all_of, any_of, none_of
// 4. Lambda 表达式

// 💡 建议
// 1. 熟悉标准库
// 2. 使用 STL 算法
// 3. 避免重复造轮子
```

### 条款 55：让自己熟悉 Boost

```cpp
// ✅ Boost 库推荐
// 1. Boost智能指针：scoped_ptr, shared_ptr
// 2. Boost函数：function, bind
// 3. Boost容器：unordered_map, circular_buffer
// 4. Boost算法：range-based for, lambda

// 💡 Boost 的意义
// 1. 标准库的试验场
// 2. 补充标准库缺失的功能
// 3. 高质量的开源库
```

---

## 难点解析

### 🔴 难点 1：虚函数表（vtable）

```cpp
// ✅ 虚函数表原理
class Base {
public:
    virtual void Function1() {}
    virtual void Function2() {}
};

class Derived : public Base {
public:
    void Function1() override {}
    void Function2() override {}
};

// 内存布局
// Base 对象：
// [vptr][data]
// vptr -> Base vtable: [&Function1, &Function2]

// Derived 对象：
// [vptr][data]
// vptr -> Derived vtable: [&Derived::Function1, &Derived::Function2]

// 💡 为什么需要 vtable？
// 1. 实现运行时多态
// 2. 根据对象实际类型调用函数
// 3. 开销：一次额外的间接寻址
```

### 🔴 难点 2：异常安全保证

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

### 🔴 难点 3：模板偏特化

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

### 🔴 难点 4：SFINAE（Substitution Failure Is Not An Error）

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

// 💡 用途
// 1. 编译时类型检查
// 2. 条件模板实例化
// 3. 模板元编程
```

---

## Unity 对照

### 概念映射

| Effective C++ 原则 | Unity 对应 | 说明 |
|-------------------|------------|------|
| 条款 7：虚析构函数 | Unity 的 MonoBehaviour | Unity 没有虚析构函数，使用垃圾回收 |
| 条款 13：以对象管理资源 | Unity 的组件系统 | Unity 使用组件而不是继承 |
| 条款 22：成员变量 private | Unity 的 SerializeField | Unity 用属性控制可见性 |
| 条款 32：is-a 关系 | Unity 的继承体系 | Unity 的 MonoBehaviour 继承链 |
| 条款 38：组合优于继承 | Unity 的组件模式 | Unity 使用组件而不是深继承链 |

### Unity 中的应用

```cpp
// 1. 虚析构函数
// Unity 的 MonoBehaviour 没有虚析构函数
// 但 Unity 使用垃圾回收，不需要手动析构
// 理解虚析构函数可以帮助理解 Unity 的底层

// 2. 资源管理
// Unity 的 Resources.Load ≈ RAII
// Unity 的 Addressables ≈ 智能指针
// 理解 RAII 可以帮助理解 Unity 的资源管理

// 3. 异常安全
// Unity 的 C# 有异常处理
// 但 Unity 不建议在 Update 中使用异常
// 理解异常安全可以帮助编写健壮的 Unity 代码

// 4. 模板和泛型
// Unity 的 C# 泛型比 C++ 模板简单
// 但原理相似
// 理解 C++ 模板可以帮助理解 Unity 的泛型
```

### 代码对比

```cpp
// Effective C++ 原则：以对象管理资源
// C++ 版本
class Texture {
public:
    Texture(const char *path) {
        id = LoadTexture(path);
    }
    
    ~Texture() {
        UnloadTexture(id);
    }
    
private:
    unsigned int id;
};

// Unity C# 版本
public class Texture : MonoBehaviour {
    private Texture2D texture;
    
    void Awake() {
        texture = new Texture2D(width, height);
    }
    
    void OnDestroy() {
        Destroy(texture);
    }
}

// 💡 区别
// C++：手动管理，RAII 保证自动释放
// Unity：垃圾回收，手动调用 Destroy()
```

---

## 📝 学习建议

### 阅读顺序
1. 先读习惯 C++（条款 1-5）
2. 再读资源管理（条款 13-17）
3. 然后读设计与声明（条款 18-29）
4. 最后读继承和模板（条款 32-48）

### 实践方法
1. 每学一个条款，写一个示例
2. 对比 Unity 的实现方式
3. 思考为什么这个原则重要
4. 将原则应用到实际项目中

### 常见错误
1. 忘记虚析构函数
2. 不使用初始化列表
3. 返回局部变量的引用
4. 混淆拷贝和移动语义

### 推荐练习
1. 实现一个 RAII 资源管理类
2. 实现一个智能指针
3. 实现一个异常安全的类
4. 实现一个模板容器类

---

*本文件持续更新中...*
