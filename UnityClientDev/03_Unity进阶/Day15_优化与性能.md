# Day 15：优化与性能 — 从 Profiler 到 IL2CPP

## 0. 为什么需要性能优化？

游戏的性能瓶颈通常出现在三个方面：

```
CPU 瓶颈（我们的工作）：
- 过多的 Draw Call → 合批
- 频繁的 GC → 对象池、缓存
- 复杂的脚本逻辑 → 算法优化

GPU 瓶颈（艺术家/TA 的工作）：
- 复杂的 Shader → 简化计算
- 过多的顶点 → LOD
- 像素 Overdraw → 减少半透明重叠

内存瓶颈：
- 纹理太大 → 压缩
- Assets 加载过多 → Addressables 分包
- 内存泄露 → 引用管理
```

**第一条优化原则：不要猜，直接测！** Profiler 是你的眼睛。

---

## 1. Profiler——性能分析器

### Profiler 的工作原理

```csharp
// Profiler 有两种工作模式：
// 1. Instrumentation（插桩）
//    在代码的关键位置插入计时标记
//    精度高，但有运行时开销
//    可以看到每个函数的调用次数和时间

// 2. Sampling（采样）
//    每隔一定时间（如 1ms）暂停程序，记录当前执行位置
//    精度不如插桩，但开销小
//    适合看整体性能分布
```

### Profiler 的各个面板

```
CPU Usage（CPU 使用率）：
- PlayerLoop：所有 Unity 引擎系统
- Scripts：你的 MonoBehaviour 脚本
- Physics：PhysX 物理引擎
- Rendering：渲染管线
- GC：垃圾回收时间

Rendering（渲染）：
- Draw Calls：每帧的 Draw Call 数量
- Batches：批处理次数（合批后的 Draw Call）
- Triangles：三角形数量
- Vertices：顶点数量

Memory（内存）：
- Total Allocated：总分配内存
- Texture Memory：纹理占用的 GPU 内存
- Mesh Memory：网格数据内存
- GC Heap：托管堆大小

GPU Usage（GPU 使用率）：
- Render Thread：渲染线程时间
- Present Frame：帧缓冲显示时间
```

---

## 2. GC 优化——Unity 的最大性能杀手

### GC 的触发和影响

```csharp
// GC 发生的时机：
// 1. 第 0 代托管堆满（最常见）
// 2. 大对象分配（>85KB）
// 3. 显式调用 GC.Collect()

// GC 的影响：
// - GC 执行时所有线程暂停（Stop The World）
// - 一次完整的 GC 可能耗时 50~200ms
// - 在游戏中表现为掉帧卡顿！

// GC 暂停时间分布：
// 第 0 代 GC：~1ms（小暂停）
// 第 1 代 GC：~5ms
// 第 2 代 GC：~50ms+（大暂停）
// 全量 GC：~100ms+（灾难性卡顿）
```

### GC 产生的常见原因

```csharp
public class GCSources : MonoBehaviour
{
    void Update()
    {
        // ─── 1. 字符串操作 ───
        // ❌ 每次产生新字符串 + 整数 ToString 装箱
        Debug.Log("Score: " + score.ToString());

        // ✅ 字符串插值（Unity 2020+ 优化了字符串内插的内存）
        Debug.Log($"Score: {score}");

        // ✅ 缓存字符串
        cachedText.text = $"Score: {score}";

        // ─── 2. foreach 隐式分配枚举器 ───
        // ❌ List<T> 的 foreach 会产生枚举器对象
        foreach (var item in GetComponents<Collider>()) { }

        // ✅ for 循环
        Collider[] cols = GetComponents<Collider>();
        for (int i = 0; i < cols.Length; i++) { }

        // ─── 3. 临时数组 ───
        // ❌ 每次 Update 分配新数组
        Vector3[] positions = new Vector3[] { a, b, c };

        // ✅ 缓存数组
        if (cachedPos == null) cachedPos = new Vector3[3];
        cachedPos[0] = a; cachedPos[1] = b; cachedPos[2] = c;

        // ─── 4. 闭包分配 ───
        // ❌ Lambda 闭包捕获变量 → 每次创建闭包类
        DoSomething(() => Debug.Log("Done"));

        // ✅ 缓存委托
        if (onDone == null) onDone = () => Debug.Log("Done");
        DoSomething(onDone);
    }
}
```

### GC 优化策略总结

```
1. 避免 Update 中的内存分配
2. 对象池（替代频繁 Instantiate/Destroy）
3. 缓存组件引用（替代每帧 GetComponent）
4. 用 StringBuilder 替代字符串拼接
5. 用 for 替代 foreach（对 List<T> 和 Dictionary 等）
6. 缓存数组、列表等容器（不要每帧 new）
7. 用 struct 替代 class（减少堆分配）
8. 延迟 GC：用 GC.Collect() 在场景加载时主动触发
```

---

## 3. Draw Call 优化

### 什么是 Draw Call？

```csharp
// Draw Call = CPU 告诉 GPU "画这个东西"的命令
// 每次 Draw Call 需要：
// 1. CPU 设置渲染状态（Shader、纹理、混合模式等）
// 2. CPU 提交顶点数据到 GPU
// 3. GPU 上下文切换

// 性能目标：
// PC：< 500 Draw Calls
// 移动端：< 100~200 Draw Calls
```

### Draw Call 的成因

```csharp
// 为什么 Draw Call 会增加？
// 每次切换材质/纹理时，需要新的 Draw Call！

// 100 个物体，使用同一种材质
// → 如果能合批：1 次 Draw Call
// → 如果不能合批：100 次 Draw Call

// 导致无法合批的原因：
// - 材质不同（多个 Material 实例）
// - 纹理不同（无法合并）
// - 渲染队列不同（不透明 vs 半透明）
// - Mesh 不同（静态合批要求相同 Mesh？不，静态合批不要求相同 Mesh）
```

### 减少 Draw Call 的方法

```csharp
// 1. 图集（Sprite Atlas）
// 将多个小图合成一张大纹理
// 同一次 Draw Call 可以渲染多个使用不同区域的对象

// 2. 静态合批（Static Batching）
// 标记对象为 Static → Unity 合并 Mesh
// 适合不动的场景物体

// 3. 动态合批（Dynamic Batching）
// 自动合批小物体（< 300 顶点）
// 限制较多，不总是生效

// 4. GPU Instancing
// 相同 Mesh + 相同 Material 大量对象时的最佳方案
// 在 Update 中每帧更新 transform 的物体也可以 Instancing

// 5. SRP Batcher（URP/HDRP）
// 使用 SRP 的场景自动减少设置渲染状态的开销
// URPs 的默认合批方式，不需要额外配置
```

### LOD（Level of Detail）

```csharp
// 距离摄像机越远，使用越简单的模型

// LOD Group 组件：
// LOD 0：完整模型（近处）
// LOD 1：简化模型（中等距离）
// LOD 2：极简模型（远处）
// Culled：完全隐藏（极远处）

// 在 URP 中，LOD 也会影响 Shadow 的质量
// 远处物体可以使用低质量 Shadow
```

### 遮挡剔除（Occlusion Culling）

```csharp
// 基本原理：被其他物体挡住的东西不渲染

// 流程：
// 1. 预处理：将场景分割为多个单元格（Occlusion Data）
// 2. 运行时：对每个单元格判断是否被前面的物体挡住
// 3. 被挡住的单元格内的对象跳过渲染

// 配置：
// Window → Rendering → Occlusion Culling
// 1. 标记所有需要参与的物体为 Occlusion Static
// 2. Bake（烘焙遮挡数据）
// 3. 在 Scene 视图的 Occlusion Culling 模式下预览

// 注意：必须关卡已完成（不会再移动物体）才能烘焙
```

---

## 4. IL2CPP——AOT 编译优化

### Mono vs IL2CPP

```csharp
// Unity 有两种 C# 运行时：

// Mono：
// - C# 代码编译为 IL（中间语言）
// - 运行时 JIT（Just-In-Time）编译为机器码
// - 支持反射、动态代码生成
// - 性能一般，启动慢

// IL2CPP：
// - IL（中间语言）→ C++ 代码
// - C++ 编译器编译为原生机器码
// - 不支持 JIT（iOS 要求）
// - 性能更好（C++ 编译器的优化）
// - 启动更快
// - 包体更大
```

### IL2CPP 的优化效果

```
        Mono        IL2CPP
启动：   慢（JIT）   快（AOT）
CPU：    一般        好（编译器优化）
包体：   小          大（包含所有代码）
反射：   完整        受限
泛型：   JIT 生成    提前展开
平台：   所有        所有（iOS 必须）
```

---

## 5. 性能预算

### 帧率目标

```csharp
// 不同平台的帧率目标：
// PC/主机：60 FPS（每帧 16.6ms）
// 移动端：30 FPS（每帧 33.3ms）
// VR：90~120 FPS（每帧 8~11ms）
```

### 每帧时间分配示例

```
60 FPS 的预算（16.6ms）：
├── 脚本 Update:     ~4ms
├── 物理:            ~2ms
├── 渲染:            ~8ms
├── 其他（UI等）:    ~2ms
└── 缓冲:            ~0.6ms

如果超过 16.6ms → 掉帧！
```

---

## 6. Unity 脚本性能优化专项

### Update 的优化

```csharp
// 很多脚本不需要每帧 Update！
// 每个空的 Update() 方法即使不做任何事，也有调用开销

public class Enemy : MonoBehaviour
{
    // ❌ 坏：每秒 60 次空调用
    void Update()
    {
        // 这个敌人 90% 的时间在 idle，不需要每帧检查
    }
}

// ✅ 好：用协程替代定时检查
public class EnemyOptimized : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(CheckPlayerDistance());
    }

    IEnumerator CheckPlayerDistance()
    {
        while (true)
        {
            // 每 0.5 秒检查一次，不是每帧
            yield return new WaitForSeconds(0.5f);

            float dist = Vector3.Distance(
                transform.position, player.position);

            if (dist < alertRange)
            {
                // 进入战斗状态后再开始每帧 Update
                enabled = true;
            }
        }
    }
}

// ✅ 更好：完全不需要 Update 时禁用脚本
// GetComponent<Enemy>().enabled = false;
```

### 物理性能

```csharp
// 物理引擎的性能消耗：
// - Collider 越多越慢（碰撞检测 O(n²)）
// - Rigidbody 越多越慢（约束求解）

// 优化方向：
// 1. 减少 Collider 数量
//    使用 Mesh Collider（精确但昂贵）→ 替代为多个基本 Collider
//    远处的对象使用简化 Collider

// 2. 刚体睡眠
//    长时间不动的 Rigidbody 自动进入睡眠（不参与物理模拟）
//    不要在睡眠的刚体上持续施加微小力

// 3. 物理层级（Layer Collision Matrix）
//    不同 Layer 的物体之间不检测碰撞
//    Edit → Project Settings → Physics → Layer Collision Matrix
//    项目符号与玩家不碰撞、玩家与敌人不碰撞等
```

### 字符串操作的完整优化

```csharp
// Unity 中字符串操作的 GC 压力
// 场景：显示 FPS 计数器

// ❌ 坏：每帧分配
void OnGUI()
{
    GUI.Label(new Rect(10, 10, 100, 20), "FPS: " + fps.ToString());
    // 每帧：字符串拼接 → 新 string 对象
    //      ：fps.ToString() → 新 string 对象（值类型 ToString 不装箱）
    //      ：总计每帧 2 个 string + 其他
}

// ✅ 好：缓存 StringBuilder
private StringBuilder sb = new StringBuilder(32);

void OnGUI()
{
    sb.Clear();
    sb.Append("FPS: ");
    sb.Append(fps);
    GUI.Label(new Rect(10, 10, 100, 20), sb.ToString());
}

// ✅ 最好：只在变化时更新
private int lastFPS;
private string cachedFPS;

void Update()
{
    int currentFPS = (int)(1f / Time.deltaTime);
    if (currentFPS != lastFPS)
    {
        lastFPS = currentFPS;
        cachedFPS = $"FPS: {currentFPS}";
    }
}
// OnGUI 中使用 cachedFPS——零分配
```

### 协程的内存优化

```csharp
// 协程的 GC 优化

// ❌ 坏：每次 yield new 都分配
IEnumerator BadCoroutine()
{
    yield return new WaitForSeconds(1f);
    yield return new WaitForSeconds(1f);
    yield return new WaitForSeconds(1f);
}

// ✅ 好：缓存 WaitForSeconds 实例
private WaitForSeconds wait1 = new WaitForSeconds(1f);

IEnumerator GoodCoroutine()
{
    yield return wait1;
    yield return wait1;
    yield return wait1;
    // 零 GC 分配！
}
```

---

## 7. 真机调试

### 连接真机 Profiler

```
Android USB 连接流程：
1. 手机开启开发者模式 + USB 调试
2. 连接 USB 线
3. Unity Editor → Window → Analysis → Profiler
4. 点击 Active Profiler → 选择 AndroidPlayer
5. 手机上运行游戏，Editor 中查看性能数据

iOS 连接流程：
1. 通过 USB 或 WiFi 连接设备
2. XCode 中 Run 到设备
3. 通过 Profile 连线远程连接

Remote 模式（Editor 模拟）：
- 在 Editor 中用 Game 视图运行
- 性能数据是 Editor 环境的数据
- 不等于真机性能（可能差 5~10 倍）
- 但适合快速排查逻辑问题
```

### Memory Profiler

```csharp
// Unity Memory Profiler（Window → Analysis → Memory Profiler）
// 高级内存分析工具，可以：
// 1. 拍摄内存快照（Snapshot）
// 2. 对比两个快照的差异
// 3. 查看每个对象的引用链（找出无法 GC 的原因）
// 4. 查看 Unity 原生对象和 C# 对象的详细内存占用

// 使用场景：
// - 怀疑内存泄漏时：拍快照 → 做操作 → 拍快照 → 对比
// - 资源过大时：查看 Texture/Mesh/Audio 各占多少
// - 重复资源：同一张纹理被加载了多次？
```

---

## 8. 内存优化

### 纹理内存

```csharp
// 纹理是游戏最大的内存占用

// 优化策略：
// 1. 压缩格式
//    Android：ASTC（推荐）/ ETC2
//    iOS：ASTC
//    PC：DXT / BC7

// 2. Mipmap
//    3D 场景中的纹理开启 Mipmap（+33% 内存）
//    UI 纹理不需要 Mipmap

// 3. 分辨率限制
//    最大纹理尺寸限制为 1024/2048
//    从 4096→2048 可以节省 75% 显存

// 4. 纹理图集（Sprite Atlas）
//    合并小纹理，减少 Draw Call 的同时节省内存
```

### 音频内存

```csharp
// 音频的加载类型（Load Type）：
// - Decompress On Load：加载时完全解压（质量好，内存大）
// - Compressed In Memory：保持压缩，播放时解压（中等内存）
// - Streaming：流式加载，不放入内存（适合大音乐）

// 1 分钟的 44.1kHz 16bit 立体声：
// 未压缩：60秒 × 44100 × 2字节 × 2声道 = 约 10MB
// MP3 压缩后：约 1MB
// Streaming：几乎不占运行时内存

// 手机游戏建议：
// - 短音效（<5秒）：Decompress On Load
// - 背景音乐：Streaming
// - 其他：Compressed In Memory
```

### 性能调试流程

```
1. 连接 Profiler（Editor 或真机）
2. 运行游戏到性能问题的场景
3. 观察 CPU Usage 面板 → 找到最耗时的系统
4. 如果是脚本：双击查看哪个函数最慢
5. 如果是渲染：查看 Draw Call 和顶点数
6. 如果是 GC：查看 GC Allocation 曲线
7. 优化热点
8. 重新 Profiler 验证
9. 重复直到达到目标
```

---

## 练习：Profiler 使用流程

```csharp
// 1. 在 Editor 中运行游戏
// 2. 打开 Window → Analysis → Profiler (Ctrl+7)
// 3. 点击 Record
// 4. 在游戏中触发性能问题的操作
// 5. 停止 Record
// 6. 观察 CPU Usage 面板的柱状图
// 7. 找到最高的柱子 → 双击展开
// 8. 找到最耗时的函数
// 9. 优化它
```

---

---

## C++/Raylib 对照总结

| 概念 | C++ / Raylib | C# / Unity |
|------|-------------|-----------|
| 性能分析 | 手动 `clock()` / 外部工具 | Profiler 内置工具 |
| GC | 手动 new/delete | 自动代龄 GC |
| 内存管理 | RAII + 智能指针 | IDisposable + GC |
| 编译方式 | 原生编译 | Mono JIT / IL2CPP AOT |
| 纹理加载 | `LoadTexture` + `UnloadTexture` | Resources / Addressables 管理 |
| 音频加载 | `LoadSound` + `UnloadSound` | AudioClip 加载类型控制 |
| Draw Call | 手动控制 | 合批自动管理 |
| 对象管理 | 手动分配/释放 | 对象池（手动实现）|

## 停靠点

> **不要猜，直接测**——Profiler 是你的眼睛。
> GC 优化三板斧：对象池、缓存、避免字符串拼接/foreach/临时分配。
> Draw Call 数量决定渲染性能。静态合批、GPU Instancing、SRP Batcher 是主要手段。
> IL2CPP 将 C# IL 转换为原生代码——iOS 必须，性能更好。
> 性能预算是硬约束——超出预算就是掉帧。

