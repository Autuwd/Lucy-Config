# Day 7：Transform 与坐标系 — 深入篇：矩阵运算、Job System 整合与高级变换

## 0. 引言：隐藏的矩阵链

上一章我们知道了 Transform 维护 m_LocalToWorldMatrix 和 m_WorldToLocalMatrix 两个矩阵缓存。但矩阵是怎么计算的？如何利用 Job System 批量处理变换？本文回答这些底层问题。

在 Raylib/C++ 中，矩阵运算是显式的：
```cpp
Matrix mat = MatrixIdentity();
mat = MatrixMultiply(mat, MatrixTranslate(x, y, z));
mat = MatrixMultiply(mat, MatrixRotateY(angle));
mat = MatrixMultiply(mat, MatrixScale(sx, sy, sz));
// 然后：rlm = mat; 用于 shader
```

Unity 的 Transform 自动做这一切——但了解矩阵原理让你能突破 API 的限制。

---

## 1. LocalToWorld 矩阵的数学推导

### TRS 矩阵构造

每个 Transform 在 C++ 层维护一个 **TRS 矩阵** = T(平移) × R(旋转) × S(缩放)：

```
T = | 1  0  0  tx |    R = 四元数 → 3×3 旋转矩阵
    | 0  1  0  ty |    
    | 0  0  1  tz |    S = | sx  0   0   0 |
    | 0  0  0  1  |         | 0   sy  0   0 |
                            | 0   0   sz  0 |
                            | 0   0   0   1 |

最终 TRS = T × R × S：

| R00*sx  R01*sy  R02*sz  tx |
| R10*sx  R11*sy  R12*sz  ty |
| R20*sx  R21*sy  R22*sz  tz |
|   0       0       0     1  |

注意：先缩放再旋转再平移 = TRS 顺序
      先旋转再缩放 = 非均匀缩放 + 旋转 = 错切
```

### 层级矩阵乘法链

```csharp
// 从根到叶子的矩阵链：
// 每个节点的 worldMatrix = parent.worldMatrix * localTRS

// 用代码验证：
public class MatrixDebug : MonoBehaviour
{
    void Start()
    {
        // 手动计算 worldToLocalMatrix
        Matrix4x4 localToWorld = transform.localToWorldMatrix;
        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

        // 验证：两者互为逆矩阵
        Debug.Assert(
            (localToWorld * worldToLocal).isIdentity,
            "localToWorld and worldToLocal should be inverses"
        );

        // 查看矩阵内容
        Debug.Log(localToWorld.ToString("F4"));

        // 手动验证位置：
        // localToWorld 的最后一列 = 世界坐标位置
        Vector4 posColumn = localToWorld.GetColumn(3);
        Debug.Log($"Position from matrix: {posColumn}");
        Debug.Log($"Transform.position: {transform.position}");
    }
}
```

### WorldToLocal 的计算

```
worldToLocalMatrix = inverse(localToWorldMatrix)

对于 TRS 矩阵，逆矩阵有解析公式（不需要高斯消元）：
- 平移的逆 = -平移（在旋转空间中）
- 旋转的逆 = 转置（因为旋转矩阵是正交矩阵）
- 缩放的逆 = 1/缩放

所以：worldToLocal = S⁻¹ × R⁻¹ × T⁻¹
                 = 缩放逆 × 旋转逆 × 平移逆
```

---

## 2. TransformAccessArray——批量处理变换（Job System）

当你需要处理成千上万个对象的变换时，单个操作效率太低。**TransformAccessArray** 允许你在 Job 中并⾏读取/写入 Transform 数据。

```csharp
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class TransformJobDemo : MonoBehaviour
{
    public int objectCount = 10000;
    public GameObject prefab;
    private TransformAccessArray accessArray;
    private Transform[] transforms;

    void Start()
    {
        // 创建 10000 个对象
        transforms = new Transform[objectCount];
        for (int i = 0; i < objectCount; i++)
        {
            GameObject obj = Instantiate(prefab, 
                Random.insideUnitSphere * 50f, Quaternion.identity);
            transforms[i] = obj.transform;
        }

        // 用 TransformAccessArray 包装——零拷贝
        accessArray = new TransformAccessArray(transforms);
    }

    void Update()
    {
        // 创建旋转 Job——在 Job 中并行更新所有 Transform
        var job = new RotationJob
        {
            deltaTime = Time.deltaTime,
            speed = 30f
        };

        // 调度 Job（使用所有可用的工作线程）
        JobHandle handle = job.Schedule(accessArray);
        
        // 等待 Job 完成
        handle.Complete();

        // 注意：因为我们在主线程调用了 Complete
        // 所以可以直接访问 transforms
    }

    void OnDestroy()
    {
        // 必须释放！TransformAccessArray 是原生容器
        if (accessArray.IsCreated)
            accessArray.Dispose();
    }
}

// 并⾏旋转 Job——同时处理多个 Transform
public struct RotationJob : IJobParallelForTransform
{
    public float deltaTime;
    public float speed;

    // 每个 Transform 会调用一次这个方法
    public void Execute(int index, TransformAccess transform)
    {
        // 注意：这里用的是 TransformAccess——不是 Transform
        // TransformAccess 是 Transform 的"代理"，可在 Job 中使用
        Vector3 pos = transform.position;
        
        // 旋转逻辑（绕 Y 轴公转）
        float angle = speed * deltaTime;
        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        
        float x = pos.x * cos - pos.z * sin;
        float z = pos.x * sin + pos.z * cos;
        
        transform.position = new Vector3(x, pos.y, z);
        transform.rotation = transform.rotation * 
            Quaternion.Euler(0, angle, 0);
    }
}
```

### TransformAccess 的局限性

```csharp
// TransformAccess 只支持：
// - position（Vector3）
// - rotation（Quaternion）
// - localPosition
// - localRotation
// - localScale

// 不支持：
// - parent（父子关系变更）
// - SetParent
// - GetChild
// - TransformPoint / InverseTransformPoint

// 所以：TransformAccess 适合"大量对象的独立变换更新"
// 不适合：层级结构变更
```

### 与常规 Transform API 的性能对比

```
操作 10000 个对象旋转（每帧）：

常规方法（单线程）：
for (int i = 0; i < 10000; i++)
{
    transforms[i].Rotate(Vector3.up, speed * Time.deltaTime);
}
→ 单线程：约 3-5ms

TransformAccessArray（并⾏ Job）：
var job = new RotationJob { deltaTime = Time.deltaTime, speed = speed };
JobHandle handle = job.Schedule(accessArray);
handle.Complete();
→ 6 线程并⾏：约 0.5-1ms
→ 提升 5-10 倍（取决于 CPU 核心数）
```

---

## 3. Transform.rotation 内部——四元数运算

上一章介绍了四元数的基本概念。这里深入四元数的运算法则。

### 四元数乘法——旋转的组合

```csharp
// 四元数乘法 ≠ 欧拉角加法
// 两个旋转的组合 = 四元数乘法

Quaternion a = Quaternion.Euler(0, 90, 0);  // 绕 Y 转 90°
Quaternion b = Quaternion.Euler(90, 0, 0);  // 绕 X 转 90°

// 组合旋转：先 a 再 b
Quaternion combined = b * a;  // 注意顺序！右侧先应用
// 先绕 Y 转 90°，再绕 X 转 90°

// 逆旋转
Quaternion inverse = Quaternion.Inverse(combined);
// combined * inverse = identity（无旋转）

// 四元数点乘——测量旋转差异
float dot = Quaternion.Dot(a, b);
// dot = 1 → 完全相同
// dot = -1 → 相反方向
// dot = 0 → 垂直（90° 差异）

// 角度差
float angle = Quaternion.Angle(a, b);
Debug.Log($"Angle between: {angle}°");
```

### 手动实现一个简单的四元数类（逻辑讲解）

```csharp
// Unity 内部四元数乘法的简化实现：
// q1 * q2 的数学公式：

public struct MyQuaternion
{
    public float x, y, z, w;

    public static MyQuaternion Multiply(MyQuaternion a, MyQuaternion b)
    {
        // 四元数乘法：Graßmann 积
        return new MyQuaternion
        {
            w = a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z,
            x = a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            y = a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            z = a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w
        };
    }

    // 用四元数旋转一个向量：
    // v' = q * v * q⁻¹
    public static Vector3 Rotate(MyQuaternion q, Vector3 v)
    {
        // 将向量转为纯四元数 (0, v)
        MyQuaternion p = new MyQuaternion 
        { 
            w = 0, x = v.x, y = v.y, z = v.z 
        };
        
        MyQuaternion qInv = Inverse(q);
        MyQuaternion result = Multiply(Multiply(q, p), qInv);
        
        return new Vector3(result.x, result.y, result.z);
    }
}
```

### Slerp 的数学原理

```csharp
// Quaternion.Slerp(a, b, t) 球面线性插值
// 在四元数球面上沿弧线插值——产生平滑的旋转过渡

// 数学原理：
// 1. 计算 a 和 b 之间的角度差
// 2. 在球面上沿弧线进行插值
// 3. 结果保持单位四元数（长度 = 1）

// Slerp vs Lerp（线性插值）：
Quaternion.Lerp(a, b, 0.5f);
// 直接线性插值 + 归一化
// 更快，但在大角度时不均匀

Quaternion.Slerp(a, b, 0.5f);
// 球面插值
// 更慢，但在大角度时更平滑
// 对于小角度（< 30°），两者几乎没有差别
```

---

## 4. 自定义 Transform 传播

### 手动控制矩阵

某些情况下，你需要绕过 Transform 直接操作矩阵。

```csharp
// 自定义变换——不依赖 Transform 层级
public class CustomTransformation : MonoBehaviour
{
    // 手动管理的变换数据
    public Vector3 customPosition;
    public Quaternion customRotation;
    public Vector3 customScale = Vector3.one;

    // 手动构建 TRS 矩阵
    public Matrix4x4 GetTRSMatrix()
    {
        return Matrix4x4.TRS(customPosition, customRotation, customScale);
    }

    // 矩阵分解——从矩阵中提取 TRS
    public void DecomposeMatrix(Matrix4x4 matrix)
    {
        // Matrix4x4.Decompose 返回 TRS
        Vector3 translation;
        Quaternion rotation;
        Vector3 scale;
        
        bool success = matrix.Decompose(
            out translation, 
            out rotation, 
            out scale
        );

        if (success)
        {
            customPosition = translation;
            customRotation = rotation;
            customScale = scale;
        }
    }

    void Update()
    {
        // 直接设置矩阵——跳过 Transform 属性
        // 注意：这不会影响子对象！
        // 如果你的对象有子对象，子对象不会跟随
        Matrix4x4 mat = GetTRSMatrix();
        
        // 通过矩阵设置位置
        transform.position = mat.GetColumn(3);
        // 通过矩阵设置旋转
        transform.rotation = mat.rotation;
        // 通过矩阵设置缩放
        transform.localScale = mat.lossyScale;
    }
}
```

### Matrix4x4.TRS 的完整分解

```csharp
public class TRSDecomposition : MonoBehaviour
{
    void Start()
    {
        Matrix4x4 mat = transform.localToWorldMatrix;

        // 手动提取平移
        Vector3 translation = mat.GetColumn(3);

        // 手动提取缩放
        Vector3 scale = new Vector3(
            mat.GetColumn(0).magnitude,  // X 轴长度
            mat.GetColumn(1).magnitude,  // Y 轴长度
            mat.GetColumn(2).magnitude   // Z 轴长度
        );

        // 手动提取旋转——去除缩放的影响
        Matrix4x4 rotationMat = mat;
        rotationMat.SetColumn(0, mat.GetColumn(0) / scale.x);
        rotationMat.SetColumn(1, mat.GetColumn(1) / scale.y);
        rotationMat.SetColumn(2, mat.GetColumn(2) / scale.z);
        Quaternion rotation = rotationMat.rotation;

        Debug.Log($"Translation: {translation}");
        Debug.Log($"Rotation: {rotation.eulerAngles}");
        Debug.Log($"Scale: {scale}");
    }
}
```

---

## 5. SetParent 的矩阵运算——worldPositionStays 的真相

上一章介绍了 `SetParent(parent, true)` 会保持世界位置不变。内部发生了什么？

```csharp
// SetParent(parent, worldPositionStays: true) 的内部逻辑（伪代码）

// 1. 保存当前世界矩阵
Matrix4x4 currentWorldMatrix = transform.localToWorldMatrix;

// 2. 修改父对象
transform.parent = newParent;

// 3. 重新计算 localPosition/localRotation/localScale
//    使得 worldMatrix 保持不变
//    newLocalTRS = inverse(newParent.worldMatrix) * currentWorldMatrix
    
//    相当于：
Vector3 savedPosition = transform.position;       // 保存世界坐标
Quaternion savedRotation = transform.rotation;     // 保存世界旋转
Vector3 savedScale = transform.lossyScale;         // 保存世界缩放

transform.SetParent(newParent, false);  // 不保持世界位置

// 然后手动还原世界位置：
// ...但这不生效，因为世界位置取决于父对象！
// Unity 用矩阵乘逆矩阵来计算新的 local 值：

// newLocalPosition = newParent.worldToLocalMatrix * savedPosition
// newLocalRotation = Quaternion.Inverse(newParent.rotation) * savedRotation
// newLocalScale = ... (更复杂，涉及非均匀缩放)
```

```csharp
// 手动实现 SetParent 的效果：

public static void Reparent(Transform child, Transform newParent, bool keepWorld)
{
    if (keepWorld)
    {
        // 保存世界状态
        Vector3 pos = child.position;
        Quaternion rot = child.rotation;
        Vector3 scale = child.lossyScale;

        // 切换父亲（不保持）
        child.SetParent(newParent, false);

        // 手动设置新的 local 值
        child.localPosition = newParent != null 
            ? newParent.InverseTransformPoint(pos)  // 世界→局部
            : pos;
        
        child.localRotation = newParent != null
            ? Quaternion.Inverse(newParent.rotation) * rot
            : rot;
        
        // 缩放还原比较复杂——因为 lossyScale 是只读的
    }
    else
    {
        child.SetParent(newParent, false);
    }
}
```

---

## 6. Transform.hasChanged 标志位

```csharp
public class HasChangedDemo : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        // hasChanged 在每一帧开始时重置为 false
        // 如果这一帧 Transform 被修改了，变成 true
        
        if (target.hasChanged)
        {
            Debug.Log("Target moved this frame!");
            
            // 处理位置变化
            UpdateFollower();
            
            // 手动重置标志（避免重复处理）
            target.hasChanged = false;
        }
    }

    // hasChanged 的典型应用：延迟更新缓存
    private Matrix4x4 cachedWorldMatrix;
    private bool matrixDirty = true;

    public Matrix4x4 GetCachedWorldMatrix()
    {
        if (transform.hasChanged || matrixDirty)
        {
            cachedWorldMatrix = transform.localToWorldMatrix;
            matrixDirty = false;
            // 注意：不重置 hasChanged——让引擎管理
        }
        return cachedWorldMatrix;
    }
}
```

### hasChanged 的底层行为

```
Engine 内部的 hasChanged 工作方式：

帧开始：
  → 将所有 Transform.hasChanged 设为 false
  → 看是否有变换操作

如果你修改了 transform.position：
  → 设置 dirty 标志（矩阵需要重算）
  → 设置 hasChanged = true
  → 通知子节点（设置子节点的 dirty）

如果你读取 transform.position：
  → 检查 dirty
  → 如果 dirty：重算矩阵
  → 返回缓存值

关键：hasChanged 是只读的（你可以写，但不推荐）
      引擎在帧开始重置它
```

---

## 7. TransformStreamHandle——Burst 兼容的变换操作

当使用 Burst Compiler 和 Job System 时，TransformAccessArray 是首选。但如果你需要操作单个 Transform 的 Job，可以用 TransformStreamHandle。

```csharp
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel;
using UnityEngine;
using UnityEngine.Jobs;

public class StreamHandleDemo : MonoBehaviour
{
    private TransformStreamHandle handle;

    void Start()
    {
        // 获取当前 Transform 的流式句柄
        handle = transform.GetTransformStreamHandle();
    }

    void Update()
    {
        var job = new TransformStreamJob
        {
            handle = handle,
            time = Time.time
        };
        job.Schedule().Complete();
    }
}

public struct TransformStreamJob : IJob
{
    public TransformStreamHandle handle;
    public float time;

    public void Execute()
    {
        // 检查句柄是否有效
        if (!handle.isValid) return;

        // 读取当前变换
        // 需要 isExclusive = false（非独占访问）
        Vector3 pos = handle.GetLocalPosition(false);
        
        // 更新变换
        handle.SetLocalPosition(false, new Vector3(
            Mathf.Sin(time) * 5f,
            pos.y,
            pos.z
        ));
    }
}
```

---

## 8. 坐标系转换实战：Gizmo 绘制

```csharp
using UnityEngine;

public class CoordinateGizmo : MonoBehaviour
{
    [Header("Gizmo Settings")]
    public float gizmoSize = 1f;
    public bool showAxes = true;
    public bool showMatrix = false;

    // 在 Scene 视图中可视化变换矩阵
    void OnDrawGizmosSelected()
    {
        if (!showAxes) return;

        // 绘制局部坐标轴——验证 rotation
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * gizmoSize);
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * gizmoSize);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * gizmoSize);

        if (!showMatrix) return;

        // 可视化 localToWorldMatrix 的四列
        Matrix4x4 mat = transform.localToWorldMatrix;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(mat.GetColumn(3), 0.1f);  // 位置

        // 如果是子对象，绘制父对象的局部坐标系
        if (transform.parent != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(
                transform.parent.position, 
                gizmoSize * 1.2f
            );
        }
    }
}
```

---

## C++/Raylib 对照总结

| 高级概念 | Raylib (C++) | Unity (C#) |
|---------|-------------|-----------|
| 矩阵运算 | `MatrixMultiply`, 手写 TRS | `Matrix4x4.TRS`, `Decompose` |
| 批量变换 | 手写 SSE/AVX 循环 | `TransformAccessArray` + Job |
| 四元数运算 | 无内置四元数 | `Quaternion *`, `Slerp`, `Angle` |
| 矩阵分解 | 无 | `Matrix4x4.Decompose()` |
| 变换检测 | 手动标志位 | `transform.hasChanged` |
| 并⾏变换 | 手动多线程 | `IJobParallelForTransform` |
| 流式句柄 | 无 | `TransformStreamHandle`（Burst 兼容） |

## 停靠点

> TRS 矩阵 = Translation × Rotation × Scale；层级结构 = 矩阵乘法链。
> `TransformAccessArray` + `IJobParallelForTransform` 实现并⾏批量变换——10000 个对象性能提升 5-10 倍。
> 四元数乘法不是交换的——`b * a` 表示"先 a 再 b"。
> `SetParent(parent, true)` 的底层 = 保存世界矩阵 → 切换父亲 → 用逆矩阵重新计算局部值。
> `hasChanged` 每帧重置——用于检测 Transform 是否被修改。
> 矩阵分解 = 从 `Matrix4x4` 中提取 position/rotation/scale——不要求矩阵必须是 TRS。
