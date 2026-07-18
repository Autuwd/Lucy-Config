// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 TerrainData — Unity 地形数据容器（ScriptableObject）
//
// 📌 作用：
//   TerrainData 是地形数据的核心存储对象。
//   它不负责渲染，只负责存储和提供地形所有子系统的数据访问。
//   一个 TerrainData 可以被多个 Terrain 引用吗？不行，是一对一关系。
//
// 🏗 地形数据管线（Terrain Data Pipeline）：
//
//   高度图 → Alphamap(Splatmap) → Detail Layer → Tree Instances
//   (Heightmap)  (地表纹理混合)    (草地/小花)   (树木实例)
//       ↓              ↓               ↓              ↓
//   地形形状       地表外观       植被分布       树木位置
//
//   1️⃣ Heightmap（高度图）：
//      - 存储地形的高度信息，决定地形形状
//      - 分辨率 = heightmapResolution（如 513 = 512+1）
//      - 值范围 0~1，乘以 size.y 得到世界高度
//      - 还包含 Holes 纹理（控制哪些区域不生成网格）
//
//   2️⃣ Alphamap（Splatmap / 地表纹理混合贴图）：
//      - 存储每层 TerrainLayer 的混合权重
//      - 维度：[alphamapResolution × alphamapResolution × alphamapLayers]
//      - 所有权重之和为 1（归一化）
//      - BaseMap 是预烘焙的低分辨率版本，用于远距离渲染
//
//   3️⃣ Detail Layer（细节层 / 草地 / 小花）：
//      - 存储每个 DetailPrototype 在每个像素位置的密度
//      - 使用 int 数组，值越大表示该位置该种类的密度越高
//      - 有 CoverageMode（密度覆盖）和 InstanceCountMode（实例计数）两种模式
//
//   4️⃣ Tree Instances（树木实例）：
//      - TreeInstance 结构体数组，每个包含：位置、缩放、旋转、颜色
//      - prototypeIndex 指向 TreePrototype 数组中的对应树种
//      - 支持 GPU Instancing 高性能渲染
//
// 💡 命名规范说明：
//   C# 端使用 "Alphamap" 术语（字母 Alpha），
//   C++ 端对应 "Splatmap" 术语（贴片混合地图），
//   两者是同一个概念的不同叫法。
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // ==============================================================
    // 🌳 TreePrototype — 树木原型定义
    //
    // 📌 作用：
    //   定义一种树木的原型（预制体 + 参数）。
    //   每个 TreeInstance 通过 prototypeIndex 引用这里定义的原型。
    //
    // 💡 属性说明：
    //   - prefab：树木的 GameObject 预制体（必须包含 Tree 组件）
    //   - bendFactor：树木在风中弯曲的程度（0=不弯曲）
    //   - navMeshLod：用于生成 NavMesh 的 LOD 级别
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeAsStruct]
    public sealed partial class TreePrototype
    {
        [NativeName("prefab")]
        internal GameObject m_Prefab;
        [NativeName("bendFactor")]
        internal float m_BendFactor;
        [NativeName("navMeshLod")]
        internal int m_NavMeshLod;

        public GameObject prefab { get { return m_Prefab; } set { m_Prefab = value; } }

        public float bendFactor { get { return m_BendFactor; } set { m_BendFactor = value; } }

        public int navMeshLod { get { return m_NavMeshLod; } set { m_NavMeshLod = value; } }

        public TreePrototype() {}

        public TreePrototype(TreePrototype other)
        {
            prefab = other.prefab;
            bendFactor = other.bendFactor;
            navMeshLod = other.navMeshLod;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TreePrototype);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private bool Equals(TreePrototype other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(other, this))
                return true;

            if (GetType() != other.GetType())
                return false;

            bool equals = prefab == other.prefab &&
                bendFactor == other.bendFactor &&
                navMeshLod == other.navMeshLod;

            return equals;
        }

        internal bool Validate(out string errorMessage)
            => ValidateTreePrototype(this, out errorMessage);

        [FreeFunction("TerrainDataScriptingInterface::ValidateTreePrototype")]
        extern internal static bool ValidateTreePrototype([NotNull] TreePrototype prototype, out string errorMessage);
    }

    // ==============================================================
    // 🌿 DetailRenderMode — 细节物体渲染模式
    //
    //   GrassBillboard (0) — 使用公告板（始终面向摄像机）渲染草地
    //   VertexLit      (1) — 逐顶点光照的 3D 模型
    //   Grass          (2) — 交错排列的草地片（默认模式）
    // ==============================================================
    public enum DetailRenderMode
    {
        GrassBillboard = 0,
        VertexLit = 1,
        Grass = 2
    }

    // ==============================================================
    // 📊 DetailScatterMode — 细节物体散射模式
    //
    //   CoverageMode (0) — 覆盖模式：targetCoverage 控制密度比例
    //   InstanceCountMode (1) — 实例计数模式：int[,] 直接控制实例数
    // ==============================================================
    public enum DetailScatterMode
    {
        CoverageMode = 0,
        InstanceCountMode = 1
    }

    // should match TreeMotionVectorModeOverride enum in Terrain.h
    public enum TreeMotionVectorModeOverride
    {
        CameraMotionOnly = 0,
        PerObjectMotion = 1,
        ForceNoMotion = 2,
        InheritFromPrototype = 3,
    }

    // ==============================================================
    // 🌿 DetailPrototype — 细节物体（草地/小花）原型定义
    //
    // 📌 作用：
    //   定义一种细节物体的渲染方式、颜色、大小、密度。
    //   每个 DetailPrototype 可以基于纹理（草地公告板）或
    //   基于 Mesh（3D 小花/石头）渲染。
    //
    // 🏗 两种渲染方式（由 usePrototypeMesh 控制）：
    //   - Mesh 模式：prototype（GameObject），使用 GPU Instancing
    //   - Texture 模式：prototypeTexture（Texture2D），公告板渲染
    //
    // 💡 关键参数：
    //   - healthyColor/dryColor：健康/枯萎时的颜色插值
    //   - minWidth/maxWidth/minHeight/maxHeight：随机缩放范围
    //   - density：密度乘数（影响分布数量）
    //   - noiseSeed/noiseSpread：控制随机分布的模式
    //   - targetCoverage：CoverageMode 下目标覆盖率
    //   - alignToGround/positionJitter：是否贴合地形 + 随机位置偏移
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("TerrainScriptingClasses.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainDataScriptingInterface.h")]
    [UsedByNativeCode]
    [NativeAsStruct]
    public sealed partial class DetailPrototype
    {
        internal static readonly Color DefaultHealthColor = new Color(67 / 255F, 249 / 255F, 42 / 255F, 1);
        internal static readonly Color DefaultDryColor = new Color(205 / 255.0F, 188 / 255.0F, 26 / 255.0F, 1.0F);

        [NativeName("prototype")]
        internal GameObject m_Prototype = null;
        [NativeName("prototypeTexture")]
        internal Texture2D m_PrototypeTexture = null;
        [NativeName("healthyColor")]
        internal Color m_HealthyColor = DefaultHealthColor;
        [NativeName("dryColor")]
        internal Color m_DryColor = DefaultDryColor;
        [NativeName("minWidth")]
        internal float m_MinWidth = 1.0F;
        [NativeName("maxWidth")]
        internal float m_MaxWidth = 2.0F;
        [NativeName("minHeight")]
        internal float m_MinHeight = 1F;
        [NativeName("maxHeight")]
        internal float m_MaxHeight = 2F;
        [NativeName("noiseSeed")]
        internal int m_NoiseSeed = 0;
        [NativeName("noiseSpread")]
        internal float m_NoiseSpread = 0.1F;
        [NativeName("density")]
        internal float m_Density = 1.0F;
        [NativeName("holeTestRadius")]
        internal float m_HoleEdgePadding = 0.0F;
        [NativeName("renderMode")]
        internal int m_RenderMode = 2;
        [NativeName("usePrototypeMesh")]
        internal int m_UsePrototypeMesh = 0;
        [NativeName("useInstancing")]
        internal int m_UseInstancing = 0;
        [NativeName("useDensityScaling")]
        internal int m_UseDensityScaling = 0;
        [NativeName("alignToGround")]
        internal float m_AlignToGround = 0;
        [NativeName("positionJitter")]
        internal float m_PositionJitter = 0;
        [NativeName("targetCoverage")]
        internal float m_TargetCoverage = 1.0F;

        public GameObject prototype { get { return m_Prototype; } set { m_Prototype = value; } }

        public Texture2D prototypeTexture { get { return m_PrototypeTexture; } set { m_PrototypeTexture = value; } }

        public float minWidth { get { return m_MinWidth; } set { m_MinWidth = value; } }

        public float maxWidth { get { return m_MaxWidth; } set { m_MaxWidth = value; } }

        public float minHeight { get { return m_MinHeight; } set { m_MinHeight = value; } }

        public float maxHeight { get { return m_MaxHeight; } set { m_MaxHeight = value; } }

        public int noiseSeed { get { return m_NoiseSeed; } set { m_NoiseSeed = value; } }

        public float noiseSpread { get { return m_NoiseSpread; } set { m_NoiseSpread = value; } }

        public float density { get { return m_Density; } set { m_Density = value; } }

        [Obsolete("bendFactor has no effect and is deprecated.", false)]
        public float bendFactor { get { return 0.0f; } set {} }

        public float holeEdgePadding { get { return m_HoleEdgePadding; } set { m_HoleEdgePadding = value; } }

        public Color healthyColor { get { return m_HealthyColor; } set { m_HealthyColor = value; } }

        public Color dryColor { get { return m_DryColor; } set { m_DryColor = value; } }

        public DetailRenderMode renderMode { get { return (DetailRenderMode)m_RenderMode; } set { m_RenderMode = (int)value; } }

        public bool usePrototypeMesh { get { return m_UsePrototypeMesh != 0; } set { m_UsePrototypeMesh = value ? 1 : 0; } }

        public bool useInstancing {
            get { return m_UseInstancing != 0; }
            set { m_UseInstancing = value ? 1 : 0; }
        }

        public float targetCoverage {
            get { return m_TargetCoverage; }
            set { m_TargetCoverage = value; } }

        public bool useDensityScaling { get { return m_UseDensityScaling != 0; } set { m_UseDensityScaling = value ? 1 : 0; } }

        public float alignToGround { get { return m_AlignToGround; } set { m_AlignToGround = value; } }

        public float positionJitter { get { return m_PositionJitter; } set { m_PositionJitter = value; } }

        public DetailPrototype() {}

        public DetailPrototype(DetailPrototype other)
        {
            m_Prototype = other.m_Prototype;
            m_PrototypeTexture = other.m_PrototypeTexture;
            m_HealthyColor = other.m_HealthyColor;
            m_DryColor = other.m_DryColor;
            m_MinWidth = other.m_MinWidth;
            m_MaxWidth = other.m_MaxWidth;
            m_MinHeight = other.m_MinHeight;
            m_MaxHeight = other.m_MaxHeight;
            m_NoiseSeed = other.m_NoiseSeed;
            m_NoiseSpread = other.m_NoiseSpread;
            m_Density = other.m_Density;
            m_HoleEdgePadding = other.m_HoleEdgePadding;
            m_RenderMode = other.m_RenderMode;
            m_UsePrototypeMesh = other.m_UsePrototypeMesh;
            m_UseInstancing = other.m_UseInstancing;
            m_UseDensityScaling = other.m_UseDensityScaling;
            m_AlignToGround = other.m_AlignToGround;
            m_PositionJitter = other.m_PositionJitter;
            m_TargetCoverage = other.m_TargetCoverage;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DetailPrototype);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private bool Equals(DetailPrototype other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(other, this))
                return true;

            if (GetType() != other.GetType())
                return false;

            return m_Prototype == other.m_Prototype
                && m_PrototypeTexture == other.m_PrototypeTexture
                && m_HealthyColor == other.m_HealthyColor
                && m_DryColor == other.m_DryColor
                && m_MinWidth == other.m_MinWidth
                && m_MaxWidth == other.m_MaxWidth
                && m_MinHeight == other.m_MinHeight
                && m_MaxHeight == other.m_MaxHeight
                && m_NoiseSeed == other.m_NoiseSeed
                && m_NoiseSpread == other.m_NoiseSpread
                && m_Density == other.m_Density
                && m_HoleEdgePadding == other.m_HoleEdgePadding
                && m_RenderMode == other.m_RenderMode
                && m_UsePrototypeMesh == other.m_UsePrototypeMesh
                && m_UseInstancing == other.m_UseInstancing
                && m_TargetCoverage == other.m_TargetCoverage
                && m_UseDensityScaling == other.m_UseDensityScaling;
        }

        public bool Validate()
            => ValidateDetailPrototype(this, out _);

        public bool Validate(out string errorMessage)
            => ValidateDetailPrototype(this, out errorMessage);

        [FreeFunction("TerrainDataScriptingInterface::ValidateDetailPrototype")]
        extern internal static bool ValidateDetailPrototype([NotNull] DetailPrototype prototype, out string errorMessage);

        internal bool ValidateTextures(out string errorMessage)
            => ValidateDetailPrototypeTextures(this, out errorMessage);

        [FreeFunction("TerrainDataScriptingInterface::ValidateDetailPrototypeTextures")]
        extern internal static bool ValidateDetailPrototypeTextures([NotNull] DetailPrototype prototype, out string errorMessage);

        internal bool ValidateMesh(out string errorMessage)
            => ValidateDetailPrototypeMesh(this, out errorMessage);

        [FreeFunction("TerrainDataScriptingInterface::ValidateDetailPrototypeMesh")]
        extern internal static bool ValidateDetailPrototypeMesh([NotNull] DetailPrototype prototype, out string errorMessage);

        internal static bool IsModeSupportedByRenderPipeline(DetailRenderMode renderMode, bool useInstancing, out string errorMessage)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                if (renderMode == DetailRenderMode.GrassBillboard && GraphicsSettings.GetDefaultShader(DefaultShaderType.TerrainDetailGrassBillboard) == null)
                {
                    errorMessage = "The current render pipeline does not support Billboard details. Details will not be rendered.";
                    return false;
                }
                else if (renderMode == DetailRenderMode.VertexLit && !useInstancing && GraphicsSettings.GetDefaultShader(DefaultShaderType.TerrainDetailLit) == null)
                {
                    errorMessage = "The current render pipeline does not support VertexLit details. Details will be rendered using the default shader.";
                    return false;
                }
                else if (renderMode == DetailRenderMode.Grass && GraphicsSettings.GetDefaultShader(DefaultShaderType.TerrainDetailGrass) == null)
                {
                    errorMessage = "The current render pipeline does not support Grass details. Details will be rendered using the default shader without alpha test and animation.";
                    return false;
                }
            }
            errorMessage = string.Empty;
            return true;
        }
    }
    // ==============================================================
    // ⚠️ SplatPrototype — [已废弃] 地表贴片原型
    //
    // 📌 说明：
    //   这是旧版地形系统的地表贴片定义，已被 TerrainLayer 取代。
    //   保留仅用于兼容旧项目。
    //
    // 📦 属性：
    //   - texture/normalMap：漫反射贴图/法线贴图
    //   - tileSize/tileOffset：平铺大小/偏移
    //   - specular/metallic/smoothness：PBR 参数
    // ==============================================================
    [Obsolete("SplatPrototype is obsolete. Use TerrainLayer instead.", false)]
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeAsStruct]
    public sealed partial class SplatPrototype
    {
        [NativeName("texture")]
        internal Texture2D m_Texture;
        [NativeName("normalMap")]
        internal Texture2D m_NormalMap;
        [NativeName("tileSize")]
        internal Vector2 m_TileSize = new Vector2(15, 15);
        [NativeName("tileOffset")]
        internal Vector2 m_TileOffset = new Vector2(0, 0);
        [NativeName("specularMetallic")]
        internal Vector4 m_SpecularMetallic = new Vector4(0, 0, 0, 0);
        [NativeName("smoothness")]
        internal float m_Smoothness = 0.0f;

        public Texture2D texture { get { return m_Texture; } set { m_Texture = value; } }

        public Texture2D normalMap { get { return m_NormalMap; } set { m_NormalMap = value; } }

        public Vector2 tileSize { get { return m_TileSize; } set { m_TileSize = value; } }

        public Vector2 tileOffset { get { return m_TileOffset; } set { m_TileOffset = value; } }

        public Color specular { get { return new Color(m_SpecularMetallic.x, m_SpecularMetallic.y, m_SpecularMetallic.z); } set { m_SpecularMetallic.x = value.r; m_SpecularMetallic.y = value.g; m_SpecularMetallic.z = value.b; } }

        public float metallic { get { return m_SpecularMetallic.w; } set { m_SpecularMetallic.w = value; } }

        public float smoothness { get { return m_Smoothness; } set { m_Smoothness = value; } }
    }

    // ==============================================================
    // 🌳 TreeInstance — 树木实例数据结构
    //
    // 📌 作用：
    //   存储单个树木的位置、缩放、旋转和颜色信息。
    //   所有 TreeInstance 组成 treeInstances 数组。
    //
    // 💡 属性说明：
    //   - position：地形局部坐标（x, z 0~1，y = 地形高度比）
    //   - widthScale/heightScale：缩放（可单独控制宽高）
    //   - rotation：绕 Y 轴旋转角度
    //   - color/lightmapColor：颜色和光照贴图颜色
    //   - prototypeIndex：指向 TreePrototype[] 的索引
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public partial struct TreeInstance
    {
        public Vector3 position;

        public float widthScale;

        public float heightScale;

        public float rotation;

        public Color32 color;

        public Color32 lightmapColor;

        public int prototypeIndex;

        internal float temporaryDistance;
    }

    // ==============================================================
    // 📏 PatchExtents — 地形 Patch 的高度范围
    //
    // 📌 作用：
    //   存储地形的一个 Patch 块的最小/最大高度。
    //   用于 LOD 选择和视锥体裁剪优化。
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public struct PatchExtents
    {
        internal float m_min;
        internal float m_max;

        public float min { get { return m_min; } set { m_min = value; } }
        public float max { get { return m_max; } set { m_max = value; } }
    }

    // ==============================================================
    // 🔄 TerrainHeightmapSyncControl — 高度图同步控制
    //
    //   None (0) — 不同步
    //   HeightOnly (1) — 仅同步高度数据
    //   HeightAndLod (2) — 同步高度 + LOD
    //
    // 📌 用于 SetHeightsDelayLOD + DirtyHeightmapRegion 等延迟更新 API。
    // ==============================================================
    public enum TerrainHeightmapSyncControl
    {
        None = 0,
        HeightOnly,
        HeightAndLod
    }

    // ==============================================================
    // 📦 DetailInstanceTransform — 细节实例的变换数据
    //
    // 📌 作用：
    //   ComputeDetailInstanceTransforms() 返回的每个细节实例的变换。
    //   用于 Job System 和 DOTS 中直接访问细节物体的位置和旋转。
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public struct DetailInstanceTransform
    {
        public float posX;
        public float posY;
        public float posZ;
        public float scaleXZ;
        public float scaleY;
        public float rotationY;
    }

    // ==============================================================
    // 🎯 TerrainData — 地形数据容器（核心数据类）
    //
    // 📌 作用：
    //   TerrainData 是一个 ScriptableObject，包含地形所有数据。
    //   Terrain 组件引用 TerrainData，渲染其中的数据。
    //
    // 🏗 内部子系统（对应 C++ 端的 5 个数据库）：
    //
    //   ┌─────────────────────────────────────────────────┐
    //   │  TerrainData                                    │
    //   │  ┌───────────┐  ┌──────────┐  ┌──────────────┐ │
    //   │  │ Heightmap │  │ SplatDB  │  │ DetailDB     │ │
    //   │  │ (地形形状) │  │ (地表贴图)│  │ (草地/小花)  │ │
    //   │  └───────────┘  └──────────┘  └──────────────┘ │
    //   │  ┌───────────┐  ┌──────────┐                   │
    //   │  │ TreeDB    │  │ Terrain  │                   │
    //   │  │ (树木)    │  │ Layer[]  │                   │
    //   │  └───────────┘  └──────────┘                   │
    //   └─────────────────────────────────────────────────┘
    //
    // ⚡ 关键常量（从 C++ 端获取的边界值）：
    //   k_MaximumResolution = 最大 heightmap 分辨率
    //   k_MaximumAlphamapResolution = 最大 alphamap 分辨率
    //   k_MaximumDetailPatchCount = 最大细节 Patch 数
    // ==============================================================
    [NativeHeader("TerrainScriptingClasses.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainDataScriptingInterface.h")]
    [UsedByNativeCode]
    public sealed partial class TerrainData : Object
    {
        private const string k_ScriptingInterfaceName = "TerrainDataScriptingInterface";
        private const string k_ScriptingInterfacePrefix = k_ScriptingInterfaceName + "::";
        private const string k_HeightmapPrefix = "GetHeightmap().";
        private const string k_DetailDatabasePrefix = "GetDetailDatabase().";
        private const string k_TreeDatabasePrefix = "GetTreeDatabase().";
        private const string k_SplatDatabasePrefix = "GetSplatDatabase().";

        private enum BoundaryValueType
        {
            // THESE VALUES ARE SYNCED WITH C CODE (see the same enum in TerrainDataScriptingInterface.h)
            MaxHeightmapRes = 0,
            MinDetailResPerPatch = 1,
            MaxDetailResPerPatch = 2,
            MaxDetailPatchCount = 3,
            MaxCoveragePerRes = 4,
            MinAlphamapRes = 5,
            MaxAlphamapRes = 6,
            MinBaseMapRes = 7,
            MaxBaseMapRes = 8
        }

        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor(k_ScriptingInterfaceName, StaticAccessorType.DoubleColon)]
        extern private static int GetBoundaryValue(BoundaryValueType type);

        internal static readonly int k_MaximumResolution = GetBoundaryValue(BoundaryValueType.MaxHeightmapRes);
        internal static readonly int k_MinimumDetailResolutionPerPatch = GetBoundaryValue(BoundaryValueType.MinDetailResPerPatch);
        internal static readonly int k_MaximumDetailResolutionPerPatch = GetBoundaryValue(BoundaryValueType.MaxDetailResPerPatch);
        internal static readonly int k_MaximumDetailPatchCount = GetBoundaryValue(BoundaryValueType.MaxDetailPatchCount);
        internal static readonly int k_MinimumAlphamapResolution = GetBoundaryValue(BoundaryValueType.MinAlphamapRes);
        internal static readonly int k_MaximumAlphamapResolution = GetBoundaryValue(BoundaryValueType.MaxAlphamapRes);
        internal static readonly int k_MinimumBaseMapResolution = GetBoundaryValue(BoundaryValueType.MinBaseMapRes);
        internal static readonly int k_MaximumBaseMapResolution = GetBoundaryValue(BoundaryValueType.MaxBaseMapRes);

        // ==============================================================
        // 🔧 构造 & 常量边界值
        //
        // TerrainData 通过 Internal_Create 在 C++ 端创建。
        // 所有边界值（如最大分辨率）从 C++ 枚举获取，保持两端同步。
        // ==============================================================
        public TerrainData()
        {
            Internal_Create(this);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "Create")]
        extern private static void Internal_Create([Writable] TerrainData terrainData);

        // ==============================================================
        // ⛰️ Heightmap（高度图） — 地形形状的核心数据
        //
        // 📌 概念：
        //   - heightmapResolution = N（实际纹理尺寸为 N×N）
        //   - heightmapTexture：GPU 上的高度纹理（RenderTexture）
        //   - heightmapScale：每个高度图格子的世界单位大小
        //   - size：地形的世界空间尺寸（与 heightmapScale 关联）
        //
        // 💡 分辨率规则：
        //   heightmapResolution 必须是 2^n + 1（如 33, 65, 129, 257, 513）
        //   因为高度图需要中心顶点来实现无缝 LOD 过渡。
        //
        // 🏗 数据访问方式：
        //   - GetHeights(x, y, w, h) → float[,]：批量读取
        //   - SetHeights(x, y, heights)：批量写入（立即生效）
        //   - SetHeightsDelayLOD：写入但不立即更新 LOD（性能优化）
        //   - GetHeight(x, y)：单个点高度
        //   - GetInterpolatedHeight(x, y)：插值高度（浮点坐标）
        //   - SampleHeight()：世界坐标 → 地形高度（Terrain 组件方法）
        // ==============================================================
        [Obsolete("Please use DirtyHeightmapRegion instead.", false)]
        public void UpdateDirtyRegion(int x, int y, int width, int height, bool syncHeightmapTextureImmediately)
        {
            DirtyHeightmapRegion(new RectInt(x, y, width, height), syncHeightmapTextureImmediately ? TerrainHeightmapSyncControl.HeightOnly : TerrainHeightmapSyncControl.None);
        }

        [Obsolete("Please use heightmapResolution instead. (UnityUpgradable) -> heightmapResolution", false)]
        public int heightmapWidth => heightmapResolution;

        [Obsolete("Please use heightmapResolution instead. (UnityUpgradable) -> heightmapResolution", false)]
        public int heightmapHeight => heightmapResolution;

        extern public RenderTexture heightmapTexture
        {
            [NativeName(k_HeightmapPrefix + "GetHeightmapTexture")]
            get;
        }

        public int heightmapResolution
        {
            get { return internalHeightmapResolution; }
            set
            {
                int clamped = value;
                if (value < 0 || value > k_MaximumResolution)
                {
                    Debug.LogWarning("heightmapResolution is clamped to the range of [0, " + k_MaximumResolution + "].");
                    clamped = Math.Min(k_MaximumResolution, Math.Max(value, 0));
                }

                internalHeightmapResolution = clamped;
            }
        }

        extern private int internalHeightmapResolution
        {
            [NativeName(k_HeightmapPrefix + "GetResolution")]
            get;

            [NativeName(k_HeightmapPrefix + "SetResolution")]
            set;
        }

        extern public Vector3 heightmapScale
        {
            [NativeName(k_HeightmapPrefix + "GetScale")]
            get;
        }

        public Texture holesTexture
        {
            get
            {
                if (IsHolesTextureCompressed())
                {
                    return GetCompressedHolesTexture();
                }
                else
                {
                    return GetHolesTexture();
                }
            }
        }

        extern public bool enableHolesTextureCompression
        {
            [NativeName(k_HeightmapPrefix + "GetEnableHolesTextureCompression")]
            get;

            [NativeName(k_HeightmapPrefix + "SetEnableHolesTextureCompression")]
            set;
        }

        internal RenderTexture holesRenderTexture
        {
            get
            {
                return GetHolesTexture();
            }
        }

        [NativeName(k_HeightmapPrefix + "IsHolesTextureCompressed")]
        extern internal bool IsHolesTextureCompressed();

        [NativeName(k_HeightmapPrefix + "GetHolesTexture")]
        extern internal RenderTexture GetHolesTexture();

        [NativeName(k_HeightmapPrefix + "GetCompressedHolesTexture")]
        extern internal Texture2D GetCompressedHolesTexture();

        public int holesResolution => heightmapResolution - 1;

        // ==============================================================
        // 📐 地形的世界空间尺寸与边界
        //
        // size：   地形在世界空间中的尺寸（Vector3）
        // bounds： 地形的包围盒（只读，由引擎计算）
        // ==============================================================
        extern public Vector3 size
        {
            [NativeName(k_HeightmapPrefix + "GetSize")]
            get;

            [NativeName(k_HeightmapPrefix + "SetSize")]
            set;
        }

        extern public Bounds bounds
        {
            [NativeName(k_HeightmapPrefix + "CalculateBounds")]
            get;
        }

        [Obsolete("Terrain thickness is no longer required by the physics engine. Set appropriate continuous collision detection modes to fast moving bodies.")]
        public float thickness
        {
            get { return 0; }
            set {}
        }

        // ==============================================================
        // 📏 高度采样方法
        //
        // GetHeight(x, y)：              整数坐标采样（0 ~ heightmapResolution-1）
        // GetInterpolatedHeight(x, y)：  浮点坐标双线性插值采样
        // GetInterpolatedHeights()：     批量插值采样（返回 float[,]）
        // ==============================================================
        [NativeName(k_HeightmapPrefix + "GetHeight")]
        extern public float GetHeight(int x, int y);

        [NativeName(k_HeightmapPrefix + "GetInterpolatedHeight")]
        extern public float GetInterpolatedHeight(float x, float y);

        public float[,] GetInterpolatedHeights(float xBase, float yBase, int xCount, int yCount, float xInterval, float yInterval)
        {
            if (xCount <= 0)
                throw new ArgumentOutOfRangeException("xCount");
            else if (yCount <= 0)
                throw new ArgumentOutOfRangeException("yCount");

            float[,] results = new float[yCount, xCount];
            Internal_GetInterpolatedHeights(results, xCount, 0, 0, xBase, yBase, xCount, yCount, xInterval, yInterval);
            return results;
        }

        public void GetInterpolatedHeights(float[,] results, int resultXOffset, int resultYOffset, float xBase, float yBase, int xCount, int yCount, float xInterval, float yInterval)
        {
            if (results == null)
                throw new ArgumentNullException("results");
            else if (xCount <= 0)
                throw new ArgumentOutOfRangeException("xCount");
            else if (yCount <= 0)
                throw new ArgumentOutOfRangeException("yCount");
            else if (resultXOffset < 0 || resultXOffset + xCount > results.GetLength(1))
                throw new ArgumentOutOfRangeException("resultXOffset");
            else if (resultYOffset < 0 || resultYOffset + yCount > results.GetLength(0))
                throw new ArgumentOutOfRangeException("resultYOffset");

            Internal_GetInterpolatedHeights(results, results.GetLength(1), resultXOffset, resultYOffset, xBase, yBase, xCount, yCount, xInterval, yInterval);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetInterpolatedHeights", HasExplicitThis = true)]
        private extern void Internal_GetInterpolatedHeights(float[,] results, int resultXDimension, int resultXOffset, int resultYOffset, float xBase, float yBase, int xCount, int yCount, float xInterval, float yInterval);

        // ==============================================================
        // ⛰️ 高度图批量读写
        //
        // GetHeights(xBase, yBase, width, height) → float[,]
        //   读取指定区域的高度数据。返回的数组：第 0 维是 y，第 1 维是 x。
        //
        // SetHeights(xBase, yBase, float[,] heights)
        //   写入高度数据（立即生效，触发 GPU 同步）。
        //
        // SetHeightsDelayLOD(xBase, yBase, float[,] heights)
        //   写入高度数据（延迟 LOD 更新，适合大批量编辑时使用）。
        //
        // 💡 性能建议：
        //   大批量编辑地形时优先使用 SetHeightsDelayLOD，最后手动 Flush。
        // ==============================================================
        public float[,] GetHeights(int xBase, int yBase, int width, int height)
        {
            if (xBase < 0 || yBase < 0 || xBase + width < 0 || yBase + height < 0 || xBase + width > heightmapResolution || yBase + height > heightmapResolution)
            {
                throw new System.ArgumentException("Trying to access out-of-bounds terrain height information.");
            }

            float[,] heights = new float[height, width];
            Internal_GetHeights(xBase, yBase, width, height, heights);
            return heights;
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetHeights", HasExplicitThis = true)]
        extern private void Internal_GetHeights(int xBase, int yBase, int width, int height, float[,] heights);

        public void SetHeights(int xBase, int yBase, float[,] heights)
        {
            if (heights == null)
            {
                throw new System.NullReferenceException();
            }
            if (xBase + heights.GetLength(1) > heightmapResolution || xBase + heights.GetLength(1) < 0 || yBase + heights.GetLength(0) < 0 || xBase < 0 || yBase < 0 || yBase + heights.GetLength(0) > heightmapResolution)
            {
                throw new System.ArgumentException(string.Format("X or Y base out of bounds. Setting up to {0}x{1} while map size is {2}x{2}", xBase + heights.GetLength(1), yBase + heights.GetLength(0), heightmapResolution));
            }

            Internal_SetHeights(xBase, yBase, heights.GetLength(1), heights.GetLength(0), heights);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHeights", HasExplicitThis = true)]
        extern private void Internal_SetHeights(int xBase, int yBase, int width, int height, float[,] heights);

        [FreeFunction(k_ScriptingInterfacePrefix + "GetPatchMinMaxHeights", HasExplicitThis = true)]
        extern public PatchExtents[] GetPatchMinMaxHeights();

        [FreeFunction(k_ScriptingInterfacePrefix + "OverrideMinMaxPatchHeights", HasExplicitThis = true)]
        extern public void OverrideMinMaxPatchHeights(PatchExtents[] minMaxHeights);

        [FreeFunction(k_ScriptingInterfacePrefix + "GetMaximumHeightError", HasExplicitThis = true)]
        extern public float[] GetMaximumHeightError();

        [FreeFunction(k_ScriptingInterfacePrefix + "OverrideMaximumHeightError", HasExplicitThis = true)]
        extern public void OverrideMaximumHeightError(float[] maxError);

        public void SetHeightsDelayLOD(int xBase, int yBase, float[,] heights)
        {
            if (heights == null) throw new System.ArgumentNullException("heights");

            int height = heights.GetLength(0);
            int width = heights.GetLength(1);

            if (xBase < 0 || (xBase + width) < 0 || (xBase + width) > heightmapResolution)
                throw new System.ArgumentException(string.Format("X out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", xBase, xBase + width, heightmapResolution));

            if (yBase < 0 || (yBase + height) < 0 || (yBase + height) > heightmapResolution)
                throw new System.ArgumentException(string.Format("Y out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", yBase, yBase + height, heightmapResolution));

            Internal_SetHeightsDelayLOD(xBase, yBase, width, height, heights);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHeightsDelayLOD", HasExplicitThis = true)]
        extern private void Internal_SetHeightsDelayLOD(int xBase, int yBase, int width, int height, float[,] heights);

        // ==============================================================
        // 🕳️ Holes（孔洞） — 控制地形的"镂空"区域
        //
        // 📌 作用：
        //   Holes 纹理决定地形网格的哪些区域不生成几何体。
        //   用于创建悬崖洞穴、隧道入口等地形镂空效果。
        //
        // 💡 工作原理：
        //   - holesResolution = heightmapResolution - 1
        //   - true = 有孔洞（不生成网格），false = 无孔洞（正常网格）
        //   - 可以通过 GPU 纹理渲染后再同步到 CPU
        //
        // 🏗 API：
        //   IsHole(x, y) — 查询单点
        //   GetHoles(xBase, yBase, w, h) → bool[,] — 批量读取
        //   SetHoles(xBase, yBase, bool[,]) — 批量写入（立即生效）
        //   SetHolesDelayLOD — 延迟 LOD 更新写入
        // ==============================================================
        public bool IsHole(int x, int y)
        {
            if (x < 0 || x >= holesResolution || y < 0 || y >= holesResolution)
            {
                throw new ArgumentException("Trying to access out-of-bounds terrain holes information.");
            }

            return Internal_IsHole(x, y);
        }

        public bool[,] GetHoles(int xBase, int yBase, int width, int height)
        {
            if (xBase < 0 || yBase < 0 || width <= 0 || height <= 0 || xBase + width > holesResolution || yBase + height > holesResolution)
            {
                throw new ArgumentException("Trying to access out-of-bounds terrain holes information.");
            }

            bool[,] holes = new bool[width, height];
            Internal_GetHoles(xBase, yBase, width, height, holes);
            return holes;
        }

        public void SetHoles(int xBase, int yBase, bool[,] holes)
        {
            if (holes == null) throw new ArgumentNullException("holes");

            int height = holes.GetLength(0);
            int width = holes.GetLength(1);

            if (xBase < 0 || (xBase + width) > holesResolution)
                throw new ArgumentException(string.Format("X out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", xBase, xBase + width, holesResolution));

            if (yBase < 0 || (yBase + height) > holesResolution)
                throw new ArgumentException(string.Format("Y out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", yBase, yBase + height, holesResolution));

            Internal_SetHoles(xBase, yBase, holes.GetLength(1), holes.GetLength(0), holes);
        }

        public void SetHolesDelayLOD(int xBase, int yBase, bool[,] holes)
        {
            if (holes == null) throw new ArgumentNullException("holes");

            int height = holes.GetLength(0);
            int width = holes.GetLength(1);

            if (xBase < 0 || (xBase + width) > holesResolution)
                throw new ArgumentException(string.Format("X out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", xBase, xBase + width, holesResolution));

            if (yBase < 0 || (yBase + height) > holesResolution)
                throw new ArgumentException(string.Format("Y out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", yBase, yBase + height, holesResolution));

            Internal_SetHolesDelayLOD(xBase, yBase, width, height, holes);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHoles", HasExplicitThis = true)]
        extern private void Internal_SetHoles(int xBase, int yBase, int width, int height, bool[,] holes);

        [FreeFunction(k_ScriptingInterfacePrefix + "GetHoles", HasExplicitThis = true)]
        extern private void Internal_GetHoles(int xBase, int yBase, int width, int height, bool[,] holes);


        [FreeFunction(k_ScriptingInterfacePrefix + "IsHole", HasExplicitThis = true)]
        extern private bool Internal_IsHole(int x, int y);

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHolesDelayLOD", HasExplicitThis = true)]
        extern private void Internal_SetHolesDelayLOD(int xBase, int yBase, int width, int height, bool[,] holes);

        [NativeName(k_HeightmapPrefix + "GetSteepness")]
        extern public float GetSteepness(float x, float y);

        [NativeName(k_HeightmapPrefix + "GetInterpolatedNormal")]
        extern public Vector3 GetInterpolatedNormal(float x, float y);

        [NativeName(k_HeightmapPrefix + "GetAdjustedSize")]
        extern internal int GetAdjustedSize(int size);

        // ==============================================================
        // 🌿 Detail Layer（细节层）— 草地/小花的全局参数
        //
        // wavingGrassStrength：草地摆动强度（受风影响）
        // wavingGrassAmount：  草地摆动的幅度
        // wavingGrassSpeed：   草地摆动的速度
        // wavingGrassTint：    草地颜色色调
        // ==============================================================
        extern public float wavingGrassStrength
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassStrength")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassStrength", HasExplicitThis = true)]
            set;
        }

        extern public float wavingGrassAmount
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassAmount")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassAmount", HasExplicitThis = true)]
            set;
        }

        extern public float wavingGrassSpeed
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassSpeed")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassSpeed", HasExplicitThis = true)]
            set;
        }

        extern public Color wavingGrassTint
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassTint")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassTint", HasExplicitThis = true)]
            set;
        }

        // ==============================================================
        // 📐 细节层分辨率管理
        //
        // detailWidth / detailHeight：细节纹理的尺寸
        // maxDetailScatterPerRes：每分辨率单位最大散射数
        //
        // SetDetailResolution(detailRes, resPerPatch)：
        //   设置细节层的分辨率。
        //   patchCount = detailRes / resPerPatch
        //   每个 Patch 是 GPU 实例化的基本单位。
        //
        // SetDetailScatterMode(mode)：
        //   CoverageMode：基于覆盖率的散射（targetCoverage 控制密度）
        //   InstanceCountMode：基于实例计数的散射（int[,] 直接控制）
        // ==============================================================
        extern public int detailWidth
        {
            [NativeName(k_DetailDatabasePrefix + "GetWidth")]
            get;
        }

        extern public int detailHeight
        {
            [NativeName(k_DetailDatabasePrefix + "GetHeight")]
            get;
        }

        extern public int maxDetailScatterPerRes
        {
            [NativeName(k_DetailDatabasePrefix + "GetMaximumScatterPerRes")]
            get;
        }

        public void SetDetailResolution(int detailResolution, int resolutionPerPatch)
        {
            if (detailResolution < 0)
            {
                Debug.LogWarning("detailResolution must not be negative.");
                detailResolution = 0;
            }

            if (resolutionPerPatch < k_MinimumDetailResolutionPerPatch || resolutionPerPatch > k_MaximumDetailResolutionPerPatch)
            {
                Debug.LogWarning("resolutionPerPatch is clamped to the range of [" + k_MinimumDetailResolutionPerPatch + ", " + k_MaximumDetailResolutionPerPatch + "].");
                resolutionPerPatch = Math.Min(k_MaximumDetailResolutionPerPatch, Math.Max(resolutionPerPatch, k_MinimumDetailResolutionPerPatch));
            }

            int patchCount = detailResolution / resolutionPerPatch;
            if (patchCount > k_MaximumDetailPatchCount)
            {
                Debug.LogWarning("Patch count (detailResolution / resolutionPerPatch) is clamped to the range of [0, " + k_MaximumDetailPatchCount + "].");
                patchCount = Math.Min(k_MaximumDetailPatchCount, Math.Max(patchCount, 0));
            }

            Internal_SetDetailResolution(patchCount, resolutionPerPatch);
        }

        [NativeName(k_DetailDatabasePrefix + "SetDetailResolution")]
        extern private void Internal_SetDetailResolution(int patchCount, int resolutionPerPatch);

        public void SetDetailScatterMode(DetailScatterMode scatterMode)
        {
            Internal_SetDetailScatterMode(scatterMode);
        }

        [NativeName(k_DetailDatabasePrefix + "SetDetailScatterMode")]
        extern private void Internal_SetDetailScatterMode(DetailScatterMode scatterMode);

        extern public int detailPatchCount
        {
            [NativeName(k_DetailDatabasePrefix + "GetPatchCount")]
            get;
        }

        extern public int detailResolution
        {
            [NativeName(k_DetailDatabasePrefix + "GetResolution")]
            get;
        }

        extern public int detailResolutionPerPatch
        {
            [NativeName(k_DetailDatabasePrefix + "GetResolutionPerPatch")]
            get;
        }

        extern public DetailScatterMode detailScatterMode
        {
            [NativeName(k_DetailDatabasePrefix + "GetDetailScatterMode")]
            get;
        }

        [NativeName(k_DetailDatabasePrefix + "ResetDirtyDetails")]
        extern internal void ResetDirtyDetails();

        // ==============================================================
        // 🔄 DetailPrototype 管理
        //
        // RefreshPrototypes()：刷新所有原型（编辑器中使用）
        // detailPrototypes：    获取/设置细节原型数组
        // GetSupportedLayers()：获取指定区域内存在的细节层索引
        // GetDetailLayer()：    获取指定区域的细节密度数据
        // SetDetailLayer()：    设置指定区域的细节密度数据
        // ==============================================================
        [FreeFunction(k_ScriptingInterfacePrefix + "RefreshPrototypes", HasExplicitThis = true)]
        extern public void RefreshPrototypes();

        extern public DetailPrototype[] detailPrototypes
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetDetailPrototypes", HasExplicitThis = true)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetDetailPrototypes", HasExplicitThis = true)]
            set;
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetSupportedLayers", HasExplicitThis = true)]
        extern public int[] GetSupportedLayers(int xBase, int yBase, int totalWidth, int totalHeight);

        public int[] GetSupportedLayers(Vector2Int positionBase, Vector2Int size)
        {
            return GetSupportedLayers(positionBase.x, positionBase.y, size.x, size.y);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetDetailLayer", HasExplicitThis = true)]
        extern void GetDetailLayer(int xBase, int yBase, int width, int height, int layer, int[,] detailLayer);

        public int[,] GetDetailLayer(int xBase, int yBase, int width, int height, int layer)
        {
            int[,] detailLayer = new int[width, height];
            GetDetailLayer(xBase, yBase, width, height, layer, detailLayer);
            return detailLayer;
        }

        public int[,] GetDetailLayer(Vector2Int positionBase, Vector2Int size, int layer)
        {
            return GetDetailLayer(positionBase.x, positionBase.y, size.x, size.y, layer);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "ComputeDetailInstanceTransforms", HasExplicitThis = true)]
        extern public DetailInstanceTransform[] ComputeDetailInstanceTransforms(int patchX, int patchY, int layer, float density, out Bounds bounds);


        [FreeFunction(k_ScriptingInterfacePrefix + "ComputeDetailCoverage", HasExplicitThis = true)]
        extern public float ComputeDetailCoverage(int detailPrototypeIndex);

        public void SetDetailLayer(int xBase, int yBase, int layer, int[,] details)
        {
            Internal_SetDetailLayer(xBase, yBase, details.GetLength(1), details.GetLength(0), layer, details);
        }

        public void SetDetailLayer(Vector2Int basePosition, int layer, int[,] details)
        {
            SetDetailLayer(basePosition.x, basePosition.y, layer, details);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetDetailLayer", HasExplicitThis = true)]
        extern private void Internal_SetDetailLayer(int xBase, int yBase, int totalWidth, int totalHeight, int detailIndex, int[,] data);

        [FreeFunction(k_ScriptingInterfacePrefix + "GetClampedDetailPatches", HasExplicitThis = true)]
        extern public Vector2Int[] GetClampedDetailPatches(float density);

        // ==============================================================
        // 🌳 Tree Instances（树木实例）
        //
        // 📌 概念：
        //   treeInstances：    所有树木实例的数组（TreeInstance[]）
        //   treeInstanceCount：树木实例数量
        //   treePrototypes：   树木原型定义（TreePrototype[]）
        //
        // 🏗 数据流：
        //   TreePrototype[] 定义了"有哪些树种"
        //   TreeInstance[] 的每个实例的 prototypeIndex 指向 TreePrototype
        //
        // 💡 运行时操作：
        //   - AddTreeInstance()：添加一棵树（Terrain 组件方法）
        //   - SetTreeInstances()：批量设置所有树（snapToHeightmap 自动贴合）
        //   - GetTreeInstance(index)：按索引获取单棵树
        //   - RefreshPrototypes()：原型变化后刷新
        // ==============================================================
        public TreeInstance[] treeInstances
        {
            get
            {
                return Internal_GetTreeInstances();
            }

            set
            {
                SetTreeInstances(value, false);
            }
        }

        [NativeName(k_TreeDatabasePrefix + "GetInstances")]
        extern private TreeInstance[] Internal_GetTreeInstances();

        [FreeFunction(k_ScriptingInterfacePrefix + "SetTreeInstances", HasExplicitThis = true)]
        extern public void SetTreeInstances([NotNull] TreeInstance[] instances, bool snapToHeightmap);

        public TreeInstance GetTreeInstance(int index)
        {
            if (index < 0 || index >= treeInstanceCount)
                throw new ArgumentOutOfRangeException("index");

            return Internal_GetTreeInstance(index);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetTreeInstance", HasExplicitThis = true)]
        extern private TreeInstance Internal_GetTreeInstance(int index);

        [FreeFunction(k_ScriptingInterfacePrefix + "SetTreeInstance", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetTreeInstance(int index, TreeInstance instance);

        extern public int treeInstanceCount
        {
            [NativeName(k_TreeDatabasePrefix + "GetInstances().size")]
            get;
        }

        extern public TreePrototype[] treePrototypes
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetTreePrototypes", HasExplicitThis = true)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetTreePrototypes", HasExplicitThis = true)]
            set;
        }

        [NativeName(k_TreeDatabasePrefix + "RemoveTreePrototype")]
        extern internal void RemoveTreePrototype(int index);

        [NativeName(k_DetailDatabasePrefix + "RemoveDetailPrototype")]
        extern public void RemoveDetailPrototype(int index);

        [NativeName(k_TreeDatabasePrefix + "NeedUpgradeScaledPrototypes")]
        extern internal bool NeedUpgradeScaledTreePrototypes();

        [FreeFunction(k_ScriptingInterfacePrefix + "UpgradeScaledTreePrototype", HasExplicitThis = true)]
        extern internal void UpgradeScaledTreePrototype();

        // ==============================================================
        // 🎨 Alphamap（Splatmap）— 地表纹理混合系统
        //
        // 📌 概念：
        //   Alphamap 是 TerrainLayer 混合系统的数据核心。
        //   一个 [w×h×layers] 的 float 数组，存储每层在每个像素的混合权重。
        //   所有权重之和必须为 1（归一化）。
        //
        // 🏗 层级关系：
        //   TerrainLayer[] → 定义每层的贴图/PBR参数
        //   Alphamap       → 定义每层在每个像素的混合权重
        //   BaseMap        → 预烘焙的低分辨率版本（远距离使用）
        //
        // 💡 alphamapLayers 与 terrainLayers：
        //   alphamapLayers = terrainLayers.Length（层数）
        //   每层对应一个 TerrainLayer（漫反射+法线+Mask贴图）
        //
        // ⚠️ 性能注意事项：
        //   - alphamapResolution 越大，混合越精细，但 GPU 开销越大
        //   - 一般 512 足够，高性能场景建议 256
        //   - BaseMap 分辨率单独控制，不影响运行时混合精度
        // ==============================================================
        extern public int alphamapLayers
        {
            [NativeName(k_SplatDatabasePrefix + "GetSplatCount")]
            get;
        }

        public float[,,] GetAlphamaps(int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || width < 0 || height < 0)
                throw new ArgumentException("Invalid argument for GetAlphaMaps");

            OutArray3D<float> alphamaps = default;
            Internal_GetAlphamaps(x, y, width, height, in alphamaps);
            return alphamaps.Value;

        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetAlphamaps", HasExplicitThis = true)]
        extern private void Internal_GetAlphamaps(int x, int y, int width, int height, in OutArray3D<float> alphamaps);


        public int alphamapResolution
        {
            get { return Internal_alphamapResolution; }
            set
            {
                int clamped = value;
                if (value < k_MinimumAlphamapResolution || value > k_MaximumAlphamapResolution)
                {
                    Debug.LogWarning("alphamapResolution is clamped to the range of [" + k_MinimumAlphamapResolution + ", " + k_MaximumAlphamapResolution + "].");
                    clamped = Math.Min(k_MaximumAlphamapResolution, Math.Max(value, k_MinimumAlphamapResolution));
                }

                Internal_alphamapResolution = clamped;
            }
        }

        // Needed by GI code which will call this by reflection
        [RequiredByNativeCode]
        [NativeName(k_SplatDatabasePrefix + "GetAlphamapResolution")]
        extern internal float GetAlphamapResolutionInternal();

        extern private int Internal_alphamapResolution
        {
            [NativeName(k_SplatDatabasePrefix + "GetAlphamapResolution")]
            get;

            [NativeName(k_SplatDatabasePrefix + "SetAlphamapResolution")]
            set;
        }

        public int alphamapWidth { get { return alphamapResolution; } }

        public int alphamapHeight { get { return alphamapResolution; } }

        public int baseMapResolution
        {
            get { return Internal_baseMapResolution; }
            set
            {
                int clamped = value;
                if (value < k_MinimumBaseMapResolution || value > k_MaximumBaseMapResolution)
                {
                    Debug.LogWarning("baseMapResolution is clamped to the range of [" + k_MinimumBaseMapResolution + ", " + k_MaximumBaseMapResolution + "].");
                    clamped = Math.Min(k_MaximumBaseMapResolution, Math.Max(value, k_MinimumBaseMapResolution));
                }

                Internal_baseMapResolution = clamped;
            }
        }

        extern private int Internal_baseMapResolution
        {
            [NativeName(k_SplatDatabasePrefix + "GetBaseMapResolution")]
            get;

            [NativeName(k_SplatDatabasePrefix + "SetBaseMapResolution")]
            set;
        }

        public void SetAlphamaps(int x, int y, float[,,] map)
        {
            if (map.GetLength(2) != alphamapLayers)
            {
                throw new System.Exception(string.Format("Float array size wrong (layers should be {0})", alphamapLayers));
            }

            // TODO: crop the map or throw if outside.

            Internal_SetAlphamaps(x, y, map.GetLength(1), map.GetLength(0), map);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetAlphamaps", HasExplicitThis = true)]
        extern private void Internal_SetAlphamaps(int x, int y, int width, int height, float[,,] map);

        [NativeName(k_SplatDatabasePrefix + "SetBaseMapsDirty")]
        extern public void SetBaseMapDirty();

        [NativeName(k_SplatDatabasePrefix + "GetAlphaTexture")]
        extern public Texture2D GetAlphamapTexture(int index);

        public extern int alphamapTextureCount
        {
            [NativeName(k_SplatDatabasePrefix + "GetAlphaTextureCount")]
            get;
        }

        public Texture2D[] alphamapTextures
        {
            get
            {
                Texture2D[] splatTextures = new Texture2D[alphamapTextureCount];
                for (int i = 0; i < splatTextures.Length; i++)
                    splatTextures[i] = GetAlphamapTexture(i);
                return splatTextures;
            }
        }

        [Obsolete("TerrainData.splatPrototypes is obsolete. Use TerrainData.terrainLayers instead.", false)]
        extern public SplatPrototype[] splatPrototypes
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetSplatPrototypes", HasExplicitThis = true)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetSplatPrototypes", HasExplicitThis = true)]
            set;
        }

        extern public TerrainLayer[] terrainLayers
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetTerrainLayers", HasExplicitThis = true)]
            [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetTerrainLayers", HasExplicitThis = true)]
            [param: UnityMarshalAs(NativeType.ScriptingObjectPtr)] set;
        }

        public void SetTerrainLayersRegisterUndo(TerrainLayer[] terrainLayers, string undoName)
        {
            if (string.IsNullOrEmpty(undoName))
            {
                // The native code will skip creating undo if the name is empty (for the native path without undo).
                // Make sure we don't hit that path by using an empty string.
                throw new ArgumentNullException("undoName");
            }
            Internal_SetTerrainLayersRegisterUndo(terrainLayers, undoName);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetTerrainLayersRegisterUndo", HasExplicitThis = true)]
        extern private void Internal_SetTerrainLayersRegisterUndo([UnityMarshalAs(NativeType.ScriptingObjectPtr)] TerrainLayer[] terrainLayers, string undoName);

        [NativeName(k_TreeDatabasePrefix + "AddTree")]
        extern internal void AddTree(ref TreeInstance tree);

        [NativeName(k_TreeDatabasePrefix + "RemoveTrees")]
        extern internal int RemoveTrees(Vector2 position, float radius, int prototypeIndex);

        [NativeName(k_HeightmapPrefix + "CopyHeightmapFromActiveRenderTexture")]
        private extern void Internal_CopyActiveRenderTextureToHeightmap(RectInt rect, int destX, int destY, TerrainHeightmapSyncControl syncControl);

        [NativeName(k_HeightmapPrefix + "DirtyHeightmapRegion")]
        private extern void Internal_DirtyHeightmapRegion(int x, int y, int width, int height, TerrainHeightmapSyncControl syncControl);

        [NativeName(k_HeightmapPrefix + "SyncHeightmapGPUModifications")]
        public extern void SyncHeightmap();

        [NativeName(k_HeightmapPrefix + "CopyHolesFromActiveRenderTexture")]
        private extern void Internal_CopyActiveRenderTextureToHoles(RectInt rect, int destX, int destY, bool allowDelayedCPUSync);

        [NativeName(k_HeightmapPrefix + "DirtyHolesRegion")]
        private extern void Internal_DirtyHolesRegion(int x, int y, int width, int height, bool allowDelayedCPUSync);

        [NativeName(k_HeightmapPrefix + "SyncHolesGPUModifications")]
        private extern void Internal_SyncHoles();

        [NativeName(k_SplatDatabasePrefix + "MarkDirtyRegion")]
        private extern void Internal_MarkAlphamapDirtyRegion(int alphamapIndex, int x, int y, int width, int height);

        [NativeName(k_SplatDatabasePrefix + "ClearDirtyRegion")]
        private extern void Internal_ClearAlphamapDirtyRegion(int alphamapIndex);

        [NativeName(k_SplatDatabasePrefix + "SyncGPUModifications")]
        private extern void Internal_SyncAlphamaps();

        extern internal TextureFormat atlasFormat
        {
            [NativeName(k_DetailDatabasePrefix + "GetAtlasTextureFormat")]
            get;
        }

        internal extern Terrain[] users
        {
            get;
        }
    }
}
