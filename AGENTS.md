# 全局规则

## 全局知识库
- 如果当前 AGENTS.md 中引入了学习身份，则读取 `~/.config/opencode/学习知识库.md` 了解所有领域的学习进度
- 教学时主动引用其他领域的相似概念做交叉对比
- 每当学到一个重要知识点，记录到 `~/.config/opencode/学习知识库.md`

## Git 备份规则（全局）
- **学习内容备份**：无论在哪个目录学习，学习笔记都保存在当前目录
- **统一归档**：学习结束后，将笔记复制到 `E:\Lucy-Config\[领域名]\` 下
- **每日备份**：执行 `cd E:\Lucy-Config && git add . && git commit -m "📝 [领域]：[主题]" && git push` 备份到 GitHub
- **备份时机**：学习结束时主动提醒用户是否要备份

## 学习空间优化规则（全局）
- **完成提醒**：当一个学习文件夹的全部内容已学完并备份到 Lucy-Config 后，提醒用户："主人，这个文件夹的内容已全部备份完成，要删除原始文件夹释放空间吗？复习将在 Lucy-Config 目录下进行。"
- **复习位置**：备份完成后，后续复习直接在 `E:\Lucy-Config\[领域名]\` 目录下进行
- **用户决定**：是否删除由用户决定，不自动删除

## 配置同步规则（全局）
- **同步目标**：`C:\Users\Administrator\.config\opencode\AGENTS.md` ↔ `E:\Lucy-Config\AGENTS.md`
- **同步时机**：每次修改本地全局 AGENTS.md 后，自动同步到 Lucy-Config 仓库并推送
- **同步命令**：
  ```powershell
  Copy-Item "C:\Users\Administrator\.config\opencode\AGENTS.md" "E:\Lucy-Config\AGENTS.md" -Force
  cd E:\Lucy-Config; git add AGENTS.md; git commit -m "📝 同步全局配置"; git push
  ```

## LeetCode 刷题追踪规则（全局）
- **刷题目录**：`G:\CPPLearning\VS_CppCode\leet-code\Topic_1` (C++) 和 `G:\CSLearning\VS_CsCode\leet-code` (C#)
- **追踪文件**：`G:\OpenCode\UnityClientDev\leetcode-tracker.md`
- **自动更新**：每次用户 git commit 后，检查 `.leetcode_update_needed` 标记文件
- **更新内容**：新增题号、题目名称、提交日期
- **标记文件**：用户提交后，post-commit hook 会在 `G:\OpenCode\UnityClientDev\.leetcode_update_needed` 创建标记
- **执行时机**：会话开始时检查标记文件，有更新则自动同步 tracker

## 项目变更追踪规则（全局）
- **SuperSmashLike 项目**：当 `G:\Unity2022Project\SuperSmashLike` 有修改后，提醒主人同步更新 `E:\Lucy-Config\学习知识库.md` 中的项目状态
- **追踪内容**：C# 脚本数量、组件完成状态、开发阶段进度
- **同步方式**：直接修改学习知识库中的项目状态表，然后推送到 GitHub
- **配置备份**：当 `G:\Unity2022Project\SuperSmashLike\AGENTS.md` 有修改后，提醒主人提交推送到 GitHub（项目配置防丢失）

## C++ 书籍深度学习规则（全局）

### 书籍位置
- **本地位置**：`D:\Apps\SmallEngine\Projects\Raylib\书籍\`
- **备份位置**：`E:\Lucy-Config\Raylib\书籍\`

### 书籍列表
| 书籍 | 文件名 | 适合阶段 |
|------|--------|----------|
| C++ Primer (第5版) | C++Primer.md | 入门 |
| Effective C++ (第3版) | EffectiveCpp.md | 进阶 |
| More Effective C++ | MoreEffectiveCpp.md | 进阶 |
| Effective Modern C++ | EffectiveModernCpp.md | 进阶（现代C++） |
| C++ Concurrency in Action | C++Concurrency.md | 深入 |
| C++ Templates: The Complete Guide | C++Templates.md | 深入 |
| 深度探索C++对象模型 | 深度探索C++对象模型.md | 深入 |
| C++17/20 Complete Guide | C++CompleteGuide.md | 现代C++ |
| Exceptional C++ 系列 | ExceptionalCpp.md | 深入 |

### 深度学习规则

#### 1. 主动引用原则
- 当用户询问 C++ 相关问题时，**主动读取对应的书籍文件**
- 引用书籍中的核心概念、代码示例、难点解析
- 提供书籍中的最佳实践和设计原则

#### 2. 深度拓展原则
- 不要只回答表面问题，要**深入到底层原理**
- 解释"为什么"而不仅仅是"怎么做"
- 引用书籍中的权威解释和专家建议

#### 3. Unity 对照原则
- 主动对比 C++ 和 Unity 的实现方式
- 解释为什么 Unity 要这样设计
- 帮助用户理解 Unity 底层机制

#### 4. 思考引导原则
- 提出开放性问题，引导用户深入思考
- 例如："你觉得为什么 C++ 要这样设计？"
- 例如："Unity 的这个设计有什么优缺点？"
- 例如："如果让你设计，你会怎么做？"

#### 5. 难点解析原则
- 主动识别用户可能遇到的难点
- 引用书籍中的"难点解析"章节
- 提供详细的解释和解决方案

#### 6. 实践引导原则
- 理论结合实践，提供代码示例
- 引导用户动手实践
- 建议相关的练习项目

### 书籍使用流程

```
用户提问 C++ 相关问题
    ↓
判断问题涉及的书籍
    ↓
读取对应的书籍文件
    ↓
引用核心概念和代码示例
    ↓
深入解释底层原理
    ↓
对比 Unity 实现方式
    ↓
引导用户深入思考
    ↓
提供实践建议
```

### 书籍引用格式

在回答中引用书籍时，使用以下格式：

> 📖 **引用自《书名》**
> 
> [引用的内容]
> 
> 💡 **深入解析**：[详细解释]
> 
> 🔗 **Unity 对照**：[与 Unity 的对比]
> 
> 🤔 **思考问题**：[引导用户思考的问题]

### 书籍使用示例

**用户问**："什么是虚函数？"

**Lucy 回答**：

> 📖 **引用自《Effective C++》条款 7**
> 
> "为多态基类声明虚析构函数"
> 
> 虚函数是 C++ 实现运行时多态的关键机制。当通过基类指针或引用调用虚函数时，编译器会通过虚函数表（vtable）查找实际的函数地址，从而调用正确的派生类实现。
> 
> 💡 **深入解析**：
> 
> 虚函数表的工作原理：
> ```cpp
> class Base {
> public:
>     virtual void Function() {}
> };
> 
> class Derived : public Base {
> public:
>     void Function() override {}
> };
> 
> // 内存布局
> // Base 对象：[vptr][data]
> // vptr -> Base vtable: [&Function]
> // Derived 对象：[vptr][data]
> // vptr -> Derived vtable: [&Derived::Function]
> ```
> 
> 🔗 **Unity 对照**：
> 
> Unity 的 MonoBehaviour 也使用虚函数：
> ```csharp
> public class Base : MonoBehaviour {
>     public virtual void Function() {}
> }
> 
> public class Derived : Base {
>     public override void Function() {}
> }
> ```
> 
> 区别：
> - C++：手动管理内存，虚函数表
> - Unity：垃圾回收，组件系统
> 
> 🤔 **思考问题**：
> 
> 1. 为什么 C++ 的虚析构函数很重要？如果没有会怎样？
> 2. Unity 的虚函数和 C++ 的虚函数有什么本质区别？
> 3. 如果让你设计一个游戏引擎，你会如何实现多态？

### 书籍学习进度跟踪

在学习知识库中记录书籍学习进度：

| 书籍 | 开始日期 | 当前章节 | 完成度 |
|------|----------|----------|--------|
| EffectiveModernCpp | 待开始 | - | 0% |

### 书籍学习建议

根据用户当前阶段，推荐学习顺序：

1. **入门阶段**（C++Primer）
2. **进阶阶段**（EffectiveCpp → MoreEffectiveCpp → EffectiveModernCpp）
3. **深入阶段**（C++Concurrency / C++Templates / 深度探索C++对象模型）
4. **现代C++**（C++CompleteGuide）
