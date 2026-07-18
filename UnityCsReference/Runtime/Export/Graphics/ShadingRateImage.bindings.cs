// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ====================================================================
// 🎯 可变速率着色(VRS) — ShadingRateImage的分配大小计算
// 💡 GetAllocSizeInternal根据像素宽高计算VRS tile的宽高
// 💡 VRS允许对不同屏幕区域使用不同的着色率，优化性能/质量平衡
// ⚡ 底层实现在ShadingRateImage.h中，通过FreeFunction暴露
// ====================================================================

using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/ShadingRateImage.h")]
    public static partial class ShadingRateImage
    {
        [FreeFunction("ShadingRateImage::GetAllocSizeInternal")]
        internal static extern void GetAllocSizeInternal(int pixelWidth, int pixelHeight, out int tileWidth, out int tileHeight);
    }
}
