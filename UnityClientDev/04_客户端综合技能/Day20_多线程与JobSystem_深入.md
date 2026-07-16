# Day 20：多线程与 Job System — Burst 编译、NativeContainer、调度器内幕

## 0. 为什么需要深入 Job System？

基础篇介绍了 IJob、IJobParallelFor 的基本用法。但实际项目中你会发现：

```
基础 Job System 的局限：
1. 性能不够好 → 需要 Burst 编译器
2. 内存管理复杂 → 需要理解 NativeContainer 生命周期
3. 调度效率低 → 需要理解 Job 调度器内部机制
4. 数据类型受限 → 需要用特殊集合

Job System 不是"另一个线程池"，而是为游戏量身定做的并行框架：
- 自动利用所有 CPU 核心
- 避免缓存行冲突（Cache-line bouncing）
- 内存分配策略可控
- 编译期优化（Burst）
```

---

## 1. Burst Compiler 深入

### Burst 的工作原理

```
普通 C# IL 代码：
player.position.x += 1.0f;
↓ C# 编译器 (.NET)
IL: ldloc.0; ldfld position; ldfld x; ldc.r4 1.0; add; stfld x
↓ Mono/IL2CPP JIT
x86: movss xmm0, [eax]; addss xmm0, [const]; movss [eax], xmm0
（使用 SSE 标量指令，一次只处理一个 float）

Burst 编译后：
player.position.x += 1.0f;
↓ Burst 编译器 (LLVM)
x86: addss xmm0, xmm1  ← 仍然标量，但更优

但如果是批量操作（比如处理 1000 个粒子）：
普通 C#：循环 + 数组索引 + 边界检查
Burst：自动向量化（SIMD），一次处理 4 个 float
     vaddps ymm0, ymm1, ymm2  ← AVX 处理 8 个 float 并行
```

### Unity.Mathematics 库

```csharp
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

// Burst 需要使用 Unity.Mathematics 中的类型
// 不像普通 Vector3（是 class），math 的类型是 struct

[BurstCompile]
public struct ParticleUpdateJob : IJobParallelFor
{
    public NativeArray<float3> positions;
    public NativeArray<float3> velocities;
    public float deltaTime;
    public float3 gravity;

    public void Execute(int index)
    {
        // 使用 math 库的函数（Burst 可优化）
        velocities[index] += gravity * deltaTime;
        positions[index] += velocities[index] * deltaTime;

        // 反弹检测
        if (positions[index].y < 0f)
        {
            positions[index].y = 0f;
            velocities[index].y = math.abs(velocities[index].y) * 0.5f;
        }
    }
}
```

### Burst 能优化的运算

```
Burst 擅长：
- 纯数值计算（浮点运算、矩阵乘法）
- 循环（自动向量化）
- struct 方法调用（内联展开）
- 数学函数（math.sin, math.normalize 等）

Burst 不擅长：
- 字符串操作
- 反射
- 虚方法调用
- 异常处理
- 托管对象访问

如果 Job 中有大量分支，Burst 的优化效果会下降：
// ❌ 每个粒子走不同分支 → 导致 SIMD 向量化失效
if (particle.type == 0) DoA();
else if (particle.type == 1) DoB();
else DoC();
```

### Burst 调试技巧

```csharp
// 1. 检查 Burst 是否生效
// 在 Player Settings 中开启 Jobs → Burst → Enable Burst Compilation
// 运行游戏 → 打开 Burst Inspector（Window → Burst Inspector）
// 查看你的 Job 是否显示 "Burst Compilation Succeeded"

// 2. 在 Burst 中调试
[BurstCompile(CompileSynchronously = true)]
// 同步编译模式，确保 Burst 在 Editor 中编译

[BurstCompile(Debug = true)]
// 调试模式（性能会下降，但可以捕获异常）

[BurstCompile(FloatMode = FloatMode.Fast)]
[BurstCompile(FloatPrecision = FloatPrecision.Low)]
// 快速模式：允许浮点重排序（更快的数学运算）
// 如果数学结果对顺序敏感，用 FloatMode.Strict

// 3. 禁止 Burst（用于对比性能）
// [BurstCompile(DisableDirectCall = true)]
```

### 矩阵运算的 Burst 优化

```csharp
[BurstCompile]
public struct MatrixTransformJob : IJobParallelFor
{
    public NativeArray<float3> vertices;
    public float4x4 transformMatrix;  // Unity.Mathematics 的矩阵

    public void Execute(int index)
    {
        // float4x4 的乘法在 Burst 中会被编译为 SIMD 指令
        float3 v = vertices[index];
        vertices[index] = math.transform(transformMatrix, v);
        // 相当于 4 次乘加并行执行
    }
}

// 手动布局优化
// Burst 对 struct 的布局敏感：
// ✅ 好的布局（连续内存 + 对齐）
public struct ParticleData
{
    public float3 position;  // 12 字节（float4 对齐到 16 字节）
    public float life;       // 4 字节 → 凑到 16 字节
    public float3 velocity;  // 12 字节
    public float mass;       // 4 字节 → 凑到 16 字节
}

// ❌ 不好的布局（填充浪费）
public struct BadParticle
{
    public float x;          // 4
    public float y;          // 4
    public float life;       // 4
    public float velocityX;  // 4
    // 总共 16 字节（还行）
    // 但如果中间插了 bool → 填充浪费
}
```

---

## 2. Job System 安全检查

### 安全检查做了什么

```
Unity 的 Job System 有内置安全检查（Editor 模式）：
1. 写入冲突检测：两个 Job 不能同时写同一个 NativeArray
2. 越界访问检测：尝试访问 NativeArray[length] 会报错
3. 主线程写入检测：Job 执行时主线程不能写 NativeContainer
4. 泄露检测：忘记 Dispose 会收到警告
5. 依赖检测：Job B 读取 Job A 写入的数据 → 必须传入依赖

这些检查只在 Editor 中生效（Release 版本无检查，更快更危险）
```

### 常见安全错误

```csharp
// ❌ 错误 1：两个 Job 同时写同一个数据
public struct JobA : IJob
{
    public NativeArray<int> data;
    public void Execute() { data[0] = 1; }
}

public struct JobB : IJob
{
    public NativeArray<int> data;  // 和 JobA 用的是同一个 data！
    public void Execute() { data[0] = 2; }
}

// 调度
var a = new JobA { data = sharedData }.Schedule();
var b = new JobB { data = sharedData }.Schedule();
// ↑ 运行时可能报错：WriteAccessConflict

// ✅ 正确：加依赖
var b = new JobB { data = sharedData }.Schedule(a);
b.Complete();

// ❌ 错误 2：主线程写 Job 正在读的数据
job.Schedule();     // Job 在后台执行
sharedData[0] = 5;  // 主线程也在写！（Editor 会报错）

// ✅ 正确：等 Job 完成再写
jobHandle.Complete();
sharedData[0] = 5;

// ❌ 错误 3：忘记 Dispose
NativeArray<int> arr = new(10, Allocator.TempJob);
var job = new MyJob { data = arr }.Schedule();
// ... 忘记 Complete 和 Dispose
// Editor 在下一帧会报警：NativeArray has not been disposed!
```

### ParallelFor 的原子操作

```csharp
// IJobParallelFor 不能直接用 lock（Burst 不支持 lock）
// 需要原子操作

[BurstCompile]
public struct ParallelSumJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<int> results;

    public NativeArray<int> input;
    public NativeReference<int> sum;  // 线程安全的引用

    public void Execute(int index)
    {
        // 累计求和（线程安全）
        int val = input[index];
        // AtomicAdd 是 Burst 支持的原语
        System.Threading.Interlocked.Add(
            ref UnsafeUtility.AsRef<int>(sum.GetUnsafePtr()), val);
    }
}

// 更高效的方案：每个线程局部累计，最后合并
[BurstCompile]
public struct ParallelSumWithLocal : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<int> results;

    public NativeArray<int> input;
    public NativeArray<int> threadSums;  // 每个线程一个槽

    public void Execute(int index)
    {
        // 获取当前线程 ID（Job System 提供）
        int threadIndex = UnityEngine.Jobs.JobSystem.ThreadIndex;
        threadSums[threadIndex] += input[index];
    }
}
```

---

## 3. NativeContainer 生命周期管理

### 三种 Allocator

```csharp
// 1. Allocator.Temp
// 生命周期：单帧（自动释放）
// 速度：最快（分配在临时缓冲区）
// 限制：不能用在 Job 中！只能主线程用

{
    NativeArray<int> temp = new(100, Allocator.Temp);
    // 使用...
    // 不需要手动 Dispose，下一帧自动释放
}

// 2. Allocator.TempJob
// 生命周期：4 帧（代码必须手动 Dispose）
// 速度：快
// 限制：可以传给 Job，但必须手动释放

NativeArray<int> tempJob = new(100, Allocator.TempJob);
try
{
    var job = new SomeJob { data = tempJob };
    job.Schedule().Complete();
}
finally
{
    tempJob.Dispose();  // 必须释放！
}
// 如果在 4 帧内没释放，Editor 报警

// 3. Allocator.Persistent
// 生命周期：手动控制（直到 Dispose）
// 速度：最慢（从系统堆分配）
// 适合：长期存在的缓存

NativeArray<int> persistent = new(100, Allocator.Persistent);
// 在整个场景生命周期中使用
// 场景切换时 Dispose

// ⚠️ Persistent 的分配/释放开销最大
// 不要在每帧创建 Persistent 的 NativeArray
```

### NativeContainer 类型

```csharp
// Unity.Collections 提供的线程安全容器

// 1. NativeArray<T> — 最基础
NativeArray<float3> points = new(1000, Allocator.TempJob);

// 2. NativeSlice<T> — 数组的"视图"
NativeSlice<float3> slice = points.Slice(100, 200);  // 对 [100,300) 做操作

// 3. NativeList<T> — 动态数组（可扩容）
NativeList<float3> dynamicList = new(Allocator.Persistent);
dynamicList.Add(new float3(1, 2, 3));
// 扩容时会触发重新分配

// 4. NativeQueue<T> — 线程安全队列
NativeQueue<int> queue = new(Allocator.TempJob);
queue.Enqueue(42);
bool ok = queue.TryDequeue(out int val);

// 5. NativeHashMap<K, V> — 哈希表
NativeHashMap<int, float3> map = new(100, Allocator.TempJob);
map.TryAdd(1, new float3(1, 2, 3));
map.TryGetValue(1, out float3 pos);

// 6. NativeHashSet<T> — 哈希集合
NativeHashSet<int> set = new(100, Allocator.TempJob);

// 7. NativeMultiHashMap<K, V> — 一个 Key 对应多个 Value
NativeMultiHashMap<int, int> multiMap = new(100, Allocator.TempJob);
multiMap.Add(1, 10);
multiMap.Add(1, 20);  // 同一个 key 存两个值

// 8. NativeReference<T> — 单个值的引用
NativeReference<int> counter = new(Allocator.TempJob);
counter.Value = 0;

// 9. NativeStream — 用于多线程写入后合并
NativeStream stream = new(4, Allocator.TempJob);  // 4 个线程
// 每个线程写入自己的分段，最后合并
```

### 自定义 NativeContainer

```csharp
// 自定义容器需要实现 IDisposable
// 并正确处理 Job 依赖

public struct NativeGrid<T> : IDisposable where T : unmanaged
{
    private NativeArray<T> data;
    private int width;
    private int height;

    public NativeGrid(int width, int height, Allocator allocator)
    {
        this.width = width;
        this.height = height;
        data = new NativeArray<T>(width * height, allocator);
    }

    public T this[int x, int y]
    {
        get => data[y * width + x];
        set => data[y * width + x] = value;
    }

    public void Dispose() => data.Dispose();
}
```

### NativeDisable 安全属性

```csharp
// 当 Job 需要同时读写一个 NativeArray 的多个元素时（非连续访问）
// 默认安全检查会阻止，需要显示声明

[BurstCompile]
public struct CustomAccessJob : IJobParallelFor
{
    // 禁用安全检查（允许随机访问同个数组的不同索引）
    [NativeDisableParallelForRestriction]
    public NativeArray<int> data;

    // 禁用容器安全检查（更快但更危险）
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<int> unsafeData;

    // 禁用对父容器的安全检查
    // 当 Job 持有的 NativeContainer 在父对象中也有引用时使用
    [NativeDisableUnsafePtrRestriction]
    public NativeArray<int> ptrData;

    public void Execute(int index)
    {
        // 随机访问（即使不是当前线程的分配范围）
        int otherIndex = data[index] % data.Length;
        data[otherIndex] += data[index];
        // ↑ 没有 [NativeDisableParallelForRestriction] 会报错
    }
}
```

---

## 4. IJobParallelForTransform

```csharp
// IJobParallelForTransform 是专门处理 Transform 的 Job
// 可以并行操作 Transform 组件

using UnityEngine.Jobs;

[BurstCompile]
public struct RotateTransformJob : IJobParallelForTransform
{
    public float deltaTime;
    public float speed;

    public void Execute(int index, TransformAccess transform)
    {
        // transform 是 TransformAccess（不是 Transform！）
        // 可以安全地在 Job 中操作
        Vector3 pos = transform.position;
        pos.y += math.sin(Time.time + index) * deltaTime * speed;
        transform.position = pos;
    }
}

// 使用
public class TransformJobExample : MonoBehaviour
{
    public GameObject[] objects;
    private TransformAccessArray transformArray;

    void Start()
    {
        // 收集所有 Transform
        Transform[] transforms = new Transform[objects.Length];
        for (int i = 0; i < objects.Length; i++)
            transforms[i] = objects[i].transform;

        transformArray = new TransformAccessArray(transforms);
    }

    void Update()
    {
        var job = new RotateTransformJob
        {
            deltaTime = Time.deltaTime,
            speed = 2f
        };

        job.Schedule(transformArray).Complete();
    }

    void OnDestroy()
    {
        transformArray.Dispose();
    }
}

// TransformAccessArray 本身也是一个 NativeContainer
// 需要 Dispose，可以传递给多个 Job 但每次只能有一个写入
```

---

## 5. Job 调度器内部机制

### 调度流程

```
调用 Schedule() 时发生了什么：

1. Job 被放入调度队列
2. 工作线程从队列中取出 Job
3. 检查依赖是否满足
4. 执行 Job.Execute()
5. 标记完成

工作线程管理：
- 主线程：运行 Update()、渲染等
- 工作线程数 = CPU 核心数 - 1（预留一个给主线程）
- 4 核 CPU → 3 个工作线程
- 工作线程不会抢占主线程

Job 批处理（Batch）：
IJobParallelFor 把数组分成多个 Batch
每个 Batch 是一个工作单元

例如：
1000 个元素，Batch=100
→ 10 个 Batch → 3 个线程各处理 3~4 个 Batch
```

### 调度优化

```csharp
// 1. 选择合适的 Batch Size
// Batch 大小影响调度效率：

// 小 Batch（每个元素计算很轻量时）：
// Batch = 1  → 调度开销 > 计算开销（不划算）
// Batch = 64 → 调度开销分摊，利用率高

// 大 Batch（每个元素计算很重时）：
// Batch = 16 → 负载均衡好（不会一个线程很久，其他闲着）

// 经验法则：
// 每个元素执行时间 × Batch Size ≈ 0.1ms
// 如果每个元素都很轻 → Batch = 64~256
// 如果每个元素都很重 → Batch = 8~32

// 2. 避免 Job 碎片（很多小 Job）
// ❌ 不推荐：每帧调度 100 个小 Job
for (int i = 0; i < 100; i++)
{
    var job = new TinyJob { ... }.Schedule();
}
// 调度 100 次有 100 次上下文切换开销

// ✅ 推荐：合并成大 Job
var bigJob = new BigJob { ... }.Schedule(elements.Length, 64);

// 3. 尽早 Schedule，晚 Complete
// ❌ 错误：立即 Complete
var handle = job.Schedule();
handle.Complete();  // = 在主线程执行，没有任何并行效果

// ✅ 正确：先调度，做点别的事，最后 Complete
JobHandle handle = job.Schedule();
// 做一些不依赖 Job 结果的主线程工作...
UpdateUI();
ProcessInput();
// 然后等 Job
handle.Complete();
```

### JobHandle 组合

```csharp
// 多个 Job 的依赖管理

// 1. 并行执行（不互相依赖）
JobHandle a = jobA.Schedule();
JobHandle b = jobB.Schedule();
JobHandle.ParallelForComplete(a, b);  // 等所有完成

// 2. 串联执行（A → B → C）
JobHandle aHandle = jobA.Schedule();
JobHandle bHandle = jobB.Schedule(aHandle);
JobHandle cHandle = jobC.Schedule(bHandle);
cHandle.Complete();  // 等 C，A 和 B 已经完成

// 3. 合并依赖（A 和 B 都完成后才能 C）
JobHandle ab = JobHandle.CombineDependencies(aHandle, bHandle);
JobHandle cHandle = jobC.Schedule(ab);
cHandle.Complete();
```

---

## 6. 实战：粒子系统优化

```csharp
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

// 完整的 Burst + Job 粒子系统
public class BurstParticleSystem : MonoBehaviour
{
    private struct Particle
    {
        public float3 position;
        public float3 velocity;
        public float life;
        public float maxLife;
        public float4 color;
    }

    private NativeArray<Particle> particles;
    private JobHandle particleHandle;
    private const int PARTICLE_COUNT = 10000;

    void Start()
    {
        particles = new NativeArray<Particle>(PARTICLE_COUNT, Allocator.Persistent);

        // 初始化粒子
        for (int i = 0; i < PARTICLE_COUNT; i++)
        {
            particles[i] = new Particle
            {
                position = UnityEngine.Random.insideUnitSphere * 5f,
                velocity = UnityEngine.Random.insideUnitSphere * 2f,
                life = UnityEngine.Random.Range(1f, 3f),
                maxLife = 3f,
                color = new float4(1, 1, 1, 1)
            };
        }
    }

    void Update()
    {
        // 等上一帧的 Job 完成
        particleHandle.Complete();

        var updateJob = new ParticleUpdateJob
        {
            particles = particles,
            deltaTime = Time.deltaTime,
            gravity = new float3(0, -9.81f, 0)
        };

        var renderJob = new ParticleRenderJob
        {
            particles = particles,
            // ... 渲染相关参数
        };

        // 更新粒子和渲染可以并行
        JobHandle updateHandle = updateJob.Schedule(PARTICLE_COUNT, 64);
        // 渲染依赖更新结果
        particleHandle = renderJob.Schedule(updateHandle);
    }

    void OnDestroy()
    {
        particleHandle.Complete();
        particles.Dispose();
    }
}

[BurstCompile]
public struct ParticleUpdateJob : IJobParallelFor
{
    public NativeArray<BurstParticleSystem.Particle> particles;
    public float deltaTime;
    public float3 gravity;

    public void Execute(int index)
    {
        var p = particles[index];

        p.life -= deltaTime;
        if (p.life <= 0)
        {
            // 重置粒子
            p.position = float3.zero;
            p.velocity = UnityEngine.Random.insideUnitSphere * 2f;
            p.life = p.maxLife;
            p.color = new float4(1, 1, 1, 1);
        }

        p.velocity += gravity * deltaTime;
        p.position += p.velocity * deltaTime;

        // 生命值影响颜色和大小
        float t = p.life / p.maxLife;
        p.color.a = math.saturate(t);

        particles[index] = p;
    }
}
```

---

## 7. C++/Raylib 对比

| 概念 | C++/Raylib | Unity/C# |
|------|-----------|----------|
| 线程 | std::thread | System.Threading.Thread |
| 线程池 | std::async / 手写 | ThreadPool / Task |
| 原子操作 | std::atomic | System.Threading.Interlocked |
| 并发队列 | moodycamel::ConcurrentQueue | NativeQueue / ConcurrentQueue |
| SIMD | SSE/AVX 手写 intrinsic | Burst 自动向量化 |
| 内存分配 | 自定义分配器 | Allocator.Temp/TempJob/Persistent |

**Burst 编译器对标的是 C++ 编译器优化**：IL2CPP 把 C# 转 C++ 再编译，但 Burst 直接从 C# 转 LLVM IR 生成优化后的机器码，比普通 IL2CPP 路径更快。可以理解为 Unity 的"JIT 编译器"但面向 Job 代码。

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Burst | LLVM 编译 C# 到优化机器码，性能接近 C++ |
| Unity.Mathematics | Burst 专用的数学库（float3、float4x4） |
| SIMD 自动向量化 | Burst 自动把循环转为 SSE/AVX 指令 |
| Allocator.Temp | 最快，单帧有效，不能给 Job |
| Allocator.TempJob | 快，4 帧内必须 Dispose |
| Allocator.Persistent | 慢，长期存在 |
| NativeHashMap | 线程安全的哈希表 |
| TransformAccessArray | Transform 的并行操作容器 |
| JobHandle 组合 | CombineDependencies / Schedule(dependsOn) |
| Batch Size | 调度粒度，选择基于计算密度 |
| 安全检查 | Editor 自动检测写入冲突/泄露 |

**对比 C++：** Unity Job System 本质是一个工作窃取调度器（work-stealing scheduler），类似 Intel TBB（Threading Building Blocks）的概念。Burst 编译是 Unity 独有的优势——C++ 中你需要手写 SSE/AVX intrinsic 或者依赖编译器的自动向量化（GCC -O3 -mavx2），而 Burst 在编译 C# Job 时会自动生成 SIMD 指令。
