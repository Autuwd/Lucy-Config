# Day 24：渲染管线实战 — 自定义 SRP、Render Graph、延迟渲染

## 0. 前言：为什么要自定义渲染管线？

Day12 我们学了 Unity 的三种渲染管线。今天学习如何**自己写渲染管线**：

```
自定义 SRP 的好处：
1. 完全控制渲染流程
2. 针对特定需求优化
3. 学习渲染管线的底层原理
4. 实现特殊效果（如风格化渲染）
```

---

## 1. SRP（Scriptable Render Pipeline）基础

### 最简单的 SRP

```csharp
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipeline : RenderPipelineAsset
{
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipelineInstance();
    }
}

public class CustomRenderPipelineInstance : RenderPipeline
{
    // 渲染每个相机
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            RenderSingleCamera(context, camera);
        }
    }
    
    void RenderSingleCamera(ScriptableRenderContext context, Camera camera)
    {
        // 1. 设置相机属性
        context.SetupCameraProperties(camera);
        
        // 2. 清除渲染目标
        CommandBuffer cmd = new CommandBuffer { name = "Clear" };
        cmd.ClearRenderTarget(true, true, camera.backgroundColor);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();
        
        // 3. 剔除
        if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams))
            return;
        
        CullingResults cullingResults = context.Cull(ref cullingParams);
        
        // 4. 排序
        SortingSettings sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        
        DrawingSettings drawingSettings = new DrawingSettings(
            new ShaderTagId("SRPDefaultUnlit"),  // Shader Pass 名称
            sortingSettings
        );
        
        FilteringSettings filteringSettings = new FilteringSettings(
            RenderQueueRange.opaque
        );
        
        // 5. 绘制不透明物体
        context.DrawRenderers(
            cullingResults,
            ref drawingSettings,
            ref filteringSettings
        );
        
        // 6. 绘制天空盒
        context.DrawSkybox(camera);
        
        // 7. 绘制透明物体
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        
        context.DrawRenderers(
            cullingResults,
            ref drawingSettings,
            ref filteringSettings
        );
        
        // 8. 提交
        context.Submit();
    }
}
```

---

## 2. Render Graph（渲染图）

### 核心思想

```
Render Graph 将渲染流程表示为有向无环图（DAG）：

节点（Pass）：渲染操作（如 Draw Opaque、Post Process）
边（Edge）：数据依赖（如 Color Buffer、Depth Buffer）

优点：
1. 自动资源管理（临时 RT 自动释放）
2. 自动排序（按依赖关系）
3. 易于扩展（添加新 Pass 不影响现有代码）
4. 性能优化（自动合并 Pass、延迟创建资源）
```

### Render Graph 实现

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class CustomRenderGraph : ScriptableRendererFeature
{
    class OpaquePass : ScriptableRenderPass
    {
        private RenderGraphHandle depthHandle;
        private RenderGraphHandle colorHandle;
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // 声明渲染目标
            var depthDesc = cameraTextureDescriptor;
            depthDesc.depthBufferBits = 32;
            
            // 创建临时 RT（由 Render Graph 管理）
            depthHandle = UniversalRenderer.CreateRenderGraphHandle(
                TextureHandle.Create(depthDesc)
            );
            
            colorHandle = UniversalRenderer.CreateRenderGraphHandle(
                TextureHandle.Create(cameraTextureDescriptor)
            );
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Opaque Pass");
            
            // 设置渲染目标
            var depthTex = GetTextureFromHandle(depthHandle);
            var colorTex = GetTextureFromHandle(colorHandle);
            
            cmd.SetRenderTarget(colorTex, depthTex);
            cmd.ClearRenderTarget(true, true, Color.clear);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 设置渲染目标（可选）
        }
    }
    
    OpaquePass m_OpaquePass;
    
    public override void Create()
    {
        m_OpaquePass = new OpaquePass();
        m_OpaquePass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_OpaquePass);
    }
}
```

---

## 3. 延迟渲染（Deferred Rendering）

### 核心思想

```
前向渲染（Forward）：
- 每个物体：渲染几何 → 计算光照
- 100 个物体 × 10 个光源 = 1000 次光照计算

延迟渲染（Deferred）：
- Pass 1：只渲染几何，存储到 G-Buffer
- Pass 2：用 G-Buffer 计算光照
- 1 个全屏 Pass 计算所有光源
- 复杂度：10 个光源 = 10 次光照计算

延迟渲染更适合多光源场景
```

### G-Buffer 布局

```
G-Buffer 包含：

RT0：RGB = Albedo，A = Metallic
RT1：RGB = World Normal，A = 未使用
RT2：RGB = Emission，A = 未使用
RT3：RGB = 自定义数据，A = 未使用
Depth：深度缓冲

每个像素存储足够的信息用于光照计算
```

### 延迟渲染 Shader

```hlsl
// G-Buffer Pass（渲染几何）
struct GBufferOutput
{
    float4 albedo : SV_Target0;   // RT0
    float4 normal : SV_Target1;   // RT1
    float4 emission : SV_Target2; // RT2
    float4 data : SV_Target3;     // RT3
};

GBufferOutput fragGBuffer (v2f i)
{
    GBufferOutput o;
    
    // 采样纹理
    float4 albedo = tex2D(_MainTex, i.uv) * _Color;
    float3 normal = normalize(i.worldNormal);
    float3 emission = _Emission.rgb;
    
    o.albedo = albedo;
    o.normal = float4(normal * 0.5 + 0.5, 1.0);  // 编码到 [0,1]
    o.emission = float4(emission, 1.0);
    o.data = float4(_Metallic, _Smoothness, 0, 0);
    
    return o;
}
```

### 光照 Pass（全屏）

```hlsl
// 光照计算 Pass
struct LightData
{
    float3 position;
    float3 color;
    float range;
    float intensity;
};

StructuredBuffer<LightData> _Lights;

float4 fragLighting (v2f i) : SV_Target
{
    // 从 G-Buffer 采样
    float4 albedo = tex2D(_GBuffer0, i.uv);
    float3 normal = tex2D(_GBuffer1, i.uv).rgb * 2.0 - 1.0;
    float3 emission = tex2D(_GBuffer2, i.uv).rgb;
    float2 data = tex2D(_GBuffer3, i.uv).rg;
    float metallic = data.x;
    float smoothness = data.y;
    
    // 重建世界位置
    float depth = SampleDepth(i.uv);
    float3 worldPos = ReconstructWorldPos(i.uv, depth);
    
    // 计算光照
    float3 lightAccumulation = float3(0, 0, 0);
    
    for (int j = 0; j < _LightCount; j++)
    {
        LightData light = _Lights[j];
        
        float3 lightDir = light.position - worldPos;
        float distance = length(lightDir);
        lightDir = normalize(lightDir);
        
        // 衰减
        float attenuation = saturate(1.0 - distance / light.range);
        attenuation *= attenuation;
        
        // 漫反射
        float NdotL = saturate(dot(normal, lightDir));
        
        // 高光（简化 Blinn-Phong）
        float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
        float3 halfDir = normalize(lightDir + viewDir);
        float NdotH = saturate(dot(normal, halfDir));
        float spec = pow(NdotH, smoothness * 256.0);
        
        float3 diffuse = albedo.rgb * light.color * NdotL * attenuation;
        float3 specular = light.color * spec * attenuation;
        
        lightAccumulation += diffuse + specular;
    }
    
    // 最终颜色
    float3 color = lightAccumulation + emission;
    
    return float4(color, 1.0);
}
```

---

## 4. 延迟渲染的优化

### Light Culling（光源剔除）

```csharp
// 只计算影响当前像素的光源
// 方法1：基于屏幕空间的光源索引
// 方法2：Tiled/Clustered 延迟渲染

// Tiled 延迟渲染：
// 将屏幕分成 16x16 的 tile
// 每个 tile 计算哪些光源影响它
// 只计算这些光源的光照
```

### Clustered 延迟渲染

```hlsl
// 3D 空间划分（视锥体 + 深度分层）
struct Cluster
{
    uint lightStart;
    uint lightCount;
};

StructuredBuffer<Cluster> _Clusters;
StructuredBuffer<uint> _LightIndices;

float3 CalculateLighting(float3 worldPos, float3 normal, float3 albedo, float metallic, float smoothness)
{
    // 找到对应的 cluster
    int3 clusterCoord = GetClusterCoord(worldPos);
    Cluster cluster = _Clusters[clusterCoord.x + clusterCoord.y * _ClusterX + clusterCoord.z * _ClusterX * _ClusterY];
    
    float3 lightAccumulation = float3(0, 0, 0);
    
    for (uint i = 0; i < cluster.lightCount; i++)
    {
        uint lightIndex = _LightIndices[cluster.lightStart + i];
        LightData light = _Lights[lightIndex];
        
        // 计算该光源的贡献
        lightAccumulation += CalculateSingleLight(light, worldPos, normal, albedo, metallic, smoothness);
    }
    
    return lightAccumulation;
}
```

---

## 5. 风格化渲染示例

### 卡通渲染（Toon Shading）

```hlsl
// 卡通着色器
fixed4 fragToon (v2f i) : SV_Target
{
    float3 N = normalize(i.worldNormal);
    float3 L = normalize(_WorldSpaceLightPos0.xyz);
    
    // NdotL 离散化
    float NdotL = dot(N, L);
    float steps = 3.0;
    float toon = floor(NdotL * steps) / steps;
    toon = saturate(toon);
    
    // 颜色
    fixed4 col = tex2D(_MainTex, i.uv) * _Color;
    col.rgb *= toon;
    
    return col;
}
```

### 色调分离（Posterization）

```hlsl
// 色调分离
fixed4 fragPosterize (v2f i) : SV_Target
{
    fixed4 col = tex2D(_MainTex, i.uv);
    
    float levels = 8.0;
    col.rgb = floor(col.rgb * levels) / levels;
    
    return col;
}
```

---

## 6. C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity SRP |
|------|-------------|-----------|
| 渲染循环 | `BeginDrawing/EndDrawing` | RenderPipeline.Render() |
| 渲染目标 | 手动管理 FBO | CommandBuffer + RT |
| 光照计算 | CPU 端 | GPU Shader |
| 渲染架构 | 线性流程 | Render Graph（DAG） |
| 延迟渲染 | 无 | G-Buffer + 光照 Pass |
| 风格化渲染 | 手动实现 | 自定义 Shader |

## 停靠点

> SRP：用 C# 控制渲染流程，`ScriptableRenderContext` 是核心 API。
> Render Graph：将渲染表示为 DAG，自动资源管理和排序。
> 延迟渲染：先存几何信息到 G-Buffer，再统一计算光照。
> Clustered 渲染：3D 空间划分，高效处理大量光源。
> 风格化渲染：利用自定义 Shader 实现卡通、色调分离等效果。

## 练习建议

1. **最小 SRP**：实现只有 Clear + Draw 的最简单管线
2. **G-Buffer 调试**：可视化 G-Buffer 的每个通道
3. **延迟渲染对比**：对比前向和延迟在多光源下的性能
4. **卡通渲染**：实现 Toon Shader，添加描边效果
