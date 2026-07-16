# Day 23：游戏数学进阶 — 向量·矩阵·四元数

## 0. 为什么需要游戏数学？

游戏本质上是一个**实时 3D 数学计算器**：

```
玩家按 W 键：
1. 获取玩家面向方向向量
2. 用向量 × 速度 × deltaTime = 位移
3. 加到当前位置
4. 渲染器用矩阵把世界坐标转屏幕坐标
5. 像素显示在显示器上

每一帧都在做：向量运算 + 矩阵变换 + 插值
```

在 Raylib 中你可能只用过 `Vector2` 和简单的坐标加减。Unity 隐藏了很多数学细节（Transform 帮你做了），但理解底层数学让你能解决"引擎做不到"的问题。

---

## 1. 向量运算 — 游戏中的"感官"

### 向量的可视化

```
向量 = 有方向的箭头

Vector3(3, 2, 0)：
    ↑ y
    │
    2┊   ↗ (3, 2)
    │  ╱
    1┊ ╱
    │╱
    ─┊─────→ x
    0│ 1  2  3
```

### 点积（Dot Product）— 判断前后

```csharp
// 点积公式：a · b = |a| × |b| × cos(θ)
// 结果 > 0：方向大致相同（在前面）
// 结果 = 0：垂直
// 结果 < 0：方向相反（在后面）

public class DotProductExample : MonoBehaviour
{
    public Transform player;
    public Transform enemy;

    void Update()
    {
        Vector3 toPlayer = (player.position - enemy.position).normalized;
        Vector3 enemyForward = enemy.forward;

        float dot = Vector3.Dot(enemyForward, toPlayer);

        if (dot > 0.7f)
            Debug.Log("玩家在前方很近（夹角 < 45°）");
        else if (dot > 0f)
            Debug.Log("玩家在前方");
        else if (dot > -0.7f)
            Debug.Log("玩家在侧方");
        else
            Debug.Log("玩家在后方");
    }
}
```

### 叉积（Cross Product）— 判断左右

```csharp
// 叉积公式：a × b = 垂直于 a 和 b 的向量
// Unity 是左手坐标系，叉积方向用左手定则

public class CrossProductExample : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        Vector3 toTarget = (target.position - transform.position).normalized;
        Vector3 cross = Vector3.Cross(transform.forward, toTarget);

        // cross.y > 0：目标在右边
        // cross.y < 0：目标在左边
        if (cross.y > 0)
            Debug.Log("目标在右侧");
        else
            Debug.Log("目标在左侧");
    }
}
```

### 向量投影（Projection）— "踩影子"

投影计算"一个向量在另一个方向上的分量"，用于：
- 把移动方向投射到地面（去掉 Y 分量）
- 把力的分量分解

```csharp
// 手动实现向量投影
// project a onto b = (a·b / |b|²) × b
Vector3 ProjectOnto(Vector3 a, Vector3 b)
{
    float dot = Vector3.Dot(a, b);
    float sqrMag = b.sqrMagnitude;
    return b * (dot / sqrMag);
}

// 实际应用：把移动投射到地面
void MoveWithGravity()
{
    Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

    // 把移动向量投射到地面（去掉朝向天花板/地板的部分）
    Vector3 forward = transform.forward;
    forward.y = 0;
    forward.Normalize();
    Vector3 right = transform.right;
    right.y = 0;
    right.Normalize();

    Vector3 worldMove = (forward * moveInput.z + right * moveInput.x) * speed;
    // 保留 Y 方向的重力
    worldMove.y = rigidbody.velocity.y;
    rigidbody.velocity = worldMove;
}
```

### 向量反射（Reflection）— "碰撞反弹"

```csharp
// 反射公式：r = v - 2(v·n)n
// v = 入射方向，n = 法线方向

Vector3 Reflect(Vector3 incoming, Vector3 normal)
{
    return incoming - 2 * Vector3.Dot(incoming, normal) * normal;
}

// Unity 内置版
Vector3 reflected = Vector3.Reflect(velocity, hit.normal);

// 应用：台球反弹、光线反射
```

### 应用：扇形检测（视野范围）

```csharp
public bool IsInFOV(Transform viewer, Transform target, float fovAngle, float maxDistance)
{
    Vector3 toTarget = target.position - viewer.position;

    // 1. 距离检测（用 sqrMagnitude 避免开平方）
    if (toTarget.sqrMagnitude > maxDistance * maxDistance)
        return false;

    // 2. 角度检测（用点积）
    float angle = Vector3.Angle(viewer.forward, toTarget.normalized);
    return angle < fovAngle / 2;
}
```

---

## 2. 矩阵变换 — TRS 的底层

### 位移矩阵（T）

```
位移 (tx, ty, tz) 的矩阵：
┌ 1  0  0  tx ┐
│ 0  1  0  ty │
│ 0  0  1  tz │
└ 0  0  0  1  ┘

乘以顶点 (x, y, z, 1)：
结果 = (x + tx, y + ty, z + tz, 1)
```

### 缩放矩阵（S）

```
缩放 (sx, sy, sz) 的矩阵：
┌ sx 0  0  0 ┐
│ 0  sy 0  0 │
│ 0  0  sz 0 │
└ 0  0  0  1 ┘

乘以顶点 (x, y, z, 1)：
结果 = (x*sx, y*sy, z*sz, 1)
```

### 旋转矩阵（R）— 绕 Y 轴

```
绕 Y 轴旋转 θ 度：
┌ cos(θ)  0  sin(θ)  0 ┐
│   0     1    0      0 │
│ -sin(θ) 0  cos(θ)  0 │
└   0     0    0      1 ┘
```

### TRS 的复合

```csharp
// TRS 矩阵 = 位移 × 旋转 × 缩放
// 注意顺序：先缩放，再旋转，再位移

Matrix4x4 trs = Matrix4x4.TRS(
    new Vector3(1, 2, 3),       // T：位移
    Quaternion.Euler(0, 45, 0), // R：旋转
    Vector3.one * 2             // S：缩放
);

// 矩阵 × 顶点 = 变换后的顶点
Vector3 localPoint = new Vector3(1, 0, 0);
Vector3 worldPoint = trs.MultiplyPoint(localPoint);

// 手动分解的运算过程：
// localPoint(1, 0, 0)
// 1. 缩放：    (1 * 2, 0 * 2, 0 * 2) = (2, 0, 0)
// 2. 旋转 45°： (2*cos45, 0, 2*sin45) ≈ (1.41, 0, 1.41)
// 3. 位移：     (1.41 + 1, 0 + 2, 1.41 + 3) = (2.41, 2, 4.41)
```

### 坐标系转换

```csharp
// 世界坐标 ↔ 局部坐标
public class CoordinateExample : MonoBehaviour
{
    public Transform child;

    void Update()
    {
        // 子对象的世界坐标
        Vector3 worldPos = child.position;

        // 子对象相对于父对象的局部坐标
        Vector3 localPos = child.localPosition;

        // 手动转换：
        // 世界 → 局部 = 世界坐标 × 父对象矩阵的逆
        // 局部 → 世界 = 局部坐标 × 父对象矩阵
        Vector3 computedWorld = transform.TransformPoint(localPos);
        Vector3 computedLocal = transform.InverseTransformPoint(worldPos);
    }
}
```

---

## 3. 四元数 — 为什么不用欧拉角？

### 欧拉角的问题

```csharp
// 欧拉角：用 (x, y, z) 表示旋转
Vector3 euler = new Vector3(30, 45, 60); // 绕 X 转 30°，绕 Y 转 45°，绕 Z 转 60°

// 问题 1：万向锁（Gimbal Lock）
// 当绕 X 旋转 ±90° 时，Y 和 Z 的旋转冲突，丢失一个自由度
// 实际表现：镜头突然翻转、角色诡异旋转

// 问题 2：插值不均匀
Quaternion a = Quaternion.Euler(0, 0, 0);
Quaternion b = Quaternion.Euler(0, 90, 0);
// 用欧拉角插值：Vector3.Lerp(a.eulerAngles, b.eulerAngles, 0.5f)
// → 可能得到 (0, 45, 0)，但中间姿态不对！
// 正确用四元数插值：
Quaternion mid = Quaternion.Slerp(a, b, 0.5f); // 球面插值，姿态正确
```

### 四元数本质

```
四元数 = (w, x, y, z)，其中 w 是实部，(x, y, z) 是虚部

表示绕轴 (ax, ay, az) 旋转 θ 角度：
q = (cos(θ/2), ax·sin(θ/2), ay·sin(θ/2), az·sin(θ/2))

例如：绕 Y 轴转 90°
q = (cos(45°), 0, sin(45°), 0)
  = (0.707, 0, 0.707, 0)

优点：
- 无万向锁
- 插值平滑（Slerp）
- 组合旋转高效（乘法）
- 占 4 个 float，比矩阵（16 个）小
```

### 四元数乘法

```
四元数乘法不满足交换律！q1 * q2 ≠ q2 * q1

先绕 Y 转 90°，再绕 X 转 90°：
q_final = q_x * q_y
       = (cos45°, sin45°, 0, 0) * (cos45°, 0, sin45°, 0)
       = (0.5, 0.5, 0.5, 0.5)

先绕 X 转 90°，再绕 Y 转 90°：
q_final = q_y * q_x
       = (0.5, 0.5, -0.5, 0.5)

结果不同！因为旋转顺序不同
```

### 常用四元数操作

```csharp
public class QuaternionExample : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        // 1. 创建旋转
        Quaternion rot1 = Quaternion.Euler(0, 45, 0);    // 欧拉角转四元数
        Quaternion rot2 = Quaternion.LookRotation(dir);  // 面向方向
        Quaternion rot3 = Quaternion.AngleAxis(90, Vector3.up); // 绕轴转

        // 2. 组合旋转
        // 先绕 Y 转 45°，再绕 X 转 30°
        Quaternion q_y = Quaternion.Euler(0, 45, 0);
        Quaternion q_x = Quaternion.Euler(30, 0, 0);
        Quaternion combined = q_x * q_y; // 注意：先应用的写在右边！

        // 3. 插值
        // 平滑旋转（Slerp = 球面线性插值）
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target.rotation,
            Time.deltaTime * 2f
        );

        // 4. 旋转向量
        Vector3 forward = Quaternion.Euler(0, 45, 0) * Vector3.forward;
        // 把 Vector3.forward 绕 Y 轴转 45°

        // 5. 取逆旋转
        Quaternion inverse = Quaternion.Inverse(transform.rotation);
        // 逆旋转 = 转回去

        // 6. 转欧拉角
        Vector3 euler = transform.rotation.eulerAngles;
    }
}
```

### 手动实现 LookAt

```csharp
public Quaternion ManualLookAt(Vector3 from, Vector3 to)
{
    Vector3 forward = (to - from).normalized;
    Vector3 up = Vector3.up;

    // 1. 计算右向量 = 前 × 上（叉积）
    Vector3 right = Vector3.Cross(up, forward).normalized;

    // 2. 重新计算真正的上向量 = 前 × 右
    Vector3 actualUp = Vector3.Cross(forward, right);

    // 3. 构建旋转矩阵
    Matrix4x4 lookMatrix = Matrix4x4.identity;
    lookMatrix.SetColumn(0, right);
    lookMatrix.SetColumn(1, actualUp);
    lookMatrix.SetColumn(2, forward);

    // 4. 矩阵转四元数
    return lookMatrix.rotation;
}
```

---

## 4. 碰撞检测数学

### AABB（轴对齐包围盒）

```csharp
public struct AABB
{
    public Vector3 center;
    public Vector3 halfSize; // 半尺寸

    // AABB vs AABB
    public bool Intersects(AABB other)
    {
        Vector3 diff = other.center - center;
        Vector3 overlap = halfSize + other.halfSize - new Vector3(
            Mathf.Abs(diff.x),
            Mathf.Abs(diff.y),
            Mathf.Abs(diff.z)
        );

        return overlap.x > 0 && overlap.y > 0 && overlap.z > 0;
    }

    // 点是否在 AABB 内
    public bool ContainsPoint(Vector3 point)
    {
        Vector3 diff = point - center;
        return Mathf.Abs(diff.x) <= halfSize.x
            && Mathf.Abs(diff.y) <= halfSize.y
            && Mathf.Abs(diff.z) <= halfSize.z;
    }
}
```

### Raycast（射线检测）

```csharp
public struct Ray
{
    public Vector3 origin;
    public Vector3 direction;

    // 射线 vs 球体
    // 公式推导：
    // 球体方程：|P - C|² = r²
    // 射线方程：P = O + t·D
    // 代入：|O + t·D - C|² = r²
    // 展开：(D·D)t² + 2(O-C)·D t + (O-C)·(O-C) - r² = 0
    // 判别式 Δ = b² - 4ac >= 0 时有交点
    public bool IntersectsSphere(Vector3 sphereCenter, float sphereRadius)
    {
        Vector3 oc = origin - sphereCenter;
        float a = Vector3.Dot(direction, direction);
        float b = 2 * Vector3.Dot(oc, direction);
        float c = Vector3.Dot(oc, oc) - sphereRadius * sphereRadius;
        float discriminant = b * b - 4 * a * c;

        return discriminant >= 0;
    }

    // 射线 vs 平面
    public bool IntersectsPlane(Vector3 planeNormal, float planeDistance, out float t)
    {
        float denom = Vector3.Dot(direction, planeNormal);
        if (Mathf.Abs(denom) < 1e-6f) // 平行
        {
            t = 0;
            return false;
        }
        t = -(Vector3.Dot(origin, planeNormal) + planeDistance) / denom;
        return t >= 0;
    }
}
```

---

## 5. Unity 数学 vs 手写数学

```csharp
// Unity 已经封装好了所有常用运算
public class UnityMathWrap : MonoBehaviour
{
    void Demo()
    {
        // 向量
        float mag = Vector3.Magnitude(vec);
        float sqrMag = vec.sqrMagnitude;  // 避免开平方，比 magnitude 快
        Vector3 norm = vec.normalized;
        float dot = Vector3.Dot(a, b);
        Vector3 cross = Vector3.Cross(a, b);
        float dist = Vector3.Distance(a, b);
        Vector3 lerped = Vector3.Lerp(a, b, t);
        Vector3 proj = Vector3.Project(vec, onNormal);
        Vector3 refl = Vector3.Reflect(dir, normal);

        // 矩阵
        Matrix4x4 trs = Matrix4x4.TRS(pos, rot, scale);
        Vector3 world = trs.MultiplyPoint3x4(local);
        Vector3 local = trs.inverse.MultiplyPoint3x4(world);

        // 四元数
        Quaternion look = Quaternion.LookRotation(dir);
        Quaternion slerped = Quaternion.Slerp(a, b, t);
        Quaternion fromTo = Quaternion.FromToRotation(from, to);
        Vector3 euler = quat.eulerAngles;
        Quaternion inv = Quaternion.Inverse(quat);
    }
}
```

### 数学性能提示

```csharp
// ❌ 慢：频繁计算 magnitude（涉及开平方）
if (Vector3.Distance(a, b) < range)

// ✅ 快：用 sqrMagnitude（只需乘加，无开平方）
float sqrRange = range * range;
if ((a - b).sqrMagnitude < sqrRange)

// ❌ 慢：每帧新建向量
void Update() {
    transform.position = new Vector3(x, y, z);  // 产生 GC
}

// ✅ 快：复用
private Vector3 tempPos;
void Update() {
    tempPos.Set(x, y, z);
    transform.position = tempPos;
}

// ⚠️ 避免每帧用除法
float half = value / 2f;      // 除法比乘法慢
float half = value * 0.5f;     // 乘法更快
```

---

## 6. 练习

### 练习 1：向量检测

```csharp
// 实现一个"警戒系统"：
// 1. 敌人有 120° 视野，10m 检测范围
// 2. 判断目标是否在视野内（用点积）
// 3. 判断目标在左边还是右边（用叉积）
// 4. 用 Gizmos 画出视野范围
//
// 附加：有障碍物遮挡时不应看到玩家（加射线检测）
```

### 练习 2：手动 TRS

```csharp
// 不借助 Transform，手动实现：
// 1. 构建一个物体在世界中的 TRS 矩阵
// 2. 将一个局部坐标转换为世界坐标
// 3. 将世界坐标转回局部坐标
// 4. 验证结果和 Transform 的一致
//
// 进阶：实现一个围绕原点旋转的方块（不用 Transform.Rotate）
```

### 练习 3：四元数旋转

```csharp
// 实现第一人称摄像机旋转（不用 Transform.LookAt）：
// 1. 鼠标垂直移动 → 绕 X 轴旋转（上下看）
// 2. 鼠标水平移动 → 绕 Y 轴旋转（左右看）
// 3. 用四元数乘法组合两个旋转
// 4. 防止万向锁（限制垂直角度 ±90°）
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 点积 | 判断前后（cos(θ)） |
| 叉积 | 判断左右（垂直向量） |
| 投影 | 一个向量在另一个方向上的分量 |
| 反射 | 碰撞反弹方向 |
| 矩阵 | T × R × S → 4×4 矩阵变换坐标 |
| 四元数 | (w, x, y, z)，绕轴旋转，无万向锁 |
| Slerp | 球面插值，旋转动画的基石 |
| sqrMagnitude | 比 Distance 快，因为没有开平方 |

**对比 Raylib：** Raylib 也有 `Vector2`/`Vector3` 和矩阵运算（`Matrix`、`MatrixRotateXYZ`），但 Unity 的 `Mathf`、`Vector3`、`Quaternion`、`Matrix4x4` 是完整封装的数学库，用法更简洁。四元数是 Unity 的默认旋转方式，Raylib 中你更多用欧拉角。Raylib 的 `Matrix` 操作需要手动管理，Unity 的 `Transform` 帮你自动维护。
