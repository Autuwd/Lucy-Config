# Day 9：Prefab 与 Instantiate — 深入篇：嵌套预制体、Serialization 回调与 Addressables

## 0. 引言：预置体的完整生态

上一章我们学习了 Prefab 的基础：模板化、Instantiate、对象池、DontDestroyOnLoad。但在大型项目中，Prefab 的使用远比这些复杂——嵌套层级管理、版本迁移、资源加载策略都是关键挑战。

在 Raylib/C++ 中，对象的"模板"就是 struct 的默认值：
```cpp
// Raylib：无 Prefab，每次手动初始化
Bullet bullet = {0};  // 必须知道所有字段的默认值
```

Unity 的 Prefab 系统是**全序列化**的——不仅有字段值，还有组件、层级、引用，并且支持嵌套和继承。

---

## 1. 嵌套 Prefabs 与 Prefab Variants——继承关系

### Nested Prefabs 的层级

```
一个角色 Prefab 的内部结构：

Hero.prefab（根预制体）
├── Transform
├── CharacterController
├── Animator
│
├── Spine（子 GameObject——也是一个 Prefab）
│   └── Spine_Animator.prefab（嵌套预制体）
│       ├── Transform
│       ├── SkinnedMeshRenderer
│       └── AnimationController
│
├── Weapon (子 GameObject——嵌套预制体)
│   └── Sword.prefab
│       ├── Transform
│       ├── MeshFilter
│       └── MeshRenderer
│
└── UI (子 GameObject——嵌套预制体)
    └── HUD.prefab
        ├── Canvas
        └── HealthBar

修改 Sword.prefab → 所有引用 Sword.prefab 的地方都更新
这就是嵌套预制体的威力！
```

### Prefab 覆盖（Overrides）的管理

```csharp
// 当你有嵌套 Prefab 时，Override 的传播规则：

// 场景中的实例：
// Hero (Override: Weapon.color = red)
//   └── Sword.prefab (原始：color = blue)
//        └── 实例：color = red（Override）

// Apply 到 Prefab 时有两种选择：

// 1. Apply 到 Hero.prefab（只覆盖 Hero）
//    → 所有 Hero 实例的 Weapon 颜色都变红
//    → Sword.prefab 本身不变

// 2. Apply 到 Sword.prefab（传播到源）
//    → 所有使用 Sword.prefab 的地方都变色
//    → 更广泛的影响

// 代码中判断是否有 Override：
public class OverrideChecker : MonoBehaviour
{
    void Start()
    {
        // 使用 PrefabUtility（Editor 脚本）
#if UNITY_EDITOR
        var instance = gameObject;
        var overrides = UnityEditor.PrefabUtility
            .GetPropertyModifications(instance);
        
        foreach (var mod in overrides)
        {
            Debug.Log($"Override: {mod.propertyPath} = {mod.value}");
        }
#endif
    }
}
```

### Prefab Variant——预制体变体

```csharp
// 变体是"继��"了基础 Prefab 的新 Prefab
// 变体 = 基础 Prefab + 修改的属性 + 新增的组件

// 创建 Variant：
// 右键 Prefab → Create → Prefab Variant

// 使用场景：
/*
基础 Enemy.prefab
├── Transform, SpriteRenderer, Rigidbody2D
├── EnemyAI.cs (speed = 3, hp = 100)
└── Collider2D

Enemy_Fast (Variant of Enemy)
├── Override: speed = 8
├── Override: hp = 50
└── 新增：EnemyFlasher.cs（受伤闪烁效果，仅在变体中）

Enemy_Tank (Variant of Enemy)
├── Override: speed = 1
├── Override: hp = 500
└── 新增：Armor.cs（护甲逻辑）
*/
```

### 变体的继承链——多层变体

```csharp
// 变体可以多层继承

// 基础 Prefab
//   └── Variant Level 1
//         └── Variant Level 2
//               └── Variant Level 3

// 每一层可以覆盖上一层的属性
// 修改基础 Prefab → 所有 Variant 同步更新（除非被覆盖）

// 注意：不要超过 3-4 层
// 深层继承链：
// - 性能开销（每次修改需要追踪多层覆盖）
// - 理解难度（不知道某个值来自哪一层）
// - 推荐用 Composition（组合）代替多级继承
```

---

## 2. Runtime Instantiation 高级模式

### Factory Pattern——工厂方法

```csharp
using UnityEngine;
using System.Collections.Generic;

// 工厂模式——封装对象创建逻辑
public abstract class EnemyFactory : MonoBehaviour
{
    public abstract GameObject CreateEnemy(EnemyType type, Vector3 position);
}

public class PrefabFactory : EnemyFactory
{
    [System.Serializable]
    public class EnemyEntry
    {
        public EnemyType type;
        public GameObject prefab;
        public int poolSize = 10;
    }

    public EnemyEntry[] entries;
    private Dictionary<EnemyType, Queue<GameObject>> pools;

    void Awake()
    {
        // 初始化对象池
        pools = new Dictionary<EnemyType, Queue<GameObject>>();
        foreach (var entry in entries)
        {
            var pool = new Queue<GameObject>();
            for (int i = 0; i < entry.poolSize; i++)
            {
                GameObject obj = Instantiate(entry.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                pool.Enqueue(obj);
            }
            pools[entry.type] = pool;
        }
    }

    public override GameObject CreateEnemy(EnemyType type, Vector3 position)
    {
        if (!pools.TryGetValue(type, out var pool))
            return null;

        if (pool.Count == 0)
        {
            // 扩容——找到对应的 prefab
            foreach (var entry in entries)
            {
                if (entry.type == type)
                {
                    GameObject obj = Instantiate(entry.prefab);
                    obj.SetActive(false);
                    obj.transform.SetParent(transform);
                    pool.Enqueue(obj);
                    break;
                }
            }
        }

        GameObject enemy = pool.Dequeue();
        enemy.transform.position = position;
        enemy.SetActive(true);
        return enemy;
    }

    public void ReturnEnemy(EnemyType type, GameObject enemy)
    {
        enemy.SetActive(false);
        pools[type].Enqueue(enemy);
    }
}

public enum EnemyType
{
    Grunt,
    Archer,
    Mage,
    Boss
}
```

### IEnumerator 顺序生成——波次系统

```csharp
public class WaveSpawner : MonoBehaviour
{
    public EnemyFactory factory;
    public Transform[] spawnPoints;

    IEnumerator Start()
    {
        // 波次 1：5 个步兵
        yield return StartCoroutine(SpawnWave(EnemyType.Grunt, 5, 0.5f));
        
        // 等待 3 秒
        yield return new WaitForSeconds(3f);
        
        // 波次 2：3 个弓箭手 + 2 个步兵
        yield return StartCoroutine(SpawnMixedWave());
        
        // 波次 3：Boss
        yield return new WaitForSeconds(5f);
        factory.CreateEnemy(EnemyType.Boss, spawnPoints[0].position);
    }

    IEnumerator SpawnWave(EnemyType type, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            factory.CreateEnemy(type, pos);
            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator SpawnMixedWave()
    {
        for (int i = 0; i < 3; i++)
        {
            factory.CreateEnemy(EnemyType.Archer, spawnPoints[0].position);
            yield return new WaitForSeconds(0.8f);
        }
        
        for (int i = 0; i < 2; i++)
        {
            factory.CreateEnemy(EnemyType.Grunt, spawnPoints[1].position);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
```

---

## 3. 对象池高级模式

### 泛型对象池

```csharp
using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

// 使用 Unity 内置的 ObjectPool<T>（Unity 2021+）
public class AdvancedObjectPool : MonoBehaviour
{
    // Unity 自带的泛型对象池
    private ObjectPool<Bullet> bulletPool;

    void Awake()
    {
        bulletPool = new ObjectPool<Bullet>(
            createFunc: CreateBullet,
            actionOnGet: OnGetBullet,
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: (b) => Destroy(b.gameObject),
            collectionCheck: true,    // 检查重复释放
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    Bullet CreateBullet()
    {
        Bullet b = Instantiate(bulletPrefab).GetComponent<Bullet>();
        b.gameObject.SetActive(false);
        b.pool = bulletPool;  // Bullet 持有池引用
        return b;
    }

    void OnGetBullet(Bullet b)
    {
        b.gameObject.SetActive(true);
        b.transform.position = firePoint.position;
        b.transform.rotation = firePoint.rotation;
        b.ResetState();  // 重置子弹状态
    }

    void OnReleaseBullet(Bullet b)
    {
        b.gameObject.SetActive(false);
    }

    public Bullet Get() => bulletPool.Get();
    public void Release(Bullet b) => bulletPool.Release(b);
}

// Bullet 脚本——使用 ObjectPool
public class Bullet : MonoBehaviour
{
    public ObjectPool<Bullet> pool;

    public void ResetState()
    {
        // 重置位置、速度、生命值
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        lifetime = 0;
    }

    private float lifetime;
    void Update()
    {
        lifetime += Time.deltaTime;
        if (lifetime > 5f)
        {
            // 归还到池中
            pool?.Release(this);
        }
    }
}
```

### 预热（Warmup）策略

```csharp
public class PoolWarmup : MonoBehaviour
{
    public int warmupFrames = 5;

    void Start()
    {
        // 分帧预热——避免首帧卡顿

        // 问题：如果一次性 Instantiate 100 个对象
        // Start 会卡 50ms+
        // 解决方案：分到多帧执行

        StartCoroutine(WarmupOverFrames());
    }

    IEnumerator WarmupOverFrames()
    {
        int objectsPerFrame = 20;
        int totalObjects = 100;
        int spawned = 0;

        while (spawned < totalObjects)
        {
            int batch = Mathf.Min(objectsPerFrame, totalObjects - spawned);
            
            for (int i = 0; i < batch; i++)
            {
                // 创建预热对象并放入池
                GameObject obj = Instantiate(warmupPrefab);
                obj.SetActive(false);
                pool.Enqueue(obj);
                spawned++;
            }

            // 让出一帧——避免卡顿
            yield return null;
        }

        Debug.Log($"Pool warmup complete: {spawned} objects");
    }
}
```

---

## 4. PrefabUtility——编辑器脚本

在编辑器模式下，你可以通过 **PrefabUtility** 类操作 Prefab。

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrefabTooling
{
    [MenuItem("Tools/Apply All Prefab Overrides")]
    static void ApplyAllOverrides()
    {
        // 查找场景中所有 Prefab 实例
        var instances = FindObjectsOfType<GameObject>();
        
        foreach (var go in instances)
        {
            // 判断是否是 Prefab 实例
            var status = PrefabUtility.GetPrefabInstanceStatus(go);
            
            if (status == PrefabInstanceStatus.Connected)
            {
                // 获取覆盖的修改
                var overrides = PrefabUtility
                    .GetPropertyModifications(go);
                
                if (overrides != null && overrides.Length > 0)
                {
                    // 应用到 Prefab
                    PrefabUtility.ApplyPrefabInstance(
                        go,
                        InteractionMode.AutomatedAction
                    );
                    Debug.Log($"Applied {go.name}");
                }
            }
        }
    }

    [MenuItem("Assets/Create Prefab Variants From Selection")]
    static void CreateVariants()
    {
        // 为选中的 Prefab 批量创建变体
        foreach (var guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                GameObject variant = PrefabUtility
                    .InstantiatePrefab(prefab) as GameObject;
                
                string variantPath = path.Replace(".prefab", "_Variant.prefab");
                
                // 保存为变体
                PrefabUtility.SaveAsPrefabAsset(variant, variantPath);
                Object.DestroyImmediate(variant);
            }
        }
    }
}
#endif
```

---

## 5. [FormerlySerializedAs]——平滑重命名

上一章提到 `[FormerlySerializedAs]`。这里展示完整的序列化迁移策略。

```csharp
using UnityEngine;
using UnityEngine.Serialization;

// 场景：你有一个已发布的游戏，PlayerData 已经序列化到很多 Prefab 中
// 你重命名了字段，但不想丢失已有的数据

[System.Serializable]
public class PlayerData
{
    // 这是最初的字段名
    [FormerlySerializedAs("hp")]
    [FormerlySerializedAs("healthPoints")]  // 可以链式多个
    public float health;

    [FormerlySerializedAs("maxHp")]
    public float maxHealth;

    [FormerlySerializedAs("inventory")]
    [FormerlySerializedAs("items")]
    public List<string> itemIDs;
    
    // 类型变了怎么办？
    [FormerlySerializedAs("damage")]
    [SerializeField]  // 因为 int 不能直接重命名为 float
    private int legacyDamage;  // 旧数据读到这里
    
    public float Damage  // 新字段用属性
    {
        get => legacyDamage;  // 兼容旧数据
        set => legacyDamage = (int)value;
    }
}

// Unity 序列化反序列化的流程：
// 1. 读 "hp" → 找不到 → 找 [FormerlySerializedAs("hp")] → 找到 health
// 2. 读 "healthPoints" → 找不到 → 找 [FormerlySerializedAs("healthPoints")]
//    → 找到 health（但已经有 health 值，跳过）
// 3. 写入 health 的值
```

---

## 6. ISerializationCallbackReceiver——自定义序列化

```csharp
using UnityEngine;
using System.Collections.Generic;

// 当 Unity 的默认序列化不够用时
// 比如：序列化 Dictionary、HashSet、复杂数据结构

public class SerializableDictionary : MonoBehaviour, 
    ISerializationCallbackReceiver
{
    // Unity 不能直接序列化 Dictionary
    // 所以用两个 List 做桥接
    [SerializeField]
    private List<string> keys = new List<string>();
    
    [SerializeField]
    private List<int> values = new List<int>();

    // 实际使用的数据
    public Dictionary<string, int> data = new Dictionary<string, int>();

    // 序列化前调用——把 Dictionary 转成 List
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        
        foreach (var kvp in data)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    // 反序列化后调用——把 List 转回 Dictionary
    public void OnAfterDeserialize()
    {
        data.Clear();
        for (int i = 0; i < keys.Count && i < values.Count; i++)
        {
            data[keys[i]] = values[i];
        }
    }
}

// 更通用的方案：
[System.Serializable]
public class SerializedDictionary<TKey, TValue> 
    : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> _keys = new List<TKey>();
    [SerializeField] private List<TValue> _values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        _keys.Clear();
        _values.Clear();
        foreach (var kvp in this)
        {
            _keys.Add(kvp.Key);
            _values.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        Clear();
        for (int i = 0; i < _keys.Count; i++)
            this[_keys[i]] = _values[i];
    }
}
```

---

## 7. SerializeReference——多态序列化

Unity 2020+ 支持 `[SerializeReference]`，可以序列化接口/抽象类的子类。

```csharp
using UnityEngine;

// 定义一个效果接口
public interface IEffect
{
    void Apply(GameObject target);
    string Description { get; }
}

// 多种效果实现
[System.Serializable]
public class DamageEffect : IEffect
{
    public float damage = 10;
    public DamageType type = DamageType.Physical;

    public void Apply(GameObject target)
    {
        // 造成伤害
        Debug.Log($"Dealt {damage} {type} damage to {target.name}");
    }

    public string Description => $"Deal {damage} {type} damage";
}

[System.Serializable]
public class HealEffect : IEffect
{
    public float healAmount = 20;
    public bool overheal = false;

    public void Apply(GameObject target)
    {
        Debug.Log($"Healed {healAmount} HP for {target.name}");
    }

    public string Description => $"Heal {healAmount} HP";
}

[System.Serializable]
public class BuffEffect : IEffect
{
    public BuffType buff = BuffType.SpeedUp;
    public float duration = 5f;

    public void Apply(GameObject target)
    {
        Debug.Log($"Applied {buff} for {duration}s on {target.name}");
    }

    public string Description => $"Apply {buff} for {duration}s";
}

// 使用 [SerializeReference] 的技能系统
public class Skill : MonoBehaviour
{
    public string skillName = "Fireball";

    // 关键：用 [SerializeReference] 而不是 [SerializeField]
    // [SerializeField] 只能存一个具体的类
    // [SerializeReference] 可以存任何 IEffect 的实现
    [SerializeReference]
    public List<IEffect> effects = new List<IEffect>();
    
    // 在 Inspector 中：点击加号 → 选择效果类型
    // 支持多态！

    void Use(GameObject target)
    {
        foreach (var effect in effects)
        {
            effect?.Apply(target);
        }
    }
}

// 对比：
// 没有 [SerializeReference]：
// 需要在 Inspector 中拖不同的 MonoBehavior → 碎片化

// 有 [SerializeReference]：
// 同一个 Skill 组件可以配置不同效果 → Inspector 中直接选择类型
// 数据驱动的技能系统——不需要写不同的脚本
```

---

## 8. Addressables 与 Prefab——什么时候用什么？

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesDemo : MonoBehaviour
{
    // ─── 什么时候用 Prefab（直接引用）───
    // 1. 对象数量少（< 50 种不同类型）
    // 2. 必须立即可用的对象（UI、玩家角色、核心系统）
    // 3. 小项目或原型阶段
    // 4. 编辑器开发时频繁修改

    public GameObject directPrefab;  // 直接拖入 Inspector

    // ─── 什么时候用 Addressables ───
    // 1. 大量资源（1000+ 种不同对象）
    // 2. 需要按需加载（关卡内容、DLC）
    // 3. 跨项目复用资源
    // 4. 热更新
    // 5. 内存管理（加载/卸载控制）

    public AssetReference addressablePrefab;  // Addressable 引用

    void Start()
    {
        // Prefab：直接 Instantiate——简单直接
        GameObject obj1 = Instantiate(directPrefab);

        // Addressables：异步加载——不阻塞主线程
        AsyncOperationHandle<GameObject> handle = 
            addressablePrefab.InstantiateAsync(Vector3.zero, Quaternion.identity);
        
        // 或者更精细的控制：
        // 1. 加载 Asset
        // 2. 实例化
        // 3. 用完后释放
    }

    // Addressables 的完整生命周期
    IEnumerator LoadAndUseEnemy(string address)
    {
        // 1. 加载资源（从 AssetBundle 或本地）
        var handle = Addressables.LoadAssetAsync<GameObject>(address);
        yield return handle;
        
        // 2. 实例化
        GameObject enemy = Instantiate(handle.Result);
        
        // 3. 使用...
        yield return new WaitForSeconds(5f);
        
        // 4. 销毁实例
        Destroy(enemy);
        
        // 5. 释放资源——减少引用计数
        Addressables.Release(handle);
        // 注意：如果还有别的对象在引用同一个资源
        // 资源不会被卸载——引用计数归零才卸载
    }
}

// 决策树：
/*
选择 Prefab 还是 Addressables？

对象是否在场景加载时必须存在？
├─ 是 → Prefab（直接引用，保证可用）
└─ 否 → 数量多吗？
        ├─ < 50 种 → Prefab（简单）
        └─ >= 50 种 → Addressables（按需加载）

是否需要热更新？
├─ 是 → Addressables（AssetBundle 远程加载）
└─ 否 → Prefab（没有远程依赖）

是否需要内存精细化控制？
├─ 是 → Addressables（引用计数、按需卸载）
└─ 否 → Prefab（引擎自动管理）

是否跨项目复用？
├─ 是 → Addressables（资源包可作为 package）
└─ 否 → Prefab（项目内使用）
*/
```

---

## C++/Raylib 对照总结

| 高级概念 | Raylib (C++) | Unity (C#) |
|---------|-------------|-----------|
| 对象模板继承 | 无 | Prefab Variant（预制体变体） |
| 嵌套模板 | 手动组装 | Nested Prefabs（嵌套预制体） |
| 序列化版本迁移 | 无 | `[FormerlySerializedAs]` + `ISerializationCallbackReceiver` |
| 多态序列化 | 虚函数 + 手动保存类型 | `[SerializeReference]` 自动多态 |
| 高级对象池 | 手写循环缓冲区 | `ObjectPool<T>`（内置 API） |
| 按需加载 | `LoadTexture` 手动管理 | Addressables（引用计数 + 依赖分析） |
| 编辑器工具 | 无 | PrefabUtility（Editor 脚本） |

## 停靠点

> Prefab Variant 实现了预制体的"继承"——基础模板修改 → 所有变体同步。
> `[SerializeReference]` 让 Inspector 支持接口/抽象类的多态选择——适合技能/效果/物品系统。
> `ISerializationCallbackReceiver` 桥接 Unity 不能直接序列化的类型（Dictionary 等）。
> `[FormerlySerializedAs]` 让你安全重命名序列化字段——旧数据不丢失。
> Addressables 通过引用计数管理资源生命周期——对比 Prefab 的直接引用，更灵活但更复杂。
> 预热（分帧创建）避免对象池初始化时的首帧卡顿。
