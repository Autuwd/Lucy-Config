### 快速入门指南（阶段2）
- 目标：快速掌握 Unity 核心开发流程与基础组件，包括 GameObject/Component、Transform、Prefab、输入系统、生命周期、UI、以及简单的物理概念。
- 学习路线：先看对照学习点与练习入口（UnityInterview/逐条对应章节与练习模板.md），再完成阶段2 的对照练习。
- 对照入口：UnityInterview/逐条对应章节与练习模板.md
- 运行/验证：在一个简化场景中实现一个可控制移动的玩家、简单 UI 按钮、以及与地形的简单交互。
- 最小化可运行示例：请查看 UnityInterview/Examples/Stage2/PlayerMovementLite.cs 和 UnityInterview/Examples/Stage2/Stage2UIButtonDemo.cs 进行直接拷贝运行。

# 第二阶段：Unity 基础

> 本阶段帮助你快速掌握 Unity 引擎的核心概念和基本操作

---

## 配套视频教程

| 知识点 | 推荐视频 | 视频链接 |
|--------|----------|----------|
| Unity 入门 | Unity与C# 2D游戏编程制作基础 | [B站视频](https://www.bilibili.com/video/BV1pb421J7pB/) |
| 生命周期 | Unity 生命周期详解 | [B站视频](https://www.bilibili.com/video/BV17a4ezCEf2/) |
| 物理碰撞 | Unity 物理系统教程 | [B站视频](https://www.bilibili.com/video/BV1Eu4y1w7Rg/) |

> 更多视频教程请查看：[视频教程汇总.md](./视频教程汇总.md)

---

## 目录

- [2.1 Unity 编辑器界面与操作](#21-unity-编辑器界面与操作)
- [2.2 GameObject 与 Component](#22-gameobject-与-component)
- [2.3 Transform 组件详解](#23-transform-组件详解)
- [2.4 Prefab 与 Instantiate](#24-prefab-与-instantiate)
- [2.5 输入系统](#25-输入系统)
- [2.6 生命周期与 Update](#26-生命周期与-update)
- [2.7 物理系统](#27-物理系统)
- [2.8 碰撞检测](#28-碰撞检测)
- [2.9 音频系统](#29-音频系统)
- [2.10 动画系统](#210-动画系统)
- [2.11 UI 系统](#211-ui-系统)

---

## 2.1 Unity 编辑器界面与操作

#### 核心概念解析

**什么是Unity编辑器？**
- Unity编辑器是集成开发环境（IDE），用于创建、编辑、测试和部署游戏
- 它提供了可视化的操作界面，让开发者可以通过拖拽、点击等交互方式完成大部分工作
- 同时支持代码编写，实现可视化与代码的完美结合

```
Unity编辑器的工作原理：

┌─────────────────────────────────────────────────────────┐
│                    Unity 编辑器                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   场景 ←→ 游戏对象 ←→ 组件 ←→ 脚本                      │
│     ↓            ↓           ↓          ↓              │
│   视觉表现    实体管理     功能模块    程序逻辑           │
│                                                         │
│   用户通过编辑器操作 → 自动生成/修改数据 → 游戏运行时   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**编辑器与游戏的区别**
- 场景视图（Scene View）：编辑时的可视化界面，可以看到并操作所有游戏对象
- 游戏视图（Game View）：运行游戏时的实际画面，只显示摄像机拍摄的内容
- 这是两个完全不同的视角，帮助开发者在编辑和测试时切换

**为什么需要熟悉编辑器？**
- 提高开发效率：快捷操作可以节省大量时间
- 便于调试：了解各窗口功能才能快速定位问题
- 团队协作：统一的操作习惯便于代码和资源管理

---

### 主要窗口介绍

```
┌─────────────────────────────────────────────────────────────────┐
│  菜单栏 (Menu Bar)                                               │
├────────────┬────────────────────────────────┬───────────────────┤
│  层级视图  │     场景视图 (Scene View)       │   检查器 (Inspector)│
│  (Hierarchy│                                │                    │
│   )        │                                │   - Transform     │
│            │     游戏对象可视化编辑           │   - Mesh Filter  │
│  - Main    │     相机/灯光/物体             │   - Mesh Renderer│
│   Camera   │                                │   - Collider      │
│  - Plane   │                                │   - 脚本组件      │
│  - Player  │                                │                    │
├────────────┴────────────────────────────────┴───────────────────┤
│  项目窗口 (Project)           │      控制台 (Console)            │
│  - Assets 文件夹             │      - 编译错误                  │
│  - 脚本/材质/预制体/场景     │      - 运行日志                  │
└─────────────────────────────┴──────────────────────────────────┘
                              │
                              ▼
                    游戏视图 (Game View)
                    - 实际运行效果预览
```

### 常用快捷键

| 操作 | Windows | Mac |
|------|---------|-----|
| 播放/暂停 | Ctrl+P | Cmd+P |
| 停止 | Ctrl+Shift+P | Cmd+Shift+P |
| 搜索 | Ctrl+F | Cmd+F |
| 打开脚本 | Ctrl+Shift+C | Cmd+Shift+C |
| 刷新 Asset | Ctrl+R | Cmd+R |
| 保存场景 | Ctrl+S | Cmd+S |
| 撤销 | Ctrl+Z | Cmd+Z |
| 创建空物体 | Ctrl+Shift+N | Cmd+Shift+N |

### 场景视图操作

```csharp
// 鼠标操作
// - 右键拖动: 旋转视角
// - 滚轮: 缩放
// - 中键拖动: 平移
// - 按住 Alt + 左键: 环绕选中物体旋转

// 快捷键
// Q: 手型工具（平移）
// W: 移动工具
// E: 旋转工具
// R: 缩放工具
// T: 矩形变换工具
// L: 绘制路径点
// F: 聚焦选中物体
```

### 练习 2.1

**练习题：**
1. 说出 Unity 编辑器中至少4个主要窗口的名称及其作用
2. 使用快捷键创建一个空物体，并将其移动到位置 (5, 3, 0)

**答案：**
1. 窗口作用：
   - Hierarchy（层级视图）：显示场景中所有游戏对象
   - Scene（场景视图）：可视化编辑游戏世界
   - Inspector（检查器）：显示和修改选中对象的属性
   - Project（项目窗口）：管理项目中的资源文件
   - Console（控制台）：显示日志和错误信息
   - Game（游戏视图）：预览游戏运行效果

2. 操作步骤：
   - 按 Ctrl+Shift+N 创建空物体
   - 按 W 切换到移动工具
   - 在 Inspector 中输入 Position: X=5, Y=3, Z=0

### 相关知识点

- 2.2 GameObject - 游戏对象是编辑器中操作的主体
- 2.3 Transform - 物体的位置通过Transform组件控制

---

## 2.2 GameObject 与 Component

#### 核心概念解析

**什么是 GameObject？**
- GameObject 是 Unity 中**所有实体**的基类
- 场景中的每个物体都是 GameObject（玩家、敌人、子弹、UI等）
- GameObject 本身**不包含功能**，功能由 **Component（组件）** 提供

```
GameObject 与 Component 的关系：

┌────────────────────────────┐
│      GameObject            │
│  ┌──────────────────────┐ │
│  │ Transform (必须)      │ │  ← 位置/旋转/缩放
│  ├──────────────────────┤ │
│  │ MeshRenderer          │ │  ← 渲染网格
│  ├──────────────────────┤ │
│  │ Collider              │ │  ← 碰撞体
│  ├──────────────────────┤ │
│  │ Rigidbody             │ │  ← 物理效果
│  ├──────────────────────┤ │
│  │ 自定义脚本 (Player)   │ │  ← 业务逻辑
│  └──────────────────────┘ │
└────────────────────────────┘

GameObject = 容器
Component = 功能模块
```

**GameObject 的层级结构**
- Transform 组件定义了父子层级关系
- 子物体会**继承**父物体的位置、旋转、缩放变换
- 移动父物体，所有子物体一起移动

**组件系统特点**
1. **组合优于继承**：通过添加组件实现不同功能
2. **数据驱动**：组件可以在 Inspector 中配置
3. **可插拔**：运行时可以添加/移除组件

### GameObject（游戏对象）

Unity 中所有实体的基类

```csharp
// ========== 创建游戏对象 ==========

// 创建一个空物体，名称为"MyObject"
GameObject emptyObj = new GameObject("MyObject");

// 创建名为"Player"的物体，并添加组件
GameObject player = new GameObject("Player");

// AddComponent<T>()：添加指定类型的组件到游戏对象
// Rigidbody：物理组件，让物体受重力影响
player.AddComponent<Rigidbody>();

// 添加自定义脚本组件，处理游戏逻辑
player.AddComponent<PlayerController>();

// ========== 查找游戏对象 ==========

// 按名称查找场景中名为"Player"的对象（性能较差，少用）
GameObject.Find("Player");

// 按标签查找，效率比名称查找高
// 需先在Inspector中为物体设置Tag
GameObject.FindGameObjectWithTag("Enemy");

// 查找所有指定类型的物体，返回数组
GameObject.FindObjectsOfType<Enemy>();

// ========== 获取组件 ==========

// 获取当前物体上的指定组件，返回null如果不存在
// 这是最常用的获取组件方式
Rigidbody rb = gameObject.GetComponent<Rigidbody>();

// 改进版：获取不到则添加一个
Rigidbody rb2 = gameObject.GetComponent<Rigidbody>();
if (rb2 == null)
{
    // AddComponent：运行时动态添加组件
    rb2 = gameObject.AddComponent<Rigidbody>();
}

// 获取当前物体上的所有组件，返回数组
Component[] components = gameObject.GetComponents<Component>();

// ========== 启用/禁用游戏对象 ==========

// SetActive：设置游戏对象是否可见/激活
// 参数false：隐藏游戏对象（包括所有子物体）
gameObject.SetActive(false);

// 参数true：显示游戏对象
gameObject.SetActive(true);

// activeSelf：获取自身的激活状态（不受父物体影响）
// activeInHierarchy：获取实际激活状态（受父物体影响）
bool isActive = gameObject.activeSelf;

// ========== 销毁游戏对象 ==========

// Destroy：销毁游戏对象，在当前帧结束后执行
// 常用于子弹消失、敌人死亡等
Destroy(gameObject);

// DestroyImmediate：立即销毁，不等待帧结束
// 注意：在编辑器中常用，运行时少用
DestroyImmediate(gameObject);

// ========== 标签和层级 ==========

// tag：游戏对象的标签，用于快速识别
// 常用标签：Player, Enemy, Ground, Respawn
gameObject.tag = "Player";

// layer：游戏对象的层级，用于碰撞检测和射线检测
// 使用LayerMask将字符串转换为层级编号
gameObject.layer = LayerMask.NameToLayer("Default");

// CompareTag：比较标签，比直接访问tag属性更高效
bool isPlayer = gameObject.CompareTag("Player");
```

### 练习 2.2

**练习题：**
1. 创建一个名为 "Player" 的游戏对象，并添加 Rigidbody 和自定义脚本组件
2. 编写代码实现：查找场景中所有标签为 "Enemy" 的物体，并获取它们的 Transform 组件

**答案：**
```csharp
// 1. 创建游戏对象并添加组件
GameObject player = new GameObject("Player");
player.AddComponent<Rigidbody>();
player.AddComponent<PlayerController>();

// 2. 查找所有Enemy并获取Transform
GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
foreach (GameObject enemy in enemies)
{
    Transform enemyTransform = enemy.transform;
    Debug.Log($"Enemy位置: {enemyTransform.position}");
}
```

### 相关知识点

- 2.3 Transform - Transform组件控制位置旋转缩放
- 2.6 生命周期 - 脚本组件在生命周期中的调用顺序

---

## 2.3 Transform 组件详解

#### 核心概念解析

**什么是Transform组件？**
- Transform是Unity中最基础、最重要的组件
- 每个GameObject都必须有Transform，它决定了物体在游戏世界中的位置、旋转和大小
- 没有Transform，物体就无法存在于游戏世界中

```
Transform 的核心作用：

┌─────────────────────────────────────────────────────┐
│              Transform 组件                          │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────┐   ┌─────────┐   ┌─────────┐         │
│  │ Position│   │ Rotation│   │  Scale  │         │
│  │  位置   │   │  旋转   │   │  缩放   │         │
│  │ (x,y,z) │   │ (x,y,z) │   │ (x,y,z) │         │
│  └─────────┘   └─────────┘   └─────────┘         │
│       ↓              ↓             ↓              │
│   坐标系统        方向向量       大小比例           │
│                                                     │
└─────────────────────────────────────────────────────┘
```

**Transform 的层级关系**
- 世界空间（World Space）：游戏世界的绝对坐标
- 局部空间（Local Space）：相对于父物体的坐标
- 子物体会继承父物体的变换，形成层级结构

**游戏开发中的应用**
- 移动角色：改变position
- 旋转视角：改变rotation
- 缩放特效：改变scale
- 相机跟随：基于Target的Transform计算位置

---

Transform 是每个 GameObject 必须有的组件，控制位置、旋转、缩放

### 核心属性

```csharp
public class TransformDemo : MonoBehaviour
{
    void Start()
    {
        // ========== 位置属性 ==========
        
        // position：物体在世界空间中的绝对坐标
        // 相对于场景原点的位置，用于计算距离、判断位置等
        Vector3 pos = transform.position;
        
        // localPosition：相对于父物体的位置
        // 如果没有父物体，等同于position
        // 用于获取物体在父物体坐标系中的位置
        Vector3 localPos = transform.localPosition;
        
        // ========== 旋转属性 ==========
        
        // rotation：物体在世界空间中的旋转（四元数表示）
        // 四元数避免万向锁问题，推荐使用
        Quaternion rot = transform.rotation;
        
        // localRotation：相对于父物体的旋转
        // 用于获取物体相对于父物体的旋转角度
        Quaternion localRot = transform.localRotation;
        
        // ========== 缩放属性 ==========
        
        // localScale：相对于父物体的缩放
        // 值为1表示正常大小，0.5表示一半，2表示两倍
        // 子物体会继承父物体的缩放（相乘）
        Vector3 scale = transform.localScale;
        
        // ========== 方向向量 ==========
        
        // forward：物体的正前方（蓝色Z轴方向）
        // 用于移动、发射子弹等需要朝向的操作
        Vector3 forward = transform.forward;
        
        // right：物体的正右方（红色X轴方向）
        // 用于计算水平移动方向
        Vector3 right = transform.right;
        
        // up：物体的正上方（绿色Y轴方向）
        // 用于计算向上方向
        Vector3 up = transform.up;
    }
}
```

### 移动旋转

```csharp
public class MovementDemo : MonoBehaviour
{
    public float speed = 5f;
    public float rotateSpeed = 100f;
    
    void Update()
    {
        // 方式1: 直接修改 position（不推荐，因为会无视碰撞）
        transform.position += Vector3.forward * speed * Time.deltaTime;
        
        // 方式2: Translate（本地坐标系）
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
        
        // 方式3: 欧拉角旋转
        float h = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * h * rotateSpeed * Time.deltaTime);
        
        // 方式4: LookAt 看向目标
        Transform target = GameObject.Find("Target").transform;
        transform.LookAt(target);
        
        // 方式5: Lerp 平滑移动
        Vector3 start = transform.position;
        Vector3 end = new Vector3(10, 0, 10);
        transform.position = Vector3.Lerp(start, end, Time.deltaTime);
        
        // 方式6: MoveTowards 匀速移动
        transform.position = Vector3.MoveTowards(
            transform.position, 
            end, 
            speed * Time.deltaTime
        );
        
        // 方式7: SmoothDamp 平滑阻尼（相机跟随常用）
        Vector3 velocity;
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            target.position, 
            ref velocity, 
            0.3f
        );
    }
}
```

### 层级操作

```csharp
public class HierarchyDemo : MonoBehaviour
{
    void Demo()
    {
        // 获取父物体
        Transform parent = transform.parent;
        
        // 设置父物体
        GameObject newParent = new GameObject("Parent");
        transform.SetParent(newParent.transform);
        
        // 设置父物体（世界坐标不变）
        transform.SetParent(newParent.transform, false);
        
        // 解除父子关系
        transform.SetParent(null);
        
        // 获取子物体数量
        int childCount = transform.childCount;
        
        // 获取子物体
        Transform firstChild = transform.GetChild(0);
        Transform lastChild = transform.GetChild(childCount - 1);
        
        // 查找子物体
        Transform findChild = transform.Find("ChildName");
        
        // 遍历所有子物体
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Debug.Log(child.name);
        }
        
        // 获取根物体
        Transform root = transform.root;
        
        // 判断是否是其他物体的子物体
        bool isChild = transform.IsChildOf(someParent);
    }
}
```

### 练习 2.3

**练习题：**
1. 使用 Transform 的 Lerp 方法，实现一个物体从位置 A 平滑移动到位置 B
2. 编写代码：获取当前物体的父物体，并设置父物体为 null（解除父子关系）

**答案：**
```csharp
// 1. Lerp平滑移动
public Transform targetPoint;
public float duration = 2f;

IEnumerator MoveToTarget()
{
    Vector3 startPos = transform.position;
    float timer = 0;
    
    while (timer < duration)
    {
        timer += Time.deltaTime;
        transform.position = Vector3.Lerp(startPos, targetPoint.position, timer / duration);
        yield return null;
    }
    transform.position = targetPoint.position;
}

// 2. 解除父子关系
Transform parent = transform.parent;
if (parent != null)
{
    transform.SetParent(null);  // 解除父子关系
}
```

### 相关知识点

- 2.2 Component - Transform继承自Component
- 2.7 物理系统 - 物理移动与Transform移动的区别

---

## 2.4 Prefab 与 Instantiate

#### 核心概念解析

**什么是Prefab（预制体）？**
- Prefab是一种可重复使用的游戏对象模板
- 它保存了游戏对象的完整结构：包括Transform、渲染器、碰撞器、脚本等所有组件和属性
- 类似于面向对象中的"类"，Prefab是对象的模板，可以创建出无数个实例

```
Prefab 的工作原理：

┌─────────────────────────────────────────────────────────┐
│                   Prefab（模板）                          │
│   ┌─────────────────────────────────────────────────┐  │
│   │ EnemyPrefab                                       │  │
│   │  ├ Transform (位置/旋转/缩放)                      │  │
│   │  ├ MeshRenderer (模型渲染)                        │  │
│   │  ├ Collider (碰撞体)                              │  │
│   │  ├ Rigidbody (物理组件)                           │  │
│   │  └ EnemyController (AI脚本)                       │  │
│   └─────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓ Instantiate（实例化）
┌─────────────────────────────────────────────────────────┐
│                   实例（Instance）                        │
│   ┌─────────┐  ┌─────────┐  ┌─────────┐               │
│   │ Enemy1  │  │ Enemy2  │  │ Enemy3  │  →  多个敌人   │
│   └─────────┘  └─────────┘  └─────────┘               │
└─────────────────────────────────────────────────────────┘
```

**为什么使用Prefab？**
- 效率：修改Prefab会自动更新所有实例，无需逐个修改
- 复用：一个Prefab可以创建无数个对象
- 团队协作：Prefab作为资源便于团队共享和管理

**Prefab变体（Prefab Variant）**
- 继承自另一个Prefab，保留父级Prefab的结构
- 可以覆盖或添加新的属性，实现差异化管理

---

### Prefab（预制体）

可重复使用的游戏对象模板

```csharp
public class PrefabDemo : MonoBehaviour
{
    // 在 Inspector 中拖入预制体
    public GameObject bulletPrefab;
    public GameObject enemyPrefab;
    
    void Start()
    {
        // 实例化（克隆）预制体
        GameObject bullet = Instantiate(bulletPrefab);
        
        // 实例化并设置位置和旋转
        GameObject enemy = Instantiate(
            enemyPrefab, 
            new Vector3(0, 0, 10), 
            Quaternion.identity
        );
        
        // 实例化并设置父物体
        GameObject obj = Instantiate(bulletPrefab, transform);
        
        // 实例化并设置父物体（保持世界坐标）
        GameObject obj2 = Instantiate(bulletPrefab, transform, false);
    }
}
```

### 预制体的三种实例

| 类型 | 说明 | 使用场景 |
|------|------|----------|
| Original | 预制体本体 | 在 Project 窗口 |
| Instance | 场景中的实例 | 在 Hierarchy 窗口 |
| Variant | 变体，继承自 Original | 继承预制体 |

### 预制体连接与解耦

```csharp
public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 3f;
    
    void Start()
    {
        // 向前飞行
        GetComponent<Rigidbody>().velocity = transform.forward * speed;
        
        // 自动销毁
        Destroy(gameObject, lifetime);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // 销毁子弹
        Destroy(gameObject);
    }
}

public class Shooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.1f;
    
    private float nextFireTime;
    
    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }
    
    void Shoot()
    {
        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }
}
```

### 练习 2.4

**练习题：**
1. 创建一个子弹预制体，编写脚本实现：射击时实例化子弹，3秒后自动销毁
2. 说明 Prefab 变体（Variant）与普通 Prefab 的区别及使用场景

**答案：**
```csharp
// 1. 子弹脚本
public class Bullet : MonoBehaviour
{
    public float lifetime = 3f;
    public float speed = 20f;
    
    void Start()
    {
        // 向前飞行
        GetComponent<Rigidbody>().velocity = transform.forward * speed;
        
        // 3秒后自动销毁
        Destroy(gameObject, lifetime);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // 碰撞后立即销毁
        Destroy(gameObject);
    }
}

// 2. Prefab变体 vs 普通Prefab
// 普通Prefab：独立模板，修改影响所有实例
// Prefab变体：继承自另一个Prefab，可以覆盖部分属性
// 使用场景：
// - 多个敌人需要不同属性但共享结构 → 使用变体
// - 需要统一修改所有实例 → 使用普通Prefab
```

### 相关知识点

- 3.5 对象池技术 - 大量子弹创建应使用对象池优化性能
- 4.5 工厂模式 - 配合工厂模式管理预制体创建

---

## 2.5 输入系统

#### 核心概念解析

**什么是Unity输入系统？**
- 输入系统是Unity与玩家交互的桥梁，负责将键盘、鼠标、手柄、触摸等外设操作转换为游戏内的动作
- Unity提供了两套输入系统：传统的Input系统和新版的Input System

```
输入系统的工作流程：

┌─────────────────────────────────────────────────────────┐
│                    输入系统                              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   外设操作    →    输入系统    →    游戏逻辑           │
│   (键盘/鼠标)    (转换处理)      (执行对应行为)        │
│                                                         │
│   例如：按下"W"  →  Input.GetAxis →  角色向前移动        │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**传统Input vs 新输入系统**

| 特性 | 传统Input | 新Input System |
|------|----------|----------------|
| 架构 | 旧版，基于Manager | 基于Package |
| 配置 | Project Settings中配置 | 使用Input Actions |
| 支持 | 键盘/鼠标/触摸 | 键盘/鼠标/触摸/手柄/自定义 |
| 学习成本 | 简单 | 较复杂 |

**为什么需要新的输入系统？**
- 旧系统难以扩展支持新设备
- 新系统支持更多输入设备
- 配置更灵活，支持自定义映射
- 支持运行时切换键盘/手柄

---

### 传统 Input 系统

```csharp
public class InputDemo : MonoBehaviour
{
    void Update()
    {
        // ========== 键盘输入 ==========
        
        // GetKeyDown：按键按下的瞬间返回true（只触发一次）
        // 适用于：跳跃、攻击等一次性动作
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("空格键按下");
        }
        
        // GetKey：按键按住期间持续返回true
        // 适用于：持续移动、蓄力等
        if (Input.GetKey(KeyCode.W))
        {
            Debug.Log("W 键按住");
        }
        
        // GetKeyUp：按键抬起的瞬间返回true（只触发一次）
        // 适用于：释放技能、结束蓄力等
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Debug.Log("空格键抬起");
        }
        
        // ========== 轴输入（虚拟轴） ==========
        
        // GetAxis：获取虚拟轴的值，返回-1到1之间的浮点数
        // 自动处理WASD和方向键，带输入平滑效果
        // Horizontal：水平轴（负值=左，正值=右）
        float h = Input.GetAxis("Horizontal");
        
        // Vertical：垂直轴（负值=下，正值=上）
        float v = Input.GetAxis("Vertical");
        
        // GetAxisRaw：获取原始轴值，无平滑
        // 返回值只有-1、0、1三个选项，适合竞技游戏
        float hRaw = Input.GetAxisRaw("Horizontal");
        
        // ========== 鼠标输入 ==========
        
        // mousePosition：鼠标在屏幕上的像素坐标
        // 左下角为(0,0)，右上角为(屏幕宽,屏幕高)
        // 常用于：将屏幕坐标转换为世界坐标
        Vector3 mousePos = Input.mousePosition;
        
        // mouseScrollDelta：鼠标滚轮滚动值
        // 正值=向上滚动，负值=向下滚动
        float mouseScroll = Input.mouseScrollDelta.y;
        
        // GetMouseButton：鼠标按钮状态
        // 0=左键，1=右键，2=中键
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("鼠标左键点击");
        }
        
        if (Input.GetMouseButton(1))
        {
            Debug.Log("鼠标右键按住");
        }
        
        // ========== 触摸输入 ==========
        
        // touchCount：当前触摸点数量
        if (Input.touchCount > 0)
        {
            // GetTouch：获取指定索引的触摸点
            Touch touch = Input.GetTouch(0);
            
            // position：触摸点在屏幕上的位置
            Vector2 pos = touch.position;
            
            // phase：触摸点的状态（开始、移动、结束等）
            TouchPhase phase = touch.phase;
        }
    }
}
```

### Unity 6 新输入系统

```csharp
// 需要安装 Input System 包

using UnityEngine.InputSystem;

public class NewInputDemo : MonoBehaviour
{
    private PlayerInput playerInput;
    
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    
    // 通过 Input Action 回调
    public void OnMove(InputValue value)
    {
        Vector2 move = value.Get<Vector2>();
        Debug.Log($"移动: {move}");
    }
    
    public void OnFire(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("开火!");
        }
    }
    
    // 手动查询
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard.wKey.isPressed)
        {
            Debug.Log("W键按住");
        }
        
        var mouse = Mouse.current;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Debug.Log("鼠标左键按下");
        }
    }
}
```

### 练习 2.5

**练习题：**
1. 编写一个脚本，使用传统Input检测：按下空格键时跳跃，按下鼠标左键时射击
2. 使用新输入系统实现：WASD移动 + Shift加速

**答案：**
```csharp
// 1. 传统Input
void Update()
{
    // 跳跃
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Jump();
    }
    
    // 射击
    if (Input.GetMouseButtonDown(0))
    {
        Fire();
    }
}

// 2. 新输入系统
public void OnMove(InputValue value)
{
    Vector2 input = value.Get<Vector2>();
    float speed = Input.GetKey(KeyCode.LeftShift) ? 12f : 6f;
    transform.Translate(new Vector3(input.x, 0, input.y) * speed * Time.deltaTime);
}
```

### 相关知识点

- 2.6 生命周期 - 输入检测应在Update中进行
- 2.3 Transform - 使用Transform实现角色移动

---

## 2.6 生命周期与 Update

#### 核心概念解析

**Unity 脚本生命周期是什么？**
- Unity 脚本从创建到销毁，会经历一系列**回调方法**
- 这些方法在特定时机自动被调用
- 理解生命周期是编写 Unity 脚本的基础

```
Unity 脚本生命周期图：

┌─────────────────────────────────────────────────────────────┐
│                    场景加载                                  │
├─────────────────────────────────────────────────────────────┤
│  Awake()          ─── 只调用一次，脚本启用就调用            │
│       ↓                                                     │
│  OnEnable()       ─── 每次脚本从禁用变为启用时调用          │
│       ↓                                                     │
│  Start()          ─── 第一帧 Update 前调用，一次             │
├─────────────────────────────────────────────────────────────┤
│                    游戏运行中                               │
├─────────────────────────────────────────────────────────────┤
│                    ↓ 每帧执行                              │
│  FixedUpdate()    ─── 物理计算（固定时间间隔）             │
│       ↓                                                     │
│  Update()         ─── 游戏逻辑（每帧一次）                 │
│       ↓                                                     │
│  LateUpdate()    ─── 所有 Update 后执行（相机跟随）       │
├─────────────────────────────────────────────────────────────┤
│                    脚本禁用/销毁                            │
├─────────────────────────────────────────────────────────────┤
│  OnDisable()      ─── 脚本禁用或销毁时                      │
│       ↓                                                     │
│  OnDestroy()      ─── 对象销毁时调用                        │
└─────────────────────────────────────────────────────────────┘
```

**为什么需要多个 Update？**

| 回调 | 执行时机 | 用途 |
|------|----------|------|
| FixedUpdate | 固定时间间隔（默认0.02s） | 物理计算、力应用 |
| Update | 每帧（帧率不定） | 游戏逻辑、输入检测 |
| LateUpdate | 所有 Update 后 | 相机跟随、UI更新 |

**为什么相机要在 LateUpdate？**

```csharp
// Player 的 Update
void Update() {
    transform.position += Vector3.forward * speed;  // 玩家移动
}

// Camera 的 Update（错误做法）
void Update() {
    transform.position = player.position + offset;  // 可能获取到未更新的位置
}

// Camera 的 LateUpdate（正确做法）
void LateUpdate() {
    transform.position = player.position + offset;  // 此时玩家已经移动完毕
}
```

**Time.deltaTime 的重要性**
- `deltaTime` = 上一帧到当前帧的时间间隔
- 乘以 `deltaTime` 可以实现**帧率无关**的移动
- 60 FPS 时 deltaTime ≈ 0.016，30 FPS 时 ≈ 0.033

### Unity 脚本生命周期

```
┌─────────────────────────────────────────────────────────────┐
│  场景加载时                                                   │
│    Awake → OnEnable → Start                                │
├─────────────────────────────────────────────────────────────┤
│  每帧 (Update)                                              │
│    ↓                                                        │
│  物理更新 (FixedUpdate) ← 固定时间间隔                      │
│    ↓                                                        │
│  游戏逻辑 (Update)                                          │
│    ↓                                                        │
│  渲染前 (LateUpdate) ← 所有 Update 后执行                   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│  场景切换/物体销毁时                                         │
│    OnDisable → OnDestroy                                   │
└─────────────────────────────────────────────────────────────┘
```

### 各生命周期详解

```csharp
public class LifecycleDemo : MonoBehaviour
{
    // 1. Awake - 对象创建时调用（即使脚本禁用）
    //    最先执行，用于初始化
    void Awake()
    {
        Debug.Log("Awake");
        
        // 常用初始化
        rb = GetComponent<Rigidbody>();
        health = 100;
    }
    
    // 2. OnEnable - 脚本变为启用时调用
    //    每次 enabled = true 时执行
    void OnEnable()
    {
        Debug.Log("OnEnable");
    }
    
    // 3. Start - 第一帧 Update 前调用
    //    用于组件间通信
    void Start()
    {
        Debug.Log("Start");
        
        // 获取其他组件（此时它们已 Start）
        GameObject player = GameObject.FindWithTag("Player");
    }
    
    // 4. FixedUpdate - 固定时间间隔调用（默认 0.02 秒）
    //    用于物理计算
    void FixedUpdate()
    {
        // 物理力
        rb.AddForce(Vector3.forward * 10);
    }
    
    // 5. Update - 每帧调用一次
    //    用于游戏逻辑、输入检测
    void Update()
    {
        // 输入
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
        
        // 动画状态检查
        // AI 决策
    }
    
    // 6. LateUpdate - 所有 Update 后调用
    //    用于相机跟随（确保所有物体移动完毕）
    void LateUpdate()
    {
        // 相机跟随玩家
        Camera.main.transform.position = player.position + offset;
    }
    
    // 7. OnDisable - 脚本变为禁用时调用
    void OnDisable()
    {
        Debug.Log("OnDisable");
    }
    
    // 8. OnDestroy - 对象销毁时调用
    void OnDestroy()
    {
        Debug.Log("OnDestroy");
    }
}
```

### Time 类常用属性

```csharp
void TimeDemo()
{
    float dt = Time.deltaTime;     // 上一帧到这帧的时间（秒）
    float t = Time.time;          // 游戏开始到现在的时间
    
    float unscaled = Time.unscaledDeltaTime;  // 不受 Time.timeScale 影响
    float unscaledTime = Time.unscaledTime;
    
    int frame = Time.frameCount;  // 总帧数
    
    float scale = Time.timeScale;  // 时间缩放（1=正常，0.5=慢动作）
    
    // 设置时间缩放实现子弹时间
    Time.timeScale = 0.1f;  // 慢动作
    Time.timeScale = 1f;    // 恢复正常
}
```

### 练习 2.6

**练习题：**
1. 说明Awake、Start、Update、FixedUpdate、LateUpdate的执行顺序和时机
2. 使用Time.deltaTime实现一个不受帧率影响的移动逻辑，并解释原理

**答案：**
```csharp
// 1. 执行顺序
// 场景加载时：Awake → OnEnable → Start
// 每帧：FixedUpdate → Update → LateUpdate
// 脚本禁用：OnDisable
// 对象销毁：OnDestroy

// 2. 帧率无关移动
public float speed = 5f;

void Update()
{
    // Time.deltaTime 是上一帧到当前帧的时间（秒）
    // 60FPS时约0.016，30FPS时约0.033
    // 距离 = 速度 × 时间
    float moveDistance = speed * Time.deltaTime;
    transform.Translate(Vector3.forward * moveDistance);
}
// 原理：无论帧率如何，每秒移动距离都是 speed 米
```

### 相关知识点

- 2.7 物理系统 - 物理计算应在FixedUpdate中进行
- 2.9 相机控制 - 相机跟随应在LateUpdate中

---

## 2.7 物理系统

#### 核心概念解析

**什么是 Rigidbody（刚体）？**
- Rigidbody 是 Unity 的**物理模拟组件**
- 添加 Rigidbody 后，物体将受**重力**和**力的影响**
- 物理系统会自动计算物体的**运动轨迹**

```
添加 Rigidbody 前后对比：

┌────────────────────────────────────┐
│         不添加 Rigidbody            │
│  • 物体不受重力影响                 │
│  • 不会自然下落                     │
│  • 只能手动设置位置/旋转             │
│  • 不参与物理碰撞                   │
└────────────────────────────────────┘

┌────────────────────────────────────┐
│         添加 Rigidbody              │
│  • 受重力影响自然下落               │
│  • 受力后产生加速度                 │
│  • 与其他物体碰撞                   │
│  • 可以设置速度、角速度              │
└────────────────────────────────────┘
```

**Rigidbody 核心属性**

| 属性 | 作用 | 游戏开发中的应用 |
|------|------|-----------------|
| Mass | 质量 | 大质量物体更难推动 |
| Drag | 线性阻力 | 空气阻力，控制停止速度 |
| Angular Drag | 旋转阻力 | 控制旋转停止 |
| Use Gravity | 重力开关 | 失重状态、太空场景 |
| Is Kinematic | 运动学 | 手动控制但仍参与碰撞 |
| Constraints | 约束 | 限制某些轴的移动/旋转 |

**ForceMode（力的模式）**

| 模式 | 作用 | 使用场景 |
|------|------|----------|
| Force | 持续力，考虑质量 | 车辆加速、人物移动 |
| Acceleration | 持续力，忽略质量 | 火箭推进 |
| Impulse | 瞬间冲量 | 跳跃、爆炸冲击 |
| VelocityChange | 瞬间速度改变 | 子弹发射 |

**CharacterController vs Rigidbody**

| 特性 | Rigidbody | CharacterController |
|------|-----------|---------------------|
| 物理模拟 | 完整 | 简化 |
| 碰撞检测 | 自动 | 手动检测 |
| 楼梯处理 | 困难 | 容易 |
| 斜坡滑动 | 自动 | 可控 |
| 推荐用途 | 车辆、抛射物 | 玩家角色 |

### Rigidbody（刚体）

使物体受物理影响

```csharp
public class RigidbodyDemo : MonoBehaviour
{
    private Rigidbody rb;
    
    void Start()
    {
        // 获取当前物体上的Rigidbody组件
        rb = GetComponent<Rigidbody>();
        
        // ========== 刚体类型 ==========
        
        // isKinematic：是否为运动学刚体
        // false（默认）：物理控制，受重力影响，可被其他物体推动
        // true：手动控制位置/旋转，但仍然参与碰撞检测
        rb.isKinematic = false;
        rb.isKinematic = true;
        
        // ========== 质量 ==========
        
        // mass：刚体的质量（千克）
        // 质量越大，受力时产生的加速度越小
        // 建议：大物体质量大，小物体质量小
        rb.mass = 1f;
        
        // ========== 阻力 ==========
        
        // drag：线性阻力，控制物体在空气中移动时的减速
        // 值越大，停止越快。0=无阻力（真空）
        rb.drag = 0f;
        
        // angularDrag：旋转阻力，控制物体旋转时的减速
        // 值越大，旋转停止越快
        rb.angularDrag = 0.05f;
        
        // ========== 重力 ==========
        
        // useGravity：是否使用重力
        // true：受重力影响向下坠落
        // false：失重状态（太空场景）
        rb.useGravity = true;
        rb.useGravity = false;
        
        // ========== 约束 ==========
        
        // constraints：冻结变换，限制刚体的移动或旋转
        // FreezePositionX：锁定X轴移动（防止左右移动）
        // FreezePositionY：锁定Y轴移动（防止上下移动）
        // FreezePositionZ：锁定Z轴移动（防止前后移动）
        // FreezeRotationX：锁定X轴旋转
        // FreezeRotationY：锁定Y轴旋转
        // FreezeRotationZ：锁定Z轴旋转
        // FreezeRotation：锁定所有旋转
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezePositionX;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // ========== 插值（平滑移动） ==========
        
        // interpolation：减少快速移动时的抖动
        // Interpolate：在渲染帧之间插值（推荐）
        // Extrapolate：预测下一帧位置（可能更平滑但可能有误差）
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // ========== 碰撞检测模式 ==========
        
        // collisionDetectionMode：碰撞检测频率
        // Discrete（默认）：每帧检测一次，可能漏检高速物体
        // Continuous：连续检测，防止高速物体穿墙
        // ContinuousDynamic：动态连续检测，用于高速碰撞
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }
    
    // FixedUpdate：固定时间间隔调用（默认0.02秒）
    // 物理计算必须在这里进行，保证物理模拟的稳定性
    void FixedUpdate()
    {
        // ========== 施加力 ==========
        
        // AddForce：施加力到刚体
        // 需要在FixedUpdate中调用
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        // 创建一个力向量（水平+垂直，Y轴为0）
        Vector3 force = new Vector3(h, 0, v) * 10f;
        
        // 施加力：受质量影响，产生加速度
        rb.AddForce(force);
        
        // ========== ForceMode（力的模式） ==========
        
        // Force（默认）：持续力，考虑质量
        // 适用于：持续推动、引擎动力
        rb.AddForce(Vector3.up * 5, ForceMode.Force);
        
        // Impulse：瞬间冲量，相当于撞击
        // 适用于：跳跃、爆炸、击退
        rb.AddForce(Vector3.up * 5, ForceMode.Impulse);
        
        // Acceleration：持续力，忽略质量
        // 适用于：火箭推进（不考虑质量）
        rb.AddForce(Vector3.up * 5, ForceMode.Acceleration);
        
        // VelocityChange：瞬间速度改变，忽略质量
        // 适用于：子弹发射（瞬间达到目标速度）
        rb.AddForce(Vector3.up * 5, ForceMode.VelocityChange);
        
        // ========== 爆炸力 ==========
        
        // AddExplosionForce：模拟爆炸效果
        // 参数1：爆炸力大小
        // 参数2：爆炸中心位置
        // 参数3：爆炸半径
        rb.AddExplosionForce(500f, transform.position, 10f);
        
        // ========== 速度控制 ==========
        
        // velocity：直接设置刚体的速度向量
        // 立即生效，不考虑质量
        // 适用于：瞬移、强制移动
        rb.velocity = Vector3.forward * 10f;
        
        // 停止移动：设为零向量
        rb.velocity = Vector3.zero;
        
        // angularVelocity：设置旋转速度
        rb.angularVelocity = Vector3.zero;
        
        // ========== 运动学控制 ==========
        
        // MovePosition：带物理碰撞的运动学移动
        // 移动时会检测碰撞，比直接设置position更安全
        rb.MovePosition(transform.position + Vector3.forward * speed * Time.deltaTime);
        
        // MoveRotation：带物理碰撞的运动学旋转
        rb.MoveRotation(Quaternion.Euler(0, 90, 0));
    }
}
```

### CharacterController（角色控制器）

用于玩家角色

```csharp
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController cc;
    
    public float speed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    
    private Vector3 velocity;
    private bool isGrounded;
    
    void Start()
    {
        cc = GetComponent<CharacterController>();
    }
    
    void Update()
    {
        // 检测地面
        isGrounded = cc.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;  // 保持贴地
        }
        
        // 输入
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        
        // 移动
        cc.Move(move * speed * Time.deltaTime);
        
        // 跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        // 重力
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
}
```

### 练习 2.7

**练习题：**
1. 对比 Rigidbody.AddForce 和 Rigidbody.velocity 的区别及适用场景
2. 使用 CharacterController 实现一个带跳跃和重力的人物控制器

**答案：**
```csharp
// 1. AddForce vs velocity
// AddForce：施加持续力，考虑质量，产生加速度（物理真实）
//   适用：车辆、抛射物、需要物理模拟的对象
// velocity：直接设置速度，立即生效（无加速过程）
//   适用：角色移动、精确控制速度的场景

// 2. 角色控制器
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    private Vector3 velocity;
    private bool isGrounded;
    
    void Update()
    {
        isGrounded = cc.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;  // 保持贴地
        }
        
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        cc.Move(move * speed * Time.deltaTime);
        
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
}
```

### 相关知识点

- 2.8 碰撞检测 - 碰撞回调与物理系统的配合
- 4.3 状态机 - 角色状态管理与物理移动的结合

---

## 2.8 碰撞检测

#### 核心概念解析

**什么是碰撞检测？**
- 碰撞检测用于检测物体之间的**物理接触**
- Unity 使用**分离轴定理（SAT）**进行碰撞检测
- 需要 **Collider（碰撞体）** 配合使用

```
碰撞检测三要素：

┌─────────────────────────────────────────┐
│              GameObject                  │
│         ┌───────────────┐               │
│         │ Collider      │ ← 碰撞体形状   │
│         │ (碰撞检测)    │               │
│         └───────────────┘               │
│         ┌───────────────┐               │
│         │ Rigidbody     │ ← 物理模拟    │
│         │ (受力)        │  (至少一个)   │
│         └───────────────┘               │
└─────────────────────────────────────────┘
```

**Collider（碰撞体）类型**

| 类型 | 形状 | 适用场景 |
|------|------|----------|
| BoxCollider | 立方体 | 箱子、门、建筑 |
| SphereCollider | 球体 | 球、角色头部 |
| CapsuleCollider | 胶囊 | 角色身体 |
| MeshCollider | 任意网格 | 复杂地形、静态物体 |
| WheelCollider | 特殊 | 车辆轮子 |

**碰撞 vs 触发器**

| 特性 | 碰撞（Collision） | 触发器（Trigger） |
|------|-------------------|------------------|
| isTrigger | false | true |
| 物理效果 | 有碰撞，会弹开 | 无碰撞，穿过 |
| 回调方法 | OnCollisionEnter | OnTriggerEnter |
| 刚体要求 | 至少一个 | 至少一个 |
| 用途示例 | 墙壁、地面 | 奖励区域、传送门 |

```
碰撞和触发器的使用场景：

碰撞（OnCollision）：
┌─────────┐     ┌─────────┐
│  玩家   │────→│  墙壁   │  → 被挡住，无法穿过
└─────────┘     └─────────┘

触发器（OnTrigger）：
┌─────────┐     ┌─────────┐
│  玩家   │────→│ 金币    │  → 穿过，触发收集
└─────────┘     └─────────┘
```

**碰撞检测的 Layer 设置**
- 通过 Layer Collision Matrix 设置哪些层之间进行碰撞
- 避免不必要的碰撞检测，提高性能
- 例如：玩家与 UI 层不需要碰撞

### Collider（碰撞体）

```csharp
public class ColliderDemo : MonoBehaviour
{
    void Start()
    {
        Collider col = GetComponent<Collider>();
        
        // 启用/禁用碰撞
        col.enabled = false;
        
        // 触发器模式
        col.isTrigger = true;  // 设为触发器，不产生物理碰撞
        
        // 碰撞体大小
        BoxCollider box = col as BoxCollider;
        if (box != null)
        {
            box.size = new Vector3(1, 2, 1);
            box.center = Vector3.zero;
        }
        
        // 碰撞层（避免不必要的碰撞检测）
        // Edit > Project Settings > Physics > Layer Collision Matrix
        int layer = gameObject.layer;
    }
}
```

### 碰撞回调

```csharp
public class CollisionCallback : MonoBehaviour
{
    // 进入碰撞（两个物体都有 Collider + 至少一个 Rigidbody）
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"碰撞开始: {collision.gameObject.name}");
        
        // 获取碰撞信息
        ContactPoint[] contacts = collision.contacts;
        Vector3 point = contacts[0].point;      // 碰撞点
        Vector3 normal = contacts[0].normal;   // 碰撞法线
        Rigidbody rb = collision.rigidbody;     // 对方刚体
    }
    
    // 持续碰撞
    void OnCollisionStay(Collision collision)
    {
        // 每帧都在碰撞时执行
    }
    
    // 结束碰撞
    void OnCollisionExit(Collision collision)
    {
        Debug.Log($"碰撞结束: {collision.gameObject.name}");
    }
}
```

### 触发器回调

```csharp
public class TriggerCallback : MonoBehaviour
{
    // 进入触发器（至少一个物体有 Rigidbody）
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"触发进入: {other.name}");
        
        // 常见用途：收集金币、进入关卡
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家进入区域");
        }
    }
    
    // 持续触发
    void OnTriggerStay(Collider other)
    {
        // 每帧都在触发区域内
    }
    
    // 退出触发器
    void OnTriggerExit(Collider other)
    {
        Debug.Log($"触发退出: {other.name}");
    }
}
```

### 碰撞 vs 触发器

| 特性 | 碰撞 (Collision) | 触发器 (Trigger) |
|------|-----------------|------------------|
| IsTrigger | false | true |
| 物理效果 | 有碰撞，会弹开 | 无碰撞，穿过 |
| 回调方法 | OnCollisionEnter | OnTriggerEnter |
| 需要 Rigidbody | 至少一个 | 至少一个 |

### 练习 2.8

**练习题：**
1. 编写代码实现：玩家触碰金币时，金币消失并播放音效
2. 解释 OnCollisionEnter 和 OnTriggerEnter 的区别及各自适用场景

**答案：**
```csharp
// 1. 金币收集
public class Coin : MonoBehaviour
{
    public AudioClip collectSound;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 播放音效
            other.GetComponent<AudioSource>().PlayOneShot(collectSound);
            
            // 增加分数
            GameManager.Instance.AddScore(10);
            
            // 销毁金币
            Destroy(gameObject);
        }
    }
}

// 2. 区别
// OnCollisionEnter：物理碰撞，两个物体都会受到力
//   适用：墙壁、地面、需要物理阻挡的场景
// OnTriggerEnter：触发器，物体可以穿过
//   适用：金币、传送门、区域检测等
```

### 相关知识点

- 2.7 物理系统 - Collider需要配合Rigidbody使用
- 4.2 观察者模式 - 金币收集可通知分数系统

---

## 2.9 音频系统

#### 核心概念解析

**什么是Unity音频系统？**
- Unity音频系统负责游戏中所有声音的处理
- 包括背景音乐、音效、3D空间音效等功能
- 通过AudioSource和AudioListener组件配合实现

```
Unity 音频系统架构：

┌─────────────────────────────────────────────────────────┐
│                    音频系统                              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   AudioClip（音频文件）                                  │
│       ↓                                                 │
│   AudioSource（播放组件）←→ AudioListener（聆听者）      │
│       ↓                                                 │
│   输出到扬声器                                          │
│                                                         │
│   AudioSource：负责播放声音                              │
│   AudioListener：接收声音，通常挂载在主相机              │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**2D音效 vs 3D音效**

| 特性 | 2D音效 | 3D音效 |
|------|--------|--------|
| spatialBlend | 0 | 1 |
| 空间感 | 无，始终相同音量 | 根据距离和方向变化 |
| 应用场景 | 背景音乐、UI音效 | 脚步声、枪声、环境音 |

**音频在游戏开发中的重要性**
- 沉浸感：好的音效让游戏更真实
- 反馈：操作音效提供交互反馈
- 性能：注意音频文件的压缩和加载策略

---

### AudioSource（音频源）

```csharp
public class AudioDemo : MonoBehaviour
{
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // 播放
        audioSource.Play();
        audioSource.PlayDelayed(2f);  // 延迟播放（秒）
        
        // 暂停/继续
        audioSource.Pause();
        audioSource.UnPause();
        
        // 停止
        audioSource.Stop();
        
        // 播放状态
        bool isPlaying = audioSource.isPlaying;
        
        // 循环
        audioSource.loop = true;
        
        // 音量
        audioSource.volume = 0.5f;  // 0~1
        
        // 音调
        audioSource.pitch = 1f;      // 1=正常，0.5=慢，2=快
        
        // 3D 音效
        audioSource.spatialBlend = 1f;  // 1=3D，0=2D
        
        // 播放并获取剪辑时长
        audioSource.clip = audioClip;
        Debug.Log(audioSource.clip.length);
    }
    
    // 动态播放音效
    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
    
    public void PlaySoundWithVolume(AudioClip clip, float volume)
    {
        audioSource.PlayOneShot(clip, volume);
    }
}
```

### AudioListener（音频监听器）

```csharp
// 音频监听器通常挂载在主相机上
// 每个场景只能有一个 AudioListener
// Unity 会自动在主相机上添加
```

### 练习 2.9

**练习题：**
1. 编写脚本：根据玩家移动速度在 Idle 和 Run 动画之间切换
2. 实现：播放攻击动画时禁止移动，动画结束后恢复移动

**答案：**
```csharp
// 1. 速度切换动画
public Animator animator;
public float speed;

void Update()
{
    float speed = Input.GetAxis("Vertical");
    animator.SetFloat("Speed", Mathf.Abs(speed));
}

// 2. 攻击动画状态机
public class PlayerAnimator : MonoBehaviour
{
    public Animator animator;
    public CharacterController cc;
    
    void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            cc.enabled = false;  // 攻击时禁止移动
        }
        else
        {
            cc.enabled = true;
        }
    }
}
```

### 相关知识点

- 4.3 状态机 - 动画状态机是状态模式的Unity实现
- 4.2 观察者 - 动画事件可通知其他系统

---

## 2.10 动画系统

#### 核心概念解析

**什么是Unity动画系统？**
- Unity动画系统（Animator）用于控制角色的动作和动画状态切换
- 基于状态机（State Machine）实现不同动画状态之间的转换
- 支持动画混合（Animation Blending）实现平滑过渡

```
Unity 动画系统架构：

┌─────────────────────────────────────────────────────────┐
│                  Animator 组件                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   ┌─────────┐    ┌─────────┐    ┌─────────┐           │
│   │  Idle   │───→│  Run    │───→│  Jump   │           │
│   │ 待机动画│    │ 奔跑动画│    │ 跳跃动画│           │
│   └─────────┘    └─────────┘    └─────────┘           │
│       ↑              ↓               ↑                 │
│       └──────────────┴───────────────┘                 │
│                     状态机                              │
│                                                         │
│   动画参数：Float、Bool、Int、Trigger                   │
└─────────────────────────────────────────────────────────┘
```

**Animator vs Animation**
- Animation：旧版系统，只能播放单个动画
- Animator：新版系统，基于状态机，支持动画混合和分层

**动画层（Animation Layers）的应用**
- 身体不同部位可以独立播放动画
- 上半身攻击动画 + 下半身移动动画
- 权重控制各层动画的混合程度

---

### Animator 组件

```csharp
public class AnimationDemo : MonoBehaviour
{
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        
        // 获取动画参数 ID（性能更好）
        // 在 Animator 窗口创建的参数会有对应的 ID
    }
    
    // 状态切换触发
    void Update()
    {
        float speed = Input.GetAxis("Vertical");
        
        // 设置浮点参数
        animator.SetFloat("Speed", Mathf.Abs(speed));
        
        // 设置布尔参数
        animator.SetBool("IsGrounded", true);
        
        // 设置整数参数
        animator.SetInteger("Combo", 1);
        
        // 设置触发（自动重置为 false）
        animator.SetTrigger("Jump");
        
        // 获取当前状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isIdle = stateInfo.IsName("Idle");
        bool isRunning = stateInfo.IsName("Running");
        float progress = stateInfo.normalizedTime;  // 0~1
    }
}
```

### 动画层

```csharp
void AnimationLayerDemo()
{
    // 设置层级权重
    animator.SetLayerWeight(1, 1f);  // 上半身动画层
    
    // 获取层级索引
    int layerIndex = animator.GetLayerIndex("UpperBody");
}
```

### 练习 2.10

**练习题：**
1. 使用 Animator Controller 创建一个包含 Idle、Run、Jump 三个状态的状态机
2. 说明动画层（Animation Layers）的作用，并实现：下半身播放移动动画，上半身播放攻击动画

**答案：**
```csharp
// 2. 动画层实现
// 在Animator中创建两个层：
// - Base Layer：放置Idle、Run、Jump（权重1）
// - UpperBody Layer：放置Attack（权重1）

// 代码控制
void Update()
{
    // 行走时切换到Run
    if (Input.GetAxis("Horizontal") != 0)
    {
        animator.SetBool("IsMoving", true);
    }
    
    // 攻击时设置上层权重
    if (Input.GetMouseButtonDown(0))
    {
        animator.SetTrigger("Attack");
    }
}
```

### 相关知识点

- 4.3 状态机模式 - Animator是状态机的Unity实现
- 2.11 UI - UI按钮可触发动画

---

## 2.11 UI 系统

#### 核心概念解析

**什么是Unity UI系统？**
- Unity UI系统（Canvas）是游戏界面的核心
- 用于创建游戏菜单、HUD（抬头显示）、对话框等用户界面
- 基于RectTransform实现灵活的布局和定位

```
Unity UI 系统架构：

┌─────────────────────────────────────────────────────────┐
│                    Canvas（画布）                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   ┌─────────────────────────────────────────────┐      │
│   │           RectTransform                      │      │
│   │  ┌─────────┐  ┌─────────┐  ┌─────────┐     │      │
│   │  │  Text   │  │ Button  │  │ Image   │     │      │
│   │  └─────────┘  └─────────┘  └─────────┘     │      │
│   └─────────────────────────────────────────────┘      │
│                                                         │
│   Canvas Render Mode:                                   │
│   - Screen Space Overlay: 覆盖在屏幕上                   │
│   - Screen Space Camera: 跟随相机                        │
│   - World Space: 世界空间（3D UI）                       │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Canvas的三种渲染模式**

| 模式 | 特点 | 适用场景 |
|------|------|----------|
| Screen Space - Overlay | 最上层，不受相机影响 | 2D游戏、菜单 |
| Screen Space - Camera | 跟随相机，可添加特效 | 3D游戏HUD |
| World Space | 在3D空间中放置 | 3D场景中的界面 |

**UI开发要点**
- Canvas Scaler：适配不同分辨率
- 事件系统：处理点击、拖拽等交互
- 布局组件：自动排列UI元素

---

### Canvas 画布

```csharp
public class UIDemo : MonoBehaviour
{
    public Canvas canvas;
    public RectTransform uiElement;
    
    void Start()
    {
        // Canvas 渲染模式
        // Screen Space - Overlay: 覆盖在屏幕上
        // Screen Space - Camera: 跟随相机
        // World Space: 世界空间
        
        // 获取屏幕坐标
        Vector2 screenPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out screenPoint
        );
        
        // UI 元素坐标转换
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiElement,
            Input.mousePosition,
            null,  // Screen Space Overlay 不需要 camera
            out localPoint
        );
    }
}
```

### 常用 UI 组件

```csharp
public class UIComponentsDemo : MonoBehaviour
{
    // Text (Legacy 或 TextMeshPro)
    public Text legacyText;
    public TMPro.TextMeshProUGUI tmpText;
    
    // Image
    public Image image;
    public UnityEngine.UI.RawImage rawImage;
    
    // Button
    public UnityEngine.UI.Button button;
    
    // Slider
    public UnityEngine.UI.Slider slider;
    
    // Toggle
    public UnityEngine.UI.Toggle toggle;
    
    // InputField
    public UnityEngine.UI.InputField inputField;
    
    void Start()
    {
        // Text
        legacyText.text = "Hello";
        legacyText.color = Color.white;
        
        tmpText.text = "Hello TMP";
        
        // Image
        image.sprite = someSprite;
        image.color = new Color(1, 1, 1, 0.5f);  // 半透明
        
        // Button 点击事件
        button.onClick.AddListener(OnButtonClick);
        
        // Slider 值变化
        slider.onValueChanged.AddListener(OnSliderChanged);
        
        // Toggle 状态变化
        toggle.onValueChanged.AddListener(OnToggleChanged);
        
        // InputField 内容变化
        inputField.onValueChanged.AddListener(OnInputChanged);
    }
    
    void OnButtonClick()
    {
        Debug.Log("按钮点击");
    }
    
    void OnSliderChanged(float value)
    {
        Debug.Log($"滑块值: {value}");
    }
    
    void OnToggleChanged(bool isOn)
    {
        Debug.Log($"开关: {isOn}");
    }
    
    void OnInputChanged(string text)
    {
        Debug.Log($"输入: {text}");
    }
}
```

### UI 布局

```csharp
// Horizontal Layout Group
// Vertical Layout Group
// Grid Layout Group
// Content Size Fitter
// Aspect Ratio Fitter

public class LayoutDemo : MonoBehaviour
{
    // 动态添加 UI 元素
    public GameObject itemPrefab;
    public Transform contentParent;
    
    void AddItems()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject item = Instantiate(itemPrefab, contentParent);
            item.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"Item {i}";
        }
    }
}
```

### 练习 2.11

**练习题：**
1. 创建一个血条UI：当玩家受伤时，血条减少并更新显示
2. 实现：点击按钮开始游戏，点击Settings打开设置面板，点击Close关闭面板

**答案：**
```csharp
// 1. 血条系统
public class HealthBar : MonoBehaviour
{
    public Image healthFillImage;
    public Text healthText;
    private int maxHealth = 100;
    private int currentHealth;
    
    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();
    }
    
    void UpdateHealthBar()
    {
        float fillAmount = (float)currentHealth / maxHealth;
        healthFillImage.fillAmount = fillAmount;
        healthText.text = $"{currentHealth}/{maxHealth}";
    }
}

// 2. 面板切换
public class UIManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    
    public void OnStartButton()
    {
        mainMenuPanel.SetActive(false);
        // 开始游戏逻辑
    }
    
    public void OnSettingsButton()
    {
        settingsPanel.SetActive(true);
    }
    
    public void OnCloseButton()
    {
        settingsPanel.SetActive(false);
    }
}
```

### 相关知识点

- 4.6 MVC架构 - UI可按MVC模式分离
- 4.2 观察者模式 - 血量变化通过事件通知UI更新

---

## 2.12 射线检测

#### 核心概念解析

**什么是射线检测？**
- 射线检测（Raycast）是从一点向指定方向发射一条射线，检测是否与物体碰撞
- 常用于：点击选中物体、检测视线、拾取物品、AI视野检测

### Physics.Raycast

```csharp
public class RaycastDemo : MonoBehaviour
{
    void Update()
    {
        // 从摄像机到鼠标位置发射射线
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"点击了: {hit.collider.gameObject.name}");
            }
        }
        
        // 从物体位置向前发射射线
        Ray forwardRay = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(forwardRay, out RaycastHit hitInfo, 10f))
        {
            Debug.Log($"前方有物体: {hitInfo.collider.name}");
        }
        
        // 使用LayerMask过滤
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int mask = 1 << enemyLayer;
        if (Physics.Raycast(ray, out hit, 100f, mask))
        {
            Debug.Log($"检测到敌人");
        }
    }
}
```

### 射线检测技巧

```csharp
public class RaycastTips : MonoBehaviour
{
    // 所有碰撞结果
    void RaycastAll()
    {
        Ray ray = transform.forward * 10f;
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward);
        
        foreach (var hit in hits)
        {
            Debug.Log($"碰撞: {hit.collider.name}");
        }
    }
    
    // 球形射线检测
    void SphereCast()
    {
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, 0.5f, transform.forward, out hit, 10f))
        {
            Debug.Log($"球形检测: {hit.collider.name}");
        }
    }
    
    // 2D射线检测
    void Raycast2D()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right);
        if (hit.collider != null)
        {
            Debug.Log($"2D碰撞: {hit.collider.name}");
        }
    }
}
```

### 练习 2.12

**练习题：**
1. 编写一个FPS射击系统：点击鼠标发射射线，击中敌人时造成伤害
2. 实现：检测玩家前方是否有障碍物阻挡视线

**答案：**
```csharp
// 1. FPS射击系统
public class FPShooter : MonoBehaviour
{
    public float damage = 10f;
    public float range = 100f;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }
    
    void Shoot()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, range))
        {
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}

// 2. 视线遮挡检测
public bool HasLineOfSight(Vector3 targetPosition)
{
    Vector3 direction = targetPosition - transform.position;
    float distance = direction.magnitude;
    
    RaycastHit hit;
    if (Physics.Raycast(transform.position, direction, out hit, distance))
    {
        return false;  // 有障碍物遮挡
    }
    return true;
}
```

### 相关知识点

- 2.8 碰撞检测 - 射线检测也是碰撞检测的一种
- 2.7 物理系统 - 射线检测依赖Collider

---

## 2.13 相机控制

#### 核心概念解析

**什么是相机控制？**
- 相机控制决定玩家能看到什么
- 常见类型：跟随相机、第一人称相机、俯视相机、轨道相机

### 简单跟随相机

```csharp
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -10);
    public float smoothSpeed = 0.125f;
    
    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        transform.position = smoothedPosition;
        transform.LookAt(target);
    }
}
```

### 平滑阻尼跟随

```csharp
public class SmoothFollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 3, -6);
    public float smoothTime = 0.3f;
    
    private Vector3 velocity;
    
    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 targetPosition = target.position + offset;
        
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
        
        transform.LookAt(target);
    }
}
```

### 第一人称相机

```csharp
public class FirstPersonCamera : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    
    private float xRotation = 0f;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
```

### 练习 2.13

**练习题：**
1. 实现第三人称相机：鼠标控制视角旋转，滚轮调整距离
2. 实现相机碰撞检测：与障碍物距离过近时自动拉近

**答案：**
```csharp
// 1. 第三人称相机
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float mouseSensitivity = 100f;
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 10f;
    
    private float currentX = 0f;
    private float currentY = 20f;
    
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            currentX += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            currentY = Mathf.Clamp(currentY, -30, 60);
        }
        
        distance -= Input.mouseScrollDelta.y;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }
    
    void LateUpdate()
    {
        Vector3 direction = new Vector3(0, 0, -distance);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        transform.position = target.position + rotation * direction;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}

// 2. 相机碰撞检测
public class CameraCollision : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float minDistance = 1f;
    
    void LateUpdate()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        
        RaycastHit hit;
        if (Physics.Raycast(target.position, -direction, out hit, distance))
        {
            transform.position = target.position - direction * Mathf.Max(hit.distance, minDistance);
        }
        else
        {
            transform.position = target.position - direction * distance;
        }
    }
}
```

### 相关知识点

- 2.3 Transform - 相机移动通过Transform控制
- 2.6 生命周期 - 相机必须在LateUpdate中更新

---

## 2.14 粒子系统

#### 核心概念解析

**什么是粒子系统？**
- 粒子系统用于创建动态效果：火焰、烟雾、爆炸、雪花等
- 由发射器、粒子、渲染器组成

### 代码控制粒子系统

```csharp
public class ParticleSystemDemo : MonoBehaviour
{
    private ParticleSystem ps;
    
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        
        ps.Play();
        ps.Pause();
        ps.Stop();
        ps.Play();
        
        var main = ps.main;
        main.loop = true;
        
        var emission = ps.emission;
        emission.rateOverTime = 50;
        
        var startSize = ps.main;
        startSize.startSize = 0.5f;
        
        var startColor = ps.main;
        startColor.startColor = Color.red;
    }
}
```

### 粒子系统技巧

```csharp
public class ParticleControl : MonoBehaviour
{
    public ParticleSystem ps;
    
    // 一次性发射
    public void EmitBurst(int count)
    {
        var emitParams = new ParticleSystem.EmitParams();
        ps.Emit(emitParams, count);
    }
    
    // 获取当前粒子数
    public int GetParticleCount()
    {
        return ps.particleCount;
    }
}
```

### 练习 2.14

**练习题：**
1. 实现点击地面生成爆炸特效，1秒后自动销毁
2. 实现角色移动时脚下产生尘土粒子

**答案：**
```csharp
// 1. 点击生成爆炸
public class ExplosionEffect : MonoBehaviour
{
    public ParticleSystem explosionPrefab;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                ParticleSystem ps = Instantiate(explosionPrefab, hit.point, Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, 1f);
            }
        }
    }
}

// 2. 角色尘土粒子
public class PlayerDust : MonoBehaviour
{
    public ParticleSystem dustParticles;
    public CharacterController cc;
    
    void Update()
    {
        if (cc.isGrounded && Input.GetAxis("Horizontal") != 0)
        {
            if (!dustParticles.isPlaying)
                dustParticles.Play();
        }
        else
        {
            dustParticles.Stop();
        }
    }
}
```

### 相关知识点

- 3.5 对象池技术 - 大量粒子应使用对象池优化
- 4.8 享元模式 - 粒子系统是享元模式的典型应用

---

## 本阶段练习

### 基础练习

1. 创建一个可控制移动的玩家
2. 实现跳跃功能
3. 制作一个可收集的金币

### 进阶练习

1. 实现敌人 AI 追踪
2. 制作血条 UI
3. 添加背景音乐和音效

### 额外补充章节

## 2.15 NavMesh 导航系统

### NavMeshAgent

```csharp
using UnityEngine.AI;

public class NavMeshDemo : MonoBehaviour
{
    private NavMeshAgent agent;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(new Vector3(10, 0, 10));
        agent.speed = 3.5f;
    }
    
    void Update()
    {
        if (agent.remainingDistance < 0.5f)
            Debug.Log("到达目的地");
    }
}
```

### 练习 2.15

**练习题：**
1. 实现：点击地面，角色自动寻路到达点击位置
2. 实现：敌人AI巡逻，发现玩家时追击

**答案：**
```csharp
public class ClickToMove : MonoBehaviour
{
    private NavMeshAgent agent;
    void Start() => agent = GetComponent<NavMeshAgent>();
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
                agent.SetDestination(hit.point);
        }
    }
}
```

### 相关知识点

- 2.7 物理系统 - 导航系统与物理系统配合

---

## 2.16 时间与工具类

### Time 类深入

```csharp
void Update()
{
    float time = Time.time;
    float deltaTime = Time.deltaTime;
    Time.timeScale = 0.5f;  // 慢动作
}
```

### Mathf 常用函数

```csharp
float a = Mathf.Lerp(0, 10, Time.deltaTime);
float b = Mathf.Clamp(value, 0, 10);
```

### 练习 2.16

**练习题：**
1. 实现：子弹时间效果（按下空格键时间变慢，松开恢复）
2. 使用 Mathf 实现钟摆效果

**答案：**
```csharp
public class BulletTime : MonoBehaviour
{
    public float slowTimeScale = 0.2f;
    void Update()
        Time.timeScale = Input.GetKey(KeyCode.Space) ? slowTimeScale : 1f;
}
```

### 相关知识点

- 2.6 生命周期 - 时间在Update中使用

---

## 2.17 场景管理

### SceneManager

```csharp
using UnityEngine.SceneManagement;

void LoadScene() => SceneManager.LoadScene("Level1");

IEnumerator LoadSceneAsync()
{
    AsyncOperation op = SceneManager.LoadSceneAsync("Level1");
    yield return op;
}
```

### 练习 2.17

**练习题：**
1. 实现：Loading界面显示加载进度条
2. 实现：按Escape键暂停游戏

**答案：**
```csharp
public class LoadingScreen : MonoBehaviour
{
    public Slider progressBar;
    void Start() => StartCoroutine(LoadScene());
    IEnumerator LoadScene()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("Level1");
        while (!op.isDone) { progressBar.value = op.progress; yield return null; }
    }
}
```

### 相关知识点

- 3.4 资源加载 - 场景加载也是资源加载的一部分

---

## 相关学习链接

- ← 上一阶段：[阶段一_CSharp基础.md](./阶段一_CSharp基础.md)
- → 下一阶段：[阶段三_Unity进阶.md](./阶段三_Unity进阶.md)
- 📺 配套视频：[视频教程汇总.md](./视频教程汇总.md)
- 📝 面试题库：[面试题库.md](./面试题库.md)
- ⚡ 面试突击：[面试突击计划.md](./面试突击计划.md)

> 第二阶段完成！下一步将学习 Unity 进阶内容。
## 对照学习点与练习
- 对照点：Stage 2 的 GameObject/Component、Transform、Prefab、输入、生命周期等，参见逐条模板中阶段映射。
- 对照练习：实现一个简单的玩家移动、碰撞检测、以及一个小型 UI 原型，使用 Stage 3 的资源加载模板进行扩展。
- 资源与事件对照：将 Resources、Prefab 实例化、以及事件驱动的输入绑定做对照练习。
- 练习建议：将逐条模板中的 4-6 条练习包括在阶段2的场景中进行初步实现，作为对照练习。
