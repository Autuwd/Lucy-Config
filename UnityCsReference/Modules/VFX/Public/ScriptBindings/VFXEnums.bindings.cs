// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine.VFX
{
    // ================================================================
    // VFXSpace —— VFX 空间坐标枚举
    //
    // 🎯 指定 VFX 参数/属性的坐标空间（局部/世界）
    // 💡 通过 GetExposedSpace() 查询 exposed 属性的空间类型
    // ================================================================
    public enum VFXSpace
    {
        None = -1,
        Local = 0,
        World = 1,
    }

    // ================================================================
    // VFXCullingFlags —— VFX 裁剪标志
    //
    // 🎯 控制 VFX 系统裁剪行为（模拟裁剪 / 包围盒更新裁剪）
    // 💡 默认 CullDefault = 模拟 + 包围盒均裁剪
    // ================================================================
    [Flags]
    internal enum VFXCullingFlags
    {
        CullNone = 0,
        CullSimulation = 1 << 0,
        CullBoundsUpdate = 1 << 1,
        CullDefault = CullSimulation | CullBoundsUpdate,
    }

    // ================================================================
    // VFXExpressionOperation —— VFX 表达式操作类型
    //
    // 🎯 VFX Graph 编译后的表达式树中所有支持的操作码
    // 💡 涵盖数学运算/位运算/类型转换/矩阵运算/采样/噪声/相机等
    // 📌 被表达式编译器用于生成 ComputeShader 指令
    // ================================================================
    internal enum VFXExpressionOperation
    {
        // no-op
        None,

        // Value, combine, extract
        Value,

        // float math operations
        // unary
        Sin,
        Cos,
        Tan,
        ASin,
        ACos,
        ATan,
        Abs,
        Sign,
        Saturate,
        Ceil,
        Round,
        Frac,
        Floor,
        Log2,
        // binary
        Mul,
        Divide,
        Add,
        Subtract,
        Min,
        Max,
        Pow,
        ATan2,

        // Bit wise operations
        BitwiseLeftShift,
        BitwiseRightShift,
        BitwiseOr,
        BitwiseAnd,
        BitwiseXor,
        BitwiseComplement,

        // Cast operations
        CastUintToFloat,
        CastIntToFloat,
        CastFloatToUint,
        CastIntToUint,
        CastFloatToInt,
        CastUintToInt,

        CastIntToBool,
        CastUintToBool,
        CastFloatToBool,
        CastBoolToInt,
        CastBoolToUint,
        CastBoolToFloat,

        // combine
        Combine2f,
        Combine3f,
        Combine4f,
        ExtractComponent,

        // Flow
        Condition,
        Branch,

        // Random
        GenerateRandom,
        GenerateFixedRandom,

        // Logical operations
        LogicalAnd,
        LogicalOr,
        LogicalNot,




        // ------------ End of simple ops ------------------

        // built-in values
        DeltaTime,
        TotalTime,
        SystemSeed,
        LocalToWorld,
        WorldToLocal,
        FrameIndex,
        PlayRate,
        UnscaledDeltaTime,
        ManagerMaxDeltaTime,
        ManagerFixedTimeStep,

        //Game time manager access (built-in values)
        GameDeltaTime,
        GameUnscaledDeltaTime,
        GameSmoothDeltaTime,
        GameTotalTime,
        GameUnscaledTotalTime,
        GameTotalTimeSinceSceneLoad,
        GameTimeScale,

        // matrix operations
        TRSToMatrix,
        InverseMatrix,
        InverseTRSMatrix,
        TransposeMatrix,
        ExtractPositionFromMatrix,
        ExtractAnglesFromMatrix,
        ExtractScaleFromMatrix,

        TransformMatrix,
        TransformPos,
        TransformVec,
        TransformDir,
        TransformVector4,

        // Construct/Split matrices
        RowToMatrix,
        ColumnToMatrix,
        AxisToMatrix,
        MatrixToRow,
        MatrixToColumn,
        MatrixToAxis,

        // Sampling and baking
        SampleCurve,
        SampleGradient,

        SampleMeshVertexFloat,
        SampleMeshVertexFloat2,
        SampleMeshVertexFloat3,
        SampleMeshVertexFloat4,
        SampleMeshVertexColor,

        SampleMeshIndex,
        VertexBufferFromMesh,
        VertexBufferFromSkinnedMeshRenderer,
        IndexBufferFromMesh,
        MeshFromSkinnedMeshRenderer,
        RootBoneTransformFromSkinnedMeshRenderer,

        BakeCurve,
        BakeGradient,

        // Color transformations
        RGBtoHSV,
        HSVtoRGB,

        // Camera operations
        ExtractMatrixFromMainCamera,
        ExtractFOVFromMainCamera,
        ExtractNearPlaneFromMainCamera,
        ExtractFarPlaneFromMainCamera,
        ExtractAspectRatioFromMainCamera,
        ExtractPixelDimensionsFromMainCamera,
        ExtractScaledPixelDimensionsFromMainCamera,
        ExtractLensShiftFromMainCamera,
        GetBufferFromMainCamera,
        IsMainCameraOrthographic,
        GetOrthographicSizeFromMainCamera,

        // Noise
        ValueNoise1D,
        ValueNoise2D,
        ValueNoise3D,
        ValueCurlNoise2D,
        ValueCurlNoise3D,

        PerlinNoise1D,
        PerlinNoise2D,
        PerlinNoise3D,
        PerlinCurlNoise2D,
        PerlinCurlNoise3D,

        CellularNoise1D,
        CellularNoise2D,
        CellularNoise3D,
        CellularCurlNoise2D,
        CellularCurlNoise3D,

        VoroNoise2D,

        // Mesh
        MeshVertexCount,
        MeshChannelOffset,
        MeshChannelInfos,
        MeshVertexStride,
        MeshIndexCount,
        MeshIndexFormat,

        // Buffer
        BufferStride,
        BufferCount,

        TextureWidth,
        TextureHeight,
        TextureDepth,
        TextureFormat,

        // Event attribute in spawner
        ReadEventAttribute,

        // Spawner state accessors
        SpawnerStateNewLoop,
        SpawnerStateLoopState,
        SpawnerStateSpawnCount,
        SpawnerStateDeltaTime,
        SpawnerStateTotalTime,
        SpawnerStateDelayBeforeLoop,
        SpawnerStateLoopDuration,
        SpawnerStateDelayAfterLoop,
        SpawnerStateLoopIndex,
        SpawnerStateLoopCount,
    }

    // ================================================================
    // VFXValueType —— VFX 值类型枚举
    //
    // 🎯 VFX Graph 中所有值的类型，对应 C++ 侧 VFXEnums.h 定义
    // 💡 用于表达式编译和值序列化的类型标签
    // ================================================================
    // Must match enum in VFXEnums.h
    internal enum VFXValueType
    {
        None,
        Float,
        Float2,
        Float3,
        Float4,
        Int32,
        Uint32,
        EntityId,
        Texture2D,
        Texture2DArray,
        Texture3D,
        TextureCube,
        TextureCubeArray,
        CameraBuffer,
        Matrix4x4,
        Curve,
        ColorGradient,
        Mesh,
        Spline,
        Boolean,
        Buffer,
        SkinnedMeshRenderer
    }

    // ================================================================
    // VFXTaskType —— VFX 任务类型枚举
    //
    // 🎯 VFX Graph 粒子系统的四大任务阶段 + 具体子类型
    // 💡 粒子生命周期：
    //   Spawner（发射决策）→ Initialize（初始化粒子）
    //   → Update（更新位置/速度/颜色）→ Output（渲染输出）
    // 📌 子类型：
    //   - Spawner：常量/爆发/周期/变量/自定义/设属性/求值
    //   - Output：点/线/四边形/六面体/网格/三角形/八边形
    // ================================================================
    internal enum VFXTaskType
    {
        None = 0,

        Spawner     = 0x10000000,
        Initialize  = 0x20000000,
        Update      = 0x30000000,
        Output      = 0x40000000,

        // updates
        CameraSort                  = Update | 1,
        PerCameraUpdate             = Update | 2,
        PerCameraSort               = Update | 3, //deprecated (since sortingKeys), here for compatibility
        PerOutputSort               = Update | 4,
        GlobalSort                  = Update | 5,


        // outputs
        ParticlePointOutput         = Output | 0,
        ParticleLineOutput          = Output | 1,
        ParticleQuadOutput          = Output | 2,
        ParticleHexahedronOutput    = Output | 3,
        ParticleMeshOutput          = Output | 4,
        ParticleTriangleOutput      = Output | 5,
        ParticleOctagonOutput       = Output | 6,

        // spawners
        ConstantRateSpawner         = Spawner | 0,
        BurstSpawner                = Spawner | 1,
        PeriodicBurstSpawner        = Spawner | 2,
        VariableRateSpawner         = Spawner | 3,
        CustomCallbackSpawner       = Spawner | 4,
        SetAttributeSpawner         = Spawner | 5,
        EvaluateExpressionsSpawner  = Spawner | 6
    }

    // ================================================================
    // VFXSystemType —— VFX 系统类型
    //
    // 🎯 VFX Graph 中的系统分类：生成器 / 粒子 / 网格 / 输出事件
    // ================================================================
    internal enum VFXSystemType
    {
        Spawner,
        Particle,
        Mesh,
        OutputEvent
    }

    // ================================================================
    // VFXSystemFlag —— VFX 系统功能标志位
    //
    // 🎯 编译时确定的系统特性位掩码，描述系统支持的功能
    // 💡 包含 Kill/IndirectBuffer/GPUEvent/Strips/
    //     Instancing/RayTracing 等能力标识
    // ================================================================
    internal enum VFXSystemFlag
    {
        SystemDefault = 0,
        SystemHasKill = 1 << 0,
        SystemHasIndirectBuffer = 1 << 1,
        SystemReceivedEventGPU = 1 << 2,
        SystemHasStrips = 1 << 3,
        SystemNeedsComputeBounds = 1 << 4,
        SystemAutomaticBounds = 1 << 5,
        SystemInWorldSpace = 1 << 6,
        SystemHasDirectLink = 1 << 7,
        SystemHasAttributeBuffer = 1 << 8,
        SystemUsesInstancedRendering = 1 << 9,
        SystemIsRayTraced = 1 << 10,
    }

    // ================================================================
    // VFXUpdateMode —— VFX 粒子更新模式
    //
    // 🎯 控制粒子系统的时间更新策略
    // 💡 选项：
    //   - FixedDeltaTime：固定步长（与物理同步）
    //   - DeltaTime：使用可变帧时间
    //   - IgnoreTimeScale：无视 Time.timeScale
    //   - ExactFixedTimeStep：精确固定步长（无累积误差）
    // ================================================================
    [Flags]
    internal enum VFXUpdateMode
    {
        FixedDeltaTime = 0,
        DeltaTime = 1 << 0,
        IgnoreTimeScale = 1 << 1,
        ExactFixedTimeStep = 1 << 2,

        //Following line is only for UI compatibility
        //This entry can be removed once a new package has been released
        //It provides a way to access to all option without changing C# package code
        DeltaTimeAndIgnoreTimeScale = DeltaTime | IgnoreTimeScale,
        FixedDeltaAndExactTime = FixedDeltaTime | ExactFixedTimeStep, //Actually equals to ExactFixedTimeStep
        FixedDeltaAndExactTimeAndIgnoreTimeScale = FixedDeltaTime | ExactFixedTimeStep | IgnoreTimeScale
    }

    // ================================================================
    // VFXCameraBufferTypes —— VFX 相机缓冲区请求类型
    //
    // 🎯 通过 IsCameraBufferNeeded / SetCameraBuffer 请求相机缓冲区
    // 💡 支持 Depth/Color/Normal 缓冲区，用于 VFX 中的相机纹理采样
    // ================================================================
    [Flags]
    public enum VFXCameraBufferTypes
    {
        None = 0,
        Depth = 1 << 0,
        Color = 1 << 1,
        Normal = 1 << 2,
    }

    // ================================================================
    // VFXInstancingMode —— VFX GPU 实例化模式
    //
    // 🎯 控制 VisualEffectAsset 的实例化渲染方式
    // 💡 Disabled = 关闭实例化，Auto = 自动批次容量，Custom = 自定义
    // ⚡ 实例化可以大幅减少相同 VFX 的 DrawCall
    // ================================================================
    internal enum VFXInstancingMode
    {
        Disabled = -1,
        [InspectorName("Automatic batch capacity")]
        Auto = 0,
        [InspectorName("Custom batch capacity")]
        Custom
    };

    // ================================================================
    // VFXInstancingDisabledReason —— 实例化禁用原因标志
    //
    // 🎯 记录哪些因素阻止了 GPU Instancing 的启用
    // 💡 常见原因：IndirectDraw / OutputEvent / GPUEvent /
    //     AutomaticBounds / MeshOutput / ExposedObject /
    //     ShaderKeyword 等
    // ================================================================
    [Flags]
    internal enum VFXInstancingDisabledReason
    {
        None = 0,
        [Description("A system is using indirect draw.")]
        IndirectDraw = 1 << 0,
        [Description("The effect is using output events.")]
        OutputEvent = 1 << 1,
        [Description("The effect is using GPU events.")]
        GPUEvent = 1 << 2,
        [Description("An Initialize node has Bounds Mode set to 'Automatic'.")]
        AutomaticBounds = 1 << 3,
        [Description("The effect contains a mesh output.")]
        MeshOutput = 1 << 4,
        [Description("The effect has exposed texture, mesh or graphics buffer properties.")]
        ExposedObject = 1 << 5,
        [Description("The effect uses Shader Keywords in particle output.")]
        ShaderKeyword = 1 << 6,
        [Description("Unknown reason.")]
        Unknown = -1
    };

    // ================================================================
    // VFXCompilationMode —— VFX 编译模式
    //
    // 🎯 控制 VFX Graph 的编译目标：Runtime（运行时）/ Edition（编辑器）
    // ================================================================
    internal enum VFXCompilationMode
    {
        Runtime = 0,
        Edition = 1
    }

    // ================================================================
    // VFXMainCameraBufferFallback —— 主相机缓冲区回退策略
    //
    // 🎯 当主相机不可用时，控制场景相机缓冲区的回退行为
    // 💡 选项：无回退 / 优先主相机 / 优先场景相机
    // ================================================================
    internal enum VFXMainCameraBufferFallback
    {
        NoFallback,
        PreferMainCamera,
        PreferSceneCamera
    };

    // ================================================================
    // VFXSkinnedMeshFrame —— 蒙皮网格帧选择
    //
    // 🎯 控制从 SkinnedMeshRenderer 采样的帧：当前帧或上一帧
    // 💡 Previous 用于运动模糊等需要前一帧数据的特效
    // ================================================================
    internal enum VFXSkinnedMeshFrame
    {
        Current = 0,
        Previous = 1,
    };

    // ================================================================
    // VFXSkinnedTransform —— 蒙皮骨骼变换空间
    //
    // 🎯 指定骨骼变换的坐标空间：局部根骨骼或世界根骨骼
    // ================================================================
    internal enum VFXSkinnedTransform
    {
        LocalRootBoneTransform = 0,
        WorldRootBoneTransform = 1,
    };
}
