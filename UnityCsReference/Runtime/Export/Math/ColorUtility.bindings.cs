// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ColorUtility — 颜色格式转换工具
//
// 📌 作用：
//   在 Color / Color32 与 HTML 颜色字符串间转换
//
// 💡 核心方法（在 Unity 其他源码中实现）：
//   - TryParseHtmlString: "#FF0000" → Color
//   - ToHtmlStringRGB / ToHtmlStringRGBA: Color → "#FF0000" / "#FF0000FF"
//
// 🎯 本文件仅包含底层 C++ 绑定 DoTryParseHtmlColor
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Math/ColorUtility.h")]
    public partial class ColorUtility
    {
        [FreeFunction("TryParseHtmlColor", true)]
        extern internal static bool DoTryParseHtmlColor(string htmlString, out Color32 color);
    }
}
