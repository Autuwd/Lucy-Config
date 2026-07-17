// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// Clipper2D — 2D 多边形裁剪运算库
//
// 🎯 核心作用：
//   Clipper2D 封装了 Clipper 库，提供 2D 多边形的布尔运算能力。
//   可以对多条路径（Path）执行交集、并集、差集、异或操作。
//   主要用于 2D 游戏中的碰撞形状生成、地形裁剪等。
//
// 📌 四种裁剪类型（ClipType）：
//   1. ctIntersection（交集）：仅保留重叠区域
//   2. ctUnion（并集）：合并所有区域
//   3. ctDifference（差集）：从主体中减去裁剪区域
//   4. ctXor（异或）：保留非重叠区域
//
// 💡 关键概念：
//   - Subject（主体路径）：被操作的原始多边形
//   - Clip（裁剪路径）：用于切割的多边形
//   - PolyFillType：填充规则（EvenOdd/NonZero/Positive/Negative）
//     · EvenOdd：奇偶规则，嵌套区域交替填充
//     · NonZero：非零环绕数规则，更常用
//   - intScale：整数缩放因子（默认 65536），将浮点坐标转为整数提高精度
//   - useRounding：启用四舍五入，避免浮点误差导致的退化三角形
//
// ⚡ 使用流程：
//   1. 准备 NativeArray<Vector2> 的点和路径大小
//   2. 配置 PathArguments（polyType + closed）和 ExecuteArguments
//   3. 调用 Execute() 执行裁剪
//   4. 从 Solution 中获取结果点和路径大小
//
// 📍 对应 C++ 头文件：Runtime/2D/Common/ClipperWrapper.h
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
    [NativeHeader("Runtime/2D/Common/ClipperWrapper.h")]
    // ==============================================================
    // Clipper2D — 多边形裁剪核心结构体
    // ==============================================================
    // 🎯 封装 Clipper 库的 C# 接口，支持多路径布尔运算
    //
    // 📌 枚举类型：
    //   ClipType：裁剪操作类型（交集/并集/差集/异或）
    //   PolyType：路径角色（Subject 主体 / Clip 裁剪器）
    //   PolyFillType：填充规则（EvenOdd/NonZero/Positive/Negative）
    //   InitOptions：初始化选项（反向解/严格简单/保持共线）
    //
    // 📌 核心结构：
    //   PathArguments：路径参数（polyType + 是否闭合）
    //   ExecuteArguments：执行参数（裁剪类型 + 填充规则 + 初始化选项）
    //   Solution：结果容器（points 点数组 + pathSizes 路径大小 + boundingRect）
    //
    // 💡 Execute() 方法：
    //   输入多条路径的点和参数，输出裁剪后的结果。
    //   内部使用整数坐标（intScale = 65536）避免浮点精度问题。
    //   Solution 的 NativeArray 可预分配复用，减少内存分配。
    // ==============================================================
    internal struct Clipper2D
    {
        public enum ClipType { ctIntersection, ctUnion, ctDifference, ctXor };
        public enum PolyType { ptSubject, ptClip };
        public enum PolyFillType { pftEvenOdd, pftNonZero, pftPositive, pftNegative };
        public enum InitOptions { ioDefault = 0, oReverseSolution = 1, ioStrictlySimple = 2, ioPreserveCollinear = 4 };

        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader("Runtime/2D/Common/ClipperWrapper.h")]
        public struct PathArguments
        {
            // All members should be valid when their value is 0
            public PolyType polyType;
            public bool closed;

            // Default are set to the 0 enum value for continuity with default constructor
            public PathArguments(PolyType inPolyType = PolyType.ptSubject, bool inClosed = false)
            {
                polyType = inPolyType;
                closed = inClosed;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader("Runtime/2D/Common/ClipperWrapper.h")]
        public struct ExecuteArguments
        {
            // All members should be valid when their value is 0
            public InitOptions initOption;
            public ClipType clipType;
            public PolyFillType subjFillType;
            public PolyFillType clipFillType;
            public bool reverseSolution;
            public bool strictlySimple;
            public bool preserveColinear;


            // Default are set to the 0 enum value for continuity with default constructor
            public ExecuteArguments(InitOptions inInitOption = InitOptions.ioDefault, ClipType inClipType = ClipType.ctIntersection, PolyFillType inSubjFillType = PolyFillType.pftEvenOdd, PolyFillType inClipFillType = PolyFillType.pftEvenOdd, bool inReverseSolution = false, bool inStrictlySimple = false, bool inPreserveColinear = false)
            {
                initOption = inInitOption;
                clipType = inClipType;
                subjFillType = inSubjFillType;
                clipFillType = inClipFillType;
                reverseSolution = inReverseSolution;
                strictlySimple = inStrictlySimple;
                preserveColinear = inPreserveColinear;
            }
        }

        public struct Solution : IDisposable
        {
            public NativeArray<Vector2> points;
            public NativeArray<int> pathSizes;
            public NativeArray<Rect> boundingRect;  // Only the first array element is valid (index 0)

            // This is an optional constructor when using execute. When using this constructor the allocated buffers will be used when calling execute. 
            public Solution(int pointsBufferSize, int pathSizesBufferSize, Allocator allocator)
            {
                points = new NativeArray<Vector2>(pointsBufferSize, allocator, NativeArrayOptions.ClearMemory);
                pathSizes = new NativeArray<int>(pathSizesBufferSize, allocator, NativeArrayOptions.ClearMemory);
                boundingRect = new NativeArray<Rect>(1, allocator);
            }

            public void Dispose()
            {
                if (points.IsCreated)
                    points.Dispose();
                if (pathSizes.IsCreated)
                    pathSizes.Dispose();
                if (boundingRect.IsCreated)
                    boundingRect.Dispose();
            }
        }


        // If solution has uncreated NativeArrays, they will be automatically created to fit the solution. Otherwise the existing arrays will be used.
        public static void Execute(ref Solution solution, NativeArray<Vector2> inPoints, NativeArray<int> inPathSizes, NativeArray<PathArguments> inPathArguments, ExecuteArguments inExecuteArguments, Allocator inSolutionAllocator, int inIntScale = 65536, bool useRounding = false)
        {
            IntPtr clipperPoints;
            IntPtr clipperPathSizes;
            int clipperPointCount;
            int clipperPathCount;

            unsafe
            {
                if (!solution.boundingRect.IsCreated)
                    solution.boundingRect = new NativeArray<Rect>(1, inSolutionAllocator);

                solution.boundingRect[0] = Internal_Execute(out clipperPoints, out clipperPointCount, out clipperPathSizes, out clipperPathCount, new IntPtr(inPoints.m_Buffer), inPoints.Length, new IntPtr(inPathSizes.m_Buffer), new IntPtr(inPathArguments.m_Buffer), inPathSizes.Length, inExecuteArguments, inIntScale, useRounding);
                if(clipperPointCount > 0)
                {
                    if(!solution.pathSizes.IsCreated)
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
                else
                {
                    if (!solution.pathSizes.IsCreated)
                        solution.points = new NativeArray<Vector2>(0, inSolutionAllocator);
                    if (!solution.points.IsCreated)
                        solution.pathSizes = new NativeArray<int>(0, inSolutionAllocator);
                }
            }
        }


        //---------------------------------
        // Extern Functions
        //---------------------------------
        [NativeMethod(Name = "Clipper2D::Execute", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static unsafe Rect Internal_Execute(out IntPtr outClippedPoints, out int outClippedPointsCount, out IntPtr outClippedPathSizes, out int outClippedPathCount, IntPtr inPoints, int inPointCount, IntPtr inPathSizes, IntPtr inPathArguments, int inPathCount, ExecuteArguments inExecuteArguments, float inIntScale, bool useRounding);

        [NativeMethod(Name = "Clipper2D::Execute_Cleanup", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static unsafe void Internal_Execute_Cleanup(IntPtr inPoints, IntPtr inPathSizes);
    }
}
