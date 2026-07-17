// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 UnityWebRequest — Unity 内置的 HTTP 网络请求系统
// ================================================================
//
// 📌 概述（Overview）
//   UnityWebRequest 是 Unity 5.4+ 引入的完整 HTTP 客户端 API，
//   用于替代旧版 WWW 类。它是一个功能丰富的网络请求框架，
//   支持 GET/POST/PUT/DELETE 等 HTTP 方法，以及自定义方法。
//
// 🏗 核心架构（Architecture）
//   UnityWebRequest 采用「三处理器」架构，将请求和响应分离：
//
//   ┌─────────────────────────────────────────────────────────────┐
//   │                    UnityWebRequest                          │
//   │  ┌─────────────┐  ┌──────────────────┐  ┌──────────────┐  │
//   │  │UploadHandler│  │  CertificateHandler │  │DownloadHandler│ │
//   │  │ (上行数据)   │  │   (证书验证)        │  │ (下行数据)    │  │
//   │  └─────────────┘  └──────────────────┘  └──────────────┘  │
//   └─────────────────────────────────────────────────────────────┘
//
//   - UploadHandler：负责编码和发送请求体数据（POST/PUT）
//     常见实现：UploadHandlerRaw（原始字节）, UploadHandlerForm（表单）
//
//   - CertificateHandler：负责 SSL/TLS 证书验证
//     可自定义验证逻辑，用于开发环境跳过自签名证书等
//
//   - DownloadHandler：负责接收和解码响应数据
//     常见实现：DownloadHandlerBuffer（缓冲区）,
//               DownloadHandlerAssetBundle（AssetBundle）,
//               DownloadHandlerTexture（纹理）,
//               DownloadHandlerAudioClip（音频）
//
// 🔄 生命周期（Lifecycle）
//   UnityWebRequest 的完整生命周期分为以下阶段：
//
//   1️⃣ 创建阶段（Create）
//      var request = new UnityWebRequest(url, method);
//      // 或使用工厂方法：UnityWebRequest.Get(url)
//
//   2️⃣ 配置阶段（Configure）
//      request.uploadHandler = new UploadHandlerRaw(data);
//      request.downloadHandler = new DownloadHandlerBuffer();
//      request.SetRequestHeader("Content-Type", "application/json");
//
//   3️⃣ 发送阶段（Send）
//      var op = request.SendWebRequest();
//      // 此时返回 UnityWebRequestAsyncOperation，可用于协程等待
//
//   4️⃣ 等待阶段（Wait）
//      yield return request.SendWebRequest();
//      // 或检查 request.isDone
//
//   5️⃣ 结果处理（Process）
//      if (request.result == UnityWebRequest.Result.Success) {
//          string text = request.downloadHandler.text;
//      }
//
//   6️⃣ 释放阶段（Dispose）
//      request.Dispose();
//      // 释放所有处理器和原生资源
//
// 📊 错误处理（Error Handling）
//   UnityWebRequest.Result 枚举定义了请求结果：
//   - InProgress：请求进行中
//   - Success：请求成功（HTTP 2xx）
//   - ConnectionError：网络连接错误（DNS 解析失败、连接超时等）
//   - ProtocolError：HTTP 协议错误（4xx/5xx 状态码）
//   - DataProcessingError：数据处理错误（如下载的数据格式不正确）
//
// 🎯 与旧版 WWW 的对比
//   ┌──────────────────┬──────────────────────┬──────────────────┐
//   │      特性        │        WWW           │  UnityWebRequest │
//   ├──────────────────┼──────────────────────┼──────────────────┤
//   │ HTTP 方法        │ 仅 GET/POST          │ 任意 HTTP 方法   │
//   │ 超时控制         │ 无                   │ timeout 属性     │
//   │ 自定义请求头     │ 有限                 │ 完全支持         │
//   │ 证书验证         │ 无                   │ CertificateHandler│
//   │ 异步操作         │ 协程                 │ 协程/await       │
//   │ 上传进度         │ 无                   │ uploadProgress   │
//   │ 重定向控制       │ 无                   │ redirectLimit    │
//   │ 内存效率         │ 较低                 │ 较高             │
//   └──────────────────┴──────────────────────┴──────────────────┘
//
// 💡 性能提示
//   1. 使用 using 或 Dispose() 及时释放资源
//   2. 下载大文件时使用 DownloadHandlerFile 避免内存占用
//   3. 复用 UnityWebRequest 实例可减少对象创建开销
//   4. 设置 timeout 避免长时间挂起
// ================================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Networking
{
    // ================================================================
    // UnityWebRequestAsyncOperation — 异步操作句柄
    // ================================================================
    // 📌 作用：
    //   UnityWebRequest.SendWebRequest() 返回此对象，
    //   继承自 AsyncOperation，可用于：
    //   - yield return（协程等待）
    //   - .completed 事件（回调模式）
    //   - .isDone 属性（轮询模式）
    //
    // 💡 关键属性：
    //   - webRequest：关联的 UnityWebRequest 实例
    //     通过此引用可访问请求结果和下载数据
    //
    // ⚡ 注意：此对象由 Unity 内部创建，不应手动实例化
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Modules/UnityWebRequest/Public/UnityWebRequestAsyncOperation.h")]
    [NativeHeader("UnityWebRequestScriptingClasses.h")]
    public class UnityWebRequestAsyncOperation : AsyncOperation
    {
        public UnityWebRequestAsyncOperation() { }

        private UnityWebRequestAsyncOperation(IntPtr ptr) : base(ptr) {}

        public UnityWebRequest webRequest { get; internal set; }

        new internal static class BindingsMarshaller
        {
            public static UnityWebRequestAsyncOperation ConvertToManaged(IntPtr ptr) => new UnityWebRequestAsyncOperation(ptr);
        }
    }

    public enum HttpForcedVersion
    {
        // 📌 强制 HTTP 版本
        // NotForced：不强制，使用系统默认协商（推荐）
        // HTTP1_0：强制使用 HTTP/1.0（兼容旧服务器）
        // HTTP1_1：强制使用 HTTP/1.1
        // HTTP2：强制使用 HTTP/2（需要服务器支持）
        //
        // 💡 使用场景：
        //   - 测试环境需要强制特定协议版本
        //   - 某些服务器仅支持特定 HTTP 版本
        //   - 性能优化时强制 HTTP/2 多路复用
        NotForced = 0,
        HTTP1_0 = 1,
        HTTP1_1 = 2,
        HTTP2 = 3,
    }

    // ================================================================
    // UnityWebRequest — 核心请求类
    // ================================================================
    // 📌 核心职责：
    //   1. 管理 HTTP 请求的完整生命周期
    //   2. 协调 UploadHandler、DownloadHandler、CertificateHandler
    //   3. 提供状态查询和结果获取接口
    //
    // 🔧 内部结构（Internal Structure）
    //   - m_Ptr：原生对象指针（IntPtr），指向 C++ 端的 UnityWebRequest 实例
    //   - m_DownloadHandler：下载处理器引用
    //   - m_UploadHandler：上传处理器引用
    //   - m_CertificateHandler：证书处理器引用
    //   - m_Uri：目标 URI（缓存）
    //
    // ⚡ 伪 null 机制
    //   UnityWebRequest 实现了 Unity 的伪 null 模式：
    //   - 创建后 m_Ptr 有效
    //   - Dispose 后 m_Ptr = IntPtr.Zero
    //   - 可用于 if (request) 检查是否有效
    //
    // 📊 UnityWebRequestMethod 内部枚举
    //   - Get = 0：HTTP GET（获取资源）
    //   - Post = 1：HTTP POST（提交数据）
    //   - Put = 2：HTTP PUT（更新资源）
    //   - Head = 3：HTTP HEAD（仅获取响应头）
    //   - Custom = 4：自定义方法（如 PATCH、DELETE）
    //
    // 📊 UnityWebRequestError 内部错误码
    //   定义了各种可能的网络错误类型：
    //   - OK/OKCached：无错误
    //   - SDKError：SDK 初始化失败
    //   - UnsupportedProtocol：不支持的协议
    //   - MalformattedUrl：格式错误的 URL
    //   - CannotResolveHost：DNS 解析失败
    //   - CannotConnectToHost：无法连接到主机
    //   - AccessDenied：访问被拒绝
    //   - Timeout：请求超时
    //   - SSLCannotConnect：SSL 连接失败
    //   - SSLCertificateError：SSL 证书错误
    //   - TooManyRedirects：重定向次数过多
    //   - NoInternetConnection：无网络连接
    //   - InsecureConnectionNotAllowed：不允许不安全连接
    //   等共 40+ 种错误类型
    //
    // 📊 Result 公开枚举
    //   用户可直接检查的请求结果：
    //   - InProgress (0)：请求仍在进行中
    //   - Success (1)：请求成功完成
    //   - ConnectionError (2)：网络连接错误（无法到达服务器）
    //   - ProtocolError (3)：HTTP 协议错误（4xx/5xx）
    //   - DataProcessingError (4)：数据处理错误
    //
    // 💡 状态机转换
    //   [创建] → [配置] → [发送] → [进行中] → [完成]
    //     ↑                              ↑          ↓
    //     │                              │     [Success | Error]
    //     │                              │          ↓
    //     └──────────[Dispose]←──────────┴──[释放资源]
    // ================================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UnityWebRequest.h")]
    public partial class UnityWebRequest : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        [System.NonSerialized]
        internal DownloadHandler m_DownloadHandler;

        [System.NonSerialized]
        internal UploadHandler m_UploadHandler;

        [System.NonSerialized]
        internal CertificateHandler m_CertificateHandler;

        [System.NonSerialized]
        internal Uri m_Uri;

        internal enum UnityWebRequestMethod
        {
            Get = 0,
            Post = 1,
            Put = 2,
            Head = 3,
            Custom = 4
        }

        internal enum UnityWebRequestError
        {
            OK = 0,     // No Error
            OKCached = 1,
            Unknown = 2,
            SDKError = 3,     // SDK error, such as initialization failed
            UnsupportedProtocol = 4,
            MalformattedUrl = 5,
            CannotResolveProxy = 6,
            CannotResolveHost = 7,
            CannotConnectToHost = 8,
            AccessDenied = 9,
            GenericHttpError = 10,
            WriteError = 11,
            ReadError = 12,
            OutOfMemory = 13,
            Timeout = 14,
            HTTPPostError = 15,
            SSLCannotConnect = 16,
            Aborted = 17,
            TooManyRedirects = 18,
            ReceivedNoData = 19,
            SSLNotSupported = 20,
            FailedToSendData = 21,
            FailedToReceiveData = 22,
            SSLCertificateError = 23,
            SSLCipherNotAvailable = 24,
            SSLCACertError = 25,
            UnrecognizedContentEncoding = 26,
            LoginFailed = 27,
            SSLShutdownFailed = 28,
            RedirectLimitInvalid = 29,
            InvalidRedirect = 30,
            CannotModifyRequest = 31,
            HeaderNameContainsInvalidCharacters = 32,
            HeaderValueContainsInvalidCharacters = 33,
            CannotOverrideSystemHeaders = 34,
            AlreadySent = 35,
            InvalidMethod = 36,
            NotImplemented = 37,
            NoInternetConnection = 38,
            DataProcessingError = 39,
            InsecureConnectionNotAllowed = 40,
        }

        public enum Result
        {
            InProgress = 0,
            Success = 1,
            ConnectionError = 2,
            ProtocolError = 3,
            DataProcessingError = 4,
        }


        // ================================================================
        // HTTP 方法常量（HTTP Verb Constants）
        // ================================================================
        // 📌 这些常量定义了标准的 HTTP 请求方法名称
        //
        // 💡 使用示例：
        //   var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        //   var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE);
        //
        // ⚠️ 注意：kHttpVerbCREATE 和 kHttpVerbDELETE 不是标准 HTTP 方法，
        //   它们是 Unity 扩展的特殊方法（如某些 API 需要）。
        //
        // 📊 HTTP 方法说明：
        //   - GET：获取资源（幂等，安全）
        //   - HEAD：仅获取响应头，不获取响应体
        //   - POST：提交数据（非幂等）
        //   - PUT：完整更新资源（幂等）
        //   - CREATE：创建资源（非标准）
        //   - DELETE：删除资源（幂等）
        // ================================================================
        public const string kHttpVerbGET = "GET";
        public const string kHttpVerbHEAD = "HEAD";
        public const string kHttpVerbPOST = "POST";
        public const string kHttpVerbPUT = "PUT";
        public const string kHttpVerbCREATE = "CREATE";
        public const string kHttpVerbDELETE = "DELETE";

        // ================================================================
        // disposeXxxOnDispose — 控制处理器释放行为
        // ================================================================
        // 📌 这三个属性控制 Dispose() 时是否自动释放关联的处理器
        //
        // 💡 默认值：
        //   - disposeCertificateHandlerOnDispose = true
        //   - disposeDownloadHandlerOnDispose = true
        //   - disposeUploadHandlerOnDispose = true
        //
        // ⚡ 使用场景：
        //   当你需要在多个请求间复用同一个处理器时：
        //   1. 设置对应的 disposeXxxOnDispose = false
        //   2. 请求完成后手动管理处理器生命周期
        //   3. 适合高频率请求的性能优化
        //
        // ⚠️ 注意：
        //   如果设为 false 但不手动释放，会导致内存泄漏！
        // ================================================================
        public bool disposeCertificateHandlerOnDispose { get; set; }
        public bool disposeDownloadHandlerOnDispose { get; set; }
        public bool disposeUploadHandlerOnDispose { get; set; }

        // ================================================================
        // ClearCookieCache — 清除 Cookie 缓存
        // ================================================================
        // 📌 三种重载：
        //   1. ClearCookieCache()：清除所有域名的 Cookie
        //   2. ClearCookieCache(Uri uri)：清除指定 URI 相关的 Cookie
        //      自动提取域名和路径进行过滤
        //   3. ClearCookieCache(domain, path)：精确清除指定域名/路径的 Cookie
        //
        // 💡 使用场景：
        //   - 用户登出时清除认证 Cookie
        //   - 需要强制重新登录时
        //   - 测试环境清理 Cookie 状态
        //
        // ⚠️ 注意：这是全局操作，会影响所有使用 UnityWebRequest 的请求
        // ================================================================
        public static void ClearCookieCache()
        {
            ClearCookieCache(null, null);
        }

        public static void ClearCookieCache(Uri uri)
        {
            if (uri == null)
                ClearCookieCache(null, null);
            else
            {
                string domain = uri.Host;
                string path = uri.AbsolutePath;
                if (path == "/")
                    path = null;
                ClearCookieCache(domain, path);
            }
        }

        private static extern void ClearCookieCache(string domain, string path);

        // ================================================================
        // 构造函数 — 创建 UnityWebRequest
        // ================================================================
        // 📌 共有 6 个构造函数重载，从简单到完整：
        //
        // 1. UnityWebRequest()
        //    - 创建空请求，需手动设置 url、method、handler
        //
        // 2. UnityWebRequest(string url)
        //    - 创建 GET 请求（默认方法为 GET）
        //    - URL 会被自动规范化（处理相对路径等）
        //
        // 3. UnityWebRequest(Uri uri)
        //    - 同上，但接受 Uri 类型（必须是绝对 URI）
        //
        // 4. UnityWebRequest(string url, string method)
        //    - 指定 URL 和 HTTP 方法
        //    - 方法名不区分大小写，会自动转换为大写
        //
        // 5. UnityWebRequest(Uri uri, string method)
        //    - 同上，接受 Uri 类型
        //
        // 6. UnityWebRequest(string url, string method, DownloadHandler, UploadHandler)
        //    - 最完整的构造函数，指定所有参数
        //    - 适合需要完全控制的场景
        //
        // ⚡ 性能提示：
        //   所有构造函数都会调用 Create() 分配原生内存，
        //   因此应避免在循环中频繁创建。
        //   优先使用工厂方法如 UnityWebRequest.Get(url)。
        // ================================================================


        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_UNITYWEBREQUEST")]
        private extern static string GetWebErrorString(UnityWebRequestError err);
        [VisibleToOtherModules]
        internal extern static string GetHTTPStatusString(long responseCode);

        public bool disposeCertificateHandlerOnDispose { get; set; }

        public bool disposeDownloadHandlerOnDispose { get; set; }

        public bool disposeUploadHandlerOnDispose { get; set; }

        public static void ClearCookieCache()
        {
            ClearCookieCache(null, null);
        }

        public static void ClearCookieCache(Uri uri)
        {
            if (uri == null)
                ClearCookieCache(null, null);
            else
            {
                string domain = uri.Host;
                string path = uri.AbsolutePath;
                if (path == "/")
                    path = null;
                ClearCookieCache(domain, path);
            }
        }

        private static extern void ClearCookieCache(string domain, string path);

        [NativeMethod(ThrowsException = true)]
        internal extern static IntPtr Create();

        [NativeMethod(IsThreadSafe = true)]
        private extern void Release();

        internal void InternalDestroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Abort();
                Release();
                m_Ptr = IntPtr.Zero;
            }
        }

        private void InternalSetDefaults()
        {
            this.disposeDownloadHandlerOnDispose = true;
            this.disposeUploadHandlerOnDispose = true;
            this.disposeCertificateHandlerOnDispose = true;
        }

        public UnityWebRequest()
        {
            m_Ptr = Create();
            InternalSetDefaults();
        }

        public UnityWebRequest(string url)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.url = url;
        }

        public UnityWebRequest(Uri uri)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.uri = uri;
        }

        public UnityWebRequest(string url, string method)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.url = url;
            this.method = method;
        }

        public UnityWebRequest(Uri uri, string method)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.uri = uri;
            this.method = method;
        }

        public UnityWebRequest(string url, string method, DownloadHandler downloadHandler, UploadHandler uploadHandler)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.url = url;
            this.method = method;
            this.downloadHandler = downloadHandler;
            this.uploadHandler = uploadHandler;
        }

        public UnityWebRequest(Uri uri, string method, DownloadHandler downloadHandler, UploadHandler uploadHandler)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.uri = uri;
            this.method = method;
            this.downloadHandler = downloadHandler;
            this.uploadHandler = uploadHandler;
        }


        ~UnityWebRequest()
        {
            DisposeHandlers();
            InternalDestroy();
        }

        public void Dispose()
        {
            DisposeHandlers();
            InternalDestroy();
            GC.SuppressFinalize(this);
        }

        private void DisposeHandlers()
        {
            if (disposeDownloadHandlerOnDispose)
            {
                DownloadHandler dh = this.downloadHandler;
                if (dh != null)
                {
                    dh.Dispose();
                }
            }

            if (disposeUploadHandlerOnDispose)
            {
                UploadHandler uh = this.uploadHandler;
                if (uh != null)
                {
                    uh.Dispose();
                }
            }

            if (disposeCertificateHandlerOnDispose)
            {
                CertificateHandler ch = this.certificateHandler;
                if (ch != null)
                {
                    ch.Dispose();
                }
            }
        }

        [NativeMethod(ThrowsException = true)]
        internal extern UnityWebRequestAsyncOperation BeginWebRequest();

        [Obsolete("Use SendWebRequest.  It returns a UnityWebRequestAsyncOperation which contains a reference to the WebRequest object.", false)]
        public AsyncOperation Send() {return SendWebRequest(); }

        public UnityWebRequestAsyncOperation SendWebRequest()
        {
            UnityWebRequestAsyncOperation webOp = BeginWebRequest();
            if (webOp != null)
                webOp.webRequest = this;
            return webOp;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern void Abort();

        private extern UnityWebRequestError SetMethod(UnityWebRequestMethod methodType);

        internal void InternalSetMethod(UnityWebRequestMethod methodType)
        {
            if (!isModifiable)
                throw new InvalidOperationException("UnityWebRequest has already been sent and its request method can no longer be altered");

            UnityWebRequestError ret = SetMethod(methodType);
            if (ret != UnityWebRequestError.OK)
                throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
        }

        private extern UnityWebRequestError SetCustomMethod(string customMethodName);

        internal void InternalSetCustomMethod(string customMethodName)
        {
            if (!isModifiable)
                throw new InvalidOperationException("UnityWebRequest has already been sent and its request method can no longer be altered");

            UnityWebRequestError ret = SetCustomMethod(customMethodName);
            if (ret != UnityWebRequestError.OK)
                throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
        }

        internal extern UnityWebRequestMethod GetMethod();
        internal extern string GetCustomMethod();

        public string method
        {
            get
            {
                UnityWebRequestMethod m = GetMethod();
                switch (m)
                {
                    case UnityWebRequestMethod.Get:
                        return kHttpVerbGET;
                    case UnityWebRequestMethod.Post:
                        return kHttpVerbPOST;
                    case UnityWebRequestMethod.Put:
                        return kHttpVerbPUT;
                    case UnityWebRequestMethod.Head:
                        return kHttpVerbHEAD;
                    default:
                        return GetCustomMethod();
                }
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Cannot set a UnityWebRequest's method to an empty or null string");
                }

                switch (value.ToUpper())
                {
                    case kHttpVerbGET:
                        InternalSetMethod(UnityWebRequestMethod.Get);
                        break;
                    case kHttpVerbPOST:
                        InternalSetMethod(UnityWebRequestMethod.Post);
                        break;
                    case kHttpVerbPUT:
                        InternalSetMethod(UnityWebRequestMethod.Put);
                        break;
                    case kHttpVerbHEAD:
                        InternalSetMethod(UnityWebRequestMethod.Head);
                        break;
                    default:
                        InternalSetCustomMethod(value.ToUpper());
                        break;
                }
            }
        }

        private extern UnityWebRequestError GetError();

        public string error
        {
            get
            {
                switch (result)
                {
                    case Result.InProgress:
                    case Result.Success:
                        return null;
                    case Result.ProtocolError:
                        return string.Format("HTTP/1.1 {0} {1}", responseCode, GetHTTPStatusString(responseCode));
                    default:
                        return GetWebErrorString(GetError());
                }
            }
        }

        private extern bool use100Continue { get; set; }

        public bool useHttpContinue
        {
            get { return use100Continue; }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent and its 100-Continue setting cannot be altered");
                use100Continue = value;
            }
        }

        public string url
        {
            get
            {
                return GetUrl();
            }

            set
            {
                // We need to sanitize the incoming URL so it's a proper absolute URL
                // This permits us to allow relative URLs and correct minor user mistakes.

                string localUrl = "https://localhost/";

                InternalSetUrl(WebRequestUtils.MakeInitialUrl(value, localUrl));
            }
        }

        public Uri uri
        {
            get
            {
                // always return from native (it will change in case of redirect)
                return new Uri(GetUrl());
            }
            set
            {
                if (!value.IsAbsoluteUri)
                    throw new ArgumentException("URI must be absolute");
                InternalSetUrl(WebRequestUtils.MakeUriString(value, value.OriginalString, false));
                m_Uri = value;
            }
        }

        private extern string GetUrl();
        private extern UnityWebRequestError SetUrl(string url);

        private void InternalSetUrl(string url)
        {
            if (!isModifiable)
                throw new InvalidOperationException("UnityWebRequest has already been sent and its URL cannot be altered");

            UnityWebRequestError ret = SetUrl(url);
            if (ret != UnityWebRequestError.OK)
                throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
        }

        public extern long responseCode { get; }
        private extern float GetUploadProgress();
        private extern bool IsExecuting();

        public float uploadProgress
        {
            get
            {
                if (!(IsExecuting() || isDone))
                    return -1.0f;
                else
                    return GetUploadProgress();
            }
        }

        public extern bool isModifiable {[NativeMethod("IsModifiable")] get; }
        public bool isDone { get { return result != Result.InProgress; } }
        // These two are referenced by packages, deprecate after packages get updated
        [System.Obsolete("UnityWebRequest.isNetworkError is deprecated. Use (UnityWebRequest.result == UnityWebRequest.Result.ConnectionError) instead.", false)]
        public bool isNetworkError { get { return result == Result.ConnectionError; } }
        [System.Obsolete("UnityWebRequest.isHttpError is deprecated. Use (UnityWebRequest.result == UnityWebRequest.Result.ProtocolError) instead.", false)]
        public bool isHttpError { get { return result == Result.ProtocolError; } }
        public extern Result result { [NativeMethod("GetResult")] get; }

        private extern float GetDownloadProgress();

        public float downloadProgress
        {
            get
            {
                if (!(IsExecuting() || isDone))
                    return -1.0f;
                else
                    return GetDownloadProgress();
            }
        }

        public extern ulong uploadedBytes { get; }
        public extern ulong downloadedBytes { get; }

        private extern int GetRedirectLimit();
        [NativeMethod(ThrowsException = true)]
        private extern void SetRedirectLimitFromScripting(int limit);

        public int redirectLimit
        {
            get { return GetRedirectLimit(); }
            set { SetRedirectLimitFromScripting(value); }
        }

        private extern bool GetChunked();
        private extern UnityWebRequestError SetChunked(bool chunked);

        [Obsolete("HTTP/2 and many HTTP/1.1 servers don't support this; we recommend leaving it set to false (default).", false)]
        public bool chunkedTransfer
        {
            get { return GetChunked(); }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent and its chunked transfer encoding setting cannot be altered");

                UnityWebRequestError ret = SetChunked(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
            }
        }

        public extern string GetRequestHeader(string name);

        [NativeMethod("SetRequestHeader")]
        internal extern UnityWebRequestError InternalSetRequestHeader(string name, string value);

        public void SetRequestHeader(string name, string value)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Cannot set a Request Header with a null or empty name");

            // Only check for null here, as in general header value can be empty, i.e. Accept-Encoding can have empty value according spec.
            if (value == null)
                throw new ArgumentException("Cannot set a Request header with a null");
            if (!isModifiable)
                throw new InvalidOperationException("UnityWebRequest has already been sent and its request headers cannot be altered");

            UnityWebRequestError ret = InternalSetRequestHeader(name, value);
            if (ret != UnityWebRequestError.OK)
                throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
        }

        public extern string GetResponseHeader(string name);

        internal extern string[] GetResponseHeaderKeys();

        public Dictionary<string, string> GetResponseHeaders()
        {
            string[] headerKeys = GetResponseHeaderKeys();
            if (headerKeys == null || headerKeys.Length == 0)
            {
                return null;
            }

            Dictionary<string, string> headers = new Dictionary<string, string>(headerKeys.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerKeys.Length; i++)
            {
                string val = GetResponseHeader(headerKeys[i]);
                headers.Add(headerKeys[i], val);
            }

            return headers;
        }

        public extern string GetResponseTrailer(string name);

        internal extern string[] GetResponseTrailerKeys();

        public Dictionary<string, string> GetResponseTrailers()
        {
            string[] headerKeys = GetResponseTrailerKeys();
            if (headerKeys == null || headerKeys.Length == 0)
            {
                return null;
            }

            Dictionary<string, string> headers = new Dictionary<string, string>(headerKeys.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerKeys.Length; i++)
            {
                string val = GetResponseTrailer(headerKeys[i]);
                headers.Add(headerKeys[i], val);
            }

            return headers;
        }

        private extern UnityWebRequestError SetUploadHandler(UploadHandler uh);

        public UploadHandler uploadHandler
        {
            get
            {
                return m_UploadHandler;
            }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the upload handler");
                UnityWebRequestError ret = SetUploadHandler(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
                m_UploadHandler = value;
            }
        }

        private extern UnityWebRequestError SetDownloadHandler(DownloadHandler dh);

        public DownloadHandler downloadHandler
        {
            get
            {
                return m_DownloadHandler;
            }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the download handler");
                UnityWebRequestError ret = SetDownloadHandler(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
                m_DownloadHandler = value;
            }
        }

        private extern UnityWebRequestError SetCertificateHandler(CertificateHandler ch);

        public CertificateHandler certificateHandler
        {
            get
            {
                return m_CertificateHandler;
            }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the certificate handler");
                UnityWebRequestError ret = SetCertificateHandler(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
                m_CertificateHandler = value;
            }
        }
        private extern int GetTimeoutMsec();
        private extern UnityWebRequestError SetTimeoutMsec(int timeout);

        public int timeout
        {
            get { return GetTimeoutMsec() / 1000; }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the timeout");

                value = Math.Max(value, 0);
                UnityWebRequestError ret = SetTimeoutMsec(value * 1000);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
            }
        }

        private extern bool GetSuppressErrorsToConsole();
        private extern UnityWebRequestError SetSuppressErrorsToConsole(bool suppress);

        internal bool suppressErrorsToConsole
        {
            get { return GetSuppressErrorsToConsole(); }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the timeout");
                UnityWebRequestError ret = SetSuppressErrorsToConsole(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
            }
        }

        private extern HttpForcedVersion GetHttpForcedVersion();
        private extern UnityWebRequestError SetHttpForcedVersion(HttpForcedVersion forceHttp2);

        public HttpForcedVersion httpForcedVersion
        {
            get { return GetHttpForcedVersion(); }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the protocol version");
                UnityWebRequestError ret = SetHttpForcedVersion(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
            }
        }

        private extern string responseVersionString { get; }
        internal Version responseVersion
        {
            get
            {
                return new Version(responseVersionString);
            }
        }

        // accept certificate for addresses which starts from url, required for running tests
        internal extern static void AcceptCertificateForUrl(string url);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UnityWebRequest unityWebRequest) => unityWebRequest.m_Ptr;
        }
    }
}
