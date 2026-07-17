// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// ClipperOffset2D — 2D 多边形偏移/膨胀运算库
//
// 🎯 核心作用：
//   ClipperOffset2D 对 2D 多边形路径执行偏移操作（向内收缩或向外膨胀），
//   是 Clipper2D 的配套工具。常用于生成碰撞区域的轮廓线、
//   地形边界扩展/收缩等。
//
// 📌 五种端点类型（EndType）：
//   1. etClosedPolygon：闭合多边形（自动首尾相连）
//   2. etClosedLine：闭合折线（首尾相连但不填充）
//   3. etOpenButt：开放端，方头截断
//   4. etOpenSquare：开放端，方头延伸
//   5. etOpenRound：开放端，圆头
//
// 💡 三种连接类型（JoinType）：
//   - jtSquare：方形连接（直角转折）
//   - jtRound：圆形连接（弧形过渡）
//   - jtMiter：尖角连接（尖锐延伸）
//
// ⚡ 关键参数：
//   - delta：偏移量，正值向外膨胀，负值向内收缩
//   - miterLimit：尖角连接的最大延伸限制（防止过长尖角）
//   - roundPrecision：圆弧精度（值越小越平滑但计算量越大）
//   - arcTolerance：弧线容差
//   - intScale：整数缩放因子（默认 65536）
//
// 📍 对应 C++ 头文件：Runtime/2D/Common/ClipperOffsetWrapper.h
// ==============================================================

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;

namespace UnityEngine.U2D
{
    [NativeHeader("Runtime/2D/Common/ClipperOffsetWrapper.h")]
    // ==============================================================
    // ClipperOffset2D — 多边形偏移/膨胀核心结构体
    // ==============================================================
    // 🎯 对 2D 路径执行等距偏移操作（膨胀或收缩）
    //
    // 📌 枚举类型：
    //   JoinType：连接类型
    //     - jtSquare：方形连接（直角）
    //     - jtRound：圆形连接（弧形）
    //     - jtMiter：尖角连接（延伸）
    //   EndType：端点类型
    //     - etClosedPolygon：闭合多边形
    //     - etClosedLine：闭合折线
    //     - etOpenButt/Square/Round：开放端的三种处理
    //
    // 📌 核心结构：
    //   PathArguments：路径参数（joinType + endType）
    //   Solution：结果容器（points + pathSizes）
    //
    // 💡 Execute() 方法：
    //   对输入路径应用偏移操作。delta > 0 向外膨胀，< 0 向内收缩。
    //   inMiterLimit 控制尖角延伸的最大距离。
    //   inRoundPrecision 控制圆弧的细分精度。
    // ==============================================================
    internal struct ClipperOffset2D
    {
        public enum JoinType { jtSquare, jtRound, jtMiter };
        public enum EndType { etClosedPolygon, etClosedLine, etOpenButt, etOpenSquare, etOpenRound };


        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader("Runtime/2D/Common/ClipperOffsetWrapper.h")]
        public struct PathArguments
        {
            // All members should be valid when their value is 0
            public JoinType joinType;
            public EndType endType;

            // Default are set to the 0 enum value for continuity with default constructor
            public PathArguments(JoinType inJoinType = JoinType.jtSquare, EndType inEndType = EndType.etClosedPolygon)
            {
                joinType = inJoinType;
                endType = inEndType;
            }
        }

        public struct Solution
        {
            public NativeArray<Vector2> points;
            public NativeArray<int> pathSizes;

            public Solution(int pointsBufferSize, int pathSizesBufferSize, Allocator allocator)
            {
                points = new NativeArray<Vector2>(pointsBufferSize, allocator, NativeArrayOptions.ClearMemory);
                pathSizes = new NativeArray<int>(pathSizesBufferSize, allocator, NativeArrayOptions.ClearMemory);
            }

            public void Dispose()
            {
                if (points.IsCreated)
                    points.Dispose();
                if (pathSizes.IsCreated)
                    pathSizes.Dispose();
            }
        }

        public static void Execute(ref Solution solution, NativeArray<Vector2> inPoints, NativeArray<int> inPathSizes, NativeArray<PathArguments> inPathArguments, Allocator inSolutionAllocator, double inDelta = 0, double inMiterLimit = 2.0, double inRoundPrecision = 0.25, double inArcTolerance=0.0, double inIntScale = 65536, bool useRounding = false)
        {
            IntPtr clipperPoints;
            IntPtr clipperPathSizes;
            int clipperPointCount;
            int clipperPathCount;

            unsafe
            {
                Internal_Execute(out clipperPoints, out clipperPointCount, out clipperPathSizes, out clipperPathCount, new IntPtr(inPoints.m_Buffer), inPoints.Length, new IntPtr(inPathSizes.m_Buffer), new IntPtr(inPathArguments.m_Buffer), inPathSizes.Length, inDelta, inMiterLimit, inRoundPrecision, inArcTolerance, inIntScale, useRounding);
                if (!solution.pathSizes.IsCreated)
                    solution.pathSizes = new NativeArray<int>(clipperPathCount, inSolutionAllocator);
                if (!solution.points.IsCreated)
                    solution.points = new NativeArray<Vector2>(clipperPointCount, inSolutionAllocator);

                // Check for enough elements
                if (solution.points.Length >= clipperPointCount && solution.pathSizes.Length >= clipperPathCount)
                {
                    UnsafeUtility.MemCpy(solution.points.m_Buffer, clipperPoints.ToPointer(), clipperPointCount * sizeof(Vector2));
                    UnsafeUtility.MemCpy(solution.pathSizes.m_Buffer, clipperPathSizes.ToPointer(), clipperPathCount * sizeof(int));
                    Internal_Execute_Cleanup(clipperPoints, clipperPathSizes);
                }
                else
                {
                    Internal_Execute_Cleanup(clipperPoints, clipperPathSizes);
                    throw new IndexOutOfRangeException();
                }
            }
        }

        //---------------------------------
        // Extern Functions
        //---------------------------------
        [NativeMethod(Name = "ClipperOffset2D::Execute", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static unsafe void Internal_Execute(out IntPtr outClippedPoints, out int outClippedPointsCount, out IntPtr outClippedPathSizes, out int outClippedPathCount, IntPtr inPoints, int inPointCount, IntPtr inPathSizes, IntPtr inPathArguments, int inPathCount, double inDelta, double inMiterLimit, double inRoundPrecision, double inArcTolerance, double inIntScale, bool useRounding);

        [NativeMethod(Name = "ClipperOffset2D::Execute_Cleanup", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static unsafe void Internal_Execute_Cleanup(IntPtr inPoints, IntPtr inPathSizes);
    }
}
