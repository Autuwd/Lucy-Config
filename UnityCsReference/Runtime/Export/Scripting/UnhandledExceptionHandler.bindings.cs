// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    //=============================================================================
    // 📌 UnhandledExceptionHandler —— 未处理异常处理器
    //
    // 设计说明:
    //   将 AppDomain.CurrentDomain.UnhandledException 事件捕获到的
    //   .NET 未处理异常转发到 Unity 的 Debug.LogException 系统，
    //   确保异常信息能正确显示在 Unity Console 窗口中。
    //
    // ⚠️ 平台限制:
    //   [NativeHeader] 指向 iOS 平台绑定头文件，但注册逻辑是跨平台的。
    //   CoreCLR 不使用此代码路径。
    //
    // 💡 行为说明:
    //   仅记录日志，不会阻止进程终止（UnhandledException 事件的默认行为）。
    //   用于诊断而非恢复，帮助开发者在崩溃前获取最后的异常信息。
    //=============================================================================

    [NativeHeader("PlatformDependent/iPhonePlayer/IOSScriptBindings.h")]
    internal sealed partial class UnhandledExceptionHandler
    {
        [RequiredByNativeCode]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAC0006:AppDomain usage", Justification = "Domain reload in Mono unregisters it. CoreCLR does not use this codepath")]
        static void RegisterUECatcher()
        {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                Debug.LogException(e.ExceptionObject as Exception);
            };
        }

    }
}
