// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AssetBundle 系统 — Unity 运行时资源包加载与管理
//
// 功能概述：
//   AssetBundle 是 Unity 的资源包分发系统。开发者可以将资源（模型、纹理、
//   场景、音频等）打包为 AssetBundle 文件，在运行时下载并加载。
//
// 核心使用场景：
//   1. 热更新 — 通过下载新 AssetBundle 替换旧资源，无需重新发布主程序
//   2. 按需加载 — 降低初始包体大小，只在需要时下载特定关卡或角色资源
//   3. 多平台分发 — 不同平台使用不同的压缩和打包策略
//
// 核心 API 概览：
//   - LoadFromFile()      — 从本地文件加载（最快方式，推荐）
//   - LoadFromMemory()    — 从内存二进制数据加载
//   - LoadFromStream()    — 从可读流加载
//   - LoadAsset()         — 从包中加载单个资源（同步）
//   - LoadAssetAsync()    — 从包中加载单个资源（异步）
//   - LoadAllAssets()     — 加载包中所有资源
//   - LoadAssetWithSubAssets() — 加载资源及其子资源
//   - Unload()            — 卸载包（可选择是否卸载已加载的对象）
//
// 压缩方式选择：
//   - LZMA（默认）：包体最小，加载时需要完整解压，加载慢
//   - LZ4：包体略大，加载快，支持随机读取
//   - Uncompressed：包体最大，加载最快
//
// 相关技术演进：
//   AssetBundle → AssetBundle V2 → Addressables
//   Addressables 是基于 AssetBundle 的高级抽象，建议新项目使用 Addressables。
// ============================================================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
    /// <summary>
    /// AssetBundle 异步操作的结果状态枚举。
    /// 用于 AssetBundleRecompressOperation.result 等异步操作的结果反馈。
    /// </summary>
    public enum AssetBundleLoadResult
    {
        /// <summary>操作成功完成</summary>
        Success,
        /// <summary>操作被取消</summary>
        Cancelled,
        /// <summary>CRC 校验不匹配，数据可能已损坏</summary>
        NotMatchingCrc,
        /// <summary>缓存操作失败</summary>
        FailedCache,
        /// <summary>数据不是有效的 AssetBundle 格式</summary>
        NotValidAssetBundle,
        /// <summary>AssetBundle 中没有序列化数据</summary>
        NoSerializedData,
        /// <summary>AssetBundle 版本不兼容（Unity 版本不匹配）</summary>
        NotCompatible,
        /// <summary>AssetBundle 已被加载</summary>
        AlreadyLoaded,
        /// <summary>读取 AssetBundle 文件失败</summary>
        FailedRead,
        /// <summary>解压缩失败</summary>
        FailedDecompression,
        /// <summary>写入 AssetBundle 文件失败</summary>
        FailedWrite,
        /// <summary>删除重新压缩的目标文件失败</summary>
        FailedDeleteRecompressionTarget,
        /// <summary>重新压缩的目标 AssetBundle 正在被加载，无法操作</summary>
        RecompressionTargetIsLoaded,
        /// <summary>重新压缩的目标已存在但不是有效的归档文件</summary>
        RecompressionTargetExistsButNotArchive
    }

    /// <summary>
    /// AssetBundle — Unity 运行时资源包核心类。
    ///
    /// 对应 C++ 端 AssetBundle 对象，继承自 UnityEngine.Object。
    /// 通过此类的静态方法创建/加载 AssetBundle，再通过实例方法加载其中的资源。
    ///
    /// 生命周期：
    ///   1. 创建 — 使用 LoadFromFile / LoadFromMemory / LoadFromStream 等静态方法
    ///   2. 使用 — 使用 LoadAsset / LoadAllAssets / LoadAssetWithSubAssets 加载资源
    ///   3. 卸载 — 使用 Unload(bool unloadAllLoadedObjects)
    ///
    /// 注意：
    ///   - AssetBundle 不支持 new 实例化，必须通过静态工厂方法创建。
    ///   - 卸载时必须小心：Unload(false) 只卸载 AssetBundle 本身，已加载的 Object
    ///     仍有效；Unload(true) 会销毁所有从该包加载的对象。
    /// </summary>
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromFileAsyncOperation.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromMemoryAsyncOperation.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromManagedStreamAsyncOperation.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadAssetOperation.h")]
    [NativeHeader("Runtime/Scripting/ScriptingExportUtility.h")]
    [NativeHeader("Scripting/ScriptingUtility.h")]
    [NativeHeader("AssetBundleScriptingClasses.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleSaveAndLoadHelper.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleUtility.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadAssetUtility.h")]
    [ExcludeFromPreset]
    public partial class AssetBundle : Object
    {
        private AssetBundle() {}

        /// <summary>
        /// [已废弃] 获取 AssetBundle 的主资源。
        /// Unity 5.0 后不再支持，请使用新的构建系统。
        /// </summary>
        [Obsolete("mainAsset has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public Object mainAsset
        {
            get { return returnMainAsset(this); }
        }

        [FreeFunction("LoadMainObjectFromAssetBundle", true)]
        internal static extern Object returnMainAsset([NotNull] AssetBundle bundle);

        /// <summary>
        /// 卸载所有已加载的 AssetBundle。
        /// 当 unloadAllObjects = true 时，所有从此包加载的对象也会被销毁。
        ///
        /// 注意：这会影响整个应用程序中所有已加载的 AssetBundle。
        /// </summary>
        [FreeFunction("UnloadAllAssetBundles")]
        public extern static void UnloadAllAssetBundles(bool unloadAllObjects);

        /// <summary>获取所有已加载的 AssetBundle（内部实现，返回原生数组）</summary>
        [FreeFunction("GetAllAssetBundles")]
        internal extern static AssetBundle[] GetAllLoadedAssetBundles_Native();

        /// <summary>获取所有已加载的 AssetBundle 枚举器，用于遍历检查。</summary>
        public static IEnumerable<AssetBundle> GetAllLoadedAssetBundles()
        {
            return GetAllLoadedAssetBundles_Native();
        }

        // ============================================================
        // LoadFromFile 系列 — 从本地文件加载 AssetBundle
        //
        // 这是推荐的方式，因为 Unity 会使用操作系统级的文件缓存。
        // 仅读取包索引头，实际资源在请求时才从文件读取。
        // 支持 LZ4 压缩格式的随机读取，无需完整解压。
        //
        // 参数：
        //   path   — AssetBundle 文件路径
        //   crc    — 可选的 CRC 校验（非 0 时，加载会验证 CRC，不匹配则报错）
        //   offset — 文件偏移量。如果 AssetBundle 数据嵌在另一个文件中，可用此参数跳过头部
        // ============================================================

        [FreeFunction("LoadFromFileAsync")]
        internal extern static AssetBundleCreateRequest LoadFromFileAsync_Internal(string path, uint crc, ulong offset);

        /// <summary>从本地文件异步加载 AssetBundle。</summary>
        public static AssetBundleCreateRequest LoadFromFileAsync(string path)
        {
            return LoadFromFileAsync_Internal(path, 0, 0);
        }

        /// <summary>从本地文件异步加载 AssetBundle，带 CRC 校验。</summary>
        public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc)
        {
            return LoadFromFileAsync_Internal(path, crc, 0);
        }

        /// <summary>从本地文件异步加载 AssetBundle，带 CRC 校验和偏移量。</summary>
        public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc, ulong offset)
        {
            return LoadFromFileAsync_Internal(path, crc, offset);
        }

        [FreeFunction("LoadFromFile")]
        internal extern static AssetBundle LoadFromFile_Internal(string path, uint crc, ulong offset);

        /// <summary>
        /// 从本地文件同步加载 AssetBundle。
        /// 这是最高效的加载方式，推荐使用。
        /// </summary>
        public static AssetBundle LoadFromFile(string path)
        {
            return LoadFromFile_Internal(path, 0, 0);
        }

        /// <summary>从本地文件同步加载 AssetBundle，带 CRC 校验。</summary>
        public static AssetBundle LoadFromFile(string path, uint crc)
        {
            return LoadFromFile_Internal(path, crc, 0);
        }

        /// <summary>从本地文件同步加载 AssetBundle，带 CRC 校验和偏移量。</summary>
        public static AssetBundle LoadFromFile(string path, uint crc, ulong offset)
        {
            return LoadFromFile_Internal(path, crc, offset);
        }

        // ============================================================
        // LoadFromMemory 系列 — 从内存字节数组加载 AssetBundle
        //
        // 适用于从网络下载数据后直接加载的场景。
        // 注意：此方法会创建数据的副本，且需要完整解压 LZMA 数据。
        // 如果数据已在本地文件，请使用 LoadFromFile（性能更好）。
        //
        // 性能建议：优先使用 LoadFromFile，除非数据来自内存缓存或网络。
        // ============================================================

        [FreeFunction("LoadFromMemoryAsync")]
        internal extern static AssetBundleCreateRequest LoadFromMemoryAsync_Internal(byte[] binary, uint crc);

        /// <summary>从内存字节数组异步加载 AssetBundle（例如从网络下载后直接加载）。</summary>
        public static AssetBundleCreateRequest LoadFromMemoryAsync(byte[] binary)
        {
            return LoadFromMemoryAsync_Internal(binary, 0);
        }

        /// <summary>从内存字节数组异步加载，带 CRC 校验。</summary>
        public static AssetBundleCreateRequest LoadFromMemoryAsync(byte[] binary, uint crc)
        {
            return LoadFromMemoryAsync_Internal(binary, crc);
        }

        [FreeFunction("LoadFromMemory")]
        internal extern static AssetBundle LoadFromMemory_Internal(byte[] binary, uint crc);

        /// <summary>从内存字节数组同步加载 AssetBundle。</summary>
        public static AssetBundle LoadFromMemory(byte[] binary)
        {
            return LoadFromMemory_Internal(binary, 0);
        }

        /// <summary>从内存字节数组同步加载 AssetBundle，带 CRC 校验。</summary>
        public static AssetBundle LoadFromMemory(byte[] binary, uint crc)
        {
            return LoadFromMemory_Internal(binary, crc);
        }

        // ============================================================
        // LoadFromStream 系列 — 从托管流（Stream）加载 AssetBundle
        //
        // 适用于需要自定义数据源的情况，如：
        //   - 加密的 AssetBundle（解密后通过 MemoryStream 传入）
        //   - 从自定义文件格式中提取的 AssetBundle 段
        //   - 网络流（需确保流可读可查找）
        //
        // 要求：
        //   stream.CanRead == true（必须可读）
        //   stream.CanSeek == true（必须可查找，流需要支持随机访问）
        // ============================================================

        internal static void ValidateLoadFromStream(System.IO.Stream stream)
        {
            if (stream == null)
                throw new System.ArgumentNullException("ManagedStream object must be non-null", "stream");
            if (!stream.CanRead)
                throw new System.ArgumentException("ManagedStream object must be readable (stream.CanRead must return true)", "stream");
            if (!stream.CanSeek)
                throw new System.ArgumentException("ManagedStream object must be seekable (stream.CanSeek must return true)", "stream");
        }

        /// <summary>从 Stream 异步加载 AssetBundle，可自定义读取缓冲区大小。</summary>
        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream, uint crc, uint managedReadBufferSize)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, crc, managedReadBufferSize);
        }

        /// <summary>从 Stream 异步加载 AssetBundle。</summary>
        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream, uint crc)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, crc, 0);
        }

        /// <summary>从 Stream 异步加载 AssetBundle（无 CRC 校验）。</summary>
        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, 0, 0);
        }

        /// <summary>从 Stream 同步加载 AssetBundle，可自定义读取缓冲区大小。</summary>
        public static AssetBundle LoadFromStream(System.IO.Stream stream, uint crc, uint managedReadBufferSize)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, crc, managedReadBufferSize);
        }

        /// <summary>从 Stream 同步加载 AssetBundle。</summary>
        public static AssetBundle LoadFromStream(System.IO.Stream stream, uint crc)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, crc, 0);
        }

        /// <summary>从 Stream 同步加载 AssetBundle（无 CRC 校验）。</summary>
        public static AssetBundle LoadFromStream(System.IO.Stream stream)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, 0, 0);
        }

        [FreeFunction("LoadFromStreamAsyncInternal")]
        internal extern static AssetBundleCreateRequest LoadFromStreamAsyncInternal(System.IO.Stream stream, uint crc,
            uint managedReadBufferSize);

        [FreeFunction("LoadFromStreamInternal")]
        internal extern static AssetBundle LoadFromStreamInternal(System.IO.Stream stream, uint crc,
            uint managedReadBufferSize);

        /// <summary>
        /// 判断此 AssetBundle 是否包含流式场景（Streamed Scene）。
        /// 如果为 true，表示此包包含通过 BuildPipeline.BuildPlayer 的 Scene Bundle 选项构建的场景。
        /// 流式场景包中的场景是通过场景流式加载系统加载的。
        /// </summary>
        public extern bool isStreamedSceneAssetBundle
        {
            [NativeMethod("GetIsStreamedSceneAssetBundle")]
            get;
        }

        /// <summary>检查 AssetBundle 中是否包含指定名称的资源。</summary>
        [NativeMethod("Contains")]
        public extern bool Contains(string name);

        // ============================================================
        // 已弃用的旧 API（Load / LoadAsync / LoadAll）
        //
        // 这些方法在 Unity 5.0 引入新加载系统后废弃。
        // 新代码应使用 LoadAsset / LoadAssetAsync / LoadAllAssets。
        // ============================================================

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
        public Object Load(string name) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
        public Object Load<T>(string name) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
        Object Load(string name, Type type) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAsync has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAssetAsync instead and check the documentation for details.", true)]
        AssetBundleRequest LoadAsync(string name, Type type) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
        Object[] LoadAll(Type type) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
        public UnityEngine.Object[] LoadAll() { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
        public T[] LoadAll<T>() where T : Object { return null; }

        // ============================================================
        // LoadAsset 系列 — 从 AssetBundle 加载单个资源（同步）
        //
        // name 参数是在打包时赋给资源的名称（通常与原始文件名相同，不含扩展名）。
        // 如果 AssetBundle 使用 LZ4 压缩，只解压请求的资源块。
        // ============================================================

        /// <summary>从 AssetBundle 同步加载指定名称的资源。</summary>
        public Object LoadAsset(string name)
        {
            return LoadAsset(name, typeof(Object));
        }

        /// <summary>泛型版本的 LoadAsset。</summary>
        public T LoadAsset<T>(string name) where T : Object
        {
            return (T)LoadAsset(name, typeof(T));
        }

        /// <summary>指定类型的 LoadAsset 重载。</summary>
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        public Object LoadAsset(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAsset_Internal(name, type);
        }

        /// <summary>
        /// LoadAsset 的内部原生实现。
        /// 当资源不存在时，ThrowsException = true 会抛出异常。
        /// </summary>
        [NativeMethod("LoadAsset_Internal", ThrowsException = true)]
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        private extern Object LoadAsset_Internal(string name, Type type);

        // ============================================================
        // LoadAssetAsync 系列 — 异步加载单个资源
        //
        // 返回 AssetBundleRequest（继承自 ResourceRequest），
        // 可通过协程 yield return 或 completed 回调获得结果。
        // 推荐在主线程需要保持响应性时使用。
        // ============================================================

        /// <summary>异步加载 AssetBundle 中的指定资源。</summary>
        public AssetBundleRequest LoadAssetAsync(string name)
        {
            return LoadAssetAsync(name, typeof(UnityEngine.Object));
        }

        /// <summary>泛型版本的 LoadAssetAsync。</summary>
        public AssetBundleRequest LoadAssetAsync<T>(string name)
        {
            return LoadAssetAsync(name, typeof(T));
        }

        /// <summary>指定类型的 LoadAssetAsync 重载。</summary>
        public AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetAsync_Internal(name, type);
        }

        // ============================================================
        // LoadAssetWithSubAssets 系列 — 加载资源及其子资源
        //
        // 适用于包含多个子资源的资源文件，如：
        //   - 包含多个 Sprite 的 Texture （Sprite sheet）
        //   - 包含多个动画的 FBX 文件
        //   - 包含多个 Font 的 TTF 文件
        //
        // LoadAllAssets 实际上是 LoadAssetWithSubAssets("", type) 的别名。
        // ============================================================

        /// <summary>从 AssetBundle 加载指定资源及其所有子资源。</summary>
        public Object[] LoadAssetWithSubAssets(string name)
        {
            return LoadAssetWithSubAssets(name, typeof(Object));
        }

        internal static T[] ConvertObjects<T>(Object[] rawObjects) where T : Object
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        /// <summary>泛型版本的 LoadAssetWithSubAssets。</summary>
        public T[] LoadAssetWithSubAssets<T>(string name) where T : Object
        {
            return ConvertObjects<T>(LoadAssetWithSubAssets(name, typeof(T)));
        }

        /// <summary>指定类型的 LoadAssetWithSubAssets 重载。</summary>
        public Object[] LoadAssetWithSubAssets(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssets_Internal(name, type);
        }

        /// <summary>异步加载资源及其子资源。</summary>
        public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name)
        {
            return LoadAssetWithSubAssetsAsync(name, typeof(UnityEngine.Object));
        }

        /// <summary>泛型版本的 LoadAssetWithSubAssetsAsync。</summary>
        public AssetBundleRequest LoadAssetWithSubAssetsAsync<T>(string name)
        {
            return LoadAssetWithSubAssetsAsync(name, typeof(T));
        }

        /// <summary>指定类型的 LoadAssetWithSubAssetsAsync 重载。</summary>
        public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssetsAsync_Internal(name, type);
        }

        // ============================================================
        // LoadAllAssets 系列 — 加载 AssetBundle 中所有资源
        //
        // 内部实现是调用 LoadAssetWithSubAssets_Internal("", type)，
        // 即传入空字符串名称来请求包中所有资源。
        // ============================================================

        /// <summary>加载 AssetBundle 中所有类型为 Object 的资源。</summary>
        public UnityEngine.Object[] LoadAllAssets()
        {
            return LoadAllAssets(typeof(UnityEngine.Object));
        }

        /// <summary>泛型版本的 LoadAllAssets。</summary>
        public T[] LoadAllAssets<T>() where T : Object
        {
            return ConvertObjects<T>(LoadAllAssets(typeof(T)));
        }

        /// <summary>指定类型的 LoadAllAssets 重载。</summary>
        public UnityEngine.Object[] LoadAllAssets(Type type)
        {
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssets_Internal("", type);
        }

        /// <summary>异步加载 AssetBundle 中所有资源。</summary>
        public AssetBundleRequest LoadAllAssetsAsync()
        {
            return LoadAllAssetsAsync(typeof(UnityEngine.Object));
        }

        /// <summary>泛型版本的 LoadAllAssetsAsync。</summary>
        public AssetBundleRequest LoadAllAssetsAsync<T>()
        {
            return LoadAllAssetsAsync(typeof(T));
        }

        /// <summary>指定类型的 LoadAllAssetsAsync 重载。</summary>
        public AssetBundleRequest LoadAllAssetsAsync(Type type)
        {
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssetsAsync_Internal("", type);
        }

        [Obsolete("This method is deprecated.Use GetAllAssetNames() instead.", false)]
        public string[] AllAssetNames()
        {
            return GetAllAssetNames();
        }

        /// <summary>LoadAssetAsync 内部原生实现。</summary>
        [NativeMethod("LoadAssetAsync_Internal", ThrowsException = true)]
        private extern AssetBundleRequest LoadAssetAsync_Internal(string name, Type type);

        /// <summary>
        /// 卸载此 AssetBundle。
        ///
        /// 参数 unloadAllLoadedObjects：
        ///   false — 仅卸载 AssetBundle 本身，从此包加载的对象仍然有效。
        ///           适用于需要保留对象但释放 AssetBundle 内存的场景。
        ///   true  — 卸载 AssetBundle 并销毁所有从此包加载的对象。
        ///           适用于完全卸载某个资源包的场景。
        ///
        /// 最佳实践：
        ///   使用 Unload(false) 保留已加载的对象引用。
        ///   务必在不再需要时及时卸载 AssetBundle，避免内存泄漏。
        /// </summary>
        [NativeMethod("Unload", ThrowsException = true)]
        public extern void Unload(bool unloadAllLoadedObjects);

        /// <summary>
        /// 异步卸载此 AssetBundle。
        /// 返回 AssetBundleUnloadOperation，可追踪卸载进度。
        /// 通过 WaitForCompletion() 方法可阻塞等待卸载完成。
        /// </summary>
        [NativeMethod("UnloadAsync", ThrowsException = true)]
        public extern AssetBundleUnloadOperation UnloadAsync(bool unloadAllLoadedObjects);

        /// <summary>获取 AssetBundle 中所有资源名称。</summary>
        [NativeMethod("GetAllAssetNames")]
        public extern string[] GetAllAssetNames();

        /// <summary>获取 AssetBundle 中所有场景路径。</summary>
        [NativeMethod("GetAllScenePaths")]
        public extern string[] GetAllScenePaths();

        /// <summary>加载资源及其子资源的内部原生实现。</summary>
        [NativeMethod("LoadAssetWithSubAssets_Internal", ThrowsException = true)]
        internal extern Object[] LoadAssetWithSubAssets_Internal(string name, Type type);

        /// <summary>异步加载资源及其子资源的内部原生实现。</summary>
        [NativeMethod("LoadAssetWithSubAssetsAsync_Internal", ThrowsException = true)]
        private extern AssetBundleRequest LoadAssetWithSubAssetsAsync_Internal(string name, Type type);

        /// <summary>
        /// 异步重新压缩 AssetBundle。
        /// 将 AssetBundle 从一种压缩方式转换到另一种（如 LZMA → LZ4），
        /// 适用于下载后重新压缩以优化运行时加载性能。
        /// </summary>
        /// <param name="inputPath">输入 AssetBundle 路径</param>
        /// <param name="outputPath">输出 AssetBundle 路径</param>
        /// <param name="method">目标压缩方式（BuildCompression 枚举）</param>
        /// <param name="expectedCRC">期望的 CRC 校验值（可选）</param>
        /// <param name="priority">线程优先级</param>
        public static AssetBundleRecompressOperation RecompressAssetBundleAsync(string inputPath, string outputPath, BuildCompression method, UInt32 expectedCRC = 0, ThreadPriority priority = ThreadPriority.Low)
        {
            return RecompressAssetBundleAsync_Internal(inputPath, outputPath, method, expectedCRC, priority);
        }

        [FreeFunction("RecompressAssetBundleAsync_Internal", ThrowsException = true)]
        internal static extern AssetBundleRecompressOperation RecompressAssetBundleAsync_Internal(string inputPath, string outputPath, BuildCompression method, UInt32 expectedCRC, ThreadPriority priority);

        /// <summary>
        /// AssetBundle 异步加载缓存的内存预算（KB）。
        /// 控制加载过程中的内存缓冲区大小。
        /// 增大此值可以提高并行加载的吞吐量，但会占用更多内存。
        /// </summary>
        public static uint memoryBudgetKB
        {
            get { return AssetBundleLoadingCache.memoryBudgetKB; }
            set { AssetBundleLoadingCache.memoryBudgetKB = value; }
        }
    }
}
