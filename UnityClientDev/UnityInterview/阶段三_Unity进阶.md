### 快速入门指南（阶段3）
- 目标：深入理解渲染管线、材质与着色器、资源加载、对象池、协程、性能分析工具、DrawCall 优化与内存管理等。
- 学习路线：先查看对照学习点与练习入口（UnityInterview/逐条对应章节与练习模板.md），再完成阶段3 的对照练习。
- 对照入口：UnityInterview/逐条对应章节与练习模板.md
- 运行/验证：搭建一个简化场景，完成渲染相关练习（材质/着色/Instancing）与对象池/协程示例，使用 Profiler/Frame Debugger 观察性能。
- 最小化可运行示例：请查看 UnityInterview/Examples/Stage3/ObjectPoolLite.cs、CoroutineDemoLite.cs、MaterialToggleLite.cs 进行直接拷贝运行。

# 第三阶段：Unity 进阶

> 深入理解 Unity 渲染、资源管理和性能优化

---

## 配套视频教程

| 知识点 | 推荐视频 | 视频链接 |
|--------|----------|----------|
| 协程原理 | 【Unity主程面试经典】协程的核心原理详解 | [B站视频](https://www.bilibili.com/video/BV17a4ezCEf2/) |
| 协程详解 | Unity3D主程进阶：协程详解 | [B站视频](https://www.bilibili.com/video/BV1r44y1R7GJ/) |
| 渲染优化 | Unity 渲染管线与优化 | [B站视频](https://www.bilibili.com/video/BV1r44y1R7GJ/) |

> 更多视频教程请查看：[视频教程汇总.md](./视频教程汇总.md)

---

## 目录

- [3.1 渲染管线与材质](#31-渲染管线与材质)
- [3.2 光照与阴影](#32-光照与阴影)
- [3.3 Shader 基础](#33-shader-基础)
- [3.4 资源加载](#34-资源加载)
- [3.5 对象池技术](#35-对象池技术)
- [3.6 协程](#36-协程)
- [3.7 性能分析工具](#37-性能分析工具)
- [3.8 DrawCall 优化](#38-drawcall-优化)
- [3.9 内存管理](#39-内存管理)

---

## 3.1 渲染管线与材质

#### 核心概念解析

**什么是渲染管线？**
- 渲染管线是将 3D 场景转换为 2D 屏幕图像的**完整流程**
- 经历了从几何数据到最终像素的多个阶段

```
Unity 渲染管线流程：

┌─────────────────────────────────────────────────────────────┐
│                     渲染管线                                  │
├─────────────────────────────────────────────────────────────┤
│  1. 应用阶段 (Application)                                  │
│     → CPU 准备数据（顶点、材质、纹理）                       │
│                                                             │
│  2. 几何阶段 (Geometry)                                    │
│     → 顶点着色器 → 曲面细分 → 几何着色器 →                   │
│       → 裁剪 → 屏幕映射                                      │
│                                                             │
│  3. 光栅化阶段 (Rasterization)                              │
│     → 三角形设置 → 三角形遍历 → 片段着色器                  │
│                                                             │
│  4. 像素处理 (Pixel Processing)                            │
│     → 混合 → 帧缓冲 → 屏幕输出                              │
└─────────────────────────────────────────────────────────────┘
```

**三种渲染管线的区别**

| 特性 | Built-in（内置） | URP（通用） | HDRP（高清） |
|------|-----------------|------------|--------------|
| 质量 | 中等 | 可调 | 最高 |
| 性能 | 中等 | 优化好 | 较高 |
| 平台 | 全平台 | 移动/主机/PC | PC/主机 |
| Shader | HLSL/CG | Shader Graph/LL | Shader Graph/LL |
| 光照 | 实时+烘焙 | 实时+烘焙 | 实时全局光照 |

**为什么选择 URP？**
- 移动端性能好
- 可以使用 Shader Graph 可视化编程
- 兼容性好，容易迁移

### Unity 渲染管线

| 渲染管线 | 特点 | 适用场景 |
|----------|------|----------|
| Built-in (内置) | 传统，兼容性最好 | 简单项目，学习 |
| URP (通用渲染管线) | 性能好，移动端首选 | 中小型项目 |
| HDRP (高清渲染管线) | 高质量，PC/主机 | 主机游戏，演示 |

### 切换渲染管线

```
1. Window > Package Manager
2. Unity Registry
3. 找到 Universal RP 或 High Definition RP
4. Install
5. Project Settings > Graphics > Assign
```

### 材质 (Material)

```csharp
public class MaterialDemo : MonoBehaviour
{
    private Renderer rend;
    
    void Start()
    {
        rend = GetComponent<Renderer>();
        
        // 获取材质
        Material mat = rend.material;
        
        // 修改颜色
        mat.color = Color.red;
        
        // 修改主纹理
        Texture tex = Resources.Load<Texture>("Textures/Player");
        mat.mainTexture = tex;
        
        // 设置纹理偏移和缩放
        mat.mainTextureOffset = new Vector2(0.5f, 0f);
        mat.mainTextureScale = new Vector2(0.5f, 0.5f);
        
        // 设置 Shader 属性
        mat.SetFloat("_Glossiness", 0.5f);
        mat.SetColor("_Color", Color.blue);
        
        // 获取属性值
        float gloss = mat.GetFloat("_Glossiness");
        
        // 启用/禁用关键字
        mat.EnableKeyword("_EMISSION");
        mat.DisableKeyword("_EMISSION");
        
        // 射线检测获取材质
        // 使用 Physics.Raycast
    }
    
    void Update()
    {
        // 动态修改（性能敏感）
        float t = Mathf.PingPong(Time.time * 0.5f, 1f);
        mat.color = Color.Lerp(Color.red, Color.blue, t);
    }
    
    void OnDestroy()
    {
        // 销毁材质（避免内存泄漏）
        if (mat != null)
            Destroy(mat);
    }
}
```

### 材质类型

```csharp
// Standard Shader - 标准着色器
// URP/Lit - URP 物理着色器
// Unlit - 无光照（性能最好）
// Mobile/Diffuse - 移动端简化版
// Particles/Standard Surface - 粒子系统
// Skybox/Procedural - 天空盒
```

### 练习 3.1

**练习题：**
1. 编写脚本：动态切换材质的颜色，实现红色→蓝色→绿色的循环变化
2. 使用 Shader Graph 创建一个支持法线贴图的材质，说明实现步骤

**答案：**
```csharp
// 1. 颜色循环切换
public class ColorCycle : MonoBehaviour
{
    public float duration = 3f;
    private Material mat;
    
    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }
    
    void Update()
    {
        float t = (Time.time % (duration * 3)) / duration;
        
        if (t < 1)  // 红→蓝
            mat.color = Color.Lerp(Color.red, Color.blue, t);
        else if (t < 2)  // 蓝→绿
            mat.color = Color.Lerp(Color.blue, Color.green, t - 1);
        else  // 绿→红
            mat.color = Color.Lerp(Color.green, Color.red, t - 2);
    }
}

// 2. Shader Graph实现步骤：
// - 创建PBR Graph
// - 添加Main Texture节点 → 连接Base Color
// - 添加Normal Map节点 → 连接Normal
// - 保存并创建材质
// - 将材质拖到物体上
```

### 相关知识点

- 3.3 Shader基础 - Shader Graph是可视化Shader
- 3.8 DrawCall优化 - 材质数量影响DrawCall

---

## 3.2 光照与阴影

#### 核心概念解析

**什么是Unity光照系统？**
- 光照系统是游戏视觉效果的核心，决定了场景的氛围和真实感
- 光照分为实时光照（Realtime）和烘焙光照（Baked）两种模式
- 光照计算是游戏性能开销的主要来源之一

```
Unity 光照系统架构：

┌─────────────────────────────────────────────────────────┐
│                    光照系统                              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   光源（Light）                                         │
│   ├─ Directional Light（平行光）→ 太阳光，全局照明     │
│   ├─ Point Light（点光源）→ 局部照明，距离衰减         │
│   ├─ Spot Light（聚光灯）→ 聚焦照明，角度控制          │
│   └─ Area Light（面光源）→ 柔和照明，烘焙专用          │
│                                                         │
│   光照计算                                             │
│   ├─ 直接光照（Direct）→ 光源直接照射                   │
│   ├─ 间接光照（Indirect）→ 光线反弹                     │
│   └─ 环境光照（Ambient）→ 全局环境光                    │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**光照模式对比**

| 模式 | 计算方式 | 性能 | 质量 |
|------|----------|------|------|
| Realtime | 实时计算 | 高 | 动态变化 |
| Baked | 预计算烘焙 | 低 | 静态 |
| Mixed | 混合使用 | 中等 | 最佳 |

**为什么需要阴影？**
- 阴影提供物体空间关系的视觉线索
- 增强场景的深度感和立体感
- 阴影是判断物体位置的重要参考

---

### 光源类型

| 类型 | 特点 | 适用场景 |
|------|------|----------|
| Directional | 平行光，类似太阳 | 主光源 |
| Point | 点光源，衰减 | 灯泡、火把 |
| Spot | 聚光灯，聚束 | 手电筒、车灯 |
| Area | 面光源，柔和 | 室内照明 |

```csharp
public class LightDemo : MonoBehaviour
{
    private Light mainLight;
    
    void Start()
    {
        mainLight = GetComponent<Light>();
        
        // 光源类型
        mainLight.type = LightType.Directional;
        
        // 颜色
        mainLight.color = Color.white;
        
        // 强度
        mainLight.intensity = 1f;
        
        // 范围（Point/Spot）
        mainLight.range = 10f;
        
        // 角度（Spot）
        mainLight.spotAngle = 40f;
        
        // 阴影
        mainLight.shadows = LightShadows.Soft;  // 软阴影
        mainLight.shadows = LightShadows.Hard;  // 硬阴影
        mainLight.shadows = LightShadows.None; // 无阴影
        
        // 阴影强度
        mainLight.shadowStrength = 1f;
    }
}
```

### 光照设置

```csharp
// Window > Rendering > Lighting

// Environment
// - Skybox Material: 天空盒
// - Sun Source: 太阳光源
// - Ambient Light: 环境光

// Realtime Lighting
// - Realtime Lighting: 实时照明
// - Mixed Lighting: 混合照明

// Lightmapping
// - Lightmapper: 光照贴图烘焙
// - Indirect Intensity: 间接光强度
```

### 练习 3.2

**练习题：**
1. 创建场景：主方向光+一个点光源+一个聚光灯，编写脚本动态调整它们的强度
2. 实现：光照强度随时间从白天到夜晚平滑过渡

**答案：**
```csharp
// 1. 动态调整光照强度
public class LightController : MonoBehaviour
{
    public Light directionalLight;
    public Light pointLight;
    public Light spotLight;
    
    void Update()
    {
        // 随时间变化
        float intensity = Mathf.Sin(Time.time * 0.5f) * 0.5f + 0.5f;
        
        directionalLight.intensity = intensity;
        pointLight.intensity = intensity * 2f;
    }
}

// 2. 白天到夜晚过渡
public class DayNightCycle : MonoBehaviour
{
    public Light sunLight;
    public Gradient dayNightGradient;
    
    void Update()
    {
        float t = Mathf.PingPong(Time.time * 0.01f, 1f);
        sunLight.color = dayNightGradient.Evaluate(t);
        sunLight.intensity = Mathf.Lerp(0.2f, 1.5f, t);
    }
}
```

### 相关知识点

- 3.1 渲染管线 - 光照计算是渲染的一部分
- 3.9 内存管理 - 实时光照消耗性能

---

## 3.3 Shader 基础

#### 核心概念解析

**什么是Shader（着色器）？**
- Shader是运行在GPU上的小程序，负责渲染出每个像素的颜色
- 它定义了物体表面的外观：颜色、光泽、纹理、透明度等
- Unity中的Shader最终会编译为GPU指令

```
Shader 工作原理：

┌─────────────────────────────────────────────────────────┐
│                   GPU 渲染流水线                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   顶点数据（位置、法线、UV）                            │
│        ↓                                               │
│   顶点着色器（Vertex Shader）                          │
│   → 变换坐标、计算法线、传递数据                       │
│        ↓                                               │
│   片段着色器（Fragment/Pixel Shader）                 │
│   → 计算颜色、光照、纹理采样                          │
│        ↓                                               │
│   最终像素颜色输出到屏幕                               │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Shader vs Material（着色器 vs 材质）**
- Shader：定义了渲染的算法和逻辑
- Material：是Shader的实例，可以设置具体参数
- 同一个Shader可以创建多个Material

**为什么游戏开发者需要了解Shader？**
- 实现特殊效果：水、玻璃、火焰等
- 性能优化：简化计算提高帧率
- 美术效果：实现独特的视觉风格
- 问题排查：理解渲染问题原因

---

### Shader 是什么？

Shader 是运行在 GPU 上的程序，决定如何渲染像素

### Unity Shader 类型

```shader
// 1. Unlit Shader（无光照，最简单）
Shader "Custom/SimpleUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}
```

### Surface Shader（表面着色器）

```shader
Shader "Custom/SimpleSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
```

### Shader 优化技巧

```glsl
// 1. 减少精度
fixed4 color;     // 11位，-2~2，适合颜色
half4 color;     // 16位，适合 UV、法线
float4 color;    // 32位，适合位置、世界坐标

// 2. 减少指令
// 不好
half4 c1 = tex2D(_MainTex, uv1);
half4 c2 = tex2D(_BumpMap, uv2);
half4 result = c1 * c2;

// 好
half4 c1 = tex2D(_MainTex, uv1);
half4 result = c1 * tex2D(_BumpMap, uv2);

// 3. 使用 LOD
ShaderLOD 200
```

### 练习 3.3

**练习题：**
1. 编写一个简单的无光照 Shader，实现贴图显示和颜色混合
2. 说明 Vertex Shader 和 Fragment Shader 的区别及各自作用

**答案：**
```csharp
// 1. 简单Unlit Shader
Shader "Custom/SimpleUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _Color;
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
}

// 2. Vertex vs Fragment Shader
// Vertex Shader：对每个顶点执行，处理位置变换、传递数据
// Fragment Shader：对每个像素执行，计算最终颜色
```

### 相关知识点

- 3.1 渲染管线 - Shader是渲染管线的核心
- 3.2 光照与阴影 - 光照计算在Fragment Shader中进行

---

## 3.4 资源加载

#### 核心概念解析

**什么是Unity资源加载？**
- 资源加载是将游戏资源（模型、纹理、音频等）从存储设备加载到内存的过程
- 资源管理是游戏性能优化的关键环节
- 需要平衡加载速度和内存占用

```
Unity 资源加载系统架构：

┌─────────────────────────────────────────────────────────┐
│                    资源类型                               │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   ┌───────────┐   ┌───────────┐   ┌───────────┐       │
│   │  Resources│   │AssetBundle│   │Addressable│       │
│   │  打包时   │   │  运行时   │   │  智能管理  │       │
│   └───────────┘   └───────────┘   └───────────┘       │
│                                                         │
│   加载方式：                                            │
│   ├─ 同步加载：立即返回，阻塞主线程                     │
│   └─ 异步加载：后台加载，不阻塞游戏                      │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Resources vs AssetBundle vs Addressables**

| 特性 | Resources | AssetBundle | Addressables |
|------|-----------|-------------|--------------|
| 加载方式 | 同步/异步 | 异步 | 异步 |
| 更新 | 需重新打包 | 热更新 | 热更新 |
| 内存管理 | 手动 | 手动 | 自动引用计数 |
| 推荐程度 | 不推荐 | 可用 | 推荐 |

**为什么需要不同的加载方式？**
- 小型资源：Resources简单快捷
- 大型资源：AssetBundle分批加载
- 现代项目：Addressables更智能

---

### Resources 方式

```csharp
// 加载 Resources 文件夹中的资源
// 路径相对于 Assets/Resources，不需要扩展名

// 同步加载
GameObject prefab = Resources.Load<GameObject>("Prefabs/Player");
Texture texture = Resources.Load<Texture>("Textures/Sprite");
AudioClip clip = Resources.Load<AudioClip>("Sounds/BGM");
TextAsset json = Resources.Load<TextAsset>("Data/Config");

// 加载所有同类型
GameObject[] allPrefabs = Resources.LoadAll<GameObject>("Prefabs");

// 异步加载
ResourceRequest request = Resources.LoadAsync<GameObject>("Prefabs/Player");
yield return request;
GameObject prefab = request.asset as GameObject;
```

### AssetBundle 方式

```csharp
using UnityEngine.Networking;

public class AssetBundleDemo : MonoBehaviour
{
    private AssetBundle bundle;
    
    // 加载 AssetBundle
    IEnumerator LoadBundle(string url)
    {
        using (UnityWebRequest wr = UnityWebRequestAssetBundle.GetAssetBundle(url))
        {
            yield return wr.SendWebRequest();
            
            if (wr.result == UnityWebRequest.Result.Success)
            {
                bundle = DownloadHandlerAssetBundle.GetContent(wr);
            }
        }
    }
    
    // 加载资源
    IEnumerator LoadAsset()
    {
        string url = "file://" + Application.streamingAssetsPath + "/myassets.bundle";
        
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url);
        yield return request.SendWebRequest();
        
        AssetBundle ab = DownloadHandlerAssetBundle.GetContent(request);
        
        // 同步加载
        GameObject prefab = ab.LoadAsset<GameObject>("Player");
        
        // 异步加载
        AssetBundleRequest asyncRequest = ab.LoadAssetAsync<GameObject>("Player");
        yield return asyncRequest;
        GameObject player = asyncRequest.asset as GameObject;
        
        // 加载所有
        UnityEngine.Object[] all = ab.LoadAllAssets();
    }
    
    // 卸载
    void Unload()
    {
        bundle.Unload(false);  // 卸载 bundle，保留实例
        bundle.Unload(true);  // 卸载 bundle 和所有实例
    }
    
    // 依赖加载
    IEnumerator LoadWithDependencies()
    {
        AssetBundle manifestBundle = AssetBundle.LoadFromFile(
            Application.streamingAssetsPath + "/StreamingAssets"
        );
        AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        
        string[] dependencies = manifest.GetAllDependencies("myassets.bundle");
        
        foreach (string dep in dependencies)
        {
            AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + dep);
        }
    }
}
```

### 练习 3.4

**练习题：**
1. 使用 Resources.Load 异步加载一个预制体，并在加载完成后实例化
2. 对比 Resources、AssetBundle、Addressables 三种加载方式的优缺点

**答案：**
```csharp
// 1. 异步加载Resources
IEnumerator LoadPrefabAsync()
{
    ResourceRequest request = Resources.LoadAsync<GameObject>("Prefabs/Player");
    yield return request;
    
    GameObject prefab = request.asset as GameObject;
    Instantiate(prefab);
}

// 2. 三种方式对比
// Resources: 简单，同步/异步，但打包后无法热更新
// AssetBundle: 可热更新，管理依赖，但需要手动管理
// Addressables: 智能引用计数，自动热更新，推荐使用
```

### 相关知识点

- 3.6 协程 - 异步加载需要协程配合
- 3.9 内存管理 - 资源加载涉及内存管理

---

### Addressables 方式（推荐）

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesDemo : MonoBehaviour
{
    // 初始化
    IEnumerator Init()
    {
        var handle = Addressables.InitializeAsync();
        yield return handle;
    }
    
    // 加载
    IEnumerator Load()
    {
        // 异步加载
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>("Player");
        yield return handle;
        
        GameObject player = handle.Result;
        Instantiate(player);
        
        // 释放
        Addressables.Release(handle);
    }
    
    // 加载多个
    IEnumerator LoadMultiple()
    {
        var handles = Addressables.LoadAssetsAsync<GameObject>("Enemies", null);
        yield return handles;
        
        foreach (var obj in handles.Result)
        {
            Debug.Log(obj.name);
        }
        
        Addressables.Release(handles);
    }
    
    // 实例化（推荐）
    IEnumerator Instantiate()
    {
        var handle = Addressables.InstantiateAsync("PlayerPrefab");
        yield return handle;
        
        GameObject player = handle.Result;
        
        // 销毁
        Addressables.ReleaseInstance(player);
    }
}
```

---

## 3.5 对象池技术

#### 核心概念解析

**什么是对象池？**
- 对象池是一种**复用对象**的技术
- 预先创建一组对象，使用时从池中获取，用完后归还池中
- 避免**频繁创建和销毁对象**带来的性能开销

```
对象池原理：

普通方式（频繁创建销毁）：
┌────────────────────────────────────────────────────┐
│  Frame 1: new Bullet() → Destroy()               │
│  Frame 2: new Bullet() → Destroy()               │
│  Frame 3: new Bullet() → Destroy()               │
│  Frame 4: new Bullet() → Destroy()               │
│  ...                                              │
│  结果：每帧都在分配/释放内存，触发 GC              │
└────────────────────────────────────────────────────┘

对象池方式（复用）：
┌────────────────────────────────────────────────────┐
│  初始化：创建 100 个 Bullet 放入池中                │
│                                                      │
│  Frame 1: pool.Get() → 使用 → pool.Return()       │
│  Frame 2: pool.Get() → 使用 → pool.Return()       │
│  Frame 3: pool.Get() → 使用 → pool.Return()       │
│  ...                                              │
│  结果：内存分配一次，零 GC                          │
└────────────────────────────────────────────────────┘
```

**对象池的核心操作**

| 操作 | 说明 | 目的 |
|------|------|------|
| Get/Acquire | 从池中获取对象 | 复用而非新建 |
| Return/Release | 将对象归还池中 | 等待下次使用 |
| Preload | 预加载对象 | 减少运行时分配 |
| Clear | 清空池 | 释放内存 |

**为什么游戏开发需要对象池？**
- 游戏中有大量**短暂存在的物体**（子弹、粒子、特效）
- `new` 和 `Destroy` 会触发**垃圾回收（GC）**
- GC 会造成**帧率波动**（卡顿）

### 对象池原理

频繁创建和销毁对象会产生大量 GC（垃圾回收），使用对象池复用对象可以显著提升性能。

### 简单对象池实现

```csharp
public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int initialSize = 20;
    public bool canExpand = true;
    
    private Queue<GameObject> pool;
    
    void Awake()
    {
        pool = new Queue<GameObject>();
        
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNew();
            pool.Enqueue(obj);
        }
    }
    
    private GameObject CreateNew()
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        obj.name = prefab.name;
        return obj;
    }
    
    // 获取对象
    public GameObject Get()
    {
        GameObject obj;
        
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else if (canExpand)
        {
            obj = CreateNew();
        }
        else
        {
            return null;
        }
        
        obj.SetActive(true);
        return obj;
    }
    
    // 归还对象
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
    
    // 批量归还
    public void ReturnAll(IEnumerable<GameObject> objs)
    {
        foreach (var obj in objs)
        {
            Return(obj);
        }
    }
}
```

### 练习 3.5

**练习题：**
1. 编写一个通用的泛型对象池，支持预加载和动态扩展
2. 使用对象池实现：10个敌人同时存在，每个敌人发射子弹

**答案：**
```csharp
// 1. 泛型对象池
public class ObjectPool<T> where T : class
{
    private Stack<T> pool = new Stack<T>();
    private System.Func<T> createFunc;
    private System.Action<T> resetAction;
    private int maxSize;
    
    public ObjectPool(System.Func<T> create, System.Action<T> reset = null, int max = 100)
    {
        createFunc = create;
        resetAction = reset;
        maxSize = max;
    }
    
    public T Get()
    {
        return pool.Count > 0 ? pool.Pop() : createFunc();
    }
    
    public void Return(T obj)
    {
        resetAction?.Invoke(obj);
        if (pool.Count < maxSize) pool.Push(obj);
    }
}
```

### 相关知识点

- 4.4 对象池模式 - 设计模式章节有详细实现
- 3.9 内存管理 - 对象池减少GC压力

---

### 使用示例

```csharp
// 子弹管理器
public class BulletManager : MonoBehaviour
{
    public ObjectPool bulletPool;
    
    void Start()
    {
        // 自动获取或创建
        if (bulletPool == null)
            bulletPool = GetComponent<ObjectPool>();
    }
    
    public GameObject SpawnBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = bulletPool.Get();
        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.Reset();
        
        return bullet;
    }
    
    public void ReturnBullet(GameObject bullet)
    {
        bulletPool.Return(bullet);
    }
}

// 子弹类
public class Bullet : MonoBehaviour
{
    private BulletManager manager;
    private Rigidbody rb;
    private float lifetime = 3f;
    private float timer;
    
    public void Initialize(BulletManager mgr)
    {
        manager = mgr;
        rb = GetComponent<Rigidbody>();
    }
    
    public void Reset()
    {
        timer = 0;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= lifetime)
        {
            Return();
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Return();
    }
    
    void Return()
    {
        if (manager != null)
            manager.ReturnBullet(gameObject);
    }
}
```

---

## 3.6 协程

#### 核心概念解析

**什么是协程？**
- 协程（Coroutine）是 Unity 特有的**异步执行**机制
- **不是线程**，仍在主线程执行，但可以实现**分段执行**
- 本质：**在指定位置暂停，在后续帧继续执行**

```
协程 vs 线程：

┌─────────────────────────────────────────────────────────────┐
│  线程（Thread）                                             │
│  • 并行执行，占用独立栈空间                                 │
│  • 需要锁来防止数据竞争                                      │
│  • 创建和切换开销大                                         │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  协程（Coroutine）                                          │
│  • 主线程上分时执行                                         │
│  • 无需锁，天然顺序执行                                     │
│  • 轻量级，几乎无开销                                       │
│  • 适合：延时、异步加载、状态机                             │
└─────────────────────────────────────────────────────────────┘
```

**协程的执行原理**

```csharp
// 协程代码：
IEnumerator Download()
{
    Debug.Log("1. 开始下载");
    yield return new WaitForSeconds(1);  // ← 暂停，1秒后继续
    Debug.Log("2. 下载完成");
    yield return null;  // ← 暂停一帧
    Debug.Log("3. 处理数据");
}

// 执行流程：
// Frame 1: 输出 "1. 开始下载"，暂停
// Frame 2-61: 等待（1秒，约60帧）
// Frame 62: 恢复执行，输出 "2. 下载完成"，暂停
// Frame 63: 恢复执行，输出 "3. 处理完成"，协程结束
```

**协程 vs async/await**

| 特性 | 协程 | async/await |
|------|------|-------------|
| 线程 | 主线程 | 可创建新线程 |
| 适用平台 | Unity | .NET 全平台 |
| 等待单位 | 秒/帧/条件 | Task |
| 取消 | Coroutine对象 | CancellationToken |
| Unity 2020+ | 支持 | 支持（需配置） |

**协程的常见用途**
1. **延时操作**：`yield return new WaitForSeconds(2)`
2. **异步加载**：`yield return www`
3. **状态机**：分步骤执行复杂逻辑
4. **动画过渡**：平滑过渡效果
5. **定时任务**：循环执行某操作

**为什么 Unity 用协程而非线程？**
- Unity 的 API 大部分**不是线程安全的**
- 协程在主线程执行，直接操作 Unity 对象
- 避免线程同步的复杂性

### 协程基础

协程不是线程，而是在主线程上分时执行的代码块

```csharp
public class CoroutineDemo : MonoBehaviour
{
    // 启动协程
    // StartCoroutine：启动协程，返回Coroutine对象
    // 可以启动多个协程同时运行
    void Start()
    {
        // 启动名为"DoSomething"的协程
        StartCoroutine(DoSomething());
        
        // 启动带参数的协程
        StartCoroutine(DoSomethingWithParam(42));
    }
    
    // 基本协程
    // IEnumerator：协程的返回类型
    // 协程函数可以暂停执行，在后续帧继续执行
    IEnumerator DoSomething()
    {
        Debug.Log("开始");
        
        // yield return null：暂停协程，等待下一帧后继续执行
        // 相当于"让出执行权一帧"
        yield return null;
        
        Debug.Log("第一帧后");
        
        // WaitForSeconds：等待指定秒数后继续
        // 注意：受到Time.timeScale影响（时间缩放）
        // 2秒后（如果timeScale=1）协程恢复执行
        yield return new WaitForSeconds(2f);
        
        Debug.Log("2秒后");
    }
    
    // 带参数的协程
    IEnumerator DoSomethingWithParam(int value)
    {
        // 使用参数
        Debug.Log($"值: {value}");
        
        // 等待1秒
        yield return new WaitForSeconds(1f);
        
        Debug.Log("完成");
    }
    
    // 停止协程
    void StopDemo()
    {
        // StopAllCoroutines：停止该脚本启动的所有协程
        StopAllCoroutines();
        
        // StopCoroutine：停止指定名称的协程（字符串）
        StopCoroutine("DoSomething");
    }
}
```

### 常用 Wait 类型

```csharp
IEnumerator WaitTypes()
{
    // ========== 帧等待 ==========
    
    // null：等待一帧，然后在下一帧继续执行
    // 最常用的等待方式，用于分帧处理
    yield return null;
    
    // WaitForEndOfFrame：等待到帧末渲染完成后执行
    // 常用于需要等待UI更新完成后再执行的逻辑
    yield return new WaitForEndOfFrame();
    
    // ========== 时间等待 ==========
    
    // WaitForFixedUpdate：等待物理更新完成后执行
    // 配合FixedUpdate使用，确保物理计算同步
    yield return new WaitForFixedUpdate();
    
    // WaitForSeconds：等待指定秒数（受timeScale影响）
    // timeScale=0.5时，WaitForSeconds(2)实际等待4秒
    yield return new WaitForSeconds(2f);
    
    // WaitForSecondsRealtime：等待真实时间（不受timeScale影响）
    // 用于倒计时、真实延时等场景
    yield return new WaitForSecondsRealtime(2f);
    
    // ========== 条件等待 ==========
    
    // WaitUntil：等待条件变为true后继续执行
    // 传入一个返回bool的Lambda表达式
    yield return new WaitUntil(() => someCondition == true);
    
    // WaitWhile：等待条件变为false后继续执行
    // 当条件为true时持续等待
    yield return new WaitWhile(() => someCondition == true);
    
    // ========== 网络请求 ==========
    
    // UnityWebRequest：Unity的网络请求类
    // 常用于下载资源、请求API等
    using (UnityWebRequest wr = UnityWebRequest.Get(url))
    {
        // SendWebRequest：发送异步请求
        // yield return会等待请求完成
        yield return wr.SendWebRequest();
        
        // downloadHandler.text：获取响应内容
        Debug.Log(wr.downloadHandler.text);
    }
}
```

### 协程实际应用

```csharp
// 延时执行
IEnumerator DelayAction(float delay, System.Action action)
{
    yield return new WaitForSeconds(delay);
    action?.Invoke();
}

// 淡入淡出
IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
{
    float timer = 0;
    while (timer < duration)
    {
        timer += Time.deltaTime;
        canvasGroup.alpha = Mathf.Lerp(0, 1, timer / duration);
        yield return null;
    }
    canvasGroup.alpha = 1;
}

// 异步加载场景
IEnumerator LoadSceneAsync(string sceneName)
{
    AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
    
    while (!op.isDone)
    {
        Debug.Log($"加载进度: {op.progress * 100}%");
        yield return null;
    }
    
    Debug.Log("加载完成");
}

// 移动到目标位置
IEnumerator MoveTo(Transform target, float duration)
{
    Vector3 start = transform.position;
    Vector3 end = target.position;
    float timer = 0;
    
    while (timer < duration)
    {
        timer += Time.deltaTime;
        transform.position = Vector3.Lerp(start, end, timer / duration);
        yield return null;
    }
    
    transform.position = end;
}
```

### 练习 3.6

**练习题：**
1. 使用协程实现：倒计时3秒后执行某个操作，每秒显示剩余时间
2. 实现：异步下载图片并显示进度

**答案：**
```csharp
// 1. 倒计时
IEnumerator Countdown(int seconds)
{
    for (int i = seconds; i > 0; i--)
    {
        Debug.Log($"剩余 {i} 秒");
        yield return new WaitForSeconds(1f);
    }
    Debug.Log("开始!");
}

// 2. 异步下载
IEnumerator DownloadImage(string url)
{
    using (UnityWebRequest wr = UnityWebRequest.Get(url))
    {
        wr.SendWebRequest();
        
        while (!wr.isDone)
        {
            Debug.Log($"进度: {wr.downloadProgress * 100}%");
            yield return null;
        }
        
        if (wr.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(wr);
        }
    }
}
```

### 相关知识点

- 3.4 资源加载 - 协程用于异步加载
- 3.7 性能分析 - 协程性能分析

---

## 3.7 性能分析工具

#### 核心概念解析

**什么是性能分析？**
- 性能分析是找出游戏性能瓶颈的过程
- 通过分析工具了解CPU、GPU、内存的消耗情况
- 目标是确保游戏在目标帧率下稳定运行

```
性能分析流程：

┌─────────────────────────────────────────────────────────┐
│                    性能分析                              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   1. 设定目标帧率（30/60 FPS）                          │
│        ↓                                               │
│   2. 使用Profiler分析                                  │
│        ↓                                               │
│   3. 找出瓶颈（CPU/GPU/内存）                          │
│        ↓                                               │
│   4. 优化对应模块                                      │
│        ↓                                               │
│   5. 验证优化效果                                      │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**常见的性能问题**

| 问题 | 表现 | 原因 |
|------|------|------|
| CPU 瓶颈 | 帧时间过长 | 脚本逻辑、GC、物理 |
| GPU 瓶颈 | 渲染时间长 | 过多DrawCall、复杂Shader |
| 内存问题 | 内存持续增长 | 资源未释放、内存泄漏 |

**Unity性能分析工具**

| 工具 | 用途 |
|------|------|
| Profiler | 综合性能分析 |
| Frame Debugger | 渲染过程分析 |
| Memory Profiler | 内存分析 |
| Statistics | 实时帧率统计 |

---

### Profiler 窗口

```
Window > Analysis > Profiler
```

```csharp
// 性能分析代码
void ProfileCode()
{
    // 开始分析
    Profiler.BeginSample("My Code Block");
    
    // 要分析的代码
    for (int i = 0; i < 1000; i++)
    {
        // 耗时操作
    }
    
Profiler.EndSample();
}
```

### 练习 3.7

**练习题：**
1. 使用 Profiler.BeginSample 分析一段代码的性能，找出耗时最长的部分
2. 说明如何通过 Profiler 识别 CPU 瓶颈和 GPU 瓶颈

**答案：**
```csharp
// 1. 性能分析代码
void MyExpensiveFunction()
{
    Profiler.BeginSample("My Heavy Function");
    
    for (int i = 0; i < 1000; i++)
    {
        // 耗时操作
        GameObject.FindObjectsOfType<MonoBehaviour>();
    }
    
    Profiler.EndSample();
}

// 2. 瓶颈识别
// CPU瓶颈：CPU时间过高
// - 检查Scripts项下的函数调用
// - 查找GC调用（垃圾回收）
// - 检查物理计算
// GPU瓶颈：GPU时间过高
// - 检查渲染耗时
// - 查看DrawCall数量
// - 检查Shader复杂度
```

### 相关知识点

- 3.8 DrawCall优化 - 通过Profiler观察优化效果
- 3.9 内存管理 - 通过Profiler观察内存使用

---

### CPU 分析

```csharp
Profiler > CPU
- Main: 主线程
- Rendering: 渲染
- Physics: 物理
- GarbageCollector: GC
```

### GPU 分析

```
Profiler > GPU
- 渲染耗时
- Shader 复杂度
```

### 内存分析

```
Profiler > Memory
- Total: 总内存
- Mono: .NET 堆内存
- GFX: 纹理、网格等
```

### Frame Debugger

```
Window > Analysis > Frame Debugger
```

可以看到每一帧的 DrawCall 详情

---

## 3.8 DrawCall 优化

### 什么是 DrawCall？

DrawCall 是 CPU 向 GPU 发送的渲染命令，每次 DrawCall 都有开销

### 优化方法

```csharp
// 1. 静态批处理
// Inspector 中勾选 GameObject 的 Static
// 或使用 API
StaticBatchingUtility.Combine(gameObject);

// 2. 动态批处理（自动启用）
// 条件：< 900 顶点，使用相同材质
// 开启：Project Settings > Player > Dynamic Batching

// 3. GPU Instancing（实例化渲染）
// 材质中勾选 Enable GPU Instancing
// 代码中使用
Graphics.DrawMeshInstanced(mesh, 0, material, matrices);

// 4. 图集（Sprite Atlas）
// 将小图片合并为大图
// TextureImporter > Sprite Mode > Multiple > Sprite Editor

// 5. 减少材质数量
// 合并材质
Material[] mats = renderer.materials;
Material newMat = new Material(Shader.Find("Standard"));
mats[0] = newMat;
renderer.materials = mats;

// 6. 减少透明物体
// 透明物体需要从后往前渲染
// 尽量使用不透明物体

// 7. 遮挡剔除（Occlusion Culling）
// Window > Rendering > Occlusion Culling
// 设置遮挡体和被遮挡体
```

### Batcher 相关信息

```csharp
// 检查是否会被批处理
// DrawCall 详细信息可在 Frame Debugger 中查看

// 批处理要求：
// - 相同材质
// - 相同 Shader
// - 相同纹理（除非使用图集）
```

### 练习 3.8

**练习题：**
1. 场景有100个相同模型，如何优化DrawCall？至少列出3种方法
2. 使用 GPU Instancing 渲染1000个相同物体

**答案：**
```csharp
// 1. DrawCall优化方法
// - 静态批处理：对静止物体勾选Static
// - 动态批处理：开启Dynamic Batching
// - GPU Instancing：相同材质启用Instancing
// - 图集：将小纹理打包成图集
// - 材质合并：减少材质数量

// 2. GPU Instancing
public class InstancingDemo : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int count = 1000;
    
    void Update()
    {
        Matrix4x4[] matrices = new Matrix4x4[count];
        
        for (int i = 0; i < count; i++)
        {
            Vector3 position = new Vector3(i % 10 * 1.5f, 0, i / 10 * 1.5f);
            matrices[i] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
        }
        
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
    }
}
```

### 相关知识点

- 3.7 性能分析 - 用Frame Debugger查看DrawCall
- 3.9 内存管理 - Instancing减少内存占用

---

## 3.9 内存管理

#### 核心概念解析

**什么是Unity内存管理？**
- Unity内存分为托管堆（Managed）和原生内存（Native）两部分
- 托管内存由C#垃圾回收器（GC）自动管理
- 原生内存需要手动管理（纹理、网格、音频等）

```
Unity 内存架构：

┌─────────────────────────────────────────────────────────┐
│                   Unity 内存                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   ┌───────────────────────┐                            │
│   │   Managed Heap（托管堆）│  ← C# 对象，由GC管理       │
│   │   - 类实例            │                            │
│   │   - 数组、字符串      │                            │
│   │   - 委托、Lambda      │                            │
│   └───────────────────────┘                            │
│                                                         │
│   ┌───────────────────────┐                            │
│   │   Native Memory（原生）│  ← Unity内部，由引擎管理   │
│   │   - 纹理 Texture      │                            │
│   │   - 网格 Mesh          │                            │
│   │   - 音频 AudioClip    │                            │
│   │   - 动画 Animation    │                            │
│   └───────────────────────┘                            │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**GC（垃圾回收）的工作原理**
- GC自动扫描不再使用的对象并释放内存
- GC会暂停游戏造成卡顿
- 频繁GC会导致帧率不稳定

**内存优化原则**
- 减少堆内存分配，避免频繁GC
- 及时释放不再使用的资源
- 使用对象池复用频繁创建销毁的对象
- 监控内存使用，发现泄漏及时处理

---

### Unity 内存区域

```
┌─────────────────────────────────────────┐
│           Managed Heap (托管堆)          │
│  - C# 对象（new 出来的）                │
│  - 由 GC 自动管理                        │
│  - 可用内存不足时触发 GC                 │
├─────────────────────────────────────────┤
│           Native Memory                 │
│  - Unity 内部对象                        │
│  - 纹理、网格、音频                      │
│  - 需要手动管理                          │
└─────────────────────────────────────────┘
```

### GC 触发时机

```csharp
// GC 自动触发条件：
// - 托管堆内存不足
// - 达到阈值
// - 手动调用 GC.Collect()

// 优化建议：
// - 避免频繁创建对象
// - 及时 null 引用
// - 使用对象池
```

### 内存优化技巧

```csharp
public class MemoryOptimization : MonoBehaviour
{
    // 1. 缓存组件引用
    private Renderer rend;
    private Collider col;
    
    void Awake()
    {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }
    
    // 2. 使用 StringBuilder
    void BadStringConcat()
    {
        string s = "";
        for (int i = 0; i < 100; i++)
        {
            s += i.ToString();  // 每次循环创建新字符串
        }
    }
    
    void GoodStringConcat()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            sb.Append(i);
        }
        string s = sb.ToString();
    }
    
    // 3. 避免 LINQ（每次调用会产生垃圾）
    void AvoidLinq()
    {
        var list = new List<int>();
        
        // 不好
        var result = list.Where(x => x > 5).ToList();
        
        // 好
        List<int> result2 = new List<int>();
        foreach (var x in list)
        {
            if (x > 5) result2.Add(x);
        }
    }
    
    // 4. 结构体 vs 类
    // 结构体在栈上，不产生 GC
    struct Point3D
    {
        public float X, Y, Z;
    }
    
    // 5. 及时销毁资源
    void OnDestroy()
    {
        if (texture != null)
        {
            Destroy(texture);
        }
        
        if (audioClip != null)
        {
            Destroy(audioClip);
        }
    }
    
    // 6. Resources.UnloadUnusedAssets
    void UnloadUnused()
    {
        Resources.UnloadUnusedAssets();
    }
}
```

### 内存泄漏检测

```csharp
// 使用 Profiler 监控内存
// 观察 Memory 曲线是否持续上升

// 常见泄漏原因：
// - 未取消订阅的事件
// - 静态引用
// - 缓存未清理
// - 协程未停止

// 防止事件泄漏
void OnEnable()
{
    SomeEvent += OnEvent;
}

void OnDisable()
{
    SomeEvent -= OnEvent;  // 取消订阅
}

// 防止协程泄漏
private Coroutine coroutine;

void Start()
{
    coroutine = StartCoroutine(MyCoroutine());
}

void OnDestroy()
{
    if (coroutine != null)
    {
        StopCoroutine(coroutine);
    }
}
```

### 练习 3.9

**练习题：**
1. 编写代码：使用 StringBuilder 拼接10000个字符串，对比 String 的性能
2. 说明常见的内存泄漏原因及检测方法

**答案：**
```csharp
// 1. StringBuilder vs String
public void TestStringConcat()
{
    // 使用String（产生大量GC）
    string s = "";
    for (int i = 0; i < 10000; i++) s += i.ToString();
    
    // 使用StringBuilder（零GC）
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    for (int i = 0; i < 10000; i++) sb.Append(i);
    string result = sb.ToString();
}

// 2. 内存泄漏检测
// 常见原因：
// - 事件订阅未取消
// - 静态引用
// - 缓存未清理
// - 协程未停止
// - 大材质未释放
// 
// 检测方法：
// - Profiler观察Memory曲线
// - 加载场景前后对比内存
// - 长时间运行观察内存增长
```

### 相关知识点

- 3.6 协程 - 协程泄漏导致内存问题
- 4.4 对象池 - 对象池减少内存分配

---

## 3.10 SRP Batcher 详解

#### 核心概念解析

**什么是 SRP Batcher？**
- SRP Batcher（Scriptable Render Pipeline Batcher）是 Unity 2019+ 的渲染优化技术
- 专门配合 URP/HDRP 使用，将多个使用相同 Shader 的材质合并为一个 DrawCall

### SRP Batcher 原理

```
SRP Batcher 原理：

未使用SRP Batcher：          使用SRP Batcher：
┌─────┐ ┌─────┐ ┌─────┐      ┌───────────────┐
│Mat A│ │Mat A│ │Mat A│  →    │ SRP Batcher  │
│Draw │ │Draw │ │Draw │      │ 合并为1个Draw │
└─────┘ └─────┘ └─────┘      └───────────────┘
  3次DrawCall                  1次DrawCall
```

### SRP Batcher 要求

```csharp
// 1. 使用 URP/HDRP 渲染管线
// 2. Shader 必须是 Shader Graph 或支持 SRP Batcher 的 Shader
// 3. 材质属性块使用 MaterialPropertyBlock

public class SRPBatcherDemo : MonoBehaviour
{
    private Material mat;
    private MaterialPropertyBlock props;
    private Renderer rend;
    
    void Start()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;
        props = new MaterialPropertyBlock();
    }
    
    void Update()
    {
        // 使用PropertyBlock设置属性
        props.SetColor("_Color", Color.Lerp(Color.red, Color.blue, Time.time));
        rend.SetPropertyBlock(props);
    }
}
```

### 练习 3.10

**练习题：**
1. 创建一个 URP 项目，验证 SRP Batcher 是否开启
2. 说明 SRP Batcher 与传统批处理的区别

**答案：**
```csharp
// 1. 检查SRP Batcher
// - Project Settings > Graphics > URP设置
// - 检查 "SRP Batcher" 是否勾选
// - 在Frame Debugger中查看是否有 "SRP Batcher" 项

// 2. 区别
// 传统批处理：需要相同材质、相同Shader
// SRP Batcher：相同Shader即可，即使材质不同也能批处理
// SRP Batcher支持动态改变材质属性
```

### 相关知识点

- 3.1 渲染管线 - URP渲染管线
- 3.8 DrawCall优化 - DrawCall优化技术

---

## 3.11 遮挡剔除进阶

#### 核心概念解析

**什么是遮挡剔除？**
- 遮挡剔除（Occlusion Culling）是被其他物体遮挡的物体不进行渲染
- 减少不必要的渲染，提升性能

### 遮挡剔除设置

```csharp
public class OcclusionDemo : MonoBehaviour
{
    // 在 Project Settings > Rendering 中开启
    // Window > Rendering > Occlusion Culling
    
    // 设置遮挡体（Occluder）：遮挡其他物体的物体
    // 设置被遮挡体（Occludee）：被遮挡的物体
    
    // 通过代码设置
    void SetupOcclusion()
    {
        // 设置为遮挡体
        gameObject.layer = LayerMask.NameToLayer("Occluder");
        
        // 设置为被遮挡体
        gameObject.layer = LayerMask.NameToLayer("Occludee");
    }
}
```

### 练习 3.11

**练习题：**
1. 创建一个有遮挡关系的场景，配置遮挡剔除
2. 对比开启和关闭遮挡剔除的性能差异

**答案：**
```csharp
// 遮挡剔除配置步骤：
// 1. Window > Rendering > Occlusion Culling
// 2. 点击 "Generate Occlusion Culling"
// 3. 在场景中放置遮挡体（大型建筑）
// 4. 生成后测试效果
//
// 性能对比：
// - 关闭：大面积建筑后仍有DrawCall
// - 开启：被遮挡物体不渲染，DrawCall减少
```

### 相关知识点

- 3.7 性能分析 - 用Profiler观察优化效果
- 3.8 DrawCall优化 - 减少渲染开销

---

## 3.12 GPU Instancing 深入

#### 核心概念解析

**GPU Instancing 深入**
- GPU Instancing 允许一次绘制调用渲染多个相同几何体
- 区别于传统批处理：Instancing 可在运行时动态使用

### 高级 Instancing 用法

```csharp
public class AdvancedInstancing : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int instanceCount = 10000;
    
    private Matrix4x4[] matrices;
    private Vector4[] colors;
    
    void Start()
    {
        matrices = new Matrix4x4[instanceCount];
        colors = new Vector4[instanceCount];
        
        for (int i = 0; i < instanceCount; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 50,
                Quaternion.identity,
                Vector3.one * 0.5f
            );
            colors[i] = new Vector4(
                Random.value, Random.value, Random.value, 1
            );
        }
    }
    
    void Update()
    {
        // 动态更新颜色
        for (int i = 0; i < instanceCount; i++)
        {
            colors[i] = new Vector4(
                Mathf.Sin(Time.time + i) * 0.5f + 0.5f,
                0, 0, 1
            );
        }
        
        // 使用PropertyBlock传递额外数据
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetVectorArray("_Color", colors);
        
        Graphics.DrawMeshInstanced(
            mesh, 0, material, matrices,
            instanceCount, props
        );
    }
}
```

### 练习 3.12

**练习题：**
1. 实现：1000个物体使用GPU Instancing渲染，每个物体有不同的颜色
2. 说明 GPU Instancing 的适用场景和限制

**答案：**
```csharp
// 1. 颜色不同但使用Instancing
// 使用MaterialPropertyBlock传递每个实例的颜色

// 2. 适用场景
// - 大量相同模型
// - 草地、树木、粒子
// - 建筑群
// 
// 限制：
// - 需要相同Mesh和Material
// - 某些Shader不支持Instancing
// - 实例数量超过1023需要分批绘制
```

### 相关知识点

- 3.8 DrawCall优化 - Instancing减少DrawCall
- 3.10 SRP Batcher - 配合SRP Batcher使用

---

## 3.13 ScriptableObject 深入

#### 核心概念解析

**什么是 ScriptableObject？**
- ScriptableObject 是 Unity 的数据容器
- 用于存储配置数据、创建资源文件、脱离场景的数据持久化

### ScriptableObject 创建

```csharp
// 创建自定义ScriptableObject
[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    public string playerName;
    public int maxHealth;
    public float moveSpeed;
    public WeaponData defaultWeapon;
}

// 在代码中创建
PlayerData data = ScriptableObject.CreateInstance<PlayerData>();
data.playerName = "Hero";
data.maxHealth = 100;
AssetDatabase.CreateAsset(data, "Assets/Data/PlayerData.asset");
```

### ScriptableObject 应用

```csharp
// 1. 配置数据
public class GameConfig : ScriptableObject
{
    public int maxPlayers = 4;
    public float roundTime = 60f;
    public string[] startingWeapons;
}

// 使用
public class GameManager : MonoBehaviour
{
    public GameConfig config;
    
    void Start()
    {
        Debug.Log($"最大玩家: {config.maxPlayers}");
    }
}

// 2. 事件数据
public class GameEvent : ScriptableObject
{
    private System.Action listeners;
    
    public void Raise() => listeners?.Invoke();
    public void Register(System.Action callback) => listeners += callback;
    public void Unregister(System.Action callback) => listeners -= callback;
}
```

### 练习 3.13

**练习题：**
1. 使用 ScriptableObject 创建敌人配置数据表
2. 实现：游戏开始时加载配置数据，运行时修改配置

**答案：**
```csharp
// 1. 敌人配置
[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    public string enemyName;
    public int health;
    public float speed;
    public int damage;
    public GameObject prefab;
}

// 2. 配置管理器
public class ConfigManager : MonoBehaviour
{
    public EnemyConfig[] enemyConfigs;
    
    public EnemyConfig GetConfig(string name)
    {
        return System.Array.Find(enemyConfigs, c => c.enemyName == name);
    }
    
    // 动态修改（运行时）
    public void ModifyConfig(string name, int newHealth)
    {
        var config = GetConfig(name);
        if (config != null)
            config.health = newHealth;
    }
}
```

### 相关知识点

- 3.4 资源加载 - ScriptableObject通过资源系统加载
- 4.5 工厂模式 - 工厂使用配置数据

---

## 3.14 后处理效果

#### 核心概念解析

**什么是后处理？**
- 后处理（Post-Processing）是渲染完成后对画面进行调整
- 包括：Bloom、颜色分级、景深、运动模糊等

### 后处理设置

```csharp
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingDemo : MonoBehaviour
{
    public Volume postProcessVolume;
    
    void Start()
    {
        // 获取后处理层
        var volume = GetComponent<Volume>();
        
        // 调整Bloom
        if (volume.profile.TryGet(out Bloom bloom))
        {
            bloom.intensity.value = 1.5f;
            bloom.threshold.value = 0.9f;
        }
        
        // 调整颜色分级
        if (volume.profile.TryGet(out ColorAdjustments colorAdj))
        {
            colorAdj.contrast.value = 20f;
            colorAdj.saturation.value = 10f;
        }
    }
}
```

### 常见后处理效果

```csharp
// 1. Bloom（泛光）
var bloom = volume.profile.Add<Bloom>();
bloom.intensity.value = 1f;
bloom.threshold.value = 0.9f;

// 2. 景深
var depthOfField = volume.profile.Add<DepthOfField>();
depthOfField.focusDistance.value = 10f;
depthOfField.focalLength.value = 50f;

// 3. 颜色分级
var colorAdjust = volume.profile.Add<ColorAdjustments>();
colorAdjust.contrast.value = 15f;
colorAdjust.saturation.value = 5f;

// 4. 运动模糊
var motionBlur = volume.profile.Add<MotionBlur>();
motionBlur.intensity.value = 0.5f;
```

### 练习 3.14

**练习题：**
1. 实现：按下按键切换不同的后处理配置（白天/夜晚/战斗）
2. 创建自定义后处理效果：简单的颜色滤镜

**答案：**
```csharp
// 1. 切换后处理配置
public class PostProcessingSwitcher : MonoBehaviour
{
    public Volume dayVolume;
    public Volume nightVolume;
    public Volume battleVolume;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) SetProfile(dayVolume);
        if (Input.GetKeyDown(KeyCode.F2)) SetProfile(nightVolume);
        if (Input.GetKeyDown(KeyCode.F3)) SetProfile(battleVolume);
    }
    
    void SetProfile(Volume target)
    {
        dayVolume.weight = target == dayVolume ? 1 : 0;
        nightVolume.weight = target == nightVolume ? 1 : 0;
        battleVolume.weight = target == battleVolume ? 1 : 0;
    }
}

// 2. 自定义颜色滤镜（使用Shader）
// 需要创建自定义渲染管线或使用Render Feature
```

### 相关知识点

- 3.1 渲染管线 - URP的后处理系统
- 3.3 Shader - 后处理也是Shader的一种应用

---

## 本阶段练习

### 进阶练习

1. 编写一个简单的 Shader
2. 实现子弹对象池系统
3. 使用协程实现场景淡入淡出

---

## 相关学习链接

- ← 上一阶段：[阶段二_Unity基础.md](./阶段二_Unity基础.md)
- → 下一阶段：[阶段四_设计模式.md](./阶段四_设计模式.md)
- 📺 配套视频：[视频教程汇总.md](./视频教程汇总.md)
- 📝 面试题库：[面试题库.md](./面试题库.md)
- ⚡ 面试突击：[面试突击计划.md](./面试突击计划.md)

> 第三阶段完成！下一步将学习设计模式与架构。
## 对照学习点与练习
- 练习对照：渲染管线、材质/着色、光照、Shader 基础、资源加载、对象池、协程、内存管理等，逐条映射至逐条对应练习模板中的相关练习。
- 具体对照：MaterialDemo、简单 Shader、InstancingDemo、ResourcesLoader、AssetBundleLoader、AddressablesLoader、CoroutineDemo、AsyncDemo、BridgeDemo 等示例脚本。
- 练习建议：在一个简化场景中把以上要点串起来，按 Stage3 的排序顺序实现并用 Profiler/Frame Debugger 观察性能变化。
