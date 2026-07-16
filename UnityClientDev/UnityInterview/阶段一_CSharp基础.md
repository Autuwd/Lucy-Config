# C# 编程基础与进阶完全指南

> 本文档系统整理 C#/.NET 开发所需掌握的知识点，分为基础篇和进阶篇
> 每个知识点都包含概念讲解、示例说明、习题练习及答案

---

# 基础篇

## 目录

- [1.1 变量与数据类型](#11-变量与数据类型)
- [1.2 运算符](#12-运算符)
- [1.3 条件语句](#13-条件语句)
- [1.4 循环语句](#14-循环语句)
- [1.5 方法](#15-方法)
- [1.6 数组](#16-数组)
- [1.7 集合](#17-集合)
- [1.8 类与对象](#18-类与对象)
- [1.9 构造函数](#19-构造函数)
- [1.10 访问修饰符与属性](#110-访问修饰符与属性)
- [1.11 静态成员](#111-静态成员)
- [1.12 封装](#112-封装)

---

## 1.1 变量与数据类型

### 概念讲解

**变量**是存储数据的容器，每个变量都有三个要素：

1. **数据类型** - 决定变量能存储什么数据、占用多大内存
2. **变量名** - 用于程序中访问该变量的标识符
3. **变量值** - 存储的具体数据

```
内存中的变量：
┌─────────────┐
│ 变量名: age │
│ 类型: int   │
│ 值: 25      │
│ 地址: 0x10  │
└─────────────┘
```

**数据类型**分为两大类：

| 类型 | 说明 | 存储位置 | 内存分配 |
|------|------|----------|----------|
| 值类型 | 直接存储数据 | 栈 | 小而固定 |
| 引用类型 | 存储数据的引用（地址） | 栈（引用）+ 堆（数据） | 大而可变 |

**常见值类型**：int、float、double、decimal、bool、char、枚举、结构体

**常见引用类型**：string、数组、类、接口、委托

### 举例说明

```csharp
// ===== 值类型示例 =====
int age = 25;              // 整数
float height = 1.75f;      // 单精度浮点数（必须加f后缀）
double weight = 65.5;     // 双精度浮点数
decimal salary = 5000m;   // 精确小数（金融计算用）
bool isStudent = true;    // 布尔值
char grade = 'A';         // 单个字符

// ===== 引用类型示例 =====
string name = "张三";     // 字符串
int[] numbers = {1, 2, 3}; // 数组

// ===== 值类型赋值（复制值） =====
int a = 10;
int b = a;    // b 复制了 a 的值
b = 20;       // 修改 b
Console.WriteLine($"a = {a}, b = {b}");  // a = 10, b = 20（互不影响）

// ===== 引用类型赋值（复制引用） =====
int[] arr1 = {1, 2, 3};
int[] arr2 = arr1;    // arr2 指向 arr1 的同一内存地址
arr2[0] = 100;        // 修改 arr2 的元素
Console.WriteLine($"arr1[0] = {arr1[0]}");  // arr1[0] = 100（互相影响）
```

**常见数据类型一览**：

| 类型 | 中文名 | 占用 | 取值范围 | 示例 |
|------|--------|------|----------|------|
| int | 整数 | 4字节 | -21亿 ~ 21亿 | 100, -50 |
| long | 长整数 | 8字节 | -9e18 ~ 9e18 | 100000L |
| float | 单精度浮点 | 4字节 | ±3.4e38 | 3.14f |
| double | 双精度浮点 | 8字节 | ±1.7e308 | 3.14159 |
| decimal | 精确小数 | 16字节 | ±7.9e28 | 99.99m |
| bool | 布尔 | 1字节 | true/false | true |
| char | 字符 | 2字节 | Unicode字符 | 'A', '中' |
| string | 字符串 | 变长 | 文本 | "Hello" |

### 习题练习

#### 练习 1.1.1
1. 声明一个 int 类型变量 age，赋值为 20
2. 声明一个 double 类型变量 score，赋值为 95.5
3. 声明一个 bool 类型变量 isPassed，赋值为 true
4. 声明一个 char 类型变量 level，赋值为 'A'

#### 练习 1.1.2
分析以下代码的输出结果，并说明原因：

```csharp
int x = 5;
int y = x;
y = 10;
Console.WriteLine($"x = {x}, y = {y}");

int[] arr1 = {1, 2, 3};
int[] arr2 = arr1;
arr2[0] = 100;
Console.WriteLine($"arr1[0] = {arr1[0]}, arr2[0] = {arr2[0]}");
```

#### 练习 1.1.3
选择合适的数据类型：
1. 存储学生的年龄
2. 存储商品的价格（需要精确计算）
3. 存储一个人的姓名
4. 存储是否登录的状态

### 练习讲解

#### 练习 1.1.1 答案

```csharp
int age = 20;
double score = 95.5;
bool isPassed = true;
char level = 'A';
```

#### 练习 1.1.2 答案

```
第一段输出：x = 5, y = 10
原因：int 是值类型，y = x 时复制了值，修改 y 不影响 x

第二段输出：arr1[0] = 100, arr2[0] = 100
原因：数组是引用类型，arr2 = arr1 让两个变量指向同一内存
      修改 arr2[0] 也就是修改了 arr1[0] 指向的数据
```

#### 练习 1.1.3 答案

1. **学生年龄**：int（年龄通常用整数表示）
2. **商品价格**：decimal（价格需要精确计算，避免浮点误差）
3. **姓名**：string（文本内容）
4. **登录状态**：bool（只有两种状态）

### 相关知识点

- 1.2 运算符
- 1.6 数组
- 1.7 集合
- 2.14 装箱与拆箱
- 2.23 ref/out/in 参数

---

## 1.2 运算符

### 概念讲解

**运算符**是用于执行操作的符号，C# 中的运算符可分为以下几类：

| 类别 | 运算符 | 说明 |
|------|--------|------|
| 算术运算符 | +、-、*、/、% | 加减乘除取余 |
| 赋值运算符 | =、+=、-=、*=、/=、%= | 赋值与复合赋值 |
| 比较运算符 | ==、!=、>、<、>=、<= | 返回 bool |
| 逻辑运算符 | &&、\|\|、! | 与或非 |
| 位运算符 | &、\|、^、~、<<、>> | 按位操作 |
| 三元运算符 | condition ? true : false | 条件表达式 |

**运算符优先级**：先乘除取余，后加减，有括号先算括号

### 举例说明

```csharp
// ===== 算术运算符 =====
int a = 10, b = 3;
Console.WriteLine($"a + b = {a + b}");   // 13
Console.WriteLine($"a - b = {a - b}");   // 7
Console.WriteLine($"a * b = {a * b}");   // 30
Console.WriteLine($"a / b = {a / b}");   // 3（整数除法取整）
Console.WriteLine($"a % b = {a % b}");   // 1（余数）

// 浮点除法
double c = 10.0, d = 3.0;
Console.WriteLine($"c / d = {c / d}");   // 3.333...

// ===== 自增自减 =====
int i = 5;
Console.WriteLine($"i++ = {i++}");  // 输出5，然后 i 变为6（后置）
Console.WriteLine($"i = {i}");      // 6
Console.WriteLine($"++i = {++i}");  // i 先变为7，然后输出7（前置）

int j = 5;
Console.WriteLine($"j-- = {j--}");  // 输出5，然后 j 变为4
Console.WriteLine($"--j = {--j}");  // j 先变为3，然后输出3

// ===== 赋值运算符 =====
int x = 10;
x += 5;   // x = x + 5 -> 15
x -= 3;   // x = x - 3 -> 12
x *= 2;   // x = x * 2 -> 24
x /= 4;   // x = x / 4 -> 6
x %= 5;   // x = x % 5 -> 1

// ===== 比较运算符 =====
int p = 5, q = 10;
Console.WriteLine($"p == q: {p == q}");   // false
Console.WriteLine($"p != q: {p != q}");   // true
Console.WriteLine($"p > q: {p > q}");    // false
Console.WriteLine($"p < q: {p < q}");    // true
Console.WriteLine($"p >= q: {p >= q}");  // false
Console.WriteLine($"p <= q: {p <= q}");   // true

// ===== 逻辑运算符 =====
bool m = true, n = false;
Console.WriteLine($"m && n: {m && n}");   // false（两者都为true才为true）
Console.WriteLine($"m || n: {m || n}");   // true（任一为true就为true）
Console.WriteLine($"!m: {!m}");          // false（取反）

// ===== 三元运算符 =====
int score = 85;
string result = score >= 60 ? "及格" : "不及格";
Console.WriteLine(result);  // 及格
```

### 习题练习

#### 练习 1.2.1
计算以下表达式的值：
1. `10 + 5 * 2`
2. `(10 + 5) * 2`
3. `17 % 5`
4. `10 / 3`（整数除法）
5. `10.0 / 3`（浮点除法）

#### 练习 1.2.2
写出以下代码的输出：
```csharp
int a = 3;
int b = 5;
Console.WriteLine(a++ * b);
Console.WriteLine(a);
```

#### 练习 1.2.3
使用三元运算符判断：如果成绩 >= 60 显示"及格"，否则显示"不及格"

#### 练习 1.2.4
判断以下表达式的结果：
```csharp
bool result = (5 > 3) && (10 < 8) || !(2 == 2);
```

### 练习讲解

#### 练习 1.2.1 答案

1. `10 + 5 * 2 = 10 + 10 = 20`（乘法优先）
2. `(10 + 5) * 2 = 15 * 2 = 30`
3. `17 % 5 = 2`（17除5余2）
4. `10 / 3 = 3`（整数除法取整）
5. `10.0 / 3 = 3.333...`

#### 练习 1.2.2 答案

```
输出：15（先使用 a 的值3乘以5，然后 a 自增变为4）
输出：4
```

#### 练习 1.2.3 答案

```csharp
int score = 75;
string result = score >= 60 ? "及格" : "不及格";
Console.WriteLine(result);  // 及格
```

#### 练习 1.2.4 答案

```
(5 > 3) = true
(10 < 8) = false
!(2 == 2) = !true = false

true && false = false
false || false = false

最终结果：false
```

### 相关知识点

- 1.1 变量与数据类型
- 1.3 条件语句
- 1.4 循环语句

---

## 1.3 条件语句

### 概念讲解

**条件语句**用于根据条件选择执行不同的代码路径。

| 语句类型 | 说明 |
|----------|------|
| if...else | 单条件或多条件分支 |
| switch | 多值匹配分支（C# 7+ 支持模式匹配） |
| 三元运算符 | 简单的条件选择 |

### 举例说明

```csharp
// ===== if 语句 =====
int score = 85;

if (score >= 90)
{
    Console.WriteLine("优秀");
}
else if (score >= 80)
{
    Console.WriteLine("良好");
}
else if (score >= 60)
{
    Console.WriteLine("及格");
}
else
{
    Console.WriteLine("不及格");
}

// ===== 三元运算符 =====
string grade = score >= 60 ? "及格" : "不及格";

// ===== switch 语句 =====
int day = 3;
string dayName;

switch (day)
{
    case 1:
        dayName = "星期一";
        break;
    case 2:
        dayName = "星期二";
        break;
    case 3:
        dayName = "星期三";
        break;
    case 4:
        dayName = "星期四";
        break;
    case 5:
        dayName = "星期五";
        break;
    default:
        dayName = "周末";
        break;
}
Console.WriteLine(dayName);  // 星期三

// ===== switch 表达式（C# 8.0+） =====
string dayName2 = day switch
{
    1 => "星期一",
    2 => "星期二",
    3 => "星期三",
    4 => "星期四",
    5 => "星期五",
    _ => "周末"
};

// ===== 模式匹配（C# 7+） =====
object obj = 42;
switch (obj)
{
    case int i when i > 0:
        Console.WriteLine($"正整数: {i}");
        break;
    case int i when i < 0:
        Console.WriteLine($"负整数: {i}");
        break;
    case string s:
        Console.WriteLine($"字符串: {s}");
        break;
    case null:
        Console.WriteLine("空值");
        break;
}
```

### 习题练习

#### 练习 1.3.1
使用 if...else 判断成绩等级：
- >=90: 优秀
- >=80: 良好
- >=70: 中等
- >=60: 及格
- <60: 不及格

#### 练习 1.3.2
使用 switch 判断星期几（1-7）

#### 练习 1.3.3
判断一个数是正数、负数还是零

#### 练习 1.3.4
使用三元运算符判断年龄是否成年（>=18）

### 练习讲解

#### 练习 1.3.1 答案

```csharp
int score = 85;

if (score >= 90)
{
    Console.WriteLine("优秀");
}
else if (score >= 80)
{
    Console.WriteLine("良好");
}
else if (score >= 70)
{
    Console.WriteLine("中等");
}
else if (score >= 60)
{
    Console.WriteLine("及格");
}
else
{
    Console.WriteLine("不及格");
}
```

#### 练习 1.3.2 答案

```csharp
int day = 4;
switch (day)
{
    case 1: Console.WriteLine("星期一"); break;
    case 2: Console.WriteLine("星期二"); break;
    case 3: Console.WriteLine("星期三"); break;
    case 4: Console.WriteLine("星期四"); break;
    case 5: Console.WriteLine("星期五"); break;
    case 6:
    case 7: Console.WriteLine("周末"); break;
    default: Console.WriteLine("无效"); break;
}
```

#### 练习 1.3.3 答案

```csharp
int num = -5;

if (num > 0)
{
    Console.WriteLine("正数");
}
else if (num < 0)
{
    Console.WriteLine("负数");
}
else
{
    Console.WriteLine("零");
}
```

#### 练习 1.3.4 答案

```csharp
int age = 20;
string result = age >= 18 ? "成年" : "未成年";
Console.WriteLine(result);  // 成年
```

### 相关知识点

- 1.2 运算符
- 1.4 循环语句

---

## 1.4 循环语句

### 概念讲解

**循环语句**用于重复执行一段代码。

| 循环类型 | 特点 | 适用场景 |
|----------|------|----------|
| for | 知道循环次数 | 固定次数循环 |
| while | 先判断后执行 | 条件驱动循环 |
| do...while | 先执行后判断 | 至少执行一次 |
| foreach | 遍历集合/数组 | 遍历容器元素 |

**跳转语句**：
- `break` - 跳出整个循环
- `continue` - 跳过本次循环，继续下一次

### 举例说明

```csharp
// ===== for 循环 =====
for (int i = 0; i < 5; i++)
{
    Console.WriteLine(i);  // 输出 0,1,2,3,4
}

// 遍历数组
int[] numbers = {1, 2, 3, 4, 5};
for (int i = 0; i < numbers.Length; i++)
{
    Console.WriteLine(numbers[i]);
}

// ===== while 循环 =====
int count = 0;
while (count < 5)
{
    Console.WriteLine(count);  // 0,1,2,3,4
    count++;
}

// ===== do...while 循环 =====
int num = 0;
do
{
    Console.WriteLine(num);  // 至少执行一次
    num++;
} while (num < 5);

// ===== foreach 循环 =====
string[] names = {"张三", "李四", "王五"};
foreach (string name in names)
{
    Console.WriteLine(name);
}

// ===== 跳转语句 =====
for (int i = 0; i < 10; i++)
{
    if (i == 5)
        break;  // 跳出整个循环，只输出 0,1,2,3,4
    Console.WriteLine(i);
}

for (int i = 0; i < 5; i++)
{
    if (i == 2)
        continue;  // 跳过 i==2，输出 0,1,3,4
    Console.WriteLine(i);
}
```

### 习题练习

#### 练习 1.4.1
使用 for 循环计算 1 到 100 的和

#### 练习 1.4.2
使用 while 循环输出 10 到 1 的整数

#### 练习 1.4.3
使用 foreach 遍历数组 {1, 3, 5, 7, 9} 并计算元素之和

#### 练习 1.4.4
使用 for 循环输出 1-20 的整数，但跳过 5 的倍数

#### 练习 1.4.5
使用 do...while 实现一个简单的猜数字游戏（随机生成1-10，用户输入直到猜对）

### 练习讲解

#### 练习 1.4.1 答案

```csharp
int sum = 0;
for (int i = 1; i <= 100; i++)
{
    sum += i;
}
Console.WriteLine($"1到100的和: {sum}");  // 5050
```

#### 练习 1.4.2 答案

```csharp
int num = 10;
while (num >= 1)
{
    Console.WriteLine(num);
    num--;
}
```

#### 练习 1.4.3 答案

```csharp
int[] arr = {1, 3, 5, 7, 9};
int sum = 0;
foreach (int n in arr)
{
    sum += n;
}
Console.WriteLine($"和: {sum}");  // 25
```

#### 练习 1.4.4 答案

```csharp
for (int i = 1; i <= 20; i++)
{
    if (i % 5 == 0)
        continue;  // 跳过5的倍数
    Console.WriteLine(i);
}
```

#### 练习 1.4.5 答案

```csharp
Random random = new Random();
int target = random.Next(1, 11);
int guess;

do
{
    Console.Write("请输入猜测(1-10): ");
    guess = int.Parse(Console.ReadLine());
    
    if (guess < target)
        Console.WriteLine("太小了！");
    else if (guess > target)
        Console.WriteLine("太大了！");
} while (guess != target);

Console.WriteLine($"恭喜猜对了！答案是 {target}");
```

### 相关知识点

- 1.2 运算符
- 1.6 数组
- 1.7 集合

---

## 1.5 方法

### 概念讲解

**方法（函数）**是组织代码的基本单元，用于实现特定功能。

**方法组成**：
- 返回类型 - 方法返回的数据类型（void 表示无返回值）
- 方法名 - 用于调用的标识符
- 参数列表 - 输入数据
- 方法体 - 执行的代码
- return 语句 - 返回结果

**参数传递方式**：
- 值传递 - 传递数据的副本
- ref - 传递引用，修改影响原变量
- out - 输出参数，方法内必须赋值
- params - 可变参数

### 举例说明

```csharp
// ===== 无返回值方法 =====
void SayHello()
{
    Console.WriteLine("你好！");
}

SayHello();  // 调用

// ===== 有返回值方法 =====
int Add(int a, int b)
{
    return a + b;
}

int result = Add(3, 5);  // result = 8

// ===== 带默认参数的方法 =====
void Greet(string name, string prefix = "你好")
{
    Console.WriteLine($"{prefix}，{name}！");
}

Greet("张三");        // 你好，张三！
Greet("李四", "欢迎"); // 欢迎，李四！

// ===== ref 参数 =====
void DoubleValue(ref int value)
{
    value *= 2;
}

int num = 5;
DoubleValue(ref num);
Console.WriteLine(num);  // 10

// ===== out 参数 =====
void GetMinMax(int[] arr, out int min, out int max)
{
    min = arr[0];
    max = arr[0];
    foreach (int n in arr)
    {
        if (n < min) min = n;
        if (n > max) max = n;
    }
}

int[] numbers = {3, 1, 4, 1, 5, 9, 2, 6};
GetMinMax(numbers, out int min, out int max);
Console.WriteLine($"最小: {min}, 最大: {max}");  // 最小: 1, 最大: 9

// ===== params 可变参数 =====
int Sum(params int[] values)
{
    int total = 0;
    foreach (int v in values)
        total += v;
    return total;
}

Console.WriteLine(Sum(1, 2, 3));      // 6
Console.WriteLine(Sum(1, 2, 3, 4, 5)); // 15

// ===== 方法重载 =====
int Add(int a, int b) => a + b;
double Add(double a, double b) => a + b;
int Add(int a, int b, int c) => a + b + c;

Console.WriteLine(Add(1, 2));      // 3
Console.WriteLine(Add(1.5, 2.5)); // 4.0
Console.WriteLine(Add(1, 2, 3));   // 6

// ===== 递归方法 =====
int Factorial(int n)
{
    if (n <= 1)
        return 1;
    return n * Factorial(n - 1);
}

Console.WriteLine(Factorial(5));  // 120

// ===== 局部函数（C# 7.0+） =====
void Process()
{
    int Add(int a, int b) => a + b;
    Console.WriteLine(Add(3, 5));  // 8
}
```

### 习题练习

#### 练习 1.5.1
写一个方法判断一个数是否为偶数

#### 练习 1.5.2
写一个方法计算两个数的最大值

#### 练习 1.5.3
写一个方法用递归计算斐波那契数列第 n 项（1,1,2,3,5,8...）

#### 练习 1.5.4
写一个方法使用 ref 交换两个数的值

#### 练习 1.5.5
使用方法重载实现：计算两个整数、三个整数、两个小数的加法

### 练习讲解

#### 练习 1.5.1 答案

```csharp
bool IsEven(int n)
{
    return n % 2 == 0;
}

Console.WriteLine(IsEven(4));  // true
Console.WriteLine(IsEven(5));  // false
```

#### 练习 1.5.2 答案

```csharp
int Max(int a, int b)
{
    return a > b ? a : b;
}

// 或使用内置方法
int max = Math.Max(a, b);
```

#### 练习 1.5.3 答案

```csharp
int Fibonacci(int n)
{
    if (n <= 2)
        return 1;
    return Fibonacci(n - 1) + Fibonacci(n - 2);
}

for (int i = 1; i <= 10; i++)
{
    Console.Write(Fibonacci(i) + " ");  // 1 1 2 3 5 8 13 21 34 55
}
```

#### 练习 1.5.4 答案

```csharp
void Swap(ref int a, ref int b)
{
    int temp = a;
    a = b;
    b = temp;
}

int x = 5, y = 10;
Swap(ref x, ref y);
Console.WriteLine($"x = {x}, y = {y}");  // x = 10, y = 5
```

#### 练习 1.5.5 答案

```csharp
int Add(int a, int b) => a + b;
int Add(int a, int b, int c) => a + b + c;
double Add(double a, double b) => a + b;

Console.WriteLine(Add(1, 2));      // 3
Console.WriteLine(Add(1, 2, 3));   // 6
Console.WriteLine(Add(1.5, 2.5)); // 4.0
```

### 相关知识点

- 1.1 变量与数据类型
- 1.2 运算符
- 2.7 委托与 Lambda

---

## 1.6 数组

### 概念讲解

**数组**是存储固定数量同类型元素的数据结构。

| 数组类型 | 说明 |
|----------|------|
| 一维数组 | 单行元素 |
| 多维数组 | 多行多列（矩阵） |
| 锯齿数组 | 每行长度不同 |

**特点**：
- 长度固定，创建后不能改变
- 元素类型必须一致
- 索引从 0 开始

### 举例说明

```csharp
// ===== 一维数组 =====
int[] nums = new int[5];  // 长度5，初始值为0
int[] scores = {90, 85, 78, 92, 88};  // 初始化赋值
string[] names = new string[] {"张三", "李四", "王五"};

// 访问元素
Console.WriteLine(scores[0]);  // 90
scores[2] = 80;              // 修改第三个元素

// 遍历
for (int i = 0; i < scores.Length; i++)
{
    Console.WriteLine(scores[i]);
}

foreach (int score in scores)
{
    Console.WriteLine(score);
}

// ===== 二维数组 =====
int[,] matrix = new int[3, 3];  // 3x3 矩阵
int[,] grid = {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};

Console.WriteLine(grid[0, 0]);  // 1
grid[1, 2] = 10;              // 修改元素

// 遍历二维数组
for (int i = 0; i < 3; i++)
{
    for (int j = 0; j < 3; j++)
    {
        Console.Write(grid[i, j] + " ");
    }
    Console.WriteLine();
}

// ===== 锯齿数组 =====
int[][] jagged = new int[3][];
jagged[0] = new int[] {1, 2};        // 第一行2个元素
jagged[1] = new int[] {3, 4, 5};     // 第二行3个元素
jagged[2] = new int[] {6};           // 第三行1个元素

Console.WriteLine(jagged[1][2]);  // 5
```

### Array 类静态方法

```csharp
int[] nums = {5, 2, 8, 1, 9, 3};

Array.Sort(nums);       // 排序: 1,2,3,5,8,9
Array.Reverse(nums);    // 反转: 9,8,5,3,2,1
int index = Array.IndexOf(nums, 5);  // 查找索引: 2
int lastIndex = Array.LastIndexOf(nums, 5);  // 最后一次出现位置

int[] arr = new int[5];
Array.Fill(arr, 10);    // 填充: 10,10,10,10,10

int[] source = {1, 2, 3, 4, 5};
int[] dest = new int[3];
Array.Copy(source, dest, 3);  // 复制前3个元素到dest

// 判断
bool exists = Array.Exists(nums, n => n > 5);  // 是否存在>5的元素
int[] filtered = Array.FindAll(nums, n => n > 5);  // 找出所有>5的元素
```

### 习题练习

#### 练习 1.6.1
创建一个整型数组 {1, 2, 3, 4, 5}，计算所有元素的和与平均值

#### 练习 1.6.2
创建一个整型数组 {5, 2, 8, 1, 9}，使用 Array 类方法进行排序和反转

#### 练习 1.6.3
找出数组 {3, 7, 2, 9, 1} 中的最大值和最小值

#### 练习 1.6.4
创建一个 3x3 的二维数组，填充 1-9 的数字，并打印出来

#### 练习 1.6.5
使用锯齿数组存储三门课程的成绩，计算每门课程的平均分

### 练习讲解

#### 练习 1.6.1 答案

```csharp
int[] arr = {1, 2, 3, 4, 5};
int sum = 0;
foreach (int n in arr)
{
    sum += n;
}
double avg = sum / (double)arr.Length;
Console.WriteLine($"和: {sum}, 平均值: {avg}");  // 和: 15, 平均值: 3
```

#### 练习 1.6.2 答案

```csharp
int[] nums = {5, 2, 8, 1, 9};
Console.WriteLine("原数组: " + string.Join(",", nums));

Array.Sort(nums);
Console.WriteLine("排序后: " + string.Join(",", nums));  // 1,2,5,8,9

Array.Reverse(nums);
Console.WriteLine("反转后: " + string.Join(",", nums));  // 9,8,5,2,1
```

#### 练习 1.6.3 答案

```csharp
int[] arr = {3, 7, 2, 9, 1};
int max = arr[0];
int min = arr[0];

foreach (int n in arr)
{
    if (n > max) max = n;
    if (n < min) min = n;
}

Console.WriteLine($"最大值: {max}, 最小值: {min}");  // 最大: 9, 最小: 1
```

#### 练习 1.6.4 答案

```csharp
int[,] matrix = new int[3, 3];
int num = 1;

for (int i = 0; i < 3; i++)
{
    for (int j = 0; j < 3; j++)
    {
        matrix[i, j] = num++;
    }
}

for (int i = 0; i < 3; i++)
{
    for (int j = 0; j < 3; j++)
    {
        Console.Write(matrix[i, j] + " ");
    }
    Console.WriteLine();
}
```

#### 练习 1.6.5 答案

```csharp
int[][] scores = new int[][]
{
    new int[] {85, 90, 78},  // 课程1
    new int[] {92, 88, 95},  // 课程2
    new int[] {70, 75, 80}   // 课程3
};

for (int i = 0; i < scores.Length; i++)
{
    int sum = 0;
    foreach (int score in scores[i])
    {
        sum += score;
    }
    double avg = sum / (double)scores[i].Length;
    Console.WriteLine($"课程{i + 1}平均分: {avg:F2}");
}
```

### 相关知识点

- 1.1 变量与数据类型
- 1.4 循环语句
- 1.7 集合（动态数组）

---

## 1.7 集合

### 概念讲解

**集合**是动态大小的数据结构，可以根据需要添加或删除元素。

| 集合类型 | 特点 | 适用场景 |
|----------|------|----------|
| List<T> | 动态数组 | 顺序存储、随机访问 |
| Dictionary<TKey, TValue> | 键值对 | 快速查找 |
| Queue<T> | 先进先出 | 任务队列 |
| Stack<T> | 后进先出 | 撤销操作 |
| HashSet<T> | 不重复集合 | 去重 |

### 举例说明

```csharp
// ===== List<T> =====
List<int> numbers = new List<int>();
List<string> names = new List<string> {"张三", "李四"};

// 添加
numbers.Add(1);
numbers.Add(2);
numbers.AddRange(new int[] {3, 4, 5});

// 访问
int first = numbers[0];
int count = numbers.Count;

// 修改
numbers[0] = 100;

// 插入删除
numbers.Insert(1, 50);  // 在索引1插入
numbers.Remove(100);     // 删除第一个匹配项
numbers.RemoveAt(0);    // 删除指定索引
numbers.Clear();        // 清空

// 查找
bool exists = names.Contains("张三");
int index = names.IndexOf("李四");

// 排序
numbers.Sort();

// ===== Dictionary<TKey, TValue> =====
Dictionary<string, int> ages = new Dictionary<string, int>();
ages["张三"] = 25;
ages.Add("李四", 30);

// 访问
int age = ages["张三"];

// 查找
if (ages.ContainsKey("张三"))
    Console.WriteLine(ages["张三"]);

if (ages.ContainsValue(25))
    Console.WriteLine("有25岁的人");

// 遍历
foreach (var item in ages)
{
    Console.WriteLine($"{item.Key}: {item.Value}");
}

// 删除
ages.Remove("张三");
ages.Clear();

// ===== Queue<T> =====
Queue<string> queue = new Queue<string>();
queue.Enqueue("任务1");
queue.Enqueue("任务2");
queue.Enqueue("任务3");

string task = queue.Dequeue();  // 取出第一个: 任务1
string peek = queue.Peek();     // 查看第一个: 任务2
int count = queue.Count;        // 剩余数量: 2

// ===== Stack<T> =====
Stack<int> stack = new Stack<int>();
stack.Push(1);
stack.Push(2);
stack.Push(3);

int top = stack.Pop();   // 取出最后进入的: 3
int peek = stack.Peek(); // 查看栈顶: 2
int count = stack.Count; // 剩余数量: 2

// ===== HashSet<T> =====
HashSet<int> set = new HashSet<int>();
set.Add(1);
set.Add(2);
set.Add(1);  // 重复，不会添加

bool has = set.Contains(2);
set.Remove(2);
```

### 习题练习

#### 练习 1.7.1
使用 List 存储 5 个学生姓名，添加、删除、遍历

#### 练习 1.7.2
使用 Dictionary 存储学生姓名和成绩，查找特定学生成绩

#### 练习 1.7.3
使用 Queue 模拟银行叫号系统

#### 练习 1.7.4
使用 Stack 实现括号匹配验证

#### 练习 1.7.5
使用 HashSet 去除数组中的重复元素

### 练习讲解

#### 练习 1.7.1 答案

```csharp
List<string> students = new List<string> {"张三", "李四", "王五", "赵六", "钱七"};

// 添加
students.Add("孙八");

// 删除
students.Remove("张三");
students.RemoveAt(0);

// 遍历
foreach (string name in students)
{
    Console.WriteLine(name);
}
```

#### 练习 1.7.2 答案

```csharp
Dictionary<string, int> scores = new Dictionary<string, int>
{
    {"张三", 85},
    {"李四", 72},
    {"王五", 90}
};

// 查找
string name = "李四";
if (scores.ContainsKey(name))
{
    Console.WriteLine($"{name}的成绩: {scores[name]}");
}
else
{
    Console.WriteLine("未找到该学生");
}
```

#### 练习 1.7.3 答案

```csharp
Queue<int> queue = new Queue<int>();
int number = 1;

// 叫号
queue.Enqueue(number++);
queue.Enqueue(number++);
queue.Enqueue(number++);

// 服务
while (queue.Count > 0)
{
    int current = queue.Dequeue();
    Console.WriteLine($"正在服务 {current} 号客户");
}
```

#### 练习 1.7.4 答案

```csharp
string input = "{[()]}";
Stack<char> stack = new Stack<char>();

foreach (char c in input)
{
    if (c == '(' || c == '[' || c == '{')
    {
        stack.Push(c);
    }
    else if (c == ')' || c == ']' || c == '}')
    {
        if (stack.Count == 0)
        {
            Console.WriteLine("不匹配");
            return;
        }
        char top = stack.Pop();
        if ((c == ')' && top != '(') ||
            (c == ']' && top != '[') ||
            (c == '}' && top != '{'))
        {
            Console.WriteLine("不匹配");
            return;
        }
    }
}

Console.WriteLine(stack.Count == 0 ? "匹配" : "不匹配");
```

#### 练习 1.7.5 答案

```csharp
int[] arr = {1, 2, 3, 2, 4, 1, 5};
HashSet<int> set = new HashSet<int>(arr);

int[] unique = new int[set.Count];
set.CopyTo(unique);

Console.WriteLine("去重后: " + string.Join(",", unique));
```

### 相关知识点

- 1.6 数组
- 2.9 泛型
- 2.19 LINQ

---

## 1.8 类与对象

### 概念讲解

**类（Class）**是面向对象编程的基本单位，是一类事物的抽象描述。

**对象（Object）**是类的实例，是具体存在的个体。

```
类（模板）                      对象（实例）
───────────────                ──────────────
┌──────────────────┐         ┌──────────────────┐
│ class Player      │         │ Player p1        │
│ ─────────────     │         │ ──────────────   │
│ string name;      │         │ name = "张三"    │
│ int health;       │         │ health = 100     │
│ void Attack(){}   │         │ Attack() 方法    │
└──────────────────┘         └──────────────────┘
```

**面向对象三大特性**：

| 特性 | 含义 |
|------|------|
| 封装 | 把数据和方法打包在一起 |
| 继承 | 子类复用父类的代码 |
| 多态 | 同一接口，不同表现 |

### 举例说明

```csharp
// ===== 类的定义 =====
class Person
{
    // 字段（类的属性数据）
    public string name;
    public int age;
    
    // 方法
    public void SayHello()
    {
        Console.WriteLine($"你好，我叫{name}，今年{age}岁");
    }
    
    // 带返回值的方法
    public int GetBirthYear()
    {
        return DateTime.Now.Year - age;
    }
}

// ===== 创建对象 =====
Person p1 = new Person();
Person p2 = new Person();

p1.name = "张三";
p1.age = 25;

p2.name = "李四";
p2.age = 30;

// ===== 调用方法 =====
p1.SayHello();  // 你好，我叫张三，今年25岁
p2.SayHello();  // 你好，我叫李四，今年30岁

// ===== 完整类示例 =====
class Player
{
    // 属性
    public string Name { get; set; }
    public int Health { get; set; }
    public int AttackPower { get; set; }
    
    // 构造函数
    public Player(string name, int health, int attack)
    {
        Name = name;
        Health = health;
        AttackPower = attack;
    }
    
    // 方法
    public void Attack(Player target)
    {
        Console.WriteLine($"{Name} 攻击 {target.Name}，造成 {AttackPower} 点伤害");
        target.Health -= AttackPower;
    }
    
    public void ShowInfo()
    {
        Console.WriteLine($"角色: {Name}, 生命: {Health}, 攻击力: {AttackPower}");
    }
}

// 使用
Player player1 = new Player(" warrior", 100, 20);
Player player2 = new Player("Monster", 80, 15);

player1.Attack(player2);  // warrior 攻击 Monster，造成 20 点伤害
player2.ShowInfo();      // 角色: Monster, 生命: 60, 攻击力: 15
```

### 习题练习

#### 练习 1.8.1
定义一个 Student 类，包含姓名、年龄、成绩属性，以及学习方法和显示信息方法

#### 练习 1.8.2
创建两个 Student 对象，调用显示信息方法

#### 练习 1.8.3
定义一个 Rectangle 类，包含长和宽属性，计算面积和周长的方法

#### 练习 1.8.4
定义一个 Calculator 类，实现加减乘除四个方法

### 练习讲解

#### 练习 1.8.1 答案

```csharp
class Student
{
    public string Name { get; set; }
    public int Age { get; set; }
    public double Score { get; set; }
    
    public void Study()
    {
        Console.WriteLine($"{Name} 正在学习...");
    }
    
    public void ShowInfo()
    {
        Console.WriteLine($"姓名: {Name}, 年龄: {Age}, 成绩: {Score}");
    }
}

Student s = new Student();
s.Name = "张三";
s.Age = 20;
s.Score = 85.5;
s.Study();
s.ShowInfo();
```

#### 练习 1.8.2 答案

```csharp
Student s1 = new Student { Name = "张三", Age = 20, Score = 85 };
Student s2 = new Student { Name = "李四", Age = 22, Score = 92 };

s1.ShowInfo();
s2.ShowInfo();
```

#### 练习 1.8.3 答案

```csharp
class Rectangle
{
    public double Length { get; set; }
    public double Width { get; set; }
    
    public Rectangle(double length, double width)
    {
        Length = length;
        Width = width;
    }
    
    public double GetArea()
    {
        return Length * Width;
    }
    
    public double GetPerimeter()
    {
        return 2 * (Length + Width);
    }
}

Rectangle rect = new Rectangle(5, 3);
Console.WriteLine($"面积: {rect.GetArea()}");      // 15
Console.WriteLine($"周长: {rect.GetPerimeter()}"); // 16
```

#### 练习 1.8.4 答案

```csharp
class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
    public double Divide(int a, int b)
    {
        if (b == 0)
        {
            Console.WriteLine("除数不能为0");
            return 0;
        }
        return (double)a / b;
    }
}

Calculator calc = new Calculator();
Console.WriteLine(calc.Add(10, 5));      // 15
Console.WriteLine(calc.Subtract(10, 5)); // 5
Console.WriteLine(calc.Multiply(10, 5)); // 50
Console.WriteLine(calc.Divide(10, 5));   // 2
```

### 相关知识点

- 1.9 构造函数
- 1.10 访问修饰符与属性
- 1.11 静态成员
- 1.12 封装

---

## 1.9 构造函数

### 概念讲解

**构造函数**是创建对象时自动调用的特殊方法，用于初始化对象。

**特点**：
- 名称与类名相同
- 没有返回类型（连 void 都不能写）
- 在创建对象时自动调用
- 可以重载（多个构造函数）

**类型**：
- 无参构造函数
- 带参构造函数
- 静态构造函数（初始化静态成员）

### 举例说明

```csharp
class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    
    // 无参构造函数
    public Person()
    {
        Name = "未知";
        Age = 0;
    }
    
    // 带参构造函数
    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }
    
    // 另一个重载
    public Person(string name) : this(name, 0)  // 调用另一个构造函数
    {
    }
}

Person p1 = new Person();              // 调用无参构造
Person p2 = new Person("张三", 25);     // 调用带参构造
Person p3 = new Person("李四");        // 调用一个参数构造

// ===== 构造函数链 =====
class Student
{
    public string Name { get; set; }
    public int Age { get; set; }
    public double Score { get; set; }
    
    // 链式调用
    public Student() : this("未知", 0, 0) { }
    
    public Student(string name, int age) : this(name, age, 0) { }
    
    public Student(string name, int age, double score)
    {
        Name = name;
        Age = age;
        Score = score;
    }
}

// ===== 静态构造函数 =====
class GameManager
{
    public static int PlayerCount { get; private set; }
    
    static GameManager()
    {
        PlayerCount = 0;
        Console.WriteLine("游戏管理器初始化");
    }
    
    public static void AddPlayer()
    {
        PlayerCount++;
    }
}
```

### 习题练习

#### 练习 1.9.1
定义一个 Car 类，使用构造函数初始化品牌、颜色、价格

#### 练习 1.9.2
定义一个 Product 类，包含无参、带参、拷贝三个构造函数

#### 练习 1.9.3
创建两个 Product 对象，分别使用不同构造函数初始化

#### 练习 1.9.4
定义一个 BankAccount 类，使用构造函数初始化账户号和初始余额，提供存钱和取钱方法

### 练习讲解

#### 练习 1.9.1 答案

```csharp
class Car
{
    public string Brand { get; set; }
    public string Color { get; set; }
    public decimal Price { get; set; }
    
    public Car(string brand, string color, decimal price)
    {
        Brand = brand;
        Color = color;
        Price = price;
    }
    
    public void ShowInfo()
    {
        Console.WriteLine($"品牌: {Brand}, 颜色: {Color}, 价格: {Price}万");
    }
}

Car car = new Car("比亚迪", "白色", 20.5m);
car.ShowInfo();
```

#### 练习 1.9.2 答案

```csharp
class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    
    // 无参构造
    public Product()
    {
        Name = "未知商品";
        Price = 0;
        Stock = 0;
    }
    
    // 带参构造
    public Product(string name, decimal price, int stock)
    {
        Name = name;
        Price = price;
        Stock = stock;
    }
    
    // 拷贝构造
    public Product(Product other)
    {
        Name = other.Name;
        Price = other.Price;
        Stock = other.Stock;
    }
}
```

#### 练习 1.9.3 答案

```csharp
Product p1 = new Product();  // 无参
Product p2 = new Product("手机", 5000, 100);  // 带参
Product p3 = new Product(p2);  // 拷贝，p3 和 p2 属性相同

Console.WriteLine(p1.Name);  // 未知商品
Console.WriteLine(p2.Name);  // 手机
Console.WriteLine(p3.Name);  // 手机
```

#### 练习 1.9.4 答案

```csharp
class BankAccount
{
    public string AccountNumber { get; }
    public decimal Balance { get; private set; }
    
    public BankAccount(string accountNumber, decimal initialBalance)
    {
        AccountNumber = accountNumber;
        Balance = initialBalance;
    }
    
    public void Deposit(decimal amount)
    {
        if (amount > 0)
        {
            Balance += amount;
            Console.WriteLine($"存款成功，余额: {Balance}");
        }
    }
    
    public bool Withdraw(decimal amount)
    {
        if (amount > 0 && amount <= Balance)
        {
            Balance -= amount;
            Console.WriteLine($"取款成功，余额: {Balance}");
            return true;
        }
        Console.WriteLine("取款失败，余额不足");
        return false;
    }
}

BankAccount account = new BankAccount("123456", 1000);
account.Deposit(500);     // 存款成功，余额: 1500
account.Withdraw(2000);   // 取款失败
account.Withdraw(800);    // 取款成功，余额: 700
```

### 相关知识点

- 1.8 类与对象
- 1.10 访问修饰符与属性
- 2.1 继承与构造函数

---

## 1.10 访问修饰符与属性

### 概念讲解

**访问修饰符**控制成员的访问权限：

| 修饰符 | 访问范围 |
|--------|----------|
| public | 任意位置可访问 |
| private | 仅本类可访问 |
| protected | 本类及子类可访问 |
| internal | 同程序集可访问 |
| protected internal | 本程序集或子类可访问 |
| private protected | 本类或子类（同程序集）可访问 |

**属性（Property）**是封装字段的方式，提供 getter 和 setter：

| 属性类型 | 说明 |
|----------|------|
| 读写属性 | get 和 set 都可访问 |
| 只读属性 | 只有 get |
| 只写属性 | 只有 set（很少用） |
| 自动属性 | 编译器自动生成 backing field |

### 举例说明

```csharp
class Player
{
    // ===== 访问修饰符 =====
    public string name;      // 公开，任意可访问
    private int health;      // 私有，仅本类可访问
    protected float speed;   // 受保护，本类及子类可访问
    internal string team;    // 同程序集可访问
    
    // ===== 属性（完整属性） =====
    private int level;
    public int Level
    {
        get { return level; }
        set 
        { 
            if (value > 0 && value <= 100)
                level = value; 
        }
    }
    
    // ===== 自动属性 =====
    public int Score { get; set; }
    
    // ===== 只读属性 =====
    public string Title
    {
        get { return Score >= 100 ? "大师" : "新手"; }
    }
    
    // ===== 外部只读，内部可写 =====
    public int Health
    {
        get => health;
        private set => health = value;
    }
    
    // ===== 方法访问私有字段 =====
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health < 0) health = 0;
    }
}

Player p = new Player();
p.name = "张三";
p.Level = 50;
p.Score = 150;
p.TakeDamage(30);

// p.health = 100;  // 错误，private
// p.Score = 200;   // 错误，外部不可写
Console.WriteLine(p.Title);  // 大师（只读属性）
```

### 习题练习

#### 练习 1.10.1
定义一个 Rectangle 类，长和宽使用 private，通过属性访问

#### 练习 1.10.2
为 Rectangle 添加只读属性 Area（面积）和 Perimeter（周长）

#### 练习 1.10.3
定义一个 Employee 类，Salary 属性外部只读，内部可写

#### 练习 1.10.4
定义一个 Student 类，使用自动属性，包含验证逻辑：年龄必须在 6-100 之间

### 练习讲解

#### 练习 1.10.1 答案

```csharp
class Rectangle
{
    private double length;
    private double width;
    
    public double Length
    {
        get => length;
        set => length = value > 0 ? value : 0;
    }
    
    public double Width
    {
        get => width;
        set => width = value > 0 ? value : 0;
    }
    
    public Rectangle(double length, double width)
    {
        Length = length;
        Width = width;
    }
}

Rectangle rect = new Rectangle(5, 3);
Console.WriteLine($"长: {rect.Length}, 宽: {rect.Width}");
```

#### 练习 1.10.2 答案

```csharp
class Rectangle
{
    public double Length { get; set; }
    public double Width { get; set; }
    
    // 只读属性
    public double Area => Length * Width;
    public double Perimeter => 2 * (Length + Width);
    
    public Rectangle(double length, double width)
    {
        Length = length;
        Width = width;
    }
}

Rectangle rect = new Rectangle(5, 3);
Console.WriteLine($"面积: {rect.Area}");      // 15
Console.WriteLine($"周长: {rect.Perimeter}"); // 16
```

#### 练习 1.10.3 答案

```csharp
class Employee
{
    public string Name { get; set; }
    public string Position { get; set; }
    
    // 外部只读，内部可写
    public decimal Salary { get; private set; }
    
    public Employee(string name, string position, decimal salary)
    {
        Name = name;
        Position = position;
        Salary = salary;
    }
    
    // 内部方法可以修改 Salary
    public void IncreaseSalary(decimal amount)
    {
        if (amount > 0)
            Salary += amount;
    }
}

Employee emp = new Employee("张三", "工程师", 10000);
Console.WriteLine(emp.Salary);  // 10000
emp.IncreaseSalary(2000);
Console.WriteLine(emp.Salary);  // 12000
// emp.Salary = 15000;  // 错误，外部不可写
```

#### 练习 1.10.4 答案

```csharp
class Student
{
    private int age;
    
    public string Name { get; set; }
    public int Age
    {
        get => age;
        set
        {
            if (value < 6 || value > 100)
                throw new ArgumentException("年龄必须在6-100之间");
            age = value;
        }
    }
    public double Score { get; set; }
}

Student s = new Student();
s.Name = "张三";
s.Age = 20;
Console.WriteLine($"姓名: {s.Name}, 年龄: {s.Age}");
```

### 相关知识点

- 1.8 类与对象
- 1.12 封装
- 2.2 多态（属性重写）

---

## 1.11 静态成员

### 概念讲解

**静态成员**属于类本身，而不是某个对象。所有对象共享同一份静态数据。

| 特性 | 静态成员 | 实例成员 |
|------|----------|----------|
| 关键字 | static | 无 |
| 内存位置 | 类的存储区域 | 对象的存储区域 |
| 访问方式 | 类名.成员 | 对象.成员 |
| 生命周期 | 程序开始到结束 | 对象创建到销毁 |

**静态类**：所有成员必须是静态的，不能实例化（如 Math 类）

### 举例说明

```csharp
class Player
{
    // 实例字段（每个对象独立）
    public string name;
    
    // 静态字段（所有对象共享）
    public static int totalPlayers = 0;
    
    // 静态构造函数（初始化静态成员）
    static Player()
    {
        Console.WriteLine("游戏初始化...");
        totalPlayers = 0;
    }
    
    // 实例构造函数
    public Player(string name)
    {
        this.name = name;
        totalPlayers++;  // 访问静态字段
    }
    
    // 静态方法
    public static void ResetGame()
    {
        totalPlayers = 0;
        Console.WriteLine("游戏重置");
    }
    
    // 实例方法
    public void ShowInfo()
    {
        Console.WriteLine($"玩家: {name}, 总人数: {totalPlayers}");
    }
}

// 使用
Player.ResetGame();  // 调用静态方法

Player p1 = new Player("张三");
p1.ShowInfo();  // 玩家: 张三, 总人数: 1

Player p2 = new Player("李四");
p2.ShowInfo();  // 玩家: 李四, 总人数: 2

Console.WriteLine(Player.totalPlayers);  // 2

// ===== 静态类 =====
static class MathHelper
{
    public const double PI = 3.14159;
    
    public static int Max(int a, int b) => a > b ? a : b;
    public static int Min(int a, int b) => a < b ? a : b;
    public static bool IsEven(int n) => n % 2 == 0;
}

Console.WriteLine(MathHelper.PI);  // 3.14159
Console.WriteLine(MathHelper.Max(3, 5));  // 5
```

### 习题练习

#### 练习 1.11.1
定义一个银行类，使用静态字段存储利率，所有账户共享同一利率

#### 练习 1.11.2
定义一个计数器类，使用静态方法获取当前计数器和重置计数器

#### 练习 1.11.3
创建多个银行账户对象，验证静态字段是共享的

#### 练习 1.11.4
定义一个静态工具类，提供计算矩形面积和三角形面积的方法

### 练习讲解

#### 练习 1.11.1 答案

```csharp
class BankAccount
{
    // 静态字段，所有账户共享
    public static double interestRate = 0.03;
    
    public string AccountNumber { get; }
    public decimal Balance { get; private set; }
    
    public BankAccount(string accountNumber, decimal initialBalance)
    {
        AccountNumber = accountNumber;
        Balance = initialBalance;
    }
    
    public void CalculateInterest()
    {
        decimal interest = Balance * (decimal)interestRate;
        Console.WriteLine($"账户 {AccountNumber} 利息: {interest}");
    }
}

BankAccount account1 = new BankAccount("001", 10000);
BankAccount account2 = new BankAccount("002", 20000);

account1.CalculateInterest();  // 300
account2.CalculateInterest(); // 600

// 修改利率影响所有账户
BankAccount.interestRate = 0.05;
account1.CalculateInterest();  // 500
```

#### 练习 1.11.2 答案

```csharp
class Counter
{
    private static int count = 0;
    
    public static int GetCount() => count;
    
    public static void Reset() => count = 0;
    
    public void Increment() => count++;
}

Counter c1 = new Counter();
Counter c2 = new Counter();

c1.Increment();
c1.Increment();
c2.Increment();

Console.WriteLine(Counter.GetCount());  // 3

Counter.Reset();
Console.WriteLine(Counter.GetCount());  // 0
```

#### 练习 1.11.3 答案

```csharp
BankAccount a1 = new BankAccount("001", 1000);
BankAccount a2 = new BankAccount("002", 2000);
BankAccount a3 = new BankAccount("003", 3000);

Console.WriteLine($"创建了 {BankAccount.totalAccounts} 个账户");
// 三个账户共享同一个 interestRate
// 修改 BankAccount.interestRate 会影响所有账户
```

#### 练习 1.11.4 答案

```csharp
static class GeometryHelper
{
    public static double RectangleArea(double width, double height)
    {
        return width * height;
    }
    
    public static double TriangleArea(double baseLength, double height)
    {
        return 0.5 * baseLength * height;
    }
    
    public static double CircleArea(double radius)
    {
        return Math.PI * radius * radius;
    }
}

Console.WriteLine(GeometryHelper.RectangleArea(5, 3));    // 15
Console.WriteLine(GeometryHelper.TriangleArea(4, 6));      // 12
Console.WriteLine(GeometryHelper.CircleArea(2));          // 12.57
```

### 相关知识点

- 1.8 类与对象
- 2.1 继承（静态成员在子类中的访问）
- 2.23 单例模式

---

## 1.12 封装

### 概念讲解

**封装**是把数据（属性）和操作（方法）打包在一起，对外隐藏实现细节，只暴露必要的接口。

**封装的优点**：
1. **数据保护** - 防止非法值
2. **简化使用** - 用户只需知道方法用途
3. **易于维护** - 修改内部实现不影响外部
4. **隐藏复杂性**

```
封装示意图：
┌─────────────────────────────────┐
│        银行账户类                │
│  ┌───────────────────────────┐  │
│  │ private balance          │  │  ← 私有数据
│  │  (外部不能直接访问)       │  │
│  └───────────────────────────┘  │
│  ┌───────────────────────────┐  │
│  │ public Deposit()         │  │  ← 公有方法
│  │ public Withdraw()        │  │    (受控访问)
│  └───────────────────────────┘  │
└─────────────────────────────────┘
```

### 举例说明

```csharp
// ===== 封装示例：银行账户 =====
class BankAccount
{
    // 私有字段（隐藏数据）
    private decimal balance;
    private string accountNumber;
    
    // 公开属性（受控访问）
    public string AccountNumber => accountNumber;
    
    // 受保护构造函数
    protected BankAccount(string number, decimal initialBalance)
    {
        accountNumber = number;
        balance = initialBalance;
    }
    
    // 公有方法（接口）
    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("存款金额必须大于0");
            return;
        }
        balance += amount;
        Console.WriteLine($"存款成功，当前余额: {balance:C}");
    }
    
    public bool Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("取款金额必须大于0");
            return false;
        }
        if (amount > balance)
        {
            Console.WriteLine("余额不足");
            return false;
        }
        balance -= amount;
        Console.WriteLine($"取款成功，当前余额: {balance:C}");
        return true;
    }
    
    public decimal GetBalance()
    {
        return balance;
    }
}

// 使用
BankAccount account = new BankAccount("123456", 1000);
account.Deposit(500);           // 存款成功
account.Withdraw(200);          // 取款成功
Console.WriteLine(account.GetBalance());  // 1300
// account.balance = 1000000;  // 编译错误，无法直接访问私有字段

// ===== 封装示例：游戏角色 =====
class GameCharacter
{
    private int health;
    private int maxHealth = 100;
    
    public string Name { get; }
    
    public int Health
    {
        get => health;
        private set
        {
            health = value < 0 ? 0 : (value > maxHealth ? maxHealth : value);
        }
    }
    
    public GameCharacter(string name)
    {
        Name = name;
        Health = maxHealth;
    }
    
    public void TakeDamage(int damage)
    {
        if (damage < 0) return;
        Health -= damage;
        Console.WriteLine($"{Name} 受到 {damage} 点伤害，剩余 {Health} HP");
        
        if (Health == 0)
            Console.WriteLine($"{Name} 死亡！");
    }
    
    public void Heal(int amount)
    {
        if (amount < 0) return;
        Health += amount;
        Console.WriteLine($"{Name} 恢复 {amount} HP，当前 {Health} HP");
    }
}

GameCharacter player = new GameCharacter(" warrior");
player.TakeDamage(30);  // warrior 受到 30 点伤害，剩余 70 HP
player.Heal(50);        // warrior 恢复 50 HP，当前 100 HP
player.TakeDamage(200); // warrior 受到 200 点伤害，剩余 0 HP
                        // warrior 死亡！
```

### 习题练习

#### 练习 1.12.1
封装一个温度类，内部存储摄氏度，外部提供获取华氏度的接口

#### 练习 1.12.2
封装一个成绩类，验证成绩在 0-100 之间

#### 练习 1.12.3
封装一个用户类，密码只能设置不能读取，提供验证密码方法

#### 练习 1.12.4
封装一个购物车类，添加商品、移除商品、计算总价

### 练习讲解

#### 练习 1.12.1 答案

```csharp
class Temperature
{
    private double celsius;
    
    public double Celsius
    {
        get => celsius;
        set => celsius = value;
    }
    
    // 只读属性：华氏度
    public double Fahrenheit => celsius * 9 / 5 + 32;
    
    public Temperature(double celsius)
    {
        this.celsius = celsius;
    }
}

Temperature temp = new Temperature(25);
Console.WriteLine($"摄氏度: {temp.Celsius}°C");          // 25
Console.WriteLine($"华氏度: {temp.Fahrenheit}°F");      // 77
```

#### 练习 1.12.2 答案

```csharp
class Score
{
    private double score;
    
    public double Value
    {
        get => score;
        set
        {
            if (value < 0 || value > 100)
                throw new ArgumentException("成绩必须在0-100之间");
            score = value;
        }
    }
    
    public string Grade
    {
        get
        {
            if (score >= 90) return "A";
            if (score >= 80) return "B";
            if (score >= 70) return "C";
            if (score >= 60) return "D";
            return "F";
        }
    }
}

Score s = new Score();
s.Value = 85;
Console.WriteLine($"成绩: {s.Value}, 等级: {s.Grade}");
```

#### 练习 1.12.3 答案

```csharp
class User
{
    private string password;
    
    public string Username { get; }
    
    public User(string username, string password)
    {
        Username = username;
        this.password = password;
    }
    
    // 只写属性
    public void SetPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("密码不能为空");
        password = newPassword;
    }
    
    // 验证密码
    public bool ValidatePassword(string input)
    {
        return password == input;
    }
}

User user = new User("zhangsan", "123456");
Console.WriteLine(user.ValidatePassword("123456"));  // true
Console.WriteLine(user.ValidatePassword("wrong"));   // false
```

#### 练习 1.12.4 答案

```csharp
class ShoppingCart
{
    private Dictionary<string, (int quantity, decimal price)> items = new Dictionary<string, (int, decimal)>();
    
    // 添加商品
    public void AddItem(string productName, int quantity, decimal price)
    {
        if (items.ContainsKey(productName))
        {
            var existing = items[productName];
            items[productName] = (existing.quantity + quantity, existing.price);
        }
        else
        {
            items[productName] = (quantity, price);
        }
    }
    
    // 移除商品
    public void RemoveItem(string productName)
    {
        items.Remove(productName);
    }
    
    // 计算总价
    public decimal GetTotalPrice()
    {
        decimal total = 0;
        foreach (var item in items.Values)
        {
            total += item.quantity * item.price;
        }
        return total;
    }
    
    // 显示购物车
    public void ShowCart()
    {
        Console.WriteLine("购物车内容:");
        foreach (var item in items)
        {
            Console.WriteLine($"  {item.Key}: {item.Value.quantity} x {item.Value.price:C}");
        }
        Console.WriteLine($"总计: {GetTotalPrice():C}");
    }
}

ShoppingCart cart = new ShoppingCart();
cart.AddItem("手机", 1, 5000);
cart.AddItem("耳机", 2, 200);
cart.AddItem("手机", 1, 5000);  // 再加一台手机
cart.ShowCart();
cart.RemoveItem("耳机");
cart.ShowCart();
```

### 相关知识点

- 1.10 访问修饰符与属性
- 2.1 继承（访问修饰符在继承中的表现）
- 2.5 接口（封装实现细节）

---

# 进阶篇

## 目录

- [2.1 继承与多态](#21-继承与多态)
- [2.2 接口与抽象类](#22-接口与抽象类)
- [2.3 委托与事件](#23-委托与事件)
- [2.4 泛型](#24-泛型)
- [2.5 异步编程基础](#25-异步编程基础)
- [2.6 反射与特性](#26-反射与特性)
- [2.7 LINQ](#27-linq)
- [2.8 委托与事件（完整版）](#28-委托与事件完整版)
- [2.9 高级主题](#29-高级主题)

---

## 2.1 继承与多态

### 概念讲解

**继承（Inheritance）**是面向对象编程的三大特性之一，它允许一个类（称为**子类**或**派生类**）继承另一个类（称为**父类**或**基类**）的属性和方法。通过继承，子类可以直接使用父类已经实现的功能，无需重复编写相同的代码。

```
继承的核心概念：

父类（基类）                    子类（派生类）
┌─────────────────────┐       ┌─────────────────────┐
│ - name: string      │       │                     │
│ - age: int          │       │ 继承父类的所有成员   │
│ + Eat()             │──────→│                     │
│ + Sleep()           │       │ 可以添加新的成员     │
└─────────────────────┘       │ 可以重写父类的方法   │
                              └─────────────────────┘
```

**C# 继承的特点**：
1. **单继承**：每个类只能继承一个父类（但可以实现多个接口）
2. **多层继承**：可以形成 A→B→C 的继承链
3. **密封继承**：使用 sealed 关键字阻止类被继承

**多态（Polymorphism）**是指同一个方法调用在不同对象上产生不同行为的能力。多态让我们能够使用父类类型的引用来指向子类对象，从而调用子类的实现。

```
多态的实现原理：

父类引用指向子类对象：
Animal animal = new Dog("旺财");

调用 Speak() 方法时：
- 如果是 Dog 对象 → 调用 Dog 的 Speak()
- 如果是 Cat 对象 → 调用 Cat 的 Speak()
- 如果是 Animal 对象 → 调用 Animal 的 Speak()
```

**实现多态的三种方式**：
1. **虚方法（virtual + override）**：父类声明虚方法，子类重写实现
2. **抽象方法（abstract）**：父类声明抽象方法，子类必须实现
3. **接口（Interface）**：通过接口实现多态

### 举例说明

```csharp
// ===== 1. 父类的定义 =====
// 使用 virtual 关键字标记方法为虚方法，允许子类重写
class Animal
{
    // 自动属性：动物的名称
    public string Name { get; set; }
    
    // 自动属性：动物的年龄
    public int Age { get; set; }
    
    // 构造函数：创建动物对象时初始化名称和年龄
    // 使用 : base(name, age) 调用父类构造函数
    public Animal(string name, int age)
    {
        Name = name;
        Age = age;
    }
    
    // 普通方法：动物进食行为（所有动物都有的行为）
    public void Eat()
    {
        Console.WriteLine($"{Name} 正在吃东西");
    }
    
    // 虚方法（virtual）：动物发出声音
    // virtual 关键字表示该方法可以被子类重写（override）
    // 如果子类不重写，则调用父类的默认实现
    public virtual void Speak()
    {
        Console.WriteLine($"{Name} 发出了声音");
    }
}

// ===== 2. 子类的定义 =====
class Dog : Animal  // 使用 : Animal 表示继承自 Animal 类
{
    // Dog 类新增的属性：品种
    public string Breed { get; set; }
    
    // 构造函数：使用 base(name, age) 调用父类构造函数
    public Dog(string name, int age, string breed) : base(name, age)
    {
        // 先调用父类构造函数初始化 Name 和 Age
        // 然后再初始化 Dog 类特有的 Breed 属性
        Breed = breed;
    }
    
    // 重写父类的虚方法 Speak()
    // override 关键字表示这是对父类方法的覆写
    // 当通过父类引用调用 Speak() 时，会执行子类的实现
    public override void Speak()
    {
        Console.WriteLine($"{Name} 汪汪叫");
    }
    
    // Dog 类独有的方法：捡球
    public void Fetch()
    {
        Console.WriteLine($"{Name} 正在捡球");
    }
}

// 定义另一个子类：猫
class Cat : Animal
{
    // 构造函数：直接调用 base 传递参数
    public Cat(string name, int age) : base(name, age) { }
    
    // 重写 Speak 方法
    public override void Speak()
    {
        Console.WriteLine($"{Name} 喵喵叫");
    }
}

// ===== 3. 多态的使用场景 =====
class AnimalWorld
{
    static void Main()
    {
        // 创建Animal数组，存放不同类型的动物
        // 注意：数组类型是 Animal，但实际对象是 Dog、Cat 或 Animal
        // 这就是多态的体现：父类引用可以指向子类对象
        Animal[] animals = new Animal[]
        {
            new Dog("旺财", 3, "金毛"),    // 实际是 Dog 对象
            new Cat("咪咪", 2),           // 实际是 Cat 对象
            new Animal("老虎", 5)          // 实际是 Animal 对象
        };
        
        // 遍历数组，调用 Speak 方法
        // 由于多态，会自动调用各个对象实际的 Speak 方法
        foreach (Animal a in animals)
        {
            // 这里体现多态：调用的是实际对象的 Speak()
            a.Speak();  
        }
        // 输出结果：
        // 旺财 汪汪叫       （调用 Dog 的 Speak）
        // 咪咪 喵喵叫       （调用 Cat 的 Speak）
        // 老虎 发出了声音   （调用 Animal 的 Speak）
    }
}
```

### 补充说明：base 关键字和 sealed 关键字

```csharp
// base 关键字的使用场景：
// 1. 在构造函数中调用父类构造函数
class DerivedClass : BaseClass
{
    public DerivedClass(int value) : base(value)  // 调用父类的构造函数
    {
        // 还可以继续初始化子类特有的内容
    }
    
    // 在方法中调用父类的方法
    public override void DoSomething()
    {
        // 先调用父类的实现
        base.DoSomething();
        
        // 然后执行子类特有的逻辑
        Console.WriteLine("子类特有的逻辑");
    }
}

// sealed 关键字：阻止类被继承或方法被重写
// 密封类：不能被继承
public sealed class Singleton  // 常见的单例模式使用 sealed
{
    private static Singleton instance;
    public static Singleton Instance => instance ??= new Singleton();
}

// 密封方法：阻止子类重写该方法
public class Player
{
    public virtual void TakeDamage(int damage) { }  // 普通虚方法
    
    public sealed override void Die()  // sealed 阻止子类重写
    {
        // 死亡逻辑，锁定为最终实现
        Console.WriteLine("游戏结束");
    }
}

// 尝试继承 sealed 类会导致编译错误
// class MySingleton : Singleton { }  // 错误！
```

### 习题练习

#### 练习 2.1.1
定义一个基类 Vehicle（交通工具），包含品牌（Brand）、颜色（Color）属性，以及运行（Run）方法。创建子类 Car（汽车）和 Motorcycle（摩托车），汽车有座位数（Seats）属性，摩托车有边车（HasSidecar）属性。

#### 练习 2.1.2
创建一个图形类层次结构：基类 Shape（抽象类），包含抽象属性 Area（面积）和 Perimeter（周长）。创建子类 Circle（圆形）和 Rectangle（矩形），实现相应的计算逻辑。

#### 练习 2.1.3
创建一个员工类层次结构：基类 Employee，包含姓名和职位属性，以及计算工资的虚方法。创建子类 Developer 和 Designer，重写计算工资方法。

#### 练习 2.1.4
使用 sealed 关键字实现一个游戏管理器单例类，确保不能被继承。

### 练习讲解

#### 练习 2.1.1 答案

```csharp
// 基类：交通工具
class Vehicle
{
    // 属性：品牌
    public string Brand { get; set; }
    
    // 属性：颜色
    public string Color { get; set; }
    
    // 构造函数
    public Vehicle(string brand, string color)
    {
        Brand = brand;
        Color = color;
    }
    
    // 方法：运行
    public void Run()
    {
        Console.WriteLine($"{Color} 色的 {Brand} 正在行驶");
    }
}

// 子类：汽车
class Car : Vehicle
{
    // 汽车特有的属性：座位数
    public int Seats { get; set; }
    
    // 构造函数，使用 base 调用父类构造函数
    public Car(string brand, string color, int seats) : base(brand, color)
    {
        Seats = seats;
    }
    
    // 子类可以添加自己的方法
    public void Honk()
    {
        Console.WriteLine($"{Brand} 按喇叭：滴滴！");
    }
}

// 子类：摩托车
class Motorcycle : Vehicle
{
    // 摩托车特有的属性：是否有边车
    public bool HasSidecar { get; set; }
    
    public Motorcycle(string brand, string color, bool hasSidecar) : base(brand, color)
    {
        HasSidecar = hasSidecar;
    }
    
    // 子类重写父类的方法
    public override void Run()
    {
        string sidecarInfo = HasSidecar ? "带边车" : "不带边车";
        Console.WriteLine($"{Color} 色的 {Brand} 摩托车正在行驶，{sidecarInfo}");
    }
}

// 测试
class Program
{
    static void Main()
    {
        Car car = new Car("特斯拉", "红色", 5);
        car.Run();  // 输出：红色的 特斯拉 正在行驶
        car.Honk(); // 输出：特斯拉 按喇叭：滴滴！
        
        Motorcycle motorcycle = new Motorcycle("雅马哈", "黑色", true);
        motorcycle.Run(); // 输出：黑色的 雅马哈 摩托车正在行驶，带边车
    }
}
```

#### 练习 2.1.2 答案

```csharp
// 抽象基类：图形
// abstract 关键字表示这是一个抽象类，不能直接实例化
abstract class Shape
{
    // 抽象属性：面积（子类必须实现）
    public abstract double Area { get; }
    
    // 抽象属性：周长（子类必须实现）
    public abstract double Perimeter { get; }
}

// 子类：圆形
class Circle : Shape
{
    // 圆的半径
    public double Radius { get; }
    
    // 构造函数
    public Circle(double radius)
    {
        Radius = radius;
    }
    
    // 实现抽象属性：面积 = π * r²
    public override double Area => Math.PI * Radius * Radius;
    
    // 实现抽象属性：周长 = 2 * π * r
    public override double Perimeter => 2 * Math.PI * Radius;
}

// 子类：矩形
class Rectangle : Shape
{
    // 矩形的宽和高
    public double Width { get; }
    public double Height { get; }
    
    public Rectangle(double width, double height)
    {
        Width = width;
        Height = height;
    }
    
    // 实现抽象属性：面积 = 宽 * 高
    public override double Area => Width * Height;
    
    // 实现抽象属性：周长 = 2 * (宽 + 高)
    public override double Perimeter => 2 * (Width + Height);
}

// 测试
class Program
{
    static void Main()
    {
        Shape[] shapes = new Shape[]
        {
            new Circle(5),          // 半径为5的圆
            new Rectangle(4, 6)    // 宽4高6的矩形
        };
        
        foreach (Shape shape in shapes)
        {
            Console.WriteLine($"面积: {shape.Area:F2}, 周长: {shape.Perimeter:F2}");
        }
        // 输出：
        // 面积: 78.54, 周长: 31.42
        // 面积: 24.00, 周长: 20.00
    }
}
```

#### 练习 2.1.3 答案

```csharp
// 基类：员工
class Employee
{
    public string Name { get; set; }    // 姓名
    public string Position { get; set; } // 职位
    
    // 构造函数
    public Employee(string name, string position)
    {
        Name = name;
        Position = position;
    }
    
    // 虚方法：计算工资（默认实现）
    public virtual decimal CalculateSalary()
    {
        return 5000m;  // 默认基本工资
    }
}

// 子类：开发者
class Developer : Employee
{
    public int WorkingYears { get; set; }  // 工作年限
    
    public Developer(string name, int years) : base(name, "开发者")
    {
        WorkingYears = years;
    }
    
    // 重写计算工资方法
    public override decimal CalculateSalary()
    {
        // 开发者工资 = 基本工资 + 工作年限 * 1000
        return 5000m + WorkingYears * 1000m;
    }
}

// 子类：设计师
class Designer : Employee
{
    public int ProjectCount { get; set; }  // 完成项目数
    
    public Designer(string name, int projects) : base(name, "设计师")
    {
        ProjectCount = projects;
    }
    
    // 重写计算工资方法
    public override decimal CalculateSalary()
    {
        // 设计师工资 = 基本工资 + 项目数 * 800
        return 5000m + ProjectCount * 800m;
    }
}

// 测试
class Program
{
    static void Main()
    {
        Employee[] employees = new Employee[]
        {
            new Employee("张三", "普通员工"),
            new Developer("李四", 3),      // 3年工作经验
            new Designer("王五", 5)        // 5个项目
        };
        
        foreach (Employee emp in employees)
        {
            Console.WriteLine($"{emp.Name} ({emp.Position}): {emp.CalculateSalary()}元");
        }
        // 输出：
        // 张三 (普通员工): 5000元
        // 李四 (开发者): 8000元
        // 王五 (设计师): 9000元
    }
}
```

#### 练习 2.1.4 答案

```csharp
// 使用 sealed 关键字将类声明为密封类
// 密封类不能被其他类继承
public sealed class GameManager
{
    // 单例模式：静态实例
    private static GameManager instance;
    
    // 私有构造函数：防止外部通过 new 创建实例
    private GameManager()
    {
        Console.WriteLine("游戏管理器初始化");
    }
    
    // 公共静态属性：获取单例实例
    public static GameManager Instance
    {
        get
        {
            // 懒汉式单例：第一次访问时创建实例
            if (instance == null)
            {
                instance = new GameManager();
            }
            return instance;
        }
    }
    
    // 游戏管理器的方法
    public void StartGame() => Console.WriteLine("游戏开始");
    public void EndGame() => Console.WriteLine("游戏结束");
}

// 尝试继承会导致编译错误：
// class MyGameManager : GameManager { }  // 错误：无法从密封类继承
```

### 相关知识点

- 1.12 封装 - 继承是对封装的扩展，通过继承实现代码复用
- 2.2 接口与抽象类 - 抽象类是介于普通类和接口之间的概念
- 2.3 委托与事件 - 结合继承可以实现更灵活的事件系统

---

## 2.2 接口与抽象类

### 概念讲解

**接口（Interface）**和**抽象类（Abstract Class）**都是面向对象编程中用于定义行为规范的重要概念。

**接口**是一组方法签名的集合，它只定义"做什么"（方法声明），而不关心"怎么做"（方法实现）。接口本质上是一种契约，它告诉实现类必须提供哪些功能。

```
接口的概念：

┌─────────────────────┐
│   IPlayable         │  ← 接口名（通常以 I 开头）
├─────────────────────┤
│ + Play()            │  ← 方法签名（只有声明，没有实现）
│ + Pause()           │
│ + Stop()            │
└─────────────────────┘
         ↓ 实现
┌─────────────────────┐
│   VideoGame         │
├─────────────────────┤
│ + Play() { ... }    │  ← 必须实现接口的所有方法
│ + Pause() { ... }   │
│ + Stop() { ... }    │
└─────────────────────┘
```

**抽象类**是介于普通类和接口之间的概念。它可以包含：
- 抽象方法（只有声明，没有实现）- 子类必须实现
- 具体方法（有实现）- 子类可以直接使用或重写
- 字段和属性

| 特性对比 | 接口 | 抽象类 |
|----------|------|--------|
| 继承数量 | 可以多继承（多个接口） | 只能单继承（一个父类） |
| 成员类型 | 只有方法声明、属性、索引器、事件 | 可以有方法实现、字段、属性 |
| 构造函数 | 没有构造函数 | 可以有构造函数 |
| 访问修饰符 | 成员默认 public | 成员可以自定义（private/protected等） |
| 使用场景 | 定义行为规范（"能做什么"） | 定义公共功能 + 规范（"是什么" + "能做什么"） |

### 举例说明

```csharp
// ===== 1. 接口的定义和使用 =====

// 定义可伤害的接口
// 接口只声明方法签名，不提供实现
interface IDamageable
{
    // 方法：受到伤害
    void TakeDamage(int damage);
    
    // 属性：当前生命值（只读）
    int Health { get; }
}

// 定义可治疗的接口
interface IHealable
{
    // 方法：治疗
    void Heal(int amount);
}

// 定义可移动的接口
interface IMovable
{
    // 方法：移动
    void Move(Vector2 direction);
    
    // 属性：移动速度
    float Speed { get; }
}

// 实现类：玩家同时实现多个接口
// 类可以实现多个接口，这是接口的重要优势
class Player : IDamageable, IHealable
{
    // 私有字段：生命值
    private int health = 100;
    
    // 属性：实现 IDamageable 接口的 Health 属性
    public int Health
    {
        get => health;  // 只读属性
    }
    
    // 方法：实现 IDamageable 接口的 TakeDamage 方法
    public void TakeDamage(int damage)
    {
        health -= damage;  // 扣减生命值
        if (health < 0) health = 0;  // 确保不低于0
        Console.WriteLine($"玩家受到 {damage} 点伤害，剩余 {health} HP");
    }
    
    // 方法：实现 IHealable 接口的 Heal 方法
    public void Heal(int amount)
    {
        health += amount;  // 增加生命值
        if (health > 100) health = 100;  // 上限100
        Console.WriteLine($"玩家恢复 {amount} 点生命，当前 {health} HP");
    }
}

// 敌人实现单个接口
class Enemy : IDamageable
{
    public int Health { get; private set; } = 50;
    
    public void TakeDamage(int damage)
    {
        Health -= damage;
        Console.WriteLine($"敌人受到 {damage} 点伤害，剩余 {Health} HP");
    }
}

// ===== 2. 抽象类的定义和使用 =====

// 抽象基类：武器
abstract class Weapon
{
    // 抽象属性：攻击力（子类必须实现）
    public abstract int AttackPower { get; }
    
    // 抽象方法：攻击行为（子类必须实现）
    public abstract void Attack();
    
    // 具体方法：显示武器信息（子类可以直接使用）
    public void ShowInfo()
    {
        Console.WriteLine($"武器名称: {Name}, 攻击力: {AttackPower}");
    }
    
    // 普通属性：武器名称
    public string Name { get; set; }
    
    // 抽象类的构造函数（用于初始化子类共有的属性）
    protected Weapon(string name)
    {
        Name = name;
    }
}

// 剑：继承自抽象类 Weapon
class Sword : Weapon
{
    // 构造函数：调用父类构造函数初始化名称
    public Sword() : base("剑") { }
    
    // 实现抽象属性
    public override int AttackPower => 50;
    
    // 实现抽象方法
    public override void Attack()
    {
        Console.WriteLine($"{Name} 挥砍攻击！造成 {AttackPower} 点伤害");
    }
}

// 弓：继承自抽象类 Weapon
class Bow : Weapon
{
    public Bow() : base("弓") { }
    
    public override int AttackPower => 30;
    
    public override void Attack()
    {
        Console.WriteLine($"{Name} 远程射箭攻击！造成 {AttackPower} 点伤害");
    }
}

// ===== 3. 接口与抽象类的选择 =====

// 什么时候用接口？
// - 定义行为规范，不关心实现细节
// - 需要多继承（类可以实现多个接口）
// - 各种不相关的类都需要实现同一行为

// 什么时候用抽象类？
// - 需要提供一些公共实现
// - 需要使用字段
// - 类之间有明确的 "是..." 关系（is-a）

// 例如：游戏中不同类型的武器
// 抽象类适合：因为所有武器都是 Weapon，有共同属性（名称、攻击力计算逻辑等）
// 接口也适合：如果需要让武器、玩家、敌人都实现"可伤害"行为
```

### 补充说明：接口的默认实现（C# 8.0+）

```csharp
// C# 8.0 引入了接口的默认实现
interface ILogger
{
    // 传统方法声明
    void Log(string message);
    
    // 默认实现：使用默认格式输出
    void LogWithTimestamp(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }
}

// 实现类只需要实现必须的方法，默认方法可以直接使用
class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
    // LogWithTimestamp 使用默认实现
}

// 使用
var logger = new ConsoleLogger();
logger.LogWithTimestamp("测试消息");  // 使用默认实现
```

### 习题练习

#### 练习 2.2.1
定义一个 `IPlayable` 接口，包含 `Play()`、`Pause()`、`Stop()` 方法。创建 `VideoGame` 和 `MusicPlayer` 实现类。

#### 练习 2.2.2
定义一个抽象类 `Shape`（图形），包含抽象方法 `CalculateArea()` 和 `CalculatePerimeter()`，以及具体方法 `ShowInfo()`。创建 `Circle` 和 `Rectangle` 子类。

#### 练习 2.2.3
创建一个系统，包含 `ISaveable`（可保存）和 `ILoadable`（可加载）接口，以及同时实现这两个接口的 `GameData` 类。

#### 练习 2.2.4
对比接口和抽象类的特点，说明在什么场景下选择哪个。

### 练习讲解

#### 练习 2.2.1 答案

```csharp
// 定义可播放接口
interface IPlayable
{
    // 播放
    void Play();
    
    // 暂停
    void Pause();
    
    // 停止
    void Stop();
}

// 视频游戏实现接口
class VideoGame : IPlayable
{
    // 属性：游戏标题
    public string Title { get; set; }
    
    // 属性：游戏状态
    private bool isPlaying = false;
    private bool isPaused = false;
    
    public void Play()
    {
        if (!isPlaying)
        {
            isPlaying = true;
            isPaused = false;
            Console.WriteLine($"开始游戏：{Title}");
        }
        else if (isPaused)
        {
            isPaused = false;
            Console.WriteLine($"继续游戏：{Title}");
        }
    }
    
    public void Pause()
    {
        if (isPlaying && !isPaused)
        {
            isPaused = true;
            Console.WriteLine($"暂停游戏：{Title}");
        }
    }
    
    public void Stop()
    {
        isPlaying = false;
        isPaused = false;
        Console.WriteLine($"停止游戏：{Title}");
    }
}

// 音乐播放器实现接口
class MusicPlayer : IPlayable
{
    public string CurrentSong { get; private set; }
    private bool isPlaying = false;
    private bool isPaused = false;
    
    public void Play()
    {
        if (string.IsNullOrEmpty(CurrentSong))
        {
            Console.WriteLine("请先选择歌曲");
            return;
        }
        
        if (!isPlaying)
        {
            isPlaying = true;
            isPaused = false;
            Console.WriteLine($"播放音乐：{CurrentSong}");
        }
        else if (isPaused)
        {
            isPaused = false;
            Console.WriteLine($"继续播放：{CurrentSong}");
        }
    }
    
    public void Pause()
    {
        if (isPlaying && !isPaused)
        {
            isPaused = true;
            Console.WriteLine("暂停播放");
        }
    }
    
    public void Stop()
    {
        isPlaying = false;
        isPaused = false;
        Console.WriteLine("停止播放");
    }
    
    // 特有方法：选歌
    public void SelectSong(string song)
    {
        CurrentSong = song;
        Console.WriteLine($"已选择歌曲：{song}");
    }
}

// 测试
class Program
{
    static void Main()
    {
        // 使用接口引用实现类
        IPlayable game = new VideoGame { Title = "王者荣耀" };
        game.Play();   // 输出：开始游戏：王者荣耀
        game.Pause();  // 输出：暂停游戏：王者荣耀
        game.Stop();   // 输出：停止游戏：王者荣耀
        
        Console.WriteLine();
        
        IPlayable music = new MusicPlayer();
        music.Play();  // 输出：请先选择歌曲
        
        // 向下转型调用特有方法
        var player = (MusicPlayer)music;
        player.SelectSong("夜曲");
        player.Play();  // 输出：播放音乐：夜曲
    }
}
```

#### 练习 2.2.2 答案

```csharp
// 抽象基类：图形
abstract class Shape
{
    // 抽象方法：计算面积（子类必须实现）
    public abstract double CalculateArea();
    
    // 抽象方法：计算周长（子类必须实现）
    public abstract double CalculatePerimeter();
    
    // 具体方法：显示图形信息（可直接使用或重写）
    public void ShowInfo()
    {
        Console.WriteLine($"面积: {CalculateArea():F2}, 周长: {CalculatePerimeter():F2}");
    }
    
    // 抽象类可以有属性
    public abstract string ShapeType { get; }
}

// 圆形
class Circle : Shape
{
    public double Radius { get; }
    
    public Circle(double radius)
    {
        Radius = radius;
    }
    
    // 实现抽象方法：面积 = πr²
    public override double CalculateArea() => Math.PI * Radius * Radius;
    
    // 实现抽象方法：周长 = 2πr
    public override double CalculatePerimeter() => 2 * Math.PI * Radius;
    
    // 实现抽象属性
    public override string ShapeType => "圆形";
}

// 矩形
class Rectangle : Shape
{
    public double Width { get; }
    public double Height { get; }
    
    public Rectangle(double width, double height)
    {
        Width = width;
        Height = height;
    }
    
    // 实现抽象方法：面积 = 宽 × 高
    public override double CalculateArea() => Width * Height;
    
    // 实现抽象方法：周长 = 2 × (宽 + 高)
    public override double CalculatePerimeter() => 2 * (Width + Height);
    
    public override string ShapeType => "矩形";
}

// 测试
class Program
{
    static void Main()
    {
        Shape[] shapes = new Shape[]
        {
            new Circle(5),
            new Rectangle(4, 6)
        };
        
        foreach (Shape shape in shapes)
        {
            Console.WriteLine($"图形类型: {shape.ShapeType}");
            shape.ShowInfo();
        }
    }
}
```

#### 练习 2.2.3 答案

```csharp
// 定义可保存接口
interface ISaveable
{
    // 保存数据到指定路径
    void Save(string filePath);
    
    // 获取保存所需的数据
    string GetSaveData();
}

// 定义可加载接口
interface ILoadable
{
    // 从指定路径加载数据
    void Load(string filePath);
    
    // 应用加载的数据
    void ApplyLoadData(string data);
}

// 游戏数据类同时实现两个接口
class GameData : ISaveable, ILoadable
{
    // 游戏数据属性
    public int PlayerLevel { get; set; }
    public string PlayerName { get; set; }
    public int Score { get; set; }
    public List<string> Inventory { get; set; } = new List<string>();
    
    // 实现 ISaveable
    public string GetSaveData()
    {
        // 将数据序列化为 JSON 格式
        return $"{PlayerName},{PlayerLevel},{Score},{string.Join(",", Inventory)}";
    }
    
    public void Save(string filePath)
    {
        string data = GetSaveData();
        System.IO.File.WriteAllText(filePath, data);
        Console.WriteLine($"游戏数据已保存到: {filePath}");
    }
    
    // 实现 ILoadable
    public void ApplyLoadData(string data)
    {
        // 解析数据
        string[] parts = data.Split(',');
        if (parts.Length >= 3)
        {
            PlayerName = parts[0];
            PlayerLevel = int.Parse(parts[1]);
            Score = int.Parse(parts[2]);
            
            // 解析物品栏
            Inventory.Clear();
            if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3]))
            {
                Inventory.AddRange(parts[3].Split(','));
            }
        }
    }
    
    public void Load(string filePath)
    {
        if (System.IO.File.Exists(filePath))
        {
            string data = System.IO.File.ReadAllText(filePath);
            ApplyLoadData(data);
            Console.WriteLine($"游戏数据已从: {filePath} 加载");
        }
        else
        {
            Console.WriteLine($"文件不存在: {filePath}");
        }
    }
}

// 测试
class Program
{
    static void Main()
    {
        // 创建游戏数据
        GameData data = new GameData
        {
            PlayerName = "玩家1",
            PlayerLevel = 10,
            Score = 5000,
            Inventory = new List<string> { "剑", "盾牌", "药水" }
        };
        
        string path = "save.txt";
        
        // 使用 ISaveable 接口保存
        ISaveable saveable = data;
        saveable.Save(path);
        
        // 创建新数据并从文件加载
        GameData newData = new GameData();
        
        // 使用 ILoadable 接口加载
        ILoadable loadable = newData;
        loadable.Load(path);
        
        // 显示加载的数据
        Console.WriteLine($"玩家: {newData.PlayerName}, 等级: {newData.PlayerLevel}, 分数: {newData.Score}");
    }
}
```

#### 练习 2.2.4 答案

```
接口 vs 抽象类的选择场景：

【选择接口的情况】
1. 定义行为规范，而不关心实现细节
   - 例如：IComparable（可比较）、ICloneable（可克隆）
   
2. 需要多继承
   - 类已经有一个父类，但还需要实现其他行为
   - 例如：class MyClass : ParentClass, ISerializable, IDisposal
   
3. 各种不相关的类需要实现同一行为
   - 例如：IEnumerable 接口可以让数组、List、Dictionary 等都可以被遍历

【选择抽象类的情况】
1. 需要提供一些公共实现
   - 例如：abstract class Animal 有公共的 Age 属性和具体的 Eat() 方法
   
2. 需要使用非静态字段
   - 接口不能有实例字段，抽象类可以
   
3. 类之间有明确的 "是..." 关系（is-a）
   - 例如：Dog 是 Animal的一种 → class Dog : Animal
   - 而非 "能做什么" 关系（can-do）
   - 例如：Dog 能 Fly → interface IFlyable

【总结】
- 接口 = "能做什么"（能力/行为）
- 抽象类 = "是什么"（类型/本质）
```

### 相关知识点

- 2.1 继承与多态 - 接口和抽象类都是实现多态的重要方式
- 2.4 泛型 - 泛型接口是常见的用法，如 `IComparer<T>`、`IEnumerable<T>`

---

## 2.3 委托与事件

### 概念讲解

**委托（Delegate）**是 C# 中一种特殊的类型，它本质上是一个**类型安全的函数指针**。委托可以指向某个方法，并且像调用方法一样调用被引用的方法。委托使得方法可以作为参数传递，这是 C# 实现回调、事件、LINQ 等功能的基础。

```
委托的工作原理：

┌─────────────────────────────────────────┐
│         委托类型定义                      │
│  delegate int Calculator(int a, int b); │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│         委托实例（指向方法）               │
│  Calculator add = Add;                  │
│  Calculator sub = Sub;                  │
│  Calculator mul = (a,b) => a * b;       │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│              调用                         │
│  add(3, 5)  →  8                       │
│  mul(3, 5)  →  15                       │
└─────────────────────────────────────────┘
```

**事件（Event）**是基于委托的**发布-订阅模式**实现。事件让一个对象能够通知其他对象发生了特定情况，而不需要了解谁在监听。

```
事件的工作流程（发布-订阅模式）：

发布者（Publisher）          订阅者（Subscriber）
┌─────────────────────┐      ┌─────────────────────┐
│  玩家类             │      │  UI类               │
│  - OnDied 事件      │      │  - OnPlayerDied()  │
│  - 当玩家死亡时触发 │ ────→│    处理方法        │
└─────────────────────┘      └─────────────────────┘

步骤：
1. 订阅者注册到事件（+=）
2. 发布者触发事件（?.Invoke()）
3. 所有订阅者的处理方法被调用
4. 订阅者取消注册（-=）
```

**内置委托类型**：

| 委托类型 | 签名 | 用途 | 示例 |
|----------|------|------|------|
| Action | void Action(T) | 无返回值的操作 | `Action<string> print = Console.WriteLine` |
| Func<T, TResult> | TResult Func(T) | 有返回值的函数 | `Func<int, int, int> add = (a,b) => a + b` |
| Predicate<T> | bool Predicate(T) | 返回 bool 的判断 | `Predicate<int> isEven = n => n % 2 == 0` |

### 举例说明

```csharp
// ===== 1. 委托的定义和使用 =====

// 定义一个委托类型：接受两个 int 参数，返回 int 结果
delegate int Calculator(int a, int b);

// 静态方法
class MathOperations
{
    // 加法
    public static int Add(int a, int b) => a + b;
    
    // 减法
    public static int Subtract(int a, int b) => a - b;
}

class Program
{
    static void Main()
    {
        // 将方法赋值给委托实例
        Calculator add = MathOperations.Add;      // 引用静态方法
        Calculator subtract = MathOperations.Subtract;
        
        // 使用 Lambda 表达式（匿名方法）
        Calculator multiply = (a, b) => a * b;     // 乘法
        Calculator divide = (a, b) => b != 0 ? a / b : 0;  // 除法
        
        // 调用委托
        Console.WriteLine(add(3, 5));       // 输出: 8
        Console.WriteLine(subtract(10, 3)); // 输出: 7
        Console.WriteLine(multiply(4, 6));  // 输出: 24
        Console.WriteLine(divide(20, 4));  // 输出: 5
        
        // 多播委托：一个委托可以指向多个方法
        Calculator combined = add;
        combined += multiply;  // 添加另一个方法
        combined += divide;    // 再添加一个方法
        
        // 调用多播委托：依次执行所有方法，返回最后一个结果
        Console.WriteLine(combined(10, 2)); // 输出: 5 (10+10=20, 20*2=20, 20/2=5)
    }
}

// ===== 2. 使用内置委托类型 =====

class BuiltInDelegates
{
    static void Main()
    {
        // Action: 无返回值的委托
        // Action<T1, T2> 表示接受两个参数，无返回值
        Action<string, int> greet = (name, times) =>
        {
            for (int i = 0; i < times; i++)
                Console.WriteLine($"你好，{name}！");
        };
        greet("张三", 3);  // 输出3次问候
        
        // Func: 有返回值的委托
        // Func<T1, T2, TResult> 表示接受两个参数，返回 TResult
        Func<int, int, string> calculate = (a, b) =>
        {
            return $"和: {a + b}, 差: {a - b}";
        };
        Console.WriteLine(calculate(10, 5));  // 和: 15, 差: 5
        
        // Predicate: 返回 bool 的委托
        // Predicate<T> 等价于 Func<T, bool>
        Predicate<int> isPositive = n => n > 0;
        Console.WriteLine(isPositive(5));   // True
        Console.WriteLine(isPositive(-3));  // False
    }
}

// ===== 3. 事件的定义和使用 =====

// 事件发布者：玩家类
class Player
{
    // 定义事件：血量变化时触发
    // event 关键字确保事件只能在本类中触发，外部只能 += 或 -=
    public event Action<int> OnHealthChanged;
    
    // 定义事件：玩家死亡时触发
    public event EventHandler OnDied;
    
    // 私有字段：生命值
    private int health = 100;
    
    // 属性：生命值（通过属性设置可以触发事件）
    public int Health
    {
        get => health;
        set
        {
            health = value;
            
            // 触发血量变化事件
            // ?.Invoke() 是空值判断的安全调用
            OnHealthChanged?.Invoke(value);
            
            // 检查是否死亡
            if (health <= 0)
            {
                // 触发死亡事件
                OnDied?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    
    // 方法：受到伤害
    public void TakeDamage(int damage)
    {
        if (damage > 0)
        {
            Health -= damage;
            Console.WriteLine($"受到 {damage} 点伤害，当前生命: {Health}");
        }
    }
    
    // 方法：恢复生命
    public void Heal(int amount)
    {
        if (amount > 0)
        {
            Health += amount;
            Console.WriteLine($"恢复 {amount} 点生命，当前生命: {Health}");
        }
    }
}

// 事件订阅者：游戏UI类
class GameUI
{
    // 订阅者方法：处理血量变化
    public void OnHealthChanged(int health)
    {
        // 根据血量显示不同颜色
        if (health > 70)
            Console.WriteLine("[UI] 生命值充足，显示绿色");
        else if (health > 30)
            Console.WriteLine("[UI] 生命值警告，显示黄色");
        else
            Console.WriteLine("[UI] 生命值危险，显示红色");
    }
    
    // 订阅者方法：处理玩家死亡
    public void OnPlayerDied(object sender, EventArgs e)
    {
        Console.WriteLine("[UI] 游戏结束！显示死亡画面");
    }
}

// 事件订阅者：日志系统
class Logger
{
    public void LogHealthChange(int health)
    {
        Console.WriteLine($"[日志] 玩家生命值变化为: {health}");
    }
    
    public void LogDeath(object sender, EventArgs e)
    {
        Console.WriteLine("[日志] 玩家已死亡");
    }
}

class EventDemo
{
    static void Main()
    {
        // 创建发布者和订阅者
        Player player = new Player();
        GameUI ui = new GameUI();
        Logger logger = new Logger();
        
        // 订阅事件：使用 += 添加事件处理程序
        player.OnHealthChanged += ui.OnHealthChanged;      // UI订阅血量变化
        player.OnHealthChanged += logger.LogHealthChange;  // 日志订阅血量变化
        player.OnDied += ui.OnPlayerDied;                   // UI订阅死亡
        player.OnDied += logger.LogDeath;                  // 日志订阅死亡
        
        // 触发事件
        Console.WriteLine("=== 第一次伤害 ===");
        player.TakeDamage(30);  // 血量变为70，触发 OnHealthChanged
        
        Console.WriteLine("\n=== 第二次伤害 ===");
        player.TakeDamage(50);  // 血量变为20，触发 OnHealthChanged
        
        Console.WriteLine("\n=== 第三次伤害 ===");
        player.TakeDamage(30);  // 血量变为0，触发 OnHealthChanged 和 OnDied
        
        // 取消订阅：使用 -= 移除事件处理程序
        player.OnHealthChanged -= logger.LogHealthChange;
        
        Console.WriteLine("\n=== 恢复生命 ===");
        player.Heal(50);  // 血量变为50，只触发 UI 的处理（已取消日志）
    }
}

// ===== 4. 使用 EventHandler<T> =====

// 使用标准的 EventHandler<T> 模式
class PlayerWithEventHandler
{
    // 使用泛型 EventHandler<PlayerDamageEventArgs>
    public event EventHandler<PlayerDamageEventArgs> OnDamaged;
    
    private int health = 100;
    public int Health => health;
    
    public void TakeDamage(int damage, string cause)
    {
        health -= damage;
        
        // 创建事件参数并触发事件
        OnDamaged?.Invoke(this, new PlayerDamageEventArgs(damage, cause, health));
    }
}

// 自定义事件参数
class PlayerDamageEventArgs : EventArgs
{
    public int Damage { get; }        // 伤害值
    public string Cause { get; }      // 伤害原因
    public int RemainingHealth { get; } // 剩余生命
    
    public PlayerDamageEventArgs(int damage, string cause, int remaining)
    {
        Damage = damage;
        Cause = cause;
        RemainingHealth = remaining;
    }
}

// 使用
class Program2
{
    static void Main()
    {
        PlayerWithEventHandler player = new PlayerWithEventHandler();
        
        // 订阅事件
        player.OnDamaged += (sender, e) =>
        {
            Console.WriteLine($"玩家受到 {e.Damage} 点{e.Cause}伤害，剩余 {e.RemainingHealth} HP");
        };
        
        player.TakeDamage(50, "怪物攻击");  // 输出：玩家受到 50 点怪物攻击伤害，剩余 50 HP
    }
}
```

### 补充说明：Lambda 表达式与委托

```csharp
// Lambda 表达式本质上就是委托的简写形式

// 完整写法
Func<int, int, int> add1 = delegate(int a, int b) { return a + b; };

// Lambda 表达式（最常用）
Func<int, int, int> add2 = (a, b) => a + b;

// 单参数可以省略括号
Func<int, int> double = x => x * 2;

// 无参数
Action greeting = () => Console.WriteLine("你好");

// 多行语句需要使用大括号
Func<int, int> factorial = n =>
{
    int result = 1;
    for (int i = 2; i <= n; i++)
        result *= i;
    return result;
};
```

### 习题练习

#### 练习 2.3.1
创建一个自定义委托 `OperationDelegate`，接受两个 int 参数，返回 int。实现加法、减法、乘法、除法（考虑除数为0的情况），使用委托调用。

#### 练习 2.3.2
使用 `EventHandler<T>` 实现一个温度监控系统，包含温度变化事件和温度警戒事件。

#### 练习 2.3.3
使用内置的 `Action` 和 `Func` 实现一个简单的消息处理系统。

#### 练习 2.3.4
创建一个简单的观察者模式：Subject（主题）类包含订阅和通知方法，Observer（观察者）接口包含 Update 方法。

### 练习讲解

#### 练习 2.3.1 答案

```csharp
// 定义委托类型
delegate int OperationDelegate(int a, int b);

class Program
{
    // 各种操作的静态方法
    static int Add(int a, int b) => a + b;
    static int Subtract(int a, int b) => a - b;
    static int Multiply(int a, int b) => a * b;
    static int Divide(int a, int b) => b != 0 ? a / b : 0;
    
    static void Main()
    {
        // 创建委托实例指向不同方法
        OperationDelegate add = Add;
        OperationDelegate subtract = Subtract;
        OperationDelegate multiply = Multiply;
        OperationDelegate divide = Divide;
        
        // 使用委托调用
        Console.WriteLine($"10 + 5 = {add(10, 5)}");       // 15
        Console.WriteLine($"10 - 5 = {subtract(10, 5)}"); // 5
        Console.WriteLine($"10 * 5 = {multiply(10, 5)}"); // 50
        Console.WriteLine($"10 / 5 = {divide(10, 5)}");  // 2
        Console.WriteLine($"10 / 0 = {divide(10, 0)}");  // 0
        
        // 多播委托
        Console.WriteLine("\n多播委托演示：");
        OperationDelegate chain = add;
        chain += multiply;
        
        // 遍历多播委托的所有目标方法
        foreach (var d in chain.GetInvocationList())
        {
            // 使用 DynamicInvoke 调用，返回 object 类型
            Console.WriteLine($"结果: {d.DynamicInvoke(10, 5)}");
        }
    }
}
```

#### 练习 2.3.2 答案

```csharp
// 温度数据事件参数
class TemperatureEventArgs : EventArgs
{
    public double Temperature { get; }   // 当前温度
    public double PreviousTemperature { get; }  // 之前温度
    public DateTime Time { get; }        // 时间
    
    public TemperatureEventArgs(double temp, double prev, DateTime time)
    {
        Temperature = temp;
        PreviousTemperature = prev;
        Time = time;
    }
}

// 温度监控系统
class TemperatureMonitor
{
    // 温度变化事件
    public event EventHandler<TemperatureEventArgs> OnTemperatureChanged;
    
    // 温度警戒事件
    public event EventHandler<TemperatureEventArgs> OnTemperatureWarning;
    
    private double currentTemp = 20.0;
    public double CurrentTemp => currentTemp;
    
    // 警戒温度阈值
    public double WarningThreshold { get; set; } = 35.0;
    public double CriticalThreshold { get; set; } = 40.0;
    
    // 设置温度
    public void SetTemperature(double newTemp)
    {
        double prevTemp = currentTemp;
        currentTemp = newTemp;
        
        // 触发温度变化事件
        OnTemperatureChanged?.Invoke(this, 
            new TemperatureEventArgs(currentTemp, prevTemp, DateTime.Now));
        
        // 检查是否触发警戒
        if (currentTemp >= CriticalThreshold)
        {
            Console.WriteLine("🔴 温度达到危险级别！");
        }
        else if (currentTemp >= WarningThreshold)
        {
            Console.WriteLine("🟡 温度达到警戒级别！");
        }
    }
}

// 使用
class Program
{
    static void Main()
    {
        TemperatureMonitor monitor = new TemperatureMonitor();
        
        // 订阅温度变化事件
        monitor.OnTemperatureChanged += (sender, e) =>
        {
            Console.WriteLine($"温度变化: {e.PreviousTemperature:F1}°C → {e.Temperature:F1}°C [{e.Time:HH:mm:ss}]");
        };
        
        // 订阅警戒事件
        monitor.OnTemperatureWarning += (sender, e) =>
        {
            Console.WriteLine($"⚠️ 温度警告: 当前 {e.Temperature:F1}°C，超过阈值 {monitor.WarningThreshold}°C");
        };
        
        // 模拟温度变化
        monitor.SetTemperature(22.0);  // 正常
        monitor.SetTemperature(30.0);   // 上升
        monitor.SetTemperature(36.0);  // 达到警戒
        monitor.SetTemperature(42.0);   // 危险
    }
}
```

#### 练习 2.3.3 答案

```csharp
class MessageSystem
{
    // 使用 Action 定义消息处理（无返回值）
    public Action<string> OnMessageReceived { get; set; }
    
    // 使用 Func 定义消息转换（有返回值）
    public Func<string, string> MessageProcessor { get; set; }
    
    // 接收消息
    public void ReceiveMessage(string message)
    {
        Console.WriteLine($"📥 接收原始消息: {message}");
        
        // 使用 Func 处理消息
        if (MessageProcessor != null)
        {
            message = MessageProcessor(message);
            Console.WriteLine($"🔄 处理后消息: {message}");
        }
        
        // 使用 Action 触发回调
        OnMessageReceived?.Invoke(message);
    }
}

class Program
{
    static void Main()
    {
        var messageSystem = new MessageSystem();
        
        // 使用 Action 处理接收到的消息
        messageSystem.OnMessageReceived = msg =>
        {
            Console.WriteLine($"📢 广播消息: {msg}");
            
            // 根据消息内容做不同处理
            if (msg.Contains("紧急"))
            {
                Console.WriteLine("🚨 发送紧急通知！");
            }
            else if (msg.Contains("通知"))
            {
                Console.WriteLine("📢 普通通知");
            }
        };
        
        // 使用 Func 处理消息
        messageSystem.MessageProcessor = msg =>
        {
            // 示例：添加时间戳
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}";
        };
        
        // 发送消息
        messageSystem.ReceiveMessage("系统维护通知");
        Console.WriteLine();
        messageSystem.ReceiveMessage("紧急任务");
    }
}
```

#### 练习 2.3.4 答案

```csharp
// 观察者接口
interface IObserver
{
    // 接收到通知时执行的方法
    void Update(string message);
}

// 主题（被观察者）接口
interface ISubject
{
    // 添加观察者
    void Attach(IObserver observer);
    
    // 移除观察者
    void Detach(IObserver observer);
    
    // 通知所有观察者
    void Notify(string message);
}

// 具体主题实现
class NewsAgency : ISubject
{
    // 观察者列表
    private List<IObserver> observers = new List<IObserver>();
    
    // 新闻标题
    private string latestNews;
    public string LatestNews => latestNews;
    
    // 添加观察者
    public void Attach(IObserver observer)
    {
        observers.Add(observer);
        Console.WriteLine($"已添加观察者");
    }
    
    // 移除观察者
    public void Detach(IObserver observer)
    {
        observers.Remove(observer);
        Console.WriteLine($"已移除观察者");
    }
    
    // 发布新闻，通知所有观察者
    public void PublishNews(string news)
    {
        latestNews = news;
        Console.WriteLine($"\n📰 发布新闻: {news}");
        Notify(news);
    }
    
    // 通知所有观察者
    public void Notify(string message)
    {
        foreach (var observer in observers)
        {
            observer.Update(message);
        }
    }
}

// 具体观察者实现：新闻订阅者
class NewsSubscriber : IObserver
{
    public string Name { get; }
    
    public NewsSubscriber(string name)
    {
        Name = name;
    }
    
    public void Update(string message)
    {
        Console.WriteLine($"  → {Name} 收到通知: {message}");
    }
}

// 具体观察者实现：新闻网站
class NewsWebsite : IObserver
{
    public string WebsiteName { get; }
    
    public NewsWebsite(string name)
    {
        WebsiteName = name;
    }
    
    public void Update(string message)
    {
        Console.WriteLine($"  → {WebsiteName} 更新: {message}");
    }
}

class Program
{
    static void Main()
    {
        // 创建主题
        var newsAgency = new NewsAgency();
        
        // 创建观察者
        var subscriber1 = new NewsSubscriber("张三");
        var subscriber2 = new NewsSubscriber("李四");
        var website1 = new NewsWebsite("新浪新闻");
        var website2 = new NewsWebsite("腾讯新闻");
        
        // 订阅
        newsAgency.Attach(subscriber1);
        newsAgency.Attach(subscriber2);
        newsAgency.Attach(website1);
        newsAgency.Attach(website2);
        
        // 发布新闻（所有订阅者都会收到通知）
        newsAgency.PublishNews("C# 9.0 正式发布！");
        
        Console.WriteLine();
        
        // 取消订阅
        newsAgency.Detach(subscriber2);
        
        // 再次发布（只有三个订阅者收到）
        newsAgency.PublishNews(".NET 5.0 即将发布！");
    }
}
```

### 相关知识点

- 2.4 泛型 - 泛型委托如 `Action<T>`、`Func<T, TResult>` 是常用模式
- 2.7 LINQ - LINQ 底层基于委托实现

---

## 2.4 泛型

### 概念讲解

**泛型（Generics）**是 C# 2.0 引入的重要特性，它允许编写**可复用的代码**，同时支持**多种数据类型**。泛型的核心思想是**把类型当作参数**，在使用时再指定具体类型。

```
泛型的优势：

【不使用泛型】
┌────────────┐  ┌────────────┐  ┌────────────┐
│ IntBox      │  │ StringBox  │  │ FloatBox   │
│ ────────    │  │ ────────   │  │ ────────   │
│ int value;  │  │ string val;│  │ float val; │
└────────────┘  └────────────┘  └────────────┘
每个类型都要写一个类！

【使用泛型】
┌─────────────────────────────┐
│  Box<T>  (一个类兼容所有类型)  │
│  ─────────────────────────   │
│  T value;  (T 是类型参数)    │
│  { get; set; }              │
└─────────────────────────────┘

Box<int>     → int 类型的盒子
Box<string>  → string 类型的盒子
Box<float>  → float 类型的盒子
```

**泛型的好处**：
1. **代码复用**：一套代码支持多种类型
2. **类型安全**：编译时检查类型错误
3. **性能提升**：避免装箱拆箱（对于值类型）

```
装箱 vs 泛型（性能对比）：

【ArrayList（装箱）】
ArrayList list = new ArrayList();
list.Add(123);        // int → object 装箱（额外内存分配）
int num = (int)list[0]; // object → int 拆箱（类型转换开销）
// 每次操作都有性能损耗！

【List<T>（无装箱）】
List<int> list = new List<int>();
list.Add(123);        // 直接存储，无装箱
int num = list[0];    // 直接使用，无拆箱
// 零损耗！
```

**泛型约束（where）**：

泛型约束用于限制泛型类型必须满足的条件：

| 约束 | 说明 | 示例 |
|------|------|------|
| `class` | 必须是引用类型 | `T : class` |
| `struct` | 必须是值类型 | `T : struct` |
| `new()` | 必须有无参构造函数 | `T : new()` |
| `BaseClass` | 必须是指定基类的子类 | `T : MonoBehaviour` |
| `IInterface` | 必须实现指定接口 | `T : IComparable` |

### 举例说明

```csharp
// ===== 1. 泛型类 =====

// 定义泛型类：Box<T>，T 是类型参数
class Box<T>
{
    // 泛型属性
    public T Content { get; set; }
    
    // 泛型方法
    public void ShowContent()
    {
        Console.WriteLine($"内容: {Content}");
    }
    
    // 获取内容类型
    public Type GetContentType()
    {
        return typeof(T);
    }
}

class GenericClassDemo
{
    static void Main()
    {
        // 使用不同的类型实例化泛型类
        Box<int> intBox = new Box<int> { Content = 123 };
        Box<string> strBox = new Box<string> { Content = "Hello" };
        Box<double> dblBox = new Box<double> { Content = 3.14 };
        
        intBox.ShowContent();    // 内容: 123
        strBox.ShowContent();     // 内容: Hello
        dblBox.ShowContent();     // 内容: 3.14
        
        // 不同的泛型类型是不兼容的
        // intBox = strBox;  // 编译错误！类型不兼容
    }
}

// ===== 2. 泛型方法 =====

// 泛型方法
class ArrayHelper
{
    // 交换两个元素（使用 ref 传递）
    public static void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b;
        b = temp;
    }
    
    // 查找元素在数组中的索引
    public static int FindIndex<T>(T[] array, T item)
    {
        for (int i = 0; i < array.Length; i++)
        {
            // 使用 EqualityComparer 处理相等性比较
            if (EqualityComparer<T>.Default.Equals(array[i], item))
                return i;
        }
        return -1;
    }
    
    // 打印数组元素
    public static void PrintArray<T>(T[] array)
    {
        Console.WriteLine($"[{string.Join(", ", array)}]");
    }
}

class GenericMethodDemo
{
    static void Main()
    {
        // 使用泛型方法
        int x = 5, y = 10;
        Console.WriteLine($"交换前: x={x}, y={y}");
        ArrayHelper.Swap(ref x, ref y);
        Console.WriteLine($"交换后: x={x}, y={y}");  // x=10, y=5
        
        string s1 = "apple", s2 = "banana";
        ArrayHelper.Swap(ref s1, ref s2);
        Console.WriteLine($"交换后: s1={s1}, s2={s2}");  // s1=banana, s2=apple
        
        // 查找索引
        string[] colors = { "红", "黄", "蓝", "绿" };
        int index = ArrayHelper.FindIndex(colors, "蓝");
        Console.WriteLine($"蓝色在索引: {index}");  // 2
    }
}

// ===== 3. 泛型约束 =====

// 约束1：要求 T 是引用类型（class）
class RefClass<T> where T : class
{
    public T Value { get; set; }
    
    // 可以使用 null
    public void Clear() => Value = null;
}

// 约束2：要求 T 是值类型（struct）
class ValueClass<T> where T : struct
{
    public T Value { get; set; }
    
    // 可以使用默认值
    public void Reset() => Value = default;
}

// 约束3：要求 T 有无参构造函数（new()）
class Factory<T> where T : new()
{
    public T Create()
    {
        return new T();  // 可以直接创建实例
    }
}

// 约束4：要求 T 实现了特定接口（IComparable）
class Comparer<T> where T : IComparable
{
    public bool IsGreater(T a, T b)
    {
        return a.CompareTo(b) > 0;
    }
}

// 约束5：要求 T 是特定类的子类
class Manager<T> where T : MonoBehaviour
{
    // Unity 中的组件管理示例
    public T GetComponent()
    {
        // 假设在某个 GameObject 上获取组件
        // return someGameObject.GetComponent<T>();
        return default;
    }
}

// 多个约束
class MultiConstraint<T> where T : class, new(), IComparable
{
    // T 必须是引用类型、有无参构造函数、实现了 IComparable
    public T CreateAndCompare(T other)
    {
        T instance = new T();  // 因为约束了 new()，所以可以 new
        return instance.CompareTo(other) > 0 ? instance : other;
    }
}

// ===== 4. 泛型接口 =====

// 定义泛型接口
interface IRepository<T>
{
    // 获取所有实体
    List<T> GetAll();
    
    // 根据 ID 获取实体
    T GetById(int id);
    
    // 添加实体
    void Add(T entity);
    
    // 更新实体
    void Update(T entity);
    
    // 删除实体
    void Delete(int id);
}

// 实现泛型接口
class PlayerRepository : IRepository<Player>
{
    private List<Player> players = new List<Player>();
    
    public List<Player> GetAll() => players;
    
    public Player GetById(int id)
    {
        return players.FirstOrDefault(p => p.Id == id);
    }
    
    public void Add(Player player)
    {
        players.Add(player);
    }
    
    public void Update(Player player)
    {
        int index = players.FindIndex(p => p.Id == player.Id);
        if (index >= 0)
            players[index] = player;
    }
    
    public void Delete(int id)
    {
        players.RemoveAll(p => p.Id == id);
    }
}

// 数据模型
class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
}
```

### 补充说明：协变与逆变

```csharp
// 协变（out 关键字）：返回类型可以是子类
// 只能用在返回类型上
interface IProducer<out T>
{
    T Produce();  // T 作为返回值
}

// 使用协变
class Animal { }
class Dog : Animal { }

IProducer<Dog> dogProducer = () => new Dog();
IProducer<Animal> animalProducer = dogProducer;  // 允许！Dog → Animal

// 逆变（in 关键字）：参数类型可以是父类
// 只能用在参数上
interface IConsumer<in T>
{
    void Consume(T item);  // T 作为参数
}

// 使用逆变
IConsumer<Animal> animalConsumer = animal => Console.WriteLine(animal);
IConsumer<Dog> dogConsumer = animalConsumer;  // 允许！Animal → Dog
```

### 习题练习

#### 练习 2.4.1
创建一个泛型 `Stack<T>` 类，实现 Push（入栈）、Pop（出栈）、Peek（查看栈顶）、Count（元素数量）方法。

#### 练习 2.4.2
创建一个泛型方法 `FindMax<T>`，接受一个 T 类型数组，返回最大值。要求 T 实现 `IComparable` 接口。

#### 练习 2.4.3
创建一个泛型缓存类 `Cache<T>`，使用约束确保 T 是引用类型，并包含获取和设置缓存的方法。

#### 练习 2.4.4
创建一个泛型接口 `IComparator<T>`，包含比较方法。实现 `StringComparator` 和 `IntComparator`。

### 练习讲解

#### 练习 2.4.1 答案

```csharp
// 泛型栈类
class Stack<T>
{
    // 内部使用 List 存储元素
    private List<T> items = new List<T>();
    
    // 入栈：添加元素到顶部
    public void Push(T item)
    {
        items.Add(item);
    }
    
    // 出栈：移除并返回顶部元素
    public T Pop()
    {
        if (items.Count == 0)
            throw new InvalidOperationException("栈为空");
        
        // 获取最后一个元素
        T item = items[items.Count - 1];
        // 移除最后一个元素
        items.RemoveAt(items.Count - 1);
        return item;
    }
    
    // 查看栈顶：返回顶部元素但不移除
    public T Peek()
    {
        if (items.Count == 0)
            throw new InvalidOperationException("栈为空");
        
        return items[items.Count - 1];
    }
    
    // 元素数量
    public int Count => items.Count;
    
    // 是否为空
    public bool IsEmpty => items.Count == 0;
}

// 测试
class Program
{
    static void Main()
    {
        // 创建整数栈
        Stack<int> intStack = new Stack<int>();
        
        intStack.Push(1);
        intStack.Push(2);
        intStack.Push(3);
        
        Console.WriteLine($"栈顶元素: {intStack.Peek()}");  // 3
        Console.WriteLine($"元素数量: {intStack.Count}");   // 3
        
        Console.WriteLine($"弹出: {intStack.Pop()}");  // 3
        Console.WriteLine($"弹出: {intStack.Pop()}");  // 2
        Console.WriteLine($"剩余数量: {intStack.Count}"); // 1
        
        // 创建字符串栈
        Stack<string> strStack = new Stack<string>();
        strStack.Push("Hello");
        strStack.Push("World");
        Console.WriteLine($"弹出: {strStack.Pop()}");  // World
    }
}
```

#### 练习 2.4.2 答案

```csharp
// 泛型方法：找最大值
class ArrayOperations
{
    // 约束：T 必须实现 IComparable 接口，以便比较大小
    public static T FindMax<T>(T[] array) where T : IComparable
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("数组不能为空");
        
        // 假设第一个是最大值
        T max = array[0];
        
        // 遍历数组比较
        for (int i = 1; i < array.Length; i++)
        {
            // CompareTo 返回 > 0 表示当前元素大于最大值
            if (array[i].CompareTo(max) > 0)
            {
                max = array[i];
            }
        }
        
        return max;
    }
    
    // 泛型方法：找最小值
    public static T FindMin<T>(T[] array) where T : IComparable
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("数组不能为空");
        
        T min = array[0];
        
        for (int i = 1; i < array.Length; i++)
        {
            if (array[i].CompareTo(min) < 0)
            {
                min = array[i];
            }
        }
        
        return min;
    }
}

// 测试
class Program
{
    static void Main()
    {
        // 整数数组
        int[] nums = { 3, 1, 4, 1, 5, 9, 2, 6 };
        Console.WriteLine($"最大值: {ArrayOperations.FindMax(nums)}");  // 9
        Console.WriteLine($"最小值: {ArrayOperations.FindMin(nums)}");  // 1
        
        // 浮点数数组
        double[] doubles = { 1.5, 2.8, 0.3, 5.7, 3.2 };
        Console.WriteLine($"最大值: {ArrayOperations.FindMax(doubles)}");  // 5.7
        
        // 字符串数组（按字母顺序比较）
        string[] words = { "apple", "banana", "cherry", "date" };
        Console.WriteLine($"最大值: {ArrayOperations.FindMax(words)}");  // date
    }
}
```

#### 练习 2.4.3 答案

```csharp
// 泛型缓存类
class Cache<T> where T : class
{
    // 缓存存储
    private T cachedValue;
    private DateTime? cacheTime;
    
    // 缓存过期时间（可配置）
    public TimeSpan ExpiryDuration { get; set; } = TimeSpan.FromMinutes(5);
    
    // 获取缓存
    public T Get(Func<T> factory)
    {
        // 检查缓存是否有效
        if (cachedValue != null && cacheTime.HasValue)
        {
            // 检查是否过期
            if (DateTime.Now - cacheTime.Value < ExpiryDuration)
            {
                Console.WriteLine("从缓存获取");
                return cachedValue;
            }
        }
        
        // 缓存无效或过期，创建新值
        Console.WriteLine("创建新值");
        cachedValue = factory();
        cacheTime = DateTime.Now;
        
        return cachedValue;
    }
    
    // 强制刷新缓存
    public void Invalidate()
    {
        cachedValue = null;
        cacheTime = null;
        Console.WriteLine("缓存已清除");
    }
    
    // 查看缓存状态
    public bool HasCache => cachedValue != null && cacheTime.HasValue;
}

// 使用
class Program
{
    static void Main()
    {
        // 创建缓存实例
        var playerCache = new Cache<PlayerInfo>();
        
        // 获取玩家信息（第一次，创建新值）
        var player = playerCache.Get(() => 
        {
            // 模拟从数据库加载
            return new PlayerInfo { Name = "张三", Level = 100 };
        });
        
        // 再次获取（从缓存）
        var player2 = playerCache.Get(() => new PlayerInfo { Name = "李四", Level = 50 });
        
        // 强制刷新
        playerCache.Invalidate();
        
        // 再次获取（创建新值）
        var player3 = playerCache.Get(() => new PlayerInfo { Name = "王五", Level = 30 });
    }
}

class PlayerInfo
{
    public string Name { get; set; }
    public int Level { get; set; }
}
```

#### 练习 2.4.4 答案

```csharp
// 泛型比较器接口
interface IComparator<T>
{
    // 比较两个值，返回负数、零、正数
    int Compare(T a, T b);
}

// 整数比较器
class IntComparator : IComparator<int>
{
    public int Compare(int a, int b)
    {
        return a.CompareTo(b);  // 使用 int 的 CompareTo 方法
    }
}

// 字符串比较器
class StringComparator : IComparator<string>
{
    public int Compare(string a, string b)
    {
        // 按长度比较，长度相同则按字典序
        int lengthCompare = a.Length.CompareTo(b.Length);
        if (lengthCompare != 0)
            return lengthCompare;
        
        return string.Compare(a, b, StringComparison.Ordinal);
    }
}

// 浮点数比较器
class DoubleComparator : IComparator<double>
{
    public int Compare(double a, double b)
    {
        // 处理精度问题
        const double epsilon = 0.0001;
        double diff = a - b;
        
        if (Math.Abs(diff) < epsilon)
            return 0;
        
        return diff > 0 ? 1 : -1;
    }
}

// 泛型排序器
class Sorter<T>
{
    // 使用比较器进行排序
    public void Sort(T[] array, IComparator<T> comparator)
    {
        // 简单的冒泡排序
        for (int i = 0; i < array.Length - 1; i++)
        {
            for (int j = 0; j < array.Length - i - 1; j++)
            {
                // 使用比较器比较
                if (comparator.Compare(array[j], array[j + 1]) > 0)
                {
                    // 交换
                    T temp = array[j];
                    array[j] = array[j + 1];
                    array[j + 1] = temp;
                }
            }
        }
    }
}

// 测试
class Program
{
    static void Main()
    {
        // 整数排序
        int[] numbers = { 5, 2, 8, 1, 9, 3 };
        var intComp = new IntComparator();
        var sorter = new Sorter<int>();
        
        sorter.Sort(numbers, intComp);
        Console.WriteLine($"排序后: {string.Join(", ", numbers)}");  // 1,2,3,5,8,9
        
        // 字符串排序
        string[] words = { "apple", "cat", "banana", "dog", "bird" };
        var strComp = new StringComparator();
        var strSorter = new Sorter<string>();
        
        strSorter.Sort(words, strComp);
        Console.WriteLine($"排序后: {string.Join(", ", words)}");  // cat, dog, bird, apple, banana (按长度)
    }
}
```

### 相关知识点

- 1.7 集合 - List<T>、Dictionary<TKey, TValue> 都是泛型集合
- 2.3 委托与事件 - 泛型委托 Action<T>、Func<T, TResult>
- 2.7 LINQ - LINQ 大量使用泛型

---

## 2.5 异步编程基础

### 概念讲解

**异步编程**是一种不阻塞当前线程的执行方式，特别适合处理耗时操作（如 I/O 操作、网络请求、数据库查询等）。传统的同步编程会导致线程等待完成，而异步编程可以让线程在等待期间处理其他任务，从而提高应用程序的吞吐量和响应性。

```
同步 vs 异步执行流程：

【同步执行】
┌─────────────────────────────┐
│ 主线程                      │
│ ┌─────────┐                │
│ │ 下载文件 │ (等待3秒)      │
│ └─────────┘                │
│ ┌─────────┐                │
│ │ 处理数据 │ (等待1秒)      │
│ └─────────┘                │
│ 总耗时：4秒                │
└─────────────────────────────┘

【异步执行】
┌─────────────────────────────┐
│ 主线程     │ 后台线程         │
│ ┌─────────┐  │ ┌───────────┐ │
│ │ 开始下载 │→ │ 下载文件   │ │
│ │ 处理其他 │  │ (等待3秒)  │ │
│ │ ...     │  │           │ │
│ │ ────────│  │           │ │
│ │ 收到完成 │← │ 返回结果  │ │
│ │ 处理数据 │  │           │ │
│ └─────────┘  └───────────┘ │
│ 总耗时：1秒（主线程）+ 3秒（后台）= 实际 4 秒，但主线程不阻塞
└─────────────────────────────┘
```

**async/await** 是 C# 5.0 引入的异步编程模型，让异步代码的编写和阅读像同步代码一样直观。

```
async/await 核心概念：

async 关键字：标记方法为异步方法
- 方法内部可以使用 await
- 返回类型必须是 Task、Task<T> 或 void（不推荐）

await 关键字：等待异步操作完成
- 等待一个 Task 完成
- 不会阻塞线程
- 只能在 async 方法内使用
```

**Task** 是代表一个异步操作的对象。通过 Task，开发者可以更精细地控制异步执行，包括等待、取消、异常处理等。

### 举例说明

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

// ===== 1. 基本的 async/await 使用 =====

class AsyncBasicDemo
{
    // 异步方法：模拟下载文件
    // async 关键字标记这是一个异步方法
    // Task<string> 表示该方法返回一个字符串的异步操作
    static async Task<string> DownloadAsync(string url)
    {
        // await 关键字：等待异步操作完成
        // Task.Delay(1000) 模拟 1 秒的网络延迟
        await Task.Delay(1000);
        
        // 模拟下载完成
        return $"下载完成: {url}";
    }
    
    // 调用异步方法
    static async Task Main()
    {
        Console.WriteLine("开始下载...");
        
        // 使用 await 调用异步方法
        // 注意：Main 方法也是 async，这样才能使用 await
        string result = await DownloadAsync("http://example.com/file.zip");
        
        Console.WriteLine(result);  // 下载完成: http://example.com/file.zip
    }
}

// ===== 2. async 方法的不同返回类型 =====

class AsyncReturnTypes
{
    // 返回 Task：无返回值
    // 等同于返回 void 的同步方法
    static async Task DoSomethingAsync()
    {
        await Task.Delay(500);
        Console.WriteLine("任务完成");
    }
    
    // 返回 Task<T>：有返回值
    // 这是最推荐的方式
    static async Task<int> CalculateAsync()
    {
        await Task.Delay(500);
        return 42;
    }
    
    // 返回 void：仅用于事件处理程序
    // 尽量避免使用，无法 await，无法捕获异常
    static async void ButtonClick(object sender, EventArgs e)
    {
        await Task.Delay(1000);
        Console.WriteLine("按钮点击处理完成");
    }
    
    // 使用示例
    static async Task TestReturnTypes()
    {
        // 调用无返回值的异步方法
        await DoSomethingAsync();
        
        // 调用有返回值的异步方法
        int result = await CalculateAsync();
        Console.WriteLine($"计算结果: {result}");  // 42
    }
}

// ===== 3. 并行执行多个异步任务 =====

class ParallelAsyncDemo
{
    static async Task<string> DownloadAsync(string url, int delay)
    {
        await Task.Delay(delay);
        return $"从 {url} 下载完成";
    }
    
    static async Task Main()
    {
        // 顺序执行：每个任务依次等待，总耗时 = 所有任务耗时之和
        Console.WriteLine("=== 顺序执行 ===");
        var start1 = DateTime.Now;
        var result1 = await DownloadAsync("url1", 1000);
        var result2 = await DownloadAsync("url2", 1000);
        var result3 = await DownloadAsync("url3", 1000);
        Console.WriteLine($"耗时: {(DateTime.Now - start1).TotalMilliseconds}ms");
        
        // 并行执行：同时启动所有任务，总耗时 = 最长任务的耗时
        Console.WriteLine("\n=== 并行执行 ===");
        var start2 = DateTime.Now;
        
        // 同时启动三个任务
        Task<string> task1 = DownloadAsync("url1", 1000);
        Task<string> task2 = DownloadAsync("url2", 1000);
        Task<string> task3 = DownloadAsync("url3", 1000);
        
        // 等待所有任务完成
        await Task.WhenAll(task1, task2, task3);
        
        Console.WriteLine($"耗时: {(DateTime.Now - start2).TotalMilliseconds}ms");
        
        // 并行执行 + 获取结果
        var results = await Task.WhenAll(task1, task2, task3);
        foreach (var r in results)
            Console.WriteLine(r);
    }
}

// ===== 4. 异常处理 =====

class AsyncExceptionDemo
{
    // 模拟可能抛出异常的异步方法
    static async Task<string> RiskyOperationAsync()
    {
        await Task.Delay(500);
        throw new InvalidOperationException("操作失败！");
    }
    
    static async Task HandleExceptionAsync()
    {
        try
        {
            await RiskyOperationAsync();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"捕获异常: {ex.Message}");
        }
        
        // 使用 AggregateException 处理多个异常
        try
        {
            // 同时执行多个可能失败的任务
            var tasks = new Task[]
            {
                RiskyOperationAsync(),
                RiskyOperationAsync(),
                RiskyOperationAsync()
            };
            
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // Task.WhenAll 会将所有异常包装在 AggregateException 中
            Console.WriteLine($"异常数量: {((AggregateException)ex).InnerExceptions.Count}");
        }
    }
}

// ===== 5. 取消操作 =====

class CancellationDemo
{
    // 支持取消的异步方法
    static async Task DownloadWithCancelAsync(CancellationToken token)
    {
        for (int i = 0; i < 10; i++)
        {
            // 检查是否请求了取消
            if (token.IsCancellationRequested)
            {
                Console.WriteLine("任务已取消");
                return;
            }
            
            Console.WriteLine($"下载进度: {(i + 1) * 10}%");
            
            // 等待，但可以被取消
            await Task.Delay(500, token);
        }
        
        Console.WriteLine("下载完成");
    }
    
    static async Task Main()
    {
        // 创建取消令牌源
        var cts = new CancellationTokenSource();
        
        try
        {
            // 启动异步任务，传入取消令牌
            var task = DownloadWithCancelAsync(cts.Token);
            
            // 模拟 2 秒后手动取消
            await Task.Delay(2000);
            Console.WriteLine("请求取消...");
            cts.Cancel();
            
            // 等待任务完成（或取消）
            await task;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("任务被取消");
        }
        
        // 使用 timeout 自动取消
        var cts2 = new CancellationTokenSource();
        cts2.CancelAfter(TimeSpan.FromSeconds(2));  // 2秒后自动取消
        
        try
        {
            await DownloadWithCancelAsync(cts2.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("任务超时被取消");
        }
    }
}

// ===== 6. Task.Run 后台执行 CPU 密集型任务 =====

class TaskRunDemo
{
    // 在线程池后台线程执行 CPU 密集型操作
    static async Task<long> CalculateHeavyAsync()
    {
        // 使用 Task.Run 在后台线程执行
        return await Task.Run(() =>
        {
            long sum = 0;
            for (long i = 0; i < 10_000_000; i++)
            {
                sum += i;
            }
            return sum;
        });
    }
    
    // 使用 IProgress<T] 报告进度
    static async Task DownloadWithProgress(IProgress<int> progress)
    {
        for (int i = 0; i <= 100; i += 10)
        {
            await Task.Delay(200);
            progress?.Report(i);  // 报告进度
        }
    }
    
    static async Task Main()
    {
        // 报告进度的示例
        var progress = new Progress<int>(p => Console.WriteLine($"进度: {p}%"));
        await DownloadWithProgress(progress);
    }
}
```

### 补充说明：async/await 常见误解

```csharp
// 误解1：async 意味着并行
// 正确：async 只是不阻塞线程，不一定并行
static async Task<string> WrongBelief()
{
    // 这仍然是顺序执行，不是并行！
    var result1 = await DownloadAsync("url1");  // 等待完成
    var result2 = await DownloadAsync("url2");  // 然后才执行这个
    
    return result1 + result2;
}

// 正确做法：如果需要并行，使用 Task.WhenAll
static async Task<string> CorrectBelief()
{
    var task1 = DownloadAsync("url1");  // 同时开始
    var task2 = DownloadAsync("url2");  // 同时开始
    
    await Task.WhenAll(task1, task2);   // 等待全部完成
    return task1.Result + task2.Result;
}

// 误解2：await 会创建新线程
// 正确：await 不会创建新线程，只是暂停当前方法的执行
//       底层线程会被释放回线程池，等待完成后继续执行
```

### 习题练习

#### 练习 2.5.1
创建一个异步方法 `FetchDataAsync`，模拟从不同数据源获取数据，总耗时应该 < 2秒。

#### 练习 2.5.2
使用 `Task.WhenAll` 并行获取多个资源，并计算总耗时。

#### 练习 2.5.3
实现一个支持取消的异步下载方法。

#### 练习 2.5.4
使用 `IProgress<T>` 实现一个带进度报告的批量处理方法。

### 练习讲解

#### 练习 2.5.1 答案

```csharp
class DataFetcher
{
    // 模拟从不同数据源获取数据
    static async Task<string> FetchFromSourceA()
    {
        await Task.Delay(800);  // 模拟 800ms 延迟
        return "数据源A的内容";
    }
    
    static async Task<string> FetchFromSourceB()
    {
        await Task.Delay(600);  // 模拟 600ms 延迟
        return "数据源B的内容";
    }
    
    static async Task<string> FetchFromSourceC()
    {
        await Task.Delay(500);  // 模拟 500ms 延迟
        return "数据源C的内容";
    }
    
    // 获取所有数据（顺序执行，总耗时 = 800 + 600 + 500 = 1900ms）
    static async Task<string> FetchAllDataSequential()
    {
        var start = DateTime.Now;
        
        var a = await FetchFromSourceA();
        var b = await FetchFromSourceB();
        var c = await FetchFromSourceC();
        
        Console.WriteLine($"顺序执行耗时: {(DateTime.Now - start).TotalMilliseconds}ms");
        return $"{a}\n{b}\n{c}";
    }
    
    // 获取所有数据（并行执行，总耗时 = max(800, 600, 500) = 800ms）
    static async Task<string> FetchAllDataParallel()
    {
        var start = DateTime.Now;
        
        var taskA = FetchFromSourceA();
        var taskB = FetchFromSourceB();
        var taskC = FetchFromSourceC();
        
        await Task.WhenAll(taskA, taskB, taskC);
        
        Console.WriteLine($"并行执行耗时: {(DateTime.Now - start).TotalMilliseconds}ms");
        return $"{taskA.Result}\n{taskB.Result}\n{taskC.Result}";
    }
    
    static async Task Main()
    {
        Console.WriteLine("=== 顺序执行 ===");
        var result1 = await FetchAllDataSequential();
        
        Console.WriteLine("\n=== 并行执行 ===");
        var result2 = await FetchAllDataParallel();
    }
}
```

#### 练习 2.5.2 答案

```csharp
class ParallelFetch
{
    // 模拟不同资源的获取
    static async Task<string> FetchResource(string name, int delayMs)
    {
        await Task.Delay(delayMs);
        return $"来自 {name} 的数据";
    }
    
    static async Task<string> FetchAll()
    {
        var start = DateTime.Now;
        
        // 并行启动多个任务
        var task1 = FetchResource("服务器A", 1000);
        var task2 = FetchResource("服务器B", 1500);
        var task3 = FetchResource("服务器C", 800);
        var task4 = FetchResource("服务器D", 1200);
        
        // 等待所有任务完成
        await Task.WhenAll(task1, task2, task3, task4);
        
        var elapsed = (DateTime.Now - start).TotalMilliseconds;
        
        // 汇总结果
        var results = new[] { task1.Result, task2.Result, task3.Result, task4.Result };
        
        Console.WriteLine($"并行获取 {results.Length} 个资源，总耗时: {elapsed:F0}ms");
        Console.WriteLine("（顺序执行需要 4500ms，并行执行只需 1500ms）");
        
        return string.Join("\n", results);
    }
    
    static async Task Main()
    {
        var result = await FetchAll();
        Console.WriteLine("\n结果:\n" + result);
    }
}
```

#### 练习 2.5.3 答案

```csharp
class CancellableDownload
{
    // 支持取消的下载方法
    static async Task DownloadFileAsync(string fileName, CancellationToken token)
    {
        Console.WriteLine($"开始下载: {fileName}");
        
        try
        {
            for (int i = 0; i <= 100; i += 10)
            {
                // 检查是否已取消
                token.ThrowIfCancellationRequested();
                
                // 模拟下载
                await Task.Delay(200, token);
                
                Console.WriteLine($"{fileName} 进度: {i}%");
            }
            
            Console.WriteLine($"{fileName} 下载完成！");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"{fileName} 下载已取消");
        }
    }
    
    static async Task Main()
    {
        var cts = new CancellationTokenSource();
        
        // 启动下载任务
        var downloadTask = DownloadFileAsync("document.pdf", cts.Token);
        
        // 模拟 1.5 秒后手动取消
        await Task.Delay(1500);
        Console.WriteLine("正在取消下载...");
        cts.Cancel();
        
        try
        {
            await downloadTask;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("下载已取消");
        }
        
        // 自动超时取消
        Console.WriteLine("\n=== 超时取消示例 ===");
        var cts2 = new CancellationTokenSource();
        cts2.CancelAfter(TimeSpan.FromSeconds(2));  // 2秒后自动取消
        
        await DownloadFileAsync("video.mp4", cts2.Token);
    }
}
```

#### 练习 2.5.4 答案

```csharp
class ProgressDemo
{
    // 处理项的事件参数
    class ProcessItemEventArgs : EventArgs
    {
        public int CurrentIndex { get; }
        public int TotalCount { get; }
        public string Item { get; }
        public int ProgressPercent => TotalCount > 0 ? CurrentIndex * 100 / TotalCount : 0;
        
        public ProcessItemEventArgs(int index, int total, string item)
        {
            CurrentIndex = index;
            TotalCount = total;
            Item = item;
        }
    }
    
    // 处理进度报告
    class ProcessProgress
    {
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public string CurrentItem { get; set; }
        
        public int ProgressPercent => TotalItems > 0 ? ProcessedItems * 100 / TotalItems : 0;
    }
    
    // 带进度报告的批量处理
    static async Task ProcessItemsAsync(
        List<string> items,
        IProgress<ProcessProgress> progress)
    {
        var total = items.Count;
        
        for (int i = 0; i < items.Count; i++)
        {
            // 模拟处理
            await Task.Delay(300);
            
            // 报告进度
            progress?.Report(new ProcessProgress
            {
                TotalItems = total,
                ProcessedItems = i + 1,
                CurrentItem = items[i]
            });
        }
    }
    
    static async Task Main()
    {
        var items = new List<string>
        {
            "文件A.txt", "文件B.txt", "文件C.txt",
            "文件D.txt", "文件E.txt", "文件F.txt"
        };
        
        // 创建进度报告器
        var progress = new Progress<ProcessProgress>(p =>
        {
            Console.WriteLine($"[{p.ProgressPercent,3}%] " +
                $"正在处理: {p.CurrentItem} ({p.ProcessedItems}/{p.TotalItems})");
        });
        
        Console.WriteLine("开始处理...\n");
        
        await ProcessItemsAsync(items, progress);
        
        Console.WriteLine("\n全部处理完成！");
    }
}
```

### 相关知识点

- 1.5 方法 - async 方法的本质
- 2.3 委托与事件 - async/await 底层实现
- 2.7 LINQ - 异步 LINQ 操作

---

## 2.6 反射与特性

### 概念讲解

**反射（Reflection）**是 C# 提供的一种强大机制，允许在**运行时**动态获取类型信息、访问成员（属性、方法、字段等）、创建对象、调用方法。反射是很多框架和库的基础，如依赖注入容器、ORM 框架、序列化库等。

```
反射的能力：

【运行时获取信息】
- 获取类型名称、命名空间、程序集
- 获取类型的成员（属性、方法、字段、构造函数）
- 获取成员的特性（Attribute）

【运行时动态操作】
- 创建对象（无需知道具体类型）
- 调用方法（无需编译时绑定）
- 访问/修改属性和字段
```

**特性（Attribute）**是一种可附加在代码元素（类、方法、属性、参数等）上的元数据。特性不会直接影响代码的执行，但可以通过反射在运行时读取，从而影响程序行为。特性常用于：
- 代码生成（如 `[GeneratedCode]`）
- 序列化控制（如 `[Serializable]`）
- 权限检查（如 `[PrincipalPermission]`）
- 标记和标记（如 `[Obsolete]`）

### 举例说明

```csharp
using System;
using System.Reflection;

// ===== 1. 获取类型信息 =====

class TypeInfoDemo
{
    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        
        public void SayHello() => Console.WriteLine($"你好，我是{Name}");
        
        private void PrivateMethod() => Console.WriteLine("私有方法");
    }
    
    static void Main()
    {
        // 获取类型信息的三种方式
        Type type1 = typeof(Person);                    // 使用 typeof
        Type type2 = new Person().GetType();           // 使用对象实例
        Type type3 = Type.GetType("TypeInfoDemo+Person");  // 使用类型名字符串
        
        // 输出类型信息
        Console.WriteLine($"类型名: {type1.Name}");
        Console.WriteLine($"完整名: {type1.FullName}");
        Console.WriteLine($"命名空间: {type1.Namespace}");
        Console.WriteLine($"程序集: {type1.Assembly.GetName().Name}");
        
        // 获取成员信息
        Console.WriteLine("\n=== 成员信息 ===");
        
        // 属性
        Console.WriteLine("\n属性:");
        foreach (var prop in type1.GetProperties())
            Console.WriteLine($"  {prop.Name} ({prop.PropertyType.Name})");
        
        // 方法（排除继承的 object 方法）
        Console.WriteLine("\n方法:");
        foreach (var method in type1.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            if (method.DeclaringType == type1)
                Console.WriteLine($"  {method.Name}() -> {method.ReturnType.Name}");
    }
}

// ===== 2. 动态创建对象和调用方法 =====

class DynamicInvokeDemo
{
    class Calculator
    {
        public int Add(int a, int b) => a + b;
        public static int Multiply(int a, int b) => a * b;
    }
    
    static void Main()
    {
        // 动态创建对象
        Type calculatorType = typeof(Calculator);
        
        // 方法1：使用 Activator 创建实例
        object calc = Activator.CreateInstance(calculatorType);
        
        // 方法2：使用构造函数信息
        ConstructorInfo ctor = calculatorType.GetConstructor(Type.EmptyTypes);
        object calc2 = ctor.Invoke(null);
        
        // 动态调用方法
        MethodInfo addMethod = calculatorType.GetMethod("Add");
        
        // Invoke 参数：实例对象，方法参数数组
        object result = addMethod.Invoke(calc, new object[] { 10, 20 });
        Console.WriteLine($"Add(10, 20) = {result}");  // 30
        
        // 调用静态方法
        MethodInfo multiplyMethod = calculatorType.GetMethod("Multiply");
        object result2 = multiplyMethod.Invoke(null, new object[] { 5, 6 });
        Console.WriteLine($"Multiply(5, 6) = {result2}");  // 30
    }
}

// ===== 3. 访问和修改属性 =====

class PropertyAccessDemo
{
    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        private bool isActive = true;
        
        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }
    }
    
    static void Main()
    {
        Person person = new Person { Name = "张三", Age = 25 };
        Type type = typeof(Person);
        
        // 获取属性
        PropertyInfo nameProp = type.GetProperty("Name");
        PropertyInfo ageProp = type.GetProperty("Age");
        PropertyInfo activeProp = type.GetProperty("IsActive");
        
        // 读取属性值
        object nameValue = nameProp.GetValue(person);
        object ageValue = ageProp.GetValue(person);
        Console.WriteLine($"Name: {nameValue}, Age: {ageValue}");
        
        // 设置属性值
        nameProp.SetValue(person, "李四");
        ageProp.SetValue(person, 30);
        
        Console.WriteLine($"修改后: {person.Name}, {person.Age}");
        
        // 处理私有属性
        PropertyInfo isActiveProp = type.GetProperty("IsActive",
            BindingFlags.Public | BindingFlags.Instance);
        bool activeValue = (bool)isActiveProp.GetValue(person);
        Console.WriteLine($"IsActive: {activeValue}");
    }
}

// ===== 4. 自定义特性 =====

// 定义特性类：继承自 Attribute
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
class DescriptionAttribute : Attribute
{
    // 特性可以有属性
    public string Description { get; set; }
    public string Author { get; set; }
    public DateTime CreatedDate { get; set; }
    
    // 构造函数
    public DescriptionAttribute(string description)
    {
        Description = description;
        Author = "未知";
        CreatedDate = DateTime.Now;
    }
}

// 应用特性到类
[Description("这是一个玩家类", Author = "张三")]
class Player
{
    [Description("玩家名称")]
    public string Name { get; set; }
    
    [Description("玩家等级")]
    public int Level { get; set; }
    
    [Description("玩家经验")]
    public long Experience { get; set; }
    
    [Description("玩家行动")]
    public void Move()
    {
        Console.WriteLine("玩家移动");
    }
}

// ===== 5. 读取特性 =====

class ReadAttributeDemo
{
    static void Main()
    {
        Type playerType = typeof(Player);
        
        // 读取类的特性
        var classAttr = playerType.GetCustomAttribute<DescriptionAttribute>();
        if (classAttr != null)
        {
            Console.WriteLine($"类特性: {classAttr.Description}");
            Console.WriteLine($"  作者: {classAttr.Author}");
            Console.WriteLine($"  创建日期: {classAttr.CreatedDate}");
        }
        
        // 读取属性特性
        Console.WriteLine("\n属性特性:");
        foreach (var prop in playerType.GetProperties())
        {
            var attr = prop.GetCustomAttribute<DescriptionAttribute>();
            if (attr != null)
            {
                Console.WriteLine($"  {prop.Name}: {attr.Description}");
            }
        }
        
        // 读取方法特性
        Console.WriteLine("\n方法特性:");
        foreach (var method in playerType.GetMethods())
        {
            var attr = method.GetCustomAttribute<DescriptionAttribute>();
            if (attr != null)
            {
                Console.WriteLine($"  {method.Name}: {attr.Description}");
            }
        }
    }
}

// ===== 6. 反射在 DI 中的应用 =====

class DependencyInjectionDemo
{
    // 简单的 DI 容器
    class Container
    {
        private Dictionary<Type, Type> registrations = new Dictionary<Type, Type>();
        
        // 注册服务
        public void Register<TInterface, TImplementation>()
            where TImplementation : TInterface, new()
        {
            registrations[typeof(TInterface)] = typeof(TImplementation);
        }
        
        // 解析服务
        public TInterface Resolve<TInterface>()
        {
            var implementationType = registrations[typeof(TInterface)];
            
            // 使用反射创建实例
            var ctor = implementationType.GetConstructors()[0];
            var parameters = ctor.GetParameters();
            
            if (parameters.Length == 0)
            {
                return (TInterface)Activator.CreateInstance(implementationType);
            }
            
            // 递归解析依赖
            object[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var resolveMethod = GetType().GetMethod("Resolve").MakeGenericMethod(paramType);
                args[i] = resolveMethod.Invoke(this, null);
            }
            
            return (TInterface)ctor.Invoke(args);
        }
    }
    
    // 示例接口和实现
    interface ILogger
    {
        void Log(string message);
    }
    
    class ConsoleLogger : ILogger
    {
        public void Log(string message) => Console.WriteLine($"[日志] {message}");
    }
    
    interface IRepository
    {
        void Save();
    }
    
    class UserRepository : IRepository
    {
        private ILogger logger;
        
        // 构造函数注入
        public UserRepository(ILogger logger)
        {
            this.logger = logger;
        }
        
        public void Save()
        {
            logger.Log("保存用户数据");
        }
    }
    
    static void Main()
    {
        var container = new Container();
        container.Register<ILogger, ConsoleLogger>();
        container.Register<IRepository, UserRepository>();
        
        var repository = container.Resolve<IRepository>();
        repository.Save();
    }
}
```

### 补充说明：编译时特性 vs 运行时特性

```csharp
// 一些常用系统特性：

// 标记方法已过时
[Obsolete("请使用新方法 NewMethod", true)]
void OldMethod() { }

// 阻止代码被序列化
[NonSerialized]
public int secret;

// 条件编译
[Conditional("DEBUG")]
void DebugLog(string msg) { }

// 继承父亲成员不公开
new void Method() { }
```

### 反射核心类、方法、作用一览表

| 核心类                | 方法/成员                         | 作用（描述）                                    | 调用示例                                                     |
| :-------------------- | :-------------------------------- | :---------------------------------------------- | :----------------------------------------------------------- |
| **`Type`**            | `typeof(类名)`                    | 编译时获取类型对象（不依赖实例）                | `Type t = typeof(Player);`                                   |
|                       | `实例.GetType()`                  | 从已存在的对象获取其运行时类型                  | `Type t = player.GetType();`                                 |
|                       | `Type.GetType("全名")`            | 通过字符串（含命名空间）动态获取类型            | `Type t = Type.GetType("MyApp.Player");`                     |
|                       | `.GetMethods()`                   | 返回所有**公共**方法（含继承）的 `MethodInfo[]` | `MethodInfo[] ms = t.GetMethods();`                          |
|                       | `.GetFields()`                    | 返回所有公共字段的 `FieldInfo[]`                | `FieldInfo[] fs = t.GetFields();`                            |
|                       | `.GetProperties()`                | 返回所有公共属性的 `PropertyInfo[]`             | `PropertyInfo[] ps = t.GetProperties();`                     |
|                       | `.GetConstructors()`              | 返回所有公共构造函数的 `ConstructorInfo[]`      | `ConstructorInfo[] cs = t.GetConstructors();`                |
|                       | `.GetMethod("名称")`              | 按名称获取公共方法（可加参数类型重载）          | `MethodInfo m = t.GetMethod("Attack");`                      |
|                       | `.GetField("名称")`               | 按名称获取公共字段                              | `FieldInfo f = t.GetField("health");`                        |
|                       | `.GetProperty("名称")`            | 按名称获取公共属性                              | `PropertyInfo p = t.GetProperty("Name");`                    |
|                       | `.GetCustomAttribute<T>()`        | 获取该类型上标记的特定特性实例                  | `var attr = t.GetCustomAttribute<SerializableAttribute>();`  |
|                       | `.GetCustomAttributes()`          | 获取该类型上的所有特性                          | `object[] attrs = t.GetCustomAttributes(false);`             |
|                       | `.IsSubclassOf(基类)`             | 判断是否继承自某个基类                          | `bool b = t.IsSubclassOf(typeof(MonoBehaviour));`            |
|                       | `.IsAssignableFrom(类型)`         | 判断是否可以给变量赋值为该类型                  | `bool ok = typeof(IComparable).IsAssignableFrom(t);`         |
| **`MethodInfo`**      | `.Invoke(实例, 参数[])`           | 动态调用方法；实例方法传对象，静态方法传null    | `m.Invoke(player, new object[] { 10 });`                     |
|                       | `.CreateDelegate(委托类型)`       | 将方法转为强类型委托，后续调用几乎零开销        | `var del = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), null, m);` |
|                       | `.MakeGenericMethod(类型参数[])`  | 构造泛型方法的特定版本                          | `MethodInfo gm = genericMethod.MakeGenericMethod(typeof(int));` |
| **`FieldInfo`**       | `.GetValue(实例)`                 | 读取字段的值                                    | `int h = (int)field.GetValue(player);`                       |
|                       | `.SetValue(实例, 值)`             | 设置字段的值                                    | `field.SetValue(player, 100);`                               |
|                       | `.FieldType`                      | 获取该字段的类型（`Type`）                      | `Type ft = field.FieldType;`                                 |
| **`PropertyInfo`**    | `.GetValue(实例)`                 | 调用 `get` 访问器获取属性值                     | `string name = (string)prop.GetValue(player);`               |
|                       | `.SetValue(实例, 值)`             | 调用 `set` 访问器设置属性值                     | `prop.SetValue(player, "Alice");`                            |
|                       | `.PropertyType`                   | 获取该属性的类型                                | `Type pt = prop.PropertyType;`                               |
| **`ConstructorInfo`** | `.Invoke(参数[])`                 | 调用构造函数并返回新实例                        | `object obj = ctor.Invoke(new object[] { 100 });`            |
|                       | `.GetParameters()`                | 获取构造函数的参数信息                          | `ParameterInfo[] pars = ctor.GetParameters();`               |
| **`Activator`**       | `.CreateInstance(Type)`           | 使用无参构造函数动态创建对象                    | `object obj = Activator.CreateInstance(typeof(Enemy));`      |
|                       | `.CreateInstance(Type, 参数…)`    | 使用带参构造函数动态创建对象                    | `object obj = Activator.CreateInstance(typeof(Enemy), 100, "Orc");` |
| **`Assembly`**        | `Assembly.GetExecutingAssembly()` | 获取当前代码所在的程序集                        | `Assembly asm = Assembly.GetExecutingAssembly();`            |
|                       | `typeof(某类).Assembly`           | 通过某个已知类型获取其所在的程序集              | `Assembly asm = typeof(Player).Assembly;`                    |
|                       | `.GetTypes()`                     | 返回程序集中定义的所有公共类型                  | `Type[] allTypes = asm.GetTypes();`                          |
|                       | `Assembly.Load("名称")`           | 动态加载一个程序集（谨慎使用）                  | `Assembly asm = Assembly.Load("MyAssembly");`                |

------

### `BindingFlags` 枚举常用成员及其作用

| 枚举成员                        | 含义                     | 作用说明                                           |
| :------------------------------ | :----------------------- | :------------------------------------------------- |
| `BindingFlags.Public`           | 搜索公共成员             | 限定只查找 public 修饰的成员                       |
| `BindingFlags.NonPublic`        | 搜索非公共成员           | 可以查找 private、internal、protected 等修饰的成员 |
| `BindingFlags.Instance`         | 搜索实例成员             | 限定查找属于具体对象的成员（非静态）               |
| `BindingFlags.Static`           | 搜索静态成员             | 限定查找属于类型本身的成员（static）               |
| `BindingFlags.DeclaredOnly`     | 仅搜索当前类型声明的成员 | 不包含从父类继承的成员，常用于避免过多结果         |
| `BindingFlags.FlattenHierarchy` | 平铺层次结构             | 用于获取继承链的上层静态成员（一般与 Static 组合） |
| `BindingFlags.IgnoreCase`       | 忽略大小写               | 按名称查找时忽略名字大小写差异                     |

### 📦 C# 内置常用特性

| 特性                                                 | 作用                                     | 适用目标               |
| :--------------------------------------------------- | :--------------------------------------- | :--------------------- |
| `[Obsolete]`                                         | 标记过时成员，编译器发出警告或错误       | 类、方法、属性、字段等 |
| `[Serializable]`                                     | 标记类型可进行序列化                     | 类、结构体、枚举、委托 |
| `[NonSerialized]`                                    | 标记字段不参与序列化                     | 字段                   |
| `[Conditional("宏")]`                                | 仅当定义了指定宏时，该方法调用才被编译   | 方法（返回 void）      |
| `[DllImport("库名")]`                                | 声明外部非托管 DLL 中的函数              | 静态方法               |
| `[AttributeUsage]`                                   | 指定自定义特性能应用于哪些目标及重用规则 | 自定义特性类           |
| `[CallerMemberName]`                                 | 自动获取调用方的成员名                   | 方法参数               |
| `[CallerFilePath]`                                   | 自动获取调用方的源文件路径               | 方法参数               |
| `[CallerLineNumber]`                                 | 自动获取调用方的源代码行号               | 方法参数               |
| `[DebuggerStepThrough]`                              | 调试时跳过此方法                         | 方法、属性、构造函数   |
| `[MethodImpl(MethodImplOptions.AggressiveInlining)]` | 建议 JIT 编译器进行内联优化              | 方法                   |
| `[Flags]`                                            | 指示枚举可以按位组合                     | 枚举                   |
| `[CLSCompliant(bool)]`                               | 声明程序集或成员是否符合公共语言规范     | 程序集、类型、成员     |

------

### 🎮 Unity 常用特性

#### Inspector 布局与序列化

| 特性                             | 作用                                    | 适用目标          |
| :------------------------------- | :-------------------------------------- | :---------------- |
| `[SerializeField]`               | 强制私有字段显示在 Inspector 中并序列化 | 字段              |
| `[HideInInspector]`              | 隐藏公共字段，不在 Inspector 中显示     | 字段              |
| `[Range(min, max)]`              | 将数值字段显示为滑动条                  | 字段（float/int） |
| `[Header("标题文本")]`           | 在字段上方添加粗体标题                  | 字段              |
| `[Tooltip("提示信息")]`          | 鼠标悬停时显示工具提示                  | 字段              |
| `[Space(像素)]`                  | 字段前添加空白间距                      | 字段              |
| `[TextArea(minLines, maxLines)]` | 字符串显示为多行文本区域                | 字段              |
| `[Multiline]`                    | 字符串可多行输入（不带滚动条）          | 字段              |
| `[ColorUsage(showAlpha, hdr)]`   | 颜色字段使用高级吸管和 HDR 选项         | 字段（Color）     |

#### 脚本与组件管理

| 特性                                               | 作用                                            | 适用目标            |
| :------------------------------------------------- | :---------------------------------------------- | :------------------ |
| `[RequireComponent(typeof(组件1), typeof(组件2))]` | 添加该脚本时自动添加所需组件，且不能移除        | 类（MonoBehaviour） |
| `[DisallowMultipleComponent]`                      | 同一 GameObject 上只能添加一个该脚本            | 类（MonoBehaviour） |
| `[SelectionBase]`                                  | 在 Scene 视图中选择物体时优先选中挂此脚本的对象 | 类（MonoBehaviour） |
| `[AddComponentMenu("路径/脚本名")]`                | 在 Component 菜单中指定添加脚本的路径           | 类（MonoBehaviour） |
| `[DefaultExecutionOrder(int)]`                     | 设置脚本的 Awake/Start/Update 等执行顺序        | 类（MonoBehaviour） |

#### 编辑器菜单与快捷操作

| 特性                                           | 作用                                                      | 适用目标            |
| :--------------------------------------------- | :-------------------------------------------------------- | :------------------ |
| `[MenuItem("菜单路径")]`                       | 在 Editor 顶部菜单栏创建菜单项                            | 静态方法            |
| `[ContextMenu("菜单名")]`                      | 在 Inspector 组件右键菜单中添加命令                       | 实例方法            |
| `[ContextMenuItem("菜单名", "函数名")]`        | 在字段的右键菜单中添加命令                                | 字段                |
| `[CreateAssetMenu(fileName, menuName, order)]` | 允许通过 Assets/Create 菜单快速创建 ScriptableObject 资源 | ScriptableObject 类 |

#### 运行模式与生命周期

| 特性                  | 作用                                              | 适用目标            |
| :-------------------- | :------------------------------------------------ | :------------------ |
| `[ExecuteAlways]`     | 使脚本在编辑器非运行模式下也执行生命周期          | 类（MonoBehaviour） |
| `[ExecuteInEditMode]` | 旧版 ExecuteAlways 的同功能，建议用 ExecuteAlways | 类（MonoBehaviour） |

#### 构建与代码剥离

| 特性         | 作用                                     | 适用目标               |
| :----------- | :--------------------------------------- | :--------------------- |
| `[Preserve]` | 防止 IL2CPP 字节码剥离时移除该类型或成员 | 类、方法、字段、属性等 |

#### 网络（旧版 UNET，但可能仍会遇到）

| 特性          | 作用                               |
| :------------ | :--------------------------------- |
| `[Command]`   | 由客户端调用，在服务器执行（方法） |
| `[ClientRpc]` | 由服务器调用，在客户端执行（方法） |
| `[SyncVar]`   | 字段值从服务器自动同步到客户端     |

### 习题练习

#### 练习 2.6.1
使用反射获取一个类（自定义）的所有属性名称和类型，并输出到控制台。

#### 练习 2.6.2
使用反射动态创建一个对象，并调用其方法。

#### 练习 2.6.3
创建一个自定义特性 `TableName`，应用到一个类上，通过反射读取并输出表名。

#### 练习 2.6.4
使用反射实现一个简单的对象拷贝功能。

### 练习讲解

#### 练习 2.6.1 答案

```csharp
class ReflectProperties
{
    // 示例类
    class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
    }
    
    static void Main()
    {
        Type type = typeof(Product);
        
        Console.WriteLine($"类名: {type.Name}");
        Console.WriteLine($"属性数量: {type.GetProperties().Length}");
        Console.WriteLine("\n=== 所有属性 ===");
        
        foreach (var prop in type.GetProperties())
        {
            Console.WriteLine($"  {prop.Name}: {prop.PropertyType.Name}");
        }
        
        // 输出：
        // 类名: Product
        // 属性数量: 5
        //
        // === 所有属性 ===
        //   Id: Int32
        //   Name: String
        //   Price: Decimal
        //   Stock: Int32
        //   IsActive: Boolean
    }
}
```

#### 练习 2.6.2 答案

```csharp
class ReflectMethodInvoke
{
    class UserService
    {
        public string GetUserName(int id)
        {
            return id == 1 ? "张三" : "未知用户";
        }
        
        public bool UpdateUser(string name, int age)
        {
            Console.WriteLine($"更新用户: {name}, 年龄: {age}");
            return true;
        }
        
        public static string GetVersion()
        {
            return "1.0.0";
        }
    }
    
    static void Main()
    {
        Type type = typeof(UserService);
        
        // 创建实例
        object service = Activator.CreateInstance(type);
        
        // 调用实例方法
        MethodInfo getUserName = type.GetMethod("GetUserName");
        object result = getUserName.Invoke(service, new object[] { 1 });
        Console.WriteLine($"GetUserName(1) = {result}");  // 张三
        
        // 调用带多个参数的方法
        MethodInfo updateUser = type.GetMethod("UpdateUser");
        bool updateResult = (bool)updateUser.Invoke(service, new object[] { "李四", 25 });
        Console.WriteLine($"UpdateUser 结果: {updateResult}");  // True
        
        // 调用静态方法
        MethodInfo getVersion = type.GetMethod("GetVersion");
        string version = (string)getVersion.Invoke(null, null);  // 静态方法不需要实例
        Console.WriteLine($"版本: {version}");  // 1.0.0
    }
}
```

#### 练习 2.6.3 答案

```csharp
// 定义特性：表名
[AttributeUsage(AttributeTargets.Class)]
class TableNameAttribute : Attribute
{
    public string Name { get; }
    
    public TableNameAttribute(string name)
    {
        Name = name;
    }
}

// 应用特性到类
[TableName("users")]
class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[TableName("products")]
class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// 读取特性
class Program
{
    static void Main()
    {
        // 检查 User 类
        Type userType = typeof(User);
        var userTableAttr = userType.GetCustomAttribute<TableNameAttribute>();
        Console.WriteLine($"User 表名: {userTableAttr?.Name}");  // users
        
        // 检查 Product 类
        Type productType = typeof(Product);
        var productTableAttr = productType.GetCustomAttribute<TableNameAttribute>();
        Console.WriteLine($"Product 表名: {productTableAttr?.Name}");  // products
        
        // 通用方法：获取任意类的表名
        string GetTableName<T>()
        {
            var attr = typeof(T).GetCustomAttribute<TableNameAttribute>();
            return attr?.Name;
        }
        
        Console.WriteLine($"\n通用方法结果:");
        Console.WriteLine($"User: {GetTableName<User>()}");
        Console.WriteLine($"Product: {GetTableName<Product>()}");
    }
}
```

#### 练习 2.6.4 答案

```csharp
// 简单的对象拷贝工具
class ObjectCopier
{
    // 使用反射复制对象
    public static T Copy<T>(T source) where T : class
    {
        if (source == null) return null;
        
        Type type = typeof(T);
        
        // 创建新实例
        object target = Activator.CreateInstance(type);
        
        // 复制所有属性
        foreach (var prop in type.GetProperties())
        {
            // 只复制可写且可读的属性
            if (prop.CanRead && prop.CanWrite)
            {
                object value = prop.GetValue(source);
                prop.SetValue(target, value);
            }
        }
        
        return (T)target;
    }
    
    // 测试
    static void Main()
    {
        class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public Address Address { get; set; }
        }
        
        class Address
        {
            public string City { get; set; }
            public string Street { get; set; }
        }
        
        // 原始对象
        var original = new Person
        {
            Name = "张三",
            Age = 25,
            Address = new Address { City = "北京", Street = "朝阳区" }
        };
        
        // 拷贝
        var copy = ObjectCopier.Copy(original);
        
        // 修改拷贝不影响原始对象（对于引用类型字段是浅拷贝）
        copy.Name = "李四";
        copy.Age = 30;
        copy.Address.City = "上海";
        
        Console.WriteLine($"原始: {original.Name}, {original.Age}, {original.Address.City}");
        Console.WriteLine($"拷贝: {copy.Name}, {copy.Age}, {copy.Address.City}");
        
        // 注意：Address 是引用类型，所以 City 的修改会影响原始对象
        // 这是浅拷贝的效果
    }
}
```

### 相关知识点

- 1.10 访问修饰符 - 反射可以访问私有成员
- 2.7 LINQ - 表达式树基于反射实现
- 2.8 序列化 - 序列化库大量使用反射

---

## 2.7 LINQ

### 概念讲解

**LINQ**（Language Integrated Query，语言集成查询）是 C# 3.0 引入的一项革命性功能，它提供了一种统一的语法来查询各种数据源。LINQ 使得开发者能够使用类似 SQL 的声明式语法来操作数组、集合、XML、数据库、Entity Framework 等数据源。

```
LINQ 支持的数据源：

┌─────────────────────────────────────────┐
│              LINQ 查询                   │
├─────────────────────────────────────────┤
│  ↓          ↓         ↓         ↓       │
│ 数组      集合      数据库    XML/JSON  │
│ (IEnumerable)  (EF)    (Linq to SQL)    │
└─────────────────────────────────────────┘

LINQ to Objects      LINQ to Entities
LINQ to XML          LINQ to DataSet
```

**两种语法形式**：

1. **查询表达式语法**（Query Syntax）：
```csharp
var result = from x in collection
             where x > 5
             orderby x
             select x * 2;
```

2. **方法语法**（Method Syntax / Fluent API）：
```csharp
var result = collection
    .Where(x => x > 5)
    .OrderBy(x => x)
    .Select(x => x * 2);
```

两种语法最终会编译成相同 IL 代码，性能相同。

### 举例说明

```csharp
using System;
using System.Linq;
using System.Collections.Generic;

// ===== 1. 基本筛选和投影 =====

class BasicLinqDemo
{
    static void Main()
    {
        int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        
        // Where: 筛选
        var evens = numbers.Where(n => n % 2 == 0);
        Console.WriteLine($"偶数: {string.Join(", ", evens)}");  // 2,4,6,8,10
        
        // Select: 投影（转换）
        var squares = numbers.Select(n => n * n);
        Console.WriteLine($"平方: {string.Join(", ", squares)}");  // 1,4,9,16,25,36,49,64,81,100
        
        // 组合使用
        var result = numbers
            .Where(n => n > 5)
            .Select(n => $"数字: {n}");
        
        foreach (var item in result)
            Console.WriteLine(item);
    }
}

// ===== 2. 排序操作 =====

class OrderingDemo
{
    static void Main()
    {
        var persons = new[]
        {
            new { Name = "张三", Age = 25, Score = 85 },
            new { Name = "李四", Age = 30, Score = 92 },
            new { Name = "王五", Age = 25, Score = 78 },
            new { Name = "赵六", Age = 28, Score = 90 }
        };
        
        // 单级排序
        var byAge = persons.OrderBy(p => p.Age);
        Console.WriteLine("按年龄升序:");
        foreach (var p in byAge)
            Console.WriteLine($"  {p.Name}, {p.Age}岁");
        
        // 多级排序：先按年龄升序，再按分数降序
        var multiOrder = persons
            .OrderBy(p => p.Age)
            .ThenByDescending(p => p.Score);
        
        Console.WriteLine("\n多级排序:");
        foreach (var p in multiOrder)
            Console.WriteLine($"  {p.Name}, {p.Age}岁, {p.Score}分");
        
        // 降序
        var desc = persons.OrderByDescending(p => p.Score);
    }
}

// ===== 3. 分组操作 =====

class GroupingDemo
{
    static void Main()
    {
        var products = new[]
        {
            new { Name = "苹果", Category = "水果", Price = 5 },
            new { Name = "香蕉", Category = "水果", Price = 3 },
            new { Name = "白菜", Category = "蔬菜", Price = 2 },
            new { Name = "萝卜", Category = "蔬菜", Price = 3 },
            new { Name = "葡萄", Category = "水果", Price = 8 }
        };
        
        // 按类别分组
        var grouped = products.GroupBy(p => p.Category);
        
        Console.WriteLine("=== 按类别分组 ===");
        foreach (var group in grouped)
        {
            Console.WriteLine($"\n{group.Key} ({group.Count()} 个产品):");
            foreach (var p in group)
                Console.WriteLine($"  - {p.Name}: {p.Price}元");
        }
        
        // 分组后聚合
        var categoryStats = products
            .GroupBy(p => p.Category)
            .Select(g => new
            {
                Category = g.Key,
                Count = g.Count(),
                TotalPrice = g.Sum(p => p.Price),
                AvgPrice = g.Average(p => p.Price)
            });
        
        Console.WriteLine("\n=== 类别统计 ===");
        foreach (var stat in categoryStats)
        {
            Console.WriteLine($"{stat.Category}: 共{stat.Count}个, " +
                $"总价{stat.TotalPrice}元, 平均{stat.AvgPrice:F2}元");
        }
    }
}

// ===== 4. 连接操作 =====

class JoinDemo
{
    static void Main()
    {
        var customers = new[]
        {
            new { Id = 1, Name = "张三" },
            new { Id = 2, Name = "李四" },
            new { Id = 3, Name = "王五" }
        };
        
        var orders = new[]
        {
            new { CustomerId = 1, Product = "电脑", Amount = 5000 },
            new { CustomerId = 1, Product = "鼠标", Amount = 100 },
            new { CustomerId = 2, Product = "键盘", Amount = 300 },
            new { CustomerId = 2, Product = "显示器", Amount = 800 }
        };
        
        // 内连接
        var innerJoin = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Product, o.Amount });
        
        Console.WriteLine("=== 内连接 ===");
        foreach (var item in innerJoin)
            Console.WriteLine($"{item.Name} 购买了 {item.Product}, 金额: {item.Amount}");
        
        // 分组连接（类似 LEFT JOIN）
        var groupJoin = customers
            .GroupJoin(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, orderGroup) => new
                {
                    Customer = c.Name,
                    OrderCount = orderGroup.Count(),
                    TotalAmount = orderGroup.Sum(o => o.Amount)
                });
        
        Console.WriteLine("\n=== 分组连接 ===");
        foreach (var item in groupJoin)
            Console.WriteLine($"{item.Customer}: {item.OrderCount}个订单, " +
                $"总金额: {item.TotalAmount}");
    }
}

// ===== 5. 聚合操作 =====

class AggregationDemo
{
    static void Main()
    {
        int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        
        Console.WriteLine($"Sum: {numbers.Sum()}");           // 55
        Console.WriteLine($"Average: {numbers.Average():F1}"); // 5.5
        Console.WriteLine($"Min: {numbers.Min()}");           // 1
        Console.WriteLine($"Max: {numbers.Max()}");           // 10
        Console.WriteLine($"Count: {numbers.Count()}");        // 10
        Console.WriteLine($"Count(偶数): {numbers.Count(n => n % 2 == 0)}");  // 5
        
        // 条件聚合
        var data = new[] { 3, 1, 4, 1, 5, 9, 2, 6 };
        Console.WriteLine($"\n数据: {string.Join(", ", data)}");
        Console.WriteLine($"大于4的元素: {data.Count(n => n > 4)}");  // 3
        Console.WriteLine($"大于4的和: {data.Where(n => n > 4).Sum()}");  // 25
    }
}

// ===== 6. 集合操作 =====

class SetOperationsDemo
{
    static void Main()
    {
        var a = new[] { 1, 2, 3, 4, 5 };
        var b = new[] { 4, 5, 6, 7, 8 };
        
        // Union: 并集
        var union = a.Union(b);
        Console.WriteLine($"并集: {string.Join(", ", union)}");  // 1,2,3,4,5,6,7,8
        
        // Intersect: 交集
        var intersect = a.Intersect(b);
        Console.WriteLine($"交集: {string.Join(", ", intersect)}");  // 4,5
        
        // Except: 差集 (a 有但 b 没有)
        var except = a.Except(b);
        Console.WriteLine($"差集: {string.Join(", ", except)}");  // 1,2,3
        
        // Distinct: 去重
        var withDuplicates = new[] { 1, 2, 2, 3, 3, 3, 4 };
        var distinct = withDuplicates.Distinct();
        Console.WriteLine($"去重: {string.Join(", ", distinct)}");  // 1,2,3,4
    }
}

// ===== 7. 分页和限制 =====

class PagingDemo
{
    static void Main()
    {
        var data = Enumerable.Range(1, 20);  // 1-20
        
        // Take: 取前N个
        var first5 = data.Take(5);
        Console.WriteLine($"前5个: {string.Join(", ", first5)}");  // 1,2,3,4,5
        
        // Skip: 跳过前N个
        var skip10 = data.Skip(10);
        Console.WriteLine($"跳过前10个: {string.Join(", ", skip10)}");  // 11-20
        
        // 分页：每页3条，取第2页
        int pageSize = 3;
        int pageNumber = 2;
        var page = data
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
        Console.WriteLine($"第2页: {string.Join(", ", page)}");  // 4,5,6
        
        // TakeWhile: 满足条件时继续取
        var takeWhile = data.TakeWhile(n => n < 5);
        Console.WriteLine($"取到小于5: {string.Join(", ", takeWhile)}");  // 1,2,3,4
    }
}

// ===== 8. 元素操作 =====

class ElementOperationsDemo
{
    static void Main()
    {
        var numbers = new[] { 3, 1, 4, 1, 5, 9, 2, 6 };
        var empty = new int[] { };
        
        // First/FirstOrDefault
        Console.WriteLine($"First: {numbers.First()}");  // 3
        Console.WriteLine($"First(>5): {numbers.First(n => n > 5)}");  // 9
        Console.WriteLine($"FirstOrDefault: {empty.FirstOrDefault()}");  // 0
        
        // Last/LastOrDefault
        Console.WriteLine($"Last: {numbers.Last()}");  // 6
        Console.WriteLine($"LastOrDefault: {empty.LastOrDefault(-1)}");  // -1
        
        // Single/SingleOrDefault（只有一个元素时获取）
        var single = new[] { 42 };
        Console.WriteLine($"Single: {single.Single()}");  // 42
        
        // ElementAt: 按索引获取
        Console.WriteLine($"ElementAt(3): {numbers.ElementAt(3)}");  // 1
        Console.WriteLine($"ElementAtOrDefault(100): {numbers.ElementAtOrDefault(100)}");  // 0
    }
}

// ===== 9. 查询表达式语法 =====

class QuerySyntaxDemo
{
    static void Main()
    {
        var students = new[]
        {
            new { Name = "张三", Score = 85 },
            new { Name = "李四", Score = 72 },
            new { Name = "王五", Score = 90 },
            new { Name = "赵六", Score = 68 },
            new { Name = "钱七", Score = 78 }
        };
        
        // 基本查询
        var query1 = from s in students
                     where s.Score >= 80
                     orderby s.Score descending
                     select s.Name;
        
        Console.WriteLine(">=80分学生: " + string.Join(", ", query1));
        // 张三, 王五
        
        // 分组查询
        var query2 = from s in students
                     group s by (s.Score >= 60 ? "及格" : "不及格") into g
                     select new { Group = g.Key, Count = g.Count() };
        
        foreach (var item in query2)
            Console.WriteLine($"{item.Group}: {item.Count}人");
        
        // 复杂查询：带多个 let 和 join
        var departments = new[]
        {
            new { Id = 1, Name = "技术部" },
            new { Id = 2, Name = "销售部" }
        };
        
        var empDepts = new[]
        {
            new { Name = "张三", DeptId = 1 },
            new { Name = "李四", DeptId = 1 },
            new { Name = "王五", DeptId = 2 }
        };
        
        var result = from e in empDepts
                     join d in departments on e.DeptId equals d.Id
                     select new { e.Name, d.Name };
        
        foreach (var r in result)
            Console.WriteLine($"{r.Name} - {r.Name}");
    }
}
```

### 补充说明：延迟执行

```csharp
class DeferredExecutionDemo
{
    static void Main()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        
        // 延迟执行：创建查询，不执行
        var query = numbers.Where(n => n > 2);
        
        Console.WriteLine("查询已创建");
        
        // 第一次执行
        Console.WriteLine("第一次遍历:");
        foreach (var n in query)
            Console.WriteLine(n);
        
        // 第二次执行：查询会重新执行！
        Console.WriteLine("第二次遍历:");
        foreach (var n in query)
            Console.WriteLine(n);
        
        // 解决方案：缓存结果
        var cached = query.ToList();
        
        Console.WriteLine("\n使用缓存后:");
        foreach (var n in cached)  // 不再重新执行
            Console.WriteLine(n);
    }
}
```

### 习题练习

#### 练习 2.7.1
使用 LINQ 从整数列表中筛选出所有奇数，并计算它们的平均值。

#### 练习 2.7.2
使用 LINQ 按部门分组员工，计算每个部门的平均工资。

#### 练习 2.7.3
使用 LINQ 实现分页功能，从列表中获取第 3 页的数据（每页 10 条）。

#### 练习 2.7.4
使用 LINQ 找出两个列表的交集和差集。

### 练习讲解

#### 练习 2.7.1 答案

```csharp
class LinqExercise1
{
    static void Main()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        
        // 筛选奇数
        var odds = numbers.Where(n => n % 2 != 1);
        
        // 计算平均值
        var average = odds.Average();
        
        Console.WriteLine($"数字: {string.Join(", ", numbers)}");
        Console.WriteLine($"奇数: {string.Join(", ", odds)}");
        Console.WriteLine($"奇数平均值: {average}");
    }
}
```

#### 练习 2.7.2 答案

```csharp
class LinqExercise2
{
    class Employee
    {
        public string Name { get; set; }
        public string Department { get; set; }
        public decimal Salary { get; set; }
    }
    
    static void Main()
    {
        var employees = new List<Employee>
        {
            new Employee { Name = "张三", Department = "技术部", Salary = 8000 },
            new Employee { Name = "李四", Department = "技术部", Salary = 7000 },
            new Employee { Name = "王五", Department = "销售部", Salary = 6000 },
            new Employee { Name = "赵六", Department = "销售部", Salary = 5500 },
            new Employee { Name = "钱七", Department = "人事部", Salary = 5000 }
        };
        
        // 按部门分组，计算平均工资
        var deptAvg = employees
            .GroupBy(e => e.Department)
            .Select(g => new
            {
                Department = g.Key,
                AverageSalary = g.Average(e => e.Salary),
                Count = g.Count()
            });
        
        Console.WriteLine("=== 部门工资统计 ===");
        foreach (var dept in deptAvg)
        {
            Console.WriteLine($"{dept.Department}: " +
                $"平均 {dept.AverageSalary:F0} 元 ({dept.Count}人)");
        }
    }
}
```

#### 练习 2.7.3 答案

```csharp
class LinqExercise3
{
    static void Main()
    {
        // 生成 35 条数据
        var allData = Enumerable.Range(1, 35).Select(i => $"数据{i}").ToList();
        
        // 分页参数
        int pageSize = 10;
        int pageNumber = 3;
        
        // 使用 LINQ 实现分页
        var pageData = allData
            .Skip((pageNumber - 1) * pageSize)  // 跳过前 20 条
            .Take(pageSize);                     // 取 10 条
        
        Console.WriteLine($"总数据: {allData.Count} 条");
        Console.WriteLine($"第 {pageNumber} 页，每页 {pageSize} 条:");
        foreach (var item in pageData)
            Console.WriteLine($"  {item}");
    }
}
```

#### 练习 2.7.4 答案

```csharp
class LinqExercise4
{
    static void Main()
    {
        var list1 = new List<int> { 1, 2, 3, 4, 5 };
        var list2 = new List<int> { 4, 5, 6, 7, 8 };
        
        // 交集：两个列表都有的元素
        var intersection = list1.Intersect(list2);
        Console.WriteLine($"交集: {string.Join(", ", intersection)}");  // 4,5
        
        // 差集：list1 有但 list2 没有的
        var except1 = list1.Except(list2);
        Console.WriteLine($"list1 的差集: {string.Join(", ", except1)}");  // 1,2,3
        
        // 差集：list2 有但 list1 没有的
        var except2 = list2.Except(list1);
        Console.WriteLine($"list2 的差集: {string.Join(", ", except2)}");  // 6,7,8
        
        // 并集：所有不重复的元素
        var union = list1.Union(list2);
        Console.WriteLine($"并集: {string.Join(", ", union)}");  // 1,2,3,4,5,6,7,8
    }
}
```

### 相关知识点

- 1.7 集合 - LINQ 操作的输入输出都是集合
- 2.3 委托与事件 - Lambda 表达式是 LINQ 的基础
- 2.4 泛型 - IEnumerable<T> 是 LINQ 的基础接口

---

## 2.8 高级主题

### 概念讲解

本节介绍一些 C# 开发中的高级概念，包括**装箱与拆箱**、**协变与逆变**、**元组**、**模式匹配**等。

**装箱与拆箱（Boxing/Unboxing）**：
- **装箱**：将值类型转换为 object（引用类型）
- **拆箱**：将 object 转换回值类型

```
装箱过程：
┌─────────┐     ┌─────────────────┐
│ int i=5 │ ──→ │ object o = i    │
└─────────┘     │ ┌─────────────┐ │
               │ │ 值: 5        │ │
               │ │ 类型: int    │ │
               │ └─────────────┘ │
               └─────────────────┘
                堆上分配内存
```

**协变与逆变（Covariance/Contravariance）**：
- **协变**：返回类型从子类到父类（out 关键字）
- **逆变**：参数类型从父类到子类（in 关键字）

**元组（Tuple）**：
- 轻量级数据结构，用于组合多个值
- C# 7.0 引入了更强大的元组语法

### 举例说明

```csharp
using System;

// ===== 1. 装箱与拆箱 =====

class BoxingDemo
{
    static void Main()
    {
        // 装箱：值类型 → object
        int i = 42;
        object o = i;  // 装箱：堆上分配内存
        
        // 拆箱：object → 值类型
        int j = (int)o;  // 拆箱：必须显式转换
        
        // 装箱拆箱的性能影响
        // 频繁的装箱拆箱会导致性能问题
        // 解决方案：使用泛型集合
        
        // 不推荐：ArrayList（会有装箱拆箱）
        ArrayList list = new ArrayList();
        list.Add(100);  // int → object 装箱
        int val = (int)list[0];  // object → int 拆箱
        
        // 推荐：List<T>（无装箱拆箱）
        List<int> list2 = new List<int>();
        list2.Add(100);  // 直接存储，无装箱
        int val2 = list2[0];  // 直接获取，无拆箱
    }
}

// ===== 2. 协变与逆变 =====

class VarianceDemo
{
    // 基类
    class Animal { }
    
    // 子类
    class Dog : Animal { }
    
    // 协变接口：返回类型是 T
    interface IProducer<out T>
    {
        T Produce();
    }
    
    // 逆变接口：参数类型是 T
    interface IConsumer<in T>
    {
        void Consume(T item);
    }
    
    // 实现
    class DogProducer : IProducer<Dog>
    {
        public Dog Produce() => new Dog();
    }
    
    static void Main()
    {
        // 协变：Dog → Animal
        IProducer<Dog> dogProducer = new DogProducer();
        IProducer<Animal> animalProducer = dogProducer;  // 允许！
        Animal animal = animalProducer.Produce();
        
        // 逆变：Animal → Dog
        IConsumer<Animal> animalConsumer = (a) => Console.WriteLine(a);
        IConsumer<Dog> dogConsumer = animalConsumer;  // 允许！
        dogConsumer.Consume(new Dog());
    }
}

// ===== 3. 元组 =====

class TupleDemo
{
    static void Main()
    {
        // C# 旧语法（.NET 4.0+）
        var tuple1 = Tuple.Create(1, "张三", true);
        Console.WriteLine(tuple1.Item1);  // 1
        Console.WriteLine(tuple1.Item2);  // 张三
        
        // C# 7.0+ 新语法（推荐）
        var person = (Id: 1, Name: "张三", Age: 25);
        Console.WriteLine(person.Id);     // 1
        Console.WriteLine(person.Name);   // 张三
        
        // 解构元组
        var (id, name, age) = person;
        Console.WriteLine($"{id}, {name}, {age}");
        
        // 方法返回元组
        var result = Divide(10, 3);
        Console.WriteLine($"商: {result.Quotient}, 余数: {result.Remainder}");
        
        // 交换变量（无需临时变量）
        int a = 5, b = 10;
        (a, b) = (b, a);
        Console.WriteLine($"a={a}, b={b}");  // a=10, b=5
    }
    
    // 返回元组的方法
    static (int Quotient, int Remainder) Divide(int dividend, int divisor)
    {
        return (dividend / divisor, dividend % divisor);
    }
}

// ===== 4. 模式匹配 =====

class PatternMatchingDemo
{
    static void Main()
    {
        // 1. 类型模式匹配
        object obj = "Hello World";
        
        if (obj is string s)
        {
            Console.WriteLine($"是字符串，长度: {s.Length}");
        }
        
        // 2. 关系模式
        int score = 85;
        string grade = score switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            _ => "F"
        };
        Console.WriteLine($"分数: {score}, 等级: {grade}");
        
        // 3. 属性模式
        var person = new { Name = "张三", Age = 25, City = "北京" };
        
        if (person is { Age: >= 18, City: "北京" })
        {
            Console.WriteLine("北京的成年人");
        }
        
        // 4. 列表模式（C# 9.0+）
        var list = new[] { 1, 2, 3, 4, 5 };
        
        if (list is [_, 2, ..])  // 第二个元素是2，后面任意
        {
            Console.WriteLine("第二个元素是2");
        }
    }
}

// ===== 5. 记录类型（C# 9.0+） =====

record Person(string Name, int Age);

class RecordDemo
{
    static void Main()
    {
        // 创建记录
        var p1 = new Person("张三", 25);
        var p2 = new Person("张三", 25);
        
        // 值相等比较（自动生成 Equals）
        Console.WriteLine($"相等: {p1 == p2}");  // True
        
        // 不可变性
        // var p3 = p1 with { Age = 30 };  // 创建副本（需 C# 10+）
        
        // ToString 自动实现
        Console.WriteLine(p1);  // Person { Name = 张三, Age = 25 }
    }
}

// ===== 6. 可空引用类型（C# 8.0+） =====

class NullableDemo
{
    // 非空字符串（默认）
    // string name = null;  // 警告
    
    // 可空字符串
    string? nullableName = null;
    
    // 安全访问
    string? name = null;
    // Console.WriteLine(name.Length);  // 警告
    Console.WriteLine(name?.Length);  // 安全：输出 null
    
    //  null 合并
    string result = name ?? "默认值";
    Console.WriteLine(result);  // 默认值
    
    // null 合并赋值
    name ??= "新值";
    Console.WriteLine(name);  // 新值
}
```

### 习题练习

#### 练习 2.8.1
说明装箱和拆箱的性能问题，以及如何避免。

#### 练习 2.8.2
使用协变接口实现泛型动物园案例。

#### 练习 2.8.3
使用元组返回多个值，实现一个统计方法。

#### 练习 2.8.4
使用模式匹配实现一个简单计算器。

### 练习讲解

#### 练习 2.8.1 答案

```csharp
class BoxingExercise
{
    static void Main()
    {
        // 装箱拆箱的性能问题：
        // 1. 每次装箱都需要在堆上分配内存
        // 2. 每次拆箱都需要类型检查
        // 3. 频繁操作会导致 GC 压力
        
        // 问题示例
        ArrayList list = new ArrayList();
        for (int i = 0; i < 1000; i++)
        {
            list.Add(i);  // 每次都装箱！
        }
        
        // 解决方案：使用泛型
        List<int> genericList = new List<int>();
        for (int i = 0; i < 1000; i++)
        {
            genericList.Add(i);  // 无装箱！
        }
        
        // 总结：
        // 1. 使用泛型集合代替非泛型集合
        // 2. 避免将值类型存储在 object 变量中
        // 3. 需要装箱时考虑使用 Nullable<T> 或直接用引用类型
    }
}
```

#### 练习 2.8.2 答案

```csharp
class CovarianceExercise
{
    // 基类
    class Animal
    {
        public string Name { get; }
        public Animal(string name) => Name = name;
    }
    
    // 子类
    class Dog : Animal
    {
        public Dog(string name) : base(name) { }
    }
    
    class Cat : Animal
    {
        public Cat(string name) : base(name) { }
    }
    
    // 协变接口：可以产生动物
    interface IAnimalProducer<out T> where T : Animal
    {
        T Produce();
    }
    
    // 实现
    class DogProducer : IAnimalProducer<Dog>
    {
        public Dog Produce() => new Dog("旺财");
    }
    
    class CatProducer : IAnimalProducer<Cat>
    {
        public Cat Produce() => new Cat("咪咪");
    }
    
    static void Main()
    {
        // 协变：DogProducer → IAnimalProducer<Animal>
        IAnimalProducer<Dog> dogProducer = new DogProducer();
        IAnimalProducer<Animal> animalProducer = dogProducer;
        
        Animal animal = animalProducer.Produce();
        Console.WriteLine($"产生了: {animal.Name}");  // 旺财
    }
}
```

#### 练习 2.8.3 答案

```csharp
class TupleExercise
{
    // 使用元组返回多个统计值
    static (int Count, int Sum, double Average, int Min, int Max) 
        CalculateStatistics(List<int> numbers)
    {
        if (numbers.Count == 0)
            return (0, 0, 0, 0, 0);
        
        return (
            Count: numbers.Count,
            Sum: numbers.Sum(),
            Average: numbers.Average(),
            Min: numbers.Min(),
            Max: numbers.Max()
        );
    }
    
    static void Main()
    {
        var data = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };
        
        var stats = CalculateStatistics(data);
        
        Console.WriteLine($"统计结果:");
        Console.WriteLine($"  数量: {stats.Count}");
        Console.WriteLine($"  总和: {stats.Sum}");
        Console.WriteLine($"  平均: {stats.Average:F2}");
        Console.WriteLine($"  最小: {stats.Min}");
        Console.WriteLine($"  最大: {stats.Max}");
    }
}
```

#### 练习 2.8.4 答案

```csharp
class PatternMatchingExercise
{
    // 表达式类型
    interface IExpression { }
    
    class Number : IExpression
    {
        public double Value { get; }
        public Number(double value) => Value = value;
    }
    
    class BinaryOperation : IExpression
    {
        public IExpression Left { get; }
        public char Operator { get; }
        public IExpression Right { get; }
        
        public BinaryOperation(IExpression left, char op, IExpression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }
    
    // 使用模式匹配计算
    static double Evaluate(IExpression expr)
    {
        return expr switch
        {
            Number n => n.Value,
            
            BinaryOperation(var left, '+', var right) 
                => Evaluate(left) + Evaluate(right),
            
            BinaryOperation(var left, '-', var right) 
                => Evaluate(left) - Evaluate(right),
            
            BinaryOperation(var left, '*', var right) 
                => Evaluate(left) * Evaluate(right),
            
            BinaryOperation(var left, '/', var right) 
                => Evaluate(left) / Evaluate(right),
            
            _ => throw new ArgumentException("未知表达式")
        };
    }
    
    static void Main()
    {
        // 构建表达式: (2 + 3) * 4 = 20
        var expr = new BinaryOperation(
            new BinaryOperation(new Number(2), '+', new Number(3)),
            '*',
            new Number(4)
        );
        
        double result = Evaluate(expr);
        Console.WriteLine($"计算结果: {result}");  // 20
    }
}
```

### 相关知识点

- 1.1 值类型与引用类型 - 装箱拆箱的基础
- 2.4 泛型 - 避免装箱拆箱
- 2.7 LINQ - 大量使用委托和泛型

---

## 2.9 线程基础与并行

### 概念讲解

**线程（Thread）**是操作系统调度的基本单位，允许多个代码段同时执行。在 C# 中，可以通过 `System.Threading` 命名空间创建和管理线程。

**多线程的优势**：
- 提高应用程序的响应性（UI 线程不阻塞）
- 充分利用多核 CPU
- 并行处理多个任务

**并行编程**：
- 使用 `Parallel` 类进行并行循环
- 使用 PLINQ 进行并行 LINQ 查询

### 举例说明

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

class ThreadingDemo
{
    // ===== 1. 创建和启动线程 =====
    static void CreateThread()
    {
        // 创建新线程
        Thread t = new Thread(() =>
        {
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"子线程: {i}");
                Thread.Sleep(100);  // 模拟耗时操作
            }
        });
        
        t.Start();  // 启动线程
        
        // 主线程继续执行
        for (int i = 0; i < 3; i++)
        {
            Console.WriteLine($"主线程: {i}");
            Thread.Sleep(50);
        }
        
        t.Join();   // 等待子线程结束
        Console.WriteLine("主线程结束");
    }
    
    // ===== 2. 使用 Task（推荐） =====
    static async Task UseTask()
    {
        // 创建 Task
        Task t1 = Task.Run(() => Console.WriteLine("任务1"));
        Task t2 = Task.Run(() => Console.WriteLine("任务2"));
        
        // 等待所有任务完成
        await Task.WhenAll(t1, t2);
        
        // 带返回值的 Task
        Task<int> t3 = Task.Run(() =>
        {
            int sum = 0;
            for (int i = 0; i < 1000; i++) sum += i;
            return sum;
        });
        
        Console.WriteLine($"结果: {t3.Result}");
    }
    
    // ===== 3. Parallel.For 并行循环 =====
    static void ParallelLoop()
    {
        int[] data = Enumerable.Range(1, 100).ToArray();
        int[] result = new int[100];
        
        // 并行执行
        Parallel.For(0, 100, i =>
        {
            result[i] = data[i] * 2;
        });
        
        Console.WriteLine($"前5个: {string.Join(", ", result.Take(5))}");
    }
    
    // ===== 4. 线程安全 =====
    static int counter = 0;
    static object lockObj = new object();
    
    static void ThreadSafeCounter()
    {
        // 使用 lock 保证线程安全
        lock (lockObj)
        {
            counter++;
        }
        
        // 或使用 Interlocked 类
        Interlocked.Increment(ref counter);
    }
    
    // ===== 5. 线程局部存储 =====
    static ThreadLocal<int> threadLocal = new ThreadLocal<int>(() => 0);
    
    static void UseThreadLocal()
    {
        Thread t1 = new Thread(() =>
        {
            threadLocal.Value = 10;
            Console.WriteLine($"线程1: {threadLocal.Value}");
        });
        
        Thread t2 = new Thread(() =>
        {
            threadLocal.Value = 20;
            Console.WriteLine($"线程2: {threadLocal.Value}");
        });
        
        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();
    }
}
```

### 习题练习

#### 练习 2.9.1
使用 Task 实现并行下载多个文件

#### 练习 2.9.2
使用 Parallel.For 计算数组中每个元素的平方

### C++ 对比：线程与并发

```cpp
#include <iostream>
#include <thread>
#include <mutex>
#include <future>
#include <vector>
#include <algorithm>

// C++ 线程基础
void ThreadDemo() {
    // 1. 创建线程
    std::thread t([]() {
        for (int i = 0; i < 5; i++) {
            std::cout << "子线程: " << i << std::endl;
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
        }
    });
    
    for (int i = 0; i < 3; i++) {
        std::cout << "主线程: " << i << std::endl;
        std::this_thread::sleep_for(std::chrono::milliseconds(50));
    }
    t.join(); // 等待子线程结束，等价于 C# 的 t.Join()
    
    // 2. std::async（类似 C# Task）
    auto future = std::async(std::launch::async, []() {
        int sum = 0;
        for (int i = 0; i < 1000; i++) sum += i;
        return sum;
    });
    std::cout << "结果: " << future.get() << std::endl; // 等价于 t3.Result
    
    // 3. 并行循环（类似 C# Parallel.For）
    std::vector<int> data(100);
    std::iota(data.begin(), data.end(), 1);
    std::vector<int> result(100);
    
    #pragma omp parallel for  // OpenMP 并行化
    for (int i = 0; i < 100; i++) {
        result[i] = data[i] * 2;
    }
    
    // 4. 线程安全（互斥锁）
    int counter = 0;
    std::mutex mtx;
    
    // std::lock_guard = RAII 方式的 lock（推荐）
    {
        std::lock_guard<std::mutex> lock(mtx);
        counter++;
    } // 自动解锁
    
    // 或使用原子操作（无锁）
    std::atomic<int> atomicCounter{0};
    atomicCounter.fetch_add(1); // 等价于 Interlocked.Increment
}

// C# vs C++ 线程对比：
// ┌─────────────────────────┬────────────────────────────────┐
// │ C#                      │ C++                           │
// ├─────────────────────────┼────────────────────────────────┤
// │ Thread t = new Thread() │ std::thread t([](){})         │
// │ t.Start()               │ 构造时自动启动                 │
// │ t.Join()                │ t.join()                      │
// │ Task.Run()              │ std::async()                  │
// │ lock(lockObj){}         │ std::lock_guard<std::mutex>   │
// │ Interlocked.Increment   │ std::atomic::fetch_add        │
// │ Parallel.For            │ #pragma omp parallel for      │
// └─────────────────────────┴────────────────────────────────┘
```

### 相关知识点

- 2.5 异步编程 - async/await 的基础
- 2.7 LINQ - PLINQ 并行查询

---

## 2.10 表达式树（Expression）

### 概念讲解

**表达式树（Expression Tree）**是将代码表示为数据结构（树形结构）。它允许在运行时分析和修改代码，常用于：
- LINQ to SQL 将查询转换为 SQL
- 动态构建查询
- 延迟执行的 LINQ 查询

```
表达式树结构：

   Multiply
    /    \
  x       2

表示：x => x * 2
```

### 举例说明

```csharp
using System;
using System.Linq.Expressions;

class ExpressionTreeDemo
{
    static void Main()
    {
        // ===== 1. 创建表达式树 =====
        
        // 创建参数 x
        ParameterExpression x = Expression.Parameter(typeof(int), "x");
        
        // 创建表达式: x * 2
        Expression body = Expression.Multiply(x, Expression.Constant(2));
        
        // 创建 lambda: x => x * 2
        Expression<Func<int, int>> lambda = Expression.Lambda<Func<int, int>>(body, x);
        
        // 编译并执行
        Func<int, int> func = lambda.Compile();
        Console.WriteLine($"x * 2 = {func(5)}");  // 10
        
        // ===== 2. 动态构建查询条件 =====
        
        // 构建: n => n > 5
        ParameterExpression n = Expression.Parameter(typeof(int), "n");
        Expression greaterThanFive = Expression.GreaterThan(
            n,
            Expression.Constant(5)
        );
        
        var predicate = Expression.Lambda<Func<int, bool>>(greaterThanFive, n).Compile();
        
        int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var filtered = numbers.Where(predicate);
        Console.WriteLine($"大于5的数: {string.Join(", ", filtered)}");
        
        // ===== 3. 访问表达式树结构 =====
        
        Expression<Func<int, int, int>> addExpr = (a, b) => a + b;
        
        Console.WriteLine($"\n表达式类型: {addExpr.NodeType}");
        Console.WriteLine($"返回类型: {addExpr.ReturnType}");
        Console.WriteLine($"参数: {string.Join(", ", addExpr.Parameters)}");
        
        // 获取方法调用信息
        var methodCall = (MethodCallExpression)((BinaryExpression)addExpr.Body).Method;
        Console.WriteLine($"方法名: {methodCall.Method.Name}");
    }
}
```

### 相关知识点

- 2.7 LINQ - LINQ 底层使用表达式树
- 2.6 反射 - 表达式树使用反射获取成员信息

---

## 2.11 扩展方法

### 概念讲解

**扩展方法（Extension Method）**允许向现有类型添加新方法，而无需创建子类或修改原类型。扩展方法是静态方法，但调用时像实例方法一样。

### 举例说明

```csharp
using System;

// ===== 1. 基本扩展方法 =====

// 定义扩展方法（必须是静态类中的静态方法）
// 第一个参数 this 表示要扩展的类型
static class StringExtensions
{
    // 扩展 string 类型，添加 IsEmail 方法
    public static bool IsEmail(this string str)
    {
        return str != null && str.Contains("@") && str.Contains(".");
    }
    
    // 扩展方法可以带参数
    public static string Repeat(this string str, int count)
    {
        string result = "";
        for (int i = 0; i < count; i++)
            result += str;
        return result;
    }
    
    // 扩展泛型方法
    public static bool IsNullOrEmpty<T>(this T[] array)
    {
        return array == null || array.Length == 0;
    }
}

class ExtensionMethodDemo
{
    static void Main()
    {
        // 像调用实例方法一样使用扩展方法
        string email = "test@example.com";
        Console.WriteLine($"是邮箱: {email.IsEmail()}");  // True
        Console.WriteLine($"是邮箱: {"invalid".IsEmail()}");  // False
        
        // 使用带参数的扩展方法
        Console.WriteLine($"重复: {"Hi".Repeat(3)}");  // HiHiHi
        
        // ===== 2. 链式调用 =====
        
        // 扩展 LINQ 方法
        int[] numbers = { 1, 2, 3, 4, 5 };
        
        // 自定义扩展方法
        var result = numbers
            .Where(n => n > 2)
            .Select(n => n * 2)
            .ToList();
        
        // ===== 3. 创建自定义集合扩展 =====
    }
}

// ===== 2. 集合的扩展方法 =====

static class ListExtensions
{
    // 安全获取元素（索引越界返回默认值）
    public static T SafeGet<T>(this List<T> list, int index)
    {
        if (index < 0 || index >= list.Count)
            return default(T);
        return list[index];
    }
    
    // 交换元素位置
    public static void Swap<T>(this List<T> list, int i, int j)
    {
        if (i < 0 || i >= list.Count || j < 0 || j >= list.Count)
            return;
        
        T temp = list[i];
        list[i] = list[j];
        list[j] = temp;
    }
}

class ListExtensionDemo
{
    static void Main()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        
        // 使用扩展方法
        int value = list.SafeGet(10);  // 越界返回 0
        Console.WriteLine($"SafeGet: {value}");
        
        list.Swap(0, 4);  // 交换位置
        Console.WriteLine($"交换后: {string.Join(", ", list)}");  // 5,2,3,4,1
    }
}
```

### 习题练习

#### 练习 2.11.1
创建一个 int 数组的扩展方法，计算所有元素的阶乘

#### 练习 2.11.2
创建一个字符串扩展方法，实现单词首字母大写

### 相关知识点

- 2.7 LINQ - LINQ 方法就是扩展方法
- 2.4 泛型 - 泛型扩展方法

---

## 2.12 命名空间与程序集

### 概念讲解

**命名空间（Namespace）**用于组织代码，避免类型名称冲突。

**程序集（Assembly）**是 .NET 中编译和部署的基本单位（.dll 或 .exe 文件）。

### 举例说明

```csharp
// ===== 1. 命名空间定义 =====

// 定义命名空间
namespace MyCompany.MyProject.Models
{
    class User
    {
        public string Name { get; set; }
    }
}

// 嵌套命名空间
namespace MyCompany
{
    namespace MyProject
    {
        namespace Services
        {
            class UserService { }
        }
    }
}

// ===== 2. using 指令 =====

using MyCompany.MyProject.Models;      // 引入命名空间
using MyCompany.MyProject.Services;
using Alias = MyCompany.MyProject.Models.User;  // 别名

// ===== 3. 类型可见性 =====

// 同一命名空间中的类型可以相互访问
namespace NamespaceA
{
    class ClassA { }
    
    class ClassB  // 可以访问 ClassA
    {
        ClassA obj = new ClassA();
    }
}

namespace NamespaceB
{
    using NamespaceA;  // 引入后才能访问
    
    class ClassC
    {
        ClassA obj = new ClassA();  // 需要 using
    }
}

// ===== 4. 程序集引用 =====

// 项目A引用项目B的程序集
// 在代码中直接使用 B 的命名空间
// 编译时自动链接程序集
```

### 相关知识点

- 1.8 类与对象 - 类的组织方式
- 2.6 反射 - 获取程序集信息

---

## 2.13 垃圾回收机制（GC）

### 概念讲解

**垃圾回收（Garbage Collection）**是 .NET 自动管理内存的机制，用于回收不再使用的对象所占用的内存。

**GC 的工作原理**：
1. 标记阶段：标记所有可达对象
2. 清除阶段：清除不可达对象
3. 压缩阶段：压缩内存碎片
4. 分代回收：针对不同代使用不同策略

**GC 代**：
- 第 0 代：短生命周期对象
- 第 1 代：中等生命周期对象
- 第 2 代：长生命周期对象

### 举例说明

```csharp
using System;

class GCDemo
{
    static void Main()
    {
        // ===== 1. 强制垃圾回收 =====
        // 不推荐，但在特定场景下有用
        // GC.Collect();  // 强制回收
        // GC.WaitForPendingFinalizers();  // 等待终结器
        
        // ===== 2. 资源释放（IDisposable） =====
        using (var stream = new FileStream("test.txt", FileMode.Create))
        {
            // 使用资源
        }  // 自动调用 Dispose
        
        // ===== 3. 弱引用 =====
        // 允许垃圾回收器回收对象，但仍能访问
        var obj = new object();
        WeakReference<object> weak = new WeakReference<object>(obj);
        
        Console.WriteLine($"有引用: {weak.TryGetResult(out _)}");  // True
        obj = null;  // 清除引用
        GC.Collect();  // 强制回收
        Console.WriteLine($"无引用: {weak.TryGetResult(out _)}");  // False
        
        // ===== 4. 终结器（Finalizer） =====
        // 对象被回收前执行清理代码
        // 不推荐使用，因为会增加回收开销
    }
}

// 使用 using 和 Dispose
class ResourceHolder : IDisposable
{
    private bool disposed = false;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);  // 阻止终结器调用
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // 释放托管资源
            }
            // 释放非托管资源
            disposed = true;
        }
    }
    
    // 终结器（仅在未调用 Dispose 时执行）
    ~ResourceHolder()
    {
        Dispose(false);
    }
}
```

### C++ 对比：内存管理（无GC vs 有GC）

```cpp
#include <iostream>
#include <memory>
#include <vector>

// C++ 没有自动 GC，需要手动管理内存
void MemoryDemo() {
    // ===== C++ 内存分配方式 =====
    
    // 1. 栈上分配（自动释放，最快）
    int stackVar = 42;             // 函数结束时自动销毁
    
    // 2. 堆上分配（需手动释放）
    int* heapVar = new int(42);
    delete heapVar;               // 必须手动释放！
    heapVar = nullptr;            // 避免悬空指针
    
    // 3. RAII 智能指针（推荐，类似C#的using）
    std::unique_ptr<int> uptr = std::make_unique<int>(42);
    // 离开作用域时自动 delete，无需手动释放
    
    std::shared_ptr<int> sptr = std::make_shared<int>(42);
    // 引用计数，类似 C# 的 GC 但更确定
    
    // ===== 资源管理：RAII =====
    class FileHandle {
        FILE* file;
    public:
        FileHandle(const char* path) {
            fopen_s(&file, path, "r");
        }
        ~FileHandle() {
            if (file) fclose(file); // 析构时自动释放
        }
        // 禁止拷贝...
    }; // 类似 C# 的 IDisposable + using
    
    // ===== 内存泄漏检测 =====
    // C# 有 GC 兜底，C++ 泄漏就是真泄漏
    // 工具：Valgrind, AddressSanitizer
}

// C# vs C++ 内存管理对比：
// ┌───────────────────┬──────────────────────────────┐
// │ C#               │ C++                         │
// ├───────────────────┼──────────────────────────────┤
// │ new 自动上堆     │ 栈/堆由声明位置决定           │
// │ GC 自动回收      │ delete / 智能指针手动释放     │
// │ using + IDisposable│ RAII (析构函数)             │
// │ WeakReference    │ std::weak_ptr                │
// │ 几乎无内存泄漏   │ 需严格管理，易泄漏            │
// │ GC暂停有性能开销  │ 无GC暂停，性能可预测         │
// └───────────────────┴──────────────────────────────┘
```

### 相关知识点

- 1.1 值类型与引用类型 - GC 管理的是堆上的对象
- 2.25 序列化 - 序列化涉及对象创建和回收

---

## 2.14 序列化

### 概念讲解

**序列化（Serialization）**将对象转换为可存储或传输的格式，**反序列化**则相反。

常见序列化格式：
- JSON：轻量级，跨平台
- XML：结构化，可读性好
- 二进制：效率高

### 举例说明

```csharp
using System;
using System.Text.Json;     // .NET 5+ 内置
using System.Text.Json.Serialization;
using System.IO;
using System.Xml.Serialization;

class SerializationDemo
{
    // ===== 1. JSON 序列化 =====
    
    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        
        [JsonIgnore]  // 忽略属性
        public string Secret { get; set; }
        
        [JsonPropertyName("full_name")]  // 自定义属性名
        public string DisplayName { get; set; }
    }
    
    static void JsonSerialization()
    {
        Person p = new Person { Name = "张三", Age = 25 };
        
        // 序列化
        string json = JsonSerializer.Serialize(p);
        Console.WriteLine($"JSON: {json}");
        
        // 反序列化
        Person p2 = JsonSerializer.Deserialize<Person>(json);
        Console.WriteLine($"Name: {p2.Name}");
    }
    
    // ===== 2. XML 序列化 =====
    
    class XmlPerson
    {
        [XmlAttribute("name")]  // 作为属性
        public string Name { get; set; }
        
        [XmlElement("age")]
        public int Age { get; set; }
    }
    
    static void XmlSerialization()
    {
        XmlPerson p = new XmlPerson { Name = "李四", Age = 30 };
        
        // 序列化
        var serializer = new XmlSerializer(typeof(XmlPerson));
        using (var writer = new StringWriter())
        {
            serializer.Serialize(writer, p);
            Console.WriteLine($"XML: {writer.ToString()}");
        }
        
        // 反序列化
        string xml = "<XmlPerson name='王五' age='28' />";
        var p2 = (XmlPerson)serializer.Deserialize(new StringReader(xml));
    }
    
    // ===== 3. 二进制序列化（需要引用 System.Runtime.Serialization） =====
    
    // [Serializable]  // 标记可序列化
    // class BinaryPerson
    // {
    //     public string Name;
    //     public int Age;
    // }
    
    // static void BinarySerialization()
    // {
    //     var formatter = new BinaryFormatter();
    //     // 序列化
    //     using (var stream = new MemoryStream())
    //     {
    //         formatter.Serialize(stream, person);
    //         byte[] data = stream.ToArray();
    //     }
    // }
    
    // ===== 4. 自定义 JSON 转换 =====
    
    class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }
        
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
        }
    }
}
```

### 习题练习

#### 练习 2.14.1
使用 JSON 序列化一个学生列表到文件，并从文件反序列化

#### 练习 2.14.2
使用 XML 序列化一个配置对象

### 相关知识点

- 2.6 反射 - 序列化库使用反射获取属性
- 2.8 高级主题 - 元组与数据转换

---

## 2.15 依赖注入（DI）基础

### 概念讲解

**依赖注入（Dependency Injection）**是一种实现**控制反转（IoC）**的技术，通过外部注入依赖对象，减少类之间的耦合。

**DI 容器**负责管理对象的创建和生命周期。

### 举例说明

```csharp
using System;

// ===== 1. 依赖反转原则 =====

// 不好的设计：类自己创建依赖
class BadService
{
    private Database db = new Database();  // 强耦合
    
    public void Save() => db.Save();
}

// 好的设计：依赖注入
interface IDatabase
{
    void Save();
}

class MySqlDatabase : IDatabase
{
    public void Save() => Console.WriteLine("保存到 MySQL");
}

class SqlServerDatabase : IDatabase
{
    public void Save() => Console.WriteLine("保存到 SQL Server");
}

// 通过构造函数注入依赖
class GoodService
{
    private readonly IDatabase _database;
    
    public GoodService(IDatabase database)
    {
        _database = database;
    }
    
    public void Save() => _database.Save();
}

// ===== 2. 简单 DI 容器 =====

class SimpleContainer
{
    private Dictionary<Type, Type> _services = new Dictionary<Type, Type>();
    
    // 注册服务
    public void Register<TInterface, TImplementation>()
        where TImplementation : TInterface, new()
    {
        _services[typeof(TInterface)] = typeof(TImplementation);
    }
    
    // 解析服务
    public TInterface Resolve<TInterface>()
    {
        var type = _services[typeof(TInterface)];
        return (TInterface)Activator.CreateInstance(type);
    }
}

// ===== 3. 使用示例 =====

class DIDemo
{
    static void Main()
    {
        var container = new SimpleContainer();
        
        // 注册服务
        container.Register<IDatabase, MySqlDatabase>();
        container.Register<IService, GoodService>();
        
        // 解析并使用
        var service = container.Resolve<IService>();
        service.Save();
    }
}

interface IService
{
    void Save();
}
```

### 相关知识点

- 2.6 反射 - DI 容器使用反射创建实例
- 2.4 泛型 - 泛型服务注册
- 2.3 委托与事件 - 生命周期回调

---

## 2.16 设计模式

### 概念讲解

**设计模式**是软件开发中常见问题的可复用解决方案。

### 举例说明

```csharp
using System;

// ===== 1. 单例模式（Singleton） =====

// 线程安全单例（双重检查锁定）
class Singleton
{
    private static volatile Singleton _instance;
    private static readonly object _lock = new object();
    
    private Singleton() { }  // 私有构造函数
    
    public static Singleton Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new Singleton();
                }
            }
            return _instance;
        }
    }
}

// ===== 2. 工厂模式（Factory） =====

interface IProduct
{
    void Operation();
}

class ProductA : IProduct
{
    public void Operation() => Console.WriteLine("产品A");
}

class ProductB : IProduct
{
    public void Operation() => Console.WriteLine("产品B");
}

// 简单工厂
class SimpleFactory
{
    public static IProduct Create(string type)
    {
        return type switch
        {
            "A" => new ProductA(),
            "B" => new ProductB(),
            _ => throw new ArgumentException("未知类型")
        };
    }
}

// ===== 3. 观察者模式（Observer） =====

// 主题
class Subject
{
    private List<IObserver> _observers = new List<IObserver>();
    private string _state;
    
    public string State
    {
        get => _state;
        set
        {
            _state = value;
            Notify();
        }
    }
    
    public void Attach(IObserver observer) => _observers.Add(observer);
    public void Detach(IObserver observer) => _observers.Remove(observer);
    
    private void Notify()
    {
        foreach (var o in _observers)
            o.Update(_state);
    }
}

// 观察者接口
interface IObserver
{
    void Update(string state);
}

// 具体观察者
class ConcreteObserver : IObserver
{
    private string _name;
    
    public ConcreteObserver(string name) => _name = name;
    
    public void Update(string state)
    {
        Console.WriteLine($"{_name} 收到通知: {state}");
    }
}

// ===== 4. 建造者模式（Builder） =====

class User
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

class UserBuilder
{
    private User _user = new User();
    
    public UserBuilder Name(string name)
    {
        _user.Name = name;
        return this;
    }
    
    public UserBuilder Age(int age)
    {
        _user.Age = age;
        return this;
    }
    
    public UserBuilder Email(string email)
    {
        _user.Email = email;
        return this;
    }
    
    public User Build() => _user;
}

// 使用
class BuilderDemo
{
    static void Main()
    {
        var user = new UserBuilder()
            .Name("张三")
            .Age(25)
            .Email("test@example.com")
            .Build();
    }
}

// ===== 5. 策略模式（Strategy） =====

interface IPaymentStrategy
{
    void Pay(decimal amount);
}

class CreditCardPayment : IPaymentStrategy
{
    public void Pay(decimal amount) => Console.WriteLine($"信用卡支付: {amount}");
}

class PayPalPayment : IPaymentStrategy
{
    public void Pay(decimal amount) => Console.WriteLine($"PayPal支付: {amount}");
}

class ShoppingCart
{
    private IPaymentStrategy _paymentStrategy;
    
    public void SetPaymentStrategy(IPaymentStrategy strategy)
    {
        _paymentStrategy = strategy;
    }
    
    public void Checkout(decimal amount)
    {
        _paymentStrategy?.Pay(amount);
    }
}
```

### 习题练习

#### 练习 2.16.1
使用单例模式实现一个配置管理器

#### 练习 2.16.2
使用观察者模式实现一个新闻发布系统

### 相关知识点

- 2.15 依赖注入 - DI 容器常用设计模式
- 2.3 委托与事件 - 观察者模式的基础
- 2.4 泛型 - 泛型工厂模式

---

## 2.17 值类型深入（ref、out、in）

### 概念讲解

C# 提供了三个关键字用于更精细地控制值类型参数的传递方式：

| 关键字 | 作用 | 特点 |
|--------|------|------|
| ref | 传入引用 | 可读可写 |
| out | 传出参数 | 只写不读（方法内必须赋值） |
| in | 只读引用 | 只读不可写（.NET 7+） |

### 举例说明

```csharp
using System;

class RefOutInDemo
{
    // ===== ref: 传入引用，可读写 =====
    static void ModifyByRef(ref int value)
    {
        value *= 2;  // 修改原值
    }
    
    // ===== out: 传出参数，只写 =====
    static void GetValues(int[] arr, out int min, out int max)
    {
        min = arr[0];
        max = arr[0];
        foreach (var n in arr)
        {
            if (n < min) min = n;
            if (n > max) max = n;
        }
    }
    
    // ===== in: 只读引用（.NET 7+）=====
    static int CalculateSum(in int[] arr)
    {
        // arr[0] = 100;  // 错误！不能修改
        int sum = 0;
        foreach (var n in arr)
            sum += n;
        return sum;
    }
    
    // ===== ref 返回值 =====
    static ref int FindFirstEven(int[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] % 2 == 0)
                return ref arr[i];  // 返回引用
        }
        return ref arr[0];  // 返回第一个元素的引用
    }
    
    static void Main()
    {
        // ref 示例
        int num = 5;
        ModifyByRef(ref num);
        Console.WriteLine($"ref 结果: {num}");  // 10
        
        // out 示例
        int[] numbers = { 3, 1, 4, 1, 5, 9, 2, 6 };
        GetValues(numbers, out int min, out int max);
        Console.WriteLine($"out: min={min}, max={max}");
        
        // in 示例
        int sum = CalculateSum(numbers);
        Console.WriteLine($"in: sum={sum}");
        
        // ref 返回值示例
        ref int firstEven = ref FindFirstEven(numbers);
        firstEven = 100;  // 修改数组中的元素
        Console.WriteLine($"修改后: {numbers[2]}");  // 100
    }
}
```

### C++ 对比：值类型与引用传递

```cpp
#include <iostream>

// C++ 默认所有类型都是值类型（类似 C# struct）
struct Point {
    int x, y;
};

class Player {
public:
    int health = 100;
};

// 1. 按值传递（复制，类似 C# 值类型）
void ModifyByValue(Point p) {
    p.x = 99;  // 修改的是副本，原值不变
}

// 2. 按指针传递（类似 C# 引用类型）
void ModifyByPointer(Point* p) {
    if (p) p->x = 99;  // 修改原值
}

// 3. 按引用传递（C++ 特有，类似 C# ref）
void ModifyByReference(Point& p) {
    p.x = 99;  // 修改原值
}

// 4. 常量引用（类似 C# in）
void PrintByConstRef(const Point& p) {
    // p.x = 99; // 错误！不能修改
    std::cout << p.x << ", " << p.y << std::endl;
}

int main() {
    Point p1{1, 2};
    ModifyByValue(p1);           // p1 不变
    ModifyByPointer(&p1);        // p1 改变
    ModifyByReference(p1);       // p1 改变
    
    // C++ 默认值传递 vs C# 默认引用传递
    Player player1;              // 栈上分配
    Player* player2 = new Player(); // 堆上分配
    delete player2;
}
```

**C# vs C++ 值/引用语义对比：**

| 场景 | C# | C++ |
|------|-----|-----|
| 默认传递 | 引用类型(class)=引用，值类型(struct)=值 | 全部是值传递 |
| 引用传递 | ref/out 关键字 | & 引用符号 / * 指针 |
| 只读引用 | in 关键字 | const & |
| 堆上对象 | new 关键字 | new 或智能指针 |
| 栈上对象 | struct 声明 | 普通变量声明 |

---

### 相关知识点

- 1.5 方法 - 方法参数传递方式
- 1.1 值类型与引用类型 - ref/struct 区别

---

## 2.18 扩展阅读与最佳实践

### 补充内容

**值类型 vs 引用类型选择**：

| 场景 | 推荐 |
|------|------|
| 简单数据（数值、布尔、字符） | 值类型 |
| 对象较小且频繁创建 | 值类型（struct） |
| 不需要共享修改 | 值类型 |
| 复杂对象、继承层次 | 引用类型 |
| 需要在方法间传递且可能修改 | 引用类型 |

**性能优化建议**：

1. **避免不必要的装箱**
   - 使用泛型集合
   - 使用泛型方法

2. **减少对象分配**
   - 使用对象池
   - 使用 StringBuilder 处理大量字符串

3. **异步编程最佳实践**
   - I/O 密集型使用 async/await
   - CPU 密集型使用 Task.Run
   - 避免 async void（除非是事件处理）

4. **LINQ 性能**
   - 对大数据集使用 ToList/ToArray 缓存
   - 避免在循环中使用 LINQ

---

> 文档已完成！本指南涵盖了 C#/.NET 开发的核心知识点，包括基础篇（1.1-1.12）和进阶篇（2.1-2.18）。

---

# 附录A：Unity开发专题

## A.1 MonoBehaviour 生命周期

### 概念讲解

在Unity中，所有游戏对象脚本都继承自 `MonoBehaviour`。理解生命周期方法对于正确管理游戏逻辑至关重要。

```
MonoBehaviour 生命周期流程：

┌─────────────────────────────────────────────────────────────┐
│                      编辑器阶段                              │
├─────────────────────────────────────────────────────────────┤
│ Awake()      ─ 只调用一次，脚本启用时调用（即使未启用）      │
│ OnEnable()   ─ 每次脚本启用时调用                            │
├─────────────────────────────────────────────────────────────┤
│                      游戏循环（每帧）                         │
├─────────────────────────────────────────────────────────────┤
│ Start()      ─ 只调用一次，第一帧之前调用                    │
│ Update()     ─ 每帧调用，用于游戏逻辑                        │
│ LateUpdate() ─ 每帧调用，在所有 Update 后调用（相机跟随）    │
│ FixedUpdate()─ 固定时间间隔调用，物理计算                     │
├─────────────────────────────────────────────────────────────┤
│                      禁用/销毁阶段                           │
├─────────────────────────────────────────────────────────────┤
│ OnDisable()  ─ 脚本禁用时调用                                │
│ OnDestroy() ─ 脚本销毁时调用（对象销毁时）                   │
├─────────────────────────────────────────────────────────────┤
│                      编辑器退出                               │
├─────────────────────────────────────────────────────────────┤
│ OnApplicationQuit() ─ 应用退出前调用                         │
└─────────────────────────────────────────────────────────────┘
```

### 举例说明

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ===== Awake：初始化 =====
// 脚本所在的 GameObject 被实例化时调用（即使组件未启用也会调用）
// 常用于单例初始化、获取组件引用
    private void Awake()
    {
        Debug.Log("Awake 被调用");
        // 示例：单例初始化
        // instance = this;
    }
    
    // ===== OnEnable：每次启用时调用 =====
    private void OnEnable()
    {
        Debug.Log("OnEnable 被调用");
        // 订阅事件、注册委托
    }
    
    // ===== Start：第一次帧更新前调用 =====
    // 只调用一次，常用于游戏逻辑初始化
    private void Start()
    {
        Debug.Log("Start 被调用");
        // 示例：获取其他组件
        // rb = GetComponent<Rigidbody>();
    }
    
    // ===== Update：每帧调用 =====
    // 用于玩家输入、游戏逻辑
    private void Update()
    {
        // 检测输入
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("空格键按下");
        }
        
        // 移动逻辑
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, 0, vertical);
        transform.Translate(movement * Time.deltaTime * 5f);
    }
    
    // ===== FixedUpdate：固定时间间隔调用 =====
    // 用于物理计算，与帧率无关
    private void FixedUpdate()
    {
        // 物理力应用
        // rb.AddForce(Vector3.up * 10f);
    }
    
    // ===== LateUpdate：所有 Update 后调用 =====
    // 用于相机跟随、后期处理
    private void LateUpdate()
    {
        // 相机跟随逻辑
        // cameraTransform.position = transform.position + offset;
    }
    
    // ===== OnDisable：脚本禁用时调用 =====
    private void OnDisable()
    {
        Debug.Log("OnDisable 被调用");
        // 取消订阅、移除监听
    }
    
    // ===== OnDestroy：对象销毁时调用 =====
    private void OnDestroy()
    {
        Debug.Log("OnDestroy 被调用");
        // 清理资源、保存数据
    }
}
```

### 常见使用场景

| 生命周期方法 | 适用场景 |
|-------------|----------|
| Awake | 单例初始化、获取组件 |
| Start | 游戏逻辑初始化、延迟初始化 |
| Update | 输入检测、移动逻辑 |
| FixedUpdate | 物理计算、力应用 |
| LateUpdate | 相机跟随、依赖其他更新的逻辑 |
| OnDisable | 取消事件订阅、停止协程 |
| OnDestroy | 保存游戏数据、清理资源 |

---

## A.2 Unity 协程（Coroutine）

### 概念讲解

**协程**是 Unity 中实现异步操作的重要方式，它允许在指定帧或时间后继续执行代码，而不会阻塞主线程。

```
协程 vs 线程的区别：

线程：
- 真正的多线程
- 会阻塞主线程
- 消耗资源较多

协程：
- 在主线程上运行
- 非阻塞，通过 yield 暂停
- 适合游戏中的延时操作
```

### 举例说明

```csharp
using UnityEngine;
using System.Collections;

public class CoroutineDemo : MonoBehaviour
{
    // ===== 1. 启动协程 =====
    void Start()
    {
        // 方式1：直接启动
        StartCoroutine(ProcessAsync());
        
        // 方式2：带名称启动
        StartCoroutine("ProcessWithName");
        
        // 方式3：条件启动
        StartCoroutine(WaitAndLoad());
    }
    
    // ===== 2. 基础协程 =====
    // 返回 IEnumerator 类型
    IEnumerator ProcessAsync()
    {
        Debug.Log("开始处理...");
        
        // 等待指定时间（秒）
        yield return new WaitForSeconds(2f);
        
        Debug.Log("2秒后继续执行");
        
        // 等待下一帧
        yield return null;
        
        Debug.Log("下一帧继续");
        
        // 等待直到条件满足
        // yield return new WaitUntil(() => condition);
        
        // 等待固定帧数
        yield return new WaitForFrames(10);
        
        Debug.Log("10帧后继续");
    }
    
    // ===== 3. 协程返回值 =====
    IEnumerator<string> LoadData()
    {
        // 模拟加载
        yield return new WaitForSeconds(1f);
        
        // 返回结果
        yield return "数据加载完成";
    }
    
    // 接收返回值
    IEnumerator GetResult()
    {
        var enumerator = LoadData();
        yield return enumerator;
        Debug.Log(enumerator.Current);  // 输出：数据加载完成
    }
    
    // ===== 4. 停止协程 =====
    private Coroutine myCoroutine;
    
    void StartCoroutineDemo()
    {
        // 启动
        myCoroutine = StartCoroutine(ProcessAsync());
        
        // 停止指定协程
        if (myCoroutine != null)
            StopCoroutine(myCoroutine);
        
        // 停止所有协程
        StopAllCoroutines();
    }
    
    // ===== 5. 实际应用场景 =====
    
    // 场景1：倒计时
    IEnumerator Countdown(int seconds)
    {
        while (seconds > 0)
        {
            Debug.Log($"倒计时: {seconds}");
            yield return new WaitForSeconds(1f);
            seconds--;
        }
        Debug.Log("开始游戏！");
    }
    
    // 场景2：淡入淡出
    IEnumerator FadeInOut()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        
        // 淡入
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime;
            yield return null;
        }
        
        // 等待
        yield return new WaitForSeconds(2f);
        
        // 淡出
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime;
            yield return null;
        }
    }
    
    // 场景3：分帧处理大量数据
    IEnumerator ProcessLargeData()
    {
        // 假设有大量数据需要处理
        int totalCount = 10000;
        
        for (int i = 0; i < totalCount; i++)
        {
            // 处理单个数据
            ProcessData(i);
            
            // 每100帧暂停一下，避免卡顿
            if (i % 100 == 0)
                yield return null;
        }
    }
    
    void ProcessData(int index)
    {
        // 处理逻辑
    }
    
    // ===== 6. WaitForSeconds vs WaitForSecondsRealtime ======
    
    IEnumerator CompareWaitTimes()
    {
        // 受 Time.timeScale 影响（默认1）
        yield return new WaitForSeconds(2f);
        
        // 不受 Time.timeScale 影响
        yield return new WaitForSecondsRealtime(2f);
    }
    
    // ===== 7. 并行协程 =====
    IEnumerator ParallelCoroutines()
    {
        // 同时运行多个协程
        var a = StartCoroutine(CoroutineA());
        var b = StartCoroutine(CoroutineB());
        
        // 等待所有完成
        yield return new WaitUntil(() => 
            GetComponent<CoroutineDemo>() == null || 
            IsCoroutineComplete(a) && IsCoroutineComplete(b));
    }
    
    IEnumerator CoroutineA()
    {
        yield return new WaitForSeconds(1f);
    }
    
    IEnumerator CoroutineB()
    {
        yield return new WaitForSeconds(2f);
    }
    
    bool IsCoroutineComplete(Coroutine coroutine)
    {
        // 通过反射检查协程状态
        return true; // 简化示例
    }
}
```

### 习题练习

#### 练习 A.2.1
创建一个倒计时器，倒计时结束后显示"游戏开始"

#### 练习 A.2.2
使用协程实现一个简单的加载条

### 相关知识点

- A.1 MonoBehaviour 生命周期
- 2.3 委托与事件

---

## A.3 UnityEvent 与 事件系统

### 概念讲解

**UnityEvent** 是 Unity 内置的事件系统，它允许在 Inspector 中配置事件响应，无需编写代码即可连接事件。

```
UnityEvent 的优势：

1. 可在 Inspector 中可视化配置
2. 无需编写事件订阅代码
3. 支持持久化保存
4. 支持动态参数
```

### 举例说明

```csharp
using UnityEngine;
using UnityEngine.Events;

// ===== 1. 自定义 UnityEvent =====
[System.Serializable]
public class MyEvent : UnityEvent<string> { }

public class EventSystemDemo : MonoBehaviour
{
    // ===== 基础 UnityEvent（无参数） =====
    public UnityEvent onStartGame;
    public UnityEvent onPauseGame;
    public UnityEvent onGameOver;
    
    // ===== 带参数的 UnityEvent =====
    public UnityEvent<int> onScoreChanged;
    public UnityEvent<string, int> onPlayerDamaged;
    
    // ===== 自定义 UnityEvent =====
    public MyEvent onCustomEvent;
    
    void Start()
    {
        // 编程方式触发事件
        onStartGame?.Invoke();
        onScoreChanged?.Invoke(100);
        onCustomEvent?.Invoke("Hello");
    }
}

// ===== 2. 事件监听 =====

// 方式1：编辑器中拖拽（见下文）
// 方式2：代码订阅
public class EventListener : MonoBehaviour
{
    public EventSystemDemo eventSource;
    
    void OnEnable()
    {
        if (eventSource != null)
        {
            eventSource.onStartGame.AddListener(OnGameStart);
            eventSource.onScoreChanged.AddListener(OnScoreChanged);
        }
    }
    
    void OnDisable()
    {
        if (eventSource != null)
        {
            eventSource.onStartGame.RemoveListener(OnGameStart);
            eventSource.onScoreChanged.RemoveListener(OnScoreChanged);
        }
    }
    
    void OnGameStart()
    {
        Debug.Log("游戏开始！");
    }
    
    void OnScoreChanged(int score)
    {
        Debug.Log($"分数变化: {score}");
    }
}

// ===== 3. 持久化事件（编辑器中配置）=====
/*
 * 在 Inspector 中配置步骤：
 * 1. 创建脚本并声明 UnityEvent 字段
 * 2. 在 Inspector 中找到该字段
 * 3. 点击 "+" 添加响应
 * 4. 拖拽目标对象到 Object 字段
 * 5. 选择要调用的方法
 */

// ===== 4. 动态添加监听 =====
public class DynamicListener : MonoBehaviour
{
    public UnityEvent myEvent;
    
    void Start()
    {
        // 动态添加监听
        myEvent.AddListener(() => Debug.Log("被触发！"));
        
        // 带参数版本
        myEvent.AddListener((message) => Debug.Log(message));
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            myEvent?.Invoke();
        }
    }
}

// ===== 5. 泛型 UnityEvent =====
public class GenericEventDemo : MonoBehaviour
{
    // UnityEvent 支持最多4个参数
    public UnityEvent;
    public UnityEvent<bool>;
    public UnityEvent<int, float>;
    public UnityEvent<string, int, bool>;
    
    void Start()
    {
        this.Invoke("TestMethod", 1f);
    }
    
    public void TestMethod() { }
    public void TestMethodBool(bool value) { }
    public void TestMethodIntFloat(int i, float f) { }
}
```

### UnityEditor 中配置图解

```
┌─────────────────────────────────────────────┐
│  EventSystemDemo (Script)                   │
├─────────────────────────────────────────────┤
│  On Start Game                              │
│  ┌─────────────────────────────────────────┐ │
│  │ + ────────────────────────────────────  │ │
│  └─────────────────────────────────────────┘ │
│  ▼ Runtime Only                             │
│  None (MonoBehaviour)                    ▼ │
│  No Function                            ▼ │
├─────────────────────────────────────────────┤
│  On Score Changed (Int)                     │
│  ┌─────────────────────────────────────────┐ │
│  │ + ────────────────────────────────────  │ │
│  └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘

配置步骤：
1. 在 Inspector 中找到事件字段
2. 点击 "+" 按钮添加响应
3. 拖拽要接收事件的对象
4. 选择要调用的方法
5. （可选）传入静态参数
```

### 相关知识点

- 2.3 委托与事件 - 底层原理相同
- 2.5 异步编程 - 与协程配合使用

---

## A.4 ScriptableObject

### 概念讲解

**ScriptableObject** 是 Unity 中的一种数据容器，用于存储游戏数据。它与 MonoBehaviour 的区别：

| 特性 | MonoBehaviour | ScriptableObject |
|------|----------------|------------------|
| 需要 GameObject | 是 | 否 |
| 继承自 | Component | Object |
| 编辑器保存 | 场景中 | 资源文件中 |
| 用途 | 组件逻辑 | 数据存储 |
| 内存 | 场景相关 | 资源独立 |

### 举例说明

```csharp
using UnityEngine;

// ===== 1. 创建 ScriptableObject =====

// 定义数据类
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;
    public int price;
    public bool isStackable;
    public int maxStackSize;
}

// 创建后会在菜单中显示：Game/Item
// 点击后会在 Project 窗口创建 .asset 文件
```

```csharp
// ===== 2. 使用 ScriptableObject =====

public class ItemManager : MonoBehaviour
{
    // 在 Inspector 中拖拽资源
    public ItemData[] allItems;
    
    void Start()
    {
        // 遍历所有物品
        foreach (var item in allItems)
        {
            Debug.Log($"物品: {item.itemName}, 价格: {item.price}");
        }
    }
}

// ===== 3. 动态创建 ScriptableObject =====

public class DynamicSO : MonoBehaviour
{
    void CreateData()
    {
        // 方式1：代码创建（不保存到资源）
        var data = ScriptableObject.CreateInstance<ItemData>();
        data.itemName = "临时物品";
        data.price = 100;
        
        // 方式2：创建并保存到资源文件
        var asset = ScriptableObject.CreateInstance<ItemData>();
        asset.itemName = "装备";
        asset.price = 500;
        
        // 保存到 Assets/Resources/Items/equipment.asset
        // AssetDatabase.CreateAsset(asset, "Assets/Resources/Items/equipment.asset");
    }
}

// ===== 4. 运行时加载 =====

public class RuntimeLoader : MonoBehaviour
{
    // 方式1：直接引用
    public ItemData weaponData;
    
    // 方式2：Resources 加载
    IEnumerator LoadFromResources()
    {
        var data = Resources.Load<ItemData>("Items/weapon");
        if (data != null)
        {
            Debug.Log($"加载: {data.itemName}");
        }
    }
    
    // 方式3： Addressables 加载（推荐）
    // 需要在 Package Manager 中安装 Addressables
    /*
    IEnumerator LoadFromAddressables()
    {
        var handle = Addressables.LoadAssetAsync<ItemData>("weapon_data");
        yield return handle;
        var data = handle.Result;
    }
    */
}

// ===== 5. ScriptableObject 作为配置 =====

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Config")]
public class GameConfig : ScriptableObject
{
    [Header("游戏设置")]
    public float gameSpeed = 1f;
    public int maxPlayers = 4;
    
    [Header("玩家设置")]
    public float playerSpeed = 5f;
    public int initialHealth = 100;
    
    [Header("难度设置")]
    public float enemySpawnRate = 1f;
    public float enemyHealthMultiplier = 1f;
}

// 使用配置
public class UseConfig : MonoBehaviour
{
    public GameConfig config;
    
    void Start()
    {
        Debug.Log($"游戏速度: {config.gameSpeed}");
        Debug.Log($"初始生命: {config.initialHealth}");
    }
}

// ===== 6. 事件型 ScriptableObject =====
public class GameEvents : ScriptableObject
{
    public delegate void ScoreEvent(int score);
    public event ScoreEvent OnScoreChanged;
    
    public void RaiseScoreChanged(int score)
    {
        OnScoreChanged?.Invoke(score);
    }
}

// 全局事件管理器
public class GlobalEvents : MonoBehaviour
{
    public static GameEvents ScoreEvent = null;
    
    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        // 在游戏开始时创建
        ScoreEvent = ScriptableObject.CreateInstance<GameEvents>();
    }
}
```

### 相关知识点

- A.3 UnityEvent - 可以绑定 ScriptableObject 事件
- 2.4 泛型 - 泛型 ScriptableObject

---

## A.5 Unity 中的集合与泛型

### 举例说明

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UnityCollections : MonoBehaviour
{
    // ===== 1. List 在 Unity 中的使用 =====
    
    List<Enemy> enemies = new List<Enemy>();
    
    void ManageEnemies()
    {
        // 添加敌人
        enemies.Add(new Enemy());
        
        // 移除死亡敌人（倒序遍历安全）
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i].IsDead)
            {
                enemies.RemoveAt(i);
            }
        }
        
        // 使用 LINQ 查找
        var livingEnemies = enemies.Where(e => !e.IsDead).ToList();
        var boss = enemies.FirstOrDefault(e => e.IsBoss);
    }
    
    // ===== 2. Dictionary 存储游戏数据 =====
    
    Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();
    Dictionary<int, Player> players = new Dictionary<int, Player>();
    
    void SetupDatabase()
    {
        // 初始化物品数据库
        itemDatabase.Add("sword", new ItemData { itemName = "剑" });
        itemDatabase.Add("shield", new ItemData { itemName = "盾" });
        
        // 查找物品
        if (itemDatabase.TryGetValue("sword", out var item))
        {
            Debug.Log($"找到物品: {item.itemName}");
        }
    }
    
    // ===== 3. 队列和栈的应用 =====
    
    Queue<Wave> waveQueue = new Queue<Wave>();
    Stack<Command> commandStack = new Stack<Command>();
    
    // ===== 4. HashSet 去重 =====
    
    HashSet<string> unlockedAchievements = new HashSet<string>();
    
    void UnlockAchievement(string achievementId)
    {
        if (unlockedAchievements.Add(achievementId))
        {
            Debug.Log($"解锁成就: {achievementId}");
        }
    }
    
    // ===== 5. Unity 特定的集合类型 =====
    
    // Serializable 序列化（用于 Inspector 显示）
    [SerializeField]
    private List<Quest> activeQuests = new List<Quest>();
    
    // Serializable 字典（旧版 Unity）
    [System.Serializable]
    public class StringItemPair { public string key; public ItemData value; }
    [SerializeField]
    private List<StringItemPair> itemPairs = new List<StringItemPair>();
}

// 敌人数据类
public class Enemy
{
    public bool IsDead { get; set; }
    public bool IsBoss { get; set; }
}

// 波次数据
public class Wave { }

// 命令模式
public class Command { }

// 任务
public class Quest { }
```

### 相关知识点

- 1.7 集合 - 基础集合类型
- 2.7 LINQ - LINQ 在 Unity 中的使用

---

# 附录B：.NET 后端开发专题

## B.1 异常处理

### 概念讲解

**异常处理**是后端开发中保证系统稳定性的关键机制。

```
异常处理结构：

try
{
    // 可能抛出异常的代码
}
catch (ExceptionType1 ex)  // 特定异常
{
    // 处理特定异常
}
catch (Exception ex)       // 通用异常
{
    // 处理其他异常
    // 记录日志
    // 返回错误信息
}
finally
{
    // 无论是否异常都执行
    // 释放资源、关闭连接
}
```

### 举例说明

```csharp
using System;
using System.IO;

namespace ExceptionHandlingDemo
{
    // ===== 1. 基本异常处理 =====
    
    class BasicException
    {
        public void ProcessData(string input)
        {
            try
            {
                int number = int.Parse(input);
                int result = 10 / number;
                Console.WriteLine($"结果: {result}");
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"格式错误: {ex.Message}");
            }
            catch (DivideByZeroException ex)
            {
                Console.WriteLine($"除零错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"未知错误: {ex.Message}");
            }
        }
    }
    
    // ===== 2. 自定义异常 =====
    
    // 定义业务异常
    public class BusinessException : Exception
    {
        public int ErrorCode { get; }
        
        public BusinessException(string message, int errorCode) 
            : base(message)
        {
            ErrorCode = errorCode;
        }
        
        public BusinessException(string message, int errorCode, Exception inner)
            : base(message, inner)
        {
            ErrorCode = errorCode;
        }
    }
    
    // 使用自定义异常
    class UseCustomException
    {
        public void ValidateAge(int age)
        {
            if (age < 0)
                throw new BusinessException("年龄不能为负", 1001);
            
            if (age > 150)
                throw new BusinessException("年龄超出范围", 1002);
        }
    }
    
    // ===== 3. 异常过滤器（C# 6+） =====
    
    class ExceptionFilter
    {
        public void Process()
        {
            try
            {
                RiskyOperation();
            }
            catch (Exception ex) when (ex.Message.Contains("timeout"))
            {
                // 只处理超时相关的异常
                Console.WriteLine("处理超时...");
            }
            catch (Exception ex) when (ex.Message.Contains("database"))
            {
                // 只处理数据库相关的异常
                Console.WriteLine("处理数据库错误...");
            }
        }
        
        void RiskyOperation() { }
    }
    
    // ===== 4. finally 清理资源 =====
    
    class ResourceCleanup
    {
        public void ReadFile(string path)
        {
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(path);
                string content = reader.ReadToEnd();
                Console.WriteLine(content);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"文件不存在: {ex.FileName}");
            }
            finally
            {
                // 确保资源被释放
                reader?.Close();
            }
        }
        
        // C# 8+ using 语法（推荐）
        public void ReadFileModern(string path)
        {
            try
            {
                using var reader = new StreamReader(path);
                string content = reader.ReadToEnd();
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"文件不存在: {ex.FileName}");
            }
            // using 自动调用 Dispose
        }
    }
    
    // ===== 5. 全局异常处理（ASP.NET Core） =====
    /*
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "未处理的异常");
                await HandleExceptionAsync(context, ex);
            }
        }
        
        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            
            var result = JsonSerializer.Serialize(new 
            { 
                error = ex.Message,
                stack = ex.StackTrace
            });
            
            await context.Response.WriteAsync(result);
        }
    }
    */
    
    // ===== 6. 异常与日志记录 =====
    
    class LoggingException
    {
        // 实际项目中应该使用依赖注入的 Logger
        public void ProcessWithLogging(string input)
        {
            try
            {
                // 业务逻辑
                int.Parse(input);
            }
            catch (Exception ex)
            {
                // 记录详细日志
                // logger.LogError(ex, "解析失败，输入: {Input}", input);
                
                // 转换为用户友好的异常
                throw new InvalidOperationException("输入数据格式不正确，请检查", ex);
            }
        }
    }
}
```

### 最佳实践

| 实践 | 说明 |
|------|------|
| 捕获特定异常 | 不要 catch 所有异常 |
| 不要吞掉异常 | 至少记录日志 |
| 抛出有意义的异常 | 自定义异常包含错误码 |
| 使用 finally 或 using | 确保资源释放 |
| 全局异常处理 | 防止程序崩溃 |

### 相关知识点

- B.2 文件I/O - 文件操作中的异常处理
- B.4 数据库操作 - 数据库异常处理

---

## B.2 文件 I/O 操作

### 概念讲解

.NET 提供了丰富的文件操作类：

| 类 | 用途 |
|------|------|
| File | 静态方法，操作文件 |
| FileInfo | 实例方法，获取文件信息 |
| Directory | 静态方法，操作目录 |
| StreamReader/Writer | 文本读写 |
| BinaryReader/Writer | 二进制读写 |
| FileStream | 字节流操作 |

### 举例说明

```csharp
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FileIODemo
{
    // ===== 1. 文件基本操作 =====
    
    class BasicFileOps
    {
        // 判断文件是否存在
        bool Exists(string path) => File.Exists(path);
        
        // 创建文件
        void CreateFile(string path, string content)
        {
            File.WriteAllText(path, content);
            // 或
            File.WriteAllText(path, content, Encoding.UTF8);
        }
        
        // 读取文件
        string ReadFile(string path)
        {
            return File.ReadAllText(path);
            // 或
            return File.ReadAllText(path, Encoding.UTF8);
        }
        
        // 读取行
        string[] ReadLines(string path)
        {
            return File.ReadAllLines(path);
        }
        
        // 追加内容
        void AppendFile(string path, string content)
        {
            File.AppendAllText(path, content);
        }
        
        // 删除文件
        void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        
        // 复制文件
        void CopyFile(string source, string dest)
        {
            File.Copy(source, dest, true);  // true 覆盖已存在
        }
        
        // 移动文件
        void MoveFile(string source, string dest)
        {
            File.Move(source, dest);
        }
    }
    
    // ===== 2. 目录操作 =====
    
    class DirectoryOps
    {
        // 创建目录
        void CreateDirectory(string path)
        {
            // 创建多层目录
            Directory.CreateDirectory(path);
        }
        
        // 获取文件列表
        string[] GetFiles(string folderPath)
        {
            // 所有文件
            return Directory.GetFiles(folderPath);
            
            // 指定扩展名
            return Directory.GetFiles(folderPath, "*.txt");
            
            // 递归搜索
            return Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        }
        
        // 获取子目录
        string[] GetDirectories(string path)
        {
            return Directory.GetDirectories(path);
        }
        
        // 删除目录
        void DeleteDirectory(string path, bool recursive = true)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive);
        }
        
        // 获取文件信息
        void GetFileInfo(string path)
        {
            FileInfo info = new FileInfo(path);
            
            Console.WriteLine($"名称: {info.Name}");
            Console.WriteLine($"大小: {info.Length} 字节");
            Console.WriteLine($"创建: {info.CreationTime}");
            Console.WriteLine($"修改: {info.LastWriteTime}");
            Console.WriteLine($"扩展名: {info.Extension}");
        }
    }
    
    // ===== 3. 流读写操作 =====
    
    class StreamOps
    {
        // 使用 StreamReader 读取
        void ReadWithStreamReader(string path)
        {
            using StreamReader reader = new StreamReader(path);
            
            // 读取全部
            string content = reader.ReadToEnd();
            
            // 按行读取
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                Console.WriteLine(line);
            }
            
            // 读取指定字符数
            char[] buffer = new char[1024];
            reader.Read(buffer, 0, buffer.Length);
        }
        
        // 使用 StreamWriter 写入
        void WriteWithStreamWriter(string path, string content)
        {
            using StreamWriter writer = new StreamWriter(path, append: true);
            
            writer.WriteLine(content);
            writer.Write("不带换行");
            
            // 刷新缓冲区
            writer.Flush();
        }
        
        // 使用 FileStream 读写字节
        void ReadWriteBytes(string path)
        {
            byte[] data = { 1, 2, 3, 4, 5 };
            
            // 写入
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                fs.Write(data, 0, data.Length);
            }
            
            // 读取
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
            }
        }
    }
    
    // ===== 4. 异步文件操作 =====
    
    class AsyncFileOps
    {
        // 异步读取
        async Task<string> ReadFileAsync(string path)
        {
            return await File.ReadAllTextAsync(path);
        }
        
        // 异步写入
        async Task WriteFileAsync(string path, string content)
        {
            await File.WriteAllTextAsync(path, content);
        }
        
        // 异步追加
        async Task AppendFileAsync(string path, string content)
        {
            await File.AppendAllTextAsync(path, content);
        }
        
        // 批量文件处理
        async Task ProcessFilesAsync(string[] paths)
        {
            foreach (var path in paths)
            {
                var content = await ReadFileAsync(path);
                // 处理内容
                await WriteFileAsync(path + ".processed", content);
            }
        }
    }
    
    // ===== 5. 临时文件 =====
    
    class TempFileOps
    {
        void UseTempFile()
        {
            // 创建临时文件
            string tempPath = Path.GetTempFileName();
            
            // 创建临时目录
            string tempDir = Path.GetTempPath();
            
            // 使用完删除
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            
            // 使用 GUID 创建唯一文件名
            string uniqueFile = Path.Combine(Path.GetTempPath(), 
                $"data_{Guid.NewGuid()}.txt");
        }
    }
    
    // ===== 6. Path 类的使用 =====
    
    class PathOps
    {
        void ProcessPath(string filePath)
        {
            Console.WriteLine($"目录: {Path.GetDirectoryName(filePath)}");
            Console.WriteLine($"文件名: {Path.GetFileName(filePath)}");
            Console.WriteLine($"无扩展名: {Path.GetFileNameWithoutExtension(filePath)}");
            Console.WriteLine($"扩展名: {Path.GetExtension(filePath)}");
            Console.WriteLine($"根目录: {Path.GetPathRoot(filePath)}");
            
            // 组合路径
            string combined = Path.Combine("C:", "Folder", "SubFolder", "file.txt");
            Console.WriteLine($"组合: {combined}");
            
            // 跨平台路径处理
            string linuxPath = "folder/subfolder/file.txt";
            Console.WriteLine($"跨平台: {linuxPath.Replace('/', Path.DirectorySeparatorChar)}");
        }
    }
}
```

### 相关知识点

- B.1 异常处理 - 文件操作需要异常处理
- 2.14 序列化 - 序列化到文件

---

## B.3 网络编程基础

### 概念讲解

.NET 后端开发中，网络请求是基础能力：

| 类型 | 用途 |
|------|------|
| HttpClient | HTTP 请求 |
| WebClient | 简单 HTTP 操作 |
| HttpWebRequest | 底层 HTTP |
| TcpListener/TcpClient | TCP 协议 |
| UdpClient | UDP 协议 |

### 举例说明

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace NetworkDemo
{
    // ===== 1. HttpClient 基本使用 =====
    
    class HttpClientDemo
    {
        private readonly HttpClient _httpClient;
        
        public HttpClientDemo()
        {
            // 创建 HttpClient 实例
            _httpClient = new HttpClient();
            
            // 配置超时
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // 设置默认请求头
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0");
        }
        
        // GET 请求
        async Task<string> GetAsync(string url)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        
        // POST 请求
        async Task<string> PostAsync(string url, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        
        // PUT 请求
        async Task PutAsync(string url, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await _httpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
        }
        
        // DELETE 请求
        async Task DeleteAsync(string url)
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
        }
    }
    
    // ===== 2. 带错误处理的 HTTP =====
    
    class HttpWithErrorHandling
    {
        private readonly HttpClient _client = new HttpClient();
        
        public async Task<Response<T>> GetSafely<T>(string url)
        {
            try
            {
                var response = await _client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<T>(json);
                    return new Response<T> { Success = true, Data = data };
                }
                else
                {
                    return new Response<T> 
                    { 
                        Success = false, 
                        ErrorMessage = $"HTTP {response.StatusCode}" 
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                return new Response<T> 
                { 
                    Success = false, 
                    ErrorMessage = $"网络错误: {ex.Message}" 
                };
            }
            catch (TaskCanceledException)
            {
                return new Response<T> 
                { 
                    Success = false, 
                    ErrorMessage = "请求超时" 
                };
            }
        }
    }
    
    // 响应包装类
    public class Response<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    // ===== 3. 并行 HTTP 请求 =====
    
    class ParallelRequests
    {
        private readonly HttpClient _client = new HttpClient();
        
        public async Task<string[]> GetMultipleUrls(string[] urls)
        {
            var tasks = urls.Select(url => _client.GetStringAsync(url));
            return await Task.WhenAll(tasks);
        }
        
        public async Task<string[]> GetWithThrottling(string[] urls, int maxConcurrent = 5)
        {
            using var semaphore = new SemaphoreSlim(maxConcurrent);
            
            var tasks = urls.Select(async url =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await _client.GetStringAsync(url);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            return await Task.WhenAll(tasks);
        }
    }
    
    // ===== 4. REST API 客户端封装 =====
    
    class RestApiClient
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        
        public RestApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        }
        
        public async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json);
        }
        
        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _client.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(result);
        }
        
        public async Task PutAsync(string endpoint, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _client.PutAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
        }
        
        public async Task DeleteAsync(string endpoint)
        {
            var response = await _client.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();
        }
    }
    
    // 使用示例
    // class Program
    // {
    //     static async Task Main()
    //     {
    //         var client = new RestApiClient("https://api.example.com");
    //         
    //         // 获取用户
    //         var user = await client.GetAsync<User>("/users/1");
    //         
    //         // 创建订单
    //         var order = await client.PostAsync<Order>("/orders", newOrder);
    //     }
    // }
    // 
    // public class User { public int Id { get; set; } public string Name { get; set; } }
    // public class Order { public int Id { get; set; } public decimal Amount { get; set; } }
    
    // ===== 5. JSON 处理 =====
    
    class JsonHandling
    {
        // 序列化
        string Serialize<T>(T obj)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            return JsonSerializer.Serialize(obj, options);
        }
        
        // 反序列化
        T Deserialize<T>(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<T>(json, options);
        }
    }
}
```

### C++ 对比：网络编程

```cpp
#include <iostream>
#include <string>
#include <curl/curl.h> // libcurl (C++标准无内置HTTP)
#include <thread>
#include <future>
#include <mutex>
#include <vector>
#include <sstream>

// C++ 网络编程需要第三方库（如 libcurl、Boost.Asio）
// .NET 内置 HttpClient 开箱即用

// 1. HTTP 请求（使用 libcurl）
size_t WriteCallback(void* contents, size_t size, size_t nmemb, std::string* output) {
    size_t totalSize = size * nmemb;
    output->append((char*)contents, totalSize);
    return totalSize;
}

std::string HttpGet(const std::string& url) {
    CURL* curl = curl_easy_init();
    std::string response;
    
    if (curl) {
        curl_easy_setopt(curl, CURLOPT_URL, url.c_str());
        curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
        curl_easy_setopt(curl, CURLOPT_WRITEDATA, &response);
        curl_easy_setopt(curl, CURLOPT_TIMEOUT, 30L);
        
        CURLcode res = curl_easy_perform(curl);
        if (res != CURLE_OK) {
            std::cerr << "curl失败: " << curl_easy_strerror(res) << std::endl;
        }
        curl_easy_cleanup(curl);
    }
    return response;
}

// 2. TCP Socket（原生，无第三方）
#include <winsock2.h>
#pragma comment(lib, "ws2_32.lib")

void TcpSocketDemo() {
    WSADATA wsaData;
    WSAStartup(MAKEWORD(2, 2), &wsaData);
    
    SOCKET sock = socket(AF_INET, SOCK_STREAM, 0);
    sockaddr_in server;
    server.sin_family = AF_INET;
    server.sin_port = htons(8080);
    inet_pton(AF_INET, "127.0.0.1", &server.sin_addr);
    
    connect(sock, (sockaddr*)&server, sizeof(server));
    // send/recv...
    
    closesocket(sock);
    WSACleanup();
}

// C# vs C++ 网络对比：
// ┌───────────────────┬────────────────────────────────┐
// │ C#               │ C++                           │
// ├───────────────────┼────────────────────────────────┤
// │ HttpClient 内置   │ 需第三方库（libcurl/Boost）    │
// │ TcpClient 封装好  │ 直接调用 Socket API（winsock）│
// │ async/await 优雅  │ 回调/特征值/future 较繁琐      │
// │ JSON 内置序列化   │ 需 nlohmann/json 等第三方库    │
// │ UnityWebRequest   │ 无游戏引擎自带方案              │
// └───────────────────┴────────────────────────────────┘
```

### 相关知识点

- 2.14 序列化 - JSON 序列化
- 2.5 异步编程 - async/await 在网络请求中的应用

---

## B.4 数据库基础

### 概念讲解

.NET 后端开发中，主要使用以下技术访问数据库：

| 技术 | 说明 |
|------|------|
| ADO.NET | 原始数据库访问 |
| Entity Framework | ORM 框架 |
| Dapper | 轻量级 ORM |
| Dapper + Repository | 常用架构模式 |

### 举例说明

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DatabaseDemo
{
    // ===== 1. ADO.NET 基础操作 =====
    
    class AdoNetDemo
    {
        private string _connectionString = "Server=localhost;Database=MyDB;Trusted_Connection=True";
        
        // 查询
        List<User> GetUsers()
        {
            var users = new List<User>();
            
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            var command = new SqlCommand("SELECT * FROM Users", connection);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2)
                });
            }
            
            return users;
        }
        
        // 带参数的查询
        User GetUserById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            var command = new SqlCommand("SELECT * FROM Users WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2)
                };
            }
            
            return null;
        }
        
        // 插入
        int InsertUser(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            var sql = @"INSERT INTO Users (Name, Email) 
                       VALUES (@Name, @Email);
                       SELECT SCOPE_IDENTITY();";
            
            var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", user.Name);
            command.Parameters.AddWithValue("@Email", user.Email);
            
            var id = command.ExecuteScalar();
            return Convert.ToInt32(id);
        }
        
        // 更新
        void UpdateUser(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            var sql = "UPDATE Users SET Name = @Name, Email = @Email WHERE Id = @Id";
            
            var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@Name", user.Name);
            command.Parameters.AddWithValue("@Email", user.Email);
            
            command.ExecuteNonQuery();
        }
        
        // 删除
        void DeleteUser(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            var sql = "DELETE FROM Users WHERE Id = @Id";
            
            var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            
            command.ExecuteNonQuery();
        }
    }
    
    // ===== 2. Entity Framework Core =====
    
    // 定义实体
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // 导航属性
        public List<Order> Orders { get; set; }
    }
    
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // 导航属性
        public User User { get; set; }
    }
    
    // 定义 DbContext
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer("Server=localhost;Database=MyDB;Trusted_Connection=True");
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
            });
            
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(e => e.UserId);
            });
        }
    }
    
    // 使用 EF Core
    class EfCoreDemo
    {
        // 查询
        List<User> GetAllUsers()
        {
            using var context = new AppDbContext();
            return context.Users.ToList();
        }
        
        // 条件查询
        List<User> GetActiveUsers()
        {
            using var context = new AppDbContext();
            return context.Users
                .Where(u => u.CreatedAt > DateTime.Now.AddMonths(-1))
                .ToList();
        }
        
        // 关联查询
        List<Order> GetUserOrders(int userId)
        {
            using var context = new AppDbContext();
            return context.Orders
                .Include(o => o.User)
                .Where(o => o.UserId == userId)
                .ToList();
        }
        
        // 插入
        int CreateUser(User user)
        {
            using var context = new AppDbContext();
            context.Users.Add(user);
            context.SaveChanges();
            return user.Id;
        }
        
        // 更新
        void UpdateUser(User user)
        {
            using var context = new AppDbContext();
            context.Users.Update(user);
            context.SaveChanges();
        }
        
        // 删除
        void DeleteUser(int id)
        {
            using var context = new AppDbContext();
            var user = context.Users.Find(id);
            if (user != null)
            {
                context.Users.Remove(user);
                context.SaveChanges();
            }
        }
        
        // 事务
        void TransferMoney(int fromUserId, int toUserId, decimal amount)
        {
            using var context = new AppDbContext();
            using var transaction = context.Database.BeginTransaction();
            
            try
            {
                var fromUser = context.Users.Find(fromUserId);
                var toUser = context.Users.Find(toUserId);
                
                fromUser.Email = $"balance_{amount}";  // 扣款
                toUser.Email = $"receive_{amount}";   // 加款
                
                context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
    
    // ===== 3. Dapper 轻量级 ORM =====
    
    class DapperDemo
    {
        private string _connectionString = "Server=localhost;Database=MyDB;Trusted_Connection=True";
        
        // 查询
        List<User> GetUsers()
        {
            using var connection = new SqlConnection(_connectionString);
            return connection.Query<User>("SELECT * FROM Users").ToList();
        }
        
        // 参数化查询
        User GetUserById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            return connection.QueryFirstOrDefault<User>(
                "SELECT * FROM Users WHERE Id = @Id", 
                new { Id = id });
        }
        
        // 插入并获取ID
        int InsertUser(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"INSERT INTO Users (Name, Email) 
                       VALUES (@Name, @Email);
                       SELECT CAST(SCOPE_IDENTITY() as int);";
            
            return connection.ExecuteScalar<int>(sql, user);
        }
        
        // 执行多条语句
        void InsertBatch(List<User> users)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            using var transaction = connection.BeginTransaction();
            
            foreach (var user in users)
            {
                connection.Execute(
                    "INSERT INTO Users (Name, Email) VALUES (@Name, @Email)",
                    user, transaction);
            }
            
            transaction.Commit();
        }
    }
}
```

### 相关知识点

- B.1 异常处理 - 数据库操作异常处理
- 2.6 反射 - EF Core 依赖反射

---

## B.5 日志记录

### 举例说明

```csharp
using System;
using System.IO;

namespace LoggingDemo
{
    // ===== 1. 简单日志实现 =====
    
    class SimpleLogger
    {
        private readonly string _logFile;
        
        public SimpleLogger(string logFile)
        {
            _logFile = logFile;
        }
        
        public void Log(string level, string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            
            // 写入文件
            File.AppendAllText(_logFile, logEntry + Environment.NewLine);
            
            // 同时输出到控制台
            Console.WriteLine(logEntry);
        }
        
        public void Info(string message) => Log("INFO", message);
        public void Warning(string message) => Log("WARN", message);
        public void Error(string message) => Log("ERROR", message);
        
        public void Error(string message, Exception ex)
        {
            Log("ERROR", $"{message}\n{ex}");
        }
    }
    
    // ===== 2. 通用日志接口 =====
    
    interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception ex = null);
    }
    
    // ===== 3. 文件日志实现 =====
    
    class FileLogger : ILogger
    {
        private readonly string _logDirectory;
        
        public FileLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(logDirectory);
        }
        
        public void LogInfo(string message) => WriteLog("INFO", message);
        
        public void LogWarning(string message) => WriteLog("WARN", message);
        
        public void LogError(string message, Exception ex = null)
        {
            var msg = ex != null ? $"{message}\n{ex}" : message;
            WriteLog("ERROR", msg);
        }
        
        private void WriteLog(string level, string message)
        {
            var fileName = Path.Combine(_logDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
            var entry = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
            
            lock (this)  // 线程安全
            {
                File.AppendAllText(fileName, entry + Environment.NewLine);
            }
        }
    }
    
    // ===== 4. 日志级别过滤 =====
    
    class LevelFilteredLogger : ILogger
    {
        private readonly ILogger _logger;
        private readonly LogLevel _minLevel;
        
        [Flags]
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }
        
        public LevelFilteredLogger(ILogger logger, LogLevel minLevel = LogLevel.Info)
        {
            _logger = logger;
            _minLevel = minLevel;
        }
        
        public void LogInfo(string message)
        {
            if (LogLevel.Info >= _minLevel) _logger.LogInfo(message);
        }
        
        public void LogWarning(string message)
        {
            if (LogLevel.Warning >= _minLevel) _logger.LogWarning(message);
        }
        
        public void LogError(string message, Exception ex = null)
        {
            if (LogLevel.Error >= _minLevel) _logger.LogError(message, ex);
        }
    }
    
    // ===== 5. 结构化日志 =====
    
    class StructuredLogger
    {
        public void LogWithContext(string message, object context)
        {
            var props = new System.Text.StringBuilder();
            
            foreach (var prop in context.GetType().GetProperties())
            {
                props.Append($" {prop.Name}={prop.GetValue(context)};");
            }
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}{props}");
        }
    }
    
    // 使用示例
    // var user = new { Id = 1, Name = "张三" };
    // new StructuredLogger().LogWithContext("用户登录", user);
    // 输出: [10:30:45] 用户登录 Id=1; Name=张三;
}
```

### 相关知识点

- B.1 异常处理 - 日志记录异常
- 2.6 反射 - 结构化日志使用反射获取属性

---

## B.6 配置文件

### 举例说明

```csharp
using System;
using System.Configuration;
using System.IO;
using System.Text.Json;

namespace ConfigDemo
{
    // ===== 1. appsettings.json（推荐） =====
    
    /*
    {
        "AppSettings": {
            "SiteName": "My Website",
            "MaxUsers": 1000,
            "EnableCache": true
        },
        "Database": {
            "ConnectionString": "Server=localhost;Database=MyDB",
            "Timeout": 30
        },
        "Logging": {
            "Level": "Info",
            "FilePath": "logs/app.log"
        }
    }
    */
    
    // 读取配置类
    class AppSettings
    {
        public AppSettingsSection AppSettings { get; set; }
        public DatabaseSettings Database { get; set; }
        public LoggingSettings Logging { get; set; }
    }
    
    class AppSettingsSection
    {
        public string SiteName { get; set; }
        public int MaxUsers { get; set; }
        public bool EnableCache { get; set; }
    }
    
    class DatabaseSettings
    {
        public string ConnectionString { get; set; }
        public int Timeout { get; set; }
    }
    
    class LoggingSettings
    {
        public string Level { get; set; }
        public string FilePath { get; set; }
    }
    
    // 读取 JSON 配置
    class JsonConfigLoader
    {
        public AppSettings Load(string path = "appsettings.json")
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json);
        }
        
        // 带默认值
        public AppSettings LoadWithDefaults(string path = "appsettings.json")
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            if (!File.Exists(path))
            {
                return GetDefaults();
            }
            
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, options) ?? GetDefaults();
        }
        
        private AppSettings GetDefaults()
        {
            return new AppSettings
            {
                AppSettings = new AppSettingsSection
                {
                    SiteName = "Default Site",
                    MaxUsers = 100,
                    EnableCache = false
                },
                Database = new DatabaseSettings
                {
                    ConnectionString = "Server=localhost",
                    Timeout = 30
                },
                Logging = new LoggingSettings
                {
                    Level = "Info",
                    FilePath = "logs/default.log"
                }
            };
        }
    }
    
    // ===== 2. 环境变量配置 =====
    
    class EnvironmentConfig
    {
        public string GetConfig(string key, string defaultValue = "")
        {
            return Environment.GetEnvironmentVariable(key) ?? defaultValue;
        }
        
        public T GetConfig<T>(string key, T defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        // 获取连接字符串
        public string GetConnectionString(string name)
        {
            // 优先级：appsettings.json > 环境变量 > 配置文件
            var envConnectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{name}");
            
            if (!string.IsNullOrEmpty(envConnectionString))
                return envConnectionString;
            
            // 读取默认配置...
            return "";
        }
    }
    
    // ===== 3. INI 文件读取（传统方式） =====
    
    class IniFileReader
    {
        public string GetValue(string filePath, string section, string key)
        {
            var lines = File.ReadAllLines(filePath);
            
            string currentSection = "";
            
            foreach (var line in lines)
            {
                line = line.Trim();
                
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2);
                    continue;
                }
                
                if (currentSection == section && line.Contains("="))
                {
                    var parts = line.Split('=');
                    if (parts[0].Trim() == key)
                        return parts[1].Trim();
                }
            }
            
            return null;
        }
    }
}
```

### 相关知识点

- B.1 异常处理 - 配置文件解析异常处理
- 2.14 序列化 - JSON 序列化

---

> 附录已补充完成！本文档现在包含：
> - 基础篇（1.1-1.12）
> - 进阶篇（2.1-2.18）
> - Unity开发专题（A.1-A.5）
> - .NET后端开发专题（B.1-B.6）