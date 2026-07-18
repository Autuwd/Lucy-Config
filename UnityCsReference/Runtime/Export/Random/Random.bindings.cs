// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Random — 全局随机数生成器
//
// 📌 作用：
//   提供伪随机数生成，支持可复现的确定序列
//
// 💡 关键概念：
//   - InitState(seed): 用种子初始化 RNG，相同种子 → 相同序列
//   - State: 完整 RNG 状态快照，保存/恢复实现确定性随机
//   - value: [0, 1] 均匀分布 float
//   - Range(int): [min, maxExclusive) 整数范围
//   - Range(float): [min, maxInclusive] 浮点范围
//
// 💡 实用分布：
//   insideUnitSphere / onUnitSphere / insideUnitCircle / rotation
//
// ⚠️ 旧版 seed 属性已废弃，用 InitState() 或 state 属性替代
// ==============================================================

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Class for generating random data.
    [NativeHeader("Runtime/Export/Random/Random.bindings.h")]
    public static partial class Random
    {
        // Random number generator engine state struct
        [System.Serializable]
        public struct State
        {
#pragma warning disable 0169
            [SerializeField]
            private int s0;
            [SerializeField]
            private int s1;
            [SerializeField]
            private int s2;
            [SerializeField]
            private int s3;
        }

        // Initializes the RNG state with a 32 bit seed
        [StaticAccessor("GetScriptingRand()", StaticAccessorType.Dot)]
        [NativeMethod("SetSeed")]
        extern public static void InitState(int seed);

        // Gets/Sets the state of the random number generator.
        [StaticAccessor("GetScriptingRand()", StaticAccessorType.Dot)]
        extern public static Random.State state { get; set; }

        // Returns a random float number between and [minInclusive, maxInclusive] (RO).
        [FreeFunction]
        extern public static float Range(float minInclusive, float maxInclusive);

        // Returns a random integer number between [minInclusive, maxExclusive) (RO).
        public static int Range(int minInclusive, int maxExclusive) { return RandomRangeInt(minInclusive, maxExclusive); }

        [FreeFunction]
        extern private static int RandomRangeInt(int minInclusive, int maxExclusive);

        // Returns a random number between 0.0 [inclusive] and 1.0 [inclusive] (RO).
        extern public static float value
        {
            [FreeFunction]
            get;
        }

        // Returns a random point inside a sphere with radius 1 (RO).
        extern public static Vector3 insideUnitSphere
        {
            [FreeFunction]
            get;
        }

        // Workaround for gcc/msvc where passing small mono structures by value does not work
        [FreeFunction]
        extern private static void GetRandomUnitCircle(out Vector2 output);

        // Returns a random point inside a circle with radius 1 (RO).
        public static Vector2 insideUnitCircle { get { Vector2 r; GetRandomUnitCircle(out r); return r; } }

        // Returns a random point on the circumference of a circle with radius 1 (RO).
        extern public static Vector2 onUnitCircle
        {
            [FreeFunction]
            get;
        }

        // Returns a random point on the surface of a sphere with radius 1 (RO).
        extern public static Vector3 onUnitSphere
        {
            [FreeFunction]
            get;
        }

        // Returns a random rotation (RO).
        extern public static Quaternion rotation
        {
            [FreeFunction]
            get;
        }

        // Returns a random rotation with uniform distribution(RO).
        extern public static Quaternion rotationUniform
        {
            [FreeFunction]
            get;
        }

        // OBSOLETE

        [StaticAccessor("GetScriptingRand()", StaticAccessorType.Dot)]
        [Obsolete("Deprecated. Use InitState() function or Random.state property instead.")]
        extern public static int seed { get; set; }

        [Obsolete("Use Random.Range instead")]
        public static float RandomRange(float min, float max)  { return Range(min, max); }

        [Obsolete("Use Random.Range instead")]
        public static int RandomRange(int min, int max) { return Range(min, max); }
    }
}
