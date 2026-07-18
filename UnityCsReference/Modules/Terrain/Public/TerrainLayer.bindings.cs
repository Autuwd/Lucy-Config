// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 TerrainLayer — 地形图层（地表贴片材质定义）
//
// 📌 作用：
//   TerrainLayer 替代了旧版 SplatPrototype，是 Unity 地形
//   贴片混合系统的核心。每个 TerrainLayer 定义一种地表材质
//   （草地、泥土、岩石、砂砾等），包含 PBR 全套贴图。
//
// 🏗 TerrainLayer 混合系统：
//
//   ┌──────────────────────────────────────────────────────────┐
//   │  TerrainLayer[]                     Alphamap            │
//   │  ┌─────────────────┐              ┌──────────────────┐ │
//   │  │ Layer 0: 草地    │──diffuse──→│                  │ │
//   │  │         normalMap│──normal──→│  [w×h×layers]     │ │
//   │  │         maskMap  │──mask───→│  float 数组        │ │
//   │  ├─────────────────┤              │  所有权重和为1    │ │
//   │  │ Layer 1: 泥土    │              └──────────────────┘ │
//   │  ├─────────────────┤                                     │
//   │  │ Layer 2: 岩石    │              BaseMap               │
//   │  └─────────────────┘              ┌──────────────────┐ │
//   │                                    │ 预烘焙低分辨率    │ │
//   │      每个 Layer 提供：              │ 远距离缩略版     │ │
//   │      - 漫反射(diffuse)             └──────────────────┘ │
//   │      - 法线(normalMap)                                   │
//   │      - Mask贴图(R/M/AO/Smoothness)                       │
//   │      - 平铺尺寸/偏移(tileSize/Offset)                    │
//   │      - PBR参数(specular/metallic/smoothness)             │
//   └──────────────────────────────────────────────────────────┘
//
// 💡 与旧版 SplatPrototype 的区别：
//   - TerrainLayer 是 ScriptableObject（可复用、可独立保存）
//   - 支持完整的 PBR 贴图链（diffuse + normal + mask）
//   - 支持 diffuseRemap / maskMapRemap 颜色重映射
//   - 支持 smoothnessSource（从 diffuse alpha 或常量获取）
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // ==============================================================
    // 🎯 TerrainLayerSmoothnessSource — 平滑度来源选择
    //
    // 📌 控制地形材质的平滑度值从哪里获取：
    //
    //   ConstantMultipliedByDiffuseAlpha (0) — 平滑度常量 × Diffuse Alpha 通道
    //   DiffuseAlphaChannel             (1) — 直接从 Diffuse 贴图的 Alpha 通道读取
    //   ConstantOnly                    (2) — 只用常量值，忽略贴图
    //
    // 💡 不同地表材质的典型选择：
    //   - 草地：DiffuseAlphaChannel（用 alpha 控制光滑区域）
    //   - 岩石：ConstantOnly（统一光滑度）
    //   - 泥土：ConstantMultipliedByDiffuseAlpha（混合控制）
    // ==============================================================
    public enum TerrainLayerSmoothnessSource
    {
        [InspectorName("Constant * Diffuse Alpha")]
        ConstantMultipliedByDiffuseAlpha = 0,

        [InspectorName("Diffuse Alpha Channel")]
        DiffuseAlphaChannel = 1,

        [InspectorName("Constant Only")]
        ConstantOnly = 2
    }

    // ==============================================================
    // 🎯 TerrainLayer — 地形图层的 ScriptableObject
    //
    // 📌 使用方式：
    //   1. 在 Project 窗口中创建 TerrainLayer（Create → Terrain Layer）
    //   2. 在 Terrain 的 Paint Texture 工具中选择
    //   3. 用 Alphamap 控制每层的混合权重
    //
    // 📦 贴图槽位：
    //   - diffuseTexture：   漫反射贴图（RGB = 颜色，A = 可选平滑度来源）
    //   - normalMapTexture： 法线贴图（法线方向细节）
    //   - maskMapTexture：   Mask 贴图（R=金属度, G=环境遮挡, B=高度, A=平滑度）
    //
    // 📐 平铺控制：
    //   - tileSize：   贴图在 terrain 上的平铺大小（世界单位）
    //   - tileOffset： 贴图偏移量（用于对齐不同层）
    //
    // 🎨 PBR 参数：
    //   - specular：   高光颜色
    //   - metallic：   金属度（0=非金属, 1=金属）
    //   - smoothness： 平滑度（0=粗糙, 1=光滑）
    //   - normalScale：法线强度（0=无法线效果）
    //
    // 🔄 颜色重映射：
    //   - diffuseRemapMin/Max：  漫反射贴图的颜色通道重新映射范围
    //   - maskMapRemapMin/Max：  Mask 贴图的通道重新映射范围
    //   - 例：diffuseRemapMin=(0,0,0,0), Max=(1,1,1,1) = 使用原始贴图
    //
    // 🎯 smoothnessSource：
    //   控制平滑度的来源（见 TerrainLayerSmoothnessSource 枚举）
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("TerrainScriptingClasses.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainLayerScriptingInterface.h")]
    public sealed partial class TerrainLayer : Object
    {
        public TerrainLayer() { Internal_Create(this); }

        [FreeFunction("TerrainLayerScriptingInterface::Create")]
        extern private static void Internal_Create([Writable] TerrainLayer layer);

        extern public Texture2D diffuseTexture { get; set; }
        extern public Texture2D normalMapTexture { get; set; }
        extern public Texture2D maskMapTexture { get; set; }
        extern public Vector2 tileSize { get; set; }
        extern public Vector2 tileOffset { get; set; }

        [NativeProperty("SpecularColor")] extern public Color specular { get; set; }

        extern public float metallic { get; set; }
        extern public float smoothness { get; set; }
        extern public float normalScale { get; set; }
        extern public Vector4 diffuseRemapMin { get; set; }
        extern public Vector4 diffuseRemapMax { get; set; }
        extern public Vector4 maskMapRemapMin { get; set; }
        extern public Vector4 maskMapRemapMax { get; set; }
        extern public TerrainLayerSmoothnessSource smoothnessSource { get; set; }
    }
}
