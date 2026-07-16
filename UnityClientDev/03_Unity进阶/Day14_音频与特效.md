# Day 14：音频与特效 — 从 PCM 数据到粒子系统

## 0. 为什么需要音频和特效系统？

在 Raylib/C++ 中，音频和特效也是手动管理的：

```cpp
// Raylib：手动管理音效
InitAudioDevice();
Sound shoot = LoadSound("shoot.wav");
PlaySound(shoot);

// 粒子系统也是手写的循环缓冲区
struct Particle {
    Vector2 pos;
    Vector2 vel;
    float life;
};
Particle particles[100];
```

Unity 提供了完整的音频管线和粒子系统——几千个参数帮你自动管理，你只需要配置。

---

## 1. Unity 音频系统架构

### 音频数据的从文件到播放

```
硬盘上的音频文件 (.wav, .mp3, .ogg, .aiff)
    │
    ▼ 导入设置
Unity Audio Importer
  - 加载类型：Decompress On Load / Compressed In Memory / Streaming
  - 压缩格式：PCM / Vorbis / ADPCM
  - 采样率：保持原样或强制转换
    │
    ▼ AudioClip（运行时）
存储在内存中的音频数据
  - 对于小音效：完全解压到内存
  - 对于大音乐：流式读取（不全部加载）
    │
    ▼ AudioSource（播放器）
控制播放行为
  - 播放、暂停、停止
  - 音量、音调、循环
  - 3D 空间音效
    │
    ▼ AudioMixer（混音器）
信号处理
  - 分组控制
  - 添加效果（回声、混响、低通）
    │
    ▼ 音频输出设备
扬声器或耳机
```

### AudioSource——播放器

```csharp
public class SoundManager : MonoBehaviour
{
    public AudioClip bgm;         // 背景音乐
    public AudioClip shootSound;  // 射击音效
    public AudioClip hitSound;    // 受击音效

    private AudioSource musicSource;
    private AudioSource sfxSource;

    void Awake()
    {
        // 创建两个独立的 AudioSource
        // 原因：BGM 需要循环，SFX 需要一次性
        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        // 配置 BGM
        musicSource.clip = bgm;
        musicSource.loop = true;          // 循环播放
        musicSource.volume = 0.5f;        // 音量 50%
        musicSource.priority = 0;         // 优先级（0 最高，256 最低）
        musicSource.Play();               // 开始播放
    }

    void Update()
    {
        // 播放音效（一次性）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // PlayOneShot：在现有播放基础上叠加播放
            // 不会中断当前正在播放的音频
            sfxSource.PlayOneShot(shootSound, 1.0f);  // 音量系数 1.0
        }

        // 受击音效（可以同时播放）
        if (Input.GetKeyDown(KeyCode.H))
        {
            sfxSource.PlayOneShot(hitSound, 0.8f);
        }
    }
}
```

### AudioSource 的关键属性

```csharp
AudioSource source = GetComponent<AudioSource>();

// ─── 基础控制 ───
source.Play();          // 从头播放
source.Pause();         // 暂停（保留位置）
source.UnPause();       // 继续
source.Stop();          // 停止（回到开头）
source.time = 30f;      // 跳转到指定时间（秒）

// ─── 音量与音调 ───
source.volume = 0.5f;   // 音量（0~1）
source.pitch = 1.0f;    // 音调（1=原速，2=两倍速）
// pitch > 1：声音变高、变快（高速倍速）
// pitch < 1：声音变低、变慢（慢速倍速）

// ─── 循环 ───
source.loop = true;     // 循环播放

// ─── 3D 音效 ───
source.spatialBlend = 1.0f;  // 0 = 2D, 1 = 3D（空间音效）
source.minDistance = 5f;     // 最小距离（音量最大）
source.maxDistance = 50f;    // 最大距离（音量归零）
source.rolloffMode = AudioRolloffMode.Linear;  // 衰减模式
```

### 3D 音效原理

```
3D 音效 = 根据听者（AudioListener）和声源（AudioSource）的相对位置调整音量

听者在 AudioSource 的：
- 左侧：左声道更响
- 右侧：右声道更响
- 远处：音量衰减
- 近处：音量最大（minDistance 内无衰减）

衰减曲线（Rolloff）：
音量
1.0 ┤    ╱╲
    │   ╱  ╲          ← Linear（线性）：均匀衰减
    │  ╱    ╲
    │ ╱      ╲        ← Logarithmic（对数）：体验更自然
0.0 ┤╱        ╲
    └──────────────────
    距离    min     max
```

### AudioMixer——混音器

```csharp
// AudioMixer 是混音和效果处理的中心

// 创建：Assets → Create → Audio Mixer

// 混音器结构：
Master
├── Music（BGM 组）
├── SFX（音效组）
│   ├── Weapons（武器音效子组）
│   └── UI（UI 音效子组）
└── Voice（语音组）

// 每个组可以：
// 1. 独立控制音量
// 2. 添加效果器（Echo、Reverb、LowPass 等）
// 3. 通过 Snapshot 切换配置

// 代码控制音量
using UnityEngine.Audio;

public class AudioMixerControl : MonoBehaviour
{
    public AudioMixer mixer;

    void SetVolume(float volume)
    {
        // AudioMixer 的 volume 范围是 -80~0 dB
        // 需要做映射
        mixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        mixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
    }

    void SetEffect()
    {
        // 切换到"LowPass" Snapshot（如：暂停菜单时切背景音）
        AudioMixerSnapshot snapshot = mixer.FindSnapshot("LowPass");
        snapshot.TransitionTo(0.5f);  // 0.5 秒过渡
    }
}
```

---

## 2. Particle System——粒子系统

### 粒子系统的架构

```
Particle System（C++ 粒子引擎）

每帧执行：
1. Emit（发射）
   - 按 rate 生成新粒子
   - 或一次性发射（Burst）

2. Update（更新）
   - 更新每个粒子的位置、速度、颜色、大小
   - 根据模块配置改变粒子属性

3. Render（渲染）
   - 将所有粒子合并到一个 Mesh
   - 使用 Billboard（广告牌）技术（始终面向摄像机）
   - 提交渲染
```

### 粒子系统的模块

```csharp
// Particle System 的模块系统：
// 每个模块控制一个方面的行为

ParticleSystem ps = GetComponent<ParticleSystem>();
var main = ps.main;         // 主模块：持续时间、循环、起始属性
var emission = ps.emission; // 发射模块：发射速率、Burst
var shape = ps.shape;       // 形状模块：从什么形状发射
var velocity = ps.velocityOverLifetime; // 速度
var color = ps.colorOverLifetime;       // 颜色变化
var size = ps.sizeOverLifetime;         // 大小变化
var rotation = ps.rotationOverLifetime; // 旋转
var noise = ps.noise;       // 噪声（随机扰动）
var renderer = ps.renderer; // 渲染设置
```

### 代码控制粒子系统

```csharp
public class EffectManager : MonoBehaviour
{
    public ParticleSystem explosionPrefab;  // 爆炸特效 Prefab
    public ParticleSystem trailPrefab;      // 拖尾特效 Prefab

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 在鼠标位置播放爆炸特效
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                Input.mousePosition + Vector3.forward * 10f);
            PlayExplosion(worldPos);
        }
    }

    void PlayExplosion(Vector3 position)
    {
        // Instantiate 粒子系统并播放
        ParticleSystem ps = Instantiate(explosionPrefab, position, Quaternion.identity);
        ps.Play();

        // 自动销毁（播放完后）
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }
}
```

### 粒子系统的性能

```csharp
// 粒子系统的性能瓶颈：
// 1. 粒子数量：每帧更新的粒子数
// 2. Overdraw：半透明粒子重叠在屏幕上

// 性能优化建议：
// - 控制最大粒子数（Max Particles）
// - 减少半透明粒子的 Overdraw
// - 使用 GPU 粒子（Particle System 的 Renderer 选择 Mesh）
// - 小粒子不需要旋转、碰撞等模块

// 性能预算：
// 移动端：最多 100~200 个粒子同时存在
// PC 端：最多 500~1000 个粒子同时存在
```

---

## 3. VFX Graph——高级视觉特效

```csharp
// VFX Graph 是 Unity 2019+ 的高级特效系统
// 基于 GPU 计算（Compute Shader）
// 可以处理数十万粒子

// 对比 Particle System：
// Particle System  | VFX Graph
// CPU 粒子         | GPU 粒子
// 几千个粒子        | 数十万个粒子
// 组件化配置        | 节点图编程
// 适合小特效        | 适合大场景特效

// VFX Graph 无法在代码中创建——必须通过 .vfx 文件
// 但可以通过属性控制：

public class VFXControl : MonoBehaviour
{
    public VisualEffect vfx;

    void Start()
    {
        // 设置 VFX Graph 的公开属性
        vfx.SetFloat("SpawnRate", 100);
        vfx.SetVector3("SpawnPosition", transform.position);

        // 播放
        vfx.Play();
    }
}
```

---

## 4. Post-processing——后期处理

### 什么是 Post-processing？

```
Post-processing = 最终渲染完成后对屏幕图像进行处理

输入：摄像机渲染的帧缓冲
    │
    ▼
后处理链（顺序可配置）：
1. Anti-aliasing（抗锯齿）
2. Ambient Occlusion（环境光遮蔽）
3. Bloom（泛光）
4. Tonemapping（色调映射）
5. Color Grading（颜色分级）
6. Vignette（暗角）
7. Depth of Field（景深）
8. Motion Blur（运动模糊）
    │
    ▼
输出：最终屏幕图像
```

### URP 中的后处理配置

```csharp
// 在 URP 中使用 Post-processing：

// 1. 创建 Volume 组件
// 2. 添加 Volume Profile
// 3. 勾选需要的后处理效果

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessControl : MonoBehaviour
{
    public Volume volume;  // 场景中的 Volume 组件

    private Vignette vignette;
    private Bloom bloom;
    private ColorAdjustments colorAdjustments;

    void Start()
    {
        // 获取 Volume Profile 中的效果
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out bloom);
        volume.profile.TryGet(out colorAdjustments);

        // 启用/禁用
        bloom.active = true;
    }

    void Update()
    {
        // 受伤时暗角
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartCoroutine(FadeVignette(0.5f, 0.1f));  // 0.5 → 0.1
        }
    }

    IEnumerator FadeVignette(float from, float to)
    {
        float elapsed = 0;
        float duration = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            vignette.intensity.value = Mathf.Lerp(from, to, t);
            yield return null;
        }
        vignette.intensity.value = to;
    }
}
```

---

## 练习：音频 + 特效系统

```csharp
using UnityEngine;

public class Day14_AudioVFX : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip explosionSound;
    private AudioSource audioSource;

    [Header("VFX")]
    public ParticleSystem muzzleFlash;    // 枪口火焰
    public ParticleSystem hitEffect;      // 命中特效
    public ParticleSystem explosionEffect; // 爆炸特效

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // 音效
        audioSource.PlayOneShot(shootSound);

        // 枪口火焰
        muzzleFlash.Play();

        // 射线检测
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // 命中特效
            ParticleSystem hit = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
            hit.Play();
            Destroy(hit.gameObject, 2f);

            // 命中音效
            AudioSource.PlayClipAtPoint(explosionSound, hit.point);

            // 物理冲击
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForceAtPosition(
                    ray.direction * 10f, hit.point, ForceMode.Impulse);
            }
        }
    }
}
```

---

---

## C++/Raylib 对照总结

| 概念 | Raylib (C++) | Unity (C#) |
|------|-------------|-----------|
| 音频初始化 | `InitAudioDevice()` | Unity 自动初始化 |
| 加载音效 | `LoadSound("file.wav")` | `AudioClip` 拖入 Inspector |
| 播放音效 | `PlaySound(sound)` | `AudioSource.PlayOneShot(clip)` |
| 背景音乐 | `PlayMusicStream()` + `UpdateMusicStream()` | `AudioSource.loop = true` + `Play()` |
| 音量控制 | `SetSoundVolume(sound, vol)` | `AudioSource.volume` |
| — | 无 | AudioMixer（分组混音） |
| — | 无 | 3D 空间音效（衰减） |
| 粒子 | 手写循环缓冲区 | Particle System（模块化） |
| — | 无 | VFX Graph（GPU 粒子） |
| 后期特效 | 无 | Post-processing Volume |

## 停靠点

> AudioSource 处理播放控制，AudioMixer 处理混音和效果。
> 3D 音效 = 音量随距离衰减 + 左右声道偏移。
> Particle System = CPU 粒子引擎，适合几百个粒子。
> VFX Graph = GPU 粒子系统，适合几十万个粒子。
> Post-processing = 渲染完成后的屏幕特效（Bloom、暗角、色调映射）。

