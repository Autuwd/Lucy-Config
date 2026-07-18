// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 UploadHandler — UWR 上传处理器基类体系
// ================================================================
//
// 📌 职责
//   管理 HTTP 请求体的编码与发送。每个 UnityWebRequest 可绑定
//   一个 UploadHandler，POST/PUT 请求时必须配置。
//
// 🏗 类层级（Class Hierarchy）
//
//   UploadHandler (abstract base)
//   ├── UploadHandlerRaw     # 原始字节 / NativeArray 上传
//   ├── UploadHandlerFile    # 文件流式上传（磁盘→网络）
//   └── UploadHandlerStream  # Span<byte> 流式写入（internal）
//
// 💡 UploadHandlerRaw — 原始数据上传
//   ⚡ 两个构造分支：
//   1. byte[] 重载 → 内部拷贝为 NativeArray（Allocator.Persistent）
//   2. NativeArray<byte> 重载 → 零拷贝（需 transferOwnership 参数）
//      - transferOwnership=true  : UploadHandler 接管生命周期
//      - transferOwnership=false : 调用者自行管理 NativeArray
//   3. NativeArray<byte>.ReadOnly 重载 → 只读路径，不接管所有权
//
//   ⚠️ 所有构造器内部调用 Create(this, data, length) 传入原生指针
//      C++ 端直接引用内存，避免托管堆拷贝
//
// 💡 UploadHandlerFile — 文件上传
//   📌 直接将磁盘文件内容发送到网络
//   ⚡ 接受文件路径，由 C++ 原生层负责流式读取
//     适合大文件上传（不上托管堆）
//
// 💡 UploadHandlerStream — 流式上传（internal）
//   📌 通过 WriteData(ReadOnlySpan<byte>) 逐块推送数据
//   ⚡ PushData 标记为 IsThreadSafe，可在工作线程调用
//     Close() 关闭写入，Reset() 重置状态
//     适合实时生成数据的场景（如摄像头流）
//
// 📌 与 MultipartFormHelper 配合
//   上传表单数据时，UnityWebRequest.Post() 内部使用
//   MultipartFormDataSection / MultipartFormFileSection 配合
//   UploadHandlerRaw，自动编码为 multipart/form-data 格式。
//
// ⚠️ 生命周期注意事项
//   - UploadHandler 默认随 UnityWebRequest.Dispose() 释放
//   - disposeUploadHandlerOnDispose=false 时可复用处理器
//   - 复用 UploadHandlerRaw 时注意 NativeArray 的释放时机
// ================================================================

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Networking
{
    // ================================================================
    // 📌 UploadHandler — 基类：所有上传处理器的公共接口
    //   管理 data、contentType、progress 的存取
    //   💡 属性通过 internal virtual 方法转发到 C++ 原生端
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandler.h")]
    public class UploadHandler : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        [NativeMethod(IsThreadSafe = true)]
        private extern void ReleaseFromScripting();

        internal UploadHandler() {}

        ~UploadHandler()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                ReleaseFromScripting();
                m_Ptr = IntPtr.Zero;
            }
        }

        public byte[] data
        {
            get
            {
                return GetData();
            }
        }

        public string contentType
        {
            get
            {
                return GetContentType();
            }
            set
            {
                SetContentType(value);
            }
        }

        public float progress
        {
            get
            {
                return GetProgress();
            }
        }

        internal virtual byte[] GetData() { return null; }
        internal virtual string GetContentType() { return InternalGetContentType(); }
        internal virtual void   SetContentType(string newContentType) { InternalSetContentType(newContentType); }
        internal virtual float  GetProgress() { return InternalGetProgress(); }

        [NativeMethod("GetContentType")]
        private extern string InternalGetContentType();

        [NativeMethod("SetContentType")]
        private extern void InternalSetContentType(string newContentType);

        [NativeMethod("GetProgress")]
        private extern float InternalGetProgress();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UploadHandler uploadHandler) => uploadHandler.m_Ptr;
        }
    }

    // ================================================================
    // 📌 UploadHandlerRaw — 原生字节上传处理器
    //   💡 核心能力：将原始字节/NativeArray 作为 HTTP 请求体发送
    //
    //   ⚡ 三种构造方式：
    //   - byte[] → 内部拷贝为 NativeArray(Allocator.Persistent)
    //   - NativeArray<byte>(transferOwnership)
    //     true=接管所有权(C++ 端释放), false=不接管(调用者释放)
    //   - NativeArray<byte>.ReadOnly → 只读路径，不持有所有权
    //
    //   ⚠️ 空数据处理
    //     空数组/未创建的 NativeArray → 不发送请求体（m_Ptr = null）
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandlerRaw.h")]
    public sealed class UploadHandlerRaw : UploadHandler
    {
        NativeArray<byte> m_Payload;

        private static extern unsafe IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] UploadHandlerRaw self, byte* data, int dataLength);

        public UploadHandlerRaw(byte[] data)
            : this((data == null || data.Length == 0) ? new NativeArray<byte>() : new NativeArray<byte>(data, Allocator.Persistent), true)
        {
        }

        public UploadHandlerRaw(NativeArray<byte> data, bool transferOwnership)
        {
            unsafe
            {
                if (!data.IsCreated || data.Length == 0)
                    m_Ptr = Create(this, null, 0);
                else
                {
                    if (transferOwnership)
                        m_Payload = data;
                    m_Ptr = Create(this, (byte*)data.GetUnsafeReadOnlyPtr(), data.Length);
                }
            }
        }

        public UploadHandlerRaw(NativeArray<byte>.ReadOnly data)
        {
            unsafe
            {
                if (!data.IsCreated || data.Length == 0)
                    m_Ptr = Create(this, null, 0);
                else
                {
                    if (data.Length == 0)
                        m_Ptr = Create(this, null, 0);
                    else
                        m_Ptr = Create(this, (byte*)data.GetUnsafeReadOnlyPtr(), data.Length);
                }
            }
        }

        internal override byte[] GetData()
        {
            if (m_Payload.IsCreated)
                return m_Payload.ToArray();
            return null;
        }

        public override void Dispose()
        {
            if (m_Payload.IsCreated)
                m_Payload.Dispose();
            base.Dispose();
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UploadHandlerRaw uploadHandler) => uploadHandler.m_Ptr;
        }
    }

    // ================================================================
    // 📌 UploadHandlerFile — 文件上传处理器
    //   💡 将磁盘文件直接发送为 HTTP 请求体
    //   ⚡ C++ 端负责流式读取文件，不占托管堆内存
    //   🎯 适合大文件上传（如日志上报、截图分享）
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandlerFile.h")]
    public sealed class UploadHandlerFile : UploadHandler
    {
        [NativeMethod(ThrowsException = true)]
        private static extern IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] UploadHandlerFile self, string filePath);

        public UploadHandlerFile(string filePath)
        {
            m_Ptr = Create(this, filePath);
        }
        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UploadHandlerFile uploadHandler) => uploadHandler.m_Ptr;
        }
    }

    // ================================================================
    // 📌 UploadHandlerStream — 流式上传处理器（internal）
    //   💡 通过 WriteData(ReadOnlySpan<byte>) 逐块推送数据
    //   ⚡ PushData 线程安全，可跨线程推送
    //   🎯 适合实时数据流（摄像头、麦克风、程序化生成）
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandlerStream.h")]
    internal sealed class UploadHandlerStream : UploadHandler
    {
        private static extern unsafe IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] UploadHandlerStream self);

        [NativeMethod(IsThreadSafe = true)]
        public extern void Close();

        [NativeMethod(IsThreadSafe = true)]
        private extern void Reset();

        [NativeMethod(IsThreadSafe = true)]
        private extern void PushData(ReadOnlySpan<byte> data);

        public UploadHandlerStream()
        {
            unsafe
            {
                m_Ptr = Create(this);
            }
        }

        public void WriteData(ReadOnlySpan<byte> data) {
            PushData(data);
        }

        internal override byte[] GetData()
        {
            throw new System.NotSupportedException("Raw data access is not supported");
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UploadHandlerStream uploadHandler) => uploadHandler.m_Ptr;
        }
    }
}
