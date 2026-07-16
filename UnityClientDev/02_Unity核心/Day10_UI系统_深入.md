# Day 10：UI 系统 — 深入篇：UI Toolkit 架构、USS 样式系统与 UGUI 深度优化

## 0. 引言：两代 UI 系统

上一章我们学习了 **UGUI**（uGUI = Unity 内置的 GameObject-based UI）。但 Unity 正在全力推进新一代 UI 系统——**UI Toolkit**（原名 UIElements）。

理解两者的关系很重要：
- **UGUI** = 2005 年架构，基于 GameObject/Component，适合运行时 UI
- **UI Toolkit** = 2020+ 架构，基于 Web 技术（CSS-like），适合编辑器工具 + 运行时 UI

在 Raylib/C++ 中，UI 只是一系列绘制函数：
```cpp
// Raylib：每次刷新重新绘制整个 UI
DrawRectangle(100, 100, 200, 50, GRAY);
DrawText("Click", 150, 115, 20, BLACK);

// 按钮的状态需要手动跟踪
if (CheckCollisionPointRec(mousePos, buttonRect)) { /* hover */ }
if (IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) { /* click */ }
```

Unity 的 UI 系统（无论 UGUI 还是 UI Toolkit）都是**保留式（Retained Mode）** UI——你声明布局和样式，引擎负责渲染和更新。

---

## 1. UI Toolkit 架构

### 系统层级

```
UI Toolkit 的完整架构：

VisualElement 树（你的 UI 结构）
    ├── 元素层级（类似于 HTML DOM）
    ├── USS 样式（类似于 CSS）
    ├── 事件系统（类似于 DOM 事件）
    └── 布局引擎（Flexbox-based）

↓

Panel（面板——管理一个 UI 文档）
    ├── 自动布局计算
    ├── 样式匹配与解析
    └── 事件分发

↓

Renderer（渲染器）
    ├── Mesh 生成
    ├── 纹理合并
    └── Draw Call 提交
```

### 与 UGUI 架构对比

```
UGUI 渲染流程：
GameObject → RectTransform → Graphic (Image/Text) → Mesh → Canvas → Renderer

UI Toolkit 渲染流程：
XML/UXML → VisualElement Tree → Style Resolution → Layout → Mesh → Renderer

关键区别：
- UGUI：每个 UI 元素是一个 GameObject（有 Transform、组件开销）
- UI Toolkit：UI 元素是轻量级 VisualElement（无 GameObject 开销）
- 1000 个 UGUI 元素 = 1000 个 GameObject（~50KB+ 每对象）
- 1000 个 UI Toolkit 元素 = 内存占用少 10-20 倍
```

### UI Document——运行时的入口

```csharp
using UnityEngine;
using UnityEngine.UIElements;

// UI Document 组件：将 UXML 文档挂接到场景中
// 相当于 UGUI 中的 Canvas

public class UITK_Demo : MonoBehaviour
{
    private UIDocument document;

    void Awake()
    {
        // 获取 UI Document 组件
        document = GetComponent<UIDocument>();
        
        // 获取根 VisualElement
        VisualElement root = document.rootVisualElement;
        
        // 查找子元素（类似 jQuery 选择器）
        Button myButton = root.Q<Button>("my-button");
        
        // 注册事件
        myButton.clicked += () => Debug.Log("UI Toolkit button clicked!");
        
        // 动态修改样式
        myButton.style.backgroundColor = new StyleColor(Color.blue);
        myButton.style.width = 200;
        myButton.style.height = 50;
    }
}
```

---

## 2. UXML——UI 的 XML 描述语言

### 基本语法

```xml
<!-- UXML 文件：描述 UI 结构 -->
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:uie="UnityEditor.UIElements">
    
    <!-- 类似 HTML 的层级 -->
    <ui:VisualElement class="main-container">
        
        <ui:Label text="Hello UI Toolkit" 
                  class="title-text" />
        
        <ui:VisualElement class="button-row">
            <ui:Button text="Click Me" 
                       name="primary-button" />
            <ui:Button text="Cancel" 
                       name="secondary-button" />
        </ui:VisualElement>
        
        <ui:Slider name="volume-slider" 
                   low-value="0" 
                   high-value="100" 
                   value="50" />
        
    </ui:VisualElement>
</ui:UXML>
```

### 与 UGUI 的对应关系

```
UXML 元素          → UGUI 对应物
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
VisualElement      → GameObject（空 UI 容器）
Label              → TextMeshProUGUI
Button             → Button + TextMeshProUGUI
Image              → Image
Slider             → Slider
ScrollView         → ScrollRect
TextField          → InputField
Toggle             → Toggle
ProgressBar        → Slider（只读）
MinMaxSlider       → 无内置
DropdownField      → Dropdown
ListView           → ScrollView + Content
```

---

## 3. USS——样式系统

### 基本语法

```css
/* style.uss 文件：类似 CSS */
.main-container {
    background-color: rgba(30, 30, 30, 255);
    padding: 10;
    flex-direction: column;
}

.title-text {
    font-size: 24px;
    -unity-font-style: bold;
    color: white;
    margin-bottom: 15;
    -unity-text-align: middle-center;
}

.button-row {
    flex-direction: row;
    justify-content: center;
    gap: 10;
}

#primary-button {
    background-color: rgb(0, 120, 200);
    color: white;
    border-radius: 4;
    width: 120;
    height: 35;
}

#primary-button:hover {
    background-color: rgb(0, 150, 255);
}

#primary-button:active {
    background-color: rgb(0, 80, 180);
}

/* USS 选择器类型 */
.class-name { }      /* 类选择器：class="class-name" */
#element-name { }    /* ID 选择器：name="element-name" */
VisualElement { }    /* 类型选择器：所有 VisualElement */
.parent > .child { } /* 子选择器 */
.class1.class2 { }   /* 多重类组合 */
```

### USS 变量

```css
/* 定义变量（在 :root 中） */
:root {
    --primary-color: rgb(0, 120, 200);
    --danger-color: rgb(200, 50, 50);
    --spacing-unit: 8;
}

/* 使用变量 */
.button {
    background-color: var(--primary-color);
    margin: var(--spacing-unit);
}

.danger-button {
    background-color: var(--danger-color);
}
```

### 从代码中动态修改样式

```csharp
public class USSModifier : MonoBehaviour
{
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        
        // 1. 通过类名切换
        root.AddToClassList("highlighted");
        root.RemoveFromClassList("highlighted");
        root.ToggleInClassList("hidden");
        
        // 2. 直接设置内联样式
        root.style.color = Color.red;
        root.style.opacity = 0.5f;
        root.style.display = DisplayStyle.None;  // 隐藏
        
        // 3. 加载外部 USS
        var styleSheet = Resources.Load<StyleSheet>("Styles/main");
        root.styleSheets.Add(styleSheet);
        
        // 4. 伪类（Pseudo-class）
        var button = root.Q<Button>();
        button.RegisterCallback<PointerEnterEvent>(e =>
        {
            button.AddToClassList("hover");
        });
        button.RegisterCallback<PointerLeaveEvent>(e =>
        {
            button.RemoveFromClassList("hover");
        });
    }
}
```

---

## 4. VisualElement 层级——自定义组件

### 创建自定义 VisualElement

```csharp
using UnityEngine;
using UnityEngine.UIElements;

// 自定义 UI 组件——继承 VisualElement
public class HealthBar : VisualElement
{
    // 创建新的 VisualElement 类（类似于 UGUI 中继承 Graphic）

    // 声明 USS 类名——用于样式
    public new class UxmlFactory : UxmlFactory<HealthBar> { }

    // USS 类名常量
    public static readonly string ussClassName = "health-bar";
    public static readonly string fillUssClassName = "health-bar__fill";
    public static readonly string labelUssClassName = "health-bar__label";

    // 内部元素
    private VisualElement fill;
    private Label label;

    // 属性
    private float _value = 1f;
    public float value
    {
        get => _value;
        set
        {
            _value = Mathf.Clamp01(value);
            // 更新填充宽度
            fill.style.width = Length.Percent(_value * 100);
            // 更新文字
            label.text = $"{(_value * 100):F0}%";
            // 颜色变化
            if (_value < 0.3f)
                fill.style.backgroundColor = new StyleColor(Color.red);
            else
                fill.style.backgroundColor = new StyleColor(Color.green);
        }
    }

    // 构造函数
    public HealthBar()
    {
        // 添加 USS 类
        AddToClassList(ussClassName);

        // 创建背景/边框
        style.flexDirection = FlexDirection.Row;
        style.height = 20;
        style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));

        // 创建填充条
        fill = new VisualElement();
        fill.AddToClassList(fillUssClassName);
        fill.style.width = Length.Percent(100);
        fill.style.height = Length.Percent(100);
        fill.style.backgroundColor = new StyleColor(Color.green);
        Add(fill);

        // 创建文字标签
        label = new Label("100%");
        label.AddToClassList(labelUssClassName);
        label.style.position = Position.Absolute;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.width = Length.Percent(100);
        label.style.height = Length.Percent(100);
        Add(label);
    }
}
```

### 在 UXML 中使用自定义组件

```xml
<ui:UXML ...>
    <ui:VisualElement class="player-hud">
        <!-- 使用自定义 HealthBar 组件 -->
        <HealthBar name="player-health" />
        
        <HealthBar name="player-energy" />
        
        <ui:Button text="Take Damage" 
                   name="damage-button" />
    </ui:VisualElement>
</ui:UXML>
```

### 从代码中操控自定义组件

```csharp
public class HealthController : MonoBehaviour
{
    private HealthBar healthBar;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        healthBar = root.Q<HealthBar>("player-health");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            // 减少血量
            healthBar.value -= 0.1f;
        }
    }
}
```

---

## 5. UI Builder 工作流

```
Window → UI Toolkit → UI Builder

UI Builder = UI Toolkit 的"场景编辑器"：
- 可视化编辑 UXML 结构
- 实时 USS 样式预览
- 拖拽创建 UI 元素
- 属性面板编辑
- 预设样式模板

工作流：
1. 在 UI Builder 中设计 UI（所见即所得）
2. 保存为 UXML + USS 文件
3. 在场景 UIDocument 中引用 UXML
4. 代码中获取元素引用 + 注册事件

对比 UGUI 工作流：
UGUI：Hierarchy 中创建 → Inspector 调整 → 代码控制
UI Toolkit：UI Builder 设计 → UXML 输出 → 代码控制

UI Builder 的优势：UI 设计者和程序员可以分工协作
- 设计者：用 UI Builder 调样式
- 程序员：关注逻辑代码
```

---

## 6. 运行时 UI 创建——完全用代码

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class RuntimeUICreator : MonoBehaviour
{
    private VisualElement root;

    void Start()
    {
        // 完全在代码中创建 UI——不需要 UXML
        root = new VisualElement();
        root.style.flexGrow = 1;
        root.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.5f));

        // 创建面板容器
        var panel = new VisualElement();
        panel.style.width = 300;
        panel.style.height = 200;
        panel.style.backgroundColor = new StyleColor(Color.white * 0.9f);
        panel.style.alignSelf = Align.Center;
        panel.style.marginTop = 50;

        // 标题
        var title = new Label("Pause Menu");
        title.style.fontSize = 28;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.marginBottom = 20;
        title.style.marginTop = 10;
        panel.Add(title);

        // 按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Column;
        buttonContainer.style.alignItems = Align.Center;

        // 创建按钮
        string[] buttonTexts = { "Resume", "Settings", "Main Menu" };
        foreach (var text in buttonTexts)
        {
            var button = new Button();
            button.text = text;
            button.style.width = 200;
            button.style.height = 40;
            button.style.marginBottom = 10;
            button.clicked += () => OnMenuButton(text);
            buttonContainer.Add(button);
        }

        panel.Add(buttonContainer);
        root.Add(panel);

        // 将 UI 添加到 UIDocument（场景中需要有 UIDocument 组件）
        var doc = GetComponent<UIDocument>();
        if (doc != null)
        {
            doc.rootVisualElement.Add(root);
        }
    }

    void OnMenuButton(string buttonText)
    {
        Debug.Log($"Menu button: {buttonText}");
        if (buttonText == "Resume")
        {
            // 关闭菜单
            root.RemoveFromHierarchy();
            Time.timeScale = 1;
        }
    }
}
```

---

## 7. Canvas 优化——批处理深度分析

### Canvas 的 Batch 重建机制

上一章提到"动静分离 Canvas"。这里深入剖析 Canvas 的批处理内部机制。

```
Canvas 渲染管线：

1. Layout 阶段
   - 计算 RectTransform 位置/大小
   - 触发需要重新布局的回调

2. Batch 构建阶段
   - 遍历所有 Graphic 子元素
   - 按材质/纹理/Shader 排序
   - 合并相同属性的 Graphic → Batch
   - 生成 Mesh（将所有顶点合并）

3. 渲染阶段
   - 提交 Batches 到 GPU

Canvas 重建的触发条件：
- 任何 Graphic 的属性变化（颜色、尺寸、精灵）
- 任何 RectTransform 的变化
- 文本内容变化
- 新增/移除子元素
```

### Batch Breaking 分析

```csharp
// 批处理断裂的原因：

// 正确的批处理（所有元素合并到一个 Batch）：
/*
Canvas
  ├── Image A (Sprite: ui_atlas)  → Batch 1
  ├── Image B (Sprite: ui_atlas)  → Batch 1 (合并)
  └── Image C (Sprite: ui_atlas)  → Batch 1 (合并)
  3 个 Draw Call
*/

// 批处理断裂：
/*
Canvas
  ├── Image A (Sprite: ui_atlas_1)  → Batch 1
  ├── Image B (Sprite: ui_atlas_2)  → Batch 2 (纹理不同！)
  ├── Image C (Sprite: ui_atlas_1)  → Batch 3 (纹理不同 + Z 顺序中断)
  └── Text D (TMP font)            → Batch 4 (材质不同！)
  4 个 Draw Call
*/
```

### 使用 Profiler 分析 Canvas

```csharp
// 在 Profiler 中分析 UI 性能：
// Window → Analysis → Profiler
// 选择 "Rendering" 模块

// 关键指标：
// - Batches: Draw Call 数量
// - Saved by batching: 合并后节省的 Batch 数
// - SetPass calls: Shader 切换次数

// 在代码中监测：
public class UIDebugger : MonoBehaviour
{
    void Update()
    {
        // 获取 Canvas 的批处理统计
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            int batches = canvas.rootCanvas.renderOrder;
            // 注意：这个值在运行时只读
        }
    }
}
```

### Canvas 优化的具体策略

```csharp
// 策略 1：使用 Sprite Atlas
// 将所有 UI 精灵放入同一个图集 → 所有 Image 用同一纹理 → 合批

// 策略 2：减少 Graphic 嵌套层数
// 每多一层 → 多一次矩阵变换 → 多一次顶点重算

// 策略 3：禁用不使用的 Graphic
// Image.enabled = false （而不是 SetActive(false)）
// disabled 的 Graphic 不会进入 Batch 构建

// 策略 4：TextMeshPro 优化
// - 使用 Sprite Asset 代替大量独立的 Image
// - 合理设置 Atlas Size（避免纹理过大）
// - 使用 Shared Material

// 策略 5：Canvas 嵌套
// 子 Canvas 会创建独立的渲染层
// 子 Canvas 的变化不会引起父 Canvas 重建
// 适合：弹出菜单、Tooltip

public class CanvasOptimizer : MonoBehaviour
{
    public Canvas dynamicCanvas;
    public Canvas tooltipCanvas;

    void ShowTooltip()
    {
        // Show/hide tooltip — 不影响主 Canvas
        tooltipCanvas.enabled = true;
        // tooltipCanvas 的重建只影响它自己
        // 主 Canvas 的 Batches 不变
    }

    void OptimizeForMobile()
    {
        // 移动端 Canvas 优化
        Canvas canvas = GetComponent<Canvas>();
        
        // 不要每帧修改 UI 元素
        // 可以改为：值变化时才更新 UI
        
        // 用 Canvas.overrideSorting 分割 Canvas
        canvas.overrideSorting = true;  // 独立排序
    }
}
```

---

## 8. UGUI vs UI Toolkit——选型对比

```
对比维度          UGUI                    UI Toolkit
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
架构基础         GameObject/Component    VisualElement（轻量）
渲染方式         Canvas + Graphic        Panel + Mesh 生成
布局系统         RectTransform + 锚点     Flexbox（CSS Flex）
样式系统         Inspector 属性           USS（类似 CSS）
UI 定义          Hierarchy 拖拽          UXML + USS 文件
编辑器工具        Scene 视图编辑          UI Builder
运行时 UI        成熟（10 年+）          较新（正在完善）
编辑器扩展        EditorWindow（旧）      全部使用 UI Toolkit
性能             100 个元素以内好         1000+ 元素优势明显
自定义组件       继承 Graphic            继承 VisualElement
数据绑定         手动（写代码）           Binding 系统（2022+）
ListView         需要第三方或手写         内置 ListView/TreeView
3D 空间 UI       World Space Canvas      需要额外的转换
学习成本          低                      中等（CSS 知识）

选型建议：

纯游戏 UI（HUD、菜单）→ UGUI
- 成熟的工具链
- 大量教程和资源
- 场景集成好（World Space）

编辑器扩展 → UI Toolkit（唯一选择）
- Unity 官方已全面迁移

数据密集 UI → UI Toolkit
- ListView 处理 10000+ 条数据
- 虚拟化滚动

跨平台 UI 库 → UI Toolkit
- UXML + USS = 声明式 UI
- 设计器友好
```

---

## C++/Raylib 对照总结

| 高级概念 | Raylib (C++) | UGUI (Unity) | UI Toolkit (Unity) |
|---------|-------------|-------------|-------------------|
| UI 架构 | 即时模式绘制 | GameObject 保留式 | VisualElement 保留式 |
| 样式系统 | 参数传函数 | Inspector 属性 | USS（CSS 语法） |
| UI 标记语言 | 无 | 无 | UXML（XML 语法） |
| 布局系统 | 手动 x/y | RectTransform 锚点 | Flexbox |
| 自定义组件 | 手写绘制函数 | 继承 Graphic | 继承 VisualElement |
| 事件系统 | 手动区域检测 | IPointerXxxHandler | CallbackRegistry |
| 大型列表 | 手动滚动 | 手写 ScrollRect | ListView（虚拟化） |

## 停靠点

> UI Toolkit 是 Unity 的新一代 UI 系统：基于 VisualElement（轻量），使用 UXML（结构）+ USS（样式）。
> USS 是 CSS 的变体——选择器、变量、伪类（:hover、:active）都支持。
> Flexbox 布局让 UI 自适应比 UGUI 更直观（flex-direction、justify-content、align-items）。
> UGUI 的 Canvas 批处理优化关键：Sprite Atlas（图集合并纹理）、动静分离 Canvas、减少 Graphic 嵌套。
> UI Toolkit 适合大量数据展示（ListView 虚拟化）、编辑器工具开发。
> 选择 UGUI 还是 UI Toolkit：游戏内 HUD/菜单 → UGUI；编辑器扩展 → UI Toolkit（别无选择）。
