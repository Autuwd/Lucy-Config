# Day 13：动画系统 — Playable API 与程序化动画

## 0. 为什么还需要深入学习动画系统？

Day13 的基础篇覆盖了 Animation Clip、状态机和 Blend Tree。但在真实项目中，你总会遇到基础动画系统搞不定的场景：

```
基础状态机能解决的问题：
- 角色走路/跑步/跳跃/攻击
- NPC 的简单动画循环
- UI 元素的基本动画

状态机搞不定的场景：
- 过场动画中多个角色同步演出
- 受击后身体的物理反馈（布娃娃过渡）
- 程序化生成的尾巴/触手摆动
- 手柄交互的实时 IK 解算
- 大规模集群单位的群组动画
```

这些场景需要 Playable API、Animation Rigging、C# Jobs 等进阶工具。

---

## 1. Playable API——动画系统的底层控制

### 1.1 什么是 Playable API？

```csharp
/*
 Playable API 是 Unity 动画系统的底层 API。
 它不像 Animator 那样封装成"状态机"，
 而是允许你直接操作"播放图"（PlayableGraph）。

 对比：
 ┌───────────────────────────────────────────────────────┐
 │ Animator（高层封装）                                   │
 │ - 状态机 + Transition                                  │
 │ - 适合：角色行为控制（90% 的场景）                      │
 │ - 灵活度：中等                                         │
 │                                                       │
 │ Playable API（底层控制）                                │
 │ - 直接连接和操作动画节点                                 │
 │ - 适合：程序化动画、多轨道混合、过场动画                  │
 │ - 灵活度：极高（你就是动画系统本身）                     │
 └───────────────────────────────────────────────────────┘
*/
```

### 1.2 创建你的第一个 PlayableGraph

```csharp
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animation;

public class PlayableGraphDemo : MonoBehaviour
{
    public AnimationClip runClip;
    public AnimationClip idleClip;
    
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    
    void Start()
    {
        // ─── 1. 创建 PlayableGraph ───
        graph = PlayableGraph.Create("CharacterGraph");
        
        // ─── 2. 创建输出节点 ───
        // 动画输出 → 连接到 Animator 组件
        AnimationPlayableOutput output =
            AnimationPlayableOutput.Create(graph, "AnimOutput",
                GetComponent<Animator>());
        
        // ─── 3. 创建混合器节点 ───
        // AnimationMixerPlayable 可以混合多个动画输入
        mixer = AnimationMixerPlayable.Create(graph, 2);  // 2 个输入
        
        // ─── 4. 创建动画片段节点 ───
        AnimationClipPlayable runPlayable =
            AnimationClipPlayable.Create(graph, runClip);
        AnimationClipPlayable idlePlayable =
            AnimationClipPlayable.Create(graph, idleClip);
        
        // ─── 5. 连接节点 ───
        // idlePlayable → mixer[0]
        // runPlayable  → mixer[1]
        graph.Connect(idlePlayable, 0, mixer, 0);
        graph.Connect(runPlayable, 0, mixer, 1);
        
        // ─── 6. 混合器 → 输出 → Animator ───
        output.SetSourcePlayable(mixer);
        
        // ─── 7. 启动 PlayableGraph ───
        graph.Play();
    }
    
    void Update()
    {
        // ─── 运行时控制混合权重 ───
        float speed = Input.GetAxis("Horizontal");
        
        // 根据速度调整混合
        // idle 权重 = 1 - speed
        mixer.SetInputWeight(0, Mathf.Max(0, 1 - Mathf.Abs(speed)));
        // run 权重 = speed
        mixer.SetInputWeight(1, Mathf.Abs(speed));
        
        // 同步时间（让动画正确播放）
        mixer.SetTime(Time.time);
    }
    
    void OnDestroy()
    {
        // ─── 8. 销毁 Graph ───
        // 很重要！不销毁会一直占用内存
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
```

### 1.3 PlayableGraph 节点类型

```csharp
/*
 Unity 内置的 Playable 节点类型：

 ┌─────────────────────────────────────────────────────┐
 │ AnimationClipPlayable                                │
 │ 播放一个 AnimationClip                               │
 │ 用法：最基础的节点，作为动画数据的源                    │
 │                                                      │
 │ AnimationMixerPlayable                                │
 │ 混合多个动画，按权重                                   │
 │ 用法：Idle → Walk → Run 的平滑过渡                    │
 │                                                      │
 │ AnimationLayerMixerPlayable                           │
 │ 分层混合（基础层 + 覆盖层）                            │
 │ 用法：上半身射击 + 下半身跑步                          │
 │                                                      │
 │ AnimationMotionXToDeltaPlayable                       │
 │ 将动画的移动量转为刚体速度                             │
 │ 用法：Root Motion 转换                                │
 │                                                      │
 │ AnimationScriptPlayable                               │
 │ 自定义 C# 处理逻辑                                    │
 │ 用法：程序化修改骨骼位置                                │
 └─────────────────────────────────────────────────────┘
*/
```

### 1.4 多轨道过场动画

```csharp
// ─── 过场动画：多个角色同时演出 ───
// 每个角色是一个轨道

public class CutsceneDirector : MonoBehaviour
{
    public Transform[] characters;       // 角色列表
    public AnimationClip[] characterClips; // 每个角色的动画
    
    private PlayableGraph cutsceneGraph;
    
    void Start()
    {
        cutsceneGraph = PlayableGraph.Create("CutsceneGraph");
        
        // 创建线性轨道混合器
        // Linear 表示所有输入同时播放，不像混合器那样合并
        var trackMixer = AnimationLayerMixerPlayable.Create(
            cutsceneGraph, characters.Length);
        
        // 为每个角色创建一个轨道
        for (int i = 0; i < characters.Length; i++)
        {
            // 每个角色需要独立的 Animator？
            // 不，PlayableGraph 可以控制多个 Animator
            
            var animator = characters[i].GetComponent<Animator>();
            
            // 每个角色单独的输出
            var output = AnimationPlayableOutput.Create(
                cutsceneGraph, $"Character_{i}", animator);
            
            // 创建角色的动画片段节点
            var clipPlayable = AnimationClipPlayable.Create(
                cutsceneGraph, characterClips[i]);
            
            // 连接
            output.SetSourcePlayable(clipPlayable);
        }
        
        // 设置时间线控制
        cutsceneGraph.SetTimeUpdateMode(
            DirectorUpdateMode.Manual);  // 手动控制时间
        
        cutsceneGraph.Play();
    }
    
    void Update()
    {
        // 手动控制时间（配合 UI 进度条）
        if (Input.GetKey(KeyCode.RightArrow))
        {
            cutsceneGraph.Evaluate(Time.deltaTime);
        }
    }
    
    void OnDestroy()
    {
        if (cutsceneGraph.IsValid())
            cutsceneGraph.Destroy();
    }
}
```

---

## 2. Animation Rigging——程序化控制骨骼

### 2.1 什么是 Animation Rigging？

```csharp
/*
 Animation Rigging 是 Unity 的官方程序化动画包。
 它允许你在不修改动画 Clip 的情况下，在动画之上叠加骨骼变换。

 安装：Window → Package Manager → Animation Rigging

 典型应用：
 - 角色的头部跟随目标（Look-At IK）
 - 手的瞄准方向（Aim IK）
 - 脚贴合地面（Two-Bone IK）
 - 手枪的后座力效果
 - 物理布料/头发模拟（轻量化）
*/
```

### 2.2 设置 IK（反向动力学）

```csharp
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKController : MonoBehaviour
{
    [Header("Rig References")]
    public Rig aimRig;           // Rig 组件（约束的容器）
    public MultiAimConstraint headAim;     // 头部朝向
    public TwoBoneIKConstraint handIK;     // 手部 IK
    
    [Header("Targets")]
    public Transform aimTarget;   // 瞄准目标
    public Transform handTarget;  // 手部目标
    
    void Start()
    {
        // Rig.weight 控制整个约束系统的整体权重
        // 0 = 完全使用原始动画，1 = 完全使用 IK
        
        // 开始时禁用 IK，需要时慢慢开启
        aimRig.weight = 0f;
    }
    
    void Update()
    {
        // ─── 头部跟随瞄准目标 ───
        // MultiAimConstraint 会让头部朝向目标
        
        // ─── 手部 IK（如：举枪瞄准） ───
        if (Input.GetButton("Fire2"))  // 右键瞄准
        {
            // 平滑开启 IK
            aimRig.weight = Mathf.Lerp(
                aimRig.weight, 1f, Time.deltaTime * 5f);
            
            // 设置手的目标位置（从摄像机前面的射线获取）
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, transform.position + Vector3.forward * 10);
            
            if (plane.Raycast(ray, out float distance))
            {
                aimTarget.position = ray.GetPoint(distance);
            }
        }
        else
        {
            // 平滑关闭 IK
            aimRig.weight = Mathf.Lerp(
                aimRig.weight, 0f, Time.deltaTime * 3f);
        }
    }
    
    // ─── 触发 IK 效果（如受击后仰） ───
    public void TriggerHitReaction(float intensity)
    {
        // 临时将 Rig weight 设为 1
        // 叠加一个受击动画效果
        StartCoroutine(HitReactionRoutine(intensity));
    }
    
    IEnumerator HitReactionRoutine(float intensity)
    {
        aimRig.weight = 1f;
        
        // 让 Rig 在一段时间后恢复
        yield return new WaitForSeconds(0.2f);
        
        float elapsed = 0;
        while (elapsed < 0.3f)
        {
            aimRig.weight = Mathf.Lerp(1f, 0f, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        aimRig.weight = 0f;
    }
}
```

### 2.3 自定义 Rig 约束

```csharp
// ─── 创建自定义 Rig 约束 ───
// 让武器始终指向鼠标方向

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

// 需要实现 IRigConstraint 接口
public class WeaponAimConstraint : MonoBehaviour, IRigConstraint
{
    public Transform weaponBone;     // 武器骨骼
    public Transform aimTarget;      // 瞄准目标
    private Quaternion initialRotation;
    
    public RigWeight weight { get; set; }
    public IAnimationJob CreateJob(Animator animator)
    {
        return new WeaponAimJob
        {
            weaponHandle = animator.BindStreamTransform(weaponBone),
            targetPosition = aimTarget.position,
            weight = weight
        };
    }
    
    void Start()
    {
        initialRotation = weaponBone.localRotation;
    }
    
    // 作业（Job）结构体——在动画线程中执行
    struct WeaponAimJob : IAnimationJob
    {
        public AnimationStreamHandle weaponHandle;
        public Vector3 targetPosition;
        public RigWeight weight;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            // 获取当前骨骼变换
            TransformStreamHandle handle = weaponHandle;
            
            // 计算朝向
            Vector3 direction = targetPosition -
                handle.GetPosition(stream);
            
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                
                // 根据权重混合
                Quaternion currentRot = handle.GetRotation(stream);
                Quaternion blended = Quaternion.Slerp(
                    currentRot, targetRot, weight.value);
                
                handle.SetRotation(stream, blended);
            }
        }
        
        public void ProcessRootMotion(AnimationStream stream) { }
    }
}
```

---

## 3. 程序化动画

### 3.1 使用 Playables 做程序化动画

```csharp
// ─── 完全用代码驱动骨骼动画 ───
// 没有 Animation Clip，全靠计算

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animation;

public class ProceduralAnimation : MonoBehaviour
{
    private PlayableGraph graph;
    
    void Start()
    {
        graph = PlayableGraph.Create("ProceduralGraph");
        
        // 创建自定义动画 Playable
        var scriptPlayable = ScriptPlayable<ProceduralBehaviour>.Create(graph);
        
        // 设置输出
        var output = AnimationPlayableOutput.Create(
            graph, "ProceduralOutput", GetComponent<Animator>());
        output.SetSourcePlayable(scriptPlayable);
        
        graph.Play();
    }
    
    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}

// ─── 自定义动画行为 ───
public class ProceduralBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable,
        FrameData info, object playerData)
    {
        Animator animator = playerData as Animator;
        if (animator == null) return;
        
        float time = (float)playable.GetTime();
        
        // ─── 程序化生成动画数据 ───
        
        // 1. 骨骼变换——直接在 C# 中计算
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        
        // 2. 呼吸效果——骨骼轻微起伏
        float breathe = Mathf.Sin(time * 2f) * 0.01f;
        hips.localPosition += Vector3.up * breathe;
        
        // 3. 尾巴摆动（如果有尾巴骨骼）
        Transform tail = animator.GetBoneTransform(HumanBodyBones.LastBone);
        if (tail != null)
        {
            float tailSwing = Mathf.Sin(time * 3f) * 15f;
            tail.localRotation = Quaternion.Euler(0, 0, tailSwing);
        }
        
        // 4. 头发物理模拟（简化版）
        // 可以在这里实现弹簧质点系统
    }
}
```

### 3.2 Animation C# Jobs——多线程动画

```csharp
// ─── 使用 IJobParallelFor 批量更新动画 ───
// 适合大量相似单位的动画更新（如 RTS 游戏）

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct UnitAnimationJob : IJobParallelFor
{
    // 输入数据
    [ReadOnly] public NativeArray<Vector3> targetPositions;
    [ReadOnly] public NativeArray<float> speeds;
    
    // 输出数据（骨骼偏移）
    public NativeArray<float> boneOffsets;
    
    // 时间参数
    public float deltaTime;
    public float globalTime;
    
    public void Execute(int index)
    {
        // 每个单位独立计算动画状态
        Vector3 target = targetPositions[index];
        float speed = speeds[index];
        
        // 程序化生成散步动画
        float walkCycle = globalTime * speed;
        float verticalOffset = Mathf.Sin(walkCycle * Mathf.PI * 2) * 0.1f;
        float horizontalOffset = Mathf.Cos(walkCycle * Mathf.PI * 2) * 0.05f;
        
        // 写出结果
        boneOffsets[index * 3 + 0] = verticalOffset;    // 上下
        boneOffsets[index * 3 + 1] = horizontalOffset;  // 左右
        boneOffsets[index * 3 + 2] = 0f;                // 前后
    }
}

// ─── 调度 Job 的 MonoBehaviour ───

using Unity.Jobs;

public class CrowdAnimationManager : MonoBehaviour
{
    public int unitCount = 100;
    private NativeArray<Vector3> targetPositions;
    private NativeArray<float> speeds;
    private NativeArray<float> boneOffsets;
    private JobHandle jobHandle;
    
    void Start()
    {
        targetPositions = new NativeArray<Vector3>(
            unitCount, Allocator.Persistent);
        speeds = new NativeArray<float>(
            unitCount, Allocator.Persistent);
        boneOffsets = new NativeArray<float>(
            unitCount * 3, Allocator.Persistent);
    }
    
    void Update()
    {
        // 每帧调度 Job
        UnitAnimationJob job = new UnitAnimationJob
        {
            targetPositions = targetPositions,
            speeds = speeds,
            boneOffsets = boneOffsets,
            deltaTime = Time.deltaTime,
            globalTime = Time.time
        };
        
        // 100 个单位在多个 CPU 核心上并行计算
        jobHandle = job.Schedule(unitCount, 32);  // 32 个一组
        
        // 确保下一帧开始时 Job 完成
        JobHandle.ScheduleBatchedJobs();
    }
    
    void LateUpdate()
    {
        // 等待 Job 完成
        jobHandle.Complete();
        
        // 使用计算结果更新单位位置
        for (int i = 0; i < unitCount; i++)
        {
            // 应用 boneOffsets 到第 i 个单位
        }
    }
    
    void OnDestroy()
    {
        // 释放 Native 内存
        targetPositions.Dispose();
        speeds.Dispose();
        boneOffsets.Dispose();
    }
}
```

---

## 4. Blend Tree 高级技巧

### 4.1 1D、2D、Freeform 的区别

```csharp
/*
 ┌─────────────────────────────────────────────────────┐
 │ Blend Type 选择指南                                   │
 │                                                      │
 │ 1D Simple：一个参数，线性混合                           │
 │   输入：Speed (0~1)                                   │
 │   节点：Idle → Walk → Run → Sprint                   │
 │   适用：水平移动速度                                   │
 │                                                      │
 │ 2D Simple：两个参数，方向性混合                         │
 │   输入：Forward (0~1), Turn (-1~1)                    │
 │   适用：基本方向移动                                   │
 │                                                      │
 │ 2D Freeform Directional：方向优先                     │
 │   输入：任何两个参数                                   │
 │   节点位置由角度和幅度决定                              │
 │   适用：8 方向移动                                     │
 │                                                      │
 │ 2D Freeform Cartesian：坐标优先                       │
 │   输入：任何两个参数                                   │
 │   节点位置由 x/y 坐标决定                              │
 │   适用：需要精确控制混合权重的场景                       │
 └─────────────────────────────────────────────────────┘
*/
```

### 4.2 Freeform Cartesian 精确混合

```csharp
// ─── 代码动态配置 Blend Tree ───

using UnityEditor;
using UnityEngine;

public class DynamicBlendTree : MonoBehaviour
{
    private Animator animator;
    
    // 混合参数
    private float moveX;   // -1 ~ 1
    private float moveZ;   // -1 ~ 1
    
    void Start()
    {
        animator = GetComponent<Animator>();
        ConfigureBlendTree();
    }
    
    void ConfigureBlendTree()
    {
        // 通常在 Animator Controller 中配置
        // 但也可以运行时动态修改 Blend Tree
        // 这里展示的是 Animator 参数的设置
        
        // 参数命名规范：
        // Blend 参数：MoveX, MoveZ（Float）
        // 混合树使用这俩参数做 2D Freeform Cartesian
    }
    
    void Update()
    {
        // 获取输入
        moveX = Input.GetAxis("Horizontal");
        moveZ = Input.GetAxis("Vertical");
        
        // 限制长度 ≤ 1（防止斜向走更快）
        Vector2 move = new Vector2(moveX, moveZ);
        if (move.magnitude > 1f)
        {
            move.Normalize();
        }
        
        // 设置 Animator 参数
        animator.SetFloat("MoveX", move.x, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveZ", move.y, 0.1f, Time.deltaTime);
        // 第三个参数 damping：平滑过渡时间
    }
}
```

---

## 5. StateMachineBehaviour——状态机脚本

### 5.1 在状态进入/退出时执行逻辑

```csharp
// ─── StateMachineBehaviour ───
// 附加在 Animator Controller 的状态节点上
// 在状态转换时调用

using UnityEngine;

public class AttackStateBehaviour : StateMachineBehaviour
{
    [Header("State Config")]
    public bool enableRootMotion = true;
    public float damageWindowStart = 0.3f;   // 伤害判定开始
    public float damageWindowEnd = 0.7f;     // 伤害判定结束
    public float comboTiming = 0.5f;         // 连击窗口
    
    // 状态变量（在 OnStateEnter 初始化）
    private bool hasDealtDamage = false;
    private float stateEnterTime;
    
    // OnStateEnter：进入状态时调用
    override public void OnStateEnter(Animator animator,
        AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 初始化
        hasDealtDamage = false;
        stateEnterTime = Time.time;
        
        // 启用 Root Motion（攻击时的位移）
        animator.applyRootMotion = enableRootMotion;
        
        Debug.Log("进入攻击状态！");
    }
    
    // OnStateUpdate：状态播放时每帧调用
    override public void OnStateUpdate(Animator animator,
        AnimatorStateInfo stateInfo, int layerIndex)
    {
        float normalizedTime = stateInfo.normalizedTime;
        // normalizedTime：0~1（循环状态下 >1）
        
        // 伤害判定窗口
        if (!hasDealtDamage &&
            normalizedTime >= damageWindowStart &&
            normalizedTime <= damageWindowEnd)
        {
            // 执行伤害判定
            DealDamage(animator);
            hasDealtDamage = true;
        }
        
        // 连击窗口检测
        if (normalizedTime < comboTiming &&
            Input.GetButtonDown("Fire1"))
        {
            // 触发连击
            animator.SetTrigger("ComboAttack");
        }
    }
    
    // OnStateExit：离开状态时调用
    override public void OnStateExit(Animator animator,
        AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 恢复
        animator.applyRootMotion = false;
        
        // 重置触发器（防止残留）
        animator.ResetTrigger("ComboAttack");
    }
    
    // OnStateMove：处理 Root Motion 时调用
    override public void OnStateMove(Animator animator,
        AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 如果开启了 Root Motion，在这里处理位移
        // animator.SetLookAtPosition(target.position);
    }
    
    void DealDamage(Animator animator)
    {
        // 获取攻击者组件
        // 使用 animator.GetComponent<CombatSystem>().DealDamage();
        Debug.Log("造成伤害！");
    }
}
```

### 5.2 Sub-StateMachine 分层管理

```csharp
/*
 ─── 子状态机（Sub-StateMachine）───
 动画状态机可以嵌套，避免状态爆炸。

 示例分层：
 
 Character (Layer 0)
 ├── Movement (Sub-StateMachine)
 │   ├── Idle
 │   ├── Walk
 │   ├── Run
 │   └── Sprint
 ├── Combat (Sub-StateMachine)
 │   ├── SwordAttack
 │   │   ├── Attack1 → Attack2 → Attack3
 │   │   └── ComboEnd
 │   └── BowAttack
 │       ├── Draw → Aim → Shoot
 │       └── Cancel
 └── Damage (Sub-StateMachine)
     ├── HitReact_Light
     ├── HitReact_Heavy
     ├── Stagger
     └── KnockDown → GetUp

 子状态机的优势：
 1. 内部状态切换不影响外部
 2. 子状态机可以复用（如不同角色共享 Movement）
 3. 每个子状态机可以用不同的参数
*/
```

---

## 6. Animator 参数优化

### 6.1 参数哈希与缓存

```csharp
// ─── Animator 参数访问的性能陷阱 ───

public class AnimatorOptimization : MonoBehaviour
{
    private Animator animator;
    
    // ❌ 坏：每帧用字符串查找
    void BadUpdate()
    {
        // SetFloat/SetBool/SetTrigger 内部是字符串哈希
        // 每帧都做字符串→哈希的转换（有 GC！）
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", grounded);
        animator.SetTrigger("Jump");
    }
    
    // ✅ 好：缓存哈希值
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    
    void GoodUpdate()
    {
        // 使用哈希（int）→ 零分配，更快
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(GroundedHash, grounded);
        animator.SetTrigger(JumpHash);
    }
}
```

### 6.2 减少不必要的 Animator 更新

```csharp
// ─── Animator 的 Culling Mode（剔除模式） ───

/*
 Animator 的 Update Mode：
 
 1. Always Animate（默认）
    不管是否可见都更新动画
    资源浪费：屏幕外的角色也在更新
    
 2. Cull Update Transform
    不可见时只更新位置（不更新动画）
    节省动画混合的计算
    
 3. Cull Completely（推荐）
    不可见时完全不更新
    最佳性能
*/

// 代码设置（运行时动态调整）
void OptimizeAnimator()
{
    Animator anim = GetComponent<Animator>();
    
    // 根据距离调整更新模式
    float dist = Vector3.Distance(
        Camera.main.transform.position, transform.position);
    
    if (dist > 50f)
    {
        // 远处完全不更新
        anim.cullingMode = AnimatorCullingMode.CullCompletely;
    }
    else if (dist > 20f)
    {
        // 中等距离只更新位置
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransform;
    }
    else
    {
        // 近处完全更新
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }
}
```

---

## 7. Animation Burst 编译

```csharp
// ─── 用 Burst 编译加速动画 Job ───
// 将动画计算编译为高效的 SIMD 机器码

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]  // ← 关键属性！
public struct BurstAnimationJob : IJobParallelFor
{
    public NativeArray<float3> bonePositions;
    public NativeArray<quaternion> boneRotations;
    public float time;
    
    public void Execute(int index)
    {
        // 使用 float3 和 quaternion（Unity.Mathematics）
        // 这些类型是 Burst 友好的
        
        float angle = time * (index + 1) * 0.5f;
        
        // 计算摆动
        float sin = math.sin(angle);
        float cos = math.cos(angle);
        
        // 更新骨骼位置
        bonePositions[index] = new float3(
            sin * 0.1f,
            cos * 0.1f + 0.5f,
            0f
        );
        
        // 更新骨骼旋转
        boneRotations[index] = quaternion.Euler(
            0, 0, sin * 0.3f);
    }
}

/*
 Burst 编译的效果：
 - C# 普通 Job：IL 解释执行
 - Burst 编译后：SIMD AVX/NEON 指令
 - 性能提升：5~20 倍
 - 适用场景：大量骨骼的群体动画
*/
```

---

## C++/Raylib 对照总结

| 概念 | C++ / Raylib | Unity 进阶动画 |
|------|-------------|---------------|
| 动画图 | 手动更新循环 | PlayableGraph 多节点连接 |
| 程序化动画 | 直接修改骨骼矩阵 | PlayableBehaviour + C# Jobs |
| IK 解算 | 自己实现 CCD/FABRIK | Animation Rigging 约束系统 |
| 多线程 | std::thread 手动管理 | IJobParallelFor 自动调度 |
| 状态分离 | if-else 嵌套 | Sub-StateMachine 分层 |
| 运动混合 | Lerp 骨骼变换 | Blend Tree 自动加权重 |
| — | 无 | Burst Compiler SIMD 加速 |
| — | 无 | Root Motion 根骨骼驱动 |
| — | 无 | StateMachineBehaviour 状态脚本 |

## 停靠点

> Playable API 是动画系统的底层——直接操作 PlayableGraph，适合过场动画和程序化动画。
> Animation Rigging 在动画之上叠加骨骼控制——LookAt IK、TwoBone IK 等开箱即用。
> Animation C# Jobs 多线程更新动画——适合大量单位的批量动画计算。
> Blend Tree 支持 1D/2D Simple/2D Freeform 三种模式——Freeform Cartesian 最灵活。
> StateMachineBehaviour 在状态进入/退出时执行自定义逻辑——替代 Update 中的状态判断。
> Animator 参数用 StringToHash 缓存，CullingMode 优化远距离动画。
> Burst 编译将动画 Job 加速 5~20 倍——SIMD 自动向量化。
