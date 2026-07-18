// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ====================================================================
// 🎯 水印系统 — Unity启动时的水印可见性控制
// 💡 IsVisible检测当前是否有任何水印可见
// 💡 showDeveloperWatermark控制开发者水印的显示/隐藏
// ⚡ 通过NativeProperty直接操作C++端s_ShowDeveloperWatermark静态字段
// ====================================================================

using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/DrawSplashScreenAndWatermarks.h")]
    public class Watermark
    {
        [FreeFunction("IsAnyWatermarkVisible")]
        extern public static bool IsVisible();

        [NativeProperty("s_ShowDeveloperWatermark", false, TargetType.Field)]
        public extern static bool showDeveloperWatermark
        {
            get;
            set;
        }
    }
} // namespace UnityEngine.Rendering
