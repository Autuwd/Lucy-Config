# Day 21：阴影技术进阶 — PCF、CSM、VSM 与优化

## 0. 前言：为什么需要学阴影算法？

Day12 我们学了 Shadow Map 的基础原理。今天深入**工程实践**中常用的阴影优化技术。

---

## 1. Shadow Map 回顾与问题

### 基本原理

```
1. 从光源渲染场景 → 深度缓冲（Shadow Map）
2. 渲染主相机时，将像素转换到光源空间
3. 比较深度：如果当前像素深度 > Shadow Map 中的深度 → 在阴影中
```

### 常见问题

```
1. 走样（Aliasing）：
   - Shadow Map 分辨率不够 → 锯齿状阴影边缘
   - 一个像素对应 Shadow Map 中多个纹素

2. 彼得·潘现象（Peter Panning）：
   - 阴影与物体分离
   - 原因：深度偏移（Bias）太大

3. 阴影痤疮（Shadow Acne）：
   - 物体表面出现条纹
   - 原因：深度偏移太小或没有

4. 阴影闪烁（Shadow Flickering）：
   - 阴影边缘不稳定
   - 原因：光源视角的投影矩阵不稳定
```

---

## 2. PCF（Percentage-Closer Filtering）

### 核心思想

不是用单点采样，而是**多次采样取平均**，产生软阴影边缘：

```hlsl
// PCF 软阴影
float ShadowPCF(float4 shadowCoord, float bias)
{
    float shadow = 0.0;
    float2 texelSize = 1.0 / _ShadowMapTexture_TexelSize.xy;
    
    // 3x3 卷积核
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            float2 offset = float2(x, y) * texelSize;
            float shadowDepth = tex2D(_ShadowMapTexture, shadowCoord.xy + offset).r;
            shadow += shadowCoord.z - bias > shadowDepth ? 0.0 : 1.0;
        }
    }
    
    return shadow / 9.0;  // 平均
}
```

### 优化：泊松圆盘采样

```hlsl
// 泊松圆盘采样（更自然的软阴影）
static const float2 poissonDisk[16] = 
{
    float2(-0.94201624, -0.39906216),
    float2( 0.94558609, -0.76890725),
    float2(-0.09418410, -0.92938870),
    float2( 0.34495938,  0.29387760),
    float2(-0.91588581,  0.45776236),
    float2(-0.81544232, -0.87912464),
    float2(-0.38277543,  0.27676845),
    float2( 0.97484398,  0.75648379),
    float2( 0.44323325, -0.97511554),
    float2( 0.53742981, -0.47373420),
    float2(-0.26496911, -0.41893023),
    float2( 0.79197514,  0.19090188),
    float2(-0.24188840,  0.99706507),
    float2(-0.81409955,  0.91437590),
    float2( 0.19984126,  0.78641367),
    float2( 0.14383161, -0.14100790)
};

float ShadowPCFPoisson(float4 shadowCoord, float bias)
{
    float shadow = 0.0;
    float2 texelSize = 1.0 / _ShadowMapTexture_TexelSize.xy;
    
    for (int i = 0; i < 16; ++i)
    {
        float2 offset = poissonDisk[i] * texelSize * 2.0;
        float shadowDepth = tex2D(_ShadowMapTexture, shadowCoord.xy + offset).r;
        shadow += shadowCoord.z - bias > shadowDepth ? 0.0 : 1.0;
    }
    
    return shadow / 16.0;
}
```

---

## 3. CSM（Cascaded Shadow Maps）

### 核心思想

将视锥体分成多个级联，近处用高精度，远处用低精度：

```
视锥体分割：
┌─────────────────────────────────────┐
│  Near      │      │      │     Far  │
│  Cascade 0 │  C1  │  C2  │   C3    │
│  (高精度)  │      │      │ (低精度) │
└─────────────────────────────────────┘

每个级联有独立的 Shadow Map：
- Cascade 0：覆盖 0~10m，高分辨率
- Cascade 1：覆盖 10~30m，中分辨率
- Cascade 2：覆盖 30~60m，低分辨率
- Cascade 3：覆盖 60~100m，最低分辨率
```

### Unity 中的 CSM 设置

```csharp
// Unity 内置支持 CSM
// Quality Settings → Shadow Cascades: 2 或 4
// Shadow Cascade 2 Split: 近/远分割点（如 0.33, 0.66）
// Shadow Cascade 4 Split: 四个分割点

// 代码获取级联信息
void OnRenderObject()
{
    // 获取当前相机的级联矩阵
    Matrix4x4[] cascadeMatrices = new Matrix4x4[4];
    for (int i = 0; i < 4; i++)
    {
        cascadeMatrices[i] = GL.GetUnity扭曲MatrixVP(i);
    }
}
```

### CSM Shader 实现

```hlsl
// CSM 阴影采样
#define MAX_CASCADES 4

float4 _CascadeSplits;  // 级联分割距离
float4x4 _CascadeMatrices[MAX_CASCADES];  // 每个级联的光源 VP 矩阵

float GetCascadeIndex(float viewDepth)
{
    for (int i = 0; i < MAX_CASCADES; ++i)
    {
        if (viewDepth < _CascadeSplits[i])
            return i;
    }
    return MAX_CASCADES - 1;
}

float ShadowCSM(float3 worldPos, float viewDepth)
{
    int cascadeIndex = GetCascadeIndex(viewDepth);
    
    // 转换到对应级联的光源空间
    float4 shadowCoord = mul(_CascadeMatrices[cascadeIndex], float4(worldPos, 1.0));
    
    // 采样 Shadow Map
    return ShadowPCF(shadowCoord);
}
```

---

## 4. VSM（Variance Shadow Maps）

### 核心思想

用**统计方法**存储深度信息，支持任意滤波：

```
传统 Shadow Map 存储：深度值
VSM 存储：(深度, 深度²) → 用于计算均值和方差

利用切比雪夫不等式：
P(x ≥ t) ≤ σ² / (σ² + (t - μ)²)

其中：
μ = 深度均值
σ² = 深度方差
t = 当前深度
```

### VSM Shader 实现

```hlsl
// VSM 输出（从光源渲染时）
struct VSMOutput
{
    float4 depth : SV_Target0;   // (深度, 深度², 0, 0)
};

VSMOutput fragVSM(v2f i)
{
    VSMOutput o;
    float depth = i.linearDepth;
    o.depth = float4(depth, depth * depth, 0, 0);
    return o;
}

// VSM 阴影计算（从主相机渲染时）
float ShadowVSM(float4 shadowCoord, float minVariance)
{
    // 采样 (深度, 深度²)
    float2 moments = tex2D(_ShadowMapTexture, shadowCoord.xy).rg;
    
    // 计算均值和方差
    float mean = moments.x;
    float meanSq = moments.y;
    float variance = max(meanSq - mean * mean, minVariance);
    
    // 切比雪夫不等式
    float d = shadowCoord.z - mean;
    float probability = variance / (variance + d * d);
    
    // 平滑过渡
    float probabilityMax = smoothstep(0.2, 1.0, probability);
    
    return probabilityMax;
}
```

### VSM 的优点

```
1. 支持任意滤波（模糊、双线性插值）
2. 无锯齿（软阴影边缘自然）
3. 可复用（同一张图可用于多光源）
4. 支持运动模糊（累积帧间阴影）
```

---

## 5. 阴影优化技巧

### 深度偏移（Depth Bias）

```hlsl
// 避免阴影痤疮
float bias = max(0.005 * (1.0 - dot(N, L)), 0.001);
float shadow = shadowCoord.z - bias > shadowDepth ? 0.0 : 1.0;

// 自适应偏移
float bias = 0.005 * tan(acos(dot(N, L)));
```

### 阴影距离裁剪

```csharp
// 只渲染近处物体的阴影
QualitySettings.shadowDistance = 50f;  // 超过 50m 不渲染阴影
```

### 阴影分辨率优化

```csharp
// 根据光源类型设置分辨率
light.shadowResolution = LightShadowResolution.High;  // 方向光
light.shadowResolution = LightShadowResolution.Medium; // 点光源
```

### 阴影剔除

```csharp
// 只让需要阴影的物体渲染 Shadow Map
renderer.shadowCastingMode = ShadowCastingMode.On;      // 投射阴影
renderer.shadowCastingMode = ShadowCastingMode.Off;     // 不投射
renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly; // 只渲染阴影
```

---

## 6. C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity 阴影系统 |
|------|-------------|---------------|
| 阴影实现 | 手写 Shadow Map | 内置 Shadow Map |
| 软阴影 | 手动 PCF | PCF / VSM |
| 级联阴影 | 无 | CSM（内置） |
| 深度偏移 | 手动计算 | Bias 参数 |
| 阴影距离 | 无 | Shadow Distance |

## 停靠点

> PCF：多次采样取平均，产生软阴影边缘。
> CSM：视锥体分割，近处高精度，远处低精度。
> VSM：统计方法存储深度，支持任意滤波。
> 深度偏移：避免阴影痤疮，自适应偏移更稳定。
> 阴影优化：分辨率、距离、剔除。

## 练习建议

1. **实现 PCF**：对比 3x3 和 5x5 卷积核的软阴影效果
2. **配置 CSM**：调整级联分割点，观察远处阴影质量变化
3. **VSM 对比**：对比传统 Shadow Map 和 VSM 的阴影边缘
