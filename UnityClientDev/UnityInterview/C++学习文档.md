# C++ 学习文档

从基础到进阶，从理论到实践 —— 一份详尽且系统的 C++ 学习指南。

---

## 目录

- [一、基础篇](#一基础篇)
  - [1.1 C++ 简介与环境搭建](#11-c-简介与环境搭建)
  - [1.2 基本语法](#12-基本语法)
  - [1.3 流程控制](#13-流程控制)
  - [1.4 函数](#14-函数)
  - [1.5 数组与字符串](#15-数组与字符串)
  - [1.6 指针与引用](#16-指针与引用)
  - [1.7 结构体、联合体与枚举](#17-结构体联合体与枚举)
- [二、进阶篇](#二进阶篇)
  - [2.1 面向对象编程](#21-面向对象编程)
  - [2.2 运算符重载](#22-运算符重载)
  - [2.3 模板](#23-模板)
  - [2.4 STL 标准模板库](#24-stl-标准模板库)
  - [2.5 异常处理](#25-异常处理)
  - [2.6 文件 I/O](#26-文件-io)
- [三、高级篇](#三高级篇)
  - [3.1 智能指针与内存管理](#31-智能指针与内存管理)
  - [3.2 移动语义与右值引用](#32-移动语义与右值引用)
  - [3.3 Lambda 表达式](#33-lambda-表达式)
  - [3.4 并发编程与多线程](#34-并发编程与多线程)
  - [3.5 C++11/14/17/20 新特性](#35-c11141720-新特性)
  - [3.6 设计模式](#36-设计模式)
- [四、深入学习方向](#四深入学习方向)

---

## 一、基础篇

### 1.1 C++ 简介与环境搭建

C++ 由 Bjarne Stroustrup 于 1979 年在贝尔实验室开发，最初名为 **"C with Classes"**，是 C 语言的超集。它在保留 C 语言高效性的同时引入了面向对象、泛型和函数式编程。

**核心特性：**
- **零开销抽象**：不使用的特性不产生性能代价
- **多范式**：过程式、OOP、泛型、函数式
- **手动内存管理**：精细化控制资源
- **硬件接近性**：适合系统编程、游戏引擎、嵌入式

**编译流程：**
```
源代码 (.cpp) -> 预处理 -> 编译 -> 汇编 -> 链接 -> 可执行文件
```

**工具链：**

| 工具 | 用途 |
|------|------|
| GCC/G++ | Linux 标准编译器 |
| Clang | 错误信息最友好 |
| MSVC | Visual Studio 编译器 |
| CMake | 跨平台构建系统（行业标准） |
| vcpkg | 微软 C++ 包管理器 |

**第一个程序：**

```cpp
// 每个 C++ 程序必须有 main 函数——程序的入口点
// #include 是预处理指令，用于包含头文件
#include <iostream>  // 标准输入输出流库（cin/cout）

// 使用 std 命名空间（头文件中切勿使用，会污染全局命名空间）
using namespace std;

// main 函数：返回值 int 表示退出码，0=成功，非0=出错
int main() {
    // << 是流插入运算符，将右侧内容输出到控制台
    // endl 换行并刷新缓冲区
    cout << "Hello, World!" << endl;

    return 0;  // 返回 0 表示程序正常结束
}
```

**编译：**
```bash
g++ -std=c++17 -Wall -Wextra -O2 hello.cpp -o hello && ./hello
```

---

### 1.2 基本语法

#### 变量与数据类型

```cpp
#include <iostream>
#include <climits>
#include <cstdint>
using namespace std;

int main() {
    // 基本数据类型
    int a = 42;                    // 基本整型，通常 4 字节（32位）
    short b = 10;                  // 短整型，至少 2 字节
    long c = 100000L;              // 长整型
    long long d = 10000000000LL;   // C++11，至少 8 字节（64位）

    unsigned int u = 100;          // 无符号：只能表示非负数

    // 固定宽度整型（C++11）
    int32_t  i32 = 0;              // 精确 32 位有符号
    uint64_t u64 = 0;              // 精确 64 位无符号

    // 字符类型
    char c = 'A';                  // 单字节 ASCII，1 字节
    wchar_t wc = L'我';            // 宽字符
    char16_t c16 = u'中';          // C++11 UTF-16

    // 浮点型
    float  f = 3.14f;              // 单精度，4 字节
    double d = 3.1415926535;       // 双精度，8 字节（默认）
    long double ld = 3.14L;        // 扩展精度

    // 布尔型
    bool flag = true;              // 可转为 int：true=1，false=0

    // auto 类型推导（C++11）
    auto x = 42;                   // 推导为 int
    auto y = 3.14;                 // 推导为 double

    // 常量
    const int DAYS = 7;            // const：运行时常量
    constexpr int SEC_PER_MIN = 60; // C++11 编译期常量

    // sizeof 运算符
    cout << "int: " << sizeof(int) << " 字节" << endl;

    return 0;
}
```

#### 运算符详解

```cpp
#include <iostream>
using namespace std;

int main() {
    // 算术运算符
    int sum  = 10 + 3;      // 13
    int quot = 10 / 3;      // 3（整数除法截断小数！）
    double dq = 10.0 / 3.0; // 3.33333（浮点除法）
    int rem  = 10 % 3;      // 1（取模）

    // 自增/自减
    int i = 5;
    int pre  = ++i;   // 前缀：先自增再使用，i=6, pre=6
    int post = i++;   // 后缀：先使用再自增，i=7, post=6
    // 前缀比后缀高效（后缀需保存旧值拷贝）

    // 关系运算符（返回 bool）
    bool eq = (a == b);    // 等于
    bool lt = (a < b);     // 小于

    // 逻辑运算符（短路求值）
    bool and_ = p && q;    // 一假即假
    bool or_  = p || q;    // 一真即真
    bool not_ = !p;        // 取反

    // 位运算符
    unsigned int ba = 0b1100 & 0b1010;   // 按位与：0b1000
    unsigned int bo = 0b1100 | 0b1010;   // 按位或：0b1110
    unsigned int bx = 0b1100 ^ 0b1010;   // 按位异或：0b0110
    unsigned int ls = 0b1100 << 2;       // 左移：0b110000
    unsigned int rs = 0b1100 >> 1;       // 右移：0b0110

    // 三元运算符
    string result = (score >= 60) ? "passed" : "failed";

    return 0;
}
```

---

### 1.3 流程控制

```cpp
#include <iostream>
#include <vector>
using namespace std;

int main() {
    // 1. if-else
    int score = 85;
    if (score >= 90) {
        cout << "优秀" << endl;
    } else if (score >= 80) {
        cout << "良好" << endl;
    } else {
        cout << "不及格" << endl;
    }

    // 2. switch（必须加 break，否则穿透）
    char grade = 'B';
    switch (grade) {
        case 'A': cout << "90-100" << endl; break;
        case 'B': cout << "80-89"  << endl; break;
        case 'F': cout << "不及格" << endl; break;
        default:  cout << "无效"    << endl; break;
    }

    // 3. while（先判断后执行）
    int count = 0;
    while (count < 3) cout << "while: " << count++ << endl;

    // 4. do-while（至少执行一次）
    int num = 0;
    do { cout << "do: " << num++ << endl; } while (num < 3);

    // 5. for 循环
    for (int i = 0; i < 5; ++i) cout << "for: " << i << endl;

    // 6. 基于范围的 for（C++11）
    vector<int> nums = {10, 20, 30};
    for (int n : nums) cout << n << " ";         // 值拷贝
    for (int& n : nums) n *= 2;                   // 引用可修改
    for (const int& n : nums) cout << n;          // const 引用只读
    cout << endl;

    // 7. break 与 continue
    for (int i = 0; i < 10; ++i) {
        if (i == 3) continue;   // 跳过 i=3
        if (i == 7) break;      // i=7 终止
        cout << i << " ";       // 0 1 2 4 5 6
    }
    cout << endl;

    // 8. if 初始化语句（C++17）
    if (int x = rand() % 100; x > 50) {
        cout << "大于50: " << x << endl;
    }

    return 0;
}
```

---

### 1.4 函数

```cpp
#include <iostream>
#include <string>
#include <vector>
using namespace std;

// ========== 函数声明（函数原型）==========
int add(int a, int b);                     // 声明，告诉编译器函数的存在
void swapVal(int& a, int& b);              // 引用参数
void printAll(const vector<int>& vec);     // const 引用避免拷贝

// ========== 函数定义 ==========
// 语法：返回值类型 函数名(参数列表) { 函数体 }
int add(int a, int b) {
    return a + b;
}

// ========== 传值 vs 传引用 vs 传指针 ==========
void byValue(int x) { x = 100; }            // 传值：不影响外部
void byRef(int& x) { x = 100; }             // 传引用：影响外部，无拷贝
void byPtr(int* p) { if (p) *p = 100; }     // 传指针：影响外部，需检查空指针

// ========== 默认参数 ==========
// 默认参数必须从右向左连续提供
void greet(const string& name, const string& greeting = "Hello") {
    cout << greeting << ", " << name << "!" << endl;
}

// ========== 函数重载 ==========
// 同名函数，参数列表不同（个数、类型、顺序）
// 返回值不能作为重载区分依据
void print(int x)       { cout << "int: " << x << endl; }
void print(double x)    { cout << "double: " << x << endl; }
void print(const string& s) { cout << "string: " << s << endl; }

// ========== 内联函数 ==========
// 建议编译器将函数体展开到调用点，避免函数调用开销
// 适用于短小频繁调用的函数
inline int square(int x) { return x * x; }

// ========== 递归函数 ==========
int factorial(int n) {
    if (n <= 1) return 1;           // 基线条件（终止条件）
    return n * factorial(n - 1);    // 递归步骤
}

// 斐波那契数列（高效迭代版）
int fibonacci(int n) {
    if (n <= 1) return n;
    int prev = 0, curr = 1;
    for (int i = 2; i <= n; ++i) {
        int next = prev + curr;
        prev = curr;
        curr = next;
    }
    return curr;
}

// ========== 函数指针 ==========
int multiply(int a, int b) { return a * b; }
using MathOp = int(*)(int, int);  // C++11 类型别名

int main(int argc, char* argv[]) {
    // argc: 参数个数，argv: 参数数组，argv[0] 是程序名
    cout << "程序名: " << argv[0] << endl;

    int a = 5, b = 3;
    cout << "add(5, 3) = " << add(a, b) << endl;       // 8

    int x = 10;
    byValue(x);  cout << x << endl;    // 10（不变）
    byRef(x);    cout << x << endl;    // 100（变了）
    byPtr(&x);   cout << x << endl;    // 100（变了）

    greet("Alice");                     // Hello, Alice!
    greet("Bob", "Hi");                 // Hi, Bob!

    print(42);       // int: 42
    print(3.14);     // double: 3.14
    print("hello");  // string: hello

    cout << "5! = " << factorial(5) << endl;           // 120
    cout << "fib(10) = " << fibonacci(10) << endl;     // 55

    // 函数指针
    MathOp op = multiply;
    cout << "op(3,4) = " << op(3, 4) << endl;          // 12

    return 0;
}
```

---

### 1.5 数组与字符串

```cpp
#include <iostream>
#include <string>
#include <vector>
#include <array>       // C++11 std::array
#include <algorithm>   // sort, find
#include <sstream>     // 字符串流
#include <cstring>     // C 风格字符串函数
using namespace std;

int main() {
    // =================================================
    // 1. C 风格数组（栈上分配，大小编译期确定）
    // =================================================
    int arr1[5];                      // 未初始化，值不确定
    int arr2[5] = {1, 2, 3, 4, 5};   // 完全初始化
    int arr3[]  = {1, 2, 3};          // 自动推导大小 = 3
    int arr4[5] = {1, 2};             // 部分初始化，其余为 0

    arr2[0] = 99;                     // 通过下标访问和修改

    // 数组退化：数组名退化为指向首元素的指针
    int* ptr = arr2;                  // arr2 隐式转为 int*
    size_t size = sizeof(arr2) / sizeof(arr2[0]);  // 计算元素个数

    for (int x : arr2) cout << x << " "; cout << endl;  // 范围 for

    // =================================================
    // 2. std::array（C++11 固定大小容器）
    // =================================================
    array<int, 5> stdArr = {10, 20, 30, 40, 50};
    cout << "size: " << stdArr.size() << endl;       // 5
    cout << "front: " << stdArr.front() << endl;      // 10
    cout << "at(2): " << stdArr.at(2) << endl;        // 30（带边界检查）
    // stdArr.at(10);  // 抛出 out_of_range 异常
    sort(stdArr.begin(), stdArr.end());

    // =================================================
    // 3. std::vector（动态数组，最常用容器）
    // =================================================
    vector<int> vec1;                        // 空
    vector<int> vec2(5, 10);                 // 5 个元素，值 10
    vector<int> vec3 = {1, 2, 3, 4, 5};     // 初始化列表（C++11）

    // 增删改查
    vec1.push_back(100);  // 末尾添加 O(1)
    vec1.push_back(200);
    vec1.pop_back();      // 删除末尾 O(1)
    cout << "size: " << vec1.size() << ", capacity: " << vec1.capacity() << endl;

    // 预分配（提高 push_back 性能）
    vector<int> bigVec;
    bigVec.reserve(1000);  // 预分配 1000 个元素空间

    // 遍历方式
    for (size_t i = 0; i < vec1.size(); ++i) cout << vec1[i] << " ";
    for (int x : vec1) cout << x << " ";
    for (auto it = vec1.begin(); it != vec1.end(); ++it) cout << *it << " ";

    // =================================================
    // 4. C 风格字符串（以 '\0' 结尾）
    // =================================================
    char cstr1[] = "hello";          // 实际占 6 字节：h e l l o '\0'
    const char* cstr2 = "world";

    cout << "strlen: " << strlen(cstr1) << endl;  // 5（不含 '\0'）
    cout << "strcmp: " << strcmp(cstr1, "hello") << endl;  // 0（相等）

    // =================================================
    // 5. std::string（推荐）
    // =================================================
    string s1 = "hello";
    string s2("world");
    string s4 = s1 + " " + s2;    // 拼接："hello world"

    cout << "length: " << s4.length() << endl;    // 11
    cout << "empty: " << s4.empty() << endl;      // false
    cout << "at(0): " << s4.at(0) << endl;        // 'h'（带边界检查）

    // 查找
    size_t pos = s4.find("world");
    if (pos != string::npos) cout << "找到位置: " << pos << endl;

    string sub = s4.substr(0, 5);       // "hello"
    s4 += "!";                          // 追加
    s4.insert(5, ",");                  // 插入
    s4.replace(7, 5, "C++");           // 替换
    s4.erase(5, 2);                    // 删除
    s4.clear();                        // 清空

    // 字符串 <-> 数字转换
    int num = stoi("123");              // string to int（C++11）
    double d = stod("3.14");            // string to double
    string str = to_string(456);        // int to string

    // 字符串流（灵活的格式化）
    ostringstream oss;
    oss << "Value: " << 42 << ", Pi: " << 3.14159;
    string fmt = oss.str();

    istringstream iss("100 200 300");
    int a, b, c;
    iss >> a >> b >> c;  // 从字符串解析三个整数

    return 0;
}
```

---

### 1.6 指针与引用

指针和引用是 C++ 最核心的概念。理解它们的内存语义是掌握 C++ 的关键。

```cpp
#include <iostream>
using namespace std;

int main() {
    // =================================================
    // 1. 指针基础
    // =================================================
    int value = 42;
    int* ptr = &value;            // ptr 存储 value 的地址

    cout << "value: " << value << endl;         // 42
    cout << "地址: " << ptr << endl;            // 0x...
    cout << "解引用: " << *ptr << endl;         // 42

    *ptr = 100;                   // 通过指针修改 value
    cout << "修改后: " << value << endl;        // 100

    // =================================================
    // 2. 空指针与悬空指针
    // =================================================
    int* nullPtr = nullptr;       // C++11 推荐的空指针字面量
    // *nullPtr = 42;             // 危险！解引用空指针是未定义行为

    // 悬空指针：指向已释放内存
    int* dangling;
    {
        int temp = 10;
        dangling = &temp;
    }  // temp 已销毁，dangling 成为悬空指针
    // *dangling;                 // 危险！

    // =================================================
    // 3. const 与指针（三种组合）
    // =================================================
    int x = 10, y = 20;

    // 指向常量的指针：不能通过指针修改值，但可以改变指向
    const int* p1 = &x;
    // *p1 = 100;    // 错误
    p1 = &y;          // 可以

    // 常量指针：不能改变指向，但可以修改值
    int* const p2 = &x;
    *p2 = 100;        // 可以
    // p2 = &y;      // 错误

    // 指向常量的常量指针：都不能修改
    const int* const p3 = &x;
    // *p3 = 100;    // 错误
    // p3 = &y;      // 错误

    // 记忆技巧：const 修饰它右边的部分
    // const int*   -> const 修饰 int（指向的对象是常量）
    // int* const   -> const 修饰 *（指针本身是常量）

    // =================================================
    // 4. 指针算术与数组
    // =================================================
    int arr[] = {10, 20, 30, 40, 50};
    int* ap = arr;  // 等价于 &arr[0]

    cout << *ap << endl;         // 10
    cout << *(ap + 1) << endl;   // 20（指针移位：+1 偏移 sizeof(int) 字节）
    cout << 2[arr] << endl;      // 30（arr[2] 等价于 *(arr+2) 等价于 *(2+arr) 等价于 2[arr]）

    for (int* p = arr; p < arr + 5; ++p) {
        cout << *p << " ";       // 用指针遍历数组
    }
    cout << endl;

    // =================================================
    // 5. 引用（变量的别名）
    // =================================================
    int original = 10;
    int& ref = original;         // ref 是 original 的引用，必须初始化且不能改变绑定

    cout << ref << endl;         // 10
    ref = 20;                    // 修改 original
    cout << original << endl;    // 20

    // 引用 vs 指针核心区别：
    // 1. 引用不能为 nullptr，指针可以
    // 2. 引用不能改变绑定，指针可以改变指向
    // 3. 引用语法更自然（像普通变量），不需要解引用

    // 应用场景：
    // - 函数参数（大对象用 const 引用避免拷贝）
    // - 返回值（避免拷贝，可以赋值修改）
    // - 范围 for 循环（for (auto& elem : container)）

    return 0;
}
```

---

### 1.7 结构体、联合体与枚举

```cpp
#include <iostream>
#include <string>
#include <vector>
using namespace std;

// =================================================
// 1. 结构体（struct）
// =================================================
// struct 成员默认 public（class 默认 private）
// C++ 的 struct 可以有成员函数、构造函数、继承

struct Point {
    double x, y;

    // 构造函数初始化列表
    Point() : x(0), y(0) {}
    Point(double x, double y) : x(x), y(y) {}

    // const 成员函数：承诺不修改成员变量
    double distance() const {
        return sqrt(x * x + y * y);
    }
};

struct Student {
    string name;
    int age;
    double scores[3];

    double average() const {
        double sum = 0;
        for (double s : scores) sum += s;
        return sum / 3;
    }
};

// =================================================
// 2. 联合体（union）
// =================================================
// 所有成员共享同一块内存，一次只能使用一个成员
// 大小 = 最大成员的大小
union Data {
    int i;
    float f;
    char str[4];
};

// 带标签的联合体（更安全的使用方式）
enum class DataType { INTEGER, FLOAT };
struct SafeData {
    DataType type;
    union { int i; float f; };

    void setInt(int val) {
        type = DataType::INTEGER;
        i = val;
    }
    int getInt() const {
        if (type != DataType::INTEGER)
            throw runtime_error("类型不匹配");
        return i;
    }
};

// =================================================
// 3. 枚举
// =================================================
// 传统枚举（值在外部作用域可见）
enum Color { RED, GREEN, BLUE };  // RED=0, GREEN=1, BLUE=2
enum Status { OK = 200, NOT_FOUND = 404, ERROR = 500 };

// C++11 强类型枚举（作用域限定，不会隐式转整数）
enum class Weekday {
    MONDAY = 1, TUESDAY, WEDNESDAY, THURSDAY, FRIDAY, SATURDAY, SUNDAY
};

enum class Month : char {  // 可指定底层类型
    JAN = 1, FEB, MAR, APR, MAY, JUN,
    JUL, AUG, SEP, OCT, NOV, DEC
};

int main() {
    // 结构体使用
    Point p1;            // 默认构造：(0, 0)
    Point p2(3, 4);      // (3, 4)
    cout << "距离: " << p2.distance() << endl;  // 5

    // 强类型枚举
    Weekday today = Weekday::MONDAY;
    // int d = today;                    // 错误！
    int d = static_cast<int>(today);      // 必须显式转换：1

    // 联合体
    Data data;
    data.i = 42;
    cout << data.i << endl;   // 42
    // data.f 与 data.i 共享内存
    // 注意：写入一个成员后读取另一个成员是未定义行为

    return 0;
}
```

---

## 二、进阶篇

### 2.1 面向对象编程

C++ 面向对象的三大特性：封装（Encapsulation）、继承（Inheritance）、多态（Polymorphism）。

```cpp
#include <iostream>
#include <string>
#include <vector>
#include <memory>
using namespace std;

// =================================================
// 1. 类与封装
// =================================================
class BankAccount {
private:  // 私有：外部不可直接访问
    string ownerName;
    string accountNumber;
    double balance;

    void logTransaction(const string& type, double amount) const {
        cout << "[" << type << "] " << ownerName
             << ": " << amount << " 元" << endl;
    }

protected:  // 受保护：子类可访问
    string bankCode;

public:   // 公有：外部接口
    // ---- 构造函数 ----
    BankAccount()
        : ownerName("unknown"), accountNumber("000000"), balance(0.0) {
        // 初始化列表：比函数体内赋值更高效
    }

    BankAccount(const string& name, const string& accNo, double initBal)
        : ownerName(name), accountNumber(accNo), balance(initBal) {
        if (initBal < 0) throw invalid_argument("余额不能为负");
        logTransaction("开户", initBal);
    }

    // 拷贝构造
    BankAccount(const BankAccount& other)
        : ownerName(other.ownerName)
        , accountNumber(other.accountNumber)
        , balance(other.balance) {}

    // 移动构造（C++11）
    BankAccount(BankAccount&& other) noexcept
        : ownerName(move(other.ownerName))
        , accountNumber(move(other.accountNumber))
        , balance(other.balance) {
        other.balance = 0;
    }

    // 析构函数：对象销毁时调用，用于释放资源
    virtual ~BankAccount() = default;  // C++11 default 生成默认实现

    // ---- 成员函数 ----
    double getBalance() const { return balance; }
    string getOwner() const  { return ownerName; }

    void deposit(double amount) {
        if (amount <= 0) throw invalid_argument("金额必须为正");
        balance += amount;
        logTransaction("存款", amount);
    }

    virtual void withdraw(double amount) {  // virtual 见多态
        if (amount > balance) throw runtime_error("余额不足");
        balance -= amount;
        logTransaction("取款", amount);
    }

    // 静态成员函数：属于类，不属于对象
    static string getBankName() { return "MyBank"; }
};

// =================================================
// 2. 继承
// =================================================
class SavingsAccount : public BankAccount {  // 公有继承
private:
    double minimumBalance;

public:
    SavingsAccount(const string& name, const string& accNo,
                   double initBal, double minBal)
        : BankAccount(name, accNo, initBal)   // 调用基类构造函数
        , minimumBalance(minBal > 0 ? minBal : 100) {}

    // override（C++11）：显式标记重写，让编译器检查
    void withdraw(double amount) override {
        if (getBalance() - amount < minimumBalance)
            throw runtime_error("余额不能低于最低限额");
        BankAccount::withdraw(amount);  // 调用基类版本
    }
};

// =================================================
// 3. 多态（虚函数）
// =================================================
// 抽象基类：含有纯虚函数，不能实例化
class Shape {
public:
    virtual double area() const = 0;       // 纯虚函数
    virtual double perimeter() const = 0;
    virtual ~Shape() = default;             // 虚析构函数（极其重要！）
};

class Rectangle : public Shape {
    double w, h;
public:
    Rectangle(double w, double h) : w(w), h(h) {}
    double area() const override      { return w * h; }
    double perimeter() const override { return 2 * (w + h); }
};

class Circle : public Shape {
    double r;
public:
    explicit Circle(double r) : r(r) {}
    double area() const override      { return 3.14159 * r * r; }
    double perimeter() const override { return 2 * 3.14159 * r; }
};

// =================================================
// 4. Rule of Five（C++11）
// =================================================
// 如果一个类管理资源（如动态内存），则需要定义：
// 析构函数 + 拷贝构造 + 拷贝赋值 + 移动构造 + 移动赋值
class Buffer {
private:
    int* data;
    size_t size;
public:
    Buffer(size_t sz) : data(new int[sz]), size(sz) {}
    ~Buffer() { delete[] data; }

    Buffer(const Buffer& other)
        : data(new int[other.size]), size(other.size) {
        copy(other.data, other.data + size, data);
    }

    Buffer& operator=(const Buffer& other) {
        if (this == &other) return *this;  // 自赋值检查
        int* newData = new int[other.size];
        copy(other.data, other.data + other.size, newData);
        delete[] data;
        data = newData;
        size = other.size;
        return *this;
    }

    Buffer(Buffer&& other) noexcept
        : data(other.data), size(other.size) {
        other.data = nullptr;
        other.size = 0;
    }

    Buffer& operator=(Buffer&& other) noexcept {
        if (this != &other) {
            delete[] data;
            data = other.data;
            size = other.size;
            other.data = nullptr;
            other.size = 0;
        }
        return *this;
    }
};

// =================================================
// 使用示例
// =================================================
int main() {
    // 封装：通过公有接口操作
    BankAccount acc("Alice", "123456", 1000);
    acc.deposit(500);
    acc.withdraw(200);
    cout << "余额: " << acc.getBalance() << endl;  // 1300
    // acc.balance = 9999;  错误！balance 是 private

    // 继承
    SavingsAccount savings("Bob", "789012", 2000, 500);
    savings.withdraw(1000);

    // 多态
    Shape* shapes[] = { new Rectangle(3, 4), new Circle(5) };
    for (auto s : shapes) {
        cout << "面积: " << s->area()
             << ", 周长: " << s->perimeter() << endl;
        delete s;  // 虚析构函数确保正确清理
    }

    return 0;
}
```

#### 继承与访问控制

| 基类成员 | public 继承 | protected 继承 | private 继承 |
|----------|-------------|----------------|--------------|
| public   | public      | protected      | private      |
| protected| protected   | protected      | private      |
| private  | 不可见       | 不可见          | 不可见        |

继承选择：public 表示 "is-a" 关系；private 表示 "implemented-in-terms-of"（组合的替代）。

---

### 2.2 运算符重载

```cpp
#include <iostream>
using namespace std;

// 大部分运算符可重载，不能重载的：.  .*  ::  ?:  sizeof  typeid

class Complex {
private:
    double real, imag;
public:
    Complex(double r = 0, double i = 0) : real(r), imag(i) {}

    // 二元 +：返回新对象
    Complex operator+(const Complex& other) const {
        return Complex(real + other.real, imag + other.imag);
    }

    // 一元 -：返回新对象
    Complex operator-() const {
        return Complex(-real, -imag);
    }

    // 复合赋值：返回引用（支持链式：a += b += c）
    Complex& operator+=(const Complex& other) {
        real += other.real;
        imag += other.imag;
        return *this;
    }

    // 前缀 ++：返回引用
    Complex& operator++() {
        ++real; ++imag;
        return *this;
    }

    // 后缀 ++：返回旧值的拷贝（int 参数用于区分）
    Complex operator++(int) {
        Complex temp = *this;
        ++(*this);
        return temp;
    }

    // 比较运算符
    bool operator==(const Complex& other) const {
        return real == other.real && imag == other.imag;
    }

    // 下标运算符：返回引用以便修改
    double& operator[](size_t idx) {
        if (idx == 0) return real;
        if (idx == 1) return imag;
        throw out_of_range("下标为 0 或 1");
    }

    // 函数调用运算符（后述：lambda 底层就是重载 operator()）
    double operator()() const {
        return sqrt(real * real + imag * imag);  // 模长
    }

    // 类型转换运算符
    operator double() const {
        return sqrt(real * real + imag * imag);
    }

    // 友元声明
    friend ostream& operator<<(ostream& os, const Complex& c);
    friend istream& operator>>(istream& is, Complex& c);
};

// 流运算符必须为非成员函数（左操作数是流对象）
ostream& operator<<(ostream& os, const Complex& c) {
    os << c.real;
    if (c.imag >= 0) os << " + " << c.imag << "i";
    else             os << " - " << -c.imag << "i";
    return os;
}

istream& operator>>(istream& is, Complex& c) {
    return is >> c.real >> c.imag;
}

int main() {
    Complex a(3, 4), b(1, 2);
    Complex c = a + b;          // operator+(a, b)
    cout << "a + b = " << c << endl;  // operator<<

    c += a;
    cout << "c += a: " << c << endl;

    cout << "前缀++: " << ++c << endl;
    cout << "后缀++: " << c++ << ", 之后: " << c << endl;

    cout << "c[0] = " << c[0] << ", c[1] = " << c[1] << endl;

    double mag = c();           // 调用 operator()
    cout << "模长: " << mag << endl;

    double d = c;               // 调用 operator double()
    cout << "作为 double: " << d << endl;

    return 0;
}
```

---

### 2.3 模板

模板是 C++ 泛型编程的基础，允许编写与类型无关的代码。

```cpp
#include <iostream>
#include <vector>
#include <string>
using namespace std;

// =================================================
// 1. 函数模板
// =================================================
template <typename T>
T maximum(T a, T b) {
    return (a > b) ? a : b;  // T 必须支持 >
}

// 多模板参数
template <typename T, typename U>
auto add(T a, U b) -> decltype(a + b) {  // 尾置返回类型（C++11）
    return a + b;
}

// 模板特化（为特定类型提供专门实现）
template <>
const char* maximum<const char*>(const char* a, const char* b) {
    return (strcmp(a, b) > 0) ? a : b;
}

// =================================================
// 2. 类模板
// =================================================
template <typename T>
class Stack {
private:
    vector<T> elements;
public:
    void push(const T& val) { elements.push_back(val); }
    void pop() {
        if (empty()) throw runtime_error("Stack empty");
        elements.pop_back();
    }
    T& top() {
        if (empty()) throw runtime_error("Stack empty");
        return elements.back();
    }
    size_t size() const { return elements.size(); }
    bool empty() const { return elements.empty(); }
};

// =================================================
// 3. 可变参数模板（C++11）
// =================================================
// 递归终止
void printAll() { cout << endl; }

template <typename First, typename... Rest>
void printAll(const First& first, const Rest&... rest) {
    cout << first << " ";
    printAll(rest...);
}

// C++17 折叠表达式
template <typename... Args>
void printAllCpp17(Args... args) {
    ((cout << args << " "), ...);
    cout << endl;
}

// =================================================
// 4. 非类型模板参数
// =================================================
template <typename T, size_t Size>
constexpr size_t getSize(T (&)[Size]) {
    return Size;
}

// =================================================
// 使用示例
// =================================================
int main() {
    cout << maximum(3, 7) << endl;           // 7（int）
    cout << maximum(3.14, 2.71) << endl;     // 3.14（double）
    cout << maximum('A', 'Z') << endl;       // 'Z'

    // 类模板
    Stack<int> intStack;
    intStack.push(1);
    intStack.push(2);
    cout << "top: " << intStack.top() << endl;  // 2

    // 可变参数
    printAll(1, 2.5, "hello", 'A');           // 1 2.5 hello A
    printAllCpp17(1, 2.5, "hello", 'A');

    // 非类型模板
    int arr[] = {1, 2, 3, 4, 5};
    cout << "数组大小: " << getSize(arr) << endl;  // 5

    return 0;
}
```

---

### 2.4 STL 标准模板库

STL 包含六大组件：容器、迭代器、算法、函数对象、适配器、分配器。

```cpp
#include <iostream>
#include <vector>
#include <deque>
#include <list>
#include <array>
#include <map>
#include <unordered_map>
#include <set>
#include <algorithm>
#include <numeric>
#include <functional>
using namespace std;

int main() {
    // =================================================
    // 序列容器
    // =================================================

    // vector：连续内存动态数组，随机访问 O(1)，末尾操作分摊 O(1)
    vector<int> vec = {1, 2, 3, 4, 5};
    vec.push_back(6);
    vec.pop_back();
    cout << "size: " << vec.size() << ", capacity: " << vec.capacity() << endl;

    // deque：双端队列
    deque<int> deq = {1, 2, 3};
    deq.push_front(0);
    deq.push_back(4);

    // list：双向链表
    list<int> lst = {5, 2, 8, 1, 9};
    lst.sort();
    lst.unique();
    lst.remove(5);

    // =================================================
    // 关联容器
    // =================================================

    set<int> s = {3, 1, 4, 1, 5, 9};  // 实际：{1, 3, 4, 5, 9}
    s.insert(6);
    s.erase(1);
    auto it = s.find(3);

    map<string, int> ages;
    ages["Alice"] = 30;
    ages.insert({"Bob", 25});
    ages.emplace("Charlie", 35);

    // C++17 结构化绑定
    for (const auto& [name, age] : ages) {
        cout << name << ": " << age << endl;
    }

    // unordered_map：哈希表，查找 O(1) 平均
    unordered_map<string, int> scores;
    scores["Alice"] = 95;

    // =================================================
    // 迭代器
    // =================================================
    vector<int> v = {10, 20, 30, 40, 50};
    for (auto it = v.begin(); it != v.end(); ++it) cout << *it << " ";
    for (auto it = v.rbegin(); it != v.rend(); ++it) cout << *it << " ";

    // =================================================
    // 算法
    // =================================================
    vector<int> data = {5, 2, 8, 1, 9, 3, 7, 4, 6};

    sort(data.begin(), data.end());
    stable_sort(data.begin(), data.end());

    auto found = find(data.begin(), data.end(), 5);
    bool ex5 = binary_search(data.begin(), data.end(), 5);

    auto lower = lower_bound(data.begin(), data.end(), 3);
    auto upper = upper_bound(data.begin(), data.end(), 7);

    transform(data.begin(), data.end(), data.begin(),
              [](int x) { return x * 2; });

    auto newEnd = remove(data.begin(), data.end(), 0);
    data.erase(newEnd, data.end());

    vector<int> nums = {1, 2, 3, 4, 5};
    int sum = accumulate(nums.begin(), nums.end(), 0);  // 15

    return 0;
}
```

---

### 2.5 异常处理

```cpp
#include <iostream>
#include <stdexcept>
#include <string>
using namespace std;

// 自定义异常类
class MathException : public exception {
    string msg;
    int code;
public:
    MathException(const string& m, int c = 0) : msg(m), code(c) {}
    const char* what() const noexcept override { return msg.c_str(); }
    int getCode() const { return code; }
};

// RAII：资源绑定到对象生命周期
class FileHandler {
    FILE* file;
public:
    explicit FileHandler(const char* fname) {
        file = fopen(fname, "r");
        if (!file) throw runtime_error("无法打开文件");
    }
    ~FileHandler() { if (file) fclose(file); }
    FileHandler(const FileHandler&) = delete;
    FileHandler& operator=(const FileHandler&) = delete;
};

int main() {
    try {
        throw runtime_error("发生了一个错误");
    }
    catch (const runtime_error& e) {
        cerr << "捕获: " << e.what() << endl;
    }
    catch (const exception& e) {
        cerr << "标准异常: " << e.what() << endl;
    }
    catch (...) {
        cerr << "未知异常" << endl;
    }

    // 重新抛出
    try {
        try {
            throw runtime_error("内部错误");
        } catch (const runtime_error&) {
            cout << "捕获内部错误，重新抛出" << endl;
            throw;
        }
    } catch (const runtime_error& e) {
        cout << "外部重新捕获: " << e.what() << endl;
    }

    return 0;
}

/* 异常最佳实践：
 * 1. 使用 RAII 管理所有资源
 * 2. 按引用捕获 const exception&
 * 3. 不在析构函数中抛出异常
 * 4. 异常用于异常情况
 * 5. noexcept 标记不抛出的函数
 * 6. throw; 重新抛出，非 throw e;
 */
```

---

### 2.6 文件 I/O

```cpp
#include <iostream>
#include <fstream>
#include <sstream>
#include <string>
#include <filesystem>  // C++17
using namespace std;
namespace fs = filesystem;

int main() {
    // 写入
    ofstream out("output.txt");
    if (!out) { cerr << "无法打开" << endl; return 1; }
    out << "Hello!" << endl << "数字: " << 42 << endl;
    out.close();

    ofstream app("output.txt", ios::app);
    app << "追加内容" << endl;

    // 读取
    ifstream in("input.txt");
    string line;
    while (getline(in, line)) cout << "Line: " << line << endl;

    // 字符串流
    ostringstream oss;
    oss << "ID: " << 1001 << ", Score: " << 95.5;
    string fmt = oss.str();

    istringstream iss("42 3.14 hello");
    int i; double d; string s;
    iss >> i >> d >> s;

    // C++17 文件系统
    fs::path p = "C:\\Users\\file.txt";
    cout << "文件名: " << p.filename() << endl;
    cout << "扩展名: " << p.extension() << endl;

    fs::create_directory("new_folder");
    fs::create_directories("a/b/c");

    for (const auto& entry : fs::directory_iterator(".")) {
        cout << (entry.is_directory() ? "[DIR]" : "[FILE]")
             << entry.path().filename() << endl;
    }

    return 0;
}
```

---

## 三、高级篇

### 3.1 智能指针与内存管理

C++11 引入了三种智能指针，实现了 RAII 思想在动态内存管理上的应用，极大降低了内存泄漏风险。

```cpp
#include <iostream>
#include <memory>   // 智能指针全部定义在此
#include <string>
#include <vector>
using namespace std;

// ==========================================
// 1. unique_ptr — 独占所有权
// ==========================================
// - 不可拷贝，只能移动（转移所有权）
// - 大小与裸指针相同，零开销
// - 适合工厂函数返回值

class Resource {
    string name;
public:
    explicit Resource(const string& n) : name(n) {
        cout << "创建: " << name << endl;
    }
    ~Resource() { cout << "销毁: " << name << endl; }
    void work() const { cout << name << " working" << endl; }
};

unique_ptr<Resource> createRes(const string& name) {
    return make_unique<Resource>(name);  // C++14
}

void demoUnique() {
    auto res1 = make_unique<Resource>("R1");  // 推荐！异常安全

    // 移动所有权
    unique_ptr<Resource> res2 = move(res1);   // res1 变为 nullptr
    if (!res1) cout << "res1 为空" << endl;

    res2->work();          // 箭头运算符
    (*res2).work();        // 解引用

    Resource* raw = res2.get();   // 获取裸指针（不转移所有权）

    res2.reset(new Resource("R2"));  // 销毁旧对象，指向新对象
    res2.reset();                    // 销毁对象，变为 nullptr
}

// 自定义删除器
void demoDeleter() {
    auto fileDeleter = [](FILE* f) {
        if (f) fclose(f);
    };
    unique_ptr<FILE, decltype(fileDeleter)>
        fp(fopen("test.txt", "w"), fileDeleter);
}

// ==========================================
// 2. shared_ptr — 共享所有权
// ==========================================
// - 引用计数管理生命周期
// - 最后一个 shared_ptr 销毁时释放对象
// - 大小为裸指针的两倍（控制块）

void demoShared() {
    auto sp1 = make_shared<Resource>("Shared");  // 一次分配，高效
    cout << "引用计数: " << sp1.use_count() << endl;  // 1

    {
        auto sp2 = sp1;  // 拷贝构造，引用计数 +1
        cout << "引用计数: " << sp1.use_count() << endl;  // 2
    }  // sp2 销毁，引用计数 -1

    cout << "引用计数: " << sp1.use_count() << endl;  // 1
}

// 循环引用问题
struct Node {
    int val;
    shared_ptr<Node> next;       // 循环引用会导致内存泄漏
    // weak_ptr<Node> next;      // 解决方案：改用 weak_ptr
    ~Node() { cout << "Node " << val << " 销毁" << endl; }
};

void demoCycle() {
    auto a = make_shared<Node>();
    auto b = make_shared<Node>();
    a->next = b;
    b->next = a;  // 循环引用！两个对象永远不会被销毁
    cout << a.use_count() << endl;  // 2（永远降不到 0）
}

// ==========================================
// 3. weak_ptr — 弱引用
// ==========================================
// - 不影响引用计数，不控制生命周期
// - 使用 lock() 获取 shared_ptr
// - 用于解决循环引用

struct TreeNode {
    int val;
    weak_ptr<TreeNode> parent;  // 避免循环引用
    shared_ptr<TreeNode> left, right;
    TreeNode(int v) : val(v) {}
};

void demoWeak() {
    auto root = make_shared<TreeNode>(1);
    auto child = make_shared<TreeNode>(2);
    root->left = child;
    child->parent = root;  // weak_ptr 不影响计数

    // 使用 weak_ptr
    if (auto parent = child->parent.lock()) {  // lock() 返回 shared_ptr
        cout << "parent val: " << parent->val << endl;
    }
    if (child->parent.expired()) {  // 检查对象是否已销毁
        cout << "parent 已过期" << endl;
    }
}

// ==========================================
// 使用原则
// ==========================================
// 1. 优先 unique_ptr，除非需要共享所有权
// 2. 使用 make_unique / make_shared 创建
// 3. 函数参数：用裸指针或引用（非拥有）
// 4. 函数返回值：返回 unique_ptr
// 5. 用 weak_ptr 打破循环引用

int main() {
    demoUnique();
    demoShared();
    // demoCycle();  // 演示循环引用（会导致泄漏）
    demoWeak();
    return 0;
}
```

---

### 3.2 移动语义与右值引用

移动语义是 C++11 最重大的改进，通过\"窃取\"资源避免了不必要的拷贝。

```cpp
#include <iostream>
#include <string>
#include <cstring>
using namespace std;

// ==========================================
// 左值与右值
// ==========================================
// 左值(lvalue)：可取地址，有名字
// 右值(rvalue)：临时对象，不能取地址

void demoBasic() {
    int x = 42;           // x 是左值
    int* p = &x;          // 可取地址
    // &42;               // 错误！不能取右值地址

    // 左值引用 T& 绑定到左值
    int& lr = x;          // 正确
    // int& lr2 = 42;     // 错误！不能绑定到右值

    // 右值引用 T&& 绑定到右值
    int&& rr = 42;        // 正确
    // int&& rr2 = x;     // 错误！不能绑定到左值

    // const 左值引用可绑定到右值（延长临时对象生命周期）
    const int& clr = 42;  // 正确
}

// ==========================================
// 移动构造与移动赋值
// ==========================================
class StringBuf {
    char* data;
    size_t len;
public:
    StringBuf(const char* s = "") : len(strlen(s)) {
        data = new char[len + 1];
        strcpy(data, s);
        cout << "构造: " << data << endl;
    }

    ~StringBuf() {
        delete[] data;
        cout << "析构" << endl;
    }

    // 拷贝构造（深拷贝）
    StringBuf(const StringBuf& other) : len(other.len) {
        data = new char[len + 1];
        strcpy(data, other.data);
        cout << "拷贝构造: " << data << endl;
    }

    // 移动构造（窃取资源）
    StringBuf(StringBuf&& other) noexcept
        : data(other.data), len(other.len) {
        other.data = nullptr;  // 源对象置为可销毁状态
        other.len = 0;
        cout << "移动构造: " << data << endl;
    }

    // 移动赋值
    StringBuf& operator=(StringBuf&& other) noexcept {
        if (this != &other) {
            delete[] data;
            data = other.data;
            len = other.len;
            other.data = nullptr;
            other.len = 0;
            cout << "移动赋值" << endl;
        }
        return *this;
    }
};

// ==========================================
// std::move 与 std::forward
// ==========================================
// move：无条件将左值转为右值引用
// forward：条件性转发（完美转发）

void process(StringBuf& s) {
    cout << "左值版本" << endl;
}
void process(StringBuf&& s) {
    cout << "右值版本" << endl;
}

template <typename T>
void wrapper(T&& arg) {          // 转发引用
    process(forward<T>(arg));    // 保持参数的值类别
}

int main() {
    StringBuf a("Hello");
    StringBuf b = move(a);       // 移动构造（a 的资源转给 b）
    cout << "a 移动后: " << (a.c_str() ? a.c_str() : "null") << endl;

    StringBuf c("World");
    c = move(b);                 // 移动赋值

    // 完美转发
    wrapper(c);                  // 左值版本
    wrapper(StringBuf("Tmp"));   // 右值版本

    return 0;
}

/*
 * 移动语义使用指南：
 * 1. 移动构造函数和移动赋值标记为 noexcept
 * 2. 移动后的对象处于"有效但未指定"状态
 * 3. 不要对 const 对象使用 move
 * 4. 不要过度使用 move（RVO/NRVO 通常已足够）
 */
```

---

### 3.3 Lambda 表达式

Lambda 是 C++11 引入的匿名函数对象，可捕获外部变量，极大地简化了代码。

```cpp
#include <iostream>
#include <vector>
#include <algorithm>
#include <functional>
using namespace std;

int main() {
    // ==========================================
    // 基本语法
    // ==========================================
    // [捕获](参数) -> 返回类型 { 函数体 }
    // -> 返回类型可省略（自动推导）

    auto hello = [] { cout << "Hello Lambda!" << endl; };
    hello();

    auto add = [](int a, int b) { return a + b; };
    cout << "3+5=" << add(3, 5) << endl;

    // ==========================================
    // 捕获外部变量
    // ==========================================
    int base = 10;
    int mult = 2;

    auto byVal = [base](int x) { return base * x; };       // 按值捕获
    auto byRef = [&base](int x) { return base * x; };      // 按引用捕获
    auto allByVal = [=](int x) { return x * mult; };       // 全部按值
    auto allByRef = [&](int x) { return x * mult; };       // 全部按引用

    // C++14 初始化捕获
    auto initCap = [value = base * 2](int x) { return x * value; };

    // 移动捕获（C++14）
    unique_ptr<int> ptr = make_unique<int>(42);
    auto moveCap = [p = move(ptr)] { return *p; };
    // ptr 已为空

    // ==========================================
    // mutable：允许修改按值捕获的变量
    // ==========================================
    int counter = 0;
    auto mu = [counter]() mutable {
        return ++counter;  // 修改的是 lambda 内部的拷贝
    };
    cout << mu() << endl;   // 1
    cout << mu() << endl;   // 2
    cout << counter << endl; // 0（外部不变）

    // ==========================================
    // 泛型 Lambda（C++14 auto 参数）
    // ==========================================
    auto generic = [](auto a, auto b) { return a + b; };
    cout << generic(3, 4) << endl;           // int
    cout << generic(2.5, 3.7) << endl;       // double

    // ==========================================
    // Lambda 与算法（最常见用法）
    // ==========================================
    vector<int> nums = {5, 2, 8, 1, 9, 3, 7, 4, 6};

    sort(nums.begin(), nums.end(), [](int a, int b) {
        return a > b;  // 降序
    });

    auto it = find_if(nums.begin(), nums.end(),
                      [](int n) { return n > 5; });

    int even = count_if(nums.begin(), nums.end(),
                        [](int n) { return n % 2 == 0; });

    transform(nums.begin(), nums.end(), nums.begin(),
              [](int n) { return n * n; });

    // ==========================================
    // Lambda 捕获 this 与 *this
    // ==========================================
    struct Worker {
        int value = 42;
        void work() {
            // [this] 捕获指针（小心悬空）
            auto f1 = [this] { return value; };

            // [*this]（C++17）捕获当前对象的拷贝
            auto f2 = [*this] { return value; };  // 安全
        }
    };

    // ==========================================
    // Lambda 与 std::function
    // ==========================================
    // std::function 可存储任意可调用对象
    function<int(int, int)> func;

    func = [](int a, int b) { return a + b; };
    cout << func(3, 4) << endl;   // 7

    func = multiplies<int>();
    cout << func(3, 4) << endl;   // 12

    return 0;
}
```

---

### 3.4 并发编程与多线程

C++11 在标准库中引入了线程支持库，这是 C++ 标准化进程中的重要里程碑。

```cpp
#include <iostream>
#include <thread>       // std::thread
#include <mutex>        // std::mutex, std::lock_guard
#include <future>       // std::async, std::future
#include <atomic>       // std::atomic
#include <chrono>       // 时间库
#include <vector>
using namespace std;

// ==========================================
// 1. 基本线程
// ==========================================
void worker(int id) {
    cout << "线程 " << id << " 启动" << endl;
    this_thread::sleep_for(chrono::milliseconds(100));
    cout << "线程 " << id << " 结束" << endl;
}

void demoThread() {
    thread t1(worker, 1);  // 创建线程并启动
    thread t2(worker, 2);

    t1.join();  // 等待线程结束（阻塞）
    t2.join();  // 必须 join 或 detach，否则析构会调用 terminate

    // detach：分离线程（后台运行）
    // thread t3(worker, 3);
    // t3.detach();
}

// ==========================================
// 2. 互斥锁与数据竞争
// ==========================================
int sharedCounter = 0;       // 共享数据——需要保护！
mutex mtx;                   // 互斥锁

void safeIncrement() {
    for (int i = 0; i < 100000; ++i) {
        // lock_guard：RAII 包装，构造时加锁，析构时解锁
        lock_guard<mutex> lock(mtx);
        ++sharedCounter;     // 受保护的临界区
    }
    // 自动解锁
}

void unsafeIncrement() {
    for (int i = 0; i < 100000; ++i) {
        ++sharedCounter;     // 数据竞争！未定义行为！
    }
}

// ==========================================
// 3. std::async 与 std::future
// ==========================================
int compute(int x) {
    this_thread::sleep_for(chrono::seconds(1));  // 模拟耗时的计算
    return x * x;
}

void demoFuture() {
    // async：异步执行，返回 future
    future<int> result = async(launch::async, compute, 42);

    cout << "等待结果..." << endl;

    // get()：阻塞直到结果可用
    int value = result.get();
    cout << "结果: " << value << endl;  // 1764

    // launch::deferred：延迟执行（直到调用 get）
    // launch::async：在新线程上立即执行
    // launch::async | launch::deferred：默认，自动选择
}

// ==========================================
// 4. std::atomic — 无锁编程
// ==========================================
atomic<int> atomicCounter(0);

void atomicIncrement() {
    for (int i = 0; i < 100000; ++i) {
        ++atomicCounter;    // 原子操作，无需互斥锁
    }
}

// ==========================================
// 5. 条件变量（生产者-消费者模式）
// ==========================================
struct MessageQueue {
    queue<int> q;
    mutex mtx;
    condition_variable cv;

    void produce(int val) {
        lock_guard<mutex> lock(mtx);
        q.push(val);
        cv.notify_one();  // 唤醒一个等待的消费者
    }

    int consume() {
        unique_lock<mutex> lock(mtx);
        cv.wait(lock, [this] { return !q.empty(); });  // 等待条件
        int val = q.front();
        q.pop();
        return val;
    }
};

int main() {
    // 基本线程
    demoThread();

    // 互斥锁
    sharedCounter = 0;
    thread t1(safeIncrement);
    thread t2(safeIncrement);
    t1.join(); t2.join();
    cout << "安全计数: " << sharedCounter << endl;  // 200000

    // atomic
    atomicCounter = 0;
    thread ta1(atomicIncrement);
    thread ta2(atomicIncrement);
    ta1.join(); ta2.join();
    cout << "原子计数: " << atomicCounter.load() << endl;  // 200000

    // future
    demoFuture();

    return 0;
}

/*
 * 并发最佳实践：
 * 1. 优先使用 async 而不是直接创建线程
 * 2. 使用 lock_guard / unique_lock 确保异常安全
 * 3. 避免死锁：多锁时使用 std::lock 或固定加锁顺序
 * 4. 简单计数用 atomic 替代 mutex
 * 5. 线程间通信优先使用 future/promise 而非共享变量
 */
```

---

### 3.5 C++11/14/17/20 新特性概览

C++ 每三年发布一个新标准。以下按版本梳理关键特性。

#### C++11（最重要的版本）

| 特性 | 说明 |
|------|------|
| auto | 类型自动推导 |
| decltype | 获取表达式类型 |
| 右值引用与移动语义 | move, forward |
| Lambda 表达式 | 匿名函数 |
| 智能指针 | unique_ptr, shared_ptr, weak_ptr |
| 范围 for | for (int x : vec) |
| 初始化列表 | vector<int> v = {1, 2, 3} |
| 静态断言 | static_assert |
| 强类型枚举 | enum class |
| 常量表达式 | constexpr |
| nullptr | 类型安全的空指针 |
| 可变参数模板 | template<typename... Args> |
| 委托构造 | 构造函数调用同类的其他构造 |
| = delete / = default | 禁止/默认生成特殊成员 |
| override / final | 显式重写和禁止重写 |
| long long | 64 位整型 |
| thread 支持库 | 线程、互斥锁、条件变量 |

```cpp
// auto 与 decltype
auto x = 42;                    // int
decltype(x) y = 100;            // int

// 初始化列表
vector<int> v = {1, 2, 3};
map<string, int> m = {{"a", 1}, {"b", 2}};

// 委托构造
class Foo {
    Foo() : Foo(0, "") {}       // 委托到另一个构造
    Foo(int n, string s) : num(n), str(s) {}
    int num; string str;
};

// = delete 与 = default
class NoCopy {
    NoCopy(const NoCopy&) = delete;      // 禁止拷贝
    NoCopy& operator=(const NoCopy&) = delete;
    ~NoCopy() = default;                  // 使用默认实现
};

// static_assert 编译期断言
static_assert(sizeof(int) == 4, "int must be 4 bytes");
```

#### C++14

| 特性 | 说明 |
|------|------|
| 泛型 Lambda | auto 参数 |
| 返回类型推导 | auto 返回值 |
| 变量模板 | template<T> T pi = T(3.14) |
| 初始化捕获 | [x = expr] |
| make_unique | 完善智能指针 |
| 二进制字面量 | 0b1010 |
| 数字分隔符 | 1'000'000 |
| [[deprecated]] | 标记弃用 |

```cpp
// 泛型 Lambda
auto add = [](auto a, auto b) { return a + b; };

// 变量模板
template<typename T>
constexpr T pi = T(3.141592653589793);

cout << pi<double> << endl;
```

#### C++17

| 特性 | 说明 |
|------|------|
| 折叠表达式 | (args + ...) |
| if/switch 初始化 | if (init; cond) |
| 结构化绑定 | auto [a, b] = pair |
| string_view | 字符串视图（零拷贝） |
| optional | 可选值 |
| variant | 类型安全联合体 |
| any | 任意类型值 |
| 内联变量 | inline static 成员 |
| if constexpr | 编译期条件分支 |
| 文件系统库 | filesystem |
| 并行算法 | execution policy |
| nodiscard | 返回值不应被忽略 |
| [[fallthrough]] | 表明 case 穿透是有意的 |

```cpp
// 折叠表达式
template<typename... Args>
auto sum(Args... args) { return (args + ...); }

// 结构化绑定
map<string, int> scores;
for (const auto& [name, score] : scores) { ... }

// if constexpr
template<typename T>
auto getValue(T t) {
    if constexpr (is_pointer_v<T>) return *t;
    else return t;
}

// string_view（零拷贝字符串引用）
string_view sv = "hello world";  // 不分配内存

// optional
optional<int> maybeInt = 42;
if (maybeInt) cout << *maybeInt << endl;
```

#### C++20

| 特性 | 说明 |
|------|------|
| Concepts | 模板约束（编译期接口） |
| 协程（Coroutines） | co_await, co_yield, co_return |
| 范围（Ranges） | 惰性求值视图和管道操作 |
| 三向比较 | <=> 太空船运算符 |
| 标准库模块 | import（替代 #include） |
| constexpr 扩展 | constexpr virtual / vector / string |
| [[likely]] / [[unlikely]] | 分支预测提示 |
| std::span | 连续内存视图 |
| 计时库改进 | clock_cast, utc_clock |

```cpp
// Concept
template<typename T>
concept Integral = is_integral_v<T>;

template<Integral T>
T half(T val) { return val / 2; }

// 三向比较（自动生成所有比较运算符）
struct Point {
    int x, y;
    auto operator<=>(const Point&) const = default;
};
// 自动支持 ==, !=, <, <=, >, >=

// Ranges
vector<int> v = {5, 2, 8, 1, 9};
auto even = v | views::filter([](int n) { return n % 2 == 0; });
```

---

### 3.6 设计模式

以下是 C++ 中最常用的一些设计模式实现。

```cpp
#include <iostream>
#include <memory>
#include <string>
#include <map>
using namespace std;

// ==========================================
// 1. 单例模式（Singleton）
// ==========================================
// 确保一个类只有一个实例，提供全局访问点
class Singleton {
private:
    Singleton() = default;                          // 私有构造
    Singleton(const Singleton&) = delete;           // 禁止拷贝
    Singleton& operator=(const Singleton&) = delete;

public:
    static Singleton& getInstance() {               // 唯一访问点
        static Singleton instance;                  // C++11 线程安全的局部静态
        return instance;
    }

    void doSomething() { cout << "Singleton work" << endl; }
};

// ==========================================
// 2. 工厂模式（Factory Method）
// ==========================================
// 定义一个创建对象的接口，让子类决定实例化哪个类
struct Product {
    virtual void use() = 0;
    virtual ~Product() = default;
};

struct ProductA : Product { void use() override { cout << "Product A" << endl; } };
struct ProductB : Product { void use() override { cout << "Product B" << endl; } };

struct Factory {
    static unique_ptr<Product> create(const string& type) {
        if (type == "A") return make_unique<ProductA>();
        if (type == "B") return make_unique<ProductB>();
        return nullptr;
    }
};

// ==========================================
// 3. 观察者模式（Observer）
// ==========================================
// 定义一对多依赖关系，当一个对象状态改变时，所有依赖者得到通知
struct Observer {
    virtual void update(const string& msg) = 0;
    virtual ~Observer() = default;
};

struct Subject {
    vector<Observer*> observers;

    void attach(Observer* o) { observers.push_back(o); }
    void notify(const string& msg) {
        for (auto o : observers) o->update(msg);
    }
};

struct User : Observer {
    string name;
    explicit User(const string& n) : name(n) {}
    void update(const string& msg) override {
        cout << name << " 收到: " << msg << endl;
    }
};

// ==========================================
// 4. 策略模式（Strategy）
// ==========================================
// 定义一组算法，使其可以互相替换
struct SortStrategy {
    virtual void sort(vector<int>& data) = 0;
    virtual ~SortStrategy() = default;
};

struct BubbleSort : SortStrategy {
    void sort(vector<int>& data) override { cout << "冒泡排序" << endl; }
};

struct QuickSort : SortStrategy {
    void sort(vector<int>& data) override { cout << "快速排序" << endl; }
};

struct Sorter {
    unique_ptr<SortStrategy> strategy;
    explicit Sorter(unique_ptr<SortStrategy> s) : strategy(move(s)) {}
    void execute(vector<int>& data) { strategy->sort(data); }
};

// ==========================================
// 使用示例
// ==========================================
int main() {
    // Singleton
    Singleton::getInstance().doSomething();

    // Factory
    auto prod = Factory::create("A");
    prod->use();

    // Observer
    Subject subj;
    User u1("Alice"), u2("Bob");
    subj.attach(&u1);
    subj.attach(&u2);
    subj.notify("有新消息！");

    // Strategy
    vector<int> data = {5, 2, 8, 1, 9};
    Sorter sorter(make_unique<QuickSort>());
    sorter.execute(data);

    return 0;
}
```

---

## 四、深入学习方向

### 4.1 内存模型与性能优化

C++ 允许程序员精细控制内存，这是其性能优势的来源。

**核心概念：**

| 概念 | 说明 |
|------|------|
| 栈（Stack） | 局部变量，自动分配释放，大小有限（~1-8MB） |
| 堆（Heap） | 动态分配（new/delete），空间大，需要手动管理 |
| 静态存储区 | 全局/静态变量，程序开始到结束 |
| RAII | 资源获取即初始化，对象生命周期管理资源 |
| Cache 友好性 | 连续内存访问（vector vs list） |
| 虚函数开销 | 通过 vtable 间接调用，阻止内联 |
| 异常开销 | 无异常时零开销，抛出异常时代价大 |

**性能优化要点：**

```cpp
// 1. 减少动态分配
// 坏：每次添加都分配
vector<int> bad;
for (int i = 0; i < 1000; ++i) bad.push_back(i);

// 好：预分配
vector<int> good;
good.reserve(1000);
for (int i = 0; i < 1000; ++i) good.push_back(i);

// 2. 传 const 引用避免拷贝
void process(const LargeObject& obj);      // 好
void process(LargeObject obj);             // 坏

// 3. 使用移动语义
vector<string> createStrings();

vector<string> v = createStrings();  // 移动（而非拷贝）

// 4. 使用 emplace 代替 push_back
v.emplace_back("hello");   // 直接在容器内构造，避免临时对象
v.push_back("hello");      // 构造临时对象 + 移动/拷贝

// 5. 编译期计算
constexpr int factorial(int n) {
    return n <= 1 ? 1 : n * factorial(n - 1);
}
int arr[factorial(5)];  // 编译期确定数组大小
```

### 4.2 网络编程

C++ 网络编程通常使用 Boost.Asio 或 C++20 标准网络库。

**核心概念：**

- **Socket**：网络通信的端点
- **TCP**：面向连接的可靠流协议
- **UDP**：无连接的不可靠数据报协议
- **I/O 多路复用**：select / poll / epoll / IOCP
- **异步 I/O**：非阻塞操作，回调通知

**常用库：**

| 库 | 说明 |
|----|------|
| Boost.Asio | 最流行的跨平台网络库 |
| libuv | Node.js 底层异步 I/O 库 |
| cpp-httplib | 轻量级 HTTP 库 |
| POCO | 全面的 C++ 网络库 |
| Asio（standalone）| Boost.Asio 的独立版本 |

### 4.3 游戏开发中的 C++

C++ 是游戏开发领域的首选语言，几乎所有主流游戏引擎都使用 C++。

**主要应用：**

| 方向 | 说明 |
|------|------|
| 游戏引擎 | Unreal Engine (C++), Unity (C++ 内核), Godot (C++ 内核) |
| 渲染引擎 | DirectX, Vulkan, OpenGL |
| 物理引擎 | PhysX, Box2D, Bullet |
| 音频引擎 | FMOD, Wwise |
| 网络引擎 | 多人游戏服务器端 |

**关键技能：**

- 内存管理（对象池、内存池、自定义分配器）
- 数据驱动设计（ECS 架构）
- 性能分析（profiling）与优化
- 多线程与 Job System
- 网络同步（帧同步、状态同步）

### 4.4 推荐资源

| 资源类型 | 名称 | 说明 |
|----------|------|------|
| 书籍 | 《C++ Primer 中文版》 | 最推荐的入门书，覆盖 C++11 |
| 书籍 | 《Effective C++》 | Scott Meyers，55 个最佳实践 |
| 书籍 | 《Effective Modern C++》 | C++11/14 最佳实践 |
| 书籍 | 《The C++ Programming Language》 | Bjarne Stroustrup 著，C++之父 |
| 书籍 | 《C++ Concurrency in Action》 | 并发编程权威指南 |
| 书籍 | 《STL 源码剖析》 | 侯捷，深入理解 STL 实现 |
| 网站 | cppreference.com | 最权威的 C++ 参考文档 |
| 网站 | isocpp.org | C++ 标准委员会官网 |
| 网站 | godbolt.org | Compiler Explorer，在线调试汇编 |
| 网站 | quick-bench.com | 在线性能测试对比 |
| 课程 | 侯捷 C++ 系列课程 | 台湾名师，深入浅出 |
| 课程 | Stanford CS106B | 斯坦福大学 C++ 课程 |
| 工具 | Visual Studio / CLion | 两大主流 IDE |
| 工具 | Valgrind / AddressSanitizer | 内存错误检测工具 |
| 工具 | perf / VTune / Tracy | 性能分析工具 |

**学习路线建议：**

```
1. 基础语法与概念  →  《C++ Primer》前 7 章
2. STL 与泛型编程  →  《C++ Primer》第 8-13 章 + cppreference
3. 面向对象编程    →  《C++ Primer》第 14-19 章
4. 现代 C++ 特性   →  《Effective Modern C++》
5. 实践项目        →  小工具 → 简单游戏 → 网络库
6. 深入源码        →  STL 源码阅读 → 开源项目贡献
7. 专家之路        →  性能优化 → 模板元编程 → 库设计
```

---

## 五、工程实践篇——从学会到能工作

> **本章的目标**：掌握前面四章内容后，你已经具备了 C++ 语言的坚实基础。但实际工作中的 C++ 开发还需要掌握构建系统、测试、调试、代码审查、第三方库集成等一系列工程技能。本章将这些必备技能系统化，帮助你在真实项目中高效工作。

### 5.1 项目组织与构建系统（CMake）

C++ 项目使用 CMake 作为事实上的标准构建系统生成器。

```cmake
# CMakeLists.txt —— 最小项目
cmake_minimum_required(VERSION 3.20)          # 指定最低 CMake 版本
project(MyApp VERSION 1.0.0 LANGUAGES CXX)    # 项目名、版本、语言

set(CMAKE_CXX_STANDARD 20)                    # C++20 标准
set(CMAKE_CXX_STANDARD_REQUIRED ON)            # 强制使用，不降级
set(CMAKE_CXX_EXTENSIONS OFF)                  # 禁止编译器扩展（保证可移植）

# 添加可执行目标
add_executable(my_app
    src/main.cpp
    src/utils.cpp
    src/utils.h
)

# 添加库目标
add_library(my_lib
    src/core.cpp
    src/core.h
)

# 链接库
target_link_libraries(my_app PRIVATE my_lib)

# 指定包含目录
target_include_directories(my_lib
    PUBLIC  include      # 使用者也能看到
    PRIVATE src          # 仅自己可见
)
```

**标准项目目录结构：**

```
my_project/
├── CMakeLists.txt          # 顶层构建文件
├── cmake/                  # CMake 辅助模块
│   └── FindSomething.cmake
├── include/                # 公有头文件
│   └── my_project/
│       ├── core.h
│       └── utils.h
├── src/                    # 源文件和私有头文件
│   ├── core.cpp
│   ├── utils.cpp
│   └── main.cpp
├── tests/                  # 测试文件
│   ├── CMakeLists.txt
│   └── test_core.cpp
├── third_party/            # 第三方依赖（git submodule）
│   └── fmt/
├── scripts/                # 工具脚本
├── docs/                   # 文档
└── README.md
```

**CMake 常用命令速查：**

```cmake
# 查找依赖包
find_package(fmt REQUIRED)
target_link_libraries(my_app PRIVATE fmt::fmt)

# 编译选项
target_compile_options(my_app PRIVATE -Wall -Wextra -Wpedantic -Werror)

# 添加定义宏
target_compile_definitions(my_app PRIVATE NDEBUG)  # Release 时禁用 assert

# 安装规则
install(TARGETS my_app DESTINATION bin)
install(FILES config.json DESTINATION etc)

# 测试支持
enable_testing()
add_test(NAME test_core COMMAND test_core)
```

**构建与运行：**
```bash
# Configure + Build
cmake -B build -DCMAKE_BUILD_TYPE=Debug      # 配置（Debug）
cmake --build build                           # 编译
./build/my_app                                # 运行

# Release 构建
cmake -B build_release -DCMAKE_BUILD_TYPE=Release
cmake --build build_release

# 运行测试
cmake --build build --target test
ctest --test-dir build --output-on-failure
```

---

### 5.2 Pimpl 惯用法与 ABI 稳定性

Pimpl（Pointer to Implementation）是 C++ 中减少编译依赖、隐藏实现细节的经典技巧。

```cpp
// ============ widget.h ============
#pragma once
#include <memory>

// 对外暴露的接口类
class Widget {
public:
    Widget();
    ~Widget();

    Widget(Widget&&) noexcept;             // 移动构造
    Widget& operator=(Widget&&) noexcept;   // 移动赋值

    void doSomething(int value);

private:
    // 前置声明实现类——用户看不到实现细节
    struct Impl;
    std::unique_ptr<Impl> pImpl;
};

// ============ widget.cpp ============
#include "widget.h"
#include <iostream>
#include <string>
#include <vector>  // 用户不需要知道我们用了 vector！

struct Widget::Impl {
    std::string name;
    std::vector<int> data;
    void log() const {
        std::cout << "Widget: " << name << std::endl;
    }
};

Widget::Widget() : pImpl(std::make_unique<Impl>()) {}
Widget::~Widget() = default;   // 必须放在 .cpp 中，因为 Impl 在这里完整定义
Widget::Widget(Widget&&) noexcept = default;
Widget& Widget::operator=(Widget&&) noexcept = default;

void Widget::doSomething(int value) {
    pImpl->data.push_back(value);
    pImpl->log();
}

/* Pimpl 优势：
 * 1. 编译加速：修改 Impl 不触发使用者重新编译
 * 2. ABI 稳定：Impl 大小变化不影响 Widget 大小
 * 3. 接口纯净：头文件只暴露公有 API
 * 4. 实现隐藏：用户看不到实现细节
 */
```

---

### 5.3 CRTP（奇异递归模板模式）

CRTP 是一种静态多态技巧，在编译期实现"基类调用派生类方法"，零运行时开销。

```cpp
#include <iostream>
using namespace std;

// CRTP 基类模板
template <typename Derived>
class AnimalBase {
public:
    // 静态多态：在基类中调用派生类的方法
    void speak() const {
        // 将 this 转为派生类指针，调用派生类的 speakImpl
        static_cast<const Derived*>(this)->speakImpl();
    }

    // 提供默认实现的虚函数（可选）
    void move() const {
        cout << "Animal moves" << endl;
    }
};

class Dog : public AnimalBase<Dog> {
public:
    void speakImpl() const {
        cout << "Woof! Woof!" << endl;
    }
};

class Cat : public AnimalBase<Cat> {
public:
    void speakImpl() const {
        cout << "Meow~" << endl;
    }
};

// 使用编译期多态的模板函数
template <typename T>
void makeSpeak(const AnimalBase<T>& animal) {
    animal.speak();  // 编译期绑定，无虚函数开销
}

int main() {
    Dog dog;
    Cat cat;

    makeSpeak(dog);  // Woof! Woof!
    makeSpeak(cat);  // Meow~

    return 0;
}

/* CRTP 典型应用场景：
 * 1. 代码复用（如为派生类自动实现 operator==）
 * 2. 静态接口（完全避免虚函数开销）
 * 3. enable_shared_from_this（标准库就用了 CRTP！）
 */
```

---

### 5.4 Google Test 单元测试

测试是专业 C++ 开发的必备技能，Google Test 是最流行的框架。

```cpp
// ============ test_core.cpp ============
#include <gtest/gtest.h>          // Google Test 头文件
#include "core.h"                 // 被测代码

// 基本测试用例
TEST(MathTest, Add) {
    EXPECT_EQ(add(2, 3), 5);      // 期望相等（不致命）
    EXPECT_EQ(add(-1, 1), 0);
    ASSERT_EQ(add(0, 0), 0);      // 断言相等（失败会终止当前测试）
}

TEST(MathTest, Divide) {
    EXPECT_DOUBLE_EQ(divide(10, 3), 3.3333333333);  // 浮点比较
    ASSERT_THROW(divide(1, 0), std::invalid_argument); // 期望抛出异常
}

// 测试夹具（Test Fixture）：复用设置
class VectorTest : public ::testing::Test {
protected:
    void SetUp() override {       // 每个 TEST_F 前执行
        v = {1, 2, 3, 4, 5};
    }
    void TearDown() override { }  // 每个 TEST_F 后执行

    std::vector<int> v;
};

TEST_F(VectorTest, Size) {
    EXPECT_EQ(v.size(), 5);
}

TEST_F(VectorTest, PushBack) {
    v.push_back(6);
    EXPECT_EQ(v.size(), 6);
    EXPECT_EQ(v.back(), 6);
}

// 参数化测试
class ParamTest : public ::testing::TestWithParam<int> {};
TEST_P(ParamTest, IsPositive) {
    EXPECT_GT(GetParam(), 0);
}
INSTANTIATE_TEST_SUITE_P(PositiveValues, ParamTest,
                         ::testing::Values(1, 5, 10, 100));

// Mock 对象（Google Mock）
#include <gmock/gmock.h>

class Database {
public:
    virtual ~Database() = default;
    virtual bool save(const std::string& data) = 0;
};

class MockDatabase : public Database {
public:
    MOCK_METHOD(bool, save, (const std::string&), (override));
};

TEST(UserService, SaveUser) {
    MockDatabase db;
    EXPECT_CALL(db, save(testing::_))
        .Times(1)
        .WillOnce(testing::Return(true));

    UserService service(&db);
    EXPECT_TRUE(service.saveUser("Alice"));
}
```

**CMake 集成 Google Test：**

```cmake
include(FetchContent)
FetchContent_Declare(
    googletest
    URL https://github.com/google/googletest/archive/release-1.12.1.zip
)
FetchContent_MakeAvailable(googletest)

enable_testing()
add_executable(test_core test_core.cpp)
target_link_libraries(test_core PRIVATE gtest gmock gtest_main)
add_test(NAME test_core COMMAND test_core)
```

---

### 5.5 调试与诊断

```cpp
#include <iostream>
#include <vector>
#include <cassert>    // assert 宏
#include <stdexcept>
using namespace std;

// ==========================================
// 1. 断言（Assertion）
// ==========================================
int dividePositive(int a, int b) {
    // assert：调试期检查，Release 模式下（NDEBUG）被移除
    assert(b > 0 && "分母必须为正数");

    // 接口参数的运行时检查（即使 Release 也生效）
    if (b <= 0) {
        throw invalid_argument("分母必须为正数");
    }
    return a / b;
}

// ==========================================
// 2. static_assert —— 编译期断言
// ==========================================
template <typename T>
T compute(T val) {
    static_assert(std::is_arithmetic_v<T>,
                  "T 必须是算术类型");
    return val * val;
}

// ==========================================
// 3. AddressSanitizer 检测
// ==========================================
// 编译时加上：-fsanitize=address -g
// 运行时自动检测：越界访问、use-after-free、内存泄漏
void demoASan() {
    int* arr = new int[10];
    // arr[10] = 42;  // 越界！ASan 会报告并终止
    // delete[] arr;
    // arr[0] = 1;    // use-after-free！ASan 会报告
}

// ==========================================
// 4. 日志分级（工作中必备）
// ==========================================
enum class LogLevel { DEBUG, INFO, WARN, ERROR };

class Logger {
public:
    static void log(LogLevel level, const string& msg) {
        static const char* levelStr[] = {"DEBUG", "INFO", "WARN", "ERROR"};
        cout << "[" << levelStr[static_cast<int>(level)] << "] " << msg << endl;
    }
};

#define LOG_DEBUG(msg) Logger::log(LogLevel::DEBUG, msg)
#define LOG_INFO(msg)  Logger::log(LogLevel::INFO,  msg)
#define LOG_WARN(msg)  Logger::log(LogLevel::WARN,  msg)
#define LOG_ERROR(msg) Logger::log(LogLevel::ERROR, msg)

/*
 * 调试技巧：
 *
 * GDB 基础命令：
 *   break file.cpp:42   断点
 *   run                 运行
 *   next                单步跳过
 *   step                单步进入
 *   print var           打印变量
 *   backtrace           调用堆栈
 *   frame 2             切换到第 2 帧
 *   info locals         查看所有局部变量
 *
 * VS Code 调试（launch.json）：
 * {
 *   "type": "cppdbg",
 *   "request": "launch",
 *   "program": "${workspaceFolder}/build/app",
 *   "args": [],
 *   "stopAtEntry": false,
 *   "cwd": "${workspaceFolder}",
 *   "environment": [],
 *   "externalConsole": false,
 *   "MIMode": "gdb",
 *   "miDebuggerPath": "/usr/bin/gdb",
 *   "setupCommands": [{"text": "-enable-pretty-printing"}]
 * }
 *
 * 编译选项速查：
 *   -g         含调试符号
 *   -O0        关闭优化（调试时用）
 *   -O2        开启优化（Release）
 *   -Wall -Wextra -Wpedantic -Werror  全开警告且视为错误
 *   -fsanitize=address  内存错误检测
 *   -fsanitize=undefined 未定义行为检测
 *   -fno-omit-frame-pointer  保留帧指针（方便栈回溯）
 */
```

---

### 5.6 常用第三方库速查

```cpp
#include <iostream>
#include <string>
#include <vector>
using namespace std;

// ==========================================
// 1. fmtlib —— 现代格式化（已被 C++20 标准采纳）
// ==========================================
// #include <fmt/core.h>     // 第三方库
// #include <format>         // C++20 标准版

void demoFmt() {
    // fmt::format 比 std::ostringstream 更安全、更清晰
    string msg = fmt::format("Hello, {}! You are {} years old.", "Alice", 30);
    // fmt::print("Pi = {:.2f}\n", 3.14159);  // 直接输出
}

// ==========================================
// 2. spdlog —— 高性能日志库
// ==========================================
// #include <spdlog/spdlog.h>
// #include <spdlog/sinks/basic_file_sink.h>

void demoSpdlog() {
    // 控制台日志
    // spdlog::info("Server started on port {}", 8080);
    // spdlog::warn("Low disk space: {} MB", 500);
    // spdlog::error("Failed to connect: {}", err);

    // 文件日志
    // auto file = spdlog::basic_logger_mt("file", "logs/app.log");
    // file->info("User logged in: {}", user_id);
}

// ==========================================
// 3. nlohmann/json —— 现代 JSON 库
// ==========================================
// #include <nlohmann/json.hpp>
// using json = nlohmann::json;

void demoJson() {
    // // 构建 JSON
    // json j;
    // j["name"] = "Alice";
    // j["age"] = 30;
    // j["scores"] = {95, 87, 92};
    // j["address"]["city"] = "Beijing";

    // // 从字符串解析
    // json parsed = json::parse(R"({"name":"Bob","age":25})");
    // string name = parsed["name"];
    // int age = parsed["age"];

    // // 序列化
    // string str = j.dump(4);  // 带缩进
}

// ==========================================
// 4. range-v3 —— 范围库（C++20 部分采纳）
// ==========================================
// #include <range/v3/all.hpp>

void demoRange() {
    // vector<int> v = {5, 2, 8, 1, 9};
    // auto even = v | ranges::views::filter([](int n) { return n % 2 == 0; });
    // C++20 已有 std::views::filter
}

// ==========================================
// 5. Boost 常用组件
// ==========================================
// Boost.Asio     — 网络库（或独立 Asio）
// Boost.Beast    — HTTP/WebSocket
// Boost.Filesystem  — 文件系统（C++17 已标准化）
// Boost.Optional — 可选值（C++17 已标准化）
// Boost.Variant  — 类型安全联合体（C++17 已标准化）
// Boost.Any      — 任意类型值（C++17 已标准化）
// Boost.ProgramOptions — 命令行参数解析
// Boost.Leaf     — 错误处理

// ==========================================
// 6. 其他常用库
// ==========================================
// Google Benchmark  — 性能基准测试
// Catch2            — 另一流行的测试框架
// Magic Enum        — 枚举转字符串
// Taskflow          — 任务并行库
// EnTT              — ECS 框架（游戏开发）
// GLM               — 图形数学库
// Dear ImGui        — 即时模式 GUI

```

---

### 5.7 网络编程实战（Boost.Asio）

```cpp
#include <iostream>
#include <string>
#include <thread>
#include <vector>
using namespace std;

// ==========================================
// TCP 回显服务器（异步）
// ==========================================
// #include <boost/asio.hpp>
// using boost::asio::ip::tcp;

// class EchoServer {
//     tcp::acceptor acceptor;

// public:
//     EchoServer(boost::asio::io_context& io, short port)
//         : acceptor(io, tcp::endpoint(tcp::v4(), port)) {
//         doAccept();
//     }

// private:
//     void doAccept() {
//         auto socket = make_shared<tcp::socket>(acceptor.get_executor());
//         acceptor.async_accept(*socket, [this, socket](auto ec) {
//             if (!ec) {
//                 cout << "New connection from "
//                      << socket->remote_endpoint() << endl;
//                 doRead(socket);
//             }
//             doAccept();  // 继续接受新连接
//         });
//     }

//     void doRead(shared_ptr<tcp::socket> socket) {
//         auto buf = make_shared<vector<char>>(1024);
//         socket->async_read_some(boost::asio::buffer(*buf),
//             [this, socket, buf](auto ec, size_t len) {
//                 if (!ec) {
//                     cout.write(buf->data(), len);
//                     doWrite(socket, buf, len);
//                 }
//             });
//     }

//     void doWrite(shared_ptr<tcp::socket> socket,
//                  shared_ptr<vector<char>> buf, size_t len) {
//         async_write(*socket, boost::asio::buffer(*buf, len),
//             [socket, buf](auto ec, size_t) {});
//     }
// };

// int main() {
//     try {
//         boost::asio::io_context io(1);  // 单线程
//         EchoServer server(io, 8080);
//         io.run();
//     } catch (const exception& e) {
//         cerr << "Error: " << e.what() << endl;
//         return 1;
//     }
//     return 0;
// }

/*
 * 网络编程 Core Concepts：
 *
 * 同步模型（简单但阻塞）：
 *   socket → connect → send/recv → close
 *
 * 异步模型（高效但复杂）：
 *   io_context → async_accept → async_read → async_write
 *
 * 常用设计模式：
 *   每个连接一个线程（简单，但连接多时不适用）
 *   单线程事件循环 + 异步回调（高性能）
 *   线程池 + 事件循环（平衡方案）
 *
 * 协议选择：
 *   TCP：可靠有序，适合大多数应用
 *   UDP：不可靠但低延迟，适合音视频/游戏
 *   WebSocket：浏览器友好的双向通信
 *   HTTP/REST：微服务间通信
 */
```

---

### 5.8 序列化——真实项目中的数据交换

```cpp
#include <iostream>
#include <fstream>
#include <vector>
#include <cstdint>
using namespace std;

// ==========================================
// 1. 自定义二进制序列化
// ==========================================
// 适用于高性能场景（游戏网络同步、本地缓存）
struct PlayerData {
    uint32_t id;
    float x, y, z;
    float health;
    uint8_t level;

    // 序列化到缓冲区
    vector<uint8_t> serialize() const {
        vector<uint8_t> buf(sizeof(*this));
        auto* ptr = buf.data();
        memcpy(ptr, &id, sizeof(id)); ptr += sizeof(id);
        memcpy(ptr, &x, sizeof(x));   ptr += sizeof(x);
        memcpy(ptr, &y, sizeof(y));   ptr += sizeof(y);
        memcpy(ptr, &z, sizeof(z));   ptr += sizeof(z);
        memcpy(ptr, &health, sizeof(health)); ptr += sizeof(health);
        memcpy(ptr, &level, sizeof(level));
        return buf;
    }

    // 反序列化
    static PlayerData deserialize(const vector<uint8_t>& buf) {
        PlayerData p;
        const auto* ptr = buf.data();
        memcpy(&p.id, ptr, sizeof(p.id)); ptr += sizeof(p.id);
        memcpy(&p.x, ptr, sizeof(p.x));   ptr += sizeof(p.x);
        memcpy(&p.y, ptr, sizeof(p.y));   ptr += sizeof(p.y);
        memcpy(&p.z, ptr, sizeof(p.z));   ptr += sizeof(p.z);
        memcpy(&p.health, ptr, sizeof(p.health)); ptr += sizeof(p.health);
        memcpy(&p.level, ptr, sizeof(p.level));
        return p;
    }
};

// ==========================================
// 2. Protocol Buffers 风格
// ==========================================
// Proto 定义：
//   message Person {
//     string name = 1;
//     int32  age = 2;
//     repeated string tags = 3;
//   }
//
// C++ 生成代码使用：
//   Person p;
//   p.set_name("Alice");
//   p.set_age(30);
//   p.add_tags("friend");
//   string data = p.SerializeAsString();
//   Person q;
//   q.ParseFromString(data);

// ==========================================
// 3. JSON 序列化（工作中最常见）
// ==========================================
// #include <nlohmann/json.hpp>
// using json = nlohmann::json;
//
// struct Person {
//     string name;
//     int age;
//     vector<string> tags;
// };
//
// // 序列化
// json j = {{"name", "Alice"}, {"age", 30}, {"tags", {"friend", "dev"}}};
// string str = j.dump();
//
// // 反序列化
// json j2 = json::parse(str);
// Person p{j2["name"], j2["age"], j2["tags"].get<vector<string>>()};
```

---

### 5.9 线程池实现

工作中不会每次都从零写线程池，但理解其原理是并发编程的基础。

```cpp
#include <iostream>
#include <vector>
#include <queue>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <future>
#include <functional>
#include <type_traits>
using namespace std;

class ThreadPool {
private:
    vector<thread> workers;           // 工作线程
    queue<function<void()>> tasks;    // 任务队列

    mutex mtx;
    condition_variable cv;
    bool stop = false;

public:
    explicit ThreadPool(size_t numThreads) {
        // 创建 numThreads 个工作线程
        for (size_t i = 0; i < numThreads; ++i) {
            workers.emplace_back([this] {
                while (true) {
                    function<void()> task;
                    {
                        unique_lock<mutex> lock(mtx);
                        // 等待任务或停止信号
                        cv.wait(lock, [this] {
                            return stop || !tasks.empty();
                        });
                        if (stop && tasks.empty()) {
                            return;  // 线程退出
                        }
                        task = move(tasks.front());
                        tasks.pop();
                    }
                    // 执行任务（不在锁内，避免阻塞其他线程）
                    task();
                }
            });
        }
    }

    // 提交任务，返回 future 获取结果
    template <typename F, typename... Args>
    auto enqueue(F&& f, Args&&... args)
        -> future<invoke_result_t<F, Args...>>
    {
        using ReturnType = invoke_result_t<F, Args...>;

        // 将函数和参数打包为 packaged_task
        auto task = make_shared<packaged_task<ReturnType()>>(
            bind(forward<F>(f), forward<Args>(args)...)
        );

        future<ReturnType> result = task->get_future();

        {
            lock_guard<mutex> lock(mtx);
            if (stop) {
                throw runtime_error("enqueue on stopped ThreadPool");
            }
            tasks.emplace([task]() { (*task)(); });
        }
        cv.notify_one();  // 唤醒一个工作线程
        return result;
    }

    ~ThreadPool() {
        {
            lock_guard<mutex> lock(mtx);
            stop = true;
        }
        cv.notify_all();  // 唤醒所有线程
        for (auto& worker : workers) {
            worker.join();
        }
    }
};

// 使用示例
int main() {
    ThreadPool pool(4);  // 4 个线程

    // 提交任务并获取结果
    auto result1 = pool.enqueue([](int a, int b) {
        return a + b;
    }, 10, 20);

    auto result2 = pool.enqueue([](int x) {
        return x * x;
    }, 42);

    cout << "10 + 20 = " << result1.get() << endl;  // 30
    cout << "42^2 = " << result2.get() << endl;      // 1764

    // 提交无返回值任务
    pool.enqueue([] {
        cout << "任务执行中..." << endl;
    });

    // 析构时自动等待所有任务完成

    return 0;
}
```

---

### 5.10 面试常见题型

以下是 C++ 面试中最常出现的问题类型，对找工作直接相关。

```cpp
#include <iostream>
#include <string>
#include <vector>
#include <memory>
using namespace std;

// ==========================================
// 1. 虚函数与多态
// ==========================================
// 问：虚函数是如何实现的？虚函数表存在哪里？
// 答：每个有虚函数的类有一个虚函数表（vtable），
//    每个对象有一个虚指针（vptr）指向该表。
//    vtable 存在只读数据段（.rodata），
//    vptr 存在对象的内存开头。
//    动态绑定通过 vptr->vtable[index] 实现。

// ==========================================
// 2. 拷贝构造与移动语义
// ==========================================
// 问：什么时候会调用拷贝构造函数？
// 答：1) 传值参数   2) 函数返回值（RVO 可能优化掉）
//    3) 用已有对象初始化新对象

// 问：std::move 做了什么？
// 答：static_cast<T&&>(lvalue) —— 只是一个转型，没有任何运行时操作

// ==========================================
// 3. 智能指针
// ==========================================
// 问：shared_ptr 的实现原理？
// 答：引用计数 + 控制块。每次拷贝计数 +1，析构 -1，
//    减到 0 时删除对象和控制块。控制块包含引用计数、
//    弱引用计数、删除器等。

// 问：weak_ptr 如何解决循环引用？
// 答：weak_ptr 不增加引用计数，lock() 返回 shared_ptr
//    时检查对象是否还在。

// ==========================================
// 4. 内存管理
// ==========================================
// 问：new / delete 与 malloc / free 的区别？
// 答：new 调用构造函数，delete 调用析构函数。
//    malloc 只分配内存。new 失败抛出 bad_alloc，
//    malloc 返回 nullptr。

// 问：内存泄漏如何检测？
// 答：Valgrind（Linux）、AddressSanitizer（编译时加 -fsanitize=address）、
//    Visual Studio 的 CRT 调试、智能指针

// ==========================================
// 5. const 的各种用法
// ==========================================
struct ConstDemo {
    int value = 42;

    // const 成员函数：不修改成员变量
    int getValue() const { return value; }

    // mutable 成员：即使在 const 函数中也能修改
    mutable int cacheHit = 0;
    int getValueCached() const {
        ++cacheHit;  // 允许修改 mutable
        return value;
    }
};

void constDemo() {
    const int a = 10;          // 常量
    const int* p1;              // 指向常量的指针
    int* const p2 = nullptr;    // 常量指针（不能改变指向）
    const int* const p3 = nullptr; // 指向常量的常量指针

    const ConstDemo obj;
    obj.getValue();            // 只能调 const 成员函数
}

// ==========================================
// 6. 关键字与语法
// ==========================================
// 问：static 关键字的用途？
// 答：1) 静态局部变量（生命周期为程序运行期）
//    2) 静态全局变量/函数（文件作用域，仅本文件可见）
//    3) 静态成员变量（属于类，所有对象共享）
//    4) 静态成员函数（属于类，没有 this，只能访问静态成员）

// 问：extern "C" 的作用？
// 答：告诉编译器以 C 的方式链接（不进行名字改编 name mangling），
//    用于 C 和 C++ 混编。

// 问：volatile 的作用？
// 答：告诉编译器不要优化对该变量的访问，每次从内存读取。
//    用于：1) 硬件寄存器 2) 信号处理 3) 多线程（但优先用 atomic）

// ==========================================
// 7. RAII
// ==========================================
// 问：RAII 的好处？
// 答：1) 异常安全 2) 资源不泄漏 3) 代码简洁
//    例子：lock_guard、unique_ptr、fstream 等

// ==========================================
// 8. 模板相关问题
// ==========================================
// 问：模板特化与偏特化的区别？
// 答：全特化：为特定类型完全定制
//    偏特化：为某种模式（如指针类型）定制
//    函数模板不支持偏特化

// 问：enable_if 和 Concept 的区别？
// 答：enable_if（C++11）用 SFINAE 实现，可读性差
//    Concept（C++20）更简洁、错误信息更好

// ==========================================
// 9. 算法与数据结构
// ==========================================
// 工作中常考但不限于：
// - vector 扩容机制（capacity 翻倍增长，均摊 O(1)）
// - map vs unordered_map 的选择
// - sort 的实现（IntroSort = QuickSort + HeapSort + InsertionSort）
// - 迭代器失效（vector 插入/删除后迭代器可能失效）

// ==========================================
// 10. 系统设计题（高级岗位）
// ==========================================
// 设计一个线程池（见 5.9 节）
// 设计一个内存池
// 设计一个日志系统
// 设计一个对象池（避免频繁 new/delete）

int main() {
    cout << "面试准备要点：" << endl;
    cout << "1. 手撕代码：LeetCode 中等难度" << endl;
    cout << "2. C++ 基础：虚函数、const、static、智能指针" << endl;
    cout << "3. 内存模型：栈/堆、RAII、内存对齐" << endl;
    cout << "4. 并发基础：线程安全、死锁、原子操作" << endl;
    cout << "5. 项目经验：STL 使用、CMake、测试" << endl;
    cout << "6. 系统设计：线程池、内存池、日志系统" << endl;
    return 0;
}
```

---

### 5.11 编码规范与 Code Review 检查清单

```cpp
// ==========================================
// 1. 命名规范（Google C++ Style）
// ==========================================
// 文件名：       my_class.cpp, my_class.h
// 类型名：       PascalCase:  MyClass, MyStruct
// 变量名：       snake_case: my_variable
// 成员变量：     snake_case_（加下划线后缀）
// 常量：         kPascalCase: kMaxSize
// 函数名：       PascalCase: DoSomething()
// 宏（尽量少用）：  MY_MACRO
// 命名空间：     snake_case: my_namespace

// ==========================================
// 2. Code Review 检查清单
// ==========================================
//
// [正确性]
//   □ 是否处理了边界条件？
//   □ 异常安全性：RAII 是否用了？异常会不会泄漏资源？
//   □ 多线程安全：共享数据是否有锁保护？
//   □ 是否有数据竞争或死锁风险？
//
// [可读性]
//   □ 变量/函数命名是否表达了意图？
//   □ 函数是否足够小（不超过 40 行）？
//   □ 注释是否解释了"为什么"而非"是什么"？
//   □ 是否有不必要的注释（代码本身就能说明的）？
//
// [性能]
//   □ 不必要的拷贝？（传值 vs 传引用，emplace_back vs push_back）
//   □ 是否预分配了足够的容量？（reserve）
//   □ 热路径上是否有虚函数调用？
//   □ 是否有不必要的动态内存分配？
//
// [可维护性]
//   □ 是否遵循了现有代码的约定和模式？
//   □ 是否添加了相应的单元测试？
//   □ 是否有硬编码的魔数？（应该用 const/constexpr）
//   □ API 是否清晰且最小化？
//
// [安全]
//   □ 是否检查了外部输入（边界、类型、范围）？
//   □ 是否存在可能的整数溢出？
//   □ 指针解引用前是否检查了 nullptr？
//   □ 是否在头文件中使用了 using namespace std; ？
//
// ==========================================
// 3. 常见代码异味
// ==========================================
// 异味 1: 巨型函数（>100 行）→ 拆分为多个小函数
// 异味 2: 重复代码 → 提取为函数
// 异味 3: 深层嵌套 → 提前 return 或拆函数
// 异味 4: 裸 new/delete → 用智能指针或容器
// 异味 5: 头文件包含过多 → 用前置声明
// 异味 6: 魔法数字 → 用命名常量

// 坏例子
void bad() {
    int* p = new int(42);
    // ... 可能提前 return 或抛出异常 ...
    delete p;  // 容易泄漏！
}

// 好例子
void good() {
    auto p = make_unique<int>(42);
    // ... 安全，自动释放 ...
}

// ==========================================
// 4. 头文件规范
// ==========================================
// 好头文件的标准：
// 1. 有 #pragma once 或头文件保护宏
// 2. 最小化 #include（尽量用前置声明）
// 3. 永远不在头文件中 using namespace std;
// 4. 公有 API 放在 include/，私有放在 src/
// 5. 接口尽可能稳定（Pimpl 惯用法）
```

---

### 5.12 从学习到工作的最后建议

```cpp
/*
 * ==========================================
 * 1. 学习完本文档后，你应该能：
 * ==========================================
 * ✓ 独立编写 C++ 程序（基础到中等等级）
 * ✓ 使用 CMake 组织多文件项目
 * ✓ 编写单元测试（Google Test）
 * ✓ 使用智能指针管理动态内存
 * ✓ 使用 STL 容器和算法
 * ✓ 理解并应用 RAII 和移动语义
 * ✓ 使用 C++11/14/17/20 现代特性
 * ✓ 编写基本的并发程序
 * ✓ 使用 git 进行版本控制
 * ✓ 使用调试工具定位问题
 *
 * ==========================================
 * 2. 但仍需在实战中学习的：
 * ==========================================
 * △ 大型项目的代码阅读与维护
 * △ 特定领域的专业知识（图形、网络、嵌入式等）
 * △ 持续集成/持续部署（CI/CD）
 * △ 代码审查技巧
 * △ 性能调优经验
 * △ 与团队协作的工程流程
 *
 * ==========================================
 * 3. 建议的实战练习项目
 * ==========================================
 * [初级]
 *   - 实现一个 JSON 解析器
 *   - 实现一个文本编辑器（控制台）
 *   - 实现一个 Markdown 到 HTML 转换器
 *
 * [中级]
 *   - 实现一个 HTTP 服务器
 *   - 实现一个简单的游戏（如俄罗斯方块、贪吃蛇）
 *   - 实现一个内存池/对象池
 *   - 为开源库贡献一个小测试或文档
 *
 * [高级]
 *   - 参与知名 C++ 开源项目（LLVM、Boost、fmt、spdlog）
 *   - 实现一个简单的游戏引擎
 *   - 实现一个高性能网络库
 *
 * ==========================================
 * 4. 求职准备清单
 * ==========================================
 * [简历]
 *   ☐ 列出 2-3 个 C++ 项目，突出你的职责和成果
 *   ☐ 提及使用的 C++ 标准、工具链、第三方库
 *   ☐ 如果参与过开源项目，一定要写上
 *
 * [面试]
 *   ☐ 复习本文档 5.10 节的所有知识点
 *   ☐ LeetCode 刷题（中等难度 50 题左右）
 *   ☐ 准备一个你最有信心的项目详细讲解
 *   ☐ 准备系统设计问题的思考框架
 *
 * [主动学习]
 *   ☐ 关注 isocpp.org 了解最新标准进展
 *   ☐ 阅读 C++ 社区博客（Herb Sutter、Andrei Alexandrescu 等）
 *   ☐ 参加 C++ 会议（CppCon、Meeting C++）或看录像
 *   ☐ 在公司积极要求 Code Review，是最好的学习途径
 */
```

---

> **最终总结：** 仅仅阅读文档无法让你成为专家。真正的成长来自于**大量编写代码**、**阅读高质量的代码**（LLVM、Boost、STL 源码）、以及**接受严格的代码审查**。
>
> 本文档的目标是为你提供一张完整的知识地图，告诉你"有哪些东西需要学"以及"它们是如何工作的"。当你遇到具体问题时，知道去哪里查（cppreference、Google）、知道用什么工具（AddressSanitizer、perf、GDB）。
>
> 记住：C++ 的学习没有终点。即使 20 多年的专家，每次新标准发布仍然有新的东西可以学习。保持谦逊、保持好奇，写安全的、可读的、高效的代码。祝你学习顺利，早日拿到心仪的 Offer！


