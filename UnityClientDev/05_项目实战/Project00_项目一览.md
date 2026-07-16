# 项目实战：项目一览

## 可选项目

| 项目 | 难度 | 涉及知识点 | 预计天数 |
|------|------|-----------|---------|
| **太空射击** ↑ 已完成 | ★★☆ | Prefab、碰撞、对象池、UI、UI | 3 天 |
| **打砖块 (Unity 版)** | ★★☆ | 物理、Trigger、关卡管理 | 3 天 |
| **平台跳跃** | ★★★ | 物理、动画、摄像机跟随 | 4 天 |
| **简易 RPG** | ★★★★ | 状态机、对话、背包 | 5 天 |

## 各项目快速说明

### 打砖块 (Unity 版)

对照 Raylib 07，看 Unity 帮你省了多少代码：

| 功能 | Raylib (手写) | Unity (组件) |
|------|-------------|-------------|
| 碰撞检测 | 手写 AABB 4 个 if | BoxCollider2D + OnCollisionEnter2D |
| 砖块管理 | vector + erase | Destroy(gameObject) |
| 球反弹 | 手写方向计算 | Physics Material 2D → bounciness |
| 游戏状态 | enum + switch | SceneManager / 状态变量 |

**核心代码：**
```csharp
// Unity 版打砖块，所有碰撞代码就这 3 行
void OnCollisionEnter2D(Collision2D col)
{
    if (col.gameObject.CompareTag("Brick"))
        Destroy(col.gameObject);  // 比 erase 简洁多了
}
```

### 平台跳跃

| 功能 | 手写 (对标 Raylib) | Unity |
|------|-------------------|-------|
| 重力 | `vy += gravity * dt` | Rigidbody2D.gravityScale |
| 跳跃 | `vy = jumpForce` | `AddForce(impulse)` |
| 地面检测 | 手写 AABB 向下碰撞 | `Physics2D.Raycast` |
| 动画 | 手动 `frame++` | Animator + Blend Tree |

### 简易 RPG

| 系统 | 实现方式 |
|------|---------|
| 移动 | Rigidbody2D + 瓦片地图碰撞 |
| 对话 | 触发区域 + UI 面板 |
| 背包 | ScriptableObject 道具定义 + List 管理 |
| NPC | 状态机 (Idle → Talk → GiveQuest) |

## 通用开发流程

```
1. 在纸上画设计图     → 确定需要哪些 GameObject
2. 创建基础场景       → Player + 地面 + 墙壁
3. 实现核心循环       → 先让 Player 能动
4. 逐步加功能         → 碰撞 → 敌人 → UI → 音效
5. 打磨优化           → 调参数、加特效、优化 GC
```

## 我的建议

做完 **太空射击** 后，下一个做 **打砖块**——因为它和你之前做的 Raylib 07 是同一个游戏，你可以直接对比两种语言的差异和 Unity 替你做了多少事。
