# Day 11：资源管理 — Addressables 深度实践与生产管线

## 0. 为什么还需要深入资源管理？

Day11 的基础篇介绍了四种加载方式。但在真实项目中，资源管理远比那复杂。大型游戏动辄几千个资源文件，几十 GB 的包体，跨平台分发。这就需要 Addressables 作为核心方案登场了。

```
基础篇解决的是"怎么加载"
深入篇解决的是"怎么在项目中落地"：
- 资源怎么分组才合理？
- 远程资源怎么分发（CDN）？
- 加载性能怎么分析和优化？
- 多平台怎么管理？
- 内存预算怎么控制？
```

对于 C++ 背景的你——Addressables 可以理解为**资源加载的智能指针**：引用计数自动管理生命周期，依赖链自动追踪。

---

## 1. Addressables 分组策略

### 1.1 Group 设计原则

```csharp
// Addressables Groups 是资源组织的核心单元
// 每个 Group 打包为一个或多个 AssetBundle

// ─── 按功能分组的推荐模式 ───

// 组名命名规范：
// [平台][功能][角色]
// 例如：
//   Windows_Characters_Player     → 玩家角色资源
//   Mobile_UI_MainMenu            → 移动端主菜单UI
//   Shared_Environment_City       → 共享的城市环境资源

// ─── 分组策略示例 ───
/*
 基础策略 - 按更新频率分：
 ┌─────────────────────────────────────────┐
 │ Group: Core_Engine                      │
 │ 内容：核心引擎资源（几乎不改）             │
 │ 打包：包体内置，不进热更新                 │
 │ 例如：Shader、基础材质、核心 UI           │
 ├─────────────────────────────────────────┤
 │ Group: Content_Stage1                   │
 │ 内容：第一章游戏内容（每版本更新）         │
 │ 打包：远程 CDN，按需下载                  │
 ├─────────────────────────────────────────┤
 │ Group: Content_Stage2                   │
 │ 内容：第二章（后续版本新增）               │
 │ 打包：远程 CDN，付费解锁后下载            │
 ├─────────────────────────────────────────┤
 │ Group: Dynamic_LootBoxSkin              │
 │ 内容：活动资源（高频率更新）               │
 │ 打包：远程 CDN，小包粒度                  │
 └─────────────────────────────────────────┘
*/
```

### 1.2 Schema 配置详解

```csharp
// 每个 Group 可以关联多个 Schema（方案配置）
// Addressables 内置了三个 Schema：

// 1. Content Packing & Loading Schema
//    控制如何打包和加载

/*
 关键属性：
 ┌─────────────────────────────────────────────────────┐
 │ Bundle Mode:                                        │
 │   PackTogether          → 所有资源打一个 Bundle      │
 │   PackSeparately        → 每个资源单独打 Bundle      │
 │   PackTogetherByLabel   → 按标签分组打包             │
 │                                                     │
 │ Bundle Naming:                                      │
 │   AppendHash            → mybundle_abc123.bundle    │
 │   用途：CDN 缓存控制，内容变化后 URL 自动变           │
 │                                                     │
 │ Compress & CRC:                                     │
 │   Compress: LZ4 / LZMA                              │
 │     LZ4：加载快，但包体较大                           │
 │     LZMA：包体最小，但加载慢（需全解压）               │
 ├─────────────────────────────────────────────────────┤
 │ Build & Load Paths:                                 │
 │   [UnityEngine.AddressableAssets.Addressables]      │
 │   BuildPath:  打包输出目录                            │
 │   LoadPath:   运行时加载地址（本地或远程 URL）         │
 │                                                     │
 │   远程加载配置示例：                                   │
 │   LoadPath: https://cdn.mygame.com/                  │
 │             {UnityEngine.AddressableAssets.          │
 │              Addressables.RuntimePathPrefix}         │
 └─────────────────────────────────────────────────────┘
*/

// 2. Content Update Schema（内容更新配置）
//    控制热更新行为

/*
 ┌─────────────────────────────────────────┐
 │ Static Content:                         │
 │   true  → 不参与热更新（包体内置）        │
 │   false → 可通过远程更新                  │
 │                                         │
 │ Local / Remote Catalog:                 │
 │   Catalog 是资源索引表                    │
 │   远程 Catalog 的 URL 指向 CDN            │
 └─────────────────────────────────────────┘
*/
```

### 1.3 按需加载设计

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class LevelLoader : MonoBehaviour
{
    [Header("Level Config")]
    public AssetReference sceneReference;   // 场景引用
    
    // ─── 依赖预下载 ───
    // 在关卡选择界面，预下载下一关的资源
    
    public IEnumerator PreloadNextLevel(string levelKey)
    {
        // 1. 获取资源依赖列表（不加载，只查）
        AsyncOperationHandle<IList<IResourceLocation>> depHandle =
            Addressables.GetDependencies(levelKey);
        yield return depHandle;
        
        long totalSize = 0;
        
        // 2. 计算总大小
        foreach (var loc in depHandle.Result)
        {
            AsyncOperationHandle<long> sizeHandle =
                Addressables.GetDownloadSizeAsync(loc);
            yield return sizeHandle;
            totalSize += sizeHandle.Result;
        }
        
        Addressables.Release(depHandle);
        
        if (totalSize > 0)
        {
            Debug.Log($"需要下载 {totalSize / 1048576} MB");
            
            // 3. 预下载
            AsyncOperationHandle downloadHandle =
                Addressables.DownloadDependenciesAsync(levelKey);
            
            while (!downloadHandle.IsDone)
            {
                float progress = downloadHandle.PercentComplete;
                // 显示进度条
                yield return null;
            }
            
            Addressables.Release(downloadHandle);
        }
    }
    
    // ─── 快速切换场景（内存中保持） ───
    // 两个场景共享的资源不卸载，实现秒切
    
    private AsyncOperationHandle sceneHandle;
    
    public IEnumerator SwitchScene(string sceneKey)
    {
        // 释放旧场景但不卸载共享资源
        if (sceneHandle.IsValid())
        {
            Addressables.ReleaseInstance(sceneHandle);
        }
        
        // 加载新场景
        sceneHandle = Addressables.LoadSceneAsync(sceneKey);
        yield return sceneHandle;
    }
}
```

---

## 2. AssetBundle 构建管线定制

### 2.1 自定义 Build Script

```csharp
// Unity 允许通过 IBuildScript 接口定制打包行为
// 场景：自动给所有 Bundle 名加版本号前缀

using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.Collections.Generic;

public class CustomBuildScript : IDataBuilder
{
    public string Name => "Custom Addressables Build";
    
    // ─── 构建前的预处理 ───
    // 在打包前自动调整设置
    
    public void ClearCachedData()
    {
        // 清理缓存
        Addressables.CleanBundleCache();
    }
    
    // ─── 批量设置 Group 属性 ───
    [MenuItem("Tools/Addressables/Setup Production Groups")]
    static void SetupProductionGroups()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        
        foreach (var group in settings.groups)
        {
            if (group == null) continue;
            
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null) continue;
            
            // 根据组名自动配置
            if (group.Name.StartsWith("Core_"))
            {
                // 核心资源：LZ4 压缩，本地加载
                schema.CompressionType =
                    BundledAssetGroupSchema.BundleCompressionType.LZ4;
                schema.BuildPath.SetVariable(
                    settings, "LocalBuildPath");
                schema.LoadPath.SetVariable(
                    settings, "LocalLoadPath");
            }
            else if (group.Name.StartsWith("Content_"))
            {
                // 内容资源：LZMA 压缩，远程加载
                schema.CompressionType =
                    BundledAssetGroupSchema.BundleCompressionType.LZMA;
                schema.BuildPath.SetVariable(
                    settings, "RemoteBuildPath");
                schema.LoadPath.SetVariable(
                    settings, "RemoteLoadPath");
            }
        }
        
        Debug.Log("Groups configured for production!");
    }
}
```

### 2.2 多平台打包管线

```csharp
// ─── 完整的 CI/CD 打包脚本 ───

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;

public class BuildPipelineCI
{
    // 构建参数
    private static string[] targetPlatforms = {
        "StandaloneWindows64",
        "Android",
        "iOS"
    };
    
    [MenuItem("Build/Publish All Platforms")]
    static void BuildAllPlatforms()
    {
        foreach (string platform in targetPlatforms)
        {
            SwitchPlatform(platform);
            BuildAddressablesForPlatform(platform);
        }
    }
    
    static void SwitchPlatform(string platform)
    {
        // 切换构建目标
        BuildTarget target = BuildTarget.StandaloneWindows64;
        BuildTargetGroup group = BuildTargetGroup.Standalone;
        
        switch (platform)
        {
            case "Android":
                target = BuildTarget.Android;
                group = BuildTargetGroup.Android;
                break;
            case "iOS":
                target = BuildTarget.iOS;
                group = BuildTargetGroup.iOS;
                break;
        }
        
        EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
    }
    
    static void BuildAddressablesForPlatform(string platform)
    {
        // 执行 Addressables 构建
        AddressableAssetSettings.BuildPlayerContent();
        
        // 输出目录处理
        string outputPath = $"Build/Addressables/{platform}";
        string builtPath = Addressables.BuildPath;
        
        // 复制到输出目录
        FileUtil.CopyFileOrDirectory(builtPath, outputPath);
        
        Debug.Log($"Addressables built for {platform} → {outputPath}");
    }
}
```

---

## 3. CDN 集成方案

### 3.1 CDN 部署架构

```csharp
/*
  ┌──────────────────────────────────────────────────┐
  │               CDN 架构                            │
  │                                                   │
  │  构建机器 → 上传工具 → CDN 源站                     │
  │                          │                        │
  │                ┌─────────┼─────────┐              │
  │                ▼         ▼         ▼              │
  │            Edge节点1  Edge节点2  Edge节点3          │
  │                │         │         │              │
  │                └─────────┼─────────┘              │
  │                          │                        │
  │                    玩家设备                         │
  │             自动选择最近的 Edge 节点                 │
  └──────────────────────────────────────────────────┘

  CDN 上传内容结构：
  /
  ├── catalog_20240101.json        → 资源目录索引
  ├── StandaloneWindows64/
  │   ├── core_assets_abc123.bundle
  │   ├── content_stage1_def456.bundle
  │   └── ...
  ├── Android/
  │   ├── core_assets_789abc.bundle
  │   └── ...
  └── version.txt                   → 版本号文件
*/
```

### 3.2 版本检查与增量更新

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class CDNVersionChecker : MonoBehaviour
{
    [Header("CDN Config")]
    public string cdnBaseURL = "https://cdn.mygame.com/";
    public string versionURL = "version.txt";
    public string localVersion;
    
    private void Start()
    {
        StartCoroutine(CheckForUpdates());
    }
    
    IEnumerator CheckForUpdates()
    {
        // 1. 读取本地版本号
        localVersion = PlayerPrefs.GetString("LocalVersion", "1.0.0");
        
        // 2. 请求远端版本号
        string url = cdnBaseURL + versionURL;
        UnityWebRequest req = UnityWebRequest.Get(url);
        req.timeout = 5;  // 5秒超时
        
        yield return req.SendWebRequest();
        
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("版本检查失败，使用本地资源");
            yield break;
        }
        
        string remoteVersion = req.downloadHandler.text.Trim();
        
        // 3. 比较版本号
        if (remoteVersion != localVersion)
        {
            Debug.Log($"发现新版本：{localVersion} → {remoteVersion}");
            StartCoroutine(DownloadUpdate());
            
            // 记录新版本
            PlayerPrefs.SetString("LocalVersion", remoteVersion);
        }
        else
        {
            Debug.Log("已是最新版本！");
        }
    }
    
    IEnumerator DownloadUpdate()
    {
        // 使用 Addressables 的 Catalog 更新
        // 会自动对比本地缓存和远程 Catalog 的差异
        
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;
        
        if (checkHandle.Result.Count > 0)
        {
            var updateHandle = Addressables.UpdateCatalogs(
                checkHandle.Result, false);
            yield return updateHandle;
            
            Addressables.Release(updateHandle);
        }
        
        Addressables.Release(checkHandle);
    }
}
```

### 3.3 断点续传与容错

```csharp
// ─── 可断点续传的下载管理器 ───

public class ResumableDownloader : MonoBehaviour
{
    private Dictionary<string, DownloadState> downloadStates =
        new Dictionary<string, DownloadState>();
    
    class DownloadState
    {
        public string url;
        public string savePath;
        public long downloadedBytes;
        public long totalBytes;
        public bool isComplete;
    }
    
    IEnumerator DownloadWithResume(string url, string savePath)
    {
        DownloadState state = new DownloadState
        {
            url = url,
            savePath = savePath,
            downloadedBytes = GetExistingFileSize(savePath)
        };
        
        downloadStates[url] = state;
        
        using (UnityWebRequest req = new UnityWebRequest(url))
        {
            req.method = "GET";
            
            // 设置断点续传范围
            if (state.downloadedBytes > 0)
            {
                req.SetRequestHeader("Range",
                    $"bytes={state.downloadedBytes}-");
            }
            
            req.downloadHandler = new DownloadHandlerFile(savePath, true);
            // true = append（追加模式，支持续传）
            
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                state.isComplete = true;
                Debug.Log($"下载完成：{savePath}");
            }
            else if (req.responseCode == 416)
            {
                // 416 = Range Not Satisfiable（文件已完整）
                state.isComplete = true;
            }
            else
            {
                Debug.LogError($"下载失败：{req.error}");
                // 下次启动时重试（下载状态保留）
            }
        }
    }
    
    long GetExistingFileSize(string path)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path).Length;
        }
        return 0;
    }
}
```

---

## 4. Addressables 性能诊断

### 4.1 事件系统监控

```csharp
// Addressables 提供事件诊断接口
// 可以监控每个加载操作的细节

using UnityEngine.ResourceManagement.Diagnostics;

public class AddressablesMonitor : MonoBehaviour
{
    private void OnEnable()
    {
        // 订阅 Addressables 事件
        AddressablesDiagnostics.RegisterEventHandler(
            DiagnosticEventType.AssemblyReload,
            OnAddressablesEvent
        );
        
        AddressablesDiagnostics.RegisterEventHandler(
            DiagnosticEventType.AsyncOperation,
            OnAddressablesEvent
        );
    }
    
    private void OnAddressablesEvent(DiagnosticEvent evt)
    {
        // 每个事件包含：
        // - evt.Stream: 哪个系统
        // - evt.Description: 事件描述
        // - evt.Frame: 哪一帧发生
        
        if (evt.Stream == "ResourceManager")
        {
            // 资源加载事件
            Debug.Log($"[Addressables Event] {evt.Description}");
            
            // 检测过长的加载操作（超过 1 秒）
            if (evt.Description.Contains("Load") &&
                Time.realtimeSinceStartup > 1f)
            {
                Debug.LogWarning($"慢加载操作：{evt.Description}");
            }
        }
    }
    
    private void OnDisable()
    {
        AddressablesDiagnostics.UnregisterEventHandler(OnAddressablesEvent);
    }
}
```

### 4.2 Profiler 中的 Addressables

```csharp
/*
 使用 Addressables Profiler 模块：

 Window → Analysis → Addressables Profiler

 查看内容：
 ┌──────────────────────────────────────────────────┐
 │ Addressables Profiler                             │
 │                                                   │
 │ 1. 引用计数图（Reference Count Graph）              │
 │   每个资源的引用数变化曲线                           │
 │   骤降 = 卸载，异常不降 = 泄露                      │
 │                                                   │
 │ 2. 加载操作列表（Active Operations）                │
 │   当前正在进行的所有加载操作                          │
 │   可以看到每个操作的：状态、时长、依赖                 │
 │                                                   │
 │ 3. 内存分布（Memory Breakdown）                     │
 │   各个 Group 占用的内存                             │
 │   找出哪个组占用了最多的内存                         │
 └──────────────────────────────────────────────────┘
*/
```

### 4.3 常见性能问题排查

```csharp
// ─── 问题 1：资源重复加载 ───
// 同一个资源被加载了多次 → 浪费内存

public class DuplicateChecker : MonoBehaviour
{
    void CheckDuplicates()
    {
        // 在 Addressables Profiler 中检查：
        // 查找同名资源的不同实例
    }
}

// ─── 问题 2：引用泄露 ───
// Load 了但没有 Release

/*
 典型表现：
 1. 引用计数持续增长，不为 0
 2. 场景切换后内存不释放
 3. 反复进出同一个场景后内存膨胀

 解决的黄金法则：
 每个 Load 操作必须对应一个 Release！
 用 try/finally 保证释放：
*/

void SafeLoad()
{
    AsyncOperationHandle<GameObject> handle =
        Addressables.LoadAssetAsync<GameObject>("Enemy");
    
    // 包装在 try-finally 中（或 using）
    try
    {
        // 使用资源
        Instantiate(handle.Result);
    }
    finally
    {
        Addressables.Release(handle);
    }
}

// ─── 问题 3：Bundle 粒度过大 ───
// 一个 Bundle 太大 → 加载耗时、浪费带宽

/*
 理想 Bundle 大小：
 移动端：1~2 MB 每个
 PC：2~5 MB 每个

 检查方法：
 查看 Build Report 中每个 Bundle 的大小
 如果某个 Bundle > 10MB，考虑拆分
*/
```

---

## 5. 自定义加载策略

### 5.1 自定义 AssetProvider

```csharp
// Addressables 允许自定义资源提供者
// 场景：实现资源解密、自定义压缩等

using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.AsyncOperations;

public class EncryptedAssetProvider : ResourceProviderBase
{
    public override string ProviderId => "EncryptedAssetProvider";
    
    public override void Provide(ProvideHandle provideHandle)
    {
        // 1. 获取资源位置和类型
        IResourceLocation location = provideHandle.Location;
        Type type = provideHandle.Type;
        
        // 2. 加载加密资源
        byte[] encryptedData = File.ReadAllBytes(
            Application.persistentDataPath + "/" + location.InternalId);
        
        // 3. 解密
        byte[] decryptedData = DecryptData(encryptedData);
        
        // 4. 转为 Unity 可识别的格式
        AssetBundle bundle = AssetBundle.LoadFromMemory(decryptedData);
        
        // 5. 完成加载
        object asset = bundle.LoadAsset(location.PrimaryKey, type);
        provideHandle.Complete(asset, true, null);
    }
    
    byte[] DecryptData(byte[] data)
    {
        // 简单的 XOR 解密示例
        byte key = 0xAB;
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= key;
        }
        return data;
    }
}
```

### 5.2 多级缓存策略

```csharp
// 资源加载的缓存层级

public class MultiLevelCache
{
    // 级别 0：内存缓存（最快）
    private Dictionary<string, object> memoryCache =
        new Dictionary<string, object>();
    
    // 级别 1：本地文件缓存
    private string cacheDir =
        Application.persistentDataPath + "/AssetCache/";
    
    // 级别 2：远程加载（最慢）
    private string remoteURL = "https://cdn.mygame.com/";
    
    public IEnumerator LoadWithCache<T>(string key) where T : UnityEngine.Object
    {
        // 1. 检查内存缓存
        if (memoryCache.TryGetValue(key, out object cached))
        {
            yield return cached as T;
            yield break;
        }
        
        // 2. 检查本地缓存
        string localPath = cacheDir + key;
        if (File.Exists(localPath))
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(localPath);
            T asset = bundle.LoadAsset<T>(key);
            memoryCache[key] = asset;
            bundle.Unload(false);
            yield return asset;
            yield break;
        }
        
        // 3. 远程加载
        UnityWebRequest req = UnityWebRequest.Get(remoteURL + key);
        yield return req.SendWebRequest();
        
        if (req.result == UnityWebRequest.Result.Success)
        {
            // 写入本地缓存
            File.WriteAllBytes(localPath, req.downloadHandler.data);
            
            // 加载到内存
            AssetBundle bundle = AssetBundle.LoadFromMemory(req.downloadHandler.data);
            T asset = bundle.LoadAsset<T>(key);
            memoryCache[key] = asset;
            bundle.Unload(false);
            
            yield return asset;
        }
    }
}
```

---

## 6. 大型项目的内存预算

```csharp
/*
 ─── 移动端游戏内存预算示例（2GB RAM 设备） ───

 总可用内存（2GB）
 ├── OS 和其他 App：         ~500MB
 ├── Unity 引擎：            ~200MB
 │   ├── Mono/IL2CPP 运行时    ~50MB
 │   ├── 渲染系统               ~80MB
 │   ├── 物理系统               ~30MB
 │   └── 其他                   ~40MB
 ├── 游戏内容：               ~800MB
 │   ├── 纹理（最大部分）        ~400MB
 │   ├── 模型 + 动画            ~150MB
 │   ├── 音频                   ~100MB
 │   ├── UI 资源                ~100MB
 │   └── 其他                   ~50MB
 └── 留空（防止 OOM）：       ~500MB

 ─── 纹理内存预算细化 ───

 纹理总量目标：400MB（移动端）
 │
 ├── UI 纹理：50MB
 │   不需要 mipmap，低分辨率
 │
 ├── 环境纹理：150MB
 │   使用 mipmap，最高 2048 分辨率
 │   压缩格式：ASTC 6x6
 │
 ├── 角色纹理：100MB
 │   使用 mipmap，最高 1024 分辨率
 │   压缩格式：ASTC 8x8（质量稍低）
 │
 └── 特效纹理：100MB
     使用 mipmap，最高 512 分辨率
     压缩格式：ASTC 8x8
*/
```

### 6.1 运行时内存监控

```csharp
// ─── 内存告警系统 ───

public class MemoryWatcher : MonoBehaviour
{
    [Header("Thresholds")]
    public float warningPercent = 0.7f;   // 70% 警告
    public float criticalPercent = 0.85f;  // 85% 临界
    
    private long totalMemoryMB;
    private long usedMemoryMB;
    
    void Start()
    {
        // 获取设备总内存
        totalMemoryMB = SystemInfo.systemMemorySize;
        StartCoroutine(MonitorMemory());
    }
    
    IEnumerator MonitorMemory()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);  // 每 5 秒检查
            
            // 获取当前 Unity 使用的内存
            usedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() /
                           1024 / 1024;
            
            float usagePercent = (float)usedMemoryMB / totalMemoryMB;
            
            if (usagePercent >= criticalPercent)
            {
                Debug.LogWarning($"内存危机！已使用 {usagePercent:P}，" +
                    $"当前 {usedMemoryMB}MB / {totalMemoryMB}MB");
                
                // 紧急释放
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
                
                // 降低纹理质量
                QualitySettings.globalTextureMipmapLimit = 2;
            }
            else if (usagePercent >= warningPercent)
            {
                Debug.Log($"内存警告：{usagePercent:P}");
            }
        }
    }
}
```

---

## C++/Raylib 对照总结

| 概念 | C++ 手动管理 | Unity Addressables |
|------|-------------|-------------------|
| 资源定位 | 硬编码路径 | 地址（Address）抽象层 |
| 依赖追踪 | 手动检查引用 | 自动依赖图 |
| 引用计数 | 智能指针 (shared_ptr) | Addressables 内置引用计数 |
| 内存预算 | 手动估算 | Memory Profiler + Addressables Profiler |
| 远程加载 | libcurl 自己实现 | 内建 CDN 支持 |
| 增量更新 | 自己写 diff 工具 | 自动生成差异包 |
| 并发控制 | 自己处理 | 内建队列和优先级 |
| — | 无 | AssetBundle → LZ4/LZMA 自动压缩 |
| — | 无 | Catalog 运行时索引 |

## 停靠点

> Addressables 分组策略决定了热更新的粒度：Core 组内置不更新，Content 组远程更新，Dynamic 组高频更新。
> Schema 是 Group 的配置模板——打包模式、压缩方式、加载路径都在 Schema 中设置。
> CDN 集成要点：版本检查 → Catalog 更新 → 增量下载。
> 自定义 AssetProvider 可以扩展加载流程（解密、解压、自定义格式）。
> 内存预算是硬约束——根据平台设定上限，超出时主动降级。
> **每个 Load 都要有对应的 Release**——这是 Addressables 使用的最关键规则。
