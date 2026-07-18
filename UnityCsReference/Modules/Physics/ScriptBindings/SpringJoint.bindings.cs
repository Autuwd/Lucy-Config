// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 SpringJoint — 弹簧关节
//
// 📌 作用：
//   在两个刚体之间模拟弹簧连接。
//   弹簧力公式：F = -k * distance - d * velocity（胡克定律 + 阻尼）
//
// 💡 核心参数：
//   - spring：弹性系数（刚度，越大越硬）
//   - damper：阻尼系数（缓冲，越大越稳）
//   - minDistance/maxDistance：弹簧作用范围
//   - tolerance：弹簧放松阈值
//   - autoConfigureConnectedAnchor：自动锚点配置
//
// ⚡ 适用：弹跳平台、绳索、弹性武器、悬挂。
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/SpringJoint.h")]
    [NativeClass("Unity::SpringJoint")]
    public class SpringJoint : Joint
    {
        extern public float spring { get; set; }
        extern public float damper { get; set; }
        extern public float minDistance { get; set; }
        extern public float maxDistance { get; set; }
        extern public float tolerance { get; set; }
    }
}
