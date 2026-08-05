# Unity 游戏开发工程师面试题库

> 本题库包含 C# 基础、Unity 核心、数据结构与算法三大板块，助你顺利通过游戏开发面试

---

## 目录

1. [C# 基础](#1-c-基础)
2. [Unity 核心](#2-unity-核心)
3. [数据结构与算法](#3-数据结构与算法)
4. [游戏开发实战](#4-游戏开发实战)

---

## 1. C# 基础

### 1.1 面向对象编程

#### Q1: 什么是多态？C# 中如何实现多态？

**解答：**

多态（Polymorphism）是面向对象编程的三大特性之一（封装、继承、多态），指同一个接口表现出不同的行为。

**C# 实现方式：**

1. **虚方法 (Virtual Method)**
```csharp
public class Animal
{
    public virtual void Speak()
    {
        Console.WriteLine("Animal speaks");
    }
}

public class Dog : Animal
{
    public override void Speak()
    {
        Console.WriteLine("Dog barks");
    }
}

public class Cat : Animal
{
    public override void Speak()
    {
        Console.WriteLine("Cat meows");
    }
}

// 使用
Animal animal = new Dog();
animal.Speak(); // 输出 "Dog barks"
```

2. **接口 (Interface)**
```csharp
public interface IDamageable
{
    void TakeDamage(int damage);
}

public class Player : IDamageable
{
    public void TakeDamage(int damage)
    {
        Health -= damage;
    }
}

public class Enemy : IDamageable
{
    public void TakeDamage(int damage)
    {
        HP -= damage;
    }
}
```

3. **抽象类 (Abstract Class)**
```csharp
public abstract class Weapon
{
    public abstract void Attack(); // 抽象方法，子类必须实现
    
    public void ShowInfo() // 具体方法
    {
        Console.WriteLine("This is a weapon");
    }
}
```

---

#### Q2: 解释 sealed 关键字的作用

**解答：**

`sealed` 关键字用于阻止类被继承或方法被重写：

```csharp
// 密封类，不能被继承
public sealed class GameManager
{
    private static GameManager instance;
    public static GameManager Instance => instance ??= new GameManager();
}

// 密封方法，不能被重写
public class BasePlayer
{
    public virtual void Move() { }
    
    public sealed override void TakeDamage() { } // 阻止子类重写
}
```

**使用场景：**
- 单例模式中密封类
- 不想被扩展的类
- 性能优化（密封类 JIT 更容易内联）

---

#### Q3: 什么是依赖注入？Unity 中如何实现？

**解答：**

依赖注入（DI）是一种设计模式，通过外部注入依赖对象，减少类之间的耦合度。

**Unity 中的实现方式：**

1. **构造函数注入**
```csharp
public class PlayerController
{
    private readonly IHealthSystem healthSystem;
    private readonly IInventory inventory;
    
    public PlayerController(IHealthSystem health, IInventory inv)
    {
        healthSystem = health;
        inventory = inv;
    }
}
```

2. **属性注入**
```csharp
public class UIManager
{
    [SerializeField] private GameObject inventoryPanel;
    
    public GameObject InventoryPanel => inventoryPanel;
}
```

3. **接口 + 简单容器**
```csharp
public class DIContainer
{
    private static Dictionary<Type, object> services = new Dictionary<Type, object>();
    
    public static void Register<T>(T service) where T : class
    {
        services[typeof(T)] = service;
    }
    
    public static T Resolve<T>() where T : class
    {
        return services[typeof(T)] as T;
    }
}
```

---

### 1.2 内存管理

#### Q4: 什么是 GC（垃圾回收）？C# 中如何优化内存？

**解答：**

.NET 的垃圾回收器自动管理内存，回收不再使用的对象。

**GC 分代机制：**
- **Gen 0**: 新建对象，频繁回收
- **Gen 1**: 存活时间短的对象
- **Gen 2**: 存活时间长的对象（大对象）

**内存优化策略：**

```csharp
// 1. 避免频繁创建对象
// 不好
for (int i = 0; i < 1000; i++)
{
    string s = new string('a', 10); // 每循环创建新字符串
}

// 好
string s = new string('a', 10);
for (int i = 0; i < 1000; i++)
{
    // 复用 s
}

// 2. 使用 StringBuilder 处理字符串拼接
StringBuilder sb = new StringBuilder();
for (int i = 0; i < 1000; i++)
{
    sb.Append(i);
}
string result = sb.ToString();

// 3. 对象池
public class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> pool = new Stack<T>();
    
    public T Get()
    {
        return pool.Count > 0 ? pool.Pop() : new T();
    }
    
    public void Return(T obj)
    {
        pool.Push(obj);
    }
}

// 4. 使用 struct 替代 class（值类型，在栈上）
struct Point3D
{
    public float X, Y, Z;
}

// 5. 及时清理资源
void OnDestroy()
{
    if (texture != null)
    {
        Destroy(texture);
    }
}
```

---

#### Q5: ref 和 out 的区别？

**解答：**

| 特性 | ref | out |
|------|-----|-----|
| 初始化 | 必须先赋值 | 不需要赋值（但方法内必须赋值） |
| 传入方向 | 双向（传入+传出） | 单向（仅传出） |
| 编译要求 | 调用前必须初始化 | 调用前不需要初始化 |

```csharp
// ref 示例
void RefExample(ref int value)
{
    value = value * 2; // 使用传入的值
}

int num = 10;
RefExample(ref num); // num 变为 20

// out 示例
void OutExample(out int value)
{
    value = 100; // 必须赋值
}

int num;
OutExample(out num); // num 为 100
```

**Unity 使用场景：**
- `TryGetComponent` 使用 out
- 自定义方法需要返回多个值时

---

### 1.3 泛型

#### Q6: 什么是泛型？使用泛型有什么优势？

**解答：**

泛型允许编写可重用的代码，同时保持类型安全。

**优势：**

1. **类型安全**：编译时检查类型错误
2. **性能提升**：避免装箱拆箱
3. **代码复用**：一套代码处理多种类型

```csharp
// 泛型类
public class Cache<T>
{
    private Dictionary<string, T> cache = new Dictionary<string, T>();
    
    public void Set(string key, T value)
    {
        cache[key] = value;
    }
    
    public T Get(string key)
    {
        return cache.TryGetValue(key, out T value) ? value : default;
    }
}

// 使用
Cache<int> intCache = new Cache<int>();
Cache<string> strCache = new Cache<string>();

// 泛型约束
public class MonoSingleton<T> where T : MonoBehaviour
{
    private static T instance;
    public static T Instance => instance ??= FindObjectOfType<T>();
}

// 泛型方法
public static T[] Shuffle<T>(T[] array)
{
    for (int i = array.Length - 1; i > 0; i--)
    {
        int j = Random.Range(0, i + 1);
        (array[i], array[j]) = (array[j], array[i]);
    }
    return array;
}
```

---

### 1.4 异步编程

#### Q7: async/await 是什么？和 Thread 的区别？

**解答：**

`async/await` 是 C# 的异步编程模型，让异步代码像同步代码一样易读。

**与 Thread 的区别：**

| 特性 | async/await | Thread |
|------|-------------|--------|
| 资源 | 线程池线程 | 新建线程 |
| 阻塞 | 不阻塞线程 | 阻塞线程 |
| 适用 | I/O 操作 | CPU 密集型 |

```csharp
// 异步方法
public async Task<string> LoadTextureAsync(string path)
{
    using (UnityWebRequest request = UnityWebRequest.Get(path))
    {
        await request.SendWebRequest();
        return request.downloadHandler.text;
    }
}

// 调用
async void LoadData()
{
    string result = await LoadTextureAsync("path");
    Debug.Log(result);
}

// Task.Run 用于 CPU 密集型
public async Task<int> CalculateHeavyAsync()
{
    return await Task.Run(() => 
    {
        int sum = 0;
        for (int i = 0; i < 1000000; i++) sum += i;
        return sum;
    });
}
```

---

### 1.5 委托与事件

#### Q8: 解释委托和事件的区别

**解答：**

- **委托（Delegate）**：类型安全的函数指针
- **事件（Event）**：封装了委托，限制外部随意调用

```csharp
// 委托声明
public delegate void DamageEventHandler(int damage);

// 事件声明
public class Player
{
    // 事件
    public event DamageEventHandler OnDamage;
    
    private int health = 100;
    
    public void TakeDamage(int damage)
    {
        health -= damage;
        // 触发事件（只有类内部能触发）
        OnDamage?.Invoke(damage);
    }
}

// 订阅事件
public class GameUI
{
    public void OnPlayerDamage(int damage)
    {
        Debug.Log($"Player took {damage} damage!");
    }
}

// 在某处订阅
player.OnDamage += ui.OnPlayerDamage;
player.OnDamage -= ui.OnPlayerDamage; // 取消订阅
```

---

## 2. Unity 核心

### 2.1 生命周期

#### Q9: Unity 脚本的生命周期顺序？

**解答：**

```
Awate → OnEnable → Start → FixedUpdate
       ↓
    Update (每帧) → LateUpdate
       ↓
    OnDisable → OnDestroy
```

**详细说明：**

```csharp
public class LifecycleDemo : MonoBehaviour
{
    // 1. 唤醒 - 对象创建时调用（即使脚本禁用）
    void Awake()
    {
        Debug.Log("Awake");
    }
    
    // 2. 启用 - 脚本变为可用时调用
    void OnEnable()
    {
        Debug.Log("OnEnable");
    }
    
    // 3. 开始 - 第一帧更新前调用
    void Start()
    {
        Debug.Log("Start");
    }
    
    // 4. 固定更新 - 固定时间间隔（物理）
    void FixedUpdate()
    {
        // 物理计算，0.02秒/次
    }
    
    // 5. 更新 - 每帧调用（游戏逻辑）
    void Update()
    {
        // 输入检测
        // 游戏逻辑
    }
    
    // 6. 延迟更新 - 所有 Update 后调用（相机跟随）
    void LateUpdate()
    {
        // 相机跟随
    }
    
    // 7. 禁用 - 脚本变为禁用时调用
    void OnDisable()
    {
        Debug.Log("OnDisable");
    }
    
    // 8. 销毁 - 对象销毁时调用
    void OnDestroy()
    {
        Debug.Log("OnDestroy");
    }
}
```

**Unity 6 新增：**
- `OnBeforeRender`: 渲染前
- `OnAfterRender`: 渲染后

---

#### Q10: Update、FixedUpdate、LateUpdate 的区别？

**解答：**

| 方法 | 调用时机 | 帧率 | 用途 |
|------|----------|------|------|
| Update | 每帧 | 可变 | 输入、游戏逻辑 |
| FixedUpdate | 固定时间 | 固定(0.02s) | 物理计算 |
| LateUpdate | Update 后 | 可变 | 相机跟随 |

```csharp
public class MovementExample : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 targetPosition;
    
    void Update()
    {
        // 输入检测（可变帧率）
        if (Input.GetKeyDown(KeyCode.W))
        {
            targetPosition += Vector3.forward;
        }
    }
    
    void FixedUpdate()
    {
        // 物理操作（固定帧率）
        rb.MovePosition(targetPosition);
    }
    
    void LateUpdate()
    {
        // 相机跟随（必须最后）
        transform.position = player.position + offset;
    }
}
```

---

### 2.2 组件系统

#### Q11: Transform 最重要的属性和方法？

**解答：**

Transform 是 Unity 中最常用的组件。

**核心属性：**
```csharp
transform.position      // 世界坐标
transform.localPosition // 局部坐标
transform.rotation      // 世界旋转（四元数）
transform.localRotation // 局部旋转
transform.localScale    // 局部缩放
transform.forward       // 前方向（蓝色轴）
transform.right         // 右方向（红色轴）
transform.up            // 上方向（绿色轴）
transform.parent        // 父Transform
transform.childCount   // 子对象数量
```

**核心方法：**
```csharp
// 移动
transform.Translate(Vector3.forward * speed * Time.deltaTime);
transform.position = Vector3.Lerp(a, b, t);

// 旋转
transform.Rotate(Vector3.up, 90);
transform.rotation = Quaternion.Euler(0, 90, 0);

// 朝向
transform.LookAt(target);

// 层级操作
transform.SetParent(parent);
transform.SetSiblingIndex(0);
transform.GetChild(0);
transform.Find("ChildName");
```

---

#### Q12: GetComponent 的区别和性能优化？

**解答：**

| 方法 | 作用 |
|------|------|
| GetComponent<T>() | 获取同物体上的组件 |
| GetComponents<T>() | 获取所有指定组件 |
| GetComponentInChildren<T>() | 在子物体查找 |
| GetComponentInParent<T>() | 在父物体查找 |

**性能优化：**

```csharp
public class BadExample : MonoBehaviour
{
    void Update()
    {
        // ❌ 每帧调用，开销大
        GetComponent<Rigidbody>().velocity = Vector3.forward;
    }
}

public class GoodExample : MonoBehaviour
{
    private Rigidbody rb;
    
    void Awake()
    {
        // ✅ 缓存引用
        rb = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        rb.velocity = Vector3.forward;
    }
}

// Unity 6 新语法
public class Unity6Example : MonoBehaviour
{
    private readonly Rigidbody rb = GetComponent<Rigidbody>();
}
```

---

### 2.3 物理系统

#### Q13: Rigidbody 的类型和区别？

**解答：**

| 类型 | 特性 | 适用场景 |
|------|------|----------|
| Dynamic | 受物理影响，可动 | 玩家、抛射物 |
| Kinematic | 运动学，不受力但可动 | 移动平台、门 |
| Static | 静态，不动 | 墙壁、地面 |

```csharp
public class PhysicsExample : MonoBehaviour
{
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    // 施加力（持续效果）
    void FixedUpdate()
    {
        rb.AddForce(Vector3.forward * 10);
    }
    
    // 施加冲量（瞬间效果）
    public void Jump()
    {
        rb.AddForce(Vector3.up * 500, ForceMode.Impulse);
    }
    
    // 设置速度
    void SetVelocity(Vector3 vel)
    {
        rb.velocity = vel;
    }
    
    // 停止移动
    void Stop()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
```

---

#### Q14: Collider 和 Trigger 的区别？

**解答：**

| 特性 | Collider (碰撞) | Trigger (触发器) |
|------|-----------------|------------------|
| 物理碰撞 | 有 | 无 |
| 回调方法 | OnCollisionEnter | OnTriggerEnter |
| 刚体需求 | 至少有一个 | 至少有一个 |

```csharp
public class CollisionDemo : MonoBehaviour
{
    // 碰撞回调（需要 Collider + Rigidbody）
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"碰撞: {collision.gameObject.name}");
    }
    
    void OnCollisionStay(Collision collision)
    {
        // 持续接触
    }
    
    void OnCollisionExit(Collision collision)
    {
        // 结束接触
    }
    
    // 触发器回调（IsTrigger = true）
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"触发: {other.name}");
    }
    
    void OnTriggerStay(Collider other)
    {
        // 持续触发
    }
    
    void OnTriggerExit(Collider other)
    {
        // 结束触发
    }
}
```

---

### 2.4 渲染系统

#### Q15: DrawCall 是什么？如何优化？

**解答：**

DrawCall 是 CPU 向 GPU 发送的渲染命令次数，越少越好。

**优化方法：**

1. **静态批处理**
```csharp
// Inspector: 将 Static 勾选
// 代码: StaticBatchingUtility.Combine(gameObject);
```

2. **动态批处理**
```csharp
// 条件：< 900 顶点，材质相同
// 自动启用
```

3. **GPU Instancing**
```csharp
// 材质勾选 Enable GPU Instancing
// 代码
Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
```

4. **图集 (Sprite Atlas)**
```csharp
// 将小图合并为大图，减少材质切换
```

5. **减少材质数量**
```csharp
// 合并材质
Material[] mats = new Material[] { mat1, mat2 };
renderer.materials = mats;
```

---

#### Q16: 什么是 Shader？Unity 中如何优化 Shader？

**解答：**

Shader 是运行在 GPU 上的程序，控制如何渲染像素。

**Unity Shader 类型：**

| 类型 | 复杂度 | 性能 | 适用 |
|------|--------|------|------|
| Built-in Surface | 中 | 中 | 简单效果 |
| URP/HDRP Lit | 高 | 低-中 | 高质量渲染 |
| Unlit | 低 | 高 | 不需要光照 |
| Custom | 可控 | 可控 | 特殊效果 |

**Shader 优化：**

```glsl
// 1. 减少精度
half   // 16位，-60000 ~ 60000
fixed  // 11位，-2 ~ 2
float  // 32位

// 2. 减少指令数
// 不好
half4 color = tex2D(_MainTex, uv);
color *= tex2D(_BumpMap, uv);

// 好
half4 color = tex2D(_MainTex, uv) * tex2D(_BumpMap, uv);

// 3. 使用 LOD
ShaderLOD 200; // 高
ShaderLOD 100; // 中
ShaderLOD 50;  // 低
```

---

### 2.5 资源管理

#### Q17: Resources 和 AssetBundle 的区别？

**解答：**

| 特性 | Resources | AssetBundle |
|------|-----------|-------------|
| 加载时机 | 打包时全部加载 | 运行时按需加载 |
| 内存 | 占用内存 | 可卸载 |
| 更新 | 需要重新打包 | 可热更新 |
| 大小 | 不适合大资源 | 适合大资源 |

```csharp
// Resources 加载
GameObject prefab = Resources.Load<GameObject>("Prefabs/Player");
Sprite sprite = Resources.Load<Sprite>("Sprites/icon");

// AssetBundle 加载
IEnumerator LoadAsset()
{
    string url = "file://" + Application.streamingAssetsPath + "/myasset.bundle";
    using (UnityWebRequest wr = UnityWebRequestAssetBundle.GetAssetBundle(url))
    {
        yield return wr.SendWebRequest();
        AssetBundle ab = DownloadHandlerAssetBundle.GetContent(wr);
        GameObject prefab = ab.LoadAsset<GameObject>("Player");
    }
}

// 卸载
Resources.UnloadUnusedAssets();
Resources.UnloadAsset(obj);
ab.Unload(true); // 卸载所有
```

---

### 2.6 性能优化

#### Q18: Unity 性能优化的常见手段？

**解答：**

**1. 渲染优化**
```csharp
// 减少透明物体
// 使用遮挡剔除 (Occlusion Culling)
// 控制 Draw Call
// 使用 LOD (Level of Detail)
GetComponent< LODGroup >().SetLODs(lods);
```

**2. 物理优化**
```csharp
// 减少碰撞层
// 使用简化的碰撞器
// 禁用不用的 Rigidbody
rb.isKinematic = true; // 停止物理计算
```

**3. 内存优化**
```csharp
// 对象池
public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    public GameObject Get()
    {
        return pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab);
    }
    
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

**4. 代码优化**
```csharp
// 避免每帧创建新变量
// 使用 for 替代 foreach
// 减少 LINQ 使用
// 使用 [SerializeField] 替代 public
```

---

## 3. 数据结构与算法

### 3.1 线性结构

#### Q19: 数组和链表的区别？

**解答：**

| 特性 | 数组 | 链表 |
|------|------|------|
| 存储 | 连续内存 | 分散内存 |
| 访问 | O(1) 随机访问 | O(n) 遍历 |
| 插入/删除 | O(n) | O(1) |
| 内存 | 节省 | 额外指针开销 |
| 缓存 | 命中率高 | 命中率低 |

```csharp
// 数组 - 适合随机访问
int[] array = new int[10];
array[0] = 1; // O(1)

// 链表 - 适合频繁插入删除
public class ListNode
{
    public int val;
    public ListNode next;
    public ListNode(int val) { this.val = val; }
}

public class LinkedList
{
    private ListNode head;
    
    public void AddFirst(int val)
    {
        ListNode node = new ListNode(val);
        node.next = head;
        head = node;
    }
    
    public void AddLast(int val)
    {
        ListNode node = new ListNode(val);
        if (head == null) { head = node; return; }
        
        ListNode current = head;
        while (current.next != null)
            current = current.next;
        current.next = node;
    }
}
```

**Unity 使用场景：**
- 固定大小用数组（如坐标数组）
- 动态大小用 List<T>（内部数组实现）

---

#### Q20: List<T> 的内部实现和扩容机制？

**解答：**

List<T> 内部使用数组，默认容量为 0，首次 Add 时扩容为 4，之后按 2 倍扩容。

```csharp
// 内部实现简化
public class MyList<T>
{
    private T[] items;
    private int size;
    private int capacity;
    
    public MyList(int capacity = 0)
    {
        this.capacity = capacity;
        items = new T[capacity];
    }
    
    public void Add(T item)
    {
        if (size >= capacity)
        {
            capacity = capacity == 0 ? 4 : capacity * 2;
            Array.Resize(ref items, capacity);
        }
        items[size++] = item;
    }
    
    public T this[int index]
    {
        get => items[index];
        set => items[index] = value;
    }
}
```

**Unity 中使用建议：**
```csharp
// 预先知道大小时指定容量
List<GameObject> pool = new List<GameObject>(100);

// 频繁增删考虑 LinkedList
LinkedList<GameObject> linkedList = new LinkedList<GameObject>();
```

---

### 3.2 非线性结构

#### Q21: 栈和队列的区别和应用场景？

**解答：**

| 结构 | 特点 | 操作 | 应用场景 |
|------|------|------|----------|
| 栈 | LIFO 后进先出 | push/pop | 撤销、递归 |
| 队列 | FIFO 先进先出 | enqueue/dequeue | 任务队列、缓冲 |

```csharp
// 栈 - 撤销功能
public class UndoSystem
{
    private Stack<IAction> history = new Stack<IAction>();
    
    public void Do(IAction action)
    {
        action.Execute();
        history.Push(action);
    }
    
    public void Undo()
    {
        if (history.Count > 0)
            history.Pop().Undo();
    }
}

// 队列 - 任务队列
public class TaskQueue
{
    private Queue<Action> tasks = new Queue<Action>();
    
    public void Enqueue(Action task)
    {
        tasks.Enqueue(task);
    }
    
    public void Update()
    {
        if (tasks.Count > 0)
            tasks.Dequeue().Invoke();
    }
}
```

---

#### Q22: 哈希表（Dictionary）的原理和优化？

**解答：**

哈希表通过哈希函数将键映射到数组索引，实现 O(1) 平均查找。

```csharp
// 内部结构
public class HashTable<K, V>
{
    private class Entry
    {
        public K key;
        public V value;
        public int hashCode;
        public Entry next;
    }
    
    private Entry[] table;
    private int capacity;
    private int count;
    
    // 哈希函数
    private int GetHash(K key)
    {
        return key.GetHashCode() & 0x7FFFFFFF % capacity;
    }
    
    public void Add(K key, V value)
    {
        int index = GetHash(key);
        // 冲突处理：链地址法
    }
}
```

**Unity 使用注意事项：**

```csharp
// 避免频繁创建 Dictionary
// 预先指定容量
Dictionary<string, GameObject> enemies = new Dictionary<string, GameObject>(100);

// 遍历时不能修改
foreach (var kvp in dict)
{
    // dict.Add() 会报错
}

// Unity 特有：使用 Struct 包装减少 GC
public struct KeyValuePair<TKey, TValue>
{
    public TKey Key;
    public TValue Value;
}
```

---

### 3.3 树与图

#### Q23: 二叉树的遍历方式？

**解答：**

| 方式 | 顺序 | 应用 |
|------|------|------|
| 前序 | 根→左→右 | 目录树 |
| 中序 | 左→根→右 | 二叉搜索树排序 |
| 后序 | 左→右→根 | 表达式树 |
| 层序 | 按层 | 层级遍历 |

```csharp
public class TreeNode
{
    public int val;
    public TreeNode left;
    public TreeNode right;
}

// 前序遍历
void PreOrder(TreeNode root)
{
    if (root == null) return;
    Debug.Log(root.val);
    PreOrder(root.left);
    PreOrder(root.right);
}

// 中序遍历
void InOrder(TreeNode root)
{
    if (root == null) return;
    InOrder(root.left);
    Debug.Log(root.val);
    InOrder(root.right);
}

// 层序遍历（BFS）
void LevelOrder(TreeNode root)
{
    if (root == null) return;
    Queue<TreeNode> q = new Queue<TreeNode>();
    q.Enqueue(root);
    
    while (q.Count > 0)
    {
        TreeNode node = q.Dequeue();
        Debug.Log(node.val);
        if (node.left != null) q.Enqueue(node.left);
        if (node.right != null) q.Enqueue(node.right);
    }
}
```

---

### 3.4 排序与搜索

#### Q24: 快速排序的原理和实现？

**解答：**

快速排序采用分治法，平均时间复杂度 O(n log n)，最坏 O(n²)。

```csharp
public class QuickSort
{
    public void Sort(int[] arr, int low, int high)
    {
        if (low < high)
        {
            int pi = Partition(arr, low, high);
            Sort(arr, low, pi - 1);
            Sort(arr, pi + 1, high);
        }
    }
    
    private int Partition(int[] arr, int low, int high)
    {
        int pivot = arr[high];
        int i = low - 1;
        
        for (int j = low; j < high; j++)
        {
            if (arr[j] <= pivot)
            {
                i++;
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
        (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);
        return i + 1;
    }
}
```

**Unity 使用：**
```csharp
// 直接使用 Array.Sort
int[] nums = { 5, 2, 8, 1, 9 };
Array.Sort(nums);

// List 排序
List<int> list = new List<int> { 5, 2, 8 };
list.Sort();
```

---

#### Q25: 二分搜索的适用场景？

**解答：**

**前提：有序数组 + 随机访问**

时间复杂度：O(log n)

```csharp
public int BinarySearch(int[] arr, int target)
{
    int left = 0, right = arr.Length - 1;
    
    while (left <= right)
    {
        int mid = left + (right - left) / 2;
        
        if (arr[mid] == target)
            return mid;
        else if (arr[mid] < target)
            left = mid + 1;
        else
            right = mid - 1;
    }
    
    return -1;
}
```

**游戏开发应用：**

```csharp
// 装备等级查找
int FindEquipmentByLevel(int level)
{
    Equipment[] sorted = GetSortedEquipments();
    // 二分查找适合的装备
}

// 分数排行榜
int FindRank(int[] scores, int score)
{
    // 二分查找排名
}
```

---

### 3.5 算法技巧

#### Q26: 什么是时间复杂度和空间复杂度？

**解答：**

| 复杂度 | O(1) | O(log n) | O(n) | O(n log n) | O(n²) |
|--------|------|----------|------|------------|-------|
| 名称 | 常数 | 对数 | 线性 | 线性对数 | 平方 |
| 示例 | 数组访问 | 二分查找 | 遍历 | 快速排序 | 冒泡排序 |

**空间复杂度：**
- O(1): 原地操作
- O(n): 额外数组
- O(n²): 二维数组

```csharp
// O(1)
int GetFirst(int[] arr) => arr[0];

// O(n)
int Sum(int[] arr)
{
    int sum = 0;
    foreach (var n in arr) sum += n;
    return sum;
}

// O(n²) - 冒泡排序
void BubbleSort(int[] arr)
{
    for (int i = 0; i < arr.Length; i++)
        for (int j = 0; j < arr.Length - i - 1; j++)
            if (arr[j] > arr[j + 1])
                (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
}
```

---

## 4. 游戏开发实战

### 4.1 设计模式

#### Q27: 单例模式在 Unity 中的实现？

**解答：**

```csharp
// 1. 基础单例
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

// 2. 泛型单例
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }
    
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
}

// 使用
public class AudioManager : Singleton<AudioManager> { }
```

---

#### Q28: 对象池模式的实现？

**解答：**

```csharp
public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int initialSize = 10;
    
    private Queue<GameObject> pool;
    
    void Awake()
    {
        pool = new Queue<GameObject>();
        
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNew();
            pool.Enqueue(obj);
        }
    }
    
    private GameObject CreateNew()
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj;
    }
    
    public GameObject Get()
    {
        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = CreateNew();
        }
        obj.SetActive(true);
        return obj;
    }
    
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}

// 使用
public class Bullet : MonoBehaviour
{
    private ObjectPool pool;
    
    public void SetPool(ObjectPool p) => pool = p;
    
    void OnCollisionEnter(Collision other)
    {
        pool.Return(gameObject);
    }
}
```

---

### 4.2 网络同步

#### Q29: Unity 中常用的网络同步方案？

**解答：**

| 方案 | 特点 | 适用 |
|------|------|------|
| Mirror | 开源/免费 | 中小型游戏 |
| Photon PUN2 | 简单/商业 | 快速原型 |
| Photon Fusion | 预测/高性能 | 竞技游戏 |
| Netcode for GameObjects | 官方 | 新项目 |
| PlayFab | 云服务 | 实时服务 |

```csharp
// Mirror 简单示例
using Mirror;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;
    
    [Command] // 服务端调用
    public void CmdTakeDamage(int damage)
    {
        health -= damage;
    }
    
    [ClientRpc] // 服务端通知客户端
    public void RpcPlayEffect(string effectName)
    {
        // 播放特效
    }
    
    void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"Health: {newHealth}");
    }
}
```

---

### 4.3 状态机

#### Q30: 有限状态机 (FSM) 的实现？

**解答：**

```csharp
// 状态接口
public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

// 状态机
public class StateMachine
{
    private IState currentState;
    
    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    
    public void Update()
    {
        currentState?.Update();
    }
}

// 具体状态 - 玩家
public class IdleState : IState
{
    private PlayerController player;
    
    public IdleState(PlayerController p) => player = p;
    
    public void Enter() { }
    
    public void Update()
    {
        if (player.IsMoving)
            player.ChangeState(new RunState(player));
        
        if (Input.GetKeyDown(KeyCode.Space))
            player.ChangeState(new JumpState(player));
    }
    
    public void Exit() { }
}
```

---

---

## 5. 计算机组成原理

### 5.1 CPU结构与工作原理

#### Q31: CPU的组成和工作原理？

**解答：**

CPU由三大部分组成：

| 部件 | 功能 |
|------|------|
| 控制单元 (CU) | 指令译码、控制信号生成 |
| 运算单元 (ALU) | 算术运算、逻辑运算 |
| 寄存器组 | 暂存指令、数据和地址 |

**指令执行周期（取指→译码→执行→回写）：**

```
C# 类比：每一帧的Update循环
while (true)
{
    Fetch();     // 取指令 ← 类似读取 Input
    Decode();    // 译码   ← 类似解析输入事件
    Execute();   // 执行   ← 类似执行游戏逻辑
    WriteBack(); // 回写   ← 类似更新状态/渲染
}
```

---

#### Q32: 什么是流水线技术？

**解答：**

流水线将指令执行分成多个阶段，不同指令的不同阶段可以重叠执行。

```
无流水线：
┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐
│取指  │ │译码  │ │执行  │ │回写  │  ← 指令1
│      │ │取指  │ │译码  │ │执行  │  ← 指令2
└─────┘ └─────┘ └─────┘ └─────┘

5级流水线：
┌─────┬─────┬─────┬─────┬─────┐
│ IF │ ID │ EX │ MEM │ WB │  ← 指令1
│ IF │ ID │ EX │ MEM │ WB │  ← 指令2
│ IF │ ID │ EX │ MEM │ WB │  ← 指令3
└─────┴─────┴─────┴─────┴─────┘
```

**Unity相关：渲染管线就是流水线的典型应用。**

---

### 5.2 内存层级（Cache、主存、辅存）

#### Q33: 计算机存储层级结构？

**解答：**

```
速度：快 → → → → → → → → → 慢
容量：小 → → → → → → → → → 大
成本：高 → → → → → → → → → 低

 寄存器     (ns级)     ≈ 1KB
  L1 Cache  (≈1ns)    ≈ 32KB
  L2 Cache  (≈5ns)    ≈ 256KB
  L3 Cache  (≈15ns)   ≈ 8MB
  主存 (RAM) (≈50ns)  ≈ 16GB
  磁盘/SSD  (≈5ms)    ≈ 1TB
```

**程序局部性原理：**
- **时间局部性**：刚访问过的数据很可能再次被访问
- **空间局部性**：刚访问过的数据附近的数据很可能被访问

```csharp
// 时间局部性：循环中的变量被反复访问
for (int i = 0; i < 1000000; i++)
{
    sum += arr[i]; // sum 具有时间局部性
}

// 空间局部性：数组元素连续访问
for (int i = 0; i < arr.Length; i++)
{
    sum += arr[i]; // arr[i] 附近的数据也被加载到缓存
}
```

```cpp
// C++ 同样遵循局部性原理
const int N = 1000000;
int arr[N];
int sum = 0;
for (int i = 0; i < N; i++) {
    sum += arr[i]; // 缓存友好
}

// 缓存不友好（按列访问会导致缓存缺失）
int matrix[N][N];
int s = 0;
for (int j = 0; j < N; j++)
    for (int i = 0; i < N; i++)
        s += matrix[i][j]; // 跨行访问，缓存不友好！
```

**Unity开发中的缓存优化：**

```csharp
// 好的做法：连续访问数组
Transform[] transforms = GetAllTransforms();
for (int i = 0; i < transforms.Length; i++)
{
    transforms[i].position += Vector3.forward; // 顺序访问，缓存友好
}

// 不好的做法：频繁切换对象
foreach (var obj in scatteredObjects)
{
    // 对象在内存中分散，不断缓存缺失
}
```

---

#### Q34: 大端序和小端序的区别？

**解答：**

| 字节序 | 说明 | 平台 |
|--------|------|------|
| 大端 (Big-Endian) | 高位字节存储在低地址 | 网络协议、某些RISC |
| 小端 (Little-Endian) | 低位字节存储在低地址 | x86/ARM (主流) |

```csharp
// C# 检测字节序
bool isLittleEndian = BitConverter.IsLittleEndian;
Console.WriteLine($"当前系统: {(isLittleEndian ? "小端" : "大端")}");

// 转换字节序
int value = 0x12345678;
if (BitConverter.IsLittleEndian)
{
    value = System.Net.IPAddress.HostToNetworkOrder(value); // 转大端
}
```

```cpp
// C++ 检测字节序
#include <iostream>
bool IsLittleEndian() {
    int x = 1;
    return *(char*)&x == 1;
}

int main() {
    std::cout << (IsLittleEndian() ? "小端" : "大端") << std::endl;
    return 0;
}
```

**游戏开发应用**：网络同步数据时需要统一字节序，通常使用大端（网络字节序）。

---

### 5.3 浮点数表示

#### Q35: 浮点数的精度问题？

**解答：**

浮点数遵循 IEEE 754 标准：
- `float`: 1位符号 + 8位指数 + 23位尾数（≈7位有效数字）
- `double`: 1位符号 + 11位指数 + 52位尾数（≈15位有效数字）

```csharp
// 浮点数精度问题
float a = 0.1f;
float b = 0.2f;
float c = a + b;
Console.WriteLine(c == 0.3f);     // false！
Console.WriteLine(c);             // 0.300000012...

// 正确比较方式
float epsilon = 0.00001f;
bool equal = Math.Abs(c - 0.3f) < epsilon;

// Unity 中的比较
if (Mathf.Approximately(a, b)) { }
```

```cpp
// C++ 浮点数比较
#include <cmath>
#include <iostream>
float a = 0.1f, b = 0.2f;
float c = a + b;
std::cout << std::boolalpha << (c == 0.3f) << std::endl; // false

// 正确比较
const float EPSILON = 0.00001f;
bool equal = std::fabs(c - 0.3f) < EPSILON;
```

**游戏开发注意**：角色位置、物理计算中避免直接相等比较，使用 `Mathf.Approximately` 或范围比较。

---

## 6. 操作系统

### 6.1 进程与线程

#### Q36: 进程和线程的区别？

**解答：**

| 特性 | 进程 (Process) | 线程 (Thread) |
|------|---------------|--------------|
| 资源 | 独立的内存空间 | 共享进程内存 |
| 切换开销 | 大（需切换页表） | 小 |
| 通信方式 | IPC (管道/消息队列/共享内存) | 直接读写共享数据 |
| 稳定性 | 一个进程挂不影响其他 | 一个线程挂可能影响整个进程 |
| 创建速度 | 慢 | 快 |

```csharp
// C# 线程创建
using System.Threading;

class Program
{
    static void Main()
    {
        // 方式1：Thread类
        Thread t1 = new Thread(() =>
        {
            Console.WriteLine("线程1执行");
        });
        t1.Start();
        t1.Join(); // 等待线程结束

        // 方式2：Task（推荐）
        Task t2 = Task.Run(() =>
        {
            Console.WriteLine("Task执行");
        });
        await t2;

        // 方式3：线程池
        ThreadPool.QueueUserWorkItem(state =>
        {
            Console.WriteLine("线程池任务");
        });
    }
}
```

```cpp
// C++ 线程创建
#include <iostream>
#include <thread>
#include <future>

int main() {
    // 方式1：std::thread
    std::thread t1([]() {
        std::cout << "线程1执行" << std::endl;
    });
    t1.join(); // 等待线程结束

    // 方式2：std::async（类似Task）
    auto future = std::async(std::launch::async, []() {
        return 42;
    });
    int result = future.get(); // 获取结果

    // 方式3：线程池（C++20 std::jthread）
    std::jthread jt([](std::stop_token st) {
        while (!st.stop_requested()) {
            // 工作循环
        }
    });

    return 0;
}
```

**Unity开发注意**：Unity API 不是线程安全的，必须在主线程调用。耗时操作应使用：
- 协程（轻量级，主线程）
- Unity Job System（安全多线程）
- C# Task（需注意主线程回调）

---

#### Q37: 线程同步的方式？

**解答：**

```csharp
// C# 线程同步
class Counter
{
    private int count = 0;
    private readonly object lockObj = new object();
    private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

    // 方式1：lock（最常用）
    public void Increment()
    {
        lock (lockObj) { count++; }
    }

    // 方式2：Monitor
    public void Decrement()
    {
        Monitor.Enter(lockObj);
        try { count--; }
        finally { Monitor.Exit(lockObj); }
    }

    // 方式3：读写锁（读多写少场景）
    public int Read()
    {
        rwLock.EnterReadLock();
        try { return count; }
        finally { rwLock.ExitReadLock(); }
    }

    // 方式4：互斥体（跨进程）
    private static Mutex mutex = new Mutex();
    
    // 方式5：信号量（控制并发数）
    private static SemaphoreSlim semaphore = new SemaphoreSlim(3);
}
```

```cpp
// C++ 线程同步
#include <mutex>
#include <shared_mutex>
#include <atomic>
#include <semaphore>

class Counter {
private:
    int count = 0;
    std::mutex mtx;                       // 方式1：互斥锁
    std::shared_mutex rw_mtx;            // 方式2：读写锁
    std::atomic<int> atomic_count{0};    // 方式3：原子操作

public:
    void Increment() {
        std::lock_guard<std::mutex> lock(mtx); // RAII 锁
        count++;
    }

    int Read() {
        std::shared_lock<std::shared_mutex> lock(rw_mtx); // 共享锁
        return count;
    }

    void AtomicIncrement() {
        atomic_count.fetch_add(1); // 无锁操作
    }
};
```

**游戏开发应用**：多线程加载资源、网络请求、AI计算等，避免主线程阻塞。

---

### 6.2 内存管理

#### Q38: 虚拟内存和物理内存的区别？

**解答：**

| 概念 | 说明 |
|------|------|
| 物理内存 (RAM) | 实际的硬件内存 |
| 虚拟内存 | 操作系统为每个进程提供的独立地址空间 |
| 页面置换 | 物理内存不足时，将不常用页换出到磁盘 |

```
进程A的虚拟地址空间：     物理内存：
┌─────────────────┐     ┌────────────┐
│ 代码段          │     │ 帧0  ← 页1 │
├─────────────────┤     ├────────────┤
│ 数据段          │     │ 帧1  ← 页3 │
├─────────────────┤     ├────────────┤
│ 堆 (Heap)       │     │ 帧2  ← 页0 │
├─────────────────┤     ├────────────┤
│ 栈 (Stack)      │     │ ...        │
├─────────────────┤     └────────────┘
│ 内核空间        │              ↑ 页表映射
└─────────────────┘
```

```csharp
// C# 内存分配
// 值类型 → 栈上分配
int x = 10;

// 引用类型 → 堆上分配（GC管理）
string s = new string('a', 100);

// 大对象（>85KB）→ 大对象堆（LOH）
byte[] largeArray = new byte[100000]; // LOH
```

```cpp
// C++ 内存分配
#include <memory>
#include <vector>

void MemoryDemo() {
    // 栈上分配（自动释放）
    int x = 10;
    
    // 堆上分配（需手动释放）
    int* p = new int(10);
    delete p; // 手动释放
    
    // 智能指针（RAII自动管理）
    std::unique_ptr<int> uptr = std::make_unique<int>(10);
    std::shared_ptr<int> sptr = std::make_shared<int>(10);
    
    // 容器在堆上管理
    std::vector<int> vec(100000);
    // vec 析构时自动释放
}
```

**C# vs C++ 内存管理对比：**

| 特性 | C# | C++ |
|------|-----|-----|
| 栈对象 | 值类型 struct | 所有类型默认栈上 |
| 堆对象 | new 即上堆，GC回收 | new 需要 delete |
| 智能指针 | 无原生支持 | unique_ptr/shared_ptr |
| 内存泄漏 | 较难（GC兜底） | 容易（手动管理） |
| 性能 | 有GC暂停开销 | 无GC，可控性高 |

---

#### Q39: 堆和栈的区别？

**解答：**

| 特性 | 栈 (Stack) | 堆 (Heap) |
|------|-----------|-----------|
| 分配方式 | 编译器自动分配 | 程序员手动分配 |
| 释放方式 | 自动（函数结束） | 手动/GC |
| 大小 | 小（通常1-8MB） | 大（可用内存） |
| 速度 | 快（入栈出栈） | 慢（需查找空闲块） |
| 碎片 | 无 | 可能产生内存碎片 |
| 方向 | 高地址→低地址 | 低地址→高地址 |

---

### 6.3 进程调度

#### Q40: 常见的进程调度算法？

**解答：**

| 算法 | 原理 | 优点 | 缺点 |
|------|------|------|------|
| FCFS | 先来先服务 | 公平 | 短任务等待长任务 |
| SJF | 最短作业优先 | 平均等待时间短 | 难以预知执行时间 |
| 时间片轮转 (RR) | 轮流分配时间片 | 响应时间均匀 | 上下文切换开销 |
| 优先级调度 | 高优先级先执行 | 区分重要任务 | 低优先级可能饥饿 |
| 多级反馈队列 | 多队列+动态优先级 | 综合优 | 实现复杂 |

**Unity中类比**：协程调度类似于时间片轮转，每帧分配执行时间片。

---

### 6.4 死锁

#### Q41: 什么是死锁？产生的四个必要条件？

**解答：**

死锁是指两个或多个进程互相等待对方释放资源，导致所有进程都无法继续执行。

**四个必要条件（缺一不可）：**
1. **互斥**：资源一次只能被一个进程使用
2. **占有并等待**：进程占有一个资源，同时等待其他资源
3. **不可剥夺**：资源只能由占有进程主动释放
4. **循环等待**：进程间形成循环等待链

```csharp
// 死锁示例
class DeadlockExample
{
    private static readonly object lockA = new object();
    private static readonly object lockB = new object();

    static void Main()
    {
        // 线程1：先锁A再锁B
        Task t1 = Task.Run(() =>
        {
            lock (lockA)
            {
                Thread.Sleep(100); // 确保死锁条件
                lock (lockB) { }
            }
        });

        // 线程2：先锁B再锁A（导致死锁）
        Task t2 = Task.Run(() =>
        {
            lock (lockB)
            {
                Thread.Sleep(100);
                lock (lockA) { }
            }
        });

        Task.WaitAll(t1, t2); // 死锁！
    }
}

// 解决方案：固定锁的顺序
Task t1 = Task.Run(() =>
{
    lock (lockA)
    {
        Thread.Sleep(100);
        lock (lockB) { } // 统一先A后B的顺序
    }
});

Task t2 = Task.Run(() =>
{
    lock (lockA) // 同样先锁A
    {
        Thread.Sleep(100);
        lock (lockB) { }
    }
});
```

---

## 7. 计算机网络

### 7.1 网络分层模型

#### Q42: OSI七层模型与TCP/IP四层模型的区别？

**解答：**

| OSI七层 | TCP/IP四层 | 协议举例 | 设备 |
|---------|-----------|---------|------|
| 应用层 | 应用层 | HTTP/DNS/FTP | - |
| 表示层 | ↓ | SSL/TLS | - |
| 会话层 | ↓ | Socket | - |
| 传输层 | 传输层 | TCP/UDP | 防火墙 |
| 网络层 | 网络层 | IP/ICMP | 路由器 |
| 数据链路层 | 数据链路层 | Ethernet/PPP | 交换机 |
| 物理层 | ↓ | 光纤/双绞线 | 集线器 |

**游戏开发中的重要协议：**

| 协议 | 传输层 | 特点 | 游戏应用 |
|------|--------|------|----------|
| HTTP/HTTPS | TCP | 可靠，有状态 | 登录、匹配、下载 |
| WebSocket | TCP | 全双工 | 实时聊天、游戏通信 |
| UDP | UDP | 不可靠，低延迟 | FPS、动作游戏同步 |

---

#### Q43: TCP三次握手和四次挥手？

**解答：**

```
三次握手（建立连接）：
客户端                         服务端
  │                              │
  │────── SYN (seq=x) ──────────→│  → 客户端进入SYN_SENT
  │                              │
  │←── SYN+ACK (seq=y, ack=x+1) ──│  → 服务端进入SYN_RCVD
  │                              │
  │────── ACK (seq=x+1, ack=y+1)─→│  → 连接建立，双方ESTABLISHED
  │                              │

四次挥手（断开连接，以主动方=客户端为例）：
主动方                        被动方
  │                              │
  │────── FIN (seq=u) ──────────→│  → 主动方进入FIN_WAIT_1
  │                              │
  │←────── ACK (seq=v, ack=u+1) ──│  → 被动方进入CLOSE_WAIT
  │                              │
  │←────── FIN (seq=w) ──────────│  → 被动方发送完毕
  │                              │
  │────── ACK (seq=u+1, ack=w+1)─→│  → 主动方TIME_WAIT后关闭

**为什么三次握手？** 防止已失效的请求连接突然传到服务端，造成资源浪费。

**为什么四次挥手？** 收到 FIN 时可能还有数据未发送完毕，所以先回 ACK，等数据发完再发 FIN。注意：挥手可由任意一端（客户端或服务端）主动发起。

```csharp
// C# TCP客户端
using System.Net.Sockets;
using System.Net;

class TcpClientExample
{
    static async Task Main()
    {
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 7777);
        
        NetworkStream stream = client.GetStream();
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello");
        await stream.WriteAsync(data); // 发送数据
        
        byte[] buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer); // 接收数据
    }
}
```

```cpp
// C++ TCP客户端
#include <winsock2.h>
#include <iostream>
#pragma comment(lib, "ws2_32.lib")

void TcpClientDemo() {
    WSADATA wsaData;
    WSAStartup(MAKEWORD(2, 2), &wsaData);
    
    SOCKET sock = socket(AF_INET, SOCK_STREAM, 0);
    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_port = htons(7777);
    inet_pton(AF_INET, "127.0.0.1", &addr.sin_addr);
    
    connect(sock, (sockaddr*)&addr, sizeof(addr)); // 连接（三次握手）
    
    const char* msg = "Hello";
    send(sock, msg, strlen(msg), 0); // 发送
    
    char buffer[1024] = {0};
    recv(sock, buffer, sizeof(buffer), 0); // 接收
    
    closesocket(sock);
    WSACleanup();
}
```

---

### 7.2 TCP vs UDP

#### Q44: TCP和UDP的区别？

**解答：**

| 特性 | TCP | UDP |
|------|-----|-----|
| 连接 | 面向连接 | 无连接 |
| 可靠性 | 可靠（确认重传） | 不可靠（尽最大努力） |
| 顺序 | 保证数据顺序 | 不保证顺序 |
| 速度 | 较慢 | 快 |
| 头部大小 | 20-60字节 | 8字节 |
| 流量控制 | 有 | 无 |
| 拥塞控制 | 有 | 无 |
| 应用场景 | 文件传输、网页 | 音视频、游戏同步 |

**游戏开发选择：**

```csharp
// 方案1：TCP（确保可靠性，适合卡牌、回合制游戏）
// Mirror, Photon PUN 使用 TCP/WebSocket

// 方案2：UDP（低延迟，适合FPS、动作游戏）
// Photon Fusion, Netcode for Entities 使用 UDP

// 方案3：混合（重要消息用TCP，高频状态用UDP）
// 自定义协议
```

**Unity中的网络库选择：**

| 库 | 传输层 | 适合场景 |
|------|--------|----------|
| Mirror | TCP | 中小型游戏 |
| Photon PUN | TCP | 快速实现 |
| Photon Fusion | UDP | 竞技游戏 |
| Netcode for GO | UDP/DTLS | 新项目 |
| LiteNetLib | UDP | 自定义需求 |

---

### 7.3 HTTP与HTTPS

#### Q45: GET和POST的区别？

**解答：**

| 特性 | GET | POST |
|------|-----|------|
| 数据位置 | URL参数 | 请求体 |
| 安全性 | 参数可见 | 相对安全 |
| 长度限制 | 有（URL长度限制） | 理论上无限制 |
| 缓存 | 可缓存 | 不可缓存 |
| 幂等性 | 幂等 | 不幂等 |
| 用途 | 查询 | 提交/修改 |

```csharp
// C# HTTP请求
using System.Net.Http;

class HttpExample
{
    static readonly HttpClient client = new HttpClient();

    // GET请求
    static async Task<string> GetAsync(string url)
    {
        var response = await client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }

    // POST请求
    static async Task<string> PostAsync(string url, string json)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }
}
```

```cpp
// C++ HTTP请求（使用libcurl）
#include <curl/curl.h>
#include <string>

size_t WriteCallback(void* contents, size_t size, size_t nmemb, std::string* output) {
    size_t totalSize = size * nmemb;
    output->append((char*)contents, totalSize);
    return totalSize;
}

void HttpGetDemo() {
    CURL* curl = curl_easy_init();
    std::string response;
    
    if (curl) {
        curl_easy_setopt(curl, CURLOPT_URL, "https://api.example.com");
        curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
        curl_easy_setopt(curl, CURLOPT_WRITEDATA, &response);
        curl_easy_perform(curl);
        curl_easy_cleanup(curl);
    }
}
```

---

### 7.4 Socket编程基础

#### Q46: Socket通信模型？

**解答：**

```csharp
// C# UDP Socket（适合游戏实时通信）
class UdpServer
{
    static void Main()
    {
        using var udpServer = new UdpClient(7777);
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        
        while (true)
        {
            // 接收数据（非连接，UDP特性）
            byte[] receiveData = udpServer.Receive(ref remoteEndPoint);
            string message = Encoding.UTF8.GetString(receiveData);
            Console.WriteLine($"收到: {message} from {remoteEndPoint}");
            
            // 回复
            byte[] sendData = Encoding.UTF8.GetBytes("Pong");
            udpServer.Send(sendData, sendData.Length, remoteEndPoint);
        }
    }
}
```

```cpp
// C++ UDP Socket
#include <winsock2.h>

void UdpServerDemo() {
    WSADATA wsaData;
    WSAStartup(MAKEWORD(2, 2), &wsaData);
    
    SOCKET sock = socket(AF_INET, SOCK_DGRAM, 0); // SOCK_DGRAM = UDP
    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_port = htons(7777);
    addr.sin_addr.s_addr = INADDR_ANY;
    
    bind(sock, (sockaddr*)&addr, sizeof(addr)); // 绑定端口
    
    char buffer[1024];
    sockaddr_in clientAddr{};
    int clientAddrLen = sizeof(clientAddr);
    
    while (true) {
        int bytes = recvfrom(sock, buffer, sizeof(buffer), 0,
            (sockaddr*)&clientAddr, &clientAddrLen);
        std::cout << "收到: " << buffer << std::endl;
    }
    
    closesocket(sock);
    WSACleanup();
}
```

**游戏服务器架构对比：**

| 架构 | 特点 | 适合 | 代表 |
|------|------|------|------|
| 单服 | 简单，所有玩家同服 | 小游戏 | 本地联机 |
| 分布式 | 多服务器分担 | 大型MMO | 魔兽世界 |
| 帧同步 | 所有客户端同步输入 | 格斗/RTS | 街霸 |
| 状态同步 | 服务端做权威计算 | FPS/MMO | 守望先锋 |

---

> 以上 5-7 节（计算机组成原理、操作系统、计算机网络）的详细学习内容请参见 [阶段零点五_计算机基础.md](./阶段零点五_计算机基础.md)，包含 C#/C++ 代码示例、习题练习和面试自测题。

---

## 面试技巧总结

### 1. 答题结构
```
1. 先回答是什么（定义）
2. 再解释为什么（原理）
3. 最后说怎么用（代码示例）
```

### 2. 常见追问
- "能写一下代码吗？"
- "有什么优缺点？"
- "在 Unity 中怎么应用？"
- "性能方面注意什么？"

### 3. 加分项
- 结合 Unity 实际项目经验
- 提到具体的优化方案
- 知道底层原理
- 能手写核心代码

---

> 持续更新中... 如有错误欢迎指正
