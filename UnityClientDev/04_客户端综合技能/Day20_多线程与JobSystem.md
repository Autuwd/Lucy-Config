# Day 20：多线程与 Job System — 从线程到并行计算

## 0. 为什么需要多线程？

Raylib/C++ 的游戏是**单线程模型**：一帧接一帧地执行。

```
单线程（一帧内）：
处理输入 → 更新逻辑 → 物理计算 → 渲染 → 处理输入 → ...

如果某个操作卡了 100ms，整帧就卡了 100ms，画面掉到 10fps。
```

Unity 也是单线程帧循环，但有些操作可以**放到后台线程**执行：

```
多线程：
主线程：  处理输入 → 更新逻辑 → 渲染...
后台线程：下载资源、路径搜索、文件读写...

主线程卡顿 = 画面掉帧
后台线程卡顿 = 不影响帧率，但影响响应速度
```

**核心原则：UI/Unity API 只能在主线程调用，耗时计算放到后台线程。**

---

## 1. 进程 vs 线程 vs 协程

```
┌──────────── 进程（Process）────────────┐
│  游戏.exe                               │
│  ┌─────── 主线程 ──────┐                │
│  │ Update() 渲染 物理   │                │
│  └─────────────────────┘                │
│  ┌─────── 网络线程 ─────┐                │
│  │ 接收数据 解析消息     │                │
│  └─────────────────────┘                │
│  ┌─────── 加载线程 ─────┐                │
│  │ 读取文件 解压资源     │                │
│  └─────────────────────┘                │
└────────────────────────────────────────┘
```

| 概念 | 内存 | 切换开销 | 通信方式 |
|------|------|---------|---------|
| 进程 | 独立 | 大 | IPC（进程间通信） |
| 线程 | 共享 | 中 | 共享变量 + lock |
| 协程 | 共享 | 极小 | yield 暂停/恢复 |

**两条铁律：**
1. **Unity API 只能在主线程调用**（Transform、GameObject、Camera...）
2. **耗时操作放到后台线程**（网络、文件IO、大规模计算）

---

## 2. C# 多线程基础

### Thread — 最底层

```csharp
using System.Threading;

public class ThreadExample
{
    private Thread workerThread;

    public void Start()
    {
        // 创建并启动线程
        workerThread = new Thread(DoWork);
        workerThread.Start();
    }

    private void DoWork()
    {
        Debug.Log($"后台线程：{Thread.CurrentThread.ManagedThreadId}");
        // 模拟耗时操作
        Thread.Sleep(2000);
        Debug.Log("工作完成");

        // ❌ 不能在这里调用 Unity API！
        // transform.position = newPos;  // 会报错
    }

    public void Stop()
    {
        workerThread?.Join(); // 等待线程结束
    }
}
```

### ThreadPool — 线程池

```csharp
// 线程池：避免频繁创建/销毁线程（线程创建开销约 1MB 内存）
ThreadPool.QueueUserWorkItem(state =>
{
    Debug.Log($"线程池执行：{Thread.CurrentThread.ManagedThreadId}");
    Thread.Sleep(1000);
});
```

### Task — .NET TPL（推荐）

```csharp
using System.Threading.Tasks;

public class TaskExample
{
    public async Task DoComplexCalculation()
    {
        // Task.Run 自动从线程池取线程
        int result = await Task.Run(() =>
        {
            int sum = 0;
            for (int i = 0; i < 100_000_000; i++)
                sum += i;
            return sum;
        });

        // await 之后自动回到调用上下文（在 Unity 中是主线程）
        Debug.Log($"计算结果：{result}");
    }
}
```

---

## 3. 线程间通信

线程共享内存，但不能同时读写同一个变量——需要**同步机制**。

### lock — 互斥锁

```csharp
public class ThreadSafeCounter
{
    private int count = 0;
    private readonly object lockObj = new();

    public void Increment()
    {
        // lock 确保同一时间只有一个线程进入
        lock (lockObj)
        {
            count++;
        }
    }

    public int GetCount()
    {
        lock (lockObj)
        {
            return count;
        }
    }
}
```

**lock 的底层原理：**

```
Monitor.Enter(lockObj)  → 尝试获取锁
  ├─ 成功 → 继续执行
  └─ 失败 → 线程阻塞（休眠，不占 CPU）

Monitor.Exit(lockObj)   → 释放锁
  └─ 唤醒等待队列中的下一个线程
```

### ConcurrentQueue — 线程安全队列（最常用）

```csharp
using System.Collections.Concurrent;

public class NetworkMessageQueue : MonoBehaviour
{
    // 线程安全队列：不需要 lock
    private ConcurrentQueue<ServerMessage> messageQueue = new();

    // 网络线程调用
    public void OnMessageReceived(ServerMessage msg)
    {
        messageQueue.Enqueue(msg);  // 可以从任何线程调用
    }

    // 主线程调用
    void Update()
    {
        // 每帧处理所有待处理消息
        while (messageQueue.TryDequeue(out ServerMessage msg))
        {
            ProcessMessage(msg);  // 这里可以安全使用 Unity API
        }
    }
}
```

### 主线程回调模式

```csharp
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher instance;
    private readonly ConcurrentQueue<System.Action> actions = new();

    void Awake() => instance = this;

    void Update()
    {
        while (actions.TryDequeue(out var action))
            action();
    }

    // 任何线程都可以调用这个
    public static void RunOnMainThread(System.Action action)
    {
        instance.actions.Enqueue(action);
    }
}

// 在后台线程中使用
await Task.Run(() =>
{
    Thread.Sleep(3000);
    MainThreadDispatcher.RunOnMainThread(() =>
    {
        // 现在在主线程了
        GameObject.Find("Player").transform.position = newPos;
    });
});
```

---

## 4. Unity 中的 async/await 陷阱

### AsyncOperation — Unity 的异步操作

Unity 的异步操作（SceneManager.LoadSceneAsync、AssetBundle.LoadAssetAsync、UnityWebRequest.SendWebRequest）不使用 C# 的 Task，而是自己的 AsyncOperation。

```csharp
using UnityEngine.SceneManagement;

public class AsyncOperationExample : MonoBehaviour
{
    // ❌ 错误：AsyncOperation 不是 Task，不能直接 await
    async void LoadSceneBad()
    {
        // SceneManager.LoadSceneAsync("Level2") 返回 AsyncOperation
        // AsyncOperation 不实现 GetAwaiter！
        // await SceneManager.LoadSceneAsync("Level2"); // 编译错误
    }

    // ✅ 正确：用协程
    IEnumerator LoadSceneGood()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("Level2");
        op.allowSceneActivation = false; // 控制何时切换场景

        // 显示加载进度
        while (op.progress < 0.9f)
        {
            Debug.Log($"加载进度：{op.progress * 100}%");
            yield return null;
        }

        // 加载完成，切换场景
        op.allowSceneActivation = true;
    }
}
```

### 包装 AsyncOperation 为 Task

```csharp
public static class AsyncOperationExtensions
{
    // 把 AsyncOperation 包装成 Task
    public static Task AsTask(this AsyncOperation op)
    {
        var tcs = new TaskCompletionSource<bool>();
        op.completed += _ => tcs.SetResult(true);
        return tcs.Task;
    }
}

// 现在可以 await 了
public class LoadingScreen : MonoBehaviour
{
    async void Start()
    {
        var op = SceneManager.LoadSceneAsync("Level2");
        await op.AsTask(); // 自定义扩展方法
        Debug.Log("场景加载完成，继续执行...");
    }
}
```

### UniTask — Unity 专用的异步方案

UniTask 是第三方库，解决了 C# Task 在 Unity 中的问题：

1. **Task 有 GC 分配**（每次 await 产生对象），UniTask 是 struct
2. **Task 依赖 SynchronizationContext**，Unity 中可能回不到主线程
3. **UniTask 支持 AsyncOperation** 直接 await

```csharp
// 需要从 Package Manager 安装 UniTask
// 安装后：
using Cysharp.Threading.Tasks;

public class UniTaskExample : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        // 直接 await AsyncOperation
        await SceneManager.LoadSceneAsync("Level2");

        // 延迟
        await UniTask.Delay(1000); // 等价于 Task.Delay(1000)，但不产生 GC

        // 下一帧
        await UniTask.Yield();

        // 等待指定帧数
        await UniTask.DelayFrame(10);

        // 切换到后台线程
        await UniTask.SwitchToThreadPool();
        // 现在在后台线程
        int result = ComputeHeavy();

        // 切换回主线程
        await UniTask.SwitchToMainThread();
        // 现在在主线程，可以安全使用 Unity API
        transform.position = newPos;
    }

    private int ComputeHeavy()
    {
        int sum = 0;
        for (int i = 0; i < 1_000_000; i++) sum += i;
        return sum;
    }
}
```

### async void 的陷阱

```csharp
// ❌ 危险：async void 的异常无法捕获
async void OnButtonClick()
{
    await Task.Run(() => throw new Exception("崩溃"));
    // 异常不会被 try-catch 捕获，直接崩溃进程
}

// ✅ 安全：async Task 的异常可捕获
async Task SafeMethod()
{
    await Task.Run(() => throw new Exception("错误"));
}

// 调用时：
try
{
    await SafeMethod();
}
catch (Exception e)
{
    Debug.LogError($"捕获异常：{e.Message}");
}
```

---

## 5. Unity 的 Job System

Unity 自己实现的**高性能多线程系统**，比 C# 的 Thread/Task 更高效：

- 自动利用所有 CPU 核心（不用手动管理线程）
- 避免 GC（使用值类型 struct）
- 与 Burst Compiler 配合获得极致性能（生成优化后的原生代码）

### IJob 基础

```csharp
using Unity.Jobs;
using Unity.Collections;

// 1. 定义一个 Job（必须是 struct）
public struct MoveJob : IJob
{
    // Job 中的数据必须用 NativeContainer
    public NativeArray<Vector3> positions;
    public Vector3 direction;
    public float speed;
    public float deltaTime;

    // 2. 执行逻辑
    public void Execute()
    {
        for (int i = 0; i < positions.Length; i++)
            positions[i] = positions[i] + direction * speed * deltaTime;
    }
}

// 3. 调度 Job
public class JobExample : MonoBehaviour
{
    void Update()
    {
        // 准备数据
        NativeArray<Vector3> positions = new(100, Allocator.TempJob);

        // 创建 Job
        MoveJob job = new MoveJob
        {
            positions = positions,
            direction = Vector3.forward,
            speed = 5f,
            deltaTime = Time.deltaTime
        };

        // 调度到工作线程
        JobHandle handle = job.Schedule();

        // 等待完成（在主线程等）
        handle.Complete();

        // 使用结果
        Debug.Log(positions[0]);

        // 释放内存
        positions.Dispose();
    }
}
```

### IJobParallelFor — 并行处理数组

```csharp
public struct IncrementJob : IJobParallelFor
{
    public NativeArray<int> numbers;

    public void Execute(int index)
    {
        numbers[index] += 1;
        // Unity 会自动把数组拆分到多个线程并行处理
    }
}

// 使用
NativeArray<int> data = new(1000, Allocator.TempJob);
IncrementJob job = new IncrementJob { numbers = data };

// 第二个参数 64 = batch size，每 64 个元素一个工作包
JobHandle handle = job.Schedule(data.Length, 64);
handle.Complete();
data.Dispose();
```

### Job 依赖链

```csharp
// Job 可以串联：Job B 必须在 Job A 完成后才能执行
public struct PrepareDataJob : IJob
{
    public NativeArray<int> data;

    public void Execute()
    {
        for (int i = 0; i < data.Length; i++)
            data[i] = i * 2;
    }
}

public struct ProcessDataJob : IJob
{
    public NativeArray<int> data;

    public void Execute()
    {
        for (int i = 0; i < data.Length; i++)
            data[i] += 10;
    }
}

// 串联执行
NativeArray<int> data = new(1000, Allocator.TempJob);

var prepare = new PrepareDataJob { data = data };
var process = new ProcessDataJob { data = data };

// prepare 先执行
JobHandle prepareHandle = prepare.Schedule();

// process 依赖 prepare，传 prepareHandle 作为依赖
JobHandle processHandle = process.Schedule(prepareHandle);

// 只等最后一个 Job 完成
processHandle.Complete();

data.Dispose();
```

---

## 6. 实战：后台加载资源

```csharp
public class AsyncResourceLoader : MonoBehaviour
{
    public async Task<Texture2D> LoadTextureAsync(string url)
    {
        // 使用 Task.Run 避免卡主线程
        byte[] bytes = await Task.Run(() =>
        {
            using var client = new System.Net.WebClient();
            return client.DownloadData(url);
        });

        // 回到主线程创建 Unity 对象
        Texture2D tex = new(2, 2);
        tex.LoadImage(bytes);
        return tex;
    }
}
```

---

## 7. 多线程常见陷阱

### 死锁

```csharp
// 两个线程互相等对方释放锁 → 永远卡死
Thread 1: lock (a) { lock (b) { ... } }
Thread 2: lock (b) { lock (a) { ... } }

// 解决：始终按同一顺序获取锁
Thread 1: lock (a) { lock (b) { ... } }
Thread 2: lock (a) { lock (b) { ... } }

// 或者用 Monitor.TryEnter 设置超时
if (Monitor.TryEnter(lockObj, TimeSpan.FromSeconds(1)))
{
    try { /* 临界区 */ }
    finally { Monitor.Exit(lockObj); }
}
else
{
    Debug.LogWarning("获取锁超时，可能死锁");
}
```

### 竞态条件

```csharp
int counter = 0;

// 两个线程同时执行
counter++; // 不是原子操作！
// 实际是：读取 → 加1 → 写入
// 可能两个线程都读到了 0，都写成 1，丢失了一次增加

// 解决：用 Interlocked 或 lock
Interlocked.Increment(ref counter);
```

### Task 未等待

```csharp
// ❌ 忘记 await
void Start()
{
    DoWork(); // Task 被触发但没人等，可能被 GC 回收
}

// ✅ 正确处理
async void Start()
{
    await DoWork(); // await 确保完成
}

// `async void` 只用于事件处理！
// 正常方法用 `async Task`
```

### 主线程检测

```csharp
// Unity 中检测是否在主线程
public static class ThreadSafety
{
    private static int mainThreadId;

    [RuntimeInitializeOnLoadMethod]
    private static void Init()
    {
        mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public static bool IsMainThread
        => Thread.CurrentThread.ManagedThreadId == mainThreadId;

    public static void AssertMainThread()
    {
        if (!IsMainThread)
            throw new System.InvalidOperationException(
                "Unity API 只能在主线程调用！");
    }
}
```

---

## 8. 练习

### 练习 1：Task 并行计算

```csharp
// 计算从 1 到 1,000,000 的质数个数
// 要求：
// 1. 用 Task.Run 分 4 个线程并行计算
// 2. 等待所有线程完成
// 3. 输出结果
// 4. 用 Stopwatch 计时，对比单线程和多线程的速度
```

### 练习 2：MainThreadDispatcher

```csharp
// 扩展我们实现的 MainThreadDispatcher：
// 1. 支持等待执行结果（类似 Invoke 但能返回值）
// 2. 添加延时执行（delay 参数）
// 3. 添加每帧执行直到条件满足
```

### 练习 3：Job System 粒子更新

```csharp
// 用 IJobParallelFor 更新 10000 个粒子的位置
// 粒子结构：position, velocity, life
// 逻辑：位置 += 速度 * dt，life -= dt，life <= 0 时重置到随机位置
// 对比不使用 Job System 的版本，感受性能差异
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 线程 | 并行执行路径，共享内存 |
| Task | C# 推荐的多线程抽象 |
| lock | 互斥锁，保护共享变量 |
| ConcurrentQueue | 线程安全队列，主线程/后台线程通信 |
| AsyncOperation | Unity 自己的异步操作，不是 Task |
| UniTask | Unity 专用 async 库，零 GC |
| Job System | Unity 的高性能多线程方案 |
| Burst | 编译优化，让 Job 速度接近 C++ |
| 铁律 | Unity API 只能在主线程调用 |

**对比 Raylib/C++：** Raylib 是纯单线程模型，没有多线程场景。C# 的 Thread/Task 和 C++ 的 std::thread 思路一致（都是操作系统线程的封装），但 Task 更高级（自动线程池、async/await 语法糖）。Unity Job System 是 Unity 自己造的轮子——C++ 没有直接对标，但概念类似 OpenMP 的 `#pragma omp parallel for`。Burst Compiler 类似于 C++ 的编译器优化（-O2/-O3），但在 Unity 的 Job 上下文中被大量使用。
