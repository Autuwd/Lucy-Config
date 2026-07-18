// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.VFX
{
    // ================================================================
    // VFXSpawnerCallbacks —— VFX 生成器脚本回调
    //
    // 🎯 自定义生成器脚本，实现 OnPlay/OnUpdate/OnStop 控制粒子发射
    // 💡 生成器架构：
    //   VFX Graph 中的 Spawner 负责"发射决策"，决定每秒/每次生成多少粒子
    //   Spawner 通过内置的 ConstantRate/Burst/PeriodicBurst 或
    //   自定义的 CustomCallbackSpawner（即 VFXSpawnerCallbacks）控制发射
    // 📌 回调生命周期：
    //   - OnPlay：Spawner 启动时调用（接收 Play 事件）
    //   - OnUpdate：每帧调用，通过 spawnCount 控制发射数量
    //   - OnStop：Spawner 停止时调用（接收 Stop 事件）
    // ⚡ 使用方式：继承此类，挂载到 VFX Graph 的 Custom Spawner 块中
    // ================================================================
    [System.Serializable]
    [RequiredByNativeCode]
    public abstract class VFXSpawnerCallbacks : ScriptableObject
    {
        public abstract void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent);
        public abstract void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent);
        public abstract void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent);
    }
}
