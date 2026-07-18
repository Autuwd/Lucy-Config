// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 FixedJoint — 固定关节
//
// 📌 作用：
//   将两个刚体完全固定在一起，保持相对位置和旋转不变。
//   相当于两个物体之间用"焊接"连接，不可断裂。
//
// 💡 无需额外参数，基类 Joint 的 anchor/breakForce 等属性可用。
//
// ⚡ 适用场景：将物体连接到移动平台、门与框、多部件机械。
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/FixedJoint.h")]
    [NativeClass("Unity::FixedJoint")]
    public class FixedJoint : Joint {}
}
