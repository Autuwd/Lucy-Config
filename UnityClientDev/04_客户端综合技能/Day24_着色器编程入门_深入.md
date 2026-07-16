# Day 24：着色器编程入门 — 自定义渲染管线、GPU 实例化、计算着色器与性能分析

## 0. 为什么需要这些高级渲染技术？

基础篇的 Shader 能实现溶解、描边、顶点动画。但这些是"已有的特效"。高级渲染技术解决的是"如何做出引擎没有的效果"和"如何让渲染跑得更快"：

```
基础 Shader 的局限：
1. 只能用引擎已有的渲染流程（前向渲染）
2. 一个 Pass 只能做一件事
3. 没有自定义的渲染顺序
4. 性能优化手段有限

高级渲染技术：
1. 自定义渲染 Pass → 在你想要的时候渲染你想要的
2. GPU 实例化 → 批量渲染 10 万棵草
3. 计算着色器 → GPU 上的通用计算（粒子、后处理）
4. 曲面细分 → 让低模变成高模
5. 渲染图 → 管理复杂渲染流程
```

---

## 1. Custom Render Pass（URP）

### 什么是自定义渲染 Pass？

```
URP 的渲染流程由一系列 Render Pass 组成：
Depth → Opaque → Transparent → Post-processing → UI

自定义 Render Pass = 在流程中间插入你自己的渲染步骤
比如：在透明物体之前渲染扫描线效果
```

### C# 侧实现

```csharp
using UnityEngine.Rendering.Universal;

// 自定义渲染 Pass
public class CustomRenderPass : ScriptableRenderPass
{
    private Material material;
    private RenderTargetIdentifier source;
    private RenderTargetHandle tempTexture;

    public CustomRenderPass(Material material)
    {
        this.material = material;
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        // ↑ 在不透明物体渲染之后执行
        tempTexture.Init("_TempRT");
    }

    // 设置渲染目标
    public void Setup(RenderTargetIdentifier source)
    {
        this.source = source;
    }

    // 执行渲染
    public override void Execute(ScriptableRenderContext context,
                                 ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("CustomPass");

        // 获取相机渲染目标
        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;

        // 创建临时 RT
        cmd.GetTemporaryRT(tempTexture.id, desc);

        // 把源 RT 拷贝到临时 RT
        cmd.Blit(source, tempTexture.id);

        // 用材质处理（Blit = 渲染一个全屏四边形）
        cmd.Blit(tempTexture.id, source, material, 0);

        // 释放临时 RT
        cmd.ReleaseTemporaryRT(tempTexture.id);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}

// 自定义渲染 Feature（负责把 Pass 添加到 URP）
public class CustomRenderFeature : ScriptableRendererFeature
{
    public Material effectMaterial;
    private CustomRenderPass renderPass;

    public override void Create()
    {
        renderPass = new CustomRenderPass(effectMaterial);
        renderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                         ref RenderingData renderingData)
    {
        if (effectMaterial != null)
        {
            renderPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(renderPass);
        }
    }
}
```

### 自定义 Pass 的 HLSL Shader

```hlsl
Shader "Custom/ScanlineEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScanlineCount ("Scanline Count", Float) = 100
        _ScanlineSpeed ("Speed", Float) = 1.0
        _Intensity ("Intensity", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        // 全屏 Pass（不写 Z，不读 Z）
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _ScanlineCount;
            float _ScanlineSpeed;
            float _Intensity;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // 扫描线效果
                float scanline = sin(input.uv.y * _ScanlineCount * UNITY_PI + _Time.y * _ScanlineSpeed);
                scanline = abs(scanline);
                color.rgb *= 1.0 - _Intensity * (1.0 - scanline);

                return color;
            }
            ENDHLSL
        }
    }
}
```

### 自定义 Pass 的应用场景

```
1. 全屏后处理效果
2. 自定义阴影渲染
3. 覆盖渲染（扫描线、噪点、故障效果）
4. 自定义光照模型
5. 视口分割效果（分屏）
```

---

## 2. Shader Variant 与 Keyword 系统

### 什么是 Shader Variant？

```
一个 Shader 中有很多 #pragma multi_compile 和 #pragma shader_feature
这些开关产生"变体"

Shader "Custom/MyShader" 包含：
- #pragma multi_compile _ ENABLE_FOG
- #pragma multi_compile _ USE_NORMAL_MAP
- #pragma multi_compile _ LIGHTMAP_ON

变体数量 = 2 × 2 × 2 = 8 种组合

如果这个数量失控 → 着色器变体爆炸！
编译时间翻倍、内存占用翻倍
```

### 变体管理

```hlsl
// 两种变体声明方式

// 1. shader_feature：不会自动收集变体
// 适合：材质面板上开关的选项
#pragma shader_feature _USE_BLUR _USE_GLOW

// 2. multi_compile：自动收集所有变体
// 适合：全局渲染功能开关
#pragma multi_compile _ ENABLE_FOG
#pragma multi_compile _ SHADOWS_SHADOWMAP

// 3. 按功能组管理
// URP 的方式：
// #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
// #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
```

### C# 侧控制 Variant

```csharp
public class ShaderVariantControl : MonoBehaviour
{
    public Material targetMaterial;

    void Start()
    {
        // 启用关键字
        targetMaterial.EnableKeyword("_USE_NORMAL_MAP");

        // 禁用关键字
        targetMaterial.DisableKeyword("_USE_NORMAL_MAP");

        // 检查关键字
        bool hasNormal = targetMaterial.IsKeywordEnabled("_USE_NORMAL_MAP");

        // 全局关键字（影响所有材质）
        Shader.EnableKeyword("ENABLE_FOG");
        Shader.DisableKeyword("ENABLE_FOG");
    }

    // 变体收集：在 Build 前需要 Preload Shader Variant
    // 方式：把用到的变体添加到 Shader Variant Collection
    public ShaderVariantCollection variantCollection;

    void BuildPreprocess()
    {
        // 运行时加载变体集合
        variantCollection.WarmUp();
        // ↑ 预编译变体，防止运行时卡顿
    }
}

// ShaderVariantCollection 的创建：
// 菜单 → Assets → Create → Shader Variant Collection
// 手动添加 "Shader + Keyword" 组合
// 或使用工具自动收集（如：Editor 中记录用到的变体）
```

### 变体数量控制最佳实践

```
1. 用 shader_feature 代替 multi_compile（如果只在材质上使用）
2. 多级功能按层拆分（不交叉的独立 multi_compile）
3. 用 ShaderVariantCollection 精确控制 Build 包含的变体
4. 开启 Stripping（Player Settings → Remove Unused Shaders）
5. 定期检查变体数量（Window → Shader Compilation → Show Variants）
```

---

## 3. GPU Instancing

### 为什么需要 Instancing？

```
同样的物体渲染 10000 次：
普通模式：10000 次 DrawCall（每次设置材质、矩阵...）
Instancing：1 次 DrawCall，GPU 展开为 10000 个

最典型的应用：草地、树木、粒子、石头
```

### Shader 中启用 GPU Instancing

```hlsl
Shader "Custom/InstancedShader"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // ↑ 这个 pragma 启用 GPU Instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                // ↑ 实例 ID（用于区分不同实例）
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                // ↑ 设置当前实例的 ID（重要！）

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                col *= _BaseColor;
                return col;
            }
            ENDHLSL
        }
    }
}
```

### C# 侧：Graphics.DrawMeshInstanced

```csharp
public class InstancedRendering : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int instanceCount = 10000;

    // 每个实例的变换矩阵
    private Matrix4x4[] matrices;
    // 每个实例的自定义数据（颜色、大小）
    private MaterialPropertyBlock propertyBlock;

    void Start()
    {
        matrices = new Matrix4x4[instanceCount];
        Vector4[] colors = new Vector4[instanceCount];

        for (int i = 0; i < instanceCount; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 20f,
                Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), 0),
                Vector3.one * Random.Range(0.5f, 1.5f)
            );
            colors[i] = Random.ColorHSV();
        }

        // 传递每实例数据
        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetVectorArray("_InstanceColor", colors);
    }

    void Update()
    {
        // 一次 DrawCall 渲染所有实例！
        Graphics.DrawMeshInstanced(
            mesh,
            0,          // submesh index
            material,
            matrices,
            instanceCount,
            propertyBlock
        );
    }
}
```

### 每实例数据（Shader 接收）

```hlsl
// 接收 C# 传来的每实例颜色
// 需要在 Shader 中添加：

UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
UNITY_INSTANCING_BUFFER_END(Props)

// 在 frag 中使用：
float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
```

---

## 4. 曲面细分（Tessellation）

### 核心概念

```
低模 → 曲面细分 → 高模

好处：GPU 动态增加顶点数，提高曲面质量
不需要真的做高模
远距离用低细分，近距离用高细分

Shade Model
     ↓
Hull Shader → 顶点数翻倍 → Domain Shader → 新的顶点位置
     ↑ 控制细分级别           ↑ 计算细分后顶点的位置
```

### Hull + Domain Shader 实现

```hlsl
Shader "Custom/TessellationExample"
{
    Properties
    {
        _TessFactor ("Tessellation", Range(1, 32)) = 4
        _Displacement ("Displacement", Range(0, 1)) = 0.1
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert        // 顶点着色器
            #pragma hull hull          // 外壳着色器
            #pragma domain domain      // 域着色器
            #pragma fragment frag      // 片元着色器

            // 需要细分控制点和输入
            struct TessellationControlPoint
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            // 顶点→控制点
            TessellationControlPoint vert(Attributes input)
            {
                TessellationControlPoint o;
                o.positionOS = input.positionOS;
                o.uv = input.uv;
                o.normalOS = input.normalOS;
                return o;
            }

            // Hull Shader 常量函数（定义细分级别）
            struct TessellationFactors
            {
                float edge[3] : SV_TessFactor;  // 三条边的细分
                float inside : SV_InsideTessFactor; // 内部细分
            };

            TessellationFactors hullConstant(InputPatch<TessellationControlPoint, 3> patch)
            {
                TessellationFactors f;
                f.edge[0] = _TessFactor;
                f.edge[1] = _TessFactor;
                f.edge[2] = _TessFactor;
                f.inside = _TessFactor;
                return f;
            }

            // Hull Shader
            [domain("tri")]           // 三角形域
            [partitioning("integer")] // 整数细分
            [outputtopology("triangle_cw")]  // 顺时针三角形
            [outputcontrolpoints(3)]   // 每个 patch 3 个控制点
            [patchconstantfunc("hullConstant")]
            TessellationControlPoint hull(InputPatch<TessellationControlPoint, 3> patch,
                                          uint id : SV_OutputControlPointID)
            {
                return patch[id];
            }

            // Domain Shader（计算细分后的顶点位置）
            [domain("tri")]
            Varyings domain(TessellationFactors factors,
                           OutputPatch<TessellationControlPoint, 3> patch,
                           float3 barycentric : SV_DomainLocation)
            {
                Varyings output;

                // 重心坐标插值
                float3 pos = patch[0].positionOS * barycentric.x
                           + patch[1].positionOS * barycentric.y
                           + patch[2].positionOS * barycentric.z;

                float2 uv = patch[0].uv * barycentric.x
                          + patch[1].uv * barycentric.y
                          + patch[2].uv * barycentric.z;

                float3 normal = patch[0].normalOS * barycentric.x
                              + patch[1].normalOS * barycentric.y
                              + patch[2].normalOS * barycentric.z;

                // 位移贴图（沿法线方向偏移顶点）
                float displacement = tex2Dlod(_DisplacementMap, float4(uv, 0, 0)).r;
                pos += normal * displacement * _Displacement;

                output.positionCS = TransformObjectToHClip(pos);
                output.uv = uv;
                output.normalWS = TransformObjectToWorldNormal(normal);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return col;
            }
            ENDHLSL
        }
    }
}
```

### 细分适用于什么？

```
✅ 适合：
- 地形：低模变高模 + 视口距离控制
- 角色肢体：平滑手肘/膝盖
- 置换贴图：石头、地面凹凸

❌ 不适合：
- 硬表面（墙体、箱子）— 不需要细分
- 远处物体 — 看不到细分效果
- 骨骼动画角色 — 细分会破坏蒙皮
```

---

## 5. 计算着色器（Compute Shader）

### 核心概念

```
普通 Shader（顶点/片元）：
只在渲染管线的固定位置执行

计算着色器：
GPU 上的通用计算工具
不涉及渲染，就是算数（线程化！
```

### Compute Shader 基础

```hlsl
// 文件：NewComputeShader.compute

#pragma kernel ParticleUpdate

// 结构体
struct Particle
{
    float3 position;
    float3 velocity;
    float life;
    float maxLife;
};

// 缓冲区（CPU 传入/传出数据）
RWStructuredBuffer<Particle> _Particles;
float _DeltaTime;
float _Gravity;

// 内核（每个线程处理一个粒子）
[numthreads(64, 1, 1)]  // 每个线程组 64 个线程
void ParticleUpdate(uint3 id : SV_DispatchThreadID)
{
    uint index = id.x;  // 当前线程 ID = 粒子索引

    Particle p = _Particles[index];

    // 更新物理
    p.velocity.y -= _Gravity * _DeltaTime;
    p.position += p.velocity * _DeltaTime;

    // 生命周期
    p.life -= _DeltaTime;
    if (p.life <= 0)
    {
        // 重置
        p.position = float3(0, 0, 0);
        p.velocity = float3(sin(index), 5, cos(index));
        p.life = p.maxLife;
    }

    _Particles[index] = p;
}
```

### C# 侧调度

```csharp
public class ComputeParticleSystem : MonoBehaviour
{
    public ComputeShader computeShader;
    public Mesh particleMesh;
    public Material particleMaterial;
    public int particleCount = 100000;

    private ComputeBuffer particleBuffer;
    private ComputeBuffer argsBuffer;
    private int kernelHandle;

    // 粒子数据
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
        public float maxLife;
    }

    void Start()
    {
        // 初始化粒子
        Particle[] particles = new Particle[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            particles[i] = new Particle
            {
                position = Random.insideUnitSphere * 5f,
                velocity = Random.insideUnitSphere * 3f,
                life = Random.Range(1f, 3f),
                maxLife = 3f
            };
        }

        // 创建 GPU 缓冲区
        particleBuffer = new ComputeBuffer(particleCount, sizeof(float) * 8);
        particleBuffer.SetData(particles);

        // 设置 Compute Shader 参数
        kernelHandle = computeShader.FindKernel("ParticleUpdate");
        computeShader.SetBuffer(kernelHandle, "_Particles", particleBuffer);
        computeShader.SetFloat("_Gravity", 9.81f);

        // GPU Instancing 渲染参数
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint),
            ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = particleMesh.GetIndexCount(0);
        args[1] = (uint)particleCount;
        argsBuffer.SetData(args);
    }

    void Update()
    {
        // 更新粒子（GPU 上并行执行）
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        int threadGroups = Mathf.CeilToInt(particleCount / 64f);
        computeShader.Dispatch(kernelHandle, threadGroups, 1, 1);

        // 渲染粒子（GPU Instancing 直接从缓冲区读位置）
        particleMaterial.SetBuffer("_ParticleBuffer", particleBuffer);
        Graphics.DrawMeshInstancedIndirect(
            particleMesh, 0, particleMaterial,
            new Bounds(Vector3.zero, Vector3.one * 100),
            argsBuffer
        );
    }

    void OnDestroy()
    {
        particleBuffer?.Release();
        argsBuffer?.Release();
    }
}

// 接收 ComputeBuffer 的 Shader
// Shader "Custom/ParticleSurface"
// Properties { }
// SubShader {
//   Pass {
//     HLSLPROGRAM
//     #pragma vertex vert
//     #pragma fragment frag
//     #pragma multi_compile_instancing
//
//     struct Particle {
//       float3 position;
//       float3 velocity;
//       float life;
//       float maxLife;
//     };
//     StructuredBuffer<Particle> _ParticleBuffer;
//
//     Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
//     {
//       Particle p = _ParticleBuffer[instanceID];
//       float3 worldPos = p.position;
//       ...
//     }
//   }
// }
```

### Compute Shader 应用场景

```
1. 粒子系统：10 万粒子全 GPU 更新
2. 物理模拟：布娃娃、布料、流体
3. 后处理：模糊、扭曲
4. 路径搜索：GPU 并行 A*
5. 草/头发渲染：分块计算
```

---

## 6. Frame Debugger 与 Shader 性能分析

### Frame Debugger 使用

```
Window → Analysis → Frame Debugger

功能：
1. 逐 DrawCall 检查（每个渲染事件）
2. 查看 Shader 代码（变体、关键字）
3. 分析渲染状态（Blend、ZTest、Cull）
4. 检查 Overdraw（屏幕像素被渲染次数）

使用步骤：
1. 打开 Frame Debugger
2. 点击 Enable
3. 拖动滑块看每个 DrawCall
4. 检查为什么有些物体渲染了多次
```

### 性能分析常见问题

```csharp
// 1. Overdraw 检查
// Frame Debugger 中切换到 "Overdraw" 视图
// 越红的区域 = 越多的 Overdraw

// 2. DrawCall 数量
// Stats 面板：Game View → Stats
// 目标：
// - 移动端 < 100 DrawCalls
// - PC < 1000 DrawCalls
// - VR < 200 DrawCalls

// 3. Shader 性能指标
// 在 Frame Debugger 中点击一个 DrawCall → Shader Properties
// 可以查看：
// - 顶点数
// - 三角形数
// - 使用的关键字
// - 着色器变体
```

### Shader 性能优化清单

```
1. 浮点精度
   float（32位）= 全精度，用于位置
   half（16位）= 半精度，用于颜色/UV
   fixed（11位）= 最低，用于简单颜色混合
   → 移动端用 half 代替 float 快 2 倍

2. 纹理采样
   SAMPLE_TEXTURE2D > tex2D（性能优化）
   减少纹理采样次数 > 多做几次运算

3. 分支
   在 GPU 上分支会导致"线程束发散"
   同一 warp（32 个线程）走不同分支 → 等待
   → 用 step/lerp 替代 if-else

4. 数学运算
   mul < dot/cross < sin/cos < pow < exp < log
   → 预计算能预计算的，用查找表替代复杂函数

5. 带宽
   寄存器数量影响能同时处理的线程数
   → 减少 varyings（从 VS 传到 FS 的数据）
   → 用 half 替代 float 传颜色/UV
```

### Profiler 中的 Shader 指标

```
在 Unity Profiler 中（Window → Analysis → Profiler）：

Rendering Profiler：
- SetPass Calls：材质切换次数（越少越好）
- Draw Calls：渲染次数
- Triangles：三角形数
- Batches：批次数（合并的 DrawCall）

GPU Profiler（需要 ADB / 真机）：
- GPU Time：GPU 花费的总时间
- Vertex Time：顶点着色器时间
- Fragment Time：片元着色器时间
```

---

## 7. C++/Raylib 对比

| 概念 | C++/Raylib | Unity/C# |
|------|-----------|----------|
| 自定义 Pass | glBegin/End / FBO | ScriptableRenderPass |
| Shader 变体 | 无（直接 GLSL 编译） | #pragma multi_compile |
| GPU Instancing | glDrawArraysInstanced | Graphics.DrawMeshInstanced |
| Tessellation | GLSL Tess Shader | Hull + Domain Shader |
| Compute Shader | GLSL Compute Shader | .compute 文件 |
| 帧调试 | RenderDoc | Frame Debugger / RenderDoc |
| 性能分析 | 自实现计时器 | Profiler / GPU Profiler |

**Raylib 的 Shader 更简单（裸 GLSL），Unity 的 ShaderLab 提供更多功能但更复杂。GPU Instancing 和 Compute Shader 在概念上完全一致。**

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Custom Render Pass | 在 URP 渲染流程中插入自定义步骤 |
| Shader Variant | 关键字组合产生的 Shader 变体 |
| GPU Instancing | 一次 DrawCall 渲染多个相同物体 |
| Hull Shader | 细分阶段 1：告诉 GPU 怎么细分 |
| Domain Shader | 细分阶段 2：计算细分后顶点的位置 |
| Compute Shader | GPU 上的通用计算，不限于渲染 |
| 线程组 | [numthreads(64,1,1)] — 每组 64 线程 |
| ComputeBuffer | CPU ↔ GPU 数据传输缓冲区 |
| Frame Debugger | 逐 DrawCall 检查渲染状态 |
| Overdraw | 像素被多次渲染，越红越浪费 |

**对比 C++/Raylib：** Unity 的渲染管线封装更高级，但底层概念一致。Frame Debugger 是 Unity 的优势——C++ 中你需要 RenderDoc 或 Nsight 做同样的分析。Compute Shader 的概念不限于 Unity，GLSL 和 HLSL 的计算着色器逻辑相同。
