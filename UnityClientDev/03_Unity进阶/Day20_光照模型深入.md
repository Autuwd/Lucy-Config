# Day 20：光照模型深入 — 从 Phong 到 PBR 的数学推导

## 0. 前言：光照的本质

在 Day12 我们学了 Unity 的 Light 组件。今天我们深入**数学原理**——GPU 到底怎么计算光照？

现实世界中，光线从光源出发，与物体表面交互，最终进入眼睛。计算机图形学用**光照模型**近似这个过程。

---

## 1. 光照组成

```
最终颜色 = 环境光 + 漫反射 + 高光反射 + 自发光

I = Ia*Ka + Id*Kd*(L·N) + Is*Ks*(R·V)^n + Ie

其中：
Ia, Id, Is = 环境光、漫反射光、高光光的强度
Ka, Kd, Ks = 物体的环境、漫反射、高光反射系数
L = 光源方向（指向光源）
N = 表面法线
R = 反射方向
V = 视线方向（指向相机）
n = 高光指数（越大高光越集中）
```

---

## 2. Phong 光照模型

### 核心思想

```
1. 环境光：模拟间接光照的近似值
   Ambient = Ka * Ia

2. 漫反射：Lambert 余弦定律
   Diffuse = Kd * Ia * max(0, L·N)

3. 高光反射：光线反射后集中向眼睛方向
   Specular = Ks * Is * max(0, R·V)^n
```

### 数学推导

```
反射向量 R 的计算：
R = 2*(L·N)*N - L

其中：
L = 光源方向（从表面指向光源）
N = 表面法线（归一化）
R = 反射方向（从表面指向反射光方向）
```

### Phong Shader 实现

```hlsl
// Phong 光照模型
fixed4 frag (v2f i) : SV_Target
{
    float3 N = normalize(i.worldNormal);
    float3 L = normalize(_WorldSpaceLightPos0.xyz);
    float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
    float3 R = reflect(-L, N);  // 反射向量
    
    // 环境光
    float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb;
    
    // 漫反射（Lambert）
    float NdotL = saturate(dot(N, L));
    float3 diffuse = _LightColor0.rgb * NdotL;
    
    // 高光反射（Phong）
    float RdotV = saturate(dot(R, V));
    float3 specular = _LightColor0.rgb * pow(RdotV, _Shininess);
    
    // 最终颜色
    fixed4 col;
    col.rgb = ambient + diffuse + specular;
    col.a = 1.0;
    return col;
}
```

---

## 3. Blinn-Phong 光照模型

### 核心改进

Blinn 用**半角向量 H** 代替反射向量 R，计算更快：

```
H = normalize(L + V)  // 半角向量（光源和视线的中间方向）

Specular = Ks * Is * max(0, N·H)^n
```

### 为什么 Blinn-Phong 更好？

1. **计算更快**：不需要计算反射向量 R
2. **更自然**：当光源在相机后方时，Phong 会出现异常
3. **Unity 默认使用**：Standard Shader 使用 Blinn-Phong 的变体

### Blinn-Phong Shader 实现

```hlsl
// Blinn-Phong 光照模型
fixed4 frag (v2f i) : SV_Target
{
    float3 N = normalize(i.worldNormal);
    float3 L = normalize(_WorldSpaceLightPos0.xyz);
    float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
    float3 H = normalize(L + V);  // 半角向量
    
    // 漫反射
    float NdotL = saturate(dot(N, L));
    float3 diffuse = _LightColor0.rgb * NdotL;
    
    // 高光反射（Blinn-Phong）
    float NdotH = saturate(dot(N, H));
    float3 specular = _LightColor0.rgb * pow(NdotH, _Shininess);
    
    // 最终颜色
    fixed4 col;
    col.rgb = diffuse + specular;
    col.a = 1.0;
    return col;
}
```

---

## 4. PBR 光照模型（Physically Based Rendering）

### 核心思想

PBR 基于物理定律，用两个参数描述几乎所有材质：
- **金属度 (Metallic)**：0 = 非金属，1 = 金属
- **粗糙度 (Roughness)**：0 = 光滑，1 = 粗糙

### BRDF（双向反射分布函数）

```
BRDF 定义了光线如何从表面反射：

出射辐射亮度 = ∫ BRDF(L, V) * 入射辐射亮度 * cos(θi) dΩ

其中：
L = 入射光方向
V = 出射方向（看向相机）
θi = 入射角（光线与法线的夹角）
```

### Cook-Torrance 高光 BRDF

PBR 使用 Cook-Torrance 模型计算高光反射：

```
f_spec(L, V) = D(h) * G(L, V) * F(L, V) / (4 * (N·L) * (N·V))

其中：
D = 法线分布函数（微表面法线的统计分布）
G = 几何遮挡函数（微表面之间的遮挡）
F = 菲涅尔方程（不同角度的反射率）
h = 半角向量（L + V 的归一化）
```

### 法线分布函数 D（GGX/Trowbridge-Reitz）

```hlsl
// GGX 法线分布函数
// 描述微表面法线与宏观法线一致的概率
float DistributionGGX(float3 N, float3 H, float roughness)
{
    float a = roughness * roughness;  // α = roughness²
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    
    float num = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = 3.14159265 * denom * denom;
    
    return num / denom;
}
```

### 几何遮挡函数 G（Schlick-GGX）

```hlsl
// Schlick-GGX 几何遮挡函数
// 描述微表面之间的遮挡和阴影
float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;  // 直接光照的 k 值
    
    float num = NdotV;
    float denom = NdotV * (1.0 - k) + k;
    
    return num / denom;
}

float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);
    
    return ggx1 * ggx2;
}
```

### 菲涅尔方程 F（Schlick 近似）

```hlsl
// Schlick 菲涅尔近似
// 描述不同角度的反射率（掠射角反射更强）
float3 fresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

// F0 = 基础反射率（垂直入射时的反射率）
// 非金属：F0 ≈ 0.04
// 金属：F0 ≈ 0.5 ~ 1.0（通常是彩色的）
```

### 完整 PBR Shader

```hlsl
// PBR 光照模型
fixed4 frag (v2f i) : SV_Target
{
    float3 N = normalize(i.worldNormal);
    float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
    
    // 金属度和粗糙度
    float metallic = _Metallic;
    float roughness = _Roughness;
    
    // 基础反射率
    float3 F0 = float3(0.04, 0.04, 0.04);
    F0 = lerp(F0, albedo.rgb, metallic);
    
    // 反射率方程（简化：单光源）
    float3 L = normalize(lightPos - i.worldPos);
    float3 H = normalize(V + L);
    float distance = length(lightPos - i.worldPos);
    float attenuation = 1.0 / (distance * distance);
    float3 radiance = lightColor * attenuation;
    
    // Cook-Torrance BRDF
    float D = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    float3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);
    
    float3 numerator = D * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
    float3 specular = numerator / denominator;
    
    // 能量守恒
    float3 kS = F;           // 镜面反射比例
    float3 kD = float3(1.0, 1.0, 1.0) - kS;  // 漫反射比例
    kD *= (1.0 - metallic);  // 金属没有漫反射
    
    // 漫反射
    float NdotL = max(dot(N, L), 0.0);
    float3 Lo = (kD * albedo / 3.14159265 + specular) * radiance * NdotL;
    
    // 环境光（简化）
    float3 ambient = float3(0.03, 0.03, 0.03) * albedo;
    float3 color = ambient + Lo;
    
    // HDR 色调映射
    color = color / (color + float3(1.0, 1.0, 1.0));
    // Gamma 校正
    color = pow(color, float3(1.0/2.2, 1.0/2.2, 1.0/2.2));
    
    return fixed4(color, 1.0);
}
```

---

## 5. 能量守恒

```
PBR 的核心原则：能量守恒

入射光能量 = 反射光能量 + 吸收光能量

在 Shader 中体现：
- 漫反射 + 高光反射 ≤ 1
- kD + kS ≤ 1（kD = 1 - kS）
- 金属只有高光反射（kD = 0）
- 非金属既有漫反射也有高光反射
```

---

## 6. C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity PBR |
|------|-------------|-----------|
| 光照模型 | 简单 Lambert | Cook-Torrance BRDF |
| 材质参数 | 颜色 + 纹理 | Metallic + Roughness |
| 高光计算 | Blinn-Phong | GGX 法线分布 |
| 能量守恒 | 无 | 严格遵守 |
| 菲涅尔 | 无 | Schlick 近似 |
| 色调映射 | 无 | HDR → LDR |

## 停靠点

> Phong：环境 + 漫反射 + 高光（R·V）。
> Blinn-Phong：用半角向量 H 代替 R，更高效、更自然。
> PBR 核心：Cook-Torrance BRDF = D × G × F。
> GGX：微表面法线分布，产生真实的高光形状。
> 能量守恒：漫反射 + 高光 ≤ 1，金属无漫反射。

## 练习建议

1. **实现完整的 Blinn-Phong**：添加多光源支持
2. **对比 Phong vs Blinn-Phong**：观察高光形状差异
3. **简化版 PBR**：只实现 GGX + 菲涅尔，跳过几何函数
