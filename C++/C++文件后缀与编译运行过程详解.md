# C++ 文件后缀与编译运行过程详解

---

## 一、文件后缀的含义

### 按用途分两类

| 后缀 | 类别 | 作用 | 是否参与编译 |
|------|------|------|-------------|
| `.h` | 头文件 | 声明（告诉编译器"有什么"） | ❌ 不编译，被 .cpp 引用 |
| `.hpp` | 头文件 | 同上，C++专用头文件 | ❌ 不编译 |
| `.cpp` | 源文件 | 实现（具体写"怎么做"） | ✅ 编译 |
| `.c` | 源文件 | C 语言源文件 | ✅ 用 gcc 编译 |
| `.hxx` / `.hh` | 头文件 | 变种写法，功能同 `.h` | ❌ |
| `.cc` / `.cxx` | 源文件 | 变种写法，功能同 `.cpp` | ✅ |
| `.o` | 目标文件 | 编译产物（机器码） | ❌ 已是二进制 |
| `.exe` | 可执行文件 | 最终产物 | ❌ 已是成品 |

### 为什么要有 .h 和 .cpp 的分离？

**一句话：声明与实现分离。**

假设多文件项目中有 `A.cpp` 想调用 `B.cpp` 里的函数 `foo()`：

```
B.cpp 写了：  void foo() { ... }
A.cpp 想用：  foo();          ← 编译器此时还不知道 foo 长什么样
```

编译器编译 `A.cpp` 时，只看 `A.cpp` 这个文件，不知道 `B.cpp` 里有个 `foo()`，所以会报错"找不到 foo"。

**解决方案：** 把 `foo()` 的声明放在 `B.h` 里：

```
B.h 声明：     void foo();          ← 告诉编译器："存在一个 foo 函数"
B.cpp 实现：   void foo() { ... }   ← 具体代码
A.cpp 引用：   #include "B.h"       ← 把声明拿过来
               foo();               ← 现在编译器知道 foo 存在了
```

### #pragma once 的作用

```cpp
// B.h
#pragma once        // ← 防止同一个头文件被多次包含
void foo();
```

如果 `A.cpp` 同时 `#include "B.h"` 和 `#include "C.h"`，而 `C.h` 也包含了 `B.h`，没有 `#pragma once` 会导致 `foo()` 被声明两次 → 编译错误。

### .h 和 .hpp 的区别

| | .h | .hpp |
|---|---|---|
| 惯用场合 | C 和 C++ 通用 | 仅限 C++ |
| 示例 | `#include "raylib.h"`（C 库） | 纯 C++ 项目偏好 |
| 内容 | 函数声明、结构体 | 还可以放类定义、模板、inline 函数 |

在实际项目中，两者可以混用，没有功能区别。

---

## 二、从源码到可执行文件的完整过程

```
源码(.cpp) 
    ↓
[1. 预处理]  ─→ 处理 #include、#define、宏替换
    ↓
[2. 编译]    ─→ 将 C++ 代码翻译成汇编代码
    ↓
[3. 汇编]    ─→ 将汇编代码翻译成机器码（.o 目标文件）
    ↓
[4. 链接]    ─→ 合并所有 .o + 库文件 → .exe
    ↓
可执行文件(.exe)
    ↓
[5. 加载运行] ─→ 操作系统加载到内存执行
```

### 第 1 步：预处理（Preprocessing）

**处理所有以 `#` 开头的指令。**

```
main.cpp 里有：
  #include "Ball.h"
  #define SCREEN_W 800
  
预处理后，main.cpp 变成（临时文件，看不到）：
  // Ball.h 的全部内容被粘贴进来...
  // Constants.h 的内容也被粘贴进来...
  // raylib.h 的数千行代码也被粘贴进来...
  int main() {
      InitWindow(800, 600, ...);  // SCREEN_W 被替换为 800
  }
```

**关键操作：**
- `#include "xxx.h"` → 把 xxx.h 的**全部内容**插入到这个位置（可以理解为"复制粘贴"）
- `#define A B` → 把所有 A 替换成 B
- `#pragma once` → 阻止同一个头文件被反复插入
- 删除注释

### 第 2 步：编译（Compilation）

**将预处理后的 C++ 代码翻译成汇编语言。**

```
// 你写的 C++：
if (x > 0) { y = y + 1; }

// 编译后变成汇编（示意）：
//    cmp  x, 0
//    jle  .L1
//    add  y, 1
//  .L1:
```

**做的事情：**
- 语法检查（少分号、少括号——这里报错）
- 类型检查（传错参数类型——这里报错）
- 生成汇编代码

**这个步骤是按 .cpp 文件独立进行的。** 编译 `main.cpp` 时看不到 `Ball.cpp` 的内容。

### 第 3 步：汇编（Assembly）

**把汇编代码转换成机器码（二进制），生成 `.o` 文件。**

```
main.cpp → 编译 + 汇编 → main.o   （二进制文件，用记事本打开是乱码）
Ball.cpp → 编译 + 汇编 → Ball.o
```

`.o` 文件是**半成品**。比如 `main.o` 知道要调用 `Ball::Launch()` 这个函数，但它此时还不知道这个函数的具体地址——它留了一个"空位"给链接器去填。

### 第 4 步：链接（Linking）

**把所有的 `.o` 文件和库文件合在一起，填好所有地址，生成 `.exe`。**

```
g++ main.o Ball.o Paddle.o ... -o game.exe -lraylib -lopengl32 ...
                     ↓
把所有 .o 合并，修正函数地址
                     ↓
              game.exe（成品）
```

**链接时做的事情：**
- 把多个 `.o` 拼接在一起
- 把 `main.o` 中调用的函数地址（如 `Ball::Launch`）指向 `Ball.o` 中该函数的具体位置
- 链接外部库（`-lraylib` 表示链接 raylib 库）

**常见的链接错误：**
```
undefined reference to 'Ball::Launch()'   ← 忘了编译 Ball.cpp，或忘了在链接时加入 Ball.o
multiple definition of 'foo()'            ← 同一个函数在多个 .cpp 里定义了
```

### 第 5 步：加载运行

```
用户双击 game.exe
     ↓
操作系统把 .exe 加载到内存
     ↓
调用 main() 开始执行
     ↓
运行到 InitWindow → 调用 raylib.dll 里的代码
运行到 CloseWindow → 程序结束
```

---

## 三、你将看到的文件类型（Raylib 项目）

### 你写的

| 文件 | 内容 |
|------|------|
| `main.cpp` | `int main()` 入口 |
| `Ball.h` | Ball 类的声明 |
| `Ball.cpp` | Ball 类的具体函数实现 |
| `Constants.h` | 常量定义 |

### 编译过程中自动产生的（可删除）

| 文件 | 内容 |
|------|------|
| `main.o` | main.cpp 编译后的目标文件 |
| `game.exe` | 最终可执行文件 |

### 系统提供的（在外部）

| 文件 | 位置 | 作用 |
|------|------|------|
| `raylib.h` | `C:\raylib\raylib\src\` | 声明了所有 raylib 函数 |
| `libraylib.a` | `C:\raylib\raylib\src\` | raylib 的预编译库（包含 InitWindow、DrawText 等函数的机器码） |

---

## 四、我们的 compile.bat 实际上做了什么

```
..\compile.bat main.cpp Ball.cpp Paddle.cpp
```

等价于手动执行（compile.bat 里已经封装好了）：

```bash
# 1+2+3：预处理 + 编译 + 汇编 → 生成 .o
g++ -c main.cpp -IC:/raylib/raylib/src -std=c++17 -O2 -Wall
g++ -c Ball.cpp -IC:/raylib/raylib/src -std=c++17 -O2 -Wall
g++ -c Paddle.cpp -IC:/raylib/raylib/src -std=c++17 -O2 -Wall

# 4：链接 → 生成 .exe
g++ main.o Ball.o Paddle.o -o game.exe ^
    -LC:/raylib/raylib/src -lraylib -lopengl32 -lgdi32 -lwinmm -mwindows

# 5：运行
./game.exe
```

不过 compile.bat 为了简便，把 1-4 步合写成了一行命令（`g++ main.cpp Ball.cpp ... -o game.exe`），编译器内部还是会按"编译每个 .cpp → 链接"的顺序处理，只是你不需要手动分步执行。

---

## 五、常见问题的本质原因

### "undefined reference"（未定义引用）

```
编译 ✅  → 声明找到了（.h 已包含）
链接 ❌  → 实现没找到（忘加 .cpp 或忘链接库）
```

**例子：** 只编译了 `main.cpp`，但 `main.cpp` 调用了 `Ball::Launch()`，而 `Ball.cpp` 没参与编译 → 链接器找不到 `Ball::Launch` 的实现 → 报错。

### "not declared in this scope"（未声明）

```
编译 ❌  → 声明没找到（忘了 #include 对应的 .h）
```

**例子：** 在 `main.cpp` 里直接写了 `Ball ball;` 但没 `#include "Ball.h"` → 编译器不知道 Ball 是什么。

### "multiple definition"（重复定义）

```
链接 ❌  → 同一个函数在多个 .o 中都有实现
```

**例子：** 把函数实现写在了 `.h` 文件中，并且这个 `.h` 被多个 `.cpp` 包含 → 每个 `.cpp` 编译后都有一份这个函数的代码 → 链接时冲突。

---

## 六、你项目中各文件的关系图

```
07_BreakoutGame/

Constants.h ─────────────────────┐  (纯声明，不编译)
                                │
AudioUtils.h ──→ AudioUtils.cpp  ├──→ 编译 → AudioUtils.o
                                │
Ball.h ────────→ Ball.cpp        ├──→ 编译 → Ball.o
                                │
Paddle.h ──────→ Paddle.cpp      ├──→ 编译 → Paddle.o
                                │
Brick.h ───────→ Brick.cpp       ├──→ 编译 → Brick.o
                                │
BreakoutGame.h ─→ BreakoutGame.cpp ─→ 编译 → BreakoutGame.o
                                │
main.cpp ────────────────────────┘──→ 编译 → main.o
                                            ↓
                                    链接器 → game.exe
```

箭头 `→` 表示"包含"：比如 `AudioUtils.cpp` 第一行 `#include "AudioUtils.h"`，编译时会把 `AudioUtils.h` 的内容复制进来。
