// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ====================================================================
// 🎯 图形设备调试设置 — 内部调试GraphicsJobs和纹理上传的延迟模拟
// 💡 GraphicsDeviceDebugSettings控制jobs启动延迟和纹理上传前延迟
// 💡 仅用于Unity内部调试/测试，不暴露给用户
// ⚡ 通过StaticAccessor直接访问C++端GraphicsDeviceDebug静态字段
// ====================================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngineInternal
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GraphicsDeviceDebugSettings
    {
        public float sleepAtStartOfGraphicsJobs;
        public float sleepBeforeTextureUpload;
    }

    [NativeHeader("Runtime/Export/Graphics/GraphicsDeviceDebug.bindings.h")]
    [StaticAccessor("GraphicsDeviceDebug", StaticAccessorType.DoubleColon)]
    internal static class GraphicsDeviceDebug
    {
        extern internal static GraphicsDeviceDebugSettings settings { get; set; }
    }
}
