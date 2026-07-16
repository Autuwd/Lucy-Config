# Day 16：热更新方案 — HybridCLR 生产环境实践

## 0. 为什么还需要深入热更新？

Day16 的基础篇介绍了 HybridCLR 的基本原理和简单使用。但在真实项目中，落地热更新远比"加载一个 DLL"复杂：

```
基础篇解决的问题：
- 理解 AOT vs JIT
- 知道 HybridCLR 和 ILRuntime 的区别
- 简单的代码热更新示例

真实项目需要面对的挑战：
- HybridCLR 完整接入：从安装到打包的每一步
- 泛型在热更新中的特殊处理
- 资源更新的完整流水线
- 版本管理：怎么判断该更新什么
- 更新失败了怎么办（回滚）
- AOT 泛型补充的详细配置
- 代码安全：防止 DLL 被反编译
- ILRuntime 迁移到 HybridCLR 的注意事项
```

---

## 1. HybridCLR 完整接入指南

### 1.1 项目结构规划

```csharp
/*
 ─── HybridCLR 项目结构示例 ───

 UnityClientDev/
 ├── Assets/
 │   ├── Scripts/
 │   │   ├── AOT/                    ← AOT 编译的核心代码
 │   │   │   ├── GameManager.cs
 │   │   │   ├── Bootstrap.cs        ← 入口，加载热更新 DLL
 │   │   │   ├── UpdateManager.cs    ← 热更新管理器
 │   │   │   ├── AOTGenericTypes.cs  ← AOT 泛型补充
 │   │   │   └── ...
 │   │   │
 │   │   ├── HotUpdate/              ← 会被热更新的代码
 │   │   │   └── (运行时从 CDN 下载)
 │   │   │
 │   │   └── ...
 │   │
 │   ├── HybridCLRConfig/            ← HybridCLR 配置
 │   │   └── ...
 │   └── ...
 │
 ├── HotUpdateDLL/                   ← 热更新 DLL 工程（外部 .csproj）
 │   ├── Properties/
 │   ├── HotUpdateMain.cs
 │   ├── UILogic/
 │   ├── GameLogic/
 │   └── ...（编译输出 HotUpdate.dll）
 │
 └── ...
*/
```

### 1.2 完整接入步骤

```csharp
// ─── 步骤 1：安装 HybridCLR ───
/*
 Window → Package Manager → Add package by name
 输入: com.focus-creative-games.hybridclr

 或者通过 UPM：
 https://github.com/focus-creative-games/hybridclr_unity.git
*/

// ─── 步骤 2：初始化 HybridCLR ───
/*
 HybridCLR → Settings:
 1. 设置热更新 DLL 目录
 2. 配置 AOT 泛型补充
 3. 生成反向 P/Invoke 包装
*/

// ─── 步骤 3：配置热更新程序集 ───

using HybridCLR;

public class HybridCLRSetup : MonoBehaviour
{
    void Start()
    {
        // ─── 初始化 HybridCLR 运行时 ───
        // 必须在加载任何热更新 DLL 之前调用
        
        // 1. 补充 AOT 泛型
        // LoadAOTGeneric();
        
        // 2. 加载热更新 DLL
        StartCoroutine(LoadHotUpdateDLLs());
    }
    
    IEnumerator LoadHotUpdateDLLs()
    {
        // ─── 步骤 4：加载热更新 DLL ───
        // 从本地缓存或 CDN 下载后，通过 Assembly.Load 加载
        
        string dllPath = Application.persistentDataPath +
            "/HotUpdate/HotUpdate.dll";
        string pdbPath = Application.persistentDataPath +
            "/HotUpdate/HotUpdate.pdb";
        
        if (File.Exists(dllPath))
        {
            byte[] dllBytes = File.ReadAllBytes(dllPath);
            byte[] pdbBytes = File.ReadAllBytes(pdbPath);
            
            // 加载程序集（带 PDB 调试信息）
            System.Reflection.Assembly.Load(dllBytes, pdbBytes);
            
            Debug.Log("热更新 DLL 加载成功！");
        }
        else
        {
            Debug.LogError("热更新 DLL 不存在！");
            yield break;
        }
        
        // ─── 步骤 5：启动热更新逻辑 ───
        // 通过反射调用热更新 DLL 中的入口
        var hotUpdateAsm = AppDomain.CurrentDomain.GetAssemblies()
            .First(a => a.GetName().Name == "HotUpdate");
        
        var mainType = hotUpdateAsm.GetType("HotUpdateMain");
        var runMethod = mainType.GetMethod("Run");
        
        // 调用入口方法
        runMethod.Invoke(null, new object[] { });
    }
}
```

### 1.3 热更新 DLL 工程配置

```csharp
// ─── HotUpdate.csproj 配置（关键部分） ───

/*
 <Project Sdk="Microsoft.NET.Sdk">
   <PropertyGroup>
     <TargetFramework>netstandard2.0</TargetFramework>
     <RootNamespace>HotUpdate</RootNamespace>
     <AssemblyName>HotUpdate</AssemblyName>
     <LangVersion>9.0</LangVersion>
     <Nullable>enable</Nullable>
   </PropertyGroup>
   
   <ItemGroup>
     <!-- 引用 Unity 的 API -->
     <Reference Include="UnityEngine" />
     <Reference Include="UnityEngine.CoreModule" />
     <Reference Include="UnityEngine.UI" />
     
     <!-- 引用主工程的接口程序集 -->
     <Reference Include="Assembly-CSharp" />
   </ItemGroup>
 </Project>
*/

// ─── 热更新代码入口 ───

using UnityEngine;

public class HotUpdateMain
{
    public static void Run()
    {
        Debug.Log("[HotUpdate] 热更新代码启动！");
        
        // 创建热更新 UI
        var uiRoot = new GameObject("HotUpdateUIRoot");
        uiRoot.AddComponent<MainMenuPanel>();
        
        // 调用 AOT 代码
        GameManager.Instance.OnHotUpdateReady();
    }
}

// ─── 热更新中的 UI 逻辑 ───
// 注意：MonoBehaviour 必须挂载在场景中的 GameObject 上
// 不能在热更新 DLL 中 new MonoBehaviour！

public class MainMenuPanel : MonoBehaviour
{
    void Start()
    {
        // 初始化 UI
        Debug.Log("主菜单面板初始化（热更新代码）");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnStartGame();
        }
    }
    
    void OnStartGame()
    {
        Debug.Log("开始游戏！");
    }
}
```

---

## 2. HybridCLR vs ILRuntime 详细对比

### 2.1 架构差异

```csharp
/*
 ─── 核心架构对比 ───

 ILRuntime：
 ┌──────────────────────────────────────┐
 │ Unity IL2CPP VM（AOT）               │
 │   ├── 主工程代码（原生）               │
 │   └── CLR 绑定（胶水代码）             │
 │         ↑ 调用开销（反射/委托）         │
 │ ILRuntime 解释器（纯 C# 实现）          │
 │   └── 热更新 DLL（IL 字节码）          │
 └──────────────────────────────────────┘
 特点：
 - 两个独立的执行环境
 - 热更新代码 ↔ AOT 代码需要 CLR 绑定
 - 泛型支持有限（解释器的泛型实现复杂）
 
 HybridCLR：
 ┌──────────────────────────────────────┐
 │ Unity IL2CPP VM（AOT + 解释器扩展）    │
 │   ├── 主工程代码（AOT 原生）           │
 │   │   ← 无缝调用（无边界开销）          │
 │   └── 热更新代码（解释执行）            │
 │         ← 完全一致的运行时环境          │
 └──────────────────────────────────────┘
 特点：
 - 同一个运行时环境（扩展了 IL2CPP）
 - 热更新代码 ↔ AOT 代码可以互相直接调用
 - 完整泛型支持（因为 CLR 级别的支持）
*/
```

### 2.2 性能对比

```csharp
/*
 ─── 性能对比数据（参考值） ───

 ┌──────────────────────────────────────────────┐
 │ 测试项目        │ 原生 AOT │ HybridCLR │ ILRuntime │
 │────────────────│─────────│──────────│──────────│
 │ 普通方法调用    │ 1x      │ 3~5x     │ 20~50x   │
 │ 属性访问        │ 1x      │ 2~3x     │ 10~30x   │
 │ 数值计算        │ 1x      │ 2~4x     │ 30~80x   │
 │ 字符串操作      │ 1x      │ 1.5~2x   │ 5~15x    │
 │ 泛型方法        │ 1x      │ 3~8x     │ 50~200x  │
 │ 反射调用        │ 1x      │ 1~2x     │ 2~5x     │
 │ 内存分配        │ 1x      │ 1~1.5x   │ 3~5x     │
 └──────────────────────────────────────────────┘

 HybridCLR 性能损耗的主要来源：
 - 解释执行每条 IL 指令的开销
 - 但通过 AOT 调用优化（内联）可以显著降低

 ILRuntime 性能损耗的主要来源：
 - CLR 边界：每次热更新 ↔ AOT 调用都要通过绑定函数
 - 解释器的虚拟指令处理（比原生 IL 解释更慢）
 - 泛型的特殊处理（fallback 到 object 类型）
*/
```

### 2.3 泛型支持对比

```csharp
/*
 ─── 泛型支持的关键差异 ───

 ILRuntime 的泛型限制：
 1. 不支持泛型虚方法
    abstract class Base<T> { public abstract T GetValue(); }
    → ILRuntime 里无法正确调用

 2. 泛型类型需要注册
    appDomain.RegisterCrossBindingAdaptor(...) 
    → 手动处理

 3. 泛型接口有限支持
    IComparable<T> 等需要特殊处理

 4. LOH（List<T> 等）需要 CLR 绑定
    
 HybridCLR 的泛型支持：
 1. 完整泛型支持——和 AOT 完全一样
 2. 包括泛型方法、泛型类、泛型接口
 3. 泛型虚方法也能正常工作
 4. 唯一注意事项：AOT 泛型补充（下一节）

 简单来说：
 ILRuntime 中尽量避免泛型
 HybridCLR 中可以正常使用泛型
*/
```

---

## 3. HybridCLR 泛型补充

### 3.1 为什么要补充泛型？

```csharp
/*
 ─── 泛型与 AOT 的矛盾 ───

 问题背景：
 - AOT 编译时，泛型类型必须为具体类型展开
 - 但 AOT 编译器不知道运行时热更新代码会用什么类型参数
 - 所以某些泛型方法在 AOT 中没有生成原生代码

 举例：
 AOT 代码中写了：
 
 public class Serializer<T> {
     public byte[] Serialize(T obj) { ... }
 }
 
 HotUpdate 代码中调用：
 Serializer<int> serializer = new Serializer<int>();  // int 在 AOT 中展开过
 Serializer<float> fSerializer = new Serializer<float>(); // float 呢？可能没有！

 如果 Serializer<float> 在 AOT 中没有展开，
 解释器在执行时会找不到 float 版本的机器码！
*/
```

### 3.2 AOTGenericTypes 配置

```csharp
// ─── 手动补充 AOT 泛型 ───
// 方法 1：在 AOT 代码中"强制使用"要补充的类型

using UnityEngine;

// AOT 泛型补充表
public class AOTGenericTypes : MonoBehaviour
{
    public void Awake()
    {
        // ─── 通过"假调用"让 AOT 编译器展开泛型 ───
        // 这些代码实际上永远不会执行
        // 作用是让 IL2CPP 编译时生成这些泛型类型的原生代码
        
        _ = new List<int>();
        _ = new List<float>();
        _ = new List<string>();
        _ = new List<Vector3>();
        _ = new Dictionary<string, int>();
        _ = new Dictionary<int, float>();
        
        // 自定义泛型类
        _ = new Serializer<int>();
        _ = new Serializer<float>();
        _ = new Serializer<string>();
        
        // 泛型方法
        int[] intArray = null;
        Array.Sort(intArray);  // 强制展开 Array.Sort<int>()
        
        Debug.Log("AOT 泛型补充完成");
    }
}

// ─── 方法 2：使用 HybridCLR 的配置文件 ───
/*
 在 HybridCLR 的配置中，可以指定需要补充的类型：
 HybridCLR → Settings → AOT Generic Types

 添加需要补充的泛型类型实例：
 - List<int>
 - Dictionary<string, GameObject>
 - Serializer<float>
 - 等等...
*/
```

### 3.3 热更新代码中使用泛型的注意事项

```csharp
// ─── 安全的泛型使用模式 ───

public class HotUpdateGenericUser
{
    // ✅ 安全：int、float、string 等基础类型
    // 这些通常在 AOT 中已经展开了
    List<int> intList = new List<int>();
    Dictionary<string, int> dict = new Dictionary<string, int>();
    
    // ✅ 安全：Unity 常用类型
    List<Vector3> positions = new List<Vector3>();
    List<GameObject> objects = new List<GameObject>();
    
    // ⚠️ 有风险：自定义值类型
    // 需要确保 AOT 中补充了 MyStruct 的泛型展开
    // List<MyStruct> customList;
    
    // ❌ 不安全：运行时才确定的泛型参数
    // Type t = typeof(float);
    // var list = Activator.CreateInstance(
    //     typeof(List<>).MakeGenericType(t));
    // → 如果 List<float> 没有 AOT 展开，会报错！
    
    // ✅ 推荐的补全方式
    // 在 AOTGenericTypes 中强制使用一次
}
```

---

## 4. 资源热更新完整流水线

### 4.1 更新架构

```csharp
/*
 ─── 游戏启动更新流程 ───

 游戏启动
    │
    ▼
 ┌─────────────────────────────┐
 │ 1. 检查本地版本              │
 │    读取 persistentDataPath   │
 │    中的 Version.txt          │
 └─────────────┬───────────────┘
               │
               ▼
 ┌─────────────────────────────┐
 │ 2. 请求服务器版本             │
 │    CDN/version.txt           │
 │    (超时 5 秒，失败则离线)    │
 └─────────────┬───────────────┘
               │
               ▼
 ┌─────────────────────────────┐
 │ 3. 对比版本号                │
 │    相等 → 直接启动           │
 │    不等 → 继续              │
 └─────────────┬───────────────┘
               │
               ▼
 ┌─────────────────────────────┐
 │ 4. 下载更新清单              │
 │    update_manifest.json     │
 │    包含所有需要更新的文件     │
 └─────────────┬───────────────┘
               │
               ▼
 ┌─────────────────────────────┐
 │ 5. 增量下载                  │
 │    - 热更新 DLL              │
 │    - Addressables Bundle     │
 │    - 配置文件                 │
 │    显示下载进度               │
 └─────────────┬───────────────┘
               │
               ▼
 ┌─────────────────────────────┐
 │ 6. 应用更新                  │
 │    写入缓存目录               │
 │    更新本地版本号             │
 └─────────────┬───────────────┘
               │
               ▼
 ┌─────────────────────────────┐
 │ 7. 加载热更新 DLL            │
 │    Assembly.Load             │
 │    进入游戏主菜单             │
 └─────────────────────────────┘
*/
```

### 4.2 更新管理器实现

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class UpdateManager : MonoBehaviour
{
    [Header("CDN Config")]
    public string cdnBaseURL = "https://cdn.mygame.com/";
    public string versionFileName = "version.txt";
    public string manifestFileName = "update_manifest.json";
    
    [Header("Local Paths")]
    private string localVersionPath;
    private string downloadCachePath;
    
    [Header("UI")]
    public UnityEngine.UI.Slider progressBar;
    public UnityEngine.UI.Text statusText;
    
    [System.Serializable]
    class ManifestEntry
    {
        public string fileName;
        public string fileHash;      // MD5
        public long fileSize;        // 字节
        public bool isRequired;      // 是否必须更新
    }
    
    [System.Serializable]
    class UpdateManifest
    {
        public string version;
        public ManifestEntry[] files;
    }
    
    void Start()
    {
        localVersionPath = Application.persistentDataPath + "/version.txt";
        downloadCachePath = Application.persistentDataPath + "/Download/";
        
        StartCoroutine(UpdateRoutine());
    }
    
    IEnumerator UpdateRoutine()
    {
        // 1. 读取本地版本
        string localVersion = "0.0.0";
        if (File.Exists(localVersionPath))
        {
            localVersion = File.ReadAllText(localVersionPath).Trim();
        }
        
        statusText.text = "检查更新...";
        
        // 2. 请求远端版本
        string remoteVersionURL = cdnBaseURL + versionFileName;
        UnityWebRequest versionReq = UnityWebRequest.Get(remoteVersionURL);
        versionReq.timeout = 5;
        yield return versionReq.SendWebRequest();
        
        if (versionReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("版本检查失败，离线启动");
            StartGame();
            yield break;
        }
        
        string remoteVersion = versionReq.downloadHandler.text.Trim();
        
        // 3. 对比版本号
        if (remoteVersion == localVersion)
        {
            Debug.Log("已是最新版本，直接启动");
            StartGame();
            yield break;
        }
        
        // 4. 下载更新清单
        statusText.text = "获取更新清单...";
        string manifestURL = cdnBaseURL + manifestFileName;
        UnityWebRequest manifestReq = UnityWebRequest.Get(manifestURL);
        yield return manifestReq.SendWebRequest();
        
        if (manifestReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("获取更新清单失败");
            StartGame();  // 也可以选择阻止进入
            yield break;
        }
        
        // 解析清单
        UpdateManifest manifest = JsonUtility
            .FromJson<UpdateManifest>(manifestReq.downloadHandler.text);
        
        // 5. 检查本地文件，差异下载
        yield return DownloadUpdates(manifest);
        
        // 6. 保存新版本号
        File.WriteAllText(localVersionPath, remoteVersion);
        
        // 7. 启动游戏
        StartGame();
    }
    
    IEnumerator DownloadUpdates(UpdateManifest manifest)
    {
        // 创建缓存目录
        if (!Directory.Exists(downloadCachePath))
            Directory.CreateDirectory(downloadCachePath);
        
        int totalFiles = manifest.files.Length;
        int completedFiles = 0;
        long totalBytes = 0;
        long downloadedBytes = 0;
        
        // 计算总大小
        foreach (var entry in manifest.files)
            totalBytes += entry.fileSize;
        
        foreach (var entry in manifest.files)
        {
            string localPath = downloadCachePath + entry.fileName;
            
            // 检查本地文件是否已存在且完整
            if (File.Exists(localPath) &&
                GetMD5(localPath) == entry.fileHash)
            {
                completedFiles++;
                continue;
            }
            
            // 下载
            statusText.text = $"正在下载 {entry.fileName}";
            string url = cdnBaseURL + entry.fileName;
            
            UnityWebRequest dlReq = UnityWebRequest.Get(url);
            yield return dlReq.SendWebRequest();
            
            if (dlReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"下载失败：{entry.fileName}");
                
                if (entry.isRequired)
                    yield break;  // 必须的资源，停止更新
                else
                    continue;
            }
            
            // 写入本地
            string dir = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            File.WriteAllBytes(localPath, dlReq.downloadHandler.data);
            
            completedFiles++;
            downloadedBytes += entry.fileSize;
            
            // 更新进度
            if (progressBar != null)
                progressBar.value = (float)downloadedBytes / totalBytes;
        }
        
        Debug.Log("所有文件下载完成！");
    }
    
    string GetMD5(string filePath)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        using (var stream = File.OpenRead(filePath))
        {
            byte[] hash = md5.ComputeHash(stream);
            return System.BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
    
    void StartGame()
    {
        // 加载热更新 DLL → 进入游戏
        statusText.text = "正在启动...";
        
        // 实际项目中在这里调用 Bootstrap
        SceneManager.LoadScene("MainMenu");
    }
}
```

---

## 5. 版本管理与回滚

### 5.1 版本号策略

```csharp
/*
 ─── 语义化版本号 ───

 格式：主版本.次版本.修订号
 示例：1.2.3

 主版本：不兼容的 API 修改（大版本更新）
 次版本：向下兼容的功能新增
 修订号：向下兼容的 Bug 修复

 ─── 游戏热更新的版本策略 ───

 包体版本（App Version）：1.0.0
   ↓
 资源版本（Res Version）：20240101 (日期)
   → 每次资源更新递增
   → CDN 上的 update_manifest.json 记录最新版本
   
 代码版本（Code Version）：100
   → 每次热更新 DLL 变动递增
   → 和资源版本配合使用

 ─── 版本文件示例（version.txt） ───
 
 {"app":"1.0.0","res":"20240315","code":105}

 客户端读取后与本地对比：
 1. app 不同 → 需要去 App Store 更新包体
 2. res 不同 → 下载资源更新
 3. code 不同 → 下载热更新 DLL
*/
```

### 5.2 回滚机制

```csharp
// ─── 更新失败时的回滚策略 ───

public class UpdateRollback : MonoBehaviour
{
    // ─── 原则：保留上一个版本 ───
    /*
     目录结构：
     persistentDataPath/
     ├── Current/           ← 当前使用的版本
     ├── Previous/          ← 上一个版本（备用）
     └── Downloading/       ← 正在下载的新版本
    */
    
    // ─── 安全更新流程 ───
    
    public IEnumerator SafeUpdate()
    {
        // 1. 下载新版本到 Downloading 目录
        yield return DownloadToDirectory("Downloading");
        
        // 2. 验证完整性（MD5 校验）
        if (!VerifyDirectory("Downloading"))
        {
            Debug.LogError("下载内容不完整，触发回滚");
            DeleteDirectory("Downloading");
            yield break;
        }
        
        // 3. 备份当前版本
        if (Directory.Exists("Current"))
        {
            // 删除旧的 Previous
            DeleteDirectory("Previous");
            
            // 移动 Current → Previous
            Directory.Move("Current", "Previous");
        }
        
        // 4. 部署新版本
        Directory.Move("Downloading", "Current");
        
        // 5. 重启并加载新版本
        Debug.Log("更新成功！");
    }
    
    // ─── 紧急回滚 ───
    public void EmergencyRollback()
    {
        if (Directory.Exists("Previous"))
        {
            DeleteDirectory("Current");
            Directory.Move("Previous", "Current");
            
            Debug.Log("已回滚到上一个版本");
        }
    }
    
    void DeleteDirectory(string dirName)
    {
        string path = Application.persistentDataPath + "/" + dirName;
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }
    
    bool VerifyDirectory(string dirName)
    {
        // 校验所有文件的 MD5
        // 和 manifest 中的 hash 对比
        return true;  // 示例
    }
    
    // ─── 版本切换时的注意事项 ───
    /*
     1. 热更新 DLL 只能在游戏启动时加载
     2. 运行时不能替换已加载的 DLL
     3. 所以版本切换需要完全重启
     
     做法：
     - 下载完成后，记录"待更新"标记
     - 显示"更新完成，请重启游戏"
     - 下次启动时加载新版本
    */
}
```

---

## 6. AOT 泛型补充深入

### 6.1 自动补充 vs 手动补充

```csharp
// ─── HybridCLR 的自动泛型补充 ───
/*
 HybridCLR 提供两个级别的泛型补充机制：

 级别 1：自动补充
 HybridCLR 在构建时会扫描热更新 DLL 中使用的泛型类型
 自动在 AOT 中补充对应的泛型展开
 但这种方式只覆盖"直接使用"的类型

 级别 2：手动补充
 对于一些"通过反射创建的泛型类型"或"间接使用"的类型
 需要开发者手动补充
*/

// ─── 常见的需要手动补充的场景 ───

public class GenericSupplementExamples
{
    // 场景 1：热更新代码中使用了，但扫描不到
    void IndirectUse()
    {
        // AOT 代码中的 List<T> 用到了 MyStruct
        // 但 HotUpdate 中间接使用了 List<MyStruct>
        // → List<MyStruct> 需要在 AOT 中补充
    }
    
    // 场景 2：通过反射创建的泛型
    void ReflectionUse()
    {
        Type listType = typeof(List<>);
        Type intList = listType.MakeGenericType(typeof(int));
        // → List<int> 如果没有在 AOT 中展开，会报错
    }
    
    // 场景 3：协变/逆变接口
    void CovariantUse(IEnumerable<object> items)
    {
        // IEnumerable<string> 到 IEnumerable<object>
        // → 需要 AOT 中包含这些接口展开
    }
}
```

### 6.2 最佳实践

```csharp
// ─── 推荐的泛型补充清单模板 ───

// 包含项目中所有热更新代码使用的泛型类型
public static class AOTGenericSupplements
{
    // ─── 基础集合类型 ───
    static void SupplementCollectionTypes()
    {
        // 确保所有热更新中用到的泛型集合
        _ = new List<object>();
        _ = new List<int>();
        _ = new List<long>();
        _ = new List<float>();
        _ = new List<double>();
        _ = new List<string>();
        _ = new List<UnityEngine.Vector2>();
        _ = new List<UnityEngine.Vector3>();
        _ = new List<UnityEngine.Quaternion>();
        _ = new List<UnityEngine.Color>();
        _ = new Dictionary<string, object>();
        _ = new Dictionary<int, object>();
        _ = new Dictionary<string, int>();
        _ = new HashSet<int>();
        _ = new HashSet<string>();
        _ = new Queue<object>();
        _ = new Stack<object>();
    }
    
    // ─── 自定义值类型 ───
    static void SupplementCustomTypes()
    {
        // 热更新中用到的自定义 struct
        // _ = new List<MyStruct>();
        // _ = new List<AnotherStruct>();
    }
    
    // ─── 委托和函数 ───
    static void SupplementDelegateTypes()
    {
        _ = new System.Func<int>();
        _ = new System.Func<bool>();
        _ = new System.Func<string, int>();
        _ = new System.Action();
        _ = new System.Action<int>();
        _ = new System.Action<float, int>();
        _ = new System.Predicate<int>();
        _ = new System.Comparison<int>();
    }
    
    // ─── 可空类型 ───
    static void SupplementNullableTypes()
    {
        _ = new System.Nullable<int>();
        _ = new System.Nullable<float>();
        _ = new System.Nullable<bool>();
    }
    
    // ─── 异步相关（如果用了 async/await） ───
    static void SupplementAsyncTypes()
    {
        // Task 相关的泛型
        // _ = new System.Threading.Tasks.Task<int>();
        // _ = new System.Threading.Tasks.Task<bool>();
    }
}
```

---

## 7. 代码加密与混淆

### 7.1 为什么要加密热更新 DLL？

```csharp
/*
 ─── 热更新 DLL 面临的安全风险 ───

 1. 反编译
    HotUpdate.dll 是标准的 .NET 程序集
    用 dnSpy / ILSpy 可以直接查看源码！
    
 2. 篡改
    黑客可以修改 DLL 的内容
    实现作弊、破解内购
    
 3. 盗版
    完整的游戏逻辑被提取
    用于制作私服
*/
```

### 7.2 加密方案

```csharp
// ─── 方案 1：XOR 简单加密 ───
// 防君子不防小人，但能挡住大部分脚本小子

public class SimpleEncryption
{
    private static byte[] xorKey = new byte[] {
        0xAB, 0xCD, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC
    };
    
    public static byte[] Encrypt(byte[] data)
    {
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ xorKey[i % xorKey.Length]);
        }
        return result;
    }
    
    public static byte[] Decrypt(byte[] encrypted)
    {
        // XOR 是对称的，再次 XOR 就解密了
        return Encrypt(encrypted);
    }
}

// ─── 方案 2：AES 对称加密（推荐） ───

using System.Security.Cryptography;

public class AESEncryption
{
    private static byte[] key = {
        0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF,
        0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10
    };
    private static byte[] iv = {
        0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
        0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
    };
    
    public static byte[] AESEncrypt(byte[] plainData)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(
                ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(plainData, 0, plainData.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }
    
    public static byte[] AESDecrypt(byte[] encryptedData)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using (var ms = new MemoryStream(encryptedData))
            using (var cs = new CryptoStream(
                ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (var result = new MemoryStream())
            {
                cs.CopyTo(result);
                return result.ToArray();
            }
        }
    }
}
```

### 7.3 混淆工具

```csharp
/*
 ─── .NET 混淆工具对比 ───

 1. ConfuserEx（免费，开源）
    - 支持重命名、控制流混淆、反调试
    - 适合中小项目
    - GitHub: https://github.com/yck1509/ConfuserEx

 2. Obfuscator（商业，付费）
    - .NET Reactor、SmartAssembly 等
    - 更强的保护
    - 适合商业项目

 3. 自定义混淆
    - 在构建后处理脚本中实现
    - 可以集成到 CI/CD 管线
*/

// ─── 构建后自动加密脚本 ───

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

public class PostBuildEncrypt : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;
    
    public void OnPostprocessBuild(BuildReport report)
    {
        // 构建完成后自动加密热更新 DLL
        string outputPath = report.summary.outputPath;
        string hotUpdateDir = Path.Combine(
            Path.GetDirectoryName(outputPath), "HotUpdate");
        
        if (Directory.Exists(hotUpdateDir))
        {
            string dllPath = Path.Combine(hotUpdateDir, "HotUpdate.dll");
            
            if (File.Exists(dllPath))
            {
                byte[] dllBytes = File.ReadAllBytes(dllPath);
                byte[] encrypted = AESEncryption.AESEncrypt(dllBytes);
                File.WriteAllBytes(dllPath + ".encrypted", encrypted);
                
                // 删除原始 DLL
                File.Delete(dllPath);
                
                Debug.Log("热更新 DLL 已加密！");
            }
        }
    }
}
```

---

## C++/Raylib 对照总结

| 概念 | C++ 传统 | Unity + HybridCLR |
|------|---------|-------------------|
| 热修复 | 重新打包发版 | 热更新 DLL 即时修复 |
| 泛型 | 模板编译时展开 | AOT 泛型补充（手动声明） |
| 代码分发 | 下载完整二进制 | 下载 IL DLL（解释执行）|
| 版本管理 | 手动记录 | 语义化版本 + MD5 校验 |
| 回滚 | 重新安装旧版 | 保留 Previous 目录回滚 |
| 加密 | 二进制难读 | AES 加密热更新 DLL |
| 资源更新 | 无 | Addressables + CDN 增量更新 |
| — | 无 | 解释器 + AOT 混合执行 |
| — | 无 | 同运行时（无缝 AOT ↔ 解释） |

## 停靠点

> HybridCLR 是扩展 IL2CPP 的解释器，与 AOT 代码在同一运行时中——无需 CLR 绑定，无缝调用。
> HyrbidCLR 性能远优于 ILRuntime（3~5x vs 20~50x 原生开销），泛型支持也更完整。
> AOT 泛型补充是 HybridCLR 的核心配置——用"假调用"或配置表强制 IL2CPP 展开泛型。
> 版本管理用 MD5 校验文件完整性，保留 Previous 版本支持回滚。
> 热更新 DLL 必须用 AES 加密——防止反编译和篡改。
> 完整热更新流水线：版本检查 → 清单下载 → 增量更新 → 完整性校验 → DLL 加载 → 启动游戏。
> 设计原则：核心框架 AOT 编译，业务逻辑热更新。
