// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 CertificateHandler — SSL/TLS 证书验证处理器
// ================================================================
//
// 📌 职责
//   控制 UnityWebRequest 对 SSL/TLS 证书的验证策略。
//   可用于开发环境跳过自签名证书，或实现自定义证书锁定。
//
// 💡 验证流程
//   UnityWebRequest.SendWebRequest()
//      → C++ 原生层收到服务器证书
//      → 调用 ValidateCertificateNative(byte[])
//      → 调用 ValidateCertificate(byte[])（虚方法）
//      → 返回 true=接受证书，false=拒绝连接
//
//   ⚡ 默认行为：ValidateCertificate() 返回 false
//   → 所有证书将被拒绝！必须派生重写才能通过验证。
//
// 📌 自定义验证器示例
//   class AcceptAllCertificates : CertificateHandler {
//       protected override bool ValidateCertificate(byte[] data) => true;
//   }
//
// ⚡ 验证入口
//   - ValidateCertificateNative() : [RequiredByNativeCode] 原生回调入口
//   - ValidateCertificate()        : protected virtual，供子类重写
//   - ValidateCertificateExternal(): internal，供同一程序集调用
//
// ⚠️ 安全注意事项
//   1. 永远不要在发布版本中无条件返回 true（中间人攻击风险）
//   2. 自签名证书验证仅限开发/测试环境
//   3. 证书锁定（Certificate Pinning）推荐使用 data 参数做哈希比对
//   4. disposeCertificateHandlerOnDispose 控制生命周期
// ================================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/CertificateHandler/CertificateHandlerScript.h")]
    public class CertificateHandler : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        extern private static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] CertificateHandler obj);

        [NativeMethod(IsThreadSafe = true)]
        extern private void ReleaseFromScripting();

        protected CertificateHandler()
        {
            m_Ptr = Create(this);
        }

        ~CertificateHandler()
        {
            Dispose();
        }

        protected virtual bool ValidateCertificate(byte[] certificateData)
        {
            return false;
        }

        [RequiredByNativeCode]
        internal bool ValidateCertificateNative(byte[] certificateData)
        {
            return ValidateCertificate(certificateData);
        }

        [VisibleToOtherModules]
        internal bool ValidateCertificateExternal(byte[] certificateData)
        {
            return ValidateCertificate(certificateData);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                ReleaseFromScripting();
                m_Ptr = IntPtr.Zero;
            }
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(CertificateHandler handler) => handler.m_Ptr;
        }

    }
}
