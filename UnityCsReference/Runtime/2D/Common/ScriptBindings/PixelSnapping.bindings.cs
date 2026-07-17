// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// PixelSnapping — 像素完美渲染工具
//
// 🎯 核心作用：
//   PixelPerfectRendering 提供像素对齐（Pixel Snapping）功能，
//   确保 2D 精灵在渲染时对齐到像素网格，避免因亚像素偏移
//   导致的模糊和抖动。
//
// 📌 工作原理：
//   - pixelSnapSpacing：定义像素对齐的间距
//   - 当启用时，渲染位置会自动吸附到最近的像素网格点
//   - 值为 1 表示对齐到每个像素，值为 0.5 表示半像素对齐
//
// 💡 使用场景：
//   - 像素艺术风格游戏：确保像素清晰锐利
//   - 2D 平台游戏：防止精灵在移动时出现亚像素抖动
//   - 配合 Pixel Perfect Camera 组件使用效果最佳
//
// ⚠️ 注意事项：
//   - 从 Experimental.U2D 迁移到正式 API（[MovedFrom]）
//   - 过大的 snap spacing 值会导致精灵"跳跃式"移动
//   - 建议配合 2D Project Settings 中的 Pixel Snap 设置使用
// ==============================================================

using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.U2D
{
    // ==============================================================
    // PixelPerfectRendering — 像素完美渲染静态工具类
    // ==============================================================
    // 🎯 提供全局像素对齐控制
    //
    // 📌 pixelSnapSpacing 属性：
    //   定义像素对齐的网格间距。
    //   - 值为 1：对齐到每个像素（经典像素游戏）
    //   - 值为 0.5：半像素精度
    //   - 值为 0：禁用像素对齐
    //
    // 💡 与 Pixel Perfect Camera 的关系：
    //   Pixel Perfect Camera 组件内部使用此属性，
    //   手动设置时需注意与 Camera 的像素比率配合。
    //
    // ⚠️ 注意：
    //   从 UnityEngine.Experimental.U2D 迁移至此命名空间
    //   （[MovedFrom] 标记保持向后兼容）
    // ==============================================================
    [MovedFrom("UnityEngine.Experimental.U2D")]
    [NativeHeader("Runtime/2D/Common/PixelSnapping.h")]
    public static class PixelPerfectRendering
    {
        extern static public float pixelSnapSpacing
        {
            [FreeFunction("GetPixelSnapSpacing")]
            get;

            [FreeFunction("SetPixelSnapSpacing")]
            set;
        }
    }
}
