// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// 🎯 AudioMixerGroup —— 混音树中的单一节点/分组
//     每个 Group 可包含子 Group、Effects、Snapshots
// 💡 通过 audioMixer 属性反向引用所属的 AudioMixer
// 📌 实现 ISubAssetNotDuplicatable 防止编辑器复制
// ============================================================
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine.Audio
{
    [NativeHeader("Modules/Audio/Public/AudioMixerGroup.h")]
    public class AudioMixerGroup : Object, ISubAssetNotDuplicatable
    {
        // Make constructor internal
        internal AudioMixerGroup() {}

        [NativeProperty]
        public extern AudioMixer audioMixer { get; }
    }
}
