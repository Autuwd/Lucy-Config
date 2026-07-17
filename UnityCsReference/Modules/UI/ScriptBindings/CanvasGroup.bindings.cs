// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEngine
{
    /// <summary>
    /// Canvas 射线过滤接口。
    /// 实现此接口的组件可以动态决定是否响应射线检测（点击/悬停等）。
    /// CanvasGroup 和 Mask 组件实现了此接口。
    /// </summary>
    public interface ICanvasRaycastFilter
    {
        /// <summary>
        /// 判断指定屏幕位置是否有效的射线命中点。
        /// </summary>
        /// <param name="sp">屏幕坐标位置</param>
        /// <param name="eventCamera">发起射线检测的 Camera</param>
        /// <returns>true = 此位置接受射线命中，false = 穿透</returns>
        bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera);
    }

    // =====================================================
    // CanvasGroup — 画布组
    // =====================================================
    // CanvasGroup 用于批量控制一组 UI 元素的显示属性，无需单独设置每个元素。
    //
    // 核心功能：
    // 1. alpha（透明度）—— 控制整组 UI 元素的透明度，子元素透明度会叠加
    // 2. interactable（可交互）—— 控制整组 UI 元素是否可以交互
    // 3. blocksRaycasts（阻挡射线）—— 控制整组 UI 元素是否响应点击
    // 4. ignoreParentGroups（忽略父级）—— 是否忽略上级 CanvasGroup 的影响
    //
    // 透明度叠加机制：
    // - CanvasGroup 的 alpha 会与子元素的 alpha 相乘
    // - 例如：父 CanvasGroup.alpha=0.5，子 Graphic.color.a=0.5，
    //   最终显示透明度 = 0.5 × 0.5 = 0.25
    //
    // 交互控制：
    // - interactable=false 时，该组下所有可交互组件（Button、InputField 等）都不可点击
    // - blocksRaycasts=false 时，射线将穿透该组直接作用于底层 UI
    //
    // 典型应用场景：
    // - 整个面板的淡入/淡出动画（只需控制 CanvasGroup.alpha）
    // - 禁用整个 UI 面板的交互（Loading 时禁止点击）
    // - UI 弹窗的遮罩层（半透明背景，阻挡射线）
    //
    // 注意：CanvasGroup 的效果是可继承的，嵌套的 CanvasGroup 会叠加效果。
    // 使用 ignoreParentGroups=true 可以打破继承链。
    // =====================================================

    [NativeClass("UI::CanvasGroup"),
     NativeHeader("Modules/UI/CanvasGroup.h")]
    [UIModuleHelpURL("class-CanvasGroup")]
    public sealed class CanvasGroup : Behaviour, ICanvasRaycastFilter
    {
        /// <summary>
        /// 整体透明度（0~1）。
        /// 0 = 完全透明（不可见），1 = 完全不透明。
        /// 与子元素的 alpha 相乘计算最终透明度。
        /// 常用于淡入/淡出动画：一个 CanvasGroup 控制整个面板的透明度。
        /// </summary>
        [NativeProperty("Alpha", false, TargetType.Function)] public extern float alpha { get; set; }

        /// <summary>
        /// 是否可交互。
        /// false 时，此组内的所有可交互 UI 元素（Button、Toggle、Slider 等）
        /// 都无法与用户交互（即使各自组件的 interactable 为 true）。
        /// 常用于加载状态中禁用整个 UI。
        /// </summary>
        [NativeProperty("Interactable", false, TargetType.Function)] public extern bool interactable { get; set; }

        /// <summary>
        /// 是否阻挡射线检测。
        /// true = 此组会阻挡射线通过（点击会命中此组内的元素）。
        /// false = 射线穿透此组（点击不会命中此组内的元素，事件会传到下层）。
        /// 用于实现"点击穿透"效果。
        /// </summary>
        [NativeProperty("BlocksRaycasts", false, TargetType.Function)] public extern bool blocksRaycasts { get; set; }

        /// <summary>
        /// 是否忽略父级 CanvasGroup 的影响。
        /// true = 父级 CanvasGroup 的 alpha/interactable/blocksRaycasts 设置不作用于本组。
        /// 用于打破 CanvasGroup 的继承链。
        /// 注意：此属性只影响父 CanvasGroup 对本组的影响，
        /// 不影响本组对子元素的影响。
        /// </summary>
        [NativeProperty("IgnoreParentGroups", false, TargetType.Function)] public extern bool ignoreParentGroups { get; set; }

        /// <summary>
        /// 实现 ICanvasRaycastFilter 接口。
        /// 根据 blocksRaycasts 属性决定是否允许射线命中。
        /// 当 blocksRaycasts=false 时，射线会穿透此组。
        /// </summary>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            return blocksRaycasts;
        }
    }
}
