// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// SpriteMask — Unity 2D 精灵遮罩系统
//
// 🎯 核心作用：
//   SpriteMask 用于控制 2D 精灵的可见区域。
//   通过指定一个 Sprite 的形状作为遮罩，可以让其他精灵
//   只在遮罩区域内（或区域外）显示。
//
// 📌 工作原理：
//   1. 遮罩自身需要一个 Sprite 作为形状模板
//   2. 被遮罩的 SpriteRenderer 设置 maskInteraction 属性：
//      - VisibleInsideMask：仅在遮罩形状内可见
//      - VisibleOutsideMask：仅在遮罩形状外可见
//   3. 通过 frontSortingLayerID/frontSortingOrder 和
//      backSortingLayerID/backSortingOrder 控制遮罩作用的排序层范围
//
// 💡 使用场景：
//   - 角色被墙壁遮挡时只显示上半身
//   - 圆形视野效果（战争迷雾）
//   - UI 元素的圆形/异形裁切
//
// 🔑 关键属性：
//   - alphaCutoff：Alpha 测试阈值，低于此值的像素不参与遮罩
//   - isCustomRangeActive：是否启用自定义排序范围
//   - spriteSortPoint：遮罩的排序参考点
//
// 🔗 继承链：
//   SpriteMask -> Renderer -> Component -> Object
//
// 📍 对应 C++ 头文件：Modules/SpriteMask/Public/SpriteMask.h
// ==============================================================

using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    // ==============================================================
    // SpriteMask — 2D 精灵遮罩组件
    // ==============================================================
    // 🎯 通过 Sprite 形状控制其他精灵的可见区域
    //
    // 📌 MaskSource：遮罩数据来源
    //   - Sprite：使用 sprite 属性指定的精灵作为遮罩形状
    //   - SupportedRenderers：使用支持的渲染器作为遮罩
    //
    // 📌 排序范围控制：
    //   frontSortingLayerID/frontSortingOrder：遮罩影响的最大排序层
    //   backSortingLayerID/backSortingOrder：遮罩影响的最小排序层
    //   仅在此范围内的 Renderer 才会受到遮罩影响
    // ==============================================================
    [RejectDragAndDropMaterial]
    [NativeHeader("Modules/SpriteMask/Public/SpriteMask.h")]
    public sealed partial class SpriteMask : Renderer
    {
        // ==============================================================
        // MaskSource — 遮罩数据来源枚举
        // ==============================================================
        //   - Sprite：使用 sprite 属性指定的精灵形状
        //   - SupportedRenderers：使用支持的渲染器作为遮罩源
        // ==============================================================
        public enum MaskSource
        {
            Sprite = 0,
            SupportedRenderers = 1,
        }

        extern public int frontSortingLayerID { get; set; }
        extern public int frontSortingOrder { get; set; }
        extern public int backSortingLayerID { get; set; }
        extern public int backSortingOrder { get; set; }
        extern public float alphaCutoff { get; set; }
        extern public Sprite sprite { get; set; }
        extern public bool isCustomRangeActive {[NativeMethod("IsCustomRangeActive")] get; [NativeMethod("SetCustomRangeActive")] set; }

        public extern SpriteSortPoint spriteSortPoint { get; set; }

        public extern MaskSource maskSource { get; set; }

        internal extern Renderer cachedSupportedRenderer { get; }

        internal extern Bounds GetSpriteBounds();
    }

    [NativeHeader("Modules/SpriteMask/Public/ScriptBindings/SpriteMask.bindings.h")]
    [StaticAccessor("SpriteUtilityBindings", StaticAccessorType.DoubleColon)]
    internal static class SpriteMaskUtility
    {
        extern internal static bool HasSpriteMaskInLayerRange(SortingLayerRange range);
    }
}
