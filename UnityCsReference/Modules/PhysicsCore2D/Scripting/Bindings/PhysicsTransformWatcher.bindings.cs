// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PhysicsTransformWatcher — Transform 变更监听器
//
// 📌 作用：
//   注册/注销对 Unity Transform 组件的变更监听。
//   当 Transform 被外部修改时，通知 PhysicsCore2D 同步物理世界。
//   确保物理引擎中的 PhysicsBody 位置与 Transform 保持一致。
//
// 🏗 架构位置：外部桥接层
//   连接 Unity Transform 系统和 PhysicsCore2D 引擎
//
// 💡 工作原理：
//   - RegisterTransformWatcher(Transform)：开始监听该 Transform
//   - UnregisterTransformWatcher(Transform)：停止监听
//   - 内部在 Native 端维护变更跟踪器
//   - 引擎在每次模拟前检查并同步已变更的 Transform
// ==============================================================

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsCore2D::RegisterTransformWatcher")] extern internal static void PhysicsCore2D_RegisterTransformWatcher(Transform transform);
        [NativeMethod(Name = "PhysicsCore2D::UnregisterTransformWatcher")] extern internal static void PhysicsCore2D_UnregisterTransformWatcher(Transform transform);
    }
}
