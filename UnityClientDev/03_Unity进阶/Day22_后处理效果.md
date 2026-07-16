# Day 22：后处理效果 — Bloom、色调映射、SSAO、运动模糊

## 0. 前言：什么是后处理？

后处理（Post-Processing）是在**渲染完成后**对画面进行的图像处理。

```
渲染流程：
1. 渲染 3D 场景 → Color Buffer（颜色缓冲）
2. 后处理效果 → 修改 Color Buffer
3. 输出到屏幕

常见的后处理：
- Bloom（泛光）：明亮物体发光
- 色调映射（Tonemapping）：HDR → LDR
- SSAO（屏幕空间环境光遮蔽）：角落变暗
- 运动模糊：快速移动时的模糊
- 景深（Depth of Field）：聚焦效果
```

---

## 1. Bloom（泛光）

### 核心思想

```
1. 提取亮部区域（亮度 > 阈值）
2. 对亮部进行模糊
3. 叠加到原图上

效果：明亮物体（太阳、灯光）产生光晕
```

### Bloom Shader 实现

```hlsl
// Step 1: 提取亮部
fixed4 fragExtractBright (v2f i) : SV_Target
{
    fixed4 col = tex2D(_MainTex, i.uv);
    float brightness = dot(col.rgb, float3(0.2126, 0.7152, 0.0722));  // 亮度
    float threshold = 0.8;
    
    // 只保留亮部
    float contribution = max(0, brightness - threshold);
    return col * (contribution / max(brightness, 0.001));
}

// Step 2: 高斯模糊
static const float weights[5] = {0.227, 0.195, 0.122, 0.054, 0.016};

fixed4 fragBlur (v2f i) : SV_Target
{
    float2 texelSize = 1.0 / _ScreenParams.xy;
    fixed4 col = tex2D(_MainTex, i.uv) * weights[0];
    
    for (int j = 1; j < 5; ++j)
    {
        col += tex2D(_MainTex, i.uv + float2(texelSize.x * j, 0)) * weights[j];
        col += tex2D(_MainTex, i.uv - float2(texelSize.x * j, 0)) * weights[j];
    }
    
    return col;
}

// Step 3: 叠加
fixed4 fragBloomComposite (v2f i) : SV_Target
{
    fixed4 original = tex2D(_MainTex, i.uv);
    fixed4 bloom = tex2D(_BloomTex, i.uv);
    
    return original + bloom * _BloomIntensity;
}
```

### Unity Post-Processing Stack

```csharp
// 使用 Unity 官方 Post-Processing Stack
// 1. 安装：Window → Package Manager → Post Processing
// 2. 创建 Volume：GameObject → Volume → Global Volume
// 3. 添加 Bloom：Add Override → Post-processing → Bloom

// 代码控制
using UnityEngine.Rendering.PostProcessing;

public class BloomController : MonoBehaviour
{
    public PostProcessVolume volume;
    private Bloom bloom;
    
    void Start()
    {
        volume.profile.TryGetSettings(out bloom);
    }
    
    void Update()
    {
        bloom.intensity.value = 2.0f + Mathf.Sin(Time.time) * 0.5f;
    }
}
```

---

## 2. 色调映射（Tonemapping）

### 核心思想

```
HDR（高动态范围）：颜色值可以 > 1.0（如太阳亮度可能是 100）
LDR（低动态范围）：屏幕只能显示 0~1

色调映射将 HDR 值压缩到 LDR，同时保留视觉细节
```

### 常见色调映射算法

```hlsl
// Reinhard 色调映射
float3 TonemapReinhard(float3 color)
{
    return color / (color + float3(1.0, 1.0, 1.0));
}

// ACES 色调映射（电影级）
float3 TonemapACES(float3 color)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return saturate((color * (a * color + b)) / (color * (c * color + d) + e));
}

// Filmic 色调映射
float3 TonemapFilmic(float3 color)
{
    float3 x = max(float3(0,0,0), color - 0.004);
    return (x * (6.2 * x + 0.5)) / (x * (6.2 * x + 1.7) + 0.06);
}

// Gamma 校正
float3 GammaCorrect(float3 color)
{
    return pow(color, float3(1.0/2.2, 1.0/2.2, 1.0/2.2));
}
```

### 完整后处理 Pass

```hlsl
fixed4 fragTonemap (v2f i) : SV_Target
{
    float3 color = tex2D(_MainTex, i.uv).rgb;
    
    // 曝光
    color *= _Exposure;
    
    // 色调映射
    color = TonemapACES(color);
    
    // Gamma 校正
    color = GammaCorrect(color);
    
    return fixed4(color, 1.0);
}
```

---

## 3. SSAO（Screen-Space Ambient Occlusion）

### 核心思想

```
模拟环境光遮蔽：角落、缝隙处接收的环境光更少，所以更暗

算法：
1. 从深度缓冲重建世界位置
2. 在法线方向随机采样周围像素
3. 检查采样点是否被遮挡
4. 被遮挡越多 → 越暗
```

### SSAO Shader 实现（简化版）

```hlsl
// SSAO 采样核
static const float3 sampleKernel[64] = { /* 预计算的半球采样点 */ };

float fragSSAO (v2f i) : SV_Target
{
    // 从深度缓冲重建世界位置
    float depth = SampleDepth(i.uv);
    float3 worldPos = ReconstructWorldPos(i.uv, depth);
    float3 normal = SampleNormal(i.uv);
    
    // 随机旋转矩阵
    float3 randomVec = tex2D(_RandomTex, i.uv * _NoiseScale).xyz;
    float3x3 TBN = float3x3(randomVec, normal, cross(randomVec, normal));
    
    // 采样周围点
    float occlusion = 0.0;
    for (int j = 0; j < 64; ++j)
    {
        // 将采样点变换到世界空间
        float3 samplePos = mul(TBN, sampleKernel[j]);
        samplePos = worldPos + samplePos * _Radius;
        
        // 投影到屏幕空间
        float2 sampleUV = WorldToScreenUV(samplePos);
        float sampleDepth = SampleDepth(sampleUV);
        
        // 检查是否被遮挡
        float rangeCheck = smoothstep(0.0, 1.0, _Radius / abs(depth - sampleDepth));
        occlusion += (sampleDepth >= samplePos.z ? 1.0 : 0.0) * rangeCheck;
    }
    
    occlusion = 1.0 - (occlusion / 64.0);
    return occlusion;
}
```

### Unity SSAO 设置

```csharp
// 使用 Unity Post-Processing Stack
// Add Override → Post-processing → Ambient Occlusion

// 参数说明：
// Intensity：AO 强度
// Radius：采样半径
// Sample Count：采样数量（越多越精确，越慢）
// AO Radius：AO 影响范围
```

---

## 4. 运动模糊（Motion Blur）

### 核心思想

```
根据像素的运动速度，在运动方向上模糊

算法：
1. 计算每个像素的速度（屏幕空间）
2. 沿速度方向多次采样并平均
```

### 运动模糊 Shader 实现

```hlsl
fixed4 fragMotionBlur (v2f i) : SV_Target
{
    // 采样速度缓冲
    float2 velocity = tex2D(_VelocityTex, i.uv).rg;
    
    // 沿速度方向采样
    fixed4 col = tex2D(_MainTex, i.uv);
    float2 texelSize = 1.0 / _ScreenParams.xy;
    
    const int SAMPLES = 16;
    for (int j = 1; j < SAMPLES; ++j)
    {
        float2 offset = velocity * (float(j) / float(SAMPLES - 1) - 0.5);
        col += tex2D(_MainTex, i.uv + offset);
    }
    
    return col / float(SAMPLES);
}
```

### 速度缓冲生成

```hlsl
// 从 MVP 矩阵计算速度
float2 CalculateVelocity(float4 currentPos, float4 prevPos)
{
    // 投影到屏幕空间
    float2 currentNDC = currentPos.xy / currentPos.w;
    float2 prevNDC = prevPos.xy / prevPos.w;
    
    // 速度 = 当前位置 - 上一帧位置
    return (currentNDC - prevNDC) * 0.5;
}
```

---

## 5. 景深（Depth of Field）

### 核心思想

```
模拟相机镜头的聚焦效果：
- 聚焦点清晰
- 远离聚焦点的区域模糊

CoC（Circle of Confusion）：模糊圆的大小
- 距离聚焦点越远 → CoC 越大 → 越模糊
```

### 景深 Shader 实现

```hlsl
fixed4 fragDOF (v2f i) : SV_Target
{
    float depth = SampleDepth(i.uv);
    float focalDistance = _FocalDistance;
    float focalRange = _FocalRange;
    
    // 计算 CoC
    float coc = saturate(abs(depth - focalDistance) / focalRange);
    
    // 多级模糊（根据 CoC 选择模糊级别）
    float4 col = float4(0,0,0,0);
    if (coc < 0.2)
        col = tex2D(_MainTex, i.uv);
    else if (coc < 0.5)
        col = tex2D(_BlurTex1, i.uv);
    else
        col = tex2D(_BlurTex2, i.uv);
    
    return col;
}
```

---

## 6. C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity 后处理 |
|------|-------------|-------------|
| 后处理 | 手动渲染到纹理 + Shader | Post-Processing Stack |
| Bloom | 手动实现 | Volume + Bloom |
| 色调映射 | 无 | Tonemapping |
| SSAO | 无 | Ambient Occlusion |
| 运动模糊 | 无 | Motion Blur |
| 景深 | 无 | Depth of Field |

## 停靠点

> Bloom：提取亮部 → 模糊 → 叠加，产生光晕效果。
> 色调映射：HDR → LDR，常用 ACES 电影级算法。
> SSAO：屏幕空间环境光遮蔽，角落变暗增加真实感。
> 运动模糊：根据像素速度沿方向模糊。
> 景深：聚焦点清晰，远离聚焦点模糊。

## 练习建议

1. **实现 Bloom**：对比不同阈值和强度的效果
2. **色调映射对比**：ACES vs Reinhard vs Filmic
3. **配置 Post-Processing Stack**：调整 SSAO 参数
