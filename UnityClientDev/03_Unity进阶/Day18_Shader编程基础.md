# Day 18：Shader 编程基础 — 从 HLSL 语法到实战着色器

## 0. 前言：为什么需要学 Shader？

在 Raylib/C++ 中，你可能直接操作顶点缓冲和纹理：

```cpp
// Raylib：手动绘制三角形
rlBegin(RL_TRIANGLES);
    rlColor4ub(255, 0, 0, 255);
    rlVertex3f(-0.5f, -0.5f, 0.0f);
    rlColor4ub(0, 255, 0, 255);
    rlVertex3f(0.5f, -0.5f, 0.0f);
    rlColor4ub(0, 0, 255, 255);
    rlVertex3f(0.0f, 0.5f, 0.0f);
rlEnd();
```

Unity 中，**Shader 定义了 GPU 如何渲染每个像素**。学 Shader 就是学"如何控制 GPU 画东西"。

---

## 1. ShaderLab 结构

Unity Shader 使用 **ShaderLab** 语言编写，它是 HLSL 的包装器：

```hlsl
Shader "Custom/MyShader"
{
    // 属性：Inspector 中可调的参数
    Properties
    {
        _Color ("主颜色", Color) = (1,1,1,1)
        _MainTex ("主纹理", 2D) = "white" {}
        _Glossiness ("光滑度", Range(0,1)) = 0.5
        _Metallic ("金属度", Range(0,1)) = 0.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        
        Pass
        {
            CGPROGRAM
            // 编译指令
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            // 变量声明（与 Properties 对应）
            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Glossiness;
            half _Metallic;
            
            // 顶点输入结构
            struct appdata
            {
                float4 vertex : POSITION;  // 顶点位置
                float3 normal : NORMAL;    // 法线
                float2 uv : TEXCOORD0;     // UV 坐标
            };
            
            // 顶点输出 / 片元输入结构
            struct v2f
            {
                float4 pos : SV_POSITION;  // 裁剪空间位置
                float2 uv : TEXCOORD0;     // 传递 UV
                float3 worldNormal : TEXCOORD1;  // 世界空间法线
            };
            
            // 顶点着色器
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);  // MVP 变换
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);    // UV 变换
                o.worldNormal = UnityObjectToWorldNormal(v.normal);  // 法线变换
                return o;
            }
            
            // 片元着色器
            fixed4 frag (v2f i) : SV_Target
            {
                // 采样纹理
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
    
    // 备选方案：如果 SubShader 不支持，使用这个
    FallBack "Diffuse"
}
```

---

## 2. HLSL 基础语法

### 数据类型

```hlsl
// 基础类型
float   // 32 位浮点（高精度）
half    // 16 位浮点（移动端推荐，范围 -65504~65504）
fixed   // 11 位定点（颜色、归一化向量）
int     // 整数

// 向量类型
float2  // 二维向量 (x,y)
float3  // 三维向量 (x,y,z)
float4  // 四维向量 (x,y,z,w)
// 同理：half2/3/4, fixed2/3/4

// 矩阵
float4x4  // 4x4 矩阵
float3x3  // 3x3 矩阵

// 采样器
sampler2D    // 2D 纹理采样器
samplerCUBE  // 立方体贴图采样器
```

### 语义（Semantics）

```hlsl
// 输入语义（appdata）
POSITION    // 顶点位置
NORMAL      // 法线
TANGENT     // 切线
TEXCOORD0   // 第一套 UV
TEXCOORD1   // 第二套 UV
COLOR       // 顶点颜色

// 输出语义（v2f）
SV_POSITION  // 裁剪空间位置（必须）
SV_Target    // 输出颜色（片元着色器返回值）
TEXCOORDn    // 传递数据到下一个阶段
```

### 常用内置函数

```hlsl
// 变换函数
UnityObjectToClipPos(float4 pos)  // 物体空间 → 裁剪空间
UnityObjectToWorldNormal(float3 normal)  // 物体空间法线 → 世界空间
UnityWorldToViewPos(float3 pos)   // 世界空间 → 观察空间

// 纹理采样
tex2D(sampler2D tex, float2 uv)   // 2D 纹理采样
texCUBE(samplerCUBE tex, float3 dir)  // 立方体贴图采样

// 数学函数
sin(x), cos(x), tan(x)
pow(x, y)     // x 的 y 次方
sqrt(x)       // 平方根
rsqrt(x)      // 1/sqrt(x)（快速倒数平方根）
dot(a, b)     // 点积
cross(a, b)   // 叉积（仅 float3）
normalize(v)  // 归一化
lerp(a, b, t) // 线性插值
saturate(x)   // clamp(x, 0, 1)
```

---

## 3. 顶点着色器详解

### 核心任务：坐标变换

```hlsl
v2f vert (appdata v)
{
    v2f o;
    
    // 1. 模型空间 → 裁剪空间（MVP 变换）
    // Unity 内置函数，等价于：mul(UNITY_MATRIX_MVP, v.vertex)
    o.pos = UnityObjectToClipPos(v.vertex);
    
    // 2. 传递其他数据到片元着色器
    o.uv = v.uv;
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    
    return o;
}
```

### 手动实现 MVP 变换（理解原理）

```hlsl
// Unity 内置的变换矩阵：
// UNITY_MATRIX_M   - 模型矩阵（物体→世界）
// UNITY_MATRIX_V   - 观察矩阵（世界→相机）
// UNITY_MATRIX_P   - 投影矩阵（相机→裁剪）
// UNITY_MATRIX_MVP - Model × View × Projection

v2f vert (appdata v)
{
    v2f o;
    
    // 手动 MVP 变换
    float4 worldPos = mul(UNITY_MATRIX_M, v.vertex);      // 物体→世界
    float4 viewPos = mul(UNITY_MATRIX_V, worldPos);       // 世界→观察
    o.pos = mul(UNITY_MATRIX_P, viewPos);                 // 观察→裁剪
    
    // 等价于：o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
    
    return o;
}
```

---

## 4. 片元着色器详解

### 核心任务：计算像素颜色

```hlsl
fixed4 frag (v2f i) : SV_Target
{
    // 基础颜色
    fixed4 col = fixed4(1, 0, 0, 1);  // 红色
    
    // 输出到屏幕
    return col;
}
```

### 法线可视化（调试常用）

```hlsl
fixed4 frag (v2f i) : SV_Target
{
    // 法线范围是 [-1, 1]，映射到 [0, 1] 显示
    float3 normal = normalize(i.worldNormal);
    fixed4 col = fixed4(normal * 0.5 + 0.5, 1.0);
    return col;
}
```

### 基础光照（Lambert）

```hlsl
fixed4 frag (v2f i) : SV_Target
{
    // 归一化法线
    float3 worldNormal = normalize(i.worldNormal);
    
    // 光源方向（方向光）
    float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
    
    // 漫反射 = max(0, dot(N, L))
    float NdotL = saturate(dot(worldNormal, lightDir));
    
    // 颜色 = 纹理 × 光照 × 光源颜色
    fixed4 texColor = tex2D(_MainTex, i.uv);
    fixed4 col = texColor * _Color * NdotL * _LightColor0;
    
    return col;
}
```

---

## 5. 实战 Shader 模板

### Unlit Shader（无光照）

```hlsl
Shader "Custom/Unlit"
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
            
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
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
```

### 透明 Shader

```hlsl
Shader "Custom/Transparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        Pass
        {
            // 关闭深度写入
            ZWrite Off
            // Alpha 混合
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
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
```

### 描边 Shader（卡通渲染）

```hlsl
Shader "Custom/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineWidth ("描边宽度", Range(0, 0.1)) = 0.02
        _OutlineColor ("描边颜色", Color) = (0,0,0,1)
    }
    SubShader
    {
        // Pass 1：渲染描边（背面剔除 + 法线外扩）
        Pass
        {
            Cull Front  // 剔除正面，只渲染背面（描边）
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            float _OutlineWidth;
            fixed4 _OutlineColor;
            
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };
            
            v2f vert (appdata v)
            {
                v2f o;
                // 法线外扩
                float3 pos = v.vertex.xyz + normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }
        
        // Pass 2：渲染正面（正常纹理）
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
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
```

---

## 6. C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity Shader (HLSL) |
|------|-------------|---------------------|
| 着色器创建 | `LoadShaderCode()` | ShaderLab 文本 |
| 绑定着色器 | `BeginShaderMode()` | `material.shader = shader` |
| 传递参数 | `SetShaderValue()` | `material.SetFloat()` |
| 纹理采样 | 手动在 shader 中写 | `tex2D(sampler, uv)` |
| 坐标变换 | 手动传 MVP 矩阵 | `UnityObjectToClipPos()` |
| 顶点数据 | `rlBegin/rlVertex` | `appdata` 结构体 |
| 输出颜色 | 片元着色器返回 | `SV_Target` 语义 |

## 停靠点

> ShaderLab = HLSL 包装器，`Properties` 定义 Inspector 参数，`Pass` 包含实际着色器代码。
> 顶点着色器：MVP 变换，将物体空间顶点投影到裁剪空间。
> 片元着色器：计算每个像素的颜色，可做纹理采样、光照、特效。
> HLSL 数据类型：`float`(高精度)、`half`(移动端)、`fixed`(颜色)。
> 语义：`POSITION`(输入)、`SV_POSITION`(输出)、`TEXCOORD`(传递数据)。

## 练习建议

1. **修改 Unlit Shader**：添加 UV 滚动效果（`uv += _Time.y * speed`）
2. **实现渐变色**：根据世界空间 Y 坐标插值两种颜色
3. **添加溶解效果**：使用噪声纹理 + clip() 函数
