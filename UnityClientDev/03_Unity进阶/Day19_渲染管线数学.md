# Day 19：渲染管线数学 — MVP 矩阵推导与空间转换

## 0. 前言：为什么数学很重要？

在 Raylib 中，你可能直接传坐标：

```cpp
// Raylib：直接用屏幕坐标
DrawRectangle(100, 100, 200, 150, RED);
```

但 GPU 需要理解**三维空间**，这就需要线性代数。理解这些数学，你才能：
- 手写 MVP 变换
- 理解 Shadow Map 为什么有效
- 实现自定义投影效果

---

## 1. 四个坐标空间

```
物体空间 (Object Space)
    ↓ 模型矩阵 (Model)
世界空间 (World Space)
    ↓ 观察矩阵 (View)
观察空间 (View/Camera Space)
    ↓ 投影矩阵 (Projection)
裁剪空间 (Clip Space)
    ↓ 透视除法
NDC 空间 (Normalized Device Coordinates)
    ↓ 视口变换
屏幕空间 (Screen Space)
```

### 各空间详解

```
物体空间：
- 顶点的原始坐标（建模时定义的）
- 每个物体有自己的原点和朝向
- 例：角色的头部相对于角色中心 (0, 1.8, 0)

世界空间：
- 所有物体共享的统一坐标系
- 物体被放置在世界中的某个位置
- 例：角色在世界坐标 (100, 0, 50)

观察空间（相机空间）：
- 以相机为原点
- 相机朝向 -Z 轴（右手坐标系）或 +Z 轴（左手，Unity）
- 用于计算"相机看到了什么"

裁剪空间：
- 透视除法前的齐次坐标
- 超出 [-1, 1] 范围的会被裁剪（剔除）
- 由投影矩阵变换得到

NDC 空间：
- 归一化设备坐标
- X/Y/Z 范围都是 [-1, 1]（OpenGL）或 X/Y: [-1,1], Z: [0,1]（DirectX/Unity）
- 透视除法：xyz/w

屏幕空间：
- 最终的像素坐标
- X: [0, 宽度], Y: [0, 高度]
```

---

## 2. 齐次坐标与 4x4 矩阵

### 为什么需要 4x4 矩阵？

3x3 矩阵可以表示旋转、缩放、剪切，但**无法表示平移**。

齐次坐标用 `(x, y, z, w)` 表示 3D 点：
- `w = 1`：表示一个点（可以平移）
- `w = 0`：表示一个方向（不受平移影响）

```
3x3 矩阵能做的：
┌         ┐   ┌   ┐     ┌           ┐
│ r00 r01 r02 │   │ x │     │ r00x+r01y+r02z │
│ r10 r11 r12 │ × │ y │  =  │ r10x+r11y+r12z │
│ r20 r21 r22 │   │ z │     │ r20x+r21y+r22z │
└         ┘   └   ┘     └           ┘
（只能旋转、缩放，不能平移）

4x4 矩阵能做的：
┌             ┐   ┌   ┐     ┌                    ┐
│ r00 r01 r02 tx │   │ x │     │ r00x+r01y+r02z+tx │
│ r10 r11 r12 ty │ × │ y │  =  │ r10x+r11y+r12z+ty │
│ r20 r21 r22 tz │   │ z │     │ r20x+r21y+r22z+tz │
│ 0   0   0  1  │   │ 1 │     │       1            │
└             ┘   └   ┘     └                    ┘
（平移 tx, ty, tz 被包含在最后一列）
```

---

## 3. 模型矩阵 (Model Matrix)

将顶点从**物体空间**变换到**世界空间**。

### 平移矩阵 (Translation)

```
T(tx, ty, tz) =
┌             ┐
│ 1   0   0  tx│
│ 0   1   0  ty│
│ 0   0   1  tz│
│ 0   0   0  1 │
└             ┘

点 (x, y, z, 1) × T = (x+tx, y+ty, z+tz, 1) ✓
方向 (x, y, z, 0) × T = (x, y, z, 0)          ✓（方向不受平移影响）
```

### 旋转矩阵 (Rotation)

绕 X 轴旋转 θ：

```
Rx(θ) =
┌             ┐
│ 1   0       0     0│
│ 0   cos(θ) -sin(θ) 0│
│ 0   sin(θ)  cos(θ) 0│
│ 0   0       0     1│
└             ┘
```

绕 Y 轴旋转 θ：

```
Ry(θ) =
┌              ┐
│ cos(θ)  0  sin(θ) 0│
│ 0       1  0      0│
│-sin(θ)  0  cos(θ) 0│
│ 0       0  0      1│
└              ┘
```

绕 Z 轴旋转 θ：

```
Rz(θ) =
┌             ┐
│ cos(θ) -sin(θ) 0 0│
│ sin(θ)  cos(θ) 0 0│
│ 0       0      1 0│
│ 0       0      0 1│
└             ┘
```

### 缩放矩阵 (Scale)

```
S(sx, sy, sz) =
┌             ┐
│ sx  0   0  0│
│ 0   sy  0  0│
│ 0   0   sz 0│
│ 0   0   0  1│
└             ┘
```

### 完整模型矩阵

```
M = T × R × S

变换顺序（从右到左）：
1. 先缩放 (S)
2. 再旋转 (R)
3. 最后平移 (T)
```

---

## 4. 观察矩阵 (View Matrix)

将顶点从**世界空间**变换到**观察空间**（以相机为原点）。

### 相机参数

```
相机由三个向量定义：
- Position（位置）：相机在世界中的位置
- Target（目标点）：相机看向的点
- Up（上方向）：通常为 (0, 1, 0)

从这三个向量，可以构建：
- Forward = normalize(Target - Position)  （相机前方）
- Right = normalize(cross(Forward, Up))   （相机右方）
- Up' = cross(Right, Forward)             （修正后的上方向）
```

### 观察矩阵推导

```
观察矩阵 = 旋转 × 平移

旋转矩阵（将相机对齐到坐标轴）：
┌           ┐
│ Rx  Ry  Rz 0│
│            1│  （3x3 部分将相机轴对齐到世界轴）
└           ┘

平移矩阵（将相机移到原点）：
T(-Position)

完整观察矩阵 V = R × T(-Position)
```

### Unity 内置函数

```hlsl
// Unity 提供的观察矩阵
float4x4 UNITY_MATRIX_V;  // 观察矩阵

// 手动计算
float3 forward = normalize(target - position);
float3 right = normalize(cross(forward, worldUp));
float3 up = cross(right, forward);

float4x4 viewMatrix = {
    right.x,    up.x,    -forward.x, 0,
    right.y,    up.y,    -forward.y, 0,
    right.z,    up.z,    -forward.z, 0,
    -dot(right, position), -dot(up, position), dot(forward, position), 1
};
```

---

## 5. 投影矩阵 (Projection Matrix)

将顶点从**观察空间**变换到**裁剪空间**。

### 透视投影（Perspective Projection）

用于 3D 游戏，产生近大远小的效果。

```
透视投影矩阵：
┌                                        ┐
│  2n/(r-l)      0        (r+l)/(r-l)    0 │
│      0      2n/(t-b)    (t+b)/(t-b)    0 │
│      0         0      -(f+n)/(f-n)  -2fn/(f-n)│
│      0         0           -1          0 │
└                                        ┘

其中：
n = 近裁剪面距离
f = 远裁剪面距离
l, r = 左右边界
t, b = 上下边界

简化版（对称视锥体）：
┌                            ┐
│  n/aspect   0     0        0 │
│      0      n     0        0 │
│      0      0  -(f+n)/(f-n) -2fn/(f-n)│
│      0      0     -1        0 │
└                            ┘

aspect = 宽度/高度
```

### 正交投影（Orthographic Projection）

用于 2D 游戏或 UI，没有透视效果。

```
正交投影矩阵：
┌                                    ┐
│  2/(r-l)      0         0    -(r+l)/(r-l) │
│      0     2/(t-b)      0    -(t+b)/(t-b) │
│      0        0     -2/(f-n)  -(f+n)/(f-n)│
│      0        0         0         1       │
└                                    ┘
```

---

## 6. 透视除法与 NDC

```
裁剪空间坐标 (x, y, z, w)
        ↓ 除以 w
NDC 坐标 (x/w, y/w, z/w)

透视除法后：
- X, Y, Z 范围变为 [-1, 1]（OpenGL）或 X,Y:[-1,1], Z:[0,1]（Unity/DX）
- 超出此范围的顶点会被裁剪（丢弃）
- w 分量存储了深度信息，用于深度测试
```

---

## 7. Unity 内置变换矩阵

```hlsl
// Unity 提供的全局变量
uniform float4x4 UNITY_MATRIX_M;      // 模型矩阵
uniform float4x4 UNITY_MATRIX_V;      // 观察矩阵
uniform float4x4 UNITY_MATRIX_P;      // 投影矩阵
uniform float4x4 UNITY_MATRIX_MV;     // Model × View
uniform float4x4 UNITY_MATRIX_MVP;    // Model × View × Projection
uniform float4x4 UNITY_MATRIX_VP;     // View × Projection
uniform float4x4 UNITY_MATRIX_I_M;    // 模型矩阵的逆矩阵
uniform float4x4 UNITY_MATRIX_I_V;    // 观察矩阵的逆矩阵
uniform float4x4 UNITY_MATRIX_I_P;    // 投影矩阵的逆矩阵

// 常用变换函数
float4 UnityObjectToClipPos(float4 pos)  // 物体→裁剪
float3 UnityObjectToWorldPos(float4 pos) // 物体→世界
float3 UnityObjectToWorldNormal(float3 normal) // 法线变换
float4 UnityWorldToClipPos(float4 pos)   // 世界→裁剪
float3 UnityWorldToViewPos(float4 pos)   // 世界→观察
```

---

## 8. C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity (HLSL) |
|------|-------------|--------------|
| 坐标系 | 屏幕坐标 (2D) | 世界坐标 (3D) |
| 投影 | 无（直接像素） | `UNITY_MATRIX_P` |
| 视图 | `Camera` 结构体 | `UNITY_MATRIX_V` |
| 模型变换 | `DrawModelEx()` | `UNITY_MATRIX_M` |
| MVP 变换 | 手动传矩阵 | `mul(UNITY_MATRIX_MVP, v.vertex)` |
| 视口大小 | `GetScreenWidth()` | `_ScreenParams.xy` |

## 停靠点

> 四个空间：物体 → 世界 → 观察 → 裁剪 → NDC → 屏幕。
> 齐次坐标 `(x,y,z,w)`：`w=1` 是点，`w=0` 是方向。
> 模型矩阵 = T × R × S（先缩放、再旋转、最后平移）。
> 投影矩阵：透视（近大远小）、正交（无透视）。
> Unity 内置：`UNITY_MATRIX_MVP`、`UnityObjectToClipPos()`。

## 练习建议

1. **手动构建观察矩阵**：给定相机位置和目标点，计算 View 矩阵
2. **验证透视除法**：打印顶点的 `w` 分量，理解深度存储
3. **实现 oblique near-plane clipping**：修改投影矩阵，让近裁剪面与任意平面对齐（用于水下效果）
