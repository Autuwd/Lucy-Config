# Day 13：动画系统 — 从关键帧插值到状态机调度

## 0. 为什么需要动画系统？

在 Raylib/C++ 中，动画是手动逐帧切换的：

```cpp
// Raylib：手动帧动画
int frame = 0;
float timer = 0;

void Update() {
    timer += GetFrameTime();
    if (timer >= 0.1f) {  // 每 0.1 秒切换一帧
        frame = (frame + 1) % 4;
        timer = 0;
    }
    
    // 根据当前帧选择纹理区域
    srcRec.x = frame * frameWidth;
    DrawTexturePro(sprite, srcRec, destRec, origin, rotation, WHITE);
}
```

Unity 的动画系统把这一切自动化了：
- **Animation Clip** 定义"怎么动"（关键帧数据）
- **Animator Controller** 定义"什么时候动"（状态机）
- **Animator** 组件负责驱动

---

## 1. Animation Clip——动画数据

### 关键帧插值原理

```
Animation Clip 存储的是关键帧（Keyframe）数据：

Position.x 曲线：
1.0 ┤        ●──────
    │       /
0.5 ┤      ●
    │     /
0.0 ┤────●───────────
    └──────────────────
    时间: 0s  0.5s  1s

关键帧：
(0.0s, 0.0) → 线性插值 → (0.5s, 0.5) → 线性插值 → (1.0s, 1.0)

插值公式：
在 t1 和 t2 之间的任意时间 t：
value = value1 + (value2 - value1) * (t - t1) / (t2 - t1)
```

### Animation Clip 的结构

```
Animation Clip
├── 名称: "Run"
├── 时长: 1.5 秒
├── 帧率: 30 fps
├── 循环: true
└── 曲线 (Curves):
    ├── Transform.position.x (关键帧数组)
    ├── Transform.position.y (关键帧数组)
    ├── Transform.position.z (关键帧数组)
    ├── Transform.rotation.x (关键帧数组)
    ├── Transform.rotation.y (关键帧数组)
    ├── Transform.rotation.z (关键帧数组)
    ├── Transform.rotation.w (关键帧数组)
    └── 其他属性曲线
```

### 创建和使用 Animation Clip

```csharp
// 在代码中创建动画（不常用，但理解原理）
public class RuntimeAnimation : MonoBehaviour
{
    void Start()
    {
        // 创建 Animation Clip
        AnimationClip clip = new AnimationClip();
        clip.legacy = false;  // 使用 Mecanim 动画系统
        clip.wrapMode = WrapMode.Loop;  // 循环播放

        // 添加曲线
        AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 10f);
        // 参数：timeStart, valueStart, timeEnd, valueEnd

        // 将曲线绑定到 Transform.position.x
        clip.SetCurve("", typeof(Transform), "localPosition.x", curve);

        // 应用到 Animator
        Animator animator = GetComponent<Animator>();
        // 通常通过 Animator Controller 引用 Clip
    }
}
```

---

## 2. Animator Controller——状态机

### 状态机的概念

```
Animator Controller = 状态机

状态 (State)：
- Idle：静止状态
- Run：跑步状态
- Jump：跳跃状态（一次性）
- Dead：死亡状态

参数 (Parameter)：
- Speed (Float)：控制 Idle ↔ Run 切换
- IsGrounded (Bool)：控制 → Jump
- Die (Trigger)：控制 → Dead

过渡 (Transition)：
Idle ──[Speed > 0.1]──→ Run
Run  ──[Speed < 0.1]──→ Idle
Any  ──[Die]──────────→ Dead
```

### 参数类型

```csharp
// Animator 支持四种参数类型：
// 1. Float：连续值（速度、方向）
// 2. Int：整数值（状态编号）
// 3. Bool：布尔值（是否活着、是否着地）
// 4. Trigger：触发一次（跳、攻击）— 使用后自动复位

public class AnimationControl : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 设置 Float 参数
        float speed = Input.GetAxis("Horizontal");
        animator.SetFloat("Speed", Mathf.Abs(speed));

        // 设置 Bool 参数
        animator.SetBool("IsGrounded", CheckGrounded());

        // 触发 Trigger 参数（瞬态，只触发一次）
        if (Input.GetButtonDown("Jump") && CheckGrounded())
        {
            animator.SetTrigger("Jump");
        }
    }
}
```

### Blend Tree——平滑混合

```
Blend Tree = 多个动画之间的平滑过渡

1D Blend Tree（一维混合）：
输入参数：Speed (0~1)
├── Speed = 0：Idle
├── Speed = 0.5：Walk
└── Speed = 1.0：Run

当 Speed 从 0 到 1 变化时，
Idle → Walk → Run 无缝混合！

2D Blend Tree（二维混合）：
输入参数：Forward (0~1), Right (-1~1)
         ←左  ←左上  ↑上  ↑右上  →右
         等等...
```

---

## 3. Avatar 与骨骼系统

### 骨骼动画的原理

```
骨骼层级树（Skeleton Hierarchy）：
Hips (根骨骼)
├── Spine
│   ├── Chest
│   │   ├── UpperArm_L
│   │   │   ├── ForeArm_L → Hand_L
│   │   └── UpperArm_R → ForeArm_R → Hand_R
│   └── Neck → Head
└── UpperLeg_L → LowerLeg_L → Foot_L
└── UpperLeg_R → LowerLeg_R → Foot_R

蒙皮（Skinning）：每个顶点受到多个骨骼的影响（带权重）
顶点位置 = Σ(骨骼变换矩阵 × 顶点偏移 × 骨骼权重)
```

### Avatar——骨骼映射

```csharp
// Avatar 是 Humanoid 角色骨骼的映射配置
// 将你的模型的骨骼映射到 Unity 的标准 Humanoid 骨架

// Humanoid 动画重定向（Animation Retargeting）：
// 一个标准人形动画可以应用到任何标准人形模型上！

// 例如：
// 模型 A（高个子男性）的跑步动画
// 应用到 模型 B（矮个子女性）上
// 动画自动适配不同的骨骼比例！

// 要求：两个模型都必须配置 Humanoid Avatar
```

---

## 4. 动画事件

```csharp
// 在动画的特定帧触发事件

// 在 Animation 窗口中：
// 1. 选择时间点
// 2. 右键 → Add Animation Event
// 3. 选择要调用的函数

// 代码中定义事件函数：
public class AnimationEventHandler : MonoBehaviour
{
    // 动画事件函数（public，无参数或只有一个参数）

    public void Footstep()
    {
        // 在脚落地时播放脚步声
        AudioSource.PlayClipAtPoint(footstepSound, transform.position);
    }

    public void AttackHit()
    {
        // 在攻击动画的命中帧时调用
        // 执行伤害判定
        Debug.Log("Attack landed!");
    }

    public void SpawnProjectile()
    {
        // 在特定帧生成子弹
        Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
```

---

## 5. 性能优化

### 动画压缩

```csharp
// Unity 提供三种动画压缩方式：

// 1. Keyframe Reduction（关键帧缩减）
//    移除冗余的关键帧（线性插值可以推导的中间帧）
//    无损压缩，建议始终开启

// 2. Optimal（最优压缩）
//    将浮点误差控制在指定精度内
//    有损但人眼难以察觉

// 3. 浮点精度压缩
//    将 float 压缩为 half (16位浮点)
//    进一步减小动画数据大小
```

### LOD 动画

```csharp
// 远处的角色可以使用低精度的动画：
// - 近处：播放完整动画（60 FPS 更新骨骼）
// - 远处：降低动画帧率（15 FPS 更新骨骼）
// - 极远：停止动画，循环最后一帧

// 在 Animator 中设置：
// Inspector → Animator → Update Mode

// Normal：每帧更新（推荐，默认）
// Animate Physics：与 FixedUpdate 同步
// Unscaled Time：不受 Time.timeScale 影响（用于菜单）
```

### 动画层级（Animation Layer）

```csharp
// Animator 可以包含多个 Layer，每个 Layer 有独立的状态机
// 用于"叠加"动画

// Layer 1 (Base Layer)：身体整体动作（跑、跳、蹲）
// Layer 2 (Upper Body)：上半身附加动作（射击、招手）
// Layer 3 (Facial)：面部表情

// Layer 可以通过权重控制混合程度：
animator.SetLayerWeight(1, 0.5f);  // 上半身 50% 权重

// 应用示例：角色跑步同时射击
// 下半身：跑步动画（Layer 0）
// 上半身：射击动画（Layer 1，覆盖上半身）
```

---

## 练习：角色动画控制

```csharp
using UnityEngine;

public class Day13_Animation : MonoBehaviour
{
    [Header("Components")]
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 水平移动输入
        float h = Input.GetAxisRaw("Horizontal");

        // 移动
        rb.velocity = new Vector2(h * moveSpeed, rb.velocity.y);

        // 翻转 Sprite（面朝方向）
        if (h != 0)
            sprite.flipX = h < 0;

        // 设置动画参数
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsGrounded", IsGrounded());

        // 跳跃
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetTrigger("Jump");
        }

        // 攻击
        if (Input.GetButtonDown("Fire1"))
        {
            animator.SetTrigger("Attack");
        }
    }

    bool IsGrounded()
    {
        // 地面检测：从脚底发射向下射线
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            0.1f,
            LayerMask.GetMask("Ground")
        );
        return hit.collider != null;
    }
}
```

---

---

## C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity (C#) |
|------|-------------|-----------|
| 帧动画 | 手动 `frame++` 切换 | Animation Clip（关键帧插值） |
| 状态管理 | `enum + switch` | Animator Controller 状态机 |
| 状态切换 | 手动条件判断 | Transition + Parameter |
| 混合 | 手动插值 | Blend Tree 自动混合 |
| 骨骼动画 | 无 | Avatar + Skinning |
| — | 无 | 动画重定向（Retargeting） |
| 动画事件 | 无 | Animation Event |
| — | 无 | 动画层（Layer） |

## 停靠点

> Animation Clip = 关键帧数据（时间 → 值的曲线）。Animator Controller = 状态机（条件 → 状态切换）。
> Blend Tree 实现多动画平滑混合（参数驱动）。
> Humanoid Avatar 支持动画重定向——同一个动画在不同模型上复用。
> 动画事件在特定帧触发函数（脚步、攻击命中）。
> 动画性能优化：压缩关键帧、层级分离、LOD 动画。

