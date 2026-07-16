# Day 15：优化与性能 — Profiler 深度分析与 Burst 编译

## 0. 为什么还需要深入优化？

Day15 的基础篇介绍了 Profiler 的基本使用和 GC 优化。但真正优化大型项目时，你需要更专业的工具和方法论：

```
基础篇解决的问题：
- 用 Profiler 定位 CPU 热点
- GC 优化（对象池、缓存）
- Draw Call 合批
- IL2CPP vs Mono

进阶篇要解决的问题：
- 内存到底被什么占用了？（Memory Profiler 快照对比）
- SRP Batcher 为什么没生效？（调试渲染合批）
- GPU 瓶颈到底在哪？（RenderDoc 逐帧分析）
- 为什么 Shader 变体这么多了？（import 优化）
- 代码大小怎么减？（Code Stripping）
- 增量 GC 什么时候用？
- Burst 到底能快多少？
- 多个场景的 Profile 数据怎么对比分析？
```

---

## 1. Memory Profiler 深度使用

### 1.1 快照对比工作流

```csharp
// ─── Memory Profiler 的标准诊断流程 ───

/*
 步骤 1：设置基线（Baseline）
 1. 打开 Window → Analysis → Memory Profiler
 2. 游戏初始化完成后拍一个快照（作为对比基准）
 3. 命名："Baseline_Init"
 
 步骤 2：执行操作
 4. 进入一个关卡，玩一会儿
 5. 返回主菜单
 6. 拍另一个快照："After_Level1"
 
 步骤 3：对比分析
 7. 选中两个快照
 8. 点击 "Compare"
 9. Memory Profiler 会显示差异表：
 
 ┌─────────────────────────────────────────────┐
 │ 资源类型  │ Baseline  │ After    │ 差异     │
 │──────────│──────────│─────────│─────────│
 │ Texture  │ 120MB    │ 180MB   │ +60MB   │ ← 可能泄露
 │ Mesh     │ 30MB     │ 45MB    │ +15MB   │ ← 可能泄露
 │ Audio    │ 20MB     │ 25MB    │ +5MB    │
 │ GC Heap  │ 50MB     │ 55MB    │ +5MB    │
 │──────────│──────────│─────────│─────────│
 │ 总计     │ 220MB    │ 305MB   │ +85MB   │
 └─────────────────────────────────────────────┘
 
 如果 After 快照比 Baseline 大很多，
 说明操作后没有正确释放资源！
*/
```

### 1.2 引用链分析

```csharp
// ─── 找出谁引用了资源 ───
// 资源无法卸载通常是因为还有引用

/*
 Memory Profiler 的引用链功能：

 选中一个"应该被卸载"的资源 → 点击 "Show References"

 你会看到类似这样的引用链：

 Scene "MainMenu" (场景引用)
    └── Canvas GameUI (画布)
        └── Image_PlayerAvatar (图片组件)
            └── Texture_PlayerIcon (纹理) ← 被 UI 引用着！
                
 如果你的场景已经卸载了，但纹理还在：
 
 DontDestroyOnLoad Object GameManager (常驻对象)
    └── Sprite cachedAvatar (静态引用) ← 内存泄露！

 解决方法：在场景卸载时清理 GameManager 中的缓存的 Sprite
*/

public class MemoryLeakFix : MonoBehaviour
{
    // ⚠️ 错误的做法：挂载在 DontDestroyOnLoad 单例上
    // Sprite cachedSprite;  // ← 引用一直存在，场景卸载了也不释放
    
    // ✅ 正确的做法
    // 1. 场景卸载时调用下面这个方法
    public void OnSceneUnloaded()
    {
        // 清理场景相关的缓存
        Resources.UnloadUnusedAssets();
    }
    
    // 2. 或者用 WeakReference（弱引用）
    private System.WeakReference<Sprite> weakCache;
    
    public Sprite GetSprite()
    {
        if (weakCache != null &&
            weakCache.TryGetTarget(out Sprite sprite))
        {
            return sprite;
        }
        return null;
    }
}
```

### 1.3 用代码拍快照

```csharp
// ─── 用代码自动化快照采集 ───
// 在测试中使用，自动对比内存变化

using UnityEditor.MemoryProfiler;
using System.IO;

public class AutomatedMemoryTest
{
    // 在场景加载前后自动拍照
    public IEnumerator TestSceneMemory(string sceneName)
    {
        // 1. 拍快照 A
        yield return CaptureSnapshot("Snapshot_A_" + sceneName);
        
        // 2. 异步加载场景
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        yield return loadOp;
        
        // 3. 等待几帧，让场景完全加载
        yield return new WaitForSeconds(2f);
        
        // 4. 拍快照 B
        yield return CaptureSnapshot("Snapshot_B_" + sceneName);
        
        // 5. 自动分析
        AnalyzeDifference("Snapshot_A_" + sceneName,
                         "Snapshot_B_" + sceneName);
    }
    
    IEnumerator CaptureSnapshot(string label)
    {
        // 触发 GC 防止干扰
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        yield return null;
        
        // 请求 Memory Profiler 拍快照
        // MemoryProfiler 包提供了 API
        // MemoryProfiler.TakeSnapshot(path);
        
        yield return null;
    }
    
    void AnalyzeDifference(string snapshotA, string snapshotB)
    {
        // 在实际项目中，可以：
        // 1. 读取两个快照文件
        // 2. 解析差异
        // 3. 生成报告
        // 4. 如果差异超过阈值 → 报警
    }
}
```

---

## 2. SRP Batcher 深度解析

### 2.1 什么是 SRP Batcher？

```csharp
/*
 SRP Batcher 是 URP/HDRP 中的渲染合批系统。
 
 传统渲染流程（没有 SRP Batcher）：
 每个物体渲染时需要设置：
 1. 设置 Shader 属性（纹理、颜色等）  ← 开销最大
 2. 设置变换矩阵（位置、旋转、缩放）
 3. 提交 Draw Call
 
 总共：CPU 对每个物体逐个设置 → 慢！
 
 SRP Batcher 的流程：
 1. 批量设置 Shader 属性（一次设置多个物体）
 2. 逐个提交 Draw Call（但跳过设置阶段）
 
 节省：每次 Draw Call 的"设置 Shader 属性"阶段！
 限制：所有物体必须使用**同一个 Shader 变体**
*/

/*
 SRP Batcher 工作示意图：
                ┌──────────────┐
                │ CPU 设置循环  │
                └──────┬───────┘
                       │
        ┌──────────────┼──────────────┐
        ▼              ▼              ▼
  物体 A 材质    物体 B 材质       物体 C 材质
  (同一 Shader)  (同一 Shader)     (不同Shader!)
        │              │              │
        ▼              ▼              ▼
   SRP Batcher    SRP Batcher       ❌ 单独 Draw
   (合批 OK!)     (合批 OK!)         (无法合批)
*/
```

### 2.2 SRP Batcher 调试

```csharp
// ─── 在运行时检查 SRP Batcher 状态 ───

using UnityEngine.Rendering;

public class SRPBatcherDebug : MonoBehaviour
{
    void Update()
    {
        // ─── 方法 1：Frame Debugger ───
        // Window → Analysis → Frame Debugger
        // 查看每个 Draw Call 是否标记为 "SRP Batcher"
        
        // ─── 方法 2：代码获取渲染统计 ───
        // FrameTimingManager 提供详细数据
    }
}

// ─── SRP Batcher 无法生效的常见原因 ───

/*
 1. 材质使用不同的 Shader
    解决：确保共享着色器，用 MaterialPropertyBlock 差异化参数

 2. Shader 被标记为 DisableBatching
    解决：检查 Shader 中是否有关闭合批的 tag

 3. 使用了 Graphics.DrawMeshInstanced
    解决：GPU Instancing 和 SRP Batcher 互斥，不同对象用不同机制

 4. 材质启用了 EnableInstancing
    解决：Instancing 和 SRP Batcher 是两套机制

 5. 使用了 Shader Graph 但 Shader 变体过多
    解决：优化 Shader，减少变体数

 SRP Batcher 合批数查看：
 Frame Debugger → 展开 Draw Calls →
 查看 "SRP Batch" 行：
   - SRP Batch: 42 (batched)  → 42 个合批了
   - SRP Batch: 5 (not batched) → 5 个没合批
*/
```

### 2.3 MaterialPropertyBlock 的最佳实践

```csharp
// ─── 用 MaterialPropertyBlock 避免材质实例化 ───
// 核心思路：同一个材质，不同参数，但不创建新材质实例

public class ColorfulObjects : MonoBehaviour
{
    public Material sharedMaterial;  // 所有物体共用一个材质
    public Color[] colors;           // 不同颜色
    
    private MeshRenderer[] renderers;
    private MaterialPropertyBlock[] blocks;
    
    void Start()
    {
        renderers = GetComponentsInChildren<MeshRenderer>();
        blocks = new MaterialPropertyBlock[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            blocks[i] = new MaterialPropertyBlock();
            
            // 设置每个物体的颜色
            blocks[i].SetColor("_BaseColor", colors[i % colors.Length]);
            
            // 应用 PropertyBlock（不用 new Material！）
            renderers[i].SetPropertyBlock(blocks[i]);
        }
    }
    
    void Update()
    {
        // 更新颜色
        for (int i = 0; i < renderers.Length; i++)
        {
            blocks[i].SetColor("_BaseColor",
                Color.Lerp(Color.white, colors[i % colors.Length],
                    Mathf.Sin(Time.time + i) * 0.5f + 0.5f));
            
            // 再次应用——不需要重新实例化材质
            renderers[i].SetPropertyBlock(blocks[i]);
        }
    }
}

/*
 对比：
 ❌ renderer.material.color = Color.red;
    → 每次访问 .material 时，Unity 会创建新的 Material 实例！
    → 1000 个物体 = 1000 个材质实例！Draw Call 爆炸！
    → 破坏了 SRP Batcher 合批！
 
 ✅ renderer.SetPropertyBlock(block);
    → 不创建新材质
    → 1000 个物体仍然共享一个材质
    → SRP Batcher 正常工作！
*/
```

---

## 3. GPU 性能分析

### 3.1 RenderDoc 集成

```csharp
/*
 ─── RenderDoc 是什么？ ───
 RenderDoc 是开源的 GPU 调试工具。
 可以捕获一帧画面的所有 GPU 命令，逐条分析。

 工作流程：
 1. 在 Unity 中启动游戏（用 -renderdoc 命令行参数）
    Unity.exe -renderdoc -projectPath MyProject
    或者在 Window → RenderDoc 中配置

 2. 按 F11 或点击 Capture 按钮
 3. 查看这一帧的 GPU 命令

 你能看到什么：
 ┌──────────────────────────────────────────────┐
 │ Event Browser（事件浏览器）                    │
 │ ├── DrawIndexed(6, 0) → "Cube" (Mesh)        │
 │ │   ├── Shader: URP/Lit                      │
 │ │   ├── Input: POSITION, NORMAL, TEXCOORD0   │
 │ │   └── Output: RenderTarget #0              │
 │ ├── DrawIndexed(36, 0) → "Sphere"            │
 │ ├── DrawIndexed(24, 0) → "Player"            │
 │ ├── ClearRenderTargetView                    │
 │ ├── Dispatch(8, 8, 1) → Compute Shader "Blur" │
 │ └── ...                                      │
 ├──────────────────────────────────────────────┤
 │ 每个事件可以查看：                              │
 │ - 输入顶点数据（位置、法线、UV）                 │
 │ - 渲染管线状态（深度测试、模板、混合）            │
 │ - Shader 汇编代码（顶点着色器、片段着色器）       │
 │ - 渲染目标结果（截图）                          │
 │ - 性能计数器（像素占用、带宽）                   │
 └──────────────────────────────────────────────┘
*/
```

### 3.2 用 RenderDoc 定位 GPU 瓶颈

```csharp
/*
 ─── 常见 GPU 瓶颈分析 ───

 瓶颈 1：Overdraw（像素过度绘制）
 现象：Frame Debugger 中看到大量 Fragment Shader 调用
 解决方法：
 - 减少半透明叠加
 - 开启 Early-Z（深度预测试）
 - 用 Occlusion Culling
 - 检查 Shader 复杂度

 瓶颈 2：带宽不足
 现象：GPU 等待纹理加载
 解决方法：
 - 减小纹理尺寸
 - 用更低的 Mipmap 级别
 - 压缩格式更激进（ASTC 8x8 → ASTC 10x10）

 瓶颈 3：顶点处理瓶颈
 现象：Draw Call 数不多但 GPU 很慢
 解决方法：
 - 简化模型（减少顶点数）
 - 使用 LOD
 - 检查顶点 Shader 复杂度

 瓶颈 4：Fill Rate（填充率不足）
 现象：高分辨率下帧率骤降
 解决方法：
 - 降低渲染分辨率（动态分辨率）
 - 减小后处理复杂度
 - 减少多 Render Target 使用
*/

// ─── 动态分辨率缩放 ───
// 检测到 GPU 负载过高时自动降低分辨率

public class DynamicResolution : MonoBehaviour
{
    private float currentScale = 1.0f;
    private float targetFrameTime = 0.016f;  // 60 FPS
    
    void Update()
    {
        // 获取上一帧的实际耗时
        float frameTime = Time.deltaTime;
        
        if (frameTime > targetFrameTime * 1.1f)
        {
            // 帧超时，降低分辨率
            currentScale = Mathf.Max(
                0.5f, currentScale - 0.05f);
        }
        else if (frameTime < targetFrameTime * 0.8f)
        {
            // 帧太快，恢复分辨率
            currentScale = Mathf.Min(
                1.0f, currentScale + 0.02f);
        }
        
        // 应用分辨率缩放
        ScalableBufferManager.ResizeBuffers(
            currentScale, currentScale);
    }
}
```

### 3.3 Unity GPU Profiler

```csharp
// ─── 使用 GPU Profiler 模块 ───

void StartGPUProfiling()
{
    // Window → Analysis → Profiler → GPU Usage
    // 需要勾选 "GPU Profiler"（仅在 Editor 中可用）
    
    // GPU Profiler 中可以看到：
    // - Gfx.WaitForPresent：等待 GPU 完成（如果是瓶颈，说明 GPU 太慢）
    // - Gfx.PresentFrame：显示帧的时间
    // - Camera.Render：渲染整个场景的时间
    // - ShadowCaster：阴影渲染
    // - PostProcessing：后处理
}
```

---

## 4. Asset Import 优化管线

### 4.1 纹理导入优化

```csharp
// ─── 批量设置纹理导入参数 ───
// 导入管线优化可以大幅提升构建速度和运行时性能

using UnityEditor;

public class TextureImportOptimizer : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        TextureImporter importer = assetImporter as TextureImporter;
        
        // 检查文件名约定
        string fileName = System.IO.Path
            .GetFileNameWithoutExtension(assetPath);
        
        // ─── 按命名规范设置参数 ───
        if (assetPath.Contains("/UI/"))
        {
            // UI 纹理：不需要 Mipmap，Sprite 格式
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 100;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }
        else if (assetPath.Contains("/Environment/"))
        {
            // 环境纹理：需要 Mipmap，尺寸尽量大
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = true;
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            importer.streamingMipmaps = true;  // 流式 Mipmap
            importer.maxTextureSize = 4096;
            
            // 根据平台选择压缩
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android:
                    importer.SetPlatformTextureSettings(
                        new TextureImporterPlatformSettings
                        {
                            name = "Android",
                            format = TextureImporterFormat.ASTC_6x6,
                            overridden = true
                        });
                    break;
                case BuildTarget.iOS:
                    importer.SetPlatformTextureSettings(
                        new TextureImporterPlatformSettings
                        {
                            name = "iPhone",
                            format = TextureImporterFormat.ASTC_6x6,
                            overridden = true
                        });
                    break;
            }
        }
        else if (assetPath.Contains("/NormalMaps/"))
        {
            // 法线贴图
            importer.textureType = TextureImporterType.NormalMap;
            importer.convertToNormalmap = false;
            importer.mipmapEnabled = true;
        }
    }
    
    // ─── 导入后验证 ───
    void OnPostprocessTexture(Texture2D texture)
    {
        // 检查纹理是否过大
        if (texture.width > 4096 || texture.height > 4096)
        {
            Debug.LogWarning($"纹理过大：{assetPath} " +
                $"({texture.width}x{texture.height})");
        }
    }
}
```

### 4.2 模型导入优化

```csharp
// ─── 模型导入自动化 ───

public class ModelImportOptimizer : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        ModelImporter importer = assetImporter as ModelImporter;
        
        if (assetPath.Contains("/Characters/"))
        {
            // 角色模型：Humanoid 骨骼
            importer.animationType = ModelImporterAnimationType.Human;
            importer.importBlendShapes = true;
            importer.importMaterials = false;  // 独立处理材质
            importer.meshCompression = ModelImporterMeshCompression.Medium;
        }
        else if (assetPath.Contains("/Props/"))
        {
            // 场景物件：不需要动画
            importer.animationType = ModelImporterAnimationType.None;
            importer.importBlendShapes = false;
            importer.meshCompression = ModelImporterMeshCompression.High;
        }
        else if (assetPath.Contains("/Buildings/"))
        {
            // 建筑：不需要动画 + 尽量压缩
            importer.animationType = ModelImporterAnimationType.None;
            importer.importBlendShapes = false;
            importer.meshCompression = ModelImporterMeshCompression.High;
            
            // 开启 Opt 优化
            importer.optimizeMesh = true;
            importer.optimizeGameObjects = true;
        }
        
        // 通用设置
        importer.importVisibility = false;   // 不需要导入可见性
        importer.importCameras = false;      // 不需要导入摄像机
        importer.importLights = false;       // 不需要导入灯光
        importer.addCollider = false;        // 不需要导入碰撞体
    }
}
```

---

## 5. Code Stripping——代码裁减

### 5.1 Managed Code Stripping

```csharp
/*
 ─── Unity 的代码裁减层级 ───

 在 Project Settings → Player → Managed Stripping Level：

 1. Disabled（禁用）
    所有代码都保留
    包体最大，但最安全

 2. Low（低度）
    移除未使用的类和方法（链接器级别）
    对大多数项目安全

 3. Medium（中度）
    更激进的链接，移除反射中不使用的代码
    需要检查运行时是否正常

 4. High（高度）—— 推荐发布用
    最激进，移除所有"看起来"不用的代码
    包括通过反射调用的代码！
    必须用 link.xml 或 ConditionalAttribute 保护

 ─── link.xml 示例 ───
*/
```

```xml
<!-- link.xml —— 保护不被裁减的代码 -->
<linker>
  <!-- 保护整个程序集 -->
  <assembly fullname="UnityEngine">
    <type fullname="UnityEngine.Animation" preserve="all"/>
  </assembly>
  
  <!-- 保护特定命名空间 -->
  <assembly fullname="Assembly-CSharp">
    <namespace fullname="MyGame.Reflection" preserve="all"/>
  </assembly>
  
  <!-- 保护特定类型和成员 -->
  <assembly fullname="Assembly-CSharp-firstpass">
    <type fullname="ThirdPartySDK" preserve="methods"/>
  </assembly>
  
  <!-- 保护通过反射调用的类型 -->
  <assembly fullname="HotUpdate">
    <type fullname="HotUpdateMain" preserve="all"/>
  </assembly>
</linker>
```

### 5.2 代码裁减的包体影响

```csharp
/*
 ─── 包体测试数据（假设项目） ───
 
 Stripping Level  包体大小 (Android)  备注
 ──────────────────────────────────────
 Disabled          45 MB              不推荐
 Low               35 MB              开发用
 Medium            28 MB              可以测试
 High              22 MB              发布用
 ──────────────────────────────────────
 
 从 Disabled 到 High：节省约 50% 代码体积！
 代价：需要测试所有功能是否正常。
*/

// ─── Preserve 属性 ───
// 在代码中显式标记需要保留的类型/方法

using UnityEngine.Scripting;

public class ReflectionTargets
{
    [Preserve]  // 告诉裁减器：这个方法虽然看起来没用，但保留它
    public void CalledViaReflection()
    {
        // 这个方法是通过反射调用的
    }
}

[Preserve]  // 保留整个类
public class ThirdPartyBridge
{
    public void Init() { }
    public void DoSomething() { }
}
```

---

## 6. Incremental GC——增量式垃圾回收

### 6.1 原理

```csharp
/*
 ─── 增量 GC 解决了什么问题？ ───

 传统 GC（Stop-The-World）：
 ┌──────┬──────────────────────┬──────┐
 │ 帧 1 │       GC 暂停        │ 帧 2 │
 │      │    (50~100ms 卡顿)   │      │
 └──────┴──────────────────────┴──────┘
 
 增量 GC：
 ┌──────┬──┬──────┬──┬──────┬──┬──────┐
 │ 帧 1 │GC│ 帧 2 │GC│ 帧 3 │GC│ 帧 4 │
 │      │←2ms→│    │←2ms→│    │←2ms→│
 └──────┴──┴──────┴──┴──────┴──┴──────┘
 
 增量式 GC 将一次完整的 GC 暂停拆分为多次小暂停
 每次暂停 ~2ms，而不是 50~100ms
 用户几乎感知不到！
*/

// ─── 启用增量 GC ───

void SetIncrementalGC()
{
    // 方法 1：Project Settings → Player → 
    //   Configuration → Use Incremental GC（勾选）
    // 全局启用
    
    // 方法 2：运行时控制（需要 Unity 2021+）
    System.GC.TryStartNoGCRegion(50 * 1024 * 1024);  // 分配 50MB 不触发 GC
    // 这个区域内的分配不会触发 GC
    // 需要小心使用，不应超过限制
}
```

### 6.2 增量 GC 的限制

```csharp
/*
 ─── 增量 GC 不适用的情况 ───

 1. 大量小对象分配（> 1MB/帧）
    增量 GC 处理不了这么大量的分配
    
 2. 大对象堆（LOH > 85KB 的对象）
    增量 GC 不对 LOH 生效
    大对象仍然会导致 Stop-The-World

 3. 内存压力大（接近上限）
    增量 GC 会频繁触发小回收
    总体时间可能更长

 ─── 最佳实践 ───
 增量 GC 不是银弹，它只是让 GC 暂停更平滑。
 优化的根本仍然是减少分配！
*/
```

---

## 7. Burst Compiler 深度理解

### 7.1 性能对比测试

```csharp
// ─── 用 Benchmark 直观感受 Burst 的加速 ───

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Diagnostics;

public class BurstBenchmark : MonoBehaviour
{
    public int iterations = 10000000;  // 1 千万次
    private Stopwatch sw = new Stopwatch();
    
    void Start()
    {
        RunBenchmarks();
    }
    
    public void RunBenchmarks()
    {
        // ─── 测试 1：纯 C# ───
        sw.Restart();
        float result1 = 0;
        for (int i = 0; i < iterations; i++)
        {
            result1 += math.sin(i * 0.001f) *
                       math.cos(i * 0.002f);
        }
        sw.Stop();
        UnityEngine.Debug.Log($"纯 C#: {sw.ElapsedMilliseconds}ms, 结果={result1}");
        
        // ─── 测试 2：Burst 编译的 Job ───
        NativeArray<float> output = new NativeArray<float>(
            1, Allocator.TempJob);
        
        var job = new BurstMathJob
        {
            iterations = iterations,
            output = output
        };
        
        sw.Restart();
        job.Run();  // 在主线程运行（但经过 Burst 编译）
        sw.Stop();
        UnityEngine.Debug.Log($"Burst: {sw.ElapsedMilliseconds}ms, 结果={output[0]}");
        
        output.Dispose();
        
        /*
         预期输出：
         纯 C#: 350ms
         Burst: 45ms
         加速比：~7.8x
         
         如果加上 Job.Schedule（多线程）：
         加速比：~20x+
        */
    }
    
    [BurstCompile]
    struct BurstMathJob : IJob
    {
        public int iterations;
        public NativeArray<float> output;
        
        public void Execute()
        {
            float result = 0;
            for (int i = 0; i < iterations; i++)
            {
                result += math.sin(i * 0.001f) *
                          math.cos(i * 0.002f);
            }
            output[0] = result;
        }
    }
}
```

### 7.2 Burst 的编译模式

```csharp
/*
 ─── Burst 的三种编译模式 ───

 1. Fast Compile（快速编译 —— 开发用）
    编译时间快，优化少
    适合开发迭代
    
 2. Safe Compile（安全编译 —— 调试用）
    包含安全检查（如数组越界检测）
    性能和快速编译差不多
    
 3. Optimized Compile（优化编译 —— 发布用）
    编译器尽全力优化
    编译时间长，但运行最快
    会包含 SSE/AVX/NEON 指令集
    
 ─── 指令集优化 ───

 Burst 自动使用 CPU 支持的最强指令集：
 - SSE2：基础（所有 x64 CPU）
 - SSE4：较新（2010 年后的 CPU）
 - AVX：高端（2012 年后的 CPU）
 - AVX2：最新（2015 年后的 CPU）
 - NEON：ARM 平台（iOS/Android）
 
 例如：AVX2 一次能处理 8 个 float 加法
 而标量代码一次只能处理 1 个
 这就是自动向量化的威力！
*/

// ─── Burst 编译选项 ───

[BurstCompile(
    CompileSynchronously = true,   // 同步编译（等待编译完成再执行）
    OptimizeFor = OptimizeFor.Performance,  // 优化目标
    FloatMode = FloatMode.Fast,    // 浮点运算模式（Fast 比 Accurate 更快）
    FloatPrecision = FloatPrecision.Standard  // 浮点精度
)]
public struct OptimizedJob : IJob
{
    public void Execute()
    {
        // 使用 float4（一次处理 4 个 float）
        // Burst 编译器会将其映射到 SSE/AVX 指令
        float4 a = new float4(1, 2, 3, 4);
        float4 b = new float4(5, 6, 7, 8);
        float4 c = a + b;  // 一次 SIMD 加法完成！
        // 等价于 4 次标量加法
    }
}
```

---

## 8. Profile Analyzer——多帧分析

```csharp
// ─── Profile Analyzer 的使用 ───
// Package：Profile Analyzer（Package Manager 安装）

/*
 ─── 为什么要用 Profile Analyzer？ ───

 Profiler 只能看"一帧"的细节
 Profile Analyzer 可以分析"多帧"的数据

 ─── 工作流程 ───

 1. 录制多帧 Profile 数据（如 300 帧）
    Window → Analysis → Profiler → Record（录制 5 秒）
    保存：Save As → "Normal_Gameplay.data"

 2. 做对比操作（比如开启和关闭阴影）
    再录一次：Save As → "Shadow_Off.data"

 3. 打开 Profile Analyzer
    Window → Analysis → Profile Analyzer
    加载两个 .data 文件

 4. 对比分析：
 
 ┌───────────────────────────────────────────┐
 │ 对比: Normal_Gameplay vs Shadow_Off       │
 │                                           │
 │                       Normal | Shadow_Off │
 │──────────┬──────────┬─────────┬───────────│
 │ Total    │ 中位数   │ 17ms   │ 13ms      │
 │          │ 中位数   │ 核心    │ 节省 23%  │
 │──────────┼──────────┼─────────┼───────────│
 │ Camera   │ 中位数   │ 5ms    │ 3ms       │
 │ .Render  │ 峰值     │ 12ms   │ 5ms       │
 │──────────┼──────────┼─────────┼───────────│
 │          │ 最慢帧   │ 45ms   │ 18ms      │ ← 卡顿帧明显减少！
 └───────────────────────────────────────────┘
*/

// ─── 用代码标记自定义区间 ───
// 在 Profile Analyzer 中可以按区间分析

using UnityEngine.Profiling;

public class CustomProfilerMarker : MonoBehaviour
{
    private CustomSampler heavyOperationSampler;
    
    void Start()
    {
        // 创建自定义采样器
        heavyOperationSampler = CustomSampler.Create("HeavyOperation");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 标记区间的开始和结束
            heavyOperationSampler.Begin();
            
            // 执行耗时操作
            DoHeavyOperation();
            
            heavyOperationSampler.End();
        }
        
        // 或者在 Profile Analyzer 中：
        // Profiler.BeginSample("MyCustomOperation");
        // ...
        // Profiler.EndSample();
    }
    
    void DoHeavyOperation()
    {
        // 一些计算
    }
}
```

---

## C++/Raylib 对照总结

| 概念 | C++ / Raylib | Unity 进阶优化 |
|------|-------------|---------------|
| 内存分析 | Valgrind / Massif | Memory Profiler 快照对比 |
| GPU 分析 | NVIDIA Nsight / RenderDoc | RenderDoc 集成 + GPU Profiler |
| 渲染合批 | 手动优化 | SRP Batcher 自动合批 |
| 着色器管理 | 手动 glProgram | Shader 变体管理 + Stripping |
| 代码裁减 | 链接器 / LTO | Managed Code Stripping + link.xml |
| 模拟向量化 | 手写 SSE/NEON | Burst 编译器自动向量化 |
| 增量 GC | 无（手动管理） | Incremental GC（平滑暂停） |
| 性能对比 | 手动测帧率 | Profile Analyzer 多帧对比 |
| — | 无 | MaterialPropertyBlock（避免材质扩散）|
| — | 无 | 动态分辨率缩放 |

## 停靠点

> Memory Profiler 的核心工作流：拍基线快照 → 执行操作 → 拍对比快照 → 分析差异引用链。
> SRP Batcher 需要**同一 Shader 变体**才能合批——MaterialPropertyBlock 是关键工具。
> RenderDoc 逐帧查看 GPU 命令——定位像素 Overdraw、带宽瓶颈。
> Import 优化管线自动设置纹理压缩、Mipmap 和网格压缩——节省构建时间和运行时内存。
> Code Stripping 高等级可减 50% 代码体积——必须用 link.xml 保护反射调用的代码。
> 增量 GC 把大暂停拆成小暂停——适合需要平滑帧率的游戏，但对大对象堆无效。
> Burst 编译器将 C# Job 编译为 SIMD 机器码——浮点计算可加速 5~20 倍。
> Profile Analyzer 对比多帧数据——用数据决策优化方向。
