# JSON 完全指南

> **零基础入门**：本指南将从最基础的概念开始讲解，通过大量通俗易懂的例子帮助你理解 JSON。即使你从未接触过编程，也能学会！

---

## 零基础概念入门：什么是 JSON？

### 场景带入：生活中的 JSON

想象一下，你需要把一个人信息记录下来，交给别人。看下面两种记录方式：

**方式一：文字描述**
```
这个人的名字叫张三，年龄25岁，是一名学生，手机号是13800001234。
```

**方式二：JSON 格式**
```json
{
  "name": "张三",
  "age": 25,
  "isStudent": true,
  "phone": "13800001234"
}
```

第二种就是 JSON 格式！看起来整整齐齐、清清爽爽的，别人一眼就能看懂。

---

### 为什么要用 JSON？

#### 1.1.1 生活中的例子

想象你在**网购**：

1. **下单时**：手机APP 把你的订单信息（商品、价格、数量、地址）转换成 JSON 发送给服务器
2. **服务器处理**：服务器收到 JSON，读取信息，处理订单
3. **返回结果**：服务器把处理结果（订单号、状态）再以 JSON 格式返回给你
4. **APP 显示**：APP 解析 JSON，把结果展示在屏幕上

整个过程，JSON 就像一份**标准化的表单**，让不同的系统能互相"说话"。

#### 1.1.2 比喻说明

| 现实中的例子 | JSON 的作用 |
|------------|------------|
| 填写表格 | 按固定格式记录信息 |
| 商务合同 | 双方约定的标准格式 |
| 国际标准语言 | 跨系统、跨语言的数据交换 |

> **简单理解**：JSON 就是一套"信息表格"的写法，全世界人都用这一套写法，这样不同国家的人、不同品牌的手机都能看懂对方的信息。

---

### 1.1 到底什么是 JSON？

**JSON** 的全称是 **JavaScript Object Notation**，中文叫"JavaScript 对象表示法"。

拆开来看：
- **JavaScript**：一种编程语言，JSON 最初就是用它写的
- **Object**：对象，这里指"一个东西、一件事"
- **Notation**：表示方法、写法

**一句话总结**：

> JSON 就是一种"信息的写法"，用固定的格式把信息记录下来，便于不同系统之间传递和理解。

就像填表格一样，左边是项目名称，右边是具体内容：

| 项目 | 内容 |
|------|------|
| 姓名 | 张三 |
| 年龄 | 25 |
| 职业 | 学生 |

JSON 就是把上面这种表格，用代码形式写出来：

```json
{
  "姓名": "张三",
  "年龄": 25,
  "职业": "学生"
}
```

---

### 1.2 JSON 长什么样？

看几个例子：

#### 例1：一本书的信息

```json
{
  "书名": "哈利波特",
  "作者": "J.K.罗琳",
  "价格": 68.00,
  "有货": true
}
```

解读：
- "书名" 对应 "哈利波特"（文字）
- "价格" 对应 68.00（数字）
- "有货" 对应 true/false（真/假）

#### 例2：一个学生列表

```json
["张三", "李四", "王五"]
```

解读：这就是��个**列表**，里面有三个学生的名字。

#### 例3：复杂的学生信息

```json
{
  "name": "张三",
  "age": 18,
  "scores": [85, 92, 78],
  "address": {
    "city": "北京",
    "district": "朝阳区"
  }
}
```

解读：
- `scores` 是一个数组（成绩列表）
- `address` 是一个对象（里面又包含城市和行政区）

---

### 1.3 为什么全世界都在用 JSON？

看一个对比表：

| 对比项 | JSON | 其他格式 |
|-------|-----|----------|
| 写起来 | 简单 | 复杂 |
| 读起来 | 容易 | 较难 |
| 传起来 | 数据量小，速度快 | 数据量大，速度慢 |
| 手机、电脑都能用 | ✓ | ✓ |

**核心优势**：
1. **简单**：学一会儿就能会
2. **轻量**：数据量小，传输快
3. **通用**：全世界都在用

---

## 一、JSON 是什么

### 1.1 定义

### 1.2 JSON 的特点

| 特点 | 说明 | 简单理解 |
|------|------|----------|
| 轻量级 | 相对于 XML，JSON 数据更小，传输更快 | 数据量小，网速慢也不怕 |
| 易于理解 | 人类可读，结构清晰 | 就像填表格，一眼就能看懂 |
| 语言独立 | 不局限于 JavaScript，多语言支持 | 手机、电脑、老系统都能用 |
| 数据格式 | 纯数据，不包含方法或函数 | 只记录信息，不记录"怎么做" |
| 递归结构 | 支持嵌套，可描述复杂数据 | 对象里面还能放对象 |

### 1.3 JSON vs XML

| 特性 | JSON | XML |
|------|------|-----|
| 数据体积 | 小 | 大 |
| 读取速度 | 快 | 慢 |
| 语法复杂度 | 简单 | 复杂 |
| 数据类型 | 支持有限 | 支持丰富 |
| 可读性 | 高 | 高 |
| 扩展性 | 一般 | 强 |

---

## 二、JSON 数据类型：信息的"种类"

> **零基础理解**：JSON 就像一种表格，只能填写几种固定类型的内容。就像 Excel 表格，有的格子填文字，有的格子填数字，有的格子画勾选 √

### 2.0 为什么要了解数据类型？

想想填写表单的时候：
- 姓名栏：填文字（"张三"）
- 年龄栏：填数字（18）
- 是否VIP：勾选（是/否）
- 多个爱好：列一个清单

JSON 也有这些"格子"，叫**数据类型**。

---

### 2.1 六种基本数据类型

JSON 支持以下数据类型：

| 类型 | 示例 | 零基础理解 |
|------|------|-----------|
| 字符串 | `"Hello"` | 文字内容 |
| 数字 | `123`, `3.14` | 数字（整数或小数） |
| 布尔值 | `true`, `false" | √ 或 ×（真或假） |
| 对象 | `{"name": "Tom"}` | 一张完整的表格 |
| 数组 | `[1, 2, 3]` | 一个清单列表 |
| 空值 | `null` | 这里没有内容（空白） |

**小贴士**：就像 Excel 表格里，有的格子里写文字，有的画钩，有的留空

---

### 2.2 字符串：文字内容

#### 零基础解释

**字符串**就是"一段文字"，比如名字、地址、评论等。

**生活中的例子**：
| 现实中的表格 | JSON 写法 |
|------------|----------|
| 姓名：张三 | `"张三"` |
| 地址：北京市 | `"北京市"` |
| 备注：暂无 | `"暂无"` |

#### 具体示例

```json
"这是一个字符串"
```

```json
"这是一个字符串"
```

**规则**：
- 必须使用**双引号**（""），不能用单引号（''）
- 就像表格里写文字必须加引号，这是 JSON 的"规矩"

**进阶：转义字符**（特殊文字怎么写？）

| 想写什么 | 怎么写 | 说明 |
|---------|--------|------|
| 双引号 | `\"` | 文字中要显示引号 |
| 反斜杠 | `\\` | 文字中要显示 \ |
| 换行 | `\n` | 文字中要换行 |
| 制表符 | `\t` | 文字中要空一段 |

**示例**：
```json
"他说：\"你好\"，然后\\走开了"
```
显示出来就是：`他说："你好"，然后\走开了`

---

### 2.3 数字：数字内容

#### 零基础解释

**数字**就是数字，可以是整数也可以是小数。

**生活中的例子**：
| 现实中的表格 | JSON 写法 |
|------------|----------|
| 年龄：18 | `18` |
| 价格：99.50 | `99.5` |
| 欠款：-100 | `-100` |

#### 具体示例

```json
42          // 整数：就像 42
3.14        // 小数：就像 3.14
-10         // 负数：欠了10块
1.5e10      // 科学计数法：1.5×10的10次方
```

**小贴士**：数字不需要加引号，直接写就行！

```json
42          // 整数
3.14        // 浮点数
-10         // 负数
1.5e10      // 科学计数法
```

### 2.4 布尔值：√ 或 ×

#### 零基础解释

**布尔值**就是"真"或"假"，就像表格里的复选框，打钩✓ 或不打钩×。

**生活中的例子**：
| 现实中的表格 | JSON 写法 |
|------------|----------|
| 是否VIP：√ | `true` |
| 是否学生：× | `false` |
| 有货吗？ | `true` |

#### 具体示例

```json
true     // 对应：✓、是、真、开通
false    // 对应：×、否、假、没开通
```

**注意**：必须使用小写 `true`/`false`，不能写成 `True`/`False`（就像 Excel 里的复选框只能是打钩或留空）

---

### 2.5 对象：一张完整的表格

#### 零基础解释

**对象**就是"一张完整的表格"，包含多个项目。

就像这样的表格：

| 项目 | 内容 |
|------|------|
| 姓名 | 张三 |
| 年龄 | 25 |
| 是学生吗？ | √ |

用 JSON 写就是这样：

```json
{
  "姓名": "张三",
  "年龄": 25,
  "是学生": true
}
```

**解读**：用大括号 `{}` 包裹，里面写多个"项目名:内容"，用逗号分隔。

#### 具体示例

```json
{
  "name": "张三",
  "age": 25,
  "isStudent": false
}
```

**对象规则**（把表格写规范的规矩）：
- 用 `{}` 大括号包裹（就像表格有个边框）
- 键值对用 `:` 分隔（项目名:内容）
- 键必须是双引号（项目名要加引号）
- 值可以是任意类型（内容可以是文字、数字、勾选等）
- 多对之间用 `,` 分隔（项目之间加逗号）
- 最后一项不加逗号（末尾不要再加逗号）

```json
{
  "name": "张三",
  "age": 25,
  "isStudent": false
}
```

对象规则：
- 使用 `{}` 包裹
- 键值对用 `:` 分隔
- 键必须是字符串（双引号）
- 值可以是任意 JSON 类型
- 多对之间用 `,` 分隔
- 最后一项不加逗号

### 2.6 数组：一个清单列表

#### 零基础解释

**数组**就是"一个清单"，把相同类型的东西列在一起。

就像购物的清单：

| 序号 | 商品 |
|------|------|
| 1 | 苹果 |
| 2 | 香蕉 |
| 3 | 橙子 |

用 JSON 写就是这样：

```json
["苹果", "香蕉", "橙子"]
```

**解读**：用中括号 `[]` 包裹，里面放一列东西，用逗号分隔。

#### 具体示例

```json
["苹果", "香蕉", "橙子"]      // 文字清单
[1, 2, 3, 4, 5]           // 数字清单
[true, false, true]          // 布尔值清单
[{"name": "Tom"}, {"name": "Jerry"}]  // 对象清单
```

**小贴士**：数组就像 Excel 的一行，里面依次填入内容

**数组规则**：
- 用 `[]` 中括号包裹（就像清单有个边框）
- 元素类型可以混合（文字、数字、对象可以混在一起）
- 元素之间用 `,` 分隔（每项之间加逗号）

---

### 2.7 空值：空白

#### 零基础解释

**空值**就是"这里没有内容"，留白。

| 现实中的表格 | JSON 写法 |
|------------|----------|
| （空白） | `null` |

#### 具体示例

```json
null
```

**使用场景**：
- 不知道的信息写 `null`
- 还没有填的内容写 `null`
- 删除的内容也可以用 `null` 表示

---

### 2.8 总结：数据类型一览表

| 数据类型 | 怎么写 | 像什么 | 例子 |
|---------|--------|--------|------|
| 字符串 | 加引号 | 文字 | `"你好"` |
| 数字 | 不加引号 | 数字 | `18`、`3.14` |
| 布尔值 | true/false | √ 或 × | `true` |
| 对象 | {} 包裹 | 表格 | `{"名":"张三"}` |
| 数组 | [] 包裹 | 清单 | `["甲","乙"]` |
| 空值 | null | 空白 | `null` |

```json
null
```

---

## 三、JSON 语法规则：写规范的"规矩"

> **零基础理解**：就像填表格有规矩，JSON 也有自己的"规矩"。不按规矩来，别人就看不懂了！

### 3.0 为什么要有语法规则？

想想如果表格这样写会怎么样：
- 有人用蓝笔，有人用红笔
- 有的写中文，有的写英文
- 有的有表头，有的没有

所以必须有统一的规矩，大家才能看懂！

JSON 的规矩很简单，记住以下几点就行：

---

### 3.1 核心规则

### 3.1 核心规则

```json
{
  "key": "value",    // 键必须双引号
  "num": 10,
  "flag": true,
  "arr": [1, 2, 3],
  "obj": {"inner": "object"},
  "empty": null
}
```

**必须遵守的规矩**（一定要记住！）：

| 规矩 | 正确写法 | 错误写法 | 说明 |
|------|---------|---------|------|
| 键必须双引号 | `"name"` | `name` | 就像表格的表头要写清楚 |
| 不能加注释 | `正常写` | `// 注释` | JSON不是作文，不能写注释 |
| 末尾不能加逗号 | `正常` | `{"name": "Tom",}` | 最后一项不要加逗号 |
| 不能写 undefined | `null` | `undefined` | 用 null 表示"没有" |
| 不能写函数 | `正常` | `{"say": func()}` | JSON只记数据，不记"怎么做" |

**简单记忆口诀**：
> 键要引号，尾无逗号，
> 不能注释，不能函，
> 空用 null。

### 3.2 易错点讲解

### 3.2 合法 JSON vs 非法 JSON

**正确示范**：
```json
{"name": "Tom", "age": 20}
```

**常见错误**（一定要避免！）：

| 错误类型 | 错误写法 | 正确写法 |
|---------|---------|---------|
| 键没引号 | `{name: "Tom"}` | `{"name": "Tom"}` |
| 加了注释 | `{"name": "Tom"} // 错` | `{"name": "Tom"}` |
| 末尾逗号 | `{"name": "Tom",}` | `{"name": "Tom"}` |
| 用 undefined | `{"a": undefined}` | `{"a": null}` |
| 加了函数 | `{"say": function(){}}` | 不要加 |

---

## 四、JSON 在 JavaScript 中的使用

> **零基础理解**：JavaScript 是编程语言，JSON 是数据格式。JSON 需要通过 JavaScript 来"读取"和"生成"。这就像：JSON 是文件，JavaScript 是文件阅读器。

### 4.0 为什么要用 JavaScript 处理 JSON？

JSON 是"死"的数据，要读写它，需要编程语言来帮忙。

**比喻**：
- JSON 就像是写在纸上的字
- JavaScript 就像是阅读的人
- 读懂纸上写的是什么，叫做"解析"
- 把想写的内容印到纸上，叫做"序列化"

---

### 4.1 解析 JSON 字符串

```javascript
// 假设这是从服务器发来的"JSON 文本"
const jsonStr = '{"name": "Tom", "age": 20}';

// 用 JSON.parse() "读懂" 这段文字
// 就像找一个翻译，把 JSON 文字翻译成 JavaScript 能看懂的形式
const obj = JSON.parse(jsonStr);

// 现在 obj 就是一个"对象"了，可以直接用
console.log(obj.name);  // 输出：Tom
console.log(obj.age);  // 输出：20
```

**流程图解**：

```
服务器发送的 JSON 文本：
{"name": "Tom", "age": 20}
        ↓
   JSON.parse() 解析
        ↓
变成 JavaScript 对象：
obj = { name: "Tom", age: 20 }
        ↓
可以直接用：
obj.name → "Tom"
obj.age → 20
```

**简单理解**：
- `JSON.parse()` 就是"翻译器"
- 把 JSON 文字翻译成 JavaScript 能用的数据

### 4.2 JavaScript 对象转 JSON：写出来

```javascript
// 假设你有个 JavaScript 对象（就像手里有一份数据）
const obj = {
  name: "Tom",
  age: 20,
  hobbies: ["coding", "reading"]
};

// 用 JSON.stringify() "写下来"
// 就像把脑子里的想法写到纸上
const jsonStr = JSON.stringify(obj);

// 现在 jsonStr 就是 JSON 文字了，可以发给别人
console.log(jsonStr);
// {"name":"Tom","age":20,"hobbies":["coding","reading"]}
```

**流程图解**：

```
JavaScript 对象：
obj = { name: "Tom", age: 20, hobbies: [...] }
        ↓
   JSON.stringify() 写成文字
        ↓
JSON 文本：
{"name":"Tom","age":20,"hobbies":[...]}
        ↓
可以发送到服务器
```

**简单理解**：
- `JSON.stringify()` 就像"打印机"
- 把 JavaScript 数据印到 JSON 纸上

### 4.3 JSON.stringify() 详解：格式化输出

```javascript
const obj = { name: "Tom", age: 20 };

// 基本写法（压缩成一行）
JSON.stringify(obj);  // '{"name":"Tom","age":20}'

// 格式化写法（整齐的分行显示）
JSON.stringify(obj, null, 2);
/*
{
  "name": "Tom",
  "age": 20
}
```

**参数 explained（参数说明）**：

```javascript
const obj = { name: "Tom", age: 20 };

// 第二个参数 replacer：可以选择"留下"或"不要"哪些数据
JSON.stringify(obj, (key, value) => {
  if (key === "age") return undefined;  // 不要 age 这个字段
  return value;
});
// '{"name":"Tom"}'  ← age 被去掉了

// 第三个参数 space：格式化，让显示更整齐
JSON.stringify(obj, null, 2);  // 缩进2空格
JSON.stringify(obj, null, '\t');  // 用 Tab 缩进
```

**什么时候用格式化**？
- 调试时：用 `null, 2` 让 JSON 更好看
- 保存文件时：用 `null, 2` 生成整洁的文件
- 网络传输时：用默认（不格式化），数据量更小

### 4.4 JSON.parse() 详解：读取并转换

```javascript
// 假设收到一段 JSON 文字
const jsonStr = '{"name": "Tom", "age": 20}';

// 直接解析
const obj = JSON.parse(jsonStr);
// obj = { name: "Tom", age: 20 }

// 也可以带一个"翻译器"，边读边转换
const obj = JSON.parse(jsonStr, (key, value) => {
  if (key === "age") return value + "岁";  // 读到 age 时，加个"岁"字
  return value;
});
// obj = { name: "Tom", age: "20岁" }  ← age 变成 "20岁" 了
```

### 4.5 错误处理：出错了怎么办？

```javascript
try {
  // 尝试解析一段可能有问题的 JSON
  const jsonStr = '{"name": "Tom"';  // 少了一个 } 
  const obj = JSON.parse(jsonStr);
} catch (e) {
  console.error("解析出错了！", e.message);
  // 输出：解析出错了！ Unexpected end of JSON input
}
```

**为什么要 try-catch**？
- 网络可能中断
- 数据可能被篡改
- 格式可能不标准

**简单理解**：就像收快递，如果包裹坏了，要能够发现并处理！

```javascript
try {
  const jsonStr = '{"name": "Tom"';  // 非法 JSON
  const obj = JSON.parse(jsonStr);
} catch (e) {
  console.error("JSON 解析错误:", e.message);
}
```

---

## 五、JSON 在各语言中的使用：不同语言怎么用 JSON？

> **零基础理解**：JSON 就像一种"世界语"，不同国家的人（编程语言）都能学。Python、Java、C# 等都有自己的方式来读写 JSON。

### 5.0 为什么要学多种语言？

JSON 是通用的，但不同编程语言用起来略有不同：
- JavaScript：有内置的 `JSON.parse()` 和 `JSON.stringify()`
- Python：需要用 `json` 模块
- Java：需要用第三方库

**比喻**：JSON 就像钱，美元、人民币、欧元都能用，但换算方式不同。

---

### 5.1 Python

```python
import json

# JSON 字符串 → Python 对象
data = json.loads('{"name": "Tom", "age": 20}')
print(data["name"])  # Tom

# Python 对象 → JSON 字符串
obj = {"name": "Tom", "age": 20}
json_str = json.dumps(obj)
print(json_str)  # {"name": "Tom", "age": 20}

# 美化输出
json_str = json.dumps(obj, indent=2, ensure_ascii=False)
```

### 5.2 Java

```java
import com.fasterxml.jackson.databind.ObjectMapper;

// JSON 字符串 → 对象
String jsonStr = "{\"name\": \"Tom\", \"age\": 20}";
ObjectMapper mapper = new ObjectMapper();
User user = mapper.readValue(jsonStr, User.class);

// 对象 → JSON 字符串
String jsonStr = mapper.writeValueAsString(user);
```

```java
import org.json.JSONObject;

// 创建 JSON
JSONObject obj = new JSONObject();
obj.put("name", "Tom");
obj.put("age", 20);
System.out.println(obj.toString());
```

### 5.3 Java

```java
import javax.script.*;

ScriptEngineManager manager = new ScriptEngineManager();
ScriptEngine engine = manager.getEngineByName("Nashorn");

String jsonStr = "{\"name\": \"Tom\", \"age\": 20}";
Object obj = engine.eval("(" + jsonStr + ")");
// 或使用专门库
```

---

## 六、JSON Schema：JSON 的"体检表"

> **零基础理解**：JSON Schema 就像一份"体检表"或"检查清单"，用来验证 JSON 数据是否正确、完整。

### 6.0 为什么要用 Schema？

想象你收到一份简历：
- 你怎么知道对方有没有漏填信息？
- 你怎么知道年龄是不是写了数字？
- 你怎么知道邮箱格式对不对？

这就需要一份"简历填写要求"！JSON Schema 就是这样的"要求清单"。

**生活中的例子**：
| 现实 | JSON Schema |
|------|----------|
| 简历填写要求 | Schema 验证规则 |
| 填好了检查一遍 | 用 Schema 验证 JSON |
| 发现错误打回 | 验证失败报错 |

---

### 6.1 什么是 JSON Schema

JSON Schema 就是规定"JSON 应该长什么样"的规则：
- 哪些字段必须有？
- 每个字段是什么类型？
- 数字能填什么范围？
- 字符串是什么格式？

### 6.2 基本示例

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "用户信息",
  "type": "object",
  "required": ["name", "email"],
  "properties": {
    "name": {
      "type": "string",
      "description": "用户名"
    },
    "age": {
      "type": "integer",
      "minimum": 0,
      "maximum": 150
    },
    "email": {
      "type": "string",
      "format": "email"
    },
    "hobbies": {
      "type": "array",
      "items": {"type": "string"}
    }
  }
}
```

### 6.3 验证数据

```javascript
const schema = {
  type: "object",
  required: ["name"],
  properties: {
    name: { type: "string" },
    age: { type: "integer", minimum: 0 }
  }
};

const data = { name: "Tom", age: 20 };

// 使用 ajv 库验证
const Ajv = require("ajv");
const ajv = new Ajv();
const validate = ajv.compile(schema);

if (validate(data)) {
  console.log("验证通过");
} else {
  console.log("验证失败", validate.errors);
}
```

---

## 七、JSON API：JSON 在网络通信中的应用

> **零基础理解**：现在的软件都是"客户端"和"服务器"配合工作的。JSON 就是它们之间的"信使"。

### 7.0 什么是 API？

**API** 就像是"服务员"：
- 你点菜（发送请求）
- 厨房做菜（服务器处理）
- 服务员上菜（返回结果）

JSON 就是这份"菜单"和"菜"的内容格式！

---

### 7.1 REST API 中的 JSON

```javascript
// 服务器响应
res.json({ success: true, data: { name: "Tom" } })

// 前端请求
fetch("/api/users")
  .then(res => res.json())
  .then(data => console.log(data));
```

### 7.2 GraphQL 与 JSON

```javascript
// GraphQL 查询
const query = `
  query {
    user(id: 1) {
      name
      email
    }
  }
`;

// 响应仍是 JSON
{
  "data": {
    "user": {
      "name": "Tom",
      "email": "tom@example.com"
    }
  }
}
```

---

## 八、JSON 安全：要注意的问题

> **零基础理解**：JSON 虽然好用，但也有安全问题。就像家里的大门要锁好一样，传数据也要注意安全！

### 8.0 常见的安全问题

1. 有人偷偷看你的数据（偷看）
2. 有人假装你发信息（冒充）
3. 数据被改成假的信息（篡改）

---

### 8.1 JSON 越权读取（JSON Hijacking）

#### 生活中的例子

想象你有一份机密文件：
1. 有人打电话自称是你的朋友，要求你把文件传真给他 → **钓鱼攻击**
2. 有人在你不知道的情况下，拿走了文件的副本 → **越权读取**

**攻击原理**：攻击者通过 `<script>` 标签跨域读取 JSON 数据

```html
<!-- 攻击者网站 -->
<script src="https://victim.com/api/secret"></script>
```

```html
<!-- 攻击者网站 -->
<script src="https://victim.com/api/secret"></script>
```

**防御措施**：

1. **CSRF Token**
   ```javascript
   // 服务器验证 Token
   app.use(csrf());
   ```

2. **Referer 检查**
   ```javascript
   app.use((req, res, next) => {
     if (!req.headers.referer.includes(req.hostname)) {
       return res.status(403).send();
     }
     next();
   });
   ```

3. **自定义请求头**
   ```javascript
   // 客户端
   fetch("/api/data", {
     headers: { "X-Requested-With": "XMLHttpRequest" }
   });
   
   // 服务器
   app.use((req, res, next) => {
     if (!req.headers["x-requested-with"]) {
       return res.status(403).send();
     }
     next();
   });
   ```

### 8.2 XSS 风险

**问题**：JSON 数据被直接渲染到 HTML

```javascript
// 服务器
res.json({ content: "<script>alert(1)</script>" });

// 前端直接渲染（危险）
div.innerHTML = data.content;
```

**防御**：

```javascript
// 前端：使用 textContent 而非 innerHTML
div.textContent = data.content;

// 或进行 HTML 转义
function escapeHtml(str) {
  return str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}
```

---

## 九、JSON 工具库

### 9.1 JavaScript

| 库名 | 用途 |
|------|------|
| ajv | JSON Schema 验证 |
| lodash | 对象/数组处理 |
| JSON5 | 支持注释/宽松语法 |
| flatted | 处理循环引用 |

### 9.2 Python

```python
# 常用库
import json           # 标准库
import jsonpath      # JSONPath 查询
from jsonschema import validate  # Schema 验证
```

---

## 十、常见问题

### 10.1 日期处理

JSON 没有日期类型，通常用两种方式：

```javascript
// 方式1：字符串
{ "date": "2024-01-01" }

// 方式2：时间戳
{ "timestamp": 1704067200 }

// 转换
new Date(1704067200 * 1000)
```

### 10.2 处理大数字

```javascript
// JavaScript 中超过 2^53 的数字会有精度问题

// 解决方案：使用字符串
{ "bigInt": "123456789012345678901234567890" }
```

### 10.3 处理 undefined

```javascript
// undefined 会被忽略
JSON.stringify({ a: 1, b: undefined });  // {"a":1}

// 解决方案：自定义处理
JSON.stringify(obj, (k, v) => v === undefined ? null : v);
```

---

## 十一、JSON 在游戏开发中的应用

> **零基础理解**：游戏开发中，JSON 用来保存玩家的进度、游戏的设置、角色的信息等。就像游戏的"记录本"。

### 11.0 JSON 在游戏中做什么？

想想你玩游戏时：
- **存档**：退出游戏后再进来，进度还在 → JSON 保存了进度
- **设置**：画质、音量设置 → JSON 保存了设置
- **抽卡**：游戏数据、角色信息 → JSON 保存了数据
- **更新**：服务器发来的新活动 → JSON 传输了数据

**通俗理解**：JSON 就是游戏的"日记本"，记录了各种信息！

---

### 11.1 Unity 中的 JSON

#### 11.1.1 Unity 内置 JSON 工具

Unity 提供了 `JsonUtility` 类来处理 JSON：

```csharp
using UnityEngine;

// 定义数据类
[System.Serializable]
public class PlayerData
{
    public string name;
    public int level;
    public float health;
    public int[] inventory;
}

public class JsonExample : MonoBehaviour
{
    void Start()
    {
        // 1. 对象转 JSON
        PlayerData player = new PlayerData();
        player.name = "Hero";
        player.level = 10;
        player.health = 100f;
        player.inventory = new int[] { 1, 2, 3 };

        string json = JsonUtility.ToJson(player);
        Debug.Log(json);
        // {"name":"Hero","level":10,"health":100.0,"inventory":[1,2,3]}

        // 2. JSON 转对象
        string jsonStr = {"name":"Hero","level":10,"health":100.0};
        PlayerData loaded = JsonUtility.FromJson<PlayerData>(jsonStr);
        Debug.Log(loaded.name);
    }
}
```

#### 11.1.2 JsonUtility 限制

```csharp
// JsonUtility 的限制：
// 1. 不支持 Dictionary
// 2. 不支持 List/Array 之外的多维数组
// 3. 必须有 [Serializable] 标记
// 4. 只能序列化 public 字段

// 解决方案：使用第三方库如 Newtonsoft.Json (Newtonsoft.Json.dll)
```

#### 11.1.3 使用 Newtonsoft.Json

```csharp
using Newtonsoft.Json;

// 序列化
string json = JsonConvert.SerializeObject(player);

// 反序列化
PlayerData player = JsonConvert.DeserializeObject<PlayerData>(json);

// 支持更多类型
Dictionary<string, int> stats = new Dictionary<string, int>();
stats["strength"] = 100;
stats["agility"] = 80;

string json = JsonConvert.SerializeObject(stats);
// {"strength":100,"agility":80}
```

#### 11.1.4 PlayerPrefs vs JSON

```csharp
// PlayerPrefs 存储（适合小数据）
PlayerPrefs.SetString("playerData", json);
PlayerPrefs.Save();

// 读取
string json = PlayerPrefs.GetString("playerData");
PlayerData player = JsonUtility.FromJson<PlayerData>(json);
```

#### 11.1.5 游戏存档示例

```csharp
using Newtonsoft.Json;
using System.IO;

[System.Serializable]
public class GameSaveData
{
    public string playerName;
    public int level;
    public float playTime;
    public List<string> achievements;
    public Dictionary<string, int> playerStats;
}

public class SaveManager : MonoBehaviour
{
    public void SaveGame(string savePath)
    {
        GameSaveData data = new GameSaveData();
        data.playerName = "Hero";
        data.level = 15;
        data.playTime = 3600f;
        data.achievements = new List<string> { "FirstBlood", "Collector" };
        data.playerStats = new Dictionary<string, int>()
        {
            { "strength", 100 },
            { "intelligence", 80 }
        };

        // 序列化并保存
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(savePath, json);
    }

    public GameSaveData LoadGame(string savePath)
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            return JsonConvert.DeserializeObject<GameSaveData>(json);
        }
        return null;
    }
}
```

### 11.2 其他游戏引擎中的 JSON

> **零基础备注**：不同的游戏引擎，处理 JSON 的方式大同小异，关键是要把数据"读进来"和"写出去"。

#### 11.2.1 Unreal Engine

```cpp
// Unreal Engine 使用 FJsonObject
#include "JsonObject.h"
#include "JsonWriter.h"
#include "JsonReader.h"

// 创建 JSON
TSharedPtr<FJsonObject> JsonObject = MakeShareable(new FJsonObject());
JsonObject->SetStringField("name", "Player");
JsonObject->SetNumberField("level", 10);

// 序列化为字符串
FString OutputString;
TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory::Create(&OutputString);
FJsonSerializer::Serialize(JsonObject.ToSharedRef(), Writer);
Writer->Close();

// 反序列化
TSharedPtr<FJsonObject> JsonObject;
TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory::Create(JsonString);
FJsonSerializer::Serialize(Reader, JsonObject);

FString Name = JsonObject->GetStringField("name");
int32 Level = JsonObject->GetIntegerField("level");
```

#### 11.2.2 Godot

```gdscript
# Godot 使用 to_json() 和 parse_json()
var player = {
    "name": "Player",
    "level": 10,
    "hp": 100
}

# 编码为 JSON
var json_text = JSON.stringify(player)

# 从 JSON 解析
var result = JSON.parse(json_text)
var player_data = result.result
```

#### 11.2.3 Cocos2d-x

```cpp
// Cocos2d-x 使用 rapidjson
#include "rapidjson/document.h"
#include "rapidjson/writer.h"

using namespace rapidjson;

// 解析 JSON
const char* json = "{\"name\":\"Player\",\"level\":10}";
Document doc;
doc.Parse(json);

string name = doc["name"].GetString();
int level = doc["level"].GetInt();

// 创建 JSON
Document doc;
doc.SetObject();
doc.AddMember("name", Value("Player").Move(), doc.GetAllocator());
doc.AddMember("level", 10, doc.GetAllocator());

StringBuffer buffer;
Writer<StringBuffer> writer(buffer);
doc.Accept(writer);
const char* json = buffer.GetString();
```

---

## 十二、游戏开发常用数据存储格式

> **零基础理解**：游戏需要保存很多数据，比如角色信息、关卡数据、掉落物品等。不同的数据用不同的"本子"来记录。

### 12.0 为什么要用这么多种格式？

就像生活中：
- 写日记 → 用笔记本
- 记公式 → 用小卡片
- 列清单 → 用表格
- 存钱 → 用存折

游戏开发也是这个道理：
- 配置数据（物品属性）→ 用表格（CSV/XML）
- 玩家存档 → 用文件（JSON/二进制）
- 网络传输 → 用紧凑格式（JSON/Protobuf）

---

### 12.1 XML：老牌"表格"

#### 12.1.1 基本格式

```xml
<?xml version="1.0" encoding="UTF-8"?>
<gameData>
    <player name="Hero" level="10">
        <stats>
            <strength>100</strength>
            <agility>80</agility>
        </stats>
        <inventory>
            <item id="1" count="5"/>
            <item id="2" count="3"/>
        </inventory>
    </player>
    <enemies>
        <enemy type="slime" hp="50"/>
        <enemy type="dragon" hp="1000"/>
    </enemies>
</gameData>
```

**零基础理解**：XML 看起来像 HTML（网页的代码），用标签 `< >` 包裹内容，像这样：
```
<名字>内容</名字>
```
就像文件夹有"外表"（文件夹名）和"里面"（文件）。

**优缺点**：
| 优点 | 缺点 |
|------|------|
| 格式工整，易读 | 数据量大，传输慢 |
| 可以检查错误 | 写的规矩多，难学 |

#### 12.1.2 Unity 读取 XML

```csharp
using System.Xml.Linq;

// 加载 XML
XDocument doc = XDocument.Load(xmlPath);

// 查询数据
XElement player = doc.Root.Element("player");
string name = player.Attribute("name").Value;
int level = int.Parse(player.Attribute("level").Value);

// 修改并保存
player.Attribute("level").Value = "11";
doc.Save(xmlPath);
```

#### 12.1.3 游戏配置常用 XML

```xml
<!-- 物品配置示例 -->
<items>
    <item id="1001" name="生命药水" type="consumable">
        <effect type="heal" value="50"/>
        <price>100</price>
    </item>
    <item id="1002" name="魔法药水" type="consumable">
        <effect type="mp" value="30"/>
        <price>150</price>
    </item>
</items>
```

### 12.2 XML vs JSON vs YAML 对比：选哪个好？

| 特性 | XML | JSON | YAML |
|------|-----|------|------|
| 可读性 | 高 ★★ | 高 ★★ | 最高 ★★★ |
| 数据体积 | 大（记的多） | 中 | 小（记得少） |
| 解析速度 | 慢（复杂） | 快（简单） | 中 |
| 嵌套支持 | 好 ✓ | 好 ✓ | 好 ✓ |
| 注释支持 | ✓ | ✗ | ✓ |
| 游戏配置 | 常用 | 常用 | 较少 |

**选哪个？**

| 场景 | 推荐 |
|------|------|
| 网络传输 | JSON |
| 游戏配置（要给人看） | YAML |
| 老系统兼容 | XML |
| 简单数据 | JSON |

**通俗理解**：
- **JSON**：日常用，最方便
- **YAML**：写文档用，最好看
- **XML**：老设备用，最兼容

### 12.3 Protocol Buffers (protobuf)

#### 12.3.1 定义文件 (.proto)

```protobuf
syntax = "proto3";

package game;

// 玩家消息
message Player {
    string name = 1;
    int32 level = 2;
    int32 hp = 3;
    int32 mp = 4;
    repeated Item inventory = 5;
}

// 物品消息
message Item {
    int32 id = 1;
    string name = 2;
    int32 count = 3;
}

// 游戏数据
message GameData {
    repeated Player players = 1;
    int32 version = 2;
}
```

#### 12.3.2 C# 使用 protobuf

```csharp
using Google.Protobuf;

// 序列化
var player = new Player
{
    Name = "Hero",
    Level = 10,
    Hp = 100,
    Mp = 50
};

using (var stream = new FileStream("player.bin", FileMode.Create))
{
    player.WriteTo(stream);
}

// 反序列化
Player player;
using (var stream = new FileStream("player.bin", FileMode.Open))
{
    player = Player.Parser.ParseFrom(stream);
}
```

#### 12.3.3 protobuf 优势

| 优势 | 说明 |
|------|------|
| 高效 | 二进制格式，体积小 |
| 快速 | 解析速度比 JSON 快 5-10 倍 |
| 强类型 | 自动生成强类型代码 |
| 跨语言 | 支持多语言 |
| 版本兼容 | 支持字段增删 |

### 12.4 MessagePack

#### 12.4.1 特点

- 二进制 JSON 格式
- 比 JSON 更小更快
- 支持所有 JSON 类型

#### 12.4.2 C# 使用

```csharp
using MessagePack;

// 序列化
var player = new Player { Name = "Hero", Level = 10 };
var bytes = MessagePackSerializer.Serialize(player);

// 反序列化
var player = MessagePackSerializer.Deserialize<Player>(bytes);
```

### 12.5 CSV

#### 12.5.1 格式

```csv
id,name,hp,mp,attack
1001,战士,100,0,50
1002,法师,80,100,30
1003,刺客,90,50,40
```

#### 12.5.2 Unity 读取 CSV

```csharp
using System.Collections.Generic;
using UnityEngine;

public class CSVLoader : MonoBehaviour
{
    public TextAsset csvFile;

    void Start()
    {
        List<Dictionary<string, string>> data = ParseCSV(csvFile.text);
    }

    List<Dictionary<string, string>> ParseCSV(string csv)
    {
        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
        string[] lines = csv.Split('\n');
        
        if (lines.Length < 2) return result;

        string[] headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i])) continue;
            
            string[] values = lines[i].Split(',');
            Dictionary<string, string> row = new Dictionary<string, string>();
            
            for (int j = 0; j < headers.Length; j++)
            {
                row[headers[j]] = values[j];
            }
            
            result.Add(row);
        }

        return result;
    }
}
```

### 12.6 TOML

#### 12.6.1 格式

```toml
# 游戏配置
[graphics]
resolution_width = 1920
resolution_height = 1080
fullscreen = true
vsync = true

[audio]
bgm_volume = 0.5
sfx_volume = 0.8

[difficulty]
enemy_hp_multiplier = 1.5
experience_gain = 1.0
```

#### 12.6.2 C# 使用 TOML

```csharp
// 需要 NToml 库
using NToml;

// 解析配置
var config = Toml.ParseFile("config.toml");

// 读取值
int width = config["graphics"]["resolution_width"].AsInteger;
bool fullscreen = config["graphics"]["fullscreen"].AsBoolean;
```

### 12.7 INI 格式

#### 12.7.1 格式

```ini
[Player]
Name=Hero
Level=10
HP=100

[Graphics]
Resolution=1920x1080
Fullscreen=true

[Audio]
BGM=1.0
SFX=0.8
```

#### 12.7.2 C# 读取 INI

```csharp
using System.Runtime.InteropServices;
using System.Text;

public class INIFile
{
    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

    public static string Read(string filePath, string section, string key)
    {
        StringBuilder sb = new StringBuilder(255);
        GetPrivateProfileString(section, key, "", sb, 255, filePath);
        return sb.ToString();
    }

    public static void Write(string filePath, string section, string key, string value)
    {
        WritePrivateProfileString(section, key, value, filePath);
    }
}
```

---

## 十三、游戏数据存储方案选择

### 13.1 存储格式对比

| 场景 | 推荐格式 | 原因 |
|------|---------|------|
| 网络传输 | JSON/Protobuf | 体积小，跨平台 |
| 本地存档 | JSON/MessagePack | 易于调试，兼容性好 |
| 游戏配置 | XML/CSV/YAML | 易于编辑和维护 |
| 大量数据 | Protobuf/MessagePack | 高效二进制 |
| 简单配置 | INI/TOML | 简单易用 |

### 13.2 数据分类存储

```csharp
public enum DataStorageType
{
    // 配置数据 - XML/YAML（易于编辑）
    Config = 0,
    
    // 玩家存档 - JSON（二进制加密）
    SaveData = 1,
    
    // 网络数据 - Protobuf（小体积）
    NetworkData = 2,
    
    // 资源清单 - CSV（表格形式）
    AssetManifest = 3
}
```

### 13.3 安全存储

```csharp
using System.Security.Cryptography;
using System.Text;

public class SecureStorage
{
    // 简单加密（实际使用更复杂）
    public static string Encrypt(string data, string key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            aes.GenerateIV();
            
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                ms.Write(aes.IV, 0, 16);
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(data);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string Decrypt(string encryptedData, string key)
    {
        byte[] data = Convert.FromBase64String(encryptedData);
        
        using (Aes aes = Aes.Create())
        {
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            
            byte[] iv = new byte[16];
            Array.Copy(data, 0, iv, 0, 16);
            aes.IV = iv;
            
            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(data, 16, data.Length - 16))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
```

---

## 附录：快速查询表

### JSON 数据类型

| JSON 类型 | JS 类型 | Python 类型 |
|----------|---------|-------------|
| string | String | str |
| number | Number | int / float |
| true/false | Boolean | True / False |
| object | Object | dict |
| array | Array | list |
| null | null | None |

### 常用转换

| 操作 | JavaScript | Python |
|------|------------|---------|
| 解析 | JSON.parse() | json.loads() |
| 序列化 | JSON.stringify() | json.dumps() |

---

## 附录一：快速查询表

### JSON 数据类型

| JSON 类型 | JS 类型 | Python 类型 | 像什么 |
|----------|---------|------------|--------|
| string | String | str | 文字 |
| number | Number | int / float | 数字 |
| true/false | Boolean | True / False | √或× |
| object | Object | dict | 表格 |
| array | Array | list | 清单 |
| null | null | None | 空白 |

### 常用转换

| 操作 | JavaScript | Python |
|------|------------|---------|
| 解析 | JSON.parse() | json.loads() |
| 序列化 | JSON.stringify() | json.dumps() |

---

## 附录二：新手的常见问题

### Q1: JSON 和 JavaScript 是一回事吗？

**不是！**
- JavaScript 是编程语言
- JSON 是一种数据格式
- 就像：Java 是一种语言，但"表格"是一种格式

### Q2: 所有编程语言都能用 JSON 吗？

**是的！** 几乎所有主流语言都支持 JSON：
- JavaScript（原生支持）
- Python（用 json 模块）
- Java（用库）
- C#（用库）
- Go（用库）

### Q3: 学 JSON 难吗？

**不难！** 记住这几点就行：
1. 用 `{}` 表示对象（表格）
2. 用 `[]` 表示数组（清单）
3. 键要加引号
4. 末尾不要加逗号

### Q4: JSON 文件怎么打开？

直接用记事本就能打开，扩展名是 `.json`：
- 用 Notepad（记事本）
- 用 VS Code
- 用任何文本编辑器

### Q5: 什么时候用 JSON？

- **网页和服务器通信**
- **保存游戏存档**
- **保存配置信息**
- **存储结构化数据**

---

## 附录三：学习路线图

### 第一步：会读会写
- [ ] 认识六种数据类型
- [ ] 能写出正确的 JSON
- [ ] 能看出 JSON 错误

### 第二步：会用工具
- [ ] 用 JavaScript 解析 JSON
- [ ] 用 JavaScript 生成 JSON
- [ ] 用 Python 处理 JSON

### 第三步：会应用
- [ ] 理解 API 中的 JSON
- [ ] 会用 JSON 保存数据
- [ ] 理解 Schema 验证

### 第四步：会优化
- [ ] 理解数据安全
- [ ] 知道什么时候用什么格式
- [ ] 能处理特殊情况

---

**文档说明**：本指南涵盖 JSON 的核心知识点，从零基础入门到游戏开发应用都有讲解。新手建议从第一章开始，按顺序学习。

**完成后**：你应该能够：
1. ✓ 读懂任何 JSON
2. ✓ 写出正确的 JSON
3. ✓ 在代码中使用 JSON
4. ✓ 在游戏开发中应用 JSON
5. ✓ 选择合适的数据存储格式

加油！JSON 学起来很简单，用处很大！