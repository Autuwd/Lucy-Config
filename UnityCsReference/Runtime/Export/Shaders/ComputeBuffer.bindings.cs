// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ComputeBuffer — GPU 通用计算缓冲区
//
// 📌 作用：
//   在 GPU 上分配一块可读/写的缓冲区，供 Compute Shader 使用。
//   Unity 的 ComputeBuffer 底层由 C++ GraphicsBuffer 实现。
//
// 💡 核心概念：
//   count × stride = 总字节数
//   count  = 元素数量
//   stride = 每个元素的字节大小
//
// 🎯 UAV (Unordered Access View)：
//   ComputeBuffer 默认是 UAV，Compute Shader 可随机读写。
//   对应 HLSL 中的 RWStructuredBuffer / RWByteAddressBuffer。
//
// 🎯 SRV (Shader Resource View)：
//   也可作为 SRV 绑定到 Graphics Shader（非 Compute），
//   此时为只读，对应 HLSL 中的 StructuredBuffer / ByteAddressBuffer。
//
// ⚡ CopyCount：
//   将 Append/Consume 缓冲区的隐藏计数器值拷贝到另一个缓冲区。
//   常用于间接绘制：先用 Compute Shader 生成数据，
//   再用 CopyCount 将元素数量写入 args buffer，
//   最后用 Graphics.DrawProceduralIndirect 绘制。
//
// 💡 SetData 要求 blittable 类型（内存布局连续），
//   支持 Array、List<T>、NativeArray<T> 三种数据源。
// ==============================================================

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Note: both C# ComputeBuffer and GraphicsBuffer
    // use C++ GraphicsBuffer as an implementation object.
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/GraphicsBuffer.h")]
    [NativeHeader("Runtime/Export/Graphics/GraphicsBuffer.bindings.h")]
    [NativeClass("GraphicsBuffer")]
    public sealed class ComputeBuffer : IDisposable
    {
#pragma warning disable 414
        internal IntPtr m_Ptr;
#pragma warning restore 414

        AtomicSafetyHandle m_Safety;

        ~ComputeBuffer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release native resources
                DestroyBuffer(this);

                RemoveBufferFromLeakDetector();
            }
            else if (m_Ptr != IntPtr.Zero)
            {
                // We cannot call DestroyBuffer through GC - it is scripting_api and requires main thread, prefer leak instead of a crash
                if (UnsafeUtility.GetLeakDetectionMode() == NativeLeakDetectionMode.Disabled)
                    Debug.LogWarning("GarbageCollector disposing of ComputeBuffer. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer. To see the stack trace where the leaked resource was allocated, set the UnsafeUtility LeakDetectionMode to EnabledWithStackTrace.");
            }

            m_Ptr = IntPtr.Zero;
        }

        [FreeFunction("GraphicsBuffer_Bindings::InitComputeBuffer")]
        static extern IntPtr InitBuffer(int count, int stride, ComputeBufferType type, ComputeBufferMode usage);

        [FreeFunction("GraphicsBuffer_Bindings::DestroyComputeBuffer")]
        static extern void DestroyBuffer(ComputeBuffer buf);

        public ComputeBuffer(int count, int stride) : this(count, stride, ComputeBufferType.Default, ComputeBufferMode.Immutable, 3)
        {
        }

        public ComputeBuffer(int count, int stride, ComputeBufferType type) : this(count, stride, type, ComputeBufferMode.Immutable, 3)
        {
        }

        public ComputeBuffer(int count, int stride, ComputeBufferType type, ComputeBufferMode usage) : this(count, stride, type, usage, 3)
        {
        }

        ComputeBuffer(int count, int stride, ComputeBufferType type, ComputeBufferMode usage, int stackDepth)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Attempting to create a zero length compute buffer", "count");
            }

            if (stride <= 0)
            {
                throw new ArgumentException("Attempting to create a compute buffer with a negative or null stride", "stride");
            }

            var bufferSize = (long)count * stride;
            var maxBufferSize = SystemInfo.maxGraphicsBufferSize;
            if (bufferSize > maxBufferSize)
            {
                throw new ArgumentException($"The total size of the compute buffer ({bufferSize} bytes) exceeds the maximum buffer size. Maximum supported buffer size: {maxBufferSize} bytes.");
            }

            m_Ptr = InitBuffer(count, stride, type, usage);

            AddBufferToLeakDetector();
        }

        public void Release()
        {
            Dispose();
        }

        [FreeFunction("GraphicsBuffer_Bindings::IsValidBuffer")]
        static extern bool IsValidBuffer(ComputeBuffer buf);

        public bool IsValid()
        {
            return m_Ptr != IntPtr.Zero && IsValidBuffer(this);
        }

        // ==============================================================
        // count / stride — 缓冲区的元素数量和单个元素大小
        //
        // 📌 count  = 元素数量（如 1024 个 float4）
        // 📌 stride = 每个元素的字节大小（如 16 字节）
        //    总 GPU 内存 = count × stride
        // ==============================================================
        extern public int count { get; }

        extern public int stride { get; }

        extern private ComputeBufferMode usage { get; }

        // Set buffer data.
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData(System.Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to ComputeBuffer.SetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            var elemSize = UnsafeUtility.SizeOf(data.GetType().GetElementType());
            int dataLen = data.Length;
            InternalSetData(UnsafeUtility.GetByteSpanFromArray(data, dataLen, elemSize), 0, 0, dataLen, elemSize);
        }

        // Set buffer data.
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData<T>(List<T> data) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsGenericListBlittable<T>())
            {
                throw new ArgumentException(
                    string.Format("List<{0}> passed to ComputeBuffer.SetData(List<>) must be blittable.\n{1}",
                        typeof(T), UnsafeUtility.GetReasonForGenericListNonBlittable<T>()));
            }

            InternalSetData(UnsafeUtility.GetByteSpanFromList(data), 0, 0, NoAllocHelpers.SafeLength(data), UnsafeUtility.SizeOf<T>());
        }

        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        unsafe public void SetData<T>(NativeArray<T> data) where T : struct
        {
            // Note: no IsBlittable test here because it's already done at NativeArray creation time
            InternalSetNativeData((IntPtr)data.GetUnsafeReadOnlyPtr(), 0, 0, data.Length, UnsafeUtility.SizeOf<T>());
        }

        // Set partial buffer data
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to ComputeBuffer.SetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            var elemSize = UnsafeUtility.SizeOf(data.GetType().GetElementType());
            int dataLen = data.Length;
            InternalSetData(UnsafeUtility.GetByteSpanFromArray(data, dataLen, elemSize), managedBufferStartIndex, computeBufferStartIndex, count, elemSize);
        }

        // Set partial buffer data
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData<T>(List<T> data, int managedBufferStartIndex, int computeBufferStartIndex, int count) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsGenericListBlittable<T>())
            {
                throw new ArgumentException(
                    string.Format("List<{0}> passed to ComputeBuffer.SetData(List<>) must be blittable.\n{1}",
                        typeof(T), UnsafeUtility.GetReasonForGenericListNonBlittable<T>()));
            }

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Count)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalSetData(UnsafeUtility.GetByteSpanFromList(data), managedBufferStartIndex, computeBufferStartIndex, count, UnsafeUtility.SizeOf<T>());
        }

        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public unsafe void SetData<T>(NativeArray<T> data, int nativeBufferStartIndex, int computeBufferStartIndex, int count) where T : struct
        {
            // Note: no IsBlittable test here because it's already done at NativeArray creation time
            if (nativeBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || nativeBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (nativeBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", nativeBufferStartIndex, computeBufferStartIndex, count));

            InternalSetNativeData((IntPtr)data.GetUnsafeReadOnlyPtr(), nativeBufferStartIndex, computeBufferStartIndex, count, UnsafeUtility.SizeOf<T>());
        }

        [FreeFunction(Name = "GraphicsBuffer_Bindings::InternalSetNativeData", HasExplicitThis = true, ThrowsException = true)]
        extern void InternalSetNativeData(IntPtr data, int nativeBufferStartIndex, int computeBufferStartIndex, int count, int elemSize);

        [FreeFunction(Name = "GraphicsBuffer_Bindings::InternalSetData", HasExplicitThis = true, ThrowsException = true)]
        extern void InternalSetData(Span<byte> data, int managedBufferStartIndex, int computeBufferStartIndex, int count, int elemSize);

        // Read buffer data.
        [System.Security.SecurityCritical] // due to Marshal.SizeOf
        public void GetData(Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to ComputeBuffer.GetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            var elemSize = UnsafeUtility.SizeOf(data.GetType().GetElementType());
            int dataLen = data.Length;
            InternalGetData(UnsafeUtility.GetByteSpanFromArray(data, dataLen, elemSize), 0, 0, dataLen, elemSize);
        }

        // Read partial buffer data.
        [System.Security.SecurityCritical] // due to Marshal.SizeOf
        public void GetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to ComputeBuffer.GetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count argument (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            var elemSize = UnsafeUtility.SizeOf(data.GetType().GetElementType());
            int dataLen = data.Length;
            InternalGetData(UnsafeUtility.GetByteSpanFromArray(data, dataLen, elemSize), managedBufferStartIndex, computeBufferStartIndex, count, elemSize);
        }

        [FreeFunction(Name = "GraphicsBuffer_Bindings::InternalGetData", HasExplicitThis = true, ThrowsException = true)]
        extern void InternalGetData(Span<byte> data, int managedBufferStartIndex, int computeBufferStartIndex, int count, int elemSize);

        extern unsafe private void* BeginBufferWrite(int offset = 0, int size = 0);

        public NativeArray<T> BeginWrite<T>(int computeBufferStartIndex, int count) where T : struct
        {
            if (!IsValid())
                throw new InvalidOperationException("BeginWrite requires a valid ComputeBuffer");

            if (usage != ComputeBufferMode.SubUpdates)
                throw new ArgumentException("ComputeBuffer must be created with usage mode ComputeBufferMode.SubUpdates to be able to be mapped with BeginWrite");

            var elementSize = UnsafeUtility.SizeOf<T>();
            if (computeBufferStartIndex < 0 || count < 0 || (computeBufferStartIndex + count) * elementSize > this.count * this.stride)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (computeBufferStartIndex:{0} count:{1} elementSize:{2}, this.count:{3}, this.stride{4})", computeBufferStartIndex, count, elementSize, this.count, this.stride));

            NativeArray<T> array;
            unsafe
            {
                var ptr = BeginBufferWrite(computeBufferStartIndex * elementSize, count * elementSize);
                array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)ptr, count, Allocator.Invalid);
            }
            m_Safety = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetAllowSecondaryVersionWriting(m_Safety, true);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
            return array;
        }

        extern private void EndBufferWrite(int bytesWritten = 0);

        public void EndWrite<T>(int countWritten) where T : struct
        {
            try
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.Release(m_Safety);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("ComputeBuffer.EndWrite was called without matching ComputeBuffer.BeginWrite", e);
            }
            if (countWritten < 0)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (countWritten:{0})", countWritten));

            var elementSize = UnsafeUtility.SizeOf<T>();
            EndBufferWrite(countWritten * elementSize);
        }

        // ==============================================================
        // GetNativeBufferPtr — 获取指向本机 GraphicsBuffer 的指针
        // 用于与 Native Rendering Plugin 交互（如 Vulkan/Metal 底层 API）
        // ==============================================================
        extern public IntPtr GetNativeBufferPtr();

        [FreeFunction(Name = "GraphicsBuffer_Bindings::SetName", HasExplicitThis = true)]
        extern private void SetName(string name);

        // ==============================================================
        // SetCounterValue / CopyCount — Append/Consume 缓冲区计数器
        //
        // 📌 SetCounterValue：手动设置 Append/Consume 缓冲区的隐藏计数器
        // 📌 CopyCount：将源缓冲区的计数器值拷贝到目标缓冲区
        //    典型用途：GPU 级联 Indirect Draw，避免 CPU 回读
        // ==============================================================
        extern public void SetCounterValue(uint counterValue);

        extern public static void CopyCount(ComputeBuffer src, ComputeBuffer dst, int dstOffsetBytes);

        internal void AddBufferToLeakDetector()
        {
            if (m_Ptr == null)
                return;

            UnsafeUtility.LeakRecord(m_Ptr, LeakCategory.Persistent, 2);
        }

        internal void RemoveBufferFromLeakDetector()
        {
            if (m_Ptr == null)
                return;

            UnsafeUtility.LeakErase(m_Ptr, LeakCategory.Persistent);
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(ComputeBuffer computeBuffer) => computeBuffer.m_Ptr;
        }
    }
}
