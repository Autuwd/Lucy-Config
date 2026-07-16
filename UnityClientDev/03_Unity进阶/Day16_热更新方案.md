# Day 16：热更新方案 — 从 AOT 到混合模式执行

## 0. 为什么需要热更新？

游戏发布到各平台后，如果发现 Bug 或需要加新功能，传统方式是：

```
传统流程：
修复 Bug → 重新打包 → 提交审核 → 等待通过 → 用户下载更新包
总耗时：3~7 天（甚至更久，尤其苹果 App Store）

热更新流程：
修复 Bug → 上传新的代码 DLL → 用户启动时自动下载 → 立即生效
总耗时：几小时
```

对于手游来说，热更新是**必须的**——无法接受每次修改都重新走发版流程。

---

## 1. AOT 与 JIT 的区别

### JIT（Just-In-Time）编译

```
C# 源码 → IL（中间语言）→ 运行时 JIT 编译 → 机器码

流程：
1. 你写的 C# 代码编译为 IL（.dll）
2. 运行时，CLR 调用方法时，检查该方法是否已编译
3. 如果未编译，JIT 编译器将 IL 编译为当前平台的机器码
4. 编译后的机器码被缓存，后续调用直接使用

优点：
- 可以动态生成代码（反射发射）
- 可以根据运行时信息优化

缺点：
- 启动慢（第一次编译）
- 不支持部分平台（iOS）
```

### AOT（Ahead-Of-Time）编译

```
C# 源码 → IL → AOT 编译器 → 原生机器码 → 打包到应用

流程（Unity 的 IL2CPP）：
1. 你的 C# 代码编译为 IL（.dll）
2. IL2CPP 将 IL 转换为 C++ 代码
3. 平台的 C++ 编译器将 C++ 编译为原生机器码
4. 打包到应用中

优点：
- 启动快（已经是机器码）
- iOS 必须（Apple 禁止 JIT）
- 性能更好（C++ 编译器的优化）

缺点：
- 包体更大
- 不能动态生成代码
```

### Apple 为什么禁止 JIT？

```csharp
// Apple 的安全政策：
// 动态生成和执行代码意味着：
// 1. 可以在审核通过后下载和执行任意代码（安全风险）
// 2. 审核时看到的是一个空壳，真正功能在运行后下载

// iOS 上只能执行从 App Store 下载的原生代码
// 所以 Unity iOS 必须使用 IL2CPP（AOT 编译）
// 无法使用 JIT 编译动态生成的代码
```

---

## 2. 热更新的核心问题

```
AOT 环境下如何执行新下载的代码？

问题是：
1. C# 代码被 AOT 编译为机器码，打包在应用中了
2. 新下载的代码是 IL（.dll）——没有机器码
3. iOS 不允许 JIT 编译（不能把 IL 转为机器码）

解决方案：
1. 解释执行（Interpreter）：用解释器逐条执行 IL 指令
2. 混合模式：AOT + Interpreter

三种方案：
- ILRuntime：纯解释器方案
- HybridCLR：AOT + Interpreter 混合方案（当前推荐）
- XLua/ToLua：嵌入 Lua 虚拟机方案
```

## 2b. XLua / ToLua——Lua 热更新方案

```
XLua 和 ToLua 的原理不是在 CLR 层面做热更新，
而是嵌入一个完整的 Lua 虚拟机到 Unity 中。

架构：
┌──────────────────────────────┐
│ C# 主工程（AOT 编译）          │
│  ├── 核心框架                 │
│  ├── 底层系统                 │
│  └── Lua ↔ C# 桥接层          │
├──────────────────────────────┤
│ Lua 虚拟机（嵌入）             │
│  ├── 热更新业务逻辑（.lua）     │
│  ├── UI 逻辑                  │
│  └── 活动/任务系统             │
└──────────────────────────────┘

工作流程：
1. C# 导出（Generate）：将需要 Lua 调用的 C# API 生成包装代码
2. Lua 编写业务逻辑
3. 运行时 Lua 通过桥接层调用 C# 功能
4. 更新只替换 .lua 文件

缺点：
- 需要学 Lua（团队学习成本）
- Lua ↔ C# 跨语言调用有性能开销
- 调试困难（Lua 错误信息不够清晰）
- 大量的 C# API 需要手动导出

优点：
- Lua 本身就是解释型语言，天然支持热更新
- Lua 生态成熟（很多老项目用）
- Lua 性能在移动端够用
```

---

## 3. ILRuntime——纯解释方案

### 原理

```csharp
// ILRuntime 是一个纯 C# 实现的 IL 解释器
// 它读取 IL 指令，逐条在 C# 虚拟机中解释执行

// 流程：
// 1. 下载热更新 DLL（IL 字节码）
// 2. ILRuntime 加载并解析 IL
// 3. 调用热更新方法时，ILRuntime 解释执行
// 4. 遇到 AOT 代码的调用 → 通过 CLR 绑定或反射跳转到原生代码

// 示例：
public class ILRuntimeDemo : MonoBehaviour
{
    private ILRuntime.Runtime.Enviorment.AppDomain appDomain;

    void Start()
    {
        appDomain = new ILRuntime.Runtime.Enviorment.AppDomain();

        // 加载热更新 DLL
        byte[] dllBytes = File.ReadAllBytes(
            Application.persistentDataPath + "/HotUpdate.bytes"
        );
        byte[] pdbBytes = File.ReadAllBytes(
            Application.persistentDataPath + "/HotUpdate.pdb"
        );

        using (var ms = new MemoryStream(dllBytes))
        {
            appDomain.LoadAssembly(ms, new MemoryStream(pdbBytes));
        }

        // 调用热更新方法
        appDomain.Invoke("HotUpdateMain", "Run", null, null);
    }
}
```

### ILRuntime 的性能瓶颈

```csharp
// 主要性能开销：
// 1. IL 指令解释（每条指令都要在 C# 虚拟机中处理）
// 2. CLR 边界：热更新代码 ↔ AOT 代码的调用开销
// 3. GC 分配：解释过程中的临时对象

// 优化手段：CLR 绑定
// 将频繁调用的 AOT 方法注册为"绑定方法"
// 避免反射调用，提升 20~50 倍性能

// 但即使优化，ILRuntime 的性能仍然不如原生代码
// 不适合性能敏感的逻辑（如每帧 Update）
```

---

## 4. HybridCLR——当前推荐方案

### 原理

```csharp
// HybridCLR（原来的 huatuo）是一个 CLR 级别的 AOT+Interpreter 方案
// 它直接扩展了 Unity IL2CPP 的运行时代码

// 核心思路：
// 1. 主代码：AOT 编译（执行在 IL2CPP VM 中）
// 2. 热更新代码：动态加载（解释执行在扩展的 Interpreter 中）
// 3. 两者运行在同一个 CLR 环境中
// 4. 热更新代码与 AOT 代码的调用没有 CLR 边界！

// 相比 ILRuntime 的优势：
// - 不需要 CLR 绑定（没有边界开销）
// - 热更新代码可以自由调用 AOT 代码
// - 支持完整的泛型（ILRuntime 的泛型支持有限）
// - 性能远高于 ILRuntime
```

### 使用方式

```csharp
// 1. 主工程代码（AOT 部分）
public class Bootstrap : MonoBehaviour
{
    void Start()
    {
        // 加载热更新 DLL
        LoadHotUpdateAssembly();

        // 调用热更新入口
        System.Reflection.Assembly hotUpdateAsm = AppDomain.CurrentDomain.GetAssemblies()
            .First(a => a.GetName().Name == "HotUpdate");
        Type type = hotUpdateAsm.GetType("HotUpdateMain");
        type.GetMethod("Run").Invoke(null, null);
    }

    void LoadHotUpdateAssembly()
    {
        // 从 persistentDataPath 加载热更新 DLL
        byte[] dllBytes = System.IO.File.ReadAllBytes(
            Application.persistentDataPath + "/HotUpdate.dll"
        );
        System.Reflection.Assembly.Load(dllBytes);
    }
}

// 2. 热更新工程代码（HotUpdate 程序集）
// 这个工程不在主包中，通过热更新下载
public class HotUpdateMain
{
    public static void Run()
    {
        UnityEngine.Debug.Log("这是热更新的代码！");

        // 可以直接调用主工程的代码
        // 不需要任何绑定——和写普通 C# 代码一样！
        GameManager.Instance.LoadScene("Level2");
    }
}
```

### 工程架构

```
解决方案结构：

Main Project（主工程 — AOT 编译）
├── Assets/
│   ├── Scripts/
│   │   ├── GameManager.cs
│   │   ├── Bootstrap.cs（加载热更新 DLL）
│   │   └── ...
│   └── ...

HotUpdate（热更新工程 — DLL）
├── Scripts/
│   ├── HotUpdateMain.cs
│   ├── UI/
│   ├── GameLogic/
│   └── ...

// 设计原则：
// 核心架构写在主工程（AOT）：
//   - 框架类（GameManager、EventSystem）
//   - 底层工具（资源管理、网络）
//   - 热更新需要调用的接口

// 业务逻辑写在热更新 DLL 中：
//   - UI 逻辑
//   - 游戏玩法
//   - 活动、任务系统
//   - Bug 修复
```

### 支持的平台

```
HybridCLR 支持所有 IL2CPP 平台：
- iOS ✅（解释执行，不违反 Apple 政策）
- Android ✅
- Windows ✅
- macOS ✅
```

---

## 5. 资源热更新

### 更新流程架构

```
完整的资源热更新流程：

客户端启动
    │
    ▼
请求版本信息（CDN 上的 version.txt）
    │
    ├── 版本一致 → 使用本地缓存，进入游戏
    │
    └── 版本不一致
            │
            ▼
        下载更新清单（manifest）
            │
            ▼
        对比本地资源，计算需要下载的文件
            │
            ▼
        下载差异文件（增量更新）
            │
            ▼
        更新本地资源
            │
            ▼
        进入游戏

增量更新的优势：
版本 1.0：包体内打包了所有资源（100MB）
版本 1.1：只下载 1.0→1.1 变化的文件（可能只有 5MB）
版本 1.2：只下载 1.1→1.2 变化的文件
这样用户每次更新只需要下载少量数据
```

### Addressables 资源更新

```csharp
// 使用 Addressables 进行资源热更新
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableUpdater : MonoBehaviour
{
    [Header("UI References")]
    public Slider progressBar;
    public TextMeshProUGUI statusText;

    void Start()
    {
        StartCoroutine(UpdateRoutine());
    }

    IEnumerator UpdateRoutine()
    {
        statusText.text = "Checking for updates...";

        // 1. 检查 Addressables 远程目录是否有更新
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;

        List<string> catalogs = checkHandle.Result;

        if (catalogs.Count > 0)
        {
            statusText.text = "Downloading updates...";

            // 2. 更新目录
            var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
            yield return updateHandle;

            // 3. 下载需要更新的资源
            var downloadHandle = Addressables.DownloadDependenciesAsync(
                "GameAssets", Addressables.MergeMode.Union);

            // 4. 显示下载进度
            while (!downloadHandle.IsDone)
            {
                float progress = downloadHandle.PercentComplete;
                progressBar.value = progress;
                statusText.text = $"Downloading... {progress:P0}";
                yield return null;
            }

            Addressables.Release(downloadHandle);
        }

        statusText.text = "Update complete!";
        yield return new WaitForSeconds(1f);

        // 5. 进入游戏
        SceneManager.LoadScene("MainMenu");
    }
}

// 更底层的 AssetBundle 更新：
// 如果不用 Addressables，需要手动管理 AssetBundle：

public class AssetBundleUpdater : MonoBehaviour
{
    IEnumerator UpdateWithAssetBundle()
    {
        // 1. 下载远端 Manifest
        UnityWebRequest manifestReq = UnityWebRequestAssetBundle.GetAssetBundle(
            "https://cdn.game.com/assetbundles/StandaloneWindows");
        yield return manifestReq.SendWebRequest();

        AssetBundle manifestBundle = DownloadHandlerAssetBundle.GetContent(manifestReq);
        AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        manifestBundle.Unload(false);

        // 2. 获取所有需要下载的 Bundle 名
        string[] allBundles = manifest.GetAllAssetBundles();

        // 3. 对比本地，下载缺失的
        foreach (string bundleName in allBundles)
        {
            Hash128 remoteHash = manifest.GetAssetBundleHash(bundleName);
            string localPath = Application.persistentDataPath + "/" + bundleName;

            if (!File.Exists(localPath))
            {
                // 下载这个 Bundle
                UnityWebRequest bundleReq = UnityWebRequestAssetBundle.GetAssetBundle(
                    "https://cdn.game.com/assetbundles/" + bundleName);
                yield return bundleReq.SendWebRequest();

                byte[] data = bundleReq.downloadHandler.data;
                File.WriteAllBytes(localPath, data);
            }
        }

        Debug.Log("AssetBundle update complete!");
    }
}
```

---

## 6. 方案选择

| 方案 | 性能 | 易用性 | 泛型支持 | 推荐度 |
|------|------|--------|---------|-------|
| ILRuntime | 中 | 中 | 有限 | 老项目 |
| HybridCLR | 高 | 高 | 完整 | **新项目首选** |
| XLua/ToLua | 中 | 低（需要学 Lua） | N/A | 老项目（Lua 团队） |

---

---

## C++/Raylib 对照总结

> Raylib 没有热更新的概念——C++ 游戏通常通过重新打包发版。
> Unity 的热更新方案是手游行业的特有需求。

| 概念 | 传统 C++ 游戏 | Unity C# 手游 |
|------|-------------|--------------|
| 更新方式 | 重新编译 + 重新发版 | 热更新 DLL / Lua |
| 审核周期 | 无（PC）/ 有（主机） | 苹果 App Store 3~7 天 |
| 代码修改 | 用户下载整个安装包 | 下载几 MB 的 DLL |
| 运行时类型 | 纯原生机器码 | AOT + 解释器混合 |
| JIT 支持 | 几乎所有平台 | iOS 禁止 |

## 停靠点

> iOS 禁止 JIT——所以需要 AOT + Interpreter 混合方案来实现热更新。
> HybridCLR = AOT 主工程 + 解释执行热更新 DLL。性能和原生几乎一致。
> ILRuntime 需要 CLR 绑定来优化性能，HybridCLR 不需要。
> 热更新代码 + 资源更新 = 完整的游戏热更新方案。
> 设计原则：**核心架构 AOT，业务逻辑热更新**。

