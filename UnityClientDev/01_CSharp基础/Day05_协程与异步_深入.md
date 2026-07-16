# Day 5：协程与异步深入 — ValueTask、UniTask 与自定义 Awaitable

## 0. 为什么需要深入异步？

Day05 讲了协程和 async/await 的基本使用。但真实 Unity 项目中，Task 的开销不可忽视：

- **Task 是 class** → 每次 await 都在堆上分配
- **协程返回 IEnumerator** → 也是堆分配
- 如果你在 Update 中每帧 await 一个 Task，每秒 60 次堆分配 → GC 频繁

这一篇深入**零分配的异步方案**：ValueTask、UniTask、自定义 Awaitable（struct 实现），以及 Unity 的同步上下文机制。

---

## 1. ValueTask vs Task——何时用哪一个

### Task 的问题

```csharp
// Task 是 class——引用类型，堆分配
public async Task<int> GetScoreAsync()
{
    await Task.Delay(100);
    return 42;
}

// 每次调用 GetScoreAsync() 都会：
// 1. 创建 Task 对象（堆分配）
// 2. 如果等待，创建状态机结构体（也是堆分配）
// 3. 异步操作完成后，Task 还要继续存活

// 高频调用场景下，这些分配不可忽略
```

### ValueTask——可以零分配

```csharp
// ValueTask<T> 是 struct——值类型，可栈分配
public async ValueTask<int> GetScoreFastAsync()
{
    await Task.Delay(100);
    return 42;
}

// 但如果 await 内部是 Task.Delay（需要等待），
// ValueTask 内部会包装一个 Task 对象
// 零分配只在"同步完成"时发生
```

### 什么时候零分配？

```csharp
// 场景 1：结果立即可用——零分配！
public ValueTask<int> GetCachedScoreAsync()
{
    // _cachedScore 已经计算好了
    if (_cachedScore.HasValue)
    {
        // ValueTask 直接从值构造——零分配！
        return new ValueTask<int>(_cachedScore.Value);
    }
    // 需要异步计算——会分配
    return new ValueTask<int>(LoadScoreAsync());
}

// 场景 2：缓冲的结果
private int _lastFrame;
private int _lastScore;

public ValueTask<int> GetScoreForFrame(int frame)
{
    if (frame == _lastFrame)
    {
        // 缓存命中——同步返回，零分配
        return new ValueTask<int>(_lastScore);
    }
    return new ValueTask<int>(ComputeScoreAsync(frame));
}
```

### ValueTask 的限制

```csharp
// ✅ 可以：顺序 await
ValueTask<int> task = GetScoreAsync();
int result1 = await task;
int result2 = await task;  // 可以，但要注意底层资源

// ❌ 不可以：并行 await 同一个 ValueTask
ValueTask<int> task = GetScoreAsync();
var t1 = task.AsTask();  // 转换成 Task 可以并行
var t2 = task.AsTask();

// ❌ 不可以：存储 ValueTask 到字段后 await
private ValueTask<int> _saved;
void Bad()
{
    _saved = GetScoreAsync();
}
// await _saved;  // 危险！ValueTask 不应存储为字段

// ⚠️ 原因：ValueTask 内部可能包装了 IValueTaskSource
// IValueTaskSource 可能被复用——第二次 await 时已过期
```

### 选择指南

```csharp
// ✅ 用 Task 的场景：
// - 需要并行 await（Task.WhenAll）
// - 需要多次 await 同一个 task
// - 需要缓存 Task 到字段
// - 低频调用（每秒 < 100 次）

// ✅ 用 ValueTask 的场景：
// - 可能同步完成（缓存命中）
// - 高频调用（每秒数百次以上）
// - 异步结果不需要复用
// - 追求零分配

// Unity 中的推荐：
public ValueTask<int> GetPlayerScore(int playerId)
{
    if (_scoreCache.TryGetValue(playerId, out int score))
    {
        // 90% 的情况缓存命中——零分配
        return new ValueTask<int>(score);
    }
    // 10% 的情况需要从网络加载
    return new ValueTask<int>(LoadFromNetworkAsync(playerId));
}
```

---

## 2. UniTask 深入——为何零分配

### UniTask 的核心思想

```csharp
// UniTask 是 struct，不是 class
// 但它比 ValueTask 更进一步：

// 1. UniTask 完全零分配（包括异步路径）
// 2. UniTask 集成了 Unity 的 PlayerLoop
// 3. UniTask 有与 Unity 生命周期绑定的取消机制

using Cysharp.Threading.Tasks;

// 基本使用
public async UniTaskVoid Start()
{
    await UniTask.Delay(1000);  // 等 1 秒，零分配
    await UniTask.Yield();      // 等一帧，零分配
    await UniTask.NextFrame();  // 等下一帧，零分配
    Debug.Log("All done!");
}
```

### UniTask 为什么能零分配？

```csharp
// Task 的分配来源：
// 1. Task 对象本身（class）
// 2. 状态机装箱（async 状态机是 struct，但被 Task 包装时装箱）
// 3. MoveNextRunner 委托

// UniTask 的零分配实现：
// 1. UniTask 是 struct → 没有堆对象
// 2. 使用 IUniTaskSource（值类型接口）避免状态机装箱
// 3. 使用池化的 MoveNextRunner 复用委托

// 底层实现：
public readonly struct UniTask : IEquatable<UniTask>
{
    private readonly IUniTaskSource _source;  // 可能是池化的对象
    private readonly short _token;            // 版本控制
}
```

### UniTask 与 Unity 生命周期的集成

```csharp
public class PlayerController : MonoBehaviour
{
    private CancellationToken _destroyToken;

    void Awake()
    {
        // 获取绑定到 Destroy 事件的 CancellationToken
        _destroyToken = this.GetCancellationTokenOnDestroy();
    }

    async UniTaskVoid AttackSequence()
    {
        // 在第一个 Hit
        await PlayAnimation("Attack1");
        DealDamage(10);

        // 如果对象被 Destroy，自动取消等待！
        await UniTask.Delay(500, cancellationToken: _destroyToken);

        // 第二个 Hit
        await PlayAnimation("Attack2");
        DealDamage(20);
    }

    async UniTask PlayAnimation(string name)
    {
        // CancellationToken 传递
        await animator.PlayAndWait(name)
            .WithCancellation(_destroyToken);
    }
}
```

### UniTask 的常用功能

```csharp
// 1. 延迟——零分配
await UniTask.Delay(TimeSpan.FromSeconds(1f));

// 1a. 使用 TimeSpan
await UniTask.Delay(1000, ignoreTimeScale: true);  // 不受 Time.timeScale 影响

// 2. 等待下一帧——零分配
await UniTask.Yield(PlayerLoopTiming.Update);

// 3. 等待固定更新
await UniTask.Yield(PlayerLoopTiming.FixedUpdate);

// 4. 等待协程
await SomeCoroutine().ToUniTask();

// 5. 等待异步操作
await SceneManager.LoadSceneAsync("Scene2").ToUniTask();

// 6. 超时控制
await UniTask.Delay(5000, cancellationToken: cts.Token)
    .Timeout(3000)  // 3 秒超时
    .SuppressCancellationThrow();  // 不抛异常

// 7. 并发等待
var (result1, result2) = await UniTask.WhenAll(
    LoadTextureAsync("tex1"),
    LoadTextureAsync("tex2")
);

// 8. 任意一个完成
var asset = await UniTask.WhenAny(
    LoadFromCacheAsync(key),
    LoadFromNetworkAsync(url)
);
```

---

## 3. 自定义 Awaitable 类型——基于 struct 的实现

### 实现 IValueTaskSource

```csharp
using System.Threading.Tasks.Sources;

// 手动实现一个零分配的 awaitable
// 使用 IValueTaskSource 接口

public class DelayAwaitable : IValueTaskSource
{
    private ManualResetValueTaskSourceCore<bool> _core;

    public ValueTask AsValueTask()
    {
        return new ValueTask(this, _core.Version);
    }

    public void Complete()
    {
        _core.SetResult(true);
    }

    // IValueTaskSource 实现
    public void GetResult(short token)
        => _core.GetResult(token);

    public ValueTaskSourceStatus GetStatus(short token)
        => _core.GetStatus(token);

    public void OnCompleted(Action<object> continuation,
        object state, short token,
        ValueTaskSourceOnCompletedFlags flags)
        => _core.OnCompleted(continuation, state, token, flags);
}

// 池化版本——完全零分配
public class DelayPool
{
    private readonly ObjectPool<DelayAwaitable> _pool;

    public ValueTask Delay(int ms)
    {
        var awaitable = _pool.Get();
        var task = awaitable.AsValueTask();

        // 在 ms 毫秒后调用 awaitable.Complete()
        StartTimer(ms, () =>
        {
            awaitable.Complete();
            _pool.Return(awaitable);  // 复用
        });

        return task;
    }
}
```

### 基于 struct 的 Timer Awaitable

```csharp
// Unity 中的实现：基于 PlayerLoop 的 struct Awaitable
// 这是 UniTask 内部机制的简化版

public struct UnityTimerAwaitable
{
    private readonly float _delay;
    private readonly PlayerLoopTiming _timing;

    public UnityTimerAwaitable(float delay,
        PlayerLoopTiming timing = PlayerLoopTiming.Update)
    {
        _delay = delay;
        _timing = timing;
    }

    // 编译器寻找的 GetAwaiter 方法
    public UnityTimerAwaiter GetAwaiter()
    {
        return new UnityTimerAwaiter(_delay, _timing);
    }
}

// struct awaiter——零分配！
public struct UnityTimerAwaiter : INotifyCompletion
{
    private readonly float _delay;
    private readonly PlayerLoopTiming _timing;
    private float _startTime;

    public bool IsCompleted
    {
        get
        {
            if (_startTime == 0)
                _startTime = Time.time;
            return Time.time - _startTime >= _delay;
        }
    }

    public UnityTimerAwaiter(float delay, PlayerLoopTiming timing)
    {
        _delay = delay;
        _timing = timing;
        _startTime = 0;
    }

    public void OnCompleted(Action continuation)
    {
        // 在 PlayerLoop 中注册回调
        PlayerLoopHelper.Register(_timing, () =>
        {
            if (IsCompleted)
            {
                continuation?.Invoke();
                return true;  // 取消注册
            }
            return false;
        });
    }

    public void GetResult() { }
}

// 使用——零分配！
public async UniTaskVoid UseCustomAwaiter()
{
    await new UnityTimerAwaitable(1f);
    Debug.Log("1 second passed, zero allocation!");
}
```

---

## 4. Unity 中的 SynchronizationContext

### 什么是 SynchronizationContext？

```csharp
// SynchronizationContext 定义了"在哪执行代码"
// 在 Unity 中：主线程有一个 UnitySynchronizationContext

// 它的作用：
// 1. 捕获当前执行上下文（哪个线程）
// 2. 允许把代码发回到那个线程执行

// Unity 的版本：
// 在每帧的 Update 循环之前，Unity 会处理队列中的 Continuation
```

### async/await 的线程切换

```csharp
// await 前后的线程环境

public async UniTaskVoid TestContext()
{
    // 开始：主线程
    Debug.Log($"Before await: Thread {Environment.CurrentManagedThreadId}");

    // 如果 await 的操作完成后恢复了上下文
    await UniTask.Delay(100);

    // 之后：主线程（UnitySynchronizationContext 确保回到主线程）
    Debug.Log($"After await: Thread {Environment.CurrentManagedThreadId}");
    // 可以安全调用 Unity API
    transform.position = Vector3.one;
}
```

### ConfigureAwait 的作用

```csharp
public async UniTaskVoid TestConfigureAwait()
{
    // 默认行为：捕获上下文，恢复到原线程
    await SomeOperationAsync();
    // 回到主线程 —— 安全

    // ConfigureAwait(false)：不捕获上下文
    // 直接在完成线程继续执行
    await SomeOperationAsync().ConfigureAwait(false);
    // ❌ 危险！可能在后台线程——不能调用 Unity API！
    // transform.position = ...  // 崩溃！
}
```

### Unity 中的 SynchronizationContext 内部

```csharp
// Unity 的 SynchronizationContext 简化实现：

// 在 PlayerLoop 中，每帧：
void Update()
{
    // 1. 处理异步操作的延续
    UnitySynchronizationContext.ExecutePendingContinuations();

    // 2. 调用所有 MonoBehaviour 的 Update
    // ...

    // 3. 处理协程的 MoveNext
    CoroutineManager.UpdateCoroutines();
}

// Continuation 队列：
// 当 await 的操作完成时，会向 UnitySynchronizationContext 发送一个回调
// Unity 在主线程的 PlayerLoop 中执行这些回调
```

### 在 Unity 中使用自定义 SynchronizationContext

```csharp
// 正常情况下不需要修改
// 但如果你有自己的工作线程系统：

public class WorkerThreadSystem
{
    private readonly SynchronizationContext _mainContext;
    private Thread _worker;

    public WorkerThreadSystem()
    {
        // 捕获主线程上下文
        _mainContext = SynchronizationContext.Current;
    }

    public async UniTask<T> RunOnWorker<T>(Func<T> work)
    {
        // 在工作线程执行耗时操作
        var result = await Task.Run(work);

        // 回到主线程
        await _mainContext;

        // 现在安全了
        ApplyResultToUnity(result);
        return result;
    }
}

// 扩展方法
public static class SyncContextExtensions
{
    public static SynchronizationContextAwaiter GetAwaiter(
        this SynchronizationContext context)
    {
        return new SynchronizationContextAwaiter(context);
    }
}

public struct SynchronizationContextAwaiter : INotifyCompletion
{
    private readonly SynchronizationContext _context;

    public bool IsCompleted => false;

    public SynchronizationContextAwaiter(SynchronizationContext context)
    {
        _context = context;
    }

    public void OnCompleted(Action continuation)
    {
        // 通过 SynchronizationContext 发回到目标线程
        _context.Post(_ => continuation(), null);
    }

    public void GetResult() { }
}
```

---

## 5. ConfigureAwait(false)——为什么重要

### 死锁风险

```csharp
// 在没有 UnitySynchronizationContext 的环境中（如控制台应用、Web API）：
// ConfigureAwait(false) 可以防止死锁

// 死锁场景（非 Unity）：
public async Task<string> LoadDataAsync()
{
    // 捕获上下文
    await SomeHttpRequest();
    // 需要回到原上下文

    return "data";
}

void ButtonClick()
{
    // 阻塞等待 → 死锁！
    // var data = LoadDataAsync().Result;  // ❌ 死锁！
    // ButtonClick 线程阻塞
    // LoadDataAsync 完成时需要回到该线程
    // → 互相等待！
}

// 修复：
public async Task<string> LoadDataAsync()
{
    // 不捕获上下文——完成后不回到原线程
    await SomeHttpRequest().ConfigureAwait(false);
    return "data";
}
```

### Unity 中的情况

```csharp
// Unity 有 UnitySynchronizationContext，所以不会死锁
// 但 ConfigureAwait(false) 仍然有意义：

// 在 Unity 中：
// 默认（ConfigureAwait(true)）：
await LoadAssetAsync();
// 回到主线程 —— 可以调用 Unity API

// ConfigureAwait(false)：
await LoadAssetAsync().ConfigureAwait(false);
// 在后台线程继续 —— 不能调用 Unity API 但性能更好
// 因为省去了"回到主线程"的开销

// 适用于纯数据处理
public async UniTask<float> ProcessDataAsync(float[] data)
{
    var raw = await LoadFromDiskAsync(data).ConfigureAwait(false);
    // 在后台线程处理——不阻塞主线程
    float result = ExpensiveMath(raw);
    // 需要回到主线程时
    await Awaitable.MainThreadAsync();
    return result;
}
```

### Unity 开发者的配置建议

```csharp
// 在 Unity 中使用 UniTask：
// UniTask 默认不捕获上下文（类似 ConfigureAwait(false)）
// 需要回到主线程时用：

// 1. 自动回到主线程
await UniTask.SwitchToMainThread();

// 2. 切换到后台线程
await UniTask.SwitchToThreadPool();

// 3. 显式控制
public async UniTaskVoid Example()
{
    // 在后台线程做计算
    await UniTask.SwitchToThreadPool();
    float result = HeavyComputation();

    // 回到主线程更新 UI
    await UniTask.SwitchToMainThread();
    UpdateUI(result);
}
```

---

## 6. Channel&lt;T&gt;——生产者-消费者模式

### 基本用法

```csharp
using System.Threading.Channels;

// 创建 Channel
Channel<int> channel = Channel.CreateBounded<int>(100);
// Bounded：有容量限制（背压支持）
// Unbounded：无限制（内存可能暴涨）

// 生产者
async UniTask Producer(ChannelWriter<int> writer)
{
    for (int i = 0; i < 1000; i++)
    {
        // 写入数据——如果 Channel 满了会等待
        await writer.WriteAsync(i);
    }
    writer.Complete();  // 通知消费者生产结束
}

// 消费者
async UniTask Consumer(ChannelReader<int> reader)
{
    // await foreach 消费所有数据
    await foreach (var item in reader.ReadAllAsync())
    {
        Process(item);
    }
}
```

### Unity 中的网络消息队列

```csharp
public class NetworkMessageQueue : MonoBehaviour
{
    // 有界 Channel——防止内存无限增长
    private Channel<NetworkMessage> _messageChannel
        = Channel.CreateBounded<NetworkMessage>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
                // 队列满时丢弃最旧的消息
            });

    void Start()
    {
        // 在主线程中消费消息
        ConsumeMessagesAsync().Forget();
    }

    // 网络线程调用——生产消息
    public void OnMessageReceived(NetworkMessage msg)
    {
        _messageChannel.Writer.TryWrite(msg);
    }

    // 主线程消费
    private async UniTaskVoid ConsumeMessagesAsync()
    {
        var reader = _messageChannel.Reader;

        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out NetworkMessage msg))
            {
                // 在主线程处理消息
                HandleMessage(msg);
            }
        }
    }

    void HandleMessage(NetworkMessage msg)
    {
        switch (msg.Type)
        {
            case MessageType.PlayerJoined:
                SpawnPlayer(msg.Data);
                break;
            case MessageType.PlayerMoved:
                UpdatePosition(msg.Data);
                break;
            case MessageType.BulletFired:
                SpawnBullet(msg.Data);
                break;
        }
    }

    void OnDestroy()
    {
        _messageChannel.Writer.Complete();
    }
}
```

### Channel 作为协程之间的桥梁

```csharp
// 协程和 async/await 之间的数据交换

public class CoroutineToAsync : MonoBehaviour
{
    private Channel<AsyncOperation> _opChannel
        = Channel.CreateUnbounded<AsyncOperation>();

    void Start()
    {
        // 启动协程生产者
        StartCoroutine(LoadSequence());

        // async 消费者
        ProcessLoadedAssets().Forget();
    }

    IEnumerator LoadSequence()
    {
        // 协程中加载资源
        var req1 = Resources.LoadAsync<Texture2D>("texture1");
        yield return req1;
        _opChannel.Writer.TryWrite(req1);  // 通知 async 消费者

        var req2 = Resources.LoadAsync<Mesh>("mesh1");
        yield return req2;
        _opChannel.Writer.TryWrite(req2);
    }

    async UniTaskVoid ProcessLoadedAssets()
    {
        await foreach (var op in _opChannel.Reader.ReadAllAsync())
        {
            // 处理加载好的资源
            if (op.asset is Texture2D tex)
                ApplyTexture(tex);
            else if (op.asset is Mesh mesh)
                ApplyMesh(mesh);
        }
    }
}
```

---

## 7. IAsyncDisposable 和 await using

### 问题：Dispose 可能是异步的

```csharp
// 传统 Dispose 是同步的
public class ResourceHolder : IDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        _stream.Dispose();  // 同步，可能阻塞
    }
}

// 但有些资源释放涉及异步操作：
// - 网络连接关闭
// - 缓冲区刷新到磁盘
// - 等待后台线程完成
```

### IAsyncDisposable——异步释放

```csharp
public class AsyncResourceHolder : IAsyncDisposable
{
    private NetworkStream _stream;
    private MemoryStream _buffer;

    public async ValueTask DisposeAsync()
    {
        // 异步刷新缓冲区
        await _buffer.FlushAsync();

        // 异步关闭网络连接
        await _stream.ShutdownAsync();

        // 同步释放
        _stream.Dispose();
        _buffer.Dispose();
    }
}

// 使用：await using
public async UniTaskVoid UseResource()
{
    // await using = 自动调用 DisposeAsync
    await using var resource = new AsyncResourceHolder();

    // 使用 resource...
    await resource.SendDataAsync(data);

    // 块结束时自动 await resource.DisposeAsync()
}
```

### Unity 中的异步资源释放

```csharp
public class AsyncAssetLoader : IAsyncDisposable
{
    private AssetBundle _bundle;
    private List<Texture2D> _loadedTextures = new();
    private List<AudioClip> _loadedClips = new();

    public async UniTask LoadBundleAsync(string path)
    {
        var request = AssetBundle.LoadFromFileAsync(path);
        await request;
        _bundle = request.assetBundle;
    }

    public async UniTask<Texture2D> LoadTextureAsync(string name)
    {
        var request = _bundle.LoadAssetAsync<Texture2D>(name);
        await request;
        var tex = request.asset as Texture2D;
        _loadedTextures.Add(tex);
        return tex;
    }

    public async ValueTask DisposeAsync()
    {
        // 异步卸载所有资源
        foreach (var tex in _loadedTextures)
        {
            Resources.UnloadAsset(tex);
            await UniTask.Yield();  // 分帧卸载，不卡主线程
        }

        if (_bundle != null)
        {
            // 异步卸载 AssetBundle
            var op = _bundle.UnloadAsync(true);
            await op;
        }

        _loadedTextures.Clear();
        _loadedClips.Clear();
    }
}

// 使用：
public class GameScene : MonoBehaviour
{
    private AsyncAssetLoader _loader;

    async UniTaskVoid Start()
    {
        await using (_loader = new AsyncAssetLoader())
        {
            await _loader.LoadBundleAsync("characters");
            var tex = await _loader.LoadTextureAsync("hero");
            ApplyTexture(tex);

            // 场景结束时自动调用 _loader.DisposeAsync()
            // 即使中途抛异常也会正确释放
        }
    }
}
```

### IDisposable vs IAsyncDisposable

| | IDisposable | IAsyncDisposable |
|--|-----------|-----------------|
| 返回值 | void | ValueTask |
| 同步/异步 | 同步 | 异步 |
| 使用语法 | `using` | `await using` |
| 适用场景 | 内存释放、句柄关闭 | 网络关闭、缓冲刷新、卸载资源 |
| 复合使用 | `Dispose()` | `DisposeAsync()` |

---

## C++/Raylib 对照总结

| 概念 | C++ | C# |
|------|-----|-----|
| 零分配异步 | `co_await` + `std::coroutine_handle` | UniTask / ValueTask |
| 线程上下文 | `std::jthread` + 手动调度 | `SynchronizationContext` |
| 生产者-消费者 | `std::channel` (C++26?) | `System.Threading.Channels.Channel<T>` |
| 异步释放 | RAII 保证 | `IAsyncDisposable` + `await using` |
| 协程与线程 | C++20 协程是栈无关的 | Unity 协程在主线程，async 可跨线程 |
| 背压控制 | 手写队列 | `Channel.CreateBounded` |

## 停靠点

> ValueTask 是 struct 版的 Task——只在同步完成时零分配。异步路径仍然会分配。不能多次 await。
> UniTask 是 Unity 社区的标准零分配异步方案——完全零分配，集成 PlayerLoop，绑定生命周期。
> 自定义 Awaitable 通过实现 IValueTaskSource 或 INotifyCompletion 来实现——struct 版本可以零分配。
> Unity 的 SynchronizationContext 确保 await 后回到主线程。ConfigureAwait(false) 跳过这个恢复——性能更高但不能再调 Unity API。
> Channel&lt;T&gt; 是线程安全的生产者-消费者队列——有界模式支持背压（反压），避免内存暴涨。
> IAsyncDisposable + await using 处理异步资源释放——在 Unity 中适合 AssetBundle、网络连接等场景。
