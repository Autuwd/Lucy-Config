// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 DownloadHandler — UWR 下载处理器基类体系
// ================================================================
//
// 📌 职责
//   管理 HTTP 响应数据的接收和解码。每个 UnityWebRequest 可绑定
//   一个 DownloadHandler，决定如何消费响应的字节流。
//
// 🏗 类层级（Class Hierarchy）
//
//   DownloadHandler (abstract base)
//   ├── DownloadHandlerBuffer      # 内存缓冲区（最常用）
//   ├── DownloadHandlerScript      # 自定义脚本处理（预制缓冲区）
//   ├── DownloadHandlerFile        # 直接写磁盘（VFS 流式）
//   ├── DownloadHandlerStream      # Span<byte> 流式读取（internal）
//   ├── DownloadHandlerAssetBundle # AssetBundle 解包
//   ├── DownloadHandlerAudioClip   # 音频解码
//   └── DownloadHandlerTexture     # 纹理解码
//
// 💡 核心设计
//   每个子类可重写以下虚方法来自定义行为：
//   - GetNativeData()  → 返回 NativeArray<byte>（零拷贝路径）
//   - GetData()        → 返回 byte[]（托管数组）
//   - GetText()        → 返回 string（自动检测编码）
//   - ReceiveData()    → 每收到数据块时回调
//   - CompleteContent()→ 下载完成时回调
//   - GetProgress()    → 下载进度报告
//
// ⚡ 数据流路径
//   [C++ 原生层] → InternalGetByteArray → GetNativeData()
//                                         → GetData() / GetText()
//   通过 InternalGetNativeArray() 实现零拷贝共享
//   （仅内部模块可使用，需 VisibleToOtherModules 特性）
//
// ⚠️ 生命周期
//   - 必须在 UnityWebRequest.Dispose() 前或同时释放
//   - disposeDownloadHandlerOnDispose 控制自动释放
//   - 手动调用 Dispose() 释放原生 m_Ptr
// ================================================================

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Networking
{
    // ================================================================
    // 📌 DownloadHandler — 基类：所有下载处理器的公共接口
    //   管理原生指针 m_Ptr、释放逻辑、数据访问
    //   💡 关键方法：GetNativeData()、GetData()、GetText()
    //   ⚡ 子类通过重写这些方法自定义数据消费方式
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandler.h")]
    public class DownloadHandler : IDisposable
    {
        [System.NonSerialized]
        [VisibleToOtherModules]
        internal IntPtr m_Ptr;

        [NativeMethod(IsThreadSafe = true)]
        private extern void ReleaseFromScripting();

        [VisibleToOtherModules]
        internal DownloadHandler()
        {}

        ~DownloadHandler()
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

        public bool isDone { get { return IsDone(); } }

        private extern bool IsDone();

        public string error { get { return GetErrorMsg(); } }

        private extern string GetErrorMsg();

        public NativeArray<byte>.ReadOnly nativeData
        {
            get { return GetNativeData().AsReadOnly(); }
        }

        public byte[] data
        {
            get { return GetData(); }
        }

        public string text
        {
            get { return GetText(); }
        }

        protected virtual NativeArray<byte> GetNativeData() { return default; }

        protected virtual byte[] GetData()
        {
            return InternalGetByteArray(this);
        }

        protected virtual string GetText()
        {
            var nativeData = GetNativeData();
            if (nativeData.IsCreated && nativeData.Length > 0)
                unsafe
                {
                    return new string((sbyte*)nativeData.GetUnsafeReadOnlyPtr(), 0, nativeData.Length, GetTextEncoder());
                }
            return "";
        }

        private Encoding GetTextEncoder()
        {
            // Check for charset type
            string contentType = GetContentType();
            if (!string.IsNullOrEmpty(contentType))
            {
                int charsetKeyIndex = contentType.IndexOf("charset", StringComparison.OrdinalIgnoreCase);
                if (charsetKeyIndex > -1)
                {
                    int charsetValueIndex = contentType.IndexOf('=', charsetKeyIndex);
                    if (charsetValueIndex > -1)
                    {
                        string encoding = contentType.Substring(charsetValueIndex + 1).Trim().Trim(new[] {'\'', '"'}).Trim();
                        int semicolonIndex = encoding.IndexOf(';');
                        if (semicolonIndex > -1)
                            encoding = encoding.Substring(0, semicolonIndex);
                        try
                        {
                            return System.Text.Encoding.GetEncoding(encoding);
                        }
                        catch (ArgumentException e)
                        {
                            Debug.LogWarning(string.Format("Unsupported encoding '{0}': {1}", encoding, e.Message));
                        }
                        catch (NotSupportedException e)
                        {
                            Debug.LogWarning(string.Format("Unsupported encoding '{0}': {1}", encoding, e.Message));
                        }
                    }
                }
            }

            // Use default (utf8)
            return System.Text.Encoding.UTF8;
        }

        private extern string GetContentType();

        // Return true if you processed the data successfully, false otherwise.
        [RequiredByNativeCode]
        protected virtual bool ReceiveData(byte[] data, int dataLength) { return true; }

        [RequiredByNativeCode]
        protected virtual void ReceiveContentLengthHeader(ulong contentLength)
        {
            #pragma warning disable 618
            ReceiveContentLength((int)contentLength);
            #pragma warning restore 0618
        }

        [Obsolete("Use ReceiveContentLengthHeader")]
        protected virtual void ReceiveContentLength(int contentLength) {}

        [RequiredByNativeCode]
        private static void CompleteHeadersStatic(DownloadHandler handler) { handler.CompleteHeaders(); }
        internal virtual void CompleteHeaders() { }

        [RequiredByNativeCode]
        protected virtual void CompleteContent() {}

        [RequiredByNativeCode]
        protected virtual float GetProgress() { return 0.0f; }

        protected static T GetCheckedDownloader<T>(UnityWebRequest www) where T : DownloadHandler
        {
            if (www == null)
                throw new System.NullReferenceException("Cannot get content from a null UnityWebRequest object");
            if (!www.isDone)
                throw new System.InvalidOperationException("Cannot get content from an unfinished UnityWebRequest object");
            if (www.result == UnityWebRequest.Result.ProtocolError)
                throw new System.InvalidOperationException(www.error);
            // Invalid cast exception will be thrown if T is not a correct DLH
            return (T)www.downloadHandler;
        }

        [NativeMethod(ThrowsException = true)]
        [VisibleToOtherModules]
        internal extern static unsafe byte* InternalGetByteArray(DownloadHandler dh, out int length);

        internal static byte[] InternalGetByteArray(DownloadHandler dh)
        {
            var nativeData = dh.GetNativeData();
            if (nativeData.IsCreated)
                return nativeData.ToArray();
            return null;
        }

        [VisibleToOtherModules("UnityEngine.UnityWebRequestAudioModule", "UnityEngine.UnityWebRequestTextureModule")]
        internal static NativeArray<byte> InternalGetNativeArray(DownloadHandler dh, ref NativeArray<byte> nativeArray)
        {
            unsafe
            {
                int length;
                byte* bytes = InternalGetByteArray(dh, out length);
                if (nativeArray.IsCreated)
                {
                    // allow partial data to be accessed, recreate array if changed
                    if (nativeArray.Length == length)
                        return nativeArray;
                    DisposeNativeArray(ref nativeArray);
                }
                CreateNativeArrayForNativeData(ref nativeArray, bytes, length);
                return nativeArray;
            }
        }

        [VisibleToOtherModules("UnityEngine.UnityWebRequestAudioModule", "UnityEngine.UnityWebRequestTextureModule")]
        internal static void DisposeNativeArray(ref NativeArray<byte> data)
        {
            if (!data.IsCreated)
                return;
            var safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(data);
            AtomicSafetyHandle.Release(safety);
            data = default;
        }

        internal static unsafe void CreateNativeArrayForNativeData(ref NativeArray<byte> data, byte* bytes, int length)
        {
            data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(bytes, length, Allocator.Persistent);
            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref data, safety);
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandler handler) => handler.m_Ptr;
        }
    }

    // ================================================================
    // 📌 DownloadHandlerBuffer — 内存缓冲区下载处理器
    //   ⚡ 最常用的 DownloadHandler，将响应数据存储在托管内存中
    //   💡 通过 InternalGetNativeArray() 实现零拷贝共享
    //   🎯 GetContent(www) 快捷方法直接返回 text 字符串
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerBuffer.h")]
    public sealed class DownloadHandlerBuffer : DownloadHandler
    {
        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerBuffer obj);

        private NativeArray<byte> m_NativeData;

        private void InternalCreateBuffer()
        {
            m_Ptr = Create(this);
        }

        public DownloadHandlerBuffer()
        {
            InternalCreateBuffer();
        }

        protected override NativeArray<byte> GetNativeData()
        {
            return InternalGetNativeArray(this, ref m_NativeData);
        }

        public override void Dispose()
        {
            DisposeNativeArray(ref m_NativeData);
            base.Dispose();
        }

        public static string GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerBuffer>(www).text;
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerBuffer handler) => handler.m_Ptr;
        }
    }

    // ================================================================
    // 📌 DownloadHandlerScript — 自定义脚本处理器（高级）
    //   💡 特性：支持预分配缓冲区，避免下载过程中的 GC 分配
    //
    //   ⚡ 两个构造分支：
    //   1. DownloadHandlerScript()
    //      → 无预分配，每收到数据时动态分配 ReceiveData(byte[], int)
    //      适合数据量小或需要全量回调的场景
    //
    //   2. DownloadHandlerScript(byte[] preallocatedBuffer)
    //      → 预分配固定缓冲区，C++ 原生层直接写入同一块内存
    //      零 GC 分配！适合高性能/大文件流式处理
    //
    //   📌 自定义缓冲原理
    //     预分配缓冲区模式下：
    //     1. C++ 端直接将网络数据写入 preallocatedBuffer
    //     2. 每次写入后回调 ReceiveData() 通知托管层处理
    //     3. 处理完毕后缓冲区可被复用
    //     4. 完全避免托管堆分配（zero-allocation）
    //
    //   ⚠️ 约束
    //     - 预分配缓冲区不能为 null 或空数组
    //     - 子类必须重写 ReceiveData 处理数据
    //     - 数据可能分多次接收，需处理粘包
    //
    //   🎯 适用场景
    //     - 自定义解密/解压
    //     - 增量解析（如 JSON streaming / SAX parser）
    //     - 大文件分块处理
    //     - 需要极低 GC 压力的实时系统
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerScript.h")]
    public class DownloadHandlerScript : DownloadHandler
    {
        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerScript obj);
        private extern static IntPtr CreatePreallocated([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerScript obj, [UnityMarshalAs(NativeType.ScriptingObjectPtr)]byte[] preallocatedBuffer);

        private void InternalCreateScript()
        {
            m_Ptr = Create(this);
        }

        private void InternalCreateScript(byte[] preallocatedBuffer)
        {
            m_Ptr = CreatePreallocated(this, preallocatedBuffer);
        }

        public DownloadHandlerScript()
        {
            InternalCreateScript();
        }

        public DownloadHandlerScript(byte[] preallocatedBuffer)
        {
            if (preallocatedBuffer == null || preallocatedBuffer.Length < 1)
            {
                throw new System.ArgumentException("Cannot create a preallocated-buffer DownloadHandlerScript backed by a null or zero-length array");
            }

            InternalCreateScript(preallocatedBuffer);
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerScript handler) => handler.m_Ptr;
        }
    }

    // ================================================================
    // 📌 DownloadHandlerFile — 虚拟文件系统下载处理器
    //   💡 直接将下载数据写入磁盘文件，避免内存占用
    //   ⚡ 支持断点续传（append=true）
    //   ⚠️ GetData()/GetText() 抛出 NotSupportedException
    // 🎯 适合大文件下载（几百 MB 以上）
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerVFS.h")]
    public sealed class DownloadHandlerFile : DownloadHandler
    {
        [NativeMethod(ThrowsException = true)]
        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerFile obj, string path, bool append);

        private void InternalCreateVFS(string path, bool append)
        {
            string dir = Path.GetDirectoryName(path);
            // On UWP CreateDirectory fails when passing something like Application.presistentDataPath (works if subdir of it)
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            m_Ptr = Create(this, path, append);
        }

        public DownloadHandlerFile(string path)
        {
            InternalCreateVFS(path, false);
        }

        public DownloadHandlerFile(string path, bool append)
        {
            InternalCreateVFS(path, append);
        }

        protected override NativeArray<byte> GetNativeData()
        {
            throw new System.NotSupportedException("Raw data access is not supported");
        }

        protected override byte[] GetData()
        {
            throw new System.NotSupportedException("Raw data access is not supported");
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported");
        }

        public extern bool removeFileOnAbort { get; set; }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerFile handler) => handler.m_Ptr;
        }

    }

    // ================================================================
    // 📌 DownloadHandlerStream — 流式下载处理器（internal）
    //   💡 基于 Span<byte> 的零拷贝流式读取
    //   ⚡ IsThreadSafe: PopData() 可在工作线程调用
    //   🎯 headersCompleted 事件可注册头部完成回调
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerStream.h")]
    internal sealed class DownloadHandlerStream : DownloadHandler
    {
        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerStream obj);

        private bool m_headersComplete = false;
        private System.Action m_headersCompleteCallback;

        private void InternalCreateStream()
        {
            m_Ptr = Create(this);
        }

        public DownloadHandlerStream()
        {
            InternalCreateStream();
        }

        [NativeMethod(IsThreadSafe = true)]
        private extern int PopData(Span<byte> outData);

        [NativeMethod(IsThreadSafe = true)]
        public extern void Close();

        public int ReadData(Span<byte> outData)
        {
            return InternalReadData(outData);
        }

        internal int InternalReadData(Span<byte> outData)
        {
            if (m_Ptr == IntPtr.Zero) return 0;

            return PopData(outData);
        }

        internal override void CompleteHeaders()
        {
            if (m_headersCompleteCallback != null)
            {
                m_headersCompleteCallback();
                m_headersCompleteCallback = null;
            }
            m_headersComplete = true;
        }

        public event System.Action headersCompleted
        {
            add
            {
                if (m_headersComplete)
                {
                    value();
                }
                else
                {
                    m_headersCompleteCallback += value;
                }
            }
            remove
            {
                m_headersCompleteCallback -= value;
            }
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerStream handler) => handler.m_Ptr;
        }
    }
}
