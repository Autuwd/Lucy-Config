# Day 23：计算着色器 — GPU 通用计算入门

## 0. 前言：GPU 不只是用来渲染

传统 Shader 只做渲染（顶点→像素）。计算着色器（Compute Shader）让你用 GPU 做**通用计算**：

```
传统渲染管线：
顶点着色器 → 光栅化 → 片元着色器 → 输出像素

计算着色器：
独立于渲染管线
输入数据 → GPU 并行计算 → 输出数据
```

### GPU 并行的优势

```
CPU：4~16 核心，适合复杂逻辑
GPU：数千个核心，适合简单重复计算

示例：处理 100 万个粒子
CPU：循环 100 万次（串行）
GPU：1000 个核心 × 1000 次（并行）
```

---

## 1. 计算着色器基础

### 基本结构

```hlsl
// MyCompute.compute
#pragma kernel CSMain

// 输入/输出
RWTexture2D<float4> Result;  // 读写纹理
StructuredBuffer<float4> Input;  // 只读缓冲

[numthreads(8, 8, 1)]  // 线程组大小
void CSMain (uint3 id : SV_DispatchThreadID)
{
    // id 是每个线程的唯一 ID
    // id.x, id.y, id.z 对应 3D 网格中的位置
    
    // 简单示例：输出红色
    Result[id.xy] = float4(1, 0, 0, 1);
}
```

### 线程模型

```
线程组（Thread Group）：
- [numthreads(8, 8, 1)] 定义每个组的线程数
- 通常 X×Y 是 64 或 256（GPU warp/wavefront 大小的倍数）
- Z 通常为 1（3D 计算时可用）

Dispatch：CPU 端调度线程组
- dispatch(16, 16, 1) → 16×16 = 256 个线程组
- 总线程数 = 256 × 8×8×1 = 16384 个线程

线程 ID：
SV_DispatchThreadID = groupID × numthreads + groupThreadID
```

---

## 2. CPU 端调度

### C# 调度代码

```csharp
using UnityEngine;

public class ComputeShaderExample : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture outputTexture;
    
    void Start()
    {
        // 创建输出纹理
        outputTexture = new RenderTexture(256, 256, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();
        
        // 获取 kernel 索引
        int kernel = computeShader.FindKernel("CSMain");
        
        // 绑定输出纹理
        computeShader.SetTexture(kernel, "Result", outputTexture);
        
        // 调度计算
        // 参数：kernel, x线程组数, y线程组数, z线程组数
        // 总线程数 = (x×8, y×8, z×1) = (32×8, 32×8, 1) = (256, 256, 1)
        computeShader.Dispatch(kernel, 32, 32, 1);
    }
    
    void OnDestroy()
    {
        if (outputTexture != null)
            outputTexture.Release();
    }
}
```

---

## 3. 实战：图像处理

### 灰度图转换

```hlsl
// Grayscale.compute
#pragma kernel CSGrayscale

RWTexture2D<float4> Result;
Texture2D<float4> Input;  // 只读纹理
SamplerState sampler_Input;

[numthreads(8, 8, 1)]
void CSGrayscale (uint3 id : SV_DispatchThreadID)
{
    float4 color = Input[id.xy];
    
    // 灰度公式
    float gray = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
    
    Result[id.xy] = float4(gray, gray, gray, color.a);
}
```

### 高斯模糊

```hlsl
// Blur.compute
#pragma kernel CSBlur

RWTexture2D<float4> Result;
Texture2D<float4> Input;

static const float weights[5] = {0.227, 0.195, 0.122, 0.054, 0.016};

[numthreads(8, 8, 1)]
void CSBlur (uint3 id : SV_DispatchThreadID)
{
    float2 texelSize = 1.0 / float2(Input.Length.x, Input.Length.y);
    float4 col = float4(0, 0, 0, 0);
    
    // 水平模糊
    for (int j = -4; j <= 4; ++j)
    {
        float2 offset = float2(j, 0) * texelSize;
        col += Input[id.xy + offset] * weights[abs(j)];
    }
    
    Result[id.xy] = col;
}
```

### 边缘检测

```hlsl
// EdgeDetect.compute
#pragma kernel CSEdgeDetect

RWTexture2D<float4> Result;
Texture2D<float4> Input;

[numthreads(8, 8, 1)]
void CSEdgeDetect (uint3 id : SV_DispatchThreadID)
{
    float2 texelSize = 1.0 / float2(Input.Length.x, Input.Length.y);
    
    // Sobel 算子
    float4 samples[9];
    int index = 0;
    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            samples[index++] = Input[id.xy + float2(x, y) * texelSize];
        }
    }
    
    // 水平梯度
    float3 gx = samples[2].rgb + 2.0 * samples[5].rgb + samples[8].rgb
              - samples[0].rgb - 2.0 * samples[3].rgb - samples[6].rgb;
    
    // 垂直梯度
    float3 gy = samples[6].rgb + 2.0 * samples[7].rgb + samples[8].rgb
              - samples[0].rgb - 2.0 * samples[1].rgb - samples[2].rgb;
    
    // 边缘强度
    float3 edge = sqrt(gx * gx + gy * gy);
    
    Result[id.xy] = float4(edge, 1.0);
}
```

---

## 4. 实战：粒子系统

### 粒子数据结构

```hlsl
// 粒子结构
struct Particle
{
    float3 position;
    float3 velocity;
    float4 color;
    float life;
    float maxLife;
};
```

### 粒子更新 Compute Shader

```hlsl
// ParticleUpdate.compute
#pragma kernel CSUpdate

RWStructuredBuffer<Particle> Particles;

float deltaTime;
float3 gravity;
float turbulence;

[numthreads(256, 1, 1)]
void CSUpdate (uint3 id : SV_DispatchThreadID)
{
    Particle p = Particles[id.x];
    
    // 更新生命
    p.life -= deltaTime;
    
    if (p.life > 0)
    {
        // 物理更新
        p.velocity += gravity * deltaTime;
        
        // 添加湍流
        p.velocity += turbulence * (float3(sin(p.position.x), cos(p.position.y), sin(p.position.z)) * deltaTime);
        
        // 更新位置
        p.position += p.velocity * deltaTime;
        
        // 更新颜色（根据生命值插值）
        float lifeRatio = p.life / p.maxLife;
        p.color = lerp(float4(1,0,0,1), float4(1,1,0,1), lifeRatio);  // 红→黄
    }
    else
    {
        // 重置粒子
        p.position = float3(0, 0, 0);
        p.velocity = float3(rand(id.x) - 0.5, rand(id.x + 1) * 2, rand(id.x + 2) - 0.5);
        p.life = p.maxLife;
    }
    
    Particles[id.x] = p;
}
```

### C# 调度粒子更新

```csharp
public class GPUParticleSystem : MonoBehaviour
{
    public ComputeShader particleCompute;
    public Material particleMaterial;
    
    private ComputeBuffer particleBuffer;
    private int particleCount = 100000;
    
    void Start()
    {
        // 初始化粒子数据
        Particle[] particles = new Particle[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            particles[i] = new Particle
            {
                position = Random.insideUnitSphere,
                velocity = Random.insideUnitSphere,
                color = Color.white,
                life = Random.Range(1f, 3f),
                maxLife = 3f
            };
        }
        
        // 创建缓冲区
        particleBuffer = new ComputeBuffer(particleCount, sizeof(float) * 11);
        particleBuffer.SetData(particles);
    }
    
    void Update()
    {
        int kernel = particleCompute.FindKernel("CSUpdate");
        
        particleCompute.SetBuffer(kernel, "Particles", particleBuffer);
        particleCompute.SetFloat("deltaTime", Time.deltaTime);
        particleCompute.SetVector("gravity", new Vector4(0, -9.8f, 0, 0));
        
        // 调度：每组 256 个线程，共 100000/256 ≈ 391 组
        particleCompute.Dispatch(kernel, particleCount / 256, 1, 1);
        
        // 渲染粒子
        particleMaterial.SetBuffer("Particles", particleBuffer);
        Graphics.DrawProcedural(particleMaterial, 
            new Bounds(Vector3.zero, Vector3.one * 1000), 
            MeshTopology.Points, particleCount);
    }
    
    void OnDestroy()
    {
        particleBuffer?.Release();
    }
}
```

---

## 5. 实战：流体模拟（简介）

### Navier-Stokes 方程（简化）

```
流体模拟基于 Navier-Stokes 方程：
1. 对流（Advection）：速度场移动密度场
2. 扩散（Diffusion）：密度/速度扩散
3. 压力（Pressure）：不可压缩条件
4. 外力（External Forces）：重力、风力等
```

### 简化版流体 Compute Shader

```hlsl
// FluidSim.compute
#pragma kernel CSAdvect
#pragma kernel CSDiffuse

RWTexture2D<float> Density;
RWTexture2D<float2> Velocity;

float deltaTime;
float diffusion;

[numthreads(8, 8, 1)]
void CSAdvect (uint3 id : SV_DispatchThreadID)
{
    float2 vel = Velocity[id.xy];
    float2 prevPos = float2(id.xy) - vel * deltaTime;
    
    Density[id.xy] = Density[uint2(prevPos)];
}

[numthreads(8, 8, 1)]
void CSDiffuse (uint3 id : SV_DispatchThreadID)
{
    float center = Density[id.xy];
    float left = Density[id.xy + uint2(-1, 0)];
    float right = Density[id.xy + uint2(1, 0)];
    float up = Density[id.xy + uint2(0, 1)];
    float down = Density[id.xy + uint2(0, -1)];
    
    float laplacian = left + right + up + down - 4.0 * center;
    Density[id.xy] += diffusion * laplacian * deltaTime;
}
```

---

## 6. C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity Compute Shader |
|------|-------------|---------------------|
| GPU 计算 | 无内置支持 | Compute Shader |
| 纹理处理 | CPU 端操作 | GPU 并行处理 |
| 粒子系统 | CPU 循环 | GPU 10 万+ 粒子 |
| 图像处理 | CPU 逐像素 | GPU 并行卷积 |
| 流体模拟 | 无 | GPU 并行求解 |

## 停靠点

> Compute Shader：独立于渲染管线的 GPU 通用计算。
> 线程模型：`[numthreads(x,y,z)]` 定义线程组，`Dispatch` 调度。
> 常用场景：图像处理、粒子系统、流体模拟、物理模拟。
> 性能关键：数千核心并行，适合简单重复计算。

## 练习建议

1. **图像处理**：实现高斯模糊、边缘检测
2. **粒子系统**：实现 10 万粒子 GPU 粒子系统
3. **性能对比**：CPU vs GPU 处理 100 万次计算
