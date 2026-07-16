# C++ 经典书籍学习资料库

> 本资料库收录了 C++ 学习的经典书籍精华，每本都包含核心概念、代码示例和难点解析。
> 
> 建议配合原书阅读，遇到不理解的地方来这里找详细解释。

---

## 📚 书籍列表

### 入门阶段
| 书籍 | 文件 | 适合阶段 |
|------|------|----------|
| **C++ Primer (第5版)** | [C++Primer.md](C++Primer.md) | 初学者必读，全面覆盖 C++11 |

### 进阶阶段
| 书籍 | 文件 | 适合阶段 |
|------|------|----------|
| **Effective C++ (第3版)** | [EffectiveCpp.md](EffectiveCpp.md) | 有基础后必读，55条实战建议 |
| **More Effective C++** | [MoreEffectiveCpp.md](MoreEffectiveCpp.md) | Effective 的续作，35条进阶建议 |
| **Effective Modern C++** | [EffectiveModernCpp.md](EffectiveModernCpp.md) | 现代 C++11/14 实战指南 |

### 深入理解
| 书籍 | 文件 | 适合阶段 |
|------|------|----------|
| **C++ Concurrency in Action** | [C++Concurrency.md](C++Concurrency.md) | 多线程编程圣经 |
| **C++ Templates: The Complete Guide** | [C++Templates.md](C++Templates.md) | 模板编程百科全书 |
| **深度探索C++对象模型** | [深度探索C++对象模型.md](深度探索C++对象模型.md) | 理解 C++ 底层机制 |
| **Exceptional C++ 系列** | [ExceptionalCpp.md](ExceptionalCpp.md) | 问题驱动的深入学习 |

### 现代 C++
| 书籍 | 文件 | 适合阶段 |
|------|------|----------|
| **C++17/20 Complete Guide** | [C++CompleteGuide.md](C++CompleteGuide.md) | C++17/20 新特性详解 |

---

## 🎯 阅读建议

### 阅读顺序
```
C++ Primer (入门)
    ↓
Effective C++ → More Effective C++ → Effective Modern C++ (进阶)
    ↓
C++ Concurrency / C++ Templates / 深度探索C++对象模型 (深入)
    ↓
C++17/20 Complete Guide (现代 C++)
```

### 如何使用本资料库
1. **学习新知识时**：先看对应书籍文件，理解核心概念
2. **遇到难点时**：搜索"## 难点解析"章节
3. **想要实践时**：参考代码示例，配合 Raylib 项目练习
4. **理解 Unity 底层时**：查找"## Unity 对照"章节

### 难点标记说明
- 🔴 **红色警告**：常见错误，必须理解
- ⚠️ **黄色注意**：容易混淆的概念
- 💡 **蓝色提示**：实用技巧和最佳实践
- 🔗 **Unity 对照**：与 Unity 引擎的对应关系

---

## 📖 书籍详细说明

### C++ Primer (第5版)
- **作者**：Stanley B. Lippman, Josee Lajoie, Barbara E. Moo
- **特点**：最全面的入门书，覆盖 C++11，讲解细致
- **重点章节**：智能指针、移动语义、Lambda 表达式、模板基础

### Effective C++ (第3版)
- **作者**：Scott Meyers
- **特点**：55条改善程序设计的建议，实战性强
- **重点章节**：资源管理、构造/析构/赋值、继承与面向对象设计

### Effective Modern C++
- **作者**：Scott Meyers
- **特点**：针对 C++11/14 的42条建议
- **重点章节**：智能指针、移动语义、Lambda、并发编程

### C++ Concurrency in Action
- **作者**：Anthony Williams
- **特点**：多线程编程的权威指南
- **重点章节**：线程管理、同步、内存模型、无锁编程

### C++ Templates: The Complete Guide (第2版)
- **作者**：David Vandevoorde, Nicolai M. Josuttis, Douglas Gregor
- **特点**：模板编程的百科全书
- **重点章节**：模板基础、特化、SFINAE、C++17/20 模板新特性

### 深度探索C++对象模型
- **作者**：Stanley B. Lippman
- **特点**：揭示 C++ 底层实现机制
- **重点章节**：对象模型、构造函数、虚函数、多重继承

### C++17/20 Complete Guide
- **作者**：Nicolai M. Josuttis
- **特点**：逐条讲解 C++17/20 新特性
- **重点章节**：std::optional、std::variant、std::filesystem、Concepts、Coroutines

---

*最后更新：2026-07-16*
