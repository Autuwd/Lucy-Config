# 遗忘曲线复习计划 — Day01

> 艾宾浩斯遗忘曲线间隔：1天 → 7天 → 16天 → 30天
> 每次复习以提问开始，答不出的用费曼学习法查漏补缺

---

## 复习时间表

| 复习节点 | 日期 | 状态 |
|---------|------|:----:|
| 🔄 第1次（1天后） | 2026-07-17 | ⏳ |
| 🔄 第2次（7天后） | 2026-07-23 | ⏳ |
| 🔄 第3次（16天后） | 2026-08-01 | ⏳ |
| 🔄 第4次（30天后） | 2026-08-15 | ⏳ |

---

## Day01 知识点速查

### 1️⃣ C++ 编译流程
```
.cpp → g++ 编译 → .exe（机器码）
```
编译三阶段：**预处理**（#include 展开）→ **编译**（.cpp→.obj）→ **链接**（.obj→.exe）

### 2️⃣ 游戏循环 = Unity Update()
```cpp
while (!WindowShouldClose()) {  // 每帧执行 = Update()
    BeginDrawing();
    ClearBackground(color);     // 清屏
    // 各种绘制...
    EndDrawing();               // 提交到 GPU
}
CloseWindow();
```
**关键理解**：Unity 的 Update() 内部就是一个类似的 while 循环。

### 3️⃣ C++ vs C# 编译方式
| | C++ | C# |
|---|---|---|
| 编译结果 | 机器码 (.exe) | IL 中间码 |
| 运行时 | 直接运行 | CLR 用 JIT 翻译 |
| 改代码 | 关程序→重编译→运行 | 可热替换（编辑器内） |
| 代表 | AOT（提前编译） | JIT（即时编译） |

### 4️⃣ CLR = C# 的虚拟运行环境
- **JIT 编译**：IL → 机器码
- **GC 垃圾回收**：自动管理内存（C++ 要手动 new/delete）
- **IL2CPP**：Unity 打包时 IL → C++ → 各平台编译，获得 C++ 级性能

### 5️⃣ MonoBehaviour 生命周期
```
Awake → OnEnable → Start → Update → LateUpdate → 渲染 → OnDisable → OnDestroy
```
本质就是 Unity 把 while 循环体拆成多个时间点，让你能在不同阶段做不同事。

---

## 复习提问（下次会话用）

**Q1：** C++ 编译出来的是什么？C# 编译出来的是什么？
**Q2：** `while(!WindowShouldClose())` 对应 Unity 的什么？
**Q3：** 什么是 JIT？什么是 AOT？各有什么优缺点？
**Q4：** CLR 是什么？它做了哪几件事？
**Q5：** MonoBehaviour 的生命周期顺序是什么？
**Q6：** `BeginDrawing()/EndDrawing()` 在 Unity 里对应什么？
