# Day 17：编辑器扩展 — UI Toolkit 与现代编辑器工具链

## 0. 为什么还需要深入学习编辑器扩展？

Day17 的基础篇覆盖了 MenuItem、EditorWindow、基本 IMGUI 和 UI Toolkit 入门。但大型项目的编辑器工具需要更专业的设计：

```
基础篇解决的问题：
- 添加菜单项和快捷键
- 用 IMGUI 创建简单窗口
- 自定义 Inspector 的基本方法
- UI Toolkit 入门（UXML + USS）

进阶篇要解决的问题：
- UI Toolkit vs IMGUI：什么时候用哪个？
- 自定义 PropertyDrawer：让 Inspector 更好用
- 基于 ScriptableObject 的设置系统
- 编辑器性能分析：哪些工具拖慢了编辑器？
- 自定义 Build Pipeline：控制整个构建流程
- Editor Coroutines：异步操作编辑器
- Odin Inspector 模式（如果我们不用 Odin 怎么自己实现）
- 编辑器插件架构设计
```

---

## 1. UI Toolkit 编辑器深度使用

### 1.1 UI Toolkit 核心概念

```csharp
/*
 ─── UI Toolkit 三层架构 ───

 1. 视觉层（Visual Tree）
    - 由 VisualElement 组成的树状结构
    - 每个元素是一个节点（类似 DOM 树）
    - UXML 声明式的树，或 C# 命令式创建

 2. 样式层（Style Sheet）
    - USS 文件（类似 CSS）
    - 选择器 + 属性
    - 支持类、元素名、伪类（:hover :focus）

 3. 逻辑层（Logic）
    - C# 事件处理
    - 数据绑定
    - 回调（Callbacks）

 ─── 对比 IMGUI ───

 IMGUI（OnGUI）：                    UI Toolkit（CreateGUI）：
 - 每帧从头绘制                       - 声明式 UI 树
 - 状态用成员变量保存                   - 状态自动管理
 - 控制流代码（if/for 混合 UI）        - 事件驱动
 - 布局难控制                         - USS 精确布局
 - 没有样式复用                       - 样式复用（类选择器）
 - 简单、轻量、适合小窗口               - 适合复杂编辑器界面
*/
```

### 1.2 用 C# 构建 UI Toolkit 窗口

```csharp
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class AdvancedEditorWindow : EditorWindow
{
    // ─── 模型层（数据） ───
    private List<string> recentFiles = new List<string>();
    private string searchQuery = "";
    
    [MenuItem("Tools/Advanced Editor Window")]
    static void Open()
    {
        var wnd = GetWindow<AdvancedEditorWindow>();
        wnd.titleContent = new GUIContent("Advanced Tools");
        wnd.minSize = new Vector2(400, 300);
    }
    
    // ─── UI Toolkit 创建 GUI（替代 OnGUI） ───
    void CreateGUI()
    {
        // 方法 1：用 UXML 加载
        // var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
        //     "Assets/Editor/AdvancedWindow.uxml");
        // visualTree.CloneTree(rootVisualElement);
        
        // 方法 2：用 C# 构建（更灵活）
        BuildUI();
    }
    
    void BuildUI()
    {
        var root = rootVisualElement;
        
        // ─── 布局 ───
        // 使用 Flexbox 布局
        
        // 顶部工具栏
        var toolbar = new VisualElement();
        toolbar.style.flexDirection = FlexDirection.Row;
        toolbar.style.marginBottom = 8;
        toolbar.style.minHeight = 24;
        
        var searchField = new TextField { value = searchQuery };
        searchField.style.flexGrow = 1;
        searchField.RegisterValueChangedCallback(evt =>
        {
            searchQuery = evt.newValue;
            FilterList();
        });
        toolbar.Add(searchField);
        
        var refreshButton = new Button(() => RefreshList());
        refreshButton.text = "⟳";
        refreshButton.style.width = 30;
        toolbar.Add(refreshButton);
        
        root.Add(toolbar);
        
        // ─── 列表视图 ───
        var listView = new ListView(
            recentFiles,   // itemsSource
            20,            // itemHeight
            MakeItem,      // 创建列表项
            BindItem       // 绑定数据
        );
        listView.name = "fileListView";
        listView.style.flexGrow = 1;
        listView.selectionType = SelectionType.Single;
        listView.onSelectionChange += OnSelectionChanged;
        
        root.Add(listView);
        
        // ─── 底部操作栏 ───
        var actionBar = new VisualElement();
        actionBar.style.flexDirection = FlexDirection.Row;
        actionBar.style.justifyContent = Justify.FlexEnd;
        actionBar.style.marginTop = 8;
        
        var openButton = new Button(() => OpenSelected()) { text = "Open" };
        var deleteButton = new Button(() => DeleteSelected()) { text = "Delete" };
        
        actionBar.Add(openButton);
        actionBar.Add(deleteButton);
        root.Add(actionBar);
    }
    
    VisualElement MakeItem()
    {
        // 每个列表项
        var item = new VisualElement();
        item.style.flexDirection = FlexDirection.Row;
        item.style.paddingLeft = 4;
        item.style.paddingRight = 4;
        
        var label = new Label();
        label.style.unityTextAlign = TextAnchor.MiddleLeft;
        label.style.flexGrow = 1;
        item.Add(label);
        
        var sizeLabel = new Label();
        sizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        sizeLabel.style.color = Color.gray;
        sizeLabel.style.fontSize = 10;
        item.Add(sizeLabel);
        
        // 存储对 label 的引用
        item.userData = new { label, sizeLabel };
        return item;
    }
    
    void BindItem(VisualElement item, int index)
    {
        var data = (dynamic)item.userData;
        data.label.text = recentFiles[index];
        data.sizeLabel.text = $"{index + 1}";
    }
    
    void FilterList()
    {
        // 过滤列表逻辑
        var listView = rootVisualElement.Q<ListView>("fileListView");
        listView.Rebuild();
    }
    
    void RefreshList()
    {
        recentFiles.Clear();
        // 模拟数据
        for (int i = 0; i < 20; i++)
            recentFiles.Add($"File_{i}.asset");
        
        var listView = rootVisualElement.Q<ListView>("fileListView");
        listView.itemsSource = recentFiles;
        listView.Rebuild();
    }
    
    void OnSelectionChanged(IEnumerable<object> selection)
    {
        // 选中项改变
    }
    
    void OpenSelected()
    {
        // 打开选中的文件
    }
    
    void DeleteSelected()
    {
        // 删除选中的文件
    }
}
```

### 1.3 USS 样式主题

```css
/* ─── Editor.uss —— 编辑器窗口样式 ─── */

/* 窗口根元素 */
.advanced-window {
    background-color: rgb(50, 50, 50);
    padding: 8px;
}

/* 工具栏按钮 */
.toolbar-button {
    background-color: rgb(70, 70, 70);
    border-color: rgb(100, 100, 100);
    border-width: 1px;
    border-radius: 3px;
    padding: 4px 8px;
    margin: 2px;
    color: white;
}

.toolbar-button:hover {
    background-color: rgb(90, 90, 90);
}

.toolbar-button:active {
    background-color: rgb(50, 100, 180);
}

/* 列表视图样式 */
#fileListView {
    border-color: rgb(60, 60, 60);
    border-width: 1px;
}

#fileListView .unity-list-view__item:selected {
    background-color: rgb(50, 100, 180);
}

/* 搜索框 */
.search-field {
    flex-grow: 1;
    margin-right: 4px;
}
```

---

## 2. 自定义 PropertyDrawer

### 2.1 基础 PropertyDrawer

```csharp
// ─── 自定义属性绘制 ───
// 让 Inspector 中显示更友好

using UnityEditor;
using UnityEngine;

// 示例：用 [ProgressBar] 属性代替普通的 float 输入框

public class ProgressBarAttribute : PropertyAttribute
{
    public string label;
    public float min;
    public float max;
    
    public ProgressBarAttribute(string label = "Progress",
        float min = 0, float max = 1)
    {
        this.label = label;
        this.min = min;
        this.max = max;
    }
}

// ─── PropertyDrawer 实现 ───

[CustomPropertyDrawer(typeof(ProgressBarAttribute))]
public class ProgressBarDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position,
        SerializedProperty property, GUIContent label)
    {
        ProgressBarAttribute attr =
            attribute as ProgressBarAttribute;
        
        // 只支持 float 和 int
        if (property.propertyType == SerializedPropertyType.Float)
        {
            // 绘制进度条
            float value = property.floatValue;
            float normalized = Mathf.InverseLerp(
                attr.min, attr.max, value);
            
            // 标签
            EditorGUI.LabelField(
                new Rect(position.x, position.y,
                    position.width * 0.3f, position.height),
                attr.label);
            
            // 进度条
            EditorGUI.ProgressBar(
                new Rect(position.x + position.width * 0.3f,
                    position.y,
                    position.width * 0.5f, position.height),
                normalized,
                $"{value:F2} / {attr.max:F2}");
            
            // 滑块
            Rect sliderRect = new Rect(
                position.x + position.width * 0.85f,
                position.y,
                position.width * 0.15f, position.height);
            
            // 使用 EditorGUI 的属性处理（支持 Undo）
            property.floatValue = EditorGUI.Slider(
                sliderRect, property.floatValue,
                attr.min, attr.max);
            
            // 因为我们已经手动绘制了，要确保 auto 绘制不覆盖
        }
        else
        {
            // 不支持的类型，回退到默认绘制
            EditorGUI.PropertyField(position, property, label);
        }
    }
    
    // ─── 属性高度覆盖 ───
    public override float GetPropertyHeight(
        SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}

// ─── 使用示例 ───

public class PlayerStats : MonoBehaviour
{
    [ProgressBar("Health", 0, 100)]
    public float health = 100;
    
    [ProgressBar("Mana", 0, 100)]
    public float mana = 50;
    
    [ProgressBar("Experience", 0, 1000)]
    public float exp = 250;
}
```

### 2.2 DecoratorDrawer

```csharp
// ─── DecoratorDrawer：在属性前后加装饰 ───
// 不需要关联 SerializedProperty

public class HeaderAttribute : PropertyAttribute
{
    public string text;
    public HeaderColor color;
    
    public enum HeaderColor { Green, Blue, Orange }
    
    public HeaderAttribute(string text,
        HeaderColor color = HeaderColor.Green)
    {
        this.text = text;
        this.color = color;
    }
}

[CustomPropertyDrawer(typeof(HeaderAttribute))]
public class HeaderDecorator : DecoratorDrawer
{
    HeaderAttribute attr => attribute as HeaderAttribute;
    
    public override void OnGUI(Rect position)
    {
        // 选择颜色
        Color color = Color.green;
        switch (attr.color)
        {
            case HeaderAttribute.HeaderColor.Blue:
                color = Color.cyan; break;
            case HeaderAttribute.HeaderColor.Orange:
                color = new Color(1f, 0.6f, 0f); break;
            default: color = Color.green; break;
        }
        
        // 绘制分隔线
        Rect lineRect = new Rect(
            position.x, position.y,
            position.width, 1);
        EditorGUI.DrawRect(lineRect, color);
        
        // 绘制标签
        Rect labelRect = new Rect(
            position.x, position.y + 4,
            position.width,
            position.height - 4);
        EditorGUI.LabelField(labelRect, attr.text,
            new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color }
            });
    }
    
    public override float GetHeight()
    {
        return 24;  // 装饰器的高度
    }
}

// ─── 使用 ───
public class WeaponConfig : MonoBehaviour
{
    [Header("基础属性", HeaderAttribute.HeaderColor.Green)]
    public float damage = 10;
    public float fireRate = 0.5f;
    
    [Header("视觉设置", HeaderAttribute.HeaderColor.Blue)]
    public Color bulletColor = Color.white;
    public GameObject muzzlePrefab;
}
```

### 2.3 复杂 Inspector 布局

```csharp
// ─── 整个组件的自定义 Inspector ───

[CustomEditor(typeof(WeaponConfig))]
public class WeaponConfigInspector : Editor
{
    private SerializedProperty damageProp;
    private SerializedProperty fireRateProp;
    private SerializedProperty bulletColorProp;
    private SerializedProperty muzzlePrefabProp;
    
    private WeaponConfig targetWeapon;
    private bool showAdvanced = false;
    
    void OnEnable()
    {
        targetWeapon = target as WeaponConfig;
        
        // 缓存 SerializedProperty（避免每帧查找）
        damageProp = serializedObject.FindProperty("damage");
        fireRateProp = serializedObject.FindProperty("fireRate");
        bulletColorProp = serializedObject.FindProperty("bulletColor");
        muzzlePrefabProp = serializedObject.FindProperty("muzzlePrefab");
    }
    
    public override void OnInspectorGUI()
    {
        // 必须在开头调用
        serializedObject.Update();
        
        // ─── 自定义布局 ───
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Weapon Config",
            EditorStyles.boldLabel);
        
        // 第一行：伤害 + 射速 并排
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(damageProp, GUIContent.none,
            GUILayout.MinWidth(80));
        EditorGUILayout.PropertyField(fireRateProp, GUIContent.none,
            GUILayout.MinWidth(80));
        EditorGUILayout.EndHorizontal();
        
        // DPS 实时计算
        float dps = damageProp.floatValue / fireRateProp.floatValue;
        EditorGUILayout.LabelField($"DPS: {dps:F2}");
        
        // ─── 颜色选择 ───
        EditorGUILayout.PropertyField(bulletColorProp);
        
        // ─── 折叠高级选项 ───
        showAdvanced = EditorGUILayout.Foldout(
            showAdvanced, "Advanced Settings");
        
        if (showAdvanced)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(muzzlePrefabProp);
            
            // 添加自定义按钮
            if (GUILayout.Button("Apply to All Weapons"))
            {
                // 批量应用
                ApplyToAll();
            }
            EditorGUI.indentLevel--;
        }
        
        // ─── 预览区 ───
        EditorGUILayout.Space();
        Rect previewRect = EditorGUILayout.GetControlRect(
            false, 80);
        EditorGUI.DrawRect(previewRect, bulletColorProp.colorValue);
        
        // 必须在结尾调用
        serializedObject.ApplyModifiedProperties();
    }
    
    void ApplyToAll()
    {
        // 找到场景中所有 WeaponConfig，应用当前值
        WeaponConfig[] allWeapons = FindObjectsOfType<WeaponConfig>();
        foreach (var w in allWeapons)
        {
            if (w != targetWeapon)
            {
                Undo.RecordObject(w, "Apply Weapon Config");
                w.damage = targetWeapon.damage;
                w.fireRate = targetWeapon.fireRate;
            }
        }
    }
}
```

---

## 3. ScriptableObject 设置系统

### 3.1 编辑器设置提供模式

```csharp
// ─── 基于 ScriptableObject 的项目设置 ───
// 替代 PlayerPrefs 或 EditorPrefs

using UnityEngine;
using UnityEditor;

// 创建设置资源
[CreateAssetMenu(
    fileName = "ProjectSettings",
    menuName = "Settings/Project Settings",
    order = 100)]
public class ProjectSettings : ScriptableObject
{
    [Header("Game Settings")]
    public string gameVersion = "1.0.0";
    public string bundleIdentifier = "com.mygame.app";
    
    [Header("CDN Config")]
    public string cdnBaseURL = "https://cdn.mygame.com/";
    
    [Header("Build Settings")]
    public bool enableCodeStripping = true;
    public bool enableProfilingInBuild = false;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public LogLevel logLevel = LogLevel.Warning;
    
    public enum LogLevel { Debug, Info, Warning, Error }
    
    // ─── 单例访问 ───
    private static ProjectSettings instance;
    public static ProjectSettings Instance
    {
        get
        {
            if (instance == null)
            {
                // 从 Resources 加载或创建默认
                instance = Resources.Load<ProjectSettings>(
                    "ProjectSettings");
                
                if (instance == null)
                {
                    Debug.LogError(
                        "ProjectSettings not found! " +
                        "Create one in Resources folder.");
                }
            }
            return instance;
        }
    }
}

// ─── 在 Project Settings 中显示 ───
// SettingsProvider 把设置集成到 Edit → Project Settings

public class ProjectSettingsProvider : SettingsProvider
{
    private SerializedObject settingsSerialized;
    
    public ProjectSettingsProvider(
        string path, SettingsScope scope = SettingsScope.Project)
        : base(path, scope) { }
    
    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        return new ProjectSettingsProvider(
            "Project/My Game Settings");
    }
    
    public override void OnGUI(string searchContext)
    {
        if (settingsSerialized == null)
        {
            settingsSerialized = new SerializedObject(
                ProjectSettings.Instance);
        }
        
        settingsSerialized.Update();
        
        EditorGUILayout.LabelField("Game Settings",
            EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 绘制所有 SerializedProperty
        var prop = settingsSerialized.GetIterator();
        prop.NextVisible(true);
        while (prop.NextVisible(false))
        {
            EditorGUILayout.PropertyField(prop);
        }
        
        settingsSerialized.ApplyModifiedProperties();
        
        // 保存按钮
        if (GUILayout.Button("Save Settings",
            GUILayout.Height(30)))
        {
            EditorUtility.SetDirty(ProjectSettings.Instance);
            AssetDatabase.SaveAssets();
        }
    }
}
```

### 3.2 编辑器数据缓存

```csharp
// ─── EditorPrefs 缓存工具 ───
// 在 ScriptableObject 之外保存编辑器状态

public static class EditorCache
{
    // ─── 通用键值模式 ───
    
    public static void SetFloat(string key, float value)
    {
        EditorPrefs.SetFloat($"MyEditor_{key}", value);
    }
    
    public static float GetFloat(string key, float defaultValue = 0)
    {
        return EditorPrefs.GetFloat($"MyEditor_{key}", defaultValue);
    }
    
    // ─── 窗口布局持久化 ───
    
    public static void SaveWindowLayout(string windowName, Rect rect)
    {
        EditorPrefs.SetFloat($"{windowName}_x", rect.x);
        EditorPrefs.SetFloat($"{windowName}_y", rect.y);
        EditorPrefs.SetFloat($"{windowName}_w", rect.width);
        EditorPrefs.SetFloat($"{windowName}_h", rect.height);
    }
    
    public static Rect LoadWindowLayout(string windowName,
        Rect defaultRect)
    {
        return new Rect(
            EditorPrefs.GetFloat($"{windowName}_x", defaultRect.x),
            EditorPrefs.GetFloat($"{windowName}_y", defaultRect.y),
            EditorPrefs.GetFloat($"{windowName}_w", defaultRect.width),
            EditorPrefs.GetFloat($"{windowName}_h", defaultRect.height)
        );
    }
}
```

---

## 4. 编辑器性能分析

### 4.1 诊断慢操作

```csharp
// ─── 用 Profiler 分析编辑器代码 ───

using UnityEditor;
using UnityEngine.Profiling;

public static class EditorProfilerTools
{
    [MenuItem("Tools/Profiler/Start Editor Profiling")]
    static void StartProfiling()
    {
        // 开启编辑器 Profiler
        Profiler.enabled = true;
        
        // Window → Analysis → Profiler
        // 在 Profiler 窗口中可以看到编辑器代码的 CPU 占用
        
        Debug.Log("编辑器 Profiling 已开启");
    }
    
    [MenuItem("Tools/Profiler/Profile Selected Assets")]
    static void ProfileSelectedAssets()
    {
        // 对选中的资源执行压力测试
        var selected = Selection.objects;
        
        Profiler.BeginSample("AssetImportTest");
        
        foreach (var obj in selected)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            
            Profiler.BeginSample($"Reimport: {path}");
            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceUpdate);
            Profiler.EndSample();
        }
        
        Profiler.EndSample();
        
        Debug.Log("资源导入分析完成，查看 Profiler 结果");
    }
}
```

### 4.2 编辑器响应性监控

```csharp
// ─── 检测编辑器卡顿 ───
// 当某个操作耗时过长时报警

[InitializeOnLoad]
public static class EditorLagDetector
{
    private static double lastEditorUpdateTime;
    private static double maxFrameTime = 0.1;  // 100ms 阈值
    
    static EditorLagDetector()
    {
        // 订阅 EditorApplication.update
        EditorApplication.update += OnEditorUpdate;
    }
    
    static void OnEditorUpdate()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        double deltaTime = currentTime - lastEditorUpdateTime;
        
        if (lastEditorUpdateTime > 0 && deltaTime > maxFrameTime)
        {
            Debug.LogWarning(
                $"编辑器卡顿检测：上次更新距离 {deltaTime*1000:F0}ms，" +
                $"超过阈值 {maxFrameTime*1000:F0}ms\n" +
                "可能是某个资源导入或脚本编译导致");
        }
        
        lastEditorUpdateTime = currentTime;
    }
}
```

---

## 5. 自定义 Build Pipeline

### 5.1 BuildPlayerProcessor

```csharp
// ─── 构建处理管线 ───
// 在构建流程中注入自定义步骤

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Diagnostics;

public class CustomBuildProcessor :
    IPreprocessBuildWithReport,  // 构建前处理
    IPostprocessBuildWithReport  // 构建后处理
{
    public int callbackOrder => 0;  // 执行顺序（越小越先执行）
    
    private Stopwatch buildTimer;
    private string buildStartTime;
    
    // ─── 构建前 ───
    public void OnPreprocessBuild(BuildReport report)
    {
        buildTimer = Stopwatch.StartNew();
        buildStartTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        UnityEngine.Debug.Log(
            $"🛠 开始构建：{report.summary.platform}，" +
            $"输出：{report.summary.outputPath}");
        
        // 1. 版本号递增
        UpdateVersionNumber();
        
        // 2. 确保 Addressables 已构建
        BuildAddressablesIfNeeded();
        
        // 3. 清理旧的构建产物
        CleanOldBuilds(report.summary.outputPath);
        
        // 4. 验证场景设置
        ValidateScenes();
    }
    
    // ─── 构建后 ───
    public void OnPostprocessBuild(BuildReport report)
    {
        buildTimer.Stop();
        
        long buildTimeMs = buildTimer.ElapsedMilliseconds;
        long buildSizeMB = report.summary.totalSize / 1024 / 1024;
        
        // 输出构建报告
        UnityEngine.Debug.Log(
            $"✅ 构建完成！\n" +
            $"  平台：{report.summary.platform}\n" +
            $"  时长：{buildTimeMs / 1000}s\n" +
            $"  大小：{buildSizeMB}MB\n" +
            $"  输出：{report.summary.outputPath}");
        
        // 构建结果摘要
        var result = report.summary.result;
        if (result == BuildResult.Succeeded)
        {
            // 成功：发送通知
            EditorUtility.DisplayDialog(
                "Build Complete",
                $"Build completed in {buildTimeMs/1000}s\n" +
                $"Size: {buildSizeMB}MB",
                "OK");
        }
        else
        {
            // 失败：记录错误
            UnityEngine.Debug.LogError("Build failed!");
            
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error ||
                        msg.type == LogType.Exception)
                    {
                        UnityEngine.Debug.LogError(
                            $"[{step.name}] {msg.content}");
                    }
                }
            }
        }
    }
    
    void UpdateVersionNumber()
    {
        // 读取 PlayerSettings 的版本号，递增构建号
        PlayerSettings.bundleVersion = ProjectSettings.Instance.gameVersion;
        PlayerSettings.Android.bundleVersionCode++;
        PlayerSettings.iOS.buildNumber =
            PlayerSettings.Android.bundleVersionCode.ToString();
    }
    
    void BuildAddressablesIfNeeded()
    {
        // 检查 Addressables 是否需要重新构建
        // AddressableAssetSettings.BuildPlayerContent();
    }
    
    void CleanOldBuilds(string outputPath)
    {
        // 删除旧的构建文件
        // System.IO.Directory.Delete(outputPath, true);
    }
    
    void ValidateScenes()
    {
        // 检查 Build Settings 中的场景列表
        var scenes = EditorBuildSettings.scenes;
        foreach (var scene in scenes)
        {
            if (!scene.enabled)
            {
                UnityEngine.Debug.LogWarning(
                    $"场景未启用：{scene.path}");
            }
        }
    }
}

// ─── 一键构建菜单 ───

public static class BuildMenu
{
    [MenuItem("Build/Build Development")]
    static void BuildDevelopment()
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettingsScene.GetActiveSceneList(
                EditorBuildSettings.scenes),
            locationPathName = "Build/Development/Game",
            target = EditorUserBuildSettings.activeBuildTarget,
            options = BuildOptions.Development |
                      BuildOptions.AllowDebugging
        };
        
        BuildPipeline.BuildPlayer(options);
    }
    
    [MenuItem("Build/Build Release")]
    static void BuildRelease()
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettingsScene.GetActiveSceneList(
                EditorBuildSettings.scenes),
            locationPathName = "Build/Release/Game",
            target = EditorUserBuildSettings.activeBuildTarget,
            options = BuildOptions.None
        };
        
        BuildPipeline.BuildPlayer(options);
    }
}
```

### 5.2 构建验证系统

```csharp
// ─── 构建前的自动化验证 ───

public static class BuildValidator
{
    public static bool ValidateBeforeBuild()
    {
        bool isValid = true;
        
        // 1. 检查场景是否都在 Build Settings 中
        var allScenes = AssetDatabase.FindAssets("t:Scene");
        var buildScenes = EditorBuildSettings.scenes
            .Select(s => s.path).ToHashSet();
        
        foreach (string guid in allScenes)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!buildScenes.Contains(path))
            {
                Debug.LogWarning($"场景未添加到 Build Settings：{path}");
            }
        }
        
        // 2. 检查是否有未保存的 Prefab
        // (通过 Checkout 检查)
        
        // 3. 检查 Addressables 是否包含所有依赖
        // (通过 Addressables 分析器)
        
        // 4. 检查纹理格式是否与目标平台匹配
        // (检查 Android ASTC vs iOS PVRTC)
        
        if (!isValid)
        {
            EditorUtility.DisplayDialog(
                "Build Validation Failed",
                "请修复以上错误后重试",
                "OK");
        }
        
        return isValid;
    }
}
```

---

## 6. Editor Coroutines

### 6.1 为什么需要 Editor Coroutines？

```csharp
// ─── Editor 协程解决的问题 ───
/*
 编辑器中的一些操作需要异步完成：
 - 批量处理大量资源
 - 调用外部工具（等待结果）
 - 逐帧更新的编辑器 UI
 - 长时间运行的编辑器任务

 普通 Update 只能在运行时用
 Editor Coroutines 在编辑器中提供类似功能
*/

using UnityEditor;
using UnityEngine;
using System.Collections;
using Unity.EditorCoroutines.Editor;

public class EditorCoroutineTools : EditorWindow
{
    [MenuItem("Tools/Batch Asset Processor")]
    static void Open()
    {
        GetWindow<EditorCoroutineTools>("Batch Processor");
    }
    
    bool isProcessing = false;
    float progress = 0;
    string currentFile = "";
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Batch Asset Processor",
            EditorStyles.boldLabel);
        
        if (!isProcessing)
        {
            if (GUILayout.Button("Start Processing All Textures",
                GUILayout.Height(30)))
            {
                // 开始编辑协程
                EditorCoroutineUtility.StartCoroutine(
                    ProcessAllTextures(), this);
            }
        }
        else
        {
            // 显示进度
            Rect progressRect = EditorGUILayout.GetControlRect(
                false, 20);
            EditorGUI.ProgressBar(progressRect,
                progress, currentFile);
            
            EditorGUILayout.LabelField(
                $"Progress: {progress:P0}");
            
            if (GUILayout.Button("Cancel"))
            {
                EditorCoroutineUtility.StopAllCoroutines(this);
                isProcessing = false;
            }
        }
    }
    
    IEnumerator ProcessAllTextures()
    {
        isProcessing = true;
        
        // 查找所有纹理
        string[] textureGuids = AssetDatabase.FindAssets(
            "t:Texture2D");
        
        int total = textureGuids.Length;
        int processed = 0;
        
        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            currentFile = path;
            progress = (float)processed / total;
            
            // 处理纹理（设置压缩格式）
            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            
            processed++;
            
            // 每处理 10 个资源让出控制权（编辑器不卡）
            if (processed % 10 == 0)
            {
                yield return null;  // ← 关键：让编辑器更新 UI
                Repaint();
            }
        }
        
        isProcessing = false;
        progress = 1;
        Repaint();
        
        Debug.Log($"批量处理完成！共处理 {total} 个纹理");
    }
}
```

---

## 7. Odin Inspector 常见模式

### 7.1 在不依赖 Odin 的情况下实现类似功能

```csharp
// ─── Odin 的常用特性及其自制实现 ───

// 1. [Title] —— 分节标题
public class TitleAttribute : PropertyAttribute
{
    public string title;
    public TitleAttribute(string title) => this.title = title;
}

[CustomPropertyDrawer(typeof(TitleAttribute))]
public class TitleDrawer : DecoratorDrawer
{
    TitleAttribute attr => attribute as TitleAttribute;
    
    public override void OnGUI(Rect position)
    {
        EditorGUI.LabelField(position, attr.title,
            EditorStyles.boldLabel);
    }
    
    public override float GetHeight() => 20;
}

// 2. [LabelWidth] —— 标签宽度
public class LabelWidthAttribute : PropertyAttribute
{
    public float width;
    public LabelWidthAttribute(float width) => this.width = width;
}

[CustomPropertyDrawer(typeof(LabelWidthAttribute))]
public class LabelWidthDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position,
        SerializedProperty property, GUIContent label)
    {
        float original = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth =
            (attribute as LabelWidthAttribute).width;
        EditorGUI.PropertyField(position, property, label);
        EditorGUIUtility.labelWidth = original;
    }
}

// 3. [ReadOnly] —— 只读显示
public class ReadOnlyAttribute : PropertyAttribute { }

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position,
        SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;  // 灰化
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}

// 4. [InlineEditor] —— 内联显示子对象
// 实现较复杂，通常在 CustomEditor 中做
```

### 7.2 枚举的更好显示

```csharp
// ─── 用 Flags 和自定义枚举显示 ───

[System.Flags]
public enum BuffType
{
    None = 0,
    Speed = 1 << 0,      // 1
    Strength = 1 << 1,    // 2
    Defense = 1 << 2,     // 4
    Invisibility = 1 << 3 // 8
}

// ─── 自定义枚举绘制器 ───
// 显示为可点击按钮网格，而不是下拉列表

[CustomPropertyDrawer(typeof(BuffType))]
public class BuffTypeDrawer : PropertyDrawer
{
    private string[] options = { "Spd", "Str", "Def", "Inv" };
    
    public override void OnGUI(Rect position,
        SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        position = EditorGUI.PrefixLabel(
            position, GUIUtility.GetControlID(FocusType.Passive), label);
        
        int value = property.intValue;
        int buttonCount = options.Length;
        float buttonWidth = position.width / buttonCount;
        
        for (int i = 0; i < buttonCount; i++)
        {
            Rect btnRect = new Rect(
                position.x + i * buttonWidth,
                position.y,
                buttonWidth - 2,
                position.height);
            
            int flag = 1 << i;
            bool isSet = (value & flag) != 0;
            
            // 切换状态
            EditorGUI.BeginChangeCheck();
            bool newValue = GUI.Toggle(btnRect, isSet,
                options[i], GUI.skin.button);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (newValue)
                    property.intValue |= flag;
                else
                    property.intValue &= ~flag;
            }
        }
        
        EditorGUI.EndProperty();
    }
}
```

---

## C++/Raylib 对照总结

| 概念 | C++ / 传统工具 | Unity Editor 扩展 |
|------|-------------|------------------|
| UI 框架 | Qt / wxWidgets / Dear ImGui | IMGUI（简易）/ UI Toolkit（现代）|
| 项目设置 | JSON / XML 配置文件 | ScriptableObject + SettingsProvider |
| 构建脚本 | Makefile / CMake | BuildPlayerProcessor + CI 集成 |
| 异步操作 | 线程 / std::async | Editor Coroutines |
| 属性系统 | 反射 / Qt Property System | PropertyDrawer + SerializedProperty |
| 调试 | GDB / IDE | Editor Profiler + Frame Debugger |
| — | 无 | Undo 系统（编辑器全局 Ctrl+Z）|
| — | 无 | AssetPostprocessor 导入自动化 |
| — | 无 | Odin Inspector 第三方插件生态 |

## 停靠点

> UI Toolkit 是 Unity 编辑器的未来——采用 Flexbox 布局 + CSS 样式系统，适合复杂编辑器界面。
> 自定义 PropertyDrawer 美化 Inspector 中的属性显示——用 [CustomPropertyDrawer] + OnGUI。
> DecoratorDrawer 在属性前后添加装饰性元素（分隔线、标题）。
> SettingsProvider 将设置集成到 Project Settings 窗口——统一管理项目配置。
> BuildPlayerProcessor 在构建流程中注入自定义步骤——版本号递增、验证场景、清理旧构建。
> Editor Coroutines 在编辑器中实现异步操作——批量资源处理时保持编辑器响应。
> Odin 的功能大部分可以用 PropertyDrawer + DecoratorDrawer + CustomEditor 实现。
> **编辑器开发的原则：重复超过 3 次的操作就做成工具。**
