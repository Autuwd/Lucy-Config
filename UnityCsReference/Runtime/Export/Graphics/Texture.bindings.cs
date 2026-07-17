// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Texture 系统 — Unity 纹理类型层次结构
//
// 📌 本文件包含 Unity 所有纹理类型的 C# 绑定。
//   Texture 是所有纹理的基类，派生出多种具体类型。
//
// 🏗 纹理类型继承关系：
//   Texture（基类）
//   ├── Texture2D        — 2D 纹理（最常用，图片/贴图）
//   ├── Cubemap          — 立方体贴图（天空盒/反射）
//   ├── Texture3D        — 3D 体积纹理（体积雾/医学影像）
//   ├── Texture2DArray   — 2D 纹理数组（地形纹理混合）
//   ├── CubemapArray     — 立方体贴图数组（多天空盒）
//   ├── SparseTexture    — 稀疏纹理（虚拟纹理）
//   └── RenderTexture    — 渲染纹理（渲染目标/后处理）
//       └── CustomRenderTexture — 自定义渲染纹理（程序化纹理）
//
// 💡 关键概念：
//   - GraphicsFormat：GPU 内部存储格式（R8G8B8A8_UNorm 等）
//   - TextureFormat：旧版纹理格式（通过 GraphicsFormatUtility 转换）
//   - Mipmap：纹理的多级缩小版本，用于远距离采样和抗锯齿
//   - Streaming Mipmaps：按需加载 Mip 级别，节省显存
//   - Texture Wrap Mode：纹理坐标超出 [0,1] 时的采样行为
//     (Repeat 重复 / Clamp 钳制 / Mirror 镜像 / MirrorOnce 一次镜像)
//   - Filter Mode：纹理缩放时的过滤方式
//     (Point 点采样 / Bilinear 双线性 / Trilinear 三线性)
//
// ⚡ 性能提示：
//   - Texture2D 纹理的显存占用 = 宽 × 高 × 像素字节数 × (1 + 1/3)
//   - Mipmap 增加约 33% 显存但大幅减少远距离采样的闪烁
//   - Streaming Mipmaps 让 GPU 只加载需要的 Mip 级别
//   - isReadable 控制 CPU 端是否保留纹理副本（关闭可节省内存）
//
// 📍 对应 C++ 头文件：Runtime/Graphics/Texture.h
// ==============================================================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Unity.Collections.LowLevel.Unsafe;

using TextureDimension = UnityEngine.Rendering.TextureDimension;

namespace UnityEngine
{
    // ==============================================================
    // Texture — 所有纹理类型的基类
    //
    // 🎯 继承链：Texture → Object
    //
    // 🔑 核心属性：
    //   - width / height：纹理尺寸
    //   - dimension：纹理维度（2D/3D/Cube）
    //   - graphicsFormat：GPU 格式
    //   - mipmapCount：Mip 级别数
    //   - isReadable：CPU 是否可读
    //   - wrapMode：纹理环绕模式
    //   - filterMode：过滤模式
    //   - anisoLevel：各向异性过滤级别
    //
    // 📌 Mipmap 全局设置：
    //   - anisotropicFiltering：全局各向异性过滤模式
    //   - globalMipmapLimit：全局 Mip 级别限制（已废弃）
    //   - allowThreadedTextureCreation：允许线程创建纹理
    //
    // 📌 Texture Streaming 统计：
    //   以下属性提供纹理流式加载的运行时统计信息：
    //   - totalTextureMemory / currentTextureMemory：总/当前纹理显存
    //   - streamingTextureCount：正在流式加载的纹理数量
    //   - streamingTexturePendingLoadCount：等待加载的数量
    // ==============================================================
    [NativeHeader("Runtime/Graphics/Texture.h")]
    [NativeHeader("Runtime/Streaming/TextureStreamingManager.h")]
    [UsedByNativeCode]
    public partial class Texture : Object
    {
        protected Texture() {}

        [Obsolete("masterTextureLimit has been deprecated. Use globalMipmapLimit instead (UnityUpgradable) -> globalMipmapLimit", false)]
        [NativeProperty("ActiveGlobalMipmapLimit")] extern public static int masterTextureLimit { get; set; }
        [Obsolete("globalMipmapLimit is not supported. Use QualitySettings.globalTextureMipmapLimit or Mipmap Limit Groups instead.", false)]
        [NativeProperty("ActiveGlobalMipmapLimit")] extern public static int globalMipmapLimit { get; set; }

        extern public int mipmapCount { [NativeName("GetMipmapCount")] get; }

        [NativeProperty("AnisoLimit")] extern public static AnisotropicFiltering anisotropicFiltering { get; set; }
        [NativeName("SetGlobalAnisoLimits")] extern public static void SetGlobalAnisotropicFilteringLimits(int forcedMin, int globalMax);

        public virtual GraphicsFormat graphicsFormat
        {
            get { return GraphicsFormatUtility.GetFormat(this); }
        }

        [NativeMethod(IsThreadSafe = true)]
        extern private int GetDataWidth();

        [NativeMethod(IsThreadSafe = true)]
        extern private int GetDataHeight();

        [NativeMethod(IsThreadSafe = true)]
        extern private TextureDimension GetDimension();

        // Note: not implemented setters in base class since some classes do need to actually implement them (e.g. RenderTexture)
        virtual public int width { get { return GetDataWidth(); } set { throw new NotImplementedException(); } }
        virtual public int height { get { return GetDataHeight(); } set { throw new NotImplementedException(); } }
        virtual public TextureDimension dimension { get { return GetDimension(); } set { throw new NotImplementedException(); } }

        extern internal bool isNativeTexture { [NativeName("IsNativeTexture")] get; }

        extern virtual public bool isReadable { get; }

        extern virtual internal bool isReadableRaw { [NativeName("GetIsReadableRaw")] get; }

        extern internal bool allowReadingInEditor { get; set; }

        // Note: getter for "wrapMode" returns the U mode on purpose
        extern public TextureWrapMode wrapMode { [NativeName("GetWrapModeU")] get; set; }

        extern public TextureWrapMode wrapModeU { get; set; }
        extern public TextureWrapMode wrapModeV { get; set; }
        extern public TextureWrapMode wrapModeW { get; set; }
        extern public FilterMode filterMode { get; set; }
        extern public int anisoLevel { get; set; }
        extern public float mipMapBias { get; set; }
        extern public Vector2 texelSize { [NativeName("GetTexelSize")] get; }
        extern public IntPtr GetNativeTexturePtr();
        [Obsolete("Use GetNativeTexturePtr instead.", false)]
        public int GetNativeTextureID() { return (int)GetNativeTexturePtr(); }

        extern public uint updateCount { get; }
        extern public void IncrementUpdateCount();

        [NativeMethod("GetActiveTextureColorSpace")]
        extern private int Internal_GetActiveTextureColorSpace();

        internal ColorSpace activeTextureColorSpace
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule", "Unity.UIElements")]
            get { return Internal_GetActiveTextureColorSpace() == 0 ? ColorSpace.Linear : ColorSpace.Gamma; }
        }

        [NativeMethod("GetStoredColorSpace")]
        extern private TextureColorSpace Internal_GetStoredColorSpace();

        public bool isDataSRGB
        {
            get { return Internal_GetStoredColorSpace() == TextureColorSpace.sRGB; }
        }

        extern private protected AtomicSafetyHandle GetSafetyHandle();
        [NativeMethod("GetSafetyHandleForSlice")] extern private protected AtomicSafetyHandle GetSafetyHandleForSliceImpl(int mipLevel, int face, int element);
        [NativeMethod("GetWritableImageData")] extern private protected IntPtr GetWritableImageDataImpl(int element, int mipLevel, int depthSlice);

        extern public Hash128 imageContentsHash { get; set; }

        extern public static ulong totalTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetTotalTextureMemory")]
            get;
        }

        extern public static ulong desiredTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetDesiredTextureMemory")]
            get;
        }

        extern public static ulong targetTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetTargetTextureMemory")]
            get;
        }

        extern public static ulong currentTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetCurrentTextureMemory")]
            get;
        }

        extern public static ulong nonStreamingTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetNonStreamingTextureMemory")]
            get;
        }

        extern public static ulong streamingMipmapUploadCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingMipmapUploadCount")]
            get;
        }

        extern public static ulong streamingRendererCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingRendererCount")]
            get;
        }

        extern public static ulong streamingTextureCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingTextureCount")]
            get;
        }

        extern public static ulong nonStreamingTextureCount
        {
            [FreeFunction("GetTextureStreamingManager().GetNonStreamingTextureCount")]
            get;
        }

        extern public static ulong streamingTexturePendingLoadCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingTexturePendingLoadCount")]
            get;
        }

        extern public static ulong streamingTextureLoadingCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingTextureLoadingCount")]
            get;
        }

        [FreeFunction("GetTextureStreamingManager().SetStreamingTextureMaterialDebugProperties")]
        extern public static void SetStreamingTextureMaterialDebugProperties();

        [FreeFunction("GetTextureStreamingManager().SetStreamingTextureMaterialDebugPropertiesWithSlot")]
        extern private static void SetStreamingTextureMaterialDebugPropertiesWithSlot(int materialTextureSlot);
        public static void SetStreamingTextureMaterialDebugProperties(int materialTextureSlot)
        {
            SetStreamingTextureMaterialDebugPropertiesWithSlot(materialTextureSlot);
        }

        extern public static bool streamingTextureForceLoadAll
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetForceLoadAll")]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetForceLoadAll")]
            set;
        }
        extern public static bool streamingTextureDiscardUnusedMips
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetDiscardUnusedMips")]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetDiscardUnusedMips")]
            set;
        }
        extern public static bool allowThreadedTextureCreation
        {
            [FreeFunction(Name = "Texture2DScripting::IsCreateTextureThreadedEnabled")]
            get;
            [FreeFunction(Name = "Texture2DScripting::EnableCreateTextureThreaded")]
            set;
        }

        extern internal ulong GetPixelDataSize(int mipLevel, int element = 0);
        extern internal ulong GetPixelDataOffset(int mipLevel, int element = 0);

        extern public Rendering.GraphicsTexture graphicsTexture
        {
            [FreeFunction(Name = "Texture2DScripting::GetCurrentGraphicsTexture", HasExplicitThis = true)]
            get;
        }
    }

    // ==============================================================
    // Texture2D — 2D 纹理（最常用的纹理类型）
    //
    // 🎯 继承链：Texture2D → Texture → Object
    //
    // 📌 用途：
    //   存储 2D 图片数据，用于材质贴图（漫反射、法线、高光等）。
    //   可从文件加载、代码生成、或从 RenderTexture 拷贝。
    //
    // 🔑 关键属性：
    //   - format：纹理格式（RGBA32/DXT5/ASTC 等）
    //   - alphaIsTransparency：Alpha 通道是否表示透明度
    //   - streamingMipmaps：是否启用 Mipmap 流式加载
    //   - requestedMipmapLevel：请求加载的 Mip 级别
    //
    // 🔑 关键方法：
    //   - Apply()：将 CPU 端修改上传到 GPU
    //   - GetPixels/SetPixels：像素级读写
    //   - GetRawTextureData：获取原始字节数据
    //   - ReadPixels()：从屏幕读取像素
    //   - Compress()：运行时压缩纹理
    //   - Reinitialize()：重新初始化纹理尺寸和格式
    //
    // 📌 内置纹理（Built-in Textures）：
    //   - whiteTexture：纯白纹理（1×1）
    //   - blackTexture：纯黑纹理（1×1）
    //   - redTexture：纯红纹理（1×1）
    //   - normalTexture：默认法线贴图（朝上方向）
    //
    // ⚡ Texture2D vs RenderTexture：
    //   - Texture2D：存储静态图片数据，CPU 端创建
    //   - RenderTexture：作为渲染目标，GPU 端渲染
    //   - RenderTexture → Texture2D：通过 ReadPixels 拷贝
    //   - Texture2D → GPU 纹理：通过 Apply 上传
    //
    // 📌 Mipmap 流式加载相关属性：
    //   - calculatedMipmapLevel：GPU 计算的所需 Mip 级别
    //   - desiredMipmapLevel：期望的 Mip 级别
    //   - loadingMipmapLevel / loadedMipmapLevel：加载中的/已加载的
    // ==============================================================
    [NativeHeader("Runtime/Graphics/Texture2D.h")]
    [NativeHeader("Runtime/Graphics/GeneratedTextures.h")]
    [HelpURL("texture-type-default")] // 2D texture is considering the 'default' texture, so it hasn't been given it own dedicated 'class-Texture2D' manual page
    [UsedByNativeCode]
    [ExcludeFromPreset]
    public sealed partial class Texture2D : Texture
    {
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        extern private bool IgnoreMipmapLimit();
        extern private void SetIgnoreMipmapLimitAndReload(bool value);

        extern public string mipmapLimitGroup
        {
            [NativeName("GetMipmapLimitGroupName")] get;
        }

        extern public int activeMipmapLimit
        {
            [NativeName("GetMipmapLimit")] get;
        }

        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D whiteTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D blackTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D redTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D grayTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D linearGrayTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D normalTexture { get; }

        extern public void Compress(bool highQuality);

        [FreeFunction("Texture2DScripting::CreateEmpty")]
        extern private static bool Internal_CreateEmptyImpl([Writable] Texture2D mono);
        [FreeFunction("Texture2DScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Texture2D mono, int w, int h, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex, bool ignoreMipmapLimit, string mipmapLimitGroupName);
        private static void Internal_Create([Writable] Texture2D mono, int w, int h, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex, bool ignoreMipmapLimit, string mipmapLimitGroupName)
        {
            if (!Internal_CreateImpl(mono, w, h, mipCount, format, colorSpace, flags, nativeTex, ignoreMipmapLimit, mipmapLimitGroupName))
                throw new UnityException("Failed to create texture because of invalid parameters.");
        }

        extern override public bool isReadable { get; }
        [NativeConditional("ENABLE_VIRTUALTEXTURING && UNITY_EDITOR")][NativeName("VTOnly")] extern public bool vtOnly { get; }
        [NativeName("Apply")] extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);
        [NativeName("Reinitialize")] extern private bool ReinitializeImpl(int width, int height);
        [NativeName("SetPixel")] extern private void SetPixelImpl(int image, int mip, int x, int y, Color color);
        [NativeName("GetPixel")] extern private Color GetPixelImpl(int image, int mip, int x, int y);
        [NativeName("GetPixelBilinear")] extern private Color GetPixelBilinearImpl(int image, int mip, float u, float v);

        [FreeFunction(Name = "Texture2DScripting::ReinitializeWithFormat", HasExplicitThis = true)]
        extern private bool ReinitializeWithFormatImpl(int width, int height, GraphicsFormat format, bool hasMipMap);

        [FreeFunction(Name = "Texture2DScripting::ReinitializeWithTextureFormat", HasExplicitThis = true)]
        extern private bool ReinitializeWithTextureFormatImpl(int width, int height, TextureFormat textureFormat, bool hasMipMap);

        [FreeFunction(Name = "Texture2DScripting::ReadPixels", HasExplicitThis = true)]
        extern private void ReadPixelsImpl(Rect source, int destX, int destY, bool recalculateMipMaps);


        [FreeFunction(Name = "Texture2DScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetPixelsImpl(int x, int y, int w, int h, Color[] pixel, int miplevel, int frame);

        [FreeFunction(Name = "Texture2DScripting::LoadRawData", HasExplicitThis = true)]
        extern private bool LoadRawTextureDataImpl(IntPtr data, ulong size);

        [FreeFunction(Name = "Texture2DScripting::LoadRawData", HasExplicitThis = true)]
        extern private bool LoadRawTextureDataImplArray(byte[] data);

        [FreeFunction(Name = "Texture2DScripting::SetPixelDataSpan", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplSpan(Span<byte> data, int mipLevel, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture2DScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        extern private ulong GetDataSize();

        private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel)
        {
            return GetSafetyHandleForSliceImpl(mipLevel, 0, 0);
        }
        private IntPtr GetWritableImageData(int mipLevel = 0)
        {
            return GetWritableImageDataImpl(0, mipLevel, 0);
        }

        [FreeFunction("Texture2DScripting::GenerateAtlas")]
        extern private static void GenerateAtlasImpl(Vector2[] sizes, int padding, int atlasSize, [Out] Rect[] rect);
        extern internal bool isPreProcessed { get; }

        // Must be kept in sync with their C++ counterparts. See Texture2D::s_StreamingMipmapsPriority[Min|Max].
        internal const int streamingMipmapsPriorityMin = SByte.MinValue;
        internal const int streamingMipmapsPriorityMax = SByte.MaxValue;

        extern public bool streamingMipmaps { get; }
        extern public int streamingMipmapsPriority { get; }

        extern public int requestedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetRequestedMipmapLevel", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetRequestedMipmapLevel", HasExplicitThis = true)]
            set;
        }
        extern public int minimumMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetMinimumMipmapLevel", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetMinimumMipmapLevel", HasExplicitThis = true)]
            set;
        }

        extern internal bool loadAllMips
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadAllMips", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetLoadAllMips", HasExplicitThis = true)]
            set;
        }

        extern public int calculatedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetCalculatedMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int desiredMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetDesiredMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int loadingMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadingMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int loadedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadedMipmapLevel", HasExplicitThis = true)]
            get;
        }

        [FreeFunction(Name = "GetTextureStreamingManager().ClearRequestedMipmapLevel", HasExplicitThis = true)]
        extern public void ClearRequestedMipmapLevel();

        [FreeFunction(Name = "GetTextureStreamingManager().IsRequestedMipmapLevelLoaded", HasExplicitThis = true)]
        extern public bool IsRequestedMipmapLevelLoaded();

        [FreeFunction(Name = "GetTextureStreamingManager().ClearMinimumMipmapLevel", HasExplicitThis = true)]
        extern public void ClearMinimumMipmapLevel();

        [FreeFunction("Texture2DScripting::UpdateExternalTexture", HasExplicitThis = true)]
        extern public void UpdateExternalTexture(IntPtr nativeTex);

        [FreeFunction("Texture2DScripting::SetAllPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetAllPixels32(Color32[] colors, int miplevel);

        [FreeFunction("Texture2DScripting::SetBlockOfPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetBlockOfPixels32(int x, int y, int blockWidth, int blockHeight, Color32[] colors, int miplevel);

        [FreeFunction("Texture2DScripting::GetRawTextureData", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public byte[] GetRawTextureData();

        [FreeFunction("Texture2DScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Color[] GetPixels(int x, int y, int blockWidth, int blockHeight, [uei.DefaultValue("0")] int miplevel);

        [uei.ExcludeFromDocs]
        public Color[] GetPixels(int x, int y, int blockWidth, int blockHeight)
        {
            return GetPixels(x, y, blockWidth, blockHeight, 0);
        }

        [FreeFunction("Texture2DScripting::GetPixels32", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Color32[] GetPixels32([uei.DefaultValue("0")] int miplevel);

        [uei.ExcludeFromDocs]
        public Color32[] GetPixels32()
        {
            return GetPixels32(0);
        }

        [FreeFunction("Texture2DScripting::PackTextures", HasExplicitThis = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Rect[] PackTextures(Texture2D[] textures, int padding, int maximumAtlasSize, bool makeNoLongerReadable);

        public Rect[] PackTextures(Texture2D[] textures, int padding, int maximumAtlasSize)
        {
            return PackTextures(textures, padding, maximumAtlasSize, false);
        }

        public Rect[] PackTextures(Texture2D[] textures, int padding)
        {
            return PackTextures(textures, padding, 2048);
        }

        [FreeFunction(Name = "Texture2DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "Texture2DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstMip);

        [FreeFunction(Name = "Texture2DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstMip, int dstX, int dstY);

        extern public bool alphaIsTransparency { get; set; }

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "Unity.UIElements")]
        extern internal float pixelsPerPoint { get; set; }
    }

    // ==============================================================
    // Cubemap — 立方体贴图（6 面纹理）
    //
    // 🎯 继承链：Cubemap → Texture → Object
    //
    // 📌 用途：
    //   存储 6 个面的纹理数据（前/后/左/右/上/下），
    //   形成一个完整的立方体环境映射。
    //
    // 🔑 典型应用场景：
    //   - 天空盒（Skybox）：6 张面片组成天空环境
    //   - 反射贴图：物体表面反射周围环境
    //   - 光照探针数据：存储环境光照信息
    //   - Cookie 纹理：点光源的 6 面投影
    //
    // 🔑 关键方法：
    //   - GetPixels(face, miplevel)：获取指定面的像素
    //   - SetPixels(colors, face, miplevel)：设置指定面的像素
    //   - SmoothEdges()：平滑立方体边缘接缝
    //
    // 📌 CubemapFace 枚举：
    //   PositiveX(+X) / NegativeX(-X)
    //   PositiveY(+Y) / NegativeY(-Y)
    //   PositiveZ(+Z) / NegativeZ(-Z)
    // ==============================================================
    [NativeHeader("Runtime/Graphics/CubemapTexture.h")]
    [ExcludeFromPreset]
    public sealed partial class Cubemap : Texture
    {
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        [FreeFunction("CubemapScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Cubemap mono, int ext, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex);
        private static void Internal_Create([Writable] Cubemap mono, int ext, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex)
        {
            if (!Internal_CreateImpl(mono, ext, mipCount, format, colorSpace, flags, nativeTex))
                throw new UnityException("Failed to create texture because of invalid parameters.");
        }

        [FreeFunction(Name = "CubemapScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);

        [FreeFunction("CubemapScripting::UpdateExternalTexture", HasExplicitThis = true)]
        extern public void UpdateExternalTexture(IntPtr nativeTexture);

        extern override public bool isReadable { get; }
        [NativeName("SetPixel")] extern private void SetPixelImpl(int image, int mip, int x, int y, Color color);
        [NativeName("GetPixel")] extern private Color GetPixelImpl(int image, int mip, int x, int y);

        [NativeName("FixupEdges")] extern public void SmoothEdges([uei.DefaultValue("1")] int smoothRegionWidthInPixels);
        public void SmoothEdges() { SmoothEdges(1); }

        [FreeFunction(Name = "CubemapScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Color[] GetPixels(CubemapFace face, int miplevel);

        public Color[] GetPixels(CubemapFace face)
        {
            return GetPixels(face, 0);
        }

        [FreeFunction(Name = "CubemapScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels(Color[] colors, CubemapFace face, int miplevel);

        [FreeFunction(Name = "CubemapScripting::SetPixelDataSpan", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplSpan(Span<byte> data, int mipLevel, int face, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "CubemapScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int face, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        public void SetPixels(Color[] colors, CubemapFace face)
        {
            SetPixels(colors, face, 0);
        }

        [FreeFunction(Name = "CubemapScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "CubemapScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstFace, int dstMip);

        [FreeFunction(Name = "CubemapScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstFace, int dstMip, int dstX, int dstY);

        private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel, int face)
        {
            return GetSafetyHandleForSliceImpl(mipLevel, face, 0);
        }
        private IntPtr GetWritableImageData(int element = 0, int mipLevel = 0)
        {
            return GetWritableImageDataImpl(element, mipLevel, 0);
        }

        extern internal bool isPreProcessed { get; }

        extern public bool streamingMipmaps { get; }
        extern public int streamingMipmapsPriority { get; }

        extern public int requestedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetRequestedMipmapLevel", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetRequestedMipmapLevel", HasExplicitThis = true)]
            set;
        }

        extern internal bool loadAllMips
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadAllMips", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetLoadAllMips", HasExplicitThis = true)]
            set;
        }

        extern public int desiredMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetDesiredMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int loadingMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadingMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int loadedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadedMipmapLevel", HasExplicitThis = true)]
            get;
        }

        [FreeFunction(Name = "GetTextureStreamingManager().ClearRequestedMipmapLevel", HasExplicitThis = true)]
        extern public void ClearRequestedMipmapLevel();

        [FreeFunction(Name = "GetTextureStreamingManager().IsRequestedMipmapLevelLoaded", HasExplicitThis = true)]
        extern public bool IsRequestedMipmapLevelLoaded();

    }

    // ==============================================================
    // Texture3D — 3D 体积纹理
    //
    // 🎯 继承链：Texture3D → Texture → Object
    //
    // 📌 用途：
    //   存储三维体积数据（宽 × 高 × 深），每个"体素"（Voxel）是一个像素。
    //
    // 🔑 典型应用场景：
    //   - 体积雾/云（Volume Fog）：通过 3D 纹理采样密度场
    //   - 医学影像：CT/MRI 扫描数据可视化
    //   - 体积光照：3D 光照数据存储
    //   - 3D LUT 颜色查找表：后处理颜色校正
    //   - 程序化纹理生成：噪声纹理的 3D 切片
    //
    // 🔑 关键属性：
    //   - depth：纹理深度（层数，即 Z 方向的分辨率）
    //   - format：纹理格式
    //
    // 🔑 关键方法：
    //   - GetPixels(miplevel) / SetPixels(colors, miplevel)：体素级读写
    //   - GetPixels32 / SetPixels32：Color32 版本（节省内存）
    // ==============================================================
    [NativeHeader("Runtime/Graphics/Texture3D.h")]
    [ExcludeFromPreset]
    public sealed partial class Texture3D : Texture
    {
        extern public int depth { [NativeName("GetTextureLayerCount")] get; }
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        extern override public bool isReadable { get; }
        [NativeName("SetPixel")] extern private void SetPixelImpl(int mip, int x, int y, int z, Color color);
        [NativeName("GetPixel")] extern private Color GetPixelImpl(int mip, int x, int y, int z);
        [NativeName("GetPixelBilinear")] extern private Color GetPixelBilinearImpl(int mip, float u, float v, float w);

        [FreeFunction("Texture3DScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Texture3D mono, int w, int h, int d, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex);
        private static void Internal_Create([Writable] Texture3D mono, int w, int h, int d, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex)
        {
            if (!Internal_CreateImpl(mono, w, h, d, mipCount, format, colorSpace, flags, nativeTex))
                throw new UnityException("Failed to create texture because of invalid parameters.");
        }

        [FreeFunction("Texture3DScripting::UpdateExternalTexture", HasExplicitThis = true)]
        extern public void UpdateExternalTexture(IntPtr nativeTex);
        [FreeFunction(Name = "Texture3DScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);

        [FreeFunction(Name = "Texture3DScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Color[] GetPixels(int miplevel);

        public Color[] GetPixels()
        {
            return GetPixels(0);
        }

        [FreeFunction(Name = "Texture3DScripting::GetPixels32", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Color32[] GetPixels32(int miplevel);

        public Color32[] GetPixels32()
        {
            return GetPixels32(0);
        }

        [FreeFunction(Name = "Texture3DScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels(Color[] colors, int miplevel);

        public void SetPixels(Color[] colors)
        {
            SetPixels(colors, 0);
        }

        [FreeFunction(Name = "Texture3DScripting::SetPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels32(Color32[] colors, int miplevel);

        public void SetPixels32(Color32[] colors)
        {
            SetPixels32(colors, 0);
        }

        [FreeFunction(Name = "Texture3DScripting::SetPixelDataSpan", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplSpan(Span<byte> data, int mipLevel, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture3DScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture3DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "Texture3DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstElement, int dstMip);

        [FreeFunction(Name = "Texture3DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstElement, int dstMip, int dstX, int dstY);

        private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel)
        {
            return GetSafetyHandleForSliceImpl(mipLevel, 0, 0);
        }
        private IntPtr GetWritableImageData(int depthSlice = 0, int mipLevel = 0)
        {
            return GetWritableImageDataImpl(0, mipLevel, depthSlice);
        }
    }

    // ==============================================================
    // Texture2DArray — 2D 纹理数组
    //
    // 🎯 继承链：Texture2DArray → Texture → Object
    //
    // 📌 用途：
    //   存储多个相同尺寸的 2D 纹理到一个数组中，
    //   GPU 通过索引访问不同"层"（Slice）。
    //
    // 🔑 典型应用场景：
    //   - 地形纹理混合：存储多种地形贴图（草地/沙地/岩石），
    //     在着色器中根据地形权重混合采样
    //   - 粒子系统：多帧动画序列存储在一个数组中
    //   - 地下室/室内场景：多层楼的光照贴图
    //
    // 📌 与 Texture3D 的区别：
    //   - Texture2DArray：每层是完整的 2D 纹理，层间无插值
    //   - Texture3D：三个维度都可以线性插值
    //
    // 📌 allSlices：特殊标识符，表示操作所有层
    // ==============================================================
    [NativeHeader("Runtime/Graphics/Texture2DArray.h")]
    [ExcludeFromPreset]
    public sealed partial class Texture2DArray : Texture
    {
        extern static public int allSlices { [NativeName("GetAllTextureLayersIdentifier")] get; }
        extern public int depth { [NativeName("GetTextureLayerCount")] get; }
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        extern private bool IgnoreMipmapLimit();
        extern private void SetIgnoreMipmapLimitAndReload(bool value);

        extern public string mipmapLimitGroup
        {
            [NativeName("GetMipmapLimitGroupName")] get;
        }

        extern public int activeMipmapLimit
        {
            [NativeName("GetMipmapLimit")] get;
        }

        extern override public bool isReadable { get; }

        [FreeFunction("Texture2DArrayScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Texture2DArray mono, int w, int h, int d, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, bool ignoreMipmapLimit, string mipmapLimitGroupName);
        private static void Internal_Create([Writable] Texture2DArray mono, int w, int h, int d, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, bool ignoreMipmapLimit, string mipmapLimitGroupName)
        {
            if (!Internal_CreateImpl(mono, w, h, d, mipCount, format, colorSpace, flags, ignoreMipmapLimit, mipmapLimitGroupName))
                throw new UnityException("Failed to create 2D array texture because of invalid parameters.");
        }

        [FreeFunction(Name = "Texture2DArrayScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);

        [FreeFunction(Name = "Texture2DArrayScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Color[] GetPixels(int arrayElement, int miplevel);

        public Color[] GetPixels(int arrayElement)
        {
            return GetPixels(arrayElement, 0);
        }

        [FreeFunction(Name = "Texture2DArrayScripting::SetPixelDataSpan", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplSpan(Span<byte> data, int mipLevel, int element, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture2DArrayScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int element, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture2DArrayScripting::GetPixels32", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Color32[] GetPixels32(int arrayElement, int miplevel);

        public Color32[] GetPixels32(int arrayElement)
        {
            return GetPixels32(arrayElement, 0);
        }

        [FreeFunction(Name = "Texture2DArrayScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels(Color[] colors, int arrayElement, int miplevel);

        public void SetPixels(Color[] colors, int arrayElement)
        {
            SetPixels(colors, arrayElement, 0);
        }

        [FreeFunction(Name = "Texture2DArrayScripting::SetPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels32(Color32[] colors, int arrayElement, int miplevel);

        public void SetPixels32(Color32[] colors, int arrayElement)
        {
            SetPixels32(colors, arrayElement, 0);
        }

        [FreeFunction(Name = "Texture2DArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "Texture2DArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstElement, int dstMip);

        [FreeFunction(Name = "Texture2DArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstElement, int dstMip, int dstX, int dstY);

        private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel, int element)
        {
            return GetSafetyHandleForSliceImpl(mipLevel, 0, element);
        }
        private IntPtr GetWritableImageData(int element = 0, int mipLevel = 0)
        {
            return GetWritableImageDataImpl(element, mipLevel, 0);
        }
    }

    // ==============================================================
    // CubemapArray — 立方体贴图数组
    //
    // 🎯 继承链：CubemapArray → Texture → Object
    //
    // 📌 用途：
    //   存储多个 Cubemap 到一个数组中，GPU 按索引选择 Cubemap。
    //
    // 🔑 典型应用场景：
    //   - 反射探针数组：存储不同位置的环境反射数据
    //   - 多天空盒切换：存储多个天空盒 Cubemap
    //   - 行星级游戏：不同行星的天空环境
    //
    // 📌 cubemapCount：数组中 Cubemap 的数量
    // 📌 每个 Cubemap 有 6 个面，每面可有多个 Mip 级别
    // ==============================================================
    [NativeHeader("Runtime/Graphics/CubemapArrayTexture.h")]
    [ExcludeFromPreset]
    public sealed partial class CubemapArray : Texture
    {
        extern public int cubemapCount { get; }
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        extern override public bool isReadable { get; }

        [FreeFunction("CubemapArrayScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] CubemapArray mono, int ext, int count, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags);
        private static void Internal_Create([Writable] CubemapArray mono, int ext, int count, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags)
        {
            if (!Internal_CreateImpl(mono, ext, count, mipCount, format, colorSpace, flags))
                throw new UnityException("Failed to create cubemap array texture because of invalid parameters.");
        }

        [FreeFunction(Name = "CubemapArrayScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);

        [FreeFunction(Name = "CubemapArrayScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Color[] GetPixels(CubemapFace face, int arrayElement, int miplevel);

        public Color[] GetPixels(CubemapFace face, int arrayElement)
        {
            return GetPixels(face, arrayElement, 0);
        }

        [FreeFunction(Name = "CubemapArrayScripting::GetPixels32", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public Color32[] GetPixels32(CubemapFace face, int arrayElement, int miplevel);

        public Color32[] GetPixels32(CubemapFace face, int arrayElement)
        {
            return GetPixels32(face, arrayElement, 0);
        }

        [FreeFunction(Name = "CubemapArrayScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels(Color[] colors, CubemapFace face, int arrayElement, int miplevel);

        public void SetPixels(Color[] colors, CubemapFace face, int arrayElement)
        {
            SetPixels(colors, face, arrayElement, 0);
        }

        [FreeFunction(Name = "CubemapArrayScripting::SetPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels32(Color32[] colors, CubemapFace face, int arrayElement, int miplevel);

        public void SetPixels32(Color32[] colors, CubemapFace face, int arrayElement)
        {
            SetPixels32(colors, face, arrayElement, 0);
        }

        [FreeFunction(Name = "CubemapArrayScripting::SetPixelDataSpan", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplSpan(Span<byte> data, int mipLevel, int face, int element, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "CubemapArrayScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int face, int element, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "CubemapArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "CubemapArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstElement, int dstMip);

        [FreeFunction(Name = "CubemapArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstElement, int dstMip, int dstX, int dstY);

        private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel, int face, int element)
        {
            return GetSafetyHandleForSliceImpl(mipLevel, face, element);
        }
        private IntPtr GetWritableImageData(int element = 0, int mipLevel = 0)
        {
            return GetWritableImageDataImpl(element, mipLevel, 0);
        }
    }

    // ==============================================================
    // SparseTexture — 稀疏纹理（虚拟纹理）
    //
    // 🎯 继承链：SparseTexture → Texture → Object
    //
    // 📌 用途：
    //   使用硬件稀疏纹理支持（Virtual Texturing），
    //   仅将实际使用的纹理块（Tile）加载到显存中。
    //   适用于超大纹理（如开放世界地形纹理）。
    //
    // 🔑 关键属性：
    //   - tileWidth / tileHeight：纹理块尺寸
    //   - isCreated：是否已创建
    //
    // 🔑 关键方法：
    //   - UpdateTile()：更新指定 Tile 的数据
    //   - UpdateTileRaw()：更新原始字节数据
    //   - UnloadTile()：卸载指定 Tile（释放显存）
    //
    // 💡 稀疏纹理按需加载：
    //   只有被渲染器实际采样的 Tile 才会占用显存，
    //   未使用的 Tile 不消耗 GPU 内存。
    // ==============================================================
    [NativeHeader("Runtime/Graphics/SparseTexture.h")]
    public sealed partial class SparseTexture : Texture
    {
        extern public int tileWidth { get; }
        extern public int tileHeight { get; }
        extern public bool isCreated { [NativeName("IsInitialized")] get; }

        [FreeFunction(Name = "SparseTextureScripting::Create", ThrowsException = true)]
        extern private static void Internal_Create([Writable] SparseTexture mono, int width, int height, GraphicsFormat format, TextureColorSpace colorSpace, int mipCount);

        [FreeFunction(Name = "SparseTextureScripting::UpdateTile", HasExplicitThis = true)]
        extern public void UpdateTile(int tileX, int tileY, int miplevel, Color32[] data);

        [FreeFunction(Name = "SparseTextureScripting::UpdateTileRaw", HasExplicitThis = true)]
        extern public void UpdateTileRaw(int tileX, int tileY, int miplevel, byte[] data);

        public void UnloadTile(int tileX, int tileY, int miplevel)
        {
            UpdateTileRaw(tileX, tileY, miplevel, null);
        }
    }

    // ==============================================================
    // RenderTexture — 渲染纹理（GPU 渲染目标）
    //
    // 🎯 继承链：RenderTexture → Texture → Object
    //
    // 📌 用途：
    //   RenderTexture 可作为 GPU 渲染的目标缓冲区，
    //   即"把场景渲染到一张纹理上而不是屏幕上"。
    //   渲染完成后，纹理可作为材质输入或屏幕特效处理。
    //
    // 🔑 典型应用场景：
    //   - 屏幕后处理（Post-Processing）：模糊、泛光、色调映射
    //   - 画中画（Picture-in-Picture）：多摄像机渲染到不同 RT
    //   - 水面/镜子反射：渲染反射视图到 RT
    //   - 阴影贴图（Shadow Map）：深度信息存储
    //   - UV 动画：渲染动态纹理用于 UV 偏移
    //   - 动态天空盒：程序化生成天空盒纹理
    //
    // 📌 关键属性：
    //   - width / height：渲染目标尺寸
    //   - format / graphicsFormat：颜色格式
    //   - depthStencilFormat：深度/模板格式
    //   - useMipMap：是否生成 Mipmap
    //   - autoGenerateMips：是否自动生成 Mipmap
    //   - antiAliasing：抗锯齿采样数（MSAA）
    //   - enableRandomWrite：是否可作为 UAV（计算着色器写入）
    //   - useDynamicScale：动态缩放（跟随屏幕分辨率）
    //   - volumeDepth：3D 渲染纹理的深度（分层渲染）
    //
    // 📌 RenderTexture vs Texture2D：
    //   - RenderTexture：GPU 端创建和渲染，适合实时渲染
    //   - Texture2D：CPU 端创建，适合静态图片和运行时生成
    //   - 可互相转换：Graphics.Blit() / RenderTexture.ReadPixels()
    //
    // 📌 活动 RenderTexture（active）：
    //   全局静态属性，指定当前的渲染目标。
    //   设置为 null 则渲染到屏幕。
    //
    // 💡 GetTemporary / ReleaseTemporary：
    //   从 RenderBufferManager 池中获取/释放临时 RT，
    //   避免频繁创建销毁，是推荐的 RT 使用方式。
    // ==============================================================
    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    [NativeHeader("Runtime/Graphics/RenderBufferManager.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    [UsedByNativeCode]
    public partial class RenderTexture : Texture
    {
        override extern public int width { get; set; }
        override extern public int height { get; set; }

        override extern public TextureDimension dimension { get; set; }

        [NativeName("GetColorFormat")] extern private GraphicsFormat GetColorFormat(bool suppressWarnings);
        [NativeName("SetColorFormat")] extern private void SetColorFormat(GraphicsFormat format);
        public new GraphicsFormat graphicsFormat { get{ return GetColorFormat(true); } set { SetColorFormat(value); } } // Getter should not log warnings

        [NativeProperty("MipMap")]                  extern public bool useMipMap { get; set; }
        [NativeProperty("SRGBReadWrite")]           extern public bool sRGB { get; }
        [NativeProperty("VRUsage")]                 extern public VRTextureUsage vrUsage { get; set; }
        [NativeProperty("Memoryless")]              extern public RenderTextureMemoryless memorylessMode { get; set; }


        public RenderTextureFormat format
        {
            get
            {
                if (graphicsFormat != GraphicsFormat.None)
                {
                    return GraphicsFormatUtility.GetRenderTextureFormat(graphicsFormat);
                }
                else // If graphicsFormat is None, then the RT is a depth-only RT.
                {
                    return (GetDescriptor().shadowSamplingMode != ShadowSamplingMode.None) ? RenderTextureFormat.Shadowmap : RenderTextureFormat.Depth;
                }
            }
            // Setter can produce any of these following valid combinations, other combos are invalid. ('depthStencilFormat' untouched unless RTFormat infers a depth-only RT)
            // graphicsFormat: None                                     depthStencilFormat: a depth-stencil format (D16_UNorm, ...)     <- depth-only RT.
            // graphicsFormat: a color format (R8G8B8A8_SRGB, ...)      depthStencilFormat: a depth-stencil format (D16_UNorm, ...)     <- color + depth RT.
            // graphicsFormat: a color format (R8G8B8A8_SRGB, ...)      depthStencilFormat: None                                        <- color-only RT.
            set
            {
                var shadowSamplingMode = RenderTexture.GetShadowSamplingModeForFormat(value);
                SetShadowSamplingMode(shadowSamplingMode);
                GraphicsFormat requestedFormat = GraphicsFormatUtility.GetGraphicsFormat(value, sRGB);
                graphicsFormat = SystemInfo.GetCompatibleFormat(requestedFormat, GraphicsFormatUsage.Render);
                depthStencilFormat = RenderTexture.GetDepthStencilFormatLegacy(depth, value);
            }
        }

        extern public GraphicsFormat stencilFormat { get; set; }

        extern public GraphicsFormat depthStencilFormat { get; set; }

        extern public bool autoGenerateMips { get; set; }
        extern public int volumeDepth { get; set; }
        extern public int antiAliasing { get; set; }
        extern public bool bindTextureMS { get; set; }
        extern public bool enableRandomWrite { get; set; }
        extern public bool useDynamicScale { get; set; }
        extern public bool useDynamicScaleExplicit { get; set; }
        extern public bool enableShadingRate { get; set; }

        extern public void ApplyDynamicScale();

        // for some reason we are providing isPowerOfTwo setter which is empty (i dont know what the intent is/was)
        extern private bool GetIsPowerOfTwo();
        public bool isPowerOfTwo { get { return GetIsPowerOfTwo(); } set {} }


        [FreeFunction("RenderTexture::GetActiveAsRenderTexture")] extern private static RenderTexture GetActive();
        [FreeFunction("RenderTextureScripting::SetActive")] extern private static void SetActive(RenderTexture rt);
        public static RenderTexture active { get { return GetActive(); } set { SetActive(value); } }

        [FreeFunction(Name = "RenderTextureScripting::GetColorBuffer", HasExplicitThis = true)]
        extern private RenderBuffer GetColorBuffer();
        [FreeFunction(Name = "RenderTextureScripting::GetDepthBuffer", HasExplicitThis = true)]
        extern private RenderBuffer GetDepthBuffer();

        extern private void SetMipMapCount(int count);

        extern internal void SetShadowSamplingMode(Rendering.ShadowSamplingMode samplingMode);

        public RenderBuffer colorBuffer { get { return GetColorBuffer(); } }
        public RenderBuffer depthBuffer { get { return GetDepthBuffer(); } }

        extern public IntPtr GetNativeDepthBufferPtr();


        extern public void DiscardContents(bool discardColor, bool discardDepth);
        [Obsolete("This function has no effect.", false)]
        extern public void MarkRestoreExpected();
        public void DiscardContents() { DiscardContents(true, true); }


        [NativeName("ResolveAntiAliasedSurface")] extern private void ResolveAA();
        [NativeName("ResolveAntiAliasedSurface")] extern private void ResolveAATo(RenderTexture rt);

        public void ResolveAntiAliasedSurface() { ResolveAA(); }
        public void ResolveAntiAliasedSurface(RenderTexture target) { ResolveAATo(target); }


        [FreeFunction(Name = "RenderTextureScripting::SetGlobalShaderProperty", HasExplicitThis = true)]
        extern public void SetGlobalShaderProperty(string propertyName);


        extern public bool Create();
        extern public void Release();
        extern public bool IsCreated();
        extern public void GenerateMips();
        [NativeMethod(ThrowsException = true)]
        extern public void ConvertToEquirect(RenderTexture equirect, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono);

        extern internal void SetSRGBReadWrite(bool srgb);

        [FreeFunction("RenderTextureScripting::Create")] extern private static void Internal_Create([Writable] RenderTexture rt);

        [FreeFunction("RenderTextureSupportsStencil")] extern public static bool SupportsStencil(RenderTexture rt);

        [NativeName("SetRenderTextureDescFromScript")]
        extern private void SetRenderTextureDescriptor(RenderTextureDescriptor desc);

        [NativeName("GetRenderTextureDesc")]
        extern private RenderTextureDescriptor GetDescriptor();

        [FreeFunction("GetRenderBufferManager().GetTextures().GetTempBuffer")]
        extern private static RenderTexture GetTemporary_Internal(RenderTextureDescriptor desc);


        [FreeFunction("GetRenderBufferManager().GetTextures().ReleaseTempBuffer")]
        extern public static void ReleaseTemporary(RenderTexture temp);

        extern public int depth
        {
            [FreeFunction("RenderTextureScripting::GetDepth", HasExplicitThis = true)]
            get;
            [FreeFunction("RenderTextureScripting::SetDepth", HasExplicitThis = true)]
            set;
        }
    }

    // ==============================================================
    // CustomRenderTexture — 自定义渲染纹理（程序化纹理生成）
    //
    // 🎯 继承链：CustomRenderTexture → RenderTexture → Texture → Object
    //
    // 📌 用途：
    //   通过 Shader 自动更新的 RenderTexture，无需 CPU 参与。
    //   可实现程序化纹理、动态纹理、基于物理的纹理生成。
    //
    // 🔑 工作原理：
    //   1. 初始化阶段：使用 initializationMaterial 渲染初始内容
    //   2. 更新阶段：使用 material（Shader）每帧/定期更新纹理内容
    //   3. 支持"更新区域"（Update Zones）：只更新纹理的部分区域
    //
    // 🔑 关键属性：
    //   - material：用于更新的材质（Shader）
    //   - initializationMaterial / initializationTexture：初始化材质和纹理
    //   - updateMode：更新模式（Realtime/On Demand/Realtime）
    //   - initializationMode：初始化模式（On Load/On Demand）
    //   - shaderPass：Shader 的 Pass 索引
    //   - doubleBuffered：双缓冲（读写交替，避免竞争）
    //   - updatePeriod：更新周期（秒，0=每帧更新）
    //   - wrapUpdateZones：更新区域是否环绕边界
    //
    // 📌 更新区域（CustomRenderTextureUpdateZone）：
    //   定义纹理中的矩形区域和旋转角度，
    //   只在这个区域内执行 Shader 更新，节省 GPU 开销。
    //
    // 💡 典型用途：
    //   - 程序化噪声/花纹生成
    //   - 简单的 GPU 模拟（流体/烟雾）
    //   - 动态渐变背景
    //   - UI 动画纹理
    // ==============================================================
    [System.Serializable]
    [UsedByNativeCode]
    public struct CustomRenderTextureUpdateZone
    {
        public Vector3 updateZoneCenter;
        public Vector3 updateZoneSize;
        public float rotation;
        public int passIndex;
        public bool needSwap;
    }

    [UsedByNativeCode]
    [NativeHeader("Runtime/Graphics/CustomRenderTexture.h")]
    public sealed partial class CustomRenderTexture : RenderTexture
    {
        [FreeFunction(Name = "CustomRenderTextureScripting::Create")]
        extern private static void Internal_CreateCustomRenderTexture([Writable] CustomRenderTexture rt);

        [NativeName("TriggerUpdate")]
        extern void TriggerUpdate(int count);

        public void Update(int count)
        {
            CustomRenderTextureManager.InvokeTriggerUpdate(this, count);
            TriggerUpdate(count);
        }

        public void Update()
        {
            Update(1);
        }

        [NativeName("TriggerInitialization")]
        extern void TriggerInitialization();

        public void Initialize()
        {
            TriggerInitialization();
            CustomRenderTextureManager.InvokeTriggerInitialize(this);
        }

        extern public void ClearUpdateZones();

        extern public Material material { get; set; }

        extern public Material initializationMaterial { get; set; }

        extern public Texture initializationTexture { get; set; }

        [FreeFunction(Name = "CustomRenderTextureScripting::GetUpdateZonesInternal", HasExplicitThis = true)]
        extern internal void GetUpdateZonesInternal([NotNull, Out] List<CustomRenderTextureUpdateZone> updateZones);

        public void GetUpdateZones(List<CustomRenderTextureUpdateZone> updateZones)
        {
            GetUpdateZonesInternal(updateZones);
        }

        [FreeFunction(Name = "CustomRenderTextureScripting::SetUpdateZonesInternal", HasExplicitThis = true)]
        extern private void SetUpdateZonesInternal(CustomRenderTextureUpdateZone[] updateZones);

        [FreeFunction(Name = "CustomRenderTextureScripting::GetDoubleBufferRenderTexture", HasExplicitThis = true)]
        extern public RenderTexture GetDoubleBufferRenderTexture();

        extern public void EnsureDoubleBufferConsistency();

        public void SetUpdateZones(CustomRenderTextureUpdateZone[] updateZones)
        {
            if (updateZones == null)
                throw new ArgumentNullException("updateZones");

            SetUpdateZonesInternal(updateZones);
        }

        extern public CustomRenderTextureInitializationSource initializationSource { get; set; }
        extern public Color initializationColor { get; set; }
        extern public CustomRenderTextureUpdateMode updateMode { get; set; }
        extern public CustomRenderTextureUpdateMode initializationMode { get; set; }
        extern public CustomRenderTextureUpdateZoneSpace updateZoneSpace { get; set; }
        extern public int shaderPass { get; set; }
        extern public uint cubemapFaceMask { get; set; }
        extern public bool doubleBuffered { get; set; }
        extern public bool wrapUpdateZones { get; set; }
        extern public float updatePeriod { get; set; }
    }
}
