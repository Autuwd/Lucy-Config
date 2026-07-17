// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ReflectionProbe — 反射探针系统
//
// 📌 作用：
//   ReflectionProbe 在场景中捕获周围环境的立方体贴图（Cubemap），
//   供反射探针（Reflection Probe）组件使用，实现反射效果。
//   反射探针是 Unity 中实现实时/烘焙反射的核心机制。
//
// 🏗 核心概念：
//   - 反射探针 = 在场景中放置一个"摄像机"，向 6 个方向渲染
//     环境快照，存储为 Cubemap，供附近的反射材质使用。
//   - 反射探针的边界框（Bounds）定义其影响范围。
//   - 物体在渲染时，根据位置选择最近的反射探针。
//   - 环境探针（Scene Reflection）是全局后备反射。
//
// 📌 反射模式（ReflectionProbeMode）：
//   - Baked：预烘焙到光照探针系统（运行时零开销）
//   - Realtime：每帧/按需实时渲染（高开销但精确）
//   - Custom：手动指定 Cubemap 纹理
//   - Off：禁用此反射探针
//
// ⚡ 性能提示：
//   - Baked 模式性能最佳，适合静态环境
//   - Realtime 模式每帧渲染 6 个面，开销巨大
//   - 可设置 Realtime 刷新频率（refreshMode）
//   - 反射探针数量影响反射质量：越多越精确但越慢
//
// 💡 探针使用优先级：
//   - 先尝试 Local Reflection Probe（有 Bounds 限制）
//   - 回退到 Scene Reflection Probe（全局）
//   - 最后使用环境光照设置
//
// 📍 对应 C++ 头文件：Runtime/Graphics/ReflectionProbe.h
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    // ==============================================================
    // ReflectionProbe — 反射探针组件
    //
    // 🎯 继承链：ReflectionProbe → Behaviour → Component → Object
    //
    // 🔑 关键属性：
    //   - mode：反射模式（Baked/Realtime/Custom/Off）
    //   - bounds：探针影响范围（AxisAligned Bounding Box）
    //   - intensity：反射强度倍率
    //   - hdr：是否使用 HDR 格式存储
    //   - resolution：Cubemap 分辨率
    //   - importance：探针重要性（用于多个探针混合）
    //   - cullingMask：影响哪些层的物体
    //   - blendDistance：与相邻探针的混合距离
    //   - probePosition：探针在场景中的位置
    //   - probeSize：探针的物理尺寸
    //   - cubemap：烘焙/渲染后的 Cubemap 数据
    //   - customBakedTexture：自定义烘焙纹理
    //
    // 🔑 关键方法：
    //   - Reset()：重置为默认值
    //   - RenderProbe()：手动触发实时渲染
    //   - IsFinishedRendering()：检查渲染是否完成
    //   - GetEnvironmentData()：获取环境探针数据
    //
    // 📌 渲染控制：
    //   - renderDynamicObjects：是否渲染非静态对象
    //   - nearClip / farClip：渲染摄像机的近远裁面
    //   - shadowDistance：阴影渲染距离
    //
    // 💡 反射探针工作流程：
    //   1. 在场景中放置 GameObject，添加 ReflectionProbe 组件
    //   2. 调整 bounds 和位置，使其覆盖需要反射的区域
    //   3. 设置 mode（Baked/Realtime）
    //   4. Baked 模式：Lighting 窗口点击"Bake"烘焙
    //   5. Realtime 模式：运行时自动或手动 RenderProbe()
    //   6. 附近的材质（Standard Shader 的 Reflections 槽）自动采样
    // ==============================================================

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Graphics/ReflectionProbe.h")]
    [NativeHeader("Runtime/Export/Graphics/ReflectionProbe.bindings.h")]
    public sealed partial class ReflectionProbe : Behaviour
    {
        extern public void Reset();

        extern public ReflectionProbeMode mode { get; set; }

        extern public Texture customBakedTexture { get; set; }

        extern public Cubemap bakedTexture { get; set; }

        extern public Cubemap realTimeTexture { get; }

        extern public Texture texture { get; }

        extern public Vector4 defaultCenterOffset { get; set; }

        extern public bool sizeCustomBaked { get; set; }

        extern public Vector3 size { get; set; }

        extern public Bounds bounds { get; }

        extern public float intensity { get; set; }

        extern public bool hdr { get; set; }

        extern public int resolution { get; set; }

        extern public float nearClipPlane { get; set; }

        extern public float farClipPlane { get; set; }

        extern public float shadowDistance { get; set; }

        extern public int cullingMask { get; set; }

        extern public int importance { get; set; }

        extern public bool renderDynamicObjects { get; set; }

        extern public float blendDistance { get; set; }

        extern public ReflectionProbeRefreshMode refreshMode { get; set; }

        extern public int timeSlicingMode { get; set; }

        extern public bool boxProjection { get; set; }

        extern public ReflectionProbeType type { get; set; }

        extern public Vector3 probePosition { get; }

        extern public Quaternion probeRotation { get; }

        extern public bool isFinishedRendering { get; }

        public int RenderProbe()
        {
            return RenderProbe(-1);
        }

        public int RenderProbe(int faceMask)
        {
            return RenderProbeIntoCubemap(faceMask);
        }

        [FreeFunction("ReflectionProbe_Bindings::RenderProbeIntoCubemap", HasExplicitThis = true)]
        extern private int RenderProbeIntoCubemap(int faceMask);

        extern public bool IsFinishedRendering(int renderId);

        extern public void BlendCubemap(Texture src, float weight, Cubemap dst);

        [FreeFunction("ReflectionProbe_Bindings::GetGlobalDefaultReflection", ThrowsException = true)]
        extern internal static Cubemap GetGlobalDefaultReflection();

        extern public static Texture defaultTexture { get; }

        extern public ReflectionProbeProbeType probeType { get; set; }

        extern public float exposureCompensation { get; set; }

        extern public Vector4 layerMask { get; set; }

        [FreeFunction("ReflectionProbe_Bindings::ScheduleReflectionProbeUpdate", HasExplicitThis = true)]
        extern private void ScheduleReflectionProbeUpdate();

        public ReflectionProbeBakingData bakingData { get; set; }
    }
}
