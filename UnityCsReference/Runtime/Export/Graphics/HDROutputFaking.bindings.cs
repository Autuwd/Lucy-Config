// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ====================================================================
// 🎯 HDR输出模拟 — 内部测试用的假HDR显示模式
// 💡 SetEnabled开启模拟HDR输出，用于在非HDR显示器上测试HDR流程
// 💡 IsRealDisplayHDRAvailable检测物理显示器是否真正支持HDR
// ⚡ ExcludeFromDocs标记，仅限内部测试使用
// ====================================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

[assembly: InternalsVisibleTo("TestRuntime")]
[assembly: InternalsVisibleTo("TestRuntime.FakingHDR")]
[assembly: InternalsVisibleTo("UnityEngine.TestTools.Graphics.Contexts")]
namespace UnityEngine.Internal
{
    [NativeHeader("Runtime/GfxDevice/HDROutputSettings.h")]
    [ExcludeFromDocs]
    internal static class InternalHDROutputFaking
    {
        [FreeFunction("HDROutputSettingsBindings::SetFakeHDROutputEnabled")]
        [ExcludeFromDocs]
        extern internal static void SetEnabled(bool enabled);

        [FreeFunction("HDROutputSettingsBindings::IsRealDisplayHDRAvailable")]
        [ExcludeFromDocs]
        extern internal static bool IsRealDisplayHDRAvailable();
    }
}
