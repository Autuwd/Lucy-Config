// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PhysicsBuffer — 本地内存缓冲区，C# 与 C++ 之间的零拷贝桥梁
//
// 📌 作用：
//   PhysicsBuffer 是一个轻量级的原生内存封装，用于在托管层
//   和非托管引擎之间高效传输批量数据（如查询结果、形状列表）。
//   核心设计是"指针 + 长度 + 分配器"，避免数据拷贝。
//
// 💡 核心机制：
//   - FromNativeArray<T>() / FromSpan<T>()：将 C# 数据转为 buffer
//   - ToNativeArray<T>() / ToSpan<T>()：将 buffer 转为 C# 可读数据
//   - As<T>(index)：按索引读取值类型元素
//   - AsEngineObject<T>(index)：按索引读取引擎对象（通过 EntityId 转换）
//   - Dispose()：释放非托管内存
//
// ⚠️ 安全注意：
//   - ToNativeArray() 返回的 NativeArray 共享同一块内存
//   - 在 NativeArray 或 Span 活跃期间不能调用 Dispose()
//   - PhysicsBufferPair 是两个 buffer 的组合（用于 ChainGeometry 双输出）
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        /// <summary>
        /// Internal buffer used to marshal results efficiently from the native engine.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct PhysicsBuffer : IDisposable
        {
            /// <undoc/>
            public readonly IntPtr buffer => m_Buffer;

            /// <undoc/>
            public readonly int size => m_Size;

            /// <undoc/>
            public readonly Allocator allocator => m_Allocator;

            /// <undoc/>
            public PhysicsBuffer()
            {
                m_Buffer = IntPtr.Zero;
                m_Size = 0;
                m_Allocator = Allocator.None;
            }

            /// <undoc/>
            public PhysicsBuffer(IntPtr buffer, int size, Allocator allocator)
            {
                m_Buffer = buffer;
                m_Size = size;
                m_Allocator = allocator;
            }

            /// <undoc/>
            public static unsafe PhysicsBuffer FromNativeArray<T>(NativeArray<T> nativeArray) where T : struct
            {
                return new PhysicsBuffer((IntPtr)nativeArray.GetUnsafePtr(), nativeArray.Length, Allocator.None);
            }

            /// <undoc/>
            public static unsafe PhysicsBuffer FromSpan<T>(ReadOnlySpan<T> span) where T : unmanaged
            {
                fixed (T* addr = span)
                {
                    return new PhysicsBuffer((IntPtr)addr, span.Length, Allocator.None);
                }
            }

            /// <undoc/>
            public readonly unsafe NativeArray<T> ToNativeArray<T>() where T : struct
            {
                if (m_Size == 0)
                    return new NativeArray<T>();

                var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(dataPointer: m_Buffer.ToPointer(), length: m_Size, allocator: m_Allocator);
                var safetyHandle = (m_Allocator == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, safetyHandle);
                return nativeArray;
            }

            /// <undoc/>
            public readonly unsafe Span<T> ToSpan<T>() where T : struct
            {
                return new Span<T>(m_Buffer.ToPointer(), m_Size);
            }

            /// <undoc/>
            public readonly unsafe ReadOnlySpan<T> ToReadOnlySpan<T>() where T : struct => new ReadOnlySpan<T>(m_Buffer.ToPointer(), m_Size);

            /// <undoc/>
            public unsafe readonly T AsEngineObject<T>(int index) where T : class
            {
                if (index < 0 || index >= size)
                    throw new ArgumentOutOfRangeException("Index argument is invalid.", nameof(index));

                var entityId = UnsafeUtility.ArrayElementAsRef<EntityId>(m_Buffer.ToPointer(), index);
                return Resources.EntityIdIsValid(entityId) ? Resources.EntityIdToObject(entityId) as T : null;
            }

            /// <undoc/>
            public unsafe readonly T As<T>(int index) where T : struct
            {
                if (index < 0 || index >= size)
                    throw new ArgumentOutOfRangeException("Index argument is invalid.", nameof(index));

                return UnsafeUtility.ArrayElementAsRef<T>(m_Buffer.ToPointer(), index);
            }

            /// <summary>
            /// This should NOT be called if a NativeArray or Span are currently active and being accessed otherwise bad things will happen.
            /// Typically, the NativeArray should be disposed of but in other cases, this can be used.
            /// </summary>
            public unsafe void Dispose()
            {
                if (m_Buffer == null || m_Size == 0)
                    return;

                // Free the allocation.
                UnsafeUtility.FreeTracked(m_Buffer.ToPointer(), m_Allocator);
                m_Buffer = IntPtr.Zero;
                m_Size = 0;
                m_Allocator = Allocator.None;
            }

            /// <undoc/>
            public readonly bool isEmpty => m_Size == 0;

            /// <undoc/>
            public readonly bool isValid => !isEmpty;

            /// <undoc/>
            public readonly override string ToString() { return $"size={m_Size}, allocator={m_Allocator}"; }

            #region Internal

            IntPtr m_Buffer;
            int m_Size;
            Allocator m_Allocator;

            #endregion
        }

        /// <summary>
        /// Internal buffer pair used to marshal results efficiently from the native engine.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct PhysicsBufferPair
        {
            #region Internal

            /// <undoc/>
            public readonly PhysicsBuffer buffer1;

            /// <undoc/>
            public readonly PhysicsBuffer buffer2;

            #endregion
        }
    }
}
