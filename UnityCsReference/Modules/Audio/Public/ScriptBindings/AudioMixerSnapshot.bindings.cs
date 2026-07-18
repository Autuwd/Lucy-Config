// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// 🎯 AudioMixerSnapshot —— 混音器的参数快照
//     TransitionTo(timeToReach) 在指定时间内平滑过渡到该快照
// 💡 快照可保存一组 effect 参数值，用于 Duck Volume 等动态混音
// 📌 必须属于同一个 AudioMixer，否则抛出异常
// ============================================================
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine.Audio
{
    [NativeHeader("Modules/Audio/Public/AudioMixerSnapshot.h")]
    public partial class AudioMixerSnapshot : Object, ISubAssetNotDuplicatable
    {
        internal AudioMixerSnapshot() {}

        [NativeProperty]
        public extern AudioMixer audioMixer { get; }

        public void TransitionTo(float timeToReach)
        {
            audioMixer.TransitionToSnapshot(this, timeToReach);
        }
    }
}
