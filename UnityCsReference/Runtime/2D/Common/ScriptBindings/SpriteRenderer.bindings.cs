// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// SpriteRenderer — Unity 2D 精灵渲染器
//
// 🎯 核心作用：
//   SpriteRenderer 是 Unity 2D 游戏中最核心的渲染组件，
//   负责将 Sprite（精灵图）渲染到屏幕上。
//   它继承自 Renderer，是 2D 游戏对象的主要视觉呈现方式。
//
// 📌 三种绘制模式（Draw Mode）：
//   1. Simple（简单）：直接渲染整个 Sprite，最常用
//   2. Sliced（九宫格切片）：根据 Sprite.border 进行九宫格拉伸，
//      边角保持原始大小，中间区域拉伸填充——常用于 UI 背景
//   3. Tiled（平铺）：将 Sprite 重复平铺填满指定区域——常用于地砖/墙壁
//
// 💡 关键概念：
//   - flipX/flipY：水平/垂直翻转 Sprite（不改变几何体，只改变 UV）
//   - color：颜色叠加，可实现半透明和着色效果
//   - maskInteraction：与 SpriteMask 配合实现遮罩效果
//   - spriteSortPoint：控制排序基准点（Center 或 Pivot）
//   - sprite 属性变更时会触发 InvokeSpriteChanged 回调
//
// 🔗 继承链：
//   SpriteRenderer -> Renderer -> Component -> Object
//
// 📍 对应 C++ 头文件：Runtime/Graphics/Mesh/SpriteRenderer.h
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Events;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // ==============================================================
    // SpriteDrawMode — Sprite 绘制模式枚举
    // ==============================================================
    // 控制 SpriteRenderer 如何渲染 Sprite：
    //   - Simple：直接渲染整个 Sprite（默认模式）
    //   - Sliced：九宫格切片模式，根据 border 值将 Sprite 分为 9 区域，
    //     边角保持原始大小，边缘和中心区域拉伸填充
    //   - Tiled：平铺模式，将 Sprite 在 size 指定的区域内重复平铺
    // ==============================================================
    public enum SpriteDrawMode
    {
        Simple,
        Sliced,
        Tiled
    }

    // ==============================================================
    // SpriteTileMode — Sprite 平铺模式枚举
    // ==============================================================
    // 仅在 SpriteDrawMode.Tiled 时生效：
    //   - Continuous：连续平铺，完整地重复 Sprite
    //   - Adaptive：自适应平铺，根据区域大小自动调整 Sprite 数量
    // ==============================================================
    public enum SpriteTileMode
    {
        Continuous,
        Adaptive
    }

    // ==============================================================
    // SpriteMaskInteraction — Sprite 遮罩交互模式
    // ==============================================================
    // 控制 SpriteRenderer 如何与 SpriteMask 配合：
    //   - None：不参与遮罩（默认）
    //   - VisibleInsideMask：仅在遮罩区域内可见
    //   - VisibleOutsideMask：仅在遮罩区域外可见
    // ==============================================================
    public enum SpriteMaskInteraction
    {
        None = 0,
        VisibleInsideMask = 1,
        VisibleOutsideMask = 2
    }

    // ==============================================================
    // SpriteRenderer — 2D 精灵渲染器组件
    // ==============================================================
    // 🎯 将 Sprite 渲染到屏幕上的核心组件
    //
    // 📌 关键属性：
    //   sprite：要渲染的 Sprite 资源
    //   drawMode：绘制模式（Simple/Sliced/Tiled）
    //   size：Sliced/Tiled 模式下的渲染尺寸
    //   tileMode：Tiled 模式下的平铺方式
    //   color：颜色叠加（含透明度）
    //   flipX/flipY：水平/垂直翻转
    //   maskInteraction：遮罩交互模式
    //   spriteSortPoint：排序参考点（Center/Pivot）
    //
    // ⚡ 事件机制：
    //   RegisterSpriteChangeCallback() 注册精灵变更回调，
    //   当 sprite 属性被修改时自动触发。
    // ==============================================================
    [NativeHeader("Runtime/Graphics/Mesh/SpriteRenderer.h")]
    [RequireComponent(typeof(Transform))]
    public sealed partial class SpriteRenderer : Renderer
    {
        UnityEvent<SpriteRenderer> m_SpriteChangeEvent;

        public void RegisterSpriteChangeCallback(UnityEngine.Events.UnityAction<SpriteRenderer> callback)
        {
            if (m_SpriteChangeEvent == null)
                m_SpriteChangeEvent = new UnityEvent<SpriteRenderer>();
            m_SpriteChangeEvent.AddListener(callback);
            hasSpriteChangeEvents = true;
        }

        public void UnregisterSpriteChangeCallback(UnityEngine.Events.UnityAction<SpriteRenderer> callback)
        {
            if (m_SpriteChangeEvent != null)
            {
                m_SpriteChangeEvent.RemoveListener(callback);
                if (0 == m_SpriteChangeEvent.GetCallsCount())
                    hasSpriteChangeEvents = false;
            }
        }

        [RequiredByNativeCode]
        void InvokeSpriteChanged()
        {
            try
            {
                m_SpriteChangeEvent?.Invoke(this);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        internal extern bool shouldSupportTiling
        {
            [NativeMethod("ShouldSupportTiling")]
            get;
        }

        internal extern bool hasSpriteChangeEvents
        {
            get;
            set;
        }

        public extern Sprite sprite
        {
            get;
            set;
        }

        public extern SpriteDrawMode drawMode
        {
            get;
            set;
        }

        public extern Vector2 size
        {
            get;
            set;
        }

        public extern float adaptiveModeThreshold
        {
            get;
            set;
        }

        public extern SpriteTileMode tileMode
        {
            get;
            set;
        }

        public extern Color color
        {
            get;
            set;
        }

        public extern SpriteMaskInteraction maskInteraction
        {
            get;
            set;
        }

        public extern bool flipX
        {
            get;
            set;
        }

        public extern bool flipY
        {
            get;
            set;
        }

        public extern SpriteSortPoint spriteSortPoint
        {
            get;
            set;
        }

        extern public float GetBlendShapeWeight(int index);
        extern public void SetBlendShapeWeight(int index, float value);
        extern internal int GetBlendShapeChannelCount();

        extern internal bool IsSkinned();

        extern IntPtr GetCurrentMeshDataPtr();
        
        internal unsafe Mesh.MeshDataArray GetCurrentMeshData()
        {
            var ptr = GetCurrentMeshDataPtr();
            if (ptr == IntPtr.Zero)
                return new Mesh.MeshDataArray(0);
            var result = new Mesh.MeshDataArray(1);
            result.m_Ptrs[0] = ptr;
            return result;
        }

        [NativeMethod(Name = "GetSpriteBounds")]
        internal extern Bounds Internal_GetSpriteBounds(SpriteDrawMode mode);
        extern internal void GetSecondaryTextureProperties([NotNull]MaterialPropertyBlock mbp);
        internal Bounds GetSpriteBounds()
        {
            return Internal_GetSpriteBounds(drawMode);
        }
    }

}
