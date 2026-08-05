# C# LINQ 完全指南

> Author: AI Assistant
> Date: 2026-05-04

---

## 第一章 LINQ 简介

### 1.1 什么是LINQ

LINQ（Language Integrated Query，语言集成查询）是C# 3.0引入的一项强大功能，它提供了一种统一的语法来查询各种数据源。LINQ使得开发者能够使用类似SQL的声明式语法来操作数组、集合、XML、数据库、Entity Framework等数据源。

**核心优势：**
- 统一的查询语法
- 编译时类型检查
- 智能提示支持
- 可读性强

### 1.2 LINQ的优势与应用场景

**优势：**
1. **简洁优雅**：相比传统循环，LINQ代码更简洁
2. **类型安全**：编译时即可发现错误
3. **可组合**：易于链式调用构建复杂查询
4. **可扩展**：可自定义LINQProvider支持更多数据源

**应用场景：**
- 内存集合查询（IEnumerable）
- 数据库查询（通过Entity Framework）
- XML文档操作
- JSON数据处理

### 1.3 LINQ查询表达式语法与方法语法

LINQ支持两种语法形式：

```csharp
// 数据源
List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// 方法语法（Method Syntax）- 也称为流式语法
var result1 = numbers.Where(n => n > 5)       // 筛选出大于5的元素
                     .Select(n => n * 2);      // 将每个元素乘以2（投影）

// 查询表达式语法（Query Syntax）
var result2 = from n in numbers   // from：声明数据源和范围变量
              where n > 5         // where：筛选条件（仅保留大于5的元素）
              select n * 2;       // select：投影结果（每个元素乘以2）

// 两者结果相同：{ 12, 14, 16, 18, 20 }
```

**语法对比：**

| 特性 | 查询语法 | 方法语法 |
|------|----------|----------|
| 可读性 | 更像SQL，易读 | 更函数式，灵活 |
| 调试 | 较难 | 可逐步调试 |
| 功能 | 基本涵盖 | 更完整（全部操作） |
| 推荐 | 简单查询 | 复杂查询/链式调用 |

> **注意**：查询表达式最终会编译成方法语法，因此两者性能相同。

---

## 第二章 基础查询操作

### 2.1 筛选（Where）

`Where`用于根据条件过滤元素，返回满足条件的元素集合。

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// 获取所有偶数
var evens = numbers.Where(n => n % 2 == 0);  // 筛选出满足条件的元素
// 结果: { 2, 4, 6, 8, 10 }

// 获取大于5的元素
var greaterThan5 = numbers.Where(n => n > 5);  // 筛选出所有大于5的数
// 结果: { 6, 7, 8, 9, 10 }

// 组合条件：大于3且小于8
var combined = numbers.Where(n => n > 3 && n < 8);  // 使用 && 组合多个条件
// 结果: { 4, 5, 6, 7 }
```

**索引筛选：**
```csharp
// 带索引的筛选， predicate第二个参数是索引
var withIndex = numbers.Where((n, index) => index % 2 == 0);  // Lambda 第二个参数接收索引
// 结果: { 1, 3, 5, 7, 9 }
```

### 2.2 投影（Select / SelectMany）

`Select`用于将每个元素转换为另一种形式，`SelectMany`用于展平嵌套集合。

```csharp
// Select - 元素转换
List<string> names = new List<string> { "Alice", "Bob", "Charlie" };

var upperNames = names.Select(s => s.ToUpper());  // 将每个元素映射为大写
// 结果: { "ALICE", "BOB", "CHARLIE" }

var nameLengths = names.Select(s => s.Length);  // 将每个元素映射为其长度
// 结果: { 5, 3, 7 }

// 匿名类型投影
var anonymous = names.Select(s => new { Name = s, Length = s.Length });  // 投影为匿名类型
/* 结果:
   { Name = "Alice", Length = 5 }
   { Name = "Bob", Length = 3 }
   { Name = "Charlie", Length = 7 }
*/
```

```csharp
// SelectMany - 展平嵌套集合
List<List<string>> categories = new List<List<string>>
{
    new List<string> { "Apple", "Banana" },
    new List<string> { "Orange", "Grape" },
    new List<string> { "Mango" }
};

var flat = categories.SelectMany(c => c);  // 将嵌套集合展平为一个序列
// 结果: { "Apple", "Banana", "Orange", "Grape", "Mango" }

// 带索引的SelectMany
var indexed = categories.SelectMany((c, i) => c.Select(item => $"{i}: {item}"));  // Lambda 第二个参数接收索引
/* 结果:
   { "0: Apple", "0: Banana", "1: Orange", "1: Grape", "2: Mango" }
*/
```

### 2.3 排序（OrderBy / OrderByDescending / ThenBy）

```csharp
List<int> numbers = new List<int> { 5, 2, 8, 1, 9, 3 };

// 升序排序
var asc = numbers.OrderBy(n => n);  // 按指定键升序排序，返回 IOrderedEnumerable
// 结果: { 1, 2, 3, 5, 8, 9 }

// 降序排序
var desc = numbers.OrderByDescending(n => n);  // 按指定键降序排序
// 结果: { 9, 8, 5, 3, 2, 1 }

// 多级排序
List<Person> people = new List<Person>
{
    new Person { Name = "Alice", Age = 25 },
    new Person { Name = "Bob", Age = 30 },
    new Person { Name = "Charlie", Age = 25 }
};

// 先按年龄升序，再按姓名升序
var sorted = people.OrderBy(p => p.Age).ThenBy(p => p.Name);  // ThenBy：在第一级排序基础上再排序
// 结果: Alice(25), Charlie(25), Bob(30)

// 降序thenBy
var descThen = people.OrderByDescending(p => p.Age).ThenByDescending(p => p.Name);  // ThenByDescending：次级降序
// 结果: Bob(30), Charlie(25), Alice(25)

class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

### 2.4 分组（GroupBy）

`GroupBy`将元素按指定键进行分组。

```csharp
List<Product> products = new List<Product>
{
    new Product { Name = "Apple", Category = "Fruit", Price = 3 },
    new Product { Name = "Banana", Category = "Fruit", Price = 2 },
    new Product { Name = "Carrot", Category = "Vegetable", Price = 1 },
    new Product { Name = "Broccoli", Category = "Vegetable", Price = 2 }
};

// 按Category分组
var grouped = products.GroupBy(p => p.Category);  // 按Category字段分组，返回 IGrouping 集合

/* 结果:
   Fruit:     Apple(3), Banana(2)
   Vegetable: Carrot(1), Broccoli(2)
*/

// 遍历分组
foreach (var group in grouped)          // 遍历每个分组
{
    Console.WriteLine($"Category: {group.Key}");  // group.Key 是分组的键值（此处为类别名）
    foreach (var item in group)         // 遍历分组内的每个元素
        Console.WriteLine($"  - {item.Name}: {item.Price}");
}

// 分组后统计
var categoryCount = products.GroupBy(p => p.Category)  // 按Category分组
    .Select(g => new { Category = g.Key, Count = g.Count() });  // 投影为每个分组及其元素数量
/* 结果:
   { Category = "Fruit", Count = 2 }
   { Category = "Vegetable", Count = 2 }
*/

class Product
{
    public string Name { get; set; }
    public string Category { get; set; }
    public decimal Price { get; set; }
}
```

### 2.5 联接（Join / GroupJoin）

```csharp
List<Customer> customers = new List<Customer>
{
    new Customer { Id = 1, Name = "Alice" },
    new Customer { Id = 2, Name = "Bob" },
    new Customer { Id = 3, Name = "Charlie" }
};

List<Order> orders = new List<Order>
{
    new Order { CustomerId = 1, Product = "Book", Amount = 25 },
    new Order { CustomerId = 1, Product = "Pen", Amount = 5 },
    new Order { CustomerId = 2, Product = "Laptop", Amount = 1000 }
};

// Inner Join - 内连接
var innerJoin = customers.Join(orders,  // 将客户与订单进行内连接
    c => c.Id,  // 从客户集合提取键
    o => o.CustomerId,  // 从订单集合提取键
    (c, o) => new { CustomerName = c.Name, Product = o.Product, Amount = o.Amount });  // 投影连接结果
/* 结果:
   { CustomerName = "Alice", Product = "Book", Amount = 25 }
   { CustomerName = "Alice", Product = "Pen", Amount = 5 }
   { CustomerName = "Bob", Product = "Laptop", Amount = 1000 }
*/

// 查询语法内连接
var querySyntax = from c in customers
                  join o in orders on c.Id equals o.CustomerId
                  select new { c.Name, o.Product };

// GroupJoin - 分组连接（类似SQL LEFT JOIN）
var groupJoin = customers.GroupJoin(orders,  // 将客户与订单进行分组连接（左外连接）
    c => c.Id,  // 从客户集合提取键
    o => o.CustomerId,  // 从订单集合提取键
    (c, orderGroup) => new  // 投影每个客户及其关联订单组
    {
        CustomerName = c.Name,
        Orders = orderGroup
    });
/* 结果:
   Alice: Book, Pen
   Bob: Laptop
   Charlie: (空)
*/

class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
}

class Order
{
    public int CustomerId { get; set; }
    public string Product { get; set; }
    public decimal Amount { get; set; }
}
```

---

### 第二章 习题

#### 习题 2.1 - 筛选操作

给定以下学生列表：

```csharp
List<Student> students = new List<Student>
{
    new Student { Name = "张三", Score = 85, Grade = "A" },
    new Student { Name = "李四", Score = 72, Grade = "B" },
    new Student { Name = "王五", Score = 90, Grade = "A" },
    new Student { Name = "赵六", Score = 68, Grade = "C" },
    new Student { Name = "钱七", Score = 78, Grade = "B" }
};
```

**题目：**
1. 筛选出成绩大于80分的学生
2. 筛选出等级为A的学生
3. 筛选出成绩在70-85之间的学生

#### 习题 2.2 - 投影操作

使用上面的学生列表：
1. 获取所有学生的姓名（使用Select）
2. 获取所有学生的姓名和成绩，创建一个匿名类型
3. 使用SelectMany将每个学生的多个爱好展开为单独的元素（假设每个学生有List<string>类型的Hobbies属性）

#### 习题 2.3 - 排序操作

使用上面的学生列表：
1. 按成绩升序排序
2. 按等级降序排序，再按成绩升序排序

#### 习题 2.4 - 分组操作

```csharp
List<Product> products = new List<Product>
{
    new Product { Name = "iPhone", Category = "Electronics", Price = 8000 },
    new Product { Name = "MacBook", Category = "Electronics", Price = 12000 },
    new Product { Name = "T恤", Category = "Clothing", Price = 200 },
    new Product { Name = "裤子", Category = "Clothing", Price = 300 },
    new Product { Name = "苹果", Category = "Food", Price = 10 },
    new Product { Name = "香蕉", Category = "Food", Price = 5 }
};
```

1. 按Category分组，计算每个类别的产品数量
2. 按Category分组，计算每个类别的平均价格
3. 找出价格最高的每个类别产品

#### 习题 2.5 - 联接操作

有三个集合：学生、课程、选课

```csharp
List<Student> students = new List<Student>
{
    new Student { Id = 1, Name = "张三" },
    new Student { Id = 2, Name = "李四" },
    new Student { Id = 3, Name = "王五" }
};

List<Course> courses = new List<Course>
{
    new Course { Id = 1, Name = "数学" },
    new Course { Id = 2, Name = "语文" },
    new Course { Id = 3, Name = "英语" }
};

List<Enrollment> enrollments = new List<Enrollment>
{
    new Enrollment { StudentId = 1, CourseId = 1 },
    new Enrollment { StudentId = 1, CourseId = 2 },
    new Enrollment { StudentId = 2, CourseId = 1 },
    new Enrollment { StudentId = 2, CourseId = 3 }
};
```

1. 使用Join查询每个学生选了哪些课程
2. 使用GroupJoin查询每个学生及其选课列表（包括未选课的学生）

---

**习题 2 答案**

#### 答案 2.1

```csharp
// 1. 成绩大于80分
var highScores = students.Where(s => s.Score > 80);  // 筛选出成绩大于80分的学生
// 结果: 张三(85), 王五(90)

// 2. 等级为A
var gradeA = students.Where(s => s.Grade == "A");  // 筛选出等级为A的学生
// 结果: 张三(A), 王五(A)

// 3. 成绩在70-85之间
var between = students.Where(s => s.Score >= 70 && s.Score <= 85);  // 筛选出成绩在70到85之间（含）的学生
// 结果: 李四(72), 张三(85), 钱七(78)
```

#### 答案 2.2

```csharp
// 1. 获取所有学生姓名
var names = students.Select(s => s.Name);  // 将每个学生投影为其姓名
// 结果: "张三", "李四", "王五", "赵六", "钱七"

// 2. 匿名类型投影
var nameAndScore = students.Select(s => new { s.Name, s.Score });  // 投影为包含姓名和成绩的匿名类型
/* 结果:
   { Name = "张三", Score = 85 }
   { Name = "李四", Score = 72 }
   ...
*/

// 3. 假设添加Hobbies属性后
// 先给每个学生添加爱好
students[0].Hobbies = new List<string> { "篮球", "游泳" };
students[1].Hobbies = new List<string> { "足球" };
students[2].Hobbies = new List<string> { "篮球", "跑步", "游泳" };
students[3].Hobbies = new List<string> { "乒乓球" };
students[4].Hobbies = new List<string> { "足球", "游泳" };

// SelectMany展开
var allHobbies = students.SelectMany(s => s.Hobbies);  // 将所有学生的爱好列表展平为一个序列
// 结果: "篮球", "游泳", "足球", "篮球", "跑步", "游泳", "乒乓球", "足球", "游泳"
```

#### 答案 2.3

```csharp
// 1. 按成绩升序
var byScoreAsc = students.OrderBy(s => s.Score);  // 按成绩升序排序
// 结果: 赵六(68), 李四(72), 钱七(78), 张三(85), 王五(90)

// 2. 按等级降序，再按成绩升序
var byGradeDesc = students.OrderByDescending(s => s.Grade)  // 先按等级降序（C > B > A）
                          .ThenBy(s => s.Score);             // 再按成绩升序
// C级: 赵六 -> B级: 李四,钱七 -> A级: 张三,王五
```

#### 答案 2.4

```csharp
// 1. 每个类别的产品数量
var categoryCount = products.GroupBy(p => p.Category)  // 按Category字段分组
    .Select(g => new { Category = g.Key, Count = g.Count() });  // 从每个分组投影出 Key（类别名）和元素数量
/* 结果:
   Electronics: 2
   Clothing: 2
   Food: 2
*/

// 2. 每个类别的平均价格
var avgPrice = products.GroupBy(p => p.Category)  // 按Category字段分组
    .Select(g => new { Category = g.Key, AvgPrice = g.Average(p => p.Price) });  // 从每个分组投影出 Key 和平均价格
/* 结果:
   Electronics: 10000
   Clothing: 250
   Food: 7.5
*/

// 3. 每个类别的最高价产品
var maxPriceProducts = products.GroupBy(p => p.Category)  // 按Category字段分组
    .Select(g => g.OrderByDescending(p => p.Price)  // 在每个分组内按 Price 降序排序
                  .First());                         // 取排序后的第一个元素（即最高价产品）
/* 结果:
   iPhone, MacBook, 裤子, 苹果
*/
```

#### 答案 2.5

```csharp
// 1. 每个学生选了哪些课程（内连接）
var studentCourses = students.Join(enrollments,  // 将学生与选课表进行内连接
    s => s.Id,                      // 从学生集合提取键（学生的Id）
    e => e.StudentId,               // 从选课表提取键（选课记录中的StudentId）
    (s, e) => new { StudentName = s.Name, CourseId = e.CourseId })  // 将匹配的结果投影为中间类型
    .Join(courses,                  // 再将中间结果与课程表进行内连接
        temp => temp.CourseId,      // 从中间结果提取课程ID
        c => c.Id,                  // 从课程表提取键（课程的Id）
        (temp, c) => new { StudentName = temp.StudentName, CourseName = c.Name });  // 投影最终结果
/* 结果:
   张三 - 数学
   张三 - 语文
   李四 - 数学
   李四 - 英语
*/

// 查询语法版本
var queryResult = from s in students                          // from：声明学生数据源
                  join e in enrollments on s.Id equals e.StudentId  // 第一次连接：学生表与选课表通过 Id 关联
                  join c in courses on e.CourseId equals c.Id      // 第二次连接：选课表与课程表通过 CourseId 关联
                  select new { s.Name, c.Name };              // select：投影学生名和课程名

// 2. 每个学生及其选课列表（分组连接，类似左外连接）
var studentWithCourses = students.GroupJoin(enrollments,  // 将学生与选课表进行分组连接
    s => s.Id,                          // 从学生集合提取键
    e => e.StudentId,                   // 从选课表提取键
    (s, enrollments) => new             // 对于每个学生，enrollments 是对应的选课记录集合（可能为空）
    {
        StudentName = s.Name,
        Courses = enrollments.Join(courses,          // 在选课组内连接课程表获取课程名称
            e => e.CourseId,            // 从选课记录提取课程ID
            c => c.Id,                  // 从课程集合提取键
            (e, c) => c.Name).ToList()  // 投影为课程名称并转为 List
    });
/* 结果:
   张三: 数学, 语文
   李四: 数学, 英语
   王五: (空)
*/
```

---

## 第三章 聚合操作

### 3.1 Count / Sum / Average / Min / Max

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// Count - 计数
int count = numbers.Count();                    // 计算元素总数
int evenCount = numbers.Count(n => n % 2 == 0);  // 统计偶数的个数

// Sum - 求和
int sum = numbers.Sum();                         // 计算所有元素的总和
int evenSum = numbers.Where(n => n % 2 == 0).Sum();  // 先筛选偶数，再求和

// Average - 平均值
double avg = numbers.Average();                  // 计算所有元素的平均值
double evenAvg = numbers.Where(n => n % 2 == 0).Average(); // 先筛选偶数，再计算平均值

// Min / Max - 最小/最大值
int min = numbers.Min();                        // 找出最小值
int max = numbers.Max();                        // 找出最大值
```

**复杂对象聚合：**

```csharp
List<Product> products = new List<Product>
{
    new Product { Name = "Apple", Price = 3 },
    new Product { Name = "Banana", Price = 2 },
    new Product { Name = "Orange", Price = 4 }
};

int totalPrice = products.Sum(p => p.Price);              // 计算所有产品的总价
decimal avgPrice = products.Average(p => p.Price);        // 计算所有产品的平均价格
Product cheapest = products.MinBy(p => p.Price);          // 找出价格最低的产品
Product mostExpensive = products.MaxBy(p => p.Price);     // 找出价格最高的产品
int productCount = products.Count();                      // 统计产品总数
```

> **注意**：MinBy/MaxBy是.NET 6+的方法，旧版本可使用`OrderBy().First()`

### 3.2 Aggregate（聚合函数自定义）

`Aggregate`允许自定义聚合逻辑，类似于SQL的聚合函数。

```csharp
// 基本Aggregate - 累加
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

int product = numbers.Aggregate((acc, n) => acc * n);  // 累乘所有元素（无种子值）
// 结果: 120 (1*2*3*4*5)

// 带初始值的Aggregate
int sumWithSeed = numbers.Aggregate(10, (acc, n) => acc + n);  // 以10为种子值，累加所有元素
// 结果: 55 (10 + 1+2+3+4+5)

// 选择结果转换
string concatenated = numbers.Aggregate("0", (acc, n) => acc + "-" + n);  // 以"0"为种子拼接字符串
// 结果: "0-1-2-3-4-5"

// 复杂场景：自定义聚合
List<string> words = new List<string> { "Hello", "World", "LINQ" };
string sentence = words.Aggregate((s1, s2) => s1 + " " + s2);  // 用空格拼接所有字符串
// 结果: "Hello World LINQ"

// 分组后使用Aggregate
List<Student> students = new List<Student>
{
    new Student { Name = "张三", Score = 85 },
    new Student { Name = "李四", Score = 72 },
    new Student { Name = "王五", Score = 90 }
};

// 计算总分
int totalScore = students.Aggregate(0, (sum, s) => sum + s.Score); // 以0为种子，累加每个学生的成绩

// 字符串拼接所有学生姓名
string allNames = students.Aggregate("", (names, s) => names + s.Name + ",");  // 以空串为种子，拼接所有学生姓名并用逗号分隔
// 结果: "张三,李四,王五,"
```

### 3.3 GroupBy + 聚合函数

```csharp
List<Product> products = new List<Product>
{
    new Product { Name = "Apple", Category = "Fruit", Price = 3 },
    new Product { Name = "Banana", Category = "Fruit", Price = 2 },
    new Product { Name = "Carrot", Category = "Vegetable", Price = 1 },
    new Product { Name = "Broccoli", Category = "Vegetable", Price = 2 },
    new Product { Name = "Grape", Category = "Fruit", Price = 5 }
};

// 按类别统计
var categoryStats = products.GroupBy(p => p.Category)  // 按Category分组
    .Select(g => new  // 投影每个分组的统计信息
    {
        Category = g.Key,
        Count = g.Count(),  // 统计每个分组的产品数量
        TotalPrice = g.Sum(p => p.Price),  // 计算每个分组的总价
        AvgPrice = g.Average(p => p.Price),  // 计算每个分组的平均价格
        MaxPrice = g.Max(p => p.Price),  // 找出每个分组的最高价格
        MinPrice = g.Min(p => p.Price)  // 找出每个分组的最低价格
    });

/* 结果:
   Fruit:     Count=3, Total=10, Avg=3.33, Max=5, Min=2
   Vegetable: Count=2, Total=3,  Avg=1.5,  Max=2, Min=1
*/

// 过滤分组结果
var largeCategories = products.GroupBy(p => p.Category)  // 按Category分组
    .Where(g => g.Count() >= 3)  // 筛选出元素数量>=3的分组
    .Select(g => g.Key);  // 投影出分组键（类别名称）
// 结果: "Fruit"
```

---

### 第三章 习题

#### 习题 3.1 - 基本聚合操作

```csharp
List<int> numbers = new List<int> { 12, 45, 7, 23, 56, 89, 3, 67 };
```

1. 计算所有偶数的和
2. 计算所有奇数的平均值
3. 找出最大值和最小值
4. 统计大于30的元素个数

#### 习题 3.2 - 对象集合聚合

```csharp
List<Order> orders = new List<Order>
{
    new Order { Id = 1, Customer = "张三", Amount = 150, Date = new DateTime(2024,1,1) },
    new Order { Id = 2, Customer = "张三", Amount = 200, Date = new DateTime(2024,1,5) },
    new Order { Id = 3, Customer = "李四", Amount = 80,  Date = new DateTime(2024,1,3) },
    new Order { Id = 4, Customer = "李四", Amount = 320, Date = new DateTime(2024,1,10) },
    new Order { Id = 5, Customer = "王五", Amount = 450, Date = new DateTime(2024,1,8) }
};
```

1. 计算所有订单的总金额
2. 计算每个客户的订单总金额
3. 找出金额最高的订单
4. 计算每个客户的平均订单金额

#### 习题 3.3 - Aggregate自定义聚合

1. 使用Aggregate计算列表 `[5, 10, 15, 20]` 的阶乘
2. 使用Aggregate找出列表中的最大值（不使用Max方法）
3. 使用Aggregate将字符串列表 `["Hello", "World", "LINQ"]` 合并成 "Hello-World-LINQ"

#### 习题 3.4 - GroupBy综合

```csharp
List<Employee> employees = new List<Employee>
{
    new Employee { Name = "张三", Department = "研发", Salary = 8000 },
    new Employee { Name = "李四", Department = "研发", Salary = 7000 },
    new Employee { Name = "王五", Department = "销售", Salary = 6000 },
    new Employee { Name = "赵六", Department = "销售", Salary = 5500 },
    new Employee { Name = "钱七", Department = "人事", Salary = 5000 }
};
```

1. 按部门分组，计算每个部门的平均工资
2. 找出平均工资最高的部门
3. 按部门分组，找出每个部门工资最高的员工

---

**习题 3 答案**

#### 答案 3.1

```csharp
// 1. 所有偶数的和
int evenSum = numbers.Where(n => n % 2 == 0).Sum(); // 12+56+2 = 148 (注: 列表中偶数是12,56)

// 2. 所有奇数的平均值
double oddAvg = numbers.Where(n => n % 2 != 0).Average(); // (45+7+23+89+3+67)/6 = 39

// 3. 最大值和最小值
int max = numbers.Max();  // 89
int min = numbers.Min();  // 3

// 4. 大于30的元素个数
int countGT30 = numbers.Count(n => n > 30); // 4 (45,56,89,67)
```

#### 答案 3.2

```csharp
// 1. 所有订单总金额
decimal totalAmount = orders.Sum(o => o.Amount); // 1200

// 2. 每个客户的订单总金额
var customerTotal = orders.GroupBy(o => o.Customer)  // 按客户名称分组
    .Select(g => new { Customer = g.Key, Total = g.Sum(o => o.Amount) });  // 对每组求和计算总金额

// 3. 金额最高的订单
var maxOrder = orders.OrderByDescending(o => o.Amount)  // 按金额降序排序
                     .First();                            // 取第一个（即金额最高的订单）
// 或使用: orders.MaxBy(o => o.Amount)  // .NET 6+ 更简洁的写法
// 结果: Id=5, 王五, 450

// 4. 每个客户的平均订单金额
var customerAvg = orders.GroupBy(o => o.Customer)  // 按客户名称分组
    .Select(g => new { Customer = g.Key, Avg = g.Average(o => o.Amount) });  // 对每组计算平均金额
```

#### 答案 3.3

```csharp
// 1. 计算阶乘 5! = 5 * 4 * 3 * 2 * 1 = 120
List<int> factorialNumbers = new List<int> { 5, 4, 3, 2, 1 };
int factorial = factorialNumbers.Aggregate((acc, n) => acc * n); // 120

// 另一种更直观的理解: 5! = 1 * 2 * 3 * 4 * 5
int factorial2 = Enumerable.Range(1, 5).Aggregate((acc, n) => acc * n); // 120

// 2. 找出最大值（不使用Max）
int[] nums = { 5, 10, 15, 20, 8 };
int maxValue = nums.Aggregate((max, n) => n > max ? n : max); // 20

// 3. 字符串合并
List<string> words = new List<string> { "Hello", "World", "LINQ" };
string result = words.Aggregate((s1, s2) => s1 + "-" + s2);
// 结果: "Hello-World-LINQ"
```

#### 答案 3.4

```csharp
// 1. 每个部门的平均工资
var deptAvg = employees.GroupBy(e => e.Department)       // 按部门分组
    .Select(g => new { Department = g.Key, AvgSalary = g.Average(e => e.Salary) });  // 计算每个部门的平均工资

// 2. 平均工资最高的部门
var highestAvgDept = employees.GroupBy(e => e.Department)          // 按部门分组
    .OrderByDescending(g => g.Average(e => e.Salary))  // 按部门的平均工资降序排列
    .Select(g => g.Key)                                 // 只保留部门名称
    .First();                                           // 取第一个（即平均工资最高的部门）

// 3. 每个部门工资最高的员工
var topInDept = employees.GroupBy(e => e.Department)               // 按部门分组
    .Select(g => g.OrderByDescending(e => e.Salary)  // 在每个分组内按工资降序排序
                  .First());                          // 取第一个（即工资最高的员工）
```

---

## 第四章 集合操作

### 4.1 集合过滤与去重（Distinct）

```csharp
List<int> numbers = new List<int> { 1, 2, 2, 3, 3, 3, 4, 5, 5 };

// Distinct - 去重
var distinct = numbers.Distinct();
// 结果: { 1, 2, 3, 4, 5 }

// 复杂对象去重（基于特定属性）
List<Product> products = new List<Product>
{
    new Product { Name = "Apple", Category = "Fruit" },
    new Product { Name = "Apple", Category = "Fruit" },  // 重复
    new Product { Name = "Banana", Category = "Fruit" }
};

// 基于Name去重
var distinctProducts = products.DistinctBy(p => p.Name);
// 结果: Apple, Banana

// 带自定义比较器的去重
var distinctWithComparer = products.Distinct(new ProductComparer());
```

### 4.2 集合运算（Union / Intersect / Except）

```csharp
List<int> list1 = new List<int> { 1, 2, 3, 4, 5 };
List<int> list2 = new List<int> { 4, 5, 6, 7, 8 };

// Union - 并集（去重）
var union = list1.Union(list2);
// 结果: { 1, 2, 3, 4, 5, 6, 7, 8 }

// Intersect - 交集
var intersect = list1.Intersect(list2);
// 结果: { 4, 5 }

// Except - 差集（list1有但list2没有的）
var except = list1.Except(list2);
// 结果: { 1, 2, 3 }

// 复杂对象的集合运算
List<Product> products1 = new List<Product>
{
    new Product { Name = "Apple" },
    new Product { Name = "Banana" }
};
List<Product> products2 = new List<Product>
{
    new Product { Name = "Banana" },
    new Product { Name = "Orange" }
};

// 需要实现IEquatable或使用自定义比较器
var commonProducts = products1.Intersect(products2, new ProductEqualityComparer());
```

### 4.3 分页操作（Skip / Take）

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// Take - 取前N个
var first3 = numbers.Take(3);      // { 1, 2, 3 }
var first5 = numbers.Take(5);      // { 1, 2, 3, 4, 5 }

// Skip - 跳过前N个
var skip3 = numbers.Skip(3);       // { 4, 5, 6, 7, 8, 9, 10 }
var skip5 = numbers.Skip(5);      // { 6, 7, 8, 9, 10 }

// 分页实现：每页3条，取第2页
int pageSize = 3;
int pageNumber = 2;
var paged = numbers.Skip((pageNumber - 1) * pageSize).Take(pageSize);
// 结果: { 4, 5, 6 }

// TakeWhile - 满足条件时继续取
var takeWhile = numbers.TakeWhile(n => n < 5);
// 结果: { 1, 2, 3, 4 }

// SkipWhile - 满足条件时跳过
var skipWhile = numbers.SkipWhile(n => n < 5);
// 结果: { 5, 6, 7, 8, 9, 10 }

// 高级分页
var page = numbers
    .OrderBy(n => n)  // 先排序
    .Skip(5)
    .Take(5);
```

---

### 第四章 习题

#### 习题 4.1 - 去重操作

```csharp
List<int> numbers = new List<int> { 1, 2, 2, 3, 3, 4, 4, 4, 5 };
List<string> words = new List<string> { "apple", "Apple", "APPLE", "banana", "Banana" };
```

1. 对numbers去重
2. 对words去重（忽略大小写）

#### 习题 4.2 - 集合运算

```csharp
List<int> a = new List<int> { 1, 2, 3, 4, 5 };
List<int> b = new List<int> { 3, 4, 5, 6, 7 };
```

1. 求A和B的并集
2. 求A和B的交集
3. 求A-B的差集
4. 求B-A的差集

#### 习题 4.3 - 分页操作

```csharp
List<Student> students = new List<Student>
{
    new Student { Id = 1, Name = "张三", Score = 85 },
    new Student { Id = 2, Name = "李四", Score = 72 },
    new Student { Id = 3, Name = "王五", Score = 90 },
    new Student { Id = 4, Name = "赵六", Score = 68 },
    new Student { Id = 5, Name = "钱七", Score = 78 },
    new Student { Id = 6, Name = "孙八", Score = 92 },
    new Student { Id = 7, Name = "周九", Score = 75 },
    new Student { Id = 8, Name = "吴十", Score = 88 }
};
```

1. 实现分页查询，每页3条，获取第2页数据
2. 获取成绩最高的前3名学生
3. 跳过前5名学生，获取剩下的学生

#### 习题 4.4 - 综合练习

有一个学生列表，需要实现以下查询：
1. 去除成绩重复的学生（保留第一个）
2. 找出同时选修了"数学"和"语文"的学生ID列表
3. 实现分页获取学生列表，每页5条

---

**习题 4 答案**

#### 答案 4.1

```csharp
// 1. 对numbers去重
var distinctNumbers = numbers.Distinct(); // { 1, 2, 3, 4, 5 }

// 2. 对words去重（忽略大小写）
var distinctWords = words.Distinct(StringComparer.OrdinalIgnoreCase);
// 结果: { "apple", "banana" }

// 或者使用DistinctBy
var distinctBy = words.DistinctBy(w => w.ToLower()); // .NET 6+
```

#### 答案 4.2

```csharp
// 1. 并集
var union = a.Union(b); // { 1, 2, 3, 4, 5, 6, 7 }

// 2. 交集
var intersect = a.Intersect(b); // { 3, 4, 5 }

// 3. A-B差集
var exceptAB = a.Except(b); // { 1, 2 }

// 4. B-A差集
var exceptBA = b.Except(a); // { 6, 7 }
```

#### 答案 4.3

```csharp
// 1. 分页查询，每页3条，第2页
int pageSize = 3;
int pageNumber = 2;
var page2 = students
    .OrderBy(s => s.Id)                            // 先按 Id 排序，保证分页顺序稳定
    .Skip((pageNumber - 1) * pageSize)  // 跳过前 (2-1)*3 = 3 条
    .Take(pageSize);                               // 再取 3 条
// 结果: Id=4,5,6 的学生

// 2. 成绩最高的前3名
var top3 = students
    .OrderByDescending(s => s.Score)  // 按成绩降序排序
    .Take(3);                          // 取前3条（即成绩最高的3个）
// 结果: 王五(90), 孙八(92), 吴十(88) -> 按排序是孙八,王五,吴十

// 3. 跳过前5名
var remaining = students.Skip(5);  // 跳过前5个学生，取剩余的所有学生
// 结果: Id=6,7,8 的学生
```

#### 答案 4.4

```csharp
// 1. 去除成绩重复的学生（保留第一个）
// 假设添加Score属性后
List<StudentWithScore> studentsWithScore = new List<StudentWithScore>
{
    new StudentWithScore { Id = 1, Name = "张三", Score = 85 },
    new StudentWithScore { Id = 2, Name = "李四", Score = 85 },
    new StudentWithScore { Id = 3, Name = "王五", Score = 90 },
    new StudentWithScore { Id = 4, Name = "赵六", Score = 90 },
    new StudentWithScore { Id = 5, Name = "钱七", Score = 78 }
};

// 按分数去重，保留第一个
var uniqueByScore = studentsWithScore
    .GroupBy(s => s.Score)    // 按分数分组
    .Select(g => g.First());  // 每组取第一个学生（即每个分数保留一个）
// 结果: 张三(85), 王五(90), 钱七(78)

// 2. 同时选修了"数学"和"语文"的学生
List<StudentCourse> studentCourses = new List<StudentCourse>
{
    new StudentCourse { StudentId = 1, Course = "数学" },
    new StudentCourse { StudentId = 1, Course = "语文" },
    new StudentCourse { StudentId = 1, Course = "英语" },
    new StudentCourse { StudentId = 2, Course = "数学" },
    new StudentCourse { StudentId = 2, Course = "英语" },
    new StudentCourse { StudentId = 3, Course = "数学" },
    new StudentCourse { StudentId = 3, Course = "语文" }
};

var bothCourses = studentCourses
    .GroupBy(sc => sc.StudentId)                                    // 按学生ID分组
    .Where(g => g.Select(sc => sc.Course).Contains("数学") &&     // 筛选：分组中包含"数学"课程
                g.Select(sc => sc.Course).Contains("语文"))         // 且包含"语文"课程
    .Select(g => g.Key);                                            // 取出满足条件的学生ID
// 结果: 1, 3

// 3. 分页获取，每页5条
int pageSize = 5;
var pagedStudents = students
    .OrderBy(s => s.Id)            // 先按 Id 排序确保分页顺序一致
    .Skip(0)                        // 跳过 0 条（第1页）
    .Take(pageSize);                 // 取前 pageSize 条
```

---

## 第五章 元素操作

### 5.1 First / FirstOrDefault / Last / LastOrDefault

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

// First - 返回第一个元素（无则抛异常）
int first = numbers.First();                    // 1
int firstEven = numbers.First(n => n % 2 == 0); // 2

// FirstOrDefault - 返回第一个元素（无则返回默认值）
int firstOrDefault = numbers.FirstOrDefault();  // 1
int firstOrDefaultEmpty = new List<int>().FirstOrDefault(); // 0

// Last - 返回最后一个元素
int last = numbers.Last();                      // 5

// LastOrDefault - 返回最后一个元素（无则返回默认值）
int lastOrDefault = numbers.LastOrDefault();    // 5
```

**区别与选择：**

| 方法 | 空集合 | 无匹配元素 |
|------|--------|-----------|
| First | 抛异常 | 抛异常 |
| FirstOrDefault | 返回默认值 | 抛异常 |
| Last | 抛异常 | 抛异常 |
| LastOrDefault | 返回默认值 | 抛异常 |

```csharp
List<int> empty = new List<int>();
List<int> withNulls = new List<int> { 1, 2, 3 };

// FirstOrDefault返回默认值
Console.WriteLine(empty.FirstOrDefault());              // 0
Console.WriteLine(empty.FirstOrDefault(-1));             // -1 (可指定默认值)

// 对于引用类型，默认值是null
List<string> emptyStrings = new List<string>();
string firstStr = emptyStrings.FirstOrDefault("N/A");   // "N/A"
```

### 5.2 Single / SingleOrDefault / ElementAt

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

// Single - 返回唯一元素（不是1个则抛异常）
int single = numbers.Single();         // 抛异常，因为有5个元素

List<int> singleList = new List<int> { 42 };
int singleValue = singleList.Single();  // 42

// SingleOrDefault - 空集合返回默认值，多个元素抛异常
var empty = new List<int>();
int singleOrDefault = empty.SingleOrDefault();  // 0

// 带条件查找
int singleEven = numbers.Single(n => n == 2);  // 2

// ElementAt - 返回指定索引的元素
int element = numbers.ElementAt(2);   // 3
int elementFirst = numbers.ElementAt(0); // 1
int elementLast = numbers.ElementAt(numbers.Count - 1); // 5

// ElementAtOrDefault - 越界返回默认值
int elementOrDefault = numbers.ElementAtOrDefault(10);  // 0
```

**ElementAt使用场景：**
```csharp
// 获取第N个元素（从0开始）
var third = students.ElementAt(2);  // 第3个学生

// 配合排序获取最高/最低
var highestScore = students.OrderByDescending(s => s.Score).ElementAt(0);
```

### 5.3 默认值处理（DefaultIfEmpty）

```csharp
List<int> empty = new List<int>();
List<int> numbers = new List<int> { 1, 2, 3 };

// DefaultIfEmpty - 空集合返回默认值
var defaultEmpty = empty.DefaultIfEmpty();     // { 0 }
var defaultEmptyStr = empty.DefaultIfEmpty(5); // { 5 }

// 非空集合返回原集合
var defaultNotEmpty = numbers.DefaultIfEmpty(); // { 1, 2, 3 }

// 实际应用：左外连接
List<Customer> customers = new List<Customer>
{
    new Customer { Id = 1, Name = "张三" },
    new Customer { Id = 2, Name = "李四" }
};

List<Order> orders = new List<Order>
{
    new Order { CustomerId = 1, Amount = 100 },
    new Order { CustomerId = 3, Amount = 200 }  // 没有对应客户
};

// 左外连接示例
var leftJoin = customers.GroupJoin(orders,
    c => c.Id,
    o => o.CustomerId,
    (c, orderGroup) => new
    {
        CustomerName = c.Name,
        Order = orderGroup.DefaultIfEmpty(new Order { Amount = 0 })
    });
```

---

### 第五章 习题

#### 习题 5.1 - 元素查找

```csharp
List<string> words = new List<string> { "apple", "banana", "cherry", "date" };
List<int> empty = new List<int>();
List<int> numbers = new List<int> { 5 };
```

1. 获取words的第一个元素
2. 获取words的最后一个元素
3. 获取empty集合的FirstOrDefault（指定默认值100）
4. 使用Single获取numbers的唯一元素
5. 获取words中索引为2的元素
6. 获取索引为10的元素（超出范围）

#### 习题 5.2 - 条件元素查找

```csharp
List<Student> students = new List<Student>
{
    new Student { Id = 1, Name = "张三", Score = 85 },
    new Student { Id = 2, Name = "李四", Score = 72 },
    new Student { Id = 3, Name = "王五", Score = 90 }
};
```

1. 找到成绩最高的学生
2. 找到成绩最低的学生
3. 找到第一个成绩大于80的学生
4. 找到唯一一门成绩为100的学生（假设数据）

#### 习题 5.3 - DefaultIfEmpty应用

实现左外连接：查询所有课程及其选课学生（包括没有学生选读的课程）

```csharp
List<Course> courses = new List<Course>
{
    new Course { Id = 1, Name = "数学" },
    new Course { Id = 2, Name = "语文" },
    new Course { Id = 3, Name = "英语" }
};

List<Enrollment> enrollments = new List<Enrollment>
{
    new Enrollment { CourseId = 1, StudentName = "张三" },
    new Enrollment { CourseId = 1, StudentName = "李四" },
    new Enrollment { CourseId = 2, StudentName = "王五" }
    // 英语没有学生选读
};
```

---

**习题 5 答案**

#### 答案 5.1

```csharp
// 1. 第一个元素
string firstWord = words.First(); // "apple"

// 2. 最后一个元素
string lastWord = words.Last();   // "date"

// 3. 空集合FirstOrDefault，指定默认值100
int defaultVal = empty.FirstOrDefault(100); // 100

// 4. Single获取唯一元素
int onlyOne = numbers.Single(); // 5

// 5. 索引为2的元素
string third = words.ElementAt(2); // "cherry"

// 6. 索引10，超出范围
int outOfRange = words.ElementAtOrDefault(10); // null (string默认值)
```

#### 答案 5.2

```csharp
// 1. 成绩最高的学生
var highest = students.OrderByDescending(s => s.Score).First();
// 或: students.MaxBy(s => s.Score)
// 结果: 王五(90)

// 2. 成绩最低的学生
var lowest = students.OrderBy(s => s.Score).First();
// 或: students.MinBy(s => s.Score)
// 结果: 李四(72)

// 3. 第一个成绩大于80的学生
var firstAbove80 = students.First(s => s.Score > 80);
// 结果: 张三(85)

// 4. 唯一成绩为100的学生（假设有数据）
var perfectScore = students.SingleOrDefault(s => s.Score == 100);
// 如果没有，返回null
```

#### 答案 5.3

```csharp
// 左外连接：所有课程及其选课学生
var leftOuterJoin = courses.GroupJoin(enrollments,      // 将课程与选课表进行分组连接
    c => c.Id,                          // 从课程集合提取键
    e => e.CourseId,                    // 从选课表提取键
    (c, enrollGroup) => new             // 对于每个课程，enrollGroup 是对应的选课记录集合（可能为空）
    {
        CourseName = c.Name,
        Students = enrollGroup.Select(e => e.StudentName)      // 从选课记录中提取学生姓名
                              .DefaultIfEmpty("无学生")         // 若选课组为空，则添加默认值"无学生"
                              .ToList()                         // 转为 List
    });

/* 结果:
   数学: 张三, 李四
   语文: 王五
   英语: 无学生
*/

// 或者使用查询语法
var querySyntax = from c in courses
                  join e in enrollments on c.Id equals e.CourseId into enrollGroup  // into：将连接结果存入分组变量
                  select new
                  {
                      CourseName = c.Name,
                      Students = enrollGroup.Select(e => e.StudentName)      // 从选课记录中提取学生姓名
                                            .DefaultIfEmpty("无学生")         // 课程无学生选课时使用默认值
                                            .ToList()
                  };
```

---

## 第六章 量化操作与集合判断

### 6.1 Any / All / Contains

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

// Any - 是否有任意元素满足条件
bool hasEven = numbers.Any(n => n % 2 == 0);  // true (有偶数)
bool hasNegative = numbers.Any(n => n < 0);   // false
bool isEmpty = numbers.Any();                 // true (非空)

// All - 所有元素是否都满足条件
bool allPositive = numbers.All(n => n > 0);   // true
bool allEven = numbers.All(n => n % 2 == 0);  // false

// Contains - 是否包含指定元素
bool contains3 = numbers.Contains(3);        // true
bool contains10 = numbers.Contains(10);       // false

// 空集合的特殊情况
List<int> empty = new List<int>();
empty.Any();                                  // false
empty.All(n => n > 0);                        // true (空集合的所有都满足条件！)

// 字符串操作
string text = "Hello LINQ";
text.Any(char.IsWhiteSpace);                 // true (有空格)
text.All(char.IsLetterOrDigit);               // false (有空格)
```

**与Take/Where的区别：**
```csharp
// Any比Count更高效（找到第一个就停止）
// Bad: list.Count(x => x > 0) > 0
// Good: list.Any(x => x > 0)

// All可以用Any实现否定
bool allPositive = !numbers.Any(n => n <= 0);
```

---

### 第六章 习题

#### 习题 6.1 - 基本判断

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };
List<string> words = new List<string> { "apple", "banana", "cherry" };
```

1. 判断numbers是否包含偶数
2. 判断numbers是否所有元素都大于0
3. 判断words是否包含"banana"
4. 判断words是否所有元素都以字母开头

#### 习题 6.2 - 复杂对象判断

```csharp
List<Student> students = new List<Student>
{
    new Student { Name = "张三", Score = 85, Passed = true },
    new Student { Name = "李四", Score = 72, Passed = false },
    new Student { Name = "王五", Score = 90, Passed = true },
    new Student { Name = "赵六", Score = 55, Passed = false }
};
```

1. 判断是否有学生分数超过90
2. 判断是否所有学生都及格（Passed = true）
3. 判断是否有学生名叫"张三"
4. 判断是否所有学生分数都大于60

#### 习题 6.3 - 综合应用

实现以下判断：
1. 判断一个列表是否包含重复元素
2. 判断列表A是否完全包含列表B的所有元素（A的超集）
3. 判断列表是否已排序（升序）

---

**习题 6 答案**

#### 答案 6.1

```csharp
// 1. 是否包含偶数
bool hasEven = numbers.Any(n => n % 2 == 0); // true

// 2. 是否所有元素都大于0
bool allPositive = numbers.All(n => n > 0); // true

// 3. 是否包含"banana"
bool containsBanana = words.Contains("banana"); // true

// 4. 是否所有元素都以字母开头
bool allStartWithLetter = words.All(w => char.IsLetter(w[0])); // true
```

#### 答案 6.2

```csharp
// 1. 是否有学生分数超过90
bool hasHighScore = students.Any(s => s.Score > 90); // false (最高90)

// 2. 是否所有学生都及格
bool allPassed = students.All(s => s.Passed); // false (李四和赵六未及格)

// 3. 是否有学生名叫"张三"
bool hasZhangSan = students.Any(s => s.Name == "张三"); // true

// 4. 是否所有学生分数都大于60
bool allAbove60 = students.All(s => s.Score > 60); // false (55分)
```

#### 答案 6.3

```csharp
// 1. 是否包含重复元素
List<int> list = new List<int> { 1, 2, 3, 2, 4 };
bool hasDuplicates = list.Distinct().Count() != list.Count();
// 结果: true

// 2. A是否完全包含B（A是B的超集）
List<int> a = new List<int> { 1, 2, 3, 4, 5 };
List<int> b = new List<int> { 2, 4 };
bool isSuperset = b.All(element => a.Contains(element));
// 结果: true

// 或者使用SetEquals（需要相同去重）
bool setSuperset = new HashSet<int>(a).IsSupersetOf(b);
// 结果: true

// 3. 列表是否已排序（升序）
List<int> sorted = new List<int> { 1, 2, 3, 4, 5 };
List<int> unsorted = new List<int> { 1, 3, 2, 4, 5 };

bool isSortedAsc = sorted.SequenceEqual(sorted.OrderBy(x => x)); // true
bool isUnsorted = !unsorted.SequenceEqual(unsorted.OrderBy(x => x)); // true
```

---

## 第七章 类型转换与生成

### 7.1 OfType / Cast

```csharp
// 混合类型集合
IList mixed = new object[] { 1, "two", 3.0, "four", 5 };

// OfType<T> - 只保留指定类型的元素
var integers = mixed.OfType<int>();          // { 1, 5 }
var strings = mixed.OfType<string>();        // { "two", "four" }
var doubles = mixed.OfType<double>();        // { 3.0 }

// Cast<T> - 尝试转换所有元素（失败会抛异常）
var castIntegers = mixed.Cast<int>(); // 如果有不能转换的会抛异常

// 实际使用场景
List<BaseClass> derived = new List<DerivedClass>
{
    new DerivedClass { Value = 1 },
    new DerivedClass { Value = 2 }
};
// 转换为基类集合
IEnumerable<BaseClass> baseList = derived.Cast<BaseClass>();
```

### 7.2 AsEnumerable / AsQueryable

```csharp
List<int> list = new List<int> { 1, 2, 3, 4, 5 };

// AsEnumerable - 转换为IEnumerable（延迟执行，使用LINQ to Objects）
IEnumerable<int> enumerable = list.AsEnumerable();

// AsQueryable - 转换为IQueryable（延迟执行，表达式树）
IQueryable<int> queryable = list.AsQueryable();

// 区别：
// - IEnumerable: 在内存中执行
// - IQueryable: 生成表达式树，可用于ORM（如EF Core）
// - AsQueryable适合传递给支持IQueryable的外部系统

// 实际应用：自定义Queryable Provider
IQueryable<int> q = list.AsQueryable();
var result = q.Where(x => x > 3); // 生成表达式树而非立即执行
```

### 7.3 Range / Repeat / Empty

```csharp
// Range - 生成整数序列
var range = Enumerable.Range(1, 5);  // { 1, 2, 3, 4, 5 }
var range0 = Enumerable.Range(0, 10); // { 0, 1, 2, ..., 9 }

// Repeat - 生成重复元素
var repeated = Enumerable.Repeat("A", 3); // { "A", "A", "A" }
var repeatNum = Enumerable.Repeat(0, 5); // { 0, 0, 0, 0, 0 }

// Empty - 生成空集合
var empty = Enumerable.Empty<int>(); // {}

// 实际应用
// 生成1-100的序列
var oneToHundred = Enumerable.Range(1, 100);

// 初始化数组
var zeros = Enumerable.Repeat(0, 10).ToArray();

// 空集合作为默认值
IEnumerable<string> GetEmptyIfNull(IEnumerable<string> source)
{
    return source ?? Enumerable.Empty<string>();
}
```

---

### 第七章 习题

#### 习题 7.1 - 类型转换

```csharp
object[] mixed = { 1, "hello", 2.5, "world", 3, 4.5 };
```

1. 使用OfType提取所有int类型元素
2. 使用OfType提取所有string类型元素
3. 使用OfType提取所有double类型元素
4. 使用Cast尝试转换所有元素为string（观察结果）

#### 习题 7.2 - 集合生成

1. 生成1到20的整数序列
2. 生成10个"LINQ"字符串
3. 生成一个空的学生列表
4. 使用Range计算1到100的平方

#### 习题 7.3 - 综合应用

1. 创建一个包含1-100的数组，过滤出所有偶数并转为字符串列表
2. 实现一个方法，传入一个集合，返回它的IEnumerable和IQueryable版本

---

**习题 7 答案**

#### 答案 7.1

```csharp
// 1. int类型
var ints = mixed.OfType<int>(); // { 1, 3 }

// 2. string类型
var strings = mixed.OfType<string>(); // { "hello", "world" }

// 3. double类型
var doubles = mixed.OfType<double>(); // { 2.5, 4.5 }

// 4. Cast转string（会抛异常，因为不是所有元素都能转成string）
try
{
    var castStrings = mixed.Cast<string>().ToList();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message); // 格式化失败
}
```

#### 答案 7.2

```csharp
// 1. 1到20的整数
var range = Enumerable.Range(1, 20); // { 1, 2, ..., 20 }

// 2. 10个"LINQ"
var repeat = Enumerable.Repeat("LINQ", 10);
// 结果: "LINQ", "LINQ", ..., "LINQ" (10个)

// 3. 空学生列表
var emptyStudents = Enumerable.Empty<Student>();

// 4. 1到100的平方
var squares = Enumerable.Range(1, 100).Select(x => x * x);
// 结果: { 1, 4, 9, 16, ..., 10000 }
```

#### 答案 7.3

```csharp
// 1. 1-100偶数转字符串列表
var evenStrings = Enumerable.Range(1, 100)
    .Where(x => x % 2 == 0)
    .Select(x => x.ToString())
    .ToList();
// 结果: "2", "4", "6", ..., "100"

// 2. 返回IEnumerable和IQueryable
public static void ConvertToQueryable<T>(IEnumerable<T> source)
{
    // 转换为IEnumerable（使用LINQ to Objects）
    IEnumerable<T> enumerable = source.AsEnumerable();

    // 转换为IQueryable（可用于ORM等）
    IQueryable<T> queryable = source.AsQueryable();

    // 两者都可以继续使用LINQ操作
    var result1 = enumerable.Where(x => true);  // 内存执行
    var result2 = queryable.Where(x => true);   // 生成表达式树
}
```

---

## 第八章 延迟执行与即时执行

### 8.1 IQueryable vs IEnumerable

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

// IEnumerable - 延迟执行
IEnumerable<int> query1 = numbers.Where(n => n > 3); // 不执行
var result1 = query1.ToList(); // 执行，返回 { 4, 5 }

// IQueryable - 延迟执行（表达式树）
IQueryable<int> query2 = numbers.AsQueryable().Where(n => n > 3);
// 不执行，生成表达式树

// 延迟执行的优势：可以组合多个操作，最后一次性执行
var combined = numbers
    .Where(n => n > 2)
    .Where(n => n < 5)
    .Select(n => n * 2);
// 实际只执行一次遍历

// 延迟执行的缺点：可能多次遍历
IEnumerable<int> lazy = numbers.Where(n => n > 3);
// 第一次：lazy.ToList() -> 执行
// 第二次：lazy.ToList() -> 再次执行！
```

**执行时机对比：**
```csharp
IEnumerable<int> source = new List<int> { 1, 2, 3 };

// 延迟执行 - 不会立即执行
var deferred = source.Select(x => { Console.WriteLine(x); return x; });

// 立即执行 - ToList/ToArray等
var immediate = source.Select(x => { Console.WriteLine(x); return x; }).ToList();
```

### 8.2 ToList / ToArray / ToDictionary

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

// ToList - 转换为List<T>
List<int> list = numbers.Where(n => n > 3).ToList(); // { 4, 5 }

// ToArray - 转换为T[]
int[] array = numbers.Where(n => n > 3).ToArray(); // { 4, 5 }

// ToDictionary - 转换为Dictionary<TKey, TValue>
List<Product> products = new List<Product>
{
    new Product { Id = 1, Name = "Apple", Price = 3 },
    new Product { Id = 2, Name = "Banana", Price = 2 }
};

// 指定key和value
Dictionary<int, Product> dict1 = products.ToDictionary(p => p.Id);
// { 1: Apple, 2: Banana }

// 指定key和value（自定义）
Dictionary<string, decimal> dict2 = products.ToDictionary(p => p.Name, p => p.Price);
// { "Apple": 3, "Banana": 2 }

// ToHashSet - 转换为HashSet<T>
HashSet<int> hashSet = numbers.Distinct().ToHashSet();
```

**何时触发执行：**

| 操作 | 执行时机 | 说明 |
|------|----------|------|
| ToList/ToArray/ToDictionary | 立即 | 强制执行并缓存结果 |
| First/Last/Single | 立即 | 取值时执行 |
| Count | 立即 | 需要计算数量 |
| foreach | 延迟 | 遍历时执行 |
| Where/Select/OrderBy | 延迟 | 返回IEnumerable |

```csharp
// 常见陷阱：延迟执行导致多次执行
var query = numbers.Where(n => n > 3);
// 每次都执行一次Where！
foreach (var item in query) { }
foreach (var item in query) { }

// 解决：缓存结果
var cached = numbers.Where(n => n > 3).ToList();
foreach (var item in cached) { }
foreach (var item in cached) { } // 使用缓存，不再执行
```

---

### 第八章 习题

#### 习题 8.1 - 执行时机判断

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };
```

判断以下操作是延迟执行还是立即执行：
1. `numbers.Where(n => n > 3)`
2. `numbers.Where(n => n > 3).ToList()`
3. `numbers.Take(3)`
4. `numbers.Take(3).ToArray()`
5. `numbers.First()`
6. `numbers.Count()`
7. `numbers.OrderBy(n => n)`

#### 习题 8.2 - 性能优化

场景：有一个大集合，需要多次查询

```csharp
List<int> largeList = Enumerable.Range(1, 10000).ToList();
```

1. 错误的做法：每次都执行完整的查询
2. 正确的做法：缓存结果后多次使用

#### 习题 8.3 - ToDictionary应用

```csharp
List<Student> students = new List<Student>
{
    new Student { Id = 1, Name = "张三", Score = 85 },
    new Student { Id = 2, Name = "李四", Score = 72 },
    new Student { Id = 3, Name = "王五", Score = 90 }
};
```

1. 创建一个以Id为key的字典
2. 创建一个以Name为key，Score为value的字典

#### 习题 8.4 - 综合理解

解释以下代码的执行流程和输出：

```csharp
var list = new List<int> { 1, 2, 3, 4, 5 };
var query = list.Where(x => x > 2).Select(x => x * 2);
Console.WriteLine("查询创建完成");
var result = query.ToList();
Console.WriteLine("执行完成");
foreach (var item in result)
    Console.WriteLine(item);
```

---

**习题 8 答案**

#### 答案 8.1

```csharp
// 1. Where - 延迟执行（返回IEnumerable，不立即执行）
var q1 = numbers.Where(n => n > 3);

// 2. ToList - 立即执行（强制遍历并缓存为 List）
var q2 = numbers.Where(n => n > 3).ToList();

// 3. Take - 延迟执行（返回IEnumerable，不立即执行）
var q3 = numbers.Take(3);

// 4. ToArray - 立即执行（强制遍历并缓存为数组）
var q4 = numbers.Take(3).ToArray();

// 5. First - 立即执行（立即遍历找到第一个元素）
var q5 = numbers.First();

// 6. Count - 立即执行（立即遍历计算数量）
var q6 = numbers.Count();

// 7. OrderBy - 延迟执行（返回IOrderedEnumerable，不立即执行）
var q7 = numbers.OrderBy(n => n);
```

#### 答案 8.2

```csharp
// 错误做法：每次都重新执行
var largeList = Enumerable.Range(1, 10000).ToList();

// 错误：每次Where都会遍历整个集合
for (int i = 0; i < 3; i++)
{
    var result = largeList.Where(n => n > 5000).ToList();
    // 每次都执行一次完整的过滤操作！
}

// 正确做法：先缓存结果
var cached = largeList.Where(n => n > 5000).ToList();
for (int i = 0; i < 3; i++)
{
    // 使用缓存，不再重复执行
    foreach (var item in cached) { }
}
```

#### 答案 8.3

```csharp
// 1. 以Id为key的字典
Dictionary<int, Student> byId = students.ToDictionary(s => s.Id);  // 将集合转为 Dictionary，以 Id 为键，Student 对象为值
/* 结果:
   1 -> 张三(85)
   2 -> 李四(72)
   3 -> 王五(90)
*/

// 2. 以Name为key，Score为value的字典
Dictionary<string, int> nameToScore = students.ToDictionary(s => s.Name,  // 键选择器：以学生姓名为键
                                                             s => s.Score); // 值选择器：以学生分数为值
/* 结果:
   "张三" -> 85
   "李四" -> 72
   "王五" -> 90
*/
```

#### 答案 8.4

```csharp
var list = new List<int> { 1, 2, 3, 4, 5 };
// 延迟执行：创建查询表达式，不执行
var query = list.Where(x => x > 2).Select(x => x * 2);

// 输出: "查询创建完成" （query还没执行）
Console.WriteLine("查询创建完成");

// ToList触发立即执行：遍历list，筛选>2，乘2，转List
var result = query.ToList();

// 输出: "执行完成"
Console.WriteLine("执行完成");

// 遍历结果（已缓存）
foreach (var item in result)
    Console.WriteLine(item);

// 输出:
// 6
// 8
// 10
// (即 3*2, 4*2, 5*2)
```

---

## 附录：LINQ方法速查表

### 筛选与限制
| 方法 | 说明 |
|------|------|
| `Where` | 按条件筛选 |
| `Take` | 取前N个 |
| `Skip` | 跳过前N个 |
| `TakeWhile` | 满足条件时继续取 |
| `SkipWhile` | 满足条件时跳过 |

### 投影
| 方法 | 说明 |
|------|------|
| `Select` | 转换每个元素 |
| `SelectMany` | 展平嵌套集合 |

### 排序
| 方法 | 说明 |
|------|------|
| `OrderBy` | 升序 |
| `OrderByDescending` | 降序 |
| `ThenBy` | 次级升序 |
| `ThenByDescending` | 次级降序 |

### 聚合
| 方法 | 说明 |
|------|------|
| `Count` | 计数 |
| `Sum` | 求和 |
| `Average` | 平均值 |
| `Min` | 最小值 |
| `Max` | 最大值 |
| `Aggregate` | 自定义聚合 |

### 元素操作
| 方法 | 说明 |
|------|------|
| `First` | 第一个元素 |
| `FirstOrDefault` | 第一个或默认值 |
| `Last` | 最后一个元素 |
| `LastOrDefault` | 最后一个或默认值 |
| `Single` | 唯一元素 |
| `SingleOrDefault` | 唯一或默认值 |
| `ElementAt` | 指定索引元素 |

### 集合操作
| 方法 | 说明 |
|------|------|
| `Distinct` | 去重 |
| `Union` | 并集 |
| `Intersect` | 交集 |
| `Except` | 差集 |

### 量化操作
| 方法 | 说明 |
|------|------|
| `Any` | 任意满足 |
| `All` | 全部满足 |
| `Contains` | 包含指定元素 |

### 类型转换
| 方法 | 说明 |
|------|------|
| `OfType` | 按类型筛选 |
| `Cast` | 类型转换 |
| `ToList` | 转List |
| `ToArray` | 转数组 |
| `ToDictionary` | 转字典 |

---

**文档结束**