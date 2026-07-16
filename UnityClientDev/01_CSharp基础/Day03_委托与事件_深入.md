# Day 3：委托与事件深入 — 表达式树、自定义 Awaitable 与源码生成器

## 0. 为什么需要深入委托？

Day03 讲了 delegate、event、lambda 的基本使用。但实际项目中，委托和事件还有更复杂的场景：

- 你要在运行时**生成和执行代码**（Expression Trees）
- 你要**用 struct 避免委托的 GC 分配**（Custom Awaiter）
- 你要处理**多播委托中的异常传播**
- 你要让编译器**自动生成事件代码**（Source Generators）

这一篇深入这四个方向，以及委托在 .NET 运行时层面的底层机制。

---

## 1. Expression&lt;T&gt; 树——编译时 vs 运行时代码生成

### 委托 vs 表达式树：根本区别

```csharp
// 委托：编译为 IL，直接可执行
Func<int, int, int> add = (a, b) => a + b;
// 运行时：直接调用 IL 指令

// 表达式树：编译为数据结构，可分析/修改/优化
Expression<Func<int, int, int>> addExpr = (a, b) => a + b;
// 运行时：是一个树状结构，可以被解析、修改、编译
```

表达式树的结构：

```
addExpr 的内容：
            Lambda
           /       \
        Parameter   Parameter     Body
        (a)         (b)         BinaryExpr(+)
                                  /       \
                              Member      Member
                              (a)         (b)
```

### 如何操作表达式树

```csharp
using System.Linq.Expressions;

// 1. 手动构建表达式树
ParameterExpression paramX = Expression.Parameter(typeof(int), "x");
ParameterExpression paramY = Expression.Parameter(typeof(int), "y");
BinaryExpression body = Expression.Add(paramX, paramY);
Expression<Func<int, int, int>> lambda =
    Expression.Lambda<Func<int, int, int>>(body, paramX, paramY);

// 2. 编译为委托并执行
Func<int, int, int> compiled = lambda.Compile();
int result = compiled(3, 5);  // 8
```

### 表达式树修改

```csharp
// 场景：给所有方法调用加上日志
// 原始表达式：x => x.SomeMethod(10)
// 修改后：x => { Log("SomeMethod called"); return x.SomeMethod(10); }

// 使用 ExpressionVisitor 遍历并修改树
public class LoggingVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // 在方法调用前插入日志
        var logCall = Expression.Call(
            typeof(Debug).GetMethod("Log", new[] { typeof(object) }),
            Expression.Constant($"Calling {node.Method.Name}")
        );

        // 创建 { Log(); return OriginalCall(); } 块
        return Expression.Block(node.Type, logCall, node);
    }
}

// 使用：
Expression<Action<Player>> expr = p => p.TakeDamage(10);
var modified = new LoggingVisitor().Visit(expr);
// 等价于：p => { Debug.Log("Calling TakeDamage"); p.TakeDamage(10); }
```

### Unity 中的 Expression Trees

```csharp
// Unity IL2CPP 下 Expression.Compile() 不可用！
// 因为 IL2CPP 不支持运行时生成代码

// ❌ 不能在 Unity IL2CPP 构建中这样用：
var compiled = lambda.Compile();  // NotSupportedException!

// ✅ 替代方案：
// 1. 直接用委托（编译时已知）
// 2. 用反射（慢，但有）
// 3. 用 Source Generators（编译时生成代码）
```

---

## 2. 委托缓存与比较——避免重复分配

### 委托的创建开销

```csharp
// 每次 new Action(this.DoSomething) 或 new Action(DoSometing)
// 都会创建新的委托对象

public class Example : MonoBehaviour
{
    void Start()
    {
        // 事件绑定，每次 Start 都创建新委托
        someEvent += DoSomething;
        // 等价于：someEvent += new Action(DoSomething);
    }

    void DoSomething() { }
}
```

### 方法组转换的缓存

```csharp
// 编译器对方法组转换做了优化：

// 写法 1：方法组（编译器自动缓存）
button.onClick.AddListener(OnClick);
// 编译器会缓存委托实例，重复调用不会新建

// 写法 2：Lambda（每次新建！）
button.onClick.AddListener(() => OnClick());
// 每次执行到这行都创建新的委托实例

// 写法 3：显式 Lambda 变量（可手动缓存）
Action onClickAction = () => OnClick();
button.onClick.AddListener(onClickAction);  // 只创建一次

// 结论：能用方法组就用方法组
// 必须用 Lambda 时，缓存到变量中复用
```

### 委托的 ReferenceEquals 比较

```csharp
Action a1 = DoSomething;
Action a2 = DoSomething;

// 方法组：编译器会缓存，所以引用相等
Console.WriteLine(ReferenceEquals(a1, a2));  // true（同方法组）

// Lambda：每次新建，引用不同
Action l1 = () => DoSomething();
Action l2 = () => DoSomething();
Console.WriteLine(ReferenceEquals(l1, l2));  // false（不同实例）

// 这对事件 -= 有影响！
someEvent += () => DoSomething();
// someEvent -= () => DoSomething();  // 无法移除！不同委托实例！

// ✅ 正确做法
Action handler = () => DoSomething();
someEvent += handler;
someEvent -= handler;  // 可以移除
```

### Unity 中的委托缓存

```csharp
public class AudioManager : MonoBehaviour
{
    // ❌ 坏：每次 OnEnable 创建新委托
    void OnEnable()
    {
        EventBus.Subscribe<DamageEvent>(OnDamage);  // 方法组没问题
        EventBus.Subscribe<HealEvent>(e => PlayHealSound(e.Amount));  // 每次新建 Lambda
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<DamageEvent>(OnDamage);  // 成功移除（方法组）
        // EventBus.Unsubscribe<HealEvent>(e => PlayHealSound(e.Amount));  // 无法移除！
    }

    // ✅ 好：缓存 Lambda
    private Action<HealEvent> _healHandler;

    void Awake()
    {
        _healHandler = e => PlayHealSound(e.Amount);
    }

    void OnEnable()
    {
        EventBus.Subscribe(_healHandler);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe(_healHandler);
    }

    void OnDamage(DamageEvent e) { }
}
```

---

## 3. 自定义 Awaiter 模式——INotifyCompletion

### await 的底层协议

编译器让 `await` 可以工作在任何"可等待"类型上，只要它实现特定模式：

```csharp
// 可等待类型需要：
public interface ICriticalNotifyCompletion : INotifyCompletion
{
    // 1. 告诉编译器是否已经完成
    bool IsCompleted { get; }

    // 2. 获取结果（如果有返回值）
    // T GetResult();

    // 3. 注册延续（await 之后的代码）
    void OnCompleted(Action continuation);
    void UnsafeOnCompleted(Action continuation);
}
```

### 自定义一个 Awaiter——Unity 中的等待

```csharp
// 场景：等待 Animator 的某个动画播放完毕
public struct AnimationAwaiter : INotifyCompletion
{
    private readonly Animator _animator;
    private readonly int _stateHash;
    private readonly int _layer;

    public bool IsCompleted
    {
        get
        {
            // 检查动画是否已播放完
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_layer);
            return state.shortNameHash != _stateHash
                || state.normalizedTime >= 1.0f;
        }
    }

    public AnimationAwaiter(Animator animator, string stateName, int layer = 0)
    {
        _animator = animator;
        _stateHash = Animator.StringToHash(stateName);
        _layer = layer;
    }

    public void OnCompleted(Action continuation)
    {
        // 启动一个协程来等待动画完成然后回调
        _animator.StartCoroutine(WaitForAnimation(continuation));
    }

    private IEnumerator WaitForAnimation(Action continuation)
    {
        // 等到动画状态切换或播放完成
        yield return new WaitWhile(() =>
        {
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_layer);
            return state.shortNameHash == _stateHash
                && state.normalizedTime < 1.0f;
        });
        continuation?.Invoke();
    }

    public void GetResult() { }
}

// 扩展方法让 Animator 直接返回 awaiter
public static class AnimatorExtensions
{
    public static AnimationAwaiter PlayAndWait(
        this Animator animator, string stateName)
    {
        animator.Play(stateName);
        return new AnimationAwaiter(animator, stateName);
    }
}

// 使用——可以 await 了！
public async AwaitableVoid PlayAnimationAsync()
{
    Animator anim = GetComponent<Animator>();

    // await 动画播放完成！
    await anim.PlayAndWait("Attack");

    Debug.Log("Attack animation finished!");
    // 继续后面的逻辑...
}
```

### 更实用的例子：等待协程的 Awaiter

```csharp
// 把任意 IEnumerator（协程）变成可 await 的
public struct CoroutineAwaiter : INotifyCompletion
{
    private readonly IEnumerator _coroutine;
    private readonly MonoBehaviour _owner;
    private Coroutine _running;

    public bool IsCompleted => false;  // 协程总是需要时间

    public CoroutineAwaiter(IEnumerator coroutine, MonoBehaviour owner)
    {
        _coroutine = coroutine;
        _owner = owner;
        _running = null;
    }

    public void OnCompleted(Action continuation)
    {
        // 在协程末尾调用延续
        IEnumerator wrapped = WrapWithContinuation(continuation);
        _running = _owner.StartCoroutine(wrapped);
    }

    private IEnumerator WrapWithContinuation(Action continuation)
    {
        yield return _coroutine;
        continuation?.Invoke();
    }

    public void GetResult() { }
}

// 扩展方法
public static class CoroutineExtensions
{
    public static CoroutineAwaiter GetAwaiter(this IEnumerator coroutine,
        MonoBehaviour owner)
    {
        return new CoroutineAwaiter(coroutine, owner);
    }
}

// 使用：任意协程都可以 await！
public async AwaitableVoid UseCoroutineAwaiter()
{
    await MoveToTarget().GetAwaiter(this);
    Debug.Log("Movement finished!");
}
```

---

## 4. 源码生成器（Source Generators）——自动生成事件代码

### 为什么要用源码生成器？

```csharp
// 手动写的事件绑定代码——重复且容易出错
public class Player : MonoBehaviour
{
    // 需要手动管理订阅/取消订阅
    private Action<DamageEvent> _onDamageHandler;
    private Action<HealEvent> _onHealHandler;

    void OnEnable()
    {
        _onDamageHandler = e => HandleDamage(e);
        _onHealHandler = e => HandleHeal(e);
        EventBus.Subscribe(_onDamageHandler);
        EventBus.Subscribe(_onHealHandler);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe(_onDamageHandler);
        EventBus.Unsubscribe(_onHealHandler);
    }
}

// Source Generator 可以在编译时自动生成这些代码
```

### Source Generator 的工作原理

```
编译过程：

C# 源码 → 编译
           ↓
    源码生成器
    (实现 ISourceGenerator)
           ↓
    生成额外的 C# 代码
    (编译时，不是运行时)
           ↓
    所有 C# 代码一起编译
           ↓
    最终的程序集
```

### 通过特性标记自动生成事件绑定

```csharp
// 定义特性，标记需要自动管理事件的方法
[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute
{
    public Type EventType { get; }
    public EventHandlerAttribute(Type eventType) => EventType = eventType;
}

// 使用者——只需要标记，不需要手写订阅/取消
public partial class Player : MonoBehaviour
{
    [EventHandler(typeof(DamageEvent))]
    private void OnDamage(DamageEvent e)
    {
        Debug.Log($"Took {e.Amount} damage");
    }

    [EventHandler(typeof(HealEvent))]
    private void OnHeal(HealEvent e)
    {
        Debug.Log($"Healed {e.Amount}");
    }
}

// 源码生成器自动生成的部分：
// Player.g.cs（编译时自动生成）
public partial class Player
{
    void OnEnable()
    {
        EventBus.Subscribe<DamageEvent>(OnDamage);
        EventBus.Subscribe<HealEvent>(OnHeal);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<DamageEvent>(OnDamage);
        EventBus.Unsubscribe<HealEvent>(OnHeal);
    }
}
```

### Unity 中的源码生成器工具

```csharp
// Unity 2022+ 支持 Roslyn Source Generators
// 社区工具：UniTask 用了 Source Generator 生成优化代码
// 你的项目中可以用 Source Generator：

// 1. 自动生成 Inspector 序列化字段的访问器
// 2. 自动生成事件系统的订阅/取消订阅
// 3. 自动生成网络消息的处理路由

// 注意：Source Generator 在 Unity 中的支持有限制
// - 不能在 Entity Scene 中运行（需要预编译）
// - Android/iOS 构建中可能受 IL2CPP 影响
```

---

## 5. Span&lt;T&gt; 和 Memory&lt;T&gt; 在委托上下文中的使用

```csharp
// 委托不能直接捕获 Span<T>（因为 Span 是 ref struct）
ReadOnlySpan<char> text = "Hello".AsSpan();

// ❌ 编译错误
// Action act = () => Console.WriteLine(text[0]);

// ✅ 解决：转换为字符串或 Memory<T>
Memory<char> mem = new char[] { 'H', 'e', 'l', 'l', 'o' };
Action act = () => Console.WriteLine(mem.Span[0]);  // Span 在 Lambda 中临时使用

// 或者用数组传参
void ProcessData(ReadOnlySpan<byte> data) { }
byte[] buffer = new byte[1024];
Action processAction = () => ProcessData(buffer.AsSpan());  // AsSpan 在调用时创建
```

### Unity 中处理字节流

```csharp
// 网络消息解析——零分配委托传递
public class NetworkHandler : MonoBehaviour
{
    public delegate void MessageHandler(ReadOnlySpan<byte> data);

    private Dictionary<int, MessageHandler> _handlers = new();

    public void RegisterHandler(int msgId, MessageHandler handler)
    {
        _handlers[msgId] = handler;
    }

    public void OnMessageReceived(byte[] rawData)
    {
        int msgId = BitConverter.ToInt32(rawData, 0);
        if (_handlers.TryGetValue(msgId, out var handler))
        {
            // 传递 Span 切片——零分配
            handler(rawData.AsSpan(4));
        }
    }
}
```

---

## 6. Action&lt;T&gt;/Func&lt;T&gt; vs 自定义委托性能

### 性能差异的来源

```csharp
// 系统内置委托
public delegate void Action<T>(T obj);
public delegate TResult Func<T, TResult>(T arg);

// 自定义委托
public delegate void MyHandler(int value);

// 看起来一样？实际上有细微差异：

// Action<int> 是 System.Action<T>——CLR 预知的类型
// 自定义委托需要 JIT 编译新的方法表

// 但主要差异不在这里——主要在于调用开销：
```

### 微基准测试

```csharp
// 假设我们要测试 1 亿次调用：

// 1. 直接方法调用
void DirectCall(int x) { }
for (int i = 0; i < 100_000_000; i++) DirectCall(i);
// 最快

// 2. 系统委托
Action<int> action = DirectCall;
for (int i = 0; i < 100_000_000; i++) action(i);
// 中间

// 3. 自定义委托
MyHandler handler = DirectCall;
for (int i = 0; i < 100_000_000; i++) handler(i);
// 和系统委托几乎一样

// 4. 多播委托（+= 多个）
Action<int> multicast = DirectCall;
multicast += AnotherCall;
multicast += ThirdCall;
for (int i = 0; i < 100_000_000; i++) multicast(i);
// 最慢——需要遍历整个调用链

// 实际差异（1 亿次）：
// 直接调用：~200ms
// 单播委托：~400ms
// 多播委托：~800ms
```

### 在 Unity 中的选择建议

```csharp
// Unity 事件系统性能对比：

// 1. C# event（性能最好）
public event Action<int> OnDamage;

// 2. UnityEvent（性能差，但可序列化）
public UnityEvent<int> OnDamageUnity;

// 3. 自定义委托（和 Action 几乎一样）
public delegate void DamageHandler(int amount);
public event DamageHandler OnDamageCustom;

// 实际选择依据：
// - 代码内事件：C# event（最高性能）
// - Inspector 拖拽：UnityEvent（牺牲性能换便利）
// - 高频调用（Update 中的回调）：C# event 或缓存委托
```

---

## 7. 多播委托异常处理

### 问题的根源

```csharp
public class Player
{
    public event Action OnDeath;

    public void Die()
    {
        OnDeath?.Invoke();
    }
}

// 订阅者
void Start()
{
    GetComponent<Player>().OnDeath += () =>
    {
        throw new Exception("Something went wrong!");
    };

    GetComponent<Player>().OnDeath += () =>
    {
        Debug.Log("This will never run!");
    };
}

// 当 OnDeath 触发时：
// 第一个委托抛异常 → 第二个委托永远不会执行！
```

### 获取所有调用列表并逐个执行

```csharp
public class SafeEventDispatcher
{
    public static void InvokeSafe(Action action)
    {
        if (action == null) return;

        // 获取委托链中的所有单个委托
        Delegate[] delegates = action.GetInvocationList();

        foreach (Delegate del in delegates)
        {
            try
            {
                ((Action)del)?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Event handler failed: {ex.Message}");
                // 继续执行下一个！不让一个失败影响全部
            }
        }
    }

    public static void InvokeSafe<T>(Action<T> action, T arg)
    {
        if (action == null) return;

        Delegate[] delegates = action.GetInvocationList();
        foreach (Delegate del in delegates)
        {
            try
            {
                ((Action<T>)del)?.Invoke(arg);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Event handler failed: {ex.Message}");
            }
        }
    }
}

// 使用——替代 ?.Invoke()
public class SafePlayer : MonoBehaviour
{
    public event Action<int> OnDamage;

    public void TakeDamage(int dmg)
    {
        // SafeEventDispatcher.InvokeSafe(OnDamage, dmg);
        // 所有订阅者都会执行，即使其中一个抛异常
    }
}
```

### Unity 事件中收集异常

```csharp
// 更高级的版本：收集所有异常，统一报告
public static class SafeEvent
{
    public static void Invoke(Action action, string context = "")
    {
        if (action == null) return;

        List<Exception> exceptions = null;

        foreach (Delegate del in action.GetInvocationList())
        {
            try
            {
                ((Action)del)?.Invoke();
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(ex);
                Debug.LogError(
                    $"Event handler [{del.Method.Name}] in {context} failed: {ex.Message}");
            }
        }

        if (exceptions?.Count > 0)
        {
            Debug.LogError(
                $"{exceptions.Count} handler(s) failed in event: {context}");
        }
    }
}
```

---

## C++/Raylib 对照总结

| 概念 | C++ | C# |
|------|-----|-----|
| 运行时代码生成 | JIT / `asmjit` 库 | `Expression<T>.Compile()` |
| 委托缓存 | 函数指针直接复用 | 方法组自动缓存，Lambda 每次新建 |
| 异常安全回调 | try/catch 包裹 | 多播委托逐个 try/catch |
| 可等待对象 | `co_await` + 自定义类型 | `INotifyCompletion` 接口 |
| 编译时代码生成 | 模板 + 宏 | Roslyn Source Generators |
| Span 在回调中 | `std::span` 传引用 | 用 `Memory<T>` 替代 |

## 停靠点

> Expression&lt;T&gt; 是代码的数据结构表示——可以分析、修改、编译。Unity IL2CPP 不支持 Compile()。
> 方法组转换的委托可以被编译器缓存（引用相等），Lambda 不会。缓存在变量中再绑定事件。
> 实现 INotifyCompletion 可以让任何类型支持 await。这在 Unity 中能把协程转化为 await 风格。
> Source Generator 在编译时生成代码，自动完成事件订阅/取消的 boilerplate。
> Span&lt;T&gt; 不能出现在委托/Lambda 的闭包中——因为它必须是栈上数据。
> 多播委托中一个 handler 抛异常会阻断后续 handler——用 GetInvocationList 逐个执行并捕获异常。
