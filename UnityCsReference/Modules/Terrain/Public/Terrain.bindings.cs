// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Terrain — Unity 地形系统核心组件
//
// 📌 作用：
//   Terrain 是 Unity 地形系统的运行时组件，挂载在 Terrain GameObject 上。
//   它负责管理地形数据的渲染、LOD、物理碰撞、树木和细节的绘制。
//   每个 Terrain 引用一个 TerrainData 作为数据源。
//
// 🏗 地形渲染管线（Terrain Rendering Pipeline）：
//   1. 高度图（Heightmap）→ 网格生成基础几何体
//   2. 孔洞纹理（Holes）→ 控制哪些区域不生成几何体
//   3. Splatmap（Alphamap）→ 混合多张地表贴图（TerrainLayer 系统）
//   4. 细节（Detail）→ 草地 / 小花等散射物体
//   5. 树木（Tree）→ SpeedTree / 预制体实例化渲染
//   6. BaseMap → 远距离缩略地表贴图（预烘焙的远看版本）
//
// 🏗 地形数据管线（Terrain Data Pipeline）：
//   Heightmap → 决定地形形状
//   → Alphamap（Splatmap）→ 决定地表纹理混合
//   → Detail Layer → 草地 / 植被密度分布
//   → Tree Instances → 树木位置/种类
//   → Holes Texture → 地形孔洞（悬崖洞穴等）
//
// ⚡ 关键性能参数：
//   - heightmapPixelError / heightmapMaximumLOD：控制网格 LOD
//   - treeDistance / detailObjectDistance：控制植被剔除距离
//   - basemapDistance：远距离使用预烘焙低分辨率贴图
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    // ==============================================================
    // 🎯 TerrainChangedFlags — 地形变化追踪的位掩码
    //
    // 📌 作用：
    //   OnTerrainChanged 回调会传入这个 flags，告诉监听者地形哪些部分发生了变化。
    //   这是一个 Flags 枚举，可以用位运算组合判断。
    //
    // 💡 典型使用场景：
    //   编辑器中的 Auto-Connect（自动接缝）：
    //   Heightmap 变化时 → 更新邻居 Terrain 的接缝 LOD。
    //
    // 🏗 变化类型：
    //   - Heightmap：高度图数据变化（地形形状改变）
    //   - TreeInstances：树木实例增删改
    //   - DelayedHeightmapUpdate：延迟的高度图更新（SetHeightsDelayLOD）
    //   - FlushEverythingImmediately：立即刷新所有（编辑器刷完笔刷后）
    //   - RemoveDirtyDetailsImmediately：立即移除脏细节
    //   - HeightmapResolution：分辨率改变（耗费巨大）
    //   - Holes：孔洞纹理变化
    //   - WillBeDestroyed：地形即将被销毁
    // ==============================================================
    [Flags]
    public enum TerrainChangedFlags
    {
        Heightmap = 1,
        TreeInstances = 2,
        DelayedHeightmapUpdate = 4,
        FlushEverythingImmediately = 8,
        RemoveDirtyDetailsImmediately = 16,
        HeightmapResolution = 32,
        Holes = 64,
        DelayedHolesUpdate = 128,
        WillBeDestroyed = 256,
    }

    // ==============================================================
    // 🎯 TerrainRenderFlags — 编辑器中的地形渲染开关
    //
    // 📌 作用：
    //   控制地形在编辑器中渲染哪些部分（高度图、树木、细节）。
    //   用于编辑器视图的性能优化，运行时不受影响。
    // ==============================================================
    [Flags]
    public enum TerrainRenderFlags
    {
        [Obsolete("TerrainRenderFlags.heightmap is obsolete, use TerrainRenderFlags.Heightmap instead. (UnityUpgradable) -> Heightmap")]
        heightmap = 1,

        [Obsolete("TerrainRenderFlags.trees is obsolete, use TerrainRenderFlags.Trees instead. (UnityUpgradable) -> Trees")]
        trees = 2,

        [Obsolete("TerrainRenderFlags.details is obsolete, use TerrainRenderFlags.Details instead. (UnityUpgradable) -> Details")]
        details = 4,

        [Obsolete("TerrainRenderFlags.all is obsolete, use TerrainRenderFlags.All instead. (UnityUpgradable) -> All")]
        all = All,

        Heightmap = 1,
        Trees = 2,
        Details = 4,
        All = Heightmap | Trees | Details
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/Terrain/Public/Terrain.h")]
    [NativeHeader("Runtime/Interfaces/ITerrainManager.h")]
    [NativeHeader("TerrainScriptingClasses.h")]
    [StaticAccessor("GetITerrainManager()", StaticAccessorType.Arrow)]
    public sealed partial class Terrain : Behaviour
    {
        // ==============================================================
        // 📌 terrainData — 地形数据的 ScriptableObject 容器
        //
        // 这是 Terrain 和 Terraindata 之间的桥梁。
        // Terrain 负责"渲染"，TerrainData 负责"存储"。
        // ==============================================================
        extern public TerrainData terrainData { get; set; }

        // ==============================================================
        // 🌳 树木渲染控制参数
        //
        // treeDistance:        树木最大可视距离
        // treeBillboardDistance: 从多远处开始使用 BillBoard（公告板）替代 3D 模型
        // treeCrossFadeLength:  Billboard 和 3D 模型之间的过渡距离
        // treeMaximumFullLODCount: 场景中保持全 LOD 的最大树木数量
        // ==============================================================
        extern public float treeDistance { get; set; }

        extern public float treeBillboardDistance { get; set; }

        extern public float treeCrossFadeLength { get; set; }

        extern public int treeMaximumFullLODCount { get; set; }

        // ==============================================================
        // 🌿 细节（草地/小花）渲染控制参数
        //
        // detailObjectDistance:  细节物体的最大可视距离
        // detailObjectDensity:   细节物体的密度系数（0~1）
        // ==============================================================
        extern public float detailObjectDistance { get; set; }

        extern public float detailObjectDensity { get; set; }

        // ==============================================================
        // ⛰️ 高度图 LOD 控制参数
        //
        // heightmapPixelError:      控制网格简化的误差阈值（值越大→三角形越少）
        // heightmapMaximumLOD:      最大 LOD 级别（0=全细节）
        // heightmapMinimumLODSimplification: 最小 LOD 简化级别（远处）
        // ==============================================================
        extern public float heightmapPixelError { get; set; }
        extern public int heightmapMaximumLOD { get; set; }
        extern public int heightmapMinimumLODSimplification { get; set; }

        // ==============================================================
        // 🗺️ BaseMap — 远距离地表缩略图
        //
        // basemapDistance: 当摄像机距离超过此值，使用预烘焙的 BaseMap 替代
        //                   实时 Splatmap 混合。大幅提升远距离渲染性能。
        // ==============================================================
        extern public float basemapDistance { get; set; }

        [NativeProperty("StaticLightmapIndexInt")]
        extern public int lightmapIndex { get; set; }

        [NativeProperty("DynamicLightmapIndexInt")]
        extern public int realtimeLightmapIndex { get; set; }

        [NativeProperty("StaticLightmapST")]
        extern public Vector4 lightmapScaleOffset { get; set; }

        [NativeProperty("DynamicLightmapST")]
        extern public Vector4 realtimeLightmapScaleOffset { get; set; }

        [Obsolete("Terrain.freeUnusedRenderingResources is obsolete; use keepUnusedRenderingResources instead.")]
        [NativeProperty("FreeUnusedRenderingResourcesObsolete")]
        extern public bool freeUnusedRenderingResources { get; set; }

        [NativeProperty("KeepUnusedRenderingResources")]
        extern public bool keepUnusedRenderingResources { get; set; }

        extern public bool GetKeepUnusedCameraRenderingResources(EntityId cameraEntityId);
        extern public void SetKeepUnusedCameraRenderingResources(EntityId cameraEntityId, bool keepUnused);

        [Obsolete("GetKeepUnusedCameraRenderingResources(int) is obsolete. Use GetKeepUnusedCameraRenderingResources(EntityId) instead.", true)]
        public bool GetKeepUnusedCameraRenderingResources(int cameraInstanceID) => GetKeepUnusedCameraRenderingResources((EntityId)cameraInstanceID);
        [Obsolete("SetKeepUnusedCameraRenderingResources(int, bool) is obsolete. Use SetKeepUnusedCameraRenderingResources(EntityId, bool) instead.", true)]
        public void SetKeepUnusedCameraRenderingResources(int cameraInstanceID, bool keepUnused) => SetKeepUnusedCameraRenderingResources((EntityId)cameraInstanceID, keepUnused);

        // ==============================================================
        // 🎨 渲染与材质相关属性
        //
        // shadowCastingMode:         阴影投射模式
        // reflectionProbeUsage:      反射探针使用方式
        // materialTemplate:          地形材质模板
        // splatBaseMaterial:         BaseMap 混合材质（只读）
        // ==============================================================
        extern public ShadowCastingMode shadowCastingMode { get; set; }

        extern public ReflectionProbeUsage reflectionProbeUsage { get; set; }

        extern public void GetClosestReflectionProbes([Out,NotNull] List<ReflectionProbeBlendInfo> result);

        extern public Material materialTemplate { get; set; }

        // ==============================================================
        // ⚙️ 高度图绘制与连接
        //
        // drawHeightmap:    是否绘制高度图（用于调试）
        // allowAutoConnect: 是否自动与邻居 Terrain 缝合接缝
        // groupingID:       相同 ID 的 Terrain 为一组进行自动连接
        // drawInstanced:    是否使用 Instanced 渲染（更高性能）
        // enableHeightmapRayTracing: 是否启用高度图光线追踪
        // ==============================================================
        extern public bool drawHeightmap { get; set; }
        extern public bool allowAutoConnect { get; set; }
        extern public int groupingID { get; set; }

        extern public bool drawInstanced { get; set; }

        extern public bool enableHeightmapRayTracing { get; set; }
        extern public bool enableHeightmapLODFrustumCulling { get; set; }

        // ==============================================================
        // 📊 法线贴图 & Splat基础材质
        //
        // normalmapTexture:  从高度图计算出的法线贴图（只读）
        // splatBaseMaterial: BaseMap 远距离混合的材质（只读）
        // ==============================================================
        extern public RenderTexture normalmapTexture { [NativeMethod("TryGetNormalMapTexture")] get; }

        extern public Material splatBaseMaterial { [NativeMethod("TryGetSplatBaseMaterial")] get; }

        // ==============================================================
        // 🌳 树木与 foliage 绘制控制
        //
        // drawTreesAndFoliage: 是否绘制树木和细节植被
        // collectDetailPatches: 是否收集细节 Patch（运行时优化）
        // treeLODBiasMultiplier: 树木 LOD 偏差乘数
        // ==============================================================
        extern public bool drawTreesAndFoliage { get; set; }

        // ==============================================================
        // 📐 Patch 边界与高度采样
        //
        // patchBoundsMultiplier: Patch 包围盒缩放系数
        // SampleHeight():        获取指定世界坐标处的地形高度
        // AddTreeInstance():     在运行时添加一棵树
        // SetNeighbors():        设置四个方向的邻居 Terrain（用于接缝 LOD）
        // ==============================================================
        extern public Vector3 patchBoundsMultiplier { get; set; }

        extern public float SampleHeight(Vector3 worldPosition);

        extern public void AddTreeInstance(TreeInstance instance);

        extern public void SetNeighbors(Terrain left, Terrain top, Terrain right, Terrain bottom);

        // ==============================================================
        // ⚡ 运行时性能控制
        //
        // treeLODBiasMultiplier:  树木 LOD 偏差乘数（微调远处树木细节）
        // collectDetailPatches:   是否收集细节 Patch（关闭可省 CPU）
        // ignoreQualitySettings:  是否忽略 Project Quality 设置中的地形参数
        // editorRenderFlags:      编辑器中渲染哪些部分
        // ==============================================================
        extern public float treeLODBiasMultiplier { get; set; }

        extern public bool collectDetailPatches { get; set; }

        extern public bool ignoreQualitySettings { get; set; }

        extern public TerrainRenderFlags editorRenderFlags { get; set; }

        // ==============================================================
        // 🔧 工具方法
        //
        // GetPosition(): 获取地形在世界空间中的位置
        // Flush():       强制刷新所有待处理的地形修改到 GPU
        // ==============================================================
        extern public Vector3 GetPosition();

        extern public void Flush();

        extern internal void RemoveTrees(Vector2 position, float radius, int prototypeIndex);

        [NativeMethod("CopySplatMaterialCustomProps")]
        extern public void SetSplatMaterialPropertyBlock(MaterialPropertyBlock properties);

        public void GetSplatMaterialPropertyBlock(MaterialPropertyBlock dest)
        {
            if (dest == null)
                throw new ArgumentNullException("dest");

            Internal_GetSplatMaterialPropertyBlock(dest);
        }

        [NativeMethod("GetSplatMaterialCustomProps")]
        extern private void Internal_GetSplatMaterialPropertyBlock(MaterialPropertyBlock dest);

        extern public bool bakeLightProbesForTrees { get; set; }

        extern public bool deringLightProbesForTrees { get; set; }

        extern public TreeMotionVectorModeOverride treeMotionVectorModeOverride { get; set; }

        extern public bool preserveTreePrototypeLayers { get; set; }

        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat heightmapFormat { get; }

        static public TextureFormat heightmapTextureFormat
        {
            get { return GraphicsFormatUtility.GetTextureFormat(heightmapFormat); }
        }

        static public RenderTextureFormat heightmapRenderTextureFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(heightmapFormat); }
        }

        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat normalmapFormat { get; }

        static public TextureFormat normalmapTextureFormat
        {
            get { return GraphicsFormatUtility.GetTextureFormat(normalmapFormat); }
        }

        static public RenderTextureFormat normalmapRenderTextureFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(normalmapFormat); }
        }

        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat holesFormat { get; }

        static public RenderTextureFormat holesRenderTextureFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(holesFormat); }
        }

        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat compressedHolesFormat { get; }

        static public TextureFormat compressedHolesTextureFormat
        {
            get { return GraphicsFormatUtility.GetTextureFormat(compressedHolesFormat); }
        }

        extern public static Terrain activeTerrain { get; }
        extern public static void SetConnectivityDirty();

        [NativeProperty("ActiveTerrainsScriptingArray")]
        extern public static Terrain[] activeTerrains { [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)] get; }

        public static void GetActiveTerrains(List<Terrain> terrainList)
        {
            Internal_FillActiveTerrainList(terrainList);
        }

        extern private static void Internal_FillActiveTerrainList([NotNull] [Out] List<Terrain> terrainList);

        [UsedByNativeCode]
        extern public static GameObject CreateTerrainGameObject(TerrainData assignTerrain);

        // ==============================================================
        // 🔗 邻居 Terrain（自动接缝用）
        //
        // 通过 SetNeighbors() 设置，引擎会自动处理相邻地形之间的
        // LOD 接缝，避免地形块之间出现裂缝。
        // ==============================================================
        extern public Terrain leftNeighbor { get; }
        extern public Terrain rightNeighbor { get; }
        extern public Terrain topNeighbor { get; }
        extern public Terrain bottomNeighbor { get; }

        extern public UInt32 renderingLayerMask { get; set; }
    }

    // ==============================================================
    // 🎯 TerrainExtensions — 地形 GI 更新扩展方法
    //
    // 📌 作用：
    //   提供更新地形全局光照（GI）材质的便捷方法。
    //   当地形的 Splatmap 或 TerrainLayer 变化时，需要调用此方法
    //   通知光照系统重新烘焙 GI 数据。
    //
    // ⚡ 两种调用方式：
    //   - UpdateGIMaterials()：更新整个地形
    //   - UpdateGIMaterials(x, y, w, h)：只更新指定区域（性能更好）
    // ==============================================================
    public static partial class TerrainExtensions
    {
        public static void UpdateGIMaterials(this Terrain terrain)
        {
            if (terrain.terrainData == null)
                throw new ArgumentException("Invalid terrainData.");

            UpdateGIMaterialsForTerrain(terrain.GetEntityId(), new Rect(0, 0, 1, 1));
        }

        public static void UpdateGIMaterials(this Terrain terrain, int x, int y, int width, int height)
        {
            if (terrain.terrainData == null)
                throw new ArgumentException("Invalid terrainData.");

            float alphamapWidth = terrain.terrainData.alphamapWidth;
            float alphamapHeight = terrain.terrainData.alphamapHeight;
            UpdateGIMaterialsForTerrain(terrain.GetEntityId(), new Rect(x / alphamapWidth, y / alphamapHeight, width / alphamapWidth, height / alphamapHeight));
        }

        [FreeFunction]
        [NativeConditional("INCLUDE_DYNAMIC_GI && ENABLE_RUNTIME_GI")]
        extern internal static void UpdateGIMaterialsForTerrain(EntityId terrainInstanceID, Rect uvBounds);
    }

    // ==============================================================
    // 🌳 Tree — 树木组件（SpeedTree 容器）
    //
    // 📌 作用：
    //   挂载在作为树木预制体的 GameObject 上。
    //   主要用于关联 SpeedTree Wind Asset，使树木能响应风场。
    //
    // 💡 两种树木渲染方式：
    //   1. Terrain 管理的树木（TreeInstance 数组）→ 使用 AddTreeInstance()
    //   2. 独立树木 GameObject → 使用 Tree 组件
    //
    // 🔗 SpeedTree Wind：
    //   - hasSpeedTreeWind：检查是否包含 SpeedTree 风数据
    //   - windAsset：关联的 SpeedTreeWindAsset 资源
    // ==============================================================
    [NativeHeader("Modules/Terrain/Public/Tree.h")]
    [ExcludeFromPreset]
    public sealed partial class Tree : Component
    {
        [NativeProperty("TreeData")]
        extern public ScriptableObject data { get; set; }

        extern public bool hasSpeedTreeWind
        {
            [NativeMethod("HasSpeedTreeWind")]
            get;
        }

        [NativeProperty("SpeedTreeWindAsset")]
        extern public SpeedTreeWindAsset windAsset
        {
            [NativeMethod("GetSpeedTreeWind")]
            get;
            [NativeMethod("SetSpeedTreeWind")]
            set;
        }
    }


    // ==============================================================
    // 💨 SpeedTreeWindConfig9 — SpeedTree 9 风配置数据结构
    //
    // 📌 作用：
    //   存储 SpeedTree 9 树木的风响应参数。
    //   这是一个内存布局固定的结构体，通过 Marshal.PtrToStructure
    //   与 C++ 端直接读写。
    //
    // 🏗 风计算层级（从下到上）：
    //   1. Shared（主干基础晃动）
    //   2. Branch1（一级树枝晃动）
    //   3. Branch2（二级树枝晃动）
    //   4. Ripple（叶片涟漪 + 闪烁 shimmer）
    //
    // 💡 每个层级包含：
    //   - bend：弯曲量
    //   - oscillation：摆动幅度
    //   - speed：摆动速度
    //   - turbulence：湍流程度
    //   - flexibility：柔韧性
    //   - independence：每个实例的随机偏移
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SpeedTreeWindConfig9
    {
        public float strengthResponse;
        public float directionResponse;

        public float gustFrequency;
        public float gustStrengthMin;
        public float gustStrengthMax;
        public float gustDurationMin;
        public float gustDurationMax;
        public float gustRiseScalar;
        public float gustFallScalar;

        // branch stretch limits + shared height start
        public float branch1StretchLimit;
        public float branch2StretchLimit;

        // BranchWindLevel: Shared
        public float       sharedHeightStart;
        public fixed float bendShared[20];
        public fixed float oscillationShared[20];
        public fixed float speedShared[20];
        public fixed float turbulenceShared[20];
        public fixed float flexibilityShared[20];
        public float independenceShared;

        // BranchWindLevel: Branch1
        //BranchWindLevel m_sBranch1;
        public fixed float bendBranch1[20];
        public fixed float oscillationBranch1[20];
        public fixed float speedBranch1[20];
        public fixed float turbulenceBranch1[20];
        public fixed float flexibilityBranch1[20];
        public float independenceBranch1;

        //BranchWindLevel m_sBranch2;
        public fixed float bendBranch2[20];
        public fixed float oscillationBranch2[20];
        public fixed float speedBranch2[20];
        public fixed float turbulenceBranch2[20];
        public fixed float flexibilityBranch2[20];
        public float independenceBranch2;

        //RippleGroup m_sRipple;
        public fixed float planarRipple[20];
        public fixed float directionalRipple[20];
        public fixed float speedRipple[20];
        public fixed float flexibilityRipple[20];
        public float independenceRipple;
        public float shimmerRipple;

        public float treeExtentX;
        public float treeExtentY;
        public float treeExtentZ;

        public float windIndependence;
        public int doShared;
        public int doBranch1;
        public int doBranch2;
        public int doRipple;
        public int doShimmer;
        public int lodFade;
        public float importScale;

        public SpeedTreeWindConfig9()
        {
            // defaults from SpeedTree SDK example headers
            strengthResponse    = 5.0f;
            directionResponse   = 2.5f;
            gustFrequency       = 0.0f;
            gustStrengthMin     = 0.5f;
            gustStrengthMax     = 1.0f;
            gustDurationMin     = 1.0f;
            gustDurationMax     = 4.0f;
            gustRiseScalar      = 1.0f;
            gustFallScalar      = 1.0f;

            branch1StretchLimit = 1.0f;
            branch2StretchLimit = 1.0f;
            sharedHeightStart   = 0.0f;
            independenceShared  = 0.0f;
            independenceBranch1 = 0.0f;
            independenceBranch2 = 0.0f;
            independenceRipple  = 0.0f;
            shimmerRipple       = 0.0f;
            windIndependence    = 0.0f;
            treeExtentX         = 0.0f;
            treeExtentY         = 0.0f;
            treeExtentZ         = 0.0f;

            doShared            = 0 /*false */;
            doBranch1           = 0 /*false */;
            doBranch2           = 0 /*false */;
            doRipple            = 0 /*false */;
            doShimmer           = 0 /*false */;
            lodFade             = 0 /*false */;
            importScale         = 1.0f;
        }

        public readonly bool IsWindEnabled => (doShared != 0 || doBranch1 != 0 || doBranch2 != 0 || doRipple != 0);

        static public byte[] Serialize(SpeedTreeWindConfig9 config)
        {
            int size = Marshal.SizeOf(config);
            byte[] data = new byte[size];
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                Marshal.StructureToPtr(config, ptr, false);
            }
            finally
            {
                handle.Free();
            }
            return data;
        }
    };

    // ==============================================================
    // 📦 SpeedTreeWindAsset — SpeedTree 风资源
    //
    // 📌 作用：
    //   一个 ScriptableObject 资源，封装了 SpeedTree 风配置数据。
    //   通过 Serialize/Deserialize 与 C++ 端交换二进制数据。
    //
    // 🔄 数据流：
    //   SpeedTreeWindConfig9（结构体）
    //   → Marshal.StructureToPtr → byte[]（序列化）
    //   → C++ SpeedTreeWind 对象
    //
    // 💡 版本管理：
    //   Version 属性用于区分 SpeedTree 8 和 9 的配置格式。
    // ==============================================================
    [NativeHeader("Modules/Terrain/Public/SpeedTreeWind.h")]
    [ExcludeFromPreset] // ?
    public partial class SpeedTreeWindAsset : Object
    {
        extern public int Version { get; set; }

        internal SpeedTreeWindAsset(int version, SpeedTreeWindConfig9 config)
        {
            Internal_Create(this, version, SpeedTreeWindConfig9.Serialize(config));
        }

        [NativeMethod(ThrowsException = true)]
        static extern void Internal_Create([Writable] SpeedTreeWindAsset notSelf, int version, byte[] data);
    }
}
