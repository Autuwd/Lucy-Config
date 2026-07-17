// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// SpriteShapeUtility — 2D 形状生成工具
//
// 🎯 核心作用：
//   提供将样条控制点数据转换为实际网格的静态方法。
//   这是 SpriteShape 系统的核心生成引擎。
//
// 📌 核心数据结构：
//   - ShapeControlPoint：控制点
//     · position：控制点位置
//     · leftTangent/rightTangent：左右切线方向（贝塞尔控制）
//     · mode：插值模式（线性/平滑等）
//
//   - SpriteShapeMetaData：每个控制点的元数据
//     · height：该段的高度
//     · spriteIndex：使用的 Sprite 索引
//     · corner：是否为角点
//     · bevelCutoff/bevelSize：斜面参数
//
//   - AngleRangeInfo：角度范围配置
//     · start/end：角度范围（度）
//     · order：优先级（决定多个范围重叠时使用哪个 Sprite）
//     · sprites：该角度范围对应的 Sprite 索引数组
//
// 💡 角度阈值机制：
//   当样条曲线在某段的角度变化超过 angleThreshold（在 SpriteShapeParameters 中），
//   系统会自动切换到角点（Corner）Sprite，实现平滑的弯道过渡。
//   AngleRangeInfo 定义了不同角度范围对应的不同 Sprite。
//
// ⚡ 两种生成方式：
//   - Generate()：输出到 Mesh 对象（传统方式）
//   - GenerateSpriteShape()：直接输出到 SpriteShapeRenderer（推荐方式）
//
// 📍 对应 C++ 头文件：Modules/SpriteShape/Public/SpriteShapeUtility.h
// ==============================================================

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.U2D
{
    // ==============================================================
    // SpriteShapeMetaData — 控制点元数据
    // ==============================================================
    // 🎯 为每个控制点附加渲染参数
    //
    // 📌 字段：
    //   height：该段的地形高度
    //   bevelCutoff/bevelSize：斜面效果参数
    //   spriteIndex：使用的 Sprite 索引
    //   corner：是否为角点（触发角点 Sprite 切换）
    // ==============================================================
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public struct SpriteShapeMetaData
    {
        public float height;
        public float bevelCutoff;
        public float bevelSize;
        public uint spriteIndex;
        public bool corner;
    }

    // ==============================================================
    // ShapeControlPoint — 样条控制点
    // ==============================================================
    // 🎯 定义 SpriteShape 样条曲线的控制点
    //
    // 📌 字段：
    //   position：控制点的 3D 位置
    //   leftTangent：左侧切线方向（控制曲线弯曲）
    //   rightTangent：右侧切线方向
    //   mode：插值模式（线性/平滑等）
    //
    // 💡 贝塞尔曲线：
    //   通过 leftTangent 和 rightTangent 可以精确控制
    //   控制点两侧的曲线形状，实现平滑的地形轮廓。
    // ==============================================================
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public struct ShapeControlPoint
    {
        public Vector3 position;
        public Vector3 leftTangent;
        public Vector3 rightTangent;
        public int mode;
    }

    // ==============================================================
    // AngleRangeInfo — 角度范围配置
    // ==============================================================
    // 🎯 定义不同角度范围对应的 Sprite 映射
    //
    // 📌 字段：
    //   start/end：角度范围（度），如 150°~210°
    //   order：优先级（范围重叠时使用高优先级的 Sprite）
    //   sprites：该角度范围对应的 Sprite 索引数组
    //
    // 💡 角度切换逻辑：
    //   当样条曲线在某处的外角（Exterior Angle）落在 [start, end] 范围内，
    //   系统从 sprites 数组中选取对应 Sprite 渲染该段。
    //   多个范围重叠时，order 值大的优先。
    // ==============================================================
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public struct AngleRangeInfo
    {
        public float start;
        public float end;
        public uint order;
        public int[] sprites;
    }

    // ==============================================================
    // SpriteShapeUtility — 2D 形状网格生成引擎
    // ==============================================================
    // 🎯 核心生成方法：
    //   Generate()：输出到 Mesh（传统方式，可用于自定义渲染）
    //   GenerateSpriteShape()：直接输出到 SpriteShapeRenderer（推荐方式）
    //
    // 📌 参数说明：
    //   mesh/renderer：输出目标
    //   shapeParams：形状参数（纹理/缩放/斜面/角度阈值等）
    //   points：控制点数组（定义轮廓形状）
    //   metaData：控制点元数据（高度/角点标志/Sprite 索引）
    //   angleRange：角度范围配置（不同角度使用不同 Sprite）
    //   sprites：边线 Sprite 数组
    //   corners：角点 Sprite 数组
    //
    // 💡 执行流程：
    //   1. 根据控制点生成样条曲线
    //   2. 沿曲线按 splineDetail 采样
    //   3. 根据角度和 AngleRangeInfo 选择 Sprite
    //   4. 生成顶点、UV、索引数据
    //   5. 输出到 Mesh 或 SpriteShapeRenderer
    // ==============================================================
    [NativeHeader("Modules/SpriteShape/Public/SpriteShapeUtility.h")]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public class SpriteShapeUtility
    {
        [FreeFunction("SpriteShapeUtility::Generate", ThrowsException = true)]
        extern public static int[] Generate(Mesh mesh, SpriteShapeParameters shapeParams, ShapeControlPoint[] points, SpriteShapeMetaData[] metaData, AngleRangeInfo[] angleRange, Sprite[] sprites, Sprite[] corners);
        [FreeFunction("SpriteShapeUtility::GenerateSpriteShape", ThrowsException = true)]
        extern public static void GenerateSpriteShape(SpriteShapeRenderer renderer, SpriteShapeParameters shapeParams, ShapeControlPoint[] points, SpriteShapeMetaData[] metaData, AngleRangeInfo[] angleRange, Sprite[] sprites, Sprite[] corners);
    }
}
