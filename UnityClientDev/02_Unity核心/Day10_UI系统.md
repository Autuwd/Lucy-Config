# Day 10：UI 系统 — 从 Canvas 渲染到事件系统

## 0. 为什么 Unity 的 UI 和其他引擎不同？

在 Raylib/C++ 中，UI 就是绘制函数调用：

```cpp
// Raylib：UI 是绘图 API
DrawText("Hello", 100, 50, 20, BLACK);
GuiButton({100, 100, 200, 50}, "Click Me");
```

在 Unity 中，UI 是**场景中的对象**——你可以：
- 在 Hierarchy 中创建 UI 元素（像创建 3D 对象一样）
- 给 UI 元素挂组件、加脚本
- 在 Scene 视图中可视化编辑 UI 布局
- 支持响应式设计（不同分辨率自动适配）

---

## 1. Canvas——UI 的根

### Canvas 的作用

所有 UI 元素必须是 Canvas 的子对象（或子对象的子对象...）。Canvas 负责收集所有 UI 元素并渲染它们。

```
场景层级：
Canvas (root)
  ├── Panel
  │   ├── Button
  │   │   ├── Text (TMP)
  │   │   └── Image
  │   └── Slider
  └── HealthBar
      ├── Background
      └── Fill
```

### Canvas 的三种渲染模式

```
Screen Space - Overlay（默认）
- UI 渲染在屏幕最顶层
- 不受摄像机影响，始终在最前面
- 不需要指定摄像机
- 最适合 HUD、菜单

Screen Space - Camera
- UI 渲染在指定摄像机的前方
- 可以受摄像机效果影响（Post-processing）
- 适合需要和 3D 场景结合的 UI

World Space
- UI 像 3D 对象一样在场景中
- 可以被其他物体遮挡
- 适合：血条（漂浮在角色头顶）、3D 菜单
```

### Canvas Scaler——分辨率适配

```csharp
// Canvas 上挂载 CanvasScaler 组件

// UI Scale Mode 选择：
// 1. Constant Pixel Size（固定像素）
//    UI 像素大小固定，高分辨率下 UI 变小
//    适合手机游戏（UI 元素大一点）

// 2. Scale With Screen Size（推荐）
//    根据参考分辨率缩放
//    设置 Reference Resolution = 1920x1080
//    在不同分辨率下 UI 按比例缩放

// 3. Constant Physical Size（固定物理尺寸）
//    根据屏幕 DPI 缩放，保证 UI 物理大小一致
//    很少用

// Screen Match Mode:
// - Match Width or Height（推荐 0.5）
//   0 = 完全按宽度适配
//   1 = 完全按高度适配
//   0.5 = 宽度和高度各占一半权重
```

---

## 2. RectTransform——UI 的坐标系统

### RectTransform vs Transform

```csharp
// 普通 Transform：position 是世界坐标
// RectTransform：anchoredPosition 是相对锚点的偏移

// RectTransform 的关键属性：
RectTransform rect = GetComponent<RectTransform>();

rect.anchoredPosition  // 锚点到轴心的偏移
rect.sizeDelta         // UI 元素的宽高（相对锚点的范围）
rect.anchorMin         // 锚点左下角 (0~1 比例)
rect.anchorMax         // 锚点右上角 (0~1 比例)
rect.pivot             // 轴心 (0~1 比例，0.5=中心)
rect.offsetMin         // 左/下偏移
rect.offsetMax         // 右/上偏移
```

### 锚点（Anchor）机制——自适应布局

```
锚点决定了 UI 元素如何相对于父元素定位：

锚点在父元素角上（左上角）：
┌──────────────┐
│  ██          │  ← 按钮锚定在左上角
│  ──按钮──    │     当父元素变小时，按钮保持距左上角距离不变
│              │
└──────────────┘

锚点拉伸：
┌──────────────┐
│←───按钮────→│  ← 按钮锚定左右边缘
│              │     当父元素变小时，按钮宽度自动缩放
│              │
└──────────────┘

锚点居中：
┌──────────────┐
│              │
│   ┌──────┐   │  ← 按钮锚点居中
│   │ 按钮  │   │     始终居中
│   └──────┘   │
└──────────────┘
```

### 常用 RectTransform 设置

```csharp
// 方法：设置 UI 元素大小和位置
// 不同的对齐方式使用不同的属性组合

// 1. 左上角固定 (anchorMin=0,1, anchorMax=0,1)
rect.anchoredPosition = new Vector2(100, -50);  // 距左上角
rect.sizeDelta = new Vector2(200, 50);           // 固定大小

// 2. 全屏拉伸 (anchorMin=0,0, anchorMax=1,1)
rect.offsetMin = new Vector2(20, 20);    // 距左/下 20px
rect.offsetMax = new Vector2(-20, -20);   // 距右/上 20px

// 3. 底部停靠 (anchorMin=0,0, anchorMax=1,0)
rect.anchoredPosition = new Vector2(0, 50);  // 距底部 50px
rect.sizeDelta = new Vector2(0, 100);         // 高度 100px，宽度自适应
```

---

## 3. UI 组件详解

### TextMeshPro（TMP）——推荐文字方案

```csharp
// TextMeshPro 是 Unity 的现代文字渲染方案
// 使用 Signed Distance Field (SDF) 渲染技术

using TMPro;

public class UIText : MonoBehaviour
{
    public TextMeshProUGUI text;  // TMP 文本组件

    void Start()
    {
        // 设置文字
        text.text = "Hello World";
        
        // 格式化文字（支持富文本）
        text.text = "Score: <color=red>100</color>";
        text.text = "<b>Bold</b> <i>Italic</i> <u>Underline</u>";

        // 字体大小
        text.fontSize = 36;

        // 对齐方式
        text.alignment = TextAlignmentOptions.Center;
    }
}
```

**SDF 渲染原理：**

```
传统字体渲染：使用位图（Bitmap）纹理
- 放大时锯齿明显
- 每个字号需要不同纹理
- 旋转时效果差

SDF 渲染：
- 纹理中存储的是"到最近边缘的距离"（有符号距离场）
- 着色器在运行时计算平滑边缘
- 任意大小都清晰！任意旋转都平滑！
- 只需一个纹理（所有字号共享）
```

### Image——显示图片

```csharp
using UnityEngine.UI;

public class UIImage : MonoBehaviour
{
    public Image image;  // Image 组件

    void Start()
    {
        // 设置精灵
        image.sprite = Resources.Load<Sprite>("Sprites/Icon");

        // 颜色
        image.color = Color.red;

        // 透明度
        image.color = new Color(1, 1, 1, 0.5f);  // 半透明

        // 填充方式（适合血条）
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillAmount = 0.75f;  // 75% 填充
    }
}
```

### Button——可交互按钮

```csharp
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    public Button button;  // Button 组件

    void Start()
    {
        // 方式 1：Inspector 中拖拽绑定（推荐——最直观）
        
        // 方式 2：代码绑定
        button.onClick.AddListener(OnButtonClicked);

        // 方式 3：Lambda 表达式
        button.onClick.AddListener(() => {
            Debug.Log("Button clicked!");
        });

        // 按钮状态控制
        button.interactable = false;  // 禁用按钮
        button.interactable = true;   // 启用
    }

    void OnButtonClicked()
    {
        Debug.Log("Button was clicked!");
    }
}
```

### Slider——滑动条

```csharp
public class UISlider : MonoBehaviour
{
    public Slider slider;

    void Start()
    {
        // 设置范围
        slider.minValue = 0;
        slider.maxValue = 100;

        // 设置当前值
        slider.value = 50;

        // 值变化事件
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float value)
    {
        Debug.Log($"Slider value: {value}");
    }
}
```

---

## 4. 事件系统——UI 交互的桥梁

### EventSystem 的架构

```
EventSystem（场景中必须有 EventSystem）
  ├── Standalone Input Module（处理键盘/鼠标/手柄输入）
  └── Event System 的调度流程：

1. 检测输入（点击、悬停等）
2. Raycast 检测被点击的 UI 元素
3. 发送事件到目标元素
4. 目标元素根据事件调用对应回调
```

### 事件接口

```csharp
// UI 元素可以响应多种事件
using UnityEngine.EventSystems;

// 常用事件接口：
// IPointerClickHandler    → 点击
// IPointerEnterHandler    → 鼠标进入
// IPointerExitHandler     → 鼠标离开
// IPointerDownHandler     → 鼠标按下
// IPointerUpHandler       → 鼠标松开
// IDragHandler            → 拖拽
// IDropHandler            → 放下
// IScrollHandler          → 滚轮

// 示例：一个自定义 UI 响应
public class CustomButton : MonoBehaviour, 
    IPointerClickHandler, 
    IPointerEnterHandler, 
    IPointerExitHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Clicked!");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 鼠标悬停效果
        GetComponent<Image>().color = Color.yellow;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Image>().color = Color.white;
    }
}
```

---

## 5. UI 性能优化

### Canvas 的 Batch 重建

```
UI 的性能消耗主要来自：
1. Canvas 的 Batch 重建（当 UI 元素变化时）
2. Graphic 的顶点重算

每帧 Unity 会：
1. 将所有 UI 元素按材质/纹理排序
2. 合并相同材质/纹理的顶点为同一个 Batch
3. 如果 UI 元素变化（文字、颜色、位置），整个 Canvas 的 Batch 会重建
```

### 动静分离

```csharp
// ❌ 坏：一个 Canvas 包含所有 UI
// 一个 UI 元素变化 → 整个 Canvas 重建
Canvas singleCanvas (有很多子元素)
  ├── 静态背景 (不变)
  ├── 动态血量条 (频繁变化)
  ├── 动态伤害数字 (频繁变化)
  └── 静态菜单 (不变)

// ✅ 好：分离静态和动态 Canvas
Canvas staticCanvas (不会变化)
  ├── 背景
  ├── 边框
  └── 菜单

Canvas dynamicCanvas (频繁变化)
  ├── 血量条
  └── 伤害数字
```

### 图集（Sprite Atlas）

```csharp
// 将多个小图合并成一张大图 → 减少 Draw Call
// 同一 Canvas 中引用同一图集的元素可以合批

// 创建 Sprite Atlas：
// Assets → Create → Sprite Atlas
// 将需要合批的图片拖入
// 在 Project Settings 中启用 Sprite Atlas
```

---

## 练习：血量 HUD 完整实现

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Image fillImage;           // 血条填充图
    public TextMeshProUGUI hpText;   // 血量数字
    public TextMeshProUGUI nameText; // 名字

    [Header("Player Data")]
    public float currentHP = 100;
    public float maxHP = 100;

    void Update()
    {
        // 测试按键
        if (Input.GetKeyDown(KeyCode.H)) currentHP -= 10;
        if (Input.GetKeyDown(KeyCode.R)) currentHP = maxHP;

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateUI();
    }

    void UpdateUI()
    {
        // 血条填充
        fillImage.fillAmount = currentHP / maxHP;

        // 血条颜色渐变
        fillImage.color = Color.Lerp(Color.red, Color.green, currentHP / maxHP);

        // 血量文字
        hpText.text = $"{currentHP} / {maxHP}";

        // 低血量警告
        if (currentHP <= 20)
        {
            hpText.color = Color.red;
            hpText.text += " ⚠";
        }
        else
        {
            hpText.color = Color.white;
        }
    }
}
```

---

---

## C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity (C#) |
|------|-------------|-----------|
| 文字渲染 | `DrawText("Hello", x, y, size, color)` | `TextMeshProUGUI.text = "Hello"` |
| 按钮 | `GuiButton({x,y,w,h}, "OK")` | `Button` + `onClick.AddListener()` |
| 图片 | `DrawTexture(texture, pos, color)` | `Image.sprite` |
| 布局 | 手动计算 x/y | RectTransform + 锚点系统 |
| 事件 | 手动检测点击区域 | EventSystem + Raycaster |
| 适配 | 手动分辨率缩放 | CanvasScaler 自动适配 |
| — | 无对应 | TextMeshPro（SDF 字体渲染） |

## 停靠点

> Unity UI 是**场景对象**（Canvas → RectTransform → Graphic），不是绘图 API。
> RectTransform 用锚点（Anchor）实现自适应布局——比像素定位灵活得多。
> TextMeshPro 使用 SDF 渲染——任意大小都清晰。
> UI 性能优化的关键是**动静分离 Canvas**——避免静态 UI 和动态 UI 在同一个 Canvas 中。
> EventSystem 负责处理输入→Raycast→事件派发。

