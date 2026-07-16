# More Effective C++ 学习笔记

> **作者**：Scott Meyers
> **版本**：第1版（覆盖 C++03）
> **地位**：Effective C++ 的续作，35条进阶建议

---

## 📋 目录

1. [基础技巧](#基础技巧)
2. [运算符和类型转换](#运算符和类型转换)
3. [异常](#异常)
4. [效率](#效率)
5. [技巧](#技巧)
6. [难点解析](#难点解析)
7. [Unity 对照](#unity-对照)

---

## 基础技巧

### 条款 1：仔细区分 pointers 和 references

```cpp
// ✅ 指针 vs 引用
int x = 10;
int *p = &x;  // 指针：可以为 null
int &r = x;   // 引用：必须初始化，不能为 null

// ✅ 使用场景
// 指针：需要可选、可重新指向
// 引用：必须存在、更安全

// ✅ 传递参数
void Function(int *p);  // 指针：可以传递 null
void Function(int &r);  // 引用：不能传递 null

// 💡 原则
// 1. 当你知道需要指向某个对象时，使用引用
// 2. 当你需要可选或可重新指向时，使用指针
// 3. 当你需要动态分配时，使用指针
```

### 条款 2：最好使用 C++ 风格的类型转换

```cpp
// ❌ C 风格转型
int x = 3.14;
int y = (int)x;

// ✅ C++ 风格转型
int x = 3.14;
int y = static_cast<int>(x);

// ✅ C++ 风格转型类型
// 1. static_cast：编译时类型转换
// 2. dynamic_cast：运行时类型检查
// 3. const_cast：去除 const
// 4. reinterpret_cast：重新解释内存

// 💡 原则
// 1. 尽量避免转型
// 2. 如果必须使用，使用 C++ 风格
// 3. 不要使用 const_cast
```

### 条款 3：绝不要将多态基类的析构函数声明为 non-virtual

```cpp
// ❌ 非虚析构函数
class Base {
public:
    ~Base() {}  // 非虚析构函数
};

class Derived : public Base {
public:
    int *data;
    Derived() { data = new int[100]; }
    ~Derived() { delete[] data; }  // 不会调用！
};

Base *p = new Derived();
delete p;  // ❌ 内存泄漏！

// ✅ 虚析构函数
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

// 💡 原则
// 1. 有虚函数的类，析构函数必须是 virtual
// 2. 通过基类指针删除子类对象时
// 3. 作为基类的类
```

### 条款 4：避免使用默认构造函数以及构造函数与其参数列表形式的混淆

```cpp
// ❌ 默认构造函数的问题
class Widget {
public:
    Widget() {}  // 默认构造函数
};

Widget w;  // ✅ 可以
Widget w2();  // ❌ 这是函数声明！

// ✅ 使用初始化列表
class Widget {
public:
    Widget(int x, int y) : x(x), y(y) {}
    
private:
    int x, y;
};

Widget w(10, 20);  // ✅

// 💡 原则
// 1. 明确区分函数声明和对象定义
// 2. 使用初始化列表而不是赋值
// 3. 避免歧义
```

### 条款 5：熟悉标准库中 "与 C 兼容" 的部分

```cpp
// ✅ C 兼容部分
// 1. C 字符串函数：strcpy, strcmp, strlen 等
// 2. C 数组
// 3. C 文件 I/O：fopen, fclose 等

// ❌ 不推荐使用
// 1. C 字符串：容易出错
// 2. C 数组：没有边界检查
// 3. C 文件 I/O：不安全

// ✅ 推荐使用
// 1. std::string
// 2. std::vector
// 3. std::fstream

// 💡 原则
// 1. 优先使用 C++ 标准库
// 2. 只在必要时使用 C 兼容部分
// 3. 注意安全性
```

---

## 运算符和类型转换

### 条款 6：区分自增操作的前缀和后缀形式

```cpp
// ✅ 前缀自增
int x = 10;
++x;  // 先自增，后返回

// ✅ 后缀自增
int y = 10;
y++;  // 先返回，后自增

// ✅ 效率差异
// 前缀：更高效（不需要临时对象）
// 后缀：较低效（需要临时对象）

// ✅ 实现差异
class Counter {
public:
    // 前缀
    Counter& operator++() {
        ++value;
        return *this;
    }
    
    // 后缀
    Counter operator++(int) {
        Counter temp = *this;
        ++value;
        return temp;
    }
    
private:
    int value = 0;
};

// 💡 原则
// 1. 优先使用前缀自增
// 2. 后缀自增用于需要旧值时
// 3. 理解效率差异
```

### 条款 7：不要重载 &&, ||, 或 ,

```cpp
// ❌ 不要重载这些运算符
bool operator&&(const Widget &lhs, const Widget &rhs);
bool operator||(const Widget &lhs, const Widget &rhs);
int operator,(const Widget &lhs, const Widget &rhs);

// ✅ 原因
// 1. 短路求值会丢失
// 2. 求值顺序不确定
// 3. 违反直觉

// 💡 原则
// 1. 不要重载 &&, ||, ,
// 2. 这些运算符有特殊语义
// 3. 重载会导致意外行为
```

### 条款 8：了解各种不同意义的 new 和 delete

```cpp
// ✅ new 的不同形式
int *p1 = new int;           // 分配单个对象
int *p2 = new int[100];      // 分配数组
int *p3 = new (std::nothrow) int;  // 不抛出异常

// ✅ delete 的不同形式
delete p1;      // 释放单个对象
delete[] p2;    // 释放数组

// ✅ placement new
void *buffer = operator new(sizeof(int));
int *p = new (buffer) int(42);  // 在 buffer 中构造

// ✅ 自定义 new
class Widget {
public:
    static void *operator new(size_t size);
    static void operator delete(void *p) noexcept;
};

// 💡 原则
// 1. new 和 delete 必须匹配使用
// 2. placement new 需要手动调用析构函数
// 3. 自定义 new/delete 需要谨慎
```

### 条款 9：利用 destructors 避免内存泄漏

```cpp
// ❌ 内存泄漏
void Process() {
    int *p = new int[100];
    // ... 可能抛出异常
    delete[] p;  // 可能不会执行
}

// ✅ 使用 RAII
void Process() {
    std::unique_ptr<int[]> p(new int[100]);
    // ... 即使抛出异常，也会自动删除
}

// ✅ 使用容器
void Process() {
    std::vector<int> v(100);
    // ... 自动管理内存
}

// 💡 原则
// 1. 使用 RAII 管理资源
// 2. 使用智能指针
// 3. 使用容器
```

### 条款 10：避免 destructors 发出消息

```cpp
// ❌ 不好的做法
class Widget {
public:
    ~Widget() {
        std::cout << "Widget destroyed" << std::endl;
    }
};

// ✅ 好的做法
class Widget {
public:
    ~Widget() {
        // 安静地释放资源
    }
};

// 💡 原则
// 1. 析构函数不应该有副作用
// 2. 析构函数不应该抛出异常
// 3. 析构函数应该安静地释放资源
```

### 条款 11：禁止异常逃离 destructors

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
            // 吞掉异常
        }
    }
};

// 💡 原则
// 1. 析构函数不应该抛出异常
// 2. 如果必须抛出，在析构函数前提供关闭接口
// 3. 使用 try-catch 吞掉异常
```

### 条款 12：了解 "抛出一个异常" 与 "传递一个参数" 或 "调用一个虚函数" 之间的差异

```cpp
// ✅ 异常传递
void Function() {
    throw std::runtime_error("Error");
}

// ✅ 参数传递
void Function(int value) {
    // 值传递：拷贝
}

// ✅ 虚函数调用
class Base {
public:
    virtual void Function() {}
};

class Derived : public Base {
public:
    void Function() override {}
};

Base *p = new Derived();
p->Function();  // 调用 Derived::Function

// 💡 差异
// 1. 异常：使用拷贝构造函数
// 2. 参数：使用拷贝构造函数
// 3. 虚函数：使用动态绑定
```

### 条款 13：通过引用捕获异常

```cpp
// ❌ 值捕获
try {
    throw std::runtime_error("Error");
} catch (std::exception e) {  // 拷贝
    std::cout << e.what() << std::endl;
}

// ✅ 引用捕获
try {
    throw std::runtime_error("Error");
} catch (const std::exception &e) {  // 引用
    std::cout << e.what() << std::endl;
}

// 💡 原则
// 1. 使用 const 引用捕获异常
// 2. 避免不必要的拷贝
// 3. 保持异常的完整性
```

### 条款 14：通过使用异常规格来声明函数可能抛出的异常

```cpp
// ✅ 旧式异常规格（C++03）
void Function() throw(std::runtime_error) {
    throw std::runtime_error("Error");
}

// ✅ C++11：noexcept
void Function() noexcept {
    // 不会抛出异常
}

// ✅ 动态异常规格
void Function() throw() {
    // 不会抛出异常
}

// 💡 原则
// 1. 使用 noexcept 表示不会抛出异常
// 2. 异常规格在 C++11 中已废弃
// 3. 使用 noexcept 提高性能
```

### 条款 15：谨慎使用异常安全

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
    
private:
    int *data;
};

// 💡 原则
// 1. 优先实现强保证
// 2. 使用 copy-and-swap 技术
// 3. 使用 RAII 管理资源
```

### 条款 16：理解 "成员函数指针" 语法的含义

```cpp
// ✅ 成员函数指针
class Widget {
public:
    void Function() {}
};

void (Widget::*pmf)() = &Widget::Function;

Widget w;
(w.*pmf)();  // 调用成员函数

// ✅ 使用 std::function
std::function<void()> f = std::bind(&Widget::Function, &w);
f();

// ✅ 使用 Lambda
auto f2 = [&w]() { w.Function(); };
f2();

// 💡 原则
// 1. 成员函数指针语法复杂
// 2. 优先使用 std::function
// 3. 优先使用 Lambda
```

---

## 异常

### 条款 17：明智地使用 exception specifications

```cpp
// ❌ 旧式异常规格（C++03）
void Function() throw(std::runtime_error) {
    throw std::runtime_error("Error");
}

// ✅ C++11：noexcept
void Function() noexcept {
    // 不会抛出异常
}

// 💡 原则
// 1. 使用 noexcept 表示不会抛出异常
// 2. 异常规格在 C++11 中已废弃
// 3. 使用 noexcept 提高性能
```

### 条款 18：避免异常操作

```cpp
// ❌ 异常操作
try {
    // 可能抛出异常的代码
} catch (...) {
    // 吞掉所有异常
}

// ✅ 正确做法
try {
    // 可能抛出异常的代码
} catch (const std::exception &e) {
    // 处理特定异常
    std::cerr << e.what() << std::endl;
}

// 💡 原则
// 1. 不要吞掉所有异常
// 2. 处理特定异常
// 3. 重新抛出异常
```

### 条款 19：学习异常安全的技术

```cpp
// ✅ 异常安全技术
// 1. RAII：资源获取即初始化
// 2. copy-and-swap：创建临时对象，交换内容
// 3. 智能指针：自动管理内存

// ✅ RAII 示例
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

// ✅ copy-and-swap 示例
class Widget {
public:
    Widget& operator=(const Widget &other) {
        Widget temp(other);
        Swap(temp);
        return *this;
    }
    
    void Swap(Widget &other) noexcept {
        std::swap(data, other.data);
    }
};

// 💡 原则
// 1. 使用 RAII 管理资源
// 2. 使用 copy-and-swap 实现强保证
// 3. 使用智能指针避免内存泄漏
```

---

## 效率

### 条款 20：协助编译器完成返回值优化（RVO）

```cpp
// ✅ RVO（返回值优化）
Widget Function() {
    return Widget();  // 编译器可以优化掉拷贝
}

// ✅ NRVO（命名返回值优化）
Widget Function() {
    Widget w;
    return w;  // 编译器可以优化掉拷贝
}

// ❌ 阻止 RVO
Widget Function() {
    Widget w;
    return std::move(w);  // ❌ 阻止 RVO
}

// 💡 原则
// 1. 返回局部对象时，不要使用 std::move
// 2. 让编译器优化
// 3. 信任编译器的 RVO
```

### 条款 21：利用重载来避免隐式类型转换

```cpp
// ❌ 隐式类型转换
class Widget {
public:
    Widget(int x) : x(x) {}
    
private:
    int x;
};

Widget w = 10;  // ✅ 隐式转换

// ✅ 使用 explicit
class Widget {
public:
    explicit Widget(int x) : x(x) {}
    
private:
    int x;
};

Widget w = 10;  // ❌ 编译错误
Widget w(10);   // ✅ 显式构造

// 💡 原则
// 1. 使用 explicit 避免隐式转换
// 2. 提供重载版本
// 3. 保持接口清晰
```

### 条款 22：考虑用操作符的赋值形式（+=, -= 等）代替其独身形式（+, - 等）

```cpp
// ✅ 独身形式
Vector2D a(1, 2);
Vector2D b(3, 4);
Vector2D c = a + b;  // 创建临时对象

// ✅ 赋值形式
Vector2D a(1, 2);
Vector2D b(3, 4);
a += b;  // 直接修改 a

// 💡 原则
// 1. 赋值形式更高效
// 2. 独身形式创建临时对象
// 3. 优先使用赋值形式
```

### 条款 23：考虑使用其他替代品来代替标准库中的容器

```cpp
// ✅ 标准库容器
std::vector<int> v = {1, 2, 3, 4, 5};
std::list<int> l = {1, 2, 3, 4, 5};
std::map<int, int> m = {{1, 2}, {3, 4}};

// ✅ 替代品
// 1. std::array：固定大小数组
// 2. std::forward_list：单向链表
// 3. std::unordered_map：无序映射

// 💡 原则
// 1. 根据需求选择容器
// 2. 考虑性能特性
// 3. 不要过度使用标准库容器
```

### 条款 24：理解函数对象、Lambda 表达式、bind 函数和 std::function 的关系

```cpp
// ✅ 函数对象
struct Adder {
    int operator()(int a, int b) {
        return a + b;
    }
};

Adder add;
int result = add(1, 2);

// ✅ Lambda
auto add2 = [](int a, int b) { return a + b; };
int result2 = add2(1, 2);

// ✅ std::bind
auto add3 = std::bind(Adder{}, std::placeholders::_1, std::placeholders::_2);
int result3 = add3(1, 2);

// ✅ std::function
std::function<int(int, int)> add4 = [](int a, int b) { return a + b; };
int result4 = add4(1, 2);

// 💡 关系
// 1. 函数对象：重载 operator()
// 2. Lambda：匿名函数
// 3. std::bind：绑定参数
// 4. std::function：通用函数包装器
```

### 条款 25：使用对时性评估来优化

```cpp
// ✅ 懒惰求值
class Matrix {
public:
    // 重载 + 运算符
    Matrix operator+(const Matrix &other) const {
        return Matrix(*this, other, Op::Add);
    }
    
    // 立即求值
    Matrix Evaluate() const {
        // 实际计算
    }
};

// ✅ 使用
Matrix a, b, c;
Matrix d = a + b + c;  // 三个加法

Matrix result = (a + b + c).Evaluate();  // 一次性计算

// 💡 原则
// 1. 延迟计算，直到需要结果
// 2. 减少不必要的临时对象
// 3. 优化复合表达式
```

---

## 技巧

### 条款 26：尽可能地将 C++ 的构造函数、赋值操作符和自动类型转换函数限制为不能被隐式调用

```cpp
// ✅ 使用 explicit
class Widget {
public:
    explicit Widget(int x) : x(x) {}
    
private:
    int x;
};

Widget w = 10;  // ❌ 编译错误
Widget w(10);   // ✅ 显式构造

// ✅ 禁止隐式转换
class Widget {
public:
    explicit Widget(int x) : x(x) {}
    explicit operator int() const { return x; }
    
private:
    int x;
};

int x = Widget(10);  // ❌ 编译错误
int x = static_cast<int>(Widget(10));  // ✅ 显式转换

// 💡 原则
// 1. 使用 explicit 避免隐式转换
// 2. 禁止不期望的类型转换
// 3. 保持接口清晰
```

### 条款 27：符合或重写继承而来的非虚函数

```cpp
// ✅ 非虚函数
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

// ✅ 使用 override
class Derived : public Base {
public:
    void Function() override {  // ❌ 编译错误，非虚函数不能 override
        std::cout << "Derived" << std::endl;
    }
};

// 💡 原则
// 1. 非虚函数是静态绑定的
// 2. 不要重新定义继承而来的非虚函数
// 3. 使用 override 标记虚函数
```

### 条款 28：通过嵌套或分离（而非继承）和域（namespace）来增加封装

```cpp
// ❌ 使用继承增加封装
class Base {
protected:
    int data;
};

class Derived : public Base {
public:
    void Function() {
        data = 10;  // 可以访问 protected 成员
    }
};

// ✅ 使用嵌套增加封装
class Widget {
private:
    class Helper {
    public:
        void Function() {}
    };
    
    Helper helper;
};

// ✅ 使用域增加封装
namespace Details {
    void InternalFunction() {}
}

// 💡 原则
// 1. 优先使用组合而不是继承
// 2. 使用嵌套类隐藏实现细节
// 3. 使用域避免名称冲突
```

### 条款 29：努力使得基类的行为尽可能简单

```cpp
// ❌ 复杂的基类
class Base {
public:
    virtual void Function1() = 0;
    virtual void Function2() = 0;
    virtual void Function3() = 0;
    virtual void Function4() = 0;
    virtual void Function5() = 0;
};

// ✅ 简单的基类
class Base {
public:
    virtual void Function() = 0;
};

// 💡 原则
// 1. 基类应该尽量简单
// 2. 避免在基类中添加太多功能
// 3. 使用组合而不是继承
```

### 条款 30：一切皆在别名声明中

```cpp
// ✅ 旧式 typedef
typedef std::vector<int> IntVector;

// ✅ C++11 别名声明
using IntVector = std::vector<int>;

// ✅ 模板别名
template <typename T>
using Vector = std::vector<T>;

Vector<int> v;  // std::vector<int>

// 💡 别名声明的优点
// 1. 更清晰
// 2. 支持模板
// 3. 更易于使用
```

---

## 难点解析

### 🔴 难点 1：指针 vs 引用的选择

```cpp
// ✅ 选择原则
// 1. 当你需要指向某个对象时，使用引用
// 2. 当你需要可选或可重新指向时，使用指针
// 3. 当你需要动态分配时，使用指针

// ✅ 使用场景
// 引用：函数参数、返回值、成员变量
// 指针：动态分配、可选参数、数据结构

// ❌ 常见错误
// 1. 返回局部变量的引用
// 2. 使用悬空指针
// 3. 混淆指针和引用的语义
```

### 🔴 难点 2：异常安全的实现

```cpp
// ✅ 异常安全保证
// 1. 基本保证：操作失败时，对象处于有效状态
// 2. 强保证：操作失败时，对象保持原状态
// 3. 不抛保证：操作不会抛出异常

// ✅ 实现技术
// 1. RAII：资源获取即初始化
// 2. copy-and-swap：创建临时对象，交换内容
// 3. 智能指针：自动管理内存

// ❌ 常见错误
// 1. 在析构函数中抛出异常
// 2. 忘记释放资源
// 3. 混淆拷贝和移动语义
```

### 🔴 难点 3：效率优化

```cpp
// ✅ 效率优化技术
// 1. 使用引用避免拷贝
// 2. 使用移动语义转移资源
// 3. 使用 RVO 优化返回值
// 4. 使用 inline 减少函数调用开销

// ✅ 使用场景
// 1. 大对象：使用引用或指针
// 2. 临时对象：使用移动语义
// 3. 返回局部对象：使用 RVO
// 4. 小函数：使用 inline

// ❌ 不要过度优化
// 1. 先保证正确性
// 2. 使用性能分析工具
// 3. 只优化热点代码
```

---

## Unity 对照

### 概念映射

| More Effective C++ 原则 | Unity 对应 | 说明 |
|------------------------|------------|------|
| 条款 3：虚析构函数 | Unity MonoBehaviour | Unity 有 GC |
| 条款 9：RAII | Unity 组件系统 | 资源管理 |
| 条款 15：异常安全 | Unity 错误处理 | 健壮性 |
| 条款 20：RVO | Unity 优化 | 性能优化 |
| 条款 26：explicit | Unity 类型安全 | 避免隐式转换 |

### Unity 中的应用

```cpp
// 1. 异常安全
// Unity 的 C# 有异常处理
// 但 Unity 不建议在 Update 中使用异常
// 理解异常安全可以帮助编写健壮的 Unity 代码

// 2. 效率优化
// 理解 More Effective C++ 可以帮助优化 Unity 性能
// 例如：使用引用避免拷贝，使用移动语义转移资源

// 3. 类型安全
// 理解 explicit 可以帮助避免 Unity 中的隐式转换
// 例如：使用 explicit 构造函数

// 💡 学习建议
// 1. 先理解 More Effective C++ 原则
// 2. 对比 Unity 的实现方式
// 3. 思考为什么这些原则重要
// 4. 将原则应用到 Unity 开发中
```

### 代码对比

```cpp
// More Effective C++：RAII
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
1. 先读基础技巧（条款 1-5）
2. 再读运算符和类型转换（条款 6-16）
3. 然后读异常（条款 17-19）
4. 最后读效率和技巧（条款 20-30）

### 实践方法
1. 每学一个条款，写一个示例
2. 对比 Unity 的实现方式
3. 思考为什么这些原则重要
4. 将原则应用到项目中

### 常见错误
1. 混淆指针和引用
2. 不理解异常安全
3. 过度优化
4. 忘记虚析构函数

### 推荐练习
1. 实现一个 RAII 资源管理类
2. 实现一个异常安全的类
3. 实现一个高效的容器
4. 实现一个智能指针

---

*本文件持续更新中...*
