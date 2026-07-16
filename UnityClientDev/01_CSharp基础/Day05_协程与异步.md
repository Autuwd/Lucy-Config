# Day 5：协程与异步 — 从 yield 到 async/await

## 0. 为什么需要协程？

游戏中有很多"等待"操作：
- 等待 2 秒后播放动画
- 等待某个条件满足再继续
- 从 A 点平滑移动到 B 点

如果不使用协程，你需要用状态机或计时器：

```csharp
// 不用协程——需要额外变量跟踪状态
float timer = 0;
bool isWaiting = false;

void Update()
{
    if (isWaiting)
    {
        timer += Time.deltaTime;
        if (timer >= 2f)
        {
            isWaiting = false;
            timer = 0;
            Debug.Log("2 seconds passed!");
        }
    }
}
```

**协程（Coroutine）** 让你用**顺序代码写异步操作**——看起来像普通函数，但可以暂停并稍后继续。

---

## 1. IEnumerator 和 IEnumerable

### IEnumerator 接口

协程的核心是 `IEnumerator` 接口：

```csharp
public interface IEnumerator
{
    object Current { get; }      // 当前 yield 返回的值
    bool MoveNext();             // 移动到下一步，返回是否还有更多步骤
    void Reset();                // 重置到开始
}
```

### 实现自己的 IEnumerator

```csharp
// 一个自定义的枚举器
public class CountdownEnumerator : IEnumerator
{
    private int count = 3;

    public object Current => count;  // 当前值

    public bool MoveNext()
    {
        count--;
        return count >= 0;  // 还有更多就返回 true
    }

    public void Reset() { count = 3; }
}

// 使用：
IEnumerator e = new CountdownEnumerator();
while (e.MoveNext())
{
    Debug.Log(e.Current);  // 输出: 2, 1, 0
}
```

### yield return——编译器帮我们生成了 IEnumerator

```csharp
IEnumerator Countdown()
{
    // 这段代码看起来像顺序执行
    yield return 2;
    yield return 1;
    yield return 0;
}
```

编译器将上面的方法转换为一个**状态机类**：

```csharp
// 编译器生成的状态机（简化版）
[CompilerGenerated]
private sealed class <Countdown>d__0 : IEnumerator
{
    private int state;      // 状态: -1 = 初始, 0 = 第一步, 1 = 第二步, ...
    private object current; // Current 属性的返回值

    public <Countdown>d__0(int initialState)
    {
        state = initialState;  // 初始状态 = -2（第一次调用）或 -1（继续）
    }

    public bool MoveNext()
    {
        switch (state)
        {
            case 0:
                // 第 0 步：yield return 2
                current = 2;
                state = 1;  // 下一步
                return true;

            case 1:
                // 第 1 步：yield return 1
                current = 1;
                state = 2;
                return true;

            case 2:
                // 第 2 步：yield return 0
                current = 0;
                state = -1;  // 结束
                return true;

            default:
                return false;  // 没有更多步骤
        }
    }

    public object Current => current;
}
```

**关键：** `yield return` 不是"返回并结束函数"，而是"**暂停，保存当前状态，下次继续**"。

---

## 2. Unity 协程的工作原理

### StartCoroutine + MoveNext 调度

```
你调用 StartCoroutine(Countdown())

Unity 内部：
1. 调用 Countdown() 获取 IEnumerator
2. 每帧调用 MoveNext() 直到返回 false
3. 根据 Current 的类型决定何时调用下一次 MoveNext：

Current 的类型          → 下一次 MoveNext 何时被调用
──────────────────────────────────────────────────
null                    → 下一帧（立即）
WaitForSeconds(1)       → 等待 1 秒后
WaitForEndOfFrame()     → 本帧渲染完成后
WaitUntil(condition)    → condition 变为 true 时
WaitWhile(condition)    → condition 变为 false 时
WWW / UnityWebRequest   → 下载完成后
AsyncOperation          → 异步操作完成后（如 SceneManager.LoadAsync）
CustomYieldInstruction  → 自定义等待条件
```

### Unity 协程调度器（简化）

```csharp
// Unity 的协程调度逻辑（伪代码）
public class CoroutineManager
{
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    void UpdateCoroutines()
    {
        for (int i = activeCoroutines.Count - 1; i >= 0; i--)
        {
            Coroutine c = activeCoroutines[i];

            // 检查是否到了继续执行的时间
            if (!c.ShouldResumeNow())
                continue;

            // 调用 MoveNext()
            bool hasMore = c.enumerator.MoveNext();

            if (!hasMore)
            {
                // 协程结束，移除
                activeCoroutines.RemoveAt(i);
                continue;
            }

            // 处理 Current（yield return 的值）
            ProcessYieldInstruction(c);
        }
    }
}
```

### 协程的生命周期

```csharp
public class CoroutineDemo : MonoBehaviour
{
    void Start()
    {
        // 启动协程——返回 Coroutine 对象
        Coroutine c = StartCoroutine(MyCoroutine());

        // 停止单个协程
        StopCoroutine(c);

        // 停止所有协程
        StopAllCoroutines();

        // 禁用脚本也会停止它的协程
        this.enabled = false;  // Update 停止，但协程继续！
        // 注意：enabled = false 不会停止协程！
        // 只有 OnDisable/OnDestroy 或显式 Stop 才会
    }

    IEnumerator MyCoroutine()
    {
        Debug.Log("Start");
        yield return new WaitForSeconds(1f);
        Debug.Log("After 1 second");
    }
}
```

---

## 3. 常用 yield 指令

### WaitForSeconds——按真实时间等待

```csharp
IEnumerator WaitDemo()
{
    Debug.Log("Start");
    yield return new WaitForSeconds(2f);  // 等 2 秒
    Debug.Log("2 seconds later");

    // WaitForSeconds 受 Time.timeScale 影响
    // 如果 Time.timeScale = 0，等待永远不会结束！
}
```

### WaitForSecondsRealtime——不受 timeScale 影响

```csharp
IEnumerator RealTimeWait()
{
    // 即使 Time.timeScale = 0，这个等待也会继续
    yield return new WaitForSecondsRealtime(2f);
}
```

### WaitForEndOfFrame——等渲染完成

```csharp
IEnumerator EndOfFrame()
{
    yield return new WaitForEndOfFrame();
    // 此时所有渲染已结束，可以截图
    ScreenCapture.CaptureScreenshot("screenshot.png");
}
```

### WaitUntil / WaitWhile——条件等待

```csharp
IEnumerator WaitUntilCondition()
{
    // 等待 hp <= 0
    yield return new WaitUntil(() => hp <= 0);
    Die();

    // 或者用 WaitWhile
    yield return new WaitWhile(() => isRespawning);
    // isRespawning 变为 false 时继续
}
```

### yield return null——等一帧

```csharp
IEnumerator WaitOneFrame()
{
    Debug.Log("Frame 1");
    yield return null;  // 等一帧
    Debug.Log("Frame 2");
    yield return null;  // 再等一帧
    Debug.Log("Frame 3");
}
```

---

## 4. 实用协程模式

### 渐变效果

```csharp
IEnumerator FadeTo(Color target, float duration)
{
    Renderer rend = GetComponent<Renderer>();
    Color startColor = rend.material.color;
    float elapsed = 0;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;  // 0 → 1

        // Lerp = Linear Interpolation（线性插值）
        rend.material.color = Color.Lerp(startColor, target, t);

        yield return null;  // 每帧更新一次
    }

    // 确保最终值准确
    rend.material.color = target;
}
```

### 延时执行

```csharp
IEnumerator DelayedAction(float delay, System.Action action)
{
    yield return new WaitForSeconds(delay);
    action?.Invoke();
}

// 使用：
StartCoroutine(DelayedAction(2f, () => {
    Debug.Log("2 seconds later");
}));
```

### 序列执行

```csharp
IEnumerator Sequence()
{
    // 步骤 1：移动
    yield return MoveTo(targetPosition, 1f);

    // 步骤 2：等待
    yield return new WaitForSeconds(0.5f);

    // 步骤 3：播放动画
    animator.SetTrigger("Attack");
    yield return new WaitForSeconds(1f);

    // 步骤 4：返回
    yield return MoveTo(startPosition, 1f);

    Debug.Log("Sequence complete!");
}

IEnumerator MoveTo(Vector3 target, float duration)
{
    Vector3 start = transform.position;
    float elapsed = 0;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        transform.position = Vector3.Lerp(start, target, elapsed / duration);
        yield return null;
    }
    transform.position = target;
}
```

### 嵌套协程——yield return StartCoroutine

```csharp
IEnumerator MainSequence()
{
    // 等待另一个协程完成
    yield return StartCoroutine(FadeOut(1f));
    Debug.Log("Fade out complete");

    yield return StartCoroutine(FadeIn(1f));
    Debug.Log("Fade in complete");
}
```

---

## 5. 协程的内存开销

```csharp
// 每个协程调用都会：
// 1. 分配 IEnumerator 对象（堆分配）
// 2. 如果使用了闭包（Lambda），分配闭包类
// 3. 创建 YieldInstruction 对象

IEnumerator ExpensiveCoroutine()
{
    // 每个 yield return new WaitForSeconds 创建新对象
    yield return new WaitForSeconds(1f);  // 堆分配
    yield return new WaitForSeconds(1f);  // 又一个堆分配
}

// ✅ 优化：缓存 WaitForSeconds 实例
private WaitForSeconds waitOneSecond = new WaitForSeconds(1f);

IEnumerator OptimizedCoroutine()
{
    yield return waitOneSecond;  // 重用同一个实例！零分配
    yield return waitOneSecond;
}
```

**协程性能总结：**
- 启动协程：一次堆分配（IEnumerator）
- 每 yield return null：零分配（Current = null）
- 每 new WaitForSeconds：一次堆分配（可缓存优化）

---

## 6. async / await——C# 的现代异步

### Unity 2022+ 的 Awaitable

```csharp
using UnityEngine;

public class AsyncDemo : MonoBehaviour
{
    async AwaitableVoid Start()
    {
        Debug.Log("Start");
        await Awaitable.WaitForSecondsAsync(1f);
        Debug.Log("After 1 second");
        await Awaitable.NextFrameAsync();
        Debug.Log("Next frame");
    }
}
```

### async/await 的底层——状态机

```csharp
async AwaitableVoid ExampleAsync()
{
    Debug.Log("Before await");
    await Awaitable.WaitForSecondsAsync(1f);
    Debug.Log("After await");
}
```

编译器生成的状态机和协程类似：

```
调用 ExampleAsync() 时：
1. 创建状态机结构体（初始状态 = -1）
2. 立即执行到第一个 await
3. 遇到 await → 挂起，注册延续（continuation）
4. 等待 1 秒后 → 状态机恢复执行
5. 继续执行剩余代码
```

### async/await vs 协程

| | 协程 (IEnumerator) | async/await |
|--|-------------------|-------------|
| 返回值 | IEnumerator | Task / Awaitable |
| yield | yield return X | await X |
| 异常处理 | try/catch 可能跨 yield | 完整 try/catch 支持 |
| 返回值 | 只能 yield return | await 表达式有返回值 |
| 性能 | 较轻量 | 稍重（状态机更复杂） |
| Unity 版本 | 全部支持 | Unity 2022+ (Awaitable) |
| 调度 | Unity 协程引擎 | PlayerLoop / Task 调度器 |

### 在旧 Unity 中使用 async/await

```csharp
// 使用 System.Threading.Tasks
using System.Threading.Tasks;
using UnityEngine;

public class OldUnityAsync : MonoBehaviour
{
    async void Start()
    {
        // 注意：传统 Task 会在后台线程执行！
        // Unity 的 API 大部分不是线程安全的，不能在这里调用

        await Task.Delay(1000);
        // Debug.Log("...");  ← 这里可能在后台线程！
        // 如果要回到主线程：
        await Awaitable.MainThreadAsync();
    }
}
```

**Unity 中的线程安全问题：**
```csharp
// Unity 的 API 不是线程安全的！
// ❌ 不能在后台线程中调用：
// transform.position = ...;
// GetComponent<Renderer>();
// Debug.Log(...);

// ✅ 只能在主线程中调用 Unity API
```

---

## 练习：完整的协程应用

```csharp
using UnityEngine;
using System.Collections;

public class Day05_Coroutine : MonoBehaviour
{
    [SerializeField] private float blinkInterval = 0.3f;
    [SerializeField] private int blinkTimes = 5;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();

        // 启动闪烁协程
        StartCoroutine(BlinkRoutine());

        // 并行启动第二个协程
        StartCoroutine(LogRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        Debug.Log("Blink started!");

        for (int i = 0; i < blinkTimes; i++)
        {
            // 隐藏
            rend.enabled = false;
            yield return new WaitForSeconds(blinkInterval);

            // 显示
            rend.enabled = true;
            yield return new WaitForSeconds(blinkInterval);
        }

        Debug.Log("Blink complete!");
    }

    IEnumerator LogRoutine()
    {
        // 同时运行的另一个协程
        int count = 0;
        while (count < 5)
        {
            Debug.Log($"LogRoutine running... ({count + 1})");
            yield return new WaitForSeconds(1f);
            count++;
        }
    }

    // 实用练习：避免多个协程冲突
    private Coroutine activeCoroutine;

    public void StartSafeRoutine()
    {
        // 停止之前的同类协程
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(MySafeRoutine());
    }

    IEnumerator MySafeRoutine()
    {
        Debug.Log("Safe routine started");
        yield return new WaitForSeconds(2f);
        Debug.Log("Safe routine ended");
        activeCoroutine = null;  // 清除引用
    }
}
```

---

---

## C++/Raylib 对照总结

| 概念 | C++ / Lua (Pico-8) | C# / Unity |
|------|-------------------|-----------|
| 协程 | Lua `cocreate`/`coresume` / `yield()` | `IEnumerator` / `yield return` |
| 等待一帧 | 手动计时器 | `yield return null` |
| 等待 N 秒 | 手动累加 dt | `yield return new WaitForSeconds(n)` |
| 异步操作 | `std::async` / 线程池 | `async` / `await` / Awaitable |
| 状态机 | 手动 enum + switch | 编译器自动生成的状态机 |
| 线程 | `std::thread` | `System.Threading.Tasks` |

## 停靠点

> 协程 = 能暂停的函数。`yield return X` = "暂停，等 X 事件发生后继续"。
> 编译器将协程方法转换为 IEnumerator 状态机，每次 MoveNext() 执行一段代码。
> `yield return null` = 等一帧，`WaitForSeconds` = 等 N 秒。
> async/await 是 C# 原生的异步方案，Unity 2022+ 通过 Awaitable 支持。
> 协程性能好但 yield new 有 GC 分配——可以缓存 WaitForSeconds 实例。

