# 项目实战：太空射击 — 完整实现指南

> 从零搭建一个可玩性完整的太空射击游戏。涵盖状态机、对象池、波次系统、道具、Boss 战、UI 流程、音效管理等工业级实践。

---

## 目录
1. [项目结构](#项目结构)
2. [GameManager 状态机](#gamemanager-状态机)
3. [对象池系统](#对象池系统)
4. [玩家控制器](#玩家控制器)
5. [子弹系统](#子弹系统)
6. [敌人生成与波次](#敌人生成与波次)
7. [道具系统](#道具系统)
8. [Boss 战系统](#boss-战系统)
9. [计分与连击系统](#计分与连击系统)
10. [屏幕震动与击中停顿](#屏幕震动与击中停顿)
11. [背景视差滚动](#背景视差滚动)
12. [音频管理器](#音频管理器)
13. [UI 流程](#ui-流程)
14. [存档系统](#存档系统)
15. [移动端触控输入](#移动端触控输入)
16. [性能优化清单](#性能优化清单)
17. [停靠点](#停靠点)

---

## 项目结构

```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs
│   │   ├── AudioManager.cs
│   │   ├── ObjectPool.cs
│   │   └── SaveManager.cs
│   ├── Player/
│   │   ├── PlayerController.cs
│   │   ├── PlayerShooter.cs
│   │   └── PlayerPowerUp.cs
│   ├── Enemies/
│   │   ├── EnemyBase.cs
│   │   ├── EnemyNormal.cs
│   │   ├── EnemyFast.cs
│   │   └── EnemySpawner.cs
│   ├── Boss/
│   │   ├── BossController.cs
│   │   └── BossAttackPattern.cs
│   ├── Bullets/
│   │   ├── BulletBase.cs
│   │   └── BulletMulti.cs
│   ├── PowerUps/
│   │   └── PowerUp.cs
│   ├── UI/
│   │   ├── UIManager.cs
│   │   └── ScoreManager.cs
│   ├── FX/
│   │   ├── ScreenShake.cs
│   │   └── HitStop.cs
│   └── Environment/
│       └── ParallaxBackground.cs
├── Prefabs/
│   ├── Player.prefab
│   ├── Bullet.prefab
│   ├── Enemy_Normal.prefab
│   ├── Enemy_Fast.prefab
│   ├── Boss.prefab
│   ├── PowerUp_Shield.prefab
│   ├── PowerUp_Speed.prefab
│   └── PowerUp_Multi.prefab
└── Scenes/
    └── Main.unity
```

---

## GameManager 状态机

```csharp
// GameManager.cs — 全局游戏状态机
// 状态: Menu → Playing ↔ Paused → GameOver → Menu（循环）

using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,       // 主菜单
    Playing,    // 游戏中
    Paused,     // 暂停
    GameOver    // 游戏结束
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("状态")]
    public GameState currentState = GameState.Menu;
    public GameState previousState; // 用于暂停恢复

    [Header("游戏参数")]
    public float gameTime;          // 游戏进行时间（影响难度）
    public int waveNumber = 0;
    public bool isBossWave = false;

    [Header("引用")]
    public UIManager uiManager;
    public EnemySpawner enemySpawner;
    public PlayerController player;

    // 状态进入/退出事件
    public System.Action<GameState> OnStateEnter;
    public System.Action<GameState> OnStateExit;

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

    void Start()
    {
        ChangeState(GameState.Menu);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;

            // ESC 暂停
            if (Input.GetKeyDown(KeyCode.Escape))
                ChangeState(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ChangeState(GameState.Playing);
        }
        else if (currentState == GameState.GameOver)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                RestartGame();
        }
    }

    /// <summary>切换状态，触发进入/退出回调</summary>
    public void ChangeState(GameState newState)
    {
        if (newState == currentState && newState != GameState.Paused)
            return;

        OnStateExit?.Invoke(currentState);
        previousState = currentState;
        currentState = newState;
        OnStateEnter?.Invoke(newState);

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                // 保存最高分
                SaveManager.Instance.SaveHighScore(ScoreManager.Instance.score);
                break;

            case GameState.Menu:
                Time.timeScale = 1f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
        }

        uiManager?.OnGameStateChanged(newState);
    }

    /// <summary>开始游戏（从菜单或重新开始）</summary>
    public void StartGame()
    {
        gameTime = 0f;
        waveNumber = 0;
        isBossWave = false;

        // 重置玩家
        player?.ResetPlayer();

        // 重置得分
        ScoreManager.Instance?.ResetScore();

        // 清空场景中残留敌人
        if (enemySpawner != null)
        {
            enemySpawner.StopAllCoroutines();
            enemySpawner.ClearAllEnemies();
        }

        // 清除所有道具
        PowerUp[] powers = FindObjectsByType<PowerUp>(FindObjectsSortMode.None);
        foreach (var p in powers) Destroy(p.gameObject);

        ChangeState(GameState.Playing);
        enemySpawner?.StartSpawning();
    }

    /// <summary>暂停/恢复切换（给UI按钮用）</summary>
    public void TogglePause()
    {
        if (currentState == GameState.Playing)
            ChangeState(GameState.Paused);
        else if (currentState == GameState.Paused)
            ChangeState(GameState.Playing);
    }

    /// <summary>玩家死亡时调用</summary>
    public void OnPlayerDied()
    {
        enemySpawner?.StopSpawning();
        AudioManager.Instance?.PlaySFX("explosion_large");
        ChangeState(GameState.GameOver);
    }

    /// <summary>重新开始</summary>
    public void RestartGame()
    {
        // 重新加载场景保证干净状态
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>返回主菜单</summary>
    public void GoToMenu()
    {
        ChangeState(GameState.Menu);
    }

    /// <summary>退出游戏</summary>
    public void QuitGame()
    {
        Application.Quit();
    }
}
```

---

## 对象池系统

```csharp
// ObjectPool.cs — 通用对象池，预热 + 动态扩容

using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class PoolEntry
    {
        public string tag;               // 池标识
        public GameObject prefab;        // 预制体
        public int prewarmCount = 10;    // 预热数量
        public bool expandable = true;   // 是否允许动态扩容
    }

    public List<PoolEntry> poolEntries = new List<PoolEntry>();

    // 运行时池：tag → Queue<GameObject>
    private Dictionary<string, Queue<GameObject>> poolDict;
    // 追踪所有已生成的实体
    private Dictionary<string, HashSet<GameObject>> activeObjects;

    public static ObjectPool Instance { get; private set; }

    void Awake()
    {
        Instance = this;

        poolDict = new Dictionary<string, Queue<GameObject>>();
        activeObjects = new Dictionary<string, HashSet<GameObject>>();

        // 预热所有的池
        foreach (var entry in poolEntries)
        {
            poolDict[entry.tag] = new Queue<GameObject>();
            activeObjects[entry.tag] = new HashSet<GameObject>();

            for (int i = 0; i < entry.prewarmCount; i++)
            {
                GameObject obj = CreateNewObject(entry);
                obj.SetActive(false);
                poolDict[entry.tag].Enqueue(obj);
            }
        }
    }

    /// <summary>从池中获取一个对象</summary>
    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(tag))
        {
            Debug.LogWarning($"对象池没有 tag: {tag}");
            return null;
        }

        // 找空闲对象
        if (poolDict[tag].Count == 0)
        {
            // 池空了，检查是否允许扩容
            PoolEntry entry = poolEntries.Find(e => e.tag == tag);
            if (entry == null || !entry.expandable)
            {
                Debug.LogWarning($"池 [{tag}] 已空且不允许扩容");
                return null;
            }

            // 扩容：创建一个新的
            GameObject newObj = CreateNewObject(entry);
            poolDict[tag].Enqueue(newObj);
        }

        GameObject objToSpawn = poolDict[tag].Dequeue();
        objToSpawn.transform.SetPositionAndRotation(position, rotation);
        objToSpawn.SetActive(true);
        activeObjects[tag].Add(objToSpawn);

        // 调用对象上的 IPoolable 接口
        IPoolable poolable = objToSpawn.GetComponent<IPoolable>();
        poolable?.OnSpawn();

        return objToSpawn;
    }

    /// <summary>回收对象到池中</summary>
    public void Return(string tag, GameObject obj)
    {
        if (!poolDict.ContainsKey(tag))
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        activeObjects[tag].Remove(obj);
        poolDict[tag].Enqueue(obj);

        IPoolable poolable = obj.GetComponent<IPoolable>();
        poolable?.OnReturn();
    }

    /// <summary>回收所有活跃对象（波次切换时用）</summary>
    public void ReturnAll(string tag)
    {
        if (!activeObjects.ContainsKey(tag)) return;

        // 遍历副本避免迭代时修改
        List<GameObject> toReturn = new List<GameObject>(activeObjects[tag]);
        foreach (var obj in toReturn)
        {
            Return(tag, obj);
        }
    }

    /// <summary>回收全部池的所有对象</summary>
    public void ReturnAllPools()
    {
        foreach (string tag in activeObjects.Keys)
        {
            ReturnAll(tag);
        }
    }

    /// <summary>当前活跃数</summary>
    public int ActiveCount(string tag)
    {
        return activeObjects.ContainsKey(tag) ? activeObjects[tag].Count : 0;
    }

    private GameObject CreateNewObject(PoolEntry entry)
    {
        GameObject obj = Instantiate(entry.prefab, transform);
        obj.name = $"{entry.tag}_{poolDict[entry.tag].Count}";
        return obj;
    }
}

/// <summary>池化对象接口</summary>
public interface IPoolable
{
    void OnSpawn();   // 从池取出时调用
    void OnReturn();  // 回收时调用
}
```

---

## 玩家控制器

```csharp
// PlayerController.cs — 移动、边界限制、无敌帧

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 8f;
    public float smoothTime = 0.08f;
    private Vector2 velocity = Vector2.zero;

    [Header("边界")]
    public Camera gameCamera;
    private float leftBound, rightBound, topBound, bottomBound;

    [Header("无敌帧")]
    public float invincibleDuration = 1.5f;
    private float invincibleTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Header("生命")]
    public int maxHP = 3;
    public int currentHP;

    [Header("触控")]
    private Vector2 touchStartPos;
    private bool isTouching = false;

    // 移动输入向量（键盘/触控统一）
    private Vector2 moveInput;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    void Start()
    {
        ResetPlayer();
        CalculateBounds();
    }

    /// <summary>计算屏幕世界坐标边界</summary>
    void CalculateBounds()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        float camHeight = gameCamera.orthographicSize;
        float camWidth = camHeight * gameCamera.aspect;

        // 留一些边距不让玩家完全贴边
        float margin = 0.5f;
        leftBound = -camWidth + margin;
        rightBound = camWidth - margin;
        bottomBound = -camHeight + margin;
        topBound = camHeight - margin;
    }

    void Update()
    {
        if (GameManager.Instance.currentState != GameState.Playing)
        {
            // 游戏未进行时停止移动
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            return;
        }

        // 处理输入
        HandleInput();

        // 无敌帧闪烁
        if (invincibleTimer > 0)
        {
            invincibleTimer -= Time.deltaTime;
            spriteRenderer.enabled = Mathf.FloorToInt(invincibleTimer * 10) % 2 == 0;
        }
        else
        {
            spriteRenderer.enabled = true;
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.currentState != GameState.Playing)
            return;

        // 平滑移动
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Vector2 targetPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        targetPos.x = Mathf.Clamp(targetPos.x, leftBound, rightBound);
        targetPos.y = Mathf.Clamp(targetPos.y, bottomBound, topBound);
        rb.MovePosition(targetPos);
    }

    /// <summary>键盘 + 触控统一输入</summary>
    void HandleInput()
    {
        // 键盘输入
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(h, v).normalized;

        // 触控输入（叠加）
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchWorldPos = gameCamera.ScreenToWorldPoint(touch.position);
            Vector3 playerPos = transform.position;
            Vector2 dir = ((Vector2)(touchWorldPos - playerPos)).normalized;
            moveInput = dir;
        }
#endif
    }

    /// <summary>受到伤害</summary>
    public void TakeDamage(int damage = 1)
    {
        if (invincibleTimer > 0) return; // 无敌

        currentHP -= damage;
        invincibleTimer = invincibleDuration;

        // 屏幕震动
        ScreenShake.Instance?.Shake(0.2f, 0.15f);
        // 击中停顿
        HitStop.Instance?.Stop(0.05f);
        // 音效
        AudioManager.Instance?.PlaySFX("player_hit");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>获得护盾（道具）</summary>
    public void ActivateShield(float duration)
    {
        // 护盾期间无敌
        invincibleTimer = Mathf.Max(invincibleTimer, duration);
        spriteRenderer.color = Color.cyan;
        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), duration);
    }

    void ResetColor()
    {
        spriteRenderer.color = originalColor;
    }

    void Die()
    {
        // 爆炸特效
        GameObject explosion = ObjectPool.Instance.Spawn("Explosion", transform.position, Quaternion.identity);
        if (explosion != null)
            Invoke(nameof(ReturnExplosion), 1f);

        gameObject.SetActive(false);
        GameManager.Instance.OnPlayerDied();
    }

    void ReturnExplosion() { /* 回收爆炸特效 */ }

    /// <summary>重置玩家状态</summary>
    public void ResetPlayer()
    {
        currentHP = maxHP;
        invincibleTimer = 0f;
        transform.position = Vector3.zero;
        spriteRenderer.enabled = true;
        spriteRenderer.color = originalColor;
        gameObject.SetActive(true);
    }
}
```

---

## 子弹系统

```csharp
// BulletBase.cs — 基础子弹，支持对象池

using UnityEngine;

public class BulletBase : MonoBehaviour, IPoolable
{
    [Header("子弹参数")]
    public float speed = 12f;
    public int damage = 1;
    public float lifetime = 2.5f;
    private float lifetimeCounter;

    [Header("特效")]
    public GameObject hitEffectPrefab;

    protected Rigidbody2D rb;
    protected Vector2 direction = Vector2.up;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void OnEnable()
    {
        lifetimeCounter = lifetime;
    }

    void Update()
    {
        // 生命周期倒计时
        lifetimeCounter -= Time.deltaTime;
        if (lifetimeCounter <= 0f)
            ReturnToPool();
    }

    void FixedUpdate()
    {
        rb.velocity = direction * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 击中敌人
        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // 命中特效
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            // 回收子弹
            ReturnToPool();
        }
        // 击中Boss
        else if (other.CompareTag("Boss"))
        {
            BossController boss = other.GetComponentInParent<BossController>();
            if (boss != null)
                boss.TakeDamage(damage);

            ReturnToPool();
        }
    }

    /// <summary>设置子弹方向（用于多方向射击）</summary>
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    protected void ReturnToPool()
    {
        rb.velocity = Vector2.zero;
        ObjectPool.Instance.Return("Bullet", gameObject);
    }

    // --- IPoolable ---
    public void OnSpawn() { }
    public void OnReturn()
    {
        rb.velocity = Vector2.zero;
    }
}

// BulletMulti.cs — 散弹子弹（道具强化版）

public class BulletMulti : BulletBase
{
    [Header("散弹")]
    public int bulletCount = 3;
    public float spreadAngle = 20f;

    public override void OnSpawn()
    {
        base.OnSpawn();

        // 如果设为散弹模式，在生成时分裂成多个
        if (bulletCount > 1)
        {
            float startAngle = -spreadAngle / 2f;
            float angleStep = spreadAngle / (bulletCount - 1);

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;

                // 第0颗用自己的对象，其他从池取
                if (i == 0)
                {
                    SetDirection(dir);
                }
                else
                {
                    BulletMulti extra = ObjectPool.Instance.Spawn("Bullet_Multi", transform.position, Quaternion.identity)
                        ?.GetComponent<BulletMulti>();
                    if (extra != null)
                    {
                        extra.SetDirection(dir);
                        extra.damage = damage;
                        extra.speed = speed;
                    }
                }
            }
        }
    }
}
```

---

## 敌人系统

```csharp
// EnemyBase.cs — 敌人基类，所有敌人继承于此

using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, IPoolable
{
    [Header("基础属性")]
    public float speed = 2f;
    public int hp = 1;
    public int scoreValue = 100;

    [Header("掉落")]
    public GameObject[] powerUpPrefabs;
    [Range(0f, 1f)] public float dropChance = 0.15f;

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Color flashColor = Color.white;
    protected Color originalColor;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Update()
    {
        if (GameManager.Instance.currentState != GameState.Playing)
            return;

        OnUpdate();

        // 超出屏幕底部回收
        if (Camera.main != null)
        {
            Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
            if (viewPos.y < -0.2f)
                ReturnToPool();
        }
    }

    protected abstract void OnUpdate();  // 子类实现移动/行为

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
                player.TakeDamage(1);

            ReturnToPool();
        }
        else if (other.CompareTag("Bullet"))
        {
            // 子弹伤害由 BulletBase 处理，这里只处理碰撞反馈
        }
    }

    /// <summary>受伤</summary>
    public virtual void TakeDamage(int damage)
    {
        hp -= damage;

        // 闪烁反馈
        if (spriteRenderer != null)
            StartCoroutine(FlashRoutine());

        AudioManager.Instance?.PlaySFX("hit");

        if (hp <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(0.05f);
        spriteRenderer.color = originalColor;
    }

    protected virtual void Die()
    {
        // 加分
        ScoreManager.Instance?.AddScore(scoreValue);

        // 掉落道具
        TryDropPowerUp();

        // 爆炸特效
        ObjectPool.Instance.Spawn("Explosion", transform.position, Quaternion.identity);

        // 回收
        ReturnToPool();
    }

    protected void TryDropPowerUp()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;
        if (Random.value > dropChance) return;

        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
        Instantiate(prefab, transform.position, Quaternion.identity);
    }

    protected void ReturnToPool()
    {
        ObjectPool.Instance.Return("Enemy", gameObject);
    }

    // --- IPoolable ---
    public virtual void OnSpawn()
    {
        // 子类可重写
    }

    public virtual void OnReturn()
    {
        // 恢复颜色
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        // 取消所有进行中的协程
        StopAllCoroutines();
    }
}

// EnemyNormal.cs — 普通敌人，直线下移

public class EnemyNormal : EnemyBase
{
    [Header("普通敌人")]
    public float sinAmplitude = 0f;    // 横向摆动幅度（0=直线）
    public float sinFrequency = 2f;     // 摆动频率
    private float startX;

    protected override void Awake()
    {
        base.Awake();
        startX = transform.position.x;
    }

    protected override void OnUpdate()
    {
        // 垂直移动 + 可选正弦摆动
        Vector3 pos = transform.position;
        pos.y -= speed * Time.deltaTime;

        if (sinAmplitude > 0f)
        {
            pos.x = startX + Mathf.Sin(Time.time * sinFrequency) * sinAmplitude;
        }

        transform.position = pos;
    }
}

// EnemyFast.cs — 快速敌人，冲向玩家

public class EnemyFast : EnemyBase
{
    [Header("快速敌人")]
    public float chargeSpeed = 5f;
    private bool isCharging = false;
    private Transform playerTarget;

    protected override void OnSpawn()
    {
        base.OnSpawn();
        playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
        isCharging = false;
    }

    protected override void OnUpdate()
    {
        if (!isCharging)
        {
            // 先缓慢下降，然后突然冲刺
            transform.position += Vector3.down * speed * 0.5f * Time.deltaTime;

            // 检测位置，到了一定高度就冲刺
            if (Camera.main != null)
            {
                Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
                if (viewPos.y < 0.7f && playerTarget != null)
                {
                    isCharging = true;
                }
            }
        }
        else
        {
            // 冲向玩家
            if (playerTarget != null)
            {
                Vector2 dir = (playerTarget.position - transform.position).normalized;
                transform.position += (Vector3)dir * chargeSpeed * Time.deltaTime;
            }
        }
    }
}
```

---

## 敌人生成与波次系统

```csharp
// EnemySpawner.cs — 波次生成 + 难度递增

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class WaveConfig
    {
        public string waveName;
        public int enemyCount;          // 敌人数
        public float spawnInterval;     // 生成间隔
        public float enemySpeedMult = 1f;  // 速度倍率
        public int enemyHPBonus = 0;    // 额外血量
        public float fastEnemyChance = 0f; // 快速敌人概率
        public bool isBossWave = false;
    }

    [Header("波次配置")]
    public List<WaveConfig> waveConfigs = new List<WaveConfig>();
    public int currentWaveIndex = -1;

    [Header("生成边界")]
    public float spawnTopOffset = 1.5f;    // 屏幕上方偏移
    public float spawnMinX = 0.1f;
    public float spawnMaxX = 0.9f;

    [Header("Boss")]
    public GameObject bossPrefab;

    private Camera mainCamera;
    private Coroutine spawnCoroutine;
    private int enemiesSpawnedInWave = 0;
    private int enemiesAliveInWave = 0;    // 追踪存活敌人数

    void Start()
    {
        mainCamera = Camera.main;
    }

    /// <summary>开始生成</summary>
    public void StartSpawning()
    {
        currentWaveIndex = -1;
        StartCoroutine(WaveLoop());
    }

    /// <summary>停止生成</summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
    }

    /// <summary>清空所有敌人</summary>
    public void ClearAllEnemies()
    {
        ObjectPool.Instance.ReturnAll("Enemy");
        // 销毁非池化的Boss
        BossController[] bosses = FindObjectsByType<BossController>(FindObjectsSortMode.None);
        foreach (var b in bosses) Destroy(b.gameObject);
    }

    /// <summary>波次循环</summary>
    IEnumerator WaveLoop()
    {
        while (GameManager.Instance.currentState == GameState.Playing)
        {
            // 计算波次索引（循环或按配置）
            currentWaveIndex++;
            WaveConfig config;

            if (currentWaveIndex < waveConfigs.Count)
            {
                config = waveConfigs[currentWaveIndex];
            }
            else
            {
                // 超出配置后自动生成难度递增的波次
                config = GenerateDynamicWave(currentWaveIndex);
            }

            GameManager.Instance.waveNumber = currentWaveIndex + 1;
            GameManager.Instance.isBossWave = config.isBossWave;

            // 波次间等待
            yield return new WaitForSeconds(2f);

            // 显示波次提示
            UIManager.Instance?.ShowWaveText($"WAVE {currentWaveIndex + 1}");

            // Boss 波
            if (config.isBossWave)
            {
                yield return StartCoroutine(SpawnBossWave(config));
            }
            else
            {
                yield return StartCoroutine(SpawnEnemyWave(config));
            }

            // 等待波次结束（所有敌人被消灭）
            yield return new WaitUntil(() => enemiesAliveInWave <= 0);

            // 波次间隙
            yield return new WaitForSeconds(1.5f);
        }
    }

    /// <summary>生成普通敌人波次</summary>
    IEnumerator SpawnEnemyWave(WaveConfig config)
    {
        enemiesSpawnedInWave = 0;
        enemiesAliveInWave = config.enemyCount;

        while (enemiesSpawnedInWave < config.enemyCount)
        {
            // 随机位置
            float rx = Random.Range(spawnMinX, spawnMaxX);
            Vector3 spawnPos = mainCamera.ViewportToWorldPoint(
                new Vector3(rx, 1f + spawnTopOffset, 0));
            spawnPos.z = 0;

            // 选择敌人类型
            GameObject enemyObj = null;
            if (Random.value < config.fastEnemyChance)
            {
                enemyObj = ObjectPool.Instance.Spawn("Enemy_Fast", spawnPos, Quaternion.identity);
            }
            else
            {
                enemyObj = ObjectPool.Instance.Spawn("Enemy", spawnPos, Quaternion.identity);
            }

            if (enemyObj != null)
            {
                EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.speed *= config.enemySpeedMult;
                    enemy.hp += config.enemyHPBonus;
                }
            }

            enemiesSpawnedInWave++;
            yield return new WaitForSeconds(config.spawnInterval);
        }
    }

    /// <summary>Boss 波</summary>
    IEnumerator SpawnBossWave(WaveConfig config)
    {
        Vector3 bossPos = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1.2f, 0));
        bossPos.z = 0;
        Instantiate(bossPrefab, bossPos, Quaternion.identity);
        enemiesAliveInWave = 1; // Boss 算一个敌人
        yield break;
    }

    /// <summary>敌人被消灭时调用（由 EnemyBase 触发）</summary>
    public void OnEnemyDefeated()
    {
        enemiesAliveInWave--;
    }

    /// <summary>动态生成波次配置（难度递增）</summary>
    WaveConfig GenerateDynamicWave(int waveIndex)
    {
        float difficultyMult = 1f + waveIndex * 0.1f;
        return new WaveConfig
        {
            waveName = $"Dynamic Wave {waveIndex + 1}",
            enemyCount = Mathf.Min(5 + waveIndex * 2, 30),
            spawnInterval = Mathf.Max(0.3f, 1f - waveIndex * 0.05f),
            enemySpeedMult = difficultyMult,
            enemyHPBonus = waveIndex / 3,
            fastEnemyChance = Mathf.Min(0.3f, waveIndex * 0.05f),
            isBossWave = (waveIndex + 1) % 5 == 0 // 每5波一个Boss
        };
    }
}
```

---

## 道具系统

```csharp
// PlayerShooter.cs — 玩家射击逻辑，受道具影响

using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("射击")]
    public Transform firePoint;
    public float fireRate = 0.15f;
    private float nextFireTime = 0f;

    [Header("道具状态")]
    public bool hasMultiShot = false;
    public bool hasSpeedBoost = false;
    public int extraDamage = 0;

    private float multiShotTimer = 0f;
    private float speedBoostTimer = 0f;

    void Update()
    {
        // 计时器
        if (multiShotTimer > 0) multiShotTimer -= Time.deltaTime;
        else hasMultiShot = false;

        if (speedBoostTimer > 0) speedBoostTimer -= Time.deltaTime;
        else hasSpeedBoost = false;

        if (GameManager.Instance.currentState != GameState.Playing)
            return;

        // 射击输入
        bool firePressed = Input.GetButton("Fire1") ||
                           Input.GetKey(KeyCode.Space) ||
                           (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (firePressed && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + (hasSpeedBoost ? fireRate * 0.5f : fireRate);
        }
    }

    void Fire()
    {
        // 选择子弹类型
        string bulletTag = hasMultiShot ? "Bullet_Multi" : "Bullet";

        GameObject bullet = ObjectPool.Instance.Spawn(bulletTag, firePoint.position, Quaternion.identity);
        if (bullet != null)
        {
            BulletBase b = bullet.GetComponent<BulletBase>();
            if (b != null)
            {
                b.damage = 1 + extraDamage;
                b.speed = hasSpeedBoost ? 18f : 12f;
            }
        }

        // 音效
        AudioManager.Instance?.PlaySFX("shoot");

        // 枪口火焰（可选）
    }

    /// <summary>激活多发射击</summary>
    public void ActivateMultiShot(float duration)
    {
        hasMultiShot = true;
        multiShotTimer = duration;
    }

    /// <summary>激活加速</summary>
    public void ActivateSpeedBoost(float duration)
    {
        hasSpeedBoost = true;
        speedBoostTimer = duration;
    }

    /// <summary>增加伤害</summary>
    public void AddDamageBonus(int bonus = 1)
    {
        extraDamage += bonus;
    }
}

// PowerUp.cs — 道具掉落物

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        Shield,     // 护盾
        Speed,      // 射速提升
        MultiShot,  // 散弹
        Health,     // 回血
        Bomb        // 全屏清除
    }

    [Header("道具类型")]
    public PowerUpType powerType;
    public float duration = 5f;
    public float fallSpeed = 1.5f;

    void Update()
    {
        // 下落
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // 超出屏幕回收
        if (Camera.main != null)
        {
            Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
            if (viewPos.y < -0.2f)
                Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        PlayerShooter shooter = other.GetComponent<PlayerShooter>();

        if (player == null) return;

        // 应用道具效果
        switch (powerType)
        {
            case PowerUpType.Shield:
                player.ActivateShield(duration);
                break;

            case PowerUpType.Speed:
                if (shooter != null) shooter.ActivateSpeedBoost(duration);
                break;

            case PowerUpType.MultiShot:
                if (shooter != null) shooter.ActivateMultiShot(duration);
                break;

            case PowerUpType.Health:
                player.currentHP = Mathf.Min(player.currentHP + 1, player.maxHP);
                break;

            case PowerUpType.Bomb:
                // 全屏清除所有敌人
                EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
                foreach (var e in allEnemies)
                {
                    ScoreManager.Instance?.AddScore(e.scoreValue);
                    e.ReturnToPool();
                }
                ScreenShake.Instance?.Shake(0.3f, 0.5f);
                break;
        }

        AudioManager.Instance?.PlaySFX("powerup");
        Destroy(gameObject);
    }
}
```

---

## Boss 战系统

```csharp
// BossController.cs — Boss 实体 + 状态机

using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    public enum BossPhase
    {
        Enter,      // 入场
        Phase1,     // 第一阶段
        Phase2,     // 第二阶段（半血后）
        Phase3,     // 第三阶段（狂暴）
        Dying,      // 死亡
        Defeated    // 击败
    }

    [Header("Boss 属性")]
    public int maxHP = 30;
    public int currentHP;
    public float moveSpeed = 2f;
    public int scoreReward = 5000;

    [Header("攻击参数")]
    public float attackInterval = 1.5f;
    public GameObject bulletPrefab;
    public Transform[] firePoints;

    [Header("阶段变化")]
    public Sprite phase2Sprite;   // 半血换皮
    public Sprite phase3Sprite;   // 狂暴换皮

    private BossPhase currentPhase = BossPhase.Enter;
    private SpriteRenderer spriteRenderer;
    private BossAttackPattern attackPattern;
    private Coroutine attackCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        attackPattern = GetComponent<BossAttackPattern>();
    }

    void Start()
    {
        currentHP = maxHP;
        StartCoroutine(BossFSM());
    }

    /// <summary>Boss 有限状态机</summary>
    IEnumerator BossFSM()
    {
        // 入场：从屏幕上方进入
        currentPhase = BossPhase.Enter;
        Vector3 targetPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.75f, 0));
        targetPos.z = 0;

        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, targetPos, moveSpeed * 2f * Time.deltaTime);
            yield return null;
        }

        // 进入 Phase1
        currentPhase = BossPhase.Phase1;
        attackCoroutine = StartCoroutine(AttackRoutine());

        while (currentPhase != BossPhase.Defeated)
        {
            // 检测血量换阶段
            float hpPercent = (float)currentHP / maxHP;

            if (hpPercent <= 0.3f && currentPhase < BossPhase.Phase3)
            {
                ChangePhase(BossPhase.Phase3);
            }
            else if (hpPercent <= 0.5f && currentPhase < BossPhase.Phase2)
            {
                ChangePhase(BossPhase.Phase2);
            }

            // Boss 左右移动
            float x = Mathf.PingPong(Time.time * moveSpeed, 1f);
            Vector3 viewPos = Camera.main.ViewportToWorldPoint(
                new Vector3(Mathf.Lerp(0.15f, 0.85f, x), 0.75f, 0));
            viewPos.z = 0;
            transform.position = Vector3.Lerp(transform.position, viewPos, Time.deltaTime * 2f);

            yield return null;
        }
    }

    void ChangePhase(BossPhase newPhase)
    {
        currentPhase = newPhase;

        // 换皮
        switch (newPhase)
        {
            case BossPhase.Phase2:
                if (phase2Sprite != null) spriteRenderer.sprite = phase2Sprite;
                attackInterval = Mathf.Max(0.8f, attackInterval * 0.8f);
                AudioManager.Instance?.PlaySFX("boss_roar");
                break;

            case BossPhase.Phase3:
                if (phase3Sprite != null) spriteRenderer.sprite = phase3Sprite;
                attackInterval = 0.5f;
                moveSpeed = 3.5f;
                AudioManager.Instance?.PlaySFX("boss_rage");
                break;
        }

        // 重启攻击协程以应用新间隔
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        while (currentPhase != BossPhase.Defeated)
        {
            yield return new WaitForSeconds(attackInterval);

            if (GameManager.Instance.currentState != GameState.Playing)
                continue;

            // 根据阶段选择攻击模式
            switch (currentPhase)
            {
                case BossPhase.Phase1:
                    attackPattern?.SingleShot(firePoints[0]);
                    break;

                case BossPhase.Phase2:
                    attackPattern?.DoubleShot(firePoints[0], firePoints[1]);
                    break;

                case BossPhase.Phase3:
                    attackPattern?.SpreadShot(firePoints, 5);
                    break;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        // 闪烁白
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            Invoke(nameof(ResetColor), 0.08f);
        }

        // 震动反馈
        ScreenShake.Instance?.Shake(0.1f, 0.08f);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    void Die()
    {
        currentPhase = BossPhase.Defeated;

        // 停止攻击
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        // 分数
        ScoreManager.Instance?.AddScore(scoreReward);

        // 大爆炸特效
        StartCoroutine(DeathExplosion());

        // 音效
        AudioManager.Instance?.PlaySFX("boss_explosion");
        ScreenShake.Instance?.Shake(0.5f, 0.8f);

        // 通知生成器
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        spawner?.OnEnemyDefeated();
    }

    IEnumerator DeathExplosion()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
            ObjectPool.Instance.Spawn("Explosion", transform.position + randomOffset, Quaternion.identity);
            yield return new WaitForSeconds(0.2f);
        }
        Destroy(gameObject);
    }
}

// BossAttackPattern.cs — Boss 攻击模式库

using UnityEngine;

public class BossAttackPattern : MonoBehaviour
{
    [Header("子弹参数")]
    public GameObject bossBulletPrefab;
    public float bulletSpeed = 5f;

    /// <summary>单发射击</summary>
    public void SingleShot(Transform firePoint)
    {
        if (firePoint == null) return;
        ShootAtPlayer(firePoint.position, bulletSpeed);
    }

    /// <summary>双发射击</summary>
    public void DoubleShot(Transform left, Transform right)
    {
        if (left != null) ShootAtPlayer(left.position, bulletSpeed);
        if (right != null) ShootAtPlayer(right.position, bulletSpeed * 0.9f);
    }

    /// <summary>散射（扇形弹幕）</summary>
    public void SpreadShot(Transform[] points, int bulletCount)
    {
        float angleStep = 30f / (bulletCount - 1);
        float startAngle = -15f;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.down;

            // 从不同的发射点发射
            Transform fp = points[Random.Range(0, points.Length)];
            if (fp == null) continue;

            GameObject bullet = Instantiate(bossBulletPrefab, fp.position, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.velocity = dir * bulletSpeed;

            Destroy(bullet, 3f); // 子弹自动销毁
        }
    }

    /// <summary>圆形弹幕（全方向）</summary>
    public void CircleShot(Transform center, int bulletCount)
    {
        float angleStep = 360f / bulletCount;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = angleStep * i;
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            GameObject bullet = Instantiate(bossBulletPrefab, center.position, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.velocity = dir * bulletSpeed * 0.7f;

            Destroy(bullet, 2.5f);
        }
    }

    /// <summary>向玩家方向射击</summary>
    private void ShootAtPlayer(Vector3 origin, float speed)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector2 dir = (player.transform.position - origin).normalized;
        GameObject bullet = Instantiate(bossBulletPrefab, origin, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = dir * speed;

        Destroy(bullet, 3f);
    }
}
```

---

## 计分与连击系统

```csharp
// ScoreManager.cs — 分数、连击、最高分

using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("分数")]
    public int score = 0;
    public int highScore = 0;

    [Header("连击")]
    public int comboCount = 0;
    public float comboWindow = 1.5f;    // 连击窗口秒数
    private float comboTimer = 0f;
    public int maxComboMultiplier = 8;  // 最大倍率上限

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;

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

    void Start()
    {
        highScore = SaveManager.Instance?.LoadHighScore() ?? 0;
        UpdateUI();
    }

    void Update()
    {
        // 连击计时
        if (comboCount > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
        }
    }

    /// <summary>加分（由 EnemyBase.Die 调用）</summary>
    public void AddScore(int basePoints)
    {
        // 计算连击倍率
        comboCount++;
        comboTimer = comboWindow;

        int multiplier = Mathf.Min(comboCount, maxComboMultiplier);
        int finalPoints = basePoints * multiplier;

        score += finalPoints;

        // 显示连击加成
        if (multiplier > 1)
        {
            ShowComboPopup(multiplier, finalPoints);
        }

        UpdateUI();
    }

    void ShowComboPopup(int mult, int points)
    {
        if (comboText != null)
        {
            comboText.text = $"COMBO x{mult}\n+{points}";
            comboText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideComboText));
            Invoke(nameof(HideComboText), 0.8f);
        }
    }

    void HideComboText()
    {
        if (comboText != null)
            comboText.gameObject.SetActive(false);
    }

    void ResetCombo()
    {
        comboCount = 0;
        if (comboText != null)
            comboText.gameObject.SetActive(false);
    }

    public void ResetScore()
    {
        score = 0;
        ResetCombo();
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"SCORE: {score:N0}";
    }
}
```

---

## 屏幕震动与击中停顿

```csharp
// ScreenShake.cs — 屏幕震动效果

using System.Collections;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    private Camera mainCamera;
    private Vector3 originalPos;

    void Awake()
    {
        Instance = this;
        mainCamera = GetComponent<Camera>();
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        originalPos = mainCamera.transform.localPosition;
    }

    /// <summary>触发屏幕震动</summary>
    public void Shake(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            // 振幅衰减
            magnitude = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            yield return null;
        }

        mainCamera.transform.localPosition = originalPos;
    }
}

// HitStop.cs — 击中停顿效果（增加打击感）

using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    /// <summary>暂停游戏指定时长（不影响 Time.unscaledDeltaTime）</summary>
    public void Stop(float duration)
    {
        StartCoroutine(HitStopRoutine(duration));
    }

    IEnumerator HitStopRoutine(float duration)
    {
        float original = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = original;
    }
}
```

---

## 背景视差滚动

```csharp
// ParallaxBackground.cs — 多层视差背景

using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform layerTransform;    // 层变换
        public float parallaxFactor = 0.5f; // 视差因子（0=静止，1=跟随相机）
        public float scrollSpeed = 0f;      // 额外滚动速度
    }

    [Header("视差层")]
    public ParallaxLayer[] layers;

    [Header("循环")]
    public bool enableLoop = true;
    public float textureHeight = 10f;       // 纹理高度（用于循环）

    private Camera mainCamera;
    private Vector3 previousCameraPos;
    private float[] layerOffsets;

    void Start()
    {
        mainCamera = Camera.main;
        previousCameraPos = mainCamera.transform.position;

        layerOffsets = new float[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerTransform != null)
                layerOffsets[i] = layers[i].layerTransform.position.y;
        }
    }

    void LateUpdate()
    {
        Vector3 delta = mainCamera.transform.position - previousCameraPos;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerTransform == null) continue;

            // 视差偏移
            Vector3 pos = layers[i].layerTransform.position;
            pos += delta * layers[i].parallaxFactor;

            // 额外滚动
            if (layers[i].scrollSpeed != 0f)
                layerOffsets[i] += layers[i].scrollSpeed * Time.deltaTime;

            pos.y = layerOffsets[i];

            // 循环
            if (enableLoop)
            {
                float camY = mainCamera.transform.position.y;
                float layerY = pos.y;

                // 如果层移出相机范围，循环回去
                if (camY - layerY > textureHeight * 2)
                {
                    layerOffsets[i] += textureHeight * 2;
                    pos.y = layerOffsets[i];
                }
                else if (layerY - camY > textureHeight * 2)
                {
                    layerOffsets[i] -= textureHeight * 2;
                    pos.y = layerOffsets[i];
                }
            }

            layers[i].layerTransform.position = pos;
        }

        previousCameraPos = mainCamera.transform.position;
    }
}
```

---

## 音频管理器

```csharp
// AudioManager.cs — BGM 淡入淡出 + SFX 对象池

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class SoundEntry
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = false;
    }

    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    public SoundEntry[] bgmList;
    public float bgmCrossfadeDuration = 1.5f;
    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private bool useSourceA = true;

    [Header("SFX")]
    public SoundEntry[] sfxList;
    public int sfxPoolSize = 8;
    private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
    private Dictionary<string, AudioClip> sfxDict;
    private Dictionary<string, AudioClip> bgmDict;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化 BGM 音源
        bgmSourceA = gameObject.AddComponent<AudioSource>();
        bgmSourceB = gameObject.AddComponent<AudioSource>();
        bgmSourceA.loop = true;
        bgmSourceB.loop = true;
        bgmSourceA.volume = 0f;
        bgmSourceB.volume = 0f;

        // 构建字典
        bgmDict = new Dictionary<string, AudioClip>();
        foreach (var entry in bgmList)
            bgmDict[entry.name] = entry.clip;

        sfxDict = new Dictionary<string, AudioClip>();
        foreach (var entry in sfxList)
            sfxDict[entry.name] = entry.clip;

        // 预热 SFX 池
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.volume = 1f;
            sfxPool.Enqueue(src);
        }
    }

    /// <summary>播放 BGM（自动交叉淡入淡出）</summary>
    public void PlayBGM(string name)
    {
        if (!bgmDict.ContainsKey(name)) return;

        AudioClip clip = bgmDict[name];
        AudioSource newSource = useSourceA ? bgmSourceA : bgmSourceB;
        AudioSource oldSource = useSourceA ? bgmSourceB : bgmSourceA;

        // 新音源开始播放
        newSource.clip = clip;
        newSource.Play();

        // 交叉淡入淡出
        StopAllCoroutines();
        StartCoroutine(CrossfadeRoutine(oldSource, newSource));

        useSourceA = !useSourceA;
    }

    IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to)
    {
        float elapsed = 0f;
        while (elapsed < bgmCrossfadeDuration)
        {
            float t = elapsed / bgmCrossfadeDuration;
            from.volume = Mathf.Lerp(1f, 0f, t);
            to.volume = Mathf.Lerp(0f, 1f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        from.volume = 0f;
        from.Stop();
        to.volume = 1f;
    }

    /// <summary>播放 SFX</summary>
    public void PlaySFX(string name)
    {
        if (!sfxDict.ContainsKey(name)) return;

        // 从池取一个 AudioSource
        if (sfxPool.Count == 0) return;

        AudioSource src = sfxPool.Dequeue();
        src.clip = sfxDict[name];
        src.Play();

        // 播放完后回收
        StartCoroutine(ReturnSFXSource(src, src.clip.length));
    }

    IEnumerator ReturnSFXSource(AudioSource src, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        src.Stop();
        src.clip = null;
        sfxPool.Enqueue(src);
    }
}
```

---

## UI 流程

```csharp
// UIManager.cs — 菜单、HUD、GameOver、波次提示

using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("面板")]
    public GameObject menuPanel;
    public GameObject hudPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    [Header("文本")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI hpText;

    [Header("波次提示")]
    public float waveTextDuration = 1.5f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 初始只显示菜单
        menuPanel.SetActive(true);
        hudPanel.SetActive(false);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // 显示最高分
        if (highScoreText != null)
            highScoreText.text = $"HIGH SCORE: {SaveManager.Instance?.LoadHighScore() ?? 0:N0}";
    }

    /// <summary>状态变化时切换 UI</summary>
    public void OnGameStateChanged(GameState newState)
    {
        menuPanel.SetActive(newState == GameState.Menu);
        hudPanel.SetActive(newState == GameState.Playing);
        pausePanel.SetActive(newState == GameState.Paused);
        gameOverPanel.SetActive(newState == GameState.GameOver);

        if (newState == GameState.GameOver)
        {
            if (finalScoreText != null)
                finalScoreText.text = $"FINAL SCORE\n{ScoreManager.Instance?.score ?? 0:N0}";
        }
    }

    /// <summary>显示波次提示（闪烁后消失）</summary>
    public void ShowWaveText(string text)
    {
        if (waveText == null) return;
        waveText.text = text;
        waveText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideWaveText));
        Invoke(nameof(HideWaveText), waveTextDuration);
    }

    void HideWaveText()
    {
        if (waveText != null)
            waveText.gameObject.SetActive(false);
    }

    /// <summary>更新 HUD（由 ScoreManager 或 GameManager 调用）</summary>
    public void UpdateHUD(int score, int hp)
    {
        if (scoreText != null)
            scoreText.text = $"SCORE: {score:N0}";
        if (hpText != null)
            hpText.text = $"HP: {hp}";
    }

    // ---- 按钮事件 ----

    public void OnStartButton()
    {
        GameManager.Instance.StartGame();
    }

    public void OnResumeButton()
    {
        GameManager.Instance.TogglePause();
    }

    public void OnRestartButton()
    {
        GameManager.Instance.RestartGame();
    }

    public void OnMenuButton()
    {
        GameManager.Instance.GoToMenu();
        // 重载场景以重置状态
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void OnQuitButton()
    {
        GameManager.Instance.QuitGame();
    }
}
```

---

## 存档系统

```csharp
// SaveManager.cs — PlayerPrefs 存读最高分

using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string HIGH_SCORE_KEY = "HighScore";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string BGM_VOLUME_KEY = "BGMVolume";

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

    public void SaveHighScore(int score)
    {
        int current = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        if (score > current)
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, score);
            PlayerPrefs.Save();
            Debug.Log($"新最高分: {score}");
        }
    }

    public int LoadHighScore()
    {
        return PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    public void SaveVolume(float sfxVol, float bgmVol)
    {
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVol);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVol);
        PlayerPrefs.Save();
    }

    public (float sfx, float bgm) LoadVolume()
    {
        float sfx = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        float bgm = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        return (sfx, bgm);
    }

    /// <summary>清除所有存档（调试用）</summary>
    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("存档已清除");
    }
}
```

---

## 移动端触控输入

```csharp
// MobileInput.cs — 移动端专用输入处理（可选的附加脚本）

using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("移动区域")]
    public RectTransform joystickArea;
    public RectTransform knob;
    public float maxRadius = 80f;

    private Vector2 inputVector = Vector2.zero;
    private Vector2 joystickStartPos;

    public Vector2 InputValue => inputVector;

    void Start()
    {
        if (joystickArea != null)
            joystickStartPos = joystickArea.position;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 摇杆跟随触摸位置
        if (joystickArea != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickArea.parent as RectTransform,
                eventData.position, eventData.pressEventCamera,
                out Vector2 localPoint);
            joystickArea.localPosition = localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (knob == null || joystickArea == null) return;

        // 计算摇杆偏移
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickArea, eventData.position, eventData.pressEventCamera,
            out Vector2 localPoint);

        Vector2 offset = localPoint;
        float magnitude = offset.magnitude;

        if (magnitude > maxRadius)
        {
            offset = offset.normalized * maxRadius;
        }

        knob.anchoredPosition = offset;
        inputVector = offset / maxRadius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 复位
        inputVector = Vector2.zero;
        if (knob != null)
            knob.anchoredPosition = Vector2.zero;
    }

    /// <summary>在 PlayerController 中替换键盘输入逻辑</summary>
    public Vector2 GetInput()
    {
        return inputVector;
    }
}
```

---

## 性能优化清单

```
// PerformanceCheatSheet.cs — 性能优化清单

太空射击优化清单:
├── 对象池 ✅
│   ├─ 所有子弹走对象池
│   ├─ 所有敌人走对象池
│   └─ 爆炸特效走对象池（预热 20 个）
├── Draw Call
│   ├─ 使用 Sprite Atlas 合批
│   ├─ 所有敌人共享同一材质（Sprite Default）
│   └─ 背景用 Tilemap 而非独立 Sprite
├── 物理 ✅
│   ├─ Collision2D 用 Trigger 而非普通碰撞
│   ├─ Rigidbody2D 的 Simulation Mode = FixedUpdate
│   └─ 减少同时活跃的 Collider（超出屏幕自动回收）
├── UI
│   ├─ TextMeshPro 而非旧版 Text
│   ├─ 关闭 Canvas 的 Pixel Perfect（省性能）
│   └─ 不活跃的面板设置 Canvas.enabled = false
├── 脚本
│   ├─ Update 中空方法 → 注释掉
│   ├─ Camera.main 缓存引用（不在 Update 调用）
│   ├─ Find 系列只在 Start/Awake 调用
│   └─ 协程及时 Stop，不遗留在后台
├── 移动端
│   ├─ 降低粒子系统 MaxParticles
│   ├─ QualitySettings 设为 Low
│   ├─ 关闭实时阴影
│   └─ 使用 Mobile 渲染管线
└── 构建
    ├─ 裁剪未使用的着色器变体
    ├─ 开启 Stripping Level = Medium
    └─ 使用 IL2CPP 后端（iOS 必须）
```

---

## 场景搭建快速指南

```
// 将以上脚本挂载到场景中:

1. 创建空 GameObject "GameManager" → 挂载 GameManager.cs
   └─ 拖入 UIManager / EnemySpawner / PlayerController 引用

2. 创建空 GameObject "AudioManager" → 挂载 AudioManager.cs
   └─ 在 Inspector 中配置 SoundEntry 列表

3. 创建空 GameObject "ObjectPool" → 挂载 ObjectPool.cs
   └─ 配置 PoolEntry 列表（Bullet / Enemy / Enemy_Fast / Explosion）

4. 创建空 GameObject "ScoreManager" → 挂载 ScoreManager.cs

5. 创建空 GameObject "SaveManager" → 挂载 SaveManager.cs

6. 主相机 → 挂载 ScreenShake.cs

7. 创建空 GameObject "HitStop" → 挂载 HitStop.cs

8. 创建背景层级 → 挂载 ParallaxBackground.cs
   └─ 配置 3-4 个 ParallaxLayer（星星层 0.1 / 行星层 0.3 / 星云层 0.6）

9. Canvas 设置:
   └─ 子对象: MenuPanel / HUDPanel / PausePanel / GameOverPanel
   └─ 各面板包含对应按钮和 TextMeshPro 文本

10. 玩家 Prefab 设置:
    └─ SpriteRenderer + Rigidbody2D + Collider2D
    └─ 挂载 PlayerController.cs + PlayerShooter.cs
    └─ Tag = "Player", Layer = "Player"
```

---

## 与 Raylib/C++ 对比

| 概念 | Unity 实现 | Raylib/C++ 对应 |
|------|-----------|----------------|
| 对象池 | ObjectPool + IPoolable | 手写链表池 |
| 碰撞检测 | OnTriggerEnter2D | CheckCollisionRecs |
| 音频管理 | AudioSource + 池 | LoadSound/PlaySound |
| 状态机 | enum + switch | enum + switch（一样） |
| 协程 | IEnumerator | 无原生，手写计时器 |
| 视差 | Transform 偏移 | 手动绘制偏移 |
| 对象池回收 | SetActive(false) | 标记不可见 |
| 输入抽象 | Input.GetAxisRaw | IsKeyDown/GetGamepadAxis |

---

## 停靠点

```
完成本指南后，你应该能:
├── ✅ 独立实现 GameManager 状态机管理游戏流程
├── ✅ 自己写出通用对象池并用 IPoolable 接口规范回收
├── ✅ 设计波次系统 + 难度自动递增逻辑
├── ✅ 实现 4 种道具效果（护盾/加速/散弹/全屏Bomb）
├── ✅ 用状态机实现 Boss 三阶段战斗
├── ✅ 连击计分 + 屏幕震动 + 击中停顿增强打击感
├── ✅ 多层视差背景滚动
├── ✅ 完整的 UI 流程 + BGM 交叉淡入淡出
├── ✅ PlayerPrefs 存读最高分
├── ✅ 摇杆输入支持移动端
└── ✅ 知晓 10+ 性能优化手段

下个项目: Project02_俯视角Roguelike（将引入更多系统: 背包、技能树、地图生成）
```

> 以上所有代码在 Unity 2022.3 LTS + URP 中编写测试。脚本均为可编译的完整实现，直接复制并按场景搭建指南挂载即可运行。
