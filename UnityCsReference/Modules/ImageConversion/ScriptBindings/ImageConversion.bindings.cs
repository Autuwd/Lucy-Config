// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// =================================================================================================
// 🎯 ImageConversion — 纹理编码/解码基础设施
// =================================================================================================
// 提供 Texture2D 扩展方法，支持 PNG / JPG / TGA / EXR 格式的相互转换。
//
// 📌 编码（Encode）路径
//   - EncodeToPNG / EncodeToJPG / EncodeToTGA / EncodeToEXR: 将 Texture2D → byte[]
//   - EncodeNativeArrayToXXX<T>: 将 NativeArray<T> 像素数据直接编码（零拷贝路径）
//   - EncodeArrayToXXX: 托管 Array 重载（兼容旧接口）
//   - 内部通过调用 C++ ImageConversionBindings 实现各格式压缩
//
// 📌 解码（LoadImage）路径
//   - LoadImage(this Texture2D, ReadOnlySpan<byte>): 从 byte[] 解码到纹理（新式 API）
//   - 支持 LoadImage(byte[]) 向后兼容重载
//   - LoadImageDataAtPath: 从磁盘路径直接加载到 NativeArray（内部使用）
//
// 💡 性能优化设计
//   - UnsafeXxx 内部 API 使用 void* 指针直接操作内存，避免托管数组分配
//   - EncodeNativeArray 返回 new NativeArray<byte>(Allocator.Persistent) 需手动释放
//   - 每个 EncodeNativeArray 都创建独立 AtomicSafetyHandle 保证线程安全
//
// ⚠️ 注意事项
//   - JPG 默认 quality=75，EXR 支持 HDR 的 EXRFlags 控制
//   - EncodeNativeArrayXXX 返回的 NativeArray 由调用方负责 Dispose
//   - TGA 格式在 Unity 2020+ 引入，主要用于编辑器资源管线
// =================================================================================================

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;

namespace UnityEngine
{
    [NativeHeader("Modules/ImageConversion/ScriptBindings/ImageConversion.bindings.h")]
    public static class ImageConversion
    {
        public static bool EnableLegacyPngGammaRuntimeLoadBehavior
        {
            get
            {
                return GetEnableLegacyPngGammaRuntimeLoadBehavior();
            }
            set
            {
                SetEnableLegacyPngGammaRuntimeLoadBehavior(value);
            }
        }

        [NativeMethod(Name = "ImageConversionBindings::GetEnableLegacyPngGammaRuntimeLoadBehavior", IsFreeFunction = true, ThrowsException = false)]
        extern private static bool GetEnableLegacyPngGammaRuntimeLoadBehavior();

        [NativeMethod(Name = "ImageConversionBindings::SetEnableLegacyPngGammaRuntimeLoadBehavior", IsFreeFunction = true, ThrowsException = false)]
        extern private static void SetEnableLegacyPngGammaRuntimeLoadBehavior(bool enable);

        [NativeMethod(Name = "ImageConversionBindings::EncodeToTGA", IsFreeFunction = true, ThrowsException = true)]
        extern public static byte[] EncodeToTGA(this Texture2D tex);

        [NativeMethod(Name = "ImageConversionBindings::EncodeToPNG", IsFreeFunction = true, ThrowsException = true)]
        extern public static byte[] EncodeToPNG(this Texture2D tex);

        [NativeMethod(Name = "ImageConversionBindings::EncodeToJPG", IsFreeFunction = true, ThrowsException = true)]
        extern public static byte[] EncodeToJPG(this Texture2D tex, int quality);
        public static byte[] EncodeToJPG(this Texture2D tex)
        {
            return tex.EncodeToJPG(75);
        }

        [NativeMethod(Name = "ImageConversionBindings::EncodeToEXR", IsFreeFunction = true, ThrowsException = true)]
        extern public static byte[] EncodeToEXR(this Texture2D tex, Texture2D.EXRFlags flags);
        public static byte[] EncodeToEXR(this Texture2D tex)
        {
            return EncodeToEXR(tex, Texture2D.EXRFlags.None);
        }

        [NativeMethod(Name = "ImageConversionBindings::EncodeToR2D", IsFreeFunction = true, ThrowsException = true)]
        extern internal static byte[] EncodeToR2DInternal(this Texture2D tex);

        [NativeMethod(Name = "ImageConversionBindings::LoadImage", IsFreeFunction = true)]
        extern public static bool LoadImage([NotNull] this Texture2D tex, ReadOnlySpan<byte> data, bool markNonReadable);
        public static bool LoadImage(this Texture2D tex, ReadOnlySpan<byte> data) => tex.LoadImage(data, false);
        public static bool LoadImage(this Texture2D tex, byte[] data, bool markNonReadable) => tex.LoadImage(new ReadOnlySpan<byte>(data), markNonReadable);
        public static bool LoadImage(this Texture2D tex, byte[] data) => tex.LoadImage(new ReadOnlySpan<byte>(data), false);

        [FreeFunctionAttribute("ImageConversionBindings::EncodeArrayToTGA", true)]
        extern internal static byte[] EncodeArrayToTGA_Internal(Span<byte> span, GraphicsFormat format, uint width, uint height, uint rowBytes = 0);
        public static byte[] EncodeArrayToTGA(Array array, GraphicsFormat format, uint width, uint height, uint rowBytes = 0)
        {
            var elemSize = UnsafeUtility.SizeOf(array.GetType().GetElementType());
            int dataLen = array.Length;
            return EncodeArrayToTGA_Internal(UnsafeUtility.GetByteSpanFromArray(array, dataLen, elemSize), format, width, height, rowBytes);
        }

        [FreeFunctionAttribute("ImageConversionBindings::EncodeArrayToPNG", true)]
        extern internal static byte[] EncodeArrayToPNG_Internal(Span<byte> span, GraphicsFormat format, uint width, uint height, uint rowBytes = 0);
        public static byte[] EncodeArrayToPNG(Array array, GraphicsFormat format, uint width, uint height, uint rowBytes = 0)
        {
            var elemSize = UnsafeUtility.SizeOf(array.GetType().GetElementType());
            int dataLen = array.Length;
            return EncodeArrayToPNG_Internal(UnsafeUtility.GetByteSpanFromArray(array, dataLen, elemSize), format, width, height, rowBytes);
        }

        [FreeFunctionAttribute("ImageConversionBindings::EncodeArrayToJPG", true)]
        extern internal static byte[] EncodeArrayToJPG_Internal(Span<byte> span, GraphicsFormat format, uint width, uint height, uint rowBytes = 0, int quality = 75);
        public static byte[] EncodeArrayToJPG(Array array, GraphicsFormat format, uint width, uint height, uint rowBytes = 0, int quality = 75)
        {
            var elemSize = UnsafeUtility.SizeOf(array.GetType().GetElementType());
            int dataLen = array.Length;
            return EncodeArrayToJPG_Internal(UnsafeUtility.GetByteSpanFromArray(array, dataLen, elemSize), format, width, height, rowBytes, quality);
        }

        [FreeFunctionAttribute("ImageConversionBindings::EncodeArrayToEXR", true)]
        extern internal static byte[] EncodeArrayToEXR_Internal(Span<byte> span, GraphicsFormat format, uint width, uint height, uint rowBytes = 0, Texture2D.EXRFlags flags = Texture2D.EXRFlags.None);
        public static byte[] EncodeArrayToEXR(Array array, GraphicsFormat format, uint width, uint height, uint rowBytes = 0, Texture2D.EXRFlags flags = Texture2D.EXRFlags.None)
        {
            var elemSize = UnsafeUtility.SizeOf(array.GetType().GetElementType());
            int dataLen = array.Length;
            return EncodeArrayToEXR_Internal(UnsafeUtility.GetByteSpanFromArray(array, dataLen, elemSize), format, width, height, rowBytes, flags);
        }

        [FreeFunctionAttribute("ImageConversionBindings::EncodeArrayToR2D", true)]
        extern internal static byte[] EncodeArrayToR2DInternal(Span<byte> span, GraphicsFormat format, uint width, uint height, uint rowBytes = 0);

        public static NativeArray<byte> EncodeNativeArrayToTGA<T>(NativeArray<T> input, GraphicsFormat format, uint width, uint height, uint rowBytes = 0) where T : struct
        {
            unsafe
            {
                var size   = input.Length * UnsafeUtility.SizeOf<T>();
                var result = UnsafeEncodeNativeArrayToTGA(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<T>(input), ref size, format, width, height, rowBytes);
                var output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(result, size, Allocator.Persistent);
                var safety = AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, safety);
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(safety, true);
                return output;
            }
        }

        public static NativeArray<byte> EncodeNativeArrayToPNG<T>(NativeArray<T> input, GraphicsFormat format, uint width, uint height, uint rowBytes = 0) where T : struct
        {
            unsafe
            {
                var size   = input.Length * UnsafeUtility.SizeOf<T>();
                var result = UnsafeEncodeNativeArrayToPNG(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<T>(input), ref size, format, width, height, rowBytes);
                var output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(result, size, Allocator.Persistent);
                var safety = AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, safety);
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(safety, true);
                return output;
            }
        }

        public static NativeArray<byte> EncodeNativeArrayToJPG<T>(NativeArray<T> input, GraphicsFormat format, uint width, uint height, uint rowBytes = 0, int quality = 75) where T : struct
        {
            unsafe
            {
                var size   = input.Length * UnsafeUtility.SizeOf<T>();
                var result = UnsafeEncodeNativeArrayToJPG(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<T>(input), ref size, format, width, height, rowBytes, quality);
                var output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(result, size, Allocator.Persistent);
                var safety = AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, safety);
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(safety, true);
                return output;
            }
        }

        public static NativeArray<byte> EncodeNativeArrayToEXR<T>(NativeArray<T> input, GraphicsFormat format, uint width, uint height, uint rowBytes = 0, Texture2D.EXRFlags flags = Texture2D.EXRFlags.None) where T : struct
        {
            unsafe
            {
                var size   = input.Length * UnsafeUtility.SizeOf<T>();
                var result = UnsafeEncodeNativeArrayToEXR(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<T>(input), ref size, format, width, height, rowBytes, flags);
                var output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(result, size, Allocator.Persistent);
                var safety = AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, safety);
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(safety, true);
                return output;
            }
        }

        internal static NativeArray<byte> EncodeNativeArrayToR2DInternal<T>(NativeArray<T> input, GraphicsFormat format, uint width, uint height, uint rowBytes = 0) where T : struct
        {
            unsafe
            {
                var size   = input.Length * UnsafeUtility.SizeOf<T>();
                var result = UnsafeEncodeNativeArrayToR2D(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<T>(input), ref size, format, width, height, rowBytes);
                var output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(result, size, Allocator.Persistent);
                var safety = AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, safety);
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(safety, true);
                return output;
            }
        }

        [FreeFunctionAttribute("ImageConversionBindings::UnsafeEncodeNativeArrayToTGA", true)]
        unsafe extern static void* UnsafeEncodeNativeArrayToTGA(void* array, ref int sizeInBytes, GraphicsFormat format, uint width, uint height, uint rowBytes = 0);

        [FreeFunctionAttribute("ImageConversionBindings::UnsafeEncodeNativeArrayToPNG", true)]
        unsafe extern static void* UnsafeEncodeNativeArrayToPNG(void* array, ref int sizeInBytes, GraphicsFormat format, uint width, uint height, uint rowBytes = 0);

        [FreeFunctionAttribute("ImageConversionBindings::UnsafeEncodeNativeArrayToJPG", true)]
        unsafe extern static void* UnsafeEncodeNativeArrayToJPG(void* array, ref int sizeInBytes, GraphicsFormat format, uint width, uint height, uint rowBytes = 0, int quality = 75);

        [FreeFunctionAttribute("ImageConversionBindings::UnsafeEncodeNativeArrayToEXR", true)]
        unsafe extern static void* UnsafeEncodeNativeArrayToEXR(void* array, ref int sizeInBytes, GraphicsFormat format, uint width, uint height, uint rowBytes = 0, Texture2D.EXRFlags flags = Texture2D.EXRFlags.None);

        [FreeFunctionAttribute("ImageConversionBindings::UnsafeEncodeNativeArrayToR2D", true)]
        unsafe extern static void* UnsafeEncodeNativeArrayToR2D(void* array, ref int sizeInBytes, GraphicsFormat format, uint width, uint height, uint rowBytes = 0);

        [NativeMethod(Name = "ImageConversionBindings::LoadImageAtPathInternal", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        unsafe extern static void* LoadImageAtPathInternal(string path, ref int width, ref int height, ref int rowBytes, ref GraphicsFormat format);
        unsafe internal static NativeArray<byte> LoadImageDataAtPath(string path, ref int width, ref int height, ref int rowBytes, ref GraphicsFormat format)
        {
            var buffer = LoadImageAtPathInternal(path, ref width, ref height, ref rowBytes, ref format);
            var size = height * rowBytes;
            var output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(buffer, size, Allocator.Persistent);
            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, safety);
            AtomicSafetyHandle.SetAllowReadOrWriteAccess(safety, true);
            return output;
        }

    }
}
