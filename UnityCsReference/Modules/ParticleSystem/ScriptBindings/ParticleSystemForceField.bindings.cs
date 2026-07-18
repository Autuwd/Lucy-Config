// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ParticleSystemForceField — 粒子系统力场
//
// 📌 作用：
//   为粒子系统（ParticleSystem）添加外部力场影响。
//   支持方向力、涡流、噪声等模式，用 MinMaxCurve 控制强度。
//
// 💡 核心类型：
//   - ParticleSystemForceFieldShape：形状（Sphere/Hemisphere/Cylinder/Box/Donut）
//   - 强度通过 MinMaxCurve 设定，支持随机范围
//
// ⚡ 适用：风场、引力场、涡旋、爆炸冲击波等环境效果。
// ==============================================================

using UnityEngine.Bindings;
using MinMaxCurve = UnityEngine.ParticleSystem.MinMaxCurve;
using MinMaxCurveBlittable = UnityEngine.ParticleSystem.MinMaxCurveBlittable;

namespace UnityEngine
{
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemForceField.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemForceFieldManager.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [RequireComponent(typeof(Transform))]
    public partial class ParticleSystemForceField : Behaviour
    {
        [NativeName("ForceShape")]
        extern public ParticleSystemForceFieldShape shape { get; set; }
        extern public float startRange { get; set; }
        extern public float endRange { get; set; }
        extern public float length { get; set; }
        extern public float gravityFocus { get; set; }
        extern public Vector2 rotationRandomness { get; set; }
        extern public bool multiplyDragByParticleSize { get; set; }
        extern public bool multiplyDragByParticleVelocity { get; set; }
        extern public Texture3D vectorField { get; set; }

        public MinMaxCurve directionX { get => directionXBlittable; set => directionXBlittable = value; }
        [NativeName("DirectionX")] private extern MinMaxCurveBlittable directionXBlittable { get; set; }

        public MinMaxCurve directionY { get => directionYBlittable; set => directionYBlittable = value; }
        [NativeName("DirectionY")] private extern MinMaxCurveBlittable directionYBlittable { get; set; }

        public MinMaxCurve directionZ { get => directionZBlittable; set => directionZBlittable = value; }
        [NativeName("DirectionZ")] private extern MinMaxCurveBlittable directionZBlittable { get; set; }

        public MinMaxCurve gravity { get => gravityBlittable; set => gravityBlittable = value; }
        [NativeName("Gravity")] private extern MinMaxCurveBlittable gravityBlittable { get; set; }

        public MinMaxCurve rotationSpeed { get => rotationSpeedBlittable; set => rotationSpeedBlittable = value; }
        [NativeName("RotationSpeed")] private extern MinMaxCurveBlittable rotationSpeedBlittable { get; set; }

        public MinMaxCurve rotationAttraction { get => rotationAttractionBlittable; set => rotationAttractionBlittable = value; }
        [NativeName("RotationAttraction")] private extern MinMaxCurveBlittable rotationAttractionBlittable { get; set; }

        public MinMaxCurve drag { get => dragBlittable; set => dragBlittable = value; }
        [NativeName("Drag")] private extern MinMaxCurveBlittable dragBlittable { get; set; }

        public MinMaxCurve vectorFieldSpeed { get => vectorFieldSpeedBlittable; set => vectorFieldSpeedBlittable = value; }
        [NativeName("VectorFieldSpeed")] private extern MinMaxCurveBlittable vectorFieldSpeedBlittable { get; set; }

        public MinMaxCurve vectorFieldAttraction { get => vectorFieldAttractionBlittable; set => vectorFieldAttractionBlittable = value; }
        [NativeName("VectorFieldAttraction")] private extern MinMaxCurveBlittable vectorFieldAttractionBlittable { get; set; }

        [StaticAccessor("GetParticleSystemForceFieldManager()", StaticAccessorType.Dot)]
        [NativeMethod("GetForceFields")]
        extern public static ParticleSystemForceField[] FindAll();
    }
}
