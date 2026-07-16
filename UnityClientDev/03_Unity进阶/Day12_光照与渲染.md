# Day 12：光照与渲染 — 从 GPU 管线到可编程渲染

## 0. 为什么需要渲染管线？

在 Raylib/C++ 中，"渲染"就是调用一系列绘图函数：

```cpp
// Raylib：调用绘图 API，GPU 内部执行
BeginDrawing();
ClearBackground(RAYWHITE);
DrawTexturePro(texture, src, dest, origin, rotation, WHITE);
DrawRectangle(x, y, w, h, color);
DrawText("Hello", 100, 50, 20, BLACK);
EndDrawing();
```

Unity 的渲染是**引擎自动执行的**——你放置 Camera 和对象，引擎每帧自动渲染。你可以通过**渲染管线**（Render Pipeline）控制渲染的方式和效果。

---

## 1. GPU 渲染管线——从顶点到像素

### 完整渲染流程

```
CPU 端（每帧）：
1. 剔除（Culling）
   - 视锥体裁剪（Frustum Culling）：不在摄像机视野内的不渲染
   - 遮挡剔除（Occlusion Culling）：被其他物体遮挡的不渲染
   
2. 合批（Batching）
   - 将相同材质/网格的对象合并成一次 Draw Call

3. 提交渲染命令
   - 向 GPU 发送绘制指令

GPU 端（对每个 Draw Call）：
┌─────────────────────────────────────────────┐
│ 顶点着色器 (Vertex Shader)                    │
│ 输入：顶点位置、法线、UV、颜色                  │
│ 处理：模型→世界→视口→投影变换                   │
│ 输出：裁剪空间中的顶点                          │
├─────────────────────────────────────────────┤
│ 光栅化 (Rasterization)                       │
│ 将三角形转换为像素片段                          │
├─────────────────────────────────────────────┤
│ 片元着色器 (Fragment / Pixel Shader)          │
│ 输入：插值后的顶点数据（UV、法线等）              │
│ 处理：纹理采样、光照计算、阴影                    │
│ 输出：像素颜色 + 深度值                         │
├─────────────────────────────────────────────┤
│ 输出合并 (Output Merger)                      │
│ 深度测试、模板测试、颜色混合                     │
│ 写入帧缓冲区 (Frame Buffer)                   │
└─────────────────────────────────────────────┘
```

---

## 2. Unity 的三种渲染管线

### Built-in Render Pipeline（内置管线）

```
Unity 的传统渲染管线
- 固定功能 + 可编程 Shader 混合
- 配置简单，开箱即用
- 性能一般，扩展性有限

适用场景：老项目、简单 2D 游戏、快速原型
```

### URP（Universal Render Pipeline）

```
Unity 的轻量级可编程渲染管线
- 基于 SRP（Scriptable Render Pipeline）
- 性能优化（单 Pass 前向渲染、减少 Overdraw）
- 支持 2D、3D、VR
- 内置后期处理（Bloom、Tonemapping 等）

适用场景：大多数新项目（推荐）、移动游戏、PC 游戏
```

### HDRP（High Definition Render Pipeline）

```
Unity 的高端渲染管线
- 基于 SRP，用于高质量游戏
- 物理级光照、实时全局光照
- 高级视觉效果（体积光、SSR、SSAO）
- GPU 性能要求高

适用场景：3A 大作、高画质 PC/主机游戏
```

### SRP（Scriptable Render Pipeline）——可编程管线的底层

```csharp
// SRP 是 Unity 2019+ 的新架构
// 让你用 C# 自定义渲染管线

// 管线的 C# 控制（简化）：
public class CustomRenderPipeline : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            // 1. 剔除（Culling）
            context.SetupCameraProperties(camera);
            CullingResults cullingResults = context.Cull(ref cullingParams);

            // 2. 排序（Sorting）
            // 3. 渲染（Draw）
            DrawingSettings drawSettings = new DrawingSettings(...);
            FilteringSettings filterSettings = new FilteringSettings(...);
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);

            // 4. 提交（Submit）
            context.Submit();
        }
    }
}
```

---

## 3. 材质（Material）与 Shader

### 材质 = Shader + 参数

```
Material（材质）
├── Shader（着色器程序）—— 告诉 GPU 怎么画
├── 公开参数（Properties）
│   ├── _Color (Color)
│   ├── _MainTex (Texture)
│   ├── _Glossiness (Float)
│   └── _Metallic (Float)
└── 渲染状态
    ├── RenderQueue（渲染队列顺序）
    ├── Cull（剔除模式）
    └── Blend（混合模式）
```

### 通过代码控制材质

```csharp
public class MaterialControl : MonoBehaviour
{
    private Renderer rend;

    void Awake() => rend = GetComponent<Renderer>();

    void Start()
    {
        // 获取实例化的材质（修改不影响其他对象）
        Material mat = rend.material;

        // 设置属性
        mat.SetColor("_Color", Color.red);
        mat.SetFloat("_Glossiness", 0.5f);
        mat.SetTexture("_MainTex", Resources.Load<Texture2D>("Textures/Fabric"));

        // 如果使用 Standard Shader，有封装好的属性名：
        mat.color = Color.red;  // 等同于 SetColor("_Color", ...)
    }
}
```

### Standard Shader 的 BRDF 模型

```
Standard Shader 使用 PBR（Physically Based Rendering）模型

核心参数：
- Albedo（漫反射颜色）：物体本身的颜色
- Metallic（金属度）：0 = 非金属（塑料/木材），1 = 金属
- Smoothness（光滑度）：0 = 粗糙，1 = 镜面

光照反射模型：
- 漫反射（Diffuse）：Lambert 模型，光线均匀向各方向反射
- 高光（Specular）：Blinn-Phong 或 GGX 模型，光线沿反射方向集中
- 环境反射（Environment）：通过 Reflection Probe 捕捉

PBR 的核心思想：
- 现实世界中几乎所有表面属性都可以用 "金属度" + "粗糙度" 描述
- 一个参数集可以适应从塑料到黄金的所有材质
```

---

## 4. 光照（Light）组件

### 光源类型

```csharp
// Directional Light（方向光）
// - 模拟无限远的平行光（太阳）
// - 位置不影响光照方向，只有旋转影响
// - 所有物体受到相同方向的光照
// - 性能开销最小

// Point Light（点光源）
// - 从一点向所有方向发光（灯泡、蜡烛）
// - 有范围（Range）和衰减（Intensity 随距离减弱）
// - 性能开销中等

// Spot Light（聚光灯）
// - 锥形光照区域（手电筒、射灯）
// - 有范围、角度、衰减
// - 性能开销中等

// Area Light（面光源）
// - 从矩形区域发出光（荧光灯管、窗户）
// - 仅用于烘焙（Baked），不支持实时
// - 产生最真实的软阴影
```

### 光照模式

```csharp
// 实时光照（Realtime）
// - 每帧计算
// - 支持动态物体
// - 性能开销大（尤其多光源）

// 烘焙光照（Baked）
// - 预先计算并存储在光照贴图（Lightmap）中
// - 不支持动态物体
// - 零运行时开销

// 混合光照（Mixed）
// - 结合实时和烘焙
// - 静态物体使用烘焙
// - 动态物体受到实时光照
```

### 阴影（Shadow）

```csharp
// 阴影的实现：Shadow Map（阴影贴图）算法

// 原理：
// 1. 从光源位置渲染场景到深度缓冲（Shadow Map）
// 2. 渲染主摄像机时，将每个像素转换到光源空间
// 3. 比较深度：如果在阴影中（深度 > Shadow Map 中的值），则变暗

// 阴影设置：
// - Shadow Resolution（阴影贴图分辨率）：越高越清晰，消耗越大
// - Shadow Distance（阴影距离）：超过此距离不渲染阴影
// - Shadow Cascades（级联阴影映射）：近处用高精度，远处用低精度
```

---

## 5. 合批（Batching）——减少 Draw Call

### 合批的目标

```csharp
// 每次 Draw Call 的 CPU 开销：
// 1. 设置渲染状态（Material、Texture、Shader 参数）
// 2. 提交顶点数据到 GPU
// 3. GPU 上下文切换

// 目标是：减少 Draw Call 数量！

// 100 个相同材质的小球 → 1 次 Draw Call（合批）
// 100 个不同材质的小球 → 100 次 Draw Call（不合批）
```

### 静态合批（Static Batching）

```csharp
// 将标记为 "Static" 的物体合并成一个大的 Mesh
// 一次性提交，减少 Draw Call

// 开启方式：
// 1. 选中 GameObject → Inspector 右上角 Static 打勾
// 2. 或者代码：gameObject.isStatic = true;
// 3. Player Settings → Static Batching 启用

// 限制：
// - 只能合批不动的物体（标记 Static）
// - 会增加内存（大 Mesh）
```

### 动态合批（Dynamic Batching）

```csharp
// Unity 自动将小物体的顶点合并后提交
// 不需要标记 Static

// 限制（Built-in 管线）：
// - 顶点数 < 300
// - 材质必须相同
// - 不能是蒙皮网格（Skinned Mesh）
// - 受光照影响时限制更多
```

### GPU Instancing

```csharp
// GPU Instancing：
// 一种更高效的合批方式
// 相同 Mesh + 相同 Material 的对象可以 Instancing

// 使用条件：
// - 使用支持 Instancing 的 Shader（Standard Shader 默认支持）
// - 材质使用 MaterialPropertyBlock（每个实例可以有不同的颜色等）

// 性能对比：
// 10,000 个相同物体：
// 无合批：10,000 Draw Calls
// 静态合批：1 Draw Call（大 Mesh）
// GPU Instancing：1 Draw Call（硬件级）

public class InstancingExample : MonoBehaviour
{
    public Mesh mesh;
    public Material material;

    void Update()
    {
        // 使用 MaterialPropertyBlock 设置每个实例的不同属性
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        for (int i = 0; i < 10000; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 50;
            Quaternion rot = Random.rotation;
            Vector3 scale = Vector3.one * Random.Range(0.5f, 1.5f);
            Matrix4x4 mat = Matrix4x4.TRS(pos, rot, scale);

            // 每个实例不同颜色
            block.SetColor("_Color", Random.ColorHSV());

            // 提交 Instancing 调用
            Graphics.DrawMeshInstanced(mesh, 0, material,
                new List<Matrix4x4> { mat }, block);
        }
    }
}
```

---

## 6. 练习：光照系统

```csharp
using UnityEngine;

public class Day12_Lighting : MonoBehaviour
{
    void Start()
    {
        // 创建光源
        GameObject lightObj = new GameObject("MyPointLight");
        Light light = lightObj.AddComponent<Light>();

        // 配置光源
        light.type = LightType.Point;      // 点光源
        light.range = 10f;                 // 光照范围
        light.intensity = 3f;              // 强度
        light.color = Color.yellow;        // 颜色
        light.shadows = LightShadows.Hard; // 阴影类型
        lightObj.transform.position = new Vector3(0, 5, 0);

        // 光源闪烁效果
        StartCoroutine(FlickerLight(light));
    }

    IEnumerator FlickerLight(Light light)
    {
        while (true)
        {
            // 使用正弦波产生平滑闪烁
            float intensity = 2f + Mathf.Sin(Time.time * 5f) * 1.5f;
            light.intensity = intensity;

            // 颜色循环
            float hue = (Time.time * 0.1f) % 1f;
            light.color = Color.HSVToRGB(hue, 1, 1);

            yield return null;
        }
    }
}
```

---

---

## C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity (C#) |
|------|-------------|-----------|
| 渲染循环 | `BeginDrawing()`/`EndDrawing()` | 渲染管线自动执行 |
| 清屏 | `ClearBackground(RAYWHITE)` | Camera Clear Flags |
| 绘制纹理 | `DrawTexturePro(...)` | SpriteRenderer / MeshRenderer |
| 着色器 | `BeginShaderMode(shader)` | Material + Shader |
| 光照 | 手动光照计算 | Light 组件 + 物理光照 |
| 阴影 | 手写 Shadow Map | Light > Shadow Type |
| 材质 | `DrawTexturePro` 直接画 | Material + Shader 参数 |
| — | 无 | SRP（可编程渲染管线） |
| — | 无 | URP / HDRP 管线选择 |
| — | 无 | Post-processing Volume |

## 停靠点

> GPU 渲染管线：顶点着色器 → 光栅化 → 片元着色器 → 输出合并。
> Unity 三种管线：Built-in（传统）、URP（推荐新项目）、HDRP（3A 画质）。
> PBR（Standard Shader）用"金属度 + 粗糙度"两个参数模拟几乎所有材质。
> 合批减少 Draw Call：静态合批（不动物体）、动态合批（小物体）、GPU Instancing（大量相同物体）。
> SRP（可编程渲染管线）让你用 C# 控制渲染全流程。

