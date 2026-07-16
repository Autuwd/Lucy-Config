# Day 23：游戏数学进阶 — SIMD、定点数、空间哈希、噪声与样条

## 0. 为什么需要这些数学工具？

基础篇的向量/矩阵/四元数是"日常工具"，而这些高级数学是"专业工具"：

```
基础数学解决的问题：
点积 → 判断前后
叉积 → 判断左右
四元数 → 平滑旋转
矩阵 → TRS 变换

高级数学解决的问题：
SIMD → 批量处理 10000 个顶点（快 4~8 倍）
定点数 → Lockstep 确定性和网络同步
空间哈希 → 从 10000 个物体中快速找邻居
噪声 → 程序化生成地形/纹理/特效
样条 → 平滑的摄像机路径/过场动画
```

每个工具在 C++/Raylib 中都有对应的库，在 Unity 中有更便捷的使用方式。

---

## 1. SIMD 数学

### 什么是 SIMD？

```
SIMD = Single Instruction, Multiple Data
一条指令同时处理多个数据

普通运算（SISD，标量）：
a1 += b1    ← 一次加法
a2 += b2    ← 又一次加法
a3 += b3    ← 又一次加法
a4 += b4    ← 又一次加法
4 次加法指令

SIMD 运算：
[ a1 a2 a3 a4 ] += [ b1 b2 b3 b4 ]  ← 一次加法指令
一次加法指令同时处理 4 个 float

CPU 的 SIMD 指令集：
SSE： 128 位寄存器 → 4 × float（或 2 × double）
AVX： 256 位寄存器 → 8 × float（或 4 × double）
AVX-512：512 位寄存器 → 16 × float（或 8 × double）
```

### System.Numerics.Vector

```csharp
// C# 的 SIMD 支持（System.Numerics）
// 不需要 Unity，纯 .NET 功能

using System.Numerics;

public class VectorSIMD
{
    // Vector2-4 在 .NET 中是 SIMD 加速的！
    public void SIMDDemo()
    {
        // 普通的 Vector3（单个计算）
        Vector3 a = new Vector3(1, 2, 3);
        Vector3 b = new Vector3(4, 5, 6);
        Vector3 c = a + b;                     // 编译器可能 SIMD 优化

        // 显式 SIMD 向量
        Vector<float> va = new Vector<float>(new float[] { 1, 2, 3, 4 });
        Vector<float> vb = new Vector<float>(new float[] { 5, 6, 7, 8 });

        // 一次 SIMD 加法 = 4 个 float 同时加
        Vector<float> vc = va + vb;

        // 一次 SIMD 乘法
        Vector<float> vd = va * vb;

        // 点积（SIMD 加速）
        float dot = Vector.Dot(va, vb);

        // 测试 Vector<float> 的宽度
        // 如果 CPU 支持 AVX：宽度 = 8（256 位）
        // 如果只支持 SSE：宽度 = 4（128 位）
        int simdWidth = Vector<float>.Count;
        Debug.Log($"当前 CPU 的 SIMD 宽度：{simdWidth}");
    }
}
```

### 批量矩阵乘法性能对比

```csharp
public class SIMDPerformanceTest : MonoBehaviour
{
    private const int COUNT = 100000;
    private float3[] positions;
    private float4x4 transformMatrix;

    void Start()
    {
        positions = new float3[COUNT];
        for (int i = 0; i < COUNT; i++)
            positions[i] = new float3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f)
            );

        transformMatrix = float4x4.TRS(
            new float3(1, 2, 3),
            quaternion.EulerXYZ(0, 45, 0),
            new float3(1, 1, 1)
        );
    }

    void Update()
    {
        // 测试 1：普通 C# 循环
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < COUNT; i++)
        {
            positions[i] = math.transform(transformMatrix, positions[i]);
        }
        sw.Stop();
        Debug.Log($"普通循环：{sw.ElapsedMilliseconds}ms");

        // 测试 2：如果使用 Job + Burst（SIMD 自动向量化）
        // 效果类似以下伪代码：
        // Burst 编译器会把 math.transform 自动转为 SIMD 指令
        // 一次处理 4/8 个 float → 性能提升 2~4 倍
    }
}
```

### Burst 中的 SIMD

```
Burst 编译器的自动向量化：

普通 C# 代码：
for (int i = 0; i < N; i++)
    result[i] = a[i] + b[i] * c[i];

Burst 编译为：
vload ymm0, [a+i]      ← 一次加载 8 个 float
vload ymm1, [b+i]
vmul  ymm2, ymm1, c    ← 一次 8 个乘法
vadd  ymm3, ymm0, ymm2 ← 一次 8 个加法
vstore [result+i], ymm3

一次循环迭代处理 8 个元素
循环次数减少到 1/8
```

---

## 2. Unity.Mathematics 深度

### 类型体系

```csharp
// Unity.Mathematics 的所有类型都是 struct（值类型，零 GC）

// 标量
bool2, bool3, bool4      // 布尔向量（用于条件选择）
int2, int3, int4         // 整数向量
uint2, uint3, uint4
float2, float3, float4   // 浮点向量
double2, double3, double4

// 矩阵
float2x2, float3x3, float4x4
float3x4  // 3 行 4 列（模拟行主矩阵）
// 四元数（但不是传统四元数）
quaternion  // Unity.Mathematics 的四元数

// 随机数
Random rng = Random.CreateFromIndex(42);
float rand = rng.NextFloat(0f, 1f);

// 数学常量
math.PI      // π
math.E       // 自然常数
math.INFINITY // 无穷大

// 常用函数
math.lerp(a, b, t);       // 线性插值
math.smoothstep(a, b, x); // 平滑过渡
math.clamp(x, lo, hi);    // 截断
math.select(a, b, cond);  // 条件选择（SIMD 友好）
```

### 在 Burst Job 中使用

```csharp
[BurstCompile]
public struct MathInBurst : IJobParallelFor
{
    public NativeArray<float3> positions;
    public NativeArray<float3> velocities;
    public float deltaTime;

    public void Execute(int i)
    {
        float3 pos = positions[i];
        float3 vel = velocities[i];

        // 使用 math 运算
        pos += vel * deltaTime;

        // 边界约束
        pos = math.clamp(pos, new float3(-10, -10, -10), new float3(10, 10, 10));

        // 弹性碰撞
        bool3 outOfBounds = pos > new float3(9, 9, 9) | pos < new float3(-9, -9, -9);
        // select: outOfBounds 为 true 的位置取反速度
        vel = math.select(vel, -vel, outOfBounds);

        // 归一化（Burst 优化）
        float3 dir = math.normalize(pos);

        positions[i] = pos;
        velocities[i] = vel;
    }
}
```

---

## 3. 定点数数学

### 为什么需要定点数？

```
浮点数的不确定性：
在不同 CPU/GPU 上，同样的浮点运算可能得到不同结果
float a = 0.1f;  // 实际存储 ≈ 0.10000000149011612
float b = 0.2f;  // 实际存储 ≈ 0.20000000298023224
float c = a + b; // 可能 ≈ 0.30000001192092896

问题：
A 客户端：0.1 + 0.2 = 0.30000001
B 客户端：0.1 + 0.2 = 0.30000002
1000 帧后的位置差异：A(300.0) vs B(300.1)
→ 看起来两个客户端不一样了！

这就是 Lockstep 要求定点数的原因。
```

### 定点数实现

```csharp
// 32.32 定点数（32 位整数部分 + 32 位小数部分）
// 用 long（64 位）存储

[System.Serializable]
public struct FixedPoint : IEquatable<FixedPoint>
{
    // 一个 FixedPoint 就是一个 long（64 位整数）
    // 低 32 位 = 小数部分
    // 高 32 位 = 整数部分
    private const long FRACTION_BITS = 32;
    private const long FRACTION_MASK = 0xFFFFFFFF;  // 低 32 位掩码
    private const long ONE = 1L << 32;  // 1.0 = 2^32

    public long rawValue;

    private FixedPoint(long value) { rawValue = value; }

    // float → FixedPoint
    public static FixedPoint FromFloat(float f)
        => new FixedPoint((long)(f * ONE));

    // FixedPoint → float
    public float ToFloat()
        => (float)rawValue / ONE;

    // int → FixedPoint
    public static FixedPoint FromInt(int i)
        => new FixedPoint((long)i << 32);

    // 常量
    public static FixedPoint Zero => new FixedPoint(0);
    public static FixedPoint One => new FixedPoint(ONE);
    public static FixedPoint Half => new FixedPoint(ONE / 2);
    public static FixedPoint Pi => FromFloat(MathF.PI);

    // 加减法（普通 long 加减）
    public static FixedPoint operator +(FixedPoint a, FixedPoint b)
        => new FixedPoint(a.rawValue + b.rawValue);

    public static FixedPoint operator -(FixedPoint a, FixedPoint b)
        => new FixedPoint(a.rawValue - b.rawValue);

    // 乘法（需要位移避免溢出）
    public static FixedPoint operator *(FixedPoint a, FixedPoint b)
    {
        // a × b = (a.rawValue / 2^32) × (b.rawValue / 2^32)
        //       = (a.rawValue × b.rawValue) / 2^64
        // 但我们只要一个 2^32 的分母 → 右移 32 位
        return new FixedPoint((a.rawValue * b.rawValue) >> 32);
    }

    // 除法
    public static FixedPoint operator /(FixedPoint a, FixedPoint b)
    {
        // a / b = (a.rawValue / 2^32) / (b.rawValue / 2^32)
        //       = a.rawValue / b.rawValue
        // 但精度不够 → 先左移再除
        return new FixedPoint((a.rawValue << 32) / b.rawValue);
    }

    // 比较
    public static bool operator >(FixedPoint a, FixedPoint b)
        => a.rawValue > b.rawValue;
    public static bool operator <(FixedPoint a, FixedPoint b)
        => a.rawValue < b.rawValue;
    public static bool operator ==(FixedPoint a, FixedPoint b)
        => a.rawValue == b.rawValue;
    public static bool operator !=(FixedPoint a, FixedPoint b)
        => a.rawValue != b.rawValue;

    // 数学函数
    public static FixedPoint Abs(FixedPoint a)
        => new FixedPoint(Math.Abs(a.rawValue));

    public static FixedPoint Sqrt(FixedPoint a)
    {
        // 牛顿迭代法求平方根
        if (a < Zero) return Zero;
        FixedPoint x = a;
        for (int i = 0; i < 10; i++)
            x = (x + a / x) * Half;
        return x;
    }

    // 整数部分
    public long IntegerPart => rawValue >> 32;
    // 小数部分
    public long FractionPart => rawValue & FRACTION_MASK;
}

// 定点数向量
public struct FixedVector3
{
    public FixedPoint x, y, z;

    public FixedVector3(FixedPoint x, FixedPoint y, FixedPoint z)
    { this.x = x; this.y = y; this.z = z; }

    public static FixedVector3 operator +(FixedVector3 a, FixedVector3 b)
        => new FixedVector3(a.x + b.x, a.y + b.y, a.z + b.z);

    public static FixedVector3 operator *(FixedVector3 v, FixedPoint s)
        => new FixedVector3(v.x * s, v.y * s, v.z * s);

    public FixedPoint SqrMagnitude
        => x * x + y * y + z * z;

    public FixedPoint Magnitude
        => FixedPoint.Sqrt(SqrMagnitude);
}
```

### 常用定点数库

```
Unity 没有内置定点数，推荐使用第三方库：
- libfixmath（C++ 移植到 C#）
- FixedMath.Net（NuGet）
- 或者自己实现（足够游戏用）
```

---

## 4. 空间哈希

### 为什么需要空间哈希？

```
从 10000 个物体中找出距离玩家最近的：

暴力法：循环 10000 次，计算距离
O(n) 时间，每次都要检查全部

空间哈希：
把空间划分成网格
玩家只在 9 个格子中查找（自己所在的 + 周围 8 个）
平均只需要检查 100 个对象 → 快 100 倍
```

```csharp
public class SpatialHash3D<T> where T : class
{
    private Dictionary<int, List<T>> buckets = new();
    private float cellSize;
    private Func<T, Vector3> getPosition;

    public SpatialHash3D(float cellSize, Func<T, Vector3> getPos)
    {
        this.cellSize = cellSize;
        this.getPosition = getPos;
    }

    // 空间坐标 → 哈希键
    private int HashKey(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / cellSize);
        int y = Mathf.FloorToInt(pos.y / cellSize);
        int z = Mathf.FloorToInt(pos.z / cellSize);

        // 用 10 位每组混合（支持 ±512 范围的格子）
        return (x & 0x3FF) | ((y & 0x3FF) << 10) | ((z & 0x3FF) << 20);
    }

    // 插入对象
    public void Insert(T obj)
    {
        Vector3 pos = getPosition(obj);
        int key = HashKey(pos);

        if (!buckets.TryGetValue(key, out var list))
        {
            list = new List<T>();
            buckets[key] = list;
        }
        list.Add(obj);
    }

    // 更新位置（先删除再插入）
    public void Update(T obj, Vector3 oldPos)
    {
        int oldKey = HashKey(oldPos);
        if (buckets.TryGetValue(oldKey, out var list))
            list.Remove(obj);

        Insert(obj);
    }

    // 查询某个半径范围内的对象
    public List<T> QueryRange(Vector3 center, float radius)
    {
        HashSet<T> result = new();

        // 计算覆盖哪些格子
        int minX = Mathf.FloorToInt((center.x - radius) / cellSize);
        int maxX = Mathf.FloorToInt((center.x + radius) / cellSize);
        int minY = Mathf.FloorToInt((center.y - radius) / cellSize);
        int maxY = Mathf.FloorToInt((center.y + radius) / cellSize);
        int minZ = Mathf.FloorToInt((center.z - radius) / cellSize);
        int maxZ = Mathf.FloorToInt((center.z + radius) / cellSize);

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
        {
            int key = (x & 0x3FF) | ((y & 0x3FF) << 10) | ((z & 0x3FF) << 20);

            if (buckets.TryGetValue(key, out var bucket))
            {
                foreach (var obj in bucket)
                {
                    // 精确距离过滤
                    float dist = Vector3.Distance(center, getPosition(obj));
                    if (dist <= radius)
                        result.Add(obj);
                }
            }
        }

        return result.ToList();
    }

    // 清空
    public void Clear() => buckets.Clear();
}
```

### 空间哈希 vs 八叉树

| 维度 | 空间哈希 | 八叉树 |
|------|---------|--------|
| 实现复杂度 | 低 | 高 |
| 动态物体 | 好（更新 O(1)） | 差（需要重建树） |
| 均匀分布 | 好 | 好 |
| 不均匀分布 | 差（空格子浪费） | 好（自适应细分） |
| 碰撞检测 | 好 | 好 |
| Raycast | 一般 | 更好 |

---

## 5. 四元数 SLERP 与高级旋转

```csharp
// 基础 SLERP 在 Day23 基础篇已有
// 这里介绍高级技巧

public class AdvancedRotation
{
    // 1. 沿指定轴旋转
    public Quaternion RotateAroundAxis(Vector3 axis, float angle)
        => Quaternion.AngleAxis(angle, axis);

    // 2. 从方向 A 平滑转到方向 B
    public Quaternion SmoothRotate(Transform obj, Vector3 targetDir, float smoothTime)
    {
        Quaternion targetRot = Quaternion.LookRotation(targetDir);
        // 使用 SmoothDamp 替代 Slerp（带加速/减速）
        // 需要自己实现或使用：Quaternion 版 SmoothDamp
        return Quaternion.Slerp(obj.rotation, targetRot,
            Time.deltaTime / smoothTime);
    }

    // 3. 限制旋转范围（如炮塔只能 -180~180 度旋转）
    public Quaternion ClampRotation(Quaternion rotation, Vector2 yawRange, Vector2 pitchRange)
    {
        Vector3 euler = rotation.eulerAngles;
        // 转换到 -180~180 范围
        float yaw = euler.y > 180 ? euler.y - 360 : euler.y;
        float pitch = euler.x > 180 ? euler.x - 360 : euler.x;

        yaw = Mathf.Clamp(yaw, yawRange.x, yawRange.y);
        pitch = Mathf.Clamp(pitch, pitchRange.x, pitchRange.y);

        return Quaternion.Euler(pitch, yaw, 0);
    }

    // 4. SQuad（Slerp 的球面四边形插值，比 Slerp 更平滑）
    // Slerp 只插值两条边，SQuad 考虑四个控制点
    // Unity 没有内置 SQuad，可以用第三方库

    // 5. 旋转弹簧效果（带惯性）
    public Quaternion SpringRotation(Quaternion current, Quaternion target,
        ref Vector3 angularVelocity, float stiffness, float damping)
    {
        // 计算当前和目标之间的角度差
        Quaternion diff = target * Quaternion.Inverse(current);
        diff.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;
        if (Mathf.Abs(angle) < 0.01f) return target;

        // 弹簧力（Hooke's Law）
        Vector3 torque = axis * angle * stiffness;
        torque -= angularVelocity * damping;

        // 更新角速度
        angularVelocity += torque * Time.deltaTime;

        // 应用旋转
        Quaternion springRot = Quaternion.AngleAxis(
            angularVelocity.magnitude * Time.deltaTime * Mathf.Rad2Deg,
            angularVelocity.normalized);
        return springRot * current;
    }
}
```

---

## 6. 噪声函数

### Perlin 噪声

```csharp
// Unity 内置的 Perlin 噪声
// 用于：程序化地形、云、纹理

public class NoiseDemo : MonoBehaviour
{
    public int width = 64;
    public int height = 64;
    public float scale = 10f;

    // 生成 Perlin 噪声纹理
    public Texture2D GenerateNoiseTexture()
    {
        Texture2D tex = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Mathf.PerlinNoise 输入 (0~1) 范围
                float sampleX = (float)x / width * scale;
                float sampleY = (float)y / height * scale;

                float noise = Mathf.PerlinNoise(sampleX, sampleY);
                tex.SetPixel(x, y, new Color(noise, noise, noise));
            }
        }

        tex.Apply();
        return tex;
    }

    // 多层叠加（Octave）实现更丰富的噪声
    public float OctaveNoise(float x, float y, int octaves)
    {
        float value = 0;
        float amplitude = 1;
        float frequency = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= 0.5f;  // 每层振幅减半
            frequency *= 2f;    // 每层频率翻倍
        }

        return value / maxValue;  // 归一化到 0~1
    }
}

// Simplex 噪声（比 Perlin 更好）
// Unity 没有内置，需要第三方库
public class SimplexNoiseExample
{
    // FastNoise、FastNoiseLite 等库提供多种噪声
    // 安装后：
    // using FastNoiseLite;
    //
    // var noise = new FastNoiseLite(seed);
    // noise.SetNoiseType(NoiseType.OpenSimplex2);
    // float val = noise.GetNoise(x, y);
}

// Worley 噪声（细胞噪声）
// 用于：格子纹理、水晶效果
public class WorleyNoise
{
    public float GetWorley(float x, float y)
    {
        // 对每个像素，找到最近的特征点距离
        Vector2 pixel = new Vector2(x, y);
        float minDist = float.MaxValue;

        for (int i = 0; i < featurePointCount; i++)
        {
            float dist = Vector2.Distance(pixel, featurePoints[i]);
            minDist = Mathf.Min(minDist, dist);
        }

        return minDist;
    }
}
```

### 噪声应用

```
Perlin 噪声 → 平滑的地形/云
Simplex 噪声 → 效果更好，无方向性artifact
Worley 噪声 → 鳞片/水晶/细胞纹理
FBM（分形布朗运动）→ 多层噪声叠加 → 真实感地形
Domain Warp → 扭曲噪声空间 → 流体效果
```

---

## 7. 贝塞尔曲线与样条

### 贝塞尔曲线

```csharp
// 三次贝塞尔曲线（最常用）
// P0 = 起点, P1 = 控制点1, P2 = 控制点2, P3 = 终点
// B(t) = (1-t)³P0 + 3(1-t)²tP1 + 3(1-t)t²P2 + t³P3

public struct BezierCurve
{
    public Vector3 p0, p1, p2, p3;

    public Vector3 Evaluate(float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        return uu * u * p0      // (1-t)³
             + 3 * uu * t * p1  // 3(1-t)²t
             + 3 * u * tt * p2  // 3(1-t)t²
             + tt * t * p3;     // t³
    }

    // 切线（速度）
    public Vector3 Tangent(float t)
    {
        float u = 1 - t;
        return 3 * u * u * (p1 - p0)
             + 6 * u * t * (p2 - p1)
             + 3 * t * t * (p3 - p2);
    }

    // 沿曲线的长度（数值积分）
    public float ApproximateLength(int samples = 100)
    {
        float length = 0;
        Vector3 prev = p0;

        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 curr = Evaluate(t);
            length += Vector3.Distance(prev, curr);
            prev = curr;
        }

        return length;
    }
}

// 摄像机路径
public class CameraPath : MonoBehaviour
{
    public Transform[] waypoints;  // 路径点
    private BezierCurve[] segments;

    void Start()
    {
        // 用 Catmull-Rom 样条把路径点转为平滑曲线
        segments = new BezierCurve[waypoints.Length - 1];
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            segments[i] = CatmullRomToBezier(i);
        }
    }

    private BezierCurve CatmullRomToBezier(int index)
    {
        // Catmull-Rom 样条：通过所有控制点的平滑曲线
        // 不需要手动调控制点
        Vector3 p0 = waypoints[Mathf.Max(0, index - 1)].position;
        Vector3 p1 = waypoints[index].position;
        Vector3 p2 = waypoints[index + 1].position;
        Vector3 p3 = waypoints[Mathf.Min(index + 2, waypoints.Length - 1)].position;

        // Catmull-Rom → Bezier 转换
        BezierCurve bezier;
        bezier.p0 = p1;
        bezier.p1 = p1 + (p2 - p0) / 6f;
        bezier.p2 = p2 - (p3 - p1) / 6f;
        bezier.p3 = p2;

        return bezier;
    }

    public Vector3 GetPosition(float t)
    {
        // t 是 0~1 整个路径的进度
        float segmentT = t * segments.Length;
        int segIndex = Mathf.Min((int)segmentT, segments.Length - 1);
        float localT = segmentT - segIndex;

        return segments[segIndex].Evaluate(localT);
    }
}
```

### C++/Raylib 对比

| 概念 | C++ | Unity/C# |
|------|-----|----------|
| SIMD | SSE intrinsic / Eigen | System.Numerics.Vector / Burst |
| 定点数 | libfixmath | 自实现 / FixedMath.Net |
| 空间哈希 | 自实现 | 同上 |
| Perlin 噪声 | stb_perlin | Mathf.PerlinNoise |
| Simplex 噪声 | FastNoiseLite | FastNoiseLite(C# 移植) |
| 贝塞尔 | 自实现 / glm | 自实现 |
| 样条 | Catmull-Rom 自实现 | 同上 |

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| SIMD | 一条指令处理多个数据，自动向量化 |
| Unity.Mathematics | Burst 专用数学库，SIMD 友好 |
| 定点数 | 用整数模拟小数，确保跨平台确定性 |
| 空间哈希 | 网格划分空间，O(1) 邻域查询 |
| Perlin 噪声 | 平滑伪随机，程序化生成基础 |
| FBM 噪声 | 多层噪声叠加，更丰富细节 |
| 贝塞尔曲线 | 控制点曲线的数学公式 |
| Catmull-Rom 样条 | 通过所有控制点的平滑曲线 |
| 四元数 Spring | 带物理惯性的旋转插值 |

**对比 C++：** SIMD 在 C++ 中可以用 SSE/AVX intrinsic 手写或用 Eigen 库自动优化。Unity 的 Burst 编译器自动完成这个工作。定点数数学在任何语言的实现都一样——关键是在 Lockstep 项目中必须在所有客户端使用完全相同的数据类型。
