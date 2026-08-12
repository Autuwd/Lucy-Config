# 第 2 章 Transform 与坐标系

> **本章管辖**：位置/旋转/缩放 → `Transform`，以及世界坐标 vs 本地坐标、父子层级、坐标转换。
> **一句话**：**每个** GameObject 身上都必挂一个 `Transform`（第一个组件，无法删除），它决定了物体在哪、朝哪、多大。
> **前置**：了解 [第 1 章](第01章_核心物件体系.md) 的 GameObject/Component。

---

## 功能速查表（功能 → API）

| 我想… | 用这个 |
|-------|--------|
| 移动物体 | `transform.position` 直接赋值 / `Translate` (增量) |
| 绕轴转 | `transform.Rotate` / `transform.rotation` |
| 缩放大/小 | `transform.localScale` / `Scale` |
| 面向某点/某方向 | `LookAt` / `LookRotation` |
| 设父子关系 | `SetParent` / `parent` |
| 按路径找子物体 | `transform.Find(path)` |
| 世界坐标↔本地坐标 | `TransformPoint` / `InverseTransformPoint` |
| 方向转换 | `TransformDirection` / `InverseTransformDirection` |
| 非等比缩放的安全置换 | `TransformVector` |

---

## 2.1 `class Transform : Component`

**【是什么】** Unity 最常用的组件，存储**局部**位置/旋转/缩放，并维护与父物体的层级关系。**每个 GameObject 有且仅有一个 Transform，不能删除。**

**【用途】** 你操作物体的位置、旋转、缩放；处理父子关系；在局部/世界坐标系之间换算。

**【核心属性】**

| 属性 | 含义 | 世界 or 局部 |
|------|------|-------------|
| `position` | 世界坐标位置 | **世界** |
| `localPosition` | 相对父物体的位置 | **局部** |
| `rotation` | 世界旋转（四元数） | **世界** |
| `localRotation` | 相对父旋转 | **局部** |
| `localScale` | 相对父缩放 | **局部** |
| `eulerAngles` | 旋转（欧拉角常见于开销小但易万向锁） | 世界 |
| `localEulerAngles` | 局部欧拉角 | 局部 |

【⚠️ 注意】**为什么不要直接用 `gameObject.transform.position.x = 5`**：因为 `position` 返回的是 `Vector3`（值类型），直接改 `.x` 是改一个临时副本，不会生效。**务必整体赋值：**

```csharp
// ❌ 编译错误！CS1612「无法修改非变量表达式返回值」
transform.position.x = 5;            // position 是值类型（Vector3），.x 改的是临时副本，编译都不让过

// ✅
Vector3 p = transform.position;      
p.x = 5;
transform.position = p;               // 整体赋值才生效

// ✅ 或一行
transform.position += new Vector3(5, 0, 0);
```

---

## 2.2 位置相关 API

### 2.2.1 `position` / `localPosition`

```csharp
transform.position    // Vector3：世界坐标（无父=等于 localPosition）
transform.localPosition // Vector3：相对父
```
- **世界 vs 局部**：`localPosition` 是相对父物体的坐标；`position` 是"绝对的世界坐标"。二者在**没有父物体**时相等。
- 用**局部**操作子物体（游戏里摆子物品），用**世界**做全局运算（如角色在世界中移动）。

---

### 2.2.2 `Translate(Vector3 translation, Space relativeTo = Space.Self)`

**【是什么】** 沿指定方向移动物体（原地平移）。

**【参数说明】**
- `translation`：位移量（每帧一次的增量）。
- `relativeTo`：`Space.Self`（本地，沿自身轴）或 `Space.World`（世界轴）。

**【代码示例】**
```csharp
transform.Translate(0, 0, speed * Time.deltaTime);                 // 沿自身 Z 轴前进
transform.Translate(speed, 0, 0, Space.World);                     // 沿世界 X 轴
transform.Translate(Vector3.forward * speed * Time.deltaTime);     // 向前
```

【⚠️ 注意】真正物理移动用 `Rigidbody`（见第 5 章），不要用 `Transform.Translate` 直接操作有 `Rigidbody` 的物体。

---

## 2.3 旋转相关 API

### 2.3.1 `rotation` / `localRotation`（Quaternion 四元数）

```csharp
transform.rotation = Quaternion.Euler(0, 90, 0);   // ✅ 转 90 度常用写法
```

- `rotation`：世界旋转；`localRotation`：局部。
- 赋值用 **Quaternion**（不能直接赋 Vector3！）。

---

### 2.3.2 `Rotate(Vector3 eulers, Space relativeTo = Space.Self)`

**【是什么】** 相对旋转（每帧增量旋转）。
```csharp
// Rotate(Vector3.up, 90*dt)：第一个参数是旋转轴指示器，第二是角度（度）
// 不传 Space 时默认 Space.Self，绕**自身本地 Y 轴**（不是世界 Y 轴）
transform.Rotate(Vector3.up, 90 * Time.deltaTime);               // 绕本地Y轴每秒90度
transform.Rotate(0, 10, 0, Space.Self);                          // 绕自身y轴
transform.Rotate(0, 10, 0, Space.World);                         // 绕世界y轴
```

> 轴重载说明：`Rotate(Vector3 axis, float angle, Space relativeTo)` 才是"绕指定轴转过 angle 度"。示例里 `Vector3.up` 作为 axis 传入，在 `Space.Self` 下绕的是当地 y 轴。

【面试常考】`Rotate(归一化的轴, 角度)`：第一个参数是旋转轴向量，第二个是角度（度）。

---

### 2.3.3 `LookAt(Transform/Vector3 target, Vector3 up = Vector3.up)`

**【是什么】** 让物体**的 Z 轴朝向目标**（常用于敌人面向玩家、摄像头看目标）。
```csharp
transform.LookAt(player.transform);
```

**【参数】** target：目标（世界位置或物体）；可选 `up`：保持向上的轴向。

---

### 2.3.4 `Quaternion.LookRotation(Vector3 forward, Vector3 upwards)`（静态）

**【是什么】** 用两个方向向量生成一个"面向该方向的旋转四元数"。与 `transform.LookAt` 类似但不改自己，而是返回值让你赋值/叠加。
```csharp
Vector3 dir = targetPos - transform.position;
Quaternion rot = Quaternion.LookRotation(dir);
transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5);
```

**相似区别：**
| | `LookAt` | `LookRotation` |
|--|----------|----------------|
| 作用 | 直接让物体转向目标 | 返回一个四元数，你来赋值 |
| 用感 | 一步转向 | 可配合 Slerp 平滑转向 |

---

## 2.4 缩放

### 2.4.1 `localScale (Vector3)`

```csharp
transform.localScale = new Vector3(2, 2, 2);    // 放大2倍
```
【⚠️ 注意】只有 `localScale`，没有 `scale` 属性。父缩放会被继承，局部缩放是"相对父"的。

---

## 2.5 父子关系

### 2.5.1 `parent` 属性 & `SetParent(Transform parent, bool worldPositionStays = true)`

```csharp
transform.SetParent(parentGo.transform);              // 同理但是否保留世界位置由参数决定
transform.parent = parentGo.transform;               // 直接赋值（较少用）

// 挂载时保持世界位置：
transform.SetParent(parent, true);    // 世界位置不变（默认）
transform.SetParent(parent, false);   // 局部位置不变
```

**【参数说明】**
- `parent`：新的父物体 Transform。
- `worldPositionStays`（默认 `true`）：若为 true，**世界坐标保持不变**（只是把父子关系挂上）；若 false，**局部坐标不变**（物体会随父移动改变世界位置）。

【⚠️ 注意】`SetParent(null)` 会脱离父物体，成为根级物体。

---

### 2.5.2 `Find(string path)` — 按路径找子物体

```csharp
Transform child = transform.Find("Body/Head");    // 相对本物体找子物体，支持斜杠路径
```

**【返回】** 找到则返回子 `Transform`；没有返回 `null`。
**【区别】** `transform.Find` 找**子物体**，速度快；`GameObject.Find` 找整个场景，慢。

---

### 2.5.3 `childCount` & `GetChild(int i)`

```csharp
int n = transform.childCount;                    // 子物体数量
Transform first = transform.GetChild(0);         // 第0个子物体
for(int i=0;i<transform.childCount;i++)
{
    Transform c = transform.GetChild(i);
    // ...
}
```

---

## 2.6 坐标/方向转换（世界 ↔ 局部）

这些方法把"点/向量/方向"在**物体自身坐标系**与**世界坐标系**之间做变换。最容易混的一堆，务必集中记忆：

| 方法 | 含义（局部→世界 或 世界→局部） | 典型用途 |
|------|--------------------------------|----------|
| `TransformPoint(Vector3 local)` | 局部坐标点 → 世界坐标点 | 拿物体局部某个点在世界里的位置 |
| `InverseTransformPoint(Vector3 world)` | 世界 → 局部点 | 把世界坐标变成"相对物体"的坐标 |
| `TransformDirection` | 局部**方向** → 世界（不缩放） | 局部前进方向转世界 |
| `InverseTransformDirection` | 世界方向 → 局部方向 |  |
| `TransformVector` | 局部**带缩放的向量** → 世界 | 带缩放的位移转换 |
| `InverseTransformVector` | 世界带缩放向量 → 局部 |  |

**【代码示例：把局部点转世界】**
```csharp
// 物体的局部坐标 (1,0,0) 在世界坐标系中的位置
Vector3 worldPos = transform.TransformPoint(new Vector3(1,0,0));
// 鼠标屏幕的世界点转换到局部（常用于在物体上点击检测）
Vector3 local = transform.InverseTransformPoint(worldClickPos);
```

**【相似区别：TransDirection vs TransformVector】**
- `TransformDirection`：只转换方向（忽略缩放、偏移），不改变向量长度。
- `TransformVector`：转换"带距离的向量"（考虑缩放），保「矢量真实大小」。

---

## 2.7 其他常用函数

- `DetachChildren()`：把所有子物体变成根级（取消父关系）。
- `SetAsFirstSibling()/SetAsLastSibling()/SetSiblingIndex()`：调整兄弟顺序（UI/渲染顺序相关）。

---

## 2.8 高频对比总表

| 想做的事 | 用这个 | 备注 |
|---------|--------|------|
| 世界位置 | `transform.position` | 没有直接 `.x=` 谨慎 |
| 局部位置 | `transform.localPosition` | 有父时和 position 不同 |
| 世界旋转 | `transform.rotation` | 四元数 |
| 局部旋转 | `transform.localRotation` |  |
| 可直接看的角度 | `transform.eulerAngles` | 有万向锁风险 |
| 平移 | `Translate` | 物理的可移动用 Rigidbody |
| 指向目标 | `LookAt`（自身转向） | 要旋转四元数用 `Quaternion.LookRotation` |
| 设定父层 | `SetParent` | `parent` 直接赋值 |

> 下一章接上：第 3 章会详解这里用到的 `Vector3` `Quaternion` `Euler` 这些数学类型。
