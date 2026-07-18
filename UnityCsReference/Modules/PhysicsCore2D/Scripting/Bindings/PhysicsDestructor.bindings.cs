// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PhysicsDestructor — 几何破坏器，实现碎片化和切割
//
// 📌 作用：
//   PhysicsDestructor 提供两种几何破坏操作：
//   1. Fragment（碎片化）：在几何体上指定分裂点，将其破碎成多块
//   2. Slice（切割）：沿一条线将几何体切割成两半
//   用于实现可破坏场景、切割效果等。
//
// 🏗 架构位置：Destructor → FragmentResult/SliceResult
//   破坏操作输出多个 FragmentGeometry，可直接创建 PhysicsShape
//
// 💡 关键能力：
//   - Fragment()：在指定点集处碎裂几何体
//   - FragmentMasked()：用掩码几何体遮盖后再碎裂
//   - Slice()：沿指定方向切割几何体
//
// 📌 结果包含碎片的位置、旋转、几何数据，
//   可用于创建新的 PhysicsBody + PhysicsShape。
// ==============================================================

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    [NativeHeader("Modules/PhysicsCore2D/Core/PhysicsDestructor2D.h")]
    [StaticAccessor("PhysicsDestructor2D", StaticAccessorType.DoubleColon)]
    static class PhysicsDestructorScripting2D
    {
        [NativeMethod(Name = "Fragment", IsThreadSafe = true)] extern internal static PhysicsDestructor.FragmentResult PhysicsDestructor_Fragment(PhysicsDestructor.FragmentGeometry target, ReadOnlySpan<Vector2> fragmentPoints, Allocator allocator);
        [NativeMethod(Name = "FragmentMasked", IsThreadSafe = true)] extern internal static PhysicsDestructor.FragmentResult PhysicsDestructor_FragmentMasked(PhysicsDestructor.FragmentGeometry target, PhysicsDestructor.FragmentGeometry mask, ReadOnlySpan<Vector2> fragmentPoints, Allocator allocator);
        [NativeMethod(Name = "Slice", IsThreadSafe = true)] extern internal static PhysicsDestructor.SliceResult PhysicsDestructor_Slice(PhysicsDestructor.FragmentGeometry target, Vector2 origin, Vector2 translation, Allocator allocator);
    }
}
