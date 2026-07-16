# Day 14：音频与特效 — Mixer 深度控制与 GPU 粒子系统

## 0. 为什么还需要深入音频和特效？

基础篇让你能用 AudioSource 播放声音，用 Particle System 做特效。但真实项目需要的远不止这些：

```
基础篇解决的问题：
- 播放 BGM 和 SFX
- 基本的 3D 音效衰减
- 简单的粒子爆炸效果

真实项目需要面对的挑战：
- 复杂的音频混合管理（按场景切换混音配置）
- 空间音频（HRTF、Ambisonic 全景声）
- 随机音频容器（让音效听起来不那么重复）
- 几十万粒子的 GPU 特效（VFX Graph）
- 粒子系统之间的联动（Sub-Emitters）
- 自定义粒子外观的 Shader
- 音频性能预算控制
```

---

## 1. AudioMixer 深度控制

### 1.1 混音器架构与信号流

```csharp
/*
 ─── AudioMixer 的内部信号流 ───

 AudioSource → AudioMixer Group → AudioMixer Group → ... → Audio Listener
                                  效果器链 → 效果器链 →
 
 信号处理流水线：
 Input Signal
    │
    ▼
 ├─ Attenuation（衰减）—— 3D 空间音效的体积控制
 │   ├─ Distance:    根据距离调整音量
 │   └─ Cone:        根据声源方向调整音量（模拟喇叭）
    │
    ▼
 ├─ Effects（效果器链）
 │   ├─ Lowpass      低通滤波（模拟水下、隔墙声）
 │   ├─ Highpass     高通滤波（去除低频噪声）
 │   ├─ Echo         回声效果
 │   ├─ Reverb       混响（模拟不同空间的反射）
 │   ├─ Chorus       合唱效果（加厚声音）
 │   └─ Pitch Shift  音调偏移
    │
    ▼
 ├─ Group Volume    组的总体音量
    │
    ▼
 Output Signal → 父 Group 或 AudioListener
*/
```

### 1.2 动态 Mixer 控制

```csharp
using UnityEngine.Audio;
using System.Collections;

public class AdvancedAudioMixer : MonoBehaviour
{
    [Header("Mixer References")]
    public AudioMixer mainMixer;
    public AudioMixerSnapshot normalSnapshot;
    public AudioMixerSnapshot lowPassSnapshot;
    public AudioMixerSnapshot mutedSnapshot;
    
    // ─── Snapshot 过渡 ───
    // Snapshot 是 Mixer 的"预设"——保存所有参数和效果的配置
    
    // 进入战斗时：切到"战斗"配置
    public void EnterCombat()
    {
        // 过渡到"低通"快照（模拟进入战斗的听觉聚焦）
        // 第一个参数：过渡时间（秒）
        lowPassSnapshot.TransitionTo(1.5f);
        
        // 本质上会 Lerp 所有参数从当前值到目标值
    }
    
    // 暂停游戏时：切到"暂停"配置
    public void OnPauseMenu()
    {
        // 将 BGM 音量降到 -30dB，开启低通效果
        mutedSnapshot.TransitionTo(0.5f);
    }
    
    // ─── 通过暴露参数控制 ───
    // 需要先在 Mixer 的 Exposed Parameters 中勾选
    
    public void SetMusicVolume(float volume)
    {
        // volume 范围 0~1
        // Mixer 内部用 dB，需要转换
        
        // dB = 20 * log10(linear)
        // linear = 0   → dB = -∞（静音）
        // linear = 0.5 → dB = -6
        // linear = 1.0 → dB = 0
        
        float dB = volume > 0.001f
            ? Mathf.Log10(volume) * 20f
            : -80f;  // 接近静音
        
        mainMixer.SetFloat("MusicVolume", dB);
    }
    
    // ─── 渐入渐出 ───
    public IEnumerator FadeMusic(float targetVolume, float duration)
    {
        mainMixer.GetFloat("MusicVolume", out float currentDB);
        
        float startDB = currentDB;
        float targetDB = Mathf.Log10(Mathf.Max(targetVolume, 0.001f)) * 20f;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 指数渐变（听觉上更自然）
            // dB 感知是对数的，所以线性插值 dB 值
            float db = Mathf.Lerp(startDB, targetDB, t);
            mainMixer.SetFloat("MusicVolume", db);
            
            yield return null;
        }
        
        mainMixer.SetFloat("MusicVolume", targetDB);
    }
}
```

### 1.3 效果器参数编程控制

```csharp
// ─── 运行时控制 AudioMixer 效果器参数 ───

public class ReverbController : MonoBehaviour
{
    public AudioMixer mixer;
    public AnimationCurve reverbDecay;  // 衰减曲线
    
    void Start()
    {
        // 效果器参数也需要在 Exposed Parameters 中暴露
        
        // 设置混响衰减时间
        // reverbDecay 曲线 × 时间 → 控制混响大小
    }
    
    void Update()
    {
        // 进入大房间时 → 加大混响
        // 进入狭窄通道 → 减小混响
        // 进入水下 → 低通 + 特殊混响
        
        // 根据当前环境设置混响参数
        if (IsInLargeRoom())
        {
            mixer.SetFloat("ReverbDecayTime", 2.5f);  // 大房间长回响
            mixer.SetFloat("ReverbWetLevel", -6f);     // 湿声比例
        }
        else if (IsInTunnel())
        {
            mixer.SetFloat("ReverbDecayTime", 0.8f);   // 隧道短回响
            mixer.SetFloat("ReverbWetLevel", -3f);     // 更多湿声
        }
    }
    
    bool IsInLargeRoom()
    {
        // 通过 Physics.OverlapSphere 检测周围墙壁
        // 计算空间体积
        return false;  // 示例
    }
}
```

---

## 2. 空间音频

### 2.1 3D 音效的数学原理

```csharp
/*
 ─── 人耳定位声音的原理 ───

 1. 音量差（Interaural Level Difference, ILD）
    声源在右边 → 右耳听到的更响
    高频声音的头部遮挡效果更明显

 2. 时间差（Interaural Time Difference, ITD）
    声源在右边 → 右耳先听到
    差距约 0~0.6ms

 3. 频谱变化（Head-Related Transfer Function, HRTF）
    头部和耳廓的共振改变了频谱
    不同方向的声音有不同的频率特征
    这是最精确的定位方法

 4. 距离衰减
    近：声压大 + 高频保留完整
    远：声压小 + 高频被空气吸收

 ─── Unity 的实现 ───
      
 简单 3D 音效（默认）：
 - 只使用 ILD（音量差）
 - 距离衰减曲线
 - 在移动设备上性能好

 Audio Spatializer SDK（高级）：
 - 使用 HRTF 算法
 - 需要安装特定平台的空间化插件
 - 需要 .NET 插件（IUnitySpatializer 接口）
*/
```

### 2.2 AudioSpatializer 配置

```csharp
// ─── 高级空间音频配置 ───
// 需要安装 Audio Spatializer SDK 或使用平台原生实现

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SpatialAudioConfig : MonoBehaviour
{
    private AudioSource source;
    
    void Start()
    {
        source = GetComponent<AudioSource>();
        
        // ─── 基础 3D 设置 ───
        source.spatialBlend = 1.0f;     // 纯 3D 音效
        source.spatialize = true;       // 开启空间化（使用平台的 HRTF）
        source.spatializePostEffects = false;  // 是否在效果器之后空间化
        
        // ─── 衰减设置 ───
        source.minDistance = 2f;        // 最小距离
        source.maxDistance = 50f;       // 最大距离
        
        // 自定义衰减曲线
        AnimationCurve customRolloff = new AnimationCurve();
        customRolloff.AddKey(0f, 1f);        // 0m: 音量 100%
        customRolloff.AddKey(2f, 1f);        // 2m: 音量 100%
        customRolloff.AddKey(10f, 0.5f);     // 10m: 音量 50%
        customRolloff.AddKey(50f, 0f);       // 50m: 音量 0%
        source.SetCustomCurve(
            AudioSourceCurveType.CustomRolloff, customRolloff);
        
        // ─── 多普勒效应 ───
        source.dopplerLevel = 1.0f;     // 多普勒效应强度
        // 声源快速接近你 → 音调升高
        // 声源快速远离你 → 音调降低
        // 0 = 关闭多普勒
    }
}
```

### 2.3 Ambisonic 环境音

```csharp
// ─── Ambisonic 音频 ───
// 全景声（Ambisonics）是 360° 环绕声的编码格式
// 适合 VR/AR 和开放世界的环境音

/*
 Ambisonic 对比立体声：
 立体声（Stereo）：2 个声道（左/右）
 环绕声（5.1）：6 个声道
 Surround（7.1）：8 个声道
 Ambisonic（1阶）：4 个声道（W, X, Y, Z）
 Ambisonic（2阶）：9 个声道
 Ambisonic（3阶）：16 个声道

 Ambisonic 的优势：
 - 解码与扬声器配置无关（一副耳机/7.1 系统都可以）
 - 音源方向和听者旋转时的表现很自然
 - 记录的是"整个球面的声场"而非若干声道
*/

// 使用 Ambisonic（AudioClip 需要是 Ambisonic 格式）
void ConfigureAmbisonicSource()
{
    AudioSource ambiSource = GetComponent<AudioSource>();
    
    // 标记为 Ambisonic
    ambiSource.clip = ambisonicClip;  // 四声道 WXYZ 编码
    
    // Ambisonic 需要特殊的解码器
    // 通常使用平台的 Audio Spatializer 解码
}

/*
 录制 Ambisonic：
 需要 Ambisonic 麦克风（如 Zoom H3-VR）
 或使用工具将 5.1/7.1 编码为 Ambisonic
*/
```

---

## 3. AudioRandomContainer

### 3.1 解决"重复感"

```csharp
// ─── 为什么需要随机音频 ───

/*
 问题：
 同一个枪声播放 10 次 → 玩家耳朵会识别出"哦，一样的"
 这种"重复感"破坏了真实感

 解决方案：
 1. 变体（Variants）：录多个版本随机播放
 2. 音调随机化：每次播放微调 pitch
 3. AudioRandomContainer：Unity 2022+ 的正式解决方案
*/

// ─── 传统随机播放方法 ───

public class BasicRandomAudio : MonoBehaviour
{
    public AudioClip[] shootVariants;  // 10 种不同的枪声
    
    public void PlayRandomShoot()
    {
        AudioSource source = GetComponent<AudioSource>();
        
        // 随机选一个
        int index = Random.Range(0, shootVariants.Length);
        source.PlayOneShot(shootVariants[index]);
        
        // 随机微调音调（每次听起来略有不同）
        source.pitch = Random.Range(0.95f, 1.05f);
    }
}
```

### 3.2 AudioRandomContainer 使用

```csharp
// ─── AudioRandomContainer（Unity 2022.2+）───
// 包：Audio Random Container（Package Manager 安装）

using UnityEngine;
using UnityEngine.Audio;

public class RandomContainerDemo : MonoBehaviour
{
    // 在 Inspector 中创建 AudioRandomContainer 资源
    // 设置多个变体和随机规则
    public AudioRandomContainer footstepContainer;
    public AudioRandomContainer weaponContainer;
    
    private AudioSource source;
    
    void Start()
    {
        source = gameObject.AddComponent<AudioSource>();
    }
    
    // ─── 播放脚步声 ───
    // AudioRandomContainer 自动处理：
    // - 随机选择变体
    // - 随机音调偏移
    // - 保证不连续重复（Non-Repeating）
    public void PlayFootstep()
    {
        // 直接从容器播放
        source.PlayOneShotFromContainer(footstepContainer);
        
        // 也可以获取 AudioClip 手动播放
        // AudioClip clip = footstepContainer.GetRandomClip();
        // source.PlayOneShot(clip);
    }
    
    // ─── AudioRandomContainer 配置项 ───
    /*
     在 Inspector 中可以看到：
     
     ┌─────────────────────────────────────────┐
     │ AudioRandomContainer                    │
     ├─────────────────────────────────────────┤
     │ Clips:                                  │
     │   ├─ footstep_concrete_01.wav           │
     │   ├─ footstep_concrete_02.wav           │
     │   ├─ footstep_concrete_03.wav           │
     │   └─ footstep_concrete_04.wav           │
     │                                         │
     │ Randomization:                          │
     │   Pitch Range:     0.9 ~ 1.1            │
     │   Volume Range:    0.95 ~ 1.0           │
     │   Delay Range:     0 ~ 0.05s            │
     │                                         │
     │ Selection Mode:                         │
     │   Random (完全随机)                      │
     │   Non-Repeating (不重复上次选中的)        │
     │   Shuffle (洗牌模式：播完一轮前不重复)     │
     └─────────────────────────────────────────┘
    */
}
```

---

## 4. VFX Graph——GPU 粒子系统

### 4.1 VFX Graph 架构

```csharp
/*
 VFX Graph 与 Particle System 的核心区别：

 ┌─────────────────────────────────────────────────────┐
 │ Particle System（CPU 粒子）                          │
 │                                                      │
 │ - 每个粒子由 CPU 更新                                  │
 │ - 适合 100~1000 个粒子                                │
 │ - 组件化 Inspector 配置                                │
 │ - 适合：枪火、小爆炸、拖尾                              │
 │ - CPU 负担：1000 个粒子 × 每帧更新 = 1000 次计算       │
 │                                                      │
 │ VFX Graph（GPU 粒子）                                 │
 │                                                      │
 │ - 每个粒子由 GPU 的 Compute Shader 更新               │
 │ - 适合 10000~1000000 个粒子                           │
 │ - 可视化节点图编程                                     │
 │ - 适合：大范围特效、银河、魔法阵、暴风雪                  │
 │ - GPU 负担：100000 个粒子 × 极轻的着色器计算            │
 └─────────────────────────────────────────────────────┘
*/
```

### 4.2 VFX Graph 基本工作流

```csharp
// ─── VFX Graph 的 C# 控制 ───

using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

public class VFXController : MonoBehaviour
{
    [Header("VFX References")]
    public VisualEffect fireVFX;
    public VisualEffect explosionVFX;
    public VisualEffect magicCircleVFX;
    
    void Start()
    {
        // ─── 公开属性（Exposed Properties）───
        // VFX Graph 中可以标记某些参数为 Exposed
        // 然后在 C# 中通过名称控制
        
        // 设置浮点参数
        fireVFX.SetFloat("SpawnRate", 50f);
        
        // 设置向量参数
        magicCircleVFX.SetVector3("CenterPosition", transform.position);
        
        // 设置颜色参数
        magicCircleVFX.SetVector4("GlowColor",
            new Vector4(1f, 0.5f, 0f, 1f));  // RGBA
        
        // 设置纹理参数
        // fireVFX.SetTexture("NoiseTexture", noiseTexture);
        
        // ─── 播放控制 ───
        fireVFX.Play();        // 开始播放
        // fireVFX.Stop();     // 停止播放
        // fireVFX.Reinit();   // 重新初始化
    }
    
    // ─── 在指定位置播放特效 ───
    public void PlayExplosionAt(Vector3 position, float scale)
    {
        // VFX Graph 也可以像 Prefab 一样实例化
        VisualEffect vfx = Instantiate(explosionVFX, position, Quaternion.identity);
        
        vfx.SetFloat("Scale", scale);
        vfx.Play();
        
        // 播放完后自动销毁
        Destroy(vfx.gameObject, 3f);
    }
    
    // ─── 事件（Event）控制 ───
    public void TriggerEvent()
    {
        // VFX Graph 中可以定义自定义事件
        // 在图中对应"Event"节点
        fireVFX.SendEvent("OnHitTarget");
        
        // 例如：火焰遇到敌人时触发一次爆炸子事件
    }
}
```

### 4.3 VFX Graph 常用节点模式

```csharp
/*
 ─── VFX Graph 常用配置模式 ───

 模式 1：火焰喷射
 ┌────────────────────────────────────┐
 │ Spawn (Rate=100/秒)                │
 │   → Initialize (Position=Cone,     │
 │       Lifetime=2s, Size=0.1)       │
 │   → Update (Velocity=向上+噪声,    │
 │       Size=随时间增大)             │
 │   → Output (Billboard, Additive,   │
 │       Gradient=黄→红→透明)         │
 └────────────────────────────────────┘

 模式 2：爆炸冲击波
 ┌────────────────────────────────────┐
 │ Spawn (Burst=1, 500个)             │
 │   → Initialize (Position=Sphere,   │
 │       Lifetime=0.5s, Size=0.05)    │
 │   → Update (Velocity=向外扩散,     │
 │       Size=Linear 缩小)            │
 │   → Output (Mesh=Sphere, Unlit)    │
 └────────────────────────────────────┘

 模式 3：粒子光环
 ┌────────────────────────────────────┐
 │ Spawn (Burst=1, 5000个)            │
 │   → Initialize (Position=Circle边缘,│
 │       Lifetime=∞)                  │
 │   → Update (Orbit=绕Y轴旋转,       │
 │       Speed=uniform)               │
 │   → Output (Billboard, Additive,   │
 │       Color=蓝色渐变, Size=2)       │
 └────────────────────────────────────┘
*/
```

### 4.4 VFX Graph 性能调优

```csharp
/*
 ─── VFX Graph 性能优化指南 ───

 1. 粒子上限（Capacity）
    设一个合理的上限，不是越高越好
    火焰特效：500 粒子就够了
    星际背景：100000 粒子也可能可以
    
 2. 渲染模式
    - Billboard（公告牌）：最轻量，总是面向摄像机
    - Mesh：需要渲染 3D 模型（更重）
    - 用简单几何体代替复杂 Mesh
    
 3. 混合模式
    - Additive（叠加）：不需要深度写入，最快
    - Alpha Blended（半透明）：需要深度，慢一些
    - Opaque（不透明）：最快但遮挡后面的粒子
    
 4. Compute Shader vs. Vertex Shader
    - 默认用 Compute Shader（能力强、灵活）
    - 纯粒子运动可以用 Vertex Shader（性能更好）
    
 5. LOD（VFX Graph 也支持！）
    远处降低粒子数量或质量
*/
```

---

## 5. Particle System 高级技巧

### 5.1 Sub-Emitters 子发射器

```csharp
// ─── Sub-Emitters 实现粒子链式反应 ───

/*
 Sub-Emitters 允许一个粒子系统在特定条件下
 "发射"另一个粒子系统

 触发条件：
 - Birth：粒子诞生时（爆炸粒子 → 火花子粒子）
 - Collision：粒子碰撞时（雨滴 → 水花）
 - Death：粒子死亡时（火焰粒子 → 烟雾粒子）
 - Manual：代码手动触发

 典型链式反应：
 爆炸（主粒子，Burst=1）
   ├── Birth → 冲击波（环形扩散）
   ├── Birth → 碎片（随机飞散）
   ├── Death → 烟雾（上升消散）
   └── Collision → 火花（接触地面时）
*/

public class SubEmitterExample : MonoBehaviour
{
    public ParticleSystem explosionMain;
    
    void Start()
    {
        // 配置 Sub-Emitters
        var main = explosionMain.main;
        var subEmitters = explosionMain.subEmitters;
        
        // 在 Inspector 中配置比代码更方便
        // 但在代码中可以动态调整
        
        Debug.Log($"Sub-Emitters count: {subEmitters.subEmittersCount}");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 触发爆炸
            explosionMain.Play();
        }
    }
}
```

### 5.2 Particle System 脚本高级控制

```csharp
// ─── 每粒子控制（ParticleSystem.GetParticles） ───

public class AdvancedParticleControl : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        
        // 预分配粒子数组（避免每帧 GC）
        particles = new ParticleSystem.Particle[
            ps.main.maxParticles];
    }
    
    void Update()
    {
        // ─── 获取当前所有粒子 ───
        int count = ps.GetParticles(particles);
        
        // ─── 遍历每个粒子，应用自定义逻辑 ───
        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle p = particles[i];
            
            // 1. 让粒子朝向移动方向
            if (p.velocity.sqrMagnitude > 0.01f)
            {
                p.rotation3D = Quaternion.LookRotation(
                    p.velocity).eulerAngles;
            }
            
            // 2. 对粒子施加引力
            Vector3 attractor = Vector3.zero;  // 世界中心
            Vector3 direction = attractor - p.position;
            p.velocity += direction.normalized *
                Time.deltaTime * 5f;
            
            // 3. 颜色根据速度变化
            float speed = p.velocity.magnitude;
            p.startColor = Color.Lerp(
                Color.blue, Color.red, speed / 10f);
            
            // 写入修改
            particles[i] = p;
        }
        
        // ─── 将修改后的粒子写回系统 ───
        ps.SetParticles(particles, count);
    }
}
```

### 5.3 Custom Particle Shader

```csharp
// ─── Shader Graph 中创建自定义粒子 Shader ───
// 让粒子有独特的视觉风格

/*
 Shader Graph 节点示例：溶解消失效果

 ┌─────────────────────────────────────────┐
 │ Vertex Color (R)                         │
 │   → Multiply with Time                   │
 │   → Step with Noise Texture              │
 │   → Clip (Alpha Test)                    │
 │                                          │
 │ Color 输出：                             │
 │   Base Color = Gradient (根据 lifetime)   │
 │   Alpha = Noise Value                    │
 └─────────────────────────────────────────┘

 关键 Shader Graph 属性：
 - Alpha Clipping（透明度裁剪）
 - Two Sided（双面渲染）
 - Custom Vertex Streams（自定义顶点数据流）
*/

// ─── C# 设置自定义粒子数据流 ───

void ConfigureParticleShaderData()
{
    var ps = GetComponent<ParticleSystem>();
    var renderer = ps.GetComponent<ParticleSystemRenderer>();
    
    // 启用自定义顶点流
    // 允许向 Shader 传递额外数据
    var streams = renderer.activeVertexStreams;
    
    // 常见数据流：
    // - Position: 位置
    // - Velocity: 速度（用于运动模糊）
    // - Color:  颜色
    // - UV:    纹理坐标
    // - Normal: 法线
    // - Tangent: 切线
    // - Custom1.x:  自定义数据（可传给 Shader）
    // - Custom2.x:  自定义数据
    
    // 添加自定义数据流
    renderer.material = customParticleMaterial;
}
```

---

## 6. 音频性能优化

### 6.1 音频内存策略

```csharp
/*
 ─── 音频加载类型的内存影响 ───

 Decompress On Load（加载时解压）：
 ┌────────────────────────────────────┐
 │ 内存：大（完全解压的 PCM 数据）      │
 │ CPU：加载时解压，播放时无负担         │
 │ 适合：短音效（<5秒）                 │
 │ ────────────────────────────────── │
 │ 示例：10MB MP3 → 50MB PCM          │
 └────────────────────────────────────┘

 Compressed In Memory（内存中压缩）：
 ┌────────────────────────────────────┐
 │ 内存：小（存储压缩格式）              │
 │ CPU：播放时实时解压（略耗 CPU）       │
 │ 适合：中等时长的音效                   │
 │ ────────────────────────────────── │
 │ 示例：10MB MP3 → 10MB（保持压缩）    │
 └────────────────────────────────────┘

 Streaming（流式加载）：
 ┌────────────────────────────────────┐
 │ 内存：极小（只缓冲几秒）              │
 │ CPU：持续从磁盘或网络流式读取          │
 │ 适合：背景音乐、长语音                 │
 │ ────────────────────────────────── │
 │ 示例：10MB MP3 → ~0.5MB 缓冲        │
 └────────────────────────────────────┘
*/
```

### 6.2 音频性能预算

```csharp
/*
 ─── 音频性能指南 ───

 移动端预算：
 ┌────────────────────────────────────┐
 │ 同时播放的最大音效数：8~12           │
 │ 总音频内存：15~25 MB                 │
 │ AudioSource 组件数：< 20            │
 │ 采样率：22050 Hz（比 44100 省一半）  │
 │ 压缩格式：Vorbis (质量 0.6~0.7)     │
 └────────────────────────────────────┘

 PC/主机预算：
 ┌────────────────────────────────────┐
 │ 同时播放的最大音效数：32~48          │
 │ 总音频内存：50~100 MB                │
 │ AudioSource 组件数：< 64            │
 │ 采样率：44100 Hz                     │
 │ 压缩格式：Vorbis (质量 0.8~0.9)     │
 └────────────────────────────────────┘

 质量 vs 性能取舍：
 44100 Hz → 22050 Hz → 11025 Hz
 （质量降级，内存减半→再减半）
 
 立体声 → 单声道
 （内存减半，适用：不需要左右区分的音效）
*/
```

### 6.3 音频调试工具

```csharp
// ─── Audio Profiler 的使用 ───

using UnityEngine.Profiling;

public class AudioDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            // 输出音频统计信息
            Debug.Log($"Audio Sources: {FindObjectsOfType<AudioSource>().Length}");
            
            // Profiler 中的音频信息：
            // Window → Analysis → Profiler → Audio
            // 查看：
            // - 当前播放的 AudioSource 数
            // - 音频解码器使用情况
            // - 总音频内存
            // - 音频相关 CPU 时间
        }
    }
}

/*
 ─── 常见音频性能问题 ───

 1. 太多 AudioSource 同时播放
    解决方法：AudioSource Pool（对象池）

 2. 大音效使用 Decompress On Load
    解决方法：改为 Streaming

 3. 不必要的 3D 处理（2D UI 音效）
    解决方法：spatialBlend = 0

 4. 音频文件太大
    解决方法：降低采样率、用单声道
*/
```

---

## C++/Raylib 对照总结

| 概念 | C++ / Raylib | Unity 进阶音频/VFX |
|------|-------------|-------------------|
| 混音器 | 手动混合音频缓冲区 | AudioMixer + Snapshot + Effects |
| 空间音频 | 简单的音量衰减 | HRTF Spatializer + Ambisonic |
| 随机音频 | `rand() % n` 手选 | AudioRandomContainer 自动管理 |
| 粒子系统 | 手写循环缓冲数组 | VFX Graph（GPU 加速，百万级粒子）|
| 子发射器 | 手动粒子生成 | Sub-Emitters 链式反应 |
| 粒子 Shader | 手写 GLSL | Shader Graph + 自定义数据流 |
| 音频内存 | 手动管理缓冲区 | 三种加载策略 + Profiler 监控 |
| — | 无 | AudioMixer Snapshot 过渡 |
| — | 无 | VFX Graph 的 Compute Shader |

## 停靠点

> AudioMixer 的信号流：输入 → 衰减 → 效果器链 → 组音量 → 输出。
> Snapshot 是 Mixer 的完整状态快照——用 TransitionTo 在不同状态间平滑过渡。
> HRTF 空间音频用频谱滤波模拟人耳定位——比简单的音量差更真实。
> AudioRandomContainer 通过变体 + 随机音调打破重复感——Non-Repeating 避免连续重复。
> VFX Graph 用 GPU Compute Shader 做粒子更新——百万粒子也不卡。
> Sub-Emitters 让粒子系统可以链式触发（爆炸 → 碎片 → 烟雾 → 火花）。
> 音频性能的关键：选对加载类型，控制同时播放数，降低采样率。
