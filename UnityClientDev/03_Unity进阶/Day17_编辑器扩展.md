# Day 17：编辑器扩展 — 从 IMGUI 到 UI Toolkit

## 0. 为什么需要编辑器扩展？

Unity 编辑器是一个**可扩展的开发工具**。你可以为它写插件，把重复操作自动化。

```
不用扩展：
每次设计关卡 → 手动拖入 50 个敌人 → 手动设置每个的属性 → 耗时 20 分钟

用扩展：
点击 "生成关卡" 按钮 → 一键生成 → 耗时 2 秒
```

**原则：任何重复操作超过 3 次，就写个编辑器工具。**

---

## 1. Unity Editor 的架构

### Editor 运行环境

```
Unity Editor 使用一个独立的 .NET/Mono 运行时（不同于游戏中的运行时）

编辑器和游戏的区别：
                    Editor                 Runtime
运行时：       Editor Mono/Net          Mono/IL2CPP
代码：        #if UNITY_EDITOR          游戏代码
API：         Editor命名空间可用         Editor API 不可用
功能：        编辑、预览、调试           运行游戏
```

### 编辑器扩展的三种方式

```
1. MenuItem — 菜单和快捷键
   最简单，添加菜单项到顶部菜单栏

2. EditorWindow — 自定义窗口
   浮动或停靠面板，包含各种控件

3. CustomEditor — 自定义 Inspector
   改变某个组件在 Inspector 中的显示方式
```

---

## 2. MenuItem——菜单 + 快捷键

### 基本使用

```csharp
using UnityEditor;

public class ToolsMenu
{
    [MenuItem("Tools/Clear PlayerPrefs")]
    static void ClearPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("PlayerPrefs cleared!");
    }
}
```

### 快捷键

```csharp
[MenuItem("Tools/Clear PlayerPrefs %#c")]
// 特殊符号：
// % = Ctrl (macOS: Cmd)
// # = Shift
// & = Alt
// _ = 没有修饰键
// 所以 %#c = Ctrl+Shift+C

[MenuItem("Tools/Do Something _F5")]
// 不用修饰键，按 F5 触发
```

### 右键菜单

```csharp
[MenuItem("GameObject/My Tools/Reset Position", false, 0)]
// 在 Hierarchy 和 Scene 视图中右键 → My Tools → Reset Position

static void ResetPosition(MenuCommand cmd)
{
    // MenuCommand 可以获取到被右键点击的对象
    GameObject go = cmd.context as GameObject;
    if (go != null)
    {
        Undo.RecordObject(go.transform, "Reset Position");
        go.transform.position = Vector3.zero;
    }
}

// 第二个参数 priority = 10（菜单中的位置顺序）
// false 表示是否验证（Validate）

[MenuItem("GameObject/My Tools/Reset Position", true)]
static bool ValidateResetPosition()
{
    // 验证函数：返回 false 时禁用菜单项
    return Selection.activeGameObject != null;
}
```

---

## 3. EditorWindow——自定义窗口

### 基本结构

```csharp
using UnityEditor;
using UnityEngine;

public class MyWindow : EditorWindow
{
    // 菜单项打开窗口
    [MenuItem("Tools/My Window")]
    static void Open()
    {
        // 创建或获取已存在的窗口
        MyWindow window = GetWindow<MyWindow>("My Window");
        window.Show();
    }

    // GUI 绘制——每帧调用
    void OnGUI()
    {
        // 你在这里绘制窗口内容
        GUILayout.Label("Hello from Editor!");
    }
}
```

### IMGUI 控件

```csharp
// IMGUI（Immediate Mode GUI）是 Unity 编辑器的传统 UI 系统
// 每帧绘制所有控件，通过返回值获取用户操作

void OnGUI()
{
    // ─── 布局控件 ───
    GUILayout.Label("Settings", EditorStyles.boldLabel);  // 标签
    GUILayout.Space(10);                                  // 间距

    // ─── 输入控件（返回值 = 用户输入的值） ───
    string text = EditorGUILayout.TextField("Name", text);
    int count = EditorGUILayout.IntField("Count", count);
    float value = EditorGUILayout.FloatField("Value", value);
    bool toggle = EditorGUILayout.Toggle("Enabled", toggle);

    // ─── 对象引用 ───
    GameObject prefab = EditorGUILayout.ObjectField(
        "Prefab", prefab, typeof(GameObject), false
    ) as GameObject;

    // ─── 滑块 ───
    float speed = EditorGUILayout.Slider("Speed", speed, 0f, 100f);

    // ─── 颜色 ───
    Color color = EditorGUILayout.ColorField("Color", color);

    // ─── 枚举 ───
    MyEnum option = (MyEnum)EditorGUILayout.EnumPopup("Option", option);

    // ─── 按钮 ───
    if (GUILayout.Button("Execute"))
    {
        // 执行操作
        ExecuteCommand();
    }
}
```

### 窗口的生命周期

```csharp
public class AdvancedWindow : EditorWindow
{
    void OnEnable()
    {
        // 窗口打开或脚本重编译时调用
        // 初始化数据
        // 订阅编辑器事件
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    void OnDisable()
    {
        // 窗口关闭时调用
        // 取消订阅
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    void OnDestroy()
    {
        // 窗口被销毁时
    }

    void OnFocus()
    {
        // 窗口获得焦点时
    }

    void OnLostFocus()
    {
        // 窗口失去焦点时
    }

    void OnGUI()
    {
        // 每帧绘制（Editor 循环的一部分）
    }

    void OnPlayModeChanged(PlayModeStateChange state)
    {
        // 播放模式状态变化时
        // 进入编辑模式、进入播放模式、退出播放模式
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                break;
            case PlayModeStateChange.EnteredPlayMode:
                break;
        }
    }
}
```

### 窗口数据持久化

```csharp
public class PersistentWindow : EditorWindow
{
    private string data;

    void OnEnable()
    {
        // 从 EditorPrefs 读取上次保存的数据
        data = EditorPrefs.GetString("MyWindow_Data", "default");
    }

    void OnDisable()
    {
        // 保存数据
        EditorPrefs.SetString("MyWindow_Data", data);
    }

    // 或者用 ScriptableObject 做更复杂的持久化
    private MySettings settings;

    void OnEnable()
    {
        // 从 Assets 目录加载或创建 Settings 文件
        string path = "Assets/Editor/MySettings.asset";
        settings = AssetDatabase.LoadAssetAtPath<MySettings>(path);
        if (settings == null)
        {
            settings = CreateInstance<MySettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
        }
    }
}
```

---

## 4. CustomEditor——自定义 Inspector

### 基本使用

```csharp
// 要自定义的组件
public class Player : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f;
    public string playerName = "Player";
    public int level = 1;
}

// 自定义 Inspector
[CustomEditor(typeof(Player))]
public class PlayerInspector : Editor
{
    private Player player;

    void OnEnable()
    {
        player = (Player)target;  // target 就是被编辑的对象
    }

    public override void OnInspectorGUI()
    {
        // 绘制默认 Inspector
        DrawDefaultInspector();

        GUILayout.Space(10);

        // 添加自定义控件
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);

        // 显示血量条
        Rect progressRect = EditorGUILayout.GetControlRect();
        EditorGUI.ProgressBar(progressRect,
            player.health / player.maxHealth,
            $"{player.health} / {player.maxHealth}"
        );

        // 按钮
        if (GUILayout.Button("Reset Health"))
        {
            Undo.RecordObject(player, "Reset Health");
            player.health = player.maxHealth;
            EditorUtility.SetDirty(player);  // 标记为已修改
        }

        // 标记场景为 dirty（需要保存）
        if (GUI.changed)
        {
            EditorUtility.SetDirty(player);
            SceneView.RepaintAll();  // 刷新 Scene 视图
        }
    }
}
```

---

## 5. UI Toolkit——现代编辑器 UI

### 什么是 UI Toolkit？

```
UI Toolkit = Unity 的现代 UI 系统（取代 IMGUI）

特点：
- XML（UXML）定义布局 → 类似 HTML
- CSS（USS）定义样式 → 类似网页设计
- C# 处理逻辑 → 分离结构和行为
- 支持数据绑定

对比 IMGUI：
IMGUI              | UI Toolkit
每帧绘制所有控件     | 声明式布局
代码和 UI 混合      | UI 和逻辑分离
难以自定义样式      | 强大的 CSS 样式系统
适合小窗口          | 适合复杂界面
```

### UXML——布局

```xml
<!-- MyWindow.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:ue="UnityEditor.UIElements">
    <ui:Label text="Player Settings" class="header"/>

    <ui:VisualElement class="section">
        <ui:Label text="Basic Info"/>
        <ui:TextField label="Name" binding-path="playerName"/>
        <ui:IntegerField label="Level" binding-path="level"/>
    </ui:VisualElement>

    <ui:VisualElement class="section">
        <ui:Label text="Health"/>
        <ui:Slider label="Health" binding-path="health"
                   low-value="0" high-value="100"/>
    </ui:VisualElement>

    <ui:Button text="Reset" clickable="clickable"/>
</ui:UXML>
```

### USS——样式

```css
/* MyWindow.uss */
.header {
    font-size: 18px;
    -unity-font-style: bold;
    color: rgb(72, 150, 255);
    margin-bottom: 10px;
}

.section {
    background-color: rgba(255, 255, 255, 0.05);
    padding: 10px;
    margin-bottom: 8px;
}

Button {
    background-color: rgb(72, 150, 255);
    color: white;
    padding: 5px 10px;
}

Button:hover {
    background-color: rgb(100, 180, 255);
}
```

### C#——逻辑

```csharp
using UnityEditor;
using UnityEngine.UIElements;

public class UIToolkitWindow : EditorWindow
{
    [MenuItem("Tools/UI Toolkit Window")]
    static void Open()
    {
        GetWindow<UIToolkitWindow>("UI Toolkit");
    }

    void CreateGUI()
    {
        // 加载 UXML 布局
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Editor/MyWindow.uxml"
        );
        visualTree.CloneTree(rootVisualElement);

        // 加载 USS 样式
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Editor/MyWindow.uss"
        );
        rootVisualElement.styleSheets.Add(styleSheet);

        // 事件绑定
        rootVisualElement.Q<Button>("resetButton").clicked += () =>
        {
            Debug.Log("Reset clicked!");
        };
    }
}
```

---

## 6. AssetPostprocessor——资源导入自动化

```csharp
// 在资源导入时自动执行操作

public class MyAssetPostprocessor : AssetPostprocessor
{
    // 纹理导入时调用
    void OnPreprocessTexture()
    {
        TextureImporter importer = assetImporter as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100;
        importer.mipmapEnabled = false;  // UI 图片不需要 mipmap
    }

    // 模型导入时调用
    void OnPreprocessModel()
    {
        ModelImporter importer = assetImporter as ModelImporter;
        importer.importMaterials = false;  // 不导入材质
        importer.animationType = ModelImporterAnimationType.Human;
    }

    // 所有资源导入完成后调用
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (path.EndsWith(".prefab"))
            {
                Debug.Log($"Prefab imported: {path}");
            }
        }
    }
}
```

---

## 练习：一键关卡生成工具

```csharp
using UnityEditor;
using UnityEngine;

public class LevelDesignerWindow : EditorWindow
{
    [Header("Settings")]
    private GameObject enemyPrefab;
    private int enemyCount = 10;
    private float spawnRadius = 10f;
    private float minHeight = 0f;
    private float maxHeight = 5f;

    [MenuItem("Tools/Level Designer")]
    static void Open()
    {
        GetWindow<LevelDesignerWindow>("Level Designer");
    }

    void OnGUI()
    {
        GUILayout.Label("Spawn Settings", EditorStyles.boldLabel);

        enemyPrefab = EditorGUILayout.ObjectField(
            "Enemy Prefab", enemyPrefab, typeof(GameObject), false
        ) as GameObject;

        enemyCount = EditorGUILayout.IntField("Count", enemyCount);
        spawnRadius = EditorGUILayout.FloatField("Radius", spawnRadius);

        EditorGUILayout.MinMaxSlider(
            "Height Range", ref minHeight, ref maxHeight, 0f, 10f);

        GUI.enabled = enemyPrefab != null;
        if (GUILayout.Button("Spawn Enemies", GUILayout.Height(30)))
        {
            SpawnEnemies();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Clear All Enemies"))
        {
            ClearEnemies();
        }
    }

    void SpawnEnemies()
    {
        if (enemyPrefab == null) return;

        GameObject parent = new GameObject("Enemies");
        Undo.RegisterCreatedObjectUndo(parent, "Create Enemies");

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = new Vector3(
                circle.x,
                Random.Range(minHeight, maxHeight),
                circle.y
            );

            GameObject go = PrefabUtility.InstantiatePrefab(
                enemyPrefab, parent.transform
            ) as GameObject;
            go.transform.position = pos;

            Undo.RegisterCreatedObjectUndo(go, "Spawn Enemy");
        }
    }

    void ClearEnemies()
    {
        GameObject parent = GameObject.Find("Enemies");
        if (parent != null)
        {
            Undo.DestroyObjectImmediate(parent);
        }
    }
}
```

---

---

## C++/Raylib 对照总结

> Raylib 没有编辑器扩展的概念——Unity 的编辑器本身就是一个可扩展的开发工具。

| 概念 | Raylib / C++ | Unity 编辑器 |
|------|-------------|-------------|
| 开发工具 | 外部编辑器 + 命令行 | Unity Editor（内建扩展系统）|
| 自动化 | 手写脚本 / Makefile | MenuItem / EditorWindow |
| UI 定制 | 无（独立窗口工具） | CustomEditor / UI Toolkit |
| 资源管理 | 手动组织文件 | AssetPostprocessor 自动化 |
| — | 无 | IMGUI（即时模式 GUI）|
| — | 无 | UI Toolkit（CSS 样式系统）|

## 停靠点

> MenuItem 添加菜单+快捷键，EditorWindow 创建自定义窗口，CustomEditor 修改 Inspector。
> IMGUI 是传统编辑器 UI（OnGUI 中每帧绘制），UI Toolkit 是现代方案（UXML + USS 分离）。
> Undo 系统让你的工具支持 Ctrl+Z——必须用 Undo.RecordObject。
> **重复操作超过 3 次，就写个编辑器工具。**

