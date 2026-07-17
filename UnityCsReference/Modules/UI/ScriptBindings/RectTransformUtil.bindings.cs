// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    // =====================================================
    // RectTransformUtility — Native 绑定部分
    // =====================================================
    // 此文件包含 RectTransformUtility 中调用 C++ 端（Native）的方法声明。
    // StaticAccessor("UI", ...) 表示这些方法对应 C++ 端的 "UI" 命名空间。
    //
    // 对应 C++ 头文件：
    // - Modules/UI/RectTransformUtil.h — 主要的 Native 实现
    // - Runtime/Camera/Camera.h — PixelAdjust 方法需要 Camera 信息
    // - Modules/UI/Canvas.h — 需要 Canvas 的渲染模式信息
    // - Runtime/Transform/RectTransform.h — RectTransform 数据访问
    // =====================================================

    [NativeHeader("Runtime/Camera/Camera.h"),
     NativeHeader("Modules/UI/Canvas.h"),
     NativeHeader("Modules/UI/RectTransformUtil.h"),
     NativeHeader("Runtime/Transform/RectTransform.h"),
     StaticAccessor("UI", StaticAccessorType.DoubleColon)]
    partial class RectTransformUtility
    {
        /// <summary>
        /// 对指定点进行像素修正（Pixel Perfect 调整）。
        /// 当 Canvas 启用 pixelPerfect 时，将点坐标对齐到最近的整数像素位置，
        /// 消除因子像素位置导致的渲染模糊。
        ///
        /// 参数：
        ///   point            — 要修正的点（在 elementTransform 本地空间中）
        ///   elementTransform — 元素所在的 Transform
        ///   canvas           — 所属的 Canvas（用于获取 pixelPerfect 设置）
        ///
        /// 返回：修正后的位置（在元素本地空间中）。
        /// 如果 Canvas 没有启用 pixelPerfect，则返回原始位置。
        /// </summary>
        public static extern Vector2 PixelAdjustPoint(Vector2 point, Transform elementTransform, Canvas canvas);

        /// <summary>
        /// 对 RectTransform 进行像素修正（Pixel Perfect 调整）。
        /// 将 RectTransform 的位置和大小调整到整数像素边界。
        ///
        /// 参数：
        ///   rectTransform — 要修正的 RectTransform
        ///   canvas        — 所属的 Canvas（用于获取 pixelPerfect 设置）
        ///
        /// 返回：修正后的矩形。
        /// 用于 UI 元素需要精确对齐到像素的场景（如像素风格游戏）。
        /// </summary>
        public static extern Rect PixelAdjustRect(RectTransform rectTransform, Canvas canvas);

        /// <summary>
        /// [Native] 判断屏幕坐标点是否在 RectTransform 的矩形内。
        /// 这是 RectangleContainsScreenPoint 方法的底层实现，
        /// 直接在 C++ 端进行高效的矩形-点碰撞检测。
        ///
        /// offset 参数允许对检测区域进行扩展/收缩，
        /// 例如用于扩大点击区域（增加触控容差）或缩小检测范围。
        /// </summary>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <param name="rect">要检测的 RectTransform</param>
        /// <param name="cam">视角 Camera（可为 null）</param>
        /// <param name="offset">检测偏移量 (left, bottom, right, top)</param>
        /// <returns>true = 点在矩形内</returns>
        private static extern bool PointInRectangle(Vector2 screenPoint, RectTransform rect, Camera cam, Vector4 offset);
    }
}
