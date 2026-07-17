// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Display — 多显示器管理 API
//
// 📌 作用：
//   Display 类管理 Unity 的显示输出，支持多显示器场景。
//   每个连接的显示器对应一个 Display 实例。
//
// 🏗 核心概念：
//   - displays[]：所有可用显示器的数组
//   - main：主显示器（displays[0]）
//   - renderingWidth/Height：渲染分辨率
//   - systemWidth/Height：系统级分辨率
//   - Activate()：激活并设置显示器分辨率和刷新率
//   - RelativeMouseAt()：多显示器环境下的鼠标坐标转换
//
// 💡 理解关键：
//   Unity 默认只渲染到主显示器。要使用多显示器，
//   需要对每个额外显示器调用 displays[i].Activate()。
//   每个 Display 有独立的颜色缓冲和深度缓冲。
//
// ⚠️  注意：
//   - 多显示器支持取决于目标平台
//   - 移动平台通常只有一个显示器
//   - 游戏主机可能有特殊限制
//
// 📍 对应 C++ 头文件：Runtime/Graphics/DisplayManager.h
// ==============================================================

using System;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // ==============================================================
    // Display — 多显示器管理类
    //
    // 🔑 核心属性与方法：
    //   - main：主显示器（displays[0] 的快捷方式）
    //   - renderingWidth / renderingHeight：当前渲染分辨率
    //   - systemWidth / systemHeight：操作系统显示分辨率
    //   - colorBuffer / depthBuffer：渲染缓冲区引用
    //   - Activate()：激活显示器并设置分辨率/刷新率
    //   - RelativeMouseAt()：多显示器下的鼠标坐标转换
    //   - SetRenderingResolution()：设置渲染分辨率
    //
    // 💡 使用示例：
    //   Display.displays[1].Activate(1920, 1080, 60); // 激活第二个显示器
    // ==============================================================
    [UsedByNativeCode]
    [NativeHeader("Runtime/Graphics/DisplayManager.h")]
    public class Display
    {
        internal IntPtr  nativeDisplay;
        internal Display()
        {
            this.nativeDisplay = new IntPtr(0);
        }

        internal Display(IntPtr nativeDisplay)   { this.nativeDisplay = nativeDisplay; }

        public int    renderingWidth
        {
            get
            {
                int w = 0, h = 0;
                GetRenderingExtImpl(nativeDisplay, out w, out h);
                return w;
            }
        }
        public int    renderingHeight
        {
            get
            {
                int w = 0, h = 0;
                GetRenderingExtImpl(nativeDisplay, out w, out h);
                return h;
            }
        }

        public int    systemWidth
        {
            get
            {
                int w = 0, h = 0;
                GetSystemExtImpl(nativeDisplay, out w, out h);
                return w;
            }
        }
        public int    systemHeight
        {
            get
            {
                int w = 0, h = 0;
                GetSystemExtImpl(nativeDisplay, out w, out h);
                return h;
            }
        }

        public RenderBuffer colorBuffer
        {
            get
            {
                RenderBuffer color, depth;
                GetRenderingBuffersImpl(nativeDisplay, out color, out depth);
                return color;
            }
        }

        public RenderBuffer depthBuffer
        {
            get
            {
                RenderBuffer color, depth;
                GetRenderingBuffersImpl(nativeDisplay, out color, out depth);
                return depth;
            }
        }

        public bool active
        {
            get
            {
                return GetActiveImpl(nativeDisplay);
            }
        }

        public bool requiresBlitToBackbuffer
        {
            get
            {
                int displayIndex = nativeDisplay.ToInt32();
                if (displayIndex < HDROutputSettings.displays.Length)
                {
                    bool active = HDROutputSettings.displays[displayIndex].available && HDROutputSettings.displays[displayIndex].active;
                    if (active)
                        return true;
                }
                return RequiresBlitToBackbufferImpl(nativeDisplay);
            }
        }

        public bool requiresSrgbBlitToBackbuffer
        {
            get
            {
                return RequiresSrgbBlitToBackbufferImpl(nativeDisplay);
            }
        }

        public void Activate()
        {
            ActivateDisplayImpl(nativeDisplay, 0, 0, new RefreshRate() { numerator = 60, denominator = 1 });
        }

        public void Activate(int width, int height, RefreshRate refreshRate)
        {
            ActivateDisplayImpl(nativeDisplay, width, height, refreshRate);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Activate(int, int, int) is deprecated. Use Activate(int, int, RefreshRate) instead.", false)]
        public void Activate(int width, int height, int refreshRate)
        {
            if (refreshRate < 0)
                refreshRate = 0;

            ActivateDisplayImpl(nativeDisplay, width, height, new RefreshRate() { numerator = (uint)refreshRate, denominator = 1 });
        }

        public void SetParams(int width, int height, int x, int y)
        {
            SetParamsImpl(nativeDisplay, width, height, x, y);
        }

        public void SetRenderingResolution(int w, int h)
        {
            SetRenderingResolutionImpl(nativeDisplay, w, h);
        }

        [System.Obsolete("MultiDisplayLicense has been deprecated.", false)]
        public static bool MultiDisplayLicense()
        {
            return true;
        }

        public static Vector3 RelativeMouseAt(Vector3 inputMouseCoordinates)
        {
            Vector3 vec;
            int rx = 0, ry = 0;
            int x = (int)inputMouseCoordinates.x;
            int y = (int)inputMouseCoordinates.y;
            vec.z = (int)RelativeMouseAtImpl(x, y, out rx, out ry);
            vec.x = rx;
            vec.y = ry;
            return vec;
        }

        public static Display[] displays    = new Display[1] { new Display() };
        private static Display _mainDisplay = displays[0];
        public static Display   main        { get {return _mainDisplay; } }

        private static int m_ActiveEditorGameViewTarget = 0;

        public static int activeEditorGameViewTarget  { get { return m_ActiveEditorGameViewTarget; } internal set { m_ActiveEditorGameViewTarget = value; } }

        [RequiredByNativeCode]
        internal static void RecreateDisplayList(IntPtr[] nativeDisplay)
        {
            if (nativeDisplay.Length == 0) // case 1017288
                return;

            Display.displays = new Display[nativeDisplay.Length];
            for (int i = 0; i < nativeDisplay.Length; ++i)
                Display.displays[i] = new Display(nativeDisplay[i]);

            _mainDisplay = displays[0];
        }

        [RequiredByNativeCode]
        internal static void FireDisplaysUpdated()
        {
            if (onDisplaysUpdated != null)
                onDisplaysUpdated();
        }

        public delegate void DisplaysUpdatedDelegate();
        public static event DisplaysUpdatedDelegate onDisplaysUpdated = null;

        [FreeFunction("UnityDisplayManager_DisplaySystemResolution")]
        extern private static void GetSystemExtImpl(IntPtr nativeDisplay, out int w, out int h);

        [FreeFunction("UnityDisplayManager_DisplayRenderingResolution")]
        extern private static void GetRenderingExtImpl(IntPtr nativeDisplay, out int w, out int h);

        [FreeFunction("UnityDisplayManager_GetRenderingBuffersWrapper")]
        extern private static void GetRenderingBuffersImpl(IntPtr nativeDisplay, out RenderBuffer color, out RenderBuffer depth);

        [FreeFunction("UnityDisplayManager_SetRenderingResolution")]
        extern private static void SetRenderingResolutionImpl(IntPtr nativeDisplay, int w, int h);

        [FreeFunction("UnityDisplayManager_ActivateDisplay")]
        extern private static void ActivateDisplayImpl(IntPtr nativeDisplay, int width, int height, RefreshRate refreshRate);

        [FreeFunction("UnityDisplayManager_SetDisplayParam")]
        extern private static void SetParamsImpl(IntPtr nativeDisplay, int width, int height, int x, int y);

        [FreeFunction("UnityDisplayManager_RelativeMouseAt")]
        extern private static int RelativeMouseAtImpl(int x, int y, out int rx, out int ry);

        [FreeFunction("UnityDisplayManager_DisplayActive")]
        extern private static bool GetActiveImpl(IntPtr nativeDisplay);

        [FreeFunction("UnityDisplayManager_RequiresBlitToBackbuffer")]
        extern private static bool RequiresBlitToBackbufferImpl(IntPtr nativeDisplay);

        [FreeFunction("UnityDisplayManager_RequiresSRGBBlitToBackbuffer")]
        extern private static bool RequiresSrgbBlitToBackbufferImpl(IntPtr nativeDisplay);
    }
}

namespace UnityEngineInternal
{
    internal class DisplayInternal
    {
        [FreeFunction("UnityDisplayManager_PrimaryDisplayIndex")]
        extern internal static int PrimaryDisplayIndex();

        internal static bool IsASecondaryDisplayIndex(int displayIndex)
        {
            return displayIndex >= 0 && displayIndex < UnityEngine.Display.displays.Length && displayIndex != PrimaryDisplayIndex();
        }
    }
}
