# Day 24：着色器编程入门 — 从 GPU 到像素

## 0. 为什么需要着色器？

在 Day12 中我们了解了渲染管线的概念。着色器（Shader）是**渲染管线中可编程的部分**。

```
固定功能管线（不可编程）：
顶点 → 光栅化 → 像素 → 输出
            ↓
     硬件决定的，不能改

可编程管线（Shader）：
顶点 → 顶点着色器 → 光栅化 → 片元着色器 → 输出
            ↑                        ↑
        程序员控制顶点位置       程序员控制像素颜色
```

**为什么学 Shader？**
- 实现引擎自带不了的视觉效果（溶解、扭曲、描边）
- 性能优化（把 CPU 工作移到 GPU）
- 理解渲染本质
- 这是 TA（技术美术）和图形程序员的必备技能

---

## 1. GPU 渲染管线的两个着色器

### 顶点着色器（Vertex Shader）

```
输入：一个顶点（位置、法线、UV）
输出：变换后的顶点位置

做什么：
- 将模型顶点从模型空间 → 世界空间 → 观察空间 → 裁剪空间
- 传递数据到片元着色器

每个顶点执行一次 → 10000 个顶点 = 执行 10000 次
```

### 片元着色器（Fragment Shader）

```
输入：一个像素（插值后的位置、UV、法线）
输出：这个像素的颜色

做什么：
- 计算光照
- 纹理采样
- 应用颜色效果

每个像素执行一次 → 1920×1080 = 约 200 万次/帧
```

### GPU 与 CPU 的本质差异

```
CPU： 4~16 个核心，每个核心非常快
      适合串行逻辑（if-else、循环、递归）

GPU： 几千个核心，每个核心较慢
      适合并行计算（做同样的操作处理大量数据）
      10000 个顶点 = 10000 个线程并行处理

所以 Shader 里不能有分支？（不全是）
if 语句在 GPU 上会导致"线程束发散"——同一批线程中走不同分支的会等待
尽可能避免在 Shader 中用复杂分支
```

---

## 2. ShaderLab — Unity 的 Shader 语言

Unity 使用 **ShaderLab** 作为 Shader 封装，内部用 HLSL 写逻辑。

```hlsl
// Unity 内置 Shader 模板（Built-in RP）
Shader "Custom/SimpleColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}  // 材质面板上可调参数
        _Color ("Tint Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert           // 顶点着色器入口
            #pragma fragment frag         // 片元着色器入口

            #include "UnityCG.cginc"      // Unity 的常用函数库

            // 顶点着色器输入
            struct appdata
            {
                float4 vertex : POSITION;   // 顶点位置
                float2 uv : TEXCOORD0;      // UV 坐标
            };

            // 从顶点着色器传到片元着色器的数据
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION; // 裁剪空间位置（必须）
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            // 顶点着色器
            v2f vert (appdata v)
            {
                v2f o;
                // 模型空间 → 裁剪空间
                o.vertex = UnityObjectToClipPos(v.vertex);
                // UV 变换（平铺+偏移）
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 片元着色器
            fixed4 frag (v2f i) : SV_Target
            {
                // 采样纹理
                fixed4 col = tex2D(_MainTex, i.uv);
                // 叠加颜色
                col *= _Color;
                return col;
            }
            ENDCG
        }
    }
}
```

### URP 下的 Shader 写法

URP（Universal Render Pipeline）使用不同的包含文件和一些 API 变化：

```hlsl
// URP Shader（URP RP）
Shader "Custom/URPExample"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                // URP 使用 TransformObjectToHClip
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

### Built-in vs URP 关键区别

```
Built-in RP              URP
──────────────           ──────────────
CGPROGRAM                HLSLPROGRAM
#include "UnityCG.cginc" #include "Core.hlsl"
UnityObjectToClipPos     TransformObjectToHClip
tex2D                    SAMPLE_TEXTURE2D
sampler2D                TEXTURE2D + SAMPLER
```

---

## 3. Shader Graph — 可视化编程

Unity 的 **Shader Graph** 让你不用写代码就能创建 Shader：

```
1. 右键 → Create → Shader Graph → URP/Lit Shader Graph
2. 双击打开 Shader Graph 编辑器
3. 用节点连接代替写代码
```

### 常用节点

| 节点 | 作用 |
|------|------|
| Time | 获取时间（做动画效果） |
| Noise | 生成噪声（溶解、地形） |
| Fresnel Effect | 边缘光效果 |
| Sample Texture 2D | 纹理采样 |
| Lerp | 在两个值之间混合 |
| Multiply/Add | 数学运算 |
| Vertex Position | 顶点位置（用于顶点动画） |

### 节点示例：溶解效果

```
                 ┌─────────────┐
Noise 节点 ─────▶│ Step 比较     │───▶ Alpha（透明度）
                 │ threshold   │
Time 节点 ───────▶│ (溶解进度)    │
                 └─────────────┘

等价于 HLSL：
float noise = tex2D(_NoiseTex, uv).r;
clip(noise - _Threshold);
```

---

## 4. HLSL 基础语法

### 数据类型

```hlsl
// 标量
float a = 1.5;
int b = 42;
bool c = true;
half  d = 0.5;   // half = 16位浮点（精度低但快，移动端常用）
fixed e = 0.3;   // fixed = 11位（更省，颜色值够用）

// 向量（最常用）
float2 pos = float2(1, 2);          // 2D 坐标
float3 color = float3(1, 0.5, 0);   // RGB
float4 rgba = float4(1, 0, 0, 1);   // RGBA

// 访问分量
rgba.r   // 红色通道 = 1
rgba.g   // 绿色通道 = 0
rgba.xy  // float2(1, 0)
rgba.xyz // float3(1, 0, 0)

// 矩阵
float3x3 rotationMatrix;
float4x4 viewMatrix;
```

### 常用函数

```hlsl
// 数学
abs(x)      // 绝对值
sin(x)      // 正弦函数
cos(x)      // 余弦
lerp(a,b,t) // 线性插值 = a + (b-a)*t
saturate(x) // 截断到 [0,1] 区间
step(a,x)   // x >= a 返回 1，否则 0
smoothstep(a,b,x) // 平滑过渡

// 向量
dot(a,b)    // 点积
cross(a,b)  // 叉积
normalize(v) // 归一化
length(v)   // 向量长度

// 纹理采样
tex2D(tex, uv)        // Built-in RP 的采样
SAMPLE_TEXTURE2D(tex, sampler, uv)  // URP 的采样
```

### 语义（Semantics）— 数据从哪里来

```hlsl
// 语义告诉 GPU 这个数据从哪里来/到哪里去

// 顶点着色器输入语义（来自顶点缓冲）：
struct appdata
{
    float4 vertex : POSITION;   // 顶点位置（必须）
    float3 normal : NORMAL;     // 法线
    float2 uv : TEXCOORD0;      // UV 坐标
    float4 color : COLOR;       // 顶点颜色
};

// 顶点着色器输出语义（去光栅化器）：
struct v2f
{
    float4 vertex : SV_POSITION; // 裁剪空间位置（必须！）
    float2 uv : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    float3 worldNormal : TEXCOORD2;
};

// 片元着色器输出：
fixed4 frag(v2f i) : SV_Target // SV_Target = 最终像素颜色
```

---

## 5. 效果示例：溶解 Shader

```hlsl
Shader "Custom/Dissolve"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Threshold ("Dissolve Threshold", Range(0,1)) = 0.5
        _EdgeColor ("Edge Color", Color) = (1,1,0,1)
        _EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

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
            sampler2D _NoiseTex;
            float _Threshold;
            float4 _EdgeColor;
            float _EdgeWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float noise = tex2D(_NoiseTex, i.uv).r;

                // 溶解区域：噪声值 < Threshold 的部分不可见
                float dissolve = noise - _Threshold;

                // 边缘发光
                float edge = smoothstep(0, _EdgeWidth, dissolve)
                           - step(0, dissolve);
                col.rgb += _EdgeColor.rgb * edge;

                // 透明度
                col.a = step(0, dissolve);
                return col;
            }
            ENDCG
        }
    }
}
```

---

## 6. 多 Pass 着色器（描边效果）

有些效果需要多个 Pass 才能实现。描边是最经典的多 Pass Shader：

```hlsl
Shader "Custom/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        // Pass 1：描边
        Pass
        {
            // 只渲染背面
            Cull Front
            // 关闭深度写入，让描边在物体内部不显示
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                // 法线外扩顶点（在模型空间）
                v.vertex.xyz += v.normal * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // Pass 2：正常渲染
        Pass
        {
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= _Color;
                return col;
            }
            ENDCG
        }
    }
}
```

### 描边工作原理

```
Pass 1（描边）：
1. 把顶点沿法线方向向外扩（模型变胖了）
2. 只渲染背面（Cull Front）
3. 背面外扩后的部分 = 描边轮廓

Pass 2（正常）：
1. 正常渲染正面
2. 描边在正常物体周围露出来

最终效果：物体周围有一圈黑色描边
```

---

## 7. 顶点动画 — 让顶点"动起来"

在顶点着色器中修改顶点位置可以实现波浪、波动等效果。

### 波浪效果

```hlsl
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

v2f vert (appdata v)
{
    v2f o;
    float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

    // 用顶点的 XZ 坐标和时间计算 Y 轴偏移
    float wave = sin(worldPos.x * _WaveFrequency + _Time.y * _WaveSpeed)
               * cos(worldPos.z * _WaveFrequency + _Time.y * _WaveSpeed)
               * _WaveHeight;

    v.vertex.y += wave;

    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
}
```

### 旗帜/布料波动

```hlsl
// 根据 UV 的 x 值（旗帜水平位置）决定波动幅度
// 固定边（x=0）不动，自由边（x=1）波动最大
float wave = sin(v.uv.x * _WaveFrequency + _Time.y * _WaveSpeed)
           * v.uv.x  // 越靠近固定端幅度越小
           * _WaveHeight;
v.vertex.z += wave;
```

---

## 8. 调试 Shader 的技巧

```hlsl
// 1. 输出纯色测试
fixed4 frag(v2f i) : SV_Target
{
    return fixed4(1, 0, 0, 1); // 红色 → 看到颜色说明 Shader 生效了
}

// 2. 输出 UV 坐标
fixed4 frag(v2f i) : SV_Target
{
    return fixed4(i.uv.x, i.uv.y, 0, 1);
    // 红色 = U，绿色 = V，黑色 = 无UV
}

// 3. 输出法线方向
fixed4 frag(v2f i) : SV_Target
{
    return fixed4(i.normal * 0.5 + 0.5, 1);
    // 法线映射到 0~1 范围显示为颜色
    // 朝上 = 青色 (0.5, 1, 0.5)
    // 朝右 = 红色 (1, 0.5, 0.5)
}

// 4. 输出世界坐标
fixed4 frag(v2f i) : SV_Target
{
    return fixed4(i.worldPos.xyz * 0.1, 1);
    // 可以检查坐标计算是否正确
}
```

---

## 9. 练习

### 练习 1：基础 Shader 修改

```csharp
// 基于 Unity 的 Built-in Shader 模板，做以下修改：
// 1. 让纹理上下翻转（UV.y = 1 - UV.y）
// 2. 让纹理随时间滚动（水流效果）
// 3. 添加一个 _Brightness 参数控制整体亮度
```

### 练习 2：溶解效果扩展

```csharp
// 扩展溶解 Shader：
// 1. 溶解边缘颜色随时间变化（HSB 循环）
// 2. 溶解方向从底部开始（UV.y < Threshold）
// 3. 用 Script 控制 _Threshold 从 0 到 1 实现逐渐消失
```

### 练习 3：顶点动画

```csharp
// 创建一个"呼吸"效果：
// 1. 物体整体缩放动画（在顶点着色器中完成）
// 2. 缩放值 = 1 + sin(time) * 0.05
// 3. 应用到所有顶点位置
// 
// 进阶：做草地随风摆动的效果
// - 每个顶点的偏移量 = sin(时间 + 顶点.x) × 顶点.y（越高摆动越大）
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 顶点着色器 | 控制顶点位置，每个顶点执行一次 |
| 片元着色器 | 控制像素颜色，每个像素执行一次 |
| HLSL | Unity 的着色器编程语言 |
| Shader Graph | 可视化 Shader 编辑，适合非程序员 |
| UV | 纹理坐标 (0~1)，决定如何贴图 |
| SV_Target | 片元着色器的输出——最终颜色 |
| 语义 | 告诉 GPU 数据的来源和去向 |
| URP | 现代 Unity 项目应使用 URP |
| 多 Pass | 多个渲染 Pass 合成最终效果（描边） |
| 顶点动画 | 在顶点着色器中修改顶点位置 |

**对比 Raylib：** Raylib 也支持 GLSL Shader（`LoadShader`、`BeginShaderMode`），但 Unity 用 ShaderLab 封装了 HLSL，写法不同。Raylib 的 Shader 是裸 GLSL，需要手动处理矩阵传参（`SetShaderValueMatrix`）。Unity 的 ShaderLab 帮你自动传递了大部分常用矩阵（`unity_ObjectToWorld`、`UNITY_MATRIX_VP` 等）。核心概念一致——顶点着色器处理顶点位置，片元着色器处理像素颜色。如果你熟悉 Raylib 的 GLSL 基础，迁移到 HLSL 只需要适应语法差异。
