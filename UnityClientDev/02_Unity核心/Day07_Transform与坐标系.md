# Day 7：Transform 与坐标系 — 从 TRS 矩阵到空间变换

## 0. 为什么需要 Transform？

每个 GameObject 在场景中都有一个**空间位置**（在哪里？）、**朝向**（面向哪里？）、**大小**（多大？）。

在 Raylib/C++ 中，这些信息是手动管理的：
```cpp
// Raylib：手动管理位置、旋转、缩放
Vector2 position = {100, 200};
float rotation = 45.0f;
float scale = 1.0f;

// 变换也要手动：
// position += direction * speed * dt;
// DrawTexturePro(texture, src, dest, origin, rotation, WHITE);
```

在 Unity 中，**Transform 组件**统一管理这些，并且自动维护父子层级关系。

---

## 1. Transform 的核心数据结构

Transform 是 Unity 引擎中**每个 GameObject 必须拥有**的组件（不能删除）。它的底层是一个 C++ 对象：

```
Transform 的内部数据结构（C++ 层）：
┌─────────────────────────────────────┐
│ Transform                            │
├─────────────────────────────────────┤
│ m_LocalPosition: Vector3            │ ← 相对父对象的局部坐标
│ m_LocalRotation: Quaternion         │ ← 相对父对象的局部旋转
│ m_LocalScale: Vector3               │ ← 相对父对象的局部缩放
│                                       │
│ m_Parent: Transform*                │ ← 指向父 Transform 的指针
│ m_Children: List<Transform*>        │ ← 子 Transform 列表
│                                       │
│ m_LocalToWorldMatrix: Matrix4x4     │ ← 缓存的世界矩阵
│ m_WorldToLocalMatrix: Matrix4x4     │ ← 缓存的逆矩阵
│                                       │
│ m_Dirty: bool                       │ ← 修改时标记为脏
│ m_ParentDirty: bool                 │ ← 父节点变换时标记
└─────────────────────────────────────┘
```

### Dirty 标志位——性能优化

```csharp
// 当你修改 transform.position：
transform.position = new Vector3(10, 0, 0);

// Unity 不会立即重新计算所有矩阵
// 而是将 dirty 标志设为 true
// 实际矩阵在访问时重新计算

// 比如：
Vector3 pos = transform.position;  // 读取——此时 recalc 如果 dirty
```

**性能影响：** 频繁修改 transform 会触发矩阵重算。如果要设置多个属性，一次设置完：

```csharp
// ❌ 坏：每次赋值都触发矩阵重算
transform.position = new Vector3(1, 0, 0);
transform.rotation = Quaternion.Euler(0, 90, 0);
transform.localScale = new Vector3(2, 2, 2);
// dirty 了 3 次，重算了 3 次

// ✅ 好：使用 SetPositionAndRotation 一次设置
// 注意：没有 SetScale，但可以用中间变量
Vector3 newPos = new Vector3(1, 0, 0);
Quaternion newRot = Quaternion.Euler(0, 90, 0);
transform.SetPositionAndRotation(newPos, newRot);
// 只 dirty 一次
```

---

## 2. 位置——position 与 localPosition

### 世界坐标 vs 局部坐标

```csharp
// position = 世界坐标（原点 (0,0,0) 是场景中心）
transform.position = new Vector3(10, 0, 0);
// 对象被放在世界坐标 (10, 0, 0) 处

// localPosition = 相对父对象的偏移
// 如果父对象在世界坐标 (5, 0, 0)：
transform.localPosition = new Vector3(5, 0, 0);
// 对象在世界坐标 (10, 0, 0) = (5 + 5, 0, 0)
```

```
父子关系示例：
世界原点 (0,0,0)
    │
    └── Sun (position=5,0,0)
           │
           └── Earth (localPosition=3,0,0)
                  │
                  └── Moon (localPosition=1,0,0)

Sun 的世界坐标 = (5, 0, 0)
Earth 的世界坐标 = (5 + 3, 0, 0) = (8, 0, 0)
Moon 的世界坐标 = (5 + 3 + 1, 0, 0) = (9, 0, 0)
```

### Transform 的矩阵计算

```csharp
// Unity 内部计算世界坐标：
// 世界坐标 = 父对象的世界矩阵 × 局部位置

// 矩阵乘法链：
// 根节点：WorldMatrix = TRS(localPosition, localRotation, localScale)
// 子节点：WorldMatrix = Parent.WorldMatrix × TRS(localPosition, localRotation, localScale)
// 以此类推...整个层级树形成一条链

// 以 Sun → Earth → Moon 为例：
// Sun.WorldMatrix = I (单位矩阵) × TRS(5,0,0, ...) = 平移矩阵
// Earth.WorldMatrix = Sun.WorldMatrix × TRS(3,0,0, ...)
// Moon.WorldMatrix = Earth.WorldMatrix × TRS(1,0,0, ...)
```

---

## 3. 旋转——四元数（Quaternion）

### 为什么不用欧拉角？

```csharp
// 欧拉角 (Euler Angles) = (pitch, yaw, roll) = (x, y, z)
// 直观，但有严重问题：万向锁（Gimbal Lock）

// 万向锁示例：当 pitch = 90° 时
transform.eulerAngles = new Vector3(90, 0, 0);  // 仰头
transform.eulerAngles = new Vector3(90, 90, 0); // 试图绕 y 轴转...
// 实际上绕 z 轴转了！因为 pitch=90° 导致 y 轴和 z 轴对齐

// 这就是万向锁——丢失了一个旋转自由度
```

### 四元数（Quaternion）的数学原理

```
四元数 = (x, y, z, w)，其中 x² + y² + z² + w² = 1

用 4 个分量表示 3D 旋转：
- (x, y, z) = 旋转轴的方向（单位向量）
- w = 绕该轴旋转角度的 cos(θ/2)

θ = 2 * arccos(w)
轴 = (x, y, z) / sin(θ/2)

好处：无万向锁，插值平滑，计算稳定
代价：不直观，4 个数字不如 3 个好理解
```

### Unity 中的 Quaternion API

```csharp
// 创建旋转的几种方式

// 1. 用欧拉角创建（推荐——直观）
transform.rotation = Quaternion.Euler(0, 90, 0);  // 绕 Y 轴转 90°

// 2. 看向一个方向
transform.rotation = Quaternion.LookRotation(target.position - transform.position);
// 让对象的 Z 轴正方向指向目标

// 3. 绕轴旋转
transform.rotation = Quaternion.AngleAxis(45, Vector3.up);  // 绕 Y 轴转 45°

// 4. 两个旋转之间插值
Quaternion from = transform.rotation;
Quaternion to = Quaternion.Euler(0, 90, 0);
float t = 0.5f;
transform.rotation = Quaternion.Slerp(from, to, t);  // Slerp = 球面线性插值
```

### 方向向量

```csharp
// Unity 中每个对象有自己的"前/右/上"方向
// 这些方向受 rotation 影响

Vector3 forward = transform.forward;  // 对象的 Z 轴正方向（蓝色箭头）
Vector3 right = transform.right;      // 对象的 X 轴正方向（红色箭头）
Vector3 up = transform.up;            // 对象的 Y 轴正方向（绿色箭头）

// 示例：在对象前方生成物体
Instantiate(bulletPrefab, transform.position + transform.forward * 2, transform.rotation);
```

---

## 4. 缩放——localScale

```csharp
// 缩放相对于父对象
transform.localScale = new Vector3(2, 2, 2);  // 放大 2 倍

// 注意：Unity 不支持非均匀缩放在旋转后的子对象上
// 会导致 parent 变换矩阵不是纯正交的
// 复杂的情况下，子对象的缩放可能产生"错切"（shear）效果
```

---

## 5. 父子层级关系

### 设置父子关系

```csharp
// 方式 1：直接设置 parent（不保留世界坐标）
moon.transform.parent = earth.transform;
moon.transform.localPosition = new Vector3(1, 0, 0);  // 相对地球的偏移

// 方式 2：SetParent（可保留世界坐标）
moon.transform.SetParent(earth.transform);            // 不保留世界坐标
moon.transform.SetParent(earth.transform, true);      // 保留世界坐标！

// 方式 3：解除父子关系
moon.transform.parent = null;
// 或者：
moon.transform.SetParent(null);
```

### SetParent(true) 的原理

```csharp
// SetParent(parent, worldPositionStays: true)
// 将子对象保持在世界空间中的位置不变

// 例如：Moon 的世界坐标是 (9,0,0)，父对象是 Earth (8,0,0)
moon.transform.SetParent(sun.transform, true);
// Moon 的世界坐标仍然保持 (9,0,0)
// 但 Moon 的 localPosition 会被重新计算：
// localPosition = worldPosition - sun.worldPosition
//              = (9,0,0) - (5,0,0) = (4,0,0)
```

### 遍历层级

```csharp
// 获取父对象
Transform parent = transform.parent;

// 获取子对象数量
int childCount = transform.childCount;

// 遍历所有子对象
for (int i = 0; i < transform.childCount; i++)
{
    Transform child = transform.GetChild(i);
    Debug.Log($"Child {i}: {child.name}");
}

// 查找子对象（递归）
Transform found = transform.Find("Child/GrandChild");  // 按路径查找
```

---

## 6. 坐标空间与转换

### 坐标空间层级

```
模型空间 (Model Space / Local Space)
    ↓ 模型矩阵 (Model Matrix = TRS)
世界空间 (World Space)
    ↓ 视图矩阵 (View Matrix = Camera 的逆矩阵)
视口空间 (View Space / Camera Space)
    ↓ 投影矩阵 (Perspective / Orthographic Projection)
裁剪空间 (Clip Space)
    ↓ 透视除法 (w 分量归一化)
NDC 空间 (Normalized Device Coordinates: -1 ~ 1)
    ↓ 视口变换
屏幕空间 (Screen Space: 像素坐标)
```

### 坐标转换 API

```csharp
// 局部坐标 ↔ 世界坐标
Vector3 worldPos = transform.TransformPoint(localPos);     // 局部→世界
Vector3 localPos = transform.InverseTransformPoint(worldPos); // 世界→局部

// 方向转换（不受平移影响）
Vector3 worldDir = transform.TransformDirection(localDir);  // 局部方向→世界
Vector3 localDir = transform.InverseTransformDirection(worldDir);

// 向量转换（受缩放影响）
Vector3 worldVec = transform.TransformVector(localVec);
Vector3 localVec = transform.InverseTransformVector(worldVec);
```

### Camera 坐标转换

```csharp
Camera cam = Camera.main;

// 世界坐标 → 屏幕像素坐标
Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
// screenPos.x = 0~Screen.width, screenPos.y = 0~Screen.height
// screenPos.z = 到摄像机的距离

// 屏幕像素坐标 → 世界坐标（需要指定 Z 深度）
Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenX, screenY, zDistance));

// 世界坐标 → 视口坐标 (0~1)
Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);

// 射线检测：从屏幕点击位置发射射线
Ray ray = cam.ScreenPointToRay(Input.mousePosition);
if (Physics.Raycast(ray, out RaycastHit hit))
{
    Debug.Log($"Hit: {hit.point}");
}
```

---

## 7. Transform 的常用方法

```csharp
// 移动
transform.Translate(Vector3.forward * speed * Time.deltaTime);  // 相对自身方向移动
transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.World);  // 相对世界方向

// 旋转
transform.Rotate(Vector3.up, 90 * Time.deltaTime);     // 绕 Y 轴旋转
transform.RotateAround(center.position, Vector3.up, speed * Time.deltaTime);  // 绕某点旋转

// 缩放
transform.localScale += Vector3.one * Time.deltaTime;  // 均匀放大

// 坐标轴对齐
transform.LookAt(target);  // 让 Z 轴指向目标
```

---

## 练习：行星公转系统

```csharp
using UnityEngine;

public class Orbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform center;           // 围绕的中心
    public float orbitSpeed = 30f;     // 公转速度（度/秒）
    public float orbitRadius = 5f;     // 轨道半径
    public float selfRotateSpeed = 60f; // 自转速度

    private float angle = 0;

    void Update()
    {
        // 公转：绕中心旋转
        angle += orbitSpeed * Time.deltaTime;

        // 用三角函数计算轨道位置
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * orbitRadius,
            0,
            Mathf.Sin(rad) * orbitRadius
        );
        transform.position = center.position + offset;

        // 自转
        transform.Rotate(Vector3.up, selfRotateSpeed * Time.deltaTime);
    }
}

// 太阳系结构：
// Sun (中心)
//  ├── Earth (Orbit 脚本：center=Sun, radius=5)
//  │     └── Moon (Orbit 脚本：center=Earth, radius=1)
//  └── Venus (Orbit 脚本：center=Sun, radius=3)
```

---

---

## C++/Raylib 对照总结

| Raylib (C++) | Unity (C#) | 说明 |
|-------------|-----------|------|
| 手动 Vector2/3 | `transform.position` | Unity 自动管理位置 |
| 手动旋转累加 | `transform.rotation` / `Quaternion` | 四元数防万向锁 |
| 手动缩放 | `transform.localScale` | 局部缩放 |
| — | 父子层级 `parent` / `GetChild()` | Raylib 无对应概念 |
| 手动矩阵运算 | `TransformPoint` / `InverseTransformPoint` | 坐标空间转换 |
| — | `Camera.WorldToScreenPoint` | 3D→2D 投影变换 |
| 手动帧率计时 | `Time.deltaTime` | 引擎提供帧时间 |

## 停靠点

> Transform = 每个 GameObject 必备的组件，管理位置/旋转/缩放和父子层级。
> `position` = 世界坐标，`localPosition` = 相对父对象的偏移。
> 四元数避免万向锁——用 `Quaternion.Euler()` 直观创建，用 `Quaternion.Slerp()` 平滑插值。
> Transform 有 Dirty 标志：多次修改后统一读取更高效。
> 父子关系形成矩阵乘法链——父动子跟着动。

