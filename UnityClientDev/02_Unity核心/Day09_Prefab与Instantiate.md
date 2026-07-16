# Day 9：Prefab 与 Instantiate — 从序列化模板到运行时克隆

## 0. 为什么需要 Prefab？

在 Raylib/C++ 中，每个对象都是手动创建的：

```cpp
// Raylib：手动创建每个子弹
struct Bullet {
    Vector2 pos;
    Vector2 vel;
    bool active;
};

Bullet bullets[100];
// 每次发射要手动初始化：
bullets[i] = { {x, y}, {0, -speed}, true };
```

在 Unity 中，**Prefab（预制体）** 是一个**可复用的对象模板**。你设计一次（添加组件、设置参数），然后可以在场景中、代码中重复实例化。

---

## 1. Prefab 的本质——序列化模板

### Prefab 在磁盘上的表示

```
当你创建一个 Prefab（.prefab 文件）时，Unity 将：
- GameObject 的所有组件
- 每个组件的所有属性值
- 组件的引用（如对材质的引用、对其他组件的引用）
- 子对象的层级结构

全部序列化为一个文件（YAML 格式）。

Bullet.prefab 的内部（简化 YAML）：
--- !u!1 &100001
GameObject:
  m_Name: Bullet
  m_Component:
  - !u!4 &400001   // Transform
    m_LocalPosition: {x: 0, y: 0, z: 0}
  - !u!212 &2120001 // SpriteRenderer
    m_Sprite: {fileID: ...}  // 引用精灵纹理
  - !u!50 &500001  // Rigidbody2D
    m_Mass: 1
  - !u!114 &1140001 // MonoBehaviour (Bullet.cs)
    m_Script: {fileID: ...}
```

### Prefab 的三种模式

```
Prefab 的工作模式：

1. Prefab Asset（磁盘上的模板）
   └── Bullet.prefab（原始设计）

2. Prefab Instance（场景中的实例）
   ├── Bullet (1)  ← 从 Prefab 克隆
   ├── Bullet (2)  ← 修改了颜色（Override）
   └── Bullet (3)  ← 修改了速度（Override）

3. Prefab Variant（变体）
   └── Bullet_Variant.prefab
       └── 继承了原始 Prefab，修改了一些属性
```

### Override——实例对模板的修改

```csharp
// 场景中修改了 Prefab 实例的属性
// 在 Inspector 中会显示为 "Override"（粗体属性名）
// 你可以：
// 1. 保留 Override（仅这个实例修改）
// 2. Apply → 应用到 Prefab（所有实例都改）
// 3. Revert → 还原到 Prefab 默认值
```

---

## 2. Instantiate——从模板克隆

### 基本用法

```csharp
public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;  // Inspector 中拖入 Prefab

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            // Instantiate 的 3 种常见形式：

            // 1. 最简单的：只克隆 Prefab
            GameObject bullet = Instantiate(bulletPrefab);

            // 2. 指定位置和旋转
            GameObject bullet = Instantiate(
                bulletPrefab,
                firePoint.position,     // 位置
                firePoint.rotation      // 旋转
            );

            // 3. 指定位置、旋转和父对象
            GameObject bullet = Instantiate(
                bulletPrefab,
                firePoint.position,
                firePoint.rotation,
                parentTransform         // 设置父对象
            );
        }
    }
}
```

### Instantiate 的底层流程

```
调用 Instantiate(bulletPrefab, position, rotation)

Unity 内部执行：
1. 读取 Prefab 的序列化数据
2. 在托管堆上分配新的 GameObject 对象
3. 在 C++ 层分配对应的原生对象
4. 反序列化组件数据：复制所有组件及其属性值
5. 设置 Transform 的位置和旋转
6. 调用 Awake()（如果 GameObject active）
7. 调用 OnEnable()（如果组件 enabled）
8. 返回新的 GameObject 引用
```

### Instantiate 的性能开销

```csharp
// Instantiate 是比较重的操作：
// - 内存分配（C# 堆 + C++ 原生对象）
// - 反序列化所有组件
// - 生命周期回调（Awake、OnEnable）

// 游戏中的频繁 Instantiate（如子弹射击）：
// - 每秒可能 Instantiate 几十次
// - 每次都有内存分配 → GC 压力
// - 解决方案：对象池（后面会讲）
```

---

## 3. Destroy——销毁对象

### 基本用法

```csharp
// 立即销毁
Destroy(gameObject);  // 销毁自身
Destroy(other.gameObject);  // 销毁其他对象

// 延迟销毁（秒）
Destroy(gameObject, 2f);  // 2 秒后销毁

// 在 Editor 中销毁（播放模式）
DestroyImmediate(gameObject);  // 立即销毁，不推荐在运行时使用
```

### Destroy 的延迟行为

```
调用 Destroy(gameObject) 时：

1. 对象被标记为 "pending destroy"
2. 当前帧继续执行——不会立即删除
3. 当前帧结束时：
   a. 调用 OnDisable()
   b. 调用 OnDestroy()
   c. 从场景中移除
   d. 标记为待 GC 回收

所以：Destroy 后同一帧内：
- gameObject != null 可能返回 true！（Unity 重写了 == 运算符）
- 但 Destroy 后的对象不应再被使用
```

### DontDestroyOnLoad——跨场景持久化

```csharp
void Awake()
{
    // 使这个对象在场景加载时不被销毁
    DontDestroyOnLoad(gameObject);

    // 常见用途：
    // - 音乐管理器（跨场景 BGM 不中断）
    // - 游戏管理器（GameManager 单例）
    // - 事件系统
}

// 注意：DontDestroyOnLoad 的对象不能放在场景中（没有父对象）
// 最好创建一个独立的 GameObject 来承载：

public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // 单例持久化
        }
        else
        {
            Destroy(gameObject);  // 重复的单例删除
        }
    }
}
```

---

## 4. 对象池（Object Pool）——性能优化的核心

### 为什么需要对象池？

```csharp
// ❌ 不用对象池：每次射击都 Instantiate + Destroy
void Shoot()
{
    GameObject bullet = Instantiate(bulletPrefab, ...);
    // 2 秒后 Destroy(bullet)

    // 每 Destroy 一次 → GC 产生垃圾
    // 每 Instantiate 一次 → 内存分配 + 对象创建
    // 频繁 GC → 卡顿！
}

// ✅ 对象池：预创建 + 复用
void Shoot()
{
    GameObject bullet = pool.Get();  // 从池中取一个
    bullet.transform.position = ...;
    // 用完后 pool.Return(bullet) 放回池中
    // 不销毁，只隐藏——零 GC！
}
```

### 对象池的实现

```csharp
using UnityEngine;
using System.Collections.Generic;

public class BulletPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public GameObject bulletPrefab;     // 子弹 Prefab
    public int initialSize = 20;        // 预创建数量
    public bool expandable = true;      // 不够时是否扩容

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Start()
    {
        // 预创建 N 个子弹，全部隐藏
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNewBullet();
            obj.SetActive(false);               // 隐藏
            pool.Enqueue(obj);                  // 入队
        }
    }

    // 从池中取出一个对象
    public GameObject Get()
    {
        if (pool.Count == 0)
        {
            if (!expandable)
            {
                Debug.LogWarning("Pool exhausted!");
                return null;
            }

            // 扩容：创建一个新的
            GameObject obj = CreateNewBullet();
            obj.SetActive(true);
            return obj;
        }

        GameObject bullet = pool.Dequeue();     // 出队
        bullet.SetActive(true);                 // 显示
        return bullet;
    }

    // 归还对象到池中
    public void Return(GameObject obj)
    {
        obj.SetActive(false);                   // 隐藏
        pool.Enqueue(obj);                      // 入队
    }

    private GameObject CreateNewBullet()
    {
        GameObject obj = Instantiate(bulletPrefab);
        // 设置对象池引用
        Bullet bulletScript = obj.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.pool = this;

        return obj;
    }
}
```

### Bullet 脚本——自动回收

```csharp
public class Bullet : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 10f;

    [HideInInspector]
    public BulletPool pool;  // 由对象池设置

    private float lifetimeTimer;
    public float maxLifetime = 3f;

    void OnEnable()
    {
        // 每次从池中取出时调用
        // 重置状态
        lifetimeTimer = 0;

        // 可选：重置位置、旋转、速度
        // 在 Get() 中设置
    }

    void Update()
    {
        // 移动
        transform.position += transform.right * speed * Time.deltaTime;

        // 生命周期倒计时
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
        {
            ReturnToPool();
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // 撞到东西就回收
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (pool != null)
            pool.Return(gameObject);
        else
            Destroy(gameObject);  // 安全措施
    }
}
```

### Gun 脚本——使用对象池

```csharp
public class Gun : MonoBehaviour
{
    public BulletPool bulletPool;
    public Transform firePoint;

    public float fireRate = 0.15f;  // 射速：每秒约 6.7 发
    private float nextFireTime;

    void Update()
    {
        // 按住射击键 + 冷却检查
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            // 从对象池取子弹
            GameObject bullet = bulletPool.Get();
            if (bullet != null)
            {
                bullet.transform.position = firePoint.position;
                bullet.transform.rotation = firePoint.rotation;
            }
        }
    }
}
```

### 对象池性能对比

```
场景：每秒生成 60 颗子弹，每颗存活 2 秒

不用对象池：
- 每秒 Instantiate 60 次（堆分配 60 次）
- 每秒 Destroy 30 次（GC 标记 30 次）
- 第 2 秒后 GC 开始频繁触发 → 卡顿
- 长期 GC 暂停时间可能达到 100-200ms

用对象池：
- Start 时 Instantiate 20 次（一次性）
- 运行时零分配！
- 零 GC！
- 零卡顿！

结论：任何频繁创建/销毁的场景都需要对象池
```

---

## 5. Prefab 的高级用法

### Prefab Variant（预制体变体）

```csharp
// 创建一个基础 Enemy Prefab
// 然后创建 Variant：
// - Enemy_Fast: 修改 speed = 10
// - Enemy_Tank: 修改 speed = 2, 添加 extraHealth 脚本

// Prefab Variant 共享同一模板，但可以覆盖特定属性
// 修改基础 Prefab → 所有 Variant 同步更新（除非覆盖了）
```

### 在代码中修改 Prefab（运行时）

```csharp
public class Spawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs;  // 多种敌人 Prefab

    public GameObject SpawnRandomEnemy(Vector3 position)
    {
        // 随机选择一种 Prefab 实例化
        int index = Random.Range(0, enemyPrefabs.Length);
        GameObject enemy = Instantiate(enemyPrefabs[index], position, Quaternion.identity);

        // 运行时修改实例属性
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.speed += Random.Range(-1f, 1f);  // 随机化速度
            enemyScript.scale = Random.Range(0.8f, 1.2f); // 随机化大小
            enemy.transform.localScale *= enemyScript.scale;
        }

        return enemy;
    }
}
```

---

## 练习：射击系统 + 对象池

```csharp
using UnityEngine;

public class Day09_Shooting : MonoBehaviour
{
    [Header("Gun Settings")]
    public BulletPool bulletPool;      // 引用对象池
    public Transform firePoint;        // 枪口位置
    public float fireRate = 0.15f;     // 射速

    private float nextFireTime;

    void Update()
    {
        // 射击逻辑
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Fire();
        }

        // 按 R 键重置（模拟重新加载场景）
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void Fire()
    {
        nextFireTime = Time.time + fireRate;

        // 从对象池取出子弹
        GameObject bullet = bulletPool.Get();
        if (bullet == null) return;  // 池耗尽了

        // 设置子弹初始状态
        bullet.transform.SetPositionAndRotation(
            firePoint.position,
            firePoint.rotation
        );
    }
}
```

---

---

## C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity (C#) |
|------|-------------|-----------|
| 对象模板 | 手动每个创建 | Prefab（序列化模板） |
| 克隆对象 | 手动初始化 struct | `Instantiate(prefab)` |
| 销毁对象 | 标记回收/复用 | `Destroy(gameObject)` |
| 动态数组 | `std::vector<Bullet>` | `List<GameObject>` |
| 循环缓冲区 | 手写粒子池 | 对象池 `Queue<T>` |
| — | 无对应概念 | Prefab Variant（变体） |
| — | 无对应概念 | `DontDestroyOnLoad`（跨场景持久化） |

## 停靠点

> Prefab = 序列化的对象模板（YAML 格式）。Instantiate = 从模板克隆（反序列化 + 创建）。
> Destroy 不是立即删除——当前帧结束时才执行。
> **对象池（Object Pool）** = 预创建 + 复用对象。避免频繁 Instantiate/Destroy 导致的 GC。
> 对象池的核心操作：`SetActive(true/false)`（显隐切换）代替 `Instantiate/Destroy`（创建销毁）。
> `DontDestroyOnLoad` 让对象跨场景存活。

