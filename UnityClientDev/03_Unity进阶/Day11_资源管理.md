# Day 11：资源管理 — 从硬盘到 GPU 的完整管线

## 0. 为什么需要资源管理？

游戏资源（纹理、模型、音效、Prefab）存储在硬盘上，运行时需要加载到内存和 GPU 中。

在 Raylib/C++ 中，资源加载是手动管理的：

```cpp
// Raylib：手动加载和卸载
Texture2D texture = LoadTexture("assets/player.png");  // 从硬盘加载到 GPU
Sound sound = LoadSound("assets/shoot.wav");            // 从硬盘加载到内存
// ...
UnloadTexture(texture);  // 手动卸载
UnloadSound(sound);
```

Unity 提供了多种资源管理方式——从最简单的 Inspector 拖拽，到 Addressables 的完整资源生命周期管理。

---

## 1. Unity 资源管线的完整流程

```
硬盘上的源文件 (.png, .fbx, .wav, .cs, ...)
    │
    ▼ 导入 (Import)
Unity Asset Importer 处理源文件
  - 纹理：设置压缩格式、mipmap、wrap mode
  - 模型：设置骨骼、动画
  - 音效：设置压缩、加载类型
    │
    ▼ 打包 (Build)
生成的 Asset Bundle 或 Resources 文件
    │
    ▼ 加载 (Load)
运行时从包体或 CDN 加载到内存
  - 纹理：CPU 内存中的原始数据 → GPU 显存中的纹理
  - 模型：CPU 内存中的网格数据 → GPU 显存中的 VBO/IBO
  - 音效：CPU 内存中的 PCM 数据 → 音频系统的缓冲区
    │
    ▼ 使用 (Use)
在游戏中使用资源
    │
    ▼ 卸载 (Unload)
不再需要时释放内存
```

---

## 2. 四种资源加载方式

### 方式 1：直接引用——最简单

```csharp
public class Player : MonoBehaviour
{
    // 直接在 Inspector 中拖拽赋值
    // Unity 在场景/ Prefab 序列化时保存引用
    // 加载场景时自动加载（没有异步/延迟）

    public GameObject bulletPrefab;   // Inspector 拖拽
    public Sprite playerSprite;       // Inspector 拖拽
    public AudioClip shootSound;      // Inspector 拖拽

    void Start()
    {
        // 直接用——无需手动加载
        GetComponent<SpriteRenderer>().sprite = playerSprite;
    }
}

// 原理：
// 1. Unity 序列化时保存了对资源的 GUID（全局唯一标识符）
// 2. 加载场景时，Unity 根据 GUID 找到资源并加载到内存
// 3. 资源依赖（如材质引用的纹理）也会递归加载
```

**优点：** 最简单，最直观，编辑器完全可视化管理

**缺点：**
- 场景加载时一次性加载所有引用的资源
- 无法控制加载时机
- 不适合热更新

### 方式 2：Resources——按路径加载

```csharp
// 资源必须放在 Assets/Resources/ 文件夹下
// 运行时通过 Resources.Load 按路径加载

public class ResourceLoader : MonoBehaviour
{
    void Start()
    {
        // 同步加载
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Bullet");
        Instantiate(prefab);

        // 异步加载（不卡主线程）
        StartCoroutine(LoadAsync());
    }

    IEnumerator LoadAsync()
    {
        ResourceRequest request = Resources.LoadAsync<GameObject>("Prefabs/BigModel");
        
        // 等待加载完成（不阻塞主线程）
        yield return request;

        // 使用加载的资源
        GameObject model = request.asset as GameObject;
        Instantiate(model);
    }
}
```

**Resources 的打包机制：**

```
Resources 文件夹下的所有资源会被打包到：
- 主包体中的 resources.assets 文件
- 索引文件：resources.resource

Resources.Load("Path/To/Asset") 的查找流程：
1. 在 resources.resource 索引中查找路径
2. 在 resources.assets 中找到对应的资源数据
3. 反序列化为 Asset 对象
4. 返回

问题：
- Resources 文件夹中的所有资源**总是被打包**，不管用不用
- 无法按需分包
- 包体会膨胀
```

**优点：** 简单，支持路径加载

**缺点：**
- 无法分下载（全部在包体内）
- 无法热更新
- 不用的资源也被打包
- Unity 官方**不推荐**大量使用 Resources

### 方式 3：AssetBundle——手动分包

```csharp
// AssetBundle 允许你将资源分到不同的包中
// 可以放在本地或服务器上
// 支持热更新！

public class BundleLoader : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadFromBundle());
    }

    IEnumerator LoadFromBundle()
    {
        // 从本地加载 AssetBundle
        AssetBundle bundle = AssetBundle.LoadFromFile(
            Application.streamingAssetsPath + "/bundles/weapons"
        );

        // 从 AssetBundle 加载资源
        AssetBundleRequest request = bundle.LoadAssetAsync<GameObject>("Sword");
        yield return request;

        GameObject sword = request.asset as GameObject;
        Instantiate(sword);

        // 卸载 AssetBundle（但保留已加载的资源）
        bundle.Unload(false);
    }
}
```

**AssetBundle 的依赖管理：**

```
假设：
  bundle_a (包含 sword.png)
  bundle_b (包含 sword_prefab.prefab，引用 sword.png)

加载 bundle_b 前必须加载 bundle_a！
否则 sword_prefab 的纹理丢失（粉红色）

依赖清单（manifest）：
Weapons.manifest
  - Assets/Models/Sword.prefab
  - Dependencies:
    - bundle_textures
    - bundle_materials
```

**优点：** 灵活，支持分包下载和热更新

**缺点：** 手动管理依赖非常复杂，容易出错

### 方式 4：Addressables——现代推荐方案

```csharp
// Addressables 是 Unity 的现代资源管理系统
// 它封装了 AssetBundle 的复杂性
// 支持远程加载、依赖自动管理、引用计数

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableLoader : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadWithAddressables());
    }

    IEnumerator LoadWithAddressables()
    {
        // 通过地址（Address）加载资源
        AsyncOperationHandle<GameObject> handle =
            Addressables.LoadAssetAsync<GameObject>("Enemy_Tank");

        // 等待完成
        yield return handle;

        // 使用资源
        GameObject enemy = handle.Result;
        Instantiate(enemy);

        // 使用完后释放（引用计数减 1）
        Addressables.Release(handle);
    }
}
```

**Addressables 的架构：**

```
地址 (Address) → 映射表 → 资源位置（哪个 Bundle？哪个文件？）
                                          │
                                          ▼
                                  加载 Bundle
                                          │
                                          ▼
                                  实例化资源（引用计数 + 1）
                                          │
                                          ▼
                                  释放资源（引用计数 - 1）
                                          │
                                          ▼
                                  引用计数为 0 → 卸载 Bundle
```

**Addressables 的核心特性：**

```csharp
// 1. 引用计数——自动管理依赖
// 加载 Enemy_Tank 时，自动加载它依赖的纹理和材质 Bundle
// 释放 Enemy_Tank 时，引用计数减 1
// 引用计数到 0 时自动卸载依赖 Bundle

// 2. 远程内容——支持热更新
// 可以将资源上传到 CDN
// 运行时检查更新并下载新版本

// 3. 标签系统——批量操作
// 给资源加标签："Character", "Level1", "UI"
Addressables.LoadAssetsAsync<GameObject>(
    new List<string> { "Level1" },  // 按标签加载
    obj => { /* 每个加载完成时回调 */ },
    Addressables.MergeMode.Union
);
```

**优点：** 自动依赖管理、引用计数、远程加载、热更新支持

**缺点：** 学习曲线较陡，项目初期需要规划分组策略

---

## 3. 场景加载

```csharp
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 同步加载（会卡顿）
    void LoadSceneSync()
    {
        SceneManager.LoadScene("Level2");
    }

    // 异步加载（不卡顿，显示进度条）
    IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync("Level2");
        operation.allowSceneActivation = false;  // 不自动激活

        // 显示加载进度
        while (operation.progress < 0.9f)  // 0~0.9 是加载阶段
        {
            float progress = operation.progress / 0.9f;
            Debug.Log($"Loading: {progress:P0}");  // 百分比
            yield return null;
        }

        Debug.Log("Loading complete! Press any key to continue");
        yield return new WaitUntil(() => Input.anyKeyDown);

        operation.allowSceneActivation = true;  // 激活场景
    }

    // 加载场景时保留一些对象
    void DontDestroyExample()
    {
        // GameManager 等不希望被销毁的对象
        DontDestroyOnLoad(gameObject);
    }
}
```

---

## 4. 资源卸载与内存管理

### Unity 的内存区域

```
Unity 内存分类：
┌─────────────────────────┐
│ 应用程序内存               │
├─────────────────────────┤
│ C# 托管堆 (Managed Heap) │ ← GC 管理
│  - C# 对象               │
│  - 反射调用等              │
├─────────────────────────┤
│ C# 原生堆 (Native Heap)  │ ← 引用计数管理
│  - 纹理、网格数据          │
│  - Shader                │
│  - AudioClip 的解压数据   │
├─────────────────────────┤
│ GPU 显存 (Video Memory)  │ ← 显卡内存
│  - 纹理的 GPU 格式         │
│  - 网格的 VBO/IBO         │
│  - Shader 程序            │
└─────────────────────────┘
```

### 手动卸载资源

```csharp
public class MemoryManager : MonoBehaviour
{
    // 卸载单个资源（从 CPU 内存中卸载纹理原始数据）
    void UnloadSingle()
    {
        Resources.UnloadAsset(myTexture);
        // 注意：只能卸载通过 Resources.Load 加载的资源
    }

    // 卸载所有未使用的资源
    IEnumerator UnloadUnused()
    {
        // 异步卸载所有未使用的 Asset 对象
        AsyncOperation op = Resources.UnloadUnusedAssets();
        yield return op;

        // 强制 GC 回收
        System.GC.Collect();
    }

    // AssetBundle 的卸载
    void UnloadBundle(AssetBundle bundle)
    {
        // false：卸载 Bundle，但保留已加载的资源
        // true：卸载 Bundle 和所有从它加载的资源
        bundle.Unload(false);
    }
}
```

### Unity 的内存泄露常见原因

```csharp
// 1. 事件未取消订阅
void Start()
{
    EventManager.OnPlayerDeath += OnPlayerDied;
    // 对象销毁时事件还在 → 对象无法 GC
}
void OnDestroy()
{
    EventManager.OnPlayerDeath -= OnPlayerDied;  // 必须取消！
}

// 2. 场景加载未清理引用
void Start()
{
    // 场景中的引用
    DontDestroyOnLoad(gameObject);
    // 但包含了对场景对象的引用
    referenceToSceneObject = GameObject.Find("TempObject");
    // 场景卸载后 TempObject 无法回收！
}

// 3. Resources.Load 但未卸载
void Update()
{
    // 每帧加载但不卸载！
    Texture2D tex = Resources.Load<Texture2D>("Textures/BigTexture");
    // 内存不断增长！
}
```

---

## 练习：资源加载管理器

```csharp
using UnityEngine;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    private static ResourceManager instance;
    public static ResourceManager Instance => instance;

    private Dictionary<string, Object> cache = new Dictionary<string, Object>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public T Load<T>(string path) where T : Object
    {
        // 缓存检查——避免重复加载
        if (cache.TryGetValue(path, out Object obj))
        {
            return obj as T;
        }

        // 加载
        T loaded = Resources.Load<T>(path);
        if (loaded != null)
        {
            cache[path] = loaded;
        }
        else
        {
            Debug.LogError($"Failed to load: {path}");
        }

        return loaded;
    }

    public void ClearCache()
    {
        cache.Clear();
        Resources.UnloadUnusedAssets();
    }

    void OnDestroy()
    {
        ClearCache();
    }
}
```

---

---

## C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity (C#) |
|------|-------------|-----------|
| 资源加载 | `LoadTexture("file.png")` | `Resources.Load<Texture2D>("path")` |
| 异步加载 | `--`（阻塞） | `Resources.LoadAsync()` / Addressables |
| 资源释放 | `UnloadTexture(texture)` | `Resources.UnloadAsset()` |
| — | 无 | Addressables 引用计数自动管理 |
| — | 无 | AssetBundle 分包 |
| — | 无 | Inspector 拖拽自动引用 |

## 停靠点

> Unity 的资源加载方式：直接引用（最简单）、Resources（按路径）、AssetBundle（手动分包）、Addressables（现代推荐）。
> AssetBundle 的依赖管理是最大痛点——Addressables 自动处理依赖。
> 资源生命周期：Load → Instantiate → Use → Release → Unload。
> 内存泄露三大来源：未取消的事件订阅、跨场景引用残留、Resources 加载未卸载。
> Unity 官方推荐 Addressables + 直接引用，避免大量使用 Resources。

