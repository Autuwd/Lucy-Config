# 项目实战：太空射击 — 3 天完成

> 对应学习计划中的第一个实战项目，综合运用 Day06~15 的知识

## 项目概览

| 项目 | 太空射击 (Space Shooter) |
|------|------------------------|
| 难度 | ★★☆ |
| 涉及知识点 | 输入、物理、Prefab、对象池、UI、协程、音效 |
| 预计时间 | 3 天 × 45 分钟 |
| 对照 Raylib | Raylib 中没有直接对应——但每个子系统你都手写过 |

## Day 1：玩家控制 + 射击

### 步骤

1. **新建 Unity 项目**（2D 模板，URP）

2. **创建 Player**
   - GameObject → 2D Object → Sprite（方形/圆圈都行）
   - 挂载 `Rigidbody2D` + `BoxCollider2D`
   - 写脚本：

```csharp
public class Player : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        rb.velocity = new Vector2(h, v).normalized * speed;

        // 限制在屏幕内
        Vector3 pos = Camera.main.WorldToViewportPoint(transform.position);
        pos.x = Mathf.Clamp01(pos.x);
        pos.y = Mathf.Clamp01(pos.y);
        transform.position = Camera.main.ViewportToWorldPoint(pos);
    }
}
```

3. **创建子弹 Prefab**
   - 小圆形 Sprite + Rigidbody2D + CircleCollider2D
   - 写子弹脚本 → 拖成 Prefab

```csharp
public class Bullet : MonoBehaviour
{
    public float speed = 10f;

    void OnEnable()
    {
        // 从对象池取出时自动移动
        GetComponent<Rigidbody2D>().velocity = Vector2.up * speed;
        Invoke(nameof(ReturnPool), 2f);
    }

    void ReturnPool() => gameObject.SetActive(false);
}
```

4. **添加射击**

```csharp
public class Player : MonoBehaviour
{
    public GameObject bulletPrefab;  // 拖入子弹 Prefab
    public Transform firePoint;
    public float fireRate = 0.15f;
    private float nextFire;

    void Update()
    {
        // ... 移动代码 ...

        if (Input.GetButton("Fire1") && Time.time > nextFire)
        {
            nextFire = Time.time + fireRate;
            Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        }
    }
}
```

**今晚检查点：** WASD 移动，空格射击，子弹向上飞。

## Day 2：敌人 + 碰撞 + 对象池

1. **敌人 Prefab**
   - 红色 Sprite + Collider2D
   - 从上往下移动，到达底部后销毁

```csharp
public class Enemy : MonoBehaviour
{
    public float speed = 2f;

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;

        // 超出屏幕底部自动回收
        if (Camera.main.WorldToViewportPoint(transform.position).y < -0.1f)
            gameObject.SetActive(false);
    }
}
```

2. **敌人生成器**

```csharp
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 1f;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            float x = Random.Range(0.1f, 0.9f);
            Vector3 pos = Camera.main.ViewportToWorldPoint(new Vector3(x, 1.1f, 0));
            Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
    }
}
```

3. **碰撞检测**

```csharp
public class Bullet : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            gameObject.SetActive(false);  // 回收子弹
            other.gameObject.SetActive(false);  // 回收敌人
            // 加分 + 播放音效
        }
    }
}
```

4. **改为对象池**

```csharp
// 用 Day09 的对象池替换 Instantiate
// 性能提升：零 GC 分配
```

**今晚检查点：** 敌人自动生成，子弹击中敌人后双方消失。

## Day 3：UI + 音效 + 完善

1. **得分 UI**

```csharp
public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    private int score;

    public void AddScore(int points)
    {
        score += points;
        scoreText.text = $"Score: {score}";
    }
}
```

2. **音效**
   - 导入音效资源（或自己录制简单的）
   - 射击时播放 `PlayOneShot`
   - 击中时播放爆炸音效

3. **游戏状态**

```csharp
public enum GameState { Menu, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public GameState state = GameState.Menu;

    public void StartGame()
    {
        state = GameState.Playing;
        // 重置得分、生成敌人
    }

    public void GameOver()
    {
        state = GameState.GameOver;
        Time.timeScale = 0f;  // 暂停游戏
        // 显示 Game Over 界面
    }
}
```

4. **最终检查清单**
   - [ ] 玩家移动流畅，边界限制正常
   - [ ] 射击手感好（射速合适）
   - [ ] 敌人持续生成，速度递增
   - [ ] 碰撞检测准确
   - [ ] 得分实时更新
   - [ ] 音效正常播放
   - [ ] Game Over 后能重开

## 扩展挑战（有空做）

| 功能 | 涉及知识点 |
|------|-----------|
| 不同的敌人类型 | 继承/多态 |
| Boss 战 | 状态机 + 弹幕 |
| 道具系统（加速/护盾） | Trigger + 协程 |
| 排行榜 | PlayerPrefs |
| 屏幕震动 | 协程 + 随机位移 |

## 项目架构总结

```
Scenes/
└── Main.unity
    ├── Player (Player.cs)
    │   └── FirePoint
    ├── EnemySpawner (EnemySpawner.cs)
    ├── GameManager (GameManager.cs)
    ├── Canvas
    │   └── ScoreText (TextMeshPro)
    └── Main Camera

Prefabs/
├── Bullet.prefab (Bullet.cs)
└── Enemy.prefab (Enemy.cs)
```

做完这个项目，你就把 C# 基础 → Unity 核心 → 进阶知识串起来了。
