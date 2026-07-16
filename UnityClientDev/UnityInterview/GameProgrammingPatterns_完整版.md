# Game Programming Patterns — 游戏编程模式 完整版

> 本文件是 [Game Programming Patterns](https://gameprogrammingpatterns.com/)（Robert Nystrom 著）的完整中英双语学习笔记。
> 包含：全部 19 个模式的**原书逐字内容**（英文 + 中文翻译）+ 原书 C++ 代码 + C# 对照实现 + Unity 应用场景。
> 原始文件来源：https://gameprogrammingpatterns.com/
---

# Game Programming Patterns — 游戏编程模式

> 本系列文档是对 [Game Programming Patterns](https://gameprogrammingpatterns.com/)（Robert Nystrom 著）的中英双语学习笔记。
> 每篇包含：模式概念（中英对照）、原书 C++ 代码、C# 对照实现（含详细注释）、Unity 应用场景。

---

## 目录

### 设计模式再探 (Design Patterns Revisited)
| # | 模式 | 英文 | 核心用途 |
|---|------|------|----------|
| 1 | [命令模式](Pattern_01_Command.md) | Command | 将请求封装为对象，支持撤销/重做/队列 |
| 2 | [享元模式](Pattern_02_Flyweight.md) | Flyweight | 共享细粒度对象，减少内存占用 |
| 3 | [观察者模式](Pattern_03_Observer.md) | Observer | 一对多通知，解耦事件源和监听器 |
| 4 | [原型模式](Pattern_04_Prototype.md) | Prototype | 通过克隆创建对象，避免子类爆炸 |
| 5 | [单例模式](Pattern_05_Singleton.md) | Singleton | 全局唯一实例访问点 |
| 6 | [状态模式](Pattern_06_State.md) | State | 用对象表示状态，消除 if/switch 分支 |

### 序列模式 (Sequencing Patterns)
| # | 模式 | 英文 | 核心用途 |
|---|------|------|----------|
| 7 | [双缓冲模式](Pattern_07_DoubleBuffer.md) | Double Buffer | 用两个缓冲区交替读写，避免数据竞争 |
| 8 | [游戏循环模式](Pattern_08_GameLoop.md) | Game Loop | 解耦游戏时间推进与输入/帧率 |
| 9 | [更新方法模式](Pattern_09_UpdateMethod.md) | Update Method | 每帧更新所有游戏对象的统一接口 |

### 行为模式 (Behavioral Patterns)
| # | 模式 | 英文 | 核心用途 |
|---|------|------|----------|
| 10 | [字节码模式](Pattern_10_Bytecode.md) | Bytecode | 将行为编码为指令序列，支持热更新 |
| 11 | [子类沙箱模式](Pattern_11_SubclassSandbox.md) | Subclass Sandbox | 基类定义保护方法，子类组合行为 |
| 12 | [类型对象模式](Pattern_12_TypeObject.md) | Type Object | 用对象表示类型，运行时动态定义新类型 |

### 解耦模式 (Decoupling Patterns)
| # | 模式 | 英文 | 核心用途 |
|---|------|------|----------|
| 13 | [组件模式](Pattern_13_Component.md) | Component | 用组合替代继承，灵活组装对象行为 |
| 14 | [事件队列模式](Pattern_14_EventQueue.md) | Event Queue | 异步解耦事件生产和消费 |
| 15 | [服务定位模式](Pattern_15_ServiceLocator.md) | Service Locator | 全局服务访问点，解耦服务使用者与实现 |

### 优化模式 (Optimization Patterns)
| # | 模式 | 英文 | 核心用途 |
|---|------|------|----------|
| 16 | [数据局部性模式](Pattern_16_DataLocality.md) | Data Locality | 利用 CPU 缓存，按访问模式组织内存 |
| 17 | [脏标志模式](Pattern_17_DirtyFlag.md) | Dirty Flag | 标记状态变化，避免重复计算 |
| 18 | [对象池模式](Pattern_18_ObjectPool.md) | Object Pool | 复用对象，减少分配/回收开销 |
| 19 | [空间分区模式](Pattern_19_SpatialPartition.md) | Spatial Partition | 按空间组织对象，加速碰撞/查询 |

---

## 关于本书

### Architecture, Performance, and Games (架构、性能与游戏)

* Game programming sits at the intersection of software engineering and real-time performance. Unlike business applications where code clarity is the primary concern, game code must be both clean *and* fast. Design patterns help manage complexity, but performance patterns help manage hardware constraints.

* 游戏编程位于软件工程和实时性能的交汇点。与业务应用不同（代码清晰是首要关注），游戏代码必须**既清晰又快速**。设计模式帮助管理复杂性，而优化模式帮助管理硬件约束。

**Key insight / 关键洞察：**
- Patterns like Command and Observer make code flexible and maintainable（让代码灵活可维护）
- Patterns like Data Locality and Object Pool make code fast（让代码高性能）
- The best game code uses both kinds wisely（最佳游戏代码明智地结合两者）

---

## C# / C++ 对照学习说明

原书代码使用 **C++** 编写。本系列在每个模式中提供：

| 语言 | 用途 |
|------|------|
| **C++** | 原书代码（保持原样，便于对照原书理解） |
| **C#** | 对照实现，添加详细中文注释 |
| **Unity** | 每个模式附 Unity 实际应用场景 |

### 代码风格约定

```csharp
// 原书 C++ 代码（保持原样）
// Original C++ code from the book

class Command {
public:
    virtual ~Command() {}
    virtual void execute() = 0;  // 纯虚函数 = C# 的 abstract method
};

// ─── C# 对照实现 ─────────────────────────────────────────
// C# equivalent with detailed Chinese comments

public abstract class Command
{
    public abstract void Execute();  // 抽象方法，等价于 C++ 的纯虚函数
    // C# 与 C++ 区别：C# 使用 abstract 关键字，C++ 使用 = 0
    // C# 的接口是干净的抽象，C++ 的纯虚类可包含成员变量
}
```

---

## 学习建议

1. **先读原理解释** — 理解模式的 intent（意图）和 motivation（动机）
2. **对比 C++ / C# 代码** — 注意两种语言在语法和惯用法上的差异
3. **思考 Unity 场景** — 每个模式在 Unity 中都有对应实现（MonoBehaviour、Coroutine、Job System 等）
4. **动手改写** — 将 C++ 代码手动改写为 C#，加深理解

> 下一章：[命令模式 (Command)](Pattern_01_Command.md)

---

# Command Pattern — 命令模式

> **EN:** Encapsulate a request as an object, thereby letting users parameterize clients with different requests, queue or log requests, and support undoable operations.
> **CN:** 将请求封装为对象，从而允许用户用不同请求对客户端进行参数化、排队或记录请求，以及支持可撤销操作。

---

## Intent / 意图

Command is one of my favorite patterns. Most large programs I write, games or otherwise, end up using it somewhere. When I've used it in the right place, it's neatly untangled some really gnarly code. For such a swell pattern, the Gang of Four has a predictably abstruse description:

命令是我最喜欢的模式之一。我编写的大多数大型程序——无论是游戏还是其他类型的软件——最终都会在某个地方用到它。当我在正确的位置使用它时，它总能优雅地解开一些极其棘手的代码。对于这样一个出色的模式，GoF 四人组给出了一个意料之中晦涩难懂的定义：

> Encapsulate a request as an object, thereby letting users parameterize clients with different requests, queue or log requests, and support undoable operations.

> "将一个请求封装为一个对象，从而让你可以用不同的请求对客户端进行参数化、对请求进行排队或记录日志，以及支持可撤销操作。"

I think we can all agree that that's a terrible sentence. First of all, it mangles whatever metaphor it's trying to establish. Outside of the weird world of software where words can mean anything, a "client" is a *person* — someone you do business with. Last I checked, human beings can't be "parameterized".

我想我们都同意这是一个糟糕的句子。首先，它搞混了它试图建立的隐喻。在软件这个怪异的、词义可以随意定义的世界之外，"客户端"指的是"人"——你与之做生意的人。据我所知，人是不能被"参数化"的。

Then, the rest of that sentence is just a list of stuff you could maybe possibly use the pattern for. Not very illuminating unless your use case happens to be in that list. *My* pithy tagline for the Command pattern is:

其次，这个句子的其余部分只是列出了你可能在某些情况下会用到的功能列表。除非你的用例恰好在这个列表中，否则它并不能提供什么启发。我对命令模式的简洁概括是：

**A command is a *reified method call*.**

**"命令是一个被'物化'（reified）的方法调用。"**

"Reify" comes from the Latin "res", for "thing", with the English suffix "–fy". So it basically means "thingify", which, honestly, would be a more fun word to use.

"Reify"来源于拉丁语的"res"（意为"事物"）加上英语后缀"-fy"。所以它基本上就是"thingify"（使之成为事物）的意思——老实说，用"thingify"这个词会更有趣。

Of course, "pithy" often means "impenetrably terse", so this may not be much of an improvement. Let me unpack that a bit. "Reify", in case you've never heard it, means "make real". Another term for reifying is making something "first-class".

当然，"精辟"往往也意味着"难以理解的简洁"——所以这可能也算不上什么改进。让我稍微展开解释一下。"Reify"（如果你以前没听说过的话）的意思是"使其变得真实"。与之相关的另一个术语是使其成为"一等公民"（first-class）。

*Reflection systems* in some languages let you work with the types in your program imperatively at runtime. You can get an object that represents the class of some other object, and you can play with that to see what the type can do. In other words, reflection is a *reified type system*.

某些语言中的*反射系统*让你可以在运行时以命令式的方式操作程序中的类型。你可以获取一个代表某个类的对象，然后操作它来查看该类型的能力。换句话说，反射就是一个*被物化的类型系统*。

Both terms mean taking some *concept* and turning it into a piece of *data* — an object — that you can stick in a variable, pass to a function, etc. So by saying the Command pattern is a "reified method call", what I mean is that it's a method call wrapped in an object.

这两个术语的意思都是将某个*概念*转化为一段*数据*——一个对象——然后你可以将其存入变量、传递给函数等。所以，当我说命令模式是一个"被物化的方法调用"时，我的意思是它是一个被封装在对象中的方法调用。

That sounds a lot like a "callback", "first-class function", "function pointer", "closure", or "partially applied function" depending on which language you're coming from, and indeed those are all in the same ballpark. The Gang of Four later says:

这听起来很像"回调"（callback）、"一等函数"（first-class function）、"函数指针"（function pointer）、"闭包"（closure）或"偏应用函数"（partially applied function）——具体取决于你来自哪种编程语言。事实上，这些都属于同一个范畴。GoF 在后文中也提到：

> Commands are an object-oriented replacement for callbacks.

> "命令是回调的一种面向对象替代方案。"

That would be a better slugline for the pattern than the one they chose.

这比他们选择的那个标题要更能说明模式本身。

But all of this is abstract and nebulous. I like to start chapters with something concrete, and I blew that. To make up for it, from here on out it's all examples where commands are a brilliant fit.

但所有这些都太抽象和模糊了。我喜欢用具体的内容来开篇，而这一点我搞砸了。为了弥补，接下来全是展示命令模式如何完美适配实际的例子。

> — Robert Nystrom, *Game Programming Patterns*
---

## Motivation / 动机

### Configuring Input / 配置输入

Somewhere in every game is a chunk of code that reads in raw user input — button presses, keyboard events, mouse clicks, whatever. It takes each input and translates it to a meaningful action in the game:

在每一款游戏中，都有一段代码负责读取原始用户输入——按钮按下、键盘事件、鼠标点击，等等。它将每个输入转换为游戏中有意义的动作：

![A controller, with A mapped to swapWeapon(), B mapped to lurch(), X mapped to jump(), and Y mapped to fireGun().](images/command-buttons-one.png)

![一个手柄示意图：A 键映射到 swapWeapon()，B 键映射到 lurch()，X 键映射到 jump()，Y 键映射到 fireGun()。](images/command-buttons-one.png)

A dead simple implementation looks like:

一个极其简单的实现如下：

```cpp
void InputHandler::handleInput() {
  if (isPressed(BUTTON_X)) jump();
  else if (isPressed(BUTTON_Y)) fireGun();
  else if (isPressed(BUTTON_A)) swapWeapon();
  else if (isPressed(BUTTON_B)) lurchIneffectively();
}
```

Pro tip: Don't press B very often.

专业提示：别经常按 B 键。

This function typically gets called once per frame by the [game loop](game-loop.html), and I'm sure you can figure out what it does. This code works if we're willing to hard-wire user inputs to game actions, but many games let the user *configure* how their buttons are mapped.

这个函数通常由[游戏主循环](game-loop.html)每帧调用一次，我相信你能明白它的作用。如果我们愿意将用户输入硬编码到游戏动作中，这段代码是没问题的。但许多游戏允许用户*自定义*按键映射。

To support that, we need to turn those direct calls to `jump()` and `fireGun()` into something that we can swap out. "Swapping out" sounds a lot like assigning a variable, so we need an *object* that we can use to represent a game action. Enter: the Command pattern.

为了支持这一点，我们需要将对 `jump()` 和 `fireGun()` 的直接调用转换为可以替换的东西。"替换"听起来很像给变量赋值，所以我们需要一个*对象*来代表一个游戏动作。接下来登场的就是：命令模式。

We define a base class that represents a triggerable game command:

我们定义一个代表可触发的游戏命令的基类：

```cpp
class Command {
public:
  virtual ~Command() {}
  virtual void execute() = 0;
};
```

When you have an interface with a single method that doesn't return anything, there's a good chance it's the Command pattern.

当你看到一个只有一个不返回任何值的方法的接口时，它很可能就是命令模式。

Then we create subclasses for each of the different game actions:

然后，我们为每个不同的游戏动作创建子类：

```cpp
class JumpCommand : public Command {
public:
  virtual void execute() { jump(); }
};

class FireCommand : public Command {
public:
  virtual void execute() { fireGun(); }
};

// You get the idea...
```

// 你懂的...
```

In our input handler, we store a pointer to a command for each button:

在我们的输入处理器中，为每个按钮存储一个命令指针：

```cpp
class InputHandler {
public:
  void handleInput();

// Methods to bind commands...

// 绑定命令的方法...

private:
  Command* buttonX_;
  Command* buttonY_;
  Command* buttonA_;
  Command* buttonB_;
};
```

Now the input handling just delegates to those:

现在输入处理只是简单地委托给这些命令：

```cpp
void InputHandler::handleInput() {
  if (isPressed(BUTTON_X)) buttonX_->execute();
  else if (isPressed(BUTTON_Y)) buttonY_->execute();
  else if (isPressed(BUTTON_A)) buttonA_->execute();
  else if (isPressed(BUTTON_B)) buttonB_->execute();
}
```

Notice how we don't check for `NULL` here? This assumes each button will have *some* command wired up to it.

注意到我们没有检查 `NULL` 吗？这假设了每个按钮上都已经绑定了*某个*命令。

If we want to support buttons that do nothing without having to explicitly check for `NULL`, we can define a command class whose `execute()` method does nothing. Then, instead of setting a button handler to `NULL`, we point it to that object. This is a pattern called [Null Object](http://en.wikipedia.org/wiki/Null_Object_pattern).

如果我们希望在不需要显式检查 `NULL` 的情况下支持什么都不做的按钮，可以定义一个其 `execute()` 方法为空操作的命令类。然后，我们不将按钮处理句柄设置为 `NULL`，而是将其指向那个对象。这被称为[空对象](http://en.wikipedia.org/wiki/Null_Object_pattern)模式。

Where each input used to directly call a function, now there's a layer of indirection:

现在，每个输入不再直接调用函数，而是有了一层间接引用：

![A controller, with each button mapped to a corresponding 'button_' variable which in turn is mapped to a function.](images/command-buttons-two.png)

![一个手柄示意图：每个按钮映射到对应的 'button_' 变量，再映射到函数。](images/command-buttons-two.png)

This is the Command pattern in a nutshell. If you can see the merit of it already, consider the rest of this chapter a bonus.

这就是命令模式的核心。如果你已经能看到它的优点，那么本章剩下的内容就当是附赠的礼物吧。
---

### Directions for Actors / 为角色指定方向

The command classes we just defined work for the previous example, but they're pretty limited. The problem is that they assume there are these top-level `jump()`, `fireGun()`, etc. functions that implicitly know how to find the player's avatar and make him dance like the puppet he is.

我们刚才定义的命令类适用于前面的例子，但它们非常有限。问题在于它们假设存在顶层的 `jump()`、`fireGun()` 等函数，这些函数隐式地知道如何找到玩家的化身，并让他像木偶一样舞动。

That assumed coupling limits the usefulness of those commands. The *only* thing the `JumpCommand` can make jump is the player. Let's loosen that restriction. Instead of calling functions that find the commanded object themselves, we'll *pass in* the object that we want to order around:

这种假设的耦合限制了这些命令的实用性。`JumpCommand` 能让其跳跃的*唯一*对象就是玩家。让我们来放松这个限制。我们不再调用那些自己查找命令目标对象的函数，而是*传入*我们想要指挥的对象：

```cpp
class Command {
public:
  virtual ~Command() {}
  virtual void execute(GameActor& actor) = 0;
};
```

Here, `GameActor` is our "game object" class that represents a character in the game world. We pass it in to `execute()` so that the derived command can invoke methods on an actor of our choice, like so:

这里 `GameActor` 是我们的"游戏对象"类，代表游戏世界中的一个角色。我们将其传入 `execute()`，这样派生命令就可以在我们选择的对象上调用方法，如下所示：

```cpp
class JumpCommand : public Command {
public:
  virtual void execute(GameActor& actor)
  {
    actor.jump();
  }
};
```

Now, we can use this one class to make any character in the game hop around. We're just missing a piece between the input handler and the command that takes the command and invokes it on the right object. First, we change `handleInput()` so that it *returns* commands:

现在，我们可以用这一个类让游戏中的任何角色四处跳跃。我们只是缺少了输入处理器和命令之间的一环——获取命令并在正确的对象上执行它。首先，我们修改 `handleInput()` 使其*返回*命令：

```cpp
Command* InputHandler::handleInput() {
  if (isPressed(BUTTON_X)) return buttonX_;
  if (isPressed(BUTTON_Y)) return buttonY_;
  if (isPressed(BUTTON_A)) return buttonA_;
  if (isPressed(BUTTON_B)) return buttonB_;

// Nothing pressed, so do nothing.
  return NULL;
}
```

// 没有按键按下，什么都不做。
  return NULL;
}
```

It can't execute the command immediately since it doesn't know what actor to pass in. Here's where we take advantage of the fact that the command is a reified call — we can *delay* when the call is executed.

它无法立即执行命令，因为它不知道要传入哪个角色。这里我们就利用了命令是被物化的调用这一特性——我们可以*延迟*调用的执行时间。

Then, we need some code that takes that command and runs it on the actor representing the player. Something like:

然后，我们需要一些代码来获取这个命令，并在代表玩家的角色上执行它：

```cpp
Command* command = inputHandler.handleInput();
if (command) {
  command->execute(actor);
}
```

Assuming `actor` is a reference to the player's character, this correctly drives him based on the user's input, so we're back to the same behavior we had in the first example. But adding a layer of indirection between the command and the actor that performs it has given us a neat little ability: *we can let the player control any actor in the game now by changing the actor we execute the commands on.*

假设 `actor` 是玩家角色的引用，这段代码会根据用户的输入正确驱动他。所以我们回到了第一个例子中的相同行为。但是在命令和执行命令的角色之间增加一层间接引用，给了我们一个很酷的能力：*通过更改执行命令的角色，我们可以让玩家控制游戏中的任何一个角色。*

In practice, that's not a common feature, but there is a similar use case that *does* pop up frequently. So far, we've only considered the player-driven character, but what about all of the other actors in the world? Those are driven by the game's AI. We can use this same command pattern as the interface between the AI engine and the actors; the AI code simply emits `Command` objects.

在实践中，这并不常见，但有一个类似的用例却*确实*频繁出现。到目前为止，我们只考虑了由玩家驱动的角色，但世界中的其他角色呢？那些是由游戏的 AI 驱动的。我们可以使用相同的命令模式作为 AI 引擎和角色之间的接口；AI 代码只需发出 `Command` 对象。

The decoupling here between the AI that selects commands and the actor code that performs them gives us a lot of flexibility. We can use different AI modules for different actors. Or we can mix and match AI for different kinds of behavior. Want a more aggressive opponent? Just plug-in a more aggressive AI to generate commands for it. In fact, we can even bolt AI onto the *player's* character, which can be useful for things like demo mode where the game needs to run on auto-pilot.

这种在"选择命令的 AI"和"执行命令的角色代码"之间的解耦给了我们很大的灵活性。我们可以为不同的角色使用不同的 AI 模块。或者我们可以混合搭配 AI 来实现不同类型的行为。想要一个更具攻击性的对手？只需插入一个更具攻击性的 AI 来为其生成命令。实际上，我们甚至可以将 AI 附加到*玩家*的角色上，这对于演示模式等需要游戏自动运行的情况非常有用。

By making the commands that control an actor first-class objects, we've removed the tight coupling of a direct method call. Instead, think of it as a queue or stream of commands:

通过将控制角色的命令作为一等对象，我们消除了直接方法调用的紧耦合。取而代之的，可以将其视为一个命令队列或流：

For lots more on what queueing can do for you, see [Event Queue](event-queue.html).

关于队列能为你做什么的更多内容，请参见[事件队列](event-queue.html)。

![A pipe connecting AI to Actor.](images/command-stream.png)

![连接 AI 和角色的管道。](images/command-stream.png)

Some code (the input handler or AI) produces commands and places them in the stream. Other code (the dispatcher or actor itself) consumes commands and invokes them. By sticking that queue in the middle, we've decoupled the producer on one end from the consumer on the other.

某些代码（输入处理器或 AI）产生命令并将其放入流中。其他代码（分发器或角色本身）消费命令并调用它们。通过在中间加入一个队列，我们解耦了一端的生产者和另一端的消费者。

If we take those commands and make them *serializable*, we can send the stream of them over the network. We can take the player's input, push it over the network to another machine, and then replay it. That's one important piece of making a networked multi-player game.

如果我们让这些命令变得*可序列化*，就可以通过网络发送命令流。我们可以获取玩家的输入，通过网络将其推送到另一台机器，然后回放它。这是构建联网多玩家游戏的关键组成部分之一。
---

### Undo and Redo / 撤销与重做

The final example is the most well-known use of this pattern. If a command object can *do* things, it's a small step for it to be able to *undo* them. Undo is used in some strategy games where you can roll back moves that you didn't like. It's *de rigueur* in tools that people use to *create* games. The surest way to make your game designers hate you is giving them a level editor that can't undo their fat-fingered mistakes.

最后一个例子是这个模式最著名的用途。如果一个命令对象可以*执行*操作，那么让它能够*撤销*操作也就只有一步之遥了。撤销功能在一些策略游戏中用于回滚你不喜欢的操作。在人们用来*创造*游戏的工具中，这是*必不可少的*。让你的关卡设计师恨你的最可靠方法，就是给他们一个不能撤销错误操作的关卡编辑器。

I may be speaking from experience here.

我可能是在用自己的亲身经历说话。

Without the Command pattern, implementing undo is surprisingly hard. With it, it's a piece of cake. Let's say we're making a single-player, turn-based game and we want to let users undo moves so they can focus more on strategy and less on guesswork.

没有命令模式，实现撤销异常困难。有了它，简直是小菜一碟。假设我们正在制作一个单人回合制游戏，我们希望允许用户撤销操作，这样他们可以更多地专注于策略，而不是猜测。

We're conveniently already using commands to abstract input handling, so every move the player makes is already encapsulated in them. For example, moving a unit may look like:

我们已经方便地使用命令来抽象输入处理，所以玩家的每一步操作都已经封装在命令中了。例如，移动一个单位可能如下所示：

```cpp
class MoveUnitCommand : public Command {
public:
  MoveUnitCommand(Unit* unit, int x, int y)
  : unit_(unit),
    x_(x),
    y_(y)
  {}

virtual void execute()
  {
    unit_->moveTo(x_, y_);
  }

private:
  Unit* unit_;
  int x_, y_;
};
```

Note this is a little different from our previous commands. In the last example, we wanted to *abstract* the command from the actor that it modified. In this case, we specifically want to *bind* it to the unit being moved. An instance of this command isn't a general "move something" operation that you could use in a bunch of contexts; it's a specific concrete move in the game's sequence of turns.

注意，这和我们之前的命令有些不同。在上一个例子中，我们希望*抽象出*命令与它所修改的角色之间的关系。在这种情况下，我们明确希望*将*命令绑定到正在被移动的单位上。这个命令的一个实例不是一个通用的"移动某个东西"操作——你不能在一堆不同的上下文中使用它；它是游戏中特定回合序列中的一个具体移动。

This highlights a variation in how the Command pattern gets implemented. In some cases, like our first couple of examples, a command is a reusable object that represents a *thing that can be done*. Our earlier input handler held on to a single command object and called its `execute()` method anytime the right button was pressed.

这突出了命令模式实现方式的一个变体。在某些情况下（比如我们的头几个例子），一个命令是一个可重用的对象，代表一个*可以做的事情*。我们之前的输入处理器持有单个命令对象，并在合适的按钮被按下时调用其 `execute()` 方法。

Here, the commands are more specific. They represent a thing that can be done at a specific point in time. This means that the input handling code will be *creating* an instance of this every time the player chooses a move. Something like:

在这里，命令更加具体。它们代表在特定时间点可以做的事情。这意味着输入处理代码将在每次玩家选择一个操作时*创建*一个新的实例。类似这样：

```cpp
Command* handleInput() {
  Unit* unit = getSelectedUnit();

if (isPressed(BUTTON_UP)) {
    // Move the unit up one.
    int destY = unit->y() - 1;
    return new MoveUnitCommand(unit, unit->x(), destY);
  }

if (isPressed(BUTTON_UP)) {
    // 将单位向上移动一格。
    int destY = unit->y() - 1;
    return new MoveUnitCommand(unit, unit->x(), destY);
  }

if (isPressed(BUTTON_DOWN)) {
    // Move the unit down one.
    int destY = unit->y() + 1;
    return new MoveUnitCommand(unit, unit->x(), destY);
  }

if (isPressed(BUTTON_DOWN)) {
    // 将单位向下移动一格。
    int destY = unit->y() + 1;
    return new MoveUnitCommand(unit, unit->x(), destY);
  }

// Other moves...

// 其他移动...

return NULL;
}
```

Of course, in a non-garbage-collected language like C++, this means the code executing commands will also be responsible for freeing their memory.

当然，在像 C++ 这样的非垃圾回收语言中，这意味着执行命令的代码也负责释放它们的内存。

The fact that commands are one-use-only will come to our advantage in a second. To make commands undoable, we define another operation each command class needs to implement:

命令"一次性使用"这一特性很快将成为我们的优势。为了让命令可撤销，我们为每个命令类定义另一个需要实现的操作：

```cpp
class Command {
public:
  virtual ~Command() {}
  virtual void execute() = 0;
  virtual void undo() = 0;
};
```

An `undo()` method reverses the game state changed by the corresponding `execute()` method. Here's our previous move command with undo support:

`undo()` 方法逆转由相应的 `execute()` 方法改变的游戏状态。以下是支持撤销的移动命令：

```cpp
class MoveUnitCommand : public Command {
public:
  MoveUnitCommand(Unit* unit, int x, int y)
  : unit_(unit),
    xBefore_(0),
    yBefore_(0),
    x_(x),
    y_(y)
  {}

virtual void execute()
  {
    // Remember the unit's position before the move
    // so we can restore it.
    xBefore_ = unit_->x();
    yBefore_ = unit_->y();

virtual void execute()
  {
    // 记住单位移动前的位置，以便恢复
    xBefore_ = unit_->x();
    yBefore_ = unit_->y();

unit_->moveTo(x_, y_);
  }

virtual void undo()
  {
    unit_->moveTo(xBefore_, yBefore_);
  }

private:
  Unit* unit_;
  int xBefore_, yBefore_;
  int x_, y_;
};
```

Note that we added some more state to the class. When a unit moves, it forgets where it used to be. If we want to be able to undo that move, we have to remember the unit's previous position ourselves, which is what `xBefore_` and `yBefore_` do.

注意我们为类添加了更多的状态。当一个单位移动时，它会忘记自己之前的位置。如果我们想要能够撤销这次移动，我们必须自己记住单位之前的位置，这就是 `xBefore_` 和 `yBefore_` 的作用。

This seems like a place for the [Memento](http://en.wikipedia.org/wiki/Memento_pattern) pattern, but I haven't found it to work well. Since commands tend to modify only a small part of an object's state, snapshotting the rest of its data is a waste of memory. It's cheaper to manually store only the bits you change.

这似乎是使用[备忘录](http://en.wikipedia.org/wiki/Memento_pattern)模式的好地方，但我发现它效果并不好。由于命令往往只修改对象状态的一小部分，快照其余数据是对内存的浪费。手动只存储你改变的部分更经济。

[*Persistent data structures*](http://en.wikipedia.org/wiki/Persistent_data_structure) are another option. With these, every modification to an object returns a new one, leaving the original unchanged. Through clever implementation, these new objects share data with the previous ones, so it's much cheaper than cloning the entire object.

[*持久化数据结构*](http://en.wikipedia.org/wiki/Persistent_data_structure)是另一种选择。使用这些结构，每次对对象的修改都会返回一个新的对象，原始对象保持不变。通过巧妙的实现，这些新对象与之前的对象共享数据，因此比克隆整个对象要经济得多。

Using a persistent data structure, each command stores a reference to the object before the command was performed, and undo just means switching back to the old object.

使用持久化数据结构，每个命令存储一个"命令执行前对象"的引用，撤销只需切换回旧的对象。

To let the player undo a move, we keep around the last command they executed. When they bang on Control-Z, we call that command's `undo()` method. (If they've already undone, then it becomes "redo" and we execute the command again.)

为了让玩家撤销一步操作，我们保留他们执行的最后一个命令。当他们按下 Control-Z 时，我们调用该命令的 `undo()` 方法。（如果他们之前已经撤销了，那么这就变成"重做"，我们再次执行命令。）

Supporting multiple levels of undo isn't much harder. Instead of remembering the last command, we keep a list of commands and a reference to the "current" one. When the player executes a command, we append it to the list and point "current" at it.

支持多级撤销并不是更困难。我们不只记住最后一个命令，而是维护一个命令列表和一个指向"当前"命令的引用。当玩家执行一个命令时，我们将其追加到列表中，并将"当前"指针指向它。

![A stack of commands from older to newer. A 'current' arrow points to one command, an 'undo' arrow points to the previous one, and 'redo' points to the next.](images/command-undo.png)

![一个从旧到新的命令栈。'current' 箭头指向一个命令，'undo' 箭头指向前一个，'redo' 指向后一个。](images/command-undo.png)

When the player chooses "Undo", we undo the current command and move the current pointer back. When they choose "Redo", we advance the pointer and then execute that command. If they choose a new command after undoing some, everything in the list after the current command is discarded.

当玩家选择"撤销"时，我们撤销当前命令并将当前指针向后移动。当选择"重做"时，我们向前移动指针然后执行该命令。如果在撤销一些操作后选择了新命令，当前命令之后列表中的所有内容都被丢弃。

The first time I implemented this in a level editor, I felt like a genius. I was astonished at how straightforward it was and how well it worked. It takes discipline to make sure every data modification goes through a command, but once you do that, the rest is easy.

我第一次在关卡编辑器中实现这个功能时，感觉自己像个天才。我惊讶于它是如此直观且工作得如此之好。确保每个数据修改都通过命令进行需要纪律性，但一旦你做到了，剩下的就简单了。

Redo may not be common in games, but re-*play* is. A naïve implementation would record the entire game state at each frame so it can be replayed, but that would use too much memory.

"重做"在游戏中可能不常见，但"重*放*"是。一种天真的实现会在每一帧记录整个游戏状态以便重放，但那会消耗太多内存。

Instead, many games record the set of commands every entity performed each frame. To replay the game, the engine just runs the normal game simulation, executing the pre-recorded commands.

相反，许多游戏记录每个实体每帧执行的一组命令。为了重放游戏，引擎只需运行正常的游戏模拟，执行预先记录的命令。
---

## The Pattern / 模式描述

The Command pattern encapsulates a request as an object. At its core, it defines an abstract base class (or interface) with a single `execute()` method. Concrete command subclasses implement specific actions. An invoker class stores command objects and calls `execute()` when triggered, decoupling the caller from the callee.

命令模式将一次请求封装为一个对象。其核心是定义一个带有单个 `execute()` 方法的抽象基类（或接口）。具体的命令子类实现特定的操作。调用者（Invoker）存储命令对象，并在被触发时调用 `execute()`，从而解耦调用方与被调用方。

The key participants are:

主要参与者：

- **Command:** Declares an interface (`execute()`, optionally `undo()`) for performing an operation.
- **ConcreteCommand:** Defines a binding between a Receiver and an action. Implements `execute()` by invoking the corresponding operation(s) on the Receiver.
- **Client:** Creates ConcreteCommand objects and sets their receiver.
- **Invoker:** Asks the command to carry out the request (e.g., an input handler that calls `execute()` on a button-mapped command).
- **Receiver:** The object that knows how to perform the work (e.g., a `Unit`, `GameActor`, or any game entity).

- **Command（命令）：** 声明执行操作的接口（`execute()`，可选 `undo()`）。
- **ConcreteCommand（具体命令）：** 定义接收者（Receiver）和动作之间的绑定。通过调用 Receiver 上的相应操作来实现 `execute()`。
- **Client（客户端）：** 创建 ConcreteCommand 对象并设置其 Receiver。
- **Invoker（调用者）：** 要求命令执行请求（例如，输入处理器在按钮映射的命令上调用 `execute()`）。
- **Receiver（接收者）：** 知道如何执行实际工作的对象（例如 `Unit`、`GameActor` 或任何游戏实体）。

The pattern creates a layer of indirection: Instead of code A calling code B directly, A invokes a command, and that command invokes B. This indirection enables:

该模式创建了一层间接引用：不再是代码 A 直接调用代码 B，而是 A 调用一个命令，然后该命令再调用 B。这种间接引用实现了：

- **Parameterization:** You can configure objects with different commands (e.g., rebindable keys).
- **Queuing:** Commands can be collected and executed later (e.g., AI command streams, network replay).
- **Logging:** Each command can be recorded for replay, debugging, or auditing.
- **Undo/Redo:** Commands can be given an `undo()` method since they encapsulate the full context of an operation.

- **参数化：** 可以用不同的命令配置对象（例如，可自定义按键）。
- **排队：** 命令可以被收集起来稍后执行（例如，AI 命令流、网络回放）。
- **日志记录：** 每个命令可以被记录用于回放、调试或审计。
- **撤销/重做：** 由于命令封装了操作的完整上下文，可以为其添加 `undo()` 方法。
---

## C++ Code (原书代码)

```cpp
// ============================================================
// 基础命令接口
// ============================================================
class Command {
public:
  virtual ~Command() {}
  virtual void execute() = 0;
};

// ============================================================
// 具体命令：跳跃 / 开火
// ============================================================
class JumpCommand : public Command {
public:
  virtual void execute() { jump(); }
};

class FireCommand : public Command {
public:
  virtual void execute() { fireGun(); }
};

// ============================================================
// 输入处理器：为每个按钮存储命令指针
// ============================================================
class InputHandler {
public:
  void handleInput();

  // Methods to bind commands...

private:
  Command* buttonX_;
  Command* buttonY_;
  Command* buttonA_;
  Command* buttonB_;
};

// ============================================================
// 输入处理委托给命令
// ============================================================
void InputHandler::handleInput() {
  if (isPressed(BUTTON_X)) buttonX_->execute();
  else if (isPressed(BUTTON_Y)) buttonY_->execute();
  else if (isPressed(BUTTON_A)) buttonA_->execute();
  else if (isPressed(BUTTON_B)) buttonB_->execute();
}

// ============================================================
// 支持角色参数化的命令接口（GameActor 版本）
// ============================================================
class Command {
public:
  virtual ~Command() {}
  virtual void execute(GameActor& actor) = 0;
};

class JumpCommand : public Command {
public:
  virtual void execute(GameActor& actor)
  {
    actor.jump();
  }
};

// ============================================================
// 返回命令而非直接执行
// ============================================================
Command* InputHandler::handleInput() {
  if (isPressed(BUTTON_X)) return buttonX_;
  if (isPressed(BUTTON_Y)) return buttonY_;
  if (isPressed(BUTTON_A)) return buttonA_;
  if (isPressed(BUTTON_B)) return buttonB_;

  // Nothing pressed, so do nothing.
  return NULL;
}

// ============================================================
// 执行命令
// ============================================================
Command* command = inputHandler.handleInput();
if (command) {
  command->execute(actor);
}

// ============================================================
// 可撤销的命令接口
// ============================================================
class Command {
public:
  virtual ~Command() {}
  virtual void execute() = 0;
  virtual void undo() = 0;
};

// ============================================================
// MoveUnitCommand：移动单位 + 撤销支持
// ============================================================
class MoveUnitCommand : public Command {
public:
  MoveUnitCommand(Unit* unit, int x, int y)
  : unit_(unit),
    xBefore_(0),
    yBefore_(0),
    x_(x),
    y_(y)
  {}

  virtual void execute()
  {
    // Remember the unit's position before the move
    // so we can restore it.
    xBefore_ = unit_->x();
    yBefore_ = unit_->y();

    unit_->moveTo(x_, y_);
  }

  virtual void undo()
  {
    unit_->moveTo(xBefore_, yBefore_);
  }

private:
  Unit* unit_;
  int xBefore_, yBefore_;
  int x_, y_;
};

// ============================================================
// 输入处理：每帧创建 MoveUnitCommand
// ============================================================
Command* handleInput() {
  Unit* unit = getSelectedUnit();

  if (isPressed(BUTTON_UP)) {
    // Move the unit up one.
    int destY = unit->y() - 1;
    return new MoveUnitCommand(unit, unit->x(), destY);
  }

  if (isPressed(BUTTON_DOWN)) {
    // Move the unit down one.
    int destY = unit->y() + 1;
    return new MoveUnitCommand(unit, unit->x(), destY);
  }

  // Other moves...

  return NULL;
}
```

---

## C# Equivalent (C# 对照实现)

```csharp
// 在 C# 中，Command 模式推荐使用接口（interface）而不是抽象类
// 因为 C# 接口支持多继承，且语义更清晰
public interface ICommand
{
    void Execute();
    void Undo();
}

// 在 Unity 中，Unit 通常是 MonoBehaviour，因此不能直接 new
// 需要通过 GameObject.Find 或依赖注入获得引用
public class MoveUnitCommand : ICommand
{
    // C# 属性替代 C++ 的 getter/setter，更简洁
    private readonly Unit _unit;   // readonly 表示命令绑定的目标不可变
    private int _xBefore, _yBefore; // 撤销所需的旧位置
    private readonly int _x, _y;     // 目标位置（只读，命令创建后不可改）

    // C# 构造函数：使用 => 表达式主体（C# 6+ 语法）
    public MoveUnitCommand(Unit unit, int x, int y) => (_unit, _x, _y) = (unit, x, y);

    public void Execute()
    {
        // 保存历史状态
        _xBefore = _unit.X;
        _yBefore = _unit.Y;
        _unit.MoveTo(_x, _y);
        Debug.Log($"Moved unit from ({_xBefore},{_yBefore}) to ({_x},{_y})");
    }

    public void Undo()
    {
        _unit.MoveTo(_xBefore, _yBefore);
        Debug.Log($"Undid move: returned to ({_xBefore},{_yBefore})");
    }
}

// ============================================
// Unity 输入处理器：处理玩家输入并生成命令
// ============================================
public class InputHandler : MonoBehaviour
{
    // 使用 [SerializeField] 可在 Inspector 中拖拽绑定，实现可视化配置
    [SerializeField] private CommandSet _commandSet; // ScriptableObject 方案

    [SerializeField] private KeyCode moveUpKey = KeyCode.W;
    [SerializeField] private KeyCode moveDownKey = KeyCode.S;

    // Unity 的 Update 每帧调用，替代 C++ 主循环中的 handleInput
    private void Update()
    {
        // 每帧检查输入并生成命令
        ICommand command = HandleInput();
        if (command != null)
        {
            CommandManager.Instance.ExecuteCommand(command);
        }
    }

    private ICommand HandleInput()
    {
        // 通过 Unity 的 Selectable 系统获取当前选中单位
        Unit selectedUnit = SelectionManager.GetSelectedUnit();
        if (selectedUnit == null) return null;

        // C# 的 Input 系统：GetKeyDown 替代 C++ 的 isPressed
        if (Input.GetKeyDown(moveUpKey))
            return new MoveUnitCommand(selectedUnit, selectedUnit.X, selectedUnit.Y - 1);
        if (Input.GetKeyDown(moveDownKey))
            return new MoveUnitCommand(selectedUnit, selectedUnit.X, selectedUnit.Y + 1);

        return null; // 无输入时返回 null，调用方需处理
    }
}

// ============================================
// 命令管理器：管理撤销/重做栈（单例模式）
// ============================================
// 使用 Unity 的 Singleton 模式，可在任何地方访问
// 避免 C++ 中的手动内存管理——C# 有 GC，不需要手动 delete
public class CommandManager : MonoBehaviour
{
    // 经典 Unity 单例实现
    public static CommandManager Instance { get; private set; }
    private void Awake() => Instance = this;

    // 使用 Stack<T> 泛型集合管理命令历史
    // C++ 中需要手写链表或使用 std::vector，C# 有现成的泛型集合
    private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
    private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // 执行新命令后清空重做栈
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        ICommand command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        ICommand command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
    }

    private void Update()
    {
        // Unity 中通过 Input 检测 Ctrl+Z / Ctrl+Y
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
            Undo();
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y))
            Redo();
    }
}

// ============================================
// 高级用法：使用委托作为轻量级命令
// ============================================
// 当命令不需要撤销功能时，C# 的 Action 委托可以替代完整接口
// 这是 C++ 函数指针做不到的优雅方式

public class DelegateCommand : ICommand
{
    // C# 的 Action 委托：封装一个无返回值的方法
    private readonly Action _execute;
    private readonly Action _undo;

    public DelegateCommand(Action execute, Action undo = null)
    {
        _execute = execute;
        _undo = undo;
    }

    public void Execute() => _execute?.Invoke();
    public void Undo() => _undo?.Invoke();
}

// 使用示例（lambda 表达式，C# 3.0+）：
// var cmd = new DelegateCommand(
//     () => player.MoveTo(10, 20),            // execute
//     () => player.MoveTo(previousX, previousY) // undo
// );
```

---

## Unity Application / Unity 应用场景

- **Input System 与按键绑定：** Unity 的新输入系统（Input System Package）底层即采用命令模式思想。每个 `InputAction` 可绑定到 `ICommand` 实现，支持运行时动态改键和输入设备热插拔。
- **撤销/重做系统：** 关卡编辑器、Tilemap 编辑器或对话编辑器中，使用 `Stack<ICommand>` 管理编辑历史，确保设计师可以撤回误操作。每次修改都封装为命令对象压入撤销栈。
- **AI 行为队列：** 将 AI 决策结果（移动、攻击、施法等）封装为命令并放入队列，AI 每帧从队列取出命令执行。支持为不同角色插拔不同的 AI 策略模块，且天然支持网络同步——序列化命令流发送到客户端回放。

---

## When to Use / 使用场景

Use the Command pattern when:

在以下情况下使用命令模式：

- **You need to parameterize objects with an action.** This is the classic motivating example: you want to let users rebind input keys without changing the input handling code. By making each action a command object, you can swap them in and out at runtime.
- **You need to queue, schedule, or execute actions at different times.** Commands can be deferred — stored for later execution. This is useful for AI systems that generate command streams, or for networked games where player input is transmitted as serialized commands and replayed on remote machines.
- **You need to support undo.** The command is the natural unit for undo because it encapsulates both the action and the state needed to reverse it. By storing the command's previous state (e.g., position before a move), the `undo()` method can restore it. Combining this with a history stack gives you multi-level undo.
- **You need to support replay or logging.** Record each command executed per frame, then replay them later by running the normal game loop against those pre-recorded commands. This is far more memory-efficient than recording full game state snapshots.

- **需要用动作对对象进行参数化时。** 这是经典的动机示例：你希望允许用户在运行时重新绑定按键，而无需修改输入处理代码。通过将每个动作制作成命令对象，可以在运行时进行替换。
- **需要排队、调度或在不同时间执行动作时。** 命令可以被延迟——存储起来供后续执行。这对于生成命令流的 AI 系统，或是在联网游戏中将玩家输入作为序列化命令传输并在远程机器上回放，非常有用。
- **需要支持撤销时。** 命令是撤销操作的自然单元，因为它既封装了动作，也封装了逆转该动作所需的状态。通过存储命令执行前的状态（例如移动前的位置），`undo()` 方法可以恢复它。结合历史栈，即可实现多级撤销。
- **需要支持回放或日志记录时。** 记录每帧每个实体执行的命令，然后通过运行正常的游戏循环来执行这些预先记录的命令。这比记录完整的游戏状态快照要节省内存得多。
---

## Keep in Mind / 注意事项

Earlier, I said commands are similar to first-class functions or closures, but every example I showed here used class definitions. If you're familiar with functional programming, you're probably wondering where the functions are.

我之前提到，命令类似于一等函数或闭包，但我在这里展示的每个示例都使用了类定义。如果你熟悉函数式编程，你可能会疑惑函数在哪里。

I wrote the examples this way because C++ has pretty limited support for first-class functions. Function pointers are stateless, functors are weird and still require defining a class, and the lambdas in C++11 are tricky to work with because of manual memory management.

我这样写示例是因为 C++ 对一等函数的支持非常有限。函数指针是无状态的，仿函数（functors）很奇怪而且仍然需要定义一个类，而 C++11 中的 lambda 由于手动内存管理的原因使用起来也很棘手。

That's *not* to say you shouldn't use functions for the Command pattern in other languages. If you have the luxury of a language with real closures, by all means, use them! In some ways, the Command pattern is a way of emulating closures in languages that don't have them.

这*并不是*说你不应该在其它语言中为命令模式使用函数。如果你有幸使用一种拥有真正闭包的语言，尽管去用！在某种意义上，命令模式是在没有闭包的语言中模拟闭包的一种方式。

I say *some* ways here because building actual classes or structures for commands is still useful even in languages that have closures. If your command has multiple operations (like undoable commands), mapping that to a single function is awkward.

我在这里说"在某种意义上"，是因为即使在使用闭包的语言中，为命令构建实际的类或结构体仍然是有用的。如果你的命令有多个操作（例如可撤销的命令），将其映射到单个函数会很尴尬。

Defining an actual class with fields also helps readers easily tell what data the command contains. Closures are a wonderfully terse way of automatically wrapping up some state, but they can be so automatic that it's hard to see what state they're actually holding.

定义一个带有字段的实际类也能帮助读者轻松判断命令包含哪些数据。闭包是一种极其简洁的自动封装某些状态的方式，但它们可能太"自动"了，以至于很难看出它们实际持有了什么状态。

For example, if we were building a game in JavaScript, we could create a move unit command just like this:

例如，如果我们用 JavaScript 构建游戏，可以这样创建一个移动单位命令：

```js
function makeMoveUnitCommand(unit, x, y) {
  // This function here is the command object:
  return function() {
    unit.moveTo(x, y);
  }
}
```

```js
function makeMoveUnitCommand(unit, x, y) {
  // 这个函数本身就是命令对象：
  return function() {
    unit.moveTo(x, y);
  }
}
```

We could add support for undo as well using a pair of closures:

我们还可以使用一对闭包来添加撤销支持：

```js
function makeMoveUnitCommand(unit, x, y) {
  var xBefore, yBefore;
  return {
    execute: function() {
      xBefore = unit.x();
      yBefore = unit.y();
      unit.moveTo(x, y);
    },
    undo: function() {
      unit.moveTo(xBefore, yBefore);
    }
  };
}
```

If you're comfortable with a functional style, this way of doing things is natural. If you aren't, I hope this chapter helped you along the way a bit. For me, the usefulness of the Command pattern really shows how effective the functional paradigm is for many problems.

如果你适应函数式风格，这种做法是很自然的。如果你不适应，我希望本章能对你有所帮助。对我来说，命令模式的价值恰恰展示了函数式范式对许多问题的有效性。

— Robert Nystrom, *Game Programming Patterns*
---

## Key Differences / 关键差异

| C++ | C# |
|-----|----|
| 使用抽象类 `class Command` + 纯虚函数 `virtual void execute() = 0` | 使用 `interface ICommand`，更轻量、支持多继承 |
| 手动内存管理：`new` / `delete` | GC 自动回收，`Stack<T>` 等泛型集合开箱即用 |
| 无函数式特性：函数指针无状态，std::function 复杂 | 支持 `Action`/`Func` 委托和 lambda，可做轻量命令 |
| NULL 表示空命令 | 可用 `null` 或引入 Null Object 模式（空实现的类） |
| 撤销需手动保存历史状态到成员变量 | 同样需保存，但 C# 属性语法更简洁，`readonly` 保证不变性 |
| 无内置序列化 | `[Serializable]` + `BinaryFormatter`/`JsonUtility` 可轻松序列化命令用于网络同步 |

---

## See Also / 扩展阅读

- You may end up with a lot of different command classes. In order to make it easier to implement those, it's often helpful to define a concrete base class with a bunch of convenient high-level methods that the derived commands can compose to define their behavior. That turns the command's main `execute()` method into the [Subclass Sandbox](subclass-sandbox.html) pattern.

- 你最终可能会有很多不同的命令类。为了更容易实现它们，定义一个具体的基类通常会很有帮助，基类提供一组方便的高级方法，派生命令可以组合这些方法来定义自己的行为。这将命令的主要 `execute()` 方法转变为[子类沙盒](subclass-sandbox.html)模式。

- In our examples, we explicitly chose which actor would handle a command. In some cases, especially where your object model is hierarchical, it may not be so cut-and-dried. An object may respond to a command, or it may decide to pawn it off on some subordinate object. If you do that, you've got yourself the [Chain of Responsibility](http://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) pattern.

- 在我们的示例中，我们明确选择了由哪个角色来处理命令。在某些情况下，特别是当你的对象模型是分层结构时，事情可能没有那么明确。一个对象可能响应一个命令，也可能决定将其推给某个下属对象。如果你这样做，就得到了[职责链](http://en.wikipedia.org/wiki/Chain-of-responsibility_pattern)模式。

- Some commands are stateless chunks of pure behavior like the `JumpCommand` in the first example. In cases like that, having more than one instance of that class wastes memory since all instances are equivalent. The [Flyweight](flyweight.html) pattern addresses that.

- 有些命令是无状态的纯行为——比如第一个示例中的 `JumpCommand`。在这种情况下，拥有该类的多个实例会浪费内存，因为所有实例都是等价的。[享元](flyweight.html)模式解决了这个问题。

You could make it a [singleton](singleton.html) too, but friends don't let friends create singletons.

你也可以将其做成[单例](singleton.html)，但"朋友之间不让朋友创建单例"。

---

> *This document contains verbatim excerpts from **Game Programming Patterns** by Robert Nystrom, © 2009-2021. Used for educational purposes.*

---

# Flyweight Pattern — 享元模式

> **EN:** Flyweight, like its name implies, comes into play when you have objects that need to be more lightweight, generally because you have too many of them. The pattern solves that by separating out an object's data into two kinds — intrinsic (shared, context-free) state and extrinsic (unique, per-instance) state. This pattern saves memory by sharing one copy of the intrinsic state across every place where an object appears.
>
> **CN:** 享元模式如其名所示，当你拥有过多需要变得更轻量的对象时，它就派上用场了。该模式通过将对象的数据分为两类来解决这一问题——内在状态（可共享、与上下文无关）和外在状态（每个实例独有的数据）。该模式通过在对象的每个出现位置共享一份内在状态来节省内存。
## Intent / 意图

* Flyweight, like its name implies, comes into play when you have objects that need to be more lightweight, generally because you have too many of them.

The pattern solves that by separating out an object's data into two kinds. The first kind of data is the stuff that's not specific to a single *instance* of that object and can be shared across all of them. The Gang of Four calls this the *intrinsic* state, but I like to think of it as the "context-free" stuff. In the example here, this is the geometry and textures for the tree.

The rest of the data is the *extrinsic* state, the stuff that is unique to that instance. In this case, that is each tree's position, scale, and color. Just like in the chunk of sample code up there, this pattern saves memory by sharing one copy of the intrinsic state across every place where an object appears.

**CN:** 享元模式如其名所示，当你拥有过多需要变得更轻量的对象时，它就派上用场了。

该模式通过将对象的数据分为两类来解决这一问题。第一类数据是不特定于对象的单个实例、可以在所有实例间共享的数据。四人帮称之为*内在*状态，但我更愿意将其理解为"与上下文无关"的数据。在树的例子中，这就是树的几何体和纹理。

其余数据是*外在*状态，即该实例特有的数据。在这个例子中，就是每棵树的位置、缩放和颜色。就像上面的示例代码所示，该模式通过在对象的每个出现位置共享一份内在状态来节省内存。

## Motivation / 动机

* The fog lifts, revealing a majestic old growth forest. Ancient hemlocks, countless in number, tower over you forming a cathedral of greenery. The stained glass canopy of leaves fragments the sunlight into golden shafts of mist. Between giant trunks, you can make out the massive forest receding into the distance.

This is the kind of otherworldly setting we dream of as game developers, and scenes like these are often enabled by a pattern whose name couldn't possibly be more modest: the humble Flyweight.

### Forest for the Trees

I can describe a sprawling woodland with just a few sentences, but actually *implementing* it in a realtime game is another story. When you've got an entire forest of individual trees filling the screen, all that a graphics programmer sees is the millions of polygons they'll have to somehow shovel onto the GPU every sixtieth of a second.

We're talking thousands of trees, each with detailed geometry containing thousands of polygons. Even if you have enough *memory* to describe that forest, in order to render it, that data has to make its way over the bus from the CPU to the GPU.

Each tree has a bunch of bits associated with it:

- A mesh of polygons that define the shape of the trunk, branches, and greenery.
- Textures for the bark and leaves.
- Its location and orientation in the forest.
- Tuning parameters like size and tint so that each tree looks different.

If you were to sketch it out in code, you'd have something like this:

```cpp
class Tree {
private:
  Mesh mesh_;
  Texture bark_;
  Texture leaves_;
  Vector position_;
  double height_;
  double thickness_;
  Color barkTint_;
  Color leafTint_;
};
```

That's a lot of data, and the mesh and textures are particularly large. An entire forest of these objects is too much to throw at the GPU in one frame. Fortunately, there's a time-honored trick to handling this.

The key observation is that even though there may be thousands of trees in the forest, they mostly look similar. They will likely all use the same mesh and textures. That means most of the fields in these objects are the *same* between all of those instances.

We can model that explicitly by splitting the object in half. First, we pull out the data that all trees have in common and move it into a separate class:

```cpp
class TreeModel {
private:
  Mesh mesh_;
  Texture bark_;
  Texture leaves_;
};
```

The game only needs a single one of these, since there's no reason to have the same meshes and textures in memory a thousand times. Then, each *instance* of a tree in the world has a *reference* to that shared `TreeModel`. What remains in `Tree` is the state that is instance-specific:

```cpp
class Tree {
private:
  TreeModel* model_;

  Vector position_;
  double height_;
  double thickness_;
  Color barkTint_;
  Color leafTint_;
};
```

This looks a lot like the [Type Object](type-object.html) pattern. Both involve delegating part of an object's state to some other object shared between a number of instances. However, the intent behind the patterns differs.

With a type object, the goal is to minimize the number of classes you have to define by lifting "types" into your own object model. Any memory sharing you get from that is a bonus. The Flyweight pattern is purely about efficiency.

This is all well and good for storing stuff in main memory, but that doesn't help rendering. Before the forest gets on screen, it has to work its way over to the GPU. We need to express this resource sharing in a way that the graphics card understands.

### A Thousand Instances

To minimize the amount of data we have to push to the GPU, we want to be able to send the shared data — the `TreeModel` — just *once*. Then, separately, we push over every tree instance's unique data — its position, color, and scale. Finally, we tell the GPU, "Use that one model to render each of these instances."

Fortunately, today's graphics APIs and cards support exactly that. The details are fiddly and out of the scope of this book, but both Direct3D and OpenGL can do something called [*instanced rendering*](http://en.wikipedia.org/wiki/Geometry_instancing).

In both APIs, you provide two streams of data. The first is the blob of common data that will be rendered multiple times — the mesh and textures in our arboreal example. The second is the list of instances and their parameters that will be used to vary that first chunk of data each time it's drawn. With a single draw call, an entire forest grows.

The fact that this API is implemented directly by the graphics card means the Flyweight pattern may be the only Gang of Four design pattern to have actual hardware support.

**CN:** 雾气升起，显露出一片雄伟的古老森林。无数古老的铁杉高耸入云，在你周围形成一座绿色的大教堂。彩绘玻璃般的树叶天幕将阳光切割成一道道金色的雾柱。在巨大的树干之间，你可以看到广袤的森林向远方延伸。

这就是我们作为游戏开发者梦寐以求的那种超凡脱俗的场景，而这样的场景通常是由一个名字再朴素不过的模式实现的：谦逊的享元模式。

### 森林之树

我可以用几句话来描述一片广阔的林地，但在实时游戏中*实现*它则是另一回事。当整个森林的树木充满屏幕时，图形程序员看到的是他们必须每六十分之一秒以某种方式将数百万个多边形塞进 GPU。

我们说的是数千棵树，每棵都有包含数千个多边形的精细几何体。即使你有足够的内存来描述那片森林，为了渲染它，这些数据也必须通过总线从 CPU 传输到 GPU。

每棵树都有一堆相关的数据：

- 定义树干、树枝和绿叶形状的多边形网格。
- 树皮和树叶的纹理。
- 它在森林中的位置和朝向。
- 大小和色调等调节参数，使每棵树看起来不同。

关键观察结果是：即使森林中可能有数千棵树，它们看起来也大多相似。它们很可能都使用相同的网格和纹理。这意味着这些对象中的大多数字段在所有实例之间都是*相同*的。

我们可以通过将对象分成两半来显式建模。首先，我们提取出所有树共有的数据，并将其移到一个单独的类中。游戏只需要一个这样的实例，因为没有理由在内存中存储相同的网格和纹理一千次。然后，世界中的每个树*实例*都有一个指向共享 `TreeModel` 的*引用*。`Tree` 中剩下的就是实例特有的状态。

这看起来很像类型对象模式。两者都涉及将对象的部分状态委托给在多个实例之间共享的其他对象。然而，这些模式背后的意图是不同的。

对于类型对象，目标是通过将"类型"提升到对象模型中来最小化需要定义的类的数量。从中获得的任何内存共享都是额外收获。而享元模式纯粹是关于效率的。

### 成千上万的实例

为了最小化需要推送到 GPU 的数据量，我们希望只发送共享数据（`TreeModel`）*一次*。然后，分别推送每个树实例的独特数据——它的位置、颜色和缩放。最后，告诉 GPU："使用那一个模型来渲染每个实例。"

幸运的是，今天的图形 API 和显卡完全支持这一点。细节繁琐且超出了本书的范围，但 Direct3D 和 OpenGL 都可以做一种叫做*实例化渲染*的技术。

在两种 API 中，你都提供两个数据流。第一个是会被多次渲染的公共数据块——在我们的树木例子中就是网格和纹理。第二个是实例列表及其参数，这些参数将用于每次绘制时变化第一块数据。通过一次绘制调用，一整片森林就生长出来了。

这个 API 由显卡直接实现这一事实意味着享元模式可能是四人帮设计模式中唯一拥有实际硬件支持的模式。

## The Pattern / 模式描述

* Now that we've got one concrete example under our belts, I can walk you through the general pattern. Flyweight, like its name implies, comes into play when you have objects that need to be more lightweight, generally because you have too many of them.

With instanced rendering, it's not so much that they take up too much memory as it is they take too much *time* to push each separate tree over the bus to the GPU, but the basic idea is the same.

The pattern solves that by separating out an object's data into two kinds. The first kind of data is the stuff that's not specific to a single *instance* of that object and can be shared across all of them. The Gang of Four calls this the *intrinsic* state, but I like to think of it as the "context-free" stuff. In the example here, this is the geometry and textures for the tree.

The rest of the data is the *extrinsic* state, the stuff that is unique to that instance. In this case, that is each tree's position, scale, and color. Just like in the chunk of sample code up there, this pattern saves memory by sharing one copy of the intrinsic state across every place where an object appears.

From what we've seen so far, this seems like basic resource sharing, hardly worth being called a pattern. That's partially because in this example here, we could come up with a clear separate *identity* for the shared state: the `TreeModel`.

I find this pattern to be less obvious (and thus more clever) when used in cases where there isn't a really well-defined identity for the shared object. In those cases, it feels more like an object is magically in multiple places at the same time. Let me show you another example.

### A Place To Put Down Roots

The ground these trees are growing on needs to be represented in our game too. There can be patches of grass, dirt, hills, lakes, rivers, and whatever other terrain you can dream up. We'll make the ground *tile-based*: the surface of the world is a huge grid of tiny tiles. Each tile is covered in one kind of terrain.

Each terrain type has a number of properties that affect gameplay:

- A movement cost that determines how quickly players can move through it.
- A flag for whether it's a watery terrain that can be crossed by boats.
- A texture used to render it.

Because we game programmers are paranoid about efficiency, there's no way we'd store all of that state in each tile in the world. Instead, a common approach is to use an enum for terrain types:

After all, we already learned our lesson with those trees.

```cpp
enum Terrain {
  TERRAIN_GRASS,
  TERRAIN_HILL,
  TERRAIN_RIVER
  // Other terrains...
};
```

Then the world maintains a huge grid of those:

```cpp
class World {
private:
  Terrain tiles_[WIDTH][HEIGHT];
};
```

To actually get the useful data about a tile, we do something like:

```cpp
int World::getMovementCost(int x, int y) {
  switch (tiles_[x][y])
  {
    case TERRAIN_GRASS: return 1;
    case TERRAIN_HILL:  return 3;
    case TERRAIN_RIVER: return 2;
      // Other terrains...
  }
}

bool World::isWater(int x, int y) {
  switch (tiles_[x][y])
  {
    case TERRAIN_GRASS: return false;
    case TERRAIN_HILL:  return false;
    case TERRAIN_RIVER: return true;
      // Other terrains...
  }
}
```

You get the idea. This works, but I find it ugly. I think of movement cost and wetness as *data* about a terrain, but here that's embedded in code. Worse, the data for a single terrain type is smeared across a bunch of methods. It would be really nice to keep all of that encapsulated together. After all, that's what objects are designed for.

It would be great if we could have an actual terrain *class*, like:

```cpp
class Terrain {
public:
  Terrain(int movementCost,
          bool isWater,
          Texture texture)
  : movementCost_(movementCost),
    isWater_(isWater),
    texture_(texture)
  {}

  int getMovementCost() const { return movementCost_; }
  bool isWater() const { return isWater_; }
  const Texture& getTexture() const { return texture_; }

private:
  int movementCost_;
  bool isWater_;
  Texture texture_;
};
```

You'll notice that all of the methods here are `const`. That's no coincidence. Since the same object is used in multiple contexts, if you were to modify it, the changes would appear in multiple places simultaneously.

That's probably not what you want. Sharing objects to save memory should be an optimization that doesn't affect the visible behavior of the app. Because of this, Flyweight objects are almost always immutable.

But we don't want to pay the cost of having an instance of that for each tile in the world. If you look at that class, you'll notice that there's actually *nothing* in there that's specific to *where* that tile is. In flyweight terms, *all* of a terrain's state is "intrinsic" or "context-free".

Given that, there's no reason to have more than one of each terrain type. Every grass tile on the ground is identical to every other one. Instead of having the world be a grid of enums or Terrain objects, it will be a grid of *pointers* to `Terrain` objects:

```cpp
class World {
private:
  Terrain* tiles_[WIDTH][HEIGHT];

  // Other stuff...
};
```

Each tile that uses the same terrain will point to the same terrain instance.

Since the terrain instances are used in multiple places, their lifetimes would be a little more complex to manage if you were to dynamically allocate them. Instead, we'll just store them directly in the world:

```cpp
class World {
public:
  World()
  : grassTerrain_(1, false, GRASS_TEXTURE),
    hillTerrain_(3, false, HILL_TEXTURE),
    riverTerrain_(2, true, RIVER_TEXTURE)
  {}

private:
  Terrain grassTerrain_;
  Terrain hillTerrain_;
  Terrain riverTerrain_;

  // Other stuff...
};
```

Then we can use those to paint the ground like this:

```cpp
void World::generateTerrain() {
  // Fill the ground with grass.
  for (int x = 0; x < WIDTH; x++)
  {
    for (int y = 0; y < HEIGHT; y++)
    {
      // Sprinkle some hills.
      if (random(10) == 0)
      {
        tiles_[x][y] = &hillTerrain_;
      }
      else
      {
        tiles_[x][y] = &grassTerrain_;
      }
    }
  }

  // Lay a river.
  int x = random(WIDTH);
  for (int y = 0; y < HEIGHT; y++) {
    tiles_[x][y] = &riverTerrain_;
  }
}
```

Now instead of methods on `World` for accessing the terrain properties, we can expose the `Terrain` object directly:

```cpp
const Terrain& World::getTile(int x, int y) const {
  return *tiles_[x][y];
}
```

This way, `World` is no longer coupled to all sorts of details of terrains. If you want some property of the tile, you can get it right from that object:

```cpp
int cost = world.getTile(2, 3).getMovementCost();
```

We're back to the pleasant API of working with real objects, and we did this with almost no overhead — a pointer is often no larger than an enum.

**CN:** 现在我们有了一个具体的例子，我可以带你了解一般模式了。享元模式如其名所示，当你拥有过多需要变得更轻量的对象时，它就派上用场了。

对于实例化渲染来说，与其说是它们占用了太多内存，不如说是将每棵独立的树通过总线推送到 GPU 花费了太多*时间*，但基本思路是一样的。

该模式通过将对象的数据分为两类来解决这一问题。第一类数据是不特定于对象的单个实例、可以在所有实例间共享的数据。四人帮称之为*内在*状态，但我更愿意将其理解为"与上下文无关"的数据。在树的例子中，这就是树的几何体和纹理。

其余数据是*外在*状态，即该实例特有的数据。在这个例子中，就是每棵树的位置、缩放和颜色。就像上面的示例代码所示，该模式通过在对象的每个出现位置共享一份内在状态来节省内存。

从我们目前所看到的来看，这似乎是基本的资源共享，几乎不值得被称为模式。部分原因是在这个例子中，我们可以为共享状态找到一个清晰的独立*身份*：`TreeModel`。

我发现当共享对象没有一个真正明确定义的身份时，这个模式就不那么明显（因此也更巧妙）。在这些情况下，感觉就像一个对象神奇地同时出现在多个地方。让我给你展示另一个例子。

### 扎根之地

这些树木生长的地面也需要在我们的游戏中表现出来。可能有草地、泥土、丘陵、湖泊、河流，以及你能想象到的任何其他地形。我们将地面做成*基于瓦片*的：世界表面是一个由微小瓦片组成的巨大网格。每个瓦片上覆盖着一种地形。

每种地形类型都有许多影响游戏玩法的属性：

- 移动消耗，决定玩家通过它的速度。
- 一个标记，指示它是否是可以通过船只的水域地形。
- 用于渲染它的纹理。

因为我们游戏程序员对效率偏执多疑，我们绝不可能在世界中的每个瓦片中存储所有这些状态。相反，一种常见的方法是对地形类型使用枚举：

毕竟，我们已经从那些树那里学到了教训。

但我不想为世界中的每个瓦片都付出拥有一个实例的代价。如果你看看那个类，你会注意到里面实际上*没有任何*东西是特定于该瓦片*位置*的。用享元的术语来说，地形的*所有*状态都是"内在的"或"与上下文无关的"。

鉴于此，没有理由每种地形类型拥有多于一个实例。地面上的每块草瓦片与其他任何一块都是相同的。世界不再是枚举或 Terrain 对象的网格，而是指向 `Terrain` 对象的*指针*网格。

每个使用相同地形的瓦片将指向同一个地形实例。

由于地形实例在多个地方使用，如果动态分配它们，它们的生命周期管理会稍微复杂一些。相反，我们直接将它们存储在世界中。

这样，`World` 就不再与各种地形细节耦合了。如果你想要瓦片的某个属性，可以直接从该对象获取：

```cpp
int cost = world.getTile(2, 3).getMovementCost();
```

我们又回到了使用真实对象的愉悦 API，而且我们几乎没有开销就做到了这一点——指针通常不比枚举大。

## When to Use / 使用场景

* Flyweight, like its name implies, comes into play when you have objects that need to be more lightweight, generally because you have too many of them.

If you find yourself creating an enum and doing lots of switches on it, consider this pattern instead.

What I *am* confident of is that using flyweight objects shouldn't be dismissed out of hand. They give you the advantages of an object-oriented style without the expense of tons of objects.

**CN:** 享元模式如其名所示，当你拥有过多需要变得更轻量的对象时，它就派上用场了。

如果你发现自己创建了一个枚举并对其做了大量 switch 分支判断，请考虑使用此模式替代。

我*确信*的是，享元对象不应该被轻易否定。它们为你提供了面向对象风格的优势，而无需付出大量对象的代价。

## Keep in Mind / 注意事项

* I say "almost" here because the performance bean counters will rightfully want to know how this compares to using an enum. Referencing the terrain by pointer implies an indirect lookup. To get to some terrain data like the movement cost, you first have to follow the pointer in the grid to find the terrain object and then find the movement cost there. Chasing a pointer like this can cause a cache miss, which can slow things down.

As always, the golden rule of optimization is *profile first*. Modern computer hardware is too complex for performance to be a game of pure reason anymore. In my tests for this chapter, there was no penalty for using a flyweight over an enum. Flyweights were actually noticeably faster. But that's entirely dependent on how other stuff is laid out in memory.

What I *am* confident of is that using flyweight objects shouldn't be dismissed out of hand. They give you the advantages of an object-oriented style without the expense of tons of objects. If you find yourself creating an enum and doing lots of switches on it, consider this pattern instead. If you're worried about performance, at least profile first before changing your code to a less maintainable style.

**CN:** 我在这里说"几乎"，是因为那些计较性能的人会理所当然地想知道这和使用枚举相比如何。通过指针引用地形意味着间接查找。要获取地形数据（如移动消耗），你首先需要跟踪网格中的指针找到地形对象，然后在其中找到移动消耗值。这样的指针追踪可能导致缓存未命中，从而降低速度。

一如既往，优化的黄金法则是*先做性能分析*。现代计算机硬件太复杂了，性能不再是一个纯粹推理的游戏。在我为本章所做的测试中，使用享元比使用枚举没有任何性能损失。享元实际上明显更快。但这完全取决于其他内容在内存中的布局。

我*确信*的是，享元对象不应该被轻易否定。它们为你提供了面向对象风格的优势，而无需付出大量对象的代价。如果你发现自己创建了一个枚举并对其做了大量 switch 分支判断，请考虑使用此模式替代。如果你担心性能，至少在将代码改为可维护性更差的风格之前先做性能分析。

## C++ Code (原书代码)

```cpp
// —— 方案一：树木渲染 ——

// 享元前的 Tree 类（每个实例存储所有数据）
class Tree {
private:
  Mesh mesh_;
  Texture bark_;
  Texture leaves_;
  Vector position_;
  double height_;
  double thickness_;
  Color barkTint_;
  Color leafTint_;
};

// 共享的"树木模型"（内在状态）
class TreeModel {
private:
  Mesh mesh_;
  Texture bark_;
  Texture leaves_;
};

// 享元后的 Tree 类（只持有外在状态 + 指向共享模型的指针）
class Tree {
private:
  TreeModel* model_;

  Vector position_;
  double height_;
  double thickness_;
  Color barkTint_;
  Color leafTint_;
};

// —— 方案二：地形瓦片 ——

// 枚举方式（重构前）
enum Terrain {
  TERRAIN_GRASS,
  TERRAIN_HILL,
  TERRAIN_RIVER
  // Other terrains...
};

class World {
private:
  Terrain tiles_[WIDTH][HEIGHT];
};

int World::getMovementCost(int x, int y) {
  switch (tiles_[x][y])
  {
    case TERRAIN_GRASS: return 1;
    case TERRAIN_HILL:  return 3;
    case TERRAIN_RIVER: return 2;
      // Other terrains...
  }
}

bool World::isWater(int x, int y) {
  switch (tiles_[x][y])
  {
    case TERRAIN_GRASS: return false;
    case TERRAIN_HILL:  return false;
    case TERRAIN_RIVER: return true;
      // Other terrains...
  }
}

// 享元方式（重构后）

// 地形类型：所有属性都是内在状态，可以被所有同类型瓦片共享
class Terrain {
public:
  Terrain(int movementCost,
          bool isWater,
          Texture texture)
  : movementCost_(movementCost),
    isWater_(isWater),
    texture_(texture)
  {}

  int getMovementCost() const { return movementCost_; }
  bool isWater() const { return isWater_; }
  const Texture& getTexture() const { return texture_; }

private:
  int movementCost_;
  bool isWater_;
  Texture texture_;
};

// 游戏世界：瓦片网格中存储指针而非完整对象
class World {
public:
  World()
  : grassTerrain_(1, false, GRASS_TEXTURE),
    hillTerrain_(3, false, HILL_TEXTURE),
    riverTerrain_(2, true, RIVER_TEXTURE)
  {}

  void generateTerrain() {
    // Fill the ground with grass.
    for (int x = 0; x < WIDTH; x++)
    {
      for (int y = 0; y < HEIGHT; y++)
      {
        // Sprinkle some hills.
        if (random(10) == 0)
        {
          tiles_[x][y] = &hillTerrain_;
        }
        else
        {
          tiles_[x][y] = &grassTerrain_;
        }
      }
    }

    // Lay a river.
    int x = random(WIDTH);
    for (int y = 0; y < HEIGHT; y++) {
      tiles_[x][y] = &riverTerrain_;
    }
  }

  const Terrain& getTile(int x, int y) const {
    return *tiles_[x][y];
  }

private:
  Terrain* tiles_[WIDTH][HEIGHT];
  Terrain grassTerrain_;
  Terrain hillTerrain_;
  Terrain riverTerrain_;
};

// 使用示例：
// int cost = world.getTile(2, 3).getMovementCost();
```

## C# Equivalent (C# 对照实现)

```csharp
// 在 C# 中，享元模式的核心是分离内在状态和外在状态
// Unity 中常用 ScriptableObject 作为享元池——天然支持资源共享和序列化

// ============================================
// 方案一：树木渲染（GPU Instancing）
// ============================================

// 内在状态：使用 ScriptableObject 实现享元
// ScriptableObject 是 Unity 中创建可共享数据资产的推荐方式
// 它被保存在项目中，运行时所有引用指向同一份实例
// C++ 中的 TreeModel* 指针在 C# 中由 Unity 自动管理
[CreateAssetMenu(fileName = "TreeModel", menuName = "Game/Tree Model")]
public class TreeModel : ScriptableObject
{
    // 内在状态：所有树共享的网格和纹理
    // [SerializeField] 确保数据可在 Inspector 中编辑并持久化
    [SerializeField] private Mesh _mesh;
    [SerializeField] private Material _barkMaterial;  // 树皮材质
    [SerializeField] private Material _leavesMaterial; // 叶子材质

    // 公开只读属性（C# 表达式主体语法）
    public Mesh Mesh => _mesh;
    public Material BarkMaterial => _barkMaterial;
    public Material LeavesMaterial => _leavesMaterial;
}

// 外在状态：每棵树实例的独特数据（MonoBehaviour 挂载在场景中的 GameObject 上）
public class TreeInstance : MonoBehaviour
{
    // 引用共享的 ScriptableObject 享元
    // C++ 中是 TreeModel* 指针，C# 中直接引用对象（引用类型天然是指针语义）
    [SerializeField] private TreeModel _model; // 在 Inspector 中拖拽赋值

    // 外在状态：每个实例独有的数据
    [SerializeField] private float _height = 1f;
    [SerializeField] private float _thickness = 0.5f;
    [SerializeField] private Color _barkTint = Color.white;
    [SerializeField] private Color _leafTint = Color.white;

    // Unity 中利用 GPU Instancing 渲染大量树木
    // Graphics.DrawMeshInstanced 可以一次绘制所有共享同一 Mesh 和 Material 的实例
    // 这相当于享元模式的硬件级实现
    private void DrawWithInstancing()
    {
        if (_model == null || _model.Mesh == null) return;

        // 计算该树的变换矩阵（位置、旋转、缩放）
        Matrix4x4 matrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one * _height
        );

        // 将每个实例的矩阵收集到数组中
        // Unity 会将这些数据一次性发送到 GPU
        // C++ 需要手动管理顶点缓冲区，Unity 封装了这些底层细节
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BarkTint", _barkTint);
        block.SetColor("_LeafTint", _leafTint);

        // 注意：实际项目中应收集所有实例统一调用一次 DrawMeshInstanced
        // 这里仅为演示单棵树如何提供外在状态数据
        Graphics.DrawMesh(
            _model.Mesh,
            matrix,
            _model.BarkMaterial,
            0,
            null,
            0,
            block
        );
    }
}

// ============================================
// 方案二：地形瓦片（基于 ScriptableObject 的享元池）
// ============================================

// C++ 中 Terrain 类的 C# 等价实现
// 使用 ScriptableObject 而不是普通 class，因为：
// 1. 可序列化存储在项目中，作为资源使用
// 2. 运行时所有引用共享同一实例，天然享元
// 3. 可在 Inspector 中可视化编辑
[CreateAssetMenu(fileName = "TerrainType", menuName = "Game/Terrain Type")]
public class TerrainType : ScriptableObject
{
    // 内在状态：所有同类型瓦片共享的属性
    // 这些数据在运行时不可修改（只读），符合享元不可变原则
    [SerializeField] private int _movementCost = 1;
    [SerializeField] private bool _isWater;
    [SerializeField] private Texture2D _texture;  // Unity 使用 Texture2D 而非 Texture

    // C# 属性封装字段（替代 C++ 的 getter 方法）
    public int MovementCost => _movementCost;
    public bool IsWater => _isWater;
    public Texture2D Texture => _texture;

    // 可选：为每种地形添加额外游戏逻辑
    public float GetSpeedModifier(UnitType unitType)
    {
        // 不同单位在不同地形上的速度加成
        // 此方法虽无内在状态参数，但行为完全由内在状态决定
        return _isWater && unitType == UnitType.Boat ? 1.5f : 1.0f;
    }
}

// 游戏世界：使用享元模式管理瓦片网格
public class GameWorld : MonoBehaviour
{
    // —— 享元池 ——
    // 在 Inspector 中拖拽赋值，运行时这些 ScriptableObject 实例被所有瓦片共享
    // C++ 中需要手动声明 Terrain 成员变量并取地址
    // C# 中引用类型本身即为指针，ScriptableObject 自动管理生命周期
    [SerializeField] private TerrainType _grassTerrain;  // 所有草地瓦片共享此对象
    [SerializeField] private TerrainType _hillTerrain;    // 所有丘陵瓦片共享此对象
    [SerializeField] private TerrainType _riverTerrain;   // 所有河流瓦片共享此对象

    // C++ 中使用 Terrain* tiles_[WIDTH][HEIGHT]
    // C# 中引用类型数组天然存储引用（指针），等价于指针数组
    // 注意：C# 二维数组是 jagged array（交错数组）或 rectangular array（矩形数组）
    // 这里使用 rectangular array [,] 更符合 C++ 的连续内存布局
    private TerrainType[,] _tiles;
    [SerializeField] private int _width = 100;
    [SerializeField] private int _height = 100;

    private void Awake()
    {
        // 初始化瓦片网格
        _tiles = new TerrainType[_width, _height];

        // 填充地形（使用享元共享实例）
        // 与 C++ generateTerrain() 逻辑一致
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                // 所有草地块引用同一个 _grassTerrain 对象
                // C++ 中取地址 &grassTerrain_，C# 中直接赋值引用
                _tiles[x, y] = _grassTerrain;

                // 随机放置丘陵
                if (Random.Range(0, 10) == 0)
                {
                    _tiles[x, y] = _hillTerrain; // 共享丘陵实例
                }
            }
        }

        // 挖一条河流
        int riverX = Random.Range(0, _width);
        for (int y = 0; y < _height; y++)
        {
            _tiles[riverX, y] = _riverTerrain; // 共享河流实例
        }
    }

    // 公开访问瓦片数据的 API
    // C++ 返回 const Terrain&，C# 返回引用类型（默认是引用语义）
    public TerrainType GetTile(int x, int y)
    {
        // 范围检查（C++ 中通常不做，C# 推荐防御性编程）
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return null;
        return _tiles[x, y];
    }

    // 使用享元的优势：查询瓦片属性无需 switch-case
    // C++ 中 getMovementCost 方法需要 switch 枚举，这里直接调用对象方法
    public int GetMovementCostAt(int x, int y)
    {
        TerrainType tile = GetTile(x, y);
        return tile?.MovementCost ?? int.MaxValue; // null 合并运算符
    }
}

// ============================================
// 方案三：粒子特效中的享元模式
// ============================================
// Unity 的 ParticleSystem 内部即采用享元思想：
// 所有粒子共享同一个材质和纹理，每个粒子只维护位置/速度/颜色等外在状态

// 自定义粒子享元示例：
public class ParticleFlyweight
{
    // 内在状态（共享的）
    public Mesh SharedMesh { get; }
    public Material SharedMaterial { get; }
    public float Lifetime { get; }

    public ParticleFlyweight(Mesh mesh, Material material, float lifetime)
    {
        SharedMesh = mesh;
        SharedMaterial = material;
        Lifetime = lifetime;
    }
}

public class ParticleInstance
{
    private readonly ParticleFlyweight _flyweight; // 共享享元

    // 外在状态（每个粒子独有的）
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Age { get; set; }
    public Color Tint { get; set; }

    public ParticleInstance(ParticleFlyweight flyweight)
    {
        _flyweight = flyweight;
    }

    public void Update(float deltaTime)
    {
        Age += deltaTime;
        Position += Velocity * deltaTime;
    }

    // 渲染时使用享元的外在数据和内在数据
    public void Render()
    {
        // _flyweight.SharedMesh, _flyweight.SharedMaterial 是全局共享的
        // Position, Tint 是每个粒子独有的
    }
}

// ============================================
// 享元工厂：管理享元对象的创建和复用
// ============================================
// C++ 中需要手动管理享元池生命周期
// C# 中使用 Dictionary 作为享元工厂，按需创建并缓存

public class TerrainFactory
{
    // 享元池：使用 Dictionary 缓存已创建的 TerrainType
    // Key 是地形名称，Value 是共享的 ScriptableObject 实例
    private readonly Dictionary<string, TerrainType> _cache =
        new Dictionary<string, TerrainType>();

    // 工厂方法：获取享元。如果已存在则返回缓存实例，否则创建新实例。
    // 这正是 C++ 原书"See Also"中提到的 Factory Method 模式
    public TerrainType GetTerrain(string name, int movementCost, bool isWater)
    {
        if (_cache.TryGetValue(name, out TerrainType existing))
        {
            // 已存在则复用（享元核心逻辑）
            Debug.Log($"Reusing existing terrain: {name}");
            return existing;
        }

        // 不存在则创建并加入缓存
        TerrainType terrain = ScriptableObject.CreateInstance<TerrainType>();
        // 注意：实际项目中应通过 AssetDatabase 加载已有资源
        // 这里仅作演示
        _cache[name] = terrain;
        Debug.Log($"Created new terrain: {name}");
        return terrain;
    }
}
```

## Unity Application / Unity 应用场景

- **GPU Instancing（GPU 实例化）：** Unity 的 `Graphics.DrawMeshInstanced` 和 `Graphics.DrawMeshInstancedIndirect` 是享元模式的硬件实现。所有实例共享同一 Mesh 和 Material（内在状态），每帧只传递一个变换矩阵数组（外在状态），大幅减少 Draw Call。
- **ScriptableObject 资源复用：** 将物品数据、角色配置、技能参数等定义为 `ScriptableObject`，所有引用共享同一实例。修改一处即全局生效，且不增加额外内存。这在 C++ 中需要手动管理共享指针。
- **Tilemap 系统：** Unity 的 Tilemap + TileBase 方案本质是享元模式。每个 `Tile` 是一个共享的 `ScriptableObject`，Tilemap 网格中只存储 `Tile` 引用和少量变换数据。千万级瓦片的世界只需少量 Tile 资产实例。

## Key Differences / 关键差异

| C++ | C# |
|-----|----|
| 手动管理共享指针，需确保生命周期（谁负责 delete） | GC 自动管理引用计数，`ScriptableObject` 生命周期由 Unity 引擎控制 |
| `Terrain* tiles_[WIDTH][HEIGHT]` 指针数组 | `TerrainType[,]` 引用类型数组，天然存储引用（等价于指针） |
| 享元对象在栈上或堆上分配 | `ScriptableObject` 继承自 `UnityEngine.Object`，由 Unity 资源系统管理 |
| 内在状态通过 `const` 方法保证不可变 | C# 中只读属性 `=>` 表达式 + `[SerializeField]` 内部赋值确保不可变性 |
| 硬件 Instancing 需手动管理 Vertex Buffer + Instance Buffer | `Graphics.DrawMeshInstanced` 封装底层细节，开发者只需提供矩阵数组 |
| 无内置序列化，网络传输需自行处理 | `ScriptableObject` 可被 Unity 序列化系统管理，支持 AssetBundle 热更新 |
| 工厂模式中享元池需手写链表/vector | `Dictionary<TKey, TValue>` 泛型集合开箱即用，作为享元缓存 |

## See Also / 扩展阅读

- In the tile example, we just eagerly created an instance for each terrain type and stored it in `World`. That made it easy to find and reuse the shared instances. In many cases, though, you won't want to create *all* of the flyweights up front.

- 在瓦片示例中，我们提前为每种地形类型创建了一个实例并存储在 `World` 中。这使得查找和复用共享实例变得容易。但在许多情况下，你不需要预先创建*所有*享元。

If you can't predict which ones you actually need, it's better to create them on demand. To get the advantage of sharing, when you request one, you first see if you've already created an identical one. If so, you just return that instance.

如果你无法预测实际需要哪些享元，最好按需创建它们。为了获得共享的优势，当请求一个享元时，首先检查是否已经创建了相同的实例。如果是，直接返回该实例。

This usually means that you have to encapsulate construction behind some interface that can first look for an existing object. Hiding a constructor like this is an example of the [Factory Method](http://en.wikipedia.org/wiki/Factory_method_pattern) pattern.

这通常意味着你需要将构造过程封装在某个可以首先查找现有对象的接口后面。像这样隐藏构造函数是工厂方法模式的一个例子。

- In order to return a previously created flyweight, you'll have to keep track of the pool of ones that you've already instantiated. As the name implies, that means that an [object pool](object-pool.html) might be a helpful place to store them.

- 为了返回之前创建的享元，你需要跟踪已经实例化的享元池。顾名思义，这意味着对象池可能是存储它们的有用场所。

- When you're using the [State](state.html) pattern, you often have "state" objects that don't have any fields specific to the machine that the state is being used in. The state's identity and methods are enough to be useful. In that case, you can apply this pattern and reuse that same state instance in multiple state machines at the same time without any problems.

- 当你使用状态模式时，通常会有"状态"对象，它们没有任何特定于使用该状态的机器的字段。状态的标识和方法就足以发挥作用。在这种情况下，你可以应用此模式，同时在多个状态机中复用相同的状态实例，而不会出现任何问题。

---

# Observer Pattern — 观察者模式

> **EN:** You can't throw a rock at a computer without hitting an application built using the Model-View-Controller architecture, and underlying that is the Observer pattern. Observer is so pervasive that Java put it in its core library (`java.util.Observer`) and C# baked it right into the *language* (the `event` keyword).
>
> Like so many things in software, MVC was invented by Smalltalkers in the seventies. Lispers probably claim they came up with it in the sixties but didn't bother writing it down.
>
> Observer is one of the most widely used and widely known of the original Gang of Four patterns, but the game development world can be strangely cloistered at times, so maybe this is all news to you. In case you haven't left the abbey in a while, let me walk you through a motivating example.
>
> **CN:** 观察者模式是 Model-View-Controller 架构的底层基础，它是如此普及以至于 Java 将其放入核心库，C# 将其直接内建为语言特性（`event` 关键字）。观察者是 GoF 原始设计模式中应用最广泛、最知名的模式之一。
## Intent / 意图

* Define a one-to-many dependency between objects so that when one object changes state, all its dependents are notified and updated automatically.

The Observer pattern lets one piece of code announce that something interesting happened *without actually caring who receives the notification*.

**CN:** 定义对象之间一对多的依赖关系，当一个对象状态发生变化时，所有依赖于它的对象都会自动收到通知并更新。观察者模式允许一段代码宣布发生了某些有趣的事情，而无需关心谁会收到通知。

## Motivation / 动机

### Achievement Unlocked

**"太慢了"** — 发送通知只是遍历列表并调用一些虚方法。在除最追求性能的代码路径之外，这个成本可以忽略不计。

Say we're adding an achievements system to our game. It will feature dozens of different badges players can earn for completing specific milestones like "Kill 100 Monkey Demons", "Fall off a Bridge", or "Complete a Level Wielding Only a Dead Weasel".

**"动态分配太多了"** — 分配仅发生在注册观察者时；发送通知不需要任何内存分配，只是方法调用。如果这是问题，可以通过链表方式实现零动态分配的注册/注销。

![Achievement: Weasel Wielder](images/observer-weasel-wielder.png)

**销毁主体和观察者** — 当删除观察者时，主体可能仍持有指向它的悬空指针。观察者需要在析构函数中从主体取消注册。更安全的方案是在基类观察者中自动处理取消注册逻辑。

I swear I had no double meaning in mind when I drew this.

**延迟监听问题（Lapsed Listener Problem）** — 当 UI 界面关闭但未取消注册观察者时，GC 因主体仍持有引用而无法回收 UI 对象，导致僵尸对象持续接收通知浪费 CPU。解决方案是严格管理注销，或使用弱事件模式（WeakEvent）。

This is tricky to implement cleanly since we have such a wide range of achievements that are unlocked by all sorts of different behaviors. If we aren't careful, tendrils of our achievement system will twine their way through every dark corner of our codebase. Sure, "Fall off a Bridge" is somehow tied to the physics engine, but do we really want to see a call to `unlockFallOffBridge()` right in the middle of the linear algebra in our collision resolution algorithm?

**隐式耦合（What's going on?）** — 观察者模式在运行时通过观察者列表解耦，但也使通信流程难以静态追踪。如果某个 bug 跨越观察者链，你无法通过 IDE 直接跳转到方法调用处，而必须通过运行时状态来推断通知的接收者。

This is a rhetorical question. No self-respecting physics programmer would ever let us sully their beautiful mathematics with something as pedestrian as *gameplay*.

What we'd like, as always, is to have all the code concerned with one facet of the game nicely lumped in one place. The challenge is that achievements are triggered by a bunch of different aspects of gameplay. How can that work without coupling the achievement code to all of them?

That's what the observer pattern is for. It lets one piece of code announce that something interesting happened *without actually caring who receives the notification*.

For example, we've got some physics code that handles gravity and tracks which bodies are relaxing on nice flat surfaces and which are plummeting toward sure demise. To implement the "Fall off a Bridge" badge, we could just jam the achievement code right in there, but that's a mess. Instead, we can just do:

```cpp
void Physics::updateEntity(Entity& entity) {
  bool wasOnSurface = entity.isOnSurface();
  entity.accelerate(GRAVITY);
  entity.update();
  if (wasOnSurface && !entity.isOnSurface())
  {
    notify(entity, EVENT_START_FALL);
  }
}
```

All it does is say, "Uh, I don't know if anyone cares, but this thing just fell. Do with that as you will."

The physics engine does have to decide what notifications to send, so it isn't entirely decoupled. But in architecture, we're most often trying to make systems *better*, not *perfect*.

The achievement system registers itself so that whenever the physics code sends a notification, the achievement system receives it. It can then check to see if the falling body is our less-than-graceful hero, and if his perch prior to this new, unpleasant encounter with classical mechanics was a bridge. If so, it unlocks the proper achievement with associated fireworks and fanfare, and it does all of this with no involvement from the physics code.

In fact, we can change the set of achievements or tear out the entire achievement system without touching a line of the physics engine. It will still send out its notifications, oblivious to the fact that nothing is receiving them anymore.

Of course, if we *permanently* remove achievements and nothing else ever listens to the physics engine's notifications, we may as well remove the notification code too. But during the game's evolution, it's nice to have this flexibility.

**CN:** 假设我们要为游戏添加一个成就系统，包含数十种不同的徽章，玩家通过完成特定里程碑来解锁，例如"杀死 100 只猴魔"、"从桥上坠落"或"仅使用死鼬鼠完成关卡"。由于成就由各种不同的行为触发，干净地实现这一点很棘手。如果不小心，成就系统的触角将蔓延到代码库的每个角落。

观察者模式正是为此而生——它让一段代码宣布"发生了有趣的事情"，而无需关心谁会收到通知。物理引擎只需要说"我不知道有没有人在乎，但这个东西刚才掉下去了"，成就系统注册自身来接收这个通知，然后决定是否解锁成就。

## The Pattern / 模式描述

### How it Works

If you don't already know how to implement the pattern, you could probably guess from the previous description, but to keep things easy on you, I'll walk through it quickly.

#### The observer

We'll start with the nosy class that wants to know when another object does something interesting. These inquisitive objects are defined by this interface:

```cpp
class Observer {
public:
  virtual ~Observer() {}
  virtual void onNotify(const Entity& entity, Event event) = 0;
};
```

The parameters to `onNotify()` are up to you. That's why this is the Observer *pattern* and not the Observer "ready-made code you can paste into your game". Typical parameters are the object that sent the notification and a generic "data" parameter you stuff other details into.

If you're coding in a language with generics or templates, you'll probably use them here, but it's also fine to tailor them to your specific use case. Here, I'm just hardcoding it to take a game entity and an enum that describes what happened.

Any concrete class that implements this becomes an observer. In our example, that's the achievement system, so we'd have something like so:

```cpp
class Achievements : public Observer {
public:
  virtual void onNotify(const Entity& entity, Event event)
  {
    switch (event)
    {
    case EVENT_ENTITY_FELL:
      if (entity.isHero() && heroIsOnBridge_)
      {
        unlock(ACHIEVEMENT_FELL_OFF_BRIDGE);
      }
      break;

// Handle other events, and update heroIsOnBridge_...
    }
  }

private:
  void unlock(Achievement achievement)
  {
    // Unlock if not already unlocked...
  }

bool heroIsOnBridge_;
};
```

#### The subject

The notification method is invoked by the object being observed. In Gang of Four parlance, that object is called the "subject". It has two jobs. First, it holds the list of observers that are waiting oh-so-patiently for a missive from it:

```cpp
class Subject {
private:
  Observer* observers_[MAX_OBSERVERS];
  int numObservers_;
};
```

In real code, you would use a dynamically-sized collection instead of a dumb array. I'm sticking with the basics here for people coming from other languages who don't know C++'s standard library.

The important bit is that the subject exposes a *public* API for modifying that list:

```cpp
class Subject {
public:
  void addObserver(Observer* observer)
  {
    // Add to array...
  }

void removeObserver(Observer* observer)
  {
    // Remove from array...
  }

// Other stuff...
};
```

That allows outside code to control who receives notifications. The subject communicates with the observers, but it isn't *coupled* to them. In our example, no line of physics code will mention achievements. Yet, it can still talk to the achievements system. That's the clever part about this pattern.

It's also important that the subject has a *list* of observers instead of a single one. It makes sure that observers aren't implicitly coupled to *each other*. For example, say the audio engine also observes the fall event so that it can play an appropriate sound. If the subject only supported one observer, when the audio engine registered itself, that would *un*\-register the achievements system.

That means those two systems would interfere with each other — and in a particularly nasty way, since the second would disable the first. Supporting a list of observers ensures that each observer is treated independently from the others. As far as they know, each is the only thing in the world with eyes on the subject.

The other job of the subject is sending notifications:

```cpp
class Subject {
protected:
  void notify(const Entity& entity, Event event)
  {
    for (int i = 0; i < numObservers_; i++)
    {
      observers_[i]->onNotify(entity, event);
    }
  }

// Other stuff...
};
```

Note that this code assumes observers don't modify the list in their `onNotify()` methods. A more robust implementation would either prevent or gracefully handle concurrent modification like that.

#### Observable physics

Now, we just need to hook all of this into the physics engine so that it can send notifications and the achievement system can wire itself up to receive them. We'll stay close to the original *Design Patterns* recipe and inherit `Subject`:

```cpp
class Physics : public Subject {
public:
  void updateEntity(Entity& entity);
};
```

This lets us make `notify()` in `Subject` protected. That way the derived physics engine class can call it to send notifications, but code outside of it cannot. Meanwhile, `addObserver()` and `removeObserver()` are public, so anything that can get to the physics system can observe it.

In real code, I would avoid using inheritance here. Instead, I'd make `Physics` *have* an instance of `Subject`. Instead of observing the physics engine itself, the subject would be a separate "falling event" object. Observers could register themselves using something like:

```cpp
physics.entityFell()
  .addObserver(this);
```

To me, this is the difference between "observer" systems and "event" systems. With the former, you observe *the thing that did something interesting*. With the latter, you observe an object that represents *the interesting thing that happened*.

Now, when the physics engine does something noteworthy, it calls `notify()` like in the motivating example before. That walks the observer list and gives them all the heads up.

![A Subject containing a list of Observer pointers. The first two point to Achievements and Audio.](images/observer-list.png)

Pretty simple, right? Just one class that maintains a list of pointers to instances of some interface. It's hard to believe that something so straightforward is the communication backbone of countless programs and app frameworks.

But the Observer pattern isn't without its detractors. When I've asked other game programmers what they think about this pattern, they bring up a few complaints. Let's see what we can do to address them, if anything.

**CN:** 模式的工作方式：从好奇的观察者类开始，它们想要知道其他对象何时做了有趣的事情。这些好问的对象通过 `Observer` 接口定义。被观察的对象称为"主体"（Subject），它持有等待通知的观察者列表，并暴露公共 API 来修改该列表。主体与观察者通信，但不与它们耦合——这正是此模式的精妙之处。

## C++ Code (原书代码)

```cpp
// Base Observer interface
class Observer {
public:
  virtual ~Observer() {}
  virtual void onNotify(const Entity& entity, Event event) = 0;
};

// Concrete observer - Achievement system
class Achievements : public Observer {
public:
  virtual void onNotify(const Entity& entity, Event event)
  {
    switch (event)
    {
    case EVENT_ENTITY_FELL:
      if (entity.isHero() && heroIsOnBridge_)
      {
        unlock(ACHIEVEMENT_FELL_OFF_BRIDGE);
      }
      break;

// Handle other events, and update heroIsOnBridge_...
    }
  }

private:
  void unlock(Achievement achievement)
  {
    // Unlock if not already unlocked...
  }

bool heroIsOnBridge_;
};

// Subject with fixed array of observers
class Subject {
public:
  void addObserver(Observer* observer)
  {
    // Add to array...
  }

void removeObserver(Observer* observer)
  {
    // Remove from array...
  }

protected:
  void notify(const Entity& entity, Event event)
  {
    for (int i = 0; i < numObservers_; i++)
    {
      observers_[i]->onNotify(entity, event);
    }
  }

// Other stuff...

private:
  Observer* observers_[MAX_OBSERVERS];
  int numObservers_;
};

// Physics engine inheriting Subject
class Physics : public Subject {
public:
  void updateEntity(Entity& entity);
};

void Physics::updateEntity(Entity& entity) {
  bool wasOnSurface = entity.isOnSurface();
  entity.accelerate(GRAVITY);
  entity.update();
  if (wasOnSurface && !entity.isOnSurface())
  {
    notify(entity, EVENT_START_FALL);
  }
}

// -------- Linked-list variant (no dynamic allocation) --------

class Subject {
  Subject()
  : head_(NULL)
  {}

// Methods...
private:
  Observer* head_;
};

class Observer {
  friend class Subject;

public:
  Observer()
  : next_(NULL)
  {}

// Other stuff...
private:
  Observer* next_;
};

void Subject::addObserver(Observer* observer) {
  observer->next_ = head_;
  head_ = observer;
}

void Subject::removeObserver(Observer* observer) {
  if (head_ == observer)
  {
    head_ = observer->next_;
    observer->next_ = NULL;
    return;
  }

Observer* current = head_;
  while (current != NULL)
  {
    if (current->next_ == observer)
    {
      current->next_ = observer->next_;
      observer->next_ = NULL;
      return;
    }

current = current->next_;
  }
}

void Subject::notify(const Entity& entity, Event event) {
  Observer* observer = head_;
  while (observer != NULL)
  {
    observer->onNotify(entity, event);
    observer = observer->next_;
  }
}

// -------- Non-intrusive variant with object pool (sketch) --------
// Each Subject has a linked list of separate "list node" objects.
// Each node contains a pointer to the Observer and a next_ pointer.
// Multiple nodes can point to the same Observer, so an Observer
// can observe multiple Subjects simultaneously.
// Nodes are pre-allocated in an Object Pool to avoid dynamic allocation.
```

## C# Equivalent (C# 对照实现)

```csharp
using System;
using System.Collections.Generic;

// 定义事件参数类型，携带通知数据
public struct EntityEvent
{
    public Entity Entity { get; }
    public EventType Type { get; }

public EntityEvent(Entity entity, EventType type)
    {
        Entity = entity;
        Type = type;
    }
}

public enum EventType
{
    EventEntityFell,
    EventEntityKilled,
    EventEntityDamaged
}

// 观察者接口——C# 中可选用，但 event 关键字更简洁
// 注意：C# 有内置 event 关键字，原生支持观察者模式
// 使用 event 比手动实现 Observer 接口更简洁
// C# 的事件委托机制自动处理 += 和 -= 注册/注销
public class Physics
{
    // 使用 EventHandler<T> 泛型委托作为事件声明
    // C# 的 event 关键字自动维护调用列表，无需手动管理 Observer 集合
    public event EventHandler<EntityEvent> OnEntityFall;

public void UpdateEntity(Entity entity)
    {
        bool wasOnSurface = entity.IsOnSurface;
        entity.Accelerate(Physics.Gravity);
        entity.Update();

// 如果实体从地面状态变为坠落状态，触发事件
        if (wasOnSurface && !entity.IsOnSurface)
        {
            // ?. 是 C# 的空条件运算符，没有订阅者时不会抛出异常
            // Invoke 方法同步调用所有已注册的事件处理程序
            OnEntityFall?.Invoke(this, new EntityEvent(entity, EventType.EventEntityFell));
        }
    }
}

// 成就系统——观察者具体实现
public class Achievements
{
    private bool heroIsOnBridge_;

// 事件处理方法签名必须匹配 EventHandler<EntityEvent> 委托
    // object sender: 事件发送者, EntityEvent e: 事件参数
    public void OnEntityFall(object sender, EntityEvent e)
    {
        if (e.Type == EventType.EventEntityFell && e.Entity.IsHero && heroIsOnBridge_)
        {
            Unlock(Achievement.AchievementFellOffBridge);
        }
        // 处理其他事件...
    }

private void Unlock(Achievement achievement)
    {
        // 解锁成就逻辑
        Console.WriteLine($"成就解锁: {achievement}");
    }
}

// 注册与注销事件
// Physics physics = new Physics();
// Achievements achievements = new Achievements();
// physics.OnEntityFall += achievements.OnEntityFall;  // 注册观察者
// physics.OnEntityFall -= achievements.OnEntityFall;  // 注销观察者

// 使用弱事件模式避免延迟监听问题（lapsed listener problem）
// 在 WPF 中可使用 WeakEvent 模式，或使用 IDisposable 模式管理事件生命周期
public class UIObserver : IDisposable
{
    private readonly Physics _physics;

public UIObserver(Physics physics)
    {
        _physics = physics;
        _physics.OnEntityFall += OnEntityFall;
    }

private void OnEntityFall(object sender, EntityEvent e)
    {
        // 更新 UI...
    }

// 实现 IDisposable 确保对象销毁时正确注销事件
    // 否则 subject 持有对 observer 的引用，会导致 GC 无法回收（延迟监听问题）
    public void Dispose()
    {
        _physics.OnEntityFall -= OnEntityFall;
    }
}

// 使用自定义委托替代 EventHandler（更灵活）
public class AchievementSystem
{
    // 自定义委托声明
    public delegate void AchievementEventHandler(object sender, AchievementEventArgs e);

// 也可以直接使用 Action<T> / Func<T> 泛型委托
    public event Action<Entity, EventType> OnAchievementEvent;

public void RaiseEvent(Entity entity, EventType type)
    {
        OnAchievementEvent?.Invoke(entity, type);
    }
}
```

## Unity Application / Unity 应用场景

* Unity provides several built-in mechanisms for the Observer pattern:

* Unity 提供了多种内置的观察者模式机制：

| Mechanism | Description | Use Case |
|---|---|---|
| **UnityEvent** | Serialized event system visible in Inspector | UI button clicks, modular decoupled systems |
| **C# event** | Native C# delegate-based events | In-code event communication |
| **SendMessage** | Reflection-based messaging (slow, avoid in hot paths) | Quick prototyping, legacy code |
| **UnityEngine.Events.UnityEvent** | Editable in Inspector with persistent listeners | Prefab wiring, designer-friendly events |

```csharp
// UnityEvent 示例——可在 Inspector 中拖拽绑定
using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    public float health = 100f;

// UnityEvent 支持 Inspector 序列化绑定
    // 设计师可以直接在编辑器中拖拽对象到监听器列表
    public UnityEvent<float> onHealthChanged;
    public UnityEvent onDeath;

public void TakeDamage(float damage)
    {
        health -= damage;
        // 触发事件，所有在 Inspector 中绑定的监听器都会收到通知
        onHealthChanged?.Invoke(health);

if (health <= 0f)
        {
            onDeath?.Invoke();
        }
    }
}

// 消息系统示例（事件中心模式）
public class EventDispatcher : MonoBehaviour
{
    // 使用字典管理事件，支持动态注册和注销
    private static Dictionary<GameEvent, Action> _eventTable = new();

public static void Register(GameEvent type, Action listener)
    {
        if (_eventTable.TryGetValue(type, out var existing))
            _eventTable[type] = existing + listener;  // 多播委托组合
        else
            _eventTable[type] = listener;
    }

public static void Unregister(GameEvent type, Action listener)
    {
        if (_eventTable.TryGetValue(type, out var existing))
            _eventTable[type] = existing - listener;  // 多播委托移除
    }

public static void Dispatch(GameEvent type)
    {
        if (_eventTable.TryGetValue(type, out var handler))
            handler?.Invoke();
    }
}
```

## When to Use / 使用场景

The Observer pattern fits best outside of hot code paths anyway, so you can usually afford the dynamic dispatch. Aside from that, there's virtually no overhead. We aren't allocating objects for messages. There's no queueing. It's just an indirection over a synchronous method call.

This pattern is a great way to let mostly unrelated lumps of code talk to each other without them merging into one big lump. It's less useful *within* a single lump of code dedicated to one feature or aspect.

That's why it fits our example well: achievements and physics are almost entirely unrelated domains, likely implemented by different people. We want the bare minimum of communication between them so that working on either one doesn't require much knowledge of the other.

If you often need to think about *both* sides of some communication in order to understand a part of the program, don't use the Observer pattern to express that linkage. Prefer something more explicit.

**CN:** 观察者模式最适合热代码路径之外的场景，动态分派的成本通常可以接受。它几乎没有额外开销——没有消息对象分配，没有队列，只是对同步方法调用的一层间接引用。

此模式非常适合让大部分不相关的代码块相互通信，而不会合并成一个大块。如果为了理解程序的某一部分，你经常需要同时考虑通信的两端，则不要使用观察者模式来表达这种关联，应选择更显式的方式。

## Keep in Mind / 注意事项

### "It's Too Slow"

I hear this a lot, often from programmers who don't actually know the details of the pattern. They have a default assumption that anything that smells like a "design pattern" must involve piles of classes and indirection and other creative ways of squandering CPU cycles.

The Observer pattern gets a particularly bad rap here because it's been known to hang around with some shady characters named "events", "messages", and even "data binding". Some of those systems *can* be slow (often deliberately, and for good reason). They involve things like queuing or doing dynamic allocation for each notification.

But, now that you've seen how the pattern is actually implemented, you know that isn't the case. Sending a notification is simply walking a list and calling some virtual methods. Granted, it's a *bit* slower than a statically dispatched call, but that cost is negligible in all but the most performance-critical code.

In fact, you have to be careful because the Observer pattern *is* synchronous. The subject invokes its observers directly, which means it doesn't resume its own work until all of the observers have returned from their notification methods. A slow observer can block a subject.

This sounds scary, but in practice, it's not the end of the world. It's just something you have to be aware of. UI programmers — who've been doing event-based programming like this for ages — have a time-worn motto for this: "stay off the UI thread".

If you're responding to an event synchronously, you need to finish and return control as quickly as possible so that the UI doesn't lock up. When you have slow work to do, push it onto another thread or a work queue.

You do have to be careful mixing observers with threading and explicit locks, though. If an observer tries to grab a lock that the subject has, you can deadlock the game. In a highly threaded engine, you may be better off with asynchronous communication using an Event Queue.

### "It Does Too Much Dynamic Allocation"

Whole tribes of the programmer clan — including many game developers — have moved onto garbage collected languages, and dynamic allocation isn't the boogie man that it used to be. But for performance-critical software like games, memory allocation still matters, even in managed languages. Dynamic allocation takes time, as does reclaiming memory, even if it happens automatically.

Many game developers are less worried about allocation and more worried about *fragmentation*. When your game needs to run continuously for days without crashing in order to get certified, an increasingly fragmented heap can prevent you from shipping.

The first thing to notice is that it only allocates memory when observers are being wired up. *Sending* a notification requires no memory allocation whatsoever — it's just a method call. If you hook up your observers at the start of the game and don't mess with them much, the amount of allocation is minimal.

If it's still a problem, you can implement adding and removing observers without any dynamic allocation at all using a linked list of observers (see the C++ Code section for the implementation).

### Destroying subjects and observers

What happens when you delete a subject or an observer? If you carelessly call `delete` on some observer, a subject may still have a pointer to it. That's now a dangling pointer into deallocated memory. When that subject tries to send a notification, well… let's just say you're not going to have a good time.

Not to point fingers, but I'll note that *Design Patterns* doesn't mention this issue at all.

Destroying the subject is easier since in most implementations, the observer doesn't have any references to it. But even then, sending the subject's bits to the memory manager's recycle bin may cause some problems. Those observers may still be expecting to receive notifications in the future, and they don't know that that will never happen now. They aren't observers at all, really, they just think they are.

You can deal with this in a couple of different ways. The simplest is to do what I did and just punt on it. It's an observer's job to unregister itself from any subjects when it gets deleted. More often than not, the observer *does* know which subjects it's observing, so it's usually just a matter of adding a `removeObserver()` call to its destructor.

As is often the case, the hard part isn't doing it, it's *remembering* to do it.

### The lapsed listener problem

Imagine this: you've got some UI screen that shows a bunch of stats about the player's character like their health and stuff. When the player brings up the screen, you instantiate a new object for it. When they close it, you just forget about the object and let the GC clean it up.

Every time the character takes a punch to the face (or elsewhere, I suppose), it sends a notification. The UI screen observes that and updates the little health bar. Great. Now what happens when the player dismisses the screen, but you don't unregister the observer?

The UI isn't visible anymore, but it won't get garbage collected since the character's observer list still has a reference to it. Every time the screen is loaded, we add a new instance of it to that increasingly long list.

The entire time the player is playing the game, running around, and getting in fights, the character is sending notifications that get received by *all* of those screens. They aren't on screen, but they receive notifications and waste CPU cycles updating invisible UI elements. If they do other things like play sounds, you'll get noticeably wrong behavior.

This is such a common issue in notification systems that it has a name: the *lapsed listener problem*. Since subjects retain references to their listeners, you can end up with zombie UI objects lingering in memory. The lesson here is to be disciplined about unregistration.

### What's going on? (Implicit coupling)

The other, deeper issue with the Observer pattern is a direct consequence of its intended purpose. We use it because it helps us loosen the coupling between two pieces of code. It lets a subject indirectly communicate with some observer without being statically bound to it.

This is a real win when you're trying to reason about the subject's behavior, and any hangers-on would be an annoying distraction. If you're poking at the physics engine, you really don't want your editor — or your mind — cluttered up with a bunch of stuff about achievements.

On the other hand, if your program isn't working and the bug spans some chain of observers, reasoning about that communication flow is much more difficult. With an explicit coupling, it's as easy as looking up the method being called. This is child's play for your average IDE since the coupling is static.

But if that coupling happens through an observer list, the only way to tell who will get notified is by seeing which observers happen to be in that list *at runtime*. Instead of being able to *statically* reason about the communication structure of the program, you have to reason about its *imperative, dynamic* behavior.
## Key Differences / 关键差异

| Aspect | C++ (GoF) | C# / Unity |
|---|---|---|
| **Interface** | Abstract class `Observer` with virtual `onNotify()` | `event` keyword + `EventHandler<T>` delegate |
| **Registration** | Manual `addObserver()` / `removeObserver()` | `+=` / `-=` operators on event |
| **Memory** | Manual list management (fixed array or linked list) | GC-managed; `WeakEvent` pattern for preventing leaks |
| **Multiple subjects** | Observer needs subject pointer in `onNotify()` | `sender` parameter provides subject reference |
| **Safety** | Dangling pointer risk when observer/subject deleted | `?.Invoke()` null-conditional; `IDisposable` for cleanup |
| **Unity-specific** | N/A | `UnityEvent` serialization, `SendMessage` reflection, `AnimationEvent` |

## See Also / 扩展阅读

### Observers Today

- **策略模式 (Strategy)** — 结构上与状态模式相似，但意图不同：策略用于解耦行为，而状态通过改变委托对象来改变行为。
- **类型对象模式 (Type Object)** — 另一种主对象委托给从属对象的模式；多个对象共享同一个类型对象的引用。
- **享元模式 (Flyweight)** — 当状态没有实例特定数据时，用于在多个 FSM 之间共享静态状态实例。
- **对象池模式 (Object Pool)** — 在动态分配状态对象时帮助管理碎片问题。
- **更新模式 (Update Method)** — 本章中提及，用于每帧状态更新（如蓄力计时）。
- **行为树 (Behavior Trees)** — 超越 FSM 的更强大的 AI 架构，现代游戏 AI 的趋势。
- **规划系统 (Planning Systems)** — 目标导向行动规划（GOAP）和其他基于规划的 AI 系统。

*Design Patterns* came out in 1994. Back then, object-oriented programming was *the* hot paradigm. Every programmer on Earth wanted to "Learn OOP in 30 Days," and middle managers paid them based on the number of classes they created. Engineers judged their mettle by the depth of their inheritance hierarchies.

---

That same year, Ace of Base had not one but *three* hit singles, so that may tell you something about our taste and discernment back then.

*This chapter is from **Game Programming Patterns** by Robert Nystrom. The full text is available at [gameprogrammingpatterns.com](https://gameprogrammingpatterns.com/state.html).*

The Observer pattern got popular during that zeitgeist, so it's no surprise that it's class-heavy. But mainstream coders now are more comfortable with functional programming. Having to implement an entire interface just to receive a notification doesn't fit today's aesthetic.

---

It feels heavyweight and rigid. It *is* heavyweight and rigid. For example, you can't have a single class that uses different notification methods for different subjects.

# Double Buffer — 双缓冲模式

A more modern approach is for an "observer" to be only a reference to a method or function. In languages with first-class functions, and especially ones with closures, this is a much more common way to do observers.

> **EN:** Cause a series of sequential operations to appear instantaneous or simultaneous.
> **CN:** 使一系列顺序操作看起来像是瞬间完成或同时发生。

If I were designing an observer system today, I'd make it function-based instead of class-based. Even in C++, I would tend toward a system that let you register member function pointers as observers instead of instances of some `Observer` interface.

### Observers Tomorrow

Event systems and other observer-like patterns are incredibly common these days. They're a well-worn path. But if you write a few large apps using them, you start to notice something. A lot of the code in your observers ends up looking the same. It's usually something like:

1. Get notified that some state has changed.
2. Imperatively modify some chunk of UI to reflect the new state.

It's all, "Oh, the hero health is 7 now? Let me set the width of the health bar to 70 pixels." After a while, that gets pretty tedious. Computer science academics and software engineers have been trying to eliminate that tedium for a *long* time. Their attempts have gone under a number of different names: "dataflow programming", "functional reactive programming", etc.

While there have been some successes, usually in limited domains like audio processing or chip design, the Holy Grail still hasn't been found. In the meantime, a less ambitious approach has started gaining traction. Many recent application frameworks now use "data binding".

Unlike more radical models, data binding doesn't try to entirely eliminate imperative code and doesn't try to architect your entire application around a giant declarative dataflow graph. What it does do is automate the busywork where you're tweaking a UI element or calculated property to reflect a change to some value.

Like other declarative systems, data binding is probably a bit too slow and complex to fit inside the core of a game engine. But I would be surprised if I didn't see it start making inroads into less critical areas of the game like UI.

In the meantime, the good old Observer pattern will still be here waiting for us. Sure, it's not as exciting as some hot technique that manages to cram both "functional" and "reactive" in its name, but it's dead simple and it works. To me, those are often the two most important criteria for a solution.

### Related patterns from the book

- **Event Queue** — For asynchronous communication in highly-threaded engines, use an Event Queue instead of synchronous Observer notifications.
- **Object Pool** — Pre-allocate list nodes in an object pool to avoid dynamic allocation in observer registration.
- **Chain of Responsibility** — If an observer can stop notification propagation, you're close to the Chain of Responsibility pattern.
- **Flyweight** — Previous chapter in *Game Programming Patterns*.
- **Prototype** — Next chapter in *Game Programming Patterns*.
- **Model-View-Controller** — Underlying architectural pattern built on Observer.
- **java.util.Observer** / **java.util.Observable** — Java's built-in Observer support.
- **C# `event` keyword** — C# bakes the Observer pattern into the language with delegate-based events.

**CN:** 《设计模式》出版于 1994 年，当时面向对象编程是热门范式，观察者模式在那个时代流行起来，因此自然会偏重类结构。如今更现代的方式是让"观察者"仅作为方法或函数的引用——在拥有一等函数和闭包的语言中，这是更常见的做法。

未来的趋势是数据绑定（data binding）和函数式响应式编程（FRP），它们试图自动化"状态变化后更新 UI"的样板代码。但对于游戏引擎的核心部分，数据绑定可能太慢太复杂；然而在 UI 等次要领域，它已经开始被采用。

与此同时，经典的观察者模式仍然有效——它极其简单且行之有效。

---

*Content source: [Game Programming Patterns — Observer](https://gameprogrammingpatterns.com/observer.html) by Robert Nystrom*

---

# Prototype Pattern — 原型模式

> **EN:** Specify the kinds of objects to create using a prototypical instance, and create new objects by copying this prototype.
> **CN:** 使用原型实例指定要创建的对象类型，并通过复制这个原型来创建新对象。

## Intent / 意图

* The first time I heard the word "prototype" was in *Design Patterns*. Today, it seems like everyone is saying it, but it turns out they aren't talking about the design pattern. We'll cover that here, but I'll also show you other, more interesting places where the term "prototype" and the concepts behind it have popped up. But first, let's revisit the original pattern.

I don't say "original" lightly here. *Design Patterns* cites Ivan Sutherland's legendary Sketchpad project in *1963* as one of the first examples of this pattern in the wild. While everyone else was listening to Dylan and the Beatles, Sutherland was busy just, you know, inventing the basic concepts of CAD, interactive graphics, and object-oriented programming.

The key idea is that *an object can spawn other objects similar to itself*.

**CN:** 我第一次听到"原型"这个词是在《设计模式》这本书中。如今，似乎每个人都在谈论它，但结果他们说的并不是那个设计模式。我们将在这里介绍它，但我也会展示"原型"这个概念出现过的其他更有趣的地方。但首先，让我们回顾一下最初的模式。

我在这里说"最初"可不是随便说的。《设计模式》引用了 Ivan Sutherland 在 1963 年创建的传奇 Sketchpad 项目，作为该模式在现实中的最早案例之一。当其他人都在听 Dylan 和 Beatles 的时候，Sutherland 却忙于发明 CAD、交互图形和面向对象编程的基本概念。

核心思想是：*一个对象可以生成与自身相似的其他对象*。

## Motivation / 动机

* Pretend we're making a game in the style of Gauntlet. We've got creatures and fiends swarming around the hero, vying for their share of his flesh. These unsavory dinner companions enter the arena by way of "spawners", and there is a different spawner for each kind of enemy.

For the sake of this example, let's say we have different classes for each kind of monster in the game — `Ghost`, `Demon`, `Sorcerer`, etc., like:

```cpp
class Monster {
  // Stuff...
};

class Ghost : public Monster {};
class Demon : public Monster {};
class Sorcerer : public Monster {};
```

A spawner constructs instances of one particular monster type. To support every monster in the game, we *could* brute-force it by having a spawner class for each monster class, leading to a parallel class hierarchy:

![Parallel class hierarchies. Ghost, Demon, and Sorceror all inherit from Monster. GhostSpawner, DemonSpawner, and SorcerorSpawner inherit from Spawner.]

Implementing it would look like this:

```cpp
class Spawner {
public:
  virtual ~Spawner() {}
  virtual Monster* spawnMonster() = 0;
};

class GhostSpawner : public Spawner {
public:
  virtual Monster* spawnMonster()
  {
    return new Ghost();
  }
};

class DemonSpawner : public Spawner {
public:
  virtual Monster* spawnMonster()
  {
    return new Demon();
  }
};

// You get the idea...
```

Unless you get paid by the line of code, this is obviously not a fun way to hack this together. Lots of classes, lots of boilerplate, lots of redundancy, lots of duplication, lots of repeating myself…

The Prototype pattern offers a solution. The key idea is that *an object can spawn other objects similar to itself*. If you have one ghost, you can make more ghosts from it. If you have a demon, you can make other demons. Any monster can be treated as a *prototypal* monster used to generate other versions of itself.

To implement this, we give our base class, `Monster`, an abstract `clone()` method:

```cpp
class Monster {
public:
  virtual ~Monster() {}
  virtual Monster* clone() = 0;

// Other stuff...
};
```

Each monster subclass provides an implementation that returns a new object identical in class and state to itself. For example:

```cpp
class Ghost : public Monster {
public:
  Ghost(int health, int speed)
  : health_(health),
    speed_(speed)
  {}

virtual Monster* clone()
  {
    return new Ghost(health_, speed_);
  }

private:
  int health_;
  int speed_;
};
```

Once all our monsters support that, we no longer need a spawner class for each monster class. Instead, we define a single one:

```cpp
class Spawner {
public:
  Spawner(Monster* prototype)
  : prototype_(prototype)
  {}

Monster* spawnMonster()
  {
    return prototype_->clone();
  }

private:
  Monster* prototype_;
};
```

It internally holds a monster, a hidden one whose sole purpose is to be used by the spawner as a template to stamp out more monsters like it, sort of like a queen bee who never leaves the hive.

To create a ghost spawner, we create a prototypal ghost instance and then create a spawner holding that prototype:

```cpp
Monster* ghostPrototype = new Ghost(15, 3);
Spawner* ghostSpawner = new Spawner(ghostPrototype);
```

One neat part about this pattern is that it doesn't just clone the *class* of the prototype, it clones its *state* too. This means we could make a spawner for fast ghosts, weak ghosts, or slow ghosts just by creating an appropriate prototype ghost.

I find something both elegant and yet surprising about this pattern. I can't imagine coming up with it myself, but I can't imagine *not* knowing about it now that I do.

**CN:** 假设我们在制作一款类似 Gauntlet 风格的游戏。游戏中充满了蜂拥而至的怪物，它们围绕着英雄，争相分食他的血肉。这些不受欢迎的"晚餐伙伴"通过"生成器"进入竞技场，每种敌人都有不同的生成器。

为了这个例子，假设每种怪物都有独立的类——比如 `Ghost`、`Demon`、`Sorcerer` 等等：

一个生成器构建特定怪物类型的实例。为了支持游戏中的每种怪物，我们可以粗暴地为每个怪物类创建一个生成器类，导致平行的类层次结构：

实现起来是这样的：

除非你按代码行数收费，否则这显然不是一种有趣的实现方式。太多的类、太多的模板代码、太多的冗余、太多的重复、太多重复自己了……

原型模式提供了一个解决方案。核心思想是：*一个对象可以生成与自身相似的其他对象*。如果你有一个幽灵，你可以从中制造出更多幽灵。如果你有一个恶魔，你可以制造出更多恶魔。任何怪物都可以被视为一个"原型"怪物，用来生成它自身的其他版本。

为了实现这一点，我们给基类 `Monster` 添加一个抽象的 `clone()` 方法：

每个怪物子类提供一个实现，返回一个与自身在类和状态上都相同的新对象。例如：

一旦我们所有的怪物都支持这一点，我们就不再需要为每个怪物类创建一个生成器类了。相反，我们定义一个单一的生成器：

它在内部持有一个怪物——一个隐藏的怪物，其唯一目的是被生成器用作模板，来 stamp 出更多像它一样的怪物，有点像永远不离开蜂巢的蜂后。

要创建一个幽灵生成器，我们先创建一个原型幽灵实例，然后创建一个持有该原型的生成器：

这个模式的一个巧妙之处在于，它不仅仅克隆原型的*类*，还克隆了它的*状态*。这意味着我们可以通过创建适当的原型幽灵，来为快速幽灵、弱幽灵或慢速幽灵创建生成器。

我觉得这个模式既优雅又令人惊讶。我无法想象自己能想到它，但我现在也无法想象*不知道*它会怎样。

## The Pattern / 模式描述

* The key idea is that *an object can spawn other objects similar to itself*.

The Prototype pattern allows an object to spawn other objects similar to itself, avoiding a parallel class hierarchy of factories. It enables creating new objects by cloning a prototypal instance. Instead of having a separate factory class for each type of object, you create a single generic spawner that clones a stored prototype instance. This approach supports reuse of both class and state — the prototype isn't just a template for the class of object to create, but also carries default state that is copied into each clone.

**CN:** 核心思想是：*一个对象可以生成与自身相似的其他对象*。

原型模式允许一个对象生成与自身相似的其他对象，避免了平行的工厂类层次结构。它通过克隆一个原型实例来创建新对象。你不需要为每种对象类型创建独立的工厂类，而是创建一个单一的通用生成器来克隆存储的原型实例。这种方法支持类和状态的双重复用——原型不仅是要创建的对象类型的模板，还携带了会被复制到每个克隆体中的默认状态。

## When to Use / 使用场景

* Well, we don't have to create a separate spawner class for each monster, so that's good. But we *do* have to implement `clone()` in each monster class. That's just about as much code as the spawners.

Even if we do have different classes for each monster, there are other ways to decorticate this *Felis catus*. Instead of making separate spawner *classes* for each monster, we could make spawn *functions*, like so:

```cpp
Monster* spawnGhost() {
  return new Ghost();
}
```

This is less boilerplate than rolling a whole class for constructing a monster of some type. Then the one spawner class can simply store a function pointer:

```cpp
typedef Monster* (*SpawnCallback)();

class Spawner {
public:
  Spawner(SpawnCallback spawn)
  : spawn_(spawn)
  {}

Monster* spawnMonster()
  {
    return spawn_();
  }

private:
  SpawnCallback spawn_;
};
```

To create a spawner for ghosts, you do:

```cpp
Spawner* ghostSpawner = new Spawner(spawnGhost);
```

By now, most C++ developers are familiar with templates. Our spawner class needs to construct instances of some type, but we don't want to hard code some specific monster class. The natural solution then is to make it a *type parameter*, which templates let us do:

```cpp
class Spawner {
public:
  virtual ~Spawner() {}
  virtual Monster* spawnMonster() = 0;
};

template <class T>
class SpawnerFor : public Spawner {
public:
  virtual Monster* spawnMonster() { return new T(); }
};
```

Using it looks like:

```cpp
Spawner* ghostSpawner = new SpawnerFor<Ghost>();
```

The previous two solutions address the need to have a class, `Spawner`, which is parameterized by a type. In C++, types aren't generally first-class, so that requires some gymnastics. If you're using a dynamically-typed language like JavaScript, Python, or Ruby where classes *are* regular objects you can pass around, you can solve this much more directly.

With all of these options, I honestly can't say I've found a case where I felt the Prototype *design pattern* was the best answer. Maybe your experience will be different.

**CN:** 好吧，我们确实不需要为每种怪物创建独立的生成器类了，这很好。但我们*确实*需要在每个怪物类中实现 `clone()`。这跟写生成器的代码量差不多。

即使我们确实为每种怪物维护了不同的类，还有其他方法可以解决这个问题。我们不需要为每种怪物创建独立的生成器*类*，而是可以创建生成*函数*：

这比为一个类型构建一个完整的类要少很多模板代码。然后，单一的生成器类可以简单地存储一个函数指针：

要为幽灵创建生成器，你只需这样做：

如今，大多数 C++ 开发者都熟悉模板。我们的生成器类需要构建某种类型的实例，但我们不想硬编码某个特定的怪物类。那么自然的解决方案就是把它变成一个*类型参数*，模板让我们可以做到这一点：

使用起来是这样的：

前两种解决方案解决了需要一个以类型为参数的 `Spawner` 类的问题。在 C++ 中，类型通常不是一等公民，所以需要一些技巧。如果你使用的是动态类型语言，比如 JavaScript、Python 或 Ruby，其中类*就是*可以传递的普通对象，那么你可以更直接地解决这个问题。

有了所有这些选项，老实说，我还没发现原型*设计模式*是最佳答案的情况。也许你的体验会不同。

## Keep in Mind / 注意事项

* There are also some nasty semantic ratholes when you sit down to try to write a correct `clone()`. Does it do a deep clone or shallow one? In other words, if a demon is holding a pitchfork, does cloning the demon clone the pitchfork too?

Also, not only does this not look like it's saving us much code in this contrived problem, there's the fact that it's a *contrived problem*. We had to take as a given that we have separate classes for each monster. These days, that's definitely *not* the way most game engines roll.

Most of us learned the hard way that big class hierarchies like this are a pain to manage, which is why we instead use patterns like Component and Type Object to model different kinds of entities without enshrining each in its own class.

**CN:** 当你坐下来尝试编写一个正确的 `clone()` 时，还会遇到一些令人头疼的语义问题。它应该执行深拷贝还是浅拷贝？换句话说，如果一个恶魔拿着一把叉子，克隆恶魔时也会克隆那把叉子吗？

而且，这个看起来并没有在这个精心设计的问题中节省多少代码，还有一个事实是——这本身就是一个*精心设计的问题*。我们不得不假设每种怪物都有独立的类。如今，这绝对*不是*大多数游戏引擎的运作方式。

我们大多数人都通过惨痛的经历学到了：像这样的大型类层次结构难以管理，这就是为什么我们转而使用 Component 和 Type Object 等模式来建模不同类型的实体，而不是让每种实体都自成一类。

## C++ Code (原书代码)

All original C++ code from the book:

```cpp
// Base monster class hierarchy
class Monster {
  // Stuff...
};

class Ghost : public Monster {};
class Demon : public Monster {};
class Sorcerer : public Monster {};

// Parallel spawner hierarchy (the problem)
class Spawner {
public:
  virtual ~Spawner() {}
  virtual Monster* spawnMonster() = 0;
};

class GhostSpawner : public Spawner {
public:
  virtual Monster* spawnMonster()
  {
    return new Ghost();
  }
};

class DemonSpawner : public Spawner {
public:
  virtual Monster* spawnMonster()
  {
    return new Demon();
  }
};

// The Prototype solution — clone() method
class Monster {
public:
  virtual ~Monster() {}
  virtual Monster* clone() = 0;
  // Other stuff...
};

class Ghost : public Monster {
public:
  Ghost(int health, int speed)
  : health_(health),
    speed_(speed)
  {}

virtual Monster* clone()
  {
    return new Ghost(health_, speed_);
  }

private:
  int health_;
  int speed_;
};

// Single generic spawner — no parallel class hierarchy
class Spawner {
public:
  Spawner(Monster* prototype)
  : prototype_(prototype)
  {}

Monster* spawnMonster()
  {
    return prototype_->clone();
  }

private:
  Monster* prototype_;
};

// Usage
Monster* ghostPrototype = new Ghost(15, 3);
Spawner* ghostSpawner = new Spawner(ghostPrototype);

// Alternative: Spawn functions
Monster* spawnGhost() {
  return new Ghost();
}

typedef Monster* (*SpawnCallback)();

class Spawner {
public:
  Spawner(SpawnCallback spawn)
  : spawn_(spawn)
  {}

Monster* spawnMonster()
  {
    return spawn_();
  }

private:
  SpawnCallback spawn_;
};

Spawner* ghostSpawner = new Spawner(spawnGhost);

// Alternative: Templates
class Spawner {
public:
  virtual ~Spawner() {}
  virtual Monster* spawnMonster() = 0;
};

template <class T>
class SpawnerFor : public Spawner {
public:
  virtual Monster* spawnMonster() { return new T(); }
};

Spawner* ghostSpawner = new SpawnerFor<Ghost>();
```

## C# Equivalent (C# 对照实现)

```csharp
using System;

public interface IPrototype<T>
{
    T Clone();
}

public class Monster : ICloneable
{
    public string Name { get; set; }
    public int Health { get; set; }
    public int Speed { get; set; }

public object Clone()
    {
        return this.MemberwiseClone();
    }

public Monster CloneMonster()
    {
        return (Monster)this.MemberwiseClone();
    }
}

public class Ghost : Monster
{
    public Inventory Inventory { get; set; }

public Ghost DeepClone()
    {
        Ghost cloned = (Ghost)this.MemberwiseClone();
        cloned.Inventory = new Inventory(this.Inventory);
        return cloned;
    }
}

public class Inventory
{
    public string[] Items { get; set; }

public Inventory() { }

public Inventory(Inventory other)
    {
        this.Items = other.Items != null
            ? (string[])other.Items.Clone()
            : null;
    }
}

public class Spawner<T> where T : Monster
{
    private T _prototype;

public Spawner(T prototype)
    {
        _prototype = prototype;
    }

public T Spawn()
    {
        return (T)_prototype.Clone();
    }
}

// Usage:
// var ghostPrototype = new Ghost { Name = "Ghost", Health = 15, Speed = 3 };
// var ghostSpawner = new Spawner<Ghost>(ghostPrototype);
// var ghost1 = ghostSpawner.Spawn();
// var ghost2 = ghostSpawner.Spawn();
```

## Unity Application / Unity 应用场景

* Unity heavily uses prototyping via `Instantiate()`, `ScriptableObject`, and prefab systems. The entire prefab system is a real-world application of the Prototype pattern.

* Unity 通过 `Instantiate()`、`ScriptableObject` 和预制体系统大量使用原型模式。整个预制体系统就是原型模式的实际应用。

```csharp
using UnityEngine;

// Unity's Instantiate is a direct implementation of the Prototype pattern
// GameObject original = Resources.Load<GameObject>("Enemy/Goblin");
// GameObject clone = Instantiate(original);

public class PrototypeSpawner : MonoBehaviour
{
    public EnemyConfig prototypeConfig;

public void SpawnEnemy()
    {
        if (prototypeConfig == null) return;

GameObject newEnemy = Instantiate(
            prototypeConfig.prefab,
            transform.position,
            Quaternion.identity
        );

EnemyComponent enemyComp = newEnemy.GetComponent<EnemyComponent>();
        if (enemyComp != null)
        {
            enemyComp.Initialize(prototypeConfig);
        }
    }
}

// Prefab Variant — Unity 2018.3+ nested prototype system
// 1. Create base prefab "Goblin" (base prototype)
// 2. Create variant "Goblin_Wizard" inheriting from "Goblin" (prototype chain)
// 3. Modify variant-specific properties
// This is essentially delegation in the Prototype pattern:
//   properties not overridden in the variant inherit from the base prototype
```

## Key Differences / 关键差异

| Aspect | C++ (GoF) | C# / Unity |
|---|---|---|
| **Cloning mechanism** | Manual `clone()` virtual method | `ICloneable`, `MemberwiseClone()`, `Instantiate()` |
| **Deep vs shallow** | Manual deep copy in each `clone()` | `MemberwiseClone()` does shallow copy; manual deep copy needed for ref types |
| **Factory elimination** | Eliminates parallel spawner hierarchy | Unity prefabs + `Instantiate()` already solve this |
| **State cloning** | Clones both class and state | `ScriptableObject` as prototype stores shared data; `Instantiate` clones scene objects |
| **Unity-specific** | N/A | `Instantiate()` (runtime), `Prefab Variant` (editor), `ScriptableObject` (data) |
| **Prototype chain** | Not built-in | Unity Prefab Variant supports hierarchical inheritance (prototype delegation) |

## See Also / 扩展阅读

* Most of us learned the hard way that big class hierarchies like this are a pain to manage, which is why we instead use patterns like Component and Type Object to model different kinds of entities without enshrining each in its own class.

With all of these options, I honestly can't say I've found a case where I felt the Prototype *design pattern* was the best answer. Maybe your experience will be different, but for now let's put that away and talk about something else: prototypes as a *language paradigm*.

Related patterns from the book:
- **Component pattern** — alternative to large class hierarchies for entity modeling
- **Type Object pattern** — another approach to defining entity types without per-class hierarchies
- **Observer pattern** — previous chapter
- **Singleton pattern** — next chapter

For further reading on prototype-based languages, see Self (the original prototype-based language) and JavaScript's prototypal inheritance model, both discussed extensively in this chapter.

**CN:** 我们大多数人都通过惨痛的经历学到了：像这样的大型类层次结构难以管理，这就是为什么我们转而使用 Component 和 Type Object 等模式来建模不同类型的实体，而不是让每种实体都自成一类。

有了所有这些选项，老实说，我还没发现原型*设计模式*是最佳答案的情况。也许你的体验会不同，但让我们先放一放，谈谈别的：原型作为一种*语言范式*。

相关模式：
- **Component 模式** — 大型类层次结构的替代方案，用于实体建模
- **Type Object 模式** — 定义实体类型的另一种方法，无需逐类层次结构
- **Observer 模式** — 前一章
- **Singleton 模式** — 下一章

关于基于原型的语言的进一步阅读，参见 Self（最初的基于原型的语言）和 JavaScript 的原型继承模型，本章都进行了详细讨论。

---

# Singleton Pattern — 单例模式

> **EN:** Ensure a class has one instance, and provide a global point of access to it.
> **CN:** 确保一个类只有一个实例，并提供一个全局访问点。

## Intent / 意图

* *Design Patterns* summarizes Singleton like this:

> Ensure a class has one instance, and provide a global point of access to it.

**CN:** 《设计模式》对单例模式的总结如下：

> 确保一个类只有一个实例，并提供一个全局访问点。

### Restricting a class to one instance / 限制类为单一实例

* There are times when a class cannot perform correctly if there is more than one instance of it. The common case is when the class interacts with an external system that maintains its own global state.

Consider a class that wraps an underlying file system API. Because file operations can take a while to complete, our class performs operations asynchronously. This means multiple operations can be running concurrently, so they must be coordinated with each other. If we start one call to create a file and another one to delete that same file, our wrapper needs to be aware of both to make sure they don't interfere with each other.

To do this, a call into our wrapper needs to have access to every previous operation. If users could freely create instances of our class, one instance would have no way of knowing about operations that other instances started. Enter the singleton. It provides a way for a class to ensure at compile time that there is only a single instance of the class.

**CN:** 有时候，如果一个类存在多个实例，它将无法正确工作。常见的情况是当该类与一个维护自身全局状态的外部系统交互时。

考虑一个封装底层文件系统 API 的类。由于文件操作可能需要一段时间才能完成，我们的类以异步方式执行操作。这意味着多个操作可以同时运行，因此它们必须相互协调。如果我们启动一个创建文件的调用，同时又启动另一个删除同一文件的调用，我们的包装器需要感知到这两个操作，确保它们不会相互干扰。

为此，对包装器的调用需要访问所有先前的操作。如果用户可以自由地创建我们类的实例，一个实例将无法知道其他实例启动的操作。单例模式应运而生。它提供了一种方式，让类可以在编译时确保只有该类的单个实例。

### Providing a global point of access / 提供全局访问点

* Several different systems in the game will use our file system wrapper: logging, content loading, game state saving, etc. If those systems can't create their own instances of our file system wrapper, how can they get ahold of one?

Singleton provides a solution to this too. In addition to creating the single instance, it also provides a globally available method to get it. This way, anyone anywhere can get their paws on our blessed instance.

**CN:** 游戏中的多个不同系统都会使用我们的文件系统包装器：日志系统、内容加载、游戏状态保存等。如果这些系统不能创建自己的文件系统包装器实例，它们如何才能获得一个实例？

单例模式对此也提供了解决方案。除了创建单一实例外，它还提供了一个全局可用的方法来获取它。这样，任何地方的任何人都可以拿到我们那个被赐福的实例。

## Motivation / 动机

* This chapter is an anomaly. Every other chapter in this book shows you how to use a design pattern. This chapter shows you how *not* to use one.

Despite noble intentions, the [Singleton](http://c2.com/cgi/wiki?SingletonPattern) pattern described by the Gang of Four usually does more harm than good. They stress that the pattern should be used sparingly, but that message was often lost in translation to the game industry.

Like any pattern, using Singleton where it doesn't belong is about as helpful as treating a bullet wound with a splint. Since it's so overused, most of this chapter will be about *avoiding* singletons, but first, let's go over the pattern itself.

When much of the industry moved to object-oriented programming from C, one problem they ran into was "how do I get an instance?" They had some method they wanted to call but didn't have an instance of the object that provides that method in hand. Singletons (in other words, making it global) were an easy way out.

**CN:** 本章是一个异类。本书中的其他每一章都在教你如何使用一个设计模式。而这一章教你如何*不*使用一个设计模式。

尽管意图高尚，但 GoF 描述的[单例](http://c2.com/cgi/wiki?SingletonPattern)模式通常弊大于利。他们强调该模式应谨慎使用，但这一信息在传入游戏行业时常常被曲解。

就像任何模式一样，在不该使用的地方使用单例，就像用夹板治疗枪伤一样"有用"。由于它被过度使用，本章的大部分内容将讨论*如何避免*单例，但首先，让我们看看模式本身。

当行业中的大部分人从 C 语言转向面向对象编程时，他们遇到的一个问题是"我如何获得一个实例？"他们有想要调用的方法，但手头却没有提供该方法的对象实例。单例（换句话说，使其成为全局的）成了一种简单的出路。

### Why We Use It / 我们为什么使用它

* It seems we have a winner. Our file system wrapper is available wherever we need it without the tedium of passing it around everywhere. The class itself cleverly ensures we won't make a mess of things by instantiating a couple of instances. It's got some other nice features too:

- **It doesn't create the instance if no one uses it.** Saving memory and CPU cycles is always good. Since the singleton is initialized only when it's first accessed, it won't be instantiated at all if the game never asks for it.

- **It's initialized at runtime.** A common alternative to Singleton is a class with static member variables. I like simple solutions, so I use static classes instead of singletons when possible, but there's one limitation static members have: automatic initialization. The compiler initializes statics before `main()` is called. This means they can't use information known only once the program is up and running (for example, configuration loaded from a file). It also means they can't reliably depend on each other — the compiler does not guarantee the order in which statics are initialized relative to each other.

Lazy initialization solves both of those problems. The singleton will be initialized as late as possible, so by that time any information it needs should be available. As long as they don't have circular dependencies, one singleton can even refer to another when initializing itself.

- **You can subclass the singleton.** This is a powerful but often overlooked capability. Let's say we need our file system wrapper to be cross-platform. To make this work, we want it to be an abstract interface for a file system with subclasses that implement the interface for each platform.

With a simple compiler switch, we bind our file system wrapper to the appropriate concrete type. Our entire codebase can access the file system using `FileSystem::instance()` without being coupled to any platform-specific code. That coupling is instead encapsulated within the implementation file for the `FileSystem` class itself.

This takes us about as far as most of us go when it comes to solving a problem like this. We've got a file system wrapper. It works reliably. It's available globally so every place that needs it can get to it. It's time to check in the code and celebrate with a tasty beverage.

**CN:** 看起来我们找到了一个赢家。我们的文件系统包装器在我们需要的任何地方都可用，无需繁琐地到处传递它。类本身巧妙地确保了我们不会因实例化多个实例而把事情搞砸。它还有一些其他不错的特性：

- **如果没人使用，它就不会创建实例。** 节省内存和 CPU 周期总是好的。由于单例仅在首次访问时初始化，如果游戏从未请求它，它就根本不会被实例化。

- **它在运行时初始化。** 单例的一个常见替代方案是使用静态成员变量的类。我喜欢简单的解决方案，所以尽可能使用静态类而不是单例，但静态成员有一个限制：自动初始化。编译器在 `main()` 被调用之前初始化静态变量。这意味着它们不能使用只有在程序启动运行后才能知道的信息（例如，从文件加载的配置）。这也意味着它们不能可靠地相互依赖——编译器不保证静态变量之间的初始化顺序。

懒初始化解决了这两个问题。单例会尽可能晚地初始化，因此到那时它需要的任何信息都应该可用了。只要它们没有循环依赖，一个单例在初始化时甚至可以引用另一个单例。

- **你可以子类化单例。** 这是一个强大但经常被忽视的能力。假设我们需要我们的文件系统包装器跨平台。为此，我们希望它成为一个文件系统的抽象接口，并有子类为每个平台实现该接口。

通过一个简单的编译器开关，我们将文件系统包装器绑定到适当的具象类型。整个代码库可以使用 `FileSystem::instance()` 访问文件系统，而无需与任何平台特定代码耦合。这种耦合被封装在 `FileSystem` 类本身的实现文件中。

这差不多就是我们大多数人在解决这类问题时所能做到的。我们有了一个文件系统包装器。它工作可靠。它是全局可用的，所以每个需要它的地方都能访问到。是时候提交代码并用美味饮料庆祝了。

## The Pattern / 模式描述

* All together, the classic implementation looks like this:

The static `instance_` member holds an instance of the class, and the private constructor ensures that it is the *only* one. The public static `instance()` method grants access to the instance from anywhere in the codebase. It is also responsible for instantiating the singleton instance lazily the first time someone asks for it.

A modern take looks like this:

C++11 mandates that the initializer for a local static variable is only run once, even in the presence of concurrency. So, assuming you've got a modern C++ compiler, this code is thread-safe where the first example is not.

Of course, the thread-safety of your singleton class itself is an entirely different question! This just ensures that its *initialization* is.

**CN:** 综上所述，经典的实现如下所示：

静态成员 `instance_` 持有该类的一个实例，私有构造函数确保它是*唯一*的一个。公有静态方法 `instance()` 授予从代码库任何位置访问该实例的权限。它还负责在首次有人请求时懒实例化单例实例。

现代的实现方式如下：

C++11 规定，即使存在并发，局部静态变量的初始化器也只运行一次。因此，假设你有一个现代的 C++ 编译器，这段代码是线程安全的，而第一个例子则不是。

当然，单例类本身的线程安全完全是另一个问题！这仅仅确保它的*初始化*是线程安全的。

## C++ Code (原书代码)

```cpp
// Classic Singleton implementation from Game Programming Patterns
class FileSystem {
public:
  static FileSystem& instance()
  {
    // Lazy initialize.
    if (instance_ == NULL) instance_ = new FileSystem();
    return *instance_;
  }

private:
  FileSystem() {}
  static FileSystem* instance_;
};

// Modern C++11 thread-safe version:
class FileSystem {
public:
  static FileSystem& instance()
  {
    static FileSystem *instance = new FileSystem();
    return *instance;
  }

private:
  FileSystem() {}
};

// Polymorphic Singleton — base class with abstract interface
class FileSystem {
public:
  static FileSystem& instance();

virtual ~FileSystem() {}
  virtual char* readFile(char* path) = 0;
  virtual void  writeFile(char* path, char* contents) = 0;

protected:
  FileSystem() {}
};

// Platform-specific subclasses
class PS3FileSystem : public FileSystem {
public:
  virtual char* readFile(char* path)
  {
     // Use Sony file IO API...
  }

virtual void writeFile(char* path, char* contents)
  {
    // Use sony file IO API...
  }
};

class WiiFileSystem : public FileSystem {
public:
  virtual char* readFile(char* path)
  {
     // Use Nintendo file IO API...
  }

virtual void writeFile(char* path, char* contents)
  {
    // Use Nintendo file IO API...
  }
};

// Platform-selecting instance() implementation
FileSystem& FileSystem::instance() {
  #if PLATFORM == PLAYSTATION3
    static FileSystem *instance = new PS3FileSystem();
  #elif PLATFORM == WII
    static FileSystem *instance = new WiiFileSystem();
  #endif

return *instance;
}
```

## C# Equivalent (C# 对照实现)

```csharp
// ============================================================
// C# 中有多种实现单例的方式，每种有不同的线程安全和性能特性
// ============================================================

// ---------- 方式 1: 静态初始化（最常用，线程安全） ----------
// CLR 在类型初始化时自动创建实例，保证线程安全。
// 缺点：无法懒加载，类被引用时即创建；不支持参数化构造。
public sealed class GameManager
{
    private static readonly GameManager instance = new GameManager();

static GameManager() { }

private GameManager() { }

public static GameManager Instance => instance;

public void GameStart() { }
}

// ---------- 方式 2: 懒加载 + Lazy<T>（推荐用于耗时初始化） ----------
// 使用 Lazy<T> 实现线程安全的延迟创建，适合初始化开销大的系统。
public sealed class AudioManager
{
    private static readonly Lazy<AudioManager> lazyInstance =
        new Lazy<AudioManager>(() => new AudioManager());

private AudioManager() { }

public static AudioManager Instance => lazyInstance.Value;

public void PlaySound(string soundName) { }
}

// ---------- 方式 3: Unity MonoBehaviour 单例（泛型基类） ----------
public class UnitySingleton<T> : MonoBehaviour where T : Component
{
    private static T instance;

public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name);
                    instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
}

// 使用示例
public class GameManager : UnitySingleton<GameManager>
{
    public void StartGame() { }
}

// ---------- 方式 4: 双重检查锁（不推荐，仅作了解） ----------
public sealed class ResourceManager
{
    private static ResourceManager instance;
    private static readonly object padlock = new object();

private ResourceManager() { }

public static ResourceManager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ResourceManager();
                    }
                }
            }
            return instance;
        }
    }
}
```

### C# 实现方式对比 / Comparison

| 方式 | 线程安全 | 懒加载 | 性能 | 适用场景 |
|------|---------|--------|------|---------|
| 静态初始化 | ✅ CLR保证 | ❌ 类型加载时创建 | ★★★ 最高 | 大多数情况 |
| Lazy\<T\> | ✅ Lazy保证 | ✅ 首次访问 | ★★☆ 中 | 初始化耗时长的对象 |
| UnitySingleton | ✅ Awake检查 | ✅ 首次访问 | ★★☆ 中 | Unity MonoBehaviour |
| 双重检查锁 | ✅ 手动实现 | ✅ 首次访问 | ★☆☆ 低 | 兼容旧框架(不推荐) |

## Unity Application / Unity 应用场景

* Singleton is extremely common in Unity projects, often used for:

- **GameManager** — Game state, score, level flow
- **AudioManager** — Centralized sound playback
- **UIManager** — Screen management, HUD control
- **ObjectPool** — Object reuse pool
- **EventBus** — Cross-component event communication

**However, Singleton is problematic in Unity for several reasons:**

1. **Lifecycle conflicts**: Unity's scene-loading destroys GameObjects, requiring careful `DontDestroyOnLoad` handling. Singletons can be destroyed mid-game, leaving stale references.
2. **Testing difficulty**: Unity tests (EditMode/PlayMode) run in isolation. Global singleton state persists between tests, causing test pollution.
3. **Tight coupling**: Every script that calls `GameManager.Instance` is coupled to GameManager. Refactoring becomes painful.
4. **Memory management**: Singletons persist forever, preventing scene-specific memory cleanup.
5. **Order-of-initialization issues**: One singleton's Awake might depend on another singleton that hasn't started yet.

**Alternatives in Unity:** Dependency Injection containers (Zenject/VContainer), ScriptableObject-based event systems, Service Locator pattern.

**CN:** 单例模式在 Unity 项目中极为常见，常用于以下系统：

- **GameManager** — 游戏状态、分数、关卡流程
- **AudioManager** — 统一的音效播放
- **UIManager** — 屏幕管理、HUD 控制
- **ObjectPool** — 对象复用池
- **EventBus** — 跨组件的消息通信

**然而，单例模式在 Unity 中有几个严重问题：**

1. **生命周期冲突**：Unity 的场景加载会销毁 GameObject，需要使用 `DontDestroyOnLoad` 谨慎处理。单例可能在游戏中途被销毁，留下悬空引用。
2. **测试困难**：Unity 测试（EditMode/PlayMode）在隔离环境中运行。全局单例状态会在测试间持续存在，造成测试污染。
3. **紧耦合**：每个调用 `GameManager.Instance` 的脚本都与 GameManager 耦合，重构极为痛苦。
4. **内存管理**：单例永久存在，阻碍场景特定的内存回收。
5. **初始化顺序问题**：一个单例的 Awake 可能依赖另一个尚未初始化的单例。

**Unity 中的替代方案：** 依赖注入容器（Zenject/VContainer）、ScriptableObject 事件系统、服务定位器模式。

## When to Use / 使用场景

* The question remains, where *should* we use the real Singleton pattern? Honestly, I've never used the full Gang of Four implementation in a game. To ensure single instantiation, I usually simply use a static class. If that doesn't work, I'll use a static flag to check at runtime that only one instance of the class is constructed.

* 问题依然存在——我们*应该*在何处使用真正的单例模式？说实话，我从未在游戏中使用过完整的 GoF 实现。为确保单一实例化，我通常简单地使用静态类。如果那行不通，我会使用一个静态标志在运行时检查是否只构造了该类的一个实例。

### What We Can Do Instead / 我们可以用什么替代

* If I've accomplished my goal so far, you'll think twice before you pull Singleton out of your toolbox the next time you have a problem. But you still have a problem that needs solving. What tool *should* you pull out? Depending on what you're trying to do, I have a few options for you to consider.

#### See if you need the class at all / 先看看你是否真的需要这个类

* Many of the singleton classes I see in games are "managers" — those nebulous classes that exist just to babysit other objects. I've seen codebases where it seems like *every* class has a manager: Monster, MonsterManager, Particle, ParticleManager, Sound, SoundManager, ManagerManager. Sometimes, for variety, they'll throw a "System" or "Engine" in there, but it's still the same idea.

While caretaker classes are sometimes useful, often they just reflect unfamiliarity with OOP. Consider these two contrived classes:

```cpp
class Bullet {
public:
  int getX() const { return x_; }
  int getY() const { return y_; }

void setX(int x) { x_ = x; }
  void setY(int y) { y_ = y; }

private:
  int x_, y_;
};

class BulletManager {
public:
  Bullet* create(int x, int y)
  {
    Bullet* bullet = new Bullet();
    bullet->setX(x);
    bullet->setY(y);

return bullet;
  }

bool isOnScreen(Bullet& bullet)
  {
    return bullet.getX() >= 0 &&
           bullet.getX() < SCREEN_WIDTH &&
           bullet.getY() >= 0 &&
           bullet.getY() < SCREEN_HEIGHT;
  }

void move(Bullet& bullet)
  {
    bullet.setX(bullet.getX() + 5);
  }
};
```

The answer here is *zero*, actually. Here's how we solve the "singleton" problem for our manager class:

```cpp
class Bullet {
public:
  Bullet(int x, int y) : x_(x), y_(y) {}

bool isOnScreen()
  {
    return x_ >= 0 && x_ < SCREEN_WIDTH &&
           y_ >= 0 && y_ < SCREEN_HEIGHT;
  }

void move() { x_ += 5; }

private:
  int x_, y_;
};
```

There we go. No manager, no problem. Poorly designed singletons are often "helpers" that add functionality to another class. If you can, just move all of that behavior into the class it helps. After all, OOP is about letting objects take care of themselves.

**CN:** 我看到游戏中的许多单例类都是"管理器"——那些模糊的、仅仅为了照看其他对象而存在的类。我见过一些代码库，似乎*每个*类都有一个管理器：Monster、MonsterManager、Particle、ParticleManager、Sound、SoundManager、ManagerManager。有时为了多样性，他们会加入"System"或"Engine"之类的词，但本质是一样的。

虽然管理类有时很有用，但它们通常只是反映了对 OOP 的不熟悉。考虑这两个人为构造的类：

```cpp
class Bullet {
public:
  int getX() const { return x_; }
  int getY() const { return y_; }

void setX(int x) { x_ = x; }
  void setY(int y) { y_ = y; }

private:
  int x_, y_;
};

class BulletManager {
public:
  Bullet* create(int x, int y)
  {
    Bullet* bullet = new Bullet();
    bullet->setX(x);
    bullet->setY(y);

return bullet;
  }

bool isOnScreen(Bullet& bullet)
  {
    return bullet.getX() >= 0 &&
           bullet.getX() < SCREEN_WIDTH &&
           bullet.getY() >= 0 &&
           bullet.getY() < SCREEN_HEIGHT;
  }

void move(Bullet& bullet)
  {
    bullet.setX(bullet.getX() + 5);
  }
};
```

实际上答案是*零*个。下面是我们如何为管理类解决"单例"问题：

```cpp
class Bullet {
public:
  Bullet(int x, int y) : x_(x), y_(y) {}

bool isOnScreen()
  {
    return x_ >= 0 && x_ < SCREEN_WIDTH &&
           y_ >= 0 && y_ < SCREEN_HEIGHT;
  }

void move() { x_ += 5; }

private:
  int x_, y_;
};
```

瞧。没有管理器，没有问题。设计不佳的单例通常是向另一个类添加功能的"助手"。如果可以，只需将这些行为全部移到它帮助的类中。毕竟，OOP 就是关于让对象自己照顾自己。

#### To limit a class to a single instance / 限制类为单一实例

* This is one half of what the Singleton pattern gives you. As in our file system example, it can be critical to ensure there's only a single instance of a class. However, that doesn't necessarily mean we also want to provide *public*, *global* access to that instance. We may want to restrict access to certain areas of the code or even make it private to a single class. In those cases, providing a public global point of access weakens the architecture.

```cpp
class FileSystem {
public:
  FileSystem()
  {
    assert(!instantiated_);
    instantiated_ = true;
  }

~FileSystem() { instantiated_ = false; }

private:
  static bool instantiated_;
};

bool FileSystem::instantiated_ = false;
```

**CN:** 这是单例模式提供的一半功能。正如我们的文件系统示例所示，确保只有一个类实例可能是至关重要的。然而，这并不一定意味着我们也要提供对该实例的*公有*、*全局*访问。我们可能希望限制对代码特定区域的访问，甚至将其对单个类私有。在这些情况下，提供公有全局访问点会削弱架构。

```cpp
class FileSystem {
public:
  FileSystem()
  {
    assert(!instantiated_);
    instantiated_ = true;
  }

~FileSystem() { instantiated_ = false; }

private:
  static bool instantiated_;
};

bool FileSystem::instantiated_ = false;
```

#### To provide convenient access to an instance / 提供便捷的实例访问

* Convenient access is the main reason we reach for singletons. They make it easy to get our hands on an object we need to use in a lot of different places. That ease comes at a cost, though — it becomes equally easy to get our hands on the object in places where we *don't* want it being used.

The general rule is that we want variables to be as narrowly scoped as possible while still getting the job done. The smaller the scope an object has, the fewer places we need to keep in our head while we're working with it. Before we take the shotgun approach of a singleton object with *global* scope, let's consider other ways our codebase can get access to an object:

- **Pass it in.** The simplest solution, and often the best, is to simply pass the object you need as an argument to the functions that need it. It's worth considering before we discard it as too cumbersome.

- **Get it from the base class.** Many game architectures have shallow but wide inheritance hierarchies, often only one level deep. For example, you may have a base `GameObject` class with derived classes for each enemy or object in the game. With architectures like this, a large portion of the game code will live in these "leaf" derived classes. This means that all these classes already have access to the same thing: their `GameObject` base class.

```cpp
class GameObject {
protected:
  Log& getLog() { return log_; }

private:
  static Log& log_;
};

class Enemy : public GameObject {
  void doSomething()
  {
    getLog().write("I can log!");
  }
};
```

- **Get it from something already global.** The goal of removing *all* global state is admirable, but rarely practical. Most codebases will still have a couple of globally available objects, such as a single `Game` or `World` object representing the entire game state.

```cpp
class Game {
public:
  static Game& instance() { return instance_; }

// Functions to set log_, et. al. ...

Log&         getLog()         { return *log_; }
  FileSystem&  getFileSystem()  { return *fileSystem_; }
  AudioPlayer& getAudioPlayer() { return *audioPlayer_; }

private:
  static Game instance_;

Log         *log_;
  FileSystem  *fileSystem_;
  AudioPlayer *audioPlayer_;
};
```

- **Get it from a Service Locator.** So far, we're assuming the global class is some regular concrete class like `Game`. Another option is to define a class whose sole reason for being is to give global access to objects. This common pattern is called a [Service Locator](service-locator.html) and gets its own chapter.

**CN:** 便捷访问是我们使用单例的主要原因。它们让我们轻松获取需要在许多不同地方使用的对象。然而，这种便利是有代价的——它同样使得在*不*希望使用该对象的地方也能轻松获取它。

一般规则是，我们希望变量的作用域尽可能小，同时还能完成任务。对象的作用域越小，我们在处理它时需要记住的地方就越少。在我们采用具有*全局*作用域的单例对象这种散弹枪式方法之前，让我们考虑一下代码库获取对象的其他方式：

- **传递进来。** 最简单的解决方案，通常也是最好的解决方案，就是将你需要的对象作为参数传递给需要它的函数。在我们认为它太繁琐而放弃之前，值得考虑一下。

- **从基类获取。** 许多游戏架构具有浅而宽的继承层次，通常只有一层深。例如，你可能有一个基类 `GameObject`，以及为游戏中每个敌人或对象派生的子类。在这样的架构中，大部分游戏代码将存在于这些"叶子"派生类中。这意味着所有这些类已经可以访问同一个东西：它们的 `GameObject` 基类。

```cpp
class GameObject {
protected:
  Log& getLog() { return log_; }

private:
  static Log& log_;
};

class Enemy : public GameObject {
  void doSomething()
  {
    getLog().write("I can log!");
  }
};
```

- **从已经全局的东西获取。** 消除*所有*全局状态的目标是令人钦佩的，但很少实用。大多数代码库仍然会有一些全局可用的对象，例如代表整个游戏状态的单个 `Game` 或 `World` 对象。

```cpp
class Game {
public:
  static Game& instance() { return instance_; }

// Functions to set log_, et. al. ...

Log&         getLog()         { return *log_; }
  FileSystem&  getFileSystem()  { return *fileSystem_; }
  AudioPlayer& getAudioPlayer() { return *audioPlayer_; }

private:
  static Game instance_;

Log         *log_;
  FileSystem  *fileSystem_;
  AudioPlayer *audioPlayer_;
};
```

- **从服务定位器获取。** 到目前为止，我们假设全局类是一些常规的具象类，比如 `Game`。另一种选择是定义一个类，其存在的唯一理由就是提供对对象的全局访问。这种常见模式称为[服务定位器](service-locator.html)，并有专门的一章。

## Keep in Mind / 注意事项

* In the short term, the Singleton pattern is relatively benign. Like many design choices, we pay the cost in the long term. Once we've cast a few unnecessary singletons into cold hard code, here's the trouble we've bought ourselves:

* 短期内，单例模式相对无害。像许多设计选择一样，我们在长期付出代价。一旦我们将一些不必要的单例铸入冷冰冰的代码中，我们就给自己带来了以下麻烦：

### It's a global variable / 它就是全局变量

* When games were still written by a couple of guys in a garage, pushing the hardware was more important than ivory-tower software engineering principles. Old-school C and assembly coders used globals and statics without any trouble and shipped good games. As games got bigger and more complex, architecture and maintainability started to become the bottleneck. We struggled to ship games not because of hardware limitations, but because of *productivity* limitations.

So we moved to languages like C++ and started applying some of the hard-earned wisdom of our software engineer forebears. One lesson we learned is that global variables are bad for a variety of reasons:

- **They make it harder to reason about code.** Say we're tracking down a bug in a function someone else wrote. If that function doesn't touch any global state, we can wrap our heads around it just by understanding the body of the function and the arguments being passed to it.

Computer scientists call functions that don't access or modify global state "pure" functions. Pure functions are easier to reason about, easier for the compiler to optimize, and let you do neat things like memoization where you cache and reuse the results from previous calls to the function.

While there are challenges to using purity exclusively, the benefits are enticing enough that computer scientists have created languages like Haskell that *only* allow pure functions.

Now, imagine right in the middle of that function is a call to `SomeClass::getSomeGlobalData()`. To figure out what's going on, we have to hunt through the entire codebase to see what touches that global data. You don't really hate global state until you've had to `grep` a million lines of code at three in the morning trying to find the one errant call that's setting a static variable to the wrong value.

- **They encourage coupling.** The new coder on your team isn't familiar with your game's beautifully maintainable loosely coupled architecture, but he's just been given his first task: make boulders play sounds when they crash onto the ground. You and I know we don't want the physics code to be coupled to *audio* of all things, but he's just trying to get his task done. Unfortunately for us, the instance of our `AudioPlayer` is globally visible. So, one little `#include` later, and our new guy has compromised a carefully constructed architecture.

Without a global instance of the audio player, even if he *did* `#include` the header, he still wouldn't be able to do anything with it. That difficulty sends a clear message to him that those two modules should not know about each other and that he needs to find another way to solve his problem. *By controlling access to instances, you control coupling.*

- **They aren't concurrency-friendly.** The days of games running on a simple single-core CPU are pretty much over. Code today must at the very least *work* in a multi-threaded way even if it doesn't take full advantage of concurrency. When we make something global, we've created a chunk of memory that every thread can see and poke at, whether or not they know what other threads are doing to it. That path leads to deadlocks, race conditions, and other hell-to-fix thread-synchronization bugs.

Issues like these are enough to scare us away from declaring a global variable, and thus the Singleton pattern too, but that still doesn't tell us how we *should* design the game. How do you architect a game without global state?

There are some extensive answers to that question (most of this book in many ways *is* an answer to just that), but they aren't apparent or easy to come by. In the meantime, we have to get games out the door. The Singleton pattern looks like a panacea. It's in a book on object-oriented design patterns, so it *must* be architecturally sound, right? And it lets us design software the way we have been doing for years.

Unfortunately, it's more placebo than cure. If you scan the list of problems that globals cause, you'll notice that the Singleton pattern doesn't solve any of them. That's because a singleton *is* global state — it's just encapsulated in a class.

**CN:** 当游戏还是由几个家伙在车库里编写时，压榨硬件比象牙塔般的软件工程原则更重要。老派的 C 和汇编程序员使用全局变量和静态变量毫无问题，并且发布了优秀的游戏。随着游戏变得越来越大、越来越复杂，架构和可维护性开始成为瓶颈。我们艰难地发布游戏，不是因为硬件限制，而是因为*生产力*限制。

于是我们转向了 C++ 等语言，并开始应用软件工程前辈们来之不易的智慧。我们学到的一课是，全局变量因多种原因而有害：

- **它们使代码推理更加困难。** 假设我们在追踪别人写的函数中的 bug。如果该函数不接触任何全局状态，我们只需理解函数体及其参数就能掌握它。

计算机科学家将不访问或修改全局状态的函数称为"纯"函数。纯函数更容易推理，更容易被编译器优化，并且让你可以实现诸如记忆化（缓存和重用之前函数调用的结果）这样的巧妙功能。

尽管完全使用纯函数存在挑战，但其好处足够诱人，以至于计算机科学家创造了像 Haskell 这样*只*允许纯函数的语言。

现在，想象一下在该函数中间有一个对 `SomeClass::getSomeGlobalData()` 的调用。要弄清楚发生了什么，我们必须搜索整个代码库，看看是什么在触碰那个全局数据。你真的不会讨厌全局状态，直到你不得不在凌晨三点 `grep` 一百万行代码，试图找到那个将静态变量设置为错误值的错误调用。

- **它们鼓励耦合。** 团队中的新程序员不熟悉你精心维护的松耦合架构，但他刚接到了第一个任务：让巨石撞到地面时播放声音。你我都知道我们不想让物理代码与*音频*耦合，但他只是想完成他的任务。不幸的是，我们的 `AudioPlayer` 实例是全局可见的。于是，一个小小的 `#include` 之后，我们的新员工就破坏了一个精心构建的架构。

如果没有音频播放器的全局实例，即使他*确实* `#include` 了头文件，他仍然无法用它做任何事情。这种困难向他传递了一个明确的信息：这两个模块不应该知道彼此，他需要找到另一种方法来解决他的问题。*通过控制对实例的访问，你控制了耦合。*

- **它们不友好于并发。** 游戏在简单的单核 CPU 上运行的时代基本上已经结束了。今天的代码至少必须以多线程方式*工作*，即使它没有充分利用并发。当我们把某些东西设为全局时，我们就创建了一块每个线程都能看到和操作的内存，无论它们是否知道其他线程在对其做什么。这条路会导致死锁、竞态条件和其他难以修复的线程同步 bug。

这样的问题足以让我们远离声明全局变量，也因此远离单例模式，但这仍然没有告诉我们*应该*如何设计游戏。如何在没有全局状态的情况下构建游戏架构？

这个问题有一些广泛的答案（本书的大部分内容在某种意义上就是对此的回答），但它们并不明显或容易获得。与此同时，我们必须把游戏做出来。单例模式看起来像是一剂万灵药。它出现在一本关于面向对象设计模式的书中，所以它*必定*在架构上是合理的，对吗？而且它让我们可以按照多年来一直使用的方式设计软件。

不幸的是，它更像是安慰剂而不是治疗。如果你浏览一下全局变量引起的问题列表，你会注意到单例模式一个都没有解决。这是因为单例*就是*全局状态——只不过被封装在一个类中。

### It solves two problems even when you just have one / 解决了两个问题，即使你只需要一个

* The word "and" in the Gang of Four's description of Singleton is a bit strange. Is this pattern a solution to one problem or two? What if we have only one of those? Ensuring a single instance is useful, but who says we want to let *everyone* poke at it? Likewise, global access is convenient, but that's true even for a class that allows multiple instances.

The latter of those two problems, convenient access, is almost always why we turn to the Singleton pattern. Consider a logging class. Most modules in the game can benefit from being able to log diagnostic information. However, passing an instance of our `Log` class to every single function clutters the method signature and distracts from the intent of the code.

The obvious fix is to make our `Log` class a singleton. Every function can then go straight to the class itself to get an instance. But when we do that, we inadvertently acquire a strange little restriction. All of a sudden, we can no longer create more than one logger.

At first, this isn't a problem. We're writing only a single log file, so we only need one instance anyway. Then, deep in the development cycle, we run into trouble. Everyone on the team has been using the logger for their own diagnostics, and the log file has become a massive dumping ground. Programmers have to wade through pages of text just to find the one entry they care about.

We'd like to fix this by partitioning the logging into multiple files. To do this, we'll have separate loggers for different game domains: online, UI, audio, gameplay. But we can't. Not only does our `Log` class no longer allow us to create multiple instances, that design limitation is entrenched in every single call site that uses it:

`Log::instance().write("Some event.");`

In order to make our `Log` class support multiple instantiation (like it originally did), we'll have to fix both the class itself and every line of code that mentions it. Our convenient access isn't so convenient anymore.

It could be even worse than this. Imagine your `Log` class is in a library being shared across several *games*. Now, to change the design, you'll have to coordinate the change across several groups of people, most of whom have neither the time nor the motivation to fix it.

**CN:** GoF 对单例的描述中的"并且"这个词有点奇怪。这个模式是解决一个问题还是两个问题？如果我们只需要其中之一呢？确保单一实例是有用的，但谁说我们想让*每个人*都来戳它？同样，全局访问是方便的，但这对于允许多个实例的类也同样成立。

这两个问题中的后一个——便捷访问——几乎总是我们求助于单例模式的原因。考虑一个日志类。游戏中的大多数模块都可以从记录诊断信息中受益。然而，将我们的 `Log` 类实例传递给每个函数会使方法签名变得杂乱，并分散代码意图。

明显的解决方法是让我们的 `Log` 类成为一个单例。然后每个函数可以直接访问类本身来获取一个实例。但当我们这样做时，我们无意中获得了一个奇怪的小限制。突然之间，我们不能再创建多个日志记录了。

起初，这不是问题。我们只写一个日志文件，所以无论如何我们只需要一个实例。然后，在开发周期的深处，我们遇到了麻烦。团队中的每个人都在使用日志记录器进行自己的诊断，日志文件变成了一个巨大的倾倒地。程序员不得不翻阅成页的文本来找到他们关心的那一条目。

我们希望通过将日志分区到多个文件来解决这个问题。为此，我们将为不同的游戏域设立单独的日志记录器：网络、UI、音频、游戏玩法。但我们做不到。不仅我们的 `Log` 类不再允许我们创建多个实例，而且这个设计限制已经根植于使用它的每一个调用点：

`Log::instance().write("Some event.");`

为了让我们的 `Log` 类支持多实例化（像它原本那样），我们必须修复类本身以及所有提到它的每一行代码。我们的便捷访问不再那么便捷了。

情况甚至可能比这更糟。想象一下你的 `Log` 类在一个被多个*游戏*共享的库中。现在，要更改设计，你必须在多个团队之间协调更改，而其中大多数人既没有时间也没有动力去修复它。

### Lazy initialization takes control away from you / 懒初始化让你失去控制

* In the desktop PC world of virtual memory and soft performance requirements, lazy initialization is a smart trick. Games are a different animal. Initializing a system can take time: allocating memory, loading resources, etc. If initializing the audio system takes a few hundred milliseconds, we need to control when that's going to happen. If we let it lazy-initialize itself the first time a sound plays, that could be in the middle of an action-packed part of the game, causing visibly dropped frames and stuttering gameplay.

Likewise, games generally need to closely control how memory is laid out in the heap to avoid fragmentation. If our audio system allocates a chunk of heap when it initializes, we want to know *when* that initialization is going to happen, so that we can control *where* in the heap that memory will live.

See [Object Pool](object-pool.html) for a detailed explanation of memory fragmentation.

Because of these two problems, most games I've seen don't rely on lazy initialization. Instead, they implement the Singleton pattern like this:

```cpp
class FileSystem {
public:
  static FileSystem& instance() { return instance_; }

private:
  FileSystem() {}

static FileSystem instance_;
};
```

That solves the lazy initialization problem, but at the expense of discarding several singleton features that *do* make it better than a raw global variable. With a static instance, we can no longer use polymorphism, and the class must be constructible at static initialization time. Nor can we free the memory that the instance is using when not needed.

Instead of creating a singleton, what we really have here is a simple static class. That isn't necessarily a bad thing, but if a static class is all you need, why not get rid of the `instance()` method entirely and use static functions instead? Calling `Foo::bar()` is simpler than `Foo::instance().bar()`, and also makes it clear that you really are dealing with static memory.

The usual argument for choosing singletons over static classes is that if you decide to change the static class into a non-static one later, you'll need to fix every call site. In theory, you don't have to do that with singletons because you could be passing the instance around and calling it like a normal instance method.

In practice, I've never seen it work that way. Everyone just does `Foo::instance().bar()` in one line. If we changed Foo to not be a singleton, we'd still have to touch every call site. Given that, I'd rather have a simpler class and a simpler syntax to call into it.

**CN:** 在具有虚拟内存和软性能要求的桌面 PC 世界中，懒初始化是一个聪明的技巧。游戏则不同。初始化一个系统可能需要时间：分配内存、加载资源等。如果初始化音频系统需要几百毫秒，我们需要控制何时发生。如果我们让它在第一次播放声音时懒初始化自己，那可能发生在游戏动作密集的部分，导致明显的掉帧和游戏卡顿。

同样，游戏通常需要严格控制内存在堆中的布局以避免碎片化。如果我们的音频系统在初始化时分配了一大块堆内存，我们想知道初始化*何时*发生，以便我们控制该内存在堆中的*位置*。

关于内存碎片化的详细说明，请参见[对象池](object-pool.html)。

由于这两个问题，我见过的大多数游戏都不依赖懒初始化。相反，他们这样实现单例模式：

```cpp
class FileSystem {
public:
  static FileSystem& instance() { return instance_; }

private:
  FileSystem() {}

static FileSystem instance_;
};
```

这解决了懒初始化问题，但代价是丢弃了几个确实使其优于原始全局变量的单例特性。使用静态实例，我们不能再使用多态，并且类必须在静态初始化时可构造。我们也不能在不需要时释放实例正在使用的内存。

我们这里所拥有的实际上不是一个单例，而是一个简单的静态类。这不一定是一件坏事，但如果一个静态类就是你所需要的，为什么不完全去掉 `instance()` 方法而直接使用静态函数呢？调用 `Foo::bar()` 比 `Foo::instance().bar()` 更简单，而且清楚地表明你确实在处理静态内存。

选择单例而非静态类的常见论点是，如果你以后决定将静态类改为非静态类，你需要修复每一个调用点。理论上，使用单例你不需要这样做，因为你可以传递实例并像普通实例方法一样调用它。

在实践中，我从未见过它这样工作。每个人都是在一行中写 `Foo::instance().bar()`。如果我们把 Foo 改成非单例，我们仍然需要触及每一个调用点。鉴于此，我更愿意有一个更简单的类和更简单的语法来调用它。

## Key Differences / 关键差异

| Aspect | C++ (原书) | C# (Unity) |
|--------|-----------|------------|
| **Memory model** | Manual heap allocation (`new`) | CLR-managed heap, GC collects |
| **Thread safety** | C++11 local static guarantees single init | CLR static init is inherently thread-safe |
| **Construction** | Constructor is private, `new` in static method | Same concept, but CLR handles type initialization |
| **Destruction** | Manual cleanup needed | No explicit destructor needed (GC) |
| **Unity-specific** | No equivalent | Must inherit `MonoBehaviour`, attach to GameObject |
| **Polymorphism** | Virtual methods + platform `#define` | Can use abstract base + IoC container |
| **Lazy initialization** | Manual null check pattern | `Lazy<T>` or `System.Lazy` built-in |

## See Also / 扩展阅读

* The question remains, where *should* we use the real Singleton pattern? Honestly, I've never used the full Gang of Four implementation in a game. To ensure single instantiation, I usually simply use a static class. If that doesn't work, I'll use a static flag to check at runtime that only one instance of the class is constructed.

There are a couple of other chapters in this book that can also help here. The [Subclass Sandbox](subclass-sandbox.html) pattern gives instances of a class access to some shared state without making it globally available. The [Service Locator](service-locator.html) pattern *does* make an object globally available, but it gives you more flexibility with how that object is configured.

**CN:** 问题依然存在——我们*应该*在何处使用真正的单例模式？说实话，我从未在游戏中使用过完整的 GoF 实现。为确保单一实例化，我通常简单地使用静态类。如果那行不通，我会使用一个静态标志在运行时检查是否只构造了该类的一个实例。

本书中还有另外两章可以帮助解决这个问题。[子类沙盒](subclass-sandbox.html)模式让类的实例可以访问一些共享状态，而不必将其设为全局可用。[服务定位器](service-locator.html)模式*确实*使对象全局可用，但它在该对象的配置方式上给了你更多灵活性。

### Summary of Alternatives / 替代方案总结

1. **Pass it in (Dependency Injection):** Pass dependencies as parameters instead of reaching out globally. The simplest solution, often the best.
2. **Base class access (Subclass Sandbox):** Provide shared state through a base class — see `getLog()` example above.
3. **Service Locator:** A single global that provides access to registered services (more flexible than Singleton).
4. **Static class:** If you don't need polymorphism or lazy init, just use a static class with static functions.
5. **Assertion guard:** Use a static flag to assert only one instance is constructed, without providing global access.
6. **Piggyback on an existing global:** Route access through an already-global `Game` or `World` object.

---

# State Pattern — 状态模式

> **EN:** Allow an object to alter its behavior when its internal state changes. The object will appear to change its class.
> **CN:** 允许一个对象在其内部状态改变时改变其行为，使对象看起来像是修改了自己的类。

## Intent / 意图

* Allow an object to alter its behavior when its internal state changes. The object will appear to change its class.

* 允许一个对象在其内部状态改变时改变其行为，使对象看起来像是修改了自己的类。

## Motivation / 动机

* Confession time: I went a little overboard and packed way too much into this chapter. It's ostensibly about the State design pattern, but I can't talk about that and games without going into the more fundamental concept of *finite state machines* (or "FSMs"). But then once I went there, I figured I might as well introduce *hierarchical state machines* and *pushdown automata*.

That's a lot to cover, so to keep things as short as possible, the code samples here leave out a few details that you'll have to fill in on your own. I hope they're still clear enough for you to get the big picture.

Don't feel sad if you've never heard of a state machine. While well known to AI and compiler hackers, they aren't that familiar to other programming circles. I think they should be more widely known, so I'm going to throw them at a different kind of problem here.

This pairing echoes the early days of artificial intelligence. In the '50s and '60s, much of AI research was focused on language processing. Many of the techniques compilers now use for parsing programming languages were invented for parsing human languages.

**CN:** 坦白说：这一章我有点用力过猛，塞了太多内容。表面上它讲的是状态模式，但如果不先介绍更基础的*有限状态机*（FSM）概念，就无法在游戏中讨论它。而一旦我开始讲这个，我想不妨再介绍一下*层级状态机*和*下推自动机*。

内容很多，为了尽量简短，这里的代码示例省略了一些细节，你需要自己补充。希望它们仍然足够清晰，让你能把握整体。

如果你从没听说过状态机，别难过。虽然对 AI 和编译器开发者来说它耳熟能详，但在其他编程圈子里并不常见。我认为它应该被更广泛地了解，所以我将用一种不同的问题来介绍它。

这种配对呼应了人工智能的早期时代。在五六十年代，大量 AI 研究都集中在语言处理上。编译器现在用于解析编程语言的许多技术，最初都是为了解析人类语言而发明的。

---

### We've All Been There / 我们都经历过

* We're working on a little side-scrolling platformer. Our job is to implement the heroine that is the player's avatar in the game world. That means making her respond to user input. Push the B button and she should jump. Simple enough:

```cpp
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    yVelocity_ = JUMP_VELOCITY;
    setGraphics(IMAGE_JUMP);
  }
}
```

Spot the bug?

There's nothing to prevent "air jumping" — keep hammering B while she's in the air, and she will float forever. The simple fix is to add an `isJumping_` Boolean field to `Heroine` that tracks when she's jumping, and then do:

```cpp
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    if (!isJumping_)
    {
      isJumping_ = true;
      // Jump...
    }
  }
}
```

There should also be code that sets `isJumping_` back to `false` when the heroine touches the ground. I've omitted that here for brevity's sake.

Next, we want the heroine to duck if the player presses down while she's on the ground and stand back up when the button is released:

```cpp
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    // Jump if not jumping...
  }
  else if (input == PRESS_DOWN)
  {
    if (!isJumping_)
    {
      setGraphics(IMAGE_DUCK);
    }
  }
  else if (input == RELEASE_DOWN)
  {
    setGraphics(IMAGE_STAND);
  }
}
```

Spot the bug this time?

With this code, the player could:

1. Press down to duck.
2. Press B to jump from a ducking position.
3. Release down while still in the air.

The heroine will switch to her standing graphic in the middle of the jump. Time for another flag…

```cpp
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    if (!isJumping_ && !isDucking_)
    {
      // Jump...
    }
  }
  else if (input == PRESS_DOWN)
  {
    if (!isJumping_)
    {
      isDucking_ = true;
      setGraphics(IMAGE_DUCK);
    }
  }
  else if (input == RELEASE_DOWN)
  {
    if (isDucking_)
    {
      isDucking_ = false;
      setGraphics(IMAGE_STAND);
    }
  }
}
```

Next, it would be cool if the heroine did a dive attack if the player presses down in the middle of a jump:

```cpp
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    if (!isJumping_ && !isDucking_)
    {
      // Jump...
    }
  }
  else if (input == PRESS_DOWN)
  {
    if (!isJumping_)
    {
      isDucking_ = true;
      setGraphics(IMAGE_DUCK);
    }
    else
    {
      isJumping_ = false;
      setGraphics(IMAGE_DIVE);
    }
  }
  else if (input == RELEASE_DOWN)
  {
    if (isDucking_)
    {
      // Stand...
    }
  }
}
```

Bug hunting time again. Find it?

We check that you can't air jump while jumping, but not while diving. Yet another field…

Something is clearly wrong with our approach. Every time we touch this handful of code, we break something. We need to add a bunch more moves — we haven't even added *walking* yet — but at this rate, it will collapse into a heap of bugs before we're done with it.

Those coders you idolize who always seem to create flawless code aren't simply superhuman programmers. Instead, they have an intuition about which *kinds* of code are error-prone, and they steer away from them.

Complex branching and mutable state — fields that change over time — are two of those error-prone kinds of code, and the examples above have both.

**CN:** 我们正在开发一个小型横版过关游戏。任务是实现玩家在游戏世界中的化身——女主角。这意味着让她响应用户输入。按 B 键，她应该跳起来。很简单：

[上述 C++ 代码]

找到 Bug 了吗？

没有任何机制能防止"空中跳跃"——在空中猛按 B 键，她就会永远漂浮。简单的修复是给 `Heroine` 添加一个 `isJumping_` 布尔字段来追踪她是否在跳跃：

[上述 C++ 代码]

当然还需要在女主角落地时将 `isJumping_` 设回 `false` 的代码。为简洁起见，此处省略。

接下来，我们希望玩家在地面上按下键时女主角蹲下，松开按键时站起来：

[上述 C++ 代码]

这次找到 Bug 了吗？

使用这段代码，玩家可以：

1. 按下键蹲下。
2. 从蹲伏状态按 B 键跳跃。
3. 在空中松开下键。

女主角会在跳跃中途切换到站立动画。又得加一个标志了……

[上述 C++ 代码]

接下来，如果玩家在跳跃中按下键，女主角做下落攻击，那会很酷：

[上述 C++ 代码]

又到了找 Bug 时间。找到了吗？

我们检查了跳跃时不能二段跳，但没有检查下落时。又一个字段……

我们的方法显然有问题。每次碰这段代码都会搞坏点什么。我们还需要添加更多动作——我们甚至还没加入*行走*——但按这个速度，在完成之前它就会崩塌成一堆 Bug。

你崇拜的那些似乎总能写出完美代码的程序员并非超人。相反，他们对哪些*类型*的代码容易出错有直觉，并且会避开它们。

复杂的分支和可变状态——随时间变化的字段——就是两种容易出错的代码，而上面的例子两者兼备。

---

### Finite State Machines to the Rescue / 有限状态机来帮忙

* In a fit of frustration, you sweep everything off your desk except a pen and paper and start drawing a flowchart. You draw a box for each thing the heroine can be doing: standing, jumping, ducking, and diving. When she can respond to a button press in one of those states, you draw an arrow from that box, label it with that button, and connect it to the state she changes to.

![A flowchart containing boxes for Standing, Jumping, Diving, and Ducking. Arrows for button presses and releases connect some of the boxes.](images/state-flowchart.png)

Congratulations, you've just created a *finite state machine*. These came out of a branch of computer science called *automata theory* whose family of data structures also includes the famous Turing machine. FSMs are the simplest member of that family.

The gist is:

- **You have a fixed *set of states* that the machine can be in.** For our example, that's standing, jumping, ducking, and diving.

- **The machine can only be in *one* state at a time.** Our heroine can't be jumping and standing simultaneously. In fact, preventing that is one reason we're going to use an FSM.

- **A sequence of *inputs* or *events* is sent to the machine.** In our example, that's the raw button presses and releases.

- **Each state has a *set of transitions*, each associated with an input and pointing to a state.** When an input comes in, if it matches a transition for the current state, the machine changes to the state that transition points to.

For example, pressing down while standing transitions to the ducking state. Pressing down while jumping transitions to diving. If no transition is defined for an input on the current state, the input is ignored.

In their pure form, that's the whole banana: states, inputs, and transitions. You can draw it out like a little flowchart. Unfortunately, the compiler doesn't recognize our scribbles, so how do we go about *implementing* one? The Gang of Four's State pattern is one method — which we'll get to — but let's start simpler.

My favorite analogy for FSMs is the old text adventure games like Zork. You have a world of rooms that are connected to each other by exits. You explore them by entering commands like "go north".

This maps directly to a state machine: Each room is a state. The room you're in is the current state. Each room's exits are its transitions. The navigation commands are the inputs.

**CN:** 挫败之中，你把桌上所有东西都扫到一边，只留下笔和纸，开始画流程图。你为女主角可能做的每件事画一个框：站立、跳跃、蹲伏和俯冲。当她在某个状态下能响应按钮按下时，你从那个框画出一个箭头，标上按钮名称，连接到她会切换到的状态。

恭喜，你刚刚创建了一个*有限状态机*。这来自计算机科学的一个分支——*自动机理论*，其数据结构家族还包括著名的图灵机。FSM 是该家族中最简单的成员。

要点如下：

- **机器有一个固定的*状态集合*可以处于其中。** 在我们的例子中，就是站立、跳跃、蹲伏和俯冲。
- **机器一次只能处于*一个*状态。** 我们的女主角不能同时跳跃和站立。事实上，防止这种情况正是我们使用 FSM 的原因之一。
- **一系列*输入*或*事件*被发送给机器。** 在我们的例子中，就是原始的按钮按下和释放。
- **每个状态有一组*转换*，每个转换关联一个输入并指向一个状态。** 当输入到来时，如果它匹配当前状态的某个转换，机器就转换到该转换指向的状态。

例如，站立时按下键转换到蹲伏状态。跳跃时按下键转换到俯冲。如果当前状态没有为某个输入定义转换，则忽略该输入。

在最纯粹的形式下，这就是全部内容：状态、输入和转换。你可以像画小流程图一样画出来。不幸的是，编译器不认识我们的涂鸦，那么如何*实现*它呢？四人帮的状态模式是一种方法——我们稍后会讲到——但让我们从更简单的开始。

我最喜欢的 FSM 类比是像 Zork 这样的古老文字冒险游戏。你有一个由房间组成的世界，房间之间由出口连接。你通过输入"向北走"这样的命令来探索。

这直接对应到状态机：每个房间是一个状态。你所在的房间是当前状态。每个房间的出口是它的转换。导航命令是输入。

---

### Enums and Switches / 枚举与 Switch

* One problem our `Heroine` class has is some combinations of those Boolean fields aren't valid: `isJumping_` and `isDucking_` should never both be true, for example. When you have a handful of flags where only one is `true` at a time, that's a hint that what you really want is an `enum`.

In this case, that `enum` is exactly the set of states for our FSM, so let's define that:

```cpp
enum State
{
  STATE_STANDING,
  STATE_JUMPING,
  STATE_DUCKING,
  STATE_DIVING
};
```

Instead of a bunch of flags, `Heroine` will just have one `state_` field. We also flip the order of our branching. In the previous code, we switched on input, *then* on state. This kept the code for handling one button press together, but it smeared around the code for one state. We want to keep that together, so we switch on state first. That gives us:

```cpp
void Heroine::handleInput(Input input)
{
  switch (state_)
  {
    case STATE_STANDING:
      if (input == PRESS_B)
      {
        state_ = STATE_JUMPING;
        yVelocity_ = JUMP_VELOCITY;
        setGraphics(IMAGE_JUMP);
      }
      else if (input == PRESS_DOWN)
      {
        state_ = STATE_DUCKING;
        setGraphics(IMAGE_DUCK);
      }
      break;

case STATE_JUMPING:
      if (input == PRESS_DOWN)
      {
        state_ = STATE_DIVING;
        setGraphics(IMAGE_DIVE);
      }
      break;

case STATE_DUCKING:
      if (input == RELEASE_DOWN)
      {
        state_ = STATE_STANDING;
        setGraphics(IMAGE_STAND);
      }
      break;
  }
}
```

This seems trivial, but it's a real improvement over the previous code. We still have some conditional branching, but we simplified the mutable state to a single field. All of the code for handling a single state is now nicely lumped together. This is the simplest way to implement a state machine and is fine for some uses.

In particular, the heroine can no longer be in an *invalid* state. With the Boolean flags, some sets of values were possible but meaningless. With the `enum`, each value is valid.

Your problem may outgrow this solution, though. Say we want to add a move where our heroine can duck for a while to charge up and unleash a special attack. While she's ducking, we need to track the charge time.

We add a `chargeTime_` field to `Heroine` to store how long the attack has charged. Assume we already have an `update()` that gets called each frame. In there, we add:

```cpp
void Heroine::update()
{
  if (state_ == STATE_DUCKING)
  {
    chargeTime_++;
    if (chargeTime_ > MAX_CHARGE)
    {
      superBomb();
    }
  }
}
```

If you guessed that this is the Update Method pattern, you win a prize!

We need to reset the timer when she starts ducking, so we modify `handleInput()`:

```cpp
void Heroine::handleInput(Input input)
{
  switch (state_)
  {
    case STATE_STANDING:
      if (input == PRESS_DOWN)
      {
        state_ = STATE_DUCKING;
        chargeTime_ = 0;
        setGraphics(IMAGE_DUCK);
      }
      // Handle other inputs...
      break;

// Other states...
  }
}
```

All in all, to add this charge attack, we had to modify two methods and add a `chargeTime_` field onto `Heroine` even though it's only meaningful while in the ducking state. What we'd prefer is to have all of that code and data nicely wrapped up in one place. The Gang of Four has us covered.

**CN:** 我们的 `Heroine` 类的一个问题是某些布尔字段的组合是无效的：例如，`isJumping_` 和 `isDucking_` 不应该同时为 true。当你有一堆标志，但一次只有一个为 `true` 时，这暗示你真正需要的是一个 `enum`。

在这种情况下，这个 `enum` 正是我们 FSM 的状态集合，所以让我们定义它：

[上述 C++ 代码]

`Heroine` 不再需要一堆标志，只需要一个 `state_` 字段。我们也翻转了分支的顺序。在之前的代码中，我们先对输入进行 switch，*然后*对状态进行 switch。这让处理一个按钮按压的代码在一起，但分散了同一个状态的代码。我们想把同一个状态的代码放在一起，所以我们先对状态进行 switch：

[上述 C++ 代码]

这看起来很简单，但比之前的代码有了实质性的改进。我们仍然有一些条件分支，但将可变状态简化为一个字段。处理单个状态的所有代码现在都很好地集中在一起。这是实现状态机最简单的方式，对某些用途来说已经足够了。

特别是，女主角不再可能处于*无效*状态。使用布尔标志时，某些值组合可能存在但毫无意义。使用 `enum`，每个值都是有效的。

不过，你的问题可能会超出这个方案的适用范围。假设我们想添加一个动作：女主角可以蹲伏一段时间来蓄力，然后释放一个特殊攻击。在她蹲伏时，我们需要追踪蓄力时间。

我们给 `Heroine` 添加一个 `chargeTime_` 字段来存储攻击蓄力的时长。假设我们已经有一个每帧调用的 `update()`。在其中，我们添加：

[上述 C++ 代码]

如果你猜到这是更新模式，你赢了！

我们需要在她开始蹲伏时重置计时器，所以修改 `handleInput()`：

[上述 C++ 代码]

总之，为了添加这个蓄力攻击，我们不得不修改两个方法，并在 `Heroine` 上添加一个 `chargeTime_` 字段——尽管它只在蹲伏状态中有意义。我们更希望所有这些代码和数据都很好地封装在一个地方。四人帮已经为我们准备好了方案。

---

## The Pattern / 模式描述

* For people deeply into the object-oriented mindset, every conditional branch is an opportunity to use dynamic dispatch (in other words a virtual method call in C++). I think you can go too far down that rabbit hole. Sometimes an `if` is all you need.

There's a historical basis for this. Many of the original object-oriented apostles like *Design Patterns*' Gang of Four, and *Refactoring*'s Martin Fowler came from Smalltalk. There, `ifThen:` is just a method you invoke on the condition, which is implemented differently by the `true` and `false` objects.

But in our example, we've reached a tipping point where something object-oriented is a better fit. That gets us to the State pattern. In the words of the Gang of Four:

> Allow an object to alter its behavior when its internal state changes. The object will appear to change its class.

That doesn't tell us much. Heck, our `switch` does that. The concrete pattern they describe looks like this when applied to our heroine:

**CN:** 对于深度面向对象思维的人来说，每个条件分支都是使用动态分发（在 C++ 中就是虚方法调用）的机会。我认为你可能在这条路上走得太远。有时候一个 `if` 就足够了。

这有历史原因。许多原初的面向对象使徒——比如《设计模式》的四人帮和《重构》的 Martin Fowler——都来自 Smalltalk 世界。在那里，`ifThen:` 只是你在条件上调用的一個方法，由 `true` 和 `false` 对象以不同方式实现。

但在我们的例子中，我们已经达到了一个临界点，面向对象的方式更合适。这就引出了状态模式。用四人帮的话来说：

> 允许一个对象在其内部状态改变时改变其行为，使对象看起来像是修改了自己的类。

这并没有告诉我们太多。其实，我们的 `switch` 也做到了这一点。他们描述的具体模式应用到我们的女主角身上时是这样的：

---

### A State Interface / 状态接口

* First, we define an interface for the state. Every bit of behavior that is state-dependent — every place we had a `switch` before — becomes a virtual method in that interface. For us, that's `handleInput()` and `update()`:

```cpp
class HeroineState
{
public:
  virtual ~HeroineState() {}
  virtual void handleInput(Heroine& heroine, Input input) {}
  virtual void update(Heroine& heroine) {}
};
```

**CN:** 首先，我们为状态定义一个接口。所有依赖于状态的行为——之前每个出现 `switch` 的地方——都成为该接口中的虚方法。对我们来说，就是 `handleInput()` 和 `update()`：

[上述 C++ 代码]

---

### Classes for Each State / 每个状态对应一个类

* For each state, we define a class that implements the interface. Its methods define the heroine's behavior when in that state. In other words, take each `case` from the earlier `switch` statements and move them into their state's class. For example:

```cpp
class DuckingState : public HeroineState
{
public:
  DuckingState()
  : chargeTime_(0)
  {}

virtual void handleInput(Heroine& heroine, Input input)
  {
    if (input == RELEASE_DOWN)
    {
      // Change to standing state...
      heroine.setGraphics(IMAGE_STAND);
    }
  }

virtual void update(Heroine& heroine)
  {
    chargeTime_++;
    if (chargeTime_ > MAX_CHARGE)
    {
      heroine.superBomb();
    }
  }

private:
  int chargeTime_;
};
```

Note that we also moved `chargeTime_` out of `Heroine` and into the `DuckingState` class. This is great — that piece of data is only meaningful while in that state, and now our object model reflects that explicitly.

**CN:** 对于每个状态，我们定义一个实现该接口的类。它的方法定义了女主角在该状态下的行为。换句话说，把之前 `switch` 语句中的每个 `case` 移到其状态类中。例如：

[上述 C++ 代码]

注意我们还把 `chargeTime_` 从 `Heroine` 移到了 `DuckingState` 类中。这很好——那段数据只在蹲伏状态中有意义，现在我们的对象模型明确反映了这一点。

---

### Delegate to the State / 委托给状态

* Next, we give the `Heroine` a pointer to her current state, lose each big `switch`, and delegate to the state instead:

```cpp
class Heroine
{
public:
  virtual void handleInput(Input input)
  {
    state_->handleInput(*this, input);
  }

virtual void update()
  {
    state_->update(*this);
  }

// Other methods...
private:
  HeroineState* state_;
};
```

In order to "change state", we just need to assign `state_` to point to a different `HeroineState` object. That's the State pattern in its entirety.

This looks like the Strategy and Type Object patterns. In all three, you have a main object that delegates to another subordinate one. The difference is *intent*.

- With Strategy, the goal is to *decouple* the main class from some portion of its behavior.

- With Type Object, the goal is to make a *number* of objects behave similarly by *sharing* a reference to the same type object.

- With State, the goal is for the main object to *change* its behavior by *changing* the object it delegates to.

**CN:** 接下来，我们给 `Heroine` 一个指向当前状态的指针，去掉每个大的 `switch`，转而委托给状态：

[上述 C++ 代码]

要"改变状态"，我们只需将 `state_` 赋值为指向一个不同的 `HeroineState` 对象。这就是完整的状态模式。

这看起来像策略模式和类型对象模式。在这三种模式中，你都有一个主对象委托给另一个从属对象。区别在于*意图*。

- 在策略模式中，目标是将主类与其行为的某些部分*解耦*。
- 在类型对象模式中，目标是让*多个*对象通过*共享*同一个类型对象的引用来表现相似的行为。
- 在状态模式中，目标是让主对象通过*改变*其委托的对象来*改变*其行为。

---

### Where Are the State Objects? / 状态对象在哪里？

* I did gloss over one bit here. To change states, we need to assign `state_` to point to the new one, but where does that object come from? With our `enum` implementation, that was a no-brainer — `enum` values are primitives like numbers. But now our states are classes, which means we need an actual instance to point to. There are two common answers to this:

* 我这里确实忽略了一点。要改变状态，我们需要将 `state_` 赋值为指向新状态，但这个对象从哪来？使用 `enum` 实现时，这显而易见——`enum` 值就像数字一样的原始类型。但现在我们的状态是类，这意味着我们需要一个实际的实例来指向。有两种常见的解决方案：

#### Static States / 静态状态

* If the state object doesn't have any other fields, then the only data it stores is a pointer to the internal virtual method table so that its methods can be called. In that case, there's no reason to ever have more than one instance of it. Every instance would be identical anyway.

If your state has no fields and only *one* virtual method in it, you can simplify this pattern even more. Replace each state *class* with a state *function* — just a plain vanilla top-level function. Then, the `state_` field in your main class becomes a simple function pointer.

In that case, you can make a single *static* instance. Even if you have a bunch of FSMs all going at the same time in that same state, they can all point to the same instance since it has nothing machine-specific about it.

This is the Flyweight pattern.

*Where* you put that static instance is up to you. Find a place that makes sense. For no particular reason, let's put ours inside the base state class:

```cpp
class HeroineState
{
public:
  static StandingState standing;
  static DuckingState ducking;
  static JumpingState jumping;
  static DivingState diving;

// Other code...
};
```

Each of those static fields is the one instance of that state that the game uses. To make the heroine jump, the standing state would do something like:

```cpp
if (input == PRESS_B)
{
  heroine.state_ = &HeroineState::jumping;
  heroine.setGraphics(IMAGE_JUMP);
}
```

**CN:** 如果状态对象没有任何其他字段，那么它存储的唯一数据就是一个指向内部虚方法表的指针，以便调用它的方法。在这种情况下，没有理由拥有多个实例。每个实例都是相同的。

如果你的状态没有字段，且只有一个虚方法，你可以进一步简化这个模式。将每个状态*类*替换为一个状态*函数*——就是一个普通顶层函数。那么主类中的 `state_` 字段就变成了一个简单的函数指针。

在这种情况下，你可以创建一个单一的*静态*实例。即使你有一堆 FSM 同时处于同一状态，它们都可以指向同一个实例，因为它没有任何机器特定的数据。

这就是享元模式。

*在哪里*放置这个静态实例取决于你。找一个合理的位置。出于某种原因，我们把它放在基状态类中：

[上述 C++ 代码]

每个静态字段都是游戏使用的该状态的一个实例。要让女主角跳跃，站立状态会这样做：

[上述 C++ 代码]

#### Instantiated States / 实例化状态

* Sometimes, though, this doesn't fly. A static state won't work for the ducking state. It has a `chargeTime_` field, and that's specific to the heroine that happens to be ducking. This may coincidentally work in our game if there's only one heroine, but if we try to add two-player co-op and have two heroines on screen at the same time, we'll have problems.

In that case, we have to create a state object when we transition to it. This lets each FSM have its own instance of the state. Of course, if we're allocating a *new* state, that means we need to free the *current* one. We have to be careful here, since the code that's triggering the change is in a method in the current state. We don't want to delete `this` out from under ourselves.

Instead, we'll allow `handleInput()` in `HeroineState` to optionally return a new state. When it does, `Heroine` will delete the old one and swap in the new one, like so:

```cpp
void Heroine::handleInput(Input input)
{
  HeroineState* state = state_->handleInput(*this, input);
  if (state != NULL)
  {
    delete state_;
    state_ = state;
  }
}
```

That way, we don't delete the previous state until we've returned from its method. Now, the standing state can transition to ducking by creating a new instance:

```cpp
HeroineState* StandingState::handleInput(Heroine& heroine,
                                         Input input)
{
  if (input == PRESS_DOWN)
  {
    // Other code...
    return new DuckingState();
  }

// Stay in this state.
  return NULL;
}
```

When I can, I prefer to use static states since they don't burn memory and CPU cycles allocating objects each state change. For states that are more, uh, *stateful*, though, this is the way to go.

When you dynamically allocate states, you may have to worry about fragmentation. The Object Pool pattern can help.

**CN:** 但有时候这种方法行不通。静态状态对蹲伏状态无效。它有一个 `chargeTime_` 字段，这是特定于正在蹲伏的女主角的。如果我们的游戏中只有一个女主角，这可能碰巧能工作，但如果我们尝试添加双人合作模式，屏幕上同时有两个女主角，就会出问题。

在这种情况下，我们必须在转换到状态时创建一个状态对象。这样每个 FSM 都有自己的状态实例。当然，如果我们要分配*新*状态，就意味着需要释放*当前*状态。这里我们必须小心，因为触发更改的代码在当前状态的方法中。我们不能在自己脚下删除 `this`。

相反，我们让 `HeroineState` 中的 `handleInput()` 可选地返回一个新状态。当它返回时，`Heroine` 会删除旧状态并换入新状态：

[上述 C++ 代码]

这样，我们直到从当前状态的方法返回后才删除前一个状态。现在，站立状态可以通过创建新实例来转换到蹲伏：

[上述 C++ 代码]

只要可能，我更喜欢使用静态状态，因为它们不会在每次状态转换时消耗内存和 CPU 周期来分配对象。但对于那些……呃……更有"状态"的状态，这是正确的方法。

当你动态分配状态时，可能需要担心碎片问题。对象池模式可以帮忙。

---

### Enter and Exit Actions / 进入与退出行为

* The goal of the State pattern is to encapsulate all of the behavior and data for one state in a single class. We're partway there, but we still have some loose ends.

When the heroine changes state, we also switch her sprite. Right now, that code is owned by the state she's switching *from*. When she goes from ducking to standing, the ducking state sets her image:

```cpp
HeroineState* DuckingState::handleInput(Heroine& heroine,
                                        Input input)
{
  if (input == RELEASE_DOWN)
  {
    heroine.setGraphics(IMAGE_STAND);
    return new StandingState();
  }

// Other code...
}
```

What we really want is each state to control its own graphics. We can handle that by giving the state an *entry action*:

```cpp
class StandingState : public HeroineState
{
public:
  virtual void enter(Heroine& heroine)
  {
    heroine.setGraphics(IMAGE_STAND);
  }

// Other code...
};
```

Back in `Heroine`, we modify the code for handling state changes to call that on the new state:

```cpp
void Heroine::handleInput(Input input)
{
  HeroineState* state = state_->handleInput(*this, input);
  if (state != NULL)
  {
    delete state_;
    state_ = state;

// Call the enter action on the new state.
    state_->enter(*this);
  }
}
```

This lets us simplify the ducking code to:

```cpp
HeroineState* DuckingState::handleInput(Heroine& heroine,
                                        Input input)
{
  if (input == RELEASE_DOWN)
  {
    return new StandingState();
  }

// Other code...
}
```

All it does is switch to standing and the standing state takes care of the graphics. Now our states really are encapsulated. One particularly nice thing about entry actions is that they run when you enter the state regardless of which state you're coming *from*.

Most real-world state graphs have multiple transitions into the same state. For example, our heroine will also end up standing after she lands a jump or dive. That means we would end up duplicating some code everywhere that transition occurs. Entry actions give us a place to consolidate that.

We can, of course, also extend this to support an *exit action*. This is just a method we call on the state we're *leaving* right before we switch to the new state.

**CN:** 状态模式的目标是将一个状态的所有行为和数据封装在单个类中。我们已经完成了一部分，但仍有一些未尽事宜。

当女主角改变状态时，我们也会切换她的精灵。目前，这段代码属于她正在*离开*的状态。当她从蹲伏到站立时，蹲伏状态设置她的图像：

[上述 C++ 代码]

我们真正想要的是每个状态控制自己的图形。我们可以通过给状态一个*进入动作*来处理：

[上述 C++ 代码]

回到 `Heroine`，我们修改处理状态切换的代码，在新状态上调用 enter：

[上述 C++ 代码]

这让我们可以把蹲伏代码简化为：

[上述 C++ 代码]

它所做的只是切换到站立状态，而站立状态负责处理图形。现在我们的状态真正被封装了。进入动作的一个特别好的特性是，无论你从*哪个*状态进入，它们都会在进入状态时执行。

大多数真实世界的状态图都有多个转换进入同一个状态。例如，我们的女主角在跳跃或俯冲落地后也会回到站立状态。这意味着我们需要在每次这种转换发生的地方重复一些代码。进入动作给了我们一个集中处理的地方。

当然，我们也可以扩展以支持*退出动作*。这就是在切换到新状态之前，在我们*离开*的状态上调用一个方法。

---

### What's the Catch? / 有什么问题？

* I've spent all this time selling you on FSMs, and now I'm going to pull the rug out from under you. Everything I've said so far is true, and FSMs are a good fit for some problems. But their greatest virtue is also their greatest flaw.

State machines help you untangle hairy code by enforcing a very constrained structure on it. All you've got is a fixed set of states, a single current state, and some hardcoded transitions.

A finite state machine isn't even *Turing complete*. Automata theory describes computation using a series of abstract models, each more complex than the previous. A *Turing machine* is one of the most expressive models.

"Turing complete" means a system (usually a programming language) is powerful enough to implement a Turing machine in it, which means all Turing complete languages are, in some ways, equally expressive. FSMs are not flexible enough to be in that club.

If you try using a state machine for something more complex like game AI, you will slam face-first into the limitations of that model. Thankfully, our forebears have found ways to dodge some of those barriers. I'll close this chapter out by walking you through a couple of them.

**CN:** 我花了这么长时间向你推销 FSM，现在我要拆台了。我前面说的都是真的，FSM 适合某些问题。但它们最大的优点也是最大的缺点。

状态机通过强制一个非常受限的结构来帮助你理清混乱的代码。你拥有的只是一个固定的状态集合、一个当前状态和一些硬编码的转换。

有限状态机甚至不是*图灵完备*的。自动机理论用一系列抽象模型来描述计算，每个模型都比前一个更复杂。*图灵机*是其中表达能力最强的模型之一。

"图灵完备"意味着一个系统（通常是一种编程语言）足够强大，可以在其中实现图灵机，这意味着所有图灵完备的语言在某些方面具有同等的表达能力。FSM 不够灵活，无法加入这个俱乐部。

如果你尝试将状态机用于更复杂的事情，比如游戏 AI，你会直接撞上该模型的限制。幸运的是，我们的前辈找到了一些绕过这些障碍的方法。我将在本章结束时介绍其中几个。

---

### Concurrent State Machines / 并发状态机

* We've decided to give our heroine the ability to carry a gun. When she's packing heat, she can still do everything she could before: run, jump, duck, etc. But she also needs to be able to fire her weapon while doing it.

If we want to stick to the confines of an FSM, we have to *double* the number of states we have. For each existing state, we'll need another one for doing the same thing while she's armed: standing, standing with gun, jumping, jumping with gun, you get the idea.

Add a couple of more weapons and the number of states explodes combinatorially. Not only is it a huge number of states, it's a huge amount of redundancy: the unarmed and armed states are almost identical except for the little bit of code to handle firing.

The problem is that we've jammed two pieces of state — what she's *doing* and what she's *carrying* — into a single machine. To model all possible combinations, we would need a state for each *pair*. The fix is obvious: have two separate state machines.

If we want to cram *n* states for what she's doing and *m* states for what she's carrying into a single machine, we need *n × m* states. With two machines, it's just *n + m*.

We keep our original state machine for what she's doing and leave it alone. Then we define a separate state machine for what she's carrying. `Heroine` will have *two* "state" references, one for each, like:

```cpp
class Heroine
{
  // Other code...

private:
  HeroineState* state_;
  HeroineState* equipment_;
};
```

For illustrative purposes, we're using the full State pattern for her equipment. In practice, since it only has two states, a Boolean flag would work too.

When the heroine delegates inputs to the states, she hands it to both of them:

```cpp
void Heroine::handleInput(Input input)
{
  state_->handleInput(*this, input);
  equipment_->handleInput(*this, input);
}
```

A more full-featured system would probably have a way for one state machine to *consume* an input so that the other doesn't receive it. That would prevent both machines from erroneously trying to respond to the same input.

Each state machine can then respond to inputs, spawn behavior, and change its state independently of the other machine. When the two sets of states are mostly unrelated, this works well.

In practice, you'll find a few cases where the states do interact. For example, maybe she can't fire while jumping, or maybe she can't do a dive attack if she's armed. To handle that, in the code for one state, you'll probably just do some crude `if` tests on the *other* machine's state to coordinate them. It's not the most elegant solution, but it gets the job done.

**CN:** 我们决定让女主角能够携带枪支。当她有武器时，她仍然可以做之前能做的一切：跑、跳、蹲等。但她还需要能够在做这些事的同时开火。

如果我们坚持使用单一的 FSM，就必须将状态数量*翻倍*。对于每个现有状态，我们需要另一个在武装状态下做同样事情的状态：站立、持枪站立、跳跃、持枪跳跃，你懂的。

再添加几种武器，状态数量就会组合爆炸。不仅是状态数量庞大，而且有大量冗余：非武装和武装状态几乎相同，只有处理开火的少量代码不同。

问题在于我们把两块状态——她在*做什么*和她*携带什么*——塞进了同一个机器。要建模所有可能的组合，我们需要为每个*配对*设置一个状态。解决方法很明显：使用两个独立的状态机。

如果我们要把*做事的 n 个状态*和*携带的 m 个状态*塞进一个机器，我们需要 *n × m* 个状态。用两个机器，只需要 *n + m*。

我们保留原来的做事状态机，不去动它。然后为她的装备定义一个独立的状态机。`Heroine` 将有*两个*"状态"引用，各司其职：

[上述 C++ 代码]

为了说明目的，我们对她的装备使用了完整的状态模式。实际上，由于它只有两个状态，一个布尔标志就足够了。

当女主角将输入委托给状态时，她会同时交给两者：

[上述 C++ 代码]

一个更完善的系统可能会让一个状态机*消费*掉输入，这样另一个就不会收到它。这可以防止两个机器错误地尝试响应同一个输入。

每个状态机随后可以独立于另一个机器来响应输入、产生行为以及改变状态。当两组状态大多不相关时，这种方法效果很好。

在实践中，你会发现有些情况下状态确实会交互。例如，她可能不能在跳跃时开火，或者如果她带了武器就不能做下落攻击。为了处理这种情况，你可能会在某个状态的代码中对*另一个*机器的状态做一些粗略的 `if` 测试来协调它们。这不是最优雅的解决方案，但能完成任务。

---

### Hierarchical State Machines / 层级状态机

* After fleshing out our heroine's behavior some more, she'll likely have a bunch of similar states. For example, she may have standing, walking, running, and sliding states. In any of those, pressing B jumps and pressing down ducks.

With a simple state machine implementation, we have to duplicate that code in each of those states. It would be better if we could implement that once and reuse it across all of the states.

If this was just object-oriented code instead of a state machine, one way to share code across those states would be using inheritance. We could define a class for an "on ground" state that handles jumping and ducking. Standing, walking, running, and sliding would then inherit from that and add their own additional behavior.

This has both good and bad implications. Inheritance is a powerful means of code reuse, but it's also a very strong coupling between two chunks of code. It's a big hammer, so swing it carefully.

It turns out, this is a common structure called a *hierarchical state machine*. A state can have a *superstate* (making itself a *substate*). When an event comes in, if the substate doesn't handle it, it rolls up the chain of superstates. In other words, it works just like overriding inherited methods.

In fact, if we're using the State pattern to implement our FSM, we can use class inheritance to implement the hierarchy. Define a base class for the superstate:

```cpp
class OnGroundState : public HeroineState
{
public:
  virtual void handleInput(Heroine& heroine, Input input)
  {
    if (input == PRESS_B)
    {
      // Jump...
    }
    else if (input == PRESS_DOWN)
    {
      // Duck...
    }
  }
};
```

And then each substate inherits it:

```cpp
class DuckingState : public OnGroundState
{
public:
  virtual void handleInput(Heroine& heroine, Input input)
  {
    if (input == RELEASE_DOWN)
    {
      // Stand up...
    }
    else
    {
      // Didn't handle input, so walk up hierarchy.
      OnGroundState::handleInput(heroine, input);
    }
  }
};
```

This isn't the only way to implement the hierarchy, of course. If you aren't using the Gang of Four's State pattern, this won't work. Instead, you can model the current state's chain of superstates explicitly using a *stack* of states instead of a single state in the main class.

The current state is the one on the top of the stack, under that is its immediate superstate, and then *that* state's superstate and so on. When you dish out some state-specific behavior, you start at the top of the stack and walk down until one of the states handles it. (If none do, you ignore it.)

**CN:** 在进一步充实女主角的行为后，她可能会有很多相似的状态。例如，她可能有站立、行走、奔跑和滑行状态。在这些状态中，按 B 键跳跃，按下键蹲伏。

使用简单的状态机实现，我们必须在每个状态中重复这段代码。如果我们能实现一次并在所有状态中复用就好了。

如果这只是面向对象代码而非状态机，跨状态共享代码的一种方式是使用继承。我们可以为"地面"状态定义一个类来处理跳跃和蹲伏。然后站立、行走、奔跑和滑行继承自该类并添加自己的额外行为。

这既有好处也有坏处。继承是代码复用的强大手段，但它也是两段代码之间的强耦合。这是一个大锤子，所以小心使用。

事实证明，这是一种常见的结构，称为*层级状态机*。一个状态可以有一个*父状态*（使它自己成为一个*子状态*）。当一个事件到来时，如果子状态没有处理它，它会沿着父状态链向上传递。换句话说，它的工作方式就像覆盖继承的方法一样。

事实上，如果我们使用状态模式来实现 FSM，我们可以使用类继承来实现层级结构。为父状态定义一个基类：

[上述 C++ 代码]

然后每个子状态继承它：

[上述 C++ 代码]

当然，这不是实现层级的唯一方式。如果你没有使用四人帮的状态模式，这就不适用。相反，你可以使用一个*栈*而不是主类中的单个状态来显式建模当前状态的父状态链。

当前状态是栈顶的那个，下面紧挨着的是它的直接父状态，然后是*那个*状态的父状态，以此类推。当需要处理某个状态特定行为时，你从栈顶开始向下遍历，直到某个状态处理它。（如果没有状态处理，就忽略它。）

---

### Pushdown Automata / 下推自动机

* There's another common extension to finite state machines that also uses a stack of states. Confusingly, the stack represents something entirely different, and is used to solve a different problem.

The problem is that finite state machines have no concept of *history*. You know what state you *are* in, but have no memory of what state you *were* in. There's no easy way to go back to a previous state.

Here's an example: Earlier, we let our fearless heroine arm herself to the teeth. When she fires her gun, we need a new state that plays the firing animation and spawns the bullet and any visual effects. So we slap together a `FiringState` and make all of the states that she can fire from transition into that when the fire button is pressed.

Since this behavior is duplicated across several states, it may also be a good place to use a hierarchical state machine to reuse that code.

The tricky part is what state she transitions to *after* firing. She can pop off a round while standing, running, jumping, and ducking. When the firing sequence is complete, she should transition back to what she was doing before.

If we're sticking with a vanilla FSM, we've already forgotten what state she was in. To keep track of it, we'd have to define a slew of nearly identical states — firing while standing, firing while running, firing while jumping, and so on — just so that each one can have a hardcoded transition that goes back to the right state when it's done.

What we'd really like is a way to *store* the state she was in before firing and then *recall* it later. Again, automata theory is here to help. The relevant data structure is called a [*pushdown automaton*](http://en.wikipedia.org/wiki/Pushdown_automaton).

Where a finite state machine has a *single* pointer to a state, a pushdown automaton has a *stack* of them. In an FSM, transitioning to a new state *replaces* the previous one. A pushdown automaton lets you do that, but it also gives you two additional operations:

1. You can *push* a new state onto the stack. The "current" state is always the one on top of the stack, so this transitions to the new state. But it leaves the previous state directly under it on the stack instead of discarding it.

2. You can *pop* the topmost state off the stack. That state is discarded, and the state under it becomes the new current state.

![The stack for a pushdown automaton. First it just contains a Standing state. A Firing state is pushed on top, then popped back off when done.](images/state-pushdown.png)

This is just what we need for firing. We create a *single* firing state. When the fire button is pressed while in any other state, we *push* the firing state onto the stack. When the firing animation is done, we *pop* that state off, and the pushdown automaton automatically transitions us right back to the state we were in before.

**CN:** 有限状态机还有另一个常见扩展，也使用了状态栈。容易混淆的是，这个栈代表完全不同的东西，用于解决不同的问题。

问题在于有限状态机没有*历史*的概念。你知道你*当前*在什么状态，但不记得你*之前*在什么状态。没有简单的方法回到之前的状态。

举个例子：之前，我们让无畏的女主角全副武装。当她开火时，我们需要一个新状态来播放开火动画、生成子弹和任何视觉效果。所以我们拼凑了一个 `FiringState`，并让所有可以开火的状态在按下开火键时转换到它。

由于这种行为在多个状态中重复，这可能也是使用层级状态机来复用代码的好地方。

棘手的部分是她开火*之后*转换到什么状态。她可以在站立、奔跑、跳跃和蹲伏时开枪。当开火序列完成时，她应该转换回之前正在做的事情。

如果我们坚持使用普通的 FSM，我们已经忘记了她之前在什么状态。要记住它，我们必须定义一堆几乎相同的状态——站立时开火、奔跑时开火、跳跃时开火等等——这样每个状态才能有一个硬编码的转换在完成后回到正确的状态。

我们真正想要的是*存储*她开火前的状态，然后稍后*恢复*它。自动机理论再次来帮忙了。相关的数据结构称为[*下推自动机*](http://en.wikipedia.org/wiki/Pushdown_automaton)。

有限状态机有一个指向状态的*单一*指针，而下推自动机有一个*栈*。在 FSM 中，转换到新状态会*替换*之前的状态。下推自动机允许你这样做，但它还提供了两个额外操作：

1. 你可以*压入*一个新状态到栈上。"当前"状态总是栈顶的那个，所以这会转换到新状态。但它将之前的状态留在栈中直接位于其下，而不是丢弃它。
2. 你可以*弹出*栈顶的状态。该状态被丢弃，下面的状态成为新的当前状态。

[图片：下推自动机的栈。最初只包含站立状态。开火状态被压入栈顶，完成后弹出。]

这正是我们开火所需要的。我们创建一个*单一的*开火状态。当在任何其他状态下按下开火键时，我们*压入*开火状态到栈上。当开火动画完成时，我们*弹出*该状态，下推自动机会自动将我们转换回之前的状态。

---

## C++ Code (原书代码)

```cpp
// ============================================================
// Phase 1: The Problem — boolean flags and bugs
// ============================================================

// Initial jump handler — no guard against air jumping
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    yVelocity_ = JUMP_VELOCITY;
    setGraphics(IMAGE_JUMP);
  }
}

// Adding isJumping_ flag to prevent air jumping
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    if (!isJumping_)
    {
      isJumping_ = true;
      // Jump...
    }
  }
}

// Adding ducking — introduces bug with mid-air stand
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    // Jump if not jumping...
  }
  else if (input == PRESS_DOWN)
  {
    if (!isJumping_)
    {
      setGraphics(IMAGE_DUCK);
    }
  }
  else if (input == RELEASE_DOWN)
  {
    setGraphics(IMAGE_STAND);
  }
}

// Adding isDucking_ flag to fix the duck bug
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    if (!isJumping_ && !isDucking_)
    {
      // Jump...
    }
  }
  else if (input == PRESS_DOWN)
  {
    if (!isJumping_)
    {
      isDucking_ = true;
      setGraphics(IMAGE_DUCK);
    }
  }
  else if (input == RELEASE_DOWN)
  {
    if (isDucking_)
    {
      isDucking_ = false;
      setGraphics(IMAGE_STAND);
    }
  }
}

// Adding dive attack — introduces bug with diving + air jump
void Heroine::handleInput(Input input)
{
  if (input == PRESS_B)
  {
    if (!isJumping_ && !isDucking_)
    {
      // Jump...
    }
  }
  else if (input == PRESS_DOWN)
  {
    if (!isJumping_)
    {
      isDucking_ = true;
      setGraphics(IMAGE_DUCK);
    }
    else
    {
      isJumping_ = false;
      setGraphics(IMAGE_DIVE);
    }
  }
  else if (input == RELEASE_DOWN)
  {
    if (isDucking_)
    {
      // Stand...
    }
  }
}

// ============================================================
// Phase 2: Simple FSM — enum + switch
// ============================================================

enum State
{
  STATE_STANDING,
  STATE_JUMPING,
  STATE_DUCKING,
  STATE_DIVING
};

void Heroine::handleInput(Input input)
{
  switch (state_)
  {
    case STATE_STANDING:
      if (input == PRESS_B)
      {
        state_ = STATE_JUMPING;
        yVelocity_ = JUMP_VELOCITY;
        setGraphics(IMAGE_JUMP);
      }
      else if (input == PRESS_DOWN)
      {
        state_ = STATE_DUCKING;
        setGraphics(IMAGE_DUCK);
      }
      break;

case STATE_JUMPING:
      if (input == PRESS_DOWN)
      {
        state_ = STATE_DIVING;
        setGraphics(IMAGE_DIVE);
      }
      break;

case STATE_DUCKING:
      if (input == RELEASE_DOWN)
      {
        state_ = STATE_STANDING;
        setGraphics(IMAGE_STAND);
      }
      break;
  }
}

// Adding chargeTime — data pollution in Heroine
void Heroine::update()
{
  if (state_ == STATE_DUCKING)
  {
    chargeTime_++;
    if (chargeTime_ > MAX_CHARGE)
    {
      superBomb();
    }
  }
}

void Heroine::handleInput(Input input)
{
  switch (state_)
  {
    case STATE_STANDING:
      if (input == PRESS_DOWN)
      {
        state_ = STATE_DUCKING;
        chargeTime_ = 0;
        setGraphics(IMAGE_DUCK);
      }
      // Handle other inputs...
      break;

// Other states...
  }
}

// ============================================================
// Phase 3: The State Pattern — interface, classes, delegation
// ============================================================

// State interface
class HeroineState
{
public:
  virtual ~HeroineState() {}
  virtual void handleInput(Heroine& heroine, Input input) {}
  virtual void update(Heroine& heroine) {}
};

// Concrete state — DuckingState with its own chargeTime_ data
class DuckingState : public HeroineState
{
public:
  DuckingState()
  : chargeTime_(0)
  {}

virtual void handleInput(Heroine& heroine, Input input)
  {
    if (input == RELEASE_DOWN)
    {
      // Change to standing state...
      heroine.setGraphics(IMAGE_STAND);
    }
  }

virtual void update(Heroine& heroine)
  {
    chargeTime_++;
    if (chargeTime_ > MAX_CHARGE)
    {
      heroine.superBomb();
    }
  }

private:
  int chargeTime_;
};

// Context class — delegates to current state
class Heroine
{
public:
  virtual void handleInput(Input input)
  {
    state_->handleInput(*this, input);
  }

virtual void update()
  {
    state_->update(*this);
  }

// Other methods...
private:
  HeroineState* state_;
};

// ============================================================
// Phase 4: State Object Management
// ============================================================

// Static states (Flyweight approach) — share stateless instances
class HeroineState
{
public:
  static StandingState standing;
  static DuckingState ducking;
  static JumpingState jumping;
  static DivingState diving;

// Other code...
};

// Transition using static state:
if (input == PRESS_B)
{
  heroine.state_ = &HeroineState::jumping;
  heroine.setGraphics(IMAGE_JUMP);
}

// Instantiated states — handleInput returns new state
void Heroine::handleInput(Input input)
{
  HeroineState* state = state_->handleInput(*this, input);
  if (state != NULL)
  {
    delete state_;
    state_ = state;
  }
}

HeroineState* StandingState::handleInput(Heroine& heroine,
                                         Input input)
{
  if (input == PRESS_DOWN)
  {
    // Other code...
    return new DuckingState();
  }

// Stay in this state.
  return NULL;
}

// ============================================================
// Phase 5: Entry and Exit Actions
// ============================================================

// Standing state with entry action
class StandingState : public HeroineState
{
public:
  virtual void enter(Heroine& heroine)
  {
    heroine.setGraphics(IMAGE_STAND);
  }

// Other code...
};

// Heroine calls enter() after transitioning
void Heroine::handleInput(Input input)
{
  HeroineState* state = state_->handleInput(*this, input);
  if (state != NULL)
  {
    delete state_;
    state_ = state;

// Call the enter action on the new state.
    state_->enter(*this);
  }
}

// Simplified DuckingState — delegates graphics to StandingState
HeroineState* DuckingState::handleInput(Heroine& heroine,
                                        Input input)
{
  if (input == RELEASE_DOWN)
  {
    return new StandingState();
  }

// Other code...
}

// ============================================================
// Phase 6: Extensions
// ============================================================

// Concurrent State Machines — separate equipment state
class Heroine
{
  // Other code...

private:
  HeroineState* state_;
  HeroineState* equipment_;
};

void Heroine::handleInput(Input input)
{
  state_->handleInput(*this, input);
  equipment_->handleInput(*this, input);
}

// Hierarchical State Machine — base class for shared behavior
class OnGroundState : public HeroineState
{
public:
  virtual void handleInput(Heroine& heroine, Input input)
  {
    if (input == PRESS_B)
    {
      // Jump...
    }
    else if (input == PRESS_DOWN)
    {
      // Duck...
    }
  }
};

// Substate inherits from OnGroundState, walks up hierarchy
class DuckingState : public OnGroundState
{
public:
  virtual void handleInput(Heroine& heroine, Input input)
  {
    if (input == RELEASE_DOWN)
    {
      // Stand up...
    }
    else
    {
      // Didn't handle input, so walk up hierarchy.
      OnGroundState::handleInput(heroine, input);
    }
  }
};
```

## C# Equivalent (C# 对照实现)

```csharp
// ============================================================
// 状态接口 — 定义所有状态必须实现的行为方法
// 使用接口（interface）而非抽象类，因为 C# 支持接口的多重实现
// ============================================================
public interface IHeroineState
{
    // 处理输入：返回新状态（null 表示保持当前状态）
    // 返回 IHeroineState? 允许状态切换时创建新实例
    IHeroineState HandleInput(Heroine heroine, Input input);

// 每帧更新：处理该状态下的持续行为（如蓄力计时）
    void Update(Heroine heroine);

// 进入状态时的动作：设置动画、重置计时器等
    void Enter(Heroine heroine);

// 退出状态时的动作：清理资源、播放音效等（可选）
    void Exit(Heroine heroine);
}

// ============================================================
// 具体状态类 — 每个状态独立封装自己的行为和数据
// ============================================================

// 站立状态 — 基础状态，可以跳跃或蹲伏
public class StandingState : IHeroineState
{
    // 静态实例 — 无状态数据的单例模式复用（享元模式）
    // 因为 StandingState 没有实例字段，所有英雄共用同一个实例
    public static readonly StandingState Instance = new StandingState();

// 私有构造，防止外部创建多个实例
    private StandingState() { }

public IHeroineState HandleInput(Heroine heroine, Input input)
    {
        switch (input)
        {
            case Input.PressB:
                // B 键跳跃：切换到跳跃状态
                // 注意：这里创建新实例因为 JumpingState 可能有自己的数据
                heroine.VelocityY = JumpVelocity;  // 设置向上的初速度
                return JumpingState.Instance;       // 跳跃状态也使用享元

case Input.PressDown:
                // 下键蹲伏：切换到蹲伏状态
                // DuckingState 有 chargeTime_ 数据，所以不能使用享元
                return new DuckingState();  // 每次进入蹲伏都创建新实例以重置计时器

default:
                return null;  // 未处理的输入，保持当前状态
        }
    }

public void Update(Heroine heroine)
    {
        // 站立状态每帧不需要特殊更新
    }

public void Enter(Heroine heroine)
    {
        // 进入站立状态时设置对应的精灵动画
        heroine.SetGraphics("stand");
    }

public void Exit(Heroine heroine) { }
}

// 跳跃状态 — 在空中时可以执行下落攻击
public class JumpingState : IHeroineState
{
    public static readonly JumpingState Instance = new JumpingState();
    private JumpingState() { }

public IHeroineState HandleInput(Heroine heroine, Input input)
    {
        if (input == Input.PressDown)
        {
            // 空中按下键 → 下落攻击（俯冲）
            heroine.VelocityY = DiveVelocity;  // 快速下落速度
            return DivingState.Instance;
        }
        return null;  // 空中按 B 键无效（防止二段跳）
    }

public void Update(Heroine heroine)
    {
        // 应用重力，更新 Y 轴位置
        heroine.VelocityY += Gravity * Time.deltaTime;
        heroine.Y += heroine.VelocityY * Time.deltaTime;

// 检测是否落地
        if (heroine.IsGrounded())
        {
            // 告知英雄切换到站立状态
            // 注意：这里不直接操作状态切换，而是通过返回值机制
            // 实际切换由 Heroine.HandleInput 的统一逻辑处理
        }
    }

public void Enter(Heroine heroine)
    {
        heroine.SetGraphics("jump");
    }

public void Exit(Heroine heroine) { }
}

// 蹲伏状态 — 有实例数据（蓄力计时器），不能使用享元模式
public class DuckingState : IHeroineState
{
    // 这个状态有自己的数据字段，所以每个英雄实例需要独立的状态实例
    private int chargeTime;  // 蓄力计时，仅蹲伏状态下有意义
    private const int MaxCharge = 60;  // 60 帧后释放超级炸弹

// 没有静态 Instance — 每次进入蹲伏都创建新实例
    // 这样 chargeTime 自然从 0 开始

public IHeroineState HandleInput(Heroine heroine, Input input)
    {
        if (input == Input.ReleaseDown)
        {
            // 松开下键 → 回到站立
            return StandingState.Instance;
        }
        return null;
    }

public void Update(Heroine heroine)
    {
        // 每帧增加蓄力值
        chargeTime++;

if (chargeTime >= MaxCharge)
        {
            // 蓄力完成，释放超级炸弹
            heroine.SuperBomb();
        }
    }

public void Enter(Heroine heroine)
    {
        // 进入蹲伏时重置蓄力计时器（已由构造函数完成）
        // 设置蹲伏动画
        heroine.SetGraphics("duck");
    }

public void Exit(Heroine heroine) { }
}

// 下落攻击状态
public class DivingState : IHeroineState
{
    public static readonly DivingState Instance = new DivingState();
    private DivingState() { }

public IHeroineState HandleInput(Heroine heroine, Input input)
    {
        return null;  // 下落攻击中不可操作
    }

public void Update(Heroine heroine)
    {
        heroine.VelocityY += Gravity * Time.deltaTime;
        heroine.Y += heroine.VelocityY * Time.deltaTime;

if (heroine.IsGrounded())
        {
            // 落地后回到站立状态
            // 这里通过 HandleInput 切换实际上不合适
            // 更好的做法：Update 返回新状态或通过回调
            // 简单起见，本书中通过另一个机制处理
        }
    }

public void Enter(Heroine heroine)
    {
        heroine.SetGraphics("dive");
    }

public void Exit(Heroine heroine) { }
}

// ============================================================
// 上下文类 — 英雄角色，将行为委托给当前状态对象
// ============================================================
public class Heroine
{
    // 当前状态对象，初始为站立状态
    // 状态模式的核心：behavior = state reference
    private IHeroineState state;

public float Y { get; set; }
    public float VelocityY { get; set; }

public Heroine()
    {
        // 初始状态：站立
        // 调用 Enter() 设置初始动画
        state = StandingState.Instance;
        state.Enter(this);
    }

// 处理输入的入口 — 委托给当前状态
    public void HandleInput(Input input)
    {
        // 当前状态处理输入，可能返回新状态
        IHeroineState newState = state.HandleInput(this, input);

// 如果需要切换状态，执行完整的切换流程
        if (newState != null && newState != state)
        {
            // 1. 退出当前状态（清理、音效等）
            state.Exit(this);

// 2. 切换到新状态
            state = newState;

// 3. 进入新状态（设置动画、重置数据等）
            state.Enter(this);
        }
    }

// 每帧更新 — 委托给当前状态
    public void Update()
    {
        state.Update(this);
    }

public bool IsGrounded()
    {
        return Y <= 0;  // 简化实现
    }

public void SetGraphics(string animationName)
    {
        // 切换精灵动画
    }

public void SuperBomb()
    {
        // 释放超级炸弹效果
    }

private const float JumpVelocity = 10f;
    private const float DiveVelocity = -20f;
    private const float Gravity = -0.5f;
}

// ============================================================
// 输入枚举
// ============================================================
public enum Input
{
    PressB,       // B 键按下
    PressDown,    // 下键按下
    ReleaseDown   // 下键释放
}

// ============================================================
// 扩展：层级状态机（Hierarchical State Machine）
// 通过继承实现状态层级共享行为
// ============================================================

// 地面状态基类 — 处理在地面上时的共同行为
public abstract class GroundState : IHeroineState
{
    // 所有地面状态共享的输入处理：跳跃和蹲伏
    public virtual IHeroineState HandleInput(Heroine heroine, Input input)
    {
        switch (input)
        {
            case Input.PressB:
                return JumpingState.Instance;   // 地面按 B 键跳跃
            case Input.PressDown:
                return new DuckingState();      // 地面按下键蹲伏
            default:
                return null;                    // 未处理 → 交给子类
        }
    }

// 子类可以 override Update 或 HandleInput 添加特殊行为
    public abstract void Update(Heroine heroine);
    public abstract void Enter(Heroine heroine);
    public abstract void Exit(Heroine heroine);
}

// 行走状态继承 GroundState → 自动获得跳跃和蹲伏能力
public class WalkingState : GroundState
{
    public static readonly WalkingState Instance = new WalkingState();
    private WalkingState() { }

// 行走有特有的输入处理（如冲刺），在基类行为之上扩展
    public override IHeroineState HandleInput(Heroine heroine, Input input)
    {
        // 先尝试处理自己的输入
        if (input == Input.PressBShift)
        {
            return new SprintingState();  // Shift → 冲刺
        }

// 未处理的输入交给基类（地面状态）处理
        // 这样行走状态自动获得跳跃和蹲伏能力
        IHeroineState baseResult = base.HandleInput(heroine, input);
        return baseResult;
    }

public override void Update(Heroine heroine)
    {
        // 行走逻辑
    }

public override void Enter(Heroine heroine)
    {
        heroine.SetGraphics("walk");
    }

public override void Exit(Heroine heroine) { }
}
```

## Unity Application / Unity 应用场景

* The State pattern is widely used in Unity for:

- **Player/Enemy AI** — Patrol, Chase, Attack, Flee states
- **Animation State Machines** — Unity's Animator Controller is essentially a visual FSM
- **UI Screen Management** — Menu, Settings, Gameplay, Pause screens
- **Game Flow** — Boot, Loading, MainMenu, Playing, GameOver states

**Unity-Specific Notes:**

- Use `MonoBehaviour`-based state machines when states need Unity lifecycle methods (`Start`, `Update`, `OnTriggerEnter`).
- Unity's `Animator` already implements a powerful FSM — often you don't need a separate State pattern for animation.
- For simple AI, consider `enum + switch` first — it's often sufficient and avoids class explosion.
- The State pattern pairs well with ScriptableObjects: you can define AI states as ScriptableObject assets, allowing designers to configure them without code.
- Be careful with `Update()` in state classes — if you have many agents, each virtual call adds overhead. Consider using a flyweight approach for stateless states.

**CN:** 状态模式在 Unity 中被广泛用于：

- **玩家/敌人 AI** — 巡逻、追逐、攻击、逃跑状态
- **动画状态机** — Unity 的 Animator Controller 本质上是可视化的 FSM
- **UI 界面管理** — 菜单、设置、游戏、暂停界面
- **游戏流程** — 启动、加载、主菜单、游戏进行、游戏结束状态

**Unity 注意事项：**

- 当状态需要 Unity 生命周期方法（`Start`、`Update`、`OnTriggerEnter`）时，使用基于 `MonoBehaviour` 的状态机
- Unity 的 `Animator` 已经实现了强大的 FSM — 通常不需要为动画单独实现状态模式
- 简单的 AI 考虑先用 `enum + switch` — 通常足够且避免了类爆炸
- 状态模式与 ScriptableObject 配合良好：将 AI 状态定义为 ScriptableObject 资源，允许策划无需代码即可配置
- 注意 `Update()` 的开销 — 如果有很多 Agent，每次虚函数调用都有开销。考虑对无状态的状态使用享元模式

### Unity 状态机实现示例 / Unity State Machine Example

```csharp
// Unity 中更实用的状态机实现 —
// 使用 MonoBehaviour + 状态对象组合，比纯 State 模式更灵活
public class EnemyAI : MonoBehaviour
{
    private IEnemyState currentState;

private void Start()
    {
        TransitionTo(new PatrolState());
    }

private void Update()
    {
        currentState?.Update(this);
    }

public void TransitionTo(IEnemyState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState?.Enter(this);
    }

public void OnTriggerEnter(Collider other)
    {
        currentState?.OnTriggerEnter(this, other);
    }
}

public interface IEnemyState
{
    void Enter(EnemyAI enemy);
    void Update(EnemyAI enemy);
    void Exit(EnemyAI enemy);
    void OnTriggerEnter(EnemyAI enemy, Collider other);
}

public class PatrolState : IEnemyState
{
    public void Enter(EnemyAI enemy)
    {
        // Start patrolling
    }

public void Update(EnemyAI enemy)
    {
        // Move along patrol route
        // Check if player is detected → transition to Chase
    }

public void Exit(EnemyAI enemy) { }

public void OnTriggerEnter(EnemyAI enemy, Collider other)
    {
        if (other.CompareTag("Player"))
        {
            enemy.TransitionTo(new ChaseState());
        }
    }
}

public class ChaseState : IEnemyState
{
    public void Enter(EnemyAI enemy) { }
    public void Update(EnemyAI enemy) { /* Chase player */ }
    public void Exit(EnemyAI enemy) { }
    public void OnTriggerEnter(EnemyAI enemy, Collider other) { }
}
```

## When to Use / 使用场景

* Even with those common extensions to state machines, they are still pretty limited. The trend these days in game AI is more toward exciting things like *behavior trees* and *planning systems*. If complex AI is what you're interested in, all this chapter has done is whet your appetite. You'll want to read other books to satisfy it.

This doesn't mean finite state machines, pushdown automata, and other simple systems aren't useful. They're a good modeling tool for certain kinds of problems. Finite state machines are useful when:

- You have an entity whose behavior changes based on some internal state.

- That state can be rigidly divided into one of a relatively small number of distinct options.

- The entity responds to a series of inputs or events over time.

In games, they are most known for being used in AI, but they are also common in implementations of user input handling, navigating menu screens, parsing text, network protocols, and other asynchronous behavior.

**CN:** 即使有了这些常见的状态机扩展，它们仍然相当有限。如今游戏 AI 的趋势更多是朝向令人兴奋的*行为树*和*规划系统*。如果你对复杂 AI 感兴趣，本章只是吊起了你的胃口。你需要阅读其他书籍来满足它。

这并不意味着有限状态机、下推自动机和其他简单系统没有用。对于某些类型的问题，它们是很好的建模工具。有限状态机在以下情况下很有用：

- 你有一个实体，其行为基于某些内部状态而变化。
- 该状态可以被严格划分为相对较少的几个不同选项之一。
- 该实体随时间响应一系列输入或事件。

在游戏中，它们最出名的是用于 AI，但也常见于用户输入处理、菜单屏幕导航、文本解析、网络协议和其他异步行为的实现中。

## Keep in Mind / 注意事项

* State machines help you untangle hairy code by enforcing a very constrained structure on it. All you've got is a fixed set of states, a single current state, and some hardcoded transitions.

A finite state machine isn't even *Turing complete*. Automata theory describes computation using a series of abstract models, each more complex than the previous. A *Turing machine* is one of the most expressive models.

"Turing complete" means a system (usually a programming language) is powerful enough to implement a Turing machine in it, which means all Turing complete languages are, in some ways, equally expressive. FSMs are not flexible enough to be in that club.

If you try using a state machine for something more complex like game AI, you will slam face-first into the limitations of that model. The trend these days in game AI is more toward exciting things like *behavior trees* and *planning systems*.

**CN:** 状态机通过强制一个非常受限的结构来帮助你理清混乱的代码。你拥有的只是一个固定的状态集合、一个当前状态和一些硬编码的转换。

有限状态机甚至不是*图灵完备*的。自动机理论用一系列抽象模型来描述计算，每个模型都比前一个更复杂。*图灵机*是其中表达能力最强的模型之一。

"图灵完备"意味着一个系统（通常是一种编程语言）足够强大，可以在其中实现图灵机。FSM 不够灵活，无法加入这个俱乐部。

如果你尝试将状态机用于更复杂的事情，比如游戏 AI，你会直接撞上该模型的限制。如今游戏 AI 的趋势更多是朝向令人兴奋的*行为树*和*规划系统*。

## Key Differences / 关键差异

| Aspect | C++ (原书) | C# (Unity) |
|--------|-----------|------------|
| **State interface** | Abstract class with virtual methods | Interface (`IHeroineState`) or abstract class |
| **Null safety** | Raw pointer, manual null checks | Nullable reference types (C# 8+) |
| **Memory for state objects** | Manual heap management | GC-managed; static states for flyweight |
| **State transitions** | Direct pointer assignment or `delete + new` | Return `IHeroineState` from `HandleInput` |
| **Entry/Exit actions** | Manual `enter()` call after transition | Explicit `Enter()` / `Exit()` in transition flow |
| **Unity lifecycle** | Not applicable | Can integrate `Start`, `Update`, `OnTriggerEnter` |
| **Editor support** | None | State data can be ScriptableObject assets |
| **Concurrent state machines** | Separate state machine pointers | Can use separate components on one GameObject |

## FSM vs State Pattern / FSM 与状态模式对比

| Aspect | Simple FSM (enum + switch) | State Pattern (classes) |
|--------|---------------------------|------------------------|
| **Complexity** | Low — one file, no classes | Higher — class per state |
| **State-specific data** | In main class (pollution) | Encapsulated in each state class |
| **Adding new state** | Modify switch everywhere | Add new class, wire transitions |
| **Code reuse** | Difficult | Inheritance (hierarchical FSM) |
| **Performance** | Fast (direct branch) | Virtual dispatch overhead |
| **When to use** | < 5 states, simple behavior | Complex behavior, per-state data |

## Extensions / 扩展

* Beyond the basic State pattern and simple FSM, the book describes three key extensions:

1. **Concurrent State Machines** — Separate orthogonal state machines (e.g., movement state + equipment state) to avoid state explosion (n × m → n + m). Each machine responds to inputs independently.

2. **Hierarchical State Machines (HSM)** — States can have superstates via inheritance; unhandled inputs bubble up the hierarchy, reducing code duplication across similar states. Implement via class inheritance (State pattern) or a stack of superstates.

3. **Pushdown Automata** — Uses a stack of states instead of a single state. Push a new state (e.g., firing) and pop back to the previous state when done — perfect for temporary states that need to restore history.

**CN:** 除了基本的状态模式和简单 FSM，本书描述了三种关键扩展：

1. **并发状态机** — 分离正交的状态机（如移动状态 + 装备状态）避免状态爆炸（n × m → n + m）。每个机器独立响应输入。

2. **层级状态机 (HSM)** — 状态可以通过继承拥有父状态；未处理的输入沿层级向上冒泡，减少相似状态间的代码重复。通过类继承（状态模式）或父状态栈实现。

3. **下推自动机** — 使用状态栈而非单一状态。压入新状态（如射击）并在完成时弹出回到之前的状态——非常适合需要恢复历史的临时状态。

## See Also / 扩展阅读

- **Strategy Pattern** — Structurally similar to State, but with different intent: Strategy decouples behavior, while State changes behavior by changing its delegated object.
- **Type Object Pattern** — Another pattern where a main object delegates to a subordinate; here, multiple objects share a reference to the same type object.
- **Flyweight Pattern** — Used to share static state instances across multiple FSMs when the state has no instance-specific data.
- **Object Pool Pattern** — Helps manage fragmentation when dynamically allocating state objects.
- **Update Method Pattern** — Referenced in the chapter for per-frame state updates like charge time.
- **Behavior Trees** — A more powerful AI architecture that goes beyond FSMs, trending in modern game AI.
- **Planning Systems** — Goal-Oriented Action Planning (GOAP) and other planning-based AI systems.
---

## Intent / 意图

* Cause a series of sequential operations to appear instantaneous or simultaneous.

* 使一系列顺序操作看起来像是瞬间完成或同时发生。

---

## Motivation / 动机

* In their hearts, computers are sequential beasts. Their power comes from being able to break down the largest tasks into tiny steps that can be performed one after another. Often, though, our users need to see things occur in a single instantaneous step or see multiple tasks performed simultaneously.

* 在本质上，计算机是顺序执行的野兽。它们的力量来自于能将最庞大的任务分解成一个个可以依次执行的小步骤。然而，用户常常需要看到事情在单一的瞬间步骤中发生，或看到多个任务同时执行。

* With threading and multi-core architectures this is becoming less true, but even with several cores, only a few operations are running concurrently.

* 随着线程和多核架构的发展，这种情况正在改变，但即使有多个核心，同时运行的也只有少数操作。

* A typical example, and one that every game engine must address, is rendering. When the game draws the world the users see, it does so one piece at a time — the mountains in the distance, the rolling hills, the trees, each in its turn. If the user *watched* the view draw incrementally like that, the illusion of a coherent world would be shattered. The scene must update smoothly and quickly, displaying a series of complete frames, each appearing instantly.

* 一个典型的例子——也是每个游戏引擎都必须面对的问题——就是渲染。当游戏绘制用户所看到的世界时，它是一次绘制一个部分的：远处的山峦、绵延的丘陵、树木，依次进行。如果用户*看到*视图像这样逐步绘制，一个连贯世界的幻觉就会被打破。场景必须平滑快速地更新，显示一系列完整的帧，每一帧都瞬间出现。

* Double buffering solves this problem, but to understand how, we first need to review how a computer displays graphics.

* 双缓冲解决了这个问题，但要理解它是如何解决的，我们首先需要回顾计算机是如何显示图形的。

### How computer graphics work (briefly) / 计算机图形学工作原理（简述）

* A video display like a computer monitor draws one pixel at a time. It sweeps across each row of pixels from left to right and then moves down to the next row. When it reaches the bottom right corner, it scans back up to the top left and starts all over again. It does this so fast — around sixty times a second — that our eyes can't see the scanning. To us, it's a single static field of colored pixels — an image.

* 像计算机显示器这样的视频显示设备，一次绘制一个像素。它从左到右扫描每一行像素，然后移动到下一行。当它到达右下角时，它会扫描回左上角并重新开始。它做得如此之快——大约每秒六十次——以至于我们的眼睛看不到扫描过程。对我们来说，它是一个单一的静态彩色像素场——一幅图像。

* This explanation is, err, "simplified". If you're a low-level hardware person and you're cringing right now, feel free to skip to the next section. You already know enough to understand the rest of the chapter. If you *aren't* that person, my goal here is to give you just enough context to understand the pattern we'll discuss later.

* 这个解释嘛，呃，是"简化版"的。如果你是底层硬件专家，现在正在皱眉，请随意跳到下一节。你已经知道得足够多了。如果你*不是*那种人，我的目标只是给你足够的背景知识来理解我们后面要讨论的模式。

* You can think of this process like a tiny hose that pipes pixels to the display. Individual colors go into the back of the hose, and it sprays them out across the display, one bit of color to each pixel in its turn. So how does the hose know what colors go where?

* 你可以把这个过程想象成一根将像素输送到显示器的微小软管。单个颜色进入软管的后端，然后它将这些颜色喷到显示器上，每次一个颜色位到每个像素。那么软管是如何知道什么颜色该去哪里呢？

* In most computers, the answer is that it pulls them from a *framebuffer*. A framebuffer is an array of pixels in memory, a chunk of RAM where each couple of bytes represents the color of a single pixel. As the hose sprays across the display, it reads in the color values from this array, one byte at a time.

* 在大多数计算机中，答案是从*帧缓冲区（framebuffer）*中获取。帧缓冲区是内存中的一个像素数组，是一块 RAM，其中每几个字节代表单个像素的颜色。当软管在显示器上喷射时，它从这个数组中读取颜色值，一次一个字节。

* The specific mapping between byte values and colors is described by the *pixel format* and the *color depth* of the system. In most gaming consoles today, each pixel gets 32 bits: eight each for the red, green, and blue channels, and another eight left over for various other purposes.

* 字节值和颜色之间的具体映射由系统的*像素格式（pixel format）*和*颜色深度（color depth）*描述。在今天的多数游戏主机中，每个像素占 32 位：红色、绿色、蓝色通道各 8 位，还有 8 位留给各种其他用途。

* Ultimately, in order to get our game to appear on screen, all we do is write to that array. All of the crazy advanced graphics algorithms we have boil down to just that: setting byte values in the framebuffer. But there's a little problem.

* 最终，为了让我们的游戏显示在屏幕上，我们所做的一切就是写入那个数组。我们所有疯狂的高级图形算法都归结为这一点：设置帧缓冲区中的字节值。但有一个小问题。

* Earlier, I said computers are sequential. If the machine is executing a chunk of our rendering code, we don't expect it to be doing anything else at the same time. That's mostly accurate, but a couple of things *do* happen in the middle of our program running. One of those is that the video display will be reading from the framebuffer *constantly* while our game runs. This can cause a problem for us.

* 之前，我说计算机是顺序执行的。如果机器正在执行我们的一段渲染代码，我们不会期望它同时做其他事情。这基本正确，但有几件事*确实*会在程序运行过程中发生。其中之一就是视频显示器在游戏运行时*不断地*从帧缓冲区读取数据。这可能会给我们带来问题。

* Let's say we want a happy face to appear on screen. Our program starts looping through the framebuffer, coloring pixels. What we don't realize is that the video driver is pulling from the framebuffer right as we're writing to it. As it scans across the pixels we've written, our face starts to appear, but then it outpaces us and moves into pixels we haven't written yet. The result is *tearing*, a hideous visual bug where you see half of something drawn on screen.

* 假设我们想让一张笑脸显示在屏幕上。我们的程序开始遍历帧缓冲区，为像素着色。我们没有意识到的是，视频驱动正好在我们写入时从帧缓冲区读取数据。当它扫描我们已经写入的像素时，我们的脸开始出现，但随后它超过了我们，进入我们尚未写入的像素。结果是*画面撕裂（tearing）*，一种可怕的视觉错误，你会在屏幕上看到半成品。

* [Image: A series of images of an in-progress frame being rendered. A pointer writes pixels while another reads them. The reader outpaces the writer until it starts reading pixels that haven't been rendered yet.]

* [图示：一系列正在渲染中的帧的图像。一个指针写入像素，而另一个指针读取它们。读取者超过了写入者，直到它开始读取尚未渲染的像素。]

* We start drawing pixels just as the video driver starts reading from the framebuffer (Fig. 1). The video driver eventually catches up to the renderer and then races past it to pixels we haven't written yet (Fig. 2). We finish drawing (Fig. 3), but the driver doesn't catch those new pixels.

* 我们开始绘制像素的同时，视频驱动也开始从帧缓冲区读取（图 1）。视频驱动最终赶上了渲染器，然后超过它，到达我们尚未写入的像素（图 2）。我们完成绘制（图 3），但驱动没有捕获到那些新像素。

* The result (Fig. 4) is that the user sees half of the drawing. The name "tearing" comes from the fact that it looks like the bottom half was torn off.

* 结果（图 4）是用户看到了一半的绘制。"撕裂"这个名字来自于它看起来像是下半部分被撕掉了。

* This is why we need this pattern. Our program renders the pixels one at a time, but we need the display driver to see them all at once — in one frame the face isn't there, and in the next one it is. Double buffering solves this. I'll explain how by analogy.

* 这就是我们需要这个模式的原因。我们的程序一次一个像素地渲染，但我们需要显示驱动一次性看到它们——在这一帧中笑脸不在，在下一帧中它在了。双缓冲解决了这个问题。我将通过类比来解释。

### Act 1, Scene 1 / 第一幕，第一场

* Imagine our users are watching a play produced by ourselves. As scene one ends and scene two starts, we need to change the stage setting. If we have the stagehands run on after the scene and start dragging props around, the illusion of a coherent place will be broken. We could dim the lights while we do that (which, of course, is what real theaters do), but the audience still knows *something* is going on. We want there to be no gap in time between scenes.

* 想象我们的用户正在观看我们自己制作的一出戏剧。当第一幕结束、第二幕开始时，我们需要更换舞台布景。如果我们让舞台工作人员在场景结束后跑上台开始拖动道具，连贯场景的幻觉就会被打破。我们可以在此期间调暗灯光（当然，真正的剧院正是这样做的），但观众仍然知道*有些事*正在发生。我们希望场景之间没有时间间隙。

* With a bit of real estate, we come up with this clever solution: we build *two* stages set up so the audience can see both. Each has its own set of lights. We'll call them stage A and stage B. Scene one is shown on stage A. Meanwhile, stage B is dark as the stagehands are setting up scene two. As soon as scene one ends, we cut the lights on stage A and bring them up on stage B. The audience looks to the new stage and scene two begins immediately.

* 利用一点场地空间，我们想出了一个巧妙的解决方案：我们搭建*两个*舞台，让观众都能看到。每个舞台都有自己的灯光。我们称它们为舞台 A 和舞台 B。第一幕在舞台 A 上演出。与此同时，舞台 B 是暗的，舞台工作人员正在布置第二幕。第一幕一结束，我们就切断舞台 A 的灯光，点亮舞台 B。观众看向新舞台，第二幕立即开始。

* At the same time, our stagehands are over on the now darkened stage *A*, striking scene one and setting up scene *three*. As soon as scene two ends, we switch the lights back to stage A again. We continue this process for the entire play, using the darkened stage as a work area where we can set up the next scene. Every scene transition, we just toggle the lights between the two stages. Our audience gets a continuous performance with no delay between scenes. They never see a stagehand.

* 与此同时，我们的舞台工作人员在现在已经变暗的舞台*A*上，拆除第一幕并布置第*三*幕。第二幕一结束，我们再次将灯光切回舞台 A。我们在整出剧中持续这个过程，使用暗下来的舞台作为工作区来布置下一场戏。每次场景转换，我们只需在两个舞台之间切换灯光。我们的观众获得了一个连续的表演，场景之间没有延迟。他们从未看到舞台工作人员。

* Using a half-silvered mirror and some very smart layout, you could actually build this so that the two stages would appear to the audience in the same *place*. As soon as the lights switch, they would be looking at a different stage, but they would never have to change where they look. Building this is left as an exercise for the reader.

* 使用半镀银镜和一些非常巧妙的布局，你实际上可以这样搭建，让两个舞台在观众看来出现在同一个*位置*。一旦灯光切换，他们就会看到不同的舞台，但他们永远不需要改变他们看的地方。如何搭建这个留给读者作为练习。

### Back to the graphics / 回到图形学

* That is exactly how double buffering works, and this process underlies the rendering system of just about every game you've ever seen. Instead of a single framebuffer, we have *two*. One of them represents the current frame, stage A in our analogy. It's the one the video hardware is reading from. The GPU can scan through it as much as it wants whenever it wants.

* 这正是双缓冲的工作原理，这个流程几乎是你见过的每个游戏的渲染系统的基础。我们不是只有一个帧缓冲区，而是有*两个*。其中一个代表当前帧，类比中的舞台 A。它是视频硬件正在读取的那个。GPU 可以随时随意地扫描它。

* Not *all* games and consoles do this, though. Older and simpler consoles where memory is limited carefully sync their drawing to the video refresh instead. It's tricky.

* 不过并非*所有*游戏和主机都这样做。在内存有限的较旧和较简单的主机上，它们会仔细地将绘制与视频刷新同步。这很棘手。

* Meanwhile, our rendering code is writing to the *other* framebuffer. This is our darkened stage B. When our rendering code is done drawing the scene, it switches the lights by *swapping* the buffers. This tells the video hardware to start reading from the second buffer now instead of the first one. As long as it times that switch at the end of a refresh, we won't get any tearing, and the entire scene will appear all at once.

* 与此同时，我们的渲染代码正在写入*另一个*帧缓冲区。这是我们暗下来的舞台 B。当我们的渲染代码完成场景绘制后，它通过*交换*缓冲区来切换灯光。这告诉视频硬件现在开始从第二个缓冲区读取，而不是第一个。只要在刷新结束时恰当地切换时机，我们就不会得到任何撕裂，整个场景将一次性出现。

* Meanwhile, the old framebuffer is now available for use. We start rendering the next frame onto it. Voilà!

* 同时，旧的帧缓冲区现在可供使用了。我们开始在它上面渲染下一帧。瞧！

---

## The Pattern / 模式定义

* A **buffered class** encapsulates a **buffer**: a piece of state that can be modified. This buffer is edited incrementally, but we want all outside code to see the edit as a single atomic change. To do this, the class keeps *two* instances of the buffer: a **next buffer** and a **current buffer**.

* **缓冲类（buffered class）**封装了一个**缓冲区（buffer）**：一段可以被修改的状态。这个缓冲区被逐步编辑，但我们希望所有外部代码将这次编辑视为一个单一的原子变更。为此，该类保存了缓冲区的*两个*实例：一个**下一帧缓冲区（next buffer）**和一个**当前缓冲区（current buffer）**。

* When information is read *from* a buffer, it is always from the *current* buffer. When information is written *to* a buffer, it occurs on the *next* buffer. When the changes are complete, a **swap** operation swaps the next and current buffers instantly so that the new buffer is now publicly visible. The old current buffer is now available to be reused as the new next buffer.

* 当从缓冲区*读取*信息时，总是从*当前*缓冲区读取。当向缓冲区*写入*信息时，操作发生在*下一帧*缓冲区上。当变更完成后，一个**交换（swap）**操作会瞬间交换下一帧和当前缓冲区，使得新缓冲区现在公开可见。旧的当前缓冲区现在可以被复用为新的下一帧缓冲区。

---

## When to Use It / 使用时机

* This pattern is one of those ones where you'll know when you need it. If you have a system that lacks double buffering, it will probably look visibly wrong (tearing, etc.) or will behave incorrectly. But saying, "you'll know when you need it" doesn't give you much to go on. More specifically, this pattern is appropriate when all of these are true:

* 这个模式是那种你需要它时你就会知道的模式之一。如果你的系统缺少双缓冲，它很可能看起来明显错误（撕裂等）或行为不正确。但说"你需要它时你就会知道"并不能给你太多参考。更具体地说，当以下所有条件都成立时，这个模式是合适的：

- **EN:** We have some state that is being modified incrementally.
- **CN:** 我们有一些正在被逐步修改的状态。

- **EN:** That same state may be accessed in the middle of modification.
- **CN:** 同样的状态可能在修改过程中被访问。

- **EN:** We want to prevent the code that's accessing the state from seeing the work in progress.
- **CN:** 我们希望防止访问该状态的代码看到正在进行的中间工作。

- **EN:** We want to be able to read the state and we don't want to have to wait while it's being written.
- **CN:** 我们希望能够读取状态，并且不希望不得不在写入期间等待。

---

## Keep in Mind / 注意事项

* Unlike larger architectural patterns, double buffering exists at a lower implementation level. Because of this, it has fewer consequences for the rest of the codebase — most of the game won't even be aware of the difference. There are a couple of caveats, though.

* 与更大的架构模式不同，双缓冲存在于较低的实现层面。正因如此，它对代码库其余部分的影响较小——游戏的大部分内容甚至不会察觉到差异。不过有几个注意事项。

### The swap itself takes time / 交换本身需要时间

* Double-buffering requires a *swap* step once the state is done being modified. That operation must be atomic — no code can access *either* state while they are being swapped. Often, this is as quick as assigning a pointer, but if it takes longer to swap than it does to modify the state to begin with, then we haven't helped ourselves at all.

* 一旦状态修改完成，双缓冲需要一个*交换*步骤。该操作必须是原子性的——在交换期间，没有代码可以访问*任何一个*状态。通常，这就像赋值一个指针一样快，但如果交换所需的时间比修改状态本身还要长，那么我们根本没有帮到自己。

### We have to have two buffers / 我们必须有两个缓冲区

* The other consequence of this pattern is increased memory usage. As its name implies, the pattern requires you to keep *two* copies of your state in memory at all times. On memory-constrained devices, this can be a heavy price to pay. If you can't afford two buffers, you may have to look into other ways to ensure your state isn't being accessed during modification.

* 这个模式的另一个后果是增加内存使用量。顾名思义，该模式要求你随时在内存中保存状态的*两个*副本。在内存受限的设备上，这可能需要付出沉重的代价。如果你负担不起两个缓冲区，你可能需要研究其他方式来确保你的状态在修改期间不会被访问。

---

## Sample Code / 示例代码

* Now that we've got the theory, let's see how it works in practice. We'll write a very bare-bones graphics system that lets us draw pixels on a framebuffer. In most consoles and PCs, the video driver provides this low-level part of the graphics system, but implementing it by hand here will let us see what's going on. First up is the buffer itself:

* 现在我们已经有了理论，让我们看看它在实践中是如何工作的。我们将编写一个非常简约的图形系统，让我们可以在帧缓冲区上绘制像素。在大多数主机和 PC 上，视频驱动提供了图形系统的这个底层部分，但在这里手动实现它将让我们看到发生了什么。首先是缓冲区本身：

```cpp
class Framebuffer {
public:
  Framebuffer() { clear(); }

  void clear()
  {
    for (int i = 0; i < WIDTH * HEIGHT; i++)
    {
      pixels_[i] = WHITE;
    }
  }

  void draw(int x, int y)
  {
    pixels_[(WIDTH * y) + x] = BLACK;
  }

  const char* getPixels()
  {
    return pixels_;
  }

private:
  static const int WIDTH = 160;
  static const int HEIGHT = 120;

  char pixels_[WIDTH * HEIGHT];
};
```

* It has basic operations for clearing the entire buffer to a default color and setting the color of an individual pixel. It also has a function, `getPixels()`, to expose the raw array of memory holding the pixel data. We won't see this in the example, but the video driver will call that function frequently to stream memory from the buffer onto the screen.

* 它具有将整个缓冲区清除为默认颜色和设置单个像素颜色的基本操作。它还有一个函数 `getPixels()`，用于暴露保存像素数据的原始内存数组。我们不会在示例中看到这一点，但视频驱动会频繁调用该函数，将内存从 buffer 流传到屏幕上。

* We wrap this raw buffer in a `Scene` class. It's job here is to render something by making a bunch of `draw()` calls on its buffer:

* 我们将这个原始缓冲区包装在一个 `Scene` 类中。它的任务是通过在其缓冲区上进行一系列 `draw()` 调用来渲染某些内容：

```cpp
class Scene {
public:
  void draw()
  {
    buffer_.clear();

    buffer_.draw(1, 1);
    buffer_.draw(4, 1);
    buffer_.draw(1, 3);
    buffer_.draw(2, 4);
    buffer_.draw(3, 4);
    buffer_.draw(4, 3);
  }

  Framebuffer& getBuffer() { return buffer_; }

private:
  Framebuffer buffer_;
};
```

* Specifically, it draws this artistic masterpiece:

* 具体来说，它绘制了这幅艺术杰作：

* [Image: A pixellated smiley face.]

* [图示：一个像素化的笑脸。]

* Every frame, the game tells the scene to draw. The scene clears the buffer and then draws a bunch of pixels, one at a time. It also provides access to the internal buffer through `getBuffer()` so that the video driver can get to it.

* 每一帧，游戏告诉场景进行绘制。场景清除缓冲区，然后一次绘制一堆像素。它还通过 `getBuffer()` 提供对内部缓冲区的访问，以便视频驱动能够获取它。

* This seems pretty straightforward, but if we leave it like this, we'll run into problems. The trouble is that the video driver can call `getPixels()` on the buffer at *any* point in time, even here:

* 这看起来相当直接，但如果我们这样放着，就会遇到问题。麻烦在于，视频驱动可以在*任何*时间点调用缓冲区上的 `getPixels()`，甚至在这里：

```cpp
buffer_.draw(1, 1);
buffer_.draw(4, 1);
// <- Video driver reads pixels here!
buffer_.draw(1, 3);
buffer_.draw(2, 4);
buffer_.draw(3, 4);
buffer_.draw(4, 3);
```

* When that happens, the user will see the eyes of the face, but the mouth will disappear for a single frame. In the next frame, it could get interrupted at some other point. The end result is horribly flickering graphics. We'll fix this with double buffering:

* 当发生这种情况时，用户会看到脸的眼睛，但嘴巴会在单帧中消失。在下一帧中，它可能在另一个点被中断。最终结果是可怕地闪烁的图形。我们将用双缓冲来解决这个问题：

```cpp
class Scene {
public:
  Scene()
  : current_(&buffers_[0]),
    next_(&buffers_[1])
  {}

  void draw()
  {
    next_->clear();

    next_->draw(1, 1);
    // ...
    next_->draw(4, 3);

    swap();
  }

  Framebuffer& getBuffer() { return *current_; }

private:
  void swap()
  {
    // Just switch the pointers.
    Framebuffer* temp = current_;
    current_ = next_;
    next_ = temp;
  }

  Framebuffer  buffers_[2];
  Framebuffer* current_;
  Framebuffer* next_;
};
```

* Now `Scene` has two buffers, stored in the `buffers_` array. We don't directly reference them from the array. Instead, there are two members, `next_` and `current_`, that point into the array. When we draw, we draw onto the next buffer, referenced by `next_`. When the video driver needs to get the pixels, it always accesses the *other* buffer through `current_`.

* 现在 `Scene` 有两个缓冲区，存储在 `buffers_` 数组中。我们不直接从数组中引用它们。而是有两个成员 `next_` 和 `current_`，指向数组。当我们绘制时，我们绘制到由 `next_` 引用的下一帧缓冲区上。当视频驱动需要获取像素时，它总是通过 `current_` 访问*另一个*缓冲区。

* This way, the video driver never sees the buffer that we're working on. The only remaining piece of the puzzle is the call to `swap()` when the scene is done drawing the frame. That swaps the two buffers by simply switching the `next_` and `current_` references. The next time the video driver calls `getBuffer()`, it will get the new buffer we just finished drawing and put our recently drawn buffer on screen. No more tearing or unsightly glitches.

* 这样，视频驱动永远不会看到我们正在处理的缓冲区。拼图中剩下的唯一一块是在场景绘制完成帧时对 `swap()` 的调用。它通过简单地切换 `next_` 和 `current_` 引用来交换两个缓冲区。下次视频驱动调用 `getBuffer()` 时，它将获取我们刚刚完成绘制的新缓冲区，并将我们最近绘制的缓冲区显示在屏幕上。不再有撕裂或难看的故障。

### Not just for graphics / 不仅仅是图形

* The core problem that double buffering solves is state being accessed while it's being modified. There are two common causes of this. We've covered the first one with our graphics example — the state is directly accessed from code on another thread or interrupt.

* 双缓冲解决的核心问题是在状态被修改的同时被访问。这有两个常见原因。我们已经通过图形示例覆盖了第一个——状态被另一个线程或中断上的代码直接访问。

* There is another equally common cause, though: when the code *doing the modification* is accessing the same state that it's modifying. This can manifest in a variety of places, especially physics and AI where you have entities interacting with each other. Double-buffering is often helpful here too.

* 不过还有另一个同样常见的原因：当*执行修改的*代码正在访问它正在修改的相同状态时。这可能在各种地方出现，特别是在物理和 AI 中，其中实体之间相互交互。双缓冲在这里也经常有帮助。

### Artificial unintelligence / 人工"非"智能

* Let's say we're building the behavioral system for, of all things, a game based on slapstick comedy. The game has a stage containing a bunch of actors that run around and get up to various hijinks and shenanigans. Here's our base actor:

* 假设我们在为一个基于滑稽喜剧的游戏构建行为系统。游戏有一个舞台，包含一群跑来跑去、搞各种恶作剧的演员。这是我们的基础演员：

```cpp
class Actor {
public:
  Actor() : slapped_(false) {}

  virtual ~Actor() {}
  virtual void update() = 0;

  void reset()      { slapped_ = false; }
  void slap()       { slapped_ = true; }
  bool wasSlapped() { return slapped_; }

private:
  bool slapped_;
};
```

* Every frame, the game is responsible for calling `update()` on the actor so that it has a chance to do some processing. Critically, from the user's perspective, *all actors should appear to update simultaneously*.

* 每一帧，游戏负责在演员上调用 `update()`，以便它有机会进行一些处理。关键的是，从用户的角度来看，*所有演员应该看起来同时更新*。

* This is an example of the [Update Method](update-method.html) pattern.

* 这是[更新方法（Update Method）](update-method.html)模式的一个例子。

* Actors can also interact with each other, if by "interacting", we mean "they can slap each other around". When updating, the actor can call `slap()` on another actor to slap it and call `wasSlapped()` to determine if it has been slapped.

* 演员之间也可以互动——如果"互动"意味着"他们可以互相扇耳光"的话。在更新时，演员可以在另一个演员上调用 `slap()` 来扇它，并调用 `wasSlapped()` 来确定它是否被扇了。

* The actors need a stage where they can interact, so let's build that:

* 演员需要一个可以互动的舞台，所以让我们来构建它：

```cpp
class Stage {
public:
  void add(Actor* actor, int index)
  {
    actors_[index] = actor;
  }

  void update()
  {
    for (int i = 0; i < NUM_ACTORS; i++)
    {
      actors_[i]->update();
      actors_[i]->reset();
    }
  }

private:
  static const int NUM_ACTORS = 3;

  Actor* actors_[NUM_ACTORS];
};
```

* `Stage` lets us add actors, and provides a single `update()` call that updates each actor. To the user, actors appear to move simultaneously, but internally, they are updated one at a time.

* `Stage` 让我们添加演员，并提供一个单一的 `update()` 调用来更新每个演员。对用户来说，演员看起来是同时移动的，但在内部，它们是一个一个被更新的。

* The only other point to note is that each actor's "slapped" state is cleared immediately after updating. This is so that an actor only responds to a given slap once.

* 唯一需要指出的另一点是，每个演员的"被扇"状态在更新后立即被清除。这是为了让一个演员只对给定的一个巴掌响应一次。

* To get things going, let's define a concrete actor subclass. Our comedian here is pretty simple. He faces a single actor. Whenever he gets slapped — by anyone — he responds by slapping the actor he faces.

* 为了让事情进行下去，让我们定义一个具体的演员子类。我们这里的喜剧演员非常简单。他面对一个演员。每当他被任何人扇了，他就会通过扇他面对的演员来回应。

```cpp
class Comedian : public Actor {
public:
  void face(Actor* actor) { facing_ = actor; }

  virtual void update()
  {
    if (wasSlapped()) facing_->slap();
  }

private:
  Actor* facing_;
};
```

* Now, let's throw some comedians on a stage and see what happens. We'll set up three comedians, each facing the next. The last one will face the first, in a big circle:

* 现在，让我们把一些喜剧演员扔到舞台上，看看会发生什么。我们将设置三个喜剧演员，每个面对下一个。最后一个将面对第一个，形成一个圆圈：

```cpp
Stage stage;

Comedian* harry = new Comedian();
Comedian* baldy = new Comedian();
Comedian* chump = new Comedian();

harry->face(baldy);
baldy->face(chump);
chump->face(harry);

stage.add(harry, 0);
stage.add(baldy, 1);
stage.add(chump, 2);
```

* The resulting stage is set up as shown in the following image. The arrows show who the actors are facing, and the numbers show their index in the stage's array.

* 结果舞台的设置如下图所示。箭头显示演员面对谁，数字显示他们在舞台数组中的索引。

* [Image: Boxes for Harry, Baldy, and Chump, in that order. Harry has an arrow pointing to Baldy, who has an arrow pointing to Chump, who has an arrow pointing back to Harry.]

* [图示：依次为 Harry、Baldy 和 Chump 的方框。Harry 有一个箭头指向 Baldy，Baldy 有一个箭头指向 Chump，Chump 有一个箭头指回 Harry。]

* We'll slap Harry to get things going and see what happens when we start processing:

* 我们将扇 Harry 来让事情开始，看看当我们开始处理时会发生什么：

```cpp
harry->slap();

stage.update();
```

* Remember that the `update()` function in `Stage` updates each actor in turn, so if we step through the code, we'll find that the following occurs:

* 记住 `Stage` 中的 `update()` 函数依次更新每个演员，所以如果我们逐步执行代码，我们会发现以下情况发生：

```cpp
Stage updates actor 0 (Harry)
  Harry was slapped, so he slaps Baldy
Stage updates actor 1 (Baldy)
  Baldy was slapped, so he slaps Chump
Stage updates actor 2 (Chump)
  Chump was slapped, so he slaps Harry
Stage update ends
```

* In a single frame, our initial slap on Harry has propagated through all of the comedians. Now, to mix things up a bit, let's say we reorder the comedians within the stage's array but leave them facing each other the same way.

* 在单帧中，我们对 Harry 的初始一扇已经传播到了所有喜剧演员。现在，让我们稍微改变一下，假设我们重新排序舞台数组中的喜剧演员，但让他们面对彼此的方式保持不变。

* [Image: The same boxes as before with the same arrows, but now they are ordered Chump, Baldy, Harry.]

* [图示：与前相同的方框和箭头，但现在它们的顺序是 Chump、Baldy、Harry。]

* We'll leave the rest of the stage setup alone, but we'll replace the chunk of code where we add the actors to the stage with this:

* 我们将保持舞台设置的其余部分不变，但我们将替换将演员添加到舞台的那部分代码：

```cpp
stage.add(harry, 2);
stage.add(baldy, 1);
stage.add(chump, 0);
```

* Let's see what happens when we run our experiment again:

* 让我们看看再次运行实验时会发生什么：

```cpp
Stage updates actor 0 (Chump)
  Chump was not slapped, so he does nothing
Stage updates actor 1 (Baldy)
  Baldy was not slapped, so he does nothing
Stage updates actor 2 (Harry)
  Harry was slapped, so he slaps Baldy
Stage update ends
```

* Uh, oh. Totally different. The problem is straightforward. When we update the actors, we modify their "slapped" states, the exact same state we also *read* during the update. Because of this, changes to that state early in the update affect later parts of that *same* update step.

* 啊哦。完全不同的结果。问题很直接。当我们更新演员时，我们修改了他们的"被扇"状态，而这正是我们在更新期间*读取*的相同状态。正因如此，更新早期对该状态的更改会影响*同一个*更新步骤的后续部分。

* If you continue to update the stage, you'll see the slaps gradually cascade through the actors, one per frame. In the first frame, Harry slaps Baldy. In the next frame, Baldy slaps Chump, and so on.

* 如果你继续更新舞台，你会看到巴掌逐渐在演员中层叠传递，每帧一个。在第一帧中，Harry 扇 Baldy。在下一帧中，Baldy 扇 Chump，依此类推。

* The ultimate result is that an actor may respond to being slapped in either the *same* frame as the slap or in the *next* frame based entirely on how the two actors happen to be ordered on the stage. This violates our requirement that actors need to appear to run in parallel — the order that they update within a single frame shouldn't matter.

* 最终结果是，一个演员响应被扇的时机可能与扇发生在*同一*帧，也可能在*下一*帧，这完全取决于两个演员在舞台上的顺序。这违反了我们的要求：演员需要看起来是并行运行的——它们在单帧内的更新顺序应该无关紧要。

### Buffered slaps / 缓冲的巴掌

* Fortunately, our Double Buffer pattern can help. This time, instead of having two copies of a monolithic "buffer" object, we'll be buffering at a much finer granularity: each actor's "slapped" state:

* 幸运的是，我们的双缓冲模式可以帮上忙。这次，我们不是拥有一个整体式"缓冲区"对象的两个副本，而是在更细的粒度上进行缓冲：每个演员的"被扇"状态：

```cpp
class Actor {
public:
  Actor() : currentSlapped_(false) {}

  virtual ~Actor() {}
  virtual void update() = 0;

  void swap()
  {
    // Swap the buffer.
    currentSlapped_ = nextSlapped_;

    // Clear the new "next" buffer.
    nextSlapped_ = false;
  }

  void slap()       { nextSlapped_ = true; }
  bool wasSlapped() { return currentSlapped_; }

private:
  bool currentSlapped_;
  bool nextSlapped_;
};
```

* Instead of a single `slapped_` state, each actor now has two. Just like the previous graphics example, the current state is used for reading, and the next state is used for writing.

* 每个演员现在有两个状态，而不是一个 `slapped_` 状态。就像之前的图形示例一样，当前状态用于读取，下一帧状态用于写入。

* The `reset()` function has been replaced with `swap()`. Now, right before clearing the swap state, it copies the next state into the current one, making it the new current state. This also requires a small change in `Stage`:

* `reset()` 函数已被替换为 `swap()`。现在，在清除交换状态之前，它将下一帧状态复制到当前状态中，使其成为新的当前状态。这也需要对 `Stage` 进行一个小改动：

```cpp
void Stage::update()
{
  for (int i = 0; i < NUM_ACTORS; i++)
  {
    actors_[i]->update();
  }

  for (int i = 0; i < NUM_ACTORS; i++)
  {
    actors_[i]->swap();
  }
}
```

* The `update()` function now updates all of the actors and *then* swaps all of their states. The end result of this is that an actor will only see a slap in the frame *after* it was actually slapped. This way, the actors will behave the same no matter their order in the stage's array. As far as the user or any outside code can tell, all of the actors update simultaneously within a frame.

* `update()` 函数现在更新所有演员，*然后*交换它们的所有状态。最终结果是，演员只有在被扇后的*下一*帧才会看到那一巴掌。这样，无论演员在舞台数组中的顺序如何，它们的行为都是一样的。就用户或任何外部代码而言，所有演员在单帧内同时更新。

---

## Design Decisions / 设计决策

* Double Buffer is pretty straightforward, and the examples we've seen so far cover most of the variations you're likely to encounter. There are two main decisions that come up when implementing this pattern.

* 双缓冲相当直接，我们目前看到的例子涵盖了你可能遇到的大多数变体。实现此模式时有两个主要决策。

### How are the buffers swapped? / 如何交换缓冲区？

* The swap operation is the most critical step of the process since we must lock out all reading and modification of both buffers while it's occurring. To get the best performance, we want this to happen as quickly as possible.

* 交换操作是过程中最关键的步骤，因为在交换期间我们必须锁定对两个缓冲区的所有读取和修改。为了获得最佳性能，我们希望这尽可能快地发生。

* - **Swap pointers or references to the buffer:**
    This is how our graphics example works, and it's the most common solution for double-buffering graphics.
    - *It's fast.* Regardless of how big the buffer is, the swap is simply a couple of pointer assignments. It's hard to beat that for speed and simplicity.
    - *Outside code cannot store persistent pointers to the buffer.* This is the main limitation. Since we don't actually move the *data*, what we're essentially doing is periodically telling the rest of the codebase to look somewhere else for the buffer, like in our original stage analogy. This means that the rest of the codebase can't store pointers directly to data within the buffer — they may be pointing at the wrong one a moment later. This can be particularly troublesome on a system where the video driver expects the framebuffer to always be at a fixed location in memory. In that case, we won't be able to use this option.
    - *Existing data on the buffer will be from two frames ago, not the last frame.* Successive frames are drawn on alternating buffers with no data copied between them, like so:
      Frame 1 drawn on buffer A
      Frame 2 drawn on buffer B
      Frame 3 drawn on buffer A
      ...
      You'll note that when we go to draw the third frame, the data already on the buffer is from frame *one*, not the more recent second frame. In most cases, this isn't an issue — we usually clear the whole buffer right before drawing. But if we intend to reuse some of the existing data on the buffer, it's important to take into account that that data will be a frame older than we might expect. One classic use of old framebuffer data is simulating motion blur. The current frame is blended with a bit of the previously rendered frame to make a resulting image that looks more like what a real camera captures.

**CN:** - **交换指向缓冲区的指针或引用：**
    这是我们的图形示例的做法，也是双缓冲图形最常见的解决方案。
    - *速度快。* 无论缓冲区有多大，交换只是几个指针赋值。在速度和简单性方面很难打败它。
    - *外部代码不能存储指向缓冲区的持久指针。* 这是主要的限制。因为我们实际上并没有移动*数据*，我们本质上是在定期告诉代码库的其他部分去别处寻找缓冲区，就像我们最初的舞台类比一样。这意味着代码库的其他部分不能将指针直接存储在缓冲区内的数据上——它们可能在片刻之后指向错误的缓冲区。在视频驱动期望帧缓冲区始终位于内存固定位置的系统上，这可能特别麻烦。在这种情况下，我们将无法使用此选项。
    - *缓冲区上现有数据将是两帧之前的，而不是最近一帧的。* 连续的帧在交替的缓冲区上绘制，它们之间没有数据拷贝：
      第 1 帧绘制在缓冲区 A
      第 2 帧绘制在缓冲区 B
      第 3 帧绘制在缓冲区 A
      ...
      你会注意到，当我们要绘制第三帧时，缓冲区上已有的数据来自第*一*帧，而不是更近的第二帧。在大多数情况下，这不是问题——我们通常在绘制之前清除整个缓冲区。但如果我们打算重用缓冲区上的部分现有数据，重要的是要考虑该数据将比我们预期的老一帧。旧帧缓冲区数据的一个经典用途是模拟运动模糊。当前帧与少量先前渲染的帧混合，以产生看起来更像真实相机捕捉的结果图像。

* - **Copy the data between the buffers:**
    If we can't repoint users to the other buffer, the only other option is to actually copy the data from the next frame to the current frame. This is how our slapstick comedians work. In that case, we chose this method because the state — a single Boolean flag — doesn't take any longer to copy than a pointer to the buffer would.
    - *Data on the next buffer is only a single frame old.* This is the nice thing about copying the data as opposed to ping-ponging back and forth between the two buffers. If we need access to previous buffer data, this will give us more up-to-date data to work with.
    - *Swapping can take more time.* This, of course, is the big negative point. Our swap operation now means copying the entire buffer in memory. If the buffer is large, like an entire framebuffer, it can take a significant chunk of time to do this. Since nothing can read or write to *either* buffer while this is happening, that's a big limitation.

**CN:** - **在缓冲区之间拷贝数据：**
    如果我们不能将用户重定向到另一个缓冲区，唯一的其他选择是将数据从下一帧缓冲区拷贝到当前缓冲区。这就是我们的滑稽喜剧演员的工作方式。在这种情况下，我们选择了这种方法，因为状态——一个单一的布尔标志——拷贝所需的时间不会比指向缓冲区的指针更长。
    - *下一帧缓冲区上的数据仅一帧之隔。* 这是拷贝数据相对于在两个缓冲区之间来回乒乓切换的好处。如果我们需要访问之前的缓冲区数据，这将为我们提供更最新的数据供使用。
    - *交换可能需要更多时间。* 当然，这是很大的缺点。我们的交换操作现在意味着在内存中拷贝整个缓冲区。如果缓冲区很大，比如整个帧缓冲区，这可能需要相当多的时间。由于在此期间不能读写*任一*缓冲区，这是一个很大的限制。

### What is the granularity of the buffer? / 缓冲区的粒度是什么？

* The other question is how the buffer itself is organized — is it a single monolithic chunk of data or distributed among a collection of objects? Our graphics example uses the former, and the actors use the latter.

* 另一个问题是缓冲区本身是如何组织的——它是一个单一的整体的数据块，还是分布在对象集合中？我们的图形示例使用前者，而演员使用后者。

* Most of the time, the nature of what you're buffering will lead to the answer, but there's some flexibility. For example, our actors all could have stored their messages in a single message block that they all reference into by their index.

* 大多数时候，你要缓冲的内容的性质将决定答案，但也有一些灵活性。例如，我们的所有演员本可以将它们的消息存储在一个单一的消息块中，通过它们的索引来引用。

* - **If the buffer is monolithic:**
    - *Swapping is simpler.* Since there is only one pair of buffers, a single swap does it. If you can swap by changing pointers, then you can swap the entire buffer, regardless of size, with just a couple of assignments.

**CN:** - **如果缓冲区是整体的：**
    - *交换更简单。* 由于只有一对缓冲区，一次交换就完成了。如果你可以通过改变指针来交换，那么你可以用几个赋值来交换整个缓冲区，无论其大小如何。

* - **If many objects have a piece of data:**
    - *Swapping is slower.* In order to swap, we need to iterate through the entire collection of objects and tell each one to swap.
      In our comedian example, that was OK since we needed to clear the next slap state anyway — every piece of buffered state needed to be touched each frame. If we don't need to otherwise touch the old buffer, there's a simple optimization we can do to get the same performance of a monolithic buffer while distributing the buffer across multiple objects.
      The idea is to get the "current" and "next" pointer concept and apply it to each of our objects by turning them into object-relative *offsets*. Like so:

**CN:** - **如果许多对象各有一块数据：**
    - *交换较慢。* 为了交换，我们需要遍历整个对象集合，告诉每个对象交换。
      在我们的喜剧演员示例中，这没问题，因为无论如何我们需要清除下一帧的扇状态——每一帧都需要触碰每一块缓冲状态。如果我们不需要触碰旧的缓冲区，有一个简单的优化可以实现整体式缓冲区的相同性能，同时将缓冲区分布在多个对象上。
      这个想法是获取"当前"和"下一帧"指针的概念，并通过将它们变成对象相对的*偏移量*来应用于我们的每个对象。像这样：

```cpp
class Actor {
public:
  static void init() { current_ = 0; }
  static void swap() { current_ = next(); }

  void slap()        { slapped_[next()] = true; }
  bool wasSlapped()  { return slapped_[current_]; }

private:
  static int current_;
  static int next()  { return 1 - current_; }

  bool slapped_[2];
};
```

* Actors access their current slap state by using `current_` to index into the state array. The next state is always the other index in the array, so we can calculate that with `next()`. Swapping the state simply alternates the `current_` index. The clever bit is that `swap()` is now a *static* function — it only needs to be called once, and *every* actor's state will be swapped.

* 演员通过使用 `current_` 索引到状态数组来访问它们的当前扇状态。下一帧状态总是数组中的另一个索引，所以我们可以用 `next()` 计算它。交换状态只需交替 `current_` 索引。巧妙之处在于 `swap()` 现在是一个*静态*函数——它只需要被调用一次，*每个*演员的状态都会被交换。

---

## See Also / 参见

* - You can find the Double Buffer pattern in use in almost every graphics API out there. For example, OpenGL has `swapBuffers()`, Direct3D has "swap chains", and Microsoft's XNA framework swaps the framebuffers within its `endDraw()` method.

* - 你可以在几乎每一个图形 API 中找到双缓冲模式的使用。例如，OpenGL 有 `swapBuffers()`，Direct3D 有"交换链"，微软的 XNA 框架在其 `endDraw()` 方法中交换帧缓冲区。

---

## C# Equivalent / C# 对照实现

```csharp
/// <summary>
/// 帧缓冲区 —— 存储像素数据的内存块
/// EN: A block of memory storing pixel data that the video hardware reads from.
/// CN: 存储像素数据的内存块，视频硬件从中读取数据。
/// </summary>
public class Framebuffer
{
    // 宽高常量，模拟简单的 160x120 分辨率
    // EN: Constants simulating a 160x120 resolution display
    // CN: 模拟 160x120 分辨率显示器的常量
    private const int Width = 160;
    private const int Height = 120;

    // 用字节数组存储所有像素（每个像素一个字节，简化的颜色格式）
    // EN: Byte array holding all pixel data (simplified: 1 byte per pixel)
    // CN: 用字节数组保存所有像素数据（简化版：每个像素 1 字节）
    private readonly byte[] pixels = new byte[Width * Height];

    // 颜色常量
    // EN: Color constants for our simplified pixel format
    // CN: 简化像素格式的颜色常量
    private const byte White = 0xFF;
    private const byte Black = 0x00;

    public Framebuffer()
    {
        Clear();
    }

    /// <summary>
    /// 将整个缓冲区重置为白色
    /// EN: Reset the entire buffer to white
    /// CN: 把整个缓冲区重置为白色，相当于擦除画布
    /// </summary>
    public void Clear()
    {
        // 使用 Array.Fill 高效填充整个数组
        // EN: Efficiently fill the entire array using Array.Fill
        // CN: 用 Array.Fill 高效地填充整个数组，避免手动循环
        Array.Fill(pixels, White);
    }

    /// <summary>
    /// 在指定坐标绘制一个黑色像素
    /// EN: Draw a single black pixel at the given coordinates
    /// CN: 在指定坐标绘制一个黑色像素点
    /// </summary>
    public void Draw(int x, int y)
    {
        // 将二维坐标转换为一维数组索引
        // EN: Convert 2D coordinates to 1D array index
        // CN: 把二维坐标 (x, y) 转成一维数组的索引位置
        pixels[Width * y + x] = Black;
    }

    /// <summary>
    /// 获取像素数据的只读副本（供视频硬件读取）
    /// EN: Get a read-only copy of pixel data (for the video hardware)
    /// CN: 获取像素数据的只读副本（供视频硬件读取）
    /// </summary>
    public ReadOnlySpan<byte> GetPixels()
    {
        // 返回只读跨度，防止外部代码意外修改缓冲区内容
        // EN: Return a read-only span to prevent external code from accidentally modifying the buffer
        // CN: 返回只读跨度，防止外部代码意外篡改缓冲区内容
        return new ReadOnlySpan<byte>(pixels);
    }
}

/// <summary>
/// 场景 —— 使用双缓冲机制避免画面撕裂
/// EN: Scene class that uses double buffering to avoid screen tearing
/// CN: 场景类，使用双缓冲机制避免画面撕裂。
///     核心思路：用户在屏幕上看到的永远是一张"完整"的图，
///     而不是半成品。
/// </summary>
public class Scene
{
    // 两个缓冲区 —— 双缓冲的核心，一个作为"当前帧"显示，
    // 另一个作为"下一帧"在后台绘制
    // EN: Two buffers — the core of double buffering.
    //     One is the "current" frame being displayed,
    //     the other is "next" frame being drawn in the background.
    // CN: 两个缓冲区——双缓冲的核心：
    //     - currentBuffer: 当前正在屏幕上显示的内容
    //     - nextBuffer: 后台正在绘制的下一帧内容
    private readonly Framebuffer[] buffers = new Framebuffer[2];
    private int currentIndex = 0;  // 当前显示的缓冲区索引

    public Scene()
    {
        // 初始化两个缓冲区
        // EN: Initialize both framebuffers
        // CN: 初始化两个缓冲区，确保它们都处于可用状态
        buffers[0] = new Framebuffer();
        buffers[1] = new Framebuffer();
    }

    /// <summary>
    /// 获取当前显示的缓冲区（供视频读取）
    /// EN: Get the buffer currently being displayed (for the video driver)
    /// CN: 获取当前显示的缓冲区（供视频驱动读取）。
    ///     外部代码永远只能看到"当前"缓冲区，
    ///     看不到正在绘制的"下一帧"缓冲区。
    /// </summary>
    public Framebuffer GetCurrentBuffer()
    {
        return buffers[currentIndex];
    }

    /// <summary>
    /// 绘制场景 —— 在后台缓冲区中绘制，完成后交换
    /// EN: Draw the scene — draw into the back buffer, then swap on completion
    /// CN: 绘制场景——先在后台缓冲区中绘制所有内容，
    ///     绘制完成后再通过 Swap() 交换到前台显示。
    ///     这样用户永远不会看到绘制到一半的画面。
    /// </summary>
    public void Draw()
    {
        // 1. 获取后台缓冲区（当前不显示的另一个缓冲区）
        // EN: Get the back buffer (the one not currently displayed)
        // CN: 获取后台缓冲区 = currentIndex 的"对面"
        //     (0 对面是 1, 1 对面是 0)
        Framebuffer next = buffers[1 - currentIndex];

        // 2. 清空后台缓冲区，准备绘制新画面
        // EN: Clear the back buffer to prepare for drawing the new frame
        // CN: 清空后台缓冲区，相当于擦掉上一帧的旧内容，
        //     准备绘制全新的当前帧
        next.Clear();

        // 3. 在后台缓冲区上逐像素绘制内容
        //    此时视频硬件仍在读取前一个缓冲区，
        //    所以用户完全看不到我们的绘制过程
        // EN: Draw pixels one by one on the back buffer.
        //     The video hardware is still reading the other buffer,
        //     so the user never sees this work in progress.
        // CN: 逐像素绘制内容到后台缓冲区。
        //     视频硬件此时仍在读取前一个缓冲区中的完整画面，
        //     因此用户完全看不到我们的绘制过程，不会有撕裂感。
        next.Draw(1, 1);
        next.Draw(4, 1);
        next.Draw(1, 3);
        next.Draw(2, 4);
        next.Draw(3, 4);
        next.Draw(4, 3);

        // 4. 交换缓冲区 —— 这是一个原子操作
        //    EN: Swap buffers — this is an atomic operation
        //    CN: 交换缓冲区——这是一个原子操作。
        //        所谓"原子"意味着在这一瞬间，
        //        没有代码能同时访问"当前"和"下一帧"缓冲区。
        //        交换后，刚刚绘制好的画面立刻显示在屏幕上，
        //        而旧的缓冲区变为下一个"后台缓冲区"。
        Swap();
    }

    /// <summary>
    /// 交换缓冲区 —— 仅需切换索引，极快
    /// EN: Swap buffers — just toggle the index, extremely fast
    /// CN: 交换缓冲区——只是切换索引，时间复杂度 O(1)，
    ///     无论缓冲区有多大，交换本身都是瞬间完成的。
    ///     这就是指针/索引交换的巨大优势。
    /// </summary>
    private void Swap()
    {
        // 把 currentIndex 从 0 翻转为 1，或从 1 翻转为 0
        // EN: Flip currentIndex between 0 and 1
        // CN: 把 currentIndex 在 0 和 1 之间翻转：
        //     1 - 0 = 1, 1 - 1 = 0
        currentIndex = 1 - currentIndex;
    }
}

// 使用示例：
// var scene = new Scene();
// scene.Draw();               // 每帧调用一次 Draw
// var display = scene.GetCurrentBuffer();  // 视频驱动获取当前显示内容
```

---

## Unity Application / Unity 应用场景

* Unity uses double buffering extensively:
- **Rendering**: Unity's graphics pipeline uses a swap chain (multiple back buffers). `Camera.Render()` writes to a back buffer; Unity automatically swaps when rendering completes.
- **Animation state**: When modifying `Animator` parameters, changes are buffered and applied at specific points to avoid half-updated states.
- **Physics**: `FixedUpdate` results are buffered; `Update` reads interpolated physics state, ensuring rendering never sees an in-progress physics step.
- **Custom implementations**: Use double buffering for any data read by one system while being written by another (e.g., network state, input buffering).

**CN:** Unity 广泛使用双缓冲：
- **渲染**：Unity 的图形管线使用交换链（多个后台缓冲区）。`Camera.Render()` 写入后台缓冲区，完成后 Unity 自动交换。
- **动画状态**：修改 `Animator` 参数时，变更被缓冲并在特定时间点应用，避免半更新状态。
- **物理**：`FixedUpdate` 的结果被缓冲，`Update` 读取插值后的物理状态，确保渲染永远不会看到物理计算的中间状态。
- **自定义实现**：任何由一系统写入而另一系统读取的数据都适合双缓冲（如网络状态、输入缓冲）。

---

## Key Differences / 关键差异

```
C++ (原书)                  | C# (Unity 实现)               | 说明
------------------------------------------------------------------------------------------------
指针交换                    | 索引交换 (1 - index)          | C# 中索引交换更安全，避免悬挂指针
char pixels_[WIDTH*HEIGHT]; | byte[] pixels                | C# 使用托管数组，GC 需注意
const char* getPixels()     | ReadOnlySpan<byte>            | C# 提供只读视图，增强安全性
手动 for 循环清空           | Array.Fill()                 | C# 内置批量操作更简洁高效
Framebuffer* 原始指针       | Framebuffer[] + int index    | C# 使用数组 + 索引，无需手动内存管理
无 GC 压力                  | 需注意 GC 分配               | C# 应避免每帧 new 对象，使用对象池
```

---

# Game Loop — 游戏循环模式

## Intent / 意图

* Decouple the progression of game time from user input and processor speed.

* 将游戏时间的推进与用户输入和处理器速度解耦。

---

## Motivation / 动机

If there is one pattern this book couldn't live without, this is it. Game loops are the quintessential example of a "game programming pattern". Almost every game has one, no two are exactly alike, and relatively few programs outside of games use them.

如果这个书只能有一个模式，那就是它了。游戏循环是"游戏编程模式"的典型代表。几乎每个游戏都有一个，没有两个是完全相同的，而且游戏之外的程序很少使用它们。

To see how they're useful, let's take a quick trip down memory lane. In the olden days of computer programming when everyone had beards, programs worked like your dishwasher. You dumped a load of code in, pushed a button, waited, and got results out. Done. These were *batch mode* programs — once the work was done, the program stopped.

为了理解它们的用处，让我们快速回忆一下过去。在计算机编程的古老年代，当每个人都留着胡子的时候，程序就像你的洗碗机一样工作。你扔进一堆代码，按下一个按钮，等待，然后得到结果。完成。这些是*批处理*程序——一旦工作完成，程序就停止了。

Ada Lovelace and Rear Admiral Grace Hopper had honorary beards.

（Ada Lovelace 和 Grace Hopper 海军少将拥有荣誉胡须。）

You still see these today, though thankfully we don't have to write them on punch cards anymore. Shell scripts, command line programs, and even the little Python script that turns a pile of Markdown into this book are all batch mode programs.

今天你仍然能看到它们，虽然谢天谢地我们不再需要在穿孔卡片上编写它们。Shell 脚本、命令行程序，甚至那个把一堆 Markdown 变成这本书的小 Python 脚本，都是批处理程序。
---

### Interview with a CPU / 与 CPU 的对话

Eventually, programmers realized having to drop off a batch of code at the computing office and come back a few hours later for the results was a terribly slow way to get the bugs out of a program. They wanted immediate feedback. *Interactive* programs were born. Some of the first interactive programs were games:

最终，程序员们意识到，把一堆代码丢到计算办公室，几小时后再回来拿结果，这是一种极其缓慢的调试程序的方式。他们想要即时反馈。*交互式*程序诞生了。最初的一些交互式程序就是游戏：

```
YOU ARE STANDING AT THE END OF A ROAD BEFORE A SMALL BRICK BUILDING .
AROUND YOU IS A FOREST. A SMALL STREAM FLOWS OUT OF THE BUILDING AND DOWN A GULLY.
> GO IN
YOU ARE INSIDE A BUILDING, A WELL HOUSE FOR A LARGE SPRING.
```

This is [Colossal Cave Adventure](http://en.wikipedia.org/wiki/Colossal_Cave_Adventure), the first adventure game.

这是 [Colossal Cave Adventure](http://en.wikipedia.org/wiki/Colossal_Cave_Adventure)，第一个冒险游戏。

You could have a live conversation with the program. It waited for your input, then it would respond to you. You would reply back, taking turns just like you learned to do in kindergarten. When it was your turn, it sat there doing nothing. Something like:

你可以与程序进行实时对话。它等待你的输入，然后响应你。你回复它，像幼儿园里学的那样轮流来。当轮到它时，它就坐在那里什么都不做。类似这样：

```cpp
while (true)
{
  char* command = readCommand();
  handleCommand(command);
}
```

This loops forever, so there's no way to quit the game. A real game would do something like `while (!done)` and set `done` to exit. I've omitted that to keep things simple.

这永远循环，所以没有退出游戏的方法。真正的游戏会做类似 `while (!done)` 并将 `done` 设置为 true 来退出。我省略了这点以保持简单。
---

### Event Loops / 事件循环

Modern graphic UI applications are surprisingly similar to old adventure games once you shuck their skin off. Your word processor usually just sits there doing nothing until you press a key or click something:

一旦剥去现代图形 UI 应用的外壳，它们与古老的冒险游戏惊人地相似。你的文字处理器通常只是坐在那里什么都不做，直到你按下一个键或点击某个东西：

```cpp
while (true)
{
  Event* event = waitForEvent();
  dispatchEvent(event);
}
```

The main difference is that instead of *text commands*, the program is waiting for *user input events* — mouse clicks and key presses. It still works basically like the old text adventures where the program *blocks* waiting for user input, which is a problem.

主要的区别在于，程序等待的不是*文本命令*，而是*用户输入事件*——鼠标点击和按键。它仍然像古老的文字冒险游戏一样工作，程序*阻塞*等待用户输入，这是一个问题。

Unlike most other software, games keep moving even when the user isn't providing input. If you sit staring at the screen, the game doesn't freeze. Animations keep animating. Visual effects dance and sparkle. If you're unlucky, that monster keeps chomping on your hero.

与大多数其他软件不同，即使用户没有提供输入，游戏也会持续运行。如果你坐着盯着屏幕，游戏不会冻结。动画持续播放。视觉特效舞动闪烁。如果你不走运，那个怪物会继续啃咬你的英雄。

Most event loops do have "idle" events so you can intermittently do stuff without user input. That's good enough for a blinking cursor or a progress bar, but too rudimentary for games.

大多数事件循环确实有"空闲"事件，所以你可以在没有用户输入的情况下间歇地做一些事情。这对闪烁的光标或进度条来说足够了，但对游戏来说太简陋了。

This is the first key part of a real game loop: *it processes user input, but doesn't wait for it*. The loop always keeps spinning:

这是真正游戏循环的第一个关键部分：*它处理用户输入，但不等待输入*。循环始终保持运转：

```cpp
while (true)
{
  processInput();
  update();
  render();
}
```

We'll refine this later, but the basic pieces are here. `processInput()` handles any user input that has happened since the last call. Then, `update()` advances the game simulation one step. It runs AI and physics (usually in that order). Finally, `render()` draws the game so the player can see what happened.

我们稍后会完善它，但基本要素就在这里。`processInput()` 处理自上次调用以来发生的任何用户输入。然后 `update()` 将游戏模拟推进一步。它运行 AI 和物理（通常按此顺序）。最后，`render()` 绘制游戏画面，让玩家看到发生了什么。

As you might guess from the name, `update()` is a good place to use the [Update Method](update-method.html) pattern.

正如你可能从名字中猜到的，`update()` 是使用 [Update Method（更新方法）](update-method.html)模式的好地方。
---

### A World Out of Time / 时间之外的世界

If this loop isn't blocking on input, that leads to the obvious question: how *fast* does it spin? Each turn through the game loop advances the state of the game by some amount. From the perspective of an inhabitant of the game world, the hand of their clock has ticked forward.

如果这个循环不阻塞在输入上，那就引出一个显而易见的问题：它转得*多快*？游戏循环的每一轮都将游戏状态推进一定的量。从游戏世界居民的角度来看，他们时钟的指针向前走了一格。

The common terms for one crank of the game loop are "tick" and "frame".

游戏循环一次转动的常用术语是"tick"和"frame"（帧）。

Meanwhile, the *player's* actual clock is ticking. If we measure how quickly the game loop cycles in terms of real time, we get the game's "frames per second". If the game loop cycles quickly, the FPS is high and the game moves smoothly and quickly. If it's slow, the game jerks along like a stop motion movie.

与此同时，*玩家*的实际时钟在滴答作响。如果我们用真实时间来衡量游戏循环的周期，就得到了游戏的"每秒帧数"（FPS）。如果游戏循环周期快，FPS 就高，游戏运行流畅快速。如果慢，游戏就像定格动画一样卡顿。

With the crude loop we have now where it just cycles as quickly as it can, two factors determine the frame rate. The first is *how much work it has to do each frame*. Complex physics, a bunch of game objects, and lots of graphic detail all will keep your CPU and GPU busy, and it will take longer to complete a frame.

用现在这个尽可能快循环的原始循环，有两个因素决定了帧率。第一个是*每帧需要做多少工作*。复杂的物理、大量的游戏对象和丰富的图形细节都会让你的 CPU 和 GPU 忙碌，完成一帧需要更长时间。

The second is *the speed of the underlying platform.* Faster chips churn through more code in the same amount of time. Multiple cores, GPUs, dedicated audio hardware, and the OS's scheduler all affect how much you get done in one tick.

第二个是*底层平台的速度*。更快的芯片在相同时间内处理更多的代码。多核、GPU、专用音频硬件和操作系统的调度器都会影响你在一个 tick 内完成多少工作。
---

### Seconds Per Second / 每秒的秒数

In early video games, that second factor was fixed. If you wrote a game for the NES or Apple IIe, you knew *exactly* what CPU your game was running on and you could (and did) code specifically for that. All you had to worry about was how much work you did each tick.

在早期的视频游戏中，第二个因素是固定的。如果你为 NES 或 Apple IIe 编写游戏，你*确切地*知道你的游戏运行在什么 CPU 上，你可以（并且确实）专门为此编码。你只需要担心每个 tick 做多少工作。

Older games were carefully coded to do just enough work each frame so that the game ran at the speed the developers wanted. But if you tried to play that same game on a faster or slower machine, then the game itself would speed up or slow down.

老游戏被精心编码，每帧做刚好够的工作，让游戏以开发者想要的速度运行。但如果你试图在更快或更慢的机器上玩同一个游戏，那么游戏本身就会加速或减速。

This is why old PCs used to have "[turbo](http://en.wikipedia.org/wiki/Turbo_button)" buttons. New PCs were faster and couldn't play old games because the games would run too fast. Turning the turbo button *off* would slow the machine down and make old games playable.

这就是为什么老 PC 曾经有"[turbo](http://en.wikipedia.org/wiki/Turbo_button)"（涡轮）按钮。新的 PC 更快，无法玩老游戏，因为游戏会运行得太快。*关闭*涡轮按钮会减慢机器速度，使老游戏可以玩。

These days, though, few developers have the luxury of knowing exactly what hardware their game will run on. Instead, our games must intelligently adapt to a variety of devices.

但如今，很少有开发者有幸确切知道他们的游戏将在什么硬件上运行。相反，我们的游戏必须智能地适应各种设备。

This is the other key job of a game loop: *it runs the game at a consistent speed despite differences in the underlying hardware.*

这是游戏循环的另一个关键工作：*尽管底层硬件存在差异，它仍以一致的速度运行游戏。*
---

## The Pattern / 模式定义

A **game loop** runs continuously during gameplay. Each turn of the loop, it **processes user input** without blocking, **updates the game state**, and **renders the game**. It tracks the passage of time to **control the rate of gameplay**.

**游戏循环**在游戏过程中持续运行。循环的每一轮，它**非阻塞地处理用户输入**、**更新游戏状态**和**渲染游戏画面**。它追踪时间的流逝以**控制游戏节奏**。
---

## When to Use It / 何时使用

Using the wrong pattern can be worse than using no pattern at all, so this section is normally here to caution against over-enthusiasm. The goal of design patterns isn't to cram as many into your codebase as you can.

使用错误的模式可能比不使用任何模式更糟糕，所以这一部分通常是为了告诫不要过度热衷。设计模式的目标不是尽可能多地把它们塞进你的代码库。

But this pattern is a bit different. I can say with pretty good confidence that you *will* use this pattern. If you're using a game engine, you won't write it yourself, but it's still there.

但这个模式有点不同。我可以相当有把握地说，你*将*会使用这个模式。如果你在使用游戏引擎，你不会自己编写它，但它仍然存在。

For me, this is the difference between an "engine" and a "library". With libraries, you own the main game loop and call into the library. An engine owns the loop and calls into *your* code.

对我来说，这就是"引擎"和"库"的区别。使用库时，你拥有主游戏循环并调用库。引擎拥有循环并调用*你的*代码。

You might think you won't need this if you're making a turn-based game. But even there, though the *game state* won't advance until the user takes their turn, the *visual* and *audible* states of the game usually do. Animation and music keep running even when the game is "waiting" for you to take your turn.

你可能会认为如果做的是回合制游戏就不需要这个。但即使在那里，虽然*游戏状态*在玩家进行操作之前不会推进，但游戏的*视觉*和*听觉*状态通常仍在进行。即使游戏在"等待"你进行操作时，动画和音乐也一直在运行。
---

## Keep in Mind / 牢记

The loop we're talking about here is some of the most important code in your game. They say a program spends 90% of its time in 10% of the code. Your game loop will be firmly in that 10%. Take care with this code, and be mindful of its efficiency.

我们在这里讨论的循环是你的游戏中最重要的一些代码。人们说一个程序 90% 的时间花在 10% 的代码上。你的游戏循环将牢牢地处于那 10% 中。要精心处理这段代码，并注意它的效率。

Made up statistics like this are why "real" engineers like mechanical and electrical engineers don't take us seriously.

像这样编造的统计数据就是为什么像机械工程师和电子工程师这样的"真正"工程师不把我们当回事的原因。
---

### You May Need to Coordinate with the Platform's Event Loop / 你可能需要与平台的事件循环协调

If you're building your game on top of an OS or platform that has a graphic UI and an event loop built in, then you have *two* application loops in play. They'll need to play nice together.

如果你在具有图形 UI 和内建事件循环的操作系统或平台上构建游戏，那么你就有*两个*应用循环在运行。它们需要很好地协同工作。

Sometimes, you can take control and make your loop the only one. For example, if you're writing a game against the venerable Windows API, your `main()` can just have a game loop. Inside, you can call `PeekMessage()` to handle and dispatch events from the OS. Unlike `GetMessage()`, `PeekMessage()` doesn't block waiting for user input, so your game loop will keep cranking.

有时，你可以控制并让你的循环成为唯一的循环。例如，如果你针对古老的 Windows API 编写游戏，你的 `main()` 可以只有一个游戏循环。在内部，你可以调用 `PeekMessage()` 来处理和分发来自操作系统的事件。与 `GetMessage()` 不同，`PeekMessage()` 不会阻塞等待用户输入，所以你的游戏循环会持续运转。

Other platforms don't let you opt out of the event loop so easily. If you're targeting a web browser, the event loop is deeply built into browser's execution model. There, the event loop will run the show, and you'll use it as your game loop too. You'll call something like `requestAnimationFrame()` and it will call back into your code to keep the game running.

其他平台不会让你这么容易退出事件循环。如果你的目标是 Web 浏览器，事件循环深深内置于浏览器的执行模型中。在那里，事件循环将主导一切，你也将把它用作你的游戏循环。你会调用类似 `requestAnimationFrame()` 的东西，它会回调你的代码以保持游戏运行。
---

## Sample Code / 示例代码

For such a long introduction, the code for a game loop is actually pretty straightforward. We'll walk through a couple of variations and go over their good and bad points.

经过这么长的介绍，游戏循环的代码实际上相当直接。我们将介绍几种变体，并讨论它们的优缺点。

The game loop drives AI, rendering, and other game systems, but those aren't the point of the pattern itself, so we'll just call into fictitious methods here. Actually implementing `render()`, `update()` and others is left as a (challenging!) exercise for the reader.

游戏循环驱动 AI、渲染和其他游戏系统，但这些不是模式本身的重点，所以我们在这里只调用虚构的方法。实际实现 `render()`、`update()` 和其他方法留给读者作为（有挑战性的！）练习。
---

### Run, Run as Fast as You Can / 跑，能跑多快就跑多快

We've already seen the simplest possible game loop:

我们已经看到了最简单的游戏循环：

```cpp
while (true)
{
  processInput();
  update();
  render();
}
```

The problem with it is you have no control over how fast the game runs. On a fast machine, that loop will spin so fast users won't be able to see what's going on. On a slow machine, the game will crawl. If you have a part of the game that's content-heavy or does more AI or physics, the game will actually play slower there.

问题是你无法控制游戏运行的速度。在快的机器上，这个循环会转得太快，用户无法看清发生了什么。在慢的机器上，游戏会缓慢爬行。如果游戏某一部分内容繁重或做了更多的 AI 或物理计算，游戏在那里的运行速度实际上会更慢。
---

### Take a Little Nap / 小睡片刻

The first variation we'll look at adds a simple fix. Say you want your game to run at 60 FPS. That gives you about 16 milliseconds per frame. As long as you can reliably do all of your game processing and rendering in less than that time, you can run at a steady frame rate. All you do is process the frame and then *wait* until it's time for the next one, like so:

我们要看的第一个变体增加了一个简单的修复。假设你想让你的游戏以 60 FPS 运行。这给了你大约每帧 16 毫秒。只要你能可靠地在少于这个时间内完成所有的游戏处理和渲染，你就可以以稳定的帧率运行。你所做的就是处理帧，然后*等待*到下一帧的时间，像这样：

![A simple game loop flowchart. Process Input → Update Game → Render → Wait, then loop back to the beginning.](images/game-loop-simple.png)

![一个简单的游戏循环流程图。处理输入 → 更新游戏 → 渲染 → 等待，然后循环回到起点。](images/game-loop-simple.png)

The code looks a bit like this:

代码看起来有点像这样：

*1000 ms / FPS = ms per frame*.

*1000 ms / FPS = 每帧的毫秒数*。

```cpp
while (true)
{
  double start = getCurrentTime();
  processInput();
  update();
  render();

sleep(start + MS_PER_FRAME - getCurrentTime());
}
```

The `sleep()` here makes sure the game doesn't run too *fast* if it processes a frame quickly. It *doesn't* help if your game runs too *slowly*. If it takes longer than 16ms to update and render the frame, your sleep time goes *negative*. If we had computers that could travel back in time, lots of things would be easier, but we don't.

这里的 `sleep()` 确保如果游戏处理一帧很快时不会运行得太*快*。但如果你的游戏运行得太*慢*，它*没有*帮助。如果更新和渲染帧需要超过 16ms，你的睡眠时间就会变成*负数*。如果我们有能够时间旅行的计算机，很多事情会更容易，但我们没有。

Instead, the game slows down. You can work around this by doing less work each frame — cut down on the graphics and razzle dazzle or dumb down the AI. But that impacts the quality of gameplay for all users, even ones on fast machines.

相反，游戏会变慢。你可以通过每帧做更少的工作来解决这个问题——减少图形和特效，或降低 AI 的智能度。但这会影响所有用户的游戏质量，即使在快机器上也是如此。
---

### One Small Step, One Giant Step / 一小步，一大步

Let's try something a bit more sophisticated. The problem we have basically boils down to:

让我们尝试一些更复杂的东西。我们的问题基本上归结为：

1. Each update advances game time by a certain amount.
2. It takes a certain amount of *real* time to process that.

1. 每次更新将游戏时间推进一定的量。
2. 处理这个需要一定的*真实*时间。

If step two takes longer than step one, the game slows down. If it takes more than 16 ms of processing to advance game time by 16ms, it can't possibly keep up. But if we can advance the game by *more* than 16ms of game time in a single step, then we can update the game less frequently and still keep up.

如果第二步比第一步花费的时间长，游戏就会变慢。如果推进 16ms 的游戏时间需要超过 16ms 的处理时间，它就不可能跟上。但如果我们可以在一个步骤中推进*超过* 16ms 的游戏时间，那么我们就可以不那么频繁地更新游戏，但仍然能跟上。

The idea then is to choose a time step to advance based on how much *real* time passed since the last frame. The longer the frame takes, the bigger steps the game takes. It always keeps up with real time because it will take bigger and bigger steps to get there. They call this a *variable* or *fluid* time step. It looks like:

所以思路是根据自上一帧以来过去了多少*真实*时间来选择要推进的时间步长。帧耗时越长，游戏迈出的步伐越大。它总是能跟上真实时间，因为它会迈出越来越大的步伐来达到目标。这被称为*可变*或*流体*时间步长。它看起来像这样：

```cpp
double lastTime = getCurrentTime();
while (true)
{
  double current = getCurrentTime();
  double elapsed = current - lastTime;
  processInput();
  update(elapsed);
  render();
  lastTime = current;
}
```

Each frame, we determine how much *real* time passed since the last game update (`elapsed`). When we update the game state, we pass that in. The engine is then responsible for advancing the game world forward by that amount of time.

每帧，我们确定自上次游戏更新以来过去了多少*真实*时间（`elapsed`）。当更新游戏状态时，我们传入这个值。然后引擎负责将游戏世界推进那么多时间。

Say you've got a bullet shooting across the screen. With a fixed time step, in each frame, you'll move it according to its velocity. With a variable time step, you *scale that velocity by the elapsed time*. As the time step gets bigger, the bullet moves farther in each frame. That bullet will get across the screen in the *same* amount of *real* time whether it's twenty small fast steps or four big slow ones. This looks like a winner:

假设有一发子弹射过屏幕。使用固定时间步长，在每帧中，你根据其速度移动它。使用可变时间步长，你*将速度按经过的时间缩放*。随着时间步长变大，子弹在每帧中移动得更远。那发子弹穿过屏幕所需的*真实*时间是*相同*的——无论是二十个快速的小步还是四个缓慢的大步。这看起来是个赢家：

- The game plays at a consistent rate on different hardware.
- Players with faster machines are rewarded with smoother gameplay.

- 游戏在不同硬件上以一致的速度运行。
- 拥有更快机器的玩家获得更流畅的游戏体验作为回报。

But, alas, there's a serious problem lurking ahead: we've made the game non-deterministic and unstable. Here's one example of the trap we've set for ourselves:

但是，唉，前方潜伏着一个严重的问题：我们使游戏变得非确定性和不稳定。以下是我们为自己设置的陷阱的一个例子：

"Deterministic" means that every time you run the program, if you give it the same inputs, you get the exact same outputs back. As you can imagine, it's much easier to track down bugs in deterministic programs — find the inputs that caused the bug the first time, and you can cause it every time.

"确定性"意味着每次运行程序时，如果输入相同，你将得到完全相同的输出。可以想象，在确定性程序中追踪 bug 要容易得多——找到第一次导致 bug 的输入，你就可以每次都重现它。

Computers are naturally deterministic; they follow programs mechanically. Non-determinism appears when the messy real world creeps in. For example, networking, the system clock, and thread scheduling all rely on bits of the external world outside of the program's control.

计算机天然是确定性的；它们机械地执行程序。当混乱的 real 世界渗透进来时，非确定性就出现了。例如，网络、系统时钟和线程调度都依赖于程序控制之外的外部世界的某些部分。

Say we've got a two-player networked game and Fred has some beast of a gaming machine while George is using his grandmother's antique PC. That aforementioned bullet is flying across both of their screens. On Fred's machine, the game is running super fast, so each time step is tiny. We cram, like, 50 frames in the second it takes the bullet to cross the screen. Poor George's machine can only fit in about five frames.

假设我们有一个双人联网游戏，Fred 有一台性能怪兽般的游戏机，而 George 在用它祖母的古董 PC。前面提到的那发子弹正飞过他们两个的屏幕。在 Fred 的机器上，游戏运行得超快，所以每个时间步都很小。在子弹穿过屏幕的那一秒里，我们塞进了大约 50 帧。可怜的 George 的机器只能容纳大约五帧。

This means that on Fred's machine, the physics engine updates the bullet's position 50 times, but George's only does it five times. Most games use floating point numbers, and those are subject to *rounding error*. Each time you add two floating point numbers, the answer you get back can be a bit off. Fred's machine is doing ten times as many operations, so he'll accumulate a bigger error than George. The *same* bullet will end up in *different places* on their machines.

这意味着在 Fred 的机器上，物理引擎更新子弹位置 50 次，但 George 的只更新五次。大多数游戏使用浮点数，这些数会受到*舍入误差*的影响。每次你将两个浮点数相加，得到的结果可能会有一点偏差。Fred 的机器执行的操作次数是十倍，所以他会积累比 George 更大的误差。*同一个*子弹最终会在他们的机器上处于*不同的位置*。

This is just one nasty problem a variable time step can cause, but there are more. In order to run in real time, game physics engines are approximations of the real laws of mechanics. To keep those approximations from blowing up, damping is applied. That damping is carefully tuned to a certain time step. Vary that, and the physics gets unstable.

这只是可变时间步长可能导致的一个讨厌问题，但还有更多。为了实时运行，游戏物理引擎是对真实力学定律的近似。为了防止这些近似失控，会应用阻尼。该阻尼是针对特定的时间步长精心调校的。改变它，物理就会变得不稳定。

"Blowing up" is literal here. When a physics engine flakes out, objects can get completely wrong velocities and launch themselves into the air.

这里的"失控"是字面意思。当物理引擎出问题时，物体可能获得完全错误的速度并发射到空中。

This instability is bad enough that this example is only here as a cautionary tale and to lead us to something better…

这种不稳定性已经足够糟糕，以至于这个例子仅仅作为警示故事存在，并引导我们走向更好的方案……
---

### Play Catch Up / 追赶游戏时间

One part of the engine that usually *isn't* affected by a variable time step is rendering. Since the rendering engine captures an instant in time, it doesn't care how much time advanced since the last one. It renders things wherever they happen to be right then.

引擎中通常*不*受可变时间步长影响的部分是渲染。由于渲染引擎捕捉的是时间中的一个瞬间，它不关心自上次以来时间推进了多少。它只渲染物体当时所在的位置。

This is more or less true. Things like motion blur can be affected by time step, but if they're a bit off, the player doesn't usually notice.

这或多或少是正确的。像运动模糊这样的东西可能会受时间步长影响，但如果它们有一点偏差，玩家通常不会注意到。

We can use this fact to our advantage. We'll *update* the game using a fixed time step because that makes everything simpler and more stable for physics and AI. But we'll allow flexibility in when we *render* in order to free up some processor time.

我们可以利用这个事实。我们将使用固定时间步长来*更新*游戏，因为这使物理和 AI 的一切更简单、更稳定。但我们在*渲染*的时间点上允许灵活性，以释放一些处理器时间。

It goes like this: A certain amount of real time has elapsed since the last turn of the game loop. This is how much game time we need to simulate for the game's "now" to catch up with the player's. We do that using a *series* of *fixed* time steps. The code looks a bit like:

它是这样工作的：自游戏循环上一轮以来，已经过了一定的真实时间。这就是我们需要模拟的游戏时间量，以使游戏的"现在"赶上玩家的时间。我们使用*一系列**固定*时间步长来实现这一点。代码看起来有点像：

```cpp
double previous = getCurrentTime();
double lag = 0.0;
while (true)
{
  double current = getCurrentTime();
  double elapsed = current - previous;
  previous = current;
  lag += elapsed;

processInput();

while (lag >= MS_PER_UPDATE)
  {
    update();
    lag -= MS_PER_UPDATE;
  }

render();
}
```

There's a few pieces here. At the beginning of each frame, we update `lag` based on how much real time passed. This measures how far the game's clock is behind compared to the real world. We then have an inner loop to update the game, one fixed step at a time, until it's caught up. Once we're caught up, we render and start over again. You can visualize it sort of like this:

这里有几个部分。在每帧开始时，我们根据经过的真实时间更新 `lag`。这衡量了游戏时钟落后于真实世界多少。然后我们有一个内层循环来更新游戏，一次一个固定步长，直到它赶上。一旦赶上，我们就渲染并重新开始。你可以这样想象：

![A modified flowchart. Process Input → Update Game → Wait, then loop back to this step then → Render → Loop back to the beginning.](images/game-loop-fixed.png)

![一个修改后的流程图。处理输入 → 更新游戏 → 等待，然后循环回到这一步 → 然后 → 渲染 → 循环回到起点。](images/game-loop-fixed.png)

Note that the time step here isn't the *visible* frame rate anymore. `MS_PER_UPDATE` is just the *granularity* we use to update the game. The shorter this step is, the more processing time it takes to catch up to real time. The longer it is, the choppier the gameplay is. Ideally, you want it pretty short, often faster than 60 FPS, so that the game simulates with high fidelity on fast machines.

请注意，这里的时间步长不再是*可见的*帧率。`MS_PER_UPDATE` 只是我们用来更新游戏的*粒度*。这个步长越短，追上真实时间所需的处理时间就越多。它越长，游戏玩法就越卡顿。理想情况下，你想要它相当短，通常快于 60 FPS，这样游戏在快机器上能以高保真度模拟。

But be careful not to make it *too* short. You need to make sure the time step is greater than the time it takes to process an `update()`, even on the slowest hardware. Otherwise, your game simply can't catch up.

但注意不要让它*太*短。你需要确保时间步长大于处理一次 `update()` 所需的时间，即使在最慢的硬件上也是如此。否则，你的游戏根本无法赶上。

I left it out here, but you can safeguard this by having the inner update loop bail after a maximum number of iterations. The game will slow down then, but that's better than locking up completely.

我在这里省略了，但你可以通过让内层更新循环在达到最大迭代次数后退出（bail）来保护。游戏那时会变慢，但这比完全锁死要好。

Fortunately, we've bought ourselves some breathing room here. The trick is that we've *yanked rendering out of the update loop*. That frees up a bunch of CPU time. The end result is the game *simulates* at a constant rate using safe fixed time steps across a range of hardware. It's just that the player's *visible window* into the game gets choppier on a slower machine.

幸运的是，我们在这里为自己争取到了一些喘息空间。诀窍在于我们*将渲染从更新循环中抽离出来*。这释放了大量的 CPU 时间。最终结果是游戏在一系列硬件上使用安全的固定时间步长以恒定速率*模拟*。只是玩家进入游戏的*可视窗口*在较慢的机器上会变得更卡顿。
---

### Stuck in the Middle / 卡在中间

There's one issue we're left with, and that's residual lag. We update the game at a fixed time step, but we render at arbitrary points in time. This means that from the user's perspective, the game will often display at a point in time between two updates.

我们还剩下一个问题，那就是残余的滞后。我们以固定的时间步长更新游戏，但在任意时间点渲染。这意味着从用户的角度来看，游戏经常显示在两个更新之间的时间点。

Here's a timeline:

这是一个时间线：

![A timeline containing evenly spaced Updates and intermittent Renders.](images/game-loop-timeline.png)

![一个包含均匀间隔的更新和间歇性渲染的时间线。](images/game-loop-timeline.png)

As you can see, we update at a nice tight, fixed interval. Meanwhile, we render whenever we can. It's less frequent than updating, and it isn't steady either. Both of those are OK. The lame part is that we don't always render right at the point of updating. Look at the third render time. It's right between two updates:

如你所见，我们以一个紧凑、固定的间隔进行更新。同时，我们尽可能地进行渲染。它比更新频率低，而且也不稳定。这两者都可以接受。糟糕的部分是我们并不总是在更新点上进行渲染。看第三个渲染时间。它正好在两个更新之间：

![Close-up of the timeline showing Renders falling between Update steps.](images/game-loop-timeline-close.png)

![时间线的特写，显示渲染落在更新步骤之间。](images/game-loop-timeline-close.png)

Imagine a bullet is flying across the screen. On the first update, it's on the left side. The second update moves it to the right side. The game is rendered at a point in time between those two updates, so the user expects to see that bullet in the center of the screen. With our current implementation, it will still be on the left side. This means motion looks jagged or stuttery.

想象一发子弹飞过屏幕。在第一次更新时，它在左侧。第二次更新把它移到了右侧。游戏在两个更新之间的一个时间点被渲染，所以用户期望看到那发子弹在屏幕中央。使用我们当前的实现，它仍然会在左侧。这意味着运动看起来不平滑或卡顿。

Conveniently, we actually know *exactly* how far between update frames we are when we render: it's stored in `lag`. We bail out of the update loop when it's less than the update time step, not when it's *zero*. That leftover amount? That's how far into the next frame we are.

方便的是，我们实际上*确切地*知道渲染时我们处于更新帧之间的什么位置：它存储在 `lag` 中。当 `lag` 小于更新时间步长时我们退出更新循环，而不是当它为*零*时。剩下的量？那就是我们进入下一帧的程度。

When we go to render, we'll pass that in:

当我们进行渲染时，我们会传入这个值：

```cpp
render(lag / MS_PER_UPDATE);
```

We divide by `MS_PER_UPDATE` here to *normalize* the value. The value passed to `render()` will vary from 0 (right at the previous frame) to just under 1.0 (right at the next frame), regardless of the update time step. This way, the renderer doesn't have to worry about the frame rate. It just deals in values from 0 to 1.

我们在这里除以 `MS_PER_UPDATE` 来*归一化*这个值。传入 `render()` 的值将从 0（正好在上一个帧）变化到略低于 1.0（正好在下一个帧），无论更新时间步长是多少。这样，渲染器就不必关心帧率了。它只处理从 0 到 1 的值。

The renderer knows each game object *and its current velocity*. Say that bullet is 20 pixels from the left side of the screen and is moving right 400 pixels per frame. If we are halfway between frames, then we'll end up passing 0.5 to `render()`. So it draws the bullet half a frame ahead, at 220 pixels. Ta-da, smooth motion.

渲染器知道每个游戏对象*及其当前速度*。假设那颗子弹距离屏幕左侧 20 像素，每帧向右移动 400 像素。如果我们处于两帧的中间，那么我们将传入 0.5 给 `render()`。所以它绘制子弹在半帧之后的位置，即 220 像素。瞧，平滑的运动。

Of course, it may turn out that that extrapolation is wrong. When we calculate the next frame, we may discover the bullet hit an obstacle or slowed down or something. We rendered its position interpolated between where it was on the last frame and where we *think* it will be on the next frame. But we don't know that until we've actually done the full update with physics and AI.

当然，结果可能是这个外推是错误的。当我们计算下一帧时，我们可能会发现子弹击中了障碍物或减速了之类的。我们渲染的位置是在上一帧的位置和我们*认为*它在下一帧的位置之间插值的。但在我们实际用物理和 AI 完成完整更新之前，我们不知道这一点。

So the extrapolation is a bit of a guess and sometimes ends up wrong. Fortunately, though, those kinds of corrections usually aren't noticeable. At least, they're less noticeable than the stuttering you get if you don't extrapolate at all.

所以外推有点像猜测，有时会出错。不过幸运的是，这些类型的修正通常不容易被注意到。至少，它们比完全不外推时产生的卡顿更不明显。
---

## Design Decisions / 设计决策

Despite the length of this chapter, I've left out more than I've included. Once you throw in things like synchronizing with the display's refresh rate, multithreading, and GPUs, a real game loop can get pretty hairy. At a high level, though, here are a few questions you'll likely answer:

尽管本章篇幅很长，但我忽略的内容比包含的还多。一旦你加入与显示器刷新率同步、多线程和 GPU 之类的东西，真正的游戏循环可能变得相当复杂。但在高层面上，以下是一些你可能会回答的问题：
---

### Do You Own the Game Loop, or Does the Platform? / 你拥有游戏循环，还是平台拥有？

This is less a choice you make and more one that's made for you. If you're making a game that runs in a web browser, you pretty much *can't* write your own classic game loop. The browser's event-based nature precludes it. Likewise, if you're using an existing game engine, you will probably rely on its game loop instead of rolling your own.

这与其说是你做出的选择，不如说是为你做出的选择。如果你在做一款在 Web 浏览器中运行的游戏，你几乎*不能*编写自己的经典游戏循环。浏览器基于事件的本质排除了这种可能。同样地，如果你在使用现有的游戏引擎，你可能会依赖它的游戏循环而不是自己动手。

- **Use the platform's event loop:**
  - *It's simple.* You don't have to worry about writing and optimizing the core loop of the game.
  - *It plays nice with the platform.* You don't have to worry about explicitly giving the host time to process its own events, caching events, or otherwise managing the impedance mismatch between the platform's input model and yours.
  - *You lose control over timing.* The platform will call your code as it sees fit. If that's not as frequently or as smoothly as you'd like, too bad. Worse, most application event loops weren't designed with games in mind and usually *are* slow and choppy.

- **使用平台的事件循环：**
  - *它很简单。* 你不必担心编写和优化游戏的核心循环。
  - *它与平台协调良好。* 你不必担心显式地给宿主时间处理其自身事件、缓存事件或以其他方式管理平台输入模型和你的输入模型之间的阻抗不匹配。
  - *你失去了对时间控制的权力。* 平台会按其认为合适的方式调用你的代码。如果频率或流畅度不如你意，那也没办法。更糟糕的是，大多数应用程序事件循环并不是为游戏设计的，通常*是*缓慢且卡顿的。

- **Use a game engine's loop:**
  - *You don't have to write it.* Writing a game loop can get pretty tricky. Since that core code gets executed every frame, minor bugs or performance problems can have a large impact on your game. A tight game loop is one reason to consider using an existing engine.
  - *You don't get to write it.* Of course, the flip side to that coin is the loss of control if you *do* have needs that aren't a perfect fit for the engine.

- **使用游戏引擎的循环：**
  - *你不需要编写它。* 编写游戏循环可能相当棘手。由于核心代码每帧都执行，微小的错误或性能问题可能对你的游戏产生重大影响。一个紧凑的游戏循环是考虑使用现有引擎的一个原因。
  - *你不能编写它。* 当然，硬币的另一面是，如果你*确实*有与引擎不完全匹配的需求，就会失去控制。

- **Write it yourself:**
  - *Total control.* You can do whatever you want with it. You can design it specifically for the needs of your game.
  - *You have to interface with the platform.* Application frameworks and operating systems usually expect to have a slice of time to process events and do other work. If you own your app's core loop, it won't get any. You'll have to explicitly hand off control periodically to make sure the framework doesn't hang or get confused.

- **自己编写：**
  - *完全控制。* 你可以随心所欲地处理它。你可以根据游戏的需求专门设计它。
  - *你必须与平台接口对接。* 应用程序框架和操作系统通常期望有一段时间来处理事件和执行其他工作。如果你拥有应用的核心循环，它就不会得到任何时间。你必须定期显式地移交控制权，以确保框架不会挂起或混乱。
---

### How Do You Manage Power Consumption? / 如何管理功耗？

This wasn't an issue five years ago. Games ran on things plugged into walls or on dedicated handheld devices. But with the advent of smartphones, laptops, and mobile gaming, the odds are good that you do care about this now. A game that runs beautifully but turns players' phones into space heaters before running out of juice thirty minutes later is not a game that makes people happy.

这在五年前还不是问题。游戏运行在插着电源的设备或专用手持设备上。但随着智能手机、笔记本电脑和移动游戏的出现，你现在很可能确实关心这个问题。一个运行得很漂亮但把玩家的手机变成太空加热器并在三十分钟后耗尽电量的游戏，不是一个能让玩家开心的游戏。

Now, you may need to think not only about making your game look great, but also use as little CPU as possible. There will likely be an *upper* bound to performance where you let the CPU sleep if you've done all the work you need to do in a frame.

现在，你可能需要考虑的不仅是如何让游戏看起来很棒，还要尽可能少用 CPU。性能可能会有一个*上限*，如果你在一帧中完成了所有需要做的工作，就让 CPU 休眠。

- **Run as fast as it can:**
  This is what you're likely to do for PC games (though even those are increasingly being played on laptops). Your game loop will never explicitly tell the OS to sleep. Instead, any spare cycles will be spent cranking up the FPS or graphic fidelity.
  This gives you the best possible gameplay experience but, it will use as much power as it can. If the player is on a laptop, they'll have a nice lap warmer.

- **尽可能快地运行：**
  这可能是你在 PC 游戏上会做的（尽管即使这些也越来越多地在笔记本电脑上运行）。你的游戏循环永远不会显式地告诉操作系统休眠。相反，任何空闲周期都将用于提高 FPS 或图形保真度。
  这给了你尽可能好的游戏体验，但它会消耗尽可能多的电量。如果玩家用的是笔记本电脑，他们就有了一台不错的暖腿器。

- **Clamp the frame rate:**
  Mobile games are often more focused on the quality of gameplay than they are on maximizing the detail of the graphics. Many of these games will set an upper limit on the frame rate (usually 30 or 60 FPS). If the game loop is done processing before that slice of time is spent, it will just sleep for the rest.
  This gives the player a "good enough" experience and then goes easy on their battery beyond that.

- **限制帧率：**
  移动游戏通常更关注游戏玩法质量，而不是最大化图形细节。许多此类游戏会设置帧率上限（通常是 30 或 60 FPS）。如果游戏循环在这段时间用完之前就完成了处理，它会在剩余时间休眠。
  这给了玩家"足够好"的体验，并在那之外对电池友好。
---

### How Do You Control Gameplay Speed? / 如何控制游戏速度？

A game loop has two key pieces: non-blocking user input and adapting to the passage of time. Input is straightforward. The magic is in how you deal with time. There are a near-infinite number of platforms that games can run on, and any single game may run on quite a few. How it accommodates that variation is key.

游戏循环有两个关键部分：非阻塞的用户输入和适应时间的流逝。输入很直接。魔法在于你如何处理时间。游戏可以运行的平台几乎无穷无尽，任何一个游戏都可能在多个平台上运行。它如何适应这种变化是关键。

Game-making seems to be part of human nature, because every time we've built a machine that can do computing, one of the first things we've done is made games on it. The PDP-1 was a 2 kHz machine with only 4,096 words of memory, yet Steve Russell and friends managed to create Spacewar! on it.

制作游戏似乎是人类天性的一部分，因为每次我们制造出能够计算的机器时，我们做的第一件事就是在上面制作游戏。PDP-1 是一台 2 kHz 的机器，只有 4,096 个字的记忆体，但 Steve Russell 和他的朋友们却在上面创造了 Spacewar！

- **Fixed time step with no synchronization:**
  This was our first sample code. You just run the game loop as fast as you can.
  - *It's simple*. This is its main (well, only) virtue.
  - *Game speed is directly affected by hardware and game complexity.* And its main vice is that if there's any variation, it will directly affect the game speed. It's the fixie of game loops.

- **无同步的固定时间步长：**
  这是我们第一个示例代码。你只是尽可能快地运行游戏循环。
  - *它很简单。* 这是它的主要（嗯，唯一的）优点。
  - *游戏速度直接受硬件和游戏复杂度影响。* 它的主要缺点是如果有任何变化，会直接影响游戏速度。它是游戏循环中的"死飞"（fixie，指自行车中的固定齿轮自行车，无法滑行）。

- **Fixed time step with synchronization:**
  The next step up on the complexity ladder is running the game at a fixed time step but adding a delay or synchronization point at the end of the loop to keep the game from running too fast.
  - *Still quite simple.* It's only one line of code more than the probably-too-simple-to-actually-work example. In most game loops, you will likely do synchronization *anyway*. You will probably [double buffer](double-buffer.html) your graphics and synchronize the buffer flip to the refresh rate of the display.
  - *It's power-friendly.* This is a surprisingly important consideration for mobile games. You don't want to kill the user's battery unnecessarily. By simply sleeping for a few milliseconds instead of trying to cram ever more processing into each tick, you save power.
  - *The game doesn't play too fast.* This fixes half of the speed concerns of a fixed loop.
  - *The game can play too slowly.* If it takes too long to update and render a game frame, playback will slow down. Because this style doesn't separate updating from rendering, it's likely to hit this sooner than more advanced options. Instead of just dropping *rendering* frames to catch up, gameplay will slow down.

- **带同步的固定时间步长：**
  复杂性阶梯上的下一步是以固定时间步长运行游戏，但在循环结束时增加一个延迟或同步点，以防止游戏运行得太快。
  - *仍然相当简单。* 它比那个可能过于简单而无法实际工作的例子只多了一行代码。在大多数游戏循环中，你*无论如何*都可能进行同步。你可能会[双缓冲](double-buffer.html)你的图形，并将缓冲区交换与显示器的刷新率同步。
  - *它对功耗友好。* 这对移动游戏来说是一个惊人的重要考虑因素。你不想不必要地耗尽用户的电池。通过简单地休眠几毫秒而不是试图在每个 tick 中塞入更多的处理，你可以节省电量。
  - *游戏不会运行得太快。* 这修复了固定循环速度问题的一半。
  - *游戏可能运行得太慢。* 如果更新和渲染一帧花费太长时间，播放就会变慢。由于这种风格不分离更新和渲染，它比更高级的选项更早遇到这个问题。游戏玩法会变慢，而不是仅仅丢弃*渲染*帧来追赶。

- **Variable time step:**
  I'll put this in here as an option in the solution space with the caveat that most game developers I know recommend against it. It's good to remember *why* it's a bad idea, though.
  - *It adapts to playing both too slowly and too fast.* If the game can't keep up with real time, it will just take larger and larger time steps until it does.
  - *It makes gameplay non-deterministic and unstable.* And this is the real problem, of course. Physics and networking in particular become much harder with a variable time step.

- **可变时间步长：**
  我将其作为解决方案空间中的一个选项放在这里，但需要说明的是，我认识的大多数游戏开发者都建议不要使用它。不过，记住*为什么*它是一个坏主意是好的。
  - *它能适应运行得太慢和太快两种情况。* 如果游戏跟不上真实时间，它只会迈出越来越大的时间步长，直到跟上。
  - *它使游戏玩法变得非确定性和不稳定。* 当然，这是真正的问题。特别是物理和网络在可变时间步长下变得更加困难。

- **Fixed update time step, variable rendering:**
  The last option we covered in the sample code is the most complex, but also the most adaptable. It updates with a fixed time step, but it can drop *rendering* frames if it needs to to catch up to the player's clock.
  - *It adapts to playing both too slowly and too fast.* As long as the game can *update* in real time, the game won't fall behind. If the player's machine is top-of-the-line, it will respond with a smoother gameplay experience.
  - *It's more complex.* The main downside is there is a bit more going on in the implementation. You have to tune the update time step to be both as small as possible for the high-end, while not being too slow on the low end.

- **固定更新时间步长，可变渲染：**
  我们在示例代码中介绍的最后一个选项是最复杂的，但也是适应能力最强的。它以固定时间步长更新，但如果需要追赶玩家的时钟，它可以丢弃*渲染*帧。
  - *它能适应运行得太慢和太快两种情况。* 只要游戏能够实时*更新*，游戏就不会落后。如果玩家的机器是顶级的，它将带来更流畅的游戏体验。
  - *它更复杂。* 主要缺点是实现中有更多的事情要做。你必须调整更新时间步长，使其对高端设备尽可能小，同时在低端设备上又不会太慢。
---

## See Also / 参见

- The classic article on game loops is Glenn Fiedler's "[Fix Your Timestep](http://gafferongames.com/game-physics/fix-your-timestep/)". This chapter wouldn't be the same without it.
- Witters' article on [game loops](http://www.koonsolo.com/news/dewitters-gameloop/) is a close runner-up.
- The [Unity](http://unity3d.com/) framework has a complex game loop detailed in a wonderful illustration [here](http://docs.unity3d.com/Manual/ExecutionOrder.html).

- 关于游戏循环的经典文章是 Glenn Fiedler 的 "[Fix Your Timestep](http://gafferongames.com/game-physics/fix-your-timestep/)"。没有它，这一章将大为不同。
- Witters 关于[游戏循环](http://www.koonsolo.com/news/dewitters-gameloop/)的文章紧随其后。
- [Unity](http://unity3d.com/) 框架有一个复杂的游戏循环，在[这里](http://docs.unity3d.com/Manual/ExecutionOrder.html)有一幅精彩的图示。
---

## C++ Code (原书代码 — 完整汇总)

```cpp
// ============================================================
// 1. 最简单的游戏循环（不做速度控制）
// EN: The simplest possible game loop (no speed control)
// ============================================================
// while (true)
// {
//   processInput();
//   update();
//   render();
// }

// ============================================================
// 2. 固定时间步 + 睡眠（控制最快速度）
// EN: Fixed time step with sleep (cap max speed)
// ============================================================
// while (true)
// {
//   double start = getCurrentTime();
//   processInput();
//   update();
//   render();
//
//   sleep(start + MS_PER_FRAME - getCurrentTime());
// }

// ============================================================
// 3. 可变时间步（根据实际耗时调整更新量）
// EN: Variable time step (scale updates by elapsed time)
// ============================================================
// double lastTime = getCurrentTime();
// while (true)
// {
//   double current = getCurrentTime();
//   double elapsed = current - lastTime;
//   processInput();
//   update(elapsed);
//   render();
//   lastTime = current;
// }

// ============================================================
// 4. 固定更新 + 可变渲染（推荐方案）
// EN: Fixed update + variable render (recommended)
// ============================================================
// double previous = getCurrentTime();
// double lag = 0.0;
// while (true)
// {
//   double current = getCurrentTime();
//   double elapsed = current - previous;
//   previous = current;
//   lag += elapsed;
//
//   processInput();
//
//   while (lag >= MS_PER_UPDATE)
//   {
//     update();
//     lag -= MS_PER_UPDATE;
//   }
//
//   render(lag / MS_PER_UPDATE);
// }
```

## C# Equivalent (C# 对照实现)

```csharp
using System;
using System.Diagnostics;
using System.Threading;

/// <summary>
/// 完整的游戏循环实现 —— 从最基础到最推荐的固定更新 + 插值渲染
/// EN: Complete game loop implementations — from basic to the recommended
///     fixed update with interpolation
/// CN: 完整的游戏循环实现，展示了从最基础到最推荐的固定更新+插值渲染的演进
/// </summary>
public class GameLoop
{
    // ============================================================
    // 1. 最简单的游戏循环（仅作演示，不应实际使用）
    // EN: Simplest game loop (for demonstration only, not for real use)
    // CN: 最简单的游戏循环——理论上可以工作，但实际不可用。
    //     问题：在快的机器上游戏飞一样快，在慢的机器上像幻灯片。
    //     游戏速度完全受硬件性能影响，这是不可接受的。
    // ============================================================
    // public void RunBasic()
    // {
    //     while (true)
    //     {
    //         ProcessInput();
    //         Update();  // 每次更新固定增量
    //         Render();
    //     }
    // }

    // ============================================================
    // 2. 固定时间步 + 睡眠（控制上限速度）
    // EN: Fixed time step with sleep (cap maximum speed)
    // CN: 固定时间步 + 睡眠控制。
    //     思路：如果一帧处理得很快，就主动睡眠等待，保证不超过目标帧率。
    //     比如目标 60 FPS，每帧可用 16.67ms。
    //     如果处理只用了 5ms，就睡眠 11.67ms。
    //     缺点：如果处理时间超过 16.67ms，游戏会变慢，没有任何补偿机制。
    // ============================================================
    public void RunFixedWithSleep()
    {
        // 目标帧率 60 FPS，每帧分配 16.67 毫秒
        // EN: Target 60 FPS → 16.67ms per frame
        // CN: 目标 60 帧/秒，每帧分配时间 = 1000ms / 60 ≈ 16.67ms
        const double msPerFrame = 1000.0 / 60.0;

        while (true)
        {
            // 记录这一帧的开始时间
            // EN: Record the start time of this frame
            // CN: 用 Stopwatch 高精度计时器记录帧开始时间
            double start = Stopwatch.GetTimestamp() * 1000.0
                           / Stopwatch.Frequency;

            ProcessInput();
            Update();
            Render();

            // 计算这一帧用了多少时间
            // EN: Calculate how long this frame took
            // CN: 计算这一帧实际消耗的时间
            double elapsed = (Stopwatch.GetTimestamp() * 1000.0
                             / Stopwatch.Frequency) - start;

            // 如果处理得比预期快，就睡眠剩下的时间
            // EN: If we finished early, sleep for the remaining time
            // CN: 如果比预期快，就睡眠等待，把 CPU 让给其他进程，
            //     也节省电池（这对移动设备尤其重要）。
            double remaining = msPerFrame - elapsed;
            if (remaining > 0)
            {
                // Thread.Sleep 的精度约为 1ms，对游戏循环已经够用
                // EN: Thread.Sleep has ~1ms precision, good enough for game loops
                // CN: Thread.Sleep 精度约 1ms，对游戏循环足够
                Thread.Sleep((int)remaining);
            }

            // 注意：如果 elapsed > msPerFrame，即游戏处理太慢，
            // 这里不会做任何补偿——游戏会直接变慢。
            // EN: NOTE: If elapsed > msPerFrame (game is too slow),
            //     no compensation happens — the game just slows down.
            // CN: 注意：如果 elapsed > msPerFrame，即处理耗时超过分配时间，
            //     什么也不做——游戏直接变慢。这是此方案的根本缺陷。
        }
    }

    // ============================================================
    // 3. 可变时间步（根据实际耗时调整更新幅度）
    // EN: Variable time step (scale update by actual elapsed time)
    // CN: 可变时间步。
    //     思路：每次 update() 不再推进固定时间，而是传入实际过去了多少时间。
    //     比如子弹速度是 100 像素/秒，如果上一帧用了 0.033 秒，
    //     子弹就移动 3.3 像素；如果用了 0.1 秒，就移动 10 像素。
    //     优点：游戏在不同硬件上"速度"一致。
    //     缺点：物理引擎变得不稳定（浮点误差累积），联网时不同机器状态不同步。
    //     大多数游戏开发者不推荐此方案。
    // ============================================================
    public void RunVariableTimeStep()
    {
        // 用 Stopwatch 获取高精度时间
        // EN: Use Stopwatch for high-precision timing
        // CN: 使用高精度计时器
        var stopwatch = Stopwatch.StartNew();

        // 上一帧的时间点（以秒为单位）
        // EN: Timestamp of the previous frame (in seconds)
        // CN: 上一帧的时间点（单位为秒）
        double lastTime = stopwatch.Elapsed.TotalSeconds;

        while (true)
        {
            // 当前时间
            double current = stopwatch.Elapsed.TotalSeconds;
            // 自上一帧以来过去了多少秒
            // EN: How many seconds have passed since the last frame
            // CN: 计算自上一帧以来过去了多少秒 —— 这就是"可变"的 deltaTime
            double elapsed = current - lastTime;

            ProcessInput();

            // 把时间增量传入 update，让游戏逻辑根据实际经过的时间来推进
            // EN: Pass elapsed time to update so game logic advances proportionally
            // CN: 传入时间增量，让游戏逻辑按比例推进。
            //     例如移动速度 10 单位/秒，elapsed=0.5 → 移动 5 单位。
            Update(elapsed);

            Render();

            lastTime = current;
        }
    }

    // ============================================================
    // 4. [推荐] 固定更新步长 + 可变渲染 + 插值
    // EN: [RECOMMENDED] Fixed update step + variable render + interpolation
    // CN: 最推荐的方案：固定更新步长 + 可变渲染 + 插值。
    //     核心思想：
    //     - 物理/AI/逻辑 用固定步长更新（保证稳定性和确定性）
    //     - 渲染用可变帧率（硬件能跑多快就渲染多快）
    //     - 两个循环之间用 lag 累积时间来协调
    //     - 渲染时用插值因子让画面平滑
    // ============================================================
    public void RunFixedUpdateVariableRender()
    {
        // 固定更新步长：每帧推进 16.67ms 的游戏时间（对应 60 FPS 的更新率）
        // EN: Fixed update step: advance 16.67ms of game time per update
        // CN: 固定更新步长 = 16.67ms（60 次/秒的更新率）。
        //     这个值决定了物理/AI 的计算精度。
        //     通常比渲染帧率更高（如 60~120 次/秒），所以内层循环可能执行多次。
        const double msPerUpdate = 1000.0 / 60.0;

        var stopwatch = Stopwatch.StartNew();
        double previous = stopwatch.Elapsed.TotalMilliseconds;
        double lag = 0.0;  // 累积的"落后"时间

        while (true)
        {
            // 当前时间（毫秒）
            double current = stopwatch.Elapsed.TotalMilliseconds;
            // 自上一帧以来过去了多少毫秒
            double elapsed = current - previous;
            previous = current;

            // 把经过的时间加到 lag 中
            // EN: Add elapsed time to the lag accumulator
            // CN: 把经过的真实时间累加到 lag 中。
            //     lag 表示"游戏时间已落后真实时间多少"
            lag += elapsed;

            ProcessInput();

            // === 关键的内层循环：用固定步长追赶游戏时间 ===
            // EN: KEY inner loop: catch up game time using fixed steps
            // CN: 关键点：用固定的步长（msPerUpdate）追赶游戏时间。
            //     这个循环可能执行 0 次、1 次或多次，取决于 lag 的大小。
            //     这就是 Unity 的 FixedUpdate 做的事。
            while (lag >= msPerUpdate)
            {
                // 每次固定推进 msPerUpdate 的游戏时间
                // EN: Each call advances exactly msPerUpdate of game time
                // CN: 每次调用推进固定量的游戏时间，保证物理和 AI 的稳定性
                UpdateFixed();   // ← 对应 Unity 的 FixedUpdate
                lag -= msPerUpdate;
            }

            // 计算归一化的插值因子 [0, 1)，表示当前帧处于两次更新之间什么位置
            // EN: Calculate normalized interpolation factor [0, 1)
            // CN: 计算归一化的插值因子（范围 0 到略小于 1）。
            //     比如 lag=8ms, msPerUpdate=16.67ms → alpha=0.48，
            //     意味着当前渲染时间点位于上次更新和下次更新的 48% 位置。
            double alpha = lag / msPerUpdate;

            // 渲染时传入插值因子，用于平滑插值物体的位置
            // EN: Pass interpolation factor for smooth rendering
            // CN: 渲染时传入插值因子，让渲染器在两个更新状态之间做插值。
            //     例如：子弹上次在 x=10，这次在 x=20，alpha=0.5 → 渲染在 x=15。
            //     这样即使更新频率低于刷新率，画面看起来也是平滑的。
            Render(alpha);
        }
    }

    // ============ 辅助方法（模拟 Game 引擎接口） ============

    private void ProcessInput()
    {
        // 非阻塞处理所有用户输入事件
        // EN: Non-blocking processing of all user input events
        // CN: 非阻塞地处理所有用户输入事件。
        //     使用 PeekMessage（Windows）或 Input.GetAxis（Unity）等机制，
        //     绝不阻塞等待输入。
    }

    private void Update()
    {
        // 固定增量更新游戏逻辑（物理、AI、动画等）
        // EN: Fixed increment update for game logic (physics, AI, animation)
        // CN: 用固定增量更新游戏逻辑，每次推进相同的游戏时间
    }

    private void Update(double deltaTime)
    {
        // 可变增量更新 —— 根据传入的时间增量推进不同量的游戏时间
        // EN: Variable increment update — advance game time by the given amount
        // CN: 可变增量更新——根据传入的 deltaTime 推进不同量的游戏时间
    }

    private void UpdateFixed()
    {
        // 固定步长更新 —— 对应 Unity 的 FixedUpdate
        // EN: Fixed step update — corresponds to Unity's FixedUpdate
        // CN: 固定步长更新，对应 Unity 的 FixedUpdate。
        //     物理引擎、刚体运动等需要确定性的逻辑应在这里更新。
    }

    private void Render()
    {
        // 渲染当前帧
    }

    private void Render(double alpha)
    {
        // 使用插值因子 alpha 渲染平滑画面
    }
}
```

## Unity Application / Unity 应用场景

* Unity's execution order is a direct implementation of the recommended game loop pattern:

1. **`Update()`** — Called once per frame. Used for regular game logic (input handling, AI decisions, animation triggers). Frame rate dependent.
2. **`FixedUpdate()`** — Called at a fixed interval (default 0.02s = 50 times/sec). Used for physics — `Rigidbody` forces, movement, and any deterministic simulation. Frame rate independent.
3. **`LateUpdate()`** — Called after all `Update()` calls. Used for camera follow, procedural effects that must run after all other updates.
4. **`Start()` / `Awake()`** — Initialization hooks that run once.
5. **`OnEnable()` / `OnDisable()`** — Called when objects become active/inactive.

Unity's `Time.deltaTime` = time since last `Update` (variable). `Time.fixedDeltaTime` = fixed interval (configurable). `Time.timeScale` can pause all time-dependent logic.

**CN:** Unity 的执行顺序是推荐游戏循环模式的直接实现：
1. **`Update()`** — 每帧调用一次。用于常规游戏逻辑（输入处理、AI 决策、动画触发）。依赖帧率。
2. **`FixedUpdate()`** — 按固定间隔调用（默认 0.02s = 50 次/秒）。用于物理——`Rigidbody` 力、移动及任何需要确定性的模拟。不依赖帧率。
3. **`LateUpdate()`** — 在所有 `Update()` 之后调用。用于相机跟随、后处理效果等必须在其他更新之后运行的操作。
4. **`Start()` / `Awake()`** — 一次性初始化钩子。
5. **`OnEnable()` / `OnDisable()`** — 对象激活/停用时调用。

Unity 的 `Time.deltaTime` = 上次 Update 以来经过的时间（可变）。`Time.fixedDeltaTime` = 固定间隔（可配置）。`Time.timeScale` 可以暂停所有依赖时间逻辑。

## Key Differences / 关键差异

```
C++ (原书)                  | C# (Unity 实现)                | 说明
------------------------------------------------------------------------------------------------
getCurrentTime()            | Stopwatch / Time.realtimeSinceStartup | C# 使用托管计时器
sleep(start + MS - now)     | Thread.Sleep() / Application.targetFrameRate | Unity 用 targetFrameRate 限制帧率
update(elapsed)             | Update() + Time.deltaTime      | Unity 分离了 Update 和 FixedUpdate
内层 while(lag >= MS) 循环  | FixedUpdate 内部机制            | Unity 自动管理固定更新循环
render(lag / MS_PER_UPDATE) | 渲染管线自动处理                | Unity 的渲染由引擎管理
无                           | Time.timeScale = 0 暂停        | Unity 内置时间缩放控制
手动管理帧率                 | QualitySettings.vSyncCount     | Unity 提供垂直同步配置
```

---

# Update Method — 更新方法模式

> **EN:** Simulate a collection of independent objects by telling each to process one frame of behavior at a time.
> **CN:** 通过让每个对象每帧处理一帧的行为来模拟一组独立对象的并发运行。

---

## Intent / 意图

* Simulate a collection of independent objects by telling each to process one frame of behavior at a time.

* 通过让每个对象每帧处理一帧的行为来模拟一组独立对象的并发运行。

---

## Motivation / 动机

* The player's mighty valkyrie is on a quest to steal glorious jewels from where they rest on the bones of the long-dead sorcerer-king. She tentatively approaches the entrance of his magnificent crypt and is attacked by… *nothing*. No cursed statues shooting lightning at her. No undead warriors patrolling the entrance. She just walks right in and grabs the loot. Game over. You win.

Well, that won't do.

This crypt needs some guards — enemies our brave heroine can grapple with. First up, we want a re-animated skeleton warrior to patrol back and forth in front of the door. If you ignore everything you probably already know about game programming, the simplest possible code to make that skeleton lurch back and forth is something like:

**CN:** 玩家扮演的英勇女武神踏上了寻找宝藏的征程——这些闪耀的珠宝安放在早已死去的巫王骸骨之上。她小心翼翼地接近那座宏伟墓穴的入口，然后遭到了……*什么都没有*的攻击。没有被诅咒的雕像向她发射闪电。没有亡灵战士在入口处巡逻。她就这么走进去，拿走了宝物。游戏结束。你赢了。

这可不行。

这座墓穴需要一些守卫——一些我们的勇敢女主角能够与之搏斗的敌人。首先，我们想要一个复活的骷髅战士在门前巡逻。如果你忽略那些你可能已经知道的游戏编程知识，让这个骷髅来回走动的最简单代码大概是这样：

```cpp
while (true)
{
  // Patrol right.
  for (double x = 0; x < 100; x++)
  {
    skeleton.setX(x);
  }

  // Patrol left.
  for (double x = 100; x > 0; x--)
  {
    skeleton.setX(x);
  }
}
```

* If the sorcerer-king wanted more intelligent behavior, he should have re-animated something that still had brain tissue.

The problem here, of course, is that the skeleton moves back and forth, but the player never sees it. The program is locked in an infinite loop, which is not exactly a fun gameplay experience. What we actually want is for the skeleton to move one step *each frame*.

We'll have to remove those loops and rely on the outer game loop for iteration. That ensures the game keeps responding to user input and rendering while the guard is making his rounds. Like:

**CN:** 如果巫王想要更智能的行为，他应该复活一些还有脑组织的东西。

当然，问题在于骷髅确实在来回走动，但玩家永远看不到。程序陷入了一个无限循环，这显然不是一个有趣的游戏体验。我们真正想要的是让骷髅*每帧*移动一步。

我们必须移除那些循环，依赖外层的游戏循环来进行迭代。这确保了守卫巡逻的同时，游戏仍然能响应用户输入并进行渲染。就像这样：

```cpp
Entity skeleton;
bool patrollingLeft = false;
double x = 0;

// Main game loop:
while (true)
{
  if (patrollingLeft)
  {
    x--;
    if (x == 0) patrollingLeft = false;
  }
  else
  {
    x++;
    if (x == 100) patrollingLeft = true;
  }

  skeleton.setX(x);

  // Handle user input and render game...
}
```

* Naturally, [Game Loop](game-loop.html) is another pattern in this book.

I did the before/after here to show you how the code gets more complex. Patrolling left and right used to be two simple `for` loops. It kept track of which direction the skeleton was moving implicitly by which loop was executing. Now that we have to yield to the outer game loop each frame and then resume where we left off, we have to track the direction explicitly using that `patrollingLeft` variable.

But this more or less works, so we keep going. A brainless bag of bones doesn't give yon Norse maiden too much of a challenge, so the next thing we add is a couple of enchanted statues. These will fire bolts of lightning at her every so often to keep her on her toes.

Continuing our, "what's the simplest way to code this" style, we end up with:

**CN:** 当然，[游戏循环](game-loop.html)是本书中的另一个模式。

我在这里做了一个前后对比，来展示代码是如何变得更复杂的。向左和向右巡逻原本是两个简单的 `for` 循环。它通过当前正在执行哪个循环来隐式地追踪骷髅的移动方向。现在，由于我们必须每帧将控制权交还给外层游戏循环，然后在下一帧从中断处继续执行，我们不得不使用 `patrollingLeft` 变量来显式地追踪方向。

不过，这或多或少是可行的，所以我们继续。一个没有脑子的骨头袋子对我们的北欧少女来说算不上什么挑战，所以我们接下来要添加几座被施了魔法的雕像。它们会时不时地向她发射闪电，让她保持警觉。

继续我们"最简单的编码方式"风格，我们最终得到了这样的代码：

```cpp
// Skeleton variables...
Entity leftStatue;
Entity rightStatue;
int leftStatueFrames = 0;
int rightStatueFrames = 0;

// Main game loop:
while (true)
{
  // Skeleton code...

  if (++leftStatueFrames == 90)
  {
    leftStatueFrames = 0;
    leftStatue.shootLightning();
  }

  if (++rightStatueFrames == 80)
  {
    rightStatueFrames = 0;
    rightStatue.shootLightning();
  }

  // Handle user input and render game...
}
```

* You can tell this isn't trending towards code we'd enjoy maintaining. We've got an increasingly large pile of variables and imperative code all stuffed in the game loop, each handling one specific entity in the game. To get them all up and running at the same time, we've mushed their code together.

* 你可以看出这并非朝着我们乐于维护的代码方向发展。我们在游戏循环中塞入了越来越多变量和命令式代码，每一块都处理着游戏中的一个特定实体。为了让它们同时运行，我们把它们的代码全都混在了一起。

> **EN:** Anytime "mushed" accurately describes your architecture, you likely have a problem.
> **CN:** 任何时候，当"混在一起"准确描述了你的架构时，你很可能遇到了问题。

* The pattern we'll use to fix this is so simple you probably have it in mind already: *each entity in the game should encapsulate its own behavior.* This will keep the game loop uncluttered and make it easy to add and remove entities.

To do this, we need an *abstraction layer*, and we create that by defining an abstract `update()` method. The game loop maintains a collection of objects, but it doesn't know their concrete types. All it knows is that they can be updated. This separates each object's behavior both from the game loop and from the other objects.

Once per frame, the game loop walks the collection and calls `update()` on each object. This gives each one a chance to perform one frame's worth of behavior. By calling it on all objects every frame, they all behave simultaneously.

**CN:** 我们用来解决这个问题的模式非常简单，你可能已经想到了：*游戏中的每个实体都应该封装自己的行为。* 这将保持游戏循环的整洁，并使添加和移除实体变得容易。

为此，我们需要一个*抽象层*，我们通过定义一个抽象的 `update()` 方法来创建它。游戏循环维护着一个对象集合，但它不知道这些对象的具体类型。它只知道它们可以被更新。这将每个对象的行为与游戏循环以及其他对象分离开来。

每帧一次，游戏循环遍历这个集合并调用每个对象的 `update()` 方法。这给了每个对象执行一帧行为的机会。通过每帧对所有对象调用它，它们就能同时表现出行为。

> **EN:** Since some stickler will call me on this, yes, they don't behave *truly concurrently*. While one object is updating, none of the others are. We'll get into this more in a bit.
> **CN:** 有些较真的人会指出这一点，是的，它们的行为并不是*真正并发的*。当一个对象在更新时，其他对象都没有在更新。我们稍后会详细讨论这一点。

* The game loop has a dynamic collection of objects, so adding and removing them from the level is easy — just add and remove them from the collection. Nothing is hardcoded anymore, and we can even populate the level using some kind of data file, which is exactly what our level designers want.

* 游戏循环拥有一个动态的对象集合，因此向关卡中添加和移除对象变得很容易——只需从集合中添加和移除它们。不再有任何硬编码，我们甚至可以使用某种数据文件来填充关卡，这正是我们的关卡设计师想要的。

---

## The Pattern / 模式定义

* The **game world** maintains a **collection of objects**. Each object implements an **update method** that **simulates one frame** of the object's behavior. Each frame, the game updates every object in the collection.

* **游戏世界**维护一个**对象集合**。每个对象实现一个**更新方法**，**模拟该对象一帧**的行为。每帧，游戏更新集合中的每一个对象。

---

## When to Use It / 使用场景

* If the [Game Loop](game-loop.html) pattern is the best thing since sliced bread, then the Update Method pattern is its butter. A wide swath of games featuring live entities that the player interacts with use this pattern in some form or other. If the game has space marines, dragons, Martians, ghosts, or athletes, there's a good chance it uses this pattern.

However, if the game is more abstract and the moving pieces are less like living actors and more like pieces on a chessboard, this pattern is often a poor fit. In a game like chess, you don't need to simulate all of the pieces concurrently, and you probably don't need to tell the pawns to update themselves every frame.

You may not need to update their *behavior* each frame, but even in a board game, you may still want to update their *animation* every frame. This pattern can help with that too.

Update methods work well when:

- Your game has a number of objects or systems that need to run simultaneously.
- Each object's behavior is mostly independent of the others.
- The objects need to be simulated over time.

**CN:** 如果[游戏循环](game-loop.html)模式是切片面包以来最伟大的发明，那么更新方法模式就是它的黄油。广泛涉及玩家与之交互的活动实体的游戏，都以某种形式使用此模式。如果游戏中有太空陆战队、龙、火星人、鬼魂或运动员，那它很可能会使用这个模式。

然而，如果游戏更加抽象，其活动元素不像活生生的演员而更像棋盘上的棋子，那么这个模式通常就不太适合了。在像国际象棋这样的游戏中，你不需要同时模拟所有的棋子，你可能也不需要告诉兵每帧更新自己。

你可能不需要每帧更新它们的*行为*，但即使是在棋盘游戏中，你可能仍然希望每帧更新它们的*动画*。这个模式在这方面也能提供帮助。

更新方法在以下情况效果良好：

- 你的游戏有许多需要同时运行的对象或系统。
- 每个对象的行为大多独立于其他对象。
- 对象需要随时间进行模拟。

---

## Keep in Mind / 注意事项

### Splitting code into single frame slices makes it more complex / 将代码分割成单帧片段会增加复杂性

* When you compare the first two chunks of code, the second is a good bit more complex. Both simply make the skeleton guard walk back and forth, but the second one does this while yielding control to the game loop each frame.

That change is almost always necessary to handle user input, rendering, and the other stuff that the game loop takes care of, so the first example wasn't very practical. But it's worth keeping in mind that there's a big up front complexity cost when you julienne your behavioral code like this.

I say "almost" here because sometimes you can have your cake and eat it too. You can have straight-line code that never returns for your object behavior, while simultaneously having a number of objects running concurrently and coordinating with the game loop.

What you need is a system that lets you have multiple "threads" of execution going on at the same time. If the code for an object can pause and resume in the middle of what it's doing, instead of having to *return* completely, you can write it in a more imperative form.

Actual threads are usually too heavyweight for this to work well, but if your language supports lightweight concurrency constructs like generators, coroutines, or fibers, you may be able to use those.

The [Bytecode](bytecode.html) pattern is another option that creates threads of execution at the application level.

**CN:** 当你比较前两段代码时，第二段要复杂得多。两者都只是让骷髅守卫来回走动，但第二段在做到这一点的同时还要每帧将控制权交还给游戏循环。

为了处理用户输入、渲染以及游戏循环负责的其他事务，这种改变几乎是必须的，所以第一个例子并不太实用。但值得记住的是，当你像这样将行为代码"切丝"时，会有一个巨大的前期复杂度成本。

我在这里说"几乎"，是因为有时你可以鱼与熊掌兼得。你可以为你的对象行为编写从不返回的直线型代码，同时让多个对象并发运行并与游戏循环协调。

你需要的是一个允许你同时拥有多个执行"线程"的系统。如果一个对象的代码可以在执行过程中暂停和恢复，而不是必须*完全返回*，你就可以用更命令式的形式来编写它。

实际的线程通常过于重量级，无法很好地实现这一点，但如果你的语言支持轻量级的并发结构，如生成器、协程或纤程，你可能可以使用它们。

[字节码](bytecode.html)模式是另一种选择，它在应用层面创建执行线程。

### You have to store state to resume where you left off each frame / 必须保存状态以便每帧从中断处恢复

* In the first code sample, we didn't have any variables to indicate whether the guard was moving left or right. That was implicit based on which code was currently executing.

When we changed this to a one-frame-at-a-time form, we had to create a `patrollingLeft` variable to track that. When we return out of the code, the execution position is lost, so we need to explicitly store enough information to restore it on the next frame.

The [State](state.html) pattern can often help here. Part of the reason state machines are common in games is because (like their name implies) they store the kind of state that you need to pick up where you left off.

**CN:** 在第一个代码示例中，我们没有任何变量来指示守卫是向左还是向右移动。这一点是根据当前正在执行的代码隐式推断的。

当我们将其改为逐帧执行的形式时，我们必须创建一个 `patrollingLeft` 变量来追踪方向。当我们从代码中返回时，执行位置就丢失了，所以我们需要显式地存储足够的信息，以便在下一帧恢复它。

[状态](state.html)模式通常在这里很有帮助。状态机在游戏中很常见，部分原因就在于（如其名称所示）它们存储了你在中断后继续执行所需的那种状态。

### Objects all simulate each frame but are not truly concurrent / 所有对象每帧都在模拟，但并非真正并发

* In this pattern, the game loops over a collection of objects and updates each one. Inside the `update()` call, most objects are able to reach out and touch the rest of the game world, including other objects that are being updated. This means the *order* in which the objects are updated is significant.

If A comes before B in the list of objects, then when A updates, it will see B's previous state. But when B updates, it will see A's *new* state, since A has already been updated this frame. Even though from the player's perspective, everything is moving at the same time, the core of the game is still turn-based. It's just that a complete "turn" is only one frame long.

If, for some reason, you decide you *don't* want your game to be sequential like this, you would need to use something like the [Double Buffer](double-buffer.html) pattern. That makes the order in which A and B update not matter because *both* of them will see the previous frame's state.

This is mostly a good thing as far as the game logic is concerned. Updating objects in parallel leads you to some unpleasant semantic corners. Imagine a game of chess where black and white moved at the same time. They both try to make a move that places a piece in the same currently empty square. How should this be resolved?

Updating sequentially solves this — each update incrementally changes the world from one valid state to the next with no period of time where things are ambiguous and need to be reconciled.

It also helps online play since you have a serialized set of moves that can be sent over the network.

**CN:** 在这个模式中，游戏循环遍历一个对象集合并更新每一个。在 `update()` 调用内部，大多数对象能够触及游戏世界的其他部分，包括其他正在被更新的对象。这意味着对象被更新的*顺序*很重要。

如果 A 在对象列表中排在 B 前面，那么当 A 更新时，它将看到 B 之前的状态。但当 B 更新时，它将看到 A 的*新*状态，因为 A 在本帧已经更新过了。即使从玩家的角度来看，一切都在同时移动，游戏的核心仍然是回合制的。只不过一个完整的"回合"只有一帧那么长。

如果出于某种原因，你决定*不*希望你的游戏像这样顺序执行，你需要使用像[双缓冲](double-buffer.html)这样的模式。这会使 A 和 B 的更新顺序不再重要，因为*两者*都将看到上一帧的状态。

就游戏逻辑而言，这大体上是一件好事。并行更新对象会把你带入一些不愉快的语义角落。想象一下国际象棋中黑白双方同时移动的情况。它们都试图将一个棋子放到同一个当前为空的格子里。这该如何解决呢？

顺序更新解决了这个问题——每次更新都将世界从一个有效状态递增地改变到下一个有效状态，不存在任何需要协调的模糊时段。

这也对在线游戏有帮助，因为你拥有一系列可以序列化并通过网络发送的操作。

### Be careful modifying the object list while updating / 在更新时修改对象列表需谨慎

* When you're using this pattern, a lot of the game's behavior ends up nestled in these update methods. That often includes code that adds or removes updatable objects from the game.

For example, say a skeleton guard drops an item when slain. With a new object, you can usually add it to the end of the list without too much trouble. You'll keep iterating over that list and eventually get to the new one at the end and update it too.

But that does mean that the new object gets a chance to act during the frame that it was spawned, before the player has even had a chance to see it. If you don't want that to happen, one simple fix is to cache the number of objects in the list at the beginning of the update loop and only update that many before stopping:

**CN:** 当你使用这个模式时，游戏的很多行为最终都嵌套在这些更新方法中。这通常包括从游戏中添加或移除可更新对象的代码。

例如，假设一个骷髅守卫被击杀时会掉落一个物品。对于新对象，你通常可以将其添加到列表末尾而不太麻烦。你会继续遍历该列表，最终到达末尾的新对象并也更新它。

但这确实意味着新对象有机会在被生成的同一帧内行动，而玩家甚至还没有机会看到它。如果你不希望发生这种情况，一个简单的修复方法是在更新循环开始时缓存列表中的对象数量，并且只更新那么多：

```cpp
int numObjectsThisTurn = numObjects_;
for (int i = 0; i < numObjectsThisTurn; i++)
{
  objects_[i]->update();
}
```

* Here, `objects_` is an array of the updatable objects in the game, and `numObjects_` is its length. When new objects are added, it gets incremented. We cache the length in `numObjectsThisTurn` at the beginning of the loop so that the iteration stops before we get to any new objects added during the current frame.

A hairier problem is when objects are *removed* while iterating. You vanquish some foul beast and now it needs to get yanked out of the object list. If it happens to be before the current object you're updating in the list, you can accidentally skip an object:

**CN:** 这里，`objects_` 是游戏中可更新对象的数组，`numObjects_` 是其长度。当新对象被添加时，它会增加。我们在循环开始时将长度缓存到 `numObjectsThisTurn` 中，这样迭代会在到达当前帧期间添加的任何新对象之前停止。

一个更棘手的问题是当对象在迭代过程中被*移除*时。你击败了一些邪恶的野兽，现在它需要从对象列表中移除。如果它恰好位于你正在更新的当前对象之前，你可能会意外地跳过一个对象：

```cpp
for (int i = 0; i < numObjects_; i++)
{
  objects_[i]->update();
}
```

* This simple loop increments the index of the object being updated each iteration. The left side of the illustration below shows what the array looks like while we're updating the heroine:

Since we're updating her, `i` is 1. She slays the foul beast so it gets removed from the array. The heroine shifts up to 0, and the hapless peasant shifts up to 1. After updating the heroine, `i` is incremented to 2. As you can see on the right, the hapless peasant is skipped over and never gets updated.

A cheap solution is to walk the list *backwards* when you update. That way removing an object only shifts items that were already updated.

**CN:** 这个简单的循环每次迭代都会递增正在更新的对象的索引。下面的左侧插图显示了我们在更新女主角时数组的样子：

由于我们正在更新她，`i` 为 1。她杀死了邪恶的野兽，所以它从数组中被移除。女主角上移到 0，不幸的农民上移到 1。更新女主角后，`i` 递增到 2。如右侧所示，不幸的农民被跳过了，从未被更新。

一个廉价的解决方案是在更新时*反向*遍历列表。这样，移除一个对象只会移动已经更新过的项目。

> **EN:** One fix is to just be careful when you remove objects and update any iteration variables to take the removal into account. Another is to defer removals until you're done walking the list. Mark the object as "dead", but leave it in place. During updating, make sure to skip any dead objects. Then, when that's done, walk the list again to remove the corpses.
>
> If you have multiple threads processing the items in the update loop, then you are even more likely to defer any modification to it to avoid costly thread synchronization during updates.

> **CN:** 一种解决方法是，在移除对象时小心处理，并更新所有迭代变量以考虑移除的影响。另一种方法是将移除推迟到遍历完列表之后。将对象标记为"死亡"，但将其留在原地。在更新过程中，确保跳过任何死亡的对象。然后，当完成更新后，再次遍历列表以移除尸体。
>
> 如果你有多个线程处理更新循环中的项目，那么你更有可能推迟对它的任何修改，以避免更新期间代价高昂的线程同步。

---

## Sample Code / 示例代码

* This pattern is so straightforward that the sample code almost belabors the point. That doesn't mean the pattern isn't *useful*. It's useful in part *because* it's simple: it's a clean solution to a problem without a lot of ornamentation.

But to keep things concrete, let's walk through a basic implementation. We'll start with an `Entity` class that will represent the skeletons and statues:

**CN:** 这个模式如此直接，以至于示例代码几乎是在赘述要点。这并不意味着这个模式不*有用*。它之所以有用，部分原因正是它的简单：它是一个问题的简洁解决方案，没有太多修饰。

但为了具体起见，让我们走一遍基本实现。我们将从一个 `Entity` 类开始，它代表骷髅和雕像：

```cpp
class Entity
{
public:
  Entity()
  : x_(0), y_(0)
  {}

  virtual ~Entity() {}
  virtual void update() = 0;

  double x() const { return x_; }
  double y() const { return y_; }

  void setX(double x) { x_ = x; }
  void setY(double y) { y_ = y; }

private:
  double x_;
  double y_;
};
```

* I stuck a few things in there, but just the bare minimum we'll need later. Presumably in real code, there'd be lots of other stuff like graphics and physics. The important bit for this pattern is that it has an abstract `update()` method.

The game maintains a collection of these entities. In our sample, we'll put that in a class representing the game world:

**CN:** 我在里面塞了一些东西，但仅仅是后续需要的最低限度。可以想象，在真实代码中，还会有很多其他东西，比如图形和物理。对这个模式来说，重要的是它有一个抽象的 `update()` 方法。

游戏维护着这些实体的集合。在我们的示例中，我们将把它放在一个代表游戏世界的类中：

```cpp
class World
{
public:
  World()
  : numEntities_(0)
  {}

  void gameLoop();

private:
  Entity* entities_[MAX_ENTITIES];
  int numEntities_;
};
```

* In a real-world program, you'd probably use an actual collection class, but I'm just using a vanilla array here to keep things simple.

Now that everything is set up, the game implements the pattern by updating each entity every frame:

**CN:** 在真实世界的程序中，你可能会使用一个真正的集合类，但为了简单起见，我在这里只使用一个普通的数组。

现在一切都设置好了，游戏通过每帧更新每个实体来实现这个模式：

```cpp
void World::gameLoop()
{
  while (true)
  {
    // Handle user input...

    // Update each entity.
    for (int i = 0; i < numEntities_; i++)
    {
      entities_[i]->update();
    }

    // Physics and rendering...
  }
}
```

* As the name of the method implies, this is an example of the [Game Loop](game-loop.html) pattern.

* 正如方法名所暗示的，这是[游戏循环](game-loop.html)模式的一个示例。

### Subclassing entities?! / 子类化实体？！

* There are some readers whose skin is crawling right now because I'm using inheritance on the main `Entity` class to define different behaviors. If you don't happen to see the problem, I'll provide some context.

When the game industry emerged from the primordial seas of 6502 assembly code and VBLANKs onto the shores of object-oriented languages, developers went into a software architecture fad frenzy. One of the biggest was using inheritance. Towering, Byzantine class hierarchies were built, big enough to blot out the sun.

It turns out that was a terrible idea and no one can maintain a giant class hierarchy without it crumbling around them. Even the Gang of Four knew this in 1994 when they wrote:

> Favor 'object composition' over 'class inheritance'.

Between you and me, I think the pendulum has swung a bit too far *away* from subclassing. I generally avoid it, but being dogmatic about *not* using inheritance is as bad as being dogmatic about using it. You can use it in moderation without having to be a teetotaler.

**CN:** 有些读者现在可能感到不适，因为我正在使用 `Entity` 主类的继承来定义不同的行为。如果你碰巧没有看出问题，我来提供一些背景。

当游戏产业从 6502 汇编代码和 VBLANK 的原始海洋中涌现到面向对象语言的海岸上时，开发者们陷入了一场软件架构的时尚狂热。其中最大的一个就是使用继承。建造了高耸入云、拜占庭式的类继承层次，大到足以遮蔽太阳。

事实证明这是一个糟糕的主意，没有人能够维护一个庞大的类层次而不使其在周围崩塌。就连四人帮在 1994 年就知道这一点，当时他们写道：

> 优先使用"对象组合"而非"类继承"。

你我之间说说，我认为钟摆摆得有点太*远离*子类化了。我通常避免它，但对*不使用*继承教条主义与对使用它教条主义一样糟糕。你可以适度使用它，而不必成为一个禁欲者。

* When this realization percolated through the game industry, the solution that emerged was the [Component](component.html) pattern. Using that, `update()` would be on the entity's *components* and not on `Entity` itself. That lets you avoid creating complicated class hierarchies of entities to define and reuse behavior. Instead, you just mix and match components.

If I were making a real game, I'd probably do that too. But this chapter isn't about components. It's about `update()` methods, and the simplest way I can show them, with as few moving parts as possible, is by putting that method right on `Entity` and making a few subclasses.

**CN:** 当这种认识渗透到游戏行业时，出现的解决方案是[组件](component.html)模式。使用它，`update()` 将位于实体的*组件*上，而不是 `Entity` 本身。这让你避免创建复杂的实体类层次结构来定义和复用行为。相反，你只需混合搭配组件。

如果我正在制作一个真正的游戏，我可能也会那样做。但本章不是关于组件的。而是关于 `update()` 方法的，我展示它们的最简单方式，尽可能减少活动部件，就是将这个方法直接放在 `Entity` 上并创建几个子类。

### Defining entities / 定义实体

* OK, back to the task at hand. Our original motivation was to be able to define a patrolling skeleton guard and some lightning-bolt-unleashing magical statues. Let's start with our bony friend. To define his patrolling behavior, we make a new entity that implements `update()` appropriately:

* 好的，回到手头的任务。我们最初的动机是能够定义一个巡逻的骷髅守卫和几个发射闪电的魔法雕像。让我们从我们的骨头朋友开始。为了定义它的巡逻行为，我们创建一个新的实体，适当地实现 `update()`：

```cpp
class Skeleton : public Entity
{
public:
  Skeleton()
  : patrollingLeft_(false)
  {}

  virtual void update()
  {
    if (patrollingLeft_)
    {
      setX(x() - 1);
      if (x() == 0) patrollingLeft_ = false;
    }
    else
    {
      setX(x() + 1);
      if (x() == 100) patrollingLeft_ = true;
    }
  }

private:
  bool patrollingLeft_;
};
```

* As you can see, we pretty much just cut that chunk of code from the game loop earlier in the chapter and pasted it into `Skeleton`'s `update()` method. The one minor difference is that `patrollingLeft_` has been made into a field instead of a local variable. That way, its value sticks around between calls to `update()`.

Let's do this again with the statue:

**CN:** 如你所见，我们基本上就是把本章前面游戏循环中的那段代码剪切下来，粘贴到了 `Skeleton` 的 `update()` 方法中。唯一的小区别是 `patrollingLeft_` 已经变成了一个字段而不是局部变量。这样，它的值在 `update()` 调用之间得以保持。

让我们再来做雕像：

```cpp
class Statue : public Entity
{
public:
  Statue(int delay)
  : frames_(0),
    delay_(delay)
  {}

  virtual void update()
  {
    if (++frames_ == delay_)
    {
      shootLightning();

      // Reset the timer.
      frames_ = 0;
    }
  }

private:
  int frames_;
  int delay_;

  void shootLightning()
  {
    // Shoot the lightning...
  }
};
```

* Again, most of the change is moving code from the game loop into the class and renaming some stuff. In this case, though, we've actually made the codebase simpler. In the original nasty imperative code, there were separate local variables for each statue's frame counter and rate of fire.

Now that those have been moved into the `Statue` class itself, you can create as many as you want and each instance will have its own little timer. That's really the motivation behind this pattern — it's now much easier to add new entities to the game world because each one brings along everything it needs to take care of itself.

This pattern lets us separate *populating* the game world from *implementing* it. This in turn gives us the flexibility to populate the world using something like a separate data file or level editor.

**CN:** 同样，大部分变化就是将代码从游戏循环移到类中，并重命名一些东西。但在这个例子中，我们实际上使代码库变得更简单了。在原来糟糕的命令式代码中，每个雕像的帧计数器和发射速率都有各自的局部变量。

现在这些已经被移到了 `Statue` 类本身中，你可以创建任意数量的雕像，每个实例都将拥有自己的小计时器。这才是这个模式背后的真正动机——现在向游戏世界添加新实体变得容易多了，因为每个实体都带来了它自我管理所需的一切。

这个模式让我们将*填充*游戏世界与*实现*游戏世界分离开来。这反过来又使我们可以灵活地使用单独的数据文件或关卡编辑器来填充世界。

### Passing time / 传递时间

* That's the key pattern, but I'll just touch on a common refinement. So far, we've assumed every call to `update()` advances the state of the game world by the same fixed unit of time.

I happen to prefer that, but many games use a *variable time step*. In those, each turn of the game loop may simulate a larger or smaller slice of time depending on how long it took to process and render the previous frame.

That means that each `update()` call needs to know how far the hand of the virtual clock has swung, so you'll often see the elapsed time passed in. For example, we can make our patrolling skeleton handle a variable time step like so:

**CN:** 以上就是关键的模式，但我还想简单提一个常见的改进。到目前为止，我们假设每次调用 `update()` 都会将游戏世界的状态推进相同的固定时间单位。

我碰巧更喜欢这样，但许多游戏使用*可变时间步长*。在这些游戏中，游戏循环的每一轮可能模拟更大或更小的时间片，取决于处理并渲染上一帧花费了多长时间。

这意味着每个 `update()` 调用都需要知道虚拟时钟的指针走了多远，所以你会经常看到经过的时间被传入。例如，我们可以让巡逻的骷髅处理可变时间步长，像这样：

```cpp
void Skeleton::update(double elapsed)
{
  if (patrollingLeft_)
  {
    x -= elapsed;
    if (x <= 0)
    {
      patrollingLeft_ = false;
      x = -x;
    }
  }
  else
  {
    x += elapsed;
    if (x >= 100)
    {
      patrollingLeft_ = true;
      x = 100 - (x - 100);
    }
  }
}
```

* Now, the distance the skeleton moves increases as the elapsed time grows. You can also see the additional complexity of dealing with a variable time step. The skeleton may overshoot the bounds of its patrol with a large time slice, and we have to handle that carefully.

* 现在，骷髅移动的距离随着经过的时间增长而增加。你也可以看到处理可变时间步长带来的额外复杂性。骷髅可能会因为一个大的时间片而越过巡逻边界，我们必须小心处理。

---

## Design Decisions / 设计决策

* With a simple pattern like this, there isn't too much variation, but there are still a couple of knobs you can turn.

* 对于这样一个简单的模式，没有太多的变体，但仍有几个可以调节的旋钮。

### What class does the update method live on? / 更新方法应放在哪个类中？

* The most obvious and most important decision you'll make is what class to put `update()` on.

* 你将做出的最明显也是最重要的决定是将 `update()` 放在哪个类上。

- **The entity class:**

  * This is the simplest option if you already have an entity class since it doesn't bring any additional classes into play. This may work if you don't have too many kinds of entities, but the industry is generally moving away from this.

  Having to subclass `Entity` every time you want a new behavior is brittle and painful when you have a large number of different kinds. You'll eventually find yourself wanting to reuse pieces of code in a way that doesn't gracefully map to a single inheritance hierarchy, and then you're stuck.

  **CN:** 如果你已经有一个实体类，这是最简单的选择，因为它不需要引入任何额外的类。如果你没有太多类型的实体，这可能奏效，但业界总体上正在远离这种做法。

  每次想要新行为时都必须子类化 `Entity`，当有大量不同类型时，是脆弱且痛苦的。你最终会发现想要以某种方式重用代码片段，而这种方式无法优雅地映射到单一继承层次结构，然后你就被困住了。

- **The component class:**

  * If you're already using the [Component](component.html) pattern, this is a no-brainer. It lets each component update itself independently. In the same way that the Update Method pattern in general lets you decouple game entities from each other in the game world, this lets you decouple *parts of a single entity* from each other. Rendering, physics, and AI can all take care of themselves.

  * 如果你已经在使用[组件](component.html)模式，这是显而易见的。它让每个组件独立更新自己。就像更新方法模式总体上让你在游戏世界中解耦游戏实体一样，这让你解耦*单个实体的各个部分*。渲染、物理和 AI 都可以自我管理。

- **A delegate class:**

  * There are other patterns that involve delegating part of a class's behavior to another object. The [State](state.html) pattern does this so that you can change an object's behavior by changing what it delegates to. The [Type Object](type-object.html) pattern does this so that you can share behavior across a bunch of entities of the same "kind".

  If you're using one of those patterns, it's natural to put `update()` on that delegated class. In that case, you may still have the `update()` method on the main class, but it will be non-virtual and will simply forward to the delegated object. Something like:

  **CN:** 还有其他模式涉及将类的一部分行为委托给另一个对象。[状态](state.html)模式就是这样做的，这样你可以通过更改委托对象来改变对象的行为。[类型对象](type-object.html)模式也是这样做，这样你可以在一堆同一"种类"的实体之间共享行为。

  如果你正在使用这些模式之一，很自然会将 `update()` 放在被委托的类上。在这种情况下，你可能仍然在主类上有 `update()` 方法，但它将是非虚的，只是简单地转发给委托对象。像这样：

```cpp
void Entity::update()
{
  // Forward to state object.
  state_->update();
}
```

  * Doing this lets you define new behavior by changing out the delegated object. Like using components, it gives you the flexibility to change behavior without having to define an entirely new subclass.

  * 这样做让你通过更换委托对象来定义新行为。就像使用组件一样，它给了你改变行为的灵活性，而不必定义全新的子类。

### How are dormant objects handled? / 如何处理休眠对象？

* You often have a number of objects in the world that, for whatever reason, temporarily don't need to be updated. They could be disabled, or off-screen, or not unlocked yet. If a large number of objects are in this state, it can be a waste of CPU cycles to walk over them each frame only to do nothing.

One alternative is to maintain a separate collection of just the "live" objects that do need updating. When an object is disabled, it's removed from the collection. When it gets re-enabled, it's added back. This way, you only iterate over items that actually have real work do to.

**CN:** 你经常会有一些世界中的对象，由于某种原因，暂时不需要更新。它们可能被禁用、在屏幕外或尚未解锁。如果有大量对象处于这种状态，每帧遍历它们却什么也不做，可能会浪费 CPU 周期。

一种替代方案是维护一个单独的"活跃"对象集合，这些对象确实需要更新。当一个对象被禁用时，它被从集合中移除。当它被重新启用时，它被加回来。这样，你只遍历那些实际上有真正工作要做的项目。

- **If you use a single collection containing inactive objects:**

  - *You waste time.* For inactive objects, you'll end up either checking some "am I enabled" flag or calling a method that does nothing.

    In addition to wasted CPU cycles checking if the object is enabled and skipping past it, pointlessly iterating over objects can blow your data cache. CPUs optimize reads by loading memory from RAM into much faster on-chip caches. They do this speculatively by assuming you're likely to read memory right after a location you just read.

    When you skip over an object, you can skip past the end of the cache, forcing it to go and slowly pull in another chunk of main memory.

  **CN:** **如果你使用包含非活跃对象的单个集合：**

  - *你会浪费时间。* 对于非活跃对象，你最终要么检查某个"我是否启用"的标志，要么调用一个什么都不做的方法。

    除了浪费 CPU 周期检查对象是否启用并跳过它之外，无意义地遍历对象可能会破坏你的数据缓存。CPU 通过将内存从 RAM 加载到更快的片上缓存来优化读取。它们通过假设你可能会在刚刚读取的位置之后立即读取内存来进行推测性加载。

    当你跳过一个对象时，你可能会跳过缓存的末尾，迫使它去缓慢地拉入另一块主内存。

- **If you use a separate collection of only active objects:**

  - *You use extra memory to maintain the second collection.* There's still usually another master collection of all entities for cases where you need them all. In that case, this collection is technically redundant. When speed is tighter than memory (which it often is), this can still be a worthwhile trade-off.

    Another option to mitigate this is to have two collections, but have the other collection only contain the *inactive* entities instead of all of them.

  - *You have to keep the collections in sync.* When objects are created or completely destroyed (and not just made temporarily inactive), you have to remember to modify both the master collection and active object one.

  **CN:** **如果你使用仅包含活跃对象的单独集合：**

  - *你会使用额外内存来维护第二个集合。* 通常仍然有另一个所有实体的主集合，以备你需要所有实体的情况。在这种情况下，这个集合在技术上是冗余的。当速度比内存更紧张时（通常是这种情况），这仍然是一个值得的权衡。

    缓解这个问题的另一个选项是拥有两个集合，但让另一个集合只包含*非活跃*实体而不是全部。

  - *你必须保持集合同步。* 当对象被创建或完全销毁时（而不仅仅是暂时非活跃），你必须记得同时修改主集合和活跃对象集合。

* The metric that should guide your approach here is how many inactive objects you tend to have. The more you have, the more useful it is to have a separate collection that avoids them during your core game loop.

* 指导你选择方法的衡量标准是你倾向于拥有多少非活跃对象。你拥有的越多，拥有一个单独的集合来在核心游戏循环期间避开它们就越有用。

---

## See Also / 参见

- This pattern, along with [Game Loop](game-loop.html) and [Component](component.html), is part of a trinity that often forms the nucleus of a game engine.
- When you start caring about the cache performance of updating a bunch of entities or components in a loop each frame, the [Data Locality](data-locality.html) pattern can help make that faster.
- The [Unity](http://unity3d.com) framework uses this pattern in several classes, including [`MonoBehaviour`](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Update.html).
- Microsoft's [XNA](http://creators.xna.com/en-US/) platform uses this pattern both in the [`Game`](http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.game.update.aspx) and [`GameComponent`](http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.gamecomponent.update.aspx) classes.
- The [Quintus](http://html5quintus.com/) JavaScript game engine uses this pattern on its main [`Sprite`](http://html5quintus.com/guide/sprites.md) class.

- 此模式与[游戏循环](game-loop.html)和[组件](component.html)模式一起，构成了通常作为游戏引擎核心的三位一体。
- 当你开始关心每帧循环更新大量实体或组件的缓存性能时，[数据局部性](data-locality.html)模式可以帮助提高速度。
- [Unity](http://unity3d.com) 框架在多个类中使用此模式，包括 [`MonoBehaviour`](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Update.html)。
- 微软的 [XNA](http://creators.xna.com/en-US/) 平台在 [`Game`](http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.game.update.aspx) 和 [`GameComponent`](http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.gamecomponent.update.aspx) 类中都使用了此模式。
- [Quintus](http://html5quintus.com/) JavaScript 游戏引擎在其主要的 [`Sprite`](http://html5quintus.com/guide/sprites.md) 类上使用此模式。

---

## C# Equivalent (C# 对照实现)

```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// 所有游戏实体的抽象基类 —— 定义 update() 接口
/// EN: Abstract base class for all game entities — defines the update() interface
/// CN: 所有游戏实体的抽象基类。
///     关键设计：把"每帧要做什么"的决定权交给每个实体自己，
///     而不是由游戏循环去判断。这就是"封装行为"的本质。
/// </summary>
public abstract class Entity
{
    // 实体的位置坐标
    // EN: Entity's position in the world
    // CN: 实体的世界坐标位置
    public double X { get; protected set; }
    public double Y { get; protected set; }

    /// <summary>
    /// 更新方法 —— 每个实体每帧调用一次
    /// EN: Update method — called once per frame for each entity
    /// CN: 更新方法——每帧调用一次，模拟该实体一帧的行为。
    ///     这是整个模式的核心：子类通过覆写此方法定义自己的行为，
    ///     游戏循环完全不需要知道具体子类是什么。
    /// </summary>
    /// <param name="deltaTime">
    /// 距离上一帧经过的时间（秒）
    /// EN: Time elapsed since last frame (seconds)
    /// CN: 距离上一帧经过的时间（秒）。
    ///     如果使用固定时间步，deltaTime 是常量。
    ///     如果使用可变时间步，deltaTime 随帧耗时变化。
    /// </param>
    public abstract void Update(double deltaTime);
}

/// <summary>
/// 骷髅守卫 —— 每帧移动一步，在 0~100 之间来回巡逻
/// EN: Skeleton guard — moves one step per frame, patrols between 0 and 100
/// CN: 骷髅守卫——最简单的 AI 行为：在 x 轴 0~100 之间来回巡逻。
///     每帧通过 Update() 移动一点，由游戏循环驱动。
/// </summary>
public class Skeleton : Entity
{
    // 记住当前巡逻方向
    // EN: Remember the current patrol direction
    // CN: 记住当前巡逻方向。
    //     由于每次 Update() 结束后函数就返回了，
    //     必须用字段来保存状态，下次调用时恢复。
    //     这就是"每帧分片执行"带来的复杂性——需要显式保存中间状态。
    private bool patrollingLeft;

    public Skeleton()
    {
        X = 50;  // 从中间位置开始巡逻
    }

    public override void Update(double deltaTime)
    {
        // 根据巡逻方向移动
        // EN: Move based on patrol direction
        // CN: 根据巡逻方向决定移动方向。
        //     如果使用可变时间步（deltaTime 变化），
        //     要乘以 deltaTime 保证不同帧率下移动速度一致。
        if (patrollingLeft)
        {
            // 向左移动
            // EN: Move left
            // CN: 向左移动，速度乘以 deltaTime 保证帧率无关
            X -= 50 * deltaTime;  // 速度 50 单位/秒

            // 到达左边界时掉头
            // EN: Reverse direction at left boundary
            // CN: 到达左边界（x=0）时反转巡逻方向
            if (X <= 0)
            {
                patrollingLeft = false;
            }
        }
        else
        {
            // 向右移动
            // EN: Move right
            // CN: 向右移动
            X += 50 * deltaTime;

            // 到达右边界时掉头
            // EN: Reverse direction at right boundary
            // CN: 到达右边界（x=100）时反转巡逻方向
            if (X >= 100)
            {
                patrollingLeft = true;
            }
        }

        // 注意：deltaTime 大时可能超过边界，
        // 实际项目中需要更精细的边界处理。
        // EN: NOTE: With large deltaTime, the entity may overshoot.
        //     Real projects need more precise boundary handling.
        // CN: 注意：当 deltaTime 较大时（如掉帧），
        //     实体可能大幅越过边界，需要 clamp 或反弹处理。
    }
}

/// <summary>
/// 魔法雕像 —— 每帧计数，到达延迟帧数后发射闪电并重置
/// EN: Magic statue — counts frames, shoots lightning at interval, resets
/// CN: 魔法雕像——使用帧计数器实现周期性行为。
///     每帧 Update() 增加计数器，到达预设延迟后发射闪电并重置。
///     展示了 Update 方法如何实现定时触发的行为。
/// </summary>
public class Statue : Entity
{
    // 帧计数器
    // EN: Frame counter
    // CN: 帧计数器，从 0 开始计数，每帧增加
    private int frameCounter;

    // 发射间隔（帧数）
    // EN: Fire interval (in frames)
    // CN: 发射间隔（以帧为单位），每个雕像可以有不同延迟
    private readonly int fireDelay;

    public Statue(int delay)
    {
        fireDelay = delay;
        frameCounter = 0;

        // 每个雕像可以放在不同位置
        // EN: Each statue can be placed at a different position
        // CN: 每个雕像可以有不同的位置和发射频率
        X = 0;
        Y = 0;
    }

    public override void Update(double deltaTime)
    {
        // 每帧计数器加 1
        // EN: Increment frame counter each update
        // CN: 每帧计数器加 1，当达到发射间隔时触发闪电
        frameCounter++;

        if (frameCounter >= fireDelay)
        {
            ShootLightning();
            // 发射后重置计数器，等待下一个周期
            // EN: Reset after firing, wait for next cycle
            // CN: 发射后重置计数器，等待下一个周期
            frameCounter = 0;
        }
    }

    /// <summary>
    /// 发射闪电 —— 用虚拟方法封装具体效果
    /// EN: Shoot lightning — virtual method encapsulates specific effects
    /// CN: 发射闪电——子类可覆写此方法实现不同的视觉效果或伤害计算
    /// </summary>
    protected virtual void ShootLightning()
    {
        // 实际项目中这里会实例化闪电特效预制体、检测碰撞等
        // EN: In a real project, this would instantiate lightning VFX, detect collisions, etc.
        // CN: 实际项目中这里会实例化闪电特效预制体、检测碰撞等
        Console.WriteLine($"Statue at ({X}, {Y}) shoots lightning!");
    }
}

// ============================================================
// 游戏世界的 Update 循环 —— 这是 Unity 的 MonoBehaviour 的底层机制
// EN: Game world update loop — this is the underlying mechanism of Unity's MonoBehaviour
// CN: 游戏世界的更新循环——这就是 Unity 的 MonoBehaviour 的底层机制。
//     Unity 引擎内部维护了一个场景中所有 MonoBehaviour 的列表，
//     每帧按顺序调用每个脚本的 Update() 方法。
// ============================================================

/// <summary>
/// 游戏世界管理器 —— 管理和更新所有实体
/// EN: Game world manager — manages and updates all entities
/// CN: 游戏世界管理器，持有所有实体的列表，每帧更新它们。
///     这就是 Unity 的 Scene 和 MonoBehaviour 管理器的工作原理。
/// </summary>
public class GameWorld
{
    // 所有活跃实体的列表
    // EN: List of all active entities
    // CN: 所有活跃实体的列表。在 Unity 中，这就是场景中
    //     所有激活的 MonoBehaviour 的内部列表。
    private readonly List<Entity> entities = new List<Entity>();

    /// <summary>
    /// 添加实体到世界
    /// EN: Add an entity to the world
    /// CN: 向世界中添加实体（对应 Unity 中实例化 GameObject + AddComponent）
    /// </summary>
    public void AddEntity(Entity entity)
    {
        entities.Add(entity);
    }

    /// <summary>
    /// 从世界移除实体
    /// EN: Remove an entity from the world
    /// CN: 从世界中移除实体（对应 Unity 中的 Destroy 方法）
    /// </summary>
    public void RemoveEntity(Entity entity)
    {
        entities.Remove(entity);
    }

    /// <summary>
    /// 每帧更新所有实体 —— 核心方法
    /// EN: Update all entities each frame — core method
    /// CN: 每帧更新所有实体——这就是游戏循环的核心操作。
    ///     对应 Unity 内部每帧遍历所有激活的 MonoBehaviour
    ///     并调用其 Update() 方法的机制。
    ///
    ///     注意事项：
    ///     1. 遍历时实体可能被添加或移除，需要安全处理
    ///     2. 更新顺序可能影响行为——在 Unity 中可通过
    ///        Script Execution Order 配置不同脚本的执行顺序
    ///     3. Update 调用之间，实体状态是"冻结"的，用户看到的是
    ///        所有实体同时更新的结果
    /// </summary>
    /// <param name="deltaTime">帧时间增量</param>
    public void UpdateAll(double deltaTime)
    {
        // 使用 for 循环而不是 foreach，以便在遍历过程中
        // 可以安全地移除已完成的对象
        // EN: Use for loop instead of foreach to safely handle removals during iteration
        // CN: 使用 for 循环，因为 foreach 在遍历时修改集合会抛出异常。
        //     注意：从前往后遍历时，移除当前元素会导致索引偏移。
        //     更安全的方式是从后往前遍历，或标记"待移除"后统一处理。
        for (int i = entities.Count - 1; i >= 0; i--)
        {
            // 跳过已被标记为待移除的实体
            // EN: Skip entities marked for removal
            // CN: 跳过被标记为"死亡"的实体（在实际项目中）
            Entity entity = entities[i];
            if (entity != null)
            {
                // 核心调用：每个实体自己决定这一帧做什么
                // EN: Core call: each entity decides what to do this frame
                // CN: 核心调用——每个实体自己决定这一帧的行为。
                //     Update() 内部的逻辑可能是巡逻、攻击、计时、动画等。
                //     游戏循环完全不关心具体行为，只负责"叫醒"每个实体。
                entity.Update(deltaTime);
            }
        }
    }
}

// ============================================================
// 使用示例
// ============================================================
// var world = new GameWorld();
//
// world.AddEntity(new Skeleton());
// world.AddEntity(new Statue(delay: 90));  // 每 90 帧发射一次
// world.AddEntity(new Statue(delay: 120)); // 每 120 帧发射一次
//
// // 每帧调用 —— 不需要知道具体实体是什么
// // EN: Called every frame — no need to know what entities exist
// // CN: 每帧调用一次——添加或删除实体不需要修改这里的代码！
// world.UpdateAll(Time.deltaTime);
```

---

## Unity Application / Unity 应用场景

* This pattern is the foundation of Unity's entire component system:

- **`MonoBehaviour.Update()`** — Direct implementation. Unity calls `Update()` on every active MonoBehaviour every frame. This is how virtually all gameplay code runs.
- **`MonoBehaviour.FixedUpdate()`** — Same pattern, but at a fixed time step for physics.
- **`MonoBehaviour.LateUpdate()`** — Same pattern, called after all `Update()` calls complete.
- **`MonoBehaviour.Start()`** / **`Awake()`** — One-time initialization variants.
- **`IEnumerator` Coroutines** — A more advanced form of the same pattern, allowing behavior to be spread across multiple frames using `yield return`.
- **`ScriptableObject`** — Can implement `Update()` via a manager that calls it on all instances.
- **Custom update managers** — Some games bypass MonoBehaviour's `Update()` for performance, implementing their own update loop that iterates over a tightly-packed array of components — this is the Data Locality pattern applied to Update Method.

Performance note: Unity calls `Update()` via reflection/C++ interop, which has overhead. For thousands of objects, consider using ECS (Entities Component System) with `IJobChunk` — which is essentially Update Method at massively parallel scale.

**CN:** 此模式是 Unity 整个组件系统的基础：

- **`MonoBehaviour.Update()`** — 直接实现。Unity 每帧对所有激活的 MonoBehaviour 调用 `Update()`。几乎所有游戏逻辑都通过此方式运行。
- **`MonoBehaviour.FixedUpdate()`** — 同一模式，但使用固定时间步，用于物理。
- **`MonoBehaviour.LateUpdate()`** — 同一模式，在所有 `Update()` 完成后调用。
- **`MonoBehaviour.Start()` / `Awake()`** — 一次性初始化的变体。
- **`IEnumerator` 协程** — 同一模式的进阶形式，通过 `yield return` 将行为分散到多帧执行。
- **`ScriptableObject`** — 可通过管理器在其实例上调用 `Update()`。
- **自定义更新管理器** — 某些游戏绕过 MonoBehaviour 的 `Update()` 以获得更高性能，自己实现遍历紧密排列的组件数组的更新循环——这是数据局部性模式应用于更新方法的体现。

性能提示：Unity 通过反射/C++ 互操作调用 `Update()`，存在开销。对于数千个对象，考虑使用 ECS（实体组件系统）配合 `IJobChunk`——本质上是大规模并行版本的 Update Method。

---

## Key Differences / 关键差异

```
C++ (原书)                  | C# (Unity 实现)               | 说明
------------------------------------------------------------------------------------------------
virtual void update() = 0   | abstract void Update(float)    | C# 用抽象方法；Unity 用虚方法 + 反射
手动管理实体数组             | List<MonoBehaviour> 引擎托管  | Unity 自动管理场景中的 MonoBehaviour
每帧循环遍历                 | Unity 内部自动遍历            | 开发者只需写 Update()，无需写循环
无 deltaTime（固定步长）     | Time.deltaTime（可变）        | Unity 提供 Time.deltaTime 处理帧率变化
单继承层级                   | 组件组合                     | Unity 用 Component 模式而非继承层级
无内置生命周期               | Awake/Start/OnEnable/OnDisable/OnDestroy | Unity 提供完整的生命周期钩子
无协程支持                   | yield return / async Task    | Unity 协程允许跨帧执行行为序列
纯虚函数 = 必须实现          | virtual Update = 可选         | MonoBehaviour.Update 是虚方法，不强制覆写
```

---

# Bytecode — 字节码

## Intent / 意图

* Give behavior the flexibility of data by encoding it as instructions for a virtual machine.

* 将行为编码为虚拟机的指令，让行为拥有数据的灵活性。

---

## Motivation / 动机

* Making games may be fun, but it certainly ain't easy. Modern games require enormous, complex codebases. Console manufacturers and app marketplace gatekeepers have stringent quality requirements, and a single crash bug can prevent your game from shipping.

I worked on a game that had six million lines of C++ code. For comparison, the software controlling the Mars Curiosity rover is less than half that.

At the same time, we're expected to squeeze every drop of performance out of the platform. Games push hardware like nothing else, and we have to optimize relentlessly just to keep pace with the competition.

To handle these high stability and performance requirements, we reach for heavyweight languages like C++ that have both low-level expressiveness to make the most of the hardware and rich type systems to prevent or at least corral bugs.

We pride ourselves on our skill at this, but it has its cost. Being a proficient programmer takes years of dedicated training, after which you must contend with the sheer scale of your codebase. Build times for large games can vary somewhere between "go get a coffee" and "go roast your own beans, hand-grind them, pull an espresso, foam some milk, and practice your latte art in the froth".

On top of these challenges, games have one more nasty constraint: *fun*. Players demand a play experience that's both novel and yet carefully balanced. That requires constant iteration, but if every tweak requires bugging an engineer to muck around in piles of low-level code and then waiting for a glacial recompile, you've killed your creative flow.

**CN:** 制作游戏可能很有趣，但绝非易事。现代游戏需要庞大而复杂的代码库。主机厂商和应用商店的审核者有着严格的质量要求，一个崩溃bug就可能让你的游戏无法发售。

我曾参与过一个拥有六百万行C++代码的游戏。作为对比，控制火星好奇号探测车的软件还不到这个数字的一半。

同时，我们还被期望从平台中榨取出每一滴性能。游戏对硬件的推动无与伦比，我们必须不断优化才能跟上竞争的步伐。

为了满足这些高稳定性和高性能的要求，我们选择了像C++这样的重量级语言——它既有底层表达能力以充分利用硬件，又有丰富的类型系统来防止（或至少圈住）bug。

我们为此感到自豪，但这也带来了代价。成为一名熟练的程序员需要多年的专业训练，之后你还得面对代码库的巨大规模。大型游戏的构建时间介于"去倒杯咖啡"和"去自己烘豆、手磨、做一杯浓缩咖啡、打奶泡、在奶泡上练习拉花艺术"之间。

在这些挑战之上，游戏还有一个令人讨厌的约束：*乐趣*。玩家要求的游戏体验既要新颖又要精心平衡。这需要持续的迭代，但如果每次调整都要麻烦工程师在底层代码中折腾，然后等待漫长的重新编译，你的创作灵感早就被扼杀了。

### Spell fight! / 法术对决！

* Let's say we're working on a magic-based fighting game. A pair of wizards square off and fling enchantments at each other until a victor is pronounced. We could define these spells in code, but that means an engineer has to be involved every time one is modified. When a designer wants to tweak a few numbers and get a feel for them, they have to recompile the entire game, reboot it, and get back into a fight.

Like most games these days, we also need to be able to update the game after it ships, both to fix bugs and to add new content. If all of these spells are hard-coded, then updating them means patching the actual game executable.

Let's take things a bit further and say that we also want to support *modding*. We want *users* to be able to create their own spells. If those are in code, that means every modder needs a full compiler toolchain to build the game, and we have to release the sources. Worse, if they have a bug in their spell, it can crash the game on some other player's machine.

**CN:** 假设我们正在开发一款基于魔法的格斗游戏。两个巫师对峙，互相施法，直到决出胜负。我们可以用代码定义这些法术，但这意味着每次修改都需要工程师参与。当设计师想要调整几个数值来感受效果时，他们必须重新编译整个游戏，重启，然后重新进入战斗。

和当今大多数游戏一样，我们还需要在游戏发售后进行更新——既要修复bug，也要添加新内容。如果所有法术都是硬编码的，更新它们就意味着要修补游戏的可执行文件本身。

更进一步，我们还希望支持*模组（modding）*。我们希望*用户*能够创建自己的法术。如果用代码实现，这意味着每个模组作者都需要完整的编译工具链来构建游戏，而且我们还得发布源代码。更糟的是，如果他们的法术有bug，可能会导致其他玩家的游戏崩溃。

### Data > code / 数据胜于代码

* It's pretty clear that our engine's implementation language isn't the right fit. We need spells to be safely sandboxed from the core game. We want them to be easy to modify, easy to reload, and physically separate from the rest of the executable.

I don't know about you, but to me that sounds a lot like *data*. If we can define our behavior in separate data files that the game engine loads and "executes" in some way, we can achieve all of our goals.

We just need to figure out what "execute" means for data. How do you make some bytes in a file express behavior? There are a few ways to do this. I think it will help you get a picture of *this* pattern's strengths and weaknesses if we compare it to another one: the [Interpreter](http://en.wikipedia.org/wiki/Interpreter_pattern) pattern.

**CN:** 很明显，我们引擎的实现语言并不合适。我们需要将法术与核心游戏安全地隔离。我们希望它们易于修改、易于重新加载，并且在物理上与可执行文件的其余部分分离。

我不知道你怎么想，但对我来说这听起来很像*数据*。如果我们可以将行为定义在独立的数据文件中，让游戏引擎以某种方式加载并"执行"它们，我们就能实现所有目标。

我们只需要弄清楚"执行"对于数据意味着什么。如何让文件中的一些字节表达行为？有几种方法可以实现。我认为，如果将它与另一种模式——[解释器（Interpreter）](http://en.wikipedia.org/wiki/Interpreter_pattern)模式——进行比较，你会更清楚地理解*这种*模式的优缺点。

### The Interpreter pattern / 解释器模式

* I could write a whole chapter on this pattern, but four other guys already covered that for me. Instead, I'll cram the briefest of introductions in here. It starts with a language — think *programming* language — that you want to execute. Say, for example, it supports arithmetic expressions like this:

`(1 + 2) * (3 - 4)`

Then, you take each piece of that expression, each rule in the language's grammar, and turn it into an *object*. The number literals will be objects. Basically, they're little wrappers around the raw value. The operators will be objects too, and they'll have references to their operands. If you take into account the parentheses and precedence, that expression magically turns into a little tree of objects.

What "magic" is this? It's simple — *parsing*. A parser takes a string of characters and turns it into an *abstract syntax tree*, a collection of objects representing the grammatical structure of the text.

Whip up one of these and you've got yourself half of a compiler.

The Interpreter pattern isn't about *creating* that tree; it's about *executing* it. The way it works is pretty clever. Each object in the tree is an expression or a subexpression. In true object-oriented fashion, we'll let expressions evaluate themselves.

First, we define a base interface that all expressions implement:

```cpp
class Expression {
public:
  virtual ~Expression() {}
  virtual double evaluate() = 0;
};
```

Then, we define a class that implements this interface for each kind of expression in our language's grammar. The simplest one is numbers:

```cpp
class NumberExpression : public Expression {
public:
  NumberExpression(double value)
  : value_(value)
  {}

  virtual double evaluate()
  {
    return value_;
  }

private:
  double value_;
};
```

A literal number expression simply evaluates to its value. Addition and multiplication are a bit more complex because they contain subexpressions. Before they can evaluate themselves, they need to recursively evaluate their subexpressions. Like so:

```cpp
class AdditionExpression : public Expression {
public:
  AdditionExpression(Expression* left, Expression* right)
  : left_(left),
    right_(right)
  {}

  virtual double evaluate()
  {
    // Evaluate the operands.
    double left = left_->evaluate();
    double right = right_->evaluate();

    // Add them.
    return left + right;
  }

private:
  Expression* left_;
  Expression* right_;
};
```

I'm sure you can figure out what the implementation of multiply looks like.

Pretty neat right? Just a couple of simple classes and now we can represent and evaluate arbitrarily complex arithmetic expressions. We just need to create the right objects and wire them up correctly.

Ruby was implemented like this for something like 15 years. At version 1.9, they switched to bytecode like this chapter describes. Look how much time I'm saving you!

It's a beautiful, simple pattern, but it has some problems. Look up at the illustration. What do you see? Lots of little boxes, and lots of arrows between them. Code is represented as a sprawling fractal tree of tiny objects. That has some unpleasant consequences:

- Loading it from disk requires instantiating and wiring up tons of these small objects.
- Those objects and the pointers between them use a lot of memory. On a 32-bit machine, that little arithmetic expression up there takes up at least 68 bytes, not including padding.
- Traversing the pointers into subexpressions is murder on your data cache. Meanwhile, all of those virtual method calls wreak carnage on your instruction cache.
- Put those together, and what do they spell? S-L-O-W. There's a reason most programming languages in wide use aren't based on the Interpreter pattern. It's just too slow, and it uses up too much memory.

**CN:** 我可以为这种模式写一整章，但另外四位老兄已经替我做过了。所以我只在这里做一个最简短的介绍。它始于你想执行的一种*编程*语言。比如，它支持这样的算术表达式：

`(1 + 2) * (3 - 4)`

然后，你将表达式的每一部分，即语言语法中的每条规则，转化为一个*对象*。数字字面量成为对象——基本上只是原始值的简单包装。运算符也成为对象，并持有对其操作数的引用。考虑括号和优先级后，该表达式神奇地变成了一棵由对象组成的小树。

这是什么"魔法"？其实很简单——*解析（parsing）*。解析器将一串字符转化为*抽象语法树（abstract syntax tree）*，即一组代表文本语法结构的对象。

搞定这个，你就完成了半个编译器。

解释器模式不是关于*创建*那棵树，而是关于*执行*它。它的工作方式相当巧妙。树中的每个对象都是一个表达式或子表达式。秉承真正的面向对象风格，我们让表达式自我求值。

首先，我们定义一个所有表达式都实现的基接口：

```cpp
class Expression {
public:
  virtual ~Expression() {}
  virtual double evaluate() = 0;
};
```

然后，为语言语法中的每种表达式定义一个实现该接口的类。最简单的是数字：

```cpp
class NumberExpression : public Expression {
public:
  NumberExpression(double value)
  : value_(value)
  {}

  virtual double evaluate()
  {
    return value_;
  }

private:
  double value_;
};
```

数字字面量表达式直接求值为其数值。加法和乘法稍微复杂一些，因为它们包含子表达式。在求值自身之前，它们需要递归地求值子表达式：

```cpp
class AdditionExpression : public Expression {
public:
  AdditionExpression(Expression* left, Expression* right)
  : left_(left),
    right_(right)
  {}

  virtual double evaluate()
  {
    double left = left_->evaluate();
    double right = right_->evaluate();
    return left + right;
  }

private:
  Expression* left_;
  Expression* right_;
};
```

我相信你能猜到乘法的实现。

很简洁，对吗？只需要几个简单的类，我们就能表示和求值任意复杂的算术表达式。只需要创建正确的对象并正确连接它们。

Ruby 用这种方式实现了大约 15 年。在 1.9 版本中，他们切换到了本章描述的字节码方案。看我帮你省了多少时间！

这是一个优美、简单的模式，但它有一些问题。看看上面的图示。你看到了什么？很多小盒子，以及它们之间的大量箭头。代码被表示为一棵蔓延的分形小对象树。这带来了一些令人不快的后果：

- 从磁盘加载时需要实例化和连接大量小对象。
- 这些对象及其指针消耗大量内存。在 32 位机器上，上面那个小小的算术表达式至少占用 68 字节（不包括填充）。
- 遍历子表达式指针对你数据缓存的打击是毁灭性的。同时，所有的虚方法调用对你的指令缓存造成严重破坏。
- 综合来看，结果就是——慢。大多数广泛使用的编程语言不基于解释器模式是有原因的：太慢了，而且消耗太多内存。

### Machine code, virtually / 虚拟机器码

* Consider our game. When we run it, the player's computer doesn't traverse a bunch of C++ grammar tree structures at runtime. Instead, we compile it ahead of time to machine code, and the CPU runs that. What's machine code got going for it?

- *It's dense.* It's a solid, contiguous blob of binary data, and no bit goes to waste.
- *It's linear.* Instructions are packed together and executed one right after another. No jumping around in memory (unless you're doing actual control flow, of course).
- *It's low-level.* Each instruction does one relatively minimal thing, and interesting behavior comes from *composing* them.
- *It's fast.* As a consequence of all of these (well, and the fact that it's implemented directly in hardware), machine code runs like the wind.

This sounds swell, but we don't want actual machine code for our spells. Letting users provide machine code which our game executes is just begging for security problems. What we need is a compromise between the performance of machine code and the safety of the Interpreter pattern.

What if instead of loading actual machine code and executing it directly, we defined our own *virtual* machine code? We'd then write a little emulator for it in our game. It would be similar to machine code — dense, linear, relatively low-level — but would also be handled entirely by our game so we could safely sandbox it.

We'd call our little emulator a *virtual machine* (or "VM" for short), and the synthetic binary machine code it runs *bytecode*. It's got the flexibility and ease of use of defining things in data, but it has better performance than higher-level representations like the Interpreter pattern.

This sounds daunting, though. My goal for the rest of this chapter is to show you that if you keep your feature list pared down, it's actually pretty approachable. Even if you end up not using this pattern yourself, you'll at least have a better understanding of Lua and many other languages which are implemented using it.

**CN:** 想想我们的游戏。当它运行时，玩家的电脑并不会在运行时遍历一堆C++语法树结构。相反，我们提前将其编译为机器码，然后CPU执行它。机器码有什么优势呢？

- *紧凑。* 它是连续、致密的二进制数据块，每一比特都不浪费。
- *线性。* 指令紧密排列，一条接一条执行。不会在内存中跳来跳去（当然，除非你正在做实际的控制流）。
- *底层。* 每条指令只做一件相对微小的事情，有趣的行为来自于*组合*它们。
- *快速。* 作为上述所有特性的结果（以及它直接在硬件中实现的事实），机器码运行如风。

这听起来很棒，但我们不希望为法术使用真实的机器码。让用户提供机器码并由我们的游戏执行，这简直是在招引安全问题。我们需要的是在机器码的性能和解释器模式的安全性之间找到一个折中方案。

如果我们不加载真实的机器码并直接执行，而是定义我们自己的*虚拟*机器码呢？然后我们在游戏中为它编写一个小型模拟器。它将类似于机器码——紧凑、线性、相对底层——但完全由我们的游戏处理，所以我们可以安全地将其沙箱化。

我们称这个小模拟器为*虚拟机*（简称"VM"），它运行的人造二进制机器码称为*字节码（bytecode）*。它拥有用数据定义事物的灵活性和易用性，同时比解释器模式等高层表示具有更好的性能。

不过这听起来令人望而生畏。在本章剩余部分中，我的目标是向你展示：如果你精简功能列表，它实际上相当平易近人。即使你最终没有亲自使用这种模式，你至少也会对Lua以及许多其他基于此模式实现的语言有更好的理解。

---

## The Pattern / 模式定义

* An **instruction set** defines the low-level operations that can be performed. A series of instructions is encoded as a **sequence of bytes**. A **virtual machine** executes these instructions one at a time, using a **stack for intermediate values**. By combining instructions, complex high-level behavior can be defined.

* **指令集（instruction set）** 定义了可以执行的底层操作。一系列指令被编码为**字节序列（sequence of bytes）**。**虚拟机（virtual machine）** 逐条执行这些指令，使用**栈（stack）** 来保存中间值。通过组合指令，可以定义复杂的高层行为。

---

## When to Use It / 适用场景

* This is the most complex pattern in this book, and it's not something to throw into your game lightly. Use it when you have a lot of behavior you need to define and your game's implementation language isn't a good fit because:

- It's too low-level, making it tedious or error-prone to program in.
- Iterating on it takes too long due to slow compile times or other tooling issues.
- It has too much trust. If you want to ensure the behavior being defined can't break the game, you need to sandbox it from the rest of the codebase.

Of course, that list describes a bunch of your game. Who doesn't want a faster iteration loop or more safety? However, that doesn't come for free. Bytecode is slower than native code, so it isn't a good fit for performance-critical parts of your engine.

**CN:** 这是本书中最复杂的模式，不应轻易引入到你的游戏中。当你需要定义大量行为，而游戏的实现语言因以下原因不适合时，可以使用它：

- 语言太底层，编程时繁琐且容易出错。
- 由于编译时间慢或其他工具问题，迭代耗时太长。
- 信任级别过高。如果要确保所定义的行为不会破坏游戏，你需要将其与代码库的其余部分沙箱隔离。

当然，上述描述也适用于你游戏中的许多部分。谁不想要更快的迭代循环或更高的安全性呢？但这并非没有代价。字节码比原生代码慢，因此不适合引擎中性能关键的部分。

---

## Keep in Mind / 注意事项

* There's something seductive about creating your own language or system-within-a-system. I'll be doing a minimal example here, but in the real world, these things tend to grow like vines.

Every time I see someone define a little language or a scripting system, they say, "Don't worry, it will be tiny." Then, inevitably, they add more and more little features until it's a full-fledged language. Except, unlike some other languages, it grew in an ad-hoc, organic fashion and has all of the architectural elegance of a shanty town.

Of course, there's nothing *wrong* with making a full-fledged language. Just make sure you do so deliberately. Otherwise, be very careful to control the scope of what your bytecode can express. Put a short leash on it before it runs away from you.

**CN:** 创建自己的语言或系统中的系统，这件事有种说不出的诱惑。我在这里只做一个最小化的示例，但在现实世界中，这些东西往往会像藤蔓一样疯长。

每次我看到有人定义一种小语言或脚本系统时，他们都会说："别担心，它只会很小。"然后，不可避免地，他们不断添加功能，直到它变成一门完整的语言。只不过，与其他语言不同，它以临时、有机的方式生长，拥有贫民窟式的架构优雅性。

当然，创建一门完整的语言本身并*没有错*。只要确保你是刻意为之。否则，就要非常谨慎地控制你的字节码所能表达的范围。在它失控之前给它套上缰绳。

### You'll need a front-end / 你需要一个前端

* Low-level bytecode instructions are great for performance, but a binary bytecode format is *not* what your users are going to author. One reason we're moving behavior out of code is so that we can express it at a *higher* level. If C++ is too low-level, making your users effectively write in assembly language — even one of your own design — isn't an improvement!

Much like the Gang of Four's Interpreter pattern, it's assumed that you also have some way to *generate* the bytecode. Usually, users author their behavior in some higher-level format, and a tool translates that to the bytecode that our virtual machine understands. In other words, a compiler.

I know, that sounds scary. That's why I'm mentioning it here. If you don't have the resources to build an authoring tool, then bytecode isn't for you. But as we'll see later, it may not be as bad as you think.

**CN:** 底层字节码指令有利于性能，但二进制字节码格式*不是*用户直接编写的方式。我们将行为从代码中移出的一个原因，正是为了能在*更高*层次上表达它。如果C++太底层，那么让用户实际上用汇编语言编写（即使是你自己设计的汇编）并不是一种改进！

与GoF的解释器模式类似，这里的隐含前提是你也有某种方式来*生成*字节码。通常，用户以某种更高级的格式编写他们的行为，然后一个工具将其翻译为我们的虚拟机能够理解的字节码。换句话说，需要一个编译器。

我知道这听起来很吓人。所以我在这里提及。如果你没有资源来构建一个创作工具，那么字节码不适合你。但正如我们稍后将看到的，它可能没有你想象的那么糟糕。

### You'll miss your debugger / 你会怀念你的调试器

* Programming is hard. We know what we want the machine to do, but we don't always communicate that correctly — we write bugs. To help find and fix those, we've amassed a pile of tools to understand what our code is doing wrong, and how to right it. We have debuggers, static analyzers, decompilers, etc. All of those tools are designed to work with some existing language: either machine code or something higher level.

When you define your own bytecode VM, you leave those tools behind. Sure, you can step through the VM in your debugger, but that tells you what the VM *itself* is doing, and not what the bytecode it's interpreting is up to. It certainly doesn't help you map that bytecode back to the high-level form it was compiled from.

If the behavior you're defining is simple, you can scrape by without too much tooling to help you debug it. But as the scale of your content grows, plan to invest real time into features that help users see what their bytecode is doing. Those features might not ship in your game, but they'll be critical to ensure that you actually *can* ship your game.

Of course, if you want your game to be moddable, then you *will* ship those features, and they'll be even more important.

**CN:** 编程很难。我们知道想让机器做什么，但并非总能正确传达——我们会写出bug。为了帮助发现和修复它们，我们积累了一堆工具来理解代码做错了什么以及如何修正。我们有调试器、静态分析器、反编译器等等。所有这些工具都是为某种现有语言设计的：机器码或某种更高级的语言。

当你定义自己的字节码虚拟机时，你把这些工具都抛在了身后。当然，你可以在调试器中单步跟踪虚拟机，但这告诉你的是虚拟机*本身*在做什么，而不是它正在解释的字节码在做什么。它当然也无法帮助你将该字节码映射回它被编译而来的高级形式。

如果你要定义的行为很简单，没有太多调试工具也能勉强应付。但随着内容规模的增长，请计划投入实际时间来构建帮助用户查看字节码行为的功能。这些功能可能不会随游戏发布，但它们对于确保你*能够*发布游戏至关重要。

当然，如果你想让游戏支持模组，那么你*会*发布这些功能，它们甚至更加重要。

---

## Sample Code / 示例代码

* After the previous couple of sections, you might be surprised how straightforward the implementation is. First, we need to craft an instruction set for our VM. Before we start thinking about bytecode and stuff, let's just think about it like an API.

* 经过前面几节，你可能会惊讶地发现实现竟然如此直接。首先，我们需要为虚拟机设计一套指令集。在我们开始思考字节码之类的东西之前，先像设计API一样来思考。

### A magical API / 魔法API

* If we were defining spells in straight C++ code, what kind of API would we need for that code to call into? What are the basic operations in the game engine that spells are defined in terms of?

Most spells ultimately change one of the stats of a wizard, so we'll start with a couple for that:

```cpp
void setHealth(int wizard, int amount);
void setWisdom(int wizard, int amount);
void setAgility(int wizard, int amount);
```

The first parameter identifies which wizard is affected, say `0` for the player's and `1` for their opponent. This way, healing spells can affect the player's own wizard, while damaging attacks harm their nemesis. These three little methods cover a surprisingly wide variety of magical effects.

If the spells just silently tweaked stats, the game logic would be fine, but playing it would bore players to tears. Let's fix that:

```cpp
void playSound(int soundId);
void spawnParticles(int particleType);
```

These don't affect gameplay, but they crank up the intensity of the gameplay *experience*. We could add more for camera shake, animation, etc., but this is enough to get us started.

**CN:** 如果用纯C++代码定义法术，我们需要什么样的API供这些代码调用？法术所依赖的游戏引擎的基本操作是什么？

大多数法术最终会改变巫师的一项属性，所以我们先添加几个这样的方法：

```cpp
void setHealth(int wizard, int amount);
void setWisdom(int wizard, int amount);
void setAgility(int wizard, int amount);
```

第一个参数标识受影响的巫师，比如`0`表示玩家自己的巫师，`1`表示对手。这样，治疗法术可以影响己方巫师，而伤害攻击则伤害宿敌。这三个小方法覆盖了惊人丰富的魔法效果。

如果法术只是默默地调整属性，游戏逻辑上没问题，但玩起来会无聊到让玩家流泪。让我们来改进：

```cpp
void playSound(int soundId);
void spawnParticles(int particleType);
```

这些不影响玩法，但它们提升了游戏*体验*的强度。我们还可以添加更多用于镜头震动、动画等，但这些足够开始了。

### A magical instruction set / 魔法指令集

* Now let's see how we'd turn this *programmatic* API into something that can be controlled from data. Let's start small and then we'll work our way up to the whole shebang. For now, we'll ditch all of the parameters to these methods. We'll say the `set___()` methods always affect the player's own wizard and always max out the stat. Likewise, the FX operations always play a single hard-coded sound and particle effect.

Given that, a spell is just a series of instructions. Each one identifies which operation you want to perform. We can enumerate them:

```cpp
enum Instruction {
  INST_SET_HEALTH      = 0x00,
  INST_SET_WISDOM      = 0x01,
  INST_SET_AGILITY     = 0x02,
  INST_PLAY_SOUND      = 0x03,
  INST_SPAWN_PARTICLES = 0x04
};
```

To encode a spell in data, we store an array of `enum` values. We've only got a few different primitives, so the range of `enum` values easily fits into a byte. This means the code for a spell is just a list of bytes — ergo "bytecode".

To execute a single instruction, we see which primitive it is and dispatch to the right API method:

```cpp
switch (instruction) {
  case INST_SET_HEALTH:
    setHealth(0, 100);
    break;

  case INST_SET_WISDOM:
    setWisdom(0, 100);
    break;

  case INST_SET_AGILITY:
    setAgility(0, 100);
    break;

  case INST_PLAY_SOUND:
    playSound(SOUND_BANG);
    break;

  case INST_SPAWN_PARTICLES:
    spawnParticles(PARTICLE_FLAME);
    break;
}
```

In this way, our interpreter forms the bridge between code world and data world. We can wrap this in a little VM that executes an entire spell like so:

```cpp
class VM {
public:
  void interpret(char bytecode[], int size)
  {
    for (int i = 0; i < size; i++)
    {
      char instruction = bytecode[i];
      switch (instruction)
      {
        // Cases for each instruction...
      }
    }
  }
};
```

Type that in and you'll have written your first virtual machine. Unfortunately, it's not very flexible. We can't define a spell that touches the player's opponent or lowers a stat. We can only play one sound!

To get something that starts to have the expressive feel of an actual language, we need to get parameters in here.

**CN:** 现在我们来看看如何将这个*程序式*的API变成可以由数据控制的东西。从小处着手，再逐步扩展到完整方案。现在，我们去掉所有这些方法的参数。我们规定`set___()`方法总是影响玩家自己的巫师，并且总是将属性加到最大。同样，特效操作总是播放单个硬编码的声音和粒子效果。

这样，法术就是一系列指令。每条指令标识你想要执行的操作。我们可以枚举它们：

```cpp
enum Instruction {
  INST_SET_HEALTH      = 0x00,
  INST_SET_WISDOM      = 0x01,
  INST_SET_AGILITY     = 0x02,
  INST_PLAY_SOUND      = 0x03,
  INST_SPAWN_PARTICLES = 0x04
};
```

要将法术编码为数据，我们存储一个`enum`值数组。我们只有少数几种不同的原语，所以`enum`值的范围轻松适配到一个字节中。这意味着法术的代码只是一个字节列表——因此得名"字节码"。

要执行单条指令，我们查看它是哪个原语并派发到正确的API方法：

```cpp
switch (instruction) {
  case INST_SET_HEALTH:
    setHealth(0, 100);
    break;

  case INST_SET_WISDOM:
    setWisdom(0, 100);
    break;

  case INST_SET_AGILITY:
    setAgility(0, 100);
    break;

  case INST_PLAY_SOUND:
    playSound(SOUND_BANG);
    break;

  case INST_SPAWN_PARTICLES:
    spawnParticles(PARTICLE_FLAME);
    break;
}
```

这样，我们的解释器就构成了代码世界和数据世界之间的桥梁。我们可以将其封装在一个小虚拟机中，执行完整的法术：

```cpp
class VM {
public:
  void interpret(char bytecode[], int size)
  {
    for (int i = 0; i < size; i++)
    {
      char instruction = bytecode[i];
      switch (instruction)
      {
        // Cases for each instruction...
      }
    }
  }
};
```

输入这些代码，你就写好了你的第一个虚拟机。不幸的是，它非常不灵活。我们无法定义影响对手巫师或降低属性的法术。我们只能播放一种声音！

要获得真正语言般的表达能力，我们需要引入参数。

### A stack machine / 栈式机器

* To execute a complex nested expression, you start with the innermost subexpressions. You calculate those, and the results flow outward as arguments to the expressions that contain them until eventually, the whole expression has been evaluated.

The Interpreter pattern models this explicitly as a tree of nested objects, but we want the speed of a flat list of instructions. We still need to ensure results from subexpressions flow to the right surrounding expressions. But, since our data is flattened, we'll have to use the *order* of the instructions to control that. We'll do it the same way your CPU does — with a stack.

```cpp
class VM {
public:
  VM()
  : stackSize_(0)
  {}

  // Other stuff...

private:
  static const int MAX_STACK = 128;
  int stackSize_;
  int stack_[MAX_STACK];
};
```

The VM maintains an internal stack of values. In our example, the only kinds of values our instructions work with are numbers, so we can use a simple array of `int`s. Whenever a bit of data needs to work its way from one instruction to another, it gets there through the stack.

Like the name implies, values can be pushed onto or popped off of the stack, so let's add a couple of methods for that:

```cpp
class VM {
private:
  void push(int value)
  {
    // Check for stack overflow.
    assert(stackSize_ < MAX_STACK);
    stack_[stackSize_++] = value;
  }

  int pop()
  {
    // Make sure the stack isn't empty.
    assert(stackSize_ > 0);
    return stack_[--stackSize_];
  }

  // Other stuff...
};
```

When an instruction needs to receive parameters, it pops them off the stack like so:

```cpp
switch (instruction) {
  case INST_SET_HEALTH:
  {
    int amount = pop();
    int wizard = pop();
    setHealth(wizard, amount);
    break;
  }

  case INST_SET_WISDOM:
  case INST_SET_AGILITY:
    // Same as above...

  case INST_PLAY_SOUND:
    playSound(pop());
    break;

  case INST_SPAWN_PARTICLES:
    spawnParticles(pop());
    break;
}
```

To get some values *onto* that stack, we need one more instruction: a literal. It represents a raw integer value. But where does *it* get its value from? The trick is to take advantage of the fact that our instruction stream is a sequence of bytes — we can stuff the number directly in the byte array. We define another instruction type for a number literal like so:

```cpp
case INST_LITERAL: {
  // Read the next byte from the bytecode.
  int value = bytecode[++i];
  push(value);
  break;
}
```

Here, I'm reading a single byte for the value to avoid the fiddly code required to decode a multiple-byte integer, but in a real implementation, you'll want to support literals that cover your full numeric range.

Let's string a few of these instructions together and watch the interpreter execute them to get a feel for how the stack works. We start with an empty stack and the interpreter pointing to the first instruction.

First, it executes the first `INST_LITERAL`. That reads the next byte from the bytecode (`0`) and pushes it onto the stack.

Then, it executes the second `INST_LITERAL`. That reads the `10` and pushes it.

Finally, it executes `INST_SET_HEALTH`. That pops `10` and stores it in `amount`, then pops `0` and stores it in `wizard`. Then, it calls `setHealth()` with those parameters.

Ta-da! We've got a spell that sets the player's wizard's health to ten points. Now, we've got enough flexibility to define spells that set either wizard's stats to whatever amounts we want. We can also play different sounds and spawn particles.

But… this still feels like a *data* format. We can't, for example, raise a wizard's health by half of their wisdom. Our designers want to be able to express *rules* for spells, not just *values*.

**CN:** 要执行一个复杂的嵌套表达式，你需要从最内层的子表达式开始。你计算它们，结果作为参数向外流向包含它们的表达式，直到最终整个表达式被求值。

解释器模式将此明确建模为嵌套对象树，但我们想要平面指令列表的速度。我们仍然需要确保子表达式的结果流向正确的包围表达式。但由于我们的数据是扁平的，我们必须使用指令的*顺序*来控制这一点。我们将使用与CPU相同的方式——栈。

```cpp
class VM {
public:
  VM()
  : stackSize_(0)
  {}

private:
  static const int MAX_STACK = 128;
  int stackSize_;
  int stack_[MAX_STACK];
};
```

虚拟机维护一个内部的值栈。在我们的例子中，指令操作的唯一值类型是数字，所以我们可以使用一个简单的`int`数组。每当一些数据需要从一条指令传递到另一条时，它就通过栈来传递。

如名所示，值可以被压入栈或从栈弹出，所以让我们添加几个方法：

```cpp
class VM {
private:
  void push(int value)
  {
    assert(stackSize_ < MAX_STACK);
    stack_[stackSize_++] = value;
  }

  int pop()
  {
    assert(stackSize_ > 0);
    return stack_[--stackSize_];
  }
};
```

当一条指令需要接收参数时，它从栈中弹出参数：

```cpp
switch (instruction) {
  case INST_SET_HEALTH:
  {
    int amount = pop();
    int wizard = pop();
    setHealth(wizard, amount);
    break;
  }
  case INST_SET_WISDOM:
  case INST_SET_AGILITY:
    // Same as above...
  case INST_PLAY_SOUND:
    playSound(pop());
    break;
  case INST_SPAWN_PARTICLES:
    spawnParticles(pop());
    break;
}
```

要将值*放入*栈，我们需要另一条指令：字面量指令。它表示一个原始整数值。但它的值从何而来？关键在于利用我们的指令流是一个字节序列的事实——我们可以直接将数字嵌入字节数组。我们为数字字面量定义另一种指令类型：

```cpp
case INST_LITERAL: {
  int value = bytecode[++i];
  push(value);
  break;
}
```

这里，我读取一个字节作为值，以避免解码多字节整数所需的繁琐代码。但在实际实现中，你会希望支持覆盖完整数值范围的字面量。

让我们将几条指令串在一起，观察解释器执行它们，感受栈的工作方式。我们从空栈开始，解释器指向第一条指令。

首先，它执行第一条`INST_LITERAL`。从字节码中读取下一个字节(`0`)并压入栈。

然后，执行第二条`INST_LITERAL`。读取`10`并压入栈。

最后，执行`INST_SET_HEALTH`。弹出`10`存入`amount`，弹出`0`存入`wizard`，然后调用`setHealth()`。

嗒哒！我们有了一个将玩家巫师血量设置为10点的法术。现在我们有了足够的灵活性来定义将任一巫师属性设置为任意值的法术。我们还可以播放不同的声音和生成粒子。

但……这仍然感觉像是一种*数据*格式。例如，我们无法将巫师的血量提高其智慧值的一半。我们的设计师希望能够表达法术的*规则*，而不仅仅是*数值*。

### Behavior = composition / 行为 = 组合

* If we think of our little VM like a programming language, all it supports now is a couple of built-in functions and constant parameters for them. To get bytecode to feel like *behavior*, what we're missing is *composition*.

Our designers need to be able to create expressions that combine different values in interesting ways. For a simple example, they want spells that modify a stat *by* a certain amount instead of *to* a certain amount.

That requires taking into account a stat's current value. We have instructions for *writing* a stat, but we need to add a couple to *read* stats:

```cpp
case INST_GET_HEALTH: {
  int wizard = pop();
  push(getHealth(wizard));
  break;
}

case INST_GET_WISDOM:
case INST_GET_AGILITY:
  // You get the idea...
```

As you can see, these work with the stack in both directions. They pop a parameter to determine which wizard to get the stat for, and then they look up the stat's value and push that back onto the stack.

This lets us write spells that copy stats around. We could create a spell that set a wizard's agility to their wisdom or a strange incantation that set one wizard's health to mirror his opponent's.

Better, but still quite limited. Next, we need arithmetic. It's time our baby VM learned how to add 1 + 1. We'll add a few more instructions. By now, you've probably got the hang of it and can guess how they look. I'll just show addition:

```cpp
case INST_ADD: {
  int b = pop();
  int a = pop();
  push(a + b);
  break;
}
```

Like our other instructions, it pops a couple of values, does a bit of work, and then pushes the result back. Up until now, every new instruction gave us an incremental improvement in expressiveness, but we just made a big leap. It isn't obvious, but we can now handle all sorts of complicated, deeply nested arithmetic expressions.

Let's walk through a slightly more complex example. Say we want a spell that increases the player's wizard's health by the average of their agility and wisdom. In code, that's:

```cpp
setHealth(0, getHealth(0) +
    (getAgility(0) + getWisdom(0)) / 2);
```

You might think we'd need instructions to handle the explicit grouping that parentheses give you in the expression here, but the stack supports that implicitly. Here's how you could evaluate this by hand:

1. Get the wizard's current health and remember it.
2. Get the wizard's agility and remember it.
3. Do the same for their wisdom.
4. Get those last two, add them, and remember the result.
5. Divide that by two and remember the result.
6. Recall the wizard's health and add it to that result.
7. Take that result and set the wizard's health to that value.

Do you see all of those "remembers" and "recalls"? Each "remember" corresponds to a push, and the "recalls" are pops. That means we can translate this to bytecode pretty easily. For example, the first line to get the wizard's current health is:

```
LITERAL 0
GET_HEALTH
```

This bit of bytecode pushes the wizard's health onto the stack. If we mechanically translate each line like that, we end up with a chunk of bytecode that evaluates our original expression. To give you a feel for how the instructions compose, I've done that below.

To show how the stack changes over time, we'll walk through a sample execution where the wizard's current stats are 45 health, 7 agility, and 11 wisdom. Next to each instruction is what the stack looks like after executing it and then a little comment explaining the instruction's purpose:

```
LITERAL 0    [0]            # Wizard index
LITERAL 0    [0, 0]         # Wizard index
GET_HEALTH   [0, 45]        # getHealth()
LITERAL 0    [0, 45, 0]     # Wizard index
GET_AGILITY  [0, 45, 7]     # getAgility()
LITERAL 0    [0, 45, 7, 0]  # Wizard index
GET_WISDOM   [0, 45, 7, 11] # getWisdom()
ADD          [0, 45, 18]    # Add agility and wisdom
LITERAL 2    [0, 45, 18, 2] # Divisor
DIVIDE       [0, 45, 9]     # Average agility and wisdom
ADD          [0, 54]        # Add average to current health
SET_HEALTH   []             # Set health to result
```

If you watch the stack at each step, you can see how data flows through it almost like magic. We push `0` for the wizard index at the beginning, and it just hangs around at the bottom of the stack until we finally need it for the last `SET_HEALTH` at the end.

**CN:** 如果我们把小虚拟机看作一种编程语言，它目前只支持几个内置函数及其常量参数。要让字节码感觉像*行为*，我们缺少的是*组合*。

我们的设计师需要能够创建以有趣方式组合不同值的表达式。举个简单的例子，他们想要按**某个量**修改属性，而不是将属性设置为**某个值**。

这需要考虑到属性的当前值。我们有*写入*属性的指令，但还需要添加一些*读取*属性的指令：

```cpp
case INST_GET_HEALTH: {
  int wizard = pop();
  push(getHealth(wizard));
  break;
}

case INST_GET_WISDOM:
case INST_GET_AGILITY:
  // You get the idea...
```

如你所见，这些指令双向操作栈。它们弹出一个参数来确定要获取哪个巫师的属性，然后查找属性值并将其压回栈中。

这让我们能编写复制属性的法术。我们可以创建一个将巫师的敏捷设置为智慧值的法术，或者一个让某个巫师的血量镜像对手的奇怪咒语。

更好了一些，但仍然相当有限。接下来，我们需要算术运算。是时候让我们的小虚拟机学会加1+1了。我们再添加几条指令。到现在，你可能已经掌握了诀窍，能猜到它们长什么样。我只展示加法：

```cpp
case INST_ADD: {
  int b = pop();
  int a = pop();
  push(a + b);
  break;
}
```

和其他指令一样，它弹出几个值，做一点工作，然后将结果压回。到目前为止，每条新指令都让我们在表达能力上有了渐进式的改进，但这次我们实现了一个大飞跃。虽然不太明显，但我们现在可以处理各种复杂、深度嵌套的算术表达式了。

让我们看一个稍微复杂的例子。假设我们要一个法术，将玩家巫师的血量提高为其敏捷和智慧的平均值。用代码表示：

```cpp
setHealth(0, getHealth(0) +
    (getAgility(0) + getWisdom(0)) / 2);
```

你可能认为我们需要指令来处理表达式中的括号分组，但栈隐式地支持了这一点。以下是你手动求值的方式：

1. 获取巫师当前血量并记住它。
2. 获取巫师的敏捷并记住它。
3. 对智慧做同样的事。
4. 取后两个值，相加，记住结果。
5. 除以2，记住结果。
6. 回想巫师的血量，加到那个结果上。
7. 用那个结果设置巫师的血量。

你看到所有这些"记住"和"回想"了吗？每个"记住"对应一次压栈，"回想"对应一次弹栈。这意味着我们可以很容易地将其翻译成字节码。例如，获取巫师当前血量的第一行是：

```
LITERAL 0
GET_HEALTH
```

这段字节码将巫师的血量压入栈。如果我们机械地翻译每一行，最终会得到一段求值原始表达式的字节码。为了让你感受指令如何组合，我做了如下演示。

为了展示栈随时间的变化，我们遍历一个示例执行过程，其中巫师当前的属性是：血量45、敏捷7、智慧11。每条指令旁边是执行后栈的状态，以及解释指令用途的注释：

```
LITERAL 0    [0]            # Wizard index
LITERAL 0    [0, 0]         # Wizard index
GET_HEALTH   [0, 45]        # getHealth()
LITERAL 0    [0, 45, 0]     # Wizard index
GET_AGILITY  [0, 45, 7]     # getAgility()
LITERAL 0    [0, 45, 7, 0]  # Wizard index
GET_WISDOM   [0, 45, 7, 11] # getWisdom()
ADD          [0, 45, 18]    # Add agility and wisdom
LITERAL 2    [0, 45, 18, 2] # Divisor
DIVIDE       [0, 45, 9]     # Average agility and wisdom
ADD          [0, 54]        # Add average to current health
SET_HEALTH   []             # Set health to result
```

如果你观察每一步的栈，可以看到数据像魔法一样流经其中。我们在一开始为巫师索引压入`0`，它一直待在栈底，直到最后在`SET_HEALTH`中才用到。

### A virtual machine / 虚拟机

* I could keep going, adding more and more instructions, but this is a good place to stop. As it is, we've got a nice little VM that lets us define fairly open-ended behavior using a simple, compact data format. While "bytecode" and "virtual machines" sound intimidating, you can see they're often as simple as a stack, a loop, and a switch statement.

Remember our original goal to have behavior be nicely sandboxed? Now that you've seen exactly how the VM is implemented, it's obvious that we've accomplished that. The bytecode can't do anything malicious or reach out into weird parts of the game engine because we've only defined a few instructions that touch the rest of the game.

We control how much memory it uses by how big of a stack we create, and we're careful to make sure it can't overflow that. We can even control how much *time* it uses. In our instruction loop, we can track how many we've executed and bail out if it goes over some limit.

**CN:** 我可以继续添加越来越多的指令，但这里是一个不错的停点。目前，我们有一个漂亮的小虚拟机，允许使用简单、紧凑的数据格式来定义相当开放的行为。虽然"字节码"和"虚拟机"听起来很吓人，但你可以看到它们往往就像栈、循环和switch语句那么简单。

还记得我们最初的目标——让行为被良好地沙箱化吗？现在你已经看到了虚拟机是如何实现的，很明显我们做到了这一点。字节码不能做任何恶意的事情，也不能触及游戏引擎的古怪部分，因为我们只定义了少数与游戏其他部分交互的指令。

我们通过创建栈的大小来控制它使用的内存量，并小心地确保它不会溢出。我们甚至可以控制它使用的*时间*——在指令循环中，我们可以跟踪已经执行了多少条指令，并在超过某个限制时退出。

### Spellcasting tools / 施法工具

* One of our initial goals was to have a *higher*-level way to author behavior, but we've gone and created something *lower*-level than C++. It has the runtime performance and safety we want, but absolutely none of the designer-friendly usability.

To fill that gap, we need some tooling. We need a program that lets users define the high-level behavior of a spell and then takes that and generates the appropriate low-level stack machine bytecode.

That probably sounds way harder than making the VM. Many programmers were dragged through a compilers class in college and took away from it nothing but PTSD triggered by the sight of a book with a dragon on the cover or the words "lex" and "yacc".

In truth, compiling a text-based language isn't that bad, though it's a *bit* too broad of a topic to cram in here. However, you don't have to do that. What I said we need is a *tool* — it doesn't have to be a *compiler* whose input format is a *text file*.

On the contrary, I encourage you to consider building a graphical interface to let users define their behavior, especially if the people using it won't be highly technical. Writing text that's free of syntax errors is difficult for people who haven't spent years getting used to a compiler yelling at them.

Instead, you can build an app that lets users "script" by clicking and dragging little boxes, pulling down menu items, or whatever else makes sense for the kind of behavior you want them to create.

The nice thing about this is that your UI can make it impossible for users to create "invalid" programs. Instead of vomiting error messages on them, you can proactively disable buttons or provide default values to ensure that the thing they've created is valid at all points in time.

I want to stress how important error-handling is. As programmers, we tend to view human error as a shameful personality flaw that we strive to eliminate in ourselves. To make a system that users enjoy, you have to embrace their humanity, *including their fallibility*. Making mistakes is what people do, and is a fundamental part of the creative process. Handling them gracefully with features like undo helps your users be more creative and create better work.

This spares you from designing a grammar and writing a parser for a little language. But, I know, some of you find UI programming equally unpleasant. Well, in that case, I don't have any good news for you.

Ultimately, this pattern is about expressing behavior in a user-friendly, high-level way. You have to craft the user experience. To execute the behavior efficiently, you then need to translate that into a lower-level form. It is real work, but if you're up to the challenge, it can pay off.

**CN:** 我们的初始目标之一是拥有*更高级*的行为创作方式，但我们却创造了比C++*更底层*的东西。它拥有我们想要的运行时性能和安全，但完全没有对设计师友好的可用性。

为了填补这个空白，我们需要一些工具。我们需要一个程序，让用户定义法术的高级行为，然后将其生成适当的底层栈机器字节码。

这听起来可能比制作虚拟机还要难得多。许多程序员在大学里被拖去上编译器课程，除了看到封面有龙的书或听到"lex"和"yacc"就触发PTSD外，什么也没学到。

说实话，编译一种基于文本的语言并没有那么糟糕，尽管这个主题有点太宽泛了，无法塞进这里。但你不必那么做。我们需要的只是一个*工具*——它不一定要是一个输入格式是*文本文件*的*编译器*。

相反，我鼓励你考虑构建一个图形界面来让用户定义行为，特别是当使用者并非技术型人员时。对于那些没有花多年时间习惯编译器对他们吼叫的人来说，编写没有语法错误的文本是困难的。

你可以构建一个应用，让用户通过点击和拖动小框、下拉菜单项或其他符合你想要他们创建的行为类型的方式进行"脚本编写"。

这样做的好处是，你的UI可以让用户不可能创建"无效"程序。与其向他们输出错误信息，你可以主动禁用按钮或提供默认值，以确保他们创建的内容在任何时刻都是有效的。

我想强调错误处理有多么重要。作为程序员，我们倾向于将人为错误视为一种可耻的性格缺陷，并努力在自己身上消除它。但要创建一个用户享受的系统，你必须接纳他们的人性，*包括他们的易错性*。犯错是人类的本性，是创作过程的基本组成部分。通过撤销等功能优雅地处理错误，可以帮助你的用户更具创造力，创作出更好的作品。

这让你免于为一种小语言设计语法和编写解析器。但我知道，你们中的一些人也同样讨厌UI编程。好吧，那样的话我没有好消息给你。

最终，这种模式是关于以用户友好的高级方式表达行为。你必须精心设计用户体验。为了高效地执行行为，你需要将其翻译成更低级的形式。这是一项真正的工作，但如果你能迎接挑战，它会带来回报。

---

## Design Decisions / 设计决策

* I tried to keep this chapter as simple as I could, but what we're really doing is creating a language. That's a pretty open-ended design space. Exploring it can be tons of fun, so make sure you don't forget to finish your game.

* 我尽力让本章保持简单，但我们实际上在做的是一门语言的创建。这是一个相当开放的设计空间。探索它可以带来很多乐趣，所以确保你没有忘记完成你的游戏。

### How do instructions access the stack? / 指令如何访问栈？

* Bytecode VMs come in two main flavors: stack-based and register-based. In a stack-based VM, instructions always work from the top of the stack, like in our sample code. For example, `INST_ADD` pops two values, adds them, and pushes the result.

Register-based VMs still have a stack. The only difference is that instructions can read their inputs from deeper in the stack. Instead of `INST_ADD` always *popping* its operands, it has two indexes stored in the bytecode that identify where in the stack to read the operands from.

- **With a stack-based VM:**
  - *Instructions are small.* Since each instruction implicitly finds its arguments on top of the stack, you don't need to encode any data for that. This means each instruction can be pretty small, usually a single byte.
  - *Code generation is simpler.* When you get around to writing the compiler or tool that outputs bytecode, you'll find it simpler to generate stack-based bytecode. Since each instruction implicitly works from the top of the stack, you just need to output instructions in the right order to pass parameters between them.
  - *You have more instructions.* Each instruction only sees the very top of the stack. This means that to generate code for something like `a = b + c`, you need separate instructions to move `b` and `c` to the top of the stack, perform the operation, then move the result into `a`.

- **With a register-based VM:**
  - *Instructions are larger.* Since instructions need arguments for stack offsets, a single instruction needs more bits. For example, an instruction in Lua — probably the most well-known register-based VM — is a full 32-bits. It uses 6 bits for the instruction type, and the rest are arguments.
  - *You have fewer instructions.* Since each instruction can do more work, you don't need as many of them. Some say you get a performance improvement since you don't have to shuffle values around in the stack as much.

So which should you do? My recommendation is to stick with a stack-based VM. They're simpler to implement and much simpler to generate code for. Register-based VMs got a reputation for being a bit faster after Lua converted to that style, but it depends *deeply* on your actual instructions and on lots of other details of your VM.

**CN:** 字节码虚拟机主要有两种风格：栈式（stack-based）和寄存器式（register-based）。在栈式虚拟机中，指令总是操作栈顶，就像我们的示例代码那样。例如，`INST_ADD`弹出两个值，相加，然后压入结果。

寄存器式虚拟机仍然有一个栈。唯一的区别是，指令可以从栈中更深的位置读取输入。`INST_ADD`不是总是*弹出*操作数，而是在字节码中存储两个索引，标识从栈的哪个位置读取操作数。

- **栈式虚拟机：**
  - *指令小。* 由于每条指令隐式地在栈顶找到参数，你不需要为此编码任何数据。这意味着每条指令可以非常小，通常是一个字节。
  - *代码生成更简单。* 当你编写输出字节码的编译器或工具时，你会发现生成栈式字节码更简单。由于每条指令隐式地操作栈顶，你只需按正确的顺序输出指令，即可在它们之间传递参数。
  - *指令更多。* 每条指令只能看到栈顶。这意味着要为`a = b + c`这样的代码生成指令，你需要分别将`b`和`c`移到栈顶，执行运算，然后将结果移入`a`。

- **寄存器式虚拟机：**
  - *指令更大。* 由于指令需要栈偏移量的参数，单条指令需要更多比特。例如，Lua中的一条指令（可能最著名的寄存器式虚拟机）是完整的32位。其中6位用于指令类型，其余为参数。
  - *指令更少。* 由于每条指令可以做更多工作，你不需要那么多指令。有人说你能获得性能提升，因为你不需要在栈中频繁移动值。

那么应该选择哪种？我的建议是坚持使用栈式虚拟机。它们实现更简单，代码生成也简单得多。寄存器式虚拟机在Lua转换为此风格后获得了更快的声誉，但这*非常*依赖于你实际的指令和虚拟机的许多其他细节。

### What instructions do you have? / 包含哪些指令？

* Your instruction set defines the boundaries of what can and cannot be expressed in bytecode, and it also has a big impact on the performance of your VM. Here's a laundry list of the different kinds of instructions you may want:

- **External primitives.** These are the ones that reach out of the VM into the rest of the game engine and do stuff that the user can see. They control what kinds of real behavior can be expressed in bytecode. Without these, your VM can't do anything more than burn CPU cycles.
- **Internal primitives.** These manipulate values inside the VM — things like literals, arithmetic, comparison operators, and instructions that juggle the stack around.
- **Control flow.** Our example didn't cover these, but when you want behavior that's imperative and conditionally executes instructions or loops and executes instructions more than once, you need control flow. In the low-level language of bytecode, they're surprisingly simple: jumps. All a jump instruction does is modify that variable and change where we're currently executing. In other words, it's a `goto`. You can build all kinds of higher-level control flow using that.
- **Abstraction.** If your users start defining a *lot* of stuff in data, eventually they'll want to start reusing bits of bytecode instead of having to copy and paste it. You may want something like callable procedures. In their simplest form, procedures aren't much more complex than a jump. The only difference is that the VM maintains a second *return* stack. When it executes a "call" instruction, it pushes the current instruction index onto the return stack and then jumps to the called bytecode. When it hits a "return", the VM pops the index from the return stack and jumps back to it.

**CN:** 你的指令集定义了字节码可以表达和不能表达的内容边界，也对虚拟机的性能有很大影响。以下是你可能需要的不同种类指令的清单：

- **外部原语。** 这些指令从虚拟机伸出到游戏引擎的其他部分，做用户可以看到的事情。它们控制着可以在字节码中表达哪些真实行为。没有它们，你的虚拟机除了消耗CPU周期外什么也做不了。
- **内部原语。** 这些指令操作虚拟机内部的值——比如字面量、算术运算、比较运算符以及在栈中搬运数据的指令。
- **控制流。** 我们的示例没有涵盖这些，但当你需要命令式的行为，有条件地执行指令或循环多次执行指令时，你需要控制流。在字节码的底层语言中，它们惊人地简单：跳转。跳转指令所做的就是修改变量，改变当前执行位置。换句话说，它是一个`goto`。你可以用它构建各种更高级的控制流。
- **抽象。** 如果用户开始在数据中定义*大量*内容，最终他们会希望重用部分字节码，而不是复制粘贴。你可能需要像可调用过程（procedure）这样的东西。在最简单的形式中，过程并不比跳转复杂多少。唯一的区别是虚拟机维护了第二个*返回*栈。当它执行"call"指令时，将当前指令索引压入返回栈，然后跳转到被调用的字节码。当遇到"return"时，虚拟机从返回栈弹出索引并跳转回去。

### How are values represented? / 如何表示值？

* Our sample VM only works with one kind of value, integers. That makes answering this easy — the stack is just a stack of `int`s. A more full-featured VM will support different data types: strings, objects, lists, etc. You'll have to decide how those are stored internally.

- **A single datatype:**
  - *It's simple.* You don't have to worry about tagging, conversions, or type-checking.
  - *You can't work with different data types.* This is the obvious downside. Cramming different types into a single representation — think storing numbers as strings — is asking for pain.

- **A tagged variant:**
  This is the common representation for dynamically typed languages. Every value has two pieces. The first is a type tag — an `enum` — that identifies what data type is being stored. The rest of the bits are then interpreted appropriately according to that type, like:

  ```cpp
  enum ValueType {
    TYPE_INT,
    TYPE_DOUBLE,
    TYPE_STRING
  };

  struct Value {
    ValueType type;
    union
    {
      int    intValue;
      double doubleValue;
      char*  stringValue;
    };
  };
  ```

  - *Values know their type.* The nice thing about this representation is that you can check the type of a value at runtime. That's important for dynamic dispatch and for ensuring that you don't try to perform operations on types that don't support it.
  - *It takes more memory.* Every value has to carry around a few extra bits with it to identify its type. In something as low-level as a VM, a few bits here and there add up quickly.

- **An untagged union:**
  This uses a union like the previous form, but it does *not* have a type tag that goes along with it. You have a little blob of bits that could represent more than one type, and it's up to you to ensure you don't misinterpret them.
  This is how statically typed languages represent things in memory. Since the type system ensures at compile time that you aren't misinterpreting values, you don't need to validate it at runtime.
  - *It's compact.* You can't get any more efficient than storing just the bits you need for the value itself.
  - *It's fast.* Not having type tags implies you're not spending cycles checking them at runtime either.
  - *It's unsafe.* A bad chunk of bytecode that causes you to misinterpret a value and treat a number like a pointer or vice versa can violate the security of your game or make it crash.

- **An interface:**
  The object-oriented solution for a value that maybe be one of several different types is through polymorphism. An interface provides virtual methods for the various type tests and conversions, along the lines of:

  ```cpp
  class Value {
  public:
    virtual ~Value() {}

    virtual ValueType type() = 0;

    virtual int asInt() {
      // Can only call this on ints.
      assert(false);
      return 0;
    }

    // Other conversion methods...
  };
  ```

  Then you have concrete classes for each specific data type, like:

  ```cpp
  class IntValue : public Value {
  public:
    IntValue(int value)
    : value_(value)
    {}

    virtual ValueType type() { return TYPE_INT; }
    virtual int asInt() { return value_; }

  private:
    int value_;
  };
  ```

  - *It's open-ended.* You can define new value types outside of the core VM as long as they implement the base interface.
  - *It's object-oriented.* If you adhere to OOP principles, this does things the "right" way.
  - *It's verbose.* You have to define a separate class with all of the associated ceremonial verbiage for each data type.
  - *It's inefficient.* To get polymorphism, you have to go through a pointer, which means even tiny values like Booleans and numbers get wrapped in objects that are allocated on the heap. Every time you touch a value, you have to do a virtual method call.

My recommendation is that if you can stick with a single data type, do that. Otherwise, do a tagged union. That's what almost every language interpreter in the world does.

**CN:** 我们的示例虚拟机只处理一种值类型——整数。这让回答变得简单——栈就是一个`int`栈。一个更全功能的虚拟机将支持不同的数据类型：字符串、对象、列表等。你需要决定如何在内部存储它们。

- **单一数据类型：**
  - *简单。* 你不需要担心标签标记、转换或类型检查。
  - *无法处理不同的数据类型。* 这是明显的缺点。将不同类型塞入单一表示（如将数字存储为字符串）是在自找麻烦。

- **带标签的联合体（tagged variant）：**
  这是动态类型语言的常见表示。每个值有两部分。第一部分是一个类型标签——一个`enum`——标识存储的是什么数据类型。其余位根据该类型进行解释：
  ```cpp
  enum ValueType { TYPE_INT, TYPE_DOUBLE, TYPE_STRING };
  struct Value {
    ValueType type;
    union {
      int    intValue;
      double doubleValue;
      char*  stringValue;
    };
  };
  ```
  - *值知道自己的类型。* 这种表示的好处是可以在运行时检查值的类型。这对动态派发和确保不在不支持的类型上执行操作很重要。
  - *占用更多内存。* 每个值都需要携带额外比特来标识其类型。在像VM这样底层的系统中，这些比特会很快累积。

- **无标签联合体（untagged union）：**
  使用像前一种形式那样的联合体，但*没有*附带的类型标签。你有一小块比特，可能表示多种类型，由你来确保不会误解它们。这是静态类型语言在内存中的表示方式。
  - *紧凑。* 只存储值本身所需的比特，没有比这更高效的了。
  - *快速。* 没有类型标签意味着你不需要在运行时花周期检查它们。
  - *不安全。* 一段糟糕的字节码可能导致误解值，将数字当作指针或反之，从而破坏游戏安全或使其崩溃。

- **接口：**
  针对可能多种不同类型的值的面向对象解决方案是通过多态。接口为各种类型测试和转换提供虚方法。
  - *可扩展。* 只要实现基接口，就可以在核心VM之外定义新的值类型。
  - *面向对象。* 遵循OOP原则，"正确"地做事。
  - *冗长。* 需要为每个数据类型定义单独的类，附带所有的礼仪性代码。
  - *低效。* 为了实现多态，必须通过指针，即使像布尔值和数字这样的小值也被包装在堆上分配的对象中。每次操作值都需要一次虚方法调用。

我的建议是：如果能坚持单一数据类型，就那样做。否则，使用带标签的联合体。这几乎是世界上所有语言解释器采用的方式。

### How is the bytecode generated? / 如何生成字节码？

* I saved the most important question for last. I've walked you through the code to *consume* and *interpret* bytecode, but it's up to you to build something to *produce* it. The typical solution here is to write a compiler, but it's not the only option.

- **If you define a text-based language:**
  - *You have to define a syntax.* Both amateur and professional language designers categorically underestimate how difficult this is to do. Defining a grammar that makes parsers happy is easy. Defining one that makes *users* happy is *hard*.
  - *You have to implement a parser.* Despite their reputation, this part is pretty easy. Either use a parser generator like ANTLR or Bison, or hand-roll a little recursive descent one, and you're good to go.
  - *You have to handle syntax errors.* This is one of the most important and most difficult parts. When users make syntax and semantic errors — which they will, constantly — it's your job to guide them back onto the right path.
  - *It will likely turn off non-technical users.* Most non-programmers don't think of plaintext like that. To them, text files feel like filling in tax forms for an angry robotic auditor that yells at them if they forget a single semicolon.

- **If you define a graphical authoring tool:**
  - *You have to implement a user interface.* Buttons, clicks, drags, stuff like that. If you go down this route, it's important to treat designing the user interface as a core part of doing your job well.
  - *You have fewer error cases.* Because the user is building behavior interactively one step at a time, your application can guide them away from mistakes as soon as they happen.
  - *Portability is harder.* The nice thing about text compilers is that text files are universal. When you're building a UI, you have to choose which framework to use, and many of those are specific to one OS.

**CN:** 我把最重要的问题留到了最后。我已经带你了解了*消费*和*解释*字节码的代码，但由你来构建*产生*字节码的工具。典型的解决方案是编写编译器，但这并非唯一选项。

- **如果定义基于文本的语言：**
  - *需要定义语法。* 业余和专业语言设计师都系统性地低估了这有多难。定义让解析器开心的语法很容易。定义让*用户*开心的语法*很难*。
  - *需要实现解析器。* 尽管名声不好，但这部分相当容易。使用ANTLR或Bison等解析器生成器，或者手写一个递归下降解析器，就可以了。
  - *需要处理语法错误。* 这是最重要也是最困难的部分之一。当用户犯语法和语义错误时（他们*会*不断地犯），你的工作是引导他们回到正确的道路上。
  - *可能会吓跑非技术型用户。* 大多数非程序员不这样看待纯文本。对他们来说，文本文件就像是在为愤怒的机器人审计员填写税表——忘记一个分号就会被吼叫。

- **如果定义图形化创作工具：**
  - *需要实现用户界面。* 按钮、点击、拖拽等。如果走这条路，将用户界面设计视为做好本职工作的核心部分很重要。
  - *错误情况更少。* 因为用户逐步交互地构建行为，你的应用可以在错误发生时立即引导他们远离。
  - *可移植性更难。* 文本编译器的好处是文本文件是通用的。当构建UI时，你需要选择框架，而许多框架是特定于某个操作系统的。

---

## See Also / 参考

- This pattern's close sister is the Gang of Four's [Interpreter](http://en.wikipedia.org/wiki/Interpreter_pattern) pattern. Both give you a way to express composable behavior in terms of data.
- The [Lua](http://www.lua.org/) programming language is the most widely used scripting language in games. It's implemented internally as a very compact register-based bytecode VM.
- [Kismet](http://en.wikipedia.org/wiki/UnrealEd#Kismet) is a graphical scripting tool built into UnrealEd, the editor for the Unreal engine.
- My own little scripting language, [Wren](https://github.com/munificent/wren), is a simple stack-based bytecode interpreter.

- 这种模式近亲是GoF的[解释器模式](http://en.wikipedia.org/wiki/Interpreter_pattern)。两者都提供了一种用数据表达可组合行为的方式。
- [Lua](http://www.lua.org/)编程语言是游戏中最广泛使用的脚本语言。它内部实现为一个非常紧凑的寄存器式字节码虚拟机。
- [Kismet](http://en.wikipedia.org/wiki/UnrealEd#Kismet)是内置于UnrealEd（Unreal引擎的编辑器）中的图形化脚本工具。
- 作者自己的小脚本语言[Wren](https://github.com/munificent/wren)是一个简单的栈式字节码解释器。
---

## C# Equivalent (C# 对照实现)

```csharp
// 定义指令集枚举 —— 每个枚举值对应一个字节的操作码
// 通过枚举而非硬编码数值，提升可读性和可维护性
public enum Instruction : byte
{
    SetHealth      = 0x00,
    SetWisdom      = 0x01,
    SetAgility     = 0x02,
    PlaySound      = 0x03,
    SpawnParticles = 0x04,
    Literal        = 0x05, // 字面量：将字节流中的数值压入栈
    GetHealth      = 0x06,
    GetWisdom      = 0x07,
    GetAgility     = 0x08,
    Add            = 0x09, // 加法：弹出两个值，相加后压入结果
    Divide         = 0x0A  // 除法：弹出两个值，相除后压入结果
}

// 栈式虚拟机 —— 核心设计模式：用栈在指令间传递数据
// 所有指令从栈顶弹出操作数，计算结果压回栈顶
public class StackVM
{
    private const int MaxStack = 128;
    private readonly int[] _stack = new int[MaxStack];
    private int _stackSize;

    // 解释执行字节码 —— 逐条读取并派发指令
    // bytecode：字节数组，每条指令可能附带操作数
    public void Interpret(byte[] bytecode)
    {
        int ip = 0; // instruction pointer，指令指针

        while (ip < bytecode.Length)
        {
            Instruction op = (Instruction)bytecode[ip++];

            switch (op)
            {
                // 字面量指令：将下一个字节作为数值压入栈
                // 这是将"数据"引入栈的唯一方式
                case Instruction.Literal:
                    int value = bytecode[ip++]; // 从字节流读取值
                    Push(value);                // 压入栈顶
                    break;

                // SetHealth 弹出两个值：wizard索引 和 amount数值
                // 设计原因：调用者先压wizard索引再压amount值，所以先pop amount
                case Instruction.SetHealth:
                    int amount = Pop();
                    int wizard = Pop();
                    SetHealth(wizard, amount);
                    break;

                // GetHealth 弹出一个wizard索引，获取血量后压回栈
                // 这使得血量值可以被后续的算术指令使用
                case Instruction.GetHealth:
                    int wizIdx = Pop();
                    Push(GetHealth(wizIdx));
                    break;

                // 加法指令：弹出两个操作数，相加后压入结果
                // 栈式架构的优势 —— 无需指定操作数来源，操作数默认在栈顶
                case Instruction.Add:
                    int b = Pop();
                    int a = Pop();
                    Push(a + b);
                    break;

                // 除法指令：注意先弹出的是右操作数
                case Instruction.Divide:
                    int divisor = Pop();
                    int dividend = Pop();
                    Push(dividend / divisor);
                    break;

                case Instruction.PlaySound:
                    int soundId = Pop();
                    PlaySound(soundId);
                    break;

                case Instruction.SpawnParticles:
                    int particleType = Pop();
                    SpawnParticles(particleType);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown instruction: {op}");
            }
        }
    }

    // 压栈操作 —— 需检查栈溢出
    private void Push(int value)
    {
        if (_stackSize >= MaxStack)
            throw new StackOverflowException("VM stack overflow");
        _stack[_stackSize++] = value;
    }

    // 弹栈操作 —— 需检查栈空
    private int Pop()
    {
        if (_stackSize <= 0)
            throw new InvalidOperationException("VM stack underflow");
        return _stack[--_stackSize];
    }

    // ========== 外部原语 (Primitives) ==========
    // 这些方法是虚拟机与游戏引擎之间的桥梁
    // 通过限制可调用的原语集合，确保字节码无法越界操作

    private void SetHealth(int wizardIndex, int amount)
    {
        // 实际项目中应通过依赖注入访问游戏引擎
        Debug.Log($"Set wizard {wizardIndex} health to {amount}");
    }

    private int GetHealth(int wizardIndex)
    {
        Debug.Log($"Get wizard {wizardIndex} health");
        return 100; // 模拟返回
    }

    private void PlaySound(int soundId)
    {
        Debug.Log($"Play sound {soundId}");
    }

    private void SpawnParticles(int particleType)
    {
        Debug.Log($"Spawn particles type {particleType}");
    }
}

// ========== 使用示例 ==========
// 示例：将表达式 "setHealth(0, getHealth(0) + (getAgility(0) + getWisdom(0)) / 2)"
// 编译为字节码并执行
public class BytecodeExample
{
    private static readonly byte[] SpellBytecode = {
        (byte)Instruction.Literal, 0,        // 压入 wizard索引 0
        (byte)Instruction.Literal, 0,        // 再次压入 0（供getHealth使用）
        (byte)Instruction.GetHealth,          // 弹出0，获取血量45，压回栈
        (byte)Instruction.Literal, 0,        // 压入 0（供getAgility使用）
        (byte)Instruction.GetAgility,        // 弹出0，获取敏捷7，压回栈
        (byte)Instruction.Literal, 0,        // 压入 0（供getWisdom使用）
        (byte)Instruction.GetWisdom,         // 弹出0，获取智慧11，压回栈
        (byte)Instruction.Add,               // 弹出7和11，相加得18，压回栈
        (byte)Instruction.Literal, 2,        // 压入除数2
        (byte)Instruction.Divide,            // 弹出18和2，相除得9，压回栈
        (byte)Instruction.Add,               // 弹出45和9，相加得54，压回栈
        (byte)Instruction.SetHealth          // 弹出0和54，调用setHealth(0, 54)
    };

    public static void Run()
    {
        var vm = new StackVM();
        vm.Interpret(SpellBytecode);
        // 最终效果：wizard 0 的血量被设置为 45 + (7 + 11) / 2 = 54
    }
}
```

---

## Unity Application / Unity 应用场景

- **PlayableGraph / Playables API**: Unity's PlayableGraph is a bytecode-like system where nodes (Playables) are wired together to define behavior (animation mixing, audio sequencing) as data. This separates content from runtime logic.
- **Custom Scripting Systems**: Many Unity games (e.g., visual scripting tools like Bolt/PlayMaker) encode player-authored logic as bytecode-like instruction sets executed by a runtime interpreter, enabling designer-driven gameplay without C# recompilation.
- **AssetBundle Hot-Reload**: Behavior defined as bytecode data can be shipped in AssetBundles and swapped at runtime for patching or modding, without touching the main game binary.
- **AI Behavior Trees**: Instead of hard-coding AI logic, store behavior tree definitions as bytecode serialized into ScriptableObjects, interpreted at runtime — enabling designer iteration without programmer involvement.

- **PlayableGraph / Playables API**: Unity 的 PlayableGraph 就像字节码系统 —— Playables 节点像指令一样组合成行为（动画混合、音频排序），完全用数据驱动，将内容与运行时逻辑分离。
- **自定义脚本系统**: 许多 Unity 游戏（如 Bolt、PlayMaker）将用户编写的逻辑编码为字节码指令集，由运行时解释器执行，让设计者无需编译 C# 即可实现玩法逻辑。
- **AssetBundle 热重载**: 行为编码为字节码数据后，可通过 AssetBundle 发布，运行时直接替换，实现热更新和模组功能，无需修改主程序。
- **AI 行为树**: 将行为树定义为字节码并序列化到 ScriptableObject 中，运行时解释执行 —— 设计者可以独立迭代 AI 逻辑，无需程序员介入。
---

## Key Differences / 关键差异

| Aspect | C++ (原书) | C# (Unity) |
|--------|-----------|------------|
| **Memory Safety** | `char[]` bytecode requires manual bounds checking | `byte[]` + index checks + exceptions provide safer execution |
| **Stack Type** | `int` stack only, untyped | Can use `object[]` or generic `Stack<T>` for type flexibility |
| **IP Management** | Manual `i++` inside loop | C# `foreach` or `while` with explicit `ip` for readability |
| **Error Handling** | `assert()` aborts on failure | Exceptions enable graceful fallback and logging |
| **Serialization** | Raw binary arrays | `BinaryWriter/BinaryReader` + `[Serializable]` for Unity-friendly persistence |
| **Hot-Reload** | Rebuild executable required | C# reflection + AssetBundle + `MonoBehaviour` reloading makes hot-swap natural |

---

# Subclass Sandbox — 子类沙箱

> **EN:** Define behavior in a subclass using a set of operations provided by its base class.
> **CN:** 通过基类提供的一组操作，在子类中定义行为。

---

## Intent / 意图

* Define behavior in a subclass using a set of operations provided by its base class.

* 使用基类提供的一组操作在子类中定义行为。

---

## Motivation / 动机

* Every kid has dreamed of being a superhero, but unfortunately, cosmic rays are in short supply here on Earth. Games that let you pretend to be a superhero are the closest approximation. Because our game designers have never learned to say, "no", *our* superhero game aims to feature dozens, if not hundreds, of different superpowers that heroes may choose from.

* 每个孩子都梦想过成为超级英雄，但不幸的是，地球上的宇宙射线供应短缺。让你扮演超级英雄的游戏是最接近的替代品。因为我们的游戏设计师从未学会说"不"，*我们的*超级英雄游戏的目标是提供几十甚至数百种不同的超能力供英雄选择。

---

* Our plan is that we'll have a `Superpower` base class. Then, we'll have a derived class that implements each superpower. We'll divvy up the design doc among our team of programmers and get coding. When we're done, we'll have a hundred superpower classes.

* 我们的计划是拥有一个 `Superpower` 基类，然后每个超能力都有一个派生类来实现。我们将把设计文档分给我们的程序员团队并开始编码。完成后，我们将有一百个超能力类。

---

* When you find yourself with a *lot* of subclasses, like in this example, that often means a data-driven approach is better. Instead of lots of *code* for defining different powers, try finding a way to define that behavior in *data* instead.

* 当你发现自己像这个例子一样有*大量*的子类时，这通常意味着数据驱动的方法更好。与其用大量的*代码*来定义不同的能力，不如尝试用*数据*来定义行为。

Patterns like [Type Object](type-object.html), [Bytecode](bytecode.html), and [Interpreter](http://en.wikipedia.org/wiki/Interpreter_pattern) can all help.

像 [Type Object](type-object.html)、[Bytecode](bytecode.html) 和 [Interpreter](http://en.wikipedia.org/wiki/Interpreter_pattern) 这样的模式都能有所帮助。

---

* We want to immerse our players in a world teeming with variety. Whatever power they dreamed up when they were a kid, we want in our game. That means these superpower subclasses will be able to do just about everything: play sounds, spawn visual effects, interact with AI, create and destroy other game entities, and mess with physics. There's no corner of the codebase that they won't touch.

* 我们想让玩家沉浸在一个充满多样性的世界中。无论他们小时候幻想过什么能力，我们都想在游戏中实现。这意味着这些超能力子类几乎可以做任何事情：播放声音、生成视觉效果、与AI交互、创建和销毁其他游戏实体、以及搞乱物理系统。代码库中没有他们不会触及的角落。

---

* Let's say we unleash our team and get them writing superpower classes. What's going to happen?

* 假设我们放开团队，让他们编写超能力类。会发生什么？

- *There will be lots of redundant code.* While the different powers will be wildly varied, we can still expect plenty of overlap. Many of them will spawn visual effects and play sounds in the same way. A freeze ray, heat ray, and Dijon mustard ray are all pretty similar when you get down to it. If the people implementing those don't coordinate, there's going to be a lot of duplicate code and effort.

- *会有大量冗余代码。* 虽然不同的能力会千差万别，但我们仍然可以预期会有很多重叠。它们中的许多会以相同的方式生成视觉效果和播放声音。冰冻射线、热射线和第戎芥末射线，归根结底都非常相似。如果实现这些的人不协调，就会产生大量的重复代码和工作量。

- *Every part of the game engine will get coupled to these classes.* Without knowing better, people will write code that calls into subsystems that were never meant to be tied directly to the superpower classes. If our renderer is organized into several nice neat layers, only one of which is intended to be used by code outside of the graphics engine, we can bet that we'll end up with superpower code that pokes into every one of them.

- *游戏引擎的每个部分都会与这些类耦合。* 不了解情况的人会编写调用那些本不该直接与超能力类绑定的子系统的代码。如果我们的渲染器被组织成几个整洁的层，其中只有一个层打算供图形引擎外部的代码使用，我们可以肯定最终会有超能力代码深入到每一个层中。

- *When these outside systems need to change, odds are good some random superpower code will get broken.* Once we have different superpower classes coupling themselves to various and sundry parts of the game engine, it's inevitable that changes to those systems will impact the power classes. That's no fun because your graphics, audio, and UI programmers probably don't want to also have to be gameplay programmers *too*.

- *当这些外部系统需要改变时，很可能会破坏某些随机的超能力代码。* 一旦不同的超能力类与游戏引擎的各种各样部分耦合，对这些系统的更改不可避免地会影响到能力类。这可不有趣，因为你的图形、音频和UI程序员可能也不想*还要*成为游戏玩法程序员。

- *It's hard to define invariants that all superpowers obey.* Let's say we want to make sure that all audio played by our powers gets properly queued and prioritized. There's no easy way to do that if our hundred classes are all directly calling into the sound engine on their own.

- *很难定义所有超能力都遵守的不变约束。* 假设我们想确保所有由能力播放的音频都能正确排队和优先级排序。如果我们的上百个类都在各自直接调用音频引擎，就没有简单的方法做到这一点。

---

* What we want is to give each of the gameplay programmers who is implementing a superpower a set of primitives they can play with. You want your power to play a sound? Here's your `playSound()` function. You want particles? Here's `spawnParticles()`. We'll make sure these operations cover everything you need to do so that you don't need to `#include` random headers and nose your way into the rest of the codebase.

* 我们想要的是给每个实现超能力的游戏玩法程序员一组他们可以使用的原语。你希望你的能力播放声音？这里是你的 `playSound()` 函数。你想要粒子效果？这里是 `spawnParticles()`。我们会确保这些操作覆盖你需要做的一切，这样你就不需要 `#include` 随机的头文件并窥探代码库的其他部分。

---

* We do this by making these operations *protected methods of the* `Superpower` *base class*. Putting them in the base class gives every power subclass direct, easy access to the methods. Making them protected (and likely non-virtual) communicates that they exist specifically to be *called* by subclasses.

* 我们通过将这些操作设为 `Superpower` *基类的受保护方法*来实现这一点。将它们放在基类中，让每个能力子类都能直接、轻松地访问这些方法。将它们设为受保护（并且通常是不可虚的）传达了它们专门为被子类*调用*而存在。

---

* Once we have these toys to play with, we need a place to use them. For that, we'll define a *sandbox method*, an abstract protected method that subclasses must implement. Given those, to implement a new kind of power, you:

* 一旦我们有了这些可以玩的工具，我们需要一个使用它们的地方。为此，我们将定义一个*沙箱方法*，一个子类必须实现的抽象受保护方法。有了这些，要实现一种新的能力，你需要：

1. Create a new class that inherits from `Superpower`.

1. 创建一个继承自 `Superpower` 的新类。

2. Override `activate()`, the sandbox method.

2. 重写 `activate()`，即沙箱方法。

3. Implement the body of that by calling the protected methods that `Superpower` provides.

3. 通过调用 `Superpower` 提供的受保护方法来实现其主体。

---

* We can fix our redundant code problem now by making those provided operations as high-level as possible. When we see code that's duplicated between lots of the subclasses, we can always roll it up into `Superpower` as a new operation that they can all use.

* 我们现在可以通过使这些提供操作尽可能高层来修复冗余代码问题。当我们看到许多子类之间重复的代码时，我们总是可以将其归纳到 `Superpower` 中，作为一个所有子类都可以使用的新操作。

---

* We've addressed our coupling problem by constraining the coupling to one place. `Superpower` itself will end up coupled to the different game systems, but our hundred derived classes will not. Instead, they are *only* coupled to their base class. When one of those game systems changes, modification to `Superpower` may be necessary, but dozens of subclasses shouldn't have to be touched.

* 我们通过将耦合约束到一个地方来解决耦合问题。`Superpower` 本身最终会与不同的游戏系统耦合，但我们的上百个派生类不会。相反，它们*只*与它们的基类耦合。当其中一个游戏系统发生变化时，可能需要对 `Superpower` 进行修改，但几十个子类不应该需要被触及。

---

* This pattern leads to an architecture where you have a shallow but wide class hierarchy. Your inheritance chains aren't *deep*, but there are a *lot* of classes that hang off `Superpower`. By having a single class with a lot of direct subclasses, we have a point of leverage in our codebase. Time and love that we put into `Superpower` can benefit a wide set of classes in the game.

* 这个模式导致了一种浅而宽的类层次结构。你的继承链不*深*，但有*很多*类挂在 `Superpower` 下。通过让一个类拥有大量直接子类，我们在代码库中有了一个杠杆点。我们投入在 `Superpower` 上的时间和心血可以使游戏中的一大类群受益。

---

* Lately, you find a lot of people criticizing inheritance in object-oriented languages. Inheritance *is* problematic — there's really no deeper coupling in a codebase than the one between a base class and its subclass — but I find *wide* inheritance trees to be easier to work with than *deep* ones.

* 最近，你会发现很多人在批评面向对象语言中的继承。继承*确实*有问题——在代码库中，真的没有比基类和其子类之间更深的耦合了——但我发现*宽*的继承树比*深*的更容易处理。

---

## The Pattern / 模式

* A **base class** defines an abstract **sandbox method** and several **provided operations**. Marking them protected makes it clear that they are for use by derived classes. Each derived **sandboxed subclass** implements the sandbox method using the provided operations.

* **基类**定义了一个抽象的**沙箱方法**和几个**提供操作**。将它们标记为受保护的，明确了它们供派生类使用。每个派生的**沙箱子类**使用提供操作来实现沙箱方法。

---

## When to Use It / 何时使用

* The Subclass Sandbox pattern is a very simple, common pattern lurking in lots of codebases, even outside of games. If you have a non-virtual protected method laying around, you're probably already using something like this. Subclass Sandbox is a good fit when:

* 子类沙箱模式是一个非常简单、常见的模式，潜伏在许多代码库中，甚至在游戏之外。如果你有一个非虚的受保护方法放在那里，你可能已经在使用类似的东西了。子类沙箱在以下情况很适用：

- You have a base class with a number of derived classes.

- 你有一个基类，带有许多派生类。

- The base class is able to provide all of the operations that a derived class may need to perform.

- 基类能够提供派生类可能需要执行的所有操作。

- There is behavioral overlap in the subclasses and you want to make it easier to share code between them.

- 子类中存在行为重叠，你希望更容易地在它们之间共享代码。

- You want to minimize coupling between those derived classes and the rest of the program.

- 你想要最小化这些派生类与程序其余部分之间的耦合。

---

## Keep in Mind / 牢记

* "Inheritance" is a bad word in many programming circles these days, and one reason is that base classes tend to accrete more and more code. This pattern is particularly susceptible to that.

* 如今"继承"在许多编程圈子里是个不好的词，原因之一是基类倾向于积累越来越多的代码。这个模式特别容易受到这个问题的影响。

---

* Since subclasses go through their base class to reach the rest of the game, the base class ends up coupled to every system *any* derived class needs to talk to. Of course, the subclasses are also intimately tied to their base class. That spiderweb of coupling makes it very hard to change the base class without breaking something — you've got the [brittle base class problem](http://en.wikipedia.org/wiki/Fragile_base_class).

* 由于子类通过它们的基类来访问游戏的其他部分，基类最终会与*任何*派生类需要通信的每个系统耦合。当然，子类也与它们的基类紧密绑定。这种耦合的蜘蛛网使得在不破坏某些东西的情况下很难更改基类——这就是[脆弱的基类问题](http://en.wikipedia.org/wiki/Fragile_base_class)。

---

* The flip side of the coin is that since most of your coupling has been pushed up to the base class, the derived classes are now much more cleanly separated from the rest of the world. Ideally, most of your behavior will be in those subclasses. That means much of your codebase is isolated and easier to maintain.

* 另一面是，既然大部分耦合已经被推到了基类，派生类现在与世界其他地方更加清晰地分离。理想情况下，你的大部分行为将在那些子类中。这意味着你的大部分代码库是隔离的，更易于维护。

---

* Still, if you find this pattern is turning your base class into a giant bowl of code stew, consider pulling some of the provided operations out into separate classes that the base class can dole out responsibility to. The [Component](component.html) pattern can help here.

* 不过，如果你发现这个模式正在把你的基类变成一大锅代码杂烩，考虑将一些提供操作提取到单独的类中，让基类可以分配责任给它们。[Component](component.html) 模式在这里可以提供帮助。

---

## Sample Code / 示例代码

* Because this is such a simple pattern, there isn't much to the sample code. That doesn't mean it isn't useful — the pattern is about the *intent*, not the complexity of its implementation.

* 因为这是一个非常简单的模式，所以示例代码并不多。这并不意味着它没有用——这个模式是关于*意图*的，而不是其实现在复杂性。

---

* We'll start with our `Superpower` base class:

* 我们从 `Superpower` 基类开始：

```cpp
class Superpower {
public:
  virtual ~Superpower() {}

protected:
  virtual void activate() = 0;

  void move(double x, double y, double z)
  {
    // Code here...
  }

  void playSound(SoundId sound, double volume)
  {
    // Code here...
  }

  void spawnParticles(ParticleType type, int count)
  {
    // Code here...
  }
};
```

* The `activate()` method is the sandbox method. Since it is virtual and abstract, subclasses *must* override it. This makes it clear to someone creating a power subclass where their work has to go.

* `activate()` 方法是沙箱方法。由于它是虚的和抽象的，子类*必须*重写它。这让创建能力子类的人清楚地知道他们的工作必须放在哪里。

---

* The other protected methods, `move()`, `playSound()`, and `spawnParticles()`, are the provided operations. These are what the subclasses will call in their implementation of `activate()`.

* 其他受保护方法 `move()`、`playSound()` 和 `spawnParticles()` 是提供操作。这些是子类在实现 `activate()` 时将调用的方法。

---

* We didn't implement the provided operations in this example, but an actual game would have real code there. Those methods are where `Superpower` gets coupled to other systems in the game — `move()` may call into physics code, `playSound()` will talk to the audio engine, etc. Since this is all in the *implementation* of the base class, it keeps that coupling encapsulated within `Superpower` itself.

* 在这个例子中我们没有实现提供操作，但实际的游戏会有真正的代码在那里。这些方法是 `Superpower` 与游戏中其他系统耦合的地方——`move()` 可能调用物理代码，`playSound()` 将与音频引擎通信，等等。由于这一切都在基类的*实现*中，它使得耦合被封装在 `Superpower` 自身内部。

---

* OK, now let's get our radioactive spiders out and create a power. Here's one:

* 好了，现在让我们放出我们的放射性蜘蛛，创建一个能力。这里有一个：

```cpp
class SkyLaunch : public Superpower
{
protected:
  virtual void activate()
  {
    // Spring into the air.
    playSound(SOUND_SPROING, 1.0f);
    spawnParticles(PARTICLE_DUST, 10);
    move(0, 0, 20);
  }
};
```

* OK, maybe being able to *jump* isn't all that *super*, but I'm trying to keep things basic here.

* 好吧，也许能够*跳跃*并不那么*超级*，但我在这里尽量保持基础。

---

* This power springs the superhero into the air, playing an appropriate sound and kicking up a little cloud of dust. If all of the superpowers were this simple — just a combination of sound, particle effect, and motion — then we wouldn't need this pattern at all. Instead, `Superpower` could have a baked-in implementation of `activate()` that accesses fields for the sound ID, particle type, and movement. But that only works when every power essentially works the same way with only some differences in data. Let's elaborate on it a bit:

* 这种能力将超级英雄弹射到空中，播放适当的声音并扬起一小团灰尘。如果所有的超能力都这么简单——只是声音、粒子效果和运动的组合——那么我们根本就不需要这个模式。相反，`Superpower` 可以有一个内置的 `activate()` 实现，访问声音ID、粒子类型和移动的字段。但这只在每个能力本质上以相同方式工作，只在数据上有些差异时才有效。让我们稍微详细说明一下：

```cpp
class Superpower {
protected:
  double getHeroX()
  {
    // Code here...
  }

  double getHeroY()
  {
    // Code here...
  }

  double getHeroZ()
  {
    // Code here...
  }

  // Existing stuff...
};
```

* Here, we've added a couple of methods to get the hero's position. Our `SkyLaunch` subclass can now use those:

* 这里，我们添加了几个方法来获取英雄的位置。我们的 `SkyLaunch` 子类现在可以使用它们：

```cpp
class SkyLaunch : public Superpower
{
protected:
  virtual void activate()
  {
    if (getHeroZ() == 0)
    {
      // On the ground, so spring into the air.
      playSound(SOUND_SPROING, 1.0f);
      spawnParticles(PARTICLE_DUST, 10);
      move(0, 0, 20);
    }
    else if (getHeroZ() < 10.0f)
    {
      // Near the ground, so do a double jump.
      playSound(SOUND_SWOOP, 1.0f);
      move(0, 0, getHeroZ() + 20);
    }
    else
    {
      // Way up in the air, so do a dive attack.
      playSound(SOUND_DIVE, 0.7f);
      spawnParticles(PARTICLE_SPARKLES, 1);
      move(0, 0, -getHeroZ());
    }
  }
};
```

* Since we have access to some state, now our sandbox method can do actual, interesting control flow. Here, it's still just a couple of simple `if` statements, but you can do anything you want. By having the sandbox method be an actual full-fledged method that contains arbitrary code, the sky's the limit.

* 既然我们可以访问一些状态，现在我们的沙箱方法可以执行实际、有趣的控制流了。这里仍然只是几个简单的 `if` 语句，但你可以做任何你想做的事情。通过让沙箱方法成为一个包含任意代码的真正成熟的方法，天空才是极限。

---

* Earlier, I suggested a data-driven approach for powers. This is one reason why you may decide *not* to do that. If your behavior is complex and imperative, it is more difficult to define in data.

* 之前，我建议过对能力使用数据驱动的方法。这就是你可能决定*不*这样做的原因之一。如果你的行为是复杂和命令式的，那么用数据来定义就更困难了。

---

## Design Decisions / 设计决策

* As you can see, Subclass Sandbox is a fairly "soft" pattern. It describes a basic idea, but it doesn't have a lot of detailed mechanics. That means you'll be making some interesting choices each time you apply it. Here are some questions to consider.

* 如你所见，子类沙箱是一个相当"软"的模式。它描述了一个基本思想，但没有很多详细的机制。这意味着你每次应用它时都会做一些有趣的选择。这里有一些需要考虑的问题。

---

### What operations should be provided? / 应该提供哪些操作？

* This is the biggest question. It deeply affects how this pattern feels and how well it works. At the minimal end of the spectrum, the base class doesn't provide *any* operations. It just has a sandbox method. To implement it, you'll have to call into systems outside of the base class. If you take that angle, it's probably not even fair to say you're using this pattern.

* 这是最大的问题。它深刻影响了这个模式的感觉和效果。在最简端，基类不提供*任何*操作。它只有一个沙箱方法。要实现它，你必须调用基类外部的系统。如果你采取这个角度，甚至可能不能说你在使用这个模式。

---

* On the other end of the spectrum, the base class provides *every* operation that a subclass may need. Subclasses are *only* coupled to the base class and don't call into any outside systems whatsoever.

* 在另一端，基类提供了子类可能需要的*所有*操作。子类*只*与基类耦合，不调用任何外部系统。

---

* Concretely, this means each source file for a subclass would only need a single `#include` — the one for its base class.

* 具体来说，这意味着子类的每个源文件只需要一个 `#include`——即其基类的头文件。

---

* Between these two points, there's a wide middle ground where some operations are provided by the base class and others are accessed directly from the outside system that defines it. The more operations you provide, the less coupled subclasses are to outside systems, but the *more* coupled the base class is. It removes coupling from the derived classes, but it does so by pushing that up to the base class itself.

* 在这两点之间，有一个广阔的中间地带，其中一些操作由基类提供，另一些则直接从定义它们的外部系统访问。你提供的操作越多，子类与外部系统的耦合就越少，但基类的耦合就越*多*。它从派生类中移除了耦合，但这是通过将耦合推到基类自身来实现的。

---

* That's a win if you have a bunch of derived classes that were all coupled to some outside system. By moving the coupling up into a provided operation, you've centralized it into one place: the base class. But the more you do this, the bigger and harder to maintain that one class becomes.

* 如果你有一堆都耦合到某个外部系统的派生类，那这就是个胜利。通过将耦合上移到提供操作中，你将其集中到了一个地方：基类。但你做得越多，这个类就变得越大越难以维护。

---

* So where should you draw the line? Here are a few rules of thumb:

* 那么你应该在哪里划线呢？这里有几个经验法则：

- If a provided operation is only used by one or a few subclasses, you don't get a lot of bang for your buck. You're adding complexity to the base class, which affects everyone, but only a couple of classes benefit.

- 如果一个提供操作只被一个或少数子类使用，你就没有得到很多性价比。你在向基类添加复杂性，这会影响每个人，但只有少数类受益。

This may be worth it to make the operation consistent with other provided operations, or it may be simpler and cleaner to let those special case subclasses call out to the external systems directly.

这可能值得为了让操作与其他提供操作保持一致，或者让那些特殊情况的子类直接调用外部系统可能更简单、更清晰。

- When you call a method in some other corner of the game, it's less intrusive if that method doesn't modify any state. It still creates a coupling, but it's a "safe" coupling because it can't break anything in the game.

- 当你在游戏的某个其他角落调用方法时，如果该方法不修改任何状态，其侵入性较小。它仍然会创建耦合，但这是一个"安全"的耦合，因为它不会破坏游戏中的任何东西。

"Safe" is in quotes here because technically, even just accessing data can cause problems. If your game is multi-threaded, you could read something at the same time that it's being modified. If you aren't careful, you could end up with bogus data.

这里的"安全"加了引号，因为从技术上讲，即使是只访问数据也可能引起问题。如果你的游戏是多线程的，你可能会在数据被修改的同时读取它。如果你不小心，最终可能会得到虚假数据。

Another nasty case is if your game state is strictly deterministic (which many online games are in order to keep players in sync). If you access something outside of the set of synchronized game state, you can cause incredibly painful non-determinism bugs.

另一个讨厌的情况是，如果你的游戏状态是严格确定性的（许多在线游戏为了保持玩家同步而如此）。如果你访问了同步游戏状态集之外的某些东西，你可能导致极其痛苦的非确定性错误。

Calls that do modify state, on the other hand, more deeply tie you to those parts of the codebase, and you need to be much more cognizant of that. That makes them good candidates for being rolled up into provided operations in the more visible base class.

另一方面，确实修改状态的调用会更深入地将你绑定到代码库的那些部分，你需要更加意识到这一点。这使得它们成为被归纳到更可见基类中的提供操作的好候选。

- If the implementation of a provided operation only forwards a call to some outside system, then it isn't adding much value. In that case, it may be simpler to call the outside method directly.

- 如果一个提供操作的实现只是将调用转发到某个外部系统，那么它并没有增加多少价值。在这种情况下，直接调用外部方法可能更简单。

However, even simple forwarding can still be useful — those methods often access state that the base class doesn't want to directly expose to subclasses. For example, let's say `Superpower` provided this:

然而，即使是简单的转发仍然可能有用——这些方法通常访问基类不想直接暴露给子类的状态。例如，假设 `Superpower` 提供了这个：

```cpp
void playSound(SoundId sound, double volume)
{
  soundEngine_.play(sound, volume);
}
```

It's just forwarding the call to some `soundEngine_` field in `Superpower`. The advantage, though, is that it keeps that field encapsulated in `Superpower` so subclasses can't poke at it.

它只是将调用转发给 `Superpower` 中的某个 `soundEngine_` 字段。不过，好处是它将该字段封装在 `Superpower` 中，这样子类就不能随意触碰它了。

---

### Should methods be provided directly, or through objects that contain them? / 应该直接提供方法，还是通过包含它们的对象提供？

* The challenge with this pattern is that you can end up with a painfully large number of methods crammed into your base class. You can mitigate that by moving some of those methods over to other classes. The provided operations in the base class then just return one of those objects.

* 这个模式的挑战在于，你最终可能将大量方法塞进基类中。你可以通过将其中一些方法转移到其他类来缓解这个问题。然后基类中的提供操作只返回这些对象之一。

---

* For example, to let a power play sounds, we could add these directly to `Superpower`:

* 例如，为了让能力播放声音，我们可以直接将这些添加到 `Superpower`：

```cpp
class Superpower {
protected:
  void playSound(SoundId sound, double volume)
  {
    // Code here...
  }

  void stopSound(SoundId sound)
  {
    // Code here...
  }

  void setVolume(SoundId sound)
  {
    // Code here...
  }

  // Sandbox method and other operations...
};
```

* But if `Superpower` is already getting large and unwieldy, we might want to avoid that. Instead, we create a `SoundPlayer` class that exposes that functionality:

* 但如果 `Superpower` 已经变得庞大而笨重，我们可能想要避免这样做。相反，我们创建一个 `SoundPlayer` 类来暴露该功能：

```cpp
class SoundPlayer {
  void playSound(SoundId sound, double volume)
  {
    // Code here...
  }

  void stopSound(SoundId sound)
  {
    // Code here...
  }

  void setVolume(SoundId sound)
  {
    // Code here...
  }
};
```

* Then `Superpower` provides access to it:

* 然后 `Superpower` 提供对它的访问：

```cpp
class Superpower {
protected:
  SoundPlayer& getSoundPlayer()
  {
    return soundPlayer_;
  }

  // Sandbox method and other operations...

private:
  SoundPlayer soundPlayer_;
};
```

* Shunting provided operations into auxiliary classes like this can do a few things for you:

* 将提供操作分流到这样的辅助类中可以为你做几件事：

- *It reduces the number of methods in the base class.* In the example here, we went from three methods to just a single getter.

- *它减少了基类中的方法数量。* 在这个例子中，我们从三个方法减少到只有一个 getter。

- *Code in the helper class is usually easier to maintain.* Core base classes like `Superpower`, despite our best intentions, tend to be tricky to change since so much depends on them. By moving functionality over to a less coupled secondary class, we make that code easier to poke at without breaking things.

- *辅助类中的代码通常更容易维护。* 像 `Superpower` 这样的核心基类，尽管我们意图良好，但往往很难更改，因为太多东西依赖于它们。通过将功能移到一个耦合度较低的次类，我们使这些代码更容易修改而不会破坏东西。

- *It lowers the coupling between the base class and other systems.* When `playSound()` was a method directly on `Superpower`, our base class was directly tied to `SoundId` and whatever audio code the implementation called into. Moving that over to `SoundPlayer` reduces `Superpower`'s coupling to the single `SoundPlayer` class, which then encapsulates all of its other dependencies.

- *它降低了基类与其他系统之间的耦合。* 当 `playSound()` 是 `Superpower` 上的直接方法时，我们的基类直接与 `SoundId` 以及实现所调用的任何音频代码绑定。将其移到 `SoundPlayer` 将 `Superpower` 的耦合减少到单一的 `SoundPlayer` 类，然后该类封装了它的所有其他依赖。

---

### How does the base class get the state that it needs? / 基类如何获取它需要的状态？

* Your base class will often need some data that it wants to encapsulate and keep hidden from its subclasses. In our first example, the `Superpower` class provided a `spawnParticles()` method. If the implementation of that needs some particle system object, how would it get one?

* 你的基类通常需要一些它想要封装并对子类隐藏的数据。在我们的第一个例子中，`Superpower` 类提供了一个 `spawnParticles()` 方法。如果该方法需要某个粒子系统对象，它如何获取一个？

- **Pass it to the base class constructor:**

- **将其传递给基类构造函数：**

The simplest solution is to have the base class take it as a constructor argument:

最简单的解决方案是让基类将其作为构造函数参数接收：

```cpp
class Superpower {
public:
  Superpower(ParticleSystem* particles)
  : particles_(particles)
  {}

  // Sandbox method and other operations...

private:
  ParticleSystem* particles_;
};
```

This safely ensures that every superpower does have a particle system by the time it's constructed. But let's look at a derived class:

这安全地确保了每个超能力在构造时确实拥有一个粒子系统。但让我们看看派生类：

```cpp
class SkyLaunch : public Superpower {
public:
  SkyLaunch(ParticleSystem* particles)
  : Superpower(particles)
  {}
};
```

Here we see the problem. Every derived class will need to have a constructor that calls the base class one and passes along that argument. That exposes every derived class to a piece of state that we don't want them to know about.

这里我们看到了问题。每个派生类都需要有一个构造函数来调用基类构造函数并传递该参数。这暴露了每个派生类到我们不想让它们知道的状态。

This is also a maintenance headache. If we later add another piece of state to the base class, every constructor in each of our derived classes will have to be modified to pass it along.

这也是维护上的头疼问题。如果我们稍后向基类添加另一个状态，每个派生类中的每个构造函数都必须修改以传递它。

- **Do two-stage initialization:**

- **进行两阶段初始化：**

To avoid passing everything through the constructor, we can split initialization into two steps. The constructor will take no parameters and just create the object. Then, we call a separate method defined directly on the base class to pass in the rest of the data that it needs:

为了避免通过构造函数传递所有内容，我们可以将初始化分为两步。构造函数不带参数，只创建对象。然后，我们调用直接在基类上定义的单独方法来传入它需要的其余数据：

```cpp
Superpower* power = new SkyLaunch();
power->init(particles);
```

Note here that since we aren't passing anything into the constructor for `SkyLaunch`, it isn't coupled to anything we want to keep private in `Superpower`. The trouble with this approach, though, is that you have to make sure you always remember to call `init()`. If you ever forget, you'll have a power that's in some twilight half-created state and won't work.

注意这里，由于我们没有向 `SkyLaunch` 的构造函数传递任何东西，它没有与我们想在 `Superpower` 中保持私有的任何东西耦合。不过，这种方法的问题在于，你必须确保总是记得调用 `init()`。如果你忘记了，你将拥有一个处于某种朦胧的半创建状态且无法工作的能力。

You can fix that by encapsulating the entire process into a single function, like so:

你可以通过将整个过程封装到一个单独的函数中来修复这个问题，像这样：

```cpp
Superpower* createSkyLaunch(ParticleSystem* particles)
{
  Superpower* power = new SkyLaunch();
  power->init(particles);
  return power;
}
```

With a little trickery like private constructors and friend classes, you can ensure this `createSkylaunch()` function is the *only* function that can actually create powers. That way, you can't forget any of the initialization stages.

通过一些技巧，如私有构造函数和友元类，你可以确保这个 `createSkyLaunch()` 函数是唯一*能*实际创建能力的函数。这样，你就不会忘记任何初始化阶段。

- **Make the state static:**

- **将状态设为静态：**

In the previous example, we were initializing each `Superpower` *instance* with a particle system. That makes sense when every power needs its own unique state. But let's say that the particle system is a [singleton](singleton.html), and every power will be sharing the same state.

在前面的例子中，我们为每个 `Superpower` *实例*初始化了一个粒子系统。这在每个能力需要自己独特的状态时是有意义的。但假设粒子系统是一个[单例](singleton.html)，每个能力都将共享相同的状态。

In that case, we can make the state private to the base class and also make it *static*. The game will still have to make sure that it initializes the state, but it only has to initialize the `Superpower` *class* once for the entire game, and not each instance.

在这种情况下，我们可以将状态设为基类的私有成员，并使其*静态*。游戏仍然需要确保它初始化该状态，但它只需要为整个游戏初始化 `Superpower` *类*一次，而不是每个实例。

Keep in mind that this still has many of the problems of a singleton. You've got some state shared between lots and lots of objects (all of the `Superpower` instances). The particle system is encapsulated, so it isn't globally *visible*, which is good, but it can still make reasoning about powers harder because they can all poke at the same object.

请记住，这仍然有许多单例的问题。你有大量对象（所有 `Superpower` 实例）之间共享的状态。粒子系统被封装了，所以它不是全局*可见*的，这很好，但它仍然可能使对能力的推理更加困难，因为它们都可以操作同一个对象。

```cpp
class Superpower {
public:
  static void init(ParticleSystem* particles)
  {
    particles_ = particles;
  }

  // Sandbox method and other operations...

private:
  static ParticleSystem* particles_;
};
```

Note here that `init()` and `particles_` are both static. As long as the game calls `Superpower::init()` once early on, every power can access the particle system. At the same time, `Superpower` instances can be created freely by calling the right derived class's constructor.

注意这里，`init()` 和 `particles_` 都是静态的。只要游戏在早期调用一次 `Superpower::init()`，每个能力都可以访问粒子系统。同时，`Superpower` 实例可以通过调用正确的派生类构造函数自由创建。

Even better, now that `particles_` is a *static* variable, we don't have to store it for each instance of `Superpower`, so we've made the class use less memory.

更好的是，既然 `particles_` 是一个*静态*变量，我们不必为每个 `Superpower` 实例存储它，所以我们使该类使用了更少的内存。

- **Use a service locator:**

- **使用服务定位器：**

The previous option requires that outside code specifically remembers to push in the state that the base class needs before it needs it. That places the burden of initialization on the surrounding code. Another option is to let the base class handle it by pulling in the state it needs. One way to do that is by using the [Service Locator](service-locator.html) pattern:

前一个选项要求外部代码特别记住在需要之前推入基类所需的状态。这将初始化的负担放在了周围代码上。另一个选项是让基类通过拉取它需要的状态来处理。一种方法是使用[服务定位器](service-locator.html)模式：

```cpp
class Superpower {
protected:
  void spawnParticles(ParticleType type, int count)
  {
    ParticleSystem& particles = Locator::getParticles();
    particles.spawn(type, count);
  }

  // Sandbox method and other operations...
};
```

Here, `spawnParticles()` needs a particle system. Instead of being *given* one by outside code, it fetches one itself from the service locator.

这里，`spawnParticles()` 需要一个粒子系统。它不是由外部代码*提供*一个，而是自己从服务定位器中获取一个。

---

## See Also / 参见

- When you apply the [Update Method](update-method.html) pattern, your update method will often also be a sandbox method.
- This pattern is a role reversal of the [Template Method](http://en.wikipedia.org/wiki/Template_method_pattern) pattern. In both patterns, you implement a method using a set of primitive operations. With Subclass Sandbox, the method is in the derived class and the primitive operations are in the base class. With Template Method, the *base* class has the method and the primitive operations are implemented by the *derived* class.
- You can also consider this a variation on the [Facade](http://en.wikipedia.org/wiki/Facade_Pattern) pattern. That pattern hides a number of different systems behind a single simplified API. With Subclass Sandbox, the base class acts as a facade that hides the entire game engine from the subclasses.

- 当你应用[更新方法](update-method.html)模式时，你的更新方法通常也是一个沙箱方法。
- 这个模式是[模板方法](http://en.wikipedia.org/wiki/Template_method_pattern)模式的角色反转。在这两个模式中，你都使用一组原语操作来实现一个方法。在子类沙箱中，方法在派生类中，原语操作在基类中。在模板方法中，*基*类拥有方法，原语操作由*派生*类实现。
- 你也可以将其视为[外观](http://en.wikipedia.org/wiki/Facade_Pattern)模式的一个变体。该模式将许多不同的系统隐藏在一个简化的 API 后面。在子类沙箱中，基类充当一个外观，向子类隐藏整个游戏引擎。
---

## C# Equivalent (C# 对照实现)

```csharp
/// <summary>
/// 基类 —— 定义沙箱方法和提供操作
/// 设计要点：
/// 1. activate() 为 abstract，强制子类实现具体行为
/// 2. 所有提供操作为 protected，限定子类只能通过基类访问外部系统
/// 3. 提供操作设计为非 virtual (sealed)，防止子类重写破坏封装
/// </summary>
public abstract class Superpower
{
    // ======== 沙箱方法 (Sandbox Method) ========
    // abstract 关键字强制每个子类必须提供自己的实现
    public abstract void Activate();

    // ======== 提供操作 (Provided Operations) ========
    // protected sealed — 子类可调用但不可重写
    // 这样保证所有子类通过相同途径访问外部系统

    protected sealed void Move(Vector3 delta)
    {
        // 封装对 Unity Physics / Transform 的调用
        // 子类不需要知道具体如何移动，只需调用此方法
        Debug.Log($"Move by {delta}");
        // 实际项目中: transform.Translate(delta);
    }

    protected sealed void PlaySound(SoundId sound, float volume = 1.0f)
    {
        // 封装对 AudioSource 或 AudioManager 的调用
        // 确保所有音效经过统一的优先级/音量管理
        Debug.Log($"Play sound {sound} at volume {volume}");
        // 实际项目中: AudioManager.Play(sound, volume);
    }

    protected sealed void SpawnParticles(ParticleType type, int count = 1)
    {
        // 封装对 ParticleSystem 的调用
        // 确保粒子效果符合性能预算和视觉规范
        Debug.Log($"Spawn {count} particles of type {type}");
        // 实际项目中: ObjectPool.Spawn(type, count, transform.position);
    }

    // ======== 只读访问器 ========
    // 提供对游戏状态的受控访问，不暴露修改能力

    protected float HeroX => GetHeroPosition().x;
    protected float HeroY => GetHeroPosition().y;
    protected float HeroZ => GetHeroPosition().z;

    // 这个方法本身可以是 protected virtual，允许子类扩展
    // 但基类实现应提供默认的合理行为
    protected virtual Vector3 GetHeroPosition()
    {
        // 实际项目中通过 GameManager 或 Singleton 获取英雄位置
        return Vector3.zero;
    }
}

// ======== 具体子类 ========
// 子类不需要知道任何音频、物理、粒子系统的细节
// 只需组合基类提供的操作，像搭积木一样构建行为

public class SkyLaunch : Superpower
{
    public override void Activate()
    {
        // 注意：这里只能调用基类的 protected 方法
        // 无法直接访问 AudioSource、ParticleSystem 等 —— 这就是"沙箱"
        if (HeroZ == 0f)
        {
            // 地面跳跃
            PlaySound(SoundId.Sproing, 1.0f);
            SpawnParticles(ParticleType.Dust, 10);
            Move(new Vector3(0, 0, 20));
        }
        else if (HeroZ < 10f)
        {
            // 近地二段跳
            PlaySound(SoundId.Swoop, 1.0f);
            Move(new Vector3(0, 0, HeroZ + 20));
        }
        else
        {
            // 高空俯冲
            PlaySound(SoundId.Dive, 0.7f);
            SpawnParticles(ParticleType.Sparkles, 1);
            Move(new Vector3(0, 0, -HeroZ));
        }
    }
}

public class FreezeRay : Superpower
{
    public override void Activate()
    {
        // 冰冻射线：播放音效 + 粒子效果 + 冻结目标
        PlaySound(SoundId.FreezeBeam, 1.0f);
        SpawnParticles(ParticleType.Ice, 20);
        // 假设基类还提供了 FreezeTarget() 操作
        // Move() 在这里可能不需要
    }
}

// ======== 通过组件对象提供操作 ========
// 当基类的提供操作过多时，可将相关操作分组到独立类中
// 基类只暴露 getter，降低基类本身的耦合度

public class SoundPlayer
{
    public void Play(SoundId sound, float volume)
    {
        Debug.Log($"SoundPlayer: {sound} at {volume}");
    }

    public void Stop(SoundId sound) { }
    public void SetVolume(SoundId sound, float volume) { }
}

public abstract class SuperpowerV2
{
    // 将音效操作委托给 SoundPlayer 对象
    // 基类只需维护一个 SoundPlayer 引用
    protected SoundPlayer Sound { get; private set; }

    protected SuperpowerV2()
    {
        Sound = new SoundPlayer();
    }

    public abstract void Activate();
}

// 使用组件化基类的子类
public class ThunderStrike : SuperpowerV2
{
    public override void Activate()
    {
        Sound.Play(SoundId.Thunder, 1.0f);
        // 其他操作...
    }
}
```

## Unity Application / Unity 应用场景

- **MonoBehaviour as Sandbox Base**: Unity's `MonoBehaviour` is a real-world Subclass Sandbox. It provides protected operations like `Start()`, `Update()`, `OnCollisionEnter()` (sandbox methods) and access to `transform`, `gameObject`, `GetComponent<>()` (provided operations). Every custom script inherits from this sandbox.
- **ScriptableObject-powered Abilities**: A base `Ability` class (deriving from ScriptableObject) provides `ApplyDamage()`, `PlayVFX()`, `PlaySFX()` as sealed protected methods. Concrete abilities (Fireball, Heal, Shield) override `Execute()` — they never touch other systems directly.
- **PlayerInput Sandbox**: A base `InputHandler` class provides `GetMoveVector()`, `IsJumpPressed()`, `IsAttackHeld()` as provided operations. Subclasses (`PlayerCharacter`, `Vehicle`, `MenuNavigator`) implement `HandleInput()` without knowing input system internals.
- **Enemy AI Sandbox**: Base `EnemyBehavior` provides `MoveToward()`, `AttackTarget()`, `PlayAnimation()` as protected methods. Each enemy type (Goblin, Dragon, Slime) implements `ExecuteBehavior()` with no direct coupling to NavMeshAgent or Animator.

- **MonoBehaviour 作为沙箱基类**: Unity 的 `MonoBehaviour` 就是真实世界的子类沙箱。它提供 `Start()`、`Update()`、`OnCollisionEnter()`（沙箱方法）和 `transform`、`gameObject`、`GetComponent<>()`（提供操作）。每个自定义脚本都继承自这个沙箱。
- **ScriptableObject 技能系统**: 基类 `Ability`（继承 ScriptableObject）以 sealed protected 方式提供 `ApplyDamage()`、`PlayVFX()`、`PlaySFX()`。具体技能（火球、治疗、护盾）重写 `Execute()`，从不直接访问其他系统。
- **PlayerInput 沙箱**: 基类 `InputHandler` 提供 `GetMoveVector()`、`IsJumpPressed()`、`IsAttackHeld()` 作为提供操作。子类（`PlayerCharacter`、`Vehicle`、`MenuNavigator`）实现 `HandleInput()`，完全不知道输入系统的内部细节。
- **敌人 AI 沙箱**: 基类 `EnemyBehavior` 以受保护方法提供 `MoveToward()`、`AttackTarget()`、`PlayAnimation()`。每种敌人（哥布林、龙、史莱姆）实现 `ExecuteBehavior()`，无需直接耦合 NavMeshAgent 或 Animator。
## Key Differences / 关键差异

| Aspect | C++ (原书) | C# (Unity) |
|--------|-----------|------------|
| **Access Modifiers** | `protected` virtual methods | `protected sealed` on provided ops — C# can prevent subclass overriding |
| **Abstract Enforcement** | `= 0` pure virtual | `abstract` keyword more explicit |
| **Object Composition** | Class fields manually managed | Properties + auto-properties + constructor injection more idiomatic |
| **Helper Classes** | Forwarding methods in base | Composition with getter-only properties (`SoundPlayer` pattern) |
| **Unity Integration** | N/A | `MonoBehaviour` lifecycle (`Start`, `Update`) naturally serves as sandbox |
| **Sealed Control** | Not available | `sealed` keyword prevents subclasses from overriding provided operations |

---

# Type Object — 类型对象

> **EN:** Allow the flexible creation of new "classes" by creating a single class, each instance of which represents a different type of object.
>
> **CN:** 通过创建一个类，每个实例代表一种不同类型的对象，从而允许灵活地创建新的"类别"。

---

## Intent / 意图

* *Allow the flexible creation of new "classes" by creating a single class, each instance of which represents a different type of object.*

* *通过创建一个类，每个实例代表一种不同类型的对象，从而允许灵活地创建新的"类别"。*

---

## Motivation / 动机

Imagine we're working on a fantasy role-playing game. Our task is to write the code for the hordes of vicious monsters that seek to slay our brave hero. Monsters have a bunch of different attributes: health, attacks, graphics, sounds, etc., but for example purposes we'll just worry about the first two.

想象我们正在开发一款幻想角色扮演游戏。我们的任务是为那些试图杀死我们勇敢英雄的邪恶怪物大军编写代码。怪物有许多不同的属性：血量、攻击、图像、声音等，但为了示例，我们只关心前两个。

Each monster in the game has a value for its current health. It starts out full, and each time the monster is wounded, it diminishes. Monsters also have an attack string. When the monster attacks our hero, that text will be shown to the user somehow. (We don't care how here.)

游戏中的每个怪物都有一个当前血量值。它从满血开始，每次受伤都会减少。怪物还有一个攻击文本。当怪物攻击我们的英雄时，这段文本会以某种方式展示给用户。（我们这里不关心具体方式。）

The designers tell us that monsters come in a variety of different *breeds*, like "dragon" or "troll". Each breed describes a *kind* of monster that exists in the game, and there can be multiple monsters of the same breed running around in the dungeon at the same time.

设计人员告诉我们，怪物有多种不同的*种类*，比如"龙"或"巨魔"。每个种类描述了一种游戏中存在的怪物*类型*，同一个地牢中可以有多个同种类的怪物同时活动。

The breed determines a monster's starting health — dragons start off with more than trolls, making them harder to kill. It also determines the attack string — all monsters of the same breed attack the same way.

种类决定了怪物的初始血量——龙比巨魔初始血量更高，使得它们更难被杀死。它也决定了攻击文本——同一品种的所有怪物以相同方式攻击。
### The typical OOP answer / 典型的 OOP 方案

With that game design in mind, we fire up our text editor and start coding. According to the design, a dragon is a kind of monster, a troll is another kind, and so on with the other breeds. Thinking object-oriented, that leads us to a `Monster` base class:

带着这个游戏设计思路，我们打开文本编辑器开始编码。根据设计，龙是一种怪物，巨魔是另一种，其他种类也是如此。面向对象思维让我们想到了一个 `Monster` 基类：

```cpp
class Monster {
public:
  virtual ~Monster() {}
  virtual const char* getAttack() = 0;

protected:
  Monster(int startingHealth)
  : health_(startingHealth)
  {}

private:
  int health_; // Current health.
};
```

private:
  int health_; // 当前血量
};
```

This is the so-called "is-a" relationship. In conventional OOP thinking, since a dragon "is-a" monster, we model that by making `Dragon` a subclass of `Monster`. As we'll see, subclassing is only one way of enshrining a conceptual relation like that into code.

这就是所谓的"is-a"关系。在传统的 OOP 思维中，由于龙"is-a"怪物，我们通过让 `Dragon` 成为 `Monster` 的子类来建模。正如我们将看到的，子类化只是将这种概念关系编码进代码的一种方式。

The public `getAttack()` function lets the combat code get the string that should be displayed when the monster attacks the hero. Each derived breed class will override this to provide a different message.

公共的 `getAttack()` 函数让战斗代码获取当怪物攻击英雄时应显示的字符串。每个派生种类类都会重写这个函数以提供不同的消息。

The constructor is protected and takes the starting health for the monster. We'll have derived classes for each breed that provide their own public constructors that call this one, passing in the starting health that is appropriate for that breed.

构造函数是 protected 的，接受怪物的初始血量。我们会为每个种类创建派生类，它们提供自己的公共构造函数，传入该种类合适的初始血量。

Now let's see a couple of breed subclasses:

现在让我们看几个种类子类：

```cpp
class Dragon : public Monster {
public:
  Dragon() : Monster(230) {}

virtual const char* getAttack()
  {
    return "The dragon breathes fire!";
  }
};

class Troll : public Monster {
public:
  Troll() : Monster(48) {}

virtual const char* getAttack()
  {
    return "The troll clubs you!";
  }
};
```

Exclamation points make everything more exciting!

感叹号让一切更加刺激！

Each class derived from `Monster` passes in the starting health and overrides `getAttack()` to return the attack string for that breed. Everything works as expected, and before long, we've got our hero running around slaying a variety of beasties. We keep slinging code, and before we know it, we've got dozens of monster subclasses, from acidic slimes to zombie goats.

每个从 `Monster` 派生的类都传入初始血量并重写 `getAttack()` 以返回该种类的攻击字符串。一切按预期工作，不久之后，我们的英雄就开始四处斩杀各种怪物。我们继续写代码，不知不觉中，我们已经有了几十个怪物子类，从酸性史莱姆到僵尸山羊。

Then, strangely, things start to bog down. Our designers ultimately want to have *hundreds* of breeds, and we find ourselves spending all of our time writing these little seven-line subclasses and recompiling. It gets worse — the designers want to start tuning the breeds we've already coded. Our formerly productive workday degenerates to:

然后，奇怪的事情发生了——事情开始变得拖沓。我们的设计师最终想要有*数百个*种类，而我们发现自己所有的时间都花在编写这些只有七行的子类并重新编译上。更糟的是——设计师想开始调整我们已经编码过的种类。我们曾经高产的工作日退化成了：

1. Get email from designer asking to change health of troll from 48 to 52.
2. Check out and change `Troll.h`.
3. Recompile game.
4. Check in change.
5. Reply to email.
6. Repeat.

1. 收到设计师邮件要求把巨魔的血量从 48 改为 52。
2. 签出并修改 `Troll.h`。
3. 重新编译游戏。
4. 签入修改。
5. 回复邮件。
6. 重复。

We spend the day frustrated because we've turned into data monkeys. Our designers are frustrated because it takes them forever to get a simple number tuned. What we need is the ability to change breed stats without having to recompile the whole game every time. Even better, we'd like designers to be able to create and tune breeds without *any* programmer intervention at all.

我们整天感到沮丧，因为我们变成了数据猴子。我们的设计师感到沮丧，因为他们花很长时间才能调整一个简单的数值。我们需要的是能够在不每次重新编译整个游戏的情况下更改种类属性。更好的是，我们希望设计师能够*完全不需要程序员介入*就能创建和调整种类。
### A class for a class / 为类创建一个类

At a very high level, the problem we're trying to solve is pretty simple. We have a bunch of different monsters in the game, and we want to share certain attributes between them. A horde of monsters are beating on the hero, and we want some of them to use the same text for their attack. We define that by saying that all of those monsters are the same "breed", and that the breed determines the attack string.

从较高的层面来看，我们试图解决的问题相当简单。游戏中有许多不同的怪物，我们希望它们之间共享某些属性。一群怪物正在攻击英雄，我们希望其中一些怪物使用相同的攻击文本。我们通过说所有这些怪物都是同一个"breed"来定义这一点，而 breed 决定了攻击字符串。

We decided to implement this concept using inheritance since it lines up with our intuition of classes. A dragon is a monster, and each dragon in the game is an instance of this dragon "class". Defining each breed as a subclass of an abstract base `Monster` class, and having each monster in the game be an instance of that derived breed class mirrors that. We end up with a class hierarchy like this:

我们决定使用继承来实现这个概念，因为它符合我们对类的直觉。龙是一种怪物，游戏中的每条龙都是这个龙"类"的实例。将每个种类定义为抽象基类 `Monster` 的子类，让游戏中的每个怪物都是该派生种类类的实例——这映照了上述关系。我们最终得到了这样的类层次结构：

![A Monster base class with derived classes for Dragon, Troll, etc.](images/type-object-subclasses.png)

![具有 Dragon、Troll 等派生类的 Monster 基类](images/type-object-subclasses.png)

Here, the ![A UML arrow representing inheritance.](images/arrow-inherits.png) means "inherits from".

这里的 ![表示继承的 UML 箭头](images/arrow-inherits.png) 表示"继承自"。

Each instance of a monster in the game will be of one of the derived monster types. The more breeds we have, the bigger the class hierarchy. That's the problem of course: adding new breeds means adding new code, and each breed has to be compiled in as its own type.

游戏中的每个怪物实例都将是派生怪物类型之一。我们拥有的种类越多，类层次结构就越大。这当然是问题所在：添加新种类意味着添加新代码，每个种类都必须作为自己的类型编译进去。

This works, but it isn't the only option. We could also architect our code so that each monster *has* a breed. Instead of subclassing `Monster` for each breed, we have a single `Monster` class and a single `Breed` class:

这种方式可行，但它不是唯一的选择。我们也可以这样架构代码：让每个怪物*拥有*一个种类。我们不为每个种类子类化 `Monster`，而是使用一个单一的 `Monster` 类和一个单一的 `Breed` 类：

![A Monster object has a reference to a Breed object.](images/type-object-breed.png)

![Monster 对象持有对 Breed 对象的引用](images/type-object-breed.png)

Here, the ![A UML arrow for an object reference.](images/arrow-references.png) means "is referenced by".

这里的 ![表示对象引用的 UML 箭头](images/arrow-references.png) 表示"被引用"。

That's it. Two classes. Notice that there's no inheritance at all. With this system, each monster in the game is simply an instance of class `Monster`. The `Breed` class contains the information that's shared between all monsters of the same breed: starting health and the attack string.

就是这样。两个类。注意完全没有继承。使用这个系统，游戏中的每个怪物都只是 `Monster` 类的一个实例。`Breed` 类包含同一品种所有怪物之间共享的信息：初始血量和攻击字符串。

To associate monsters with breeds, we give each `Monster` instance a reference to a `Breed` object containing the information for that breed. To get the attack string, a monster just calls a method on its breed. The `Breed` class essentially defines a monster's "type". Each breed instance is an *object* that represents a different conceptual *type*, hence the name of the pattern: Type Object.

为了将怪物与种类关联起来，我们给每个 `Monster` 实例一个对包含该种类信息的 `Breed` 对象的引用。为了获取攻击字符串，怪物只需调用其 breed 上的方法。`Breed` 类本质上定义了一个怪物的"类型"。每个 breed 实例都是一个代表不同概念*类型*的*对象*，因此得名：Type Object（类型对象）。

What's especially powerful about this pattern is that now we can define new *types* of things without complicating the codebase at all. We've essentially lifted a portion of the type system out of the hard-coded class hierarchy into data we can define at runtime.

这个模式特别强大的地方在于，现在我们可以在完全不使代码库复杂化的情况下定义新的*类型*。我们实质上是将类型系统的一部分从硬编码的类层次结构中提升到了可以在运行时定义的数据中。

We can create hundreds of different breeds by instantiating more instances of `Breed` with different values. If we create breeds by initializing them from data read from some configuration file, we have the ability to define new types of monsters completely in data. So easy, a designer could do it!

我们可以通过实例化更多具有不同值的 `Breed` 实例来创建数百个不同的种类。如果我们通过从某个配置文件中读取数据来初始化品种，我们就拥有了完全在数据中定义新怪物类型的能力。如此简单，设计人员都能做到！
---

## The Pattern / 模式

Define a **type object** class and a **typed object** class. Each type object instance represents a different logical type. Each typed object stores a **reference to the type object that describes its type**.

定义一个**类型对象**类和一个**类型化对象**类。每个类型对象实例代表一个不同的逻辑类型。每个类型化对象存储一个对**描述其类型的类型对象的引用**。

Instance-specific data is stored in the typed object instance, and data or behavior that should be shared across all instances of the same conceptual type is stored in the type object. Objects referencing the same type object will function as if they were the same type. This lets us share data and behavior across a set of similar objects, much like subclassing lets us do, but without having a fixed set of hard-coded subclasses.

实例特有的数据存储在类型化对象实例中，而应在同一概念类型的所有实例间共享的数据或行为存储在类型对象中。引用相同类型对象的对象将表现得如同它们是相同类型一样。这让我们可以在相似对象集合间共享数据和行为，很像子类化所做的，但无需一组固定的硬编码子类。
---

## When to Use It / 何时使用

This pattern is useful anytime you need to define a variety of different "kinds" of things, but baking the kinds into your language's type system is too rigid. In particular, it's useful when either of these is true:

当你需要定义各种不同的"种类"，但将这些种类硬编码进语言的类型系统过于僵化时，此模式非常有用。尤其当以下任一条件成立时：

- You don't know what types you will need up front. (For example, what if our game needed to support downloading content that contained new breeds of monsters?)

- 你事先不知道你需要什么类型。（例如，如果我们的游戏需要支持下载包含新怪物种类的内容怎么办？）

- You want to be able to modify or add new types without having to recompile or change code.

- 你希望能够修改或添加新类型，而无需重新编译或更改代码。
---

## Keep in Mind / 牢记

This pattern is about moving the definition of a "type" from the imperative but rigid language of code into the more flexible but less behavioral world of objects in memory. The flexibility is good, but you lose some things by hoisting your types into data.

这个模式将"类型"的定义从命令式但僵化的代码语言转移到更灵活但行为性更弱的内存对象世界。灵活性是好的，但将类型提升到数据中会让你失去一些东西。
### The type objects have to be tracked manually / 类型对象需要手动跟踪

One advantage of using something like C++'s type system is that the compiler handles all of the bookkeeping for the classes automatically. The data that defines each class is automatically compiled into the static memory segment of the executable and just works.

使用像 C++ 类型系统这样的一个优点是编译器自动处理类的所有簿记工作。定义每个类的数据自动编译到可执行文件的静态内存段中，直接工作。

With the Type Object pattern, we are now responsible for managing not only our monsters in memory, but also their *types* — we have to make sure all of the breed objects are instantiated and kept in memory as long as our monsters need them. Whenever we create a new monster, it's up to us to ensure that it's correctly initialized with a reference to a valid breed.

使用 Type Object 模式，我们现在不仅负责管理内存中的怪物，还负责它们的*类型*——我们必须确保所有 breed 对象被实例化并在怪物需要它们时保持在内存中。每当我们创建一个新怪物时，我们需要确保它被正确地用一个有效 breed 的引用初始化。

We've freed ourselves from some of the limitations of the compiler, but the cost is that we have to re-implement some of what it used to be doing for us.

我们已经从编译器的某些限制中解放出来，但代价是我们必须重新实现一些编译器过去为我们做的事情。

Under the hood, C++ virtual methods are implemented using something called a "virtual function table", or just "vtable". A vtable is a simple `struct` containing a set of function pointers, one for each virtual method in a class. There is one vtable in memory for each class. Each instance of a class has a pointer to the vtable for its class.

在底层，C++ 虚方法是通过一种称为"虚函数表"或简称"vtable"的东西实现的。vtable 是一个简单的 `struct`，包含一组函数指针，每个指针对应类中的一个虚方法。每个类在内存中有一个 vtable。类的每个实例都有一个指向其类 vtable 的指针。

When you call a virtual function, the code first looks up the vtable for the object, then it calls the function stored in the appropriate function pointer in the table.

当你调用虚函数时，代码首先查找对象的 vtable，然后调用表中对应函数指针存储的函数。

Sound familiar? The vtable is our breed object, and the pointer to the vtable is the reference the monster holds to its breed. C++ classes are the Type Object pattern applied to C, handled automatically by the compiler.

听起来熟悉吗？vtable 就是我们的 breed 对象，指向 vtable 的指针就是怪物持有的对 breed 的引用。C++ 类就是应用于 C 的 Type Object 模式，由编译器自动处理。
### It's harder to define behavior for each type / 为每种类型定义行为更难

With subclassing, you can override a method and do whatever you want to — calculate values procedurally, call other code, etc. The sky is the limit. We could define a monster subclass whose attack string changed based on the phase of the moon if we wanted to. (Handy for werewolves, I suppose.)

使用子类化，你可以重写一个方法并做任何你想做的事——程序化计算值、调用其他代码等。天空才是极限。我们可以定义一个怪物子类，其攻击字符串根据月亮相位变化（我猜对狼人来说很方便）。

When we use the Type Object pattern instead, we replace an overridden method with a member variable. Instead of having monster subclasses that override a method to *calculate* an attack string using different *code*, we have a breed object that *stores* an attack string in a different *variable*.

当我们使用 Type Object 模式时，我们用成员变量替换了重写的方法。不再是让怪物子类通过不同的*代码*重写方法来*计算*攻击字符串，而是让 breed 对象在不同的*变量*中*存储*攻击字符串。

This makes it very easy to use type objects to define type-specific *data*, but hard to define type-specific *behavior*. If, for example, different breeds of monster needed to use different AI algorithms, using this pattern becomes more challenging.

这使得使用类型对象定义类型特定的*数据*非常容易，但定义类型特定的*行为*却很困难。例如，如果不同种类的怪物需要使用不同的 AI 算法，使用此模式就变得更具挑战性。

There are a couple of ways we can get around this limitation. A simple solution is to have a fixed set of pre-defined behaviors and then use data in the type object to simply *select* one of them. For example, let's say our monster AI will always be either "stand still", "chase hero", or "whimper and cower in fear" (hey, they can't all be mighty dragons). We can define functions to implement each of those behaviors. Then, we can associate an AI algorithm with a breed by having it store a pointer to the appropriate function.

有几种方法可以绕过这个限制。一个简单的解决方案是使用一组固定的预定义行为，然后使用类型对象中的数据简单地*选择*其中之一。例如，假设我们的怪物 AI 总是要么"站着不动"，要么"追逐英雄"，要么"呜咽恐惧畏缩"（嘿，它们不能都是强大的龙）。我们可以定义函数来实现每种行为。然后，我们可以通过让 breed 存储指向适当函数的指针来将 AI 算法与品种关联起来。

Sound familiar again? Now we're back to really implementing vtables in *our* type objects.

听起来又熟悉了吗？现在我们回到了真正在我们*自己的*类型对象中实现 vtable。

Another more powerful solution is to actually support defining behavior completely in data. The [Interpreter](http://c2.com/cgi-bin/wiki?InterpreterPattern) and [Bytecode](bytecode.html) patterns both let us build objects that represent behavior. If we read in a data file and use that to create a data structure for one of these patterns, we've moved the behavior's definition completely out of code and into content.

另一个更强大的解决方案是实际上支持完全在数据中定义行为。[Interpreter](http://c2.com/cgi-bin/wiki?InterpreterPattern) 和 [Bytecode](bytecode.html) 模式都让我们构建代表行为的对象。如果我们读入一个数据文件并用它来为这些模式之一创建数据结构，我们就把行为的定义完全从代码移到了内容中。

Over time, games are getting more data-driven. Hardware gets more powerful, and we find ourselves limited more by how much content we can author than how hard we can push the hardware. With a 64K cartridge, the challenge was *cramming* the gameplay into it. With a double-sided DVD, the challenge is *filling* it with gameplay.

随着时间的推移，游戏变得越来越数据驱动。硬件越来越强大，我们发现限制我们的更多是我们能创作多少内容，而不是我们能多用力地推动硬件。对于 64K 卡带，挑战是将游戏性*塞进去*。对于双层 DVD，挑战是*用游戏性填满它*。

Scripting languages and other higher-level ways of defining game behavior can give us a much needed productivity boost, at the expense of less optimal runtime performance. Since hardware keeps getting better but our brainpower doesn't, that trade-off starts to make more and more sense.

脚本语言和其他定义游戏行为的更高级别方式可以给我们带来急需的生产力提升，代价是运行时性能较低。由于硬件在不断变好，而我们的脑力没有，这种权衡开始变得越来越有意义。
---

## Sample Code / 示例代码

For our first pass at an implementation, let's start simple and build the basic system described in the motivation section. We'll start with the `Breed` class:

对于我们的第一遍实现，让我们从简单开始，构建动机部分描述的基本系统。我们从 `Breed` 类开始：

```cpp
class Breed {
public:
  Breed(int health, const char* attack)
  : health_(health),
    attack_(attack)
  {}

int getHealth() { return health_; }
  const char* getAttack() { return attack_; }

private:
  int health_; // Starting health.
  const char* attack_;
};
```
Very simple. It's basically just a container for two data fields: the starting health and the attack string. Let's see how monsters use it:

非常简单。它基本上只是一个包含两个数据字段的容器：初始血量和攻击字符串。让我们看看怪物如何使用它：

```cpp
class Monster {
public:
  Monster(Breed& breed)
  : health_(breed.getHealth()),
    breed_(breed)
  {}

const char* getAttack()
  {
    return breed_.getAttack();
  }

private:
  int    health_; // Current health.
  Breed& breed_;
};
```
When we construct a monster, we give it a reference to a breed object. This defines the monster's breed instead of the subclasses we were previously using. In the constructor, `Monster` uses the breed to determine its starting health. To get the attack string, the monster simply forwards the call to its breed.

当我们构造一个怪物时，我们给它一个对 breed 对象的引用。这定义了怪物的种类，而不是我们之前使用的子类。在构造函数中，`Monster` 使用 breed 来确定其初始血量。为了获取攻击字符串，怪物简单地将调用转发给其 breed。

This very simple chunk of code is the core idea of the pattern. Everything from here on out is bonus.

这段非常简单的代码是模式的核心思想。从这里开始的一切都是额外的好处。
### Making type objects more like types: constructors / 让类型对象更像类型：构造函数

With what we have now, we construct a monster directly and are responsible for passing in its breed. This is a bit backwards from how regular objects are instantiated in most OOP languages — we don't usually allocate a blank chunk of memory and then *give* it its class. Instead, we call a constructor function on the class itself, and it's responsible for giving us a new instance.

使用我们现在的代码，我们直接构造一个怪物并负责传入它的 breed。这与大多数 OOP 语言中常规对象的实例化方式有点相反——我们通常不会分配一块空白内存然后*给*它一个类。相反，我们在类本身上调用构造函数，它负责给我们一个新实例。

We can apply this same pattern to our type objects:

我们可以将同样的模式应用到我们的类型对象上：

```cpp
class Breed {
public:
  Monster* newMonster() { return new Monster(*this); }

// Previous Breed code...
};
```
"Pattern" is the right word here. What we're talking about is one of the classic patterns from Design Patterns: [Factory Method](http://c2.com/cgi/wiki?FactoryMethodPattern).

"模式"这个词用在这里很恰当。我们所谈论的是来自 Design Patterns 的经典模式之一：[Factory Method](http://c2.com/cgi/wiki?FactoryMethodPattern)。

In some languages, this pattern is applied for constructing *all* objects. In Ruby, Smalltalk, Objective-C, and other languages where classes are objects, you construct new instances by calling a method on the class object itself.

在某些语言中，这种模式被用于构造*所有*对象。在 Ruby、Smalltalk、Objective-C 和其他类即对象的语言中，你通过调用类对象本身的方法来构造新实例。

And the class that uses them:

使用它们的类：

```cpp
class Monster {
  friend class Breed;

public:
  const char* getAttack() { return breed_.getAttack(); }

private:
  Monster(Breed& breed)
  : health_(breed.getHealth()),
    breed_(breed)
  {}

int health_; // Current health.
  Breed& breed_;
};
```
The key difference is the `newMonster()` function in `Breed`. That's our "constructor" factory method. With our original implementation, creating a monster looked like:

关键区别在于 `Breed` 中的 `newMonster()` 函数。那是我们的"构造函数"工厂方法。使用我们最初的实现，创建怪物看起来像：

There's another minor difference here. Because the sample code is in C++, we can use a handy little feature: *friend classes.*

这里还有另一个小区别。因为示例代码是 C++，我们可以使用一个便捷的小特性：*友元类*。

We've made `Monster`'s constructor private, which prevents anyone from calling it directly. Friend classes sidestep that restriction so `Breed` can still access it. This means the *only* way to create monsters is by going through `newMonster()`.

我们已经将 `Monster` 的构造函数设为 private，防止任何人直接调用它。友元类绕过了这个限制，所以 `Breed` 仍然可以访问它。这意味着创建怪物的*唯一*方式是通过 `newMonster()`。

```cpp
Monster* monster = new Monster(someBreed);
```

After our changes, it's like this:

经过我们的修改后，变成这样：

```cpp
Monster* monster = someBreed.newMonster();
```

```cpp
Monster* monster = someBreed.newMonster();
```
So, why do this? There are two steps to creating an object: allocation and initialization. `Monster`'s constructor lets us do all of the initialization we need. In our example, that's only storing the breed, but a full game would be loading graphics, initializing the monster's AI, and doing other set-up work.

那么，为什么要这样做？创建对象有两个步骤：分配和初始化。`Monster` 的构造函数让我们能够完成所有需要的初始化。在我们的示例中，这只是存储 breed，但一个完整的游戏会加载图形、初始化怪物的 AI 以及其他设置工作。

However, that all happens *after* allocation. We've already got a chunk of memory to put our monster into before its constructor is called. In games, we often want to control that aspect of object creation too: we'll typically use things like custom allocators or the [Object Pool](object-pool.html) pattern to control where in memory our objects end up.

然而，所有这些都发生在*分配之后*。在调用构造函数之前，我们已经有一块内存来放置我们的怪物。在游戏中，我们通常也想控制对象创建的这方面：我们通常会使用像自定义分配器或 [Object Pool](object-pool.html) 模式来控制对象在内存中的位置。

Defining a "constructor" function in `Breed` gives us a place to put that logic. Instead of simply calling `new`, the `newMonster()` function can pull the memory from a pool or custom heap before passing control off to `Monster` for initialization. By putting this logic inside `Breed`, in the *only* function that has the ability to create monsters, we ensure that all monsters go through the memory management scheme we want.

在 `Breed` 中定义一个"构造函数"函数给了我们放置该逻辑的地方。`newMonster()` 函数不是简单地调用 `new`，而是可以从池或自定义堆中获取内存，然后再将控制权交给 `Monster` 进行初始化。通过将此逻辑放在 `Breed` 中——这个唯一有能力创建怪物的函数——我们确保所有怪物都经过我们想要的内存管理方案。
### Sharing data through inheritance / 通过继承共享数据

What we have so far is a perfectly serviceable type object system, but it's pretty basic. Our game will eventually have *hundreds* of different breeds, each with dozens of attributes. If a designer wants to tune all of the thirty different breeds of troll to make them a little stronger, she's got a lot of tedious data entry ahead of her.

我们目前拥有的是一个完全可用的类型对象系统，但它相当基础。我们的游戏最终会有*数百个*不同种类，每个都有几十个属性。如果设计师想要调整所有三十种不同巨魔种类让它们更强一点，她面前有大量繁琐的数据录入工作。

What would help is the ability to share attributes across multiple *breeds* in the same way that breeds let us share attributes across multiple *monsters*. Just like we did with our original OOP solution, we can solve this using inheritance. Only, this time, instead of using our language's inheritance mechanism, we'll implement it ourselves within our type objects.

有用的是能够在多个*种类*之间共享属性，就像种类让我们在多个*怪物*之间共享属性一样。就像我们用原始 OOP 解决方案所做的那样，我们可以使用继承来解决这个问题。只不过这次，我们不用语言的继承机制，而是在我们的类型对象内部自己实现它。

To keep things simple, we'll only support single inheritance. In the same way that a class can have a parent base class, we'll allow a breed to have a parent breed:

为了保持简单，我们只支持单继承。就像类可以有父基类一样，我们允许品种有父品种：

```cpp
class Breed {
public:
  Breed(Breed* parent, int health, const char* attack)
  : parent_(parent),
    health_(health),
    attack_(attack)
  {}

int         getHealth();
  const char* getAttack();

private:
  Breed*      parent_;
  int         health_; // Starting health.
  const char* attack_;
};
```
When we construct a breed, we give it a parent that it inherits from. We can pass in `NULL` for a base breed that has no ancestors.

当我们构造一个种类时，我们给它一个它继承的父种类。我们可以传入 `NULL` 表示没有祖先的基础种类。

To make this useful, a child breed needs to control which attributes are inherited from its parent and which attributes it overrides and specifies itself. For our example system, we'll say that a breed overrides the monster's health by having a non-zero value and overrides the attack by having a non-`NULL` string. Otherwise, the attribute will be inherited from its parent.

为了使其有用，子种类需要控制哪些属性从父种类继承，哪些属性自己重写并指定。对于我们的示例系统，我们规定种类通过非零值来重写怪物的血量，通过非 `NULL` 字符串来重写攻击。否则，属性将从其父种类继承。

There are two ways we can implement this. One is to handle the delegation dynamically every time the attribute is requested, like this:

有两种方式可以实现这一点。一种是在每次请求属性时动态处理委托，像这样：

```cpp
int Breed::getHealth() {
  // Override.
  if (health_ != 0 || parent_ == NULL) return health_;

// Inherit.
  return parent_->getHealth();
}

const char* Breed::getAttack() {
  // Override.
  if (attack_ != NULL || parent_ == NULL) return attack_;

// Inherit.
  return parent_->getAttack();
}
```
This has the advantage of doing the right thing if a breed is modified at runtime to no longer override, or no longer inherit some attribute. On the other hand, it takes a bit more memory (it has to retain a pointer to its parent), and it's slower. It has to walk the inheritance chain each time you look up an attribute.

这样做的好处是，如果品种在运行时被修改为不再重写或不再继承某些属性，它会做正确的事情。另一方面，它需要更多一点内存（必须保留指向其父类型的指针），而且速度更慢。每次查找属性时都必须遍历继承链。

If we can rely on a breed's attributes not changing, a faster solution is to apply the inheritance at *construction time*. This is called "copy-down" delegation because we *copy* inherited attributes *down* into the derived type when it's created. It looks like this:

如果我们可以依赖种类的属性不变，更快的解决方案是在*构造时*应用继承。这被称为"拷贝继承"（copy-down）委托，因为我们在创建时将继承的属性*拷贝到*派生类型中。看起来像这样：

```cpp
Breed(Breed* parent, int health, const char* attack)
: health_(health),
  attack_(attack)
{
  // Inherit non-overridden attributes.
  if (parent != NULL)
  {
    if (health == 0) health_ = parent->getHealth();
    if (attack == NULL) attack_ = parent->getAttack();
  }
}
```
Note that we no longer need a field for the parent breed. Once the constructor is done, we can forget the parent since we've already copied all of its attributes in. To access a breed's attribute, now we just return the field:

注意我们不再需要为父品种保留字段。一旦构造函数完成，我们可以忘记父品种，因为我们已经将其所有属性复制进来了。要访问种类的属性，现在只需返回字段：

```cpp
int         getHealth() { return health_; }
const char* getAttack() { return attack_; }
```

Nice and fast!

又好又快！

Let's say our game engine is set up to create the breeds by loading a JSON file that defines them. It could look like:

假设我们的游戏引擎设置为通过加载定义它们的 JSON 文件来创建种类。它可能看起来像这样：

```json
{
  "Troll": {
    "health": 25,
    "attack": "The troll hits you!"
  },
  "Troll Archer": {
    "parent": "Troll",
    "health": 0,
    "attack": "The troll archer fires an arrow!"
  },
  "Troll Wizard": {
    "parent": "Troll",
    "health": 0,
    "attack": "The troll wizard casts a spell on you!"
  }
}
```
We'd have a chunk of code that reads each breed entry and instantiates a new breed instance with its data. As you can see from the `"parent": "Troll"` fields, the `Troll Archer` and `Troll Wizard` breeds inherit from the base `Troll` breed.

我们会有一段代码读取每个种类条目并用其数据实例化一个新的种类实例。正如从 `"parent": "Troll"` 字段中看到的，`Troll Archer` 和 `Troll Wizard` 种类继承自基础 `Troll` 种类。

Since both of them have zero for their health, they'll inherit it from the base `Troll` breed instead. This means now our designer can tune the health in `Troll` and all three breeds will be updated. As the number of breeds and the number of different attributes each breed has increase, this can be a big time-saver. Now, with a pretty small chunk of code, we have an open-ended system that puts control in our designers' hands and makes the best use of their time. Meanwhile, we can get back to coding other features.

由于两者的血量都是 0，它们将从基类 `Troll` 继承血量。这意味着现在我们的设计师可以在 `Troll` 中调整血量，所有三个种类都会更新。随着种类数量和每个种类拥有不同属性的增加，这可以节省大量时间。现在，用相当小的代码量，我们拥有一个开放式的系统，将控制权交到设计师手中，充分利用他们的时间。同时，我们可以回去编写其他功能了。
---

## Design Decisions / 设计决策

The Type Object pattern lets us build a type system as if we were designing our own programming language. The design space is wide open, and we can do all sorts of interesting stuff.

Type Object 模式让我们可以像设计自己的编程语言一样构建类型系统。设计空间是开放的，我们可以做各种有趣的事情。

In practice, a few things curtail our fancy. Time and maintainability will discourage us from anything particularly complicated. More importantly, whatever type object system we design, our users (often non-programmers) will need to be able to easily understand it. The simpler we can make it, the more usable it will be. So what we'll cover here is the well-trodden design space, and we'll leave the far reaches for the academics and explorers.

在实践中，有几件事会限制我们的想象。时间和可维护性会阻止我们做任何特别复杂的事情。更重要的是，无论我们设计什么类型对象系统，我们的用户（通常是非程序员）都需要能够轻松理解它。我们做得越简单，它就越可用。因此，我们将在这里介绍的是经过广泛探索的设计空间，将遥远的前沿留给学者和探险家。
### Is the type object encapsulated or exposed? / 类型对象是封装的还是暴露的？

In our sample implementation, `Monster` has a reference to a breed, but it doesn't publicly expose it. Outside code can't get directly at the monster's breed. From the codebase's perspective, monsters are essentially typeless, and the fact that they have breeds is an implementation detail.

在我们的示例实现中，`Monster` 拥有对 breed 的引用，但没有公开暴露它。外部代码不能直接获取怪物的 breed。从代码库的角度看，怪物本质上是无类型的，它们拥有 breed 的事实是实现细节。

We can easily change this and allow `Monster` to return its `Breed`:

我们可以轻松改变这一点，允许 `Monster` 返回其 `Breed`：

```cpp
class Monster {
public:
  Breed& getBreed() { return breed_; }

// Existing code...
};
```
As in other examples in this book, we're following a convention where we return objects by reference instead of pointer to indicate to users that `NULL` will never be returned.

就像本书其他示例一样，我们遵循一个惯例：通过引用而非指针返回对象，以向用户表明永远不会返回 `NULL`。

Doing this changes the design of `Monster`. The fact that all monsters have breeds is now a publicly visible part of its API. There are benefits with either choice.

这样做改变了 `Monster` 的设计。所有怪物都有 breed 的事实现在是其 API 中公开可见的部分。两种选择各有好处。

- **If the type object is encapsulated:**
  - *The complexity of the Type Object pattern is hidden from the rest of the codebase.* It becomes an implementation detail that only the typed object has to worry about.
  - *The typed object can selectively override behavior from the type object.* Let's say we wanted to change the monster's attack string when it's near death. Since the attack string is always accessed through `Monster`, we have a convenient place to put that code:
    ```cpp
    const char* Monster::getAttack() {
      if (health_ < LOW_HEALTH)
      {
        return "The monster flails weakly.";
      }

- **如果类型对象是封装的：**
  - *Type Object 模式的复杂性对代码库其余部分隐藏。* 它成为只有类型化对象需要关心的实现细节。
  - *类型化对象可以有选择地重写类型对象的行为。* 假设我们想在怪物濒死时改变攻击字符串。由于攻击字符串总是通过 `Monster` 访问，我们有一个方便的地方放置该代码：
    ```cpp
    const char* Monster::getAttack() {
      if (health_ < LOW_HEALTH)
      {
        return "The monster flails weakly.";
      }

return breed_.getAttack();
    }
    ```
    If outside code was calling `getAttack()` directly on the breed, we wouldn't have the opportunity to insert that logic.
  - *We have to write forwarding methods for everything the type object exposes.* This is the tedious part of this design. If our type object class has a large number of methods, the object class will have to have its own methods for each of the ones that we want to be publicly visible.

return breed_.getAttack();
    }
    ```
    如果外部代码直接调用 breed 的 `getAttack()`，我们就不会有插入该逻辑的机会。
  - *我们必须为类型对象暴露的所有内容编写转发方法。* 这是此设计中繁琐的部分。如果我们的类型对象类有大量方法，对象类必须为我们希望公开可见的每个方法编写自己的方法。

- **If the type object is exposed:**
  - *Outside code can interact with type objects without having an instance of the typed class.* If the type object is encapsulated, there's no way to use it without also having a typed object that wraps it. This prevents us, for example, from using our constructor pattern where new monsters are created by calling a method on the breed. If users can't get to breeds directly, they wouldn't be able to call it.
  - *The type object is now part of the object's public API.* In general, narrow interfaces are easier to maintain than wide ones — the less you expose to the rest of the codebase, the less complexity and maintenance you have to deal with. By exposing the type object, we widen the object's API to include everything the type object provides.

- **如果类型对象是暴露的：**
  - *外部代码可以在没有类型化类实例的情况下与类型对象交互。* 如果类型对象是封装的，不通过包装它的类型化对象就无法使用它。例如，这会阻止我们使用构造函数模式（通过调用 breed 上的方法来创建新怪物）。如果用户不能直接访问 breed，他们就无法调用它。
  - *类型对象现在是对象公共 API 的一部分。* 通常，窄接口比宽接口更容易维护——你对代码库暴露得越少，需要处理的复杂性和维护工作就越少。通过暴露类型对象，我们将对象的 API 扩展到包含类型对象提供的所有内容。
### How are typed objects created? / 类型化对象如何创建？

With this pattern, each "object" is now a pair of objects: the main object and the type object it uses. So how do we create and bind the two together?

使用此模式，每个"对象"现在是一对对象：主对象和它使用的类型对象。那么我们如何创建并将两者绑定在一起？

- **Construct the object and pass in its type object:**
  - *Outside code can control allocation.* Since the calling code is constructing both objects itself, it can control where in memory that occurs. If we want our objects to be usable in a variety of different memory scenarios (different allocators, on the stack, etc.) this gives us the flexibility to do that.
- **Call a "constructor" function on the type object:**
  - *The type object controls memory allocation.* This is the other side of the coin. If we *don't* want users to choose where in memory our objects are created, requiring them to go through a factory method on the type object gives us control over that. This can be useful if we want to ensure all of our objects come from a certain [object pool](object-pool.html) or other memory allocator.

- **构造对象并传入其类型对象：**
  - *外部代码可以控制分配。* 因为调用代码自己构造两个对象，它可以控制这在内存中的哪个位置发生。如果我们希望对象能在各种不同内存场景（不同分配器、在栈上等）中使用，这给了我们这样做的灵活性。
- **在类型对象上调用"构造函数"函数：**
  - *类型对象控制内存分配。* 这是硬币的另一面。如果我们*不*希望用户选择在内存中创建对象的位置，要求他们通过类型对象上的工厂方法可以让我们控制这一点。如果我们想确保所有对象来自某个 [object pool](object-pool.html) 或其他内存分配器，这很有用。
### Can the type change? / 类型可以改变吗？

So far, we've presumed that once an object is created and bound to its type object that that binding will never change. The type an object is created with is the type it dies with. This isn't strictly necessary. We *could* allow an object to change its type over time.

到目前为止，我们假设一旦对象被创建并绑定到其类型对象，该绑定永远不会改变。对象创建时的类型就是它消亡时的类型。这并非严格必要。我们*可以*允许对象随时间改变其类型。

Let's look back at our example. When a monster dies, the designers tell us sometimes they want its corpse to become a reanimated zombie. We could implement this by spawning a new monster with a zombie breed when a monster dies, but another option is to simply get the existing monster and change its breed to a zombie one.

让我们回看示例。当怪物死亡时，设计师告诉我们有时他们希望其尸体变成复活的僵尸。我们可以通过在怪物死亡时生成一个具有僵尸种类的新怪物来实现这一点，但另一种选择是简单地获取现有怪物并将其种类更改为僵尸种类。

- **If the type doesn't change:**
  - *It's simpler both to code and to understand.* At a conceptual level, "type" is something most people probably will not expect to change. This codifies that assumption.
  - *It's easier to debug.* If we're trying to track down a bug where a monster gets into some weird state, it simplifies our job if we can take for granted that the breed we're looking at *now* is the breed the monster has always had.
- **If the type can change:**
  - *There's less object creation.* In our example, if the type can't change, we'll be forced to burn CPU cycles creating a new zombie monster, copying over any attributes from the original monster that need to be preserved, and then deleting it. If we can change the type, all that work gets replaced by a simple assignment.
  - *We need to be careful that assumptions are met.* There's a fairly tight coupling between an object and its type. For example, a breed might assume that a monster's *current* health is never above the starting health that comes from the breed.
    If we allow the breed to change, we need to make sure that the new type's requirements are met by the existing object. When we change the type, we will probably need to execute some validation code to make sure the object is now in a state that makes sense for the new type.

- **如果类型不改变：**
  - *编码和理解都更简单。* 在概念层面上，"类型"是大多数人可能不会期望改变的东西。这编码了该假设。
  - *更容易调试。* 如果我们试图追踪一个怪物进入某种奇怪状态的 bug，如果我们能理所当然地认为我们*现在*看到的 breed 就是怪物一直拥有的 breed，这会简化我们的工作。
- **如果类型可以改变：**
  - *对象创建更少。* 在我们的示例中，如果类型不能改变，我们将被迫消耗 CPU 周期创建新的僵尸怪物，复制需要保留的原始怪物属性，然后删除它。如果我们可以改变类型，所有这些工作被一个简单的赋值替代。
  - *需要注意假设是否满足。* 对象与其类型之间有相当紧密的耦合。例如，一个 breed 可能假设怪物的*当前*血量永远不会超过从 breed 获得的初始血量。
    如果我们允许 breed 改变，我们需要确保现有对象满足新类型的要求。当我们改变类型时，我们可能需要执行一些验证代码，以确保对象现在处于对新类型有意义的状态。
### What kind of inheritance is supported? / 支持哪种继承？

- **No inheritance:**
  - *It's simple.* Simplest is often best. If you don't have a ton of data that needs sharing between your type objects, why make things hard on yourself?
  - *It can lead to duplicated effort.* I've yet to see an authoring system where designers *didn't* want some kind of inheritance. When you've got fifty different kinds of elves, having to tune their health by changing the same number in fifty different places *sucks*.
- **Single inheritance:**
  - *It's still relatively simple.* It's easy to implement, but, more importantly, it's also pretty easy to understand. If non-technical users are going to be working with the system, the fewer moving parts, the better. There's a reason a lot of programming languages only support single inheritance. It seems to be a sweet spot between power and simplicity.
  - *Looking up attributes is slower.* To get a given piece of data from a type object, we might need to walk up the inheritance chain to find the type that ultimately decides the value. If we're in performance-critical code, we may not want to spend time on this.
- **Multiple inheritance:**
  - *Almost all data duplication can be avoided.* With a good multiple inheritance system, users can build a hierarchy for their type objects that has almost no redundancy. When it comes time to tune numbers, we can avoid a lot of copy and paste.
  - *It's complex.* Unfortunately, the benefits for this seem to be more theoretical than practical. Multiple inheritance is hard to understand and reason about.
    If our Zombie Dragon type inherits both from Zombie and Dragon, which attributes come from Zombie and which come from Dragon? In order to use the system, users will need to understand how the inheritance graph is traversed and have the foresight to design an intelligent hierarchy.
    Most C++ coding standards I see today tend to ban multiple inheritance, and Java and C# lack it completely. That's an acknowledgement of a sad fact: it's so hard to get it right that it's often best to not use it at all. While it's worth thinking about, it's rare that you'll want to use multiple inheritance for the type objects in your games. As always, simpler is better.

- **无继承：**
  - *简单。* 简单通常最好。如果你没有大量需要在类型对象间共享的数据，何必为难自己？
  - *可能导致重复劳动。* 我还没见过设计师*不*想要某种继承的创作系统。当你有五十种不同的精灵时，不得不在五十个不同地方修改相同数字来调整血量，这*糟透了*。
- **单继承：**
  - *仍然相对简单。* 它容易实现，但更重要的是，它也相当容易理解。如果非技术用户将使用该系统，活动部件越少越好。很多编程语言只支持单继承是有原因的。它似乎是强大与简单之间的最佳平衡点。
  - *查找属性较慢。* 要从类型对象获取特定数据，我们可能需要向上遍历继承链以找到最终决定该值的类型。如果我们在性能关键的代码中，可能不想花时间在这上面。
- **多继承：**
  - *几乎可以避免所有数据重复。* 有了好的多继承系统，用户可以为其类型对象构建几乎没有冗余的层次结构。当需要调整数值时，我们可以避免大量复制粘贴。
  - *复杂。* 不幸的是，这样做的好处似乎更多是理论上的而非实践上的。多继承难以理解和推理。
    如果我们的 Zombie Dragon 类型同时从 Zombie 和 Dragon 继承，哪些属性来自 Zombie，哪些来自 Dragon？为了使用该系统，用户需要理解继承图是如何遍历的，并有先见之明地设计一个智能的层次结构。
    我今天看到的大多数 C++ 编码标准都倾向于禁止多继承，而 Java 和 C# 完全缺少它。这承认了一个悲哀的事实：它太难用对了，通常最好根本不要使用它。虽然值得考虑，但你很少会在游戏中的类型对象上使用多继承。一如既往，简单更好。
---

## See Also / 参见

- The high-level problem this pattern addresses is sharing data and behavior between several objects. Another pattern that addresses the same problem in a different way is [Prototype](prototype.html).
- Type Object is a close cousin to [Flyweight](flyweight.html). Both let you share data across instances. With Flyweight, the intent is on saving memory, and the shared data may not represent any conceptual "type" of object. With the Type Object pattern, the focus is on organization and flexibility.
- There's a lot of similarity between this pattern and the [State](state.html) pattern. Both patterns let an object delegate part of what defines itself to another object. With a type object, we're usually delegating what the object *is*: invariant data that broadly describes the object. With State, we delegate what an object *is right now*: temporal data that describes an object's current configuration.
  When we discussed having an object change its type, you can look at that as having our Type Object serve double duty as a State too.

- 此模式解决的高级问题是在多个对象之间共享数据和行为。以不同方式解决相同问题的另一个模式是 [Prototype](prototype.html)。
- Type Object 是 [Flyweight](flyweight.html) 的近亲。两者都让你在实例间共享数据。Flyweight 的意图是节省内存，共享数据可能不代表任何概念上的对象"类型"。而 Type Object 模式的重点在于组织和灵活性。
- 此模式与 [State](state.html) 模式有很多相似之处。两种模式都让对象将部分定义自身的职责委托给另一个对象。对于 type object，我们通常委托对象*是什么*：广泛描述对象的不变数据。对于 State，我们委托对象*现在是什么*：描述对象当前配置的瞬时数据。
  当我们讨论让对象改变其类型时，你可以将其视为让 Type Object 也同时充当 State。
---

## C++ Code (原书完整代码)

```cpp
// ======== Breed 类型对象 ========
// 每个实例代表一种"类型"（怪物种类）
// 存储所有该种类怪物共享的数据
class Breed {
public:
  Breed(int health, const char* attack)
  : health_(health),
    attack_(attack)
  {}

  int getHealth() { return health_; }
  const char* getAttack() { return attack_; }

  // 工厂方法 —— 由 Breed 负责创建 Monster
  // 可在其中控制内存分配策略（如对象池）
  Monster* newMonster() { return new Monster(*this); }

private:
  int health_;         // 初始血量 —— 该品种所有怪物共享
  const char* attack_; // 攻击文本 —— 该品种所有怪物共享
};

// ======== Monster 类型化对象 ========
// 每个实例代表一个具体的游戏怪物
// 持有对 Breed 类型的引用，通过 Breed 获取共享数据
class Monster {
  friend class Breed;

public:
  const char* getAttack() { return breed_.getAttack(); }

  // 可在此添加实例特定的行为覆盖
  // const char* getAttack() {
  //   if (health_ < LOW_HEALTH)
  //     return "The monster flails weakly.";
  //   return breed_.getAttack();
  // }

private:
  // 私有构造函数 —— 只有 Breed（友元）可以创建 Monster
  Monster(Breed& breed)
  : health_(breed.getHealth()),
    breed_(breed)
  {}

  int    health_; // 当前血量 —— 实例特有数据
  Breed& breed_;  // 对类型对象的引用 —— 模式核心
};

// ======== 支持继承的 Breed ========
// 允许一个 breed 从另一个 breed 继承属性，通过"拷贝继承"策略
class BreedWithInheritance {
public:
  BreedWithInheritance(BreedWithInheritance* parent,
                       int health, const char* attack)
  : parent_(parent), health_(health), attack_(attack)
  {
    // "拷贝继承"（copy-down delegation）：
    // 在构造函数中将父 breed 的属性复制下来
    if (parent != NULL)
    {
      if (health == 0) health_ = parent->getHealth();
      if (attack == NULL) attack_ = parent->getAttack();
    }
  }

  int getHealth() { return health_; }
  const char* getAttack() { return attack_; }

private:
  BreedWithInheritance* parent_;
  int health_;
  const char* attack_;
};

// ======== JSON 配置示例 ========
// {
//   "Troll": {
//     "health": 25,
//     "attack": "The troll hits you!"
//   },
//   "Troll Archer": {
//     "parent": "Troll",
//     "health": 0,  // 为 0 表示从父 breed 继承
//     "attack": "The troll archer fires an arrow!"
//   },
//   "Troll Wizard": {
//     "parent": "Troll",
//     "health": 0,
//     "attack": "The troll wizard casts a spell on you!"
//   }
// }
```

## C# Equivalent (C# 对照实现)

```csharp
using UnityEngine;

// ======== Breed 类型对象 ========
// 每个实例代表一种"类型"（怪物种类、武器类型等）
// 该类只存储所有同类实例共享的数据
public class Breed
{
    public int BaseHealth { get; }
    public string AttackDescription { get; }

    public Breed(int baseHealth, string attackDescription)
    {
        BaseHealth = baseHealth;
        AttackDescription = attackDescription;
    }

    // 工厂方法：创建属于该 breed 的 Monster 实例
    // 为什么放在 Breed 而不是直接 new Monster(breed)？因为：
    // 1. 可以控制内存分配策略（如使用对象池）
    // 2. 可以执行额外的初始化逻辑
    // 3. 构造函数在 Monster 中是 internal 的，外部无法直接调用
    public Monster CreateMonster()
    {
        return new Monster(this);
    }
}

// ======== Monster 类型化对象 ========
// 每个 Monster 实例代表一个具体的游戏对象
// 它持有对 Breed 的引用，通过 Breed 获取共享的类型数据
public class Monster
{
    public int CurrentHealth { get; private set; }

    // 对类型对象的引用 —— 模式核心
    // "拥有一个类型" 而非 "是一种类型"
    private readonly Breed _breed;

    // internal 构造函数 —— 只有同程序集的 Breed 可以调用
    internal Monster(Breed breed)
    {
        _breed = breed;
        CurrentHealth = _breed.BaseHealth;
    }

    // 行为委托给 Breed
    // 可在此添加实例特定的覆盖逻辑
    public string GetAttackDescription()
    {
        // 实例特有逻辑：低血量时弱化攻击
        if (CurrentHealth < _breed.BaseHealth * 0.2f)
        {
            return "The monster flails weakly!";
        }
        return _breed.AttackDescription;
    }

    public void TakeDamage(int damage)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
    }
}

// ======== 支持继承的 Breed ========
// 通过拷贝继承策略实现属性继承
public class BreedHierarchy
{
    public int BaseHealth { get; }
    public string AttackDescription { get; }
    private readonly BreedHierarchy _parent;

    public BreedHierarchy(BreedHierarchy parent,
                           int baseHealth, string attackDescription)
    {
        _parent = parent;
        // 拷贝继承：属性为默认值时自动从父 breed 继承
        BaseHealth = baseHealth != 0
            ? baseHealth
            : (parent?.BaseHealth ?? 0);

        AttackDescription = !string.IsNullOrEmpty(attackDescription)
            ? attackDescription
            : parent?.AttackDescription ?? "The monster attacks!";
    }
}

// ======== Unity 实战：ScriptableObject 类型对象 ========
// Unity 原生的 Type Object 实现
// ScriptableObject 天生是数据资产，可在编辑器中创建和编辑

[CreateAssetMenu(menuName = "Monster/Breed")]
public class MonsterBreedSO : ScriptableObject
{
    [Header("Combat Stats")]
    public int baseHealth = 100;
    public int baseDamage = 10;
    public float moveSpeed = 3.5f;

    [Header("Visual")]
    public GameObject prefab;
    public Material material;

    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip deathSound;

    [Header("AI")]
    public AiBehaviorType aiBehavior;
    public float detectionRange = 15f;

    // 工厂方法：创建并初始化运行时实体
    public MonsterEntity CreateEntity(Vector3 position, Quaternion rotation)
    {
        GameObject go = Object.Instantiate(prefab, position, rotation);
        MonsterEntity entity = go.GetComponent<MonsterEntity>();
        entity.Initialize(this);
        return entity;
    }
}

// 运行时怪物实体 —— 对应原书中的 Monster 类
public class MonsterEntity : MonoBehaviour
{
    // 持有对类型对象的引用 —— 模式核心
    [SerializeField] private MonsterBreedSO _breed;

    // 实例特有数据
    private int _currentHealth;
    private float _currentMoveSpeed;

    public void Initialize(MonsterBreedSO breed)
    {
        _breed = breed;
        _currentHealth = breed.baseHealth;
        _currentMoveSpeed = breed.moveSpeed;
    }

    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        if (_breed.attackSound != null)
        {
            AudioSource.PlayClipAtPoint(
                _breed.attackSound, transform.position);
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_breed.deathSound != null)
        {
            AudioSource.PlayClipAtPoint(
                _breed.deathSound, transform.position);
        }
        Destroy(gameObject);
    }
}

// ======== 设计者工作流程示例 ========
// 1. 在 Project 面板右键 → Create → Monster/Breed
// 2. 命名文件 "Goblin.asset"，在 Inspector 中设置各属性
// 3. 创建 "Dragon.asset"，设置不同数值
// 4. 运行时通过代码生成怪物：
//    MonsterBreedSO goblinBreed = Resources.Load<MonsterBreedSO>("Goblin");
//    goblinBreed.CreateEntity(spawnPos, Quaternion.identity);
// 5. 修改 Goblin.asset 的数值 → 所有新生成的哥布林自动使用新数据
```

## Unity Application / Unity 实战应用

In Unity, `ScriptableObject` is the canonical implementation of the Type Object pattern. It provides a native way to define data assets that represent "types" without inheritance or hard-coded classes. Key use cases:

在 Unity 中，`ScriptableObject` 是 Type Object 模式的规范实现。它提供了一种原生方式来定义代表"类型"的数据资产，无需继承或硬编码类。关键用例：

- **Monster/Enemy Breeds:** Define stats, attack patterns, visuals, and audio per breed as individual `.asset` files.
- **Weapon/Item Types:** Store damage, range, cooldown, and visual effects per weapon archetype.
- **Quest/Objective Data:** Define quest chains, NPC dialogues, and reward tables as type objects.
- **Achievement Definitions:** Store achievement criteria, icons, and reward data.

- **怪物/敌人类别：** 将每个种类的属性、攻击模式、视觉和音频定义为单独的 `.asset` 文件。
- **武器/物品类型：** 存储每个武器原型的伤害、射程、冷却和视觉效果。
- **任务/目标数据：** 将任务链、NPC 对话和奖励表定义为类型对象。
- **成就定义：** 存储成就条件、图标和奖励数据。

The combination of `ScriptableObject` + `MonoBehaviour` gives us the exact Type Object relationship: `ScriptableObject` = Breed (type object, data asset), `MonoBehaviour` = Monster (typed object, runtime instance).

`ScriptableObject` + `MonoBehaviour` 的组合给出了精确的 Type Object 关系：`ScriptableObject` = Breed（类型对象，数据资产），`MonoBehaviour` = Monster（类型化对象，运行时实例）。
## Key Differences / 关键区别

| Aspect | 原书 C++ | C# (通用) | Unity ScriptableObject |
|--------|----------|-----------|------------------------|
| Type object class | `Breed` | `Breed` | `ScriptableObject` subclass |
| Typed object class | `Monster` | `Monster` | `MonoBehaviour` component |
| Type reference | `Breed& breed_` | `readonly Breed _breed` | `[SerializeField] MonsterBreedSO _breed` |
| Data storage | Member variables | Properties | Serialized fields |
| Factory method | `Breed::newMonster()` | `Breed.CreateMonster()` | `MonsterBreedSO.CreateEntity()` |
| Inheritance | Manual (parent pointer) | Manual (parent reference) | N/A (use separate SO assets) |
| Behavior override | Override in Monster method | Override in Monster method | Override in MonoBehaviour |
| Configuration | JSON file | JSON/XML/config | `.asset` file + Inspector |
| Runtime type change | Possible (reassign breed) | Possible | Via code reassignment |

---

# Component — 组件模式

> **EN:** Allow a single entity to span multiple domains without coupling the domains to each other.
> **CN:** 允许一个实体跨越多个领域，同时避免这些领域之间相互耦合。

## Intent / 意图

* Allow a single entity to span multiple domains without coupling the domains to each other.

* 允许一个实体跨越多个领域，同时避免这些领域之间相互耦合。

## Motivation / 动机

* Let's say we're building a platformer. The Italian plumber demographic is covered, so ours will star a Danish baker, Bjørn. It stands to reason that we'll have a class representing our friendly pastry chef, and it will contain everything he does in the game.

Brilliant game ideas like this are why I'm a programmer and not a designer.

Since the player controls him, that means reading controller input and translating that input into motion. And, of course, he needs to interact with the level, so some physics and collision go in there. Once that's done, he's got to show up on screen, so toss in animation and rendering. He'll probably play some sounds too.

Hold on a minute; this is getting out of control. Software Architecture 101 tells us that different domains in a program should be kept isolated from each other. If we're making a word processor, the code that handles printing shouldn't be affected by the code that loads and saves documents. A game doesn't have the same domains as a business app, but the rule still applies.

As much as possible, we don't want AI, physics, rendering, sound and other domains to know about each other, but now we've got all of that crammed into one class. We've seen where this road leads to: a 5,000-line dumping ground source file so big that only the bravest ninja coders on your team even dare to go in there.

This is great job security for the few who can tame it, but it's hell for the rest of us. A class that big means even the most seemingly trivial changes can have far-reaching implications. Soon, the class collects *bugs* faster than it collects *features*.

**CN:** 假设我们正在开发一个平台游戏。意大利水管工已经被做过了，所以我们的主角是一位丹麦面包师——Bjørn。很自然地，我们会有一个代表这位友好糕点师的类，它包含他在游戏中所做的一切。

像这样绝妙的游戏创意就是为什么我是个程序员而不是设计师。

由于玩家控制他，这意味着要读取控制器输入并将输入转化为运动。当然，他需要与关卡交互，所以需要一些物理和碰撞检测。完成这些之后，他需要显示在屏幕上，所以还要加入动画和渲染。他可能还会播放一些音效。

等等，这开始失控了。软件架构101告诉我们，程序中不同的领域应该保持相互隔离。如果我们正在开发一个文字处理器，处理打印的代码不应该受加载和保存文档的代码影响。游戏虽然不像商业应用那样有相同的领域，但这个规则仍然适用。

我们尽可能不希望AI、物理、渲染、音效和其他领域相互了解，但现在我们却把所有这些东西都塞进了一个类里。我们已经看到这条路通向何方：一个5000行的垃圾场源文件，大到只有团队中最勇敢的忍者程序员才敢进去。

这对少数能驾驭它的人来说是很好的工作保障，但对其他人来说简直是地狱。一个如此庞大的类意味着即使是最微不足道的修改也可能产生深远的影响。很快，这个类收集的*bug*比收集的*功能*还要快。

### The Gordian knot / 戈尔迪之结

* Even worse than the simple scale problem is the coupling one. All of the different systems in our game have been tied into a giant knotted ball of code like:

`if (collidingWithFloor() && (getRenderState() != INVISIBLE)) {   playSound(HIT_FLOOR); }`

Any programmer trying to make a change in code like that will need to know something about physics, graphics, and sound just to make sure they don't break anything.

While coupling like this sucks in *any* game, it's even worse on modern games that use concurrency. On multi-core hardware, it's vital that code is running on multiple threads simultaneously. One common way to split a game across threads is along domain boundaries — run AI on one core, sound on another, rendering on a third, etc.

Once you do that, it's critical that those domains stay decoupled in order to avoid deadlocks or other fiendish concurrency bugs. Having a single class with an `UpdateSounds()` method that must be called from one thread and a `RenderGraphics()` method that must be called from another is begging for those kinds of bugs to happen.

These two problems compound each other; the class touches so many domains that every programmer will have to work on it, but it's so huge that doing so is a nightmare. If it gets bad enough, coders will start putting hacks in other parts of the codebase just to stay out of the hairball that this `Bjorn` class has become.

**CN:** 比单纯的规模问题更糟糕的是耦合问题。我们游戏中所有不同的系统都被绑成了一个巨大的代码线团，就像这样：

`if (collidingWithFloor() && (getRenderState() != INVISIBLE)) {   playSound(HIT_FLOOR); }`

任何试图修改这类代码的程序员都需要了解物理、图形和音效方面的知识，以确保他们不会破坏任何东西。

虽然这种耦合在*任何*游戏中都很糟糕，但在使用并发的现代游戏中更糟。在多核硬件上，代码同时在多个线程上运行至关重要。一种常见的跨线程拆分游戏的方式是沿着领域边界——在一个核心上运行AI，在另一个核心上运行音效，在第三个核心上运行渲染，等等。

一旦你这样做了，保持这些领域的解耦就变得至关重要，以避免死锁或其他可怕的并发错误。拥有一个包含`UpdateSounds()`方法（必须从一个线程调用）和`RenderGraphics()`方法（必须从另一个线程调用）的单一类，就是在自找这些类型的错误。

这两个问题相互加剧；这个类涉及如此多的领域，以至于每个程序员都必须处理它，但它又如此庞大，以至于这样做是一场噩梦。如果情况变得足够糟糕，程序员们会开始在代码库的其他部分打补丁，只是为了远离这个`Bjorn`类已经变成的毛团。

### Cutting the knot / 斩断绳结

* We can solve this like Alexander the Great — with a sword. We'll take our monolithic `Bjorn` class and slice it into separate parts along domain boundaries. For example, we'll take all of the code for handling user input and move it into a separate `InputComponent` class. `Bjorn` will then own an instance of this component. We'll repeat this process for each of the domains that `Bjorn` touches.

When we're done, we'll have moved almost everything out of `Bjorn`. All that remains is a thin shell that binds the components together. We've solved our huge class problem by simply dividing it up into multiple smaller classes, but we've accomplished more than just that.

**CN:** 我们可以像亚历山大大帝那样解决这个问题——用剑。我们将把庞大的`Bjorn`类沿着领域边界切成独立的部分。例如，我们将所有处理用户输入的代码移到一个单独的`InputComponent`类中。然后`Bjorn`将拥有这个组件的一个实例。我们将对`Bjorn`涉及的每个领域重复这个过程。

完成后，我们几乎把所有东西都移出了`Bjorn`。剩下的只是一个将组件绑定在一起的薄壳。我们通过简单地将一个巨大的类分割成多个更小的类来解决大类问题，但我们做到的远不止这些。

### Loose ends / 松散的末端

* Our component classes are now decoupled. Even though `Bjorn` has a `PhysicsComponent` and a `GraphicsComponent`, the two don't know about each other. This means the person working on physics can modify their component without needing to know anything about graphics and vice versa.

In practice, the components will need to have *some* interaction between themselves. For example, the AI component may need to tell the physics component where Bjørn is trying to go. However, we can restrict this to the components that *do* need to talk instead of just tossing them all in the same playpen together.

**CN:** 我们的组件类现在是解耦的。尽管`Bjorn`有一个`PhysicsComponent`和一个`GraphicsComponent`，但两者互不了解。这意味着负责物理的人可以修改他们的组件，而无需了解任何图形方面的知识，反之亦然。

在实践中，组件之间需要*一些*交互。例如，AI组件可能需要告诉物理组件Bjørn试图去哪里。然而，我们可以将这种交互限制在*确实*需要通信的组件之间，而不是把它们都扔进同一个游戏围栏里。

### Tying back together / 重新组合

* Another feature of this design is that the components are now reusable packages. So far, we've focused on our baker, but let's consider a couple of other kinds of objects in our game world. *Decorations* are things in the world the player sees but doesn't interact with: bushes, debris and other visual detail. *Props* are like decorations but can be touched: boxes, boulders, and trees. *Zones* are the opposite of decorations — invisible but interactive. They're useful for things like triggering a cutscene when Bjørn enters an area.

When object-oriented programming first hit the scene, inheritance was the shiniest tool in its toolbox. It was considered the ultimate code-reuse hammer, and coders swung it often. Since then, we've learned the hard way that it's a heavy hammer indeed. Inheritance has its uses, but it's often too cumbersome for simple code reuse.

Instead, the growing trend in software design is to use composition instead of inheritance when possible. Instead of sharing code between two classes by having them *inherit* from the same class, we do so by having them both *own an instance* of the same class.

Now, consider how we'd set up an inheritance hierarchy for those classes if we weren't using components. A first pass might look like:

![A class diagram. Zone has collision code and inherits from GameObject. Decoration also inherits from GameObject and has rendering code. Prop inherits from Zone but then has redundant rendering code.](images/component-uml.png)

We have a base `GameObject` class that has common stuff like position and orientation. `Zone` inherits from that and adds collision detection. Likewise, `Decoration` inherits from `GameObject` and adds rendering. `Prop` inherits from `Zone`, so it can reuse the collision code. However, `Prop` can't *also* inherit from `Decoration` to reuse the *rendering* code without running into the Deadly Diamond.

The "Deadly Diamond" occurs in class hierarchies with multiple inheritance where there are two different paths to the same base class. The pain that causes is a bit out of the scope of this book, but understand that they named it "deadly" for a reason.

We could flip things around so that `Prop` inherits from `Decoration`, but then we end up having to duplicate the *collision* code. Either way, there's no clean way to reuse the collision and rendering code between the classes that need it without resorting to multiple inheritance. The only other option is to push everything up into `GameObject`, but then `Zone` is wasting memory on rendering data it doesn't need and `Decoration` is doing the same with physics.

Now, let's try it with components. Our subclasses disappear completely. Instead, we have a single `GameObject` class and two component classes: `PhysicsComponent` and `GraphicsComponent`. A decoration is simply a `GameObject` with a `GraphicsComponent` but no `PhysicsComponent`. A zone is the opposite, and a prop has both components. No code duplication, no multiple inheritance, and only three classes instead of four.

A restaurant menu is a good analogy. If each entity is a monolithic class, it's like you can only order combos. We need to have a separate class for each possible *combination* of features. To satisfy every customer, we would need dozens of combos.

Components are à la carte dining — each customer can select just the dishes they want, and the menu is a list of the dishes they can choose from.

Components are basically plug-and-play for objects. They let us build complex entities with rich behavior by plugging different reusable component objects into sockets on the entity. Think software Voltron.

**CN:** 这个设计的另一个特点是组件现在是可重用的包。到目前为止，我们一直专注于我们的面包师，但让我们考虑一下游戏世界中的其他几种对象。*装饰物*是玩家能看到但不会与之交互的世界中的东西：灌木丛、碎片和其他视觉细节。*道具*类似于装饰物但可以被触碰：箱子、巨石和树木。*区域*与装饰物相反——不可见但可交互。它们对于在Bjørn进入某个区域时触发过场动画之类的事情很有用。

当面向对象编程首次出现时，继承是其工具箱中最闪亮的工具。它被认为是终极的代码重用锤子，程序员们经常挥舞它。从那以后，我们通过艰难的方式学到了它确实是一把沉重的锤子。继承有其用途，但对于简单的代码重用来说，它往往过于笨重。

相反，软件设计中日益增长的趋势是尽可能使用组合而非继承。与其让两个类通过*继承*同一个类来共享代码，不如让它们都*拥有*同一个类的实例。

现在，考虑一下如果我们不使用组件，我们将如何为这些类建立继承层次结构。第一版可能看起来像这样：

![A class diagram. Zone has collision code and inherits from GameObject. Decoration also inherits from GameObject and has rendering code. Prop inherits from Zone but then has redundant rendering code.](images/component-uml.png)

我们有一个基类`GameObject`，包含位置和方向等通用内容。`Zone`继承自它并添加了碰撞检测。同样，`Decoration`继承自`GameObject`并添加了渲染。`Prop`继承自`Zone`，因此它可以重用碰撞代码。然而，`Prop`不能*同时*继承自`Decoration`来重用*渲染*代码，否则会陷入"致命钻石"问题。

"致命钻石"发生在具有多重继承的类层次结构中，当存在两条不同的路径通向同一个基类时。这带来的痛苦超出了本书的范围，但要知道它们被称为"致命"是有原因的。

我们可以反过来让`Prop`继承自`Decoration`，但那样我们就不得不重复*碰撞*代码。无论哪种方式，都没有干净的方法在需要它们的类之间重用碰撞和渲染代码，而不诉诸多重继承。唯一的选择是把所有东西都推入`GameObject`，但那样`Zone`会在它不需要的渲染数据上浪费内存，而`Decoration`在物理数据上也是如此。

现在，让我们用组件来试试。我们的子类完全消失了。相反，我们有一个单一的`GameObject`类和两个组件类：`PhysicsComponent`和`GraphicsComponent`。装饰物就是一个带有`GraphicsComponent`但没有`PhysicsComponent`的`GameObject`。区域则相反，道具则同时拥有两个组件。没有代码重复，没有多重继承，只有三个类而不是四个。

餐厅菜单是一个很好的类比。如果每个实体都是一个单体类，就像你只能点套餐一样。我们需要为每种可能的*功能组合*都准备一个单独的类。要满足每个顾客，我们需要几十种套餐。

组件就像是点菜——每个顾客可以选择他们想要的菜品，菜单就是他们可以选择的菜品列表。

组件基本上就是对象的即插即用。它们让我们通过将不同的可重用组件对象插入到实体上的插槽中，来构建具有丰富行为的复杂实体。就像软件版的战神金刚。

## The Pattern / 模式

* A **single entity spans multiple domains**. To keep the domains isolated, the code for each is placed in its own **component class**. The entity is reduced to a simple **container of components**.

"Component", like "Object", is one of those words that means everything and nothing in programming. Because of that, it's been used to describe a few concepts. In business software, there's a "Component" design pattern that describes decoupled services that communicate over the web.

I tried to find a different name for this unrelated pattern found in games, but "Component" seems to be the most common term for it. Since design patterns are about documenting existing practices, I don't have the luxury of coining a new term. So, following in the footsteps of XNA, Delta3D, and others, "Component" it is.

**CN:** 一个**单一的实体跨越多个领域**。为了保持领域的隔离，每个领域的代码被放置在自己的**组件类**中。实体被简化为一个简单的**组件容器**。

"组件"就像"对象"一样，是编程中既意味一切又意味虚无的词语之一。正因如此，它被用来描述几个不同的概念。在商业软件中，有一个"组件"设计模式描述通过网络通信的解耦服务。

我试图为游戏中发现的这个不相关的模式找一个不同的名称，但"组件"似乎是它最常见的术语。由于设计模式是关于记录现有实践的，我没有创造新术语的奢侈。所以，追随XNA、Delta3D和其他人的脚步，就叫"组件"吧。

## When to Use It / 何时使用

* Components are most commonly found within the core class that defines the entities in a game, but they may be useful in other places as well. This pattern can be put to good use when any of these are true:

- You have a class that touches multiple domains which you want to keep decoupled from each other.
- A class is getting massive and hard to work with.
- You want to be able to define a variety of objects that share different capabilities, but using inheritance doesn't let you pick the parts you want to reuse precisely enough.

**CN:** 组件最常见于定义游戏实体的核心类中，但它们也可能在其他地方有用。当以下任一情况成立时，这个模式可以很好地发挥作用：

- 你有一个涉及多个领域的类，而你希望这些领域之间保持解耦。
- 一个类变得庞大且难以处理。
- 你希望能够定义共享不同能力的各种对象，但使用继承无法让你足够精确地选择要重用的部分。

## Keep in Mind / 注意事项

* The Component pattern adds a good bit of complexity over simply making a class and putting code in it. Each conceptual "object" becomes a cluster of objects that must be instantiated, initialized, and correctly wired together. Communication between the different components becomes more challenging, and controlling how they occupy memory is more complex.

For a large codebase, this complexity may be worth it for the decoupling and code reuse it enables, but take care to ensure you aren't over-engineering a "solution" to a non-existent problem before applying this pattern.

Another consequence of using components is that you often have to hop through a level of indirection to get anything done. Given the container object, first you have to get the component you want, *then* you can do what you need. In performance-critical inner loops, this pointer following may lead to poor performance.

There's a flip side to this coin. The Component pattern can often *improve* performance and cache coherence. Components make it easier to use the [Data Locality](data-locality.html) pattern to organize your data in the order that the CPU wants it.

**CN:** 组件模式比简单地创建一个类并把代码放进去要复杂得多。每个概念上的"对象"都变成了一组必须被实例化、初始化并正确连接在一起的对象。不同组件之间的通信变得更具挑战性，控制它们如何占用内存也更加复杂。

对于大型代码库来说，这种复杂性可能值得，因为它实现了解耦和代码重用，但在应用这个模式之前，请确保你没有过度设计一个"解决方案"来解决一个不存在的问题。

使用组件的另一个后果是，你通常需要通过一层间接引用来完成任何事情。给定容器对象，首先你必须获取你想要的组件，*然后*才能做你需要做的事情。在性能关键的内循环中，这种指针追踪可能导致性能不佳。

但事情还有另一面。组件模式通常可以*改善*性能和缓存一致性。组件使得使用[数据局部性](data-locality.html)模式更容易，按照CPU期望的顺序组织数据。

## Sample Code / 示例代码

* One of the biggest challenges for me in writing this book is figuring out how to isolate each pattern. Many design patterns exist to contain code that itself isn't part of the pattern. In order to distill the pattern down to its essence, I try to cut as much of that out as possible, but at some point it becomes a bit like explaining how to organize a closet without showing any clothes.

The Component pattern is a particularly hard one. You can't get a real feel for it without seeing some code for each of the domains that it decouples, so I'll have to sketch in a bit more of Bjørn's code than I'd like. The pattern is really only the component *classes* themselves, but the code in them should help clarify what the classes are for. It's fake code — it calls into other classes that aren't presented here — but it should give you an idea of what we're going for.

**CN:** 对我来说，写这本书最大的挑战之一是弄清楚如何隔离每个模式。许多设计模式的存在是为了包含本身不属于该模式的代码。为了将模式提炼到其本质，我尽可能多地削减这些内容，但在某种程度上，这有点像解释如何整理衣柜却不展示任何衣服。

组件模式尤其困难。如果不展示每个被解耦的领域的一些代码，你就无法真正感受到它，所以我将不得不比我想要的更多地勾勒Bjørn的代码。这个模式实际上只是组件*类*本身，但其中的代码应该有助于澄清这些类的用途。这是伪代码——它调用了这里没有展示的其他类——但它应该能让你了解我们的目标。

### A monolithic class / 单体类

* To get a clearer picture of how this pattern is applied, we'll start by showing a monolithic `Bjorn` class that does everything we need but *doesn't* use this pattern:

I should point out that using the actual name of the character in the codebase is usually a bad idea. The marketing department has an annoying habit of demanding name changes days before you ship. "Focus tests show males between 11 and 15 respond negatively to 'Bjørn'. Use 'Sven' instead."

This is why many software projects use internal-only codenames. Well, that and because it's more fun to tell people you're working on "Big Electric Cat" than just "the next version of Photoshop."

**CN:** 为了更清楚地了解这个模式是如何应用的，我们将首先展示一个不使用此模式的单体`Bjorn`类，它完成了我们需要的一切：

我应该指出，在代码库中使用角色的实际名称通常是个坏主意。市场部门有一个讨厌的习惯，就是在你发货前几天要求改名。"焦点小组测试显示11到15岁的男性对'Bjørn'反应消极。改用'Sven'。"

这就是为什么许多软件项目只使用内部代号。嗯，还有因为告诉别人你在做"大电猫"比说"下一个版本的Photoshop"更有趣。

```cpp
class Bjorn {
public:
  Bjorn()
  : velocity_(0),
    x_(0), y_(0)
  {}

  void update(World& world, Graphics& graphics);

private:
  static const int WALK_ACCELERATION = 1;

  int velocity_;
  int x_, y_;

  Volume volume_;

  Sprite spriteStand_;
  Sprite spriteWalkLeft_;
  Sprite spriteWalkRight_;
};
```

`Bjorn` has an `update()` method that gets called once per frame by the game:

`Bjorn`有一个`update()`方法，游戏每帧调用一次：

```cpp
void Bjorn::update(World& world, Graphics& graphics) {
  // Apply user input to hero's velocity.
  switch (Controller::getJoystickDirection())
  {
    case DIR_LEFT:
      velocity_ -= WALK_ACCELERATION;
      break;

    case DIR_RIGHT:
      velocity_ += WALK_ACCELERATION;
      break;
  }

  // Modify position by velocity.
  x_ += velocity_;
  world.resolveCollision(volume_, x_, y_, velocity_);

  // Draw the appropriate sprite.
  Sprite* sprite = &spriteStand_;
  if (velocity_ < 0)
  {
    sprite = &spriteWalkLeft_;
  }
  else if (velocity_ > 0)
  {
    sprite = &spriteWalkRight_;
  }

  graphics.draw(*sprite, x_, y_);
}
```

It reads the joystick to determine how to accelerate the baker. Then it resolves its new position with the physics engine. Finally, it draws Bjørn onto the screen.

The sample implementation here is trivially simple. There's no gravity, animation, or any of the dozens of other details that make a character fun to play. Even so, we can see that we've got a single function that several different coders on our team will probably have to spend time in, and it's starting to get a bit messy. Imagine this scaled up to a thousand lines and you can get an idea of how painful it can become.

它读取摇杆来确定如何加速面包师。然后它用物理引擎解析新的位置。最后，它将Bjørn绘制到屏幕上。

这里的示例实现非常简单。没有重力、动画或任何其他几十个让角色有趣的细节。即便如此，我们可以看到我们有一个单一的函数，团队中几个不同的程序员可能都需要花时间在其中，而且它开始变得有点混乱。想象一下这扩展到一千行，你就能理解这会有多痛苦。

### Splitting out a domain / 拆分出一个领域

* Starting with one domain, let's pull a piece out of `Bjorn` and push it into a separate component class. We'll start with the first domain that gets processed: input. The first thing `Bjorn` does is read in user input and adjust his velocity based on it. Let's move that logic out into a separate class:

* 从一个领域开始，让我们从`Bjorn`中抽出一部分并将其推入一个单独的组件类。我们从第一个被处理的领域开始：输入。`Bjorn`做的第一件事是读取用户输入并根据它调整速度。让我们将该逻辑移到一个单独的类中：

```cpp
class InputComponent {
public:
  void update(Bjorn& bjorn)
  {
    switch (Controller::getJoystickDirection())
    {
      case DIR_LEFT:
        bjorn.velocity -= WALK_ACCELERATION;
        break;

      case DIR_RIGHT:
        bjorn.velocity += WALK_ACCELERATION;
        break;
    }
  }

private:
  static const int WALK_ACCELERATION = 1;
};
```

Pretty simple. We've taken the first section of `Bjorn`'s `update()` method and put it into this class. The changes to `Bjorn` are also straightforward:

很简单。我们已经将`Bjorn`的`update()`方法的第一部分移到了这个类中。对`Bjorn`的修改也很直接：

```cpp
class Bjorn {
public:
  int velocity;
  int x, y;

  void update(World& world, Graphics& graphics)
  {
    input_.update(*this);

    // Modify position by velocity.
    x += velocity;
    world.resolveCollision(volume_, x, y, velocity);

    // Draw the appropriate sprite.
    Sprite* sprite = &spriteStand_;
    if (velocity < 0)
    {
      sprite = &spriteWalkLeft_;
    }
    else if (velocity > 0)
    {
      sprite = &spriteWalkRight_;
    }

    graphics.draw(*sprite, x, y);
  }

private:
  InputComponent input_;

  Volume volume_;

  Sprite spriteStand_;
  Sprite spriteWalkLeft_;
  Sprite spriteWalkRight_;
};
```

`Bjorn` now owns an `InputComponent` object. Where before he was handling user input directly in the `update()` method, now he delegates to the component:

`input_.update(*this);`

We've only started, but already we've gotten rid of some coupling — the main `Bjorn` class no longer has any reference to `Controller`. This will come in handy later.

`Bjorn`现在拥有一个`InputComponent`对象。之前他在`update()`方法中直接处理用户输入，现在他委托给组件：

`input_.update(*this);`

我们才刚刚开始，但已经消除了一些耦合——主`Bjorn`类不再有任何对`Controller`的引用。这将在以后派上用场。

### Splitting out the rest / 拆分其余部分

* Now, let's go ahead and do the same cut-and-paste job on the physics and graphics code. Here's our new `PhysicsComponent`:

* 现在，让我们对物理和图形代码进行同样的剪切粘贴工作。这是我们的新`PhysicsComponent`：

```cpp
class PhysicsComponent {
public:
  void update(Bjorn& bjorn, World& world)
  {
    bjorn.x += bjorn.velocity;
    world.resolveCollision(volume_,
        bjorn.x, bjorn.y, bjorn.velocity);
  }

private:
  Volume volume_;
};
```

In addition to moving the physics *behavior* out of the main `Bjorn` class, you can see we've also moved out the *data* too: The `Volume` object is now owned by the component.

除了将物理*行为*移出主`Bjorn`类之外，你可以看到我们也移出了*数据*：`Volume`对象现在由组件拥有。

Last but not least, here's where the rendering code lives now:

最后但同样重要的是，渲染代码现在在这里：

```cpp
class GraphicsComponent {
public:
  void update(Bjorn& bjorn, Graphics& graphics)
  {
    Sprite* sprite = &spriteStand_;
    if (bjorn.velocity < 0)
    {
      sprite = &spriteWalkLeft_;
    }
    else if (bjorn.velocity > 0)
    {
      sprite = &spriteWalkRight_;
    }

    graphics.draw(*sprite, bjorn.x, bjorn.y);
  }

private:
  Sprite spriteStand_;
  Sprite spriteWalkLeft_;
  Sprite spriteWalkRight_;
};
```

We've yanked almost everything out, so what's left of our humble pastry chef? Not much:

我们几乎把所有东西都抽走了，那么我们谦逊的糕点师还剩下什么？不多了：

```cpp
class Bjorn {
public:
  int velocity;
  int x, y;

  void update(World& world, Graphics& graphics)
  {
    input_.update(*this);
    physics_.update(*this, world);
    graphics_.update(*this, graphics);
  }

private:
  InputComponent input_;
  PhysicsComponent physics_;
  GraphicsComponent graphics_;
};
```

The `Bjorn` class now basically does two things: it holds the set of components that actually define it, and it holds the state that is shared across multiple domains. Position and velocity are still in the core `Bjorn` class for two reasons. First, they are "pan-domain" state — almost every component will make use of them, so it isn't clear which component *should* own them if we did want to push them down.

Secondly, and more importantly, it gives us an easy way for the components to communicate without being coupled to each other. Let's see if we can put that to use.

`Bjorn`类现在基本上做两件事：它持有实际定义它的组件集合，以及持有跨多个领域共享的状态。位置和速度仍然在核心`Bjorn`类中，原因有二。首先，它们是"泛领域"状态——几乎每个组件都会使用它们，所以如果我们确实想下推它们，不清楚哪个组件*应该*拥有它们。

其次，更重要的是，它为我们提供了一种让组件之间通信而不相互耦合的简单方式。让我们看看能否利用这一点。

### Robo-Bjørn / 机器人Bjørn

* So far, we've pushed our behavior out to separate component classes, but we haven't *abstracted* the behavior out. `Bjorn` still knows the exact concrete classes where his behavior is defined. Let's change that.

We'll take our component for handling user input and hide it behind an interface. We'll turn `InputComponent` into an abstract base class:

**CN:** 到目前为止，我们已经将行为推到了单独的组件类中，但我们还没有*抽象*出行为。`Bjorn`仍然知道定义其行为的确切具体类。让我们改变这一点。

我们将把处理用户输入的组件隐藏在一个接口后面。我们将把`InputComponent`变成一个抽象基类：

```cpp
class InputComponent {
public:
  virtual ~InputComponent() {}
  virtual void update(Bjorn& bjorn) = 0;
};
```

Then, we'll take our existing user input handling code and push it down into a class that implements that interface:

然后，我们将现有的用户输入处理代码下推到一个实现该接口的类中：

```cpp
class PlayerInputComponent : public InputComponent {
public:
  virtual void update(Bjorn& bjorn)
  {
    switch (Controller::getJoystickDirection())
    {
      case DIR_LEFT:
        bjorn.velocity -= WALK_ACCELERATION;
        break;

      case DIR_RIGHT:
        bjorn.velocity += WALK_ACCELERATION;
        break;
    }
  }

private:
  static const int WALK_ACCELERATION = 1;
};
```

We'll change `Bjorn` to hold a pointer to the input component instead of having an inline instance:

我们将修改`Bjorn`，让它持有一个指向输入组件的指针，而不是内联实例：

```cpp
class Bjorn {
public:
  int velocity;
  int x, y;

  Bjorn(InputComponent* input)
  : input_(input)
  {}

  void update(World& world, Graphics& graphics)
  {
    input_->update(*this);
    physics_.update(*this, world);
    graphics_.update(*this, graphics);
  }

private:
  InputComponent* input_;
  PhysicsComponent physics_;
  GraphicsComponent graphics_;
};
```

Now, when we instantiate `Bjorn`, we can pass in an input component for it to use, like so:

现在，当我们实例化`Bjorn`时，我们可以传入一个输入组件供其使用，就像这样：

```cpp
Bjorn* bjorn = new Bjorn(new PlayerInputComponent());
```

This instance can be any concrete type that implements our abstract `InputComponent` interface. We pay a price for this — `update()` is now a virtual method call, which is a little slower. What do we get in return for this cost?

Most consoles require a game to support "demo mode." If the player sits at the main menu without doing anything, the game will start playing automatically, with the computer standing in for the player. This keeps the game from burning the main menu into your TV and also makes the game look nicer when it's running on a kiosk in a store.

Hiding the input component class behind an interface lets us get that working. We already have our concrete `PlayerInputComponent` that's normally used when playing the game. Now, let's make another one:

这个实例可以是任何实现了我们抽象`InputComponent`接口的具体类型。我们为此付出了代价——`update()`现在是一个虚方法调用，稍微慢了一些。我们从这个代价中得到了什么回报？

大多数游戏机要求游戏支持"演示模式"。如果玩家坐在主菜单前什么都不做，游戏将自动开始播放，由计算机代替玩家。这可以防止游戏将主菜单烧录到你的电视上，也让游戏在商店的展示台上运行时看起来更好看。

将输入组件类隐藏在接口后面让我们实现了这一点。我们已经有了通常在玩游戏时使用的具体`PlayerInputComponent`。现在，让我们再做一个：

```cpp
class DemoInputComponent : public InputComponent {
public:
  virtual void update(Bjorn& bjorn)
  {
    // AI to automatically control Bjorn...
  }
};
```

When the game goes into demo mode, instead of constructing Bjørn like we did earlier, we'll wire him up with our new component:

当游戏进入演示模式时，我们不再像之前那样构造Bjørn，而是用我们的新组件来装配他：

```cpp
Bjorn* bjorn = new Bjorn(new DemoInputComponent());
```

And now, just by swapping out a component, we've got a fully functioning computer-controlled player for demo mode. We're able to reuse all of the other code for Bjørn — physics and graphics don't even know there's a difference. Maybe I'm a bit strange, but it's stuff like this that gets me up in the morning.

That, and coffee. Sweet, steaming hot coffee.

现在，只需更换一个组件，我们就为演示模式拥有了一个功能完整的计算机控制玩家。我们能够重用Bjørn的所有其他代码——物理和图形甚至不知道有区别。也许我有点奇怪，但正是这样的事情让我每天早上起床。

还有咖啡。香甜、热气腾腾的咖啡。

### No Bjørn at all? / 完全没有Bjørn？

* If you look at our `Bjorn` class now, you'll notice there's nothing really "Bjørn" about it — it's just a component bag. In fact, it looks like a pretty good candidate for a base "game object" class that we can use for *every* object in the game. All we need to do is pass in *all* the components, and we can build any kind of object by picking and choosing parts like Dr. Frankenstein.

Let's take our two remaining concrete components — physics and graphics — and hide them behind interfaces like we did with input:

**CN:** 如果你现在看看我们的`Bjorn`类，你会注意到它实际上没有什么"Bjørn"特有的东西——它只是一个组件包。事实上，它看起来很适合作为基础"游戏对象"类，我们可以用它来代表游戏中的*每个*对象。我们只需要传入*所有*组件，就可以像弗兰肯斯坦博士那样挑选零件来构建任何类型的对象。

让我们把剩下的两个具体组件——物理和图形——像处理输入一样隐藏在接口后面：

```cpp
class PhysicsComponent {
public:
  virtual ~PhysicsComponent() {}
  virtual void update(GameObject& obj, World& world) = 0;
};

class GraphicsComponent {
public:
  virtual ~GraphicsComponent() {}
  virtual void update(GameObject& obj, Graphics& graphics) = 0;
};
```

Then we re-christen `Bjorn` into a generic `GameObject` class that uses those interfaces:

然后我们将`Bjorn`重新命名为一个使用这些接口的通用`GameObject`类：

```cpp
class GameObject {
public:
  int velocity;
  int x, y;

  GameObject(InputComponent* input,
             PhysicsComponent* physics,
             GraphicsComponent* graphics)
  : input_(input),
    physics_(physics),
    graphics_(graphics)
  {}

  void update(World& world, Graphics& graphics)
  {
    input_->update(*this);
    physics_->update(*this, world);
    graphics_->update(*this, graphics);
  }

private:
  InputComponent* input_;
  PhysicsComponent* physics_;
  GraphicsComponent* graphics_;
};
```

Some component systems take this even further. Instead of a `GameObject` that contains its components, the game entity is just an ID, a number. Then, you maintain separate collections of components where each one knows the ID of the entity its attached to.

These [entity component systems](http://en.wikipedia.org/wiki/Entity_component_system) take decoupling components to the extreme and let you add new components to an entity without the entity even knowing. The [Data Locality](data-locality.html) chapter has more details.

Our existing concrete classes will get renamed and implement those interfaces:

一些组件系统甚至更进一步。游戏实体不是一个包含其组件的`GameObject`，而只是一个ID，一个数字。然后，你维护独立的组件集合，每个组件都知道它所附加的实体的ID。

这些[实体组件系统](http://en.wikipedia.org/wiki/Entity_component_system)将组件解耦推向了极致，让你可以在实体甚至不知道的情况下向实体添加新组件。[数据局部性](data-locality.html)章节有更多细节。

我们现有的具体类将被重命名并实现这些接口：

```cpp
class BjornPhysicsComponent : public PhysicsComponent {
public:
  virtual void update(GameObject& obj, World& world)
  {
    // Physics code...
  }
};

class BjornGraphicsComponent : public GraphicsComponent {
public:
  virtual void update(GameObject& obj, Graphics& graphics)
  {
    // Graphics code...
  }
};
```

And now we can build an object that has all of Bjørn's original behavior without having to actually create a class for him, just like this:

现在我们可以构建一个拥有Bjørn所有原始行为的对象，而无需实际为他创建一个类，就像这样：

```cpp
GameObject* createBjorn() {
  return new GameObject(new PlayerInputComponent(),
                        new BjornPhysicsComponent(),
                        new BjornGraphicsComponent());
}
```

This `createBjorn()` function is, of course, an example of the classic Gang of Four [Factory Method](http://c2.com/cgi/wiki?FactoryMethod) pattern.

By defining other functions that instantiate `GameObjects` with different components, we can create all of the different kinds of objects our game needs.

这个`createBjorn()`函数当然是经典的四人组[工厂方法](http://c2.com/cgi/wiki?FactoryMethod)模式的一个例子。

通过定义其他用不同组件实例化`GameObject`的函数，我们可以创建游戏所需的所有不同类型的对象。

## Design Decisions / 设计决策

* The most important design question you'll need to answer with this pattern is, "What set of components do I need?" The answer there is going to depend on the needs and genre of your game. The bigger and more complex your engine is, the more finely you'll likely want to slice your components.

Beyond that, there are a couple of more specific options to consider:

**CN:** 使用这个模式你需要回答的最重要的设计问题是："我需要哪些组件？"答案取决于你游戏的需求和类型。你的引擎越大越复杂，你可能就越想精细地划分你的组件。

除此之外，还有几个更具体的选项需要考虑：

### How does the object get its components? / 对象如何获取其组件？

* Once we've split up our monolithic object into a few separate component parts, we have to decide who puts the parts back together.

- **If the object creates its own components:**
  - *It ensures that the object always has the components it needs.* You never have to worry about someone forgetting to wire up the right components to the object and breaking the game. The container object itself takes care of it for you.
  - *It's harder to reconfigure the object.* One of the powerful features of this pattern is that it lets you build new kinds of objects simply by recombining components. If our object always wires itself with the same set of hard-coded components, we aren't taking advantage of that flexibility.

- **If outside code provides the components:**
  - *The object becomes more flexible.* We can completely change the behavior of the object by giving it different components to work with. Taken to its fullest extent, our object becomes a generic component container that we can reuse over and over again for different purposes.
  - *The object can be decoupled from the concrete component types.* If we're allowing outside code to pass in components, odds are good that we're also letting it pass in *derived* component types. At that point, the object only knows about the component *interfaces* and not the concrete types themselves. This can make for a nicely encapsulated architecture.

**CN:** 一旦我们将单体对象拆分成几个独立的组件部分，我们必须决定谁来把这些部分重新组合起来。

- **如果对象自己创建组件：**
  - *它确保对象始终拥有所需的组件。* 你永远不必担心有人忘记为对象连接正确的组件而破坏游戏。容器对象本身为你处理了这一点。
  - *重新配置对象更加困难。* 这个模式的强大特性之一在于它允许你通过重新组合组件来构建新型对象。如果我们的对象总是用同一组硬编码的组件来装配自己，我们就无法利用这种灵活性。

- **如果外部代码提供组件：**
  - *对象变得更加灵活。* 我们可以通过给对象提供不同的组件来完全改变其行为。在最大程度上，我们的对象变成了一个通用的组件容器，我们可以为不同的目的反复重用它。
  - *对象可以与具体的组件类型解耦。* 如果我们允许外部代码传入组件，那么很可能我们也允许它传入*派生*的组件类型。此时，对象只知道组件的*接口*，而不知道具体的类型本身。这可以形成一个良好封装的架构。

### How do components communicate with each other? / 组件之间如何通信？

* Perfectly decoupled components that function in isolation is a nice ideal, but it doesn't really work in practice. The fact that these components are part of the *same* object implies that they are part of a larger whole and need to coordinate. That means communication.

So how can the components talk to each other? There are a couple of options, but unlike most design "alternatives" in this book, these aren't exclusive — you will likely support more than one at the same time in your designs.

**CN:** 完美解耦、独立运行的组件是一个美好的理想，但在实践中并不真正可行。这些组件是*同一个*对象的一部分，这一事实意味着它们是更大整体的一部分，需要协调。这意味着通信。

那么组件之间如何相互通信呢？有几种选择，但与本书中大多数设计的"替代方案"不同，这些方案并不是互斥的——你很可能在你的设计中同时支持多种方案。

- **By modifying the container object's state:**
  - *It keeps the components decoupled.* When our `InputComponent` set Bjørn's velocity and the `PhysicsComponent` later used it, the two components had no idea that the other even existed. For all they knew, Bjørn's velocity could have changed through black magic.
  - *It requires any information that components need to share to get pushed up into the container object.* Often, there's state that's really only needed by a subset of the components. For example, an animation and a rendering component may need to share information that's graphics-specific. Pushing that information up into the container object where *every* component can get to it muddies the object class.
    Worse, if we use the same container object class with different component configurations, we can end up wasting memory on state that isn't needed by *any* of the object's components. If we push some rendering-specific data into the container object, any invisible object will be burning memory on it with no benefit.
  - *It makes communication implicit and dependent on the order that components are processed.* In our sample code, the original monolithic `update()` method had a very carefully laid out order of operations. The user input modified the velocity, which was then used by the physics code to modify the position, which in turn was used by the rendering code to draw Bjørn at the right spot. When we split that code out into components, we were careful to preserve that order of operations.
    If we hadn't, we would have introduced subtle, hard-to-track bugs. For example, if we'd updated the graphics component *first*, we would wrongly render Bjørn at his position on the *last* frame, not this one. If you imagine several more components and lots more code, then you can get an idea of how hard it can be to avoid bugs like this.
    Shared mutable state like this where lots of code is reading and writing the same data is notoriously hard to get right. That's a big part of why academics are spending time researching pure functional languages like Haskell where there is no mutable state at all.

- **By referring directly to each other:**
  The idea here is that components that need to talk will have direct references to each other without having to go through the container object at all.

  Let's say we want to let Bjørn jump. The graphics code needs to know if he should be drawn using a jump sprite or not. It can determine this by asking the physics engine if he's currently on the ground. An easy way to do this is by letting the graphics component know about the physics component directly:

  ```cpp
  class BjornGraphicsComponent {
  public:
    BjornGraphicsComponent(BjornPhysicsComponent* physics)
    : physics_(physics)
    {}

    void Update(GameObject& obj, Graphics& graphics)
    {
      Sprite* sprite;
      if (!physics_->isOnGround())
      {
        sprite = &spriteJump_;
      }
      else
      {
        // Existing graphics code...
      }

      graphics.draw(*sprite, obj.x, obj.y);
    }

  private:
    BjornPhysicsComponent* physics_;

    Sprite spriteStand_;
    Sprite spriteWalkLeft_;
    Sprite spriteWalkRight_;
    Sprite spriteJump_;
  };
```

  When we construct Bjørn's `GraphicsComponent`, we'll give it a reference to his corresponding `PhysicsComponent`.

  - *It's simple and fast.* Communication is a direct method call from one object to another. The component can call any method that is supported by the component it has a reference to. It's a free-for-all.
  - *The two components are tightly coupled.* The downside of the free-for-all. We've basically taken a step back towards our monolithic class. It's not quite as bad as the original single class though, since we're at least restricting the coupling to only the component pairs that need to interact.

- **By sending messages:**
  - This is the most complex alternative. We can actually build a little messaging system into our container object and let the components broadcast information to each other.

    Here's one possible implementation. We'll start by defining a base `Component` interface that all of our components will implement:

    ```cpp
    class Component {
    public:
      virtual ~Component() {}
      virtual void receive(int message) = 0;
    };
    ```

    It has a single `receive()` method that component classes implement in order to listen to an incoming message. Here, we're just using an `int` to identify the message, but a fuller implementation could attach additional data to the message.

    Then, we'll add a method to our container object for sending messages:

    ```cpp
    class ContainerObject {
    public:
      void send(int message)
      {
        for (int i = 0; i < MAX_COMPONENTS; i++)
        {
          if (components_[i] != NULL)
          {
            components_[i]->receive(message);
          }
        }
      }

    private:
      static const int MAX_COMPONENTS = 10;
      Component* components_[MAX_COMPONENTS];
    };
    ```

    Now, if a component has access to its container, it can send messages to the container, which will rebroadcast the message to all of the contained components. (That includes the original component that sent the message; be careful that you don't get stuck in a feedback loop!) This has a couple of consequences:

    If you really want to get fancy, you can even make this message system *queue* messages to be delivered later. For more on this, see [Event Queue](event-queue.html).

  - *Sibling components are decoupled.* By going through the parent container object, like our shared state alternative, we ensure that the components are still decoupled from each other. With this system, the only coupling they have is the message values themselves.
    The Gang of Four call this the [Mediator](http://c2.com/cgi-bin/wiki?MediatorPattern) pattern — two or more objects communicate with each other indirectly by routing the message through an intermediate object. In this case, the container object itself is the mediator.
  - *The container object is simple.* Unlike using shared state where the container object itself owns and knows about data used by the components, here, all it does is blindly pass the messages along. That can be useful for letting two components pass very domain-specific information between themselves without having that bleed into the container object.

**CN:** 完美解耦、独立运行的组件是一个美好的理想，但在实践中并不真正可行。这些组件是*同一个*对象的一部分，这一事实意味着它们是更大整体的一部分，需要协调。这意味着通信。

那么组件之间如何相互通信呢？有几种选择，但与本书中大多数设计的"替代方案"不同，这些方案并不是互斥的——你很可能在你的设计中同时支持多种方案。

- **通过修改容器对象的状态：**
  - *它保持组件的解耦。* 当我们的`InputComponent`设置了Bjørn的速度，而`PhysicsComponent`后来使用了它时，这两个组件甚至不知道对方的存在。就它们所知，Bjørn的速度可能是通过黑魔法改变的。
  - *它要求组件需要共享的任何信息都被推送到容器对象中。* 通常，有些状态实际上只被一部分组件需要。例如，动画和渲染组件可能需要共享特定于图形的信息。将这些信息推送到*每个*组件都能访问到的容器对象中，会使对象类变得混乱。
    更糟糕的是，如果我们对不同的组件配置使用相同的容器对象类，我们最终可能会在对象*任何*组件都不需要的状态上浪费内存。如果我们将一些渲染特定的数据推送到容器对象中，任何不可见的对象都会白白浪费内存。
  - *它使通信变得隐式，并依赖于组件被处理的顺序。* 在我们的示例代码中，原始的单体`update()`方法有一个非常精心安排的操作顺序。用户输入修改了速度，然后物理代码使用速度来修改位置，接着渲染代码使用位置在正确的位置绘制Bjørn。当我们把代码拆分成组件时，我们小心地保留了那个操作顺序。
    如果我们没有这样做，就会引入微妙的、难以追踪的错误。例如，如果我们*先*更新图形组件，我们会错误地将Bjørn渲染在*上一帧*的位置，而不是这一帧。如果你想象还有更多的组件和更多的代码，你就能理解避免这类错误有多难了。
    像这样大量代码读写相同数据的共享可变状态，是出了名的难以正确处理。这就是为什么学术界花时间研究像Haskell这样的纯函数式语言（其中根本没有可变状态）的一个重要原因。

- **通过直接相互引用：**
  这里的想法是，需要通信的组件将直接相互引用，而完全不需要通过容器对象。

  假设我们想让Bjørn跳跃。图形代码需要知道他是否应该使用跳跃精灵来绘制。它可以通过询问物理引擎他当前是否在地面上来确定这一点。一个简单的方法是让图形组件直接知道物理组件：

  ```cpp
  class BjornGraphicsComponent {
  public:
    BjornGraphicsComponent(BjornPhysicsComponent* physics)
    : physics_(physics)
    {}

    void Update(GameObject& obj, Graphics& graphics)
    {
      Sprite* sprite;
      if (!physics_->isOnGround())
      {
        sprite = &spriteJump_;
      }
      else
      {
        // Existing graphics code...
      }

      graphics.draw(*sprite, obj.x, obj.y);
    }

  private:
    BjornPhysicsComponent* physics_;

    Sprite spriteStand_;
    Sprite spriteWalkLeft_;
    Sprite spriteWalkRight_;
    Sprite spriteJump_;
  };
  ```

  当我们构造Bjørn的`GraphicsComponent`时，我们会给它一个对应`PhysicsComponent`的引用。

  - *它简单且快速。* 通信是一个对象到另一个对象的直接方法调用。组件可以调用它所引用的组件支持的任何方法。这是一个自由混战。
  - *两个组件紧密耦合。* 这是自由混战的缺点。我们基本上向单体类倒退了一步。不过它没有原始的单体类那么糟糕，因为我们至少将耦合限制在需要交互的组件对上。

- **通过发送消息：**
  - 这是最复杂的替代方案。我们实际上可以在容器对象中构建一个小型消息系统，让组件之间广播信息。

    这是一个可能的实现。我们首先定义一个所有组件都将实现的基础`Component`接口：

    ```cpp
    class Component {
    public:
      virtual ~Component() {}
      virtual void receive(int message) = 0;
    };
    ```

    它有一个单一的`receive()`方法，组件类实现它来监听传入的消息。这里我们只使用一个`int`来标识消息，但更完整的实现可以将附加数据附加到消息上。

    然后，我们向容器对象添加一个发送消息的方法：

    ```cpp
    class ContainerObject {
    public:
      void send(int message)
      {
        for (int i = 0; i < MAX_COMPONENTS; i++)
        {
          if (components_[i] != NULL)
          {
            components_[i]->receive(message);
          }
        }
      }

    private:
      static const int MAX_COMPONENTS = 10;
      Component* components_[MAX_COMPONENTS];
    };
    ```

    现在，如果一个组件可以访问它的容器，它就可以向容器发送消息，容器会将消息重新广播给所有包含的组件。（这包括发送消息的原始组件；小心不要陷入反馈循环！）这有几个后果：

    如果你真的想玩得花哨，你甚至可以让这个消息系统*排队*稍后传递的消息。更多信息请参见[事件队列](event-queue.html)。

  - *兄弟组件是解耦的。* 通过父容器对象，就像我们的共享状态方案一样，我们确保组件之间仍然解耦。使用这个系统，它们唯一的耦合就是消息值本身。
    四人组称之为[中介者](http://c2.com/cgi-bin/wiki?MediatorPattern)模式——两个或多个对象通过将消息路由通过一个中间对象来间接通信。在这种情况下，容器对象本身就是中介者。
  - *容器对象很简单。* 与使用共享状态（容器对象本身拥有并了解组件使用的数据）不同，这里它所做的只是盲目地传递消息。这对于让两个组件在彼此之间传递非常特定于领域的信息而不让这些信息泄漏到容器对象中非常有用。

Unsurprisingly, there's no one best answer here. What you'll likely end up doing is using a bit of all of them. Shared state is useful for the really basic stuff that you can take for granted that every object has — things like position and size.

Some domains are distinct but still closely related. Think animation and rendering, user input and AI, or physics and collision. If you have separate components for each half of those pairs, you may find it easiest to just let them know directly about their other half.

Messaging is useful for "less important" communication. Its fire-and-forget nature is a good fit for things like having an audio component play a sound when a physics component sends a message that the object has collided with something.

As always, I recommend you start simple and then add in additional communication paths if you need them.

毫不奇怪，这里没有唯一的最佳答案。你最终可能会同时使用所有方案。共享状态对于真正基本的东西很有用，你可以理所当然地认为每个对象都有——比如位置和大小。

有些领域是不同但仍然密切相关的。想想动画和渲染、用户输入和AI、或物理和碰撞。如果你为这些对的每一半都有单独的组件，你可能会发现让它们直接了解另一半是最简单的。

消息传递对于"不太重要的"通信很有用。它的即发即弃特性非常适合这样的情况：当物理组件发送消息说对象与某物碰撞时，音频组件播放一个音效。

一如既往，我建议你从简单开始，然后在需要时添加额外的通信路径。

## See Also / 参见

- The [Unity](http://unity3d.com) framework's core [`GameObject`](http://docs.unity3d.com/Documentation/Manual/GameObjects.html) class is designed entirely around [components](http://docs.unity3d.com/Manual/UsingComponents.html).
- The open source [Delta3D](http://www.delta3d.org) engine has a base `GameActor` class that implements this pattern with the appropriately named `ActorComponent` base class.
- Microsoft's [XNA](http://creators.xna.com/en-US/) game framework comes with a core `Game` class. It owns a collection of `GameComponent` objects. Where our example uses components at the individual game entity level, XNA implements the pattern at the level of the main game object itself, but the purpose is the same.
- This pattern bears resemblance to the Gang of Four's [Strategy](http://c2.com/cgi-bin/wiki?StrategyPattern) pattern. Both patterns are about taking part of an object's behavior and delegating it to a separate subordinate object. The difference is that with the Strategy pattern, the separate "strategy" object is usually stateless — it encapsulates an algorithm, but no data. It defines *how* an object behaves, but not *what* it is.
  Components are a bit more self-important. They often hold state that describes the object and helps define its actual identity. However, the line may blur. You may have some components that don't need any local state. In that case, you're free to use the same component *instance* across multiple container objects. At that point, it really is behaving more akin to a strategy.

- [Unity](http://unity3d.com)框架的核心[`GameObject`](http://docs.unity3d.com/Documentation/Manual/GameObjects.html)类完全围绕[组件](http://docs.unity3d.com/Manual/UsingComponents.html)设计。
- 开源[Delta3D](http://www.delta3d.org)引擎有一个基础`GameActor`类，它使用命名恰当的`ActorComponent`基类实现了这个模式。
- 微软的[XNA](http://creators.xna.com/en-US/)游戏框架带有一个核心`Game`类。它拥有一个`GameComponent`对象的集合。我们的示例在单个游戏实体级别使用组件，而XNA在主游戏对象本身级别实现该模式，但目的是相同的。
- 这个模式与四人组的[策略](http://c2.com/cgi-bin/wiki?StrategyPattern)模式有相似之处。两种模式都是关于将对象的部分行为委托给一个单独的下属对象。区别在于，使用策略模式时，单独的"策略"对象通常是无状态的——它封装了一个算法，但没有数据。它定义了对象*如何*行为，而不是对象*是什么*。
  组件则更加自我重要。它们通常持有描述对象并帮助定义其实际身份的状态。然而，界限可能会模糊。你可能有一些不需要任何局部状态的组件。在这种情况下，你可以自由地在多个容器对象之间使用同一个组件*实例*。在这一点上，它确实更像是一个策略。
---

## C++ Code (原书代码)

```cpp
// === 原始的单体类 Bjorn（不含组件模式）===
class Bjorn {
public:
  Bjorn()
  : velocity_(0), x_(0), y_(0)
  {}

  void update(World& world, Graphics& graphics);

private:
  static const int WALK_ACCELERATION = 1;
  int velocity_;
  int x_, y_;
  Volume volume_;
  Sprite spriteStand_;
  Sprite spriteWalkLeft_;
  Sprite spriteWalkRight_;
};

void Bjorn::update(World& world, Graphics& graphics) {
  // 处理用户输入
  switch (Controller::getJoystickDirection()) {
    case DIR_LEFT:  velocity_ -= WALK_ACCELERATION; break;
    case DIR_RIGHT: velocity_ += WALK_ACCELERATION; break;
  }

  // 物理更新
  x_ += velocity_;
  world.resolveCollision(volume_, x_, y_, velocity_);

  // 渲染
  Sprite* sprite = &spriteStand_;
  if (velocity_ < 0) sprite = &spriteWalkLeft_;
  else if (velocity_ > 0) sprite = &spriteWalkRight_;
  graphics.draw(*sprite, x_, y_);
}

// === 使用组件模式重构 ===
class InputComponent {
public:
  void update(Bjorn& bjorn) {
    switch (Controller::getJoystickDirection()) {
      case DIR_LEFT:  bjorn.velocity -= WALK_ACCELERATION; break;
      case DIR_RIGHT: bjorn.velocity += WALK_ACCELERATION; break;
    }
  }
private:
  static const int WALK_ACCELERATION = 1;
};

class PhysicsComponent {
public:
  void update(Bjorn& bjorn, World& world) {
    bjorn.x += bjorn.velocity;
    world.resolveCollision(volume_, bjorn.x, bjorn.y, bjorn.velocity);
  }
private:
  Volume volume_;
};

class GraphicsComponent {
public:
  void update(Bjorn& bjorn, Graphics& graphics) {
    Sprite* sprite = &spriteStand_;
    if (bjorn.velocity < 0) sprite = &spriteWalkLeft_;
    else if (bjorn.velocity > 0) sprite = &spriteWalkRight_;
    graphics.draw(*sprite, bjorn.x, bjorn.y);
  }
private:
  Sprite spriteStand_, spriteWalkLeft_, spriteWalkRight_;
};

// === 瘦身后的 Bjorn（组件容器）===
class Bjorn {
public:
  int velocity;
  int x, y;

  void update(World& world, Graphics& graphics) {
    input_.update(*this);
    physics_.update(*this, world);
    graphics_.update(*this, graphics);
  }

private:
  InputComponent input_;
  PhysicsComponent physics_;
  GraphicsComponent graphics_;
};

// === 通过接口实现运行时多态 ===
class InputComponent {
public:
  virtual ~InputComponent() {}
  virtual void update(Bjorn& bjorn) = 0;
};

class PlayerInputComponent : public InputComponent {
  // 从控制器读取输入
};

class DemoInputComponent : public InputComponent {
  // AI自动控制，用于演示模式
};

// 构建时注入不同组件
Bjorn* bjorn = new Bjorn(new PlayerInputComponent());
Bjorn* demo = new Bjorn(new DemoInputComponent());
```

## C# Equivalent (C# 对照实现)

```csharp
using UnityEngine;

// ──────────────────────────────────────────────
// 组件模式的 C# 实现 —— 这正是 Unity 的核心架构！
// 在 Unity 中，GameObject 就是"实体容器"，
// MonoBehaviour 就是"组件"。
// ──────────────────────────────────────────────

// === C# 版本：实体容器（类比 Unity 的 GameObject）===
public class Entity
{
    // 实体的共享状态（位置、速度），类比 Transform
    public int X { get; set; }
    public int Y { get; set; }
    public int Velocity { get; set; }

    // 持有组件的引用（Unity 中用 GetComponent 查找）
    private readonly List<IComponent> _components = new();

    // 注册组件（类似 AddComponent<T>）
    public void AddComponent(IComponent component)
    {
        _components.Add(component);
        component.Owner = this;  // 让组件知道其所属实体
    }

    // 按类型获取组件（类似 GetComponent<T>）
    public T GetComponent<T>() where T : class, IComponent
    {
        return _components.OfType<T>().FirstOrDefault();
    }

    // 每帧更新所有组件（类似 MonoBehaviour.Update）
    public void Update()
    {
        foreach (var component in _components)
        {
            component.Update();
        }
    }
}

// 组件基类接口（类比 MonoBehaviour 的底层接口）
public interface IComponent
{
    Entity Owner { get; set; }
    void Update();
}

// === 输入组件（处理玩家输入）===
public class PlayerInputComponent : IComponent
{
    public Entity Owner { get; set; }

    private const int WalkAcceleration = 1;

    public void Update()
    {
        // 从 Unity 的输入系统读取（类比 Controller.GetJoystickDirection）
        if (Input.GetKey(KeyCode.A))
            Owner.Velocity -= WalkAcceleration;
        if (Input.GetKey(KeyCode.D))
            Owner.Velocity += WalkAcceleration;
    }
}

// === 物理组件（处理移动与碰撞）===
public class PhysicsComponent : IComponent
{
    public Entity Owner { get; set; }

    public void Update()
    {
        // 更新位置（简化版）
        Owner.X += Owner.Velocity;
        // 实际项目中应调用 Physics2D / Physics 进行碰撞检测
        // 例如：Physics2D.OverlapCircle(...)
    }
}

// === 渲染组件（处理视觉表现）===
public class GraphicsComponent : IComponent
{
    public Entity Owner { get; set; }

    // 精灵引用（相当于原书中的 Sprite 成员）
    private readonly Sprite _spriteStand;
    private readonly Sprite _spriteWalkLeft;
    private readonly Sprite _spriteWalkRight;

    public GraphicsComponent(Sprite stand, Sprite walkLeft, Sprite walkRight)
    {
        _spriteStand = stand;
        _spriteWalkLeft = walkLeft;
        _spriteWalkRight = walkRight;
    }

    public void Update()
    {
        // 根据速度方向选择精灵
        Sprite currentSprite = _spriteStand;
        if (Owner.Velocity < 0)
            currentSprite = _spriteWalkLeft;
        else if (Owner.Velocity > 0)
            currentSprite = _spriteWalkRight;

        // 绘制（在 Unity 中通常通过 SpriteRenderer 自动完成）
        // 此处仅为概念演示
    }
}

// === 组件模式的实际使用（组合不同的组件来构建不同实体）===
public class GameManager
{
    public void BuildEntities()
    {
        // 构建玩家实体 —— 拥有所有组件
        Entity player = new();
        player.AddComponent(new PlayerInputComponent());
        player.AddComponent(new PhysicsComponent());
        player.AddComponent(new GraphicsComponent(
            Resources.Load<Sprite>("stand"),
            Resources.Load<Sprite>("walkLeft"),
            Resources.Load<Sprite>("walkRight")
        ));

        // 构建装饰物实体 —— 只有渲染，没有物理和输入
        Entity decoration = new();
        decoration.AddComponent(new GraphicsComponent(
            Resources.Load<Sprite>("bush"),
            null, null
        ));

        // 构建区域触发器 —— 只有物理，没有渲染
        Entity zone = new();
        zone.AddComponent(new PhysicsComponent());
    }
}

// ──────────────────────────────────────────────
// Unity 的实际实现方式
// ──────────────────────────────────────────────

// Unity 已经为我们实现了这个模式！
// 在 Unity 中：
//   - GameObject  = Entity（容器）
//   - Transform  = 共享的位置/旋转/缩放数据
//   - MonoBehaviour = 组件基类
//   - AddComponent<T>() = 注册组件
//   - GetComponent<T>() = 按类型查找组件

// Unity 的使用方式：
// GameObject player = new GameObject("Player");
// player.AddComponent<Rigidbody2D>();       // 物理组件
// player.AddComponent<SpriteRenderer>();    // 渲染组件
// player.AddComponent<PlayerController>();  // 自定义输入组件

// 在自定义的 MonoBehaviour 中访问其他组件：
// Rigidbody2D rb = GetComponent<Rigidbody2D>();
// SpriteRenderer sr = GetComponent<SpriteRenderer>();

// 这就是为什么我们说 Unity 的整个架构就是组件模式！
```

## Unity Application / Unity 应用场景

Unity 的**整个引擎架构**就是组件模式的最佳实践。以下是最关键的应用场景：

### 1. GameObject + MonoBehaviour（Unity 的核心）
- `GameObject` 是实体容器，类似于上文 C# 实现中的 `Entity` 类
- `MonoBehaviour` 是所有组件的基类，类似于 `IComponent` 接口
- `Transform` 是共享的位置/旋转/缩放数据，类似于 `Entity` 中的 `X`、`Y`、`Velocity`
- 每个 `MonoBehaviour` 可以通过 `GetComponent<T>()` 获取同一 `GameObject` 上的其他组件
- **例子**：一个敌人 GameObject 可能同时挂载 `Rigidbody`（物理）、`Animator`（动画）、`EnemyAI`（AI）、`AudioSource`（音频）等多个组件

### 2. ECS（Entity Component System）
- Unity 的 DOTS（Data-Oriented Technology Stack）中的 ECS 是该模式的极致形式
- Entity 只是一个 ID（整数），没有任何方法
- Component 只是纯数据（struct），没有行为
- System 负责处理拥有特定组件的所有实体
- **优势**：极致的数据局部性（Data Locality）、CPU 缓存友好、适合多线程

### 3. 常见组件组合
```
玩家:   Transform + MeshRenderer + Rigidbody + PlayerInput + Health
敌人:   Transform + MeshRenderer + Rigidbody + EnemyAI + Health + DropLoot
道具:   Transform + MeshRenderer + Collider + PickupEffect
子弹:   Transform + MeshRenderer + Rigidbody + Projectile + Trail
UI按钮:  RectTransform + Image + Button + EventTrigger
```

### 4. 实际开发技巧
```csharp
// 通过 GetComponent 实现组件间通信（不破坏解耦）
public class PlayerController : MonoBehaviour
{
    private Rigidbody _rb;
    private Animator _anim;

    void Awake()
    {
        // 在 Awake 中获取引用，避免每帧 GetComponent
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 通过共享状态（Transform）隐式通信
        // 或通过 GetComponent 直接调用其他组件方法
        float speed = _rb.velocity.magnitude;
        _anim.SetFloat("Speed", speed);
    }
}
```

### 5. SendMessage 与消息系统
Unity 提供了 `SendMessage` / `BroadcastMessage` / `SendMessageUpwards` 方法，实现了组件之间通过消息的通信方式（类似原书中讨论的消息传递方案）：

```csharp
// 发送消息（性能开销大，不推荐频繁使用）
gameObject.SendMessage("OnPlayerDied");

// 推荐使用事件/Action 或 UnityEvent
// 或使用 C# 事件 + 接口的方式
```

## Key Differences / 关键差异

| Aspect | C++ (原书) | C# / Unity |
|--------|-----------|------------|
| **容器类** | 自定义 `Bjorn` 或 `GameObject` | Unity 内置的 `GameObject` |
| **组件基类** | 自定义抽象接口 | `MonoBehaviour`（由 Unity 管理生命周期） |
| **组件注册** | 构造函数注入 | `AddComponent<T>()`（Unity 自动管理） |
| **组件查找** | 直接持有指针 | `GetComponent<T>()`（基于类型反射） |
| **共享状态** | 由容器类持有（如 `x`, `y`） | `Transform` 组件统一管理位置/旋转/缩放 |
| **生命周期** | 手动管理（new/delete） | Unity 自动管理（Awake/OnEnable/Start/Update/OnDestroy） |
| **多态** | 虚函数（virtual） | 虚方法或 C# 接口（interface） |
| **序列化** | 无原生支持 | Unity 序列化支持 Inspector 显示和 prefab 保存 |
| **内存模型** | 数组/列表持有组件指针 | Unity 内部基于 chunk 的内存分配（ECS 更优） |
| **性能开销** | 虚函数间接调用 | 托管代码 + 反射（GetComponent 有开销） |

### 核心理解

**原书模式 vs Unity 实现的关键区别**：

1. **Unity 是组件模式的终极实践**：原书描述的场景在 Unity 中就是日常工作。Unity 的 `GameObject` + `MonoBehaviour` 架构本身就是组件模式的标准实现。

2. **容器更轻量**：Unity 的 `GameObject` 比原书的 `Bjorn` 更加轻量——它几乎没有任何行为，完全依赖挂载的组件来定义实体的行为和外观。

3. **组件间通信**：原书讨论了三种方案（共享状态、直接引用、消息传递），Unity 三者都支持：
   - **共享状态**：通过 `Transform` 共享位置
   - **直接引用**：通过 `GetComponent<T>()`（最常见的模式）
   - **消息传递**：通过 `SendMessage`、`UnityEvent` 或 C# 事件

4. **ECS 走向极致**：Unity 的 DOTS/ECS 将组件模式推向极致——Entity 退化为一个 ID，Component 变成纯数据（struct），行为全部由 System 负责。这解决了原书中提到的组件通信复杂性和内存局部性问题。

---

# Event Queue — 事件队列

## Intent / 意图

* Decouple when a message or event is sent from when it is processed.

* 将消息或事件的发送时刻与其处理时刻解耦。

---

## Motivation / 动机

* Unless you live under one of the few rocks that still lack Internet access, you've probably already heard of an "event queue". If not, maybe "message queue", or "event loop", or "message pump" rings a bell. To refresh your memory, let's walk through a couple of common manifestations of the pattern.

For most of the chapter, I use "event" and "message" interchangeably. Where the distinction matters, I'll make it obvious.

**CN:** 除非你生活在少数几个仍然没有互联网接入的石头下面，否则你可能已经听说过"事件队列"。如果没有，也许"消息队列"、"事件循环"或"消息泵"会让你有印象。为了唤起你的记忆，让我们来看看该模式的几种常见表现形式。

在本章大部分内容中，我会互换使用"事件"和"消息"。在需要区分的地方，我会明确说明。

### GUI Event Loops / GUI 事件循环

* If you've ever done any user interface programming, then you're well acquainted with *events*. Every time the user interacts with your program — clicks a button, pulls down a menu, or presses a key — the operating system generates an event. It throws this object at your app, and your job is to grab it and hook it up to some interesting behavior.

This application style is so common, it's considered a paradigm: [*event-driven programming*](http://en.wikipedia.org/wiki/Event-driven_programming).

In order to receive these missives, somewhere deep in the bowels of your code is an *event loop*. It looks roughly like this:

```cpp
while (running) {
  Event event = getNextEvent();
  // Handle event...
}
```

The call to `getNextEvent()` pulls a bit of unprocessed user input into your app. You route it to an event handler and, like magic, your application comes to life. The interesting part is that the application *pulls* in the event when *it* wants it. The operating system doesn't just immediately jump to some code in your app when the user pokes a peripheral.

In contrast, *interrupts* from the operating system *do* work like that. When an interrupt happens, the OS stops whatever your app was doing and forces it to jump to an interrupt handler. This abruptness is why interrupts are so hard to work with.

That means when user input comes in, it needs to go somewhere so that the operating system doesn't lose it between when the device driver reported the input and when your app gets around to calling `getNextEvent()`. That "somewhere" is a *queue*.

When user input comes in, the OS adds it to a queue of unprocessed events. When you call `getNextEvent()`, that pulls the oldest event off the queue and hands it to your application.

**CN:** 如果你曾经做过任何用户界面编程，那么你对*事件*一定很熟悉。每次用户与你的程序交互——点击按钮、下拉菜单或按下按键——操作系统都会生成一个事件。它把这个对象抛给你的应用，你的工作就是抓住它并将其连接到一些有趣的行为上。

这种应用风格非常普遍，以至于它被认为是一种范式：[*事件驱动编程*](http://en.wikipedia.org/wiki/Event-driven_programming)。

为了接收这些消息，在你的代码深处某处有一个*事件循环*。它大致看起来像这样：

```cpp
while (running) {
  Event event = getNextEvent();
  // Handle event...
}
```

调用 `getNextEvent()` 会将一些未处理的用户输入拉入你的应用。你将其路由到事件处理器，就像魔法一样，你的应用开始运行。有趣的部分在于，应用在*它自己*想要的时候*拉取*事件。当用户操作外设时，操作系统不会立即跳转到你应用中的某段代码。

相比之下，操作系统的*中断*确实是那样工作的。当中断发生时，OS 会停止你的应用正在做的任何事情，并强制它跳转到中断处理程序。这种突兀性正是中断难以处理的原因。

这意味着当用户输入进来时，它需要去某个地方，以便操作系统不会在设备驱动程序报告输入和你的应用调用 `getNextEvent()` 之间丢失它。这个"某个地方"就是一个*队列*。

当用户输入进来时，OS 将其添加到未处理事件的队列中。当你调用 `getNextEvent()` 时，它会从队列中取出最旧的事件并交给你的应用程序。

### Central Event Bus / 中央事件总线

* Most games aren't event-driven like this, but it is common for a game to have its own event queue as the backbone of its nervous system. You'll often hear "central", "global", or "main" used to describe it. It's used for high level communication between game systems that want to stay decoupled.

If you want to know *why* they aren't event-driven, crack open the [Game Loop](game-loop.html) chapter.

Say your game has a tutorial system to display help boxes after specific in-game events. For example, the first time the player vanquishes a foul beastie, you want to show a little balloon that says, "Press X to grab the loot!"

Tutorial systems are a pain to implement gracefully, and most players will spend only a fraction of their time using in-game help, so it feels like they aren't worth the effort. But that fraction where they *are* using the tutorial can be invaluable for easing the player into your game.

Your gameplay and combat code are likely complex enough as it is. The last thing you want to do is stuff a bunch of checks for triggering tutorials in there. Instead, you could have a central event queue. Any game system can send to it, so the combat code can add an "enemy died" event every time you slay a foe.

Likewise, any game system can *receive* events from the queue. The tutorial engine registers itself with the queue and indicates it wants to receive "enemy died" events. This way, knowledge of an enemy dying makes its way from the combat system over to the tutorial engine without the two being directly aware of each other.

This model where you have a shared space that entities can post information to and get notified by is similar to [blackboard systems](http://en.wikipedia.org/wiki/Blackboard_system) in the AI field.

**CN:** 大多数游戏并不像这样是事件驱动的，但游戏拥有自己的事件队列作为其神经系统的骨干是很常见的。你经常会听到用"中央"、"全局"或"主"来描述它。它用于希望保持解耦的游戏系统之间的高层通信。

如果你想知道*为什么*它们不是事件驱动的，请翻开[游戏循环](game-loop.html)那一章。

假设你的游戏有一个教程系统，用于在特定的游戏内事件发生后显示帮助框。例如，当玩家第一次击败一个邪恶的怪物时，你想显示一个小气泡说："按 X 拾取战利品！"

教程系统实现起来很痛苦，而且大多数玩家只会花很少的时间使用游戏内帮助，所以感觉它们不值得付出努力。但玩家*正在*使用教程的那一小段时间，对于引导玩家进入你的游戏来说可能是非常宝贵的。

你的游戏玩法和战斗代码本身可能已经足够复杂。你最不想做的就是往里面塞一堆触发教程的检查。相反，你可以有一个中央事件队列。任何游戏系统都可以向它发送消息，所以战斗代码可以在每次你杀死敌人时添加一个"敌人死亡"事件。

同样，任何游戏系统都可以从队列中*接收*事件。教程引擎向队列注册自己，并表明它希望接收"敌人死亡"事件。这样，敌人死亡的信息就从战斗系统传到了教程引擎，而两者并不直接知道对方的存在。

这种拥有一个共享空间，实体可以向其发布信息并从中获得通知的模型，类似于 AI 领域的[黑板系统](http://en.wikipedia.org/wiki/Blackboard_system)。

### Say What? / 加入声音

* So, instead, let's add sound to our game. Humans are mainly visual animals, but hearing is deeply connected to our emotions and our sense of physical space. The right simulated echo can make a black screen feel like an enormous cavern, and a well-timed violin adagio can make your heartstrings hum in sympathetic resonance.

To get our game wound for sound, we'll start with the simplest possible approach and see how it goes. We'll add a little "audio engine" that has an API for playing a sound given an identifier and a volume:

While I almost always shy away from the [Singleton](singleton.html) pattern, this is one of the places where it may fit since the machine likely only has one set of speakers. I'm taking a simpler approach and just making the method static.

```cpp
class Audio {
public:
  static void playSound(SoundId id, int volume);
};
```

It's responsible for loading the appropriate sound resource, finding an available channel to play it on, and starting it up. This chapter isn't about some platform's real audio API, so I'll conjure one up that we can presume is implemented elsewhere. Using it, we write our method like so:

```cpp
void Audio::playSound(SoundId id, int volume) {
  ResourceId resource = loadSound(id);
  int channel = findOpenChannel();
  if (channel == -1) return;
  startSound(resource, channel, volume);
}
```

We check that in, create a few sound files, and start sprinkling `playSound()` calls through our codebase like some magical audio fairy. For example, in our UI code, we play a little bloop when the selected menu item changes:

```cpp
class Menu {
public:
  void onSelect(int index)
  {
    Audio::playSound(SOUND_BLOOP, VOL_MAX);
    // Other stuff...
  }
};
```

After doing this, we notice that sometimes when you switch menu items, the whole screen freezes for a few frames. We've hit our first issue:

- **Problem 1: The API blocks the caller until the audio engine has completely processed the request.**

Our `playSound()` method is *synchronous* — it doesn't return back to the caller until bloops are coming out of the speakers. If a sound file has to be loaded from disc first, that may take a while. In the meantime, the rest of the game is frozen.

Ignoring that for now, we move on. In the AI code, we add a call to let out a wail of anguish when an enemy takes damage from the player. Nothing warms a gamer's heart like inflicting simulated pain on a virtual living being.

It works, but sometimes when the hero does a mighty attack, it hits two enemies in the exact same frame. That causes the game to play the wail sound twice simultaneously. If you know anything about audio, you know mixing multiple sounds together sums their waveforms. When those are the *same* waveform, it's the same as *one* sound played *twice as loud*. It's jarringly loud.

I ran into this exact issue working on [Henry Hatsworth in the Puzzling Adventure](http://en.wikipedia.org/wiki/Henry_Hatsworth_in_the_Puzzling_Adventure). My solution there is similar to what we'll cover here.

We have a related problem in boss fights when piles of minions are running around causing mayhem. The hardware can only play so many sounds at one time. When we go over that limit, sounds get ignored or cut off.

To handle these issues, we need to look at the entire *set* of sound calls to aggregate and prioritize them. Unfortunately, our audio API handles each `playSound()` call independently. It sees requests through a pinhole, one at a time.

- **Problem 2: Requests cannot be processed in aggregate.**

These problems seem like mere annoyances compared to the next issue that falls in our lap. By now, we've strewn `playSound()` calls throughout the codebase in lots of different game systems. But our game engine is running on modern multi-core hardware. To take advantage of those cores, we distribute those systems on different threads — rendering on one, AI on another, etc.

Since our API is synchronous, it runs on the *caller's* thread. When we call it from different game systems, we're hitting our API concurrently from multiple threads. Look at that sample code. See any thread synchronization? Me neither.

This is particularly egregious because we intended to have a *separate* thread for audio. It's just sitting there totally idle while these other threads are busy stepping all over each other and breaking things.

- **Problem 3: Requests are processed on the wrong thread.**

The common theme to these problems is that the audio engine interprets a call to `playSound()` to mean, "Drop everything and play the sound right now!" *Immediacy* is the problem. Other game systems call `playSound()` at *their* convenience, but not necessarily when it's convenient for the audio engine to handle that request. To fix that, we'll decouple *receiving* a request from *processing* it.

**CN:** 那么，不如让我们给游戏添加声音。人类主要是视觉动物，但听觉与我们的情感和对物理空间的感知深深相连。恰到好处的模拟回声可以让一个黑屏感觉像一个巨大的洞穴，而适时的小提琴柔板可以让你的心弦共鸣。

为了让我们的游戏充满声音，我们从最简单的方法开始，看看效果如何。我们将添加一个小型"音频引擎"，它有一个 API，可以根据给定的标识符和音量播放声音：

虽然我几乎总是回避[单例](singleton.html)模式，但这里可能是它适合的地方之一，因为机器可能只有一组扬声器。我采用更简单的方法，只是让方法成为静态的。

```cpp
class Audio {
public:
  static void playSound(SoundId id, int volume);
};
```

它负责加载合适的声音资源，找到一个可用的通道来播放，然后启动它。本章不是关于某个平台的真实音频 API，所以我将虚构一个，并假定它在其他地方已实现。使用它，我们这样编写方法：

```cpp
void Audio::playSound(SoundId id, int volume) {
  ResourceId resource = loadSound(id);
  int channel = findOpenChannel();
  if (channel == -1) return;
  startSound(resource, channel, volume);
}
```

我们将其签入，创建一些声音文件，然后像魔法音频仙女一样，开始在代码库中撒播 `playSound()` 调用。例如，在我们的 UI 代码中，当选中的菜单项改变时，播放一个小小的"哔"声：

```cpp
class Menu {
public:
  void onSelect(int index)
  {
    Audio::playSound(SOUND_BLOOP, VOL_MAX);
    // Other stuff...
  }
};
```

做完这些之后，我们注意到有时当切换菜单项时，整个画面会卡顿几帧。我们遇到了第一个问题：

- **问题 1：API 会阻塞调用者，直到音频引擎完全处理完请求。**

我们的 `playSound()` 方法是*同步的*——在扬声器发出哔哔声之前，它不会返回给调用者。如果声音文件必须先从磁盘加载，那可能需要一段时间。在此期间，游戏的其余部分被冻结。

暂时忽略这个问题，我们继续前进。在 AI 代码中，我们添加了一个调用，当敌人受到玩家伤害时发出痛苦的哀嚎。没有什么比给虚拟生物施加模拟痛苦更能温暖游戏玩家的心了。

它起作用了，但有时当英雄发动强力攻击时，它会在完全相同的帧内击中两个敌人。这导致游戏同时播放两次哀嚎声。如果你对音频有所了解，你会知道混合多个声音会将它们的波形相加。当这些是*相同*的波形时，就相当于*一个*声音以*两倍*音量播放。这刺耳地响亮。

我在开发 [Henry Hatsworth in the Puzzling Adventure](http://en.wikipedia.org/wiki/Henry_Hatsworth_in_the_Puzzling_Adventure) 时正好遇到了这个问题。我在那里的解决方案与我们将要介绍的内容类似。

在 Boss 战中，当大量小兵四处乱跑制造混乱时，我们还有一个相关问题。硬件一次只能播放有限数量的声音。当我们超过这个限制时，声音会被忽略或被切断。

为了处理这些问题，我们需要查看整个*集合*的声音调用来聚合和排序它们。不幸的是，我们的音频 API 独立处理每个 `playSound()` 调用。它通过针孔一次看到一个请求。

- **问题 2：请求无法被聚合处理。**

与接下来落到我们头上的问题相比，这些问题似乎只是小麻烦。到现在为止，我们已经将 `playSound()` 调用散布到许多不同游戏系统的代码库中。但我们的游戏引擎运行在现代多核硬件上。为了利用这些核心，我们将这些系统分配到不同的线程上——渲染在一个线程上，AI 在另一个线程上，等等。

由于我们的 API 是同步的，它在*调用者*的线程上运行。当我们从不同的游戏系统调用它时，我们是并发地从多个线程访问我们的 API。看看那个示例代码。看到任何线程同步吗？我也没有。

这尤其令人震惊，因为我们本来打算为音频设置一个*单独的*线程。它就坐在那里完全空闲，而这些其他线程正忙着互相踩踏和破坏东西。

- **问题 3：请求在错误的线程上被处理。**

这些问题的共同主题是，音频引擎将 `playSound()` 调用解释为"放下一切，立即播放声音！"*即时性*是问题所在。其他游戏系统在*它们*方便的时候调用 `playSound()`，但不一定是在音频引擎方便处理该请求的时候。为了解决这个问题，我们将*接收*请求与*处理*请求解耦。

---

## The Pattern / 模式

* A **queue** stores a series of **notifications or requests** in first-in, first-out order. Sending a notification **enqueues the request and returns**. The request processor then **processes items from the queue** at a later time. Requests can be **handled directly** or **routed to interested parties**. This **decouples the sender from the receiver** both **statically** and **in time**.

* **队列**按先进先出的顺序存储一系列**通知或请求**。发送通知将**请求入队并立即返回**。请求处理器稍后**从队列中处理条目**。请求可以被**直接处理**或**路由给感兴趣的各方**。这在**静态层面**和**时间层面**上**解耦了发送方与接收方**。

---

## When to Use It / 何时使用

* If you only want to decouple *who* receives a message from its sender, patterns like [Observer](observer.html) and [Command](command.html) will take care of this with less complexity. You only need a queue when you want to decouple something *in time*.

I mention this in nearly every chapter, but it's worth emphasizing. Complexity slows you down, so treat simplicity as a precious resource.

I think of it in terms of pushing and pulling. You have some code A that wants another chunk B to do some work. The natural way for A to initiate that is by *pushing* the request to B.

Meanwhile, the natural way for B to process that request is by *pulling* it in at a convenient time in *its* run cycle. When you have a push model on one end and a pull model on the other, you need a buffer between them. That's what a queue provides that simpler decoupling patterns don't.

Queues give control to the code that pulls from it — the receiver can delay processing, aggregate requests, or discard them entirely. But queues do this by taking control *away* from the sender. All the sender can do is throw a request on the queue and hope for the best. This makes queues a poor fit when the sender needs a response.

**CN:** 如果你只是想解耦*谁*从发送方接收消息，像[观察者](observer.html)和[命令](command.html)这样的模式可以以更低的复杂度做到这一点。只有当你想在*时间上*解耦某些东西时，你才需要一个队列。

我几乎在每一章都提到这一点，但值得强调。复杂性会拖慢你的速度，所以要把简单性当作宝贵的资源。

我把它看作是推和拉。你有代码 A 想要另一段代码 B 做一些工作。A 发起这个请求的自然方式是将请求*推送*给 B。

与此同时，B 处理该请求的自然方式是在*其*运行周期的方便时刻*拉取*它。当你一端有推送模型，另一端有拉取模型时，你需要在它们之间有一个缓冲区。这就是队列所提供的、更简单的解耦模式所不具备的。

队列将控制权交给了从其中拉取数据的代码——接收方可以延迟处理、聚合请求或完全丢弃它们。但队列这样做是将控制权从发送方*拿走*。发送方所能做的就是将请求扔到队列上并希望得到最好的结果。这使得队列在发送方需要响应时成为一个糟糕的选择。

---

## Keep in Mind / 注意事项

* Unlike some more modest patterns in this book, event queues are complex and tend to have a wide-reaching effect on the architecture of our games. That means you'll want to think hard about how — or if — you use one.

* 与本书中一些更温和的模式不同，事件队列很复杂，并且往往对我们游戏的架构有广泛的影响。这意味着你需要认真思考如何——或者是否——使用它。

### A Central Event Queue Is a Global Variable / 中央事件队列是一个全局变量

* One common use of this pattern is for a sort of Grand Central Station that all parts of the game can route messages through. It's a powerful piece of infrastructure, but *powerful* doesn't always mean *good*.

It took a while, but most of us learned the hard way that global variables are bad. When you have a piece of state that any part of the program can poke at, all sorts of subtle interdependencies creep in. This pattern wraps that state in a nice little protocol, but it's still a global, with all of the danger that entails.

**CN:** 这个模式的一个常见用途是作为一种"中央车站"，游戏的所有部分都可以通过它路由消息。它是一个强大的基础设施，但*强大*并不总是意味着*好*。

虽然花了一些时间，但我们大多数人通过艰难的方式学到了全局变量是不好的。当有一个程序任何部分都可以修改的状态时，各种微妙的相互依赖关系就会悄悄出现。这个模式将那个状态包装在一个漂亮的小协议中，但它仍然是一个全局变量，具有随之而来的所有危险。

### The State of the World Can Change Under You / 世界状态可能在你不经意间改变

* Say some AI code posts an "entity died" event to a queue when a virtual minion shuffles off its mortal coil. That event hangs out in the queue for who knows how many frames until it eventually works its way to the front and gets processed.

Meanwhile, the experience system wants to track the heroine's body count and reward her for her grisly efficiency. It receives each "entity died" event and determines the kind of entity slain and the difficulty of the kill so it can dish out an appropriate reward.

That requires various pieces of state in the world. We need the entity that died so we can see how tough it was. We may want to inspect its surroundings to see what other obstacles or minions were nearby. But if the event isn't received until later, that stuff may be gone. The entity may have been deallocated, and other nearby foes may have wandered off.

When you receive an event, you have to be careful not to assume the *current* state of the world reflects how the world was *when the event was raised*. This means queued events tend to be more data heavy than events in synchronous systems. With the latter, the notification can say "something happened" and the receiver can look around for the details. With a queue, those ephemeral details must be captured when the event is sent so they can be used later.

**CN:** 假设某个 AI 代码在一个虚拟小兵死亡时向队列发布了一个"实体死亡"事件。该事件在队列中停留了不知道多少个帧，直到最终排到前面被处理。

与此同时，经验系统想要跟踪女主角的杀敌数，并为她残忍的效率给予奖励。它接收每个"实体死亡"事件，并确定被杀实体的种类和击杀的难度，以便给予适当的奖励。

这需要世界中的各种状态。我们需要已死亡的实体来查看它有多强。我们可能想检查它周围的环境，看看附近有什么其他障碍或小兵。但如果事件稍后才被接收，这些东西可能已经消失了。该实体可能已被释放，附近的其他敌人可能已经走开了。

当你接收到一个事件时，你必须小心，不要假设世界的*当前*状态反映了事件*被引发时*世界的状态。这意味着排队的事件往往比同步系统中的事件携带更多数据。在同步系统中，通知可以说"某事发生了"，接收者可以环顾四周寻找细节。而使用队列，那些短暂易逝的细节必须在事件发送时捕获，以便以后使用。

### You Can Get Stuck in Feedback Loops / 你可能会陷入反馈循环

* All event and message systems have to worry about cycles:

1. A sends an event.
2. B receives it and responds by sending an event.
3. That event happens to be one that A cares about, so it receives it. In response, it sends an event...
4. Go to 2.

When your messaging system is *synchronous*, you find cycles quickly — they overflow the stack and crash your game. With a queue, the asynchrony unwinds the stack, so the game may keep running even though spurious events are sloshing back and forth in there. A common rule to avoid this is to avoid *sending* events from within code that's *handling* one.

A little debug logging in your event system is probably a good idea too.

**CN:** 所有事件和消息系统都必须担心循环：

1. A 发送一个事件。
2. B 接收它，并通过发送一个事件来响应。
3. 那个事件恰好是 A 关心的，所以 A 接收了它。作为回应，A 又发送了一个事件……
4. 回到第 2 步。

当你的消息系统是*同步的*，你会很快发现循环——它们会使栈溢出并导致游戏崩溃。而使用队列，异步性展开了栈，所以游戏可能继续运行，即使虚假的事件在其中来回激荡。避免这种情况的一个常见规则是避免在*处理*事件的代码中*发送*事件。

在你的事件系统中添加一点调试日志可能也是个好主意。

---

## Sample Code / 示例代码

* We've already seen some code. It's not perfect, but it has the right basic functionality — the public API we want and the right low-level audio calls. All that's left for us to do now is fix its problems.

The first is that our API *blocks*. When a piece of code plays a sound, it can't do anything else until `playSound()` finishes loading the resource and actually starts making the speaker wiggle.

We want to defer that work until later so that `playSound()` can return quickly. To do that, we need to *reify* the request to play a sound. We need a little structure that stores the details of a pending request so we can keep it around until later:

```cpp
struct PlayMessage {
  SoundId id;
  int volume;
};
```

Next, we need to give `Audio` some storage space to keep track of these pending play messages. Now, your algorithms professor might tell you to use some exciting data structure here like a [Fibonacci heap](http://en.wikipedia.org/wiki/Fibonacci_heap) or a [skip list](http://en.wikipedia.org/wiki/Skip_list), or, hell, at least a *linked* list. But in practice, the best way to store a bunch of homogenous things is almost always a plain old array:

Algorithm researchers get paid to publish analyses of novel data structures. They aren't exactly incentivized to stick to the basics.

- No dynamic allocation.
- No memory overhead for bookkeeping information or pointers.
- Cache-friendly contiguous memory usage.

For lots more on what being "cache friendly" means, see the chapter on [Data Locality](data-locality.html).

So let's do that:

```cpp
class Audio {
public:
  static void init()
  {
    numPending_ = 0;
  }

  // Other stuff...
private:
  static const int MAX_PENDING = 16;

  static PlayMessage pending_[MAX_PENDING];
  static int numPending_;
};
```

We can tune the array size to cover our worst case. To play a sound, we simply slot a new message in there at the end:

```cpp
void Audio::playSound(SoundId id, int volume) {
  assert(numPending_ < MAX_PENDING);

  pending_[numPending_].id = id;
  pending_[numPending_].volume = volume;
  numPending_++;
}
```

This lets `playSound()` return almost instantly, but we do still have to play the sound, of course. That code needs to go somewhere, and that somewhere is an `update()` method:

```cpp
class Audio {
public:
  static void update()
  {
    for (int i = 0; i < numPending_; i++)
    {
      ResourceId resource = loadSound(pending_[i].id);
      int channel = findOpenChannel();
      if (channel == -1) return;
      startSound(resource, channel, pending_[i].volume);
    }

    numPending_ = 0;
  }

  // Other stuff...
};
```

As the name implies, this is the [Update Method](update-method.html) pattern.

Now, we need to call that from somewhere convenient. What "convenient" means depends on your game. It may mean calling it from the main [game loop](game-loop.html) or from a dedicated audio thread.

This works fine, but it does presume we can process *every* sound request in a single call to `update()`. If you're doing something like processing a request asynchronously after its sound resource is loaded, that won't work. For `update()` to work on one request at a time, it needs to be able to pull requests out of the buffer while leaving the rest. In other words, we need an actual queue.

**CN:** 我们已经看到了一些代码。它并不完美，但它有正确的基本功能——我们想要的公共 API 和正确的底层音频调用。我们现在要做的就是解决它的缺陷。

第一个问题是我们的 API *会阻塞*。当一段代码播放声音时，它不能做任何其他事情，直到 `playSound()` 完成加载资源并实际开始让扬声器振动。

我们想把这个工作推迟到以后，这样 `playSound()` 可以快速返回。为此，我们需要将播放声音的请求*具体化*。我们需要一个小结构体来存储待处理请求的详细信息，这样我们可以把它保留到以后：

```cpp
struct PlayMessage {
  SoundId id;
  int volume;
};
```

接下来，我们需要给 `Audio` 一些存储空间来跟踪这些待处理的播放消息。现在，你的算法教授可能会告诉你在这里使用一些令人兴奋的数据结构，比如[斐波那契堆](http://en.wikipedia.org/wiki/Fibonacci_heap)或[跳跃列表](http://en.wikipedia.org/wiki/Skip_list)，或者，至少是*链表*。但在实践中，存储一堆同质数据的最佳方式几乎总是一个普通的旧数组：

算法研究人员靠发表新颖数据结构的分析而获得报酬。他们并没有被激励去坚持基础的东西。

- 没有动态分配。
- 没有用于记账信息或指针的内存开销。
- 缓存友好的连续内存使用。

关于"缓存友好"意味着什么的更多信息，请参见[数据局部性](data-locality.html)章节。

所以我们这样做：

```cpp
class Audio {
public:
  static void init()
  {
    numPending_ = 0;
  }

  // Other stuff...
private:
  static const int MAX_PENDING = 16;

  static PlayMessage pending_[MAX_PENDING];
  static int numPending_;
};
```

我们可以调整数组大小以覆盖最坏情况。要播放声音，我们只需在末尾放入一个新消息：

```cpp
void Audio::playSound(SoundId id, int volume) {
  assert(numPending_ < MAX_PENDING);

  pending_[numPending_].id = id;
  pending_[numPending_].volume = volume;
  numPending_++;
}
```

这让 `playSound()` 几乎立即返回，但我们当然仍然需要播放声音。那段代码需要放在某个地方，而这个地方就是 `update()` 方法：

```cpp
class Audio {
public:
  static void update()
  {
    for (int i = 0; i < numPending_; i++)
    {
      ResourceId resource = loadSound(pending_[i].id);
      int channel = findOpenChannel();
      if (channel == -1) return;
      startSound(resource, channel, pending_[i].volume);
    }

    numPending_ = 0;
  }

  // Other stuff...
};
```

顾名思义，这是[更新方法](update-method.html)模式。

现在，我们需要在方便的某个地方调用它。"方便"的含义取决于你的游戏。它可能意味着从主[游戏循环](game-loop.html)中调用，或者从专用的音频线程中调用。

这工作得很好，但它确实假设我们可以在单次 `update()` 调用中处理*每个*声音请求。如果你正在做类似于在声音资源加载后异步处理请求的事情，那就不行了。要让 `update()` 一次处理一个请求，它需要能够从缓冲区中取出请求，同时保留其余部分。换句话说，我们需要一个真正的队列。

### A Ring Buffer / 环形缓冲区

* There are a bunch of ways to implement queues, but my favorite is called a *ring buffer*. It preserves everything that's great about arrays while letting us incrementally remove items from the front of the queue.

Now, I know what you're thinking. If we remove items from the beginning of the array, don't we have to shift all of the remaining items over? Isn't that slow?

This is why they made us learn linked lists — you can remove nodes from them without having to shift things around. Well, it turns out you can implement a queue without any shifting in an array too. I'll walk you through it, but first let's get precise on some terms:

- The **head** of the queue is where requests are *read* from. The head is the oldest pending request.
- The **tail** is the other end. It's the slot in the array where the next enqueued request will be *written*. Note that it's just *past* the end of the queue. You can think of it as a half-open range, if that helps.

Since `playSound()` appends new requests at the end of the array, the head starts at element zero and the tail grows to the right.

Let's code that up. First, we'll tweak our fields a bit to make these two markers explicit in the class:

```cpp
class Audio {
public:
  static void init()
  {
    head_ = 0;
    tail_ = 0;
  }

  // Methods...
private:
  static int head_;
  static int tail_;

  // Array...
};
```

In the implementation of `playSound()`, `numPending_` has been replaced with `tail_`, but otherwise it's the same:

```cpp
void Audio::playSound(SoundId id, int volume) {
  assert(tail_ < MAX_PENDING);

  // Add to the end of the list.
  pending_[tail_].id = id;
  pending_[tail_].volume = volume;
  tail_++;
}
```

The more interesting change is in `update()`:

```cpp
void Audio::update() {
  // If there are no pending requests, do nothing.
  if (head_ == tail_) return;

  ResourceId resource = loadSound(pending_[head_].id);
  int channel = findOpenChannel();
  if (channel == -1) return;
  startSound(resource, channel, pending_[head_].volume);

  head_++;
}
```

We process the request at the head and then discard it by advancing the head pointer to the right. We detect an empty queue by seeing if there's any distance between the head and tail.

This is why we made the tail one *past* the last item. It means that the queue will be empty if the head and tail are the same index.

Now we've got a queue — we can add to the end and remove from the front. There's an obvious problem, though. As we run requests through the queue, the head and tail keep crawling to the right. Eventually, `tail_` hits the end of the array, and party time is over. This is where it gets clever.

Do you want party time to be over? No. You do not.

Notice that while the tail is creeping forward, the *head* is too. That means we've got array elements at the *beginning* of the array that aren't being used anymore. So what we do is wrap the tail back around to the beginning of the array when it runs off the end. That's why it's called a *ring* buffer — it acts like a circular array of cells.

Implementing that is remarkably easy. When we enqueue an item, we just need to make sure the tail wraps around to the beginning of the array when it reaches the end:

```cpp
void Audio::playSound(SoundId id, int volume) {
  assert((tail_ + 1) % MAX_PENDING != head_);

  // Add to the end of the list.
  pending_[tail_].id = id;
  pending_[tail_].volume = volume;
  tail_ = (tail_ + 1) % MAX_PENDING;
}
```

Replacing `tail_++` with an increment modulo the array size wraps the tail back around. The other change is the assertion. We need to ensure the queue doesn't overflow. As long as there are fewer than `MAX_PENDING` requests in the queue, there will be a little gap of unused cells between the head and the tail. If the queue fills up, those will be gone and, like some weird backwards Ouroboros, the tail will collide with the head and start overwriting it. The assertion ensures that this doesn't happen.

In `update()`, we wrap the head around too:

```cpp
void Audio::update() {
  // If there are no pending requests, do nothing.
  if (head_ == tail_) return;

  ResourceId resource = loadSound(pending_[head_].id);
  int channel = findOpenChannel();
  if (channel == -1) return;
  startSound(resource, channel, pending_[head_].volume);

  head_ = (head_ + 1) % MAX_PENDING;
}
```

There you go — a queue with no dynamic allocation, no copying elements around, and the cache-friendliness of a simple array.

If the maximum capacity bugs you, you can use a growable array. When the queue gets full, allocate a new array twice the size of the current array (or some other multiple), then copy the items over.

Even though you copy when they array grows, enqueuing an item still has constant *amortized* complexity.

**CN:** 实现队列有很多种方法，但我最喜欢的一种叫做*环形缓冲区*。它保留了数组的所有优点，同时让我们能够递增地从队列前面移除元素。

现在，我知道你在想什么。如果我们从数组的开头移除元素，难道不需要把所有剩余的元素都移过来吗？那不是很慢吗？

这就是他们让我们学习链表的原因——你可以从链表中移除节点而不需要移动其他东西。好吧，事实证明你也可以在数组中实现一个无需任何移动的队列。我将带你逐步了解，但首先让我们精确一些术语：

- 队列的**头部**是*读取*请求的位置。头部是最旧的待处理请求。
- **尾部**是另一端。它是数组中下一个入队请求将被*写入*的位置。注意，它就在队列末尾的*后面*。如果这对你有帮助，你可以把它想象成一个半开区间。

由于 `playSound()` 在数组末尾追加新请求，头部从元素 0 开始，尾部向右增长。

让我们来写代码。首先，我们稍微调整一下字段，使这两个标记在类中显式化：

```cpp
class Audio {
public:
  static void init()
  {
    head_ = 0;
    tail_ = 0;
  }

  // Methods...
private:
  static int head_;
  static int tail_;

  // Array...
};
```

在 `playSound()` 的实现中，`numPending_` 已被替换为 `tail_`，但其他方面相同：

```cpp
void Audio::playSound(SoundId id, int volume) {
  assert(tail_ < MAX_PENDING);

  // Add to the end of the list.
  pending_[tail_].id = id;
  pending_[tail_].volume = volume;
  tail_++;
}
```

更有趣的变化在 `update()` 中：

```cpp
void Audio::update() {
  // If there are no pending requests, do nothing.
  if (head_ == tail_) return;

  ResourceId resource = loadSound(pending_[head_].id);
  int channel = findOpenChannel();
  if (channel == -1) return;
  startSound(resource, channel, pending_[head_].volume);

  head_++;
}
```

我们处理头部的请求，然后通过将头部指针向右推进来丢弃它。我们通过查看头部和尾部之间是否有距离来检测队列是否为空。

这就是为什么我们将尾部设在最后一个元素的*后面*。这意味着如果头部和尾部是同一个索引，队列将为空。

现在我们有了一个队列——我们可以在末尾添加，从前面移除。但有一个明显的问题。当我们通过队列运行请求时，头部和尾部不断地向右爬行。最终，`tail_` 到达数组的末尾，派对时间结束了。这就是它变得巧妙的地方。

你希望派对时间结束吗？不。你不希望。

注意，当尾部向前移动时，*头部*也在移动。这意味着数组*开头*的数组元素不再被使用了。所以我们要做的是，当尾部跑出数组末尾时，将它绕回到数组的开头。这就是为什么它被称为*环形*缓冲区——它像一个圆形单元数组一样工作。

实现这一点非常容易。当我们入队一个条目时，我们只需要确保尾部在到达末尾时绕回到数组的开头：

```cpp
void Audio::playSound(SoundId id, int volume) {
  assert((tail_ + 1) % MAX_PENDING != head_);

  // Add to the end of the list.
  pending_[tail_].id = id;
  pending_[tail_].volume = volume;
  tail_ = (tail_ + 1) % MAX_PENDING;
}
```

将 `tail_++` 替换为按数组大小取模的递增，使尾部绕回去。另一个变化是断言。我们需要确保队列不会溢出。只要队列中的请求少于 `MAX_PENDING` 个，头部和尾部之间就会有一个小的未使用单元间隙。如果队列满了，这些间隙就会消失，就像某种奇怪的倒置衔尾蛇一样，尾部将与头部碰撞并开始覆盖它。断言确保这种情况不会发生。

在 `update()` 中，我们也让头部绕回：

```cpp
void Audio::update() {
  // If there are no pending requests, do nothing.
  if (head_ == tail_) return;

  ResourceId resource = loadSound(pending_[head_].id);
  int channel = findOpenChannel();
  if (channel == -1) return;
  startSound(resource, channel, pending_[head_].volume);

  head_ = (head_ + 1) % MAX_PENDING;
}
```

就是这样——一个没有动态分配、无需移动元素、并且具有简单数组缓存友好性的队列。

如果最大容量让你困扰，你可以使用可增长数组。当队列满时，分配一个当前数组大小两倍（或其他倍数）的新数组，然后复制条目过去。

即使你在数组增长时进行复制，入队一个条目仍然具有常数*摊还*复杂度。

### Aggregating Requests / 聚合请求

* Now that we've got a queue in place, we can move onto the other problems. The first is that multiple requests to play the same sound end up too loud. Since we know which requests are waiting to be processed now, all we need to do is merge a request if it matches an already pending one:

```cpp
void Audio::playSound(SoundId id, int volume) {
  // Walk the pending requests.
  for (int i = head_; i != tail_;
       i = (i + 1) % MAX_PENDING)
  {
    if (pending_[i].id == id)
    {
      // Use the larger of the two volumes.
      pending_[i].volume = max(volume, pending_[i].volume);

      // Don't need to enqueue.
      return;
    }
  }

  // Previous code...
}
```

When we get two requests to play the same sound, we collapse them to a single request for whichever is loudest. This "aggregation" is pretty rudimentary, but we could use the same idea to do more interesting batching.

Note that we're merging when the request is *enqueued*, not when it's *processed*. That's easier on our queue since we don't waste slots on redundant requests that will end up being collapsed later. It's also simpler to implement.

It does, however, put the processing burden on the caller. A call to `playSound()` will walk the entire queue before it returns, which could be slow if the queue is large. It may make more sense to aggregate in `update()` instead.

Another way to avoid the *O(n)* cost of scanning the queue is to use a different data structure. If we use a hash table keyed on the `SoundId`, then we can check for duplicates in constant time.

There's something important to keep in mind here. The window of "simultaneous" requests that we can aggregate is only as big as the queue. If we process requests more quickly and the queue size stays small, then we'll have fewer opportunities to batch things together. Likewise, if processing lags behind and the queue gets full, we'll find more things to collapse.

This pattern insulates the requester from knowing when the request gets processed, but when you treat the entire queue as a live data structure to be played with, then lag between making a request and processing it can visibly affect behavior. Make sure you're OK with that before doing this.

**CN:** 既然我们已经有了一个队列，我们可以继续处理其他问题了。第一个问题是，多个播放相同声音的请求最终会导致音量过大。既然我们现在知道哪些请求正在等待处理，我们所要做的就是在请求与已待处理的请求匹配时合并它：

```cpp
void Audio::playSound(SoundId id, int volume) {
  // Walk the pending requests.
  for (int i = head_; i != tail_;
       i = (i + 1) % MAX_PENDING)
  {
    if (pending_[i].id == id)
    {
      // Use the larger of the two volumes.
      pending_[i].volume = max(volume, pending_[i].volume);

      // Don't need to enqueue.
      return;
    }
  }

  // Previous code...
}
```

当我们得到两个播放相同声音的请求时，我们将它们合并为一个请求，使用音量较大的那个。这种"聚合"相当初级，但我们可以用同样的想法来做更有趣的批处理。

注意，我们在请求*入队*时进行合并，而不是在*处理*时。这对我们的队列来说更轻松，因为我们不会在最终会被合并的冗余请求上浪费槽位。实现起来也更简单。

不过，这确实将处理负担放在了调用者身上。`playSound()` 调用在返回之前会遍历整个队列，如果队列很大，这可能会很慢。也许在 `update()` 中聚合更有意义。

避免扫描队列的 *O(n)* 成本的另一种方法是使用不同的数据结构。如果我们使用以 `SoundId` 为键的哈希表，那么我们可以常数时间检查重复项。

这里有一件重要的事情要记住。我们可以聚合的"同时"请求窗口只有队列那么大。如果我们处理请求更快，队列大小保持较小，那么我们将有更少的批量处理机会。同样，如果处理落后，队列变满，我们会发现更多可以合并的东西。

这种模式使请求者不需要知道请求何时被处理，但是当你把整个队列当作一个可操作的实时数据结构时，发出请求和处理请求之间的延迟会明显地影响行为。在这样做之前，请确保你能接受这一点。

### Spanning Threads / 跨线程

* Finally, the most pernicious problem. With our synchronous audio API, whatever thread called `playSound()` was the thread that processed the request. That's often not what we want.

On today's multi-core hardware, you need more than one thread if you want to get the most out of your chip. There are infinite ways to distribute code across threads, but a common strategy is to move each domain of the game onto its own thread — audio, rendering, AI, etc.

Straight-line code only runs on a single core at a time. If you don't use threads, even if you do the asynchronous-style programming that's in vogue, the best you'll do is keep one core busy, which is a fraction of your CPU's abilities.

Server programmers compensate for that by splitting their application into multiple independent *processes*. That lets the OS run them concurrently on different cores. Games are almost always a single process, so a bit of threading really helps.

We're in good shape to do that now that we have three critical pieces:

1. The code for requesting a sound is decoupled from the code that plays it.
2. We have a queue for marshalling between the two.
3. The queue is encapsulated from the rest of the program.

All that's left is to make the methods that modify the queue — `playSound()` and `update()` — thread-safe. Normally, I'd whip up some concrete code to do that, but since this is a book about architecture, I don't want to get mired in the details of any specific API or locking mechanism.

At a high level, all we need to do is ensure that the queue isn't modified concurrently. Since `playSound()` does a very small amount of work — basically just assigning a few fields — it can lock without blocking processing for long. In `update()`, we wait on something like a condition variable so that we don't burn CPU cycles until there's a request to process.

**CN:** 最后，最棘手的问题。使用我们的同步音频 API，无论哪个线程调用 `playSound()`，都是那个线程处理该请求。这通常不是我们想要的。

在当今的多核硬件上，如果你想要充分利用你的芯片，你需要不止一个线程。在线程间分配代码的方法有无数种，但一个常见的策略是将游戏的每个领域放到自己的线程上——音频、渲染、AI 等。

直线代码一次只在一个核心上运行。如果你不使用线程，即使你做流行的异步风格编程，你最多也只能让一个核心保持忙碌，这只是你 CPU 能力的一小部分。

服务器程序员通过将他们的应用拆分为多个独立的*进程*来弥补这一点。这让 OS 在不同的核心上并发运行它们。游戏几乎总是一个单一进程，所以一点线程确实有帮助。

我们现在处于有利地位，因为我们有三个关键部分：

1. 请求声音的代码与播放声音的代码解耦。
2. 我们有一个队列来在两者之间编排。
3. 队列与程序的其余部分封装。

剩下的就是让修改队列的方法——`playSound()` 和 `update()`——成为线程安全的。通常，我会编写一些具体的代码来做到这一点，但由于这是一本关于架构的书，我不想陷入任何特定 API 或锁定机制的细节中。

在高层面上，我们需要做的就是确保队列不被并发修改。由于 `playSound()` 做了非常少量的工作——基本上只是赋值几个字段——它可以加锁而不会长时间阻塞处理。在 `update()` 中，我们在像条件变量这样的东西上等待，这样我们就不会在没有任何请求要处理时消耗 CPU 周期。

---

## Design Decisions / 设计决策

* Many games use event queues as a key part of their communication structure, and you can spend a ton of time designing all sorts of complex routing and filtering for messages. But before you go off and build something like the Los Angeles telephone switchboard, I encourage you to start simple. Here's a few starter questions to consider:

* 许多游戏使用事件队列作为其通信结构的关键部分，你可以花大量时间设计各种复杂的消息路由和过滤。但在你开始构建像洛杉矶电话交换机那样的东西之前，我鼓励你从简单开始。这里有几个入门问题需要考虑：

### What Goes in the Queue? / 队列里放什么？

* I've used "event" and "message" interchangeably so far because it mostly doesn't matter. You get the same decoupling and aggregation abilities regardless of what you're stuffing in the queue, but there are some conceptual differences.

- **If you queue events:**

    An "event" or "notification" describes something that *already* happened, like "monster died". You queue it so that other objects can *respond* to the event, sort of like an asynchronous [Observer](observer.html) pattern.

    - *You are likely to allow multiple listeners.* Since the queue contains things that already happened, the sender probably doesn't care who receives it. From its perspective, the event is in the past and is already forgotten.

    - *The scope of the queue tends to be broader.* Event queues are often used to *broadcast* events to any and all interested parties. To allow maximum flexibility for which parties can be interested, these queues tend to be more globally visible.

- **If you queue messages:**

    A "message" or "request" describes an action that we *want* to happen *in the future*, like "play sound". You can think of this as an asynchronous API to a service.

    Another word for "request" is "command", as in the [Command](command.html) pattern, and queues can be used there too.

    - *You are more likely to have a single listener.* In the example, the queued messages are requests specifically for *the audio API* to play a sound. If other random parts of the game engine started stealing messages off the queue, it wouldn't do much good.

    I say "more likely" here, because you can enqueue messages without caring which code processes it, as long as it gets processed *how* you expect. In that case, you're doing something akin to a [service locator](service-locator.html).

**CN:** 到目前为止，我互换使用了"事件"和"消息"，因为大多数情况下这并不重要。无论你在队列里塞什么，你都能获得相同的解耦和聚合能力，但存在一些概念上的差异。

- **如果你排队事件：**

    一个"事件"或"通知"描述了*已经*发生的事情，比如"怪物死亡"。你将其加入队列，以便其他对象可以*响应*这个事件，有点像异步的[观察者](observer.html)模式。

    - *你可能允许多个监听者。* 由于队列包含已经发生的事情，发送方可能不关心谁接收它。从它的角度来看，事件已经过去了，已经被遗忘了。

    - *队列的范围往往更广。* 事件队列通常用于向任何和所有感兴趣的方*广播*事件。为了允许感兴趣的方有最大的灵活性，这些队列往往更加全局可见。

- **如果你排队消息：**

    一个"消息"或"请求"描述了我们*想要**在未来*发生的动作，比如"播放声音"。你可以把这看作是一个服务的异步 API。

    "请求"的另一个词是"命令"，就像[命令](command.html)模式中那样，队列也可以在那里使用。

    - *你更有可能只有一个监听者。* 在示例中，排队的消息是专门为*音频 API* 播放声音的请求。如果游戏引擎的其他随机部分开始从队列中偷消息，那没什么好处。

    我在这里说"更有可能"，因为你可以排队消息而不关心哪个代码处理它，只要它按照你期望的方式被处理。在这种情况下，你正在做的事情类似于[服务定位器](service-locator.html)。

### Who Can Read from the Queue? / 谁可以从队列中读取？

* In our example, the queue is encapsulated and only the `Audio` class can read from it. In a user interface's event system, you can register listeners to your heart's content. You sometimes hear the terms "single-cast" and "broadcast" to distinguish these, and both styles are useful.

- **A single-cast queue:**

    This is the natural fit when a queue is part of a class's API. Like in our audio example, from the caller's perspective, they just see a `playSound()` method they can call.

    - *The queue becomes an implementation detail of the reader.* All the sender knows is that it sent a message.

    - *The queue is more encapsulated.* All other things being equal, more encapsulation is usually better.

    - *You don't have to worry about contention between listeners.* With multiple listeners, you have to decide if they *all* get every item (broadcast) or if *each* item in the queue is parceled out to *one* listener (something more like a work queue).

    In either case, the listeners may end up doing redundant work or interfering with each other, and you have to think carefully about the behavior you want. With a single listener, that complexity disappears.

- **A broadcast queue:**

    This is how most "event" systems work. If you have ten listeners when an event comes in, all ten of them see the event.

    - *Events can get dropped on the floor.* A corollary to the previous point is that if you have *zero* listeners, all zero of them see the event. In most broadcast systems, if there are no listeners at the point in time that an event is processed, the event gets discarded.

    - *You may need to filter events.* Broadcast queues are often widely visible to much of the program, and you can end up with a bunch of listeners. Multiply lots of events times lots of listeners, and you end up with a ton of event handlers to invoke.

    To cut that down to size, most broadcast event systems let a listener winnow down the set of events they receive. For example, they may say they only want to receive mouse events or events within a certain region of the UI.

- **A work queue:**

    Like a broadcast queue, here you have multiple listeners too. The difference is that each item in the queue only goes to *one* of them. This is a common pattern for parceling out jobs to a pool of concurrently running threads.

    - *You have to schedule.* Since an item only goes to one listener, the queue needs logic to figure out the best one to choose. This may be as simple as round robin or random choice, or it could be some more complex prioritizing system.

**CN:** 在我们的示例中，队列是封装的，只有 `Audio` 类可以从中读取。在用户界面的事件系统中，你可以随心所欲地注册监听器。你有时会听到"单播"和"广播"这两个术语来区分它们，两种风格都很有用。

- **单播队列：**

    当队列是类 API 的一部分时，这是很自然的方式。就像在我们的音频示例中，从调用者的角度来看，他们只看到了一个可以调用的 `playSound()` 方法。

    - *队列成为读取者的实现细节。* 发送方只知道它发送了一条消息。

    - *队列更加封装。* 在其他条件相同的情况下，更多的封装通常更好。

    - *你不必担心监听器之间的争用。* 使用多个监听器时，你必须决定是它们*全部*获得每个条目（广播），还是队列中的*每个*条目被分配给*一个*监听器（更像是工作队列）。

    在任何一种情况下，监听器最终可能做冗余的工作或相互干扰，你必须仔细考虑你想要的行为。使用单个监听器，这种复杂性就消失了。

- **广播队列：**

    这是大多数"事件"系统的工作方式。如果一个事件进来时有十个监听器，那么所有十个都能看到这个事件。

    - *事件可能被丢弃。* 前一点的一个推论是，如果你有*零个*监听器，那么所有零个都能看到这个事件。在大多数广播系统中，如果在事件被处理的时间点没有监听器，事件会被丢弃。

    - *你可能需要过滤事件。* 广播队列通常对程序的大部分可见，你最终可能会有一堆监听器。大量事件乘以大量监听器，你最终会得到一大堆需要调用的事件处理器。

    为了缩减规模，大多数广播事件系统允许监听器缩小他们接收的事件集。例如，他们可能说他们只想接收鼠标事件或 UI 中某个区域内的事件。

- **工作队列：**

    像广播队列一样，这里你也有多个监听器。不同之处在于，队列中的每个条目只给到*其中一个*。这是将工作分配给并发运行线程池的常见模式。

    - *你需要进行调度。* 由于一个条目只给一个监听器，队列需要逻辑来确定最佳的选择。这可能像轮询或随机选择一样简单，也可能是一些更复杂的优先级排序系统。

### Who Can Write to the Queue? / 谁可以写入队列？

* This is the flip side of the previous design choice. This pattern works with all of the possible read/write configurations: one-to-one, one-to-many, many-to-one, or many-to-many.

You sometimes hear "fan-in" used to describe many-to-one communication systems and "fan-out" for one-to-many.

- **With one writer:**

    This style is most similar to the synchronous [Observer](observer.html) pattern. You have one privileged object that generates events that others can then receive.

    - *You implicitly know where the event is coming from.* Since there's only one object that can add to the queue, any listener can safely assume that's the sender.

    - *You usually allow multiple readers.* You can have a one-sender-one-receiver queue, but that starts to feel less like the communication system this pattern is about and more like a vanilla queue data structure.

- **With multiple writers:**

    This is how our audio engine example works. Since `playSound()` is a public method, any part of the codebase can add a request to the queue. "Global" or "central" event buses work like this too.

    - *You have to be more careful of cycles.* Since anything can potentially put something onto the queue, it's easier to accidentally enqueue something in the middle of handling an event. If you aren't careful, that may trigger a feedback loop.

    - *You'll likely want some reference to the sender in the event itself.* When a listener gets an event, it doesn't know who sent it, since it could be anyone. If that's something they need to know, you'll want to pack that into the event object so that the listener can use it.

**CN:** 这是前面设计选择的另一面。这种模式适用于所有可能的读/写配置：一对一、一对多、多对一或多对多。

你有时会听到用"扇入"来描述多对一的通信系统，"扇出"来描述一对多。

- **一个写入者：**

    这种风格与同步的[观察者](observer.html)模式最为相似。你有一个特权对象生成事件，然后其他对象可以接收这些事件。

    - *你隐式地知道事件来自哪里。* 由于只有一个对象可以向队列添加内容，任何监听器都可以安全地假设那就是发送者。

    - *你通常允许多个读取者。* 你可以有一个发送者-一个接收者的队列，但这开始感觉不太像这种模式所涉及的通信系统，而更像是一个普通的队列数据结构。

- **多个写入者：**

    这是我们的音频引擎示例的工作方式。由于 `playSound()` 是一个公共方法，代码库的任何部分都可以向队列添加请求。"全局"或"中央"事件总线也是这样工作的。

    - *你必须更加小心循环。* 由于任何东西都可能将某些内容放入队列，在事件处理过程中意外入队某些东西变得更加容易。如果你不小心，那可能触发反馈循环。

    - *你可能希望在事件本身中包含对发送者的某种引用。* 当监听器获得一个事件时，它不知道是谁发送的，因为它可能是任何人。如果这是他们需要知道的信息，你需要将其打包到事件对象中，以便监听器可以使用它。

### What Is the Lifetime of the Objects in the Queue? / 队列中对象的生命周期是什么？

* With a synchronous notification, execution doesn't return to the sender until all of the receivers have finished processing the message. That means the message itself can safely live in a local variable on the stack. With a queue, the message outlives the call that enqueues it.

If you're using a garbage collected language, you don't need to worry about this too much. Stuff the message in the queue, and it will stick around in memory as long as it's needed. In C or C++, it's up to you to ensure the object lives long enough.

- **Pass ownership:**

    This is the traditional way to do things when managing memory manually. When a message gets queued, the queue claims it and the sender no longer owns it. When it gets processed, the receiver takes ownership and is responsible for deallocating it.

    In C++, `unique_ptr<T>` gives you these exact semantics out of the box.

- **Share ownership:**

    These days, now that even C++ programmers are more comfortable with garbage collection, shared ownership is more acceptable. With this, the message sticks around as long as anything has a reference to it and is automatically freed when forgotten.

    Likewise, the C++ type for this is `shared_ptr<T>`.

- **The queue owns it:**

    Another option is to have messages *always* live on the queue. Instead of allocating the message itself, the sender requests a "fresh" one from the queue. The queue returns a reference to a message already in memory inside the queue, and the sender fills it in. When the message gets processed, the receiver refers to the same message in the queue.

    In other words, the backing store for the queue is an [object pool](object-pool.html).

**CN:** 使用同步通知，执行不会返回给发送者，直到所有接收者都完成了消息的处理。这意味着消息本身可以安全地存在于栈上的局部变量中。而使用队列，消息的寿命超过了将其入队的调用。

如果你使用垃圾回收语言，你不需要太担心这个问题。把消息塞进队列，它会在内存中保留所需的时间。在 C 或 C++ 中，你需要自己确保对象的生命周期足够长。

- **传递所有权：**

    这是在手动管理内存时的传统做法。当消息被排队时，队列声称拥有它，发送者不再拥有它。当它被处理时，接收者取得所有权并负责释放它。

    在 C++ 中，`unique_ptr<T>` 开箱即用地提供了这些确切的语义。

- **共享所有权：**

    如今，既然连 C++ 程序员都对垃圾回收更加习惯了，共享所有权也变得更容易接受。这样，只要有任何东西引用它，消息就会存在，并在被遗忘时自动释放。

    同样，C++ 中对应的类型是 `shared_ptr<T>`。

- **队列拥有它：**

    另一个选择是让消息*始终*存在于队列中。发送者不分配消息本身，而是向队列请求一个"新鲜的"消息。队列返回一个指向队列内部已存在内存中的消息的引用，发送者填充它。当消息被处理时，接收者引用队列中的同一个消息。

    换句话说，队列的后备存储是一个[对象池](object-pool.html)。

---

## See Also / 参见

- I've mentioned this a few times already, but in many ways, this pattern is the asynchronous cousin to the well-known [Observer](observer.html) pattern.

- 我已经提到过几次了，但在许多方面，这种模式是著名的[观察者](observer.html)模式的异步表亲。

- Like many patterns, event queues go by a number of aliases. One established term is "message queue". It's usually referring to a higher-level manifestation. Where our event queues are *within* an application, message queues are usually used for communicating *between* them.

- 像许多模式一样，事件队列有许多别名。一个既定的术语是"消息队列"。它通常指的是更高级的表现形式。我们的事件队列在应用*内部*，而消息队列通常用于在应用*之间*进行通信。

Another term is "publish/subscribe", sometimes abbreviated to "pubsub". Like "message queue", it usually refers to larger distributed systems unlike the humble coding pattern we're focused on.

另一个术语是"发布/订阅"，有时缩写为"pubsub"。和"消息队列"一样，它通常指的是更大的分布式系统，而不是我们关注的这种简单的编码模式。

- A [finite state machine](http://en.wikipedia.org/wiki/Finite-state_machine), similar to the Gang of Four's [State](state.html) pattern, requires a stream of inputs. If you want it to respond to those asynchronously, it makes sense to queue them.

- [有限状态机](http://en.wikipedia.org/wiki/Finite-state_machine)，类似于四人帮的[状态](state.html)模式，需要输入流。如果你希望它异步地响应这些输入，将它们排队是合理的。

When you have a bunch of state machines sending messages to each other, each with a little queue of pending inputs (called a *mailbox*), then you've re-invented the [actor model](http://en.wikipedia.org/wiki/Actor_model) of computation.

当你有一堆状态机相互发送消息，每个状态机都有一个小的待处理输入队列（称为*邮箱*），那么你就重新发明了计算的[参与者模型](http://en.wikipedia.org/wiki/Actor_model)。

- The [Go](http://golang.org/) programming language's built-in "channel" type is essentially an event or message queue.

- [Go](http://golang.org/) 编程语言内置的"channel"类型本质上就是一个事件或消息队列。
---

## C# Equivalent (C# 对照实现)

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

// ──────────────────────────────────────────────
// 事件队列的 C# 实现 —— 线程安全的环形缓冲区
// ──────────────────────────────────────────────

// 声音请求消息
public struct PlayMessage
{
    public int SoundId;    // 声音 ID
    public int Volume;     // 音量
}

// 基于环形缓冲区的音频事件队列（线程安全版本）
public class AudioQueue
{
    // ── 队列配置 ──
    private const int MaxPending = 64;               // 最大待处理请求数
    private readonly PlayMessage[] _pending = new PlayMessage[MaxPending];
    private int _head;                               // 读取指针（出队位置）
    private int _tail;                               // 写入指针（入队位置）

    // ── 线程同步 ──
    private readonly object _lock = new();            // 锁对象，确保线程安全
    private readonly AutoResetEvent _signal = new(false); // 信号量，通知处理线程

    // ── 入队（可由任意线程调用）──
    public void PlaySound(int soundId, int volume)
    {
        lock (_lock)  // 加锁保证线程安全
        {
            // 检查队列是否已满（环形缓冲区中 tail+1 == head 表示满）
            int nextTail = (_tail + 1) % MaxPending;
            if (nextTail == _head)
            {
                // 队列满了，可选择丢弃、覆盖或阻塞
                UnityEngine.Debug.LogWarning("音频队列已满，丢弃请求");
                return;
            }

            // 聚合优化：如果队列中已有相同声音，取较大音量
            for (int i = _head; i != _tail; i = (i + 1) % MaxPending)
            {
                if (_pending[i].SoundId == soundId)
                {
                    _pending[i].Volume = Math.Max(volume, _pending[i].Volume);
                    return;  // 合并成功，不入队
                }
            }

            // 正常入队
            _pending[_tail] = new PlayMessage
            {
                SoundId = soundId,
                Volume = volume
            };
            _tail = nextTail;

            // 通知处理线程有新消息
            _signal.Set();
        }
    }

    // ── 出队（由音频线程调用）──
    public bool TryDequeue(out PlayMessage message)
    {
        lock (_lock)
        {
            if (_head == _tail)  // 队列为空
            {
                message = default;
                return false;
            }

            // 取出头部的消息
            message = _pending[_head];
            _head = (_head + 1) % MaxPending;
            return true;
        }
    }

    // ── 带等待的出队（阻塞直到有消息）──
    public PlayMessage DequeueBlocking()
    {
        while (true)
        {
            if (TryDequeue(out PlayMessage message))
                return message;

            // 队列为空，等待信号
            _signal.WaitOne();
        }
    }

    // ── 处理循环（在独立的音频线程中运行）──
    public void ProcessLoop()
    {
        while (true)
        {
            PlayMessage msg = DequeueBlocking();

            // 实际处理：加载资源、播放声音
            // LoadSound(msg.SoundId);
            // StartSound(msg.SoundId, msg.Volume);
        }
    }

    // ── 当前队列长度 ──
    public int Count
    {
        get
        {
            lock (_lock)
            {
                if (_tail >= _head)
                    return _tail - _head;
                return MaxPending - _head + _tail;
            }
        }
    }
}

// ──────────────────────────────────────────────
// 使用泛型的通用事件队列
// ──────────────────────────────────────────────
public class EventQueue<T>
{
    private readonly Queue<T> _queue = new();
    private readonly object _lock = new();

    public void Enqueue(T eventData)
    {
        lock (_lock)
        {
            _queue.Enqueue(eventData);
        }
    }

    public bool TryDequeue(out T eventData)
    {
        lock (_lock)
        {
            if (_queue.Count > 0)
            {
                eventData = _queue.Dequeue();
                return true;
            }
            eventData = default;
            return false;
        }
    }

    public int Count
    {
        get { lock (_lock) { return _queue.Count; } }
    }
}

// ──────────────────────────────────────────────
// Unity 中的简化实现 —— 使用 ConcurrentQueue
// ──────────────────────────────────────────────
using System.Collections.Concurrent;  // 命名空间：线程安全集合

public class UnityEventQueue : MonoBehaviour
{
    // C# 内置的无锁线程安全队列（比我们自己加锁更高效）
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();

    private void Update()
    {
        // 在主线程处理所有排队的操作
        while (_mainThreadActions.TryDequeue(out Action action))
        {
            action?.Invoke();  // 执行排队的操作
        }
    }

    // 其他线程可以安全调用的方法
    public void RunOnMainThread(Action action)
    {
        _mainThreadActions.Enqueue(action);
    }
}
```

## Unity Application / Unity 应用场景

### 1. 主线程与工作线程通信
Unity 的所有 API（尤其是 `UnityEngine` 命名空间下的）只能在主线程调用。Event Queue 是跨线程通信的标准方案：

```csharp
public class ThreadSafeLogger : MonoBehaviour
{
    private ConcurrentQueue<string> _logQueue = new();

    private void Update()
    {
        while (_logQueue.TryDequeue(out string msg))
        {
            Debug.Log(msg);  // 在主线程调用 Debug.Log
        }
    }

    // 可在任何线程调用
    public void LogFromAnyThread(string message)
    {
        _logQueue.Enqueue(message);
    }
}
```

### 2. 音频管理系统
游戏中有大量"一触即发"的声音需求。直接调用 `AudioSource.Play()` 可能导致：
- 同一声音叠加播放，音量异常
- 超过硬件通道限制，声音被丢弃
- 从非主线程调用导致崩溃

使用 Event Queue 可以聚合、优先级排序、延迟处理所有音频请求。

### 3. UI 事件系统
Unity 的 `EventSystem` 本质上就是一个事件队列。鼠标点击、触摸、键盘输入都先进入队列，然后在 `Update` 中按序分发。

### 4. 网络消息处理
网络数据包到达后不应立即处理（可能不在主线程），应先入队，在主线程的 `Update` 中逐一出队处理：

```csharp
public class NetworkManager : MonoBehaviour
{
    private ConcurrentQueue<NetworkPacket> _incomingPackets = new();

    private void Update()
    {
        while (_incomingPackets.TryDequeue(out NetworkPacket packet))
        {
            HandlePacket(packet);  // 在主线程安全处理
        }
    }

    // 由网络线程调用
    public void OnPacketReceived(NetworkPacket packet)
    {
        _incomingPackets.Enqueue(packet);
    }
}
```

### 5. 解耦游戏系统（中央事件总线）
```csharp
// 使用 EventQueue 作为中央事件总线
// 战斗系统、成就系统、教程系统各自独立，通过事件队列通信
public class CombatSystem
{
    private EventQueue<string> _eventBus;

    public void OnEnemyKilled()
    {
        _eventBus.Enqueue("EnemyKilled");
        // 不直接调用成就系统或教程系统
    }
}

public class AchievementSystem
{
    private EventQueue<string> _eventBus;

    public void Check()
    {
        while (_eventBus.TryDequeue(out string eventName))
        {
            if (eventName == "EnemyKilled")
                CheckKillAchievements();
        }
    }
}
```

## Key Differences / 关键差异

| Aspect | C++ (原书) | C# / Unity |
|--------|-----------|------------|
| **队列实现** | 手动实现环形缓冲区 | `Queue<T>`、`ConcurrentQueue<T>` 或自定义环形缓冲区 |
| **线程安全** | 需要手动加锁（mutex） | `ConcurrentQueue<T>` 内置无锁实现；或 `lock` 语句 |
| **内存分配** | 静态数组（无动态分配） | `Queue<T>` 内部动态扩容；可用 `ConcurrentQueue` |
| **跨线程通信** | 需要额外线程模型 | Unity 主线程限制 → Event Queue 是标准方案 |
| **聚合请求** | 手动遍历队列 | LINQ 查询或 `Dictionary<SoundId, int>` 优化 |
| **泛型支持** | 模板（template） | 泛型 `EventQueue<T>` 开箱即用 |
| **阻塞等待** | 条件变量（condition_variable） | `AutoResetEvent` / `ManualResetEvent` 或 `Monitor.Wait` |
| **框架支持** | 无 | Unity 的 `EventSystem`、`UnityEvent`、`SendMessage` |

### Unity 中的事件/消息机制对比

| 机制 | 解耦类型 | 线程安全 | 性能 | 推荐场景 |
|------|---------|---------|------|---------|
| **Event Queue** | 静态 + 时间 | ✅ | ⭐⭐⭐ | 跨线程通信、系统间解耦 |
| **UnityEvent** | 静态 | ❌ | ⭐⭐ | Inspector 绑定、UI 事件 |
| **C# event/delegate** | 静态 | ❌ | ⭐⭐⭐ | 同一线程内解耦 |
| **SendMessage** | 动态 | ❌ | ⭐ | 快速原型（不推荐生产） |
| **Interface** | 静态 | ✅ | ⭐⭐⭐⭐ | 强类型组件间通信 |
| **ScriptableObject 事件** | 静态 + 资源化 | ❌ | ⭐⭐ | 解耦预制体间通信 |

---

# Service Locator — 服务定位器

> **EN:** Provide a global point of access to a service without coupling users to the concrete class that implements it.
> **CN:** 提供对服务的全局访问点，同时避免使用者与服务的具体实现类耦合。

## Intent / 意图

**EN (from *Game Programming Patterns*):** Provide a global point of access to a service without coupling users to the concrete class that implements it.

**CN:** 提供对服务的全局访问点，同时避免使用者与服务的具体实现类耦合。

---

## Motivation / 动机

**EN (from *Game Programming Patterns*):**

Some objects or systems in a game tend to get around, visiting almost every corner of the codebase. It's hard to find a part of the game that *won't* need a memory allocator, logging, or random numbers at some point. Systems like those can be thought of as *services* that need to be available to the entire game.

For our example, we'll consider audio. It doesn't have quite the reach of something lower-level like a memory allocator, but it still touches a bunch of game systems. A falling rock hits the ground with a crash (physics). A sniper NPC fires his rifle and a shot rings out (AI). The user selects a menu item with a beep of confirmation (user interface).

Each of these places will need to be able to call into the audio system with something like one of these:

```cpp
// Use a static class?
AudioSystem::playSound(VERY_LOUD_BANG);

// Or maybe a singleton?
AudioSystem::instance()->playSound(VERY_LOUD_BANG);
```

Either gets us where we're trying to go, but we stumbled into some sticky coupling along the way. Every place in the game calling into our audio system directly references the concrete `AudioSystem` class and the mechanism for accessing it — either as a static class or a [singleton](singleton.html).

These call sites, of course, have to be coupled to *something* in order to make a sound play, but letting them poke at the concrete audio implementation directly is like giving a hundred strangers directions to your house just so they can drop a letter on your doorstep. Not only is it a little bit *too* personal, it's a real pain when you move and you have to tell each person the new directions.

There's a better solution: a phone book. People that need to get in touch with us can look us up by name and get our current address. When we move, we tell the phone company. They update the book, and everyone gets the new address. In fact, we don't even need to give out our real address at all. We can list a P.O. box or some other "representation" of ourselves instead. By having callers go through the book to find us, we have *a convenient single place where we control how we're found.*

This is the Service Locator pattern in a nutshell — it decouples code that needs a service from both *who* it is (the concrete implementation type) and *where* it is (how we get to the instance of it).

**CN:** 游戏中的某些对象或系统往往会到处"串门"，几乎遍及代码库的每个角落。很难找到游戏中有哪个部分在某时某刻*不*需要内存分配器、日志或随机数。这样的系统可以被视为*服务*，需要对整个游戏可用。

以音频为例。它虽然没有像内存分配器那样底层的东西触及面那么广，但仍然会触及大量游戏系统。坠落的岩石撞击地面发出碎裂声（物理系统），狙击手NPC开火时枪声响起（AI系统），用户选择菜单项时发出确认的哔哔声（用户界面）。

这些地方都需要通过类似的方式调用音频系统。但这样我们就遇到了棘手的耦合问题——游戏中每个调用音频系统的地方都直接引用了具体的 `AudioSystem` 类以及访问它的机制（静态类或单例）。

更好的解决方案是：电话簿。需要联系我们的人可以通过姓名查到我们当前的地址。我们搬家时告诉电话公司，他们更新电话簿，每个人都能得到新地址。实际上，我们甚至不需要给出真实地址——可以列出邮箱或其他某种"代表"我们的东西。让调用者通过电话簿找到我们，我们就有了*一个控制如何被找到的便捷单一位置*。

这就是服务定位器模式的精髓——它将需要服务的代码与服务的*身份*（具体实现类型）和*位置*（如何获取实例）解耦。

---

## The Pattern / 模式定义

**EN (from *Game Programming Patterns*):**

A **service** class defines an abstract interface to a set of operations. A concrete **service provider** implements this interface. A separate **service locator** provides access to the service by finding an appropriate provider while hiding both the provider's concrete type and the process used to locate it.

**CN:** **服务**类定义了一组操作的抽象接口。具体的**服务提供者**实现此接口。独立的**服务定位器**通过查找适当的提供者来提供对服务的访问，同时隐藏提供者的具体类型和定位过程。

---

## When to Use It / 何时使用

**EN (from *Game Programming Patterns*):**

Anytime you make something accessible to every part of your program, you're asking for trouble. That's the main problem with the [Singleton](singleton.html) pattern, and this pattern is no different. My simplest advice for when to use a service locator is: *sparingly*.

Instead of using a global mechanism to give some code access to an object it needs, first consider *passing the object to it instead*. That's dead simple, and it makes the coupling completely obvious. That will cover most of your needs.

*But…* there are some times when manually passing around an object is gratuitous or actively makes code harder to read. Some systems, like logging or memory management, shouldn't be part of a module's public API. The parameters to your rendering code should have to do with *rendering*, not stuff like logging.

Likewise, other systems represent facilities that are fundamentally singular in nature. Your game probably only has one audio device or display system that it can talk to. It is an ambient property of the environment, so plumbing it through ten layers of methods just so one deeply nested call can get to it is adding needless complexity to your code.

In those kinds of cases, this pattern can help. As we'll see, it functions as a more flexible, more configurable cousin of the Singleton pattern. When used well, it can make your codebase more flexible with little runtime cost.

Conversely, when used poorly, it carries with it all of the baggage of the Singleton pattern with worse runtime performance.

**CN:** 任何时候你让某些东西对程序的每个部分都可访问，你就是在自找麻烦。这是[单例](singleton.html)模式的主要问题，这个模式也不例外。关于何时使用服务定位器，我最简单的建议是：*谨慎使用*。

与其使用全局机制让某些代码访问它所需的对象，首先考虑*将对象传递给它*。这非常简单，并且使耦合完全明显。这将满足你大部分需求。

但是……有时候手动传递对象是多余的，或者反而让代码更难阅读。某些系统（如日志或内存管理）不应该是模块公共API的一部分。渲染代码的参数应该与*渲染*有关，而不是日志之类的事情。

同样，其他系统本质上就是单一的工具。你的游戏可能只有一个音频设备或显示系统可以与之通信。这是环境的固有属性，所以通过十层方法传递它，只为了让一个深度嵌套的调用能够访问它，是给代码增加了不必要的复杂性。

在这些情况下，这个模式可以提供帮助。正如我们将看到的，它是单例模式的一个更灵活、更可配置的表亲。使用得当，它可以以很小的运行时成本使你的代码库更加灵活。

相反，使用不当，它带有单例模式的所有包袱，而且运行时性能更差。

---

## Keep in Mind / 注意事项

**EN (from *Game Programming Patterns*):**

The core difficulty with a service locator is that it takes a dependency — a bit of coupling between two pieces of code — and defers wiring it up until runtime. This gives you flexibility, but the price you pay is that it's harder to understand what your dependencies are by reading the code.

### The service actually has to be located

With a singleton or a static class, there's no chance for the instance we need to *not* be available. Calling code can take for granted that it's there. But since this pattern has to *locate* the service, we may need to handle cases where that fails. Fortunately, we'll cover a strategy later to address this and guarantee that we'll always get *some* service when you need it.

### The service doesn't know who is locating it

Since the locator is globally accessible, any code in the game could be requesting a service and then poking at it. This means that the service must be able to work correctly in any circumstance. For example, a class that expects to be used only during the simulation portion of the game loop and not during rendering may not work as a service — it wouldn't be able to ensure that it's being used at the right time. So, if a class expects to be used only in a certain context, it's safest to avoid exposing it to the entire world with this pattern.

**CN:** 服务定位器的核心困难在于，它接受一个依赖（两段代码之间的一点耦合），并将其推迟到运行时才连接。这给了你灵活性，但代价是通过阅读代码更难理解你的依赖是什么。

### 服务实际上必须能被定位到

使用单例或静态类时，我们需要的实例不可能*不*可用。调用代码可以理所当然地认为它就在那里。但由于此模式必须*定位*服务，我们可能需要处理定位失败的情况。幸运的是，我们稍后会介绍一种策略来解决这个问题，并保证在你需要时始终能得到*某个*服务。

### 服务不知道谁在定位它

由于定位器是全局可访问的，游戏中的任何代码都可能请求服务并操作它。这意味着服务必须在任何情况下都能正确工作。例如，一个期望仅在游戏循环的模拟部分使用而非渲染期间使用的类，可能不适合作为服务——它无法确保在正确的时间被使用。因此，如果一个类期望仅在特定上下文中使用，最安全的是避免通过此模式将其暴露给整个世界。

---

## Sample Code / 示例代码

**EN (from *Game Programming Patterns*):**

Getting back to our audio system problem, let's address it by exposing the system to the rest of the codebase through a service locator.

### The service

We'll start off with the audio API. This is the interface that our service will be exposing:

**CN:** 回到我们的音频系统问题，让我们通过服务定位器将系统暴露给代码库的其他部分。

### 服务

我们从音频API开始。这是我们的服务将要暴露的接口：

```cpp
class Audio {
public:
  virtual ~Audio() {}
  virtual void playSound(int soundID) = 0;
  virtual void stopSound(int soundID) = 0;
  virtual void stopAllSounds() = 0;
};
```

* A real audio engine would be much more complex than this, of course, but this shows the basic idea. What's important is that it's an abstract interface class with no implementation bound to it.

* 当然，真正的音频引擎会比这复杂得多，但这展示了基本思想。重要的是它是一个没有绑定任何实现的抽象接口类。

### The service provider

* By itself, our audio interface isn't very useful. We need a concrete implementation. This book isn't about how to write audio code for a game console, so you'll have to imagine there's some actual code in the bodies of these functions, but you get the idea:

* 音频接口本身并不是很有用。我们需要一个具体的实现。这本书不是关于如何为游戏主机编写音频代码，所以你需要想象这些函数体中存在一些实际代码，但你明白意思：

```cpp
class ConsoleAudio : public Audio {
public:
  virtual void playSound(int soundID)
  {
    // Play sound using console audio api...
  }

  virtual void stopSound(int soundID)
  {
    // Stop sound using console audio api...
  }

  virtual void stopAllSounds()
  {
    // Stop all sounds using console audio api...
  }
};
```

* Now we have an interface and an implementation. The remaining piece is the service locator — the class that ties the two together.

* 现在我们有了接口和实现。剩下的部分是服务定位器——将两者联系在一起的类。

### A simple locator

* The implementation here is about the simplest kind of service locator you can define:

* 这里的实现是你能定义的最简单的服务定位器：

```cpp
class Locator {
public:
  static Audio* getAudio() { return service_; }

  static void provide(Audio* service)
  {
    service_ = service;
  }

private:
  static Audio* service_;
};
```

* The technique this uses is called *dependency injection*, an awkward bit of jargon for a very simple idea. Say you have one class that depends on another. In our case, our `Locator` class needs an instance of the `Audio` service. Normally, the locator would be responsible for constructing that instance itself. Dependency injection instead says that outside code is responsible for *injecting* that dependency into the object that needs it.

The static `getAudio()` function does the locating. We can call it from anywhere in the codebase, and it will give us back an instance of our `Audio` service to use:

**CN:** 这里使用的技术称为*依赖注入*，这是一个用于非常简单概念的生硬术语。假设你有一个类依赖于另一个类。在我们的案例中，`Locator` 类需要一个 `Audio` 服务的实例。通常，定位器会负责自己构造该实例。而依赖注入则相反，它说外部代码负责将依赖*注入*到需要它的对象中。

静态的 `getAudio()` 函数执行定位。我们可以从代码库的任何位置调用它，它会返回一个 `Audio` 服务的实例供我们使用：

```cpp
Audio *audio = Locator::getAudio();
audio->playSound(VERY_LOUD_BANG);
```

* The way it "locates" is very simple — it relies on some outside code to register a service provider before anything tries to use the service. When the game is starting up, it calls some code like this:

* 它"定位"的方式非常简单——它依赖于某些外部代码在任何东西尝试使用服务之前注册服务提供者。在游戏启动时，它调用这样的代码：

```cpp
ConsoleAudio *audio = new ConsoleAudio();
Locator::provide(audio);
```

* The key part to notice here is that the code that calls `playSound()` isn't aware of the concrete `ConsoleAudio` class; it only knows the abstract `Audio` interface. Equally important, not even the *locator* class is coupled to the concrete service provider. The *only* place in code that knows about the actual concrete class is the initialization code that provides the service.

There's one more level of decoupling here: the `Audio` interface isn't aware of the fact that it's being accessed in most places through a service locator. As far as it knows, it's just a regular abstract base class. This is useful because it means we can apply this pattern to *existing* classes that weren't necessarily designed around it. This is in contrast with [Singleton](singleton.html), which affects the design of the "service" class itself.

**CN:** 这里需要注意的关键点是，调用 `playSound()` 的代码并不知道具体的 `ConsoleAudio` 类；它只知道抽象的 `Audio` 接口。同样重要的是，即使是*定位器*类也不与具体的服务提供者耦合。代码中*唯一*知道实际具体类的地方是提供服务的初始化代码。

这里还有一层解耦：`Audio` 接口并不知道它大多数时候是通过服务定位器被访问的。就它而言，它只是一个普通的抽象基类。这很有用，因为这意味着我们可以将此模式应用于*已有的*、不一定围绕它设计的类。这与[单例](singleton.html)模式形成对比，后者会影响"服务"类本身的设计。

### A null service

* Our implementation so far is certainly simple, and it's pretty flexible too. But it has one big shortcoming: if we try to use the service before a provider has been registered, it returns `NULL`. If the calling code doesn't check that, we're going to crash the game.

I sometimes hear this called "temporal coupling" — two separate pieces of code that must be called in the right order for the program to work correctly. All stateful software has some degree of this, but as with other kinds of coupling, reducing temporal coupling makes the codebase easier to manage.

Fortunately, there's another design pattern called "Null Object" that we can use to address this. The basic idea is that in places where we would return `NULL` when we fail to find or create an object, we instead return a special object that implements the same interface as the desired object. Its implementation basically does nothing, but it allows code that receives the object to safely continue on as if it had received a "real" one.

To use this, we'll define another "null" service provider:

**CN:** 我们目前的实现当然很简单，也很灵活。但它有一个大缺点：如果在注册提供者之前尝试使用服务，它会返回 `NULL`。如果调用代码没有检查这一点，我们的游戏就会崩溃。

我有时听到这被称为"时间耦合"——两段独立的代码必须以正确的顺序调用，程序才能正确工作。所有有状态的软件都有一定程度的这种耦合，但与其他类型的耦合一样，减少时间耦合使代码库更易于管理。

幸运的是，我们可以使用另一种称为"空对象"的设计模式来解决这个问题。基本思想是，在我们找不到或无法创建对象而返回 `NULL` 的地方，我们改为返回一个实现相同接口的特殊对象。它的实现基本上什么都不做，但它允许接收该对象的代码安全地继续执行，就像收到了一个"真正的"对象一样。

为此，我们将定义另一个"空"服务提供者：

```cpp
class NullAudio: public Audio {
public:
  virtual void playSound(int soundID) { /* Do nothing. */ }
  virtual void stopSound(int soundID) { /* Do nothing. */ }
  virtual void stopAllSounds()        { /* Do nothing. */ }
};
```

* As you can see, it implements the service interface, but doesn't actually do anything. Now, we change our locator to this:

* 如你所见，它实现了服务接口，但实际上什么也不做。现在，我们将定位器改为这样：

```cpp
class Locator {
public:
  static void initialize() { service_ = &nullService_; }

  static Audio& getAudio() { return *service_; }

  static void provide(Audio* service)
  {
    if (service == NULL)
    {
      // Revert to null service.
      service_ = &nullService_;
    }
    else
    {
      service_ = service;
    }
  }

private:
  static Audio* service_;
  static NullAudio nullService_;
};
```

* You may notice we're returning the service by reference instead of by pointer now. Since references in C++ are (in theory!) never `NULL`, returning a reference is a hint to users of the code that they can expect to always get a valid object back.

The other thing to notice is that we're checking for `NULL` in the `provide()` function instead of checking for the accessor. That requires us to call `initialize()` early on to make sure that the locator initially correctly defaults to the null provider. In return, it moves the branch out of `getAudio()`, which will save us a couple of cycles every time the service is accessed.

Calling code will never know that a "real" service wasn't found, nor does it have to worry about handling `NULL`. It's guaranteed to always get back a valid object.

This is also useful for *intentionally* failing to find services. If we want to disable a system temporarily, we now have an easy way to do so: simply don't register a provider for the service, and the locator will default to a null provider.

Turning off audio is handy during development. It frees up some memory and CPU cycles. More importantly, when you break into a debugger just as a loud sound starts playing, it saves you from having your eardrums shredded. There's nothing like twenty milliseconds of a scream sound effect looping at full volume to get your blood flowing in the morning.

**CN:** 你可能注意到我们现在通过引用而不是指针返回服务。由于C++中的引用（理论上！）永远不会是 `NULL`，返回引用向代码的用户暗示他们可以期望总是收到一个有效的对象。

另一件需要注意的事情是，我们在 `provide()` 函数中检查 `NULL`，而不是在访问器中检查。这要求我们尽早调用 `initialize()`，以确保定位器最初能正确默认使用空提供者。作为回报，它将分支移出了 `getAudio()`，这将在每次访问服务时节省几个周期。

调用代码永远不会知道没有找到"真正的"服务，也不需要担心处理 `NULL`。它保证始终返回一个有效的对象。

这对于*故意*找不到服务也很有用。如果我们想临时禁用一个系统，我们现在有了一种简单的方法：只需不为该服务注册提供者，定位器将默认使用空提供者。

在开发期间关闭音频很方便。它可以释放一些内存和CPU周期。更重要的是，当你在一个响亮的声音开始播放时进入调试器，它可以保护你的耳膜不被震碎。没有什么比全音量循环播放的尖叫声效20毫秒更能让你在早晨热血沸腾了。

### Logging decorator

* Now that our system is pretty robust, let's discuss another refinement this pattern lets us do — decorated services. I'll explain with an example.

During development, a little logging when interesting events occur can help you figure out what's going on under the hood of your game engine. If you're working on AI, you'd like to know when an entity changes AI states. If you're the sound programmer, you may want a record of every sound as it plays so you can check that they trigger in the right order.

The typical solution is to litter the code with calls to some `log()` function. Unfortunately, that replaces one problem with another — now we have *too much* logging. The AI coder doesn't care when sounds are playing, and the sound person doesn't care about AI state transitions, but now they both have to wade through each other's messages.

Ideally, we would be able to selectively enable logging for just the stuff we care about, and in the final game build, there'd be no logging at all. If the different systems we want to conditionally log are exposed as services, then we can solve this using the [Decorator](http://www.c2.com/cgi/wiki?DecoratorPattern) pattern. Let's define another audio service provider implementation like this:

**CN:** 现在我们的系统已经很健壮了，让我们讨论一下这个模式允许我们做的另一个改进——装饰服务。我将用一个例子来解释。

在开发期间，当有趣的事件发生时做一点日志记录可以帮助你了解游戏引擎的内部运作。如果你在开发AI，你可能想知道一个实体何时改变AI状态。如果你是音效程序员，你可能想要记录每播放一个声音，以便检查它们是否按正确顺序触发。

典型的解决方案是用对某个 `log()` 函数的调用充斥代码。不幸的是，这用一个问题替换了另一个问题——现在我们有*太多*日志了。AI程序员不关心声音何时播放，音效人员也不关心AI状态转换，但现在他们都必须翻阅对方的消息。

理想情况下，我们应该能够有选择地仅为我们关心的内容启用日志，而在最终的游戏构建中，完全没有日志。如果我们想有条件地记录日志的不同系统被暴露为服务，那么我们可以使用[装饰器](http://www.c2.com/cgi/wiki?DecoratorPattern)模式来解决这个问题。让我们定义另一个音频服务提供者实现：

```cpp
class LoggedAudio : public Audio {
public:
  LoggedAudio(Audio &wrapped)
  : wrapped_(wrapped)
  {}

  virtual void playSound(int soundID)
  {
    log("play sound");
    wrapped_.playSound(soundID);
  }

  virtual void stopSound(int soundID)
  {
    log("stop sound");
    wrapped_.stopSound(soundID);
  }

  virtual void stopAllSounds()
  {
    log("stop all sounds");
    wrapped_.stopAllSounds();
  }

private:
  void log(const char* message)
  {
    // Code to log message...
  }

  Audio &wrapped_;
};
```

* As you can see, it wraps another audio provider and exposes the same interface. It forwards the actual audio behavior to the inner provider, but it also logs each sound call. If a programmer wants to enable audio logging, they call this:

* 如你所见，它包装了另一个音频提供者并暴露了相同的接口。它将实际的音频行为转发给内部的提供者，但同时也记录每次声音调用。如果程序员想要启用音频日志，他们调用：

```cpp
void enableAudioLogging() {
  // Decorate the existing service.
  Audio *service = new LoggedAudio(Locator::getAudio());

  // Swap it in.
  Locator::provide(service);
}
```

* Now, any calls to the audio service will be logged before continuing as before. And, of course, this plays nicely with our null service, so you can both *disable* audio and yet still log the sounds that it *would* play if sound were enabled.

* 现在，对音频服务的任何调用都将在继续之前被记录。当然，这与我们的空服务配合得很好，因此你可以同时*禁用*音频，但仍然记录如果声音启用时它*将*播放的声音。

---

## Design Decisions / 设计决策

**EN (from *Game Programming Patterns*):**

We've covered a typical implementation, but there are a couple of ways that it can vary based on differing answers to a few core questions:

**CN:** 我们已经介绍了一个典型的实现，但根据几个核心问题的不同答案，它可能有几种变化方式：

### How is the service located? / 服务如何被定位？

- **Outside code registers it:**

- **外部代码注册它：**

This is the mechanism our sample code uses to locate the service, and it's the most common design I see in games:

这是我们的示例代码定位服务所使用的机制，也是我在游戏中看到的最常见设计：

- *It's fast and simple.* The `getAudio()` function simply returns a pointer. It will often get inlined by the compiler, so we get a nice abstraction layer at almost no performance cost.

- *快速而简单。* `getAudio()` 函数简单地返回一个指针。编译器通常会内联它，因此我们以几乎零性能成本获得了一个良好的抽象层。

- *We control how the provider is constructed.* Consider a service for accessing the game's controllers. We have two concrete providers: one for regular games and one for playing online. The online provider passes controller input over the network so that, to the rest of the game, remote players appear to be using local controllers.

- *我们控制提供者的构造方式。* 考虑一个用于访问游戏控制器的服务。我们有两个具体的提供者：一个用于常规游戏，一个用于在线游戏。在线的提供者通过网络传递控制器输入，因此对于游戏的其他部分来说，远程玩家看起来像是在使用本地控制器。

To make this work, the online concrete provider needs to know the IP address of the other remote player. If the locator itself was constructing the object, how would it know what to pass in? The `Locator` class doesn't know anything about online at all, much less some other user's IP address.

要做到这一点，在线的具体提供者需要知道其他远程玩家的IP地址。如果定位器自己构造对象，它如何知道要传入什么？`Locator` 类对在线一无所知，更不用说其他用户的IP地址了。

Externally registered providers dodge the problem. Instead of the locator constructing the class, the game's networking code instantiates the online-specific service provider, passing in the IP address it needs. Then it gives that to the locator, who knows only about the service's abstract interface.

外部注册的提供者避开了这个问题。游戏的网络代码实例化特定于在线的服务提供者，传入它需要的IP地址，而不是由定位器构造类。然后它将提供者交给定位器，而定位器只知道服务的抽象接口。

- *We can change the service while the game is running.* We may not use this in the final game, but it's a neat trick during development. While testing, we can swap out, for example, the audio service with the null service we talked about earlier to temporarily disable sound while the game is still running.

- *我们可以在游戏运行时更改服务。* 我们可能在最终游戏中不会使用这个，但在开发期间这是一个巧妙的小技巧。在测试时，我们可以交换出——例如——将音频服务与我们之前讨论的空服务交换，以在游戏仍在运行时临时禁用声音。

- *The locator depends on outside code.* This is the downside. Any code accessing the service presumes that some code somewhere has already registered it. If that initialization doesn't happen, we'll either crash or have a service mysteriously not working.

- *定位器依赖外部代码。* 这是缺点。任何访问服务的代码都假定某处的某些代码已经注册了它。如果该初始化没有发生，我们要么崩溃，要么服务神秘地不工作。
- **Bind to it at compile time:**

- **在编译时绑定：**

The idea here is that the "location" process actually occurs at compile time using preprocessor macros. Like so:

这里的想法是，"定位"过程实际上在编译时使用预处理器宏发生。如下所示：

```cpp
class Locator {
public:
  static Audio& getAudio() { return service_; }

private:
  #if DEBUG
    static DebugAudio service_;
  #else
    static ReleaseAudio service_;
  #endif
};
```
Locating the service like this implies a few things:

- *速度快。* 由于所有实际工作都在编译时完成，运行时无事可做。编译器可能会内联 `getAudio()` 调用，为我们提供一个尽可能快的解决方案。

- *It's fast.* Since all of the real work is done at compile time, there's nothing left to do at runtime. The compiler will likely inline the `getAudio()` call, giving us a solution that's as fast as we could hope for.

- *可以保证服务可用。* 由于定位器现在拥有服务并在编译时选择它，我们可以保证如果游戏编译通过，我们不必担心服务不可用。

- *You can guarantee the service is available.* Since the locator owns the service now and selects it at compile time, we can be assured that if the game compiles, we won't have to worry about the service being unavailable.

- *不能轻易更改服务。* 这是主要缺点。由于绑定发生在构建时，任何时候你想更改服务，都必须重新编译并重启游戏。

- *You can't change the service easily.* This is the major downside. Since the binding happens at build time, anytime you want to change the service, you've got to recompile and restart the game.
- **Configure it at runtime:**

- **在运行时配置：**

Over in the khaki-clad land of enterprise business software, if you say "service locator", this is what they'll have in mind. When the service is requested, the locator does some magic at runtime to hunt down the actual implementation requested.

在卡其色西装的企业级商业软件领域，如果你说"服务定位器"，他们想到的就是这个。当请求服务时，定位器在运行时施展一些魔法来查找请求的实际实现。

*Reflection* is a capability of some programming languages to interact with the type system at runtime. For example, we could find a class with a given name, find its constructor, and then invoke it to create an instance.

*反射*是一些编程语言在运行时与类型系统交互的能力。例如，我们可以查找一个具有给定名称的类，找到它的构造函数，然后调用它来创建实例。

Dynamically typed languages like Lisp, Smalltalk, and Python get this by their very nature, but newer static languages like C# and Java also support it.

像Lisp、Smalltalk和Python这样的动态类型语言天生就具备这种能力，但像C#和Java这样的较新的静态语言也支持它。

Typically, this means loading a configuration file that identifies the provider and then using reflection to instantiate that class at runtime. This does a few things for us:

通常，这意味着加载一个标识提供者的配置文件，然后在运行时使用反射实例化该类。这为我们做了一些事情：

- *We can swap out the service without recompiling.* This is a little more flexible than a compile-time-bound service, but not quite as flexible as a registered one where you can actually change the service while the game is running.

- *无需重新编译即可交换服务。* 这比编译时绑定的服务稍微灵活一些，但不如注册式服务那样灵活（后者可以在游戏运行时实际更改服务）。

- *Non-programmers can change the service.* This is nice for when the designers want to be able to turn certain game features on and off but aren't comfortable mucking through source code. (Or, more likely, the *coders* aren't comfortable with them mucking through it.)

- *非程序员可以更改服务。* 当设计师希望能够开启和关闭某些游戏功能，但不习惯翻阅源代码时，这很好（或者更可能的是，*程序员*不习惯让他们翻阅源代码）。

- *The same codebase can support multiple configurations simultaneously.* Since the location process has been moved out of the codebase entirely, we can use the same code to support multiple service configurations simultaneously.

- *相同的代码库可以同时支持多种配置。* 由于定位过程已完全移出代码库，我们可以使用相同的代码同时支持多种服务配置。

This is one of the reasons this model is appealing over in enterprise web-land: you can deploy a single app that works on different server setups just by changing some configs. Historically, this was less useful in games since console hardware is pretty well-standardized, but as more games target a heaping hodgepodge of mobile devices, this is becoming more relevant.

这就是为什么这种模式在企业级Web领域具有吸引力的原因之一：你可以部署一个单一的应用程序，只需更改一些配置即可在不同的服务器设置上工作。从历史上看，这在游戏中不太有用，因为游戏主机硬件非常标准化，但随着越来越多的游戏面向大量各式各样的移动设备，这变得越来越相关。

- *It's complex.* Unlike the previous solutions, this one is pretty heavyweight. You have to create some configuration system, possibly write code to load and parse a file, and generally *do some stuff* to locate the service. Time spent writing this code is time not spent on other game features.

- *很复杂。* 与之前的解决方案不同，这个相当重量级。你必须创建一些配置系统，可能编写代码来加载和解析文件，并且通常*要做一些工作*来定位服务。花在编写这些代码上的时间就是没有花在其他游戏功能上的时间。

- *Locating the service takes time.* And now the smiles really turn to frowns. Going with runtime configuration means you're burning some CPU cycles locating the service. Caching can minimize this, but that still implies that the first time you use the service, the game's got to go off and spend some time hunting it down. Game developers *hate* burning CPU cycles on something that doesn't improve the player's game experience.

- *定位服务需要时间。* 笑脸真的要变成苦脸了。使用运行时配置意味着你在燃烧一些CPU周期来定位服务。缓存可以最小化这一点，但这仍然意味着第一次使用服务时，游戏需要花一些时间去寻找它。游戏开发者*讨厌*将CPU周期浪费在不能改善玩家游戏体验的事情上。
---

### What happens if the service can't be located? / 如果无法定位服务怎么办？

- **Let the user handle it:**

- **让用户处理：**

The simplest solution is to pass the buck. If the locator can't find the service, it just returns `NULL`. This implies:

最简单的解决方案是推卸责任。如果定位器找不到服务，它只返回 `NULL`。这意味着：

- *It lets users determine how to handle failure.* Some users may consider failing to find a service a critical error that should halt the game. Others may be able to safely ignore it and continue. If the locator can't define a blanket policy that's correct for all cases, then passing the failure down the line lets each call site decide for itself what the right response is.

- *让用户决定如何处理失败。* 某些用户可能认为找不到服务是应该停止游戏的严重错误。其他人可能能够安全地忽略它并继续。如果定位器无法定义一个对所有情况都正确的通用策略，那么将失败沿着调用链传递下去，让每个调用点自己决定正确的响应是什么。

- *Users of the service must handle the failure.* Of course, the corollary to this is that each call site *must* check for failure to find the service. If almost all of them handle failure the same way, that's a lot duplicate code spread throughout the codebase. If just one of the potentially hundreds of places that use the service fails to make that check, our game is going to crash.

- *服务的用户必须处理失败。* 当然，这的推论是每个调用点*必须*检查是否未能找到服务。如果几乎所有调用点都以相同方式处理失败，那将在代码库中散布大量重复代码。如果使用该服务的潜在数百个位置中只有一个未能进行检查，我们的游戏就会崩溃。
- **Halt the game:**

- **停止游戏：**

I said that we can't *prove* that the service will always be available at compile-time, but that doesn't mean we can't *declare* that availability is part of the runtime contract of the locator. The simplest way to do this is with an assertion:

我说过我们不能在编译时*证明*服务将始终可用，但这并不意味着我们不能*声明*可用性是定位器运行时约定的一部分。最简单的方法是使用断言：

```cpp
class Locator {
public:
  static Audio& getAudio()
  {
    Audio* service = NULL;

// Code here to locate service...

assert(service != NULL);
    return *service;
  }
};
```
If the service isn't located, the game stops before any subsequent code tries to use it. The `assert()` call there doesn't solve the problem of failing to locate the service, but it does make it clear whose problem it is. By asserting here, we say, "Failing to locate a service is a bug in the locator."

如果服务未被定位，游戏在任何后续代码尝试使用它之前停止。那里的 `assert()` 调用并不能解决未能定位服务的问题，但它确实明确了谁的问题。通过在这里断言，我们表明"未能定位服务是定位器的一个错误"。

The [Singleton](singleton.html) chapter explains the `assert()` function if you've never seen it before.

这为我们做了什么？

So what does this do for us?

- *用户不需要处理缺失的服务。* 由于单个服务可能在数百个地方使用，这可以节省大量代码。通过声明提供服务始终是定位器的职责，我们免除了服务用户的负担。

- *Users don't need to handle a missing service.* Since a single service may be used in hundreds of places, this can be a significant code saving. By declaring it the locator's job to always provide a service, we spare the users of the service from having to pick up that slack.

- *如果找不到服务，游戏将停止。* 万一真的找不到服务，游戏会停止。这很好，因为它迫使我们解决阻止服务被定位的错误（可能某些初始化代码没有在应该被调用的时候被调用），但对于那些因此被阻塞直到它被修复的其他人来说，这是一个真正的拖累。在一个大型开发团队中，当这种情况发生时，你可能会让程序员陷入痛苦的停机等待。

- *The game is going to halt if the service can't be found.* On the off chance that a service really can't be found, the game is going to halt. This is good in that it forces us to address the bug that's preventing the service from being located (likely some initialization code isn't being called when it should), but it's a real drag for everyone else who's blocked until it's fixed. With a large dev team, you can incur some painful programmer downtime when something like this breaks.
- **Return a null service:**

- **返回空服务：**

We showed this refinement in our sample implementation. Using this means:

我们在示例实现中展示了这个改进。使用这意味着：

- *Users don't need to handle a missing service.* Just like the previous option, we ensure that a valid service object will always be returned, simplifying code that uses the service.

- *用户不需要处理缺失的服务。* 与前一个选项一样，我们确保始终返回一个有效的服务对象，简化了使用服务的代码。

- *The game will continue if the service isn't available.* This is both a boon and a curse. It's helpful in that it lets us keep running the game even when a service isn't there. This can be really helpful on a large team when a feature we're working on may be dependent on some other system that isn't in place yet.

- *如果服务不可用，游戏将继续运行。* 这既是福也是祸。它的好处在于，即使服务不存在，我们也能继续运行游戏。在大型团队中，当我们正在开发的功能可能依赖于某个尚未到位的其他系统时，这非常有帮助。

The downside is that it may be harder to debug an *unintentionally* missing service. Say the game uses a service to access some data and then make a decision based on it. If we've failed to register the real service and that code gets a null service instead, the game may not behave how we want. It will take some work to trace that issue back to the fact that a service wasn't there when we thought it would be.

缺点是可能更难调试*意外*缺失的服务。假设游戏使用服务来访问某些数据并据此做出决策。如果我们未能注册真正的服务，而该代码获得了空服务，游戏可能不会按我们想要的方式运行。需要一些工作才能将问题追溯到这样一个事实：我们原以为服务存在，但它却没有及时到位。

We can alleviate this by having the null service print some debug output whenever it's used.

我们可以通过让空服务在被使用时打印一些调试输出来缓解这个问题。

* Among these options, the one I see used most frequently is simply asserting that the service will be found. By the time a game gets out the door, it's been very heavily tested, and it will likely be run on a reliable piece of hardware. The chances of a service failing to be found by then are pretty slim.

On a larger team, I encourage you to throw a null service in. It doesn't take much effort to implement, and can spare you from some downtime during development when a service isn't available. It also gives you an easy way to turn off a service if it's buggy or is just distracting you from what you're working on.

**CN:** 在这些选项中，我看到最常用的是简单地断言服务会被找到。到游戏发布时，它已经经过了非常严格的测试，并且很可能在可靠的硬件上运行。那时服务找不到的概率非常小。

在较大的团队中，我鼓励你加入空服务。实现它不需要太多努力，并且在开发期间服务不可用时可以节省你的停机时间。如果服务有bug或者只是干扰了你的工作，它还提供了一种简单的方法来关闭服务。
---

### What is the scope of the service? / 服务的范围是什么？

**EN (from *Game Programming Patterns*):**

Up to this point, we've assumed that the locator will provide access to the service to *anyone* who wants it. While this is the typical way the pattern is used, another option is to limit access to a single class and its descendants, like so:

**CN:** 到目前为止，我们一直假设定位器将为*任何*想要的人提供对服务的访问。虽然这是该模式的典型用法，但另一种选择是将访问限制在单个类及其子类，如下所示：

```cpp
class Base {
  // Code to locate service and set service_...

protected:
  // Derived classes can use service
  static Audio& getAudio() { return *service_; }

private:
  static Audio* service_;
};
```

With this, access to the service is restricted to classes that inherit `Base`. There are advantages either way:

有了这个，对服务的访问被限制在继承 `Base` 的类中。两种方式各有优势：

- **If access is global:**

- **如果访问是全局的：**

- *It encourages the entire codebase to all use the same service.* Most services are intended to be singular. By allowing the entire codebase to have access to the same service, we can avoid random places in code instantiating their own providers because they can't get to the "real" one.

- *它鼓励整个代码库都使用同一个服务。* 大多数服务旨在是单一的。通过允许整个代码库访问同一个服务，我们可以避免代码中随意的位置因为无法获得"真正的"服务而实例化自己的提供者。

- *We lose control over where and when the service is used.* This is the obvious cost of making something global — anything can get to it. The [Singleton](singleton.html) chapter has a full cast of characters for the horror show that global scope can spawn.

- *我们失去了对服务使用地点和时间的控制。* 这是使某些东西全局化的明显代价——任何东西都可以访问它。全局作用域可能引发的恐怖秀，在[单例](singleton.html)章节中有完整的角色阵容。

- **If access is restricted to a class:**

- **如果访问被限制在一个类中：**

- *We control coupling.* This is the main advantage. By limiting a service to a branch of the inheritance tree, we can make sure systems that should be decoupled stay decoupled.

- *我们控制耦合。* 这是主要优势。通过将服务限制在继承树的一个分支中，我们可以确保应该解耦的系统保持解耦。

- *It can lead to duplicate effort.* The potential downside is that if a couple of unrelated classes *do* need access to the service, they'll each need to have their own reference to it. Whatever process is used to locate or register the service will have to be duplicated between those classes.

- *可能导致重复工作。* 潜在的缺点是，如果几个不相关的类*确实*需要访问该服务，它们每个都需要有自己的引用。用于定位或注册服务的任何过程都必须在这些类之间重复。

(The other option is to change the class hierarchy around to give those classes a common base class, but that's probably more trouble than it's worth.)

（另一种选择是更改类层次结构，使这些类有一个共同的基类，但这可能得不偿失。）

* My general guideline is that if the service is restricted to a single domain in the game, then limit its scope to a class. For example, a service for getting access to the network can probably be limited to online classes. Services that get used more widely like logging should be global.

* 我的一般指导原则是，如果服务仅限于游戏中的单个领域，则将其范围限制在一个类中。例如，用于访问网络的服务可能可以限制在在线相关的类中。像日志这样更广泛使用的服务应该是全局的。
---

## See Also / 参见

**EN (from *Game Programming Patterns*):**

- The Service Locator pattern is a sibling to [Singleton](singleton.html) in many ways, so it's worth looking at both to see which is most appropriate for your needs.

- The [Unity](http://unity3d.com) framework uses this pattern in concert with the [Component](component.html) pattern in its [`GetComponent()`](http://docs.unity3d.com/412/Documentation/ScriptReference/Component.GetComponent.html?from=index) method.

- Microsoft's [XNA](http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.game.services.aspx) framework for game development has this pattern built into its core `Game` class. Each instance has a `GameServices` object that can be used to register and locate services of any type.

- 服务定位器模式在许多方面是[单例](singleton.html)模式的表亲，因此值得同时查看两者，以确定哪种最适合你的需求。

- [Unity](http://unity3d.com) 框架在其 [`GetComponent()`](http://docs.unity3d.com/412/Documentation/ScriptReference/Component.GetComponent.html?from=index) 方法中将此模式与[组件](component.html)模式结合使用。

- 微软的 [XNA](http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.game.services.aspx) 游戏开发框架将此模式内建在其核心 `Game` 类中。每个实例都有一个 `GameServices` 对象，可用于注册和定位任何类型的服务。

---

## C# Equivalent (C# 对照实现)

```csharp
using UnityEngine;
using System;

// ──────────────────────────────────────────────
// 服务定位器的 C# 实现 —— 泛型版本
// ──────────────────────────────────────────────

// 第一步：定义服务接口（相当于原书的 Audio 抽象类）
// 在 C# 中通常使用接口（interface）而非抽象类
public interface IAudioService
{
    void PlaySound(int soundId);
    void StopSound(int soundId);
    void StopAllSounds();
}

// 第二步：具体实现 —— 真正的音频系统
public class UnityAudioService : IAudioService
{
    // 使用 Unity 的 AudioSource 播放声音
    private readonly AudioSource _audioSource;

    public UnityAudioService(AudioSource audioSource)
    {
        _audioSource = audioSource;
    }

    public void PlaySound(int soundId)
    {
        // 从资源池加载并播放（实际项目中会使用 Resource 或 Addressables）
        Debug.Log($"播放声音: {soundId}");
        _audioSource.Play();  // Unity 的音频播放 API
    }

    public void StopSound(int soundId)
    {
        Debug.Log($"停止声音: {soundId}");
        _audioSource.Stop();
    }

    public void StopAllSounds()
    {
        Debug.Log("停止所有声音");
        _audioSource.Stop();
    }
}

// 第三步：空服务实现（Null Object 模式）
// 当没有注册真实服务时，提供"什么都不做"的安全默认值
public class NullAudioService : IAudioService
{
    public void PlaySound(int soundId)
    {
        // 什么也不做 —— 但不会崩溃
    }

    public void StopSound(int soundId) { }
    public void StopAllSounds() { }
}

// 第四步：泛型服务定位器（可注册任意类型的服务）
public static class ServiceLocator
{
    // 使用 Dictionary 存储所有服务，支持任意类型
    private static readonly Dictionary<Type, object> _services = new();

    // ── 初始化：注册空服务作为默认值 ──
    static ServiceLocator()
    {
        // 为 IAudioService 注册空实现
        Register<IAudioService>(new NullAudioService());
        // 可在此处继续注册其他服务的空实现...
    }

    // ── 注册服务（相当于原书的 provide）──
    public static void Register<T>(T service) where T : class
    {
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {
            Debug.LogWarning($"服务 {type.Name} 已被注册，将覆盖");
        }
        _services[type] = service;
    }

    // ── 获取服务（相当于原书的 getAudio）──
    public static T Get<T>() where T : class
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out object service))
        {
            return service as T;
        }

        // 如果没有注册，返回默认值（可能是 null）
        // 更好的做法：返回空服务（但需要空服务实例）
        Debug.LogError($"服务 {type.Name} 未被注册");
        return null;
    }

    // ── 安全获取：如果未注册，不抛出异常 ──
    public static bool TryGet<T>(out T service) where T : class
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out object obj))
        {
            service = obj as T;
            return service != null;
        }
        service = null;
        return false;
    }

    // ── 检查服务是否已注册 ──
    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }
}

// ──────────────────────────────────────────────
// 使用示例
// ──────────────────────────────────────────────

// ---- 游戏启动时注册服务 ----
public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // 创建真正的音频系统
        AudioSource source = gameObject.AddComponent<AudioSource>();
        IAudioService audioService = new UnityAudioService(source);

        // 注册到服务定位器
        ServiceLocator.Register<IAudioService>(audioService);

        // 注册其他服务...
        // ServiceLocator.Register<ILoggingService>(new FileLogger());
        // ServiceLocator.Register<ISaveService>(new PlayerPrefsSaveService());
    }
}

// ---- 任何地方使用服务（无需知道具体实现） ----
public class PlayerController : MonoBehaviour
{
    private void OnEnemyKilled()
    {
        // 通过服务定位器获取音频服务
        IAudioService audio = ServiceLocator.Get<IAudioService>();
        audio?.PlaySound(1001);  // 播放击杀音效
    }
}

// ---- 测试场景：使用 Mock 服务替代真实服务 ----
public class TestAudioService : IAudioService
{
    public int LastPlayedSoundId { get; private set; }

    public void PlaySound(int soundId)
    {
        LastPlayedSoundId = soundId;
        // 不发出真正的声音
    }

    public void StopSound(int soundId) { }
    public void StopAllSounds() { }
}

// 单元测试中注册测试服务
// ServiceLocator.Register<IAudioService>(new TestAudioService());
```

## Unity Application / Unity 应用场景

### 1. GetComponent<T>() —— Unity 内置的"服务定位器"
Unity 的 `GetComponent<T>()` 本身就是服务定位器模式的应用。`GameObject` 是定位器，`MonoBehaviour` 是服务：

```csharp
// GameObject = 服务定位器
// MonoBehaviour = 服务接口/实现

// "定位"服务
Rigidbody rb = gameObject.GetComponent<Rigidbody>();   // 获取物理服务
Animator anim = gameObject.GetComponent<Animator>();    // 获取动画服务
```

不同之处在于 `GetComponent` 的注册是隐式的——挂载组件到 GameObject 上即完成注册。这与 Service Locator 的显式注册方式不同，但核心思想一致。

### 2. 服务定位器 vs 单例（Singleton）

| 特征 | Singleton | Service Locator |
|------|-----------|-----------------|
| 耦合性 | 依赖具体类 | 依赖接口 |
| 可替换性 | 运行时无法替换实现 | 可运行时替换 |
| 测试友好 | 差（难以 Mock） | 好（可注入 Mock） |
| 复杂度 | 低 | 中 |
| Unity 典型例子 | `AudioManager.Instance` | `ServiceLocator.Get<IAudioService>()` |

**何时使用 Singleton**：小型项目、确定只有一个实现且不需要替换的功能（如 `Mathf`）。

**何时使用 Service Locator**：大型项目、需要在不同平台/构建中使用不同实现、需要单元测试。

### 3. 何时是反模式？

Service Locator 被批评为"增强型的全局变量"：

- **问题是**：它隐藏了依赖关系。只看一个类的代码，你不知道它依赖哪些服务。这违反了"显式依赖"原则。
- **替代方案**：**依赖注入（DI）**。通过构造函数显式传入依赖，而非从全局定位器获取。

```csharp
// 不好的方式（隐含依赖）：
public class Player
{
    public void Attack()
    {
        IAudioService audio = ServiceLocator.Get<IAudioService>();
        audio.PlaySound(42);
        // 谁也不知道 Player 依赖 IAudioService！
    }
}

// 更好的方式（显式依赖注入）：
public class Player
{
    private readonly IAudioService _audio;

    public Player(IAudioService audio)  // 依赖关系一目了然
    {
        _audio = audio;
    }

    public void Attack()
    {
        _audio.PlaySound(42);
    }
}
```

**Unity 中的实际权衡**：Unity 不原生支持构造函数注入（MonoBehaviour 由 Unity 实例化），因此 Service Locator 在 Unity 中比在普通 C# 项目中更常见、更可接受。

### 4. 使用 Zenject / VContainer 进行依赖注入
在大型 Unity 项目中，可以使用 DI 框架（如 Zenject、VContainer、StrangeIoC）替代手工实现的 Service Locator：

```csharp
// 使用 Zenject 的依赖注入（比 Service Locator 更优的替代方案）
public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // 注册服务（类似于 ServiceLocator.Register）
        Container.Bind<IAudioService>()
                 .To<UnityAudioService>()
                 .AsSingle();  // 单例
    }
}

// 消费服务（通过构造函数自动注入）
public class PlayerController : MonoBehaviour
{
    private IAudioService _audio;

    [Inject]  // Zenject 自动注入
    public void Construct(IAudioService audio)
    {
        _audio = audio;
    }
}
```

### 5. 实际项目中的服务分类

```
├── 基础设施服务（几乎全局需要）
│   ├── ILoggingService      → 日志记录
│   ├── IAudioService        → 音频播放
│   ├── ISaveService         → 存档/读档
│   └── IAnalyticsService    → 数据分析
│
├── 游戏功能服务（多个系统需要）
│   ├── IInventoryService    → 背包系统
│   ├── IQuestService        → 任务系统
│   ├── IAchievementService  → 成就系统
│   └── IAdService           → 广告系统
│
└── 平台相关服务（按平台替换）
    ├── IIAPService          → 内购（App Store / Google Play）
    ├── INotificationService → 本地推送通知
    └── ICloudSaveService    → 云存档
```

## Key Differences / 关键差异

| Aspect | C++ (原书) | C# / Unity |
|--------|-----------|------------|
| **接口定义** | 抽象类（virtual + pure = 0） | 接口（`interface`） |
| **泛型支持** | 无（针对每个服务单独实现） | 泛型 `ServiceLocator<T>` 一次实现多处使用 |
| **空服务** | 独立的 `NullAudio` 类 | 在 `static` 构造器中注册空实现 |
| **默认值安全** | 返回引用确保非 null | 返回 `null` 或使用空服务模式 |
| **线程安全** | 未讨论（需要手动加锁） | 可使用 `ConcurrentDictionary` 实现线程安全 |
| **DI 框架** | 无统一标准 | Zenject / VContainer / StrangeIoC 等 |
| **Unity 特性** | 不适用 | `GetComponent<T>()` 是 Unity 内置的定位器 |
| **注册方式** | 手动调用 `provide()` | 启动时统一注册或通过 DI 容器自动注册 |
| **生命周期** | 手动管理（new/delete） | 依赖 Unity 生命周期或 DI 容器管理 |

### 最佳实践总结

```
适合 Service Locator:
  - 跨多个不相关系统的全局服务（日志、音频、分析）
  - 需要运行时切换实现的场景
  - Unity 项目中作为 DI 容器无法使用时的替代
  - 快速原型开发

不适合 Service Locator:
  - 只有一个实现且不会变化的简单系统
  - 对性能极端敏感的热路径（额外的间接调用开销）
  - 小团队小项目（过度设计）
  - 需要严格控制依赖可见性的场景

推荐替代方案（按优先级）:
  1. 构造函数注入（纯 C# 类）
  2. DI 框架（Zenject/VContainer）
  3. Service Locator（Unity 中可接受）
  4. Singleton（小型项目）
  5. 静态类（最简单的工具类）
```

---

# Data Locality — 数据局部性

## Intent / 意图

* *Accelerate memory access by arranging data to take advantage of CPU caching.*

* *通过合理组织数据布局来利用 CPU 缓存，加速内存访问。*

## Motivation / 动机

We've been lied to. They keep showing us charts where CPU speed goes up and up every year as if Moore's Law isn't just a historical observation but some kind of divine right. Without lifting a finger, we software folks watch our programs magically accelerate just by virtue of new hardware.

我们被欺骗了。他们不断展示 CPU 速度逐年攀升的图表，仿佛摩尔定律不只是历史观察而是某种神圣权利。我们软件人员不费吹灰之力，就看着程序凭借新硬件神奇地加速。

Chips *have* been getting faster (though even that's plateauing now), but the hardware heads failed to mention something. Sure, we can *process* data faster than ever, but we can't *get* that data faster.

芯片确实越来越快（尽管现在也在趋于平稳），但硬件专家们没有提到一件事：我们确实能比以往更快地*处理*数据，但我们无法*获取*那些数据。

![A chart showing processor and RAM speed from 1980 to 2010. Processor speed increases quickly, but RAM speed lags behind.](images/data-locality-chart.png)

![处理器和 RAM 速度对比图 1980-2010](images/data-locality-chart.png)

Processor and RAM speed relative to their respective speeds in 1980. As you can see, CPUs have grown in leaps and bounds, but RAM access is lagging far behind.

处理器和 RAM 速度相对于各自 1980 年速度的对比。如你所见，CPU 增长迅猛，但 RAM 访问远远落后。

Data for this is from *Computer Architecture: A Quantitative Approach* by John L. Hennessy, David A. Patterson, Andrea C. Arpaci-Dusseau by way of Tony Albrecht's "[Pitfalls of Object-Oriented Programming](http://seven-degrees-of-freedom.blogspot.com/2009/12/pitfalls-of-object-oriented-programming.html)".

要让超快 CPU 完成大量计算，它必须先把数据从主存取出放到寄存器中。RAM 的速度远远跟不上 CPU 的增长。

For your super-fast CPU to blow through a ream of calculations, it actually has to get the data out of main memory and into registers. As you can see, RAM hasn't been keeping up with increasing CPU speeds. Not even close.

在今天的硬件上，从 RAM 获取一个字节的数据可能需要*数百个*周期。如果大多数指令都需要数据，而获取数据需要数百个周期，为什么我们的 CPU 没有 99% 的时间在空等数据？

With today's hardware, it can take *hundreds* of cycles to fetch a byte of data from RAM. If most instructions need data, and it takes hundreds of cycles to get it, how is it that our CPUs aren't sitting idle 99% of the time waiting for data?

实际上，如今它们确实有很大一部分时间在等待内存，只是情况没有糟糕到极点。解释这一点，让我们来一段过于冗长的比喻……

Actually, they *are* stuck waiting on memory an astonishingly large fraction of time these days, but it's not as bad as it could be. To explain how, let's take a trip to the Land of Overly Long Analogies…

之所以叫"随机访问存储器"，是因为理论上你可以像访问任何其他位置一样快速地访问任意一个数据位置。你不必像磁盘那样担心连续读取的问题。

It's called "random access memory" because, unlike disc drives, you can theoretically access any piece of it as quick as any other. You don't have to worry about reading things consecutively like you do a disc.

至少，过去是这样。但我们会看到，RAM 现在已经不那么随机访问了。

Or, at least, you *didn't*. As we'll see, RAM isn't so random access anymore.

#### 数据仓库

### A data warehouse

想象你是一个小办公室里的会计。你的工作是申请一箱文件，然后做一些会计工作——把一堆数字加起来之类的。你需要根据只有其他会计才能理解的某种神秘逻辑，来处理特定标签的箱子。

Imagine you're an accountant in a tiny little office. Your job is to request a box of papers and then do some accountant-y stuff with them — add up a bunch of numbers or something. You must do this for specific labeled boxes according to some arcane logic that only makes sense to other accountants.

由于勤奋、天赋和咖啡因的混合作用，你大约一分钟就能处理完一箱。但有个小问题：所有箱子都存放在另一栋楼的仓库里。要拿到箱子，你得让仓库管理员给你送来。他开着叉车在货架间穿行，直到找到你想要的箱子。

Thanks to a mixture of hard work, natural aptitude, and stimulants, you can finish an entire box in, say, a minute. There's a little problem, though. All of those boxes are stored in a warehouse in a separate building. To get a box, you have to ask the warehouse guy to bring it to you. He goes and gets a forklift and drives around the aisles until he finds the box you want.

这需要整整一天时间。不像你，他短时间内拿不到月度最佳员工。这意味着无论你多快，一天也只能处理一箱。其余时间你就坐在那里，质疑是什么人生选择导致了这份吸食灵魂的工作。

It takes him, seriously, an entire day to do this. Unlike you, he's not getting employee of the month any time soon. This means that no matter how fast you are, you only get one box a day. The rest of the time, you just sit there and question the life decisions that led to this soul-sucking job.

一天，来了一组工业设计师。他们的工作是提高运营效率——比如让流水线更快运转。观察你几天后，他们发现了几件事：

One day, a group of industrial designers shows up. Their job is to improve the efficiency of operations — things like making assembly lines go faster. After watching you work for a few days, they notice a few things:

- 很多时候，当你处理完一个箱子，下一个需要的箱子就在仓库的同一个架子上紧挨着它。
- 用叉车只搬运一箱文件很蠢。
- 你办公室角落里其实有点空地方。

- Pretty often, when you're done with one box, the next box you request is right next to it on the same shelf in the warehouse.
- Using a forklift to carry a single box of papers is pretty dumb.
- There's actually a little bit of spare room in the corner of your office.

使用刚用过的东西附近的东西，术语叫*引用局部性*。

The technical term for using something near the thing you just used is *locality of reference*.

他们想出了一个聪明的办法。每当你向仓库管理员申请一个箱子时，他会直接取一整托盘。他拿到你想要的箱子，再加上旁边的一些箱子。他不知道你是否需要那些箱子（以他的工作态度来看，显然也不在乎）；他只是尽量多地往托盘上放。

They come up with a clever fix. Whenever you request a box from the warehouse guy, he'll grab an entire pallet of them. He gets the box you want and then some more boxes that are next to it. He doesn't know if you want those (and, given his work ethic, clearly doesn't care); he simply takes as many as he can fit on the pallet.

他把整托盘运来给你。不顾工作场所安全，他直接把叉车开进来，把托盘放在你办公室的角落里。

He loads the whole pallet and brings it to you. Disregarding concerns for workplace safety, he drives the forklift right in and drops the pallet in the corner of your office.

现在当你需要新箱子时，首先看看它是否已经在办公室的托盘上了。如果是，太好了！你只需要一秒钟就能拿到它，继续算数字。如果一个托盘能装 50 个箱子，而你足够幸运*所有*需要的箱子都在上面，你的工作效率就能提高 50 倍。

When you need a new box, now, the first thing you do is see if it's already on the pallet in your office. If it is, great! It only takes you a second to grab it and get back to crunching numbers. If a pallet holds fifty boxes and you got lucky and *all* of the boxes you need happen to be on it, you can churn through fifty times more work than you could before.

但如果需要的箱子*不在*托盘上，你就回到了原点。因为你办公室只能放一个托盘，仓库管理员得把旧的拿走，再给你取全新的。

But if you need a box that's *not* on the pallet, you're back to square one. Since you can only fit one pallet in your office, your warehouse friend will have to take that one back and then bring you an entirely new one.

#### CPU 的托盘

### A pallet for your CPU

奇怪的是，这与现代计算机 CPU 的工作方式类似。如果还不够明显：你扮演 CPU，你的办公桌是 CPU 的寄存器，文件箱是寄存器能装下的数据，仓库是你的机器 RAM，而那个烦人的仓库管理员是把数据从主存搬到寄存器的总线。

Strangely enough, this is similar to how CPUs in modern computers work. In case it isn't obvious, you play the role of the CPU. Your desk is the CPU's registers, and the box of papers is the data you can fit in them. The warehouse is your machine's RAM, and that annoying warehouse guy is the bus that pulls data from main memory into registers.

如果我在三十年前写这一章，比喻到此就结束了。但随着芯片越来越快而 RAM 没有跟上，硬件工程师开始寻找解决方案。他们想到的是 *CPU 缓存*。

If I were writing this chapter thirty years ago, the analogy would stop there. But as chips got faster and RAM, well, *didn't*, hardware engineers started looking for solutions. What they came up with was *CPU caching*.

现代计算机在芯片内部有一小块内存。CPU 从中读取数据比从主存快得多。它很小，因为要放在芯片里，而且它使用的更快的内存类型（静态 RAM 或 "SRAM"）昂贵得多。

Modern computers have a little chunk of memory right inside the chip. The CPU can pull data from this much faster than it can from main memory. It's small because it has to fit in the chip and because the faster type of memory it uses (static RAM or "SRAM") is way more expensive.

现代硬件有多级缓存，即你听到的 "L1"、"L2"、"L3" 等。每一级都比前一级更大但更慢。在本章中，我们不会深入内存实际上是一个[层级结构](http://en.wikipedia.org/wiki/Memory_hierarchy)的细节，但知道这一点很重要。

Modern hardware has multiple levels of caching, which is what they mean when you hear "L1", "L2", "L3", etc. Each level is larger but slower than the previous. For this chapter, we won't worry about the fact that memory is actually a [hierarchy](http://en.wikipedia.org/wiki/Memory_hierarchy), but it's important to know.

这一小块内存称为*缓存*（具体来说，芯片上的那一块是 *L1 缓存*），在我费劲的比喻中，它扮演了托盘的角色。每当你的芯片需要从 RAM 获取一个字节的数据时，它会自动抓取一整块连续内存——通常大约 64 到 128 字节——放入缓存。这一块内存称为*缓存行*。

This little chunk of memory is called a *cache* (in particular, the chunk on the chip is your *L1 cache*), and in my belabored analogy, its part was played by the pallet of boxes. Whenever your chip needs a byte of data from RAM, it automatically grabs a whole chunk of contiguous memory — usually around 64 to 128 bytes — and puts it in the cache. This dollop of memory is called a *cache line*.

![缓存行示意图](images/data-locality-cache-line.png)

![A cache line showing the one byte requested along with the adjacent bytes that also get loaded into the cache.](images/data-locality-cache-line.png)

如果你需要的下一个字节恰好就在那个块中，CPU 直接从缓存读取，比访问 RAM *快得多*。成功在缓存中找到数据称为*缓存命中*。如果找不到而去主存寻找，那就是*缓存未命中*。

If the next byte of data you need happens to be in that chunk, the CPU reads it straight from the cache, which is *much* faster than hitting RAM. Successfully finding a piece of data in the cache is called a *cache hit*. If it can't find it in there and has to go to main memory, that's a *cache miss*.

当缓存未命中发生时，CPU *停顿*——它无法处理下一条指令，因为它需要数据。它无所事事地坐在那里几百个周期，直到数据获取完成。我们的任务就是避免这种情况。

When a cache miss occurs, the CPU *stalls* — it can't process the next instruction because it needs data. It sits there, bored out of its mind for a few hundred cycles until the fetch completes. Our mission is to avoid that. Imagine you're trying to optimize some performance-critical piece of game code and it looks like this:

#### 等等，数据即性能？

```
for (int i = 0; i < NUM_THINGS; i++) {
  sleepFor500Cycles();
  things[i].doStuff();
}
```

我开始写这一章时，花了些时间编写触发缓存最佳和最差情况的小型类游戏程序。我想要能让缓存崩溃的基准测试，以便亲自看看这会造成多大的伤害。

What's the first change you're going to make to that code? Right. Take out that pointless, expensive function call. That call is equivalent to the performance cost of a cache miss. Every time you bounce to main memory, it's like you put a delay in your code.

结果让我很惊讶。我知道这很重要，但亲眼所见还是不同。我写了两个程序执行*完全相同的*计算。唯一的区别是它们引起的缓存未命中次数。慢的那个比另一个*慢了 50 倍*。

### Wait, data is performance?

这让我大开眼界。我习惯于认为性能是*代码*的属性，而不是*数据*。一个字节无所谓快慢，它只是静态地待在那里。但由于缓存的存在，*组织数据的方式直接影响性能*。

When I started working on this chapter, I spent some time putting together little game-like programs that would trigger best case and worst case cache usage. I wanted benchmarks that would thrash the cache so I could see first-hand how much bloodshed it causes.

归结起来很简单：每当芯片读取一些内存时，它会得到整个缓存行。你能利用该缓存行中的内容越多，运行速度就越快。因此目标是*组织你的数据结构，使你正在处理的数据在内存中彼此相邻*。

When I got some stuff working, I was surprised. I knew it was a big deal, but there's nothing quite like seeing it with your own eyes. I wrote two programs that did the *exact same* computation. The only difference was how many cache misses they caused. The slow one was *fifty times* slower than the other.

但这里有一个关键假设：单线程。如果你在多线程上修改相邻数据，让它们位于*不同的*缓存行上更快。如果两个线程试图修改同一缓存行上的数据，两个核心必须进行昂贵的缓存同步。

There are a lot of caveats here. In particular, different computers have different cache setups, so my machine may be different from yours, and dedicated game consoles are very different from PCs, which are quite different from mobile devices.

换句话说，如果你的代码依次处理 `Thing`、`Another`、`Also`，你希望它们在内存中这样排列：

Your mileage will vary.

![Thing, Another, Also 在内存中连续排列](images/data-locality-things.png)

This was a real eye-opener to me. I'm used to thinking of performance being an aspect of *code*, not *data*. A byte isn't slow or fast, it's just some static thing sitting there. But because of caching, *the way you organize data directly impacts performance*.

注意，这些不是指向 `Thing`、`Another`、`Also` 的*指针*。而是它们实际的数据，原地排列，一个接一个。一旦 CPU 读入 `Thing`，它就会开始获取 `Another` 和 `Also`（取决于它们的大小和缓存行的大小）。当你接下来处理它们时，它们已经在缓存中了。

The challenge now is to wrap that up into something that fits into a chapter here. Optimization for cache usage is a huge topic. I haven't even touched on *instruction caching*. Remember, code is in memory too and has to be loaded onto the CPU before it can be executed. Someone more versed on the subject could write an entire book on it.

In fact, someone *did* write a book on it: [*Data-Oriented Design*](http://www.dataorienteddesign.com/dodmain/), by Richard Fabian.

Since you're already reading *this* book right now, though, I have a few basic techniques that will get you started along the path of thinking about how data structures impact your performance.

It all boils down to something pretty simple: whenever the chip reads some memory, it gets a whole cache line. The more you can use stuff in that cache line, the faster you go. So the goal then is to *organize your data structures so that the things you're processing are next to each other in memory*.

There's a key assumption here, though: one thread. If you are modifying nearby data on multiple threads, it's faster to have it on *different* cache lines. If two threads try to tweak data on the same cache line, both cores have to do some costly synchronization of their caches.

In other words, if your code is crunching on `Thing`, then `Another`, then `Also`, you want them laid out in memory like this:

![Thing, Another, and Also laid out directly next to each other in order in memory.](images/data-locality-things.png)

Note, these aren't *pointers* to `Thing`, `Another`, and `Also`. This is the actual data for them, in place, lined up one after the other. As soon as the CPU reads in `Thing`, it will start to get `Another` and `Also` too (depending on how big they are and how big a cache line is). When you start working on them next, they'll already be cached. Your chip is happy, and you're happy.
## The Pattern / 模式

* Modern CPUs have **caches to speed up memory access**. These can access memory **adjacent to recently accessed memory much quicker**. Take advantage of that to improve performance by **increasing data locality** — keeping data in **contiguous memory in the order that you process it**.

* 现代 CPU 拥有**加速内存访问的缓存**，可以**更快地访问最近访问过的内存的相邻区域**。利用这一点来提升性能：**增加数据局部性**——将数据按处理顺序**保存在连续内存中**。

## When to Use It / 何时使用

Like most optimizations, the first guideline for using the Data Locality pattern is *when you have a performance problem.* Don't waste time applying this to some infrequently executed corner of your codebase. Optimizing code that doesn't need it just makes your life harder since the result is almost always more complex and less flexible.

与大多数优化一样，使用数据局部性模式的第一条指导原则是*当你遇到性能问题时*。不要浪费时间将它应用到代码库中某些很少执行的角落。优化不需要优化的代码只会让你的生活更艰难，因为结果几乎总是更复杂、更不灵活。

With this pattern specifically, you'll also want to be sure your performance problems *are caused by cache misses*. If your code is slow for other reasons, this won't help.

具体到这个模式，你还需确认你的性能问题*确实是由缓存未命中引起的*。如果你的代码因其他原因变慢，这不会有帮助。

The cheap way to profile is to manually add a bit of instrumentation that checks how much time has elapsed between two points in the code, hopefully using a precise timer. To catch poor cache usage, you'll want something a little more sophisticated. You really want to see how many cache misses are occurring and where.

廉价的性能分析方法是手动添加一些检测代码来检查两点之间经过的时间。要捕获缓存使用不佳的问题，你需要更复杂的方法。你真正需要知道的是发生了多少缓存未命中以及发生在哪里。

Fortunately, there are profilers out there that report this. It's worth spending the time to get one of these working and make sure you understand the (surprisingly complex) numbers it throws at you before you do major surgery on your data structures.

幸运的是，有些性能分析工具可以报告这些信息。在对数据结构进行大手术之前，花时间让这些工具正常工作并理解它抛出的（令人惊讶地复杂的）数字是值得的。

Unfortunately, most of those tools aren't cheap. If you're on a console dev team, you probably already have licenses for them.

不幸的是，大多数这些工具并不便宜。如果你是主机开发团队的成员，你可能已经有许可证了。

If not, an excellent free option is [Cachegrind](http://valgrind.org/docs/manual/cg-manual.html). It runs your program on top of a simulated CPU and cache hierarchy and then reports all of the cache interactions.

如果没有，一个优秀的免费选项是 [Cachegrind](http://valgrind.org/docs/manual/cg-manual.html)。它在模拟的 CPU 和缓存层次结构之上运行你的程序，然后报告所有缓存交互。

That being said, cache misses *will* affect the performance of your game. While you shouldn't spend a ton of time pre-emptively optimizing for cache usage, do think about how cache-friendly your data structures are throughout the design process.

话虽如此，缓存未命中*确实*会影响你的游戏性能。虽然你不应该花费大量时间预先优化缓存使用，但在整个设计过程中要考虑你的数据结构对缓存是否友好。
## Keep in Mind / 牢记

One of the hallmarks of software architecture is *abstraction*. A large chunk of this book is about patterns to decouple pieces of code from each other so that they can be changed more easily. In object-oriented languages, this almost always means interfaces.

软件架构的标志之一是*抽象*。本书很大一部分内容是关于解耦代码片段的模式，使它们更容易修改。在面向对象语言中，这几乎总是意味着接口。

In C++, using interfaces implies accessing objects through pointers or references. But going through a pointer means hopping across memory, which leads to the cache misses this pattern works to avoid.

在 C++ 中，使用接口意味着通过指针或引用来访问对象。但通过指针意味着在内存中跳跃，这会导致此模式试图避免的缓存未命中。

The other half of interfaces is *virtual method calls*. Those require the CPU to look up an object's vtable and then find the pointer to the actual method to call there. So, again, you're chasing pointers, which can cause cache misses.

接口的另一半是*虚方法调用*。这需要 CPU 查找对象的 vtable，然后找到指向实际方法的指针。因此，你又在追逐指针，这会导致缓存未命中。

In order to please this pattern, you will have to sacrifice some of your precious abstractions. The more you design your program around data locality, the more you will have to give up inheritance, interfaces, and the benefits those tools can provide. There's no silver bullet here, only challenging trade-offs. That's what makes it fun!

为了满足这个模式，你将不得不牺牲一些宝贵的抽象。你越围绕数据局部性设计程序，就越需要放弃继承、接口以及这些工具提供的好处。这里没有银弹，只有充满挑战的权衡。
## Sample Code / 示例代码

* If you really go down the rathole of optimizing for data locality, you'll discover countless ways to slice and dice your data structures into pieces your CPU can most easily digest. To get you started, I'll show an example for each of a few of the most common ways to organize your data. We'll cover them in the context of some specific part of a game engine, but (as with other patterns), keep in mind that the general technique can be applied anywhere it fits.

* 如果你真正深入数据局部性优化的兔子洞，你会发现无数种方法将数据结构切分成 CPU 最容易消化的片段。为了让你入门，我将展示几种最常见的数据组织方式的示例。我们会以游戏引擎的特定部分为背景来讲解，但记住（如同其他模式），通用技术可以应用于任何合适的地方。

### Contiguous arrays / 连续数组

* Let's start with a [game loop](game-loop.html) that processes a bunch of game entities. Those entities are decomposed into different domains — AI, physics, and rendering — using the [Component](component.html) pattern. Here's the `GameEntity` class:

* 让我们从一个处理大量游戏实体的[游戏循环](game-loop.html)开始。这些实体通过[组件](component.html)模式分解为不同的领域——AI、物理和渲染。以下是 `GameEntity` 类：

```cpp
class GameEntity {
public:
  GameEntity(AIComponent* ai,
             PhysicsComponent* physics,
             RenderComponent* render)
  : ai_(ai), physics_(physics), render_(render)
  {}

  AIComponent* ai() { return ai_; }
  PhysicsComponent* physics() { return physics_; }
  RenderComponent* render() { return render_; }

private:
  AIComponent* ai_;
  PhysicsComponent* physics_;
  RenderComponent* render_;
};
```

* Each component has a relatively small amount of state, maybe little more than a few vectors or a matrix, and then a method to update it. The details aren't important here, but imagine something roughly along the lines of:

* 每个组件有相对少量的状态——可能不过几个向量或一个矩阵——以及一个更新方法。细节不重要，大致如下：

```cpp
class AIComponent {
public:
  void update() { /* Work with and modify state... */ }

private:
  // Goals, mood, etc. ...
};

class PhysicsComponent {
public:
  void update() { /* Work with and modify state... */ }

private:
  // Rigid body, velocity, mass, etc. ...
};

class RenderComponent {
public:
  void render() { /* Work with and modify state... */ }

private:
  // Mesh, textures, shaders, etc. ...
};
```

* The game maintains a big array of pointers to all of the entities in the world. Each spin of the game loop, we need to run the following:
1. Update the AI components for all of the entities.
2. Update the physics components for them.
3. Render them using their render components.

Lots of game engines implement that like so:

**CN:** 游戏维护一个指向世界中所有实体指针的大数组。每次游戏循环，我们需要：
1. 更新所有实体的 AI 组件。
2. 更新它们的物理组件。
3. 使用渲染组件渲染它们。

许多游戏引擎的实现如下：

```cpp
while (!gameOver) {
  // Process AI.
  for (int i = 0; i < numEntities; i++)
  {
    entities[i]->ai()->update();
  }

  // Update physics.
  for (int i = 0; i < numEntities; i++)
  {
    entities[i]->physics()->update();
  }

  // Draw to screen.
  for (int i = 0; i < numEntities; i++)
  {
    entities[i]->render()->render();
  }

  // Other game loop machinery for timing...
}
```

* Before you ever heard of a CPU cache, this looked totally innocuous. But by now, you've got an inkling that something isn't right here. This code isn't just thrashing the cache, it's taking it around back and beating it to a pulp. Watch what it's doing:
1. The array of game entities is storing *pointers* to them, so for each element in the array, we have to traverse that pointer. That's a cache miss.
2. Then the game entity has a pointer to the component. Another cache miss.
3. Then we update the component.
4. Now we go back to step one for *every component of every entity in the game*.

The scary part is that we have no idea how these objects are laid out in memory. We're completely at the mercy of the memory manager. As entities get allocated and freed over time, the heap is likely to become increasingly randomly organized.

**CN:** 在你听说 CPU 缓存之前，这看起来完全无害。但现在你应该感觉到不对劲了。这段代码不是在冲击缓存，而是在把缓存拖到后巷暴打。看看它在做什么：
1. 实体数组存储的是*指针*，因此每个元素我们都要遍历指针。一次缓存未命中。
2. 然后实体有指向组件的指针。又一次缓存未命中。
3. 然后更新组件。
4. 对游戏中*每个实体的每个组件*重复第一步。

可怕的是，我们完全不知道这些对象在内存中如何布局。我们完全受内存管理器的摆布。随着实体的分配和释放，堆会变得越来越随机排列。

![A tangled mess of objects strewn randomly through memory with pointers wiring them all together.](images/data-locality-pointer-chasing.png)

* Every frame, the game loop has to follow all of those arrows to get to the data it cares about.

* 每一帧，游戏循环都必须沿着所有这些箭头去获取它关心的数据。

* The term for wasting a bunch of time traversing pointers is "pointer chasing", which it turns out is nowhere near as fun as it sounds.

Let's do something better. Our first observation is that the only reason we follow a pointer to get to the game entity is so we can immediately follow *another* pointer to get to a component. `GameEntity` itself has no interesting state and no useful methods. The *components* are what the game loop cares about.

Instead of a giant constellation of game entities and components scattered across the inky darkness of address space, we're going to get back down to Earth. We'll have a big array for each type of component: a flat array of AI components, another for physics, and another for rendering.

**CN:** 浪费时间遍历指针的术语叫"指针追逐"，事实证明它远没有听起来那么有趣。

让我们做得更好。首先注意到，我们跟随指针到达游戏实体的唯一原因是为了立即跟随*另一个*指针到达组件。`GameEntity` 本身没有有趣的状态和有用的方法。*组件*才是游戏循环关心的。

我们会为每种组件准备一个大数组：一个 AI 组件的平面数组，一个物理组件的，一个渲染组件的。

```cpp
AIComponent* aiComponents =
    new AIComponent[MAX_ENTITIES];
PhysicsComponent* physicsComponents =
    new PhysicsComponent[MAX_ENTITIES];
RenderComponent* renderComponents =
    new RenderComponent[MAX_ENTITIES];
```

* Let me stress that these are arrays of *components* and not *pointers to components*. The data is all there, one byte after the other. The game loop can then walk these directly:

* 让我强调，这些是*组件*的数组，不是指向组件的指针。数据全部一个字节挨着一个字节地放在那里。游戏循环可以直接遍历它们：

```cpp
while (!gameOver) {
  // Process AI.
  for (int i = 0; i < numEntities; i++)
  {
    aiComponents[i].update();
  }

  // Update physics.
  for (int i = 0; i < numEntities; i++)
  {
    physicsComponents[i].update();
  }

  // Draw to screen.
  for (int i = 0; i < numEntities; i++)
  {
    renderComponents[i].render();
  }

  // Other game loop machinery for timing...
}
```

* One hint that we're doing better here is how few `->` operators there are in the new code. If you want to improve data locality, look for indirection operators you can get rid of.

We've ditched all of that pointer chasing. Instead of skipping around in memory, we're doing a straight crawl through three contiguous arrays.

**CN:** 一个提示我们做得更好的迹象是新代码中有多么少的 `->` 操作符。如果你想改善数据局部性，寻找可以去掉的间接操作符。

我们抛弃了所有指针追逐。不再在内存中跳跃，而是直线遍历三个连续数组。

![An array for each of three different kinds of components. Each array neatly packs its components together.](images/data-locality-component-arrays.png)

* This pumps a solid stream of bytes right into the hungry maw of the CPU. In my testing, this change made the update loop *fifty times* faster than the previous version.

Interestingly, we haven't lost much encapsulation here. Sure, the game loop is updating the components directly instead of going through the game entities, but it was doing that before to ensure they were processed in the right order. Even so, each component itself is still nicely encapsulated. It owns its own data and methods. We simply changed the way it's used.

This doesn't mean we need to get rid of `GameEntity` either. We can leave it as it is with pointers to its components. They'll just point into those arrays. This is still useful for other parts of the game where you want to pass around a conceptual "game entity" and everything that goes with it. The important part is that the performance-critical game loop sidesteps that and goes straight to the data.

**CN:** 这直接将稳定的字节流送入 CPU 的饥渴大口。在我的测试中，这个改变使更新循环比之前版本*快了 50 倍*。

有趣的是，我们并没有失去太多封装性。当然，游戏循环直接更新组件而不是通过游戏实体，但之前它这样做是为了确保以正确的顺序处理它们。即便如此，每个组件本身仍然很好地封装了。它拥有自己的数据和方法。我们只是改变了使用方式。

这也不意味着我们需要去掉 `GameEntity`。我们可以保持原样，让它持有指向组件的指针，指向这些数组中的元素。这对于游戏中你想要传递一个概念性的"游戏实体"及其所有内容的其他部分仍然有用。关键部分是性能关键的游戏循环绕开了它，直接访问数据。

### Packed data / 数据紧凑排列

* Say we're doing a particle system. Following the advice of the previous section, we've got all of our particles in a nice big contiguous array. Let's wrap it in a little manager class too:

* 假设我们在做粒子系统。按照上一节的建议，我们把所有粒子放在一个漂亮的大连续数组中。再把它包在一个小管理类中：

```cpp
class Particle {
public:
  void update() { /* Gravity, etc. ... */ }
  // Position, velocity, etc. ...
};

class ParticleSystem {
public:
  ParticleSystem()
  : numParticles_(0)
  {}

  void update();
private:
  static const int MAX_PARTICLES = 100000;

  int numParticles_;
  Particle particles_[MAX_PARTICLES];
};
```

* A rudimentary update method for the system just looks like this:

* 系统的基本更新方法如下：

```cpp
void ParticleSystem::update() {
  for (int i = 0; i < numParticles_; i++)
  {
    particles_[i].update();
  }
}
```

* But it turns out that we don't actually need to process *all* of the particles all the time. The particle system has a fixed-size pool of objects, but they aren't usually all actively twinkling across the screen. The easy answer is something like this:

* 但事实证明我们并不总是需要处理*所有*粒子。粒子系统有一个固定大小的对象池，但通常并不是所有粒子都活跃地在屏幕上闪烁。一个简单的答案是像这样：

```cpp
for (int i = 0; i < numParticles_; i++) {
  if (particles_[i].isActive())
  {
    particles_[i].update();
  }
}
```

* We give `Particle` a flag to track whether its in use or not. In the update loop, we check that for each particle. That loads the flag into the cache along with all of that particle's other data. If the particle *isn't* active, then we skip over it to the next one. The rest of the particle's data that we loaded into the cache is a waste.

The fewer active particles there are, the more we're skipping across memory. The more we do that, the more cache misses there are between actually doing useful work updating active particles. If the array is large and has *lots* of inactive particles in it, we're back to thrashing the cache again.

Having objects in a contiguous array doesn't solve much if the objects we're actually processing aren't contiguous in it. If it's littered with inactive objects we have to dance around, we're right back to the original problem.

**CN:** 我们给 `Particle` 一个标志来跟踪它是否在使用中。在更新循环中，我们对每个粒子检查这个标志。这会把标志连同该粒子的所有其他数据一起加载到缓存中。如果粒子*不*活跃，我们就跳过它到下一个。我们加载到缓存中的粒子其余数据就浪费了。

活跃粒子越少，我们在内存中跳过的次数就越多。做的越多，在更新活跃粒子的有用工作之间就有越多的缓存未命中。如果数组很大且其中有很多不活跃粒子，我们又回到了冲击缓存的老问题。

将对象放在连续数组中并不能解决什么问题，如果我们实际处理的对象在数组中并不是连续的。如果数组中散落着我们必须绕过的非活跃对象，我们又回到了原始问题。

* Given the title of this section, you can probably guess the answer. Instead of *checking* the active flag, we'll *sort* by it. We'll keep all of the active particles in the front of the list. If we know all of those particles are active, we don't have to check the flag at all.

We can also easily keep track of how many active particles there are. With this, our update loop turns into this thing of beauty:

**CN:** 鉴于本节的标题，你可能已经猜到了答案。我们不是*检查*活跃标志，而是按它*排序*。我们将所有活跃粒子保持在前端。如果我们知道所有这些粒子都是活跃的，就根本不需要检查标志。

我们还可以轻松跟踪有多少活跃粒子。这样，我们的更新循环变成了这样优美的代码：

```cpp
for (int i = 0; i < numActive_; i++) {
  particles[i].update();
}
```

* Now we aren't skipping over *any* data. Every byte that gets sucked into the cache is a piece of an active particle that we actually need to process.

Of course, I'm not saying you should quicksort the entire collection of particles every frame. That would more than eliminate the gains here. What we want to do is *keep* the array sorted.

Assuming the array is already sorted — and it is at first when all particles are inactive — the only time it can become *un*sorted is when a particle has been activated or deactivated. We can handle those two cases pretty easily. When a particle gets activated, we move it up to the end of the active particles by swapping it with the first *in*active one:

**CN:** 现在我们不会跳过*任何*数据。被吸入缓存的每个字节都是我们实际需要处理的活跃粒子的一部分。

当然，我不是说每帧都要对整个粒子集合进行快速排序。那会完全抵消这里的收益。我们要做的是*保持*数组有序。

假设数组已经有序——一开始所有粒子都是非活跃的——它可能变得*无序*的唯一时刻是当粒子被激活或停用时。我们可以很容易地处理这两种情况。当粒子被激活时，我们通过将其与第一个*非*活跃粒子交换来把它移到活跃粒子的末尾：

```cpp
void ParticleSystem::activateParticle(int index) {
  // Shouldn't already be active!
  assert(index >= numActive_);

  // Swap it with the first inactive particle
  // right after the active ones.
  Particle temp = particles_[numActive_];
  particles_[numActive_] = particles_[index];
  particles_[index] = temp;

  // Now there's one more.
  numActive_++;
}
```

* To deactivate a particle, we just do the opposite:

* 停用粒子时，我们做相反的操作：

```cpp
void ParticleSystem::deactivateParticle(int index) {
  // Shouldn't already be inactive!
  assert(index < numActive_);

  // There's one fewer.
  numActive_--;

  // Swap it with the last active particle
  // right before the inactive ones.
  Particle temp = particles_[numActive_];
  particles_[numActive_] = particles_[index];
  particles_[index] = temp;
}
```

* Lots of programmers (myself included) have developed allergies to moving things around in memory. Schlepping a bunch of bytes around *feels* heavyweight compared to assigning a pointer. But when you add in the cost of *traversing* that pointer, it turns out that our intuition is sometimes wrong. In some cases, it's cheaper to push things around in memory if it helps you keep the cache full.

This is your friendly reminder to *profile* when making these kinds of decisions.

There's a neat consequence of keeping the particles *sorted* by their active state — we don't need to store an active flag in each particle at all. It can be inferred by its position in the array and the `numActive_` counter. This makes our particle objects smaller, which means we can pack more in our cache lines, and that makes them even faster.

It's not all rosy, though. As you can see from the API, we've lost a bit of object orientation here. The `Particle` class no longer controls its own active state. You can't call some `activate()` method on it since it doesn't know its index. Instead, any code that wants to activate particles needs access to the particle *system*.

In this case, I'm OK with `ParticleSystem` and `Particle` being tightly tied like this. I think of them as a single *concept* spread across two physical *classes*. It just means accepting the idea that particles are *only* meaningful in the context of some particle system. Also, in this case it's likely to be the particle system that will be spawning and killing particles anyway.

**CN:** 很多程序员（包括我）对在内存中移动东西有过敏反应。搬运一堆字节*感觉*上比分配指针要重得多。但当你加上*遍历*该指针的成本时，我们的直觉有时是错误的。在某些情况下，在内存中移动数据更便宜，如果它能帮助保持缓存饱满的话。

这是你的友好提醒：做这类决定时要*做性能分析*。

保持粒子按活跃状态排序有一个巧妙的结果——我们根本不需要在每个粒子中存储活跃标志。可以通过它在数组中的位置和 `numActive_` 计数器推断出来。这使得粒子对象更小，意味着我们可以在缓存行中塞进更多粒子，从而更快。

但并非一切都那么美好。从 API 可以看出，我们失去了一些面向对象特性。`Particle` 类不再控制自己的活跃状态。你不能在它上面调用 `activate()` 方法，因为它不知道自己的索引。相反，任何想要激活粒子的代码都需要访问粒子*系统*。

在这种情况下，我认为 `ParticleSystem` 和 `Particle` 如此紧密耦合是可以接受的。我将它们视为分散在两个物理*类*上的单一*概念*。这只是意味着接受粒子*只有*在某个粒子系统的上下文中才有意义。而且在这种情况下，无论如何都可能是粒子系统来生成和销毁粒子。

### Hot/cold splitting / 热冷分离

* OK, this is the last example of a simple technique for making your cache happier. Say we've got an AI component for some game entity. It has some state in it — the animation it's currently playing, a goal position it's heading towards, energy level, etc. — stuff it checks and tweaks every single frame. Something like:

* 好的，这是最后一个让缓存更开心的简单技术示例。假设我们有一个游戏实体的 AI 组件。它包含一些状态——当前播放的动画、目标位置、能量等级等——每帧都要检查和调整的数据。大致如下：

```cpp
class AIComponent {
public:
  void update() { /* ... */ }

private:
  Animation* animation_;
  double energy_;
  Vector goalPos_;
};
```

* But it also has some state for rarer eventualities. It stores some data describing what loot it drops when it has an unfortunate encounter with the noisy end of a shotgun. That drop data is only used once in the entity's lifetime, right at its bitter end:

* 但它也有一些用于罕见情况的状状态。它存储了一些描述不幸遭遇猎枪嘈杂一端时掉落什么战利品的数数据。这些掉落数据在实体的生命周期中只使用一次，就在它悲惨的终结时刻：

```cpp
class AIComponent {
public:
  void update() { /* ... */ }

private:
  // Previous fields...
  LootType drop_;
  int minDrops_;
  int maxDrops_;
  double chanceOfDrop_;
};
```

* Assuming we followed the earlier patterns, when we update these AI components, we walk through a nice packed, contiguous array of data. But that data includes all of the loot drop information. That makes each component bigger, which reduces the number of components we can fit in a cache line. We get more cache misses because the total memory we walk over is larger. The loot data gets pulled into the cache for every component in every frame, even though we aren't even touching it.

The solution for this is called "hot/cold splitting". The idea is to break our data structure into two separate pieces. The first holds the "hot" data, the state we need to touch every frame. The other piece is the "cold" data, everything else that gets used less frequently.

The hot piece is the *main* AI component. It's the one we need to use the most, so we don't want to chase a pointer to find it. The cold component can be off to the side, but we still need to get to it, so we give the hot component a pointer to it, like so:

**CN:** 假设我们遵循了之前的模式，在更新这些 AI 组件时，遍历一个紧凑连续的数组。但该数据包括了所有掉落信息。这使每个组件变大，减少了我们能在缓存行中容纳的组件数量。由于遍历的总内存更大，会有更多缓存未命中。每帧每个组件的掉落数据都被拉入缓存，即使我们根本不碰它。

这个解决方案称为"热/冷分离"。思路是将数据结构拆分成两个独立部分。第一部分保存"热"数据——每帧都需要访问的状态。另一部分是"冷"数据——其他所有使用频率较低的数据。

热部分是*主要*的 AI 组件。它是我们使用最多的部分，所以我们不想为了找到它而追逐指针。冷组件可以放在旁边，但我们仍然需要访问它，所以给热组件一个指向它的指针：

```cpp
class AIComponent {
public:
  // Methods...
private:
  Animation* animation_;
  double energy_;
  Vector goalPos_;

  LootDrop* loot_;
};

class LootDrop {
  friend class AIComponent;
  LootType drop_;
  int minDrops_;
  int maxDrops_;
  double chanceOfDrop_;
};
```

* Now when we're walking the AI components every frame, the only data that gets loaded into the cache is stuff we are actually processing (with the exception of that one little pointer to the cold data).

We could conceivably ditch the pointer too by having parallel arrays for the hot and cold components. Then we can find the cold AI data for a component since both pieces will be at the same index in their respective arrays.

You can see how this starts to get fuzzy, though. In my example here, it's pretty obvious which data should be hot and cold, but in a real game it's rarely so clear-cut. What if you have fields that are used when an entity is in a certain mode but not in others? What if entities use a certain chunk of data only when they're in certain parts of the level?

Doing this kind of optimization is somewhere between a black art and a rathole. It's easy to get sucked in and spend endless time pushing data around to see what speed difference it makes. It will take practice to get a handle on where to spend your effort.

**CN:** 现在，当每帧遍历 AI 组件时，加载到缓存中的数据只有我们实际在处理的数据（除了那个指向冷数据的小指针）。

我们也可以通过为热数据和冷数据使用并行数组来去掉指针。这样我们可以通过相同索引在各自的数组中找到组件的冷 AI 数据。

但你可以看到这开始变得模糊。在这个例子中，哪些数据是热的、哪些是冷的很明显，但在真实游戏中很少如此清晰。如果某些字段只在实体处于某种模式时才使用呢？如果实体只在处于关卡特定部分时才使用某块数据呢？

做这种优化介于黑色艺术和兔子洞之间。很容易陷入其中，花费无尽的时间移动数据来观察速度差异。需要练习才能掌握在何处投入精力。

## Design Decisions / 设计决策

* This pattern is really about a mindset — it's getting you to think about your data's arrangement in memory as a key part of your game's performance story. The actual concrete design space is wide open. You can let data locality affect your whole architecture, or maybe it's just a localized pattern you apply to a few core data structures.

The biggest questions you'll need to answer are when and where you apply this pattern, but here are a couple of others that may come up.

**CN:** 这个模式实际上是一种思维方式——让你将数据在内存中的排列视为游戏性能的关键部分。实际的具体设计空间非常宽广。你可以让数据局部性影响整个架构，或者它可能只是应用于少数核心数据结构的局部模式。

你需要回答的最大问题是在何时何地应用此模式，但这里还有一些其他可能出现的问题。

### How do you handle polymorphism? / 如何处理多态？

* Up to this point, we've avoided subclassing and virtual methods. We have assumed we have nice packed arrays of *homogenous* objects. That way, we know they're all the exact same size. But polymorphism and dynamic dispatch are useful tools too. How do we reconcile this?

* 到目前为止，我们避免了子类化和虚方法。我们假设有整齐打包的*同质*对象数组。这样我们知道它们的大小完全相同。但多态和动态分发也是有用的工具。我们如何调和这一点？

* **Don't:**

The simplest answer is to avoid subclassing, or at least avoid it in places where you're optimizing for cache usage. Software engineer culture is drifting away from heavy use of inheritance anyway.

One way to keep much of the flexibility of polymorphism without using subclassing is through the [Type Object](type-object.html) pattern.

- *It's safe and easy.* You know exactly what class you're dealing with, and all objects are obviously the same size.
- *It's faster.* Dynamic dispatch means looking up the method in the vtable and then traversing that pointer to get to the actual code. While the cost of this varies widely across different hardware, there is *some* cost to dynamic dispatch.
- *It's inflexible.* Of course, the reason we use dynamic dispatch is because it gives us a powerful way to vary behavior between objects. If you want different entities in your game to have their own rendering styles or their own special moves and attacks, virtual methods are a proven way to model that. Having to instead stuff all of that code into a single non-virtual method that does something like a big `switch` gets messy quickly.

**CN:** **不使用多态：**

最简单的答案是避免子类化，或者至少在为缓存使用进行优化的地方避免它。软件工程文化无论如何正在远离大量使用继承。

不使用子类化而保持多态灵活性的一个方法是通过[类型对象](type-object.html)模式。

- *安全且简单。*你确切知道处理的是什么类，所有对象大小明显相同。
- *更快。*动态分发意味着在 vtable 中查找方法，然后遍历指针以到达实际代码。虽然不同硬件的成本差异很大，但动态分发确实有*一些*成本。
- *不灵活。*当然，我们使用动态分发是因为它提供了在对象之间变化行为的强大方式。如果你希望游戏中的不同实体拥有自己的渲染风格或特殊招式，虚方法是一种行之有效的建模方式。而将所有这些代码塞进一个做大型 `switch` 的非虚方法会迅速变得混乱。

* **Use separate arrays for each type:**

We use polymorphism so that we can invoke behavior on an object whose type we don't know. In other words, we have a mixed bag of stuff, and we want each object in there to do its own thing when we tell it to go.

But that raises the question of why mix the bag to begin with? Instead, why not maintain separate, homogenous collections for each type?

- *It keeps objects tightly packed.* Since each array only contains objects of one class, there's no padding or other weirdness.
- *You can statically dispatch.* Once you've got objects partitioned by type, you don't need polymorphism at all any more. You can use regular, non-virtual method calls.
- *You have to keep track of a bunch of collections.* If you have a lot of different object types, the overhead and complexity of maintaining separate arrays for each can be a chore.
- *You have to be aware of every type.* Since you have to maintain separate collections for each type, you can't be decoupled from the *set* of classes. Part of the magic of polymorphism is that it's *open-ended* — code that works with an interface can be completely decoupled from the potentially large set of types that implement that interface.

**CN:** **为每种类型使用独立数组：**

我们使用多态是为了能在类型未知的对象上调用行为。换句话说，我们有一个混合的袋子，希望袋子里的每个对象在收到指令时做自己的事。

但这引出了一个问题：一开始为什么要把袋子混在一起？相反，为什么不每种类型维护独立的同质集合？

- *保持对象紧凑打包。*由于每个数组只包含一个类的对象，没有填充或其他怪异。
- *可以静态分发。*一旦按类型分区对象，就根本不再需要多态了。可以使用常规的非虚方法调用。
- *需要跟踪一堆集合。*如果你有很多不同的对象类型，为每种维护独立数组的开销和复杂性可能成为负担。
- *必须了解每种类型。*由于必须为每种类型维护独立集合，你无法与类*集合*解耦。多态的部分魔力在于它是*开放式的*——处理接口的代码可以与实现该接口的潜在大量类型完全解耦。

* **Use a collection of pointers:**

If you weren't worried about caching, this is the natural solution. Just have an array of pointers to some base class or interface type. You get all the polymorphism you could want, and objects can be whatever size they want.

- *It's flexible.* The code that consumes the collection can work with objects of any type as long as it supports the interface you care about. It's completely open-ended.
- *It's less cache-friendly.* Of course, the whole reason we're discussing other options here is because this means cache-unfriendly pointer indirection. But, remember, if this code isn't performance-critical, that's probably OK.

**CN:** **使用指针集合：**

如果你不担心缓存，这是自然的解决方案。只需一个指向基类或接口类型的指针数组。你可以得到想要的所有多态性，对象可以是任意大小。

- *灵活。*使用该集合的代码可以处理任何类型的对象，只要它支持你关心的接口。完全开放。
- *缓存不友好。*当然，我们在此讨论其他选项的全部原因是因为这意味着缓存不友好的指针间接访问。但记住，如果这段代码不是性能关键的，那可能没问题。

### How are game entities defined? / 如何定义游戏实体？

* If you use this pattern in tandem with the [Component](component.html) pattern, you'll have nice contiguous arrays for all of the components that make up your game entities. The game loop will be iterating over those directly, so the object for the game entity itself is less important, but it's still useful in other parts of the codebase where you want to work with a single conceptual "entity".

The question then is how should it be represented? How does it keep track of its components?

**CN:** 如果你将这个模式与[组件](component.html)模式配合使用，你将获得构成游戏实体的所有组件的漂亮连续数组。游戏循环将直接迭代这些数组，所以游戏实体本身的对象不那么重要了，但在代码库的其他部分，当你想要处理单个概念"实体"时，它仍然有用。

那么问题来了：它应该如何表示？它如何跟踪自己的组件？

* **If game entities are classes with pointers to their components:**

This is what our first example looked like. It's sort of the vanilla OOP solution. You've got a class for `GameEntity`, and it has pointers to the components it owns. Since they're just pointers, it's agnostic about where and how those components are organized in memory.

- *You can store components in contiguous arrays.* Since the game entity doesn't care where its components are, you can organize them in a nice packed array to optimize iterating over them.
- *Given an entity, you can easily get to its components.* They're just a pointer indirection away.
- *Moving components in memory is hard.* When components get enabled or disabled, you may want to move them around in the array to keep the active ones up front and contiguous. If you move a component while the entity has a raw pointer to it, though, that pointer gets broken if you aren't careful. You'll have to make sure to update the entity's pointer at the same time.

**CN:** **如果游戏实体是持有指向组件的指针的类：**

这就是我们第一个示例的样子。算是朴素的 OOP 方案。你有一个 `GameEntity` 类，它持有指向其拥有的组件的指针。由于它们只是指针，它不在意这些组件在内存中的位置和方式。

- *可以将组件存储在连续数组中。*因为游戏实体不关心组件在哪里，你可以将它们组织成漂亮的紧凑数组，以优化迭代访问。
- *给定实体，可以轻松访问其组件。*只需一次指针间接访问。
- *在内存中移动组件困难。*当组件被启用或禁用时，你可能希望在数组中移动它们以保持活跃组件在前面且连续。但是如果在实体持有原始指针时移动了组件，指针就会被破坏。你必须同时更新实体的指针。

* **If game entities are classes with IDs for their components:**

The challenge with raw pointers to components is that it makes it harder to move them around in memory. You can address that by using something more abstract: an ID or index that can be used to *look up* a component.

The actual semantics of the ID and lookup process are up to you. It could be as simple as storing a unique ID in each component and walking the array, or more complex like a hash table that maps IDs to their current index in the component array.

- *It's more complex.* Your ID system doesn't have to be rocket science, but it's still more work than a basic pointer. You'll have to implement and debug it, and there will be memory overhead for bookkeeping.
- *It's slower.* It's hard to beat traversing a raw pointer. There may be some searching or hashing involved to get from an entity to one of its components.
- *You'll need access to the component "manager".* The basic idea is that you have some abstract ID that identifies a component. You can use it to get a reference to the actual component object. But to do that, you need to hand that ID to something that can actually find the component. That will be the class that wraps your raw contiguous array of component objects.

With raw pointers, if you have a game entity, you can find its components. With this, you need the game entity *and the component registry too*.

**CN:** **如果游戏实体是使用 ID 引用其组件的类：**

使用原始指针指向组件的挑战在于这使得在内存中移动它们更加困难。你可以使用更抽象的东西来解决这个问题：一个可以用来*查找*组件的 ID 或索引。

ID 和查找过程的实际语义由你决定。它可以像在每个组件中存储唯一 ID 然后遍历数组一样简单，或者更复杂如使用哈希表将 ID 映射到它们在组件数组中的当前索引。

- *更复杂。*你的 ID 系统不必是火箭科学，但仍然比基本指针更多工作。你必须实现和调试它，并且会有记账的内存开销。
- *更慢。*很难比直接遍历原始指针更快。从实体到其组件可能涉及一些搜索或哈希。
- *需要访问组件"管理器"。*基本思路是你有一个标识组件的抽象 ID，用它来获取实际组件对象的引用。但要做到这一点，你需要将该 ID 交给能实际找到组件的东西。那就是封装你的原始连续组件对象数组的类。

有了原始指针，如果你有一个游戏实体，你可以找到它的组件。有了这种方式，你需要游戏实体*以及组件注册表*。

* **If the game entity is *itself* just an ID:**

This is a newer style that some game engines use. Once you've moved all of your entity's behavior and state out of the main class and into components, what's left? It turns out, not much. The only thing an entity does is bind a set of components together. It exists just to say *this* AI component and *this* physics component and *this* render component define one living entity in the world.

That's important because components interact. The render component needs to know where the entity is, which may be a property of the physics component. The AI component wants to move the entity, so it needs to apply a force to the physics component. Each component needs a way to get the other sibling components of the entity it's a part of.

Some smart people realized all you need for that is an ID. Instead of the entity knowing its components, the components know their entity. Each component knows the ID of the entity that owns it. When the AI component needs the physics component for its entity, it simply asks for the physics component with the same entity ID that it holds.

Your entity *classes* disappear entirely, replaced by a glorified wrapper around a number.

- *Entities are tiny.* When you want to pass around a reference to a game entity, it's just a single value.
- *Entities are empty.* Of course, the downside of moving everything out of entities is that you *have* to move everything out of entities. You no longer have a place to put non-component-specific state or behavior. This style doubles down on the [Component](component.html) pattern.
- *You don't have to manage their lifetime.* Since entities are just dumb value types, they don't need to be explicitly allocated and freed. An entity implicitly "dies" when all of its components are destroyed.
- *Looking up a component for an entity may be slow.* This is the same problem as the previous answer, but in the opposite direction. To find a component for some entity, you have to map an ID to an object. That process may be costly.

This time, though, it *is* performance-critical. Components often interact with their siblings during update, so you will need to find components frequently. One solution is to make the "ID" of an entity the index of the component in its array.

If every entity has the same set of components, then your component arrays are completely parallel. The component in slot three of the AI component array will be for the same entity that the physics component in slot three of *its* array is associated with.

Keep in mind, though, that this *forces* you to keep those arrays in parallel. That's hard if you want to start sorting or packing them by different criteria. You may have some entities with disabled physics and others that are invisible. There's no way to sort the physics and render component arrays optimally for both cases if they have to stay in sync with each other.

**CN:** **如果游戏实体*本身*只是一个 ID：**

这是一些游戏引擎使用的较新风格。一旦你将实体的所有行为和状态移出主类放入组件中，还剩下什么？结果发现不多了。实体唯一做的事情是将一组组件绑定在一起。它的存在只是说*这个* AI 组件、*这个*物理组件和*这个*渲染组件定义了世界中的一个活实体。

这很重要，因为组件之间会交互。渲染组件需要知道实体的位置，这可能是物理组件的属性。AI 组件想要移动实体，所以它需要向物理组件施加力。每个组件都需要一种方式来获取它所属实体的其他兄弟组件。

一些聪明人意识到你需要的只是一个 ID。不是实体知道它的组件，而是组件知道它们的实体。每个组件都知道拥有它的实体的 ID。当 AI 组件需要其对应实体的物理组件时，它只需请求具有相同实体 ID 的物理组件。

你的实体*类*完全消失了，被一个美化的数字包装器取代。

- *实体很小。*当你想传递游戏实体的引用时，它只是一个单一值。
- *实体是空的。*当然，把所有东西都移出实体的缺点是你*必须*把所有东西都移出实体。你不再有放置非组件特定状态或行为的地方。这种风格加倍押注于[组件](component.html)模式。
- *不需要管理它们的生命周期。*由于实体只是简单的值类型，它们不需要显式分配和释放。当所有组件被销毁时，实体隐式"死亡"。
- *为实体查找组件可能很慢。*这与前一个答案相同的问题，但方向相反。为某个实体查找组件，你必须将 ID 映射到对象。这个过程可能代价高昂。

但这一次，它*是*性能关键的。组件在更新期间经常与兄弟组件交互，所以你需要频繁查找组件。一个解决方案是让实体的"ID"就是组件在其数组中的索引。

如果每个实体都有相同的组件集，那么你的组件数组是完全并行的。AI 组件数组槽位三中的组件与*其*数组槽位三中的物理组件关联的是同一个实体。

但记住，这*强制*你保持这些数组并行。如果你想根据不同标准对它们进行排序或打包，这很困难。你可能有些实体禁用了物理，有些不可见。如果物理和渲染组件数组必须彼此保持同步，就没有办法同时为两者优化排序。

## See Also / 参见

- Much of this chapter revolves around the [Component](component.html) pattern, and that pattern is definitely one of the most common data structures that gets optimized for cache usage. In fact, using the Component pattern makes this optimization easier. Since entities are updated one "domain" (AI, physics, etc.) at a time, splitting them out into components lets you slice a bunch of entities into the right pieces to be cache-friendly.
- But that doesn't mean you can *only* use this pattern with components! Any time you have performance-critical code that touches a lot of data, it's important to think about locality.
- Tony Albrecht's ["Pitfalls of Object-Oriented Programming"](http://research.scee.net/files/presentations/gcapaustralia09/Pitfalls_of_Object_Oriented_Programming_GCAP_09.pdf) is probably the most widely-read introduction to designing your game's data structures for cache-friendliness. It made a lot more people (including me!) aware of how big of a deal this is for performance.
- Around the same time, Noel Llopis wrote a [very influential blog post](http://gamesfromwithin.com/data-oriented-design) on the same topic.
- This pattern almost invariably takes advantage of a contiguous array of homogenous objects. Over time, you'll very likely be adding and removing objects from that array. The [Object Pool](object-pool.html) pattern is about exactly that.
- The [Artemis](http://gamadu.com/artemis/) game engine is one of the first and better-known frameworks that uses simple IDs for game entities.

- 本章大部分内容围绕[组件](component.html)模式，该模式绝对是最常见的为缓存使用优化的数据结构之一。事实上，使用组件模式使这种优化更容易。由于实体一次更新一个"领域"（AI、物理等），将它们拆分成组件让你将一批实体切成适合缓存友好的合适片段。
- 但这并不意味着你*只能*在组件中使用此模式！任何时候你有涉及大量数据的性能关键代码，都要考虑局部性。
- Tony Albrecht 的 ["Pitfalls of Object-Oriented Programming"](http://research.scee.net/files/presentations/gcapaustralia09/Pitfalls_of_Object_Oriented_Programming_GCAP_09.pdf) 可能是关于设计缓存友好的游戏数据结构最广为阅读的介绍。它让更多人（包括我！）意识到这对性能来说多么重要。
- 大约同一时间，Noel Llopis 在相同主题上写了一篇[非常有影响力的博文](http://gamesfromwithin.com/data-oriented-design)。
- 这个模式几乎总是利用同质对象的连续数组。随着时间的推移，你很可能会从数组中添加和移除对象。[对象池](object-pool.html)模式正是关于这个的。
- [Artemis](http://gamadu.com/artemis/) 游戏引擎是最早且最著名的使用简单 ID 表示游戏实体的框架之一。
---

## C# Equivalent (C# 对照实现)

```csharp
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

/// <summary>
/// 数据局部性模式 C# 实现
/// 演示如何使用连续数组和 Unity Job System 优化性能
/// </summary>

// —— 组件定义 ——
// 使用 struct 而非 class，因为 struct 是值类型，存储在连续内存中
// 而 class 是引用类型，存储在堆上，访问时需要指针间接跳转

public struct AIComponent
{
    // 使用固定数组还是单个字段取决于数据量
    // 对于频繁访问的数据，使用值类型避免指针间接访问
    public float energy;
    public Vector3 goalPosition;
    // 注意：这里不使用 class 类型的字段，因为它们会引入指针
    // 如果必须使用引用类型，考虑使用索引 ID 替代直接引用
}

public struct PhysicsComponent
{
    public Vector3 position;
    public Vector3 velocity;
    public float mass;

    public void Update(float deltaTime)
    {
        // 简单的物理更新
        position += velocity * deltaTime;
    }
}

public struct RenderComponent
{
    public Mesh mesh;
    public Material material;
    // 使用索引引用资源，避免在热路径中存储直接引用
}

// —— 实体管理器 ——
// 这个类管理所有实体的组件，使用并行数组存储
// 这样做的好处是：当需要更新所有物理组件时，
// CPU 可以从 RAM 中连续读取数据到缓存行

public class EntityManager
{
    // 使用 NativeArray 而非普通数组
    // NativeArray 在非托管内存中分配，适合 Job System 使用
    // 且不会被 GC 移动，保证了内存布局的确定性
    public NativeArray<AIComponent> aiComponents;
    public NativeArray<PhysicsComponent> physicsComponents;
    public NativeArray<RenderComponent> renderComponents;

    private const int MAX_ENTITIES = 10000;

    public EntityManager()
    {
        // NativeArray 分配在非托管堆上，不受 GC 管理
        // 这保证了内存位置的稳定性，不会因 GC 压缩而移动
        aiComponents = new NativeArray<AIComponent>(
            MAX_ENTITIES, Allocator.Persistent);
        physicsComponents = new NativeArray<PhysicsComponent>(
            MAX_ENTITIES, Allocator.Persistent);
        renderComponents = new NativeArray<RenderComponent>(
            MAX_ENTITIES, Allocator.Persistent);
    }

    public void Dispose()
    {
        // 必须手动释放 NativeArray 以避免内存泄漏
        if (aiComponents.IsCreated) aiComponents.Dispose();
        if (physicsComponents.IsCreated) physicsComponents.Dispose();
        if (renderComponents.IsCreated) renderComponents.Dispose();
    }

    // —— 热/冷分离模式 ——
    // "Hot" 数据是每帧都需要访问的字段
    // "Cold" 数据是偶尔才需要的字段（如掉落物品信息）
    // 通过分离它们，缓存行中只有热数据，提高了缓存效率

    public struct HotAIData
    {
        // 每帧都需要访问的数据
        public float energy;
        public Vector3 goalPosition;
    }

    public struct ColdAIData
    {
        // 仅在实体死亡时使用的数据
        public LootType dropType;
        public int minDrops;
        public int maxDrops;
        public float dropChance;
    }

    public enum LootType
    {
        Gold,
        Weapon,
        Potion
    }
}

// —— 使用 IJobParallelFor 进行并行处理 ——
// 这是 Unity Job System 的核心接口
// 它将工作分配到多个工作线程上，利用多核 CPU
// 且不会引发线程竞争问题，因为每个 Job 处理不同的索引

[BurstCompile]
public struct PhysicsUpdateJob : IJobParallelFor
{
    // 使用 NativeArray 才能在 Job 中安全访问
    // 普通的 C# 数组无法保证线程安全
    public NativeArray<PhysicsComponent> physicsComponents;
    public float deltaTime;

    // IJobParallelFor 会自动将工作分配到多个线程
    // 每个线程处理一段连续的索引范围
    // 这保证了数据局部性：每个线程处理的数据在内存中是连续的
    public void Execute(int index)
    {
        PhysicsComponent comp = physicsComponents[index];
        comp.Update(deltaTime);
        physicsComponents[index] = comp;
        // 注意：struct 是值类型，修改后必须写回
    }
}

// —— MonoBahaviour 使用示例 ——
public class DataLocalityDemo : MonoBehaviour
{
    private EntityManager entityManager;
    private JobHandle physicsJobHandle;

    void Start()
    {
        entityManager = new EntityManager();

        // 初始化组件数据
        for (int i = 0; i < EntityManager.MAX_ENTITIES; i++)
        {
            entityManager.physicsComponents[i] = new PhysicsComponent
            {
                position = Random.insideUnitSphere * 10f,
                velocity = Random.insideUnitSphere * 2f,
                mass = 1f
            };
        }
    }

    void Update()
    {
        // —— 对比：不使用 Job System 的更新 ——
        // 这个循环虽然也是连续内存遍历，但只在主线程执行
        // 无法利用多核 CPU 的全部性能
        for (int i = 0; i < EntityManager.MAX_ENTITIES; i++)
        {
            PhysicsComponent comp = entityManager.physicsComponents[i];
            comp.Update(Time.deltaTime);
            entityManager.physicsComponents[i] = comp;
        }

        // —— 使用 Job System + Burst 编译器的优化版本 ——
        // 1. Job System：自动分配到多个工作线程
        // 2. Burst Compiler：将 C# 编译为高度优化的原生代码
        //    利用 SIMD 指令批量处理数据
        // 3. NativeArray 的连续内存：确保每个线程处理的
        //    数据段都在相邻的缓存行中
        var physicsJob = new PhysicsUpdateJob
        {
            physicsComponents = entityManager.physicsComponents,
            deltaTime = Time.deltaTime
        };

        // 将工作分割到多个批次，每个批次处理 64 个元素
        // 这样的粒度既保证了负载均衡，又保持了数据局部性
        physicsJobHandle = physicsJob.Schedule(
            EntityManager.MAX_ENTITIES, 64);

        // 等待完成（实际项目中应使用 JobHandle 依赖链）
        physicsJobHandle.Complete();
    }

    void OnDestroy()
    {
        entityManager?.Dispose();
    }
}
```

## Unity Application / Unity 应用场景

1. **Unity ECS (Entities-Component-System)**: The entire ECS architecture is built on Data Locality. Components are stored in contiguous `NativeArray`s called "chunks". When iterating, you process data sequentially in memory.
2. **Job System**: `IJobParallelFor` splits work across threads, each processing a contiguous slice of `NativeArray`. This ensures per-thread data locality.
3. **Burst Compiler**: Generates highly optimized SIMD instructions from the data-parallel code, exploiting both cache locality and vectorization.
4. **Particle Systems**: Storing particle positions, velocities, and lifetimes in separate contiguous arrays (Structure of Arrays) rather than in a single array of structs (Array of Structures).
5. **Mesh Data**: Accessing vertex positions, normals, and UVs in separate arrays rather than interleaved when processing only one attribute.

1. **Unity ECS (实体-组件-系统)**: 整个 ECS 架构建立在数据局部性之上。组件存储在称为"块（Chunk）"的连续 `NativeArray` 中。迭代时，按内存顺序依次处理数据。
2. **Job System**: `IJobParallelFor` 将工作分配到多个线程，每个线程处理 `NativeArray` 的连续片段，保证了线程内的数据局部性。
3. **Burst 编译器**: 将数据并行代码编译为高度优化的 SIMD 指令，同时利用缓存局部性和向量化。
4. **粒子系统**: 将粒子的位置、速度和生命周期分别存储在独立的连续数组中（结构体数组 SoA），而非存储在单个结构体数组中（数组结构体 AoS）。
5. **网格数据**: 当只处理一个属性时，分别访问顶点位置、法线和 UV 数组，而非交错访问。
## Key Differences / 关键差异

| Aspect | C++ (原书) | C# (Unity) |
|--------|-----------|------------|
| Memory control | Manual new/delete | `NativeArray<T>` with `Allocator` |
| Parallelism | Manual threading | `IJobParallelFor` auto-scheduling |
| Compiler optimization | Compiler-dependent | Burst compiler + SIMD auto-vectorization |
| Array type | Raw pointers `T*` | `NativeArray<T>`, `NativeSlice<T>` |
| Ownership | Manual management | `Dispose()` required, ref-counting support |
| Data layout choice | AoS vs SoA manual | ECS automatically uses SoA in chunks |
| GC impact | None (native) | NativeArray on unmanaged heap, no GC |

---

# Dirty Flag — 脏标记

> **EN:** Avoid unnecessary work by deferring it until the result is needed.
> **CN:** 将工作推迟到真正需要其结果时才执行，从而避免不必要的计算。

## Intent / 意图

* Avoid unnecessary work by deferring it until the result is needed.

* 将工作推迟到真正需要其结果时才执行，从而避免不必要的计算。

## Motivation / 动机

* Many games have something called a *scene graph*. This is a big data structure that contains all of the objects in the world. The rendering engine uses this to determine where to draw stuff on the screen.

At its simplest, a scene graph is just a flat list of objects. Each object has a model, or some other graphic primitive, and a *transform*. The transform describes the object's position, rotation, and scale in the world. To move or turn an object, we simply change its transform.

The mechanics of *how* this transform is stored and manipulated are unfortunately out of scope here. The comically abbreviated summary is that it's a 4x4 matrix. You can make a single transform that combines two transforms — for example, translating and then rotating an object — by multiplying the two matrices.

How and why that works is left as an exercise for the reader.

When the renderer draws an object, it takes the object's model, applies the transform to it, and then renders it there in the world. If we had a scene *bag* and not a scene *graph*, that would be it, and life would be simple.

However, most scene graphs are *hierarchical*. An object in the graph may have a parent object that it is anchored to. In that case, its transform is relative to the *parent's* position and isn't an absolute position in the world.

For example, imagine our game world has a pirate ship at sea. Atop the ship's mast is a crow's nest. Hunched in that crow's nest is a pirate. Clutching the pirate's shoulder is a parrot. The ship's local transform positions the ship in the sea. The crow's nest's transform positions the nest on the ship, and so on.

![A pirate ship containing a crow's nest with a pirate in it with a parrot on his shoulder.](images/dirty-flag-pirate.png)

Programmer art!

This way, when a parent object moves, its children move with it automatically. If we change the local transform of the ship, the crow's nest, pirate, and parrot go along for the ride. It would be a total headache if, when the ship moved, we had to manually adjust the transforms of all the objects on it to keep them from sliding off.

To be honest, when you are at sea you *do* have to keep manually adjusting your position to keep from sliding off. Maybe I should have chosen a drier example.

But to actually draw the parrot on screen, we need to know its absolute position in the world. I'll call the parent-relative transform the object's *local transform*. To render an object, we need to know its *world transform*.

**CN:** 许多游戏都有所谓的*场景图（scene graph）*。这是一个包含世界中所有对象的大型数据结构。渲染引擎用它来决定在屏幕的何处绘制内容。

在最简单的情况下，场景图只是一个对象的平面列表。每个对象都有一个模型（或其他图形基元）和一个*变换（transform）*。变换描述了对象在世界中的位置、旋转和缩放。要移动或旋转对象，我们只需改变它的变换。

关于变换*如何*存储和操作的机制，很遗憾超出了本文的范围。一个极其简略的总结是：它是一个 4×4 矩阵。你可以通过矩阵乘法将两个变换组合成一个变换——例如，先平移再旋转一个对象。

至于这如何实现以及为何有效，留给读者作为练习。

当渲染器绘制对象时，它获取对象的模型，应用变换，然后将其渲染到世界中的相应位置。如果我们拥有的是场景*袋*而不是场景*图*，那就到此为止了，生活会很简单。

然而，大多数场景图是*层级结构*的。图中的对象可能有一个它所依附的父对象。在这种情况下，它的变换是相对于*父对象*位置的，而不是世界中的绝对位置。

例如，想象我们的游戏世界有一艘在海上的海盗船。船的桅杆顶部有一个瞭望台。瞭望台里蹲着一个海盗。海盗的肩膀上抓着一只鹦鹉。船的局部变换将船定位在海上。瞭望台的变换将瞭望台定位在船上，以此类推。

这样一来，当父对象移动时，它的子对象会自动随之移动。如果我们改变船的局部变换，瞭望台、海盗和鹦鹉都会跟着移动。如果船移动时，我们必须手动调整船上所有对象的变换来防止它们滑落，那将是一场彻底的噩梦。

老实说，在海上你*确实*需要不断手动调整位置以防止滑落。也许我应该选一个更干燥的例子。

但要真正在屏幕上绘制鹦鹉，我们需要知道它在世界中的绝对位置。我将父对象相对变换称为对象的*局部变换（local transform）*。要渲染一个对象，我们需要知道它的*全局变换（world transform）*。

### Local and World Transforms / 局部与全局变换

* Calculating an object's world transform is pretty straightforward — you just walk its parent chain starting at the root all the way down to the object, combining transforms as you go. In other words, the parrot's world transform is:

![The parrot's world position comes from multiplying the local positions for the ship, nest, pirate, and parrot.](images/dirty-flag-multiply.png)

In the degenerate case where the object has no parent, its local and world transforms are equivalent.

We need the world transform for every object in the world every frame, so even though there are only a handful of matrix multiplications per model, it's on the hot code path where performance is critical. Keeping them up to date is tricky because when a parent object moves, that affects the world transform of itself and all of its children, recursively.

The simplest approach is to calculate transforms on the fly while rendering. Each frame, we recursively traverse the scene graph starting at the top of the hierarchy. For each object, we calculate its world transform right then and draw it.

But this is terribly wasteful of our precious CPU juice! Many objects in the world are *not* moving every frame. Think of all of the static geometry that makes up the level. Recalculating their world transforms each frame even though they haven't changed is a waste.

**CN:** 计算对象的全局变换相当直接——你只需要从根节点开始沿着父链向下遍历到对象，沿途组合变换即可。换句话说，鹦鹉的全局变换是：

船的位置 × 瞭望台的位置 × 海盗的位置 × 鹦鹉的位置

在退化情况下（对象没有父节点），它的局部变换和全局变换是等价的。

我们每帧都需要世界中每个对象的全局变换，所以即使每个模型只需要少量的矩阵乘法，它也处于对性能至关重要的热代码路径上。保持它们的最新状态很棘手，因为当父对象移动时，会递归地影响自身及其所有子对象的全局变换。

最简单的方法是在渲染时即时计算变换。每帧，我们从层级顶部开始递归遍历场景图。对于每个对象，我们在此时计算它的全局变换并绘制它。

但这对我们宝贵的 CPU 资源来说是一种可怕的浪费！世界中的许多对象并*不是*每帧都在移动。想想构成关卡的所有静态几何体。即使它们没有变化，每帧重新计算它们的全局变换是一种浪费。

### Cached World Transforms / 缓存的全局变换

* The obvious answer is to *cache* it. In each object, we store its local transform and its derived world transform. When we render, we only use the precalculated world transform. If the object never moves, the cached transform is always up to date and everything's happy.

When an object *does* move, the simple approach is to refresh its world transform right then. But don't forget the hierarchy! When a parent moves, we have to recalculate its world transform *and all of its children's, recursively*.

Imagine some busy gameplay. In a single frame, the ship gets tossed on the ocean, the crow's nest rocks in the wind, the pirate leans to the edge, and the parrot hops onto his head. We changed four local transforms. If we recalculate world transforms eagerly whenever a local transform changes, what ends up happening?

![Any time an object moves, the world coordinates are recalculated eagerly and redundantly.](images/dirty-flag-update-bad.png)

You can see on the lines marked ★ that we're recalculating the parrot's world transform *four* times when we only need the result of the final one.

We only moved four objects, but we did *ten* world transform calculations. That's six pointless calculations that get thrown out before they are ever used by the renderer. We calculated the parrot's world transform *four* times, but it is only rendered once.

The problem is that a world transform may depend on several local transforms. Since we recalculate immediately each time *one* of the transforms changes, we end up recalculating the same transform multiple times when more than one of the local transforms it depends on changes in the same frame.

**CN:** 显而易见的答案是*缓存*它。在每个对象中，我们存储它的局部变换和推导出的全局变换。渲染时，我们只使用预计算的全局变换。如果对象从未移动，缓存的变换始终是最新的，一切都很美好。

当一个对象*确实*移动了，简单的方法就是立即刷新它的全局变换。但别忘了层级结构！当父节点移动时，我们必须重新计算它的全局变换，*以及递归地重新计算所有子节点的全局变换*。

想象一下繁忙的游戏过程。在一帧内，船在海浪中颠簸，瞭望台在风中摇晃，海盗向边缘倾斜，鹦鹉跳到了他头上。我们改变了四个局部变换。如果每当局部变换变化时我们就急切地重新计算全局变换，会发生什么？

你可以在标记 ★ 的行上看到，我们重新计算了鹦鹉的全局变换*四次*，而实际上我们只需要最后一次的结果。

我们只移动了四个对象，却执行了*十次*全局变换计算。其中有六次无意义的计算在被渲染器使用之前就被丢弃了。我们计算了鹦鹉的全局变换*四次*，但它只被渲染一次。

问题在于全局变换可能依赖于多个局部变换。由于我们在每次*某一个*变换变化时立即重新计算，当它所依赖的多个局部变换在同一帧中变化时，我们最终会多次重新计算同一个变换。

### Deferred Recalculation / 延迟重新计算

* We'll solve this by decoupling changing local transforms from updating the world transforms. This lets us change a bunch of local transforms in a single batch and *then* recalculate the affected world transform just once after all of those modifications are done, right before we need it to render.

It's interesting how much of software architecture is intentionally engineering a little slippage.

To do this, we add a *flag* to each object in the graph. "Flag" and "bit" are synonymous in programming — they both mean a single micron of data that can be in one of two states. We call those "true" and "false", or sometimes "set" and "cleared". I'll use all of these interchangeably.

When the local transform changes, we set it. When we need the object's world transform, we check the flag. If it's set, we calculate the world transform and then clear the flag. The flag represents, "Is the world transform out of date?" For reasons that aren't entirely clear, the traditional name for this "out-of-date-ness" is "dirty". Hence: *a dirty flag*. "Dirty bit" is an equally common name for this pattern, but I figured I'd stick with the name that didn't seem as prurient.

If we apply this pattern and then move all of the objects in our previous example, the game ends up doing:

![By deferring until all moves are done, we only recalculate once.](images/dirty-flag-update-good.png)

That's the best you could hope to do — the world transform for each affected object is calculated exactly once. With only a single bit of data, this pattern does a few things for us:

- It collapses modifications to multiple local transforms along an object's parent chain into a single recalculation on the object.
- It avoids recalculation on objects that didn't move.
- And a minor bonus: if an object gets removed before it's rendered, it doesn't calculate its world transform at all.

**CN:** 我们将通过解耦局部变换的修改和全局变换的更新来解决这个问题。这使我们能够批量修改多个局部变换，然后在这些修改全部完成后，在需要渲染之前，只重新计算一次受影响的全局变换。

有趣的是，软件架构中有相当一部分是有意设计一些"松动空间"。

为此，我们为图中的每个对象添加一个*标记（flag）*。在编程中，"flag"和"bit"是同义词——它们都表示一个可以处于两种状态之一的微小数据单元。我们称它们为"true"和"false"，或者有时称为"set"和"cleared"。我将互换使用这些术语。

当局部变换改变时，我们设置这个标记。当我们需要对象的全局变换时，我们检查这个标记。如果标记被设置了，我们就计算全局变换，然后清除标记。这个标记代表："全局变换是否已过时？"出于不完全清楚的原因，"过时"的传统名称是"脏（dirty）"。因此得名：*脏标记（dirty flag）*。"脏位（dirty bit）"也是同样常见的名称，但我认为还是坚持使用这个不那么暧昧的名字吧。

如果我们应用这个模式，然后移动前面示例中的所有对象，游戏最终会这样执行：

通过将所有移动都完成后才处理，每个受影响的对象的全局变换只被计算一次。这就是你能期望的最佳结果——每个受影响的对象的全局变换恰好被计算一次。仅用单个比特的数据，这个模式为我们做了几件事：

- 它将沿对象父链对多个局部变换的修改折叠为对对象的一次重新计算。
- 它避免了对未移动对象的重新计算。
- 还有一个次要好处：如果一个对象在渲染之前被移除了，它根本不会计算其全局变换。

## The Pattern / 模式定义

* A set of **primary data** changes over time. A set of **derived data** is determined from this using some **expensive process**. A **"dirty" flag** tracks when the derived data is out of sync with the primary data. It is **set when the primary data changes**. If the flag is set when the derived data is needed, then **it is reprocessed and the flag is cleared.** Otherwise, the previous **cached derived data** is used.

* 一组**主数据**随时间变化。一组**派生数据**通过某些**昂贵的过程**从主数据中确定。一个**"脏"标记**跟踪派生数据何时与主数据不同步。它在**主数据变化时被设置**。如果在需要派生数据时标记被设置，则**重新处理派生数据并清除标记**。否则，使用之前**缓存的派生数据**。

## When to Use It / 适用场景

* Compared to some other patterns in this book, this one solves a pretty specific problem. Also, like most optimizations, you should only reach for it when you have a performance problem big enough to justify the added code complexity.

Dirty flags are applied to two kinds of work: *calculation* and *synchronization*. In both cases, the process of going from the primary data to the derived data is time-consuming or otherwise costly.

In our scene graph example, the process is slow because of the amount of math to perform. When using this pattern for synchronization, on the other hand, it's more often that the derived data is *somewhere else* — either on disk or over the network on another machine — and simply getting it from point A to point B is what's expensive.

There are a couple of other requirements too:

- **The primary data has to change more often than the derived data is used.** This pattern works by avoiding processing derived data when a subsequent primary data change would invalidate it before it gets used. If you find yourself always needing that derived data after every single modification to the primary data, this pattern can't help.

- **It should be hard to update incrementally.** Let's say the pirate ship in our game can only carry so much booty. We need to know the total weight of everything in the hold. We *could* use this pattern and have a dirty flag for the total weight. Every time we add or remove some loot, we set the flag. When we need the total, we add up all of the booty and clear the flag.

  But a simpler solution is to *keep a running total*. When we add or remove an item, just add or remove its weight from the current total. If we can "pay as we go" like this and keep the derived data updated, then that's often a better choice than using this pattern and calculating the derived data from scratch when needed.

This makes it sound like dirty flags are rarely appropriate, but you'll find a place here or there where they help. Searching your average game codebase for the word "dirty" will often turn up uses of this pattern.

From my research, it also turns up a lot of comments apologizing for "dirty" hacks.

**CN:** 与本书中的其他一些模式相比，这个模式解决的是一个相当具体的问题。同样，与大多数优化一样，只有在性能问题足够大以至于值得增加代码复杂度时，你才应该使用它。

脏标记适用于两种工作：*计算*和*同步*。在这两种情况下，从主数据到派生数据的过程都是耗时或昂贵的。

在我们的场景图示例中，该过程之所以慢是因为需要执行大量的数学运算。而在使用此模式进行同步时，更常见的情况是派生数据在*其他地方*——在磁盘上或通过网络位于另一台机器上——仅仅是将它从 A 点传输到 B 点就很昂贵。

还有一些其他要求：

- **主数据的变化频率必须高于派生数据的使用频率。** 此模式的工作原理是，当后续的主数据变化会在派生数据被使用之前使其失效时，避免处理派生数据。如果你发现在每次修改主数据后都需要派生数据，那么这个模式帮不上忙。

- **应该难以增量更新。** 假设我们游戏中的海盗船只能携带有限数量的战利品。我们需要知道货舱中所有物品的总重量。我们*可以*使用此模式，为总重量设置一个脏标记。每次添加或移除一些战利品时，我们设置标记。当需要总重量时，我们把所有战利品加起来并清除标记。

  但是一个更简单的解决方案是*保持一个运行总计*。当添加或移除物品时，只需从当前总计中增加或减去其重量。如果我们能这样"随用随付"并保持派生数据更新，这通常比使用此模式并在需要时从头计算派生数据更好。

这听起来像是脏标记很少适用，但你总会在这里或那里找到它们有帮助的地方。在你平时的游戏代码库中搜索"dirty"这个词，通常会找到此模式的使用。

根据我的研究，它还会找到很多为"dirty"黑客行为道歉的注释。

## Keep in Mind / 注意事项

### There is a cost to deferring for too long / 延迟过久也有代价

* This pattern defers some slow work until the result is actually needed, but when it is, it's often needed *right now*. But the reason we're using this pattern to begin with is because calculating that result is slow!

This isn't a problem in our example because we can still calculate world coordinates fast enough to fit within a frame, but you can imagine other cases where the work you're doing is a big chunk that takes noticeable time to chew through. If the game doesn't *start* chewing until right when the player expects to see the result, that can cause an unpleasant visible pause.

Another problem with deferring is that if something goes wrong, you may fail to do the work at all. This can be particularly problematic when you're using this pattern to save some state to a more persistent form.

For example, text editors know if your document has "unsaved changes". That little bullet or star in your file's title bar is literally the dirty flag visualized. The primary data is the open document in memory, and the derived data is the file on disk.

![A window titlebar showing the little icon representing unsaved changes.](images/dirty-flag-title-bar.png)

Many programs don't save to disk until either the document is closed or the application is exited. That's fine most of the time, but if you accidentally kick the power cable out, there goes your masterpiece.

Editors that auto-save a backup in the background are compensating specifically for this shortcoming. The auto-save frequency is a point on the continuum between not losing too much work when a crash occurs and not thrashing the file system too much by saving all the time.

This mirrors the different garbage collection strategies in systems that automatically manage memory. Reference counting frees memory the second it's no longer needed, but it burns CPU time updating ref counts eagerly every time references are changed.

Simple garbage collectors defer reclaiming memory until it's really needed, but the cost is the dreaded "GC pause" that can freeze your entire game until the collector is done scouring the heap.

In between the two are more complex systems like deferred ref-counting and incremental GC that reclaim memory less eagerly than pure ref-counting but more eagerly than stop-the-world collectors.

**CN:** 这个模式将一些慢速工作推迟到真正需要结果时才执行，但当需要时，通常*立刻*就需要。而我们使用这个模式的原因恰恰是因为计算结果很慢！

在我们的示例中这不是问题，因为我们仍然可以足够快地计算全局坐标以适应一帧的时间，但你可以想象其他情况，你正在做的工作是一个大块，需要花费明显的时间来处理。如果游戏直到玩家期望看到结果时才*开始*处理，那可能会导致令人不快的可见停顿。

延迟的另一个问题是，如果出了问题，你可能根本无法完成这项工作。当你使用此模式将某些状态保存到更持久的形式时，这尤其成问题。

例如，文本编辑器知道你的文档是否有"未保存的更改"。文件标题栏中的那个小圆点或星号就是脏标记的可视化形式。主数据是内存中打开的文档，派生数据是磁盘上的文件。

许多程序直到关闭文档或退出应用时才保存到磁盘。这在大多数情况下没问题，但如果你意外踢掉了电源线，你的杰作就没了。

在后台自动保存备份的编辑器正是为了弥补这个缺点。自动保存频率是一个连续统上的一个点，一端是崩溃时不会丢失太多工作，另一端是不会因为一直保存而过度占用文件系统。

这反映了自动管理内存的系统中不同的垃圾回收策略。引用计数在不再需要内存的那一刻就释放它，但它会在每次引用变化时急切地消耗 CPU 时间来更新引用计数。

简单的垃圾回收器将回收内存推迟到真正需要时，但其代价是可怕的"GC 暂停"，它可能冻结整个游戏，直到回收器完成对堆的清理。

介于两者之间的是更复杂的系统，如延迟引用计数和增量式 GC，它们回收内存的急切程度低于纯引用计数，但高于全停顿收集器。

### You have to make sure to set the flag *every* time the state changes / 必须确保每次状态变化都设置标记

* Since the derived data is calculated from the primary data, it's essentially a cache. Whenever you have cached data, the trickiest aspect of it is *cache invalidation* — correctly noting when the cache is out of sync with its source data. In this pattern, that means setting the dirty flag when *any* primary data changes.

Phil Karlton famously said, "There are only two hard things in Computer Science: cache invalidation and naming things."

Miss it in one place, and your program will incorrectly use stale derived data. This leads to confused players and bugs that are very hard to track down. When you use this pattern, you'll have to take care that any code that modifies the primary state also sets the dirty flag.

One way to mitigate this is by encapsulating modifications to the primary data behind some interface. If anything that can change the state goes through a single narrow API, you can set the dirty flag there and assured that it won't be missed.

**CN:** 由于派生数据是从主数据计算而来的，它本质上是一个缓存。每当你拥有缓存数据时，最棘手的方面就是*缓存失效（cache invalidation）*——正确标记缓存何时与其源数据不同步。在这个模式中，这意味着在*任何*主数据变化时都要设置脏标记。

Phil Karlton 有句名言："计算机科学中只有两件难事：缓存失效和命名。"

在一个地方遗漏了它，你的程序就会错误地使用过时的派生数据。这会导致玩家困惑和极难追踪的 bug。当你使用此模式时，必须注意任何修改主状态的代码也要设置脏标记。

缓解此问题的一种方法是将对主数据的修改封装在某个接口后面。如果任何可以改变状态的操作都通过一个单一的狭窄 API，你可以在此处设置脏标记，并确信它不会被遗漏。

### You have to keep the previous derived data in memory / 必须将先前的派生数据保留在内存中

* When the derived data is needed and the dirty flag *isn't* set, it uses the previously calculated data. This is obvious, but that does imply that you have to keep that derived data around in memory in case you end up needing it later.

This isn't much of an issue when you're using this pattern to synchronize the primary state to some other place. In that case, the derived data isn't usually in memory at all.

If you weren't using this pattern, you could calculate the derived data on the fly whenever you needed it, then discard it when you were done. That avoids the expense of keeping it cached in memory at the cost of having to do that calculation every time you need the result.

Like many optimizations, then, this pattern trades memory for speed. In return for keeping the previously calculated data in memory, you avoid having to recalculate it when it hasn't changed. This trade-off makes sense when the calculation is slow and memory is cheap. When you've got more time than memory on your hands, it's better to calculate it as needed.

Conversely, compression algorithms make the opposite trade-off: they optimize *space* at the expense of the processing time needed to decompress.

**CN:** 当需要派生数据但脏标记*没有*被设置时，它使用先前计算的数据。这是显而易见的，但它确实意味着你必须将派生数据保留在内存中，以防你之后需要它。

当你使用此模式将主状态同步到其他地方时，这不是什么大问题。在这种情况下，派生数据通常根本不在内存中。

如果你没有使用此模式，你可以在需要时即时计算派生数据，然后在完成后丢弃它。这避免了将其缓存在内存中的开销，代价是每次需要结果时都必须重新计算。

因此，与许多优化一样，这个模式用内存换取速度。通过将先前计算的数据保留在内存中，你避免了在数据未变化时重新计算。这种权衡在计算速度慢而内存廉价时是合理的。当你拥有更多时间而不是内存时，按需计算是更好的选择。

相反，压缩算法做出了相反的权衡：它们优化*空间*，代价是解压所需的处理时间。

## Sample Code / 示例代码

* Let's assume we've met the surprisingly long list of requirements and see how the pattern looks in code. As I mentioned before, the actual math behind transform matrices is beyond the humble aims of this book, so I'll just encapsulate that in a class whose implementation you can presume exists somewhere out in the æther:

```cpp
class Transform {
public:
  static Transform origin();
  Transform combine(Transform& other);
};
```

The only operation we need here is `combine()` so that we can get an object's world transform by combining all of the local transforms along its parent chain. It also has a method to get an "origin" transform — basically an identity matrix that means no translation, rotation, or scaling at all.

Next, we'll sketch out the class for an object in the scene graph. This is the bare minimum we need *before* applying this pattern:

```cpp
class GraphNode {
public:
  GraphNode(Mesh* mesh)
  : mesh_(mesh),
    local_(Transform::origin())
  {}

private:
  Transform local_;
  Mesh* mesh_;
  GraphNode* children_[MAX_CHILDREN];
  int numChildren_;
};
```

Each node has a local transform which describes where it is relative to its parent. It has a mesh which is the actual graphic for the object. (We'll allow `mesh_` to be `NULL` too to handle non-visual nodes that are used just to group their children.) Finally, each node has a possibly empty collection of child nodes.

With this, a "scene graph" is really only a single root `GraphNode` whose children (and grandchildren, etc.) are all of the objects in the world:

```cpp
GraphNode* graph_ = new GraphNode(NULL);
// Add children to root graph node...
```

In order to render a scene graph, all we need to do is traverse that tree of nodes, starting at the root, and call the following function for each node's mesh with the right world transform:

```cpp
void renderMesh(Mesh* mesh, Transform transform);
```

We won't implement this here, but if we did, it would do whatever magic the renderer needs to draw that mesh at the given location in the world. If we can call that correctly and efficiently on every node in the scene graph, we're happy.

### An unoptimized traversal / 未优化的遍历

* To get our hands dirty, let's throw together a basic traversal for rendering the scene graph that calculates the world positions on the fly. It won't be optimal, but it will be simple. We'll add a new method to `GraphNode`:

```cpp
void GraphNode::render(Transform parentWorld) {
  Transform world = local_.combine(parentWorld);

  if (mesh_) renderMesh(mesh_, world);

  for (int i = 0; i < numChildren_; i++)
  {
    children_[i]->render(world);
  }
}
```

We pass the world transform of the node's parent into this using `parentWorld`. With that, all that's left to get the correct world transform of *this* node is to combine that with its own local transform. We don't have to walk *up* the parent chain to calculate world transforms because we calculate as we go while walking *down* the chain.

We calculate the node's world transform and store it in `world`, then we render the mesh, if we have one. Finally, we recurse into the child nodes, passing in *this* node's world transform. All in all, it's a tight, simple recursive method.

To draw an entire scene graph, we kick off the process at the root node:

```cpp
graph_->render(Transform::origin());
```

**CN:** 让我们假设我们已经满足了这出奇长的一系列要求，来看看这个模式在代码中是什么样子。正如我之前提到的，变换矩阵背后的实际数学运算超出了本书谦卑的目标，所以我将把它封装在一个类中，你可以假定它的实现存在于以太中的某个地方：

[Transform 类和 GraphNode 类的代码如上所示]

要渲染一个场景图，我们需要做的就是遍历节点树，从根节点开始，为每个节点的网格调用以下函数，并传入正确的全局变换：

```cpp
void renderMesh(Mesh* mesh, Transform transform);
```

我们不会在这里实现它，但如果实现了，它将执行渲染器在世界的给定位置绘制该网格所需的任何魔法。如果我们能正确且高效地在场景图中的每个节点上调用它，我们就满意了。

### Let's get dirty / 开始使用脏标记

* So this code does the right thing — it renders all the meshes in the right place — but it doesn't do it efficiently. It's calling `local_.combine(parentWorld)` on every node in the graph, every frame. Let's see how this pattern fixes that. First, we need to add two fields to `GraphNode`:

```cpp
class GraphNode {
public:
  GraphNode(Mesh* mesh)
  : mesh_(mesh),
    local_(Transform::origin()),
    dirty_(true)
  {}

  // Other methods...

private:
  Transform world_;
  bool dirty_;
  // Other fields...
};
```

The `world_` field caches the previously calculated world transform, and `dirty_`, of course, is the dirty flag. Note that the flag starts out `true`. When we create a new node, we haven't calculated its world transform yet. At birth, it's already out of sync with the local transform.

The only reason we need this pattern is because objects can *move*, so let's add support for that:

```cpp
void GraphNode::setTransform(Transform local) {
  local_ = local;
  dirty_ = true;
}
```

The important part here is that it sets the dirty flag too. Are we forgetting anything? Right — the child nodes!

When a parent node moves, all of its children's world coordinates are invalidated too. But here, we aren't setting their dirty flags. We *could* do that, but that's recursive and slow. Instead, we'll do something clever when we go to render. Let's see:

```cpp
void GraphNode::render(Transform parentWorld, bool dirty) {
  dirty |= dirty_;
  if (dirty)
  {
    world_ = local_.combine(parentWorld);
    dirty_ = false;
  }

  if (mesh_) renderMesh(mesh_, world_);

  for (int i = 0; i < numChildren_; i++)
  {
    children_[i]->render(world_, dirty);
  }
}
```

This is similar to the original naïve implementation. The key changes are that we check to see if the node is dirty before calculating the world transform and we store the result in a field instead of a local variable. When the node is clean, we skip `combine()` completely and use the old-but-still-correct `world_` value.

The clever bit is that `dirty` parameter. That will be `true` if any node above this node in the parent chain was dirty. In much the same way that `parentWorld` updates the world transform incrementally as we traverse down the hierarchy, `dirty` tracks the dirtiness of the parent chain.

This lets us avoid having to recursively mark each child's `dirty_` flag in `setTransform()`. Instead, we pass the parent's dirty flag down to its children when we render and look at that too to see if we need to recalculate the world transform.

The end result here is exactly what we want: changing a node's local transform is just a couple of assignments, and rendering the world calculates the exact minimum number of world transforms that have changed since the last frame.

**CN:** 所以这段代码做了正确的事情——它在正确的位置渲染了所有网格——但效率不高。它每帧对图中每个节点都调用 `local_.combine(parentWorld)`。让我们看看这个模式如何解决这个问题。首先，我们需要向 `GraphNode` 添加两个字段：

`world_` 字段缓存先前计算的全局变换，而 `dirty_` 当然是脏标记。注意标记以 `true` 开始。当我们创建一个新节点时，我们还没有计算它的全局变换。从诞生起，它就已经与局部变换不同步了。

我们需要这个模式的唯一原因是对象可以*移动*，所以让我们添加对移动的支持：

这里的重要部分是它也设置了脏标记。我们忘记了什么吗？对了——子节点！

当父节点移动时，所有子节点的全局坐标也会失效。但在这里，我们没有设置它们的脏标记。我们*可以*这样做，但那是递归且缓慢的。相反，我们会在渲染时做一些巧妙的事情：

这与原始的朴素实现类似。关键变化是我们在计算全局变换之前检查节点是否为脏，并将结果存储在字段中而不是局部变量中。当节点为清洁时，我们完全跳过 `combine()` 并使用旧的但仍然正确的 `world_` 值。

巧妙之处在于 `dirty` 参数。如果父链中此节点之上的任何节点是脏的，它将为 `true`。就像 `parentWorld` 在沿层级向下遍历时增量更新全局变换一样，`dirty` 跟踪父链的脏状态。

这使我们避免了在 `setTransform()` 中递归标记每个子节点的 `dirty_` 标记。相反，我们在渲染时将父节点的脏标记传递给子节点，并同时也检查它来判断是否需要重新计算全局变换。

最终结果正是我们想要的：改变节点的局部变换只需要几次赋值，而渲染世界时计算的是自上一帧以来发生变化的全局变换的最小数量。

## Design Decisions / 设计决策

* This pattern is fairly specific, so there are only a couple of knobs to twiddle:

* 这个模式相当具体，所以只有几个可以调节的旋钮：

### When is the dirty flag cleaned? / 何时清除脏标记？

- **When the result is needed / 当需要结果时**

- *It avoids doing calculation entirely if the result is never used.* For primary data that changes much more frequently than the derived data is accessed, this can be a big win.
  - *If the calculation is time-consuming, it can cause a noticeable pause.* Postponing the work until the player is expecting to see the result can affect their gameplay experience. It's often fast enough that this isn't a problem, but if it is, you'll have to do the work earlier.

- *如果结果从未被使用，可以完全避免计算。* 对于主数据变化频率远高于派生数据访问频率的情况，这可能是一个巨大的优势。
  - *如果计算耗时，可能会导致明显的停顿。* 将工作推迟到玩家期望看到结果时进行，可能会影响他们的游戏体验。通常这足够快以至于不是问题，但如果确实是问题，你就需要提前完成工作。

- **At well-defined checkpoints / 在明确定义的检查点**

* Sometimes, there is a point in time or in the progression of the game where it's natural to do the deferred processing. For example, we may want to save the game only when the pirate sails into port. Or the sync point may not be part of the game mechanics. We may just want to hide the work behind a loading screen or a cut scene.

- *Doing the work doesn't impact the user experience.* Unlike the previous option, you can often give something to distract the player while the game is busy processing.
  - *You lose control over when the work happens.* This is sort of the opposite of the earlier point. You have micro-scale control over when you process, and can make sure the game handles it gracefully. What you *can't* do is ensure the player actually makes it to the checkpoint or meets whatever criteria you've defined. If they get lost or the game gets in a weird state, you can end up deferring longer than you expect.

**CN:** 有时，在时间上或游戏进程中有某个自然而然的点来执行延迟处理。例如，我们可能只想在海盗驶入港口时保存游戏。或者同步点可能不属于游戏机制的一部分。我们可能只是想将工作隐藏在加载屏幕或过场动画后面。

- *执行工作不会影响用户体验。* 与前面的选项不同，你通常可以在游戏忙于处理时给玩家一些分散注意力的东西。
  - *你会失去对工作发生时间的控制。* 这与前一点有些相反。你对处理时间有微观控制，可以确保游戏优雅地处理它。你不能做的是确保玩家真正到达检查点或满足你定义的任何条件。如果玩家迷路或游戏进入奇怪状态，你最终可能会延迟得比预期更久。

- **In the background / 在后台**

* Usually, you start a fixed timer on the first modification and then process all of the changes that happen between then and when the timer fires.

The term in human-computer interaction for an intentional delay between when a program receives user input and when it responds is [*hysteresis*](http://en.wikipedia.org/wiki/Hysteresis).

- *You can tune how often the work is performed.* By adjusting the timer interval, you can ensure it happens as frequently (or infrequently) as you want.
  - *You can do more redundant work.* If the primary state only changes a tiny amount during the timer's run, you can end up processing a large chunk of mostly unchanged data.
  - *You need support for doing work asynchronously.* Processing the data "in the background" implies that the player can keep doing whatever it is that they're doing at the same time. That means you'll likely need threading or some other kind of concurrency support so that the game can work on the data while it's still being played. Since the player is likely interacting with the same primary state that you're processing, you'll need to think about making that safe for concurrent modification too.

**CN:** 通常，你在第一次修改时启动一个固定计时器，然后处理从那时起到计时器触发之间发生的所有变化。

人机交互中，程序接收到用户输入和做出响应之间的有意延迟称为[*滞后（hysteresis）*](http://en.wikipedia.org/wiki/Hysteresis)。

- *你可以调整执行工作的频率。* 通过调整计时器间隔，你可以确保工作按你想要的频率（或不频繁地）发生。
  - *你可能会做更多冗余工作。* 如果主状态在计时器运行期间只发生了微小变化，你最终可能会处理一大块基本未变化的数据。
  - *你需要支持异步执行工作。* "在后台"处理数据意味着玩家可以同时继续做他们正在做的事情。这意味着你可能需要线程或其他某种并发支持，以便游戏在仍被游玩时可以处理数据。由于玩家很可能正在与你正在处理的同一主状态进行交互，你还需要考虑使其安全地支持并发修改。
### How fine-grained is your dirty tracking? / 脏标记追踪的粒度

* Imagine our pirate game lets players build and customize their pirate ship. Ships are automatically saved online so the player can resume where they left off. We're using dirty flags to determine which decks of the ship have been fitted and need to be sent to the server. Each chunk of data we send to the server contains some modified ship data and a bit of metadata describing where on the ship this modification occurred.

- **If it's more fine-grained / 如果粒度更细**

  * Say you slap a dirty flag on each tiny plank of each deck.
  - *You only process data that actually changed.* You'll send exactly the facets of the ship that were modified to the server.

  **CN:** 假设你在每层甲板的每块小木板上都设置一个脏标记。
  - *你只处理实际变化的数据。* 你将只把被修改的船体部分发送到服务器。

- **If it's more coarse-grained / 如果粒度更粗**

  * Alternatively, we could associate a dirty bit with each deck. Changing anything on it marks the entire deck dirty.

  - *You end up processing unchanged data.* Add a single barrel to a deck and you'll have to send the whole thing to the server.
  - *Less memory is used for storing dirty flags.* Add ten barrels to a deck and you only need a single bit to track them all.
  - *Less time is spent on fixed overhead.* When processing some changed data, there's often a bit of fixed work you have to do on top of handling the data itself. In the example here, that's the metadata required to identify where on the ship the changed data is. The bigger your processing chunks, the fewer of them there are, which means the less overhead you have.

  **CN:** 或者，我们可以为每层甲板关联一个脏位。更改其上的任何内容都将整层甲板标记为脏。

  - *你最终会处理未变化的数据。* 在甲板上添加一个桶，你就必须将整层甲板发送到服务器。
  - *存储脏标记使用的内存更少。* 在甲板上添加十个桶，你只需要一个比特来跟踪它们全部。
  - *固定开销花费的时间更少。* 在处理某些变化的数据时，除了处理数据本身之外，通常还需要做一些固定工作。在这个例子中，就是识别变化数据在船上位置的元数据。你的处理块越大，它们的数量就越少，意味着开销越少。

## See Also / 参考

- This pattern is common outside of games in browser-side web frameworks like [Angular](http://angularjs.org/). They use dirty flags to track which data has been changed in the browser and needs to be pushed up to the server.
- Physics engines track which objects are in motion and which are resting. Since a resting body won't move until an impulse is applied to it, they don't need processing until they get touched. This "is moving" bit is a dirty flag to note which objects have had forces applied and need to have their physics resolved.

- 此模式在游戏之外的浏览器端 Web 框架（如 [Angular](http://angularjs.org/)）中也很常见。它们使用脏标记来跟踪浏览器中哪些数据已被更改并需要推送到服务器。
- 物理引擎跟踪哪些对象在运动，哪些在静止。由于静止的物体在受到冲量之前不会移动，它们在受到碰触之前不需要处理。这个"正在移动"的比特就是一个脏标记，用于标记哪些对象已被施加了力并需要解析其物理状态。
---

## C# Equivalent / C# 对照实现

```csharp
using UnityEngine;

/// <summary>
/// 脏标记模式 C# 实现——场景图层级变换
/// 
/// 核心思想：
/// 1. 每个节点缓存其全局变换（world transform）
/// 2. 当局部变换改变时，只设置脏标记，不立即重新计算
/// 3. 在需要全局变换时（如渲染），检查脏标记再决定是否重新计算
/// 4. 父节点的脏状态沿层级向下传递，避免递归设置所有子节点标记
/// 
/// 优势：将每帧多次冗余计算降低为每个节点最多一次计算
/// </summary>

// —— 简化的变换结构体 ——
// 使用 struct 而非 class 以减少 GC 压力
// 实际项目中应使用 Matrix4x4

[System.Serializable]
public struct SimpleTransform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public static SimpleTransform identity => new SimpleTransform
    {
        position = Vector3.zero,
        rotation = Quaternion.identity,
        scale = Vector3.one
    };

    /// <summary>
    /// 组合两个变换（相当于矩阵乘法）
    /// 将 parent 变换应用到当前变换上，得到组合后的全局变换
    /// </summary>
    public static SimpleTransform Combine(SimpleTransform local, SimpleTransform parent)
    {
        SimpleTransform result;
        result.position = parent.position + parent.rotation * Vector3.Scale(local.position, parent.scale);
        result.rotation = parent.rotation * local.rotation;
        result.scale = Vector3.Scale(parent.scale, local.scale);
        return result;
    }
}

// —— 场景图节点 ——
// 使用脏标记模式优化层级变换的更新

public class GraphNode
{
    // 局部变换——相对于父节点的位置/旋转/缩放
    // 改变此值时应设置脏标记，而非立即重新计算全局变换
    [SerializeField]
    private SimpleTransform localTransform = SimpleTransform.identity;

    // 缓存的全局变换——每帧最多重新计算一次
    // 当脏标记为 false 时直接使用此缓存值，避免计算开销
    private SimpleTransform worldTransform;

    // 脏标记：true 表示 worldTransform 需要重新计算
    // 初始为 true，因为新建节点时尚未计算过全局变换
    private bool isDirty = true;

    // 可选：用于渲染的 Mesh
    private Mesh mesh;

    // 子节点列表
    private System.Collections.Generic.List<GraphNode> children
        = new System.Collections.Generic.List<GraphNode>();

    // —— 设置局部变换 ——
    // 注意：这里只设置局部变换和脏标记
    // 不立即递归更新子节点
    // 子节点的脏状态在渲染时通过参数传递处理

    public void SetLocalTransform(SimpleTransform transform)
    {
        localTransform = transform;
        isDirty = true;  // 标记当前节点需要重新计算
        // ★ 关键：不在这里递归设置子节点脏标记
        // 子节点的有效性将在渲染时通过传递父节点脏状态来判断
    }

    // 便捷方法：设置位置
    public void SetPosition(Vector3 position)
    {
        localTransform.position = position;
        isDirty = true;
    }

    // 便捷方法：设置旋转
    public void SetRotation(Quaternion rotation)
    {
        localTransform.rotation = rotation;
        isDirty = true;
    }

    // —— 获取全局变换 ——
    // 这个方法可能被外部代码调用
    // 此时应确保返回的是最新值

    public SimpleTransform GetWorldTransform()
    {
        // 如果脏，先重新计算
        if (isDirty)
        {
            // 注：此简化版本假设有父节点引用
            // 实际需要递归获取父节点的全局变换
            RecalculateWorld();
        }
        return worldTransform;
    }

    // —— 重新计算全局变换 ——
    // 计算 localTransform 与父节点全局变换的组合

    private void RecalculateWorld()
    {
        // 如果当前节点有父节点，则与父节点的全局变换组合
        // 此处简化处理，实际项目中应通过父节点引用获取
        worldTransform = SimpleTransform.Combine(localTransform, GetParentWorld());
        isDirty = false;  // 计算完成后清除脏标记
    }

    // —— 获取父节点全局变换 ——
    // 在完整实现中，应递归获取
    private SimpleTransform GetParentWorld()
    {
        // 简化：若无父节点，全局变换等于局部变换
        return SimpleTransform.identity;
    }

    // —— 渲染方法 ——
    // 这是脏标记模式的核心：遍历场景图进行渲染
    // parentDirty 参数：父节点的脏状态沿层级向下传递
    // 这样即使当前节点的脏标记为 false，
    // 如果父节点移动了（parentDirty = true），
    // 当前节点也需要重新计算

    public void Render(SimpleTransform parentWorld, bool parentDirty)
    {
        // ★ 核心逻辑：合并父节点传递的脏状态和当前节点的脏标记
        // 如果父节点脏了，或者当前节点自己脏了，就需要重新计算
        bool needsRecalculation = parentDirty || isDirty;

        if (needsRecalculation)
        {
            // 重新计算全局变换
            worldTransform = SimpleTransform.Combine(localTransform, parentWorld);
            isDirty = false;  // 清除脏标记
        }

        // 如果有 Mesh，使用计算后的全局变换进行渲染
        if (mesh != null)
        {
            RenderMesh(mesh, worldTransform);
        }

        // 递归渲染所有子节点
        // 将当前节点的全局变换和脏状态传递给子节点
        foreach (var child in children)
        {
            child.Render(worldTransform, needsRecalculation);
        }
    }

    private void RenderMesh(Mesh mesh, SimpleTransform transform)
    {
        // 实际渲染逻辑：使用 transform 设置 GameObject 的位置等
        // 这里仅作演示
        Graphics.DrawMesh(mesh,
            transform.position,
            transform.rotation,
            null,
            0);
    }

    // —— 添加子节点 ——
    public void AddChild(GraphNode child)
    {
        children.Add(child);
    }
}

// —— Unity Monobehaviour 使用示例 ——
// 演示脏标记在场景图中的应用

public class DirtyFlagDemo : MonoBehaviour
{
    // 场景图根节点
    private GraphNode rootNode;

    // 频繁移动的节点（如玩家角色）
    private GraphNode playerNode;

    // 静态世界物体
    private GraphNode staticBuilding;

    void Start()
    {
        // 构建场景图
        rootNode = new GraphNode();
        playerNode = new GraphNode();
        staticBuilding = new GraphNode();
        rootNode.AddChild(playerNode);
        rootNode.AddChild(staticBuilding);
    }

    void Update()
    {
        // 玩家每帧移动——只设置脏标记
        // 不会立即导致整个子树的重新计算
        playerNode.SetPosition(
            playerNode.GetWorldTransform().position + 
            Vector3.forward * Time.deltaTime * 5f);

        // 静态建筑没有变化——其脏标记始终为 false
        // 在渲染时会被跳过重新计算

        // 每帧渲染时，脏标记模式确保：
        // - 只有从根到玩家的路径上的节点被重新计算
        // - staticBuilding 即使位于同一层级，也不重新计算
        // - 整个流程从根遍历到底，每个节点最多计算一次
    }
}

// —— 与 Unity Transform 的关联 ——
// Unity 的 Transform 组件内部使用了类似的脏标记机制
// 可以通过 transform.hasChanged 属性检查

public class DirtyFlagUnityExample : MonoBehaviour
{
    void CheckTransformChange()
    {
        // Unity Transform 内部使用脏标记跟踪变化
        // hasChanged 就是暴露给开发者的脏标记
        if (transform.hasChanged)
        {
            Debug.Log("Transform 发生了变化");
            // 处理变换改变后的逻辑...

            // 手动重置脏标记
            transform.hasChanged = false;
        }
    }

    void LateUpdate()
    {
        // 典型使用场景：在 LateUpdate 中检查变换变化
        // 此时所有 Update 中的变换修改都已应用
        CheckTransformChange();
    }
}
```

## Unity Application / Unity 应用场景

1. **Transform Hierarchy**: Unity's `Transform` component internally uses dirty flags. When a parent Transform changes position/rotation/scale, children's world-to-local matrix is marked dirty. The `transform.hasChanged` property exposes this flag.
2. **Mesh Collider Recalculation**: When a MeshFilter's mesh changes, the MeshCollider is marked dirty and recalculated on next physics query.
3. **UI Rebuild**: Unity's UGUI marks layout groups and canvases as dirty when content changes, rebuilding only when the layout is next queried.
4. **Navigation Mesh**: NavMeshObstacle uses dirty flags to determine when to carve the navigation mesh.
5. **Light Probe Systems**: Light probe data is recalculated only when lighting or geometry changes.

1. **Transform 层级**: Unity 的 `Transform` 组件内部使用脏标记机制。当父 Transform 变换时，子节点的 worldToLocal 矩阵被标记为脏。`transform.hasChanged` 属性暴露了这个标记。
2. **Mesh Collider 重新计算**: 当 MeshFilter 的网格改变时，MeshCollider 被标记为脏，在下一次物理查询时重新计算。
3. **UI 重建**: Unity 的 UGUI 在内容变化时将 Layout Group 和 Canvas 标记为脏，在下次查询布局时才重建。
4. **导航网格**: NavMeshObstacle 使用脏标记决定何时在导航网格中进行 carve 操作。
5. **光照探头系统**: 仅当光照或几何体发生变化时才重新计算光照探头数据。
## Key Differences / 关键差异

| Aspect | C++ (原书) | C# (Unity) |
|--------|-----------|------------|
| Memory model | Manual stack/heap | Managed heap + GC |
| Flag type | `bool dirty_` | `bool` field or `transform.hasChanged` |
| Hierarchy traversal | Recursive function | Recursive or iterative + `GetComponentInChildren` |
| Matrix operations | Custom `Transform.combine()` | Built-in `Matrix4x4` / `Transform.localToWorldMatrix` |
| Cache invalidation | Manual `dirty_ = true` | Automatic in `Transform`, manual in custom code |
| Performance monitoring | Manual profiling | Profiler markers, `transform.hasChanged` |
| Engine integration | Standalone C++ | Integrated into Unity's ECS and rendering pipeline |

---

# Object Pool — 对象池

---

## Intent / 意图

*Improve performance and memory use by reusing objects from a fixed pool instead of allocating and freeing them individually.*

*通过从固定池中复用对象而非单独分配和释放，来提升性能并优化内存使用。*
---

## Motivation / 动机

We're working on the visual effects for our game. When the hero casts a spell, we want a shimmer of sparkles to burst across the screen. This calls for a *particle system*, an engine that spawns little sparkly graphics and animates them until they wink out of existence.

我们正在为游戏开发视觉效果。当英雄施放咒语时，我们希望屏幕上爆发出闪烁的粒子光芒。这就需要*粒子系统*——一个引擎，用于生成微小闪亮的图形并动画播放直到它们消失。

Since a single wave of the wand could cause hundreds of particles to be spawned, our system needs to be able to create them very quickly. More importantly, we need to make sure that creating and destroying these particles doesn't cause *memory fragmentation*.

由于一次挥动法杖就可能产生数百个粒子，我们的系统需要能够非常快速地创建它们。更重要的是，我们需要确保创建和销毁这些粒子不会导致*内存碎片*。

### The curse of fragmentation

Programming for a game console or mobile device is closer to embedded programming than conventional PC programming in many ways. Memory is scarce, users expect games to be rock solid, and efficient compacting memory managers are rarely available. In this environment, memory fragmentation is deadly.

Fragmentation means the free space in our heap is broken into smaller pieces of memory instead of one large open block. The *total* memory available may be large, but the largest *contiguous* region might be painfully small. Say we've got fourteen bytes free, but it's fragmented into two seven-byte pieces with a chunk of in-use memory between them. If we try to allocate a twelve-byte object, we'll fail. No more sparklies on screen.

It's like trying to parallel park on a busy street where the already parked cars are spread out a bit too far. If they'd bunch up, there would be room, but the free space is *fragmented* into bits of open curb between half a dozen cars.

Here's how a heap becomes fragmented and how it can cause an allocation to fail even where there's theoretically enough memory available.

Even if fragmentation is infrequent, it can still gradually reduce the heap to an unusable foam of open holes and filled-in crevices, ultimately hosing the game completely.

Most console makers require games to pass "soak tests" where they leave the game running in demo mode for several days. If the game crashes, they don't allow it to ship. While soak tests sometimes fail because of a rarely occurring bug, it's usually creeping fragmentation or memory leakage that brings the game down.

### The best of both worlds

Because of fragmentation and because allocation may be slow, games are very careful about when and how they manage memory. A simple solution is often best — grab a big chunk of memory when the game starts, and don't free it until the game ends. But this is a pain for systems where we need to create and destroy things while the game is running.

An object pool gives us the best of both worlds. To the memory manager, we're just allocating one big hunk of memory up front and not freeing it while the game is playing. To the users of the pool, we can freely allocate and deallocate objects to our heart's content.
### 碎片的诅咒

为游戏主机或移动设备编程在许多方面比传统PC编程更接近嵌入式编程。内存稀缺，用户期望游戏坚如磐石，而高效的压缩式内存管理器很少可用。在这种环境下，内存碎片是致命的。

碎片意味着堆中的空闲空间被分割成较小的内存块而不是一个大的连续区块。可用的*总*内存可能很大，但最大的*连续*区域可能小得可怜。假设我们有14字节的空闲空间，但它被碎片化为两个7字节的片段，中间夹着一块使用中的内存。如果我们试图分配一个12字节的对象，就会失败。屏幕上不再有闪烁的粒子。

这就好比在繁忙的街道上试图平行停车，而已停好的车辆之间分布得有些分散。如果它们挤在一起，就会有空间，但空闲空间被碎片化为六辆车之间的零星路边空地。

即使碎片不频繁发生，它仍然会逐渐将堆退化成一个由空洞和填充裂缝组成的无用泡沫，最终完全搞垮游戏。

大多数主机厂商要求游戏通过"浸泡测试"——让游戏在演示模式下运行数天。如果游戏崩溃，就不允许发布。虽然浸泡测试有时因为罕见错误而失败，但通常是由逐渐发展的碎片化或内存泄漏导致游戏崩溃的。

### 两全其美

由于碎片化以及分配可能较慢，游戏在管理内存的时间和方式上非常谨慎。一个简单的解决方案通常是最好的——在游戏启动时抓取一大块内存，直到游戏结束才释放。但对于需要在游戏运行时创建和销毁对象的系统来说，这很麻烦。

对象池为我们提供了两全其美的方案。对内存管理器来说，我们只是预先分配了一大块内存并且在游戏运行时从不释放。对池的使用者来说，我们可以随心所欲地分配和释放对象。

---

## The Pattern / 模式

Define a **pool** class that maintains a collection of **reusable objects**. Each object supports an **"in use" query** to tell if it is currently "alive". When the pool is initialized, it creates the entire collection of objects up front (usually in a single contiguous allocation) and initializes them all to the "not in use" state.

定义一个**池**类，它维护着一组**可复用对象**的集合。每个对象支持一个**"使用中"查询**来判断它当前是否"存活"。当池被初始化时，它预先创建整个对象集合（通常是在一次连续分配中），并将它们全部初始化为"未使用"状态。

When you want a new object, ask the pool for one. It finds an available object, initializes it to "in use", and returns it. When the object is no longer needed, it is set back to the "not in use" state. This way, objects can be freely created and destroyed without needing to allocate memory or other resources.

当你需要新对象时，向池请求一个。它会找到一个可用对象，将其初始化为"使用中"状态并返回。当对象不再需要时，它被设置回"未使用"状态。这样，对象可以自由创建和销毁，而无需分配内存或其他资源。
---

## When to Use It / 何时使用

This pattern is used widely in games for obvious things like game entities and visual effects, but it is also used for less visible data structures such as currently playing sounds. Use Object Pool when:

此模式在游戏中广泛使用于游戏实体和视觉效果等显而易见的地方，但也用于不太显眼的数据结构，如当前正在播放的声音。在以下情况下使用对象池模式：

- You need to frequently create and destroy objects.
- Objects are similar in size.
- Allocating objects on the heap is slow or could lead to memory fragmentation.
- Each object encapsulates a resource such as a database or network connection that is expensive to acquire and could be reused.

- 你需要频繁创建和销毁对象。
- 对象大小相似。
- 在堆上分配对象较慢或可能导致内存碎片。
- 每个对象封装了诸如数据库或网络连接等获取成本高昂且可复用的资源。
---

## Keep in Mind / 注意事项

You normally rely on a garbage collector or `new` and `delete` to handle memory management for you. By using an object pool, you're saying, "I know better how these bytes should be handled." That means the onus is on you to deal with this pattern's limitations.

你通常依赖垃圾回收器或 `new` 和 `delete` 来处理内存管理。通过使用对象池，你等于在说："我更清楚这些字节该如何处理。"这意味着你有责任处理此模式的局限性。

### The pool may waste memory on unneeded objects

The size of an object pool needs to be tuned for the game's needs. When tuning, it's usually obvious when the pool is too *small* (there's nothing like a crash to get your attention). But also take care that the pool isn't too *big*. A smaller pool frees up memory that could be used for other fun stuff.

### Only a fixed number of objects can be active at any one time

In some ways, this is a good thing. Partitioning memory into separate pools for different types of objects ensures that, for example, a huge sequence of explosions won't cause your particle system to eat *all* of the available memory, preventing something more critical like a new enemy from being created.

Nonetheless, this also means being prepared for the possibility that your attempt to reuse an object from the pool will fail because they are all in use. There are a few common strategies to handle this:

- *Prevent it outright.* This is the most common "fix": tune the pool sizes so that they never overflow regardless of what the user does. For pools of important objects like enemies or gameplay items, this is often the right answer. There may be no "right" way to handle the lack of a free slot to create the big boss when the player reaches the end of the level, so the smart thing to do is make sure that never happens.

The downside is that this can force you to sit on a lot of memory for object slots that are needed only for a couple of rare edge cases. Because of this, a single fixed pool size may not be the best fit for all game states. For instance, some levels may feature effects prominently while others focus on sound. In such cases, consider having pool sizes tuned differently for each scenario.

- *Just don't create the object.* This sounds harsh, but it makes sense for cases like our particle system. If all particles are in use, the screen is probably full of flashing graphics. The user won't notice if the next explosion isn't quite as impressive as the ones currently going off.

- *Forcibly kill an existing object.* Consider a pool for currently playing sounds, and assume you want to start a new sound but the pool is full. You do *not* want to simply ignore the new sound — the user will notice if their magical wand swishes dramatically *sometimes* and stays stubbornly silent other times. A better solution is to find the quietest sound already playing and replace that with our new sound. The new sound will mask the audible cutoff of the previous sound.

In general, if the *disappearance* of an existing object would be less noticeable than the *absence* of a new one, this may be the right choice.

- *Increase the size of the pool.* If your game lets you be a bit more flexible with memory, you may be able to increase the size of the pool at runtime or create a second overflow pool. If you do grab more memory in either of these ways, consider whether or not the pool should contract to its previous size when the additional capacity is no longer needed.

### Memory size for each object is fixed

Most pool implementations store the objects in an array of in-place objects. If all of your objects are of the same type, this is fine. However, if you want to store objects of different types in the pool, or instances of subclasses that may add fields, you need to ensure that each slot in the pool has enough memory for the *largest* possible object. Otherwise, an unexpectedly large object will stomp over the next one and trash memory.

At the same time, when your objects vary in size, you waste memory. Each slot needs to be big enough to accommodate the largest object. If objects are rarely that big, you're throwing away memory every time you put a smaller one in that slot. It's like going through airport security and using a huge carry-on-sized luggage tray just for your keys and wallet.

When you find yourself burning a lot of memory this way, consider splitting the pool into separate pools for different sizes of object — big trays for luggage, little trays for pocket stuff.

This is a common pattern for implementing speed-efficient memory managers. The manager has a number of pools of different block sizes. When you ask it to allocate a block, it finds in an open slot in the pool of the appropriate size and allocates from that pool.

### Reused objects aren't automatically cleared

Most memory managers have a debug feature that will clear freshly allocated or freed memory to some obvious magic value like `0xdeadbeef`. This helps you find painful bugs caused by uninitialized variables or using memory after it's freed.

Since our object pool isn't going through the memory manager any more when it reuses an object, we lose that safety net. Worse, the memory used for a "new" object previously held an object of the exact same type. This makes it nearly impossible to tell if you forgot to initialize something when you created the new object: the memory where the object is stored may already contain *almost* correct data from its past life.

Because of this, pay special care that the code that initializes new objects in the pool *fully* initializes the object. It may even be worth spending a bit of time adding a debug feature that clears the memory for an object slot when the object is reclaimed.

I'd be honored if you cleared it to `0x1deadb0b`.

### Unused objects will remain in memory

Object pools are less common in systems that support garbage collection because the memory manager will usually deal with fragmentation for you. But pools are still useful there to avoid the cost of allocation and deallocation, especially on mobile devices with slower CPUs and simpler garbage collectors.

If you do use an object pool in concert with a garbage collector, beware of a potential conflict. Since the pool doesn't actually deallocate objects when they're no longer in use, they remain in memory. If they contain references to *other* objects, it will prevent the collector from reclaiming those too. To avoid this, when a pooled object is no longer in use, clear any references it has to other objects.
### 池可能会在不需要的对象上浪费内存

对象池的大小需要根据游戏需求进行调整。调整时，池太*小*通常显而易见（没有什么比崩溃更能引起你的注意）。但也要注意池不要*太大*。更小的池可以释放出内存用于其他有趣的内容。

### 任何时候只有固定数量的对象可以处于活动状态

在某些方面，这是一件好事。将内存划分为不同类型对象的独立池，可以确保例如一连串大爆炸不会导致粒子系统消耗*所有*可用内存，从而阻止更关键的东西（如新敌人）被创建。

然而，这也意味着你需要做好准备：向池请求复用对象可能会因为所有对象都在使用中而失败。有几种常见策略来处理这个问题：

- *彻底防止溢出。* 这是最常见的"修复"方法：调整池大小，使其无论用户做什么都不会溢出。对于敌人或游戏道具等重要对象的池，这通常是正确的答案。当玩家到达关卡末尾时，可能没有"正确"的方法来处理空闲槽位不足无法创建大Boss的情况，所以明智的做法是确保这永远不会发生。

  这样做的缺点是你可能需要占用大量内存来存放那些只在少数罕见边缘情况下才需要的对象槽位。因此，单一的固定池大小可能并不适合所有游戏状态。例如，某些关卡可能以特效为主，而其他关卡则以音效为主。在这种情况下，考虑为不同场景调整不同的池大小。

- *干脆不创建对象。* 这听起来很苛刻，但对于粒子系统这种情况来说是合理的。如果所有粒子都在使用中，屏幕可能已经充满了闪烁的图形。用户不会注意到下一个爆炸不如当前正在进行的爆炸那么引人注目。

- *强行销毁一个现有对象。* 考虑一个用于当前播放声音的池，假设你想开始一个新声音但池已满。你绝对*不*想简单地忽略新声音——如果用户发现他们的魔法法杖有时发出戏剧性的嗖嗖声，而有时却固执地保持沉默，他们会注意到。更好的解决方案是找到正在播放的最安静的声音并用新声音替换它。新声音会掩盖前一个声音的听觉中断。

  一般来说，如果现有对象的*消失*比新对象的*缺失*更不易被察觉，这可能就是正确的选择。

- *增加池的大小。* 如果你的游戏在内存方面更灵活一些，你可以在运行时增加池的大小或创建第二个溢出池。如果你以这两种方式之一获取了更多内存，请考虑当额外容量不再需要时，池是否应该收缩到先前的大小。

### 每个对象的内存大小是固定的

大多数对象池实现将对象存储在就地对象的数组中。如果所有对象都是同一类型，这没问题。但是，如果你想在池中存储不同类型的对象，或者存储可能增加字段的子类实例，你需要确保池中的每个槽位都有足够的内存容纳*最大*的可能对象。否则，一个意外的大对象会踩踏到下一个对象并破坏内存。

同时，当对象大小不同时，你会浪费内存。每个槽位都需要足够大以容纳最大的对象。如果对象很少有那么大，那么每次将较小的对象放入该槽位时都在浪费内存。这就像通过机场安检时，用一个大行李箱大小的托盘来放你的钥匙和钱包。

当你发现自己以这种方式消耗大量内存时，考虑将池拆分为不同对象大小的独立池——大托盘放行李，小托盘放口袋物品。

这是实现高速内存管理器的常见模式。管理器有多个不同块大小的池。当你要求它分配一个块时，它会在适当大小的池中找到一个空闲槽位并从该池分配。

### 复用的对象不会自动清除

大多数内存管理器都有一个调试功能，会将新分配或释放的内存清除为某个明显的魔法值，比如 `0xdeadbeef`。这帮助你找到由未初始化变量或在释放后使用内存引起的恼人错误。

由于我们的对象池在复用对象时不再经过内存管理器，我们失去了这个安全网。更糟糕的是，用于"新"对象的内存之前恰好存放了完全同类型的对象。这使得几乎不可能判断你在创建新对象时是否忘记初始化某些内容：存储对象的内存可能已经包含了来自其前世的*几乎*正确的数据。

因此，要特别注意池中初始化新对象的代码必须*完全*初始化该对象。甚至值得花一点时间添加一个调试功能，在对象被回收时清除对象槽位的内存。

如果你将其清除为 `0x1deadb0b`，我将深感荣幸。

### 未使用的对象将留在内存中

对象池在支持垃圾回收的系统中不太常见，因为内存管理器通常会为你处理碎片化。但池在那里仍然有用，可以避免分配和释放的开销，尤其是在CPU较慢和垃圾回收器较简单的移动设备上。

如果你确实将对象池与垃圾回收器一起使用，要注意一个潜在的冲突。由于池在对象不再使用时实际上并不释放它们，它们会留在内存中。如果它们包含对其他*对象*的引用，这将阻止回收器也回收这些对象。为避免这种情况，当池化对象不再使用时，清除它对其他对象的任何引用。

---

## Sample Code / 示例代码

Real-world particle systems will often apply gravity, wind, friction, and other physical effects. Our much simpler sample will only move particles in a straight line for a certain number of frames and then kill the particle. Not exactly film caliber, but it should illustrate how to use an object pool.

真实世界的粒子系统通常会应用重力、风力、摩擦力和其他物理效果。我们更简单的示例只会让粒子沿直线移动一定帧数然后销毁粒子。虽然算不上电影级别，但应该足以说明如何使用对象池。

We'll start with the simplest possible implementation. First up is the little particle class:

我们将从最简单的实现开始。首先是小小的粒子类：

```cpp
class Particle {
public:
  Particle()
  : framesLeft_(0)
  {}

void init(double x, double y,
            double xVel, double yVel, int lifetime)
  {
    x_ = x; y_ = y;
    xVel_ = xVel; yVel_ = yVel;
    framesLeft_ = lifetime;
  }

void animate()
  {
    if (!inUse()) return;

framesLeft_--;
    x_ += xVel_;
    y_ += yVel_;
  }

bool inUse() const { return framesLeft_ > 0; }

private:
  int framesLeft_;
  double x_, y_;
  double xVel_, yVel_;
};
```

The default constructor initializes the particle to "not in use". A later call to `init()` initializes the particle to a live state. Particles are animated over time using the unsurprisingly named `animate()` function, which should be called once per frame.

默认构造函数将粒子初始化为"未使用"状态。之后的 `init()` 调用将粒子初始化为活跃状态。粒子随时间使用 `animate()` 函数进行动画更新，该函数应每帧调用一次。

The pool needs to know which particles are available for reuse. It gets this from the particle's `inUse()` function. This function takes advantage of the fact that particles have a limited lifetime and uses the `framesLeft_` variable to discover which particles are in use without having to store a separate flag.

池需要知道哪些粒子可以复用。它通过粒子的 `inUse()` 函数获取此信息。该函数利用粒子具有有限生命周期这一事实，使用 `framesLeft_` 变量来判断哪些粒子正在使用中，而无需存储单独的标志。

The pool class is also simple:

池类也很简单：

```cpp
class ParticlePool {
public:
  void create(double x, double y,
              double xVel, double yVel, int lifetime);

void animate()
  {
    for (int i = 0; i < POOL_SIZE; i++)
    {
      particles_[i].animate();
    }
  }

private:
  static const int POOL_SIZE = 100;
  Particle particles_[POOL_SIZE];
};
```

The `create()` function lets external code create new particles. The game calls `animate()` once per frame, which in turn animates each particle in the pool.

`create()` 函数让外部代码创建新的粒子。游戏每帧调用一次 `animate()`，后者依次让池中每个粒子进行动画更新。

This `animate()` method is an example of the Update Method pattern.

粒子本身简单地存储在类中的固定大小数组里。在这个示例实现中，池大小在类声明中硬编码，但也可以通过使用给定大小的动态数组或值模板参数在外部定义。

The particles themselves are simply stored in a fixed-size array in the class. In this sample implementation, the pool size is hardcoded in the class declaration, but this could be defined externally by using a dynamic array of a given size or by using a value template parameter.

创建新粒子很直接：

Creating a new particle is straightforward:

```cpp
void ParticlePool::create(double x, double y,
                          double xVel, double yVel,
                          int lifetime)
{
  // Find an available particle.
  for (int i = 0; i < POOL_SIZE; i++)
  {
    if (!particles_[i].inUse())
    {
      particles_[i].init(x, y, xVel, yVel, lifetime);
      return;
    }
  }
}
```

我们遍历池寻找第一个可用粒子。找到后初始化它，完成。注意在此实现中，如果没有可用粒子，我们只是不创建新粒子。

We iterate through the pool looking for the first available particle. When we find it, we initialize it and we're done. Note that in this implementation, if there aren't any available particles, we simply don't create a new one.

除了渲染粒子之外，简单的粒子系统就是这样。我们现在可以创建一个池并使用它创建一些粒子。粒子在其生命周期结束时会自动停用。

That's all there is to a simple particle system, aside from rendering the particles, of course. We can now create a pool and create some particles using it. The particles will automatically deactivate themselves when their lifetime has expired.

这足以发布游戏，但敏锐的读者可能注意到创建新粒子需要遍历（可能）整个集合直到找到空闲槽位。如果池非常大且几乎满了，这可能会变慢。让我们看看如何改进。

This is good enough to ship a game, but keen eyes may have noticed that creating a new particle requires iterating through (potentially) the entire collection until we find an open slot. If the pool is very large and mostly full, that can get slow. Let's see how we can improve that.

### A free list

If we don't want to waste time *finding* free particles, the obvious answer is to not lose track of them. We could store a separate list of pointers to each unused particle. Then, when we need to create a particle, we remove the first pointer from the list and reuse the particle it points to.

Unfortunately, this would require us to maintain an entire separate array with as many pointers as there are objects in the pool. After all, when we first create the pool, *all* particles are unused, so the list would initially have a pointer to every object in the pool.

It would be nice to fix our performance problems *without* sacrificing any memory. Conveniently, there is some memory already lying around that we can borrow — the data for the unused particles themselves.

When a particle isn't in use, most of its state is irrelevant. Its position and velocity aren't being used. The only state it needs is the stuff required to tell if it's dead. In our example, that's the `framesLeft_` member. All those other bits can be reused. Here's a revised particle:

```cpp
class Particle {
public:
  // ...

Particle* getNext() const { return state_.next; }
  void setNext(Particle* next) { state_.next = next; }

private:
  int framesLeft_;

union
  {
    // State when it's in use.
    struct
    {
      double x, y;
      double xVel, yVel;
    } live;

// State when it's available.
    Particle* next;
  } state_;
};
```

We've moved all of the member variables except for `framesLeft_` into a `live` struct inside a `state_` union. This struct holds the particle's state when it's being animated. When the particle is unused, the other case of the union, the `next` member, is used. It holds a pointer to the next available particle after this one.

We can use these pointers to build a linked list that chains together every unused particle in the pool. We have the list of available particles we need, but we didn't need to use any additional memory. Instead, we cannibalize the memory of the dead particles themselves to store the list.

This clever technique is called a [*free list*](http://en.wikipedia.org/wiki/Free_list). For it to work, we need to make sure the pointers are initialized correctly and are maintained when particles are created and destroyed. And, of course, we need to keep track of the list's head:

```cpp
class ParticlePool {
  // ...
private:
  Particle* firstAvailable_;
};
```

When a pool is first created, *all* of the particles are available, so our free list should thread through the entire pool. The pool constructor sets that up:

```cpp
ParticlePool::ParticlePool()
{
  // The first one is available.
  firstAvailable_ = &particles_[0];

// Each particle points to the next.
  for (int i = 0; i < POOL_SIZE - 1; i++)
  {
    particles_[i].setNext(&particles_[i + 1]);
  }

// The last one terminates the list.
  particles_[POOL_SIZE - 1].setNext(NULL);
}
```

Now to create a new particle, we jump directly to the first available one:

```cpp
void ParticlePool::create(double x, double y,
                          double xVel, double yVel,
                          int lifetime)
{
  // Make sure the pool isn't full.
  assert(firstAvailable_ != NULL);

// Remove it from the available list.
  Particle* newParticle = firstAvailable_;
  firstAvailable_ = newParticle->getNext();

newParticle->init(x, y, xVel, yVel, lifetime);
}
```

We need to know when a particle dies so we can add it back to the free list, so we'll change `animate()` to return `true` if the previously live particle gave up the ghost in that frame:

```cpp
bool Particle::animate()
{
  if (!inUse()) return false;

framesLeft_--;
  x_ += xVel_;
  y_ += yVel_;

return framesLeft_ == 0;
}
```

When that happens, we simply thread it back onto the list:

```cpp
void ParticlePool::animate()
{
  for (int i = 0; i < POOL_SIZE; i++)
  {
    if (particles_[i].animate())
    {
      // Add this particle to the front of the list.
      particles_[i].setNext(firstAvailable_);
      firstAvailable_ = &particles_[i];
    }
  }
}
```

There you go, a nice little object pool with constant-time creation and deletion.
### 空闲链表

如果我们不想浪费时间*寻找*空闲粒子，显而易见的答案是不丢失它们的踪迹。我们可以存储一个单独的指针列表，指向每个未使用的粒子。然后，当我们需要创建粒子时，从列表中移除第一个指针并复用它指向的粒子。

不幸的是，这需要我们维护一个完整的独立数组，其中的指针数量与池中对象数量相同。毕竟，当我们首次创建池时，*所有*粒子都是未使用的，所以列表最初会包含指向池中每个对象的指针。

如果能*在不牺牲任何内存*的情况下解决性能问题就好了。方便的是，已经有了一些我们可以借用的内存——未使用粒子本身的数据。

当一个粒子不使用时，它的大部分状态都无关紧要。它的位置和速度没有被使用。它唯一需要的状态是判断它是否死亡所需的信息。在我们的示例中，那就是 `framesLeft_` 成员。所有其他位都可以复用。下面是修改后的粒子：

```cpp
class Particle {
public:
  // ...

  Particle* getNext() const { return state_.next; }
  void setNext(Particle* next) { state_.next = next; }

private:
  int framesLeft_;

  union
  {
    // State when it's in use.
    struct
    {
      double x, y;
      double xVel, yVel;
    } live;

    // State when it's available.
    Particle* next;
  } state_;
};
```

我们将除 `framesLeft_` 之外的所有成员变量移到了 `state_` 联合体内的 `live` 结构中。该结构体在粒子动画时保存其状态。当粒子未使用时，联合体的另一种情况——`next` 成员——被使用。它持有指向该粒子之后下一个可用粒子的指针。

我们可以使用这些指针构建一个链表，将池中每个未使用的粒子链接在一起。我们得到了所需的可用粒子列表，但不需要使用任何额外的内存。相反，我们利用了死亡粒子本身的内存来存储该列表。

这种巧妙的技术称为[空闲链表](http://en.wikipedia.org/wiki/Free_list)。要使其工作，我们需要确保指针正确初始化，并在创建和销毁粒子时维护它们。当然，我们还需要跟踪链表的头部：

```cpp
class ParticlePool {
  // ...
private:
  Particle* firstAvailable_;
};
```

当池首次创建时，*所有*粒子都是可用的，所以我们的空闲链表应该贯穿整个池。池构造函数设置如下：

```cpp
ParticlePool::ParticlePool()
{
  // The first one is available.
  firstAvailable_ = &particles_[0];

  // Each particle points to the next.
  for (int i = 0; i < POOL_SIZE - 1; i++)
  {
    particles_[i].setNext(&particles_[i + 1]);
  }

  // The last one terminates the list.
  particles_[POOL_SIZE - 1].setNext(NULL);
}
```

现在要创建新粒子，我们直接跳转到第一个可用粒子：

```cpp
void ParticlePool::create(double x, double y,
                          double xVel, double yVel,
                          int lifetime)
{
  // Make sure the pool isn't full.
  assert(firstAvailable_ != NULL);

  // Remove it from the available list.
  Particle* newParticle = firstAvailable_;
  firstAvailable_ = newParticle->getNext();

  newParticle->init(x, y, xVel, yVel, lifetime);
}
```

我们需要知道粒子何时死亡，以便将其添加回空闲链表，所以我们将修改 `animate()`，使其在之前活跃的粒子在该帧中死亡时返回 `true`：

```cpp
bool Particle::animate()
{
  if (!inUse()) return false;

  framesLeft_--;
  x_ += xVel_;
  y_ += yVel_;

  return framesLeft_ == 0;
}
```

当发生这种情况时，我们只需将其重新链接到列表中：

```cpp
void ParticlePool::animate()
{
  for (int i = 0; i < POOL_SIZE; i++)
  {
    if (particles_[i].animate())
    {
      // Add this particle to the front of the list.
      particles_[i].setNext(firstAvailable_);
      firstAvailable_ = &particles_[i];
    }
  }
}
```

搞定，一个不错的具有常量时间创建和删除操作的对象池。

---

## Design Decisions / 设计决策

As you've seen, the simplest object pool implementation is almost trivial: create an array of objects and reinitialize them as needed. Production code is rarely that minimal. There are several ways to expand on that to make the pool more generic, safer to use, or easier to maintain. As you implement pools in your games, you'll need to answer these questions:

正如你所见，最简单的对象池实现几乎是微不足道的：创建一个对象数组，并根据需要重新初始化它们。生产代码很少如此简单。有几种方法可以扩展以使其更通用、更安全或更易于维护。在游戏中实现对象池时，你需要回答以下问题：

### Are objects coupled to the pool?

The first question you'll run into when writing an object pool is whether the objects themselves know they are in a pool. Most of the time they will, but you won't have that luxury when writing a generic pool class that can hold arbitrary objects.

- **If objects are coupled to the pool:**

- *The implementation is simpler.* You can simply put an "in use" flag or function in your pooled object and be done with it.

- *You can ensure that the objects can only be created by the pool.* In C++, a simple way to do this is to make the pool class a friend of the object class and then make the object's constructor private.

```cpp
    class Particle {
      friend class ParticlePool;

private:
      Particle()
      : inUse_(false)
      {}

bool inUse_;
    };

class ParticlePool {
      Particle pool_[100];
    };
    ```

This relationship documents the intended way to use the class and ensures your users don't create objects that aren't tracked by the pool.

- *You may be able to avoid storing an explicit "in use" flag.* Many objects already retain some state that could be used to tell whether it is alive or not. For example, a particle may be available for reuse if its current position is offscreen. If the object class knows it may be used in a pool, it can provide an `inUse()` method to query that state. This saves the pool from having to burn some extra memory storing a bunch of "in use" flags.

- **If objects are not coupled to the pool:**

- *Objects of any type can be pooled.* This is the big advantage. By decoupling objects from the pool, you may be able to implement a generic reusable pool class.

- *The "in use" state must be tracked outside the objects.* The simplest way to do this is by creating a separate bit field:

```cpp
    template <class TObject>
    class GenericPool {
    private:
      static const int POOL_SIZE = 100;

TObject pool_[POOL_SIZE];
      bool    inUse_[POOL_SIZE];
    };
    ```

### What is responsible for initializing the reused objects?

In order to reuse an existing object, it must be reinitialized with new state. A key question here is whether to reinitialize the object inside the pool class or outside.

- **If the pool reinitializes internally:**

- *The pool can completely encapsulate its objects.* Depending on the other capabilities your objects need, you may be able to keep them completely internal to the pool. This makes sure that other code doesn't maintain references to objects that could be unexpectedly reused.

- *The pool is tied to how objects are initialized.* A pooled object may offer multiple functions that initialize it. If the pool manages initialization, its interface needs to support all of those and forward them to the object.

```cpp
    class Particle {
      // Multiple ways to initialize.
      void init(double x, double y);
      void init(double x, double y, double angle);
      void init(double x, double y, double xVel, double yVel);
    };

class ParticlePool {
    public:
      void create(double x, double y)
      {
        // Forward to Particle...
      }

void create(double x, double y, double angle)
      {
        // Forward to Particle...
      }

void create(double x, double y, double xVel, double yVel)
      {
        // Forward to Particle...
      }
    };
    ```

- **If outside code initializes the object:**

- *The pool's interface can be simpler.* Instead of offering multiple functions to cover each way an object can be initialized, the pool can simply return a reference to the new object:

```cpp
    class Particle {
    public:
      // Multiple ways to initialize.
      void init(double x, double y);
      void init(double x, double y, double angle);
      void init(double x, double y, double xVel, double yVel);
    };

class ParticlePool {
    public:
      Particle* create()
      {
        // Return reference to available particle...
      }
    private:
      Particle pool_[100];
    };
    ```

The caller can then initialize the object by calling any method the object exposes:

```cpp
    ParticlePool pool;

pool.create()->init(1, 2);
    pool.create()->init(1, 2, 0.3);
    pool.create()->init(1, 2, 3.3, 4.4);
    ```

- *Outside code may need to handle the failure to create a new object.* The previous example assumes that `create()` will always successfully return a pointer to an object. If the pool is full, though, it may return `NULL` instead. To be safe, you'll need to check for that before you try to initialize the object:

```cpp
    Particle* particle = pool.create();
    if (particle != NULL) particle->init(1, 2);
    ```
### 对象是否与池耦合？

编写对象池时遇到的第一个问题是对象本身是否知道它们在池中。大多数情况下它们是知道的，但在编写可以容纳任意对象的通用池类时，你就没有这个便利了。

- **如果对象与池耦合：**

  - *实现更简单。* 你只需在池化对象中放置一个"使用中"标志或函数即可。

  - *可以确保对象只能由池创建。* 在C++中，一个简单的方法是将池类声明为对象类的友元，然后将对象的构造函数设为私有。

    ```cpp
    class Particle {
      friend class ParticlePool;

    private:
      Particle()
      : inUse_(false)
      {}

      bool inUse_;
    };

    class ParticlePool {
      Particle pool_[100];
    };
    ```

    这种关系记录了使用该类的预期方式，并确保你的用户不会创建未被池跟踪的对象。

  - *可能不需要存储显式的"使用中"标志。* 许多对象已经保留了一些可以判断其是否存活的状态。例如，如果当前位置在屏幕外，粒子可能就可以复用。如果对象类知道它可能被用于池中，它可以提供一个 `inUse()` 方法来查询该状态。这使池不必浪费额外内存存储一堆"使用中"标志。

- **如果对象不与池耦合：**

  - *任何类型的对象都可以被池化。* 这是主要优势。通过将对象与池解耦，你可以实现一个通用的可复用池类。

  - *"使用中"状态必须在对象外部跟踪。* 最简单的方法是创建一个单独的位字段：

    ```cpp
    template <class TObject>
    class GenericPool {
    private:
      static const int POOL_SIZE = 100;

      TObject pool_[POOL_SIZE];
      bool    inUse_[POOL_SIZE];
    };
    ```

### 谁负责初始化复用的对象？

为了复用现有对象，必须用新状态重新初始化它。这里的关键问题是在池类内部还是外部重新初始化对象。

- **如果池在内部重新初始化：**

  - *池可以完全封装其对象。* 取决于对象所需的其他功能，你可以将它们完全保持在池内部。这确保其他代码不会持有可能被意外复用的对象的引用。

  - *池与对象的初始化方式绑定。* 池化对象可能提供多个初始化函数。如果池管理初始化，其接口需要支持所有这些函数并将它们转发给对象。

    ```cpp
    class Particle {
      // Multiple ways to initialize.
      void init(double x, double y);
      void init(double x, double y, double angle);
      void init(double x, double y, double xVel, double yVel);
    };

    class ParticlePool {
    public:
      void create(double x, double y)
      {
        // Forward to Particle...
      }

      void create(double x, double y, double angle)
      {
        // Forward to Particle...
      }

      void create(double x, double y, double xVel, double yVel)
      {
        // Forward to Particle...
      }
    };
    ```

- **如果外部代码初始化对象：**

  - *池的接口可以更简单。* 池不需要提供多个函数来覆盖每种初始化方式，而只需返回对新对象的引用：

    ```cpp
    class Particle {
    public:
      // Multiple ways to initialize.
      void init(double x, double y);
      void init(double x, double y, double angle);
      void init(double x, double y, double xVel, double yVel);
    };

    class ParticlePool {
    public:
      Particle* create()
      {
        // Return reference to available particle...
      }
    private:
      Particle pool_[100];
    };
    ```

    调用者随后可以通过调用对象暴露的任何方法来初始化对象：

    ```cpp
    ParticlePool pool;

    pool.create()->init(1, 2);
    pool.create()->init(1, 2, 0.3);
    pool.create()->init(1, 2, 3.3, 4.4);
    ```

  - *外部代码可能需要处理创建新对象的失败情况。* 前面的示例假设 `create()` 总会成功返回一个指向对象的指针。但如果池已满，它可能返回 `NULL`。为了安全起见，你需要在尝试初始化对象之前检查这一点：

    ```cpp
    Particle* particle = pool.create();
    if (particle != NULL) particle->init(1, 2);
    ```

---

## See Also / 延伸阅读

- This looks a lot like the [Flyweight](flyweight.html) pattern. Both maintain a collection of reusable objects. The difference is what "reuse" means. Flyweight objects are reused by sharing the same instance between multiple owners *simultaneously*. The Flyweight pattern avoids *duplicate* memory usage by using the same object in multiple contexts.

- 这与享元模式非常相似。两者都维护一组可复用对象的集合。区别在于"复用"的含义。享元对象是通过在多个所有者之间*同时*共享同一实例来复用的。享元模式通过在不同上下文中使用同一对象来避免*重复*内存使用。

The objects in a pool get reused too, but only over time. "Reuse" in the context of an object pool means reclaiming the memory for an object *after* the original owner is done with it. With an object pool, there isn't any expectation that an object will be shared within its lifetime.

池中的对象也被复用，但只在时间上复用。在对象池的上下文中，"复用"意味着在原始所有者使用完毕后*回收*对象的内存。对于对象池，不存在对象在其生命周期内被共享的预期。

- Packing a bunch of objects of the same type together in memory helps keep your CPU cache full as the game iterates over those objects. The [Data Locality](data-locality.html) pattern is all about that.

- 将同一类型的多个对象紧凑地打包在内存中，有助于在游戏遍历这些对象时保持CPU缓存命中率。数据局部性模式正是关于这一点的。
---

## C# Equivalent / C# 对照实现

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 对象池模式 C# 实现
/// 
/// 核心概念：
/// 1. 预分配：创建时一次分配所有对象，避免运行时频繁 new
/// 2. 复用：用完后归还池中，下次直接激活而非新建
/// 3. 避免 GC：减少堆分配次数，降低 GC 触发频率
/// 
/// 为什么对象池在 Unity/C# 中特别重要？
/// - Unity 使用 Mono/IL2CPP 运行时，GC 是帧率的头号杀手
/// - 频繁的 new/delete 会导致堆碎片化和 GC 暂停
/// - 对象池将堆分配次数从 O(n) 降低到 O(1)
/// </summary>

// —— 简单的对象池组件 ——
// 适用于管理 MonoBehaviour 对象

public class SimpleObjectPool : MonoBehaviour
{
    [Header("池设置")]
    [SerializeField] private GameObject prefab;           // 对象预制体
    [SerializeField] private int initialPoolSize = 20;    // 初始池大小
    [SerializeField] private int maxPoolSize = 100;       // 最大池大小（防止内存无限增长）
    [SerializeField] private bool canExpand = true;       // 是否允许池扩容

    // 使用 Stack 存储空闲对象
    // Stack 是后进先出（LIFO）集合
    // LIFO 有利于缓存局部性：最近使用的对象更可能仍在 CPU 缓存中
    private Stack<GameObject> availableObjects = new Stack<GameObject>();

    // 可选：跟踪所有池化对象，便于调试
    private List<GameObject> allPooledObjects = new List<GameObject>();

    void Awake()
    {
        // ★ 预分配：在游戏开始前一次性创建所有对象
        // 这样做的好处：
        // 1. 避免运行时突然的 GC 暂停
        // 2. 分配集中在一个时间段，内存更稳定
        // 3. 预热阶段可以显示加载进度条
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewObject();
        }
    }

    /// <summary>
    /// 创建一个新对象并加入池中
    /// 使用 Instantiate 分配新的游戏对象
    /// </summary>
    private GameObject CreateNewObject()
    {
        if (allPooledObjects.Count >= maxPoolSize)
        {
            Debug.LogWarning("对象池已达到最大容量：" + maxPoolSize);
            return null;
        }

        // Instantiate 是 Unity 中分配 GameObject 的方法
        // 它涉及托管堆分配和非托管资源分配
        // 这就是为什么我们应该尽量复用而非重复 Instantiate
        GameObject newObj = Instantiate(prefab);
        newObj.transform.SetParent(transform);

        // 添加到空闲栈
        availableObjects.Push(newObj);
        allPooledObjects.Add(newObj);

        // 默认设置为禁用状态
        // 禁用而非销毁，下次使用时只需重新激活
        newObj.SetActive(false);

        return newObj;
    }

    /// <summary>
    /// 从对象池中获取一个对象
    /// 
    /// 查找策略：
    /// 1. 优先从空闲栈中弹出（O(1) 复杂度）
    /// 2. 若栈为空且允许扩容，则创建新对象
    /// 3. 否则返回 null（调用方应处理此情况）
    /// </summary>
    public GameObject Get()
    {
        // 尝试从空闲栈获取
        // Stack.Pop() 是 O(1) 操作，非常高效
        if (availableObjects.Count > 0)
        {
            GameObject obj = availableObjects.Pop();
            obj.SetActive(true);  // 激活对象
            return obj;
        }

        // 栈为空，尝试扩容
        if (canExpand)
        {
            GameObject newObj = CreateNewObject();
            if (newObj != null)
            {
                newObj.SetActive(true);
                return newObj;
            }
        }

        // 池已满且不可扩容
        // 策略：返回 null（调用方决定是忽略还是复用最旧的）
        Debug.LogWarning("对象池已耗尽！");
        return null;
    }

    /// <summary>
    /// 将对象归还到对象池
    /// 
    /// 归还时应：
    /// 1. 禁用对象（避免仍在场景中渲染/交互）
    /// 2. 重置对象状态（确保下次取用时状态干净）
    /// 3. 压入空闲栈（LIFO 顺序）
    /// </summary>
    public void Return(GameObject obj)
    {
        obj.SetActive(false);  // 禁用
        availableObjects.Push(obj);  // 放回空闲栈
    }

    /// <summary>
    /// 清空对象池（场景切换时调用）
    /// </summary>
    public void Clear()
    {
        foreach (var obj in allPooledObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        availableObjects.Clear();
        allPooledObjects.Clear();
    }

    void OnDestroy()
    {
        Clear();
    }
}

// —— 可以在池化对象上使用的组件 ——
// 对象被复用时应重置自身状态

public class PooledObject : MonoBehaviour
{
    private SimpleObjectPool pool;  // 所属的对象池引用

    /// <summary>
    /// 设置所属池（由对象池在 Get 时调用）
    /// </summary>
    public void SetPool(SimpleObjectPool ownerPool)
    {
        pool = ownerPool;
    }

    /// <summary>
    /// 归还到池中（由外部触发，如动画结束、碰撞检测等）
    /// 
    /// 应替换 Destroy(gameObject) 调用
    /// </summary>
    public void ReturnToPool()
    {
        if (pool != null)
        {
            // 重置状态
            ResetState();
            pool.Return(gameObject);
        }
        else
        {
            // 如果没有关联池，回退到销毁（安全降级）
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 重置对象状态
    /// 子类应重写此方法以清理自己的状态
    /// 
    /// ★ 重要：必须完全重置状态！
    /// 否则可能出现"幽灵状态"问题
    /// （例如：粒子发射器重复使用上一次的尾迹）
    /// </summary>
    protected virtual void ResetState()
    {
        // 例如：重置位置、旋转、速度等
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // 清理子对象
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 使用对象池分配时调用（替代 OnEnable）
    /// 用于设置对象的初始状态
    /// </summary>
    public virtual void OnSpawn()
    {
        // 子类在此设置初始化状态
    }
}

// —— 高级对象池 ——
// 使用泛型支持任意类型，并支持预热和运行时统计

public class AdvancedObjectPool<T> where T : Component
{
    private Stack<T> available = new Stack<T>();     // 空闲对象栈
    private List<T> inUse = new List<T>();           // 使用中对象列表（用于调试）
    private T prefab;                                // 预制体引用
    private Transform parentTransform;               // 父级 Transform
    private int maxSize;                             // 最大容量

    /// <summary>
    /// 池统计信息（用于性能监控）
    /// </summary>
    public int AvailableCount => available.Count;
    public int InUseCount => inUse.Count;
    public int TotalCount => available.Count + inUse.Count;

    public AdvancedObjectPool(T prefab, int initialSize, 
                              int maxSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.maxSize = maxSize;
        this.parentTransform = parent;

        // 预热：创建所有初始对象
        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateNewObject();
            if (obj != null)
            {
                obj.gameObject.SetActive(false);
                available.Push(obj);
            }
        }

        Debug.Log($"对象池预热完成: {typeof(T).Name} x {initialSize}");
    }

    /// <summary>
    /// 创建一个新实例（使用 Object.Instantiate）
    /// </summary>
    private T CreateNewObject()
    {
        T obj = Object.Instantiate(prefab, parentTransform);

        // 添加自动归还组件（当 OnDisable 时自动归还）
        var autoReturn = obj.gameObject.AddComponent<AutoReturn>();
        autoReturn.Initialize(this);

        return obj;
    }

    /// <summary>
    /// 从池中获取一个对象
    /// </summary>
    public T Get()
    {
        T obj;

        if (available.Count > 0)
        {
            obj = available.Pop();
        }
        else if (TotalCount < maxSize)
        {
            obj = CreateNewObject();
        }
        else
        {
            return null;  // 池已满
        }

        obj.gameObject.SetActive(true);
        inUse.Add(obj);

        // 调用 OnSpawn 方法（如果实现了 IPoolable 接口）
        if (obj is IPoolable poolable)
        {
            poolable.OnSpawn();
        }

        return obj;
    }

    /// <summary>
    /// 归还对象到池中
    /// </summary>
    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        available.Push(obj);
        inUse.Remove(obj);

        // 调用 OnDespawn 方法
        if (obj is IPoolable poolable)
        {
            poolable.OnDespawn();
        }
    }
}

// —— 池化对象接口 ——
// 实现此接口的对象可以在获取/归还时获得通知

public interface IPoolable
{
    void OnSpawn();     // 从池中取出时调用
    void OnDespawn();   // 归还到池中时调用
}

// —— 自动归还组件 ——
// 当 GameObject 被禁用时自动归还到池

public class AutoReturn : MonoBehaviour
{
    private AdvancedObjectPool<Component> pool;

    public void Initialize<T>(AdvancedObjectPool<T> pool) where T : Component
    {
        // 使用反射/接口适配
    }

    void OnDisable()
    {
        // 当对象被禁用时，自动归还到池
        // 这样外部只需调用 gameObject.SetActive(false) 即可
    }
}

// —— Unity 官方 ObjectPool（2021+） ——
// Unity 2021 引入了内置对象池 API
// 位于 UnityEngine.Pool 命名空间
//
// 使用方式：
// using UnityEngine.Pool;
//
// var pool = new ObjectPool<MyClass>(
//     createFunc: () => new MyClass(),
//     actionOnGet: (obj) => obj.Init(),
//     actionOnRelease: (obj) => obj.Reset(),
//     actionOnDestroy: (obj) => Destroy(obj),
//     collectionCheck: true,
//     defaultCapacity: 10,
//     maxSize: 100
// );
//
// var item = pool.Get();       // 获取
// pool.Release(item);         // 归还
// pool.Clear();               // 清空

// —— 使用示例 ——
public class ObjectPoolUsageExample : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    private SimpleObjectPool bulletPool;

    void Start()
    {
        // 创建对象池
        bulletPool = gameObject.AddComponent<SimpleObjectPool>();
        // bulletPool.prefab = bulletPrefab;  // 实际通过 Inspector 设置

        // 模拟发射子弹
        for (int i = 0; i < 5; i++)
        {
            FireBullet();
        }
    }

    void FireBullet()
    {
        // 从池中获取子弹
        GameObject bullet = bulletPool.Get();
        if (bullet != null)
        {
            // 设置位置
            bullet.transform.position = transform.position + transform.forward * 2f;
            bullet.transform.forward = transform.forward;

            // 获取池化对象组件并激活
            var pooled = bullet.GetComponent<PooledObject>();
            pooled?.OnSpawn();
        }
    }

    // 当子弹碰撞后调用
    public void OnBulletHit(GameObject bullet)
    {
        var pooled = bullet.GetComponent<PooledObject>();
        pooled?.ReturnToPool();  // 归还而非销毁
    }
}
```

---

## Unity Application / Unity 应用场景

1. **Bullet/Projectile Systems**: The most common use. Bullets spawn and despawn rapidly; Object Pool eliminates GC from Instantiate/Destroy.
2. **Particle Systems (legacy)**: Custom particle systems using PooledObject for particle GameObject reuse.
3. **Audio Sources**: Pool AudioSource objects to avoid allocation spikes when playing many short sounds.
4. **Enemy Spawning**: Pool enemy GameObjects in waves to avoid loading spikes.
5. **Unity 2021+ Built-in Pool**: `UnityEngine.Pool.ObjectPool<T>` is the official API. Use `CollectionPool<T>` for List/Dictionary reuse.
6. **UI Object Pooling**: Reuse UI elements (list items, buttons) in scroll views and dynamic lists.

1. **子弹/抛射物系统**: 最常见的用途。子弹快速生成和销毁；对象池消除了 Instantiate/Destroy 带来的 GC。
2. **粒子系统（传统）**: 使用 PooledObject 实现自定义粒子系统的 GameObject 复用。
3. **音频源**: 池化 AudioSource 对象，避免大量短音效播放时的分配峰值。
4. **敌人生成**: 按波次池化敌人 GameObject，避免加载峰值。
5. **Unity 2021+ 内置对象池**: `UnityEngine.Pool.ObjectPool<T>` 是官方 API。使用 `CollectionPool<T>` 复用 List/Dictionary。
6. **UI 对象池**: 在滚动视图和动态列表中复用 UI 元素（列表项、按钮）。
---

## Key Differences / 关键差异

| Aspect | C++ (原书) | C# (Unity) |
|--------|-----------|------------|
| Allocation | `new` / `delete` on heap | `Instantiate` / `Destroy` + managed heap GC |
| Fragmentation risk | High (manual heap) | Low (compact GC, but stop-the-world pauses) |
| GC impact | None (manual) | 关键：减少 GC Allocation 避免帧率尖刺 |
| Pool structure | Fixed array or linked list with `union` | `Stack<T>` or `Queue<T>` + `List<T>` tracking |
| Free list optimization | `union` + pointer chaining | `Stack<T>` (LIFO) built-in |
| Object state tracking | `inUse()` method from `framesLeft_` | `gameObject.activeSelf` or custom `IPoolable` |
| Built-in support | None | `UnityEngine.Pool.ObjectPool<T>` since 2021 |
| Best practice | Manual memory layout control | Interface-based with `IPoolable.OnSpawn/OnDespawn` |

---

# Spatial Partition — 空间分区

> **EN:** Efficiently locate objects by storing them in a data structure organized by their positions.
> **CN:** 通过将对象按位置存储到空间数据结构中，实现高效的位置查询。

## Intent / 意图

* *Efficiently locate objects by storing them in a data structure organized by their positions.*

* 通过将对象存储到按位置组织的数据结构中，来实现高效地定位对象。

## Motivation / 动机

* Games let us visit other worlds, but those worlds typically aren't so different from our own. They often share the same basic physics and tangibility of our universe. This is why they can feel real despite being crafted of mere bits and pixels.

One bit of fake reality that we'll focus on here is *location*. Game worlds have a sense of *space*, and objects are somewhere in that space. This manifests itself in a bunch of ways. The obvious one is physics — objects move, collide, and interact — but there are other examples. The audio engine may take into account where sound sources are relative to the player so that distant sounds are quieter. Online chat may be restricted to nearby players.

This means your game engine often needs to answer to the question, "What objects are near this location?" If it has to answer this enough times each frame, it can start to be a performance bottleneck.

**CN:** 游戏让我们得以造访其他世界，但这些世界通常与我们自己的世界并没有太大不同。它们往往共享我们宇宙相同的基本物理和可触知性。这就是为什么尽管它们仅由比特和像素构成，却能让人感觉真实。

我们将要关注的虚拟现实的一个方面是*位置*。游戏世界有*空间*感，对象位于空间中的某处。这以多种方式体现出来。最明显的是物理——对象移动、碰撞和交互——但还有其他例子。音频引擎可能会考虑音源相对于玩家的位置，以便远处的声音更轻。在线聊天可能仅限于附近的玩家。

这意味着你的游戏引擎经常需要回答这样一个问题："哪些对象在这个位置附近？"如果它每帧需要回答这个问题足够多次，它就可能开始成为性能瓶颈。

### Units on the field of battle / 战场上的单位

* Say we're making a real-time strategy game. Opposing armies with hundreds of units will clash together on the field of battle. Warriors need to know which nearby enemy to swing their blades at. The naïve way to handle this is by looking at every pair of units and seeing how close they are to each other:

* 假设我们正在制作一款即时战略游戏。数百个单位组成的敌对军队将在战场上交锋。战士需要知道附近哪个敌人值得挥剑。处理此问题的朴素方式是查看每一对单位，看看它们彼此之间的距离：

```cpp
void handleMelee(Unit* units[], int numUnits) {
  for (int a = 0; a < numUnits - 1; a++)
  {
    for (int b = a + 1; b < numUnits; b++)
    {
      if (units[a]->position() == units[b]->position())
      {
        handleAttack(units[a], units[b]);
      }
    }
  }
}
```

* Here we have a doubly nested loop where each loop is walking all of the units on the battlefield. That means the number of pairwise tests we have to perform each frame increases with the *square* of the number of units. Each additional unit we add has to be compared to *all* of the previous ones. With a large number of units, that can spiral out of control.

The inner loop doesn't actually walk all of the units. It only walks the ones the outer loop hasn't already visited. This avoids comparing each pair of units *twice*, once in each order. If we've already handled a collision between A and B, we don't need to check it again for B and A.

In Big-O terms, though, this is still *O(n²)*.

**CN:** 这里我们有一个双重嵌套循环，每个循环遍历战场上的所有单位。这意味着每帧我们必须执行的对比较数量随单位数量的*平方*增长。每增加一个单位，都必须与*所有*之前的单位进行比较。当单位数量庞大时，这可能失控。

内部循环实际上并不遍历所有单位。它只遍历外层循环尚未访问过的单位。这避免了以两种顺序分别比较每对单位*两次*。如果我们已经处理了 A 和 B 之间的碰撞，就不需要再为 B 和 A 检查一次。

不过，用大 O 术语来说，这仍然是 *O(n²)*。

### Drawing battle lines / 绘制战线

* The problem we're running into is that there's no underlying order to the array of units. To find a unit near some location, we have to walk the entire array. Now, imagine we simplify our game a bit. Instead of a 2D battle*field*, imagine it's a 1D battle*line*.

* 我们遇到的问题在于单位数组没有内在的顺序。要查找某个位置附近的单位，我们必须遍历整个数组。现在，想象我们稍微简化一下游戏。不是二维战场，而是一维战线。

> ![A number line with Units positioned at different coordinates on it.](images/spatial-partition-battle-line.png)

* In that case, we could make things easier on ourselves by *sorting* the array of units by their positions on the battleline. Once we do that, we can use something like a [binary search](http://en.wikipedia.org/wiki/Binary_search) to find nearby units without having to scan the entire array.

A binary search has *O(log n)* complexity, which means find all battling units goes from *O(n²)* to *O(n log n)*. Something like a [pigeonhole sort](http://en.wikipedia.org/wiki/Pigeonhole_sort) could get that down to *O(n)*.

The lesson is pretty obvious: if we store our objects in a data structure organized by their locations, we can find them much more quickly. This pattern is about applying that idea to spaces that have more than one dimension.

**CN:** 在这种情况下，我们可以通过*排序*单位数组（按它们在战线上的位置）来简化问题。一旦完成排序，我们可以使用[二分搜索](http://en.wikipedia.org/wiki/Binary_search)之类的方法来查找附近的单位，而无需扫描整个数组。

二分搜索的复杂度为 *O(log n)*，这意味着查找所有交战单位从 *O(n²)* 降到 *O(n log n)*。像[鸽巢排序](http://en.wikipedia.org/wiki/Pigeonhole_sort)这样的方法可以将其降至 *O(n)*。

教训显而易见：如果我们将对象存储在按位置组织的数据结构中，就可以更快地找到它们。这个模式正是关于将该理念应用于具有多个维度的空间。

## The Pattern / 模式定义

* For a set of **objects**, each has a **position in space**. Store them in a **spatial data structure** that organizes the objects by their positions. This data structure lets you **efficiently query for objects at or near a location**. When an object's position changes, **update the spatial data structure** so that it can continue to find the object.

* 对于一组**对象**，每个对象都有一个**空间位置**。将它们存储在一个**按位置组织对象**的**空间数据结构**中。该数据结构使你能够**高效地查询位于某位置或其附近的对象**。当对象的位置改变时，**更新空间数据结构**，使其能继续定位该对象。

## When to Use It / 何时使用

* This is a common pattern for storing both live, moving game objects and also the static art and geometry of the game world. Sophisticated games often have multiple spatial partitions for different kinds of content.

The basic requirements for this pattern are that you have a set of objects that each have some kind of position and that you are doing enough queries to find objects by location that your performance is suffering.

**CN:** 这是一个常见的模式，既用于存储活动的、移动的游戏对象，也用于存储游戏世界的静态美术和几何体。复杂的游戏通常针对不同类型的内容使用多种空间分区。

此模式的基本要求是：你有一组对象，每个对象都有某种位置，并且你进行了足够多的按位置查找对象的查询，以致性能受到影响。

## Keep in Mind / 记住

* Spatial partitions exist to knock an *O(n)* or *O(n²)* operation down to something more manageable. The *more* objects you have, the more valuable that becomes. Conversely, if your *n* is small enough, it may not be worth the bother.

Since this pattern involves organizing objects by their positions, objects that *change* position are harder to deal with. You'll have to reorganize the data structure to keep track of an object at a new location, and that adds code complexity *and* spends CPU cycles. Make sure the trade-off is worth it.

Imagine a hash table where the keys of the hashed objects can change spontaneously, and you'll have a good feel for why it's tricky.

A spatial partition also uses additional memory for its bookkeeping data structures. Like many optimizations, it trades memory for speed. If you're shorter on memory than you are on clock cycles, that may be a losing proposition.

**CN:** 空间分区存在的意义是将 *O(n)* 或 *O(n²)* 操作降至更可管理的程度。你拥有的对象*越多*，它的价值就越大。反之，如果你的 *n* 足够小，可能就不值得费心了。

由于此模式涉及按位置组织对象，*改变*位置的对象更难处理。你必须重新组织数据结构以跟踪新位置的对象，这会增加代码复杂度*并*消耗 CPU 周期。确保这种权衡是值得的。

想象一个哈希表，其中被哈希对象的键可以自发改变，你就会对为什么它很棘手有切身体会。

空间分区还需要额外的内存来维护其记账数据结构。像许多优化一样，它用内存换取速度。如果你更缺内存而非时钟周期，这可能是一个亏本的选择。

## Sample Code / 示例代码

* The nature of patterns is that they *vary* — each implementation will be a bit different, and spatial partitions are no exception. Unlike other patterns, though, many of these variations are well-documented. Academia likes publishing papers that prove performance gains. Since I only care about the concept behind the pattern, I'm going to show you the simplest spatial partition: a *fixed grid*.

See the last section of this chapter for a list of some of the most common spatial partitions used in games.

**CN:** 模式的本质在于它们会*变化*——每个实现都会略有不同，空间分区也不例外。不过与其他模式不同，这些变体中的许多都有详尽的文档记录。学术界喜欢发表证明性能提升的论文。由于我只关心模式背后的概念，我将向你展示最简单的空间分区：*固定网格*。

请参阅本章最后一节，了解游戏中使用的一些最常见的空间分区列表。

### A sheet of graph paper / 一张方格纸

* Imagine the entire field of battle. Now, superimpose a grid of fixed-size squares onto it like a sheet of graph paper. Instead of storing our units in a single array, we put them in the cells of this grid. Each cell stores the list of units whose positions are within that cell's boundary.

* 想象整个战场。现在，像一张方格纸一样，在上面叠加一个固定大小的正方形网格。我们不再将单位存储在单个数组中，而是将它们放入此网格的单元格中。每个单元格存储位置在该单元格边界内的单位列表。

> ![A grid with Units occupying different cells. Some cells have multiple Units.](images/spatial-partition-grid.png)

* When we handle combat, we only consider units within the same cell. Instead of comparing each unit in the game with every other unit, we've *partitioned* the battlefield into a bunch of smaller mini-battlefields, each with many fewer units.

* 处理战斗时，我们只考虑同一单元格内的单位。我们不再将游戏中的每个单位与其他所有单位比较，而是将战场*划分*为许多较小的迷你战场，每个战场中的单位少得多。

### A grid of linked units / 链式链接的网格

* OK, let's get coding. First, some prep work. Here's our basic `Unit` class:

* 好了，开始编码。首先，一些准备工作。这是我们的基本 `Unit` 类：

```cpp
class Unit {
  friend class Grid;
public:
  Unit(Grid* grid, double x, double y)
  : grid_(grid),
    x_(x),
    y_(y) {}

  void move(double x, double y);
private:
  double x_, y_;
  Grid* grid_;
};
```

* Each unit has a position (in 2D) and a pointer to the `Grid` that it lives on. We make `Grid` a `friend` class because, as we'll see, when a unit's position changes, it has to do an intricate dance with the grid to make sure everything is updated correctly.

Here's a sketch of the grid:

**CN:** 每个单位有一个位置（二维）和一个指向它所处 `Grid` 的指针。我们将 `Grid` 设为 `friend` 类，因为正如我们即将看到的，当单位的位置改变时，它需要与网格进行复杂的配合，以确保一切正确更新。

以下是网格的草图：

```cpp
class Grid {
public:
  Grid()
  {
    // Clear the grid.
    for (int x = 0; x < NUM_CELLS; x++)
    {
      for (int y = 0; y < NUM_CELLS; y++)
      {
        cells_[x][y] = NULL;
      }
    }
  }

  static const int NUM_CELLS = 10;
  static const int CELL_SIZE = 20;
private:
  Unit* cells_[NUM_CELLS][NUM_CELLS];
};
```

* Note that each cell is just a pointer to a unit. Next, we'll extend `Unit` with `next` and `prev` pointers:

* 注意每个单元格只是一个指向单位的指针。接下来，我们将用 `next` 和 `prev` 指针扩展 `Unit`：

```cpp
class Unit {
  // Previous code...
private:
  Unit* prev_;
  Unit* next_;
};
```

* This lets us organize units into a [doubly linked list](http://en.wikipedia.org/wiki/Doubly_linked_list) instead of an array.

* 这使我们能够将单位组织成[双向链表](http://en.wikipedia.org/wiki/Doubly_linked_list)而不是数组。

> ![A Cell pointing to a a doubly linked list of Units.](images/spatial-partition-linked-list.png)

* Each cell in the grid points to the first unit in the list of units within that cell, and each unit has pointers to the units before it and after it in the list. We'll see why soon.

Throughout this book, I've avoided using any of the built-in collection types in the C++ standard library. I want to require as little external knowledge as possible to understand the example, and, like a magician's "nothing up my sleeve", I want to make it clear *exactly* what's going on in the code. Details are important, especially with performance-related patterns.

But this is my choice for *explaining* patterns. If you're *using* them in real code, spare yourself the headache and use the fine collections built into pretty much every programming language today. Life's too short to code linked lists from scratch.

**CN:** 网格中的每个单元格指向该单元格内单位列表的第一个单位，每个单位有指向前一个和后一个单位的指针。我们很快就会看到为什么这样做。

在整本书中，我避免使用 C++ 标准库中的任何内置集合类型。我希望尽可能少地需要外部知识来理解示例，并且像魔术师的"袖中无物"一样，我想清楚地展示代码中*到底*发生了什么。细节很重要，尤其是对于性能相关模式。

但这是我*解释*模式的选择。如果你在实际代码中*使用*它们，请省去头痛，使用现今几乎每种编程语言都内置的优秀集合。人生苦短，不要从头编写链表。

### Entering the field of battle / 进入战场

* The first thing we need to do is make sure new units are actually placed into the grid when they are created. We'll make `Unit` handle this in its constructor:

* 我们需要做的第一件事是确保新单位在创建时实际放入网格中。我们让 `Unit` 在其构造函数中处理这个：

```cpp
Unit::Unit(Grid* grid, double x, double y)
: grid_(grid),
  x_(x),
  y_(y),
  prev_(NULL),
  next_(NULL)
{
  grid_->add(this);
}
```

* This `add()` method is defined like so:

* 这个 `add()` 方法定义如下：

```cpp
void Grid::add(Unit* unit) {
  // Determine which grid cell it's in.
  int cellX = (int)(unit->x_ / Grid::CELL_SIZE);
  int cellY = (int)(unit->y_ / Grid::CELL_SIZE);

  // Add to the front of list for the cell it's in.
  unit->prev_ = NULL;
  unit->next_ = cells_[cellX][cellY];
  cells_[cellX][cellY] = unit;

  if (unit->next_ != NULL)
  {
    unit->next_->prev_ = unit;
  }
}
```

* Dividing by the cell size converts world coordinates to cell space. Then, casting to an `int` truncates the fractional part so we get the cell index.

It's a little finicky like linked list code always is, but the basic idea is pretty simple. We find the cell that the unit is sitting in and then add it to the front of that list. If there is already a list of units there, we link it in after the new unit.

**CN:** 除以单元格大小将世界坐标转换为单元格空间。然后，转换为 `int` 截断小数部分，从而得到单元格索引。

这有点像链表代码那样有点繁琐，但基本思想非常简单。我们找到单位所在的单元格，然后将其添加到该列表的前端。如果那里已有一个单位列表，我们将其链接到新单位之后。

### A clash of swords / 剑刃交锋

* Once all of the units are nestled in their cells, we can let them start hacking at each other. With this new grid, the main method for handling combat looks like this:

* 一旦所有单位都安放在各自的单元格中，我们就可以让它们开始互相砍杀了。使用这个新网格，处理战斗的主要方法如下：

```cpp
void Grid::handleMelee() {
  for (int x = 0; x < NUM_CELLS; x++)
  {
    for (int y = 0; y < NUM_CELLS; y++)
    {
      handleCell(cells_[x][y]);
    }
  }
}
```

* It walks each cell and then calls `handleCell()` on it. As you can see, we really have partitioned the battlefield into little isolated skirmishes. Each cell then handles its combat like so:

* 它遍历每个单元格然后对其调用 `handleCell()`。如你所见，我们确实将战场划分成了小的孤立冲突。然后每个单元格像这样处理其战斗：

```cpp
void Grid::handleCell(Unit* unit) {
  while (unit != NULL)
  {
    Unit* other = unit->next_;
    while (other != NULL)
    {
      if (unit->x_ == other->x_ &&
          unit->y_ == other->y_)
      {
        handleAttack(unit, other);
      }
      other = other->next_;
    }

    unit = unit->next_;
  }
}
```

* Aside from the pointer shenanigans to deal with walking a linked list, note that this is exactly like our original naïve method for handling combat. It compares each pair of units to see if they're in the same position.

The only difference is that we no longer have to compare *all* of the units in the battle to each other — just the ones close enough to be in the same cell. That's the heart of the optimization.

From a simple analysis, it looks like we've actually made the performance *worse*. We've gone from a doubly nested loop over the units to a *triply* nested loop over the cells and then the units. The trick here is that the two inner loops are now over a smaller number of units, which is enough to cancel out the cost of the outer loop over the cells.

However, that does depend a bit on the granularity of our cells. Make them too small and that outer loop can start to matter.

**CN:** 除了处理链表遍历的指针技巧外，注意这与我们最初处理战斗的朴素方法完全相同。它比较每对单位，看它们是否在相同位置。

唯一的区别是，我们不再需要比较战场中*所有*单位彼此之间的关系——只需比较足够接近、处于同一单元格中的单位。这就是优化的核心。

从简单的分析来看，我们实际上使性能*更糟*了。我们从对单位的双重嵌套循环变成了对单元格和单位的*三重*嵌套循环。关键在于两个内部循环现在处理的单位数量更少，这足以抵消外部单元格循环的成本。

然而，这确实在一定程度上取决于我们单元格的粒度。如果单元格太小，外部循环可能会变得重要。

### Charging forward / 向前冲锋

* We've solved our performance problem, but we've created a new problem in its stead. Units are now stuck in their cells. If we move a unit past the boundary of the cell that contains it, units in the cell won't see it anymore, but neither will anyone else. Our battlefield is a little *too* partitioned.

To fix that, we'll need to do a little work each time a unit moves. If it crosses a cell's boundary lines, we need to remove it from that cell and add it to the new one. First, we'll give `Unit` a method for changing its position:

**CN:** 我们解决了性能问题，但又创造了一个新问题。单位现在被困在它们的单元格里了。如果我们将一个单位移出其所在单元格的边界，该单元格中的单位将不再能看到它，但其他任何人也看不到。我们的战场有点*过于*分区了。

为了解决这个问题，我们需要在单位每次移动时做一些工作。如果它跨越了单元格的边界线，我们需要将其从原单元格中移除，并添加到新单元格中。首先，我们给 `Unit` 一个更改其位置的方法：

```cpp
void Unit::move(double x, double y) {
  grid_->move(this, x, y);
}
```

* Presumably, this gets called by the AI code for computer-controlled units and by the user input code for the player's units. All it does is hand off control to the grid, which then does:

* 可以推断，这个方法由计算机控制单位的 AI 代码和玩家单位的用户输入代码调用。它所做的只是将控制权移交给网格，然后网格会执行：

```cpp
void Grid::move(Unit* unit, double x, double y) {
  // See which cell it was in.
  int oldCellX = (int)(unit->x_ / Grid::CELL_SIZE);
  int oldCellY = (int)(unit->y_ / Grid::CELL_SIZE);

  // See which cell it's moving to.
  int cellX = (int)(x / Grid::CELL_SIZE);
  int cellY = (int)(y / Grid::CELL_SIZE);

  unit->x_ = x;
  unit->y_ = y;

  // If it didn't change cells, we're done.
  if (oldCellX == cellX && oldCellY == cellY) return;

  // Unlink it from the list of its old cell.
  if (unit->prev_ != NULL)
  {
    unit->prev_->next_ = unit->next_;
  }

  if (unit->next_ != NULL)
  {
    unit->next_->prev_ = unit->prev_;
  }

  // If it's the head of a list, remove it.
  if (cells_[oldCellX][oldCellY] == unit)
  {
    cells_[oldCellX][oldCellY] = unit->next_;
  }

  // Add it back to the grid at its new cell.
  add(unit);
}
```

* That's a mouthful of code, but it's pretty straightforward. The first bit checks to see if we've crossed a cell boundary at all. If not, all we need to do is update the unit's position and we're done.

If the unit *has* left its current cell, we remove it from that cell's linked list and then add it back to the grid. Like with adding a new unit, that will insert the unit in the linked list for its new cell.

This is why we're using a doubly linked list — we can very quickly add and remove units from lists by setting a few pointers. With lots of units moving around each frame, that can be important.

**CN:** 代码有点长，但相当直接。第一部分检查我们是否完全越过了单元格边界。如果没有，我们只需要更新单位的位置就完成了。

如果单位*确实*离开了其当前单元格，我们将其从该单元格的链表中移除，然后将其重新添加到网格中。就像添加新单位一样，这会将单位插入到其新单元格的链表中。

这就是我们使用双向链表的原因——通过设置几个指针，我们可以非常快速地从列表中添加和移除单位。当每帧有大量单位移动时，这可能很重要。

### At arm's length / 在攻击范围内

* This seems pretty simple, but I have cheated in one way. In the example I've been showing, units only interact when they have the *exact same* position. That's true for checkers and chess, but less true for more realistic games. Those usually have attack *distances* to take into account.

This pattern still works fine. Instead of just checking for an exact location match, we'll do something more like:

**CN:** 这看起来很简单，但我在一个方面作弊了。在我展示的示例中，单位只有在具有*完全相同*位置时才交互。这对跳棋和国际象棋来说是对的，但对于更真实的游戏来说就不太对了。这些游戏通常需要考虑攻击*距离*。

这个模式仍然适用。我们不只检查精确位置匹配，而是会做类似这样的事情：

```cpp
if (distance(unit, other) < ATTACK_DISTANCE) {
  handleAttack(unit, other);
}
```

* When range gets involved, there's a corner case we need to consider: units in different cells may still be close enough to interact.

* 当涉及到范围时，我们需要考虑一个边界情况：不同单元格中的单位可能仍然足够接近以进行交互。

> ![Two Units in adjacent Cells are close enough to interact.](images/spatial-partition-adjacent.png)

* Here, B is within A's attack radius even through their centerpoints are in different cells. To handle this, we will need to compare units not only in the same cell, but in neighboring cells too. To do this, first we'll split the inner loop out of `handleCell()`:

* 这里，即使 B 和 A 的中心点位于不同单元格，B 也在 A 的攻击半径内。为了处理这种情况，我们不仅需要比较同一单元格中的单位，还需要比较相邻单元格中的单位。为此，我们首先将内部循环从 `handleCell()` 中拆分出来：

```cpp
void Grid::handleUnit(Unit* unit, Unit* other) {
  while (other != NULL)
  {
    if (distance(unit, other) < ATTACK_DISTANCE)
    {
      handleAttack(unit, other);
    }

    other = other->next_;
  }
}
```

* Now we have a function that will take a single unit and a list of other units and see if there are any hits. Then we'll make `handleCell()` use that:

* 现在我们有了一个函数，它接收一个单位和一组其他单位的列表，并查看是否有任何命中。然后我们让 `handleCell()` 使用它：

```cpp
void Grid::handleCell(int x, int y) {
  Unit* unit = cells_[x][y];
  while (unit != NULL)
  {
    // Handle other units in this cell.
    handleUnit(unit, unit->next_);

    unit = unit->next_;
  }
}
```

* Note that we now also pass in the coordinates of the cell, not just its unit list. Right now, this doesn't do anything differently from the previous example, but we'll expand it slightly:

* 注意我们现在还传入了单元格的坐标，而不仅仅是它的单位列表。目前，这与前面的示例没有什么不同，但我们将稍作扩展：

```cpp
void Grid::handleCell(int x, int y) {
  Unit* unit = cells_[x][y];
  while (unit != NULL)
  {
    // Handle other units in this cell.
    handleUnit(unit, unit->next_);

    // Also try the neighboring cells.
    if (x > 0 && y > 0) handleUnit(unit, cells_[x - 1][y - 1]);
    if (x > 0) handleUnit(unit, cells_[x - 1][y]);
    if (y > 0) handleUnit(unit, cells_[x][y - 1]);
    if (x > 0 && y < NUM_CELLS - 1)
    {
      handleUnit(unit, cells_[x - 1][y + 1]);
    }

    unit = unit->next_;
  }
}
```

* Those additional `handleUnit()` calls look for hits between the current unit and units in four of the eight neighboring cells. If any unit in those neighboring cells is close enough to the edge to be within the unit's attack radius, it will find the hit.

* 这些额外的 `handleUnit()` 调用查找当前单位与八个相邻单元格中四个单元格内的单位之间的命中。如果这些相邻单元格中的任何单位足够靠近边界，处于该单位的攻击半径内，它将找到命中。

> ![The set of neighbors for a Cell with the four being considered highlighted.](images/spatial-partition-neighbors.png)

* The cell with the unit is `U`, and the neighboring cells it looks at are `X`.

We only look at *half* of the neighbors for the same reason that the inner loop starts *after* the current unit — to avoid comparing each pair of units twice. Consider what would happen if we did check all eight neighboring cells.

Let's say we have two units in adjacent cells close enough to hit each other, like the previous example. Here's what would happen if we looked at all eight cells surrounding each unit:

1. When finding hits for A, we would look at its neighbor on the right and find B. So we'd register an attack between A and B.
2. Then, when finding hits for B, we would look at its neighbor on the *left* and find A. So we'd register a *second* attack between A and B.

Only looking at half of the neighboring cells fixes that. *Which* half we look at doesn't matter at all.

There's another corner case we may need to consider too. Here, we're assuming the maximum attack distance is smaller than a cell. If we have small cells and large attack distances, we may need to scan a bunch of neighboring cells several rows out.

**CN:** 包含单位的单元格是 `U`，它查看的相邻单元格是 `X`。

我们只查看*一半*的邻居，原因与内部循环从当前单位*之后*开始相同——为了避免每对单位被比较两次。考虑一下如果我们确实检查所有八个相邻单元格会发生什么。

假设我们有两个单位在相邻单元格中，足够接近可以互相攻击，就像前面的例子。如果我们查看每个单位周围的所有八个单元格，会发生以下情况：

1. 当为 A 查找命中时，我们会查看其右侧的邻居并找到 B。于是我们记录 A 和 B 之间的攻击。
2. 然后，当为 B 查找命中时，我们会查看其*左侧*的邻居并找到 A。于是我们记录 A 和 B 之间的*第二次*攻击。

只查看一半的相邻单元格解决了这个问题。查看*哪一半*完全无关紧要。

我们可能还需要考虑另一个边界情况。这里，我们假设最大攻击距离小于一个单元格。如果我们有小的单元格和大的攻击距离，可能需要扫描多行相邻单元格。

## Design Decisions / 设计决策

* There's a relatively short list of well-defined spatial partitioning data structures, and one option would be to go through them one at a time here. Instead, I tried to organize this by their essential characteristics. My hope is that once you do learn about quadtrees and binary space partitions (BSPs) and the like, this will help you understand *how* and *why* they work and why you might choose one over the other.

* 定义明确的空间分区数据结构列表相对较短，一个选择是这里逐个介绍它们。相反，我尝试按它们的基本特征来组织。我的希望是，一旦你学习了四叉树和二叉空间分区（BSP）等，这将帮助你理解它们*如何*以及*为什么*工作，以及为什么你可能会选择其中一个而不是另一个。

### Is the partition hierarchical or flat? / 分区是层次性的还是平面的？

* Our grid example partitioned space into a single flat set of cells. In contrast, hierarchical spatial partitions divide the space into just a couple of regions. Then, if one of these regions still contains many objects, it's subdivided. This process continues recursively until every region has fewer than some maximum number of objects in it.

They usually split it in two, four, or eight — nice round numbers to a programmer.

**CN:** 我们的网格示例将空间划分为单一平面的单元格集合。相比之下，层次性空间分区将空间划分为几个区域。然后，如果这些区域之一仍然包含许多对象，它会被细分。这个过程递归地继续，直到每个区域中的对象数量少于某个最大数量。

它们通常分成两个、四个或八个——对程序员来说很好的整数。

- **If it's a flat partition:** / **如果是平面分区：**

  - *It's simpler.* Flat data structures are easier to reason about and simpler to implement.
  - *Memory usage is constant.* Since adding new objects doesn't require creating new partitions, the memory used by the spatial partition can often be fixed ahead of time.
  - *It can be faster to update when objects change their positions.* When an object moves, the data structure needs to be updated to find the object in its new location. With a hierarchical spatial partition, this can mean adjusting several layers of the hierarchy.

  - *更简单。* 平面数据结构更容易理解和实现。
  - *内存使用是固定的。* 由于添加新对象不需要创建新分区，空间分区使用的内存通常可以提前固定。
  - *当对象改变位置时更新可能更快。* 当对象移动时，需要更新数据结构以在新位置找到该对象。对于层次性空间分区，这可能意味着调整层次结构的多个层。

- **If it's hierarchical:** / **如果是层次性分区：**

  - *It handles empty space more efficiently.* Imagine in our earlier example if one whole side of the battlefield was empty. We'd have a large number of empty cells that we'd still have to allocate memory for and walk each frame. Since hierarchical space partitions don't subdivide sparse regions, a large empty space will remain a single partition. Instead of lots of little partitions to walk, there is a single big one.
  - *It handles densely populated areas more efficiently.* This is the other side of the coin: if you have a bunch of objects all clumped together, a non-hierarchical partition can be ineffective. You'll end up with one partition that has so many objects in it that you may as well not be partitioning at all. A hierarchical partition will adaptively subdivide that into smaller partitions and get you back to having only a few objects to consider at a time.

  - *更高效地处理空空间。* 想象在我们之前的例子中，如果战场的一整侧是空的。我们会有大量空单元格，但我们仍然必须为它们分配内存并每帧遍历。由于层次性空间分区不细分稀疏区域，大片的空空间将保持为单个分区。遍历一个大的分区，而不是许多小的分区。
  - *更高效地处理密集区域。* 这是硬币的另一面：如果你有一堆对象聚集在一起，非层次性分区可能效率低下。你最终会得到一个分区，其中包含太多对象，以至于你几乎等于没有分区。层次性分区将自适应地将其细分为更小的分区，使你每次只需考虑少量对象。

### Does the partitioning depend on the set of objects? / 分区是否依赖于对象集合？

* In our sample code, the grid spacing was fixed beforehand, and we slotted units into cells. Other partitioning schemes are adaptable — they pick partition boundaries based on the actual set of objects and where they are in the world.

The goal is have a *balanced* partitioning where each region has roughly the same number of objects in order to get the best performance. Consider in our grid example if all of the units were clustered in one corner of the battlefield. They'd all be in the same cell, and our code for finding attacks would regress right back to the original *O(n²)* problem that we're trying to solve.

**CN:** 在我们的示例代码中，网格间距是预先固定的，我们将单位放入单元格。其他分区方案是可适应的——它们根据实际对象集合及其在世界中的位置来选择分区边界。

目标是拥有一个*平衡的*分区，其中每个区域大致有相同数量的对象，以获得最佳性能。考虑在我们的网格示例中，如果所有单位都聚集在战场的一个角落。它们都会在同一个单元格中，我们查找攻击的代码将直接退化回我们试图解决的原始 *O(n²)* 问题。

- **If the partitioning is object-independent:** / **如果分区与对象无关：**

  - *Objects can be added incrementally.* Adding an object means finding the right partition and dropping it in, so you can do this one at a time without any performance issues.
  - *Objects can be moved quickly.* With fixed partitions, moving a unit means removing it from one and adding it to another. If the partition boundaries themselves change based on the set of objects, then moving one can cause a boundary to move, which can in turn cause lots of other objects to need to be moved to different partitions. This is directly analogous to sorted binary search trees like red-black trees or AVL trees: when you add a single item, you may end up needing to re-sort the tree and shuffle a bunch of nodes around.
  - *The partitions can be imbalanced.* Of course, the downside of this rigidity is that you have less control over your partitions being evenly distributed. If objects clump together, you get worse performance there while wasting memory in the empty areas.

  - *对象可以增量添加。* 添加对象意味着找到正确的分区并放入其中，因此可以一次一个地执行此操作，没有任何性能问题。
  - *对象可以快速移动。* 使用固定分区，移动单位意味着从一个分区移除并添加到另一个分区。如果分区边界本身根据对象集合改变，那么移动一个对象可能会导致边界移动，进而导致许多其他对象需要移动到不同的分区。这直接类似于排序的二叉搜索树，如红黑树或 AVL 树：当你添加单个项时，你可能最终需要重新排序树并混洗一堆节点。
  - *分区可能不平衡。* 当然，这种刚性的缺点是你对分区均匀分布的控制较少。如果对象聚集在一起，你在那里获得更差的性能，同时浪费空区域的内存。

- **If the partitioning adapts to the set of objects:** / **如果分区适应对象集合：**

  * Spatial partitions like BSPs and k-d trees split the world recursively so that each half contains about the same number of objects. To do this, you have to count how many objects are on each side when selecting the planes you partition along. Bounding volume hierarchies are another type of spatial partition that optimizes for the specific set of objects in the world.

  - *You can ensure the partitions are balanced.* This gives not just good performance, but *consistent* performance: if each partition has the same number of objects, you ensure that all queries in the world will take about the same amount of time. When you need to maintain a stable frame rate, this consistency may be more important than raw performance.
  - *It's more efficient to partition an entire set of objects at once.* When the *set* of objects affects where boundaries are, it's best to have all of the objects up front before you partition them. This is why these kinds of partitions are more frequently used for art and static geometry that stays fixed during the game.

  **CN:** 像 BSP 和 k-d 树这样的空间分区递归地分割世界，使得每一半包含大致相同数量的对象。为此，在选择分割平面时，你必须计算每侧有多少个对象。包围体层次结构是另一种空间分区，它针对世界中的特定对象集合进行了优化。

  - *你可以确保分区是平衡的。* 这不仅能提供良好的性能，还能提供*一致的*性能：如果每个分区有相同数量的对象，你可以确保世界中的所有查询将花费大致相同的时间。当需要保持稳定的帧率时，这种一致性可能比原始性能更重要。
  - *一次分区整个对象集合更高效。* 当对象*集合*影响边界位置时，最好在分区之前就拥有所有对象。这就是为什么这类分区更常用于在游戏期间保持固定的美术和静态几何体。

- **If the partitioning is object-independent, but *hierarchy* is object-dependent:** / **如果分区与对象无关，但*层次*与对象相关：**

  * One spatial partition deserves special mention because it has some of the best characteristics of both fixed partitions and adaptable ones: quadtrees.

  A quadtree partitions 2D space. Its 3D analogue is the *octree*, which takes a *volume* and partitions it into eight *cubes*. Aside from the extra dimension, it works the same as its flatter sibling.

  A quadtree starts with the entire space as a single partition. If the number of objects in the space exceeds some threshold, it is sliced into four smaller squares. The *boundaries* of these squares are fixed: they always slice space right in half.

  Then, for each of the four squares, we do the same process again, recursively, until every square has a small number of objects in it. Since we only recursively subdivide squares that have a high population, this partitioning adapts to the set of objects, but the partitions don't *move*.

  **CN:** 有一种空间分区值得特别提及，因为它兼具固定分区和自适应分区的一些最佳特性：四叉树。

  四叉树分割二维空间。它的三维类似物是*八叉树*，它将一个*体积*分割成八个*立方体*。除了额外的维度，它的工作方式与其二维兄弟相同。

  四叉树从整个空间作为一个单独的分区开始。如果空间中的对象数量超过某个阈值，它被分割成四个更小的正方形。这些正方形的*边界*是固定的：它们总是将空间正好从中间分割。

  然后，对于四个正方形中的每一个，我们递归地执行相同的过程，直到每个正方形中有少量对象。由于我们只递归细分人口稠密的正方形，这种分区适应对象集合，但分区不会*移动*。

  > ![A quadtree.](images/spatial-partition-quadtree.png)

  - *Objects can be added incrementally.* Adding a new object means finding the right square and adding it. If that bumps that square above the maximum count, it gets subdivided. The other objects in that square get pushed down into the new smaller squares. This requires a little work, but it's a *fixed* amount of effort: the number of objects you have to move will always be less than the maximum object count. Adding a single object can never trigger more than one subdivision.
  - *Objects can be moved quickly.* This, of course, follows from the above. "Moving" an object is just an add and a remove, and both of those are pretty quick with quadtrees.
  - *The partitions are balanced.* Since any given square will have less than some fixed maximum number of objects, even when objects are clustered together, you don't have single partitions with a huge pile of objects in them.

  - *对象可以增量添加。* 添加新对象意味着找到正确的正方形并添加它。如果这使该正方形的对象数超过最大计数，它会被细分。该正方形中的其他对象被推送到新的更小的正方形中。这需要一些工作，但工作量是*固定的*：你必须移动的对象数量总是小于最大对象计数。添加单个对象永远不会触发超过一次细分。
  - *对象可以快速移动。* 这当然是上述内容的推论。"移动"一个对象只是添加和移除，这两者在四叉树中都相当快。
  - *分区是平衡的。* 由于任何给定的正方形中的对象数量将小于某个固定的最大数量，即使对象聚集在一起，你也不会有一个分区中堆积大量对象。

### Are objects only stored in the partition? / 对象是否只存储在分区中？

* You can treat your spatial partition as *the* place where the objects in your game live, or you can consider it just a secondary cache to make look-up faster while also having another collection that directly holds the list of objects.

* 你可以将空间分区视为游戏对象的*唯一*存储位置，或者也可以将其仅视为一个辅助缓存，用于加速查找，同时还有一个直接持有对象列表的独立集合。

- **If it is the only place objects are stored:** / **如果它是唯一存储对象的地方：**

  - *It avoids the memory overhead and complexity of two collections.* Of course, it's always cheaper to store something once instead of twice. Also, if you have two collections, you have to make sure to keep them in sync. Every time an object is created or destroyed, it has to be added or removed from both.

  - *它避免了两组集合的内存开销和复杂性。* 当然，存一次总比存两次便宜。此外，如果你有两个集合，你必须确保它们保持同步。每次创建或销毁对象时，都必须从两个集合中添加或移除。

- **If there is another collection for the objects:** / **如果有另一个用于对象的集合：**

  - *Traversing all objects is faster.* If the objects in question are "live" and have some processing they need to do, you may find yourself frequently needing to visit every object regardless of its location. Imagine if, in our earlier example, most of the cells were empty. Having to walk the full grid of cells to find the non-empty ones can be a waste of time. A second collection that just stores the objects gives you a way to walk all them directly. You have two data structures, one optimized for each use case.

  - *遍历所有对象更快。* 如果涉及的对象是"活动的"并且需要执行某些处理，你可能会发现自己经常需要访问每个对象，无论其位置如何。想象一下，在我们之前的例子中，大多数单元格都是空的。不得不遍历整个网格以找到非空单元格可能浪费大量时间。第二个只存储对象的集合为你提供了一种直接遍历所有对象的方法。你拥有两个数据结构，每个都针对各自的用例进行了优化。

## See Also / 参见

- I've tried not to discuss specific spatial partitioning structures in detail here to keep the chapter high-level (and not too long!), but your next step from here should be to learn a few of the common structures. Despite their scary names, they are all surprisingly straightforward. The common ones are:
  - [Grid](http://en.wikipedia.org/wiki/Grid_\(spatial_index\))
  - [Quadtree](http://en.wikipedia.org/wiki/Quad_tree)
  - [BSP](http://en.wikipedia.org/wiki/Binary_space_partitioning)
  - [k-d tree](http://en.wikipedia.org/wiki/Kd-tree)
  - [Bounding volume hierarchy](http://en.wikipedia.org/wiki/Bounding_volume_hierarchy)
- Each of these spatial data structures basically extends an existing well-known data structure from 1D into more dimensions. Knowing their linear cousins will help you tell if they are a good fit for your problem:
  - A grid is a persistent [bucket sort](http://en.wikipedia.org/wiki/Bucket_sort).
  - BSPs, k-d trees, and bounding volume hierarchies are [binary search trees](http://en.wikipedia.org/wiki/Binary_search_tree).
  - Quadtrees and octrees are [tries](http://en.wikipedia.org/wiki/Trie).

- 我尽量避免在这里详细讨论具体的空间分区结构，以保持本章的高层视角（并且不要太长！），但你的下一步应该是学习一些常见的结构。尽管它们有可怕的名字，但都出奇地简单。常见的包括：
  - [网格](http://en.wikipedia.org/wiki/Grid_\(spatial_index\))
  - [四叉树](http://en.wikipedia.org/wiki/Quad_tree)
  - [二叉空间分区 (BSP)](http://en.wikipedia.org/wiki/Binary_space_partitioning)
  - [k-d 树](http://en.wikipedia.org/wiki/Kd-tree)
  - [包围体层次结构](http://en.wikipedia.org/wiki/Bounding_volume_hierarchy)
- 这些空间数据结构基本上都是将现有的著名数据结构从一维扩展到更多维度。了解它们的线性表亲将帮助你判断它们是否适合你的问题：
  - 网格是一个持久的[桶排序](http://en.wikipedia.org/wiki/Bucket_sort)。
  - BSP、k-d 树和包围体层次结构是[二叉搜索树](http://en.wikipedia.org/wiki/Binary_search_tree)。
  - 四叉树和八叉树是[trie 树](http://en.wikipedia.org/wiki/Trie)。
---

## C# Equivalent (C# 对照实现)

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 空间分区模式 C# 实现——固定网格空间分区
/// 
/// 核心思想：
/// 1. 将 2D/3D 空间划分为固定大小的网格
/// 2. 每个对象根据其位置分配到对应网格单元格
/// 3. 查找附近对象时只搜索同一单元格及其邻居
/// 4. 对象移动时更新其所在单元格
/// 
/// 时间复杂度对比：
/// - 暴力查找：O(n²)
/// - 网格空间分区：O(n * 平均每格密度)
/// 
/// 主要优缺点：
/// + 实现简单，性能稳定
/// + 适合均匀分布的对象
/// - 对象不均匀分布时效率下降
/// - 需要调整网格大小以获得最佳性能
/// </summary>

// —— 空间分区中的单位 ——
// 每个单位存储位置和所属网格引用

public class SpatialUnit
{
    public Vector3 position;      // 当前位置
    public float detectionRadius; // 检测半径（用于近战/交互范围）
    public int cellX, cellY;      // 当前网格坐标（缓存，避免重复计算）
    public GridCell currentCell;  // 当前所属的网格单元格

    // 上下一个节点指针（单元格内双向链表）
    // 使用链表结构便于 O(1) 插入和删除
    public SpatialUnit prev;
    public SpatialUnit next;

    public SpatialUnit(Vector3 pos, float radius = 1f)
    {
        position = pos;
        detectionRadius = radius;
    }
}

// —— 网格单元格 ——
// 每个单元格存储该区域内的所有对象的链表头

public class GridCell
{
    // 链表头指针
    // 使用双向链表可以在 O(1) 时间内添加和移除对象
    public SpatialUnit head;

    // 调试用：当前单元格中的对象数量
    public int Count
    {
        get
        {
            int count = 0;
            var current = head;
            while (current != null)
            {
                count++;
                current = current.next;
            }
            return count;
        }
    }

    /// <summary>
    /// 向单元格添加对象（插入到链表头部，O(1)）
    /// </summary>
    public void Add(SpatialUnit unit)
    {
        unit.prev = null;
        unit.next = head;
        unit.currentCell = this;

        if (head != null)
            head.prev = unit;

        head = unit;
    }

    /// <summary>
    /// 从单元格移除对象（O(1)）
    /// 得益于双向链表结构
    /// </summary>
    public void Remove(SpatialUnit unit)
    {
        // 将前后节点链接起来
        if (unit.prev != null)
            unit.prev.next = unit.next;

        if (unit.next != null)
            unit.next.prev = unit.prev;

        // 如果是链表头，更新头指针
        if (head == unit)
            head = unit.next;

        unit.prev = null;
        unit.next = null;
        unit.currentCell = null;
    }
}

// —— 网格空间分区核心类 ——

public class GridSpatialPartition
{
    public int numCellsX;          // X 方向单元格数
    public int numCellsY;          // Y 方向单元格数
    public float cellSize;         // 单元格大小（正方形单元格边长）
    public Vector2 worldOffset;    // 世界原点偏移

    // 二维网格数组
    // 使用一维数组模拟二维，提高缓存局部性
    // 访问方式：cells_[x + y * numCellsX]
    private GridCell[] cells;

    /// <summary>
    /// 初始化网格空间分区
    /// </summary>
    public GridSpatialPartition(float worldWidth, float worldHeight,
                                float cellSize, Vector2 offset = default)
    {
        this.cellSize = cellSize;
        this.worldOffset = offset;

        // 计算网格尺寸（向上取整确保覆盖整个区域）
        numCellsX = Mathf.CeilToInt(worldWidth / cellSize);
        numCellsY = Mathf.CeilToInt(worldHeight / cellSize);

        // 一维数组存储二维网格
        // 使用一维数组的优势：
        // 1. 连续内存布局，缓存友好
        // 2. 减少数组访问的间接开销
        // 3. 便于 Unity Job System 使用 NativeArray
        cells = new GridCell[numCellsX * numCellsY];

        // 初始化所有单元格
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new GridCell();
        }
    }

    /// <summary>
    /// 将世界坐标转换为网格坐标
    /// </summary>
    public (int x, int y) WorldToCell(Vector3 worldPos)
    {
        // 减去世界偏移，然后除以单元格大小
        int cellX = Mathf.FloorToInt(
            (worldPos.x - worldOffset.x) / cellSize);
        int cellY = Mathf.FloorToInt(
            (worldPos.z - worldOffset.y) / cellSize); // 使用 z 作为 2D 的 y

        // 钳制到有效范围
        cellX = Mathf.Clamp(cellX, 0, numCellsX - 1);
        cellY = Mathf.Clamp(cellY, 0, numCellsY - 1);

        return (cellX, cellY);
    }

    /// <summary>
    /// 获取指定网格坐标的单元格
    /// </summary>
    private GridCell GetCell(int x, int y)
    {
        if (x < 0 || x >= numCellsX || y < 0 || y >= numCellsY)
            return null;  // 越界返回 null

        return cells[x + y * numCellsX];
    }

    /// <summary>
    /// 添加对象到空间分区
    /// </summary>
    public void Add(SpatialUnit unit)
    {
        var (cellX, cellY) = WorldToCell(unit.position);
        unit.cellX = cellX;
        unit.cellY = cellY;

        var cell = GetCell(cellX, cellY);
        if (cell != null)
        {
            cell.Add(unit);
        }
    }

    /// <summary>
    /// 更新对象位置（当对象移动时调用）
    /// 
    /// ★ 关键性能点：只有当对象跨越单元格边界时才更新
    /// 如果对象在同一个单元格内移动，不进行链表操作
    /// </summary>
    public void Move(SpatialUnit unit, Vector3 newPosition)
    {
        // 计算新旧单元格坐标
        var (oldCellX, oldCellY) = WorldToCell(unit.position);
        var (newCellX, newCellY) = WorldToCell(newPosition);

        unit.position = newPosition;

        // 如果没有跨越单元格边界，无需更新
        if (oldCellX == newCellX && oldCellY == newCellY)
            return;

        // 从旧单元格移除
        if (unit.currentCell != null)
        {
            unit.currentCell.Remove(unit);
        }

        // 添加到新单元格
        unit.cellX = newCellX;
        unit.cellY = newCellY;
        var newCell = GetCell(newCellX, newCellY);
        if (newCell != null)
        {
            newCell.Add(unit);
        }
    }

    /// <summary>
    /// 从空间分区移除对象
    /// </summary>
    public void Remove(SpatialUnit unit)
    {
        if (unit.currentCell != null)
        {
            unit.currentCell.Remove(unit);
        }
    }

    /// <summary>
    /// 查找给定对象附近的所有对象
    /// 
    /// 策略：检查同一单元格及其 3x3 邻居
    /// 只有当两个对象之间的距离小于检测半径时才返回
    /// 
    /// 为什么只检查 3x3 邻居？
    /// 假设检测半径 <= 单元格大小，那么两个可交互的对象
    /// 必定位于同一单元格或相邻单元格中
    /// </summary>
    public List<SpatialUnit> FindNearby(SpatialUnit queryUnit)
    {
        List<SpatialUnit> result = new List<SpatialUnit>();

        int x = queryUnit.cellX;
        int y = queryUnit.cellY;
        float queryRadius = queryUnit.detectionRadius;
        float sqrRadius = queryRadius * queryRadius;

        // 检查 3x3 邻居区域
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var cell = GetCell(x + dx, y + dy);
                if (cell == null) continue;

                // 遍历单元格链表中的每个对象
                var other = cell.head;
                while (other != null)
                {
                    // 不比较自身
                    if (other != queryUnit)
                    {
                        // 使用平方距离避免开平方开销
                        float sqrDist = (queryUnit.position - other.position).sqrMagnitude;
                        if (sqrDist <= sqrRadius)
                        {
                            result.Add(other);
                        }
                    }
                    other = other.next;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 查找指定位置附近的所有对象
    /// </summary>
    public List<SpatialUnit> FindNearby(Vector3 worldPos, float radius)
    {
        List<SpatialUnit> result = new List<SpatialUnit>();
        var (cellX, cellY) = WorldToCell(worldPos);
        float sqrRadius = radius * radius;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var cell = GetCell(cellX + dx, cellY + dy);
                if (cell == null) continue;

                var other = cell.head;
                while (other != null)
                {
                    float sqrDist = (worldPos - other.position).sqrMagnitude;
                    if (sqrDist <= sqrRadius)
                    {
                        result.Add(other);
                    }
                    other = other.next;
                }
            }
        }

        return result;
    }
}

// —— 使用空间分区的 MonoBehaviour 示例 ——

public class SpatialPartitionDemo : MonoBehaviour
{
    [Header("网格设置")]
    [SerializeField] private float worldSize = 100f;
    [SerializeField] private float cellSize = 10f;

    private GridSpatialPartition spatialGrid;
    private List<SpatialUnit> allUnits = new List<SpatialUnit>();

    void Start()
    {
        // 初始化空间分区
        spatialGrid = new GridSpatialPartition(
            worldSize, worldSize, cellSize);

        // 创建测试对象
        for (int i = 0; i < 100; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(0, worldSize),
                0,
                Random.Range(0, worldSize));

            var unit = new SpatialUnit(pos, Random.Range(1f, 3f));
            spatialGrid.Add(unit);
            allUnits.Add(unit);
        }
    }

    void Update()
    {
        // 模拟单位移动
        foreach (var unit in allUnits)
        {
            // 随机移动
            Vector3 newPos = unit.position + new Vector3(
                Random.Range(-1f, 1f) * Time.deltaTime * 2f,
                0,
                Random.Range(-1f, 1f) * Time.deltaTime * 2f);

            // 空间分区只在实际跨越单元格边界时执行链表操作
            spatialGrid.Move(unit, newPos);

            // 查找附近对象进行交互（如战斗检测）
            var nearby = spatialGrid.FindNearby(unit);
            if (nearby.Count > 0)
            {
                // 和附近的单位进行交互...
                Debug.DrawLine(unit.position, nearby[0].position, Color.red);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (spatialGrid == null) return;

        // 可视化网格
        Gizmos.color = Color.gray;
        for (int x = 0; x <= spatialGrid.numCellsX; x++)
        {
            float worldX = x * spatialGrid.cellSize;
            Gizmos.DrawLine(
                new Vector3(worldX, 0, 0),
                new Vector3(worldX, 0, worldSize));
        }
        for (int y = 0; y <= spatialGrid.numCellsY; y++)
        {
            float worldY = y * spatialGrid.cellSize;
            Gizmos.DrawLine(
                new Vector3(0, 0, worldY),
                new Vector3(worldSize, 0, worldY));
        }
    }
}

// —— 使用 Unity 内置 Physics 进行简易空间查询 ——
// Unity 的物理引擎内部已经实现了空间分区（通常是 BVH 或 Grid）
// 我们可以直接利用 Physx 的 Overlap 系列 API

public class UnityPhysicsExample : MonoBehaviour
{
    /// <summary>
    /// 使用 Physics.OverlapSphere 查找附近对象
    /// Unity 的物理引擎内部使用 BVH 加速结构
    /// 这本质上是空间分区的一种实现
    /// </summary>
    public Collider[] FindNearbyWithPhysics(Vector3 center, float radius)
    {
        // Physics.OverlapSphere 使用 PhysX 内置的空间分区
        // 远快于手动遍历所有 GameObject
        // 但仅限于有 Collider 的对象
        return Physics.OverlapSphere(center, radius);
    }

    /// <summary>
    /// 使用非分配的 OverlapSphereNonAlloc（推荐）
    /// 避免每帧分配数组
    /// </summary>
    public int FindNearbyNoAlloc(Vector3 center, float radius,
                                  Collider[] results)
    {
        // NonAlloc 版本不分配新数组
        // 配合对象池复用 Collider[] 数组
        return Physics.OverlapSphereNonAlloc(center, radius, results);
    }
}

// —— 空间哈希（Spatial Hashing）—— 
// 另一种空间分区实现，使用哈希表代替二维数组
// 适合对象稀疏分布的场景

public class SpatialHash
{
    private float cellSize;
    private Dictionary<int, List<GameObject>> buckets;

    public SpatialHash(float cellSize)
    {
        this.cellSize = cellSize;
        buckets = new Dictionary<int, List<GameObject>>();
    }

    /// <summary>
    /// 将 2D 坐标转换为哈希键
    /// 使用 x 和 y 的哈希组合生成唯一键
    /// </summary>
    private int HashCoords(int cellX, int cellY)
    {
        // 使用大质数避免哈希冲突
        return cellX * 73856093 + cellY * 19349663;
    }

    /// <summary>
    /// 将对象插入空间哈希
    /// </summary>
    public void Insert(GameObject obj, Vector3 position)
    {
        int cellX = Mathf.FloorToInt(position.x / cellSize);
        int cellY = Mathf.FloorToInt(position.z / cellSize);
        int key = HashCoords(cellX, cellY);

        if (!buckets.ContainsKey(key))
            buckets[key] = new List<GameObject>();

        buckets[key].Add(obj);
    }
}
```

## Unity Application / Unity 应用场景

1. **Physics Queries**: `Physics.OverlapSphere`, `Physics.BoxCast`, `Physics.SphereCast` all use PhysX's built-in spatial partitioning (BVH).
2. **Rendering Frustum Culling**: Unity's rendering pipeline uses spatial partitioning to quickly determine which objects are visible to the camera.
3. **AI Sense Systems**: Enemy AI uses spatial queries to find players or threats within detection range.
4. **Audio Spatialization**: Audio system uses spatial queries to determine which sound sources are audible.
5. **Network Interest Management**: Multiplayer games use spatial partitioning to determine which entities to replicate to each client.
6. **Unity ECS Spatial Queries**: Using `ComponentDataFromEntity` with position-based chunk queries.

1. **物理查询**: `Physics.OverlapSphere`、`Physics.BoxCast`、`Physics.SphereCast` 都使用 PhysX 内置的空间分区（BVH 加速结构）。
2. **渲染视锥体剔除**: Unity 的渲染管线使用空间分区快速判断哪些对象对摄像机可见。
3. **AI 感知系统**: 敌人 AI 使用空间查询在检测范围内查找玩家或威胁。
4. **音频空间化**: 音频系统使用空间查询确定哪些音源可被听到。
5. **网络兴趣管理**: 多人游戏使用空间分区确定向每个客户端复制哪些实体。
6. **Unity ECS 空间查询**: 使用 `ComponentDataFromEntity` 配合基于位置的分块查询。
## Key Differences / 关键差异

| Aspect | C++ (原书) | C# (Unity) |
|--------|-----------|------------|
| Data structure | 2D array of linked lists | 1D array of objects with `List<T>` or custom linked list |
| Memory model | C-style manual array | Managed `GridCell[]` + `List<SpatialUnit>` |
| Query API | Custom `handleCell` | `Physics.OverlapSphere` / custom grid traversal |
| Movement update | Manual linked list removal/insertion | Grid check + cell transfer or hash bucket update |
| Granularity | Fixed cell size | Configurable cell size via `cellSize` parameter |
| Built-in alternatives | None | `Physics.OverlapSphere[NonAlloc]`, `Physics.SphereCast` |
| Advanced structures | Quadtree, BSP, k-d tree | Same concepts via `GameObject` or ECS queries |
| Performance measurement | Manual timer | Profiler, `Physics.autoSimulation` metrics |
